using System.Text;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Moq;

namespace Cfa.ACHInterbank.Tests;

/// <summary>
/// I08-ACH: la generación AUTÓNOMA falla cerrada ante bytes o metadatos distintos.
/// Estas pruebas InMemory NO acreditan reversión física SQL; esa prueba requiere BD real.
/// </summary>
public sealed partial class AchColombiaAutomaticReturnI06Tests
{
    [Fact]
    public async Task I08_AutomaticGenerationConfirmsOnlyAfterMatchingRebuiltArtifact()
    {
        await using var db = BuildDb();
        Seed(db);
        var generator = WorkingGenerator(db);
        var reconstructed = CompatibleArtifact();
        var result = await NewService(db, generator.Object, artifact: reconstructed.Object)
            .GenerateAutomaticallyAsync(1);
        Assert.Equal("00001234.RET", result.FileName);
        Assert.Equal("ARCHIVO", Encoding.ASCII.GetString(result.Content));
        Assert.Equal("ARCHIVO_GENERADO_SIN_ENVIO", db.AchColombiaReturnPreparations.Single().Status);
        reconstructed.Verify(x => x.BuildAsync("00001234.RET", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("bytes")]
    [InlineData("camara")]
    [InlineData("ciclo")]
    [InlineData("transaccion")]
    [InlineData("registros")]
    public async Task I08_AutomaticGenerationDoesNotApproveDifferentArtifact(string scenario)
    {
        await using var db = BuildDb();
        Seed(db);
        var generator = WorkingGenerator(db);
        var reconstructed = new Mock<IAchOutboundReturnArtifactService>(MockBehavior.Strict);
        reconstructed.Setup(x => x.BuildAsync("00001234.RET", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchOutboundReturnArtifact("00001234.RET",
                Encoding.ASCII.GetBytes(scenario == "bytes" ? "CAMBIADO" : "ARCHIVO"),
                scenario == "registros" ? 2 : 1, 1,
                scenario == "ciclo" ? "OTRO-CICLO" : "C4",
                scenario == "camara" ? 999 : 41,
                new[] { scenario == "transaccion" ? 999 : 77 }, "SHA-NO-CONFIABLE"));

        var error = await Assert.ThrowsAsync<AchColombiaReturnReviewConflictException>(() =>
            NewService(db, generator.Object, artifact: reconstructed.Object)
                .GenerateAutomaticallyAsync(1));
        Assert.Contains("no coincide", error.Message);
        Assert.Equal("PREPARADA_SIN_ENVIO", db.AchColombiaReturnPreparations.Single().Status);
        generator.Verify(x => x.GenerateReturnsFileAsync(It.IsAny<GenerateReturnsFileRequest>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task I08_AutomaticGenerationRequiresIntegrityServiceEvenWithBothGatesEnabled()
    {
        await using var db = BuildDb();
        Seed(db);
        var generator = new Mock<IAchReturnsService>(MockBehavior.Strict);
        await Assert.ThrowsAsync<AchColombiaReturnReviewConflictException>(() =>
            NewService(db, generator.Object).GenerateAutomaticallyAsync(1));
        Assert.Empty(db.AchReturnsGenerated);
        generator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task I08_AutomaticGenerationStopsIfRebuildingFails()
    {
        await using var db = BuildDb();
        Seed(db);
        var generator = WorkingGenerator(db);
        var reconstruction = new Mock<IAchOutboundReturnArtifactService>(MockBehavior.Strict);
        reconstruction.Setup(x => x.BuildAsync("00001234.RET", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Archivo no reconstruible"));
        var error = await Assert.ThrowsAsync<AchColombiaReturnReviewConflictException>(() =>
            NewService(db, generator.Object, artifact: reconstruction.Object)
                .GenerateAutomaticallyAsync(1));
        Assert.Contains("no pudo verificarse", error.Message);
        Assert.Equal("PREPARADA_SIN_ENVIO", db.AchColombiaReturnPreparations.Single().Status);
    }

    [Fact]
    public async Task I08_CenitNeverCallsAutomaticReturnGeneratorOrRebuilder()
    {
        await using var db = BuildDb();
        Seed(db, "CENIT");
        var generator = new Mock<IAchReturnsService>(MockBehavior.Strict);
        var reconstruction = new Mock<IAchOutboundReturnArtifactService>(MockBehavior.Strict);
        await Assert.ThrowsAsync<AchColombiaReturnReviewConflictException>(() =>
            NewService(db, generator.Object, artifact: reconstruction.Object)
                .GenerateAutomaticallyAsync(1));
        generator.VerifyNoOtherCalls();
        reconstruction.VerifyNoOtherCalls();
    }
}
