using Cfa.ACHInterbank.Application.Helpers.AddressIp;
using Cfa.ACHInterbank.Application.Helpers.Middleware;
using Cfa.ACHInterbank.Persistence.ACH.Services;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
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
        // Configuración del formato Json que reciben los controladores
        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.MaxDepth = 64;
            options.JsonSerializerOptions.WriteIndented = false;
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        });


        services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10 MB
            options.MultipartHeadersLengthLimit = 16 * 1024;
        });

        services.AddEndpointsApiExplorer();
        services.AddOpenApi("v1");

        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.MaxDepth = 64;
            options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
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
                        PermitLimit = 10,
                        Window = TimeSpan.FromSeconds(1),
                        QueueLimit = 2,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    });
            });
        });

        services.AddCors(options => options.AddPolicy(CorsPolicyName, builder =>
        {
            var configuredOrigins = configuration.GetSection("Cors:Origins").Get<string[]>();
            var origins = configuredOrigins?.Where(origin => !string.IsNullOrWhiteSpace(origin)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
                ?? [];

            if (origins.Length > 0)
            {
                builder
                    .WithOrigins(origins)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            }
        }));

        services.AddHttpClient();
        services.AddHttpContextAccessor();
        services.AddScoped<AchInitializationService>();

        return services;
    }

    public static void ConfigureHandler(this WebApplication app)
    {
        var exposeApiDocs = app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("OpenApi:Expose", false);
        if (exposeApiDocs)
        {
            app.MapOpenApi("/openapi/{documentName}.json").AllowAnonymous();
            app.MapScalarApiReference().AllowAnonymous();

            app.MapGet("/", context =>
            {
                context.Response.Redirect("/scalar");
                return Task.CompletedTask;
            }).AllowAnonymous();

            app.MapGet("/index.html", context =>
            {
                context.Response.Redirect("/scalar");
                return Task.CompletedTask;
            }).AllowAnonymous();
        }

        app.UseMiddleware<GlobalExceptionMiddleware>();

        var applyMigrations = app.Configuration.GetValue("Database:ApplyMigrations", false);
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

        app.UseRouting();
        app.UseCors(CorsPolicyName);

        app.UseWhen(
            context => !exposeApiDocs || !IsOpenApiOrScalarRequest(context.Request.Path),
            branch =>
            {
                branch.UseMiddleware<WafMiddleware>();
                branch.UseMiddleware<RequestResponseLoggingMiddleware>();
                branch.UseMiddleware<TokenJwtMiddleware>();
            });

        app.UseMiddleware<SecurityHeadersMiddleware>();

        if (app.Configuration.GetValue<bool>("EnableHttpsRedirection", true))
        {
            app.UseHttpsRedirection();
        }

        app.UseRateLimiter();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
    }

    private static bool IsOpenApiOrScalarRequest(PathString path)
    {
        return path.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase)
               || path.StartsWithSegments("/scalar", StringComparison.OrdinalIgnoreCase)
               || path.StartsWithSegments("/index.html", StringComparison.OrdinalIgnoreCase);
    }
}
