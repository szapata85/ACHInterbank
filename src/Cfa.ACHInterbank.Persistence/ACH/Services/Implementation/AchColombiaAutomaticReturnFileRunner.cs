using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// I06-ACH: ejecución acotada, después de guardar respuestas del core.
/// Nunca transmite, cifra ni crea operaciones CENIT. La puerta técnica está apagada
/// salvo habilitación expresa del despliegue; el modo se consulta de la BD en cada corrida.
/// </summary>
internal sealed class AchColombiaAutomaticReturnFileRunner(
    AchDbContext context, IAchColombiaReturnReviewService review)
{
    internal async Task<int> RunAsync(int maxItems, CancellationToken ct)
    {
        if (!await IsAutomaticModeAsync(ct)) return 0;

        var ids = await context.AchColombiaReturnPreparations.AsNoTracking()
            .Where(p => (p.Status == AchColombiaReturnPreparationService.Prepared
                    || p.Status == AchColombiaReturnReviewService.Approved)
                && p.OriginalTransaction.AchCycle.ClearingHouse != null
                && p.OriginalTransaction.AchCycle.ClearingHouse.Code
                    == RegulatoryCycleScheduleCatalog.AchColombiaCode)
            .OrderBy(p => p.PreparedAtUtc).ThenBy(p => p.Id)
            .Select(p => p.Id)
            .Take(Math.Clamp(maxItems, 1, 10))
            .ToListAsync(ct);
        var completed = 0;
        foreach (var id in ids)
        {
            // Revalidación de apagado inmediato entre operaciones.
            if (!await IsAutomaticModeAsync(ct)) break;
            try
            {
                await review.GenerateAutomaticallyAsync(id, ct);
                completed++;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Una falla de generación nunca puede modificar el resultado core.
                // Parar el lote: el DbContext puede tener estado pendiente y se debe
                // consultar la candidata/archivo antes de reintentar.
                System.Diagnostics.Trace.TraceWarning(
                    "I06-ACH: generación suspendida en candidata {0}; causa {1}. " +
                    "Consultar la candidata y el registro de archivo antes de reintentar.",
                    id, ex.GetType().Name);
                break;
            }
        }
        return completed;
    }

    private Task<bool> IsAutomaticModeAsync(CancellationToken ct) =>
        context.ClearingHouseConfigs.AsNoTracking().AnyAsync(c =>
            c.AutomaticReturnMode == AchColombiaAutomaticReturnSettingsService.Automatic
            && context.ClearingHouses.Any(h => h.Id == c.ClearingHouseId
                && h.Code == RegulatoryCycleScheduleCatalog.AchColombiaCode), ct);
}
