using FluentAssertions;

namespace Cfa.ACHInterbank.Tests.NachaFunctional;

internal static class NachaFixedWidthAssertions
{
    public const int RecordLength = 106;
    public const int BlockingFactor = 10;
    private static readonly HashSet<char> ValidRecordTypes = ['1', '5', '6', '7', '8', '9'];

    public static IReadOnlyList<string> SplitRecords(string content)
    {
        content.Length.Should().BeGreaterThan(0, "el archivo NACHA-M no debe estar vacio");
        (content.Length % RecordLength).Should().Be(0, "cada registro NACHA-M debe medir 106 caracteres");

        return Enumerable.Range(0, content.Length / RecordLength)
            .Select(index => content.Substring(index * RecordLength, RecordLength))
            .ToList();
    }

    public static void ShouldHaveValidFixedWidthStructure(string content)
    {
        var records = SplitRecords(content);
        records.Should().OnlyContain(x => x.Length == RecordLength, "todos los registros deben medir 106 caracteres");
        records.Should().NotContain(string.Empty, "no debe haber registros vacios");
        records.Should().OnlyContain(x => ValidRecordTypes.Contains(x[0]), "solo son validos los registros 1,5,6,7,8,9");
        records.Should().OnlyContain(x => x.All(IsAllowedCharacter), "NACHA-M funcional solo admite caracteres imprimibles ASCII en fixtures anonimizados");
    }

    public static void FileShouldHaveValidFixedWidthStructure(string path)
    {
        File.Exists(path).Should().BeTrue($"el snapshot NACHA-M requerido no existe: {path}");
        var content = File.ReadAllText(path);
        content.Should().NotBeEmpty($"el snapshot NACHA-M requerido esta vacio: {path}");
        ShouldHaveValidFixedWidthStructure(content);
    }

    public static void ShouldHaveValidPadding(string content, int blockingFactor = BlockingFactor)
    {
        var records = SplitRecords(content);
        (records.Count % blockingFactor).Should().Be(0, "la cantidad final de registros debe ser multiplo del blocking factor");

        var firstPaddingIndex = records.ToList().FindIndex(IsPaddingRecord);
        if (firstPaddingIndex < 0)
        {
            return;
        }

        records.Skip(firstPaddingIndex).Should().OnlyContain(record => IsPaddingRecord(record), "los registros de padding deben estar al final");
        records.Take(firstPaddingIndex).Should().NotContain(record => IsPaddingRecord(record), "no debe existir padding intermedio");
        records.Take(firstPaddingIndex).Should().Contain(record => record[0] == '9', "debe existir FileControl antes del padding");
    }

    public static void ShouldEndWithValidFileControlBeforePadding(string content)
    {
        var records = SplitRecords(content);
        var firstPaddingIndex = records.ToList().FindIndex(IsPaddingRecord);
        var lastBusinessRecord = firstPaddingIndex < 0 ? records[^1] : records[firstPaddingIndex - 1];
        lastBusinessRecord[0].Should().Be('9', "el ultimo registro de negocio antes del padding debe ser FileControl");
        lastBusinessRecord.Should().NotBe(new string('9', RecordLength), "FileControl no debe confundirse con padding");
    }

    public static void ShouldRejectInvalidRecordLength(string content)
    {
        Action act = () => SplitRecords(content);
        act.Should().Throw<Exception>().WithMessage("*106*");
    }

    public static bool IsPaddingRecord(string record)
        => record == new string('9', RecordLength);

    private static bool IsAllowedCharacter(char value)
        => value is >= ' ' and <= '~';
}
