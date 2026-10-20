using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/ach/nacha/config-profiles")]
[Authorize]
public sealed class NachaConfigProfilesReadOnlyController : ControllerBase
{
    private readonly INachaConfigProfileReadModelService _service;

    public NachaConfigProfilesReadOnlyController(INachaConfigProfileReadModelService service)
    {
        _service = service;
    }

    /// <summary>Consulta el resumen de configuración NACHA para el dashboard de lectura.</summary>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar dashboard de configuración NACHA")]
    [EndpointDescription("Resume los perfiles oficiales persistidos: total de perfiles, publicados y vigentes según DateTime.UtcNow, totales de variantes/campos y catálogos distintos de cámaras y tipos de registro. Si no existen perfiles, Warnings contiene el aviso definido por el servicio.")]
    [HttpGet("dashboard")]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(NachaConfigProfilesDashboardReadModel), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Resumen de los perfiles de configuración NACHA persistidos y sus conteos derivados.")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
        => Ok(await _service.GetDashboardAsync(cancellationToken));

    /// <summary>Lista perfiles NACHA mediante el read model oficial.</summary>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Listar perfiles NACHA de solo lectura")]
    [EndpointDescription("Devuelve los perfiles ordenados por código de cámara y ProfileCode. IsPublished refleja Status=PUBLICADO y IsCurrent se calcula comparando EffectiveFrom/EffectiveTo con DateTime.UtcNow; la colección puede estar vacía.")]
    [HttpGet]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(IReadOnlyList<NachaConfigProfileReadModel>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Colección, posiblemente vacía, de perfiles NACHA proyectados por el read model oficial.")]
    public async Task<IActionResult> GetProfiles(CancellationToken cancellationToken)
        => Ok(await _service.GetProfilesAsync(cancellationToken));

    /// <summary>Consulta un perfil NACHA de solo lectura por identificador.</summary>
    /// <param name="id">Identificador entero del perfil.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar perfil NACHA de solo lectura por id")]
    [EndpointDescription("Obtiene el perfil identificado por id junto con sus variantes y campos proyectados por el read model. Devuelve 404 sin body cuando el perfil no existe.")]
    [HttpGet("{id:int}")]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(NachaConfigProfileDetailReadModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Detalle de solo lectura del perfil NACHA solicitado, incluidas variantes y campos.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe un perfil NACHA con el identificador indicado.")]
    public async Task<IActionResult> GetProfile(int id, CancellationToken cancellationToken)
    {
        var profile = await _service.GetProfileAsync(id, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    /// <summary>Consulta un perfil NACHA de solo lectura por código.</summary>
    /// <param name="profileCode">Código del perfil; el servicio elimina espacios exteriores antes de buscar una coincidencia exacta.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar perfil NACHA de solo lectura por código")]
    [EndpointDescription("Recorta profileCode y busca una coincidencia exacta con ProfileCode. Devuelve el mismo detalle de solo lectura que la consulta por id o 404 sin body cuando no existe una coincidencia.")]
    [HttpGet("by-code/{profileCode}")]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(NachaConfigProfileDetailReadModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Detalle de solo lectura del perfil NACHA cuyo código coincide con el valor normalizado solicitado.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe un perfil NACHA con el código indicado después de eliminar espacios exteriores.")]
    public async Task<IActionResult> GetProfileByCode(string profileCode, CancellationToken cancellationToken)
    {
        var profile = await _service.GetProfileByCodeAsync(profileCode, cancellationToken);
        return profile is null ? NotFound() : Ok(profile);
    }

    /// <summary>Lista las variantes de layout de un perfil NACHA.</summary>
    /// <param name="id">Identificador entero del perfil.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Listar variantes de layout de un perfil NACHA")]
    [EndpointDescription("Devuelve las variantes cuyo ProfileId coincide con id, ordenadas por tipo de registro y prioridad. IsActive solo es true cuando la variante es la predeterminada del registro y su estado es PUBLICADO; BlockingFactor se obtiene del campo BLOCKINGFACTOR cuando su valor constante es entero y, en otro caso, el servicio usa 10. La ausencia de variantes se representa como colección vacía.")]
    [HttpGet("{id:int}/variants")]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(IReadOnlyList<NachaConfigProfileVariantReadModel>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Colección, posiblemente vacía, de variantes de layout del perfil solicitado.")]
    public async Task<IActionResult> GetVariants(int id, CancellationToken cancellationToken)
        => Ok(await _service.GetVariantsAsync(id, cancellationToken));

    /// <summary>Lista los campos de layout de un perfil NACHA.</summary>
    /// <param name="id">Identificador entero del perfil.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Listar campos de layout de un perfil NACHA")]
    [EndpointDescription("Devuelve los campos pertenecientes a variantes del perfil indicado, ordenados por tipo de registro y posición inicial. El read model deriva EndPosition, dirección/carácter de padding, marca de campo computado y marca de total de control a partir de la configuración persistida. La ausencia de campos se representa como colección vacía.")]
    [HttpGet("{id:int}/fields")]
    [Authorize(Policy = P1Policies.NachaRead)]
    [ProducesResponseType(typeof(IReadOnlyList<NachaConfigProfileFieldReadModel>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Colección, posiblemente vacía, de campos de layout proyectados para el perfil solicitado.")]
    public async Task<IActionResult> GetFields(int id, CancellationToken cancellationToken)
        => Ok(await _service.GetFieldsAsync(id, cancellationToken));
}
