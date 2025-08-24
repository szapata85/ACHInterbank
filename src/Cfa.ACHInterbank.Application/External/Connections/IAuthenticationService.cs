namespace Cfa.ACHInterbank.Application.External.Connections;

public interface IAuthenticationService
{
    Task<string> GetTokenAsync();
}
