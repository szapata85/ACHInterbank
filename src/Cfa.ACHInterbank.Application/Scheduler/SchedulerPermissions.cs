namespace Cfa.ACHInterbank.Application.Scheduler;

public static class SchedulerPermissions
{
    public const string View = "Scheduler.View";
    public const string ViewHistory = "Scheduler.History.View";
    public const string Execute = "Scheduler.Execute";
    public const string ManageSchedule = "Scheduler.ManageSchedule";
    public const string PauseResume = "Scheduler.PauseResume";
    public const string ViewInstances = "Scheduler.ViewInstances";
    public const string ViewTechnical = "Scheduler.Technical.View";

    public static IReadOnlyList<string> All { get; } =
    [
        View, ViewHistory, Execute, ManageSchedule, PauseResume, ViewInstances, ViewTechnical
    ];
}
