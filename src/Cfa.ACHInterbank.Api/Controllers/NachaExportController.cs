using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class NachaExportController : ControllerBase
{
    private readonly INachaFileBuilder _nachaBuilder;

    public NachaExportController(INachaFileBuilder nachaBuilder)
    {
        _nachaBuilder = nachaBuilder;
    }

    /// <summary>
    /// Genera y descarga un archivo NACHA-M (106 caracteres por registro)
    /// para los lotes indicados.
    /// </summary>
    /// <param name="batchIds">Lista de Ids de lotes ACH a exportar</param>
    [HttpPost("export")]
    public async Task<IActionResult> Export(
        [FromBody] List<int> batchIds,
        CancellationToken ct)
    {
        if (batchIds == null || batchIds.Count == 0)
            return BadRequest("Debe proporcionar al menos un Id de lote.");

        // Construye el archivo en una sola cadena sin saltos de línea
        string nachaContent = await _nachaBuilder.BuildNachaFileAsync(batchIds, ct);

        // Nombre dinámico con fecha/hora
        string fileName = $"NACHA_{DateTime.UtcNow:yyyyMMdd_HHmmss}.txt";

        // Devuelve como archivo de texto plano
        return File(
            Encoding.ASCII.GetBytes(nachaContent),
            "text/plain",
            fileName);
    }
}
