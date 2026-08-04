using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;

[Scoped]
public class ClearingHouseConfigSeeder : IDbSeeder
{
    private readonly AchDbContext _context;

    public ClearingHouseConfigSeeder(AchDbContext context)
    {
        _context = context;
    }

    int IDbSeeder.Order => 0;

    public async Task SeedAsync()
    {
        if (!await ClearingHouseConfigsTableExistsAsync())
        {
            return;
        }

        if (!await _context.ClearingHouseConfigs.AnyAsync())
        {
            _context.ChangeTracker.AutoDetectChangesEnabled = false;
            _context.ClearingHouseConfigs.Add(new ClearingHouseConfig
            {
                // Bootstrap only: ClearingHouseSeeder creates the code-resolved owned
                // configurations and repoints each clearing house without relying on IDs.
                ClearingHouseId = 0,
                HolidayStrategy = "Colombian",
                TimeZoneId = RegulatoryCycleScheduleCatalog.BogotaTimeZoneId
            });

            await _context.SaveChangesAsync();
            _context.ChangeTracker.AutoDetectChangesEnabled = true;
        }
    }

    private async Task<bool> ClearingHouseConfigsTableExistsAsync()
    {
        var connection = _context.Database.GetDbConnection();
        var shouldClose = connection.State == System.Data.ConnectionState.Closed;

        if (shouldClose)
        {
            await connection.OpenAsync();
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1 FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = 'ClearingHouseConfigs'";
            var result = await command.ExecuteScalarAsync();
            return result != null;
        }
        finally
        {
            if (shouldClose)
            {
                await connection.CloseAsync();
            }
        }
    }
}
