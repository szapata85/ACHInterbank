using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;

[Scoped]
public sealed class BankHolidaySeeder : IDbSeeder
{
    private readonly IBankHolidayProvisioningService _provisioning;
    private readonly IOperationalTimeSnapshotProvider _operationalTime;

    public BankHolidaySeeder(
        IBankHolidayProvisioningService provisioning,
        IOperationalTimeSnapshotProvider operationalTime)
    {
        _provisioning = provisioning;
        _operationalTime = operationalTime;
    }

    int IDbSeeder.Order => 3;

    public async Task SeedAsync()
    {
        var currentYear = _operationalTime.CaptureNow().OperationalDate.Year;
        await _provisioning.EnsureYearsAsync([currentYear, currentYear + 1]);
    }
}
