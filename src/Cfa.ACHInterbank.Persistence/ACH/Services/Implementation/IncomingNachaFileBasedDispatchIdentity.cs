using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// F3.3 canonical business identity for NACHA-origin monetary dispatch.
/// No PII is persisted in clear text by this key builder. Hashing does not
/// replace the authoritative duplication/concurrency controls in the queue.
/// </summary>
internal static class IncomingNachaFileBasedDispatchIdentity
{
    public static bool TryBuild(
        int clearingHouseId,
        DateTime operationalDate,
        NachaHeader header,
        EntryDetail entry,
        out string key)
    {
        key = string.Empty;
        var origin = Normalize(header.ImmediateOrigin);
        var trace = Normalize(entry.SequenceNumber);
        var destination = Normalize(entry.ReceivingParticipantEntityCode);
        var transactionCode = Normalize(entry.TransactionCode);
        var account = Normalize(entry.AccountNumber);

        if (clearingHouseId <= 0 || operationalDate == default
            || string.IsNullOrWhiteSpace(origin)
            || trace.Length != 15 || !trace.All(char.IsAsciiDigit)
            || string.IsNullOrWhiteSpace(destination)
            || string.IsNullOrWhiteSpace(transactionCode)
            || string.IsNullOrWhiteSpace(account)
            || entry.BatchNumber <= 0
            || entry.Amount is null or <= 0m)
        {
            return false;
        }

        // Version and SOAP method are domain separators; length prefixes avoid
        // accidental concatenation collisions. No internal/external cycle, ingestion
        // ID, .env random bytes or original file name participates in the key.
        var parts = new[]
        {
            "ACHCOL-FILEBASED-DISPATCH-V1", "Proc_Transacciones",
            clearingHouseId.ToString(CultureInfo.InvariantCulture),
            operationalDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture),
            origin, trace,
            entry.BatchNumber.ToString(CultureInfo.InvariantCulture),
            transactionCode, destination, account,
            Normalize(entry.RecipIdNumber),
            entry.Amount.Value.ToString("G29", CultureInfo.InvariantCulture)
        };
        var canonical = new StringBuilder();
        foreach (var part in parts)
        {
            canonical.Append(part.Length.ToString(CultureInfo.InvariantCulture))
                .Append(':').Append(part);
        }

        key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonical.ToString())));
        return true;
    }

    private static string Normalize(string? input) => (input ?? string.Empty).Trim().ToUpperInvariant();
}
