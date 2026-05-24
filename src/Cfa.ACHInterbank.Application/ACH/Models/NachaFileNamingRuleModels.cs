namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record NachaFileNamingRulePolicy(
    int RuleId,
    int ClearingHouseId,
    int? SourceFinancialInstitutionId,
    string SourceFinancialInstitutionName,
    string OriginEntityCode,
    string NamePattern,
    int DailySequenceMin,
    int DailySequenceMax,
    string InternalFileIdMappingMode,
    bool RequiresNameHeaderEntityMatch,
    string NormativeSource,
    string NormativeReference);
