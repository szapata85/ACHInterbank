using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Application.Security.Dtos;
using Cfa.ACHInterbank.Application.Security.Exceptions;
using Cfa.ACHInterbank.Application.Security.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/users/soap-integrations")]
[Authorize]
public class SoapIntegrationSettingsController : ControllerBase
{
    private readonly ISoapIntegrationSettingsService _service;

    public SoapIntegrationSettingsController(ISoapIntegrationSettingsService service)
    {
        _service = service;
    }

    /// <summary>Consulta la configuración SOAP persistida para integraciones.</summary>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar configuración SOAP de integraciones")]
    [EndpointDescription("Obtiene mappings SOAP para administración, con destino/acción sanitizados, Revision y capabilities backend. La respuesta incorpora ProcTransaccionesEffectiveSettings calculado a partir del mapping Proc_Transacciones y de la evaluación de readiness disponible.")]
    [HttpGet]
    [Authorize(Policy = P1Policies.IntegrationsRead)]
    [ProducesResponseType(typeof(SoapIntegrationSettingsDto), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Configuración SOAP sanitizada para administración con estado y capabilities calculadas por backend.")]
    public async Task<ActionResult<SoapIntegrationSettingsDto>> GetAsync(CancellationToken cancellationToken)
    {
        var settings = await _service.GetAdministrationAsync(cancellationToken);
        return Ok(settings);
    }

    /// <summary>Evalúa sin persistir un cambio de conexión antes de confirmar envío real.</summary>
    [EndpointSummary("Previsualizar cambio de conexión SOAP")]
    [EndpointDescription("Resuelve en backend el servicio y estado vigente, valida Revision y devuelve destino/acción sanitizados para la confirmación contextual. No persiste ni invoca SOAP.")]
    [HttpPost("preview-change")]
    [Authorize(Policy = P1Policies.IntegrationConnectionsManage)]
    [ProducesResponseType(typeof(SoapIntegrationSettingsChangePreviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<SoapIntegrationSettingsChangePreviewDto>> PreviewChangeAsync(
        [FromBody] SoapIntegrationSettingsChangePreviewRequestDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await _service.PreviewChangeAsync(request, cancellationToken));
        }
        catch (SoapIntegrationSettingsRevisionConflictException ex)
        {
            return RevisionConflict(ex);
        }
    }

    /// <summary>Guarda la configuración SOAP de integraciones.</summary>
    /// <param name="request">Colecciones de mappings SOAP y ExpectedRevision que se normalizan y persisten.</param>
    /// <param name="cancellationToken">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Guardar configuración SOAP de integraciones")]
    [EndpointDescription("Exige ExpectedRevision, normaliza y persiste WscfaachMappings y WsAxonRespuestaTransaccionesMappings. Valores sanitizados devueltos previamente se reconocen como placeholders y no sustituyen el valor persistido. ProcTransaccionesEffectiveSettings se recalcula para la respuesta.")]
    [HttpPut]
    [Authorize(Policy = P1Policies.IntegrationConnectionsManage)]
    [ProducesResponseType(typeof(SoapIntegrationSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Configuración SOAP persistida y respuesta sanitizada recalculada desde backend.")]
    public async Task<ActionResult<SoapIntegrationSettingsDto>> SaveAsync(
        [FromBody] SoapIntegrationSettingsDto request,
        CancellationToken cancellationToken)
    {
        try
        {
            var settings = await _service.SaveAsync(request, cancellationToken);
            return Ok(settings);
        }
        catch (SoapIntegrationSettingsRevisionConflictException ex)
        {
            return RevisionConflict(ex);
        }
    }

    private ConflictObjectResult RevisionConflict(SoapIntegrationSettingsRevisionConflictException ex)
    {
        var problem = new ProblemDetails
        {
            Title = "SOAP_SETTINGS_REVISION_CONFLICT",
            Detail = "La configuración cambió desde la última lectura. Recarga la información antes de intentar guardar nuevamente.",
            Status = StatusCodes.Status409Conflict
        };
        problem.Extensions["expectedRevision"] = ex.ExpectedRevision;
        problem.Extensions["currentRevision"] = ex.CurrentRevision;
        return Conflict(problem);
    }
}
