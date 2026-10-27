using System.Collections.Concurrent;
using System.Security.Cryptography;
using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Services.EncryptionService.Interfaces;
using Cfa.ACHInterbank.External.Connections;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Tests;

public sealed class AchColombiaSftpMftAdapterTests
{
    [Fact]
    public async Task Resolver_ShouldSelectManagedFolderAndSftpEndpoint_AndFailClosedForUnknownPair()
    {
        var provider = new MutableProvider(ManagedFolderConfiguration());
        var folder = new AchColombiaManagedMftFolderAdapter(Options.Create(new AchColombiaManagedMftOptions()));
        var sftp = new AchColombiaSftpMftAdapter(provider, new FakeEncryption(), new FakeSessionFactory());
        var resolver = new AchColombiaManagedMftAdapterResolver(provider, folder, sftp);

        Assert.Same(folder, await resolver.ResolveAsync());

        provider.Configuration = SftpConfiguration();
        Assert.Same(sftp, await resolver.ResolveAsync());

        provider.Configuration = SftpConfiguration() with { Protocol = "ManagedFile" };
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => resolver.ResolveAsync());
        Assert.Equal("ACHCOL_MFT_TRANSPORT_UNSUPPORTED", error.Message);
    }

    [Fact]
    public async Task SftpEndpoint_ShouldRequireEndpointPortPrincipalCredentialAndFingerprint()
    {
        var provider = new MutableProvider(SftpConfiguration() with { Endpoint = null });
        var adapter = Create(provider);
        await Assert.ThrowsAsync<InvalidOperationException>(() => adapter.PickupInboundAsync());

        provider.Configuration = SftpConfiguration() with { Port = null };
        await Assert.ThrowsAsync<InvalidOperationException>(() => adapter.PickupInboundAsync());

        provider.Configuration = SftpConfiguration() with { Principal = null };
        await Assert.ThrowsAsync<InvalidOperationException>(() => adapter.PickupInboundAsync());

        provider.Configuration = SftpConfiguration() with { ProtectedCredential = null };
        await Assert.ThrowsAsync<InvalidOperationException>(() => adapter.PickupInboundAsync());

        provider.Configuration = SftpConfiguration() with { SftpHostKeyFingerprint = null };
        await Assert.ThrowsAsync<ArgumentException>(() => adapter.PickupInboundAsync());
    }

    [Fact]
    public void HostKeyFingerprint_ShouldRequireSha256Fingerprint()
    {
        var valid = "SHA256:" + Convert.ToBase64String(SHA256.HashData(Array.Empty<byte>())).TrimEnd('=');
        Assert.Equal(valid, AchColombiaSftpHostKeyFingerprint.NormalizeRequired(valid));
        Assert.True(AchColombiaSftpHostKeyFingerprint.Matches(valid, Array.Empty<byte>()));
        var different = "SHA256:" + Convert.ToBase64String(SHA256.HashData(new byte[] { 1 })).TrimEnd('=');
        Assert.False(AchColombiaSftpHostKeyFingerprint.Matches(different, Array.Empty<byte>()));
        Assert.Throws<ArgumentException>(() => AchColombiaSftpHostKeyFingerprint.NormalizeRequired("SHA256:not-base64"));
        Assert.Throws<ArgumentException>(() => AchColombiaSftpHostKeyFingerprint.NormalizeRequired("MD5:00:11"));
    }

    [Fact]
    public async Task InvalidHostKey_ShouldFailClosedWithoutExposingSecret()
    {
        var factory = new FakeSessionFactory { ConnectException = new AchColombiaSftpHostKeyException() };
        var provider = new MutableProvider(SftpConfiguration());
        var adapter = Create(provider, factory);
        var content = new byte[] { 1, 2, 3 };

        var result = await adapter.HandoffOutboundAsync("file.env", content, Hash(content));

        Assert.False(result.Succeeded);
        Assert.Equal("ACHCOL_MFT_SFTP_HOST_KEY_REJECTED", result.Code);
        Assert.DoesNotContain("top-secret", result.Message, StringComparison.Ordinal);
        Assert.Equal("top-secret", factory.LastPassword);
    }

    [Fact]
    public async Task Outbound_ShouldUploadTemporaryThenRenameFinal_WithoutLeavingPartialFinal()
    {
        var factory = new FakeSessionFactory();
        var adapter = Create(new MutableProvider(SftpConfiguration()), factory);
        var content = new byte[] { 4, 5, 6 };

        var result = await adapter.HandoffOutboundAsync("batch.env", content, Hash(content));

        Assert.True(result.Succeeded);
        Assert.True(factory.Store.ContainsKey("/out/batch.env"));
        Assert.DoesNotContain(factory.Store.Keys, x => x.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(new[] { "UPLOAD", "RENAME" }, factory.Operations.Where(x => x is "UPLOAD" or "RENAME").Take(2).ToArray());
    }

    [Fact]
    public async Task OutboundUploadFailure_ShouldNeverExposePartialAsFinal()
    {
        var factory = new FakeSessionFactory { ThrowAfterPartialUpload = true };
        var adapter = Create(new MutableProvider(SftpConfiguration()), factory);
        var content = new byte[] { 7, 8, 9 };

        var result = await adapter.HandoffOutboundAsync("batch.env", content, Hash(content));

        Assert.False(result.Succeeded);
        Assert.True(result.Retryable);
        Assert.False(factory.Store.ContainsKey("/out/batch.env"));
        Assert.DoesNotContain(factory.Store.Keys, x => x.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task ExistingFinal_ShouldReconcileSameHash_AndRejectDifferentHash()
    {
        var factory = new FakeSessionFactory();
        var same = new byte[] { 1, 1, 2, 3 };
        factory.Store["/out/file.env"] = same;
        var adapter = Create(new MutableProvider(SftpConfiguration()), factory);

        var existing = await adapter.HandoffOutboundAsync("file.env", same, Hash(same));
        var collision = await adapter.HandoffOutboundAsync("file.env", new byte[] { 9 }, Hash(new byte[] { 9 }));

        Assert.True(existing.Succeeded);
        Assert.Equal("HANDOFF_ALREADY_COMMITTED", existing.Code);
        Assert.False(collision.Succeeded);
        Assert.Equal("ACHCOL_MFT_NAME_COLLISION", collision.Code);
    }

    [Fact]
    public async Task RenameTransportFailure_ShouldReturnUncertain()
    {
        var factory = new FakeSessionFactory { ThrowOnRename = true };
        var adapter = Create(new MutableProvider(SftpConfiguration()), factory);
        var content = new byte[] { 2, 4, 6 };

        var result = await adapter.HandoffOutboundAsync("file.env", content, Hash(content));

        Assert.False(result.Succeeded);
        Assert.True(result.Uncertain);
        Assert.True(result.Retryable);
        Assert.Equal("ACHCOL_MFT_SFTP_RENAME_UNCERTAIN", result.Code);
    }

    [Fact]
    public async Task Inbound_ShouldIgnoreTemporaries_ClaimExclusively_AndArchive()
    {
        var factory = new FakeSessionFactory();
        factory.Store["/in/final.OUT.env"] = new byte[] { 1, 3, 5 };
        factory.Store["/in/.partial.tmp"] = new byte[] { 8 };
        var provider = new MutableProvider(SftpConfiguration());
        var adapter = Create(provider, factory);

        var artifacts = await adapter.PickupInboundAsync();
        var artifact = Assert.Single(artifacts);
        Assert.Equal("final.OUT.env", artifact.FileName);
        Assert.NotNull(artifact.BoundaryLastWriteTimeUtc);
        Assert.DoesNotContain(factory.Store.Keys, x => x == "/in/final.OUT.env");
        Assert.Contains(factory.Store.Keys, x => x.EndsWith(".claim", StringComparison.OrdinalIgnoreCase));

        var archive = await adapter.ArchiveInboundAsync(artifact);
        Assert.Equal("sftp-archive:final.OUT.env", archive);
        Assert.True(factory.Store.ContainsKey("/archive/final.OUT.env"));
        Assert.True(factory.Store.ContainsKey("/in/.partial.tmp"));
    }

    [Fact]
    public async Task TwoConcurrentInboundClaims_ShouldProduceSingleArtifact()
    {
        var factory = new FakeSessionFactory();
        factory.Store["/in/one.OUT.env"] = new byte[] { 4, 4, 4 };
        var provider = new MutableProvider(SftpConfiguration());
        var first = Create(provider, factory);
        var second = Create(provider, factory);

        var results = await Task.WhenAll(
            Task.Run(() => first.PickupInboundAsync()),
            Task.Run(() => second.PickupInboundAsync()));

        Assert.Equal(1, results.Sum(x => x.Count));
    }

    [Fact]
    public async Task Inbound_ShouldAcceptPlainOutAndEnvelopes_AndIgnoreUnknownFinalNames()
    {
        var factory = new FakeSessionFactory();
        factory.Store["/in/a.OUT.env"] = new byte[] { 1 };
        factory.Store["/in/b.RET.env"] = new byte[] { 2 };
        factory.Store["/in/c.OUT"] = new byte[] { 7 };
        factory.Store["/in/case.out"] = new byte[] { 8 };
        factory.Store["/in/unsupported.RET"] = new byte[] { 9 };
        factory.Store["/in/c.env"] = new byte[] { 3 };
        factory.Store["/in/d.txt"] = new byte[] { 4 };
        var adapter = Create(new MutableProvider(SftpConfiguration()), factory);

        var artifacts = await adapter.PickupInboundAsync();

        Assert.Equal(4, artifacts.Count);
        Assert.Contains(artifacts, x => x.FileName == "a.OUT.env");
        Assert.Contains(artifacts, x => x.FileName == "b.RET.env");
        Assert.Contains(artifacts, x => x.FileName == "c.OUT");
        Assert.Contains(artifacts, x => x.FileName == "case.out");
        Assert.True(factory.Store.ContainsKey("/in/unsupported.RET"));
        Assert.True(factory.Store.ContainsKey("/in/c.env"));
        Assert.True(factory.Store.ContainsKey("/in/d.txt"));
    }

    [Fact]
    public async Task Inbound_ShouldRecoverOrphanedPlainOutClaim()
    {
        var factory = new FakeSessionFactory();
        factory.Store["/in/.0001283.008.20260731.8.OUT.0123456789abcdef0123456789abcdef.claim"] = new byte[] { 1, 6 };
        var adapter = Create(new MutableProvider(SftpConfiguration()), factory);

        var artifact = Assert.Single(await adapter.PickupInboundAsync());

        Assert.Equal("0001283.008.20260731.8.OUT", artifact.FileName);
        Assert.EndsWith(".claim", artifact.ClaimReference, StringComparison.OrdinalIgnoreCase);
        Assert.Equal("sftp-archive:0001283.008.20260731.8.OUT", await adapter.ArchiveInboundAsync(artifact));
    }

    [Fact]
    public async Task Inbound_ShouldRecoverOrphanedClaimAfterProcessRestart()
    {
        var factory = new FakeSessionFactory();
        factory.Store["/in/.late.OUT.env.0123456789abcdef0123456789abcdef.claim"] = new byte[] { 5, 6, 7 };
        var adapter = Create(new MutableProvider(SftpConfiguration()), factory);

        var artifact = Assert.Single(await adapter.PickupInboundAsync());

        Assert.Equal("late.OUT.env", artifact.FileName);
        Assert.EndsWith(".claim", artifact.ClaimReference, StringComparison.OrdinalIgnoreCase);
        var archive = await adapter.ArchiveInboundAsync(artifact);
        Assert.Equal("sftp-archive:late.OUT.env", archive);
        Assert.True(factory.Store.ContainsKey("/archive/late.OUT.env"));
    }

    [Fact]
    public async Task EffectiveCredential_ShouldBeDecryptedOnlyForBackendSession_AndNeverReturnedInResult()
    {
        var factory = new FakeSessionFactory();
        var encryption = new FakeEncryption();
        var adapter = new AchColombiaSftpMftAdapter(new MutableProvider(SftpConfiguration()), encryption, factory);
        var content = new byte[] { 3, 2, 1 };

        var result = await adapter.HandoffOutboundAsync("safe.env", content, Hash(content));

        Assert.Equal("protected-password", encryption.LastCiphertext);
        Assert.Equal("top-secret", factory.LastPassword);
        Assert.DoesNotContain("top-secret", result.Message + result.StorageReference, StringComparison.Ordinal);
    }

    private static AchColombiaSftpMftAdapter Create(MutableProvider provider, FakeSessionFactory? factory = null)
        => new(provider, new FakeEncryption(), factory ?? new FakeSessionFactory());

    private static AchManagedMftEffectiveConfiguration ManagedFolderConfiguration()
        => new(true, "out", "in", "processing", "archive", 1024)
        {
            Provider = "ManagedFolder",
            Protocol = "ManagedFile"
        };

    private static AchManagedMftEffectiveConfiguration SftpConfiguration()
        => new(true, "/out", "/in", "unused", "/archive", 1024)
        {
            Provider = "SftpEndpoint",
            Protocol = "Sftp",
            Endpoint = "sftp.cfa.invalid",
            Port = 22,
            Principal = "svc-ach",
            CredentialType = "Password",
            ProtectedCredential = "protected-password",
            SftpHostKeyFingerprint = "SHA256:" + Convert.ToBase64String(new byte[32]).TrimEnd('='),
            SftpTimeoutSeconds = 30
        };

    private static string Hash(byte[] content) => Convert.ToHexString(SHA256.HashData(content));

    private sealed class MutableProvider(AchManagedMftEffectiveConfiguration configuration) : IAchColombiaManagedMftConfigurationProvider
    {
        public AchManagedMftEffectiveConfiguration Configuration { get; set; } = configuration;
        public Task<AchManagedMftEffectiveConfiguration> GetEffectiveAsync(CancellationToken ct = default) => Task.FromResult(Configuration);
    }

    private sealed class FakeEncryption : IEncryptionService
    {
        public string? LastCiphertext { get; private set; }
        public string Encrypt(string plaintext) => "protected:" + plaintext;
        public string Decrypt(string ciphertext)
        {
            LastCiphertext = ciphertext;
            return "top-secret";
        }
    }

    private sealed class FakeSessionFactory : IAchColombiaSftpSessionFactory
    {
        public ConcurrentDictionary<string, byte[]> Store { get; } = new(StringComparer.Ordinal);
        public ConcurrentDictionary<string, DateTime> LastWriteTimes { get; } = new(StringComparer.Ordinal);
        public ConcurrentQueue<string> OperationQueue { get; } = new();
        public IReadOnlyCollection<string> Operations => OperationQueue.ToArray();
        public Exception? ConnectException { get; set; }
        public bool ThrowAfterPartialUpload { get; set; }
        public bool ThrowOnRename { get; set; }
        public string? LastPassword { get; private set; }

        public IAchColombiaSftpSession Connect(AchManagedMftEffectiveConfiguration configuration, string password)
        {
            LastPassword = password;
            if (ConnectException is not null) throw ConnectException;
            return new FakeSession(this);
        }

        private sealed class FakeSession(FakeSessionFactory owner) : IAchColombiaSftpSession
        {
            public bool Exists(string path) => owner.Store.ContainsKey(path);

            public IReadOnlyList<AchColombiaSftpEntry> List(string path)
            {
                var prefix = path.TrimEnd('/') + "/";
                return owner.Store
                    .Where(x => x.Key.StartsWith(prefix, StringComparison.Ordinal) && !x.Key[prefix.Length..].Contains('/'))
                    .Select(x => new AchColombiaSftpEntry(
                        x.Key[prefix.Length..],
                        x.Value.LongLength,
                        true,
                        owner.LastWriteTimes.TryGetValue(x.Key, out var lastWrite)
                            ? lastWrite
                            : DateTime.UtcNow.AddMinutes(-5)))
                    .ToArray();
            }

            public Stream OpenRead(string path)
            {
                if (!owner.Store.TryGetValue(path, out var bytes)) throw new IOException("not found");
                return new MemoryStream(bytes, writable: false);
            }

            public void Upload(Stream source, string path, CancellationToken ct)
            {
                owner.OperationQueue.Enqueue("UPLOAD");
                using var buffer = new MemoryStream();
                source.CopyTo(buffer);
                owner.Store[path] = buffer.ToArray();
                owner.LastWriteTimes[path] = DateTime.UtcNow;
                if (owner.ThrowAfterPartialUpload) throw new IOException("simulated upload interruption");
            }

            public bool TryClaim(string source, string claim)
            {
                owner.OperationQueue.Enqueue("CLAIM");
                lock (owner.Store)
                {
                    if (!owner.Store.TryRemove(source, out var bytes)) return false;
                    owner.LastWriteTimes.TryRemove(source, out _);
                    if (owner.Store.TryAdd(claim, bytes))
                    {
                        owner.LastWriteTimes[claim] = DateTime.UtcNow;
                        return true;
                    }
                    owner.Store[source] = bytes;
                    owner.LastWriteTimes[source] = DateTime.UtcNow;
                    throw new IOException("claim target exists");
                }
            }

            public void Rename(string source, string target)
            {
                owner.OperationQueue.Enqueue("RENAME");
                lock (owner.Store)
                {
                    if (owner.ThrowOnRename) throw new IOException("simulated rename uncertainty");
                    if (!owner.Store.TryRemove(source, out var bytes)) throw new IOException("source missing");
                    owner.LastWriteTimes.TryRemove(source, out _);
                    if (!owner.Store.TryAdd(target, bytes))
                    {
                        owner.Store[source] = bytes;
                        owner.LastWriteTimes[source] = DateTime.UtcNow;
                        throw new IOException("target exists");
                    }
                    owner.LastWriteTimes[target] = DateTime.UtcNow;
                }
            }

            public void Delete(string path)
            {
                owner.OperationQueue.Enqueue("DELETE");
                owner.Store.TryRemove(path, out _);
                owner.LastWriteTimes.TryRemove(path, out _);
            }

            public void Dispose() { }
        }
    }
}
