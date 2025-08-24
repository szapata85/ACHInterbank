using Cfa.ACHInterbank.Application.External.Connections;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.External.Connections;

[Scoped]
public class AuthenticationService : IAuthenticationService
{
    private readonly IHttpClientService _httpClientService;
    private readonly AppSettings appSettings = AppSettings.Settings;

    public AuthenticationService(IHttpClientService httpClientService)
    {
        _httpClientService = httpClientService;
    }

    public Task<string> GetTokenAsync()
    {
        throw new NotImplementedException();
    }
}
