using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class NachaType7RolloutPolicyTests
{
    [Fact]
    public async Task EvaluateAsync_ShouldBeEligible_WhenThresholdsAreMet()
    {
        await using var context = CreateDb();
        SeedRuns(context, "LAYOUT_A", "ACH", runs: 10, equivalence: 99.9m, includeCriticalDiff: false);

        var options = Options.Create(new NachaGenerationOptions
        {
            Type7RolloutPolicyEnabled = true,
            Type7RequireShadowBeforeDisableFallback = true,
            Type7DisableLegacyFallbackForLayouts = ["LAYOUT_A"],
            Type7EnableTableDrivenForClearingHouses = ["ACH"],
            Type7DisableFallbackEnvironments = ["Development"],
            Type7MinQualifiedRuns = 10,
            Type7MinEquivalencePercent = 99.5m,
            Type7CriticalFieldCodes = ["R7_RETURNCODE"]
        });

        var policy = new NachaType7RolloutPolicy(context, options, new FakeHostEnvironment("Development"));
        var decision = await policy.EvaluateAsync("ACH", new CfgLayoutVariant { VariantCode = "LAYOUT_A" }, "SHADOW_COMPARE");

        Assert.True(decision.EligibleToDisableFallback);
        Assert.False(decision.AllowLegacyFallback);
    }

    [Fact]
    public async Task EvaluateAsync_ShouldBlock_WhenShadowCompareIsRequiredAndModeIsHybrid()
    {
        await using var context = CreateDb();
        SeedRuns(context, "LAYOUT_A", "ACH", runs: 10, equivalence: 99.9m, includeCriticalDiff: false);

        var options = Options.Create(new NachaGenerationOptions
        {
            Type7RolloutPolicyEnabled = true,
            Type7RequireShadowBeforeDisableFallback = true,
            Type7DisableLegacyFallbackForLayouts = ["LAYOUT_A"],
            Type7EnableTableDrivenForClearingHouses = ["ACH"],
            Type7DisableFallbackEnvironments = ["Development"],
            Type7MinQualifiedRuns = 5,
            Type7MinEquivalencePercent = 99m
        });

        var policy = new NachaType7RolloutPolicy(context, options, new FakeHostEnvironment("Development"));
        var decision = await policy.EvaluateAsync("ACH", new CfgLayoutVariant { VariantCode = "LAYOUT_A" }, "HYBRID");

        Assert.True(decision.AllowLegacyFallback);
        Assert.Contains(decision.Reasons, x => x.Contains("ShadowCompareRequired", StringComparison.OrdinalIgnoreCase));
    }

    private static AchDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AchDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AchDbContext(options);
    }

    private static void SeedRuns(AchDbContext context, string layoutCode, string clearingHouse, int runs, decimal equivalence, bool includeCriticalDiff)
    {
        for (var i = 0; i < runs; i++)
        {
            var payload = new
            {
                ClearingHouseCode = clearingHouse,
                Type7LayoutVariantCode = layoutCode,
                Type7DiffByField = includeCriticalDiff ? new Dictionary<string, int> { ["R7_RETURNCODE"] = 1 } : new Dictionary<string, int>(),
                Trace = new[] { $"Type7Summary:Candidates=100;New=100;Legacy=0;Diffs=0;MatchRate={equivalence:0.00}%" }
            };

            context.HistConfigChanges.Add(new HistConfigChange
            {
                ProfileId = 1,
                EntityName = "NachaFileBuilder",
                EntityId = Guid.NewGuid().ToString("N"),
                ChangeType = "GENERATION_TRACE",
                AfterJson = JsonSerializer.Serialize(payload),
                ChangedAtUtc = DateTime.UtcNow.AddMinutes(-i),
                ChangedBy = "test"
            });
        }

        context.SaveChanges();
    }

    private sealed class FakeHostEnvironment : IHostEnvironment
    {
        public FakeHostEnvironment(string environmentName) => EnvironmentName = environmentName;
        public string EnvironmentName { get; set; }
        public string ApplicationName { get; set; } = "Tests";
        public string ContentRootPath { get; set; } = ".";
        public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider { get; set; } = null!;
    }
}
