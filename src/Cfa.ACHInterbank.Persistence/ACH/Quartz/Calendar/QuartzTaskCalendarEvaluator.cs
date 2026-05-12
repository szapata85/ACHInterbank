using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.enums;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Cfa.ACHInterbank.Persistence.ACH.Quartz.Calendar;

public sealed record QuartzCalendarEvaluation(
    bool ShouldRun,
    bool ShouldSkip,
    bool ShouldShift,
    string Reason,
    DateOnly LocalDate,
    DateTimeOffset? NextBusinessDateTime);

public class QuartzTaskCalendarEvaluator
{
    public const string DefaultTimeZoneId = "America/Bogota";

    public TimeZoneInfo ResolveTimeZone(string? timeZoneId, ILogger? logger = null)
    {
        var candidate = string.IsNullOrWhiteSpace(timeZoneId) ? DefaultTimeZoneId : timeZoneId;

        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(candidate);
        }
        catch (TimeZoneNotFoundException)
        {
            logger?.LogWarning("TimeZoneId inválido '{TimeZoneId}'. Se usará fallback {FallbackTimeZoneId}.", candidate, DefaultTimeZoneId);
            return TimeZoneInfo.FindSystemTimeZoneById(DefaultTimeZoneId);
        }
        catch (InvalidTimeZoneException)
        {
            logger?.LogWarning("TimeZoneId corrupto '{TimeZoneId}'. Se usará fallback {FallbackTimeZoneId}.", candidate, DefaultTimeZoneId);
            return TimeZoneInfo.FindSystemTimeZoneById(DefaultTimeZoneId);
        }
    }

    public DateOnly GetTaskLocalDate(DateTimeOffset utcNow, TimeZoneInfo timeZone)
    {
        var local = TimeZoneInfo.ConvertTime(utcNow, timeZone);
        return DateOnly.FromDateTime(local.DateTime);
    }

    public bool IsHoliday(AchDbContext db, DateOnly date)
        => db.BankHolidays.AsNoTracking().Any(h => h.Date == date);

    public DateTimeOffset GetNextBusinessDateTime(AchDbContext db, DateOnly fromDate, TimeOnly? preferredTime, TimeZoneInfo timeZone)
    {
        var next = fromDate.AddDays(1);
        while (next.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday || IsHoliday(db, next))
        {
            next = next.AddDays(1);
        }

        var time = preferredTime ?? new TimeOnly(9, 0);
        var localDateTime = next.ToDateTime(time, DateTimeKind.Unspecified);
        var offset = timeZone.GetUtcOffset(localDateTime);
        return new DateTimeOffset(localDateTime, offset).ToUniversalTime();
    }

    public QuartzCalendarEvaluation Evaluate(TaskDefinition task, AchDbContext db, DateTimeOffset utcNow, ILogger? logger = null)
    {
        var timeZone = ResolveTimeZone(task.TimeZoneId, logger);
        var localDate = GetTaskLocalDate(utcNow, timeZone);
        var isWeekend = localDate.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        var isHoliday = IsHoliday(db, localDate);

        return task.CalendarPolicy switch
        {
            CalendarPolicyEnum.OnlyBusinessDays when isWeekend || isHoliday
                => new(false, true, false, "Saltada por política OnlyBusinessDays.", localDate, null),

            CalendarPolicyEnum.OnlyWeekends when !isWeekend
                => new(false, true, false, "Saltada por política OnlyWeekends.", localDate, null),

            CalendarPolicyEnum.SkipHolidays when isHoliday
                => new(false, true, false, "Saltada por política SkipHolidays.", localDate, null),

            CalendarPolicyEnum.ShiftToNextBusinessDay when isWeekend || isHoliday
                => new(false, true, true,
                    "Saltada por política ShiftToNextBusinessDay; se ejecutará en el próximo disparo hábil.",
                    localDate,
                    GetNextBusinessDateTime(db, localDate, task.TimeOfDay, timeZone)),

            _ => new(true, false, false, "CalendarPolicy permite ejecutar.", localDate, null)
        };
    }
}
