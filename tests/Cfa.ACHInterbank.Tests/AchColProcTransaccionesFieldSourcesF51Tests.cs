using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class AchColProcTransaccionesFieldSourcesF51Tests
{
    [Fact]
    public void Inventory_ContainsAll25InputParameters_FromTechnicalBootstrapCatalog()
    {
        var expected = new[]
        {
            "TREG", "TIPTRAN", "BCORECEP", "BCOORIG", "NORIG", "NCTAORIG", "IDORIG",
            "DESTRAN", "FECEFEC", "NCTARECEP", "MONTO", "NRECEP", "IDRECEP", "DISCRE",
            "CONV", "PROD", "INFPAG", "IDTRAN", "IDLOTE", "REGLOTE", "IREVER", "LIBRE",
            "IDCAMCOMPE", "DIRECCIONIP", "LIBRE1"
        };
        Assert.Equal(expected.Length, AchColProcTransaccionesFieldSources.Fields.Count);
        Assert.All(expected, key => Assert.True(AchColProcTransaccionesFieldSources.Fields.ContainsKey(key), key));
    }

    [Theory]
    [InlineData("TIPTRAN", "EntryDetail.TransactionCode")]
    [InlineData("MONTO", "EntryDetail.Amount")]
    [InlineData("NCTARECEP", "EntryDetail.AccountNumber")]
    public void DirectCandidate_IsEntryBased_NotCycleId(string parameter, string source)
    {
        var field = AchColProcTransaccionesFieldSources.Get(parameter);
        Assert.Equal(AchColProcTransaccionesFieldSources.DirectCandidate, field.Status);
        Assert.Equal(source, field.CandidateSource);
        Assert.DoesNotContain("cycle.", field.CandidateSource!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ExternalCycleAndInternalCycle_AreNotSoapSourceAssumptions()
    {
        Assert.All(AchColProcTransaccionesFieldSources.Fields.Values, field =>
        {
            Assert.DoesNotContain("ExternalCycleNumber", field.CandidateSource ?? "", StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("AchCycleId", field.CandidateSource ?? "", StringComparison.OrdinalIgnoreCase);
        });
        Assert.Equal(AchColProcTransaccionesFieldSources.Conditional,
            AchColProcTransaccionesFieldSources.Get("IDCAMCOMPE").Status);
    }

    [Fact]
    public void UnapprovedOrIncompleteMappings_RemainBlocked()
    {
        Assert.Throws<InvalidOperationException>(() =>
            AchColProcTransaccionesFieldSources.RequireHomologatedMapping(externallyApproved: false));
        Assert.Throws<InvalidOperationException>(() =>
            AchColProcTransaccionesFieldSources.RequireHomologatedMapping(externallyApproved: true));
        Assert.Throws<InvalidOperationException>(() =>
            AchColProcTransaccionesFieldSources.Get("NONSENSE"));
    }

    [Fact]
    public void LegacyQueueSourceKind_RemainsDistinctFromFileBased()
    {
        Assert.NotEqual(IncomingNachaDispatchSourceKinds.CycleBound,
            IncomingNachaDispatchSourceKinds.AchColFileBased);
    }
}
