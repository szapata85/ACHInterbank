using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Interfaces.Mapping;
using Cfa.ACHInterbank.Application.ACH.Models.Mapping;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Helpers;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

internal sealed class NachaFileGenerationCoordinator
{
    private const int FixedWidthRecordLength = 106;
    private static readonly INachaOfficialRecordRenderer OfficialRecordRenderer = new NachaOfficialRecordRenderer();

    private readonly AchDbContext _context;
    private readonly INachaDataLoader _dataLoader;
    private readonly INachaTransactionValidationService _transactionValidationService;
    private readonly INachaFixedWidthRecordRenderer _recordRenderer;
    private readonly INachaRecordDataProvider _recordDataProvider;
    private readonly INachaSemanticValidator _nachaSemanticValidator;
    private readonly IBatchNumberGenerator _batchNumberGenerator;
    private readonly INachaConfigResolver? _configResolver;
    private readonly INachaType7AliasMap? _type7AliasMap;
    private readonly INachaType7GenerationStrategy? _type7GenerationStrategy;
    private readonly INachaType7LegacyRenderer? _type7LegacyRenderer;
    private readonly INachaType7RolloutPolicy? _type7RolloutPolicy;
    private readonly INachaRecordMappingEngine? _recordMappingEngine;
    private readonly IFieldMappingPlanCompiler? _mappingPlanCompiler;
    private readonly NachaGenerationOptions _generationOptions;
    private readonly ILogger<NachaFileBuilder>? _logger;
    private readonly INachaControlTotalsCalculator _controlTotalsCalculator;
    private readonly IOperationalTimeSnapshotProvider _operationalTimeProvider;
    private readonly ICycleCalendarGuard _calendarGuard;

    public NachaFileGenerationCoordinator(
        AchDbContext context,
        INachaDataLoader dataLoader,
        INachaTransactionValidationService transactionValidationService,
        INachaFixedWidthRecordRenderer recordRenderer,
        INachaRecordDataProvider recordDataProvider,
        INachaSemanticValidator nachaSemanticValidator,
        INachaConfigResolver? configResolver,
        INachaType7AliasMap? type7AliasMap,
        INachaType7GenerationStrategy? type7GenerationStrategy,
        INachaType7LegacyRenderer? type7LegacyRenderer,
        INachaType7RolloutPolicy? type7RolloutPolicy,
        INachaRecordMappingEngine? recordMappingEngine,
        IFieldMappingPlanCompiler? mappingPlanCompiler,
        NachaGenerationOptions generationOptions,
        ILogger<NachaFileBuilder>? logger,
        IBatchNumberGenerator batchNumberGenerator,
        INachaControlTotalsCalculator controlTotalsCalculator,
        IOperationalTimeSnapshotProvider operationalTimeProvider,
        ICycleCalendarGuard calendarGuard)
    {
        _context = context;
        _dataLoader = dataLoader;
        _transactionValidationService = transactionValidationService;
        _recordRenderer = recordRenderer;
        _recordDataProvider = recordDataProvider;
        _nachaSemanticValidator = nachaSemanticValidator;
        _batchNumberGenerator = batchNumberGenerator;
        _configResolver = configResolver;
        _type7AliasMap = type7AliasMap;
        _type7GenerationStrategy = type7GenerationStrategy;
        _type7LegacyRenderer = type7LegacyRenderer;
        _type7RolloutPolicy = type7RolloutPolicy;
        _recordMappingEngine = recordMappingEngine;
        _mappingPlanCompiler = mappingPlanCompiler;
        _generationOptions = generationOptions;
        _logger = logger;
        _controlTotalsCalculator = controlTotalsCalculator;
        _operationalTimeProvider = operationalTimeProvider;
        _calendarGuard = calendarGuard;
    }

    public async Task<NachaReturnOutBuildResult> BuildReturnOutAsync(
        NachaReturnOutBuildRequest request,
        CancellationToken ct = default)
    {
        if (request.Batches.Count == 0 || request.Batches.Any(batch => batch.Entries.Count == 0))
        {
            throw new NachaGenerationException("NACHA_RETURN_OUT_EMPTY", "ReturnOut requiere al menos un lote y una entrada.");
        }

        if (_configResolver is null)
        {
            throw new NachaGenerationException("NACHA_PROFILE_NOT_PUBLISHED", "Resolver NACHA Opción C no registrado. No se habilita fallback legacy.");
        }

        var recordCodes = new[] { "1", "5", "6", "7", "8", "9" };
        var clearingHouseCode = request.ClearingHouseCode.Trim().ToUpperInvariant();
        var chamberPolicy = NachaChamberFlowPolicyResolver.Resolve(clearingHouseCode);
        var returnOutFlow = chamberPolicy.ResolveReturnOutFlow(request.FlowTypeCode, request.Batches);
        var recordLength = returnOutFlow.RecordLength;
        var blockingFactor = returnOutFlow.BlockingFactor;
        var resolutionRequest = new NachaConfigResolutionRequest
        {
            ClearingHouseCode = clearingHouseCode,
            FlowTypeCode = request.FlowTypeCode,
            DirectionCode = "SALIDA",
            ProcessDateUtc = request.CreatedAtUtc,
            RecordCodes = recordCodes,
            RequireHomologated = false,
            SelectionContext = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["Flow"] = returnOutFlow.SelectionFlow,
                ["NormativeVersion"] = request.NormativeVersion
            }
        };
        var resolution = await _configResolver.ResolveAsync(resolutionRequest, ct);
        if (resolution.SelectionStatus == NachaProfileSelectionStatus.ProfileAmbiguous)
        {
            throw new NachaGenerationException("NACHA_PROFILE_AMBIGUOUS", $"Existe más de un perfil {clearingHouseCode}/{request.FlowTypeCode}/SALIDA aplicable.");
        }

        if (!resolution.Success || resolution.Profile is null)
        {
            throw new NachaGenerationException("NACHA_PROFILE_NOT_PUBLISHED", $"No existe perfil NACHA-M publicado/vigente para {clearingHouseCode}/{request.FlowTypeCode}/SALIDA.");
        }

        if (resolution.UsedFallback)
        {
            throw new NachaGenerationException("NACHA_LEGACY_GENERATION_DISABLED", $"ReturnOut {clearingHouseCode} no permite fallback físico legacy.");
        }

        var normativeVersion = resolution.Profile.Tags
            .FirstOrDefault(tag => string.Equals(tag.TagKey, "NormativeVersion", StringComparison.OrdinalIgnoreCase))?.TagValue;
        if (!string.Equals(normativeVersion, request.NormativeVersion, StringComparison.OrdinalIgnoreCase))
        {
            throw new NachaGenerationException("NACHA_RETURN_OUT_PROFILE_VERSION_REQUIRED", $"El perfil ReturnOut seleccionado no declara NormativeVersion={request.NormativeVersion}.");
        }

        var audit = new NachaGenerationAuditResult
        {
            Mode = "TABLE_DRIVEN",
            ClearingHouseCode = clearingHouseCode,
            ClearingHouseName = request.ClearingHouseName,
            ProfileId = resolution.Profile.Id,
            ProfileCode = resolution.Profile.ProfileCode,
            ProfileVersion = $"{resolution.Profile.VersionMajor}.{resolution.Profile.VersionMinor}",
            ProfileStatus = resolution.Profile.Status?.Code,
            EffectiveDate = request.CreatedAtUtc,
            LegacyFallbackUsed = false,
            Phase = returnOutFlow.AuditPhase,
            CorrelationId = $"RETURN-OUT-{request.CreatedAtUtc:yyyyMMddHHmmss}"
        };
        audit.Trace.AddRange(resolution.Trace);
        audit.Trace.Add($"ReturnOutOptionC:Profile={resolution.Profile.ProfileCode};ClearingHouse={clearingHouseCode};Flow={request.FlowTypeCode};Direction=SALIDA;LegacyFallbackUsed=false");
        audit.NewEngineRecordCodes.AddRange(recordCodes);

        foreach (var recordCode in recordCodes)
        {
            ValidateOfficialLayout(recordCode, RequireOfficialLayout(resolution, recordCode), chamberPolicy, audit);
        }

        var batchTotals = request.Batches.Select(batch => CalculateReturnOutBatchTotals(batch, clearingHouseCode)).ToList();
        var entryAddendaCount = batchTotals.Sum(total => total.EntryAddendaCount);
        var entryHash = batchTotals.Aggregate(0L, (current, total) => (current + total.EntryHash) % 10_000_000_000L);
        var totalDebit = batchTotals.Sum(total => total.TotalDebit);
        var totalCredit = batchTotals.Sum(total => total.TotalCredit);
        var recordsBeforePadding = 2 + request.Batches.Sum(batch => 2 + (batch.Entries.Count * 2));
        var blockLayout = NachaFileBlocking.Calculate(recordsBeforePadding, blockingFactor);
        var blockCount = blockLayout.BlockCount;
        var paddingCount = blockLayout.PaddingRecordCount;

        var sb = new StringBuilder(recordsBeforePadding * recordLength);
        var lineNumber = 1;
        var recordCount = 0;
        recordCount += AppendReturnOutOfficialRecords(sb, "1", [ReturnOutValues(
            ("ImmediateDestination", request.ImmediateDestination),
            ("ImmediateOrigin", request.ImmediateOrigin),
            ("FileCreationDate", request.CreatedAtUtc),
            ("FileCreationTime", request.CreatedAtUtc),
            ("FileIdModifier", request.FileIdModifier),
            ("ImmediateDestinationName", request.ImmediateDestinationName),
            ("ImmediateOriginName", request.ImmediateOriginName),
            ("ReferenceCode", request.ReferenceCode))], RequireOfficialLayout(resolution, "1"), audit, ref lineNumber);

        for (var batchIndex = 0; batchIndex < request.Batches.Count; batchIndex++)
        {
            var batch = request.Batches[batchIndex];
            var totals = batchTotals[batchIndex];
            recordCount += AppendReturnOutOfficialRecords(sb, "5", [ReturnOutValues(
                ("ServiceClassCode", batch.ServiceClassCode), ("CompanyName", batch.CompanyName),
                ("CompanyDiscretionaryData", batch.CompanyDiscretionaryData), ("CompanyIdentification", batch.CompanyIdentification),
                ("StandardEntryClassCode", batch.StandardEntryClassCode), ("CompanyEntryDescription", batch.CompanyEntryDescription),
                ("CompanyDescriptiveDate", batch.CompanyDescriptiveDate), ("EffectiveEntryDate", batch.EffectiveEntryDate),
                ("SettlementDate", batch.SettlementDate), ("OriginatingDfi", batch.OriginatingDfi), ("BatchNumber", batch.BatchNumber))],
                RequireOfficialLayout(resolution, "5"), audit, ref lineNumber);

            foreach (var entry in batch.Entries)
            {
                recordCount += AppendReturnOutOfficialRecords(sb, "6", [ReturnOutValues(
                    ("TransactionCode", entry.TransactionCode), ("ReceivingDfi", entry.ReceivingDfi), ("CheckDigit", entry.CheckDigit),
                    ("DfiAccountNumber", entry.AccountNumber), ("Amount", entry.Amount),
                    ("IndividualIdentification", entry.IndividualIdentification), ("IndividualName", entry.IndividualName),
                    ("DiscretionaryData", entry.DiscretionaryData), ("TraceNumber", entry.NewTraceNumber))],
                    RequireOfficialLayout(resolution, "6"), audit, ref lineNumber);
                recordCount += AppendReturnOutOfficialRecords(sb, "7", [ReturnOutValues(
                    ("ReturnReasonCode", entry.ReturnReasonCode), ("OriginalTraceNumber", entry.OriginalTraceNumber),
                    ("DateOfDeath", entry.DeathDate), ("OriginalReceivingDfi", entry.OriginalReceivingDfi),
                    ("AdditionalInformation", entry.AdditionalInformation), ("AddendaSequenceNumber", entry.AddendaSequenceNumber),
                    ("SourceReturnTraceNumber", entry.SourceReturnTraceNumber),
                    ("SourceReturnSettlementDate", entry.SourceReturnSettlementDate),
                    ("SourceReturnReasonCode", entry.SourceReturnReasonCode))],
                    RequireOfficialLayout(resolution, "7"), audit, ref lineNumber);
            }

            recordCount += AppendReturnOutOfficialRecords(sb, "8", [ReturnOutValues(
                ("ServiceClassCode", batch.ServiceClassCode), ("EntryAddendaCount", totals.EntryAddendaCount),
                ("EntryHash", totals.EntryHash), ("TotalDebitAmount", totals.TotalDebit), ("TotalCreditAmount", totals.TotalCredit),
                ("CompanyIdentification", batch.CompanyIdentification), ("OriginatingDfi", batch.OriginatingDfi), ("BatchNumber", batch.BatchNumber))],
                RequireOfficialLayout(resolution, "8"), audit, ref lineNumber);
        }

        recordCount += AppendReturnOutOfficialRecords(sb, "9", [ReturnOutValues(
            ("BatchCount", request.Batches.Count), ("BlockCount", blockCount), ("EntryAddendaCount", entryAddendaCount),
            ("EntryHash", entryHash), ("TotalDebitAmount", totalDebit), ("TotalCreditAmount", totalCredit))],
            RequireOfficialLayout(resolution, "9"), audit, ref lineNumber);

        var paddingRecord = NachaFileBlocking.BuildPaddingRecord(RequireOfficialLayout(resolution, "9").TotalLength);
        for (var i = 0; i < paddingCount; i++)
        {
            sb.Append(paddingRecord);
            recordCount++;
        }

        var content = sb.ToString();
        if (recordCount != blockCount * blockingFactor
            || content.Length != recordCount * recordLength)
        {
            throw new NachaGenerationException("NACHA_RECORD_COUNT_MISMATCH", "ReturnOut no cumple bloques de 10 registros de 106 caracteres.");
        }

        audit.TotalRecords = recordCount;
        audit.TotalFields = audit.FieldTraceEntries.Count;
        audit.FileHash = Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(content)));
        audit.FileIdModifier = new NachaFileIdModifierAudit { DailySequence = 0, ResolvedValue = request.FileIdModifier };
        audit.FileTotals = new NachaFileControlTotalsAudit
        {
            BatchCount = request.Batches.Count,
            BlockCount = blockCount,
            EntryAddendaCount = entryAddendaCount,
            EntryHash = entryHash,
            TotalDebitAmountInCents = DecimalToCents(totalDebit),
            TotalCreditAmountInCents = DecimalToCents(totalCredit),
            PhysicalRecordCountBeforePadding = recordsBeforePadding,
            PaddingRecordCount = paddingCount,
            PhysicalRecordCountAfterPadding = recordCount
        };
        if (request.PersistAudit)
        {
            await PersistGenerationAuditAsync(audit, resolution.Profile.Id, ct, request.CreatedAtUtc);
        }

        return new NachaReturnOutBuildResult(
            content,
            recordCount,
            resolution.Profile.ProfileCode,
            normativeVersion!,
            false,
            blockCount,
            entryAddendaCount,
            entryHash.ToString("D10", System.Globalization.CultureInfo.InvariantCulture));
    }

    private static IReadOnlyDictionary<string, object?> ReturnOutValues(params (string Key, object? Value)[] values)
        => values.ToDictionary(item => item.Key, item => item.Value, StringComparer.OrdinalIgnoreCase);

    private static ReturnOutBatchTotals CalculateReturnOutBatchTotals(NachaReturnOutBatch batch, string clearingHouseCode)
    {
        var hash = batch.Entries.Aggregate(0L, (current, entry) =>
            (current + long.Parse(entry.ReceivingDfi, CultureInfo.InvariantCulture)) % 10_000_000_000L);
        var debit = batch.Entries.Where(entry => entry.TransactionCode is "26" or "36" or "56").Sum(entry => entry.Amount);
        var credit = batch.Entries.Where(entry => entry.TransactionCode is "21" or "31" or "51").Sum(entry => entry.Amount);
        if (batch.Entries.Any(entry => entry.TransactionCode is not ("21" or "31" or "51" or "26" or "36" or "56")))
        {
            throw new NachaGenerationException("NACHA_ALLOWED_VALUE_INVALID", $"Código de transacción ReturnOut fuera del perfil {clearingHouseCode}.");
        }

        return new ReturnOutBatchTotals(batch.Entries.Count * 2, hash, debit, credit);
    }

    private static long DecimalToCents(decimal amount) => checked((long)decimal.Round(amount * 100m, 0, MidpointRounding.AwayFromZero));

    private sealed record ReturnOutBatchTotals(int EntryAddendaCount, long EntryHash, decimal TotalDebit, decimal TotalCredit);

    // ─────────────────────────────────────────────────────────────────────────────
    // MÉTODO PRINCIPAL: Generar archivo NACHA-M por lotes
    // ─────────────────────────────────────────────────────────────────────────────
    public async Task<string> BuildNachaFileAsync(IEnumerable<int> batchIds, CancellationToken ct = default)
    {
        var batches = await _dataLoader.LoadBatchesByIdsAsync(batchIds, ct);

        if (!batches.Any())
            throw new InvalidOperationException("No se encontraron lotes para exportar.");

        var cycle = batches.First().AchCycle!;
        var nachaHeader = await _dataLoader.LoadHeaderAsync(cycle.Id, ct);

        var transactions = batches.SelectMany(b => b.Transactions).ToList();
        var context = new NachaBuildContext
        {
            Cycle = cycle,
            Batches = batches,
            Transactions = transactions
        };
        var clearingHouseCode = await ResolveClearingHouseCodeAsync(context, ct);
        var chamberPolicy = NachaChamberFlowPolicyResolver.Resolve(clearingHouseCode);
        if (chamberPolicy.RequiresServicePartitioning)
        {
            var catalog = (await _dataLoader.LoadCompanyEntryDescriptionCatalogAsync(ct))
                .Select(item => new CompanyEntryDescriptionCatalogItem(item.Term, item.StandardEntryClassCode))
                .ToList();
            var serviceCodes = batches
                .Select(batch => ResolveStandardEntryClassCode(batch, batch.Transactions.ToArray(), catalog))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (serviceCodes.Length != 1)
            {
                throw new NachaGenerationException(
                    "CENIT_SINGLE_FILE_SERVICE_AMBIGUOUS",
                    "El wrapper de archivo único CENIT requiere una sola clase de servicio; use el resultado multiarchivo para entradas mixtas.");
            }

            context = new NachaBuildContext
            {
                Cycle = cycle,
                Batches = batches,
                Transactions = transactions,
                StandardEntryClassCode = serviceCodes[0]
            };
        }
        var calendarDecision = await _calendarGuard.EnsureExecutableAsync(cycle, ct);
        if (!calendarDecision.CanExecute)
        {
            throw new CycleDeferredByCalendarException(cycle.Id, calendarDecision);
        }
        chamberPolicy.EnforcePreResolutionGenerationGate(
            _generationOptions,
            IsOfficialTableDrivenMode(),
            clearingHouseCode);
        await _transactionValidationService.ValidateTransactionsForSendAsync(transactions, ct);
        if (IsOfficialTableDrivenMode())
        {
            return await BuildOfficialTableDrivenFileAsync(context, nachaHeader, ct);
        }

        var definitions = await _dataLoader.LoadDefinitionsAsync(ct);
        var layoutCache = await _dataLoader.LoadLayoutsAsync(ct);
        return await BuildFileAsync(context, definitions, layoutCache, nachaHeader, ct);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // MÉTODO ALTERNATIVO: Generar NACHA-M por ciclo
    // ─────────────────────────────────────────────────────────────────────────────
    public async Task<string> BuildNachaFileByCycleAsync(string cycleId, CancellationToken ct = default)
        => (await BuildNachaFileArtifactByCycleAsync(cycleId, ct)).Content;

    public async Task<NachaFileBuildArtifact> BuildNachaFileArtifactByCycleAsync(string cycleId, CancellationToken ct = default)
    {
        var result = await BuildNachaFilesByCycleAsync(cycleId, ct);
        if (result.Files.Count == 0)
        {
            throw new InvalidOperationException($"El ciclo {cycleId} no tiene transacciones para exportar.");
        }

        if (result.Files.Count != 1)
        {
            throw new NachaGenerationException(
                "NACHA_MULTI_FILE_RESULT_REQUIRED",
                $"El ciclo {cycleId} produce {result.Files.Count} archivos NACHA-M y debe consumirse mediante el contrato multiarchivo.");
        }

        return result.Files[0];
    }

    public async Task<NachaFileBuildResult> BuildNachaFilesByCycleAsync(string cycleId, CancellationToken ct = default)
    {
        var context = await _dataLoader.LoadByCycleAsync(cycleId, ct);
        var cycle = context.Cycle;
        var transactions = context.Transactions;
        var batches = context.Batches;

        if (transactions.Count == 0)
            return NachaFileBuildResult.Empty;

        if (batches.Count == 0)
            throw new InvalidOperationException($"El ciclo {cycleId} no tiene lotes asociados para exportar.");

        var nachaHeader = await _dataLoader.LoadHeaderAsync(cycle.Id, ct);
        var clearingHouseCode = await ResolveClearingHouseCodeAsync(context, ct);
        var chamberPolicy = NachaChamberFlowPolicyResolver.Resolve(clearingHouseCode);
        var calendarDecision = await _calendarGuard.EnsureExecutableAsync(cycle, ct);
        if (!calendarDecision.CanExecute)
        {
            throw new CycleDeferredByCalendarException(cycle.Id, calendarDecision);
        }
        chamberPolicy.EnforcePreResolutionGenerationGate(
            _generationOptions,
            IsOfficialTableDrivenMode(),
            clearingHouseCode);
        await _transactionValidationService.ValidateTransactionsForSendAsync(transactions, ct);
        if (!chamberPolicy.RequiresServicePartitioning)
        {
            var content = await BuildContextContentAsync(context, nachaHeader, ct);
            return new NachaFileBuildResult(
            [
                new NachaFileBuildArtifact(
                    content,
                    transactions.Select(transaction => transaction.Id).OrderBy(id => id).ToArray())
            ]);
        }

        var catalog = (await _dataLoader.LoadCompanyEntryDescriptionCatalogAsync(ct))
            .Select(item => new CompanyEntryDescriptionCatalogItem(item.Term, item.StandardEntryClassCode))
            .ToList();
        var transactionsByBatchId = transactions
            .GroupBy(transaction => transaction.AchBatchId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<AchTransaction>)group.OrderBy(transaction => transaction.Id).ToArray());
        var sources = batches
            .Where(batch => transactionsByBatchId.ContainsKey(batch.Id))
            .Select(batch => new CenitOutboundSourceBatch(
                batch,
                ResolveStandardEntryClassCode(batch, transactionsByBatchId[batch.Id], catalog),
                transactionsByBatchId[batch.Id]))
            .ToArray();
        var outboundPolicies = await ResolveCenitOutboundPoliciesAsync(context, sources, clearingHouseCode, ct);
        var partitions = CenitOutboundFilePartitioner.Partition(sources, outboundPolicies);
        EnsureCompleteCenitMembership(transactions, partitions);

        var artifacts = new List<NachaFileBuildArtifact>(partitions.Count);
        foreach (var partition in partitions.OrderBy(item => item.FileIndex))
        {
            var fileContext = BuildPartitionContext(context, partition);
            var content = await BuildContextContentAsync(fileContext, nachaHeader, ct);
            var memberships = partition.Batches
                .Select((batch, index) => new NachaFileBatchMembership(
                    index + 1,
                    batch.SourceBatch.Id,
                    batch.ServiceCode,
                    batch.Transactions.Select(transaction => transaction.Id).OrderBy(id => id).ToArray()))
                .ToArray();
            artifacts.Add(new NachaFileBuildArtifact(
                content,
                fileContext.Transactions.Select(transaction => transaction.Id).OrderBy(id => id).ToArray())
            {
                ProfileIdentity = partition.ProfileIdentity,
                ServiceCodes = partition.ServiceCodes,
                Batches = memberships
            });
        }

        return new NachaFileBuildResult(artifacts);
    }

    private async Task<IReadOnlyList<NachaOutboundPartitionPolicy>> ResolveCenitOutboundPoliciesAsync(
        NachaBuildContext context,
        IReadOnlyList<CenitOutboundSourceBatch> sources,
        string clearingHouseCode,
        CancellationToken ct)
    {
        if (_configResolver is null)
        {
            throw new NachaGenerationException(
                "NACHA_OUTBOUND_POLICY_RESOLVER_MISSING",
                "El resolver de perfiles no está disponible para obtener la política outbound oficial.");
        }

        var policies = new Dictionary<int, NachaOutboundPartitionPolicy>();
        foreach (var serviceGroup in sources.GroupBy(source => source.ServiceCode, StringComparer.OrdinalIgnoreCase))
        {
            var serviceTransactions = serviceGroup
                .SelectMany(source => source.Transactions)
                .OrderBy(transaction => transaction.Id)
                .ToArray();
            var resolution = await _configResolver.ResolvePublishedOrdinaryAsync(new NachaConfigResolutionRequest
            {
                ClearingHouseCode = clearingHouseCode,
                FlowTypeCode = NachaProfileDimensionResolver.ResolveFlowCode(serviceTransactions),
                DirectionCode = NachaProfileDimensionResolver.ResolveDirectionCode(serviceTransactions),
                ServiceClassCode = serviceGroup.Key,
                ProcessDateUtc = context.Cycle.ProcessingDate,
                RequireOutboundPolicy = true
            }, ct);

            if (!resolution.Success || resolution.Profile is null || resolution.OutboundPolicy is null)
            {
                throw new NachaGenerationException(
                    resolution.DiagnosticCode,
                    resolution.Trace.LastOrDefault()
                    ?? $"No se resolvió política outbound para el servicio '{serviceGroup.Key}'.");
            }

            policies.TryAdd(resolution.Profile.Id, resolution.OutboundPolicy with
            {
                CardinalityPolicy = resolution.CardinalityPolicy,
                TransactionCodeContract = resolution.TransactionCodeContract,
                SelectedProfileId = resolution.Profile.Id,
                SelectedVersionMajor = resolution.Profile.VersionMajor,
                SelectedVersionMinor = resolution.Profile.VersionMinor
            });
        }

        return policies.Values.OrderBy(policy => policy.FileOrder).ToArray();
    }

    private static void EnsureCompleteCenitMembership(
        IReadOnlyCollection<AchTransaction> sourceTransactions,
        IReadOnlyCollection<CenitOutboundFilePartition> partitions)
    {
        var sourceIds = sourceTransactions.Select(transaction => transaction.Id).OrderBy(id => id).ToArray();
        var emittedIds = partitions
            .SelectMany(partition => partition.Batches)
            .SelectMany(batch => batch.Transactions)
            .Select(transaction => transaction.Id)
            .OrderBy(id => id)
            .ToArray();
        if (!sourceIds.SequenceEqual(emittedIds))
        {
            throw new NachaGenerationException(
                "CENIT_TRANSACTION_MEMBERSHIP_INCOMPLETE",
                "La partición CENIT no preservó exactamente todas las transacciones exportables del ciclo.");
        }
    }

    private async Task<string> BuildContextContentAsync(
        NachaBuildContext context,
        NachaHeader? nachaHeader,
        CancellationToken ct)
    {
        if (IsOfficialTableDrivenMode())
        {
            return await BuildOfficialTableDrivenFileAsync(context, nachaHeader, ct);
        }

        var layoutCache = await _dataLoader.LoadLayoutsAsync(ct);
        var definitions = await _dataLoader.LoadDefinitionsAsync(ct);
        return await BuildFileAsync(context, definitions, layoutCache, nachaHeader, ct);
    }

    private static NachaBuildContext BuildPartitionContext(
        NachaBuildContext source,
        CenitOutboundFilePartition partition)
    {
        var batches = partition.Batches
            .Select((item, index) => CloneBatchForFile(item, index + 1))
            .ToArray();
        return new NachaBuildContext
        {
            Cycle = source.Cycle,
            Batches = batches,
            Transactions = batches.SelectMany(batch => batch.Transactions).ToArray(),
            StandardEntryClassCode = partition.StandardEntryClassCode,
            SelectedProfileId = partition.SelectedProfileId,
            SelectedProfileCode = partition.ProfileIdentity,
            SelectedVersionMajor = partition.SelectedVersionMajor,
            SelectedVersionMinor = partition.SelectedVersionMinor
        };
    }

    private static AchBatch CloneBatchForFile(CenitOutboundBatchPartition partition, int ordinal)
        => new()
        {
            Id = -ordinal,
            AchCycleId = partition.SourceBatch.AchCycleId,
            AchCycle = partition.SourceBatch.AchCycle,
            ServiceClassCode = partition.SourceBatch.ServiceClassCode,
            CompanyName = partition.SourceBatch.CompanyName,
            CompanyIdentification = partition.SourceBatch.CompanyIdentification,
            CompanyEntryDescription = partition.SourceBatch.CompanyEntryDescription,
            CompanyEntryDescriptionId = partition.SourceBatch.CompanyEntryDescriptionId,
            OriginOrOdfi = partition.SourceBatch.OriginOrOdfi,
            EffectiveEntryDate = partition.SourceBatch.EffectiveEntryDate,
            BatchSequenceNumber = ordinal,
            TotalDebitAmount = partition.Transactions
                .Where(transaction => transaction.Type is TransactionTypeEnum.Debit or TransactionTypeEnum.Return or TransactionTypeEnum.Reversal)
                .Sum(transaction => transaction.Amount),
            TotalCreditAmount = partition.Transactions
                .Where(transaction => transaction.Type is TransactionTypeEnum.Credit or TransactionTypeEnum.Prenotification)
                .Sum(transaction => transaction.Amount),
            Transactions = partition.Transactions.ToList()
        };

    // ─────────────────────────────────────────────────────────────────────────────
    // MÉTODO DE INTERFAZ: Cumple contrato de INachaFileBuilder
    // ─────────────────────────────────────────────────────────────────────────────
    public async Task<string> BuildRecordAsync<T>(string recordType, T entity, CancellationToken ct = default)
    {
        var layoutCache = await _dataLoader.LoadLayoutsAsync(ct);
        if (!layoutCache.TryGetValue(recordType, out var layout))
        {
            throw new InvalidOperationException($"Layout no encontrado para '{recordType}'.");
        }

        return await _recordRenderer.RenderRecordAsync(recordType, entity, layout);
    }

    private async Task<string> BuildFileAsync(
        NachaBuildContext context,
        IReadOnlyList<NachaRecordDefinition> definitions,
        IReadOnlyDictionary<string, NachaRecordLayout> layoutCache,
        NachaHeader? header,
        CancellationToken ct)
    {
        var orderedBatches = context.Batches.ToList();
        var clearingHouseCode = await ResolveClearingHouseCodeAsync(context, ct);
        var resolution = await ResolveRuntimeConfigAsync(context, definitions, clearingHouseCode, ct);
        var batchNumberAssignment = await ResolveBatchNumberAssignmentAsync(
            orderedBatches,
            clearingHouseCode,
            context.Cycle.ProcessingDate,
            resolution.OutboundPolicy?.BatchNumberAssignment,
            ct);
        var batchSequenceById = batchNumberAssignment.BatchNumberByBatchId;

        if (!orderedBatches.Any())
            throw new InvalidOperationException("No se encontraron lotes para exportar.");

        var transactionCount = context.Transactions.Count;
        var estimatedRecordCount = Math.Max(1, transactionCount * 3 + (orderedBatches.Count * 4) + 10);
        var estimatedRecordLength = layoutCache.TryGetValue("6", out var entryLayout)
            ? entryLayout.TotalLength
            : 106;
        var sb = new StringBuilder(capacity: estimatedRecordCount * estimatedRecordLength);
        var audit = new NachaGenerationAuditResult
        {
            Mode = (_generationOptions.Mode ?? "LEGACY").Trim().ToUpperInvariant(),
            ClearingHouseCode = clearingHouseCode
        };
        var settlementPolicy = resolution.SettlementPolicy?.ToString() ?? "UNRESOLVED";
        audit.Trace.Add($"HeaderPolicy:Chamber={audit.ClearingHouseCode};SettlementDate={settlementPolicy}");
        audit.Trace.Add($"BatchNumberPolicy:{batchNumberAssignment.PolicyCode};ScopedGroups={batchNumberAssignment.ScopedGroups}");
        foreach (var scopeTrace in batchNumberAssignment.ScopeTrace)
        {
            audit.Trace.Add($"BatchNumberScope:{scopeTrace.Scope};Policy={scopeTrace.PolicyCode};Previous={scopeTrace.PreviousValue};Assigned={scopeTrace.AssignedValue};WasCreated={scopeTrace.WasCreated};Reserved={scopeTrace.ReservedCount}");
        }

        if (resolution.Profile is not null)
        {
            audit.ProfileId = resolution.Profile.Id;
            audit.ProfileCode = resolution.Profile.ProfileCode;
            audit.Trace.AddRange(resolution.Trace);
            audit.Warnings.AddRange(resolution.Warnings);
        }

        var transactionsByBatchId = orderedBatches.ToDictionary(
            batch => batch.Id,
            batch => batch.Transactions.OrderBy(transaction => transaction.Id).ToList());

        long totalDebit = 0, totalCredit = 0;
        int recordCount = 0, batchCount = orderedBatches.Count, entryAddendaCount = 0;

        var companyEntryDescriptionCatalog = (await _dataLoader.LoadCompanyEntryDescriptionCatalogAsync(ct))
            .Select(item => new CompanyEntryDescriptionCatalogItem(item.Term, item.StandardEntryClassCode))
            .ToList();

        var batchCalculations = new Dictionary<int, BatchCalculation>(orderedBatches.Count);
        var totalAddendaOnlyCount = 0;
        foreach (var batch in orderedBatches)
        {
            var batchTransactions = transactionsByBatchId.TryGetValue(batch.Id, out var txs)
                ? (IReadOnlyList<AchTransaction>)txs
                : Array.Empty<AchTransaction>();

            var addendaCount = 0;
            long batchDebit = 0, batchCredit = 0;
            var creditLikeCount = 0;

            foreach (var tx in batchTransactions)
            {
                if (tx.Type is TransactionTypeEnum.Credit or TransactionTypeEnum.Prenotification)
                {
                    creditLikeCount++;
                    batchCredit += (long)(tx.Amount * 100);
                }
                else
                {
                    batchDebit += (long)(tx.Amount * 100);
                }

                addendaCount += tx.Addendas is { Count: > 0 } ? tx.Addendas.Count : 1;
            }

            totalDebit += batchDebit;
            totalCredit += batchCredit;
            totalAddendaOnlyCount += addendaCount;

            var description = (batch.CompanyEntryDescription ?? string.Empty).Trim().ToUpperInvariant();
            var secCode = ResolveStandardEntryClassCode(batch, batchTransactions, companyEntryDescriptionCatalog);
            var batchDescription = creditLikeCount > 1 ? "MULTICREDIT" : description;

            batchCalculations[batch.Id] = new BatchCalculation(
                Transactions: batchTransactions,
                EntryAddendaCount: batchTransactions.Count + addendaCount,
                AddendaOnlyCount: addendaCount,
                BatchDebit: batchDebit,
                BatchCredit: batchCredit,
                StandardEntryClassCode: secCode,
                BatchEntryDescription: batchDescription);
        }

        foreach (var definition in definitions)
        {
            if (!definition.IsEnabled)
            {
                continue;
            }

            switch (definition.RecordCode)
            {
                case "1":
                    recordCount += await AppendResolvedOrLegacyAsync(
                        sb,
                        definition.RecordCode,
                        [FileHeaderRecord.From(context.Cycle, context.Transactions, header)],
                        definition,
                        layoutCache,
                        context,
                        resolution,
                        audit,
                        ct);
                    break;
                case "5":
                    var type5Records = new List<object>(orderedBatches.Count);
                    foreach (var batch in orderedBatches)
                    {
                        var calculation = batchCalculations[batch.Id];
                        type5Records.Add(BatchHeaderRecord.From(
                            batch,
                            calculation.StandardEntryClassCode,
                            batchSequenceById[batch.Id],
                            calculation.BatchEntryDescription));
                    }

                    recordCount += await AppendResolvedOrLegacyAsync(
                        sb,
                        definition.RecordCode,
                        type5Records,
                        definition,
                        layoutCache,
                        context,
                        resolution,
                        audit,
                        ct);
                    break;
                case "6":
                    recordCount += await AppendResolvedOrLegacyAsync(
                        sb,
                        definition.RecordCode,
                        await BuildEntryDetailRecordsAsync(context.Transactions, ct),
                        definition,
                        layoutCache,
                        context,
                        resolution,
                        audit,
                        ct);
                    entryAddendaCount += transactionCount;
                    break;
                case "7":
                    var type7Candidates = (_type7GenerationStrategy?.BuildCandidates(orderedBatches)
                                           ?? BuildFallbackType7Candidates(orderedBatches)).ToList();
                    recordCount += await AppendType7ResolvedOrLegacyAsync(
                        sb,
                        definition,
                        layoutCache,
                        resolution,
                        audit,
                        type7Candidates,
                        context,
                        ct);
                    entryAddendaCount += type7Candidates.Count;
                    break;
                case "8":
                    var type8Records = new List<object>(orderedBatches.Count);
                    foreach (var batch in orderedBatches)
                    {
                        var calculation = batchCalculations[batch.Id];
                        type8Records.Add(BatchControlRecord.From(
                            batch,
                            calculation.EntryAddendaCount,
                            calculation.BatchDebit,
                            calculation.BatchCredit,
                            batchSequenceById[batch.Id]));
                    }

                    recordCount += await AppendResolvedOrLegacyAsync(
                        sb,
                        definition.RecordCode,
                        type8Records,
                        definition,
                        layoutCache,
                        context,
                        resolution,
                        audit,
                        ct);
                    break;
                case "9":
                    var totalRecords = recordCount + 1;
                    var blockLayout = NachaFileBlocking.Calculate(totalRecords, 10);
                    var blockCount = blockLayout.BlockCount;
                    var paddingNeeded = blockLayout.PaddingRecordCount;
                    var fileControl = FileControlRecord.From(context.Cycle, orderedBatches, batchCount, blockCount, entryAddendaCount, totalDebit, totalCredit);
                    audit.Trace.Add($"FileIntegrity:BatchCount={batchCount};EntryAddendaCount={entryAddendaCount};MonetaryTotals=REDACTED;BlockCount={blockCount};PaddingNeeded={paddingNeeded}");

                    recordCount += await AppendResolvedOrLegacyAsync(
                        sb,
                        definition.RecordCode,
                        [fileControl],
                        definition,
                        layoutCache,
                        context,
                        resolution,
                        audit,
                        ct);

                    if (paddingNeeded > 0)
                    {
                        var paddingRecord = NachaFileBlocking.BuildPaddingRecord(layoutCache["9"].TotalLength);
                        EnsureRecordLength("9", paddingRecord, layoutCache["9"].TotalLength);
                        for (int i = 0; i < paddingNeeded; i++)
                        {
                            sb.Append(paddingRecord);
                        }
                    }
                    break;
            }
        }

        var fileContent = sb.ToString();
        await PersistGenerationAuditAsync(audit, resolution.Profile?.Id, ct);
        if (audit.Warnings.Count > 0)
        {
            _logger?.LogWarning("NACHA generación con advertencias: {Warnings}", string.Join(" | ", audit.Warnings));
        }
        _nachaSemanticValidator.Validate(fileContent, context);
        return fileContent;
    }

    private async Task<string> BuildOfficialTableDrivenFileAsync(
        NachaBuildContext context,
        NachaHeader? header,
        CancellationToken ct)
    {
        // The loader preserves the caller/business order. ACHCOL batch numbers are file-local
        // ordinals and must never be derived, directly or indirectly, from a technical PK.
        var orderedBatches = context.Batches.ToList();
        if (!orderedBatches.Any())
        {
            throw new InvalidOperationException("No se encontraron lotes para exportar.");
        }

        var operationalSnapshot = _operationalTimeProvider.GetOrCreate(
            $"NACHA:{context.Cycle.Id}",
            DateOnly.FromDateTime(context.Cycle.ProcessingDate),
            TimeOnly.FromTimeSpan(context.Cycle.CutoffTime));

        var officialRecordCodes = new[] { "1", "5", "6", "7", "8", "9" };
        var clearingHouseCode = await ResolveClearingHouseCodeAsync(context, ct);
        var chamberPolicy = NachaChamberFlowPolicyResolver.Resolve(clearingHouseCode);
        var resolution = await ResolveOfficialRuntimeConfigAsync(context, clearingHouseCode, officialRecordCodes, chamberPolicy, ct);
        ValidatePersistedTransactionCodes(context.Transactions, resolution);
        var lineLength = RequireOfficialLayout(resolution, "9").TotalLength;
        var batchNumberAssignment = await ResolveBatchNumberAssignmentAsync(
            orderedBatches,
            clearingHouseCode,
            operationalSnapshot.BogotaTimestamp,
            resolution.OutboundPolicy?.BatchNumberAssignment,
            ct);
        var batchSequenceById = batchNumberAssignment.BatchNumberByBatchId;

        var audit = new NachaGenerationAuditResult
        {
            Mode = "TABLE_DRIVEN",
            ClearingHouseCode = clearingHouseCode,
            ClearingHouseName = context.Cycle.ClearingHouse?.Name,
            ProfileId = resolution.Profile?.Id,
            ProfileCode = resolution.Profile?.ProfileCode,
            ProfileVersion = resolution.Profile is null ? null : $"{resolution.Profile.VersionMajor}.{resolution.Profile.VersionMinor}",
            ProfileStatus = resolution.Profile?.Status?.Code,
            EffectiveDate = operationalSnapshot.BogotaTimestamp,
            LegacyFallbackUsed = false,
            Phase = "6B.3B",
            CorrelationId = $"NACHA-GEN-{operationalSnapshot.CapturedAtUtc:yyyyMMddHHmmss}"
        };
        audit.Trace.AddRange(resolution.Trace);
        audit.Trace.AddRange(batchNumberAssignment.ScopeTrace.Select(scopeTrace =>
            $"BatchNumberScope:{scopeTrace.Scope};Policy={scopeTrace.PolicyCode};Previous={scopeTrace.PreviousValue};Assigned={scopeTrace.AssignedValue};WasCreated={scopeTrace.WasCreated};Reserved={scopeTrace.ReservedCount}"));
        audit.Trace.Add($"OfficialTableDriven:Profile={resolution.Profile?.ProfileCode};LegacyFallbackUsed=false;Records={string.Join(",", officialRecordCodes)}");
        audit.NewEngineRecordCodes.AddRange(officialRecordCodes);

        var transactionsByBatchId = orderedBatches.ToDictionary(
            batch => batch.Id,
            batch => (IReadOnlyList<AchTransaction>)batch.Transactions.OrderBy(transaction => transaction.Id).ToList());

        var ordinaryFlow = chamberPolicy.ResolveOrdinaryFlow(resolution.Profile?.ProfileCode);
        var dailySequence = header?.CycleNumber > 0 ? header.CycleNumber : 1;
        var fileIdModifier = _controlTotalsCalculator.ResolveFileIdModifier(dailySequence);
        audit.FileIdModifier = new NachaFileIdModifierAudit { DailySequence = dailySequence, ResolvedValue = fileIdModifier };

        var companyEntryDescriptionCatalog = (resolution.StandardEntryClassMappings
            ?? throw new InvalidOperationException("La publicación ordinaria no contiene el catálogo SEC resuelto."))
            .Select(item => new CompanyEntryDescriptionCatalogItem(item.Term, item.StandardEntryClassCode))
            .ToList();

        var batchCalculations = new Dictionary<int, BatchCalculation>(orderedBatches.Count);
        foreach (var batch in orderedBatches)
        {
            var batchTransactions = transactionsByBatchId.TryGetValue(batch.Id, out var txs)
                ? txs
                : Array.Empty<AchTransaction>();

            var creditLikeCount = 0;
            foreach (var tx in batchTransactions)
            {
                if (tx.Type is TransactionTypeEnum.Credit or TransactionTypeEnum.Prenotification)
                {
                    creditLikeCount++;
                }
            }

            var description = (batch.CompanyEntryDescription ?? string.Empty).Trim().ToUpperInvariant();
            var secCode = ResolveStandardEntryClassCode(batch, batchTransactions, companyEntryDescriptionCatalog);
            var batchDescription = creditLikeCount > 1
                ? ResolveMassCreditDescription(RequireOfficialLayout(resolution, "5"))
                : description;

            batchCalculations[batch.Id] = new BatchCalculation(
                Transactions: batchTransactions,
                StandardEntryClassCode: secCode,
                BatchEntryDescription: batchDescription);
        }

        var type7CandidatesByBatchId = orderedBatches.ToDictionary(
            batch => batch.Id,
            batch => (_type7GenerationStrategy?.BuildCandidates([batch]) ?? BuildFallbackType7Candidates([batch])).ToList());
        var physicalRecordsBeforePadding = 1 + orderedBatches.Count * 2 + context.Transactions.Count + type7CandidatesByBatchId.Values.Sum(x => x.Count) + 1;
        var controlTotals = _controlTotalsCalculator.Calculate(new NachaControlTotalsRequest
        {
            Batches = orderedBatches,
            TransactionsByBatchId = transactionsByBatchId,
            AddendaRecordCountByBatchId = type7CandidatesByBatchId.ToDictionary(x => x.Key, x => x.Value.Count),
            EntryHashSourceFieldPath = ResolveEntryHashSourceFieldPath(RequireOfficialLayout(resolution, "6")),
            BatchEntryHashLength = ResolveOfficialFieldLength(RequireOfficialLayout(resolution, "8"), "ENTRYHASH"),
            FileEntryHashLength = ResolveOfficialFieldLength(RequireOfficialLayout(resolution, "9"), "ENTRYHASH"),
            BatchEntryAddendaCountLength = ResolveOfficialFieldLength(RequireOfficialLayout(resolution, "8"), "ENTRYADDENDACOUNT"),
            FileEntryAddendaCountLength = ResolveOfficialFieldLength(RequireOfficialLayout(resolution, "9"), "ENTRYADDENDACOUNT"),
            BatchTotalDebitAmountLength = ResolveOfficialFieldLength(RequireOfficialLayout(resolution, "8"), "TOTALDEBITAMOUNT"),
            FileTotalDebitAmountLength = ResolveOfficialFieldLength(RequireOfficialLayout(resolution, "9"), "TOTALDEBITAMOUNT"),
            BatchTotalCreditAmountLength = ResolveOfficialFieldLength(RequireOfficialLayout(resolution, "8"), "TOTALCREDITAMOUNT"),
            FileTotalCreditAmountLength = ResolveOfficialFieldLength(RequireOfficialLayout(resolution, "9"), "TOTALCREDITAMOUNT"),
            BatchCountLength = ResolveOfficialFieldLength(RequireOfficialLayout(resolution, "9"), "BATCHCOUNT"),
            BlockCountLength = ResolveOfficialFieldLength(RequireOfficialLayout(resolution, "9"), "BLOCKCOUNT"),
            PhysicalRecordCountBeforePadding = physicalRecordsBeforePadding,
            BlockSize = NachaFileBlocking.ResolveConfiguredBlockSize(RequireOfficialLayout(resolution, "1"))
        });
        var controlTotalsByBatchId = controlTotals.BatchTotals.ToDictionary(x => x.BatchId);
        AddControlTotalsAudit(audit, controlTotals);

        var assembly = new NachaOfficialAssemblyPipeline(
            OfficialRecordRenderer,
            audit,
            chamberPolicy.CreateOfficialRecordRenderContext(clearingHouseCode),
            controlTotals.FileTotals);
        string fileContent;
        try
        {
            foreach (var recordCode in officialRecordCodes)
            {
                ValidateMaterializedLayoutStructure(recordCode, RequireOfficialLayout(resolution, recordCode), audit);
            }

            if (ordinaryFlow.ValidateAllPublishedType7Variants
                && resolution.LayoutVariantsByRecordCode.TryGetValue("7", out var type7Layouts))
            {
                foreach (var type7Layout in type7Layouts)
                {
                    ValidateMaterializedLayoutStructure("7", type7Layout, audit);
                }
            }

            assembly.AppendFileHeader(new NachaOfficialAssemblyRecord(
                "1",
                FileHeaderRecord.From(context.Cycle, context.Transactions, header, operationalSnapshot, fileIdModifier),
                RequireOfficialLayout(resolution, "1")));

            foreach (var batch in orderedBatches)
            {
                var calculation = batchCalculations[batch.Id];
                assembly.BeginBatch(new NachaOfficialAssemblyRecord(
                    "5",
                    BatchHeaderRecord.From(batch, calculation.StandardEntryClassCode, batchSequenceById[batch.Id], calculation.BatchEntryDescription, operationalSnapshot),
                    RequireOfficialLayout(resolution, "5")));

                var type7Candidates = type7CandidatesByBatchId[batch.Id];
                var type7ByTransaction = type7Candidates
                    .GroupBy(candidate => candidate.Transaction.Id)
                    .ToDictionary(group => group.Key, group => group.ToList());
                if (ordinaryFlow.ValidateCtxAddendaSequences)
                {
                    ValidateCtxAddendaSequences(calculation.Transactions, type7ByTransaction,
                        resolution.CardinalityPolicy, resolution.TransactionCodeContract);
                }
                var entryDetails = await BuildEntryDetailRecordsOfficialAsync(
                    calculation.Transactions,
                    RequireOfficialLayout(resolution, "6"),
                    type7ByTransaction,
                    ct);

                foreach (var entryDetail in entryDetails)
                {
                    assembly.BeginEntry(new NachaOfficialAssemblyRecord(
                        "6",
                        entryDetail,
                        RequireOfficialLayout(resolution, "6")));

                    if (!type7ByTransaction.TryGetValue(entryDetail.TransactionId, out var associatedAddendas))
                    {
                        if (entryDetail.AddendumIndicator == "1")
                        {
                            throw BuildCrossFieldException(
                                "ACHCOL-T6-ADDENDA-INDICATOR",
                                "6/7",
                                "ADDENDARECORDINDICATOR",
                                87,
                                1,
                                "El indicador declara adenda, pero no existe T7 asociado.");
                        }

                        assembly.EndEntry();
                        continue;
                    }

                    foreach (var candidate in associatedAddendas.OrderBy(item => item.Addenda.SequenceNumber ?? 1))
                    {
                        if (ordinaryFlow.ValidateType7Association)
                        {
                            ValidateType7Association(entryDetail, candidate, ordinaryFlow.ValidateCtxAddendaSequences);
                        }

                        var type7Layout = ordinaryFlow.UsePublishedType7Variant
                            ? ResolveType7Layout(resolution, candidate)
                            : RequireOfficialLayout(resolution, "7");
                        assembly.AppendAddenda(new NachaOfficialAssemblyRecord(
                            "7",
                            candidate.FieldValues,
                            type7Layout));
                    }
                    assembly.EndEntry();
                }

                var renderedTransactionIds = entryDetails.Select(entry => entry.TransactionId).ToHashSet();
                if (ordinaryFlow.RequireAllType7CandidatesRendered && type7Candidates.Any(candidate => !renderedTransactionIds.Contains(candidate.Transaction.Id)))
                {
                    throw BuildCrossFieldException(
                        "ACHCOL-T7-TRACE-SUFFIX-MATCH",
                        "7",
                        "TRACESUFFIX",
                        88,
                        7,
                        "Existe un T7 sin T6 perteneciente al lote renderizado.");
                }

                var batchTotals = controlTotalsByBatchId[batch.Id];
                assembly.EndBatch(
                    new NachaOfficialAssemblyRecord(
                        "8",
                        BatchControlRecord.From(batch, batchTotals, batchSequenceById[batch.Id]),
                        RequireOfficialLayout(resolution, "8")),
                    batchTotals);
            }

            var fileTotals = controlTotals.FileTotals;
            var fileControl = FileControlRecord.From(context.Cycle, fileTotals);
            fileContent = assembly.Complete(new NachaOfficialAssemblyRecord(
                "9",
                fileControl,
                RequireOfficialLayout(resolution, "9")));
        }
        catch (NachaGenerationException ex)
        {
            audit.Status = "Failed";
            audit.ErrorCode = ex.Code;
            audit.TotalRecords = assembly.RenderedRecordCount;
            audit.TotalFields = audit.FieldTraceEntries.Count;
            await PersistGenerationAuditAsync(audit, resolution.Profile?.Id, ct, operationalSnapshot.CapturedAtUtc);
            throw;
        }

        audit.TotalRecords = fileContent.Length / lineLength;
        audit.TotalFields = audit.FieldTraceEntries.Count;
        audit.FileHash = Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(fileContent)));
        await PersistGenerationAuditAsync(audit, resolution.Profile?.Id, ct, operationalSnapshot.CapturedAtUtc);
        _nachaSemanticValidator.Validate(fileContent, context, resolution.SemanticContract
            ?? throw new NachaGenerationException("NACHA_SEMANTIC_CONTRACT_MISSING", "El perfil resuelto no materializó el contrato semántico ServiceClassCode."));
        return fileContent;
    }

    private int AppendReturnOutOfficialRecords(
        StringBuilder sb,
        string recordCode,
        IEnumerable<object> records,
        CfgLayoutVariant layout,
        NachaGenerationAuditResult audit,
        ref int lineNumber)
    {
        var count = 0;
        var renderContext = NachaChamberFlowPolicyResolver.Resolve(audit.ClearingHouseCode)
            .CreateOfficialRecordRenderContext(audit.ClearingHouseCode);
        foreach (var record in records)
        {
            sb.Append(OfficialRecordRenderer.Render(recordCode, record, layout, audit, renderContext, count + 1, lineNumber));
            lineNumber++;
            count++;
        }

        return count;
    }

    private Task<BatchNumberAssignmentResult> ResolveBatchNumberAssignmentAsync(
        IReadOnlyList<AchBatch> orderedBatches,
        string clearingHouseCode,
        DateTime processingDate,
        NachaOutboundBatchNumberAssignmentPolicy? batchNumberPolicy,
        CancellationToken ct)
    {
        if (batchNumberPolicy?.Strategy == NachaOutboundBatchNumberAssignmentStrategy.FileLocalOrdinal)
        {
            if (batchNumberPolicy.Scope != NachaOutboundBatchNumberScope.CurrentFile)
            {
                throw new NachaGenerationException(
                    "NACHA_BATCH_NUMBER_POLICY_INVALID",
                    "La política publicada de numeración de lotes no tiene un alcance ejecutable.");
            }
            var availableNumbers = (long)batchNumberPolicy.MaximumBatchNumber - batchNumberPolicy.StartingNumber + 1;
            if (orderedBatches.Count > availableNumbers)
            {
                throw new NachaGenerationException(
                    "NACHA_BATCH_NUMBER_OVERFLOW",
                    "La cantidad de lotes excede la capacidad publicada de numeración.",
                    ruleId: null,
                    chamber: clearingHouseCode,
                    recordType: "5/8",
                    fieldName: "BATCHNUMBER",
                    cause: "Ordinal de lote fuera de rango.",
                    expectedLength: batchNumberPolicy.MaximumBatchNumber.ToString(CultureInfo.InvariantCulture).Length);
            }

            var assignments = orderedBatches
                .Select((batch, index) => new { batch.Id, Number = batchNumberPolicy.StartingNumber + index })
                .ToDictionary(item => item.Id, item => item.Number);
            return Task.FromResult(new BatchNumberAssignmentResult(
                assignments,
                batchNumberPolicy.PolicyIdentity,
                1,
                [new BatchNumberScopeTrace(
                    batchNumberPolicy.PolicyIdentity,
                    batchNumberPolicy.Scope.ToString().ToUpperInvariant(),
                    0,
                    orderedBatches.Count,
                    true,
                    orderedBatches.Count)]));
        }

        var hasCompletePersistedAssignment = orderedBatches.Count > 0
            && orderedBatches.All(batch => batch.BatchSequenceNumber > 0)
            && orderedBatches
                .GroupBy(batch => (batch.OriginOrOdfi ?? string.Empty).Trim(), StringComparer.OrdinalIgnoreCase)
                .All(group => group.Select(batch => batch.BatchSequenceNumber).Distinct().Count() == group.Count());

        if (!hasCompletePersistedAssignment)
        {
            return _batchNumberGenerator.AssignBatchNumbersAsync(orderedBatches, clearingHouseCode, processingDate, ct);
        }

        return Task.FromResult(new BatchNumberAssignmentResult(
            orderedBatches.ToDictionary(batch => batch.Id, batch => batch.BatchSequenceNumber),
            "PERSISTED_BATCH_SEQUENCE",
            orderedBatches.Select(batch => (batch.OriginOrOdfi ?? string.Empty).Trim()).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            []));
    }

    private async Task<IReadOnlyList<EntryDetailRecord>> BuildEntryDetailRecordsOfficialAsync(
        IReadOnlyList<AchTransaction> transactions,
        CfgLayoutVariant record6Layout,
        IReadOnlyDictionary<int, List<NachaType7RecordCandidate>> type7ByTransaction,
        CancellationToken ct)
    {
        var records = new List<EntryDetailRecord>(transactions.Count);
        var receivingDfiLength = ResolveOfficialFieldLength(record6Layout, "RECEIVINGDFI");
        var receiverLookup = await BuildReceiverLookupAsync(transactions, ct);

        foreach (var transaction in transactions.OrderBy(t => t.Id))
        {
            var receiverName = await ResolveReceiverNameForType6Async(transaction, receiverLookup, ct);
            if (string.IsNullOrWhiteSpace(receiverName))
            {
                throw new NachaGenerationException(
                    "NACHA_FIELD_RULE_FAILED",
                    "El nombre del receptor es obligatorio para el registro tipo 6.",
                    ruleId: "ACHCOL-T6-INDIVIDUAL-NAME",
                    chamber: "ACHCOL",
                    recordType: "6",
                    fieldName: "INDIVIDUALNAME",
                    cause: "Valor requerido ausente.",
                    startPosition: 63,
                    expectedLength: 22);
            }

            records.Add(EntryDetailRecord.From(
                transaction,
                receiverName,
                receivingDfiLength,
                type7ByTransaction.TryGetValue(transaction.Id, out var addendas) ? addendas.Count : 0));
        }

        return records;
    }

    private static void ValidatePersistedTransactionCodes(
        IReadOnlyCollection<AchTransaction> transactions,
        NachaConfigResolutionResult resolution)
    {
        var contract = resolution.TransactionCodeContract
            ?? throw new NachaGenerationException(
                "NACHA_TRANSACTION_CODE_CONTRACT_MISSING",
                "La publicación seleccionada no contiene autoridad semántica TransactionCode para generación ordinaria.");

        foreach (var transaction in transactions)
        {
            var persistedCode = transaction.TransactionCode?.Trim() ?? string.Empty;
            if (!contract.TryGetRule(persistedCode, out var rule))
            {
                throw new NachaGenerationException(
                    "NACHA_TRANSACTION_CODE_NOT_ACCEPTED",
                    $"La publicación seleccionada no acepta el TransactionCode persistido de la transacción {transaction.Id}.");
            }

            if (rule.IsPrenotification != transaction.IsPrenotification
                || (transaction.Type == TransactionTypeEnum.Prenotification && !transaction.IsPrenotification))
            {
                throw new NachaGenerationException(
                    "NACHA_TRANSACTION_CODE_PRENOTE_MISMATCH",
                    $"El TransactionCode persistido no coincide con el estado de prenotificación de la transacción {transaction.Id}.");
            }

            var persistedDirection = transaction.Type switch
            {
                TransactionTypeEnum.Credit => NachaEntryDirection.Credit,
                TransactionTypeEnum.Debit => NachaEntryDirection.Debit,
                _ => (NachaEntryDirection?)null
            };
            if (persistedDirection.HasValue && persistedDirection.Value != rule.Direction)
            {
                throw new NachaGenerationException(
                    "NACHA_TRANSACTION_CODE_DIRECTION_MISMATCH",
                    $"El TransactionCode persistido no coincide con la dirección de la transacción {transaction.Id}.");
            }
        }
    }

    internal static CfgLayoutVariant ResolveType7Layout(
        NachaConfigResolutionResult resolution,
        NachaType7RecordCandidate candidate)
    {
        if (!resolution.LayoutVariantsByRecordCode.TryGetValue("7", out var variants) || variants.Count == 0)
        {
            throw new NachaGenerationException(
                "NACHA_REQUIRED_RECORD_MISSING",
                "Falta layout publicado para el registro tipo 7 requerido por una entrada con adenda.",
                ruleId: "ACHCOL-T7-ADDENDA-TYPE",
                chamber: "ACHCOL",
                recordType: "7",
                fieldName: "ADDENDATYPE",
                cause: "Variante de adenda requerida ausente.",
                startPosition: 2,
                expectedLength: 2);
        }

        if (candidate.Addenda.BusinessType is not (Cfa.ACHInterbank.Domain.Entities.Transactions.Enums.AchAddendaBusinessType.Credit
            or Cfa.ACHInterbank.Domain.Entities.Transactions.Enums.AchAddendaBusinessType.Debit))
        {
            throw BuildCrossFieldException(
                "ACHCOL-T7-ADDENDA-TYPE",
                "7",
                "ADDENDATYPE",
                2,
                2,
                "La variante de adenda no está demostrada o publicada para ACHCOL.");
        }

        return AchOutboundType7LayoutSelector.Select(
            variants,
            candidate.Addenda.BusinessType,
            candidate.Transaction.IsPrenotification);
    }

    private static void ValidateCtxAddendaSequences(
        IReadOnlyList<AchTransaction> transactions,
        IReadOnlyDictionary<int, List<NachaType7RecordCandidate>> type7ByTransaction,
        NachaAddendaCardinalityPolicy? cardinalityPolicy,
        NachaTransactionCodeSemanticContract? transactionCodeContract)
    {
        foreach (var transaction in transactions.OrderBy(item => item.Id))
        {
            if (!type7ByTransaction.TryGetValue(transaction.Id, out var addendas)
                || addendas.Count > CenitCtxOutbound2026Layout.MaxAddendaPerEntry)
            {
                throw new NachaGenerationException(
                    "CENIT_CTX_ADDENDA_CARDINALITY_INVALID",
                    $"La transacción CTX {transaction.Id} supera la capacidad física de {CenitCtxOutbound2026Layout.MaxAddendaPerEntry} adendas.");
            }
            if (cardinalityPolicy is not null)
            {
                if (transactionCodeContract is null
                    || string.IsNullOrWhiteSpace(transaction.TransactionCode)
                    || !transactionCodeContract.TryGetRule(transaction.TransactionCode, out var semantic)
                    || semantic.IsPrenotification != transaction.IsPrenotification
                    || !cardinalityPolicy.TryResolve("CTX", semantic.Direction, out var bounds))
                {
                    throw new NachaGenerationException(
                        "CARDINALITY_POLICY_UNRESOLVED",
                        $"La transacción CTX {transaction.Id} no tiene una cardinalidad publicada resoluble.");
                }
                if (addendas.Count < bounds.Minimum || addendas.Count > bounds.Maximum)
                {
                    throw new NachaGenerationException(
                        semantic.IsPrenotification
                            ? "CENIT_CTX_PRENOTE_ADDENDA_CARDINALITY_INVALID"
                            : "CENIT_CTX_ADDENDA_CARDINALITY_INVALID",
                        $"La transacción CTX {transaction.Id} no cumple la cardinalidad publicada.");
                }
            }
            else if (addendas.Count < 1
                     || (transaction.Type == TransactionTypeEnum.Prenotification && addendas.Count != 1))
            {
                throw new NachaGenerationException(
                    transaction.Type == TransactionTypeEnum.Prenotification
                        ? "CENIT_CTX_PRENOTE_ADDENDA_CARDINALITY_INVALID"
                        : "CENIT_CTX_ADDENDA_CARDINALITY_INVALID",
                    $"La transacción CTX {transaction.Id} no cumple la cardinalidad histórica.");
            }

            var ordered = addendas.OrderBy(item => item.Addenda.SequenceNumber).ToArray();
            for (var index = 0; index < ordered.Length; index++)
            {
                if (ordered[index].Addenda.SequenceNumber != index + 1)
                {
                    throw new NachaGenerationException(
                        "CENIT_CTX_ADDENDA_SEQUENCE_INVALID",
                        $"La secuencia de adendas CTX de la transacción {transaction.Id} debe iniciar en 0001 y ser consecutiva.");
                }
            }
        }
    }

    private static void ValidateType7Association(
        EntryDetailRecord entry,
        NachaType7RecordCandidate candidate,
        bool isCenitCtx = false)
    {
        var chamber = isCenitCtx ? "CENIT" : "ACHCOL";
        if (entry.AddendumIndicator != "1")
        {
            throw BuildCrossFieldException(
                $"{chamber}-T6-ADDENDA-INDICATOR",
                "6/7",
                "ADDENDARECORDINDICATOR",
                87,
                1,
                "Existe T7, pero el T6 asociado no declara adenda.");
        }

        var trace = entry.TraceNumber?.Trim() ?? string.Empty;
        if (trace.Length != 15 || trace.Any(character => !char.IsDigit(character)))
        {
            throw BuildCrossFieldException(
                $"{chamber}-T6-TRACE-NUMBER",
                "6",
                "TRACENUMBER",
                88,
                15,
                "Trace Number debe contener exactamente quince dígitos.");
        }

        var suffix = candidate.FieldValues.TryGetValue("TraceSuffix", out var rawSuffix)
            ? Convert.ToString(rawSuffix, CultureInfo.InvariantCulture)?.Trim() ?? string.Empty
            : string.Empty;
        if (!string.Equals(suffix, trace[^7..], StringComparison.Ordinal))
        {
            throw BuildCrossFieldException(
                $"{chamber}-T7-TRACE-SUFFIX-MATCH",
                "6/7",
                "TRACESUFFIX",
                88,
                7,
                "El sufijo T7 no coincide con el Trace Number del T6 asociado.");
        }
    }

    private static NachaGenerationException BuildCrossFieldException(
        string ruleId,
        string recordType,
        string fieldName,
        int startPosition,
        int expectedLength,
        string cause)
        => new(
            "NACHA_CROSS_FIELD_VALIDATION_FAILED",
            "Falló una validación cruzada NACHA-M.",
            ruleId,
            "ACHCOL",
            recordType,
            fieldName,
            cause,
            startPosition,
            expectedLength);

    private static int ResolveOfficialFieldLength(CfgLayoutVariant layout, string fieldCode)
    {
        var field = layout.Fields.FirstOrDefault(x =>
            x.IsEnabled &&
            string.Equals(x.FieldCode, fieldCode, StringComparison.OrdinalIgnoreCase));
        if (field is null)
        {
            throw new NachaGenerationException("NACHA_REQUIRED_FIELD_MISSING", $"Falta el campo requerido {fieldCode} en RecordCode={layout.RecordCode?.Code}.");
        }

        return field.Length;
    }

    private static string ResolveMassCreditDescription(CfgLayoutVariant layout)
    {
        const string normativeDescription = "MULTICREDIT";
        var configuredLength = ResolveOfficialFieldLength(layout, "COMPANYENTRYDESCRIPTION");
        return normativeDescription.Length <= configuredLength
            ? normativeDescription
            : normativeDescription[..configuredLength];
    }

    private static string ResolveEntryHashSourceFieldPath(CfgLayoutVariant record6Layout)
    {
        var receivingDfiField = record6Layout.Fields.FirstOrDefault(x =>
            x.IsEnabled && string.Equals(x.FieldCode, "RECEIVINGDFI", StringComparison.OrdinalIgnoreCase));
        var source = receivingDfiField?.SourceDefinition?.PropertyPath;
        if (string.IsNullOrWhiteSpace(source))
        {
            throw new NachaGenerationException("NACHA_ENTRY_HASH_SOURCE_MISSING", "Falta sourceFieldPath para RECEIVINGDFI, requerido para calcular EntryHash.");
        }

        return source;
    }

    private static void AddControlTotalsAudit(NachaGenerationAuditResult audit, NachaControlTotalsResult totals)
    {
        audit.FileTotals = new NachaFileControlTotalsAudit
        {
            BatchCount = totals.FileTotals.BatchCount,
            BlockCount = totals.FileTotals.BlockCount,
            EntryAddendaCount = totals.FileTotals.EntryAddendaCount,
            EntryHash = totals.FileTotals.EntryHash,
            TotalDebitAmountInCents = totals.FileTotals.TotalDebitAmountInCents,
            TotalCreditAmountInCents = totals.FileTotals.TotalCreditAmountInCents,
            PhysicalRecordCountBeforePadding = totals.FileTotals.PhysicalRecordCountBeforePadding,
            PaddingRecordCount = totals.FileTotals.PaddingRecordCount,
            PhysicalRecordCountAfterPadding = totals.FileTotals.PhysicalRecordCountAfterPadding
        };

        audit.BatchTotals.AddRange(totals.BatchTotals.Select(x => new NachaBatchControlTotalsAudit
        {
            BatchId = x.BatchId,
            EntryAddendaCount = x.EntryAddendaCount,
            EntryHash = x.EntryHash,
            TotalDebitAmountInCents = x.TotalDebitAmountInCents,
            TotalCreditAmountInCents = x.TotalCreditAmountInCents,
            EntryDetailCount = x.EntryDetailCount,
            AddendaCount = x.AddendaCount
        }));
    }

    private async Task<int> AppendCustomOrConfiguredAsync(
        StringBuilder sb,
        NachaRecordDefinition definition,
        IReadOnlyDictionary<string, NachaRecordLayout> layoutCache,
        IEnumerable<object> fallbackRecords,
        NachaBuildContext context,
        CancellationToken ct)
    {
        var layout = layoutCache[definition.RecordCode];
        var fallbackList = fallbackRecords.ToList();

        var forceFallback = definition.RecordCode is "1" or "5" or "8" or "9";
        IReadOnlyList<object> records = (definition.SourceType == NachaRecordSourceType.Custom || forceFallback)
            ? fallbackList
            : await _recordDataProvider.GetRecordsAsync(definition, context, ct);

        if (records.Count == 0 && fallbackList.Count > 0)
        {
            records = fallbackList;
        }

        var count = 0;
        foreach (var record in records)
        {
            count++;
            if (record is IReadOnlyDictionary<string, object?> dict)
            {
                sb.Append(await _recordRenderer.RenderRecordAsync(definition.RecordCode, dict, layout));
            }
            else
            {
                sb.Append(await _recordRenderer.RenderRecordAsync(definition.RecordCode, record, layout));
            }
        }

        return count;
    }

    private async Task<int> AppendResolvedOrLegacyAsync(
        StringBuilder sb,
        string recordCode,
        IEnumerable<object> records,
        NachaRecordDefinition definition,
        IReadOnlyDictionary<string, NachaRecordLayout> layoutCache,
        NachaBuildContext context,
        NachaConfigResolutionResult resolution,
        NachaGenerationAuditResult audit,
        CancellationToken ct)
    {
        var mode = (_generationOptions.Mode ?? "LEGACY").Trim().ToUpperInvariant();
        var settlementPolicy = resolution.SettlementPolicy?.ToString() ?? "UNRESOLVED";
        var shouldUseResolver = mode is "HYBRID" or "TABLE_DRIVEN" or "SHADOW_COMPARE";

        if (!shouldUseResolver || _configResolver is null || !resolution.LayoutsByRecordCode.TryGetValue(recordCode, out var layoutVariant))
        {
            if (!audit.LegacyRecordCodes.Contains(recordCode))
            {
                audit.LegacyRecordCodes.Add(recordCode);
            }

            audit.Warnings.Add($"Fallback legado para RecordCode={recordCode}: no hay layout resuelto o modo legado.");
            return await AppendCustomOrConfiguredAsync(sb, definition, layoutCache, records, context, ct);
        }

        var lineCount = 0;
        foreach (var record in records)
        {
            if (recordCode == "1" && ShouldUseRecord1MappingEngine(mode) && _recordMappingEngine is not null && _mappingPlanCompiler is not null)
            {
                var mapped = await TryRenderRecord1WithMappingEngineAsync(recordCode, record, layoutVariant, mode, layoutCache, context, audit, ct);
                if (!string.IsNullOrWhiteSpace(mapped))
                {
                    audit.Trace.Add("R1:MAPPING_ENGINE_APPLIED:BASE_OBJECT=FileHeaderRecord.From");
                    audit.Trace.Add($"R1:CHAMBER={audit.ClearingHouseCode};SETTLEMENT_POLICY={settlementPolicy}");
                    sb.Append(mapped);
                    lineCount++;
                    continue;
                }

                audit.Warnings.Add("RecordCode=1 usó fallback legado por cobertura insuficiente del mapping engine.");
                audit.Trace.Add("R1:MAPPING_ENGINE_FALLBACK:BASE_OBJECT=FileHeaderRecord.From");
                audit.Trace.Add($"R1:CHAMBER={audit.ClearingHouseCode};SETTLEMENT_POLICY={settlementPolicy}");
            }

            if (recordCode == "5" && ShouldUseRecord5MappingEngine(mode) && _recordMappingEngine is not null && _mappingPlanCompiler is not null)
            {
                var mapped = await TryRenderRecord5WithMappingEngineAsync(recordCode, record, layoutVariant, mode, layoutCache, context, audit, ct);
                if (!string.IsNullOrWhiteSpace(mapped))
                {
                    audit.Trace.Add("R5:MAPPING_ENGINE_APPLIED:BASE_OBJECT=BatchHeaderRecord.From");
                    audit.Trace.Add($"R5:CHAMBER={audit.ClearingHouseCode};SETTLEMENT_POLICY={settlementPolicy}");
                    sb.Append(mapped);
                    lineCount++;
                    continue;
                }

                audit.Warnings.Add("RecordCode=5 usó fallback legado por cobertura insuficiente del mapping engine.");
                audit.Trace.Add("R5:MAPPING_ENGINE_FALLBACK:BASE_OBJECT=BatchHeaderRecord.From");
                audit.Trace.Add($"R5:CHAMBER={audit.ClearingHouseCode};SETTLEMENT_POLICY={settlementPolicy}");
            }

            if (recordCode == "6" && ShouldUseRecord6MappingEngine(mode) && _recordMappingEngine is not null && _mappingPlanCompiler is not null)
            {
                var mapped = await TryRenderRecord6WithMappingEngineAsync(recordCode, record, layoutVariant, mode, layoutCache, audit, ct);
                if (!string.IsNullOrWhiteSpace(mapped))
                {
                    sb.Append(mapped);
                    lineCount++;
                    continue;
                }

                audit.Warnings.Add("RecordCode=6 usó fallback legado por cobertura insuficiente del mapping engine.");
            }

            if (recordCode == "8" && ShouldUseRecord8MappingEngine(mode) && _recordMappingEngine is not null && _mappingPlanCompiler is not null)
            {
                var mapped = await TryRenderRecord8WithMappingEngineAsync(recordCode, record, layoutVariant, mode, layoutCache, context, audit, ct);
                if (!string.IsNullOrWhiteSpace(mapped))
                {
                    audit.Trace.Add("R8:MAPPING_ENGINE_APPLIED:BASE_OBJECT=BatchControlRecord.From");
                    sb.Append(mapped);
                    lineCount++;
                    continue;
                }

                audit.Warnings.Add("RecordCode=8 usó fallback legado por cobertura insuficiente del mapping engine.");
                audit.Trace.Add("R8:MAPPING_ENGINE_FALLBACK:BASE_OBJECT=BatchControlRecord.From");
            }

            if (recordCode == "9" && ShouldUseRecord9MappingEngine(mode) && _recordMappingEngine is not null && _mappingPlanCompiler is not null)
            {
                var mapped = await TryRenderRecord9WithMappingEngineAsync(recordCode, record, layoutVariant, mode, layoutCache, context, audit, ct);
                if (!string.IsNullOrWhiteSpace(mapped))
                {
                    audit.Trace.Add("R9:MAPPING_ENGINE_APPLIED:BASE_OBJECT=FileControlRecord.From");
                    sb.Append(mapped);
                    lineCount++;
                    continue;
                }

                audit.Warnings.Add("RecordCode=9 usó fallback legado por cobertura insuficiente del mapping engine.");
                audit.Trace.Add("R9:MAPPING_ENGINE_FALLBACK:BASE_OBJECT=FileControlRecord.From");
            }

            var rendered = await RenderWithResolvedLayoutAsync(recordCode, record, layoutVariant);
            if (mode == "SHADOW_COMPARE" && layoutCache.TryGetValue(recordCode, out var legacyLayout))
            {
                var legacyRendered = record is IReadOnlyDictionary<string, object?> shadowDict
                    ? await _recordRenderer.RenderRecordAsync(recordCode, shadowDict, legacyLayout)
                    : await _recordRenderer.RenderRecordAsync(recordCode, record, legacyLayout);

                var diff = CompareRenderedLines(recordCode, legacyRendered, rendered);
                if (!string.IsNullOrWhiteSpace(diff))
                {
                    audit.EquivalenceDiffs.Add(diff);
                    audit.Warnings.Add($"Diferencia detectada en shadow compare para RecordCode={recordCode}.");
                    RegisterShadowDiff(audit, diff);
                }
            }

            EnsureRecordLength(recordCode, rendered, layoutVariant.TotalLength);
            sb.Append(rendered);
            lineCount++;
        }

        if (!audit.NewEngineRecordCodes.Contains(recordCode))
        {
            audit.NewEngineRecordCodes.Add(recordCode);
        }

        return lineCount;
    }

    private async Task<int> AppendType7ResolvedOrLegacyAsync(
        StringBuilder sb,
        NachaRecordDefinition definition,
        IReadOnlyDictionary<string, NachaRecordLayout> layoutCache,
        NachaConfigResolutionResult resolution,
        NachaGenerationAuditResult audit,
        IReadOnlyList<NachaType7RecordCandidate> candidates,
        NachaBuildContext context,
        CancellationToken ct)
    {
        var mode = (_generationOptions.Mode ?? "LEGACY").Trim().ToUpperInvariant();
        var hasLayout = resolution.LayoutsByRecordCode.TryGetValue("7", out var layoutVariant);
        audit.Type7LayoutVariantCode = layoutVariant?.VariantCode;
        audit.Type7TotalCandidates += candidates.Count;
        var shouldUseTableDriven = _generationOptions.EnableType7TableDriven
                                   && mode is "HYBRID" or "TABLE_DRIVEN" or "SHADOW_COMPARE"
                                   && _configResolver is not null
                                   && hasLayout;

        var forcedTableDrivenByLayout = layoutVariant is not null &&
                                        _generationOptions.Type7DisableLegacyFallbackForLayouts
                                            .Any(x => string.Equals(x, layoutVariant.VariantCode, StringComparison.OrdinalIgnoreCase));

        if (_generationOptions.Type7EnableTableDrivenForClearingHouses.Count > 0)
        {
            var clearingHouseCode = !string.IsNullOrWhiteSpace(audit.ClearingHouseCode)
                ? audit.ClearingHouseCode
                : await ResolveClearingHouseCodeAsync(context, ct);
            var allowForClearingHouse = _generationOptions.Type7EnableTableDrivenForClearingHouses
                .Any(x => string.Equals(x, clearingHouseCode, StringComparison.OrdinalIgnoreCase));
            shouldUseTableDriven &= allowForClearingHouse;
            if (!allowForClearingHouse)
            {
                IncrementCounter(audit.Type7FallbackReasons, $"ClearingHouseNotEnabled:{clearingHouseCode}");
            }
        }

        var rolloutDecision = _type7RolloutPolicy is null
            ? new NachaType7RolloutDecision { AllowLegacyFallback = true, Reasons = ["RolloutPolicyNotConfigured"] }
            : await _type7RolloutPolicy.EvaluateAsync(audit.ClearingHouseCode ?? "ACH", layoutVariant, mode, ct);

        audit.Trace.Add($"Type7RolloutDecision:Eligible={rolloutDecision.EligibleToDisableFallback};AllowFallback={rolloutDecision.AllowLegacyFallback};Runs={rolloutDecision.QualifiedRuns};Equivalence={rolloutDecision.EquivalenceRatePercent:0.00};Reasons={string.Join(',', rolloutDecision.Reasons)}");

        if (!shouldUseTableDriven || layoutVariant is null)
        {
            if (!audit.LegacyRecordCodes.Contains("7"))
            {
                audit.LegacyRecordCodes.Add("7");
            }

            audit.Warnings.Add("Fallback legado para RecordCode=7 por cobertura/configuración insuficiente.");
            IncrementCounter(audit.Type7FallbackReasons, "NoLayoutOrModeDisabled");
            IncrementCounter(audit.Type7FallbackByLayout, layoutVariant?.VariantCode ?? "NO_LAYOUT");
            audit.Type7GeneratedLegacy += candidates.Count;
            if (!rolloutDecision.AllowLegacyFallback)
            {
                throw new InvalidOperationException($"Rollout policy bloqueó fallback type7. Razones: {string.Join(", ", rolloutDecision.Reasons)}");
            }
            return AppendType7Legacy(sb, candidates);
        }

        var lineCount = 0;
        foreach (var candidate in candidates)
        {
            var alignedValues = AlignType7ValuesWithLayout(layoutVariant, candidate.FieldValues, audit);
            var rendered = await TryRenderType7WithMappingEngineAsync(layoutVariant, alignedValues, mode, layoutCache, candidate, audit, ct);
            if (string.IsNullOrWhiteSpace(rendered))
            {
                var renderer = _type7LegacyRenderer ?? new NachaType7LegacyRenderer();
                if (!rolloutDecision.AllowLegacyFallback)
                {
                    throw new InvalidOperationException("Rollout policy bloqueó fallback type7 para un candidato; el identificador financiero fue omitido.");
                }

                rendered = renderer.Render(candidate.Batch, candidate.Transaction, candidate.Addenda);
                audit.Type7GeneratedLegacy++;
                IncrementCounter(audit.Type7FallbackReasons, "MappingEngineFallbackToLegacy");
                IncrementCounter(audit.Type7FallbackByLayout, layoutVariant.VariantCode);
            }
            else
            {
                audit.Type7GeneratedTableDriven++;
            }

            EnsureRecordLength("7", rendered, layoutVariant.TotalLength);
            sb.Append(rendered);
            lineCount++;

            if (mode == "SHADOW_COMPARE")
            {
                var legacy = (_type7LegacyRenderer ?? new NachaType7LegacyRenderer())
                    .Render(candidate.Batch, candidate.Transaction, candidate.Addenda);
                var diff = CompareRenderedLines("7", legacy, rendered);
                if (!string.IsNullOrWhiteSpace(diff))
                {
                    audit.EquivalenceDiffs.Add(diff);
                    audit.Warnings.Add("Diferencia detectada en SHADOW_COMPARE para RecordCode=7.");
                    RegisterShadowDiff(audit, diff);
                }

                foreach (var fieldDiff in BuildType7FieldDiffs(layoutVariant, legacy, rendered))
                {
                    audit.EquivalenceDiffs.Add(fieldDiff);
                    IncrementCounter(audit.Type7DiffByField, fieldDiff.Split(':')[0]);
                }
            }
        }

        if ((forcedTableDrivenByLayout || !rolloutDecision.AllowLegacyFallback) && audit.Type7GeneratedLegacy > 0)
        {
            throw new InvalidOperationException($"Fallback legado deshabilitado para layout {layoutVariant.VariantCode}.");
        }

        if (!audit.NewEngineRecordCodes.Contains("7"))
        {
            audit.NewEngineRecordCodes.Add("7");
        }

        return lineCount;
    }

    private async Task<string?> TryRenderType7WithMappingEngineAsync(
        CfgLayoutVariant layoutVariant,
        IReadOnlyDictionary<string, object?> alignedValues,
        string mode,
        IReadOnlyDictionary<string, NachaRecordLayout> layoutCache,
        NachaType7RecordCandidate candidate,
        NachaGenerationAuditResult audit,
        CancellationToken ct)
    {
        if (!_generationOptions.EnableType7CommonMappingEngine || _recordMappingEngine is null || _mappingPlanCompiler is null)
        {
            return await RenderWithResolvedLayoutAsync("7", alignedValues, layoutVariant);
        }

        var issues = new List<string>();
        var plan = _mappingPlanCompiler.CompileRecordPlan(layoutVariant, issues);
        if (issues.Count > 0)
        {
            audit.Warnings.AddRange(issues.Select(x => $"Type7PlanIssue:{x}"));
        }

        var mapped = await _recordMappingEngine.MapRecordAsync(new RecordMappingRequest
        {
            RecordCode = "7",
            SourceRecord = alignedValues,
            RecordPlan = plan,
            ContextValues = new Dictionary<string, object?>(alignedValues, StringComparer.OrdinalIgnoreCase),
            EnableDiagnostics = _generationOptions.Record6MappingDiagnostics,
            ShadowCompare = string.Equals(mode, "SHADOW_COMPARE", StringComparison.OrdinalIgnoreCase),
            LegacyLayout = layoutCache.TryGetValue("7", out var legacyLayout) ? legacyLayout : null
        }, ct);

        foreach (var trace in mapped.FieldTraces.Take(_generationOptions.Record6MappingDiagnostics ? mapped.FieldTraces.Count : 5))
        {
            audit.Trace.Add($"R7:{trace.FieldCode}:SRC={trace.SourceUsed}:CAN={trace.CanonicalKey}:FB={trace.FallbackStrategy};VALUES=REDACTED");
        }

        if (!mapped.Success && mapped.ValuesByFieldCode.Count == 0)
        {
            audit.Warnings.Add("Type7 mapping engine devolvió fallback; identificadores y valores omitidos.");
            return null;
        }

        var mappedLayout = new NachaRecordLayout
        {
            RecordCode = "7",
            TotalLength = layoutVariant.TotalLength,
            Fields = layoutVariant.Fields
                .OrderBy(x => x.StartPosition)
                .Select(x => new NachaRecordField
                {
                    FieldName = x.FieldCode,
                    DbColumn = x.FieldCode,
                    StartPosition = x.StartPosition,
                    Length = x.Length,
                    Justification = x.Justification,
                    PadChar = x.PadChar,
                    Format = x.FormatMask
                })
                .ToList()
        };

        return await _recordRenderer.RenderRecordAsync("7", mapped.ValuesByFieldCode, mappedLayout);
    }

    private IReadOnlyDictionary<string, object?> AlignType7ValuesWithLayout(
        CfgLayoutVariant layoutVariant,
        IReadOnlyDictionary<string, object?> values,
        NachaGenerationAuditResult audit)
    {
        if (_type7AliasMap is null)
        {
            return values;
        }

        var aligned = new Dictionary<string, object?>(values, StringComparer.OrdinalIgnoreCase);
        foreach (var field in layoutVariant.Fields.Where(f => f.IsEnabled))
        {
            var rawPath = field.SourceDefinition.PropertyPath;
            if (string.IsNullOrWhiteSpace(rawPath))
            {
                continue;
            }

            var canonical = _type7AliasMap.GetCanonicalKey(rawPath);
            if (aligned.TryGetValue(canonical, out var value))
            {
                aligned[rawPath] = value;
                aligned[field.FieldCode] = value;
                audit.Type7AliasResolutionTrace.Add($"{field.FieldCode}:{rawPath}->{canonical}");
                continue;
            }

            if (!aligned.ContainsKey(rawPath))
            {
                IncrementCounter(audit.Type7FallbackReasons, $"MissingField:{field.FieldCode}");
            }
        }

        return aligned;
    }

    private static IEnumerable<string> BuildType7FieldDiffs(CfgLayoutVariant layoutVariant, string legacy, string generated)
    {
        foreach (var field in layoutVariant.Fields.Where(x => x.IsEnabled).OrderBy(x => x.StartPosition))
        {
            var start = field.StartPosition - 1;
            if (start < 0 || start + field.Length > legacy.Length || start + field.Length > generated.Length)
            {
                yield return $"{field.FieldCode}:LONGITUD:{legacy.Length}->{generated.Length}";
                continue;
            }

            var legacyValue = legacy.Substring(start, field.Length);
            var newValue = generated.Substring(start, field.Length);
            if (string.Equals(legacyValue, newValue, StringComparison.Ordinal))
            {
                continue;
            }

            var classification = ClassifyFieldDifference(legacyValue, newValue);
            yield return $"{field.FieldCode}:{classification}:LEGACY='{legacyValue}'|NEW='{newValue}'";
        }
    }

    private static string ClassifyFieldDifference(string legacyValue, string newValue)
    {
        if (legacyValue.Trim() == newValue.Trim())
        {
            return "PADDING";
        }

        if (legacyValue.Length != newValue.Length)
        {
            return "LONGITUD";
        }

        if (legacyValue.All(char.IsDigit) && newValue.All(char.IsDigit))
        {
            return "FORMATO";
        }

        return "VALOR";
    }

    private static void IncrementCounter(Dictionary<string, int> map, string key)
    {
        map[key] = map.TryGetValue(key, out var current) ? current + 1 : 1;
    }

    private int AppendType7Legacy(StringBuilder sb, IReadOnlyList<NachaType7RecordCandidate> candidates)
    {
        var renderer = _type7LegacyRenderer ?? new NachaType7LegacyRenderer();
        var count = 0;
        foreach (var candidate in candidates)
        {
            sb.Append(renderer.Render(candidate.Batch, candidate.Transaction, candidate.Addenda));
            count++;
        }

        return count;
    }

    private IReadOnlyList<NachaType7RecordCandidate> BuildFallbackType7Candidates(IReadOnlyList<AchBatch> orderedBatches)
    {
        var fallbackStrategy = new NachaType7GenerationStrategy(new NachaType7FieldValueResolver(new NachaType7AliasMap()));
        return fallbackStrategy.BuildCandidates(orderedBatches);
    }

    private async Task<string> RenderWithResolvedLayoutAsync(string recordCode, object record, CfgLayoutVariant layoutVariant)
    {
        var mappedLayout = new NachaRecordLayout
        {
            RecordCode = recordCode,
            TotalLength = layoutVariant.TotalLength,
            Fields = layoutVariant.Fields
                .OrderBy(x => x.StartPosition)
                .Select(x => new NachaRecordField
                {
                    FieldName = x.FieldCode,
                    DbColumn = ResolveDbColumnAlias(x.SourceDefinition)!,
                    StartPosition = x.StartPosition,
                    Length = x.Length,
                    Justification = x.Justification,
                    PadChar = x.PadChar,
                    Format = x.FormatMask
                })
                .ToList()
        };

        if (record is IReadOnlyDictionary<string, object?> dict)
        {
            return await _recordRenderer.RenderRecordAsync(recordCode, dict, mappedLayout);
        }

        return await _recordRenderer.RenderRecordAsync(recordCode, record, mappedLayout);
    }

    private static string? ResolveDbColumnAlias(CfgFieldSourceDefinition source)
    {
        if (source.DataSourceType.Code == "CONSTANTE")
        {
            return $"CONST:{source.ConstantValue}";
        }

        return source.PropertyPath;
    }

    private bool IsOfficialTableDrivenMode()
        => string.Equals((_generationOptions.Mode ?? "TABLE_DRIVEN").Trim(), "TABLE_DRIVEN", StringComparison.OrdinalIgnoreCase);

    private async Task<NachaConfigResolutionResult> ResolveOfficialRuntimeConfigAsync(
        NachaBuildContext context,
        string clearingHouseCode,
        IReadOnlyCollection<string> recordCodes,
        INachaChamberFlowPolicy chamberPolicy,
        CancellationToken ct)
    {
        if (_configResolver is null)
        {
            throw new NachaGenerationException("NACHA_PROFILE_NOT_PUBLISHED", "Resolver NACHA table-driven no está registrado. No se habilita fallback legacy.");
        }

        var request = BuildConfigResolutionRequest(context, clearingHouseCode, recordCodes);
        var resolution = await _configResolver.ResolvePublishedOrdinaryAsync(request, ct);
        if (context.SelectedProfileId.HasValue
            && (resolution.Profile?.Id != context.SelectedProfileId
                || resolution.Profile.ProfileCode != context.SelectedProfileCode
                || resolution.Profile.VersionMajor != context.SelectedVersionMajor
                || resolution.Profile.VersionMinor != context.SelectedVersionMinor))
        {
            throw new NachaGenerationException(
                "NACHA_PUBLISH_IDENTITY_MISMATCH",
                "La autoridad PUBLISH del archivo no coincide con la política seleccionada para su partición.");
        }
        if (resolution.SelectionStatus == NachaProfileSelectionStatus.ProfileAmbiguous
            || resolution.Warnings.Any(x => x.Contains("Ambig", StringComparison.OrdinalIgnoreCase)))
        {
            throw new NachaGenerationException("NACHA_PROFILE_AMBIGUOUS", string.Join(" | ", resolution.Warnings));
        }

        if (!resolution.Success || resolution.Profile is null)
        {
            if (resolution.Profile is not null
                && resolution.SelectionStatus == NachaProfileSelectionStatus.ProfileNotFound
                && resolution.Warnings.Any(x => x.Contains("RecordCode=", StringComparison.OrdinalIgnoreCase)))
            {
                throw new NachaGenerationException(
                    "NACHA_REQUIRED_RECORD_MISSING",
                    string.Join(" | ", resolution.Warnings));
            }

            var code = await ResolveProfileFailureCodeAsync(request, ct);
            throw new NachaGenerationException(code, $"No existe perfil NACHA-M publicado/vigente para {request.ClearingHouseCode}/{request.FlowTypeCode}/{request.DirectionCode}.");
        }

        if (resolution.UsedFallback)
        {
            var missing = recordCodes.Where(code => !resolution.LayoutsByRecordCode.ContainsKey(code)).ToArray();
            if (missing.Length > 0)
            {
                throw new NachaGenerationException("NACHA_REQUIRED_RECORD_MISSING", $"Faltan layout variants publicados para RecordCode={string.Join(",", missing)}.");
            }

            throw new NachaGenerationException("NACHA_LEGACY_GENERATION_DISABLED", "El resolver solicitó fallback legacy en modo oficial table-driven.");
        }

        chamberPolicy.EnforceResolvedProfileGate(_generationOptions, resolution.Profile);

        return resolution;
    }

    private NachaConfigResolutionRequest BuildConfigResolutionRequest(
        NachaBuildContext context,
        string clearingHouseCode,
        IReadOnlyCollection<string> recordCodes)
    {
        var serviceClassCode = context.StandardEntryClassCode
            ?? context.Batches.Select(x => x.ServiceClassCode)
                .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

        var flowTypeCode = NachaProfileDimensionResolver.ResolveFlowCode(context.Transactions);
        return new NachaConfigResolutionRequest
        {
            ClearingHouseCode = clearingHouseCode,
            FlowTypeCode = flowTypeCode,
            DirectionCode = NachaProfileDimensionResolver.ResolveDirectionCode(context.Transactions),
            ServiceClassCode = serviceClassCode,
            ProcessDateUtc = context.Cycle.ProcessingDate,
            RequestedVersionMajor = context.SelectedVersionMajor,
            RequestedVersionMinor = context.SelectedVersionMinor,
            RecordCodes = recordCodes.ToList(),
            SelectionContext = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["CycleName"] = context.Cycle.CycleName ?? string.Empty,
                ["ClearingHouseId"] = context.Cycle.ClearingHouseId.ToString(CultureInfo.InvariantCulture)
            }
        };
    }

    private async Task<string> ResolveProfileFailureCodeAsync(NachaConfigResolutionRequest request, CancellationToken ct)
    {
        var date = request.ProcessDateUtc.Date;
        var profiles = await _context.CfgProfiles
            .AsNoTracking()
            .Include(x => x.ClearingHouse)
            .Include(x => x.FlowType)
            .Include(x => x.Direction)
            .Include(x => x.ServiceClass)
            .Include(x => x.Status)
            .Where(x => x.ClearingHouse.Code == request.ClearingHouseCode
                        && x.FlowType.Code == request.FlowTypeCode
                        && x.Direction.Code == request.DirectionCode
                        && (x.ServiceClass == null || x.ServiceClass.Code == request.ServiceClassCode))
            .ToListAsync(ct);

        if (profiles.Any(x => string.Equals(x.Status.Code, "PUBLICADO", StringComparison.OrdinalIgnoreCase)
                              && (x.EffectiveFrom.Date > date || (x.EffectiveTo.HasValue && x.EffectiveTo.Value.Date < date))))
        {
            return "NACHA_PROFILE_NOT_EFFECTIVE";
        }

        return "NACHA_PROFILE_NOT_PUBLISHED";
    }

    private async Task<string> ResolveClearingHouseCodeAsync(NachaBuildContext context, CancellationToken ct)
        => await NachaClearingHouseProfileCodeResolver.ResolveAsync(_context, context.Cycle, ct);

    private static CfgLayoutVariant RequireOfficialLayout(NachaConfigResolutionResult resolution, string recordCode)
    {
        if (!resolution.LayoutsByRecordCode.TryGetValue(recordCode, out var layout))
        {
            throw new NachaGenerationException("NACHA_REQUIRED_RECORD_MISSING", $"Falta variant publicado para RecordCode={recordCode}.");
        }

        return layout;
    }

    private static void ValidateOfficialLayout(
        string recordCode,
        CfgLayoutVariant layout,
        INachaChamberFlowPolicy chamberPolicy,
        NachaGenerationAuditResult? audit = null)
    {
        ValidateMaterializedLayoutStructure(recordCode, layout, audit);
        chamberPolicy.ValidateOfficialLayoutSnapshot(recordCode, layout);
    }

    private static void ValidateMaterializedLayoutStructure(
        string recordCode,
        CfgLayoutVariant layout,
        NachaGenerationAuditResult? audit = null)
    {
        if (layout.TotalLength != FixedWidthRecordLength)
        {
            throw new NachaGenerationException(
                "NACHA_RECORD_LENGTH_INVALID",
                $"RecordCode={recordCode} debe tener longitud fija de {FixedWidthRecordLength} caracteres.");
        }

        var enabledFields = layout.Fields.Where(x => x.IsEnabled).OrderBy(x => x.StartPosition).ToList();
        if (enabledFields.Count == 0)
        {
            throw new NachaGenerationException("NACHA_REQUIRED_FIELD_MISSING", $"RecordCode={recordCode} no tiene campos activos.");
        }

        var requiredFields = recordCode switch
        {
            "1" => new[] { "RECORDTYPE", "IMMEDIATEDESTINATION", "IMMEDIATEORIGIN", "FILECREATIONDATE", "FILECREATIONTIME", "FILEIDMODIFIER" },
            "5" => new[] { "RECORDTYPE", "SERVICECLASSCODE", "COMPANYNAME", "COMPANYIDENTIFICATION", "COMPANYENTRYDESCRIPTION", "BATCHNUMBER" },
            "6" => new[] { "RECORDTYPE", "TRANSACTIONCODE", "RECEIVINGDFI", "CHECKDIGIT", "DFIACCOUNTNUMBER", "AMOUNT", "TRACENUMBER" },
            "7" => Array.Empty<string>(),
            "8" => new[] { "RECORDTYPE", "SERVICECLASSCODE", "ENTRYADDENDACOUNT", "ENTRYHASH", "TOTALDEBITAMOUNT", "TOTALCREDITAMOUNT", "BATCHNUMBER" },
            "9" => new[] { "RECORDTYPE", "BATCHCOUNT", "BLOCKCOUNT", "ENTRYADDENDACOUNT", "ENTRYHASH", "TOTALDEBITAMOUNT", "TOTALCREDITAMOUNT" },
            _ => Array.Empty<string>()
        };

        foreach (var required in requiredFields)
        {
            var field = enabledFields.FirstOrDefault(x => string.Equals(x.FieldCode, required, StringComparison.OrdinalIgnoreCase));
            if (field is null)
            {
                audit?.FieldTraceEntries.Add(new NachaGenerationTraceEntry
                {
                    TraceId = audit.TraceId,
                    RecordType = recordCode,
                    FieldName = required,
                    DisplayName = required,
                    ValidationStatus = "Failed",
                    ErrorCode = "NACHA_REQUIRED_FIELD_MISSING",
                    ErrorMessage = $"Falta el campo requerido {required} en RecordCode={recordCode}."
                });
                throw new NachaGenerationException("NACHA_REQUIRED_FIELD_MISSING", $"Falta el campo requerido {required} en RecordCode={recordCode}.");
            }
        }

        var occupied = new HashSet<int>();
        foreach (var field in enabledFields)
        {
            if (field.Length <= 0 || field.StartPosition <= 0 || field.StartPosition + field.Length - 1 > layout.TotalLength)
            {
                throw new NachaGenerationException("NACHA_FIELD_VALIDATION_FAILED", $"Campo {field.FieldCode} tiene posición/longitud inválida en RecordCode={recordCode}.");
            }

            for (var position = field.StartPosition; position < field.StartPosition + field.Length; position++)
            {
                if (!occupied.Add(position))
                {
                    throw new NachaGenerationException("NACHA_FIELD_VALIDATION_FAILED", $"Campo {field.FieldCode} se solapa en RecordCode={recordCode}.");
                }
            }

            var source = field.SourceDefinition;
            var sourceType = source?.DataSourceType?.Code;
            if (source is null || string.IsNullOrWhiteSpace(sourceType))
            {
                throw new NachaGenerationException("NACHA_FIELD_SOURCE_NOT_FOUND", $"Campo {field.FieldCode} no tiene source definition.");
            }

            if (string.Equals(sourceType, "CONSTANTE", StringComparison.OrdinalIgnoreCase) && source.ConstantValue is null)
            {
                throw new NachaGenerationException("NACHA_FIELD_SOURCE_NOT_FOUND", $"Campo constante {field.FieldCode} no tiene valor.");
            }

            if (string.Equals(sourceType, "ENTIDAD", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(source.PropertyPath))
            {
                throw new NachaGenerationException("NACHA_FIELD_SOURCE_NOT_FOUND", $"Campo entidad {field.FieldCode} no tiene sourceFieldPath.");
            }

            if (string.Equals(sourceType, "EXPRESION", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(source.ExpressionDsl))
            {
                throw new NachaGenerationException("NACHA_CALCULATION_FAILED", $"Campo calculado {field.FieldCode} no tiene calculationType.");
            }
        }

    }

    private async Task<NachaConfigResolutionResult> ResolveRuntimeConfigAsync(
        NachaBuildContext context,
        IReadOnlyList<NachaRecordDefinition> definitions,
        string clearingHouseCode,
        CancellationToken ct)
    {
        if (_configResolver is null)
        {
            return new NachaConfigResolutionResult
            {
                Success = false,
                UsedFallback = true,
                Warnings = ["Resolver de configuración NACHA no está registrado."],
                Trace = []
            };
        }

        var flow = NachaProfileDimensionResolver.ResolveFlowCode(context.Transactions);
        var direction = NachaProfileDimensionResolver.ResolveDirectionCode(context.Transactions);
        var serviceClassCode = context.Batches
            .Select(x => x.ServiceClassCode)
            .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));

        var request = new NachaConfigResolutionRequest
        {
            ClearingHouseCode = clearingHouseCode,
            FlowTypeCode = flow,
            DirectionCode = direction,
            ServiceClassCode = serviceClassCode,
            ProcessDateUtc = context.Cycle.ProcessingDate,
            RecordCodes = definitions.Where(x => x.IsEnabled).Select(x => x.RecordCode).ToList(),
            SelectionContext = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["CycleName"] = context.Cycle.CycleName ?? string.Empty,
                ["ClearingHouseId"] = context.Cycle.ClearingHouseId.ToString()
            }
        };

        var resolution = await _configResolver.ResolveAsync(request, ct);
        if (_generationOptions.FailOnResolverAmbiguity && resolution.Warnings.Any(x => x.Contains("Ambigüedad", StringComparison.OrdinalIgnoreCase)))
        {
            throw new InvalidOperationException($"Resolver NACHA encontró ambigüedad: {string.Join(" | ", resolution.Warnings)}");
        }

        return resolution;
    }

    private async Task<string?> TryRenderRecord6WithMappingEngineAsync(
        string recordCode,
        object record,
        CfgLayoutVariant layoutVariant,
        string mode,
        IReadOnlyDictionary<string, NachaRecordLayout> layoutCache,
        NachaGenerationAuditResult audit,
        CancellationToken ct)
    {
        if (_recordMappingEngine is null || _mappingPlanCompiler is null)
        {
            return null;
        }

        var issues = new List<string>();
        var plan = _mappingPlanCompiler.CompileRecordPlan(layoutVariant, issues);
        if (issues.Count > 0)
        {
            audit.Warnings.AddRange(issues.Select(x => $"Record6PlanIssue:{x}"));
        }

        var contextValues = BuildRecord6ContextValues(record);
        var mapped = await _recordMappingEngine.MapRecordAsync(new RecordMappingRequest
        {
            RecordCode = recordCode,
            SourceRecord = record,
            RecordPlan = plan,
            ContextValues = contextValues,
            EnableDiagnostics = _generationOptions.Record6MappingDiagnostics,
            ShadowCompare = string.Equals(mode, "SHADOW_COMPARE", StringComparison.OrdinalIgnoreCase),
            LegacyLayout = layoutCache.TryGetValue(recordCode, out var legacyLayout) ? legacyLayout : null
        }, ct);

        foreach (var trace in mapped.FieldTraces.Take(_generationOptions.Record6MappingDiagnostics ? mapped.FieldTraces.Count : 5))
        {
            var transforms = trace.TransformSteps.Count == 0 ? "none" : string.Join(",", trace.TransformSteps);
            var issuesText = trace.ValidationIssues.Count == 0
                ? "none"
                : string.Join("|", trace.ValidationIssues.Select(x => $"{x.RuleCode}:{x.Severity}"));
            audit.Trace.Add($"R6:{trace.FieldCode}:SRC={trace.SourceUsed}:CAN={trace.CanonicalKey}:VALUES=REDACTED:TF={transforms}:RULES={issuesText}:FB={trace.FallbackStrategy}");
        }

        if (!mapped.Success && mapped.ValuesByFieldCode.Count == 0)
        {
            return null;
        }

        var mappedLayout = new NachaRecordLayout
        {
            RecordCode = recordCode,
            TotalLength = layoutVariant.TotalLength,
            Fields = layoutVariant.Fields
                .OrderBy(x => x.StartPosition)
                .Select(x => new NachaRecordField
                {
                    FieldName = x.FieldCode,
                    DbColumn = x.FieldCode,
                    StartPosition = x.StartPosition,
                    Length = x.Length,
                    Justification = x.Justification,
                    PadChar = x.PadChar,
                    Format = x.FormatMask
                })
                .ToList()
        };

        var rendered = await _recordRenderer.RenderRecordAsync(recordCode, mapped.ValuesByFieldCode, mappedLayout);
        if (mode == "SHADOW_COMPARE" && layoutCache.TryGetValue(recordCode, out var legacy))
        {
            var legacyRendered = await _recordRenderer.RenderRecordAsync(recordCode, record, legacy);
            var diff = CompareRenderedLines(recordCode, legacyRendered, rendered);
            if (!string.IsNullOrWhiteSpace(diff))
            {
                audit.EquivalenceDiffs.Add($"R6_MAPPING:{diff}");
                audit.Warnings.Add("Diferencia SHADOW_COMPARE en record 6 mapping engine.");
                RegisterShadowDiff(audit, $"R6_MAPPING:{diff}");
            }
        }

        EnsureRecordLength(recordCode, rendered, mappedLayout.TotalLength);
        return rendered;
    }

    private Task<string?> TryRenderRecord1WithMappingEngineAsync(
        string recordCode,
        object record,
        CfgLayoutVariant layoutVariant,
        string mode,
        IReadOnlyDictionary<string, NachaRecordLayout> layoutCache,
        NachaBuildContext context,
        NachaGenerationAuditResult audit,
        CancellationToken ct)
    {
        return TryRenderHeaderWithMappingEngineAsync(recordCode, record, layoutVariant, mode, layoutCache, context, audit, ct);
    }

    private Task<string?> TryRenderRecord5WithMappingEngineAsync(
        string recordCode,
        object record,
        CfgLayoutVariant layoutVariant,
        string mode,
        IReadOnlyDictionary<string, NachaRecordLayout> layoutCache,
        NachaBuildContext context,
        NachaGenerationAuditResult audit,
        CancellationToken ct)
    {
        return TryRenderHeaderWithMappingEngineAsync(recordCode, record, layoutVariant, mode, layoutCache, context, audit, ct);
    }

    private Task<string?> TryRenderRecord8WithMappingEngineAsync(
        string recordCode,
        object record,
        CfgLayoutVariant layoutVariant,
        string mode,
        IReadOnlyDictionary<string, NachaRecordLayout> layoutCache,
        NachaBuildContext context,
        NachaGenerationAuditResult audit,
        CancellationToken ct)
    {
        return TryRenderHeaderWithMappingEngineAsync(recordCode, record, layoutVariant, mode, layoutCache, context, audit, ct);
    }

    private Task<string?> TryRenderRecord9WithMappingEngineAsync(
        string recordCode,
        object record,
        CfgLayoutVariant layoutVariant,
        string mode,
        IReadOnlyDictionary<string, NachaRecordLayout> layoutCache,
        NachaBuildContext context,
        NachaGenerationAuditResult audit,
        CancellationToken ct)
    {
        return TryRenderHeaderWithMappingEngineAsync(recordCode, record, layoutVariant, mode, layoutCache, context, audit, ct);
    }

    private async Task<string?> TryRenderHeaderWithMappingEngineAsync(
        string recordCode,
        object record,
        CfgLayoutVariant layoutVariant,
        string mode,
        IReadOnlyDictionary<string, NachaRecordLayout> layoutCache,
        NachaBuildContext context,
        NachaGenerationAuditResult audit,
        CancellationToken ct)
    {
        if (_recordMappingEngine is null || _mappingPlanCompiler is null)
        {
            return null;
        }

        var issues = new List<string>();
        var plan = _mappingPlanCompiler.CompileRecordPlan(layoutVariant, issues);
        if (issues.Count > 0)
        {
            audit.Warnings.AddRange(issues.Select(x => $"Record{recordCode}PlanIssue:{x}"));
        }

        var contextValues = BuildHeaderContextValues(record, context);
        var mapped = await _recordMappingEngine.MapRecordAsync(new RecordMappingRequest
        {
            RecordCode = recordCode,
            SourceRecord = record,
            RecordPlan = plan,
            ContextValues = contextValues,
            EnableDiagnostics = _generationOptions.Record6MappingDiagnostics,
            ShadowCompare = string.Equals(mode, "SHADOW_COMPARE", StringComparison.OrdinalIgnoreCase),
            LegacyLayout = layoutCache.TryGetValue(recordCode, out var legacyLayout) ? legacyLayout : null
        }, ct);

        foreach (var trace in mapped.FieldTraces.Take(_generationOptions.Record6MappingDiagnostics ? mapped.FieldTraces.Count : 5))
        {
            var transforms = trace.TransformSteps.Count == 0 ? "none" : string.Join(",", trace.TransformSteps);
            audit.Trace.Add($"R{recordCode}:{trace.FieldCode}:SRC={trace.SourceUsed}:CAN={trace.CanonicalKey}:VALUES=REDACTED:TF={transforms}:FB={trace.FallbackStrategy}");
        }

        if (!mapped.Success && mapped.ValuesByFieldCode.Count == 0)
        {
            return null;
        }

        var mappedLayout = new NachaRecordLayout
        {
            RecordCode = recordCode,
            TotalLength = layoutVariant.TotalLength,
            Fields = layoutVariant.Fields
                .OrderBy(x => x.StartPosition)
                .Select(x => new NachaRecordField
                {
                    FieldName = x.FieldCode,
                    DbColumn = x.FieldCode,
                    StartPosition = x.StartPosition,
                    Length = x.Length,
                    Justification = x.Justification,
                    PadChar = x.PadChar,
                    Format = x.FormatMask
                })
                .ToList()
        };

        var rendered = await _recordRenderer.RenderRecordAsync(recordCode, mapped.ValuesByFieldCode, mappedLayout);
        if (mode == "SHADOW_COMPARE" && layoutCache.TryGetValue(recordCode, out var legacy))
        {
            var legacyRendered = await _recordRenderer.RenderRecordAsync(recordCode, record, legacy);
            var diff = CompareRenderedLines(recordCode, legacyRendered, rendered);
            if (!string.IsNullOrWhiteSpace(diff))
            {
                audit.EquivalenceDiffs.Add($"R{recordCode}_MAPPING:{diff}");
                audit.Warnings.Add($"Diferencia SHADOW_COMPARE en record {recordCode} mapping engine.");
                RegisterShadowDiff(audit, $"R{recordCode}_MAPPING:{diff}");
            }
        }

        EnsureRecordLength(recordCode, rendered, mappedLayout.TotalLength);
        return rendered;
    }

    private bool ShouldUseRecord6MappingEngine(string mode)
    {
        if (!_generationOptions.EnableRecord6MappingEngine)
        {
            return false;
        }

        return mode is "HYBRID" or "TABLE_DRIVEN" or "SHADOW_COMPARE";
    }

    private bool ShouldUseRecord1MappingEngine(string mode)
    {
        if (!_generationOptions.EnableRecord1MappingEngine)
        {
            return false;
        }

        return mode is "HYBRID" or "TABLE_DRIVEN" or "SHADOW_COMPARE";
    }

    private bool ShouldUseRecord5MappingEngine(string mode)
    {
        if (!_generationOptions.EnableRecord5MappingEngine)
        {
            return false;
        }

        return mode is "HYBRID" or "TABLE_DRIVEN" or "SHADOW_COMPARE";
    }

    private bool ShouldUseRecord8MappingEngine(string mode)
    {
        if (!_generationOptions.EnableRecord8MappingEngine)
        {
            return false;
        }

        return mode is "HYBRID" or "TABLE_DRIVEN" or "SHADOW_COMPARE";
    }

    private bool ShouldUseRecord9MappingEngine(string mode)
    {
        if (!_generationOptions.EnableRecord9MappingEngine)
        {
            return false;
        }

        return mode is "HYBRID" or "TABLE_DRIVEN" or "SHADOW_COMPARE";
    }

    private static IReadOnlyDictionary<string, object?> BuildRecord6ContextValues(object record)
    {
        var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
        var type = record.GetType();
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            map[property.Name] = property.GetValue(record);
            map[$"ctx.{property.Name}"] = property.GetValue(record);
        }

        return map;
    }

    private static IReadOnlyDictionary<string, object?> BuildHeaderContextValues(object record, NachaBuildContext context)
    {
        var map = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["CycleName"] = context.Cycle.CycleName,
            ["ProcessingDateUtc"] = context.Cycle.ProcessingDate,
            ["ClearingHouseName"] = context.Cycle.ClearingHouse?.Name,
            ["ClearingHouseId"] = context.Cycle.ClearingHouseId,
            ["TransactionCount"] = context.Transactions.Count
        };

        var type = record.GetType();
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var value = property.GetValue(record);
            map[property.Name] = value;
            map[$"ctx.{property.Name}"] = value;
        }

        return map;
    }

    private static string? CompareRenderedLines(string recordCode, string legacyRendered, string newRendered)
    {
        if (string.Equals(legacyRendered, newRendered, StringComparison.Ordinal))
        {
            return null;
        }

        var max = Math.Min(legacyRendered.Length, newRendered.Length);
        var diffs = new List<string>(capacity: 3);
        var totalDiffs = 0;
        for (var i = 0; i < max; i++)
        {
            if (legacyRendered[i] != newRendered[i])
            {
                totalDiffs++;
                if (diffs.Count < 3)
                {
                    diffs.Add($"pos={i + 1}");
                }
            }
        }

        totalDiffs += Math.Abs(legacyRendered.Length - newRendered.Length);

        if (diffs.Count > 0)
        {
            return $"RecordCode={recordCode};DiffCount={totalDiffs};Diffs={string.Join("|", diffs)};LenLegacy={legacyRendered.Length};LenNew={newRendered.Length}";
        }

        return $"RecordCode={recordCode}, longitud legado={legacyRendered.Length}, longitud nuevo={newRendered.Length}";
    }

    private static void RegisterShadowDiff(NachaGenerationAuditResult audit, string diff)
    {
        audit.ShadowDiffCount++;
        if (audit.ShadowDiffDetails.Count < 200)
        {
            audit.ShadowDiffDetails.Add(diff);
        }
    }

    private static void EnsureRecordLength(string recordCode, string rendered, int expectedLength)
    {
        if (rendered.Length != expectedLength)
        {
            throw new InvalidOperationException($"RecordCode={recordCode} renderizó longitud {rendered.Length} y se esperaba {expectedLength}.");
        }
    }

    private async Task PersistGenerationAuditAsync(
        NachaGenerationAuditResult audit,
        int? profileId,
        CancellationToken ct,
        DateTime? capturedAtUtc = null)
    {
        if (!profileId.HasValue)
        {
            return;
        }

        try
        {
            var type7Compared = Math.Max(1, audit.Type7GeneratedTableDriven);
            var type7DiffCount = audit.Type7DiffByField.Values.Sum();
            var type7MatchRate = 100m * (type7Compared - Math.Min(type7Compared, type7DiffCount)) / type7Compared;
            audit.Trace.Add($"Type7Summary:Candidates={audit.Type7TotalCandidates};New={audit.Type7GeneratedTableDriven};Legacy={audit.Type7GeneratedLegacy};Diffs={type7DiffCount};MatchRate={type7MatchRate:0.00}%");
            audit.Trace.Add($"ShadowCompareSummary:DiffCount={audit.ShadowDiffCount};StoredDetails={audit.ShadowDiffDetails.Count}");

            _context.HistConfigChanges.Add(new Domain.Models.ACH.Config.HistConfigChange
            {
                ProfileId = profileId.Value,
                EntityName = "NachaFileBuilder",
                EntityId = Guid.NewGuid().ToString("N"),
                ChangeType = "GENERATION_TRACE",
                BeforeJson = null,
                AfterJson = JsonSerializer.Serialize(audit),
                ChangedAtUtc = capturedAtUtc ?? DateTime.UtcNow,
                ChangedBy = "system-runtime",
                CorrelationId = $"NACHA-GEN-{(capturedAtUtc ?? DateTime.UtcNow):yyyyMMddHHmmss}"
            });
            await _context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(
                "NACHA_GENERATION_AUDIT_PERSIST_FAILED ErrorType={ErrorType}",
                ex.GetType().Name);
        }
    }

    private static long SumBatchDebit(AchBatch batch)
    {
        return batch.Transactions
            .Where(tx => tx.Type is TransactionTypeEnum.Debit or TransactionTypeEnum.Return or TransactionTypeEnum.Reversal)
            .Sum(tx => (long)(tx.Amount * 100));
    }

    private static long SumBatchCredit(AchBatch batch)
    {
        return batch.Transactions
            .Where(tx => tx.Type is TransactionTypeEnum.Credit or TransactionTypeEnum.Prenotification)
            .Sum(tx => (long)(tx.Amount * 100));
    }

    private async Task<IReadOnlyList<NachaRecordDefinition>> LoadDefinitionsAsync(CancellationToken ct)
    {
        var definitions = await _context.NachaRecordDefinitions
            .AsNoTracking()
            .Where(d => d.IsEnabled)
            .OrderBy(d => d.Sequence)
            .ToListAsync(ct);

        return definitions.Count > 0 ? definitions : BuildDefaultDefinitions();
    }

    private static IReadOnlyList<NachaRecordDefinition> BuildDefaultDefinitions()
    {
        return new List<NachaRecordDefinition>
        {
            new()
            {
                RecordCode = "1",
                Sequence = 10,
                SourceType = NachaRecordSourceType.Custom,
                IsEnabled = true
            },
            new()
            {
                RecordCode = "5",
                Sequence = 20,
                SourceType = NachaRecordSourceType.Custom,
                IsEnabled = true
            },
            new()
            {
                RecordCode = "6",
                Sequence = 30,
                SourceType = NachaRecordSourceType.Custom,
                SourceName = nameof(AchTransaction),
                FilterKey = "BatchId",
                IsEnabled = true
            },
            new()
            {
                RecordCode = "7",
                Sequence = 40,
                SourceType = NachaRecordSourceType.Custom,
                SourceName = nameof(AchTransactionAddenda),
                FilterKey = "BatchId",
                IsEnabled = true
            },
            new()
            {
                RecordCode = "8",
                Sequence = 50,
                SourceType = NachaRecordSourceType.Custom,
                IsEnabled = true
            },
            new()
            {
                RecordCode = "9",
                Sequence = 60,
                SourceType = NachaRecordSourceType.Custom,
                IsEnabled = true
            }
        };
    }

    private async Task<NachaHeader?> LoadHeaderAsync(string cycleId, CancellationToken ct)
    {
        return await _context.NachaHeaders
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.AchCycleId == cycleId, ct);
    }

    private sealed record FileHeaderRecord
    {
        public string? PriorityCode { get; init; }
        public string? ImmediateDestination { get; init; }
        public string? ImmediateOrigin { get; init; }
        public DateTime FileCreationDate { get; init; }
        public DateTime FileCreationTime { get; init; }
        public string? FileIdModifier { get; init; }
        public string? RecordSize { get; init; }
        public string? BlockingFactor { get; init; }
        public string? FormatCode { get; init; }
        public string? ImmediateDestinationName { get; init; }
        public string? ImmediateOriginName { get; init; }
        public string? ReferenceCode { get; init; }
        public string? CycleName { get; init; }
        public DateTime ProcessingDate { get; init; }

        public static FileHeaderRecord From(
            AchCycle cycle,
            IReadOnlyCollection<AchTransaction> transactions,
            NachaHeader? header,
            OperationalTimeSnapshot operationalSnapshot,
            string? fileIdModifier = null)
        {
            var generationTimestamp = ResolveGenerationTimestamp(header, operationalSnapshot.BogotaTimestamp);
            var firstTransaction = transactions
                .OrderBy(t => t.Id)
                .FirstOrDefault();

            var destinationDfi = CoalesceNonEmpty(
                header?.ImmediateDestination,
                cycle.ClearingHouse?.OriginCode,
                firstTransaction?.ReceivingDFI);

            var originDfi = CoalesceNonEmpty(
                header?.ImmediateOrigin,
                firstTransaction?.OriginatingDFI);

            var destinationName = CoalesceNonEmpty(
                header?.ImmediateDestinationName,
                cycle.ClearingHouse?.Name,
                firstTransaction?.DestinationInstitution?.Name);

            var originName = CoalesceNonEmpty(
                header?.ImmediateOriginName,
                firstTransaction?.SourceInstitution?.Name);

            return new FileHeaderRecord
            {
                PriorityCode = CoalesceNonEmpty(header?.PriorityCode, "01"),
                ImmediateDestination = destinationDfi,
                ImmediateOrigin = originDfi,
                FileCreationDate = generationTimestamp,
                FileCreationTime = generationTimestamp,
                FileIdModifier = CoalesceNonEmpty(fileIdModifier, header?.FileIdModifier, "A"),
                RecordSize = string.IsNullOrWhiteSpace(header?.RecordSize) ? "106" : header!.RecordSize,
                BlockingFactor = string.IsNullOrWhiteSpace(header?.BlockingFactor) ? "10" : header!.BlockingFactor,
                FormatCode = string.IsNullOrWhiteSpace(header?.FormatCode) ? "1" : header!.FormatCode,
                ImmediateDestinationName = destinationName,
                ImmediateOriginName = originName,
                ReferenceCode = CoalesceNonEmpty(header?.ReferenceCode),
                CycleName = cycle.CycleName,
                ProcessingDate = operationalSnapshot.BogotaTimestamp
            };
        }

        public static FileHeaderRecord From(
            AchCycle cycle,
            IReadOnlyCollection<AchTransaction> transactions,
            NachaHeader? header,
            string? fileIdModifier = null)
        {
            var legacyTimestamp = NachaFileSnapshotTimeResolver.Resolve(cycle);
            var snapshot = new OperationalTimeSnapshot(
                legacyTimestamp.ToUniversalTime(),
                DateTime.SpecifyKind(legacyTimestamp, DateTimeKind.Unspecified),
                DateOnly.FromDateTime(cycle.ProcessingDate),
                OperationalTimeSnapshotProvider.IanaTimeZoneId);
            return From(cycle, transactions, header, snapshot, fileIdModifier);
        }

        private static string? CoalesceNonEmpty(params string?[] values)
        {
            return values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();
        }

        private static DateTime ResolveGenerationTimestamp(NachaHeader? header, DateTime fallback)
        {
            var date = fallback.Date;
            var time = fallback.TimeOfDay;

            if (!string.IsNullOrWhiteSpace(header?.FileCreationDate))
            {
                if (!DateTime.TryParseExact(
                        header.FileCreationDate.Trim(),
                        "yyyyMMdd",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var parsedDate))
                {
                    throw new NachaGenerationException(
                        "NACHA_FIELD_RULE_FAILED",
                        "La fecha de creación no cumple el formato configurado.",
                        ruleId: "ACHCOL-T1-FILE-CREATION-DATE",
                        chamber: "ACHCOL",
                        recordType: "1",
                        fieldName: "FILECREATIONDATE",
                        cause: "Formato distinto de yyyyMMdd.",
                        startPosition: 24,
                        expectedLength: 8);
                }

                date = parsedDate.Date;
            }

            if (!string.IsNullOrWhiteSpace(header?.FileCreationTime))
            {
                if (!DateTime.TryParseExact(
                        header.FileCreationTime.Trim(),
                        "HHmm",
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.None,
                        out var parsedTime))
                {
                    throw new NachaGenerationException(
                        "NACHA_FIELD_RULE_FAILED",
                        "La hora de creación no cumple el formato configurado.",
                        ruleId: "ACHCOL-T1-FILE-CREATION-TIME",
                        chamber: "ACHCOL",
                        recordType: "1",
                        fieldName: "FILECREATIONTIME",
                        cause: "Formato distinto de HHmm.",
                        startPosition: 32,
                        expectedLength: 4);
                }

                time = parsedTime.TimeOfDay;
            }

            return DateTime.SpecifyKind(date.Add(time), fallback.Kind);
        }
    }

    private sealed record CompanyEntryDescriptionCatalogItem(string Term, string StandardEntryClassCode);
    private sealed record BatchCalculation(
        IReadOnlyList<AchTransaction> Transactions,
        string StandardEntryClassCode,
        string BatchEntryDescription,
        int EntryAddendaCount = 0,
        int AddendaOnlyCount = 0,
        long BatchDebit = 0,
        long BatchCredit = 0);

    private sealed record ReceiverLookup(
        IReadOnlyDictionary<(string Document, string Account), IReadOnlyList<Customer>> CustomersByDocumentAndAccount,
        IReadOnlyDictionary<string, List<Customer>> CustomersByAccount)
    {
        public static readonly ReceiverLookup Empty = new(
            new Dictionary<(string Document, string Account), IReadOnlyList<Customer>>(),
            new Dictionary<string, List<Customer>>(StringComparer.Ordinal));
    }

    private static string ResolveBatchEntryDescription(AchBatch batch, IReadOnlyCollection<AchTransaction> batchTransactions)
    {
        var description = (batch.CompanyEntryDescription ?? string.Empty).Trim().ToUpperInvariant();
        var creditLikeCount = batchTransactions.Count(tx => tx.Type is TransactionTypeEnum.Credit or TransactionTypeEnum.Prenotification);
        return creditLikeCount > 1 ? "MULTICREDIT" : description;
    }


    private sealed record BatchHeaderRecord
    {
        public string ServiceClassCode { get; init; } = string.Empty;
        public string CompanyName { get; init; } = string.Empty;
        public string CompanyDiscretionaryData { get; init; } = string.Empty;
        public string CompanyIdentification { get; init; } = string.Empty;
        public string StandardEntryClassCode { get; init; } = "PPD";
        public string CompanyEntryDescription { get; init; } = string.Empty;
        public DateTime CompanyDescriptiveDate { get; init; }
        public DateTime EffectiveEntryDate { get; init; }
        public string SettlementDate { get; init; } = string.Empty;
        public string OriginatorStatusCode { get; init; } = "1";
        public string OriginatingDFI { get; init; } = string.Empty;
        public int BatchNumber { get; init; }

        public static BatchHeaderRecord From(
            AchBatch batch,
            string standardEntryClassCode,
            int batchNumber,
            string companyEntryDescription,
            OperationalTimeSnapshot operationalSnapshot)
        {
            if (standardEntryClassCode is not ("PPD" or "CCD" or "CTX"))
            {
                throw new InvalidOperationException("Error Fatal ID 20: Tipo de servicio del lote inválido. Solo se permite PPD, CCD o CTX.");
            }

            return new BatchHeaderRecord
            {
                ServiceClassCode = batch.ServiceClassCode,
                CompanyName = batch.CompanyName,
                CompanyDiscretionaryData = string.Empty,
                CompanyIdentification = batch.CompanyIdentification,
                StandardEntryClassCode = standardEntryClassCode,
                CompanyEntryDescription = companyEntryDescription,
                CompanyDescriptiveDate = batch.EffectiveEntryDate == default ? operationalSnapshot.BogotaTimestamp : batch.EffectiveEntryDate,
                EffectiveEntryDate = batch.EffectiveEntryDate == default ? operationalSnapshot.BogotaTimestamp : batch.EffectiveEntryDate,
                SettlementDate = string.Empty,
                OriginatorStatusCode = "1",
                OriginatingDFI = batch.OriginOrOdfi,
                BatchNumber = batchNumber
            };
        }

        public static BatchHeaderRecord From(
            AchBatch batch,
            string standardEntryClassCode,
            int batchNumber,
            string companyEntryDescription)
        {
            var fallback = batch.EffectiveEntryDate == default
                ? DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Unspecified)
                : DateTime.SpecifyKind(batch.EffectiveEntryDate, DateTimeKind.Unspecified);
            var snapshot = new OperationalTimeSnapshot(
                DateTime.SpecifyKind(fallback, DateTimeKind.Utc),
                fallback,
                DateOnly.FromDateTime(fallback),
                OperationalTimeSnapshotProvider.IanaTimeZoneId);
            return From(batch, standardEntryClassCode, batchNumber, companyEntryDescription, snapshot);
        }
    }

    private static string ResolveStandardEntryClassCode(
        AchBatch batch,
        IReadOnlyCollection<AchTransaction> batchTransactions,
        IReadOnlyCollection<CompanyEntryDescriptionCatalogItem> catalog)
    {
        string description = (batch.CompanyEntryDescription ?? string.Empty).Trim().ToUpperInvariant();
        var termMatch = catalog.FirstOrDefault(item => string.Equals(item.Term, description, StringComparison.OrdinalIgnoreCase));
        if (termMatch is not null)
        {
            return termMatch.StandardEntryClassCode;
        }

        throw new InvalidOperationException("Error Fatal ID 20: Tipo de servicio del lote inválido. El concepto no está parametrizado en el catálogo.");
    }

    private sealed record EntryDetailRecord
    {
        public int TransactionId { get; init; }
        public string TransactionCode { get; init; } = string.Empty;
        public string ReceivingDFI { get; init; } = string.Empty;
        public string CheckDigit { get; init; } = string.Empty;
        public string DestinationAccountNumber { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public string RecipientIdNumber { get; init; } = string.Empty;
        public int AddendaCount { get; init; }
        public string ReceiverName { get; init; } = string.Empty;
        public string DiscretionaryData { get; init; } = string.Empty;
        public string AddendumIndicator { get; init; } = "1";
        public string TraceNumber { get; init; } = string.Empty;
        public string CompanyIdentification { get; init; } = string.Empty;

        public static EntryDetailRecord From(AchTransaction tx, string receiverName, int receivingDfiLength, int addendaCount = 1)
        {
            var receivingDfi = (tx.ReceivingDFI ?? string.Empty).Trim();
            if (receivingDfi.Length != receivingDfiLength || receivingDfi.Any(c => !char.IsDigit(c)))
            {
                throw new InvalidOperationException($"Error Fatal ID 35: el Código Entidad Participante Receptor (posiciones 4-11) debe contener {receivingDfiLength} dígitos numéricos según configuración NACHA.");
            }

            if (receivingDfiLength != 8)
            {
                throw new InvalidOperationException($"Configuración inválida: la longitud del campo ReceivingDFI en registro tipo 6 es {receivingDfiLength}. El cálculo de dígito de chequeo ACH requiere exactamente 8.");
            }

            var expectedCheckDigit = DigitoChequeoHelper.CalcularDigitoChequeo(receivingDfi);
            var destinationCheckDigit = tx.DestinationInstitution?.CheckDigit?.Trim();
            var checkDigit = string.IsNullOrWhiteSpace(destinationCheckDigit) ? expectedCheckDigit : destinationCheckDigit;

            if (!string.Equals(checkDigit, expectedCheckDigit, StringComparison.Ordinal))
            {
                throw new InvalidOperationException($"Error Fatal ID 35: inconsistencia de dígito de chequeo para la entidad receptora {receivingDfi}. Base de datos={checkDigit}, calculado={expectedCheckDigit}.");
            }

            return new EntryDetailRecord
            {
                TransactionId = tx.Id,
                TransactionCode = tx.TransactionCode,
                ReceivingDFI = receivingDfi,
                CheckDigit = checkDigit,
                DestinationAccountNumber = tx.DestinationAccountNumber,
                Amount = tx.Amount,
                RecipientIdNumber = tx.RecipientIdNumber,
                AddendaCount = addendaCount,
                ReceiverName = receiverName,
                DiscretionaryData = tx.DiscretionaryData,
                AddendumIndicator = addendaCount > 0 ? "1" : "0",
                TraceNumber = tx.TraceNumber,
                CompanyIdentification = tx.CompanyIdentification
            };
        }
    }

    private async Task<IReadOnlyList<EntryDetailRecord>> BuildEntryDetailRecordsAsync(
        IReadOnlyList<AchTransaction> transactions,
        CancellationToken ct)
    {
        var records = new List<EntryDetailRecord>(transactions.Count);
        var receivingDfiLength = await ResolveReceivingDfiLengthFromLayoutAsync(ct);
        var receiverLookup = await BuildReceiverLookupAsync(transactions, ct);

        foreach (var transaction in transactions.OrderBy(t => t.Id))
        {
            var receiverName = await ResolveReceiverNameForType6Async(transaction, receiverLookup, ct);
            if (string.IsNullOrWhiteSpace(receiverName))
            {
                receiverName = !string.IsNullOrWhiteSpace(transaction.RecipientIdNumber)
                    ? transaction.RecipientIdNumber
                    : $"USUARIO {transaction.Id}";
            }

            var normalizedReceiverName = NachaReceiverNameHelper.SanitizeForType6(receiverName);

            if (string.IsNullOrWhiteSpace(normalizedReceiverName))
            {
                throw new InvalidOperationException("Error Fatal ID 22: el flujo legacy de desarrollo no pudo resolver el nombre del receptor.");
            }

            records.Add(EntryDetailRecord.From(transaction, normalizedReceiverName, receivingDfiLength));
        }

        return records;
    }

    private async Task<int> ResolveReceivingDfiLengthFromLayoutAsync(CancellationToken ct)
    {
        var fieldLength = await _context.NachaRecordFields
            .AsNoTracking()
            .Where(field =>
                field.Layout.RecordCode == "6" &&
                (field.FieldName == "ReceivingDFI" || field.DbColumn == "ReceivingDFI"))
            .Select(field => (int?)field.Length)
            .FirstOrDefaultAsync(ct);

        return fieldLength ?? 8;
    }

    private async Task<string> ResolveReceiverNameForType6Async(AchTransaction transaction, CancellationToken ct)
    {
        return await ResolveReceiverNameForType6Async(transaction, receiverLookup: null, ct);
    }

    private async Task<string> ResolveReceiverNameForType6Async(
        AchTransaction transaction,
        ReceiverLookup? receiverLookup,
        CancellationToken ct)
    {
        var destinationAccount = (transaction.DestinationAccountNumber ?? string.Empty).Trim();
        var recipientId = (transaction.RecipientIdNumber ?? string.Empty).Trim();

        if (receiverLookup is not null)
        {
            if (!string.IsNullOrWhiteSpace(recipientId) &&
                receiverLookup.CustomersByDocumentAndAccount.TryGetValue((recipientId, destinationAccount), out var exactMatches) &&
                exactMatches.Count > 0)
            {
                return BuildReceiverName(exactMatches[0]);
            }

            if (receiverLookup.CustomersByAccount.TryGetValue(destinationAccount, out var accountMatches) && accountMatches.Count == 1)
            {
                return BuildReceiverName(accountMatches[0]);
            }
        }
        else
        {
            Customer? customer;
            if (!string.IsNullOrWhiteSpace(recipientId))
            {
                customer = await _context.Customers
                    .AsNoTracking()
                    .Include(c => c.Accounts)
                    .FirstOrDefaultAsync(c => c.DocumentNumber == recipientId && c.Accounts.Any(a => a.AccountNumber == destinationAccount), ct);

                if (customer is not null)
                {
                    return BuildReceiverName(customer);
                }
            }

            var accountMatches = await _context.Customers
                .AsNoTracking()
                .Include(c => c.Accounts)
                .Where(c => c.Accounts.Any(a => a.AccountNumber == destinationAccount))
                .ToListAsync(ct);

            if (accountMatches.Count == 1)
            {
                return BuildReceiverName(accountMatches[0]);
            }
        }

        return string.Empty;
    }

    private static string BuildReceiverName(Customer customer)
    {
        if (string.Equals(customer.PersonType, "PJ", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(customer.CompanyName))
        {
            return customer.CompanyName;
        }

        var parts = new[] { customer.FirstName, customer.LastName }
            .Where(value => !string.IsNullOrWhiteSpace(value));

        var fullName = string.Join(' ', parts).Trim();
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            return fullName;
        }

        return customer.CompanyName ?? string.Empty;
    }

    private async Task<ReceiverLookup> BuildReceiverLookupAsync(
        IReadOnlyList<AchTransaction> transactions,
        CancellationToken ct)
    {
        var destinationAccounts = transactions
            .Select(tx => (tx.DestinationAccountNumber ?? string.Empty).Trim())
            .Where(account => !string.IsNullOrWhiteSpace(account))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (destinationAccounts.Length == 0)
        {
            return ReceiverLookup.Empty;
        }

        var customers = await _context.Customers
            .AsNoTracking()
            .Include(c => c.Accounts)
            .Where(c => c.Accounts.Any(a => destinationAccounts.Contains(a.AccountNumber)))
            .ToListAsync(ct);

        var byDocumentAndAccount = new Dictionary<(string Document, string Account), List<Customer>>();
        var byAccount = new Dictionary<string, List<Customer>>(StringComparer.Ordinal);

        foreach (var customer in customers)
        {
            var document = (customer.DocumentNumber ?? string.Empty).Trim();
            foreach (var account in customer.Accounts)
            {
                var accountNumber = (account.AccountNumber ?? string.Empty).Trim();
                if (string.IsNullOrWhiteSpace(accountNumber))
                {
                    continue;
                }

                if (!byAccount.TryGetValue(accountNumber, out var list))
                {
                    list = new List<Customer>();
                    byAccount[accountNumber] = list;
                }

                if (!list.Any(existing => existing.Id == customer.Id))
                {
                    list.Add(customer);
                }

                if (!string.IsNullOrWhiteSpace(document))
                {
                    var documentAccountKey = (document, accountNumber);
                    if (!byDocumentAndAccount.TryGetValue(documentAccountKey, out var exactList))
                    {
                        exactList = new List<Customer>();
                        byDocumentAndAccount[documentAccountKey] = exactList;
                    }

                    if (!exactList.Any(existing => existing.Id == customer.Id))
                    {
                        exactList.Add(customer);
                    }
                }
            }
        }

        return new ReceiverLookup(
            byDocumentAndAccount.ToDictionary(pair => pair.Key, pair => (IReadOnlyList<Customer>)pair.Value),
            byAccount);
    }

    private sealed record BatchControlRecord
    {
        public string ServiceClassCode { get; init; } = string.Empty;
        public int EntryAddendaCount { get; init; }
        public long EntryHash { get; init; }
        public long TotalDebitAmount { get; init; }
        public long TotalCreditAmount { get; init; }
        public string CompanyIdentification { get; init; } = string.Empty;
        public string MessageAuthenticationCode { get; init; } = string.Empty;
        public string OriginatingDFI { get; init; } = string.Empty;
        public int BatchNumber { get; init; }

        public static BatchControlRecord From(AchBatch batch, NachaBatchControlTotals totals, int batchNumber)
        {
            return new BatchControlRecord
            {
                ServiceClassCode = batch.ServiceClassCode,
                EntryAddendaCount = totals.EntryAddendaCount,
                EntryHash = totals.EntryHash,
                TotalDebitAmount = totals.TotalDebitAmountInCents,
                TotalCreditAmount = totals.TotalCreditAmountInCents,
                CompanyIdentification = batch.CompanyIdentification,
                MessageAuthenticationCode = string.Empty,
                OriginatingDFI = batch.OriginOrOdfi,
                BatchNumber = batchNumber
            };
        }

        public static BatchControlRecord From(AchBatch batch, int entryAddendaCount, long batchDebit, long batchCredit, int batchNumber)
        {
            return new BatchControlRecord
            {
                ServiceClassCode = batch.ServiceClassCode,
                EntryAddendaCount = entryAddendaCount,
                EntryHash = ComputeEntryHash(batch.Transactions),
                TotalDebitAmount = batchDebit,
                TotalCreditAmount = batchCredit,
                CompanyIdentification = batch.CompanyIdentification,
                MessageAuthenticationCode = string.Empty,
                OriginatingDFI = batch.OriginOrOdfi,
                BatchNumber = batchNumber
            };
        }
    }

    private sealed record FileControlRecord
    {
        public int BatchCount { get; init; }
        public int BlockCount { get; init; }
        public int EntryAddendaCount { get; init; }
        public long EntryHash { get; init; }
        public long TotalDebitAmount { get; init; }
        public long TotalCreditAmount { get; init; }
        public string CycleName { get; init; } = string.Empty;

        public static FileControlRecord From(AchCycle cycle, NachaFileControlTotals totals)
        {
            return new FileControlRecord
            {
                BatchCount = totals.BatchCount,
                BlockCount = totals.BlockCount,
                EntryAddendaCount = totals.EntryAddendaCount,
                EntryHash = totals.EntryHash,
                TotalDebitAmount = totals.TotalDebitAmountInCents,
                TotalCreditAmount = totals.TotalCreditAmountInCents,
                CycleName = cycle.CycleName
            };
        }

        public static FileControlRecord From(AchCycle cycle, IEnumerable<AchBatch> batches, int batchCount, int blockCount, int entryAddendaCount, long totalDebit, long totalCredit)
        {
            return new FileControlRecord
            {
                BatchCount = batchCount,
                BlockCount = blockCount,
                EntryAddendaCount = entryAddendaCount,
                EntryHash = ComputeEntryHash(batches.SelectMany(b => b.Transactions)),
                TotalDebitAmount = totalDebit,
                TotalCreditAmount = totalCredit,
                CycleName = cycle.CycleName
            };
        }
    }

    private static long ComputeEntryHash(IEnumerable<AchTransaction> transactions)
    {
        const long maxHash = 10_000_000_000L;
        long hash = 0;

        foreach (var tx in transactions)
        {
            var dfi = new string((tx.ReceivingDFI ?? string.Empty).Where(char.IsDigit).ToArray());
            if (dfi.Length == 0)
            {
                continue;
            }

            if (long.TryParse(dfi, out var value))
            {
                hash = (hash + value) % maxHash;
            }
        }

        return hash;
    }
}
