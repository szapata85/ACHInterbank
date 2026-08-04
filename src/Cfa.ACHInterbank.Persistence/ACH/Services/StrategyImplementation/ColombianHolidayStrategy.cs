using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.StrategyImplementation;

public sealed class ColombianHolidayStrategy : IHolidayStrategy
{
    public List<BankHolidayModel> GenerateHolidays(int year)
    {
        var easterSunday = GetEasterSunday(year);
        var holidays = new List<BankHolidayModel>(year >= 2026 ? 19 : 18);

        AddFixed(holidays, "CO_NEW_YEAR", new(year, 1, 1), "Año Nuevo");
        AddFixed(holidays, "CO_LABOUR_DAY", new(year, 5, 1), "Día del Trabajo");
        AddFixed(holidays, "CO_INDEPENDENCE_DAY", new(year, 7, 20), "Día de la Independencia");
        AddFixed(holidays, "CO_BOYACA_BATTLE", new(year, 8, 7), "Batalla de Boyacá");
        AddFixed(holidays, "CO_IMMACULATE_CONCEPTION", new(year, 12, 8), "Inmaculada Concepción");
        AddFixed(holidays, "CO_CHRISTMAS", new(year, 12, 25), "Navidad");

        AddEmiliani(holidays, "CO_EPIPHANY", new(year, 1, 6), "Día de los Reyes Magos");
        AddEmiliani(holidays, "CO_SAINT_JOSEPH", new(year, 3, 19), "Día de San José");
        AddEmiliani(holidays, "CO_SAINTS_PETER_PAUL", new(year, 6, 29), "San Pedro y San Pablo");
        AddEmiliani(holidays, "CO_ASSUMPTION", new(year, 8, 15), "Asunción de la Virgen");
        AddEmiliani(holidays, "CO_ETHNIC_CULTURAL_DIVERSITY", new(year, 10, 12), "Día de la Diversidad Étnica y Cultural");
        AddEmiliani(holidays, "CO_ALL_SAINTS", new(year, 11, 1), "Día de Todos los Santos");
        AddEmiliani(holidays, "CO_CARTAGENA_INDEPENDENCE", new(year, 11, 11), "Independencia de Cartagena");

        AddEaster(holidays, "CO_HOLY_THURSDAY", easterSunday.AddDays(-3), "Jueves Santo");
        AddEaster(holidays, "CO_GOOD_FRIDAY", easterSunday.AddDays(-2), "Viernes Santo");
        AddEasterEmiliani(holidays, "CO_ASCENSION", easterSunday.AddDays(39), easterSunday.AddDays(43), "Ascensión del Señor");
        AddEasterEmiliani(holidays, "CO_CORPUS_CHRISTI", easterSunday.AddDays(60), easterSunday.AddDays(64), "Corpus Christi");
        AddEasterEmiliani(holidays, "CO_SACRED_HEART", easterSunday.AddDays(68), easterSunday.AddDays(71), "Sagrado Corazón de Jesús");

        if (year >= 2026)
        {
            var commemorativeDate = new DateOnly(year, 7, 9);
            holidays.Add(Create(
                "CO_CHIQUINQUIRA",
                commemorativeDate,
                MoveToNextMonday(commemorativeDate),
                "Día de Nuestra Señora del Rosario de Chiquinquirá",
                BankHolidayRuleKind.ChiquinquiraEmiliani,
                "Ley 2578 de 2026 y Ley 51 de 1983",
                2026));
        }

        return holidays.OrderBy(x => x.Date).ToList();
    }

    public static DateOnly GetEasterSunday(int year)
    {
        var a = year % 19;
        var b = year / 100;
        var c = year % 100;
        var d = b / 4;
        var e = b % 4;
        var f = (b + 8) / 25;
        var g = (b - f + 1) / 3;
        var h = (19 * a + b - d - g + 15) % 30;
        var i = c / 4;
        var k = c % 4;
        var l = (32 + 2 * e + 2 * i - h - k) % 7;
        var m = (a + 11 * h + 22 * l) / 451;
        var month = (h + l - 7 * m + 114) / 31;
        var day = ((h + l - 7 * m + 114) % 31) + 1;
        return new DateOnly(year, month, day);
    }

    public static DateOnly MoveToNextMonday(DateOnly date)
    {
        var days = ((int)DayOfWeek.Monday - (int)date.DayOfWeek + 7) % 7;
        return date.AddDays(days);
    }

    private static void AddFixed(List<BankHolidayModel> holidays, string code, DateOnly date, string name)
        => holidays.Add(Create(code, date, date, name, BankHolidayRuleKind.Fixed, "Legislación nacional colombiana"));

    private static void AddEmiliani(List<BankHolidayModel> holidays, string code, DateOnly date, string name)
        => holidays.Add(Create(code, date, MoveToNextMonday(date), name, BankHolidayRuleKind.Emiliani, "Ley 51 de 1983"));

    private static void AddEaster(List<BankHolidayModel> holidays, string code, DateOnly date, string name)
        => holidays.Add(Create(code, date, date, name, BankHolidayRuleKind.Easter, "Festivo calculado a partir de la Pascua"));

    private static void AddEasterEmiliani(
        List<BankHolidayModel> holidays,
        string code,
        DateOnly commemorativeDate,
        DateOnly effectiveDate,
        string name)
        => holidays.Add(Create(code, commemorativeDate, effectiveDate, name, BankHolidayRuleKind.EasterEmiliani, "Ley 51 de 1983; cálculo a partir de la Pascua"));

    private static BankHolidayModel Create(
        string code,
        DateOnly commemorativeDate,
        DateOnly effectiveDate,
        string name,
        BankHolidayRuleKind kind,
        string legalOrigin,
        int? effectiveFromYear = null)
        => new()
        {
            RuleCode = code,
            CommemorativeDate = commemorativeDate,
            Date = effectiveDate,
            Description = name,
            CountryCode = "CO",
            RuleKind = kind,
            IsSystemGenerated = true,
            LegalOrigin = legalOrigin,
            EffectiveFromYear = effectiveFromYear
        };
}
