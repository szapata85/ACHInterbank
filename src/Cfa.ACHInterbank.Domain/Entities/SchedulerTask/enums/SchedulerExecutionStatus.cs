namespace Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;

/// <summary>Estado persistido de una ejecución administrada por el scheduler.</summary>
public enum SchedulerExecutionStatus
{
    /// <summary>La ejecución fue registrada y está pendiente de ejecución.</summary>
    Pending = 0,

    /// <summary>La ejecución está en curso.</summary>
    Running = 1,

    /// <summary>La ejecución finalizó satisfactoriamente.</summary>
    Succeeded = 2,

    /// <summary>La ejecución finalizó con fallo.</summary>
    Failed = 3,

    /// <summary>La ejecución corresponde a una recuperación completada.</summary>
    Recovered = 4,

    /// <summary>La ejecución fue omitida.</summary>
    Skipped = 5,

    /// <summary>La ejecución fue rechazada.</summary>
    Rejected = 6,

    /// <summary>La ejecución quedó registrada como misfire.</summary>
    Misfired = 7
}
