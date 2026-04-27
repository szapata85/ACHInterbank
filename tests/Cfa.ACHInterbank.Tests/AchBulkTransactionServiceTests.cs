using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class AchBulkTransactionServiceTests
{
    private const int TestClearingHouseConfigId = 9001;
    private const int TestClearingHouseId = 9001;
    private const int TestSourceInstitutionId = 9101;
    private const int TestDestinationInstitutionId = 9102;
    private const string TestCycleId = "CYCLE-1";
    private const int TestBatchId = 9201;

    [Fact]
    public async Task RegisterBulkAsync_ReturnsTotalSuccess_WhenAllItemsAreValid()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        var companyEntryDescriptionId = SeedCatalog(context);

        var service = CreateService(context, companyEntryDescriptionId);
        var request = BuildRequest("BULK-OK", 2, companyEntryDescriptionId);

        var response = await service.RegisterBulkAsync(request);

        Assert.Equal(2, response.TotalReceived);
        Assert.Equal(2, response.TotalSucceeded);
        Assert.Equal(0, response.TotalFailed);
        Assert.All(response.ItemResults, item => Assert.True(item.Succeeded));
    }

    [Fact]
    public async Task RegisterBulkAsync_ReturnsPartialSuccess_WhenOneItemFailsValidation()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        var companyEntryDescriptionId = SeedCatalog(context);

        var validator = new Mock<ITransactionValidator>();
        validator
            .Setup(v => v.ValidateRequest(It.Is<AchTransactionRequestData>(x => x.Reference == "BULK-PARTIAL-0002"), It.IsAny<IReadOnlySet<int>?>()))
            .Throws(new ArgumentException("Referencia inválida."));

        var service = CreateService(context, companyEntryDescriptionId, validator: validator);
        var request = BuildRequest("BULK-PARTIAL", 3, companyEntryDescriptionId);

        var response = await service.RegisterBulkAsync(request);

        Assert.Equal(3, response.TotalProcessed);
        Assert.Equal(2, response.TotalSucceeded);
        Assert.Equal(1, response.TotalFailed);
        Assert.Contains(response.ItemResults, x => !x.Succeeded && x.ErrorCode == "ITEM_VALIDATION_FAILED");
    }

    [Fact]
    public async Task RegisterBulkAsync_ReturnsTotalFailure_WhenAllItemsFailValidation()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        var companyEntryDescriptionId = SeedCatalog(context);

        var validator = new Mock<ITransactionValidator>();
        validator
            .Setup(v => v.ValidateRequest(It.IsAny<AchTransactionRequestData>(), It.IsAny<IReadOnlySet<int>?>()))
            .Throws(new ArgumentException("Lote inválido."));

        var service = CreateService(context, companyEntryDescriptionId, validator: validator);
        var request = BuildRequest("BULK-FAIL", 2, companyEntryDescriptionId);

        var response = await service.RegisterBulkAsync(request);

        Assert.Equal(0, response.TotalSucceeded);
        Assert.Equal(2, response.TotalFailed);
    }

    [Fact]
    public async Task RegisterBulkAsync_FailsItems_WhenDuplicateReferencesExistInRequestOrPersistence()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        var companyEntryDescriptionId = SeedCatalog(context);

        context.AchTransactions.Add(new AchTransaction
        {
            Amount = 100,
            Reference = "BULK-DUP-0002",
            Type = TransactionTypeEnum.Credit,
            SourceAccountNumber = "1234567890",
            DestinationAccountNumber = "9876543210",
            CompanyName = "EMPRESA",
            CompanyIdentification = "900123456",
            CompanyEntryDescriptionId = companyEntryDescriptionId,
            TransactionCode = "22",
            OriginatingDFI = "000010070",
            ReceivingDFI = "000010010",
            TraceNumber = "000010070000001",
            TraceSequenceNumber = 1,
            EffectiveEntryDate = DateTime.Today,
            SourceInstitutionId = TestSourceInstitutionId,
            DestinationInstitutionId = TestDestinationInstitutionId,
            AchCycleId = TestCycleId,
            AchBatchId = TestBatchId
        });
        await context.SaveChangesAsync();

        var service = CreateService(context, companyEntryDescriptionId);
        var request = BuildRequest("BULK-DUP", 3, companyEntryDescriptionId);
        request.Transactions[2].Reference = request.Transactions[0].Reference;

        var response = await service.RegisterBulkAsync(request);

        Assert.Equal(0, response.TotalSucceeded);
        Assert.Equal(3, response.TotalFailed);
        Assert.Contains(response.ItemResults, x => x.ErrorMessage!.Contains("duplicad", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(response.ItemResults, x => x.ErrorMessage!.Contains("ya exis", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RegisterBulkAsync_Throws_WhenBatchExceedsMaxItems()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        var companyEntryDescriptionId = SeedCatalog(context);

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Transactions:Bulk:MaxItems"] = "2",
            ["Transactions:Bulk:ChunkSize"] = "50"
        }).Build();

        var service = CreateService(context, companyEntryDescriptionId, configuration: configuration);
        var request = BuildRequest("BULK-LIMIT", 3, companyEntryDescriptionId);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.RegisterBulkAsync(request));
        Assert.Contains("máximo permitido", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static AchBulkTransactionService CreateService(
        AchDbContext context,
        int companyEntryDescriptionId,
        IConfiguration? configuration = null,
        Mock<ITransactionValidator>? validator = null)
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        unitOfWork.Setup(u => u.CommitAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var customerRepository = new Mock<IAchCustomerRepository>();
        customerRepository.Setup(r => r.ResolveDocumentTypeCodeAsync("NIT", It.IsAny<CancellationToken>())).ReturnsAsync("NIT");
        customerRepository.Setup(r => r.ResolveDocumentTypeCodeAsync("CC", It.IsAny<CancellationToken>())).ReturnsAsync("CC");
        customerRepository.Setup(r => r.ResolvePersonTypeCodeAsync("PJ", It.IsAny<CancellationToken>())).ReturnsAsync("PJ");
        customerRepository.Setup(r => r.ResolvePersonTypeCodeAsync("PN", It.IsAny<CancellationToken>())).ReturnsAsync("PN");
        customerRepository.Setup(r => r.AddAsync(It.IsAny<Customer>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var txValidator = validator ?? new Mock<ITransactionValidator>();

        var batchResolver = new Mock<IBatchResolver>();
        batchResolver.Setup(r => r.ResolveAsync(It.IsAny<AchTransactionRequestData>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new TransactionBatchContext
            {
                Batch = new AchBatch { Id = TestBatchId, AchCycleId = TestCycleId, CompanyEntryDescription = "NOMINAS", EffectiveEntryDate = DateTime.Today },
                AchCycleId = TestCycleId,
                EffectiveEntryDate = DateTime.Today,
                OriginatingDfi = "000010070",
                ReceivingDfi = "000010010",
                CompanyName = "EMPRESA",
                CompanyIdentification = "900123456",
                CompanyEntryDescription = "NOMINAS",
                CompanyEntryDescriptionId = companyEntryDescriptionId,
                ServiceClassCode = "200",
                SourceInstitutionId = TestSourceInstitutionId,
                DestinationInstitutionId = TestDestinationInstitutionId
            });

        var nextId = 100;
        var persister = new Mock<ITransactionPersister>();
        persister.Setup(p => p.PersistAsync(It.IsAny<AchTransactionRequestData>(), It.IsAny<TransactionBatchContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                nextId++;
                var tx = new AchTransaction { Id = nextId };
                return new TransactionPersistResult { Transaction = tx, Batch = new AchBatch { Id = TestBatchId } };
            });
        persister.Setup(p => p.UpdateBatchTotalsAsync(It.IsAny<AchBatch>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        persister.Setup(p => p.UpdateBatchServiceClassCodeAsync(It.IsAny<AchBatch>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var prenotification = new Mock<IPrenotificationHandler>();
        prenotification.Setup(x => x.HandleAsync(It.IsAny<AchTransactionRequestData>(), It.IsAny<AchTransaction>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var contrapartida = new Mock<IContrapartidaDispatchPersistenceService>();
        contrapartida.Setup(x => x.EnsurePendingDispatchAsync(It.IsAny<AchTransaction>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new ContrapartidaDispatchItem());

        return new AchBulkTransactionService(
            context,
            unitOfWork.Object,
            customerRepository.Object,
            txValidator.Object,
            batchResolver.Object,
            persister.Object,
            prenotification.Object,
            configuration ?? new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Transactions:Bulk:MaxItems"] = "2000",
                ["Transactions:Bulk:ChunkSize"] = "50"
            }).Build(),
            contrapartida.Object);
    }

    private static BulkAchTransactionRequest BuildRequest(string prefix, int count, int companyEntryDescriptionId)
    {
        var items = Enumerable.Range(1, count).Select(i => new BulkAchTransactionItemRequest
        {
            Amount = 1000 + i,
            Reference = $"{prefix}-{i:0000}",
            Type = TransactionTypeEnum.Credit,
            AccountType = AccountTypeEnum.Checking,
            IsPrenotification = false,
            DestinationInstitutionId = TestDestinationInstitutionId,
            SourceAccountNumber = $"123450{i:00000}",
            DestinationAccountNumber = $"987650{i:00000}",
            CompanyName = "EMPRESA",
            CompanyIdentification = "900123456",
            CompanyEntryDescriptionId = companyEntryDescriptionId,
            SourcePersonType = "PJ",
            RecipientPersonType = "PN",
            RecipientIdNumber = $"10{i:00000000}",
            RecipientName = $"CLIENTE {i}",
            Addendas = [new AddendaDto { AddendaType = "05", Information = "PAGO" }]
        }).ToList();

        return new BulkAchTransactionRequest
        {
            BatchReference = $"{prefix}-BATCH",
            ChunkSize = 50,
            Transactions = items
        };
    }

    private static int SeedCatalog(AchDbContext context)
    {
        var companyEntryDescriptionId = context.CompanyEntryDescriptionCatalogs
            .Where(x => x.Term == "NOMINAS" && x.IsActive)
            .Select(x => x.Id)
            .First();

        if (!context.ClearingHouseConfigs.Any(x => x.Id == TestClearingHouseConfigId))
        {
            context.ClearingHouseConfigs.Add(new ClearingHouseConfig
            {
                Id = TestClearingHouseConfigId,
                HolidayStrategy = "Colombian"
            });
        }

        if (!context.ClearingHouses.Any(x => x.Id == TestClearingHouseId))
        {
            context.ClearingHouses.Add(new ClearingHouse
            {
                Id = TestClearingHouseId,
                Name = "ACH Colombia",
                Code = "ACHCOL",
                OriginCode = "12345678",
                ClearingHouseId = TestClearingHouseConfigId
            });
        }

        if (!context.AchCycles.Any(x => x.Id == TestCycleId))
        {
            context.AchCycles.Add(new AchCycle
            {
                Id = TestCycleId,
                CycleName = "Ciclo 1",
                ProcessingDate = DateTime.Today,
                StartTime = TimeSpan.Zero,
                EndTime = new TimeSpan(23, 59, 0),
                CutoffTime = new TimeSpan(23, 59, 0),
                ClearingHouseId = TestClearingHouseId
            });
        }

        if (!context.AchBatches.Any(x => x.Id == TestBatchId))
        {
            context.AchBatches.Add(new AchBatch
            {
                Id = TestBatchId,
                AchCycleId = TestCycleId,
                CompanyName = "EMPRESA",
                CompanyIdentification = "900123456",
                CompanyEntryDescriptionId = companyEntryDescriptionId,
                CompanyEntryDescription = "NOMINAS",
                OriginOrOdfi = "00001007",
                EffectiveEntryDate = DateTime.Today,
                BatchSequenceNumber = 1
            });
        }

        if (!context.FinancialInstitutions.Any(x => x.Id == TestSourceInstitutionId))
        {
            context.FinancialInstitutions.Add(new FinancialInstitution { Id = TestSourceInstitutionId, Name = "Origen", RoutingNumber = "00001", TransitCode = "007", IsDefaultSource = true, Status = FinancialInstitutionStatus.Active });
        }

        if (!context.FinancialInstitutions.Any(x => x.Id == TestDestinationInstitutionId))
        {
            context.FinancialInstitutions.Add(new FinancialInstitution { Id = TestDestinationInstitutionId, Name = "Destino", RoutingNumber = "00001", TransitCode = "001", Status = FinancialInstitutionStatus.Active });
        }

        foreach (var institution in context.FinancialInstitutions.Local)
        {
            institution.CalculateCheckDigit();
        }

        context.SaveChanges();
        return companyEntryDescriptionId;
    }

    private static SqliteConnection CreateOpenConnection()
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
}
