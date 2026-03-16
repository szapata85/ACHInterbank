using System;
using Cfa.ACHInterbank.Domain.Helpers;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class DigitoChequeoHelperTests
{
    [Fact]
    public void CalcularDigitoChequeo_WithValidRoute_ReturnsExpectedDigit()
    {
        var result = DigitoChequeoHelper.CalcularDigitoChequeo("76543210");

        Assert.Equal("4", result);
    }

    [Fact]
    public void CalcularDigitoChequeo_WithNonNumericRoute_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => DigitoChequeoHelper.CalcularDigitoChequeo("12A45678"));
    }
}
