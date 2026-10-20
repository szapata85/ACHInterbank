using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record OperationalCalendarReason(
    string Code,
    string Description,
    DateOnly Date,
    int? ClearingHouseId = null,
    BankHolidayRuleKind? HolidayRuleKind = null,
    DateOnly? CommemorativeDate = null);

public sealed record OperationalDayExplanation(
    DateOnly Date,
    int ClearingHouseId,
    bool IsBusinessDay,
    IReadOnlyList<OperationalCalendarReason> Reasons);

public sealed record BankHolidayProvisioningYearResult(
    int Year,
    int Expected,
    int Inserted,
    int Updated,
    int Existing,
    int SkippedManual);

public sealed record BankHolidayProvisioningResult(
    IReadOnlyList<BankHolidayProvisioningYearResult> Years)
{
    public int Expected => Years.Sum(x => x.Expected);
    public int Inserted => Years.Sum(x => x.Inserted);
    public int Updated => Years.Sum(x => x.Updated);
    public int Existing => Years.Sum(x => x.Existing);
    public int SkippedManual => Years.Sum(x => x.SkippedManual);
}
