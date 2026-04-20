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
    [Fact]
    public async Task RegisterBulkAsync_ReturnsTotalSuccess_WhenAllItemsAreValid()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        SeedCatalog(context);

        var service = CreateService(context);
        var request = BuildRequest("BULK-OK", 2);

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
        SeedCatalog(context);

        var validator = new Mock<ITransactionValidator>();
        validator
            .Setup(v => v.ValidateRequest(It.Is<AchTransactionRequestData>(x => x.Reference == "BULK-PARTIAL-0002"), It.IsAny<IReadOnlySet<int>?>()))
            .Throws(new ArgumentException("Referencia inválida."));

        var service = CreateService(context, validator: validator);
        var request = BuildRequest("BULK-PARTIAL", 3);

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
        SeedCatalog(context);

        var validator = new Mock<ITransactionValidator>();
        validator
            .Setup(v => v.ValidateRequest(It.IsAny<AchTransactionRequestData>(), It.IsAny<IReadOnlySet<int>?>()))
            .Throws(new ArgumentException("Lote inválido."));

        var service = CreateService(context, validator: validator);
        var request = BuildRequest("BULK-FAIL", 2);

        var response = await service.RegisterBulkAsync(request);

        Assert.Equal(0, response.TotalSucceeded);
        Assert.Equal(2, response.TotalFailed);
    }

    [Fact]
    public async Task RegisterBulkAsync_FailsItems_WhenDuplicateReferencesExistInRequestOrPersistence()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        SeedCatalog(context);
        context.AchTransactions.Add(new AchTransaction
        {
            Amount = 100,
            Reference = "BULK-DUP-0002",
            Type = TransactionTypeEnum.Credit,
            SourceAccountNumber = "1234567890",
            DestinationAccountNumber = "9876543210",
            CompanyName = "EMPRESA",
            CompanyIdentification = "900123456",
            CompanyEntryDescriptionId = 1,
            TransactionCode = "22",
            OriginatingDFI = "000010070",
            ReceivingDFI = "000010010",
            TraceNumber = "000010070000001",
            TraceSequenceNumber = 1,
            EffectiveEntryDate = DateTime.Today,
            SourceInstitutionId = 1,
            DestinationInstitutionId = 2,
            AchCycleId = "CYCLE-1",
            AchBatchId = 1
        });
        await context.SaveChangesAsync();

        var service = CreateService(context);
        var request = BuildRequest("BULK-DUP", 3);
        request.Transactions[2].Reference = request.Transactions[0].Reference; // duplicate in request

        var response = await service.RegisterBulkAsync(request);

        Assert.Equal(0, response.TotalSucceeded);
        Assert.Equal(3, response.TotalFailed);
        Assert.Contains(response.ItemResults, x => x.ErrorMessage!.Contains("duplicada dentro del mismo request", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(response.ItemResults, x => x.ErrorMessage!.Contains("ya existe en persistencia", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RegisterBulkAsync_Throws_WhenBatchExceedsMaxItems()
    {
        using var connection = CreateOpenConnection();
        using var context = CreateContext(connection);
        SeedCatalog(context);

        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Transactions:Bulk:MaxItems"] = "2",
            ["Transactions:Bulk:ChunkSize"] = "50"
        }).Build();

        var service = CreateService(context, configuration: configuration);
        var request = BuildRequest("BULK-LIMIT", 3);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.RegisterBulkAsync(request));
        Assert.Contains("máximo permitido", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static AchBulkTransactionService CreateService(
        AchDbContext context,
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
                Batch = new AchBatch { Id = 1, AchCycleId = "CYCLE-1", CompanyEntryDescription = "NOMINAS", EffectiveEntryDate = DateTime.Today },
                AchCycleId = "CYCLE-1",
                EffectiveEntryDate = DateTime.Today,
                OriginatingDfi = "000010070",
                ReceivingDfi = "000010010",
                CompanyName = "EMPRESA",
                CompanyIdentification = "900123456",
                CompanyEntryDescription = "NOMINAS",
                CompanyEntryDescriptionId = 1,
                ServiceClassCode = "200",
                SourceInstitutionId = 1,
                DestinationInstitutionId = 2
            });

        var nextId = 100;
        var persister = new Mock<ITransactionPersister>();
        persister.Setup(p => p.PersistAsync(It.IsAny<AchTransactionRequestData>(), It.IsAny<TransactionBatchContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                nextId++;
                var tx = new AchTransaction { Id = nextId };
                return new TransactionPersistResult { Transaction = tx, Batch = new AchBatch { Id = 1 } };
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

    private static BulkAchTransactionRequest BuildRequest(string prefix, int count)
    {
        var items = Enumerable.Range(1, count).Select(i => new BulkAchTransactionItemRequest
        {
            Amount = 1000 + i,
            Reference = $"{prefix}-{i:0000}",
            Type = TransactionTypeEnum.Credit,
            AccountType = AccountTypeEnum.Checking,
            IsPrenotification = false,
            DestinationInstitutionId = 2,
            SourceAccountNumber = $"123450{i:00000}",
            DestinationAccountNumber = $"987650{i:00000}",
            CompanyName = "EMPRESA",
            CompanyIdentification = "900123456",
            CompanyEntryDescriptionId = 1,
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

    private static void SeedCatalog(AchDbContext context)
    {
        context.CompanyEntryDescriptionCatalogs.Add(new CompanyEntryDescriptionCatalog
        {
            Id = 1,
            Term = "NOMINAS",
            Description = "Pago nomina",
            StandardEntryClassCode = "PPD",
            IsActive = true
        });

        context.AchCycles.Add(new AchCycle
        {
            Id = "CYCLE-1",
            CycleName = "Ciclo 1",
            ProcessingDate = DateTime.Today,
            StartTime = TimeSpan.Zero,
            EndTime = new TimeSpan(23, 59, 0),
            CutoffTime = new TimeSpan(23, 59, 0),
            ClearingHouseId = 1
        });

        context.AchBatches.Add(new AchBatch
        {
            Id = 1,
            AchCycleId = "CYCLE-1",
            CompanyName = "EMPRESA",
            CompanyIdentification = "900123456",
            CompanyEntryDescriptionId = 1,
            CompanyEntryDescription = "NOMINAS",
            OriginOrOdfi = "00001007",
            EffectiveEntryDate = DateTime.Today,
            BatchSequenceNumber = 1
        });

        context.FinancialInstitutions.AddRange(
            new FinancialInstitution { Id = 1, Name = "Origen", RoutingNumber = "00001", TransitCode = "007" , IsDefaultSource = true, Status = FinancialInstitutionStatus.Active },
            new FinancialInstitution { Id = 2, Name = "Destino", RoutingNumber = "00001", TransitCode = "001" , Status = FinancialInstitutionStatus.Active }
        );

        context.SaveChanges();
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
