using Cfa.ACHInterbank.Application.Configuration;
using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Interfaces.Mapping;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Registries;
using Cfa.ACHInterbank.Application.Integrations.Services;
using Cfa.ACHInterbank.Application.Integrations.Transformations;
using Cfa.ACHInterbank.Application.ACHSobreDigital.CertificateManagement;
using Cfa.ACHInterbank.Application.ACHSobreDigital.ManagedDigitalEnvelope;
using Cfa.ACHInterbank.Application.ACHSobreDigital.Operations;
using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Application.OutgoingTransactionMonitoring;
using Cfa.ACHInterbank.Application.Reports.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Quartz;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Calendar;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs;
using Cfa.ACHInterbank.Persistence.ACH.Quartz.Jobs.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Mapping;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Integrations.Providers;
using Cfa.ACHInterbank.Persistence.Integrations.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Quartz;

namespace Cfa.ACHInterbank.Persistence;

public static class DependencyInjectionService
{
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment? hostEnvironment = null)
    {
        services.Configure<NachaGenerationOptions>(configuration.GetSection(NachaGenerationOptions.SectionName));
        services.Configure<NachaInboundSimulatorOptions>(configuration.GetSection(NachaInboundSimulatorOptions.SectionName));
        services.AddOptions<CenitLocalGatewayOptions>()
            .Bind(configuration.GetSection(CenitLocalGatewayOptions.SectionName))
            .Validate(
                options => hostEnvironment is null || !hostEnvironment.IsProduction() || !options.Enabled,
                "CENIT_LOCAL_GATEWAY_NOT_ALLOWED_IN_PRODUCTION")
            .ValidateOnStart();
        services.Configure<CertificateSecretResolverOptions>(configuration.GetSection("DigitalEnvelope:CertificateSecretResolver"));
        services.Configure<CertificateManagementOptions>(configuration.GetSection(CertificateManagementOptions.SectionName));
        services.Configure<DigitalEnvelopeCertificateBootstrapOptions>(configuration.GetSection(DigitalEnvelopeCertificateBootstrapOptions.SectionName));
        services.Configure<NachaExportDigitalEnvelopeOptions>(configuration.GetSection(NachaExportDigitalEnvelopeOptions.SectionName));
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

        services.AddHostedService<DatabaseInitializationHostedService>();
        services.AddSingleton<ICenitGatewayTransportAdapter, CenitLocalGatewayFolderTransportAdapter>();
        services.AddHostedService<CenitLocalGatewayPickupHostedService>();
        services.AddDataProtection()
            .SetApplicationName(DataProtectionKeyRingConfiguration.ApplicationName)
            .PersistKeysToDbContext<AchDbContext>();
        services.AddHostedService<DataProtectionKeyRingStartupValidator>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IAchTransactionClassificationPolicy, AchTransactionClassificationPolicy>();
        services.AddSingleton<IOutgoingTransactionMonitoringStatusPolicy, OutgoingTransactionMonitoringStatusPolicy>();

        // P3.1/P3.2: shared contract-driven request engine. Each SOAP operation consumes it only behind its own feature flag.
        services.AddScoped<IProcTransaccionesFunctionalSourceResolver, ProcTransaccionesFunctionalSourceResolver>();
        services.AddScoped<CurrentIntegrationSourceProviderFactory>();
        services.AddScoped<IIntegrationSourceProviderRegistry>(sp =>
            new IntegrationSourceProviderRegistry(sp.GetRequiredService<CurrentIntegrationSourceProviderFactory>().Create()));
        services.AddSingleton<IIntegrationTransformationRegistry>(_ =>
            new IntegrationTransformationRegistry(StandardIntegrationTransformationCatalog.Create()));
        services.AddScoped<IIntegrationParameterValueResolver, IntegrationParameterValueResolver>();
        services.AddScoped<IIntegrationExecutionContractResolver, IntegrationExecutionContractResolver>();
        services.AddScoped<IIntegrationRequestResolver, IntegrationRequestResolver>();
        services.AddSingleton<ISoapRequestBodyWriter, ContractDrivenSoapBodyWriter>();
        services.AddScoped<ProcTransaccionesRequestParityPolicy>();
        services.AddScoped<ProcContrapartidasRequestParityPolicy>();

        services.AddScoped<ICertificateCatalogService, ACH.Services.Implementation.CertificateManagement.CertificateCatalogService>();
        services.AddScoped<ICertificateLoadService, ACH.Services.Implementation.CertificateManagement.CertificateLoadService>();
        services.AddScoped<ICertificateSelectionService, ACH.Services.Implementation.CertificateManagement.CertificateSelectionService>();
        services.AddScoped<ICertificateActivationService, ACH.Services.Implementation.CertificateManagement.CertificateActivationService>();
        services.AddScoped<ICertificateDeletionService, ACH.Services.Implementation.CertificateManagement.CertificateDeletionService>();
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
        services.AddScoped<IAchColombiaAutomaticReturnSettingsService, AchColombiaAutomaticReturnSettingsService>();
        services.AddScoped<ICenitAutomaticReturnSettingsService, CenitAutomaticReturnSettingsService>();
        services.AddScoped<ICenitPassiveReturnDetector, CenitPassiveReturnDetector>();
        services.AddScoped<ICenitReturnPreparationService, CenitReturnPreparationService>();
        services.AddScoped<ICenitReturnReviewService, CenitReturnReviewService>();
        services.AddScoped<ICenitReturnArtifactService, CenitReturnArtifactService>();
        services.AddScoped<ICenitReturnIntegrationReadService, CenitReturnIntegrationReadService>();
        services.AddScoped<IAchColombiaReturnReviewService, AchColombiaReturnReviewService>();
        services.AddScoped<IAchReturnEligibilityService, AchReturnEligibilityService>();
        services.AddScoped<IAchReturnOfReturnEligibilityService, AchReturnOfReturnEligibilityService>();
        services.AddScoped<IAchReturnOfReturnFileGenerationService, AchReturnOfReturnFileGenerationService>();
        services.AddScoped<ICenitIncomingReturnPolicy, CenitIncomingReturnPolicy>();
        services.AddScoped<IAchIncomingReturnIngestionService, AchIncomingReturnIngestionService>();
        services.AddScoped<IAchReconciliationReadModelService, AchReconciliationReadModelService>();
        services.AddScoped<IAchOperationalReconciliationService, AchOperationalReconciliationService>();
        services.AddScoped<INachaSoapIdempotencyStore, NachaSoapInMemoryIdempotencyStore>();
        services.AddScoped<INachaSoapAttemptAuditor, NachaSoapInMemoryAttemptAuditor>();
        services.AddScoped<INachaRealSoapClientAdapter, NachaBlockedRealSoapClientAdapter>();
        services.AddSingleton<IAchReturnGenerationLockService, AchReturnGenerationLockService>();

        // Quartz.NET 4: servicios de DI/Hosting/JSON incorporados en Quartz.
        // Configuración tipada; conserva las opciones operacionales anteriores.
        services.AddQuartz(q =>
        {
            var quartzOptions = QuartzJobStoreOptionsFactory.Create(configuration);
            q.ConfigureScheduler(options =>
            {
                options.InstanceName = quartzOptions.SchedulerName;
                // Quartz 4 genera el identificador del nodo mediante esta opción,
                // no asignando literalmente la cadena heredada "AUTO".
                if (string.IsNullOrWhiteSpace(quartzOptions.InstanceId)
                    || string.Equals(quartzOptions.InstanceId, "AUTO", StringComparison.OrdinalIgnoreCase))
                {
                    options.GenerateInstanceId = true;
                }
                else
                {
                    options.InstanceId = quartzOptions.InstanceId;
                }
                options.MaxBatchSize = quartzOptions.MaxBatchSize;
                options.BatchTriggerAcquisitionFireAheadTimeWindow =
                    TimeSpan.FromMilliseconds(quartzOptions.BatchFireAheadMilliseconds);
            });
            q.UseDefaultThreadPool(maxConcurrency: quartzOptions.MaxConcurrency);
            // El factory de Microsoft DI es la opción por defecto desde Quartz 4.
            if (!quartzOptions.IsPersistentMode())
            {
                q.UseInMemoryStore(options =>
                    options.MisfireThreshold = TimeSpan.FromMilliseconds(quartzOptions.MisfireThresholdMilliseconds));
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

            q.UsePersistentStore(store =>
            {
                if (provider == "SqlServer")
                {
                    store.UseSqlServer(connectionString);
                }
                else
                {
                    store.UsePostgres(connectionString);
                }

                store.UseSystemTextJsonSerializer();
                store.ConfigureStore(options =>
                {
                    options.StoreJobDataAsStrings = true;
                    options.TablePrefix = quartzOptions.TablePrefix;
                    options.MisfireThreshold = TimeSpan.FromMilliseconds(quartzOptions.MisfireThresholdMilliseconds);
                    options.SchemaProvisioning = quartzOptions.PerformSchemaValidation
                        ? SchemaProvisioning.Validate
                        : SchemaProvisioning.None;
                    options.AcquireTriggersWithinLock = quartzOptions.AcquireTriggersWithinLock;
                });

                if (quartzOptions.Clustered)
                {
                    store.UseClustering(cluster =>
                        cluster.CheckinInterval = TimeSpan.FromSeconds(quartzOptions.ClusterCheckinIntervalSeconds));
                }
            });
        });

        services.AddQuartzHostedService(opt =>
        {
            opt.WaitForJobsToComplete = true;
        });

        // Servicio que sincroniza DB → Quartz
        services.AddSingleton<QuartzTaskCalendarEvaluator>();
        services.AddSingleton<SchedulerMisfireListener>();
        services.AddHostedService<SchedulerRuntimeService>();
        services.AddTransient<DynamicJobExecutor>();
        services.AddTransient<DynamicJob>();
        services.AddTransient<NonConcurrentDynamicJob>();
        services.AddSingleton<SchedulerSyncService>();
        services.AddSingleton<IAchColombiaOperationalScheduleSynchronizer>(sp => sp.GetRequiredService<SchedulerSyncService>());
        services.AddSingleton<IHostedService>(sp => sp.GetRequiredService<SchedulerSyncService>());
        services.AddTransient<ProcessBulkIngestionBatchJob>();
        services.AddScoped<IBulkFileParser, ACH.Services.Implementation.BulkParsers.JsonBulkFileParser>();
        services.AddScoped<IBulkFileParser, ACH.Services.Implementation.BulkParsers.CsvBulkFileParser>();
        services.AddScoped<IBulkFileParser, ACH.Services.Implementation.BulkParsers.ExcelBulkFileParser>();
        services.AddScoped<ExpressionDslEngine>();
        services.AddScoped<IExpressionDslCompiler>(sp => sp.GetRequiredService<ExpressionDslEngine>());
        services.AddScoped<IExpressionDslExecutor>(sp => sp.GetRequiredService<ExpressionDslEngine>());
        services.AddScoped<IReportGenerator, Reports.QuestPdfReportGenerator>();




        // Registro convencional mediante Scrutor.
        services.AddConventionServicesFromAssembly(
            typeof(DependencyInjectionService).Assembly,
            typeof(ITaskHandler),
            typeof(IDbSeeder));

        // R6B: keep the R6A facade aliases, but compose the facade from cohesive managed-MFT implementation owners.
        // All components remain scoped and share the same scoped AchDbContext; transport/session construction remains in External.
        services.AddScoped<AchColombiaManagedMftOutboundWorkflow>();
        services.AddScoped<AchColombiaManagedMftInboundWorkflow>();
        services.AddScoped<AchColombiaManagedMftRetryReprocessComponent>();
        services.AddScoped<AchColombiaManagedMftLifecycleComponent>();
        services.AddScoped<AchColombiaManagedMftQueryProjectionComponent>();
        services.AddScoped<AchColombiaManagedMftAdministrationComponent>();
        services.AddScoped<AchColombiaManagedMftCredentialComponent>();

        // R6A compatibility invariant: each capability still aliases the same scoped facade instance.
        services.AddScoped<IAchColombiaManagedMftExecutionService>(sp => sp.GetRequiredService<IAchColombiaManagedFileExchangeService>());
        services.AddScoped<IAchColombiaManagedMftTransferReadService>(sp => sp.GetRequiredService<IAchColombiaManagedFileExchangeService>());
        services.AddScoped<IAchColombiaManagedMftTransferOperationsService>(sp => sp.GetRequiredService<IAchColombiaManagedFileExchangeService>());
        services.AddScoped<IAchColombiaManagedMftAdministrationService>(sp => sp.GetRequiredService<IAchColombiaManagedFileExchangeService>());
        services.AddScoped<IAchColombiaManagedMftCredentialAdministrationService>(sp => sp.GetRequiredService<IAchColombiaManagedFileExchangeService>());


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
            $"No configured connection string was found for provider '{provider}'. " +
            "Configure 'ConnectionStrings:PostgresConnection' or 'ConnectionStrings:SqlConnection' in the active appsettings file. " +
            "Development ships with a non-secret SQL Server LocalDB connection in appsettings.Development.json; " +
            "replace it only when using another local SQL Server/PostgreSQL instance.");
    }
}
