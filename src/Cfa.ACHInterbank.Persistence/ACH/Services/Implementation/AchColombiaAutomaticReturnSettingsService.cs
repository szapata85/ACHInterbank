using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// I01/I06-ACH: configura solo ACH Colombia y publica si la ejecución de archivos
/// está conectada bajo modo AUTOMATICO y habilitación técnica de despliegue.
/// </summary>
[Scoped]
public sealed class AchColombiaAutomaticReturnSettingsService(AchDbContext context, IConfiguration? configuration = null)
    : IAchColombiaAutomaticReturnSettingsService
{
    public const string Disabled = "DESACTIVADO";
    public const string WithApproval = "CON_APROBACION";
    public const string Automatic = "AUTOMATICO";

    public async Task<AchColombiaAutomaticReturnSettings> GetAsync(CancellationToken cancellationToken = default)
    {
        var config = await LoadConfigAsync(tracking: false, cancellationToken);
        return new AchColombiaAutomaticReturnSettings(config.AutomaticReturnMode, ProcessingConnected: IsExecutionEnabled(config.AutomaticReturnMode));
    }

    public async Task<AchColombiaAutomaticReturnSettings> UpdateAsync(
        UpdateAchColombiaAutomaticReturnSettings request, CancellationToken cancellationToken = default)
    {
        if (request is null || !IsValid(request.Mode))
            throw new ArgumentException("El modo debe ser DESACTIVADO, CON_APROBACION o AUTOMATICO.", nameof(request));

        var config = await LoadConfigAsync(tracking: true, cancellationToken);
        if (config.AutomaticReturnMode != request.Mode)
        {
            config.AutomaticReturnMode = request.Mode;
            // AchDbContext registra automáticamente el antes/después, usuario y fecha en AuditLogs.
            await context.SaveChangesAsync(cancellationToken);
        }

        // I06 solo refleja la conexión interna; no habilita transmisión ni aceptación de cámara.
        return new AchColombiaAutomaticReturnSettings(config.AutomaticReturnMode, ProcessingConnected: IsExecutionEnabled(config.AutomaticReturnMode));
    }

    private async Task<ClearingHouseConfig> LoadConfigAsync(bool tracking, CancellationToken cancellationToken)
    {
        IQueryable<ClearingHouse> houses = tracking ? context.ClearingHouses : context.ClearingHouses.AsNoTracking();
        var house = await houses.SingleOrDefaultAsync(
            item => item.Code == RegulatoryCycleScheduleCatalog.AchColombiaCode,
            cancellationToken);
        if (house is null)
            throw new InvalidOperationException("No existe la cámara ACH Colombia configurada.");

        IQueryable<ClearingHouseConfig> configs = tracking ? context.ClearingHouseConfigs : context.ClearingHouseConfigs.AsNoTracking();
        var config = await configs.SingleOrDefaultAsync(item => item.ClearingHouseId == house.Id, cancellationToken);
        // Evitar modificar una configuración compartida o un fallback de otra cámara.
        if (config is null || config.Id != house.ClearingHouseId)
            throw new InvalidOperationException("La configuración propia de ACH Colombia no está disponible.");

        return config;
    }

    private bool IsExecutionEnabled(string mode) => mode == Automatic
        && configuration?.GetValue<bool>("AchCol:EnableAutomaticReturnFileGeneration") == true;

    private static bool IsValid(string? mode) => mode is Disabled or WithApproval or Automatic;
}
