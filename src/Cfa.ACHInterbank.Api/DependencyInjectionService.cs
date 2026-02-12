using Cfa.ACHInterbank.Application.Helpers.AddressIp;
using Cfa.ACHInterbank.Application.Helpers.Middleware;
using Cfa.ACHInterbank.Persistence.ACH.Services;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using NLog.Extensions.Logging;
using Scalar.AspNetCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

namespace Cfa.ACHInterbank.Api;

public static class DependencyInjectionService
{
    private const string CorsPolicyName = "CorsPolicy";

    public static IServiceCollection AddWebApi(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpenApi("v1");

        // Configuración del formato Json que reciben los controladores
        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.MaxDepth = 128; // Profundidad máxima de serialización
            options.JsonSerializerOptions.WriteIndented = true;

            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;

        });

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.SetMinimumLevel(LogLevel.Debug);
            loggingBuilder.AddNLog();
        });


        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                return RateLimitPartition.GetFixedWindowLimiter(
                    AddressIp.GetAddressIp(context.Request),
                    partition => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10, // Máximo de solicitudes
                        Window = TimeSpan.FromSeconds(1), // Por segundo
                        QueueLimit = 2,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });
        });

        services.AddCors(options => options.AddPolicy(CorsPolicyName, builder =>
        {
            var configuredOrigins = configuration.GetSection("Cors:Origins").Get<string[]>();
            var origins = configuredOrigins?.Length > 0
                ? configuredOrigins
                : new[]
                {
                    "http://localhost:4200",
                    "https://localhost:4200",
                    "http://localhost:7269",
                    "https://localhost:7269",
                    "http://cfaach.ddns.net:743",
                    "http://cfaach.ddns.net:744"
                };

            builder
                .WithOrigins(origins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        }));

        services.AddHttpClient();
        services.AddHttpContextAccessor();

        services.AddScoped<AchInitializationService>();

        return services;
    }

    public static void ConfigureHandler(this WebApplication app)
    {
        app.MapOpenApi("/openapi/{documentName}.json");
        app.MapScalarApiReference();
        app.MapGet("/", context =>
        {
            context.Response.Redirect("/scalar/v1");
            return Task.CompletedTask;
        });

        app.UseMiddleware<GlobalExceptionMiddleware>();

        var applyMigrations = app.Configuration.GetValue("Database:ApplyMigrations", !IsRunningInContainer());
        if (applyMigrations)
        {
            using var scope = app.Services.CreateScope();
            AchDbContext Context = scope.ServiceProvider.GetRequiredService<AchDbContext>();
            Context.Database.Migrate();
        }
        else
        {
            app.Logger.LogInformation("Skipping database migrations. Set Database__ApplyMigrations=true to enable.");
        }

        // Configure the HTTP request pipeline
        app.UseRouting();
        app.UseCors(CorsPolicyName);

        app.Use(async (context, next) =>
        {
            if (HttpMethods.IsOptions(context.Request.Method))
            {
                var origin = context.Request.Headers["Origin"].ToString();
                if (!string.IsNullOrEmpty(origin))
                {
                    context.Response.Headers.TryAdd("Access-Control-Allow-Origin", origin);
                    context.Response.Headers.TryAdd("Vary", "Origin");
                }

                context.Response.Headers.TryAdd("Access-Control-Allow-Credentials", "true");
                context.Response.Headers.TryAdd("Access-Control-Allow-Headers", "*");
                context.Response.Headers.TryAdd("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");

                context.Response.StatusCode = StatusCodes.Status200OK;
                await context.Response.CompleteAsync();
                return;
            }

            await next();
        });
        // Middleware Waf
        app.UseMiddleware<WafMiddleware>();
        // Middleware Log
        app.UseMiddleware<RequestResponseLoggingMiddleware>();
        // Middleware Token Expires
        app.UseMiddleware<TokenJwtMiddleware>();
        // Middleware Security Headers
        app.UseMiddleware<SecurityHeadersMiddleware>();
        if (app.Configuration.GetValue<bool>("EnableHttpsRedirection", true))
        {
            app.UseHttpsRedirection();
        }
        app.UseRateLimiter();
        //app.UseCsrfTokenMiddleware();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
    }

    private static bool IsRunningInContainer()
    {
        return string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase);
    }

}
