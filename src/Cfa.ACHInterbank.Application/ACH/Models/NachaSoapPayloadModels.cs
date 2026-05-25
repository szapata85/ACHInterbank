namespace Cfa.ACHInterbank.Application.ACH.Models;

public abstract class NachaSoapPayloadBase
{
    public string CorrelationId { get; init; } = string.Empty;
    public string SourceFileName { get; init; } = string.Empty;
    public string ClearingHouseCode { get; init; } = string.Empty;
    public string ProfileCode { get; init; } = string.Empty;
    public NachaSoapOperationCandidate OperationCandidate { get; init; } = NachaSoapOperationCandidate.None;
    public int? TransactionId { get; init; }
    public int? PrenotificationId { get; init; }
    public string EntryTraceNumber { get; init; } = string.Empty;
    public string OriginalTraceNumber { get; init; } = string.Empty;
    public long AmountInCents { get; init; }
    public string Currency { get; init; } = "COP";
    public string ReasonCode { get; init; } = string.Empty;
    public string ReasonDescription { get; init; } = string.Empty;
    public string SourceFinancialInstitutionCode { get; init; } = string.Empty;
    public string DestinationFinancialInstitutionCode { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public string Phase { get; init; } = "6B.5";
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
    public abstract bool RequiresMonetaryMovement { get; }
}

public sealed class NachaSoapProcContrapartidasPayload : NachaSoapPayloadBase
{
    public string SourceAccountReference { get; init; } = string.Empty;
    public string DestinationAccountReference { get; init; } = string.Empty;
    public override bool RequiresMonetaryMovement => true;
}

public sealed class NachaSoapProcTransaccionesPayload : NachaSoapPayloadBase
{
    public string ExternalOriginatorInstitutionCode { get; init; } = string.Empty;
    public string CfaReceiverInstitutionCode { get; init; } = string.Empty;
    public string SourceAccountReference { get; init; } = string.Empty;
    public string DestinationAccountReference { get; init; } = string.Empty;
    public override bool RequiresMonetaryMovement => true;
}

public sealed class NachaSoapRegistrarRespuestaTransaccionPayload : NachaSoapPayloadBase
{
    public string NewInternalStatus { get; init; } = string.Empty;
    public string ResponseType { get; init; } = string.Empty;
    public override bool RequiresMonetaryMovement => false;
}

public sealed class NachaSoapPayloadMappingResult
{
    public string CorrelationId { get; init; } = string.Empty;
    public NachaSoapOperationCandidate OperationCandidate { get; init; } = NachaSoapOperationCandidate.None;
    public bool IsMapped { get; init; }
    public bool IsExecutable { get; init; }
    public bool RequiresMonetaryMovement { get; init; }
    public string Phase { get; init; } = "6B.5";
    public string PayloadType { get; init; } = string.Empty;
    public NachaSoapPayloadBase? Payload { get; init; }
    public IReadOnlyDictionary<string, string> SanitizedSummary { get; init; } = new Dictionary<string, string>();
    public IReadOnlyList<string> Errors { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}

public sealed class NachaSoapExecutionContext
{
    public string CorrelationId { get; init; } = string.Empty;
    public string SourceFileName { get; init; } = string.Empty;
    public string ClearingHouseCode { get; init; } = string.Empty;
    public string ProfileCode { get; init; } = string.Empty;
    public long AmountInCents { get; init; }
    public string Currency { get; init; } = "COP";
    public string SourceAccountReference { get; init; } = string.Empty;
    public string DestinationAccountReference { get; init; } = string.Empty;
    public string SourceFinancialInstitutionCode { get; init; } = string.Empty;
    public string DestinationFinancialInstitutionCode { get; init; } = string.Empty;
    public string ExternalOriginatorInstitutionCode { get; init; } = string.Empty;
    public string CfaReceiverInstitutionCode { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}
