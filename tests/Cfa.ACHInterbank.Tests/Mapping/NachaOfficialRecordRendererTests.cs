using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using FluentAssertions;
using Xunit;

namespace Cfa.ACHInterbank.Tests.Mapping;

public class NachaOfficialRecordRendererTests
{
    [Fact]
    public void Render_ShouldPreserveConfiguredFieldOrderWidthPaddingAndTrace()
    {
        var renderer = new NachaOfficialRecordRenderer();
        var layout = BuildLayout();
        var audit = new NachaGenerationAuditResult { ClearingHouseCode = "ACH" };
        var renderContext = new NachaOfficialRecordRenderContext("ACHCOL", "ACHCOL", "ACHCOL");

        var line = renderer.Render("1", new object(), layout, audit, renderContext, 1, 1);

        line.Should().Be("1ABC  ");
        line.Should().HaveLength(6);
        audit.FieldTraceEntries.Should().HaveCount(2);
        audit.FieldTraceEntries.Select(entry => entry.FieldName).Should().Equal("RECORDTYPE", "NAME");
        audit.FieldTraceEntries.Should().OnlyContain(entry => entry.ValidationStatus == "Ok");
    }

    [Fact]
    public void Render_ShouldBeByteDeterministicForSameInputs()
    {
        var renderer = new NachaOfficialRecordRenderer();
        var layout = BuildLayout();
        var renderContext = new NachaOfficialRecordRenderContext("ACHCOL", "ACHCOL", "ACHCOL");

        var first = renderer.Render("1", new object(), layout, new NachaGenerationAuditResult(), renderContext, 1, 1);
        var second = renderer.Render("1", new object(), layout, new NachaGenerationAuditResult(), renderContext, 1, 1);

        second.Should().Be(first);
    }

    private static CfgLayoutVariant BuildLayout()
    {
        var layout = new CfgLayoutVariant
        {
            Id = 10,
            VariantCode = "R4B_TEST",
            TotalLength = 6,
            RecordCode = new CatRecordCode { Code = "1" }
        };

        layout.Fields =
        [
            BuildConstantField(layout, "RECORDTYPE", 1, 1, "1", "NUMERIC", normalizer: "NONE"),
            BuildConstantField(layout, "NAME", 2, 5, "abc", "ALPHANUMERIC", normalizer: "UPPER")
        ];

        return layout;
    }

    private static CfgLayoutField BuildConstantField(
        CfgLayoutVariant layout,
        string fieldCode,
        int startPosition,
        int length,
        string value,
        string dataType,
        string normalizer)
    {
        var field = new CfgLayoutField
        {
            LayoutVariant = layout,
            FieldCode = fieldCode,
            FieldNameEs = fieldCode,
            StartPosition = startPosition,
            Length = length,
            PadChar = ' ',
            Justification = 'L',
            IsEnabled = true,
            SourceDefinition = new CfgFieldSourceDefinition
            {
                ConstantValue = value,
                DataSourceType = new CatDataSourceType { Code = "CONSTANTE" }
            }
        };

        field.Rules =
        [
            new CfgFieldRule
            {
                LayoutField = field,
                RuleCode = $"R4B-{fieldCode}",
                IsEnabled = true,
                Order = 1,
                RuleConfigJson = $$"""
                    {
                      "recordType": "1",
                      "field": "{{fieldCode}}",
                      "startPosition": {{startPosition}},
                      "length": {{length}},
                      "dataType": "{{dataType}}",
                      "required": true,
                      "normalizer": "{{normalizer}}",
                      "overflowPolicy": "REJECT"
                    }
                    """
            }
        ];

        return field;
    }
}
