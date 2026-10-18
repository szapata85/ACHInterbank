using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Integrations.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class IntegrationContractVersionLifecycleTests
{
    [Fact]
    public async Task Resolver_ShouldReturnActiveBaselineAndExactlySevenRegistrarRespuestaParameters()
    {
        await using var fixture = await ContextFixture.CreateAsync();
        await new IntegrationCatalogBootstrapper(fixture.Context).EnsureAsync();

        var method = await fixture.Context.IntegrationMethods
            .SingleAsync(x => x.Code == "WSAXON.RegistrarRespuestaTransaccion");
        var service = new IntegrationContractVersionService(fixture.Context);

        var active = await service.ResolveAsync(method.Id);
        Assert.NotNull(active);
        Assert.Equal(IntegrationContractVersionStatusEnum.Active, active!.Status);
        Assert.Equal("baseline-v1", active.VersionLabel);

        var parameters = await service.GetParametersAsync(active.Id);
        Assert.Equal(
            [
                "idCanal",
                "nombreCanal",
                "idTransaccion",
                "idEstado",
                "causal",
                "idTransaccionAxon",
                "descripcionCausal"
            ],
            parameters.Select(x => x.ParameterPath).ToArray());
    }

    [Fact]
    public async Task DirectActivate_ShouldBeRejectedAfterAtomicActivationBecomesMandatory()
    {
        await using var fixture = await ContextFixture.CreateAsync();
        await new IntegrationCatalogBootstrapper(fixture.Context).EnsureAsync();

        var method = await fixture.Context.IntegrationMethods
            .SingleAsync(x => x.Code == "WSCFAACH.Proc_Transacciones");
        var baseline = await fixture.Context.IntegrationContractVersions
            .SingleAsync(x => x.MethodId == method.Id && x.Status == IntegrationContractVersionStatusEnum.Active);
        var baselineId = baseline.Id;
        var baselineRevision = baseline.Revision;

        var candidate = new IntegrationContractVersion
        {
            MethodId = method.Id,
            VersionLabel = "candidate-v2",
            Status = IntegrationContractVersionStatusEnum.Validated,
            OperationName = baseline.OperationName,
            TargetNamespace = baseline.TargetNamespace,
            SoapAction = baseline.SoapAction,
            SourceType = IntegrationContractSourceTypeEnum.ManualApproved,
            Revision = 1
        };
        fixture.Context.IntegrationContractVersions.Add(candidate);
        await fixture.Context.SaveChangesAsync();

        var service = new IntegrationContractVersionService(fixture.Context);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ActivateAsync(candidate.Id, 1, "p1.3-test"));
        Assert.Contains("activación atómica", error.Message, StringComparison.OrdinalIgnoreCase);

        var persistedBaseline = await fixture.Context.IntegrationContractVersions
            .AsNoTracking()
            .SingleAsync(x => x.Id == baselineId);
        Assert.Equal(IntegrationContractVersionStatusEnum.Active, persistedBaseline.Status);
        Assert.Equal(baselineRevision, persistedBaseline.Revision);

        Assert.Equal(1, await fixture.Context.IntegrationContractVersions
            .CountAsync(x => x.MethodId == method.Id && x.Status == IntegrationContractVersionStatusEnum.Active));
        Assert.Equal(2, await fixture.Context.IntegrationContractVersions.CountAsync(x => x.MethodId == method.Id));
    }

    [Fact]
    public async Task SaveChanges_ShouldRejectSecondActiveContractForSameMethod()
    {
        await using var fixture = await ContextFixture.CreateAsync();
        await new IntegrationCatalogBootstrapper(fixture.Context).EnsureAsync();

        var method = await fixture.Context.IntegrationMethods
            .SingleAsync(x => x.Code == "WSCFAACH.Proc_Contrapartidas");
        fixture.Context.IntegrationContractVersions.Add(new IntegrationContractVersion
        {
            MethodId = method.Id,
            VersionLabel = "illegal-second-active",
            Status = IntegrationContractVersionStatusEnum.Active,
            OperationName = "Proc_Contrapartidas",
            TargetNamespace = "http://tempuri.org/",
            SoapAction = "http://tempuri.org/IWSCFAACH/Proc_Contrapartidas",
            SourceType = IntegrationContractSourceTypeEnum.ManualApproved,
            Revision = 1,
            ActivatedAtUtc = DateTime.UtcNow,
            ActivatedBy = "p1.3-test"
        });

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Context.SaveChangesAsync());
        Assert.Contains("Solo puede existir una versión contractual Active", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveChanges_ShouldRejectContractAndParameterMutationOnceActive()
    {
        await using var fixture = await ContextFixture.CreateAsync();
        await new IntegrationCatalogBootstrapper(fixture.Context).EnsureAsync();

        var method = await fixture.Context.IntegrationMethods
            .SingleAsync(x => x.Code == "WSAXON.RegistrarRespuestaTransaccion");
        var active = await fixture.Context.IntegrationContractVersions
            .SingleAsync(x => x.MethodId == method.Id && x.Status == IntegrationContractVersionStatusEnum.Active);

        active.SoapAction = active.SoapAction + "/mutated";
        var contractError = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Context.SaveChangesAsync());
        Assert.Contains("Active es inmutable", contractError.Message, StringComparison.Ordinal);

        fixture.Context.Entry(active).State = EntityState.Unchanged;
        var parameter = await fixture.Context.IntegrationMethodParameters
            .FirstAsync(x => x.ContractVersionId == active.Id);
        parameter.DisplayName = parameter.DisplayName + " mutado";

        var parameterError = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Context.SaveChangesAsync());
        Assert.Contains("parámetros de una versión contractual Active", parameterError.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task SaveChanges_ShouldRejectMutationAfterContractIsRetired()
    {
        await using var fixture = await ContextFixture.CreateAsync();
        await new IntegrationCatalogBootstrapper(fixture.Context).EnsureAsync();

        var method = await fixture.Context.IntegrationMethods
            .SingleAsync(x => x.Code == "WSCFAACH.Proc_Transacciones");
        var retired = new IntegrationContractVersion
        {
            MethodId = method.Id,
            VersionLabel = "retired-history-test",
            Status = IntegrationContractVersionStatusEnum.Retired,
            OperationName = "Proc_Transacciones",
            TargetNamespace = "http://tempuri.org/",
            SoapAction = "http://tempuri.org/IWSCFAACH/Proc_Transacciones",
            SourceType = IntegrationContractSourceTypeEnum.ManualApproved,
            Revision = 1
        };
        fixture.Context.IntegrationContractVersions.Add(retired);
        await fixture.Context.SaveChangesAsync();
        retired.Notes = "mutation-not-allowed";

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Context.SaveChangesAsync());
        Assert.Contains("Retired es inmutable", error.Message, StringComparison.Ordinal);
    }

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
}
