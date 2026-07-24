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
    public async Task ThirdClearingHouse_ShouldAppearInCatalog_AndOwnItsProfile()
    {
        await using var context = await CreateSqliteContextAsync();
        await SeedCatalogAsync(context);
        context.CatClearingHouses.Add(new CatClearingHouse
        {
            Id = 3,
            Code = "REDTEST",
            Name = "Red sintética de pruebas",
            IsActive = true
        });
        await context.SaveChangesAsync();

        var query = new NachaConfigProfileQueryService(context);
        var command = new NachaConfigProfileCommandService(context, query);
        var created = await command.CreateDraftAsync(new NachaConfigCreateDraftRequest
        {
            ProfileCode = "REDTEST_PROFILE_01",
            NombreEs = "Perfil propio de tercera cámara",
            CamaraCode = "REDTEST",
            FlujoCode = "ORIGINAL",
            DireccionCode = "SALIDA",
            ServicioCode = "PPD",
            EffectiveFrom = new DateTime(2026, 7, 24)
        }, "job6-test");

        var catalogs = await query.GetFilterCatalogsAsync();
        var detail = await query.GetProfileDetailAsync(created.Id);

        catalogs.Camaras.Should().ContainSingle(x =>
            x.Code == "REDTEST" && x.LabelEs == "Red sintética de pruebas");
        detail.Should().NotBeNull();
        detail!.Camara.Should().Be("REDTEST");
        detail.CamaraNombre.Should().Be("Red sintética de pruebas");
        detail.Flujo.Should().Be("ORIGINAL");
        detail.Direccion.Should().Be("SALIDA");
        detail.Servicio.Should().Be("PPD");
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
    public async Task UpdateLayoutVariantAsync_ShouldAllowEdit_WhenBorrador()
    {
        await using var context = await CreateSqliteContextAsync();
        var profile = await SeedProfileGraphAsync(context);
        var query = new NachaConfigProfileQueryService(context);
        var command = new NachaConfigProfileCommandService(context, query);
        var record1Id = await context.CatRecordCodes.Where(x => x.Code == "1").Select(x => x.Id).SingleAsync();
        var variantId = await context.CfgLayoutVariants
            .Where(x => x.ProfileId == profile.Id && x.RecordCodeId == record1Id)
            .Select(x => x.Id)
            .SingleAsync();

        var updated = await command.UpdateLayoutVariantAsync(profile.Id, variantId, new NachaConfigLayoutVariantEditDto
        {
            NombreEs = "Variante actualizada",
            Descripcion = "Descripcion actualizada",
            Priority = 25,
            IsDefaultForRecord = false,
            EffectiveFrom = profile.EffectiveFrom.AddDays(1),
            EffectiveTo = profile.EffectiveFrom.AddDays(10),
            ExpectedRowVersion = Convert.ToBase64String(profile.RowVersion)
        }, "tester");

        updated.Should().BeTrue();
        var persisted = await context.CfgLayoutVariants.AsNoTracking().SingleAsync(x => x.Id == variantId);
        persisted.NameEs.Should().Be("Variante actualizada");
        persisted.Description.Should().Be("Descripcion actualizada");
        persisted.Priority.Should().Be(25);
        persisted.IsDefaultForRecord.Should().BeFalse();
        persisted.EffectiveFrom.Should().Be(profile.EffectiveFrom.AddDays(1));
        persisted.EffectiveTo.Should().Be(profile.EffectiveFrom.AddDays(10));
    }

    [Fact]
    public async Task UpdateLayoutFieldAsync_ShouldAllowEdit_WhenBorrador()
    {
        await using var context = await CreateSqliteContextAsync();
        var profile = await SeedProfileGraphAsync(context);
        var query = new NachaConfigProfileQueryService(context);
        var command = new NachaConfigProfileCommandService(context, query);
        var variantId = await context.CfgLayoutVariants
            .Where(x => x.ProfileId == profile.Id)
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .FirstAsync();
        var fieldId = await context.CfgLayoutFields
            .Where(x => x.LayoutVariantId == variantId)
            .Select(x => x.Id)
            .SingleAsync();

        var updated = await command.UpdateLayoutFieldAsync(profile.Id, fieldId, new NachaConfigLayoutFieldEditDto
        {
            FieldNameEs = "Campo actualizado",
            StartPosition = 7,
            Length = 12,
            PropertyPath = "AchTransaction.Amount",
            IsEnabled = false,
            ExpectedRowVersion = Convert.ToBase64String(profile.RowVersion)
        }, "tester");

        updated.Should().BeTrue();
        var persisted = await context.CfgLayoutFields
            .Include(x => x.SourceDefinition)
            .AsNoTracking()
            .SingleAsync(x => x.Id == fieldId);
        persisted.FieldNameEs.Should().Be("Campo actualizado");
        persisted.StartPosition.Should().Be(7);
        persisted.Length.Should().Be(12);
        persisted.IsEnabled.Should().BeFalse();
        persisted.SourceDefinition.PropertyPath.Should().Be("AchTransaction.Amount");
    }

    [Fact]
    public async Task UpdateFieldRuleAsync_ShouldAllowEdit_WhenBorrador()
    {
        await using var context = await CreateSqliteContextAsync();
        var profile = await SeedProfileGraphAsync(context);
        var query = new NachaConfigProfileQueryService(context);
        var command = new NachaConfigProfileCommandService(context, query);
        var ruleId = await context.CfgFieldRules
            .Where(x => x.LayoutField.LayoutVariant.ProfileId == profile.Id)
            .Select(x => x.Id)
            .SingleAsync();

        var updated = await command.UpdateFieldRuleAsync(profile.Id, ruleId, new NachaConfigFieldRuleEditDto
        {
            ErrorCode = "ERR_UPDATED",
            ErrorMessageEs = "Mensaje actualizado",
            Severity = "WARN",
            IsEnabled = false,
            ExpectedRowVersion = Convert.ToBase64String(profile.RowVersion)
        }, "tester");

        updated.Should().BeTrue();
        var persisted = await context.CfgFieldRules.AsNoTracking().SingleAsync(x => x.Id == ruleId);
        persisted.ErrorCode.Should().Be("ERR_UPDATED");
        persisted.ErrorMessageEs.Should().Be("Mensaje actualizado");
        persisted.Severity.Should().Be("WARN");
        persisted.IsEnabled.Should().BeFalse();
    }

    [Theory]
    [InlineData("PUBLICADO")]
    [InlineData("INACTIVO")]
    [InlineData("ARCHIVADO")]
    public async Task UpdateLayoutVariantAsync_ShouldReject_WhenProfileNotBorrador(string statusCode)
    {
        await using var context = await CreateSqliteContextAsync();
        var profile = await SeedProfileGraphAsync(context);
        var query = new NachaConfigProfileQueryService(context);
        var command = new NachaConfigProfileCommandService(context, query);
        var record1Id = await context.CatRecordCodes.Where(x => x.Code == "1").Select(x => x.Id).SingleAsync();
        var variantId = await context.CfgLayoutVariants
            .Where(x => x.ProfileId == profile.Id && x.RecordCodeId == record1Id)
            .Select(x => x.Id)
            .SingleAsync();

        profile = await SetProfileStatusAsync(context, profile.Id, statusCode);

        var call = () => command.UpdateLayoutVariantAsync(profile.Id, variantId, new NachaConfigLayoutVariantEditDto
        {
            NombreEs = "No permitido",
            Priority = 99,
            IsDefaultForRecord = true,
            EffectiveFrom = profile.EffectiveFrom,
            ExpectedRowVersion = Convert.ToBase64String(profile.RowVersion)
        }, "tester");

        var ex = await Assert.ThrowsAsync<NachaConfigException>(call);
        ex.ErrorCode.Should().Be("INVALID_PROFILE_STATE");
    }

    [Theory]
    [InlineData("PUBLICADO")]
    [InlineData("INACTIVO")]
    [InlineData("ARCHIVADO")]
    public async Task UpdateLayoutFieldAsync_ShouldReject_WhenProfileNotBorrador(string statusCode)
    {
        await using var context = await CreateSqliteContextAsync();
        var profile = await SeedProfileGraphAsync(context);
        var query = new NachaConfigProfileQueryService(context);
        var command = new NachaConfigProfileCommandService(context, query);
        var variantId = await context.CfgLayoutVariants
            .Where(x => x.ProfileId == profile.Id)
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .FirstAsync();
        var fieldId = await context.CfgLayoutFields
            .Where(x => x.LayoutVariantId == variantId)
            .Select(x => x.Id)
            .SingleAsync();

        profile = await SetProfileStatusAsync(context, profile.Id, statusCode);

        var call = () => command.UpdateLayoutFieldAsync(profile.Id, fieldId, new NachaConfigLayoutFieldEditDto
        {
            FieldNameEs = "No permitido",
            StartPosition = 2,
            Length = 8,
            IsEnabled = true,
            ExpectedRowVersion = Convert.ToBase64String(profile.RowVersion)
        }, "tester");

        var ex = await Assert.ThrowsAsync<NachaConfigException>(call);
        ex.ErrorCode.Should().Be("INVALID_PROFILE_STATE");
    }

    [Theory]
    [InlineData("PUBLICADO")]
    [InlineData("INACTIVO")]
    [InlineData("ARCHIVADO")]
    public async Task UpdateFieldRuleAsync_ShouldReject_WhenProfileNotBorrador(string statusCode)
    {
        await using var context = await CreateSqliteContextAsync();
        var profile = await SeedProfileGraphAsync(context);
        var query = new NachaConfigProfileQueryService(context);
        var command = new NachaConfigProfileCommandService(context, query);
        var ruleId = await context.CfgFieldRules
            .Where(x => x.LayoutField.LayoutVariant.ProfileId == profile.Id)
            .Select(x => x.Id)
            .SingleAsync();

        profile = await SetProfileStatusAsync(context, profile.Id, statusCode);

        var call = () => command.UpdateFieldRuleAsync(profile.Id, ruleId, new NachaConfigFieldRuleEditDto
        {
            ErrorCode = "ERR_BLOCKED",
            ErrorMessageEs = "No permitido",
            Severity = "ERROR",
            IsEnabled = true,
            ExpectedRowVersion = Convert.ToBase64String(profile.RowVersion)
        }, "tester");

        var ex = await Assert.ThrowsAsync<NachaConfigException>(call);
        ex.ErrorCode.Should().Be("INVALID_PROFILE_STATE");
    }

    [Fact]
    public async Task UpdateLayoutVariantAsync_ShouldFail_OnConcurrencyConflict()
    {
        await using var context = await CreateSqliteContextAsync();
        var profile = await SeedProfileGraphAsync(context);
        var query = new NachaConfigProfileQueryService(context);
        var command = new NachaConfigProfileCommandService(context, query);
        var record1Id = await context.CatRecordCodes.Where(x => x.Code == "1").Select(x => x.Id).SingleAsync();
        var variantId = await context.CfgLayoutVariants
            .Where(x => x.ProfileId == profile.Id && x.RecordCodeId == record1Id)
            .Select(x => x.Id)
            .SingleAsync();

        var call = () => command.UpdateLayoutVariantAsync(profile.Id, variantId, new NachaConfigLayoutVariantEditDto
        {
            NombreEs = "Nuevo",
            Priority = 10,
            IsDefaultForRecord = true,
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
        var srcFileId = new CfgFieldSourceDefinition { DataSourceTypeId = constante.Id, ConstantValue = "@" };
        var srcSec = new CfgFieldSourceDefinition { DataSourceTypeId = constante.Id, ConstantValue = "WEB" };
        var srcDfi = new CfgFieldSourceDefinition { DataSourceTypeId = constante.Id, ConstantValue = "ABC12345" };
        var srcGeneric = new CfgFieldSourceDefinition { DataSourceTypeId = constante.Id, ConstantValue = "X" };
        context.CfgFieldSourceDefinitions.AddRange(srcRecordSize, srcBlocking, srcFormat, srcOrigin, srcDestination, srcFileId, srcSec, srcDfi, srcGeneric);
        await context.SaveChangesAsync();

        context.CfgLayoutFields.AddRange(
            new CfgLayoutField { LayoutVariantId = record1Variant.Id, FieldCode = "PriorityCode", FieldNameEs = "PriorityCode", StartPosition = 1, Length = 2, SourceDefinitionId = srcGeneric.Id, SortOrder = 1, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record1Variant.Id, FieldCode = "ImmediateDestination", FieldNameEs = "ImmediateDestination", StartPosition = 3, Length = 10, SourceDefinitionId = srcDestination.Id, SortOrder = 2, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record1Variant.Id, FieldCode = "ImmediateOrigin", FieldNameEs = "ImmediateOrigin", StartPosition = 13, Length = 10, SourceDefinitionId = srcOrigin.Id, SortOrder = 3, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record1Variant.Id, FieldCode = "FileCreationDate", FieldNameEs = "FileCreationDate", StartPosition = 23, Length = 6, SourceDefinitionId = srcGeneric.Id, FormatMask = "yyyyMMdd", SortOrder = 4, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record1Variant.Id, FieldCode = "FileCreationTime", FieldNameEs = "FileCreationTime", StartPosition = 29, Length = 4, SourceDefinitionId = srcGeneric.Id, FormatMask = "HH:mm", SortOrder = 5, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record1Variant.Id, FieldCode = "FileIdModifier", FieldNameEs = "FileIdModifier", StartPosition = 33, Length = 1, SourceDefinitionId = srcFileId.Id, SortOrder = 6, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record1Variant.Id, FieldCode = "RecordSize", FieldNameEs = "RecordSize", StartPosition = 34, Length = 3, SourceDefinitionId = srcRecordSize.Id, SortOrder = 7, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record1Variant.Id, FieldCode = "BlockingFactor", FieldNameEs = "BlockingFactor", StartPosition = 37, Length = 2, SourceDefinitionId = srcBlocking.Id, SortOrder = 8, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record1Variant.Id, FieldCode = "FormatCode", FieldNameEs = "FormatCode", StartPosition = 39, Length = 1, SourceDefinitionId = srcFormat.Id, SortOrder = 9, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record1Variant.Id, FieldCode = "ImmediateDestinationName", FieldNameEs = "ImmediateDestinationName", StartPosition = 40, Length = 23, SourceDefinitionId = srcGeneric.Id, SortOrder = 10, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record1Variant.Id, FieldCode = "ImmediateOriginName", FieldNameEs = "ImmediateOriginName", StartPosition = 63, Length = 23, SourceDefinitionId = srcGeneric.Id, SortOrder = 11, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record1Variant.Id, FieldCode = "ReferenceCode", FieldNameEs = "ReferenceCode", StartPosition = 86, Length = 8, SourceDefinitionId = srcGeneric.Id, SortOrder = 12, IsEnabled = true },

            new CfgLayoutField { LayoutVariantId = record5Variant.Id, FieldCode = "ServiceClassCode", FieldNameEs = "ServiceClassCode", StartPosition = 2, Length = 3, SourceDefinitionId = srcGeneric.Id, SortOrder = 1, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record5Variant.Id, FieldCode = "CompanyName", FieldNameEs = "CompanyName", StartPosition = 5, Length = 16, SourceDefinitionId = srcGeneric.Id, SortOrder = 2, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record5Variant.Id, FieldCode = "CompanyDiscretionaryData", FieldNameEs = "CompanyDiscretionaryData", StartPosition = 21, Length = 20, SourceDefinitionId = srcGeneric.Id, SortOrder = 3, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record5Variant.Id, FieldCode = "CompanyIdentification", FieldNameEs = "CompanyIdentification", StartPosition = 41, Length = 10, SourceDefinitionId = srcGeneric.Id, SortOrder = 4, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record5Variant.Id, FieldCode = "StandardEntryClassCode", FieldNameEs = "StandardEntryClassCode", StartPosition = 51, Length = 3, SourceDefinitionId = srcSec.Id, SortOrder = 5, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record5Variant.Id, FieldCode = "CompanyEntryDescription", FieldNameEs = "CompanyEntryDescription", StartPosition = 54, Length = 10, SourceDefinitionId = srcGeneric.Id, SortOrder = 6, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record5Variant.Id, FieldCode = "CompanyDescriptiveDate", FieldNameEs = "CompanyDescriptiveDate", StartPosition = 64, Length = 6, SourceDefinitionId = srcGeneric.Id, FormatMask = "yyyyMMdd", SortOrder = 7, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record5Variant.Id, FieldCode = "EffectiveEntryDate", FieldNameEs = "EffectiveEntryDate", StartPosition = 70, Length = 6, SourceDefinitionId = srcGeneric.Id, FormatMask = "ddMMyy", SortOrder = 8, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record5Variant.Id, FieldCode = "SettlementDate", FieldNameEs = "SettlementDate", StartPosition = 76, Length = 3, SourceDefinitionId = srcGeneric.Id, SortOrder = 9, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record5Variant.Id, FieldCode = "OriginatorStatusCode", FieldNameEs = "OriginatorStatusCode", StartPosition = 79, Length = 1, SourceDefinitionId = srcGeneric.Id, SortOrder = 10, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record5Variant.Id, FieldCode = "OriginatingDFI", FieldNameEs = "OriginatingDFI", StartPosition = 80, Length = 8, SourceDefinitionId = srcDfi.Id, SortOrder = 11, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record5Variant.Id, FieldCode = "BatchNumber", FieldNameEs = "BatchNumber", StartPosition = 88, Length = 7, SourceDefinitionId = srcGeneric.Id, SortOrder = 12, IsEnabled = true }
        );
        await context.SaveChangesAsync();

        var validation = new NachaConfigValidationService(context);
        var result = await validation.ValidateBeforePublishAsync(profile.Id);

        result.IsValid.Should().BeFalse();
        result.Issues.Should().Contain(x => x.Codigo == "INVALID_CONSTANT_VALUE");
        result.Issues.Should().Contain(x => x.Codigo == "INVALID_DATE_FORMAT");
        result.Issues.Should().Contain(x => x.Codigo == "INVALID_HEADER_COHERENCE");
        result.Issues.Should().Contain(x => x.Codigo == "HEADER_RULE_ACH_INVALID");
    }

    [Fact]
    public async Task ValidateBeforePublishAsync_ShouldApplyCenitSettlementPolicy_AndHeaderRules()
    {
        await using var context = await CreateSqliteContextAsync();
        var profile = await SeedProfileGraphAsync(context);
        profile = await context.CfgProfiles.SingleAsync(x => x.Id == profile.Id);
        profile.ClearingHouseId = 2; // CENIT
        await context.SaveChangesAsync();

        var constante = await context.CatDataSourceTypes.SingleAsync(x => x.Code == "CONSTANTE");

        var record1Variant = await context.CfgLayoutVariants.Include(x => x.RecordCode).SingleAsync(x => x.ProfileId == profile.Id && x.RecordCode.Code == "1");
        var record5Variant = await context.CfgLayoutVariants.Include(x => x.RecordCode).SingleAsync(x => x.ProfileId == profile.Id && x.RecordCode.Code == "5");
        context.CfgLayoutFields.RemoveRange(await context.CfgLayoutFields.Where(x => x.LayoutVariantId == record1Variant.Id || x.LayoutVariantId == record5Variant.Id).ToListAsync());
        await context.SaveChangesAsync();

        var srcOrigin = new CfgFieldSourceDefinition { DataSourceTypeId = constante.Id, ConstantValue = "1234567890" }; // inválido para CENIT
        var srcDestination = new CfgFieldSourceDefinition { DataSourceTypeId = constante.Id, ConstantValue = "87654321" };
        var srcFileId = new CfgFieldSourceDefinition { DataSourceTypeId = constante.Id, ConstantValue = "1" }; // CENIT requiere letra
        var srcSettlement = new CfgFieldSourceDefinition { DataSourceTypeId = constante.Id, ConstantValue = "250" }; // CENIT debe ir vacío
        var srcSec = new CfgFieldSourceDefinition { DataSourceTypeId = constante.Id, ConstantValue = "PPD" };
        var srcGeneric = new CfgFieldSourceDefinition { DataSourceTypeId = constante.Id, ConstantValue = "X" };
        context.CfgFieldSourceDefinitions.AddRange(srcOrigin, srcDestination, srcFileId, srcSettlement, srcSec, srcGeneric);
        await context.SaveChangesAsync();

        context.CfgLayoutFields.AddRange(
            new CfgLayoutField { LayoutVariantId = record1Variant.Id, FieldCode = "PriorityCode", FieldNameEs = "PriorityCode", StartPosition = 1, Length = 2, SourceDefinitionId = srcGeneric.Id, SortOrder = 1, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record1Variant.Id, FieldCode = "ImmediateDestination", FieldNameEs = "ImmediateDestination", StartPosition = 3, Length = 10, SourceDefinitionId = srcDestination.Id, SortOrder = 2, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record1Variant.Id, FieldCode = "ImmediateOrigin", FieldNameEs = "ImmediateOrigin", StartPosition = 13, Length = 10, SourceDefinitionId = srcOrigin.Id, SortOrder = 3, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record1Variant.Id, FieldCode = "FileCreationDate", FieldNameEs = "FileCreationDate", StartPosition = 23, Length = 6, SourceDefinitionId = srcGeneric.Id, FormatMask = "yyMMdd", SortOrder = 4, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record1Variant.Id, FieldCode = "FileCreationTime", FieldNameEs = "FileCreationTime", StartPosition = 29, Length = 4, SourceDefinitionId = srcGeneric.Id, FormatMask = "HHmm", SortOrder = 5, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record1Variant.Id, FieldCode = "FileIdModifier", FieldNameEs = "FileIdModifier", StartPosition = 33, Length = 1, SourceDefinitionId = srcFileId.Id, SortOrder = 6, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record1Variant.Id, FieldCode = "RecordSize", FieldNameEs = "RecordSize", StartPosition = 34, Length = 3, SourceDefinitionId = srcGeneric.Id, SortOrder = 7, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record1Variant.Id, FieldCode = "BlockingFactor", FieldNameEs = "BlockingFactor", StartPosition = 37, Length = 2, SourceDefinitionId = srcGeneric.Id, SortOrder = 8, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record1Variant.Id, FieldCode = "FormatCode", FieldNameEs = "FormatCode", StartPosition = 39, Length = 1, SourceDefinitionId = srcGeneric.Id, SortOrder = 9, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record1Variant.Id, FieldCode = "ImmediateDestinationName", FieldNameEs = "ImmediateDestinationName", StartPosition = 40, Length = 23, SourceDefinitionId = srcGeneric.Id, SortOrder = 10, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record1Variant.Id, FieldCode = "ImmediateOriginName", FieldNameEs = "ImmediateOriginName", StartPosition = 63, Length = 23, SourceDefinitionId = srcGeneric.Id, SortOrder = 11, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record1Variant.Id, FieldCode = "ReferenceCode", FieldNameEs = "ReferenceCode", StartPosition = 86, Length = 8, SourceDefinitionId = srcGeneric.Id, SortOrder = 12, IsEnabled = true },

            new CfgLayoutField { LayoutVariantId = record5Variant.Id, FieldCode = "ServiceClassCode", FieldNameEs = "ServiceClassCode", StartPosition = 2, Length = 3, SourceDefinitionId = srcGeneric.Id, SortOrder = 1, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record5Variant.Id, FieldCode = "CompanyName", FieldNameEs = "CompanyName", StartPosition = 5, Length = 16, SourceDefinitionId = srcGeneric.Id, SortOrder = 2, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record5Variant.Id, FieldCode = "CompanyDiscretionaryData", FieldNameEs = "CompanyDiscretionaryData", StartPosition = 21, Length = 20, SourceDefinitionId = srcGeneric.Id, SortOrder = 3, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record5Variant.Id, FieldCode = "CompanyIdentification", FieldNameEs = "CompanyIdentification", StartPosition = 41, Length = 10, SourceDefinitionId = srcGeneric.Id, SortOrder = 4, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record5Variant.Id, FieldCode = "StandardEntryClassCode", FieldNameEs = "StandardEntryClassCode", StartPosition = 51, Length = 3, SourceDefinitionId = srcSec.Id, SortOrder = 5, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record5Variant.Id, FieldCode = "CompanyEntryDescription", FieldNameEs = "CompanyEntryDescription", StartPosition = 54, Length = 10, SourceDefinitionId = srcGeneric.Id, SortOrder = 6, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record5Variant.Id, FieldCode = "CompanyDescriptiveDate", FieldNameEs = "CompanyDescriptiveDate", StartPosition = 64, Length = 6, SourceDefinitionId = srcGeneric.Id, FormatMask = "yyMMdd", SortOrder = 7, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record5Variant.Id, FieldCode = "EffectiveEntryDate", FieldNameEs = "EffectiveEntryDate", StartPosition = 70, Length = 6, SourceDefinitionId = srcGeneric.Id, FormatMask = "yyMMdd", SortOrder = 8, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record5Variant.Id, FieldCode = "SettlementDate", FieldNameEs = "SettlementDate", StartPosition = 76, Length = 3, SourceDefinitionId = srcSettlement.Id, SortOrder = 9, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record5Variant.Id, FieldCode = "OriginatorStatusCode", FieldNameEs = "OriginatorStatusCode", StartPosition = 79, Length = 1, SourceDefinitionId = srcGeneric.Id, SortOrder = 10, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record5Variant.Id, FieldCode = "OriginatingDFI", FieldNameEs = "OriginatingDFI", StartPosition = 80, Length = 8, SourceDefinitionId = srcGeneric.Id, SortOrder = 11, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record5Variant.Id, FieldCode = "BatchNumber", FieldNameEs = "BatchNumber", StartPosition = 88, Length = 7, SourceDefinitionId = srcGeneric.Id, SortOrder = 12, IsEnabled = true }
        );
        await context.SaveChangesAsync();

        var validation = new NachaConfigValidationService(context);
        var result = await validation.ValidateBeforePublishAsync(profile.Id);

        result.IsValid.Should().BeFalse();
        result.Issues.Should().Contain(x => x.Codigo == "HEADER_RULE_CENIT_INVALID");
        result.Issues.Should().Contain(x => x.Codigo == "INVALID_SETTLEMENT_POLICY");
    }

    [Fact]
    public async Task ValidateBeforePublishAsync_ShouldRejectConstantControlFields_ForRecord8And9()
    {
        await using var context = await CreateSqliteContextAsync();
        var profile = await SeedProfileGraphAsync(context);
        var constante = await context.CatDataSourceTypes.SingleAsync(x => x.Code == "CONSTANTE");

        var record8Variant = await context.CfgLayoutVariants.Include(x => x.RecordCode).SingleAsync(x => x.ProfileId == profile.Id && x.RecordCode.Code == "8");
        var record9Variant = await context.CfgLayoutVariants.Include(x => x.RecordCode).SingleAsync(x => x.ProfileId == profile.Id && x.RecordCode.Code == "9");
        context.CfgLayoutFields.RemoveRange(await context.CfgLayoutFields.Where(x => x.LayoutVariantId == record8Variant.Id || x.LayoutVariantId == record9Variant.Id).ToListAsync());
        await context.SaveChangesAsync();

        var srcConstant = new CfgFieldSourceDefinition { DataSourceTypeId = constante.Id, ConstantValue = "1" };
        context.CfgFieldSourceDefinitions.Add(srcConstant);
        await context.SaveChangesAsync();

        context.CfgLayoutFields.AddRange(
            new CfgLayoutField { LayoutVariantId = record8Variant.Id, FieldCode = "ServiceClassCode", FieldNameEs = "ServiceClassCode", StartPosition = 2, Length = 3, SourceDefinitionId = srcConstant.Id, SortOrder = 1, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record8Variant.Id, FieldCode = "EntryAddendaCount", FieldNameEs = "EntryAddendaCount", StartPosition = 5, Length = 6, SourceDefinitionId = srcConstant.Id, SortOrder = 2, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record8Variant.Id, FieldCode = "EntryHash", FieldNameEs = "EntryHash", StartPosition = 11, Length = 10, SourceDefinitionId = srcConstant.Id, SortOrder = 3, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record8Variant.Id, FieldCode = "TotalDebitAmount", FieldNameEs = "TotalDebitAmount", StartPosition = 21, Length = 12, SourceDefinitionId = srcConstant.Id, SortOrder = 4, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record8Variant.Id, FieldCode = "TotalCreditAmount", FieldNameEs = "TotalCreditAmount", StartPosition = 33, Length = 12, SourceDefinitionId = srcConstant.Id, SortOrder = 5, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record8Variant.Id, FieldCode = "CompanyIdentification", FieldNameEs = "CompanyIdentification", StartPosition = 45, Length = 10, SourceDefinitionId = srcConstant.Id, SortOrder = 6, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record8Variant.Id, FieldCode = "OriginatingDFI", FieldNameEs = "OriginatingDFI", StartPosition = 80, Length = 8, SourceDefinitionId = srcConstant.Id, SortOrder = 7, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record8Variant.Id, FieldCode = "BatchNumber", FieldNameEs = "BatchNumber", StartPosition = 88, Length = 7, SourceDefinitionId = srcConstant.Id, SortOrder = 8, IsEnabled = true },

            new CfgLayoutField { LayoutVariantId = record9Variant.Id, FieldCode = "BatchCount", FieldNameEs = "BatchCount", StartPosition = 2, Length = 6, SourceDefinitionId = srcConstant.Id, SortOrder = 1, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record9Variant.Id, FieldCode = "BlockCount", FieldNameEs = "BlockCount", StartPosition = 8, Length = 6, SourceDefinitionId = srcConstant.Id, SortOrder = 2, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record9Variant.Id, FieldCode = "EntryAddendaCount", FieldNameEs = "EntryAddendaCount", StartPosition = 14, Length = 8, SourceDefinitionId = srcConstant.Id, SortOrder = 3, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record9Variant.Id, FieldCode = "EntryHash", FieldNameEs = "EntryHash", StartPosition = 22, Length = 10, SourceDefinitionId = srcConstant.Id, SortOrder = 4, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record9Variant.Id, FieldCode = "TotalDebitAmount", FieldNameEs = "TotalDebitAmount", StartPosition = 32, Length = 12, SourceDefinitionId = srcConstant.Id, SortOrder = 5, IsEnabled = true },
            new CfgLayoutField { LayoutVariantId = record9Variant.Id, FieldCode = "TotalCreditAmount", FieldNameEs = "TotalCreditAmount", StartPosition = 44, Length = 12, SourceDefinitionId = srcConstant.Id, SortOrder = 6, IsEnabled = true }
        );
        await context.SaveChangesAsync();

        var validation = new NachaConfigValidationService(context);
        var result = await validation.ValidateBeforePublishAsync(profile.Id);

        result.IsValid.Should().BeFalse();
        result.Issues.Should().Contain(x => x.Codigo == "CONTROL_FIELD_MUST_BE_RUNTIME");
    }

    [Fact]
    public async Task PublishAsync_ShouldRejectAchDraftThatDoesNotMatchOfficialDescriptor()
    {
        await using var context = await CreateSqliteContextAsync();
        var profile = await SeedProfileGraphAsync(context);
        var validation = new NachaConfigValidationService(context);
        var publication = new NachaConfigPublicationService(context, validation);
        var validationResult = await validation.ValidateBeforePublishAsync(profile.Id);

        var result = await publication.PublishAsync(profile.Id, "publisher", Convert.ToBase64String(profile.RowVersion));

        result.Publicado.Should().BeFalse();
        validationResult.Issues.Should().Contain(issue => issue.Codigo.StartsWith("ACHCOL_", StringComparison.Ordinal));
        context.HistConfigSnapshots.Should().NotContain(x => x.ProfileId == profile.Id && x.SnapshotType == "PUBLISH");
        context.HistConfigChanges.Should().NotContain(x => x.ProfileId == profile.Id && x.ChangeType == "PUBLISH");
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
        var profileEntity = await context.CfgProfiles.SingleAsync(x => x.Id == profile.Id);
        profileEntity.StatusId = await context.CatConfigStatuses
            .Where(x => x.Code == "PUBLICADO")
            .Select(x => x.Id)
            .SingleAsync();
        context.CfgProfiles.RemoveRange(await context.CfgProfiles.Where(x => x.Id != profile.Id).ToListAsync());
        await context.SaveChangesAsync();
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

    private static async Task<CfgProfile> SetProfileStatusAsync(AchDbContext context, int profileId, string statusCode)
    {
        var profile = await context.CfgProfiles.SingleAsync(x => x.Id == profileId);
        profile.StatusId = await context.CatConfigStatuses.Where(x => x.Code == statusCode).Select(x => x.Id).SingleAsync();
        await context.SaveChangesAsync();
        return await context.CfgProfiles.AsNoTracking().SingleAsync(x => x.Id == profileId);
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
        if (await context.CatClearingHouses.AnyAsync())
        {
            return;
        }

        context.CatClearingHouses.AddRange(
            new CatClearingHouse { Id = 1, Code = "ACH", Name = "ACH Colombia", IsActive = true },
            new CatClearingHouse { Id = 2, Code = "CENIT", Name = "CENIT", IsActive = true });
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
        context.CatRuleTypes.Add(new CatRuleType { Id = 1, Code = "REQUIRED", NameEs = "Required" });
        await context.SaveChangesAsync();
    }

    private static async Task<CfgProfile> CreateDraftWithoutRecordsAsync(AchDbContext context)
    {
        await SeedCatalogAsync(context);
        var achId = await context.CatClearingHouses.Where(x => x.Code == "ACH").Select(x => x.Id).SingleAsync();
        var flowId = await context.CatFlowTypes.Where(x => x.Code == "ORIGINAL").Select(x => x.Id).SingleAsync();
        var directionId = await context.CatDirections.Where(x => x.Code == "SALIDA").Select(x => x.Id).SingleAsync();
        var serviceClassId = await context.CatServiceClasses.Where(x => x.Code == "PPD").Select(x => x.Id).SingleAsync();
        var draftStatusId = await context.CatConfigStatuses.Where(x => x.Code == "BORRADOR").Select(x => x.Id).SingleAsync();
        var profile = new CfgProfile
        {
            ProfileCode = "P_NO_RECORDS",
            NameEs = "Sin records",
            ClearingHouseId = achId,
            FlowTypeId = flowId,
            DirectionId = directionId,
            ServiceClassId = serviceClassId,
            StatusId = draftStatusId,
            EffectiveFrom = DateTime.UtcNow.Date
        };
        context.CfgProfiles.Add(profile);
        await context.SaveChangesAsync();
        return profile;
    }

    private static async Task<CfgProfile> SeedProfileGraphAsync(AchDbContext context)
    {
        await SeedCatalogAsync(context);
        var achId = await context.CatClearingHouses.Where(x => x.Code == "ACH").Select(x => x.Id).SingleAsync();
        var flowId = await context.CatFlowTypes.Where(x => x.Code == "ORIGINAL").Select(x => x.Id).SingleAsync();
        var directionId = await context.CatDirections.Where(x => x.Code == "SALIDA").Select(x => x.Id).SingleAsync();
        var serviceClassId = await context.CatServiceClasses.Where(x => x.Code == "PPD").Select(x => x.Id).SingleAsync();
        var draftStatusId = await context.CatConfigStatuses.Where(x => x.Code == "BORRADOR").Select(x => x.Id).SingleAsync();
        var publishedStatusId = await context.CatConfigStatuses.Where(x => x.Code == "PUBLICADO").Select(x => x.Id).SingleAsync();
        var sourceTypeConstId = await context.CatDataSourceTypes.Where(x => x.Code == "CONSTANTE").Select(x => x.Id).SingleAsync();
        var recordCodeByCode = await context.CatRecordCodes.ToDictionaryAsync(x => x.Code, x => x.Id);

        var profile = new CfgProfile
        {
            ProfileCode = "P_VALID_01",
            NameEs = "Perfil válido",
            ClearingHouseId = achId,
            FlowTypeId = flowId,
            DirectionId = directionId,
            ServiceClassId = serviceClassId,
            StatusId = draftStatusId,
            EffectiveFrom = DateTime.UtcNow.Date.AddDays(-2),
            RowVersion = [1, 0, 0]
        };
        context.CfgProfiles.Add(profile);
        await context.SaveChangesAsync();

        context.CfgProfileRecords.AddRange(
            new CfgProfileRecord { ProfileId = profile.Id, RecordCodeId = recordCodeByCode["1"], Sequence = 10, IsEnabled = true, MinOccurs = 1, SourceStrategy = "TABLE_DRIVEN" },
            new CfgProfileRecord { ProfileId = profile.Id, RecordCodeId = recordCodeByCode["5"], Sequence = 20, IsEnabled = true, MinOccurs = 1, SourceStrategy = "TABLE_DRIVEN" },
            new CfgProfileRecord { ProfileId = profile.Id, RecordCodeId = recordCodeByCode["6"], Sequence = 30, IsEnabled = true, MinOccurs = 1, SourceStrategy = "TABLE_DRIVEN" },
            new CfgProfileRecord { ProfileId = profile.Id, RecordCodeId = recordCodeByCode["8"], Sequence = 40, IsEnabled = true, MinOccurs = 1, SourceStrategy = "TABLE_DRIVEN" },
            new CfgProfileRecord { ProfileId = profile.Id, RecordCodeId = recordCodeByCode["9"], Sequence = 50, IsEnabled = true, MinOccurs = 1, SourceStrategy = "TABLE_DRIVEN" });

        var source = new CfgFieldSourceDefinition { DataSourceTypeId = sourceTypeConstId, ConstantValue = "1" };
        context.CfgFieldSourceDefinitions.Add(source);
        await context.SaveChangesAsync();

        var variants = new[]
        {
            new CfgLayoutVariant { ProfileId = profile.Id, RecordCodeId = recordCodeByCode["1"], VariantCode = "R1", NameEs = "R1", Priority = 10, EffectiveFrom = DateTime.UtcNow.Date.AddDays(-1), StatusId = publishedStatusId, TotalLength = 106, IsDefaultForRecord = true },
            new CfgLayoutVariant { ProfileId = profile.Id, RecordCodeId = recordCodeByCode["5"], VariantCode = "R5", NameEs = "R5", Priority = 10, EffectiveFrom = DateTime.UtcNow.Date.AddDays(-1), StatusId = publishedStatusId, TotalLength = 106, IsDefaultForRecord = true },
            new CfgLayoutVariant { ProfileId = profile.Id, RecordCodeId = recordCodeByCode["6"], VariantCode = "R6", NameEs = "R6", Priority = 10, EffectiveFrom = DateTime.UtcNow.Date.AddDays(-1), StatusId = publishedStatusId, TotalLength = 106, IsDefaultForRecord = true },
            new CfgLayoutVariant { ProfileId = profile.Id, RecordCodeId = recordCodeByCode["8"], VariantCode = "R8", NameEs = "R8", Priority = 10, EffectiveFrom = DateTime.UtcNow.Date.AddDays(-1), StatusId = publishedStatusId, TotalLength = 106, IsDefaultForRecord = true },
            new CfgLayoutVariant { ProfileId = profile.Id, RecordCodeId = recordCodeByCode["9"], VariantCode = "R9", NameEs = "R9", Priority = 10, EffectiveFrom = DateTime.UtcNow.Date.AddDays(-1), StatusId = publishedStatusId, TotalLength = 106, IsDefaultForRecord = true }
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
        var ruleTypeId = await context.CatRuleTypes.Where(x => x.Code == "REQUIRED").Select(x => x.Id).SingleAsync();
        var firstFieldId = await context.CfgLayoutFields
            .Where(x => x.LayoutVariant.ProfileId == profile.Id)
            .OrderBy(x => x.Id)
            .Select(x => x.Id)
            .FirstAsync();

        context.CfgFieldRules.Add(new CfgFieldRule
        {
            LayoutFieldId = firstFieldId,
            RuleTypeId = ruleTypeId,
            RuleCode = "R1",
            ErrorCode = "ERR_REQUIRED",
            ErrorMessageEs = "Campo requerido",
            Severity = "ERROR",
            Order = 1,
            IsEnabled = true
        });
        await context.SaveChangesAsync();
        return await context.CfgProfiles.AsNoTracking().SingleAsync(x => x.Id == profile.Id);
    }
}
