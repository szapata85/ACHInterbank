using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

internal sealed class AchReturnStateTransitionComponent(
    IAchStateTransitionService stateTransitionService,
    AchReturnAuditMonitoringComponent auditMonitoring)
{
    public async Task ApplyAsync(
        AchReturnAssemblyResult assembly,
        IReadOnlyDictionary<int, AchTransaction> transactionsById,
        IReadOnlyDictionary<int, AchReturnPreparedItem> preparedById,
        AchReturnGenerationScope scope,
        AchReturnArtifactResult artifact,
        DateTime now,
        CancellationToken ct)
    {
        foreach (var row in assembly.GeneratedRows)
        {
            var originalTx = transactionsById[row.OriginalTransactionId];
            var payload = auditMonitoring.BuildReturnFileGeneratedPayload(
                originalTx,
                row,
                scope.ReturnCycle,
                artifact.FileName,
                artifact.RecordCount,
                assembly.GeneratedRows.Count,
                now,
                artifact.Content,
                AchTransferStateEnum.ReturnedByEpr,
                preparedById[row.OriginalTransactionId].AchColombiaR10Basis);

            var transition = await stateTransitionService.TransitionAsync(new AchStateTransitionRequest(
                row.OriginalTransactionId,
                AchTransferStateEnum.ReturnedByEpr,
                AchStateEventSourceEnum.Epr,
                row.ReturnReasonCode,
                payload,
                row.OriginalSequenceNumber,
                now,
                $"outbound-return-v1:{row.OriginalTransactionId}",
                scope.ReturnCycle.ClearingHouseId), ct);
            if (transition.WasDuplicate)
            {
                throw new AchReturnAlreadyGeneratedException([row.OriginalTransactionId]);
            }
        }
    }
}
