using System.Globalization;
using System.Security.Cryptography;
using System.Text;

namespace Cfa.ACHInterbank.Application.Security.PasswordHashing;

/// <summary>
/// Formato versionado: pbkdf2-sha256$v1$iteraciones$saltBase64$hashBase64.
/// La compatibilidad SHA-256 legado es de SOLO LECTURA para no romper cuentas existentes,
/// incluido el seed administrativo que se conserva sin ninguna modificación.
/// </summary>
public static class CredentialPasswordHasher
{
    private const string Algorithm = "pbkdf2-sha256";
    private const string Version = "v1";
    private const int CurrentIterations = 600_000;
    private const int MinimumAcceptedIterations = 100_000;
    private const int MaximumAcceptedIterations = 1_200_000;
    private const int SaltLength = 16;
    private const int DerivedKeyLength = 32;

    public static string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, CurrentIterations, HashAlgorithmName.SHA256, DerivedKeyLength);
        return $"{Algorithm}${Version}${CurrentIterations.ToString(CultureInfo.InvariantCulture)}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    public static CredentialVerificationResult Verify(string? password, string? storedHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(storedHash) || storedHash.Length > 256)
        {
            return CredentialVerificationResult.Failed;
        }

        // Formato SHA-256 histórico (64 caracteres hex). Nunca se generan hashes nuevos en este formato.
        if (storedHash.Length == 64 && storedHash.All(IsHex))
        {
            var expected = Convert.FromHexString(storedHash);
            var computed = SHA256.HashData(Encoding.UTF8.GetBytes(password));
            return CryptographicOperations.FixedTimeEquals(computed, expected)
                ? CredentialVerificationResult.SuccessLegacy
                : CredentialVerificationResult.Failed;
        }

        var parts = storedHash.Split('$');
        if (parts.Length != 5 || parts[0] != Algorithm || parts[1] != Version ||
            !int.TryParse(parts[2], NumberStyles.None, CultureInfo.InvariantCulture, out var iterations) ||
            iterations < MinimumAcceptedIterations || iterations > MaximumAcceptedIterations)
        {
            return CredentialVerificationResult.Failed;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[3]);
            var expected = Convert.FromBase64String(parts[4]);
            if (salt.Length != SaltLength || expected.Length != DerivedKeyLength)
            {
                return CredentialVerificationResult.Failed;
            }

            var computed = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, DerivedKeyLength);
            if (!CryptographicOperations.FixedTimeEquals(computed, expected))
            {
                return CredentialVerificationResult.Failed;
            }

            // Permite preparar rehash futuro, pero SEC-I04 NO reescribe hashes legados al iniciar sesión.
            return iterations < CurrentIterations
                ? CredentialVerificationResult.SuccessRehashRecommended
                : CredentialVerificationResult.Success;
        }
        catch (FormatException)
        {
            return CredentialVerificationResult.Failed;
        }
    }

    private static bool IsHex(char value) =>
        value is >= '0' and <= '9' or >= 'a' and <= 'f' or >= 'A' and <= 'F';
}

public enum CredentialVerificationResult
{
    Failed = 0,
    Success = 1,
    SuccessLegacy = 2,
    SuccessRehashRecommended = 3
}
