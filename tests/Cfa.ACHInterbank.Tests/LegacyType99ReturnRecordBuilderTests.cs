using Cfa.ACHInterbank.Tests.TestSupport;

namespace Cfa.ACHInterbank.Tests;

public class LegacyType99ReturnRecordBuilderTests
{
    [Fact]
    public void Build_ShouldPlaceFieldsAtTheLegacyType99ContractPositions()
    {
        const string reason = "R01";
        const string originalTrace = "123456780000001";
        const string addendaTrace = "123456789012345";

        var record = LegacyType99ReturnRecordBuilder.Build(reason, originalTrace, addendaTrace);

        Assert.Equal(106, record.Length);
        Assert.Equal('7', record[0]);
        Assert.Equal("99", record.Substring(1, 2));
        Assert.Equal(reason, record.Substring(3, 3));
        Assert.Equal(originalTrace, record.Substring(6, 15));
        Assert.Equal(addendaTrace, record.Substring(91, 15));
    }

    [Fact]
    public void Build_WithFiveCharacterReason_ShouldFailFast()
    {
        var act = () => LegacyType99ReturnRecordBuilder.Build("DEV14", "123456780000001");

        var exception = Assert.Throws<ArgumentException>(act);
        Assert.Equal("returnReason", exception.ParamName);
    }
}
