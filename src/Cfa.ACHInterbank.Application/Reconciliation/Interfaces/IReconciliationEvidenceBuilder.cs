using Cfa.ACHInterbank.Application.Reconciliation.Models;

namespace Cfa.ACHInterbank.Application.Reconciliation.Interfaces;

public interface IReconciliationEvidenceBuilder
{
    ReconciliationEvidenceResult Build(
        ReconciliationEvidenceRequest request,
        IEnumerable<ReconciliationEvidenceItem> items,
        IEnumerable<ReconciliationEvidenceAttachment> attachments,
        IEnumerable<ReconciliationEvidenceDifferenceLink> differenceLinks,
        IEnumerable<ReconciliationEvidenceReview> reviews);
}
