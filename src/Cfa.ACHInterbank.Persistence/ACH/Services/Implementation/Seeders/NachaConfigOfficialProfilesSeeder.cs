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
        var catalog = await LoadCatalogAsync();

        await EnsureProfileAsync(new ProfileSpec(
            ProfileCode: "OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0",
            Name: "Perfil oficial ACH Colombia salida original",
            Description: "Perfil oficial UAT/local table-driven para ACH Colombia. Fuente normativa: MAN-004 V32.",
            ClearingHouseCode: "ACH",
            NormativeSource: "MAN-004 V32",
            RoutingOrigin: "000101006",
            RoutingDestination: "000128300",
            ImmediateDestinationName: "ACH COLOMBIA",
            ImmediateOriginName: "CFA UAT",
            Prefix: "ACH"));

        await EnsureProfileAsync(new ProfileSpec(
            ProfileCode: "OFFICIAL_CENIT_SALIDA_ORIGINAL_V1_0",
            Name: "Perfil oficial CENIT salida original",
            Description: "Perfil oficial UAT/local table-driven para CENIT. Fuente normativa pendiente de homologacion formal: CENIT/DSP-152 placeholder.",
            ClearingHouseCode: "CENIT",
            NormativeSource: "CENIT/DSP-152 placeholder UAT",
            RoutingOrigin: "01111111",
            RoutingDestination: "02222222",
            ImmediateDestinationName: "CENIT",
            ImmediateOriginName: "CFA UAT",
            Prefix: "CENIT"));

        await _context.SaveChangesAsync();

        async Task EnsureProfileAsync(ProfileSpec spec)
        {
            var profile = await _context.CfgProfiles
                .Include(x => x.Tags)
                .Include(x => x.Records)
                .Include(x => x.LayoutVariants)
                    .ThenInclude(x => x.Fields)
                        .ThenInclude(x => x.SourceDefinition)
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
            profile.FlowTypeId = catalog.FlowTypes["ORIGINAL"];
            profile.DirectionId = catalog.Directions["SALIDA"];
            profile.ServiceClassId = null;
            profile.ContextPriority = 10;
            profile.EffectiveFrom = EffectiveFrom;
            profile.EffectiveTo = null;
            profile.StatusId = catalog.Statuses["PUBLICADO"];
            profile.VersionMajor = 1;
            profile.VersionMinor = 0;
            profile.PublishedAt = PublishedAt;
            profile.PublishedBy = "system-phase-6b1";
            profile.RowVersion = BuildRowVersion(spec.Prefix);
            profile.UpdatedAt = AuditTimestamp;

            await _context.SaveChangesAsync();

            await EnsureTagAsync(profile, "NormativeSource", spec.NormativeSource);
            await EnsureTagAsync(profile, "Phase", "6B.1");
            await EnsureTagAsync(profile, "ProductionDecision", "NO-GO");

            var sequence = 10;
            foreach (var recordCode in RecordCodes)
            {
                var variant = await EnsureVariantAsync(profile, spec, recordCode, sequence, catalog);
                await EnsureProfileRecordAsync(profile, recordCode, sequence, variant.Id, catalog);
                sequence += 10;
            }
        }

        async Task EnsureTagAsync(CfgProfile profile, string key, string value)
        {
            if (profile.Tags.Any(x => x.TagKey == key && x.TagValue == value))
            {
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
            CatalogIds catalog)
        {
            var variantCode = $"{spec.Prefix}_R{recordCode}_BASE_V1";
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
            variant.Priority = 10;
            variant.EffectiveFrom = EffectiveFrom;
            variant.EffectiveTo = null;
            variant.StatusId = catalog.Statuses["PUBLICADO"];
            variant.TotalLength = 106;
            variant.SelectionPredicateJson = null;
            variant.IsDefaultForRecord = true;
            variant.UpdatedAt = AuditTimestamp;

            await _context.SaveChangesAsync();

            foreach (var field in BuildFields(spec, recordCode))
            {
                await EnsureFieldAsync(variant, field, catalog);
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

        async Task EnsureFieldAsync(CfgLayoutVariant variant, FieldSpec field, CatalogIds catalog)
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
            await _context.CatDataSourceTypes.AsNoTracking().ToDictionaryAsync(x => x.Code, x => x.Id));
    }

    private static byte[] BuildRowVersion(string prefix)
    {
        return prefix == "CENIT" ? [6, 11, 2, 1] : [6, 11, 1, 1];
    }

    private static IReadOnlyList<FieldSpec> BuildFields(ProfileSpec profile, string recordCode)
    {
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
        string NormativeSource,
        string RoutingOrigin,
        string RoutingDestination,
        string ImmediateDestinationName,
        string ImmediateOriginName,
        string Prefix);

    private sealed record CatalogIds(
        IReadOnlyDictionary<string, int> ClearingHouses,
        IReadOnlyDictionary<string, int> FlowTypes,
        IReadOnlyDictionary<string, int> Directions,
        IReadOnlyDictionary<string, int> Statuses,
        IReadOnlyDictionary<string, int> RecordCodes,
        IReadOnlyDictionary<string, int> SourceTypes);

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
