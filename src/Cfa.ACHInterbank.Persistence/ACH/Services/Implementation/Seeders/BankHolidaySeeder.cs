using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;

[Scoped]
public class BankHolidaySeeder : IDbSeeder
{
    private readonly AchDbContext _context;

    public BankHolidaySeeder(AchDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        if (!_context.BankHolidays.Any())
        {
            _context.ChangeTracker.AutoDetectChangesEnabled = false;
            _context.BankHolidays.AddRange(
                new BankHolidayModel {Date = new DateOnly(2025, 1, 1), Description = "Año Nuevo" },
                new BankHolidayModel {Date = new DateOnly(2025, 1, 6), Description = "Día de los Reyes Magos" },
                new BankHolidayModel {Date = new DateOnly(2025, 3, 24), Description = "San José" },
                new BankHolidayModel {Date = new DateOnly(2025, 4, 17), Description = "Jueves Santo" },
                new BankHolidayModel {Date = new DateOnly(2025, 4, 18), Description = "Viernes Santo" },
                new BankHolidayModel {Date = new DateOnly(2025, 5, 1), Description = "Día del Trabajo" },
                new BankHolidayModel {Date = new DateOnly(2025, 5, 26), Description = "Ascensión del Señor" },
                new BankHolidayModel {Date = new DateOnly(2025, 6, 16), Description = "Corpus Christi" },
                new BankHolidayModel {Date = new DateOnly(2025, 6, 23), Description = "Sagrado Corazón" },
                new BankHolidayModel {Date = new DateOnly(2025, 7, 20), Description = "Día de la Independencia" },
                new BankHolidayModel {Date = new DateOnly(2025, 8, 7), Description = "Batalla de Boyacá" },
                new BankHolidayModel {Date = new DateOnly(2025, 8, 18), Description = "La Asunción" },
                new BankHolidayModel {Date = new DateOnly(2025, 10, 13), Description = "Día de la Raza" },
                new BankHolidayModel {Date = new DateOnly(2025, 11, 3), Description = "Todos los Santos" },
                new BankHolidayModel {Date = new DateOnly(2025, 11, 17), Description = "Independencia de Cartagena" },
                new BankHolidayModel {Date = new DateOnly(2025, 12, 8), Description = "Inmaculada Concepción" },
                new BankHolidayModel {Date = new DateOnly(2025, 12, 25), Description = "Navidad" }
            );

            await _context.SaveChangesAsync();

            _context.ChangeTracker.AutoDetectChangesEnabled = true;
        }
    }
}
