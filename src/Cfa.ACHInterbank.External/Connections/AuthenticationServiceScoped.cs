using Cfa.ACHInterbank.Application.External.Connections;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.External.Connections;

public class AuthenticationServiceScoped : IAuthenticationServiceScoped
{
    private readonly IHttpClientServiceScoped _httpClientService;
    private readonly AppSettings appSettings = AppSettings.Settings;

    public AuthenticationServiceScoped(IHttpClientServiceScoped httpClientService)
    {
        _httpClientService = httpClientService;
    }

    public Task<string> GetTokenAsync()
    {
        throw new NotImplementedException();
    }
}
