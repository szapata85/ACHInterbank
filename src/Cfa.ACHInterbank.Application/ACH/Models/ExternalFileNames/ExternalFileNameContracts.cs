namespace Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;

public enum ExternalFileType
{
    NachaOut = 1,
    NachaIn = 2,
    StaReject = 3,
    StaOut = 4,
    StaIn = 5,
    ReturnOut = 6,
    ReturnOfReturnOut = 7,
    OperatorReturnOut = 8,
    ResponseOut = 9,
    RejectionOut = 10
}

public enum ExternalFileFlow
{
    Originacion = 1,
    Recepcion = 2,
    Rechazo = 3
}

public enum ExternalFileDirection
{
    Outbound = 1,
    Inbound = 2
}

public enum ExternalFileValidationDisposition
{
    Passed = 0,
    HardBlock = 1,
    Warning = 2,
    AuditOnly = 3
}

public sealed class ExternalFileNameContext
{
    public int ClearingHouseId { get; init; }
    public string ClearingHouseCode { get; init; } = string.Empty;
    public string? ClearingHouseOriginCode { get; init; }
    public string? CycleId { get; init; }
    public string? CycleName { get; init; }
    public int? CycleNumber { get; init; }
    public DateTime ProcessingDate { get; init; }
    public OperationalTimeSnapshot? OperationalTimeSnapshot { get; init; }
    public string? IdempotencyKey { get; init; }
    public ExternalFileType ExternalFileType { get; init; }
    public ExternalFileFlow Flow { get; init; }
    public ExternalFileDirection Direction { get; init; }
    public bool IsPse { get; init; }
    public string? ProvidedExternalFileName { get; init; }
    public string? InternalFileName { get; init; }
    public string? NachaContent { get; init; }
    public int? DeclaredDetailCount { get; init; }
    public int? ActualDetailCount { get; init; }
    public string? FileHash { get; init; }
    public long? FileSize { get; init; }
    public string RequestedBy { get; init; } = "system";
}

public sealed class ExternalFileNameComponents
{
    public string FullName { get; init; } = string.Empty;
    public string? Prefix { get; init; }
    public int? ExternalSequence { get; init; }
    public int? CycleNumber { get; init; }
    public int? DeclaredDetailCount { get; init; }
    public char? FileIdModifier { get; init; }
    public long? ReservationId { get; init; }
    public bool ReusedReservation { get; init; }
}

public sealed record OperationalTimeSnapshot(
    DateTime CapturedAtUtc,
    DateTime BogotaTimestamp,
    DateOnly OperationalDate,
    string TimeZoneId);

public sealed record ExternalFileNameReservationResult(
    long ReservationId,
    int Sequence,
    bool WasReused,
    string IdempotencyKeyHash,
    string RequestFingerprintHash,
    string? ExternalFileName,
    char? FileIdModifier);

public sealed class ExternalFileNameValidationIssue
{
    public string RuleCode { get; init; } = string.Empty;
    public ExternalFileValidationDisposition Disposition { get; init; }
    public string IssueCode { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string? SourceReference { get; init; }
    public string? Evidence { get; init; }
}

public sealed class ExternalFileNameCorrelationEvidence
{
    public bool? NameMatchesRecord1Identifier { get; init; }
    public bool? NameMatchesDeclaredCount { get; init; }
    public char? HeaderFileIdModifier { get; init; }
    public int? ParsedSequence { get; init; }
    public int? DeclaredDetailCount { get; init; }
    public int? ActualDetailCount { get; init; }
    public string Notes { get; init; } = string.Empty;
}

public sealed class ExternalFileNameValidationResult
{
    public ExternalFileValidationDisposition Disposition { get; init; }
    public IReadOnlyList<ExternalFileNameValidationIssue> Issues { get; init; } = [];

    public bool IsHardBlocked => Disposition == ExternalFileValidationDisposition.HardBlock;
}

public sealed class ExternalFileNamePolicyResult
{
    public string ExternalFileName { get; init; } = string.Empty;
    public ExternalFileNameValidationResult Validation { get; init; } = new();
    public ExternalFileNameCorrelationEvidence CorrelationEvidence { get; init; } = new();
    public ExternalFileNameComponents Components { get; init; } = new();
}
