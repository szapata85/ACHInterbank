using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.StrategyImplementation;

public class ColombianHolidayStrategy : IHolidayStrategy
{
    public List<BankHolidayModel> GenerateHolidays(int year)
    {
        var holidays = new List<BankHolidayModel>();

        void AddHoliday(DateOnly date, string description) =>
            holidays.Add(new BankHolidayModel { Date = date, Description = description, CountryCode = "CO" });

        DateOnly MoveToNextMonday(DateOnly date)
        {
            var moved = date;
            while (moved.DayOfWeek != DayOfWeek.Monday)
            {
                moved = moved.AddDays(1);
            }

            return moved;
        }

        var easterSunday = GetEasterSunday(year);

        // Fijos
        AddHoliday(new DateOnly(year, 1, 1), "Año Nuevo");
        AddHoliday(new DateOnly(year, 5, 1), "Día del Trabajo");
        AddHoliday(new DateOnly(year, 7, 20), "Día de la Independencia");
        AddHoliday(new DateOnly(year, 8, 7), "Batalla de Boyacá");
        AddHoliday(new DateOnly(year, 12, 8), "Inmaculada Concepción");
        AddHoliday(new DateOnly(year, 12, 25), "Navidad");

        // Ley Emiliani (se traslada al siguiente lunes)
        AddHoliday(MoveToNextMonday(new DateOnly(year, 1, 6)), "Día de los Reyes Magos");
        AddHoliday(MoveToNextMonday(new DateOnly(year, 3, 19)), "San José");
        AddHoliday(MoveToNextMonday(new DateOnly(year, 6, 29)), "San Pedro y San Pablo");
        AddHoliday(MoveToNextMonday(new DateOnly(year, 8, 15)), "La Asunción");
        AddHoliday(MoveToNextMonday(new DateOnly(year, 10, 12)), "Día de la Raza");
        AddHoliday(MoveToNextMonday(new DateOnly(year, 11, 1)), "Todos los Santos");
        AddHoliday(MoveToNextMonday(new DateOnly(year, 11, 11)), "Independencia de Cartagena");

        // Pascua (Jueves y Viernes Santo sin traslado)
        AddHoliday(easterSunday.AddDays(-3), "Jueves Santo");
        AddHoliday(easterSunday.AddDays(-2), "Viernes Santo");

        // Pascua con Ley Emiliani
        AddHoliday(MoveToNextMonday(easterSunday.AddDays(39)), "Ascensión del Señor");
        AddHoliday(MoveToNextMonday(easterSunday.AddDays(60)), "Corpus Christi");
        AddHoliday(MoveToNextMonday(easterSunday.AddDays(68)), "Sagrado Corazón");

        return holidays;
    }

    private static DateOnly GetEasterSunday(int year)
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
}
