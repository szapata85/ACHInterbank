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
    public async Task Readiness_ShouldBeOk_WhenRequiredMappingsAreBootstrapSeeded()
    {
        await using var fixture = await GuaranteeFixture.CreateAsync();
        var operation = await fixture.OperationResolver.ResolveAsync(fixture.CreditFromExternal);

        var readiness = await fixture.ReadinessService.EvaluateAsync(operation);

        Assert.True(readiness.IsReady);
        Assert.Equal("Ok", readiness.Status);
        Assert.Equal("OK", readiness.Code);
        Assert.False(readiness.UsesFallback);
        Assert.True(readiness.CanBuildPayload);
        Assert.Empty(readiness.Errors);
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
    public async Task Readiness_ShouldBeOk_WhenProcContrapartidasUsesBootstrapPublishedMapping()
    {
        await using var fixture = await GuaranteeFixture.CreateAsync();
        var operation = await fixture.OperationResolver.ResolveAsync(fixture.DebitFromCfa);

        var readiness = await fixture.ReadinessService.EvaluateAsync(operation);

        Assert.True(readiness.IsReady);
        Assert.Equal("Ok", readiness.Status);
        Assert.Equal("OK", readiness.Code);
        Assert.False(readiness.UsesFallback);
        Assert.True(readiness.CanBuildPayload);
        Assert.Empty(readiness.RequiredFallbackFields);
    }

    [Fact]
    public async Task Readiness_ShouldNotUseFallback_WhenProcContrapartidasIsBootstrapPublished()
    {
        await using var fixture = await GuaranteeFixture.CreateAsync();
        var operation = await fixture.OperationResolver.ResolveAsync(fixture.DebitFromCfa);

        var readiness = await fixture.ReadinessService.EvaluateAsync(operation);

        Assert.Equal("Ok", readiness.Status);
        Assert.True(readiness.IsReady);
        Assert.False(readiness.UsesFallback);
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
        Assert.True(readiness.Readiness.IsReady);
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
    public async Task MissingMapping_ShouldExposePublishedMappings_AsReady()
    {
        await using var fixture = await GuaranteeFixture.CreateAsync();
        var operation = await fixture.OperationResolver.ResolveAsync(fixture.CreditFromExternal);

        var readiness = await fixture.ReadinessService.EvaluateAsync(operation);

        Assert.True(readiness.IsReady);
        Assert.Equal("Ok", readiness.Status);
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

        public async Task PublishCompleteMappingAsync(string operationKey, bool disableFirstRequired = false)
        {
            var method = await Context.IntegrationMethods.SingleAsync(x => x.Code == $"{IntegrationGuaranteeConstants.Wscfaach}.{operationKey}");
            var parameters = await Context.IntegrationMethodParameters
                .Where(x => x.MethodId == method.Id && x.IsActive && x.Required && x.Direction == IntegrationParameterDirectionEnum.Input)
                .OrderBy(x => x.SortOrder)
                .ToListAsync();

            var set = new IntegrationMappingSet
            {
                Id = Guid.NewGuid(),
                MethodId = method.Id,
                Name = $"{operationKey} readiness test",
                Version = 1,
                Status = IntegrationMappingSetStatusEnum.Published,
                IsActive = true,
                PublishedAtUtc = DateTime.UtcNow,
                PublishedBy = "test"
            };
            Context.IntegrationMappingSets.Add(set);

            var index = 0;
            foreach (var parameter in parameters)
            {
                Context.IntegrationMappingRules.Add(new IntegrationMappingRule
                {
                    MappingSetId = set.Id,
                    MethodId = method.Id,
                    ParameterId = parameter.Id,
                    SourceKind = IntegrationSourceKindEnum.Constant,
                    FixedValue = DefaultFor(parameter),
                    Priority = 1,
                    Enabled = !(disableFirstRequired && index == 0)
                });
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

        private static string DefaultFor(IntegrationMethodParameter parameter)
            => parameter.DataType.ToLowerInvariant() switch
            {
                "int" or "long" => "1",
                "decimal" or "double" or "float" => "1.00",
                "datetime" => DateTime.UtcNow.ToString("O"),
                _ => "TEST"
            };

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }
}
