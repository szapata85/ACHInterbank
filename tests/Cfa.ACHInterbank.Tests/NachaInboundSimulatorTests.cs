using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Configuration;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.Configuration;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging.Abstractions;

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
            OriginFinancialInstitutionId = clearingHouseCode == "CENIT" ? 3 : 2,
            EntriesCount = 1,
            Amount = 1000,
            ReferencePrefix = $"UAT-{clearingHouseCode}",
            BusinessDate = new DateOnly(2026, 5, 20),
            CycleCode = clearingHouseCode == "CENIT" ? "CENIT-CYCLE" : "ACH-CYCLE"
        }, "qa");

        Assert.True(response.FileSizeBytes > 0);
        Assert.True(response.GeneratedOnly);
        Assert.False(response.AutoImported);
        Assert.True(response.UploadRequired);
        Assert.False(response.ExternalTransmission);
        Assert.Single(Directory.GetFiles(output, response.FileName, SearchOption.AllDirectories));
        if (clearingHouseCode == "CENIT")
        {
            Assert.Matches(@"^\d{7}\.\d{3}\.\d{8}\.\d+$", response.FileName);
        }
        else
        {
            Assert.Matches(@"^\d{7}\.\d{3}\.\d{8}\.\d+\.OUT$", response.FileName);
        }
        Assert.Equal(0, await context.IncomingNachaFileIngestions.CountAsync());
        Assert.Equal(1, await context.NachaInboundSimulations.CountAsync());
        var storedEntry = await context.NachaInboundSimulationEntries.SingleAsync();
        Assert.NotNull(storedEntry.TransactionId);
        Assert.False(storedEntry.IsSynthetic);
        var evidence = await service.GetEvidenceAsync(response.Id);
        Assert.NotNull(evidence);
        Assert.False(evidence!.OriginIsDefaultSource);
        Assert.True(evidence.DestinationIsDefaultSource);
        Assert.Equal(1, evidence.DestinationFinancialInstitutionId);
    }

    [Fact]
    public async Task PrenotificationResponse_ShouldFailClosedWithoutPublishedDifferentialProfile_AndNotChangeState()
    {
        await using var context = CreateContext();
        Seed(context);
        context.AchTransactions.Add(new AchTransaction
        {
            Id = 100,
            Reference = "UAT-PRE-001",
            TransactionExternalId = "UAT-PRE-001",
            IsPrenotification = true,
            Type = TransactionTypeEnum.Prenotification,
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

        var preview = await service.PreviewAsync(new InboundSimulationEligibilityPreviewRequest
        {
            SimulationMode = NachaSimulationMode.DifferentialResponses,
            ClearingHouseCode = "ACHCOL",
            ScenarioType = NachaInboundSimulationType.IncomingPrenotificationResponse,
            OriginFinancialInstitutionId = 2,
            ResponseMode = InboundResponseMode.Approved,
            PendingPrenotificationReferences = ["UAT-PRE-001"],
            BusinessDate = new DateOnly(2026, 5, 20),
            CycleCode = "ACH-CYCLE"
        });

        var transaction = await context.AchTransactions.SingleAsync(x => x.Reference == "UAT-PRE-001");
        Assert.Equal(AchTransferStateEnum.Pending, transaction.State);
        Assert.False(preview.Eligible);
        Assert.Equal("DIFFERENTIAL_PROFILE_NOT_PUBLISHED", preview.FunctionalCode);
    }

    [Fact]
    public async Task ReturnScenario_ShouldRequireReasonCode()
    {
        await using var context = CreateContext();
        Seed(context);
        var service = CreateService(context, CreateTempOutput());

        var preview = await service.PreviewAsync(new InboundSimulationEligibilityPreviewRequest
        {
            SimulationMode = NachaSimulationMode.DifferentialResponses,
            ClearingHouseCode = "ACHCOL",
            ScenarioType = NachaInboundSimulationType.IncomingDebitReturn,
            OriginFinancialInstitutionId = 2,
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
            OriginFinancialInstitutionId = 2,
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

    [Fact]
    public async Task Generate_ShouldRejectMissingOriginFinancialInstitution()
    {
        await using var context = CreateContext();
        Seed(context);
        var service = CreateService(context, CreateTempOutput());

        var preview = await service.PreviewAsync(new InboundSimulationEligibilityPreviewRequest
        {
            ClearingHouseCode = "ACHCOL",
            ScenarioType = NachaInboundSimulationType.IncomingCredit
        });

        Assert.False(preview.Eligible);
        Assert.Equal("ORIGIN_FINANCIAL_INSTITUTION_REQUIRED", preview.FunctionalCode);
    }

    [Fact]
    public async Task Generate_ShouldRejectDefaultSourceAsOrigin()
    {
        await using var context = CreateContext();
        Seed(context);
        var service = CreateService(context, CreateTempOutput());

        var preview = await service.PreviewAsync(new InboundSimulationEligibilityPreviewRequest
        {
            ClearingHouseCode = "ACHCOL",
            OriginFinancialInstitutionId = 1,
            ScenarioType = NachaInboundSimulationType.IncomingCredit
        });

        Assert.False(preview.Eligible);
        Assert.Equal("ORIGIN_FINANCIAL_INSTITUTION_CANNOT_BE_DEFAULT_SOURCE", preview.FunctionalCode);
    }

    [Fact]
    public async Task Generate_ShouldRejectMultipleDefaultDestinations()
    {
        await using var context = CreateContext();
        Seed(context);
        var duplicate = new FinancialInstitution
        {
            Id = 4,
            Name = "CFA Duplicada UAT",
            RoutingNumber = "00002",
            TransitCode = "284",
            IsDefaultSource = true,
            Status = FinancialInstitutionStatus.Active
        };
        duplicate.CalculateCheckDigit();
        context.FinancialInstitutions.Add(duplicate);
        await context.SaveChangesAsync();
        var service = CreateService(context, CreateTempOutput());

        var preview = await service.PreviewAsync(new InboundSimulationEligibilityPreviewRequest
        {
            ClearingHouseCode = "ACHCOL",
            OriginFinancialInstitutionId = 2,
            ScenarioType = NachaInboundSimulationType.IncomingCredit
        });

        Assert.False(preview.Eligible);
        Assert.Equal("MULTIPLE_DEFAULT_DESTINATION_FINANCIAL_INSTITUTIONS", preview.FunctionalCode);
    }

    [Fact]
    public async Task AvailableCycles_ShouldOnlyReturnActiveCurrentCyclesWithEligibleTransactions()
    {
        await using var context = CreateContext();
        Seed(context);
        var service = CreateService(context, CreateTempOutput());

        var cycles = await service.ListAvailableCyclesAsync(new AvailableInboundCycleQuery
        {
            ClearingHouseCode = "ACHCOL",
            ProcessingDate = new DateOnly(2026, 5, 20),
            ScenarioType = NachaInboundSimulationType.IncomingCredit
        });

        var cycle = Assert.Single(cycles);
        Assert.Equal("ACH-CYCLE", cycle.CycleCode);
        Assert.Equal("ACHCOL", cycle.ClearingHouseCode);
        Assert.Equal(1, cycle.TransactionCount);
        Assert.Equal("Disponible", cycle.Status);

        var config = await context.ClearingHouseCycleConfigs.SingleAsync(x => x.Id == 1);
        config.IsActive = false;
        await context.SaveChangesAsync();

        Assert.Empty(await service.ListAvailableCyclesAsync(new AvailableInboundCycleQuery
        {
            ClearingHouseCode = "ACHCOL",
            ProcessingDate = new DateOnly(2026, 5, 20),
            ScenarioType = NachaInboundSimulationType.IncomingCredit
        }));
    }

    [Fact]
    public async Task Preview_ShouldRejectCycleWithoutEligibleTransactionsOrManipulatedCode()
    {
        await using var context = CreateContext();
        Seed(context);
        var service = CreateService(context, CreateTempOutput());

        var transaction = await context.AchTransactions.SingleAsync(x =>
            x.AchCycleId == "ACH-CYCLE" && x.Type == TransactionTypeEnum.Credit);
        transaction.State = AchTransferStateEnum.ReturnedByOperator;
        await context.SaveChangesAsync();

        var unavailable = await service.PreviewAsync(ValidPreview("ACH-CYCLE"));
        var manipulated = await service.PreviewAsync(ValidPreview("CICLO-INVENTADO"));

        Assert.False(unavailable.Eligible);
        Assert.Equal("CYCLE_NOT_AVAILABLE", unavailable.FunctionalCode);
        Assert.False(manipulated.Eligible);
        Assert.Equal("CYCLE_NOT_AVAILABLE", manipulated.FunctionalCode);
    }

    [Fact]
    public async Task AvailableCycles_ShouldFilterByClearingHouseDateAndOrderByWindow()
    {
        await using var context = CreateContext();
        Seed(context);
        var service = CreateService(context, CreateTempOutput());

        var achCycles = await service.ListAvailableCyclesAsync(new AvailableInboundCycleQuery
        {
            ClearingHouseCode = "ACHCOL",
            ProcessingDate = new DateOnly(2026, 5, 20),
            ScenarioType = NachaInboundSimulationType.IncomingDebit
        });
        var cenitCycles = await service.ListAvailableCyclesAsync(new AvailableInboundCycleQuery
        {
            ClearingHouseCode = "CENIT",
            ProcessingDate = new DateOnly(2026, 5, 20),
            ScenarioType = NachaInboundSimulationType.IncomingDebit
        });
        var future = await service.ListAvailableCyclesAsync(new AvailableInboundCycleQuery
        {
            ClearingHouseCode = "ACHCOL",
            ProcessingDate = new DateOnly(2026, 5, 21),
            ScenarioType = NachaInboundSimulationType.IncomingDebit
        });

        Assert.Equal(["ACH-CYCLE"], achCycles.Select(x => x.CycleCode));
        Assert.Equal(["CENIT-CYCLE"], cenitCycles.Select(x => x.CycleCode));
        Assert.Empty(future);
    }

    [Fact]
    public async Task RepairedHistoricalCycle_ShouldAppearWithoutManualEdit()
    {
        await using var context = CreateContext();
        Seed(context);
        var cycle = await context.AchCycles.SingleAsync(x => x.Id == "ACH-CYCLE");
        cycle.ClearingHouseCycleConfigId = null;
        await context.SaveChangesAsync();
        var simulator = CreateService(context, CreateTempOutput());
        Assert.Empty(await simulator.ListAvailableCyclesAsync(new AvailableInboundCycleQuery
        {
            ClearingHouseCode = "ACHCOL",
            ProcessingDate = new DateOnly(2026, 5, 20),
            ScenarioType = NachaInboundSimulationType.IncomingCredit
        }));

        MapperBootstrapper.Configure(NullLoggerFactory.Instance);
        var repair = await new AchCycleAppService(context, MapperBootstrapper.Instance)
            .RepairConfigurationLinksAsync();

        Assert.True(repair.Completed);
        Assert.Equal(1, repair.RepairedCount);
        Assert.Single(await simulator.ListAvailableCyclesAsync(new AvailableInboundCycleQuery
        {
            ClearingHouseCode = "ACHCOL",
            ProcessingDate = new DateOnly(2026, 5, 20),
            ScenarioType = NachaInboundSimulationType.IncomingCredit
        }));
    }

    [Fact]
    public async Task ManipulatedConfigurationLink_ShouldBeExcludedByListPreviewAndGenerate()
    {
        await using var context = CreateContext();
        Seed(context);
        var cycle = await context.AchCycles.SingleAsync(x => x.Id == "ACH-CYCLE");
        cycle.StartTime = TimeSpan.FromHours(11);
        await context.SaveChangesAsync();
        var service = CreateService(context, CreateTempOutput());

        Assert.Empty(await service.ListAvailableCyclesAsync(new AvailableInboundCycleQuery
        {
            ClearingHouseCode = "ACHCOL",
            ProcessingDate = new DateOnly(2026, 5, 20),
            ScenarioType = NachaInboundSimulationType.IncomingCredit
        }));
        var preview = await service.PreviewAsync(ValidPreview("ACH-CYCLE"));
        Assert.False(preview.Eligible);
        Assert.Equal("CYCLE_NOT_AVAILABLE", preview.FunctionalCode);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateAsync(
            new GenerateNachaInboundSimulationRequest
            {
                ClearingHouseCode = "ACHCOL",
                ScenarioType = NachaInboundSimulationType.IncomingCredit,
                OriginFinancialInstitutionId = 2,
                EntriesCount = 1,
                Amount = 1000,
                ReferencePrefix = "MANIPULATED",
                BusinessDate = new DateOnly(2026, 5, 20),
                CycleCode = "ACH-CYCLE"
            }, "qa"));
        Assert.Contains("CYCLE_NOT_AVAILABLE", exception.Message);
    }

    private static InboundSimulationEligibilityPreviewRequest ValidPreview(string cycleCode) => new()
    {
        ClearingHouseCode = "ACHCOL",
        ScenarioType = NachaInboundSimulationType.IncomingCredit,
        OriginFinancialInstitutionId = 2,
        BusinessDate = new DateOnly(2026, 5, 20),
        CycleCode = cycleCode
    };

    private static NachaInboundSimulationService CreateService(AchDbContext context, string output)
        => new(context, Options.Create(new NachaInboundSimulatorOptions
        {
            Enabled = true,
            Mode = "UAT",
            AllowAutoImport = false,
            AllowExternalTransmission = false,
            RequireSyntheticData = true,
            DifferentialResponsesEnabled = true,
            RequirePublishedDifferentialProfile = true,
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
        context.ClearingHouseCycleConfigs.AddRange(
            new ClearingHouseCycleConfig
            {
                Id = 1, ClearingHouseId = 1, CycleName = "Ciclo 5", IsActive = true,
                EffectiveFrom = new DateTime(2026, 1, 1), StartTime = TimeSpan.FromHours(12),
                EndTime = TimeSpan.FromHours(14), CutoffTime = TimeSpan.FromHours(14)
            },
            new ClearingHouseCycleConfig
            {
                Id = 2, ClearingHouseId = 2, CycleName = "Ciclo 1", IsActive = true,
                EffectiveFrom = new DateTime(2026, 1, 1), StartTime = TimeSpan.FromHours(8),
                EndTime = TimeSpan.FromHours(10), CutoffTime = TimeSpan.FromHours(10)
            });
        context.AchCycles.AddRange(
            new AchCycle
            {
                Id = "ACH-CYCLE", CycleName = "Ciclo 5", ClearingHouseId = 1,
                ClearingHouseCycleConfigId = 1, ProcessingDate = new DateTime(2026, 5, 20),
                StartTime = TimeSpan.FromHours(12), EndTime = TimeSpan.FromHours(14), CutoffTime = TimeSpan.FromHours(14)
            },
            new AchCycle
            {
                Id = "CENIT-CYCLE", CycleName = "Ciclo 1", ClearingHouseId = 2,
                ClearingHouseCycleConfigId = 2, ProcessingDate = new DateTime(2026, 5, 20),
                StartTime = TimeSpan.FromHours(8), EndTime = TimeSpan.FromHours(10), CutoffTime = TimeSpan.FromHours(10)
            });
        context.AchBatches.AddRange(
            TestBatch(1, "ACH-CYCLE", 1),
            TestBatch(2, "CENIT-CYCLE", 2));

        var cfa = new FinancialInstitution { Id = 1, Name = "Cooperativa Financiera de Antioquia", RoutingNumber = "00001", TransitCode = "283", IsDefaultSource = true, Status = FinancialInstitutionStatus.Active };
        var ach = new FinancialInstitution { Id = 2, Name = "Banco UAT Externo ACH", RoutingNumber = "99999", TransitCode = "900", Status = FinancialInstitutionStatus.Active };
        var cenit = new FinancialInstitution { Id = 3, Name = "Banco UAT Externo CENIT", RoutingNumber = "99998", TransitCode = "900", Status = FinancialInstitutionStatus.Active };
        cfa.CalculateCheckDigit();
        ach.CalculateCheckDigit();
        cenit.CalculateCheckDigit();
        context.FinancialInstitutions.AddRange(cfa, ach, cenit);
        context.SaveChanges();

        context.AchTransactions.AddRange(
            TestTransaction(10, "ACH-CYCLE", 1, TransactionTypeEnum.Credit, 1, 2),
            TestTransaction(11, "ACH-CYCLE", 1, TransactionTypeEnum.Debit, 1, 2),
            TestTransaction(12, "CENIT-CYCLE", 2, TransactionTypeEnum.Credit, 1, 3),
            TestTransaction(13, "CENIT-CYCLE", 2, TransactionTypeEnum.Debit, 1, 3));
        context.SaveChanges();
    }

    private static AchBatch TestBatch(int id, string cycleId, int sequence) => new()
    {
        Id = id,
        AchCycleId = cycleId,
        CompanyName = "CFA UAT",
        CompanyIdentification = "900000000",
        CompanyEntryDescription = "PRUEBA",
        CompanyEntryDescriptionId = 1,
        OriginOrOdfi = "00001283",
        EffectiveEntryDate = new DateTime(2026, 5, 20),
        BatchSequenceNumber = sequence
    };

    private static AchTransaction TestTransaction(
        int id,
        string cycleId,
        int batchId,
        TransactionTypeEnum type,
        int sourceInstitutionId,
        int destinationInstitutionId) => new()
    {
        Id = id,
        Reference = $"UAT-{id}",
        TransactionExternalId = $"UAT-{id}",
        Type = type,
        Amount = 1000,
        State = AchTransferStateEnum.Pending,
        AchCycleId = cycleId,
        AchBatchId = batchId,
        CompanyEntryDescriptionId = 1,
        CompanyName = "CFA UAT",
        CompanyIdentification = "900000000",
        OriginatingDFI = "00001283",
        ReceivingDFI = "99999900",
        TraceNumber = $"00001283{id:0000000}",
        SourceInstitutionId = sourceInstitutionId,
        DestinationInstitutionId = destinationInstitutionId,
        SourceAccountNumber = $"000000{id}",
        DestinationAccountNumber = $"999999{id}",
        RecipientIdNumber = "900000001",
        EffectiveEntryDate = new DateTime(2026, 5, 20),
        StateChangedAtUtc = new DateTime(2026, 5, 20, 10, 0, 0, DateTimeKind.Utc)
    };
}
