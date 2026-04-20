namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;

public interface IExternalFileNameSequenceProviderResolver
{
    IExternalFileNameSequenceProvider Resolve(string? providerName);
}
