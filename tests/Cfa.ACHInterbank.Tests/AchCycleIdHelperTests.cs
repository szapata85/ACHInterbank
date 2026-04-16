using Cfa.ACHInterbank.Domain.Helpers;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class AchCycleIdHelperTests
{
    [Fact]
    public void GenerateId_Returns40CharHex()
    {
        var id = AchCycleIdHelper.GenerateId(1, "CICLO-OPERATIVO-LARGO", new DateTime(2026, 4, 14));

        Assert.Equal(40, id.Length);
        Assert.Matches("^[a-f0-9]{40}$", id);
    }
}
