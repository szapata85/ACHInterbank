using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Authorize]
[Route("[controller]")]
public class MaintenanceController : ControllerBase
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;
    private readonly IIncomingProcTransaccionesE2eScenarioSetupService _procTransaccionesSetup;

    public MaintenanceController(
        IServiceProvider services,
        IConfiguration configuration,
        IIncomingProcTransaccionesE2eScenarioSetupService procTransaccionesSetup)
    {
        _services = services;
        _configuration = configuration;
        _procTransaccionesSetup = procTransaccionesSetup;
    }

    [HttpPost("seed")]
    [Authorize(Policy = P1Policies.MaintenanceSeed)]
    public async Task<IActionResult> RunDbInitializer()
    {
        var provider = _configuration.GetValue<string>("Database:Provider") ?? "SqlServer";
        var connectionStringName = provider.Trim().ToLowerInvariant() switch
        {
            "postgres" or "postgresql" or "npgsql" => "PostgresConnection",
            _ => "SqlConnection"
        };

        var connectionString = _configuration.GetConnectionString(connectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return BadRequest(new
            {
                Message = $"No se encontró ConnectionStrings:{connectionStringName}.",
                Date = DateTime.UtcNow
            });
        }

        await DbInitializer.SeedAllAsync(_services);

        return Ok(new
        {
            Message = "Seeding ejecutado correctamente desde Controller",
            Date = DateTime.UtcNow
        });
    }

    [HttpGet("proc-transacciones-e2e/readiness")]
    [Authorize(Policy = P1Policies.MaintenanceSeed)]
    public async Task<IActionResult> InspectProcTransaccionesE2e(
        [FromQuery] DateTime operationalDate,
        [FromQuery] int cycleNumber = 1,
        CancellationToken ct = default)
    {
        try
        {
            return Ok(await _procTransaccionesSetup.InspectAsync(
                new IncomingProcTransaccionesE2eScenarioRequest
                {
                    OperationalDate = operationalDate,
                    CycleNumber = cycleNumber
                },
                ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("proc-transacciones-e2e/setup")]
    [Authorize(Policy = P1Policies.MaintenanceSeed)]
    public async Task<IActionResult> SetupProcTransaccionesE2e(
        [FromBody] IncomingProcTransaccionesE2eScenarioRequest request,
        CancellationToken ct = default)
    {
        try
        {
            return Ok(await _procTransaccionesSetup.EnsureAsync(request, ct));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
