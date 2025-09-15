using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;

public class FinancialInstitutionSeeder : IDbSeeder
{
    private readonly AchDbContext _context;
    public FinancialInstitutionSeeder(AchDbContext context)
    {
        _context = context;
    }

    // Ejecutar después del seeder de ClearingHouses
    public int Order => 2;

    public async Task SeedAsync()
    {
        // ✅ Consultar cámaras ya sembradas por el seeder de Order = 1
        var idAchColombia = await _context.ClearingHouses
            .Where(ch => ch.Code == "ACHCOL")
            .Select(ch => ch.Id)
            .FirstOrDefaultAsync();

        var idCenit = await _context.ClearingHouses
            .Where(ch => ch.Code == "CENIT")
            .Select(ch => ch.Id)
            .FirstOrDefaultAsync();

        if (idAchColombia == 0 || idCenit == 0)
            throw new InvalidOperationException(
                "Las cámaras ACHCOL y/o CENIT no están registradas. Ejecuta primero el seeder de ClearingHouses.");

        // ⚡ Sembrar instituciones financieras solo si no existen
        if (!await _context.FinancialInstitutions.AnyAsync())
        {
            var institutions = new List<FinancialInstitution>
            {
                new FinancialInstitution { Name = "Banco Agrario de Colombia", Code = "1040", ClearingHouseId = idCenit },
                new FinancialInstitution { Name = "Banco AV Villas",          Code = "1052", ClearingHouseId = idCenit },
                new FinancialInstitution { Name = "Banco de Bogotá",          Code = "1001", ClearingHouseId = idCenit },
                new FinancialInstitution { Name = "Banco Davivienda",         Code = "1051", ClearingHouseId = idCenit },
                new FinancialInstitution { Name = "Banco BBVA",               Code = "1013", ClearingHouseId = idCenit },
                new FinancialInstitution { Name = "Banco ACH Ejemplo",        Code = "2001", ClearingHouseId = idAchColombia },

                // 🆕 Cooperativa Financiera de Antioquia, marcada como origen por defecto
                new FinancialInstitution { Name = "Cooperativa Financiera de Antioquia", Code = "CF001", ClearingHouseId = idCenit, IsDefaultSource = true }
            };

            // Desmarcar cualquier otra para que solo esta quede como predeterminada
            foreach (var fi in institutions.Where(f => f.Code != "CF001"))
            {
                fi.IsDefaultSource = false;
            }

            _context.FinancialInstitutions.AddRange(institutions);
            await _context.SaveChangesAsync();
        }
        else
        {
            // Garantizar que haya una por defecto
            bool hasDefault = await _context.FinancialInstitutions.AnyAsync(fi => fi.IsDefaultSource);
            if (!hasDefault)
            {
                var first = await _context.FinancialInstitutions.FirstAsync();
                first.IsDefaultSource = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}
