using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

namespace Cfa.ACHInterbank.Tests;

public class NachaRecordFieldValidatorTests
{
    [Fact]
    public void NachaRecordFieldValidator_ShouldHardBlock_WhenType1ControlIsA094106()
    {
        var provider = new NachaRecordConfigProvider();
        var cfg = provider.Resolve(7002, "ACH", NachaRecordFlow.ReturnOut, NachaRecordDirection.Outbound);
        var content = ("1" + new string(' ', 105)).Replace("1", "1A094106") + new string('5',106)+new string('8',106)+new string('9',106);
        var sut = new NachaRecordFieldValidator();
        var r = sut.Validate(new NachaRecordValidationContext(7002, "ACH", NachaRecordFlow.ReturnOut, NachaRecordDirection.Outbound, cfg, content));
        Assert.True(r.HasErrors);
    }

    [Fact]
    public void NachaRecordFieldValidator_ShouldWarn_WhenCenitCurrentLayoutUsesAchCharacterizedValues()
    {
        var provider = new NachaRecordConfigProvider();
        var cfg = provider.Resolve(7001, "CENIT", NachaRecordFlow.ReturnOfReturnOut, NachaRecordDirection.Outbound);
        var content = new string('1',106)+new string('5',106)+new string('6',106)+new string('7',106)+new string('8',106)+new string('9',106);
        var sut = new NachaRecordFieldValidator();
        var r = sut.Validate(new NachaRecordValidationContext(7001, "CENIT", NachaRecordFlow.ReturnOfReturnOut, NachaRecordDirection.Outbound, cfg, content));
        Assert.Contains(r.Issues, x => x.Severity == NachaRecordValidationSeverity.Warning);
    }
}
