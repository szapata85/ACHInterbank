using System.Text.RegularExpressions;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class CfaSoapP81StaticProtectionTests
{
    [Fact]
    public void ContractDrivenRequestPaths_ShouldRemainWiredForAllThreeSoapOperations()
    {
        var procTransacciones = Read("src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcTransaccionesRequestMapper.cs");
        Assert.Contains("_options.UseContractDrivenRequest", procTransacciones, StringComparison.Ordinal);
        Assert.Contains("ResolveContractDrivenAsync", procTransacciones, StringComparison.Ordinal);
        Assert.Contains("_requestResolver.ResolveAsync", procTransacciones, StringComparison.Ordinal);
        Assert.Contains("_soapBodyWriter", procTransacciones, StringComparison.Ordinal);
        Assert.Contains(".Write(resolved)", procTransacciones, StringComparison.Ordinal);

        var procContrapartidas = Read("src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcContrapartidasRequestMapper.cs");
        Assert.Contains("_options.UseContractDrivenRequest", procContrapartidas, StringComparison.Ordinal);
        Assert.Contains("ResolveContractDrivenAsync", procContrapartidas, StringComparison.Ordinal);
        Assert.Contains("_requestResolver.ResolveAsync", procContrapartidas, StringComparison.Ordinal);
        Assert.Contains(".Write(resolved)", procContrapartidas, StringComparison.Ordinal);

        var registrarRespuesta = Read("src/Cfa.ACHInterbank.External/Connections/RespuestaTransaccionesAch/RespuestaTransaccionesAchGateway.cs");
        Assert.Contains("_dispatchOptions.UseContractDrivenRequest", registrarRespuesta, StringComparison.Ordinal);
        Assert.Contains("BuildContractDrivenOperationAsync", registrarRespuesta, StringComparison.Ordinal);
        Assert.Contains("_requestResolver.ResolveAsync", registrarRespuesta, StringComparison.Ordinal);
        Assert.Contains("_soapRequestBodyWriter.Write", registrarRespuesta, StringComparison.Ordinal);

        var persistenceDi = Read("src/Cfa.ACHInterbank.Persistence/DependencyInjectionService.cs");
        Assert.Contains("IIntegrationSourceProviderRegistry", persistenceDi, StringComparison.Ordinal);
        Assert.Contains("IntegrationSourceProviderRegistry", persistenceDi, StringComparison.Ordinal);
        Assert.Contains("IIntegrationTransformationRegistry", persistenceDi, StringComparison.Ordinal);
        Assert.Contains("IntegrationTransformationRegistry", persistenceDi, StringComparison.Ordinal);
        Assert.Contains("IIntegrationRequestResolver, IntegrationRequestResolver", persistenceDi, StringComparison.Ordinal);
        Assert.Contains("ISoapRequestBodyWriter, ContractDrivenSoapBodyWriter", persistenceDi, StringComparison.Ordinal);
    }

    [Fact]
    public void ContractVersion_ShouldRemainTheRuntimeContractAuthorityForTheCommonEngine()
    {
        var resolver = Read("src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationExecutionContractResolver.cs");
        Assert.Contains("_context.IntegrationContractVersions", resolver, StringComparison.Ordinal);
        Assert.Contains("IntegrationContractVersionStatusEnum.Active", resolver, StringComparison.Ordinal);
        Assert.Contains("x.ContractVersionId == contract.Id", resolver, StringComparison.Ordinal);
        Assert.Contains("contract.OperationName", resolver, StringComparison.Ordinal);
        Assert.Contains("contract.TargetNamespace", resolver, StringComparison.Ordinal);
        Assert.Contains("contract.SoapAction", resolver, StringComparison.Ordinal);

        var requestResolver = Read("src/Cfa.ACHInterbank.Application/Integrations/Services/IntegrationRequestResolver.cs");
        Assert.Contains("_contractResolver.ResolveActiveAsync(context.MethodCode", requestResolver, StringComparison.Ordinal);
        Assert.Contains("contract.Parameters", requestResolver, StringComparison.Ordinal);

        var writer = Read("src/Cfa.ACHInterbank.Application/Integrations/Services/ContractDrivenSoapBodyWriter.cs");
        Assert.Contains("RESOLVED_REQUEST_PARAMETER_NOT_IN_CONTRACT", writer, StringComparison.Ordinal);
        Assert.Contains("RESOLVED_REQUEST_CONTRACT_HEADER_MISMATCH", writer, StringComparison.Ordinal);
        Assert.Contains("contract.Parameters", writer, StringComparison.Ordinal);
        Assert.Contains("OrderBy(parameter => parameter.SortOrder)", writer, StringComparison.Ordinal);
    }

    [Fact]
    public void FeatureFlaggedRollbackPaths_ShouldRemainAvailableUntilP8CutoverIsCertified()
    {
        var procTransaccionesOptions = Read("src/Cfa.ACHInterbank.Domain/Models/Configurations/ProcTransaccionesDispatchOptions.cs");
        var procContrapartidasOptions = Read("src/Cfa.ACHInterbank.Domain/Models/Configurations/ProcContrapartidasDispatchOptions.cs");
        var registrarRespuestaOptions = Read("src/Cfa.ACHInterbank.Domain/Models/Configurations/RegistrarRespuestaDispatchOptions.cs");

        Assert.Contains("public bool UseContractDrivenRequest", procTransaccionesOptions, StringComparison.Ordinal);
        Assert.Contains("public bool UseContractDrivenRequest", procContrapartidasOptions, StringComparison.Ordinal);
        Assert.Contains("public bool UseContractDrivenRequest", registrarRespuestaOptions, StringComparison.Ordinal);

        Assert.Contains("ResolveLegacyAsync", Read("src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcTransaccionesRequestMapper.cs"), StringComparison.Ordinal);
        Assert.Contains("ResolveLegacyAsync", Read("src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcContrapartidasRequestMapper.cs"), StringComparison.Ordinal);
        Assert.Contains("_requestMapper.Map(command)", Read("src/Cfa.ACHInterbank.External/Connections/RespuestaTransaccionesAch/RespuestaTransaccionesAchGateway.cs"), StringComparison.Ordinal);
    }

    [Fact]
    public void IntegrationsSpa_ShouldKeepOnlyTheCurrentFunctionalRoutes()
    {
        var routes = Read("web/ach-interbank-ui/src/app/features/integrations/integrations.routes.ts");
        foreach (var expected in new[]
                 {
                     "path: ''",
                     "path: 'contrato'",
                     "path: 'relacion-informacion'",
                     "path: 'relacion-informacion/:methodCode/:mappingSetId'",
                     "path: 'conexion'"
                 })
        {
            Assert.Contains(expected, routes, StringComparison.Ordinal);
        }

        Assert.DoesNotContain("path: 'mappings'", routes, StringComparison.Ordinal);
        Assert.DoesNotContain("path: 'soap-settings'", routes, StringComparison.Ordinal);
        Assert.DoesNotContain("redirectTo", routes, StringComparison.Ordinal);
    }

    [Fact]
    public void RetiredSoapIntegrationSettingsService_ShouldStayAbsentAndConnectionApiShouldRemainCurrent()
    {
        const string candidateRelativePath = "web/ach-interbank-ui/src/app/core/services/soap-integration-settings.service.ts";
        var candidateSymbols = new[]
        {
            "SoapIntegrationSettingsService",
            "SoapInputParameterMapping",
            "SoapEndpointMethodMapping",
            "SoapIntegrationSettings"
        };

        var root = RepositoryRoot();
        var appRoot = Path.Combine(root, "web", "ach-interbank-ui", "src", "app");
        var candidatePath = Path.Combine(root, candidateRelativePath.Replace('/', Path.DirectorySeparatorChar));
        Assert.False(File.Exists(candidatePath), $"P8.2 retired candidate still exists: {candidateRelativePath}.");

        var allTsFiles = Directory.EnumerateFiles(appRoot, "*.ts", SearchOption.AllDirectories).ToArray();
        var productionTsFiles = allTsFiles
            .Where(path => !path.EndsWith(".spec.ts", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        var testTsFiles = allTsFiles
            .Where(path => path.EndsWith(".spec.ts", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        static string[] FindReferences(IEnumerable<string> paths, IEnumerable<string> symbols, string rootPath)
            => paths
                .SelectMany(path => symbols
                    .Where(symbol => Regex.IsMatch(File.ReadAllText(path), $@"\b{Regex.Escape(symbol)}\b", RegexOptions.CultureInvariant))
                    .Select(symbol => $"{RelativePath(rootPath, path)}::{symbol}"))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();

        var productionReferences = FindReferences(productionTsFiles, candidateSymbols, root);
        var testReferences = FindReferences(testTsFiles, candidateSymbols, root);
        var importPathReferences = allTsFiles
            .Where(path => File.ReadAllText(path).Contains("soap-integration-settings.service", StringComparison.Ordinal))
            .Select(path => RelativePath(root, path))
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            productionReferences.Length == 0,
            $"P8.2 retired candidate has production references: [{string.Join(", ", productionReferences)}].");
        Assert.True(
            testReferences.Length == 0,
            $"P8.2 retired candidate has test references: [{string.Join(", ", testReferences)}].");
        Assert.True(
            importPathReferences.Length == 0,
            $"P8.2 retired candidate has import/path references: [{string.Join(", ", importPathReferences)}].");

        var currentConnectionPage = Read("web/ach-interbank-ui/src/app/features/integrations/pages/soap-integration-settings.component.ts");
        Assert.Contains("IntegrationConnectionApi", currentConnectionPage, StringComparison.Ordinal);
        Assert.DoesNotContain("SoapIntegrationSettingsService", currentConnectionPage, StringComparison.Ordinal);
    }

    private static string Read(string relativePath)
        => File.ReadAllText(Path.Combine(RepositoryRoot(), relativePath.Replace('/', Path.DirectorySeparatorChar)));

    private static string RepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            if (File.Exists(Path.Combine(current.FullName, "ACHInterbank.sln")))
            {
                return current.FullName;
            }

            current = current.Parent;
        }

        throw new DirectoryNotFoundException("No se encontró ACHInterbank.sln al resolver el root para los ratchets P8.1.");
    }

    private static string RelativePath(string root, string path)
        => Path.GetRelativePath(root, path).Replace('\\', '/');

}
