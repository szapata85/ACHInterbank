using Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Models;
using Cfa.ACHInterbank.Application.ACH.Responses.Homologation.Services;
using Cfa.ACHInterbank.Application.ACH.Responses.Operations;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Api.Contracts.AchResponses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public sealed class Job4ResponseDomainTests
{
    [Fact]
    public async Task MappingResolver_ShouldChooseOnlyHighestPriority_InSameClearingHouse()
    {
        var repo = new Mock<IAchResponseStatusMappingRepository>();
        repo.Setup(x => x.FindCandidatesAsync("ACHCOL", TipoRespuestaAch.Transaccion, "R01", It.IsAny<CancellationToken>()))
            .ReturnsAsync([
                Mapping(1, 10), Mapping(2, 20)
            ]);
        var sut = new RespuestaAchStatusMappingService(repo.Object);

        var result = await sut.HomologarAsync(new("ACHCOL", TipoRespuestaAch.Transaccion, "R01", null, Utc(2026, 7, 22)));

        Assert.Equal(MappingResolutionStatus.Matched, result.ResolutionStatus);
        Assert.Equal(2, result.MappingId);
        Assert.Equal(2, result.IdEstadoInterno);
    }

    [Fact]
    public void StatePolicy_ShouldAllowGovernedTransition_AndRejectInvalidTransition()
    {
        AchResponseStatePolicy.EnsureTransition(AchResponseProcessingStatus.Huerfana,
            AchResponseProcessingStatus.EnRevision, "operator", "Investigación", "corr-1");

        Assert.Throws<InvalidOperationException>(() => AchResponseStatePolicy.EnsureTransition(
            AchResponseProcessingStatus.Notificada, AchResponseProcessingStatus.Huerfana,
            "operator", "No permitida", "corr-2"));
    }

    [Fact]
    public async Task MappingCrud_ShouldRejectOverlappingActiveMapping_AndStaleVersion()
    {
        await using var db = NewDb();
        db.ClearingHouses.Add(House(1, "ACHCOL"));
        await db.SaveChangesAsync();
        var sut = new AchResponseOperationsService(db);
        var created = await sut.CreateMappingAsync(Command(1), "operator", "corr-create");

        await Assert.ThrowsAsync<AchResponseConflictException>(() =>
            sut.CreateMappingAsync(Command(1), "operator", "corr-overlap"));

        var stale = Command(1) with { ExpectedVersion = Guid.NewGuid(), Reason = "Edición concurrente" };
        var conflict = await Assert.ThrowsAsync<AchResponseConflictException>(() =>
            sut.UpdateMappingAsync(created.Id, stale, "operator", "corr-stale"));
        Assert.Equal(created.Version, conflict.CurrentVersion);
    }

    [Fact]
    public async Task Reprocess_ShouldBeIdempotentByCommand_AndBlockSimultaneousAttempt()
    {
        await using var db = NewDb();
        db.ClearingHouses.Add(House(1, "ACHCOL"));
        var response = Response(AchResponseProcessingStatus.ErrorTecnico);
        db.AchResponses.Add(response);
        await db.SaveChangesAsync();
        var sut = new AchResponseOperationsService(db);
        var commandId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var command = new ReprocessCommand(commandId, response.Version, "Reintento técnico", "corr-reprocess");

        var first = await sut.RequestReprocessAsync(response.Id, command, "operator");
        var duplicate = await sut.RequestReprocessAsync(response.Id, command, "operator");

        Assert.Equal(first.Id, duplicate.Id);
        var second = new ReprocessCommand(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            response.Version, "Segundo intento", "corr-reprocess-2");
        await Assert.ThrowsAsync<AchResponseConflictException>(() =>
            sut.RequestReprocessAsync(response.Id, second, "operator"));
        Assert.Single(await sut.GetAuditAsync(nameof(AchResponse), response.Id.ToString()));
    }

    [Fact]
    public async Task Orphan_ShouldKeepHistory_WhenManualReviewRejectsIt()
    {
        await using var db = NewDb();
        db.ClearingHouses.Add(House(1, "ACHCOL"));
        var response = Response(AchResponseProcessingStatus.NoHomologada);
        db.AchResponses.Add(response);
        await db.SaveChangesAsync();
        var sut = new AchResponseOperationsService(db);

        var orphan = await sut.CreateOrphanAsync(response.Id, "Correlación no inequívoca", "candidate-count=0",
            "operator", "corr-orphan");
        var review = await sut.BeginReviewAsync(orphan.Id, orphan.Version, "Inicio de revisión",
            "operator", "corr-review");
        var resolved = await sut.ResolveOrphanAsync(orphan.Id,
            new(review.Version, "Rechazo justificado", null, true, "corr-reject"), "operator");

        Assert.Equal("Rejected", resolved.ResolutionStatus);
        Assert.NotNull(await db.AchResponseOrphans.SingleOrDefaultAsync(x => x.Id == orphan.Id));
        Assert.Single(await db.AchResponseReconciliationCases.Where(x => x.AchResponseId == response.Id).ToListAsync());
        var audit = await sut.GetAuditAsync(nameof(AchResponseOrphan), orphan.Id.ToString());
        Assert.Equal(2, audit.Count);
    }

    [Fact]
    public async Task ReconciliationResolution_ShouldPersistResolution_AndAudit()
    {
        await using var db = NewDb();
        db.ClearingHouses.Add(House(1, "ACHCOL"));
        var item = new AchResponseReconciliationCase
        {
            Id = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc"), ClearingHouseId = 1,
            ExceptionType = "ResponseWithoutTransaction", Status = "Open", Reference = "synthetic-ref",
            DetectedAtUtc = Utc(2026, 7, 22), CorrelationId = "corr-detect", Version = Guid.NewGuid()
        };
        db.AchResponseReconciliationCases.Add(item);
        await db.SaveChangesAsync();
        var sut = new AchResponseOperationsService(db);

        var result = await sut.ResolveReconciliationCaseAsync(item.Id,
            new(item.Version, "AcceptedOperationally", "Validación manual", "corr-resolve"), "operator");

        Assert.Equal("Resolved", result.Status);
        Assert.Equal("AcceptedOperationally", result.Resolution);
        var audit = await sut.GetAuditAsync(nameof(AchResponseReconciliationCase), item.Id.ToString());
        Assert.Contains(audit, x => x.Action == "ReconciliationResolved");
    }

    [Fact]
    public async Task OperationsController_ShouldReturnProblemDetails409_WithCurrentVersion()
    {
        var current = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var service = new Mock<IAchResponseOperationsService>();
        service.Setup(x => x.SetMappingActiveAsync(1, true, It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AchResponseConflictException("stale", current));
        var controller = new AchResponseOperationsController(service.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext
                {
                    User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "operator")], "test"))
                }
            }
        };

        var result = await controller.ActivateMapping(1,
            new VersionedReasonRequest(Guid.NewGuid(), "Activación controlada"), default);

        var conflict = Assert.IsType<ConflictObjectResult>(result);
        var problem = Assert.IsType<ProblemDetails>(conflict.Value);
        Assert.Equal(409, problem.Status);
        Assert.Equal(current, problem.Extensions["currentVersion"]);
    }

    private static AchDbContext NewDb()
        => new(new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase($"job4-{Guid.NewGuid():N}").Options) { AuditEnabled = false };

    private static ClearingHouse House(int id, string code) => new()
        { Id = id, Name = code, Code = code, OriginCode = "0001", IsActive = true };

    private static AchResponse Response(AchResponseProcessingStatus status) => new()
    {
        Id = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd"), ClearingHouseId = 1,
        TipoRespuesta = TipoRespuestaAch.Transaccion, IdTransaccion = "synthetic-tx", CodigoCamaraCompensacion = "ACHCOL",
        CodigoEstadoExterno = "R01", HashIdempotencia = new string('A', 64), CanonicalPayloadHash = new string('A', 64),
        OperationalDate = Utc(2026, 7, 22), EstadoProcesamiento = status, FechaRecepcion = Utc(2026, 7, 22),
        FechaCreacion = Utc(2026, 7, 22), Version = Guid.NewGuid()
    };

    private static AchResponseMappingCommand Command(int houseId) => new(houseId, "Transaccion", "R01", null,
        10, 20, "Rejected", null, null, false, false, 100,
        Utc(2026, 1, 1), Utc(2026, 12, 31), true, null, "Configuración controlada");

    private static AchResponseStatusMappingModel Mapping(int id, int priority) => new()
    {
        Id = id, ClearingHouseId = 1, CodigoCamaraCompensacion = "ACHCOL", TipoRespuesta = TipoRespuestaAch.Transaccion,
        CodigoEstadoExterno = "R01", IdEstadoInterno = id, IdEstadoServicioExterno = id,
        EstadoInternoNombre = $"Status{id}", Activo = true, PermiteNotificacion = true, Priority = priority,
        FechaInicioVigencia = Utc(2026, 1, 1), FechaFinVigencia = Utc(2026, 12, 31)
    };

    private static DateTime Utc(int year, int month, int day) => new(year, month, day, 0, 0, 0, DateTimeKind.Utc);
}
