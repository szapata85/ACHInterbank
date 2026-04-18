using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class NachaType7AliasMapTests
{
    [Theory]
    [InlineData("codigoTransaccion", "TransactionCode")]
    [InlineData("NumeroTraceOriginal", "OriginalTraceNumber")]
    [InlineData("ReceiverCustomerCode", "ReceiverCustomerCode")]
    public void GetCanonicalKey_ShouldResolveAlias(string alias, string expectedCanonical)
    {
        var map = new NachaType7AliasMap();

        var canonical = map.GetCanonicalKey(alias);

        Assert.Equal(expectedCanonical, canonical);
    }

    [Fact]
    public void GetAliases_ShouldExposeCanonicalAndLegacyAliases()
    {
        var map = new NachaType7AliasMap();

        var aliases = map.GetAliases("SequenceNumber");

        Assert.Contains("SequenceNumber", aliases);
        Assert.Contains("AddendaSequence", aliases);
        Assert.Contains("SecuenciaAddenda", aliases);
    }
}
