using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Services.EncryptionService.Interfaces;
using Cfa.ACHInterbank.External.Connections;

namespace Cfa.ACHInterbank.Tests;

public sealed class AchColombiaManagedMftConnectionTesterTests
{
    [Fact]
    public async Task ManagedFolder_ShouldConfirmAccessibleConfiguredFolders()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ach-mft-test-{Guid.NewGuid():N}");
        var outbound = Path.Combine(root, "out");
        var inbound = Path.Combine(root, "in");
        var archive = Path.Combine(root, "archive");
        Directory.CreateDirectory(outbound);
        Directory.CreateDirectory(inbound);
        Directory.CreateDirectory(archive);
        try
        {
            var tester = new AchColombiaManagedMftConnectionTester(
                new Provider(new AchManagedMftEffectiveConfiguration(false, outbound, inbound, Path.Combine(root, "processing"), archive, 1024)
                {
                    Provider = "ManagedFolder",
                    Protocol = "ManagedFile"
                }),
                new FakeEncryption(),
                new FakeSessionFactory());

            var result = await tester.TestAsync();

            Assert.True(result.Succeeded);
            Assert.Equal("ACHCOL_MFT_CONNECTION_OK", result.Code);
            Assert.Equal("Conexión exitosa.", result.Message);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task ManagedFolder_ShouldFailWithFriendlyMessageWhenPathIsMissing()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ach-mft-test-{Guid.NewGuid():N}");
        var outbound = Path.Combine(root, "out");
        var inbound = Path.Combine(root, "missing");
        var archive = Path.Combine(root, "archive");
        Directory.CreateDirectory(outbound);
        Directory.CreateDirectory(archive);
        try
        {
            var tester = new AchColombiaManagedMftConnectionTester(
                new Provider(new AchManagedMftEffectiveConfiguration(false, outbound, inbound, Path.Combine(root, "processing"), archive, 1024)
                {
                    Provider = "ManagedFolder",
                    Protocol = "ManagedFile"
                }),
                new FakeEncryption(),
                new FakeSessionFactory());

            var result = await tester.TestAsync();

            Assert.False(result.Succeeded);
            Assert.Equal("No hay acceso a la carpeta configurada.", result.Message);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Sftp_ShouldUseProtectedCredentialOnlyInBackendAndCheckAllConfiguredFolders()
    {
        var factory = new FakeSessionFactory();
        var tester = new AchColombiaManagedMftConnectionTester(
            new Provider(SftpConfiguration()),
            new FakeEncryption(),
            factory);

        var result = await tester.TestAsync();

        Assert.True(result.Succeeded);
        Assert.Equal("top-secret", factory.LastPassword);
        Assert.Equal(new[] { "/out", "/in", "/archive" }, factory.ListedPaths);
        Assert.DoesNotContain("top-secret", result.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sftp_ShouldRejectInvalidHostKeyWithFriendlyMessage()
    {
        var factory = new FakeSessionFactory { ConnectException = new AchColombiaSftpHostKeyException() };
        var tester = new AchColombiaManagedMftConnectionTester(
            new Provider(SftpConfiguration()),
            new FakeEncryption(),
            factory);

        var result = await tester.TestAsync();

        Assert.False(result.Succeeded);
        Assert.Equal("ACHCOL_MFT_SFTP_HOST_KEY_REJECTED", result.Code);
        Assert.Equal("La huella del servidor no es válida.", result.Message);
    }

    private static AchManagedMftEffectiveConfiguration SftpConfiguration()
        => new(false, "/out", "/in", "unused", "/archive", 1024)
        {
            Provider = "SftpEndpoint",
            Protocol = "Sftp",
            Endpoint = "sftp.cfa.invalid",
            Port = 22,
            Principal = "svc-ach",
            CredentialType = "Password",
            ProtectedCredential = "protected-password",
            SftpHostKeyFingerprint = "SHA256:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
            SftpTimeoutSeconds = 30
        };

    private sealed class Provider(AchManagedMftEffectiveConfiguration value) : IAchColombiaManagedMftConfigurationProvider
    {
        public Task<AchManagedMftEffectiveConfiguration> GetEffectiveAsync(CancellationToken ct = default) => Task.FromResult(value);
    }

    private sealed class FakeEncryption : IEncryptionService
    {
        public string Encrypt(string plaintext) => "protected:" + plaintext;
        public string Decrypt(string ciphertext) => "top-secret";
    }

    private sealed class FakeSessionFactory : IAchColombiaSftpSessionFactory
    {
        public Exception? ConnectException { get; set; }
        public string? LastPassword { get; private set; }
        public List<string> ListedPaths { get; } = [];

        public IAchColombiaSftpSession Connect(AchManagedMftEffectiveConfiguration configuration, string password)
        {
            LastPassword = password;
            if (ConnectException is not null) throw ConnectException;
            return new FakeSession(ListedPaths);
        }

        private sealed class FakeSession(List<string> listedPaths) : IAchColombiaSftpSession
        {
            public bool Exists(string path) => true;
            public IReadOnlyList<AchColombiaSftpEntry> List(string path) { listedPaths.Add(path); return []; }
            public Stream OpenRead(string path) => new MemoryStream();
            public void Upload(Stream source, string path, CancellationToken ct) { }
            public bool TryClaim(string source, string claim) => true;
            public void Rename(string source, string target) { }
            public void Delete(string path) { }
            public void Dispose() { }
        }
    }
}
