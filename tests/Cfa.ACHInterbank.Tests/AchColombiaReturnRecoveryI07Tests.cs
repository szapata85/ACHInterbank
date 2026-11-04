using System.Text;
using System.Text.Json.Nodes;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Moq;

namespace Cfa.ACHInterbank.Tests;

/// <summary>
/// I07-ACH: pruebas de huella y trazabilidad sin alterar generación ni transporte.
/// Comparte exclusivamente los datos mínimos de pruebas I06 (clase partial).
/// </summary>
public sealed partial class AchColombiaAutomaticReturnI06Tests
{
    [Theory]
    [InlineData("contenido-distinto")]
    [InlineData("sin-auditoria")]
    [InlineData("auditoria-malformada")]
    [InlineData("archivo-distinto")]
    [InlineData("camara-distinta")]
    [InlineData("causal-distinta")]
    [InlineData("estado-envio-distinto")]
    [InlineData("huella-ausente")]
    [InlineData("cantidad-registros-distinta")]
    public async Task I07_RecoveryFailsClosedWhenOriginalFingerprintCannotBeVerified(string scenario)
    {
        await using var db = BuildDb();
        Seed(db);
        SeedGeneratedRecovery(db, withAudit: scenario != "sin-auditoria");

        if (scenario != "sin-auditoria")
        {
            var evt = db.AchTransactionStateEvents.Single();
            if (scenario == "auditoria-malformada")
            {
                evt.PayloadJson = "{contenido incompleto";
            }
            else if (scenario != "contenido-distinto")
            {
                var node = JsonNode.Parse(evt.PayloadJson!)!.AsObject();
                switch (scenario)
                {
                    case "archivo-distinto": node["fileName"] = "OTRO.RET"; break;
                    case "camara-distinta": node["clearingHouseId"] = 999; break;
                    case "causal-distinta": node["returnReasonCode"] = "R04"; break;
                    case "estado-envio-distinto": node["transmissionStatus"] = "Sent"; break;
                    case "huella-ausente": node.Remove("contentSha256"); break;
                    case "cantidad-registros-distinta": node["recordCount"] = 999; break;
                }
                evt.PayloadJson = node.ToJsonString();
            }
            await db.SaveChangesAsync();
        }

        var originalArtifact = Encoding.UTF8.GetBytes(
            scenario == "contenido-distinto" ? "ARCHIVO-ALTERADO" : "ARCHIVO");
        var artifacts = new Mock<IAchOutboundReturnArtifactService>(MockBehavior.Strict);
        artifacts.Setup(x => x.BuildAsync("00001234.RET", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchOutboundReturnArtifact("00001234.RET", originalArtifact,
                1, 1, "C4", 41, new[] { 77 }, "HASH-NO-CONFIABLE"));
        var generator = new Mock<IAchReturnsService>(MockBehavior.Strict);
        var service = NewService(db, generator.Object, artifact: artifacts.Object);

        var error = await Assert.ThrowsAsync<AchColombiaReturnReviewConflictException>(
            () => service.RecoverGeneratedFileAsync(1));
        Assert.Contains("No se pudo comprobar", error.Message);
        Assert.Single(db.AchReturnsGenerated);
        Assert.Equal("ARCHIVO_GENERADO_SIN_ENVIO", db.AchColombiaReturnPreparations.Single().Status);
        generator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task I07_RecoveryVerifiesShaAgainstBytesNotArtifactClaimedHash()
    {
        await using var db = BuildDb();
        Seed(db);
        SeedGeneratedRecovery(db);
        var artifacts = new Mock<IAchOutboundReturnArtifactService>(MockBehavior.Strict);
        artifacts.Setup(x => x.BuildAsync("00001234.RET", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AchOutboundReturnArtifact("00001234.RET",
                Encoding.UTF8.GetBytes("ARCHIVO"), 1, 1, "C4", 41, new[] { 77 },
                "HUELLA_DECLARADA_INCORRECTA"));
        var generator = new Mock<IAchReturnsService>(MockBehavior.Strict);
        var output = await NewService(db, generator.Object, artifact: artifacts.Object)
            .RecoverGeneratedFileAsync(1);

        Assert.Equal("ARCHIVO", Encoding.UTF8.GetString(output.Content));
        Assert.Equal("00001234.RET", output.FileName);
        generator.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task I07_RecoveryNeverExposesCenitReturnThroughAchEndpoint()
    {
        await using var db = BuildDb();
        Seed(db, "CENIT");
        SeedGeneratedRecovery(db);
        var artifacts = new Mock<IAchOutboundReturnArtifactService>(MockBehavior.Strict);
        var generator = new Mock<IAchReturnsService>(MockBehavior.Strict);

        await Assert.ThrowsAsync<AchColombiaReturnReviewConflictException>(() =>
            NewService(db, generator.Object, artifact: artifacts.Object)
                .RecoverGeneratedFileAsync(1));
        generator.VerifyNoOtherCalls();
        artifacts.VerifyNoOtherCalls();
    }
}
