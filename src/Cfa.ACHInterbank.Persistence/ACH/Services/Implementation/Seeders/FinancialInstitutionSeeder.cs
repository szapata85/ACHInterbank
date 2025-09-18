using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
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

    // Se ejecuta después del seeder de ClearingHouses (Order = 1)
    public int Order => 2;

    public async Task SeedAsync()
    {
        // ⚡ Solo sembrar si la tabla está vacía
        if (!await _context.FinancialInstitutions.AnyAsync())
        {
            // Definición de instituciones con Routing/Transit
            var institutions = new List<FinancialInstitution>
            {
                new FinancialInstitution {
                    Name = "Banco Agrario de Colombia",
                    RoutingNumber = "001010",
                    TransitCode = "06",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Banco AV Villas",
                    RoutingNumber = "001020",
                    TransitCode = "06",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Banco de Bogotá",
                    RoutingNumber = "001030",
                    TransitCode = "06",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Banco Davivienda",
                    RoutingNumber = "001040",
                    TransitCode = "06",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Banco BBVA",
                    RoutingNumber = "001050",
                    TransitCode = "06",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Banco ACH Ejemplo",
                    RoutingNumber = "001060",
                    TransitCode = "06",
                    Status = FinancialInstitutionStatus.Active
                },
                // 🆕 Cooperativa Financiera de Antioquia
                new FinancialInstitution {
                    Name = "Cooperativa Financiera de Antioquia",
                    IsDefaultSource = true,
                    RoutingNumber = "001070",
                    TransitCode = "06",
                    Status = FinancialInstitutionStatus.Active
                }
            };

            // 🔑 Calcular el dígito de chequeo para cada institución
            foreach (var fi in institutions)
            {
                fi.CalculateCheckDigit();
            }

            _context.FinancialInstitutions.AddRange(institutions);
            await _context.SaveChangesAsync();
        }
        else
        {
            // ✅ Garantizar que exista una entidad por defecto
            bool hasDefault = await _context.FinancialInstitutions
                .AnyAsync(fi => fi.IsDefaultSource);
            if (!hasDefault)
            {
                var first = await _context.FinancialInstitutions.FirstAsync();
                first.IsDefaultSource = true;
                await _context.SaveChangesAsync();
            }
        }
    }
}