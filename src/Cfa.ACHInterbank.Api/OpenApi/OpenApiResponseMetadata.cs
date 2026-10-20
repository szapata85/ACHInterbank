namespace Cfa.ACHInterbank.Api.OpenApi;

/// <summary>
/// Asocia una descripción explícita a una response OpenAPI que ya forma parte del contrato de la operación.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class OpenApiResponseDescriptionAttribute(int statusCode, string description) : Attribute
{
    /// <summary>
    /// Status code de la response existente que se desea describir.
    /// </summary>
    public int StatusCode { get; } = statusCode;

    /// <summary>
    /// Descripción humana que se aplicará sin crear ni alterar el contrato de la response.
    /// </summary>
    public string Description { get; } = string.IsNullOrWhiteSpace(description)
        ? throw new ArgumentException("La descripción OpenAPI de la response no puede estar vacía.", nameof(description))
        : description.Trim();
}
