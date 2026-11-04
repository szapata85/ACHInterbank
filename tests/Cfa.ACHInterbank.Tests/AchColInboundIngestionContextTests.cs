using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class AchColInboundIngestionContextTests
{
    [Theory]
    [InlineData("0001283.008.20260724.8.OUT", "H", 8, 8, "PlainNacha")]
    [InlineData("0001283.009.20260724.9.OUT.env", "I", 9, 9, "DigitalEnvelope")]
    [InlineData("0001283.008.20260724.10.OUT.env", "H", 8, 10, "DigitalEnvelope")]
    [InlineData("0001283.036.20260724.123.OUT", "9", 36, 123, "PlainNacha")]
    public async Task ValidatedMetadata_SurvivesRelationalRoundTrip_WithoutAchCycle(
        string fileName, string modifier, int sequence, int cycle, string protection)
    {
        Assert.True(AchColInboundFileNameParser.TryParse(fileName, out var parsed, out var error), error);
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options;
        var ingestionId = Guid.NewGuid();
        await using (var context = new AchDbContext(options))
        {
            await context.Database.EnsureCreatedAsync();
            var ingestion = new IncomingNachaFileIngestion
            {
                Id = ingestionId, FileName = fileName, FileHashSha256 = new string('a', 64),
                FileSize = 106, ContentType = "text/plain", FileExtension = fileName.EndsWith(".env") ? ".env" : ".OUT",
                UploadedBy = "test", CorrelationId = Guid.NewGuid().ToString(),
                ResolutionMode = "AchColFileBased", CycleResolutionStatus = IncomingNachaCycleResolutionStatus.ResueltoSinCiclo
            };
            AchColInboundIngestionContext.ApplyValidated(ingestion, parsed!);
            context.IncomingNachaFileIngestions.Add(ingestion);
            await context.SaveChangesAsync();
        }
        await using (var context = new AchDbContext(options))
        {
            var restored = await context.IncomingNachaFileIngestions.AsNoTracking().SingleAsync(x => x.Id == ingestionId);
            Assert.Equal(fileName, restored.FileName);
            Assert.Equal(parsed!.CanonicalPlainFileName, restored.CanonicalPlainFileName);
            Assert.Equal(modifier, restored.FileIdModifier);
            Assert.Equal(sequence, restored.FileSequence);
            Assert.Equal(cycle, restored.ExternalCycleNumber);
            Assert.Equal(protection, restored.FileProtectionKind);
            Assert.Equal(new DateTime(2026, 7, 24), restored.FileNameDate);
            Assert.Equal("AchColFileBased", restored.ResolutionMode);
            Assert.Equal(IncomingNachaCycleResolutionStatus.ResueltoSinCiclo, restored.CycleResolutionStatus);
            Assert.Null(restored.ResolvedAchCycleId);
        }
    }

    [Fact]
    public void Metadata_MustMatchOriginalPhysicalFilename()
    {
        Assert.True(AchColInboundFileNameParser.TryParse("0001283.008.20260724.8.OUT", out var parsed, out _));
        var ingestion = new IncomingNachaFileIngestion { FileName = "0001283.009.20260724.9.OUT" };
        Assert.Throws<ArgumentException>(() => AchColInboundIngestionContext.ApplyValidated(ingestion, parsed!));
        Assert.Null(ingestion.CanonicalPlainFileName);
        Assert.Null(ingestion.ExternalCycleNumber);
    }

    [Fact]
    public async Task LegacyIngestion_KeepsNullableMetadata()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options;
        await using var db = new AchDbContext(options);
        await db.Database.EnsureCreatedAsync();
        var row = new IncomingNachaFileIngestion
        {
            FileName = "legacy.nacha", FileHashSha256 = new string('b', 64),
            FileSize = 12, ContentType = "text/plain", FileExtension = ".nacha",
            UploadedBy = "test", CorrelationId = Guid.NewGuid().ToString()
        };
        db.IncomingNachaFileIngestions.Add(row);
        await db.SaveChangesAsync();
        Assert.Null(row.CanonicalPlainFileName);
        Assert.Null(row.FileIdModifier);
        Assert.Null(row.FileSequence);
        Assert.Null(row.ExternalCycleNumber);
        Assert.Null(row.FileProtectionKind);
    }
}
