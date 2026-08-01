namespace Cfa.ACHInterbank.Application.Security.Dtos;

public record SoapEndpointMethodMappingDto
{
    public string MethodName { get; init; } = string.Empty;
    public string Endpoint { get; init; } = string.Empty;
    public string SoapAction { get; init; } = string.Empty;
    public string OperatingMode { get; init; } = string.Empty;
    public int TimeoutSeconds { get; init; }
    public bool Enabled { get; init; } = true;
    public List<SoapInputParameterMappingDto> InputParameterMappings { get; init; } = [];
}
