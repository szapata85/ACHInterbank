using Cfa.ACHInterbank.Application.Helpers.AddressIp;
using Cfa.ACHInterbank.Application.Helpers.Middleware;
using Cfa.ACHInterbank.Persistence.ACH.Services;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using NLog.Extensions.Logging;
using System.Collections.Generic;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

namespace Cfa.ACHInterbank.Api;

public static class DependencyInjectionService
{
    private const string CorsPolicyName = "CorsPolicy";

    public static IServiceCollection AddWebApi(this IServiceCollection services)
    {
        // Configuración del Swagger
        services.AddSwaggerGen(option =>
        {
            option.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = "Swagger Document Architecture Cfa",
                Description = "Creación de plantilla generica arquitectura limpia CFA"
            });

            option.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Ingrese un token válido",
            });

            var bearerReference = new OpenApiSecuritySchemeReference("Bearer", new OpenApiDocument(), null);

            option.SwaggerGeneratorOptions.SecurityRequirements.Add(_ => new OpenApiSecurityRequirement
            {
                { bearerReference, new List<string>() }
            });

            string AssemblyName = Assembly.GetExecutingAssembly().GetName().Name!.Replace(".Api", ".Domain");

            //Assembly.Load(AssemblyName)
            //.GetTypes()
            //.Where(c => c.Namespace!.Contains(AssemblyName) && c.Name.EndsWith("Model", StringComparison.OrdinalIgnoreCase))
            //.ToList()
            //.ForEach(c =>
            //{
            //    option.MapType(c, () => new OpenApiSchema { Type = "object", Title = c.Name });
            //});


            var fileName = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            option.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, fileName));
        });

        // Configuración del formato Json que reciben los controladores
        services.AddControllers().AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.MaxDepth = 128; // Profundidad máxima de serialización
            options.JsonSerializerOptions.WriteIndented = true;

            options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;

        });

        // Está línea de código sirve para 
        //services.AddControllers(options =>
        //{
        //    options.Filters.Add<PostFilter>();
        //});

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
            builder
                .WithOrigins(
                    "http://localhost:4200",
                    "https://localhost:4200",
                    "http://localhost:7269",
                    "https://localhost:7269"
                )
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
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
            c.RoutePrefix = string.Empty;
        });

        app.UseMiddleware<GlobalExceptionMiddleware>();

        //// Configurar el middleware de autenticación MTLS
        //app.Use(async (context, next) =>
        //{
        //    X509Certificate2 clientCertificate = context.Connection.ClientCertificate!;

        //    if (clientCertificate == null || !clientCertificate.Verify())
        //    {
        //        context.Response.StatusCode = 401;
        //        context.Response.ContentType = "application/json";
        //        var response = ResponseApiService.Response(StatusCodes.Status401Unauthorized, "Debe enviar un certificado MTLS válido", "Unauthorized");
        //        await context.Response.WriteAsync(JsonConvert.SerializeObject(response));
        //        return;
        //    }

        //    // Realizar comprobaciones de validación adicionales si es necesario 

        //    await next.Invoke();
        //});



        var applyMigrations = app.Configuration.GetValue("Database:ApplyMigrations", !IsRunningInContainer());
        if (applyMigrations)
        {
            using var scope = app.Services.CreateScope();
            AchDbContext Context = scope.ServiceProvider.GetRequiredService<AchDbContext>();
            //var initializer = scope.ServiceProvider.GetRequiredService<AchInitializationService>();
            Context.Database.Migrate();

            //_ = initializer.InitializeAsync(); // Aquí se ejecuta tu lógica en runtime
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
        app.UseHttpsRedirection();
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
