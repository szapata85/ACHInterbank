using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cfa.ACHInterbank.Tests;

public sealed class ThirdPartyOnboardingServiceTests
{
    [Fact]
    public async Task EnsureAccountAsync_CreatesNormalizesAndReusesCanonicalMaster()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();
        var customerId = await SeedMasterPrerequisitesAsync(context);
        var logger = new Mock<ILogger<ThirdPartyOnboardingService>>();
        var service = new ThirdPartyOnboardingService(context, logger.Object);

        var request = new ThirdPartyOnboardingRequest(
            customerId,
            " cc ",
            " ab-123 ",
            "  Receptor Uno  ",
            " pn ",
            2,
            AccountTypeEnum.Savings,
            " 0012345 ",
            ThirdPartyAccountOriginSource.SingleTransaction);

        var first = await service.EnsureAccountAsync(request);
        var second = await service.EnsureAccountAsync(request with { SourceMetadata = ThirdPartyAccountOriginSource.BulkTransaction });

        Assert.True(first.ThirdPartyCreated);
        Assert.True(first.ThirdPartyAccountCreated);
        Assert.False(second.ThirdPartyCreated);
        Assert.False(second.ThirdPartyAccountCreated);
        Assert.Equal(first.ThirdPartyId, second.ThirdPartyId);
        Assert.Equal(first.ThirdPartyAccountId, second.ThirdPartyAccountId);
        Assert.Single(await context.ThirdParties.ToListAsync());
        Assert.Single(await context.ThirdPartyAccounts.ToListAsync());

        var thirdParty = await context.ThirdParties.SingleAsync();
        Assert.Equal("CC", thirdParty.DocumentType);
        Assert.Equal("AB-123", thirdParty.DocumentNumberNormalized);
        Assert.Equal("PN", thirdParty.PersonType);
        Assert.Equal("Receptor Uno", thirdParty.DisplayName);

        var account = await context.ThirdPartyAccounts.SingleAsync();
        Assert.Equal("0012345", account.AccountNumberNormalized);
        Assert.Equal(AccountTypeEnum.Savings, account.AccountType);
        Assert.Equal(ThirdPartyAccountOriginSource.SingleTransaction, account.OriginSource);
    }

    [Fact]
    public async Task EnsureAccountAsync_ReusesThirdParty_WhenAnotherCanonicalAccountIsDiscovered()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();
        var customerId = await SeedMasterPrerequisitesAsync(context);
        var service = new ThirdPartyOnboardingService(context, Mock.Of<ILogger<ThirdPartyOnboardingService>>());

        var first = await service.EnsureAccountAsync(BuildRequest(customerId, "10001", AccountTypeEnum.Checking));
        var second = await service.EnsureAccountAsync(BuildRequest(customerId, "10002", AccountTypeEnum.Savings));

        Assert.Equal(first.ThirdPartyId, second.ThirdPartyId);
        Assert.Single(await context.ThirdParties.ToListAsync());
        Assert.Equal(2, await context.ThirdPartyAccounts.CountAsync());
    }

    [Fact]
    public async Task EnsureAccountAsync_RecoversUniqueConflict_WithIdempotentReread()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"ach-tpa2-{Guid.NewGuid():N}.db");
        try
        {
            var baseOptions = new DbContextOptionsBuilder<AchDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;

            int customerId;
            await using (var seedContext = new AchDbContext(baseOptions))
            {
                await seedContext.Database.EnsureCreatedAsync();
                customerId = await SeedMasterPrerequisitesAsync(seedContext);
            }

            var request = BuildRequest(customerId, "70001", AccountTypeEnum.Checking);
            var interceptor = new InjectConcurrentThirdPartyInterceptor(databasePath, request);
            var options = new DbContextOptionsBuilder<AchDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .AddInterceptors(interceptor)
                .Options;

            await using var context = new AchDbContext(options);
            var service = new ThirdPartyOnboardingService(context, Mock.Of<ILogger<ThirdPartyOnboardingService>>());

            var result = await service.EnsureAccountAsync(request);

            Assert.True(result.RecoveredFromUniqueConflict);
            Assert.False(result.ThirdPartyCreated);
            Assert.True(result.ThirdPartyAccountCreated);
            Assert.Equal(1, await context.ThirdParties.CountAsync());
            Assert.Equal(1, await context.ThirdPartyAccounts.CountAsync());
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    [Fact]
    public async Task EnsureAccountAsync_RecoversConcurrentAccountUniqueConflict_WithIdempotentReread()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"ach-tpa2-account-{Guid.NewGuid():N}.db");
        try
        {
            var baseOptions = new DbContextOptionsBuilder<AchDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .Options;

            int customerId;
            long thirdPartyId;
            var request = default(ThirdPartyOnboardingRequest)!;
            await using (var seedContext = new AchDbContext(baseOptions))
            {
                await seedContext.Database.EnsureCreatedAsync();
                customerId = await SeedMasterPrerequisitesAsync(seedContext);
                request = BuildRequest(customerId, "71001", AccountTypeEnum.Checking);
                var thirdParty = new ThirdParty
                {
                    CustomerId = customerId,
                    DocumentType = request.DocumentType,
                    DocumentNumberNormalized = request.DocumentNumber,
                    DisplayName = request.DisplayName,
                    PersonType = request.PersonType
                };
                seedContext.ThirdParties.Add(thirdParty);
                await seedContext.SaveChangesAsync();
                thirdPartyId = thirdParty.Id;
            }

            var interceptor = new InjectConcurrentAccountInterceptor(databasePath, thirdPartyId, request);
            var options = new DbContextOptionsBuilder<AchDbContext>()
                .UseSqlite($"Data Source={databasePath}")
                .AddInterceptors(interceptor)
                .Options;

            await using var context = new AchDbContext(options);
            var service = new ThirdPartyOnboardingService(context, Mock.Of<ILogger<ThirdPartyOnboardingService>>());

            var result = await service.EnsureAccountAsync(request);

            Assert.True(result.RecoveredFromUniqueConflict);
            Assert.False(result.ThirdPartyCreated);
            Assert.False(result.ThirdPartyAccountCreated);
            Assert.Equal(thirdPartyId, result.ThirdPartyId);
            Assert.Equal(1, await context.ThirdPartyAccounts.CountAsync());
        }
        finally
        {
            if (File.Exists(databasePath))
            {
                File.Delete(databasePath);
            }
        }
    }

    [Fact]
    public async Task EnsureAccountAsync_PreCanceledToken_DoesNotCreateCanonicalRows()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();
        var customerId = await SeedMasterPrerequisitesAsync(context);
        var service = new ThirdPartyOnboardingService(context, Mock.Of<ILogger<ThirdPartyOnboardingService>>());
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.EnsureAccountAsync(BuildRequest(customerId, "72001", AccountTypeEnum.Checking), cts.Token));

        Assert.Empty(await context.ThirdParties.ToListAsync());
        Assert.Empty(await context.ThirdPartyAccounts.ToListAsync());
    }

    [Fact]
    public void TransactionMapper_SkipsCanonicalOnboarding_WhenSourceCustomerCannotBeResolved()
    {
        var transaction = new AchTransactionRequestData
        {
            AccountType = AccountTypeEnum.Checking,
            DestinationInstitutionId = 2,
            DestinationAccountNumber = "12345",
            RecipientIdNumber = "1010",
            RecipientName = "Receptor"
        };
        var recipient = new Customer { Id = 20, DocumentType = "CC", PersonType = "PN", DocumentNumber = "1010", FirstName = "R", LastName = "X" };

        Assert.Null(ThirdPartyOnboardingRequestMapper.TryCreate(
            transaction,
            sourceCustomer: null,
            recipient,
            ThirdPartyAccountOriginSource.SingleTransaction));
    }

    [Fact]
    public void TransactionMapper_DoesNotInventMasterIdentity_WhenRecipientDataIsInsufficient()
    {
        var transaction = new AchTransactionRequestData
        {
            AccountType = AccountTypeEnum.Checking,
            DestinationInstitutionId = 2,
            DestinationAccountNumber = "12345",
            RecipientName = "Receptor"
        };
        var source = new Customer { Id = 10, DocumentType = "NIT", PersonType = "PJ", DocumentNumber = "9001", FirstName = "E", LastName = "X" };

        Assert.Null(ThirdPartyOnboardingRequestMapper.TryCreate(
            transaction,
            source,
            recipientCustomer: null,
            ThirdPartyAccountOriginSource.BulkTransaction));
    }

    [Fact]
    public void OnboardingContract_HasNoClearingHouseOrPrenotificationDimension()
    {
        var propertyNames = typeof(ThirdPartyOnboardingRequest).GetProperties().Select(x => x.Name).ToArray();
        var forbidden = new[] { "ClearingHouse", "Prenotification", "ServiceClass", "Cycle", "Deadline", "Sla", "TransactionNature" };

        foreach (var fragment in forbidden)
        {
            Assert.DoesNotContain(propertyNames, x => x.Contains(fragment, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public async Task EnsureAccountAsync_ObservabilityDoesNotRenderDocumentOrFullAccount()
    {
        using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CreateContext(connection);
        await context.Database.EnsureCreatedAsync();
        var customerId = await SeedMasterPrerequisitesAsync(context);
        var logger = new CapturingLogger<ThirdPartyOnboardingService>();
        var service = new ThirdPartyOnboardingService(context, logger);
        var request = BuildRequest(customerId, "9988776655", AccountTypeEnum.Checking) with
        {
            DocumentNumber = "DOC-SENSITIVE-123"
        };

        await service.EnsureAccountAsync(request);

        var rendered = string.Join("\n", logger.Messages);
        Assert.DoesNotContain("DOC-SENSITIVE-123", rendered, StringComparison.Ordinal);
        Assert.DoesNotContain("9988776655", rendered, StringComparison.Ordinal);

        var auditEntries = await context.AuditLogs
            .Where(x => x.EntityName == nameof(ThirdParty) || x.EntityName == nameof(ThirdPartyAccount))
            .ToListAsync();
        var auditPayload = string.Join("\n", auditEntries.Select(x =>
            (x.BeforeJson ?? string.Empty) + (x.AfterJson ?? string.Empty) + (x.ChangedFields ?? string.Empty)));
        Assert.DoesNotContain("DOC-SENSITIVE-123", auditPayload, StringComparison.Ordinal);
        Assert.DoesNotContain("9988776655", auditPayload, StringComparison.Ordinal);
        Assert.DoesNotContain("Receptor Canonico", auditPayload, StringComparison.Ordinal);
    }

    private static ThirdPartyOnboardingRequest BuildRequest(
        int customerId,
        string accountNumber,
        AccountTypeEnum accountType)
        => new(
            customerId,
            "CC",
            "10101010",
            "Receptor Canonico",
            "PN",
            2,
            accountType,
            accountNumber,
            ThirdPartyAccountOriginSource.SingleTransaction);

    private static AchDbContext CreateContext(SqliteConnection connection)
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;
        return new AchDbContext(options);
    }

    private static async Task<int> SeedMasterPrerequisitesAsync(AchDbContext context)
    {
        if (!await context.PersonTypes.AnyAsync(x => x.Code == "PN"))
        {
            context.PersonTypes.AddRange(
                new PersonTypeCatalog { Code = "PN", Name = "Natural" },
                new PersonTypeCatalog { Code = "PJ", Name = "Juridica" });
        }

        if (!await context.DocumentTypes.AnyAsync(x => x.Code == "CC"))
        {
            context.DocumentTypes.AddRange(
                new DocumentTypeCatalog { Code = "CC", Name = "Cedula" },
                new DocumentTypeCatalog { Code = "NIT", Name = "NIT" });
        }

        if (!await context.FinancialInstitutions.AnyAsync(x => x.Id == 2))
        {
            var institution = new FinancialInstitution
            {
                Id = 2,
                Name = "Destino",
                RoutingNumber = "7654321",
                TransitCode = "0"
            };
            institution.CalculateCheckDigit();
            context.FinancialInstitutions.Add(institution);
        }

        var customer = new Customer
        {
            FirstName = "Empresa",
            LastName = "Demo",
            CompanyName = "Empresa Demo",
            PersonType = "PJ",
            DocumentType = "NIT",
            DocumentNumber = "9000000001"
        };
        context.Customers.Add(customer);
        await context.SaveChangesAsync();
        return customer.Id;
    }

    private sealed class InjectConcurrentThirdPartyInterceptor : SaveChangesInterceptor
    {
        private readonly string _databasePath;
        private readonly ThirdPartyOnboardingRequest _request;
        private bool _triggered;

        public InjectConcurrentThirdPartyInterceptor(string databasePath, ThirdPartyOnboardingRequest request)
        {
            _databasePath = databasePath;
            _request = request;
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (_triggered
                || eventData.Context is not AchDbContext context
                || !context.ChangeTracker.Entries<ThirdParty>().Any(x => x.State == EntityState.Added))
            {
                return await base.SavingChangesAsync(eventData, result, cancellationToken);
            }

            _triggered = true;
            var options = new DbContextOptionsBuilder<AchDbContext>()
                .UseSqlite($"Data Source={_databasePath}")
                .Options;
            await using var competitor = new AchDbContext(options);
            competitor.ThirdParties.Add(new ThirdParty
            {
                CustomerId = _request.CustomerId,
                DocumentType = _request.DocumentType,
                DocumentNumberNormalized = _request.DocumentNumber,
                DisplayName = _request.DisplayName,
                PersonType = _request.PersonType
            });
            await competitor.SaveChangesAsync(cancellationToken);

            throw new DbUpdateException("Simulated unique conflict.", new FakeSqliteUniqueException());
        }
    }

    private sealed class FakeSqliteUniqueException : Exception
    {
        public int SqliteErrorCode => 19;
    }

    private sealed class InjectConcurrentAccountInterceptor : SaveChangesInterceptor
    {
        private readonly string _databasePath;
        private readonly long _thirdPartyId;
        private readonly ThirdPartyOnboardingRequest _request;
        private bool _triggered;

        public InjectConcurrentAccountInterceptor(
            string databasePath,
            long thirdPartyId,
            ThirdPartyOnboardingRequest request)
        {
            _databasePath = databasePath;
            _thirdPartyId = thirdPartyId;
            _request = request;
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            if (_triggered
                || eventData.Context is not AchDbContext context
                || !context.ChangeTracker.Entries<ThirdPartyAccount>().Any(x => x.State == EntityState.Added))
            {
                return await base.SavingChangesAsync(eventData, result, cancellationToken);
            }

            _triggered = true;
            var options = new DbContextOptionsBuilder<AchDbContext>()
                .UseSqlite($"Data Source={_databasePath}")
                .Options;
            await using var competitor = new AchDbContext(options);
            competitor.ThirdPartyAccounts.Add(new ThirdPartyAccount
            {
                ThirdPartyId = _thirdPartyId,
                FinancialInstitutionId = _request.FinancialInstitutionId,
                AccountType = _request.AccountType,
                AccountNumberNormalized = _request.AccountNumber,
                OriginSource = ThirdPartyAccountOriginSource.BulkTransaction
            });
            await competitor.SaveChangesAsync(cancellationToken);

            throw new DbUpdateException("Simulated account unique conflict.", new FakeSqliteUniqueException());
        }
    }

    private sealed class CapturingLogger<T> : ILogger<T>
    {
        public List<string> Messages { get; } = [];
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
            => Messages.Add(formatter(state, exception));
    }
}
