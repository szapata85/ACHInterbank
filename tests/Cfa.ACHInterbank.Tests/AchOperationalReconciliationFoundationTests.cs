using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class AchOperationalReconciliationFoundationTests
{
    private static readonly DateOnly OperationalDate = new(2026, 8, 7);

    [Fact]
    public async Task Reconcile_ShouldPersistCanonicalCycleSnapshot_WithV35AggregatesAndBalancedEvidence()
    {
        await using var context = BuildInMemoryContext();
        SeedCoreScenario(context);

        var result = await new AchOperationalReconciliationService(context).ReconcileAsync(Request(MatchingEvidence()));

        result.ReusedExistingRevision.Should().BeFalse();
        result.Snapshot.Status.Should().Be(AchOperationalReconciliationStatus.Balanced);
        result.Snapshot.Should().BeEquivalentTo(new
        {
            ClearingHouseId = 10,
            OperationalDate,
            AchCycleId = "ACH-20260807-C1",
            Revision = 1,
            SentCount = 2,
            SentAmount = 110m,
            ReceivedCount = 2,
            ReceivedAmount = 100m,
            AppliedCount = 1,
            AppliedAmount = 60m,
            ParticipantReturnCount = 1,
            ParticipantReturnAmount = 40m,
            OperatorReturnCount = 1,
            OperatorReturnAmount = 10m,
            InternalExpectedNetPosition = -40m
        });
        result.Snapshot.Differences.Should().BeEmpty();
        (await context.AchOperationalReconciliationSnapshots.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Reconcile_WithoutExternalEvidence_ShouldFailClosedAsPending()
    {
        await using var context = BuildInMemoryContext();
        SeedCoreScenario(context);

        var result = await new AchOperationalReconciliationService(context).ReconcileAsync(Request());

        result.Snapshot.Status.Should().Be(AchOperationalReconciliationStatus.PendingExternalEvidence);
        result.Snapshot.ExternalEvidenceReference.Should().BeNull();
    }

    [Fact]
    public async Task Reconcile_ShouldPersistAuditableDeltas_ForExternalDifferences()
    {
        await using var context = BuildInMemoryContext();
        SeedCoreScenario(context);
        var evidence = MatchingEvidence() with { ReceivedCount = 3, ReceivedAmount = 125m };

        var result = await new AchOperationalReconciliationService(context).ReconcileAsync(Request(evidence));

        result.Snapshot.Status.Should().Be(AchOperationalReconciliationStatus.Differences);
        result.Snapshot.Differences.Should().ContainEquivalentOf(new
        {
            Category = AchOperationalReconciliationDifferenceCategory.ExternalReceivedCount,
            InternalValue = (decimal?)2m,
            ExternalValue = (decimal?)3m,
            Delta = (decimal?)-1m,
            EvidenceSource = "PLANILLA-ACH-C1"
        });
        result.Snapshot.Differences.Should().ContainEquivalentOf(new
        {
            Category = AchOperationalReconciliationDifferenceCategory.ExternalReceivedAmount,
            InternalValue = (decimal?)100m,
            ExternalValue = (decimal?)125m,
            Delta = (decimal?)-25m,
            EvidenceSource = "PLANILLA-ACH-C1"
        });
    }

    [Fact]
    public async Task Reconcile_ShouldEnforceReceivedEqualsAppliedPlusParticipantReturns()
    {
        await using var context = BuildInMemoryContext();
        SeedCoreScenario(context);
        context.AchTransactions.Add(Transaction(5, 25m, AchTransactionDirection.Incoming, AchTransferStateEnum.Pending, 2, 1));
        await context.SaveChangesAsync();

        var result = await new AchOperationalReconciliationService(context).ReconcileAsync(Request());

        result.Snapshot.Status.Should().Be(AchOperationalReconciliationStatus.Differences);
        result.Snapshot.Differences.Should().ContainSingle(x =>
            x.Category == AchOperationalReconciliationDifferenceCategory.ReceivedApplicationInvariant
            && x.InternalValue == 3m
            && x.ExternalValue == 2m
            && x.Delta == 1m);
    }

    [Fact]
    public async Task Reconcile_ShouldKeepParticipantAndOperatorReturnsSeparate_AndExcludeOperatorReturnsFromPlanillaComparison()
    {
        await using var context = BuildInMemoryContext();
        SeedCoreScenario(context);

        var result = await new AchOperationalReconciliationService(context).ReconcileAsync(Request(MatchingEvidence()));

        result.Snapshot.ParticipantReturnCount.Should().Be(1);
        result.Snapshot.OperatorReturnCount.Should().Be(1);
        result.Snapshot.Differences.Should().NotContain(x =>
            x.Category == AchOperationalReconciliationDifferenceCategory.ExternalSentCount
            || x.Category == AchOperationalReconciliationDifferenceCategory.ExternalSentAmount);
    }

    [Fact]
    public async Task Reconcile_SameSources_ShouldBeIdempotent_AndChangedSourcesShouldCreateAuditableRevision()
    {
        await using var context = BuildInMemoryContext();
        SeedCoreScenario(context);
        var service = new AchOperationalReconciliationService(context);

        var first = await service.ReconcileAsync(Request(MatchingEvidence()));
        var replay = await service.ReconcileAsync(Request(MatchingEvidence()));
        context.AchTransactions.Single(x => x.Id == 1).Amount = 101m;
        await context.SaveChangesAsync();
        var recalculated = await service.ReconcileAsync(Request(MatchingEvidence() with { SentAmount = 101m, NetPosition = -41m }));

        replay.ReusedExistingRevision.Should().BeTrue();
        replay.Snapshot.Id.Should().Be(first.Snapshot.Id);
        recalculated.Snapshot.Revision.Should().Be(2);
        recalculated.Snapshot.Id.Should().NotBe(first.Snapshot.Id);
        (await context.AchOperationalReconciliationSnapshots.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Reconcile_ShouldIsolateClearingHousesAndCycles()
    {
        await using var context = BuildInMemoryContext();
        SeedCoreScenario(context);
        context.ClearingHouses.Add(House(20, "CENIT"));
        context.AchCycles.Add(Cycle("CENIT-20260807-C1", 20));
        context.AchCycles.Add(Cycle("ACH-20260807-C2", 10));
        await context.SaveChangesAsync();
        var service = new AchOperationalReconciliationService(context);

        await service.ReconcileAsync(Request());
        await service.ReconcileAsync(new(10, OperationalDate, "ACH-20260807-C2"));
        await service.ReconcileAsync(new(20, OperationalDate, "CENIT-20260807-C1"));

        (await context.AchOperationalReconciliationSnapshots.Select(x => new { x.ClearingHouseId, x.AchCycleId }).ToListAsync())
            .Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Reconcile_ConcurrentIndependentContexts_ShouldPersistOneRevision()
    {
        var databaseName = $"reconciliation-{Guid.NewGuid():N}";
        var connectionString = $"Data Source=file:{databaseName}?mode=memory&cache=shared";
        await using var keeper = new SqliteConnection(connectionString);
        await keeper.OpenAsync();
        await using (var setup = BuildSqliteContext(connectionString))
        {
            await setup.Database.EnsureCreatedAsync();
            setup.ClearingHouseConfigs.Add(new ClearingHouseConfig { Id = 1, ClearingHouseId = 10 });
            setup.ClearingHouses.Add(House(10, "ACH"));
            setup.AchCycles.Add(Cycle("ACH-20260807-C1", 10));
            setup.FinancialInstitutions.Add(Institution(1, true));
            await setup.SaveChangesAsync();
        }

        async Task<AchOperationalReconciliationResult> ExecuteAsync()
        {
            await using var context = BuildSqliteContext(connectionString);
            return await new AchOperationalReconciliationService(context).ReconcileAsync(Request(ZeroEvidence()));
        }

        var results = await Task.WhenAll(ExecuteAsync(), ExecuteAsync());
        await using var assertionContext = BuildSqliteContext(connectionString);

        results.Select(x => x.Snapshot.Id).Distinct().Should().ContainSingle();
        (await assertionContext.AchOperationalReconciliationSnapshots.CountAsync()).Should().Be(1);
        (await assertionContext.AchOperationalReconciliationDifferences.CountAsync()).Should().Be(0);
    }

    [Fact]
    public void ReconciliationFoundation_ShouldNotDependOnLedgerPostingOrSoap()
    {
        var parameterNames = typeof(AchOperationalReconciliationService).GetConstructors().Single()
            .GetParameters().Select(x => x.ParameterType.Name).ToList();

        parameterNames.Should().NotContain(x => x.Contains("Soap", StringComparison.OrdinalIgnoreCase));
        parameterNames.Should().NotContain(x => x.Contains("Ledger", StringComparison.OrdinalIgnoreCase));
        parameterNames.Should().NotContain(x => x.Contains("Posting", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void PersistenceModel_ShouldDeclareCanonicalUniquenessAndOptimisticConcurrency()
    {
        using var context = BuildInMemoryContext();
        var entity = context.Model.FindEntityType(typeof(AchOperationalReconciliationSnapshot))!;

        entity.GetIndexes().Should().Contain(x => x.IsUnique && x.Properties.Select(p => p.Name).SequenceEqual(new[]
        {
            nameof(AchOperationalReconciliationSnapshot.ClearingHouseId),
            nameof(AchOperationalReconciliationSnapshot.OperationalDate),
            nameof(AchOperationalReconciliationSnapshot.AchCycleId),
            nameof(AchOperationalReconciliationSnapshot.SourceFingerprint)
        }));
        entity.FindProperty(nameof(AchOperationalReconciliationSnapshot.Version))!.IsConcurrencyToken.Should().BeTrue();
    }

    [FinancialIntegrityFact(FinancialPersistenceMigrationTests.PersistenceProvider.SqlServer)]
    [Trait("Category", "FinancialIntegrity")]
    public Task Reconcile_ShouldPersistSameFoundation_OnSqlServer()
        => AssertRealProviderAsync(FinancialPersistenceMigrationTests.PersistenceProvider.SqlServer);

    [FinancialIntegrityFact(FinancialPersistenceMigrationTests.PersistenceProvider.PostgreSql)]
    [Trait("Category", "FinancialIntegrity")]
    public Task Reconcile_ShouldPersistSameFoundation_OnPostgreSql()
        => AssertRealProviderAsync(FinancialPersistenceMigrationTests.PersistenceProvider.PostgreSql);

    private static async Task AssertRealProviderAsync(FinancialPersistenceMigrationTests.PersistenceProvider provider)
    {
        var variable = FinancialIntegrityTestConfiguration.VariableName(provider);
        var baseConnectionString = Environment.GetEnvironmentVariable(variable)
            ?? throw new InvalidOperationException(FinancialIntegrityTestConfiguration.MissingConnectionMessage(provider));
        var databaseName = provider == FinancialPersistenceMigrationTests.PersistenceProvider.SqlServer
            ? $"ach_reconciliation_{Guid.NewGuid():N}"
            : null;
        var schemaName = provider == FinancialPersistenceMigrationTests.PersistenceProvider.PostgreSql
            ? $"ach_reconciliation_{Guid.NewGuid():N}"
            : null;
        string isolatedConnectionString;

        if (provider == FinancialPersistenceMigrationTests.PersistenceProvider.SqlServer)
        {
            var admin = new SqlConnectionStringBuilder(baseConnectionString) { InitialCatalog = "master" };
            await using var connection = new SqlConnection(admin.ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE DATABASE [{databaseName}]";
            await command.ExecuteNonQueryAsync();
            admin.InitialCatalog = databaseName;
            isolatedConnectionString = admin.ConnectionString;
        }
        else
        {
            var builder = new NpgsqlConnectionStringBuilder(baseConnectionString);
            await using var connection = new NpgsqlConnection(builder.ConnectionString);
            await connection.OpenAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = $"CREATE SCHEMA \"{schemaName}\"";
            await command.ExecuteNonQueryAsync();
            builder.SearchPath = schemaName;
            isolatedConnectionString = builder.ConnectionString;
        }

        try
        {
            var options = new DbContextOptionsBuilder<AchDbContext>();
            if (provider == FinancialPersistenceMigrationTests.PersistenceProvider.SqlServer)
            {
                options.UseSqlServer(isolatedConnectionString, sql => sql.MigrationsAssembly("Cfa.ACHInterbank.Persistence.Migrations.SqlServer"));
            }
            else
            {
                options.UseNpgsql(isolatedConnectionString);
            }

            await using var context = new AchDbContext(options.Options);
            await context.Database.MigrateAsync();
            var config = new ClearingHouseConfig { ClearingHouseId = 0 };
            context.ClearingHouseConfigs.Add(config);
            await context.SaveChangesAsync();
            var house = House(0, "ACH");
            house.ClearingHouseId = config.Id;
            house.ClearingHouseConfig = config;
            context.ClearingHouses.Add(house);
            context.FinancialInstitutions.Add(Institution(0, true));
            await context.SaveChangesAsync();
            var cycle = Cycle("ACH-20260807-C1", house.Id);
            cycle.ClearingHouse = house;
            context.AchCycles.Add(cycle);
            await context.SaveChangesAsync();

            var result = await new AchOperationalReconciliationService(context).ReconcileAsync(
                new(house.Id, OperationalDate, cycle.Id, ZeroEvidence(), "provider-test"));

            result.Snapshot.Status.Should().Be(AchOperationalReconciliationStatus.Balanced);
            (await context.AchOperationalReconciliationSnapshots.CountAsync()).Should().Be(1);
            (await context.AchOperationalReconciliationDifferences.CountAsync()).Should().Be(0);
        }
        finally
        {
            if (provider == FinancialPersistenceMigrationTests.PersistenceProvider.SqlServer)
            {
                var admin = new SqlConnectionStringBuilder(baseConnectionString) { InitialCatalog = "master" };
                await using var connection = new SqlConnection(admin.ConnectionString);
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = $"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{databaseName}]";
                await command.ExecuteNonQueryAsync();
            }
            else
            {
                await using var connection = new NpgsqlConnection(baseConnectionString);
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = $"DROP SCHEMA IF EXISTS \"{schemaName}\" CASCADE";
                await command.ExecuteNonQueryAsync();
            }
        }
    }

    private static AchOperationalReconciliationRequest Request(AchOperationalReconciliationExternalEvidence? evidence = null)
        => new(10, OperationalDate, "ACH-20260807-C1", evidence, "reconciliation-test");

    private static AchOperationalReconciliationExternalEvidence MatchingEvidence() => new()
    {
        EvidenceReference = "PLANILLA-ACH-C1",
        SentCount = 1,
        SentAmount = 100m,
        ReceivedCount = 2,
        ReceivedAmount = 100m,
        NetPosition = -40m,
        RecordedAt = new DateTimeOffset(2026, 8, 7, 15, 0, 0, TimeSpan.Zero)
    };

    private static AchOperationalReconciliationExternalEvidence ZeroEvidence() => new()
    {
        EvidenceReference = "PLANILLA-ZERO",
        SentCount = 0,
        SentAmount = 0m,
        ReceivedCount = 0,
        ReceivedAmount = 0m,
        NetPosition = 0m,
        RecordedAt = new DateTimeOffset(2026, 8, 7, 15, 0, 0, TimeSpan.Zero)
    };

    private static AchDbContext BuildInMemoryContext()
        => new(new DbContextOptionsBuilder<AchDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static AchDbContext BuildSqliteContext(string connectionString)
        => new(new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connectionString).Options);

    private static void SeedCoreScenario(AchDbContext context)
    {
        context.ClearingHouses.Add(House(10, "ACH"));
        context.AchCycles.Add(Cycle("ACH-20260807-C1", 10));
        context.FinancialInstitutions.AddRange(Institution(1, true), Institution(2, false));
        context.AchTransactions.AddRange(
            Transaction(1, 100m, AchTransactionDirection.Outgoing, AchTransferStateEnum.Certified, 1, 2),
            Transaction(2, 60m, AchTransactionDirection.Incoming, AchTransferStateEnum.AppliedTacitly, 2, 1),
            Transaction(3, 40m, AchTransactionDirection.Incoming, AchTransferStateEnum.ReturnedByEpr, 2, 1),
            Transaction(4, 10m, AchTransactionDirection.Outgoing, AchTransferStateEnum.ReturnedByOperator, 1, 2));
        context.SaveChanges();
    }

    private static ClearingHouse House(int id, string code) => new()
    {
        Id = id,
        Name = code,
        Code = code,
        OriginCode = code,
        ClearingHouseId = 1
    };

    private static AchCycle Cycle(string id, int clearingHouseId) => new()
    {
        Id = id,
        CycleName = id,
        ProcessingDate = OperationalDate.ToDateTime(TimeOnly.MinValue),
        ClearingHouseId = clearingHouseId
    };

    private static FinancialInstitution Institution(int id, bool isDefault)
    {
        var institution = new FinancialInstitution
        {
            Id = id,
            Name = $"Institution {id}",
            IsDefaultSource = isDefault,
            RoutingNumber = id.ToString("0000000"),
            TransitCode = "1"
        };
        institution.CalculateCheckDigit();
        return institution;
    }

    private static AchTransaction Transaction(
        int id,
        decimal amount,
        AchTransactionDirection direction,
        AchTransferStateEnum state,
        int sourceInstitutionId,
        int destinationInstitutionId)
        => new()
        {
            Id = id,
            Amount = amount,
            TransactionExternalId = $"TX-{id}",
            Reference = $"REF-{id}",
            Type = TransactionTypeEnum.Credit,
            Direction = direction,
            State = state,
            SourceInstitutionId = sourceInstitutionId,
            DestinationInstitutionId = destinationInstitutionId,
            AchCycleId = "ACH-20260807-C1",
            SourceAccountNumber = "masked-source",
            DestinationAccountNumber = "masked-destination"
        };
}
