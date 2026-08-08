using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Moq;

namespace Cfa.ACHInterbank.Tests;

internal static class ReturnOutNachaFileBuilderFactory
{
    public static INachaFileBuilder Create()
    {
        var builder = new Mock<INachaFileBuilder>(MockBehavior.Strict);
        builder.Setup(x => x.BuildReturnOutAsync(It.IsAny<NachaReturnOutBuildRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((NachaReturnOutBuildRequest request, CancellationToken _) => Build(request));
        return builder.Object;
    }

    private static NachaReturnOutBuildResult Build(NachaReturnOutBuildRequest request)
    {
        var lines = new List<string> { Record('1') };
        lines[0] = lines[0].Remove(35, 1).Insert(35, request.FileIdModifier);
        foreach (var batch in request.Batches)
        {
            lines.Add(Record('5'));
            foreach (var entry in batch.Entries)
            {
                lines.Add(Record('6'));
                lines.Add(Record('7'));
            }
            lines.Add(Record('8'));
        }
        lines.Add(Record('9'));
        while (lines.Count % 10 != 0)
        {
            lines.Add(new string('9', 106));
        }

        return new NachaReturnOutBuildResult(string.Concat(lines), lines.Count,
            "OFFICIAL_ACH_SALIDA_DEVOLUCION_V35_1_0", "V35", false);
    }

    private static string Record(char type) => type + new string(' ', 105);
}
