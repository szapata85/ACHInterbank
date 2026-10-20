using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.ACH.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("return-reasons")]
[Authorize]
public class ReturnReasonsController : ControllerBase
{
    private readonly IReturnReasonService _service;

    public ReturnReasonsController(IReturnReasonService service)
    {
        _service = service;
    }

    /// <summary>
    /// Consulta el catálogo persistido de causales de devolución.
    /// </summary>
    /// <param name="onlyForReturn">Si es true, devuelve únicamente causales marcadas para devolución y excluye DEV14; si es false, devuelve el catálogo completo.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar causales de devolución")]
    [EndpointDescription("Devuelve las causales persistidas ordenadas por Code. Con onlyForReturn=true filtra por IsForReturn y excluye el código DEV14; con el valor predeterminado false devuelve el catálogo completo.")]
    [HttpGet]
    [Authorize(Policy = "CanReadAch")]
    [ProducesResponseType(typeof(IEnumerable<ReturnReasonDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Causales de devolución que cumplen el filtro solicitado, ordenadas por código.")]
    public async Task<IActionResult> GetAll([FromQuery] bool onlyForReturn = false, CancellationToken ct = default)
        => Ok(await _service.GetAllAsync(onlyForReturn, ct));
}
