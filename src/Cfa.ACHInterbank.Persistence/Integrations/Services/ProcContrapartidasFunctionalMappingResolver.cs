using System.Globalization;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Integrations.Services;

[Scoped]
public class ProcContrapartidasFunctionalMappingResolver : IProcContrapartidasFunctionalMappingResolver
{
    private readonly AchDbContext _context;

    public ProcContrapartidasFunctionalMappingResolver(AchDbContext context)
    {
        _context = context;
    }

    public async Task<ProcContrapartidasRequestResolution?> TryResolveAsync(
        AchCycle cycle,
        IReadOnlyCollection<AchTransaction> transactions,
        DateTime executionDateTime,
        CancellationToken ct = default)
    {
        var method = await _context.Set<IntegrationMethod>()
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Code == "WSCFAACH.Proc_Contrapartidas" && x.IsActive, ct);

        if (method is null)
        {
            return null;
        }

        var published = await _context.Set<IntegrationMappingSet>()
            .AsNoTracking()
            .Where(x => x.MethodId == method.Id && x.Status == IntegrationMappingSetStatusEnum.Published)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync(ct);

        if (published is null)
        {
            return null;
        }

        var parameters = await _context.Set<IntegrationMethodParameter>()
            .AsNoTracking()
            .Where(x => x.MethodId == method.Id && x.IsActive && x.Direction == IntegrationParameterDirectionEnum.Input)
            .ToListAsync(ct);

        var rules = await _context.Set<IntegrationMappingRule>()
            .AsNoTracking()
            .Where(x => x.MappingSetId == published.Id && x.Enabled)
            .OrderBy(x => x.Priority)
            .ToListAsync(ct);

        if (rules.Count == 0)
        {
            return null;
        }

        var sourceCatalog = await _context.Set<IntegrationSourceCatalogField>()
            .AsNoTracking()
            .Where(x => x.MethodId == method.Id && x.IsActive)
            .ToDictionaryAsync(x => x.Id, ct);

        var tx = transactions.OrderBy(x => x.Id).FirstOrDefault();
        var resolved = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);

        foreach (var parameter in parameters)
        {
            var winner = rules
                .Where(r => r.ParameterId == parameter.Id)
                .OrderBy(r => r.Priority)
                .ThenBy(r => r.Id)
                .FirstOrDefault();

            if (winner is null)
            {
                continue;
            }

            resolved[parameter.ParameterPath] = ResolveValue(winner, sourceCatalog, cycle, tx, executionDateTime);
        }

        var contract = new ProcContrapartidasRequestContract
        {
            OFNIT = ResolveString("OFNIT"),
            OFEMP = ResolveString("OFEMP"),
            OFCTA = ResolveString("OFCTA"),
            OFDD = ResolveString("OFDD"),
            OFFECHEFEC = ResolveString("OFFECHEFEC"),
            OFMONDEB = ResolveDecimal("OFMONDEB"),
            OFMONCRE = ResolveDecimal("OFMONCRE"),
            OFIDARCH = ResolveInt("OFIDARCH"),
            OFIDLOT = ResolveInt("OFIDLOT"),
            OFST = ResolveString("OFST"),
            OFIDTX = ResolveString("OFIDTX"),
            OFIDREVER = ResolveInt("OFIDREVER"),
            OFIDEBAPLI = ResolveInt("OFIDEBAPLI"),
            OFIDCAMCOMPE = ResolveInt("OFIDCAMCOMPE", cycle.ClearingHouseId),
            OFDIRECCIONIP = ResolveString("OFDIRECCIONIP"),
            OFLIBRE = ResolveString("OFLIBRE"),
            OFLIBRE1 = ResolveInt("OFLIBRE1"),
            ANSIDLOTE = ResolveInt("ANSIDLOTE"),
            ANSST = ResolveString("ANSST"),
            ANCLC = ResolveString("ANCLC"),
            ANSIDTX = ResolveString("ANSIDTX"),
            ANSIDREVER = ResolveInt("ANSIDREVER")
        };

        var snapshotHash = await _context.Set<IntegrationMappingSetHistory>()
            .AsNoTracking()
            .Where(x => x.MappingSetId == published.Id)
            .OrderByDescending(x => x.PerformedAtUtc)
            .Select(x => x.SnapshotHash)
            .FirstOrDefaultAsync(ct) ?? string.Empty;

        return new ProcContrapartidasRequestResolution
        {
            Contract = contract,
            MappingSetId = published.Id,
            MappingVersion = published.Version,
            MappingSnapshotHash = snapshotHash,
            UsedFallback = false
        };

        string ResolveString(string key)
            => resolved.TryGetValue(key, out var v) ? v ?? string.Empty : string.Empty;

        int ResolveInt(string key, int fallback = 0)
            => resolved.TryGetValue(key, out var v) && int.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : fallback;

        decimal ResolveDecimal(string key)
            => resolved.TryGetValue(key, out var v) && decimal.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : 0m;
    }

    private static string? ResolveValue(
        IntegrationMappingRule rule,
        IReadOnlyDictionary<long, IntegrationSourceCatalogField> sourceCatalog,
        AchCycle cycle,
        AchTransaction? tx,
        DateTime executionDateTime)
    {
        if (!string.IsNullOrWhiteSpace(rule.FixedValue))
        {
            return ApplyTransformation(rule.FixedValue, rule.TransformationCode, rule.FormatMask);
        }

        var sourcePath = !string.IsNullOrWhiteSpace(rule.SourceFieldPath)
            ? rule.SourceFieldPath
            : (rule.SourceCatalogFieldId.HasValue && sourceCatalog.TryGetValue(rule.SourceCatalogFieldId.Value, out var field)
                ? field.FieldPath
                : string.Empty);

        var resolved = rule.SourceKind switch
        {
            IntegrationSourceKindEnum.Constant => rule.DefaultValue,
            IntegrationSourceKindEnum.Expression => null,
            _ => ResolvePath(sourcePath, cycle, tx)
        };

        resolved ??= rule.DefaultValue;
        return ApplyTransformation(resolved, rule.TransformationCode, rule.FormatMask);
    }

    private static string? ResolvePath(string sourcePath, AchCycle cycle, AchTransaction? tx)
    {
        var key = sourcePath.Trim().ToLowerInvariant();
        return key switch
        {
            "cycle.id" => cycle.Id,
            "cycle.processingdate" => cycle.ProcessingDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            "clearinghouse.id" => cycle.ClearingHouseId.ToString(CultureInfo.InvariantCulture),
            "clearinghouse.code" => cycle.ClearingHouse?.Code,
            "transaction.id" => tx?.Id.ToString(CultureInfo.InvariantCulture),
            "transaction.reference" => tx?.Reference,
            "transaction.amount" => tx?.Amount.ToString(CultureInfo.InvariantCulture),
            "transaction.tracenumber" => tx?.TraceNumber,
            "transaction.originatingdfi" => tx?.OriginatingDFI,
            "transaction.companyidentification" => tx?.CompanyIdentification,
            _ => null
        };
    }

    private static string? ApplyTransformation(string? value, string? transformationCode, string? formatMask)
    {
        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(transformationCode))
        {
            return value;
        }

        return transformationCode switch
        {
            "Trim" => value.Trim(),
            "Uppercase" => value.ToUpperInvariant(),
            "Lowercase" => value.ToLowerInvariant(),
            "PadLeft" when int.TryParse(formatMask, out var left) => value.PadLeft(left, '0'),
            "PadRight" when int.TryParse(formatMask, out var right) => value.PadRight(right, '0'),
            "NullIfEmpty" => string.IsNullOrWhiteSpace(value) ? null : value,
            _ => value
        };
    }
}
