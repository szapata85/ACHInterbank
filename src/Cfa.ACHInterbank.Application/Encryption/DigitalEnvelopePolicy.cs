using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Application.Encryption;

public interface IDigitalEnvelopePolicy
{
    bool ShouldEncrypt(int clearingHouseId);
}

public class DigitalEnvelopePolicy : IDigitalEnvelopePolicy
{
    private readonly DigitalEnvelopeOptions _options;

    public DigitalEnvelopePolicy(IOptions<DigitalEnvelopeOptions> options)
    {
        _options = options.Value ?? new DigitalEnvelopeOptions();
    }

    public bool ShouldEncrypt(int clearingHouseId)
    {
        return _options.EnabledClearingHouseIds.Contains(clearingHouseId);
    }
}
