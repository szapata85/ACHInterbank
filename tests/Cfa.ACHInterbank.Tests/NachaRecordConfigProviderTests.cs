using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

namespace Cfa.ACHInterbank.Tests;

public class NachaRecordConfigProviderTests
{
    private readonly NachaRecordConfigProvider _sut = new();

    [Fact]
    public void NachaRecordConfigProvider_ShouldResolveReturnOutConfig_ForAchRail_CurrentLayout()
    {
        var config = _sut.Resolve(7002, "ACH", NachaRecordFlow.ReturnOut, NachaRecordDirection.Outbound);

        Assert.Equal("ACH", config.RailCode);
        Assert.True(config.IsCurrentLayout);
        Assert.Equal("000101006", config.Record1.ImmediateDestination);
        Assert.Equal("DEVOLUCIONES", config.Record5.CompanyName);
    }

    [Fact]
    public void NachaRecordConfigProvider_ShouldResolveReturnOutConfig_ForCenitRail_CurrentLayout()
    {
        var config = _sut.Resolve(7001, "CENIT", NachaRecordFlow.ReturnOut, NachaRecordDirection.Outbound);

        Assert.Equal("CENIT", config.RailCode);
        Assert.True(config.IsCurrentLayout);
        Assert.Equal("BANCORET", config.Record5.CompanyIdentification);
    }

    [Fact]
    public void NachaRecordConfigProvider_ShouldResolveReturnOfReturnOutConfig_ForAchRail_CurrentLayout()
    {
        var config = _sut.Resolve(7002, "ACH", NachaRecordFlow.ReturnOfReturnOut, NachaRecordDirection.Outbound);

        Assert.Equal("ACH", config.RailCode);
        Assert.Equal("ACHINTERBANK ROR", config.Record1.ImmediateOriginName);
        Assert.Equal("BANCROR", config.Record5.CompanyIdentification);
        Assert.Equal("RETORNO", config.Record5.CompanyEntryDescription);
    }

    [Fact]
    public void NachaRecordConfigProvider_ShouldResolveReturnOfReturnOutConfig_ForCenitRail_CurrentLayout()
    {
        var config = _sut.Resolve(7001, "CENIT", NachaRecordFlow.ReturnOfReturnOut, NachaRecordDirection.Outbound);

        Assert.Equal("CENIT", config.RailCode);
        Assert.Equal("DEV. DEV.", config.Record5.CompanyName);
        Assert.Equal("0000001", config.Record89.BatchNumber);
    }

    [Fact]
    public void NachaRecordConfigProvider_ShouldFallbackToCurrentLayout_WhenRailUnknown()
    {
        var config = _sut.Resolve(9999, "OTHER", NachaRecordFlow.ReturnOut, NachaRecordDirection.Outbound);

        Assert.Equal("UNKNOWN", config.RailCode);
        Assert.True(config.IsCurrentLayout);
        Assert.Equal("A", config.Record1.FileIdModifier);
    }

    [Fact]
    public void NachaRecordConfigProvider_ShouldNotMarkCurrentLayoutAsProductiveApproved()
    {
        var config = _sut.Resolve(7002, "ACH", NachaRecordFlow.ReturnOut, NachaRecordDirection.Outbound);

        Assert.True(config.IsCurrentLayout);
        Assert.False(config.IsProductiveApproved);
    }
}
