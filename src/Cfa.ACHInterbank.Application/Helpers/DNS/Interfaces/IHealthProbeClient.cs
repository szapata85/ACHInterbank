namespace Cfa.ACHInterbank.Application.Helpers.DNS.Interfaces;

public interface IHealthProbeClient
{
    Task<bool> IsHealthyAsync(string url, CancellationToken cancellationToken = default);
}
