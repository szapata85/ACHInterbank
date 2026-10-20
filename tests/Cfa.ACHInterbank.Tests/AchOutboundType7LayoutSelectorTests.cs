using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using FluentAssertions;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public class AchOutboundType7LayoutSelectorTests
{
    [Fact]
    public void Select_ShouldUseRenamedVariantByPredicateWithoutVariantCodeSemantics()
    {
        var renamed = Variant("SYNTHETIC_RENAMED_ID", predicate: """{"BusinessType":"DEBIT"}""");

        var selected = AchOutboundType7LayoutSelector.Select(
            [Default("UNRELATED_DEFAULT"), renamed],
            AchAddendaBusinessType.Debit,
            isPrenotification: false);

        selected.Should().BeSameAs(renamed);
    }

    [Fact]
    public void Select_OriginalCreditMonetary_ShouldUseSingleDefault()
    {
        var defaultVariant = Default("ANY_ID");

        var selected = AchOutboundType7LayoutSelector.Select(
            [defaultVariant, Debit(), CreditPrenote()],
            AchAddendaBusinessType.Credit,
            isPrenotification: false);

        selected.Should().BeSameAs(defaultVariant);
    }

    [Fact]
    public void Select_OriginalCreditPrenotification_ShouldUseTwoKeyPredicate()
    {
        var creditPrenote = CreditPrenote();

        var selected = AchOutboundType7LayoutSelector.Select(
            [Default(), Debit(), creditPrenote],
            AchAddendaBusinessType.Credit,
            isPrenotification: true);

        selected.Should().BeSameAs(creditPrenote);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Select_Debit_ShouldUseBusinessTypePredicate(bool isPrenotification)
    {
        var debit = Debit();

        var selected = AchOutboundType7LayoutSelector.Select(
            [Default(), debit, CreditPrenote()],
            AchAddendaBusinessType.Debit,
            isPrenotification);

        selected.Should().BeSameAs(debit);
    }

    [Fact]
    public void Select_PrenotificationCredit_ShouldUsePublishedDefault()
    {
        var defaultVariant = Default();

        var selected = AchOutboundType7LayoutSelector.Select(
            [defaultVariant, Debit()],
            AchAddendaBusinessType.Credit,
            isPrenotification: true);

        selected.Should().BeSameAs(defaultVariant);
    }

    [Fact]
    public void Select_PrenotificationDebit_ShouldUseBusinessTypePredicate()
    {
        var debit = Debit();

        var selected = AchOutboundType7LayoutSelector.Select(
            [Default(), debit],
            AchAddendaBusinessType.Debit,
            isPrenotification: true);

        selected.Should().BeSameAs(debit);
    }

    [Fact]
    public void Select_MultiplePredicateMatches_ShouldFailClosed()
    {
        var act = () => AchOutboundType7LayoutSelector.Select(
            [
                Default(),
                Variant("MATCH_1", predicate: """{"BusinessType":"DEBIT"}"""),
                Variant("MATCH_2", predicate: """{"BusinessType":"DEBIT","TransactionFamily":"PRENOTIFICATION"}""")
            ],
            AchAddendaBusinessType.Debit,
            isPrenotification: true);

        act.Should().Throw<NachaGenerationException>()
            .Which.Code.Should().Be("NACHA_T7_SELECTION_AMBIGUOUS");
    }

    [Fact]
    public void Select_NoMatchAndNoDefault_ShouldFailClosed()
    {
        var act = () => AchOutboundType7LayoutSelector.Select(
            [CreditPrenote()],
            AchAddendaBusinessType.Credit,
            isPrenotification: false);

        act.Should().Throw<NachaGenerationException>()
            .Which.Code.Should().Be("NACHA_T7_SELECTION_DEFAULT_MISSING");
    }

    [Fact]
    public void Select_MultipleDefaults_ShouldFailClosed()
    {
        var act = () => AchOutboundType7LayoutSelector.Select(
            [Default("DEFAULT_1"), Default("DEFAULT_2"), CreditPrenote()],
            AchAddendaBusinessType.Credit,
            isPrenotification: false);

        act.Should().Throw<NachaGenerationException>()
            .Which.Code.Should().Be("NACHA_T7_SELECTION_DEFAULT_AMBIGUOUS");
    }

    [Theory]
    [InlineData("not-json")]
    [InlineData("[]")]
    [InlineData("{}")]
    [InlineData("{\"BusinessType\":null}")]
    [InlineData("{\"BusinessType\":\"\"}")]
    public void Select_MalformedOrUnsupportedPredicate_ShouldFailClosed(string predicate)
    {
        var act = () => AchOutboundType7LayoutSelector.Select(
            [Default(), Variant("INVALID", predicate: predicate)],
            AchAddendaBusinessType.Credit,
            isPrenotification: false);

        act.Should().Throw<NachaGenerationException>()
            .Which.Code.Should().Be("NACHA_T7_SELECTION_PREDICATE_INVALID");
    }

    [Fact]
    public void Select_DefaultWithPredicate_ShouldFailClosed()
    {
        var act = () => AchOutboundType7LayoutSelector.Select(
            [Variant("INVALID_DEFAULT", isDefault: true, predicate: """{"BusinessType":"CREDIT"}""")],
            AchAddendaBusinessType.Credit,
            isPrenotification: false);

        act.Should().Throw<NachaGenerationException>()
            .Which.Code.Should().Be("NACHA_T7_SELECTION_DEFAULT_INVALID");
    }

    private static CfgLayoutVariant Default(string code = "DEFAULT")
        => Variant(code, isDefault: true);

    private static CfgLayoutVariant Debit()
        => Variant("DEBIT", predicate: """{"BusinessType":"DEBIT"}""");

    private static CfgLayoutVariant CreditPrenote()
        => Variant("CREDIT_PRENOTE", predicate: """{"BusinessType":"CREDIT","TransactionFamily":"PRENOTIFICATION"}""");

    private static CfgLayoutVariant Variant(string code, bool isDefault = false, string? predicate = null)
        => new()
        {
            VariantCode = code,
            IsDefaultForRecord = isDefault,
            SelectionPredicateJson = predicate,
            Priority = isDefault ? 10 : 20,
            TotalLength = 106
        };
}
