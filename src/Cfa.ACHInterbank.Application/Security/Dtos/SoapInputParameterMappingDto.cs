namespace Cfa.ACHInterbank.Application.Security.Dtos;

/// <summary>Relación entre un dato de entrada interno y un parámetro de una operación SOAP.</summary>
public record SoapInputParameterMappingDto
{
    /// <summary>Nombre del dato de entrada interno.</summary>
    public string InputName { get; init; } = string.Empty;

    /// <summary>Nombre del parámetro SOAP al que se asigna el dato.</summary>
    public string SoapParameterName { get; init; } = string.Empty;

    /// <summary>Valor por defecto opcional; los valores en blanco se normalizan a null al guardar.</summary>
    public string? DefaultValue { get; init; }

    /// <summary>Indica si el parámetro se considera requerido por el mapping.</summary>
    public bool Required { get; init; } = true;
}
