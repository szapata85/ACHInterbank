using System.Security.Cryptography;
using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.External.Connections;

[Scoped]
public sealed class AchOutboundReturnTransportAdapter(
    IOptions<AchOutboundReturnTransportOptions> options) : IAchOutboundReturnTransportAdapter
{
    private readonly AchOutboundReturnTransportOptions _options = options.Value;

    public async Task<AchOutboundReturnTransportResult> TransmitAsync(
        AchOutboundReturnTransportRequest request,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        if (!_options.Enabled)
        {
            return Failure("RETURN_TRANSPORT_DISABLED", "El transporte Return Out no está habilitado.", false, now);
        }

        if (!string.Equals(_options.Mode, "CfaManagedHandoff", StringComparison.OrdinalIgnoreCase))
        {
            return Failure("RETURN_TRANSPORT_MODE_UNSUPPORTED", "El modo de transporte configurado no está soportado.", false, now);
        }

        if (request.Content.Length == 0 || request.Content.LongLength > _options.MaxFileBytes)
        {
            return Failure("RETURN_TRANSPORT_SIZE_INVALID", "El artefacto cifrado está vacío o excede el límite configurado.", false, now);
        }

        var actualHash = Convert.ToHexString(SHA256.HashData(request.Content));
        if (!string.Equals(actualHash, request.ContentSha256, StringComparison.OrdinalIgnoreCase))
        {
            return Failure("RETURN_TRANSPORT_HASH_MISMATCH", "La identidad del artefacto no coincide con su contenido.", false, now);
        }

        var safeFileName = Path.GetFileName(request.FileName);
        if (!string.Equals(safeFileName, request.FileName, StringComparison.Ordinal)
            || !safeFileName.EndsWith(".ENV", StringComparison.OrdinalIgnoreCase))
        {
            return Failure("RETURN_TRANSPORT_FILENAME_INVALID", "El artefacto debe usar un nombre seguro de sobre digital .ENV.", false, now);
        }

        string root;
        try
        {
            root = ResolveHandoffRoot(_options.HandoffDirectory);
        }
        catch (InvalidOperationException ex)
        {
            return Failure("RETURN_TRANSPORT_DIRECTORY_INVALID", ex.Message, false, now);
        }

        Directory.CreateDirectory(root);
        var target = Path.GetFullPath(Path.Combine(root, safeFileName));
        if (!target.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            return Failure("RETURN_TRANSPORT_PATH_INVALID", "La ruta final queda fuera del handoff configurado.", false, now);
        }

        var reference = $"CFA-MFT-HANDOFF:{actualHash}";
        if (File.Exists(target))
        {
            return await ExistingResultAsync(target, actualHash, reference, now, ct);
        }

        var temporary = Path.Combine(root, $".{safeFileName}.{Guid.NewGuid():N}.tmp");
        try
        {
            await using (var stream = new FileStream(
                             temporary,
                             FileMode.CreateNew,
                             FileAccess.Write,
                             FileShare.None,
                             81920,
                             FileOptions.Asynchronous | FileOptions.WriteThrough))
            {
                await stream.WriteAsync(request.Content, ct);
                await stream.FlushAsync(ct);
            }

            File.Move(temporary, target, overwrite: false);
            return new AchOutboundReturnTransportResult(
                true,
                false,
                "HANDOFF_COMMITTED",
                "Artefacto cifrado depositado atómicamente en la frontera MFT administrada por CFA.",
                reference,
                now);
        }
        catch (IOException) when (File.Exists(target))
        {
            return await ExistingResultAsync(target, actualHash, reference, now, ct);
        }
        catch (IOException)
        {
            return Failure("RETURN_TRANSPORT_IO_FAILURE", "No fue posible completar el handoff del artefacto cifrado.", true, now);
        }
        catch (UnauthorizedAccessException)
        {
            return Failure("RETURN_TRANSPORT_ACCESS_DENIED", "El runtime no tiene permisos sobre el handoff configurado.", false, now);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                try
                {
                    File.Delete(temporary);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }
    }

    private static async Task<AchOutboundReturnTransportResult> ExistingResultAsync(
        string target,
        string expectedHash,
        string reference,
        DateTime now,
        CancellationToken ct)
    {
        try
        {
            await using var existing = new FileStream(target, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, FileOptions.Asynchronous);
            var actual = Convert.ToHexString(await SHA256.HashDataAsync(existing, ct));
            return string.Equals(actual, expectedHash, StringComparison.OrdinalIgnoreCase)
                ? new AchOutboundReturnTransportResult(
                    true,
                    false,
                    "HANDOFF_ALREADY_COMMITTED",
                    "El mismo artefacto cifrado ya estaba depositado en la frontera MFT.",
                    reference,
                    now)
                : Failure(
                    "RETURN_TRANSPORT_NAME_COLLISION",
                    "El nombre externo ya existe con una identidad de contenido diferente.",
                    false,
                    now);
        }
        catch (IOException)
        {
            return Failure("RETURN_TRANSPORT_IO_FAILURE", "No fue posible verificar el artefacto existente en el handoff.", true, now);
        }
        catch (UnauthorizedAccessException)
        {
            return Failure("RETURN_TRANSPORT_ACCESS_DENIED", "El runtime no puede verificar el artefacto existente en el handoff.", false, now);
        }
    }

    private static string ResolveHandoffRoot(string configured)
    {
        if (string.IsNullOrWhiteSpace(configured))
        {
            throw new InvalidOperationException("No existe directorio de handoff configurado.");
        }

        var full = Path.GetFullPath(configured.Trim());
        var root = Path.GetPathRoot(full);
        if (string.Equals(full.TrimEnd(Path.DirectorySeparatorChar), root?.TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("El directorio de handoff no puede ser la raíz del sistema de archivos.");
        }

        return full.TrimEnd(Path.DirectorySeparatorChar);
    }

    private static AchOutboundReturnTransportResult Failure(
        string code,
        string summary,
        bool retryable,
        DateTime occurredAtUtc)
        => new(false, retryable, code, summary, null, occurredAtUtc);
}
