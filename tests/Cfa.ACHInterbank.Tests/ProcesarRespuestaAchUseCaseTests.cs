using Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Models;
using Cfa.ACHInterbank.Application.ACH.Responses.Processing.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Processing.Models;
using Cfa.ACHInterbank.Application.ACH.Responses.Processing.Services;
using Cfa.ACHInterbank.Application.ACH.Responses.Processing.Validation;
using Cfa.ACHInterbank.Application.ACH.Responses.Repositories;
using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class ProcesarRespuestaAchUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_ShouldReturnValidationFailure_WhenCommandIsInvalid()
    {
        var sut = BuildUseCase(out var responseRepo, out var attemptRepo, out var mapping, out var uow, out _);
        var cmd = BuildValidCommand() with { IdCanal = 0 };

        var result = await sut.ExecuteAsync(cmd);

        Assert.False(result.Procesada);
        responseRepo.Verify(x => x.FindByIdempotencyHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        mapping.Verify(x => x.HomologarAsync(It.IsAny<HomologarRespuestaAchRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnDuplicate_WhenIdempotencyHashAlreadyExists()
    {
        var sut = BuildUseCase(out var responseRepo, out var attemptRepo, out var mapping, out var uow, out var hash);
        hash.Setup(h => h.BuildHash(It.IsAny<ProcesarRespuestaAchCommand>())).Returns("HASH1");
        responseRepo.Setup(r => r.FindByIdempotencyHashAsync("HASH1", It.IsAny<CancellationToken>())).ReturnsAsync(new AchResponse { Id = Guid.NewGuid(), EstadoProcesamiento = AchResponseProcessingStatus.Homologada, PermiteNotificacion = true, IdEstadoInterno = 1 });

        var result = await sut.ExecuteAsync(BuildValidCommand());

        Assert.True(result.Procesada);
        Assert.True(result.Duplicada);
        mapping.Verify(x => x.HomologarAsync(It.IsAny<HomologarRespuestaAchRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        responseRepo.Verify(x => x.AddAsync(It.IsAny<AchResponse>(), It.IsAny<CancellationToken>()), Times.Never);
        attemptRepo.Verify(x => x.AddAsync(It.IsAny<AchResponseNotificationAttempt>(), It.IsAny<CancellationToken>()), Times.Never);
        responseRepo.Verify(x => x.AddAuditAsync(It.Is<AchResponseAudit>(a => a.Action == "DuplicateReceipt"), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPersistNoHomologada_WhenNoMappingExists()
    {
        var sut = BuildUseCase(out var responseRepo, out var attemptRepo, out var mapping, out var uow, out _);
        responseRepo.Setup(r => r.FindByIdempotencyHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((AchResponse?)null);
        mapping.Setup(m => m.HomologarAsync(It.IsAny<HomologarRespuestaAchRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(HomologarRespuestaAchResult.NotFound("x"));

        var result = await sut.ExecuteAsync(BuildValidCommand());

        Assert.True(result.Procesada);
        Assert.Equal(AchResponseProcessingStatus.NoHomologada, result.EstadoProcesamiento);
        responseRepo.Verify(x => x.AddAsync(It.Is<AchResponse>(r => r.EstadoProcesamiento == AchResponseProcessingStatus.NoHomologada), It.IsAny<CancellationToken>()), Times.Once);
        attemptRepo.Verify(x => x.AddAsync(It.IsAny<AchResponseNotificationAttempt>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(x => x.CommitIdempotentAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPersistRequiresManualReview_WhenMappingDoesNotAllowNotification()
    {
        var sut = BuildUseCase(out var responseRepo, out var attemptRepo, out var mapping, out var uow, out _);
        responseRepo.Setup(r => r.FindByIdempotencyHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((AchResponse?)null);
        mapping.Setup(m => m.HomologarAsync(It.IsAny<HomologarRespuestaAchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(HomologarRespuestaAchResult.NotAllowed(1, 2, "estado", "R01", "desc", "no"));

        var result = await sut.ExecuteAsync(BuildValidCommand());

        Assert.Equal(AchResponseProcessingStatus.RequiereRevisionManual, result.EstadoProcesamiento);
        attemptRepo.Verify(x => x.AddAsync(It.IsAny<AchResponseNotificationAttempt>(), It.IsAny<CancellationToken>()), Times.Never);
        uow.Verify(x => x.CommitIdempotentAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldRecoverLogicalIdentity_WhenConcurrentInsertWins()
    {
        var sut = BuildUseCase(out var responseRepo, out _, out var mapping, out var uow, out var hash);
        hash.Setup(x => x.BuildHash(It.IsAny<ProcesarRespuestaAchCommand>())).Returns("CONCURRENT-HASH");
        var winner = new AchResponse
        {
            Id = Guid.NewGuid(), EstadoProcesamiento = AchResponseProcessingStatus.Homologada,
            IdEstadoInterno = 1, PermiteNotificacion = false, Version = Guid.NewGuid()
        };
        responseRepo.SetupSequence(x => x.FindByIdempotencyHashAsync("CONCURRENT-HASH", It.IsAny<CancellationToken>()))
            .ReturnsAsync((AchResponse?)null)
            .ReturnsAsync(winner);
        mapping.Setup(x => x.HomologarAsync(It.IsAny<HomologarRespuestaAchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(HomologarRespuestaAchResult.Success(false, 1, 1, "Aplicada", null, null));
        uow.Setup(x => x.CommitIdempotentAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new IdempotentWriteConflictException(new Exception("unique")));

        var result = await sut.ExecuteAsync(BuildValidCommand());

        Assert.True(result.Duplicada);
        Assert.Equal(winner.Id, result.AchResponseId);
        Assert.Equal(1, winner.DuplicateReceiptCount);
        responseRepo.Verify(x => x.AddAuditAsync(It.Is<AchResponseAudit>(a => a.Action == "DuplicateReceipt"),
            It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldPersistHomologadaAndPendingAttempt_WhenMappingAllowsNotification()
    {
        var sut = BuildUseCase(out var responseRepo, out var attemptRepo, out var mapping, out var uow, out _);
        responseRepo.Setup(r => r.FindByIdempotencyHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((AchResponse?)null);
        mapping.Setup(m => m.HomologarAsync(It.IsAny<HomologarRespuestaAchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(HomologarRespuestaAchResult.Success(true, 10, 20, "Aplicada", "R01", "Desc"));

        var result = await sut.ExecuteAsync(BuildValidCommand());

        Assert.Equal(AchResponseProcessingStatus.Homologada, result.EstadoProcesamiento);
        attemptRepo.Verify(x => x.AddAsync(It.Is<AchResponseNotificationAttempt>(a => a.EstadoNotificacion == AchResponseNotificationStatus.Pendiente && a.IdEstado == 20), It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(x => x.CommitIdempotentAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldCreatePendingSoapAttempt_WhenValidatedPrenotificationAllowsNotification()
    {
        var processor = new Mock<IDifferentialPrenotificationResponseProcessor>();
        processor.Setup(x => x.ProcessAsync(
                It.IsAny<ProcesarRespuestaAchCommand>(),
                It.IsAny<AchResponse>(),
                It.IsAny<HomologarRespuestaAchResult>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DifferentialPrenotificationResponseProcessResult(
                true, true, true, true, false, false, false, 101, Guid.NewGuid(), "Certified", null,
                "Respuesta diferencial validada sin movimiento monetario."));
        var sut = BuildUseCase(
            out var responseRepo,
            out var attemptRepo,
            out var mapping,
            out var uow,
            out _,
            processor.Object);
        responseRepo.Setup(r => r.FindByIdempotencyHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AchResponse?)null);
        mapping.Setup(m => m.HomologarAsync(It.IsAny<HomologarRespuestaAchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(HomologarRespuestaAchResult.Success(true, 10, 20, "Aprobada", null, "Aprobada"));

        var result = await sut.ExecuteAsync(BuildValidCommand() with
        {
            TipoRespuesta = TipoRespuestaAch.Prenota,
            CodigoCausalExterna = null
        });

        Assert.Equal(AchResponseProcessingStatus.Homologada, result.EstadoProcesamiento);
        Assert.True(result.PermiteNotificacion);
        Assert.True(result.IntentoPendienteCreado);
        attemptRepo.Verify(x => x.AddAsync(
            It.Is<AchResponseNotificationAttempt>(attempt =>
                attempt.EstadoNotificacion == AchResponseNotificationStatus.Pendiente
                && attempt.IdEstado == 20),
            It.IsAny<CancellationToken>()), Times.Once);
        uow.Verify(x => x.CommitIdempotentAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUseFechaRecepcionFromCommand_WhenProvided()
    {
        var sut = BuildUseCase(out var responseRepo, out _, out var mapping, out var uow, out _);
        responseRepo.Setup(r => r.FindByIdempotencyHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((AchResponse?)null);
        mapping.Setup(m => m.HomologarAsync(It.IsAny<HomologarRespuestaAchRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(HomologarRespuestaAchResult.NotFound("x"));
        var date = new DateTime(2026,1,1,0,0,0,DateTimeKind.Utc);

        await sut.ExecuteAsync(BuildValidCommand() with { FechaRecepcion = date });

        responseRepo.Verify(x => x.AddAsync(It.Is<AchResponse>(r => r.FechaRecepcion == date), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldGenerateCorrelationIdOrPreserveProvided()
    {
        var sut = BuildUseCase(out var responseRepo, out _, out var mapping, out _, out _);
        responseRepo.Setup(r => r.FindByIdempotencyHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((AchResponse?)null);
        mapping.Setup(m => m.HomologarAsync(It.IsAny<HomologarRespuestaAchRequest>(), It.IsAny<CancellationToken>())).ReturnsAsync(HomologarRespuestaAchResult.NotFound("x"));

        await sut.ExecuteAsync(BuildValidCommand() with { CorrelationId = "corr-123" });

        responseRepo.Verify(x => x.AddAsync(It.Is<AchResponse>(r => r.CorrelationId == "corr-123"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void IdempotencyHashService_ShouldGenerateSameHash_ForEquivalentNormalizedCommands()
    {
        IAchResponseIdempotencyHashService sut = new AchResponseIdempotencyHashService();
        var a = BuildValidCommand() with { CodigoCamaraCompensacion = " ach ", IdTransaccion = " tx-1 " };
        var b = BuildValidCommand() with { CodigoCamaraCompensacion = "ACH", IdTransaccion = "TX-1" };

        Assert.Equal(sut.BuildHash(a), sut.BuildHash(b));
    }

    [Fact]
    public void IdempotencyHashService_ShouldGenerateDifferentHash_WhenFunctionalIdentityChanges()
    {
        IAchResponseIdempotencyHashService sut = new AchResponseIdempotencyHashService();
        var a = BuildValidCommand() with { CodigoEstadoExterno = "00" };
        var b = BuildValidCommand() with { CodigoEstadoExterno = "01" };

        Assert.NotEqual(sut.BuildHash(a), sut.BuildHash(b));
    }

    [Fact]
    public void ProcesarRespuestaAchCommandValidator_ShouldRejectInvalidTipoRespuesta()
    {
        var validator = new ProcesarRespuestaAchCommandValidator();
        var cmd = BuildValidCommand() with { TipoRespuesta = (TipoRespuestaAch)999 };
        var errors = validator.Validate(cmd);
        Assert.Contains(nameof(ProcesarRespuestaAchCommand.TipoRespuesta), errors);
    }

    [Fact]
    public void ProcesarRespuestaAchUseCase_ShouldNotReferenceSoapOrProviderTerms()
    {
        var types = new[]
        {
            typeof(ProcesarRespuestaAchCommand), typeof(ProcesarRespuestaAchResult), typeof(IProcesarRespuestaAchUseCase),
            typeof(ProcesarRespuestaAchUseCase), typeof(IAchResponseIdempotencyHashService), typeof(AchResponseIdempotencyHashService)
        };
        var forbidden = new[] { "Axon", "Soap", "Xml", "Wsdl", "Envelope", "idTransaccionAxon", "RegistrarRespuestaTransaccion" };
        foreach (var t in types)
            Assert.DoesNotContain(forbidden, x => (t.FullName ?? t.Name).Contains(x, StringComparison.OrdinalIgnoreCase));
    }

    private static ProcesarRespuestaAchUseCase BuildUseCase(out Mock<IAchResponseRepository> responseRepo, out Mock<IAchResponseNotificationAttemptRepository> attemptRepo, out Mock<IRespuestaAchStatusMappingService> mappingService, out Mock<IUnitOfWork> unitOfWork, out Mock<IAchResponseIdempotencyHashService> hash, IDifferentialPrenotificationResponseProcessor? prenotificationResponseProcessor = null)
    {
        responseRepo = new Mock<IAchResponseRepository>();
        attemptRepo = new Mock<IAchResponseNotificationAttemptRepository>();
        mappingService = new Mock<IRespuestaAchStatusMappingService>();
        mappingService.Setup(x => x.ResolveClearingHouseIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        unitOfWork = new Mock<IUnitOfWork>();
        hash = new Mock<IAchResponseIdempotencyHashService>();
        hash.Setup(h => h.BuildHash(It.IsAny<ProcesarRespuestaAchCommand>())).Returns("HASH");
        return new ProcesarRespuestaAchUseCase(new ProcesarRespuestaAchCommandValidator(), hash.Object, responseRepo.Object, attemptRepo.Object, mappingService.Object, unitOfWork.Object, prenotificationResponseProcessor);
    }

    private static ProcesarRespuestaAchCommand BuildValidCommand() => new(
        TipoRespuestaAch.Transaccion, "TX-1", "ACH", "001", "002", "00", "R01", "Desc", 1, "Canal", 999, DateTime.UtcNow, "corr");
}
