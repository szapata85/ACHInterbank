using System.Net.Sockets;
using System.Security.Cryptography;
using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Services.EncryptionService.Interfaces;
using Renci.SshNet;
using Renci.SshNet.Common;

namespace Cfa.ACHInterbank.External.Connections;

public sealed class AchColombiaSftpMftAdapter : IAchColombiaManagedMftAdapter
{
    private static readonly TimeSpan ClaimRecoveryGracePeriod = TimeSpan.FromMinutes(1);
    private readonly IAchColombiaManagedMftConfigurationProvider _configurationProvider;
    private readonly IEncryptionService _encryption;
    private readonly IAchColombiaSftpSessionFactory _sessionFactory;

    internal AchColombiaSftpMftAdapter(
        IAchColombiaManagedMftConfigurationProvider configurationProvider,
        IEncryptionService encryption,
        IAchColombiaSftpSessionFactory sessionFactory)
    {
        _configurationProvider = configurationProvider;
        _encryption = encryption;
        _sessionFactory = sessionFactory;
    }

    public bool Enabled => true;

    public async Task<AchManagedMftResult> HandoffOutboundAsync(
        string fileName, byte[] content, string contentSha256, CancellationToken ct = default)
    {
        var configuration = await _configurationProvider.GetEffectiveAsync(ct);
        if (!configuration.Enabled) return Failure("ACHCOL_MFT_DISABLED", "El intercambio administrado no está habilitado.", false);

        var validation = ValidateOutbound(fileName, content, contentSha256, configuration.MaximumFileBytes);
        if (validation is not null) return validation;

        try
        {
            using var session = Connect(configuration);
            var target = RemoteChild(configuration.OutboundPath, fileName);
            if (session.Exists(target)) return Existing(session, target, fileName, contentSha256, ct);

            var temporary = RemoteChild(configuration.OutboundPath, $".{fileName}.{Guid.NewGuid():N}.tmp");
            try
            {
                using var source = new MemoryStream(content, writable: false);
                session.Upload(source, temporary, ct);
                ct.ThrowIfCancellationRequested();
                try
                {
                    session.Rename(temporary, target);
                }
                catch (SftpPermissionDeniedException)
                {
                    throw;
                }
                catch (Exception ex) when (IsTransportException(ex))
                {
                    var reconciliation = TryReconcileAfterRename(session, target, fileName, contentSha256, ct);
                    if (reconciliation is not null) return reconciliation;
                    return Failure("ACHCOL_MFT_SFTP_RENAME_UNCERTAIN", "No fue posible confirmar la publicación final en la frontera SFTP CFA.", true, true);
                }
                return new(true, false, false, "HANDOFF_COMMITTED", "Archivo entregado a la frontera SFTP administrada por CFA.", $"sftp-out:{fileName}");
            }
            finally
            {
                TryDelete(session, temporary);
            }
        }
        catch (AchColombiaSftpHostKeyException)
        {
            return Failure("ACHCOL_MFT_SFTP_HOST_KEY_REJECTED", "La identidad criptográfica del servidor SFTP CFA no coincide con la configuración autorizada.", false);
        }
        catch (SshAuthenticationException)
        {
            return Failure("ACHCOL_MFT_SFTP_AUTHENTICATION_FAILED", "No fue posible autenticar la sesión SFTP CFA.", false);
        }
        catch (SftpPermissionDeniedException)
        {
            return Failure("ACHCOL_MFT_SFTP_ACCESS_DENIED", "La sesión SFTP CFA no tiene permisos suficientes para la operación.", false);
        }
        catch (SftpPathNotFoundException)
        {
            return Failure("ACHCOL_MFT_SFTP_PATH_NOT_FOUND", "La ruta configurada en la frontera SFTP CFA no existe.", false);
        }
        catch (Exception ex) when (IsTransportException(ex))
        {
            return Failure("ACHCOL_MFT_SFTP_TRANSPORT_RETRYABLE", "La operación SFTP CFA no pudo completarse por una falla transitoria de transporte.", true);
        }
    }

    public async Task<IReadOnlyList<AchManagedMftArtifact>> PickupInboundAsync(CancellationToken ct = default)
    {
        var configuration = await _configurationProvider.GetEffectiveAsync(ct);
        if (!configuration.Enabled) return [];
        var artifacts = new List<AchManagedMftArtifact>();

        using var session = Connect(configuration);
        var entries = session.List(configuration.InboundPath)
            .Where(x => x.IsRegularFile)
            .ToArray();

        foreach (var entry in entries.Where(x => IsEligibleFinalName(x.Name)))
        {
            ct.ThrowIfCancellationRequested();
            if (entry.Length <= 0 || entry.Length > configuration.MaximumFileBytes) continue;

            var source = RemoteChild(configuration.InboundPath, entry.Name);
            var claim = RemoteChild(configuration.InboundPath, $".{entry.Name}.{Guid.NewGuid():N}.claim");
            if (!session.TryClaim(source, claim))
            {
                continue;
            }

            try
            {
                var bytes = ReadAll(session, claim, configuration.MaximumFileBytes, ct);
                artifacts.Add(new(entry.Name, bytes, Convert.ToHexString(SHA256.HashData(bytes)), claim)
                {
                    BoundaryLastWriteTimeUtc = DateTime.SpecifyKind(entry.LastWriteTimeUtc, DateTimeKind.Utc)
                });
            }
            catch
            {
                TryRename(session, claim, source);
                throw;
            }
        }

        // A claim may survive a process/node crash after the atomic rename and before
        // persistence. Claims remain inside the CFA boundary and are safe to surface
        // again: the persistence layer provides the functional idempotency gate.
        foreach (var entry in entries)
        {
            ct.ThrowIfCancellationRequested();
            if (!TryResolveClaimedOriginalName(entry.Name, out var originalName)) continue;
            if (DateTime.UtcNow - entry.LastWriteTimeUtc < ClaimRecoveryGracePeriod) continue;
            if (entry.Length <= 0 || entry.Length > configuration.MaximumFileBytes) continue;

            var claim = RemoteChild(configuration.InboundPath, entry.Name);
            var bytes = ReadAll(session, claim, configuration.MaximumFileBytes, ct);
            artifacts.Add(new(originalName, bytes, Convert.ToHexString(SHA256.HashData(bytes)), claim)
            {
                BoundaryLastWriteTimeUtc = DateTime.SpecifyKind(entry.LastWriteTimeUtc, DateTimeKind.Utc)
            });
        }

        return artifacts
            .GroupBy(x => x.ClaimReference, StringComparer.Ordinal)
            .Select(x => x.First())
            .ToArray();
    }

    public async Task<string> ArchiveInboundAsync(AchManagedMftArtifact artifact, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        var configuration = await _configurationProvider.GetEffectiveAsync(ct);
        var claim = NormalizeRemotePath(artifact.ClaimReference);
        var inboundPrefix = NormalizeRemoteDirectory(configuration.InboundPath) + "/";
        if (!claim.StartsWith(inboundPrefix, StringComparison.Ordinal) || !RemoteFileName(claim).EndsWith(".claim", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("ACHCOL_MFT_CLAIM_REFERENCE_INVALID");

        using var session = Connect(configuration);
        var target = RemoteChild(configuration.ArchivePath, artifact.FileName);
        if (session.Exists(target))
        {
            if (RemoteHash(session, target, ct).Equals(artifact.ContentSha256, StringComparison.OrdinalIgnoreCase))
            {
                if (session.Exists(claim)) session.Delete(claim);
                return $"sftp-archive:{artifact.FileName}";
            }
            target = RemoteChild(configuration.ArchivePath,
                $"{Path.GetFileNameWithoutExtension(artifact.FileName)}.{artifact.ContentSha256[..Math.Min(12, artifact.ContentSha256.Length)]}{Path.GetExtension(artifact.FileName)}");
            if (session.Exists(target))
            {
                if (!RemoteHash(session, target, ct).Equals(artifact.ContentSha256, StringComparison.OrdinalIgnoreCase))
                    throw new IOException("ACHCOL_MFT_ARCHIVE_COLLISION");
                if (session.Exists(claim)) session.Delete(claim);
                return $"sftp-archive:{RemoteFileName(target)}";
            }
        }

        if (session.Exists(claim)) session.Rename(claim, target);
        return $"sftp-archive:{RemoteFileName(target)}";
    }

    private IAchColombiaSftpSession Connect(AchManagedMftEffectiveConfiguration configuration)
    {
        ValidateSftpConfiguration(configuration);
        var protectedCredential = configuration.ProtectedCredential!;
        var password = _encryption.Decrypt(protectedCredential);
        try
        {
            return _sessionFactory.Connect(configuration, password);
        }
        finally
        {
            password = string.Empty;
        }
    }

    private static void ValidateSftpConfiguration(AchManagedMftEffectiveConfiguration configuration)
    {
        if (!string.Equals(configuration.Provider, "SftpEndpoint", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(configuration.Protocol, "Sftp", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("ACHCOL_MFT_SFTP_CONFIGURATION_MISMATCH");
        if (string.IsNullOrWhiteSpace(configuration.Endpoint) || configuration.Port is not (>= 1 and <= 65535)
            || string.IsNullOrWhiteSpace(configuration.Principal))
            throw new InvalidOperationException("ACHCOL_MFT_SFTP_ENDPOINT_INCOMPLETE");
        if (!string.Equals(configuration.CredentialType, "Password", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(configuration.ProtectedCredential))
            throw new InvalidOperationException("ACHCOL_MFT_SFTP_PASSWORD_CREDENTIAL_REQUIRED");
        AchColombiaSftpHostKeyFingerprint.NormalizeRequired(configuration.SftpHostKeyFingerprint);
        if (configuration.SftpTimeoutSeconds is < 1 or > 300)
            throw new InvalidOperationException("ACHCOL_MFT_SFTP_TIMEOUT_INVALID");
        _ = NormalizeRemoteDirectory(configuration.OutboundPath);
        _ = NormalizeRemoteDirectory(configuration.InboundPath);
        _ = NormalizeRemoteDirectory(configuration.ArchivePath);
    }

    private static AchManagedMftResult? ValidateOutbound(string fileName, byte[] content, string expectedHash, long maximumFileBytes)
    {
        if (content.Length == 0 || content.LongLength > maximumFileBytes)
            return Failure("ACHCOL_MFT_SIZE_INVALID", "El archivo está vacío o excede el tamaño permitido.", false);
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains('/') || fileName.Contains('\\') || fileName is "." or "..")
            return Failure("ACHCOL_MFT_FILENAME_INVALID", "El nombre del archivo no es seguro.", false);
        var hash = Convert.ToHexString(SHA256.HashData(content));
        return string.Equals(hash, expectedHash, StringComparison.OrdinalIgnoreCase)
            ? null
            : Failure("ACHCOL_MFT_HASH_MISMATCH", "La identidad del archivo no coincide con su contenido.", false);
    }

    private static AchManagedMftResult Existing(IAchColombiaSftpSession session, string target, string fileName, string expectedHash, CancellationToken ct)
        => string.Equals(RemoteHash(session, target, ct), expectedHash, StringComparison.OrdinalIgnoreCase)
            ? new(true, false, false, "HANDOFF_ALREADY_COMMITTED", "El mismo archivo ya estaba entregado.", $"sftp-out:{fileName}")
            : Failure("ACHCOL_MFT_NAME_COLLISION", "El nombre ya existe con contenido diferente.", false);

    private static AchManagedMftResult? TryReconcileAfterRename(IAchColombiaSftpSession session, string target, string fileName, string expectedHash, CancellationToken ct)
    {
        try
        {
            if (!session.Exists(target)) return null;
            var actualHash = RemoteHash(session, target, ct);
            return string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase)
                ? new(true, false, false, "HANDOFF_ALREADY_COMMITTED", "La publicación final fue confirmada después de reconciliar el resultado del rename.", $"sftp-out:{fileName}")
                : Failure("ACHCOL_MFT_NAME_COLLISION", "El nombre final existe con contenido diferente.", false);
        }
        catch (Exception ex) when (IsTransportException(ex))
        {
            return null;
        }
    }

    private static byte[] ReadAll(IAchColombiaSftpSession session, string path, long maximumFileBytes, CancellationToken ct)
    {
        using var source = session.OpenRead(path);
        if (source.Length <= 0 || source.Length > maximumFileBytes) throw new IOException("ACHCOL_MFT_SIZE_INVALID");
        using var buffer = new MemoryStream((int)Math.Min(source.Length, int.MaxValue));
        source.CopyTo(buffer);
        ct.ThrowIfCancellationRequested();
        if (buffer.Length > maximumFileBytes) throw new IOException("ACHCOL_MFT_SIZE_INVALID");
        return buffer.ToArray();
    }

    private static string RemoteHash(IAchColombiaSftpSession session, string path, CancellationToken ct)
    {
        using var stream = session.OpenRead(path);
        ct.ThrowIfCancellationRequested();
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private static bool IsEligibleFinalName(string name)
        => !string.IsNullOrWhiteSpace(name)
            && !name.StartsWith('.')
            && (name.EndsWith(".OUT", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".OUT.env", StringComparison.OrdinalIgnoreCase)
                || name.EndsWith(".RET.env", StringComparison.OrdinalIgnoreCase));

    private static bool TryResolveClaimedOriginalName(string name, out string originalName)
    {
        originalName = string.Empty;
        if (string.IsNullOrWhiteSpace(name)
            || !name.StartsWith('.')
            || !name.EndsWith(".claim", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var withoutClaim = name[1..^6];
        var separator = withoutClaim.LastIndexOf('.');
        if (separator <= 0 || separator == withoutClaim.Length - 1)
        {
            return false;
        }

        var candidate = withoutClaim[..separator];
        var token = withoutClaim[(separator + 1)..];
        if (!Guid.TryParseExact(token, "N", out _) || !IsEligibleFinalName(candidate))
        {
            return false;
        }

        originalName = candidate;
        return true;
    }

    private static string RemoteChild(string directory, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName.Contains('/') || fileName.Contains('\\') || fileName is "." or "..")
            throw new InvalidOperationException("ACHCOL_MFT_REMOTE_NAME_INVALID");
        return NormalizeRemoteDirectory(directory) + "/" + fileName;
    }

    private static string NormalizeRemoteDirectory(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) throw new InvalidOperationException("ACHCOL_MFT_PATH_NOT_CONFIGURED");
        var normalized = NormalizeRemotePath(value).TrimEnd('/');
        if (normalized.Length == 0 || normalized == "/") throw new InvalidOperationException("ACHCOL_MFT_ROOT_PATH_NOT_ALLOWED");
        return normalized;
    }

    private static string NormalizeRemotePath(string value)
    {
        var normalized = (value ?? string.Empty).Trim().Replace('\\', '/');
        if (normalized.Contains("//", StringComparison.Ordinal) || normalized.Split('/').Any(x => x == ".."))
            throw new InvalidOperationException("ACHCOL_MFT_PATH_INVALID");
        return normalized.StartsWith('/') ? "/" + string.Join('/', normalized.Split('/', StringSplitOptions.RemoveEmptyEntries))
            : string.Join('/', normalized.Split('/', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string RemoteFileName(string path)
        => NormalizeRemotePath(path).Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault() ?? string.Empty;

    private static bool IsTransportException(Exception ex)
        => ex is SshException or SocketException or TimeoutException or IOException;

    private static void TryDelete(IAchColombiaSftpSession session, string path)
    {
        try { if (session.Exists(path)) session.Delete(path); } catch { }
    }

    private static void TryRename(IAchColombiaSftpSession session, string source, string target)
    {
        try { if (session.Exists(source) && !session.Exists(target)) session.Rename(source, target); } catch { }
    }

    private static AchManagedMftResult Failure(string code, string message, bool retryable, bool uncertain = false)
        => new(false, retryable, uncertain, code, message, null);
}

internal sealed class AchColombiaSftpHostKeyException : Exception { }

internal sealed record AchColombiaSftpEntry(string Name, long Length, bool IsRegularFile, DateTime LastWriteTimeUtc);

internal interface IAchColombiaSftpSession : IDisposable
{
    bool Exists(string path);
    IReadOnlyList<AchColombiaSftpEntry> List(string path);
    Stream OpenRead(string path);
    void Upload(Stream source, string path, CancellationToken ct);
    bool TryClaim(string source, string claim);
    void Rename(string source, string target);
    void Delete(string path);
}

internal interface IAchColombiaSftpSessionFactory
{
    IAchColombiaSftpSession Connect(AchManagedMftEffectiveConfiguration configuration, string password);
}

internal sealed class SshNetAchColombiaSftpSessionFactory : IAchColombiaSftpSessionFactory
{
    public IAchColombiaSftpSession Connect(AchManagedMftEffectiveConfiguration configuration, string password)
    {
        var connectionInfo = new ConnectionInfo(
            configuration.Endpoint!, configuration.Port!.Value, configuration.Principal!,
            new PasswordAuthenticationMethod(configuration.Principal!, password))
        {
            Timeout = TimeSpan.FromSeconds(configuration.SftpTimeoutSeconds)
        };
        var client = new SftpClient(connectionInfo)
        {
            OperationTimeout = TimeSpan.FromSeconds(configuration.SftpTimeoutSeconds)
        };
        var expected = AchColombiaSftpHostKeyFingerprint.NormalizeRequired(configuration.SftpHostKeyFingerprint);
        var seen = false;
        var accepted = false;
        client.HostKeyReceived += (_, e) =>
        {
            seen = true;
            accepted = AchColombiaSftpHostKeyFingerprint.Matches(expected, e.HostKey);
            e.CanTrust = accepted;
        };
        try
        {
            client.Connect();
            if (!seen || !accepted) throw new AchColombiaSftpHostKeyException();
            return new SshNetAchColombiaSftpSession(client);
        }
        catch
        {
            client.Dispose();
            if (seen && !accepted) throw new AchColombiaSftpHostKeyException();
            throw;
        }
    }
}

internal sealed class SshNetAchColombiaSftpSession(SftpClient client) : IAchColombiaSftpSession
{
    public bool Exists(string path) => client.Exists(path);

    public IReadOnlyList<AchColombiaSftpEntry> List(string path)
        => client.ListDirectory(path)
            .Where(x => x.Name is not "." and not "..")
            .Select(x => new AchColombiaSftpEntry(x.Name, x.Attributes.Size, x.IsRegularFile, x.Attributes.LastWriteTimeUtc))
            .ToArray();

    public Stream OpenRead(string path) => client.OpenRead(path);

    public void Upload(Stream source, string path, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        client.UploadFile(source, path, false);
        ct.ThrowIfCancellationRequested();
    }

    public bool TryClaim(string source, string claim)
    {
        try
        {
            client.RenameFile(source, claim);
            return true;
        }
        catch (SftpPathNotFoundException)
        {
            return false;
        }
    }

    public void Rename(string source, string target) => client.RenameFile(source, target);
    public void Delete(string path) => client.DeleteFile(path);
    public void Dispose() => client.Dispose();
}
