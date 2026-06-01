using System.Text;
using FluentAssertions;

namespace Cfa.ACHInterbank.Tests.NachaFunctional;

internal sealed record NachaGoldenFileComparisonOptions(
    bool CompareByteByByte = true,
    bool NormalizeLineEndingsBeforeComparison = true);

internal sealed record NachaGoldenFileComparisonResult(
    bool Matches,
    string? Message = null,
    int? LineNumber = null,
    int? Position = null,
    char? Expected = null,
    char? Actual = null,
    char? RecordType = null,
    string? FieldName = null);

internal static class NachaGoldenFileComparer
{
    public static NachaGoldenFileComparisonResult CompareFile(
        string expectedPath,
        string actual,
        NachaGoldenFileComparisonOptions? options = null,
        IReadOnlyList<NachaFieldSpan>? fieldSpans = null)
    {
        if (!File.Exists(expectedPath))
        {
            return new NachaGoldenFileComparisonResult(false, $"Golden file NACHA-M no existe: {expectedPath}");
        }

        var expected = File.ReadAllText(expectedPath);
        var result = Compare(expected, actual, options, fieldSpans);
        if (result.Matches)
        {
            return result;
        }

        return result with
        {
            Message = $"{result.Message} Archivo esperado={expectedPath}. Longitud esperada={expected.Length}, Longitud generada={actual.Length}. Actualice el snapshot solo si el cambio funcional es intencional."
        };
    }

    public static void ShouldMatchPhysicalGoldenFile(
        string expectedPath,
        string actual,
        NachaGoldenFileComparisonOptions? options = null,
        IReadOnlyList<NachaFieldSpan>? fieldSpans = null)
    {
        var result = CompareFile(expectedPath, actual, options, fieldSpans);
        result.Matches.Should().BeTrue(result.Message);
    }

    public static NachaGoldenFileComparisonResult Compare(
        string expected,
        string actual,
        NachaGoldenFileComparisonOptions? options = null,
        IReadOnlyList<NachaFieldSpan>? fieldSpans = null)
    {
        options ??= new NachaGoldenFileComparisonOptions();
        if (options.NormalizeLineEndingsBeforeComparison)
        {
            expected = NormalizeLineEndings(expected);
            actual = NormalizeLineEndings(actual);
        }

        if (expected == actual)
        {
            return new NachaGoldenFileComparisonResult(true);
        }

        var max = Math.Min(expected.Length, actual.Length);
        for (var index = 0; index < max; index++)
        {
            if (expected[index] == actual[index])
            {
                continue;
            }

            return BuildDifference(expected, actual, index, fieldSpans);
        }

        return BuildDifference(expected, actual, max, fieldSpans);
    }

    public static void ShouldMatchGoldenFile(
        string expected,
        string actual,
        NachaGoldenFileComparisonOptions? options = null,
        IReadOnlyList<NachaFieldSpan>? fieldSpans = null)
    {
        var result = Compare(expected, actual, options, fieldSpans);
        result.Matches.Should().BeTrue(result.Message);
    }

    private static NachaGoldenFileComparisonResult BuildDifference(
        string expected,
        string actual,
        int index,
        IReadOnlyList<NachaFieldSpan>? fieldSpans)
    {
        var lineNumber = (index / NachaFixedWidthAssertions.RecordLength) + 1;
        var position = (index % NachaFixedWidthAssertions.RecordLength) + 1;
        var expectedChar = index < expected.Length ? expected[index] : '\0';
        var actualChar = index < actual.Length ? actual[index] : '\0';
        var recordType = index < actual.Length ? actual[(lineNumber - 1) * NachaFixedWidthAssertions.RecordLength] : (char?)null;
        var fieldName = fieldSpans?
            .FirstOrDefault(x => x.RecordType == recordType?.ToString() && position >= x.Start && position <= x.End)
            ?.Name;

        var message = new StringBuilder()
            .Append("Golden file NACHA-M no coincide. ")
            .Append(CultureSafe($"Linea={lineNumber}, Posicion={position}, RecordType={recordType ?? '?'}, Campo={fieldName ?? "desconocido"}, "))
            .Append(CultureSafe($"Esperado='{Printable(expectedChar)}', Generado='{Printable(actualChar)}'. "))
            .Append(Context(expected, index, "Esperado"))
            .Append(' ')
            .Append(Context(actual, index, "Generado"))
            .ToString();

        return new NachaGoldenFileComparisonResult(false, message, lineNumber, position, expectedChar, actualChar, recordType, fieldName);
    }

    private static string NormalizeLineEndings(string value)
        => value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    private static string Printable(char value)
        => value == '\0' ? "<EOF>" : value.ToString();

    private static string Context(string value, int index, string label)
    {
        if (value.Length == 0)
        {
            return $"{label}Context='<empty>'.";
        }

        var start = Math.Max(0, index - 8);
        var length = Math.Min(value.Length - start, 17);
        return $"{label}Context='{value.Substring(start, length)}'.";
    }

    private static string CultureSafe(FormattableString value)
        => FormattableString.Invariant(value);
}

internal sealed record NachaFieldSpan(string RecordType, string Name, int Start, int End);
