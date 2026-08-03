using Cfa.ACHInterbank.Application.OutgoingTransactionMonitoring;
using Cfa.ACHInterbank.Domain.Entities.Audit;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;

namespace Cfa.ACHInterbank.Persistence.ACH.OutgoingTransactionMonitoring;

[Scoped]
public sealed class OutgoingTransactionMonitoringAuditWriter : IOutgoingTransactionMonitoringAuditWriter
{
    private readonly AchDbContext _context;

    public OutgoingTransactionMonitoringAuditWriter(AchDbContext context) => _context = context;

    public async Task WriteAsync(OutgoingTransactionMonitoringAudit audit, CancellationToken cancellationToken = default)
    {
        _context.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityName = "OutgoingTransactionMonitoring",
            EntityId = audit.EntityId,
            Action = audit.Operation,
            ChangedBy = string.IsNullOrWhiteSpace(audit.UserId) ? "authenticated-user" : audit.UserId,
            ChangedAt = DateTime.UtcNow,
            CorrelationId = audit.CorrelationId,
            ChangedFields = "ReadOnlyQuery",
            AfterJson = audit.SanitizedCriteria,
            BeforeJson = audit.Authorized ? "Authorization:Allowed" : "Authorization:Denied"
        });
        await _context.SaveChangesAsync(cancellationToken);
    }
}
