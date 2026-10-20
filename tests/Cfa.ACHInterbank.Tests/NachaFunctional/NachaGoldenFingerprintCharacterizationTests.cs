using System.Security.Cryptography;
using Xunit;

namespace Cfa.ACHInterbank.Tests.NachaFunctional;

public sealed class NachaGoldenFingerprintCharacterizationTests
{
    private static readonly IReadOnlyDictionary<string, string> ExpectedSha256 =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["GoldenFiles/ACHColombia/Incoming/ACH_COL_IN_001.ach"] = "60240f257d818ac1a1b648c48de9f9cfb2a2e61b2110374f5cb4347b49d66032",
            ["GoldenFiles/ACHColombia/Outgoing/ACH_COL_OUT_001.ach"] = "779cabff79863e66cac79b77e91525687e3b6e72cff104b96caaeff9fa63315f",
            ["GoldenFiles/ACHColombia/Returns/ACH_COL_RET_001.RET"] = "fde736a96c1c24be0392e1e56bdd71e4910b63b287d5e8c47479a93afd7b96ee",
            ["GoldenFiles/CENIT/Incoming/CENIT_IN_001.ach"] = "16d87fc282fd4bb144a08962bf10d52f8c97745a4aaf31131c786f801ceceb93",
            ["GoldenFiles/CENIT/Outgoing/CENIT_OUT_001.ach"] = "626670feaf2f9323f92518f6d905d079864283efd6b0ab80df7084f303330836",
            ["GoldenFiles/CENIT/Returns/CENIT_RET_001.RET"] = "047fab8f6a35a4e063974c0dabe9f736cd90fd0840e75ab672c831bfdc40ca95",
            ["NormativeSynthetic/Cenit/Ror/CENIT_ROR_IN_001.ach"] = "33991f152af67ef07669b6bfc476b4d8d27263c43b8507b522a69c8823d8fdb4"
        };

    [Fact]
    public void R0CriticalNachaFixtures_ShouldRemainByteForByteStable()
    {
        var actualGoldenFiles = Directory.EnumerateFiles(NachaTestDataPaths.GoldenFilesRoot, "*", SearchOption.AllDirectories)
            .Select(path => Normalize(Path.GetRelativePath(NachaTestDataPaths.NachaRoot, path)))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();
        var expectedGoldenFiles = ExpectedSha256.Keys
            .Where(path => path.StartsWith("GoldenFiles/", StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedGoldenFiles, actualGoldenFiles);

        foreach (var (relativePath, expectedHash) in ExpectedSha256)
        {
            var absolutePath = Path.Combine(
                NachaTestDataPaths.NachaRoot,
                relativePath.Replace('/', Path.DirectorySeparatorChar));

            Assert.True(File.Exists(absolutePath), $"Required R0 NACHA characterization fixture is missing: {relativePath}");

            var actualHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(absolutePath))).ToLowerInvariant();
            Assert.Equal(expectedHash, actualHash);
        }
    }

    private static string Normalize(string path) => path.Replace('\\', '/');
}
