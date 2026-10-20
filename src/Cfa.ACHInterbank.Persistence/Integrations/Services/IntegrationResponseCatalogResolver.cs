using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Integrations.Services;

[Scoped]
public sealed class IntegrationResponseCatalogResolver : IIntegrationResponseCatalogResolver
{
    private readonly AchDbContext _context;

    public IntegrationResponseCatalogResolver(AchDbContext context)
    {
        _context = context;
    }

    public async Task<IntegrationResponseCatalogResult> ResolveAsync(
        string source,
        string method,
        string? responseCode,
        DateTime processedAtUtc,
        CancellationToken ct = default)
    {
        var normalizedSource = Normalize(source);
        var normalizedMethod = Normalize(method);
        var normalizedCode = Normalize(responseCode);
        var effectiveAt = processedAtUtc.Kind == DateTimeKind.Utc
            ? processedAtUtc
            : processedAtUtc.ToUniversalTime();

        if (normalizedSource.Length == 0 || normalizedMethod.Length == 0 || normalizedCode.Length == 0)
        {
            return Unknown(normalizedSource, normalizedMethod, normalizedCode);
        }

        var candidates = await _context.IntegrationResponseCodes
            .AsNoTracking()
            .Include(x => x.Method)
            .Where(x => x.Source.ToUpper() == normalizedSource)
            .Where(x => x.Code.ToUpper() == normalizedCode)
            .Where(x => x.Method.Code.ToUpper() == normalizedMethod
                || x.Method.DisplayName.ToUpper() == normalizedMethod
                || x.Method.Code.ToUpper().EndsWith("." + normalizedMethod))
            .Where(x => x.IsActive)
            .Where(x => x.EffectiveFromUtc <= effectiveAt)
            .Where(x => !x.EffectiveToUtc.HasValue || x.EffectiveToUtc.Value >= effectiveAt)
            .ToListAsync(ct);

        if (candidates.Count != 1)
        {
            return Unknown(normalizedSource, normalizedMethod, normalizedCode);
        }

        var match = candidates[0];
        return new IntegrationResponseCatalogResult(
            match.Id,
            match.Code,
            match.Description,
            match.Source,
            match.Category,
            match.Method.DisplayName,
            match.BusinessStatus,
            match.RetryAllowed,
            match.RequiresManualReview,
            match.IsActive,
            match.TargetTransactionState,
            true);
    }

    private static IntegrationResponseCatalogResult Unknown(string source, string method, string code)
        => new(
            null,
            code,
            "Código pendiente de parametrización",
            source,
            IntegrationResponseCategory.CoreSoapResponse,
            method,
            IntegrationResponseBusinessStatus.PendingCatalog,
            false,
            true,
            false,
            string.Empty,
            false);

    private static string Normalize(string? value)
        => (value ?? string.Empty).Trim().ToUpperInvariant();
}
