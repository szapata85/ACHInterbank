using Cfa.ACHInterbank.Api.Helpers.AddressIp;
using Cfa.ACHInterbank.Api.Filters;
using Cfa.ACHInterbank.Api.Logging;
using Cfa.ACHInterbank.Api.Middleware;
using Cfa.ACHInterbank.Api.Security;
using Cfa.ACHInterbank.Application.Helpers.Logs.Interfaces;
using Cfa.ACHInterbank.Application.Security.Interfaces;
using Cfa.ACHInterbank.Api.Configuration;
using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Api.Mappers.AchResponses;
using Cfa.ACHInterbank.Api.Validation.AchResponses;
using Cfa.ACHInterbank.Persistence.ACH.Services;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.Extensions.Options;
using System.Text.Json.Schema;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using NLog.Extensions.Logging;
using Npgsql;
using Scalar.AspNetCore;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Quartz;

namespace Cfa.ACHInterbank.Api;

public static class DependencyInjectionService
{
    private static IEnumerable<Type> EnumerateSchemaTypes(Type type)
    {
        yield return type;

        if (type.HasElementType && type.GetElementType() is { } elementType)
        {
            foreach (var nested in EnumerateSchemaTypes(elementType))
                yield return nested;
        }

        if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments())
                foreach (var nested in EnumerateSchemaTypes(argument))
                    yield return nested;
        }
    }

    private const string CorsPolicyName = "CorsPolicy";

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
            options.AddOperationTransformer<OpenApiOperationIdTransformer>();
            options.AddOperationTransformer<OpenApiIntegrationTagsTransformer>();
            options.AddOperationTransformer<OpenApiSc508PublicContractDocumentationTransformer>();
            options.AddOperationTransformer<OpenApiTransversalHeadersTransformer>();
            options.AddOperationTransformer<OpenApiExamplesTransformer>();
            options.AddOperationTransformer<OpenApiResponseDescriptionsTransformer>();
            options.AddOperationTransformer<OpenApiSecurityMetadataTransformer>();
            options.AddDocumentTransformer<OpenApiDocumentMetadataTransformer>();
            options.AddDocumentTransformer<OpenApiBearerSecuritySchemeTransformer>();
        });

        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.MaxDepth = 256;
            options.SerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.SerializerOptions.TypeInfoResolverChain.Insert(0, OpenApiSchemaJsonTypeInfoResolver.Create());
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
        services.AddScoped<ICurrentUserContext, HttpCurrentUserContext>();
        services.AddTransient<ILoggerManager, LoggerManager>();
        services.AddScoped<PostFilter>();
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
        // Keep the OpenAPI endpoint deliberately simple. Scalar consumes this exact route.
        // Caching/compression at this layer can make a generated document failure look like
        // a Scalar UI problem, so the documentation endpoint is served directly.
        app.MapOpenApi("/openapi/{documentName}.json")
            .AllowAnonymous();
        app.MapScalarApiReference(options => options
                .WithOpenApiRoutePattern("/openapi/{documentName}.json")
                .DisableAgent()
                .DisableMcp()
                .HideDeveloperTools())
            .AllowAnonymous();

        if (app.Environment.IsDevelopment())
        {
            app.MapGet("/openapi/_diagnostics", async (IServiceProvider services, CancellationToken cancellationToken) =>
            {
                try
                {
                    var provider = services.GetRequiredKeyedService<IOpenApiDocumentProvider>("v1");
                    var document = await provider.GetOpenApiDocumentAsync(cancellationToken);
                    await using var stream = new MemoryStream();
                    await document.SerializeAsJsonAsync(stream, OpenApiSpecVersion.OpenApi3_1, cancellationToken);

                    return Results.Ok(new
                    {
                        document = "v1",
                        status = "Healthy",
                        serializedBytes = stream.Length
                    });
                }
                catch (Exception exception)
                {
                    return Results.Problem(
                        title: "OpenAPI generation failed",
                        detail: exception.ToString(),
                        statusCode: StatusCodes.Status500InternalServerError);
                }
            }).AllowAnonymous().ExcludeFromDescription();

            // Development-only: isolate the response contract whose JSON schema cannot be exported.
            // This operates before custom OpenAPI transformers, where Guid/default-value failures occur.
            app.MapGet("/openapi/_schema-diagnostics", (
                IApiDescriptionGroupCollectionProvider descriptions,
                IOptions<Microsoft.AspNetCore.Http.Json.JsonOptions> jsonOptions) =>
            {
                var types = descriptions.ApiDescriptionGroups.Items
                    .SelectMany(group => group.Items)
                    .SelectMany(api => api.SupportedResponseTypes)
                    .Select(response => response.Type)
                    .Where(type => type is not null)
                    .SelectMany(type => EnumerateSchemaTypes(type!))
                    .Where(type => type.Namespace?.StartsWith("Cfa.ACHInterbank", StringComparison.Ordinal) == true)
                    .Distinct()
                    .OrderBy(type => type.FullName, StringComparer.Ordinal)
                    .ToArray();

                var failures = new List<object>();
                foreach (var type in types)
                {
                    try
                    {
                        _ = jsonOptions.Value.SerializerOptions.GetJsonSchemaAsNode(type);
                    }
                    catch (Exception exception)
                    {
                        failures.Add(new { type = type.FullName, error = exception.ToString() });
                    }
                }

                return Results.Ok(new { schemaTypesChecked = types.Length, failures });
            }).AllowAnonymous().ExcludeFromDescription();
        }

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

        app.MapPublicHealthEndpoints();

        app.UseMiddleware<GlobalExceptionMiddleware>();

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

        // Scalar and the generated OpenAPI document are documentation endpoints.
        // They must not compete with the API fixed-window limiter during page bootstrap/reloads.
        app.UseWhen(
            context => !IsOpenApiOrScalarRequest(context.Request.Path),
            branch => branch.UseRateLimiter());
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
    }


    public static void MapPublicHealthEndpoints(this WebApplication app)
    {
        var liveEndpoint = app.MapGet("/health/live", () =>
        {
            return Results.Ok(new
            {
                status = "Healthy",
                check = "live",
                service = "ACHInterbank",
                timestampUtc = DateTime.UtcNow
            });
        }).AllowAnonymous();
        liveEndpoint
            .WithTags("Auditoría y observabilidad")
            .WithSummary("Consultar liveness del servicio")
            .WithDescription("Expone la señal pública de liveness del proceso. Responde 200 con un body JSON anónimo que contiene status=Healthy, check=live, service=ACHInterbank y timestampUtc; no ejecuta comprobaciones de dependencias.")
            .Produces(StatusCodes.Status200OK, contentType: "application/json");

        var readyEndpoint = app.MapGet("/health/ready", async (IServiceProvider services, CancellationToken ct) =>
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
        readyEndpoint
            .WithTags("Auditoría y observabilidad")
            .WithSummary("Consultar readiness del servicio")
            .WithDescription("Expone la señal pública de readiness. Conserva las comprobaciones existentes de conectividad de base de datos, presencia de claves de Data Protection y estado del scheduler cuando esos servicios están registrados, con timeout enlazado de 5 segundos. Responde 200 o 503 con el mismo body JSON anónimo que contiene status, check=ready, database, dataProtectionKeyRing, scheduler, persistentStore, clusterInstance y timestampUtc.")
            .Produces(StatusCodes.Status200OK, contentType: "application/json")
            .Produces(StatusCodes.Status503ServiceUnavailable, contentType: "application/json");
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
