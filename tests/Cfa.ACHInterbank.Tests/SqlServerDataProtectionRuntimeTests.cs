using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.CertificateManagement;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class SqlServerDataProtectionRuntimeTests
{
    [Fact]
    public async Task DatabaseKeyRing_ShouldRecoverPrivateCertificateAcrossInstancesAndSign()
    {
        var connectionString = Environment.GetEnvironmentVariable("ACH_SQLSERVER_RUNTIME_CONNECTION");
        if (string.IsNullOrWhiteSpace(connectionString)) return;

        byte[] protectedMaterial;
        using (var firstProvider = CreateProvider(connectionString))
        {
            await using var scope = firstProvider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<AchDbContext>();
            (await context.DataProtectionKeys.AsNoTracking().CountAsync()).Should().BeGreaterThan(0);
            protectedMaterial = await context.DigitalCertificateVersions
                .AsNoTracking()
                .Where(x => x.PrivateMaterialStorageMode == CertificateStorageMode.DatabaseEncrypted
                    && x.HasPrivateKey
                    && x.EncryptedPrivateMaterial != null)
                .Select(x => x.EncryptedPrivateMaterial!)
                .FirstAsync();

            var protector = new DataProtectionCertificatePrivateMaterialProtector(
                firstProvider.GetRequiredService<IDataProtectionProvider>());
            using var certificate = protector.Unprotect(protectedMaterial);
            VerifySignature(certificate).Should().BeTrue();
        }

        using var secondProvider = CreateProvider(connectionString);
        var secondProtector = new DataProtectionCertificatePrivateMaterialProtector(
            secondProvider.GetRequiredService<IDataProtectionProvider>());
        using var recoveredCertificate = secondProtector.Unprotect(protectedMaterial);
        VerifySignature(recoveredCertificate).Should().BeTrue();
    }

    private static ServiceProvider CreateProvider(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddDbContext<AchDbContext>(options => options.UseSqlServer(connectionString));
        services.AddDataProtection()
            .SetApplicationName(DataProtectionKeyRingConfiguration.ApplicationName)
            .PersistKeysToDbContext<AchDbContext>();
        return services.BuildServiceProvider();
    }

    private static bool VerifySignature(X509Certificate2 certificate)
    {
        var payload = RandomNumberGenerator.GetBytes(64);
        try
        {
            using var privateKey = certificate.GetRSAPrivateKey();
            using var publicKey = certificate.GetRSAPublicKey();
            if (privateKey is null || publicKey is null) return false;
            var signature = privateKey.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            try
            {
                return publicKey.VerifyData(payload, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(signature);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(payload);
        }
    }
}
