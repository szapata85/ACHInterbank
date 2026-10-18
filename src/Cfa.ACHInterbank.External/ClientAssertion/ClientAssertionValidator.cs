using System.IdentityModel.Tokens.Jwt;
using System.Text.Json;
using AutoMapper;
using Cfa.ACHInterbank.Application.Configuration;
using Cfa.ACHInterbank.Application.External.ClientAssertion;
using Cfa.ACHInterbank.Application.External.JwksService;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using static Cfa.ACHInterbank.Domain.Entities.JwksService.JwksService;

namespace Cfa.ACHInterbank.External.ClientAssertion;

[Scoped]
public class ClientAssertionValidator : IClientAssertionValidator
{
    private readonly IJwksService _jwksService;
    private readonly IMapper _mapper = MapperBootstrapper.Instance;
    private readonly TokenGeneric _tokenSettings;
    private readonly Integrations _integrationSettings;


    public ClientAssertionValidator(
        IJwksService jwksService,
        IOptions<TokenGeneric> tokenOptions,
        IOptions<Integrations> integrationOptions)
    {
        _jwksService = jwksService;
        _tokenSettings = tokenOptions?.Value ?? throw new ArgumentNullException(nameof(tokenOptions));
        _integrationSettings = integrationOptions?.Value ?? throw new ArgumentNullException(nameof(integrationOptions));
    }
    public bool ValidateAssertionAsync(string assertion)
    {

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(assertion);

        //Obtener clave pública del cliente desde JWKSWW
        var jwks = _jwksService.GetPublicJwks().Result!.keys;
        foreach (var jwk in jwks!)
        {
            try
            {
                var key = FindMatchingKey(jwk);

                if (key == null) return false;

                var parameters = new TokenValidationParameters
                {
                    ValidIssuer = _tokenSettings.clientId,
                    ValidAudience = _integrationSettings.UrlAch,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true
                };

                handler.ValidateToken(assertion, parameters, out _);
                return true;
            }
            catch (Exception) { }

        }

        return false;
    }

    private SecurityKey FindMatchingKey(Key jwks)
    {
        return jwks != null ? new JsonWebKey(JsonSerializer.Serialize(jwks)) : null!;
    }
}

