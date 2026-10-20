namespace Cfa.ACHInterbank.Api.OpenApi;

/// <summary>
/// Proporciona un ejemplo OpenAPI explícito y determinístico para una operación.
/// </summary>
public interface IOpenApiExampleProvider
{
    /// <summary>
    /// Obtiene el objeto ficticio que debe serializarse como ejemplo.
    /// </summary>
    object GetExample();
}

/// <summary>
/// Asocia un provider de ejemplo al cuerpo de request de una operación.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
public sealed class OpenApiRequestExampleAttribute(Type providerType) : Attribute
{
    /// <summary>
    /// Tipo que implementa <see cref="IOpenApiExampleProvider"/>.
    /// </summary>
    public Type ProviderType { get; } = providerType ?? throw new ArgumentNullException(nameof(providerType));

    /// <summary>
    /// Content type existente al que se aplicará el ejemplo.
    /// </summary>
    public string ContentType { get; set; } = "application/json";
}

/// <summary>
/// Asocia un provider de ejemplo a una response existente de una operación.
/// </summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class OpenApiResponseExampleAttribute(int statusCode, Type providerType) : Attribute
{
    /// <summary>
    /// Status code existente al que se aplicará el ejemplo.
    /// </summary>
    public int StatusCode { get; } = statusCode;

    /// <summary>
    /// Tipo que implementa <see cref="IOpenApiExampleProvider"/>.
    /// </summary>
    public Type ProviderType { get; } = providerType ?? throw new ArgumentNullException(nameof(providerType));

    /// <summary>
    /// Content type existente al que se aplicará el ejemplo.
    /// </summary>
    public string ContentType { get; set; } = "application/json";
}
