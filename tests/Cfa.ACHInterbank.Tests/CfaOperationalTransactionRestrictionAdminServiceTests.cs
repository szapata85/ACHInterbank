using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class CfaOperationalTransactionRestrictionAdminServiceTests
{
    [Fact]
    public async Task Crud_UsesVersionedCycleScope_PreservesAuditConcurrencyAndStatusLifecycle()
    {
        using var connection = OpenConnection();
        await using var context = CreateContext(connection);
        SeedHouse(context, 731, 1731, "ACHCOL");
        AddCycle(context, 731, 2701, "ACHCOL-HISTORICAL-V35", "Ciclo 5");
        await context.SaveChangesAsync();
        var sut = new CfaOperationalTransactionRestrictionAdminService(context);

        var created = await sut.CreateAsync(new CreateCfaOperationalTransactionRestrictionRequest(
            ClearingHouseCode: "achcol",
            ClearingHouseCycleConfigId: 2701,
            TransactionType: TransactionTypeEnum.Debit,
            IsPrenotification: false,
            MaxAmountPerTransaction: 25_000_000m,
            MaxAmountPerCycle: null,
            MaxTransactionsPerCycle: 500,
            AllowedAccountTypes: [AccountTypeEnum.Checking, AccountTypeEnum.Savings],
            EffectiveFrom: Utc(2026, 1, 1),
            EffectiveTo: null));

        Assert.Equal(731, created.ClearingHouseId);
        Assert.Equal("ACHCOL", created.ClearingHouseCode);
        Assert.Equal(2701, created.ClearingHouseCycleConfigId);
        Assert.Equal("ACHCOL-HISTORICAL-V35", created.PolicyVersion);
        Assert.Equal("Ciclo 5", created.CycleName);
        Assert.Equal(new[] { AccountTypeEnum.Checking, AccountTypeEnum.Savings }, created.AllowedAccountTypes);
        Assert.False(string.IsNullOrWhiteSpace(created.RowVersion));
        Assert.NotEqual(default, created.CreatedAt);
        Assert.NotEqual(default, created.UpdatedAt);

        var stored = await context.CfaOperationalTransactionRestrictions.SingleAsync(x => x.Id == created.Id);
        Assert.Null(stored.ClearingHouseId);
        Assert.Equal(2701, stored.ClearingHouseCycleConfigId);

        var detail = await sut.GetByIdAsync(created.Id);
        Assert.NotNull(detail);
        Assert.Equal(created.Id, detail!.Id);

        var updated = await sut.UpdateAsync(created.Id, new UpdateCfaOperationalTransactionRestrictionRequest(
            ClearingHouseCode: "ACHCOL",
            ClearingHouseCycleConfigId: 2701,
            TransactionType: TransactionTypeEnum.Debit,
            IsPrenotification: false,
            MaxAmountPerTransaction: 30_000_000m,
            MaxAmountPerCycle: 250_000_000m,
            MaxTransactionsPerCycle: 600,
            AllowedAccountTypes: [AccountTypeEnum.Savings, AccountTypeEnum.Checking, AccountTypeEnum.Checking],
            EffectiveFrom: Utc(2026, 1, 1),
            EffectiveTo: Utc(2026, 12, 31),
            IsActive: true,
            ExpectedRowVersion: created.RowVersion));

        Assert.NotNull(updated);
        Assert.Equal(30_000_000m, updated!.MaxAmountPerTransaction);
        Assert.Equal(250_000_000m, updated.MaxAmountPerCycle);
        Assert.Equal(600, updated.MaxTransactionsPerCycle);
        Assert.Equal(new[] { AccountTypeEnum.Checking, AccountTypeEnum.Savings }, updated.AllowedAccountTypes);
        Assert.NotEqual(created.RowVersion, updated.RowVersion);

        var deactivated = await sut.SetActiveAsync(
            created.Id,
            false,
            new ChangeCfaOperationalTransactionRestrictionStatusRequest(updated.RowVersion, Utc(2026, 10, 31)));
        Assert.NotNull(deactivated);
        Assert.False(deactivated!.IsActive);
        Assert.Equal(Utc(2026, 10, 31), deactivated.EffectiveTo);

        var activated = await sut.SetActiveAsync(
            created.Id,
            true,
            new ChangeCfaOperationalTransactionRestrictionStatusRequest(deactivated.RowVersion));
        Assert.NotNull(activated);
        Assert.True(activated!.IsActive);

        var list = await sut.GetAsync(new CfaOperationalTransactionRestrictionFilter(
            ClearingHouseCode: "ACHCOL",
            PolicyVersion: "ACHCOL-HISTORICAL-V35",
            ClearingHouseCycleConfigId: 2701,
            TransactionType: TransactionTypeEnum.Debit,
            IsPrenotification: false,
            IsActive: true,
            EffectiveAt: Utc(2026, 6, 1)));
        Assert.Single(list);
        Assert.Equal(created.Id, list[0].Id);

        var audit = await context.AuditLogs
            .Where(x => x.EntityName == nameof(CfaOperationalTransactionRestriction))
            .ToListAsync();
        Assert.Contains(audit, x => x.Action == "Added");
        Assert.Contains(audit, x => x.Action == "Modified");
    }

    [Fact]
    public async Task Filters_SupportClearingHousePolicyVersionCycleTransactionPrenotificationActiveAndEffectiveAt()
    {
        using var connection = OpenConnection();
        await using var context = CreateContext(connection);
        SeedHouse(context, 731, 1731, "ACHCOL");
        SeedHouse(context, 947, 1947, "CENIT");
        AddCycle(context, 731, 2701, "V35", "Ciclo 5");
        AddCycle(context, 731, 2702, "V36", "Ciclo 5");
        AddCycle(context, 947, 3701, "CENIT-V1", "Ciclo 2");
        await context.SaveChangesAsync();

        context.CfaOperationalTransactionRestrictions.AddRange(
            Restriction(cycleConfigId: 2701, type: TransactionTypeEnum.Debit, prenote: false, from: Utc(2026, 1, 1)),
            Restriction(cycleConfigId: 2702, type: TransactionTypeEnum.Debit, prenote: false, from: Utc(2027, 1, 1)),
            Restriction(cycleConfigId: 2701, type: TransactionTypeEnum.Credit, prenote: true, from: Utc(2026, 1, 1)),
            Restriction(cycleConfigId: 3701, type: TransactionTypeEnum.Debit, prenote: false, from: Utc(2026, 1, 1)),
            Restriction(clearingHouseId: 731, type: TransactionTypeEnum.Debit, prenote: false, from: Utc(2025, 1, 1), to: Utc(2025, 12, 31)),
            Restriction(cycleConfigId: 2701, type: TransactionTypeEnum.Debit, prenote: false, from: Utc(2026, 1, 1), active: false));
        await context.SaveChangesAsync();
        var sut = new CfaOperationalTransactionRestrictionAdminService(context);

        var result = await sut.GetAsync(new CfaOperationalTransactionRestrictionFilter(
            ClearingHouseCode: "ACHCOL",
            PolicyVersion: "V35",
            ClearingHouseCycleConfigId: 2701,
            TransactionType: TransactionTypeEnum.Debit,
            IsPrenotification: false,
            IsActive: true,
            EffectiveAt: Utc(2026, 6, 1)));

        Assert.Single(result);
        Assert.Equal("V35", result[0].PolicyVersion);
        Assert.Equal(2701, result[0].ClearingHouseCycleConfigId);
        Assert.Equal(TransactionTypeEnum.Debit, result[0].TransactionType);
        Assert.Equal(false, result[0].IsPrenotification);
        Assert.True(result[0].IsActive);
    }

    [Fact]
    public async Task InvalidEffectiveRange_IsRejectedOnCreateAndUpdate()
    {
        using var connection = OpenConnection();
        await using var context = CreateContext(connection);
        var sut = new CfaOperationalTransactionRestrictionAdminService(context);

        var createError = await Assert.ThrowsAsync<CfaOperationalTransactionRestrictionValidationException>(() =>
            sut.CreateAsync(new CreateCfaOperationalTransactionRestrictionRequest(
                null, null, null, null, 1m, null, null, null,
                Utc(2026, 2, 1), Utc(2026, 1, 31))));
        Assert.Contains("EffectiveTo", createError.Message, StringComparison.OrdinalIgnoreCase);

        var created = await sut.CreateAsync(new CreateCfaOperationalTransactionRestrictionRequest(
            null, null, null, null, 1m, null, null, null,
            Utc(2026, 1, 1), null));

        await Assert.ThrowsAsync<CfaOperationalTransactionRestrictionValidationException>(() =>
            sut.UpdateAsync(created.Id, new UpdateCfaOperationalTransactionRestrictionRequest(
                null, null, null, null, 1m, null, null, null,
                Utc(2026, 2, 1), Utc(2026, 1, 31), true, created.RowVersion)));
    }

    [Fact]
    public async Task Deactivate_RejectsEffectiveToEarlierThanEffectiveFrom()
    {
        using var connection = OpenConnection();
        await using var context = CreateContext(connection);
        var sut = new CfaOperationalTransactionRestrictionAdminService(context);
        var created = await sut.CreateAsync(new CreateCfaOperationalTransactionRestrictionRequest(
            null, null, TransactionTypeEnum.Credit, false,
            1m, null, null, null, Utc(2026, 2, 1), null));

        await Assert.ThrowsAsync<CfaOperationalTransactionRestrictionValidationException>(() =>
            sut.SetActiveAsync(
                created.Id,
                false,
                new ChangeCfaOperationalTransactionRestrictionStatusRequest(
                    created.RowVersion,
                    Utc(2026, 1, 31))));

        var current = await sut.GetByIdAsync(created.Id);
        Assert.NotNull(current);
        Assert.True(current!.IsActive);
        Assert.Null(current.EffectiveTo);
    }

    [Fact]
    public async Task CycleScope_RejectsIncompatibleClearingHouse()
    {
        using var connection = OpenConnection();
        await using var context = CreateContext(connection);
        SeedHouse(context, 731, 1731, "ACHCOL");
        SeedHouse(context, 947, 1947, "CENIT");
        AddCycle(context, 947, 3701, "CENIT-V1", "Ciclo 2");
        await context.SaveChangesAsync();
        var sut = new CfaOperationalTransactionRestrictionAdminService(context);

        var error = await Assert.ThrowsAsync<CfaOperationalTransactionRestrictionValidationException>(() =>
            sut.CreateAsync(new CreateCfaOperationalTransactionRestrictionRequest(
                "ACHCOL", 3701, TransactionTypeEnum.Debit, false,
                1m, null, null, null, Utc(2026, 1, 1), null)));

        Assert.Contains("no pertenece", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(context.ChangeTracker.Entries<CfaOperationalTransactionRestriction>()
            .Where(x => x.State == EntityState.Added));
    }

    [Fact]
    public async Task StaleRowVersion_ProducesConcurrencyConflictWithoutOverwritingCurrentData()
    {
        using var connection = OpenConnection();
        await using var context = CreateContext(connection);
        var sut = new CfaOperationalTransactionRestrictionAdminService(context);
        var created = await sut.CreateAsync(new CreateCfaOperationalTransactionRestrictionRequest(
            null, null, TransactionTypeEnum.Credit, false,
            10m, null, null, null, Utc(2026, 1, 1), null));

        var updated = await sut.UpdateAsync(created.Id, new UpdateCfaOperationalTransactionRestrictionRequest(
            null, null, TransactionTypeEnum.Credit, false,
            20m, null, null, null, Utc(2026, 1, 1), null, true, created.RowVersion));
        Assert.NotNull(updated);

        var error = await Assert.ThrowsAsync<CfaOperationalTransactionRestrictionConcurrencyException>(() =>
            sut.UpdateAsync(created.Id, new UpdateCfaOperationalTransactionRestrictionRequest(
                null, null, TransactionTypeEnum.Credit, false,
                99m, null, null, null, Utc(2026, 1, 1), null, true, created.RowVersion)));

        Assert.Equal(updated!.RowVersion, error.CurrentRowVersion);
        var current = await sut.GetByIdAsync(created.Id);
        Assert.Equal(20m, current!.MaxAmountPerTransaction);
    }

    [Fact]
    public async Task AllowedAccountTypes_PreservesNullInheritanceAndExplicitEmptyAllowlist()
    {
        using var connection = OpenConnection();
        await using var context = CreateContext(connection);
        var sut = new CfaOperationalTransactionRestrictionAdminService(context);

        var inherited = await sut.CreateAsync(new CreateCfaOperationalTransactionRestrictionRequest(
            null, null, null, null, 1m, null, null, null, Utc(2026, 1, 1), null));
        var denyAll = await sut.CreateAsync(new CreateCfaOperationalTransactionRestrictionRequest(
            null, null, null, null, 2m, null, null, [], Utc(2026, 2, 1), null));

        Assert.Null(inherited.AllowedAccountTypes);
        Assert.NotNull(denyAll.AllowedAccountTypes);
        Assert.Empty(denyAll.AllowedAccountTypes!);
    }

    [Fact]
    public async Task AdminCreatedAllowedAccountTypes_AreConsumedAsExplicitAllowlistVeto()
    {
        using var connection = OpenConnection();
        await using var context = CreateContext(connection);
        SeedHouse(context, 731, 1731, "ACHCOL");
        AddCycle(context, 731, 2701, "V35", "Ciclo 5");
        await context.SaveChangesAsync();
        var admin = new CfaOperationalTransactionRestrictionAdminService(context);

        await admin.CreateAsync(new CreateCfaOperationalTransactionRestrictionRequest(
            "ACHCOL", 2701, TransactionTypeEnum.Debit, false,
            null, null, null, [AccountTypeEnum.Checking], Utc(2026, 1, 1), null));

        var resolver = new CfaOperationalTransactionRestrictionResolver(context);
        var result = await resolver.ResolveAsync(new CfaOperationalTransactionRestrictionRequest(
            ClearingHouseId: 731,
            ClearingHouseCycleConfigId: 2701,
            TransactionType: TransactionTypeEnum.Debit,
            IsPrenotification: false,
            AccountType: AccountTypeEnum.Savings,
            EffectiveAt: Utc(2026, 6, 1)));

        Assert.True(result.IsOperationallyDenied);
        Assert.Equal(new[] { AccountTypeEnum.Checking }, result.AllowedAccountTypes);
    }

    [Fact]
    public async Task AdminOperations_NeverModifyRegulatoryCyclePolicy()
    {
        using var connection = OpenConnection();
        await using var context = CreateContext(connection);
        SeedHouse(context, 731, 1731, "ACHCOL");
        AddCycle(context, 731, 2701, "V35", "Ciclo 5");
        await context.SaveChangesAsync();
        var cycle = await context.ClearingHouseCycleConfigs.SingleAsync(x => x.Id == 2701);
        cycle.AllowsMonetaryDebit = false;
        cycle.AllowsMonetaryCredit = true;
        cycle.AllowsDebitPrenotification = false;
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var admin = new CfaOperationalTransactionRestrictionAdminService(context);
        var created = await admin.CreateAsync(new CreateCfaOperationalTransactionRestrictionRequest(
            "ACHCOL", 2701, TransactionTypeEnum.Debit, false,
            10m, null, null, [AccountTypeEnum.Checking], Utc(2026, 1, 1), null));
        var updated = await admin.UpdateAsync(created.Id, new UpdateCfaOperationalTransactionRestrictionRequest(
            "ACHCOL", 2701, TransactionTypeEnum.Debit, false,
            20m, null, null, [AccountTypeEnum.Checking], Utc(2026, 1, 1), null, true, created.RowVersion));
        Assert.NotNull(updated);
        await admin.SetActiveAsync(created.Id, false,
            new ChangeCfaOperationalTransactionRestrictionStatusRequest(updated!.RowVersion));

        var regulatory = await context.ClearingHouseCycleConfigs.AsNoTracking().SingleAsync(x => x.Id == 2701);
        Assert.Equal("V35", regulatory.PolicyVersion);
        Assert.False(regulatory.AllowsMonetaryDebit);
        Assert.True(regulatory.AllowsMonetaryCredit);
        Assert.False(regulatory.AllowsDebitPrenotification);
    }

    [Fact]
    public async Task PolicyVersionFilter_IsolatesSameNamedCycleAcrossVersions()
    {
        using var connection = OpenConnection();
        await using var context = CreateContext(connection);
        SeedHouse(context, 731, 1731, "ACHCOL");
        AddCycle(context, 731, 2701, "HISTORICAL", "Ciclo 5");
        AddCycle(context, 731, 2702, "FUTURE", "Ciclo 5");
        await context.SaveChangesAsync();
        context.CfaOperationalTransactionRestrictions.AddRange(
            Restriction(cycleConfigId: 2701, type: TransactionTypeEnum.Debit, from: Utc(2026, 1, 1)),
            Restriction(cycleConfigId: 2702, type: TransactionTypeEnum.Debit, from: Utc(2026, 1, 1)));
        await context.SaveChangesAsync();
        var sut = new CfaOperationalTransactionRestrictionAdminService(context);

        var historical = await sut.GetAsync(new CfaOperationalTransactionRestrictionFilter(
            ClearingHouseCode: "ACHCOL",
            PolicyVersion: "HISTORICAL"));
        var future = await sut.GetAsync(new CfaOperationalTransactionRestrictionFilter(
            ClearingHouseCode: "ACHCOL",
            PolicyVersion: "FUTURE"));

        Assert.Single(historical);
        Assert.Single(future);
        Assert.Equal(2701, historical[0].ClearingHouseCycleConfigId);
        Assert.Equal(2702, future[0].ClearingHouseCycleConfigId);
        Assert.NotEqual(historical[0].Id, future[0].Id);
    }

    [Fact]
    public async Task CameraResolution_UsesStableCodeWithArbitraryDatabaseIds()
    {
        using var connection = OpenConnection();
        await using var context = CreateContext(connection);
        SeedHouse(context, 731, 1731, "ACHCOL");
        await context.SaveChangesAsync();
        var sut = new CfaOperationalTransactionRestrictionAdminService(context);

        var created = await sut.CreateAsync(new CreateCfaOperationalTransactionRestrictionRequest(
            "ACHCOL", null, TransactionTypeEnum.Credit, false,
            50m, null, null, null, Utc(2026, 1, 1), null));

        var stored = await context.CfaOperationalTransactionRestrictions.SingleAsync(x => x.Id == created.Id);
        Assert.Equal(731, stored.ClearingHouseId);
        Assert.NotEqual(1, stored.ClearingHouseId);
        Assert.NotEqual(2, stored.ClearingHouseId);
    }

    private static CfaOperationalTransactionRestriction Restriction(
        int? clearingHouseId = null,
        int? cycleConfigId = null,
        TransactionTypeEnum? type = null,
        bool? prenote = null,
        DateTime? from = null,
        DateTime? to = null,
        bool active = true)
        => new()
        {
            ClearingHouseId = clearingHouseId,
            ClearingHouseCycleConfigId = cycleConfigId,
            TransactionType = type,
            IsPrenotification = prenote,
            MaxAmountPerTransaction = 1m,
            EffectiveFrom = from ?? Utc(2026, 1, 1),
            EffectiveTo = to,
            IsActive = active
        };

    private static void SeedHouse(AchDbContext context, int houseId, int configId, string code)
    {
        context.ClearingHouseConfigs.Add(new ClearingHouseConfig
        {
            Id = configId,
            ClearingHouseId = houseId,
            TimeZoneId = "America/Bogota",
            HolidayStrategy = "Colombian"
        });
        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = houseId,
            ClearingHouseId = configId,
            Code = code,
            Name = code,
            OriginCode = $"{houseId:000000000}",
            IsActive = true
        });
    }

    private static void AddCycle(
        AchDbContext context,
        int houseId,
        int id,
        string policyVersion,
        string cycleName)
        => context.ClearingHouseCycleConfigs.Add(new ClearingHouseCycleConfig
        {
            Id = id,
            ClearingHouseId = houseId,
            PolicyVersion = policyVersion,
            CycleName = cycleName,
            StartTime = TimeSpan.FromHours(8),
            EndTime = TimeSpan.FromHours(9),
            CutoffTime = TimeSpan.FromHours(9),
            OutputReleaseTime = TimeSpan.FromHours(9.5),
            EffectiveFrom = Utc(2026, 1, 1),
            IsActive = true
        });

    private static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        return connection;
    }

    private static AchDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;
        var context = new AchDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static DateTime Utc(int year, int month, int day)
        => new(year, month, day, 0, 0, 0, DateTimeKind.Utc);
}
