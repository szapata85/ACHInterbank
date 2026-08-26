using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Tests.TestSupport;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class PrenotificationUatQueryAndBatchResolverTests
{
    [Fact]
    public async Task QueryByReference_ReturnsSpanishStatusAndDefaultSourceForPrenotification()
    {
        await using var context = CreateContext();
        var source = new FinancialInstitution
        {
            Id = 34,
            Name = "Cooperativa Financiera de Antioquia",
            RoutingNumber = "00001",
            TransitCode = "283",
            IsDefaultSource = true,
            Status = FinancialInstitutionStatus.Active
        };
        source.CalculateCheckDigit();
        var clearingHouse = new ClearingHouse { Id = 1, Name = "ACH Colombia", Code = "ACHCOL", OriginCode = "000101006" };
        var cycle = new AchCycle { Id = "cycle-ach", CycleName = "Ciclo 5", ProcessingDate = DateTime.UtcNow.Date, ClearingHouseId = 1, ClearingHouse = clearingHouse };
        context.FinancialInstitutions.Add(source);
        context.ClearingHouses.Add(clearingHouse);
        context.AchCycles.Add(cycle);
        context.AchTransactions.Add(new AchTransaction
        {
            Id = 300,
            Reference = "UAT-ACH-PRE-CFA-001",
            TransactionExternalId = "UAT-ACH-PRE-CFA-001",
            Type = TransactionTypeEnum.Debit,
            TransactionCode = "28",
            Amount = 0,
            IsPrenotification = true,
            State = AchTransferStateEnum.Pending,
            EffectiveEntryDate = DateTime.UtcNow.Date,
            SourceInstitutionId = 34,
            SourceInstitution = source,
            DestinationInstitutionId = 93,
            AchCycleId = "cycle-ach",
            AchCycle = cycle,
            SourceAccountNumber = "0000003101",
            DestinationAccountNumber = "0000003102",
            CompanyName = "UAT SINT",
            CompanyIdentification = "900003101"
        });
        await context.SaveChangesAsync();

        var holidays = new Mock<IBankHoliday>();
        holidays.Setup(x => x.GetHolidays(It.IsAny<int>())).Returns([]);
        var sut = new PrenotificationQueryService(context, holidays.Object);

        var result = await sut.GetByReferenceAsync("UAT-ACH-PRE-CFA-001");

        Assert.NotNull(result);
        Assert.Equal("28", result!.NachaCode);
        Assert.Equal("Pendiente", result.StatusDescription);
        Assert.True(result.SourceIsDefault);
        Assert.False(result.IsMatured);
        Assert.False(result.CanBeUsedForDebit);
        Assert.Contains("pendiente", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task BatchResolver_AllowsDebitPrenotificationInCycleFive()
    {
        await using var context = CreateContext();
        var clearingHouseConfig = new ClearingHouseConfig
        {
            Id = 10,
            ClearingHouseId = 1,
            TimeZoneId = "America/Bogota",
            PaymentRailCode = "ACH_COLOMBIA"
        };
        var clearingHouse = new ClearingHouse { Id = 1, Name = "ACH Colombia", Code = "ACHCOL", OriginCode = "000101006", ClearingHouseId = 10 };
        var cycleConfig = new ClearingHouseCycleConfig
        {
            ClearingHouseId = 1,
            PolicyVersion = "ACH-V35-TEST",
            CycleName = "Ciclo 5",
            StartTime = new TimeSpan(16, 1, 0),
            EndTime = new TimeSpan(18, 0, 0),
            CutoffTime = new TimeSpan(18, 0, 0),
            OutputReleaseTime = new TimeSpan(19, 0, 0),
            AllowsMonetaryDebit = false,
            EffectiveFrom = new DateTime(2026, 8, 4),
            EffectiveTo = new DateTime(2026, 8, 4),
            IsActive = true
        };
        var cycle = new AchCycle
        {
            Id = "cycle-five",
            CycleName = "Ciclo 5",
            ProcessingDate = new DateTime(2026, 8, 4),
            StartTime = new TimeSpan(16, 1, 0),
            EndTime = new TimeSpan(18, 0, 0),
            CutoffTime = new TimeSpan(18, 0, 0),
            OutputReleaseTime = new TimeSpan(19, 0, 0),
            ClearingHouseId = 1,
            ClearingHouse = clearingHouse,
            ClearingHouseCycleConfig = cycleConfig
        };
        context.ClearingHouseConfigs.Add(clearingHouseConfig);
        context.ClearingHouses.Add(clearingHouse);
        context.ClearingHouseCycleConfigs.Add(cycleConfig);
        context.AchCycles.Add(cycle);
        context.CompanyEntryDescriptionCatalogs.Add(new CompanyEntryDescriptionCatalog { Id = 1, Term = "PAGOS PSE", IsActive = true });
        var defaultSource = new FinancialInstitution { Id = 34, Name = "Cooperativa Financiera de Antioquia", RoutingNumber = "00001", TransitCode = "283", IsDefaultSource = true, Status = FinancialInstitutionStatus.Active };
        defaultSource.CalculateCheckDigit();
        var destination = new FinancialInstitution { Id = 93, Name = "Banco UAT Destino", RoutingNumber = "99999", TransitCode = "002", Status = FinancialInstitutionStatus.Active };
        destination.CalculateCheckDigit();
        context.FinancialInstitutions.AddRange(defaultSource, destination);
        await context.SaveChangesAsync();

        var batchRepository = new Mock<IAchBatchRepository>();
        batchRepository
            .Setup(x => x.FindForTransactionAsync("cycle-five", "UAT SINT", "900003101", "PAGOS PSE", cycle.ProcessingDate.Date, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AchBatch?)null);
        batchRepository
            .Setup(x => x.AddAsync(It.IsAny<AchBatch>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        batchRepository
            .Setup(x => x.GetUpcomingCyclesAsync(1, cycle.ProcessingDate, cycle.CutoffTime, 5, It.IsAny<CancellationToken>()))
            .ReturnsAsync([cycle, cycle, cycle, cycle, cycle]);

        var routing = new Mock<IRoutingStrategyService>();
        routing
            .Setup(x => x.ResolveClearingHouseForTransactionAsync(93, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("cycle-five");

        var fixedClock = new FixedTimeProvider(
            new DateTimeOffset(2026, 8, 4, 22, 0, 0, TimeSpan.Zero),
            TimeZoneInfo.Utc);
        var sut = new BatchResolver(context, batchRepository.Object, routing.Object, fixedClock);
        var request = new AchTransactionRequestData
        {
            Type = TransactionTypeEnum.Debit,
            AccountType = AccountTypeEnum.Checking,
            IsPrenotification = true,
            Amount = 0,
            DestinationInstitutionId = 93,
            SourceAccountNumber = "0000003101",
            DestinationAccountNumber = "0000003102",
            CompanyName = "UAT SINT",
            CompanyIdentification = "900003101",
            CompanyEntryDescriptionId = 1,
            Reference = "UAT-ACH-PRE-CFA-001"
        };

        var result = await sut.ResolveAsync(request);

        Assert.Equal("cycle-five", result.AchCycleId);
        Assert.Equal(34, result.SourceInstitutionId);
        Assert.Equal(93, result.DestinationInstitutionId);
    }

    private static AchDbContext CreateContext()
        => new(new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
