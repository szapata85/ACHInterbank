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
}
