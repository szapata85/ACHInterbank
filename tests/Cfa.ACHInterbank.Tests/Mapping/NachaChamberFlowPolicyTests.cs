using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;
using FluentAssertions;

namespace Cfa.ACHInterbank.Tests.Mapping;

public sealed class NachaChamberFlowPolicyTests
{
    [Fact]
    public void Resolver_ShouldUseCanonicalCodesWithoutDatabaseIds()
    {
        var ach = NachaChamberFlowPolicyResolver.Resolve("ACH");
        var cenit = NachaChamberFlowPolicyResolver.Resolve("CENIT");
        var compatibility = NachaChamberFlowPolicyResolver.Resolve("ACHCOL");

        ach.Should().BeOfType<AchColombiaNachaChamberFlowPolicy>();
        cenit.Should().BeOfType<CenitNachaChamberFlowPolicy>();
        compatibility.Should().BeOfType<CompatibilityNachaChamberFlowPolicy>();
        ach.RequiresServicePartitioning.Should().BeFalse();
        cenit.RequiresServicePartitioning.Should().BeTrue();
        compatibility.RequiresServicePartitioning.Should().BeFalse();

        var compatibilityCtx = compatibility.ResolveOrdinaryFlow(CenitCtxOutbound2026Layout.OriginalProfileCode);
        compatibilityCtx.ValidateCtxAddendaSequences.Should().BeTrue();
        compatibilityCtx.ValidateType7Association.Should().BeTrue();
    }

    [Fact]
    public void AchReturnOutPolicy_ShouldPreserveV35PhysicalContract()
    {
        var policy = NachaChamberFlowPolicyResolver.Resolve("ACH");

        var decision = policy.ResolveReturnOutFlow("DEVOLUCION", [BuildBatch("PPD")]);

        decision.RecordLength.Should().Be(AchColReturnOutV35Layout.RecordLength);
        decision.BlockingFactor.Should().Be(AchColReturnOutV35Layout.BlockingFactor);
        decision.SelectionFlow.Should().Be("RETURN_OUT");
        decision.AuditPhase.Should().Be("RETURN_OUT_V35");
        decision.IsReturnOfReturn.Should().BeFalse();
    }

    [Fact]
    public void CenitReturnOfReturnPolicy_ShouldPreservePpdOnlyScope()
    {
        var policy = NachaChamberFlowPolicyResolver.Resolve("CENIT");

        var ppd = policy.ResolveReturnOutFlow(
            CenitReturnOfReturn2026Layout.FlowTypeCode,
            [BuildBatch("PPD")]);

        ppd.RecordLength.Should().Be(CenitReturnOut2026Layout.RecordLength);
        ppd.BlockingFactor.Should().Be(CenitReturnOut2026Layout.BlockingFactor);
        ppd.SelectionFlow.Should().Be("RETURN_OF_RETURN_OUT");
        ppd.AuditPhase.Should().Be("CENIT_RETURN_OF_RETURN_OUT_2026");
        ppd.IsReturnOfReturn.Should().BeTrue();

        var ccd = Assert.Throws<NachaGenerationException>(() => policy.ResolveReturnOutFlow(
            CenitReturnOfReturn2026Layout.FlowTypeCode,
            [BuildBatch("CCD")]));
        ccd.Code.Should().Be(CenitReturnOfReturn2026Layout.CcdScopeStatus);

        var ctx = Assert.Throws<NachaGenerationException>(() => policy.ResolveReturnOutFlow(
            CenitReturnOfReturn2026Layout.FlowTypeCode,
            [BuildBatch("CTX")]));
        ctx.Code.Should().Be(CenitReturnOfReturn2026Layout.CtxScopeStatus);
    }

    [Fact]
    public void OrdinaryFlowPolicy_ShouldKeepAchAndCenitCtxSemanticsSeparate()
    {
        var ach = NachaChamberFlowPolicyResolver.Resolve("ACH")
            .ResolveOrdinaryFlow(AchColOfficialNachaLayout.OutboundOriginalProfileCode);
        var cenitCtx = NachaChamberFlowPolicyResolver.Resolve("CENIT")
            .ResolveOrdinaryFlow(CenitCtxOutbound2026Layout.OriginalProfileCode);
        var cenitPpd = NachaChamberFlowPolicyResolver.Resolve("CENIT")
            .ResolveOrdinaryFlow(CenitOrdinaryOutbound2026Layout.OriginalProfileCode);

        ach.ValidateCtxAddendaSequences.Should().BeFalse();
        ach.ValidateType7Association.Should().BeTrue();
        ach.UsePublishedType7Variant.Should().BeTrue();
        ach.RequireAllType7CandidatesRendered.Should().BeTrue();
        ach.ValidateAllPublishedType7Variants.Should().BeTrue();

        cenitCtx.ValidateCtxAddendaSequences.Should().BeTrue();
        cenitCtx.ValidateType7Association.Should().BeTrue();
        cenitCtx.UsePublishedType7Variant.Should().BeFalse();
        cenitCtx.RequireAllType7CandidatesRendered.Should().BeFalse();

        cenitPpd.ValidateCtxAddendaSequences.Should().BeFalse();
        cenitPpd.ValidateType7Association.Should().BeFalse();
        cenitPpd.UsePublishedType7Variant.Should().BeFalse();
    }

    [Fact]
    public void RenderContext_ShouldPreservePreR4dDiagnosticChamberValues()
    {
        var ach = NachaChamberFlowPolicyResolver.Resolve("ACH")
            .CreateOfficialRecordRenderContext("ACH");
        var cenit = NachaChamberFlowPolicyResolver.Resolve("CENIT")
            .CreateOfficialRecordRenderContext("CENIT");
        var compatibility = NachaChamberFlowPolicyResolver.Resolve("ACHCOL")
            .CreateOfficialRecordRenderContext("ACHCOL");

        ach.RuleChamberCode.Should().Be("ACHCOL");
        cenit.RuleChamberCode.Should().Be("CENIT");
        compatibility.RuleChamberCode.Should().Be("ACHCOL");
        ach.RuleMetadataChamberCode.Should().Be("ACHCOL");
        cenit.AmountFormattingChamberCode.Should().Be("ACHCOL");
    }

    [Fact]
    public void GenerationGates_ShouldPreserveCenitAndLiveOfficialFailClosedBehavior()
    {
        var cenit = NachaChamberFlowPolicyResolver.Resolve("CENIT");
        var ach = NachaChamberFlowPolicyResolver.Resolve("ACH");

        var cenitLive = Assert.Throws<NachaGenerationException>(() => cenit.EnforcePreResolutionGenerationGate(
            new NachaGenerationOptions { ExecutionScope = "LIVE", Mode = "TABLE_DRIVEN" },
            isOfficialTableDrivenMode: true,
            "CENIT"));
        cenitLive.Code.Should().Be("CENIT_NOT_HOMOLOGATED");

        var achLegacyLive = Assert.Throws<NachaGenerationException>(() => ach.EnforcePreResolutionGenerationGate(
            new NachaGenerationOptions { ExecutionScope = "LIVE", Mode = "HYBRID" },
            isOfficialTableDrivenMode: false,
            "ACH"));
        achLegacyLive.Code.Should().Be("NACHA_LIVE_OFFICIAL_MODE_REQUIRED");

        var cenitDevelopment = new NachaGenerationOptions
        {
            ExecutionScope = "DEVELOPMENT",
            AllowNonHomologatedCenitDevelopment = true
        };
        var placeholder = new CfgProfile
        {
            Tags =
            [
                new CfgProfileTag { TagKey = "IsPlaceholder", TagValue = "true" },
                new CfgProfileTag { TagKey = "IsHomologated", TagValue = "false" },
                new CfgProfileTag { TagKey = "NormativeVersion", TagValue = "NOT-DEMONSTRATED" }
            ]
        };

        cenit.EnforceResolvedProfileGate(cenitDevelopment, placeholder);
    }

    private static NachaReturnOutBatch BuildBatch(string sec)
        => new(
            "220",
            "ORIGINADOR",
            string.Empty,
            "9001234567",
            sec,
            "RETURN",
            new DateTime(2026, 8, 16),
            new DateTime(2026, 8, 16),
            string.Empty,
            "87654321",
            1,
            []);
}
