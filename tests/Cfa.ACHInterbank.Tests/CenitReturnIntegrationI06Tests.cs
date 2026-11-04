using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Cfa.ACHInterbank.Api.Controllers;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

/// <summary>
/// I06-CENIT: pruebas agregadas, pendientes de ejecutar con SDK .NET y BD real.
/// NO prueban transmisión ni certificación de cámara.
/// </summary>
public sealed class CenitReturnIntegrationI06Tests
{
    private static readonly IDataProtectionProvider Protection = new EphemeralDataProtectionProvider();
    private static readonly DateTimeOffset Now = new(2026, 11, 4, 15, 30, 0, TimeSpan.Zero);
    private sealed class FixedClock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }
    private static AchDbContext NewDb() => new(new DbContextOptionsBuilder<AchDbContext>()
        .UseInMemoryDatabase("cenit-i06-" + Guid.NewGuid().ToString("N")).Options);
    private static CenitReturnIntegrationReadService Service(AchDbContext db,
        IDataProtectionProvider? keys = null) => new(db, keys ?? Protection, new FixedClock());

    private static async Task<CenitReturnPreparation> CreateAsync(AchDbContext db)
    {
        var execution = await CenitReturnPreparationI03Tests.SeedAsync(db);
        var tx = await db.AchTransactions.SingleAsync(x => x.Id == 42);
        tx.TraceNumber = "123456780000099";
        var p = new CenitReturnPreparation
        {
            Id = Guid.NewGuid(), OriginalTransactionId = tx.Id, OriginalTransaction = tx,
            SourceExecutionId = execution.Id, SourceExecution = execution,
            SourceQueueId = execution.DispatchQueueId, OriginalCycleId = tx.AchCycleId,
            SuggestedReturnCycleId = "CENIT-I03-2", ReasonCode = "R02",
            OriginalValueDate = new DateTime(2026, 11, 3),
            SuggestedValueDate = new DateTime(2026, 11, 3),
            PreparedAtUtc = new DateTime(2026, 11, 3, 16, 30, 0, DateTimeKind.Utc)
        };
        db.CenitReturnPreparations.Add(p);
        await db.SaveChangesAsync();
        return p;
    }

    private static async Task GenerateFixtureAsync(AchDbContext db, CenitReturnPreparation p,
        string? payload = null)
    {
        var text = payload ?? ValidLocalPhysical(p);
        p.Status = CenitReturnArtifactService.Generated;
        p.ArtifactFileName = $"NO_TRANSMITIR_CENIT_I05_{p.Id:N}.txt";
        p.ArtifactTraceNumber = "876543210000001";
        p.ArtifactProfileCode = CenitReturnOut2026Layout.ProfileCode;
        p.ArtifactRecordCount = 10;
        p.GeneratedAtUtc = new DateTime(2026, 11, 3, 16, 45, 0, DateTimeKind.Utc);
        p.GeneratedByUserId = "generador";
        p.ArtifactSha256 = Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(text)));
        p.ProtectedArtifactContent = Protection.CreateProtector(
            "Cfa.ACHInterbank.CENIT.Returns.I05.LocalArtifact.v1").Protect(text);
        await db.SaveChangesAsync();
    }

    private static string ValidLocalPhysical(CenitReturnPreparation p)
    {
        static char[] Record(char n) => n.ToString().PadRight(106, ' ').ToCharArray();
        static void Put(char[] item, int idx, string text) => text.CopyTo(0, item, idx, text.Length);
        var parts = new[] { Record('1'), Record('5'), Record('6'), Record('7'), Record('8'), Record('9') };
        Put(parts[2], 87, "876543210000001");
        Put(parts[2], 29, "000000000000015000");
        Put(parts[3], 1, "99");
        Put(parts[3], 3, p.ReasonCode);
        Put(parts[3], 6, "123456780000099");
        Put(parts[3], 81, "876543210000001");
        return string.Concat(parts.Select(x => new string(x)))
            + string.Concat(Enumerable.Repeat(new string('9', 106), 4));
    }

    [Fact]
    public async Task InitialPreparationRemainsUnsentUnacknowledgedAndReadOnly()
    {
        await using var db = NewDb();
        var p = await CreateAsync(db);
        var previousAudits = await db.AuditLogs.CountAsync();
        var result = await Service(db).GetAsync(p.Id);
        Assert.NotNull(result);
        Assert.Equal("NO_GENERADO", result!.ArtifactIntegrity);
        Assert.Equal("INCONSISTENTE_O_INCOMPLETA", result.ReconciliationStatus);
        Assert.Contains("SIN_ARTEFACTO_LOCAL", result.Alerts);
        Assert.False(result.TransmissionEnabled);
        Assert.False(result.ChamberAcknowledgementAttributed);
        Assert.False(result.FinanciallyReconciled);
        Assert.Equal(previousAudits, await db.AuditLogs.CountAsync());
        Assert.Empty(db.AchReturnsGenerated);
        Assert.Empty(db.AchFileExports);
        Assert.Empty(db.AchColombiaReturnPreparations);
    }

    [Fact]
    public async Task EncryptedLocalFileCanBeVerifiedAfterWindowWithoutBeingMarkedTransmitted()
    {
        await using var db = NewDb();
        var p = await CreateAsync(db);
        await GenerateFixtureAsync(db, p);
        var mode = await db.ClearingHouseConfigs.SingleAsync(x => x.ClearingHouseId == 2);
        mode.AutomaticReturnMode = "DESACTIVADO";
        await db.SaveChangesAsync();
        var before = await db.AuditLogs.CountAsync();
        var status = (await Service(db).GetAsync(p.Id))!;
        Assert.Equal("INTEGRO_LOCAL", status.ArtifactIntegrity);
        Assert.Equal("PENDIENTE_SIN_TRANSMISION", status.ReconciliationStatus);
        Assert.Contains("MODO_CENIT_DESACTIVADO", status.Alerts);
        Assert.Equal(0, status.ExistingManualReturnCount);
        Assert.Equal(0, status.UnattributableChamberResponseCount);
        Assert.False(status.TransmissionEnabled);
        Assert.False(status.ChamberAcknowledgementAttributed);
        Assert.False(status.FinanciallyReconciled);
        Assert.Equal(before, await db.AuditLogs.CountAsync());
    }

    [Fact]
    public async Task TamperedCiphertextHashOrPhysicalRecordsFailClosed()
    {
        await using var db = NewDb();
        var p = await CreateAsync(db);
        await GenerateFixtureAsync(db, p);
        p.ArtifactSha256 = new string('0', 64);
        await db.SaveChangesAsync();
        Assert.Equal("HASH_NO_COINCIDE", (await Service(db).GetAsync(p.Id))!.ArtifactIntegrity);
        p.ArtifactSha256 = Convert.ToHexString(SHA256.HashData(Encoding.ASCII.GetBytes(ValidLocalPhysical(p))));
        p.ProtectedArtifactContent = "datos-no-protegidos";
        await db.SaveChangesAsync();
        Assert.Equal("CLAVE_O_CIFRADO_NO_DISPONIBLE", (await Service(db).GetAsync(p.Id))!.ArtifactIntegrity);
        await GenerateFixtureAsync(db, p, ValidLocalPhysical(p).Replace("R02", "R23"));
        Assert.Equal("ESTRUCTURA_INVALIDA", (await Service(db).GetAsync(p.Id))!.ArtifactIntegrity);
    }

    [Fact]
    public async Task ManualDuplicateAndExternalAckNeverConfirmI05()
    {
        await using var db = NewDb();
        var p = await CreateAsync(db);
        await GenerateFixtureAsync(db, p);
        db.AchReturnsGenerated.Add(new AchReturnGenerated
        {
            OriginalTransactionId = 42, ReturnCycleId = "CENIT-I03-2",
            ReturnReasonCode = "R02", Amount = 150m, NewSequenceNumber = "876543210000077",
            OriginalSequenceNumber = "123456780000099"
        });
        db.CenitChamberResponses.Add(new CenitChamberResponse
        {
            Id = Guid.NewGuid(), ClearingHouseId = 2, AchTransactionId = 42,
            SourceResponseId = "RESP-I06-1", SourceFileName = "respuesta.xml",
            ResponseType = CenitChamberResponseType.Ack,
            ResultingState = CenitChamberResponseState.Accepted,
            CorrelationOutcome = CenitChamberCorrelationOutcome.Matched,
            ReceivedAtUtc = Now.UtcDateTime,
            RawTechnicalReference = "REF-I06", ContentSha256 = new string('A', 64),
            IdempotencyKey = new string('B', 64)
        });
        await db.SaveChangesAsync();
        var before = await db.AuditLogs.CountAsync();
        var result = (await Service(db).GetAsync(p.Id))!;
        Assert.Equal("CONFLICTO_REQUIERE_REVISION", result.ReconciliationStatus);
        Assert.Equal(1, result.ExistingManualReturnCount);
        Assert.Equal(1, result.UnattributableChamberResponseCount);
        Assert.Contains("DEVOLUCION_EXISTENTE_RUTA_MANUAL", result.Alerts);
        Assert.Contains("RESPUESTA_EXTERNA_NO_ATRIBUIBLE_A_I05", result.Alerts);
        Assert.False(result.ChamberAcknowledgementAttributed);
        Assert.False(result.FinanciallyReconciled);
        Assert.Equal(before, await db.AuditLogs.CountAsync());
    }

    [Fact]
    public async Task LocalTraceFoundInOtherCenitExportIsAConflictNotAnI05Delivery()
    {
        await using var db = NewDb();
        var p = await CreateAsync(db);
        await GenerateFixtureAsync(db, p);
        var export = new AchFileExport
        {
            Id = 9001, ClearingHouseId = 2, AchCycleId = "CENIT-I03-2",
            ExportKind = "NACHA", FileName = "OTRA_RUTA_CENIT", TotalRecords = 1,
            TotalTransactions = 1
        };
        db.AchFileExports.Add(export);
        db.AchFileExportTransactions.Add(new AchFileExportTransaction
        {
            AchFileExport = export, AchTransactionId = 42,
            AchCycleId = "CENIT-I03-2", AchBatchId = 1, FileSequence = 1,
            TraceNumber = p.ArtifactTraceNumber!, Amount = 150m
        });
        await db.SaveChangesAsync();
        var result = (await Service(db).GetAsync(p.Id))!;
        Assert.Equal(1, result.ExistingExportTraceCount);
        Assert.Equal("CONFLICTO_REQUIERE_REVISION", result.ReconciliationStatus);
        Assert.Contains("TRAZA_LOCAL_PRESENTE_EN_OTRA_EXPORTACION", result.Alerts);
        Assert.False(result.ChamberAcknowledgementAttributed);
        Assert.False(result.TransmissionEnabled);
    }

    [Fact]
    public async Task MissingDecryptionKeysAndOtherHouseAckFailClosed()
    {
        await using var db = NewDb();
        var p = await CreateAsync(db);
        await GenerateFixtureAsync(db, p);
        db.CenitChamberResponses.Add(new CenitChamberResponse
        {
            Id = Guid.NewGuid(), ClearingHouseId = 1, AchTransactionId = 42,
            SourceResponseId = "ACK-ACHCOL-1", SourceFileName = "otro.xml",
            ResponseType = CenitChamberResponseType.Ack,
            ResultingState = CenitChamberResponseState.Accepted,
            CorrelationOutcome = CenitChamberCorrelationOutcome.Matched,
            ReceivedAtUtc = Now.UtcDateTime,
            RawTechnicalReference = "ACH-ONLY", ContentSha256 = new string('C', 64),
            IdempotencyKey = new string('D', 64)
        });
        await db.SaveChangesAsync();
        Assert.Equal(0, (await Service(db).GetAsync(p.Id))!.UnattributableChamberResponseCount);
        var broken = (await Service(db, new EphemeralDataProtectionProvider()).GetAsync(p.Id))!;
        Assert.Equal("CLAVE_O_CIFRADO_NO_DISPONIBLE", broken.ArtifactIntegrity);
        Assert.False(broken.ChamberAcknowledgementAttributed);
        Assert.False(broken.FinanciallyReconciled);
    }

    [Fact]
    public async Task WrongHouseAndBoundedListCannotLeakAchColombia()
    {
        await using var db = NewDb();
        var p = await CreateAsync(db);
        var originalCycle = await db.AchCycles.SingleAsync(x => x.Id == p.OriginalCycleId);
        originalCycle.ClearingHouseId = 1;
        originalCycle.ClearingHouse = (await db.ClearingHouses.SingleAsync(x => x.Id == 1));
        await db.SaveChangesAsync();
        Assert.Null(await Service(db).GetAsync(p.Id));
        Assert.Empty((await Service(db).ListAsync()).Items);
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => Service(db).ListAsync(101));
    }

    [Fact]
    public async Task LimitedWindowIsReportedAndApiHasOnlyAuthorizedReads()
    {
        await using var db = NewDb();
        var p = await CreateAsync(db);
        var second = new CenitReturnPreparation
        {
            Id = Guid.NewGuid(), OriginalTransactionId = 42, OriginalTransaction = p.OriginalTransaction,
            SourceExecutionId = Guid.NewGuid(), SourceQueueId = Guid.NewGuid(),
            OriginalCycleId = p.OriginalCycleId, SuggestedReturnCycleId = p.SuggestedReturnCycleId,
            ReasonCode = "R02", PreparedAtUtc = p.PreparedAtUtc.AddSeconds(1)
        };
        db.CenitReturnPreparations.Add(second);
        await db.SaveChangesAsync(); // EF InMemory no aplica índices únicos; prueba sólo paginación.
        var page = await Service(db).ListAsync(1);
        Assert.True(page.LimitedWindow);
        Assert.Single(page.Items);
        Assert.Equal(1, page.ReturnedItems);
        var type = typeof(CenitReturnIntegrationController);
        Assert.NotNull(Attribute.GetCustomAttribute(type, typeof(AuthorizeAttribute)));
        foreach (var method in new[] { nameof(CenitReturnIntegrationController.Get),
                     nameof(CenitReturnIntegrationController.List) })
        {
            var actual = type.GetMethod(method)!;
            Assert.Equal(P1Policies.ConfigRead, actual.GetCustomAttributes(typeof(AuthorizeAttribute), false)
                .Cast<AuthorizeAttribute>().Single().Policy);
            Assert.Single(actual.GetCustomAttributes(typeof(HttpGetAttribute), false));
        }
        Assert.DoesNotContain(type.GetMethods(), m => m.GetCustomAttributes(typeof(HttpPostAttribute), false).Any()
            || m.GetCustomAttributes(typeof(HttpPutAttribute), false).Any()
            || m.GetCustomAttributes(typeof(HttpDeleteAttribute), false).Any());
        Assert.Empty(db.AchFileExports);
        Assert.Empty(db.AchReturnsGenerated);
    }
}
