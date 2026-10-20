using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

internal sealed record NachaReturnOutFlowDecision(
    int RecordLength,
    int BlockingFactor,
    string SelectionFlow,
    string AuditPhase,
    bool IsReturnOfReturn);

internal sealed record NachaOrdinaryFlowDecision(
    bool ValidateCtxAddendaSequences,
    bool ValidateType7Association,
    bool UsePublishedType7Variant,
    bool RequireAllType7CandidatesRendered,
    bool ValidateAllPublishedType7Variants);

internal interface INachaChamberFlowPolicy
{
    bool RequiresServicePartitioning { get; }

    NachaReturnOutFlowDecision ResolveReturnOutFlow(
        string flowTypeCode,
        IReadOnlyList<NachaReturnOutBatch> batches);

    NachaOrdinaryFlowDecision ResolveOrdinaryFlow(string? profileCode);

    NachaOfficialRecordRenderContext CreateOfficialRecordRenderContext(string? reportedClearingHouseCode);

    void EnforcePreResolutionGenerationGate(
        NachaGenerationOptions options,
        bool isOfficialTableDrivenMode,
        string clearingHouseCode);

    void EnforceResolvedProfileGate(NachaGenerationOptions options, CfgProfile profile);

    void ValidateOfficialLayoutSnapshot(string recordCode, CfgLayoutVariant layout);
}

internal static class NachaChamberFlowPolicyResolver
{
    private static readonly INachaChamberFlowPolicy AchColombia = new AchColombiaNachaChamberFlowPolicy();
    private static readonly INachaChamberFlowPolicy Cenit = new CenitNachaChamberFlowPolicy();

    public static INachaChamberFlowPolicy Resolve(string? clearingHouseCode)
    {
        if (string.Equals(clearingHouseCode, "CENIT", StringComparison.OrdinalIgnoreCase))
        {
            return Cenit;
        }

        if (string.Equals(clearingHouseCode, "ACH", StringComparison.OrdinalIgnoreCase))
        {
            return AchColombia;
        }

        // Preserve the pre-R4D compatibility behavior for callers that provide a non-canonical code:
        // non-CENIT ReturnOut uses the ACH physical contract, while ACH-only snapshot checks remain disabled.
        return new CompatibilityNachaChamberFlowPolicy(clearingHouseCode);
    }
}

internal abstract class NachaChamberFlowPolicyBase : INachaChamberFlowPolicy
{
    public abstract bool RequiresServicePartitioning { get; }

    public abstract NachaReturnOutFlowDecision ResolveReturnOutFlow(
        string flowTypeCode,
        IReadOnlyList<NachaReturnOutBatch> batches);

    public abstract NachaOrdinaryFlowDecision ResolveOrdinaryFlow(string? profileCode);

    public abstract NachaOfficialRecordRenderContext CreateOfficialRecordRenderContext(string? reportedClearingHouseCode);

    public virtual void EnforcePreResolutionGenerationGate(
        NachaGenerationOptions options,
        bool isOfficialTableDrivenMode,
        string clearingHouseCode)
        => EnforceLiveOfficialMode(options, isOfficialTableDrivenMode, clearingHouseCode);

    public virtual void EnforceResolvedProfileGate(NachaGenerationOptions options, CfgProfile profile)
    {
    }

    public abstract void ValidateOfficialLayoutSnapshot(string recordCode, CfgLayoutVariant layout);

    protected static NachaReturnOutFlowDecision AchReturnOutDecision()
        => new(
            AchColReturnOutV35Layout.RecordLength,
            AchColReturnOutV35Layout.BlockingFactor,
            "RETURN_OUT",
            "RETURN_OUT_V35",
            IsReturnOfReturn: false);

    protected static void EnforceLiveOfficialMode(
        NachaGenerationOptions options,
        bool isOfficialTableDrivenMode,
        string clearingHouseCode)
    {
        if (!string.Equals(options.ExecutionScope?.Trim(), "LIVE", StringComparison.OrdinalIgnoreCase)
            || isOfficialTableDrivenMode)
        {
            return;
        }

        throw new NachaGenerationException(
            "NACHA_LIVE_OFFICIAL_MODE_REQUIRED",
            "La generación LIVE exige el motor oficial table-driven; HYBRID y legacy quedan aislados a desarrollo controlado.",
            ruleId: "ACHCOL-GENERATION-FAIL-CLOSED",
            chamber: clearingHouseCode,
            recordType: "FILE",
            fieldName: "GENERATION_MODE",
            cause: "Modo no oficial solicitado para alcance LIVE.");
    }

    protected static bool ReadProfileFlag(CfgProfile profile, string key, bool defaultValue)
    {
        var value = profile.Tags.FirstOrDefault(tag =>
            string.Equals(tag.TagKey, key, StringComparison.OrdinalIgnoreCase))?.TagValue;
        return bool.TryParse(value, out var parsed) ? parsed : defaultValue;
    }

    protected static bool ContainsForbiddenOfficialTransformation(string? pipelineJson)
        => !string.IsNullOrWhiteSpace(pipelineJson)
           && (pipelineJson.Contains("truncate", StringComparison.OrdinalIgnoreCase)
               || pipelineJson.Contains("substring", StringComparison.OrdinalIgnoreCase));

    protected static NachaGenerationException BuildFieldRuleException(
        string code,
        AchColOfficialFieldDescriptor descriptor,
        string cause)
        => new(
            code,
            "El perfil no cumple el descriptor oficial ACHCOL.",
            descriptor.RuleId,
            "ACHCOL",
            descriptor.RecordCode,
            descriptor.FieldCode,
            cause,
            descriptor.StartPosition,
            descriptor.Length);
}

internal sealed class AchColombiaNachaChamberFlowPolicy : NachaChamberFlowPolicyBase
{
    public override bool RequiresServicePartitioning => false;

    public override NachaReturnOutFlowDecision ResolveReturnOutFlow(
        string flowTypeCode,
        IReadOnlyList<NachaReturnOutBatch> batches)
        => AchReturnOutDecision();

    public override NachaOrdinaryFlowDecision ResolveOrdinaryFlow(string? profileCode)
        => new(
            ValidateCtxAddendaSequences: false,
            ValidateType7Association: true,
            UsePublishedType7Variant: true,
            RequireAllType7CandidatesRendered: true,
            ValidateAllPublishedType7Variants: true);

    public override NachaOfficialRecordRenderContext CreateOfficialRecordRenderContext(string? reportedClearingHouseCode)
        => new(
            RuleChamberCode: "ACHCOL",
            RuleMetadataChamberCode: "ACHCOL",
            AmountFormattingChamberCode: "ACHCOL");

    public override void ValidateOfficialLayoutSnapshot(string recordCode, CfgLayoutVariant layout)
    {
        if (layout.TotalLength != AchColOfficialNachaLayout.RecordLength)
        {
            throw new NachaGenerationException(
                "NACHA_PROFILE_LAYOUT_MISMATCH",
                "El layout publicado no coincide con el snapshot normativo ACHCOL.",
                "ACHCOL-PHYSICAL-RECORD-LENGTH",
                "ACHCOL",
                recordCode,
                "RECORD",
                "Longitud física distinta de 106.",
                1,
                AchColOfficialNachaLayout.RecordLength);
        }

        var expectedFields = AchColReturnOutV35Layout.IsVariant(layout.VariantCode)
            ? AchColReturnOutV35Layout.ForRecord(recordCode)
            : AchColOfficialNachaLayout.ForVariant(recordCode, layout.VariantCode);
        var actualFields = layout.Fields.Where(field => field.IsEnabled).ToList();
        foreach (var expected in expectedFields)
        {
            var actual = actualFields.FirstOrDefault(field =>
                string.Equals(field.FieldCode, expected.FieldCode, StringComparison.OrdinalIgnoreCase));
            if (actual is null
                || actual.StartPosition != expected.StartPosition
                || actual.Length != expected.Length
                || actual.Justification != expected.Justification
                || actual.PadChar != expected.PadChar
                || !string.Equals(actual.FormatMask, expected.Format, StringComparison.Ordinal))
            {
                throw BuildFieldRuleException(
                    "NACHA_PROFILE_LAYOUT_MISMATCH",
                    expected,
                    "Posición, longitud, alineación, relleno o formato no coincide con MAN-004 V35.");
            }

            if (!actual.Rules.Any(rule => rule.IsEnabled && string.Equals(rule.RuleCode, expected.RuleId, StringComparison.OrdinalIgnoreCase)))
            {
                throw BuildFieldRuleException(
                    "NACHA_REQUIRED_RULE_MISSING",
                    expected,
                    "CfgFieldRule ejecutable ausente para el campo crítico.");
            }

            if (ContainsForbiddenOfficialTransformation(actual.TransformationPipelineJson)
                || !string.IsNullOrWhiteSpace(actual.SourceDefinition?.FallbackPolicyJson))
            {
                throw BuildFieldRuleException(
                    "NACHA_SILENT_TRANSFORMATION_FORBIDDEN",
                    expected,
                    "La ruta oficial no admite truncamiento, substring ni fallback silencioso.");
            }
        }

        var expectedCodes = expectedFields.Select(field => field.FieldCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var unexpected = actualFields.FirstOrDefault(field => !expectedCodes.Contains(field.FieldCode));
        if (unexpected is not null)
        {
            throw new NachaGenerationException(
                "NACHA_PROFILE_LAYOUT_MISMATCH",
                "El layout publicado contiene un campo no contemplado por el snapshot ACHCOL.",
                "ACHCOL-PHYSICAL-RECORD-LENGTH",
                "ACHCOL",
                recordCode,
                unexpected.FieldCode,
                "Campo habilitado no reconocido.",
                unexpected.StartPosition,
                unexpected.Length);
        }
    }
}

internal sealed class CenitNachaChamberFlowPolicy : NachaChamberFlowPolicyBase
{
    public override bool RequiresServicePartitioning => true;

    public override NachaReturnOutFlowDecision ResolveReturnOutFlow(
        string flowTypeCode,
        IReadOnlyList<NachaReturnOutBatch> batches)
    {
        var isReturnOfReturn = string.Equals(
            flowTypeCode,
            CenitReturnOfReturn2026Layout.FlowTypeCode,
            StringComparison.Ordinal);
        if (isReturnOfReturn && batches.Any(batch => !string.Equals(
                batch.StandardEntryClassCode?.Trim(),
                "PPD",
                StringComparison.OrdinalIgnoreCase)))
        {
            var includesCtx = batches.Any(batch => string.Equals(
                batch.StandardEntryClassCode?.Trim(),
                "CTX",
                StringComparison.OrdinalIgnoreCase));
            throw new NachaGenerationException(
                includesCtx ? CenitReturnOfReturn2026Layout.CtxScopeStatus : CenitReturnOfReturn2026Layout.CcdScopeStatus,
                "El contrato específico de ROR CENIT vigente está definido para PPD.");
        }

        return new NachaReturnOutFlowDecision(
            CenitReturnOut2026Layout.RecordLength,
            CenitReturnOut2026Layout.BlockingFactor,
            isReturnOfReturn ? "RETURN_OF_RETURN_OUT" : "RETURN_OUT",
            isReturnOfReturn ? "CENIT_RETURN_OF_RETURN_OUT_2026" : "CENIT_RETURN_OUT_2026",
            isReturnOfReturn);
    }

    public override NachaOrdinaryFlowDecision ResolveOrdinaryFlow(string? profileCode)
    {
        var isCtx = CenitCtxOutbound2026Layout.IsProfile(profileCode);
        return new NachaOrdinaryFlowDecision(
            ValidateCtxAddendaSequences: isCtx,
            ValidateType7Association: isCtx,
            UsePublishedType7Variant: false,
            RequireAllType7CandidatesRendered: false,
            ValidateAllPublishedType7Variants: false);
    }

    public override NachaOfficialRecordRenderContext CreateOfficialRecordRenderContext(string? reportedClearingHouseCode)
        => new(
            RuleChamberCode: reportedClearingHouseCode,
            RuleMetadataChamberCode: "ACHCOL",
            AmountFormattingChamberCode: "ACHCOL");

    public override void EnforcePreResolutionGenerationGate(
        NachaGenerationOptions options,
        bool isOfficialTableDrivenMode,
        string clearingHouseCode)
    {
        if (string.Equals(options.ExecutionScope?.Trim(), "LIVE", StringComparison.OrdinalIgnoreCase))
        {
            throw new NachaGenerationException(
                "CENIT_NOT_HOMOLOGATED",
                "CENIT permanece NO-GO / NOT HOMOLOGATED / BLOCKED FOR LIVE.",
                ruleId: "CENIT-FORMAT-NACHAM",
                chamber: "CENIT",
                recordType: "FILE",
                fieldName: "PROFILE",
                cause: "Generación LIVE bloqueada antes de seleccionar motor o perfil.");
        }

        EnforceLiveOfficialMode(options, isOfficialTableDrivenMode, clearingHouseCode);
    }

    public override void EnforceResolvedProfileGate(NachaGenerationOptions options, CfgProfile profile)
    {
        var executionScope = (options.ExecutionScope ?? "LIVE").Trim();
        var isLive = string.Equals(executionScope, "LIVE", StringComparison.OrdinalIgnoreCase);
        var isPlaceholder = ReadProfileFlag(profile, "IsPlaceholder", defaultValue: true);
        var isHomologated = ReadProfileFlag(profile, "IsHomologated", defaultValue: false);
        var hasNormativeVersion = profile.Tags.Any(tag =>
            string.Equals(tag.TagKey, "NormativeVersion", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(tag.TagValue)
            && !tag.TagValue.Contains("NOT-DEMONSTRATED", StringComparison.OrdinalIgnoreCase));

        if (isLive || isPlaceholder || !isHomologated || !hasNormativeVersion)
        {
            if (!isLive && options.AllowNonHomologatedCenitDevelopment)
            {
                return;
            }

            throw new NachaGenerationException(
                "CENIT_NOT_HOMOLOGATED",
                "CENIT permanece NO-GO / NOT HOMOLOGATED / BLOCKED FOR LIVE. Falta especificación técnica oficial y homologación explícita.",
                ruleId: "CENIT-FORMAT-NACHAM",
                chamber: "CENIT",
                recordType: "FILE",
                fieldName: "PROFILE",
                cause: "Perfil no homologado para el alcance solicitado.");
        }
    }

    public override void ValidateOfficialLayoutSnapshot(string recordCode, CfgLayoutVariant layout)
    {
        if (CenitReturnOut2026Layout.IsVariant(layout.VariantCode))
        {
            ValidateCenitReturnOut2026LayoutSnapshot(recordCode, layout);
            return;
        }

        if (CenitOrdinaryOutbound2026Layout.IsVariant(layout.VariantCode))
        {
            ValidateCenitOrdinaryOutbound2026LayoutSnapshot(recordCode, layout);
            return;
        }

        if (CenitCtxOutbound2026Layout.IsVariant(layout.VariantCode))
        {
            ValidateCenitCtxOutbound2026LayoutSnapshot(recordCode, layout);
        }
    }

    private static void ValidateCenitCtxOutbound2026LayoutSnapshot(string recordCode, CfgLayoutVariant layout)
    {
        if (layout.TotalLength != CenitCtxOutbound2026Layout.RecordLength
            || !CenitCtxOutbound2026Layout.IsVariant(layout.VariantCode))
        {
            throw new NachaGenerationException(
                "NACHA_PROFILE_LAYOUT_MISMATCH",
                "El layout publicado no coincide con el snapshot normativo CENIT CTX 2026.");
        }

        var expectedFields = CenitCtxOutbound2026Layout.ForRecord(recordCode);
        var actualFields = layout.Fields.Where(field => field.IsEnabled).ToList();
        foreach (var expected in expectedFields)
        {
            var actual = actualFields.FirstOrDefault(field =>
                string.Equals(field.FieldCode, expected.FieldCode, StringComparison.OrdinalIgnoreCase));
            if (actual is null
                || actual.StartPosition != expected.StartPosition
                || actual.Length != expected.Length
                || actual.Justification != expected.Justification
                || actual.PadChar != expected.PadChar
                || !string.Equals(actual.FormatMask, expected.Format, StringComparison.Ordinal)
                || !actual.Rules.Any(rule => rule.IsEnabled
                    && string.Equals(rule.RuleCode, expected.RuleId, StringComparison.OrdinalIgnoreCase)))
            {
                throw BuildFieldRuleException(
                    "NACHA_PROFILE_LAYOUT_MISMATCH",
                    expected,
                    "El campo no coincide con el Manual CENIT 2026, Anexos 1.2, 1.4 o 1.5.");
            }

            if (ContainsForbiddenOfficialTransformation(actual.TransformationPipelineJson)
                || !string.IsNullOrWhiteSpace(actual.SourceDefinition?.FallbackPolicyJson))
            {
                throw BuildFieldRuleException(
                    "NACHA_SILENT_TRANSFORMATION_FORBIDDEN",
                    expected,
                    "La ruta oficial CENIT CTX no admite truncamiento, substring ni fallback silencioso.");
            }
        }

        var expectedCodes = expectedFields.Select(field => field.FieldCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (actualFields.Any(field => !expectedCodes.Contains(field.FieldCode)))
        {
            throw new NachaGenerationException(
                "NACHA_PROFILE_LAYOUT_MISMATCH",
                "El layout publicado contiene campos no contemplados por el snapshot CENIT CTX 2026.");
        }
    }

    private static void ValidateCenitOrdinaryOutbound2026LayoutSnapshot(string recordCode, CfgLayoutVariant layout)
    {
        if (layout.TotalLength != CenitOrdinaryOutbound2026Layout.RecordLength
            || !CenitOrdinaryOutbound2026Layout.IsVariant(layout.VariantCode))
        {
            throw new NachaGenerationException(
                "NACHA_PROFILE_LAYOUT_MISMATCH",
                "El layout publicado no coincide con el snapshot normativo CENIT ordinario 2026.");
        }

        var expectedFields = CenitOrdinaryOutbound2026Layout.ForRecord(recordCode);
        var actualFields = layout.Fields.Where(field => field.IsEnabled).ToList();
        foreach (var expected in expectedFields)
        {
            var actual = actualFields.FirstOrDefault(field =>
                string.Equals(field.FieldCode, expected.FieldCode, StringComparison.OrdinalIgnoreCase));
            if (actual is null
                || actual.StartPosition != expected.StartPosition
                || actual.Length != expected.Length
                || actual.Justification != expected.Justification
                || actual.PadChar != expected.PadChar
                || !string.Equals(actual.FormatMask, expected.Format, StringComparison.Ordinal)
                || !actual.Rules.Any(rule => rule.IsEnabled
                    && string.Equals(rule.RuleCode, expected.RuleId, StringComparison.OrdinalIgnoreCase)))
            {
                throw BuildFieldRuleException(
                    "NACHA_PROFILE_LAYOUT_MISMATCH",
                    expected,
                    "El campo no coincide con el Manual CENIT 2026, Anexo 1.");
            }

            if (ContainsForbiddenOfficialTransformation(actual.TransformationPipelineJson)
                || !string.IsNullOrWhiteSpace(actual.SourceDefinition?.FallbackPolicyJson))
            {
                throw BuildFieldRuleException(
                    "NACHA_SILENT_TRANSFORMATION_FORBIDDEN",
                    expected,
                    "La ruta oficial CENIT no admite truncamiento, substring ni fallback silencioso.");
            }
        }

        var expectedCodes = expectedFields.Select(field => field.FieldCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (actualFields.Any(field => !expectedCodes.Contains(field.FieldCode)))
        {
            throw new NachaGenerationException(
                "NACHA_PROFILE_LAYOUT_MISMATCH",
                "El layout publicado contiene campos no contemplados por el snapshot CENIT ordinario 2026.");
        }
    }

    private static void ValidateCenitReturnOut2026LayoutSnapshot(string recordCode, CfgLayoutVariant layout)
    {
        if (layout.TotalLength != CenitReturnOut2026Layout.RecordLength
            || !CenitReturnOut2026Layout.IsVariant(layout.VariantCode))
        {
            throw new NachaGenerationException(
                "NACHA_PROFILE_LAYOUT_MISMATCH",
                "El layout publicado no coincide con el snapshot normativo CENIT 2026.");
        }

        var expectedFields = CenitReturnOut2026Layout.ForRecord(recordCode);
        var actualFields = layout.Fields.Where(field => field.IsEnabled).ToList();
        foreach (var expected in expectedFields)
        {
            var actual = actualFields.FirstOrDefault(field =>
                string.Equals(field.FieldCode, expected.FieldCode, StringComparison.OrdinalIgnoreCase));
            if (actual is null
                || actual.StartPosition != expected.StartPosition
                || actual.Length != expected.Length
                || actual.Justification != expected.Justification
                || actual.PadChar != expected.PadChar
                || !string.Equals(actual.FormatMask, expected.Format, StringComparison.Ordinal)
                || !actual.Rules.Any(rule => rule.IsEnabled
                    && string.Equals(rule.RuleCode, expected.RuleId, StringComparison.OrdinalIgnoreCase)))
            {
                throw BuildFieldRuleException(
                    "NACHA_PROFILE_LAYOUT_MISMATCH",
                    expected,
                    "El campo no coincide con el Manual CENIT 2026, sección 7.2.1 y Anexo 1.6.");
            }

            if (ContainsForbiddenOfficialTransformation(actual.TransformationPipelineJson)
                || !string.IsNullOrWhiteSpace(actual.SourceDefinition?.FallbackPolicyJson))
            {
                throw BuildFieldRuleException(
                    "NACHA_SILENT_TRANSFORMATION_FORBIDDEN",
                    expected,
                    "La ruta oficial CENIT no admite truncamiento, substring ni fallback silencioso.");
            }
        }

        var expectedCodes = expectedFields.Select(field => field.FieldCode).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (actualFields.Any(field => !expectedCodes.Contains(field.FieldCode)))
        {
            throw new NachaGenerationException(
                "NACHA_PROFILE_LAYOUT_MISMATCH",
                "El layout publicado contiene campos no contemplados por el snapshot CENIT 2026.");
        }
    }

}

internal sealed class CompatibilityNachaChamberFlowPolicy(string? clearingHouseCode) : NachaChamberFlowPolicyBase
{
    public override bool RequiresServicePartitioning => false;

    public override NachaReturnOutFlowDecision ResolveReturnOutFlow(
        string flowTypeCode,
        IReadOnlyList<NachaReturnOutBatch> batches)
        => AchReturnOutDecision();

    public override NachaOrdinaryFlowDecision ResolveOrdinaryFlow(string? profileCode)
    {
        // Before R4D, CTX behavior was profile-driven even if a caller supplied a non-canonical chamber code.
        // Preserve that compatibility edge while avoiding any database-ID inference.
        var isCtx = CenitCtxOutbound2026Layout.IsProfile(profileCode);
        return new NachaOrdinaryFlowDecision(
            ValidateCtxAddendaSequences: isCtx,
            ValidateType7Association: isCtx,
            UsePublishedType7Variant: false,
            RequireAllType7CandidatesRendered: false,
            ValidateAllPublishedType7Variants: false);
    }

    public override NachaOfficialRecordRenderContext CreateOfficialRecordRenderContext(string? reportedClearingHouseCode)
        => new(
            RuleChamberCode: clearingHouseCode,
            RuleMetadataChamberCode: "ACHCOL",
            AmountFormattingChamberCode: "ACHCOL");

    public override void ValidateOfficialLayoutSnapshot(string recordCode, CfgLayoutVariant layout)
    {
    }
}
