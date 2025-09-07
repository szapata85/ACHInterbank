using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

public class MaintenanceController : Controller
{
    private readonly IServiceProvider _services;

    public MaintenanceController(IServiceProvider services)
    {
        _services = services;
    }

    [HttpPost("seed")]
    public async Task<IActionResult> RunDbInitializer()
    {
        await DbInitializer.SeedAllAsync(_services);

        return Ok(new
        {
            Message = "Seeding ejecutado correctamente desde Controller",
            Date = DateTime.UtcNow
        });
    }
}
