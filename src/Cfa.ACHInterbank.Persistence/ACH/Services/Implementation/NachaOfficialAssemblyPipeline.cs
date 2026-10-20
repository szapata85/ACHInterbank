using System.Globalization;
using System.Text;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// Deterministic, side-effect-free assembly session for an already-resolved official NACHA-M file.
/// The facade remains responsible for profile/PUBLISH selection, rail policy, record preparation,
/// persistence, hashing and semantic validation. This component only owns physical record composition,
/// structural T6/T7 grouping, rendered control-total verification and final blocking/padding.
/// </summary>
internal sealed class NachaOfficialAssemblyPipeline
{
    private readonly INachaOfficialRecordRenderer _renderer;
    private readonly NachaGenerationAuditResult _audit;
    private readonly NachaOfficialRecordRenderContext _renderContext;
    private readonly NachaFileControlTotals _fileTotals;
    private readonly StringBuilder _content;
    private int _lineNumber = 1;
    private int _recordCount;
    private AssemblyState _state = AssemblyState.NotStarted;

    public NachaOfficialAssemblyPipeline(
        INachaOfficialRecordRenderer renderer,
        NachaGenerationAuditResult audit,
        NachaOfficialRecordRenderContext renderContext,
        NachaFileControlTotals fileTotals)
    {
        _renderer = renderer;
        _audit = audit;
        _renderContext = renderContext;
        _fileTotals = fileTotals;
        _content = new StringBuilder();
    }

    public int RenderedRecordCount => _lineNumber - 1;

    public void AppendFileHeader(NachaOfficialAssemblyRecord header)
    {
        RequireState(AssemblyState.NotStarted, "El encabezado T1 debe iniciar el assembly oficial.");
        RequireRecordCode(header, "1");
        AppendRecord(header);
        _state = AssemblyState.FileOpen;
    }

    public void BeginBatch(NachaOfficialAssemblyRecord batchHeader)
    {
        RequireState(AssemblyState.FileOpen, "No se puede iniciar un lote mientras otro lote está abierto o el archivo no inició.");
        RequireRecordCode(batchHeader, "5");
        AppendRecord(batchHeader);
        _state = AssemblyState.BatchOpen;
    }

    public void BeginEntry(NachaOfficialAssemblyRecord entryDetail)
    {
        RequireState(AssemblyState.BatchOpen, "T6 solo puede ensamblarse dentro de un lote abierto.");
        RequireRecordCode(entryDetail, "6");
        AppendRecord(entryDetail);
        _state = AssemblyState.EntryOpen;
    }

    public void AppendAddenda(NachaOfficialAssemblyRecord addenda)
    {
        RequireState(AssemblyState.EntryOpen, "T7 requiere un T6 asociado abierto.");
        RequireRecordCode(addenda, "7");
        AppendRecord(addenda);
    }

    public void EndEntry()
    {
        RequireState(AssemblyState.EntryOpen, "No existe un T6 abierto para cerrar.");
        _state = AssemblyState.BatchOpen;
    }

    public void EndBatch(
        NachaOfficialAssemblyRecord batchControl,
        NachaBatchControlTotals expectedTotals)
    {
        RequireState(AssemblyState.BatchOpen, "T8 requiere un lote abierto.");
        RequireRecordCode(batchControl, "8");
        var controlLine = _lineNumber;
        AppendRecord(batchControl);
        ValidateRenderedControlTotals(_audit, "8", controlLine, batchControl.Layout, expectedTotals);
        _state = AssemblyState.FileOpen;
    }

    public string Complete(NachaOfficialAssemblyRecord fileControl)
    {
        RequireState(AssemblyState.FileOpen, "T9 requiere un archivo iniciado y ningún lote abierto.");
        RequireRecordCode(fileControl, "9");

        if (_recordCount + 1 != _fileTotals.PhysicalRecordCountBeforePadding)
        {
            throw new NachaGenerationException(
                "NACHA_RECORD_COUNT_MISMATCH",
                $"El conteo físico calculado {_fileTotals.PhysicalRecordCountBeforePadding} no coincide con el render previo al control {_recordCount + 1}.");
        }

        _audit.Trace.Add($"FileIntegrity:BatchCount={_fileTotals.BatchCount};EntryAddendaCount={_fileTotals.EntryAddendaCount};MonetaryTotals=REDACTED;BlockCount={_fileTotals.BlockCount};PaddingNeeded={_fileTotals.PaddingRecordCount}");
        var fileControlLine = _lineNumber;
        AppendRecord(fileControl);
        ValidateRenderedControlTotals(_audit, "9", fileControlLine, fileControl.Layout, _fileTotals);

        AppendPadding(fileControl.Layout.TotalLength);
        _state = AssemblyState.Completed;
        return _content.ToString();
    }

    private void AppendRecord(NachaOfficialAssemblyRecord record)
    {
        _content.Append(_renderer.Render(
            record.RecordCode,
            record.Value,
            record.Layout,
            _audit,
            _renderContext,
            recordSequence: 1,
            lineNumber: _lineNumber));
        _lineNumber++;
        _recordCount++;
    }

    private void AppendPadding(int recordLength)
    {
        if (_fileTotals.PaddingRecordCount <= 0)
        {
            return;
        }

        var paddingRecord = NachaFileBlocking.BuildPaddingRecord(recordLength);
        for (var i = 0; i < _fileTotals.PaddingRecordCount; i++)
        {
            _content.Append(paddingRecord);
            _audit.FieldTraceEntries.Add(new NachaGenerationTraceEntry
            {
                TraceId = _audit.TraceId,
                RecordType = "9",
                RecordSequence = i + 1,
                LineNumber = _lineNumber++,
                FieldName = "PADDING_RECORD",
                DisplayName = "Padding record",
                PositionStart = 1,
                PositionEnd = recordLength,
                Length = recordLength,
                SourceType = "CALCULATED",
                CalculationType = "PaddingRecord",
                RawValueSanitized = "9",
                RenderedValue = "[PADDING;Length=106]",
                RuntimeRenderedValue = paddingRecord,
                RenderedLength = recordLength,
                ValidationStatus = "Ok",
                GeneratedLinePreviewSanitized = $"RecordType=9;Length={recordLength};Category=PADDING",
                ValueStartIndex = 1,
                ValueEndIndex = recordLength
            });
        }
    }

    private static void ValidateRenderedControlTotals(
        NachaGenerationAuditResult audit,
        string recordCode,
        int lineNumber,
        CfgLayoutVariant layout,
        NachaBatchControlTotals totals)
    {
        ValidateRenderedControlField(audit, recordCode, lineNumber, layout, "ENTRYADDENDACOUNT", totals.EntryAddendaCount);
        ValidateRenderedControlField(audit, recordCode, lineNumber, layout, "ENTRYHASH", totals.EntryHash);
        ValidateRenderedControlField(audit, recordCode, lineNumber, layout, "TOTALDEBITAMOUNT", totals.TotalDebitAmountInCents);
        ValidateRenderedControlField(audit, recordCode, lineNumber, layout, "TOTALCREDITAMOUNT", totals.TotalCreditAmountInCents);
    }

    private static void ValidateRenderedControlTotals(
        NachaGenerationAuditResult audit,
        string recordCode,
        int lineNumber,
        CfgLayoutVariant layout,
        NachaFileControlTotals totals)
    {
        ValidateRenderedControlField(audit, recordCode, lineNumber, layout, "BATCHCOUNT", totals.BatchCount);
        ValidateRenderedControlField(audit, recordCode, lineNumber, layout, "BLOCKCOUNT", totals.BlockCount);
        ValidateRenderedControlField(audit, recordCode, lineNumber, layout, "ENTRYADDENDACOUNT", totals.EntryAddendaCount);
        ValidateRenderedControlField(audit, recordCode, lineNumber, layout, "ENTRYHASH", totals.EntryHash);
        ValidateRenderedControlField(audit, recordCode, lineNumber, layout, "TOTALDEBITAMOUNT", totals.TotalDebitAmountInCents);
        ValidateRenderedControlField(audit, recordCode, lineNumber, layout, "TOTALCREDITAMOUNT", totals.TotalCreditAmountInCents);
    }

    private static void ValidateRenderedControlField(
        NachaGenerationAuditResult audit,
        string recordCode,
        int lineNumber,
        CfgLayoutVariant layout,
        string fieldCode,
        long calculatedValue)
    {
        var field = layout.Fields.First(x => x.IsEnabled && string.Equals(x.FieldCode, fieldCode, StringComparison.OrdinalIgnoreCase));
        var expected = calculatedValue.ToString(CultureInfo.InvariantCulture);
        expected = field.Justification == 'R'
            ? expected.PadLeft(field.Length, field.PadChar)
            : expected.PadRight(field.Length, field.PadChar);
        var trace = audit.FieldTraceEntries.LastOrDefault(x =>
            x.LineNumber == lineNumber
            && string.Equals(x.RecordType, recordCode, StringComparison.OrdinalIgnoreCase)
            && string.Equals(x.FieldName, fieldCode, StringComparison.OrdinalIgnoreCase));

        if (trace is null || !string.Equals(trace.RuntimeRenderedValue, expected, StringComparison.Ordinal))
        {
            throw new NachaGenerationException(
                "NACHA_CONTROL_TOTAL_MISMATCH",
                $"El valor renderizado de {recordCode}.{fieldCode} no coincide con el calculado.");
        }
    }

    private void RequireState(AssemblyState expected, string message)
    {
        if (_state != expected)
        {
            throw new InvalidOperationException(message);
        }
    }

    private static void RequireRecordCode(NachaOfficialAssemblyRecord record, string expected)
    {
        if (!string.Equals(record.RecordCode, expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"El pipeline esperaba registro T{expected} y recibió T{record.RecordCode}.");
        }
    }

    private enum AssemblyState
    {
        NotStarted,
        FileOpen,
        BatchOpen,
        EntryOpen,
        Completed
    }
}

internal sealed record NachaOfficialAssemblyRecord(
    string RecordCode,
    object Value,
    CfgLayoutVariant Layout);
