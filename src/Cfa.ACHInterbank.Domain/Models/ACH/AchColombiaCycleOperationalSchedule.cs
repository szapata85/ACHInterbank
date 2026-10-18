using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public enum AchColombiaOutboundExecutionStrategy
{
    ExactTime = 1,
    Window = 2
}

/// <summary>
/// Programación operacional de intercambio de archivos ACH Colombia asociada a una
/// configuración regulatoria de ciclo. No modifica ni reemplaza la agenda regulatoria.
/// </summary>
public sealed class AchColombiaCycleOperationalSchedule : AuditableEntity
{
    public int Id { get; set; }

    public int ClearingHouseId { get; set; }
    public ClearingHouse ClearingHouse { get; set; } = null!;

    public int ClearingHouseCycleConfigId { get; set; }
    public ClearingHouseCycleConfig ClearingHouseCycleConfig { get; set; } = null!;

    public bool AutomaticOutboundEnabled { get; set; }
    public TimeSpan? AutomaticOutboundStartTime { get; set; }
    public TimeSpan? AutomaticOutboundLatestStartTime { get; set; }
    public AchColombiaOutboundExecutionStrategy OutboundExecutionStrategy { get; set; }
        = AchColombiaOutboundExecutionStrategy.Window;

    public string EffectiveTimeZoneId { get; set; } = null!;
    public bool IsActive { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
