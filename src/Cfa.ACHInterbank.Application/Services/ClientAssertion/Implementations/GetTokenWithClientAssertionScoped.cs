using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Cfa.ACHInterbank.Application.CacheMemory.Keys.Interfaces;
using Cfa.ACHInterbank.Application.External.Connections;
using Cfa.ACHInterbank.Application.Services.ClientAssertion.Interfaces;
using Cfa.ACHInterbank.Application.Services.ClientAssertion.Model;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.OpenData.Domain.Models.Response;
using Microsoft.IdentityModel.Tokens;
using static Cfa.ACHInterbank.Domain.Entities.JwksService.JwksService;

namespace Cfa.ACHInterbank.Application.Services.ClientAssertion.Implementations;

public class GetTokenWithClientAssertionScoped : IGetTokenWithClientAssertionScoped
{

    private readonly IKeysRepositorySingleton _keysRepository;
    private readonly IHttpClientServiceScoped _httpClient;
    private readonly AppSettings _appSettings = AppSettings.Settings;

    public GetTokenWithClientAssertionScoped(IKeysRepositorySingleton keysRepository, IHttpClientServiceScoped httpClient)
    {
        _keysRepository = keysRepository;
        _httpClient = httpClient;
    }

    public async Task<Response<TokenClientAssertion>> GenerateClientAssertion()
    {
        var response = new Response<TokenClientAssertion>();
        try
        {
            var ClientAssertion = CreateClientAssertion();
            var TokenClient = new SendClientAssertion
            {
                grant_type = _appSettings.TokenGeneric!.grant_type,
                client_assertion_type = _appSettings.TokenGeneric.client_assertion_type,
                client_assertion = ClientAssertion,
                scope = _appSettings.TokenGeneric.scope,
            };
            var url = "https://agregacioninformacionofcer.achcolombia.com.co:8446/realms/agregacion/protocol/openid-connect/token";
            response.Result = await _httpClient.SendPostRequestAsync<TokenClientAssertion>(url, TokenClient, HttpMethod.Post, TypeBody.Query, string.Empty);
            response.Success = true;
        }
        catch (Exception ex)
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
            new Claim(JwtRegisteredClaimNames.Iss, _appSettings.TokenGeneric!.clientId!),
            new Claim(JwtRegisteredClaimNames.Sub, _appSettings.TokenGeneric!.clientId!),
            new Claim(JwtRegisteredClaimNames.Aud, _appSettings.Integrations!.UrlAch!),
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
