using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Domain.Models.ACH;
using FluentAssertions;
using Xunit;

namespace Cfa.ACHInterbank.Tests.Mapping;

public class DailyResetBatchNumberGeneratorTests
{
    [Fact]
    public void AssignBatchNumbers_ShouldResetByDayAndChamber()
    {
        var sut = new DailyResetBatchNumberGenerator();
        var batches = new List<AchBatch>
        {
            new() { Id = 1, OriginOrOdfi = "11111111" },
            new() { Id = 2, OriginOrOdfi = "11111111" }
        };

        var achDay1 = sut.AssignBatchNumbers(batches, "ACH", new DateTime(2026, 4, 19, 10, 0, 0, DateTimeKind.Utc));
        var achDay2 = sut.AssignBatchNumbers(batches, "ACH", new DateTime(2026, 4, 20, 10, 0, 0, DateTimeKind.Utc));
        var cenitDay1 = sut.AssignBatchNumbers(batches, "CENIT", new DateTime(2026, 4, 19, 10, 0, 0, DateTimeKind.Utc));

        achDay1.BatchNumberByBatchId[1].Should().Be(1);
        achDay1.BatchNumberByBatchId[2].Should().Be(2);
        achDay2.BatchNumberByBatchId[1].Should().Be(1);
        cenitDay1.BatchNumberByBatchId[1].Should().Be(1);
    }

    [Fact]
    public void AssignBatchNumbers_ShouldScopeByOriginatingDfi()
    {
        var sut = new DailyResetBatchNumberGenerator();
        var batches = new List<AchBatch>
        {
            new() { Id = 10, OriginOrOdfi = "11111111" },
            new() { Id = 11, OriginOrOdfi = "22222222" },
            new() { Id = 12, OriginOrOdfi = "11111111" }
        };

        var result = sut.AssignBatchNumbers(batches, "ACH", new DateTime(2026, 4, 19, 0, 0, 0, DateTimeKind.Utc));

        result.BatchNumberByBatchId[10].Should().Be(1);
        result.BatchNumberByBatchId[11].Should().Be(1);
        result.BatchNumberByBatchId[12].Should().Be(2);
        result.PolicyCode.Should().Be("DAILY_RESET_BY_CHAMBER_DATE_ORIGINATING_DFI");
    }
}
