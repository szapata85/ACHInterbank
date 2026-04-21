using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces.ExternalFileNames;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Models.ACH.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;

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
            CreatedAtUtc = DateTime.UtcNow,
            CreatedBy = context.RequestedBy,
            RowVersion = [1]
        };

        _context.ExternalFileNameRegistry.Add(registry);
        await _context.SaveChangesAsync(ct);

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
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync(ct);
    }
}
