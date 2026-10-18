using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.External.Connections;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Security.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class IncomingNachaPostProcessingOrchestrator : IIncomingNachaPostProcessingOrchestrator
{
    // The scheduler and an authorized operator may both request a local run.
    // Preserve the existing in-process serialization guard while R5D only
    // decomposes post-processing responsibilities into explicit stages.
    private static readonly SemaphoreSlim DispatchGate = new(1, 1);

    private readonly IncomingNachaPostProcessingPipeline _pipeline;

    public IncomingNachaPostProcessingOrchestrator(
        AchDbContext context,
        IProcTransaccionesRequestMapper mapper,
        IProcTransaccionesResponseParser parser,
        IWscfaachSoapClient soapClient,
        IOptions<IncomingNachaDispatchResilienceOptions>? resilienceOptions = null,
        IOptions<ProcTransaccionesDispatchOptions>? dispatchOptions = null,
        ITransactionIntegrationOperationResolver? operationResolver = null,
        IIntegrationMappingReadinessService? mappingReadinessService = null,
        IIntegrationMappingTraceWriter? mappingTraceWriter = null,
        IContrapartidaDispatchJobService? contrapartidaDispatchJobService = null,
        ISoapIntegrationSettingsService? soapIntegrationSettingsService = null,
        IIncomingNachaLocalLivePreparationService? localLivePreparationService = null,
        IIntegrationResponseCatalogResolver? responseCatalogResolver = null,
        TimeProvider? timeProvider = null,
        IIncomingNachaAchResultResolver? achResultResolver = null,
        ICycleCalendarGuard? calendarGuard = null,
        IOperationalCycleWindowResolver? windowResolver = null,
        ICycleTransactionPolicy? cycleTransactionPolicy = null)
    {
        var effectiveTimeProvider = timeProvider ?? OperationalTimeProvider.SystemBogota;
        _pipeline = new IncomingNachaPostProcessingPipeline(
            context,
            mapper,
            parser,
            soapClient,
            resilienceOptions?.Value ?? new IncomingNachaDispatchResilienceOptions(),
            dispatchOptions?.Value ?? new ProcTransaccionesDispatchOptions(),
            operationResolver,
            mappingReadinessService,
            mappingTraceWriter,
            contrapartidaDispatchJobService,
            soapIntegrationSettingsService,
            localLivePreparationService,
            responseCatalogResolver,
            effectiveTimeProvider,
            achResultResolver,
            calendarGuard ?? new CycleCalendarGuard(context, timeProvider: effectiveTimeProvider),
            windowResolver ?? new OperationalCycleWindowResolver(),
            cycleTransactionPolicy);
    }

    public async Task<IncomingNachaPostProcessingRunResult> ExecuteAsync(
        int chunkSize,
        string triggeredBy,
        CancellationToken ct = default)
    {
        await DispatchGate.WaitAsync(ct);
        try
        {
            return await _pipeline.ExecuteAsync(chunkSize, triggeredBy, ct);
        }
        finally
        {
            DispatchGate.Release();
        }
    }
}
