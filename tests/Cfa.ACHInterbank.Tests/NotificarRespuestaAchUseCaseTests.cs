using Cfa.ACHInterbank.Application.ACH.Responses.Models;
using Cfa.ACHInterbank.Application.ACH.Responses.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Notification.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Notification.Models;
using Cfa.ACHInterbank.Application.ACH.Responses.Notification.Services;
using Cfa.ACHInterbank.Application.ACH.Responses.Repositories;
using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Moq;
using System.Text.Json;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class NotificarRespuestaAchUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationFailure_WhenAttemptIdInvalid()
    {
        var sut = BuildSut(out var attemptRepo, out var gateway, out var uow, out _);
        var result = await sut.ExecuteAsync(new NotificarRespuestaAchCommand(0, null));
        Assert.False(result.Procesada);
        attemptRepo.Verify(x => x.FindByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()), Times.Never);
        gateway.Verify(x => x.RegistrarRespuestaAsync(It.IsAny<RegistrarRespuestaAchCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnNotFound_WhenAttemptDoesNotExist()
    {
        var sut = BuildSut(out var attemptRepo, out var gateway, out var uow, out _);
        attemptRepo.Setup(x => x.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((AchResponseNotificationAttempt?)null);
        var result = await sut.ExecuteAsync(new NotificarRespuestaAchCommand(1, null));
        Assert.False(result.Encontrada);
        gateway.Verify(x => x.RegistrarRespuestaAsync(It.IsAny<RegistrarRespuestaAchCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnAlreadyProcessed_WhenAttemptIsNotPending()
    {
        var sut = BuildSut(out var attemptRepo, out var gateway, out var uow, out _);
        attemptRepo.Setup(x => x.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(BuildAttempt(AchResponseNotificationStatus.Exitosa));
        var result = await sut.ExecuteAsync(new NotificarRespuestaAchCommand(1, null));
        Assert.True(result.YaProcesada);
        gateway.Verify(x => x.RegistrarRespuestaAsync(It.IsAny<RegistrarRespuestaAchCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void ExecuteAsync_ShouldMapAttemptToRegistrarRespuestaAchCommand()
    {
        var mapper = new RegistrarRespuestaAchCommandMapper();
        var attempt = BuildAttempt(AchResponseNotificationStatus.Pendiente);
        var cmd = mapper.Map(attempt.AchResponse!, attempt);
        Assert.Equal(attempt.IdEstado, cmd.IdEstado);
        Assert.Equal(attempt.IdTransaccionServicioExterno, cmd.IdTransaccionServicioExterno);
        Assert.Equal(attempt.AchResponse!.CodigoCamaraCompensacion, cmd.CodigoCamaraCompensacion);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldMarkAttemptSuccessAndResponseNotificada_WhenGatewaySuccess()
    {
        var sut = BuildSut(out var attemptRepo, out var gateway, out var uow, out _);
        var attempt = BuildAttempt(AchResponseNotificationStatus.Pendiente);
        attemptRepo.Setup(x => x.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(attempt);
        gateway.Setup(x => x.RegistrarRespuestaAsync(It.IsAny<RegistrarRespuestaAchCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ResultadoRegistroRespuestaAch(false, null, null));

        var result = await sut.ExecuteAsync(new NotificarRespuestaAchCommand(1, null));

        Assert.True(result.Procesada);
        Assert.Equal(AchResponseNotificationStatus.Exitosa, attempt.EstadoNotificacion);
        Assert.Equal(AchResponseProcessingStatus.Notificada, attempt.AchResponse!.EstadoProcesamiento);
        Assert.NotNull(attempt.RequestPayload);
        Assert.NotNull(attempt.ResponsePayload);
        using (var requestJson = JsonDocument.Parse(attempt.RequestPayload!))
        {
            var names = requestJson.RootElement.EnumerateObject().Select(x => x.Name).ToArray();
            Assert.Equal(
                ["idCanal", "nombreCanal", "idTransaccion", "idEstado", "causal", "idTransaccionAxon", "descripcionCausal"],
                names);
        }
        uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldMarkFunctionalError_WhenGatewayReturnsExisteErrorTrue()
    {
        var sut = BuildSut(out var attemptRepo, out var gateway, out var uow, out _);
        var attempt = BuildAttempt(AchResponseNotificationStatus.Pendiente);
        attemptRepo.Setup(x => x.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(attempt);
        gateway.Setup(x => x.RegistrarRespuestaAsync(It.IsAny<RegistrarRespuestaAchCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ResultadoRegistroRespuestaAch(true, "E01", "Err"));

        var result = await sut.ExecuteAsync(new NotificarRespuestaAchCommand(1, null));

        Assert.True(result.ExisteError);
        Assert.Equal(AchResponseNotificationStatus.ErrorFuncional, attempt.EstadoNotificacion);
        Assert.Equal(AchResponseProcessingStatus.ErrorFuncional, attempt.AchResponse!.EstadoProcesamiento);
        uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRequireManualReview_AndBlockReplay_WhenGatewayTimesOut()
    {
        var sut = BuildSut(out var attemptRepo, out var gateway, out var uow, out _);
        var attempt = BuildAttempt(AchResponseNotificationStatus.Pendiente);
        attemptRepo.Setup(x => x.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(attempt);
        gateway.Setup(x => x.RegistrarRespuestaAsync(It.IsAny<RegistrarRespuestaAchCommand>(), It.IsAny<CancellationToken>())).ThrowsAsync(new TimeoutException("timeout"));

        var result = await sut.ExecuteAsync(new NotificarRespuestaAchCommand(1, null));

        Assert.True(result.ErrorTecnico);
        Assert.Equal(AchResponseNotificationStatus.RequiereRevisionManual, attempt.EstadoNotificacion);
        Assert.Equal(AchResponseProcessingStatus.RequiereRevisionManual, attempt.AchResponse!.EstadoProcesamiento);
        Assert.False(string.IsNullOrWhiteSpace(attempt.ErrorTecnico));
        Assert.NotNull(attempt.RequestPayload);
        Assert.Contains("ResultadoDesconocido", attempt.ResponsePayload!);
        uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);

        var replay = await sut.ExecuteAsync(new NotificarRespuestaAchCommand(1, null));

        Assert.True(replay.YaProcesada);
        gateway.Verify(x => x.RegistrarRespuestaAsync(It.IsAny<RegistrarRespuestaAchCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldKeepRetryPending_WhenGatewayFailsBeforeKnownDelivery()
    {
        var sut = BuildSut(out var attemptRepo, out var gateway, out var uow, out _);
        var attempt = BuildAttempt(AchResponseNotificationStatus.Pendiente);
        attemptRepo.Setup(x => x.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(attempt);
        gateway.Setup(x => x.RegistrarRespuestaAsync(It.IsAny<RegistrarRespuestaAchCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("endpoint configuration invalid"));

        var result = await sut.ExecuteAsync(new NotificarRespuestaAchCommand(1, null));

        Assert.True(result.ErrorTecnico);
        Assert.Equal(AchResponseNotificationStatus.ErrorTecnico, attempt.EstadoNotificacion);
        Assert.Equal(AchResponseProcessingStatus.PendienteReintento, attempt.AchResponse!.EstadoProcesamiento);
        Assert.Contains("ErrorTecnico", attempt.ResponsePayload!);
        uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarRespuestaTransaccion_ShouldFailControlled_WhenRequiredMappingMissing()
    {
        var attemptRepo = new Mock<IAchResponseNotificationAttemptRepository>();
        var gateway = new Mock<IRespuestaTransaccionesAchGateway>();
        var uow = new Mock<IUnitOfWork>();
        var responseRepo = new Mock<IAchResponseRepository>();
        var operationResolver = new Mock<ITransactionIntegrationOperationResolver>();
        var readiness = new Mock<IIntegrationMappingReadinessService>();
        var attempt = BuildAttempt(AchResponseNotificationStatus.Pendiente);

        operationResolver.Setup(x => x.ResolveDifferentialResponse(It.IsAny<string?>(), It.IsAny<int?>()))
            .Returns(new TransactionIntegrationOperationResult(
                null,
                "TX",
                IntegrationGuaranteeConstants.WsAxon,
                IntegrationGuaranteeConstants.RegistrarRespuestaTransaccion,
                IntegrationGuaranteeConstants.DifferentialResponseNotification,
                IntegrationGuaranteeConstants.InboundResponse,
                "Respuesta diferencial / notificacion",
                "Entidad/camara/proveedor externo",
                false,
                "Notificacion/respuesta diferencial no monetaria.",
                true,
                []));
        readiness.Setup(x => x.EvaluateAsync(It.IsAny<TransactionIntegrationOperationResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntegrationMappingReadinessResult(
                false,
                "Failed",
                "INTEGRATION_MAPPING_REQUIRED",
                IntegrationGuaranteeConstants.WsAxon,
                IntegrationGuaranteeConstants.RegistrarRespuestaTransaccion,
                IntegrationGuaranteeConstants.DifferentialResponseNotification,
                IntegrationGuaranteeConstants.InboundResponse,
                3,
                0,
                ["ANSIDTX"],
                [],
                [],
                [],
                false,
                false,
                ["Falta mapping requerido."],
                []));

        attemptRepo.Setup(x => x.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(attempt);
        var sut = new NotificarRespuestaAchUseCase(responseRepo.Object, attemptRepo.Object, new RegistrarRespuestaAchCommandMapper(), gateway.Object, uow.Object, operationResolver.Object, readiness.Object);

        var result = await sut.ExecuteAsync(new NotificarRespuestaAchCommand(1, null));

        Assert.True(result.ExisteError);
        Assert.Equal("INTEGRATION_MAPPING_REQUIRED", result.CodigoError);
        Assert.Equal(AchResponseNotificationStatus.ErrorFuncional, attempt.EstadoNotificacion);
        Assert.Equal(AchResponseProcessingStatus.ErrorFuncional, attempt.AchResponse!.EstadoProcesamiento);
        gateway.Verify(x => x.RegistrarRespuestaAsync(It.IsAny<RegistrarRespuestaAchCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarRespuestaTransaccion_ShouldPersistFieldByFieldTrace_BeforeGateway()
    {
        var attemptRepo = new Mock<IAchResponseNotificationAttemptRepository>();
        var gateway = new Mock<IRespuestaTransaccionesAchGateway>();
        var uow = new Mock<IUnitOfWork>();
        var responseRepo = new Mock<IAchResponseRepository>();
        var operationResolver = new Mock<ITransactionIntegrationOperationResolver>();
        var readiness = new Mock<IIntegrationMappingReadinessService>();
        var traceWriter = new Mock<IIntegrationMappingTraceWriter>();
        var attempt = BuildAttempt(AchResponseNotificationStatus.Pendiente);

        operationResolver.Setup(x => x.ResolveDifferentialResponse(It.IsAny<string?>(), It.IsAny<int?>()))
            .Returns(DifferentialOperation());
        readiness.Setup(x => x.EvaluateAsync(It.IsAny<TransactionIntegrationOperationResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ReadyDifferentialResponse());
        traceWriter.Setup(x => x.WriteAsync(
                It.IsAny<TransactionIntegrationOperationResult>(),
                It.IsAny<RegistrarRespuestaAchCommand>(),
                It.IsAny<int?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                true,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntegrationMappingTraceWriteResult(Guid.NewGuid(), 5, [], []));

        attemptRepo.Setup(x => x.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(attempt);
        gateway.Setup(x => x.RegistrarRespuestaAsync(It.IsAny<RegistrarRespuestaAchCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResultadoRegistroRespuestaAch(false, null, null));

        var sut = new NotificarRespuestaAchUseCase(responseRepo.Object, attemptRepo.Object, new RegistrarRespuestaAchCommandMapper(), gateway.Object, uow.Object, operationResolver.Object, readiness.Object, traceWriter.Object);

        var result = await sut.ExecuteAsync(new NotificarRespuestaAchCommand(1, "corr-uat"));

        Assert.True(result.Procesada);
        traceWriter.Verify(x => x.WriteAsync(
            It.Is<TransactionIntegrationOperationResult>(o => !o.MovesMoney && o.OperationKey == IntegrationGuaranteeConstants.RegistrarRespuestaTransaccion),
            It.IsAny<RegistrarRespuestaAchCommand>(),
            It.IsAny<int?>(),
            "TX",
            "corr-uat",
            true,
            false,
            It.IsAny<CancellationToken>()), Times.Once);
        gateway.Verify(x => x.RegistrarRespuestaAsync(It.IsAny<RegistrarRespuestaAchCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarRespuestaTransaccion_ShouldNotInvokeGateway_WhenTraceHasMissingRequiredField()
    {
        var attemptRepo = new Mock<IAchResponseNotificationAttemptRepository>();
        var gateway = new Mock<IRespuestaTransaccionesAchGateway>();
        var uow = new Mock<IUnitOfWork>();
        var responseRepo = new Mock<IAchResponseRepository>();
        var operationResolver = new Mock<ITransactionIntegrationOperationResolver>();
        var readiness = new Mock<IIntegrationMappingReadinessService>();
        var traceWriter = new Mock<IIntegrationMappingTraceWriter>();
        var attempt = BuildAttempt(AchResponseNotificationStatus.Pendiente);

        operationResolver.Setup(x => x.ResolveDifferentialResponse(It.IsAny<string?>(), It.IsAny<int?>()))
            .Returns(DifferentialOperation());
        readiness.Setup(x => x.EvaluateAsync(It.IsAny<TransactionIntegrationOperationResult>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ReadyDifferentialResponse());
        traceWriter.Setup(x => x.WriteAsync(
                It.IsAny<TransactionIntegrationOperationResult>(),
                It.IsAny<RegistrarRespuestaAchCommand>(),
                It.IsAny<int?>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                true,
                false,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntegrationMappingTraceWriteResult(Guid.NewGuid(), 5, ["ANSIDTX"], ["missing"]));

        attemptRepo.Setup(x => x.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(attempt);
        var sut = new NotificarRespuestaAchUseCase(responseRepo.Object, attemptRepo.Object, new RegistrarRespuestaAchCommandMapper(), gateway.Object, uow.Object, operationResolver.Object, readiness.Object, traceWriter.Object);

        var result = await sut.ExecuteAsync(new NotificarRespuestaAchCommand(1, null));

        Assert.True(result.ExisteError);
        Assert.Equal("DIFFERENTIAL_RESPONSE_REQUIRED_FIELD_MISSING", result.CodigoError);
        Assert.Equal(AchResponseNotificationStatus.ErrorFuncional, attempt.EstadoNotificacion);
        gateway.Verify(x => x.RegistrarRespuestaAsync(It.IsAny<RegistrarRespuestaAchCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void ExecuteAsync_ShouldNotReferenceSoapOrProviderTerms()
    {
        var types = new[]
        {
            typeof(NotificarRespuestaAchCommand), typeof(NotificarRespuestaAchResult), typeof(INotificarRespuestaAchUseCase),
            typeof(NotificarRespuestaAchUseCase), typeof(IRegistrarRespuestaAchCommandMapper), typeof(RegistrarRespuestaAchCommandMapper)
        };
        var forbidden = new[] { "Axon", "Soap", "Xml", "Wsdl", "Envelope", "idTransaccionAxon", "RegistrarRespuestaTransaccion" };
        foreach (var t in types)
            Assert.DoesNotContain(forbidden, x => (t.FullName ?? t.Name).Contains(x, StringComparison.OrdinalIgnoreCase));
    }

    private static NotificarRespuestaAchUseCase BuildSut(out Mock<IAchResponseNotificationAttemptRepository> attemptRepo, out Mock<IRespuestaTransaccionesAchGateway> gateway, out Mock<IUnitOfWork> uow, out Mock<IAchResponseRepository> responseRepo)
    {
        attemptRepo = new Mock<IAchResponseNotificationAttemptRepository>();
        gateway = new Mock<IRespuestaTransaccionesAchGateway>();
        uow = new Mock<IUnitOfWork>();
        responseRepo = new Mock<IAchResponseRepository>();
        return new NotificarRespuestaAchUseCase(responseRepo.Object, attemptRepo.Object, new RegistrarRespuestaAchCommandMapper(), gateway.Object, uow.Object);
    }

    private static AchResponseNotificationAttempt BuildAttempt(AchResponseNotificationStatus status)
        => new()
        {
            Id = 1,
            EstadoNotificacion = status,
            IdCanal = 1,
            NombreCanal = "CAN",
            IdTransaccion = "TX",
            IdEstado = 2,
            IdTransaccionServicioExterno = 99,
            AchResponse = new AchResponse { Id = Guid.NewGuid(), TipoRespuesta = TipoRespuestaAch.Transaccion, IdTransaccion = "TX", CodigoCamaraCompensacion = "ACH", EstadoProcesamiento = AchResponseProcessingStatus.Homologada }
        };

    private static TransactionIntegrationOperationResult DifferentialOperation()
        => new(
            null,
            "TX",
            IntegrationGuaranteeConstants.WsAxon,
            IntegrationGuaranteeConstants.RegistrarRespuestaTransaccion,
            IntegrationGuaranteeConstants.DifferentialResponseNotification,
            IntegrationGuaranteeConstants.InboundResponse,
            "Respuesta diferencial / notificacion",
            "Entidad/camara/proveedor externo",
            false,
            "Notificacion/respuesta diferencial no monetaria.",
            true,
            []);

    private static IntegrationMappingReadinessResult ReadyDifferentialResponse()
        => new(
            true,
            "Ok",
            "OK",
            IntegrationGuaranteeConstants.WsAxon,
            IntegrationGuaranteeConstants.RegistrarRespuestaTransaccion,
            IntegrationGuaranteeConstants.DifferentialResponseNotification,
            IntegrationGuaranteeConstants.InboundResponse,
            3,
            3,
            [],
            [],
            [],
            [],
            false,
            true,
            [],
            []);
}
