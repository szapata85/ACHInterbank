using Cfa.ACHInterbank.Application.Helpers.AddressIp;
using Cfa.ACHInterbank.Application.Helpers.Middleware;
using Cfa.ACHInterbank.Api.Configuration;
using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Api.Mappers.AchResponses;
using Cfa.ACHInterbank.Api.Validation.AchResponses;
using Cfa.ACHInterbank.Persistence.ACH.Services;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using NLog.Extensions.Logging;
using Npgsql;
using Scalar.AspNetCore;
using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Quartz;

namespace Cfa.ACHInterbank.Api;

public static class DependencyInjectionService
{
    private const string CorsPolicyName = "CorsPolicy";
    private const string OpenApiOutputCachePolicyName = "OpenApiDocument";

    public static IServiceCollection AddWebApi(this IServiceCollection services, IConfiguration configuration)
    {
        var inboundRateLimit = ApiRateLimitingOptions.FromConfiguration(configuration);

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
            options.MultipartBodyLengthLimit = 50 * 1024 * 1024; // Sobre digital: máximo controlado de 50 MB.
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
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, OpenApiSchemaJsonTypeInfoResolver.Create());
        });

        var openApiOutputCacheMinutes = Math.Clamp(
            configuration.GetValue<int?>("OpenApi:OutputCacheMinutes") ?? 15,
            1,
            1_440);
        services.AddOutputCache(options =>
        {
            options.AddPolicy(OpenApiOutputCachePolicyName, policy =>
            {
                policy.Expire(TimeSpan.FromMinutes(openApiOutputCacheMinutes));
            });
        });
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = ["application/json"];
        });
        services.Configure<BrotliCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
        });
        services.Configure<GzipCompressionProviderOptions>(options =>
        {
            options.Level = CompressionLevel.Fastest;
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
            options.OnRejected = (context, _) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        Math.Max(1, (int)Math.Ceiling(retryAfter.TotalSeconds)).ToString();
                }

                return ValueTask.CompletedTask;
            };
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                return RateLimitPartition.GetFixedWindowLimiter(
                    AddressIp.GetAddressIp(context.Request),
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = inboundRateLimit.PermitLimit,
                        Window = TimeSpan.FromSeconds(inboundRateLimit.WindowSeconds),
                        QueueLimit = inboundRateLimit.QueueLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        AutoReplenishment = true
                    });
            });
        });

        services.AddCors(options => options.AddPolicy(CorsPolicyName, builder =>
        {
            var origins = configuration.GetSection("Cors:Origins")
                .Get<string[]>()?
                .Where(origin => !string.IsNullOrWhiteSpace(origin))
                .Select(origin => origin.Trim().TrimEnd('/'))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? [];

            builder
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
                .WithExposedHeaders("Retry-After");

            if (origins.Length > 0)
            {
                builder.WithOrigins(origins);
            }
            else
            {
                // Fail closed when an environment has no explicit CORS configuration.
                builder.SetIsOriginAllowed(_ => false);
            }
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
        app.MapOpenApi("/openapi/{documentName}.json")
            .CacheOutput(OpenApiOutputCachePolicyName)
            .AllowAnonymous();
        app.MapScalarApiReference(options => options
                .DisableAgent()
                .DisableMcp()
                .HideDeveloperTools())
            .AllowAnonymous();

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
            var scheduler = "Skipped";
            var persistentStore = "Skipped";
            var clusterInstance = "Skipped";
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

                        var schedulerFactory = scope.ServiceProvider.GetService<ISchedulerFactory>();
                        if (schedulerFactory is not null)
                        {
                            var quartzScheduler = await schedulerFactory.GetScheduler(timeoutCts.Token);
                            var metadata = await quartzScheduler.GetMetaData(timeoutCts.Token);
                            scheduler = quartzScheduler.IsStarted && !quartzScheduler.IsShutdown ? "Healthy" : "Unhealthy";
                            persistentStore = metadata.JobStoreSupportsPersistence ? "Healthy" : "Unhealthy";
                            clusterInstance = await dbContext.SchedulerInstanceStates.AsNoTracking().AnyAsync(
                                x => x.SchedulerName == quartzScheduler.SchedulerName
                                     && x.InstanceId == quartzScheduler.SchedulerInstanceId,
                                timeoutCts.Token)
                                ? "Healthy"
                                : "Starting";

                            if (scheduler == "Unhealthy")
                            {
                                status = "Unhealthy";
                                statusCode = StatusCodes.Status503ServiceUnavailable;
                            }
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                database = "Unhealthy";
                dataProtectionKeyRing = "Unhealthy";
                scheduler = "Unhealthy";
                persistentStore = "Unhealthy";
                clusterInstance = "Unhealthy";
                status = "Unhealthy";
                statusCode = StatusCodes.Status503ServiceUnavailable;
            }
            catch
            {
                database = "Unhealthy";
                dataProtectionKeyRing = "Unhealthy";
                scheduler = "Unhealthy";
                persistentStore = "Unhealthy";
                clusterInstance = "Unhealthy";
                status = "Unhealthy";
                statusCode = StatusCodes.Status503ServiceUnavailable;
            }

            return Results.Json(new
            {
                status,
                check = "ready",
                database,
                dataProtectionKeyRing,
                scheduler,
                persistentStore,
                clusterInstance,
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

        if (app.Configuration.GetValue("Database:ApplySeed", false))
        {
            using var seedScope = app.Services.CreateScope();
            DbInitializer.SeedAllAsync(seedScope.ServiceProvider).GetAwaiter().GetResult();
            app.Logger.LogInformation("Database seed completed.");
        }

        app.UseRouting();
        app.UseCors(CorsPolicyName);

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
        app.UseWhen(
            context => context.Request.Path.StartsWithSegments("/openapi", StringComparison.OrdinalIgnoreCase),
            branch => branch.UseResponseCompression());
        app.UseOutputCache();
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
