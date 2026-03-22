namespace Cfa.ACHInterbank.Application.Encryption;

public class DigitalEnvelopeOptions
{
    public HashSet<int> EnabledClearingHouseIds { get; set; } = new();
}
