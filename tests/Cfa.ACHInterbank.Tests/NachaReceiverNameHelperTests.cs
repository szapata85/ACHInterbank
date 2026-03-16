using Cfa.ACHInterbank.Domain.Helpers;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class NachaReceiverNameHelperTests
{
    [Fact]
    public void SanitizeForType6_RemovesInvalidCharsAndUppercasesAndTruncates()
    {
        var value = NachaReceiverNameHelper.SanitizeForType6("José Pérez @@@ Comercialización Internacional");

        Assert.Equal("JOSE PEREZ COMERCIALIZ", value);
        Assert.Equal(22, value.Length);
    }

    [Fact]
    public void ValidateType6RawField_WhenEmpty_ReturnsFatal22()
    {
        var error = NachaReceiverNameHelper.ValidateType6RawField("                      ");

        Assert.Contains("Error Fatal ID 22", error);
    }

    [Fact]
    public void ValidateType6RawField_WhenStartsWithSpace_ReturnsFatal26()
    {
        var error = NachaReceiverNameHelper.ValidateType6RawField(" JUAN PEREZ           ");

        Assert.Contains("Error Fatal ID 26", error);
    }

    [Fact]
    public void ValidateType6RawField_WhenHasInvalidCharacter_ReturnsFatal27()
    {
        var error = NachaReceiverNameHelper.ValidateType6RawField("JUAN|PEREZ            ");

        Assert.Contains("Error Fatal ID 27", error);
    }
}
