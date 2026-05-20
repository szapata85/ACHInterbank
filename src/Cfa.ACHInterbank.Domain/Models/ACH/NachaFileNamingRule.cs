using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class NachaFileNamingRule : AuditableEntity
{
    public int Id { get; set; }
    public int ClearingHouseId { get; set; }
    public ClearingHouse ClearingHouse { get; set; } = null!;
    public int? SourceFinancialInstitutionId { get; set; }
    public FinancialInstitution? SourceFinancialInstitution { get; set; }
    public NachaFileDirection FileDirection { get; set; } = NachaFileDirection.Outbound;
    public string NamePattern { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public int DailySequenceMin { get; set; } = 1;
    public int DailySequenceMax { get; set; } = 36;
    public InternalFileIdMappingMode InternalFileIdMappingMode { get; set; } = InternalFileIdMappingMode.Alphanumeric36;
    public bool RequiresNameHeaderEntityMatch { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime EffectiveFrom { get; set; }
    public DateTime? EffectiveTo { get; set; }
    public string NormativeSource { get; set; } = string.Empty;
    public string NormativeReference { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public bool IsEffective(DateTime date)
        => IsActive
           && EffectiveFrom.Date <= date.Date
           && (!EffectiveTo.HasValue || EffectiveTo.Value.Date >= date.Date);
}
