using System.Security.Claims;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Cfa.ACHInterbank.Tests;

/// <summary>Pruebas xUnit I05: incorporar para ejecución posterior con SDK .NET 10.</summary>
public sealed class CenitReturnArtifactI05Tests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 3, 16, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task GenerateRequiresAnActuallyApprovedReservationAndDoesNotReserveTrace()
    {
        await using var db = NewContext("preparador");
        var prep = await PrepareAsync(db, approved: false);
        var sequences = new Mock<IAchReturnTraceSequenceService>(MockBehavior.Strict);
        var builder = new Mock<INachaFileBuilder>(MockBehavior.Strict);
        await Assert.ThrowsAsync<CenitReturnArtifactConflictException>(() =>
            Service(db, builder.Object, sequences.Object).GenerateAsync(prep.Id, "generador"));
        Assert.Null((await db.CenitReturnPreparations.SingleAsync()).ProtectedArtifactContent);
        Assert.Empty(db.AchReturnsGenerated);
        sequences.VerifyNoOtherCalls();
        builder.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ApprovedButMissingRawOriginalFailsClosedWithoutFinancialEffects()
    {
        await using var db = NewContext("preparador");
        var prep = await PrepareAsync(db, approved: true);
        var sequences = new Mock<IAchReturnTraceSequenceService>(MockBehavior.Strict);
        var builder = new Mock<INachaFileBuilder>(MockBehavior.Strict);
        await Assert.ThrowsAsync<CenitReturnArtifactConflictException>(() =>
            Service(db, builder.Object, sequences.Object).GenerateAsync(prep.Id, "generador"));
        Assert.Equal(CenitReturnReviewService.Approved, prep.Status);
        Assert.Empty(db.AchReturnsGenerated);
        Assert.Empty(db.AchColombiaReturnPreparations);
        sequences.VerifyNoOtherCalls();
        builder.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GenerationIsCenitOnlySingleUseProtectedAndNotExported()
    {
        await using var db = NewContext("preparador");
        var prep = await PrepareAsync(db, approved: true);
        await AttachVerifiedOriginalAsync(db);
        var sequence = new Mock<IAchReturnTraceSequenceService>(MockBehavior.Strict);
        sequence.Setup(x => x.ReserveRangeAsync("87654321", new DateOnly(2026, 11, 3),
            1, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchReturnTraceRange(1, 1));
        var builder = new Mock<INachaFileBuilder>(MockBehavior.Strict);
        NachaReturnOutBuildRequest? captured = null;
        builder.Setup(x => x.BuildReturnOutAsync(It.IsAny<NachaReturnOutBuildRequest>(),
            It.IsAny<CancellationToken>())).ReturnsAsync((NachaReturnOutBuildRequest r, CancellationToken _) =>
            {
                captured = r;
                return PhysicalSample(r);
            });
        var sut = Service(db, builder.Object, sequence.Object);
        var result = await sut.GenerateAsync(prep.Id, "generador");
        Assert.False(result.Transmittable);
        Assert.StartsWith("NO_TRANSMITIR_CENIT_I05_", result.InternalFileName);
        Assert.Equal(10, result.RecordCount);
        Assert.Equal(CenitReturnOut2026Layout.ProfileCode, result.OfficialProfile);
        Assert.NotNull(captured);
        Assert.Equal("CENIT", captured!.ClearingHouseCode);
        Assert.False(captured.PersistAudit);
        Assert.Equal("PPD", captured.Batches.Single().StandardEntryClassCode);
        Assert.Equal("R02", captured.Batches.Single().Entries.Single().ReturnReasonCode);
        Assert.Equal("123456780000099", captured.Batches.Single().Entries.Single().OriginalTraceNumber);
        Assert.Equal("876543210000001", captured.Batches.Single().Entries.Single().NewTraceNumber);
        Assert.Equal("0987654321", captured.ImmediateOrigin);
        Assert.Equal("0123456789", captured.ImmediateDestination);
        var saved = await db.CenitReturnPreparations.SingleAsync();
        Assert.Equal(CenitReturnArtifactService.Generated, saved.Status);
        Assert.NotNull(saved.ProtectedArtifactContent);
        Assert.DoesNotContain("123456780000099", saved.ProtectedArtifactContent!);
        Assert.NotEqual(Convert.ToBase64String(result.Content), saved.ProtectedArtifactContent);
        Assert.Equal(result.Sha256, saved.ArtifactSha256);
        Assert.Equal(result.Content, (await sut.ReadLocalAsync(prep.Id)).Content);
        Assert.Equal(result.Sha256, (await sut.GetMetadataAsync(prep.Id)).Sha256);
        await Assert.ThrowsAsync<CenitReturnArtifactConflictException>(() =>
            sut.GenerateAsync(prep.Id, "generador"));
        Assert.Empty(db.AchReturnsGenerated);
        Assert.Empty(db.AchColombiaReturnPreparations);
        Assert.Equal("DESACTIVADO", (await db.ClearingHouseConfigs.SingleAsync(x => x.ClearingHouseId == 1)).AutomaticReturnMode);
        sequence.Verify(x => x.ReserveRangeAsync("87654321", new DateOnly(2026, 11, 3),
            1, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once());
        builder.Verify(x => x.BuildReturnOutAsync(It.IsAny<NachaReturnOutBuildRequest>(),
            It.IsAny<CancellationToken>()), Times.Once());
    }

    [Fact]
    public async Task GenerationFailsWhenModeDisabledOrOriginalSourceBelongsToAnotherHouse()
    {
        await using var db = NewContext("preparador");
        var prep = await PrepareAsync(db, approved: true);
        await AttachVerifiedOriginalAsync(db);
        var mock = new Mock<IAchReturnTraceSequenceService>(MockBehavior.Strict);
        var builder = new Mock<INachaFileBuilder>(MockBehavior.Strict);
        var sut = Service(db, builder.Object, mock.Object);
        (await db.ClearingHouseConfigs.SingleAsync(x => x.ClearingHouseId == 2)).AutomaticReturnMode = "DESACTIVADO";
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<CenitReturnArtifactConflictException>(() => sut.GenerateAsync(prep.Id, "generador"));
        (await db.ClearingHouseConfigs.SingleAsync(x => x.ClearingHouseId == 2)).AutomaticReturnMode = "CON_APROBACION";
        var originalHeader = await db.NachaHeaders.SingleAsync();
        originalHeader.ClearingHouseId = 1;
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<CenitReturnArtifactConflictException>(() => sut.GenerateAsync(prep.Id, "generador"));
        Assert.Null(prep.ArtifactSha256);
        mock.VerifyNoOtherCalls();
        builder.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task NoPreparersCanGenerateAndNoAchtieOrTransmissionEndpointsExist()
    {
        await using var db = NewContext("preparador");
        var prep = await PrepareAsync(db, approved: true);
        var sut = Service(db, Mock.Of<INachaFileBuilder>(), Mock.Of<IAchReturnTraceSequenceService>());
        await Assert.ThrowsAsync<CenitReturnArtifactConflictException>(() => sut.GenerateAsync(prep.Id, "preparador"));
        await Assert.ThrowsAsync<ArgumentException>(() => sut.GenerateAsync(prep.Id, "system"));
        var cls = typeof(CenitReturnArtifactController);
        Assert.NotNull(Attribute.GetCustomAttribute(cls, typeof(AuthorizeAttribute)));
        foreach (var m in new[] { nameof(CenitReturnArtifactController.GenerateLocal),
                     nameof(CenitReturnArtifactController.DownloadLocal) })
            Assert.Equal(P1Policies.ConfigManage, cls.GetMethod(m)!
                .GetCustomAttributes(typeof(AuthorizeAttribute), false)
                .Cast<AuthorizeAttribute>().Single().Policy);
        Assert.Equal(P1Policies.ConfigRead, cls.GetMethod(nameof(CenitReturnArtifactController.Get))!
            .GetCustomAttributes(typeof(AuthorizeAttribute), false)
            .Cast<AuthorizeAttribute>().Single().Policy);
        Assert.DoesNotContain(cls.GetMethods(), m => m.Name.Contains("Transport", StringComparison.OrdinalIgnoreCase)
            || m.Name.Contains("Enviar", StringComparison.OrdinalIgnoreCase));
        var controller = new CenitReturnArtifactController(sut)
            { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };
        Assert.IsType<ForbidResult>(await controller.GenerateLocal(prep.Id, default));
        Assert.Empty(db.AchReturnsGenerated);
    }

    [Fact]
    public void ProfileAndPhysicalStructureHardFailIfAnyFieldIsCorrupted()
    {
        var r = PhysicalSample(new NachaReturnOutBuildRequest(DateTime.UtcNow, "A", "0123456789",
            "0987654321", "CENIT", "CFA", "RETURN", []));
        CenitReturnArtifactService.VerifyArtifact(r, "21", "123456780000099", "R02", "876543210000001");
        Assert.Throws<CenitReturnArtifactConflictException>(() =>
            CenitReturnArtifactService.VerifyArtifact(r with { ProfileCode = "ACH_V35" },
                "21", "123456780000099", "R02", "876543210000001"));
        Assert.Throws<CenitReturnArtifactConflictException>(() =>
            CenitReturnArtifactService.VerifyArtifact(r with { Content = r.Content.Replace("R02", "R23") },
                "21", "123456780000099", "R02", "876543210000001"));
        Assert.Throws<CenitReturnArtifactConflictException>(() =>
            CenitReturnArtifactService.VerifyArtifact(r with { Content = r.Content[..^1] },
                "21", "123456780000099", "R02", "876543210000001"));
    }

    private static NachaReturnOutBuildResult PhysicalSample(NachaReturnOutBuildRequest request)
    {
        static char[] Record(char code) => code.ToString().PadRight(106, ' ').ToCharArray();
        static void Put(char[] record, int zeroBased, string value) => value.CopyTo(0, record, zeroBased, value.Length);
        var header = Record('1');
        Put(header, 3, request.ImmediateDestination);
        Put(header, 13, request.ImmediateOrigin);
        var batchHeader = Record('5');
        var effective = request.Batches.Count == 0 ? new DateTime(2026, 11, 3)
            : request.Batches.Single().EffectiveEntryDate;
        Put(batchHeader, 50, request.Batches.Count == 0 ? "PPD"
            : request.Batches.Single().StandardEntryClassCode);
        Put(batchHeader, 71, effective.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture));
        var entry = Record('6');
        Put(entry, 1, "21");
        var line = request.Batches.Count == 0 ? null : request.Batches.Single().Entries.Single();
        Put(entry, 3, line?.ReceivingDfi ?? "12345678");
        Put(entry, 12, (line?.AccountNumber ?? "00123456789012345").PadRight(17, ' '));
        var cents = (long)((line?.Amount ?? 150m) * 100m);
        Put(entry, 29, cents.ToString("D18", System.Globalization.CultureInfo.InvariantCulture));
        Put(entry, 86, "1");
        Put(entry, 87, "876543210000001");
        var ad = Record('7');
        Put(ad, 1, "99");
        Put(ad, 3, "R02");
        Put(ad, 6, "123456780000099");
        Put(ad, 29, line?.OriginalReceivingDfi ?? "87654321");
        Put(ad, 81, "876543210000001");
        var batchControl = Record('8');
        Put(batchControl, 4, "000002");
        var fileControl = Record('9');
        Put(fileControl, 13, "00000002");
        var content = new string(header) + new string(batchHeader) + new string(entry)
            + new string(ad) + new string(batchControl) + new string(fileControl)
            + string.Concat(Enumerable.Repeat(new string('9', 106), 4));
        return new NachaReturnOutBuildResult(content, 10, CenitReturnOut2026Layout.ProfileCode,
            CenitReturnOut2026Layout.NormativeVersion, false);
    }

    private static readonly IDataProtectionProvider Protection = new EphemeralDataProtectionProvider();
    private static CenitReturnArtifactService Service(AchDbContext db, INachaFileBuilder b,
        IAchReturnTraceSequenceService s) => new(db, new FixedClock(), b, s, Protection);
    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private static AchDbContext NewContext(string actor)
    {
        var http = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, actor)], "test"))
        };
        return new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase("cenit-i05-" + Guid.NewGuid().ToString("N")).Options,
            new HttpContextAccessor { HttpContext = http });
    }
    private static async Task<CenitReturnPreparation> PrepareAsync(AchDbContext db, bool approved)
    {
        var evidence = await CenitReturnPreparationI03Tests.SeedAsync(db);
        (await db.ClearingHouseConfigs.SingleAsync(x => x.ClearingHouseId == 2)).AutomaticReturnMode = "CON_APROBACION";
        await db.SaveChangesAsync();
        await new CenitReturnPreparationService(db, new FixedClock()).PrepareAsync(new(42, evidence.Id));
        var p = await db.CenitReturnPreparations.SingleAsync();
        if (approved)
            await new CenitReturnReviewService(db, new FixedClock()).DecideAsync(p.Id,
                new("APROBAR", "PREPARADA_SIN_ENVIO", "Cuenta cerrada confirmada por el núcleo.",
                    "TICKET-CENIT-0001"), "revisor");
        return p;
    }
    private static async Task AttachVerifiedOriginalAsync(AchDbContext db)
    {
        var tx = await db.AchTransactions.SingleAsync(x => x.Id == 42);
        tx.TraceNumber = "123456780000099";
        tx.TransactionCode = "22";
        tx.OriginatingDFI = "12345678";
        tx.ReceivingDFI = "87654321";
        tx.DestinationAccountNumber = "00123456789012345";
        var link = await db.IncomingNachaTransactionLinks.Include(x => x.EntryDetail)
            .Include(x => x.Ingestion).SingleAsync();
        link.LinkType = IncomingNachaLinkType.ExactTrace15;
        link.Ingestion.ResolvedClearingHouseId = 2;
        var header = new NachaHeader { NachaID = "I05-CENIT-IN", ClearingHouseId = 2,
            ImmediateDestination = "0987654321", ImmediateOrigin = "0123456789",
            ImmediateDestinationName = "CENIT", ImmediateOriginName = "CFA" };
        var batch = new BatchHeader { BatchID = 505, NachaID = header.NachaID,
            NachaHeader = header, CompanyName = "ORIGINADOR", CompanyId = "AB12345678",
            StandardEntryClassCode = "PPD" };
        var entry = link.EntryDetail!;
        entry.NachaHeader = header;
        entry.BatchHeader = batch;
        entry.TransactionCode = "22";
        entry.ReceivingParticipantEntityCode = "87654321";
        entry.SequenceNumber = "123456780000099";
        entry.AccountNumber = "00123456789012345";
        entry.Amount = 150m;
        entry.RecipUserName = "BENEFICIARIO";
        await db.SaveChangesAsync();
    }
}
