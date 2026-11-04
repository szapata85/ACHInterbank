using Cfa.ACHInterbank.Application.Security.PasswordHashing;
using System.Security.Cryptography;
using System.Text;
using Xunit;

namespace Cfa.ACHInterbank.Tests.Security;

public class CredentialPasswordHasherTests
{
    [Fact]
    public void NewPasswordsUseSaltedVersionedPbkdf2()
    {
        var first = CredentialPasswordHasher.HashPassword("TestOnly#Password2026");
        var second = CredentialPasswordHasher.HashPassword("TestOnly#Password2026");

        Assert.StartsWith("pbkdf2-sha256$v1$600000$", first);
        Assert.NotEqual(first, second);
        Assert.Equal(CredentialVerificationResult.Success,
            CredentialPasswordHasher.Verify("TestOnly#Password2026", first));
        Assert.Equal(CredentialVerificationResult.Failed,
            CredentialPasswordHasher.Verify("incorrect", first));
    }

    [Fact]
    public void HistoricalSha256IsVerifiedWithoutGeneratingNewLegacyHashes()
    {
        const string password = "LegacyTestOnly#2026";
        var legacy = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password))).ToLowerInvariant();

        Assert.Equal(CredentialVerificationResult.SuccessLegacy,
            CredentialPasswordHasher.Verify(password, legacy));
        Assert.Equal(CredentialVerificationResult.Failed,
            CredentialPasswordHasher.Verify("wrong", legacy));
    }

    [Theory]
    [InlineData("")]
    [InlineData("pbkdf2-sha256$v1$999999999$YWJj$YWJj")]
    [InlineData("pbkdf2-sha256$v1$600000$###$###")]
    [InlineData("pbkdf2-sha256$v1$600000$YWJj$YWJj")]
    [InlineData("pbkdf2-sha256$v2$600000$YWJj$YWJj")]
    [InlineData("fffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffz")]
    public void MalformedHashesAreRejectedWithoutThrowing(string storedHash)
    {
        Assert.Equal(CredentialVerificationResult.Failed,
            CredentialPasswordHasher.Verify("some-password", storedHash));
    }

    [Fact]
    public void NullOrEmptyPasswordIsRejected()
    {
        var hash = CredentialPasswordHasher.HashPassword("PasswordForTests2026!");
        Assert.Equal(CredentialVerificationResult.Failed, CredentialPasswordHasher.Verify(null, hash));
        Assert.Equal(CredentialVerificationResult.Failed, CredentialPasswordHasher.Verify(string.Empty, hash));
    }
}
