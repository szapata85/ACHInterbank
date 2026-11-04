using System.Text;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

namespace Cfa.ACHInterbank.Tests;

/// <summary>
/// F07: regressions crossing the F04 return rules and F06 physical/error domain
/// with ACH Colombia. These are executable xUnit cases, not operator homologation.
/// </summary>
public sealed class CenitF07CrossPhaseRegressionTests
{
    private static readonly DateTime ValueDate = new(2026, 8, 10);

    [Theory]
    [InlineData(0xD1)]
    [InlineData(0xF1)]
    public async Task PhysicalProfileIsolation_AchColombiaAcceptedBeforeAndAfterCenitRejection(int enye)
    {
        var chars = new string(' ', 106).ToCharArray();
        chars[0] = '1';
        "106".CopyTo(0, chars, 36, 3);
        "10".CopyTo(0, chars, 39, 2);
        var first = new string(chars);
        var payload = first + new string('9', 106 * 9);
        var bytes = Encoding.Latin1.GetBytes(payload);
        bytes[58] = (byte)enye;

        async Task<NachaPhysicalFile> Parse(bool cenit)
        {
            await using var stream = new MemoryStream(bytes);
            return await NachaPhysicalParser.ParseAsync(stream, CancellationToken.None,
                isCenitNachaProfile: cenit);
        }

        Assert.Equal((char)enye, (await Parse(false)).Records[0].Content[58]);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Parse(true));
        Assert.Contains("CENIT-NACHA-FATAL ERR_INV_PATTERN", ex.Message, StringComparison.Ordinal);
        Assert.Equal((char)enye, (await Parse(false)).Records[0].Content[58]);
    }

    [Fact]
    public void ValidationDomainIsolation_CenitStaCodesDoNotLeakToAchColombiaAndBack()
    {
        var catalog = new Dictionary<string, AchFileRejectionCode>
        {
            ["D04"] = new() { Code = "D04", Description = "Codigo ACH heredado", IsActive = true }
        };
        var validator = new NachaInboundValidator(catalog);
        validator.SelectNachaErrorDomain(true);
        var cenit = Assert.Throws<InvalidOperationException>(() => validator.ThrowRegulatory("D04", "Conteo invalido."));
        Assert.Contains("CENIT-NACHA-FATAL", cenit.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("D04", cenit.Message, StringComparison.Ordinal);

        validator.SelectNachaErrorDomain(false);
        var ach = Assert.Throws<InvalidOperationException>(() => validator.ThrowRegulatory("D04", "Conteo invalido."));
        Assert.Contains("D04", ach.Message, StringComparison.Ordinal);
        Assert.Contains("Codigo ACH heredado", ach.Message, StringComparison.Ordinal);

        validator.SelectNachaErrorDomain(true);
        var cenitAgain = Assert.Throws<InvalidOperationException>(() => validator.ThrowRegulatory("D04", "Conteo invalido."));
        Assert.DoesNotContain("D04", cenitAgain.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(1, "CENIT_CYCLE5_R06_ORIGIN_C4_REQUIRED")]
    [InlineData(2, "CENIT_CYCLE5_R06_ORIGIN_C4_REQUIRED")]
    [InlineData(3, "CENIT_CYCLE5_R06_ORIGIN_C4_REQUIRED")]
    [InlineData(4, "CENIT_R06_ALLOWED")]
    public void ReturnPolicy_R06FifthCycleIsGatedBeforeLiquidityEvidence(int sourceCycle, string expected)
    {
        var request = new CenitIncomingReturnPolicyRequest(
            TransactionTypeEnum.Credit, "R06", ValueDate, ValueDate, sourceCycle, 5, 5,
            100m, 100m, ReturnRequestDate: ValueDate,
            ImmediateReturnCycleConfirmed: true, FundsAvailabilityRequired: true,
            FundsAvailabilityConfirmed: true, ConfirmationToOriginatorRecorded: true);
        var result = new CenitIncomingReturnPolicy().Evaluate(request);
        Assert.Equal(expected, result.Code);
    }

    [Fact]
    public void ReturnPolicy_R23SameDayCannotUsePriorDayExceptionOrExternalDeadline()
    {
        var request = new CenitIncomingReturnPolicyRequest(
            TransactionTypeEnum.Credit, "R23", ValueDate, ValueDate, 1, 3, 5,
            100m, 100m, ReturnRequestDate: ValueDate,
            ImmediateReturnCycleConfirmed: true, ReceiverRejectionDeadlineDate: ValueDate.AddDays(90));
        var result = new CenitIncomingReturnPolicy().Evaluate(request);
        Assert.False(result.IsAllowed);
        Assert.Equal("CENIT_R23_SAME_DAY_NOT_NEXT_CYCLE", result.Code);
    }

    [Fact]
    public void ReturnPolicy_StaRejectionCannotBecomeMonetaryReturn()
    {
        var request = new CenitIncomingReturnPolicyRequest(
            TransactionTypeEnum.Credit, "D04", ValueDate, ValueDate, 1, 2, 5, 100m, 100m);
        var result = new CenitIncomingReturnPolicy().Evaluate(request);
        Assert.False(result.IsAllowed);
        Assert.Equal("CENIT_FILE_REJECTION_NOT_RETURN", result.Code);
    }
}
