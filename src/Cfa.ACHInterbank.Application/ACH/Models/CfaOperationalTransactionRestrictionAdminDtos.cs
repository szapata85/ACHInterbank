using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record CfaOperationalTransactionRestrictionFilter(
    string? ClearingHouseCode = null,
    string? PolicyVersion = null,
    int? ClearingHouseCycleConfigId = null,
    TransactionTypeEnum? TransactionType = null,
    bool? IsPrenotification = null,
    bool? IsActive = null,
    DateTime? EffectiveAt = null);

/// <summary>Registro administrativo de una restricción operacional CFA y su alcance.</summary>
/// <param name="Id">Identificador interno de la restricción.</param>
/// <param name="ClearingHouseId">Identificador de cámara cuando la restricción tiene alcance directo por cámara; puede provenir de la configuración de ciclo al consultar el DTO.</param>
/// <param name="ClearingHouseCode">Código de la cámara asociada cuando existe alcance de cámara o de configuración de ciclo.</param>
/// <param name="ClearingHouseName">Nombre de la cámara asociada cuando existe.</param>
/// <param name="ClearingHouseCycleConfigId">Identificador de configuración versionada de ciclo cuando la restricción usa ese alcance; null para alcances globales o directos por cámara.</param>
/// <param name="PolicyVersion">Versión de política de la configuración de ciclo asociada; null fuera de ese alcance.</param>
/// <param name="CycleName">Nombre del ciclo de la configuración asociada; null fuera de ese alcance.</param>
/// <param name="TransactionType">Tipo de transacción restringido; null hace que el registro pueda aplicar a cualquier tipo durante la resolución.</param>
/// <param name="IsPrenotification">Valor de prenotificación restringido; null hace que el registro pueda aplicar tanto a prenotificaciones como a transacciones que no lo son.</param>
/// <param name="MaxAmountPerTransaction">Límite máximo por transacción. Null significa que este registro no reemplaza ese límite durante la resolución.</param>
/// <param name="MaxAmountPerCycle">Límite máximo por ciclo. Null significa que este registro no reemplaza ese límite durante la resolución.</param>
/// <param name="MaxTransactionsPerCycle">Cantidad máxima de transacciones por ciclo. Null significa que este registro no reemplaza ese límite durante la resolución.</param>
/// <param name="AllowedAccountTypes">Tipos de cuenta permitidos. Null no reemplaza una lista resuelta previamente; una colección vacía permanece vacía y no equivale a null.</param>
/// <param name="EffectiveFrom">Inicio de vigencia de la restricción.</param>
/// <param name="EffectiveTo">Fin de vigencia opcional; cuando existe no puede anteceder a EffectiveFrom.</param>
/// <param name="IsActive">Indica si la restricción está activa para la resolución operacional.</param>
/// <param name="CreatedAt">Marca temporal de creación persistida.</param>
/// <param name="UpdatedAt">Marca temporal de última actualización persistida.</param>
/// <param name="RowVersion">Token de concurrencia actual codificado en Base64 y requerido como ExpectedRowVersion para modificaciones posteriores.</param>
public sealed record CfaOperationalTransactionRestrictionDto(
    int Id,
    int? ClearingHouseId,
    string? ClearingHouseCode,
    string? ClearingHouseName,
    int? ClearingHouseCycleConfigId,
    string? PolicyVersion,
    string? CycleName,
    TransactionTypeEnum? TransactionType,
    bool? IsPrenotification,
    decimal? MaxAmountPerTransaction,
    decimal? MaxAmountPerCycle,
    int? MaxTransactionsPerCycle,
    IReadOnlyList<AccountTypeEnum>? AllowedAccountTypes,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    string RowVersion);

/// <summary>Datos para crear una restricción operacional CFA.</summary>
/// <param name="ClearingHouseCode">Código opcional de cámara. Si no se informa junto con ClearingHouseCycleConfigId, la restricción queda en alcance global.</param>
/// <param name="ClearingHouseCycleConfigId">Configuración versionada de ciclo opcional. Cuando se informa, define el alcance de ciclo y debe existir; si también se informa ClearingHouseCode, ambos deben corresponder a la misma cámara.</param>
/// <param name="TransactionType">Tipo de transacción opcional. Null permite que la restricción participe en la resolución de cualquier tipo.</param>
/// <param name="IsPrenotification">Condición opcional de prenotificación. Null permite que la restricción participe en ambos valores.</param>
/// <param name="MaxAmountPerTransaction">Límite opcional por transacción; no puede ser negativo.</param>
/// <param name="MaxAmountPerCycle">Límite opcional por ciclo; no puede ser negativo.</param>
/// <param name="MaxTransactionsPerCycle">Cantidad máxima opcional de transacciones por ciclo; no puede ser negativa.</param>
/// <param name="AllowedAccountTypes">Tipos de cuenta permitidos opcionales. El servicio elimina duplicados y los ordena por valor de enum.</param>
/// <param name="EffectiveFrom">Inicio obligatorio de vigencia; el valor por defecto de DateTime no es aceptado por el servicio.</param>
/// <param name="EffectiveTo">Fin de vigencia opcional; no puede ser anterior a EffectiveFrom.</param>
/// <param name="IsActive">Estado inicial de la restricción; por defecto true.</param>
public sealed record CreateCfaOperationalTransactionRestrictionRequest(
    string? ClearingHouseCode,
    int? ClearingHouseCycleConfigId,
    TransactionTypeEnum? TransactionType,
    bool? IsPrenotification,
    decimal? MaxAmountPerTransaction,
    decimal? MaxAmountPerCycle,
    int? MaxTransactionsPerCycle,
    IReadOnlyList<AccountTypeEnum>? AllowedAccountTypes,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    bool IsActive = true);

/// <summary>Datos para reemplazar una restricción operacional CFA existente.</summary>
/// <param name="ClearingHouseCode">Código opcional de cámara para el nuevo alcance.</param>
/// <param name="ClearingHouseCycleConfigId">Configuración versionada de ciclo opcional para el nuevo alcance; debe existir y corresponder al código de cámara cuando ambos se informan.</param>
/// <param name="TransactionType">Tipo de transacción opcional de la restricción.</param>
/// <param name="IsPrenotification">Condición opcional de prenotificación de la restricción.</param>
/// <param name="MaxAmountPerTransaction">Límite opcional por transacción; no puede ser negativo.</param>
/// <param name="MaxAmountPerCycle">Límite opcional por ciclo; no puede ser negativo.</param>
/// <param name="MaxTransactionsPerCycle">Cantidad máxima opcional de transacciones por ciclo; no puede ser negativa.</param>
/// <param name="AllowedAccountTypes">Tipos de cuenta permitidos opcionales; el servicio elimina duplicados y los ordena.</param>
/// <param name="EffectiveFrom">Nuevo inicio de vigencia; no puede ser el valor por defecto de DateTime.</param>
/// <param name="EffectiveTo">Nuevo fin de vigencia opcional; no puede ser anterior a EffectiveFrom.</param>
/// <param name="IsActive">Nuevo estado activo de la restricción.</param>
/// <param name="ExpectedRowVersion">RowVersion que el cliente espera modificar, codificada en Base64; es obligatoria para actualizar.</param>
public sealed record UpdateCfaOperationalTransactionRestrictionRequest(
    string? ClearingHouseCode,
    int? ClearingHouseCycleConfigId,
    TransactionTypeEnum? TransactionType,
    bool? IsPrenotification,
    decimal? MaxAmountPerTransaction,
    decimal? MaxAmountPerCycle,
    int? MaxTransactionsPerCycle,
    IReadOnlyList<AccountTypeEnum>? AllowedAccountTypes,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    bool IsActive,
    string ExpectedRowVersion);

/// <summary>Datos para activar o desactivar una restricción operacional CFA con control de concurrencia.</summary>
/// <param name="ExpectedRowVersion">RowVersion que el cliente espera modificar, codificada en Base64; es obligatoria.</param>
/// <param name="EffectiveTo">Fecha final de vigencia opcional que se guarda junto con el cambio de estado; no puede anteceder a EffectiveFrom.</param>
public sealed record ChangeCfaOperationalTransactionRestrictionStatusRequest(
    string ExpectedRowVersion,
    DateTime? EffectiveTo = null);

/// <summary>Error contractual producido por validaciones o conflictos de concurrencia de restricciones operacionales CFA.</summary>
/// <param name="Code">Código estable del error devuelto por el controller.</param>
/// <param name="Message">Mensaje asociado a la validación o al conflicto detectado.</param>
/// <param name="CurrentRowVersion">RowVersion vigente codificada en Base64 cuando el error es de concurrencia y el servicio pudo obtenerla.</param>
public sealed record CfaOperationalTransactionRestrictionApiError(
    string Code,
    string Message,
    string? CurrentRowVersion = null);
