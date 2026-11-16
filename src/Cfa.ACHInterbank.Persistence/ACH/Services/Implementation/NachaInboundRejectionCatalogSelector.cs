using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// Ensures a rejection is interpreted within its chamber and service context.
/// STA D01-D06 are file-transfer causes (DSP-152 Annex B), not NACHA-M errors.
/// This selector never maps a technical NACHA error into a supposed official STA cause.
/// </summary>
internal static class NachaInboundRejectionCatalogSelector
{
    private const string StaSource = "CENIT DSP-152 Anexo B";
    private static readonly HashSet<string> StaCodes =
        new(StringComparer.OrdinalIgnoreCase) { "D01", "D02", "D03", "D04", "D05", "D06" };

    internal static void EnsureNachaProfileMatchesChamber(
        int? resolvedHouseId, string? resolvedHouseCode, bool isCenitNachaProfile)
    {
        if (!resolvedHouseId.HasValue)
            return; // Legacy direct parsing: no declared chamber from which to infer a service.

        if (string.IsNullOrWhiteSpace(resolvedHouseCode))
            throw new InvalidOperationException("NACHA_CLEARING_HOUSE_NOT_FOUND");

        var code = resolvedHouseCode.Trim();
        if ((string.Equals(code, "CENIT", StringComparison.OrdinalIgnoreCase) && !isCenitNachaProfile)
            || ((string.Equals(code, "ACHCOL", StringComparison.OrdinalIgnoreCase)
                  || string.Equals(code, "ACH", StringComparison.OrdinalIgnoreCase)) && isCenitNachaProfile))
        {
            throw new InvalidOperationException("NACHA_CLEARING_HOUSE_PROFILE_MISMATCH");
        }
    }

    internal static IReadOnlyDictionary<string, AchFileRejectionCode> SelectForNacha(
        IEnumerable<AchFileRejectionCode> catalogue, int? resolvedHouseId, bool isCenitNachaProfile)
    {
        // NACHA-M CENIT has an independent fatal/formal domain; the caller does not
        // read STA catalogue rows at all. Keep the contract defensive as well.
        if (isCenitNachaProfile)
            return new Dictionary<string, AchFileRejectionCode>(StringComparer.OrdinalIgnoreCase);

        var result = new Dictionary<string, AchFileRejectionCode>(StringComparer.OrdinalIgnoreCase);
        var applicable = catalogue.Where(row => row.IsActive && !IsStaRow(row)
            && (!row.ClearingHouseId.HasValue || row.ClearingHouseId == resolvedHouseId));
        foreach (var group in applicable.GroupBy(row => row.Code, StringComparer.OrdinalIgnoreCase))
        {
            AchFileRejectionCode[] owned = resolvedHouseId.HasValue
                ? group.Where(row => row.ClearingHouseId == resolvedHouseId).ToArray()
                : [];
            var candidates = owned.Length > 0
                ? owned
                : group.Where(row => !row.ClearingHouseId.HasValue).ToArray();
            if (candidates.Length != 1)
                throw new InvalidOperationException("NACHA_REJECTION_CATALOG_AMBIGUOUS");
            result.Add(group.Key, candidates[0]);
        }
        return result;
    }

    /// <summary>
    /// A pure catalogue boundary for STA consumers. It does not authorize the
    /// NACHA upload endpoint to ingest, decrypt or reject STA files.
    /// </summary>
    internal static IReadOnlyDictionary<string, AchFileRejectionCode> SelectForSta(
        IEnumerable<AchFileRejectionCode> catalogue, int cenitHouseId)
    {
        var result = new Dictionary<string, AchFileRejectionCode>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in catalogue.Where(row => row.IsActive && row.ClearingHouseId == cenitHouseId
                     && IsStaRow(row)))
        {
            if (!result.TryAdd(row.Code, row))
                throw new InvalidOperationException("STA_REJECTION_CATALOG_AMBIGUOUS");
        }
        return result;
    }

    private static bool IsStaRow(AchFileRejectionCode row)
        => StaCodes.Contains(row.Code)
           && row.RegulatorySource.StartsWith(StaSource, StringComparison.OrdinalIgnoreCase);
}
