using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using System.Drawing;

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
                    Name = "Bancolombia",
                    RoutingNumber = "00001",
                    TransitCode = "007",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Banco de Bogota",
                    RoutingNumber = "00001",
                    TransitCode = "001",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Banco Popular",
                    RoutingNumber = "00001",
                    TransitCode = "002",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Bancafe - No Disponible",
                    RoutingNumber = "00001",
                    TransitCode = "005",
                    Status = FinancialInstitutionStatus.Inactive
                },
                new FinancialInstitution {
                    Name = "Banco ITAU",
                    RoutingNumber = "00001",
                    TransitCode = "006",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Scotiabank Colombia S.A",
                    RoutingNumber = "00001",
                    TransitCode = "008",
                    Status = FinancialInstitutionStatus.Inactive 
                },
                new FinancialInstitution {
                    Name = "Citibank Colombia",
                    RoutingNumber = "00001",
                    TransitCode = "009",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Banco GNB Colombia S.A",
                    RoutingNumber = "00001",
                    TransitCode = "010",
                    Status = FinancialInstitutionStatus.Inactive
                },
                new FinancialInstitution {
                    Name = "Banco GNB Sudameris",
                    RoutingNumber = "00001",
                    TransitCode = "012",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "BBVA",
                    RoutingNumber = "00001",
                    TransitCode = "013",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Helm Bank S.A",
                    RoutingNumber = "00001",
                    TransitCode = "014",
                    Status = FinancialInstitutionStatus.Inactive
                },
                new FinancialInstitution {
                    Name = "Scotiabank Colpatria",
                    RoutingNumber = "00001",
                    TransitCode = "019",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Banco Tequendama",
                    RoutingNumber = "00001",
                    TransitCode = "029",
                    Status = FinancialInstitutionStatus.Inactive
                },
                new FinancialInstitution {
                    Name = "BCSC S.A",
                    RoutingNumber = "00001",
                    TransitCode = "032",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Banco Davivienda",
                    RoutingNumber = "00001",
                    TransitCode = "051",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Granahorrar",
                    RoutingNumber = "00001",
                    TransitCode = "054",
                    Status = FinancialInstitutionStatus.Inactive
                },
                new FinancialInstitution {
                    Name = "Conavi",
                    RoutingNumber = "00001",
                    TransitCode = "055",
                    Status = FinancialInstitutionStatus.Inactive
                },
                new FinancialInstitution {
                    Name = "Colmena",
                    RoutingNumber = "00001",
                    TransitCode = "057",
                    Status = FinancialInstitutionStatus.Inactive
                },
                new FinancialInstitution {
                    Name = "Banco de Occidente",
                    RoutingNumber = "00001",
                    TransitCode = "023",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "AV Villas",
                    RoutingNumber = "00001",
                    TransitCode = "052",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Megabanco",
                    RoutingNumber = "00001",
                    TransitCode = "036",
                    Status = FinancialInstitutionStatus.Inactive
                },
                new FinancialInstitution {
                    Name = "Banco Superior",
                    RoutingNumber = "00001",
                    TransitCode = "034",
                    Status = FinancialInstitutionStatus.Inactive
                },
                new FinancialInstitution {
                    Name = "Banco Boston",
                    RoutingNumber = "00001",
                    TransitCode = "037",
                    Status = FinancialInstitutionStatus.Inactive
                },
                new FinancialInstitution {
                    Name = "Banco Agrario de Colombia",
                    RoutingNumber = "00001",
                    TransitCode = "040",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Banco de la Republica",
                    RoutingNumber = "00001",
                    TransitCode = "000",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Direccion del Tesoro Nacional",
                    RoutingNumber = "00001",
                    TransitCode = "683",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Skandia",
                    RoutingNumber = "00001",
                    TransitCode = "502",
                    Status = FinancialInstitutionStatus.Inactive
                },
                new FinancialInstitution {
                    Name = "Banco Coopcentral",
                    RoutingNumber = "00001",
                    TransitCode = "066",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Banco Chartered",
                    RoutingNumber = "00001",
                    TransitCode = "024",
                    Status = FinancialInstitutionStatus.Inactive
                },
                new FinancialInstitution {
                    Name = "PSE",
                    RoutingNumber = "00001",
                    TransitCode = "101",
                    Status = FinancialInstitutionStatus.Inactive
                },
                new FinancialInstitution {
                    Name = "Granbanco S.A.",
                    RoutingNumber = "00001",
                    TransitCode = "050",
                    Status = FinancialInstitutionStatus.Inactive
                },
                new FinancialInstitution {
                    Name = "Corficolombiana",
                    RoutingNumber = "00001",
                    TransitCode = "090",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Deceval",
                    RoutingNumber = "00001",
                    TransitCode = "550",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Cooperativa Financiera de Antioquia",
                    IsDefaultSource = true,
                    RoutingNumber = "00001",
                    TransitCode = "283",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Financiera Juriscoop",
                    RoutingNumber = "00001",
                    TransitCode = "296",
                    Status = FinancialInstitutionStatus.Inactive
                },
                new FinancialInstitution {
                    Name = "Banco Bancien",
                    RoutingNumber = "00001",
                    TransitCode = "058",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Confiar Cooperativa Financiera",
                    RoutingNumber = "00001",
                    TransitCode = "292",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Bancoldex",
                    RoutingNumber = "00001",
                    TransitCode = "031",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Compensar",
                    RoutingNumber = "00001",
                    TransitCode = "083",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Asopagos S.A",
                    RoutingNumber = "00001",
                    TransitCode = "086",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Fedecajas",
                    RoutingNumber = "00001",
                    TransitCode = "087",
                    Status = FinancialInstitutionStatus.Inactive
                },
                new FinancialInstitution {
                    Name = "Simple S.A.",
                    RoutingNumber = "00001",
                    TransitCode = "088",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Enlace Operativo S.A.",
                    RoutingNumber = "00001",
                    TransitCode = "089",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "JPMorgan Corp Fciera",
                    RoutingNumber = "00001",
                    TransitCode = "041",
                    Status = FinancialInstitutionStatus.Inactive
                },
                new FinancialInstitution {
                    Name = "Aportes en Linea S.A.",
                    RoutingNumber = "00001",
                    TransitCode = "084",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Coopcentral",
                    RoutingNumber = "00001",
                    TransitCode = "076",
                    Status = FinancialInstitutionStatus.Inactive
                },
                new FinancialInstitution {
                    Name = "Bancoomeva",
                    RoutingNumber = "00001",
                    TransitCode = "061",
                    Status = FinancialInstitutionStatus.Inactive
                },
                new FinancialInstitution {
                    Name = "Banco Pichincha",
                    RoutingNumber = "00001",
                    TransitCode = "060",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Cotrafa Cooperativa Financiera",
                    RoutingNumber = "00001",
                    TransitCode = "289",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Banco Finandina S.A",
                    RoutingNumber = "00001",
                    TransitCode = "063",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "BNP Paribas Colombia",
                    RoutingNumber = "00001",
                    TransitCode = "042",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Coltefinanciera",
                    RoutingNumber = "00001",
                    TransitCode = "370",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "DGCPTN Sistema Gral Regalias",
                    RoutingNumber = "00001",
                    TransitCode = "685",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Banco Santander Negocios Colom",
                    RoutingNumber = "00001",
                    TransitCode = "065",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Financiera Juriscoop CF",
                    RoutingNumber = "00001",
                    TransitCode = "121",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Banco Multibank S.A.",
                    RoutingNumber = "00001",
                    TransitCode = "064",
                    Status = FinancialInstitutionStatus.Inactive
                },
                new FinancialInstitution {
                    Name = "Mi Banco",
                    RoutingNumber = "00001",
                    TransitCode = "067",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Banco Falabella",
                    RoutingNumber = "00001",
                    TransitCode = "062",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Serfinansa S.A",
                    RoutingNumber = "00001",
                    TransitCode = "342",
                    Status = FinancialInstitutionStatus.Inactive
                },
                new FinancialInstitution {
                    Name = "Banco Mundo Mujer",
                    RoutingNumber = "00001",
                    TransitCode = "047",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Banco Serfinanza",
                    RoutingNumber = "00001",
                    TransitCode = "069",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Coofinep Cooperativa Financiera",
                    RoutingNumber = "00001",
                    TransitCode = "291",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Banco JP Morgan",
                    RoutingNumber = "00001",
                    TransitCode = "071",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Bancamia",
                    RoutingNumber = "00001",
                    TransitCode = "059",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "JFK Cooperativa Financiera",
                    RoutingNumber = "00001",
                    TransitCode = "286",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Fogafin",
                    RoutingNumber = "00001",
                    TransitCode = "684",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "IRIS Financiamiento S A",
                    RoutingNumber = "00001",
                    TransitCode = "637",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Uala",
                    RoutingNumber = "00001",
                    TransitCode = "804",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Movii",
                    RoutingNumber = "00001",
                    TransitCode = "801",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Nequi",
                    RoutingNumber = "00001",
                    TransitCode = "507",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Daviplata",
                    RoutingNumber = "00001",
                    TransitCode = "551",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Banco Btg Pactual Colombia SA",
                    RoutingNumber = "00001",
                    TransitCode = "805",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Pibank",
                    RoutingNumber = "00001",
                    TransitCode = "560",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Banco Union",
                    RoutingNumber = "00001",
                    TransitCode = "303",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Ding Tecnipagos SA",
                    RoutingNumber = "00001",
                    TransitCode = "802",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Powwi",
                    RoutingNumber = "00001",
                    TransitCode = "803",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Rappipay",
                    RoutingNumber = "00001",
                    TransitCode = "811",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Coink",
                    RoutingNumber = "00001",
                    TransitCode = "812",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Banco W",
                    RoutingNumber = "00001",
                    TransitCode = "053",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Lulo Bank",
                    RoutingNumber = "00001",
                    TransitCode = "070",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Nu Bank",
                    RoutingNumber = "00001",
                    TransitCode = "809",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Bold CF",
                    RoutingNumber = "00001",
                    TransitCode = "808",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Global66",
                    RoutingNumber = "00001",
                    TransitCode = "814",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Banco Contactar",
                    RoutingNumber = "00001",
                    TransitCode = "819",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Dale Aval Soluciones Digitales",
                    RoutingNumber = "00001",
                    TransitCode = "899",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Ria Money Transfer",
                    RoutingNumber = "00001",
                    TransitCode = "817",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "ACH Colombia",
                    RoutingNumber = "00001",
                    TransitCode = "115",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Credifamilia",
                    RoutingNumber = "00001",
                    TransitCode = "117",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Crezcamos",
                    RoutingNumber = "00001",
                    TransitCode = "816",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Koa CF",
                    RoutingNumber = "00001",
                    TransitCode = "807",
                    Status = FinancialInstitutionStatus.Active
                },
                new FinancialInstitution {
                    Name = "Paycash",
                    RoutingNumber = "00001",
                    TransitCode = "824",
                    Status = FinancialInstitutionStatus.Active
                }
            };

            // 🔑 Calcular el dígito de chequeo para cada institución
            //foreach (var fi in institutions)
            //{
            //    fi.CalculateCheckDigit();
            //}
            // Optimizado con LINQ
            institutions.ToList().ForEach(fi => fi.CalculateCheckDigit());

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