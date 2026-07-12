namespace Cfa.ACHInterbank.Application.Security.Dtos;

public record SoapIntegrationSettingsDto
{
    public List<SoapEndpointMethodMappingDto> WscfaachMappings { get; init; } = [];
    public List<SoapEndpointMethodMappingDto> WsAxonRespuestaTransaccionesMappings { get; init; } = [];
    public ProcTransaccionesEffectiveSettingsDto? ProcTransaccionesEffectiveSettings { get; init; }
}

public record ProcTransaccionesEffectiveSettingsDto
{
    public string Operation { get; init; } = "Proc_Transacciones";
    public string EffectiveMode { get; init; } = "DryRun";
    public string Endpoint { get; init; } = string.Empty;
    public bool Enabled { get; init; }
    public bool MappingReady { get; init; }
}
