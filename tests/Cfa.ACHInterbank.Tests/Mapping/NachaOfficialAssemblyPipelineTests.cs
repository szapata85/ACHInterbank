using System.Globalization;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using FluentAssertions;
using Xunit;

namespace Cfa.ACHInterbank.Tests.Mapping;

public class NachaOfficialAssemblyPipelineTests
{
    [Fact]
    public void Assemble_ShouldPreserveOfficialOrderAssociationControlTotalsAndPadding()
    {
        var (content, audit, renderedCount) = Assemble();

        content.Length.Should().Be(10 * 12);
        Enumerable.Range(0, 10)
            .Select(index => content[index * 12])
            .Should().Equal('1', '5', '6', '7', '8', '9', '9', '9', '9', '9');
        renderedCount.Should().Be(10);
        audit.Trace.Should().ContainSingle(item =>
            item == "FileIntegrity:BatchCount=1;EntryAddendaCount=2;MonetaryTotals=REDACTED;BlockCount=1;PaddingNeeded=4");
        audit.FieldTraceEntries.Count(item => item.FieldName == "PADDING_RECORD").Should().Be(4);
    }

    [Fact]
    public void Assemble_ShouldBeByteDeterministicForSameResolvedInputs()
    {
        var first = Assemble().Content;
        var second = Assemble().Content;

        second.Should().Be(first);
    }

    private static (string Content, NachaGenerationAuditResult Audit, int RenderedCount) Assemble()
    {
        var audit = new NachaGenerationAuditResult();
        var fileTotals = new NachaFileControlTotals
        {
            BatchCount = 1,
            BlockCount = 1,
            EntryAddendaCount = 2,
            EntryHash = 3,
            TotalDebitAmountInCents = 4,
            TotalCreditAmountInCents = 5,
            PhysicalRecordCountBeforePadding = 6,
            PaddingRecordCount = 4,
            PhysicalRecordCountAfterPadding = 10
        };
        var batchTotals = new NachaBatchControlTotals
        {
            BatchId = 10,
            EntryDetailCount = 1,
            AddendaCount = 1,
            EntryAddendaCount = 2,
            EntryHash = 3,
            TotalDebitAmountInCents = 4,
            TotalCreditAmountInCents = 5
        };
        var pipeline = new NachaOfficialAssemblyPipeline(
            new TraceAwareStubRenderer(),
            audit,
            new NachaOfficialRecordRenderContext("IGNORED", "IGNORED", "IGNORED"),
            fileTotals);

        pipeline.AppendFileHeader(Item("1"));
        pipeline.BeginBatch(Item("5"));
        pipeline.BeginEntry(Item("6"));
        pipeline.AppendAddenda(Item("7"));
        pipeline.EndEntry();
        pipeline.EndBatch(
            Item("8", new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["ENTRYADDENDACOUNT"] = 2,
                ["ENTRYHASH"] = 3,
                ["TOTALDEBITAMOUNT"] = 4,
                ["TOTALCREDITAMOUNT"] = 5
            }, BuildControlLayout("8", "ENTRYADDENDACOUNT", "ENTRYHASH", "TOTALDEBITAMOUNT", "TOTALCREDITAMOUNT")),
            batchTotals);

        var content = pipeline.Complete(Item(
            "9",
            new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["BATCHCOUNT"] = 1,
                ["BLOCKCOUNT"] = 1,
                ["ENTRYADDENDACOUNT"] = 2,
                ["ENTRYHASH"] = 3,
                ["TOTALDEBITAMOUNT"] = 4,
                ["TOTALCREDITAMOUNT"] = 5
            },
            BuildControlLayout("9", "BATCHCOUNT", "BLOCKCOUNT", "ENTRYADDENDACOUNT", "ENTRYHASH", "TOTALDEBITAMOUNT", "TOTALCREDITAMOUNT")));

        return (content, audit, pipeline.RenderedRecordCount);
    }

    private static NachaOfficialAssemblyRecord Item(
        string recordCode,
        object? value = null,
        CfgLayoutVariant? layout = null)
        => new(recordCode, value ?? new object(), layout ?? BuildLayout(recordCode));

    private static CfgLayoutVariant BuildLayout(string recordCode)
        => new()
        {
            VariantCode = $"R4C-{recordCode}",
            TotalLength = 12,
            RecordCode = new CatRecordCode { Code = recordCode },
            Fields = []
        };

    private static CfgLayoutVariant BuildControlLayout(string recordCode, params string[] fieldCodes)
    {
        var layout = BuildLayout(recordCode);
        layout.Fields = fieldCodes.Select((fieldCode, index) => new CfgLayoutField
        {
            LayoutVariant = layout,
            FieldCode = fieldCode,
            FieldNameEs = fieldCode,
            StartPosition = (index * 2) + 1,
            Length = 2,
            Justification = 'R',
            PadChar = '0',
            IsEnabled = true
        }).ToList();
        return layout;
    }

    private sealed class TraceAwareStubRenderer : INachaOfficialRecordRenderer
    {
        public string Render(
            string recordCode,
            object record,
            CfgLayoutVariant layout,
            NachaGenerationAuditResult audit,
            NachaOfficialRecordRenderContext renderContext,
            int recordSequence,
            int lineNumber)
        {
            if (record is IReadOnlyDictionary<string, object?> values)
            {
                foreach (var field in layout.Fields.Where(field => field.IsEnabled))
                {
                    var raw = Convert.ToInt64(values[field.FieldCode], CultureInfo.InvariantCulture);
                    var rendered = raw.ToString(CultureInfo.InvariantCulture).PadLeft(field.Length, field.PadChar);
                    audit.FieldTraceEntries.Add(new NachaGenerationTraceEntry
                    {
                        RecordType = recordCode,
                        RecordSequence = recordSequence,
                        LineNumber = lineNumber,
                        FieldName = field.FieldCode,
                        RuntimeRenderedValue = rendered
                    });
                }
            }

            return new string(recordCode[0], layout.TotalLength);
        }
    }
}
