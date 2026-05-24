using FluentAssertions;

namespace Cfa.ACHInterbank.Tests.NachaFunctional;

internal static class NachaTestDataPaths
{
    public static string NachaRoot => ResolveDirectory("TestData", "Nacha");
    public static string GoldenFilesRoot => ResolveDirectory("TestData", "Nacha", "GoldenFiles");

    public static string AchColombiaOutgoing001 => ResolveFile("GoldenFiles", "ACHColombia", "Outgoing", "ACH_COL_OUT_001.ach");
    public static string AchColombiaIncoming001 => ResolveFile("GoldenFiles", "ACHColombia", "Incoming", "ACH_COL_IN_001.ach");
    public static string AchColombiaReturn001 => ResolveFile("GoldenFiles", "ACHColombia", "Returns", "ACH_COL_RET_001.RET");
    public static string CenitOutgoing001 => ResolveFile("GoldenFiles", "CENIT", "Outgoing", "CENIT_OUT_001.ach");
    public static string CenitIncoming001 => ResolveFile("GoldenFiles", "CENIT", "Incoming", "CENIT_IN_001.ach");
    public static string CenitReturn001 => ResolveFile("GoldenFiles", "CENIT", "Returns", "CENIT_RET_001.RET");

    public static IReadOnlyList<string> AllGoldenFiles =>
    [
        AchColombiaOutgoing001,
        AchColombiaIncoming001,
        AchColombiaReturn001,
        CenitOutgoing001,
        CenitIncoming001,
        CenitReturn001
    ];

    public static string ReadRequiredText(string path)
    {
        File.Exists(path).Should().BeTrue($"el snapshot NACHA-M requerido no existe: {path}");
        var content = File.ReadAllText(path);
        content.Should().NotBeEmpty($"el snapshot NACHA-M requerido esta vacio: {path}");
        return content;
    }

    public static string ResolveMissingSnapshotForTest()
        => Path.Combine(GoldenFilesRoot, "MISSING_SNAPSHOT_FOR_TEST.ach");

    private static string ResolveFile(params string[] parts)
    {
        var path = Path.Combine([NachaRoot, .. parts]);
        File.Exists(path).Should().BeTrue($"el snapshot NACHA-M requerido no existe: {path}");
        return path;
    }

    private static string ResolveDirectory(params string[] parts)
    {
        var current = AppContext.BaseDirectory;
        for (var i = 0; i < 8; i++)
        {
            var candidate = Path.Combine([current, .. parts]);
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            var parent = Directory.GetParent(current);
            if (parent is null)
            {
                break;
            }

            current = parent.FullName;
        }

        throw new DirectoryNotFoundException($"No se encontro TestData NACHA. Base={AppContext.BaseDirectory}, Relative={Path.Combine(parts)}.");
    }
}
