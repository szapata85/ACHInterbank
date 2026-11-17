using System.Text.Json;
using Cfa.ACHInterbank.Application.Security.Dtos;
using Cfa.ACHInterbank.Domain.Entities.User;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Integrations.Services;
using Cfa.ACHInterbank.Persistence.Security.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class SoapIntegrationSettingsBootstrapperTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    [Fact]
    public async Task EnsureAsync_CreatesLiveDatabaseConfigurationFromCatalog_AndIsIdempotent()
    {
        await using var fixture = await ContextFixture.CreateAsync();
        await SeedCatalogAsync(fixture.Context);
        fixture.Context.AuditEnabled = true;
        var sut = new SoapIntegrationSettingsBootstrapper(fixture.Context, BuildConfiguration());

        await sut.EnsureAsync();

        var row = await fixture.Context.SoapIntegrationSettings.AsNoTracking().SingleAsync();
        var contrapartidas = Assert.Single(ReadMappings(row.WscfaachMappingsJson), x =>
            x.MethodName == "Proc_Contrapartidas");
        var proc = Assert.Single(ReadMappings(row.WscfaachMappingsJson), x =>
            x.MethodName == "Proc_Transacciones");
        var registrar = Assert.Single(ReadMappings(row.WsAxonRespuestaTransaccionesMappingsJson), x =>
            x.MethodName == "RegistrarRespuestaTransaccion");
        Assert.Equal("http://localhost:7083/WSCFAACH.svc", proc.Endpoint);
        Assert.Equal("http://tempuri.org/IWSCFAACH/Proc_Transacciones", proc.SoapAction);
        Assert.Equal("Live", proc.OperatingMode);
        Assert.Equal(15, proc.TimeoutSeconds);
        Assert.True(proc.Enabled);
        Assert.Equal(25, proc.InputParameterMappings.Count);
        Assert.Equal("http://localhost:7083/WSCFAACH.svc", contrapartidas.Endpoint);
        Assert.Equal("http://tempuri.org/IWSCFAACH/Proc_Contrapartidas", contrapartidas.SoapAction);
        Assert.Equal("Live", contrapartidas.OperatingMode);
        Assert.Equal(17, contrapartidas.InputParameterMappings.Count(x => x.Required));
        Assert.Equal("http://localhost:7083/WSAxonRespuestaTransacciones.svc", registrar.Endpoint);
        Assert.Equal(
            "http://tempuri.org/IWSAxonRespuestaTransacciones/RegistrarRespuestaTransaccion",
            registrar.SoapAction);
        Assert.Equal("Live", registrar.OperatingMode);
        Assert.Equal(15, registrar.TimeoutSeconds);
        Assert.True(registrar.Enabled);
        Assert.Equal(
            ["idCanal", "nombreCanal", "idTransaccion", "idEstado", "causal", "idTransaccionAxon", "descripcionCausal"],
            registrar.InputParameterMappings.Select(x => x.SoapParameterName));
        var auditCount = await fixture.Context.AuditLogs.CountAsync();
        Assert.True(auditCount > 0);

        await sut.EnsureAsync();

        Assert.Single(await fixture.Context.SoapIntegrationSettings.AsNoTracking().ToListAsync());
        Assert.Equal(auditCount, await fixture.Context.AuditLogs.CountAsync());
    }

    [Fact]
    public async Task EnsureAsync_ReconcilesWsdlArguments_WithoutOverwritingOperatorRuntimeSettings()
    {
        await using var fixture = await ContextFixture.CreateAsync();
        await SeedCatalogAsync(fixture.Context);
        fixture.Context.SoapIntegrationSettings.Add(new SoapIntegrationSetting
        {
            WscfaachMappingsJson = JsonSerializer.Serialize(new[]
            {
                new SoapEndpointMethodMappingDto
                {
                    MethodName = "Proc_Contrapartidas",
                    Endpoint = "http://localhost:7083/WSCFAACH.svc",
                    SoapAction = "http://tempuri.org/IWSCFAACH/Proc_Contrapartidas",
                    OperatingMode = "DryRun",
                    Enabled = true
                }
            }, JsonOptions),
            WsAxonRespuestaTransaccionesMappingsJson = JsonSerializer.Serialize(new[]
            {
                StaleRegistrar(),
                StaleRegistrar()
            }, JsonOptions)
        });
        await fixture.Context.SaveChangesAsync();
        fixture.Context.AuditEnabled = true;

        await new SoapIntegrationSettingsBootstrapper(fixture.Context, BuildConfiguration()).EnsureAsync();

        var row = await fixture.Context.SoapIntegrationSettings.AsNoTracking().SingleAsync();
        var wscfaach = ReadMappings(row.WscfaachMappingsJson);
        var wsAxon = ReadMappings(row.WsAxonRespuestaTransaccionesMappingsJson);
        Assert.Single(wscfaach, x => x.MethodName == "Proc_Transacciones");
        var contrapartidas = Assert.Single(wscfaach, x => x.MethodName == "Proc_Contrapartidas");
        Assert.Equal("DryRun", contrapartidas.OperatingMode);
        Assert.Equal(15, contrapartidas.TimeoutSeconds);
        Assert.Equal(22, contrapartidas.InputParameterMappings.Count);
        var registrar = Assert.Single(wsAxon, x => x.MethodName == "RegistrarRespuestaTransaccion");
        Assert.Equal("https://backend1.example.com", registrar.Endpoint);
        Assert.Equal("Disabled", registrar.OperatingMode);
        Assert.True(registrar.Enabled);
        Assert.Equal(7, registrar.InputParameterMappings.Count);
        Assert.Equal("http://tempuri.org/IWSAxonRespuestaTransacciones/RegistrarRespuestaTransaccion", registrar.SoapAction);
    }

    [Fact]
    public async Task EnsureAsync_LiveOnly_ReconcilesPersistedModesWithoutChangingOperatorEndpointsOrEnablement()
    {
        await using var fixture = await ContextFixture.CreateAsync();
        await SeedCatalogAsync(fixture.Context);
        fixture.Context.SoapIntegrationSettings.Add(new SoapIntegrationSetting
        {
            WscfaachMappingsJson = JsonSerializer.Serialize(new[]
            {
                new SoapEndpointMethodMappingDto
                {
                    MethodName = "Proc_Contrapartidas",
                    Endpoint = "https://bank.example.invalid/contrapartidas",
                    SoapAction = "http://tempuri.org/IWSCFAACH/Proc_Contrapartidas",
                    OperatingMode = "DryRun",
                    TimeoutSeconds = 40,
                    Enabled = true
                },
                new SoapEndpointMethodMappingDto
                {
                    MethodName = "Proc_Transacciones",
                    Endpoint = "https://bank.example.invalid/transacciones",
                    SoapAction = "http://tempuri.org/IWSCFAACH/Proc_Transacciones",
                    OperatingMode = "Disabled",
                    TimeoutSeconds = 35,
                    Enabled = false
                }
            }, JsonOptions),
            WsAxonRespuestaTransaccionesMappingsJson = JsonSerializer.Serialize(new[]
            {
                new SoapEndpointMethodMappingDto
                {
                    MethodName = "RegistrarRespuestaTransaccion",
                    Endpoint = "https://bank.example.invalid/axon",
                    SoapAction = "http://tempuri.org/IWSAxonRespuestaTransacciones/RegistrarRespuestaTransaccion",
                    OperatingMode = "DryRun",
                    TimeoutSeconds = 20,
                    Enabled = true
                }
            }, JsonOptions)
        });
        await fixture.Context.SaveChangesAsync();
        var values = ConfigurationValues();
        values["SoapIntegrationBootstrap:EnforceLiveOnly"] = "true";
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        var sut = new SoapIntegrationSettingsBootstrapper(fixture.Context, configuration);

        await sut.EnsureAsync();
        var settings = await fixture.Context.SoapIntegrationSettings.AsNoTracking().SingleAsync();
        var mappings = ReadMappings(settings.WscfaachMappingsJson);
        var counterparties = Assert.Single(mappings, x => x.MethodName == "Proc_Contrapartidas");
        Assert.Equal("Live", counterparties.OperatingMode);
        Assert.Equal("https://bank.example.invalid/contrapartidas", counterparties.Endpoint);
        Assert.Equal(40, counterparties.TimeoutSeconds);
        Assert.True(counterparties.Enabled);

        var transactions = Assert.Single(mappings, x => x.MethodName == "Proc_Transacciones");
        Assert.Equal("Live", transactions.OperatingMode);
        Assert.Equal("https://bank.example.invalid/transacciones", transactions.Endpoint);
        Assert.Equal(35, transactions.TimeoutSeconds);
        Assert.False(transactions.Enabled);

        var axon = Assert.Single(ReadMappings(settings.WsAxonRespuestaTransaccionesMappingsJson),
            x => x.MethodName == "RegistrarRespuestaTransaccion");
        Assert.Equal("Live", axon.OperatingMode);
        Assert.Equal("https://bank.example.invalid/axon", axon.Endpoint);
        Assert.Equal(20, axon.TimeoutSeconds);
        await sut.EnsureAsync();
        Assert.Single(await fixture.Context.SoapIntegrationSettings.ToListAsync());
    }

    [Fact]
    public async Task EnsureAsync_WhenRequiredEndpointIsMissing_FailsWithoutSourceCodeFallback()
    {
        await using var fixture = await ContextFixture.CreateAsync();
        await SeedCatalogAsync(fixture.Context);
        var values = ConfigurationValues();
        values.Remove("SoapIntegrationBootstrap:RegistrarRespuestaTransaccion:Endpoint");
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            new SoapIntegrationSettingsBootstrapper(fixture.Context, configuration).EnsureAsync());

        Assert.Contains("Endpoint", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(await fixture.Context.SoapIntegrationSettings.ToListAsync());
    }

    [Fact]
    public async Task EnsureAsync_ExplicitOperatorRecoverySwitch_CanReplacePersistedRuntimeSettings()
    {
        await using var fixture = await ContextFixture.CreateAsync();
        await SeedCatalogAsync(fixture.Context);
        fixture.Context.SoapIntegrationSettings.Add(new SoapIntegrationSetting
        {
            WscfaachMappingsJson = JsonSerializer.Serialize(new[]
            {
                new SoapEndpointMethodMappingDto
                {
                    MethodName = "Proc_Contrapartidas",
                    Endpoint = "https://old.example.invalid/service",
                    SoapAction = "old-action",
                    OperatingMode = "Disabled",
                    TimeoutSeconds = 45,
                    Enabled = false
                }
            }, JsonOptions),
            WsAxonRespuestaTransaccionesMappingsJson = "[]"
        });
        await fixture.Context.SaveChangesAsync();

        var values = ConfigurationValues();
        values["SoapIntegrationBootstrap:OverwriteExistingSettings"] = "true";
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(values).Build();
        await new SoapIntegrationSettingsBootstrapper(fixture.Context, configuration).EnsureAsync();

        var row = await fixture.Context.SoapIntegrationSettings.AsNoTracking().SingleAsync();
        var mapping = Assert.Single(ReadMappings(row.WscfaachMappingsJson), x =>
            x.MethodName == "Proc_Contrapartidas");
        Assert.Equal("Live", mapping.OperatingMode);
        Assert.True(mapping.Enabled);
        Assert.Equal("http://localhost:7083/WSCFAACH.svc", mapping.Endpoint);
    }

    private static SoapEndpointMethodMappingDto StaleRegistrar()
        => new()
        {
            MethodName = "RegistrarRespuestaTransaccion",
            Endpoint = "https://backend1.example.com",
            SoapAction = "http://tempuri.org/IWSAxonRespuestaTransacciones/RegistrarRespuestaTransaccion",
            OperatingMode = "Disabled",
            Enabled = true,
            InputParameterMappings =
            [
                new SoapInputParameterMappingDto
                {
                    InputName = "respuesta",
                    SoapParameterName = "Respuesta",
                    Required = true
                }
            ]
        };

    private static async Task SeedCatalogAsync(AchDbContext context)
    {
        context.AuditEnabled = false;
        await new IntegrationCatalogBootstrapper(context).EnsureAsync();
        await new IntegrationMappingBootstrapper(context).EnsureAsync();
    }

    private static IConfiguration BuildConfiguration()
        => new ConfigurationBuilder()
            .AddInMemoryCollection(ConfigurationValues())
            .Build();

    private static Dictionary<string, string?> ConfigurationValues()
        => new()
        {
            ["SoapIntegrationBootstrap:Enabled"] = "true",
            ["SoapIntegrationBootstrap:DefaultTimeoutSeconds"] = "15",
            ["SoapIntegrationBootstrap:ProcContrapartidas:Endpoint"] = "http://localhost:7083/WSCFAACH.svc",
            ["SoapIntegrationBootstrap:ProcContrapartidas:SoapAction"] = "http://tempuri.org/IWSCFAACH/Proc_Contrapartidas",
            ["SoapIntegrationBootstrap:ProcContrapartidas:OperatingMode"] = "Live",
            ["SoapIntegrationBootstrap:ProcContrapartidas:TimeoutSeconds"] = "15",
            ["SoapIntegrationBootstrap:ProcContrapartidas:Enabled"] = "true",
            ["SoapIntegrationBootstrap:ProcTransacciones:Endpoint"] = "http://localhost:7083/WSCFAACH.svc",
            ["SoapIntegrationBootstrap:ProcTransacciones:SoapAction"] = "http://tempuri.org/IWSCFAACH/Proc_Transacciones",
            ["SoapIntegrationBootstrap:ProcTransacciones:OperatingMode"] = "Live",
            ["SoapIntegrationBootstrap:ProcTransacciones:TimeoutSeconds"] = "15",
            ["SoapIntegrationBootstrap:ProcTransacciones:Enabled"] = "true",
            ["SoapIntegrationBootstrap:RegistrarRespuestaTransaccion:Endpoint"] = "http://localhost:7083/WSAxonRespuestaTransacciones.svc",
            ["SoapIntegrationBootstrap:RegistrarRespuestaTransaccion:SoapAction"] = "http://tempuri.org/IWSAxonRespuestaTransacciones/RegistrarRespuestaTransaccion",
            ["SoapIntegrationBootstrap:RegistrarRespuestaTransaccion:OperatingMode"] = "Live",
            ["SoapIntegrationBootstrap:RegistrarRespuestaTransaccion:TimeoutSeconds"] = "15",
            ["SoapIntegrationBootstrap:RegistrarRespuestaTransaccion:Enabled"] = "true"
        };

    private static List<SoapEndpointMethodMappingDto> ReadMappings(string json)
        => JsonSerializer.Deserialize<List<SoapEndpointMethodMappingDto>>(json, JsonOptions) ?? [];

    private sealed class ContextFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private ContextFixture(SqliteConnection connection, AchDbContext context)
        {
            _connection = connection;
            Context = context;
        }

        public AchDbContext Context { get; }

        public static async Task<ContextFixture> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            var context = new AchDbContext(
                new DbContextOptionsBuilder<AchDbContext>()
                    .UseSqlite(connection)
                    .Options);
            await context.Database.EnsureCreatedAsync();
            return new ContextFixture(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
