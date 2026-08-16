using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class IncomingNachaFunctionalClassifierTests
{
    private readonly IncomingNachaFunctionalClassifier _sut = new();

    [Fact]
    public void Classify_Credit_Incoming()
    {
        var result = _sut.Classify(new EntryDetail { TransactionCode = "22", Amount = 1000 }, null);
        Assert.Equal(IncomingNachaFunctionalClass.CreditoEntrante, result.FunctionalClass);
    }

    [Fact]
    public void Classify_Debit_Incoming()
    {
        var result = _sut.Classify(new EntryDetail { TransactionCode = "27", Amount = 1000 }, null);
        Assert.Equal(IncomingNachaFunctionalClass.DebitoEntrante, result.FunctionalClass);
    }

    [Fact]
    public void Classify_Prenotification()
    {
        var result = _sut.Classify(new EntryDetail { TransactionCode = "23", Amount = 0 }, null);
        Assert.Equal(IncomingNachaFunctionalClass.Prenotificacion, result.FunctionalClass);
    }

    [Fact]
    public void Classify_Return_WithAddenda99()
    {
        var result = _sut.Classify(
            new EntryDetail { TransactionCode = "21", Amount = 0 },
            new AddendaRecord { CodeTypeAddendumRecord = "99", ReturnReasonCode = "R01", OriginalTraceNumber = "123456789012345" });

        Assert.Equal(IncomingNachaFunctionalClass.Devolucion, result.FunctionalClass);
        Assert.Equal("R01", result.ReturnReasonCode);
    }

    [Fact]
    public void Classify_ReturnEpr_WithDevReason()
    {
        var result = _sut.Classify(
            new EntryDetail { TransactionCode = "21", Amount = 0 },
            new AddendaRecord { CodeTypeAddendumRecord = "99", ReturnReasonCode = "DEV14", OriginalTraceNumber = "123456789012345" });

        Assert.Equal(IncomingNachaFunctionalClass.RetornoEpr, result.FunctionalClass);
    }

    [Theory]
    [InlineData("R60")]
    [InlineData("R74")]
    public void Classify_ReturnOfReturn_DoesNotEnterOrdinaryReturnFlow(string reason)
    {
        var result = _sut.Classify(
            new EntryDetail { TransactionCode = "21", Amount = 1000 },
            new AddendaRecord { CodeTypeAddendumRecord = "99", ReturnReasonCode = reason, OriginalTraceNumber = "123456789012345" });

        Assert.Equal(IncomingNachaFunctionalClass.Inconsistente, result.FunctionalClass);
        Assert.Equal(IncomingNachaEligibilityStatus.RevisionManual, result.EligibilityStatus);
        Assert.False(result.RequiresLink);
        Assert.True(result.RequiresManualResolution);
        Assert.Contains("ROR", result.BusinessMeaning);
    }
}
