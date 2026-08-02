using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.External.Connections;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Application.Security.Dtos;
using Cfa.ACHInterbank.Application.Security.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Integrations.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class IncomingNachaPostProcessingOrchestratorTests
{

    [Fact]
    public async Task ExecuteAsync_ReturnsNoElementsSummary_WhenQueueIsEmpty()
    {
        await using var context = BuildContext();
        var mapper = new Mock<IProcTransaccionesRequestMapper>(MockBehavior.Strict);
        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);

        var sut = new IncomingNachaPostProcessingOrchestrator(
            context,
            mapper.Object,
            new ProcTransaccionesResponseParser(),
            soap.Object,
            dispatchOptions: LiveProcTransaccionesOptions());

        var result = await sut.ExecuteAsync(50, "tester");

        Assert.Equal(0, result.Picked);
        Assert.Contains("Sin elementos en cola", result.Summary);
        soap.Verify(x => x.ProcTransaccionesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ConfirmsQueue_AndStoresIntegrationExecution_WhenSoapResponseIsSuccessful()
    {
        await using var context = BuildContext();
        SeedDispatchItem(context);

        var mappingIdentity = BuildMappingIdentity();
        var mapper = BuildMapperSuccess(mappingIdentity.MappingSetId, mappingIdentity.Version, mappingIdentity.SnapshotHash);
        const string requestXml = "<Proc_Transacciones><IDTRAN>1</IDTRAN></Proc_Transacciones>";
        mapper.Setup(x => x.BuildSoapBody(It.IsAny<ProcTransaccionesRequestContract>())).Returns(requestXml);

        const string responseXml = "<Envelope><Body><Proc_TransaccionesResponse><RTAACH>R96</RTAACH><RTALOC>OK</RTALOC></Proc_TransaccionesResponse></Body></Envelope>";
        var soap = new Mock<IWscfaachSoapClient>();
        soap.Setup(x => x.ProcTransaccionesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(responseXml);
        var operationResolver = BuildProcTransaccionesOperationResolver();
        var readiness = BuildProcTransaccionesReadinessService(mappingIdentity.MappingSetId, mappingIdentity.Version, mappingIdentity.SnapshotHash);
        var mappingTraceWriter = new Mock<IIntegrationMappingTraceWriter>();
        await new IntegrationCatalogBootstrapper(context).EnsureAsync();

        var sut = new IncomingNachaPostProcessingOrchestrator(
            context,
            mapper.Object,
            new ProcTransaccionesResponseParser(),
            soap.Object,
            dispatchOptions: LiveProcTransaccionesOptions(),
            operationResolver: operationResolver.Object,
            mappingReadinessService: readiness.Object,
            mappingTraceWriter: mappingTraceWriter.Object,
            soapIntegrationSettingsService: SoapSettingsService("http://localhost:7083/WSCFAACH.svc"),
            responseCatalogResolver: new IntegrationResponseCatalogResolver(context));

        var result = await sut.ExecuteAsync(50, "tester");

        Assert.Equal(1, result.Confirmed);
        var queue = await context.IncomingNachaDispatchQueue.FirstAsync();
        Assert.Equal(IncomingNachaDispatchQueueStatus.Confirmed, queue.QueueStatus);
        Assert.Null(queue.NextAttemptAtUtc);

        var execution = await context.IncomingNachaIntegrationExecution.FirstAsync();
        Assert.Equal(requestXml, execution.RequestPayloadXml);
        Assert.Equal(responseXml, execution.ResponsePayloadXml);
        Assert.False(string.IsNullOrWhiteSpace(execution.RequestHash));
        Assert.False(string.IsNullOrWhiteSpace(execution.ResponseHash));
        Assert.Equal("Proc_Transacciones", execution.SoapMethodName);
        Assert.Equal("http://localhost:7083/WSCFAACH.svc", execution.SoapEndpoint);
        Assert.Equal("Live", execution.ExecutionMode);
        Assert.Equal("R96", execution.SoapResponseCode);
        Assert.Equal("Crédito aplicado correctamente", execution.SoapResponseDescription);
        Assert.NotNull(execution.ResponseCatalogId);
        Assert.Equal(IntegrationTransportStatus.Succeeded, execution.TransportStatus);
        Assert.Equal(IntegrationResponseBusinessStatus.Success, execution.BusinessStatus);
        Assert.Equal("Succeeded", execution.SoapTechnicalStatus);
        Assert.True(execution.IsSuccessful);
        Assert.False(execution.IsFunctionalRejection);
        Assert.False(execution.IsTechnicalFailure);
        Assert.Equal("R96", execution.ResponseCode);
        Assert.Equal("Crédito aplicado correctamente", execution.ResponseMessage);
        Assert.DoesNotContain("<METODO>", execution.RequestPayloadXml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Proc_Contrapartidas", execution.RequestPayloadXml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RegistrarRespuestaTransaccion", execution.RequestPayloadXml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PLValidarUsuarioBV", execution.RequestPayloadXml, StringComparison.OrdinalIgnoreCase);

        var readModel = await new TransactionIntegrationResultService(context).GetAsync(queue.AchTransactionId);
        Assert.NotNull(readModel?.Latest);
        Assert.Equal("Proc_Transacciones", readModel.Latest.Method);
        Assert.Equal("R96", readModel.Latest.ResponseCode);
        Assert.Equal("Crédito aplicado correctamente", readModel.Latest.ResponseDescription);
        Assert.Equal("Success", readModel.Latest.BusinessStatus);
        mappingTraceWriter.Verify(x => x.WriteAsync(
            It.IsAny<TransactionIntegrationOperationResult>(),
            It.IsAny<object>(),
            100,
            It.IsAny<string>(),
            It.IsAny<string>(),
            false,
            true,
            It.IsAny<CancellationToken>()), Times.Once);
    }
    [Fact]
    public async Task ExecuteAsync_BlocksQueue_WhenMappingIsInvalid()
    {
        await using var context = BuildContext();
        SeedDispatchItem(context);

        var mappingIdentity = BuildMappingIdentity();
        var mapper = new Mock<IProcTransaccionesRequestMapper>();
        mapper.Setup(x => x.ResolveAsync(
                It.IsAny<IncomingNachaDispatchQueue>(),
                It.IsAny<IncomingNachaFileIngestion>(),
                It.IsAny<IncomingNachaEntryClassification>(),
                It.IsAny<AchTransaction>(),
                It.IsAny<AchCycle>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("mapping inválido"));

        var operationResolver = BuildProcTransaccionesOperationResolver();
        var readiness = BuildProcTransaccionesReadinessService(mappingIdentity.MappingSetId, mappingIdentity.Version, mappingIdentity.SnapshotHash);

        var sut = new IncomingNachaPostProcessingOrchestrator(
            context,
            mapper.Object,
            new ProcTransaccionesResponseParser(),
            Mock.Of<IWscfaachSoapClient>(),
            operationResolver: operationResolver.Object,
            mappingReadinessService: readiness.Object);

        var result = await sut.ExecuteAsync(50, "tester");

        Assert.Equal(1, result.Blocked);
        var queue = await context.IncomingNachaDispatchQueue.FirstAsync();
        Assert.Equal(IncomingNachaDispatchQueueStatus.Blocked, queue.QueueStatus);
        Assert.Equal("MAPPING_INVALID", queue.LastErrorCode);
        Assert.True(await context.IncomingNachaIntegrationExecution.AnyAsync());
    }

    [Fact]
    public async Task ProcTransacciones_DryRun_ShouldGeneratePayloadAndNotTransmitExternally()
    {
        await using var context = BuildContext();
        SeedDispatchItem(context);

        var mappingIdentity = BuildMappingIdentity();
        var mapper = BuildMapperSuccess(mappingIdentity.MappingSetId, mappingIdentity.Version, mappingIdentity.SnapshotHash);
        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);
        var operationResolver = BuildProcTransaccionesOperationResolver();
        var readiness = BuildProcTransaccionesReadinessService(mappingIdentity.MappingSetId, mappingIdentity.Version, mappingIdentity.SnapshotHash);
        var mappingTraceWriter = new Mock<IIntegrationMappingTraceWriter>();

        var sut = new IncomingNachaPostProcessingOrchestrator(
            context,
            mapper.Object,
            new ProcTransaccionesResponseParser(),
            soap.Object,
            dispatchOptions: Options.Create(new ProcTransaccionesDispatchOptions { Mode = "DryRun" }),
            operationResolver: operationResolver.Object,
            mappingReadinessService: readiness.Object,
            mappingTraceWriter: mappingTraceWriter.Object);

        var result = await sut.ExecuteAsync(50, "tester");

        Assert.Equal(1, result.FailedFinal);
        var execution = await context.IncomingNachaIntegrationExecution.FirstAsync();
        Assert.False(string.IsNullOrWhiteSpace(execution.RequestPayloadXml));
        Assert.Contains("PROC_TRANSACCIONES_DRY_RUN", execution.ResponsePayloadXml);
        Assert.Equal("Proc_Transacciones", execution.SoapMethodName);
        Assert.Equal("DryRun", execution.ExecutionMode);
        Assert.Equal("PROC_TRANSACCIONES_DRY_RUN", execution.SoapResponseCode);
        Assert.Equal("DryRun", execution.SoapTechnicalStatus);
        Assert.False(execution.IsSuccessful);
        Assert.False(execution.IsFunctionalRejection);
        Assert.False(execution.IsTechnicalFailure);
        Assert.DoesNotContain("<METODO>", execution.RequestPayloadXml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Proc_Contrapartidas", execution.RequestPayloadXml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RegistrarRespuestaTransaccion", execution.RequestPayloadXml, StringComparison.OrdinalIgnoreCase);
        Assert.True(await context.IncomingNachaProcessingEvents.AnyAsync(x => x.EventType == "ProcTransaccionesDryRunGuardrail"));
        soap.Verify(x => x.ProcTransaccionesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        mappingTraceWriter.Verify(x => x.WriteAsync(
            It.IsAny<TransactionIntegrationOperationResult>(),
            It.IsAny<object>(),
            100,
            It.IsAny<string>(),
            It.IsAny<string>(),
            true,
            false,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcTransacciones_DisabledMode_ShouldBlockControlledAndNotInvokeSoap()
    {
        await using var context = BuildContext();
        SeedDispatchItem(context);

        var mappingIdentity = BuildMappingIdentity();
        var mapper = BuildMapperSuccess(mappingIdentity.MappingSetId, mappingIdentity.Version, mappingIdentity.SnapshotHash);
        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);
        var operationResolver = BuildProcTransaccionesOperationResolver();
        var readiness = BuildProcTransaccionesReadinessService(mappingIdentity.MappingSetId, mappingIdentity.Version, mappingIdentity.SnapshotHash);

        var sut = new IncomingNachaPostProcessingOrchestrator(
            context,
            mapper.Object,
            new ProcTransaccionesResponseParser(),
            soap.Object,
            dispatchOptions: Options.Create(new ProcTransaccionesDispatchOptions { Mode = "Disabled" }),
            operationResolver: operationResolver.Object,
            mappingReadinessService: readiness.Object);

        var result = await sut.ExecuteAsync(50, "tester");

        Assert.Equal(1, result.Blocked);
        var queue = await context.IncomingNachaDispatchQueue.FirstAsync();
        Assert.Equal(IncomingNachaDispatchQueueStatus.Blocked, queue.QueueStatus);
        Assert.Equal("PROC_TRANSACCIONES_DISABLED", queue.LastErrorCode);
        var execution = await context.IncomingNachaIntegrationExecution.FirstAsync();
        Assert.Equal("PROC_TRANSACCIONES_DISABLED", execution.ResponseCode);
        Assert.Equal("Proc_Transacciones", execution.SoapMethodName);
        Assert.Equal("Disabled", execution.ExecutionMode);
        Assert.Equal("Disabled", execution.SoapTechnicalStatus);
        Assert.Equal("PROC_TRANSACCIONES_DISABLED", execution.SoapResponseCode);
        soap.Verify(x => x.ProcTransaccionesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcTransacciones_DryRun_ShouldValidateReadinessBeforePayload()
    {
        await using var context = BuildContext();
        SeedDispatchItem(context);

        var mapper = new Mock<IProcTransaccionesRequestMapper>(MockBehavior.Strict);
        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);
        var operationResolver = new Mock<ITransactionIntegrationOperationResolver>();
        var readiness = new Mock<IIntegrationMappingReadinessService>();
        operationResolver.Setup(x => x.ResolveAsync(It.IsAny<AchTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProcTransaccionesOperation());
        readiness.Setup(x => x.EvaluateAsync(It.IsAny<TransactionIntegrationOperationResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(FailedProcTransaccionesReadiness());

        var sut = new IncomingNachaPostProcessingOrchestrator(
            context,
            mapper.Object,
            new ProcTransaccionesResponseParser(),
            soap.Object,
            dispatchOptions: Options.Create(new ProcTransaccionesDispatchOptions { Mode = "DryRun" }),
            operationResolver: operationResolver.Object,
            mappingReadinessService: readiness.Object);

        var result = await sut.ExecuteAsync(50, "tester");

        Assert.Equal(1, result.Blocked);
        mapper.Verify(x => x.ResolveAsync(
            It.IsAny<IncomingNachaDispatchQueue>(),
            It.IsAny<IncomingNachaFileIngestion>(),
            It.IsAny<IncomingNachaEntryClassification>(),
            It.IsAny<AchTransaction>(),
            It.IsAny<AchCycle>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Never);
        soap.Verify(x => x.ProcTransaccionesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldDelegateProcContrapartidas_WhenOperationResolverReturnsDebitCandidate()
    {
        await using var context = BuildContext();
        SeedDispatchItem(context);

        var mapper = new Mock<IProcTransaccionesRequestMapper>(MockBehavior.Strict);
        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);
        var operationResolver = new Mock<ITransactionIntegrationOperationResolver>();
        var contrapartidaDispatch = new Mock<IContrapartidaDispatchJobService>();

        operationResolver.Setup(x => x.ResolveAsync(It.IsAny<AchTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProcContrapartidasOperation());
        contrapartidaDispatch
            .Setup(x => x.ProcessCycleAsync("C1", 1, "tester", 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ContrapartidaCycleDispatchResult("C1", 1, 1, 0, 1, 0, 1, "Proc_Contrapartidas dry-run."));

        var sut = new IncomingNachaPostProcessingOrchestrator(
            context,
            mapper.Object,
            new ProcTransaccionesResponseParser(),
            soap.Object,
            dispatchOptions: LiveProcTransaccionesOptions(),
            operationResolver: operationResolver.Object,
            contrapartidaDispatchJobService: contrapartidaDispatch.Object);

        var result = await sut.ExecuteAsync(50, "tester");

        Assert.Equal(1, result.Confirmed);
        var queue = await context.IncomingNachaDispatchQueue.FirstAsync();
        Assert.Equal(IncomingNachaDispatchQueueStatus.Dispatched, queue.QueueStatus);
        Assert.Null(queue.NextAttemptAtUtc);
        contrapartidaDispatch.Verify(x => x.ProcessCycleAsync("C1", 1, "tester", 50, It.IsAny<CancellationToken>()), Times.Once);
        mapper.Verify(x => x.ResolveAsync(
            It.IsAny<IncomingNachaDispatchQueue>(),
            It.IsAny<IncomingNachaFileIngestion>(),
            It.IsAny<IncomingNachaEntryClassification>(),
            It.IsAny<AchTransaction>(),
            It.IsAny<AchCycle>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Never);
        soap.Verify(x => x.ProcTransaccionesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcTransacciones_DryRun_ShouldFail_WhenRequiredFieldUsesFallback()
    {
        await using var context = BuildContext();
        SeedDispatchItem(context);

        var mapper = new Mock<IProcTransaccionesRequestMapper>(MockBehavior.Strict);
        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);
        var operationResolver = new Mock<ITransactionIntegrationOperationResolver>();
        var readiness = new Mock<IIntegrationMappingReadinessService>();
        operationResolver.Setup(x => x.ResolveAsync(It.IsAny<AchTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProcTransaccionesOperation());
        readiness.Setup(x => x.EvaluateAsync(It.IsAny<TransactionIntegrationOperationResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntegrationMappingReadinessResult(
                true,
                "Partial",
                "PROC_TRANSACCIONES_REQUIRED_FIELD_USES_FALLBACK",
                IntegrationGuaranteeConstants.Wscfaach,
                IntegrationGuaranteeConstants.ProcTransacciones,
                IntegrationGuaranteeConstants.MonetaryCreditRequest,
                IntegrationGuaranteeConstants.OutboundRequest,
                5,
                5,
                [],
                [],
                ["IDTRAN"],
                ["IDTRAN"],
                true,
                true,
                [],
                ["Fallback requerido."]));

        var sut = new IncomingNachaPostProcessingOrchestrator(
            context,
            mapper.Object,
            new ProcTransaccionesResponseParser(),
            soap.Object,
            dispatchOptions: Options.Create(new ProcTransaccionesDispatchOptions { Mode = "DryRun" }),
            operationResolver: operationResolver.Object,
            mappingReadinessService: readiness.Object);

        var result = await sut.ExecuteAsync(50, "tester");

        Assert.Equal(1, result.Blocked);
        var queue = await context.IncomingNachaDispatchQueue.FirstAsync();
        Assert.Contains("PROC_TRANSACCIONES_REQUIRED_FIELD_USES_FALLBACK", queue.LastErrorMessage);
        soap.Verify(x => x.ProcTransaccionesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcTransacciones_DryRun_ShouldBlock_WhenMappingSnapshotChangesBeforeDispatch()
    {
        await using var context = BuildContext();
        SeedDispatchItem(context);

        var mapper = new Mock<IProcTransaccionesRequestMapper>(MockBehavior.Strict);
        mapper.Setup(x => x.ResolveAsync(
                It.IsAny<IncomingNachaDispatchQueue>(),
                It.IsAny<IncomingNachaFileIngestion>(),
                It.IsAny<IncomingNachaEntryClassification>(),
                It.IsAny<AchTransaction>(),
                It.IsAny<AchCycle>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcTransaccionesRequestResolution(
                new ProcTransaccionesRequestContract(new Dictionary<string, string> { ["TIPTRAN"] = "32" }),
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                7,
                "HASH-RESOLUTION"));

        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);
        var operationResolver = new Mock<ITransactionIntegrationOperationResolver>();
        var readiness = new Mock<IIntegrationMappingReadinessService>();
        operationResolver.Setup(x => x.ResolveAsync(It.IsAny<AchTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProcTransaccionesOperation());
        readiness.Setup(x => x.EvaluateAsync(It.IsAny<TransactionIntegrationOperationResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntegrationMappingReadinessResult(
                true,
                "Ok",
                "OK",
                IntegrationGuaranteeConstants.Wscfaach,
                IntegrationGuaranteeConstants.ProcTransacciones,
                IntegrationGuaranteeConstants.MonetaryCreditRequest,
                IntegrationGuaranteeConstants.OutboundRequest,
                5,
                5,
                [],
                [],
                [],
                [],
                false,
                true,
                [],
                [])
            {
                MappingSetId = Guid.Parse("22222222-2222-2222-2222-222222222222"),
                MappingVersion = 7,
                MappingSnapshotHash = "HASH-READY"
            });

        var sut = new IncomingNachaPostProcessingOrchestrator(
            context,
            mapper.Object,
            new ProcTransaccionesResponseParser(),
            soap.Object,
            dispatchOptions: Options.Create(new ProcTransaccionesDispatchOptions { Mode = "DryRun" }),
            operationResolver: operationResolver.Object,
            mappingReadinessService: readiness.Object);

        var result = await sut.ExecuteAsync(50, "tester");

        Assert.Equal(1, result.Blocked);
        var queue = await context.IncomingNachaDispatchQueue.FirstAsync();
        Assert.Equal(IncomingNachaDispatchQueueStatus.Blocked, queue.QueueStatus);
        Assert.Equal("MAPPING_SNAPSHOT_CHANGED", queue.LastErrorCode);
        Assert.Contains("MAPPING_SNAPSHOT_CHANGED", queue.LastErrorMessage);
        soap.Verify(x => x.ProcTransaccionesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcTransacciones_DryRun_ShouldBlock_WhenOperationResolverIsMissing()
    {
        await using var context = BuildContext();
        SeedDispatchItem(context);

        var mapper = new Mock<IProcTransaccionesRequestMapper>(MockBehavior.Strict);
        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);

        var sut = new IncomingNachaPostProcessingOrchestrator(
            context,
            mapper.Object,
            new ProcTransaccionesResponseParser(),
            soap.Object,
            dispatchOptions: Options.Create(new ProcTransaccionesDispatchOptions { Mode = "DryRun" }),
            mappingReadinessService: new Mock<IIntegrationMappingReadinessService>().Object);

        var result = await sut.ExecuteAsync(50, "tester");

        Assert.Equal(1, result.Blocked);
        var queue = await context.IncomingNachaDispatchQueue.FirstAsync();
        Assert.Equal("PROC_TRANSACCIONES_READINESS_SERVICE_UNAVAILABLE", queue.LastErrorCode);
        mapper.Verify(x => x.ResolveAsync(
            It.IsAny<IncomingNachaDispatchQueue>(),
            It.IsAny<IncomingNachaFileIngestion>(),
            It.IsAny<IncomingNachaEntryClassification>(),
            It.IsAny<AchTransaction>(),
            It.IsAny<AchCycle>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Never);
        soap.Verify(x => x.ProcTransaccionesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcTransacciones_DryRun_ShouldBlock_WhenReadinessServiceIsMissing()
    {
        await using var context = BuildContext();
        SeedDispatchItem(context);

        var mapper = new Mock<IProcTransaccionesRequestMapper>(MockBehavior.Strict);
        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);
        var operationResolver = new Mock<ITransactionIntegrationOperationResolver>();
        operationResolver.Setup(x => x.ResolveAsync(It.IsAny<AchTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProcTransaccionesOperation());

        var sut = new IncomingNachaPostProcessingOrchestrator(
            context,
            mapper.Object,
            new ProcTransaccionesResponseParser(),
            soap.Object,
            dispatchOptions: Options.Create(new ProcTransaccionesDispatchOptions { Mode = "DryRun" }),
            operationResolver: operationResolver.Object);

        var result = await sut.ExecuteAsync(50, "tester");

        Assert.Equal(1, result.Blocked);
        var queue = await context.IncomingNachaDispatchQueue.FirstAsync();
        Assert.Equal("PROC_TRANSACCIONES_READINESS_SERVICE_UNAVAILABLE", queue.LastErrorCode);
        mapper.Verify(x => x.ResolveAsync(
            It.IsAny<IncomingNachaDispatchQueue>(),
            It.IsAny<IncomingNachaFileIngestion>(),
            It.IsAny<IncomingNachaEntryClassification>(),
            It.IsAny<AchTransaction>(),
            It.IsAny<AchCycle>(),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()), Times.Never);
        soap.Verify(x => x.ProcTransaccionesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcTransacciones_DryRun_ShouldBlock_WhenReadinessMissingMappingSetId()
    {
        await using var context = BuildContext();
        SeedDispatchItem(context);

        var resolution = BuildResolution(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            7,
            "HASH-RESOLUTION");
        var mapper = new Mock<IProcTransaccionesRequestMapper>(MockBehavior.Strict);
        mapper.Setup(x => x.ResolveAsync(
                It.IsAny<IncomingNachaDispatchQueue>(),
                It.IsAny<IncomingNachaFileIngestion>(),
                It.IsAny<IncomingNachaEntryClassification>(),
                It.IsAny<AchTransaction>(),
                It.IsAny<AchCycle>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolution);

        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);
        var operationResolver = new Mock<ITransactionIntegrationOperationResolver>();
        var readiness = new Mock<IIntegrationMappingReadinessService>();
        operationResolver.Setup(x => x.ResolveAsync(It.IsAny<AchTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProcTransaccionesOperation());
        readiness.Setup(x => x.EvaluateAsync(It.IsAny<TransactionIntegrationOperationResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildReadiness(
                mappingSetId: null,
                mappingVersion: 7,
                mappingSnapshotHash: "HASH-RESOLUTION"));

        var sut = new IncomingNachaPostProcessingOrchestrator(
            context,
            mapper.Object,
            new ProcTransaccionesResponseParser(),
            soap.Object,
            dispatchOptions: Options.Create(new ProcTransaccionesDispatchOptions { Mode = "DryRun" }),
            operationResolver: operationResolver.Object,
            mappingReadinessService: readiness.Object);

        var result = await sut.ExecuteAsync(50, "tester");

        Assert.Equal(1, result.Blocked);
        var queue = await context.IncomingNachaDispatchQueue.FirstAsync();
        Assert.Equal("MAPPING_SNAPSHOT_CHANGED", queue.LastErrorCode);
        Assert.Contains("identidad completa", queue.LastErrorMessage, StringComparison.OrdinalIgnoreCase);
        soap.Verify(x => x.ProcTransaccionesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcTransacciones_DryRun_ShouldBlock_WhenReadinessMissingVersion()
    {
        await using var context = BuildContext();
        SeedDispatchItem(context);

        var resolution = BuildResolution(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            7,
            "HASH-RESOLUTION");
        var mapper = new Mock<IProcTransaccionesRequestMapper>(MockBehavior.Strict);
        mapper.Setup(x => x.ResolveAsync(
                It.IsAny<IncomingNachaDispatchQueue>(),
                It.IsAny<IncomingNachaFileIngestion>(),
                It.IsAny<IncomingNachaEntryClassification>(),
                It.IsAny<AchTransaction>(),
                It.IsAny<AchCycle>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolution);

        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);
        var operationResolver = new Mock<ITransactionIntegrationOperationResolver>();
        var readiness = new Mock<IIntegrationMappingReadinessService>();
        operationResolver.Setup(x => x.ResolveAsync(It.IsAny<AchTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProcTransaccionesOperation());
        readiness.Setup(x => x.EvaluateAsync(It.IsAny<TransactionIntegrationOperationResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildReadiness(
                mappingSetId: resolution.MappingSetId,
                mappingVersion: null,
                mappingSnapshotHash: "HASH-RESOLUTION"));

        var sut = new IncomingNachaPostProcessingOrchestrator(
            context,
            mapper.Object,
            new ProcTransaccionesResponseParser(),
            soap.Object,
            dispatchOptions: Options.Create(new ProcTransaccionesDispatchOptions { Mode = "DryRun" }),
            operationResolver: operationResolver.Object,
            mappingReadinessService: readiness.Object);

        var result = await sut.ExecuteAsync(50, "tester");

        Assert.Equal(1, result.Blocked);
        var queue = await context.IncomingNachaDispatchQueue.FirstAsync();
        Assert.Equal("MAPPING_SNAPSHOT_CHANGED", queue.LastErrorCode);
        Assert.Contains("identidad completa", queue.LastErrorMessage, StringComparison.OrdinalIgnoreCase);
        soap.Verify(x => x.ProcTransaccionesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcTransacciones_DryRun_ShouldBlock_WhenReadinessMissingSnapshotHash()
    {
        await using var context = BuildContext();
        SeedDispatchItem(context);

        var resolution = BuildResolution(
            Guid.Parse("11111111-1111-1111-1111-111111111111"),
            7,
            "HASH-RESOLUTION");
        var mapper = new Mock<IProcTransaccionesRequestMapper>(MockBehavior.Strict);
        mapper.Setup(x => x.ResolveAsync(
                It.IsAny<IncomingNachaDispatchQueue>(),
                It.IsAny<IncomingNachaFileIngestion>(),
                It.IsAny<IncomingNachaEntryClassification>(),
                It.IsAny<AchTransaction>(),
                It.IsAny<AchCycle>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(resolution);

        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);
        var operationResolver = new Mock<ITransactionIntegrationOperationResolver>();
        var readiness = new Mock<IIntegrationMappingReadinessService>();
        operationResolver.Setup(x => x.ResolveAsync(It.IsAny<AchTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProcTransaccionesOperation());
        readiness.Setup(x => x.EvaluateAsync(It.IsAny<TransactionIntegrationOperationResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildReadiness(
                mappingSetId: resolution.MappingSetId,
                mappingVersion: resolution.MappingVersion,
                mappingSnapshotHash: null));

        var sut = new IncomingNachaPostProcessingOrchestrator(
            context,
            mapper.Object,
            new ProcTransaccionesResponseParser(),
            soap.Object,
            dispatchOptions: Options.Create(new ProcTransaccionesDispatchOptions { Mode = "DryRun" }),
            operationResolver: operationResolver.Object,
            mappingReadinessService: readiness.Object);

        var result = await sut.ExecuteAsync(50, "tester");

        Assert.Equal(1, result.Blocked);
        var queue = await context.IncomingNachaDispatchQueue.FirstAsync();
        Assert.Equal("MAPPING_SNAPSHOT_CHANGED", queue.LastErrorCode);
        Assert.Contains("identidad completa", queue.LastErrorMessage, StringComparison.OrdinalIgnoreCase);
        soap.Verify(x => x.ProcTransaccionesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ProcTransacciones_DryRun_ShouldProceed_WhenSnapshotMatches()
    {
        await using var context = BuildContext();
        SeedDispatchItem(context);

        var mappingSetId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var mapper = new Mock<IProcTransaccionesRequestMapper>(MockBehavior.Strict);
        mapper.Setup(x => x.ResolveAsync(
                It.IsAny<IncomingNachaDispatchQueue>(),
                It.IsAny<IncomingNachaFileIngestion>(),
                It.IsAny<IncomingNachaEntryClassification>(),
                It.IsAny<AchTransaction>(),
                It.IsAny<AchCycle>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildResolution(mappingSetId, 7, "HASH-MATCH"));
        mapper.Setup(x => x.BuildSoapBody(It.IsAny<ProcTransaccionesRequestContract>()))
            .Returns("<request/>");

        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);
        var operationResolver = new Mock<ITransactionIntegrationOperationResolver>();
        var readiness = new Mock<IIntegrationMappingReadinessService>();
        operationResolver.Setup(x => x.ResolveAsync(It.IsAny<AchTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProcTransaccionesOperation());
        readiness.Setup(x => x.EvaluateAsync(It.IsAny<TransactionIntegrationOperationResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildReadiness(mappingSetId, 7, "HASH-MATCH"));

        var sut = new IncomingNachaPostProcessingOrchestrator(
            context,
            mapper.Object,
            new ProcTransaccionesResponseParser(),
            soap.Object,
            dispatchOptions: Options.Create(new ProcTransaccionesDispatchOptions { Mode = "Live" }),
            operationResolver: operationResolver.Object,
            mappingReadinessService: readiness.Object,
            soapIntegrationSettingsService: SoapSettingsService("http://localhost:7083/WSCFAACH.svc"),
            responseCatalogResolver: Catalog(Success("00", "Crédito aplicado")));

        soap.Setup(x => x.ProcTransaccionesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<Envelope><Body><Proc_TransaccionesResponse><RTAACH>00</RTAACH><RTALOC>OK</RTALOC></Proc_TransaccionesResponse></Body></Envelope>");

        var result = await sut.ExecuteAsync(50, "tester");

        Assert.Equal(1, result.Confirmed);
        var queue = await context.IncomingNachaDispatchQueue.FirstAsync();
        Assert.Equal(IncomingNachaDispatchQueueStatus.Confirmed, queue.QueueStatus);
        mapper.Verify(x => x.BuildSoapBody(It.IsAny<ProcTransaccionesRequestContract>()), Times.Once);
        soap.Verify(x => x.ProcTransaccionesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_SetsRetryPending_WhenTechnicalErrorOccurs()
    {
        await using var context = BuildContext();
        SeedDispatchItem(context);

        var mappingIdentity = BuildMappingIdentity();
        var mapper = new Mock<IProcTransaccionesRequestMapper>();
        mapper.Setup(x => x.ResolveAsync(
                It.IsAny<IncomingNachaDispatchQueue>(),
                It.IsAny<IncomingNachaFileIngestion>(),
                It.IsAny<IncomingNachaEntryClassification>(),
                It.IsAny<AchTransaction>(),
                It.IsAny<AchCycle>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcTransaccionesRequestResolution(
                new ProcTransaccionesRequestContract(new Dictionary<string, string> { ["TREG"] = "6", ["TIPTRAN"] = "22", ["MONTO"] = "10", ["IDTRAN"] = "1", ["IDCAMCOMPE"] = "1" }),
                mappingIdentity.MappingSetId,
                mappingIdentity.Version,
                mappingIdentity.SnapshotHash));
        mapper.Setup(x => x.BuildSoapBody(It.IsAny<ProcTransaccionesRequestContract>())).Returns("<request/>");

        var soap = new Mock<IWscfaachSoapClient>();
        soap.Setup(x => x.ProcTransaccionesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("timeout"));
        var operationResolver = BuildProcTransaccionesOperationResolver();
        var readiness = BuildProcTransaccionesReadinessService(mappingIdentity.MappingSetId, mappingIdentity.Version, mappingIdentity.SnapshotHash);

        var sut = new IncomingNachaPostProcessingOrchestrator(
            context,
            mapper.Object,
            new ProcTransaccionesResponseParser(),
            soap.Object,
            operationResolver: operationResolver.Object,
            mappingReadinessService: readiness.Object,
            dispatchOptions: LiveProcTransaccionesOptions());

        var result = await sut.ExecuteAsync(50, "tester");

        Assert.Equal(1, result.RetryPending);
        var queue = await context.IncomingNachaDispatchQueue.FirstAsync();
        Assert.Equal(IncomingNachaDispatchQueueStatus.RetryPending, queue.QueueStatus);
        Assert.NotNull(queue.NextAttemptAtUtc);
        var execution = await context.IncomingNachaIntegrationExecution.FirstAsync();
        Assert.Equal("Proc_Transacciones", execution.SoapMethodName);
        Assert.Equal("Live", execution.ExecutionMode);
        Assert.Equal("ITIMEOUT", execution.SoapResponseCode);
        Assert.Equal("TechnicalException", execution.SoapTechnicalStatus);
        Assert.False(execution.IsSuccessful);
        Assert.False(execution.IsFunctionalRejection);
        Assert.True(execution.IsTechnicalFailure);
        Assert.Contains("timeout", execution.TechnicalException, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<METODO>", execution.RequestPayloadXml, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_BlocksUnknownFunctionalCode_ForCatalogReview()
    {
        await using var context = BuildContext();
        SeedDispatchItem(context);

        var mappingIdentity = BuildMappingIdentity();
        var mapper = BuildMapperSuccess(mappingIdentity.MappingSetId, mappingIdentity.Version, mappingIdentity.SnapshotHash);
        var soap = new Mock<IWscfaachSoapClient>();
        soap.Setup(x => x.ProcTransaccionesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<Envelope><Body><Proc_TransaccionesResponse><RTAACH>R17</RTAACH><RTALOC>Codigo funcional observado</RTALOC></Proc_TransaccionesResponse></Body></Envelope>");
        var operationResolver = BuildProcTransaccionesOperationResolver();
        var readiness = BuildProcTransaccionesReadinessService(mappingIdentity.MappingSetId, mappingIdentity.Version, mappingIdentity.SnapshotHash);

        var sut = new IncomingNachaPostProcessingOrchestrator(
            context,
            mapper.Object,
            new ProcTransaccionesResponseParser(),
            soap.Object,
            operationResolver: operationResolver.Object,
            mappingReadinessService: readiness.Object,
            dispatchOptions: LiveProcTransaccionesOptions());

        var result = await sut.ExecuteAsync(50, "tester");

        Assert.Equal(1, result.Blocked);
        var queue = await context.IncomingNachaDispatchQueue.FirstAsync();
        Assert.Equal(IncomingNachaDispatchQueueStatus.Blocked, queue.QueueStatus);
        Assert.Equal("R17", queue.LastErrorCode);
        var execution = await context.IncomingNachaIntegrationExecution.FirstAsync();
        Assert.Equal("R17", execution.SoapResponseCode);
        Assert.Equal("Código pendiente de parametrización", execution.SoapResponseDescription);
        Assert.Equal("Succeeded", execution.SoapTechnicalStatus);
        Assert.Equal(IntegrationTransportStatus.Succeeded, execution.TransportStatus);
        Assert.Equal(IntegrationResponseBusinessStatus.PendingCatalog, execution.BusinessStatus);
        Assert.True(execution.RequiresManualReview);
        Assert.False(execution.RetryAllowed);
        Assert.False(execution.IsSuccessful);
        Assert.False(execution.IsFunctionalRejection);
        Assert.False(execution.IsTechnicalFailure);
        Assert.Equal("R17", execution.ResponseCode);
        Assert.True(await context.IncomingNachaProcessingEvents.AnyAsync(x => x.EventType == "IntegrationResponsePendingCatalog"));
    }

    [Fact]
    public async Task IncomingNachaIntegrationExecution_EfModel_ShouldExposeSoapAuditColumnsAndIndexes()
    {
        await using var context = BuildContext();

        var entity = context.Model.FindEntityType(typeof(IncomingNachaIntegrationExecution));

        Assert.NotNull(entity);
        Assert.NotNull(entity.FindProperty(nameof(IncomingNachaIntegrationExecution.SoapMethodName)));
        Assert.NotNull(entity.FindProperty(nameof(IncomingNachaIntegrationExecution.SoapEndpoint)));
        Assert.NotNull(entity.FindProperty(nameof(IncomingNachaIntegrationExecution.ExecutionMode)));
        Assert.NotNull(entity.FindProperty(nameof(IncomingNachaIntegrationExecution.DurationMs)));
        Assert.NotNull(entity.FindProperty(nameof(IncomingNachaIntegrationExecution.SoapResponseCode)));
        Assert.NotNull(entity.FindProperty(nameof(IncomingNachaIntegrationExecution.SoapResponseDescription)));
        Assert.NotNull(entity.FindProperty(nameof(IncomingNachaIntegrationExecution.SoapTechnicalStatus)));
        Assert.NotNull(entity.FindProperty(nameof(IncomingNachaIntegrationExecution.IsSuccessful)));
        Assert.NotNull(entity.FindProperty(nameof(IncomingNachaIntegrationExecution.IsFunctionalRejection)));
        Assert.NotNull(entity.FindProperty(nameof(IncomingNachaIntegrationExecution.IsTechnicalFailure)));
        Assert.NotNull(entity.FindProperty(nameof(IncomingNachaIntegrationExecution.TechnicalException)));
        Assert.True(entity.FindProperty(nameof(IncomingNachaIntegrationExecution.EntryDetailId))!.IsNullable);
        Assert.False(entity.GetForeignKeys().Single(x => x.PrincipalEntityType.ClrType == typeof(EntryDetail)).IsRequired);
        Assert.Contains(entity.GetIndexes(), x => x.Properties.Any(p => p.Name == nameof(IncomingNachaIntegrationExecution.CorrelationId)));
        Assert.Contains(entity.GetIndexes(), x => x.Properties.Any(p => p.Name == nameof(IncomingNachaIntegrationExecution.DispatchQueueId)));
        Assert.Contains(entity.GetIndexes(), x => x.Properties.Any(p => p.Name == nameof(IncomingNachaIntegrationExecution.SoapMethodName)));
        Assert.Contains(entity.GetIndexes(), x => x.Properties.Any(p => p.Name == nameof(IncomingNachaIntegrationExecution.StartedAtUtc)));
        Assert.Contains(entity.GetIndexes(), x => x.Properties.Any(p => p.Name == nameof(IncomingNachaIntegrationExecution.SoapResponseCode)));
        Assert.Contains(entity.GetIndexes(), x => x.Properties.Any(p => p.Name == nameof(IncomingNachaIntegrationExecution.SoapTechnicalStatus)));
    }

    [Fact]
    public async Task ExecuteAsync_SetsFailedFinal_WhenRetryableButMaxAttemptsExceeded()
    {
        await using var context = BuildContext();
        SeedDispatchItem(context);
        var queue = await context.IncomingNachaDispatchQueue.FirstAsync();
        queue.AttemptCount = 1;
        await context.SaveChangesAsync();

        var mappingIdentity = BuildMappingIdentity();
        var mapper = BuildMapperSuccess(mappingIdentity.MappingSetId, mappingIdentity.Version, mappingIdentity.SnapshotHash);
        var soap = new Mock<IWscfaachSoapClient>();
        soap.Setup(x => x.ProcTransaccionesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<Envelope><Body><Fault><faultstring>SOAP timeout</faultstring></Fault></Body></Envelope>");
        var operationResolver = BuildProcTransaccionesOperationResolver();
        var readiness = BuildProcTransaccionesReadinessService(mappingIdentity.MappingSetId, mappingIdentity.Version, mappingIdentity.SnapshotHash);

        var sut = new IncomingNachaPostProcessingOrchestrator(
            context,
            mapper.Object,
            new ProcTransaccionesResponseParser(),
            soap.Object,
            resilienceOptions: Options.Create(new IncomingNachaDispatchResilienceOptions
            {
                MaxAttempts = 2,
                InitialBackoffSeconds = 1,
                MaxBackoffSeconds = 10
            }),
            dispatchOptions: LiveProcTransaccionesOptions(),
            operationResolver: operationResolver.Object,
            mappingReadinessService: readiness.Object);

        var result = await sut.ExecuteAsync(50, "tester");

        Assert.Equal(1, result.FailedFinal);
        queue = await context.IncomingNachaDispatchQueue.FirstAsync();
        Assert.Equal(IncomingNachaDispatchQueueStatus.FailedFinal, queue.QueueStatus);
        Assert.Equal("ITIMEOUT", queue.LastErrorCode);
        Assert.Null(queue.NextAttemptAtUtc);
        Assert.True(await context.IncomingNachaProcessingEvents.AnyAsync(x => x.EventType == "MaxAttemptsExceeded"));
    }

    [Fact]
    public async Task ExecuteAsync_ReleasesWaitingWindowItems_WhenDue()
    {
        await using var context = BuildContext();
        SeedDispatchItem(context);
        var clock = TestSupport.TestClock.Create();
        var queue = await context.IncomingNachaDispatchQueue.FirstAsync();
        var cycle = await context.AchCycles.SingleAsync(x => x.Id == queue.AchCycleId);
        cycle.ProcessingDate = TestSupport.TestClock.OperationalDate;
        cycle.StartTime = new TimeSpan(8, 0, 0);
        cycle.EndTime = new TimeSpan(16, 0, 0);
        queue.QueueStatus = IncomingNachaDispatchQueueStatus.WaitingWindow;
        queue.NextAttemptAtUtc = clock.UtcNow.UtcDateTime.AddMinutes(-2);
        await context.SaveChangesAsync();

        var mappingIdentity = BuildMappingIdentity();
        var mapper = BuildMapperSuccess(mappingIdentity.MappingSetId, mappingIdentity.Version, mappingIdentity.SnapshotHash);
        var soap = new Mock<IWscfaachSoapClient>();
        soap.Setup(x => x.ProcTransaccionesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<Envelope><Body><Proc_TransaccionesResponse><RTAACH>00</RTAACH><RTALOC>OK</RTALOC></Proc_TransaccionesResponse></Body></Envelope>");
        var operationResolver = BuildProcTransaccionesOperationResolver();
        var readiness = BuildProcTransaccionesReadinessService(mappingIdentity.MappingSetId, mappingIdentity.Version, mappingIdentity.SnapshotHash);

        var sut = new IncomingNachaPostProcessingOrchestrator(
            context,
            mapper.Object,
            new ProcTransaccionesResponseParser(),
            soap.Object,
            operationResolver: operationResolver.Object,
            mappingReadinessService: readiness.Object,
            dispatchOptions: LiveProcTransaccionesOptions(),
            responseCatalogResolver: Catalog(Success("00", "Crédito aplicado")),
            timeProvider: clock);

        var result = await sut.ExecuteAsync(50, "tester");
        var secondResult = await sut.ExecuteAsync(50, "tester");

        Assert.Equal(1, result.WaitingWindow);
        Assert.Equal(1, result.Confirmed);
        Assert.Equal(0, secondResult.WaitingWindow);
        Assert.Equal(0, secondResult.Confirmed);
        queue = await context.IncomingNachaDispatchQueue.SingleAsync();
        Assert.Equal(IncomingNachaDispatchQueueStatus.Confirmed, queue.QueueStatus);
        soap.Verify(
            client => client.ProcTransaccionesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ReleasesWaitingWindowItem_AtExactWindowBoundary()
    {
        await using var context = BuildContext();
        SeedDispatchItem(context);
        var clock = TestSupport.TestClock.Create();
        var queue = await context.IncomingNachaDispatchQueue.FirstAsync();
        var cycle = await context.AchCycles.SingleAsync(x => x.Id == queue.AchCycleId);
        cycle.ProcessingDate = TestSupport.TestClock.OperationalDate;
        cycle.StartTime = new TimeSpan(12, 0, 0);
        cycle.EndTime = new TimeSpan(12, 0, 0);
        queue.QueueStatus = IncomingNachaDispatchQueueStatus.WaitingWindow;
        queue.NextAttemptAtUtc = clock.UtcNow.UtcDateTime;
        await context.SaveChangesAsync();

        var mappingIdentity = BuildMappingIdentity();
        var mapper = BuildMapperSuccess(mappingIdentity.MappingSetId, mappingIdentity.Version, mappingIdentity.SnapshotHash);
        var soap = new Mock<IWscfaachSoapClient>();
        soap.Setup(client => client.ProcTransaccionesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<Envelope><Body><Proc_TransaccionesResponse><RTAACH>00</RTAACH><RTALOC>OK</RTALOC></Proc_TransaccionesResponse></Body></Envelope>");

        var sut = new IncomingNachaPostProcessingOrchestrator(
            context,
            mapper.Object,
            new ProcTransaccionesResponseParser(),
            soap.Object,
            operationResolver: BuildProcTransaccionesOperationResolver().Object,
            mappingReadinessService: BuildProcTransaccionesReadinessService(
                mappingIdentity.MappingSetId,
                mappingIdentity.Version,
                mappingIdentity.SnapshotHash).Object,
            dispatchOptions: LiveProcTransaccionesOptions(),
            responseCatalogResolver: Catalog(Success("00", "Crédito aplicado")),
            timeProvider: clock);

        var result = await sut.ExecuteAsync(50, "tester");

        Assert.Equal(1, result.WaitingWindow);
        Assert.Equal(1, result.Confirmed);
        Assert.Equal(IncomingNachaDispatchQueueStatus.Confirmed,
            (await context.IncomingNachaDispatchQueue.SingleAsync()).QueueStatus);
    }

    [Fact]
    public async Task ExecuteAsync_ReleasesMultipleDueItems_FromDifferentCyclesAndClearingHouses()
    {
        await using var context = BuildContext();
        SeedDispatchItem(context);
        SeedSecondDispatchItem(context);
        var clock = TestSupport.TestClock.Create();
        var cycles = await context.AchCycles.ToListAsync();
        foreach (var cycle in cycles)
        {
            cycle.ProcessingDate = TestSupport.TestClock.OperationalDate;
            cycle.StartTime = new TimeSpan(8, 0, 0);
            cycle.EndTime = new TimeSpan(16, 0, 0);
        }
        var queues = await context.IncomingNachaDispatchQueue.ToListAsync();
        foreach (var queue in queues)
        {
            queue.QueueStatus = IncomingNachaDispatchQueueStatus.WaitingWindow;
            queue.NextAttemptAtUtc = clock.UtcNow.UtcDateTime.AddMinutes(-1);
        }
        await context.SaveChangesAsync();

        var mappingIdentity = BuildMappingIdentity();
        var mapper = BuildMapperSuccess(mappingIdentity.MappingSetId, mappingIdentity.Version, mappingIdentity.SnapshotHash);
        var soap = new Mock<IWscfaachSoapClient>();
        soap.Setup(client => client.ProcTransaccionesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<Envelope><Body><Proc_TransaccionesResponse><RTAACH>00</RTAACH><RTALOC>OK</RTALOC></Proc_TransaccionesResponse></Body></Envelope>");
        var sut = new IncomingNachaPostProcessingOrchestrator(
            context,
            mapper.Object,
            new ProcTransaccionesResponseParser(),
            soap.Object,
            operationResolver: BuildProcTransaccionesOperationResolver().Object,
            mappingReadinessService: BuildProcTransaccionesReadinessService(
                mappingIdentity.MappingSetId,
                mappingIdentity.Version,
                mappingIdentity.SnapshotHash).Object,
            dispatchOptions: LiveProcTransaccionesOptions(),
            responseCatalogResolver: Catalog(Success("00", "Crédito aplicado")),
            timeProvider: clock);

        var result = await sut.ExecuteAsync(50, "tester");

        Assert.Equal(2, result.WaitingWindow);
        Assert.Equal(2, result.Confirmed);
        Assert.All(
            await context.IncomingNachaDispatchQueue.ToListAsync(),
            queue => Assert.Equal(IncomingNachaDispatchQueueStatus.Confirmed, queue.QueueStatus));
        soap.Verify(
            client => client.ProcTransaccionesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
    }

    [Fact]
    public async Task ExecuteAsync_IgnoresAlreadyReleasedAndNonApplicableState_Idempotently()
    {
        await using var context = BuildContext();
        SeedDispatchItem(context);
        var queue = await context.IncomingNachaDispatchQueue.FirstAsync();
        queue.QueueStatus = IncomingNachaDispatchQueueStatus.Confirmed;
        queue.NextAttemptAtUtc = null;
        await context.SaveChangesAsync();

        var mapper = new Mock<IProcTransaccionesRequestMapper>(MockBehavior.Strict);
        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);
        var sut = new IncomingNachaPostProcessingOrchestrator(
            context,
            mapper.Object,
            new ProcTransaccionesResponseParser(),
            soap.Object,
            timeProvider: TestSupport.TestClock.Create());

        var firstResult = await sut.ExecuteAsync(50, "tester");
        var secondResult = await sut.ExecuteAsync(50, "tester");

        Assert.Equal(0, firstResult.WaitingWindow);
        Assert.Equal(0, firstResult.Picked);
        Assert.Equal(0, secondResult.WaitingWindow);
        Assert.Equal(0, secondResult.Picked);
        Assert.Equal(IncomingNachaDispatchQueueStatus.Confirmed,
            (await context.IncomingNachaDispatchQueue.SingleAsync()).QueueStatus);
        soap.Verify(
            client => client.ProcTransaccionesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_NullWaitingTimestampBeforeWindow_IsScheduledAndNotReleased()
    {
        await using var context = BuildContext();
        SeedDispatchItem(context);
        var queue = await context.IncomingNachaDispatchQueue.FirstAsync();
        var cycle = await context.AchCycles.SingleAsync(x => x.Id == queue.AchCycleId);
        cycle.ProcessingDate = TestSupport.TestClock.OperationalDate;
        cycle.StartTime = new TimeSpan(13, 0, 0);
        cycle.EndTime = new TimeSpan(14, 0, 0);
        queue.QueueStatus = IncomingNachaDispatchQueueStatus.WaitingWindow;
        queue.NextAttemptAtUtc = null;
        await context.SaveChangesAsync();

        var mapper = new Mock<IProcTransaccionesRequestMapper>(MockBehavior.Strict);
        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);
        var sut = new IncomingNachaPostProcessingOrchestrator(
            context,
            mapper.Object,
            new ProcTransaccionesResponseParser(),
            soap.Object,
            timeProvider: TestSupport.TestClock.Create());

        var result = await sut.ExecuteAsync(50, "tester");

        queue = await context.IncomingNachaDispatchQueue.FirstAsync();
        Assert.Equal(0, result.Picked);
        Assert.Equal(IncomingNachaDispatchQueueStatus.WaitingWindow, queue.QueueStatus);
        Assert.Equal(new DateTime(2026, 7, 24, 18, 0, 0, DateTimeKind.Utc), queue.NextAttemptAtUtc);
        Assert.True(await context.IncomingNachaProcessingEvents.AnyAsync(x => x.EventType == "DispatchWindowScheduled"));
        soap.Verify(x => x.ProcTransaccionesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ExpiredWaitingWindow_BlocksWithoutDispatch()
    {
        await using var context = BuildContext();
        SeedDispatchItem(context);
        var queue = await context.IncomingNachaDispatchQueue.FirstAsync();
        var cycle = await context.AchCycles.SingleAsync(x => x.Id == queue.AchCycleId);
        cycle.ProcessingDate = TestSupport.TestClock.OperationalDate;
        cycle.StartTime = new TimeSpan(8, 0, 0);
        cycle.EndTime = new TimeSpan(10, 0, 0);
        queue.QueueStatus = IncomingNachaDispatchQueueStatus.WaitingWindow;
        queue.NextAttemptAtUtc = null;
        await context.SaveChangesAsync();

        var mapper = new Mock<IProcTransaccionesRequestMapper>(MockBehavior.Strict);
        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);
        var sut = new IncomingNachaPostProcessingOrchestrator(
            context,
            mapper.Object,
            new ProcTransaccionesResponseParser(),
            soap.Object,
            timeProvider: TestSupport.TestClock.Create());

        var result = await sut.ExecuteAsync(50, "tester");

        queue = await context.IncomingNachaDispatchQueue.FirstAsync();
        Assert.Equal(0, result.Picked);
        Assert.Equal(1, result.Blocked);
        Assert.Equal(IncomingNachaDispatchQueueStatus.Blocked, queue.QueueStatus);
        Assert.Equal("WINDOW_EXPIRED", queue.LastErrorCode);
        Assert.Null(queue.NextAttemptAtUtc);
        Assert.True(await context.IncomingNachaProcessingEvents.AnyAsync(x => x.EventType == "DispatchWindowExpired"));
        soap.Verify(x => x.ProcTransaccionesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static void SeedDispatchItem(AchDbContext context)
    {
        context.ClearingHouseConfigs.Add(new ClearingHouseConfig { Id = 1, ClearingHouseId = 1, HolidayStrategy = "Colombian" });
        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = 1,
            Name = "ACH Colombia",
            Code = "ACH",
            OriginCode = "12345678",
            ClearingHouseId = 1
        });
        var companyEntryDescriptionId = context.CompanyEntryDescriptionCatalogs
            .Where(x => x.Term == "PAGOS")
            .Select(x => x.Id)
            .FirstOrDefault();
        if (companyEntryDescriptionId == 0)
        {
            companyEntryDescriptionId = 999;
            context.CompanyEntryDescriptionCatalogs.Add(new CompanyEntryDescriptionCatalog
            {
                Id = companyEntryDescriptionId,
                Term = "PAGOS",
                Description = "Pagos",
                StandardEntryClassCode = "PPD",
                IsActive = true
            });
        }
        var fi = new FinancialInstitution
        {
            Id = 1,
            Name = "Banco Test",
            RoutingNumber = "12345",
            TransitCode = "678",
            IsDefaultSource = true,
            Status = FinancialInstitutionStatus.Active
        };
        fi.CalculateCheckDigit();
        context.FinancialInstitutions.Add(fi);

        var ingestion = new IncomingNachaFileIngestion
        {
            Id = Guid.NewGuid(),
            FileName = "in.ach",
            FileHashSha256 = "h",
            FileSize = 1,
            ContentType = "text/plain",
            UploadedBy = "tester",
            CorrelationId = "c",
            Notes = "n"
        };
        var cycle = new AchCycle
        {
            Id = "C1",
            CycleName = "c1",
            ClearingHouseId = 1,
            ProcessingDate = DateTime.Today,
            StartTime = TimeSpan.Zero,
            EndTime = new TimeSpan(23, 59, 0),
            CutoffTime = new TimeSpan(23, 0, 0)
        };
        context.AchCycles.Add(cycle);
        context.AchBatches.Add(new AchBatch { Id = 1, AchCycleId = "C1", CompanyEntryDescriptionId = companyEntryDescriptionId, EffectiveEntryDate = DateTime.Today });
        var tx = new AchTransaction
        {
            Id = 100,
            Amount = 100m,
            TransactionExternalId = "EXT-1",
            Reference = "R",
            Type = TransactionTypeEnum.Credit,
            TransactionCode = "22",
            SourceAccountNumber = "S",
            DestinationAccountNumber = "D",
            SourceInstitutionId = 1,
            DestinationInstitutionId = 1,
            OriginatingDFI = "11111111",
            ReceivingDFI = "222222220",
            TraceNumber = "123456789012345",
            CompanyName = "C",
            CompanyIdentification = "I",
            AchCycleId = "C1",
            AchBatchId = 1,
            EffectiveEntryDate = DateTime.Today
        };
        context.AchTransactions.Add(tx);
        context.EntryDetails.Add(new EntryDetail
        {
            EntryDetailID = 1,
            TransactionCode = "22",
            ReceivingParticipantEntityCode = "22222222",
            AccountNumber = "D",
            Amount = 100m,
            RecipUserName = "Receiver"
        });
        var classification = new IncomingNachaEntryClassification { Id = Guid.NewGuid(), IncomingNachaFileIngestionId = ingestion.Id, EntryDetailId = 1 };
        var link = new IncomingNachaTransactionLink { Id = Guid.NewGuid(), IncomingNachaFileIngestionId = ingestion.Id, EntryDetailId = 1, AchTransactionId = tx.Id, IsFinal = true, LinkType = IncomingNachaLinkType.ExactTrace15 };

        context.IncomingNachaFileIngestions.Add(ingestion);
        context.IncomingNachaEntryClassifications.Add(classification);
        context.IncomingNachaTransactionLinks.Add(link);
        context.IncomingNachaDispatchQueue.Add(new IncomingNachaDispatchQueue
        {
            Id = Guid.NewGuid(),
            IncomingNachaFileIngestionId = ingestion.Id,
            IncomingNachaEntryClassificationId = classification.Id,
            IncomingNachaTransactionLinkId = link.Id,
            AchTransactionId = tx.Id,
            AchCycleId = "C1",
            ClearingHouseId = 1,
            OperationalDate = DateTime.Today,
            QueueStatus = IncomingNachaDispatchQueueStatus.Queued,
            Priority = 100,
            IdempotencyDispatchKey = Guid.NewGuid().ToString("N"),
            NextAttemptAtUtc = DateTime.UtcNow.AddMinutes(-1)
        });
        context.SaveChanges();
    }

    private static void SeedSecondDispatchItem(AchDbContext context)
    {
        var ingestion = context.IncomingNachaFileIngestions.Single();
        var companyEntryDescriptionId = context.AchBatches
            .Where(batch => batch.Id == 1)
            .Select(batch => batch.CompanyEntryDescriptionId)
            .Single();
        context.ClearingHouseConfigs.Add(new ClearingHouseConfig { Id = 2, ClearingHouseId = 2, HolidayStrategy = "Colombian" });
        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = 2,
            Name = "CENIT",
            Code = "CENIT",
            OriginCode = "87654321",
            ClearingHouseId = 2
        });
        context.AchCycles.Add(new AchCycle
        {
            Id = "C2",
            CycleName = "c2",
            ClearingHouseId = 2,
            ProcessingDate = TestSupport.TestClock.OperationalDate,
            StartTime = new TimeSpan(8, 0, 0),
            EndTime = new TimeSpan(16, 0, 0),
            CutoffTime = new TimeSpan(15, 0, 0)
        });
        context.AchBatches.Add(new AchBatch
        {
            Id = 2,
            AchCycleId = "C2",
            CompanyEntryDescriptionId = companyEntryDescriptionId,
            EffectiveEntryDate = TestSupport.TestClock.OperationalDate
        });
        var transaction = new AchTransaction
        {
            Id = 101,
            Amount = 200m,
            TransactionExternalId = "EXT-2",
            Reference = "R2",
            Type = TransactionTypeEnum.Credit,
            TransactionCode = "22",
            SourceAccountNumber = "S2",
            DestinationAccountNumber = "D2",
            SourceInstitutionId = 1,
            DestinationInstitutionId = 1,
            OriginatingDFI = "11111111",
            ReceivingDFI = "222222220",
            TraceNumber = "123456789012346",
            CompanyName = "C2",
            CompanyIdentification = "I2",
            AchCycleId = "C2",
            AchBatchId = 2,
            EffectiveEntryDate = TestSupport.TestClock.OperationalDate
        };
        context.AchTransactions.Add(transaction);
        context.EntryDetails.Add(new EntryDetail
        {
            EntryDetailID = 2,
            TransactionCode = "22",
            ReceivingParticipantEntityCode = "22222222",
            AccountNumber = "D2",
            Amount = 200m,
            RecipUserName = "Receiver 2"
        });
        var classification = new IncomingNachaEntryClassification
        {
            Id = Guid.NewGuid(),
            IncomingNachaFileIngestionId = ingestion.Id,
            EntryDetailId = 2
        };
        var link = new IncomingNachaTransactionLink
        {
            Id = Guid.NewGuid(),
            IncomingNachaFileIngestionId = ingestion.Id,
            EntryDetailId = 2,
            AchTransactionId = transaction.Id,
            IsFinal = true,
            LinkType = IncomingNachaLinkType.ExactTrace15
        };
        context.IncomingNachaEntryClassifications.Add(classification);
        context.IncomingNachaTransactionLinks.Add(link);
        context.IncomingNachaDispatchQueue.Add(new IncomingNachaDispatchQueue
        {
            Id = Guid.NewGuid(),
            IncomingNachaFileIngestionId = ingestion.Id,
            IncomingNachaEntryClassificationId = classification.Id,
            IncomingNachaTransactionLinkId = link.Id,
            AchTransactionId = transaction.Id,
            AchCycleId = "C2",
            ClearingHouseId = 2,
            OperationalDate = TestSupport.TestClock.OperationalDate,
            QueueStatus = IncomingNachaDispatchQueueStatus.WaitingWindow,
            Priority = 101,
            IdempotencyDispatchKey = Guid.NewGuid().ToString("N"),
            NextAttemptAtUtc = TestSupport.TestClock.UtcNow.UtcDateTime.AddMinutes(-1)
        });
        context.SaveChanges();
    }

    private static AchDbContext BuildContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new AchDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static Mock<IProcTransaccionesRequestMapper> BuildMapperSuccess(
        Guid? mappingSetId = null,
        int mappingVersion = 1,
        string mappingSnapshotHash = "hash")
    {
        var identity = mappingSetId ?? BuildMappingIdentity().MappingSetId;
        var mapper = new Mock<IProcTransaccionesRequestMapper>();
        mapper.Setup(x => x.ResolveAsync(
                It.IsAny<IncomingNachaDispatchQueue>(),
                It.IsAny<IncomingNachaFileIngestion>(),
                It.IsAny<IncomingNachaEntryClassification>(),
                It.IsAny<AchTransaction>(),
                It.IsAny<AchCycle>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ProcTransaccionesRequestResolution(
                new ProcTransaccionesRequestContract(new Dictionary<string, string> { ["TREG"] = "6", ["TIPTRAN"] = "22", ["MONTO"] = "10", ["IDTRAN"] = "1", ["IDCAMCOMPE"] = "1" }),
                identity,
                mappingVersion,
                mappingSnapshotHash));
        mapper.Setup(x => x.BuildSoapBody(It.IsAny<ProcTransaccionesRequestContract>()))
            .Returns("<Proc_Transacciones><IDTRAN>1</IDTRAN></Proc_Transacciones>");
        return mapper;
    }

    private static (Guid MappingSetId, int Version, string SnapshotHash) BuildMappingIdentity()
        => (Guid.Parse("11111111-1111-1111-1111-111111111111"), 7, "HASH-MATCH");

    private static Mock<ITransactionIntegrationOperationResolver> BuildProcTransaccionesOperationResolver()
    {
        var operationResolver = new Mock<ITransactionIntegrationOperationResolver>();
        operationResolver.Setup(x => x.ResolveAsync(It.IsAny<AchTransaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ProcTransaccionesOperation());
        return operationResolver;
    }

    private static Mock<IIntegrationMappingReadinessService> BuildProcTransaccionesReadinessService(
        Guid mappingSetId,
        int mappingVersion,
        string mappingSnapshotHash)
    {
        var readiness = new Mock<IIntegrationMappingReadinessService>();
        readiness.Setup(x => x.EvaluateAsync(It.IsAny<TransactionIntegrationOperationResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(BuildReadiness(mappingSetId, mappingVersion, mappingSnapshotHash));
        return readiness;
    }

    private static ProcTransaccionesRequestResolution BuildResolution(Guid mappingSetId, int version, string snapshotHash)
        => new(
            new ProcTransaccionesRequestContract(new Dictionary<string, string> { ["TREG"] = "6", ["TIPTRAN"] = "22", ["MONTO"] = "10", ["IDTRAN"] = "1", ["IDCAMCOMPE"] = "1" }),
            mappingSetId,
            version,
            snapshotHash);

    private static IntegrationMappingReadinessResult BuildReadiness(Guid? mappingSetId, int? mappingVersion, string? mappingSnapshotHash)
        => new IntegrationMappingReadinessResult(
            true,
            "Ok",
            "OK",
            IntegrationGuaranteeConstants.Wscfaach,
            IntegrationGuaranteeConstants.ProcTransacciones,
            IntegrationGuaranteeConstants.MonetaryCreditRequest,
            IntegrationGuaranteeConstants.OutboundRequest,
            5,
            5,
            [],
            [],
            [],
            [],
            false,
            true,
            [],
            [])
        {
            MappingSetId = mappingSetId,
            MappingVersion = mappingVersion,
            MappingSnapshotHash = mappingSnapshotHash
        };

    private static ISoapIntegrationSettingsService SoapSettingsService(string endpoint)
    {
        var settings = new Mock<ISoapIntegrationSettingsService>();
        settings.Setup(x => x.GetAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SoapIntegrationSettingsDto
            {
                WscfaachMappings =
                [
                    new SoapEndpointMethodMappingDto
                    {
                        MethodName = "Proc_Transacciones",
                        Enabled = true,
                        Endpoint = endpoint,
                        SoapAction = "http://tempuri.org/IWSCFAACH/Proc_Transacciones"
                    }
                ]
            });

        return settings.Object;
    }

    private static IOptions<ProcTransaccionesDispatchOptions> LiveProcTransaccionesOptions()
        => Options.Create(new ProcTransaccionesDispatchOptions { Mode = "Live" });

    private static IIntegrationResponseCatalogResolver Catalog(params IntegrationResponseCatalogResult[] results)
        => new StubResponseCatalogResolver(results);

    private static IntegrationResponseCatalogResult Success(string code, string description)
        => new(
            null,
            code,
            description,
            IntegrationResponseCategory.CoreSoapResponse,
            IntegrationResponseCategory.CoreSoapResponse,
            "Proc_Transacciones",
            IntegrationResponseBusinessStatus.Success,
            false,
            false,
            true,
            "AppliedTacitly",
            true);

    private sealed class StubResponseCatalogResolver(IEnumerable<IntegrationResponseCatalogResult> results)
        : IIntegrationResponseCatalogResolver
    {
        private readonly IReadOnlyDictionary<string, IntegrationResponseCatalogResult> _results =
            results.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);

        public Task<IntegrationResponseCatalogResult> ResolveAsync(
            string source,
            string method,
            string? responseCode,
            DateTime processedAtUtc,
            CancellationToken ct = default)
        {
            var code = responseCode?.Trim() ?? string.Empty;
            return Task.FromResult(_results.TryGetValue(code, out var result)
                ? result
                : new IntegrationResponseCatalogResult(
                    null,
                    code,
                    "Código pendiente de parametrización",
                    source,
                    IntegrationResponseCategory.CoreSoapResponse,
                    method,
                    IntegrationResponseBusinessStatus.PendingCatalog,
                    false,
                    true,
                    false,
                    string.Empty,
                    false));
        }
    }

    private static TransactionIntegrationOperationResult ProcTransaccionesOperation()
        => new(
            100,
            "R",
            IntegrationGuaranteeConstants.Wscfaach,
            IntegrationGuaranteeConstants.ProcTransacciones,
            IntegrationGuaranteeConstants.MonetaryCreditRequest,
            IntegrationGuaranteeConstants.OutboundRequest,
            "Credito monetario",
            "Entidad financiera externa; CFA receptora",
            true,
            "Credito monetario originado por otra entidad financiera.",
            true,
            []);

    private static TransactionIntegrationOperationResult ProcContrapartidasOperation()
        => new(
            100,
            "R",
            IntegrationGuaranteeConstants.Wscfaach,
            IntegrationGuaranteeConstants.ProcContrapartidas,
            IntegrationGuaranteeConstants.MonetaryDebitRequest,
            IntegrationGuaranteeConstants.OutboundRequest,
            "Debito monetario",
            "Entidad financiera originada por CFA",
            true,
            "Debito monetario originado por CFA.",
            true,
            []);

    private static IntegrationMappingReadinessResult FailedProcTransaccionesReadiness()
        => new(
            false,
            "Failed",
            "PROC_TRANSACCIONES_REQUIRED_MAPPING_MISSING",
            IntegrationGuaranteeConstants.Wscfaach,
            IntegrationGuaranteeConstants.ProcTransacciones,
            IntegrationGuaranteeConstants.MonetaryCreditRequest,
            IntegrationGuaranteeConstants.OutboundRequest,
            5,
            0,
            ["IDTRAN"],
            [],
            [],
            [],
            false,
            false,
            ["Falta mapping requerido."],
            []);
}
