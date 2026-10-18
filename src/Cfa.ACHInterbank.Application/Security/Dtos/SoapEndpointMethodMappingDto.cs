using System.Text.Json.Serialization;

namespace Cfa.ACHInterbank.Application.Security.Dtos;

/// <summary>Mapping persistido para un método de una integración SOAP.</summary>
public record SoapEndpointMethodMappingDto
{
    /// <summary>Nombre del método SOAP; los mappings sin nombre se descartan al guardar.</summary>
    public string MethodName { get; init; } = string.Empty;

    /// <summary>Endpoint configurado para el método. En respuestas de administración se devuelve sanitizado.</summary>
    public string Endpoint { get; init; } = string.Empty;

    /// <summary>SOAPAction configurado para el método. En respuestas de administración se devuelve sanitizado.</summary>
    public string SoapAction { get; init; } = string.Empty;

    /// <summary>Modo operativo solicitado; el servicio lo normaliza a Live, Disabled o DryRun.</summary>
    public string OperatingMode { get; init; } = string.Empty;

    /// <summary>Timeout del método en segundos. Al guardar un método existente, un valor no positivo conserva el timeout previamente persistido.</summary>
    public int TimeoutSeconds { get; init; }

    /// <summary>Indica si el mapping está habilitado.</summary>
    public bool Enabled { get; init; } = true;

    /// <summary>Destino sanitizado calculado exclusivamente en backend para presentación y confirmaciones.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SanitizedEndpoint { get; init; }

    /// <summary>Acción técnica sanitizada calculada exclusivamente en backend para presentación.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? SanitizedSoapAction { get; init; }

    /// <summary>Estado funcional de la configuración calculado en backend.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? ConfigurationState { get; init; }

    /// <summary>Capabilities de administración calculadas en backend; la UI debe fallar cerrado si faltan.</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SoapEndpointMethodCapabilitiesDto? Capabilities { get; init; }

    /// <summary>Mappings de parámetros de entrada usados para construir la invocación SOAP.</summary>
    public List<SoapInputParameterMappingDto> InputParameterMappings { get; init; } = [];
}

/// <summary>Capabilities de administración de una conexión SOAP. No contiene reglas de seguridad del endpoint.</summary>
public record SoapEndpointMethodCapabilitiesDto
{
    /// <summary>Indica que backend reconoce el método y su estado vigente.</summary>
    public bool ConfigurationStateKnown { get; init; }

    /// <summary>Indica que backend puede evaluar una solicitud que habilite envío real para este método.</summary>
    public bool CanRequestRealSend { get; init; }

    /// <summary>Motivo funcional cuando la capability no puede determinarse.</summary>
    public string? BlockingReason { get; init; }
}
