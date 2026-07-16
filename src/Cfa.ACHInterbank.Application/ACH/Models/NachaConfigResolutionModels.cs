using Cfa.ACHInterbank.Domain.Models.ACH.Config;
using System.Text.Json.Serialization;

namespace Cfa.ACHInterbank.Application.ACH.Models;

public class NachaConfigResolutionRequest
{
    public string ClearingHouseCode { get; init; } = "ACH";
    public string FlowTypeCode { get; init; } = "ORIGINAL";
    public string DirectionCode { get; init; } = "SALIDA";
    public string? ServiceClassCode { get; init; }
    public DateTime ProcessDateUtc { get; init; }
    public IReadOnlyCollection<string> RecordCodes { get; init; } = Array.Empty<string>();
    public IReadOnlyDictionary<string, string> SelectionContext { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}

public class NachaConfigResolutionResult
{
    public bool Success { get; init; }
    public bool UsedFallback { get; set; }
    public CfgProfile? Profile { get; init; }
    public IReadOnlyDictionary<string, CfgLayoutVariant> LayoutsByRecordCode { get; init; } = new Dictionary<string, CfgLayoutVariant>(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyDictionary<string, IReadOnlyList<CfgLayoutVariant>> LayoutVariantsByRecordCode { get; init; }
        = new Dictionary<string, IReadOnlyList<CfgLayoutVariant>>(StringComparer.OrdinalIgnoreCase);
    public List<string> Trace { get; init; } = [];
    public List<string> Warnings { get; init; } = [];
}

public class NachaGenerationAuditResult
{
    public string TraceId { get; set; } = Guid.NewGuid().ToString("N");
    public string? CorrelationId { get; set; }
    public DateTime GeneratedAtUtc { get; set; } = DateTime.UtcNow;
    public string Mode { get; set; } = "LEGACY";
    public int? ProfileId { get; set; }
    public string? ProfileCode { get; set; }
    public string? ProfileVersion { get; set; }
    public string? ProfileStatus { get; set; }
    public DateTime? EffectiveDate { get; set; }
    public string? ClearingHouseName { get; set; }
    public string? FileHash { get; set; }
    public int TotalRecords { get; set; }
    public int TotalFields { get; set; }
    public bool LegacyFallbackUsed { get; set; }
    public string Status { get; set; } = "Ok";
    public string? ErrorCode { get; set; }
    public List<string> NewEngineRecordCodes { get; init; } = [];
    public List<string> LegacyRecordCodes { get; init; } = [];
    public List<string> Warnings { get; init; } = [];
    public List<string> Trace { get; init; } = [];
    public List<NachaGenerationTraceEntry> FieldTraceEntries { get; init; } = [];
    public List<string> EquivalenceDiffs { get; init; } = [];
    public int Type7TotalCandidates { get; set; }
    public int Type7GeneratedTableDriven { get; set; }
    public int Type7GeneratedLegacy { get; set; }
    public string? ClearingHouseCode { get; set; }
    public string? Type7LayoutVariantCode { get; set; }
    public Dictionary<string, int> Type7FallbackReasons { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> Type7FallbackByLayout { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, int> Type7DiffByField { get; init; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> Type7AliasResolutionTrace { get; init; } = [];
    public int ShadowDiffCount { get; set; }
    public List<string> ShadowDiffDetails { get; init; } = [];
    public string Phase { get; set; } = "6B.3A";
    public NachaFileIdModifierAudit? FileIdModifier { get; set; }
    public NachaFileControlTotalsAudit? FileTotals { get; set; }
    public List<NachaBatchControlTotalsAudit> BatchTotals { get; init; } = [];
}

public sealed class NachaFileIdModifierAudit
{
    public int DailySequence { get; init; }
    public string ResolvedValue { get; init; } = string.Empty;
}

public sealed class NachaFileControlTotalsAudit
{
    public int BatchCount { get; init; }
    public int BlockCount { get; init; }
    public int EntryAddendaCount { get; init; }
    public long EntryHash { get; init; }
    public long TotalDebitAmountInCents { get; init; }
    public long TotalCreditAmountInCents { get; init; }
    public int PhysicalRecordCountBeforePadding { get; init; }
    public int PaddingRecordCount { get; init; }
    public int PhysicalRecordCountAfterPadding { get; init; }
}

public sealed class NachaBatchControlTotalsAudit
{
    public int BatchId { get; init; }
    public int EntryAddendaCount { get; init; }
    public long EntryHash { get; init; }
    public long TotalDebitAmountInCents { get; init; }
    public long TotalCreditAmountInCents { get; init; }
    public int EntryDetailCount { get; init; }
    public int AddendaCount { get; init; }
}

public class NachaGenerationTraceEntry
{
    public string TraceEntryId { get; init; } = Guid.NewGuid().ToString("N");
    public string? TraceId { get; set; }
    public string RecordType { get; set; } = string.Empty;
    public int RecordSequence { get; set; }
    public int LineNumber { get; set; }
    public int LayoutVariantId { get; set; }
    public string LayoutVariantCode { get; set; } = string.Empty;
    public int FieldDefinitionId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public int PositionStart { get; set; }
    public int PositionEnd { get; set; }
    public int Length { get; set; }
    public string DataType { get; set; } = "string";
    public bool Required { get; set; } = true;
    public string SourceType { get; set; } = string.Empty;
    public string? SourceFieldPath { get; set; }
    public string? ConstantValueSanitized { get; set; }
    public string? CalculationType { get; set; }
    public string? TransformationApplied { get; set; }
    public string PaddingDirection { get; set; } = "Right";
    public string PaddingChar { get; set; } = " ";
    public string? RawValueSanitized { get; set; }
    public string? RenderedValue { get; set; }
    [JsonIgnore]
    public string? RuntimeRenderedValue { get; set; }
    public int RenderedLength { get; set; }
    public string ValidationStatus { get; set; } = "Ok";
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public string? GeneratedLinePreviewSanitized { get; set; }
    public int ValueStartIndex { get; set; }
    public int ValueEndIndex { get; set; }
}
