using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Quartz;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;
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
        var connectionString = GetConnectionString(provider, configuration);
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

    private static string GetConnectionString(string provider, IConfiguration configuration)
    {
        var connectionStringName = provider?.Trim().ToLowerInvariant() switch
        {
            "postgres" or "postgresql" or "npgsql" => "PostgresConnection",
            _ => "SqlConnection"
        };

        var connectionString = configuration.GetConnectionString(connectionStringName);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Connection string '{connectionStringName}' is missing for provider '{provider}'.");
        }

        return connectionString;
    }
}
