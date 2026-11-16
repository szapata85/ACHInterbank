using Cfa.ACHInterbank.Domain.Entities.Integrations;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.Integrations.Services;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using Xunit;

namespace Cfa.ACHInterbank.Tests;

public sealed class NptMap01MappingSafetyTests
{
    [Theory]
    [InlineData("OFEMP", "clearinghouse.code")]
    [InlineData("OFEMP", "transaction.companyIdentification")]
    [InlineData("OFIDARCH", "batch.id")]
    [InlineData("OFIDARCH", "transaction.id")]
    [InlineData("OFDIRECCIONIP", "constant.value")]
    [InlineData("OFDIRECCIONIP", "transaction.reference")]
    public void UnaccreditedIdentifiers_CannotBeUnlockedWithAlternativeSources(string field, string source)
    {
        var rule = NewRule(source);
        Assert.Contains("PROC_CONTRAPARTIDAS_SOURCE_NOT_HOMOLOGATED",
            ProcContrapartidasFunctionalMappingResolver.AssessProtectedRule(field, rule, source));
    }

    [Theory]
    [InlineData("OFCTA", "transaction.sourceAccountNumber")]
    [InlineData("OFIDTX", "transaction.reference")]
    public void ProtectedFields_OnlyPermitUnmodifiedTransactionSource(string field, string source)
    {
        var rule = NewRule(source);
        Assert.Null(ProcContrapartidasFunctionalMappingResolver.AssessProtectedRule(field, rule, source));

        rule.FixedValue = "OVERRIDE";
        AssertProtected(field, rule, source);
        rule.FixedValue = null;
        rule.DefaultValue = "OVERRIDE";
        AssertProtected(field, rule, source);
        rule.DefaultValue = null;
        rule.TransformationCode = "Lowercase";
        AssertProtected(field, rule, source);
        rule.TransformationCode = null;
        rule.FormatMask = "000000";
        AssertProtected(field, rule, source);
        rule.FormatMask = null;
        rule.ConditionExpression = "true";
        AssertProtected(field, rule, source);
        rule.ConditionExpression = null;
        rule.SourceKind = IntegrationSourceKindEnum.Expression;
        AssertProtected(field, rule, source);
        rule.SourceKind = IntegrationSourceKindEnum.Constant;
        AssertProtected(field, rule, source);
        rule.SourceKind = IntegrationSourceKindEnum.Transaction;
        AssertProtected(field, rule, "unknown.nonexistent");
    }

    [Fact]
    public void ResolvedIdentity_CannotDifferFromPersistedAccountOrReference()
    {
        var tx = new AchTransaction { SourceAccountNumber = "ACCT-ORIG-001", Reference = "REF-001" };
        ProcContrapartidasFunctionalMappingResolver.EnsureProtectedResolvedValues(tx, "ACCT-ORIG-001", "REF-001");
        var accountError = Assert.Throws<InvalidOperationException>(() =>
            ProcContrapartidasFunctionalMappingResolver.EnsureProtectedResolvedValues(tx, "ACCT-RECEPTOR-002", "REF-001"));
        Assert.Contains("OFCTA", accountError.Message);
        var referenceError = Assert.Throws<InvalidOperationException>(() =>
            ProcContrapartidasFunctionalMappingResolver.EnsureProtectedResolvedValues(tx, "ACCT-ORIG-001", "SPOOF-REF"));
        Assert.Contains("OFIDTX", referenceError.Message);
    }

    [Theory]
    [InlineData("NORIG", "batchHeaders.companyName", IntegrationSourceKindEnum.BatchHeader)]
    [InlineData("IDORIG", "batchHeaders.companyId", IntegrationSourceKindEnum.BatchHeader)]
    [InlineData("NCTARECEP", "entrydetails.accountnumber", IntegrationSourceKindEnum.EntryDetail)]
    public void IncomingPhysicalSources_RejectOverridesAndReceiverOriginSwaps(
        string field, string expected, IntegrationSourceKindEnum kind)
    {
        var rule = NewRule(expected);
        rule.SourceKind = kind;
        Assert.Null(ProcTransaccionesRequestMapper.AssessPhysicalNachaRule(field, rule));
        rule.FixedValue = "SPOOF";
        Assert.Contains("PROC_TRANSACCIONES_NACHA_SOURCE_INVALID",
            ProcTransaccionesRequestMapper.AssessPhysicalNachaRule(field, rule));
        rule.FixedValue = null;
        rule.SourceFieldPath = field == "NCTARECEP" ? "transaction.sourceAccountNumber" : "entrydetails.accountnumber";
        Assert.Contains("PROC_TRANSACCIONES_NACHA_SOURCE_INVALID",
            ProcTransaccionesRequestMapper.AssessPhysicalNachaRule(field, rule));
    }

    [Fact]
    public void PhysicalNachaValues_CannotSwapOriginatorAndReceiver()
    {
        var valid = new Dictionary<string, string>
        {
            ["NORIG"] = "BATCH-ORIGINATOR", ["IDORIG"] = "ORIG-ID", ["NCTARECEP"] = "RECEPTOR-ACCOUNT"
        };
        ProcTransaccionesRequestMapper.EnsurePhysicalNachaValues(valid,
            "BATCH-ORIGINATOR", "ORIG-ID", "RECEPTOR-ACCOUNT");
        valid["NCTARECEP"] = "ORIGIN-ACCOUNT";
        var error = Assert.Throws<InvalidOperationException>(() =>
            ProcTransaccionesRequestMapper.EnsurePhysicalNachaValues(valid,
                "BATCH-ORIGINATOR", "ORIG-ID", "RECEPTOR-ACCOUNT"));
        Assert.Contains("NCTARECEP", error.Message);
    }

    private static IntegrationMappingRule NewRule(string source) => new()
    {
        SourceKind = IntegrationSourceKindEnum.Transaction,
        SourceFieldPath = source,
        Enabled = true
    };

    private static void AssertProtected(string field, IntegrationMappingRule rule, string source)
        => Assert.Contains("PROC_CONTRAPARTIDAS_PROTECTED_SOURCE_INVALID",
            ProcContrapartidasFunctionalMappingResolver.AssessProtectedRule(field, rule, source));
}
