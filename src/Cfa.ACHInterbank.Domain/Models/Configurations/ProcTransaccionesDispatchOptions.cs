namespace Cfa.ACHInterbank.Domain.Models.Configurations;

public sealed class ProcTransaccionesDispatchOptions
{
    public const string SectionName = "ProcTransacciones";

    public string Mode { get; set; } = "Live";

    public bool UseContractDrivenRequest { get; set; }

    public string NormalizedMode
        => string.IsNullOrWhiteSpace(Mode) ? "Live" : Mode.Trim();

    public bool IsLive
        => string.Equals(NormalizedMode, "Live", StringComparison.OrdinalIgnoreCase);

    public bool IsDisabled
        => string.Equals(NormalizedMode, "Disabled", StringComparison.OrdinalIgnoreCase);

    public bool IsDryRunLike
        => !IsLive && !IsDisabled;
}
