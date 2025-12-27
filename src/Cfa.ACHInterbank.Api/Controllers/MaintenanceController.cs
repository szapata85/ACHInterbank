using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

public class MaintenanceController : Controller
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _configuration;

    public MaintenanceController(IServiceProvider services, IConfiguration configuration)
    {
        _services = services;
        _configuration = configuration;
    }

    [HttpPost("seed")]
    public async Task<IActionResult> RunDbInitializer()
    {
        var allowSeed = _configuration.GetValue("Database:AllowSeed", !IsRunningInContainer());
        if (!allowSeed)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new
            {
                Message = "Seeding deshabilitado. Configure Database__AllowSeed=true para habilitarlo.",
                Date = DateTime.UtcNow
            });
        }

        var connectionString = _configuration.GetConnectionString("SqlConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return BadRequest(new
            {
                Message = "No se encontró ConnectionStrings:SqlConnection.",
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

    private static bool IsRunningInContainer()
    {
        return string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase);
    }
}
