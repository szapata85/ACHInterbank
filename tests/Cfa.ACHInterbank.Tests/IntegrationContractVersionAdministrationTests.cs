using Cfa.ACHInterbank.Application.Integrations.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Integrations.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class IntegrationContractVersionAdministrationTests
{
    [Fact]
    public async Task CreateDraft_ShouldCreateManualApprovedVersionWithoutChangingActiveContract()
    {
        await using var fixture = await ContextFixture.CreateAsync();
        await new IntegrationCatalogBootstrapper(fixture.Context).EnsureAsync();
        var method = await fixture.Context.IntegrationMethods.SingleAsync(x => x.Code == "WSCFAACH.Proc_Transacciones");
        var activeBefore = await fixture.Context.IntegrationContractVersions
            .AsNoTracking()
            .SingleAsync(x => x.MethodId == method.Id && x.Status == IntegrationContractVersionStatusEnum.Active);

        var service = new IntegrationContractVersionService(fixture.Context);
        var created = await service.CreateDraftAsync(method.Id, new CreateIntegrationContractVersionRequest(
            "manual-v2",
            activeBefore.OperationName,
            activeBefore.TargetNamespace,
            activeBefore.SoapAction,
            "CFA change request",
            "Draft manual",
            "p4.1-test"));

        Assert.Equal(IntegrationContractVersionStatusEnum.Draft, created.Status);
        Assert.Equal(IntegrationContractSourceTypeEnum.ManualApproved, created.SourceType);
        Assert.Equal(1, created.Revision);
        Assert.Equal("p4.1-test", created.ImportedBy);
        Assert.NotNull(created.ImportedAtUtc);

        var activeAfter = await fixture.Context.IntegrationContractVersions
            .AsNoTracking()
            .SingleAsync(x => x.MethodId == method.Id && x.Status == IntegrationContractVersionStatusEnum.Active);
        Assert.Equal(activeBefore.Id, activeAfter.Id);
        Assert.Equal(activeBefore.Revision, activeAfter.Revision);
    }

    [Fact]
    public async Task Clone_ShouldCopyParametersToFullyVersionedDraftAndPreserveSource()
    {
        await using var fixture = await ContextFixture.CreateAsync();
        await new IntegrationCatalogBootstrapper(fixture.Context).EnsureAsync();
        var method = await fixture.Context.IntegrationMethods.SingleAsync(x => x.Code == "WSAXON.RegistrarRespuestaTransaccion");
        var source = await fixture.Context.IntegrationContractVersions
            .AsNoTracking()
            .SingleAsync(x => x.MethodId == method.Id && x.Status == IntegrationContractVersionStatusEnum.Active);
        var service = new IntegrationContractVersionService(fixture.Context);
        var sourceParameters = await service.GetParametersAsync(source.Id);

        var clone = await service.CloneAsync(method.Id, new CloneIntegrationContractVersionRequest(
            source.Id,
            "registrar-v2",
            "Versionado funcional",
            "p4.1-test"));

        Assert.Equal(IntegrationContractVersionStatusEnum.Draft, clone.Contract.Status);
        Assert.Equal(IntegrationContractSourceTypeEnum.ManualApproved, clone.Contract.SourceType);
        Assert.Equal(1, clone.Contract.Revision);
        Assert.Equal(sourceParameters.Count, clone.Parameters.Count);
        Assert.All(clone.Parameters, parameter => Assert.Equal(clone.Contract.Id, parameter.ContractVersionId));
        Assert.Equal(
            sourceParameters.Select(x => x.ParameterPath).OrderBy(x => x, StringComparer.Ordinal),
            clone.Parameters.Select(x => x.ParameterPath).OrderBy(x => x, StringComparer.Ordinal));

        var persistedSource = await fixture.Context.IntegrationContractVersions.AsNoTracking().SingleAsync(x => x.Id == source.Id);
        Assert.Equal(IntegrationContractVersionStatusEnum.Active, persistedSource.Status);
        Assert.Equal(source.Revision, persistedSource.Revision);
    }

    [Fact]
    public async Task UpdatePresentation_ShouldAdvanceContractRevisionWithoutChangingTechnicalMetadata()
    {
        await using var fixture = await ContextFixture.CreateAsync();
        await new IntegrationCatalogBootstrapper(fixture.Context).EnsureAsync();
        var method = await fixture.Context.IntegrationMethods.SingleAsync(x => x.Code == "WSCFAACH.Proc_Contrapartidas");
        var source = await fixture.Context.IntegrationContractVersions
            .AsNoTracking()
            .SingleAsync(x => x.MethodId == method.Id && x.Status == IntegrationContractVersionStatusEnum.Active);
        var service = new IntegrationContractVersionService(fixture.Context);
        var clone = await service.CloneAsync(method.Id, new CloneIntegrationContractVersionRequest(
            source.Id,
            "contrapartidas-v2",
            null,
            "p4.1-test"));
        var parameter = clone.Parameters.First();

        var updated = await service.UpdateParameterPresentationAsync(
            clone.Contract.Id,
            parameter.Id,
            new UpdateIntegrationContractParameterPresentationRequest(
                "Nombre funcional actualizado",
                "Descripción funcional",
                "Ayuda funcional",
                "Contrapartida",
                "ABC",
                1,
                "p4.1-test"));

        Assert.Equal(2, updated.ContractRevision);
        Assert.Equal("Nombre funcional actualizado", updated.Parameter.DisplayName);
        Assert.Equal(parameter.ParameterPath, updated.Parameter.ParameterPath);
        Assert.Equal(parameter.XmlElementName, updated.Parameter.XmlElementName);
        Assert.Equal(parameter.DataType, updated.Parameter.DataType);
        Assert.Equal(parameter.XsdType, updated.Parameter.XsdType);

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => service.UpdateParameterPresentationAsync(
            clone.Contract.Id,
            parameter.Id,
            new UpdateIntegrationContractParameterPresentationRequest(
                "Stale",
                null,
                null,
                null,
                null,
                1,
                "p4.1-test")));
    }

    [Fact]
    public async Task UpdatePresentation_ShouldRejectActiveContract()
    {
        await using var fixture = await ContextFixture.CreateAsync();
        await new IntegrationCatalogBootstrapper(fixture.Context).EnsureAsync();
        var method = await fixture.Context.IntegrationMethods.SingleAsync(x => x.Code == "WSAXON.RegistrarRespuestaTransaccion");
        var source = await fixture.Context.IntegrationContractVersions
            .AsNoTracking()
            .SingleAsync(x => x.MethodId == method.Id && x.Status == IntegrationContractVersionStatusEnum.Active);
        var parameter = (await new IntegrationContractVersionService(fixture.Context).GetParametersAsync(source.Id)).First();
        var service = new IntegrationContractVersionService(fixture.Context);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateParameterPresentationAsync(
            source.Id,
            parameter.Id,
            new UpdateIntegrationContractParameterPresentationRequest(
                "No permitido",
                null,
                null,
                null,
                null,
                source.Revision,
                "p4.1-test")));

        Assert.Contains("Active o Retired es inmutable", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Compare_ShouldClassifySupportedScalarAsMappingAndObjectAsDevelopment()
    {
        await using var fixture = await ContextFixture.CreateAsync();
        await new IntegrationCatalogBootstrapper(fixture.Context).EnsureAsync();
        var method = await fixture.Context.IntegrationMethods.SingleAsync(x => x.Code == "WSCFAACH.Proc_Transacciones");
        var source = await fixture.Context.IntegrationContractVersions
            .AsNoTracking()
            .SingleAsync(x => x.MethodId == method.Id && x.Status == IntegrationContractVersionStatusEnum.Active);
        var service = new IntegrationContractVersionService(fixture.Context);
        var clone = await service.CloneAsync(method.Id, new CloneIntegrationContractVersionRequest(
            source.Id,
            "diff-v2",
            null,
            "p4.1-test"));

        fixture.Context.IntegrationMethodParameters.AddRange(
            NewParameter(method.Id, clone.Contract.Id, "NEW_SCALAR", IntegrationParameterCardinalityEnum.Scalar, "Supported", 900),
            NewParameter(method.Id, clone.Contract.Id, "NEW_OBJECT", IntegrationParameterCardinalityEnum.Object, "RequiresDevelopment", 901));
        await fixture.Context.SaveChangesAsync();

        var diff = await service.CompareAsync(source.Id, clone.Contract.Id);

        Assert.Equal(2, diff.Summary.Added);
        Assert.Equal(1, diff.Summary.RequiresMapping);
        Assert.Equal(1, diff.Summary.RequiresDevelopment);
        Assert.Contains(diff.Changes, change => change.TechnicalName == "NEW_SCALAR"
            && change.ChangeType == IntegrationContractChangeTypeEnum.Added
            && change.Impact == IntegrationContractDiffImpactEnum.MappingRequired);
        Assert.Contains(diff.Changes, change => change.TechnicalName == "NEW_OBJECT"
            && change.ChangeType == IntegrationContractChangeTypeEnum.Added
            && change.Impact == IntegrationContractDiffImpactEnum.RequiresDevelopment);
    }

    private static IntegrationMethodParameter NewParameter(
        int methodId,
        Guid contractVersionId,
        string path,
        IntegrationParameterCardinalityEnum cardinality,
        string supportStatus,
        int sortOrder)
        => new()
        {
            MethodId = methodId,
            ContractVersionId = contractVersionId,
            ParameterPath = path,
            XmlElementName = path,
            DisplayName = path,
            DescriptionEs = string.Empty,
            UiHelpText = string.Empty,
            Category = "Test",
            ExampleValue = string.Empty,
            DataType = "string",
            XsdType = "xsd:string",
            Direction = IntegrationParameterDirectionEnum.Input,
            Cardinality = cardinality,
            Required = true,
            Nillable = false,
            MinOccurs = 1,
            MaxOccurs = cardinality == IntegrationParameterCardinalityEnum.Collection ? null : 1,
            SupportStatus = supportStatus,
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
}
