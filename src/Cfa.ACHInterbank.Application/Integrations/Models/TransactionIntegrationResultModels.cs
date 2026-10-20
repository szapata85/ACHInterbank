namespace Cfa.ACHInterbank.Application.Integrations.Models;

/// <summary>
/// Resultado resumido de un intento de integración registrado para una transacción.
/// </summary>
/// <param name="CatalogId">Identificador del catálogo de respuesta asociado al intento, cuando existe.</param>
/// <param name="Method">Método técnico de integración registrado para el intento.</param>
/// <param name="TransportStatus">Estado de transporte registrado por la integración.</param>
/// <param name="BusinessStatus">Estado de negocio registrado por la integración.</param>
/// <param name="ResponseCode">Código de respuesta registrado por la integración.</param>
/// <param name="ResponseDescription">Descripción de respuesta registrada por la integración.</param>
/// <param name="ProcessedAt">Fecha y hora de procesamiento registradas para el intento, cuando existen.</param>
/// <param name="AttemptNumber">Número de intento registrado.</param>
/// <param name="RetryAllowed">Indica si el registro de integración permite reintento.</param>
/// <param name="RequiresManualReview">Indica si el registro de integración requiere revisión manual.</param>
/// <param name="TransactionState">Estado actual de la transacción al momento de construir la consulta.</param>
public sealed record TransactionIntegrationResultItemDto(
    long? CatalogId,
    string Method,
    string TransportStatus,
    string BusinessStatus,
    string ResponseCode,
    string ResponseDescription,
    DateTime? ProcessedAt,
    int AttemptNumber,
    bool RetryAllowed,
    bool RequiresManualReview,
    string TransactionState);

/// <summary>
/// Resultado técnico de integración acumulado para una transacción.
/// </summary>
/// <param name="TransactionId">Identificador interno de la transacción consultada.</param>
/// <param name="Latest">Intento más reciente según fecha de procesamiento y número de intento; es nulo cuando no existen registros de integración.</param>
/// <param name="History">Historial de intentos registrados, ordenado del más reciente al más antiguo.</param>
public sealed record TransactionIntegrationResultDto(
    int TransactionId,
    TransactionIntegrationResultItemDto? Latest,
    IReadOnlyList<TransactionIntegrationResultItemDto> History);
