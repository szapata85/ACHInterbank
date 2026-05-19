using FluentAssertions;

namespace Cfa.ACHInterbank.Tests;

public class ProjectConfigurationPortabilityTests
{
    private static readonly string RepoRoot = FindRepositoryRoot();

    [Fact]
    public void ApiProject_DocumentationFile_ShouldNotUseWindowsAbsolutePath()
    {
        var projectPath = Path.Combine(RepoRoot, "src", "Cfa.ACHInterbank.Api", "Cfa.ACHInterbank.Api.csproj");
        var content = File.ReadAllText(projectPath);

        content.Should().Contain("<GenerateDocumentationFile>True</GenerateDocumentationFile>");
        content.Should().NotMatchRegex(@"<DocumentationFile>[A-Za-z]:\\");
        content.Should().Contain(@"<DocumentationFile>bin\$(Configuration)\$(TargetFramework)\Cfa.ACHInterbank.Api.xml</DocumentationFile>");
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
        var compose = File.ReadAllText(Path.Combine(RepoRoot, "docker-compose.yml"));

        compose.Should().NotContain("Cooperativa");
        compose.Should().NotContain("POSTGRES_USER:-sa");
        compose.Should().Contain("example_password_change_me");
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
