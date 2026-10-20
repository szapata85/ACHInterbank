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
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>()).Build();

        var options = QuartzJobStoreOptionsFactory.Create(config);
        options.Mode.Should().Be("RAM");
        options.IsPersistentMode().Should().BeFalse();
        options.Provider.Should().BeEmpty();
    }

    [Fact]
    public void QuartzConfiguration_ShouldReadPersistentPostgresSettings()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Quartz:JobStore:Mode"] = "Persistent",
            ["Quartz:JobStore:Provider"] = "postGres",
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

        Action act = () => options.GetNormalizedProvider();
        var exception = act.Should().Throw<InvalidOperationException>().Which;
        exception.Message.Should().Contain("Oracle");
        exception.Message.Should().Contain("Postgres");
        exception.Message.Should().Contain("SqlServer");
    }


    [Fact]
    public void QuartzConfiguration_ShouldReadPersistentSqlServerSettings()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Quartz:JobStore:Mode"] = "Persistent",
            ["Quartz:JobStore:Provider"] = "sqlserver"
        }).Build();

        var options = QuartzJobStoreOptionsFactory.Create(config);
        options.IsPersistentMode().Should().BeTrue();
        options.GetNormalizedProvider().Should().Be("SqlServer");
    }

    [Fact]
    public void QuartzConfiguration_ShouldRejectEmptyProvider()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Quartz:JobStore:Mode"] = "Persistent",
            ["Quartz:JobStore:Provider"] = ""
        }).Build();

        var options = QuartzJobStoreOptionsFactory.Create(config);

        Action act = () => options.GetNormalizedProvider();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*provider is required*");
    }

    [Fact]
    public void QuartzConfiguration_ShouldRejectMissingProviderWhenPersistent()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Quartz:JobStore:Mode"] = "Persistent"
        }).Build();

        var options = QuartzJobStoreOptionsFactory.Create(config);

        options.Provider.Should().BeEmpty();
        options.IsPersistentMode().Should().BeTrue();

        Action act = () => options.GetNormalizedProvider();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*provider is required*");
    }

    [Fact]
    public void QuartzConfiguration_ShouldAllowRamModeWithoutProvider()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Quartz:JobStore:Mode"] = "RAM"
        }).Build();

        var options = QuartzJobStoreOptionsFactory.Create(config);

        options.Provider.Should().BeEmpty();
        options.IsPersistentMode().Should().BeFalse();
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
    public void ProductionConfiguration_ShouldUseNamedPersistentCluster()
    {
        var config = new ConfigurationBuilder().AddJsonFile(ResolveRepoPath("src", "Cfa.ACHInterbank.Api", "appsettings.json")).Build();
        var options = QuartzJobStoreOptionsFactory.Create(config);

        options.SchedulerName.Should().Be("ACHInterbankScheduler");
        options.InstanceId.Should().Be("AUTO");
        options.IsPersistentMode().Should().BeTrue();
        options.Clustered.Should().BeTrue();
        options.AcquireTriggersWithinLock.Should().BeTrue();
    }

    [Fact]
    public void QuartzDocumentation_ShouldReferenceQrtzScripts()
    {
        var content = File.ReadAllText(ResolveRepoPath("docs","dev","quartz-persistent-store-operacion.md"));
        content.Should().Contain("QRTZ_");
        content.Should().Contain("artifacts/sql/quartz");
    }


    [Fact]
    public void QuartzDocs_ShouldMentionBothPostgresAndSqlServerUatPaths()
    {
        var content = File.ReadAllText(ResolveRepoPath("docs","dev","quartz-cierre-tecnico-plan-uat.md"));
        content.Should().Contain("Quartz__JobStore__Provider=Postgres");
        content.Should().Contain("Quartz__JobStore__Provider=SqlServer");
        content.Should().Contain("ConnectionStrings__PostgresConnection");
        content.Should().Contain("ConnectionStrings__SqlConnection");
        content.Should().Contain("sqlserver-qrtz-schema.sql");
        content.Should().Contain("PostgreSQL");
        content.Should().Contain("SQL Server");
    }

    [Fact]
    public void QuartzSqlScripts_ShouldOnlyContainQuartzArtifacts()
    {
        var scriptsDir = ResolveRepoPath("artifacts", "sql", "quartz");
        var sqlFiles = Directory.GetFiles(scriptsDir, "*.sql", SearchOption.TopDirectoryOnly);
        sqlFiles.Should().NotBeEmpty();

        var forbiddenTokens = new[]
        {
            "SoapIntegrationSettings",
            "AchTransactions",
            "AchTransactionStateEvents",
            "Wscfaach",
            "Axon",
            "StateChangedAtUtc",
            "SlaDeadlineAtUtc",
            "ReturnReasonCode",
            "OriginalTraceRef"
        };

        foreach (var file in sqlFiles)
        {
            var content = File.ReadAllText(file);
            forbiddenTokens.Should().OnlyContain(token => !content.Contains(token, StringComparison.OrdinalIgnoreCase));
        }

        var sqlServerContent = File.ReadAllText(Path.Combine(scriptsDir, "sqlserver-qrtz-schema.sql"));
        sqlServerContent.Should().Contain("QRTZ_JOB_DETAILS");
        sqlServerContent.Should().Contain("QRTZ_TRIGGERS");

        var postgresContent = File.ReadAllText(Path.Combine(scriptsDir, "postgres-qrtz-schema.sql"));
        postgresContent.Should().Contain("qrtz_job_details");
        postgresContent.Should().Contain("qrtz_scheduler_state");
        postgresContent.Should().Contain("CREATE TABLE IF NOT EXISTS");
    }

    [Fact]
    public void QuartzSqlServerScript_ShouldDefaultDropDbToFalse()
    {
        var script = File.ReadAllText(ResolveRepoPath("artifacts", "sql", "quartz", "sqlserver-qrtz-schema.sql"));
        script.Should().Contain("DECLARE @DropDb BIT = 0");
        script.Should().NotContain("DECLARE @DropDb BIT = 1");
    }

}
