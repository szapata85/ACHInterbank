using Cfa.ACHInterbank.Application.ACH.Models;

namespace Cfa.ACHInterbank.Application.ACH.Interfaces;

public interface IBankHolidayProvisioningService
{
    Task<BankHolidayProvisioningResult> EnsureYearsAsync(IEnumerable<int> years, CancellationToken ct = default);
}
