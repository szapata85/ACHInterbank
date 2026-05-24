using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class OfficialNachaGenerationTableDrivenTests
{
    private static readonly string[] OfficialRecordCodes = ["1", "5", "6", "7", "8", "9"];

    [Fact]
    public async Task OfficialNachaGeneration_ShouldUsePublishedAchProfile()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        var content = await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);

        content.Should().NotBeNullOrWhiteSpace();
        SplitRecords(content).Select(x => x[0]).Should().Contain(['1', '5', '6', '7', '8', '9']);
        setup.LegacyRenderer.VerifyNoOtherCalls();
        setup.LegacyRecordProvider.VerifyNoOtherCalls();
        setup.Loader.Verify(x => x.LoadLayoutsAsync(It.IsAny<CancellationToken>()), Times.Never);
        setup.Loader.Verify(x => x.LoadDefinitionsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task OfficialNachaGeneration_ShouldUsePublishedCenitProfile()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "CENIT");

        var content = await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);

        content.Should().NotBeNullOrWhiteSpace();
        SplitRecords(content).Select(x => x[0]).Should().Contain(['1', '5', '6', '7', '8', '9']);
        content.Should().Contain("CENIT");
    }

    [Fact]
    public async Task OfficialNachaGeneration_ShouldNotFallbackToLegacy_WhenProfileMissing()
    {
        await using var context = CreateContext();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        var ex = await Assert.ThrowsAsync<NachaGenerationException>(() => setup.Sut.BuildNachaFileAsync([100], CancellationToken.None));

        ex.Code.Should().Be("NACHA_PROFILE_NOT_PUBLISHED");
        setup.Loader.Verify(x => x.LoadLayoutsAsync(It.IsAny<CancellationToken>()), Times.Never);
        setup.Loader.Verify(x => x.LoadDefinitionsAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task MissingRecord_ShouldReturn_NACHA_REQUIRED_RECORD_MISSING()
    {
        await using var context = await SeedAsync();
        var record7 = await context.CfgLayoutVariants
            .Include(x => x.RecordCode)
            .Include(x => x.Profile)
            .FirstAsync(x => x.Profile.ProfileCode == "OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0" && x.RecordCode.Code == "7");
        record7.StatusId = await context.CatConfigStatuses.Where(x => x.Code == "INACTIVO").Select(x => x.Id).FirstAsync();
        await context.SaveChangesAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        var ex = await Assert.ThrowsAsync<NachaGenerationException>(() => setup.Sut.BuildNachaFileAsync([100], CancellationToken.None));

        ex.Code.Should().Be("NACHA_REQUIRED_RECORD_MISSING");
    }

    [Fact]
    public async Task MissingRequiredField_ShouldReturn_NACHA_REQUIRED_FIELD_MISSING()
    {
        await using var context = await SeedAsync();
        var amountField = await LoadFieldAsync(context, "OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0", "6", "AMOUNT");
        amountField.IsEnabled = false;
        await context.SaveChangesAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        var ex = await Assert.ThrowsAsync<NachaGenerationException>(() => setup.Sut.BuildNachaFileAsync([100], CancellationToken.None));

        ex.Code.Should().Be("NACHA_REQUIRED_FIELD_MISSING");
    }

    [Fact]
    public async Task FieldSourceNotFound_ShouldReturn_NACHA_FIELD_SOURCE_NOT_FOUND()
    {
        await using var context = await SeedAsync();
        var amountField = await LoadFieldAsync(context, "OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0", "6", "AMOUNT");
        amountField.SourceDefinition.PropertyPath = "CampoInexistente";
        await context.SaveChangesAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        var ex = await Assert.ThrowsAsync<NachaGenerationException>(() => setup.Sut.BuildNachaFileAsync([100], CancellationToken.None));

        ex.Code.Should().Be("NACHA_FIELD_SOURCE_NOT_FOUND");
    }

    [Fact]
    public async Task FieldExceedsLength_ShouldReturn_NACHA_FIELD_LENGTH_INVALID()
    {
        await using var context = await SeedAsync();
        var destinationField = await LoadFieldAsync(context, "OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0", "1", "IMMEDIATEDESTINATION");
        destinationField.SourceDefinition.ConstantValue = "12345678901234567890";
        await context.SaveChangesAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        var ex = await Assert.ThrowsAsync<NachaGenerationException>(() => setup.Sut.BuildNachaFileAsync([100], CancellationToken.None));

        ex.Code.Should().Be("NACHA_FIELD_LENGTH_INVALID");
    }

    [Fact]
    public async Task CalculationFailure_ShouldReturn_NACHA_CALCULATION_FAILED()
    {
        await using var context = await SeedAsync();
        var field = await LoadFieldAsync(context, "OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0", "9", "BLOCKCOUNT");
        field.SourceDefinition.ExpressionDsl = """{"source":"runtime","calculationType":"CalculationNotAvailable"}""";
        await context.SaveChangesAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        var ex = await Assert.ThrowsAsync<NachaGenerationException>(() => setup.Sut.BuildNachaFileAsync([100], CancellationToken.None));

        ex.Code.Should().Be("NACHA_CALCULATION_FAILED");
    }

    [Fact]
    public async Task ChangingAchColombiaField_ShouldAffectOnlyAchColombiaFile()
    {
        await using var context = await SeedAsync();
        var achSetup = CreateOfficialSut(context, "ACH Colombia");
        var cenitSetup = CreateOfficialSut(context, "CENIT");
        var achBefore = await achSetup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var cenitBefore = await cenitSetup.Sut.BuildNachaFileAsync([100], CancellationToken.None);

        var achField = await LoadFieldAsync(context, "OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0", "1", "IMMEDIATEORIGINNAME");
        achField.SourceDefinition.ConstantValue = "ACH-CAMBIO-UAT";
        await context.SaveChangesAsync();

        var achAfter = await achSetup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var cenitAfter = await cenitSetup.Sut.BuildNachaFileAsync([100], CancellationToken.None);

        achAfter.Should().NotBe(achBefore);
        cenitAfter.Should().Be(cenitBefore);
    }

    [Fact]
    public async Task ChangingCenitField_ShouldAffectOnlyCenitFile()
    {
        await using var context = await SeedAsync();
        var achSetup = CreateOfficialSut(context, "ACH Colombia");
        var cenitSetup = CreateOfficialSut(context, "CENIT");
        var achBefore = await achSetup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var cenitBefore = await cenitSetup.Sut.BuildNachaFileAsync([100], CancellationToken.None);

        var cenitField = await LoadFieldAsync(context, "OFFICIAL_CENIT_SALIDA_ORIGINAL_V1_0", "1", "IMMEDIATEORIGINNAME");
        cenitField.SourceDefinition.ConstantValue = "CENIT-CAMBIO";
        await context.SaveChangesAsync();

        var achAfter = await achSetup.Sut.BuildNachaFileAsync([100], CancellationToken.None);
        var cenitAfter = await cenitSetup.Sut.BuildNachaFileAsync([100], CancellationToken.None);

        achAfter.Should().Be(achBefore);
        cenitAfter.Should().NotBe(cenitBefore);
    }

    [Fact]
    public async Task OfficialGeneration_ShouldGenerateNonEmptyFile_ForAchColombia()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "ACH Colombia");

        var content = await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);

        content.Length.Should().BeGreaterThan(0);
        (content.Length % 106).Should().Be(0);
    }

    [Fact]
    public async Task OfficialGeneration_ShouldGenerateNonEmptyFile_ForCenit()
    {
        await using var context = await SeedAsync();
        var setup = CreateOfficialSut(context, "CENIT");

        var content = await setup.Sut.BuildNachaFileAsync([100], CancellationToken.None);

        content.Length.Should().BeGreaterThan(0);
        (content.Length % 106).Should().Be(0);
    }

    private static OfficialSut CreateOfficialSut(AchDbContext context, string clearingHouseName)
    {
        var loader = new Mock<INachaDataLoader>(MockBehavior.Strict);
        var validation = new Mock<INachaTransactionValidationService>(MockBehavior.Strict);
        var renderer = new Mock<INachaFixedWidthRecordRenderer>(MockBehavior.Strict);
        var recordProvider = new Mock<INachaRecordDataProvider>(MockBehavior.Strict);
        var semantic = new Mock<INachaSemanticValidator>(MockBehavior.Strict);
        var holiday = new Mock<IBankHoliday>(MockBehavior.Strict);
        var batchNumberGenerator = new Mock<IBatchNumberGenerator>(MockBehavior.Strict);

        var contextData = BuildContext(clearingHouseName);
        loader.Setup(x => x.LoadBatchesByIdsAsync(It.IsAny<IEnumerable<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(contextData.Batches);
        loader.Setup(x => x.LoadHeaderAsync(contextData.Cycle.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NachaHeader
            {
                AchCycleId = contextData.Cycle.Id,
                FileCreationDate = "2026-05-24",
                FileCreationTime = "14:00",
                FileIdModifier = "A",
                ReferenceCode = "UAT6B2"
            });
        loader.Setup(x => x.LoadCompanyEntryDescriptionCatalogAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<(string Term, string StandardEntryClassCode)> { ("PAGOS", "PPD") });
        validation.Setup(x => x.ValidateTransactionsForSendAsync(It.IsAny<IReadOnlyList<AchTransaction>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        semantic.Setup(x => x.Validate(It.IsAny<string>(), It.IsAny<NachaBuildContext>()));
        batchNumberGenerator.Setup(x => x.AssignBatchNumbersAsync(It.IsAny<IReadOnlyList<AchBatch>>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyList<AchBatch> batches, string _, DateTime _, CancellationToken _) =>
                new BatchNumberAssignmentResult(
                    batches.ToDictionary(x => x.Id, _ => 1),
                    "TEST_FIXED",
                    1,
                    []));

        var sut = new NachaFileBuilder(
            context,
            holiday.Object,
            loader.Object,
            validation.Object,
            renderer.Object,
            recordProvider.Object,
            semantic.Object,
            configResolver: new NachaConfigResolver(context),
            type7GenerationStrategy: new NachaType7GenerationStrategy(new NachaType7FieldValueResolver(new NachaType7AliasMap())),
            generationOptions: Options.Create(new NachaGenerationOptions { Mode = "TABLE_DRIVEN" }),
            batchNumberGenerator: batchNumberGenerator.Object);

        return new OfficialSut(sut, loader, renderer, recordProvider);
    }

    private static NachaBuildContext BuildContext(string clearingHouseName)
    {
        var cycle = new AchCycle
        {
            Id = $"cycle-{clearingHouseName.Replace(" ", "-", StringComparison.OrdinalIgnoreCase)}",
            CycleName = "UAT6B2",
            ProcessingDate = new DateTime(2026, 5, 24, 0, 0, 0, DateTimeKind.Utc),
            ClearingHouseId = clearingHouseName.Contains("CENIT", StringComparison.OrdinalIgnoreCase) ? 2 : 1,
            ClearingHouse = new ClearingHouse
            {
                Id = clearingHouseName.Contains("CENIT", StringComparison.OrdinalIgnoreCase) ? 2 : 1,
                Name = clearingHouseName,
                OriginCode = clearingHouseName.Contains("CENIT", StringComparison.OrdinalIgnoreCase) ? "87654321" : "12345678"
            }
        };

        var batch = new AchBatch
        {
            Id = 100,
            AchCycleId = cycle.Id,
            AchCycle = cycle,
            ServiceClassCode = "PPD",
            CompanyEntryDescription = "PAGOS",
            CompanyIdentification = "9001234567",
            CompanyName = "EMPRESA UAT",
            OriginOrOdfi = "12345678",
            EffectiveEntryDate = new DateTime(2026, 5, 24, 0, 0, 0, DateTimeKind.Utc)
        };

        var transaction = new AchTransaction
        {
            Id = 10,
            Type = TransactionTypeEnum.Credit,
            Amount = 100m,
            AchBatchId = batch.Id,
            AchBatch = batch,
            AchCycleId = cycle.Id,
            TransactionCode = "22",
            ReceivingDFI = "12345678",
            TraceNumber = "123456780000001",
            EffectiveEntryDate = batch.EffectiveEntryDate,
            DestinationAccountNumber = "123456789",
            RecipientIdNumber = "RCV001",
            CompanyIdentification = batch.CompanyIdentification,
            OriginatingDFI = batch.OriginOrOdfi,
            Addendas = []
        };
        batch.Transactions = [transaction];

        return new NachaBuildContext
        {
            Cycle = cycle,
            Batches = [batch],
            Transactions = [transaction]
        };
    }

    private static async Task<AchDbContext> SeedAsync()
    {
        var context = CreateContext();
        await new NachaConfigOfficialProfilesSeeder(context).SeedAsync();
        return context;
    }

    private static AchDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var context = new AchDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private static async Task<CfgLayoutField> LoadFieldAsync(AchDbContext context, string profileCode, string recordCode, string fieldCode)
    {
        return await context.CfgLayoutFields
            .Include(x => x.SourceDefinition)
            .Include(x => x.LayoutVariant)
                .ThenInclude(x => x.RecordCode)
            .Include(x => x.LayoutVariant)
                .ThenInclude(x => x.Profile)
            .SingleAsync(x => x.LayoutVariant.Profile.ProfileCode == profileCode
                              && x.LayoutVariant.RecordCode.Code == recordCode
                              && x.FieldCode == fieldCode);
    }

    private static IReadOnlyList<string> SplitRecords(string content)
        => Enumerable.Range(0, content.Length / 106)
            .Select(i => content.Substring(i * 106, 106))
            .ToList();

    private sealed record OfficialSut(
        NachaFileBuilder Sut,
        Mock<INachaDataLoader> Loader,
        Mock<INachaFixedWidthRecordRenderer> LegacyRenderer,
        Mock<INachaRecordDataProvider> LegacyRecordProvider);
}
