using System.Security.Cryptography;
using System.Text;

namespace Cfa.ACHInterbank.Application.ACH.Services;

public static class AchIncomingEventIdentityPolicy
{
    public static string BuildReturnKey(
        int clearingHouseId,
        int transactionId,
        string? originalTrace,
        string? returnReasonCode)
    {
        var canonical = string.Join('|',
            // File name, ingestion and receipt/operational dates are transport
            // metadata. They must not change the identity of the same return
            // replayed in another file.
            "incoming-return-v2",
            clearingHouseId,
            transactionId,
            (originalTrace ?? string.Empty).Trim(),
            (returnReasonCode ?? string.Empty).Trim().ToUpperInvariant());
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical))).ToLowerInvariant();
    }
}
