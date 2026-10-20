using System.Globalization;
using System.Text;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// Deterministic physical NACHA-M parser. It owns byte/framing validation and fixed-width
/// record tokenization only; profile resolution, semantic validation, persistence and post-processing
/// remain with their existing owners.
/// </summary>
internal static class NachaPhysicalParser
{
    internal static async Task<NachaPhysicalFile> ParseAsync(
        Stream nachaStream,
        CancellationToken ct,
        NachaProfileRecordReader? profileReader = null)
    {
        ArgumentNullException.ThrowIfNull(nachaStream);

        using var buffer = new MemoryStream();
        await nachaStream.CopyToAsync(buffer, ct);
        var bytes = buffer.ToArray();

        if (bytes.Length == 0)
        {
            ThrowTechnical("ACHCOL-PHYSICAL-FILE-NOT-EMPTY: el archivo está vacío.");
        }

        if (bytes.AsSpan().StartsWith(Encoding.UTF8.Preamble)
            || bytes.AsSpan().StartsWith(new byte[] { 0xFF, 0xFE })
            || bytes.AsSpan().StartsWith(new byte[] { 0xFE, 0xFF }))
        {
            ThrowTechnical("ACHCOL-PHYSICAL-NO-BOM: el archivo contiene BOM.");
        }

        if (bytes.Any(value => value is (byte)'\r' or (byte)'\n'))
        {
            ThrowTechnical("ACHCOL-PHYSICAL-NO-LINE-ENDINGS: el archivo contiene CR/LF.");
        }

        if (bytes.Any(value => !IsAcceptedByte(value)))
        {
            ThrowTechnical("ACHCOL-PHYSICAL-ASCII-REPERTOIRE: el archivo contiene bytes fuera de la tabla de caracteres aceptados por ACH Colombia V35.");
        }

        var recordLength = profileReader?.RecordLength ?? AchColOfficialNachaLayout.RecordLength;
        if (bytes.Length % recordLength != 0)
        {
            ThrowTechnical("ACHCOL-PHYSICAL-RECORD-LENGTH: existen bytes residuales; cada registro debe tener 106 bytes.");
        }

        var content = Encoding.Latin1.GetString(bytes);
        var records = Enumerable.Range(0, bytes.Length / recordLength)
            .Select(index =>
            {
                var raw = content.Substring(index * recordLength, recordLength);
                return new NachaPhysicalRecord(index, raw[0], raw);
            })
            .ToArray();

        if (records[0].RecordType != '1')
        {
            ThrowTechnical("ACHCOL-T1-RECORD-TYPE: el primer registro debe ser tipo 1.");
        }

        var declaredLength = ReadField(profileReader, records[0].Content, "1", "RECORDSIZE");
        if (!int.TryParse(declaredLength, NumberStyles.None, CultureInfo.InvariantCulture, out var parsedRecordLength)
            || parsedRecordLength != recordLength)
        {
            ThrowTechnical($"ACHCOL-T1-RECORD-SIZE: el tamaño declarado debe ser {recordLength}.");
        }

        var declaredBlockingFactor = ReadField(profileReader, records[0].Content, "1", "BLOCKINGFACTOR");
        if (!int.TryParse(declaredBlockingFactor, NumberStyles.None, CultureInfo.InvariantCulture, out var blockingFactor)
            || blockingFactor <= 0
            || records.Length % blockingFactor != 0)
        {
            ThrowTechnical("ACHCOL-PHYSICAL-BLOCKING-FACTOR: el archivo no ocupa bloques exactos de 10 registros.");
        }

        return new NachaPhysicalFile(recordLength, records);
    }

    private static bool IsAcceptedByte(byte value)
        => value == (byte)' '
           || value is >= (byte)'0' and <= (byte)'9'
           || value is >= (byte)'A' and <= (byte)'Z'
           || value is >= (byte)'a' and <= (byte)'z'
           || value is (byte)'.' or (byte)',' or (byte)';' or (byte)':' or (byte)'-'
               or (byte)'*' or (byte)'/' or (byte)'&' or (byte)'#' or (byte)'$'
               or (byte)'%' or (byte)'='
           || value is 0xD1 or 0xF1;

    private static string ReadField(
        NachaProfileRecordReader? profileReader,
        string record,
        string recordCode,
        string fieldCode)
        => profileReader is not null
            ? profileReader.Read(record, recordCode, fieldCode)
            : AchColOfficialNachaLayout.Read(record, recordCode, fieldCode);

    private static void ThrowTechnical(string message)
        => throw new InvalidOperationException($"[Technical] {message}");
}

internal sealed record NachaPhysicalFile(
    int RecordLength,
    IReadOnlyList<NachaPhysicalRecord> Records);

internal sealed record NachaPhysicalRecord(
    int Index,
    char RecordType,
    string Content);
