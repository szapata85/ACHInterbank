namespace Cfa.ACHInterbank.Tests.NachaFunctional;

internal sealed class NachaFunctionalScenario
{
    public required string ScenarioId { get; init; }
    public required string ClearingHouseCode { get; init; }
    public required string ProfileCode { get; init; }
    public required string FlowType { get; init; }
    public required string ExpectedFileName { get; init; }
    public required string ExpectedGoldenFilePath { get; init; }
    public required NachaExpectedControlTotals ExpectedTotals { get; init; }
    public bool CompareByteByByte { get; init; } = true;
    public bool NormalizeLineEndingsBeforeComparison { get; init; } = true;
}

internal sealed class NachaExpectedControlTotals
{
    public int BatchCount { get; init; }
    public int BlockCount { get; init; }
    public int EntryAddendaCount { get; init; }
    public long EntryHash { get; init; }
    public long TotalDebitAmountInCents { get; init; }
    public long TotalCreditAmountInCents { get; init; }
    public int PhysicalRecordCountBeforePadding { get; init; }
    public int PaddingRecordCount { get; init; }
    public int PhysicalRecordCountAfterPadding { get; init; }
}

internal sealed class NachaFunctionalValidationResult
{
    public required string ScenarioId { get; init; }
    public required string Status { get; init; }
    public string? Message { get; init; }
}
