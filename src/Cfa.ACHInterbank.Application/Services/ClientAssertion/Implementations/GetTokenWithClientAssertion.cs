using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Cfa.ACHInterbank.Application.CacheMemory.Keys.Interfaces;
using Cfa.ACHInterbank.Application.External.Connections;
using Cfa.ACHInterbank.Application.Services.ClientAssertion.Interfaces;
using Cfa.ACHInterbank.Application.Services.ClientAssertion.Model;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using IntegrationOptions = Cfa.ACHInterbank.Domain.Models.Configurations.Integrations;
using Cfa.OpenData.Domain.Models.Response;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using static Cfa.ACHInterbank.Domain.Entities.JwksService.JwksService;

namespace Cfa.ACHInterbank.Application.Services.ClientAssertion.Implementations;

[Scoped]
public class GetTokenWithClientAssertion : IGetTokenWithClientAssertion
{

    private readonly IKeysRepository _keysRepository;
    private readonly IHttpClientService _httpClient;
    private readonly TokenGeneric _tokenSettings;
    private readonly IntegrationOptions _integrationSettings;

    public GetTokenWithClientAssertion(
        IKeysRepository keysRepository,
        IHttpClientService httpClient,
        IOptions<TokenGeneric> tokenOptions,
        IOptions<IntegrationOptions> integrationOptions)
    {
        _keysRepository = keysRepository;
        _httpClient = httpClient;
        _tokenSettings = tokenOptions?.Value ?? throw new ArgumentNullException(nameof(tokenOptions));
        _integrationSettings = integrationOptions?.Value ?? throw new ArgumentNullException(nameof(integrationOptions));
    }

    public async Task<Response<TokenClientAssertion>> GenerateClientAssertion()
    {
        var response = new Response<TokenClientAssertion>();
        try
        {
            var ClientAssertion = CreateClientAssertion();
            var TokenClient = new SendClientAssertion
            {
                grant_type = _tokenSettings.grant_type,
                client_assertion_type = _tokenSettings.client_assertion_type,
                client_assertion = ClientAssertion,
                scope = _tokenSettings.scope,
            };
            var url = "https://agregacioninformacionofcer.achcolombia.com.co:8446/realms/agregacion/protocol/openid-connect/token";
            response.Result = await _httpClient.SendPostRequestAsync<TokenClientAssertion>(url, TokenClient, HttpMethod.Post, TypeBody.Query, string.Empty);
            response.Success = true;
        }
        catch
        {
            response.Success = false;
            //response.Errors = ex.Message.ToString();
        }

        return response;

    }

    private string CreateClientAssertion()
    {
        var now = DateTime.UtcNow;
        var expirationTime = now.AddMinutes(10);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Iss, _tokenSettings.clientId!),
            new Claim(JwtRegisteredClaimNames.Sub, _tokenSettings.clientId!),
            new Claim(JwtRegisteredClaimNames.Aud, _integrationSettings.UrlAch!),
            new Claim(JwtRegisteredClaimNames.Exp, ((int)(expirationTime - DateTime.UnixEpoch).TotalSeconds).ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, ((int)(now - DateTime.UnixEpoch).TotalSeconds).ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        };

        var rsa = GetKeyRSA();
        var signingCredentials = new SigningCredentials(new RsaSecurityKey(rsa.privateKey), SecurityAlgorithms.RsaSha256);

        var rsas = rsa.publicKey.ExportParameters(false);

        var key = new Key
        {
            kty = "RSA",
            alg = SecurityAlgorithms.RsaSha256,
            use = "sig",
            kid = Guid.NewGuid().ToString(),
            n = Base64UrlEncoder.Encode(rsas.Modulus),
            e = Base64UrlEncoder.Encode(rsas.Exponent),
            Expire = DateTime.Now.AddMinutes(10)
        };

        _keysRepository.AddKey(key);

        var token = new JwtSecurityToken(
            //issuer: clientId,
            //audience: tokenEndpoint,
            claims: claims,
            expires: expirationTime,
            signingCredentials: signingCredentials
            );

        var tokenHandler = new JwtSecurityTokenHandler();
        return tokenHandler.WriteToken(token);
    }

    private (RSA privateKey, RSA publicKey) GetKeyRSA()
    {
        var rsa = RSA.Create(2048);
        var privateKey = rsa.ExportRSAPrivateKey();
        var publicKey = rsa.ExportRSAPublicKey();
        return (privateKey: rsa, publicKey: rsa);
    }

}
