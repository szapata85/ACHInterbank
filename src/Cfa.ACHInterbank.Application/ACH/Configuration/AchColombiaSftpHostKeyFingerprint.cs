namespace Cfa.ACHInterbank.Application.ACH.Configuration;

public static class AchColombiaSftpHostKeyFingerprint
{
    private const string Prefix = "SHA256:";

    public static bool Matches(string expectedFingerprint, ReadOnlySpan<byte> hostKey)
    {
        var expected = NormalizeRequired(expectedFingerprint);
        var actual = Prefix + Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(hostKey)).TrimEnd('=');
        return System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
            System.Text.Encoding.ASCII.GetBytes(expected),
            System.Text.Encoding.ASCII.GetBytes(actual));
    }

    public static string NormalizeRequired(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || !value.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("ACHCOL_MFT_SFTP_HOST_KEY_FINGERPRINT_INVALID");

        var payload = value[Prefix.Length..].Trim().TrimEnd('=');
        if (payload.Length == 0) throw new ArgumentException("ACHCOL_MFT_SFTP_HOST_KEY_FINGERPRINT_INVALID");

        byte[] bytes;
        try
        {
            var padded = payload.PadRight(payload.Length + ((4 - payload.Length % 4) % 4), '=');
            bytes = Convert.FromBase64String(padded);
        }
        catch (FormatException)
        {
            throw new ArgumentException("ACHCOL_MFT_SFTP_HOST_KEY_FINGERPRINT_INVALID");
        }

        if (bytes.Length != 32) throw new ArgumentException("ACHCOL_MFT_SFTP_HOST_KEY_FINGERPRINT_INVALID");
        return Prefix + Convert.ToBase64String(bytes).TrimEnd('=');
    }
}
