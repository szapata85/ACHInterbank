using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.StrategyImplementation;

public class HolidayStrategyFactory : IHolidayStrategyFactory
{
    private readonly AchDbContext _context;

    public HolidayStrategyFactory(AchDbContext context)
    {
        _context = context;
    }

    public IHolidayStrategy GetStrategyForClearingHouse(int clearingHouseId)
    {
        var config = _context.ClearingHouseConfigs
            .AsNoTracking()
            .FirstOrDefault(c => c.ClearingHouseId == clearingHouseId);

        var strategyName = config?.HolidayStrategy ?? "Colombian";

        return strategyName switch
        {
            "Colombian" => new ColombianHolidayStrategy(),
            // "US" => new USHolidayStrategy(),
            // Add more as needed
            _ => throw new NotSupportedException($"Strategy '{strategyName}' is not supported.")
        };
    }
}

