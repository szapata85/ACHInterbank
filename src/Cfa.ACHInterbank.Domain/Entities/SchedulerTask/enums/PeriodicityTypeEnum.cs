namespace Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;

/// <summary>Tipo de periodicidad persistido para una definición de tarea.</summary>
public enum PeriodicityTypeEnum
{
    /// <summary>Ejecución única.</summary>
    Once,

    /// <summary>Periodicidad expresada cada N minutos.</summary>
    EveryNMinutes,

    /// <summary>Periodicidad horaria asociada a un minuto.</summary>
    HourlyAtMinute,

    /// <summary>Periodicidad diaria asociada a una hora.</summary>
    DailyAtTime,

    /// <summary>Periodicidad semanal.</summary>
    Weekly,

    /// <summary>Periodicidad mensual.</summary>
    Monthly,

    /// <summary>Periodicidad representada mediante expresión cron.</summary>
    Cron,

    /// <summary>Periodicidad anual.</summary>
    Yearly
}
