using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cfa.ACHInterbank.Application.ACHSobreDigital.CertificateManagement;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.CertificateManagement;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Tests;

public sealed class CertificateBootstrapHostedServiceTests
{
    [Fact]
    public async Task Bootstrap_IsIdempotentAndDoesNotDuplicateThumbprintPerContext()
    {
        const string password = "unit-test-bootstrap-password";
        var directory = Directory.CreateTempSubdirectory("ach-cert-bootstrap-");
        try
        {
            using var certificate = CreateCertificate();
            await File.WriteAllBytesAsync(Path.Combine(directory.FullName, "ACHcolombia.cer"), certificate.Export(X509ContentType.Cert));
            var pfx = certificate.Export(X509ContentType.Pfx, password);
            try
            {
                await File.WriteAllBytesAsync(Path.Combine(directory.FullName, "CFA.pfx"), pfx);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(pfx);
            }

            var services = new ServiceCollection();
            var databaseName = $"bootstrap-{Guid.NewGuid():N}";
            services.AddLogging();
            services.AddDataProtection();
            services.AddDbContext<AchDbContext>(options => options.UseInMemoryDatabase(databaseName));
            services.AddScoped<ICertificateSecretProtector, CertificateSecretProtectorService>();
            services.AddSingleton<ICertificatePrivateMaterialProtector, DataProtectionCertificatePrivateMaterialProtector>();
            services.AddScoped<ICertificateLoadService, CertificateLoadService>();
            services.AddScoped<ICertificateValidationService, CertificateValidationService>();
            services.AddScoped<ICertificateActivationService, CertificateActivationService>();
            services.Configure<DigitalEnvelopeCertificateBootstrapOptions>(options =>
            {
                options.Enabled = true;
                options.DirectoryPath = directory.FullName;
                options.PfxPassword = password;
                options.ClearingHouseId = 1;
                options.Environment = CertificateEnvironment.Test;
            });
            services.AddSingleton<CertificateBootstrapHostedService>();

            await using var provider = services.BuildServiceProvider();
            var bootstrap = provider.GetRequiredService<CertificateBootstrapHostedService>();
            await bootstrap.StartAsync(CancellationToken.None);
            await bootstrap.StartAsync(CancellationToken.None);

            await using var assertScope = provider.CreateAsyncScope();
            var assertContext = assertScope.ServiceProvider.GetRequiredService<AchDbContext>();
            (await assertContext.ClearingHouses.CountAsync(x => x.Code == "ACHCOL")).Should().Be(1);
            var versions = await assertContext.DigitalCertificateVersions.AsNoTracking().ToListAsync();
            versions.Should().HaveCount(3);
            versions.Should().OnlyContain(x => x.Status == CertificateStatus.Active);
            versions.Select(x => new { x.Thumbprint, x.Purpose, x.HolderType })
                .Should().OnlyHaveUniqueItems();
            versions.Should().ContainSingle(x => x.Purpose == CertificatePurpose.OutboundEncryption && !x.HasPrivateKey);
            versions.Should().ContainSingle(x => x.Purpose == CertificatePurpose.OutboundSigning && x.HasPrivateKey);
            versions.Should().ContainSingle(x => x.Purpose == CertificatePurpose.InboundDecryption && x.HasPrivateKey);
        }
        finally
        {
            directory.Delete(recursive: true);
        }
    }

    private static X509Certificate2 CreateCertificate()
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest("CN=Certificate Bootstrap Unit Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(
            X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.KeyEncipherment,
            critical: true));
        using var generated = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(30));
        var pfx = generated.Export(X509ContentType.Pfx, "clone");
        try
        {
            return X509CertificateLoader.LoadPkcs12(pfx, "clone", X509KeyStorageFlags.Exportable | X509KeyStorageFlags.EphemeralKeySet);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pfx);
        }
    }
}
