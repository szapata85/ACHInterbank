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

/// <summary>
/// Estado de preparación de los mappings requeridos para construir el payload de una operación de integración.
/// </summary>
/// <param name="IsReady">Indica si la evaluación considera listos los mappings requeridos.</param>
/// <param name="Status">Estado textual producido por la evaluación de readiness.</param>
/// <param name="Code">Código producido por la evaluación de readiness.</param>
/// <param name="IntegrationKey">Clave de integración evaluada.</param>
/// <param name="OperationKey">Clave de operación evaluada.</param>
/// <param name="MappingPurpose">Propósito de mapping evaluado.</param>
/// <param name="MappingDirection">Dirección de mapping evaluada.</param>
/// <param name="RequiredMappings">Cantidad de mappings requeridos por la evaluación.</param>
/// <param name="ActiveMappings">Cantidad de mappings activos encontrados.</param>
/// <param name="MissingRequiredMappings">Mappings requeridos que no fueron encontrados.</param>
/// <param name="InactiveRequiredMappings">Mappings requeridos encontrados pero inactivos.</param>
/// <param name="FallbackFields">Campos para los que la evaluación identifica fallback.</param>
/// <param name="RequiredFallbackFields">Campos de fallback requeridos por la evaluación.</param>
/// <param name="UsesFallback">Indica si la evaluación recurre a fallback.</param>
/// <param name="CanBuildPayload">Indica si la configuración evaluada permite construir el payload técnico.</param>
/// <param name="Errors">Errores producidos durante la evaluación.</param>
/// <param name="Warnings">Advertencias producidas durante la evaluación.</param>
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
    IReadOnlyCollection<string> FallbackFields,
    IReadOnlyCollection<string> RequiredFallbackFields,
    bool UsesFallback,
    bool CanBuildPayload,
    IReadOnlyCollection<string> Errors,
    IReadOnlyCollection<string> Warnings)
{
    /// <summary>Identificador del conjunto de mappings seleccionado, cuando existe.</summary>
    public Guid? MappingSetId { get; init; }

    /// <summary>Versión del conjunto de mappings seleccionado, cuando existe.</summary>
    public int? MappingVersion { get; init; }

    /// <summary>Hash del snapshot de mappings seleccionado, cuando existe.</summary>
    public string? MappingSnapshotHash { get; init; }
}

/// <summary>
/// Resultado de readiness de integración para una transacción existente.
/// </summary>
/// <param name="TransactionId">Identificador interno de la transacción evaluada.</param>
/// <param name="Reference">Referencia operativa resuelta para la integración.</param>
/// <param name="IntegrationKey">Clave de integración resuelta.</param>
/// <param name="OperationKey">Clave de operación resuelta.</param>
/// <param name="MappingPurpose">Propósito de mapping resuelto.</param>
/// <param name="MappingDirection">Dirección de mapping resuelta.</param>
/// <param name="FunctionalNature">Naturaleza funcional resuelta.</param>
/// <param name="FunctionalOriginator">Originador funcional resuelto.</param>
/// <param name="MovesMoney">Indica si la clasificación resuelta representa una operación monetaria.</param>
/// <param name="Reason">Motivo asociado a la resolución de la operación.</param>
/// <param name="IsSupported">Indica si la operación de integración resuelta está soportada.</param>
/// <param name="Readiness">Evaluación técnica de mappings y capacidad de construir el payload.</param>
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
