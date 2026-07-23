using Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Models;
using Cfa.ACHInterbank.Application.ACH.Responses.Reprocessing;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public sealed class AchResponseReprocessPipelineTests
{
    [Fact]
    public async Task ExecuteAsync_ReusesPersistedResponse_WithoutDuplicateReceiptOrEffect()
    {
        await using var db = NewDb();
        var response = Response();
        db.AchResponses.Add(response);
        await db.SaveChangesAsync();
        var mapping = Mapping(MappingResolutionStatus.Matched, exists: true, allowed: true);
        var sut = new AchResponseReprocessPipeline(db, mapping.Object);

        var result = await sut.ExecuteAsync(response.Id, 1);

        Assert.Equal(AchResponseReprocessResultCode.Completed, result.Code);
        Assert.Equal(1, await db.AchResponses.CountAsync());
        Assert.Equal(0, (await db.AchResponses.SingleAsync()).DuplicateReceiptCount);
        Assert.Equal(1, await db.AchResponseNotificationAttempts.CountAsync());

        var second = await sut.ExecuteAsync(response.Id, 1);
        Assert.Equal(AchResponseReprocessResultCode.Completed, second.Code);
        Assert.Equal(1, await db.AchResponseNotificationAttempts.CountAsync());
    }

    [Theory]
    [InlineData(MappingResolutionStatus.NoMatch, AchResponseReprocessResultCode.MappingNotFound)]
    [InlineData(MappingResolutionStatus.Ambiguous, AchResponseReprocessResultCode.MappingAmbiguous)]
    public async Task ExecuteAsync_MapsFunctionalResolutionToTypedResult(MappingResolutionStatus status,
        AchResponseReprocessResultCode expected)
    {
        await using var db = NewDb();
        var response = Response();
        db.AchResponses.Add(response);
        await db.SaveChangesAsync();
        var mapping = Mapping(status, exists: false, allowed: false);
        var sut = new AchResponseReprocessPipeline(db, mapping.Object);

        var result = await sut.ExecuteAsync(response.Id, 1);

        Assert.Equal(expected, result.Code);
        Assert.Equal(1, await db.AchResponses.CountAsync());
        Assert.Equal(0, await db.AchResponseNotificationAttempts.CountAsync());
    }

    [Fact]
    public async Task ExecuteAsync_RecognizesConfirmedEffect_AsIdempotentCompletion()
    {
        await using var db = NewDb();
        var response = Response();
        response.NotificationAttempts.Add(new AchResponseNotificationAttempt
        {
            NumeroIntento = 1, EstadoNotificacion = AchResponseNotificationStatus.Exitosa, NombreCanal = "test",
            IdTransaccion = response.IdTransaccion, FechaCreacion = DateTime.UtcNow
        });
        db.AchResponses.Add(response);
        await db.SaveChangesAsync();
        var sut = new AchResponseReprocessPipeline(db, Mapping(MappingResolutionStatus.Matched, true, true).Object);

        var result = await sut.ExecuteAsync(response.Id, 1);

        Assert.Equal(AchResponseReprocessResultCode.AlreadyApplied, result.Code);
        Assert.Equal(1, await db.AchResponseNotificationAttempts.CountAsync());
    }

    [Fact]
    public void StatePolicy_AllowsManualReviewTerminalPath()
        => AchResponseStatePolicy.EnsureTransition(AchResponseProcessingStatus.Reprocesando,
            AchResponseProcessingStatus.RequiereRevisionManual, "system", "mapping ambiguous", "corr");

    private static Mock<IRespuestaAchStatusMappingService> Mapping(MappingResolutionStatus status, bool exists, bool allowed)
    {
        var mock = new Mock<IRespuestaAchStatusMappingService>();
        var result = status switch
        {
            MappingResolutionStatus.Ambiguous => HomologarRespuestaAchResult.Ambiguous("ambiguous"),
            MappingResolutionStatus.NoMatch => HomologarRespuestaAchResult.NotFound("not found"),
            _ => HomologarRespuestaAchResult.Success(allowed, 1, 2, "Applied", "cause", "description", 3)
        };
        mock.Setup(x => x.HomologarAsync(It.IsAny<HomologarRespuestaAchRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        return mock;
    }

    private static AchDbContext NewDb()
        => new(new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);

    private static AchResponse Response() => new()
    {
        Id = Guid.NewGuid(), ClearingHouseId = 1, TipoRespuesta = TipoRespuestaAch.Transaccion, IdTransaccion = "safe-ref",
        CodigoCamaraCompensacion = "ACHCOL", CodigoEstadoExterno = "R01", HashIdempotencia = Guid.NewGuid().ToString("N"),
        CanonicalPayloadHash = Guid.NewGuid().ToString("N"), OperationalDate = DateTime.UtcNow.Date,
        CorrelationId = "corr", FechaRecepcion = DateTime.UtcNow, FechaCreacion = DateTime.UtcNow,
        EstadoProcesamiento = AchResponseProcessingStatus.Reprocesando, Version = Guid.NewGuid()
    };
}
