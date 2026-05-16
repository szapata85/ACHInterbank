namespace Cfa.ACHInterbank.Application.ACH.Models;

public enum AchCauseCodeRail { Unknown=0, AchColombia=1, Cenit=2, Sta=3, Internal=4 }
public enum AchCauseCodeFlow { OutboundReturn=1, IncomingReturn=2, ReturnOfReturn=3, FileRejectTotal=4, FileRejectPartial=5, OperatorResponse=6, CommandCenter=7, InternalOnly=8 }
public enum AchCauseCodeKind { Unknown=0, ReturnReason=1, ReturnOfReturnReason=2, FileRejection=3, TechnicalIntegration=4, Internal=5 }
public enum AchCauseCodePolicySeverity { Info=1, Warning=2, Error=3 }

public sealed record AchCauseCodePolicyRequest(
    string Code,
    AchCauseCodeFlow Flow,
    int? ClearingHouseId = null,
    string? ClearingHouseCode = null,
    string? TransactionType = null,
    DateTime? EffectiveDate = null,
    string? OriginalReasonCode = null,
    string? NewReasonCode = null,
    string? Source = null);

public sealed record AchCauseCodePolicyIssue(string Code, string Message, AchCauseCodePolicySeverity Severity);

public sealed record AchCauseCodePolicyResult(
    bool IsAllowed,
    AchCauseCodeRail Rail,
    AchCauseCodeKind Kind,
    bool IsNormativePending,
    IReadOnlyList<AchCauseCodePolicyIssue> Issues);
