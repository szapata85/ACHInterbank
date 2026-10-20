using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

public abstract class ReportsClearingHouseControllerBase : ControllerBase
{
    private readonly IClearingHouseService _clearingHouseService;

    protected ReportsClearingHouseControllerBase(IClearingHouseService clearingHouseService)
    {
        _clearingHouseService = clearingHouseService;
    }

    protected async Task<IActionResult?> ValidateClearingHouseIdAsync(int? clearingHouseId, CancellationToken ct)
    {
        if (!clearingHouseId.HasValue)
        {
            return null;
        }

        if (clearingHouseId.Value <= 0)
        {
            return BadRequest(new { message = "ClearingHouseId debe ser mayor a cero." });
        }

        var exists = await _clearingHouseService.GetByIdAsync(clearingHouseId.Value, ct);
        if (exists is null)
        {
            return BadRequest(new { message = "La cámara seleccionada no existe." });
        }

        return null;
    }
}
