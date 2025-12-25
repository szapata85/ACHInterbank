using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;

[Scoped]
public class SeedBankHolidaysHandler : ITaskHandler
{
    private readonly IBankHoliday _bankHoliday;

    public SeedBankHolidaysHandler(IBankHoliday bankHoliday)
    {
        _bankHoliday = bankHoliday;
    }

    public string Code => "SeedBankHolidays";

    public async Task<string> ExecuteAsync(TaskDefinition task, CancellationToken cancellationToken)
    {
        var years = ParseYears(task) ?? BuildDefaultYears();
        var ok = 0;

        foreach (var year in years)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await _bankHoliday.SeedHolidaysIfNotExistsAsync(year);
            ok++;
        }

        return $"Festivos sembrados para {ok} año(s): {string.Join(",", years)}.";
    }

    private static List<int> BuildDefaultYears()
    {
        var y = DateTime.Now.Year;
        return new List<int> { y, y + 1 };
    }

    private static List<int>? ParseYears(TaskDefinition task)
    {
        var yearsParam = task.Parameters.FirstOrDefault(p => p.Key == "Years")?.Value;
        if (!string.IsNullOrWhiteSpace(yearsParam))
        {
            var parsed = new List<int>();
            foreach (var s in yearsParam.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                if (int.TryParse(s, out var yy)) parsed.Add(yy);
            if (parsed.Count > 0) return parsed;
        }

        var nextParam = task.Parameters.FirstOrDefault(p => p.Key == "SeedNextYears")?.Value;
        if (!string.IsNullOrWhiteSpace(nextParam) && int.TryParse(nextParam, out var n) && n >= 0)
        {
            var baseYear = DateTime.Now.Year;
            return Enumerable.Range(baseYear, n + 1).ToList();
        }

        return null;
    }
}
