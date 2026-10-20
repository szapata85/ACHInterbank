using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class IncomingNachaTraceabilityCoreTests
{
    [Fact]
    public async Task Admission_OpenCycleAndMatchingDates_IsAccepted()
    {
        await using var fixture = await Fixture.CreateAsync(AchCycleOperationalStatus.Open);
        var sut = new IncomingNachaAdmissionValidator(fixture.Context, fixture.Clock);

        var result = await sut.ValidateAsync(Request("0001283.001.20260724.1", fixture.Cycle.Id));

        Assert.True(result.IsAccepted);
        Assert.Equal(new DateOnly(2026, 7, 24), result.Header!.FileCreationDate);
        Assert.Null(result.Issue);
    }

    [Fact]
    public async Task Admission_FileNameAndHeaderDateDiffer_ReturnsHumanizedIssue()
    {
        await using var fixture = await Fixture.CreateAsync(AchCycleOperationalStatus.Open);
        var sut = new IncomingNachaAdmissionValidator(fixture.Context, fixture.Clock);

        var result = await sut.ValidateAsync(Request("0001283.001.20260723.1", fixture.Cycle.Id));

        Assert.False(result.IsAccepted);
        Assert.Equal("HEADER_DATE_MISMATCH", result.Issue!.Code);
        Assert.Contains("23/07/2026", result.Issue.Message);
        Assert.Contains("24/07/2026", result.Issue.Message);
        Assert.False(string.IsNullOrWhiteSpace(result.Issue.SuggestedAction));
        Assert.Equal("Functional", result.Issue.ErrorType);
    }

    [Fact]
    public async Task Admission_ClosedCycle_RejectsBeforeParser()
    {
        await using var fixture = await Fixture.CreateAsync(AchCycleOperationalStatus.Closed);
        var sut = new IncomingNachaAdmissionValidator(fixture.Context, fixture.Clock);

        var result = await sut.ValidateAsync(Request("0001283.001.20260724.1", fixture.Cycle.Id));

        Assert.False(result.IsAccepted);
        Assert.Equal("CYCLE_ALREADY_CLOSED", result.Issue!.Code);
        Assert.Contains("cerrado", result.Issue.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Admission_Holiday_RejectsWithAction()
    {
        await using var fixture = await Fixture.CreateAsync(AchCycleOperationalStatus.Open);
        fixture.Context.BankHolidays.Add(new BankHolidayModel
        {
            Date = new DateOnly(2026, 7, 24),
            Description = "Festivo de prueba"
        });
        await fixture.Context.SaveChangesAsync();
        var sut = new IncomingNachaAdmissionValidator(fixture.Context, fixture.Clock);

        var result = await sut.ValidateAsync(Request("0001283.001.20260724.1", fixture.Cycle.Id));

        Assert.False(result.IsAccepted);
        Assert.Equal("NON_BUSINESS_DAY", result.Issue!.Code);
        Assert.Contains("día hábil", result.Issue.Title, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task AchResultResolver_UsesClearingHouseFlowAndCatalogOutcome()
    {
        await using var fixture = await Fixture.CreateAsync(AchCycleOperationalStatus.Open);
        fixture.Context.AchReturnCodes.AddRange(
            new AchReturnCode
            {
                ClearingHouseId = fixture.ClearingHouse.Id,
                Code = "R96",
                FlowType = AchReturnFlowType.Any,
                Description = "Procesada exitosamente",
                AppliesToCredit = true,
                BusinessOutcome = IncomingNachaBusinessOutcome.Successful,
                EffectiveFrom = new DateTime(2020, 1, 1),
                IsActive = true,
                RegulatorySource = "CENIT"
            },
            new AchReturnCode
            {
                ClearingHouseId = fixture.ClearingHouse.Id,
                Code = "R16",
                FlowType = AchReturnFlowType.Any,
                Description = "Cuenta bloqueada",
                AppliesToCredit = true,
                BusinessOutcome = IncomingNachaBusinessOutcome.Returned,
                EffectiveFrom = new DateTime(2020, 1, 1),
                IsActive = true,
                RegulatorySource = "CENIT"
            },
            new AchReturnCode
            {
                ClearingHouseId = fixture.ClearingHouse.Id,
                Code = "R17",
                FlowType = AchReturnFlowType.Any,
                Description = "Identificación inconsistente",
                AppliesToCredit = true,
                BusinessOutcome = IncomingNachaBusinessOutcome.Returned,
                EffectiveFrom = new DateTime(2020, 1, 1),
                IsActive = true,
                RegulatorySource = "CENIT"
            },
            new AchReturnCode
            {
                ClearingHouseId = fixture.ClearingHouse.Id,
                Code = "R18",
                FlowType = AchReturnFlowType.Any,
                Description = "Código inactivo",
                AppliesToCredit = true,
                EffectiveFrom = new DateTime(2020, 1, 1),
                IsActive = false,
                RegulatorySource = "CENIT"
            });
        await fixture.Context.SaveChangesAsync();
        var sut = new IncomingNachaAchResultResolver(fixture.Context);

        var success = await sut.ResolveAsync(ResultRequest(fixture.ClearingHouse.Id, "R96"));
        var returned = await sut.ResolveAsync(ResultRequest(fixture.ClearingHouse.Id, "R16"));
        var returnedR17 = await sut.ResolveAsync(ResultRequest(fixture.ClearingHouse.Id, "R17"));
        var inactive = await sut.ResolveAsync(ResultRequest(fixture.ClearingHouse.Id, "R18"));
        var wrongHouse = await sut.ResolveAsync(ResultRequest(fixture.ClearingHouse.Id + 100, "R16"));

        Assert.True(success.IsResolved);
        Assert.Equal(IncomingNachaBusinessOutcome.Successful, success.BusinessOutcome);
        Assert.True(returned.IsResolved);
        Assert.Equal(IncomingNachaBusinessOutcome.Returned, returned.BusinessOutcome);
        Assert.True(returnedR17.IsResolved);
        Assert.Equal("Identificación inconsistente", returnedR17.ResultDescription);
        Assert.False(inactive.IsResolved);
        Assert.False(wrongHouse.IsResolved);
        Assert.Equal("ACH_RESULT_CODE_NOT_FOUND", wrongHouse.ResolutionCode);
    }

    [Fact]
    public async Task AuditClock_CreatedAtIsImmutableAndUpdatedAtChangesOnlyForRealModification()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var clock = new MutableClock(new DateTimeOffset(2026, 7, 24, 12, 0, 0, TimeSpan.Zero));
        var options = new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options;
        await using var context = new AchDbContext(options, timeProvider: clock);
        await context.Database.EnsureCreatedAsync();
        var header = new NachaHeader { NachaID = "audit-header", CycleNumber = 1 };
        context.NachaHeaders.Add(header);
        await context.SaveChangesAsync();
        var createdAt = header.CreatedAt;
        var firstUpdatedAt = header.UpdatedAt;

        clock.UtcNow = clock.UtcNow.AddMinutes(5);
        header.CreatedAt = clock.UtcNow.AddYears(-10);
        header.ReferenceCode = "cambio-real";
        await context.SaveChangesAsync();

        Assert.Equal(createdAt, header.CreatedAt);
        Assert.Equal(clock.UtcNow, header.UpdatedAt);
        Assert.True(header.UpdatedAt > firstUpdatedAt);

        clock.UtcNow = clock.UtcNow.AddMinutes(5);
        context.Entry(header).Property(x => x.UpdatedAt).IsModified = true;
        await context.SaveChangesAsync();
        Assert.Equal(clock.UtcNow.AddMinutes(-5), header.UpdatedAt);
        await connection.DisposeAsync();
    }

    [Fact]
    public void ParsedModels_AreNotEfEntities()
    {
        var parsedTypes = new[]
        {
            typeof(ParsedNachaHeader), typeof(ParsedBatchHeader), typeof(ParsedEntryDetail),
            typeof(ParsedAddendaRecord), typeof(ParsedBatchControl), typeof(ParsedFileControl)
        };

        Assert.All(parsedTypes, type =>
        {
            Assert.False(typeof(Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Services.IAuditableEntity).IsAssignableFrom(type));
            Assert.DoesNotContain(type.GetProperties(), property =>
                property.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.KeyAttribute), true).Length > 0);
        });
    }

    [Fact]
    public void IngestionStateMachine_RejectsIncoherentTransitions()
    {
        var ingestion = new IncomingNachaFileIngestion { Stage = IncomingNachaIngestionStage.Received };

        Assert.False(IncomingNachaStageTransitions.CanMove(
            IncomingNachaIngestionStage.Received,
            IncomingNachaIngestionStage.Persisted));
        var error = Assert.Throws<InvalidOperationException>(() =>
            IncomingNachaStageTransitions.MoveTo(ingestion, IncomingNachaIngestionStage.Persisted));

        Assert.Contains("Received->Persisted", error.Message);
        Assert.Equal(IncomingNachaIngestionStage.Received, ingestion.Stage);
    }

    private static IncomingNachaAdmissionRequest Request(string fileName, string cycleId)
        => new(fileName, [HeaderRecord()], new IncomingNachaCycleResolutionResult
        {
            IsResolved = true,
            ClearingHouseId = 1,
            DetectedClearingHouseId = 1,
            OperationalDate = new DateTime(2026, 7, 24),
            AchCycleId = cycleId,
            Status = IncomingNachaCycleResolutionStatus.ResueltoConfirmado
        }, false);

    private static IncomingNachaAchResultRequest ResultRequest(int clearingHouseId, string code)
        => new(clearingHouseId, code, AchReturnFlowType.Any, false, true, false, false, new DateTime(2026, 7, 24));

    private static string HeaderRecord()
    {
        var value = Enumerable.Repeat(' ', 106).ToArray();
        value[0] = '1';
        "DESTINO001".CopyTo(0, value, 3, 10);
        "ORIGEN0001".CopyTo(0, value, 13, 10);
        "20260724".CopyTo(0, value, 23, 8);
        "0900".CopyTo(0, value, 31, 4);
        "FILE0001".CopyTo(0, value, 94, 8);
        return new string(value);
    }

    private sealed class MutableClock(DateTimeOffset utcNow) : TimeProvider
    {
        public DateTimeOffset UtcNow { get; set; } = utcNow;
        public override DateTimeOffset GetUtcNow() => UtcNow;
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        public AchDbContext Context { get; }
        public ClearingHouse ClearingHouse { get; }
        public AchCycle Cycle { get; }
        public TimeProvider Clock { get; }

        private Fixture(SqliteConnection connection, AchDbContext context, ClearingHouse clearingHouse, AchCycle cycle, TimeProvider clock)
        {
            _connection = connection;
            Context = context;
            ClearingHouse = clearingHouse;
            Cycle = cycle;
            Clock = clock;
        }

        public static async Task<Fixture> CreateAsync(AchCycleOperationalStatus status)
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var timeZone = TimeZoneInfo.CreateCustomTimeZone("America/Bogota-Test", TimeSpan.FromHours(-5), "Bogotá", "Bogotá");
            var clock = new MutableClock(new DateTimeOffset(2026, 7, 24, 15, 0, 0, TimeSpan.Zero));
            var options = new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options;
            var context = new AchDbContext(options, timeProvider: clock);
            await context.Database.EnsureCreatedAsync();
            var config = new ClearingHouseConfig { Id = 1, ClearingHouseId = 1, TimeZoneId = timeZone.Id };
            var clearingHouse = new ClearingHouse
            {
                Id = 1,
                Name = "CENIT",
                Code = "CENIT",
                OriginCode = "ORIGEN0001",
                ClearingHouseId = config.Id,
                ClearingHouseConfig = config
            };
            var cycle = new AchCycle
            {
                Id = "CENIT-20260724-1",
                CycleName = "Ciclo 1",
                ProcessingDate = new DateTime(2026, 7, 24),
                StartTime = new TimeSpan(8, 0, 0),
                CutoffTime = new TimeSpan(16, 0, 0),
                EndTime = new TimeSpan(17, 0, 0),
                ClearingHouseId = clearingHouse.Id,
                OperationalStatus = status
            };
            context.AddRange(config, clearingHouse, cycle);
            await context.SaveChangesAsync();
            return new Fixture(connection, context, clearingHouse, cycle, clock);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
