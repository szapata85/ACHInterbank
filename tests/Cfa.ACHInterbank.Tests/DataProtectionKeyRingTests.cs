using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Cfa.ACHInterbank.Domain.Models.ACHSobreDigital;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.CertificateManagement;
using Cfa.ACHInterbank.Persistence.DataBase;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class DataProtectionKeyRingTests
{
    [Fact]
    public void KeyRing_ShouldPersistAndBeReadableBySecondApplicationInstance()
    {
        var databasePath = CreateDatabasePath();
        try
        {
            using var first = CreateProvider(databasePath);
            EnsureDatabaseCreated(first);
            var protectedValue = first.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("test.v1")
                .Protect("shared-key-ring");

            using (var scope = first.CreateScope())
            {
                scope.ServiceProvider.GetRequiredService<AchDbContext>()
                    .DataProtectionKeys.Should().ContainSingle();
            }

            using var second = CreateProvider(databasePath);
            var recovered = second.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("test.v1")
                .Unprotect(protectedValue);

            recovered.Should().Be("shared-key-ring");
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    [Fact]
    public void KeyRing_FromDifferentDatabase_ShouldNotDecryptBlob()
    {
        var sourcePath = CreateDatabasePath();
        var otherPath = CreateDatabasePath();
        try
        {
            using var source = CreateProvider(sourcePath);
            EnsureDatabaseCreated(source);
            var protectedValue = source.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("test.v1")
                .Protect("database-bound");

            using var other = CreateProvider(otherPath);
            EnsureDatabaseCreated(other);
            var act = () => other.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("test.v1")
                .Unprotect(protectedValue);

            act.Should().Throw<CryptographicException>();
        }
        finally
        {
            DeleteDatabase(sourcePath);
            DeleteDatabase(otherPath);
        }
    }

    [Fact]
    public void Configuration_ShouldUseFixedApplicationNameAndEfRepository()
    {
        var databasePath = CreateDatabasePath();
        try
        {
            using var provider = CreateProvider(databasePath);
            var dataProtectionOptions = provider.GetRequiredService<IOptions<DataProtectionOptions>>().Value;
            var keyManagementOptions = provider.GetRequiredService<IOptions<KeyManagementOptions>>().Value;

            DataProtectionKeyRingConfiguration.ApplicationName.Should().Be("Cfa.ACHInterbank");
            dataProtectionOptions.ApplicationDiscriminator.Should().Be("Cfa.ACHInterbank");
            keyManagementOptions.XmlRepository.Should().NotBeNull();
            keyManagementOptions.XmlRepository!.GetType().FullName.Should().Contain("EntityFrameworkCoreXmlRepository");
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    [Fact]
    public void SqlServerAndPostgresModels_ShouldContainOfficialDataProtectionEntity()
    {
        var sqlOptions = new DbContextOptionsBuilder<AchDbContext>()
            .UseSqlServer("Server=localhost;Database=model;User Id=model;Password=model;TrustServerCertificate=True")
            .Options;
        var postgresOptions = new DbContextOptionsBuilder<AchDbContext>()
            .UseNpgsql("Host=localhost;Database=model;Username=model;Password=model")
            .Options;

        using var sqlContext = new AchDbContext(sqlOptions);
        using var postgresContext = new AchDbContext(postgresOptions);

        sqlContext.Model.FindEntityType(typeof(DataProtectionKey)).Should().NotBeNull();
        postgresContext.Model.FindEntityType(typeof(DataProtectionKey)).Should().NotBeNull();
        sqlContext.Model.FindEntityType(typeof(DataProtectionKey))!.GetTableName().Should().Be("DataProtectionKeys");
        postgresContext.Model.FindEntityType(typeof(DataProtectionKey))!.GetTableName().Should().Be("DataProtectionKeys");
    }

    [Fact]
    public void ProtectedPfx_ShouldBeRecoverableAndUsableForSigningAcrossInstances()
    {
        var databasePath = CreateDatabasePath();
        const string password = "temporary-test-password";
        try
        {
            using var rsa = RSA.Create(2048);
            var request = new CertificateRequest("CN=Data Protection Test", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            using var sourceCertificate = request.CreateSelfSigned(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1));
            var pfx = sourceCertificate.Export(X509ContentType.Pkcs12, password);

            using var first = CreateProvider(databasePath);
            EnsureDatabaseCreated(first);
            var protectedMaterial = new DataProtectionCertificatePrivateMaterialProtector(
                first.GetRequiredService<IDataProtectionProvider>()).Protect(pfx, password);

            protectedMaterial.AsSpan().IndexOf(pfx).Should().Be(-1);
            protectedMaterial.AsSpan().IndexOf(System.Text.Encoding.UTF8.GetBytes(password)).Should().Be(-1);

            using var second = CreateProvider(databasePath);
            var protector = new DataProtectionCertificatePrivateMaterialProtector(
                second.GetRequiredService<IDataProtectionProvider>());
            using var recoveredCertificate = protector.Unprotect(protectedMaterial);
            var payload = RandomNumberGenerator.GetBytes(64);
            using var privateKey = recoveredCertificate.GetRSAPrivateKey();
            using var publicKey = recoveredCertificate.GetRSAPublicKey();
            var signature = privateKey!.SignData(payload, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

            publicKey!.VerifyData(payload, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1).Should().BeTrue();
            typeof(DigitalCertificateVersion).GetProperty("Password").Should().BeNull();

            CryptographicOperations.ZeroMemory(pfx);
            CryptographicOperations.ZeroMemory(protectedMaterial);
            CryptographicOperations.ZeroMemory(payload);
            CryptographicOperations.ZeroMemory(signature);
        }
        finally
        {
            DeleteDatabase(databasePath);
        }
    }

    private static ServiceProvider CreateProvider(string databasePath)
    {
        var services = new ServiceCollection();
        services.AddDbContext<AchDbContext>(options => options.UseSqlite($"Data Source={databasePath}"));
        services.AddDataProtection()
            .SetApplicationName(DataProtectionKeyRingConfiguration.ApplicationName)
            .PersistKeysToDbContext<AchDbContext>();
        return services.BuildServiceProvider();
    }

    private static void EnsureDatabaseCreated(IServiceProvider provider)
    {
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<AchDbContext>().Database.EnsureCreated();
    }

    private static string CreateDatabasePath()
        => Path.Combine(Path.GetTempPath(), $"ach-dp-{Guid.NewGuid():N}.db");

    private static void DeleteDatabase(string databasePath)
    {
        SqliteConnection.ClearAllPools();
        File.Delete(databasePath);
    }
}
