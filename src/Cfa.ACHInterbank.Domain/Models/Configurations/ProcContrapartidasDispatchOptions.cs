namespace Cfa.ACHInterbank.Domain.Models.Configurations;

public sealed class ProcContrapartidasDispatchOptions
{
    public const string SectionName = "ProcContrapartidas";

    public string Mode { get; set; } = "DryRun";

    public bool UseContractDrivenRequest { get; set; }

    public string NormalizedMode
        => string.IsNullOrWhiteSpace(Mode) ? "DryRun" : Mode.Trim();

    public bool IsLive
        => string.Equals(NormalizedMode, "Live", StringComparison.OrdinalIgnoreCase);

    public bool IsDisabled
        => string.Equals(NormalizedMode, "Disabled", StringComparison.OrdinalIgnoreCase);

    public bool IsDryRunLike
        => !IsLive && !IsDisabled;
}
