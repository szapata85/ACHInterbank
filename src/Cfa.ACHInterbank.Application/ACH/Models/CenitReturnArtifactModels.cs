namespace Cfa.ACHInterbank.Application.ACH.Models;

/// <summary>El nombre interno no cumple ni sustituye el naming externo homologado CENIT.</summary>
public sealed record CenitReturnArtifactResult(
    Guid PreparationId, string InternalFileName, byte[] Content, string Sha256,
    int RecordCount, string OfficialProfile, bool Transmittable = false);

public sealed record CenitReturnArtifactMetadata(
    Guid PreparationId, int OriginalTransactionId, string Status,
    string? Sha256, string? InternalFileName, string? ProfileCode,
    string? TraceNumber, DateTime? GeneratedAtUtc, string? GeneratedByUserId,
    bool Transmittable = false);
