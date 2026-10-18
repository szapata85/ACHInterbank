using System.Text.RegularExpressions;
using System.Xml.Linq;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class RefactorArchitectureRatchetTests
{
    private static readonly (string Concern, string Pattern, string[] AllowedFiles)[] ApplicationSourceRatchet =
    [
        (
            "ASP.NET",
            @"Microsoft\.AspNetCore|IHttpContextAccessor",
            []),
        (
            "WCF",
            @"System\.ServiceModel|ServiceContract|OperationContract|ClientBase<|ChannelFactory<|BasicHttpBinding|EndpointAddress",
            []),
        (
            "NLog",
            @"\bNLog\b|LogManager\.",
            []),
        (
            "X509Store",
            @"\bX509Store\b|StoreName\.|StoreLocation\.",
            []),
        (
            "manual HttpClient construction",
            @"new\s+HttpClient\b",
            [])
    ];

    private static readonly string[] ApplicationBoundaryPackageAllowlist = [];

    // R2 begins enforcing hidden runtime-service fallbacks. These entries are pre-existing
    // Persistence debt owned by later focused phases; new entries are forbidden and removals
    // must shrink this allowlist. Value objects/options are intentionally excluded.
    private static readonly string[] PersistenceRuntimeFallbackAllowlist =
    [
        "src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/AchContrapartidasByCycleHandler.cs|OperationalCycleWindowResolver",
        "src/Cfa.ACHInterbank.Persistence/ACH/Quartz/Jobs/Implementation/AchCycleSchedulerHandler.cs|OperationalCycleWindowResolver",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchBulkTransactionService.cs|AchTransactionClassificationPolicy",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchCycleAppService.cs|ClearingHouseCyclePolicyResolver",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchCycleAppService.cs|OperationalCycleWindowResolver",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchCycleScheduler.cs|ClearingHouseCyclePolicyResolver",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchCycleScheduler.cs|OperationalCalendarService",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchCycleScheduler.cs|OperationalCycleWindowResolver",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchCycleSeeder.cs|CycleNumberResolver",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchIncomingReturnIngestionService.cs|AchStateTransitionService",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnsService.cs|AchReturnTraceSequenceService",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchReturnsService.cs|AchStateTransitionService",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchTransactionService.cs|AchTransactionClassificationPolicy",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/BatchResolver.cs|CycleTransactionPolicy",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/BatchResolver.cs|OperationalCycleWindowResolver",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitCycleExecutionService.cs|CycleCalendarGuard",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitOperatingCalendarPolicy.cs|CycleNumberResolver",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitOperatingCalendarPolicy.cs|OperationalCalendarService",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitOperatingCalendarPolicy.cs|OperationalCycleWindowResolver",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CenitReturnOfReturnService.cs|CycleNumberResolver",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ClearingHouseCycleConfigService.cs|CycleNumberResolver",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ClearingHouseSpecialDateService.cs|CycleCalendarGuard",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ClearingHouseSpecialDateService.cs|OperationalCalendarService",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ClearingHouseSpecialDateService.cs|OperationalTimeSnapshotProvider",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ContrapartidaDispatchJobService.cs|CycleCalendarGuard",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ContrapartidaDispatchJobService.cs|CycleTransactionPolicy",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ContrapartidaDispatchJobService.cs|OperationalCycleWindowResolver",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/CycleCalendarGuard.cs|OperationalCalendarService",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaIngestionAppService.cs|LegacyCompatibleAdmissionValidator",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaIngestionAppService.cs|NachaConfigResolver",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostParseProcessor.cs|CenitIncomingReturnPolicy",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostParseProcessor.cs|CycleNumberResolver",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostParseProcessor.cs|IncomingNachaAchResultResolver",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostProcessingOrchestrator.cs|CycleCalendarGuard",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostProcessingOrchestrator.cs|OperationalCycleWindowResolver",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaFileBuilder.cs|CycleCalendarGuard",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaFileBuilder.cs|DailyResetBatchNumberGenerator",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaFileBuilder.cs|OperationalTimeSnapshotProvider",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaInboundSimulationService.cs|AchRegulatoryCatalogService",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaInboundSimulationService.cs|AchReturnTraceSequenceService",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaInboundSimulationService.cs|NachaConfigResolver",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaInboundSimulationService.cs|TransactionValidator",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaTransactionValidationService.cs|CycleTransactionPolicy",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcTransaccionesRequestMapper.cs|IntegrationMappingSnapshotBuilder",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/RoutingStrategyService.cs|NullPaymentRailContextService",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/RoutingStrategyService.cs|OperationalCalendarService",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/RoutingStrategyService.cs|OperationalCycleWindowResolver",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/Seeders/CfaOperationalTransactionRestrictionSeeder.cs|CycleNumberResolver",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/Seeders/IntegrationMappingScenarioSeeder.cs|IntegrationMappingSnapshotBuilder",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/Seeders/RegulatoryCycleSeedRepair.cs|CycleNumberResolver",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/TransactionPolicyService.cs|CycleTransactionPolicy",
        "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/TransactionPolicyService.cs|OperationalCycleWindowResolver",
        "src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingBootstrapper.cs|IntegrationMappingSnapshotBuilder",
        "src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingReadinessService.cs|IntegrationMappingSnapshotBuilder",
        "src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingSetService.cs|IntegrationMappingSnapshotBuilder",
        "src/Cfa.ACHInterbank.Persistence/Scheduler/SchedulerAdminService.cs|OperationalCalendarService",
        "src/Cfa.ACHInterbank.Persistence/Scheduler/SchedulerAdminService.cs|OperationalCycleWindowResolver",
    ];

    private static readonly IReadOnlyDictionary<string, string[]> ProjectReferenceRatchet =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj"] =
            [
                "Cfa.ACHInterbank.Application",
                "Cfa.ACHInterbank.Domain",
                "Cfa.ACHInterbank.External",
                "Cfa.ACHInterbank.Persistence",
                "Cfa.ACHInterbank.Persistence.Migrations.SqlServer"
            ],
            ["src/Cfa.ACHInterbank.Application/Cfa.ACHInterbank.Application.csproj"] =
            [
                "Cfa.ACHInterbank.Domain"
            ],
            ["src/Cfa.ACHInterbank.Domain/Cfa.ACHInterbank.Domain.csproj"] = [],
            ["src/Cfa.ACHInterbank.External/Cfa.ACHInterbank.External.csproj"] =
            [
                "Cfa.ACHInterbank.Application",
                "Cfa.ACHInterbank.Domain"
            ],
            ["src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj"] =
            [
                "Cfa.ACHInterbank.Application",
                "Cfa.ACHInterbank.Domain"
            ],
            ["src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/Cfa.ACHInterbank.Persistence.Migrations.SqlServer.csproj"] =
            [
                "Cfa.ACHInterbank.Persistence"
            ],
            ["tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj"] =
            [
                "Cfa.ACHInterbank.Api",
                "Cfa.ACHInterbank.Application",
                "Cfa.ACHInterbank.Domain",
                "Cfa.ACHInterbank.Persistence",
                "Cfa.ACHInterbank.Persistence.Migrations.SqlServer"
            ]
        };

    [Fact]
    public void ApplicationSourceBoundary_ShouldMatchShrinkingLegacyAllowlist()
    {
        var repositoryRoot = FindRepositoryRoot();
        var applicationRoot = Path.Combine(repositoryRoot, "src", "Cfa.ACHInterbank.Application");

        foreach (var (concern, pattern, allowedFiles) in ApplicationSourceRatchet)
        {
            var regex = new Regex(pattern, RegexOptions.CultureInvariant);
            var actual = Directory.EnumerateFiles(applicationRoot, "*.cs", SearchOption.AllDirectories)
                .Where(path => regex.IsMatch(File.ReadAllText(path)))
                .Select(path => RelativePath(repositoryRoot, path))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            var expected = allowedFiles.OrderBy(path => path, StringComparer.Ordinal).ToArray();

            Assert.True(
                expected.SequenceEqual(actual, StringComparer.Ordinal),
                $"Architecture ratchet changed for {concern}. " +
                $"Expected legacy allowlist: [{string.Join(", ", expected)}]. " +
                $"Actual violations: [{string.Join(", ", actual)}]. " +
                "New violations are forbidden; removed violations must also be removed from the allowlist.");
        }
    }


    [Fact]
    public void R2RuntimeComposition_ShouldRejectNewGlobalsAndCriticalHiddenConstruction()
    {
        var repositoryRoot = FindRepositoryRoot();
        var sourceRoot = Path.Combine(repositoryRoot, "src");
        var allSourceFiles = Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories).ToArray();

        var globalConfigViolations = allSourceFiles
            .Where(path => Regex.IsMatch(File.ReadAllText(path), @"\bAppSettings\.Settings\b", RegexOptions.CultureInvariant))
            .Select(path => RelativePath(repositoryRoot, path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        Assert.True(globalConfigViolations.Length == 0,
            $"R2 forbids AppSettings.Settings runtime globals. Violations: [{string.Join(", ", globalConfigViolations)}].");

        var criticalRoots = new[]
        {
            Path.Combine(sourceRoot, "Cfa.ACHInterbank.Api"),
            Path.Combine(sourceRoot, "Cfa.ACHInterbank.Application"),
            Path.Combine(sourceRoot, "Cfa.ACHInterbank.External")
        };
        var runtimeFallbackRegex = new Regex(
            @"\?\?\s*new\s+(?:[\w.]+\.)?\w+(?:Service|Provider|Resolver|Policy|Validator|Builder|Factory|Guard|Strategy|Generator|Store|Auditor)\s*\(",
            RegexOptions.CultureInvariant);
        var criticalFallbackViolations = criticalRoots
            .SelectMany(root => Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            .Where(path => runtimeFallbackRegex.IsMatch(File.ReadAllText(path)))
            .Select(path => RelativePath(repositoryRoot, path))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        Assert.True(criticalFallbackViolations.Length == 0,
            "R2 forbids hidden runtime-service fallbacks in Api/Application/External. " +
            $"Violations: [{string.Join(", ", criticalFallbackViolations)}].");

        var prohibitedConstructionPatterns = new[]
        {
            ("concrete LoggerManager", @"new\s+LoggerManager\s*\("),
            ("hidden SFTP session factory", @"new\s+SshNetAchColombiaSftpSessionFactory\s*\(")
        };
        foreach (var (concern, pattern) in prohibitedConstructionPatterns)
        {
            var regex = new Regex(pattern, RegexOptions.CultureInvariant);
            var violations = allSourceFiles
                .Where(path => regex.IsMatch(File.ReadAllText(path)))
                .Select(path => RelativePath(repositoryRoot, path))
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToArray();
            Assert.True(violations.Length == 0,
                $"R2 forbids {concern} construction outside composition. Violations: [{string.Join(", ", violations)}].");
        }

        var apiRoot = Path.Combine(sourceRoot, "Cfa.ACHInterbank.Api");
        var directPersistenceProviderViolations = Directory.EnumerateFiles(apiRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => Regex.IsMatch(
                File.ReadAllText(path),
                @"new\s+(?:Cfa\.ACHInterbank\.Persistence\.[\w.]+\.)?OperationalTimeSnapshotProvider\s*\(",
                RegexOptions.CultureInvariant))
            .Select(path => RelativePath(repositoryRoot, path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        Assert.True(directPersistenceProviderViolations.Length == 0,
            "R2 requires the API operational-time provider to be supplied by DI. " +
            $"Violations: [{string.Join(", ", directPersistenceProviderViolations)}].");
    }

    [Fact]
    public void R3ExternalIntegrations_ShouldUseManagedClientsAndSafeLogging()
    {
        var repositoryRoot = FindRepositoryRoot();
        var sourceRoot = Path.Combine(repositoryRoot, "src");
        var sourceFiles = Directory.EnumerateFiles(sourceRoot, "*.cs", SearchOption.AllDirectories).ToArray();

        var directHttpClientViolations = sourceFiles
            .Where(path => Regex.IsMatch(File.ReadAllText(path), @"new\s+HttpClient\b", RegexOptions.CultureInvariant))
            .Select(path => RelativePath(repositoryRoot, path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        Assert.True(directHttpClientViolations.Length == 0,
            $"R3 forbids direct HttpClient construction in production source. Violations: [{string.Join(", ", directHttpClientViolations)}].");

        var rawDefaultHttpClientViolations = sourceFiles
            .Where(path => Regex.IsMatch(File.ReadAllText(path), @"\[FromServices\]\s+HttpClient\b", RegexOptions.CultureInvariant))
            .Select(path => RelativePath(repositoryRoot, path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        Assert.True(rawDefaultHttpClientViolations.Length == 0,
            $"R3 requires explicit named/typed policy for external HTTP action usage. Violations: [{string.Join(", ", rawDefaultHttpClientViolations)}].");

        var externalDiPath = Path.Combine(sourceRoot, "Cfa.ACHInterbank.External", "DependencyInjectionService.cs");
        var externalDi = File.ReadAllText(externalDiPath);
        Assert.Contains("ExternalHttpClientNames.HealthProbe", externalDi, StringComparison.Ordinal);
        Assert.Contains("ExternalHttpClientNames.GenericNoRetry", externalDi, StringComparison.Ordinal);
        Assert.Contains("ExternalHttpClientNames.WscfaachSoapNoRetry", externalDi, StringComparison.Ordinal);
        Assert.Contains("ExternalHttpClientNames.WsAxonSoapNoRetry", externalDi, StringComparison.Ordinal);
        Assert.DoesNotContain("Resilience:Soap", externalDi, StringComparison.Ordinal);

        var noRetryHelper = externalDi
            .Split("private static void AddNoRetryHttpClient", StringSplitOptions.None)[1]
            .Split("private static bool IsValidHttpPolicy", StringSplitOptions.None)[0];
        Assert.DoesNotContain("AddRetry", noRetryHelper, StringComparison.Ordinal);
        Assert.DoesNotContain("AddResilienceHandler", noRetryHelper, StringComparison.Ordinal);

        var prohibitedSensitiveLogging = new[]
        {
            "Curl de la petición",
            "datos enviados al autorizador",
            "Respuesta del autorizador",
            "Este es la respuesta del servicio",
            "JsonConvert.SerializeObject(healthCheck)",
            "JsonConvert.SerializeObject(localServers)",
            "Link: {Link}",
            "Error en la creación del token con los datos"
        };
        var sensitiveLoggingViolations = sourceFiles
            .Where(path => prohibitedSensitiveLogging.Any(fragment =>
                File.ReadAllText(path).Contains(fragment, StringComparison.Ordinal)))
            .Select(path => RelativePath(repositoryRoot, path))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        Assert.True(sensitiveLoggingViolations.Length == 0,
            $"R3 secure-logging ratchet failed. Violations: [{string.Join(", ", sensitiveLoggingViolations)}].");
    }

    [Fact]
    public void PersistenceRuntimeFallbacks_ShouldMatchR2ShrinkingLegacyAllowlist()
    {
        var repositoryRoot = FindRepositoryRoot();
        var persistenceRoot = Path.Combine(repositoryRoot, "src", "Cfa.ACHInterbank.Persistence");
        var regex = new Regex(
            @"\?\?\s*new\s+(?:[\w.]+\.)?(?<type>\w+(?:Service|Provider|Resolver|Policy|Validator|Builder|Factory|Guard|Strategy|Generator|Store|Auditor))\s*\(",
            RegexOptions.CultureInvariant);

        var actual = Directory.EnumerateFiles(persistenceRoot, "*.cs", SearchOption.AllDirectories)
            .SelectMany(path => regex.Matches(File.ReadAllText(path)).Cast<Match>()
                .Select(match => $"{RelativePath(repositoryRoot, path)}|{match.Groups["type"].Value}"))
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        var expected = PersistenceRuntimeFallbackAllowlist.OrderBy(value => value, StringComparer.Ordinal).ToArray();

        Assert.True(
            expected.SequenceEqual(actual, StringComparer.Ordinal),
            "R2 Persistence fallback ratchet changed. " +
            $"Expected legacy allowlist: [{string.Join(", ", expected)}]. " +
            $"Actual: [{string.Join(", ", actual)}]. " +
            "New hidden runtime-service fallbacks are forbidden; removed debt must shrink the allowlist.");
    }

    [Fact]
    public void DomainSourceBoundary_ShouldRemainFreeOfPresentationTransportAndConcreteLoggingConcerns()
    {
        var repositoryRoot = FindRepositoryRoot();
        var domainRoot = Path.Combine(repositoryRoot, "src", "Cfa.ACHInterbank.Domain");
        var regex = new Regex(
            @"Microsoft\.AspNetCore|System\.ServiceModel|\bNLog\b|LogManager\.|\bX509Store\b|new\s+HttpClient\s*\(",
            RegexOptions.CultureInvariant);

        var violations = Directory.EnumerateFiles(domainRoot, "*.cs", SearchOption.AllDirectories)
            .Where(path => regex.IsMatch(File.ReadAllText(path)))
            .Select(path => RelativePath(repositoryRoot, path))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void ApplicationBoundaryPackages_ShouldMatchShrinkingLegacyAllowlist()
    {
        var repositoryRoot = FindRepositoryRoot();
        var projectPath = Path.Combine(
            repositoryRoot,
            "src",
            "Cfa.ACHInterbank.Application",
            "Cfa.ACHInterbank.Application.csproj");

        var actual = XDocument.Load(projectPath)
            .Descendants("PackageReference")
            .Select(element => (string?)element.Attribute("Include"))
            .Where(package => package is not null && IsApplicationBoundaryPackage(package))
            .Select(package => package!)
            .OrderBy(package => package, StringComparer.Ordinal)
            .ToArray();
        var expected = ApplicationBoundaryPackageAllowlist
            .OrderBy(package => package, StringComparer.Ordinal)
            .ToArray();

        Assert.True(
            expected.SequenceEqual(actual, StringComparer.Ordinal),
            "Application boundary package ratchet changed. " +
            $"Expected legacy allowlist: [{string.Join(", ", expected)}]. " +
            $"Actual boundary packages: [{string.Join(", ", actual)}]. " +
            "New packages are forbidden; removed packages must also be removed from the allowlist.");
    }

    [Fact]
    public void ProjectReferences_ShouldMatchEstablishedDependencyDirectionRatchet()
    {
        var repositoryRoot = FindRepositoryRoot();

        foreach (var (relativeProjectPath, expectedReferences) in ProjectReferenceRatchet)
        {
            var projectPath = Path.Combine(repositoryRoot, relativeProjectPath.Replace('/', Path.DirectorySeparatorChar));
            var actual = XDocument.Load(projectPath)
                .Descendants("ProjectReference")
                .Select(element => (string?)element.Attribute("Include"))
                .Where(include => !string.IsNullOrWhiteSpace(include))
                .Select(include => include!.Replace('\\', Path.DirectorySeparatorChar).Replace('/', Path.DirectorySeparatorChar))
                .Select(Path.GetFileNameWithoutExtension)
                .OrderBy(reference => reference, StringComparer.Ordinal)
                .ToArray();
            var expected = expectedReferences.OrderBy(reference => reference, StringComparer.Ordinal).ToArray();

            Assert.True(
                expected.SequenceEqual(actual, StringComparer.Ordinal),
                $"Project-reference ratchet changed for {relativeProjectPath}. " +
                $"Expected: [{string.Join(", ", expected)}]. Actual: [{string.Join(", ", actual)}].");
        }
    }

    private static bool IsApplicationBoundaryPackage(string package)
        => package.StartsWith("Microsoft.AspNetCore.", StringComparison.Ordinal)
           || package.Equals("NLog", StringComparison.Ordinal)
           || package.StartsWith("System.ServiceModel.", StringComparison.Ordinal);

    private static string RelativePath(string repositoryRoot, string path)
        => Path.GetRelativePath(repositoryRoot, path).Replace('\\', '/');

    private static string FindRepositoryRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        for (var i = 0; i < 10 && current is not null; i++, current = current.Parent)
        {
            if (File.Exists(Path.Combine(current.FullName, "ACHInterbank.sln")))
            {
                return current.FullName;
            }
        }

        throw new DirectoryNotFoundException(
            $"Could not locate ACHInterbank.sln from {AppContext.BaseDirectory} for architecture ratchet tests.");
    }
}
