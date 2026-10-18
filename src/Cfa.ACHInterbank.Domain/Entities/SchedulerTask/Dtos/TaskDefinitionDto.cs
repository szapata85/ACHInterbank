using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;

namespace Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Dtos;

/// <summary>Contrato persistido para consultar, crear o actualizar una definición de tarea del scheduler.</summary>
public class TaskDefinitionDto
{
    /// <summary>Identificador numérico persistido de la definición.</summary>
    public int Id { get; set; }

    /// <summary>Código persistido de la tarea; el servicio recorta espacios al crear o actualizar.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Nombre persistido de la tarea; el servicio recorta espacios al crear o actualizar.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Descripción opcional; al persistir, texto vacío o compuesto solo por espacios se convierte en null.</summary>
    public string? Description { get; set; }

    /// <summary>Estado persistido de habilitación de la tarea.</summary>
    public TaskStatusEnum Status { get; set; } = TaskStatusEnum.Enabled;

    /// <summary>Política de calendario persistida para la definición.</summary>
    public CalendarPolicyEnum CalendarPolicy { get; set; } = CalendarPolicyEnum.OnlyBusinessDays;

    /// <summary>Identificador opcional de zona horaria; al persistir, texto vacío o compuesto solo por espacios se convierte en null.</summary>
    public string? TimeZoneId { get; set; }

    /// <summary>Política de concurrencia persistida para la tarea.</summary>
    public ConcurrencyPolicyEnum ConcurrencyPolicy { get; set; } = ConcurrencyPolicyEnum.SkipIfRunning;

    /// <summary>Indica si la definición persiste la opción de reintentar ante fallo.</summary>
    public bool RetryOnFailure { get; set; } = true;

    /// <summary>Número máximo opcional de reintentos persistido por la definición.</summary>
    public int? MaxRetries { get; set; }

    /// <summary>Backoff persistido entre reintentos, expresado en segundos.</summary>
    public int RetryBackoffSeconds { get; set; } = 60;

    /// <summary>Política de misfire persistida para la definición.</summary>
    public SchedulerMisfirePolicy MisfirePolicy { get; set; } = SchedulerMisfirePolicy.DoNothing;

    /// <summary>Indica si la definición solicita recuperación del job.</summary>
    public bool RequestsRecovery { get; set; }

    /// <summary>Indica si la definición habilita ejecución manual.</summary>
    public bool ManualExecutionEnabled { get; set; }

    /// <summary>Indica si la definición está marcada como pausada.</summary>
    public bool Paused { get; set; }

    /// <summary>Tipo de periodicidad persistido para la definición.</summary>
    public PeriodicityTypeEnum PeriodicityType { get; set; }

    /// <summary>Valor N opcional asociado a la periodicidad persistida.</summary>
    public int? N { get; set; }

    /// <summary>Minuto opcional asociado a la periodicidad persistida.</summary>
    public int? Minute { get; set; }

    /// <summary>Hora opcional representada como texto. El servicio intenta convertirla a TimeOnly y persiste null si no puede interpretarla.</summary>
    public string? TimeOfDay { get; set; }

    /// <summary>Día de la semana opcional asociado a la periodicidad.</summary>
    public DayOfWeek? WeeklyDay { get; set; }

    /// <summary>Día del mes opcional asociado a la periodicidad.</summary>
    public int? MonthDay { get; set; }

    /// <summary>Expresión cron opcional persistida sin validación en este servicio; texto vacío o compuesto solo por espacios se convierte en null.</summary>
    public string? CronExpression { get; set; }

    /// <summary>Instante opcional de inicio persistido para la definición.</summary>
    public DateTimeOffset? StartAt { get; set; }

    /// <summary>Instante opcional de fin persistido para la definición.</summary>
    public DateTimeOffset? EndAt { get; set; }

    /// <summary>Parámetros persistidos de la definición. El servicio descarta elementos con Key vacío al crear o actualizar.</summary>
    public List<TaskParameterDto> Parameters { get; set; } = [];
}
