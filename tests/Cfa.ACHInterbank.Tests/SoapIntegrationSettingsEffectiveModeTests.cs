using Cfa.ACHInterbank.Application.Security.Dtos;
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
    public async Task GetAsync_ExposesTheSameEffectiveModeConfiguredForProcTransaccionesDispatch()
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
            Options.Create(new ProcTransaccionesDispatchOptions { Mode = "Live" }),
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
        await SeedSoapSettingsAsync(context);
        var service = new SoapIntegrationSettingsService(
            context,
            Options.Create(new ProcTransaccionesDispatchOptions { Mode = "DryRun" }));

        var result = await service.GetAsync();

        Assert.Equal("DryRun", result.ProcTransaccionesEffectiveSettings!.EffectiveMode);
        Assert.False(result.ProcTransaccionesEffectiveSettings.MappingReady);
        Assert.Equal("FUNCTIONAL_MAPPING_INVALID", result.ProcTransaccionesEffectiveSettings.MappingIssueCode);
        Assert.DoesNotContain("password", System.Text.Json.JsonSerializer.Serialize(result), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("token", System.Text.Json.JsonSerializer.Serialize(result), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetAsync_WhenNoSettingsExist_ReturnsDefaultsWithoutPersisting()
    {
        await using var context = BuildContext();
        var service = new SoapIntegrationSettingsService(
            context,
            Options.Create(new ProcTransaccionesDispatchOptions { Mode = "DryRun" }));

        var result = await service.GetAsync();

        Assert.NotEmpty(result.WscfaachMappings);
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

    private static AchDbContext BuildContext()
        => new(new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static async Task SeedSoapSettingsAsync(AchDbContext context)
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
                    Enabled = true,
                    InputParameterMappings = [new SoapInputParameterMappingDto { InputName = "IDTRAN", SoapParameterName = "IDTRAN" }]
                }
            }),
            WsAxonRespuestaTransaccionesMappingsJson = "[]"
        });
        await context.SaveChangesAsync();
    }
}
