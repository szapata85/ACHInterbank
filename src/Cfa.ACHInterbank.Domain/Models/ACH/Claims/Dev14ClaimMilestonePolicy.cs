namespace Cfa.ACHInterbank.Domain.Models.ACH.Claims;

/// <summary>
/// Ventanas de seguimiento conocidas para DEV14 expresadas en ordinales de día hábil.
/// Esta política no calcula calendario; una capa superior debe resolver el ordinal usando
/// el calendario operacional vigente de la cámara.
/// </summary>
public static class Dev14ClaimMilestonePolicy
{
    public const int MaximumBusinessDays = 60;

    public static IReadOnlyList<ClaimMilestoneRule> Rules { get; } =
    [
        new(ClaimMilestone.InitialProgress, 1, 2, [ClaimCaseEventType.Progress, ClaimCaseEventType.Resolved]),
        new(ClaimMilestone.SecondProgress, 16, 17, [ClaimCaseEventType.Progress, ClaimCaseEventType.Resolved]),
        new(ClaimMilestone.ThirdProgress, 31, 32, [ClaimCaseEventType.Progress, ClaimCaseEventType.Resolved]),
        new(ClaimMilestone.FourthProgress, 46, 47, [ClaimCaseEventType.Progress, ClaimCaseEventType.Resolved]),
        new(ClaimMilestone.FinalClosure, 60, 60, [ClaimCaseEventType.Resolved, ClaimCaseEventType.Expired])
    ];

    public static ClaimMilestoneRule GetRule(ClaimMilestone milestone)
        => Rules.Single(x => x.Milestone == milestone);
}

public enum ClaimMilestone
{
    InitialProgress = 1,
    SecondProgress = 2,
    ThirdProgress = 3,
    FourthProgress = 4,
    FinalClosure = 5
}

public sealed record ClaimMilestoneRule(
    ClaimMilestone Milestone,
    int StartBusinessDay,
    int EndBusinessDay,
    IReadOnlyList<ClaimCaseEventType> AcceptedEventTypes)
{
    public bool IsSatisfiedBy(int businessDayNumber, ClaimCaseEventType eventType)
        => businessDayNumber >= StartBusinessDay
           && businessDayNumber <= EndBusinessDay
           && AcceptedEventTypes.Contains(eventType);
}
