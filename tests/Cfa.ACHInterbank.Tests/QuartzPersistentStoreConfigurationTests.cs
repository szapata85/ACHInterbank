using Cfa.ACHInterbank.Persistence.ACH.Quartz;
using FluentAssertions;
using Microsoft.Extensions.Configuration;

public class QuartzPersistentStoreConfigurationTests
{
    private static string ResolveRepoPath(params string[] parts)
    {
        var probe = new DirectoryInfo(AppContext.BaseDirectory);
        while (probe is not null && !Directory.Exists(Path.Combine(probe.FullName, "src")))
        {
            probe = probe.Parent;
        }

        probe.Should().NotBeNull();
        return Path.Combine(new[] { probe!.FullName }.Concat(parts).ToArray());
    }

    [Fact]
    public void QuartzConfiguration_ShouldDefaultToRamJobStore_WhenModeMissingOrRam()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Quartz:JobStore:Provider"] = "Postgres"
        }).Build();

        var options = QuartzJobStoreOptionsFactory.Create(config);
        options.Mode.Should().Be("RAM");
        options.IsPersistentMode().Should().BeFalse();
    }

    [Fact]
    public void QuartzConfiguration_ShouldReadPersistentPostgresSettings()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Quartz:JobStore:Mode"] = "Persistent",
            ["Quartz:JobStore:Provider"] = "Postgres",
            ["Quartz:JobStore:TablePrefix"] = "QRTZ_",
            ["Quartz:JobStore:Clustered"] = "true"
        }).Build();

        var options = QuartzJobStoreOptionsFactory.Create(config);
        options.IsPersistentMode().Should().BeTrue();
        options.GetNormalizedProvider().Should().Be("Postgres");
        options.TablePrefix.Should().Be("QRTZ_");
        options.Clustered.Should().BeTrue();
    }

    [Fact]
    public void QuartzConfiguration_ShouldRejectUnsupportedProvider()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Quartz:JobStore:Mode"] = "Persistent",
            ["Quartz:JobStore:Provider"] = "Oracle"
        }).Build();

        var options = QuartzJobStoreOptionsFactory.Create(config);
        options.GetNormalizedProvider().Should().Be("Postgres");
    }

    [Fact]
    public void QuartzConfiguration_ShouldNotEnablePersistentStoreInDevelopmentByDefault()
    {
        var config = new ConfigurationBuilder().AddJsonFile(ResolveRepoPath("src","Cfa.ACHInterbank.Api","appsettings.Development.json")).Build();
        var options = QuartzJobStoreOptionsFactory.Create(config);

        options.Mode.Should().Be("RAM");
        options.IsPersistentMode().Should().BeFalse();
    }

    [Fact]
    public void QuartzDocumentation_ShouldReferenceQrtzScripts()
    {
        var content = File.ReadAllText(ResolveRepoPath("docs","dev","quartz-persistent-store-operacion.md"));
        content.Should().Contain("QRTZ_");
        content.Should().Contain("artifacts/sql/quartz");
    }
}
