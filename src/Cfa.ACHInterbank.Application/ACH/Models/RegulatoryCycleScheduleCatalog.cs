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
    bool AllowsReturnOfReturn = true);

public static class RegulatoryCycleScheduleCatalog
{
    public const string AchColombiaCode = "ACHCOL";
    public const string CenitCode = "CENIT";
    public const string BogotaTimeZoneId = "America/Bogota";

    private static readonly IReadOnlyList<RegulatoryCycleSchedule> AchColombiaV35 =
    [
        new(1, new TimeSpan(19, 1, 0), new TimeSpan(8, 30, 0), new TimeSpan(8, 30, 0), new TimeSpan(9, 30, 0)),
        new(2, new TimeSpan(8, 31, 0), new TimeSpan(11, 0, 0), new TimeSpan(11, 0, 0), new TimeSpan(12, 0, 0)),
        new(3, new TimeSpan(11, 1, 0), new TimeSpan(14, 0, 0), new TimeSpan(14, 0, 0), new TimeSpan(15, 0, 0)),
        new(4, new TimeSpan(14, 1, 0), new TimeSpan(16, 0, 0), new TimeSpan(16, 0, 0), new TimeSpan(17, 0, 0)),
        new(5, new TimeSpan(16, 1, 0), new TimeSpan(18, 0, 0), new TimeSpan(18, 0, 0), new TimeSpan(19, 0, 0),
            AllowsMonetaryDebit: false)
    ];

    private static readonly IReadOnlyList<RegulatoryCycleSchedule> CenitDsp152 =
    [
        new(1, new TimeSpan(7, 30, 0), new TimeSpan(10, 30, 0), new TimeSpan(10, 30, 0), new TimeSpan(10, 30, 0),
            AllowsReturn: false, AllowsReturnOfReturn: false),
        new(2, new TimeSpan(11, 0, 0), new TimeSpan(13, 0, 0), new TimeSpan(13, 0, 0), new TimeSpan(13, 0, 0)),
        new(3, new TimeSpan(13, 30, 0), new TimeSpan(15, 0, 0), new TimeSpan(15, 0, 0), new TimeSpan(15, 0, 0)),
        new(4, new TimeSpan(15, 30, 0), new TimeSpan(17, 15, 0), new TimeSpan(17, 15, 0), new TimeSpan(17, 15, 0)),
        new(5, new TimeSpan(17, 45, 0), new TimeSpan(18, 45, 0), new TimeSpan(18, 45, 0), new TimeSpan(19, 15, 0),
            AllowsMonetaryCredit: false,
            AllowsMonetaryDebit: false,
            AllowsCreditPrenotification: false,
            AllowsDebitPrenotification: false)
    ];

    public static IReadOnlyList<RegulatoryCycleSchedule> GetRequired(string clearingHouseCode)
        => clearingHouseCode.Trim().ToUpperInvariant() switch
        {
            AchColombiaCode => AchColombiaV35,
            CenitCode => CenitDsp152,
            _ => throw new InvalidOperationException($"No existe agenda regulatoria registrada para la cámara '{clearingHouseCode}'.")
        };
}

