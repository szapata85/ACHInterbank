using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// I01-CENIT: parametrización sin conexión al procesador, generador, transporte o tareas.
/// Reutiliza una fila de ClearingHouseConfigs exclusiva de CENIT y AuditLogs de AchDbContext.
/// </summary>
[Scoped]
public sealed class CenitAutomaticReturnSettingsService(AchDbContext context)
    : ICenitAutomaticReturnSettingsService
{
    public const string Disabled = "DESACTIVADO";
    public const string WithApproval = "CON_APROBACION";
    public const string Automatic = "AUTOMATICO";

    public async Task<CenitAutomaticReturnSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        var config = await LoadOwnConfigAsync(tracking: false, cancellationToken);
        return new(config.AutomaticReturnMode, ProcessingConnected: false);
    }

    public async Task<CenitAutomaticReturnSettings> UpdateAsync(
        UpdateCenitAutomaticReturnSettings request, CancellationToken cancellationToken = default)
    {
        if (request is null || !IsValid(request.Mode) || !IsValid(request.ExpectedMode))
            throw new ArgumentException(
                "El modo y el modo anterior deben ser DESACTIVADO, CON_APROBACION o AUTOMATICO.", nameof(request));

        var config = await LoadOwnConfigAsync(tracking: true, cancellationToken);
        if (config.AutomaticReturnMode != request.ExpectedMode)
            throw new DbUpdateConcurrencyException("La configuración CENIT cambió desde la última consulta.");

        if (config.AutomaticReturnMode != request.Mode)
        {
            config.AutomaticReturnMode = request.Mode;
            // SaveChanges registra la modificación en AuditLogs; AutomaticReturnMode es concurrency token EF.
            await context.SaveChangesAsync(cancellationToken);
        }

        // I01 no conecta la selección con ningún procesamiento, aunque sea AUTOMATICO.
        return new(config.AutomaticReturnMode, ProcessingConnected: false);
    }

    private async Task<ClearingHouseConfig> LoadOwnConfigAsync(bool tracking, CancellationToken ct)
    {
        IQueryable<ClearingHouse> houses = tracking ? context.ClearingHouses : context.ClearingHouses.AsNoTracking();
        var house = await houses.SingleOrDefaultAsync(
            item => item.Code == RegulatoryCycleScheduleCatalog.CenitCode, ct);
        if (house is null)
            throw new InvalidOperationException("No existe una cámara CENIT configurada.");

        IQueryable<ClearingHouseConfig> configs = tracking
            ? context.ClearingHouseConfigs : context.ClearingHouseConfigs.AsNoTracking();
        var config = await configs.SingleOrDefaultAsync(item => item.ClearingHouseId == house.Id, ct);
        if (config is null || config.Id != house.ClearingHouseId ||
            await context.ClearingHouses.AsNoTracking().AnyAsync(
                item => item.Id != house.Id && item.ClearingHouseId == config.Id, ct))
            throw new InvalidOperationException("La configuración exclusiva de CENIT no está disponible.");

        if (!IsValid(config.AutomaticReturnMode))
            throw new InvalidOperationException("La configuración CENIT contiene un modo no permitido.");

        return config;
    }

    private static bool IsValid(string? mode) => mode is Disabled or WithApproval or Automatic;
}
