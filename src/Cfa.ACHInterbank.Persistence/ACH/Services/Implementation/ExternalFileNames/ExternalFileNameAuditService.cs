using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Models.ACH.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;

[Scoped]
public class ExternalFileNameAuditService : IExternalFileNameAuditService
{
    private readonly AchDbContext _context;

    public ExternalFileNameAuditService(AchDbContext context)
    {
        _context = context;
    }

    public async Task RegisterAsync(ExternalFileNameContext context, ExternalFileNamePolicyResult result, CancellationToken ct = default)
    {
        if (result.Components.ReservationId.HasValue
            && await _context.ExternalFileNameRegistry.AsNoTracking().AnyAsync(
                x => x.GenerationReservationId == result.Components.ReservationId.Value,
                ct))
        {
            return;
        }

        var auditTimestamp = context.OperationalTimeSnapshot?.CapturedAtUtc ?? DateTime.UtcNow;
        var registry = new ExternalFileNameRegistry
        {
            ClearingHouseId = context.ClearingHouseId,
            FlowCode = context.Flow.ToString(),
            Direction = context.Direction.ToString(),
            ExternalFileName = result.ExternalFileName,
            InternalFileName = context.InternalFileName,
            ExternalFileType = context.ExternalFileType.ToString(),
            FileIdModifier = result.CorrelationEvidence.HeaderFileIdModifier?.ToString(),
            ExternalSequence = result.Components.ExternalSequence,
            DeclaredDetailCount = result.CorrelationEvidence.DeclaredDetailCount,
            ActualDetailCount = result.CorrelationEvidence.ActualDetailCount,
            FileHash = context.FileHash,
            FileSize = context.FileSize,
            ProcessingDate = context.ProcessingDate,
            CycleId = context.CycleId,
            ValidationDisposition = result.Validation.Disposition.ToString(),
            ValidationResult = result.Validation.IsHardBlocked ? "Rejected" : "Accepted",
            ValidationIssuesJson = JsonSerializer.Serialize(result.Validation.Issues),
            CorrelationEvidenceJson = JsonSerializer.Serialize(result.CorrelationEvidence),
            CreatedAtUtc = auditTimestamp,
            CreatedBy = context.RequestedBy,
            RowVersion = [1],
            GenerationReservationId = result.Components.ReservationId
        };

        _context.ExternalFileNameRegistry.Add(registry);
        try
        {
            await _context.SaveChangesAsync(ct);
        }
        catch (DbUpdateException) when (result.Components.ReservationId.HasValue)
        {
            _context.Entry(registry).State = EntityState.Detached;
            if (await _context.ExternalFileNameRegistry.AsNoTracking().AnyAsync(
                    x => x.GenerationReservationId == result.Components.ReservationId.Value,
                    ct))
            {
                return;
            }

            throw;
        }

        foreach (var issue in result.Validation.Issues)
        {
            _context.ExternalFileNameValidationLog.Add(new ExternalFileNameValidationLog
            {
                RegistryId = registry.Id,
                ValidationStage = "ValidateExternalName",
                RuleCode = issue.RuleCode,
                Severity = issue.Disposition.ToString(),
                IssueCode = issue.IssueCode,
                IssueMessage = issue.Message,
                IssuePayloadJson = JsonSerializer.Serialize(new { issue.SourceReference, issue.Evidence }),
                CreatedAtUtc = auditTimestamp
            });
        }

        await _context.SaveChangesAsync(ct);
    }
}
