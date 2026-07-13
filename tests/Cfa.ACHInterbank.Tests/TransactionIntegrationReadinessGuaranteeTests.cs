using Cfa.ACHInterbank.Application.ACH.Responses.Notification.Services;
using Cfa.ACHInterbank.Application.External.Connections;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Integrations.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class TransactionIntegrationReadinessGuaranteeTests
{
    [Fact]
    public async Task DebitOriginatedByCfa_ShouldResolve_ProcContrapartidas_MonetaryDebitRequest()
    {
        await using var fixture = await GuaranteeFixture.CreateAsync();

        var result = await fixture.OperationResolver.ResolveAsync(fixture.DebitFromCfa);

        Assert.True(result.IsSupported);
        Assert.Equal(IntegrationGuaranteeConstants.Wscfaach, result.IntegrationKey);
        Assert.Equal(IntegrationGuaranteeConstants.ProcContrapartidas, result.OperationKey);
        Assert.Equal(IntegrationGuaranteeConstants.MonetaryDebitRequest, result.MappingPurpose);
        Assert.Equal(IntegrationGuaranteeConstants.OutboundRequest, result.MappingDirection);
        Assert.True(result.MovesMoney);
    }

    [Fact]
    public async Task CreditOriginatedByExternalInstitution_ShouldResolve_ProcTransacciones_MonetaryCreditRequest()
    {
        await using var fixture = await GuaranteeFixture.CreateAsync();

        var result = await fixture.OperationResolver.ResolveAsync(fixture.CreditFromExternal);

        Assert.True(result.IsSupported);
        Assert.Equal(IntegrationGuaranteeConstants.Wscfaach, result.IntegrationKey);
        Assert.Equal(IntegrationGuaranteeConstants.ProcTransacciones, result.OperationKey);
        Assert.Equal(IntegrationGuaranteeConstants.MonetaryCreditRequest, result.MappingPurpose);
        Assert.Equal(IntegrationGuaranteeConstants.OutboundRequest, result.MappingDirection);
        Assert.True(result.MovesMoney);
    }

    [Fact]
    public async Task DifferentialResponse_ShouldResolve_RegistrarRespuestaTransaccion_NonMonetary()
    {
        await using var fixture = await GuaranteeFixture.CreateAsync();

        var result = fixture.OperationResolver.ResolveDifferentialResponse("RESP-001", fixture.CreditFromExternal.Id);

        Assert.True(result.IsSupported);
        Assert.Equal(IntegrationGuaranteeConstants.WsAxon, result.IntegrationKey);
        Assert.Equal(IntegrationGuaranteeConstants.RegistrarRespuestaTransaccion, result.OperationKey);
        Assert.Equal(IntegrationGuaranteeConstants.DifferentialResponseNotification, result.MappingPurpose);
        Assert.Equal(IntegrationGuaranteeConstants.InboundResponse, result.MappingDirection);
        Assert.False(result.MovesMoney);
    }

    [Fact]
    public async Task RegistrarRespuestaTransaccion_ShouldHave_MovesMoneyFalse()
    {
        await using var fixture = await GuaranteeFixture.CreateAsync();
        var result = fixture.OperationResolver.ResolveDifferentialResponse();

        Assert.False(result.MovesMoney);
        Assert.Equal(IntegrationGuaranteeConstants.DifferentialResponseNotification, result.MappingPurpose);
    }

    [Fact]
    public async Task Readiness_ShouldBeOk_WhenRequiredMappingsAreActive()
    {
        await using var fixture = await GuaranteeFixture.CreateAsync();
        await fixture.PublishCompleteMappingAsync(IntegrationGuaranteeConstants.ProcTransacciones);
        var operation = await fixture.OperationResolver.ResolveAsync(fixture.CreditFromExternal);

        var readiness = await fixture.ReadinessService.EvaluateAsync(operation);

        Assert.True(readiness.IsReady);
        Assert.Equal("Ok", readiness.Status);
        Assert.False(readiness.UsesFallback);
        Assert.Empty(readiness.MissingRequiredMappings);
    }

    [Fact]
    public async Task ProcTransaccionesReadiness_ShouldNotBeOk_WhenSeededCriticalFieldsUsePlaceholders()
    {
        await using var fixture = await GuaranteeFixture.CreateAsync();
        var operation = await fixture.OperationResolver.ResolveAsync(fixture.CreditFromExternal);

        var readiness = await fixture.ReadinessService.EvaluateAsync(operation);

        Assert.False(readiness.IsReady);
        Assert.Equal("Failed", readiness.Status);
        Assert.Equal("FUNCTIONAL_MAPPING_PLACEHOLDER", readiness.Code);
        Assert.False(readiness.UsesFallback);
        Assert.False(readiness.CanBuildPayload);
        Assert.DoesNotContain(readiness.Errors, x => x.Contains("TREG", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(readiness.Errors, x => x.Contains("IDLOTE", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(readiness.Errors, x => x.Contains("BCORECEP", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Readiness_ShouldFail_WhenRequiredMappingsAreInactive()
    {
        await using var fixture = await GuaranteeFixture.CreateAsync();
        await fixture.PublishCompleteMappingAsync(IntegrationGuaranteeConstants.ProcTransacciones, disableFirstRequired: true);
        var operation = await fixture.OperationResolver.ResolveAsync(fixture.CreditFromExternal);

        var readiness = await fixture.ReadinessService.EvaluateAsync(operation);

        Assert.False(readiness.IsReady);
        Assert.Equal("Failed", readiness.Status);
        Assert.NotEmpty(readiness.InactiveRequiredMappings);
    }

    [Fact]
    public async Task ProcContrapartidasReadiness_ShouldNotBeOk_WhenMonetaryOrDirectionFieldsUsePlaceholders()
    {
        await using var fixture = await GuaranteeFixture.CreateAsync();
        var operation = await fixture.OperationResolver.ResolveAsync(fixture.DebitFromCfa);

        var readiness = await fixture.ReadinessService.EvaluateAsync(operation);

        Assert.False(readiness.IsReady);
        Assert.Equal("Failed", readiness.Status);
        Assert.Equal("FUNCTIONAL_MAPPING_PLACEHOLDER", readiness.Code);
        Assert.False(readiness.UsesFallback);
        Assert.False(readiness.CanBuildPayload);
        Assert.Empty(readiness.RequiredFallbackFields);
        Assert.Contains(readiness.Errors, x => x.Contains("OFMONDEB", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(readiness.Errors, x => x.Contains("OFDD", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Readiness_ShouldNotUseFallback_WhenProcContrapartidasIsBootstrapPublished()
    {
        await using var fixture = await GuaranteeFixture.CreateAsync();
        var operation = await fixture.OperationResolver.ResolveAsync(fixture.DebitFromCfa);

        var readiness = await fixture.ReadinessService.EvaluateAsync(operation);

        Assert.Equal("Failed", readiness.Status);
        Assert.False(readiness.IsReady);
        Assert.False(readiness.UsesFallback);
    }

    [Fact]
    public async Task Readiness_ShouldFail_WhenRequiredFunctionalParameterUsesSeedPlaceholder()
    {
        await using var fixture = await GuaranteeFixture.CreateAsync();
        await fixture.PublishCompleteMappingAsync(
            IntegrationGuaranteeConstants.ProcTransacciones,
            configureRule: (parameter, rule) =>
            {
                if (parameter.ParameterPath == "TREG")
                {
                    rule.SourceKind = IntegrationSourceKindEnum.Constant;
                    rule.SourceFieldPath = string.Empty;
                    rule.FixedValue = "SEED";
                }
            });
        var operation = await fixture.OperationResolver.ResolveAsync(fixture.CreditFromExternal);

        var readiness = await fixture.ReadinessService.EvaluateAsync(operation);

        Assert.False(readiness.IsReady);
        Assert.Equal("FUNCTIONAL_MAPPING_PLACEHOLDER", readiness.Code);
        Assert.Contains(readiness.Errors, x => x.Contains("TREG", StringComparison.OrdinalIgnoreCase)
            && x.Contains("SEED", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Readiness_ShouldFail_WhenCriticalMonetaryParameterUsesZeroPlaceholder()
    {
        await using var fixture = await GuaranteeFixture.CreateAsync();
        await fixture.PublishCompleteMappingAsync(
            IntegrationGuaranteeConstants.ProcContrapartidas,
            configureRule: (parameter, rule) =>
            {
                if (parameter.ParameterPath == "OFMONDEB")
                {
                    rule.SourceKind = IntegrationSourceKindEnum.Constant;
                    rule.SourceFieldPath = string.Empty;
                    rule.FixedValue = "0";
                }
            });
        var operation = await fixture.OperationResolver.ResolveAsync(fixture.DebitFromCfa);

        var readiness = await fixture.ReadinessService.EvaluateAsync(operation);

        Assert.False(readiness.IsReady);
        Assert.Equal("FUNCTIONAL_MAPPING_PLACEHOLDER", readiness.Code);
        Assert.Contains(readiness.Errors, x => x.Contains("OFMONDEB", StringComparison.OrdinalIgnoreCase)
            && x.Contains("0", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Readiness_ShouldAllow_ProcContrapartidasObservedFunctionalConstants()
    {
        await using var fixture = await GuaranteeFixture.CreateAsync();
        await fixture.PublishCompleteMappingAsync(
            IntegrationGuaranteeConstants.ProcContrapartidas,
            configureRule: (parameter, rule) =>
            {
                var value = parameter.ParameterPath switch
                {
                    "OFDD" => "TRANSFER  ",
                    "OFMONCRE" => "0",
                    "OFST" => "OO",
                    "OFIDTX" => "0",
                    "OFIDREVER" => "0",
                    "OFIDEBAPLI" => "1",
                    _ => null
                };

                if (value is null)
                {
                    return;
                }

                rule.SourceKind = IntegrationSourceKindEnum.Constant;
                rule.SourceFieldPath = "constant.value";
                rule.FixedValue = value;
                rule.DefaultValue = value;
                rule.TransformationCode = null;
                rule.FormatMask = null;
            });
        var operation = await fixture.OperationResolver.ResolveAsync(fixture.DebitFromCfa);

        var readiness = await fixture.ReadinessService.EvaluateAsync(operation);

        Assert.True(readiness.IsReady);
        Assert.Equal("Ok", readiness.Status);
        Assert.DoesNotContain(readiness.Errors, x => x.Contains("OFDD", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(readiness.Errors, x => x.Contains("OFIDTX", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Readiness_ShouldWarn_WhenTechnicalConstantIsDocumentedButNotFunctional()
    {
        await using var fixture = await GuaranteeFixture.CreateAsync();
        await fixture.PublishCompleteMappingAsync(
            IntegrationGuaranteeConstants.ProcContrapartidas,
            configureRule: (parameter, rule) =>
            {
                if (parameter.ParameterPath == "OFDIRECCIONIP")
                {
                    rule.SourceKind = IntegrationSourceKindEnum.Constant;
                    rule.SourceFieldPath = string.Empty;
                    rule.FixedValue = "0.0.0.0";
                }
            });
        var operation = await fixture.OperationResolver.ResolveAsync(fixture.DebitFromCfa);

        var readiness = await fixture.ReadinessService.EvaluateAsync(operation);

        Assert.True(readiness.IsReady);
        Assert.Equal("ReadyWithWarnings", readiness.Status);
        Assert.Equal("READY_WITH_WARNINGS", readiness.Code);
        Assert.Contains(readiness.Warnings, x => x.Contains("OFDIRECCIONIP", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Readiness_ShouldNotFail_ForProcContrapartidasOptionalReservedAnsFields()
    {
        await using var fixture = await GuaranteeFixture.CreateAsync();
        await fixture.PublishCompleteMappingAsync(IntegrationGuaranteeConstants.ProcContrapartidas);
        var operation = await fixture.OperationResolver.ResolveAsync(fixture.DebitFromCfa);

        var readiness = await fixture.ReadinessService.EvaluateAsync(operation);

        Assert.True(readiness.IsReady);
        Assert.Equal("Ok", readiness.Status);
        Assert.DoesNotContain(readiness.Errors, x => x.Contains("ANS", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(readiness.Warnings, x => x.Contains("ANS", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RegistrarReadiness_ShouldRemainReady_WithSevenWsdlDifferentialResponseSources()
    {
        await using var fixture = await GuaranteeFixture.CreateAsync();
        var operation = fixture.OperationResolver.ResolveDifferentialResponse("RESP-READY", fixture.CreditFromExternal.Id);

        var readiness = await fixture.ReadinessService.EvaluateAsync(operation);
        var registrar = await fixture.Context.IntegrationMethods
            .AsNoTracking()
            .SingleAsync(x => x.Code == "WSAXON.RegistrarRespuestaTransaccion");
        var activeParameters = await fixture.Context.IntegrationMethodParameters
            .AsNoTracking()
            .Where(x => x.MethodId == registrar.Id && x.IsActive)
            .Select(x => x.ParameterPath)
            .ToListAsync();

        Assert.True(readiness.IsReady);
        Assert.Equal("Ok", readiness.Status);
        Assert.Equal("OK", readiness.Code);
        Assert.Equal(7, activeParameters.Count);
        Assert.Contains("idCanal", activeParameters);
        Assert.Contains("nombreCanal", activeParameters);
        Assert.Contains("idTransaccion", activeParameters);
        Assert.Contains("idEstado", activeParameters);
        Assert.Contains("causal", activeParameters);
        Assert.Contains("idTransaccionAxon", activeParameters);
        Assert.Contains("descripcionCausal", activeParameters);
        Assert.DoesNotContain(activeParameters, x => x.StartsWith("ANS", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RegistrarCatalog_ShouldNotContainAnsParameters()
    {
        await using var fixture = await GuaranteeFixture.CreateAsync();
        await fixture.ReadinessService.EvaluateAsync(fixture.OperationResolver.ResolveDifferentialResponse());

        var registrar = await fixture.Context.IntegrationMethods
            .AsNoTracking()
            .SingleAsync(x => x.Code == "WSAXON.RegistrarRespuestaTransaccion");
        var activeParameters = await fixture.Context.IntegrationMethodParameters
            .AsNoTracking()
            .Where(x => x.MethodId == registrar.Id && x.IsActive)
            .Select(x => x.ParameterPath)
            .ToListAsync();

        Assert.DoesNotContain(activeParameters, x => x.StartsWith("ANS", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PLValidarUsuarioBV_ShouldNotBeCataloged()
    {
        await using var fixture = await GuaranteeFixture.CreateAsync();

        var exists = await fixture.Context.IntegrationMethods
            .AsNoTracking()
            .AnyAsync(x => x.Code.Contains("PLValidarUsuarioBV"));

        Assert.False(exists);
    }

    [Fact]
    public async Task Readiness_ShouldExposeWarnings_WhenOnlyNonBlockingDefaultsExist()
    {
        await using var fixture = await GuaranteeFixture.CreateAsync();
        await fixture.PublishCompleteMappingAsync(
            IntegrationGuaranteeConstants.ProcContrapartidas,
            configureRule: (parameter, rule) =>
            {
                if (parameter.ParameterPath == "OFIDCAMCOMPE")
                {
                    rule.SourceKind = IntegrationSourceKindEnum.ClearingHouse;
                    rule.SourceFieldPath = "clearinghouse.id";
                    rule.DefaultValue = "1";
                }
            });
        var operation = await fixture.OperationResolver.ResolveAsync(fixture.DebitFromCfa);

        var readiness = await fixture.ReadinessService.EvaluateAsync(operation);

        Assert.True(readiness.IsReady);
        Assert.Equal("ReadyWithWarnings", readiness.Status);
        Assert.Equal("READY_WITH_WARNINGS", readiness.Code);
        Assert.Contains(readiness.Warnings, x => x.Contains("OFIDCAMCOMPE", StringComparison.OrdinalIgnoreCase)
            && x.Contains("1", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Readiness_ShouldNotInvokeSoap()
    {
        await using var fixture = await GuaranteeFixture.CreateAsync();
        await fixture.PublishCompleteMappingAsync(IntegrationGuaranteeConstants.ProcTransacciones);
        var operation = await fixture.OperationResolver.ResolveAsync(fixture.CreditFromExternal);

        var readiness = await fixture.ReadinessService.EvaluateAsync(operation);

        Assert.True(readiness.IsReady);
        Assert.DoesNotContain(typeof(IWscfaachSoapClient), fixture.ReadinessService.GetType().GetConstructors().SelectMany(x => x.GetParameters()).Select(x => x.ParameterType));
    }

    [Fact]
    public async Task Readiness_ShouldNotChangeTransactionState()
    {
        await using var fixture = await GuaranteeFixture.CreateAsync();
        await fixture.PublishCompleteMappingAsync(IntegrationGuaranteeConstants.ProcTransacciones);
        var before = fixture.CreditFromExternal.State;
        var operation = await fixture.OperationResolver.ResolveAsync(fixture.CreditFromExternal);

        await fixture.ReadinessService.EvaluateAsync(operation);

        var after = await fixture.Context.AchTransactions
            .AsNoTracking()
            .Where(x => x.Id == fixture.CreditFromExternal.Id)
            .Select(x => x.State)
            .SingleAsync();
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task CreatedDebitTransaction_ShouldExposeIntegrationReadinessForProcContrapartidas()
    {
        await using var fixture = await GuaranteeFixture.CreateAsync();

        var readiness = await fixture.TransactionReadinessService.GetTransactionReadinessAsync(fixture.DebitFromCfa.Id);

        Assert.NotNull(readiness);
        Assert.Equal(IntegrationGuaranteeConstants.ProcContrapartidas, readiness!.OperationKey);
        Assert.Equal(IntegrationGuaranteeConstants.MonetaryDebitRequest, readiness.MappingPurpose);
        Assert.True(readiness.MovesMoney);
    }

    [Fact]
    public async Task CreatedCreditTransactionFromExternal_ShouldExposeIntegrationReadinessForProcTransacciones()
    {
        await using var fixture = await GuaranteeFixture.CreateAsync();
        await fixture.PublishCompleteMappingAsync(IntegrationGuaranteeConstants.ProcTransacciones);

        var readiness = await fixture.TransactionReadinessService.GetTransactionReadinessAsync(fixture.CreditFromExternal.Id);

        Assert.NotNull(readiness);
        Assert.Equal(IntegrationGuaranteeConstants.ProcTransacciones, readiness!.OperationKey);
        Assert.False(readiness.Readiness.IsReady);
        Assert.Equal("FUNCTIONAL_MAPPING_PLACEHOLDER", readiness.Readiness.Code);
        Assert.Contains(readiness.Readiness.Errors, x => x.Contains("IDLOTE", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CreatedTransaction_ShouldNotExecuteSoap()
    {
        await using var fixture = await GuaranteeFixture.CreateAsync();

        var readiness = await fixture.TransactionReadinessService.GetTransactionReadinessAsync(fixture.CreditFromExternal.Id);

        Assert.NotNull(readiness);
        Assert.DoesNotContain(typeof(IWscfaachSoapClient), fixture.TransactionReadinessService.GetType().GetConstructors().SelectMany(x => x.GetParameters()).Select(x => x.ParameterType));
    }

    [Fact]
    public async Task CreatedTransaction_ShouldNotChangeStateByReadinessCheck()
    {
        await using var fixture = await GuaranteeFixture.CreateAsync();
        var before = fixture.DebitFromCfa.State;

        await fixture.TransactionReadinessService.GetTransactionReadinessAsync(fixture.DebitFromCfa.Id);

        var after = await fixture.Context.AchTransactions
            .AsNoTracking()
            .Where(x => x.Id == fixture.DebitFromCfa.Id)
            .Select(x => x.State)
            .SingleAsync();
        Assert.Equal(before, after);
    }

    [Fact]
    public async Task DebitFromCfa_ShouldNotResolve_ProcTransacciones()
    {
        await using var fixture = await GuaranteeFixture.CreateAsync();

        var result = await fixture.OperationResolver.ResolveAsync(fixture.DebitFromCfa);

        Assert.NotEqual(IntegrationGuaranteeConstants.ProcTransacciones, result.OperationKey);
        Assert.NotEqual(IntegrationGuaranteeConstants.MonetaryCreditRequest, result.MappingPurpose);
    }

    [Fact]
    public async Task CreditFromExternal_ShouldNotResolve_ProcContrapartidas()
    {
        await using var fixture = await GuaranteeFixture.CreateAsync();

        var result = await fixture.OperationResolver.ResolveAsync(fixture.CreditFromExternal);

        Assert.NotEqual(IntegrationGuaranteeConstants.ProcContrapartidas, result.OperationKey);
        Assert.NotEqual(IntegrationGuaranteeConstants.MonetaryDebitRequest, result.MappingPurpose);
    }

    [Fact]
    public async Task RegistrarRespuestaTransaccion_ShouldNotResolve_MonetaryDebitRequest()
    {
        await using var fixture = await GuaranteeFixture.CreateAsync();

        var result = fixture.OperationResolver.ResolveDifferentialResponse();

        Assert.NotEqual(IntegrationGuaranteeConstants.MonetaryDebitRequest, result.MappingPurpose);
    }

    [Fact]
    public async Task RegistrarRespuestaTransaccion_ShouldNotResolve_MonetaryCreditRequest()
    {
        await using var fixture = await GuaranteeFixture.CreateAsync();

        var result = fixture.OperationResolver.ResolveDifferentialResponse();

        Assert.NotEqual(IntegrationGuaranteeConstants.MonetaryCreditRequest, result.MappingPurpose);
    }

    [Fact]
    public async Task BootstrapPublishedMappings_ShouldNotBeReady_WhenFunctionalPlaceholdersRemain()
    {
        await using var fixture = await GuaranteeFixture.CreateAsync();
        var operation = await fixture.OperationResolver.ResolveAsync(fixture.CreditFromExternal);

        var readiness = await fixture.ReadinessService.EvaluateAsync(operation);

        Assert.False(readiness.IsReady);
        Assert.Equal("Failed", readiness.Status);
        Assert.Equal("FUNCTIONAL_MAPPING_PLACEHOLDER", readiness.Code);
        Assert.Equal(readiness.RequiredMappings, readiness.ActiveMappings);
        Assert.Empty(readiness.MissingRequiredMappings);
    }

    [Fact]
    public async Task RegistrarRespuestaTransaccion_ShouldNotInvokeWscfaachClient()
    {
        var constructorTypes = typeof(NotificarRespuestaAchUseCase)
            .GetConstructors()
            .SelectMany(x => x.GetParameters())
            .Select(x => x.ParameterType)
            .ToList();

        Assert.DoesNotContain(typeof(IWscfaachSoapClient), constructorTypes);
    }

    private sealed class GuaranteeFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private GuaranteeFixture(SqliteConnection connection, AchDbContext context)
        {
            _connection = connection;
            Context = context;
            CatalogService = new IntegrationCatalogService(context);
            OperationResolver = new TransactionIntegrationOperationResolver(context);
            ReadinessService = new IntegrationMappingReadinessService(context, CatalogService);
            TransactionReadinessService = new TransactionIntegrationReadinessService(context, OperationResolver, ReadinessService);
        }

        public AchDbContext Context { get; }
        public IntegrationCatalogService CatalogService { get; }
        public TransactionIntegrationOperationResolver OperationResolver { get; }
        public IntegrationMappingReadinessService ReadinessService { get; }
        public TransactionIntegrationReadinessService TransactionReadinessService { get; }
        public AchTransaction DebitFromCfa { get; private set; } = null!;
        public AchTransaction CreditFromExternal { get; private set; } = null!;

        public static async Task<GuaranteeFixture> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<AchDbContext>()
                .UseSqlite(connection)
                .EnableSensitiveDataLogging()
                .Options;

            var context = new AchDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var fixture = new GuaranteeFixture(connection, context);
            await fixture.SeedAsync();
            return fixture;
        }

        public async Task PublishCompleteMappingAsync(
            string operationKey,
            bool disableFirstRequired = false,
            Action<IntegrationMethodParameter, IntegrationMappingRule>? configureRule = null)
        {
            var integrationKey = operationKey == IntegrationGuaranteeConstants.RegistrarRespuestaTransaccion
                ? IntegrationGuaranteeConstants.WsAxon
                : IntegrationGuaranteeConstants.Wscfaach;
            var method = await Context.IntegrationMethods.SingleAsync(x => x.Code == $"{integrationKey}.{operationKey}");
            var parameters = await Context.IntegrationMethodParameters
                .Where(x => x.MethodId == method.Id && x.IsActive && x.Required && x.Direction == IntegrationParameterDirectionEnum.Input)
                .OrderBy(x => x.SortOrder)
                .ToListAsync();
            var nextVersion = (await Context.IntegrationMappingSets
                .Where(x => x.MethodId == method.Id)
                .Select(x => (int?)x.Version)
                .MaxAsync() ?? 0) + 100;

            var set = new IntegrationMappingSet
            {
                Id = Guid.NewGuid(),
                MethodId = method.Id,
                Name = $"{operationKey} readiness test",
                Version = nextVersion,
                Status = IntegrationMappingSetStatusEnum.Published,
                IsActive = true,
                PublishedAtUtc = DateTime.UtcNow,
                PublishedBy = "test"
            };
            Context.IntegrationMappingSets.Add(set);

            var index = 0;
            foreach (var parameter in parameters)
            {
                var rule = new IntegrationMappingRule
                {
                    MappingSetId = set.Id,
                    MethodId = method.Id,
                    ParameterId = parameter.Id,
                    SourceKind = SourceKindForFunctionalTest(parameter),
                    SourceFieldPath = SourcePathForFunctionalTest(parameter),
                    Priority = 1,
                    Enabled = !(disableFirstRequired && index == 0)
                };
                configureRule?.Invoke(parameter, rule);
                Context.IntegrationMappingRules.Add(rule);
                index++;
            }

            await Context.SaveChangesAsync();
        }

        private async Task SeedAsync()
        {
            await CatalogService.GetMethodsAsync();

            Context.ClearingHouseConfigs.Add(new ClearingHouseConfig
            {
                Id = 1,
                HolidayStrategy = "Colombian"
            });
            Context.ClearingHouses.Add(new ClearingHouse
            {
                Id = 10,
                Name = "ACH Colombia",
                Code = "ACH",
                OriginCode = "0001283",
                ClearingHouseId = 1
            });

            var cfa = new FinancialInstitution
            {
                Id = 1,
                Name = "Cooperativa Financiera de Antioquia",
                RoutingNumber = "0001",
                TransitCode = "0283",
                IsDefaultSource = true,
                Status = FinancialInstitutionStatus.Active
            };
            cfa.CalculateCheckDigit();
            var external = new FinancialInstitution
            {
                Id = 2,
                Name = "Banco UAT Externo",
                RoutingNumber = "9999",
                TransitCode = "0111",
                IsDefaultSource = false,
                Status = FinancialInstitutionStatus.Active
            };
            external.CalculateCheckDigit();
            Context.FinancialInstitutions.AddRange(cfa, external);

            var cycle = new AchCycle
            {
                Id = "READINESS-CYCLE",
                CycleName = "Readiness",
                ProcessingDate = DateTime.UtcNow.Date,
                StartTime = TimeSpan.FromHours(8),
                EndTime = TimeSpan.FromHours(17),
                CutoffTime = TimeSpan.FromHours(16),
                ClearingHouseId = 10
            };
            var batch = new AchBatch
            {
                Id = 900,
                AchCycleId = cycle.Id,
                BatchSequenceNumber = 1,
                EffectiveEntryDate = cycle.ProcessingDate,
                CompanyEntryDescriptionId = 1
            };
            DebitFromCfa = BuildTransaction(101, TransactionTypeEnum.Debit, "UAT-DEB-CFA-101", 1, 2, cycle.Id, batch.Id);
            CreditFromExternal = BuildTransaction(102, TransactionTypeEnum.Credit, "UAT-CRED-EXT-102", 2, 1, cycle.Id, batch.Id);

            Context.AchCycles.Add(cycle);
            Context.AchBatches.Add(batch);
            Context.AchTransactions.AddRange(DebitFromCfa, CreditFromExternal);
            await Context.SaveChangesAsync();
        }

        private static AchTransaction BuildTransaction(
            int id,
            TransactionTypeEnum type,
            string reference,
            int sourceInstitutionId,
            int destinationInstitutionId,
            string cycleId,
            int batchId)
            => new()
            {
                Id = id,
                Amount = 1000m,
                TransactionExternalId = reference,
                Reference = reference,
                Type = type,
                TransactionCode = type == TransactionTypeEnum.Debit ? "27" : "22",
                OriginatingDFI = "0001283",
                ReceivingDFI = "9999111",
                TraceNumber = $"TRACE{id}",
                CompanyIdentification = "900000001",
                SourceAccountNumber = "0000001001",
                DestinationAccountNumber = "0000001002",
                EffectiveEntryDate = DateTime.UtcNow.Date,
                SourceInstitutionId = sourceInstitutionId,
                DestinationInstitutionId = destinationInstitutionId,
                AchCycleId = cycleId,
                AchBatchId = batchId,
                State = AchTransferStateEnum.Pending
            };

        private static IntegrationSourceKindEnum SourceKindForFunctionalTest(IntegrationMethodParameter parameter)
            => SourcePathForFunctionalTest(parameter).Split('.', 2)[0] switch
            {
                "transaction" => IntegrationSourceKindEnum.Transaction,
                "cycle" => IntegrationSourceKindEnum.Cycle,
                _ => IntegrationSourceKindEnum.Transaction
            };

        private static string SourcePathForFunctionalTest(IntegrationMethodParameter parameter)
            => parameter.DataType.ToLowerInvariant() switch
            {
                "int" or "long" => "transaction.id",
                "decimal" or "double" or "float" => "transaction.amount",
                "datetime" => "cycle.processingDate",
                _ => "transaction.reference"
            };

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
