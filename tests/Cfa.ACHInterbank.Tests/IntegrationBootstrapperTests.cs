using Cfa.ACHInterbank.Application.DataBase;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.Seeders;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Integrations.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace Cfa.ACHInterbank.Tests;

public sealed class IntegrationBootstrapperTests
{
    [Fact]
    public async Task CatalogBootstrapper_ShouldSeedExpectedCatalog_AndBeIdempotent()
    {
        await using var fixture = await ContextFixture.CreateAsync();

        var bootstrapper = new IntegrationCatalogBootstrapper(fixture.Context);
        await bootstrapper.EnsureAsync();
        var firstCounts = await ReadCountsAsync(fixture.Context);

        await bootstrapper.EnsureAsync();
        var secondCounts = await ReadCountsAsync(fixture.Context);

        Assert.Equal(3, firstCounts.Methods);
        Assert.Equal(27, firstCounts.TransaccionesParameters);
        Assert.Equal(22, firstCounts.ContrapartidasParameters);
        Assert.Equal(7, firstCounts.RespuestaParameters);
        Assert.Equal(68, firstCounts.TransaccionesSourceFields);
        Assert.Equal(68, firstCounts.ContrapartidasSourceFields);
        Assert.Equal(68, firstCounts.RespuestaSourceFields);
        AssertCountsEqual(firstCounts, secondCounts);
    }

    [Fact]
    public async Task MappingBootstrapper_ShouldSeedPublishedBaseMappings_AndBeIdempotent()
    {
        await using var fixture = await ContextFixture.CreateAsync();

        var bootstrapper = new IntegrationMappingBootstrapper(fixture.Context);
        await bootstrapper.EnsureAsync();
        var firstCounts = await ReadCountsAsync(fixture.Context);

        await bootstrapper.EnsureAsync();
        var secondCounts = await ReadCountsAsync(fixture.Context);

        Assert.Equal(3, firstCounts.PublishedSets);
        Assert.Equal(42, firstCounts.MappingRules);
        Assert.Equal(2, firstCounts.ResponseStatusMappings);
        Assert.Equal(18, firstCounts.TransaccionesPublishedRules);
        Assert.Equal(17, firstCounts.ContrapartidasPublishedRules);
        Assert.Equal(7, firstCounts.RespuestaPublishedRules);
        Assert.Equal(0, firstCounts.ContrapartidasOptionalPublishedRules);
        AssertCountsEqual(firstCounts, secondCounts);
    }

    [Fact]
    public async Task MappingBootstrapper_ShouldPersistCanonicalSnapshotHistory()
    {
        await using var fixture = await ContextFixture.CreateAsync();

        var bootstrapper = new IntegrationMappingBootstrapper(fixture.Context);
        await bootstrapper.EnsureAsync();

        var method = await fixture.Context.IntegrationMethods.SingleAsync(x => x.Code == "WSCFAACH.Proc_Transacciones");
        var published = await fixture.Context.IntegrationMappingSets.SingleAsync(x =>
            x.MethodId == method.Id
            && x.Status == IntegrationMappingSetStatusEnum.Published
            && x.Name == "ProcTransacciones Published NACHA desagregado");
        var history = await fixture.Context.IntegrationMappingSetHistory.SingleAsync(x =>
            x.MappingSetId == published.Id
            && x.Action == "SeedPublishedReference");
        var snapshot = await new IntegrationMappingSnapshotBuilder(fixture.Context).BuildAsync(published.Id);

        Assert.Equal(snapshot.SnapshotJson, history.SnapshotJson);
        Assert.Equal(snapshot.SnapshotHash, history.SnapshotHash);
        Assert.Contains("\"MappingSetId\"", history.SnapshotJson);
        Assert.Contains("\"Parameters\"", history.SnapshotJson);
        Assert.DoesNotContain("\"mappingSet\"", history.SnapshotJson, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task DbInitializer_Development_ShouldSeedDemoMappings_AndBeIdempotent()
    {
        await using var fixture = await ServiceFixture.CreateAsync("Development", registerScenarioSeeder: true);

        await DbInitializer.SeedAllAsync(fixture.Provider);
        var firstCounts = await fixture.ReadCountsAsync();

        await DbInitializer.SeedAllAsync(fixture.Provider);
        var secondCounts = await fixture.ReadCountsAsync();

        Assert.Equal(6, firstCounts.MappingSets);
        Assert.Equal(3, firstCounts.DemoSets);
        Assert.Contains(firstCounts.MappingSetNames, x => x == "ProcContrapartidas Draft Valido");
        Assert.Contains(firstCounts.MappingSetNames, x => x == "ProcContrapartidas Draft Invalido");
        Assert.Contains(firstCounts.MappingSetNames, x => x == "ProcContrapartidas Clone Draft");
        AssertCountsEqual(firstCounts, secondCounts);
    }

    [Fact]
    public async Task DbInitializer_Testing_ShouldSeedDemoMappings()
    {
        await using var fixture = await ServiceFixture.CreateAsync("Testing", registerScenarioSeeder: true);

        await DbInitializer.SeedAllAsync(fixture.Provider);
        var counts = await fixture.ReadCountsAsync();

        Assert.Equal(3, counts.DemoSets);
        Assert.Contains(counts.MappingSetNames, x => x == "ProcContrapartidas Draft Valido");
        Assert.Contains(counts.MappingSetNames, x => x == "ProcContrapartidas Draft Invalido");
        Assert.Contains(counts.MappingSetNames, x => x == "ProcContrapartidas Clone Draft");
    }

    [Fact]
    public async Task DbInitializer_Production_ShouldNotCreateDemoMappings()
    {
        await using var fixture = await ServiceFixture.CreateAsync("Production", registerScenarioSeeder: true);

        await DbInitializer.SeedAllAsync(fixture.Provider);
        var counts = await fixture.ReadCountsAsync();

        Assert.Equal(3, counts.PublishedSets);
        Assert.Equal(0, counts.DemoSets);
        Assert.DoesNotContain(counts.MappingSetNames, x => x.Contains("Draft", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(counts.MappingSetNames, x => x == "ProcContrapartidas Published");
        Assert.Contains(counts.MappingSetNames, x => x == "ProcTransacciones Published NACHA desagregado");
        Assert.Contains(counts.MappingSetNames, x => x == "RegistrarRespuestaTransaccion Published respuesta diferencial");
    }

    [Fact]
    public async Task ProcContrapartidas_BaseMapping_ShouldKeepFiveOptionalParametersReserved()
    {
        await using var fixture = await ContextFixture.CreateAsync();
        await new IntegrationMappingBootstrapper(fixture.Context).EnsureAsync();

        var method = await fixture.Context.IntegrationMethods.SingleAsync(x => x.Code == "WSCFAACH.Proc_Contrapartidas");
        var parameters = await fixture.Context.IntegrationMethodParameters.Where(x => x.MethodId == method.Id).ToListAsync();
        var published = await fixture.Context.IntegrationMappingSets.SingleAsync(x => x.MethodId == method.Id && x.Status == IntegrationMappingSetStatusEnum.Published);
        var rules = await fixture.Context.IntegrationMappingRules.Where(x => x.MappingSetId == published.Id).ToListAsync();

        Assert.Equal(22, parameters.Count);
        Assert.Equal(17, parameters.Count(x => x.Required));
        Assert.Equal(5, parameters.Count(x => !x.Required));
        Assert.Equal(17, rules.Count);
        Assert.Equal(5, parameters.Count(x => !x.Required && !rules.Any(r => r.ParameterId == x.Id)));
        Assert.Contains(parameters, x => x.ParameterPath == "ANSIDLOTE" && !x.Required && x.IsActive);
        Assert.Contains(parameters, x => x.ParameterPath == "ANSST" && !x.Required && x.IsActive);
        Assert.Contains(parameters, x => x.ParameterPath == "ANCLC" && !x.Required && x.IsActive);
        Assert.Contains(parameters, x => x.ParameterPath == "ANSIDTX" && !x.Required && x.IsActive);
        Assert.Contains(parameters, x => x.ParameterPath == "ANSIDREVER" && !x.Required && x.IsActive);
    }

    [Fact]
    public async Task ProcTransacciones_BaseMapping_ShouldKeepObservedOptionalInputsAndResponseOutputs()
    {
        await using var fixture = await ContextFixture.CreateAsync();
        await new IntegrationMappingBootstrapper(fixture.Context).EnsureAsync();

        var method = await fixture.Context.IntegrationMethods.SingleAsync(x => x.Code == "WSCFAACH.Proc_Transacciones");
        var parameters = await fixture.Context.IntegrationMethodParameters
            .Where(x => x.MethodId == method.Id && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ToListAsync();
        var published = await fixture.Context.IntegrationMappingSets.SingleAsync(x => x.MethodId == method.Id && x.Status == IntegrationMappingSetStatusEnum.Published);
        var rules = await fixture.Context.IntegrationMappingRules.Where(x => x.MappingSetId == published.Id).ToListAsync();

        Assert.Equal(27, parameters.Count);
        Assert.Contains(parameters, x => x.ParameterPath == "NCTAORIG" && !x.Required && x.Direction == IntegrationParameterDirectionEnum.Input);
        Assert.Contains(parameters, x => x.ParameterPath == "DISCRE" && !x.Required && x.Direction == IntegrationParameterDirectionEnum.Input);
        Assert.DoesNotContain(parameters, x => x.ParameterPath == "ILR");
        Assert.All(new[] { "TREG", "CONV", "PROD", "REGLOTE", "LIBRE", "DIRECCIONIP", "LIBRE1" },
            path => Assert.Contains(parameters, x => x.ParameterPath == path && !x.Required));
        Assert.DoesNotContain(rules, x => string.Equals(x.DefaultValue, "SEED", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(parameters, x => x.ParameterPath == "MONTO" && x.Required && x.Direction == IntegrationParameterDirectionEnum.Input);
        Assert.Contains(parameters, x => x.ParameterPath == "RTAACH" && !x.Required && x.Direction == IntegrationParameterDirectionEnum.Output);
        Assert.Contains(parameters, x => x.ParameterPath == "RTALOC" && !x.Required && x.Direction == IntegrationParameterDirectionEnum.Output);
        Assert.DoesNotContain(rules, x => parameters.Any(p => p.Id == x.ParameterId && p.Direction == IntegrationParameterDirectionEnum.Output));
    }

    [Fact]
    public async Task RegistrarRespuestaTransaccion_Catalog_ShouldUseRealWsdlParameters_AndExcludeNonWsdlAns()
    {
        await using var fixture = await ContextFixture.CreateAsync();
        await new IntegrationMappingBootstrapper(fixture.Context).EnsureAsync();

        var method = await fixture.Context.IntegrationMethods.SingleAsync(x => x.Code == "WSAXON.RegistrarRespuestaTransaccion");
        var parameters = await fixture.Context.IntegrationMethodParameters
            .Where(x => x.MethodId == method.Id && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .Select(x => new { x.ParameterPath, x.Required })
            .ToListAsync();

        Assert.Equal(
            ["idCanal", "nombreCanal", "idTransaccion", "idEstado", "causal", "idTransaccionAxon", "descripcionCausal"],
            parameters.Select(x => x.ParameterPath).ToArray());
        Assert.Equal(["idCanal", "nombreCanal", "idTransaccion", "idEstado", "idTransaccionAxon"], parameters.Where(x => x.Required).Select(x => x.ParameterPath).ToArray());
        Assert.DoesNotContain(parameters, x => x.ParameterPath.StartsWith("ANS", StringComparison.OrdinalIgnoreCase));
        Assert.False(await fixture.Context.IntegrationMethods.AnyAsync(x => x.Code.Contains("PLValidarUsuarioBV")));
    }

    [Fact]
    public async Task RegistrarRespuestaTransaccion_Bootstrapper_ShouldArchiveIncorrectSeedMapping_WithoutDeletingHistory()
    {
        await using var fixture = await ContextFixture.CreateAsync();
        var method = new IntegrationMethod
        {
            Code = "WSAXON.RegistrarRespuestaTransaccion",
            DisplayName = "RegistrarRespuestaTransaccion",
            SoapClientCode = "WsAxonRespuestaTransaccionesSoapClient",
            IsActive = true
        };
        fixture.Context.IntegrationMethods.Add(method);
        await fixture.Context.SaveChangesAsync();

        var invalidSeedParameters = new[]
        {
            InvalidRegistrarSeedParameter(method.Id, "ANSIDLOTE", "Id lote no WSDL", "int", true, 1),
            InvalidRegistrarSeedParameter(method.Id, "ANSST", "Estado no WSDL", "string", true, 2),
            InvalidRegistrarSeedParameter(method.Id, "ANCLC", "Codigo no WSDL", "string", false, 3),
            InvalidRegistrarSeedParameter(method.Id, "ANSIDTX", "Id transaccion no WSDL", "string", true, 4),
            InvalidRegistrarSeedParameter(method.Id, "ANSIDREVER", "Id reverso no WSDL", "int", false, 5)
        };
        var invalidSeedSet = new IntegrationMappingSet
        {
            MethodId = method.Id,
            Name = "RegistrarRespuestaTransaccion Published respuesta diferencial",
            Version = 1,
            Status = IntegrationMappingSetStatusEnum.Published,
            IsActive = true,
            PublishedAtUtc = DateTime.UtcNow,
            PublishedBy = "seed"
        };
        fixture.Context.IntegrationMethodParameters.AddRange(invalidSeedParameters);
        fixture.Context.IntegrationMappingSets.Add(invalidSeedSet);
        await fixture.Context.SaveChangesAsync();

        fixture.Context.IntegrationMappingRules.AddRange(invalidSeedParameters.Select((parameter, index) => new IntegrationMappingRule
        {
            MappingSetId = invalidSeedSet.Id,
            MethodId = method.Id,
            ParameterId = parameter.Id,
            SourceKind = IntegrationSourceKindEnum.DifferentialResponse,
            SourceFieldPath = index switch
            {
                0 => "batchHeaders.batchNumber",
                1 => "differentialResponse.codigoEstadoExterno",
                2 => "differentialResponse.codigoCausalExterna",
                3 => "entryDetails.sequenceNumber",
                _ => "addendaRecords.originalTraceNumber"
            },
            Priority = 1,
            Enabled = true
        }));
        fixture.Context.IntegrationMappingSetHistory.Add(new IntegrationMappingSetHistory
        {
            MappingSetId = invalidSeedSet.Id,
            MethodId = method.Id,
            Version = invalidSeedSet.Version,
            Status = invalidSeedSet.Status,
            Action = "SeedPublishedReference",
            PerformedBy = "seed",
            SnapshotJson = "{}",
            SnapshotHash = "incorrect-seed"
        });
        await fixture.Context.SaveChangesAsync();

        await new IntegrationMappingBootstrapper(fixture.Context).EnsureAsync();

        var sets = await fixture.Context.IntegrationMappingSets
            .Where(x => x.MethodId == method.Id)
            .OrderBy(x => x.Version)
            .ToListAsync();
        var activeParameters = await fixture.Context.IntegrationMethodParameters
            .Where(x => x.MethodId == method.Id && x.IsActive)
            .Select(x => x.ParameterPath)
            .ToListAsync();

        Assert.Contains(sets, x => x.Id == invalidSeedSet.Id && x.Status == IntegrationMappingSetStatusEnum.Archived && !x.IsActive);
        Assert.Contains(sets, x => x.Id != invalidSeedSet.Id && x.Status == IntegrationMappingSetStatusEnum.Published && x.IsActive);
        Assert.DoesNotContain(activeParameters, x => x.StartsWith("ANS", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(await fixture.Context.IntegrationMappingSetHistory.ToListAsync(), x => x.MappingSetId == invalidSeedSet.Id && x.Action == "ArchivedInvalidSeedContract");
    }

    [Fact]
    public async Task RegistrarRespuestaTransaccion_Bootstrapper_ShouldArchiveManualNonWsdlMapping()
    {
        await using var fixture = await ContextFixture.CreateAsync();
        var method = new IntegrationMethod
        {
            Code = "WSAXON.RegistrarRespuestaTransaccion",
            DisplayName = "RegistrarRespuestaTransaccion",
            SoapClientCode = "WsAxonRespuestaTransaccionesSoapClient",
            IsActive = true
        };
        fixture.Context.IntegrationMethods.Add(method);
        await fixture.Context.SaveChangesAsync();

        var nonWsdlParameter = new IntegrationMethodParameter
        {
            MethodId = method.Id,
            ParameterPath = "ANSIDTX",
            DisplayName = "Id transaccion no WSDL manual",
            DescriptionEs = "Parametro manual fuera del WSDL vigente.",
            Category = "Contrato no WSDL",
            ExampleValue = "TX-1",
            UiHelpText = "No corresponde al WSDL de RegistrarRespuestaTransaccion.",
            DataType = "string",
            Direction = IntegrationParameterDirectionEnum.Input,
            Cardinality = IntegrationParameterCardinalityEnum.Scalar,
            Required = true,
            SortOrder = 1,
            IsActive = true
        };
        var manualSet = new IntegrationMappingSet
        {
            MethodId = method.Id,
            Name = "RegistrarRespuestaTransaccion Manual publicado",
            Version = 7,
            Status = IntegrationMappingSetStatusEnum.Published,
            IsActive = true,
            PublishedAtUtc = DateTime.UtcNow,
            PublishedBy = "operador-uat"
        };
        fixture.Context.IntegrationMethodParameters.Add(nonWsdlParameter);
        fixture.Context.IntegrationMappingSets.Add(manualSet);
        await fixture.Context.SaveChangesAsync();

        fixture.Context.IntegrationMappingRules.Add(new IntegrationMappingRule
        {
            MappingSetId = manualSet.Id,
            MethodId = method.Id,
            ParameterId = nonWsdlParameter.Id,
            SourceKind = IntegrationSourceKindEnum.DifferentialResponse,
            SourceFieldPath = "differentialResponse.idTransaccion",
            Priority = 1,
            Enabled = true
        });
        await fixture.Context.SaveChangesAsync();

        await new IntegrationMappingBootstrapper(fixture.Context).EnsureAsync();

        var sets = await fixture.Context.IntegrationMappingSets
            .Where(x => x.MethodId == method.Id)
            .ToListAsync();
        var activeParameters = await fixture.Context.IntegrationMethodParameters
            .Where(x => x.MethodId == method.Id && x.IsActive)
            .OrderBy(x => x.SortOrder)
            .Select(x => x.ParameterPath)
            .ToListAsync();

        Assert.Equal(2, sets.Count);
        Assert.Contains(sets, x => x.Id == manualSet.Id && x.Status == IntegrationMappingSetStatusEnum.Archived && !x.IsActive);
        Assert.Contains(sets, x => x.Id != manualSet.Id && x.Status == IntegrationMappingSetStatusEnum.Published && x.IsActive);
        Assert.Equal(
            ["idCanal", "nombreCanal", "idTransaccion", "idEstado", "causal", "idTransaccionAxon", "descripcionCausal"],
            activeParameters);
        Assert.Contains(await fixture.Context.IntegrationMappingSetHistory.ToListAsync(), x => x.MappingSetId == manualSet.Id && x.Action == "ArchivedInvalidSeedContract");
    }

    private sealed record SeedCounts(
        int Methods,
        int TransaccionesParameters,
        int ContrapartidasParameters,
        int RespuestaParameters,
        int TransaccionesSourceFields,
        int ContrapartidasSourceFields,
        int RespuestaSourceFields,
        int MappingSets,
        int PublishedSets,
        int DemoSets,
        int MappingRules,
        int TransaccionesPublishedRules,
        int ContrapartidasPublishedRules,
        int RespuestaPublishedRules,
        int ContrapartidasOptionalPublishedRules,
        int ResponseStatusMappings,
        IReadOnlyCollection<string> MappingSetNames);

    private static void AssertCountsEqual(SeedCounts expected, SeedCounts actual)
    {
        Assert.Equal(expected.Methods, actual.Methods);
        Assert.Equal(expected.TransaccionesParameters, actual.TransaccionesParameters);
        Assert.Equal(expected.ContrapartidasParameters, actual.ContrapartidasParameters);
        Assert.Equal(expected.RespuestaParameters, actual.RespuestaParameters);
        Assert.Equal(expected.TransaccionesSourceFields, actual.TransaccionesSourceFields);
        Assert.Equal(expected.ContrapartidasSourceFields, actual.ContrapartidasSourceFields);
        Assert.Equal(expected.RespuestaSourceFields, actual.RespuestaSourceFields);
        Assert.Equal(expected.MappingSets, actual.MappingSets);
        Assert.Equal(expected.PublishedSets, actual.PublishedSets);
        Assert.Equal(expected.DemoSets, actual.DemoSets);
        Assert.Equal(expected.MappingRules, actual.MappingRules);
        Assert.Equal(expected.TransaccionesPublishedRules, actual.TransaccionesPublishedRules);
        Assert.Equal(expected.ContrapartidasPublishedRules, actual.ContrapartidasPublishedRules);
        Assert.Equal(expected.RespuestaPublishedRules, actual.RespuestaPublishedRules);
        Assert.Equal(expected.ContrapartidasOptionalPublishedRules, actual.ContrapartidasOptionalPublishedRules);
        Assert.Equal(expected.ResponseStatusMappings, actual.ResponseStatusMappings);
        Assert.Equal(expected.MappingSetNames.OrderBy(x => x), actual.MappingSetNames.OrderBy(x => x));
    }

    private static async Task<SeedCounts> ReadCountsAsync(AchDbContext context)
    {
        var methods = await context.IntegrationMethods.AsNoTracking().ToDictionaryAsync(x => x.Code, x => x.Id);
        var parameters = await context.IntegrationMethodParameters.AsNoTracking().ToListAsync();
        var sources = await context.IntegrationSourceCatalogFields.AsNoTracking().ToListAsync();
        var mappingSets = await context.IntegrationMappingSets.AsNoTracking().ToListAsync();
        var mappingRules = await context.IntegrationMappingRules.AsNoTracking().ToListAsync();
        var responseMappings = await context.AchResponseStatusMappings.AsNoTracking().CountAsync();

        int CountParams(string code)
            => methods.TryGetValue(code, out var id)
                ? parameters.Count(x => x.MethodId == id)
                : 0;

        int CountSources(string code)
            => methods.TryGetValue(code, out var id)
                ? sources.Count(x => x.MethodId == id)
                : 0;

        int CountRules(string code)
            => methods.TryGetValue(code, out var id)
                ? mappingRules.Count(x => x.MethodId == id && mappingSets.Any(s => s.Id == x.MappingSetId && s.Status == IntegrationMappingSetStatusEnum.Published))
                : 0;

        var contrapartidasOptionalPublishedRules = methods.TryGetValue("WSCFAACH.Proc_Contrapartidas", out var contrapartidasId)
            ? parameters.Count(x => x.MethodId == contrapartidasId && !x.Required && mappingRules.Any(r =>
                r.ParameterId == x.Id
                && mappingSets.Any(s => s.Id == r.MappingSetId && s.Status == IntegrationMappingSetStatusEnum.Published)))
            : 0;

        var demoContrapartidasId = methods.TryGetValue("WSCFAACH.Proc_Contrapartidas", out var demoMethodId)
            ? demoMethodId
            : -1;

        return new SeedCounts(
            Methods: methods.Count,
            TransaccionesParameters: CountParams("WSCFAACH.Proc_Transacciones"),
            ContrapartidasParameters: CountParams("WSCFAACH.Proc_Contrapartidas"),
            RespuestaParameters: CountParams("WSAXON.RegistrarRespuestaTransaccion"),
            TransaccionesSourceFields: CountSources("WSCFAACH.Proc_Transacciones"),
            ContrapartidasSourceFields: CountSources("WSCFAACH.Proc_Contrapartidas"),
            RespuestaSourceFields: CountSources("WSAXON.RegistrarRespuestaTransaccion"),
            MappingSets: mappingSets.Count,
            PublishedSets: mappingSets.Count(x => x.Status == IntegrationMappingSetStatusEnum.Published),
            DemoSets: demoContrapartidasId > 0
                ? mappingSets.Count(x => x.MethodId == demoContrapartidasId && x.Name.Contains("Draft", StringComparison.OrdinalIgnoreCase))
                : 0,
            MappingRules: mappingRules.Count,
            TransaccionesPublishedRules: CountRules("WSCFAACH.Proc_Transacciones"),
            ContrapartidasPublishedRules: CountRules("WSCFAACH.Proc_Contrapartidas"),
            RespuestaPublishedRules: CountRules("WSAXON.RegistrarRespuestaTransaccion"),
            ContrapartidasOptionalPublishedRules: contrapartidasOptionalPublishedRules,
            ResponseStatusMappings: responseMappings,
            MappingSetNames: mappingSets.Select(x => x.Name).ToList());
    }

    private static IntegrationMethodParameter InvalidRegistrarSeedParameter(
        int methodId,
        string parameterPath,
        string displayName,
        string dataType,
        bool required,
        int sortOrder)
        => new()
        {
            MethodId = methodId,
            ParameterPath = parameterPath,
            DisplayName = displayName,
            DescriptionEs = "Parametro de seed incorrecto fuera del WSDL validado.",
            Category = "Contrato no WSDL",
            ExampleValue = "SEED",
            UiHelpText = "Se conserva solo para probar normalizacion del seed incorrecto.",
            DataType = dataType,
            Direction = IntegrationParameterDirectionEnum.Input,
            Cardinality = IntegrationParameterCardinalityEnum.Scalar,
            Required = required,
            SortOrder = sortOrder,
            IsActive = true
        };

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

            var options = new DbContextOptionsBuilder<AchDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new AchDbContext(options);
            await context.Database.EnsureCreatedAsync();

            return new ContextFixture(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private sealed class ServiceFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private ServiceFixture(SqliteConnection connection, ServiceProvider provider)
        {
            _connection = connection;
            Provider = provider;
        }

        public ServiceProvider Provider { get; }

        public static async Task<ServiceFixture> CreateAsync(string environmentName, bool registerScenarioSeeder)
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var services = new ServiceCollection();
            services.AddSingleton<IHostEnvironment>(new TestHostEnvironment(environmentName));
            services.AddDbContext<AchDbContext>(options => options.UseSqlite(connection));

            if (registerScenarioSeeder)
            {
                services.AddScoped<IDbSeeder, IntegrationMappingScenarioSeeder>();
            }

            var provider = services.BuildServiceProvider();
            using (var scope = provider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<AchDbContext>();
                await context.Database.EnsureCreatedAsync();
                return new ServiceFixture(connection, (ServiceProvider)provider);
            }
        }

        public async Task<SeedCounts> ReadCountsAsync()
        {
            using var scope = Provider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<AchDbContext>();
            return await IntegrationBootstrapperTests.ReadCountsAsync(context);
        }

        public async ValueTask DisposeAsync()
        {
            await Provider.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public TestHostEnvironment(string environmentName)
        {
            EnvironmentName = environmentName;
            ApplicationName = nameof(IntegrationBootstrapperTests);
            ContentRootPath = AppContext.BaseDirectory;
        }

        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; }
        public string ContentRootPath { get; set; }
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
