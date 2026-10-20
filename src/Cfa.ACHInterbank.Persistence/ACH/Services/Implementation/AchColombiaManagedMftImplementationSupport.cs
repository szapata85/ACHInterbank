using System.Globalization;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

internal static class AchColombiaManagedMftImplementationSupport
{
    internal const string ClearingHouseCode = "ACHCOL";

    internal static async Task<AchManagedFileTransfer> RequiredAsync(AchDbContext context, Guid id, CancellationToken ct)
        => await context.AchManagedFileTransfers.Include(x => x.Events)
               .SingleOrDefaultAsync(x => x.Id == id && x.ClearingHouse.Code == ClearingHouseCode, ct)
           ?? throw new KeyNotFoundException("ACHCOL_MFT_TRANSFER_NOT_FOUND");

    internal static AchManagedFileTransfer NewTransfer(
        int chamberId,
        AchManagedFileDirection direction,
        string fileName,
        byte[] content,
        DateTime operationalDate,
        string? cycleId,
        AchManagedFileExecutionOrigin origin,
        string actor,
        string idempotencyKey)
        => new()
        {
            ClearingHouseId = chamberId,
            Direction = direction,
            LogicalFileIdentity = $"{direction}:{Convert.ToHexString(SHA256.HashData(content))}",
            PhysicalFileName = Path.GetFileName(fileName),
            ContentSha256 = Convert.ToHexString(SHA256.HashData(content)),
            FileSize = content.LongLength,
            RetainedContent = content.ToArray(),
            OperationalDate = operationalDate,
            AchCycleId = cycleId,
            ExecutionOrigin = origin,
            OperatorIdentity = origin == AchManagedFileExecutionOrigin.Manual ? actor : null,
            CorrelationId = Guid.NewGuid().ToString("N"),
            IdempotencyKey = idempotencyKey
        };

    internal static void AddEvent(
        AchManagedFileTransfer transfer,
        string type,
        string result,
        string message,
        AchManagedFileExecutionOrigin origin,
        string actor)
        => transfer.Events.Add(new AchManagedFileTransferEvent
        {
            EventType = type,
            Result = result,
            Message = Limit(message, 1000),
            ExecutionOrigin = origin,
            Actor = Limit(actor, 160)
        });

    internal static bool IsEnabled(
        AchManagedFileTransferConfiguration configuration,
        AchManagedFileDirection direction,
        AchManagedFileExecutionOrigin origin)
        => (direction, origin) switch
        {
            (AchManagedFileDirection.Outbound, AchManagedFileExecutionOrigin.Automatic) => configuration.AutomaticOutboundEnabled,
            (AchManagedFileDirection.Inbound, AchManagedFileExecutionOrigin.Automatic) => configuration.AutomaticInboundEnabled,
            (AchManagedFileDirection.Outbound, _) => configuration.ManualOutboundAllowed,
            _ => configuration.ManualInboundAllowed
        };

    internal static void ValidateCommand(string actor, string key)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(actor);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
    }

    internal static string LimitRequired(string value, int max)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        return Limit(value, max);
    }

    internal static string Limit(string value, int max)
    {
        var trimmed = value.Trim();
        return trimmed[..Math.Min(trimmed.Length, max)];
    }

    internal static string? Optional(string? value, int max)
        => string.IsNullOrWhiteSpace(value) ? null : Limit(value, max);

    internal static int ResolveCycleNumber(string value)
    {
        var matches = Regex.Matches(value ?? string.Empty, @"(?<!\d)(\d+)(?!\d)");
        if (matches.Count != 1
            || !int.TryParse(matches[0].Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var number)
            || number <= 0)
        {
            throw new InvalidOperationException("ACHCOL_CYCLE_NUMBER_INVALID");
        }

        return number;
    }

    internal static string NormalizeHeaderIdentifier(string content, char? identifier)
    {
        if (!identifier.HasValue || content.Length < 36) return content;
        var chars = content.ToCharArray();
        chars[35] = identifier.Value;
        return new string(chars);
    }

    internal static bool HasPersistedInboundResult(AchManagedFileTransfer transfer)
        => transfer.Status is AchManagedFileTransferStatus.Processed
            or AchManagedFileTransferStatus.Duplicate
            or AchManagedFileTransferStatus.Rejected
            or AchManagedFileTransferStatus.RetryPending
            or AchManagedFileTransferStatus.Failed;

    internal static bool IsAuthorizedInboundEnvelopeName(string fileName)
    {
        var name = Path.GetFileName(fileName);
        return name.EndsWith(".OUT.env", StringComparison.OrdinalIgnoreCase)
            || name.EndsWith(".RET.env", StringComparison.OrdinalIgnoreCase);
    }

    internal static AchManagedFileTransferEventDto MapEvent(AchManagedFileTransferEvent e)
        => new(e.Id, e.OccurredAtUtc, e.EventType, e.Result, e.Message, e.ExecutionOrigin, e.Actor)
        {
            DisplayName = AchManagedFileTransferPresentation.EventName(e.EventType),
            DisplayResult = AchManagedFileTransferPresentation.EventResult(e.Result),
            DisplayMessage = AchManagedFileTransferPresentation.EventMessage(e.EventType, e.Result),
            ActorDisplay = AchManagedFileTransferPresentation.ActorDisplay(e.ExecutionOrigin, e.Actor)
        };

    internal static string? SanitizeStorageReference(string? reference)
    {
        if (string.IsNullOrWhiteSpace(reference)) return null;
        var normalized = reference.Trim().Replace('\\', '/');
        var separator = Math.Max(normalized.LastIndexOf('/'), normalized.LastIndexOf(':'));
        var fileName = separator >= 0 && separator + 1 < normalized.Length ? normalized[(separator + 1)..] : normalized;
        fileName = fileName.TrimStart('.');
        if (string.IsNullOrWhiteSpace(fileName)) return "Referencia administrada";
        if (normalized.StartsWith("retained:", StringComparison.OrdinalIgnoreCase)) return "Contenido retenido";
        if (normalized.StartsWith("mft-out:", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("sftp-out:", StringComparison.OrdinalIgnoreCase))
        {
            return $"Publicado en CFA: {fileName}";
        }

        if (normalized.Contains("archive", StringComparison.OrdinalIgnoreCase)) return $"Archivado: {fileName}";
        return $"Referencia administrada: {fileName}";
    }

    internal static (string Provider, string Protocol) NormalizeTransport(string provider, string protocol)
    {
        if (string.Equals(provider?.Trim(), "ManagedFolder", StringComparison.OrdinalIgnoreCase)
            && string.Equals(protocol?.Trim(), "ManagedFile", StringComparison.OrdinalIgnoreCase))
        {
            return ("ManagedFolder", "ManagedFile");
        }

        if (string.Equals(provider?.Trim(), "SftpEndpoint", StringComparison.OrdinalIgnoreCase)
            && string.Equals(protocol?.Trim(), "Sftp", StringComparison.OrdinalIgnoreCase))
        {
            return ("SftpEndpoint", "Sftp");
        }

        throw new ArgumentException("ACHCOL_MFT_TRANSPORT_UNSUPPORTED");
    }
}
