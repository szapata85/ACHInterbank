using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;

[Scoped]
public class ClearingHouseSeeder(AchDbContext context) : IDbSeeder
{
    int IDbSeeder.Order => 1;

    public async Task SeedAsync()
    {
        var fallback = await context.ClearingHouseConfigs.OrderBy(x => x.Id).FirstOrDefaultAsync()
            ?? throw new InvalidOperationException("Seeder ClearingHouseSeeder: falta la configuración base de cámaras.");

        var initial = new[]
        {
            new { Code = RegulatoryCycleScheduleCatalog.AchColombiaCode, Name = "ACH Colombia", OriginCode = "000101006", PaymentRailCode = PaymentRailCodes.AchColombia },
            new { Code = RegulatoryCycleScheduleCatalog.CenitCode, Name = "CENIT", OriginCode = "011111111", PaymentRailCode = PaymentRailCodes.Cenit }
        };

        foreach (var item in initial)
        {
            var house = await context.ClearingHouses
                .SingleOrDefaultAsync(x => x.Code.ToUpper() == item.Code);

            if (house is null)
            {
                house = new ClearingHouse
                {
                    Code = item.Code,
                    Name = item.Name,
                    OriginCode = item.OriginCode,
                    IsActive = true,
                    ClearingHouseId = fallback.Id
                };
                context.ClearingHouses.Add(house);
                await context.SaveChangesAsync();
            }

            var ownConfig = await context.ClearingHouseConfigs
                .SingleOrDefaultAsync(x => x.ClearingHouseId == house.Id);
            if (ownConfig is null)
            {
                if (fallback.ClearingHouseId == house.Id)
                {
                    ownConfig = fallback;
                }
                else
                {
                    ownConfig = new ClearingHouseConfig
                    {
                        ClearingHouseId = house.Id,
                        HolidayStrategy = fallback.HolidayStrategy,
                        TimeZoneId = fallback.TimeZoneId,
                        PaymentRailCode = item.PaymentRailCode
                    };
                    context.ClearingHouseConfigs.Add(ownConfig);
                    await context.SaveChangesAsync();
                }
            }

            // Completa solamente valores base ausentes de las cámaras iniciales.
            // Las configuraciones administradas y las cámaras adicionales no se sobrescriben.
            var configCompleted = false;
            if (string.IsNullOrWhiteSpace(ownConfig.TimeZoneId))
            {
                ownConfig.TimeZoneId = RegulatoryCycleScheduleCatalog.BogotaTimeZoneId;
                configCompleted = true;
            }

            if (string.IsNullOrWhiteSpace(ownConfig.HolidayStrategy))
            {
                ownConfig.HolidayStrategy = "Colombian";
                configCompleted = true;
            }

            if (string.IsNullOrWhiteSpace(ownConfig.PaymentRailCode))
            {
                ownConfig.PaymentRailCode = item.PaymentRailCode;
                configCompleted = true;
            }

            if (configCompleted)
            {
                await context.SaveChangesAsync();
            }

            if (house.ClearingHouseId != ownConfig.Id)
            {
                house.ClearingHouseId = ownConfig.Id;
                await context.SaveChangesAsync();
            }
        }
    }
}
