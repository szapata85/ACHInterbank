using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Application.Integrations.Dtos;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/integrations")]
[Authorize]
public class IntegrationCatalogController : ControllerBase
{
    private readonly IIntegrationCatalogService _catalogService;

    public IntegrationCatalogController(IIntegrationCatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    /// <summary>Consulta el catálogo de métodos de integración.</summary>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar métodos de integración")]
    [EndpointDescription("Devuelve los métodos del catálogo de integración después de que el servicio asegura su bootstrap persistido. Los métodos se ordenan por Id e incluyen las claves y la clasificación funcional calculadas por el servicio a partir del código del método.")]
    [HttpGet("methods")]
    [Authorize(Policy = P1Policies.IntegrationsRead)]
    [ProducesResponseType(typeof(IReadOnlyCollection<IntegrationMethodDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Colección de métodos de integración expuestos por el catálogo, ordenada por identificador.")]
    public async Task<IActionResult> GetMethods(CancellationToken ct)
        => Ok(await _catalogService.GetMethodsAsync(ct));

    /// <summary>Consulta parámetros activos de un método de integración.</summary>
    /// <param name="methodId">Identificador entero del método de integración.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar parámetros de un método de integración")]
    [EndpointDescription("Devuelve los parámetros activos cuyo MethodId coincide con methodId, ordenados por SortOrder y ParameterPath. Si el identificador no tiene parámetros activos, el servicio devuelve una colección vacía; la acción no traduce ese caso a 404.")]
    [HttpGet("methods/{methodId:int}/parameters")]
    [Authorize(Policy = P1Policies.IntegrationsRead)]
    [ProducesResponseType(typeof(IReadOnlyCollection<IntegrationMethodParameterDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Colección, posiblemente vacía, de parámetros activos del método solicitado.")]
    public async Task<IActionResult> GetParametersByMethod(int methodId, CancellationToken ct)
        => Ok(await _catalogService.GetMethodParametersAsync(methodId, ct));

    /// <summary>Consulta el catálogo de campos fuente disponible para mappings de integración.</summary>
    /// <param name="methodId">Filtro opcional por método. Cuando se informa, el servicio incluye campos globales sin MethodId y campos asociados al método indicado.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar catálogo de campos fuente de integración")]
    [EndpointDescription("Devuelve campos fuente activos. Sin methodId incluye todo el catálogo activo; con methodId incluye los campos globales cuyo MethodId es null y los asociados al método solicitado. El servicio ordena por SourceKind, SortOrder y FieldPath.")]
    [HttpGet("source-catalog")]
    [Authorize(Policy = P1Policies.IntegrationsRead)]
    [ProducesResponseType(typeof(IReadOnlyCollection<IntegrationSourceCatalogFieldDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Colección de campos fuente activos que cumplen el filtro opcional por método.")]
    public async Task<IActionResult> GetSourceCatalog([FromQuery] int? methodId, CancellationToken ct)
        => Ok(await _catalogService.GetSourceCatalogAsync(methodId, ct));

    /// <summary>Consulta las transformaciones soportadas por el catálogo de integración.</summary>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar transformaciones de integración")]
    [EndpointDescription("Devuelve el catálogo en memoria de transformaciones que el servicio publica para mappings, junto con los indicadores que señalan si cada transformación admite máscara de formato o múltiples fuentes.")]
    [HttpGet("transformations")]
    [Authorize(Policy = P1Policies.IntegrationsRead)]
    [ProducesResponseType(typeof(IReadOnlyCollection<IntegrationTransformationCatalogDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Colección de transformaciones disponibles para configuración de mappings de integración.")]
    public async Task<IActionResult> GetTransformations(CancellationToken ct)
        => Ok(await _catalogService.GetTransformationsAsync(ct));
}
