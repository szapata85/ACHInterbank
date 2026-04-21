using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.Mapping;
using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Quartz;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Mapping;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Quartz;
using Quartz.Simpl;
using System.Reflection;

namespace Cfa.ACHInterbank.Persistence;

public static class DependencyInjectionService
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<TransactionPolicyOptions>(configuration.GetSection("TransactionPolicies"));
        services.Configure<NachaGenerationOptions>(configuration.GetSection(NachaGenerationOptions.SectionName));
        //services.AddDbContext<DataBaseService>(options => options.UseAseClient(configuration.GetConnectionString("SybaseConnection")));

        //using (var connection = new AseConnection(""))
        //using (var commmand = connection.CreateCommand())
        //{
        //    connection.Open();
        //    //command stuff
        //}

        //services.AddDbContext<DataBaseService>(options => options.UseSqlServer(configuration.GetConnectionString("SqlConnection")));

        var commandTimeout = configuration.GetValue<int?>("Database:CommandTimeoutSeconds");
        var provider = configuration.GetValue<string>("Database:Provider") ?? "SqlServer";
        var maxRetryCount = configuration.GetValue<int?>("Database:MaxRetryCount") ?? 5;
        var maxRetryDelaySeconds = configuration.GetValue<int?>("Database:MaxRetryDelaySeconds") ?? 10;
        var maxRetryDelay = TimeSpan.FromSeconds(maxRetryDelaySeconds);
        var resolvedDatabase = ResolveDatabase(provider, configuration);
        provider = resolvedDatabase.Provider;
        var connectionString = resolvedDatabase.ConnectionString;
        services.AddDbContext<AchDbContext>(options =>
        {
            options.ConfigureWarnings(warnings =>
                warnings.Ignore(RelationalEventId.PendingModelChangesWarning));

            switch (provider.Trim().ToLowerInvariant())
            {
                case "sqlserver":
                case "mssql":
                    options.UseSqlServer(
                        connectionString,
                        sqlOptions =>
                        {
                            if (commandTimeout.HasValue)
                            {
                                sqlOptions.CommandTimeout(commandTimeout.Value);
                            }

                            sqlOptions.EnableRetryOnFailure(
                                maxRetryCount: maxRetryCount,
                                maxRetryDelay: maxRetryDelay,
                                errorNumbersToAdd: null);
                        });
                    break;
                case "postgres":
                case "postgresql":
                case "npgsql":
                    options.UseNpgsql(
                        connectionString,
                        npgsqlOptions =>
                        {
                            if (commandTimeout.HasValue)
                            {
                                npgsqlOptions.CommandTimeout(commandTimeout.Value);
                            }

                            npgsqlOptions.EnableRetryOnFailure(
                                maxRetryCount: maxRetryCount,
                                maxRetryDelay: maxRetryDelay,
                                errorCodesToAdd: null);
                        });
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported database provider '{provider}'.");
            }
        });

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<IExternalFileNameSequenceProvider, PostgresExternalFileNameSequenceService>();
        services.AddScoped<IExternalFileNameSequenceProvider, SqlServerExternalFileNameSequenceService>();
        services.AddScoped<IExternalFileNameSequenceProvider, EfGenericExternalFileNameSequenceService>();

        services.AddQuartz(q =>
        {
            // Quartz usará el contenedor de DI para crear los Jobs
            q.UseJobFactory<MicrosoftDependencyInjectionJobFactory>();
        });

        services.AddQuartzHostedService(opt =>
        {
            opt.WaitForJobsToComplete = true;
        });

        // Servicio que sincroniza DB → Quartz
        services.AddHostedService<SchedulerSyncService>();
        services.AddTransient<ProcessBulkIngestionBatchJob>();
        services.AddScoped<IBulkFileParser, ACH.Services.Implementation.BulkParsers.JsonBulkFileParser>();
        services.AddScoped<IBulkFileParser, ACH.Services.Implementation.BulkParsers.CsvBulkFileParser>();
        services.AddScoped<IBulkFileParser, ACH.Services.Implementation.BulkParsers.ExcelBulkFileParser>();
        services.AddScoped<ExpressionDslEngine>();
        services.AddScoped<IExpressionDslCompiler>(sp => sp.GetRequiredService<ExpressionDslEngine>());
        services.AddScoped<IExpressionDslExecutor>(sp => sp.GetRequiredService<ExpressionDslEngine>());




        //Injection
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
                .FirstOrDefault(i => i.Name == $"I{implementationType.Name}" || i == typeof(ITaskHandler) || i == typeof(IDbSeeder));

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


        // Ejecuta seeders al inicio    
        //_ = DbInitializer.SeedAllAsync(services.BuildServiceProvider());

        return services;
    }

    private static (string Provider, string ConnectionString) ResolveDatabase(string provider, IConfiguration configuration)
    {
        var normalizedProvider = provider?.Trim().ToLowerInvariant() switch
        {
            "postgres" or "postgresql" or "npgsql" => "postgres",
            _ => "sqlserver"
        };

        var sqlConnection = configuration.GetConnectionString("SqlConnection");
        var postgresConnection = configuration.GetConnectionString("PostgresConnection");

        if (normalizedProvider == "postgres" && !string.IsNullOrWhiteSpace(postgresConnection))
        {
            return ("postgres", postgresConnection);
        }

        if (normalizedProvider == "sqlserver" && !string.IsNullOrWhiteSpace(sqlConnection))
        {
            return ("sqlserver", sqlConnection);
        }

        if (!string.IsNullOrWhiteSpace(postgresConnection))
        {
            return ("postgres", postgresConnection);
        }

        if (!string.IsNullOrWhiteSpace(sqlConnection))
        {
            return ("sqlserver", sqlConnection);
        }

        throw new InvalidOperationException(
            $"No configured connection string was found for provider '{provider}'. Configure either 'ConnectionStrings:PostgresConnection' or 'ConnectionStrings:SqlConnection'.");
    }
}
