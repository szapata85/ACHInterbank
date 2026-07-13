using System.Globalization;
using System.Xml.Linq;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Helpers;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Integrations.Services;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class ProcTransaccionesRequestMapper : IProcTransaccionesRequestMapper
{
    private static readonly XNamespace ActionNamespace = "http://tempuri.org/";
    private readonly AchDbContext _context;
    private readonly IntegrationMappingSnapshotBuilder _snapshotBuilder;

    public ProcTransaccionesRequestMapper(AchDbContext context, IntegrationMappingSnapshotBuilder? snapshotBuilder = null)
    {
        _context = context;
        _snapshotBuilder = snapshotBuilder ?? new IntegrationMappingSnapshotBuilder(context);
    }

    public async Task<ProcTransaccionesRequestResolution> ResolveAsync(
        IncomingNachaDispatchQueue queueItem,
        IncomingNachaFileIngestion ingestion,
        IncomingNachaEntryClassification classification,
        AchTransaction transaction,
        AchCycle cycle,
        DateTime executionDateTime,
        CancellationToken ct = default)
    {
        var method = await _context.IntegrationMethods
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == "WSCFAACH.Proc_Transacciones" && x.IsActive, ct)
            ?? throw new InvalidOperationException("No existe IntegrationMethod activo para WSCFAACH.Proc_Transacciones.");

        var publishedMappingSets = await _context.IntegrationMappingSets
            .AsNoTracking()
            .Where(x => x.MethodId == method.Id
                && x.Status == IntegrationMappingSetStatusEnum.Published
                && x.IsActive)
            .OrderByDescending(x => x.Version)
            .ToListAsync(ct);
        if (publishedMappingSets.Count == 0)
            throw new InvalidOperationException("PUBLISHED_MAPPING_NOT_FOUND: no existe IntegrationMappingSet publicado y activo para Proc_Transacciones.");
        if (publishedMappingSets.Count != 1)
            throw new InvalidOperationException("MAPPING_SET_NOT_UNIQUE: existe más de un IntegrationMappingSet publicado y activo para Proc_Transacciones.");
        var mappingSet = publishedMappingSets[0];

        var parameters = await _context.IntegrationMethodParameters
            .AsNoTracking()
            .Where(x => x.MethodId == method.Id && x.IsActive && x.Direction == IntegrationParameterDirectionEnum.Input)
            .ToListAsync(ct);

        var rules = await _context.IntegrationMappingRules
            .AsNoTracking()
            .Where(x => x.MappingSetId == mappingSet.Id && x.Enabled)
            .OrderBy(x => x.Priority)
            .ThenBy(x => x.Id)
            .ToListAsync(ct);

        if (rules.Count == 0)
        {
            throw new InvalidOperationException($"El mapping set {mappingSet.Id} publicado no tiene reglas habilitadas.");
        }

        var nachaSource = await LoadNachaSourceContextAsync(classification, ingestion, ct);
        var requiresFunctionalSource = rules.Any(x =>
            x.SourceFieldPath.StartsWith("destinationInstitution.", StringComparison.OrdinalIgnoreCase)
            || x.SourceFieldPath.StartsWith("sourceInstitution.", StringComparison.OrdinalIgnoreCase)
            || x.SourceFieldPath.StartsWith("procTransacciones.", StringComparison.OrdinalIgnoreCase));
        var functionalSource = requiresFunctionalSource
            ? await LoadFunctionalSourceContextAsync(transaction, cycle, nachaSource, ct)
            : FunctionalSourceContext.Empty;
        var resolved = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var sourceValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in parameters)
        {
            var rule = rules.FirstOrDefault(x => x.ParameterId == parameter.Id);
            if (rule is null)
            {
                continue;
            }

            var value = ResolveValue(rule, queueItem, ingestion, classification, transaction, cycle, executionDateTime, nachaSource, functionalSource);
            if (!parameter.Required && IsPlaceholder(value))
            {
                continue;
            }
            if (string.IsNullOrWhiteSpace(value) && parameter.Required)
            {
                throw new InvalidOperationException($"El parámetro requerido {parameter.ParameterPath} no pudo resolverse.");
            }

            resolved[parameter.ParameterPath] = value ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(rule.SourceFieldPath))
            {
                sourceValues[rule.SourceFieldPath] = value ?? string.Empty;
            }
        }

        var hasHomologatedIdLoteSource = rules.Any(x =>
            parameters.Any(p => p.Id == x.ParameterId
                && string.Equals(p.ParameterPath, "IDLOTE", StringComparison.OrdinalIgnoreCase))
            && string.Equals(x.SourceFieldPath, "procTransacciones.functionalBatchId", StringComparison.OrdinalIgnoreCase));
        resolved.TryGetValue("IDLOTE", out var idLote);
        if (hasHomologatedIdLoteSource
            && idLote is not null
            && (!resolved.TryGetValue("IDTRAN", out var idTran)
                || !idTran.All(char.IsDigit)
                || idTran.Length != 7))
        {
            throw new InvalidOperationException("IDTRAN debe contener exactamente siete digitos derivados de la entrada NACHA-M.");
        }

        if (hasHomologatedIdLoteSource
            && idLote is not null
            && (!idLote.All(char.IsDigit) || idLote.Length != 6))
        {
            throw new InvalidOperationException("IDLOTE debe contener exactamente seis digitos y una fuente funcional homologada.");
        }

        var snapshot = await _snapshotBuilder.BuildAsync(mappingSet.Id, ct);

        return new ProcTransaccionesRequestResolution(
            Contract: new ProcTransaccionesRequestContract(resolved, sourceValues),
            MappingSetId: mappingSet.Id,
            MappingVersion: mappingSet.Version,
            MappingSnapshotHash: snapshot.SnapshotHash);
    }

    public string BuildSoapBody(ProcTransaccionesRequestContract request)
    {
        var operation = new XElement(ActionNamespace + "Proc_Transacciones",
            request.Parameters
                .Where(x => !string.IsNullOrWhiteSpace(x.Value)
                    && !string.Equals(x.Key, "METODO", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(x.Key, "ILR", StringComparison.OrdinalIgnoreCase))
                .Select(x => new XElement(ActionNamespace + x.Key, x.Value)));
        var envelope = new XDocument(
            new XElement(XName.Get("Envelope", "http://schemas.xmlsoap.org/soap/envelope/"),
                new XAttribute(XNamespace.Xmlns + "soapenv", "http://schemas.xmlsoap.org/soap/envelope/"),
                new XAttribute(XNamespace.Xmlns + "tem", ActionNamespace.NamespaceName),
                new XElement(XName.Get("Body", "http://schemas.xmlsoap.org/soap/envelope/"), operation)));
        return envelope.ToString(SaveOptions.DisableFormatting);
    }

    private static string? ResolveValue(
        IntegrationMappingRule rule,
        IncomingNachaDispatchQueue queue,
        IncomingNachaFileIngestion ingestion,
        IncomingNachaEntryClassification classification,
        AchTransaction transaction,
        AchCycle cycle,
        DateTime executionDateTime,
        NachaSourceContext nachaSource,
        FunctionalSourceContext functionalSource)
    {
        if (!string.IsNullOrWhiteSpace(rule.FixedValue))
        {
            return rule.FixedValue.Trim();
        }

        var source = (rule.SourceFieldPath ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(source))
        {
            return rule.DefaultValue;
        }

        return source switch
        {
            "transaction.id" => transaction.Id.ToString(CultureInfo.InvariantCulture),
            "transaction.amount" => transaction.Amount.ToString(CultureInfo.InvariantCulture),
            "transaction.transactioncode" => transaction.TransactionCode,
            "transaction.tracenumber" or "transaction.trace" => transaction.TraceNumber,
            "transaction.tracesequencenumber" => transaction.TraceSequenceNumber.ToString("D7", CultureInfo.InvariantCulture),
            "transaction.transactionexternalid" or "transaction.externalid" => transaction.TransactionExternalId,
            "transaction.reference" => transaction.Reference,
            "transaction.companyidentification" => transaction.CompanyIdentification,
            "transaction.sourceaccountnumber" => transaction.SourceAccountNumber,
            "transaction.destinationaccountnumber" => transaction.DestinationAccountNumber,
            "transaction.effectiveentrydate" => transaction.EffectiveEntryDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            "destinationinstitution.transitcodenormalized" => functionalSource.DestinationTransitCode,
            "sourceinstitution.transitcodenormalized" => functionalSource.SourceTransitCode,
            "destinationinstitution.name" => functionalSource.DestinationInstitutionName,
            "sourceinstitution.name" => functionalSource.SourceInstitutionName,
            "proctransacciones.paymentinformation" => functionalSource.PaymentInformation,
            "proctransacciones.functionalbatchid" => functionalSource.FunctionalBatchId,
            "batch.id" => transaction.AchBatchId.ToString(CultureInfo.InvariantCulture),
            "cycle.id" => cycle.Id,
            "cycle.processingdate" => cycle.ProcessingDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            "cycle.clearinghouseid" => string.IsNullOrEmpty(functionalSource.ClearingHouseId)
                ? cycle.ClearingHouseId.ToString(CultureInfo.InvariantCulture)
                : functionalSource.ClearingHouseId,
            "ingestion.id" => ingestion.Id.ToString("N"),
            "classification.class" => ((int)classification.FunctionalClass).ToString(CultureInfo.InvariantCulture),
            "queue.idempotencykey" => queue.IdempotencyDispatchKey,
            "execution.datetimeutc" => executionDateTime.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            "execution.dateyyyymmdd" => executionDateTime.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            "nachaheaders.nachaid" => nachaSource.Header?.NachaID,
            "nachaheaders.immediateorigin" => nachaSource.Header?.ImmediateOrigin,
            "nachaheaders.immediatedestination" => nachaSource.Header?.ImmediateDestination,
            "nachaheaders.fileidmodifier" => nachaSource.Header?.FileIdModifier,
            "nachaheaders.referencecode" => nachaSource.Header?.ReferenceCode,
            "batchheaders.companyid" => nachaSource.BatchHeader?.CompanyId,
            "batchheaders.companyname" => nachaSource.BatchHeader?.CompanyName,
            "batchheaders.standardentryclasscode" => nachaSource.BatchHeader?.StandardEntryClassCode,
            "batchheaders.companyentrydescription" => nachaSource.BatchHeader?.CompanyEntryDescription,
            "batchheaders.effectiveentrydate" => nachaSource.BatchHeader?.EffectiveEntryDate,
            "batchheaders.originparticipantentitycode" => nachaSource.BatchHeader?.OriginParticipantEntityCode,
            "batchheaders.batchnumber" => nachaSource.BatchHeader?.BatchNumber.ToString(CultureInfo.InvariantCulture),
            "entrydetails.transactioncode" => nachaSource.EntryDetail?.TransactionCode,
            "entrydetails.receivingparticipantentitycode" => nachaSource.EntryDetail?.ReceivingParticipantEntityCode,
            "entrydetails.accountnumber" => nachaSource.EntryDetail?.AccountNumber,
            "entrydetails.amount" => nachaSource.EntryDetail?.Amount?.ToString(CultureInfo.InvariantCulture),
            "entrydetails.recipidnumber" => nachaSource.EntryDetail?.RecipIdNumber,
            "entrydetails.recipusername" => nachaSource.EntryDetail?.RecipUserName,
            "entrydetails.sequencenumber" => nachaSource.EntryDetail?.SequenceNumber,
            "addendarecords.infofromoriginator" => FirstNonEmpty(
                nachaSource.AddendaRecord?.InfofromOriginator,
                nachaSource.AddendaRecord?.CollectorId),
            "addendarecords.invoiceoraccountnumber" => nachaSource.AddendaRecord?.InvoiceOrAccountNumber,
            "addendarecords.returnreasoncode" => nachaSource.AddendaRecord?.ReturnReasonCode,
            "addendarecords.originaltracenumber" => nachaSource.AddendaRecord?.OriginalTraceNumber,
            "batchcontrols.entryaddendacount" => nachaSource.BatchControl?.EntryAddendaCount?.ToString(CultureInfo.InvariantCulture),
            "batchcontrols.entryhash" => nachaSource.BatchControl?.EntryHash?.ToString(CultureInfo.InvariantCulture),
            "batchcontrols.totaldebitamount" => nachaSource.BatchControl?.TotalDebitAmount.ToString(CultureInfo.InvariantCulture),
            "batchcontrols.totalcreditamount" => nachaSource.BatchControl?.TotalCreditAmount.ToString(CultureInfo.InvariantCulture),
            "filecontrols.batchcount" => nachaSource.FileControl?.BatchCount.ToString(CultureInfo.InvariantCulture),
            "filecontrols.blockcount" => nachaSource.FileControl?.BlockCount.ToString(CultureInfo.InvariantCulture),
            "filecontrols.entryaddendacount" => nachaSource.FileControl?.EntryAddendaCount.ToString(CultureInfo.InvariantCulture),
            "filecontrols.entryhash" => nachaSource.FileControl?.EntryHash.ToString(CultureInfo.InvariantCulture),
            "filecontrols.totaldebitamount" => nachaSource.FileControl?.TotalDebitAmount.ToString(CultureInfo.InvariantCulture),
            "filecontrols.totalcreditamount" => nachaSource.FileControl?.TotalCreditAmount.ToString(CultureInfo.InvariantCulture),
            _ => rule.DefaultValue
        };
    }

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))?.Trim();

    private static bool IsPlaceholder(string? value)
        => string.Equals(value?.Trim(), "SEED", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value?.Trim(), "PLACEHOLDER", StringComparison.OrdinalIgnoreCase)
            || string.Equals(value?.Trim(), "TODO", StringComparison.OrdinalIgnoreCase);

    private async Task<NachaSourceContext> LoadNachaSourceContextAsync(
        IncomingNachaEntryClassification classification,
        IncomingNachaFileIngestion ingestion,
        CancellationToken ct)
    {
        var entryDetail = await _context.EntryDetails
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.EntryDetailID == classification.EntryDetailId, ct);

        var nachaId = entryDetail?.NachaID;
        var header = !string.IsNullOrWhiteSpace(nachaId)
            ? await _context.NachaHeaders.AsNoTracking().FirstOrDefaultAsync(x => x.NachaID == nachaId, ct)
            : await _context.NachaHeaders.AsNoTracking().FirstOrDefaultAsync(x => x.IncomingNachaFileIngestionId == ingestion.Id, ct);

        nachaId ??= header?.NachaID;

        var batchHeader = string.IsNullOrWhiteSpace(nachaId) || entryDetail is null
            ? null
            : await _context.BatchHeaders.AsNoTracking().FirstOrDefaultAsync(
                x => x.NachaID == nachaId && x.BatchNumber == entryDetail.BatchNumber,
                ct);

        var addendaRecord = classification.AddendaRecordId.HasValue
            ? await _context.AddendaRecords.AsNoTracking().FirstOrDefaultAsync(x => x.AddendaID == classification.AddendaRecordId.Value, ct)
            : null;

        if (addendaRecord is null && !string.IsNullOrWhiteSpace(nachaId))
        {
            addendaRecord = await _context.AddendaRecords
                .AsNoTracking()
                .OrderBy(x => x.AddendaID)
                .FirstOrDefaultAsync(x => x.NachaID == nachaId
                    && (entryDetail == null
                        || x.EntryDetailSequenceNumber == entryDetail.SequenceNumber
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

        var fileControl = string.IsNullOrWhiteSpace(nachaId)
            ? null
            : await _context.FileControls.AsNoTracking().OrderBy(x => x.FileControlID).FirstOrDefaultAsync(x => x.NachaID == nachaId, ct);

        return new NachaSourceContext(header, batchHeader, entryDetail, addendaRecord, batchControl, fileControl);
    }

    private async Task<FunctionalSourceContext> LoadFunctionalSourceContextAsync(
        AchTransaction transaction,
        AchCycle cycle,
        NachaSourceContext nachaSource,
        CancellationToken ct)
    {
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
            .Where(x => x.Status == Cfa.ACHInterbank.Domain.Entities.Transactions.Enums.FinancialInstitutionStatus.Active)
            .ToListAsync(ct);
        var sourceFromNacha = ResolveInstitutionByParticipantCode(configuredInstitutions, originatorCode, "BCOORIG");
        var destinationFromNacha = ResolveInstitutionByParticipantCode(configuredInstitutions, receiverCode, "BCORECEP");
        if (string.Equals(clearingHouse.Code, "ACHCOL", StringComparison.OrdinalIgnoreCase))
        {
            var immediateOrigin = (nachaSource.Header?.ImmediateOrigin ?? string.Empty).Trim();
            var expectedImmediateOrigin = $"{originatorCode}{DigitoChequeoHelper.CalcularDigitoChequeo(originatorCode)}";
            if (!string.Equals(immediateOrigin, expectedImmediateOrigin, StringComparison.Ordinal))
                throw new InvalidOperationException("PROC_TRANSACCIONES_ACHCOL_IMMEDIATE_ORIGIN_CHECK_DIGIT_MISMATCH: tipo 1 no coincide con tipo 5 y su dígito de chequeo.");
        }
        if (!destinationFromNacha.IsDefaultSource)
            throw new InvalidOperationException("PROC_TRANSACCIONES_DESTINATION_NOT_CFA: la entidad tipo 6 receptora no es CFA.");
        if (transaction.SourceInstitutionId != sourceFromNacha.Id || transaction.DestinationInstitutionId != destinationFromNacha.Id)
            throw new InvalidOperationException("PROC_TRANSACCIONES_TRANSACTION_INSTITUTION_MISMATCH: la transacción no coincide con las instituciones derivadas de NACHA-M.");

        var addenda = nachaSource.AddendaRecord;
        if (addenda is null || !string.Equals(addenda.CodeTypeAddendumRecord, "05", StringComparison.Ordinal))
        {
            throw new InvalidOperationException("PROC_TRANSACCIONES_ADDENDA_05_MISSING: el crédito entrante requiere addenda tipo 05.");
        }

        var paymentInformation = ProcTransaccionesPaymentInformationBuilder.Build(
            transaction.CompanyIdentification,
            batch.CompanyEntryDescription ?? string.Empty,
            addenda.PaymentRelatedInformation ?? string.Empty);

        return new FunctionalSourceContext(
            sourceCode,
            destinationCode,
            sourceFromNacha.Name,
            destinationFromNacha.Name,
            ToFunctionalBatchId(batch.BatchNumber.ToString("D7", CultureInfo.InvariantCulture)),
            clearingHouse.Id.ToString(CultureInfo.InvariantCulture),
            paymentInformation);
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

    private static void ValidateNachaOriginatorEvidence(
        NachaHeader? header,
        BatchHeader batch,
        EntryDetail entry,
        BatchControl? control,
        string clearingHouseCode)
    {
        var originator = NormalizeParticipantCode(batch.OriginParticipantEntityCode, "BCOORIG");
        if (originator.All(c => c == '0'))
            throw new InvalidOperationException("PROC_TRANSACCIONES_ORIGINATOR_ZERO: el código originador NACHA-M no puede ser ceros.");
        var trace = (entry.SequenceNumber ?? string.Empty).Trim();
        if (trace.Length != 15 || trace.Any(c => !char.IsDigit(c)) || !string.Equals(trace[..8], originator, StringComparison.Ordinal))
            throw new InvalidOperationException("PROC_TRANSACCIONES_ORIGINATOR_TRACE_MISMATCH: tipo 5 no coincide con tipo 6.");
        if (control is null || !string.Equals((control.IdOrigEntity ?? string.Empty).Trim(), originator, StringComparison.Ordinal))
            throw new InvalidOperationException("PROC_TRANSACCIONES_ORIGINATOR_CONTROL_MISMATCH: tipo 5 no coincide con tipo 8.");
        if (string.Equals(clearingHouseCode, "ACHCOL", StringComparison.OrdinalIgnoreCase))
        {
            var immediateOrigin = (header?.ImmediateOrigin ?? string.Empty).Trim();
            if (immediateOrigin.Length != 9 || immediateOrigin.Any(c => !char.IsDigit(c)) || !string.Equals(immediateOrigin[..8], originator, StringComparison.Ordinal))
                throw new InvalidOperationException("PROC_TRANSACCIONES_ACHCOL_IMMEDIATE_ORIGIN_MISMATCH: tipo 1 no coincide con tipo 5.");
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
            throw new InvalidOperationException($"PROC_TRANSACCIONES_ORIGINATOR_INVALID: {parameter} requiere ocho dígitos NACHA-M.");
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
        var expected = clearingHouse.Code.ToUpperInvariant() switch
        {
            "ACHCOL" => 1,
            "CENIT" => 2,
            _ => throw new InvalidOperationException("PROC_TRANSACCIONES_CLEARING_HOUSE_UNSUPPORTED: la cámara no es ACHCOL ni CENIT.")
        };
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
        BatchControl? BatchControl,
        FileControl? FileControl);

    private sealed record FunctionalSourceContext(
        string SourceTransitCode,
        string DestinationTransitCode,
        string SourceInstitutionName,
        string DestinationInstitutionName,
        string FunctionalBatchId,
        string ClearingHouseId,
        string PaymentInformation)
    {
        public static FunctionalSourceContext Empty { get; } = new("", "", "", "", "", "", "");
    }
}
