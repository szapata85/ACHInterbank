namespace Cfa.ACHInterbank.Application.Integrations.Models;

public static class IntegrationGuaranteeConstants
{
    public const string Wscfaach = "WSCFAACH";
    public const string WsAxon = "WSAXON";
    public const string ProcContrapartidas = "Proc_Contrapartidas";
    public const string ProcTransacciones = "Proc_Transacciones";
    public const string RegistrarRespuestaTransaccion = "RegistrarRespuestaTransaccion";
    public const string MonetaryDebitRequest = "MonetaryDebitRequest";
    public const string MonetaryCreditRequest = "MonetaryCreditRequest";
    public const string DifferentialResponseNotification = "DifferentialResponseNotification";
    public const string OutboundRequest = "OutboundRequest";
    public const string InboundResponse = "InboundResponse";
}

public sealed record TransactionIntegrationOperationResult(
    int? TransactionId,
    string Reference,
    string IntegrationKey,
    string OperationKey,
    string MappingPurpose,
    string MappingDirection,
    string FunctionalNature,
    string FunctionalOriginator,
    bool MovesMoney,
    string Reason,
    bool IsSupported,
    IReadOnlyCollection<string> Errors);

public sealed record IntegrationMappingReadinessResult(
    bool IsReady,
    string Status,
    string Code,
    string IntegrationKey,
    string OperationKey,
    string MappingPurpose,
    string MappingDirection,
    int RequiredMappings,
    int ActiveMappings,
    IReadOnlyCollection<string> MissingRequiredMappings,
    IReadOnlyCollection<string> InactiveRequiredMappings,
    bool UsesFallback,
    bool CanBuildPayload,
    IReadOnlyCollection<string> Errors,
    IReadOnlyCollection<string> Warnings);

public sealed record TransactionIntegrationReadinessResult(
    int TransactionId,
    string Reference,
    string IntegrationKey,
    string OperationKey,
    string MappingPurpose,
    string MappingDirection,
    string FunctionalNature,
    string FunctionalOriginator,
    bool MovesMoney,
    string Reason,
    bool IsSupported,
    IntegrationMappingReadinessResult Readiness);
