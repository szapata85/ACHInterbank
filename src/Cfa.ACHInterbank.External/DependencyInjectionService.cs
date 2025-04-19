using System.Reflection;
using System.Text;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace Cfa.ACHInterbank.External;

public static class DependencyInjectionService
{
    private static readonly AppSettings _appSettings = AppSettings.Settings;

    public static IServiceCollection AddExternal(this IServiceCollection services, IConfiguration configuration)
    {

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(option =>
        {
            option.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_appSettings.TokenManager!.secretKetJwt!)),
                ValidIssuer = _appSettings.TokenManager.issuerJwt,
                ValidAudience = _appSettings.TokenManager.audienceJwt,
                ClockSkew = TimeSpan.Zero
            };
        });

        #region Services
        // Injection Dependency Traditional
        //services.AddSingleton<IConnectionAuth, ConnectionAuth>();
        //services.AddSingleton<IAuthenticationService, AuthenticationService>();
        //services.AddSingleton<IHttpClientService, HttpClientService>();
        //services.AddSingleton<IGenerateTokenExternal, GenerateTokenExternal>();

        // Injection Dependency Dynamic
        Assembly.GetExecutingAssembly().GetTypes()
          .Where(t => t.IsClass && !t.IsAbstract) // Filtra solo las clases concretas
          .SelectMany(implementationType => implementationType.GetInterfaces().Where(o => o.Name.Contains(implementationType.Name))
              .Select(interfacetype => new { Interface = interfacetype, Implementation = implementationType }))
          .ToList()
          .ForEach(pair =>
          {
              switch (true)
              {
                  case var _ when pair.Implementation.Name.Contains("Singleton"):
                      services.AddSingleton(pair.Interface, pair.Implementation);
                      break;
                  case var _ when pair.Implementation.Name.Contains("Scoped"):
                      services.AddScoped(pair.Interface, pair.Implementation);
                      break;
                  default:
                      services.AddTransient(pair.Interface, pair.Implementation);
                      break;
              }
          });

        #endregion Services


        return services;
    }
}
