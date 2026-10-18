namespace Cfa.ACHInterbank.Application.Security.Dtos;

/// <summary>Configuración REST de mappings para las integraciones SOAP administradas por la aplicación.</summary>
public record SoapIntegrationSettingsDto
{
    /// <summary>Token de revisión calculado sobre la configuración persistida.</summary>
    public string Revision { get; init; } = "0";

    /// <summary>Revisión que el cliente espera modificar. Solo se usa en requests de actualización.</summary>
    public string? ExpectedRevision { get; init; }

    /// <summary>Mappings persistidos para métodos del servicio WSCFAACH.</summary>
    public List<SoapEndpointMethodMappingDto> WscfaachMappings { get; init; } = [];

    /// <summary>Mappings persistidos para métodos del servicio WsAxonRespuestaTransacciones.</summary>
    public List<SoapEndpointMethodMappingDto> WsAxonRespuestaTransaccionesMappings { get; init; } = [];

    /// <summary>Vista efectiva calculada por el servicio para Proc_Transacciones; se devuelve en la respuesta y no se persiste desde el request.</summary>
    public ProcTransaccionesEffectiveSettingsDto? ProcTransaccionesEffectiveSettings { get; init; }
}

/// <summary>Solicitud no persistente para evaluar un cambio de conexión antes de confirmar envío real.</summary>
public record SoapIntegrationSettingsChangePreviewRequestDto
{
    public string MethodName { get; init; } = string.Empty;
    public string Endpoint { get; init; } = string.Empty;
    public string SoapAction { get; init; } = string.Empty;
    public string OperatingMode { get; init; } = "DryRun";
    public bool Enabled { get; init; }
    public string ExpectedRevision { get; init; } = string.Empty;
}

/// <summary>Resultado backend para una confirmación contextual de cambio de conexión.</summary>
public record SoapIntegrationSettingsChangePreviewDto
{
    public string MethodName { get; init; } = string.Empty;
    public string Revision { get; init; } = string.Empty;
    public string CurrentConfigurationState { get; init; } = "Unknown";
    public string RequestedConfigurationState { get; init; } = "Unknown";
    public string SanitizedEndpoint { get; init; } = string.Empty;
    public string SanitizedSoapAction { get; init; } = string.Empty;
    public bool WouldEnableRealSend { get; init; }
    public bool CanApply { get; init; }
    public string? BlockingReason { get; init; }
}

/// <summary>Estado efectivo calculado para la configuración y readiness de Proc_Transacciones.</summary>
public record ProcTransaccionesEffectiveSettingsDto
{
    /// <summary>Nombre de la operación evaluada; la implementación vigente usa Proc_Transacciones.</summary>
    public string Operation { get; init; } = "Proc_Transacciones";

    /// <summary>Modo efectivo normalizado por el servicio: Live, Disabled o DryRun.</summary>
    public string EffectiveMode { get; init; } = "DryRun";

    /// <summary>Endpoint configurado para el mapping de Proc_Transacciones, sanitizado para presentación.</summary>
    public string Endpoint { get; init; } = string.Empty;

    /// <summary>Timeout configurado para el mapping, en segundos; es 0 cuando no existe mapping.</summary>
    public int TimeoutSeconds { get; init; }

    /// <summary>Indica si el mapping de Proc_Transacciones está habilitado.</summary>
    public bool Enabled { get; init; }

    /// <summary>Indica si la configuración runtime y la evaluación de mapping permiten construir el payload requerido.</summary>
    public bool MappingReady { get; init; }

    /// <summary>Código de la condición que bloquea el readiness; es null cuando MappingReady es true.</summary>
    public string? MappingIssueCode { get; init; }

    /// <summary>Parámetros requeridos que la evaluación de readiness identifica como bloqueantes.</summary>
    public IReadOnlyCollection<string> BlockingParameters { get; init; } = [];
}
