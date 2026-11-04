using Moq;
using Quartz;

/// <summary>Adapter for Quartz 4 query-based scheduler listings in existing unit tests.</summary>
internal static class Quartz4SchedulerMockHelpers
{
    public static void SetupJobKeys(Mock<IScheduler> scheduler, Func<IEnumerable<JobKey>> keys)
    {
        scheduler.Setup(x => x.QueryJobs(It.IsAny<JobQuery>(), It.IsAny<CancellationToken>()))
            .Returns((JobQuery query, CancellationToken _) =>
            {
                var all = keys().OrderBy(k => k.Group, StringComparer.Ordinal)
                    .ThenBy(k => k.Name, StringComparer.Ordinal).ToArray();
                var page = all.Skip(query.Skip).Take(query.Take)
                    .Select(k => new JobHeader(k, null, "TestJob", false, false, false, false))
                    .ToList();
                return ValueTask.FromResult(new PagedResult<JobHeader>(
                    page, all.Length > query.Skip + page.Count,
                    query.IncludeTotalCount ? all.Length : null));
            });
    }

    public static void SetupTriggersForJob(Mock<IScheduler> scheduler, Func<IEnumerable<ITrigger>> triggers)
    {
        scheduler.Setup(x => x.QueryTriggers(It.IsAny<TriggerQuery>(), It.IsAny<CancellationToken>()))
            .Returns((TriggerQuery query, CancellationToken _) =>
            {
                var all = triggers().Where(t => query.Job is null || t.JobKey == query.Job)
                    .OrderBy(t => t.Key.Group, StringComparer.Ordinal)
                    .ThenBy(t => t.Key.Name, StringComparer.Ordinal).ToArray();
                var page = all.Skip(query.Skip).Take(query.Take)
                    .Select(t => new TriggerHeader(t.Key, t.JobKey, t.Description, "SIMPLE",
                        TriggerState.Normal, t.StartTimeUtc, t.EndTimeUtc, t.NextFireTimeUtc,
                        t.PreviousFireTimeUtc, t.CalendarName, t.Priority, t.ExecutionGroup, null, 0))
                    .ToList();
                return ValueTask.FromResult(new PagedResult<TriggerHeader>(
                    page, all.Length > query.Skip + page.Count,
                    query.IncludeTotalCount ? all.Length : null));
            });
        scheduler.Setup(x => x.GetTriggers(It.IsAny<IReadOnlyCollection<TriggerKey>>(), It.IsAny<CancellationToken>()))
            .Returns((IReadOnlyCollection<TriggerKey> keys, CancellationToken _) =>
                ValueTask.FromResult(triggers().Where(t => keys.Contains(t.Key)).ToList()));
    }
}
