using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Tests;

public class NachaInboundSimulatorTests
{
    [Theory]
    [InlineData("ACHCOL", NachaInboundSimulationType.IncomingCredit)]
    [InlineData("CENIT", NachaInboundSimulationType.IncomingCredit)]
    [InlineData("ACHCOL", NachaInboundSimulationType.IncomingDebit)]
    [InlineData("CENIT", NachaInboundSimulationType.IncomingDebit)]
    public async Task Generate_ShouldCreateNonEmptyFile_WithoutAutoImport(string clearingHouseCode, NachaInboundSimulationType scenario)
    {
        await using var context = CreateContext();
        Seed(context);
        var output = CreateTempOutput();
        var service = CreateService(context, output);

        var response = await service.GenerateAsync(new GenerateNachaInboundSimulationRequest
        {
            ClearingHouseCode = clearingHouseCode,
            ScenarioType = scenario,
            EntriesCount = 2,
            Amount = 1000,
            ReferencePrefix = $"UAT-{clearingHouseCode}",
            BusinessDate = new DateOnly(2026, 5, 20),
            CycleCode = "Ciclo 3"
        }, "qa");

        Assert.True(response.FileSizeBytes > 0);
        Assert.True(response.GeneratedOnly);
        Assert.False(response.AutoImported);
        Assert.True(response.UploadRequired);
        Assert.False(response.ExternalTransmission);
        Assert.True(File.Exists(Path.Combine(output, clearingHouseCode == "CENIT" ? "cenit" : "ach-colombia", response.FileName)));
        Assert.Equal(0, await context.IncomingNachaFileIngestions.CountAsync());
        Assert.Equal(1, await context.NachaInboundSimulations.CountAsync());
    }

    [Fact]
    public async Task PrenotificationResponse_ShouldRequirePendingReference_AndNotChangeState()
    {
        await using var context = CreateContext();
        Seed(context);
        context.AchTransactions.Add(new AchTransaction
        {
            Id = 100,
            Reference = "UAT-PRE-001",
            TransactionExternalId = "UAT-PRE-001",
            IsPrenotification = true,
            Type = TransactionTypeEnum.Debit,
            Amount = 0,
            State = AchTransferStateEnum.Pending,
            AchCycleId = "ACH-CYCLE",
            AchBatchId = 1,
            CompanyEntryDescriptionId = 1,
            CompanyName = "CFA UAT",
            CompanyIdentification = "900000000",
            OriginatingDFI = "00001283",
            ReceivingDFI = "99999900",
            TraceNumber = "000012830000001",
            SourceInstitutionId = 1,
            DestinationInstitutionId = 2,
            SourceAccountNumber = "0000000001",
            DestinationAccountNumber = "0000000002",
            RecipientIdNumber = "900000001",
            EffectiveEntryDate = new DateTime(2026, 5, 20),
            StateChangedAtUtc = new DateTime(2026, 5, 20, 10, 0, 0, DateTimeKind.Utc)
        });
        await context.SaveChangesAsync();
        var service = CreateService(context, CreateTempOutput());

        var response = await service.GenerateAsync(new GenerateNachaInboundSimulationRequest
        {
            ClearingHouseCode = "ACHCOL",
            ScenarioType = NachaInboundSimulationType.IncomingPrenotificationResponse,
            ResponseMode = InboundResponseMode.Approved,
            PendingPrenotificationReferences = ["UAT-PRE-001"],
            BusinessDate = new DateOnly(2026, 5, 20),
            CycleCode = "Ciclo 5"
        }, "qa");

        var transaction = await context.AchTransactions.SingleAsync(x => x.Reference == "UAT-PRE-001");
        Assert.Equal(AchTransferStateEnum.Pending, transaction.State);
        Assert.True(response.GeneratedOnly);
    }

    [Fact]
    public async Task ReturnScenario_ShouldRequireReasonCode()
    {
        await using var context = CreateContext();
        Seed(context);
        var service = CreateService(context, CreateTempOutput());

        var preview = await service.PreviewAsync(new InboundSimulationEligibilityPreviewRequest
        {
            ClearingHouseCode = "ACHCOL",
            ScenarioType = NachaInboundSimulationType.IncomingDebitReturn,
            TransactionReferences = ["UAT-TX-001"],
            BusinessDate = new DateOnly(2026, 5, 20)
        });

        Assert.False(preview.Eligible);
        Assert.Equal("TRANSACTION_REASON_CODE_REQUIRED", preview.FunctionalCode);
    }

    [Fact]
    public async Task DisabledSimulator_ShouldBlockGeneration()
    {
        await using var context = CreateContext();
        Seed(context);
        var service = new NachaInboundSimulationService(context, Options.Create(new NachaInboundSimulatorOptions { Enabled = false, Mode = "Disabled" }));

        var preview = await service.PreviewAsync(new InboundSimulationEligibilityPreviewRequest
        {
            ClearingHouseCode = "ACHCOL",
            ScenarioType = NachaInboundSimulationType.IncomingCredit
        });

        Assert.False(preview.Eligible);
        Assert.Equal("SIMULATOR_DISABLED", preview.FunctionalCode);
    }

    [Fact]
    public async Task MenuSeed_ShouldExposeInboundSimulator()
    {
        await using var context = CreateContext();
        context.Database.EnsureCreated();

        var menuItem = await context.MenuItems.SingleAsync(x => x.Route == "/uat/nacha-inbound-simulator");
        Assert.Equal(MenuItemConfiguration.NachaInboundSimulatorId, menuItem.Id);
        Assert.True(menuItem.IsActive);
        Assert.True(await context.MenuItemRoles.AnyAsync(x => x.MenuItemId == menuItem.Id && x.RoleId == RoleConfiguration.AdminRoleId));
        Assert.True(await context.MenuItemRoles.AnyAsync(x => x.MenuItemId == menuItem.Id && x.RoleId == RoleConfiguration.OperatorRoleId));
    }

    [Fact]
    public async Task Cfa_ShouldRemainUniqueDefaultSource()
    {
        await using var context = CreateContext();
        Seed(context);

        var defaults = await context.FinancialInstitutions.Where(x => x.IsDefaultSource).ToListAsync();
        Assert.Single(defaults);
        Assert.Contains("Cooperativa Financiera de Antioquia", defaults[0].Name);
    }

    private static NachaInboundSimulationService CreateService(AchDbContext context, string output)
        => new(context, Options.Create(new NachaInboundSimulatorOptions
        {
            Enabled = true,
            Mode = "UAT",
            AllowAutoImport = false,
            AllowExternalTransmission = false,
            RequireSyntheticData = true,
            OutputDirectory = output,
            MaxEntriesPerSimulation = 10,
            AllowedClearingHouses = ["ACHCOL", "CENIT"]
        }));

    private static string CreateTempOutput()
    {
        var path = Path.Combine(Path.GetTempPath(), "ach-inbound-simulator-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static AchDbContext CreateContext()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .EnableSensitiveDataLogging()
            .Options;
        var context = new AchDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static void Seed(AchDbContext context)
    {
        if (context.ClearingHouses.Any())
        {
            return;
        }

        context.ClearingHouseConfigs.AddRange(
            new ClearingHouseConfig { Id = 1, ClearingHouseId = 1, HolidayStrategy = "Colombian" },
            new ClearingHouseConfig { Id = 2, ClearingHouseId = 2, HolidayStrategy = "Colombian" });
        context.ClearingHouses.AddRange(
            new ClearingHouse { Id = 1, Name = "ACH Colombia", Code = "ACHCOL", OriginCode = "ACH", ClearingHouseId = 1 },
            new ClearingHouse { Id = 2, Name = "CENIT", Code = "CENIT", OriginCode = "CEN", ClearingHouseId = 2 });
        context.AchCycles.Add(new AchCycle
        {
            Id = "ACH-CYCLE",
            CycleName = "Ciclo 5",
            ClearingHouseId = 1,
            ProcessingDate = new DateTime(2026, 5, 20),
            StartTime = TimeSpan.FromHours(12),
            EndTime = TimeSpan.FromHours(14),
            CutoffTime = TimeSpan.FromHours(14)
        });
        context.AchBatches.Add(new AchBatch
        {
            Id = 1,
            AchCycleId = "ACH-CYCLE",
            CompanyName = "CFA UAT",
            CompanyIdentification = "900000000",
            CompanyEntryDescription = "PRENOTE",
            CompanyEntryDescriptionId = 1,
            OriginOrOdfi = "00001283",
            EffectiveEntryDate = new DateTime(2026, 5, 20),
            BatchSequenceNumber = 1
        });

        var cfa = new FinancialInstitution { Id = 1, Name = "Cooperativa Financiera de Antioquia", RoutingNumber = "00001", TransitCode = "283", IsDefaultSource = true, Status = FinancialInstitutionStatus.Active };
        var ach = new FinancialInstitution { Id = 2, Name = "Banco UAT Externo ACH", RoutingNumber = "99999", TransitCode = "900", Status = FinancialInstitutionStatus.Active };
        var cenit = new FinancialInstitution { Id = 3, Name = "Banco UAT Externo CENIT", RoutingNumber = "99998", TransitCode = "900", Status = FinancialInstitutionStatus.Active };
        cfa.CalculateCheckDigit();
        ach.CalculateCheckDigit();
        cenit.CalculateCheckDigit();
        context.FinancialInstitutions.AddRange(cfa, ach, cenit);
        context.SaveChanges();
    }
}
