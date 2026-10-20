namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record RegulatoryCycleSchedule(
    int CycleNumber,
    TimeSpan StartTime,
    TimeSpan EndTime,
    TimeSpan CutoffTime,
    TimeSpan OutputReleaseTime,
    bool AllowsMonetaryCredit = true,
    bool AllowsMonetaryDebit = true,
    bool AllowsCreditPrenotification = true,
    bool AllowsDebitPrenotification = true,
    bool AllowsReturn = true,
    bool AllowsReturnOfReturn = true,
    TimeSpan? CompensationStatementStartTime = null,
    TimeSpan? CompensationStatementEndTime = null,
    TimeSpan? CompensationPaymentStartTime = null,
    TimeSpan? CompensationPaymentEndTime = null,
    TimeSpan? OutputReleaseStartTime = null,
    TimeSpan? OutputReleaseEndTime = null);

public sealed record RegulatoryCyclePolicySnapshot(
    string ClearingHouseCode,
    string PolicyVersion,
    DateTime EffectiveFrom,
    IReadOnlyList<RegulatoryCycleSchedule> Cycles);

public sealed record RegulatoryCycleScheduleCandidate(
    int CycleNumber,
    TimeSpan? StartTime,
    TimeSpan EndTime,
    TimeSpan CutoffTime,
    TimeSpan OutputReleaseTime,
    bool AllowsMonetaryCredit = true,
    bool AllowsMonetaryDebit = true,
    bool AllowsCreditPrenotification = true,
    bool AllowsDebitPrenotification = true,
    bool AllowsReturn = true,
    bool AllowsReturnOfReturn = true,
    TimeSpan? CompensationStatementStartTime = null,
    TimeSpan? CompensationStatementEndTime = null,
    TimeSpan? CompensationPaymentStartTime = null,
    TimeSpan? CompensationPaymentEndTime = null,
    TimeSpan? OutputReleaseStartTime = null,
    TimeSpan? OutputReleaseEndTime = null)
{
    public bool IsResolved
        => StartTime.HasValue
            && CompensationStatementStartTime.HasValue
            && CompensationStatementEndTime.HasValue
            && CompensationPaymentStartTime.HasValue
            && CompensationPaymentEndTime.HasValue
            && OutputReleaseStartTime.HasValue
            && OutputReleaseEndTime.HasValue;

    internal RegulatoryCycleSchedule RequireResolved()
    {
        if (!StartTime.HasValue)
        {
            throw new InvalidOperationException(
                $"La política candidata no puede activarse: falta confirmar la hora de inicio del Ciclo {CycleNumber}.");
        }
        if (!IsResolved)
        {
            throw new InvalidOperationException(
                $"La política candidata no puede activarse: las etapas horarias del Ciclo {CycleNumber} están incompletas.");
        }

        return new RegulatoryCycleSchedule(
            CycleNumber,
            StartTime.Value,
            EndTime,
            CutoffTime,
            OutputReleaseTime,
            AllowsMonetaryCredit,
            AllowsMonetaryDebit,
            AllowsCreditPrenotification,
            AllowsDebitPrenotification,
            AllowsReturn,
            AllowsReturnOfReturn,
            CompensationStatementStartTime,
            CompensationStatementEndTime,
            CompensationPaymentStartTime,
            CompensationPaymentEndTime,
            OutputReleaseStartTime,
            OutputReleaseEndTime);
    }
}

public enum RegulatoryCycleSourceDay
{
    PreviousOperationalDay = -1,
    CurrentOperationalDay = 0
}

public sealed record RegulatoryCycleReturnSource(
    int CycleNumber,
    RegulatoryCycleSourceDay SourceDay);

public sealed record RegulatoryCycleReturnWindow(
    int ReturnCycleNumber,
    IReadOnlyList<RegulatoryCycleReturnSource> EligibleSources)
{
    public bool Allows(int sourceCycleNumber, RegulatoryCycleSourceDay sourceDay)
        => EligibleSources.Any(source => source.CycleNumber == sourceCycleNumber
            && source.SourceDay == sourceDay);
}

public sealed record RegulatoryCyclePolicyCandidate(
    string ClearingHouseCode,
    string PolicyVersion,
    DateTime? EffectiveFrom,
    IReadOnlyList<RegulatoryCycleScheduleCandidate> Cycles)
{
    public IReadOnlyList<RegulatoryCycleReturnWindow> ReturnWindows { get; init; }
        = Array.Empty<RegulatoryCycleReturnWindow>();

    public bool IsActivatable
        => EffectiveFrom.HasValue
            && EffectiveFrom.Value != default
            && Cycles.Count > 0
            && Cycles.All(cycle => cycle.IsResolved);

    public RegulatoryCyclePolicySnapshot RequireActivatable()
    {
        if (!EffectiveFrom.HasValue || EffectiveFrom.Value == default)
        {
            throw new InvalidOperationException(
                $"La política candidata '{PolicyVersion}' no puede activarse sin un EffectiveFrom oficial válido.");
        }

        var cycles = Array.AsReadOnly(Cycles.Select(cycle => cycle.RequireResolved()).ToArray());
        return new RegulatoryCyclePolicySnapshot(
            ClearingHouseCode,
            PolicyVersion,
            DateTime.SpecifyKind(EffectiveFrom.Value.Date, DateTimeKind.Utc),
            cycles);
    }
}

public static class RegulatoryCycleScheduleCatalog
{
    public const string AchColombiaCode = "ACHCOL";
    public const string CenitCode = "CENIT";
    public const string BogotaTimeZoneId = "America/Bogota";

    // Compatibility anchor inherited from the pre-versioning seeder. This preserves the
    // existing persisted historical baseline; it is not a newly asserted regulatory date.
    public static readonly DateTime HistoricalBootstrapEffectiveFrom =
        DateTime.SpecifyKind(new DateTime(2025, 1, 1), DateTimeKind.Utc);

    public const string HistoricalBootstrapPolicyVersion = "REGULATORY-2025";
    public const string AchColombiaSevenCycleCandidateVersion = "ACHCOL-7C-CANDIDATE";

    private static readonly IReadOnlyList<RegulatoryCycleSchedule> AchColombiaV35HistoricalCycles = Freeze(
        new RegulatoryCycleSchedule(1, new TimeSpan(19, 1, 0), new TimeSpan(8, 30, 0), new TimeSpan(8, 30, 0), new TimeSpan(9, 30, 0)),
        new RegulatoryCycleSchedule(2, new TimeSpan(8, 31, 0), new TimeSpan(11, 0, 0), new TimeSpan(11, 0, 0), new TimeSpan(12, 0, 0)),
        new RegulatoryCycleSchedule(3, new TimeSpan(11, 1, 0), new TimeSpan(14, 0, 0), new TimeSpan(14, 0, 0), new TimeSpan(15, 0, 0)),
        new RegulatoryCycleSchedule(4, new TimeSpan(14, 1, 0), new TimeSpan(16, 0, 0), new TimeSpan(16, 0, 0), new TimeSpan(17, 0, 0)),
        new RegulatoryCycleSchedule(5, new TimeSpan(16, 1, 0), new TimeSpan(18, 0, 0), new TimeSpan(18, 0, 0), new TimeSpan(19, 0, 0),
            AllowsMonetaryDebit: false));

    private static readonly IReadOnlyList<RegulatoryCycleSchedule> CenitDsp152Cycles = Freeze(
        new RegulatoryCycleSchedule(1, new TimeSpan(7, 30, 0), new TimeSpan(10, 30, 0), new TimeSpan(10, 30, 0), new TimeSpan(10, 30, 0),
            AllowsReturn: false, AllowsReturnOfReturn: false),
        new RegulatoryCycleSchedule(2, new TimeSpan(11, 0, 0), new TimeSpan(13, 0, 0), new TimeSpan(13, 0, 0), new TimeSpan(13, 0, 0)),
        new RegulatoryCycleSchedule(3, new TimeSpan(13, 30, 0), new TimeSpan(15, 0, 0), new TimeSpan(15, 0, 0), new TimeSpan(15, 0, 0)),
        new RegulatoryCycleSchedule(4, new TimeSpan(15, 30, 0), new TimeSpan(17, 15, 0), new TimeSpan(17, 15, 0), new TimeSpan(17, 15, 0)),
        new RegulatoryCycleSchedule(5, new TimeSpan(17, 45, 0), new TimeSpan(18, 45, 0), new TimeSpan(18, 45, 0), new TimeSpan(19, 15, 0),
            AllowsMonetaryCredit: false,
            AllowsMonetaryDebit: false,
            AllowsCreditPrenotification: false,
            AllowsDebitPrenotification: false));

    private static readonly IReadOnlyList<RegulatoryCycleReturnWindow> AchColombiaSevenCycleReturnWindows = Freeze(
        ReturnWindow(1, Previous(4), Previous(5), Previous(6), Previous(7)),
        ReturnWindow(2, Previous(5), Previous(6), Previous(7), Current(1)),
        ReturnWindow(3, Previous(6), Previous(7), Current(1), Current(2)),
        ReturnWindow(4, Previous(7), Current(1), Current(2), Current(3)),
        ReturnWindow(5, Current(1), Current(2), Current(3), Current(4)),
        ReturnWindow(6, Current(2), Current(3), Current(4), Current(5)),
        ReturnWindow(7, Current(3), Current(4), Current(5), Current(6)));

    private static readonly IReadOnlyList<RegulatoryCycleScheduleCandidate> AchColombiaSevenCycleCandidateCycles = Freeze(
        Candidate(
            1,
            new TimeSpan(19, 1, 0),
            new TimeSpan(7, 0, 0),
            new TimeSpan(7, 30, 0),
            new TimeSpan(7, 45, 0),
            new TimeSpan(8, 0, 0)),
        Candidate(
            2,
            new TimeSpan(7, 1, 0),
            new TimeSpan(9, 0, 0),
            new TimeSpan(9, 30, 0),
            new TimeSpan(9, 45, 0),
            new TimeSpan(10, 0, 0)),
        Candidate(
            3,
            new TimeSpan(9, 1, 0),
            new TimeSpan(11, 0, 0),
            new TimeSpan(11, 30, 0),
            new TimeSpan(11, 45, 0),
            new TimeSpan(12, 0, 0)),
        Candidate(
            4,
            null,
            new TimeSpan(12, 30, 0),
            new TimeSpan(13, 0, 0),
            new TimeSpan(13, 15, 0),
            new TimeSpan(13, 30, 0)),
        Candidate(
            5,
            new TimeSpan(12, 31, 0),
            new TimeSpan(14, 30, 0),
            new TimeSpan(15, 0, 0),
            new TimeSpan(15, 15, 0),
            new TimeSpan(15, 30, 0)),
        Candidate(
            6,
            new TimeSpan(14, 31, 0),
            new TimeSpan(16, 0, 0),
            new TimeSpan(16, 30, 0),
            new TimeSpan(16, 45, 0),
            new TimeSpan(17, 0, 0)),
        Candidate(
            7,
            new TimeSpan(16, 1, 0),
            new TimeSpan(18, 0, 0),
            new TimeSpan(18, 30, 0),
            new TimeSpan(18, 45, 0),
            new TimeSpan(19, 0, 0),
            allowsMonetaryDebit: false));

    public static RegulatoryCyclePolicySnapshot AchColombiaV35HistoricalPolicy { get; } = new(
        AchColombiaCode,
        HistoricalBootstrapPolicyVersion,
        HistoricalBootstrapEffectiveFrom,
        AchColombiaV35HistoricalCycles);

    public static RegulatoryCyclePolicyCandidate AchColombiaSevenCycleCandidate { get; } = new(
        AchColombiaCode,
        AchColombiaSevenCycleCandidateVersion,
        EffectiveFrom: null,
        Cycles: AchColombiaSevenCycleCandidateCycles)
    {
        ReturnWindows = AchColombiaSevenCycleReturnWindows
    };

    internal static RegulatoryCyclePolicySnapshot CenitHistoricalSeedPolicy { get; } = new(
        CenitCode,
        HistoricalBootstrapPolicyVersion,
        HistoricalBootstrapEffectiveFrom,
        CenitDsp152Cycles);

    // Exact immutable fingerprint used by seed repair for the historical ACH Colombia V35 baseline.
    public static IReadOnlyList<RegulatoryCycleSchedule> AchColombiaV35HistoricalFingerprint
        => AchColombiaV35HistoricalCycles;

    public static IReadOnlyList<RegulatoryCycleSchedule> GetRequired(string clearingHouseCode)
        => NormalizeCode(clearingHouseCode) switch
        {
            AchColombiaCode => AchColombiaV35HistoricalCycles,
            CenitCode => CenitDsp152Cycles,
            _ => throw new InvalidOperationException($"No existe agenda regulatoria registrada para la cámara '{clearingHouseCode}'.")
        };

    public static RegulatoryCyclePolicySnapshot GetHistoricalSeedPolicy(string clearingHouseCode)
        => NormalizeCode(clearingHouseCode) switch
        {
            AchColombiaCode => AchColombiaV35HistoricalPolicy,
            CenitCode => CenitHistoricalSeedPolicy,
            _ => throw new InvalidOperationException($"No existe política histórica de ciclos registrada para la cámara '{clearingHouseCode}'.")
        };

    private static RegulatoryCycleScheduleCandidate Candidate(
        int cycleNumber,
        TimeSpan? startTime,
        TimeSpan closeTime,
        TimeSpan statementEndTime,
        TimeSpan paymentEndTime,
        TimeSpan releaseEndTime,
        bool allowsMonetaryDebit = true)
        => new(
            cycleNumber,
            startTime,
            closeTime,
            closeTime,
            releaseEndTime,
            AllowsMonetaryDebit: allowsMonetaryDebit,
            CompensationStatementStartTime: closeTime,
            CompensationStatementEndTime: statementEndTime,
            CompensationPaymentStartTime: statementEndTime,
            CompensationPaymentEndTime: paymentEndTime,
            OutputReleaseStartTime: paymentEndTime,
            OutputReleaseEndTime: releaseEndTime);

    private static RegulatoryCycleReturnSource Previous(int cycleNumber)
        => new(cycleNumber, RegulatoryCycleSourceDay.PreviousOperationalDay);

    private static RegulatoryCycleReturnSource Current(int cycleNumber)
        => new(cycleNumber, RegulatoryCycleSourceDay.CurrentOperationalDay);

    private static RegulatoryCycleReturnWindow ReturnWindow(
        int returnCycleNumber,
        params RegulatoryCycleReturnSource[] eligibleSources)
        => new(returnCycleNumber, Array.AsReadOnly(eligibleSources));

    private static IReadOnlyList<T> Freeze<T>(params T[] values)
        => Array.AsReadOnly(values);

    private static string NormalizeCode(string clearingHouseCode)
        => clearingHouseCode?.Trim().ToUpperInvariant()
            ?? throw new ArgumentNullException(nameof(clearingHouseCode));
}
