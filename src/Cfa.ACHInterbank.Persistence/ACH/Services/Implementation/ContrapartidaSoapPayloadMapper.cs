using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class ContrapartidaSoapPayloadMapper : IContrapartidaSoapPayloadMapper
{
    public IReadOnlyDictionary<string, object?> BuildProcContrapartidasPayload(
        AchCycle cycle,
        IReadOnlyCollection<AchTransaction> transactions,
        DateTime executionDateTime)
    {
        var txPayload = transactions.Select(t => new Dictionary<string, object?>
        {
            ["TransactionId"] = t.Id,
            ["AchCycleId"] = t.AchCycleId,
            ["AchBatchId"] = t.AchBatchId,
            ["Amount"] = t.Amount,
            ["Type"] = t.Type.ToString(),
            ["TransactionCode"] = t.TransactionCode,
            ["TraceNumber"] = t.TraceNumber,
            ["Reference"] = t.Reference,
            ["OriginatingDFI"] = t.OriginatingDFI,
            ["ReceivingDFI"] = t.ReceivingDFI,
            ["CompanyIdentification"] = t.CompanyIdentification,
            ["EffectiveEntryDate"] = t.EffectiveEntryDate,
            ["DestinationInstitutionId"] = t.DestinationInstitutionId,
            ["SourceInstitutionId"] = t.SourceInstitutionId,
            ["Addendas"] = t.Addendas
                .OrderBy(a => a.SequenceNumber)
                .Select(a => new Dictionary<string, object?>
                {
                    ["AddendaType"] = a.AddendaType,
                    ["BusinessType"] = a.BusinessType.ToString(),
                    ["Information"] = a.Information,
                    ["Purpose"] = a.Purpose,
                    ["Reference"] = a.Reference,
                    ["CollectorId"] = a.CollectorId,
                    ["ReceiverCustomerCode"] = a.ReceiverCustomerCode,
                    ["ServiceDescription"] = a.ServiceDescription,
                    ["ReturnReasonCode"] = a.ReturnReasonCode,
                    ["OriginalTraceNumber"] = a.OriginalTraceNumber,
                    ["NewTraceNumber"] = a.NewTraceNumber,
                    ["SequenceNumber"] = a.SequenceNumber
                })
                .ToList()
        }).ToList();

        return new Dictionary<string, object?>
        {
            ["ClearingHouseId"] = cycle.ClearingHouseId,
            ["ClearingHouseCode"] = cycle.ClearingHouse?.Code,
            ["CycleId"] = cycle.Id,
            ["CycleName"] = cycle.CycleName,
            ["ProcessingDate"] = cycle.ProcessingDate,
            ["StartTime"] = cycle.StartTime,
            ["EndTime"] = cycle.EndTime,
            ["CutoffTime"] = cycle.CutoffTime,
            ["ExecutionDateTime"] = executionDateTime,
            ["Transactions"] = txPayload
        };
    }
}
