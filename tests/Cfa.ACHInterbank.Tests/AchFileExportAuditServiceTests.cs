using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class AchFileExportAuditServiceTests
{
    [Fact]
    public async Task RecordGeneratedFileAsync_ShouldBeIdempotentForSameOutput()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new AchDbContext(options);
        var service = new AchFileExportAuditService(context);

        await service.RecordGeneratedFileAsync(
            "f78ca7bae2b80c3034353fc3dbccd801c605e7ee",
            1,
            "NACHA",
            "0000001.001.20260728.1.OUT",
            10,
            1,
            false);
        await service.RecordGeneratedFileAsync(
            "f78ca7bae2b80c3034353fc3dbccd801c605e7ee",
            1,
            "NACHA",
            "0000001.001.20260728.1.OUT",
            10,
            1,
            false);

        context.AchFileExports.Should().ContainSingle();
    }

    [Fact]
    public async Task RecordGeneratedFileAsync_ShouldKeepPlainAndEncryptedEvidenceSeparate()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new AchDbContext(options);
        var service = new AchFileExportAuditService(context);
        const string cycleId = "f78ca7bae2b80c3034353fc3dbccd801c605e7ee";
        const string fileName = "0000001.001.20260728.1.OUT";

        await service.RecordGeneratedFileAsync(cycleId, 1, "NACHA", fileName, 10, 1, false);
        await service.RecordGeneratedFileAsync(cycleId, 1, "NACHA", fileName, 10, 1, true);

        context.AchFileExports.Should().HaveCount(2);
        context.AchFileExports.Select(x => x.IsEncrypted).Should().BeEquivalentTo([false, true]);
    }

    [Fact]
    public async Task NachaAdmission_MovedAwayBeforeAudit_DoesNotWriteExportEvidence()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new AchDbContext(options);
        context.AchTransactions.Add(new Cfa.ACHInterbank.Domain.Models.ACH.AchTransaction
        {
            Id = 101, AchCycleId = "C2", AchBatchId = 52,
            State = Cfa.ACHInterbank.Domain.Entities.Transactions.Enums.AchTransferStateEnum.Pending
        });
        await context.SaveChangesAsync();
        var service = new AchFileExportAuditService(context);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RecordGeneratedFileAsync("C1", 1, "NACHA", "f.txt", 4, 1, false,
                new[] { 101 }, "ABCDEF"));
        Assert.Contains("verificar todas", exception.Message);
        Assert.Empty(await context.AchFileExports.ToListAsync());
    }

    [Fact]
    public async Task NachaAdmission_DoesNotReplaceExistingMembershipOnSameFileName()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        await using var context = new AchDbContext(options);
        const string cycle = "C1";
        context.AchTransactions.AddRange(
            new Cfa.ACHInterbank.Domain.Models.ACH.AchTransaction
            { Id = 201, AchCycleId = cycle, AchBatchId = 51,
                State = Cfa.ACHInterbank.Domain.Entities.Transactions.Enums.AchTransferStateEnum.Pending },
            new Cfa.ACHInterbank.Domain.Models.ACH.AchTransaction
            { Id = 202, AchCycleId = cycle, AchBatchId = 51,
                State = Cfa.ACHInterbank.Domain.Entities.Transactions.Enums.AchTransferStateEnum.Pending });
        await context.SaveChangesAsync();
        var service = new AchFileExportAuditService(context);
        await service.RecordGeneratedFileAsync(cycle, 1, "NACHA", "same.txt", 4, 1, false,
            new[] { 201 }, "ABCDEF");
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.RecordGeneratedFileAsync(cycle, 1, "NACHA", "same.txt", 4, 1, false,
                new[] { 202 }, "ABCDEF"));
        Assert.Single(await context.AchFileExportTransactions.ToListAsync());
    }
}
