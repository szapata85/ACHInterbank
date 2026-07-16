using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Interfaces.Mapping;
using Cfa.ACHInterbank.Application.ACHSobreDigital.CertificateManagement;
using Cfa.ACHInterbank.Application.ACHSobreDigital.ManagedDigitalEnvelope;
using Cfa.ACHInterbank.Application.ACHSobreDigital.Operations;
using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Quartz;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Calendar;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Mapping;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
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
        services.Configure<NachaInboundSimulatorOptions>(configuration.GetSection(NachaInboundSimulatorOptions.SectionName));
        services.Configure<CertificateSecretResolverOptions>(configuration.GetSection("DigitalEnvelope:CertificateSecretResolver"));
        services.Configure<DigitalEnvelopeCertificateBootstrapOptions>(configuration.GetSection(DigitalEnvelopeCertificateBootstrapOptions.SectionName));
        services.Configure<IncomingNachaDispatchResilienceOptions>(configuration.GetSection(IncomingNachaDispatchResilienceOptions.SectionName));
        services.Configure<OperationArtifactOptions>(configuration.GetSection(OperationArtifactOptions.SectionName));
        services.Configure<ProcContrapartidasDispatchOptions>(configuration.GetSection(ProcContrapartidasDispatchOptions.SectionName));
        services.Configure<ProcTransaccionesDispatchOptions>(configuration.GetSection(ProcTransaccionesDispatchOptions.SectionName));
        services.AddHttpClient();
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

                            sqlOptions.MigrationsAssembly("Cfa.ACHInterbank.Persistence.Migrations.SqlServer");

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

        services.AddDataProtection()
            .SetApplicationName(DataProtectionKeyRingConfiguration.ApplicationName)
            .PersistKeysToDbContext<AchDbContext>();
        services.AddHostedService<DataProtectionKeyRingStartupValidator>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddScoped<ICertificateCatalogService, ACH.Services.Implementation.CertificateManagement.CertificateCatalogService>();
        services.AddScoped<ICertificateLoadService, ACH.Services.Implementation.CertificateManagement.CertificateLoadService>();
        services.AddScoped<ICertificateSelectionService, ACH.Services.Implementation.CertificateManagement.CertificateSelectionService>();
        services.AddScoped<ICertificateActivationService, ACH.Services.Implementation.CertificateManagement.CertificateActivationService>();
        services.AddScoped<ICertificateRotationService, ACH.Services.Implementation.CertificateManagement.CertificateRotationService>();
        services.AddScoped<ICertificateValidationService, ACH.Services.Implementation.CertificateManagement.CertificateValidationService>();
        services.AddScoped<ICertificateSecretProtector, ACH.Services.Implementation.CertificateManagement.CertificateSecretProtectorService>();
        services.AddSingleton<ICertificatePrivateMaterialProtector, ACH.Services.Implementation.CertificateManagement.DataProtectionCertificatePrivateMaterialProtector>();
        services.AddScoped<ICertificateAuditService, ACH.Services.Implementation.CertificateManagement.CertificateAuditService>();
        services.AddScoped<ICertificateUsageLogger, ACH.Services.Implementation.CertificateManagement.CertificateUsageLoggerService>();
        services.AddScoped<ICertificateSecretProvider, ACH.Services.Implementation.CertificateManagement.DatabaseEncryptedCertificateSecretProvider>();
        services.AddScoped<ICertificateSecretProviderResolver, ACH.Services.Implementation.CertificateManagement.CertificateSecretProviderResolver>();
        services.AddScoped<ICertificateSecretResolver, ACH.Services.Implementation.CertificateManagement.CertificateSecretResolver>();
        services.AddScoped<IManagedDigitalEnvelopeService, ACH.Services.Implementation.CertificateManagement.ManagedDigitalEnvelopeService>();
        services.AddHostedService<ACH.Services.Implementation.CertificateManagement.CertificateBootstrapHostedService>();

        services.AddScoped<IExternalFileNameSequenceProvider, PostgresExternalFileNameSequenceService>();
        services.AddScoped<IExternalFileNameSequenceProvider, SqlServerExternalFileNameSequenceService>();
        services.AddScoped<IExternalFileNameSequenceProvider, EfGenericExternalFileNameSequenceService>();
        services.AddScoped<IPaymentRailCapabilityRegistryService, ACH.Services.Implementation.PaymentRailCapabilityRegistryService>();
        services.AddScoped<IAchReturnEligibilityService, AchReturnEligibilityService>();
        services.AddScoped<IAchReturnOfReturnEligibilityService, AchReturnOfReturnEligibilityService>();
        services.AddScoped<IAchReturnOfReturnFileGenerationService, AchReturnOfReturnFileGenerationService>();
        services.AddScoped<IAchIncomingReturnIngestionService, AchIncomingReturnIngestionService>();
        services.AddScoped<IAchReconciliationReadModelService, AchReconciliationReadModelService>();
        services.AddScoped<INachaSoapIdempotencyStore, NachaSoapInMemoryIdempotencyStore>();
        services.AddScoped<INachaSoapAttemptAuditor, NachaSoapInMemoryAttemptAuditor>();
        services.AddScoped<INachaRealSoapClientAdapter, NachaBlockedRealSoapClientAdapter>();
        services.AddSingleton<IAchReturnGenerationLockService, AchReturnGenerationLockService>();

        services.AddQuartz(q =>
        {
            q.UseJobFactory<MicrosoftDependencyInjectionJobFactory>();

            var quartzOptions = QuartzJobStoreOptionsFactory.Create(configuration);
            if (!quartzOptions.IsPersistentMode())
            {
                return;
            }

            var provider = quartzOptions.GetNormalizedProvider();
            var connectionString = provider == "SqlServer"
                ? configuration.GetConnectionString("SqlConnection")
                : configuration.GetConnectionString("PostgresConnection");

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException($"Quartz persistent mode requiere cadena de conexión para proveedor {provider}.");
            }

            q.SetProperty("quartz.jobStore.type", "Quartz.Impl.AdoJobStore.JobStoreTX, Quartz");
            q.SetProperty("quartz.jobStore.useProperties", "true");
            q.SetProperty("quartz.jobStore.dataSource", "default");
            q.SetProperty("quartz.jobStore.tablePrefix", quartzOptions.TablePrefix);
            q.SetProperty("quartz.jobStore.misfireThreshold", quartzOptions.MisfireThresholdMilliseconds.ToString());
            q.SetProperty("quartz.jobStore.performSchemaValidation", quartzOptions.PerformSchemaValidation.ToString().ToLowerInvariant());
            q.SetProperty("quartz.serializer.type", "json");

            q.SetProperty("quartz.jobStore.driverDelegateType", provider == "SqlServer"
                ? "Quartz.Impl.AdoJobStore.SqlServerDelegate, Quartz"
                : "Quartz.Impl.AdoJobStore.PostgreSQLDelegate, Quartz");
            q.SetProperty("quartz.dataSource.default.provider", provider == "SqlServer" ? "SqlServer" : "Npgsql");
            q.SetProperty("quartz.dataSource.default.connectionString", connectionString);

            if (quartzOptions.Clustered)
            {
                q.SetProperty("quartz.jobStore.clustered", "true");
                q.SetProperty("quartz.jobStore.clusterCheckinInterval", (quartzOptions.ClusterCheckinIntervalSeconds * 1000).ToString());
            }
            else
            {
                q.SetProperty("quartz.jobStore.clustered", "false");
            }
        });

        services.AddQuartzHostedService(opt =>
        {
            opt.WaitForJobsToComplete = true;
        });

        // Servicio que sincroniza DB → Quartz
        services.AddSingleton<QuartzTaskCalendarEvaluator>();
        services.AddTransient<DynamicJobExecutor>();
        services.AddTransient<DynamicJob>();
        services.AddTransient<NonConcurrentDynamicJob>();
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
