using System.Data;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Models.ACH.ExternalFileNames;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cfa.ACHInterbank.Tests;

[Trait("Category", "Postgres")]
[Trait("Category", "Integration")]
[Trait("Category", "ExternalFileName")]
public class PostgresExternalFileNameIntegrationTests
{
    [Fact]
    public async Task PostgresExternalFileNameSequence_ShouldReserveUniqueSequentialValuesUnderConcurrency()
    {
        await using var harness = await PostgresHarness.CreateAsync();
        if (harness.IsDisabled) return;

        var context = CreateSequenceContext(harness.ClearingHouseId, harness.ProcessingDate, "CENIT");
        var tasks = Enumerable.Range(0, 50)
            .Select(_ => ReserveSequenceWithFreshContextAsync(harness.ConnectionString, context))
            .ToArray();

        var values = await Task.WhenAll(tasks);

        Assert.Equal(50, values.Length);
        Assert.Equal(50, values.Distinct().Count());
        Assert.Equal(1, values.Min());
        Assert.Equal(50, values.Max());
    }

    [Fact]
    public async Task PostgresExternalFileNameReservation_ShouldCollapseFiftyConcurrentRetries()
    {
        await using var harness = await PostgresHarness.CreateAsync();
        if (harness.IsDisabled) return;

        var request = new ExternalFileNameContext
        {
            ClearingHouseId = harness.ClearingHouseId,
            ClearingHouseCode = "CENIT",
            ClearingHouseOriginCode = "1234567",
            ProcessingDate = harness.ProcessingDate,
            ExternalFileType = ExternalFileType.NachaOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound,
            RequestedBy = "postgres-integration",
            IdempotencyKey = "synthetic-concurrent-retry"
        };
        var tasks = Enumerable.Range(0, 50)
            .Select(_ => ReserveIdempotentWithFreshContextAsync(harness.ConnectionString, request))
            .ToArray();

        var reservations = await Task.WhenAll(tasks);

        Assert.All(reservations, item => Assert.Equal(1, item.Sequence));
        Assert.Single(reservations.Select(item => item.ReservationId).Distinct());
        await using var verification = harness.CreateContext();
        Assert.Equal(1, await verification.ExternalFileNameReservations.CountAsync());
        Assert.Equal(1, (await verification.ExternalFileSequences.SingleAsync()).LastValue);
    }

    [Fact]
    public async Task PostgresExternalFileNameSequence_ShouldResetPerDay()
    {
        await using var harness = await PostgresHarness.CreateAsync();
        if (harness.IsDisabled) return;

        var day1 = CreateSequenceContext(harness.ClearingHouseId, new DateTime(2026, 04, 20), "ACH");
        var day2 = CreateSequenceContext(harness.ClearingHouseId, new DateTime(2026, 04, 21), "ACH");

        var firstDay = await ReserveSequenceWithFreshContextAsync(harness.ConnectionString, day1);
        var nextDay = await ReserveSequenceWithFreshContextAsync(harness.ConnectionString, day2);

        Assert.Equal(1, firstDay);
        Assert.Equal(1, nextDay);
    }

    [Fact]
    public async Task PostgresExternalFileNameSequence_ShouldBeIsolatedByClearingHouseAndDate()
    {
        await using var harness = await PostgresHarness.CreateAsync();
        if (harness.IsDisabled) return;

        var achDay1 = CreateSequenceContext(harness.ClearingHouseId, new DateTime(2026, 04, 20), "ACH");
        var achDay2 = CreateSequenceContext(harness.ClearingHouseId, new DateTime(2026, 04, 21), "ACH");
        var cenitDay1 = CreateSequenceContext(harness.ClearingHouseId + 1, new DateTime(2026, 04, 20), "CENIT");

        var achFirst = await ReserveSequenceWithFreshContextAsync(harness.ConnectionString, achDay1);
        var achSecond = await ReserveSequenceWithFreshContextAsync(harness.ConnectionString, achDay1);
        var achNextDay = await ReserveSequenceWithFreshContextAsync(harness.ConnectionString, achDay2);
        var cenitFirst = await ReserveSequenceWithFreshContextAsync(harness.ConnectionString, cenitDay1);

        Assert.Equal(1, achFirst);
        Assert.Equal(2, achSecond);
        Assert.Equal(1, achNextDay);
        Assert.Equal(1, cenitFirst);
    }

    [Fact]
    public async Task PostgresExternalFileNameSequence_ShouldIsolateReturnOutFromNachaOutOnSameDay()
    {
        await using var harness = await PostgresHarness.CreateAsync();
        if (harness.IsDisabled) return;

        var nachaOut = harness.NewExternalFileContext(ExternalFileType.NachaOut, ExternalFileFlow.Originacion, ExternalFileDirection.Outbound);
        var returnOut = harness.NewExternalFileContext(ExternalFileType.ReturnOut, ExternalFileFlow.Originacion, ExternalFileDirection.Outbound);

        var nachaFirst = await ReserveSequenceWithFreshContextAsync(harness.ConnectionString, nachaOut);
        var returnFirst = await ReserveSequenceWithFreshContextAsync(harness.ConnectionString, returnOut);
        var nachaSecond = await ReserveSequenceWithFreshContextAsync(harness.ConnectionString, nachaOut);
        var returnSecond = await ReserveSequenceWithFreshContextAsync(harness.ConnectionString, returnOut);

        Assert.Equal(1, nachaFirst);
        Assert.Equal(1, returnFirst);
        Assert.Equal(2, nachaSecond);
        Assert.Equal(2, returnSecond);
    }

    [Fact]
    public async Task PostgresExternalFileNameRegistry_ShouldPersistValidationEvidence()
    {
        await using var harness = await PostgresHarness.CreateAsync();
        if (harness.IsDisabled) return;
        await using var context = harness.CreateContext();
        var audit = new ExternalFileNameAuditService(context);

        var policyResult = new ExternalFileNamePolicyResult
        {
            ExternalFileName = "1234567.001.1",
            Components = new ExternalFileNameComponents { FullName = "1234567.001.1", ExternalSequence = 1 },
            CorrelationEvidence = new ExternalFileNameCorrelationEvidence { ParsedSequence = 1, HeaderFileIdModifier = 'A' },
            Validation = new ExternalFileNameValidationResult
            {
                Disposition = ExternalFileValidationDisposition.Warning,
                Issues =
                [
                    new ExternalFileNameValidationIssue
                    {
                        RuleCode = "W1",
                        IssueCode = "WARN",
                        Message = "warning",
                        Disposition = ExternalFileValidationDisposition.Warning,
                        Evidence = "evidence-json"
                    }
                ]
            }
        };

        await audit.RegisterAsync(harness.NewExternalFileContext(ExternalFileType.NachaOut, ExternalFileFlow.Originacion, ExternalFileDirection.Outbound), policyResult);

        var registry = await context.ExternalFileNameRegistry.SingleAsync();
        var log = await context.ExternalFileNameValidationLog.SingleAsync();

        Assert.Contains("W1", registry.ValidationIssuesJson, StringComparison.Ordinal);
        Assert.Contains("ParsedSequence", registry.CorrelationEvidenceJson, StringComparison.Ordinal);
        Assert.Equal("W1", log.RuleCode);
    }

    [Fact]
    public async Task PostgresDuplicateGuard_ShouldDetectDuplicateForCenitStaReject()
    {
        await using var harness = await PostgresHarness.CreateAsync();
        if (harness.IsDisabled) return;
        await using var context = harness.CreateContext();
        context.ExternalFileNameRegistry.Add(new ExternalFileNameRegistry
        {
            ClearingHouseId = harness.ClearingHouseId,
            FlowCode = ExternalFileFlow.Rechazo.ToString(),
            Direction = ExternalFileDirection.Inbound.ToString(),
            ExternalFileName = "STA.REJECT.000002.txt",
            ExternalFileType = ExternalFileType.StaReject.ToString(),
            ProcessingDate = harness.ProcessingDate,
            ValidationDisposition = "Passed",
            ValidationResult = "Accepted",
            ValidationIssuesJson = "[]",
            CorrelationEvidenceJson = "{}",
            CreatedBy = "integration-test"
        });
        await context.SaveChangesAsync();

        var validator = new ExternalFileNameValidator(new ExternalFileDuplicateGuard(context), new ExternalFileNameCorrelationService(new FakeIdentifierMapService()), new FakeIdentifierMapService());
        var result = await validator.ValidateAsync(
            harness.NewExternalFileContext(ExternalFileType.StaReject, ExternalFileFlow.Rechazo, ExternalFileDirection.Inbound),
            new ExternalFileNameComponents { FullName = "STA.REJECT.000002.txt", DeclaredDetailCount = 2 });

        Assert.Equal(ExternalFileValidationDisposition.HardBlock, result.Disposition);
        Assert.Contains(result.Issues, issue => issue.RuleCode == "STA_D04");
    }

    [Fact]
    public async Task PostgresFilenamePolicy_ShouldHardBlockAchWhenZzzDoesNotMatchR1()
    {
        await using var harness = await PostgresHarness.CreateAsync();
        if (harness.IsDisabled) return;
        await using var context = harness.CreateContext();
        var validator = new ExternalFileNameValidator(new ExternalFileDuplicateGuard(context), new ExternalFileNameCorrelationService(new FakeIdentifierMapService()), new FakeIdentifierMapService());

        var result = await validator.ValidateAsync(
            CreateContextWithNacha(harness, ExternalFileType.NachaOut, ExternalFileFlow.Originacion, ExternalFileDirection.Outbound, BuildNachaHeader('C')),
            new ExternalFileNameComponents { FullName = "1234567.001.1" });

        Assert.Equal(ExternalFileValidationDisposition.HardBlock, result.Disposition);
        Assert.Contains(result.Issues, issue => issue.RuleCode == "ACH_ZZZ_R1");
    }

    [Fact]
    public async Task PostgresFilenamePolicy_ShouldHardBlockCenitD05WhenDeclaredCountDiffers()
    {
        await using var harness = await PostgresHarness.CreateAsync();
        if (harness.IsDisabled) return;
        await using var context = harness.CreateContext();
        var validator = new ExternalFileNameValidator(new ExternalFileDuplicateGuard(context), new ExternalFileNameCorrelationService(new FakeIdentifierMapService()), new FakeIdentifierMapService());

        var result = await validator.ValidateAsync(
            CreateContextWithCounts(harness, 10, "6record\n6record\n"),
            new ExternalFileNameComponents { FullName = "STA.REJECT.000010.txt", DeclaredDetailCount = 10 });

        Assert.Equal(ExternalFileValidationDisposition.HardBlock, result.Disposition);
        Assert.Contains(result.Issues, issue => issue.RuleCode == "STA_D05");
    }

    [Fact]
    public async Task PostgresExternalFileNameSequence_ShouldRespectAchDailyLimit36()
    {
        await using var harness = await PostgresHarness.CreateAsync();
        if (harness.IsDisabled) return;

        var context = harness.NewExternalFileContext(ExternalFileType.NachaOut, ExternalFileFlow.Originacion, ExternalFileDirection.Outbound);
        for (var i = 0; i < 36; i++)
        {
            await ReserveSequenceWithFreshContextAsync(harness.ConnectionString, context);
        }

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => ReserveSequenceWithFreshContextAsync(harness.ConnectionString, context));
        Assert.Contains("máximo 36", ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PostgresSchema_ShouldContainExternalFileNameTablesAndConstraints()
    {
        await using var harness = await PostgresHarness.CreateAsync();
        if (harness.IsDisabled) return;
        await using var connection = new NpgsqlConnection(harness.ConnectionString);
        await connection.OpenAsync();

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = """
            SELECT count(*)
            FROM information_schema.tables
            WHERE table_schema = current_schema()
              AND table_name IN ('ExternalFileSequences','ExternalFileNameRegistry','ExternalFileNameValidationLog','BatchNumberSequences');
            """;
        var tableCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        Assert.Equal(4, tableCount);

        cmd.CommandText = """
            SELECT count(*)
            FROM pg_indexes
            WHERE schemaname=current_schema()
              AND tablename='ExternalFileSequences'
              AND indexname='IX_ExternalFileSequences_ClearingHouseId_ScopeCode_SequenceDate';
            """;
        var indexCount = Convert.ToInt32(await cmd.ExecuteScalarAsync());
        Assert.Equal(1, indexCount);
    }

    private static ExternalFileNameContext CreateContextWithNacha(
        PostgresHarness harness,
        ExternalFileType fileType,
        ExternalFileFlow flow,
        ExternalFileDirection direction,
        string nachaContent)
    {
        return new ExternalFileNameContext
        {
            ClearingHouseId = harness.ClearingHouseId,
            ClearingHouseCode = fileType is ExternalFileType.StaReject or ExternalFileType.StaOut ? "CENIT" : "ACH",
            ClearingHouseOriginCode = "1234567",
            ProcessingDate = harness.ProcessingDate,
            ExternalFileType = fileType,
            Flow = flow,
            Direction = direction,
            RequestedBy = "postgres-integration",
            NachaContent = nachaContent
        };
    }

    private static ExternalFileNameContext CreateContextWithCounts(PostgresHarness harness, int declaredDetailCount, string nachaContent)
    {
        return new ExternalFileNameContext
        {
            ClearingHouseId = harness.ClearingHouseId,
            ClearingHouseCode = "CENIT",
            ClearingHouseOriginCode = "1234567",
            ProcessingDate = harness.ProcessingDate,
            ExternalFileType = ExternalFileType.StaReject,
            Flow = ExternalFileFlow.Rechazo,
            Direction = ExternalFileDirection.Inbound,
            RequestedBy = "postgres-integration",
            DeclaredDetailCount = declaredDetailCount,
            NachaContent = nachaContent
        };
    }

    private static ExternalFileNameContext CreateSequenceContext(int clearingHouseId, DateTime processingDate, string clearingHouseCode)
    {
        return new ExternalFileNameContext
        {
            ClearingHouseId = clearingHouseId,
            ClearingHouseCode = clearingHouseCode,
            ClearingHouseOriginCode = "1234567",
            ProcessingDate = processingDate,
            ExternalFileType = ExternalFileType.NachaOut,
            Flow = ExternalFileFlow.Originacion,
            Direction = ExternalFileDirection.Outbound,
            RequestedBy = "postgres-integration"
        };
    }

    private static async Task<int> ReserveSequenceWithFreshContextAsync(string connectionString, ExternalFileNameContext sequenceContext)
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        await using var context = new AchDbContext(options);
        var adapter = new PostgresExternalFileNameSequenceService(context);
        return await adapter.ReserveNextSequenceAsync(sequenceContext);
    }

    private static async Task<ExternalFileNameReservationResult> ReserveIdempotentWithFreshContextAsync(
        string connectionString,
        ExternalFileNameContext sequenceContext)
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        await using var context = new AchDbContext(options);
        var provider = new PostgresExternalFileNameSequenceService(context);
        var resolver = new ExternalFileNameSequenceProviderResolver([provider]);
        var sequence = new ExternalFileNameSequenceService(context, resolver);
        var reservation = new ExternalFileNameReservationService(context, sequence);
        var result = await reservation.ReserveAsync(sequenceContext, "synthetic-fingerprint-v1");
        await reservation.CompleteAsync(result.ReservationId, "1234567.001.1", null);
        return result;
    }

    private static string BuildNachaHeader(char fileId)
    {
        var chars = Enumerable.Repeat('1', 106).ToArray();
        chars[35] = fileId;
        return new string(chars);
    }

    private sealed class FakeIdentifierMapService : INachaFileIdentifierMapService
    {
        public Task<char> ResolveIdentifierAsync(int sequence, CancellationToken ct = default)
        {
            if (sequence is < 1 or > 36)
            {
                throw new InvalidOperationException("Sequence out of range");
            }

            return Task.FromResult(sequence <= 26 ? (char)('A' + (sequence - 1)) : (char)('0' + (sequence - 27)));
        }
    }

    private sealed class PostgresHarness : IAsyncDisposable
    {
        private readonly NpgsqlConnection _adminConnection;
        private readonly string _schemaName;

        private PostgresHarness(string connectionString, NpgsqlConnection adminConnection, string schemaName)
        {
            ConnectionString = connectionString;
            _adminConnection = adminConnection;
            _schemaName = schemaName;
        }

        public bool IsDisabled { get; private set; }
        public string ConnectionString { get; }
        public int ClearingHouseId { get; } = Random.Shared.Next(4000, 9000);
        public DateTime ProcessingDate { get; } = new(2026, 04, 20, 0, 0, 0, DateTimeKind.Utc);

        public static async Task<PostgresHarness> CreateAsync()
        {
            var requirePostgresTests = string.Equals(
                Environment.GetEnvironmentVariable("REQUIRE_POSTGRES_TESTS"),
                "true",
                StringComparison.OrdinalIgnoreCase);

            var cs = Environment.GetEnvironmentVariable("POSTGRES_TEST_CONNECTION_STRING")
                     ?? Environment.GetEnvironmentVariable("ConnectionStrings__PostgresConnection");

            if (string.IsNullOrWhiteSpace(cs))
            {
                if (requirePostgresTests)
                {
                    throw new InvalidOperationException(
                        "REQUIRE_POSTGRES_TESTS=true but no PostgreSQL connection string was provided. Set POSTGRES_TEST_CONNECTION_STRING or ConnectionStrings__PostgresConnection.");
                }

                Console.WriteLine("[PostgresIntegration] Missing POSTGRES_TEST_CONNECTION_STRING / ConnectionStrings__PostgresConnection. Test skipped by early return.");
                return new PostgresHarness(string.Empty, new NpgsqlConnection(), string.Empty) { IsDisabled = true };
            }

            NpgsqlConnection adminConnection;
            try
            {
                adminConnection = new NpgsqlConnection(cs);
                await adminConnection.OpenAsync();
            }
            catch (Exception ex) when (!requirePostgresTests)
            {
                Console.WriteLine($"[PostgresIntegration] PostgreSQL is unreachable and REQUIRE_POSTGRES_TESTS is not true. Tests skipped. Error: {ex.Message}");
                return new PostgresHarness(string.Empty, new NpgsqlConnection(), string.Empty) { IsDisabled = true };
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "REQUIRE_POSTGRES_TESTS=true and PostgreSQL connection failed. Check container health and connection string values.",
                    ex);
            }

            var schemaName = $"it_{Guid.NewGuid():N}";
            await using var cmd = adminConnection.CreateCommand();
            cmd.CommandText = $"CREATE SCHEMA IF NOT EXISTS \"{schemaName}\";";
            await cmd.ExecuteNonQueryAsync();

            var builder = new NpgsqlConnectionStringBuilder(cs)
            {
                SearchPath = schemaName
            };

            var options = new DbContextOptionsBuilder<AchDbContext>()
                .UseNpgsql(builder.ConnectionString)
                .Options;
            await using var context = new AchDbContext(options);
            await context.Database.MigrateAsync();

            return new PostgresHarness(builder.ConnectionString, adminConnection, schemaName);
        }

        public AchDbContext CreateContext()
        {
            if (IsDisabled)
            {
                throw new InvalidOperationException("Postgres harness is disabled in this environment.");
            }
            var options = new DbContextOptionsBuilder<AchDbContext>()
                .UseNpgsql(ConnectionString)
                .Options;
            return new AchDbContext(options);
        }

        public ExternalFileNameContext NewExternalFileContext(ExternalFileType fileType, ExternalFileFlow flow, ExternalFileDirection direction)
            => new()
            {
                ClearingHouseId = ClearingHouseId,
                ClearingHouseCode = fileType is ExternalFileType.StaReject or ExternalFileType.StaOut ? "CENIT" : "ACH",
                ClearingHouseOriginCode = "1234567",
                ProcessingDate = ProcessingDate,
                ExternalFileType = fileType,
                Flow = flow,
                Direction = direction,
                RequestedBy = "postgres-integration"
            };

        public async ValueTask DisposeAsync()
        {
            if (IsDisabled)
            {
                return;
            }

            if (_adminConnection.State != ConnectionState.Open)
            {
                await _adminConnection.OpenAsync();
            }

            await using var cmd = _adminConnection.CreateCommand();
            cmd.CommandText = $"DROP SCHEMA IF EXISTS \"{_schemaName}\" CASCADE;";
            await cmd.ExecuteNonQueryAsync();

            await _adminConnection.DisposeAsync();
        }
    }
}
