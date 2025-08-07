namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IAchCycleSeeder
{
    Task SeedCyclesIfNotExistsAsync(int clearingHouseId, int year);
}
