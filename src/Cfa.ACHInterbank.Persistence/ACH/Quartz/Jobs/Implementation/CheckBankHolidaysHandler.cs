using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;

[Scoped]
internal class CheckBankHolidaysHandler : ITaskHandler
{
    private readonly IBankHoliday _bankholiday;

    public CheckBankHolidaysHandler(IBankHoliday bankholiday)
    {
        _bankholiday = bankholiday;
    }

    public string Code => "CheckBankHolidays";

    public Task<string> ExecuteAsync(TaskDefinition task, CancellationToken cancellationToken)
    {
        var year = DateTime.Now.Year;
        var holidays = _bankholiday.GetHolidays(year);
        return Task.FromResult($"{holidays.Count} festivos encontrados en {year}");
    }
}
