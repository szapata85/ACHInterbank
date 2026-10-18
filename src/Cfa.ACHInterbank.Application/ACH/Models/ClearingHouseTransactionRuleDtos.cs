using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Models;

/// <summary>Versión persistida de una política o regla transaccional asociada a una cámara compensadora.</summary>
/// <param name="Id">Identificador interno de la versión.</param>
/// <param name="ClearingHouseId">Identificador interno de la cámara compensadora.</param>
/// <param name="ClearingHouseName">Nombre de la cámara cargado junto con la versión.</param>
/// <param name="TransactionNature">Naturaleza Credit o Debit derivada del tipo de transacción por el servicio canónico.</param>
/// <param name="TransactionType">Tipo de transacción al que aplica la versión; el servicio canónico admite Credit y Debit.</param>
/// <param name="RequiresPrenotification">Indicador de compatibilidad que es true cuando PrenotificationMode es Mandatory.</param>
/// <param name="PrenotificationMode">Modo de prenotificación configurado para la versión.</param>
/// <param name="PrenotificationLeadBusinessDays">Cantidad opcional de días hábiles previos; se admite en modos Mandatory u Optional cuando la norma lo exige y se rechazan valores negativos.</param>
/// <param name="RequiresReceiverIdentificationValidation">Indica si la versión exige validación de identificación del receptor.</param>
/// <param name="ReceiverIdentificationValidationMode">Modo configurado para la validación de identificación del receptor.</param>
/// <param name="AppliesToNachaExport">Indicador de aplicación a exportación NACHA-M; el servicio canónico lo sincroniza en true.</param>
/// <param name="AppliesToMonetaryTransactions">Indicador de aplicación a transacciones monetarias; el servicio canónico lo sincroniza en true.</param>
/// <param name="EffectiveFrom">Fecha inicial de vigencia de la versión.</param>
/// <param name="EffectiveTo">Fecha final de vigencia cuando la versión está cerrada o limitada.</param>
/// <param name="IsActive">Indica si la versión está activa para resolución de política.</param>
/// <param name="NormativeSource">Texto de fuente normativa almacenado con la versión; el servicio exige un valor no vacío.</param>
/// <param name="NormativeReference">Referencia normativa almacenada con la versión; el servicio exige un valor no vacío.</param>
/// <param name="Notes">Notas persistidas de la versión; el servicio expone cadena vacía cuando no se informaron.</param>
/// <param name="CreatedAt">Marca temporal de creación heredada de la entidad auditable.</param>
/// <param name="UpdatedAt">Marca temporal de última actualización heredada de la entidad auditable.</param>
public sealed record ClearingHouseTransactionRuleDto(
    int Id,
    int ClearingHouseId,
    string ClearingHouseName,
    TransactionNature TransactionNature,
    TransactionTypeEnum TransactionType,
    bool RequiresPrenotification,
    PrenotificationRequirementMode PrenotificationMode,
    int? PrenotificationLeadBusinessDays,
    bool RequiresReceiverIdentificationValidation,
    ValidationRequirementMode ReceiverIdentificationValidationMode,
    bool AppliesToNachaExport,
    bool AppliesToMonetaryTransactions,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    bool IsActive,
    string NormativeSource,
    string NormativeReference,
    string Notes,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    PrenotificationResponseDeadlineMode PrenotificationResponseDeadlineMode = PrenotificationResponseDeadlineMode.NotApplicable,
    int? PrenotificationResponseWindowCycles = null,
    bool TacitAcceptanceEnabled = false,
    int? ValidationRetentionMonthsAfterLastMonetaryUse = null);

/// <summary>Contrato para crear una regla transaccional asociada a una cámara.</summary>
/// <param name="ClearingHouseId">Identificador de la cámara que debe existir.</param>
/// <param name="TransactionNature">Naturaleza que debe corresponder al TransactionType informado.</param>
/// <param name="TransactionType">Tipo de transacción; el servicio admite tipos que resuelven a naturaleza Credit o Debit.</param>
/// <param name="RequiresPrenotification">Indicador que debe coincidir con si PrenotificationMode es Mandatory.</param>
/// <param name="PrenotificationMode">Modo de prenotificación de la nueva versión.</param>
/// <param name="PrenotificationLeadBusinessDays">Días hábiles previos opcionales; se admiten para modos Mandatory u Optional cuando la norma lo exige y no pueden ser negativos.</param>
/// <param name="RequiresReceiverIdentificationValidation">Indica si la regla requiere validación de identificación del receptor.</param>
/// <param name="ReceiverIdentificationValidationMode">Modo de la validación de identificación del receptor.</param>
/// <param name="AppliesToNachaExport">Debe ser true para la política canónica actual.</param>
/// <param name="AppliesToMonetaryTransactions">Debe ser true para la política canónica actual.</param>
/// <param name="EffectiveFrom">Fecha inicial de vigencia.</param>
/// <param name="EffectiveTo">Fecha final opcional; no puede ser anterior a EffectiveFrom ni solaparse con versiones posteriores.</param>
/// <param name="NormativeSource">Fuente normativa asociada; no puede ser vacía.</param>
/// <param name="NormativeReference">Referencia normativa asociada; no puede ser vacía.</param>
/// <param name="Notes">Notas opcionales de la regla.</param>
public sealed record CreateClearingHouseTransactionRuleRequest(
    int ClearingHouseId,
    TransactionNature TransactionNature,
    TransactionTypeEnum TransactionType,
    bool RequiresPrenotification,
    PrenotificationRequirementMode PrenotificationMode,
    int? PrenotificationLeadBusinessDays,
    bool RequiresReceiverIdentificationValidation,
    ValidationRequirementMode ReceiverIdentificationValidationMode,
    bool AppliesToNachaExport,
    bool AppliesToMonetaryTransactions,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    string NormativeSource,
    string NormativeReference,
    string? Notes,
    PrenotificationResponseDeadlineMode PrenotificationResponseDeadlineMode = PrenotificationResponseDeadlineMode.NotApplicable,
    int? PrenotificationResponseWindowCycles = null,
    bool TacitAcceptanceEnabled = false,
    int? ValidationRetentionMonthsAfterLastMonetaryUse = null);

/// <summary>Contrato para actualizar una regla transaccional existente.</summary>
/// <param name="ClearingHouseId">Identificador de cámara; no puede cambiar respecto de la versión existente.</param>
/// <param name="TransactionNature">Naturaleza que debe corresponder al TransactionType informado.</param>
/// <param name="TransactionType">Tipo de transacción; no puede cambiar respecto de la versión existente.</param>
/// <param name="RequiresPrenotification">Indicador que debe coincidir con si PrenotificationMode es Mandatory.</param>
/// <param name="PrenotificationMode">Modo de prenotificación solicitado.</param>
/// <param name="PrenotificationLeadBusinessDays">Días hábiles previos opcionales; se admiten para modos Mandatory u Optional cuando la norma lo exige y no pueden ser negativos.</param>
/// <param name="RequiresReceiverIdentificationValidation">Indica si se requiere validación de identificación del receptor.</param>
/// <param name="ReceiverIdentificationValidationMode">Modo solicitado para la validación de identificación del receptor.</param>
/// <param name="AppliesToNachaExport">Debe ser true para la política canónica actual.</param>
/// <param name="AppliesToMonetaryTransactions">Debe ser true para la política canónica actual.</param>
/// <param name="EffectiveFrom">Fecha inicial. Si cambia la decisión funcional debe ser posterior a la de la versión existente y el servicio crea una nueva versión.</param>
/// <param name="EffectiveTo">Fecha final opcional de vigencia.</param>
/// <param name="NormativeSource">Fuente normativa asociada; no puede ser vacía.</param>
/// <param name="NormativeReference">Referencia normativa asociada; no puede ser vacía.</param>
/// <param name="Notes">Notas opcionales de la regla.</param>
public sealed record UpdateClearingHouseTransactionRuleRequest(
    int ClearingHouseId,
    TransactionNature TransactionNature,
    TransactionTypeEnum TransactionType,
    bool RequiresPrenotification,
    PrenotificationRequirementMode PrenotificationMode,
    int? PrenotificationLeadBusinessDays,
    bool RequiresReceiverIdentificationValidation,
    ValidationRequirementMode ReceiverIdentificationValidationMode,
    bool AppliesToNachaExport,
    bool AppliesToMonetaryTransactions,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    string NormativeSource,
    string NormativeReference,
    string? Notes,
    PrenotificationResponseDeadlineMode PrenotificationResponseDeadlineMode = PrenotificationResponseDeadlineMode.NotApplicable,
    int? PrenotificationResponseWindowCycles = null,
    bool TacitAcceptanceEnabled = false,
    int? ValidationRetentionMonthsAfterLastMonetaryUse = null);

/// <summary>Datos usados para previsualizar la política de prerequisitos transaccionales.</summary>
/// <param name="ClearingHouseId">Identificador de cámara usado en la evaluación; en la ruta canónica anidada el controller lo reemplaza por el identificador de la URL.</param>
/// <param name="TransactionType">Tipo de transacción a evaluar. Los tipos que no resuelven a Credit o Debit producen una decisión NOT_APPLICABLE.</param>
/// <param name="EffectiveEntryDate">Fecha efectiva usada para seleccionar una regla activa dentro de su intervalo de vigencia.</param>
/// <param name="AppliesToNachaExport">Indica el valor de alcance de exportación con el que se busca la regla; por defecto es true.</param>
public sealed record TransactionPrerequisitePreviewRequest(
    int ClearingHouseId,
    TransactionTypeEnum TransactionType,
    DateTime EffectiveEntryDate,
    bool AppliesToNachaExport = true);

/// <summary>Resultado de la evaluación previa de una política de prerequisitos transaccionales.</summary>
/// <param name="RuleConfigured">Indica si se encontró una regla vigente para los datos evaluados.</param>
/// <param name="RequiresPrenotification">Indica si la regla encontrada requiere prenotificación obligatoria.</param>
/// <param name="PrenotificationMode">Modo de prenotificación resultante; NotApplicable se usa cuando el tipo no aplica o no existe regla.</param>
/// <param name="RequiresReceiverIdentificationValidation">Indica si la regla vigente requiere validación de identificación del receptor.</param>
/// <param name="ReceiverIdentificationValidationMode">Modo de validación de identificación del receptor resultante.</param>
/// <param name="NormativeSource">Fuente normativa almacenada en la regla cuando existe una regla vigente.</param>
/// <param name="NormativeReference">Referencia normativa almacenada en la regla cuando existe una regla vigente.</param>
/// <param name="Decision">Código textual calculado por el servicio, como NOT_APPLICABLE, RULE_NOT_CONFIGURED, PRENOTIFICATION_REQUIRED o PRENOTIFICATION_OPTIONAL.</param>
/// <param name="Message">Mensaje descriptivo calculado por el servicio para la decisión.</param>
public sealed record TransactionPrerequisitePreviewResponse(
    bool RuleConfigured,
    bool RequiresPrenotification,
    PrenotificationRequirementMode PrenotificationMode,
    bool RequiresReceiverIdentificationValidation,
    ValidationRequirementMode ReceiverIdentificationValidationMode,
    string? NormativeSource,
    string? NormativeReference,
    string Decision,
    string Message);


/// <summary>
/// Contexto server-side para decidir si una transacción ya enrutada puede cruzar el prerequisito
/// canónico antes de persistirse.
/// </summary>
public sealed record TransactionRegistrationPrerequisiteRequest(
    bool IsPrenotification,
    ClearingHouseTransactionRule? PolicyRule,
    ThirdPartyAccountEligibilityResult? Eligibility);

/// <summary>Solicitud canónica para crear una nueva versión de política transaccional dentro de una cámara.</summary>
/// <param name="TransactionType">Tipo de transacción de la versión; el servicio canónico admite Credit y Debit.</param>
/// <param name="PrenotificationMode">Modo de prenotificación de la nueva versión.</param>
/// <param name="PrenotificationLeadBusinessDays">Días hábiles previos opcionales; se admiten en modos Mandatory u Optional cuando la norma lo exige y no pueden ser negativos.</param>
/// <param name="EffectiveFrom">Fecha inicial de vigencia.</param>
/// <param name="EffectiveTo">Fecha final opcional; no puede ser anterior a EffectiveFrom ni solaparse con una versión posterior.</param>
/// <param name="IsActive">Estado activo inicial solicitado para la versión.</param>
/// <param name="NormativeSource">Fuente normativa asociada; no puede ser vacía.</param>
/// <param name="NormativeReference">Referencia normativa asociada; no puede ser vacía.</param>
/// <param name="Notes">Notas opcionales de la versión.</param>
public sealed record CreateClearingHouseTransactionPolicyVersionRequest(
    TransactionTypeEnum TransactionType,
    PrenotificationRequirementMode PrenotificationMode,
    int? PrenotificationLeadBusinessDays,
    DateTime EffectiveFrom,
    DateTime? EffectiveTo,
    bool IsActive,
    string NormativeSource,
    string NormativeReference,
    string? Notes,
    PrenotificationResponseDeadlineMode PrenotificationResponseDeadlineMode = PrenotificationResponseDeadlineMode.NotApplicable,
    int? PrenotificationResponseWindowCycles = null,
    bool TacitAcceptanceEnabled = false,
    int? ValidationRetentionMonthsAfterLastMonetaryUse = null);

/// <summary>Solicitud para reemplazar únicamente la metadata descriptiva de una versión de política.</summary>
/// <param name="NormativeSource">Nueva fuente normativa; no puede ser vacía.</param>
/// <param name="NormativeReference">Nueva referencia normativa; no puede ser vacía.</param>
/// <param name="Notes">Nuevas notas opcionales; se almacenan como cadena vacía cuando no se informan.</param>
public sealed record UpdateClearingHouseTransactionPolicyMetadataRequest(
    string NormativeSource,
    string NormativeReference,
    string? Notes);

/// <summary>Solicitud para fijar la fecha final de vigencia de una versión de política.</summary>
/// <param name="EffectiveTo">Fecha final a asignar; no puede ser anterior al inicio de la versión ni solaparse con el inicio de la siguiente versión.</param>
public sealed record CloseClearingHouseTransactionPolicyVersionRequest(
    DateTime EffectiveTo);
