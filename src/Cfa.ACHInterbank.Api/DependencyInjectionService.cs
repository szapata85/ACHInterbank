using Cfa.ACHInterbank.Application.Helpers.AddressIp;
using Cfa.ACHInterbank.Application.Helpers.Middleware;
using Cfa.ACHInterbank.Persistence.ACH.Services;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System;
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

            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Ingrese un token válido",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            };

            option.AddSecurityDefinition("Bearer", securityScheme);

            option.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                { securityScheme, Array.Empty<string>() }
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
                .WithOrigins("http://localhost:4200", "https://localhost:4200")
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



        using (var scope = app.Services.CreateScope())
        {
            AchDbContext Context = scope.ServiceProvider.GetRequiredService<AchDbContext>();
            //var initializer = scope.ServiceProvider.GetRequiredService<AchInitializationService>();
            Context.Database.Migrate();

            //_ = initializer.InitializeAsync(); // Aquí se ejecuta tu lógica en runtime
        }



        // Configure the HTTP request pipeline
        app.UseMiddleware<GlobalExceptionMiddleware>();
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
        app.UseRouting();
        //app.UseCsrfTokenMiddleware();
        app.UseCors(CorsPolicyName);
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
    }

}
