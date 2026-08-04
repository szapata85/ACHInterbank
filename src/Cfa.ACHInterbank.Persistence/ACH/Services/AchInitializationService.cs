using Cfa.ACHInterbank.Application.ACH.Interfaces;

namespace Cfa.ACHInterbank.Persistence.ACH.Services;

public class AchInitializationService
{
    private readonly IBankHoliday _bankHolidaySeeder;
    private readonly IAchCycleSeeder _achCycleSeeder;
    private readonly IOperationalTimeSnapshotProvider _operationalTime;

    public AchInitializationService(
        IBankHoliday bankHolidaySeeder,
        IAchCycleSeeder achCycleSeeder,
        IOperationalTimeSnapshotProvider operationalTime)
    {
        _bankHolidaySeeder = bankHolidaySeeder;
        _achCycleSeeder = achCycleSeeder;
        _operationalTime = operationalTime;
    }

    public async Task InitializeAsync()
    {
        var currentYear = _operationalTime.CaptureNow().OperationalDate.Year;

        // Seed bank holidays if not already present
        await _bankHolidaySeeder.SeedHolidaysIfNotExistsAsync(currentYear);

        // Optional: dynamically load clearing houses from DB if needed
        var clearingHouseIds = new List<int> { 1, 2 }; // or load from DB

        foreach (var chId in clearingHouseIds)
        {
            await _achCycleSeeder.SeedCyclesIfNotExistsAsync(chId, currentYear);
        }
    }
}
