using Cfa.ACHInterbank.Application.Configuration;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Application.JobsQuartz.Interfaces;
using Cfa.ACHInterbank.Application.Services.Notifications.Interfaces;
using Cfa.ACHInterbank.Application.Services.EncryptionService.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Responses.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.Security.Dtos;
using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.External.Notifications;
using Cfa.ACHInterbank.External.Connections.RespuestaTransaccionesAch;
using Cfa.ACHInterbank.External.Connections;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

namespace Cfa.ACHInterbank.External;

public static class DependencyInjectionService
{
    public static IServiceCollection AddExternal(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<Token>()
            .Bind(configuration.GetSection("appSettings:tokenManager"))
            .Validate(options => !string.IsNullOrWhiteSpace(options.secretKetJwt), "appSettings:tokenManager:secretKetJwt es obligatorio.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.issuerJwt), "appSettings:tokenManager:issuerJwt es obligatorio.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.audienceJwt), "appSettings:tokenManager:audienceJwt es obligatorio.")
            .Validate(options => options.accessExpiration > 0, "appSettings:tokenManager:accessExpiration debe ser positivo.")
            .ValidateOnStart();
        services.AddOptions<TokenGeneric>()
            .Bind(configuration.GetSection("appSettings:TokenGeneric"));
        services.AddOptions<Integrations>()
            .Bind(configuration.GetSection("appSettings:Integrations"));
        services.AddOptions<ServesSettings>()
            .Bind(configuration.GetSection("appSettings:servers"))
            .Validate(options => options.services is not null, "appSettings:servers:services es obligatorio.")
            .Validate(options => options.servicesWCF is not null, "appSettings:servers:servicesWCF es obligatorio.")
            .ValidateOnStart();
        services.PostConfigure<ServesSettings>(options =>
        {
            options.services = options.services?.Where(service => service.State).ToList();
            options.servicesWCF = options.servicesWCF?.Where(service => service.State).ToList();
        });
        services.AddOptions<CertificatesSettings>()
            .Bind(configuration.GetSection("appSettings:Certificates"));
        services.AddOptions<ApplicationEncryptionOptions>()
            .Configure(options => options.Key = configuration["appSettings:key"]);
        services.AddOptions<AuthenticationUiOptions>()
            .Configure(options =>
            {
                var configured = configuration["appSettings:address"];
                if (!string.IsNullOrWhiteSpace(configured))
                {
                    options.ResetPasswordBaseUrl = configured.Trim();
                }
            });
        services.AddOptions<WcfAuthorizationOptions>()
            .Configure(options => options.TimeoutMinutes = configuration["appSettings:prueba"]);
        services.Configure<WsAxonEndpointSecurityOptions>(
            configuration.GetSection(WsAxonEndpointSecurityOptions.SectionName));
        services.Configure<RegistrarRespuestaDispatchOptions>(
            configuration.GetSection(RegistrarRespuestaDispatchOptions.SectionName));
        services.Configure<ControlledLocalSoapTransportOptions>(
            configuration.GetSection(ControlledLocalSoapTransportOptions.SectionName));
        services.Configure<AchOutboundReturnTransportOptions>(
            configuration.GetSection(AchOutboundReturnTransportOptions.SectionName));
        services.Configure<AchColombiaManagedMftOptions>(
            configuration.GetSection(AchColombiaManagedMftOptions.SectionName));
        services.AddScoped<AchColombiaManagedMftFolderAdapter>();
        services.AddScoped<IAchColombiaSftpSessionFactory, SshNetAchColombiaSftpSessionFactory>();
        services.AddScoped<AchColombiaSftpMftAdapter>(provider => new AchColombiaSftpMftAdapter(
            provider.GetRequiredService<IAchColombiaManagedMftConfigurationProvider>(),
            provider.GetRequiredService<IEncryptionService>(),
            provider.GetRequiredService<IAchColombiaSftpSessionFactory>()));
        services.AddScoped<IAchColombiaManagedMftAdapterResolver, AchColombiaManagedMftAdapterResolver>();
        services.AddScoped<IAchColombiaManagedMftConnectionTester>(provider => new AchColombiaManagedMftConnectionTester(
            provider.GetRequiredService<IAchColombiaManagedMftConfigurationProvider>(),
            provider.GetRequiredService<IEncryptionService>(),
            provider.GetRequiredService<IAchColombiaSftpSessionFactory>()));
        services.AddScoped<IAchColombiaManagedMftAdapter, AchColombiaManagedMftDispatchAdapter>();

        var externalHttpSection = configuration.GetSection(ExternalHttpResilienceOptions.SectionName);
        var externalHttpOptions = externalHttpSection.Get<ExternalHttpResilienceOptions>()
            ?? new ExternalHttpResilienceOptions();

        services.AddOptions<ExternalHttpResilienceOptions>()
            .Bind(externalHttpSection)
            .Validate(options => IsValidHttpPolicy(options.HealthProbe, allowRetry: true, requireTimeout: true),
                "Resilience:ExternalHttp:HealthProbe contiene valores inválidos.")
            .Validate(options => IsValidHttpPolicy(options.Generic, allowRetry: false, requireTimeout: true),
                "Resilience:ExternalHttp:Generic debe tener retry deshabilitado y timeout positivo.")
            .Validate(options => IsValidHttpPolicy(options.Soap, allowRetry: false, requireTimeout: false),
                "Resilience:ExternalHttp:Soap debe tener retry deshabilitado; el timeout lo controla el mapping SOAP.")
            .ValidateOnStart();

        AddHealthProbeHttpClient(services, externalHttpOptions.HealthProbe);
        AddNoRetryHttpClient(
            services,
            ExternalHttpClientNames.GenericNoRetry,
            externalHttpOptions.Generic,
            configureTimeout: true);
        AddNoRetryHttpClient(
            services,
            ExternalHttpClientNames.WscfaachSoapNoRetry,
            externalHttpOptions.Soap,
            configureTimeout: false);
        AddNoRetryHttpClient(
            services,
            ExternalHttpClientNames.WsAxonSoapNoRetry,
            externalHttpOptions.Soap,
            configureTimeout: false);

        services.AddScoped<IEmailSender, LoggingEmailSender>();
        services.AddScoped<RegistrarRespuestaAchSoapRequestMapper>();
        services.AddScoped<RegistrarRespuestaAchSoapResponseParser>();
        services.AddScoped<IRespuestaTransaccionesAchGateway, RespuestaTransaccionesAchGateway>();

        var validIssuer = configuration["appSettings:tokenManager:issuerJwt"]
            ?? throw new InvalidOperationException("Debe configurar appSettings:tokenManager:issuerJwt.");
        var validAudience = configuration["appSettings:tokenManager:audienceJwt"]
            ?? throw new InvalidOperationException("Debe configurar appSettings:tokenManager:audienceJwt.");
        var jwtSecret = ResolveJwtSecret(configuration);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(option =>
        {
            option.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
                ValidIssuer = validIssuer,
                ValidAudience = validAudience,
                NameClaimType = JwtRegisteredClaimNames.Sub,
                RoleClaimType = ClaimTypes.Role,
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization(options =>
        {
            options.AddPolicy("AdminOnly", policy => policy.RequireRole("Admin"));
            options.AddPolicy("CanManageAch", policy => policy.RequireClaim("permission", "CanManageAch"));
            options.AddPolicy("CanReadAch", policy => policy.RequireClaim("permission", "CanReadAch"));

            options.AddPolicy(FineGrainedPermissions.CanGenerateNacha,
                policy => policy.RequireAssertion(ctx =>
                    ctx.User.HasClaim("permission", FineGrainedPermissions.CanGenerateNacha)
                    || ctx.User.HasClaim("permission", "CanManageAch")
                    || ctx.User.HasClaim("permission", "CanReadAch")));

            options.AddPolicy(FineGrainedPermissions.CanGenerateEncryptedNacha,
                policy => policy.RequireAssertion(ctx =>
                    ctx.User.HasClaim("permission", FineGrainedPermissions.CanGenerateEncryptedNacha)
                    || ctx.User.HasClaim("permission", "CanManageAch")
                    || ctx.User.HasClaim("permission", "CanReadAch")));

            options.AddPolicy(FineGrainedPermissions.CanManualEncryptEnvelope,
                policy => policy.RequireAssertion(ctx =>
                    ctx.User.HasClaim("permission", FineGrainedPermissions.CanManualEncryptEnvelope)
                    || ctx.User.HasClaim("permission", "CanManageAch")
                    || ctx.User.HasClaim("permission", "CanReadAch")));

            options.AddPolicy(FineGrainedPermissions.CanManualDecryptEnvelope,
                policy => policy.RequireAssertion(ctx =>
                    ctx.User.HasClaim("permission", FineGrainedPermissions.CanManualDecryptEnvelope)
                    || ctx.User.HasClaim("permission", "CanManageAch")
                    || ctx.User.HasClaim("permission", "CanReadAch")));

            options.AddPolicy(FineGrainedPermissions.CanDownloadPlainNacha,
                policy => policy.RequireAssertion(ctx =>
                    ctx.User.HasClaim("permission", FineGrainedPermissions.CanDownloadPlainNacha)
                    || ctx.User.HasClaim("permission", "CanManageAch")
                    || ctx.User.HasClaim("permission", "CanReadAch")));

            options.AddPolicy(FineGrainedPermissions.CanDownloadEnvelope,
                policy => policy.RequireAssertion(ctx =>
                    ctx.User.HasClaim("permission", FineGrainedPermissions.CanDownloadEnvelope)
                    || ctx.User.HasClaim("permission", "CanManageAch")
                    || ctx.User.HasClaim("permission", "CanReadAch")));

            options.AddPolicy(FineGrainedPermissions.CanViewNachaSecurityAudit,
                policy => policy.RequireAssertion(ctx =>
                    ctx.User.HasClaim("permission", FineGrainedPermissions.CanViewNachaSecurityAudit)
                    || ctx.User.HasClaim("permission", "CanManageAch")
                    || ctx.User.HasClaim("permission", "CanReadAch")));

            options.AddPolicy(FineGrainedPermissions.CanManageCertificates,
                policy => policy.RequireClaim("permission", FineGrainedPermissions.CanManageCertificates));

            options.AddPolicy(FineGrainedPermissions.CanRunInteroperabilityHarness,
                policy => policy.RequireAssertion(ctx =>
                    ctx.User.HasClaim("permission", FineGrainedPermissions.CanRunInteroperabilityHarness)
                    || ctx.User.HasClaim("permission", "CanManageAch")));

            options.AddPolicy(FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry,
                policy => policy.RequireAssertion(ctx =>
                    ctx.User.HasClaim("permission", FineGrainedPermissions.CanViewPaymentRailCapabilityRegistry)
                    || ctx.User.HasClaim("permission", "CanManageAch")
                    || ctx.User.HasClaim("permission", "CanReadAch")));


            options.AddPolicy(P0Policies.TransactionsRead,
                policy => policy.RequireAssertion(ctx =>
                    ctx.User.HasClaim("permission", FineGrainedPermissions.Transactions.Read)
                    || ctx.User.HasClaim("permission", "CanReadAch")));

            options.AddPolicy(P0Policies.TransactionsCreate,
                policy => policy.RequireAssertion(ctx =>
                    ctx.User.HasClaim("permission", FineGrainedPermissions.Transactions.Create)
                    || ctx.User.HasClaim("permission", "CanManageAch")));

            options.AddPolicy(P0Policies.TransactionsBulkSubmit,
                policy => policy.RequireAssertion(ctx =>
                    ctx.User.HasClaim("permission", FineGrainedPermissions.Transactions.BulkSubmit)
                    || ctx.User.HasClaim("permission", "CanManageAch")));

            options.AddPolicy(P0Policies.TransactionsPolicyPreview,
                policy => policy.RequireAssertion(ctx =>
                    ctx.User.HasClaim("permission", FineGrainedPermissions.Transactions.PolicyPreview)
                    || ctx.User.HasClaim("permission", "CanReadAch")));

            options.AddPolicy(P0Policies.TraceabilityRead,
                policy => policy.RequireAssertion(ctx =>
                    ctx.User.HasClaim("permission", FineGrainedPermissions.Traceability.Read)
                    || ctx.User.HasClaim("permission", "CanReadAch")));

            options.AddPolicy(P0Policies.TraceabilityCertifySol02,
                policy => policy.RequireAssertion(ctx =>
                    ctx.User.HasClaim("permission", FineGrainedPermissions.Traceability.CertifySol02)
                    || ctx.User.HasClaim("permission", "CanManageAch")));

            options.AddPolicy(P0Policies.ReturnsRead,
                policy => policy.RequireAssertion(ctx =>
                    ctx.User.HasClaim("permission", FineGrainedPermissions.Returns.Read)
                    || ctx.User.HasClaim("permission", "CanReadAch")));

            options.AddPolicy(P0Policies.ReturnsGenerateFile,
                policy => policy.RequireAssertion(ctx =>
                    ctx.User.HasClaim("permission", FineGrainedPermissions.Returns.GenerateFile)
                    || ctx.User.HasClaim("permission", "CanManageAch")));

            options.AddPolicy(P0Policies.ClaimsRead,
                policy => policy.RequireClaim("permission", FineGrainedPermissions.Claims.Read));

            options.AddPolicy(P0Policies.ClaimsCreateDev14,
                policy => policy.RequireClaim("permission", FineGrainedPermissions.Claims.CreateDev14));

            options.AddPolicy(P0Policies.ClaimsProgress,
                policy => policy.RequireClaim("permission", FineGrainedPermissions.Claims.Progress));

            options.AddPolicy(P0Policies.ClaimsResolve,
                policy => policy.RequireClaim("permission", FineGrainedPermissions.Claims.Resolve));

            options.AddPolicy(P0Policies.ClaimsExpire,
                policy => policy.RequireClaim("permission", FineGrainedPermissions.Claims.Expire));

            options.AddPolicy(P0Policies.OutgoingTransactionsMonitorRead,
                policy => policy.RequireAssertion(ctx =>
                    ctx.User.HasClaim("permission", FineGrainedPermissions.OutgoingTransactions.MonitorRead)
                    || ctx.User.HasClaim("permission", "CanReadAch")
                    || ctx.User.HasClaim("permission", "CanManageAch")));

            options.AddPolicy(P0Policies.OutgoingTransactionsMonitorTechnicalDetailRead,
                policy => policy.RequireAssertion(ctx =>
                    ctx.User.HasClaim("permission", FineGrainedPermissions.OutgoingTransactions.MonitorTechnicalDetailRead)
                    || ctx.User.HasClaim("permission", "CanManageAch")));

            options.AddPolicy(P1Policies.ConfigRead, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.Config.Read)
                || ctx.User.HasClaim("permission", "CanReadAch")));
            options.AddPolicy(P1Policies.ConfigManage, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.Config.Manage)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.IntegrationsRead, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.Integrations.Read)
                || ctx.User.HasClaim("permission", "CanReadAch")));
            options.AddPolicy(P1Policies.IntegrationContractsManage, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.Integrations.ManageContracts)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.IntegrationConnectionsManage, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.Integrations.ManageConnections)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.IntegrationMappingsManage, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.Integrations.ManageMappings)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.IntegrationMappingsValidate, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.Integrations.Validate)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.IntegrationMappingsPublish, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.Integrations.Publish)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.MaintenanceSeed, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.Maintenance.Seed)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.MaintenanceRunAdminTask, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.Maintenance.RunAdminTask)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.BulkIngestionRead, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.BulkIngestion.Read)
                || ctx.User.HasClaim("permission", "CanReadAch")));
            options.AddPolicy(P1Policies.BulkIngestionUpload, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.BulkIngestion.Upload)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.BulkIngestionRetry, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.BulkIngestion.Retry)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.BulkIngestionCancel, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.BulkIngestion.Cancel)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.CommandCenterRead, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.CommandCenter.Read)
                || ctx.User.HasClaim("permission", "CanReadAch")));
            options.AddPolicy(P1Policies.CommandCenterRetry, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.CommandCenter.Retry)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.CommandCenterUnblock, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.CommandCenter.Unblock)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.CommandCenterRequeue, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.CommandCenter.Requeue)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.CommandCenterMarkFailedFinal, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.CommandCenter.MarkFailedFinal)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.NachaRead, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.Nacha.Read)
                || ctx.User.HasClaim("permission", "CanReadAch")));
            options.AddPolicy(P1Policies.NachaUpload, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.Nacha.Upload)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.NachaGenerate, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.Nacha.Generate)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.NachaExport, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.Nacha.Export)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.NachaSimulatorRead, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.NachaSimulator.Read)
                || ctx.User.HasClaim("permission", "CanReadAch")));
            options.AddPolicy(P1Policies.NachaSimulatorGenerateIncoming, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.NachaSimulator.GenerateIncoming)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.NachaSimulatorGenerateDifferential, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.NachaSimulator.GenerateDifferential)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.NachaSimulatorDownload, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.NachaSimulator.Download)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.NachaSimulatorLive, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.NachaSimulator.Live)));
            options.AddPolicy(P1Policies.CertificatesRead, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.Certificates.Read)
                || ctx.User.HasClaim("permission", FineGrainedPermissions.CanManageCertificates)
                || ctx.User.HasClaim("permission", "CanReadAch")));
            options.AddPolicy(P1Policies.CertificatesUploadPublic, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.Certificates.UploadPublic)
                || ctx.User.HasClaim("permission", FineGrainedPermissions.CanManageCertificates)));
            options.AddPolicy(P1Policies.CertificatesRegisterPrivate, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.Certificates.RegisterPrivate)
                || ctx.User.HasClaim("permission", FineGrainedPermissions.CanManageCertificates)));
            options.AddPolicy(P1Policies.CertificatesActivate, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.Certificates.Activate)
                || ctx.User.HasClaim("permission", FineGrainedPermissions.CanManageCertificates)));
            options.AddPolicy(P1Policies.CertificatesRevoke, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.Certificates.Revoke)
                || ctx.User.HasClaim("permission", FineGrainedPermissions.CanManageCertificates)));
            options.AddPolicy(P1Policies.CertificatesValidate, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.Certificates.Validate)
                || ctx.User.HasClaim("permission", FineGrainedPermissions.CanManageCertificates)));
            options.AddPolicy(P1Policies.CertificatesAudit, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.Certificates.Audit)
                || ctx.User.HasClaim("permission", FineGrainedPermissions.CanManageCertificates)));
            options.AddPolicy(P1Policies.DigitalEnvelopeEncrypt, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.DigitalEnvelope.Encrypt)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.DigitalEnvelopeDecrypt, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.DigitalEnvelope.Decrypt)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.DigitalEnvelopeTest, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.DigitalEnvelope.Test)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.NachaSecurityRead, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.NachaSecurity.Read)
                || ctx.User.HasClaim("permission", "CanReadAch")));
            options.AddPolicy(P1Policies.NachaSecurityGenerateEncrypted, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.NachaSecurity.GenerateEncrypted)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.NachaSecurityManualEncrypt, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.NachaSecurity.ManualEncrypt)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.NachaSecurityManualDecrypt, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.NachaSecurity.ManualDecrypt)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.NachaSecurityAuthorizeDownload, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.NachaSecurity.AuthorizeDownload)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.UsersRead, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.Users.Read)
                || ctx.User.HasClaim("permission", "CanReadAch")));
            options.AddPolicy(P1Policies.UsersCreate, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.Users.Create)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.UsersUpdate, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.Users.Update)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.UsersDeactivate, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.Users.Deactivate)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.UsersAssignRoles, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.Users.AssignRoles)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.UsersManageBranding, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.Users.ManageBranding)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.UsersManagePasswordRules, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.Users.ManagePasswordRules)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.UsersManageLockout, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.Users.ManageLockout)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.RolesRead, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.Roles.Read)
                || ctx.User.HasClaim("permission", FineGrainedPermissions.NavigationMenu.Manage)
                || ctx.User.HasClaim("permission", "CanReadAch")));
            options.AddPolicy(P1Policies.RolesCreate, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.Roles.Create)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.RolesUpdate, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.Roles.Update)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.RolesDelete, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.Roles.Delete)
                || ctx.User.HasClaim("permission", "CanManageAch")));
            options.AddPolicy(P1Policies.PermissionsRead, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.Permissions.Read)
                || ctx.User.HasClaim("permission", FineGrainedPermissions.NavigationMenu.Manage)
                || ctx.User.HasClaim("permission", "CanReadAch")));
            options.AddPolicy(P1Policies.PermissionsAssign, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.Permissions.Assign)
                || ctx.User.HasClaim("permission", "CanManageAch")));

            options.AddPolicy(P1Policies.AuditLogsRead, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.AuditLogs.Read)
                || ctx.User.HasClaim("permission", "CanManageUsers")));
            options.AddPolicy(P1Policies.AuthLogsRead, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.AuthLogs.Read)
                || ctx.User.HasClaim("permission", "CanManageUsers")));
            options.AddPolicy(P1Policies.NavigationLogsRead, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.NavigationLogs.Read)
                || ctx.User.HasClaim("permission", "CanReadAch")));

            options.AddPolicy(P1Policies.NavigationMenuManage, policy => policy.RequireAssertion(ctx =>
                ctx.User.HasClaim("permission", FineGrainedPermissions.NavigationMenu.Manage)
                || ctx.User.HasClaim("permission", "CanManageUsers")));

            options.AddPolicy(P1Policies.SchedulerView,
                policy => policy.RequireClaim("permission", FineGrainedPermissions.Scheduler.View));
            options.AddPolicy(P1Policies.SchedulerHistoryView,
                policy => policy.RequireClaim("permission", FineGrainedPermissions.Scheduler.HistoryView));
            options.AddPolicy(P1Policies.SchedulerExecute,
                policy => policy.RequireClaim("permission", FineGrainedPermissions.Scheduler.Execute));
            options.AddPolicy(P1Policies.SchedulerManageSchedule,
                policy => policy.RequireClaim("permission", FineGrainedPermissions.Scheduler.ManageSchedule));
            options.AddPolicy(P1Policies.SchedulerPauseResume,
                policy => policy.RequireClaim("permission", FineGrainedPermissions.Scheduler.PauseResume));
            options.AddPolicy(P1Policies.SchedulerViewInstances,
                policy => policy.RequireClaim("permission", FineGrainedPermissions.Scheduler.ViewInstances));
            options.AddPolicy(P1Policies.SchedulerViewTechnical,
                policy => policy.RequireClaim("permission", FineGrainedPermissions.Scheduler.ViewTechnical));

            foreach (var permission in FineGrainedPermissions.AllPermissions)
            {
                if (options.GetPolicy(permission) is null)
                {
                    options.AddPolicy(permission, policy => policy.RequireClaim("permission", permission));
                }
            }
        });

        #region Services
        // Injection Dependency Traditional


        // Registro convencional mediante Scrutor.
        services.AddConventionServicesFromAssembly(
            typeof(DependencyInjectionService).Assembly,
            typeof(ITaskHandler));

        #endregion Services


        return services;
    }

    private static void AddHealthProbeHttpClient(IServiceCollection services, ExternalHttpPolicyOptions policy)
    {
        services.AddHttpClient(ExternalHttpClientNames.HealthProbe)
            .SetHandlerLifetime(TimeSpan.FromMinutes(10))
            .AddResilienceHandler("ach-health-probe-idempotent-pipeline", pipeline =>
            {
                pipeline.AddConcurrencyLimiter(policy.BulkheadPermitLimit, policy.BulkheadQueueLimit);
                if (policy.MaxRetryAttempts > 0)
                {
                    pipeline.AddRetry(new HttpRetryStrategyOptions
                    {
                        MaxRetryAttempts = policy.MaxRetryAttempts,
                        BackoffType = DelayBackoffType.Exponential,
                        Delay = TimeSpan.FromMilliseconds(policy.RetryBaseDelayMilliseconds),
                        UseJitter = true
                    });
                }

                pipeline.AddTimeout(TimeSpan.FromSeconds(policy.TimeoutSeconds));
                pipeline.AddCircuitBreaker(new HttpCircuitBreakerStrategyOptions
                {
                    FailureRatio = policy.CircuitBreakerFailureRatio,
                    SamplingDuration = TimeSpan.FromSeconds(policy.CircuitBreakerSamplingDurationSeconds),
                    MinimumThroughput = policy.CircuitBreakerMinimumThroughput,
                    BreakDuration = TimeSpan.FromSeconds(policy.CircuitBreakerBreakDurationSeconds)
                });
            });
    }

    private static void AddNoRetryHttpClient(
        IServiceCollection services,
        string clientName,
        ExternalHttpPolicyOptions policy,
        bool configureTimeout)
    {
        // Non-idempotent/uncertain operations deliberately receive no transport retry, circuit breaker,
        // or bulkhead rejection policy. IHttpClientFactory still owns handler reuse. Timeout ownership
        // remains operation-specific: generic HTTP preserves the legacy 100s timeout; SOAP uses its
        // database integration mapping per operation.
        var builder = services.AddHttpClient(clientName);
        if (configureTimeout)
        {
            builder.ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(policy.TimeoutSeconds));
        }

        builder.SetHandlerLifetime(TimeSpan.FromMinutes(10));
    }

    private static bool IsValidHttpPolicy(
        ExternalHttpPolicyOptions policy,
        bool allowRetry,
        bool requireTimeout)
    {
        if (policy is null
            || (requireTimeout && policy.TimeoutSeconds <= 0)
            || (!requireTimeout && policy.TimeoutSeconds < 0)
            || policy.MaxRetryAttempts < 0
            || (!allowRetry && policy.MaxRetryAttempts != 0)
            || policy.RetryBaseDelayMilliseconds < 0
            || policy.CircuitBreakerFailureRatio <= 0
            || policy.CircuitBreakerFailureRatio > 1
            || policy.CircuitBreakerSamplingDurationSeconds <= 0
            || policy.CircuitBreakerMinimumThroughput <= 1
            || policy.CircuitBreakerBreakDurationSeconds <= 0
            || policy.BulkheadPermitLimit <= 0
            || policy.BulkheadQueueLimit < 0)
        {
            return false;
        }

        return true;
    }

    private static string ResolveJwtSecret(IConfiguration configuration)
    {
        var configured = configuration["appSettings:tokenManager:secretKetJwt"]
            ?? Environment.GetEnvironmentVariable("appSettings__tokenManager__secretKetJwt");

        if (string.IsNullOrWhiteSpace(configured))
        {
            throw new InvalidOperationException("Debe configurar appSettings__tokenManager__secretKetJwt mediante variables de entorno o secretos del entorno.");
        }

        return configured.Trim();
    }
}
