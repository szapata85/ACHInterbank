using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;

[Scoped]
public class CheckBankHolidaysHandler : ITaskHandler
{
    private readonly IBankHoliday _bankholiday;
    private readonly IOperationalTimeSnapshotProvider _operationalTime;

    public CheckBankHolidaysHandler(
        IBankHoliday bankholiday,
        IOperationalTimeSnapshotProvider operationalTime)
    {
        _bankholiday = bankholiday;
        _operationalTime = operationalTime;
    }

    public string Code => "CheckBankHolidays";

    public Task<string> ExecuteAsync(TaskDefinition task, CancellationToken cancellationToken)
    {
        var year = _operationalTime.CaptureNow().OperationalDate.Year;
        var holidays = _bankholiday.GetHolidays(year);
        return Task.FromResult($"{holidays.Count} festivos encontrados en {year}");
    }
}
