using System.Text.Json;
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

public sealed class NachaConfigOperationalTestServiceTests
{
    [Fact]
    public async Task TestAsync_ShouldRenderFixedValueAndCalculation_WithOfficialRenderer()
    {
        await using var context = await CreateContextAsync();
        var profile = await SeedPreviewProfileAsync(context, "ACH", "BORRADOR");
        var service = new NachaConfigOperationalTestService(
            context,
            StubValidation.Valid(profile.Id),
            new RecordingPreviewService(profile.Id, profile.ProfileCode));

        var result = await service.TestAsync(profile.Id);

        result.Should().NotBeNull();
        result!.CanPublish.Should().BeTrue();
        result.Records.Should().ContainSingle();
        var preview = result.Records.Single();
        preview.ContentPreviewAvailable.Should().BeTrue();
        preview.RenderedRecord.Should().Be("6ABXXX");
        preview.Fields.Single(x => x.FieldNameEs == "Valor fijo").SampleValue.Should().Be("AB");
        preview.Fields.Single(x => x.FieldNameEs == "Cálculo de ejemplo").SourceSummaryEs.Should().Contain("Cálculo configurado");
        preview.Fields.Single(x => x.FieldNameEs == "Cálculo de ejemplo").SampleValue.Should().Be("XXX");
    }

    [Fact]
    public async Task TestAsync_ShouldHumanizeOverlapLengthMissingSourceAndInvalidRule()
    {
        await using var context = await CreateContextAsync();
        var profile = await SeedPreviewProfileAsync(context, "ACH", "BORRADOR");
        var field = await context.CfgLayoutFields.Include(x => x.LayoutVariant).ThenInclude(x => x.RecordCode).FirstAsync(x => x.FieldNameEs == "Valor fijo");
        var issues = new[]
        {
            new NachaConfigValidationIssueDto { Severidad = "ERROR", Codigo = "FIELD_OVERLAP", Mensaje = $"Solapamiento de fields en variante {field.LayoutVariant.VariantCode}: RECORDTYPE y {field.FieldCode}." },
            new NachaConfigValidationIssueDto { Severidad = "ERROR", Codigo = "INVALID_RECORD_LENGTH", Mensaje = $"La variante {field.LayoutVariant.VariantCode} tiene longitud inválida." },
            new NachaConfigValidationIssueDto { Severidad = "ERROR", Codigo = "MISSING_SOURCE", Mensaje = $"Field {field.FieldCode} sin source crítico." },
            new NachaConfigValidationIssueDto { Severidad = "WARN", Codigo = "INVALID_FIELD_RULE_CONFIG", Mensaje = $"Field {field.FieldCode} tiene RuleConfigJson inválido." }
        };
        var service = new NachaConfigOperationalTestService(
            context,
            StubValidation.WithIssues(profile.Id, issues),
            new RecordingPreviewService(profile.Id, profile.ProfileCode));

        var result = await service.TestAsync(profile.Id);

        result.Should().NotBeNull();
        result!.CanPublish.Should().BeFalse();
        result.ResultLabelEs.Should().Be("Requiere correcciones");
        result.Issues.Should().Contain(x => x.Code == "FIELD_OVERLAP" && x.MessageEs.Contains("posiciones superpuestas"));
        result.Issues.Should().Contain(x => x.Code == "INVALID_RECORD_LENGTH" && x.RecordNameEs == "Detalle de transacción");
        result.Issues.Should().Contain(x => x.Code == "MISSING_SOURCE" && x.FieldNameEs == "Valor fijo" && x.SuggestedActionEs != null);
        result.Issues.Should().Contain(x => x.Code == "INVALID_FIELD_RULE_CONFIG" && x.Severity == "WARN");
    }

    [Fact]
    public async Task TestAsync_ShouldKeepPublishedProfileReadOnly_AndNotPersistChanges()
    {
        await using var context = await CreateContextAsync();
        var profile = await SeedPreviewProfileAsync(context, "ACH", "PUBLICADO");
        var beforeChanges = await context.HistConfigChanges.CountAsync();
        var beforeFields = await context.CfgLayoutFields.AsNoTracking().Select(x => new { x.Id, x.StartPosition, x.Length, x.SourceDefinitionId }).ToListAsync();
        var service = new NachaConfigOperationalTestService(
            context,
            StubValidation.Valid(profile.Id),
            new RecordingPreviewService(profile.Id, profile.ProfileCode));

        var result = await service.TestAsync(profile.Id);

        result.Should().NotBeNull();
        (await context.HistConfigChanges.CountAsync()).Should().Be(beforeChanges);
        var afterFields = await context.CfgLayoutFields.AsNoTracking().Select(x => new { x.Id, x.StartPosition, x.Length, x.SourceDefinitionId }).ToListAsync();
        afterFields.Should().BeEquivalentTo(beforeFields);
    }

    [Fact]
    public async Task TestAsync_ShouldKeepAchAndCenitSelectionContextsIsolated()
    {
        await using var context = await CreateContextAsync();
        var ach = await SeedPreviewProfileAsync(context, "ACH", "BORRADOR");
        var cenit = await SeedPreviewProfileAsync(context, "CENIT", "BORRADOR");
        var preview = new RecordingPreviewService(ach.Id, ach.ProfileCode);
        var service = new NachaConfigOperationalTestService(context, StubValidation.Valid(ach.Id), preview);

        await service.TestAsync(ach.Id);
        preview.LastRequest!.CamaraCode.Should().Be("ACH");

        service = new NachaConfigOperationalTestService(context, StubValidation.Valid(cenit.Id), preview);
        await service.TestAsync(cenit.Id);
        preview.LastRequest!.CamaraCode.Should().Be("CENIT");
    }

    private static async Task<AchDbContext> CreateContextAsync()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();
        var context = new AchDbContext(new DbContextOptionsBuilder<AchDbContext>().UseSqlite(connection).Options);
        await context.Database.EnsureCreatedAsync();
        await SeedCatalogsAsync(context);
        return context;
    }

    private static async Task SeedCatalogsAsync(AchDbContext context)
    {
        context.CatClearingHouses.AddRange(
            new CatClearingHouse { Code = "ACH", Name = "ACH Colombia", IsActive = true },
            new CatClearingHouse { Code = "CENIT", Name = "CENIT", IsActive = true });
        context.CatFlowTypes.Add(new CatFlowType { Code = "ORIGINAL", NameEs = "Operaciones originales", IsActive = true });
        context.CatDirections.Add(new CatDirection { Code = "SALIDA", NameEs = "Salida", IsActive = true });
        context.CatServiceClasses.AddRange(
            new CatServiceClass { Code = "PPD", NameEs = "Pago preacordado", IsActive = true });
        context.CatConfigStatuses.AddRange(
            new CatConfigStatus { Code = "BORRADOR", IsEditable = true, IsPublishable = true },
            new CatConfigStatus { Code = "PUBLICADO", IsEditable = false, IsPublishable = false });
        context.CatRecordCodes.Add(new CatRecordCode { Code = "6", NameEs = "Detalle de transacción", IsMandatoryBase = true });
        context.CatDataSourceTypes.AddRange(
            new CatDataSourceType { Code = "CONSTANTE", NameEs = "Valor fijo" },
            new CatDataSourceType { Code = "EXPRESION", NameEs = "Cálculo" });
        context.CatRuleTypes.Add(new CatRuleType { Code = "FORMAT", NameEs = "Formato del campo" });
        await context.SaveChangesAsync();
    }

    private static async Task<CfgProfile> SeedPreviewProfileAsync(AchDbContext context, string chamberCode, string statusCode)
    {
        var chamber = await context.CatClearingHouses.SingleAsync(x => x.Code == chamberCode);
        var flow = await context.CatFlowTypes.SingleAsync();
        var direction = await context.CatDirections.SingleAsync();
        var service = await context.CatServiceClasses.SingleAsync();
        var status = await context.CatConfigStatuses.SingleAsync(x => x.Code == statusCode);
        var recordCode = await context.CatRecordCodes.SingleAsync();
        var ruleType = await context.CatRuleTypes.SingleAsync();
        var constantType = await context.CatDataSourceTypes.SingleAsync(x => x.Code == "CONSTANTE");
        var expressionType = await context.CatDataSourceTypes.SingleAsync(x => x.Code == "EXPRESION");

        var profile = new CfgProfile
        {
            ProfileCode = $"{chamberCode}_{statusCode}_{Guid.NewGuid():N}",
            NameEs = $"Perfil {chamberCode}",
            ClearingHouseId = chamber.Id,
            FlowTypeId = flow.Id,
            DirectionId = direction.Id,
            ServiceClassId = service.Id,
            StatusId = status.Id,
            EffectiveFrom = DateTime.UtcNow.Date.AddDays(-1),
            RowVersion = [1]
        };
        context.CfgProfiles.Add(profile);
        await context.SaveChangesAsync();

        var variant = new CfgLayoutVariant
        {
            ProfileId = profile.Id,
            RecordCodeId = recordCode.Id,
            VariantCode = $"{chamberCode}_R6",
            NameEs = "Detalle estándar",
            Priority = 1,
            EffectiveFrom = profile.EffectiveFrom,
            StatusId = status.Id,
            TotalLength = 6,
            IsDefaultForRecord = true
        };
        context.CfgLayoutVariants.Add(variant);
        await context.SaveChangesAsync();
        context.CfgProfileRecords.Add(new CfgProfileRecord
        {
            ProfileId = profile.Id,
            RecordCodeId = recordCode.Id,
            Sequence = 1,
            IsEnabled = true,
            MinOccurs = 1,
            SourceStrategy = "TABLE_DRIVEN",
            LayoutVariantId = variant.Id
        });

        var recordTypeSource = new CfgFieldSourceDefinition { DataSourceTypeId = constantType.Id, ConstantValue = "6" };
        var fixedSource = new CfgFieldSourceDefinition { DataSourceTypeId = constantType.Id, ConstantValue = "AB" };
        var calculationSource = new CfgFieldSourceDefinition
        {
            DataSourceTypeId = expressionType.Id,
            ExpressionDsl = JsonSerializer.Serialize(new { calculationType = "SAMPLE_CALC" })
        };
        context.CfgFieldSourceDefinitions.AddRange(recordTypeSource, fixedSource, calculationSource);
        await context.SaveChangesAsync();

        var fields = new[]
        {
            new CfgLayoutField { LayoutVariantId = variant.Id, FieldCode = "RECORDTYPE", FieldNameEs = "Tipo de registro", StartPosition = 1, Length = 1, PadChar = ' ', Justification = 'L', SortOrder = 1, IsEnabled = true, SourceDefinitionId = recordTypeSource.Id },
            new CfgLayoutField { LayoutVariantId = variant.Id, FieldCode = "FIXED", FieldNameEs = "Valor fijo", StartPosition = 2, Length = 2, PadChar = ' ', Justification = 'L', SortOrder = 2, IsEnabled = true, SourceDefinitionId = fixedSource.Id },
            new CfgLayoutField { LayoutVariantId = variant.Id, FieldCode = "CALC", FieldNameEs = "Cálculo de ejemplo", StartPosition = 4, Length = 3, PadChar = ' ', Justification = 'L', SortOrder = 3, IsEnabled = true, SourceDefinitionId = calculationSource.Id }
        };
        context.CfgLayoutFields.AddRange(fields);
        await context.SaveChangesAsync();

        foreach (var field in fields)
        {
            context.CfgFieldRules.Add(new CfgFieldRule
            {
                LayoutFieldId = field.Id,
                RuleTypeId = ruleType.Id,
                RuleCode = $"RULE_{field.FieldCode}",
                ErrorCode = $"ERR_{field.FieldCode}",
                ErrorMessageEs = $"Validar {field.FieldNameEs}",
                Severity = "ERROR",
                Order = 1,
                IsEnabled = true,
                RuleConfigJson = JsonSerializer.Serialize(new
                {
                    recordType = "6",
                    field = field.FieldCode,
                    startPosition = field.StartPosition,
                    length = field.Length,
                    dataType = "ALPHANUMERIC",
                    required = true,
                    normalizer = "NONE",
                    overflowPolicy = "REJECT",
                    allowedValues = field.FieldCode == "RECORDTYPE" ? new[] { "6" } : Array.Empty<string>()
                })
            });
        }
        await context.SaveChangesAsync();
        return await context.CfgProfiles.AsNoTracking().SingleAsync(x => x.Id == profile.Id);
    }

    private sealed class StubValidation : INachaConfigValidationService
    {
        private readonly NachaConfigValidationResultDto _result;
        private StubValidation(NachaConfigValidationResultDto result) => _result = result;

        public static StubValidation Valid(int profileId) => new(new NachaConfigValidationResultDto
        {
            ProfileId = profileId,
            IsValid = true,
            Resumen = "Validación exitosa."
        });

        public static StubValidation WithIssues(int profileId, IReadOnlyList<NachaConfigValidationIssueDto> issues) => new(new NachaConfigValidationResultDto
        {
            ProfileId = profileId,
            IsValid = false,
            ErroresBloqueantes = issues.Count(x => x.Severidad == "ERROR"),
            Advertencias = issues.Count(x => x.Severidad == "WARN"),
            Resumen = "Validación con errores.",
            Issues = issues
        });

        public Task<NachaConfigValidationResultDto> ValidateBeforePublishAsync(int profileId, CancellationToken ct = default)
            => Task.FromResult(_result);
    }

    private sealed class RecordingPreviewService : INachaConfigPreviewService
    {
        private readonly int _profileId;
        private readonly string _profileCode;
        public NachaConfigResolverPreviewRequest? LastRequest { get; private set; }

        public RecordingPreviewService(int profileId, string profileCode)
        {
            _profileId = profileId;
            _profileCode = profileCode;
        }

        public Task<NachaConfigResolverPreviewResultDto> PreviewResolverAsync(NachaConfigResolverPreviewRequest request, CancellationToken ct = default)
        {
            LastRequest = request;
            return Task.FromResult(new NachaConfigResolverPreviewResultDto
            {
                Success = true,
                ProfileId = _profileId,
                ProfileCode = _profileCode,
                Trace = ["Perfil seleccionado para la prueba."],
                Warnings = []
            });
        }
    }
}
