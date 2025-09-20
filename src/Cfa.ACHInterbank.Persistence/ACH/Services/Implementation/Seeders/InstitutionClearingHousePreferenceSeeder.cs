using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;

public class InstitutionClearingHousePreferenceSeeder : IDbSeeder
{
    private readonly AchDbContext _context;

    public InstitutionClearingHousePreferenceSeeder(AchDbContext context)
    {
        _context = context;
    }

    // Se ejecuta después de que ya existen cámaras (Order = 1) e instituciones (Order = 2)
    public int Order => 3;

    public async Task SeedAsync()
    {
        // ✅ Cargar instituciones y cámaras existentes
        var institutions = await _context.FinancialInstitutions
            .AsNoTracking()
            .ToListAsync();

        var clearingHouses = await _context.ClearingHouses
            .AsNoTracking()
            .ToListAsync();

        if (!institutions.Any() || !clearingHouses.Any())
            throw new InvalidOperationException(
                "No hay instituciones financieras o cámaras compensadoras registradas.");

        if (!await _context.InstitutionClearingHousePreferences.AnyAsync())
        {
            // Genera todas las combinaciones Institución–Cámara en una sola expresión
            var preferences = institutions
                .SelectMany(fi => clearingHouses.Select(ch => new InstitutionClearingHousePreference
                {
                    FinancialInstitutionId = fi.Id,
                    ClearingHouseId = ch.Id,
                    Priority = fi.IsDefaultSource && ch.Code == "ACHCOL" ? 1 : 2,
                    IsDefault = fi.IsDefaultSource && ch.Code == "ACHCOL"
                }))
                .ToList();

            _context.InstitutionClearingHousePreferences.AddRange(preferences);
            await _context.SaveChangesAsync();
        }

    }
}

