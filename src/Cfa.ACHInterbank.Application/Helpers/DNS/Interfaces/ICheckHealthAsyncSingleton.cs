namespace Cfa.ACHInterbank.Application.Helpers.DNS.Interfaces;

public interface ICheckHealthAsyncSingleton
{
    Task CheckHealthAsync(string Type);
}
