using System.Xml.Linq;
using Cfa.ACHInterbank.Application.Integrations.Registries;
using Cfa.ACHInterbank.Application.Integrations.Services;
using Cfa.ACHInterbank.Application.Integrations.Transformations;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.Integrations.Providers;
using Microsoft.Extensions.Options;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Integrations.Dtos;
using Cfa.ACHInterbank.Application.Integrations.Exceptions;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Integrations.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class IntegrationMappingEndToEndTests
{
    [Fact]
    public async Task Catalog_Methods_Available()
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        var methods = await fixture.Catalog.GetMethodsAsync();
        Assert.Contains(methods, x =>
            x.Code == "WSCFAACH.Proc_Contrapartidas"
            && x.IntegrationKey == "WSCFAACH"
            && x.OperationKey == "Proc_Contrapartidas"
            && x.MappingPurpose == "MonetaryDebitRequest"
            && x.MovesMoney);
        Assert.Contains(methods, x =>
            x.Code == "WSCFAACH.Proc_Transacciones"
            && x.SoapClientCode == "WscfaachSoapClient"
            && x.MappingPurpose == "MonetaryCreditRequest"
            && x.MovesMoney);
        Assert.Contains(methods, x =>
            x.Code == "WSAXON.RegistrarRespuestaTransaccion"
            && x.SoapClientCode == "WsAxonRespuestaTransaccionesSoapClient"
            && x.MappingPurpose == "DifferentialResponseNotification"
            && x.MovesMoney == false
            && x.IsActive);
    }

    [Fact]
    public async Task Catalog_Parameters_Available_ByMethod()
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        var parameters = await fixture.Catalog.GetMethodParametersAsync(fixture.MethodId);
        Assert.Contains(parameters, x => x.ParameterPath == "OFIDLOT");
        Assert.Contains(parameters, x => x.ParameterPath == "OFIDTX");
    }

    [Fact]
    public async Task Catalog_SourceFields_Available_ByMethod()
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        var source = await fixture.Catalog.GetSourceCatalogAsync(fixture.MethodId);
        Assert.Contains(source, x => x.FieldPath == "transaction.reference");
        Assert.Contains(source, x => x.FieldPath == "execution.dateYyyyMMdd");
        Assert.Contains(source, x => x.FieldPath == "cycle.id");
    }

    [Fact]
    public async Task MappingWorkspace_GuidedParameterEdit_UsesBackendMetadataAndRevision()
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        var draft = await fixture.MappingSetService.CreateDraftAsync(
            new CreateIntegrationMappingSetRequest(fixture.MethodId, "Guided edit", "", "tester"));
        var workspace = await fixture.MappingSetService.GetWorkspaceAsync(draft.Id);

        Assert.NotNull(workspace);
        Assert.NotEmpty(workspace!.SourceGroups);
        Assert.NotEmpty(workspace.SourceCompatibility);

        var compatibility = workspace.SourceCompatibility.First();
        var source = workspace.SourceGroups
            .SelectMany(group => group.Fields)
            .Single(item => item.Id == compatibility.SourceCatalogFieldId);
        var selectedTransformation = compatibility.AllowsNoTransformation
            ? null
            : compatibility.Transformations.First();
        var formatMask = selectedTransformation?.RequiresFormatMask == true ? "yyyyMMdd" : null;

        var updated = await fixture.MappingSetService.UpdateParameterMappingAsync(
            draft.Id,
            compatibility.ParameterId,
            new UpdateIntegrationParameterMappingRequest(
                draft.Revision,
                source.Id,
                selectedTransformation?.Code,
                formatMask,
                "tester"));

        Assert.Equal(draft.Revision + 1, updated.Revision);
        var rule = Assert.Single(updated.Rules, item => item.ParameterId == compatibility.ParameterId);
        Assert.Equal(source.Id, rule.SourceCatalogFieldId);
        Assert.Equal(source.FieldPath, rule.SourceFieldPath);
        Assert.Equal(selectedTransformation?.Code, rule.TransformationCode);
        Assert.Null(rule.ConditionExpression);

        await Assert.ThrowsAsync<IntegrationMappingRevisionConflictException>(() =>
            fixture.MappingSetService.UpdateParameterMappingAsync(
                draft.Id,
                compatibility.ParameterId,
                new UpdateIntegrationParameterMappingRequest(
                    draft.Revision,
                    source.Id,
                    selectedTransformation?.Code,
                    formatMask,
                    "tester")));
    }

    [Fact]
    public async Task MappingSet_CanCreateAndEditDraft()
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        var created = await fixture.MappingSetService.CreateDraftAsync(new CreateIntegrationMappingSetRequest(fixture.MethodId, "Draft A", "notes", "tester"));
        var updated = await fixture.MappingSetService.UpdateDraftAsync(created.Id, new UpdateIntegrationMappingSetRequest("Draft B", "notes2", true, "tester"));

        Assert.Equal("Draft B", updated.Name);
    }

    [Fact]
    public async Task MappingSet_BulkUpsertRules_Works()
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        var created = await fixture.CreateDraftWithRulesAsync("BulkUpsert");
        Assert.NotEmpty(created.Rules);
    }

    [Fact]
    public async Task MappingSet_PreservesContractSignificantTrailingSpaces()
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        var draft = await fixture.CreateDraftWithRulesAsync("FixedWidthSoapValue");
        var original = draft.Rules.First();

        var updated = await fixture.MappingSetService.UpsertRulesAsync(
            draft.Id,
            new UpsertIntegrationMappingRulesRequest("tester", [
                new UpsertIntegrationMappingRuleRequest(
                    original.Id,
                    fixture.MethodId,
                    original.ParameterId,
                    IntegrationSourceKindEnum.Constant,
                    null,
                    string.Empty,
                    "TRANSFER  ",
                    "TRANSFER  ",
                    null,
                    null,
                    1,
                    original.RequiredOverride,
                    true,
                    null)
            ]));

        var rule = Assert.Single(updated.Rules, item => item.Id == original.Id);
        Assert.Equal("TRANSFER  ", rule.FixedValue);
        Assert.Equal("TRANSFER  ", rule.DefaultValue);
    }

    [Fact]
    public async Task MappingSet_Validation_DetectsErrors_AndCoverage()
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        var draft = await fixture.MappingSetService.CreateDraftAsync(new CreateIntegrationMappingSetRequest(fixture.MethodId, "Invalid", "", "tester"));

        var required = fixture.Parameters.First(p => p.Required);
        await fixture.MappingSetService.UpsertRulesAsync(draft.Id, new UpsertIntegrationMappingRulesRequest("tester", [
            new UpsertIntegrationMappingRuleRequest(null, fixture.MethodId, required.Id, IntegrationSourceKindEnum.Transaction, null, "", null, null, "NotAllowed", null, 1, null, false, null)
        ]));

        var validation = await fixture.MappingSetService.ValidateAsync(draft.Id, new ValidateIntegrationMappingSetRequest(true));

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Issues, i => i.Code is "TRANSFORMATION_INVALID" or "SOURCE_NOT_DEFINED");
        Assert.True(validation.Coverage.IncompleteParameters >= 1 || validation.Coverage.InvalidParameters >= 1);
    }

    [Fact]
    public async Task MappingSet_Validation_ShouldRejectSourceCatalogFieldFromAnotherMethod()
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        var draft = await fixture.MappingSetService.CreateDraftAsync(
            new CreateIntegrationMappingSetRequest(fixture.MethodId, "Cross-method source", "", "p8.3-test"));
        var required = fixture.Parameters.First(parameter => parameter.Required);
        var foreignSource = await fixture.Context.IntegrationSourceCatalogFields
            .AsNoTracking()
            .FirstAsync(source => source.MethodId != fixture.MethodId && source.IsActive);

        await fixture.MappingSetService.UpsertRulesAsync(
            draft.Id,
            new UpsertIntegrationMappingRulesRequest("p8.3-test",
            [
                new UpsertIntegrationMappingRuleRequest(
                    null,
                    fixture.MethodId,
                    required.Id,
                    foreignSource.SourceKind,
                    foreignSource.Id,
                    foreignSource.FieldPath,
                    null,
                    null,
                    null,
                    null,
                    1,
                    null,
                    true,
                    null)
            ]));

        var validation = await fixture.MappingSetService.ValidateAsync(
            draft.Id,
            new ValidateIntegrationMappingSetRequest(true));

        Assert.False(validation.IsValid);
        Assert.Contains(validation.Issues, issue => issue.Code == "SOURCE_FIELD_NOT_FOUND");
    }

    [Fact]
    public async Task MappingSet_Preview_ReturnsItemsAndPayload()
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        var draft = await fixture.CreateDraftWithRulesAsync("Preview");

        var preview = await fixture.MappingSetService.PreviewAsync(draft.Id, new PreviewIntegrationMappingSetRequest(UseControlledSample: true, MaxItems: 20));
        Assert.NotEmpty(preview.Items);
        Assert.Contains("{", preview.PayloadPreviewJson);
        Assert.NotNull(preview.Validation);
        Assert.All(preview.Items, item => Assert.False(string.IsNullOrWhiteSpace(item.FunctionalName)));
    }

    [Fact]
    public async Task MappingSet_Preview_MasksSensitiveValues_AndIncludesFunctionalValidation()
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        const string fullAccountNumber = "123456789012";
        fixture.Transaction.SourceAccountNumber = fullAccountNumber;
        await fixture.Context.SaveChangesAsync();

        var draft = await fixture.MappingSetService.CreateDraftAsync(
            new CreateIntegrationMappingSetRequest(fixture.MethodId, "Sanitized preview", "", "tester"));
        var workspace = await fixture.MappingSetService.GetWorkspaceAsync(draft.Id);
        Assert.NotNull(workspace);

        var accountSource = workspace!.SourceGroups
            .SelectMany(group => group.Fields)
            .Single(field => field.FieldPath == "transaction.sourceAccountNumber");
        var compatibility = workspace.SourceCompatibility
            .First(item => item.SourceCatalogFieldId == accountSource.Id && item.AllowsNoTransformation);

        var updated = await fixture.MappingSetService.UpdateParameterMappingAsync(
            draft.Id,
            compatibility.ParameterId,
            new UpdateIntegrationParameterMappingRequest(
                draft.Revision,
                accountSource.Id,
                null,
                null,
                "tester"));

        var preview = await fixture.MappingSetService.PreviewAsync(
            updated.Id,
            new PreviewIntegrationMappingSetRequest(
                SampleTransactionId: fixture.Transaction.Id,
                MaxItems: 100,
                UseControlledSample: false));

        var item = Assert.Single(preview.Items, current => current.ParameterId == compatibility.ParameterId);
        Assert.True(item.IsMasked);
        Assert.NotEqual(fullAccountNumber, item.PreviewValue);
        Assert.NotNull(item.PreviewValue);
        Assert.EndsWith("9012", item.PreviewValue!);
        Assert.Equal(accountSource.DisplayName, item.SourceDisplayName);
        Assert.False(string.IsNullOrWhiteSpace(item.FunctionalName));
        Assert.NotNull(item.ValidationIssues);
        Assert.DoesNotContain(fullAccountNumber, preview.PayloadPreviewJson);
        Assert.DoesNotContain(fullAccountNumber, preview.RawPreviewJson);
        Assert.Equal(preview.MappingSetId, preview.Validation.MappingSetId);
    }

    [Fact]
    public async Task MappingSet_Publish_RequiresValidConfiguration()
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        var draft = await fixture.CreateDraftWithRulesAsync("Publish");

        var published = await fixture.MappingSetService.PublishAsync(draft.Id, new PublishIntegrationMappingSetRequest(draft.Revision, "tester", "ok"));
        Assert.Equal(IntegrationMappingSetStatusEnum.Published, published.Status);
    }

    [Fact]
    public async Task MappingSet_Publish_RejectsStaleRevision_WithoutPublishing()
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        var draft = await fixture.CreateDraftWithRulesAsync("Publish conflict");

        var exception = await Assert.ThrowsAsync<IntegrationMappingRevisionConflictException>(() =>
            fixture.MappingSetService.PublishAsync(
                draft.Id,
                new PublishIntegrationMappingSetRequest(draft.Revision - 1, "tester")));

        Assert.Equal(draft.Revision - 1, exception.ExpectedRevision);
        Assert.Equal(draft.Revision, exception.CurrentRevision);
        var current = await fixture.MappingSetService.GetByIdAsync(draft.Id);
        Assert.NotNull(current);
        Assert.Equal(IntegrationMappingSetStatusEnum.Draft, current!.Status);
    }

    [Fact]
    public async Task MappingSet_Clone_Works()
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        var draft = await fixture.CreateDraftWithRulesAsync("CloneSource");
        var clone = await fixture.MappingSetService.CloneAsync(draft.Id, new CloneIntegrationMappingSetRequest("CloneResult", "tester"));
        Assert.Equal("CloneResult", clone.Name);
        Assert.NotEmpty(clone.Rules);
    }

    [Fact]
    public async Task Resolver_RejectsUnaccreditedPublishedMappingBeforeSoap()
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        await fixture.CreateAndPublishResolverMappingAsync();
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Resolver.TryResolveAsync(fixture.Cycle, [fixture.Transaction], DateTime.UtcNow));
        Assert.Contains("PROC_CONTRAPARTIDAS_SOURCE_NOT_HOMOLOGATED", error.Message);
    }

    [Fact]
    public async Task Resolver_BlocksBootstrapMapping_WithoutBusinessCompanyId()
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Resolver.TryResolveAsync(fixture.Cycle, [fixture.Transaction], DateTime.UtcNow));
        Assert.Contains("INTEGRATION_MAPPING_REQUIRED", error.Message);
        Assert.Contains("OFEMP", error.Message);
    }

    [Theory]
    [InlineData("OFEMP", "clearinghouse.code", IntegrationSourceKindEnum.ClearingHouse)]
    [InlineData("OFIDARCH", "batch.id", IntegrationSourceKindEnum.Batch)]
    public async Task Resolver_RejectsUnrelatedSources_EvenWhenPublished(string field, string badPath, IntegrationSourceKindEnum kind)
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        var published = await fixture.CreateAndPublishResolverMappingAsync();
        var parameter = fixture.Parameters.Single(x => x.ParameterPath == field);
        var rule = await fixture.Context.IntegrationMappingRules.SingleAsync(x =>
            x.MappingSetId == published.Id && x.ParameterId == parameter.Id);
        rule.SourceKind = kind;
        rule.SourceFieldPath = badPath;
        rule.FixedValue = null;
        rule.DefaultValue = null;
        await fixture.Context.SaveChangesAsync();
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Resolver.TryResolveAsync(fixture.Cycle, [fixture.Transaction], DateTime.UtcNow));
        Assert.Contains("PROC_CONTRAPARTIDAS_SOURCE_NOT_HOMOLOGATED", error.Message);
        var directGuard = ProcContrapartidasFunctionalMappingResolver.AssessProtectedRule(field, rule, badPath);
        Assert.Contains(field, directGuard);
    }

    [Fact]
    public async Task Resolver_RejectsReservedIpConstant_EvenWhenPublished()
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        var published = await fixture.CreateAndPublishResolverMappingAsync();
        var parameter = fixture.Parameters.Single(x => x.ParameterPath == "OFDIRECCIONIP");
        var rule = await fixture.Context.IntegrationMappingRules.SingleAsync(x =>
            x.MappingSetId == published.Id && x.ParameterId == parameter.Id);
        rule.SourceKind = IntegrationSourceKindEnum.Constant;
        rule.SourceFieldPath = "constant.value";
        rule.FixedValue = "0.0.0.0";
        rule.DefaultValue = null;
        await fixture.Context.SaveChangesAsync();
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Resolver.TryResolveAsync(fixture.Cycle, [fixture.Transaction], DateTime.UtcNow));
        Assert.Contains("PROC_CONTRAPARTIDAS_SOURCE_NOT_HOMOLOGATED", error.Message);
        var directGuard = ProcContrapartidasFunctionalMappingResolver.AssessProtectedRule("OFDIRECCIONIP", rule, "constant.value");
        Assert.Contains("OFDIRECCIONIP", directGuard);
    }

    [Theory]
    [InlineData(TransactionTypeEnum.Credit, false, 75.5)]
    [InlineData(TransactionTypeEnum.Debit, true, 75.5)]
    [InlineData(TransactionTypeEnum.Debit, false, 0.0)]
    public async Task Resolver_RejectsNonMonetaryOrNonDebitTransactions(
        TransactionTypeEnum transactionType, bool isPrenotification, double amount)
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        fixture.Transaction.Type = transactionType;
        fixture.Transaction.IsPrenotification = isPrenotification;
        fixture.Transaction.Amount = (decimal)amount;

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Resolver.TryResolveAsync(fixture.Cycle, [fixture.Transaction], DateTime.UtcNow));

        Assert.Contains("PROC_CONTRAPARTIDAS_DEBIT_ONLY", error.Message);
    }

    [Theory]
    [InlineData("OFDD", "C")]
    [InlineData("OFMONDEB", "80.00")]
    [InlineData("OFMONCRE", "10.00")]
    public async Task Resolver_RejectsPublishedMappingWithInconsistentDebitSemantics(string field, string incorrectValue)
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        var published = await fixture.CreateAndPublishResolverMappingAsync();
        var parameter = fixture.Parameters.Single(p => p.ParameterPath == field);
        var rule = await fixture.Context.IntegrationMappingRules.SingleAsync(r =>
            r.MappingSetId == published.Id && r.ParameterId == parameter.Id);
        // Simulate a corrupted/prematurely published rule without relying on external UAT.
        rule.SourceKind = IntegrationSourceKindEnum.Constant;
        rule.SourceFieldPath = string.Empty;
        rule.FixedValue = incorrectValue;
        rule.DefaultValue = null;
        await fixture.Context.SaveChangesAsync();

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Resolver.TryResolveAsync(fixture.Cycle, [fixture.Transaction], DateTime.UtcNow));

        Assert.Contains("PROC_CONTRAPARTIDAS_SOURCE_NOT_HOMOLOGATED", error.Message);
    }

    [Fact]
    public async Task ProcContrapartidas_ContractDrivenFeatureFlag_CannotBypassUnaccreditedMappings()
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        await fixture.CreateAndPublishResolverMappingAsync();
        var executionDate = new DateTime(2026, 10, 12, 15, 0, 0, DateTimeKind.Utc);
        var legacyMapper = new ProcContrapartidasRequestMapper(fixture.Resolver);
        var contractDrivenMapper = CreateContractDrivenProcContrapartidasMapper(fixture);
        var legacyError = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            legacyMapper.ResolveAsync(fixture.Cycle, [fixture.Transaction], executionDate));
        var genericError = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            contractDrivenMapper.ResolveAsync(fixture.Cycle, [fixture.Transaction], executionDate));
        Assert.Contains("PROC_CONTRAPARTIDAS_SOURCE_NOT_HOMOLOGATED", legacyError.Message);
        Assert.Contains("PROC_CONTRAPARTIDAS_SOURCE_NOT_HOMOLOGATED", genericError.Message);
    }

    [Fact]
    public async Task ProcContrapartidas_ContractDrivenFeatureFlag_ShouldPreserveLegacyRequiredSourceGuard()
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        await fixture.CreateAndPublishResolverMappingAsync();
        fixture.Transaction.Reference = string.Empty;
        await fixture.Context.SaveChangesAsync();
        var mapper = CreateContractDrivenProcContrapartidasMapper(fixture);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            mapper.ResolveAsync(
                fixture.Cycle,
                [fixture.Transaction],
                new DateTime(2026, 10, 12, 15, 0, 0, DateTimeKind.Utc)));

        Assert.Contains("PROC_CONTRAPARTIDAS_SOURCE_NOT_HOMOLOGATED", error.Message);
        Assert.Throws<InvalidOperationException>(() =>
            ProcContrapartidasFunctionalMappingResolver.EnsureProtectedResolvedValues(
                fixture.Transaction, fixture.Transaction.SourceAccountNumber, "SPOOF-REFERENCE"));
    }

    [Fact]
    public async Task Resolver_ShouldFailControlled_WhenRequiredReferenceSourceIsMissing()
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        await fixture.CreateAndPublishResolverMappingAsync();
        fixture.Transaction.Reference = string.Empty;

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            fixture.Resolver.TryResolveAsync(fixture.Cycle, [fixture.Transaction], DateTime.UtcNow));

        Assert.Contains("PROC_CONTRAPARTIDAS_SOURCE_NOT_HOMOLOGATED", error.Message);
        Assert.Throws<InvalidOperationException>(() =>
            ProcContrapartidasFunctionalMappingResolver.EnsureProtectedResolvedValues(
                fixture.Transaction, fixture.Transaction.SourceAccountNumber, "SPOOF-REFERENCE"));
    }

    [Fact]
    public async Task Resolver_ShouldUseInvariantMonetaryConversion()
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        await fixture.CreateAndPublishResolverMappingAsync();
        var originalCulture = System.Globalization.CultureInfo.CurrentCulture;
        var originalUiCulture = System.Globalization.CultureInfo.CurrentUICulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("de-DE");

            var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                fixture.Resolver.TryResolveAsync(fixture.Cycle, [fixture.Transaction], DateTime.UtcNow));
            Assert.Contains("PROC_CONTRAPARTIDAS_SOURCE_NOT_HOMOLOGATED", error.Message);
        }
        finally
        {
            System.Globalization.CultureInfo.CurrentCulture = originalCulture;
            System.Globalization.CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Fact]
    public async Task PublishedMappingMetadata_HasIdVersionAndSnapshotHash()
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        var published = await fixture.CreateAndPublishResolverMappingAsync();

        var history = await fixture.MappingSetService.GetHistoryAsync(published.Id);
        Assert.Contains(history, x => x.SnapshotHash.Length > 10);
        Assert.True(published.Version > 0);
        Assert.NotEqual(Guid.Empty, published.Id);
    }

    [Fact]
    public async Task MappingSet_Compare_ReturnsRuleDiffsAndMetadata()
    {
        await using var fixture = await IntegrationFixture.CreateAsync();
        var left = await fixture.CreateDraftWithRulesAsync("Compare Left");
        var right = await fixture.CreateDraftWithRulesAsync("Compare Right");

        var firstRule = right.Rules.First();
        await fixture.MappingSetService.UpsertRulesAsync(
            right.Id,
            new UpsertIntegrationMappingRulesRequest("tester", [
                new UpsertIntegrationMappingRuleRequest(firstRule.Id, fixture.MethodId, firstRule.ParameterId, firstRule.SourceKind, firstRule.SourceCatalogFieldId, firstRule.SourceFieldPath, firstRule.FixedValue, "NEW-DEFAULT", firstRule.TransformationCode, firstRule.FormatMask, firstRule.Priority + 1, firstRule.RequiredOverride, firstRule.Enabled, firstRule.ConditionExpression)
            ]));

        var compare = await fixture.MappingSetService.CompareAsync(new CompareIntegrationMappingSetsRequest(left.Id, right.Id));

        Assert.Equal(left.Id, compare.Left.MappingSetId);
        Assert.Equal(right.Id, compare.Right.MappingSetId);
        Assert.Contains(compare.Rules, x => x.ChangeType is "Modified" or "Equal");
    }

    private static ProcContrapartidasRequestMapper CreateContractDrivenProcContrapartidasMapper(IntegrationFixture fixture)
    {
        var sourceRegistry = new IntegrationSourceProviderRegistry(
            new CurrentIntegrationSourceProviderFactory(fixture.Context).Create());
        var transformationRegistry = new IntegrationTransformationRegistry(
            StandardIntegrationTransformationCatalog.Create());
        var requestResolver = new IntegrationRequestResolver(
            new IntegrationExecutionContractResolver(fixture.Context),
            new IntegrationParameterValueResolver(sourceRegistry, transformationRegistry));

        return new ProcContrapartidasRequestMapper(
            fixture.Resolver,
            Options.Create(new ProcContrapartidasDispatchOptions
            {
                Mode = "DryRun",
                UseContractDrivenRequest = true
            }),
            requestResolver,
            new ContractDrivenSoapBodyWriter(),
            new ProcContrapartidasRequestParityPolicy());
    }

    private static void AssertProcContrapartidasSoapParity(string legacyXml, string contractDrivenXml)
    {
        var expected = ExtractSoapOperation(legacyXml);
        var actual = ExtractSoapOperation(contractDrivenXml);

        Assert.Equal(expected.Name, actual.Name);
        var expectedElements = expected.Elements().ToArray();
        var actualElements = actual.Elements().ToArray();
        Assert.Equal(22, actualElements.Length);
        Assert.Equal(expectedElements.Select(element => element.Name).ToArray(), actualElements.Select(element => element.Name).ToArray());
        Assert.Equal(expectedElements.Select(element => element.Value).ToArray(), actualElements.Select(element => element.Value).ToArray());
    }

    private static XElement ExtractSoapOperation(string xml)
    {
        var root = Assert.IsType<XElement>(XDocument.Parse(xml, LoadOptions.PreserveWhitespace).Root);
        if (!string.Equals(root.Name.LocalName, "Envelope", StringComparison.Ordinal))
        {
            return root;
        }

        var body = Assert.Single(root.Elements().Where(element => element.Name.LocalName == "Body"));
        return Assert.Single(body.Elements());
    }

    private sealed class IntegrationFixture : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;
        public AchDbContext Context { get; }
        public IntegrationCatalogService Catalog { get; }
        public IntegrationMappingSetService MappingSetService { get; }
        public ProcContrapartidasFunctionalMappingResolver Resolver { get; }
        public int MethodId { get; private set; }
        public List<IntegrationMethodParameter> Parameters { get; private set; } = [];
        public AchCycle Cycle { get; private set; } = null!;
        public AchTransaction Transaction { get; private set; } = null!;

        private IntegrationFixture(SqliteConnection connection, AchDbContext context)
        {
            _connection = connection;
            Context = context;
            Catalog = new IntegrationCatalogService(context);
            var validation = new IntegrationMappingValidationService(context);
            var preview = new IntegrationMappingPreviewService(context);
            MappingSetService = new IntegrationMappingSetService(context, validation, preview);
            Resolver = new ProcContrapartidasFunctionalMappingResolver(context);
        }

        public static async Task<IntegrationFixture> CreateAsync()
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<AchDbContext>()
                .UseSqlite(connection)
                .EnableSensitiveDataLogging()
                .Options;

            var context = new AchDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var fixture = new IntegrationFixture(connection, context);
            await fixture.SeedAsync();
            return fixture;
        }

        public async Task SeedAsync()
        {
            await Catalog.GetMethodsAsync();
            var methods = await Catalog.GetMethodsAsync();
            MethodId = methods.First(x => x.Code == "WSCFAACH.Proc_Contrapartidas").Id;
            Parameters = await Context.IntegrationMethodParameters.Where(x => x.MethodId == MethodId).ToListAsync();

            Context.ClearingHouseConfigs.Add(new ClearingHouseConfig
            {
                Id = 1,
                HolidayStrategy = "Colombian"
            });

            Context.ClearingHouses.Add(new ClearingHouse
            {
                Id = 10,
                Code = "ACH",
                Name = "ACH",
                OriginCode = "ORG",
                ClearingHouseId = 1
            });

            var source = new FinancialInstitution
            {
                Id = 1,
                Name = "Origen",
                RoutingNumber = "12345",
                TransitCode = "678",
                IsDefaultSource = true,
                Status = FinancialInstitutionStatus.Active
            };
            source.CalculateCheckDigit();

            var destination = new FinancialInstitution
            {
                Id = 2,
                Name = "Destino",
                RoutingNumber = "76543",
                TransitCode = "210",
                Status = FinancialInstitutionStatus.Active
            };
            destination.CalculateCheckDigit();
            Context.FinancialInstitutions.AddRange(source, destination);
            await Context.SaveChangesAsync();

            var clearingHouse = await Context.ClearingHouses.FirstAsync(x => x.Id == 10);

            Cycle = new AchCycle
            {
                Id = "CYCLE-T-001",
                CycleName = "Cycle T",
                ProcessingDate = DateTime.UtcNow.Date,
                StartTime = TimeSpan.FromHours(8),
                EndTime = TimeSpan.FromHours(17),
                CutoffTime = TimeSpan.FromHours(16),
                ClearingHouseId = 10,
                ClearingHouse = clearingHouse
            };

            var batch = new AchBatch { Id = 999, AchCycleId = Cycle.Id, EffectiveEntryDate = Cycle.ProcessingDate, BatchSequenceNumber = 1, CompanyEntryDescriptionId = 1 };
            Transaction = new AchTransaction
            {
                Id = 1001,
                TransactionExternalId = "TX-1001",
                AchCycleId = Cycle.Id,
                AchCycle = Cycle,
                AchBatch = batch,
                AchBatchId = batch.Id,
                Amount = 75.5m,
                Reference = "TX-REF-001",
                Type = TransactionTypeEnum.Debit,
                TransactionCode = "27",
                TraceNumber = "TRACE001",
                CompanyIdentification = "900",
                OriginatingDFI = "001",
                ReceivingDFI = "002",
                EffectiveEntryDate = DateTime.UtcNow.Date,
                SourceInstitutionId = 1,
                DestinationInstitutionId = 2,
                SourceAccountNumber = "123",
                DestinationAccountNumber = "456",
                Addendas = [new AchTransactionAddenda { AddendaType = "05", Information = "INFO", SequenceNumber = 1, BusinessType = AchAddendaBusinessType.Credit }]
            };

            Context.AchCycles.Add(Cycle);
            Context.AchBatches.Add(batch);
            Context.AchTransactions.Add(Transaction);
            await Context.SaveChangesAsync();
        }

        public async Task<IntegrationMappingSetDto> CreateDraftWithRulesAsync(string name)
        {
            var draft = await MappingSetService.CreateDraftAsync(new CreateIntegrationMappingSetRequest(MethodId, name, "", "tester"));
            var rules = Parameters
                .Where(x => x.Required)
                .Select(p => new UpsertIntegrationMappingRuleRequest(null, MethodId, p.Id, IntegrationSourceKindEnum.Constant, null, null, DefaultFor(p), null, null, null, 1, null, true, null))
                .ToArray();

            return await MappingSetService.UpsertRulesAsync(draft.Id, new UpsertIntegrationMappingRulesRequest("tester", rules));
        }

        public async Task<IntegrationMappingSetDto> CreateAndPublishResolverMappingAsync()
        {
            var draft = await MappingSetService.CreateDraftAsync(new CreateIntegrationMappingSetRequest(MethodId, "Resolver", "", "tester"));

            var rules = new List<UpsertIntegrationMappingRuleRequest>();
            foreach (var parameter in Parameters.Where(x => x.Required))
            {
                var (kind, path, fixedVal) = parameter.ParameterPath switch
                {
                    "OFDD" => (IntegrationSourceKindEnum.Transaction, "transaction.debitCreditIndicator", (string?)null),
                    "OFMONDEB" => (IntegrationSourceKindEnum.Transaction, "transaction.amount", (string?)null),
                    "OFMONCRE" => (IntegrationSourceKindEnum.Constant, string.Empty, "0"),
                    "OFIDLOT" => (IntegrationSourceKindEnum.Batch, "batch.id", (string?)null),
                    "OFIDTX" => (IntegrationSourceKindEnum.Transaction, "transaction.transactionExternalId", (string?)null),
                    "CycleId" => (IntegrationSourceKindEnum.Cycle, "cycle.id", (string?)null),
                    "CycleName" => (IntegrationSourceKindEnum.Cycle, "cycle.cycleName", (string?)null),
                    "ClearingHouseId" => (IntegrationSourceKindEnum.ClearingHouse, "clearinghouse.id", (string?)null),
                    "ClearingHouseCode" => (IntegrationSourceKindEnum.ClearingHouse, "clearinghouse.code", (string?)null),
                    "Transactions[].Reference" => (IntegrationSourceKindEnum.Transaction, "transaction.reference", (string?)null),
                    "Transactions[].TransactionId" => (IntegrationSourceKindEnum.Transaction, "transaction.id", (string?)null),
                    "Transactions[].AchBatchId" => (IntegrationSourceKindEnum.Batch, "batch.id", (string?)null),
                    "Transactions[].Amount" => (IntegrationSourceKindEnum.Transaction, "transaction.amount", (string?)null),
                    "Transactions[].Addendas[].AddendaType" => (IntegrationSourceKindEnum.Addenda, "addenda.addendaType", (string?)null),
                    _ => (IntegrationSourceKindEnum.Constant, string.Empty, DefaultFor(parameter))
                };

                rules.Add(new UpsertIntegrationMappingRuleRequest(null, MethodId, parameter.Id, kind, null, path, fixedVal, DefaultFor(parameter), null, null, 1, null, true, null));
            }

            var mapped = await MappingSetService.UpsertRulesAsync(draft.Id, new UpsertIntegrationMappingRulesRequest("tester", rules));
            return await MappingSetService.PublishAsync(mapped.Id, new PublishIntegrationMappingSetRequest(mapped.Revision, "tester"));
        }

        private static string DefaultFor(IntegrationMethodParameter parameter)
            => parameter.DataType.ToLowerInvariant() switch
            {
                "int" or "long" => "1",
                "decimal" or "double" or "float" => "1.00",
                "datetime" => DateTime.UtcNow.ToString("O"),
                "timespan" => "08:00:00",
                _ => "TEST"
            };

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

}
