using System.Security.Claims;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Entities.Audit;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

/// <summary>I04-CENIT: pruebas incluidas, no ejecutadas sin SDK .NET 10.</summary>
public sealed class CenitReturnReviewI04Tests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 3, 16, 30, 0, TimeSpan.Zero);
    private static readonly CenitReturnReviewDecision Approval = new(
        "APROBAR", "PREPARADA_SIN_ENVIO", "Cuenta cerrada confirmada por el núcleo.", "TICKET-CENIT-0001");
    private static readonly CenitReturnReviewDecision Discard = new(
        "DESCARTAR", "PREPARADA_SIN_ENVIO", "Se verificó la corrección con el área operativa.", null);

    [Fact]
    public async Task DifferentAuthenticatedPersonApprovesOrdinaryValidPreparationWithoutSending()
    {
        await using var db = NewContext("preparador");
        var row = await Prepare(db);
        var service = Review(db);
        var result = await service.DecideAsync(row.Id, Approval, "revisor");
        Assert.Equal(CenitReturnReviewService.Approved, result.Status);
        Assert.Equal("revisor", result.ReviewedByUserId);
        Assert.Equal("TICKET-CENIT-0001", result.DecisionEvidenceReference);
        Assert.False(result.ProcessingConnected);
        Assert.Contains(db.AuditLogs, x => x.EntityName == nameof(CenitReturnPreparation)
            && x.AfterJson != null && x.AfterJson.Contains("APROBADA_SIN_ENVIO"));
        Assert.Empty(await db.AchReturnsGenerated.ToListAsync());
        Assert.Empty(await db.AchColombiaReturnPreparations.ToListAsync());
        Assert.Equal("DESACTIVADO", (await db.ClearingHouseConfigs.SingleAsync(x => x.ClearingHouseId == 1)).AutomaticReturnMode);
    }

    [Fact]
    public async Task SamePreparerCannotApproveOrDiscard()
    {
        await using var db = NewContext("preparador");
        var row = await Prepare(db);
        await Assert.ThrowsAsync<CenitReturnReviewConflictException>(() =>
            Review(db).DecideAsync(row.Id, Approval, "preparador"));
        await Assert.ThrowsAsync<CenitReturnReviewConflictException>(() =>
            Review(db).DecideAsync(row.Id, Discard, "PREPARADOR"));
        Assert.Equal(CenitReturnPreparationService.Prepared, (await db.CenitReturnPreparations.SingleAsync()).Status);
    }

    [Fact]
    public async Task NoReliablePreparerIdentityCannotBeApproved()
    {
        await using var db = NewContext(null);
        var row = await Prepare(db);
        await Assert.ThrowsAsync<CenitReturnReviewConflictException>(() =>
            Review(db).DecideAsync(row.Id, Approval, "revisor"));
        Assert.Single(await db.CenitReturnPreparations.ToListAsync());
        Assert.Empty(await db.AchReturnsGenerated.ToListAsync());
    }

    [Fact]
    public async Task DiscardRemainsAvailableWhenModeDisabledAndIsTerminal()
    {
        await using var db = NewContext("preparador");
        var row = await Prepare(db);
        var config = await db.ClearingHouseConfigs.SingleAsync(x => x.ClearingHouseId == 2);
        config.AutomaticReturnMode = "DESACTIVADO";
        await db.SaveChangesAsync();
        var result = await Review(db).DecideAsync(row.Id, Discard, "revisor");
        Assert.Equal(CenitReturnReviewService.Discarded, result.Status);
        await Assert.ThrowsAsync<CenitReturnReviewConflictException>(() =>
            Review(db).DecideAsync(row.Id, Approval, "otro-revisor"));
        Assert.Equal("DESACTIVADO", (await db.ClearingHouseConfigs.SingleAsync(x => x.ClearingHouseId == 2)).AutomaticReturnMode);
    }

    [Fact]
    public async Task ApprovalFailsIfModeDisabledOrTargetWindowChanged()
    {
        await using var db = NewContext("preparador");
        var row = await Prepare(db);
        var cfg = await db.ClearingHouseConfigs.SingleAsync(x => x.ClearingHouseId == 2);
        cfg.AutomaticReturnMode = "DESACTIVADO";
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<CenitReturnReviewConflictException>(() =>
            Review(db).DecideAsync(row.Id, Approval, "revisor"));
        cfg.AutomaticReturnMode = "CON_APROBACION";
        var target = await db.AchCycles.SingleAsync(x => x.Id == "CENIT-I03-2");
        target.CutoffTime = new TimeSpan(11, 0, 0);
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<CenitReturnReviewConflictException>(() =>
            Review(db).DecideAsync(row.Id, Approval, "revisor"));
        Assert.Equal("PREPARADA_SIN_ENVIO", (await db.CenitReturnPreparations.SingleAsync()).Status);
    }

    [Fact]
    public async Task ApprovalFailsOnNewerSuccessfulEvidenceAndSpecialCausals()
    {
        await using var db = NewContext("preparador");
        var row = await Prepare(db);
        var execution = await db.IncomingNachaIntegrationExecution.SingleAsync();
        var previous = await db.CenitReturnPreparations.SingleAsync();
        previous.ReasonCode = "R23";
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<CenitReturnReviewConflictException>(() =>
            Review(db).DecideAsync(row.Id, Approval, "revisor"));
        previous.ReasonCode = "R02";
        db.IncomingNachaIntegrationExecution.Add(new IncomingNachaIntegrationExecution
        {
            Id = Guid.NewGuid(), DispatchQueueId = execution.DispatchQueueId,
            DispatchQueue = execution.DispatchQueue,
            EntryDetailId = execution.EntryDetailId, ClearingHouseId = 2,
            StartedAtUtc = execution.StartedAtUtc.AddMinutes(1),
            ProcessedAtUtc = execution.ProcessedAtUtc!.Value.AddMinutes(1),
            ExecutionMode = "LIVE", SoapMethodName = "Proc_Transacciones",
            TransportStatus = IntegrationTransportStatus.Succeeded,
            BusinessStatus = IntegrationResponseBusinessStatus.Success,
            BusinessOutcome = IncomingNachaBusinessOutcome.Successful,
            ProcessingStatus = IncomingNachaIndividualProcessingStatus.Completed,
            IsSuccessful = true, ResponseCatalogId = 99, ResultCode = "00"
        });
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<CenitReturnReviewConflictException>(() =>
            Review(db).DecideAsync(row.Id, Approval, "revisor"));
    }

    [Theory]
    [InlineData("APROBAR", "BREVE", "TICKET-1234")]
    [InlineData("APROBAR", "Justificación válida de revisión", null)]
    [InlineData("REABRIR", "Justificación válida de revisión", "TICKET-1234")]
    public async Task RejectInvalidDecisionsWithoutAnyWrite(string action, string justification, string? reference)
    {
        await using var db = NewContext("preparador");
        var row = await Prepare(db);
        var count = await db.AuditLogs.CountAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => Review(db).DecideAsync(row.Id,
            new(action, "PREPARADA_SIN_ENVIO", justification, reference), "revisor"));
        Assert.Equal(count, await db.AuditLogs.CountAsync());
    }

    [Fact]
    public async Task UnauthenticatedOrSpoofedIdentityNeverDecides()
    {
        await using var db = NewContext("preparador");
        var row = await Prepare(db);
        await Assert.ThrowsAsync<CenitReturnReviewConflictException>(() =>
            Review(db).DecideAsync(row.Id, Approval, "system"));
        var fake = new FakeDetector();
        var controller = new CenitReturnReviewController(Review(db), fake)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        Assert.IsType<ForbidResult>((await controller.Decide(row.Id, Approval, default)).Result);
        Assert.Equal("PREPARADA_SIN_ENVIO", (await db.CenitReturnPreparations.SingleAsync()).Status);
        var type = typeof(CenitReturnReviewController);
        Assert.NotNull(type.GetCustomAttributes(typeof(AuthorizeAttribute), false).Single());
        Assert.Equal(P1Policies.ConfigManage, type.GetMethod(nameof(CenitReturnReviewController.Decide))!
            .GetCustomAttributes(typeof(AuthorizeAttribute), false).Cast<AuthorizeAttribute>().Single().Policy);
        Assert.Equal(P1Policies.ConfigRead, type.GetMethod(nameof(CenitReturnReviewController.Get))!
            .GetCustomAttributes(typeof(AuthorizeAttribute), false).Cast<AuthorizeAttribute>().Single().Policy);
        Assert.Equal(P1Policies.ConfigRead, type.GetMethod(nameof(CenitReturnReviewController.GetExceptions))!
            .GetCustomAttributes(typeof(AuthorizeAttribute), false).Cast<AuthorizeAttribute>().Single().Policy);
    }

    [Fact]
    public async Task ReadOnlyExceptionRouteDoesNotCreateReservations()
    {
        await using var db = NewContext("preparador");
        var fake = new FakeDetector();
        var controller = new CenitReturnReviewController(Review(db), fake);
        Assert.IsType<BadRequestObjectResult>((await controller.Get(0, default)).Result);
        Assert.IsType<BadRequestObjectResult>((await controller.GetExceptions(101, default)).Result);
        var result = Assert.IsType<OkObjectResult>((await controller.GetExceptions(100, default)).Result);
        var found = Assert.IsType<CenitPassiveReturnDetection>(result.Value);
        Assert.Single(found.Findings);
        Assert.Equal("REVISION_SOLICITUD", found.Findings[0].Classification);
        Assert.False(found.ProcessingConnected);
        Assert.Empty(db.CenitReturnPreparations);
        Assert.Empty(db.AchReturnsGenerated);
    }

    [Fact]
    public async Task ModelConcurrencyAndBothDatabaseMigrationsAreDeclared()
    {
        await using var db = NewContext("preparador");
        var entity = db.Model.FindEntityType(typeof(CenitReturnPreparation));
        Assert.True(entity!.FindProperty(nameof(CenitReturnPreparation.Status))!.IsConcurrencyToken);
        Assert.Equal(500, entity.FindProperty(nameof(CenitReturnPreparation.DecisionJustification))!.GetMaxLength());
        Assert.Equal(128, entity.FindProperty(nameof(CenitReturnPreparation.ReviewedByUserId))!.GetMaxLength());
    }

    private static CenitReturnReviewService Review(AchDbContext db) => new(db, new FixedClock(Now));
    private static readonly string ConnectionPrefix = "cenit-i04-";
    private static AchDbContext NewContext(string? userId)
    {
        var http = new DefaultHttpContext();
        if (userId is not null)
            http.User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, userId)], "test"));
        var accessor = new HttpContextAccessor { HttpContext = http };
        return new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(ConnectionPrefix + Guid.NewGuid().ToString("N")).Options, accessor);
    }
    private sealed class FixedClock(DateTimeOffset instant) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => instant;
    }

    private static async Task<CenitReturnPreparation> Prepare(AchDbContext db)
    {
        var execution = await CenitReturnPreparationI03Tests.SeedAsync(db);
        (await db.ClearingHouseConfigs.SingleAsync(x => x.ClearingHouseId == 2)).AutomaticReturnMode = "CON_APROBACION";
        await db.SaveChangesAsync();
        await new CenitReturnPreparationService(db, new FixedClock(Now))
            .PrepareAsync(new(42, execution.Id));
        return await db.CenitReturnPreparations.SingleAsync();
    }

    private sealed class FakeDetector : ICenitPassiveReturnDetector
    {
        public Task<CenitPassiveReturnDetection> DetectAsync(int maxTransactions = 100,
            CancellationToken cancellationToken = default) => Task.FromResult(new CenitPassiveReturnDetection(
                "DESACTIVADO", false, 1, false,
                [new(42, Guid.NewGuid(), Guid.NewGuid(), "REVISION_SOLICITUD", "R23", "Revisión humana", DateTime.UtcNow),
                 new(43, Guid.NewGuid(), Guid.NewGuid(), "INDICIO_CAUSAL_CENIT", "R02", "Indicio", DateTime.UtcNow)]));
    }
}
