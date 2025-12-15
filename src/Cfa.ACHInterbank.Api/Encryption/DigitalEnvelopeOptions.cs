using System.Collections.Generic;

namespace Cfa.ACHInterbank.Api.Encryption;

public class DigitalEnvelopeOptions
{
    public HashSet<int> EnabledClearingHouseIds { get; set; } = new();
}
