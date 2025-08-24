using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Cfa.ACHInterbank.Application.External.TokenClient;
using Cfa.ACHInterbank.Application.Services.TokenClient.Model;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.IdentityModel.Tokens;


namespace Cfa.ACHInterbank.External.TokenClient;

[Transient]
public class GenerateTokenExternal : IGenerateTokenExternal
{
    private readonly AppSettings _appSettings = AppSettings.Settings;

    public GetTokenResponseModel GenerateToken(TokenModelClient model)
    {
        var accessExpiration = _appSettings.TokenManager!.accessExpiration;
        var expirationType = _appSettings.TokenManager!.expirationType;
        // authentication successful so generate jwt token
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_appSettings.TokenManager.secretKetJwt!);
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Issuer = _appSettings.TokenManager.issuerJwt,
            Audience = _appSettings.TokenManager.audienceJwt,
            Subject = new ClaimsIdentity(new Claim[]
            {
                    new Claim(ClaimTypes.Name, model.client_id!),
                    new Claim(ClaimTypes.NameIdentifier, model.client_secret!),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            }),
            Expires = expirationType!.Equals("m") ? (DateTime.UtcNow.AddMinutes(accessExpiration)) : DateTime.UtcNow.AddDays(accessExpiration),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature),
            TokenType = "Bearer Jwt"
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return new GetTokenResponseModel
        {
            access_token = tokenHandler.WriteToken(token),
            expires = tokenDescriptor.Expires,
            token_type = tokenDescriptor.TokenType,
        };

    }
}
