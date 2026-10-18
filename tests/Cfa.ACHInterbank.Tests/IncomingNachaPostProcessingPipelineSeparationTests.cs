using FluentAssertions;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class IncomingNachaPostProcessingPipelineSeparationTests
{
    [Fact]
    public void R5dOrchestrator_ShouldRemainThinAndDelegateToExplicitStagePipeline()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostProcessingOrchestrator.cs"));

        source.Should().Contain("IncomingNachaPostProcessingPipeline");
        source.Should().Contain("_pipeline.ExecuteAsync(chunkSize, triggeredBy, ct)");
        source.Should().NotContain("ProcTransaccionesAsync(");
        source.Should().NotContain("SaveChangesAsync(");
        source.Should().NotContain("IncomingNachaIntegrationExecution.Add");
    }

    [Fact]
    public void R5dPipeline_ShouldDeclareAllAuthorizedStagesWithCancellationBoundaries()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostProcessingPipeline.cs"));

        var stages = new[]
        {
            "IncomingNachaResolveContextStage",
            "IncomingNachaEligibilityStage",
            "IncomingNachaLinkCorrelationStage",
            "IncomingNachaStateTransitionStage",
            "IncomingNachaDispatchPlanningStage",
            "IncomingNachaExternalDispatchStage",
            "IncomingNachaAuditEventsStage"
        };

        foreach (var stage in stages)
        {
            source.Should().Contain($"internal sealed class {stage}");
        }

        source.Should().Contain("CancellationToken ct");
        source.Should().Contain("PrepareAsync(");
        source.Should().Contain("SendAsync(");
        source.Should().Contain("RecordResponseAsync(");
    }

    [Fact]
    public void R5dPipeline_ShouldNotReopenR5aR5bOrR5cParsedGraphOwnership()
    {
        var root = FindRepositoryRoot();
        var source = File.ReadAllText(Path.Combine(
            root,
            "src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostProcessingPipeline.cs"));

        source.Should().NotContain("NachaPhysicalParser");
        source.Should().NotContain("NachaInboundValidator");
        source.Should().NotContain("NachaInboundPersistenceCoordinator");
        source.Should().NotContain("ParsedNachaEntityMapper");
        source.Should().NotContain("NachaHeaders.AddRange");
        source.Should().NotContain("BeginTransactionAsync");
        source.Should().NotContain("CommitAsync");
        source.Should().NotContain("RollbackAsync");
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "ACHInterbank.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("No se encontró la raíz del repositorio para validar la separación R5D.");
    }
}
