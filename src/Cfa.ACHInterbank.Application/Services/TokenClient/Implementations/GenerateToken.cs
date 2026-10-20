using System.IdentityModel.Tokens.Jwt;
using Cfa.ACHInterbank.Application.CacheMemory.Tokens.Interfaces;
using Cfa.ACHInterbank.Application.External.ClientAssertion;
using Cfa.ACHInterbank.Application.External.TokenClient;
using Cfa.ACHInterbank.Application.Helpers.Logs.Interfaces;
using Cfa.ACHInterbank.Application.Services.TokenClient.Interfaces;
using Cfa.ACHInterbank.Application.Services.TokenClient.Model;
using Cfa.ACHInterbank.Domain.Entities.Token;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Application.Services.TokenClient.Implementations;

[Transient]
public class GenerateToken : IDisposable, IGenerateToken
{

    #region Diposable

    private bool disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {

            }
            disposed = true;
        }
    }

    // Destructor
    ~GenerateToken()
    {
        Dispose(false);
    }

    #endregion Diposable


    private readonly ILoggerManager _logger;
    private readonly IGenerateTokenExternal _tokenExternal;
    private readonly ITokenRepository _tokenRepository;
    private readonly IClientAssertionValidator _clientAssertion;
    private readonly Token _tokenSettings;

    public GenerateToken(ILoggerManager logger, IGenerateTokenExternal tokenExternal, ITokenRepository tokenRepository, IClientAssertionValidator clientAssertion, IOptions<Token> tokenOptions)
    {
        _logger = logger;
        _tokenExternal = tokenExternal;
        _tokenRepository = tokenRepository;
        _clientAssertion = clientAssertion;
        _tokenSettings = tokenOptions?.Value ?? throw new ArgumentNullException(nameof(tokenOptions));
    }
    public async Task<GetTokenResponseModel> GenerateTokenAsync(TokenModelClient model)
    {
        if ((model.client_id != _tokenSettings.clientId || model.client_secret != _tokenSettings.clientSecret || model.client_secret != _tokenSettings.x_api_key) || model is null)
        {
            _logger.LogError("Token creation rejected; client credentials are redacted.");
            throw new Exception("En el momento no podemos completar la operación, por favor intente mas tarde.");
        }

        var token = _tokenRepository.GetToken(_tokenSettings.clientId!);
        if (token is null)
        {
            var tokenModel = await Task.Run(() => new { GetTokenResponseModel = _tokenExternal.GenerateToken(model) });
            tokenModel.GetTokenResponseModel.expires_in = Math.Round((double)(tokenModel.GetTokenResponseModel.expires - DateTime.UtcNow)!.Value.TotalSeconds);
            //TimeZoneInfo bogotaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
            //tokenModel.GetTokenResponseModel.expires = TimeZoneInfo.ConvertTimeFromUtc((DateTime)tokenModel.GetTokenResponseModel.expires!, bogotaTimeZone);
            var tokenCache = new TokenCache
            {
                expires = tokenModel.GetTokenResponseModel.expires_in,
                expires_in = tokenModel.GetTokenResponseModel.expires,
                access_token = tokenModel.GetTokenResponseModel.access_token,
                token_type = tokenModel.GetTokenResponseModel.token_type,
                client_id = _tokenSettings.clientId,
            };
            _tokenRepository.AddToken(tokenCache);

            return tokenModel.GetTokenResponseModel;
        }

        var lst = _tokenRepository.ListToken();

        return new GetTokenResponseModel
        {
            expires_in = Math.Round((double)token.expires!)!,
            access_token = token.access_token,
            token_type = token.token_type,
        };

    }

    public async Task<GetTokenResponseModel> GenerateTokenAsync(string model)
    {
        if (_clientAssertion.ValidateAssertionAsync(model))
        {
            var TokenHandler = new JwtSecurityTokenHandler();
            var JwtToken = TokenHandler.ReadToken(model) as JwtSecurityToken;
            if (JwtToken != null)
            {
                var token = _tokenRepository.GetToken(_tokenSettings.clientId!);
                if (token is null)
                {
                    var claims = JwtToken.Claims.ToList();
                    var modelToken = new TokenModelClient
                    {
                        client_id = claims.FirstOrDefault(x => x.Type.Equals("iss"))!.ToString().Replace("iss: ", ""),
                        client_secret = claims.FirstOrDefault(x => x.Type.Equals("aud"))!.ToString().Replace("aud: ", ""),
                        client_key = claims.FirstOrDefault(x => x.Type.Equals("jti"))!.ToString().Replace("jti: ", ""),
                    };
                    var tokenModel = await Task.Run(() => new { GetTokenResponseModel = _tokenExternal.GenerateToken(modelToken) });
                    //TimeZoneInfo bogotaTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Bogota");
                    //tokenModel.GetTokenResponseModel.expires_in = TimeZoneInfo.ConvertTimeFromUtc((DateTime)tokenModel.GetTokenResponseModel.expires_in!, bogotaTimeZone);
                    tokenModel.GetTokenResponseModel.expires_in = Math.Round((double)(tokenModel.GetTokenResponseModel.expires - DateTime.UtcNow)!.Value.TotalSeconds);
                    var tokenCache = new TokenCache
                    {
                        expires = tokenModel.GetTokenResponseModel.expires_in,
                        access_token = tokenModel.GetTokenResponseModel.access_token,
                        token_type = tokenModel.GetTokenResponseModel.token_type,
                        client_id = _tokenSettings.clientId,
                    };
                    _tokenRepository.AddToken(tokenCache);

                    return tokenModel.GetTokenResponseModel;
                }

                var lst = _tokenRepository.ListToken();

                return new GetTokenResponseModel
                {
                    expires_in = Math.Round((double)token.expires!)!,
                    access_token = token.access_token,
                    token_type = token.token_type,
                };
            }
            else
            {
                _logger.LogError("Token creation rejected; client assertion claims are invalid and the assertion is redacted.");
                throw new Exception("En el momento no podemos completar la operación, por favor intente mas tarde.");
            }
        }
        _logger.LogError("Token creation rejected; client assertion validation failed and the assertion is redacted.");
        throw new Exception("En el momento no podemos completar la operación, por favor intente mas tarde.");

    }
}

