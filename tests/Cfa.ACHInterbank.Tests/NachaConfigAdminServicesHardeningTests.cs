using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class NachaConfigAdminServicesHardeningTests
{
    [Fact]
    public async Task CreateDraftAsync_ShouldCreateDraft_AndAudit()
    {
        await using var context = await CreateSqliteContextAsync();
        await SeedCatalogAsync(context);
        var query = new NachaConfigProfileQueryService(context);
        var command = new NachaConfigProfileCommandService(context, query);

        var created = await command.CreateDraftAsync(new NachaConfigCreateDraftRequest
        {
            ProfileCode = "P_DRAFT_01",
            NombreEs = "Perfil borrador",
            CamaraCode = "ACH",
            FlujoCode = "ORIGINAL",
            DireccionCode = "SALIDA",
            ServicioCode = "PPD",
            EffectiveFrom = DateTime.UtcNow.Date
        }, "tester");

        created.ProfileCode.Should().Be("P_DRAFT_01");
        context.HistConfigChanges.Count().Should().Be(1);
        context.HistConfigChanges.Single().ChangeType.Should().Be("DRAFT_CREATE");
    }

    [Fact]
    public async Task UpdateDraftAsync_ShouldFail_OnConcurrencyConflict()
    {
        await using var context = await CreateSqliteContextAsync();
        var profile = await SeedProfileGraphAsync(context);
        var query = new NachaConfigProfileQueryService(context);
        var command = new NachaConfigProfileCommandService(context, query);

        var call = () => command.UpdateDraftAsync(profile.Id, new NachaConfigUpdateProfileRequest
        {
            NombreEs = "Nuevo",
            ContextPriority = 10,
            EffectiveFrom = profile.EffectiveFrom,
            ExpectedRowVersion = Convert.ToBase64String([99, 99, 99])
        }, "tester");

        var ex = await Assert.ThrowsAsync<NachaConfigException>(call);
        ex.ErrorCode.Should().Be("CONCURRENCY_CONFLICT");
    }

    [Fact]
    public async Task CloneProfileAsync_ShouldCreateSupersedingDraft()
    {
        await using var context = await CreateSqliteContextAsync();
        var profile = await SeedProfileGraphAsync(context);
        var query = new NachaConfigProfileQueryService(context);
        var command = new NachaConfigProfileCommandService(context, query);

        var cloned = await command.CloneProfileAsync(profile.Id, new NachaConfigCloneProfileRequest
        {
            NuevoProfileCode = "P_CLONE_01",
            NuevoNombreEs = "Perfil clonado",
            EffectiveFrom = DateTime.UtcNow.Date,
            ExpectedRowVersion = Convert.ToBase64String(profile.RowVersion)
        }, "tester");

        cloned.Should().NotBeNull();
        cloned!.ProfileCode.Should().Be("P_CLONE_01");
        var cloneEntity = await context.CfgProfiles.SingleAsync(x => x.ProfileCode == "P_CLONE_01");
        cloneEntity.SupersedesProfileId.Should().Be(profile.Id);
    }

    [Fact]
    public async Task ValidateBeforePublishAsync_ShouldReturnErrors_WhenMissingRequiredRecords()
    {
        await using var context = await CreateSqliteContextAsync();
        await SeedCatalogAsync(context);
        var draftStatus = await context.CatConfigStatuses.SingleAsync(x => x.Code == "BORRADOR");
        var ach = await context.CatClearingHouses.SingleAsync(x => x.Code == "ACH");
        var flow = await context.CatFlowTypes.SingleAsync(x => x.Code == "ORIGINAL");
        var dir = await context.CatDirections.SingleAsync(x => x.Code == "SALIDA");
        var svc = await context.CatServiceClasses.SingleAsync(x => x.Code == "PPD");
        context.CfgProfiles.Add(new CfgProfile
        {
            ProfileCode = "P_INVALID",
            NameEs = "Inválido",
            StatusId = draftStatus.Id,
            ClearingHouseId = ach.Id,
            FlowTypeId = flow.Id,
            DirectionId = dir.Id,
            ServiceClassId = svc.Id,
            EffectiveFrom = DateTime.UtcNow.Date
        });
        await context.SaveChangesAsync();

        var validation = new NachaConfigValidationService(context);
        var result = await validation.ValidateBeforePublishAsync(context.CfgProfiles.Single().Id);

        result.IsValid.Should().BeFalse();
        result.Issues.Should().Contain(x => x.Codigo == "MISSING_RECORD");
    }

    [Fact]
    public async Task ValidateBeforePublishAsync_ShouldFlagCanonicalAliasProblems_AndType7Collisions()
    {
        await using var context = await CreateSqliteContextAsync();
        var profile = await SeedProfileGraphAsync(context);

        var record7 = await context.CatRecordCodes.SingleAsync(x => x.Code == "7");
        var entidad = await context.CatDataSourceTypes.SingleAsync(x => x.Code == "ENTIDAD");

        var variant7 = new CfgLayoutVariant
        {
            ProfileId = profile.Id,
            RecordCodeId = record7.Id,
            VariantCode = "R7",
            NameEs = "R7",
            Priority = 10,
            EffectiveFrom = DateTime.UtcNow.Date.AddDays(-1),
            StatusId = 2,
            IsDefaultForRecord = true
        };
        context.CfgLayoutVariants.Add(variant7);
        await context.SaveChangesAsync();

        var srcAliasA = new CfgFieldSourceDefinition { DataSourceTypeId = entidad.Id, PropertyPath = "tipoaddenda" };
        var srcAliasB = new CfgFieldSourceDefinition { DataSourceTypeId = entidad.Id, PropertyPath = "addendatype" };
        var srcInvalidCanonical = new CfgFieldSourceDefinition { DataSourceTypeId = entidad.Id, PropertyPath = "UnknownCanonicalField" };
        var srcMissingAlias = new CfgFieldSourceDefinition { DataSourceTypeId = entidad.Id, PropertyPath = "alias_no_resuelve" };
        context.CfgFieldSourceDefinitions.AddRange(srcAliasA, srcAliasB, srcInvalidCanonical, srcMissingAlias);
        await context.SaveChangesAsync();

        context.CfgLayoutFields.AddRange(
            new CfgLayoutField { LayoutVariantId = variant7.Id, FieldCode = "A7_1", FieldNameEs = "A7_1", StartPosition = 1, Length = 10, SourceDefinitionId = srcAliasA.Id, SortOrder = 1, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = variant7.Id, FieldCode = "A7_2", FieldNameEs = "A7_2", StartPosition = 11, Length = 10, SourceDefinitionId = srcAliasB.Id, SortOrder = 2, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = variant7.Id, FieldCode = "A7_3", FieldNameEs = "A7_3", StartPosition = 21, Length = 10, SourceDefinitionId = srcInvalidCanonical.Id, SortOrder = 3, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = variant7.Id, FieldCode = "A7_4", FieldNameEs = "A7_4", StartPosition = 31, Length = 10, SourceDefinitionId = srcMissingAlias.Id, SortOrder = 4, IsEnabled = true });
        await context.SaveChangesAsync();

        var validation = new NachaConfigValidationService(context);
        var result = await validation.ValidateBeforePublishAsync(profile.Id);

        result.IsValid.Should().BeFalse();
        result.Issues.Should().Contain(x => x.Codigo == "AMBIGUOUS_ALIAS");
        result.Issues.Should().Contain(x => x.Codigo == "INVALID_CANONICAL_KEY");
        result.Issues.Should().Contain(x => x.Codigo == "UNRESOLVABLE_ALIAS");
    }

    [Fact]
    public async Task ValidateBeforePublishAsync_ShouldFlagHeaderNormativeViolations_ForRecord1And5()
    {
        await using var context = await CreateSqliteContextAsync();
        var profile = await SeedProfileGraphAsync(context);
        var entidad = await context.CatDataSourceTypes.SingleAsync(x => x.Code == "ENTIDAD");
        var constante = await context.CatDataSourceTypes.SingleAsync(x => x.Code == "CONSTANTE");

        var record1Variant = await context.CfgLayoutVariants.Include(x => x.RecordCode).SingleAsync(x => x.ProfileId == profile.Id && x.RecordCode.Code == "1");
        var record5Variant = await context.CfgLayoutVariants.Include(x => x.RecordCode).SingleAsync(x => x.ProfileId == profile.Id && x.RecordCode.Code == "5");

        var record1Fields = await context.CfgLayoutFields.Where(x => x.LayoutVariantId == record1Variant.Id).ToListAsync();
        var record5Fields = await context.CfgLayoutFields.Where(x => x.LayoutVariantId == record5Variant.Id).ToListAsync();
        context.CfgLayoutFields.RemoveRange(record1Fields);
        context.CfgLayoutFields.RemoveRange(record5Fields);
        await context.SaveChangesAsync();

        var srcRecordSize = new CfgFieldSourceDefinition { DataSourceTypeId = constante.Id, ConstantValue = "120" };
        var srcBlocking = new CfgFieldSourceDefinition { DataSourceTypeId = constante.Id, ConstantValue = "12" };
        var srcFormat = new CfgFieldSourceDefinition { DataSourceTypeId = constante.Id, ConstantValue = "9" };
        var srcOrigin = new CfgFieldSourceDefinition { DataSourceTypeId = constante.Id, ConstantValue = "11111111" };
        var srcDestination = new CfgFieldSourceDefinition { DataSourceTypeId = constante.Id, ConstantValue = "11111111" };
        var srcSec = new CfgFieldSourceDefinition { DataSourceTypeId = constante.Id, ConstantValue = "WEB" };
        var srcDfi = new CfgFieldSourceDefinition { DataSourceTypeId = constante.Id, ConstantValue = "ABC12345" };
        var srcEntity = new CfgFieldSourceDefinition { DataSourceTypeId = entidad.Id, PropertyPath = "Dummy" };
        context.CfgFieldSourceDefinitions.AddRange(srcRecordSize, srcBlocking, srcFormat, srcOrigin, srcDestination, srcSec, srcDfi, srcEntity);
        await context.SaveChangesAsync();

        context.CfgLayoutFields.AddRange(
            new CfgLayoutField { LayoutVariantId = record1Variant.Id, FieldCode = "PriorityCode", FieldNameEs = "PriorityCode", StartPosition = 1, Length = 2, SourceDefinitionId = srcEntity.Id, SortOrder = 1, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record1Variant.Id, FieldCode = "ImmediateDestination", FieldNameEs = "ImmediateDestination", StartPosition = 3, Length = 10, SourceDefinitionId = srcDestination.Id, SortOrder = 2, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record1Variant.Id, FieldCode = "ImmediateOrigin", FieldNameEs = "ImmediateOrigin", StartPosition = 13, Length = 10, SourceDefinitionId = srcOrigin.Id, SortOrder = 3, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record1Variant.Id, FieldCode = "FileCreationDate", FieldNameEs = "FileCreationDate", StartPosition = 23, Length = 6, SourceDefinitionId = srcEntity.Id, FormatMask = "yyyyMMdd", SortOrder = 4, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record1Variant.Id, FieldCode = "FileCreationTime", FieldNameEs = "FileCreationTime", StartPosition = 29, Length = 4, SourceDefinitionId = srcEntity.Id, FormatMask = "HH:mm", SortOrder = 5, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record1Variant.Id, FieldCode = "FileIdModifier", FieldNameEs = "FileIdModifier", StartPosition = 33, Length = 1, SourceDefinitionId = srcEntity.Id, SortOrder = 6, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record1Variant.Id, FieldCode = "RecordSize", FieldNameEs = "RecordSize", StartPosition = 34, Length = 3, SourceDefinitionId = srcRecordSize.Id, SortOrder = 7, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record1Variant.Id, FieldCode = "BlockingFactor", FieldNameEs = "BlockingFactor", StartPosition = 37, Length = 2, SourceDefinitionId = srcBlocking.Id, SortOrder = 8, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record1Variant.Id, FieldCode = "FormatCode", FieldNameEs = "FormatCode", StartPosition = 39, Length = 1, SourceDefinitionId = srcFormat.Id, SortOrder = 9, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record1Variant.Id, FieldCode = "ImmediateDestinationName", FieldNameEs = "ImmediateDestinationName", StartPosition = 40, Length = 23, SourceDefinitionId = srcEntity.Id, SortOrder = 10, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record1Variant.Id, FieldCode = "ImmediateOriginName", FieldNameEs = "ImmediateOriginName", StartPosition = 63, Length = 23, SourceDefinitionId = srcEntity.Id, SortOrder = 11, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record1Variant.Id, FieldCode = "ReferenceCode", FieldNameEs = "ReferenceCode", StartPosition = 86, Length = 8, SourceDefinitionId = srcEntity.Id, SortOrder = 12, IsEnabled = true },

            new CfgLayoutField { LayoutVariantId = record5Variant.Id, FieldCode = "ServiceClassCode", FieldNameEs = "ServiceClassCode", StartPosition = 2, Length = 3, SourceDefinitionId = srcEntity.Id, SortOrder = 1, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record5Variant.Id, FieldCode = "CompanyName", FieldNameEs = "CompanyName", StartPosition = 5, Length = 16, SourceDefinitionId = srcEntity.Id, SortOrder = 2, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record5Variant.Id, FieldCode = "CompanyDiscretionaryData", FieldNameEs = "CompanyDiscretionaryData", StartPosition = 21, Length = 20, SourceDefinitionId = srcEntity.Id, SortOrder = 3, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record5Variant.Id, FieldCode = "CompanyIdentification", FieldNameEs = "CompanyIdentification", StartPosition = 41, Length = 10, SourceDefinitionId = srcEntity.Id, SortOrder = 4, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record5Variant.Id, FieldCode = "StandardEntryClassCode", FieldNameEs = "StandardEntryClassCode", StartPosition = 51, Length = 3, SourceDefinitionId = srcSec.Id, SortOrder = 5, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record5Variant.Id, FieldCode = "CompanyEntryDescription", FieldNameEs = "CompanyEntryDescription", StartPosition = 54, Length = 10, SourceDefinitionId = srcEntity.Id, SortOrder = 6, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record5Variant.Id, FieldCode = "CompanyDescriptiveDate", FieldNameEs = "CompanyDescriptiveDate", StartPosition = 64, Length = 6, SourceDefinitionId = srcEntity.Id, FormatMask = "yyyyMMdd", SortOrder = 7, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record5Variant.Id, FieldCode = "EffectiveEntryDate", FieldNameEs = "EffectiveEntryDate", StartPosition = 70, Length = 6, SourceDefinitionId = srcEntity.Id, FormatMask = "ddMMyy", SortOrder = 8, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record5Variant.Id, FieldCode = "SettlementDate", FieldNameEs = "SettlementDate", StartPosition = 76, Length = 3, SourceDefinitionId = srcEntity.Id, SortOrder = 9, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record5Variant.Id, FieldCode = "OriginatorStatusCode", FieldNameEs = "OriginatorStatusCode", StartPosition = 79, Length = 1, SourceDefinitionId = srcEntity.Id, SortOrder = 10, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record5Variant.Id, FieldCode = "OriginatingDFI", FieldNameEs = "OriginatingDFI", StartPosition = 80, Length = 8, SourceDefinitionId = srcDfi.Id, SortOrder = 11, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record5Variant.Id, FieldCode = "BatchNumber", FieldNameEs = "BatchNumber", StartPosition = 88, Length = 7, SourceDefinitionId = srcEntity.Id, SortOrder = 12, IsEnabled = true }
        );
        await context.SaveChangesAsync();

        var validation = new NachaConfigValidationService(context);
        var result = await validation.ValidateBeforePublishAsync(profile.Id);

        result.IsValid.Should().BeFalse();
        result.Issues.Should().Contain(x => x.Codigo == "INVALID_CONSTANT_VALUE");
        result.Issues.Should().Contain(x => x.Codigo == "INVALID_DATE_FORMAT");
        result.Issues.Should().Contain(x => x.Codigo == "INVALID_HEADER_COHERENCE");
        result.Issues.Should().Contain(x => x.Codigo == "INVALID_SEC_CODE");
        result.Issues.Should().Contain(x => x.Codigo == "INVALID_ORIGINATING_DFI");
    }

    [Fact]
    public async Task PublishAsync_ShouldSucceed_AndPersistSnapshotAndHistory()
    {
        await using var context = await CreateSqliteContextAsync();
        var profile = await SeedProfileGraphAsync(context);
        var validation = new NachaConfigValidationService(context);
        var publication = new NachaConfigPublicationService(context, validation);

        var result = await publication.PublishAsync(profile.Id, "publisher", Convert.ToBase64String(profile.RowVersion));

        result.Publicado.Should().BeTrue();
        context.HistConfigSnapshots.Should().Contain(x => x.ProfileId == profile.Id && x.SnapshotType == "PUBLISH");
        context.HistConfigChanges.Should().Contain(x => x.ProfileId == profile.Id && x.ChangeType == "PUBLISH");
    }

    [Fact]
    public async Task PublishAsync_ShouldFail_WhenValidationBlocked_WithoutPartialState()
    {
        await using var context = await CreateSqliteContextAsync();
        await SeedCatalogAsync(context);
        var profile = await CreateDraftWithoutRecordsAsync(context);
        var validation = new NachaConfigValidationService(context);
        var publication = new NachaConfigPublicationService(context, validation);

        var result = await publication.PublishAsync(profile.Id, "publisher", Convert.ToBase64String(profile.RowVersion));

        result.Publicado.Should().BeFalse();
        context.HistConfigSnapshots.Should().BeEmpty();
        context.HistConfigChanges.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateRecordSequenceAsync_ShouldRejectDuplicatedSequence()
    {
        await using var context = await CreateSqliteContextAsync();
        var profile = await SeedProfileGraphAsync(context);
        var query = new NachaConfigProfileQueryService(context);
        var command = new NachaConfigProfileCommandService(context, query);
        var records = await context.CfgProfileRecords.Where(x => x.ProfileId == profile.Id).ToListAsync();

        var call = () => command.UpdateRecordSequenceAsync(profile.Id, new NachaConfigRecordSequenceUpdateRequest
        {
            ExpectedRowVersion = Convert.ToBase64String(profile.RowVersion),
            Records =
            [
                new NachaConfigProfileRecordSequenceDto { ProfileRecordId = records[0].Id, Sequence = 10 },
                new NachaConfigProfileRecordSequenceDto { ProfileRecordId = records[1].Id, Sequence = 10 }
            ]
        }, "tester");

        var ex = await Assert.ThrowsAsync<NachaConfigException>(call);
        ex.ErrorCode.Should().Be("INVALID_SEQUENCE");
    }

    [Fact]
    public async Task InactivateAndArchive_ShouldChangeStatus()
    {
        await using var context = await CreateSqliteContextAsync();
        var profile = await SeedProfileGraphAsync(context);
        var query = new NachaConfigProfileQueryService(context);
        var command = new NachaConfigProfileCommandService(context, query);

        var inactivated = await command.InactivateProfileAsync(profile.Id, "tester", Convert.ToBase64String(profile.RowVersion));
        inactivated.Should().BeTrue();
        var refreshed = await context.CfgProfiles.AsNoTracking().SingleAsync(x => x.Id == profile.Id);
        var inactivo = await context.CatConfigStatuses.SingleAsync(x => x.Id == refreshed.StatusId);
        inactivo.Code.Should().Be("INACTIVO");

        var archived = await command.ArchiveProfileAsync(profile.Id, "tester", Convert.ToBase64String(refreshed.RowVersion));
        archived.Should().BeTrue();
        var archivedEntity = await context.CfgProfiles.AsNoTracking().SingleAsync(x => x.Id == profile.Id);
        var archivado = await context.CatConfigStatuses.SingleAsync(x => x.Id == archivedEntity.StatusId);
        archivado.Code.Should().Be("ARCHIVADO");
    }

    [Fact]
    public async Task PreviewService_ShouldReuseResolverAndReturnLayoutSelection()
    {
        await using var context = await CreateSqliteContextAsync();
        var profile = await SeedProfileGraphAsync(context);
        var resolver = new NachaConfigResolver(context);
        var preview = new NachaConfigPreviewService(resolver);

        var result = await preview.PreviewResolverAsync(new NachaConfigResolverPreviewRequest
        {
            CamaraCode = "ACH",
            FlujoCode = "ORIGINAL",
            DireccionCode = "SALIDA",
            ServicioCode = "PPD",
            ProcessDateUtc = DateTime.UtcNow,
            RecordCodes = ["1", "5", "6", "8", "9"]
        });

        result.Success.Should().BeTrue();
        result.ProfileId.Should().Be(profile.Id);
        result.LayoutByRecordCode.Should().ContainKey("1");
    }

    [Fact]
    public async Task GetFilterCatalogsAsync_ShouldReturnConfiguredCatalogs()
    {
        await using var context = await CreateSqliteContextAsync();
        await SeedCatalogAsync(context);
        var query = new NachaConfigProfileQueryService(context);

        var result = await query.GetFilterCatalogsAsync();

        result.Estados.Should().Contain(x => x.Code == "BORRADOR");
        result.Camaras.Should().Contain(x => x.Code == "ACH");
        result.Flujos.Should().Contain(x => x.Code == "ORIGINAL");
        result.Direcciones.Should().Contain(x => x.Code == "SALIDA");
        result.Servicios.Should().Contain(x => x.Code == "PPD");
    }

    private static async Task<AchDbContext> CreateSqliteContextAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AchDbContext(options);
        await context.Database.EnsureCreatedAsync();
        return context;
    }

    private static async Task SeedCatalogAsync(AchDbContext context)
    {
        context.CatClearingHouses.Add(new CatClearingHouse { Id = 1, Code = "ACH", Name = "ACH Colombia", IsActive = true });
        context.CatFlowTypes.Add(new CatFlowType { Id = 1, Code = "ORIGINAL", NameEs = "Original", IsActive = true });
        context.CatDirections.Add(new CatDirection { Id = 1, Code = "SALIDA", NameEs = "Salida", IsActive = true });
        context.CatServiceClasses.Add(new CatServiceClass { Id = 1, Code = "PPD", NameEs = "PPD", IsActive = true });
        context.CatConfigStatuses.AddRange(
            new CatConfigStatus { Id = 1, Code = "BORRADOR", IsEditable = true, IsPublishable = true },
            new CatConfigStatus { Id = 2, Code = "PUBLICADO", IsEditable = false, IsPublishable = false },
            new CatConfigStatus { Id = 3, Code = "INACTIVO", IsEditable = false, IsPublishable = false },
            new CatConfigStatus { Id = 4, Code = "ARCHIVADO", IsEditable = false, IsPublishable = false });
        context.CatRecordCodes.AddRange(
            new CatRecordCode { Id = 1, Code = "1", NameEs = "Header", IsMandatoryBase = true },
            new CatRecordCode { Id = 2, Code = "5", NameEs = "Batch", IsMandatoryBase = true },
            new CatRecordCode { Id = 3, Code = "6", NameEs = "Detail", IsMandatoryBase = true },
            new CatRecordCode { Id = 4, Code = "7", NameEs = "Addenda", IsMandatoryBase = false },
            new CatRecordCode { Id = 5, Code = "8", NameEs = "BatchCtl", IsMandatoryBase = true },
            new CatRecordCode { Id = 6, Code = "9", NameEs = "FileCtl", IsMandatoryBase = true });
        context.CatDataSourceTypes.AddRange(
            new CatDataSourceType { Id = 1, Code = "CONSTANTE", NameEs = "Constante" },
            new CatDataSourceType { Id = 2, Code = "ENTIDAD", NameEs = "Entidad" });
        await context.SaveChangesAsync();
    }

    private static async Task<CfgProfile> CreateDraftWithoutRecordsAsync(AchDbContext context)
    {
        await SeedCatalogAsync(context);
        var profile = new CfgProfile
        {
            ProfileCode = "P_NO_RECORDS",
            NameEs = "Sin records",
            ClearingHouseId = 1,
            FlowTypeId = 1,
            DirectionId = 1,
            ServiceClassId = 1,
            StatusId = 1,
            EffectiveFrom = DateTime.UtcNow.Date
        };
        context.CfgProfiles.Add(profile);
        await context.SaveChangesAsync();
        return profile;
    }

    private static async Task<CfgProfile> SeedProfileGraphAsync(AchDbContext context)
    {
        await SeedCatalogAsync(context);
        var profile = new CfgProfile
        {
            ProfileCode = "P_VALID_01",
            NameEs = "Perfil válido",
            ClearingHouseId = 1,
            FlowTypeId = 1,
            DirectionId = 1,
            ServiceClassId = 1,
            StatusId = 1,
            EffectiveFrom = DateTime.UtcNow.Date.AddDays(-2),
            RowVersion = [1, 0, 0]
        };
        context.CfgProfiles.Add(profile);
        await context.SaveChangesAsync();

        context.CfgProfileRecords.AddRange(
            new CfgProfileRecord { ProfileId = profile.Id, RecordCodeId = 1, Sequence = 10, IsEnabled = true, MinOccurs = 1, SourceStrategy = "TABLE_DRIVEN" },
            new CfgProfileRecord { ProfileId = profile.Id, RecordCodeId = 2, Sequence = 20, IsEnabled = true, MinOccurs = 1, SourceStrategy = "TABLE_DRIVEN" },
            new CfgProfileRecord { ProfileId = profile.Id, RecordCodeId = 3, Sequence = 30, IsEnabled = true, MinOccurs = 1, SourceStrategy = "TABLE_DRIVEN" },
            new CfgProfileRecord { ProfileId = profile.Id, RecordCodeId = 5, Sequence = 40, IsEnabled = true, MinOccurs = 1, SourceStrategy = "TABLE_DRIVEN" },
            new CfgProfileRecord { ProfileId = profile.Id, RecordCodeId = 6, Sequence = 50, IsEnabled = true, MinOccurs = 1, SourceStrategy = "TABLE_DRIVEN" });

        var source = new CfgFieldSourceDefinition { DataSourceTypeId = 1, ConstantValue = "1" };
        context.CfgFieldSourceDefinitions.Add(source);
        await context.SaveChangesAsync();

        var variants = new[]
        {
            new CfgLayoutVariant { ProfileId = profile.Id, RecordCodeId = 1, VariantCode = "R1", NameEs = "R1", Priority = 10, EffectiveFrom = DateTime.UtcNow.Date.AddDays(-1), StatusId = 2, IsDefaultForRecord = true },
            new CfgLayoutVariant { ProfileId = profile.Id, RecordCodeId = 2, VariantCode = "R5", NameEs = "R5", Priority = 10, EffectiveFrom = DateTime.UtcNow.Date.AddDays(-1), StatusId = 2, IsDefaultForRecord = true },
            new CfgLayoutVariant { ProfileId = profile.Id, RecordCodeId = 3, VariantCode = "R6", NameEs = "R6", Priority = 10, EffectiveFrom = DateTime.UtcNow.Date.AddDays(-1), StatusId = 2, IsDefaultForRecord = true },
            new CfgLayoutVariant { ProfileId = profile.Id, RecordCodeId = 5, VariantCode = "R8", NameEs = "R8", Priority = 10, EffectiveFrom = DateTime.UtcNow.Date.AddDays(-1), StatusId = 2, IsDefaultForRecord = true },
            new CfgLayoutVariant { ProfileId = profile.Id, RecordCodeId = 6, VariantCode = "R9", NameEs = "R9", Priority = 10, EffectiveFrom = DateTime.UtcNow.Date.AddDays(-1), StatusId = 2, IsDefaultForRecord = true }
        };
        context.CfgLayoutVariants.AddRange(variants);
        await context.SaveChangesAsync();

        foreach (var variant in variants)
        {
            context.CfgLayoutFields.Add(new CfgLayoutField
            {
                LayoutVariantId = variant.Id,
                FieldCode = $"F{variant.RecordCodeId}",
                FieldNameEs = $"Campo {variant.RecordCodeId}",
                StartPosition = 1,
                Length = 1,
                SourceDefinitionId = source.Id,
                SortOrder = 1,
                IsEnabled = true
            });
        }

        await context.SaveChangesAsync();
        return await context.CfgProfiles.AsNoTracking().SingleAsync(x => x.Id == profile.Id);
    }
}
