namespace Cfa.ACHInterbank.Application.External.Connections;

public interface IAuthenticationServiceScoped
{
    Task<string> GetTokenAsync();
}
