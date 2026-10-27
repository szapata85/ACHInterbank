using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

/// <summary>
/// I04: proyección futura validada, sin enviar fuera de ventana ni modificar fecha valor.
/// Pruebas escritas: requieren dotnet para su ejecución.
/// </summary>
public sealed class AchCycleCalendarProjectionTests
{
    private static readonly DateTime Monday = new(2026, 10, 26);

    [Fact]
    public async Task CreatedSaturday_ProjectedMonday_UsesMondayValueDateAndCycle()
    {
        await using var scenario = await Scenario.CreateAsync(
            new DateTimeOffset(2026, 10, 24, 18, 0, 0, TimeSpan.Zero), Monday);
        var result = await scenario.Resolver.ResolveAsync(Scenario.Request);
        Assert.Equal("future-cycle", result.AchCycleId);
        Assert.Equal(Monday, result.EffectiveEntryDate);
        Assert.Equal(Monday, result.Batch.EffectiveEntryDate);
        scenario.Calendar.Verify(x => x.IsBusinessDayAsync(new DateOnly(2026, 10, 26), 1,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HolidayMonday_RoutedTuesday_UsesTuesdayValueDate()
    {
        var tuesday = new DateTime(2026, 10, 13);
        await using var scenario = await Scenario.CreateAsync(
            new DateTimeOffset(2026, 10, 12, 15, 0, 0, TimeSpan.Zero), tuesday);
        var result = await scenario.Resolver.ResolveAsync(Scenario.Request);
        Assert.Equal(tuesday, result.EffectiveEntryDate);
    }

    [Fact]
    public async Task SameDayBeforeCycleOpening_StillDenied()
    {
        await using var scenario = await Scenario.CreateAsync(
            new DateTimeOffset(2026, 10, 26, 11, 0, 0, TimeSpan.Zero), Monday);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => scenario.Resolver.ResolveAsync(Scenario.Request));
        Assert.Contains("aún no está abierto", exception.Message);
    }

    [Fact]
    public async Task FutureDateMarkedNonBusiness_DeniedWithoutCreatingBatch()
    {
        await using var scenario = await Scenario.CreateAsync(
            new DateTimeOffset(2026, 10, 24, 18, 0, 0, TimeSpan.Zero), Monday,
            isBusinessDay: false);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => scenario.Resolver.ResolveAsync(Scenario.Request));
        Assert.Contains("día no hábil", exception.Message);
        scenario.Batches.Verify(x => x.AddAsync(It.IsAny<AchBatch>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(AchCycleOperationalStatus.Closed)]
    [InlineData(AchCycleOperationalStatus.Cancelled)]
    public async Task FutureCycleClosedOrCancelled_Denied(AchCycleOperationalStatus status)
    {
        await using var scenario = await Scenario.CreateAsync(
            new DateTimeOffset(2026, 10, 24, 18, 0, 0, TimeSpan.Zero), Monday,
            status: status);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => scenario.Resolver.ResolveAsync(Scenario.Request));
        Assert.Contains("no está disponible", exception.Message);
    }

    [Fact]
    public async Task CycleCutoffExpired_StillDenied()
    {
        await using var scenario = await Scenario.CreateAsync(
            new DateTimeOffset(2026, 10, 26, 21, 0, 0, TimeSpan.Zero), Monday);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => scenario.Resolver.ResolveAsync(Scenario.Request));
        Assert.Contains("está cerrado", exception.Message);
    }

    [Fact]
    public async Task FutureProjection_DoesNotBypassTypePolicy()
    {
        await using var scenario = await Scenario.CreateAsync(
            new DateTimeOffset(2026, 10, 24, 18, 0, 0, TimeSpan.Zero), Monday,
            policyAllowed: false);
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => scenario.Resolver.ResolveAsync(Scenario.Request));
        Assert.Contains("TYPE_NOT_ALLOWED", exception.Message);
    }

    private sealed class Scenario : IAsyncDisposable
    {
        public static readonly AchTransactionRequestData Request = new()
        {
            Type = TransactionTypeEnum.Credit,
            AccountType = AccountTypeEnum.Checking,
            Amount = 1000m,
            DestinationInstitutionId = 2,
            CompanyName = "EMPRESA",
            CompanyIdentification = "900123456",
            CompanyEntryDescriptionId = 9,
            Reference = "I04-SATURDAY",
            SourceAccountNumber = "12345",
            DestinationAccountNumber = "98765"
        };

        private readonly AchDbContext _context;
        public BatchResolver Resolver { get; }
        public Mock<IOperationalCalendarService> Calendar { get; }
        public Mock<IAchBatchRepository> Batches { get; }

        private Scenario(AchDbContext context, BatchResolver resolver,
            Mock<IOperationalCalendarService> calendar, Mock<IAchBatchRepository> batches)
        {
            _context = context;
            Resolver = resolver;
            Calendar = calendar;
            Batches = batches;
        }

        public static async Task<Scenario> CreateAsync(
            DateTimeOffset now, DateTime processingDate,
            bool isBusinessDay = true,
            AchCycleOperationalStatus status = AchCycleOperationalStatus.Scheduled,
            bool policyAllowed = true)
        {
            var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>()
                .UseInMemoryDatabase($"ach-cr-i04-{Guid.NewGuid():N}").Options);
            context.ClearingHouses.Add(new ClearingHouse
            {
                Id = 1, Code = "ACHCOL", Name = "ACH Colombia", OriginCode = "000101006"
            });
            context.CatClearingHouses.Add(new CatClearingHouse
            {
                Code = "ACHCOL", Name = "ACH Colombia", IsActive = true
            });
            context.AchCycles.Add(new AchCycle
            {
                Id = "future-cycle", ClearingHouseId = 1, CycleName = "Ciclo 1",
                ProcessingDate = processingDate, StartTime = new TimeSpan(7, 0, 0),
                CutoffTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(9, 0, 0),
                OperationalStatus = status
            });
            context.CompanyEntryDescriptionCatalogs.Add(new CompanyEntryDescriptionCatalog
            {
                Id = 9, Term = "PAGOS PSE", StandardEntryClassCode = "PPD", IsActive = true
            });
            var origin = new FinancialInstitution
            {
                Id = 1, Name = "Origen", RoutingNumber = "00001", TransitCode = "283",
                IsDefaultSource = true, Status = FinancialInstitutionStatus.Active
            };
            var destination = new FinancialInstitution
            {
                Id = 2, Name = "Destino", RoutingNumber = "99999", TransitCode = "002",
                Status = FinancialInstitutionStatus.Active
            };
            origin.CalculateCheckDigit();
            destination.CalculateCheckDigit();
            context.FinancialInstitutions.AddRange(origin, destination);
            await context.SaveChangesAsync();

            var batches = new Mock<IAchBatchRepository>();
            batches.Setup(x => x.FindForTransactionAsync(It.IsAny<string>(),
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                    It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AchBatch?)null);
            batches.Setup(x => x.AddAsync(It.IsAny<AchBatch>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            batches.Setup(x => x.GetUpcomingCyclesAsync(It.IsAny<int>(), It.IsAny<DateTime>(),
                    It.IsAny<TimeSpan>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((IReadOnlyList<AchCycle>)Array.Empty<AchCycle>());

            var routing = new Mock<IRoutingStrategyService>();
            routing.Setup(x => x.ResolveClearingHouseForTransactionAsync(2,
                    It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("future-cycle");
            var calendar = new Mock<IOperationalCalendarService>();
            calendar.Setup(x => x.IsBusinessDayAsync(It.IsAny<DateOnly>(),
                    It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(isBusinessDay);
            var policy = new Mock<ICycleTransactionPolicy>();
            policy.Setup(x => x.EvaluateAsync(It.IsAny<CycleTransactionPolicyRequest>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CycleTransactionPolicyResult(policyAllowed,
                    policyAllowed ? "ALLOWED" : "TYPE_NOT_ALLOWED", "", "ACH", null, "MonetaryCredit"));
            var code = new Mock<IOrdinaryTransactionCodeAuthority>();
            code.Setup(x => x.ResolveAsync(It.IsAny<OrdinaryTransactionCodeAuthorityContext>(),
                    It.IsAny<OrdinaryTransactionCodeSemanticRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync("22");

            var resolver = new BatchResolver(context, batches.Object, routing.Object,
                new FixedTimeProvider(now), cycleTransactionPolicy: policy.Object,
                transactionCodeAuthority: code.Object, calendar: calendar.Object);
            return new Scenario(context, resolver, calendar, batches);
        }

        public ValueTask DisposeAsync() => _context.DisposeAsync();
    }

    private sealed class FixedTimeProvider(DateTimeOffset instant) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => instant;
    }
}
