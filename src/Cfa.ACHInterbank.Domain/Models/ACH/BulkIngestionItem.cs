using Cfa.ACHInterbank.Domain.Entities.SchedulerTask.Base;

namespace Cfa.ACHInterbank.Domain.Models.ACH;

public class BulkIngestionItem : AuditableEntity
{
    public long Id { get; set; }
    public Guid BatchId { get; set; }
    public BulkIngestionBatch Batch { get; set; } = null!;

    public int ItemIndex { get; set; }
    public string Reference { get; set; } = string.Empty;
    public BulkIngestionItemStatusEnum Status { get; set; } = BulkIngestionItemStatusEnum.Ready;
    public string Message { get; set; } = string.Empty;
    public int? TransactionId { get; set; }

    public string RawPayloadJson { get; set; } = string.Empty;
    public string? NormalizedPayloadJson { get; set; }
}

public enum BulkIngestionItemStatusEnum
{
    Ready = 1,
    StructuralError = 2,
    ProcessingError = 3,
    Processed = 4
}
