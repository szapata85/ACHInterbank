namespace Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;

/// <summary>Política de calendario persistida para una definición de tarea.</summary>
public enum CalendarPolicyEnum
{
    /// <summary>No aplica una política de calendario.</summary>
    IgnoreCalendar,

    /// <summary>Valor de política que identifica únicamente días hábiles.</summary>
    OnlyBusinessDays,

    /// <summary>Valor de política que identifica únicamente fines de semana.</summary>
    OnlyWeekends,

    /// <summary>Valor de política que identifica omisión de festivos.</summary>
    SkipHolidays,

    /// <summary>Valor de política que identifica desplazamiento al siguiente día hábil.</summary>
    ShiftToNextBusinessDay
}
