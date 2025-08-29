using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;

[Scoped]
public class ClearingHouseSeeder : IDbSeeder
{
    private readonly AchDbContext _context;

    public ClearingHouseSeeder(AchDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        if (!_context.ClearingHouses.Any())
        {
            _context.ChangeTracker.AutoDetectChangesEnabled = false;
            _context.ClearingHouses.AddRange(
                 new ClearingHouse {Name = "ACH Colombia", Code = "ACHCOL", ClearingHouseId = 1 },
                 new ClearingHouse {Name = "CENIT", Code = "CENIT", ClearingHouseId = 1 }
            );

            await _context.SaveChangesAsync();
            _context.ChangeTracker.AutoDetectChangesEnabled = true;
        }
    }
}
