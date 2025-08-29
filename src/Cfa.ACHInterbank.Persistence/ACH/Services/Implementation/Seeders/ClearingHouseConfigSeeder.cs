using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;

[Scoped]
public class ClearingHouseConfigSeeder : IDbSeeder
{
    private readonly AchDbContext _context;

    public ClearingHouseConfigSeeder(AchDbContext context)
    {
        _context = context;
    }

    public async Task SeedAsync()
    {
        if (!_context.ClearingHouseConfigs.Any())
        {
            _context.ClearingHouseConfigs.Add(new ClearingHouseConfig() {ClearingHouseId = 1, HolidayStrategy = "Colombian" });
        }

        await _context.SaveChangesAsync();
    }
}
