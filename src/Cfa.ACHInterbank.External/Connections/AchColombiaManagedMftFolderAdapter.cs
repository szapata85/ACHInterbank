using System.Security.Cryptography;
using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.External.Connections;

[Scoped]
public sealed class AchColombiaManagedMftFolderAdapter(IOptions<AchColombiaManagedMftOptions> options)
    : IAchColombiaManagedMftAdapter
{
    private readonly AchColombiaManagedMftOptions _options = options.Value;
    public bool Enabled => _options.Enabled;

    public async Task<AchManagedMftResult> HandoffOutboundAsync(
        string fileName, byte[] content, string contentSha256, CancellationToken ct = default)
    {
        if (!Enabled) return Failure("ACHCOL_MFT_DISABLED", "El intercambio administrado no está habilitado.", false);
        var validation = Validate(fileName, content, contentSha256);
        if (validation is not null) return validation;
        var root = ResolveRoot(_options.OutboundPath);
        Directory.CreateDirectory(root);
        var target = ResolveChild(root, fileName);
        if (File.Exists(target)) return await ExistingAsync(target, contentSha256, ct);
        var temporary = Path.Combine(root, $".{fileName}.{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllBytesAsync(temporary, content, ct);
            File.Move(temporary, target, false);
            return new(true, false, false, "HANDOFF_COMMITTED", "Archivo entregado a la frontera administrada.", $"mft-out:{fileName}");
        }
        catch (IOException) when (File.Exists(target)) { return await ExistingAsync(target, contentSha256, ct); }
        catch (IOException) { return Failure("ACHCOL_MFT_IO_UNCERTAIN", "No fue posible confirmar la entrega del archivo.", true, true); }
        catch (UnauthorizedAccessException) { return Failure("ACHCOL_MFT_ACCESS_DENIED", "El proceso no tiene acceso a la ubicación administrada.", false); }
        finally { TryDeleteTemporary(temporary); }
    }

    public async Task<IReadOnlyList<AchManagedMftArtifact>> PickupInboundAsync(CancellationToken ct = default)
    {
        if (!Enabled) return [];
        var inbound = ResolveRoot(_options.InboundPath);
        var processing = ResolveRoot(_options.ProcessingPath);
        Directory.CreateDirectory(inbound);
        Directory.CreateDirectory(processing);
        foreach (var source in Directory.EnumerateFiles(inbound).Where(IsEligible))
        {
            var claimed = ResolveChild(processing, Path.GetFileName(source));
            try
            {
                await using (var readiness = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.None, 1, FileOptions.Asynchronous))
                {
                    if (readiness.Length == 0 || readiness.Length > _options.MaximumFileBytes) continue;
                }
                File.Move(source, claimed, false);
            }
            catch (IOException) when (File.Exists(claimed)) { }
            catch (IOException) { }
        }

        var artifacts = new List<AchManagedMftArtifact>();
        foreach (var path in Directory.EnumerateFiles(processing).Where(IsEligible))
        {
            ct.ThrowIfCancellationRequested();
            var bytes = await File.ReadAllBytesAsync(path, ct);
            if (bytes.LongLength == 0 || bytes.LongLength > _options.MaximumFileBytes) continue;
            artifacts.Add(new(Path.GetFileName(path), bytes, Convert.ToHexString(SHA256.HashData(bytes)), path));
        }
        return artifacts;
    }

    public Task<string> ArchiveInboundAsync(AchManagedMftArtifact artifact, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var processing = ResolveRoot(_options.ProcessingPath);
        var archive = ResolveRoot(_options.ArchivePath);
        var source = Path.GetFullPath(artifact.ClaimReference);
        if (!source.StartsWith(processing + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("ACHCOL_MFT_CLAIM_REFERENCE_INVALID");
        Directory.CreateDirectory(archive);
        var target = ResolveChild(archive, artifact.FileName);
        if (File.Exists(target))
        {
            target = ResolveChild(archive, $"{Path.GetFileNameWithoutExtension(artifact.FileName)}.{artifact.ContentSha256[..12]}{Path.GetExtension(artifact.FileName)}");
        }
        if (File.Exists(source) && File.Exists(target))
        {
            using var stream = File.OpenRead(target);
            var hash = Convert.ToHexString(SHA256.HashData(stream));
            if (!string.Equals(hash, artifact.ContentSha256, StringComparison.OrdinalIgnoreCase))
                throw new IOException("ACHCOL_MFT_ARCHIVE_COLLISION");
            File.Delete(source);
        }
        else if (File.Exists(source)) File.Move(source, target, false);
        return Task.FromResult($"mft-archive:{Path.GetFileName(target)}");
    }

    private AchManagedMftResult? Validate(string fileName, byte[] content, string expectedHash)
    {
        if (content.Length == 0 || content.LongLength > _options.MaximumFileBytes)
            return Failure("ACHCOL_MFT_SIZE_INVALID", "El archivo está vacío o excede el tamaño permitido.", false);
        if (!string.Equals(fileName, Path.GetFileName(fileName), StringComparison.Ordinal))
            return Failure("ACHCOL_MFT_FILENAME_INVALID", "El nombre del archivo no es seguro.", false);
        var hash = Convert.ToHexString(SHA256.HashData(content));
        return string.Equals(hash, expectedHash, StringComparison.OrdinalIgnoreCase)
            ? null
            : Failure("ACHCOL_MFT_HASH_MISMATCH", "La identidad del archivo no coincide con su contenido.", false);
    }

    private async Task<AchManagedMftResult> ExistingAsync(string path, string expectedHash, CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);
        var hash = Convert.ToHexString(await SHA256.HashDataAsync(stream, ct));
        return string.Equals(hash, expectedHash, StringComparison.OrdinalIgnoreCase)
            ? new(true, false, false, "HANDOFF_ALREADY_COMMITTED", "El mismo archivo ya estaba entregado.", $"mft-out:{Path.GetFileName(path)}")
            : Failure("ACHCOL_MFT_NAME_COLLISION", "El nombre ya existe con contenido diferente.", false);
    }

    private static bool IsEligible(string path) => !Path.GetFileName(path).StartsWith('.') && path.EndsWith(".env", StringComparison.OrdinalIgnoreCase);
    private static string ResolveRoot(string configured)
    {
        if (string.IsNullOrWhiteSpace(configured)) throw new InvalidOperationException("ACHCOL_MFT_PATH_NOT_CONFIGURED");
        var full = Path.GetFullPath(configured).TrimEnd(Path.DirectorySeparatorChar);
        if (string.Equals(full, Path.GetPathRoot(full)?.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("ACHCOL_MFT_ROOT_PATH_NOT_ALLOWED");
        return full;
    }
    private static string ResolveChild(string root, string fileName)
    {
        var path = Path.GetFullPath(Path.Combine(root, Path.GetFileName(fileName)));
        if (!path.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("ACHCOL_MFT_PATH_INVALID");
        return path;
    }
    private static void TryDeleteTemporary(string path) { try { if (File.Exists(path)) File.Delete(path); } catch { } }
    private static AchManagedMftResult Failure(string code, string message, bool retryable, bool uncertain = false)
        => new(false, retryable, uncertain, code, message, null);
}
