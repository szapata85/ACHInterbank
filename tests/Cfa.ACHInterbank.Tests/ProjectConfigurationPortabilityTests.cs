using FluentAssertions;
using System.Xml.Linq;

namespace Cfa.ACHInterbank.Tests;

public class ProjectConfigurationPortabilityTests
{
    private static readonly string RepoRoot = FindRepositoryRoot();

    [Theory]
    [InlineData("Cfa.ACHInterbank.Api")]
    [InlineData("Cfa.ACHInterbank.Application")]
    [InlineData("Cfa.ACHInterbank.Domain")]
    public void OpenApiContractProject_ShouldGenerateDocumentationXmlWithoutWindowsAbsolutePath(string projectName)
    {
        var projectPath = Path.Combine(RepoRoot, "src", projectName, $"{projectName}.csproj");
        var project = XDocument.Load(projectPath);

        var generateDocumentationFile = project
            .Descendants()
            .FirstOrDefault(element => element.Name.LocalName == "GenerateDocumentationFile");

        generateDocumentationFile.Should().NotBeNull($"{projectName} expone documentación usada por OpenAPI");
        bool.TryParse(generateDocumentationFile!.Value, out var enabled).Should().BeTrue();
        enabled.Should().BeTrue($"{projectName} debe generar su XML de documentación");

        foreach (var documentationFile in project.Descendants().Where(element => element.Name.LocalName == "DocumentationFile"))
        {
            documentationFile.Value.Trim().Should().NotMatchRegex(@"^(?:[A-Za-z]:[\\/]|\\\\)",
                $"{projectName} no debe depender de una ruta absoluta de Windows para generar XML");
        }
    }

    [Fact]
    public void Gitignore_ShouldProtectLocalEnvironmentFiles()
    {
        var gitignore = File.ReadAllText(Path.Combine(RepoRoot, ".gitignore"));

        gitignore.Should().Contain(".env");
        gitignore.Should().Contain("!.env.example");
        gitignore.Should().Contain("!.env.test.example");
    }

    [Fact]
    public void DockerCompose_ShouldNotKeepLegacyHardcodedPostgresDefaults()
    {
        var baseCompose = File.ReadAllText(Path.Combine(RepoRoot, "docker-compose.yml"));
        var postgresCompose = File.ReadAllText(Path.Combine(RepoRoot, "docker-compose.postgres.yml"));
        var sqlServerCompose = File.ReadAllText(Path.Combine(RepoRoot, "docker-compose.sqlserver.yml"));

        baseCompose.Should().NotContain("Cooperativa");
        baseCompose.Should().NotContain("example_password_change_me");
        baseCompose.Should().NotContain("MSSQL_SA_PASSWORD:-");
        baseCompose.Should().NotContain("POSTGRES_PASSWORD:-");

        postgresCompose.Should().Contain("POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:?POSTGRES_PASSWORD es obligatoria}");
        postgresCompose.Should().Contain("ConnectionStrings__PostgresConnection");
        postgresCompose.Should().NotContain("example_password_change_me");
        postgresCompose.Should().NotContain("Cooperativa");

        sqlServerCompose.Should().Contain("MSSQL_SA_PASSWORD: ${MSSQL_SA_PASSWORD:?MSSQL_SA_PASSWORD es obligatoria}");
        sqlServerCompose.Should().Contain("ConnectionStrings__SqlConnection");
        sqlServerCompose.Should().NotContain("Example_sqlServer_2026*");
        sqlServerCompose.Should().NotContain("Cooperativa");
    }

    [Fact]
    public void SqlServerRuntimeScript_ShouldUseContainerShellPasswordAndAvoidVisibleArguments()
    {
        var script = File.ReadAllText(Path.Combine(RepoRoot, "scripts", "start-sqlserver-runtime.ps1"));

        script.Should().Contain("'sqlserver', 'sh', '-lc'");
        script.Should().Contain("SQLCMDPASSWORD=\"$MSSQL_SA_PASSWORD\"");
        script.Should().Contain("/opt/mssql-tools18/bin/sqlcmd");
        script.Should().Contain("/opt/mssql-tools/bin/sqlcmd");
        script.Should().NotContain("SQLCMDPASSWORD=$env:MSSQL_SA_PASSWORD");
        script.Should().NotContain("-P $env:MSSQL_SA_PASSWORD");
        script.Should().NotContain("-P \"$env:MSSQL_SA_PASSWORD\"");
        script.Should().NotContain("-e \"SQLCMDPASSWORD=");
    }

    private static string FindRepositoryRoot()
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

        throw new DirectoryNotFoundException("No se encontro la raiz del repositorio ACHInterbank.sln.");
    }
}
