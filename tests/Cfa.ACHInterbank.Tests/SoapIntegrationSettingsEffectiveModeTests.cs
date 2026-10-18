using Cfa.ACHInterbank.Application.Security.Dtos;
using Cfa.ACHInterbank.Application.Security.Exceptions;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Security.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class SoapIntegrationSettingsEffectiveModeTests
{
    [Fact]
    public async Task GetAsync_UsesPersistedLiveModeInsteadOfDispatchOption()
    {
        await using var context = BuildContext();
        await SeedSoapSettingsAsync(context);
        var readiness = new Mock<IIntegrationMappingReadinessService>();
        readiness.Setup(x => x.EvaluateAsync(
                IntegrationGuaranteeConstants.Wscfaach,
                IntegrationGuaranteeConstants.ProcTransacciones,
                IntegrationGuaranteeConstants.MonetaryCreditRequest,
                IntegrationGuaranteeConstants.OutboundRequest,
                null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntegrationMappingReadinessResult(
                true, "Ok", "OK", IntegrationGuaranteeConstants.Wscfaach,
                IntegrationGuaranteeConstants.ProcTransacciones,
                IntegrationGuaranteeConstants.MonetaryCreditRequest,
                IntegrationGuaranteeConstants.OutboundRequest, 1, 1, [], [], [], [], false, true, [], []));
        var service = new SoapIntegrationSettingsService(
            context,
            Options.Create(new ProcTransaccionesDispatchOptions { Mode = "DryRun" }),
            readiness.Object);

        var result = await service.GetAsync();

        var effective = Assert.IsType<ProcTransaccionesEffectiveSettingsDto>(result.ProcTransaccionesEffectiveSettings);
        Assert.Equal("Proc_Transacciones", effective.Operation);
        Assert.Equal("Live", effective.EffectiveMode);
        Assert.True(effective.Enabled);
        Assert.True(effective.MappingReady);
        Assert.NotEmpty(effective.Endpoint);
        Assert.Null(effective.MappingIssueCode);
        Assert.Empty(effective.BlockingParameters);
    }

    [Fact]
    public async Task GetAsync_ReportsDryRunWithoutChangingTheConfiguredSoapMapping()
    {
        await using var context = BuildContext();
        await SeedSoapSettingsAsync(context, operatingMode: "DryRun");
        var service = new SoapIntegrationSettingsService(
            context,
            Options.Create(new ProcTransaccionesDispatchOptions { Mode = "DryRun" }));

        var result = await service.GetAsync();

        Assert.Equal("DryRun", result.ProcTransaccionesEffectiveSettings!.EffectiveMode);
        Assert.False(result.ProcTransaccionesEffectiveSettings.MappingReady);
        Assert.Equal("SOAP_RUNTIME_CONFIGURATION_INVALID", result.ProcTransaccionesEffectiveSettings.MappingIssueCode);
        Assert.DoesNotContain("password", System.Text.Json.JsonSerializer.Serialize(result), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", System.Text.Json.JsonSerializer.Serialize(result), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAsync_WhenNoSettingsExist_DoesNotSynthesizeOrPersistEndpoints()
    {
        await using var context = BuildContext();
        var service = new SoapIntegrationSettingsService(
            context,
            Options.Create(new ProcTransaccionesDispatchOptions { Mode = "DryRun" }));

        var result = await service.GetAsync();

        Assert.Empty(result.WscfaachMappings);
        Assert.Empty(result.WsAxonRespuestaTransaccionesMappings);
        Assert.Equal("Disabled", result.ProcTransaccionesEffectiveSettings!.EffectiveMode);
        Assert.Equal("SOAP_RUNTIME_CONFIGURATION_INVALID", result.ProcTransaccionesEffectiveSettings.MappingIssueCode);
        Assert.Empty(await context.Set<SoapIntegrationSetting>().ToListAsync());
    }

    [Fact]
    public async Task GetAsync_DoesNotRewriteStoredJsonDuringNormalization()
    {
        await using var context = BuildContext();
        await SeedSoapSettingsAsync(context);
        var original = await context.Set<SoapIntegrationSetting>().AsNoTracking().SingleAsync();
        var service = new SoapIntegrationSettingsService(
            context,
            Options.Create(new ProcTransaccionesDispatchOptions { Mode = "DryRun" }));

        await service.GetAsync();
        context.ChangeTracker.Clear();

        var persisted = await context.Set<SoapIntegrationSetting>().AsNoTracking().SingleAsync();
        Assert.Equal(original.WscfaachMappingsJson, persisted.WscfaachMappingsJson);
        Assert.Equal(original.WsAxonRespuestaTransaccionesMappingsJson, persisted.WsAxonRespuestaTransaccionesMappingsJson);
    }

    [Fact]
    public async Task GetAsync_ReturnsSanitizedDestinationStateAndCapabilities()
    {
        await using var context = BuildContext();
        context.Set<SoapIntegrationSetting>().Add(new SoapIntegrationSetting
        {
            WscfaachMappingsJson = System.Text.Json.JsonSerializer.Serialize(new[]
            {
                new SoapEndpointMethodMappingDto
                {
                    MethodName = "Proc_Transacciones",
                    Endpoint = "https://usuario:secreto@host-interno.example:7443/WSCFAACH.svc?token=muy-secreto",
                    SoapAction = "https://tempuri.org/IWSCFAACH/Proc_Transacciones?secret=oculto",
                    OperatingMode = "DryRun",
                    TimeoutSeconds = 15,
                    Enabled = true
                }
            }),
            WsAxonRespuestaTransaccionesMappingsJson = "[]"
        });
        await context.SaveChangesAsync();
        var service = new SoapIntegrationSettingsService(
            context,
            Options.Create(new ProcTransaccionesDispatchOptions { Mode = "DryRun" }));

        var result = await service.GetAdministrationAsync();

        var mapping = Assert.Single(result.WscfaachMappings);
        Assert.False(string.IsNullOrWhiteSpace(result.Revision));
        Assert.Equal("Simulation", mapping.ConfigurationState);
        Assert.True(mapping.Capabilities!.ConfigurationStateKnown);
        Assert.True(mapping.Capabilities.CanRequestRealSend);
        Assert.Equal(mapping.SanitizedEndpoint, mapping.Endpoint);
        Assert.DoesNotContain("secreto", mapping.Endpoint, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token=", mapping.Endpoint, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret=", mapping.SoapAction, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("usuario", System.Text.Json.JsonSerializer.Serialize(result), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task PreviewChangeAsync_UsesBackendSanitizationAndRevisionBeforeRealSendConfirmation()
    {
        await using var context = BuildContext();
        await SeedSoapSettingsAsync(context, operatingMode: "DryRun");
        var service = new SoapIntegrationSettingsService(
            context,
            Options.Create(new ProcTransaccionesDispatchOptions { Mode = "DryRun" }));
        var current = await service.GetAsync();

        var preview = await service.PreviewChangeAsync(new SoapIntegrationSettingsChangePreviewRequestDto
        {
            MethodName = "Proc_Transacciones",
            Endpoint = "https://usuario:clave@destino-externo.example:8443/soap?token=abc",
            SoapAction = "https://tempuri.org/IWSCFAACH/Proc_Transacciones?secret=abc",
            OperatingMode = "Live",
            Enabled = true,
            ExpectedRevision = current.Revision
        });

        Assert.True(preview.CanApply);
        Assert.True(preview.WouldEnableRealSend);
        Assert.Equal("Simulation", preview.CurrentConfigurationState);
        Assert.Equal("RealSendEnabled", preview.RequestedConfigurationState);
        Assert.Equal(current.Revision, preview.Revision);
        Assert.DoesNotContain("clave", preview.SanitizedEndpoint, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token=", preview.SanitizedEndpoint, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("secret=", preview.SanitizedSoapAction, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SaveAsync_WithStaleExpectedRevision_ThrowsRevisionConflict()
    {
        await using var context = BuildContext();
        await SeedSoapSettingsAsync(context, operatingMode: "DryRun");
        var service = new SoapIntegrationSettingsService(
            context,
            Options.Create(new ProcTransaccionesDispatchOptions { Mode = "DryRun" }));
        var current = await service.GetAsync();

        var request = current with { ExpectedRevision = "revision-obsoleta" };

        var exception = await Assert.ThrowsAsync<SoapIntegrationSettingsRevisionConflictException>(
            () => service.SaveAsync(request));
        Assert.Equal(current.Revision, exception.CurrentRevision);
    }

    [Fact]
    public async Task SaveAsync_PreservesPersistedSensitiveValuesWhenClientReturnsSanitizedPlaceholders()
    {
        await using var context = BuildContext();
        await SeedSoapSettingsAsync(context, operatingMode: "DryRun");
        var service = new SoapIntegrationSettingsService(
            context,
            Options.Create(new ProcTransaccionesDispatchOptions { Mode = "DryRun" }));
        var current = await service.GetAdministrationAsync();
        var sanitized = Assert.Single(current.WscfaachMappings);

        var saved = await service.SaveAsync(current with
        {
            ExpectedRevision = current.Revision,
            WscfaachMappings = [sanitized with { OperatingMode = "Disabled", Enabled = false }]
        });

        context.ChangeTracker.Clear();
        var persisted = await context.Set<SoapIntegrationSetting>().AsNoTracking().SingleAsync();
        Assert.Contains("http://local/WSCFAACH.svc", persisted.WscfaachMappingsJson, StringComparison.Ordinal);
        Assert.DoesNotContain("***", persisted.WscfaachMappingsJson, StringComparison.Ordinal);
        Assert.Equal("Disabled", Assert.Single(saved.WscfaachMappings).ConfigurationState);
        Assert.NotEqual(current.Revision, saved.Revision);
    }

    private static AchDbContext BuildContext()
        => new(new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task SeedSoapSettingsAsync(
        AchDbContext context,
        string operatingMode = "Live")
    {
        context.Set<SoapIntegrationSetting>().Add(new SoapIntegrationSetting
        {
            WscfaachMappingsJson = System.Text.Json.JsonSerializer.Serialize(new[]
            {
                new SoapEndpointMethodMappingDto
                {
                    MethodName = "Proc_Transacciones",
                    Endpoint = "http://local/WSCFAACH.svc",
                    SoapAction = "http://tempuri.org/IWSCFAACH/Proc_Transacciones",
                    OperatingMode = operatingMode,
                    TimeoutSeconds = 15,
                    Enabled = true,
                    InputParameterMappings = [new SoapInputParameterMappingDto { InputName = "IDTRAN", SoapParameterName = "IDTRAN" }]
                }
            }),
            WsAxonRespuestaTransaccionesMappingsJson = "[]"
        });
        await context.SaveChangesAsync();
    }
}
