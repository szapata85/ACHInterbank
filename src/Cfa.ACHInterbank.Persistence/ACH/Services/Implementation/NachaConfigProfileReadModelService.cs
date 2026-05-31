using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class NachaConfigProfileReadModelService : INachaConfigProfileReadModelService
{
    private static readonly string[] ControlTotalFields =
    [
        "ENTRYADDENDACOUNT",
        "ENTRYHASH",
        "TOTALDEBITAMOUNT",
        "TOTALCREDITAMOUNT",
        "BATCHCOUNT",
        "BLOCKCOUNT"
    ];

    private readonly AchDbContext _context;

    public NachaConfigProfileReadModelService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<NachaConfigProfilesDashboardReadModel> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var profiles = await GetProfilesAsync(cancellationToken);

        return new NachaConfigProfilesDashboardReadModel
        {
            ProfileCount = profiles.Count,
            PublishedProfileCount = profiles.Count(x => x.IsPublished),
            CurrentProfileCount = profiles.Count(x => x.IsCurrent),
            LayoutVariantCount = profiles.Sum(x => x.LayoutVariantCount),
            FieldCount = profiles.Sum(x => x.FieldCount),
            ClearingHouses = profiles.Select(x => x.ClearingHouseCode).Where(x => !string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x => x).ToList(),
            RecordTypes = profiles.SelectMany(x => x.RecordTypes).Distinct().OrderBy(x => x).ToList(),
            Warnings = profiles.Count == 0
                ? ["No hay perfiles nacha-config oficiales persistidos para mostrar."]
                : []
        };
    }

    public async Task<IReadOnlyList<NachaConfigProfileReadModel>> GetProfilesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var profiles = await BaseProfiles()
            .OrderBy(x => x.ClearingHouse.Code)
            .ThenBy(x => x.ProfileCode)
            .ToListAsync(cancellationToken);

        return profiles.Select(x => ProjectProfile(x, now)).ToList();
    }

    public async Task<NachaConfigProfileDetailReadModel?> GetProfileAsync(int profileId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var profile = await DetailProfiles().FirstOrDefaultAsync(x => x.Id == profileId, cancellationToken);
        return profile is null ? null : ProjectDetail(profile, now);
    }

    public async Task<NachaConfigProfileDetailReadModel?> GetProfileByCodeAsync(string profileCode, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var normalized = (profileCode ?? string.Empty).Trim();
        var profile = await DetailProfiles().FirstOrDefaultAsync(x => x.ProfileCode == normalized, cancellationToken);
        return profile is null ? null : ProjectDetail(profile, now);
    }

    public async Task<IReadOnlyList<NachaConfigProfileVariantReadModel>> GetVariantsAsync(int profileId, CancellationToken cancellationToken = default)
    {
        var variants = await _context.CfgLayoutVariants
            .AsNoTracking()
            .Include(x => x.RecordCode)
            .Include(x => x.Status)
            .Include(x => x.Fields)
                .ThenInclude(x => x.SourceDefinition)
            .Where(x => x.ProfileId == profileId)
            .OrderBy(x => x.RecordCode.Code)
            .ThenBy(x => x.Priority)
            .ToListAsync(cancellationToken);

        return variants.Select(ProjectVariant).ToList();
    }

    public async Task<IReadOnlyList<NachaConfigProfileFieldReadModel>> GetFieldsAsync(int profileId, CancellationToken cancellationToken = default)
    {
        var fields = await _context.CfgLayoutFields
            .AsNoTracking()
            .Include(x => x.LayoutVariant)
                .ThenInclude(x => x.RecordCode)
            .Include(x => x.SourceDefinition)
                .ThenInclude(x => x.DataSourceType)
            .Where(x => x.LayoutVariant.ProfileId == profileId)
            .OrderBy(x => x.LayoutVariant.RecordCode.Code)
            .ThenBy(x => x.StartPosition)
            .ToListAsync(cancellationToken);

        return fields.Select(ProjectField).ToList();
    }

    private IQueryable<CfgProfile> BaseProfiles()
        => _context.CfgProfiles
            .AsNoTracking()
            .Include(x => x.Status)
            .Include(x => x.ClearingHouse)
            .Include(x => x.FlowType)
            .Include(x => x.Records)
                .ThenInclude(x => x.RecordCode)
            .Include(x => x.LayoutVariants)
                .ThenInclude(x => x.Fields);

    private IQueryable<CfgProfile> DetailProfiles()
        => BaseProfiles()
            .Include(x => x.LayoutVariants)
                .ThenInclude(x => x.RecordCode)
            .Include(x => x.LayoutVariants)
                .ThenInclude(x => x.Status)
            .Include(x => x.LayoutVariants)
                .ThenInclude(x => x.Fields)
                    .ThenInclude(x => x.SourceDefinition)
                        .ThenInclude(x => x.DataSourceType);

    private static NachaConfigProfileReadModel ProjectProfile(CfgProfile profile, DateTime now)
        => new()
        {
            ProfileId = profile.Id,
            ProfileCode = profile.ProfileCode,
            ProfileName = profile.NameEs,
            ClearingHouseCode = profile.ClearingHouse.Code,
            FlowType = profile.FlowType.Code,
            Status = profile.Status.Code,
            Version = $"v{profile.VersionMajor}.{profile.VersionMinor}",
            IsPublished = string.Equals(profile.Status.Code, "PUBLICADO", StringComparison.OrdinalIgnoreCase),
            IsCurrent = profile.EffectiveFrom <= now && (profile.EffectiveTo is null || profile.EffectiveTo >= now),
            EffectiveFrom = profile.EffectiveFrom,
            EffectiveTo = profile.EffectiveTo,
            LayoutVariantCount = profile.LayoutVariants.Count,
            FieldCount = profile.LayoutVariants.SelectMany(x => x.Fields).Count(),
            RecordTypes = profile.Records.Select(x => x.RecordCode.Code).Distinct().OrderBy(x => x).ToList()
        };

    private static NachaConfigProfileDetailReadModel ProjectDetail(CfgProfile profile, DateTime now)
    {
        var summary = ProjectProfile(profile, now);
        return new NachaConfigProfileDetailReadModel
        {
            ProfileId = summary.ProfileId,
            ProfileCode = summary.ProfileCode,
            ProfileName = summary.ProfileName,
            ClearingHouseCode = summary.ClearingHouseCode,
            FlowType = summary.FlowType,
            Status = summary.Status,
            Version = summary.Version,
            IsPublished = summary.IsPublished,
            IsCurrent = summary.IsCurrent,
            EffectiveFrom = summary.EffectiveFrom,
            EffectiveTo = summary.EffectiveTo,
            LayoutVariantCount = summary.LayoutVariantCount,
            FieldCount = summary.FieldCount,
            RecordTypes = summary.RecordTypes,
            Variants = profile.LayoutVariants
                .OrderBy(x => x.RecordCode.Code)
                .ThenBy(x => x.Priority)
                .Select(ProjectVariant)
                .ToList(),
            Fields = profile.LayoutVariants
                .OrderBy(x => x.RecordCode.Code)
                .SelectMany(x => x.Fields.OrderBy(f => f.StartPosition))
                .Select(ProjectField)
                .ToList()
        };
    }

    private static NachaConfigProfileVariantReadModel ProjectVariant(CfgLayoutVariant variant)
        => new()
        {
            VariantId = variant.Id,
            VariantCode = variant.VariantCode,
            RecordType = variant.RecordCode.Code,
            RecordLength = variant.TotalLength,
            BlockingFactor = ResolveBlockingFactor(variant),
            IsActive = variant.IsDefaultForRecord && string.Equals(variant.Status.Code, "PUBLICADO", StringComparison.OrdinalIgnoreCase),
            FieldCount = variant.Fields.Count
        };

    private static NachaConfigProfileFieldReadModel ProjectField(CfgLayoutField field)
    {
        var sourceType = field.SourceDefinition.DataSourceType.Code;
        return new NachaConfigProfileFieldReadModel
        {
            FieldId = field.Id,
            RecordType = field.LayoutVariant.RecordCode.Code,
            FieldName = string.IsNullOrWhiteSpace(field.FieldNameEs) ? field.FieldCode : field.FieldNameEs,
            StartPosition = field.StartPosition,
            Length = field.Length,
            EndPosition = field.StartPosition + field.Length - 1,
            DataType = sourceType,
            IsRequired = field.IsEnabled,
            DefaultValue = field.SourceDefinition.ConstantValue,
            SourceFieldPath = BuildSourceFieldPath(field.SourceDefinition),
            PaddingDirection = field.Justification == 'R' ? "LeftPad" : "RightPad",
            PaddingChar = field.PadChar.ToString(),
            Format = field.FormatMask,
            IsComputed = string.Equals(sourceType, "EXPRESION", StringComparison.OrdinalIgnoreCase),
            IsControlTotalField = ControlTotalFields.Contains(field.FieldCode, StringComparer.OrdinalIgnoreCase)
        };
    }

    private static string? BuildSourceFieldPath(CfgFieldSourceDefinition source)
    {
        if (!string.IsNullOrWhiteSpace(source.EntityName) && !string.IsNullOrWhiteSpace(source.PropertyPath))
        {
            return $"{source.EntityName}.{source.PropertyPath}";
        }

        if (!string.IsNullOrWhiteSpace(source.PropertyPath))
        {
            return source.PropertyPath;
        }

        if (!string.IsNullOrWhiteSpace(source.ExpressionDsl))
        {
            return "computed";
        }

        return null;
    }

    private static int ResolveBlockingFactor(CfgLayoutVariant variant)
    {
        var field = variant.Fields.FirstOrDefault(x => string.Equals(x.FieldCode, "BLOCKINGFACTOR", StringComparison.OrdinalIgnoreCase));
        return int.TryParse(field?.SourceDefinition?.ConstantValue, out var value) ? value : 10;
    }
}
