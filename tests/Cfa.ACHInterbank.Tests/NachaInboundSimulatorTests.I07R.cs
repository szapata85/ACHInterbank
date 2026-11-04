using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public partial class NachaInboundSimulatorTests
{
    [Theory]
    [InlineData("ACHCOL", "ACH-CYCLE", 2)]
    [InlineData("CENIT", "CENIT-CYCLE", 3)]
    public async Task I07R_DownloadVerifiesStoredSha256AndFileLength(
        string houseCode, string cycleId, int originId)
    {
        await using var context = CreateContext();
        Seed(context);
        var output = CreateTempOutput();
        var simulator = CreateService(context, output);
        var response = await simulator.GenerateAsync(new GenerateNachaInboundSimulationRequest
        {
            ClearingHouseCode = houseCode,
            ScenarioType = NachaInboundSimulationType.IncomingCredit,
            OriginFinancialInstitutionId = originId,
            EntriesCount = 1,
            Amount = 1000,
            ReferencePrefix = "I07R",
            BusinessDate = new DateOnly(2026, 5, 20),
            CycleCode = cycleId
        }, "uat-test");

        var valid = await simulator.GetFileAsync(response.Id);
        Assert.NotNull(valid);
        Assert.Equal(response.FileSizeBytes, valid.Value.Content.LongLength);
        var stored = await context.NachaInboundSimulations.SingleAsync(x => x.Id == response.Id);
        Assert.Equal(NachaInboundSimulationStatus.Downloaded, stored.Status);

        // Mismo tamaño, distinto hash: no basta con revisar FileSizeBytes.
        var tampered = (byte[])valid.Value.Content.Clone();
        tampered[5] ^= 1;
        await File.WriteAllBytesAsync(stored.FilePath, tampered);
        var hashError = await Assert.ThrowsAsync<InvalidOperationException>(
            () => simulator.GetFileAsync(response.Id));
        Assert.Contains("SIMULATOR_FILE_INTEGRITY_FAILED", hashError.Message);

        // También se detectan truncamientos o adiciones de bytes.
        await File.AppendAllTextAsync(stored.FilePath, "TAMPER");
        var lengthError = await Assert.ThrowsAsync<InvalidOperationException>(
            () => simulator.GetFileAsync(response.Id));
        Assert.Contains("SIMULATOR_FILE_INTEGRITY_FAILED", lengthError.Message);
        Assert.Equal(NachaInboundSimulationStatus.Downloaded,
            (await context.NachaInboundSimulations.SingleAsync(x => x.Id == response.Id)).Status);
    }
}
