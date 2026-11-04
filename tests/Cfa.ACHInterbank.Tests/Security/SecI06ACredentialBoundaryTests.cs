using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Cfa.ACHInterbank.Application.Security.PasswordHashing;
using Xunit;

namespace Cfa.ACHInterbank.Tests.Security;

/// <summary>Casos límite de compatibilidad, sin consultar ni alterar el seed real.</summary>
public sealed class SecI06ACredentialBoundaryTests
{
    [Fact]
    public void UppercaseSha256LegacyRemainsValidWithoutRehashing()
    {
        const string syntheticPassword = "SecI06A-Legacy-Test-Only!";
        var stored = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(syntheticPassword)));

        Assert.Equal(CredentialVerificationResult.SuccessLegacy,
            CredentialPasswordHasher.Verify(syntheticPassword, stored));
        Assert.Equal(CredentialVerificationResult.Failed,
            CredentialPasswordHasher.Verify("wrong", stored));
    }

    [Fact]
    public void OlderVersionedPbkdf2RecommendsRehashButDoesNotRewriteHash()
    {
        const string syntheticPassword = "SecI06A-Pbkdf2-Test-Only!";
        const int iterations = 100_000;
        var salt = Enumerable.Range(0, 16).Select(i => (byte)i).ToArray(); // SOLO fixture de prueba
        var derived = Rfc2898DeriveBytes.Pbkdf2(
            syntheticPassword, salt, iterations, HashAlgorithmName.SHA256, 32);
        var stored = "pbkdf2-sha256$v1$" + iterations.ToString(CultureInfo.InvariantCulture) + "$"
            + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(derived);

        Assert.Equal(CredentialVerificationResult.SuccessRehashRecommended,
            CredentialPasswordHasher.Verify(syntheticPassword, stored));
        Assert.Equal(CredentialVerificationResult.Failed,
            CredentialPasswordHasher.Verify("wrong", stored));
        Assert.EndsWith(Convert.ToBase64String(derived), stored); // verificación de solo lectura
    }

    [Theory]
    [InlineData("pbkdf2-sha256$v1$99999999$AA==$AA==")]
    [InlineData("pbkdf2-sha256$v1$0$AA==$AA==")]
    [InlineData("pbkdf2-sha256$v1$-1$AA==$AA==")]
    [InlineData("pbkdf2-sha256$v3$600000$AA==$AA==")]
    public void OutOfPolicyHashFormatsFailClosed(string stored)
    {
        Assert.Equal(CredentialVerificationResult.Failed,
            CredentialPasswordHasher.Verify("synthetic-test-only", stored));
    }
}
