using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.External.Connections;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Application.Security.Dtos;
using Cfa.ACHInterbank.Application.Security.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
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

        var mapper = BuildMapperSuccess();
        const string requestXml = "<Proc_Transacciones><IDTRAN>1</IDTRAN></Proc_Transacciones>";
        mapper.Setup(x => x.BuildSoapBody(It.IsAny<ProcTransaccionesRequestContract>())).Returns(requestXml);

        const string responseXml = "<Envelope><Body><Proc_TransaccionesResponse><RTAACH>R96</RTAACH><RTALOC>OK</RTALOC></Proc_TransaccionesResponse></Body></Envelope>";
        var soap = new Mock<IWscfaachSoapClient>();
        soap.Setup(x => x.ProcTransaccionesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(responseXml);

        var sut = new IncomingNachaPostProcessingOrchestrator(
            context,
            mapper.Object,
            new ProcTransaccionesResponseParser(),
            soap.Object,
            dispatchOptions: LiveProcTransaccionesOptions(),
            soapIntegrationSettingsService: SoapSettingsService("http://localhost:7083/WSCFAACH.svc"));

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
        Assert.Equal("OK", execution.SoapResponseDescription);
        Assert.Equal("Succeeded", execution.SoapTechnicalStatus);
        Assert.True(execution.IsSuccessful);
        Assert.False(execution.IsFunctionalRejection);
        Assert.False(execution.IsTechnicalFailure);
        Assert.Equal("R96", execution.ResponseCode);
        Assert.Equal("OK", execution.ResponseMessage);
        Assert.DoesNotContain("<METODO>", execution.RequestPayloadXml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Proc_Contrapartidas", execution.RequestPayloadXml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("RegistrarRespuestaTransaccion", execution.RequestPayloadXml, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("PLValidarUsuarioBV", execution.RequestPayloadXml, StringComparison.OrdinalIgnoreCase);
    }
    [Fact]
    public async Task ExecuteAsync_BlocksQueue_WhenMappingIsInvalid()
    {
        await using var context = BuildContext();
        SeedDispatchItem(context);

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

        var sut = new IncomingNachaPostProcessingOrchestrator(
            context,
            mapper.Object,
            new ProcTransaccionesResponseParser(),
            Mock.Of<IWscfaachSoapClient>());

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

        var mapper = BuildMapperSuccess();
        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);

        var sut = new IncomingNachaPostProcessingOrchestrator(
            context,
            mapper.Object,
            new ProcTransaccionesResponseParser(),
            soap.Object,
            dispatchOptions: Options.Create(new ProcTransaccionesDispatchOptions { Mode = "DryRun" }));

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
    }

    [Fact]
    public async Task ProcTransacciones_DisabledMode_ShouldBlockControlledAndNotInvokeSoap()
    {
        await using var context = BuildContext();
        SeedDispatchItem(context);

        var mapper = BuildMapperSuccess();
        var soap = new Mock<IWscfaachSoapClient>(MockBehavior.Strict);

        var sut = new IncomingNachaPostProcessingOrchestrator(
            context,
            mapper.Object,
            new ProcTransaccionesResponseParser(),
            soap.Object,
            dispatchOptions: Options.Create(new ProcTransaccionesDispatchOptions { Mode = "Disabled" }));

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
    public async Task ExecuteAsync_SetsRetryPending_WhenTechnicalErrorOccurs()
    {
        await using var context = BuildContext();
        SeedDispatchItem(context);

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
                Guid.NewGuid(),
                1,
                "hash"));
        mapper.Setup(x => x.BuildSoapBody(It.IsAny<ProcTransaccionesRequestContract>())).Returns("<request/>");

        var soap = new Mock<IWscfaachSoapClient>();
        soap.Setup(x => x.ProcTransaccionesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("timeout"));

        var sut = new IncomingNachaPostProcessingOrchestrator(
            context,
            mapper.Object,
            new ProcTransaccionesResponseParser(),
            soap.Object,
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
    public async Task ExecuteAsync_SetsFailedFinal_AndStoresFunctionalRejectionAudit_WhenObservedFunctionalCodeOccurs()
    {
        await using var context = BuildContext();
        SeedDispatchItem(context);

        var mapper = BuildMapperSuccess();
        var soap = new Mock<IWscfaachSoapClient>();
        soap.Setup(x => x.ProcTransaccionesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<Envelope><Body><Proc_TransaccionesResponse><RTAACH>R17</RTAACH><RTALOC>Codigo funcional observado</RTALOC></Proc_TransaccionesResponse></Body></Envelope>");

        var sut = new IncomingNachaPostProcessingOrchestrator(
            context,
            mapper.Object,
            new ProcTransaccionesResponseParser(),
            soap.Object,
            dispatchOptions: LiveProcTransaccionesOptions());

        var result = await sut.ExecuteAsync(50, "tester");

        Assert.Equal(1, result.FailedFinal);
        var queue = await context.IncomingNachaDispatchQueue.FirstAsync();
        Assert.Equal(IncomingNachaDispatchQueueStatus.FailedFinal, queue.QueueStatus);
        Assert.Equal("IFUNC", queue.LastErrorCode);
        var execution = await context.IncomingNachaIntegrationExecution.FirstAsync();
        Assert.Equal("R17", execution.SoapResponseCode);
        Assert.Equal("Codigo funcional observado", execution.SoapResponseDescription);
        Assert.Equal("FunctionalRejection", execution.SoapTechnicalStatus);
        Assert.False(execution.IsSuccessful);
        Assert.True(execution.IsFunctionalRejection);
        Assert.False(execution.IsTechnicalFailure);
        Assert.Equal("R17", execution.ResponseCode);
        Assert.True(await context.IncomingNachaProcessingEvents.AnyAsync(x => x.EventType == "IntegrationNonRetryableFailed"));
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

        var mapper = BuildMapperSuccess();
        var soap = new Mock<IWscfaachSoapClient>();
        soap.Setup(x => x.ProcTransaccionesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<Envelope><Body><Fault><faultstring>SOAP timeout</faultstring></Fault></Body></Envelope>");

        var sut = new IncomingNachaPostProcessingOrchestrator(
            context,
            mapper.Object,
            new ProcTransaccionesResponseParser(),
            soap.Object,
            Options.Create(new IncomingNachaDispatchResilienceOptions
            {
                MaxAttempts = 2,
                InitialBackoffSeconds = 1,
                MaxBackoffSeconds = 10
            }),
            LiveProcTransaccionesOptions());

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
        var queue = await context.IncomingNachaDispatchQueue.FirstAsync();
        queue.QueueStatus = IncomingNachaDispatchQueueStatus.WaitingWindow;
        queue.NextAttemptAtUtc = DateTime.UtcNow.AddMinutes(-2);
        await context.SaveChangesAsync();

        var mapper = BuildMapperSuccess();
        var soap = new Mock<IWscfaachSoapClient>();
        soap.Setup(x => x.ProcTransaccionesAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("<Envelope><Body><Proc_TransaccionesResponse><RTAACH>00</RTAACH><RTALOC>OK</RTALOC></Proc_TransaccionesResponse></Body></Envelope>");

        var sut = new IncomingNachaPostProcessingOrchestrator(
            context,
            mapper.Object,
            new ProcTransaccionesResponseParser(),
            soap.Object,
            dispatchOptions: LiveProcTransaccionesOptions());

        var result = await sut.ExecuteAsync(50, "tester");

        Assert.Equal(1, result.WaitingWindow);
        Assert.Equal(1, result.Confirmed);
    }

    private static void SeedDispatchItem(AchDbContext context)
    {
        context.ClearingHouseConfigs.Add(new ClearingHouseConfig { Id = 1, HolidayStrategy = "Colombian" });
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

    private static Mock<IProcTransaccionesRequestMapper> BuildMapperSuccess()
    {
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
                Guid.NewGuid(),
                1,
                "hash"));
        mapper.Setup(x => x.BuildSoapBody(It.IsAny<ProcTransaccionesRequestContract>()))
            .Returns("<Proc_Transacciones><IDTRAN>1</IDTRAN></Proc_Transacciones>");
        return mapper;
    }

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
