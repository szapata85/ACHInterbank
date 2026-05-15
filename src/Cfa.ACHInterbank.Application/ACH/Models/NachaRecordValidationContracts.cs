namespace Cfa.ACHInterbank.Application.ACH.Models;

public enum NachaRecordValidationSeverity { Info, Warning, Error }

public sealed record NachaRecordValidationIssue(string Code, NachaRecordValidationSeverity Severity, string Message);

public sealed record NachaRecordValidationResult(bool IsValid, IReadOnlyList<NachaRecordValidationIssue> Issues)
{
    public bool HasErrors => Issues.Any(x => x.Severity == NachaRecordValidationSeverity.Error);
}

public sealed record NachaRecordValidationContext(
    int ClearingHouseId,
    string? ClearingHouseCode,
    NachaRecordFlow Flow,
    NachaRecordDirection Direction,
    NachaRailRecordConfig Config,
    string NachaContent,
    bool IsCurrentLayoutMode = true);
