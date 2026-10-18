using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Domain.Entities.Ach.Dtos;

/// <summary>
/// Representa la configuración administrativa y el estado operativo resumido de una cámara compensadora.
/// </summary>
public class ClearingHouseDto
{
    /// <summary>Identificador interno de la cámara compensadora.</summary>
    public int Id { get; set; }

    /// <summary>Nombre configurado para la cámara compensadora.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Código funcional de la cámara usado por ACHInterbank.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Código de origen configurado para la cámara.</summary>
    public string OriginCode { get; set; } = string.Empty;

    /// <summary>Indica si la cámara está activa.</summary>
    public bool IsActive { get; set; }

    /// <summary>Identificador de zona horaria configurado para la operación de la cámara.</summary>
    public string TimeZoneId { get; set; } = string.Empty;

    /// <summary>Estrategia de calendario configurada para la cámara.</summary>
    public string? HolidayStrategy { get; set; }

    /// <summary>Código de la estrategia operativa o riel de pagos asociado a la cámara, cuando aplica.</summary>
    public string? PaymentRailCode { get; set; }

    /// <summary>Indica si la cámara requiere un perfil NACHA-M para cumplir su readiness.</summary>
    public bool RequiresNachaProfile { get; set; }

    /// <summary>Identificador del perfil NACHA-M asociado, cuando existe.</summary>
    public int? NachaProfileId { get; set; }

    /// <summary>Código del perfil NACHA-M asociado, cuando existe.</summary>
    public string? NachaProfileCode { get; set; }

    /// <summary>Nombre del perfil NACHA-M asociado, cuando existe.</summary>
    public string? NachaProfileName { get; set; }

    /// <summary>Cantidad de configuraciones de ciclo activas y vigentes consideradas para el readiness actual.</summary>
    public int ActiveCycleCount { get; set; }

    /// <summary>Indica si la cámara cumple actualmente todos los requisitos evaluados por el servicio de readiness.</summary>
    public bool IsReady { get; set; }

    /// <summary>Requisitos de readiness que aún no se cumplen; permanece vacío cuando IsReady es verdadero.</summary>
    public IReadOnlyList<string> MissingRequirements { get; set; } = [];

    /// <summary>Fecha y hora de creación registrada para la cámara.</summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>Fecha y hora de la última actualización registrada para la cámara.</summary>
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// Define los filtros y la paginación de la consulta administrativa de cámaras compensadoras.
/// </summary>
public sealed class ClearingHouseAdminQuery : PaginationRequest
{
    /// <summary>Texto opcional que se busca en el código o el nombre de la cámara.</summary>
    public string? Search { get; set; }

    /// <summary>Estado activo opcional usado para filtrar el listado.</summary>
    public bool? IsActive { get; set; }
}

/// <summary>
/// Define la configuración necesaria para registrar una cámara compensadora.
/// </summary>
public class CreateClearingHouseRequest
{
    /// <summary>Código funcional solicitado para la cámara.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Nombre de la cámara compensadora.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Código de origen asociado a la cámara.</summary>
    public string OriginCode { get; set; } = string.Empty;

    /// <summary>Identificador de la zona horaria que usará la operación de la cámara.</summary>
    public string TimeZoneId { get; set; } = string.Empty;

    /// <summary>Estrategia de calendario que se almacenará para la cámara.</summary>
    public string HolidayStrategy { get; set; } = string.Empty;

    /// <summary>Código opcional de la estrategia operativa registrada que se asociará a la cámara.</summary>
    public string? PaymentRailCode { get; set; }

    /// <summary>Indica si la cámara requiere un perfil NACHA-M asociado.</summary>
    public bool RequiresNachaProfile { get; set; }

    /// <summary>Identificador del perfil NACHA-M que se asociará cuando la cámara lo requiera.</summary>
    public int? NachaProfileId { get; set; }
}

/// <summary>
/// Define los valores editables de una cámara compensadora y un control opcional de concurrencia.
/// </summary>
public sealed class UpdateClearingHouseRequest : CreateClearingHouseRequest
{
    /// <summary>Marca de tiempo de actualización esperada; si no coincide con la versión persistida, la actualización se rechaza por conflicto.</summary>
    public DateTimeOffset? ExpectedUpdatedAt { get; set; }
}

/// <summary>
/// Solicita el estado activo deseado para una cámara compensadora.
/// </summary>
public sealed class ChangeClearingHouseStatusRequest
{
    /// <summary>Estado activo solicitado. La activación requiere que la cámara cumpla su readiness actual.</summary>
    public bool IsActive { get; set; }
}

/// <summary>
/// Resume el resultado de la evaluación de readiness de una cámara compensadora.
/// </summary>
public sealed class ClearingHouseReadinessDto
{
    /// <summary>Indica si todos los requisitos de readiness evaluados están cumplidos.</summary>
    public bool IsReady { get; init; }

    /// <summary>Requisitos pendientes detectados por la evaluación; permanece vacío cuando la cámara está lista.</summary>
    public IReadOnlyList<string> MissingRequirements { get; init; } = [];
}

/// <summary>
/// Opción de perfil NACHA-M publicada para la configuración de una cámara compensadora.
/// </summary>
public sealed class ClearingHouseNachaProfileOptionDto
{
    /// <summary>Identificador seleccionable como NachaProfileId al configurar una cámara.</summary>
    public int Id { get; init; }

    /// <summary>Código técnico del perfil NACHA-M.</summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>Nombre legible del perfil NACHA-M.</summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>Código de la cámara compensadora a la que corresponde el perfil.</summary>
    public string ClearingHouseCode { get; init; } = string.Empty;

    /// <summary>Indica que el perfil se encuentra en estado publicado.</summary>
    public bool IsPublished { get; init; }

    /// <summary>Indica si el perfil publicado está vigente en la fecha actual.</summary>
    public bool IsCurrent { get; init; }
}

/// <summary>
/// Opción de estrategia o riel de pago seleccionable para una cámara compensadora.
/// </summary>
public sealed class ClearingHousePaymentRailOptionDto
{
    /// <summary>Código técnico seleccionable como PaymentRailCode al configurar una cámara.</summary>
    public string Code { get; init; } = string.Empty;

    /// <summary>Nombre legible de la estrategia o riel de pago.</summary>
    public string Name { get; init; } = string.Empty;
}
