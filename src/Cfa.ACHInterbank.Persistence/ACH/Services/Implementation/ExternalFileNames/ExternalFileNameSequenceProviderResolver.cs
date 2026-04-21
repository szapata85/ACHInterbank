using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;

[Scoped]
public class ExternalFileNameSequenceProviderResolver : IExternalFileNameSequenceProviderResolver
{
    private readonly IReadOnlyList<IExternalFileNameSequenceProvider> _providers;

    public ExternalFileNameSequenceProviderResolver(IEnumerable<IExternalFileNameSequenceProvider> providers)
    {
        _providers = providers.ToList();
    }

    public IExternalFileNameSequenceProvider Resolve(string? providerName)
    {
        var provider = _providers.FirstOrDefault(x => x.CanHandle(providerName));
        if (provider is not null)
        {
            return provider;
        }

        throw new NotSupportedException($"No ExternalFileName sequence provider is registered for database provider '{providerName ?? "<null>"}'.");
    }
}
