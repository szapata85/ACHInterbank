using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class NachaConfigValidationService : INachaConfigValidationService
{
    private static readonly HashSet<string> RequiredRecordCodes = ["1", "5", "6", "8", "9"];
    private readonly AchDbContext _context;

    public NachaConfigValidationService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<NachaConfigValidationResultDto> ValidateBeforePublishAsync(int profileId, CancellationToken ct = default)
    {
        var issues = new List<NachaConfigValidationIssueDto>();
        var profile = await _context.CfgProfiles
            .AsNoTracking()
            .Include(x => x.Records)
                .ThenInclude(x => x.RecordCode)
            .Include(x => x.LayoutVariants)
                .ThenInclude(x => x.RecordCode)
            .Include(x => x.LayoutVariants)
                .ThenInclude(x => x.Fields)
                    .ThenInclude(x => x.SourceDefinition)
            .FirstOrDefaultAsync(x => x.Id == profileId, ct);

        if (profile is null)
        {
            issues.Add(new NachaConfigValidationIssueDto { Severidad = "ERROR", Codigo = "PROFILE_NOT_FOUND", Mensaje = "Perfil no existe." });
            return Build(profileId, issues);
        }

        var configuredCodes = profile.Records.Where(x => x.IsEnabled).Select(x => x.RecordCode.Code).ToHashSet(StringComparer.OrdinalIgnoreCase);
        foreach (var required in RequiredRecordCodes)
        {
            if (!configuredCodes.Contains(required))
            {
                issues.Add(new NachaConfigValidationIssueDto { Severidad = "ERROR", Codigo = "MISSING_RECORD", Mensaje = $"Falta record obligatorio {required}." });
            }
        }

        var enabledRecords = profile.Records.Where(x => x.IsEnabled).ToList();
        if (enabledRecords.GroupBy(x => x.Sequence).Any(g => g.Count() > 1))
        {
            issues.Add(new NachaConfigValidationIssueDto { Severidad = "ERROR", Codigo = "INVALID_SEQUENCE", Mensaje = "Existen records habilitados con secuencia duplicada." });
        }

        if (enabledRecords.Count == 0)
        {
            issues.Add(new NachaConfigValidationIssueDto { Severidad = "ERROR", Codigo = "NO_ENABLED_RECORDS", Mensaje = "El perfil no tiene records habilitados." });
        }

        var ambiguousLayouts = profile.LayoutVariants
            .GroupBy(x => x.RecordCode.Code)
            .Where(g => g.Count(v => v.IsDefaultForRecord) > 1)
            .ToList();
        foreach (var ambiguous in ambiguousLayouts)
        {
            issues.Add(new NachaConfigValidationIssueDto { Severidad = "ERROR", Codigo = "AMBIGUOUS_LAYOUT", Mensaje = $"Más de un layout default para record {ambiguous.Key}." });
        }

        var missingVariant = enabledRecords
            .Where(r => !profile.LayoutVariants.Any(v => v.RecordCodeId == r.RecordCodeId))
            .Select(r => r.RecordCode.Code)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        foreach (var code in missingVariant)
        {
            issues.Add(new NachaConfigValidationIssueDto { Severidad = "ERROR", Codigo = "MISSING_VARIANT", Mensaje = $"No hay variantes configuradas para record {code}." });
        }

        foreach (var variant in profile.LayoutVariants)
        {
            var ordered = variant.Fields.Where(f => f.IsEnabled).OrderBy(f => f.StartPosition).ToList();
            if (ordered.Count == 0)
            {
                issues.Add(new NachaConfigValidationIssueDto
                {
                    Severidad = "ERROR",
                    Codigo = "EMPTY_VARIANT",
                    Mensaje = $"La variante {variant.VariantCode} no tiene fields habilitados."
                });
                continue;
            }

            for (var i = 1; i < ordered.Count; i++)
            {
                var prev = ordered[i - 1];
                var current = ordered[i];
                if (current.StartPosition < prev.StartPosition + prev.Length)
                {
                    issues.Add(new NachaConfigValidationIssueDto
                    {
                        Severidad = "ERROR",
                        Codigo = "FIELD_OVERLAP",
                        Mensaje = $"Solapamiento de fields en variante {variant.VariantCode}: {prev.FieldCode} y {current.FieldCode}."
                    });
                }
            }

            foreach (var field in ordered)
            {
                if (field.SourceDefinitionId == 0 || (string.IsNullOrWhiteSpace(field.SourceDefinition.PropertyPath)
                    && string.IsNullOrWhiteSpace(field.SourceDefinition.ConstantValue)
                    && string.IsNullOrWhiteSpace(field.SourceDefinition.ExpressionDsl)))
                {
                    issues.Add(new NachaConfigValidationIssueDto
                    {
                        Severidad = "ERROR",
                        Codigo = "MISSING_SOURCE",
                        Mensaje = $"Field {field.FieldCode} sin source crítico."
                    });
                }
            }
        }

        var conflictingEffective = await _context.CfgProfiles
            .AsNoTracking()
            .AnyAsync(x => x.Id != profile.Id
                           && x.ClearingHouseId == profile.ClearingHouseId
                           && x.FlowTypeId == profile.FlowTypeId
                           && x.DirectionId == profile.DirectionId
                           && x.ServiceClassId == profile.ServiceClassId
                           && x.Status.Code == "PUBLICADO"
                           && x.EffectiveFrom <= (profile.EffectiveTo ?? DateTime.MaxValue)
                           && (x.EffectiveTo ?? DateTime.MaxValue) >= profile.EffectiveFrom, ct);

        if (conflictingEffective)
        {
            issues.Add(new NachaConfigValidationIssueDto { Severidad = "ERROR", Codigo = "EFFECTIVE_CONFLICT", Mensaje = "Conflicto de vigencia con perfil publicado." });
        }

        return Build(profileId, issues);
    }

    private static NachaConfigValidationResultDto Build(int profileId, IReadOnlyList<NachaConfigValidationIssueDto> issues)
    {
        var blocking = issues.Count(x => string.Equals(x.Severidad, "ERROR", StringComparison.OrdinalIgnoreCase));
        var warnings = issues.Count(x => string.Equals(x.Severidad, "WARN", StringComparison.OrdinalIgnoreCase));

        return new NachaConfigValidationResultDto
        {
            ProfileId = profileId,
            IsValid = blocking == 0,
            ErroresBloqueantes = blocking,
            Advertencias = warnings,
            Resumen = blocking == 0
                ? "Validación exitosa."
                : $"Validación con {blocking} errores bloqueantes.",
            Issues = issues.ToList()
        };
    }
}
