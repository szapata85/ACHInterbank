using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

[Trait("Category", "SqlServer")]
[Trait("Category", "Integration")]
[Trait("Category", "ExternalFileName")]
public class SqlServerExternalFileNameIntegrationTests
{
    [Fact]
    public async Task SqlServerSequence_ShouldReserveFiftyUniqueValuesUnderConcurrency()
    {
        var connectionString = RequireConnectionStringOrSkip();
        if (connectionString is null) return;
        var clearingHouseId = Random.Shared.Next(20_000, 30_000);
        var request = CreateContext(clearingHouseId, "CENIT");

        var values = await Task.WhenAll(Enumerable.Range(0, 50)
            .Select(_ => ReserveSequenceWithFreshContextAsync(connectionString, request)));

        Assert.Equal(50, values.Distinct().Count());
        Assert.Equal(1, values.Min());
        Assert.Equal(50, values.Max());
    }

    [Fact]
    public async Task SqlServerReservation_ShouldCollapseFiftyConcurrentRetries()
    {
        var connectionString = RequireConnectionStringOrSkip();
        if (connectionString is null) return;
        var clearingHouseId = Random.Shared.Next(30_001, 40_000);
        var request = CreateContext(clearingHouseId, "CENIT", "synthetic-sqlserver-concurrent-retry");

        var reservations = await Task.WhenAll(Enumerable.Range(0, 50)
            .Select(_ => ReserveIdempotentWithFreshContextAsync(connectionString, request)));

        Assert.All(reservations, item => Assert.Equal(1, item.Sequence));
        Assert.Single(reservations.Select(item => item.ReservationId).Distinct());
        await using var verification = CreateContext(connectionString);
        Assert.Equal(1, await verification.ExternalFileNameReservations.CountAsync(x => x.ClearingHouseId == clearingHouseId));
        Assert.Equal(1, (await verification.ExternalFileSequences.SingleAsync(x => x.ClearingHouseId == clearingHouseId)).LastValue);
    }

    [Fact]
    public async Task SqlServerSequence_ShouldResetOnOperationalDateAndFailClosedAtAchLimit()
    {
        var connectionString = RequireConnectionStringOrSkip();
        if (connectionString is null) return;
        var clearingHouseId = Random.Shared.Next(40_001, 50_000);
        var dayOne = CreateContext(clearingHouseId, "ACHCOL");
        for (var sequence = 1; sequence <= 36; sequence++)
        {
            Assert.Equal(sequence, await ReserveSequenceWithFreshContextAsync(connectionString, dayOne));
        }

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ReserveSequenceWithFreshContextAsync(connectionString, dayOne));

        var dayTwo = CreateContext(clearingHouseId, "ACHCOL", operationalDate: new DateTime(2026, 7, 17));
        Assert.Equal(1, await ReserveSequenceWithFreshContextAsync(connectionString, dayTwo));
        await using var verification = CreateContext(connectionString);
        Assert.Equal(36, (await verification.ExternalFileSequences.SingleAsync(
            x => x.ClearingHouseId == clearingHouseId && x.SequenceDate == new DateOnly(2026, 7, 16))).LastValue);
    }

    [Fact]
    public async Task SqlServerSchema_ShouldContainReservationConstraints()
    {
        var connectionString = RequireConnectionStringOrSkip();
        if (connectionString is null) return;
        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT COUNT(*)
            FROM sys.indexes
            WHERE name IN (
                'UX_ExternalFileNameReservations_Idempotency',
                'UX_ExternalFileNameReservations_Sequence',
                'UX_ExternalFileNameRegistry_GenerationReservation');
            """;

        Assert.Equal(3, Convert.ToInt32(await command.ExecuteScalarAsync()));
    }

    private static async Task<int> ReserveSequenceWithFreshContextAsync(
        string connectionString,
        ExternalFileNameContext request)
    {
        await using var context = CreateContext(connectionString);
        return await new SqlServerExternalFileNameSequenceService(context).ReserveNextSequenceAsync(request);
    }

    private static async Task<ExternalFileNameReservationResult> ReserveIdempotentWithFreshContextAsync(
        string connectionString,
        ExternalFileNameContext request)
    {
        await using var context = CreateContext(connectionString);
        var provider = new SqlServerExternalFileNameSequenceService(context);
        var resolver = new ExternalFileNameSequenceProviderResolver([provider]);
        var sequence = new ExternalFileNameSequenceService(context, resolver);
        var reservation = new ExternalFileNameReservationService(context, sequence);
        var result = await reservation.ReserveAsync(request, "synthetic-fingerprint-v1");
        await reservation.CompleteAsync(result.ReservationId, "1234567.001.1", null);
        return result;
    }

    private static AchDbContext CreateContext(string connectionString)
        => new(new DbContextOptionsBuilder<AchDbContext>().UseSqlServer(connectionString).Options);

    private static ExternalFileNameContext CreateContext(
        int clearingHouseId,
        string clearingHouseCode,
        string? idempotencyKey = null,
        DateTime? operationalDate = null)
        => new()
        {
            ClearingHouseId = clearingHouseId,
            ClearingHouseCode = clearingHouseCode,
            ClearingHouseOriginCode = "1234567",
            ProcessingDate = operationalDate ?? new DateTime(2026, 7, 16),
            ExternalFileType = ExternalFileType.NachaOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound,
            IdempotencyKey = idempotencyKey,
            RequestedBy = "sqlserver-integration"
        };

    private static string? RequireConnectionStringOrSkip()
    {
        var connectionString = Environment.GetEnvironmentVariable("SQLSERVER_TEST_CONNECTION_STRING");
        var required = string.Equals(
            Environment.GetEnvironmentVariable("REQUIRE_SQLSERVER_TESTS"),
            "true",
            StringComparison.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(connectionString))
        {
            return connectionString;
        }

        if (required)
        {
            throw new InvalidOperationException("REQUIRE_SQLSERVER_TESTS=true but SQLSERVER_TEST_CONNECTION_STRING is missing.");
        }

        Console.WriteLine("[SqlServerIntegration] Test skipped because SQLSERVER_TEST_CONNECTION_STRING is not configured.");
        return null;
    }
}
