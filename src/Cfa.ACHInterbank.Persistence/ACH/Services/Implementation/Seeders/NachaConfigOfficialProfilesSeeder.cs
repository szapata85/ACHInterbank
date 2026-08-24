using System.Text.Json;
using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;

[Scoped]
public sealed class NachaConfigOfficialProfilesSeeder : IDbSeeder
{
    private static readonly DateTime EffectiveFrom = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime PublishedAt = new(2026, 5, 24, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTimeOffset AuditTimestamp = new(2026, 5, 24, 0, 0, 0, TimeSpan.Zero);
    private static readonly string[] RecordCodes = ["1", "5", "6", "7", "8", "9"];

    private readonly AchDbContext _context;

    public NachaConfigOfficialProfilesSeeder(AchDbContext context)
    {
        _context = context;
    }

    public int Order => 9;

    public async Task SeedAsync()
    {
        await EnsureReturnOfReturnFlowTypeAsync();
        var catalog = await LoadCatalogAsync();
        await RetireLegacyOrdinaryAchProfilesAsync(catalog.Statuses["INACTIVO"]);

        await EnsureProfileAsync(new ProfileSpec(
            ProfileCode: AchColOfficialNachaLayout.OutboundOriginalProfileCode,
            Name: "Perfil oficial ACH Colombia V35 salida original",
            Description: "Perfil ordinario table-driven ACH Colombia V35 para créditos y débitos monetarios de salida.",
            ClearingHouseCode: "ACH",
            FlowTypeCode: "ORIGINAL",
            NormativeSource: "DDS-DIS-MAN-004, ACH Colombia Manual de Servicio V35, secciones 6.4 y 6.5",
            NormativeVersion: AchColOfficialNachaLayout.NormativeVersion,
            ApprovedRuleMatrix: "ACH-Colombia-V35.md#6.4-6.5",
            IsPlaceholder: false,
            IsHomologated: false,
            RoutingOrigin: "000128300",
            RoutingDestination: "000101006",
            ImmediateDestinationName: "ACH COLOMBIA",
            ImmediateOriginName: "CFA UAT",
            Prefix: "ACH_ORIGINAL_V35",
            VersionMajor: AchColOfficialNachaLayout.ProfileVersionMajor,
            VersionMinor: AchColOfficialNachaLayout.ProfileVersionMinor));

        await EnsureProfileAsync(new ProfileSpec(
            ProfileCode: AchColOfficialNachaLayout.OutboundPrenotificationProfileCode,
            Name: "Perfil oficial ACH Colombia V35 salida prenotificación",
            Description: "Perfil ordinario table-driven ACH Colombia V35 para prenotificaciones crédito y débito de salida.",
            ClearingHouseCode: "ACH",
            FlowTypeCode: "PRENOTIFICACION",
            NormativeSource: "DDS-DIS-MAN-004, ACH Colombia Manual de Servicio V35, secciones 6.4 y 6.5",
            NormativeVersion: AchColOfficialNachaLayout.NormativeVersion,
            ApprovedRuleMatrix: "ACH-Colombia-V35.md#6.4-6.5",
            IsPlaceholder: false,
            IsHomologated: false,
            RoutingOrigin: "000128300",
            RoutingDestination: "000101006",
            ImmediateDestinationName: "ACH COLOMBIA",
            ImmediateOriginName: "CFA UAT",
            Prefix: "ACH_PRENOTE_V35",
            VersionMajor: AchColOfficialNachaLayout.ProfileVersionMajor,
            VersionMinor: AchColOfficialNachaLayout.ProfileVersionMinor));

        await EnsureProfileAsync(new ProfileSpec(
            ProfileCode: AchColOfficialNachaLayout.InboundOriginalProfileCode,
            Name: "Perfil oficial ACH Colombia V35 entrada original",
            Description: "Perfil ordinario table-driven ACH Colombia V35 para créditos y débitos monetarios de entrada.",
            ClearingHouseCode: "ACH",
            FlowTypeCode: "ORIGINAL",
            NormativeSource: "DDS-DIS-MAN-004, ACH Colombia Manual de Servicio V35, secciones 6.4 y 6.5",
            NormativeVersion: AchColOfficialNachaLayout.NormativeVersion,
            ApprovedRuleMatrix: "ACH-Colombia-V35.md#6.4-6.5",
            IsPlaceholder: false,
            IsHomologated: false,
            RoutingOrigin: "000101006",
            RoutingDestination: "000128300",
            ImmediateDestinationName: "CFA UAT",
            ImmediateOriginName: "ACH COLOMBIA",
            Prefix: "ACH_IN_ORIGINAL_V35",
            DirectionCode: "ENTRADA",
            VersionMajor: AchColOfficialNachaLayout.ProfileVersionMajor,
            VersionMinor: AchColOfficialNachaLayout.ProfileVersionMinor));

        await EnsureProfileAsync(new ProfileSpec(
            ProfileCode: AchColOfficialNachaLayout.InboundPrenotificationProfileCode,
            Name: "Perfil oficial ACH Colombia V35 entrada prenotificación",
            Description: "Perfil ordinario table-driven ACH Colombia V35 para prenotificaciones crédito y débito de entrada.",
            ClearingHouseCode: "ACH",
            FlowTypeCode: "PRENOTIFICACION",
            NormativeSource: "DDS-DIS-MAN-004, ACH Colombia Manual de Servicio V35, secciones 6.4 y 6.5",
            NormativeVersion: AchColOfficialNachaLayout.NormativeVersion,
            ApprovedRuleMatrix: "ACH-Colombia-V35.md#6.4-6.5",
            IsPlaceholder: false,
            IsHomologated: false,
            RoutingOrigin: "000101006",
            RoutingDestination: "000128300",
            ImmediateDestinationName: "CFA UAT",
            ImmediateOriginName: "ACH COLOMBIA",
            Prefix: "ACH_IN_PRENOTE_V35",
            DirectionCode: "ENTRADA",
            VersionMajor: AchColOfficialNachaLayout.ProfileVersionMajor,
            VersionMinor: AchColOfficialNachaLayout.ProfileVersionMinor));

        await EnsureProfileAsync(new ProfileSpec(
            ProfileCode: "OFFICIAL_ACH_SALIDA_DEVOLUCION_V35_1_0",
            Name: "Perfil canónico ACH Colombia salida devolución V35",
            Description: "Perfil table-driven canónico de ReturnOut ACH Colombia. Implementación técnica V35; homologación externa pendiente.",
            ClearingHouseCode: "ACH",
            FlowTypeCode: "DEVOLUCION",
            NormativeSource: "ACH Colombia Manual de Servicio V35, sección 6.6",
            NormativeVersion: "V35",
            ApprovedRuleMatrix: "ACH-Colombia-V35.md#6.6",
            IsPlaceholder: false,
            IsHomologated: false,
            RoutingOrigin: "",
            RoutingDestination: "000101006",
            ImmediateDestinationName: "ACH COLOMBIA",
            ImmediateOriginName: "",
            Prefix: "ACH_RETURN_OUT_V35"));

        await EnsureProfileAsync(new ProfileSpec(
            ProfileCode: "OFFICIAL_ACH_ENTRADA_DEVOLUCION_V1_0",
            Name: "Perfil oficial ACH Colombia entrada devolución V35",
            Description: "Perfil table-driven para respuestas diferenciales entrantes de ACH Colombia con addenda de devolución tipo 99.",
            ClearingHouseCode: "ACH",
            FlowTypeCode: "RETORNO",
            NormativeSource: "ACH Colombia Manual de Servicio V35, secciones 6.6 y 6.7",
            NormativeVersion: "V35",
            ApprovedRuleMatrix: "ACH-Colombia-V35.md#6.6-6.7",
            IsPlaceholder: false,
            IsHomologated: true,
            RoutingOrigin: "",
            RoutingDestination: "",
            ImmediateDestinationName: "ACH COLOMBIA",
            ImmediateOriginName: "",
            Prefix: "ACH_RETURN_IN_V35",
            DirectionCode: "ENTRADA",
            VersionMinor: 0,
            IncludeReturnAddenda99: true));

        await EnsureProfileAsync(new ProfileSpec(
            ProfileCode: CenitReturnIn2026Layout.ProfileCode,
            Name: "Perfil CENIT entrada devolución 2026",
            Description: "Perfil table-driven para Return In CENIT conforme al formato NACHA-M del 07-may-2026; certificación externa pendiente.",
            ClearingHouseCode: "CENIT",
            FlowTypeCode: "RETORNO",
            NormativeSource: "Manual de Especificaciones Formato NACHA-M CENIT, 07-may-2026",
            NormativeVersion: "2026-05-07",
            ApprovedRuleMatrix: "7.2.1;7.3.1;Anexo 1.6;CENIT-Anexo-A",
            IsPlaceholder: false,
            IsHomologated: true,
            RoutingOrigin: "",
            RoutingDestination: "",
            ImmediateDestinationName: "CENIT",
            ImmediateOriginName: "",
            Prefix: "CENIT_RETURN_IN_2026",
            DirectionCode: "ENTRADA",
            VersionMinor: 0));

        await EnsureProfileAsync(new ProfileSpec(
            ProfileCode: CenitReturnOut2026Layout.ProfileCode,
            Name: "Perfil CENIT salida devolución 2026",
            Description: "Perfil table-driven para Return Out CENIT conforme al formato NACHA-M del 07-may-2026; certificación externa pendiente.",
            ClearingHouseCode: "CENIT",
            FlowTypeCode: "DEVOLUCION",
            NormativeSource: "Manual de Especificaciones Formato NACHA-M CENIT, 07-may-2026",
            NormativeVersion: CenitReturnOut2026Layout.NormativeVersion,
            ApprovedRuleMatrix: "7.2.1;Anexo 1.6;Tabla 6;CENIT-Anexo-A",
            IsPlaceholder: false,
            IsHomologated: true,
            RoutingOrigin: "",
            RoutingDestination: "",
            ImmediateDestinationName: "CENIT",
            ImmediateOriginName: "",
            Prefix: "CENIT_RETURN_OUT_2026",
            DirectionCode: "SALIDA",
            VersionMinor: 0));

        await EnsureProfileAsync(new ProfileSpec(
            ProfileCode: CenitReturnOfReturn2026Layout.InProfileCode,
            Name: "Perfil CENIT entrada devolución de devolución 2026",
            Description: "Perfil table-driven ROR In CENIT conforme al formato NACHA-M del 07-may-2026; certificación externa pendiente.",
            ClearingHouseCode: "CENIT",
            FlowTypeCode: CenitReturnOfReturn2026Layout.FlowTypeCode,
            NormativeSource: "Manual de Especificaciones Formato NACHA-M CENIT, 07-may-2026",
            NormativeVersion: CenitReturnOfReturn2026Layout.NormativeVersion,
            ApprovedRuleMatrix: "7.1.2;Anexo 1.7;Tabla 6;CENIT-Anexo-A Tabla 2",
            IsPlaceholder: false,
            IsHomologated: true,
            RoutingOrigin: "",
            RoutingDestination: "",
            ImmediateDestinationName: "CENIT",
            ImmediateOriginName: "",
            Prefix: "CENIT_ROR_IN_2026",
            DirectionCode: "ENTRADA",
            VersionMinor: 0));

        await EnsureProfileAsync(new ProfileSpec(
            ProfileCode: CenitReturnOfReturn2026Layout.OutProfileCode,
            Name: "Perfil CENIT salida devolución de devolución 2026",
            Description: "Perfil table-driven ROR Out CENIT conforme al formato NACHA-M del 07-may-2026; certificación externa pendiente.",
            ClearingHouseCode: "CENIT",
            FlowTypeCode: CenitReturnOfReturn2026Layout.FlowTypeCode,
            NormativeSource: "Manual de Especificaciones Formato NACHA-M CENIT, 07-may-2026",
            NormativeVersion: CenitReturnOfReturn2026Layout.NormativeVersion,
            ApprovedRuleMatrix: "7.1.2;Anexo 1.7;Tabla 6;CENIT-Anexo-A Tabla 2",
            IsPlaceholder: false,
            IsHomologated: true,
            RoutingOrigin: "",
            RoutingDestination: "",
            ImmediateDestinationName: "CENIT",
            ImmediateOriginName: "",
            Prefix: "CENIT_ROR_OUT_2026",
            DirectionCode: "SALIDA",
            VersionMinor: 0));

        await EnsureProfileAsync(new ProfileSpec(
            ProfileCode: "OFFICIAL_CENIT_SALIDA_ORIGINAL_V1_0",
            Name: "Perfil oficial CENIT salida original",
            Description: "Perfil oficial UAT/local table-driven para CENIT. Fuente normativa pendiente de homologacion formal: CENIT/DSP-152 placeholder.",
            ClearingHouseCode: "CENIT",
            FlowTypeCode: "ORIGINAL",
            NormativeSource: "CENIT/DSP-152 placeholder UAT",
            NormativeVersion: "NOT-DEMONSTRATED",
            ApprovedRuleMatrix: "MATRIZ_REGLAS_CENIT.md@NO-GO",
            IsPlaceholder: true,
            IsHomologated: false,
            RoutingOrigin: "01111111",
            RoutingDestination: "02222222",
            ImmediateDestinationName: "CENIT",
            ImmediateOriginName: "CFA UAT",
            Prefix: "CENIT"));

        await EnsureProfileAsync(new ProfileSpec(
            ProfileCode: "OFFICIAL_CENIT_SALIDA_PRENOTIFICACION_V1_0",
            Name: "Perfil oficial CENIT salida prenotificación",
            Description: "Perfil UAT/local table-driven para prenotificaciones CENIT. Fuente normativa pendiente de homologación formal: CENIT/DSP-152 placeholder.",
            ClearingHouseCode: "CENIT",
            FlowTypeCode: "PRENOTIFICACION",
            NormativeSource: "CENIT/DSP-152 placeholder UAT",
            NormativeVersion: "NOT-DEMONSTRATED",
            ApprovedRuleMatrix: "MATRIZ_REGLAS_CENIT.md@NO-GO",
            IsPlaceholder: true,
            IsHomologated: false,
            RoutingOrigin: "01111111",
            RoutingDestination: "02222222",
            ImmediateDestinationName: "CENIT",
            ImmediateOriginName: "CFA UAT",
            Prefix: "CENIT_PRENOTE"));

        await _context.SaveChangesAsync();

        async Task EnsureProfileAsync(ProfileSpec spec)
        {
            var profile = await _context.CfgProfiles
                .Include(x => x.Tags)
                .Include(x => x.Records)
                .Include(x => x.LayoutVariants)
                    .ThenInclude(x => x.Fields)
                        .ThenInclude(x => x.SourceDefinition)
                .Include(x => x.LayoutVariants)
                    .ThenInclude(x => x.Fields)
                        .ThenInclude(x => x.Rules)
                .FirstOrDefaultAsync(x => x.ProfileCode == spec.ProfileCode);

            if (profile is null)
            {
                profile = new CfgProfile
                {
                    ProfileCode = spec.ProfileCode,
                    CreatedAt = AuditTimestamp,
                    UpdatedAt = AuditTimestamp
                };
                _context.CfgProfiles.Add(profile);
            }

            profile.NameEs = spec.Name;
            profile.Description = spec.Description;
            profile.ClearingHouseId = catalog.ClearingHouses[spec.ClearingHouseCode];
            profile.FlowTypeId = catalog.FlowTypes[spec.FlowTypeCode];
            profile.DirectionId = catalog.Directions[spec.DirectionCode];
            profile.ServiceClassId = null;
            profile.ContextPriority = 10;
            profile.EffectiveFrom = EffectiveFrom;
            profile.EffectiveTo = null;
            profile.StatusId = catalog.Statuses["PUBLICADO"];
            profile.VersionMajor = spec.VersionMajor;
            profile.VersionMinor = spec.VersionMinor;
            profile.PublishedAt = PublishedAt;
            profile.PublishedBy = spec.IsPlaceholder ? "system-phase-6b1" : "system-nacha-execution-2";
            profile.RowVersion = BuildRowVersion(spec.Prefix);
            profile.UpdatedAt = AuditTimestamp;

            await _context.SaveChangesAsync();

            await EnsureTagAsync(profile, "NormativeSource", spec.NormativeSource);
            await EnsureTagAsync(profile, "NormativeVersion", spec.NormativeVersion);
            await EnsureTagAsync(profile, "IsPlaceholder", spec.IsPlaceholder ? "true" : "false");
            await EnsureTagAsync(profile, "IsHomologated", spec.IsHomologated ? "true" : "false");
            await EnsureTagAsync(profile, "ApprovedRuleMatrix", spec.ApprovedRuleMatrix);
            await EnsureTagAsync(profile, "Phase", spec.IsPlaceholder ? "6B.1" : "NACHA-EXECUTION-2");
            await EnsureTagAsync(profile, "ProductionDecision", "NO-GO");

            var sequence = 10;
            foreach (var recordCode in RecordCodes)
            {
                var isReturnOutV35 = IsReturnOutV35(spec);
                var isCenitReturnIn2026 = IsCenitReturnIn2026(spec);
                var isCenitReturnOut2026 = IsCenitReturnOut2026(spec);
                var isCenitReturnOfReturn2026 = IsCenitReturnOfReturn2026(spec);
                var variant = await EnsureVariantAsync(
                    profile,
                    spec,
                    recordCode,
                    sequence,
                    catalog,
                    isCenitReturnOfReturn2026
                        ? CenitReturnOfReturn2026Layout.Variant(recordCode, spec.DirectionCode == "ENTRADA")
                        : isCenitReturnIn2026
                        ? CenitReturnIn2026Layout.Variant(recordCode)
                        : isCenitReturnOut2026
                        ? CenitReturnOut2026Layout.Variant(recordCode)
                        : isReturnOutV35
                        ? AchColReturnOutV35Layout.Variant(recordCode)
                        : recordCode == "7" && !spec.IsPlaceholder ? ResolveType7CreditVariant(spec) : null,
                    isDefault: true,
                    selectionPredicateJson: null);
                await EnsureProfileRecordAsync(profile, recordCode, sequence, variant.Id, catalog);

                if (recordCode == "7" && !spec.IsPlaceholder && !isReturnOutV35 && !isCenitReturnIn2026 && !isCenitReturnOut2026 && !isCenitReturnOfReturn2026)
                {
                    await EnsureVariantAsync(
                        profile,
                        spec,
                        recordCode,
                        sequence + 1,
                        catalog,
                        AchColOfficialNachaLayout.Type7DebitVariant,
                        isDefault: false,
                        selectionPredicateJson: JsonSerializer.Serialize(new { BusinessType = "DEBIT" }));

                    if (IsOrdinaryAchV35(spec)
                        && string.Equals(spec.FlowTypeCode, "ORIGINAL", StringComparison.OrdinalIgnoreCase))
                    {
                        await EnsureVariantAsync(
                            profile,
                            spec,
                            recordCode,
                            sequence + 2,
                            catalog,
                            AchColOfficialNachaLayout.Type7CreditPrenotificationVariant,
                            isDefault: false,
                            selectionPredicateJson: JsonSerializer.Serialize(new
                            {
                                BusinessType = "CREDIT",
                                TransactionFamily = "PRENOTIFICATION"
                            }));
                    }

                    if (spec.IncludeReturnAddenda99)
                    {
                        await EnsureVariantAsync(
                            profile,
                            spec,
                            recordCode,
                            sequence + 2,
                            catalog,
                            $"{spec.Prefix}_R7_ADDENDA_99",
                            isDefault: false,
                            selectionPredicateJson: JsonSerializer.Serialize(new { AddendaType = "99" }));
                    }
                }

                sequence += 10;
            }

            _context.ChangeTracker.Clear();
        }

        async Task EnsureTagAsync(CfgProfile profile, string key, string value)
        {
            var existing = profile.Tags.FirstOrDefault(x => x.TagKey == key);
            if (existing is not null)
            {
                existing.TagValue = value;
                existing.UpdatedAt = AuditTimestamp;
                return;
            }

            _context.CfgProfileTags.Add(new CfgProfileTag
            {
                ProfileId = profile.Id,
                TagKey = key,
                TagValue = value,
                CreatedAt = AuditTimestamp,
                UpdatedAt = AuditTimestamp
            });
            await _context.SaveChangesAsync();
        }

        async Task<CfgLayoutVariant> EnsureVariantAsync(
            CfgProfile profile,
            ProfileSpec spec,
            string recordCode,
            int sequence,
            CatalogIds catalog,
            string? explicitVariantCode,
            bool isDefault,
            string? selectionPredicateJson)
        {
            var variantCode = explicitVariantCode ?? $"{spec.Prefix}_R{recordCode}_BASE_V1";
            var variant = profile.LayoutVariants.FirstOrDefault(x => x.VariantCode == variantCode);
            if (variant is null)
            {
                variant = new CfgLayoutVariant
                {
                    ProfileId = profile.Id,
                    VariantCode = variantCode,
                    CreatedAt = AuditTimestamp,
                    UpdatedAt = AuditTimestamp
                };
                _context.CfgLayoutVariants.Add(variant);
            }

            variant.RecordCodeId = catalog.RecordCodes[recordCode];
            variant.NameEs = $"Registro {recordCode} oficial {spec.ClearingHouseCode}";
            variant.Description = $"Variant oficial UAT/local para registro {recordCode}; perfil {profile.ProfileCode}.";
            variant.Priority = isDefault ? 10 : 20;
            variant.EffectiveFrom = EffectiveFrom;
            variant.EffectiveTo = null;
            variant.StatusId = catalog.Statuses["PUBLICADO"];
            variant.TotalLength = 106;
            variant.SelectionPredicateJson = selectionPredicateJson;
            variant.IsDefaultForRecord = isDefault;
            variant.UpdatedAt = AuditTimestamp;

            await _context.SaveChangesAsync();

            var expectedFields = BuildFields(spec, recordCode, variantCode);
            var expectedCodes = expectedFields.Select(field => field.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var obsoleteField in variant.Fields.Where(field => !expectedCodes.Contains(field.FieldCode)))
            {
                obsoleteField.IsEnabled = false;
                obsoleteField.UpdatedAt = AuditTimestamp;
            }

            foreach (var field in expectedFields)
            {
                await EnsureFieldAsync(variant, field, spec, recordCode, catalog);
            }

            return variant;
        }

        async Task EnsureProfileRecordAsync(
            CfgProfile profile,
            string recordCode,
            int sequence,
            int layoutVariantId,
            CatalogIds catalog)
        {
            var recordCodeId = catalog.RecordCodes[recordCode];
            var profileRecord = profile.Records.FirstOrDefault(x => x.RecordCodeId == recordCodeId);
            if (profileRecord is null)
            {
                profileRecord = new CfgProfileRecord
                {
                    ProfileId = profile.Id,
                    RecordCodeId = recordCodeId,
                    CreatedAt = AuditTimestamp,
                    UpdatedAt = AuditTimestamp
                };
                _context.CfgProfileRecords.Add(profileRecord);
            }

            profileRecord.Sequence = sequence;
            profileRecord.IsEnabled = true;
            profileRecord.MinOccurs = recordCode == "7" ? 0 : 1;
            profileRecord.MaxOccurs = null;
            profileRecord.SourceStrategy = "TABLE_DRIVEN";
            profileRecord.LayoutVariantId = layoutVariantId;
            profileRecord.SemanticRuleSetId = null;
            profileRecord.UpdatedAt = AuditTimestamp;
            await _context.SaveChangesAsync();
        }

        async Task EnsureFieldAsync(CfgLayoutVariant variant, FieldSpec field, ProfileSpec spec, string recordCode, CatalogIds catalog)
        {
            var layoutField = variant.Fields.FirstOrDefault(x => x.FieldCode == field.Code);
            if (layoutField is null)
            {
                layoutField = new CfgLayoutField
                {
                    LayoutVariantId = variant.Id,
                    FieldCode = field.Code,
                    CreatedAt = AuditTimestamp,
                    UpdatedAt = AuditTimestamp
                };
                _context.CfgLayoutFields.Add(layoutField);
            }

            var source = layoutField.SourceDefinition;
            if (source is null)
            {
                source = new CfgFieldSourceDefinition
                {
                    CreatedAt = AuditTimestamp,
                    UpdatedAt = AuditTimestamp
                };
                _context.CfgFieldSourceDefinitions.Add(source);
                layoutField.SourceDefinition = source;
            }

            source.DataSourceTypeId = catalog.SourceTypes[field.SourceTypeCode];
            source.ConstantValue = field.ConstantValue;
            source.EntityName = field.EntityName;
            source.PropertyPath = field.PropertyPath;
            source.SqlObjectName = null;
            source.ExpressionDsl = field.ExpressionDsl;
            source.ExternalCatalogCode = null;
            source.FallbackPolicyJson = null;
            source.UpdatedAt = AuditTimestamp;

            layoutField.FieldNameEs = field.Name;
            layoutField.StartPosition = field.Start;
            layoutField.Length = field.Length;
            layoutField.PadChar = field.PadChar;
            layoutField.Justification = field.Justification;
            layoutField.FormatMask = field.FormatMask;
            layoutField.SortOrder = field.Start;
            layoutField.IsVisibleInBackoffice = true;
            layoutField.IsEnabled = true;
            layoutField.SourceDefinition = source;
            layoutField.TransformationPipelineJson = field.TransformationPipelineJson;
            layoutField.UpdatedAt = AuditTimestamp;

            await _context.SaveChangesAsync();

            if (!spec.IsPlaceholder)
            {
                var descriptor = UsesCenitReturnOfReturn2026Layout(spec)
                    ? CenitReturnOfReturn2026Layout.Field(recordCode, field.Code)
                    : UsesCenitReturnIn2026Layout(spec)
                    ? CenitReturnIn2026Layout.Field(recordCode, field.Code)
                    : UsesCenitReturnOut2026Layout(spec)
                    ? CenitReturnOut2026Layout.Field(recordCode, field.Code)
                    : UsesReturnV35Layout(spec, recordCode, variant.VariantCode)
                    ? AchColReturnOutV35Layout.Field(recordCode, field.Code)
                    : AchColOfficialNachaLayout.Field(recordCode, field.Code, variant.VariantCode);
                await EnsureExecutableRuleAsync(layoutField, descriptor, spec.ClearingHouseCode, catalog);
            }
        }

        async Task EnsureExecutableRuleAsync(
            CfgLayoutField field,
            AchColOfficialFieldDescriptor descriptor,
            string clearingHouseCode,
            CatalogIds catalog)
        {
            var rule = field.Rules.FirstOrDefault(existing => existing.RuleCode == descriptor.RuleId);
            if (rule is null)
            {
                rule = new CfgFieldRule
                {
                    LayoutField = field,
                    RuleCode = descriptor.RuleId,
                    CreatedAt = AuditTimestamp,
                    UpdatedAt = AuditTimestamp
                };
                _context.CfgFieldRules.Add(rule);
            }

            rule.RuleTypeId = catalog.RuleTypes[descriptor.DataType is NachaFieldDataType.Date or NachaFieldDataType.Time
                ? "DATE_FORMAT"
                : descriptor.AllowedValues?.Count > 0 ? "ENUM" : "REGEX"];
            rule.ErrorCode = "NACHA_FIELD_RULE_FAILED";
            rule.ErrorMessageEs = "El campo no cumple la regla normativa configurada.";
            rule.Severity = descriptor.Severity;
            rule.ConditionDsl = null;
            rule.RuleConfigJson = JsonSerializer.Serialize(new
            {
                ruleId = descriptor.RuleId,
                chamber = clearingHouseCode,
                recordType = descriptor.RecordCode,
                field = descriptor.FieldCode,
                startPosition = descriptor.StartPosition,
                length = descriptor.Length,
                dataType = descriptor.DataType.ToString().ToUpperInvariant(),
                required = descriptor.Required,
                justification = descriptor.Justification.ToString(),
                padChar = descriptor.PadChar.ToString(),
                format = descriptor.Format,
                allowedValues = descriptor.AllowedValues,
                sensitivity = descriptor.Sensitivity.ToString().ToUpperInvariant(),
                overflowPolicy = descriptor.OverflowPolicy,
                normalizer = descriptor.Normalizer,
                normativeSource = descriptor.NormativeSource,
                normativeVersion = descriptor.NormativeVersion,
                normativeSection = descriptor.NormativeSection,
                severity = descriptor.Severity
            });
            rule.Order = 10;
            rule.IsEnabled = true;
            rule.UpdatedAt = AuditTimestamp;
            await _context.SaveChangesAsync();
        }
    }

    private async Task<CatalogIds> LoadCatalogAsync()
    {
        return new CatalogIds(
            await _context.CatClearingHouses.AsNoTracking().ToDictionaryAsync(x => x.Code, x => x.Id),
            await _context.CatFlowTypes.AsNoTracking().ToDictionaryAsync(x => x.Code, x => x.Id),
            await _context.CatDirections.AsNoTracking().ToDictionaryAsync(x => x.Code, x => x.Id),
            await _context.CatConfigStatuses.AsNoTracking().ToDictionaryAsync(x => x.Code, x => x.Id),
            await _context.CatRecordCodes.AsNoTracking().ToDictionaryAsync(x => x.Code, x => x.Id),
            await _context.CatDataSourceTypes.AsNoTracking().ToDictionaryAsync(x => x.Code, x => x.Id),
            await _context.CatRuleTypes.AsNoTracking().ToDictionaryAsync(x => x.Code, x => x.Id));
    }

    private async Task RetireLegacyOrdinaryAchProfilesAsync(int inactiveStatusId)
    {
        string[] legacyProfileCodes =
        [
            "OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0",
            "OFFICIAL_ACH_SALIDA_PRENOTIFICACION_V1_0"
        ];

        var legacyProfiles = await _context.CfgProfiles
            .Where(profile => legacyProfileCodes.Contains(profile.ProfileCode))
            .ToListAsync();
        foreach (var profile in legacyProfiles)
        {
            profile.StatusId = inactiveStatusId;
            profile.EffectiveTo ??= EffectiveFrom;
            profile.UpdatedAt = AuditTimestamp;
        }

        if (legacyProfiles.Count > 0)
        {
            await _context.SaveChangesAsync();
        }
    }

    private static byte[] BuildRowVersion(string prefix)
    {
        return prefix.StartsWith("CENIT", StringComparison.OrdinalIgnoreCase) ? [6, 11, 2, 1] : [6, 11, 1, 1];
    }

    private async Task EnsureReturnOfReturnFlowTypeAsync()
    {
        if (await _context.CatFlowTypes.AnyAsync(x => x.Code == CenitReturnOfReturn2026Layout.FlowTypeCode)) return;

        var outboundDirectionId = await _context.CatDirections
            .Where(x => x.Code == "SALIDA")
            .Select(x => x.Id)
            .SingleAsync();
        _context.CatFlowTypes.Add(new CatFlowType
        {
            Code = CenitReturnOfReturn2026Layout.FlowTypeCode,
            NameEs = "Devolución de una devolución",
            DirectionDefaultId = outboundDirectionId,
            IsActive = true,
            CreatedAt = AuditTimestamp,
            UpdatedAt = AuditTimestamp
        });
        await _context.SaveChangesAsync();
    }

    private static IReadOnlyList<FieldSpec> BuildFields(ProfileSpec profile, string recordCode, string variantCode)
    {
        if (UsesCenitReturnOfReturn2026Layout(profile))
        {
            return CenitReturnOfReturn2026Layout.ForRecord(recordCode)
                .Select(BuildReturnOutV35Field)
                .ToList();
        }

        if (UsesCenitReturnIn2026Layout(profile))
        {
            return CenitReturnIn2026Layout.ForRecord(recordCode)
                .Select(BuildReturnOutV35Field)
                .ToList();
        }

        if (UsesCenitReturnOut2026Layout(profile))
        {
            return CenitReturnOut2026Layout.ForRecord(recordCode)
                .Select(BuildReturnOutV35Field)
                .ToList();
        }

        if (UsesReturnV35Layout(profile, recordCode, variantCode))
        {
            return AchColReturnOutV35Layout.ForRecord(recordCode)
                .Select(BuildReturnOutV35Field)
                .ToList();
        }

        if (!profile.IsPlaceholder)
        {
            return BuildAchColFields(profile, recordCode, variantCode);
        }

        return recordCode switch
        {
            "1" => BuildRecord1(profile),
            "5" => BuildRecord5(),
            "6" => BuildRecord6(),
            "7" => BuildRecord7(),
            "8" => BuildRecord8(),
            "9" => BuildRecord9(),
            _ => []
        };
    }

    private static bool IsReturnOutV35(ProfileSpec profile)
        => string.Equals(profile.ClearingHouseCode, "ACH", StringComparison.OrdinalIgnoreCase)
           && string.Equals(profile.FlowTypeCode, "DEVOLUCION", StringComparison.OrdinalIgnoreCase)
           && string.Equals(profile.NormativeVersion, "V35", StringComparison.OrdinalIgnoreCase);

    private static bool IsCenitReturnIn2026(ProfileSpec profile)
        => string.Equals(profile.ProfileCode, CenitReturnIn2026Layout.ProfileCode, StringComparison.Ordinal);

    private static bool IsCenitReturnOut2026(ProfileSpec profile)
        => string.Equals(profile.ProfileCode, CenitReturnOut2026Layout.ProfileCode, StringComparison.Ordinal);

    private static bool IsCenitReturnOfReturn2026(ProfileSpec profile)
        => string.Equals(profile.ProfileCode, CenitReturnOfReturn2026Layout.InProfileCode, StringComparison.Ordinal)
           || string.Equals(profile.ProfileCode, CenitReturnOfReturn2026Layout.OutProfileCode, StringComparison.Ordinal);

    private static bool UsesCenitReturnIn2026Layout(ProfileSpec profile)
        => IsCenitReturnIn2026(profile);

    private static bool UsesCenitReturnOut2026Layout(ProfileSpec profile)
        => IsCenitReturnOut2026(profile);

    private static bool UsesCenitReturnOfReturn2026Layout(ProfileSpec profile)
        => IsCenitReturnOfReturn2026(profile);

    private static bool UsesReturnV35Layout(ProfileSpec profile, string recordCode, string variantCode)
        => IsReturnOutV35(profile)
           || (profile.IncludeReturnAddenda99
               && string.Equals(recordCode, "7", StringComparison.OrdinalIgnoreCase)
               && variantCode.EndsWith("_ADDENDA_99", StringComparison.OrdinalIgnoreCase));

    private static string ResolveType7CreditVariant(ProfileSpec profile)
        => string.Equals(profile.ClearingHouseCode, "ACH", StringComparison.OrdinalIgnoreCase)
           && string.Equals(profile.FlowTypeCode, "ORIGINAL", StringComparison.OrdinalIgnoreCase)
           && string.Equals(profile.NormativeVersion, AchColOfficialNachaLayout.NormativeVersion, StringComparison.OrdinalIgnoreCase)
            ? AchColOfficialNachaLayout.Type7CreditMonetaryVariant
            : AchColOfficialNachaLayout.Type7CreditPrenotificationVariant;

    private static bool IsOrdinaryAchV35(ProfileSpec profile)
        => string.Equals(profile.ClearingHouseCode, "ACH", StringComparison.OrdinalIgnoreCase)
           && profile.FlowTypeCode is "ORIGINAL" or "PRENOTIFICACION"
           && string.Equals(profile.NormativeVersion, AchColOfficialNachaLayout.NormativeVersion, StringComparison.OrdinalIgnoreCase);

    private static FieldSpec BuildReturnOutV35Field(AchColOfficialFieldDescriptor descriptor)
    {
        var constant = (descriptor.RecordCode, descriptor.FieldCode) switch
        {
            (_, "RECORDTYPE") => descriptor.RecordCode,
            ("1", "PRIORITYCODE") => "01",
            ("1", "RECORDSIZE") => "106",
            ("1", "BLOCKINGFACTOR") => "10",
            ("1", "FORMATCODE") => "1",
            ("5", "ORIGINATORSTATUSCODE") => "1",
            ("6", "ADDENDARECORDINDICATOR") => "1",
            ("7", "ADDENDATYPE") => "99",
            _ => null
        };

        if (!string.IsNullOrWhiteSpace(constant))
        {
            return new FieldSpec(descriptor.FieldCode, descriptor.FieldCode, descriptor.StartPosition, descriptor.Length,
                descriptor.PadChar, descriptor.Justification, descriptor.Format, "CONSTANTE", constant, null, null, null, null);
        }

        var calculation = descriptor.DataType == NachaFieldDataType.Reserved
            ? "Filler"
            : descriptor.FieldCode;
        return new FieldSpec(descriptor.FieldCode, descriptor.FieldCode, descriptor.StartPosition, descriptor.Length,
            descriptor.PadChar, descriptor.Justification, descriptor.Format, "EXPRESION", null, null, null,
            JsonSerializer.Serialize(new { source = "runtime", calculationType = calculation }), null);
    }

    private static IReadOnlyList<FieldSpec> BuildAchColFields(ProfileSpec profile, string recordCode, string variantCode)
        => AchColOfficialNachaLayout.ForVariant(recordCode, variantCode)
            .Select(descriptor => BuildAchColField(profile, descriptor))
            .ToList();

    private static FieldSpec BuildAchColField(ProfileSpec profile, AchColOfficialFieldDescriptor descriptor)
    {
        var code = descriptor.FieldCode;
        var constant = (descriptor.RecordCode, code) switch
        {
            (_, "RECORDTYPE") => descriptor.RecordCode,
            ("1", "PRIORITYCODE") => "01",
            ("1", "IMMEDIATEDESTINATION") => profile.RoutingDestination,
            ("1", "IMMEDIATEORIGIN") => profile.RoutingOrigin,
            ("1", "RECORDSIZE") => "106",
            ("1", "BLOCKINGFACTOR") => "10",
            ("1", "FORMATCODE") => "1",
            ("1", "IMMEDIATEDESTINATIONNAME") => profile.ImmediateDestinationName,
            ("1", "IMMEDIATEORIGINNAME") => profile.ImmediateOriginName,
            ("5", "ORIGINATORSTATUSCODE") => "1",
            _ => null
        };

        if (!string.IsNullOrWhiteSpace(constant))
        {
            return new FieldSpec(
                code,
                code,
                descriptor.StartPosition,
                descriptor.Length,
                descriptor.PadChar,
                descriptor.Justification,
                descriptor.Format,
                "CONSTANTE",
                constant,
                null,
                null,
                null,
                null);
        }

        if (descriptor.DataType == NachaFieldDataType.Reserved)
        {
            var expression = JsonSerializer.Serialize(new { source = "runtime", calculationType = "Filler" });
            return new FieldSpec(
                code,
                code,
                descriptor.StartPosition,
                descriptor.Length,
                descriptor.PadChar,
                descriptor.Justification,
                descriptor.Format,
                "EXPRESION",
                null,
                null,
                null,
                expression,
                null);
        }

        var entityFields = descriptor.RecordCode switch
        {
            "6" => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "TRANSACTIONCODE", "RECEIVINGDFI", "DFIACCOUNTNUMBER", "AMOUNT",
                "INDIVIDUALIDENTIFICATION", "DISCRETIONARYDATA", "TRACENUMBER"
            },
            "7" => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "ADDENDATYPE", "ORIGINATORIDENTIFICATION", "PURPOSE", "REFERENCE", "INVOICEORACCOUNTNUMBER", "ORIGINATORFREEINFORMATION",
                "COLLECTORID", "RECEIVERCUSTOMERCODE",
                "SERVICEDESCRIPTION", "SEQUENCENUMBER", "TRACESUFFIX"
            },
            _ => new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        };

        var propertyPath = code switch
        {
            "DFIACCOUNTNUMBER" => "DestinationAccountNumber",
            "INDIVIDUALIDENTIFICATION" => "RecipientIdNumber",
            "INDIVIDUALNAME" => "ReceiverName",
            "ADDENDARECORDINDICATOR" => "AddendaRecordIndicator",
            _ => code
        };

        if (entityFields.Contains(code))
        {
            return new FieldSpec(
                code,
                code,
                descriptor.StartPosition,
                descriptor.Length,
                descriptor.PadChar,
                descriptor.Justification,
                descriptor.Format,
                "ENTIDAD",
                null,
                descriptor.RecordCode == "7" ? "AchTransactionAddenda" : "AchTransaction",
                propertyPath,
                null,
                null);
        }

        var calculatedExpression = JsonSerializer.Serialize(new { source = "runtime", calculationType = propertyPath });
        return new FieldSpec(
            code,
            code,
            descriptor.StartPosition,
            descriptor.Length,
            descriptor.PadChar,
            descriptor.Justification,
            descriptor.Format,
            "EXPRESION",
            null,
            null,
            null,
            calculatedExpression,
            null);
    }

    private static IReadOnlyList<FieldSpec> BuildRecord1(ProfileSpec profile)
    {
        return
        [
            Constant("RECORDTYPE", "Record type", 1, 1, "1"),
            Constant("PRIORITYCODE", "Priority code", 2, 2, "01", '0', 'R'),
            Constant("IMMEDIATEDESTINATION", "Immediate destination", 4, 10, profile.RoutingDestination, ' ', 'R'),
            Constant("IMMEDIATEORIGIN", "Immediate origin", 14, 10, profile.RoutingOrigin, ' ', 'R'),
            Calculated("FILECREATIONDATE", "File creation date", 24, 6, "FileCreationDate", "yyMMdd", '0', 'R'),
            Calculated("FILECREATIONTIME", "File creation time", 30, 4, "FileCreationTime", "HHmm", '0', 'R'),
            Calculated("FILEIDMODIFIER", "File id modifier", 34, 1, "FileIdModifier"),
            Constant("RECORDSIZE", "Record size", 35, 3, "106", '0', 'R'),
            Constant("BLOCKINGFACTOR", "Blocking factor", 38, 2, "10", '0', 'R'),
            Constant("FORMATCODE", "Format code", 40, 1, "1"),
            Constant("IMMEDIATEDESTINATIONNAME", "Immediate destination name", 41, 23, profile.ImmediateDestinationName),
            Constant("IMMEDIATEORIGINNAME", "Immediate origin name", 64, 23, profile.ImmediateOriginName),
            Calculated("REFERENCECODE", "Reference code", 87, 8, "ReferenceCode"),
            Filler("FILLER", 95, 12)
        ];
    }

    private static IReadOnlyList<FieldSpec> BuildRecord5()
    {
        return
        [
            Constant("RECORDTYPE", "Record type", 1, 1, "5"),
            Calculated("SERVICECLASSCODE", "Service class code", 2, 3, "ServiceClassCode", null, '0', 'R'),
            Calculated("COMPANYNAME", "Company name", 5, 16, "CompanyName"),
            Filler("COMPANYDISCRETIONARYDATA", 21, 20),
            Calculated("COMPANYIDENTIFICATION", "Company identification", 41, 10, "CompanyIdentification"),
            Constant("STANDARDENTRYCLASSCODE", "Standard entry class code", 51, 3, "PPD"),
            Calculated("COMPANYENTRYDESCRIPTION", "Company entry description", 54, 10, "CompanyEntryDescription"),
            Calculated("COMPANYDESCRIPTIVEDATE", "Company descriptive date", 64, 6, "CompanyDescriptiveDate", "yyMMdd", '0', 'R'),
            Calculated("EFFECTIVEENTRYDATE", "Effective entry date", 70, 6, "EffectiveEntryDate", "yyMMdd", '0', 'R'),
            Calculated("SETTLEMENTDATE", "Settlement date", 76, 3, "SettlementDate", null, ' ', 'R'),
            Constant("ORIGINATORSTATUSCODE", "Originator status code", 79, 1, "1"),
            Calculated("ORIGINATINGDFI", "Originating DFI", 80, 8, "OriginatingDfi", null, '0', 'R'),
            Calculated("BATCHNUMBER", "Batch number", 88, 7, "BatchNumber", null, '0', 'R'),
            Filler("FILLER", 95, 12)
        ];
    }

    private static IReadOnlyList<FieldSpec> BuildRecord6()
    {
        return
        [
            Constant("RECORDTYPE", "Record type", 1, 1, "6"),
            Source("TRANSACTIONCODE", "Transaction code", 2, 2, "TransactionCode", "AchTransaction", "0", 'R'),
            Source("RECEIVINGDFI", "Receiving DFI", 4, 8, "ReceivingDFI", "AchTransaction", "0", 'R'),
            Calculated("CHECKDIGIT", "Check digit", 12, 1, "CheckDigit", null, '0', 'R'),
            Source("DFIACCOUNTNUMBER", "DFI account number", 13, 17, "DestinationAccountNumber", "AchTransaction"),
            Source("AMOUNT", "Amount", 30, 10, "Amount", "AchTransaction", "0", 'R'),
            Source("INDIVIDUALIDENTIFICATION", "Individual identification", 40, 15, "ReceiverCustomerCode", "AchTransaction"),
            Calculated("INDIVIDUALNAME", "Individual name", 55, 22, "IndividualName"),
            Filler("DISCRETIONARYDATA", 77, 2),
            Calculated("ADDENDARECORDINDICATOR", "Addenda record indicator", 79, 1, "AddendaRecordIndicator", null, '0', 'R'),
            Source("TRACENUMBER", "Trace number", 80, 15, "TraceNumber", "AchTransaction", "0", 'R'),
            Filler("FILLER", 95, 12)
        ];
    }

    private static IReadOnlyList<FieldSpec> BuildRecord7()
    {
        return
        [
            Constant("RECORDTYPE", "Record type", 1, 1, "7"),
            Source("ADDENDATYPE", "Addenda type", 2, 2, "AddendaType", "AchTransactionAddenda", "0", 'R'),
            Calculated("PAYMENTRELATEDINFORMATION", "Payment related information", 4, 80, "PaymentRelatedInformation"),
            Source("SEQUENCENUMBER", "Addenda sequence number", 84, 4, "SequenceNumber", "AchTransactionAddenda", "0", 'R'),
            Source("TRACESUFFIX", "Entry detail sequence number", 88, 7, "TraceSuffix", "AchTransactionAddenda", "0", 'R'),
            Filler("FILLER", 95, 12)
        ];
    }

    private static IReadOnlyList<FieldSpec> BuildRecord8()
    {
        return
        [
            Constant("RECORDTYPE", "Record type", 1, 1, "8"),
            Calculated("SERVICECLASSCODE", "Service class code", 2, 3, "ServiceClassCode", null, '0', 'R'),
            Calculated("ENTRYADDENDACOUNT", "Entry addenda count", 5, 6, "EntryAddendaCount", null, '0', 'R'),
            Calculated("ENTRYHASH", "Entry hash", 11, 10, "EntryHash", null, '0', 'R'),
            Calculated("TOTALDEBITAMOUNT", "Total debit amount", 21, 12, "TotalDebitAmount", null, '0', 'R'),
            Calculated("TOTALCREDITAMOUNT", "Total credit amount", 33, 12, "TotalCreditAmount", null, '0', 'R'),
            Calculated("COMPANYIDENTIFICATION", "Company identification", 45, 10, "CompanyIdentification"),
            Filler("MESSAGEAUTHENTICATIONCODE", 55, 19),
            Filler("RESERVED", 74, 6),
            Calculated("ORIGINATINGDFI", "Originating DFI", 80, 8, "OriginatingDfi", null, '0', 'R'),
            Calculated("BATCHNUMBER", "Batch number", 88, 7, "BatchNumber", null, '0', 'R'),
            Filler("FILLER", 95, 12)
        ];
    }

    private static IReadOnlyList<FieldSpec> BuildRecord9()
    {
        return
        [
            Constant("RECORDTYPE", "Record type", 1, 1, "9"),
            Calculated("BATCHCOUNT", "Batch count", 2, 6, "BatchCount", null, '0', 'R'),
            Calculated("BLOCKCOUNT", "Block count", 8, 6, "BlockCount", null, '0', 'R'),
            Calculated("ENTRYADDENDACOUNT", "Entry addenda count", 14, 8, "EntryAddendaCount", null, '0', 'R'),
            Calculated("ENTRYHASH", "Entry hash", 22, 10, "EntryHash", null, '0', 'R'),
            Calculated("TOTALDEBITAMOUNT", "Total debit amount", 32, 12, "TotalDebitAmount", null, '0', 'R'),
            Calculated("TOTALCREDITAMOUNT", "Total credit amount", 44, 12, "TotalCreditAmount", null, '0', 'R'),
            Filler("FILLER", 56, 51)
        ];
    }

    private static FieldSpec Constant(
        string code,
        string name,
        int start,
        int length,
        string value,
        char pad = ' ',
        char justification = 'L')
    {
        return new FieldSpec(code, name, start, length, pad, justification, null, "CONSTANTE", value, null, null, null, null);
    }

    private static FieldSpec Source(
        string code,
        string name,
        int start,
        int length,
        string propertyPath,
        string entityName,
        string pad = " ",
        char justification = 'L')
    {
        var padChar = string.IsNullOrEmpty(pad) ? ' ' : pad[0];
        return new FieldSpec(code, name, start, length, padChar, justification, null, "ENTIDAD", null, entityName, propertyPath, null, null);
    }

    private static FieldSpec Calculated(
        string code,
        string name,
        int start,
        int length,
        string calculationType,
        string? formatMask = null,
        char pad = ' ',
        char justification = 'L')
    {
        var expression = JsonSerializer.Serialize(new { source = "runtime", calculationType });
        return new FieldSpec(code, name, start, length, pad, justification, formatMask, "EXPRESION", null, null, null, expression, null);
    }

    private static FieldSpec Filler(string code, int start, int length)
    {
        var expression = JsonSerializer.Serialize(new { source = "runtime", calculationType = "Filler" });
        return new FieldSpec(code, code, start, length, ' ', 'L', null, "EXPRESION", null, null, null, expression, null);
    }

    private sealed record ProfileSpec(
        string ProfileCode,
        string Name,
        string Description,
        string ClearingHouseCode,
        string FlowTypeCode,
        string NormativeSource,
        string NormativeVersion,
        string ApprovedRuleMatrix,
        bool IsPlaceholder,
        bool IsHomologated,
        string RoutingOrigin,
        string RoutingDestination,
        string ImmediateDestinationName,
        string ImmediateOriginName,
        string Prefix,
        string DirectionCode = "SALIDA",
        int VersionMajor = 1,
        int VersionMinor = 1,
        bool IncludeReturnAddenda99 = false);

    private sealed record CatalogIds(
        IReadOnlyDictionary<string, int> ClearingHouses,
        IReadOnlyDictionary<string, int> FlowTypes,
        IReadOnlyDictionary<string, int> Directions,
        IReadOnlyDictionary<string, int> Statuses,
        IReadOnlyDictionary<string, int> RecordCodes,
        IReadOnlyDictionary<string, int> SourceTypes,
        IReadOnlyDictionary<string, int> RuleTypes);

    private sealed record FieldSpec(
        string Code,
        string Name,
        int Start,
        int Length,
        char PadChar,
        char Justification,
        string? FormatMask,
        string SourceTypeCode,
        string? ConstantValue,
        string? EntityName,
        string? PropertyPath,
        string? ExpressionDsl,
        string? TransformationPipelineJson);
}
