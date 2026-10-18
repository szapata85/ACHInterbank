using System.Globalization;
using System.Xml.Linq;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Integrations.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class ProcTransaccionesRequestMapper : IProcTransaccionesRequestMapper
{
    private const string MethodCode = "WSCFAACH.Proc_Transacciones";
    private static readonly XNamespace ActionNamespace = "http://tempuri.org/";
    private readonly AchDbContext _context;
    private readonly IntegrationMappingSnapshotBuilder _snapshotBuilder;
    private readonly ProcTransaccionesDispatchOptions _options;
    private readonly IIntegrationRequestResolver? _requestResolver;
    private readonly ISoapRequestBodyWriter? _soapBodyWriter;
    private readonly IProcTransaccionesFunctionalSourceResolver _functionalSourceResolver;
    private readonly ProcTransaccionesRequestParityPolicy _parityPolicy;

    public ProcTransaccionesRequestMapper(
        AchDbContext context,
        IntegrationMappingSnapshotBuilder? snapshotBuilder = null,
        IOptions<ProcTransaccionesDispatchOptions>? options = null,
        IIntegrationRequestResolver? requestResolver = null,
        ISoapRequestBodyWriter? soapBodyWriter = null,
        IProcTransaccionesFunctionalSourceResolver? functionalSourceResolver = null,
        ProcTransaccionesRequestParityPolicy? parityPolicy = null)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _snapshotBuilder = snapshotBuilder ?? new IntegrationMappingSnapshotBuilder(context);
        _options = options?.Value ?? new ProcTransaccionesDispatchOptions();
        _requestResolver = requestResolver;
        _soapBodyWriter = soapBodyWriter;
        _functionalSourceResolver = functionalSourceResolver ?? new ProcTransaccionesFunctionalSourceResolver(context);
        _parityPolicy = parityPolicy ?? new ProcTransaccionesRequestParityPolicy();
    }

    public Task<ProcTransaccionesRequestResolution> ResolveAsync(
        IncomingNachaDispatchQueue queueItem,
        IncomingNachaFileIngestion ingestion,
        IncomingNachaEntryClassification classification,
        AchTransaction transaction,
        AchCycle cycle,
        DateTime executionDateTime,
        CancellationToken ct = default)
        => _options.UseContractDrivenRequest
            ? ResolveContractDrivenAsync(queueItem, ingestion, classification, transaction, cycle, executionDateTime, ct)
            : ResolveLegacyAsync(queueItem, ingestion, classification, transaction, cycle, executionDateTime, ct);

    private async Task<ProcTransaccionesRequestResolution> ResolveContractDrivenAsync(
        IncomingNachaDispatchQueue queueItem,
        IncomingNachaFileIngestion ingestion,
        IncomingNachaEntryClassification classification,
        AchTransaction transaction,
        AchCycle cycle,
        DateTime executionDateTime,
        CancellationToken ct)
    {
        if (_requestResolver is null)
        {
            throw new InvalidOperationException(
                "PROC_TRANSACCIONES_CONTRACT_ENGINE_NOT_CONFIGURED: IIntegrationRequestResolver no está registrado.");
        }

        if (_soapBodyWriter is null)
        {
            throw new InvalidOperationException(
                "PROC_TRANSACCIONES_CONTRACT_ENGINE_NOT_CONFIGURED: ISoapRequestBodyWriter no está registrado.");
        }

        var executionDateUtc = executionDateTime.Kind == DateTimeKind.Utc
            ? executionDateTime
            : executionDateTime.ToUniversalTime();
        var correlationId = string.IsNullOrWhiteSpace(ingestion.CorrelationId)
            ? queueItem.Id.ToString("N", CultureInfo.InvariantCulture)
            : ingestion.CorrelationId.Trim();
        var executionContext = new IntegrationExecutionContext(
            MethodCode,
            correlationId,
            executionDateUtc,
            transaction.Id,
            cycle.Id,
            queueItem.Id);

        // Preserve the legacy functional guards before resolving the contract-driven request.
        // The scoped resolver is also consumed by Proc_Transacciones source strategies, so this
        // validation result is reused without introducing a service-specific switch in the engine.
        _ = await _functionalSourceResolver.ResolveAsync(
            queueItem, ingestion, classification, transaction, cycle, ct);

        var genericRequest = await _requestResolver.ResolveAsync(executionContext, ct);
        var parity = _parityPolicy.Apply(genericRequest);
        ValidateHomologatedIds(parity.ContractDrivenRequest.Contract, parity.Parameters);
        var snapshot = await _snapshotBuilder.BuildAsync(parity.ContractDrivenRequest.MappingSetId, ct);

        return new ProcTransaccionesRequestResolution(
            Contract: new ProcTransaccionesRequestContract(
                parity.Parameters,
                parity.SourceValues,
                parity.ContractDrivenRequest),
            MappingSetId: parity.ContractDrivenRequest.MappingSetId,
            MappingVersion: parity.ContractDrivenRequest.MappingVersion,
            MappingSnapshotHash: snapshot.SnapshotHash);
    }

    private async Task<ProcTransaccionesRequestResolution> ResolveLegacyAsync(
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
            .FirstOrDefaultAsync(x => x.Code == MethodCode && x.IsActive, ct)
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

        var nachaSource = await LoadNachaSourceContextAsync(queueItem, classification, ingestion, ct);
        var requiresFunctionalSource = rules.Any(x =>
            x.SourceFieldPath.StartsWith("destinationInstitution.", StringComparison.OrdinalIgnoreCase)
            || x.SourceFieldPath.StartsWith("sourceInstitution.", StringComparison.OrdinalIgnoreCase)
            || x.SourceFieldPath.StartsWith("procTransacciones.", StringComparison.OrdinalIgnoreCase));
        var functionalSource = requiresFunctionalSource
            ? await _functionalSourceResolver.ResolveAsync(queueItem, ingestion, classification, transaction, cycle, ct)
            : ProcTransaccionesFunctionalSourceContext.Empty;
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
        ArgumentNullException.ThrowIfNull(request);
        if (_options.UseContractDrivenRequest)
        {
            var resolved = request.ContractDrivenRequest
                ?? throw new InvalidOperationException(
                    "PROC_TRANSACCIONES_CONTRACT_REQUEST_MISSING: la solicitud no contiene el snapshot resuelto del motor genérico.");
            return (_soapBodyWriter
                ?? throw new InvalidOperationException(
                    "PROC_TRANSACCIONES_CONTRACT_ENGINE_NOT_CONFIGURED: ISoapRequestBodyWriter no está registrado."))
                .Write(resolved);
        }

        return BuildLegacySoapBody(request);
    }

    private static string BuildLegacySoapBody(ProcTransaccionesRequestContract request)
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
        ProcTransaccionesFunctionalSourceContext functionalSource)
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

        var fileControl = string.IsNullOrWhiteSpace(nachaId)
            ? null
            : await _context.FileControls.AsNoTracking().OrderBy(x => x.FileControlID).FirstOrDefaultAsync(x => x.NachaID == nachaId, ct);

        return new NachaSourceContext(header, batchHeader, entryDetail, addendaRecord, batchControl, fileControl);
    }

    public static string ToFunctionalBatchId(string rawBatchNumber)
        => ProcTransaccionesFunctionalSourceResolver.ToFunctionalBatchId(rawBatchNumber);

    private static void ValidateHomologatedIds(
        IntegrationExecutionContract contract,
        IReadOnlyDictionary<string, string> resolved)
    {
        var hasHomologatedIdLoteSource = contract.Parameters.Any(binding =>
            binding.IsMapped
            && string.Equals(binding.ParameterPath, "IDLOTE", StringComparison.OrdinalIgnoreCase)
            && string.Equals(binding.Source.SourceKey, "procTransacciones.functionalBatchId", StringComparison.OrdinalIgnoreCase));
        if (!hasHomologatedIdLoteSource || !resolved.TryGetValue("IDLOTE", out var idLote))
        {
            return;
        }

        if (!resolved.TryGetValue("IDTRAN", out var idTran)
            || !idTran.All(char.IsDigit)
            || idTran.Length != 7)
        {
            throw new InvalidOperationException("IDTRAN debe contener exactamente siete digitos derivados de la entrada NACHA-M.");
        }

        if (!idLote.All(char.IsDigit) || idLote.Length != 6)
        {
            throw new InvalidOperationException("IDLOTE debe contener exactamente seis digitos y una fuente funcional homologada.");
        }
    }

    private sealed record NachaSourceContext(
        NachaHeader? Header,
        BatchHeader? BatchHeader,
        EntryDetail? EntryDetail,
        AddendaRecord? AddendaRecord,
        BatchControl? BatchControl,
        FileControl? FileControl);

}
