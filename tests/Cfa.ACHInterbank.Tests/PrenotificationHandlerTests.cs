using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Repositories.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class PrenotificationHandlerTests
{

    [Fact]
    public async Task HandleAsync_CreatesPendingThirdParty_WhenSourceCustomerExistsAndIdentityIsNew()
    {
        var customer = new Customer { Id = 17 };
        var customerRepository = new Mock<Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories.IAchCustomerRepository>();
        customerRepository
            .Setup(repository => repository.FindBySourceAccountNumberAsync("111122223333", It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        CustomerThirdParty? added = null;
        var thirdPartyRepository = new Mock<Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories.ICustomerThirdPartyRepository>();
        thirdPartyRepository
            .Setup(repository => repository.FindAsync(
                customer.Id,
                2,
                "999988887777",
                "REC-001",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerThirdParty?)null);
        thirdPartyRepository
            .Setup(repository => repository.AddAsync(It.IsAny<CustomerThirdParty>(), It.IsAny<CancellationToken>()))
            .Callback<CustomerThirdParty, CancellationToken>((entity, _) => added = entity)
            .Returns(Task.CompletedTask);

        var transaction = new AchTransaction { Id = 51 };
        var handler = new PrenotificationHandler(customerRepository.Object, thirdPartyRepository.Object);

        await handler.HandleAsync(
            new AchTransactionRequestData
            {
                IsPrenotification = true,
                SourceAccountNumber = "111122223333",
                DestinationInstitutionId = 2,
                DestinationAccountNumber = " 999988887777 ",
                RecipientIdNumber = " rec-001 "
            },
            transaction,
            CancellationToken.None);

        Assert.NotNull(added);
        Assert.Equal(customer.Id, added!.CustomerId);
        Assert.Same(customer, added.Customer);
        Assert.Equal(CustomerThirdPartyStatusEnum.Pending, added.Status);
        Assert.Equal("999988887777", added.DestinationAccountNumber);
        Assert.Equal("REC-001", added.RecipientIdNumber);
        Assert.Same(transaction, added.PrenotificationTransaction);
    }

    [Fact]
    public async Task HandleAsync_ReusesPendingThirdParty_AndAddsOnlyOnceAcrossRetry()
    {
        var customer = new Customer { Id = 17 };
        var customerRepository = new Mock<Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories.IAchCustomerRepository>();
        customerRepository
            .Setup(repository => repository.FindBySourceAccountNumberAsync("111122223333", It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        CustomerThirdParty? stored = null;
        var thirdPartyRepository = new Mock<Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories.ICustomerThirdPartyRepository>();
        thirdPartyRepository
            .Setup(repository => repository.FindAsync(
                customer.Id,
                2,
                "999988887777",
                "REC-001",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => stored);
        thirdPartyRepository
            .Setup(repository => repository.AddAsync(It.IsAny<CustomerThirdParty>(), It.IsAny<CancellationToken>()))
            .Callback<CustomerThirdParty, CancellationToken>((entity, _) => stored = entity)
            .Returns(Task.CompletedTask);

        var handler = new PrenotificationHandler(customerRepository.Object, thirdPartyRepository.Object);
        var request = new AchTransactionRequestData
        {
            IsPrenotification = true,
            SourceAccountNumber = "111122223333",
            DestinationInstitutionId = 2,
            DestinationAccountNumber = "999988887777",
            RecipientIdNumber = "REC-001"
        };

        await handler.HandleAsync(request, new AchTransaction { Id = 51 }, CancellationToken.None);
        await handler.HandleAsync(request, new AchTransaction { Id = 52 }, CancellationToken.None);

        Assert.NotNull(stored);
        Assert.Equal(CustomerThirdPartyStatusEnum.Pending, stored!.Status);
        Assert.Equal(52, stored.PrenotificationTransactionId);
        thirdPartyRepository.Verify(
            repository => repository.AddAsync(It.IsAny<CustomerThirdParty>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_DoesNothing_WhenSourceCustomerCannotBeResolved()
    {
        var customerRepository = new Mock<Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories.IAchCustomerRepository>();
        customerRepository
            .Setup(repository => repository.FindBySourceAccountNumberAsync("UNKNOWN", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Customer?)null);
        var thirdPartyRepository = new Mock<Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories.ICustomerThirdPartyRepository>();
        var handler = new PrenotificationHandler(customerRepository.Object, thirdPartyRepository.Object);

        await handler.HandleAsync(
            new AchTransactionRequestData
            {
                IsPrenotification = true,
                SourceAccountNumber = "UNKNOWN",
                DestinationInstitutionId = 2,
                DestinationAccountNumber = "999988887777",
                RecipientIdNumber = "REC-001"
            },
            new AchTransaction { Id = 53 },
            CancellationToken.None);

        thirdPartyRepository.Verify(
            repository => repository.FindAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
        thirdPartyRepository.Verify(
            repository => repository.AddAsync(It.IsAny<CustomerThirdParty>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }
    [Theory]
    [InlineData(CustomerThirdPartyStatusEnum.Active)]
    [InlineData(CustomerThirdPartyStatusEnum.Rejected)]
    public async Task HandleAsync_DoesNotResetDefinitiveThirdPartyStatus(
        CustomerThirdPartyStatusEnum definitiveStatus)
    {
        var customer = new Customer { Id = 17 };
        var existing = new CustomerThirdParty
        {
            Id = 29,
            CustomerId = customer.Id,
            DestinationInstitutionId = 2,
            DestinationAccountNumber = "999988887777",
            RecipientIdNumber = "REC-001",
            PrenotificationTransactionId = 41
        };
        existing.ApplyAutomaticNachaResult(
            definitiveStatus,
            41,
            "CYCLE-VALIDATED",
            new DateTime(2026, 7, 26, 12, 0, 0, DateTimeKind.Utc),
            "Resultado definitivo",
            "test-evidence");
        var customerRepository = new Mock<Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories.IAchCustomerRepository>();
        customerRepository
            .Setup(repository => repository.FindBySourceAccountNumberAsync("111122223333", It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);
        var thirdPartyRepository = new Mock<Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories.ICustomerThirdPartyRepository>();
        thirdPartyRepository
            .Setup(repository => repository.FindAsync(
                customer.Id,
                2,
                "999988887777",
                "REC-001",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        var handler = new PrenotificationHandler(customerRepository.Object, thirdPartyRepository.Object);

        await handler.HandleAsync(
            new AchTransactionRequestData
            {
                IsPrenotification = true,
                SourceAccountNumber = "111122223333",
                DestinationInstitutionId = 2,
                DestinationAccountNumber = " 999988887777 ",
                RecipientIdNumber = " rec-001 "
            },
            new AchTransaction { Id = 99 },
            CancellationToken.None);

        Assert.Equal(definitiveStatus, existing.Status);
        Assert.Equal(41, existing.PrenotificationTransactionId);
        Assert.Equal("CYCLE-VALIDATED", existing.ValidationCycleId);
        Assert.Equal("Resultado definitivo", existing.ValidationMessage);
        thirdPartyRepository.Verify(
            repository => repository.AddAsync(It.IsAny<CustomerThirdParty>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_RecoversConcurrentCustomerThirdPartyUniqueConflict_ByRereadingWinner()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new InjectedSaveFailureAchDbContext(
            options,
            new FakeUniqueConstraintException());
        await using var databaseTransaction = await context.Database.BeginTransactionAsync();

        var customer = new Customer { Id = 17 };
        var winner = new CustomerThirdParty
        {
            Id = 29,
            CustomerId = customer.Id,
            DestinationInstitutionId = 2,
            DestinationAccountNumber = "999988887777",
            RecipientIdNumber = "REC-001",
            PrenotificationTransactionId = 41
        };
        var transaction = new AchTransaction { Id = 99 };

        var customerRepository = new Mock<Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories.IAchCustomerRepository>();
        customerRepository
            .Setup(repository => repository.FindBySourceAccountNumberAsync("111122223333", It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        CustomerThirdParty? attemptedInsert = null;
        var thirdPartyRepository = new Mock<Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories.ICustomerThirdPartyRepository>();
        thirdPartyRepository
            .SetupSequence(repository => repository.FindAsync(
                customer.Id,
                2,
                "999988887777",
                "REC-001",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerThirdParty?)null)
            .ReturnsAsync(winner);
        thirdPartyRepository
            .Setup(repository => repository.AddAsync(It.IsAny<CustomerThirdParty>(), It.IsAny<CancellationToken>()))
            .Callback<CustomerThirdParty, CancellationToken>((entity, _) =>
            {
                attemptedInsert = entity;
                context.Entry(entity).State = EntityState.Added;
            })
            .Returns(Task.CompletedTask);

        var handler = new PrenotificationHandler(customerRepository.Object, thirdPartyRepository.Object, context);

        await handler.HandleAsync(
            new AchTransactionRequestData
            {
                IsPrenotification = true,
                SourceAccountNumber = "111122223333",
                DestinationInstitutionId = 2,
                DestinationAccountNumber = " 999988887777 ",
                RecipientIdNumber = " rec-001 "
            },
            transaction,
            CancellationToken.None);

        Assert.Equal(1, context.SaveAttempts);
        Assert.NotNull(attemptedInsert);
        Assert.Equal(EntityState.Detached, context.Entry(attemptedInsert!).State);
        Assert.Empty(context.ChangeTracker.Entries<CustomerThirdParty>().Where(entry => entry.State == EntityState.Added));
        Assert.Equal(CustomerThirdPartyStatusEnum.Pending, winner.Status);
        Assert.Equal(transaction.Id, winner.PrenotificationTransactionId);
        thirdPartyRepository.Verify(
            repository => repository.FindAsync(
                customer.Id,
                2,
                "999988887777",
                "REC-001",
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        thirdPartyRepository.Verify(
            repository => repository.AddAsync(It.IsAny<CustomerThirdParty>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(CustomerThirdPartyStatusEnum.Active)]
    [InlineData(CustomerThirdPartyStatusEnum.Rejected)]
    public async Task HandleAsync_ConcurrentWinnerWithFinalStatus_IsRereadWithoutReset(
        CustomerThirdPartyStatusEnum definitiveStatus)
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new InjectedSaveFailureAchDbContext(
            options,
            new FakeUniqueConstraintException());
        await using var databaseTransaction = await context.Database.BeginTransactionAsync();

        var customer = new Customer { Id = 17 };
        var winner = new CustomerThirdParty
        {
            Id = 29,
            CustomerId = customer.Id,
            DestinationInstitutionId = 2,
            DestinationAccountNumber = "999988887777",
            RecipientIdNumber = "REC-001",
            PrenotificationTransactionId = 41
        };
        winner.ApplyAutomaticNachaResult(
            definitiveStatus,
            41,
            "CYCLE-VALIDATED",
            new DateTime(2026, 7, 26, 12, 0, 0, DateTimeKind.Utc),
            "Resultado definitivo",
            "test-evidence");

        var customerRepository = new Mock<Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories.IAchCustomerRepository>();
        customerRepository
            .Setup(repository => repository.FindBySourceAccountNumberAsync("111122223333", It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var thirdPartyRepository = new Mock<Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories.ICustomerThirdPartyRepository>();
        thirdPartyRepository
            .SetupSequence(repository => repository.FindAsync(
                customer.Id,
                2,
                "999988887777",
                "REC-001",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerThirdParty?)null)
            .ReturnsAsync(winner);
        thirdPartyRepository
            .Setup(repository => repository.AddAsync(It.IsAny<CustomerThirdParty>(), It.IsAny<CancellationToken>()))
            .Callback<CustomerThirdParty, CancellationToken>((entity, _) => context.Entry(entity).State = EntityState.Added)
            .Returns(Task.CompletedTask);

        var handler = new PrenotificationHandler(customerRepository.Object, thirdPartyRepository.Object, context);

        await handler.HandleAsync(
            new AchTransactionRequestData
            {
                IsPrenotification = true,
                SourceAccountNumber = "111122223333",
                DestinationInstitutionId = 2,
                DestinationAccountNumber = "999988887777",
                RecipientIdNumber = "REC-001"
            },
            new AchTransaction { Id = 99 },
            CancellationToken.None);

        Assert.Equal(definitiveStatus, winner.Status);
        Assert.Equal(41, winner.PrenotificationTransactionId);
        Assert.Equal("CYCLE-VALIDATED", winner.ValidationCycleId);
        Assert.Equal("Resultado definitivo", winner.ValidationMessage);
        thirdPartyRepository.Verify(
            repository => repository.FindAsync(
                customer.Id,
                2,
                "999988887777",
                "REC-001",
                It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        thirdPartyRepository.Verify(
            repository => repository.AddAsync(It.IsAny<CustomerThirdParty>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_DoesNotRetryOrReread_ForNonUniquePersistenceFailure()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new InjectedSaveFailureAchDbContext(
            options,
            new InvalidOperationException("Injected non-unique persistence failure."));
        await using var databaseTransaction = await context.Database.BeginTransactionAsync();

        var customer = new Customer { Id = 17 };
        var customerRepository = new Mock<Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories.IAchCustomerRepository>();
        customerRepository
            .Setup(repository => repository.FindBySourceAccountNumberAsync("111122223333", It.IsAny<CancellationToken>()))
            .ReturnsAsync(customer);

        var thirdPartyRepository = new Mock<Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories.ICustomerThirdPartyRepository>();
        thirdPartyRepository
            .Setup(repository => repository.FindAsync(
                customer.Id,
                2,
                "999988887777",
                "REC-001",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((CustomerThirdParty?)null);
        thirdPartyRepository
            .Setup(repository => repository.AddAsync(It.IsAny<CustomerThirdParty>(), It.IsAny<CancellationToken>()))
            .Callback<CustomerThirdParty, CancellationToken>((entity, _) => context.Entry(entity).State = EntityState.Added)
            .Returns(Task.CompletedTask);

        var handler = new PrenotificationHandler(customerRepository.Object, thirdPartyRepository.Object, context);

        await Assert.ThrowsAsync<DbUpdateException>(() => handler.HandleAsync(
            new AchTransactionRequestData
            {
                IsPrenotification = true,
                SourceAccountNumber = "111122223333",
                DestinationInstitutionId = 2,
                DestinationAccountNumber = "999988887777",
                RecipientIdNumber = "REC-001"
            },
            new AchTransaction { Id = 99 },
            CancellationToken.None));

        Assert.Equal(1, context.SaveAttempts);
        thirdPartyRepository.Verify(
            repository => repository.FindAsync(
                customer.Id,
                2,
                "999988887777",
                "REC-001",
                It.IsAny<CancellationToken>()),
            Times.Once);
        thirdPartyRepository.Verify(
            repository => repository.AddAsync(It.IsAny<CustomerThirdParty>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_CreatesThirdParty_WithCustomerNavigation_WhenCustomerIsTrackedWithTemporaryKey()
    {
        using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();

        context.ClearingHouseConfigs.Add(new ClearingHouseConfig
        {
            Id = 1,
            HolidayStrategy = "Colombian"
        });

        context.ClearingHouses.Add(new ClearingHouse
        {
            Id = 1,
            Name = "ACH Colombia",
            Code = "ACH",
            OriginCode = "12345678",
            ClearingHouseId = 1
        });

        var sourceFi = new FinancialInstitution
        {
            Id = 1,
            Name = "Banco Origen",
            Status = FinancialInstitutionStatus.Active,
            IsDefaultSource = true,
            RoutingNumber = "12345",
            TransitCode = "678"
        };
        sourceFi.CalculateCheckDigit();

        var destinationFi = new FinancialInstitution
        {
            Id = 2,
            Name = "Banco Destino",
            Status = FinancialInstitutionStatus.Active,
            IsDefaultSource = false,
            RoutingNumber = "76543",
            TransitCode = "210"
        };
        destinationFi.CalculateCheckDigit();
        context.FinancialInstitutions.AddRange(sourceFi, destinationFi);

        var cycle = new AchCycle
        {
            Id = "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
            ClearingHouseId = 1,
            CycleName = "CICLO-1",
            ProcessingDate = DateTime.UtcNow.Date,
            StartTime = TimeSpan.Zero,
            EndTime = new TimeSpan(23, 59, 0),
            CutoffTime = new TimeSpan(22, 0, 0)
        };
        context.AchCycles.Add(cycle);

        var companyEntryDescriptionId = await context.CompanyEntryDescriptionCatalogs
            .Where(x => x.Term == "NOMINAS" && x.IsActive)
            .Select(x => x.Id)
            .FirstAsync();

        var batch = new AchBatch
        {
            AchCycleId = cycle.Id,
            CompanyName = "EMPRESA",
            CompanyIdentification = "900123456",
            CompanyEntryDescription = "NOMINAS",
            CompanyEntryDescriptionId = companyEntryDescriptionId,
            OriginOrOdfi = "12345678",
            EffectiveEntryDate = cycle.ProcessingDate,
            ServiceClassCode = "220",
            BatchSequenceNumber = 1
        };

        var customer = new Customer
        {
            FirstName = "Empresa",
            LastName = "Demo",
            PersonType = "PJ",
            DocumentType = "NIT",
            DocumentNumber = "900123456",
            CompanyName = "EMPRESA"
        };
        customer.Accounts.Add(new CustomerAccount { AccountNumber = "111122223333" });
        context.Customers.Add(customer);

        var tx = new AchTransaction
        {
            Amount = 0m,
            TransactionExternalId = "TX-PRN-001",
            Reference = "REF-PRN-001",
            Type = TransactionTypeEnum.Prenotification,
            TransactionCode = "23",
            ServiceClassCode = "220",
            CompanyEntryDescriptionId = companyEntryDescriptionId,
            CompanyName = "EMPRESA",
            CompanyIdentification = "900123456",
            OriginatingDFI = "123456780",
            ReceivingDFI = "765432100",
            TraceNumber = "123456780000001",
            TraceSequenceNumber = 1,
            EffectiveEntryDate = DateTime.UtcNow.Date,
            AddendaRecordIndicator = true,
            IsPrenotification = true,
            SourceAccountNumber = "111122223333",
            DestinationAccountNumber = "999988887777",
            SourceInstitutionId = 1,
            DestinationInstitutionId = 2,
            AchCycleId = cycle.Id,
            AchBatch = batch
        };
        context.AchTransactions.Add(tx);

        var customerRepo = new AchCustomerRepository(context);
        var thirdPartyRepo = new CustomerThirdPartyRepository(context);
        var handler = new PrenotificationHandler(customerRepo, thirdPartyRepo);

        var request = new AchTransactionRequestData
        {
            IsPrenotification = true,
            SourceAccountNumber = "111122223333",
            DestinationAccountNumber = "999988887777",
            DestinationInstitutionId = 2,
            RecipientIdNumber = "123456789"
        };

        await handler.HandleAsync(request, tx, CancellationToken.None);
        await context.SaveChangesAsync();

        var thirdParty = await context.CustomerThirdParties.SingleAsync();
        Assert.True(thirdParty.CustomerId > 0);
        Assert.Equal(customer.Id, thirdParty.CustomerId);
    }
    private sealed class FakeUniqueConstraintException : Exception
    {
        public int SqliteErrorCode => 19;
    }

    private sealed class InjectedSaveFailureAchDbContext : AchDbContext
    {
        private readonly Exception _innerException;
        private bool _failNextSave = true;

        public InjectedSaveFailureAchDbContext(
            DbContextOptions<AchDbContext> options,
            Exception innerException)
            : base(options)
        {
            _innerException = innerException;
        }

        public int SaveAttempts { get; private set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveAttempts++;
            if (_failNextSave)
            {
                _failNextSave = false;
                throw new DbUpdateException("Injected persistence failure.", _innerException);
            }

            return base.SaveChangesAsync(cancellationToken);
        }
    }

}
