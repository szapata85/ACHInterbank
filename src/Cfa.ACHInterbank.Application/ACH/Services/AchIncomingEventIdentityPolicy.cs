using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Cfa.ACHInterbank.Application.ACH.Services;

public static class AchIncomingEventIdentityPolicy
{
    public static string BuildReturnKey(
        int clearingHouseId,
        int transactionId,
        string? originalTrace,
        string? returnReasonCode,
        DateTime effectiveDate)
    {
        var canonical = string.Join('|',
            "incoming-return-v1",
            clearingHouseId,
            transactionId,
            (originalTrace ?? string.Empty).Trim(),
            (returnReasonCode ?? string.Empty).Trim().ToUpperInvariant(),
            effectiveDate.Date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }
}
