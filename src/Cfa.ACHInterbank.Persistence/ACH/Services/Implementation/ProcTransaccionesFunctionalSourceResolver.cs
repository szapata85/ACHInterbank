using System.Globalization;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Helpers;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

public sealed class ProcTransaccionesFunctionalSourceResolver : IProcTransaccionesFunctionalSourceResolver
{
    private static readonly IReadOnlyDictionary<string, int> CanonicalClearingHouseIds =
        new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["ACHCOL"] = 1,
            ["CENIT"] = 2
        };

    private readonly AchDbContext _context;
    private readonly object _cacheSync = new();
    private readonly Dictionary<CacheKey, Task<ProcTransaccionesFunctionalSourceContext>> _cache = new();

    public ProcTransaccionesFunctionalSourceResolver(AchDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<ProcTransaccionesFunctionalSourceContext> ResolveAsync(
        IncomingNachaDispatchQueue queueItem,
        IncomingNachaFileIngestion ingestion,
        IncomingNachaEntryClassification classification,
        AchTransaction transaction,
        AchCycle cycle,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(queueItem);
        ArgumentNullException.ThrowIfNull(ingestion);
        ArgumentNullException.ThrowIfNull(classification);
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentNullException.ThrowIfNull(cycle);

        var key = new CacheKey(queueItem.Id, ingestion.Id, classification.Id, transaction.Id, cycle.Id);
        lock (_cacheSync)
        {
            if (!_cache.TryGetValue(key, out var pending))
            {
                pending = ResolveCoreAsync(queueItem, ingestion, classification, transaction, cycle, ct);
                _cache[key] = pending;
            }

            return pending;
        }
    }

    public async Task<ProcTransaccionesFunctionalSourceContext> ResolveAsync(
        IntegrationExecutionContext context,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(context);
        if (!context.IncomingNachaQueueId.HasValue)
        {
            throw new InvalidOperationException($"SOURCE_CONTEXT_REQUIRED: {context.MethodCode} requiere IncomingNachaQueueId.");
        }

        if (!context.TransactionId.HasValue)
        {
            throw new InvalidOperationException($"SOURCE_CONTEXT_REQUIRED: {context.MethodCode} requiere TransactionId.");
        }

        var queue = await _context.IncomingNachaDispatchQueue
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == context.IncomingNachaQueueId.Value, ct)
            ?? throw new KeyNotFoundException($"SOURCE_INCOMING_QUEUE_NOT_FOUND: {context.IncomingNachaQueueId.Value}.");
        var ingestion = await _context.IncomingNachaFileIngestions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == queue.IncomingNachaFileIngestionId, ct)
            ?? throw new KeyNotFoundException($"SOURCE_INGESTION_NOT_FOUND: {queue.IncomingNachaFileIngestionId}.");
        var classification = await _context.IncomingNachaEntryClassifications
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == queue.IncomingNachaEntryClassificationId, ct)
            ?? throw new KeyNotFoundException($"SOURCE_CLASSIFICATION_NOT_FOUND: {queue.IncomingNachaEntryClassificationId}.");
        var transaction = await _context.AchTransactions
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == context.TransactionId.Value, ct)
            ?? throw new KeyNotFoundException($"SOURCE_TRANSACTION_NOT_FOUND: {context.TransactionId.Value}.");

        var cycleId = string.IsNullOrWhiteSpace(context.CycleId) ? transaction.AchCycleId : context.CycleId;
        var cycle = await _context.AchCycles
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == cycleId, ct)
            ?? throw new KeyNotFoundException($"SOURCE_CYCLE_NOT_FOUND: {cycleId}.");

        return await ResolveAsync(queue, ingestion, classification, transaction, cycle, ct);
    }

    public static string ToFunctionalBatchId(string rawBatchNumber)
    {
        var value = (rawBatchNumber ?? string.Empty).Trim();
        if (value.Length != 7 || value.Any(c => !char.IsDigit(c)))
        {
            throw new InvalidOperationException("PROC_TRANSACCIONES_BATCH_NUMBER_INVALID: BatchNumber debe contener exactamente siete dígitos.");
        }

        var numeric = int.Parse(value, CultureInfo.InvariantCulture);
        if (numeric is < 0 or > 999_999)
        {
            throw new InvalidOperationException("PROC_TRANSACCIONES_BATCH_NUMBER_RANGE: BatchNumber debe estar entre 0000000 y 0999999.");
        }

        return numeric.ToString("D6", CultureInfo.InvariantCulture);
    }

    private async Task<ProcTransaccionesFunctionalSourceContext> ResolveCoreAsync(
        IncomingNachaDispatchQueue queueItem,
        IncomingNachaFileIngestion ingestion,
        IncomingNachaEntryClassification classification,
        AchTransaction transaction,
        AchCycle cycle,
        CancellationToken ct)
    {
        var nachaSource = await LoadNachaSourceContextAsync(queueItem, classification, ingestion, ct);

        var institutions = await _context.FinancialInstitutions
            .AsNoTracking()
            .Where(x => x.Id == transaction.SourceInstitutionId || x.Id == transaction.DestinationInstitutionId)
            .ToListAsync(ct);
        var source = institutions.SingleOrDefault(x => x.Id == transaction.SourceInstitutionId)
            ?? throw new InvalidOperationException("PROC_TRANSACCIONES_SOURCE_INSTITUTION_MISSING: no existe la institución originadora enlazada.");
        var destination = institutions.SingleOrDefault(x => x.Id == transaction.DestinationInstitutionId)
            ?? throw new InvalidOperationException("PROC_TRANSACCIONES_DESTINATION_INSTITUTION_MISSING: no existe la institución receptora enlazada.");

        var sourceCode = NormalizeTransitCode(source.TransitCode, "BCOORIG");
        var destinationCode = NormalizeTransitCode(destination.TransitCode, "BCORECEP");
        if (!destination.IsDefaultSource)
        {
            throw new InvalidOperationException("PROC_TRANSACCIONES_DESTINATION_NOT_CFA: la institución receptora no es la CFA canónica.");
        }

        var clearingHouse = await _context.ClearingHouses
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == cycle.ClearingHouseId, ct)
            ?? throw new InvalidOperationException("PROC_TRANSACCIONES_CLEARING_HOUSE_MISSING: la cámara del ciclo no está resuelta.");
        ValidateCanonicalClearingHouse(clearingHouse);

        var batch = nachaSource.BatchHeader
            ?? throw new InvalidOperationException("PROC_TRANSACCIONES_BATCH_MISSING: no se encontró el BatchHeader de la entrada clasificada.");
        var entry = nachaSource.EntryDetail
            ?? throw new InvalidOperationException("PROC_TRANSACCIONES_ENTRY_MISSING: no existe el registro tipo 6 clasificado.");
        var originatorCode = NormalizeParticipantCode(batch.OriginParticipantEntityCode, "BCOORIG");
        var receiverCode = NormalizeParticipantCode(entry.ReceivingParticipantEntityCode, "BCORECEP");
        ValidateNachaOriginatorEvidence(nachaSource.Header, batch, entry, nachaSource.BatchControl, clearingHouse.Code);

        var configuredInstitutions = await _context.FinancialInstitutions
            .AsNoTracking()
            .Where(x => x.Status == FinancialInstitutionStatus.Active)
            .ToListAsync(ct);
        var sourceFromNacha = ResolveInstitutionByParticipantCode(configuredInstitutions, originatorCode, "BCOORIG");
        var destinationFromNacha = ResolveInstitutionByParticipantCode(configuredInstitutions, receiverCode, "BCORECEP");
        if (string.Equals(clearingHouse.Code, "ACHCOL", StringComparison.OrdinalIgnoreCase))
        {
            var immediateOrigin = (nachaSource.Header?.ImmediateOrigin ?? string.Empty).Trim();
            var expectedImmediateOrigin = $"{originatorCode}{DigitoChequeoHelper.CalcularDigitoChequeo(originatorCode)}";
            if (!string.Equals(immediateOrigin, expectedImmediateOrigin, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("PROC_TRANSACCIONES_ACHCOL_IMMEDIATE_ORIGIN_CHECK_DIGIT_MISMATCH: tipo 1 no coincide con tipo 5 y su dígito de chequeo.");
            }
        }

        if (!destinationFromNacha.IsDefaultSource)
        {
            throw new InvalidOperationException("PROC_TRANSACCIONES_DESTINATION_NOT_CFA: la entidad tipo 6 receptora no es CFA.");
        }

        if (transaction.SourceInstitutionId != sourceFromNacha.Id || transaction.DestinationInstitutionId != destinationFromNacha.Id)
        {
            throw new InvalidOperationException("PROC_TRANSACCIONES_TRANSACTION_INSTITUTION_MISMATCH: la transacción no coincide con las instituciones derivadas de NACHA-M.");
        }

        var addenda = nachaSource.AddendaRecord;
        if (addenda is null || !string.Equals(addenda.CodeTypeAddendumRecord, "05", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("PROC_TRANSACCIONES_ADDENDA_05_MISSING: el crédito entrante requiere addenda tipo 05.");
        }

        var paymentInformation = ProcTransaccionesPaymentInformationBuilder.Build(
            transaction.CompanyIdentification,
            batch.CompanyEntryDescription ?? string.Empty,
            addenda.PaymentRelatedInformation ?? string.Empty);

        return new ProcTransaccionesFunctionalSourceContext(
            sourceCode,
            destinationCode,
            sourceFromNacha.Name,
            destinationFromNacha.Name,
            ToFunctionalBatchId(batch.BatchNumber.ToString("D7", CultureInfo.InvariantCulture)),
            clearingHouse.Id.ToString(CultureInfo.InvariantCulture),
            paymentInformation,
            entry.AccountNumber,
            entry.RecipUserName,
            entry.RecipIdNumber);
    }

    private async Task<NachaSourceContext> LoadNachaSourceContextAsync(
        IncomingNachaDispatchQueue queue,
        IncomingNachaEntryClassification classification,
        IncomingNachaFileIngestion ingestion,
        CancellationToken ct)
    {
        var link = await _context.IncomingNachaTransactionLinks
            .AsNoTracking()
            .Include(x => x.EntryDetail)
            .Include(x => x.AddendaRecord)
            .FirstOrDefaultAsync(x => x.Id == queue.IncomingNachaTransactionLinkId, ct)
            ?? throw new InvalidOperationException("PROC_TRANSACCIONES_QUEUE_LINK_MISSING: la cola no tiene un vínculo NACHA persistido.");

        var entryDetail = link.EntryDetail
            ?? await _context.EntryDetails.AsNoTracking().FirstOrDefaultAsync(x => x.EntryDetailID == classification.EntryDetailId, ct)
            ?? throw new InvalidOperationException("PROC_TRANSACCIONES_ENTRY_MISSING: no existe el registro tipo 6 asociado a la cola.");

        if (classification.EntryDetailId != entryDetail.EntryDetailID)
        {
            throw new InvalidOperationException("PROC_TRANSACCIONES_ENTRY_MISMATCH: la clasificación no coincide con el EntryDetail enlazado a la cola.");
        }

        var nachaId = entryDetail.NachaID;
        var header = !string.IsNullOrWhiteSpace(nachaId)
            ? await _context.NachaHeaders.AsNoTracking().FirstOrDefaultAsync(x => x.NachaID == nachaId, ct)
            : await _context.NachaHeaders.AsNoTracking().FirstOrDefaultAsync(x => x.IncomingNachaFileIngestionId == ingestion.Id, ct);

        nachaId ??= header?.NachaID;

        var batchHeader = string.IsNullOrWhiteSpace(nachaId)
            ? null
            : await _context.BatchHeaders.AsNoTracking().FirstOrDefaultAsync(
                x => x.NachaID == nachaId && x.BatchNumber == entryDetail.BatchNumber,
                ct);

        var addendaRecord = link.AddendaRecord
            ?? (classification.AddendaRecordId.HasValue
                ? await _context.AddendaRecords.AsNoTracking().FirstOrDefaultAsync(x => x.AddendaID == classification.AddendaRecordId.Value, ct)
                : null);

        if (addendaRecord is null && !string.IsNullOrWhiteSpace(nachaId))
        {
            addendaRecord = await _context.AddendaRecords
                .AsNoTracking()
                .OrderBy(x => x.AddendaID)
                .FirstOrDefaultAsync(x => x.NachaID == nachaId
                    && (x.EntryDetailSequenceNumber == entryDetail.SequenceNumber
                        || (!string.IsNullOrWhiteSpace(x.EntryDetailSequenceNumber)
                            && !string.IsNullOrWhiteSpace(entryDetail.SequenceNumber)
                            && entryDetail.SequenceNumber.EndsWith(x.EntryDetailSequenceNumber))
                        || x.OriginalTraceNumber == entryDetail.SequenceNumber), ct);
        }

        var batchControl = string.IsNullOrWhiteSpace(nachaId) || batchHeader is null
            ? null
            : await _context.BatchControls.AsNoTracking().OrderBy(x => x.BatchControlID).FirstOrDefaultAsync(
                x => x.NachaID == nachaId
                    && x.BatchNumber == batchHeader.BatchNumber.ToString("D7", CultureInfo.InvariantCulture), ct);

        return new NachaSourceContext(header, batchHeader, entryDetail, addendaRecord, batchControl);
    }

    private static void ValidateNachaOriginatorEvidence(
        NachaHeader? header,
        BatchHeader batch,
        EntryDetail entry,
        BatchControl? control,
        string clearingHouseCode)
    {
        var originator = NormalizeParticipantCode(batch.OriginParticipantEntityCode, "BCOORIG");
        if (originator.All(c => c == '0'))
        {
            throw new InvalidOperationException("PROC_TRANSACCIONES_ORIGINATOR_ZERO: el código originador NACHA-M no puede ser ceros.");
        }

        var trace = (entry.SequenceNumber ?? string.Empty).Trim();
        if (trace.Length != 15 || trace.Any(c => !char.IsDigit(c)) || !string.Equals(trace[..8], originator, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("PROC_TRANSACCIONES_ORIGINATOR_TRACE_MISMATCH: tipo 5 no coincide con tipo 6.");
        }

        if (control is null || !string.Equals((control.IdOrigEntity ?? string.Empty).Trim(), originator, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("PROC_TRANSACCIONES_ORIGINATOR_CONTROL_MISMATCH: tipo 5 no coincide con tipo 8.");
        }

        if (string.Equals(clearingHouseCode, "ACHCOL", StringComparison.OrdinalIgnoreCase))
        {
            var immediateOrigin = (header?.ImmediateOrigin ?? string.Empty).Trim();
            if (immediateOrigin.Length != 9 || immediateOrigin.Any(c => !char.IsDigit(c)) || !string.Equals(immediateOrigin[..8], originator, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("PROC_TRANSACCIONES_ACHCOL_IMMEDIATE_ORIGIN_MISMATCH: tipo 1 no coincide con tipo 5.");
            }
        }
    }

    private static FinancialInstitution ResolveInstitutionByParticipantCode(
        IReadOnlyCollection<FinancialInstitution> institutions,
        string participantCode,
        string parameter)
    {
        var matches = institutions.Where(x => string.Equals(
            $"{x.RoutingNumber?.Trim()}{x.TransitCode?.Trim()}", participantCode, StringComparison.Ordinal)).ToList();
        return matches.Count switch
        {
            1 => matches[0],
            0 => throw new InvalidOperationException($"PROC_TRANSACCIONES_INSTITUTION_NOT_FOUND: {parameter}={participantCode} no existe en FinancialInstitutions."),
            _ => throw new InvalidOperationException($"PROC_TRANSACCIONES_INSTITUTION_AMBIGUOUS: {parameter}={participantCode} tiene más de una institución activa.")
        };
    }

    private static string NormalizeParticipantCode(string? value, string parameter)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length != 8 || normalized.Any(c => !char.IsDigit(c)))
        {
            throw new InvalidOperationException($"PROC_TRANSACCIONES_ORIGINATOR_INVALID: {parameter} requiere ocho dígitos NACHA-M.");
        }

        return normalized;
    }

    private static string NormalizeTransitCode(string? value, string parameter)
    {
        var normalized = (value ?? string.Empty).Trim();
        if (normalized.Length != 3 || normalized.Any(c => !char.IsDigit(c)))
        {
            throw new InvalidOperationException($"PROC_TRANSACCIONES_TRANSIT_CODE_INVALID: {parameter} requiere TransitCode numérico de tres dígitos.");
        }

        return int.Parse(normalized, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
    }

    private static void ValidateCanonicalClearingHouse(ClearingHouse clearingHouse)
    {
        if (!CanonicalClearingHouseIds.TryGetValue(clearingHouse.Code, out var expected))
        {
            throw new InvalidOperationException("PROC_TRANSACCIONES_CLEARING_HOUSE_UNSUPPORTED: la cámara no es ACHCOL ni CENIT.");
        }

        if (clearingHouse.Id != expected)
        {
            throw new InvalidOperationException($"PROC_TRANSACCIONES_CLEARING_HOUSE_ID_INVALID: {clearingHouse.Code} debe conservar Id canónico {expected}.");
        }
    }

    private sealed record NachaSourceContext(
        NachaHeader? Header,
        BatchHeader? BatchHeader,
        EntryDetail? EntryDetail,
        AddendaRecord? AddendaRecord,
        BatchControl? BatchControl);

    private readonly record struct CacheKey(
        Guid QueueId,
        Guid IngestionId,
        Guid ClassificationId,
        int TransactionId,
        string CycleId);
}
