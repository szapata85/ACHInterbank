using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class NachaRecordDefinition : AuditableEntity
{
    public int Id { get; set; }
    public string RecordCode { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public NachaRecordSourceType SourceType { get; set; }
    public string? SourceName { get; set; }
    public string? FilterKey { get; set; }
    public bool IsEnabled { get; set; } = true;
}
