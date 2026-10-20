using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.Mapping;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Models.Mapping;
using Cfa.ACHInterbank.Domain.Helpers;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class NachaFileBuilder : INachaFileBuilder
{
    private readonly NachaFileGenerationCoordinator _coordinator;

    public NachaFileBuilder(
        AchDbContext context,
        IBankHoliday holidayService,
        INachaDataLoader dataLoader,
        INachaTransactionValidationService transactionValidationService,
        INachaFixedWidthRecordRenderer recordRenderer,
        INachaRecordDataProvider recordDataProvider,
        INachaSemanticValidator nachaSemanticValidator,
        INachaConfigResolver? configResolver = null,
        INachaType7AliasMap? type7AliasMap = null,
        INachaType7GenerationStrategy? type7GenerationStrategy = null,
        INachaType7LegacyRenderer? type7LegacyRenderer = null,
        INachaType7RolloutPolicy? type7RolloutPolicy = null,
        INachaRecordMappingEngine? recordMappingEngine = null,
        IFieldMappingPlanCompiler? mappingPlanCompiler = null,
        IOptions<NachaGenerationOptions>? generationOptions = null,
        ILogger<NachaFileBuilder>? logger = null,
        IBatchNumberGenerator? batchNumberGenerator = null,
        INachaControlTotalsCalculator? controlTotalsCalculator = null,
        IOperationalTimeSnapshotProvider? operationalTimeProvider = null,
        ICycleCalendarGuard? calendarGuard = null)
    {
        _ = holidayService;

        var resolvedBatchNumberGenerator = batchNumberGenerator
            ?? new DailyResetBatchNumberGenerator(new BatchNumberSequenceStore(context));
        var resolvedControlTotalsCalculator = controlTotalsCalculator ?? new NachaControlTotalsCalculator();
        var resolvedOperationalTimeProvider = operationalTimeProvider ?? new OperationalTimeSnapshotProvider();
        var resolvedCalendarGuard = calendarGuard ?? new CycleCalendarGuard(context);

        _coordinator = new NachaFileGenerationCoordinator(
            context,
            dataLoader,
            transactionValidationService,
            recordRenderer,
            recordDataProvider,
            nachaSemanticValidator,
            configResolver,
            type7AliasMap,
            type7GenerationStrategy,
            type7LegacyRenderer,
            type7RolloutPolicy,
            recordMappingEngine,
            mappingPlanCompiler,
            generationOptions?.Value ?? new NachaGenerationOptions(),
            logger,
            resolvedBatchNumberGenerator,
            resolvedControlTotalsCalculator,
            resolvedOperationalTimeProvider,
            resolvedCalendarGuard);
    }

    public Task<NachaReturnOutBuildResult> BuildReturnOutAsync(
        NachaReturnOutBuildRequest request,
        CancellationToken ct = default)
        => _coordinator.BuildReturnOutAsync(request, ct);

    public Task<string> BuildNachaFileAsync(IEnumerable<int> batchIds, CancellationToken ct = default)
        => _coordinator.BuildNachaFileAsync(batchIds, ct);

    public Task<string> BuildNachaFileByCycleAsync(string cycleId, CancellationToken ct = default)
        => _coordinator.BuildNachaFileByCycleAsync(cycleId, ct);

    public Task<NachaFileBuildArtifact> BuildNachaFileArtifactByCycleAsync(
        string cycleId,
        CancellationToken ct = default)
        => _coordinator.BuildNachaFileArtifactByCycleAsync(cycleId, ct);

    public Task<NachaFileBuildResult> BuildNachaFilesByCycleAsync(string cycleId, CancellationToken ct = default)
        => _coordinator.BuildNachaFilesByCycleAsync(cycleId, ct);

    public Task<string> BuildRecordAsync<T>(string recordType, T entity, CancellationToken ct = default)
        => _coordinator.BuildRecordAsync(recordType, entity, ct);

    internal static CfgLayoutVariant ResolveType7Layout(
        NachaConfigResolutionResult resolution,
        NachaType7RecordCandidate candidate)
        => NachaFileGenerationCoordinator.ResolveType7Layout(resolution, candidate);
}
