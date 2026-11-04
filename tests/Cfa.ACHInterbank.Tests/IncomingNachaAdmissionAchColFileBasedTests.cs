using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

/// <summary>
/// F2.2: admission checks for an ACH Colombia inbound physical OUT whose
/// externally declared cycle number is not an internal AchCycle.
/// These are unit/in-memory provider tests, not CFA certification.
/// </summary>
public sealed class IncomingNachaAdmissionAchColFileBasedTests
{
    [Theory]
    [InlineData("0001283.008.20260724.8.OUT", 'H', 8)]
    [InlineData("0001283.009.20260724.9.OUT", 'I', 9)]
    [InlineData("0001283.008.20260724.10.OUT", 'H', 10)]
    public async Task ValidAchColFile_IsAdmittedWithoutAnyAchCycle(string fileName, char identifier, int expectedExternalCycle)
    {
        await using var fixture = await Fixture.CreateAsync();
        var admission = await fixture.Validator().ValidateAsync(
            Request(fileName, Header(identifier), Batch("20260724")));

        Assert.Empty(await fixture.Context.AchCycles.ToListAsync());
        Assert.True(admission.IsAccepted);
        Assert.Null(admission.Issue);
        Assert.Equal(expectedExternalCycle, admission.CycleNumber);
        Assert.Equal(new DateOnly(2026, 7, 24), admission.FileNameDate);
        Assert.Equal(new DateOnly(2026, 7, 24), admission.EffectiveDate);
    }

    [Fact]
    public async Task FlagDisabled_DoesNotBypassLegacyCycleRequirements()
    {
        await using var fixture = await Fixture.CreateAsync();
        var result = await fixture.Validator(enabled: false).ValidateAsync(Request(
            "0001283.008.20260724.8.OUT", Header('H')));

        Assert.False(result.IsAccepted);
        Assert.Equal("CYCLE_NOT_RESOLVED", result.Issue?.Code);
    }

    [Theory]
    [InlineData(IncomingNachaCycleResolutionStatus.NoResuelto)]
    [InlineData(IncomingNachaCycleResolutionStatus.ResueltoConfirmado)]
    public async Task IncorrectResolutionStatus_DoesNotBypassCycleRequirements(IncomingNachaCycleResolutionStatus status)
    {
        await using var fixture = await Fixture.CreateAsync();
        var result = await fixture.Validator().ValidateAsync(Request(
            "0001283.008.20260724.8.OUT", Header('H'), status: status));

        Assert.False(result.IsAccepted);
        Assert.Equal("CYCLE_NOT_RESOLVED", result.Issue?.Code);
    }

    [Fact]
    public async Task AmbiguousResolution_IsNeverAdmitted()
    {
        await using var fixture = await Fixture.CreateAsync();
        var result = await fixture.Validator().ValidateAsync(Request(
            "0001283.008.20260724.8.OUT", Header('H'), ambiguous: true));

        Assert.False(result.IsAccepted);
        Assert.Equal("CYCLE_NOT_RESOLVED", result.Issue?.Code);
    }

    [Theory]
    [InlineData("0001283.008.20260724.8.OUT.env")]
    [InlineData("0001283.008.20260724.0.OUT")]
    [InlineData("0001283.008.20260724.8.OTHER")]
    public async Task NonCanonicalPlainFilename_IsRejected(string fileName)
    {
        await using var fixture = await Fixture.CreateAsync();
        var result = await fixture.Validator().ValidateAsync(Request(fileName, Header('H')));

        Assert.False(result.IsAccepted);
        Assert.Equal("ACHCOL_INBOUND_HEADER_CORRELATION_INVALID", result.Issue?.Code);
    }

    [Fact]
    public async Task HeaderIdentifierMismatch_RejectsRatherThanInferringCycle()
    {
        await using var fixture = await Fixture.CreateAsync();
        var result = await fixture.Validator().ValidateAsync(Request(
            "0001283.008.20260724.8.OUT", Header('I')));

        Assert.False(result.IsAccepted);
        Assert.Equal("ACHCOL_INBOUND_HEADER_CORRELATION_INVALID", result.Issue?.Code);
    }

    [Fact]
    public async Task HeaderDateMismatch_IsRejectedEvenWhenResolutionClaimsCorrectDate()
    {
        await using var fixture = await Fixture.CreateAsync();
        var result = await fixture.Validator().ValidateAsync(Request(
            "0001283.008.20260724.8.OUT", Header('H', date: "20260723")));

        Assert.False(result.IsAccepted);
        Assert.Equal("ACHCOL_INBOUND_HEADER_CORRELATION_INVALID", result.Issue?.Code);
    }

    [Fact]
    public async Task InvalidHeaderTime_IsRejected()
    {
        await using var fixture = await Fixture.CreateAsync();
        var result = await fixture.Validator().ValidateAsync(Request(
            "0001283.008.20260724.8.OUT", Header('H', time: "2460")));

        Assert.False(result.IsAccepted);
        Assert.Equal("ACHCOL_HEADER_TIME_INVALID", result.Issue?.Code);
    }

    [Theory]
    [InlineData("20260723", "EFFECTIVE_DATE_MISMATCH")]
    [InlineData("20261345", "ACHCOL_EFFECTIVE_DATE_INVALID")]
    public async Task BatchEffectiveDate_IsValidatedInEightDigitAchColFormat(string effectiveDate, string expectedCode)
    {
        await using var fixture = await Fixture.CreateAsync();
        var result = await fixture.Validator().ValidateAsync(Request(
            "0001283.008.20260724.8.OUT", Header('H'), Batch(effectiveDate)));

        Assert.False(result.IsAccepted);
        Assert.Equal(expectedCode, result.Issue?.Code);
    }

    [Fact]
    public async Task BankHoliday_IsStillRejectedWithoutCycle()
    {
        await using var fixture = await Fixture.CreateAsync();
        fixture.Context.BankHolidays.Add(new BankHolidayModel
        {
            Date = new DateOnly(2026, 7, 24),
            Description = "Festivo de prueba"
        });
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Validator().ValidateAsync(Request(
            "0001283.008.20260724.8.OUT", Header('H')));

        Assert.False(result.IsAccepted);
        Assert.Equal("NON_BUSINESS_DAY", result.Issue?.Code);
    }

    [Theory]
    [InlineData(false, "ACHCOL")]
    [InlineData(true, "CENIT")]
    public async Task InactiveOrWrongClearingHouse_IsNeverGrantedAchColException(bool active, string code)
    {
        await using var fixture = await Fixture.CreateAsync();
        var house = await fixture.Context.ClearingHouses.SingleAsync();
        house.IsActive = active;
        house.Code = code;
        await fixture.Context.SaveChangesAsync();

        var result = await fixture.Validator().ValidateAsync(Request(
            "0001283.008.20260724.8.OUT", Header('H')));

        Assert.False(result.IsAccepted);
        Assert.Equal("ACHCOL_CLEARING_HOUSE_NOT_AUTHORIZED", result.Issue?.Code);
    }

    [Fact]
    public async Task CycleIdPresentedWithCycleIndependentStatus_DoesNotAllowBypass()
    {
        await using var fixture = await Fixture.CreateAsync();
        var result = await fixture.Validator().ValidateAsync(Request(
            "0001283.008.20260724.8.OUT", Header('H'), cycleId: "FAKE-CYCLE-8"));

        Assert.False(result.IsAccepted);
        Assert.Equal("CYCLE_NOT_RESOLVED", result.Issue?.Code);
    }

    private static IncomingNachaAdmissionRequest Request(
        string fileName,
        string header,
        string? batch = null,
        IncomingNachaCycleResolutionStatus status = IncomingNachaCycleResolutionStatus.ResueltoSinCiclo,
        bool ambiguous = false,
        string? cycleId = null)
    {
        IReadOnlyList<string> records = batch is null ? [header] : [header, batch];
        return new IncomingNachaAdmissionRequest(fileName, records, new IncomingNachaCycleResolutionResult
        {
            IsResolved = true,
            IsAmbiguous = ambiguous,
            ClearingHouseId = 1,
            DetectedClearingHouseId = 1,
            OperationalDate = new DateTime(2026, 7, 24),
            AchCycleId = cycleId,
            Status = status,
            ResolutionMode = "AchColFileBased"
        }, false);
    }

    private static string Header(char identifier, string date = "20260724", string time = "0900")
    {
        var data = Enumerable.Repeat(' ', 106).ToArray();
        data[0] = '1';
        "DESTINO001".CopyTo(0, data, 3, 10);
        "ORIGEN0001".CopyTo(0, data, 13, 10);
        date.CopyTo(0, data, 23, 8);
        time.CopyTo(0, data, 31, 4);
        data[35] = identifier;
        return new string(data);
    }

    private static string Batch(string effectiveDate)
    {
        var data = Enumerable.Repeat(' ', 106).ToArray();
        data[0] = '5';
        effectiveDate.CopyTo(0, data, 71, 8);
        return new string(data);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        public AchDbContext Context { get; }

        private Fixture(SqliteConnection connection, AchDbContext context)
        {
            _connection = connection;
            Context = context;
        }

        public IncomingNachaAdmissionValidator Validator(bool enabled = true)
        {
            var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["AchCol:AllowCycleIndependentInboundDispatch"] = enabled ? "true" : "false"
            }).Build();
            return new IncomingNachaAdmissionValidator(Context, configuration: config);
        }

        public static async Task<Fixture> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options;
            var context = new AchDbContext(options);
            await context.Database.EnsureCreatedAsync();
            var config = new ClearingHouseConfig { Id = 1, ClearingHouseId = 1, TimeZoneId = "America/Bogota" };
            context.AddRange(config, new ClearingHouse
            {
                Id = 1,
                ClearingHouseId = 1,
                ClearingHouseConfig = config,
                Name = "ACH Colombia",
                Code = "ACHCOL",
                OriginCode = "ORIGEN0001",
                IsActive = true
            });
            await context.SaveChangesAsync();
            return new Fixture(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
