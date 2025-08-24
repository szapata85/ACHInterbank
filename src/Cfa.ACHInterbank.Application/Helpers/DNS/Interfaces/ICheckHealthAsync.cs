namespace Cfa.ACHInterbank.Application.Helpers.DNS.Interfaces;

public interface ICheckHealthAsync
{
    Task CheckHealthAsync(string Type);
}
