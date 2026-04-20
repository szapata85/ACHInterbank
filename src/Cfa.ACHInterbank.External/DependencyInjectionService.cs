using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Application.Services.Notifications.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.External.Notifications;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using System.Threading.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

namespace Cfa.ACHInterbank.External;

public static class DependencyInjectionService
{
    private static readonly AppSettings _appSettings = AppSettings.Settings;

    public static IServiceCollection AddExternal(this IServiceCollection services, IConfiguration configuration)
    {

        var resilienceSection = configuration.GetSection("Resilience:Soap");
        var timeoutSeconds = resilienceSection.GetValue<int?>("TimeoutSeconds") ?? 15;
        var maxRetryAttempts = resilienceSection.GetValue<int?>("MaxRetryAttempts") ?? 3;
        var retryBaseDelaySeconds = resilienceSection.GetValue<int?>("RetryBaseDelaySeconds") ?? 2;
        var breakerFailureRatio = resilienceSection.GetValue<double?>("CircuitBreaker:FailureRatio") ?? 0.5;
        var breakerSamplingSeconds = resilienceSection.GetValue<int?>("CircuitBreaker:SamplingDurationSeconds") ?? 30;
        var breakerMinimumThroughput = resilienceSection.GetValue<int?>("CircuitBreaker:MinimumThroughput") ?? 20;
        var breakerBreakSeconds = resilienceSection.GetValue<int?>("CircuitBreaker:BreakDurationSeconds") ?? 30;
        var outboundRatePermitLimit = resilienceSection.GetValue<int?>("RateLimit:PermitLimit") ?? 50;
        var outboundRateQueueLimit = resilienceSection.GetValue<int?>("RateLimit:QueueLimit") ?? 100;
        var outboundBulkheadPermitLimit = resilienceSection.GetValue<int?>("Bulkhead:PermitLimit") ?? 10;
        var outboundBulkheadQueueLimit = resilienceSection.GetValue<int?>("Bulkhead:QueueLimit") ?? 50;

        services.AddHttpClient(nameof(Connections.WscfaachSoapClient))
            .SetHandlerLifetime(TimeSpan.FromMinutes(10))
            .AddResilienceHandler("ach-soap-pipeline", pipeline =>
            {
                pipeline.AddRateLimiter(new HttpRateLimiterStrategyOptions
                {
                    DefaultRateLimiterOptions = new ConcurrencyLimiterOptions
                    {
                        PermitLimit = outboundRatePermitLimit,
                        QueueLimit = outboundRateQueueLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    }
                });

                pipeline.AddTimeout(TimeSpan.FromSeconds(timeoutSeconds));

                pipeline.AddRetry(new HttpRetryStrategyOptions
                {
                    MaxRetryAttempts = maxRetryAttempts,
                    BackoffType = DelayBackoffType.Exponential,
                    Delay = TimeSpan.FromSeconds(retryBaseDelaySeconds),
                    UseJitter = true
                });

                pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    FailureRatio = breakerFailureRatio,
                    SamplingDuration = TimeSpan.FromSeconds(breakerSamplingSeconds),
                    MinimumThroughput = breakerMinimumThroughput,
                    BreakDuration = TimeSpan.FromSeconds(breakerBreakSeconds)
                });

                pipeline.AddConcurrencyLimiter(outboundBulkheadPermitLimit, outboundBulkheadQueueLimit);
            });

        services.AddScoped<IEmailSender, LoggingEmailSender>();

        var validIssuer = configuration["appSettings:tokenManager:issuerJwt"]
            ?? _appSettings.TokenManager?.issuerJwt
            ?? string.Empty;
        var validAudience = configuration["appSettings:tokenManager:audienceJwt"]
            ?? _appSettings.TokenManager?.audienceJwt
            ?? string.Empty;
        var jwtSecret = ResolveJwtSecret(configuration);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(option =>
        {
            option.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ValidIssuer = validIssuer,
                ValidAudience = validAudience,
                NameClaimType = JwtRegisteredClaimNames.Sub,
                RoleClaimType = ClaimTypes.Role,
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
            options.AddPolicy("CanManageAch", policy => policy.RequireClaim("permission", "CanManageAch"));
            options.AddPolicy("CanReadAch", policy => policy.RequireClaim("permission", "CanReadAch"));
        });

        #region Services
        // Injection Dependency Traditional


        // Injection Dependency Dynamic
        List<Type> types = Assembly.GetExecutingAssembly().GetTypes()
            .Where(t => t.IsClass
            && !t.IsAbstract
            && !t.IsGenericTypeDefinition
            && !t.ContainsGenericParameters
            && !t.IsDefined(typeof(System.Runtime.CompilerServices.CompilerGeneratedAttribute), inherit: false)
            && (t.IsPublic || t.IsNestedPublic))
            .ToList();

        foreach (var implementationType in types)
        {
            Type? interfaceType = implementationType.GetInterfaces()
                .FirstOrDefault(i => i.Name == $"I{implementationType.Name}" || i == typeof(ITaskHandler));

            if (interfaceType == null) continue;

            if (implementationType.GetCustomAttribute<SingletonAttribute>() != null)
            {
                services.AddSingleton(interfaceType, implementationType);
            }
            else if (implementationType.GetCustomAttribute<ScopedAttribute>() != null)
            {
                services.AddScoped(interfaceType, implementationType);
            }
            else
            {
                services.AddTransient(interfaceType, implementationType);
            }
        }

        #endregion Services


        return services;
    }

    private static string ResolveJwtSecret(IConfiguration configuration)
    {
        var configured = configuration["appSettings:tokenManager:secretKetJwt"]
            ?? Environment.GetEnvironmentVariable("appSettings__tokenManager__secretKetJwt")
            ?? _appSettings.TokenManager?.secretKetJwt;

        if (string.IsNullOrWhiteSpace(configured))
        {
            throw new InvalidOperationException("Debe configurar appSettings__tokenManager__secretKetJwt mediante variables de entorno o secretos del entorno.");
        }

        return configured.Trim();
    }
}
