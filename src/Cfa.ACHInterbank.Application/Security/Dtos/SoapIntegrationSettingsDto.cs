namespace Cfa.ACHInterbank.Application.Security.Dtos;

public record SoapIntegrationSettingsDto
{
    public List<SoapEndpointMethodMappingDto> WscfaachMappings { get; init; } = [];
    public List<SoapEndpointMethodMappingDto> WsAxonRespuestaTransaccionesMappings { get; init; } = [];
}
