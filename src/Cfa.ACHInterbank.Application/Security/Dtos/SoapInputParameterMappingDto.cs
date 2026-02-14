namespace Cfa.ACHInterbank.Application.Security.Dtos;

public record SoapInputParameterMappingDto
{
    public string InputName { get; init; } = string.Empty;
    public string SoapParameterName { get; init; } = string.Empty;
    public string? DefaultValue { get; init; }
    public bool Required { get; init; } = true;
}
