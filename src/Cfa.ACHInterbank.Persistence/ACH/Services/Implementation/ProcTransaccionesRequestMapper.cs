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
    {
        ArgumentNullException.ThrowIfNull(queueItem);
        // The legacy contract requires real AchTransaction/AchCycle entities. Never use
        // fabricated ones to process a file-based ACH Colombia entry. F5.2 will
        // introduce the entry-scoped mapping only after the field sources are resolved.
        if (string.Equals(queueItem.SourceKind, IncomingNachaDispatchSourceKinds.AchColFileBased, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "PROC_TRANSACCIONES_ACHCOL_MAPPING_NOT_HOMOLOGATED_F51: la fuente NACHA debe mapearse sin AchTransaction/AchCycle ficticios.");
        }

        return _options.UseContractDrivenRequest
            ? ResolveContractDrivenAsync(queueItem, ingestion, classification, transaction, cycle, executionDateTime, ct)
            : ResolveLegacyAsync(queueItem, ingestion, classification, transaction, cycle, executionDateTime, ct);
    }

    /// <summary>
    /// F5.2: entry-scoped diagnostic projection. It deliberately returns a different
    /// type than ResolveAsync to prevent a partial/unhomologated contract from entering
    /// the SOAP transport. Only a persisted F4.2-approved, still-blocked row qualifies.
    /// This method is read-only and performs NO client/queue writes, even in Live mode.
    /// </summary>
    public async Task<AchColProcTransaccionesDryRunPreview> PreviewAchColFileBasedAsync(
        IncomingNachaDispatchQueue queueItem,
        IncomingNachaFileIngestion ingestion,
        IncomingNachaEntryClassification classification,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(queueItem);
        ArgumentNullException.ThrowIfNull(ingestion);
        ArgumentNullException.ThrowIfNull(classification);
        ct.ThrowIfCancellationRequested();

        const string blocked = "ACHCOL_SOAP_CONTRACT_NOT_HOMOLOGATED_F52";
        if (queueItem.SourceKind != IncomingNachaDispatchSourceKinds.AchColFileBased
            || queueItem.AchCycleId is not null
            || queueItem.AchTransactionId is not null
            || queueItem.EntryDetailId is not int entryId
            || queueItem.QueueStatus != IncomingNachaDispatchQueueStatus.Blocked
            || queueItem.LastErrorCode != IncomingNachaFileBasedPreparationStage.MappingPendingCode
            || queueItem.AttemptCount != 0 || queueItem.NextAttemptAtUtc is not null
            || queueItem.IncomingNachaFileIngestionId != ingestion.Id
            || queueItem.IncomingNachaEntryClassificationId != classification.Id
            || classification.IncomingNachaFileIngestionId != ingestion.Id
            || classification.EntryDetailId != entryId
            || classification.EligibilityStatus != IncomingNachaEligibilityStatus.Elegible
            || classification.RequiresManualResolution
            || classification.FunctionalClass is not (IncomingNachaFunctionalClass.CreditoEntrante or IncomingNachaFunctionalClass.DebitoEntrante)
            || ingestion.IngestionStatus != IncomingNachaIngestionStatus.Completado
            || ingestion.ResolutionMode != IncomingNachaDispatchSourceKinds.AchColFileBased
            || ingestion.ResolvedAchCycleId is not null
            || ingestion.ResolvedClearingHouseId != queueItem.ClearingHouseId
            || ingestion.OperationalDate?.Date != queueItem.OperationalDate.Date)
        {
            throw new InvalidOperationException("ACHCOL_F52_DRYRUN_CONTEXT_INVALID: cola, ingestion o clasificacion no corresponden.");
        }

        if (!ExternalFileNames.AchColInboundFileNameParser.TryParse(ingestion.FileName, out var file, out _)
            || file is null
            || !string.Equals(ingestion.CanonicalPlainFileName, file.CanonicalPlainFileName, StringComparison.OrdinalIgnoreCase)
            || ingestion.FileSequence != file.FileSequence
            || ingestion.ExternalCycleNumber != file.ExternalCycleNumber
            || ingestion.FileIdModifier != file.FileIdModifier.ToString()
            || ingestion.FileProtectionKind != file.ProtectionKind.ToString()
            || ingestion.FileNameDate?.Date != queueItem.OperationalDate.Date
            || file.FileDate != DateOnly.FromDateTime(queueItem.OperationalDate))
        {
            throw new InvalidOperationException("ACHCOL_F52_DRYRUN_FILE_INVALID: contexto fisico NACHA no consistente.");
        }

        var chamber = await _context.ClearingHouses.AsNoTracking()
            .SingleOrDefaultAsync(x => x.ClearingHouseId == queueItem.ClearingHouseId, ct);
        if (chamber is null || !chamber.IsActive
            || !string.Equals(chamber.Code, "ACHCOL", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("ACHCOL_F52_DRYRUN_CHAMBER_INVALID: origen distinto de ACH Colombia.");

        var entry = await _context.EntryDetails.AsNoTracking()
            .SingleOrDefaultAsync(x => x.EntryDetailID == entryId, ct)
            ?? throw new InvalidOperationException("ACHCOL_F52_DRYRUN_ENTRY_MISSING: entrada no persistida.");
        var header = await _context.NachaHeaders.AsNoTracking()
            .SingleOrDefaultAsync(x => x.NachaID == entry.NachaID && x.IncomingNachaFileIngestionId == ingestion.Id, ct)
            ?? throw new InvalidOperationException("ACHCOL_F52_DRYRUN_HEADER_MISSING: encabezado no vinculado a la ingestion.");
        if (header.FileIdModifier != ingestion.FileIdModifier
            || !IncomingNachaFileBasedDispatchIdentity.TryBuild(
                queueItem.ClearingHouseId, queueItem.OperationalDate, header, entry, out var identity)
            || !string.Equals(identity, queueItem.IdempotencyDispatchKey, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("ACHCOL_F52_DRYRUN_IDENTITY_MISMATCH: identidad de negocio no coincide.");
        }

        var code = entry.TransactionCode?.Trim();
        var monetary = classification.FunctionalClass switch
        {
            IncomingNachaFunctionalClass.CreditoEntrante => code is "22" or "32" or "52",
            IncomingNachaFunctionalClass.DebitoEntrante => code is "27" or "37" or "55",
            _ => false
        };
        if (!monetary || entry.Amount is null or <= 0m
            || classification.PrenoteStatus != IncomingNachaPrenoteStatus.NoAplica
            || !string.IsNullOrWhiteSpace(classification.ReturnReasonCode)
            || !string.IsNullOrWhiteSpace(classification.OriginalTraceRef))
        {
            throw new InvalidOperationException("ACHCOL_F52_DRYRUN_NOT_MONETARY: tipo 6 no elegible para CFA.");
        }

        var batches = await _context.BatchHeaders.AsNoTracking()
            .Where(x => x.NachaID == entry.NachaID && x.BatchNumber == entry.BatchNumber)
            .Take(2).ToListAsync(ct);
        if (batches.Count != 1)
            throw new InvalidOperationException("ACHCOL_F52_DRYRUN_BATCH_INVALID: lote faltante o ambiguo.");

        // Respect published metadata: never silently reuse a CycleBound mapping to
        // assign cycle.id, transaction.id, or uncertain institution identifiers.
        var method = await _context.IntegrationMethods.AsNoTracking()
            .SingleOrDefaultAsync(x => x.Code == MethodCode && x.IsActive, ct)
            ?? throw new InvalidOperationException("ACHCOL_F52_DRYRUN_METHOD_MISSING: metodo CFA no activo.");
        var mappingSets = await _context.IntegrationMappingSets.AsNoTracking()
            .Where(x => x.MethodId == method.Id
                && x.Status == IntegrationMappingSetStatusEnum.Published && x.IsActive)
            .Take(2).ToListAsync(ct);
        if (mappingSets.Count != 1)
            throw new InvalidOperationException("ACHCOL_F52_DRYRUN_MAPPING_INVALID: se requiere un unico mapping publicado.");
        var parameters = await _context.IntegrationMethodParameters.AsNoTracking()
            .Where(x => x.MethodId == method.Id && x.IsActive
                && x.Direction == IntegrationParameterDirectionEnum.Input)
            .ToListAsync(ct);
        if (parameters.Count == 0 || parameters.Any(x => !AchColProcTransaccionesFieldSources.Fields.ContainsKey(x.ParameterPath)))
            throw new InvalidOperationException("ACHCOL_F52_DRYRUN_CATALOG_INVALID: parametros desconocidos o ausentes.");

        // Observed NACHA values only, without imposing unapproved formats or
        // including conditional identifiers (IDTRAN, IDLOTE, IDCAMCOMPE, etc.).
        var observed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["TIPTRAN"] = code!,
            ["MONTO"] = entry.Amount.Value.ToString("G29", CultureInfo.InvariantCulture)
        };
        AddNonEmpty(observed, "NCTARECEP", entry.AccountNumber);
        AddNonEmpty(observed, "NRECEP", entry.RecipUserName);
        AddNonEmpty(observed, "IDRECEP", entry.RecipIdNumber);
        // Optional DISCRE is intentionally not rendered until its mapping is approved.
        var pending = AchColProcTransaccionesFieldSources.Fields.Values
            .Where(x => x.Status != AchColProcTransaccionesFieldSources.DirectCandidate
                || x.Parameter == "DISCRE" || !observed.ContainsKey(x.Parameter))
            .Select(x => x.Parameter)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
        var missingRequired = parameters.Where(x => x.Required && !observed.ContainsKey(x.ParameterPath))
            .Select(x => x.ParameterPath).OrderBy(x => x, StringComparer.Ordinal).ToArray();

        // SOAP-shaped diagnostic preview, NOT a complete/sendable request. The
        // caller receives no ProcTransaccionesRequestContract or SOAP client handle.
        var previewXml = BuildLegacySoapBody(new ProcTransaccionesRequestContract(observed));
        var preview = new AchColProcTransaccionesDryRunPreview(
            queueItem.Id, entryId, mappingSets[0].Id, mappingSets[0].Version,
            observed, pending, missingRequired, previewXml, blocked);
        // F5.3: validate offline SOAP shape and direct-field allowlist before
        // exposing diagnostic evidence. This is NOT contract homologation and
        // cannot unblock, enqueue or transmit a monetary operation.
        _ = AchColProcTransaccionesDryRunContractCheck.Inspect(preview);
        return preview;
    }

    private static void AddNonEmpty(Dictionary<string, string> values, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            values[key] = value.Trim();
    }

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
