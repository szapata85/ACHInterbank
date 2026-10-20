using System.Diagnostics;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.SchedulerTask;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.Extensions.Logging;
using Quartz;

namespace Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;

[Scoped]
[DisallowConcurrentExecution]
public sealed class SeedBankHolidaysHandler : ITaskHandler
{
    private const string JobIdentity = "BANK_HOLIDAY_SEED";
    private readonly IBankHolidayProvisioningService _provisioning;
    private readonly IOperationalTimeSnapshotProvider _operationalTime;
    private readonly ILogger<SeedBankHolidaysHandler> _logger;

    public SeedBankHolidaysHandler(
        IBankHolidayProvisioningService provisioning,
        IOperationalTimeSnapshotProvider operationalTime,
        ILogger<SeedBankHolidaysHandler> logger)
    {
        _provisioning = provisioning;
        _operationalTime = operationalTime;
        _logger = logger;
    }

    public string Code => "SeedBankHolidays";

    public async Task<string> ExecuteAsync(TaskDefinition task, CancellationToken cancellationToken)
    {
        var startedAt = _operationalTime.CaptureNow();
        var stopwatch = Stopwatch.StartNew();
        var years = ParseYears(task, startedAt.OperationalDate.Year)
            ?? BuildDefaultYears(startedAt.OperationalDate.Year);
        _logger.LogInformation(
            "{JobIdentity} iniciado. StartedAtUtc={StartedAtUtc} Years={Years}",
            JobIdentity,
            startedAt.CapturedAtUtc,
            years);

        try
        {
            var result = await _provisioning.EnsureYearsAsync(years, cancellationToken);
            stopwatch.Stop();
            var finishedAt = _operationalTime.CaptureNow();
            _logger.LogInformation(
                "{JobIdentity} finalizado. FinishedAtUtc={FinishedAtUtc} Years={Years} Expected={Expected} Inserted={Inserted} Updated={Updated} Existing={Existing} SkippedManual={SkippedManual} DurationMs={DurationMs} Result={Result}",
                JobIdentity,
                finishedAt.CapturedAtUtc,
                years,
                result.Expected,
                result.Inserted,
                result.Updated,
                result.Existing,
                result.SkippedManual,
                stopwatch.ElapsedMilliseconds,
                "Completed");

            return $"{JobIdentity}: años={string.Join(',', years)}, esperados={result.Expected}, insertados={result.Inserted}, actualizados={result.Updated}, existentes={result.Existing}, omitidos-manuales={result.SkippedManual}, duración-ms={stopwatch.ElapsedMilliseconds}.";
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(
                ex,
                "{JobIdentity} falló. Years={Years} DurationMs={DurationMs} Result={Result}",
                JobIdentity,
                years,
                stopwatch.ElapsedMilliseconds,
                "Failed");
            throw;
        }
    }

    private static List<int> BuildDefaultYears(int currentYear) => [currentYear, currentYear + 1];

    private static List<int>? ParseYears(TaskDefinition task, int currentYear)
    {
        var yearsParam = task.Parameters.FirstOrDefault(p => p.Key == "Years")?.Value;
        if (!string.IsNullOrWhiteSpace(yearsParam))
        {
            var parsed = yearsParam
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(value => int.TryParse(value, out var year) ? year : 0)
                .Where(year => year is >= 1900 and <= 9999)
                .Distinct()
                .OrderBy(year => year)
                .ToList();
            if (parsed.Count > 0)
            {
                return parsed;
            }
        }

        var nextParam = task.Parameters.FirstOrDefault(p => p.Key == "SeedNextYears")?.Value;
        if (!string.IsNullOrWhiteSpace(nextParam)
            && int.TryParse(nextParam, out var nextYears)
            && nextYears >= 0)
        {
            return Enumerable.Range(currentYear, nextYears + 1).ToList();
        }

        return null;
    }
}
