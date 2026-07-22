using System.Data.Common;
using Cfa.ACHInterbank.Application.ACH.Implementation.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;
using Cfa.ACHInterbank.Application.Configuration;
using Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Npgsql;

namespace Cfa.ACHInterbank.Tests;

public sealed class ClearingHouseMultiDbTests
{
    private const string RequiredVariable = "CLEARING_HOUSES_REQUIRE_DATABASES";

    [Fact]
    [Trait("Category", "ClearingHouseMultiDb")]
    [Trait("Provider", "SqlServer")]
    public Task AdministrationMigrationsCrudAndLengthGuard_RunAgainstSqlServer()
        => RunAgainstRealProviderAsync(DatabaseProvider.SqlServer);

    [Fact]
    [Trait("Category", "ClearingHouseMultiDb")]
    [Trait("Provider", "PostgreSql")]
    public Task AdministrationMigrationsCrudAndLengthGuard_RunAgainstPostgreSql()
        => RunAgainstRealProviderAsync(DatabaseProvider.PostgreSql);

    private static async Task RunAgainstRealProviderAsync(DatabaseProvider provider)
    {
        EnsureRequiredConfiguration(provider);
        await PositiveScenarioAsync(provider);
        await CanonicalCycleLinkScenarioAsync(provider);
        await LongCodeRejectionScenarioAsync(provider);
    }

    private static async Task CanonicalCycleLinkScenarioAsync(DatabaseProvider provider)
    {
        await using var fixture = await DatabaseFixture.CreateAsync(provider);
        await using var context = fixture.CreateContext();
        await context.Database.MigrateAsync();
        await new ClearingHouseConfigSeeder(context).SeedAsync();
        await new ClearingHouseSeeder(context).SeedAsync();
        var house = await context.ClearingHouses.SingleAsync(x => x.Code == "ACHCOL");
        var date = new DateTime(2026, 8, 10);
        var config = new ClearingHouseCycleConfig
        {
            ClearingHouseId = house.Id,
            CycleName = "Ciclo canónico multidb",
            StartTime = new TimeSpan(3, 17, 0),
            EndTime = new TimeSpan(4, 29, 0),
            CutoffTime = new TimeSpan(4, 11, 0),
            EffectiveFrom = DateTime.SpecifyKind(date.AddDays(-1), DateTimeKind.Utc),
            EffectiveTo = DateTime.SpecifyKind(date.AddDays(1), DateTimeKind.Utc),
            IsActive = true
        };
        context.ClearingHouseCycleConfigs.Add(config);
        await context.SaveChangesAsync();
        MapperBootstrapper.Configure(NullLoggerFactory.Instance);
        var cycleService = new AchCycleAppService(context, MapperBootstrapper.Instance);
        var request = new AchCycleRequest
        {
            ClearingHouseId = house.Id,
            ClearingHouseCycleConfigId = config.Id,
            CycleName = "Texto cliente no canónico",
            ProcessingDate = date,
            StartTime = config.StartTime,
            EndTime = config.EndTime,
            CutoffTime = config.CutoffTime
        };

        var created = await cycleService.CreateAsync(request);
        Assert.Equal(config.Id, created.ClearingHouseCycleConfigId);
        Assert.Equal(config.CycleName, created.CycleName);

        var legacyRequest = new AchCycleRequest
        {
            ClearingHouseId = house.Id,
            CycleName = "  CICLO CANÓNICO MULTIDB  ",
            ProcessingDate = date.AddDays(1),
            StartTime = config.StartTime,
            EndTime = config.EndTime,
            CutoffTime = config.CutoffTime
        };
        var legacyCreated = await cycleService.CreateAsync(legacyRequest);
        Assert.Equal(config.Id, legacyCreated.ClearingHouseCycleConfigId);
        Assert.Equal(config.CycleName, legacyCreated.CycleName);

        var historical = new AchCycle
        {
            Id = $"MDB-HIST-{provider}",
            ClearingHouseId = house.Id,
            CycleName = "Nombre histórico distinto",
            ProcessingDate = date,
            StartTime = config.StartTime,
            EndTime = config.EndTime,
            CutoffTime = config.CutoffTime
        };
        context.AchCycles.Add(historical);
        var batch = new AchBatch
        {
            AchCycleId = historical.Id,
            CompanyName = "PRUEBA",
            CompanyIdentification = "900000000",
            CompanyEntryDescription = "PRUEBA",
            CompanyEntryDescriptionId = 1,
            OriginOrOdfi = "00001283",
            EffectiveEntryDate = date,
            BatchSequenceNumber = 1
        };
        context.AchBatches.Add(batch);
        await context.SaveChangesAsync();
        var origin = await context.FinancialInstitutions.FirstOrDefaultAsync(x => !x.IsDefaultSource);
        if (origin is null)
        {
            origin = new FinancialInstitution
            {
                Name = "Origen multidb", RoutingNumber = "88001", TransitCode = "881",
                Status = FinancialInstitutionStatus.Active
            };
            origin.CalculateCheckDigit();
            context.FinancialInstitutions.Add(origin);
        }
        var destination = await context.FinancialInstitutions.FirstOrDefaultAsync(x => x.IsDefaultSource);
        if (destination is null)
        {
            destination = new FinancialInstitution
            {
                Name = "Destino CFA multidb", RoutingNumber = "88002", TransitCode = "882",
                IsDefaultSource = true, Status = FinancialInstitutionStatus.Active
            };
            destination.CalculateCheckDigit();
            context.FinancialInstitutions.Add(destination);
        }
        await context.SaveChangesAsync();
        var transaction = new AchTransaction
        {
            Reference = $"MDB-{provider}", TransactionExternalId = $"MDB-{provider}",
            Type = TransactionTypeEnum.Credit, Amount = 9876.54m, State = AchTransferStateEnum.Pending,
            AchCycleId = historical.Id, AchBatchId = batch.Id, CompanyEntryDescriptionId = 1,
            CompanyName = "PRUEBA", CompanyIdentification = "900000000", OriginatingDFI = "00001283",
            ReceivingDFI = "99999900", TraceNumber = "000012830009876",
            SourceInstitutionId = origin.Id, DestinationInstitutionId = destination.Id,
            SourceAccountNumber = "100", DestinationAccountNumber = "200", RecipientIdNumber = "900000001",
            EffectiveEntryDate = date, StateChangedAtUtc = DateTime.SpecifyKind(date, DateTimeKind.Utc)
        };
        context.AchTransactions.Add(transaction);
        await context.SaveChangesAsync();
        var amountBefore = transaction.Amount;

        var firstRepair = await cycleService.RepairConfigurationLinksAsync();
        Assert.True(firstRepair.Completed);
        Assert.Equal(1, firstRepair.RepairedCount);
        var secondRepair = await cycleService.RepairConfigurationLinksAsync();
        Assert.True(secondRepair.Completed);
        Assert.Equal(0, secondRepair.RepairedCount);

        var simulator = new NachaInboundSimulationService(context, Options.Create(new NachaInboundSimulatorOptions
        {
            Enabled = true, Mode = "UAT", AllowAutoImport = false, AllowExternalTransmission = false,
            RequireSyntheticData = true, OutputDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")),
            MaxEntriesPerSimulation = 10, AllowedClearingHouses = ["ACHCOL", "CENIT"]
        }));
        var available = await simulator.ListAvailableCyclesAsync(new AvailableInboundCycleQuery
        {
            ClearingHouseCode = "ACHCOL", ProcessingDate = DateOnly.FromDateTime(date),
            ScenarioType = NachaInboundSimulationType.IncomingCredit
        });
        Assert.Contains(available, x => x.CycleCode == historical.Id && x.TransactionCount == 1);
        var invalid = await simulator.PreviewAsync(new InboundSimulationEligibilityPreviewRequest
        {
            ClearingHouseCode = "ACHCOL",
            BusinessDate = DateOnly.FromDateTime(date), ScenarioType = NachaInboundSimulationType.IncomingCredit,
            OriginFinancialInstitutionId = origin.Id, CycleCode = "CICLO-MANIPULADO"
        });
        Assert.False(invalid.Eligible);
        Assert.Equal("CYCLE_NOT_AVAILABLE", invalid.FunctionalCode);

        historical.StartTime = historical.StartTime.Add(TimeSpan.FromMinutes(1));
        await context.SaveChangesAsync();
        Assert.DoesNotContain(await simulator.ListAvailableCyclesAsync(new AvailableInboundCycleQuery
        {
            ClearingHouseCode = "ACHCOL", ProcessingDate = DateOnly.FromDateTime(date),
            ScenarioType = NachaInboundSimulationType.IncomingCredit
        }), x => x.CycleCode == historical.Id);
        Assert.Equal(amountBefore, await context.AchTransactions.Where(x => x.Id == transaction.Id).Select(x => x.Amount).SingleAsync());

        context.ClearingHouseCycleConfigs.Add(new ClearingHouseCycleConfig
        {
            ClearingHouseId = house.Id,
            CycleName = " ciclo CANÓNICO multidb ",
            StartTime = config.StartTime,
            EndTime = config.EndTime,
            CutoffTime = config.CutoffTime,
            EffectiveFrom = config.EffectiveFrom,
            EffectiveTo = config.EffectiveTo,
            IsActive = true
        });
        var ambiguousCycle = new AchCycle
        {
            Id = $"MDB-AMB-{provider}",
            ClearingHouseId = house.Id,
            CycleName = "Nombre histórico ambiguo",
            ProcessingDate = date,
            StartTime = config.StartTime,
            EndTime = config.EndTime,
            CutoffTime = config.CutoffTime
        };
        context.AchCycles.Add(ambiguousCycle);
        await context.SaveChangesAsync();

        var ambiguousRepair = await cycleService.RepairConfigurationLinksAsync();
        Assert.False(ambiguousRepair.Completed);
        Assert.Equal(1, ambiguousRepair.AmbiguousCount);
        Assert.Null(await context.AchCycles.Where(x => x.Id == ambiguousCycle.Id)
            .Select(x => x.ClearingHouseCycleConfigId).SingleAsync());
        Assert.Equal(amountBefore, await context.AchTransactions.Where(x => x.Id == transaction.Id).Select(x => x.Amount).SingleAsync());
    }

    private static async Task PositiveScenarioAsync(DatabaseProvider provider)
    {
        await using var fixture = await DatabaseFixture.CreateAsync(provider);
        await using var context = fixture.CreateContext();
        var migrator = context.Database.GetService<IMigrator>();
        var migrations = context.Database.GetMigrations().ToList();
        var administrationMigration = migrations.Single(x => x.EndsWith("_ConfigurableClearingHouseAdministration", StringComparison.Ordinal));
        var previousMigration = migrations[migrations.IndexOf(administrationMigration) - 1];
        var latestMigration = migrations.Last();

        await migrator.MigrateAsync(previousMigration);
        await InsertKnownClearingHousesAsync(context, includeLongCode: false);
        await migrator.MigrateAsync(latestMigration);
        context.ChangeTracker.Clear();

        var known = await context.ClearingHouses.AsNoTracking()
            .Include(x => x.ClearingHouseConfig)
            .Where(x => x.Code == "ACHCOL" || x.Code == "CENIT")
            .OrderBy(x => x.Code)
            .ToListAsync();
        Assert.Equal(2, known.Count);
        Assert.All(known, x => Assert.True(x.CreatedAt > DateTimeOffset.Parse("2000-01-01T00:00:00Z")));
        Assert.All(known, x => Assert.True(x.UpdatedAt > DateTimeOffset.Parse("2000-01-01T00:00:00Z")));
        Assert.Equal(PaymentRailCodes.AchColombia, known.Single(x => x.Code == "ACHCOL").ClearingHouseConfig.PaymentRailCode);
        Assert.Equal(PaymentRailCodes.Cenit, known.Single(x => x.Code == "CENIT").ClearingHouseConfig.PaymentRailCode);

        var service = CreateService(context);
        var created = await service.CreateAsync(Request("NUEVARED", PaymentRailCodes.AchColombia));
        context.ClearingHouseCycleConfigs.Add(new ClearingHouseCycleConfig
        {
            ClearingHouseId = created.Id,
            CycleName = "Ciclo multimotor",
            IsActive = true,
            StartTime = TimeSpan.FromHours(8),
            EndTime = TimeSpan.FromHours(17),
            CutoffTime = TimeSpan.FromHours(16),
            EffectiveFrom = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(-1), DateTimeKind.Utc)
        });
        await context.SaveChangesAsync();

        var active = await service.ChangeStatusAsync(created.Id, true);
        Assert.True(active.IsActive);
        Assert.Contains(await service.GetOperationalAsync(), x => x.Id == created.Id);
        await Assert.ThrowsAsync<ClearingHouseConflictException>(() => service.CreateAsync(Request(" nuevared ", PaymentRailCodes.Cenit)));
        await Assert.ThrowsAsync<ClearingHouseConflictException>(
            () => service.UpdateAsync(created.Id, UpdateRequest(active, PaymentRailCodes.Cenit)));
        context.ChangeTracker.Clear();
        var afterConflict = await context.ClearingHouses.AsNoTracking()
            .Include(x => x.ClearingHouseConfig)
            .SingleAsync(x => x.Id == created.Id);
        Assert.Equal(PaymentRailCodes.AchColombia, afterConflict.ClearingHouseConfig.PaymentRailCode);
        Assert.Equal(active.UpdatedAt, afterConflict.UpdatedAt);
        Assert.True(afterConflict.IsActive);

        await service.ChangeStatusAsync(created.Id, false);
        var inactive = (await service.GetByIdAsync(created.Id))!;
        var changed = await service.UpdateAsync(created.Id, UpdateRequest(inactive, PaymentRailCodes.Cenit, includeExpectedUpdatedAt: false));
        Assert.False(changed.IsActive);
        Assert.Equal(PaymentRailCodes.Cenit, changed.PaymentRailCode);
        Assert.DoesNotContain(await service.GetOperationalAsync(), x => x.Id == created.Id);

        var configSeeder = new ClearingHouseConfigSeeder(context);
        var houseSeeder = new ClearingHouseSeeder(context);
        await configSeeder.SeedAsync();
        await houseSeeder.SeedAsync();
        await configSeeder.SeedAsync();
        await houseSeeder.SeedAsync();
        context.ChangeTracker.Clear();
        Assert.Equal(1, await context.ClearingHouses.CountAsync(x => x.Code == "NUEVARED"));
        Assert.Equal(PaymentRailCodes.Cenit,
            await context.ClearingHouses.Where(x => x.Code == "NUEVARED")
                .Select(x => x.ClearingHouseConfig.PaymentRailCode).SingleAsync());
        Assert.Equal(1, await context.ClearingHouseCycleConfigs.CountAsync(x => x.ClearingHouseId == created.Id));

        await migrator.MigrateAsync(previousMigration);
        context.ChangeTracker.Clear();
        Assert.Equal(3, await ScalarIntAsync(context, provider, "SELECT COUNT(*) FROM {0} WHERE {1} IN ('ACHCOL','CENIT','NUEVARED')"));

        await migrator.MigrateAsync(latestMigration);
        context.ChangeTracker.Clear();
        Assert.Equal(PaymentRailCodes.AchColombia,
            await context.ClearingHouses.Where(x => x.Code == "ACHCOL")
                .Select(x => x.ClearingHouseConfig.PaymentRailCode).SingleAsync());
        Assert.Equal(PaymentRailCodes.Cenit,
            await context.ClearingHouses.Where(x => x.Code == "CENIT")
                .Select(x => x.ClearingHouseConfig.PaymentRailCode).SingleAsync());
    }

    private static async Task LongCodeRejectionScenarioAsync(DatabaseProvider provider)
    {
        await using var fixture = await DatabaseFixture.CreateAsync(provider);
        string administrationMigration;
        string previousMigration;
        string latestMigration;

        await using (var context = fixture.CreateContext())
        {
            var migrations = context.Database.GetMigrations().ToList();
            administrationMigration = migrations.Single(x => x.EndsWith("_ConfigurableClearingHouseAdministration", StringComparison.Ordinal));
            previousMigration = migrations[migrations.IndexOf(administrationMigration) - 1];
            latestMigration = migrations.Last();
            await context.Database.GetService<IMigrator>().MigrateAsync(previousMigration);
            await InsertKnownClearingHousesAsync(context, includeLongCode: true);

            var exception = await Assert.ThrowsAnyAsync<Exception>(
                () => context.Database.GetService<IMigrator>().MigrateAsync(latestMigration));
            Assert.Contains("No es posible reducir ClearingHouses.Code a 20 caracteres", exception.ToString());
        }

        await using var verification = fixture.CreateContext();
        Assert.Equal(1, await ScalarIntAsync(verification, provider, "SELECT COUNT(*) FROM {0} WHERE {1} = 'CODIGO_SUPERIOR_A_VEINTE'"));
        Assert.Equal(50, await CodeMaximumLengthAsync(verification, provider));
        Assert.Equal(0, await MigrationAppliedAsync(verification, provider, administrationMigration));
        Assert.Equal(0, await ColumnExistsAsync(verification, provider, "ClearingHouses", "CreatedAt"));
    }

    private static ClearingHouseService CreateService(AchDbContext context)
    {
        IPaymentRailOperationalStrategy[] strategies =
        [
            new AchColombiaPaymentRailOperationalStrategy(),
            new CenitPaymentRailOperationalStrategy(),
            new UnknownPaymentRailOperationalStrategy()
        ];
        return new ClearingHouseService(context, strategies);
    }

    private static CreateClearingHouseRequest Request(string code, string? railCode) => new()
    {
        Code = code,
        Name = "Nueva Red de Pruebas",
        OriginCode = "900",
        TimeZoneId = "America/Bogota",
        HolidayStrategy = "Colombian",
        PaymentRailCode = railCode
    };

    private static UpdateClearingHouseRequest UpdateRequest(ClearingHouseDto current, string? railCode, bool includeExpectedUpdatedAt = true) => new()
    {
        Code = current.Code,
        Name = current.Name,
        OriginCode = current.OriginCode,
        TimeZoneId = current.TimeZoneId,
        HolidayStrategy = current.HolidayStrategy!,
        PaymentRailCode = railCode,
        RequiresNachaProfile = current.RequiresNachaProfile,
        NachaProfileId = current.NachaProfileId,
        ExpectedUpdatedAt = includeExpectedUpdatedAt ? current.UpdatedAt : null
    };

    private static async Task InsertKnownClearingHousesAsync(AchDbContext context, bool includeLongCode)
    {
        await InsertHouseAsync(context, "ACHCOL", "ACH Colombia", "000101006", 1001);
        await InsertHouseAsync(context, "CENIT", "CENIT", "011111111", 1002);
        if (includeLongCode)
        {
            await InsertHouseAsync(context, "CODIGO_SUPERIOR_A_VEINTE", "Código largo", "999", 1003);
        }
    }

    private static async Task InsertHouseAsync(AchDbContext context, string code, string name, string originCode, int temporaryOwner)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open) await connection.OpenAsync();
        await using var transaction = await connection.BeginTransactionAsync();
        var configId = await InsertAndReturnIdAsync(connection, transaction, context.Database.IsSqlServer()
            ? "INSERT INTO [ClearingHouseConfigs] ([ClearingHouseId], [HolidayStrategy]) OUTPUT INSERTED.[Id] VALUES (@owner, 'Colombian')"
            : "INSERT INTO \"ClearingHouseConfigs\" (\"ClearingHouseId\", \"HolidayStrategy\") VALUES (@owner, 'Colombian') RETURNING \"Id\"",
            ("@owner", temporaryOwner));
        var houseId = await InsertAndReturnIdAsync(connection, transaction, context.Database.IsSqlServer()
            ? "INSERT INTO [ClearingHouses] ([Name], [Code], [OriginCode], [ClearingHouseId]) OUTPUT INSERTED.[Id] VALUES (@name, @code, @origin, @config)"
            : "INSERT INTO \"ClearingHouses\" (\"Name\", \"Code\", \"OriginCode\", \"ClearingHouseId\") VALUES (@name, @code, @origin, @config) RETURNING \"Id\"",
            ("@name", name), ("@code", code), ("@origin", originCode), ("@config", configId));
        await ExecuteAsync(connection, transaction, context.Database.IsSqlServer()
            ? "UPDATE [ClearingHouseConfigs] SET [ClearingHouseId] = @house WHERE [Id] = @config"
            : "UPDATE \"ClearingHouseConfigs\" SET \"ClearingHouseId\" = @house WHERE \"Id\" = @config",
            ("@house", houseId), ("@config", configId));
        await transaction.CommitAsync();
    }

    private static async Task<int> InsertAndReturnIdAsync(DbConnection connection, DbTransaction transaction, string sql, params (string Name, object Value)[] values)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        AddParameters(command, values);
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static async Task ExecuteAsync(DbConnection connection, DbTransaction transaction, string sql, params (string Name, object Value)[] values)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        AddParameters(command, values);
        await command.ExecuteNonQueryAsync();
    }

    private static void AddParameters(DbCommand command, IEnumerable<(string Name, object Value)> values)
    {
        foreach (var (name, value) in values)
        {
            var parameter = command.CreateParameter();
            parameter.ParameterName = name;
            parameter.Value = value;
            command.Parameters.Add(parameter);
        }
    }

    private static async Task<int> ScalarIntAsync(AchDbContext context, DatabaseProvider provider, string template)
    {
        var sql = string.Format(template,
            provider == DatabaseProvider.SqlServer ? "[ClearingHouses]" : "\"ClearingHouses\"",
            provider == DatabaseProvider.SqlServer ? "[Code]" : "\"Code\"");
        return await ScalarIntCommandAsync(context, sql);
    }

    private static async Task<int> CodeMaximumLengthAsync(AchDbContext context, DatabaseProvider provider)
        => await ScalarIntCommandAsync(context, provider == DatabaseProvider.SqlServer
            ? "SELECT CHARACTER_MAXIMUM_LENGTH FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ClearingHouses' AND COLUMN_NAME = 'Code'"
            : "SELECT character_maximum_length FROM information_schema.columns WHERE table_name = 'ClearingHouses' AND column_name = 'Code'");

    private static async Task<int> MigrationAppliedAsync(AchDbContext context, DatabaseProvider provider, string migration)
        => await ScalarIntCommandAsync(context,
            provider == DatabaseProvider.SqlServer
                ? $"SELECT COUNT(*) FROM [__EFMigrationsHistory] WHERE [MigrationId] = '{migration}'"
                : $"SELECT COUNT(*) FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = '{migration}'");

    private static async Task<int> ColumnExistsAsync(AchDbContext context, DatabaseProvider provider, string table, string column)
        => await ScalarIntCommandAsync(context, provider == DatabaseProvider.SqlServer
            ? $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = '{table}' AND COLUMN_NAME = '{column}'"
            : $"SELECT COUNT(*) FROM information_schema.columns WHERE table_name = '{table}' AND column_name = '{column}'");

    private static async Task<int> ScalarIntCommandAsync(AchDbContext context, string sql)
    {
        var connection = context.Database.GetDbConnection();
        if (connection.State != System.Data.ConnectionState.Open) await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    private static void EnsureRequiredConfiguration(DatabaseProvider provider)
    {
        if (!string.Equals(Environment.GetEnvironmentVariable(RequiredVariable), "true", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"{RequiredVariable}=true es obligatorio para ClearingHouseMultiDb.");
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(ConnectionVariable(provider))))
            throw new InvalidOperationException($"Falta {ConnectionVariable(provider)} para ejecutar {provider} sin omisiones.");
    }

    private static string ConnectionVariable(DatabaseProvider provider)
        => provider == DatabaseProvider.SqlServer
            ? "CLEARING_HOUSES_SQLSERVER_CONNECTION_STRING"
            : "CLEARING_HOUSES_POSTGRES_CONNECTION_STRING";

    public enum DatabaseProvider { SqlServer, PostgreSql }

    private sealed class DatabaseFixture : IAsyncDisposable
    {
        private const string SqlServerMigrationsAssembly = "Cfa.ACHInterbank.Persistence.Migrations.SqlServer";
        private readonly string _databaseName;
        private readonly string _connectionString;
        private readonly string _adminConnectionString;

        private DatabaseFixture(DatabaseProvider provider, string databaseName, string connectionString, string adminConnectionString)
        {
            Provider = provider;
            _databaseName = databaseName;
            _connectionString = connectionString;
            _adminConnectionString = adminConnectionString;
        }

        public DatabaseProvider Provider { get; }

        public static async Task<DatabaseFixture> CreateAsync(DatabaseProvider provider)
        {
            var baseConnection = Environment.GetEnvironmentVariable(ConnectionVariable(provider))!;
            var databaseName = $"ach_ch_{Guid.NewGuid():N}";
            if (provider == DatabaseProvider.SqlServer)
            {
                var admin = new SqlConnectionStringBuilder(baseConnection) { InitialCatalog = "master" };
                await using var connection = new SqlConnection(admin.ConnectionString);
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = $"CREATE DATABASE [{databaseName}]";
                await command.ExecuteNonQueryAsync();
                var target = new SqlConnectionStringBuilder(baseConnection) { InitialCatalog = databaseName };
                return new DatabaseFixture(provider, databaseName, target.ConnectionString, admin.ConnectionString);
            }

            var postgresAdmin = new NpgsqlConnectionStringBuilder(baseConnection) { Database = "postgres" };
            await using (var connection = new NpgsqlConnection(postgresAdmin.ConnectionString))
            {
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = $"CREATE DATABASE \"{databaseName}\"";
                await command.ExecuteNonQueryAsync();
            }
            var postgresTarget = new NpgsqlConnectionStringBuilder(baseConnection) { Database = databaseName };
            return new DatabaseFixture(provider, databaseName, postgresTarget.ConnectionString, postgresAdmin.ConnectionString);
        }

        public AchDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AchDbContext>();
            if (Provider == DatabaseProvider.SqlServer)
                options.UseSqlServer(_connectionString, sql => sql.MigrationsAssembly(SqlServerMigrationsAssembly));
            else
                options.UseNpgsql(_connectionString);
            options.ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
            return new AchDbContext(options.Options);
        }

        public async ValueTask DisposeAsync()
        {
            if (Provider == DatabaseProvider.SqlServer)
            {
                await using var connection = new SqlConnection(_adminConnectionString);
                await connection.OpenAsync();
                await using var command = connection.CreateCommand();
                command.CommandText = $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_databaseName}]";
                await command.ExecuteNonQueryAsync();
                return;
            }

            await using var postgres = new NpgsqlConnection(_adminConnectionString);
            await postgres.OpenAsync();
            await using var terminate = postgres.CreateCommand();
            terminate.CommandText = "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = @database AND pid <> pg_backend_pid()";
            terminate.Parameters.AddWithValue("database", _databaseName);
            await terminate.ExecuteNonQueryAsync();
            await using var drop = postgres.CreateCommand();
            drop.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\"";
            await drop.ExecuteNonQueryAsync();
        }
    }
}
