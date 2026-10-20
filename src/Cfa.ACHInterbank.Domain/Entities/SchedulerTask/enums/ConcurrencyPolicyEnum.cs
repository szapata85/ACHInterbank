namespace Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;

/// <summary>Política de concurrencia persistida para una definición de tarea.</summary>
public enum ConcurrencyPolicyEnum
{
    /// <summary>Permite ejecuciones paralelas.</summary>
    AllowParallel,

    /// <summary>Identifica la política que omite una nueva ejecución cuando ya existe una en curso.</summary>
    SkipIfRunning,

    /// <summary>Identifica la política de encolamiento.</summary>
    Queue
}
