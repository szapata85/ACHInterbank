using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Claims;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class ClaimCaseDomainPersistenceTests
{
    [Fact]
    public void CreateDev14_ShouldOpenCaseAndKeepExplicitParticipantsAndCause()
    {
        var (clearingHouse, requesting, responding) = BuildParties();
        var filedAt = new DateTimeOffset(2026, 9, 30, 15, 0, 0, TimeSpan.Zero);
        var deadlineAt = filedAt.AddDays(90);

        var claimCase = ClaimCase.Create(
            clearingHouse,
            ClaimCauseCode.DEV14,
            requesting,
            responding,
            filedAt,
            externalReference: null,
            creationIdempotencyKey: "claims-create-001",
            deadlineAt: deadlineAt);

        Assert.Equal(ClaimCauseCode.DEV14, claimCase.CauseCode);
        Assert.Equal(ClaimCaseStatus.Open, claimCase.Status);
        Assert.Equal(requesting.Id, claimCase.RequestingParticipantId);
        Assert.Equal(responding.Id, claimCase.RespondingParticipantId);
        Assert.Equal(filedAt, claimCase.FiledAt);
        Assert.Equal(deadlineAt, claimCase.DeadlineAt);
        Assert.Equal("claims-create-001", claimCase.CreationIdempotencyKey);
        Assert.Empty(claimCase.Transactions);
        var opened = Assert.Single(claimCase.Events);
        Assert.Equal(ClaimCaseEventType.Opened, opened.EventType);
        Assert.Equal(filedAt, opened.OccurredAt);
    }

    [Fact]
    public void CreateDev14_ShouldRejectNonAchColombiaClearingHouse()
    {
        var cenit = new ClearingHouse
        {
            Id = 2,
            Name = "CENIT",
            Code = "CENIT",
            OriginCode = "CENIT",
            ClearingHouseId = 1
        };
        var (_, requesting, responding) = BuildParties();

        var exception = Assert.Throws<ArgumentException>(() => ClaimCase.Create(
            cenit,
            ClaimCauseCode.DEV14,
            requesting,
            responding,
            DateTimeOffset.UtcNow));

        Assert.Contains("ACH Colombia", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Create_ShouldRejectAmbiguousParticipantRoles()
    {
        var clearingHouse = BuildClearingHouse();
        var participant = new FinancialInstitution { Id = 10, Name = "Entidad", RoutingNumber = "10000000", TransitCode = "001" };

        var act = () => ClaimCase.Create(
            clearingHouse,
            ClaimCauseCode.DEV14,
            participant,
            participant,
            DateTimeOffset.UtcNow);

        var exception = Assert.Throws<ArgumentException>(act);
        Assert.Contains("deben ser distintos", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddTransaction_Dev14_ShouldAcceptAchColombiaDebitWithCoherentParticipants()
    {
        var (clearingHouse, requesting, responding) = BuildParties();
        var cycle = BuildAchColCycle(clearingHouse);
        var claimCase = ClaimCase.Create(clearingHouse, ClaimCauseCode.DEV14, requesting, responding, DateTimeOffset.UtcNow);
        var transaction = BuildTransaction(501, "TRACE00000000001", cycle, requesting, responding);

        var association = claimCase.AddTransaction(transaction);

        Assert.Equal(transaction.Id, association.AchTransactionId);
        Assert.Equal(transaction, association.AchTransaction);
    }

    [Fact]
    public void AddTransaction_Dev14_ShouldRejectNonDebitTransaction()
    {
        var (clearingHouse, requesting, responding) = BuildParties();
        var cycle = BuildAchColCycle(clearingHouse);
        var claimCase = ClaimCase.Create(clearingHouse, ClaimCauseCode.DEV14, requesting, responding, DateTimeOffset.UtcNow);
        var transaction = BuildTransaction(501, "TRACE00000000001", cycle, requesting, responding, TransactionTypeEnum.Credit);

        var exception = Assert.Throws<ArgumentException>(() => claimCase.AddTransaction(transaction));

        Assert.Contains("débito", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddTransaction_Dev14_ShouldRejectTransactionFromDifferentClearingHouse()
    {
        var (clearingHouse, requesting, responding) = BuildParties();
        var otherCycle = new AchCycle { Id = "CENIT-01", ClearingHouseId = 2 };
        var claimCase = ClaimCase.Create(clearingHouse, ClaimCauseCode.DEV14, requesting, responding, DateTimeOffset.UtcNow);
        var transaction = BuildTransaction(501, "TRACE00000000001", otherCycle, requesting, responding);

        var exception = Assert.Throws<ArgumentException>(() => claimCase.AddTransaction(transaction));

        Assert.Contains("misma cámara", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddTransaction_Dev14_ShouldRejectIncoherentParticipantRoles()
    {
        var (clearingHouse, requesting, responding) = BuildParties();
        var cycle = BuildAchColCycle(clearingHouse);
        var claimCase = ClaimCase.Create(clearingHouse, ClaimCauseCode.DEV14, requesting, responding, DateTimeOffset.UtcNow);
        var transaction = BuildTransaction(501, "TRACE00000000001", cycle, requesting, responding);
        transaction.SourceInstitutionId = requesting.Id;
        transaction.DestinationInstitutionId = responding.Id;

        var exception = Assert.Throws<ArgumentException>(() => claimCase.AddTransaction(transaction));

        Assert.Contains("solicitante", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AddTransaction_Dev14_ShouldRequireCycleEvidenceToValidateClearingHouse()
    {
        var (clearingHouse, requesting, responding) = BuildParties();
        var claimCase = ClaimCase.Create(clearingHouse, ClaimCauseCode.DEV14, requesting, responding, DateTimeOffset.UtcNow);
        var transaction = new AchTransaction
        {
            Id = 501,
            Type = TransactionTypeEnum.Debit,
            SourceInstitutionId = responding.Id,
            DestinationInstitutionId = requesting.Id,
            AchCycleId = "ACHCOL-UNKNOWN"
        };

        var exception = Assert.Throws<ArgumentException>(() => claimCase.AddTransaction(transaction));

        Assert.Contains("AchCycle", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AddTransaction_ShouldSupportOneOrManyTransactions()
    {
        var (clearingHouse, requesting, responding) = BuildParties();
        var cycle = BuildAchColCycle(clearingHouse);
        var claimCase = ClaimCase.Create(clearingHouse, ClaimCauseCode.DEV14, requesting, responding, DateTimeOffset.UtcNow);
        var transaction1 = BuildTransaction(501, "TRACE00000000001", cycle, requesting, responding);
        var transaction2 = BuildTransaction(502, "TRACE00000000002", cycle, requesting, responding);

        claimCase.AddTransaction(transaction1);
        claimCase.AddTransaction(transaction2);

        Assert.Equal(2, claimCase.Transactions.Count);
        Assert.Contains(claimCase.Transactions, x => x.AchTransactionId == transaction1.Id);
        Assert.Contains(claimCase.Transactions, x => x.AchTransactionId == transaction2.Id);
    }

    [Fact]
    public void AddTransaction_ShouldRejectDuplicateInsideSameCase()
    {
        var (clearingHouse, requesting, responding) = BuildParties();
        var cycle = BuildAchColCycle(clearingHouse);
        var claimCase = ClaimCase.Create(clearingHouse, ClaimCauseCode.DEV14, requesting, responding, DateTimeOffset.UtcNow);
        var transaction = BuildTransaction(501, "TRACE00000000001", cycle, requesting, responding);

        claimCase.AddTransaction(transaction);

        var exception = Assert.Throws<InvalidOperationException>(() => claimCase.AddTransaction(transaction));
        Assert.Contains("ya está asociada", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RecordProgress_ShouldSupportMultipleAdvances()
    {
        var (claimCase, filedAt) = BuildOpenCaseWithDeadline();

        claimCase.RecordProgress(filedAt.AddDays(1), "Primer avance", "operador", "progress-1");
        claimCase.RecordProgress(filedAt.AddDays(16), "Segundo avance", "operador", "progress-2");

        Assert.Equal(ClaimCaseStatus.Open, claimCase.Status);
        Assert.Equal(2, claimCase.Events.Count(x => x.EventType == ClaimCaseEventType.Progress));
    }

    [Fact]
    public void CloseResolved_ShouldPersistMinimalResolutionAndPreventFurtherProgress()
    {
        var (claimCase, filedAt) = BuildOpenCaseWithDeadline();
        var closedAt = filedAt.AddDays(20);

        claimCase.CloseResolved(closedAt, "Solicitud atendida", "operador", "resolved-1");

        Assert.Equal(ClaimCaseStatus.Resolved, claimCase.Status);
        Assert.Equal(closedAt, claimCase.ClosedAt);
        Assert.Equal("Solicitud atendida", claimCase.ResolutionNotes);
        Assert.Throws<InvalidOperationException>(() => claimCase.RecordProgress(closedAt.AddMinutes(1), "No permitido"));
    }

    [Fact]
    public void EventIdempotency_ShouldBeScopedToCaseAndNotPartiallyCloseOnDuplicate()
    {
        var (claimCase, filedAt) = BuildOpenCaseWithDeadline();
        claimCase.RecordProgress(filedAt.AddDays(1), "Avance", idempotencyKey: "same-event");

        Assert.Throws<InvalidOperationException>(() =>
            claimCase.CloseResolved(filedAt.AddDays(2), "Resolución", idempotencyKey: "same-event"));

        Assert.Equal(ClaimCaseStatus.Open, claimCase.Status);
        Assert.Null(claimCase.ClosedAt);
    }

    [Fact]
    public void CloseExpired_ShouldRequireDeadlineAndNotAllowEarlyExpiration()
    {
        var (clearingHouse, requesting, responding) = BuildParties();
        var filedAt = new DateTimeOffset(2026, 9, 30, 15, 0, 0, TimeSpan.Zero);
        var withoutDeadline = ClaimCase.Create(clearingHouse, ClaimCauseCode.DEV14, requesting, responding, filedAt);

        Assert.Throws<InvalidOperationException>(() => withoutDeadline.CloseExpired(filedAt.AddDays(90)));

        var deadlineAt = filedAt.AddDays(90);
        var claimCase = ClaimCase.Create(clearingHouse, ClaimCauseCode.DEV14, requesting, responding, filedAt, deadlineAt: deadlineAt);
        Assert.Throws<InvalidOperationException>(() => claimCase.CloseExpired(deadlineAt.AddMinutes(-1)));

        claimCase.CloseExpired(deadlineAt, "Sin solución dentro del plazo", "sistema", "expired-1");
        Assert.Equal(ClaimCaseStatus.Expired, claimCase.Status);
        Assert.Equal(deadlineAt, claimCase.ClosedAt);
        Assert.Equal(ClaimCaseEventType.Expired, claimCase.Events.OrderBy(x => x.OccurredAt).Last().EventType);
    }

    [Fact]
    public void Dev14Milestones_ShouldRemainBusinessDayRulesNotCaseStates()
    {
        var initial = Dev14ClaimMilestonePolicy.GetRule(ClaimMilestone.InitialProgress);
        var final = Dev14ClaimMilestonePolicy.GetRule(ClaimMilestone.FinalClosure);

        Assert.Equal((1, 2), (initial.StartBusinessDay, initial.EndBusinessDay));
        Assert.True(initial.IsSatisfiedBy(2, ClaimCaseEventType.Progress));
        Assert.False(initial.IsSatisfiedBy(3, ClaimCaseEventType.Progress));
        Assert.Equal((60, 60), (final.StartBusinessDay, final.EndBusinessDay));
        Assert.True(final.IsSatisfiedBy(60, ClaimCaseEventType.Resolved));
        Assert.DoesNotContain("Progress", Enum.GetNames<ClaimCaseStatus>());
    }

    [Fact]
    public async Task ClaimCase_ShouldPersistAndReloadLifecycleTransactionsParticipantsAndOrderedHistory()
    {
        var databaseName = $"claims-{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(databaseName)
            .Options;

        var (clearingHouse, requesting, responding) = BuildParties();
        var cycle = BuildAchColCycle(clearingHouse);
        var transaction1 = BuildTransaction(501, "TRACE00000000001", cycle, requesting, responding);
        var transaction2 = BuildTransaction(502, "TRACE00000000002", cycle, requesting, responding);
        var filedAt = new DateTimeOffset(2026, 9, 30, 15, 0, 0, TimeSpan.Zero);
        var deadlineAt = filedAt.AddDays(90);

        await using (var writeContext = new AchDbContext(options))
        {
            writeContext.AddRange(clearingHouse, requesting, responding, cycle, transaction1, transaction2);

            var claimCase = ClaimCase.Create(
                clearingHouse,
                ClaimCauseCode.DEV14,
                requesting,
                responding,
                filedAt,
                externalReference: "future-external-ref",
                creationIdempotencyKey: "claims-create-002",
                deadlineAt: deadlineAt);
            claimCase.AddTransaction(transaction1);
            claimCase.AddTransaction(transaction2);
            claimCase.RecordProgress(filedAt.AddDays(16), "Avance 2", "operador", "progress-2");
            claimCase.RecordProgress(filedAt.AddDays(1), "Avance 1", "operador", "progress-1");
            claimCase.CloseResolved(filedAt.AddDays(30), "Resuelto", "operador", "resolved-2");

            writeContext.ClaimCases.Add(claimCase);
            await writeContext.SaveChangesAsync();

            Assert.NotEqual(default, claimCase.CreatedAt);
            Assert.NotEqual(default, claimCase.UpdatedAt);
        }

        await using (var readContext = new AchDbContext(options))
        {
            var persisted = await readContext.ClaimCases
                .Include(x => x.RequestingParticipant)
                .Include(x => x.RespondingParticipant)
                .Include(x => x.Transactions)
                    .ThenInclude(x => x.AchTransaction)
                .Include(x => x.Events)
                .SingleAsync();

            Assert.Equal(ClaimCauseCode.DEV14, persisted.CauseCode);
            Assert.Equal(ClaimCaseStatus.Resolved, persisted.Status);
            Assert.Equal(deadlineAt, persisted.DeadlineAt);
            Assert.Equal(10, persisted.RequestingParticipantId);
            Assert.Equal(20, persisted.RespondingParticipantId);
            Assert.Equal("future-external-ref", persisted.ExternalReference);
            Assert.Equal(2, persisted.Transactions.Count);
            Assert.Equal(
                new[] { ClaimCaseEventType.Opened, ClaimCaseEventType.Progress, ClaimCaseEventType.Progress, ClaimCaseEventType.Resolved },
                persisted.Events.OrderBy(x => x.OccurredAt).Select(x => x.EventType).ToArray());
        }
    }

    [Fact]
    public void ClaimDeadline_ShouldBeIndependentFromTransactionReturnSla()
    {
        var (clearingHouse, requesting, responding) = BuildParties();
        var cycle = BuildAchColCycle(clearingHouse);
        var filedAt = new DateTimeOffset(2026, 9, 30, 15, 0, 0, TimeSpan.Zero);
        var claimDeadline = filedAt.AddDays(90);
        var transaction = BuildTransaction(501, "TRACE00000000001", cycle, requesting, responding);
        transaction.SlaDeadlineAtUtc = filedAt.UtcDateTime.AddHours(2);
        var claimCase = ClaimCase.Create(clearingHouse, ClaimCauseCode.DEV14, requesting, responding, filedAt, deadlineAt: claimDeadline);

        claimCase.AddTransaction(transaction);

        Assert.Equal(claimDeadline, claimCase.DeadlineAt);
        Assert.NotEqual(transaction.SlaDeadlineAtUtc, claimCase.DeadlineAt?.UtcDateTime);
    }

    [Fact]
    public async Task RelationalModel_ShouldRejectDuplicateCreationIdempotencyKey()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF;");

        const string insert = """
            INSERT INTO ClaimCases
                (ClearingHouseId, CauseCode, RequestingParticipantId, RespondingParticipantId, FiledAt, Status, CreationIdempotencyKey, CreatedAt, UpdatedAt)
            VALUES
                (1, 'DEV14', 10, 20, '2026-09-30T15:00:00+00:00', 'Open', 'claims-create-unique', '2026-09-30T15:00:00+00:00', '2026-09-30T15:00:00+00:00');
            """;

        await context.Database.ExecuteSqlRawAsync(insert);
        await Assert.ThrowsAsync<SqliteException>(() => context.Database.ExecuteSqlRawAsync(insert));
    }

    [Fact]
    public async Task RelationalModel_ShouldRejectDuplicateTransactionAssociation()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF;");

        await context.Database.ExecuteSqlRawAsync("""
            INSERT INTO ClaimTransactions
                (ClaimCaseId, AchTransactionId, CreatedAt, UpdatedAt)
            VALUES
                (1, 501, '2026-09-30T15:00:00+00:00', '2026-09-30T15:00:00+00:00');
            """);

        var duplicate = () => context.Database.ExecuteSqlRawAsync("""
            INSERT INTO ClaimTransactions
                (ClaimCaseId, AchTransactionId, CreatedAt, UpdatedAt)
            VALUES
                (1, 501, '2026-09-30T15:01:00+00:00', '2026-09-30T15:01:00+00:00');
            """);

        await Assert.ThrowsAsync<SqliteException>(duplicate);
    }

    [Fact]
    public async Task RelationalModel_ShouldRejectDuplicateEventIdempotencyKeyWithinCase()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        await context.Database.ExecuteSqlRawAsync("PRAGMA foreign_keys = OFF;");

        const string insert = """
            INSERT INTO ClaimCaseEvents
                (ClaimCaseId, EventType, OccurredAt, IdempotencyKey, CreatedAt, UpdatedAt)
            VALUES
                (1, 'Progress', '2026-10-01T15:00:00+00:00', 'event-key', '2026-10-01T15:00:00+00:00', '2026-10-01T15:00:00+00:00');
            """;

        await context.Database.ExecuteSqlRawAsync(insert);
        await Assert.ThrowsAsync<SqliteException>(() => context.Database.ExecuteSqlRawAsync(insert));
    }

    [Fact]
    public void ClaimsModel_ShouldExposeEquivalentRelationalConstraints_ForSqlServerAndPostgreSql()
    {
        using var sqlServer = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlServer("Server=localhost;Database=claims_model;User Id=model;Password=model;TrustServerCertificate=True",
                sql => sql.MigrationsAssembly("Cfa.ACHInterbank.Persistence.Migrations.SqlServer"))
            .Options);
        using var postgreSql = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
            .UseNpgsql("Host=localhost;Database=claims_model;Username=model;Password=model")
            .Options);

        AssertClaimsModel(
            sqlServer.Model,
            expectedCaseIdempotencyFilter: "[CreationIdempotencyKey] IS NOT NULL",
            expectedEventIdempotencyFilter: "[IdempotencyKey] IS NOT NULL");
        AssertClaimsModel(
            postgreSql.Model,
            expectedCaseIdempotencyFilter: "\"CreationIdempotencyKey\" IS NOT NULL",
            expectedEventIdempotencyFilter: "\"IdempotencyKey\" IS NOT NULL");
    }

    private static void AssertClaimsModel(
        IModel model,
        string expectedCaseIdempotencyFilter,
        string expectedEventIdempotencyFilter)
    {
        var claimType = model.FindEntityType(typeof(ClaimCase));
        Assert.NotNull(claimType);
        Assert.Equal(typeof(ClaimCauseCode), claimType!.FindProperty(nameof(ClaimCase.CauseCode))!.ClrType);
        Assert.Equal(typeof(ClaimCaseStatus), claimType.FindProperty(nameof(ClaimCase.Status))!.ClrType);
        Assert.DoesNotContain(claimType.GetForeignKeys(), fk => fk.PrincipalEntityType.ClrType == typeof(AchReturnCode));

        var idempotency = Assert.Single(claimType.GetIndexes(), index =>
            index.Properties.Select(x => x.Name).SequenceEqual([nameof(ClaimCase.CreationIdempotencyKey)]));
        Assert.True(idempotency.IsUnique);
        Assert.Equal(expectedCaseIdempotencyFilter, idempotency.GetFilter());
        Assert.All(claimType.GetForeignKeys(), fk => Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior));

        var eventType = model.FindEntityType(typeof(ClaimCaseEvent));
        Assert.NotNull(eventType);
        var eventIdempotency = Assert.Single(eventType!.GetIndexes(), index =>
            index.Properties.Select(x => x.Name).SequenceEqual([nameof(ClaimCaseEvent.ClaimCaseId), nameof(ClaimCaseEvent.IdempotencyKey)]));
        Assert.True(eventIdempotency.IsUnique);
        Assert.Equal(expectedEventIdempotencyFilter, eventIdempotency.GetFilter());
        Assert.All(eventType.GetForeignKeys(), fk => Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior));

        var linkType = model.FindEntityType(typeof(ClaimTransaction));
        Assert.NotNull(linkType);
        var associationIndex = Assert.Single(linkType!.GetIndexes(), index =>
            index.Properties.Select(x => x.Name).SequenceEqual([nameof(ClaimTransaction.ClaimCaseId), nameof(ClaimTransaction.AchTransactionId)]));
        Assert.True(associationIndex.IsUnique);
        Assert.All(linkType.GetForeignKeys(), fk => Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior));
    }

    private static (ClaimCase ClaimCase, DateTimeOffset FiledAt) BuildOpenCaseWithDeadline()
    {
        var (clearingHouse, requesting, responding) = BuildParties();
        var filedAt = new DateTimeOffset(2026, 9, 30, 15, 0, 0, TimeSpan.Zero);
        var claimCase = ClaimCase.Create(
            clearingHouse,
            ClaimCauseCode.DEV14,
            requesting,
            responding,
            filedAt,
            deadlineAt: filedAt.AddDays(90));
        return (claimCase, filedAt);
    }

    private static (ClearingHouse ClearingHouse, FinancialInstitution Requesting, FinancialInstitution Responding) BuildParties()
        => (
            BuildClearingHouse(),
            new FinancialInstitution { Id = 10, Name = "EPR", RoutingNumber = "10000000", TransitCode = "001" },
            new FinancialInstitution { Id = 20, Name = "EPO", RoutingNumber = "20000000", TransitCode = "002" });

    private static ClearingHouse BuildClearingHouse()
        => new()
        {
            Id = 1,
            Name = "ACH Colombia",
            Code = "ACHCOL",
            OriginCode = "ACH",
            ClearingHouseId = 1
        };

    private static AchCycle BuildAchColCycle(ClearingHouse clearingHouse)
        => new()
        {
            Id = "ACHCOL-20260930-01",
            CycleName = "Ciclo 1 - ACH Colombia",
            ClearingHouseId = clearingHouse.Id,
            ClearingHouse = clearingHouse,
            ProcessingDate = new DateTime(2026, 9, 30),
            CutoffTime = new TimeSpan(7, 0, 0),
            StartTime = new TimeSpan(5, 0, 0),
            EndTime = new TimeSpan(7, 0, 0),
            OutputReleaseTime = new TimeSpan(8, 0, 0)
        };

    private static AchTransaction BuildTransaction(
        int id,
        string traceNumber,
        AchCycle cycle,
        FinancialInstitution requesting,
        FinancialInstitution responding,
        TransactionTypeEnum type = TransactionTypeEnum.Debit)
        => new()
        {
            Id = id,
            Type = type,
            Reference = $"TX-{id}",
            TransactionExternalId = $"EXT-{id}",
            TraceNumber = traceNumber,
            SourceAccountNumber = "10001",
            DestinationAccountNumber = "20002",
            AchCycleId = cycle.Id,
            AchCycle = cycle,
            AchBatchId = 1,
            SourceInstitutionId = responding.Id,
            DestinationInstitutionId = requesting.Id,
            SlaDeadlineAtUtc = null
        };
}
