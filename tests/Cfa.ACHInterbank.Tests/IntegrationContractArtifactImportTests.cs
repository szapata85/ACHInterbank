using System.Security.Cryptography;
using Cfa.ACHInterbank.Application.Integrations.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Persistence.Integrations.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Tests;

public sealed class IntegrationContractArtifactImportTests
{
    [Theory]
    [InlineData("dtd.wsdl")]
    [InlineData("xxe.wsdl")]
    public async Task Analyze_ShouldRejectDtdAndExternalEntity(string fixtureName)
    {
        var analyzer = new IntegrationContractArtifactAnalyzer();
        var content = await File.ReadAllBytesAsync(Fixture(fixtureName));

        var result = await analyzer.AnalyzeAsync(new AnalyzeIntegrationContractArtifactRequest(fixtureName, content));

        Assert.False(result.Importable);
        Assert.Contains(result.Issues, issue => issue.Code == "UNSAFE_OR_MALFORMED_XML");
        Assert.Empty(result.Parameters);
    }

    [Fact]
    public async Task Analyze_ShouldRejectRemoteReferenceWithoutResolvingIt()
    {
        var analyzer = new IntegrationContractArtifactAnalyzer();
        var content = await File.ReadAllBytesAsync(Fixture("remote-reference.wsdl"));

        var result = await analyzer.AnalyzeAsync(new AnalyzeIntegrationContractArtifactRequest("remote-reference.wsdl", content));

        Assert.False(result.Importable);
        Assert.Contains(result.Issues, issue => issue.Code == "EXTERNAL_REFERENCE_NOT_ALLOWED");
        Assert.Empty(result.Parameters);
    }

    [Fact]
    public async Task Analyze_ShouldRejectOversizedArtifactBeforeXmlParsing()
    {
        var analyzer = new IntegrationContractArtifactAnalyzer();
        var content = await File.ReadAllBytesAsync(Fixture("oversized.wsdl"));

        var result = await analyzer.AnalyzeAsync(new AnalyzeIntegrationContractArtifactRequest("oversized.wsdl", content));

        Assert.False(result.Importable);
        Assert.Equal(IntegrationContractArtifactLimits.MaxArtifactBytes + 1L, result.SizeBytes);
        Assert.Contains(result.Issues, issue => issue.Code == "ARTIFACT_TOO_LARGE");
    }

    [Fact]
    public async Task Analyze_ShouldRejectMalformedWsdl()
    {
        var analyzer = new IntegrationContractArtifactAnalyzer();
        var content = await File.ReadAllBytesAsync(Fixture("malformed.wsdl"));

        var result = await analyzer.AnalyzeAsync(new AnalyzeIntegrationContractArtifactRequest("malformed.wsdl", content));

        Assert.False(result.Importable);
        Assert.Contains(result.Issues, issue => issue.Code == "UNSAFE_OR_MALFORMED_XML");
    }

    [Fact]
    public async Task Analyze_ShouldRejectUnsupportedChoiceFailClosed()
    {
        var analyzer = new IntegrationContractArtifactAnalyzer();
        var content = await File.ReadAllBytesAsync(Fixture("unsupported-choice.wsdl"));

        var result = await analyzer.AnalyzeAsync(new AnalyzeIntegrationContractArtifactRequest("unsupported-choice.wsdl", content));

        Assert.False(result.Importable);
        Assert.Contains(result.Issues, issue => issue.Code == "XSD_CONSTRUCT_UNSUPPORTED");
    }

    [Fact]
    public async Task Analyze_ShouldRejectUnsupportedRestrictionFacetFailClosed()
    {
        var analyzer = new IntegrationContractArtifactAnalyzer();
        var content = System.Text.Encoding.UTF8.GetBytes(
            """
            <?xml version="1.0"?>
            <xs:schema xmlns:xs="http://www.w3.org/2001/XMLSchema" targetNamespace="urn:cfa:p83:facet">
              <xs:element name="FacetOperation">
                <xs:complexType>
                  <xs:sequence>
                    <xs:element name="Code">
                      <xs:simpleType>
                        <xs:restriction base="xs:string">
                          <xs:maxLength value="3" />
                        </xs:restriction>
                      </xs:simpleType>
                    </xs:element>
                  </xs:sequence>
                </xs:complexType>
              </xs:element>
            </xs:schema>
            """);

        var result = await analyzer.AnalyzeAsync(new AnalyzeIntegrationContractArtifactRequest(
            "facet.xsd",
            content,
            "FacetOperation",
            "urn:cfa:p83:facet/FacetOperation"));

        Assert.False(result.Importable);
        Assert.Contains(result.Issues, issue => issue.Code == "XSD_FACET_UNSUPPORTED");
    }

    [Fact]
    public async Task Analyze_ShouldProduceDeterministicSha256AndSupportedParameters()
    {
        var analyzer = new IntegrationContractArtifactAnalyzer();
        var content = await File.ReadAllBytesAsync(Fixture("valid.wsdl"));
        var expectedHash = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();

        var first = await analyzer.AnalyzeAsync(new AnalyzeIntegrationContractArtifactRequest("valid.wsdl", content));
        var second = await analyzer.AnalyzeAsync(new AnalyzeIntegrationContractArtifactRequest("valid.wsdl", content));

        Assert.True(first.Importable);
        Assert.Equal(expectedHash, first.Sha256);
        Assert.Equal(first.Sha256, second.Sha256);
        Assert.Equal("TestOperation", first.OperationName);
        Assert.Equal("urn:cfa:p42:test", first.TargetNamespace);
        Assert.Equal("urn:cfa:p42:test/TestOperation", first.SoapAction);
        Assert.Collection(
            first.Parameters,
            parameter =>
            {
                Assert.Equal("idCanal", parameter.ParameterPath);
                Assert.Equal("integer", parameter.DataType);
                Assert.True(parameter.Required);
            },
            parameter =>
            {
                Assert.Equal("descripcion", parameter.ParameterPath);
                Assert.Equal("string", parameter.DataType);
                Assert.False(parameter.Required);
                Assert.True(parameter.Nillable);
            });
    }


    [Fact]
    public async Task AnalyzeMultipleWsdlOperations_ShouldExposeOnlyBackendCompatibleChoices()
    {
        var analyzer = new IntegrationContractArtifactAnalyzer();
        var content = System.Text.Encoding.UTF8.GetBytes(MultipleOperationWsdl());

        var ambiguous = await analyzer.AnalyzeAsync(new AnalyzeIntegrationContractArtifactRequest("multiple.wsdl", content));
        var selected = await analyzer.AnalyzeAsync(new AnalyzeIntegrationContractArtifactRequest("multiple.wsdl", content, "OperationB"));

        Assert.False(ambiguous.Importable);
        Assert.Contains(ambiguous.Issues, issue => issue.Code == "WSDL_OPERATION_AMBIGUOUS");
        Assert.Equal(new[] { "OperationA", "OperationB" }, ambiguous.AvailableOperations.OrderBy(value => value).ToArray());
        Assert.True(selected.Importable);
        Assert.Equal("OperationB", selected.OperationName);
        Assert.Equal(new[] { "OperationB" }, selected.AvailableOperations);
    }

    [Fact]
    public async Task PreviewDraft_ShouldReturnBackendDiffWithoutPersistingDraft()
    {
        await using var fixture = await ContextFixture.CreateAsync();
        await new IntegrationCatalogBootstrapper(fixture.Context).EnsureAsync();
        var method = await fixture.Context.IntegrationMethods.SingleAsync(x => x.Code == "WSCFAACH.Proc_Transacciones");
        var active = await fixture.Context.IntegrationContractVersions
            .AsNoTracking()
            .SingleAsync(x => x.MethodId == method.Id && x.Status == IntegrationContractVersionStatusEnum.Active);
        var beforeCount = await fixture.Context.IntegrationContractVersions.CountAsync(x => x.MethodId == method.Id);
        var content = await File.ReadAllBytesAsync(Fixture("valid.wsdl"));
        var analyzer = new IntegrationContractArtifactAnalyzer();
        var versions = new IntegrationContractVersionService(fixture.Context);
        var importer = new IntegrationContractArtifactImportService(fixture.Context, analyzer, versions);

        var preview = await importer.PreviewDraftAsync(
            method.Id,
            active.Id,
            new AnalyzeIntegrationContractArtifactRequest("valid.wsdl", content));

        Assert.True(preview.Analysis.Importable);
        Assert.NotNull(preview.Diff);
        Assert.Equal(active.Id, preview.Diff!.LeftContractVersionId);
        Assert.Equal(Guid.Empty, preview.Diff.RightContractVersionId);
        Assert.All(preview.Diff.Changes, change =>
            Assert.Contains(change.Impact, new[]
            {
                IntegrationContractDiffImpactEnum.MappingRequired,
                IntegrationContractDiffImpactEnum.RequiresDevelopment
            }));
        Assert.Equal(beforeCount, await fixture.Context.IntegrationContractVersions.CountAsync(x => x.MethodId == method.Id));
    }
    [Fact]
    public async Task AnalyzeStandaloneXsd_ShouldRequireExplicitSoapMetadataAndThenBeImportable()
    {
        var analyzer = new IntegrationContractArtifactAnalyzer();
        var content = await File.ReadAllBytesAsync(Fixture("valid-standalone.xsd"));

        var incomplete = await analyzer.AnalyzeAsync(new AnalyzeIntegrationContractArtifactRequest("valid-standalone.xsd", content));
        var complete = await analyzer.AnalyzeAsync(new AnalyzeIntegrationContractArtifactRequest(
            "valid-standalone.xsd",
            content,
            "StandaloneOperation",
            "urn:cfa:p42:xsd/StandaloneOperation"));

        Assert.False(incomplete.Importable);
        Assert.Contains(incomplete.Issues, issue => issue.Code == "XSD_OPERATION_REQUIRED");
        Assert.Contains(incomplete.Issues, issue => issue.Code == "XSD_SOAP_ACTION_REQUIRED");
        Assert.True(complete.Importable);
        Assert.Equal(IntegrationContractArtifactKindEnum.XmlSchema, complete.ArtifactKind);
        Assert.Equal(2, complete.Parameters.Count);
    }

    [Fact]
    public async Task ImportValidStandaloneXsd_ShouldCreateDraftWithExplicitSoapMetadata()
    {
        await using var fixture = await ContextFixture.CreateAsync();
        await new IntegrationCatalogBootstrapper(fixture.Context).EnsureAsync();
        var method = await fixture.Context.IntegrationMethods.SingleAsync(x => x.Code == "WSCFAACH.Proc_Contrapartidas");
        var content = await File.ReadAllBytesAsync(Fixture("valid-standalone.xsd"));
        var analyzer = new IntegrationContractArtifactAnalyzer();
        var importer = new IntegrationContractArtifactImportService(
            fixture.Context,
            analyzer,
            new IntegrationContractVersionService(fixture.Context));

        var result = await importer.ImportDraftAsync(method.Id, new ImportIntegrationContractArtifactRequest(
            "p4.2-xsd-v1",
            "valid-standalone.xsd",
            content,
            "StandaloneOperation",
            "urn:cfa:p42:xsd/StandaloneOperation",
            null,
            "p4.2-test"));

        Assert.Equal(IntegrationContractArtifactKindEnum.XmlSchema, result.Analysis.ArtifactKind);
        Assert.Equal(IntegrationContractVersionStatusEnum.Draft, result.Contract.Contract.Status);
        Assert.Equal("StandaloneOperation", result.Contract.Contract.OperationName);
        Assert.Equal("urn:cfa:p42:xsd/StandaloneOperation", result.Contract.Contract.SoapAction);
        Assert.Equal(2, result.Contract.Parameters.Count);
    }

    [Fact]
    public async Task ImportValidWsdl_ShouldCreateNewDraftWithoutChangingActiveVersion()
    {
        await using var fixture = await ContextFixture.CreateAsync();
        await new IntegrationCatalogBootstrapper(fixture.Context).EnsureAsync();
        var method = await fixture.Context.IntegrationMethods.SingleAsync(x => x.Code == "WSCFAACH.Proc_Transacciones");
        var activeBefore = await fixture.Context.IntegrationContractVersions
            .AsNoTracking()
            .SingleAsync(x => x.MethodId == method.Id && x.Status == IntegrationContractVersionStatusEnum.Active);
        var content = await File.ReadAllBytesAsync(Fixture("valid.wsdl"));
        var analyzer = new IntegrationContractArtifactAnalyzer();
        var versions = new IntegrationContractVersionService(fixture.Context);
        var importer = new IntegrationContractArtifactImportService(fixture.Context, analyzer, versions);

        var result = await importer.ImportDraftAsync(method.Id, new ImportIntegrationContractArtifactRequest(
            "p4.2-import-v1",
            "valid.wsdl",
            content,
            null,
            null,
            "Import local seguro",
            "p4.2-test"));

        Assert.True(result.Analysis.Importable);
        Assert.Equal(IntegrationContractVersionStatusEnum.Draft, result.Contract.Contract.Status);
        Assert.Equal(IntegrationContractSourceTypeEnum.WsdlImport, result.Contract.Contract.SourceType);
        Assert.Equal(1, result.Contract.Contract.Revision);
        Assert.Equal("p4.2-test", result.Contract.Contract.ImportedBy);
        Assert.Equal(result.Analysis.Sha256, result.Contract.Contract.SourceDocumentHash);
        Assert.Equal(result.Analysis.Parameters.Count, result.Contract.Parameters.Count);
        Assert.All(result.Contract.Parameters, parameter => Assert.Equal(result.Contract.Contract.Id, parameter.ContractVersionId));

        var activeAfter = await fixture.Context.IntegrationContractVersions
            .AsNoTracking()
            .SingleAsync(x => x.MethodId == method.Id && x.Status == IntegrationContractVersionStatusEnum.Active);
        Assert.Equal(activeBefore.Id, activeAfter.Id);
        Assert.Equal(activeBefore.Revision, activeAfter.Revision);
    }

    [Fact]
    public async Task ImportRejectedArtifact_ShouldPersistNothing()
    {
        await using var fixture = await ContextFixture.CreateAsync();
        await new IntegrationCatalogBootstrapper(fixture.Context).EnsureAsync();
        var method = await fixture.Context.IntegrationMethods.SingleAsync(x => x.Code == "WSCFAACH.Proc_Transacciones");
        var beforeCount = await fixture.Context.IntegrationContractVersions.CountAsync(x => x.MethodId == method.Id);
        var content = await File.ReadAllBytesAsync(Fixture("remote-reference.wsdl"));
        var analyzer = new IntegrationContractArtifactAnalyzer();
        var importer = new IntegrationContractArtifactImportService(
            fixture.Context,
            analyzer,
            new IntegrationContractVersionService(fixture.Context));

        var error = await Assert.ThrowsAsync<IntegrationContractArtifactRejectedException>(() => importer.ImportDraftAsync(
            method.Id,
            new ImportIntegrationContractArtifactRequest(
                "p4.2-rejected",
                "remote-reference.wsdl",
                content,
                null,
                null,
                null,
                "p4.2-test")));

        Assert.Contains(error.Analysis.Issues, issue => issue.Code == "EXTERNAL_REFERENCE_NOT_ALLOWED");
        Assert.Equal(beforeCount, await fixture.Context.IntegrationContractVersions.CountAsync(x => x.MethodId == method.Id));
    }


    private static string MultipleOperationWsdl() => """
        <?xml version="1.0" encoding="utf-8"?>
        <definitions xmlns="http://schemas.xmlsoap.org/wsdl/"
                     xmlns:soap="http://schemas.xmlsoap.org/wsdl/soap/"
                     xmlns:xsd="http://www.w3.org/2001/XMLSchema"
                     xmlns:tns="urn:cfa:p72:multi"
                     targetNamespace="urn:cfa:p72:multi">
          <types>
            <xsd:schema targetNamespace="urn:cfa:p72:multi" elementFormDefault="qualified">
              <xsd:element name="RequestA">
                <xsd:complexType><xsd:sequence><xsd:element name="valueA" type="xsd:string" minOccurs="1" maxOccurs="1" /></xsd:sequence></xsd:complexType>
              </xsd:element>
              <xsd:element name="RequestB">
                <xsd:complexType><xsd:sequence><xsd:element name="valueB" type="xsd:string" minOccurs="1" maxOccurs="1" /></xsd:sequence></xsd:complexType>
              </xsd:element>
            </xsd:schema>
          </types>
          <message name="RequestAMessage"><part name="parameters" element="tns:RequestA" /></message>
          <message name="RequestBMessage"><part name="parameters" element="tns:RequestB" /></message>
          <portType name="MultiPort">
            <operation name="OperationA"><input message="tns:RequestAMessage" /></operation>
            <operation name="OperationB"><input message="tns:RequestBMessage" /></operation>
          </portType>
          <binding name="MultiBinding" type="tns:MultiPort">
            <soap:binding style="document" transport="http://schemas.xmlsoap.org/soap/http" />
            <operation name="OperationA"><soap:operation soapAction="urn:cfa:p72:multi/A" /><input><soap:body use="literal" /></input></operation>
            <operation name="OperationB"><soap:operation soapAction="urn:cfa:p72:multi/B" /><input><soap:body use="literal" /></input></operation>
          </binding>
        </definitions>
        """;

    private static string Fixture(string name)
        => Path.Combine(AppContext.BaseDirectory, "TestData", "Integrations", "Contracts", "P4.2", name);

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
