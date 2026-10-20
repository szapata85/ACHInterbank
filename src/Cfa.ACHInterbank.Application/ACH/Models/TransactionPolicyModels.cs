using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Models;

/// <summary>
/// Datos usados internamente para evaluar una política transaccional sin persistir una transacción.
/// </summary>
/// <param name="Amount">Monto incluido en la evaluación.</param>
/// <param name="TransactionExternalId">Identificador operativo/idempotencia canónico, cuando se informa.</param>
/// <param name="Reference">Referencia legada usada como identificador de respaldo.</param>
/// <param name="Type">Tipo de transacción evaluado.</param>
/// <param name="AccountType">Tipo de cuenta o producto evaluado.</param>
/// <param name="IsPrenotification">Indica si la operación candidata es una prenotificación.</param>
/// <param name="DestinationInstitutionId">Identificador de la institución destino usado para resolver el contexto operativo.</param>
/// <param name="SourceAccountNumber">Cuenta de origen incluida en la evaluación.</param>
/// <param name="DestinationAccountNumber">Cuenta de destino incluida en la evaluación.</param>
/// <param name="CompanyIdentification">Identificación del originador incluida en los datos de evaluación.</param>
/// <param name="RecipientIdNumber">Identificación opcional del receptor incluida en los datos de evaluación.</param>
public sealed record TransactionPolicyPreviewRequest(
    decimal Amount,
    string? TransactionExternalId,
    string Reference,
    TransactionTypeEnum Type,
    AccountTypeEnum AccountType,
    bool IsPrenotification,
    int DestinationInstitutionId,
    string SourceAccountNumber,
    string DestinationAccountNumber,
    string CompanyIdentification,
    string? RecipientIdNumber);

/// <summary>
/// Resultado de una evaluación previa de política transaccional. No representa persistencia ni garantía de procesamiento posterior.
/// </summary>
/// <param name="CanSubmit">Indica si la evaluación previa permite continuar con el registro según las reglas evaluadas en ese momento.</param>
/// <param name="Message">Mensaje asociado al rechazo o detalle relevante de la evaluación, cuando existe.</param>
/// <param name="CycleId">Identificador del ciclo resuelto para la evaluación, cuando pudo determinarse.</param>
/// <param name="CycleName">Nombre del ciclo resuelto, cuando pudo determinarse.</param>
/// <param name="ProcessingDate">Fecha de procesamiento del ciclo resuelto, cuando pudo determinarse.</param>
/// <param name="ClearingHouseName">Nombre de la cámara asociada al ciclo resuelto, cuando está disponible.</param>
/// <param name="ClearingHouseId">Identificador de la cámara asociada al ciclo resuelto, cuando está disponible.</param>
/// <param name="WindowLabel">Representación de la ventana operativa resuelta, cuando aplica.</param>
/// <param name="IsWithinProcessingWindow">Indica si la evaluación se realizó dentro de la ventana operativa resuelta.</param>
/// <param name="MaxAmountPerTransaction">Límite por transacción resuelto por la política, cuando está configurado.</param>
/// <param name="RemainingAmountForCycle">Monto restante calculado para el ciclo, cuando existe un límite aplicable.</param>
/// <param name="RemainingTransactionsForCycle">Cantidad restante calculada para el ciclo, cuando existe un límite aplicable.</param>
/// <param name="IdempotencyKey">Clave técnica calculada por la evaluación para identificar la combinación operativa considerada.</param>
/// <param name="WouldDuplicate">Indica si la evaluación encontró una transacción equivalente en el ciclo resuelto.</param>
public sealed record TransactionPolicyPreview(
    bool CanSubmit,
    string? Message,
    string? CycleId,
    string? CycleName,
    DateTime? ProcessingDate,
    string? ClearingHouseName,
    int? ClearingHouseId,
    string? WindowLabel,
    bool IsWithinProcessingWindow,
    decimal? MaxAmountPerTransaction,
    decimal? RemainingAmountForCycle,
    int? RemainingTransactionsForCycle,
    string? IdempotencyKey,
    bool WouldDuplicate);
