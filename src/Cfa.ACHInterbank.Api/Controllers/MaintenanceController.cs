using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Authorize]
[Route("[controller]")]
public class MaintenanceController : ControllerBase
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;

    public MaintenanceController(IServiceProvider services, IConfiguration configuration)
    {
        _services = services;
        _configuration = configuration;
    }
    /// <summary>
    /// Endpoint de la API ACH Interbank.
    /// </summary>

    [HttpPost("seed")]
    [Authorize(Policy = "CanManageAch")]
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
}
