using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class AchColombiaManagedMftInboundNameTests
{
    [Theory]
    [InlineData("0001283.008.20260731.8.OUT")]
    [InlineData("0001283.009.20260731.9.out")]
    [InlineData("0001283.008.20260731.8.OUT.env")]
    [InlineData("0001283.009.20260731.9.out.ENV")]
    [InlineData("previous.RET.env")]
    public void AllowedFinalExtensions_AreClaimable(string physicalName)
        => Assert.True(AchColombiaManagedMftImplementationSupport.IsAuthorizedInboundFileName(physicalName));

    [Theory]
    [InlineData("intermediate.env")]
    [InlineData("partial.OUT.tmp")]
    [InlineData("intermediate.RET")]
    [InlineData("intermediate.OUT.env.tmp")]
    [InlineData("plain.OUT.env.env")]
    public void OtherExtensions_AreNotClaimable(string physicalName)
        => Assert.False(AchColombiaManagedMftImplementationSupport.IsAuthorizedInboundFileName(physicalName));
}
