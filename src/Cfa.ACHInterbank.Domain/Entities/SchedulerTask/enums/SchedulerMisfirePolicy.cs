namespace Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;

/// <summary>Política persistida que determina cómo se trata una ejecución perdida por misfire.</summary>
public enum SchedulerMisfirePolicy
{
    /// <summary>Omite la ejecución perdida y continúa con la siguiente programación.</summary>
    DoNothing = 0,

    /// <summary>Solicita ejecutar una vez al recuperarse y después continuar con la programación normal.</summary>
    FireAndProceed = 1
}
