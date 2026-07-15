using Cfa.ACHInterbank.Application.Helpers.AddressIp;
using Cfa.ACHInterbank.Application.Helpers.Middleware;
using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Api.Mappers.AchResponses;
using Cfa.ACHInterbank.Api.Validation.AchResponses;
using Cfa.ACHInterbank.Persistence.ACH.Services;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using NLog.Extensions.Logging;
using Npgsql;
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
            options.JsonSerializerOptions.MaxDepth = 128; // Profundidad máxima de serialización
            options.JsonSerializerOptions.WriteIndented = true;
            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        });


        services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = 10 * 1024 * 1024; // 10 MB
            options.MultipartHeadersLengthLimit = 16 * 1024;
        });

        services.AddEndpointsApiExplorer();
        services.AddOpenApi("v1", options =>
        {
            options.AddOperationTransformer<ScalarOperationDocumentationTransformer>();
            options.AddOperationTransformer<OpenApiSecurityMetadataTransformer>();
            options.AddDocumentTransformer<OpenApiBearerSecuritySchemeTransformer>();
        });

        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.MaxDepth = 256;
            options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });

        services.AddLogging(loggingBuilder =>
        {
            loggingBuilder.SetMinimumLevel(LogLevel.Debug);
            loggingBuilder.AddFilter("Microsoft.AspNetCore.DataProtection", LogLevel.Information);
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
            var origins = configuredOrigins?.Length > 0
                ? configuredOrigins
                : new[]
                {
                    "http://localhost:4200",
                    "https://localhost:4200",
                    "http://localhost:7269",
                    "https://localhost:7269",
                    "http://cfaach.ddns.net:743",
                    "http://192.168.150.6:843",
                    "http://192.168.150.6:743"
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
        services.AddScoped<ProcesarRespuestaAchApiMapper>();
        services.AddScoped<NotificarRespuestaAchApiMapper>();
        services.AddScoped<AchResponseQueryApiMapper>();
        services.AddScoped<ProcesarRespuestaAchRequestValidator>();
        services.AddScoped<NotificarRespuestaAchRequestValidator>();

        return services;
    }

    public static void ConfigureHandler(this WebApplication app)
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

        app.MapGet("/health/live", () =>
        {
            return Results.Ok(new
            {
                status = "Healthy",
                check = "live",
                service = "ACHInterbank",
                timestampUtc = DateTime.UtcNow
            });
        }).AllowAnonymous();

        app.MapGet("/health/ready", async (IServiceProvider services, CancellationToken ct) =>
        {
            var database = "Skipped";
            var dataProtectionKeyRing = "Skipped";
            var statusCode = StatusCodes.Status200OK;
            var status = "Healthy";

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(5));

            try
            {
                await using var scope = services.CreateAsyncScope();
                var dbContext = scope.ServiceProvider.GetService<AchDbContext>();
                if (dbContext is not null)
                {
                    var canConnect = await dbContext.Database.CanConnectAsync(timeoutCts.Token);
                    database = canConnect ? "Healthy" : "Unhealthy";
                    if (!canConnect)
                    {
                        status = "Unhealthy";
                        statusCode = StatusCodes.Status503ServiceUnavailable;
                    }
                    else
                    {
                        var hasKeys = await dbContext.DataProtectionKeys
                            .AsNoTracking()
                            .AnyAsync(timeoutCts.Token);
                        dataProtectionKeyRing = hasKeys ? "Healthy" : "Unhealthy";
                        if (!hasKeys)
                        {
                            status = "Unhealthy";
                            statusCode = StatusCodes.Status503ServiceUnavailable;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                database = "Unhealthy";
                dataProtectionKeyRing = "Unhealthy";
                status = "Unhealthy";
                statusCode = StatusCodes.Status503ServiceUnavailable;
            }
            catch
            {
                database = "Unhealthy";
                dataProtectionKeyRing = "Unhealthy";
                status = "Unhealthy";
                statusCode = StatusCodes.Status503ServiceUnavailable;
            }

            return Results.Json(new
            {
                status,
                check = "ready",
                database,
                dataProtectionKeyRing,
                timestampUtc = DateTime.UtcNow
            }, statusCode: statusCode);
        }).AllowAnonymous();

        app.UseMiddleware<GlobalExceptionMiddleware>();

        var applyMigrations = app.Configuration.GetValue("Database:ApplyMigrations", !IsRunningInContainer());
        if (applyMigrations)
        {
            using var scope = app.Services.CreateScope();
            AchDbContext Context = scope.ServiceProvider.GetRequiredService<AchDbContext>();
            try
            {
                EnsureLegacyPostgresTableNames(Context);
                Context.Database.Migrate();
            }
            catch (PostgresException ex) when (ex.SqlState == "3D000")
            {
                app.Logger.LogWarning("Database does not exist. Skipping migration at startup. Create DB first or disable Database:ApplyMigrations. Detail: {Detail}", ex.MessageText);
            }
        }
        else
        {
            app.Logger.LogInformation("Skipping database migrations. Set Database__ApplyMigrations=true to enable.");
        }

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

        app.UseWhen(
            context => !IsOpenApiOrScalarRequest(context.Request.Path),
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

    private static bool IsRunningInContainer()
    {
        return string.Equals(Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER"), "true", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsOpenApiOrScalarRequest(PathString path)
    {
        return path.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase)
               || path.StartsWithSegments("/scalar", StringComparison.OrdinalIgnoreCase)
               || path.StartsWithSegments("/index.html", StringComparison.OrdinalIgnoreCase);
    }

    private static void EnsureLegacyPostgresTableNames(AchDbContext context)
    {
        if (!context.Database.ProviderName?.Contains("Npgsql", StringComparison.OrdinalIgnoreCase) ?? true)
        {
            return;
        }

        const string sql = """
            DO $$
            BEGIN
                IF to_regclass('"AchBatches"') IS NULL AND to_regclass('achbatches') IS NOT NULL THEN
                    EXECUTE 'ALTER TABLE achbatches RENAME TO "AchBatches"';
                END IF;
                IF to_regclass('"AchTransactions"') IS NULL AND to_regclass('achtransactions') IS NOT NULL THEN
                    EXECUTE 'ALTER TABLE achtransactions RENAME TO "AchTransactions"';
                END IF;
                IF to_regclass('"AchCycles"') IS NULL AND to_regclass('achcycles') IS NOT NULL THEN
                    EXECUTE 'ALTER TABLE achcycles RENAME TO "AchCycles"';
                END IF;
                IF to_regclass('"ClearingHouses"') IS NULL AND to_regclass('clearinghouses') IS NOT NULL THEN
                    EXECUTE 'ALTER TABLE clearinghouses RENAME TO "ClearingHouses"';
                END IF;
                IF to_regclass('"IntegrationMethods"') IS NULL AND to_regclass('integrationmethods') IS NOT NULL THEN
                    EXECUTE 'ALTER TABLE integrationmethods RENAME TO "IntegrationMethods"';
                END IF;
                IF to_regclass('"IntegrationMethodParameters"') IS NULL AND to_regclass('integrationmethodparameters') IS NOT NULL THEN
                    EXECUTE 'ALTER TABLE integrationmethodparameters RENAME TO "IntegrationMethodParameters"';
                END IF;
                IF to_regclass('"IntegrationSourceCatalogFields"') IS NULL AND to_regclass('integrationsourcecatalogfields') IS NOT NULL THEN
                    EXECUTE 'ALTER TABLE integrationsourcecatalogfields RENAME TO "IntegrationSourceCatalogFields"';
                END IF;
                IF to_regclass('"IntegrationMappingSets"') IS NULL AND to_regclass('integrationmappingsets') IS NOT NULL THEN
                    EXECUTE 'ALTER TABLE integrationmappingsets RENAME TO "IntegrationMappingSets"';
                END IF;
                IF to_regclass('"IntegrationMappingRules"') IS NULL AND to_regclass('integrationmappingrules') IS NOT NULL THEN
                    EXECUTE 'ALTER TABLE integrationmappingrules RENAME TO "IntegrationMappingRules"';
                END IF;
                IF to_regclass('"IntegrationMappingSetHistory"') IS NULL AND to_regclass('integrationmappingsethistory') IS NOT NULL THEN
                    EXECUTE 'ALTER TABLE integrationmappingsethistory RENAME TO "IntegrationMappingSetHistory"';
                END IF;
            END $$;
            """;

        try
        {
            context.Database.ExecuteSqlRaw(sql);
        }
        catch (PostgresException ex) when (ex.SqlState == "3D000")
        {
            // DB inexistente: se maneja en ConfigureHandler para no romper el startup.
        }
    }
}
