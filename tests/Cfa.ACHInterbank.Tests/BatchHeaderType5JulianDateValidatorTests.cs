using Cfa.ACHInterbank.Application.Helpers.ACH;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class BatchHeaderType5JulianDateValidatorTests
{
    [Theory]
    [InlineData(null, "   ")]
    [InlineData("", "   ")]
    [InlineData("   ", "   ")]
    [InlineData("1", "001")]
    [InlineData("25", "025")]
    [InlineData("366", "366")]
    public void ValidateAndFormat_WhenValueIsNullOrNumeric_ReturnsFormattedValue(string? input, string expected)
    {
        var result = BatchHeaderType5JulianDateValidator.ValidateAndFormat(input);

        Assert.True(result.IsValid);
        Assert.Equal(expected, result.FormattedValue);
    }

    [Theory]
    [InlineData("A01")]
    [InlineData("12A")]
    [InlineData("$%#")]
    public void ValidateAndFormat_WhenValueContainsNonDigits_ReturnsFatalError65(string input)
    {
        var result = BatchHeaderType5JulianDateValidator.ValidateAndFormat(input);

        Assert.False(result.IsValid);
        Assert.Equal(BatchHeaderType5JulianDateValidator.FatalError65, result.ErrorCode);
    }

    [Theory]
    [InlineData("000")]
    [InlineData("367")]
    [InlineData("999")]
    public void ValidateAndFormat_WhenValueOutOfRange_ReturnsRangeError(string input)
    {
        var result = BatchHeaderType5JulianDateValidator.ValidateAndFormat(input);

        Assert.False(result.IsValid);
        Assert.Equal("ACH-T5-JULIAN-RANGE", result.ErrorCode);
    }

    [Fact]
    public void ApplyToType5Record_WhenValueIsNumeric_UpdatesPositions80To82()
    {
        var record = "5" + new string(' ', BatchHeaderType5JulianDateValidator.RecordLength - 1);

        var result = BatchHeaderType5JulianDateValidator.ApplyToType5Record(record, "7");

        Assert.True(result.IsValid);
        Assert.NotNull(result.Record);
        Assert.Equal("007", result.Record!.Substring(79, 3));
    }
}
