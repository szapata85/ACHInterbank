using System.Globalization;
using System.Xml;
using System.Xml.Linq;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class ProcContrapartidasRequestMapper : IProcContrapartidasRequestMapper
{
    private static readonly XNamespace ActionNamespace = "http://tempuri.org/";
    private readonly IProcContrapartidasFunctionalMappingResolver _functionalResolver;

    public ProcContrapartidasRequestMapper(IProcContrapartidasFunctionalMappingResolver functionalResolver)
    {
        _functionalResolver = functionalResolver;
    }

    public ProcContrapartidasRequestContract Map(
        AchCycle cycle,
        IReadOnlyCollection<AchTransaction> transactions,
        DateTime executionDateTime)
    {
        ArgumentNullException.ThrowIfNull(cycle);
        ArgumentNullException.ThrowIfNull(transactions);

        ValidateCycle(cycle);

        if (transactions.Count == 0)
        {
            throw new InvalidOperationException("Proc_Contrapartidas requiere al menos una transacción.");
        }

        try
        {
            var configurable = _functionalResolver
                .TryResolveAsync(cycle, transactions, executionDateTime)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();
            if (configurable is not null)
            {
                return configurable;
            }
        }
        catch
        {
            // fallback controlado temporal al mapper hardcoded para no romper la operación actual
        }

        var txContracts = transactions
            .OrderBy(t => t.Id)
            .Select(MapTransaction)
            .ToList();

        return new ProcContrapartidasRequestContract
        {
            ClearingHouseId = cycle.ClearingHouseId,
            ClearingHouseCode = (cycle.ClearingHouse?.Code ?? string.Empty).Trim(),
            CycleId = cycle.Id.Trim(),
            CycleName = cycle.CycleName.Trim(),
            ProcessingDate = cycle.ProcessingDate,
            StartTime = cycle.StartTime,
            EndTime = cycle.EndTime,
            CutoffTime = cycle.CutoffTime,
            ExecutionDateTime = executionDateTime,
            Transactions = txContracts
        };
    }

    public string BuildSoapBody(ProcContrapartidasRequestContract request)
    {
        ArgumentNullException.ThrowIfNull(request);

        ValidateRequestContract(request);

        var body = new XElement(ActionNamespace + "Proc_Contrapartidas",
            new XElement(ActionNamespace + "ClearingHouseId", request.ClearingHouseId),
            new XElement(ActionNamespace + "ClearingHouseCode", request.ClearingHouseCode),
            new XElement(ActionNamespace + "CycleId", request.CycleId),
            new XElement(ActionNamespace + "CycleName", request.CycleName),
            new XElement(ActionNamespace + "ProcessingDate", request.ProcessingDate.ToString("O", CultureInfo.InvariantCulture)),
            new XElement(ActionNamespace + "StartTime", XmlConvert.ToString(request.StartTime)),
            new XElement(ActionNamespace + "EndTime", XmlConvert.ToString(request.EndTime)),
            new XElement(ActionNamespace + "CutoffTime", XmlConvert.ToString(request.CutoffTime)),
            new XElement(ActionNamespace + "ExecutionDateTime", request.ExecutionDateTime.ToString("O", CultureInfo.InvariantCulture)),
            new XElement(ActionNamespace + "Transactions",
                request.Transactions.Select(BuildTransactionElement)));

        return body.ToString(SaveOptions.DisableFormatting);
    }

    private static XElement BuildTransactionElement(ProcContrapartidasTransactionContract tx)
    {
        return new XElement(ActionNamespace + "item",
            new XElement(ActionNamespace + "TransactionId", tx.TransactionId),
            new XElement(ActionNamespace + "AchBatchId", tx.AchBatchId),
            new XElement(ActionNamespace + "AchCycleId", tx.AchCycleId),
            new XElement(ActionNamespace + "Amount", tx.Amount.ToString(CultureInfo.InvariantCulture)),
            new XElement(ActionNamespace + "Type", tx.Type),
            new XElement(ActionNamespace + "TransactionCode", tx.TransactionCode),
            new XElement(ActionNamespace + "TraceNumber", tx.TraceNumber),
            new XElement(ActionNamespace + "Reference", tx.Reference),
            new XElement(ActionNamespace + "OriginatingDFI", tx.OriginatingDfi),
            new XElement(ActionNamespace + "ReceivingDFI", tx.ReceivingDfi),
            new XElement(ActionNamespace + "CompanyIdentification", tx.CompanyIdentification),
            new XElement(ActionNamespace + "EffectiveEntryDate", tx.EffectiveEntryDate.ToString("O", CultureInfo.InvariantCulture)),
            new XElement(ActionNamespace + "SourceInstitutionId", tx.SourceInstitutionId),
            new XElement(ActionNamespace + "DestinationInstitutionId", tx.DestinationInstitutionId),
            new XElement(ActionNamespace + "Addendas", tx.Addendas.Select(BuildAddendaElement)));
    }

    private static XElement BuildAddendaElement(ProcContrapartidasAddendaContract addenda)
    {
        return new XElement(ActionNamespace + "item",
            new XElement(ActionNamespace + "SequenceNumber", addenda.SequenceNumber),
            new XElement(ActionNamespace + "AddendaType", addenda.AddendaType),
            new XElement(ActionNamespace + "BusinessType", addenda.BusinessType),
            new XElement(ActionNamespace + "Information", addenda.Information),
            new XElement(ActionNamespace + "Purpose", addenda.Purpose),
            new XElement(ActionNamespace + "Reference", addenda.Reference),
            new XElement(ActionNamespace + "CollectorId", addenda.CollectorId),
            new XElement(ActionNamespace + "ReceiverCustomerCode", addenda.ReceiverCustomerCode),
            new XElement(ActionNamespace + "ServiceDescription", addenda.ServiceDescription),
            new XElement(ActionNamespace + "ReturnReasonCode", addenda.ReturnReasonCode),
            new XElement(ActionNamespace + "OriginalTraceNumber", addenda.OriginalTraceNumber),
            new XElement(ActionNamespace + "NewTraceNumber", addenda.NewTraceNumber));
    }

    private static ProcContrapartidasTransactionContract MapTransaction(AchTransaction tx)
    {
        ValidateTransaction(tx);

        var addendas = tx.Addendas
            .OrderBy(a => a.SequenceNumber)
            .Select(a => new ProcContrapartidasAddendaContract
            {
                SequenceNumber = a.SequenceNumber,
                AddendaType = (a.AddendaType ?? string.Empty).Trim(),
                BusinessType = a.BusinessType.ToString(),
                Information = (a.Information ?? string.Empty).Trim(),
                Purpose = (a.Purpose ?? string.Empty).Trim(),
                Reference = (a.Reference ?? string.Empty).Trim(),
                CollectorId = (a.CollectorId ?? string.Empty).Trim(),
                ReceiverCustomerCode = (a.ReceiverCustomerCode ?? string.Empty).Trim(),
                ServiceDescription = (a.ServiceDescription ?? string.Empty).Trim(),
                ReturnReasonCode = (a.ReturnReasonCode ?? string.Empty).Trim(),
                OriginalTraceNumber = (a.OriginalTraceNumber ?? string.Empty).Trim(),
                NewTraceNumber = (a.NewTraceNumber ?? string.Empty).Trim()
            })
            .ToList();

        return new ProcContrapartidasTransactionContract
        {
            TransactionId = tx.Id,
            AchBatchId = tx.AchBatchId,
            AchCycleId = tx.AchCycleId.Trim(),
            Amount = tx.Amount,
            Type = tx.Type.ToString(),
            TransactionCode = (tx.TransactionCode ?? string.Empty).Trim(),
            TraceNumber = (tx.TraceNumber ?? string.Empty).Trim(),
            Reference = (tx.Reference ?? string.Empty).Trim(),
            OriginatingDfi = (tx.OriginatingDFI ?? string.Empty).Trim(),
            ReceivingDfi = (tx.ReceivingDFI ?? string.Empty).Trim(),
            CompanyIdentification = (tx.CompanyIdentification ?? string.Empty).Trim(),
            EffectiveEntryDate = tx.EffectiveEntryDate,
            SourceInstitutionId = tx.SourceInstitutionId,
            DestinationInstitutionId = tx.DestinationInstitutionId,
            Addendas = addendas
        };
    }

    private static void ValidateCycle(AchCycle cycle)
    {
        if (string.IsNullOrWhiteSpace(cycle.Id))
            throw new InvalidOperationException("CycleId es obligatorio para Proc_Contrapartidas.");

        if (string.IsNullOrWhiteSpace(cycle.CycleName))
            throw new InvalidOperationException($"CycleName es obligatorio para ciclo {cycle.Id}.");

        if (cycle.ClearingHouseId <= 0)
            throw new InvalidOperationException($"ClearingHouseId inválido para ciclo {cycle.Id}.");
    }

    private static void ValidateTransaction(AchTransaction tx)
    {
        if (tx.Id <= 0)
            throw new InvalidOperationException("TransactionId inválido para Proc_Contrapartidas.");

        if (tx.AchBatchId <= 0)
            throw new InvalidOperationException($"AchBatchId inválido para transacción {tx.Id}.");

        if (string.IsNullOrWhiteSpace(tx.AchCycleId))
            throw new InvalidOperationException($"AchCycleId obligatorio para transacción {tx.Id}.");

        if (string.IsNullOrWhiteSpace(tx.TraceNumber))
            throw new InvalidOperationException($"TraceNumber obligatorio para transacción {tx.Id}.");

        if (string.IsNullOrWhiteSpace(tx.TransactionCode))
            throw new InvalidOperationException($"TransactionCode obligatorio para transacción {tx.Id}.");

        if (string.IsNullOrWhiteSpace(tx.OriginatingDFI) || string.IsNullOrWhiteSpace(tx.ReceivingDFI))
            throw new InvalidOperationException($"DFI origen/destino obligatorios para transacción {tx.Id}.");

        if (string.IsNullOrWhiteSpace(tx.CompanyIdentification))
            throw new InvalidOperationException($"CompanyIdentification obligatorio para transacción {tx.Id}.");
    }

    private static void ValidateRequestContract(ProcContrapartidasRequestContract request)
    {
        if (request.Transactions.Count == 0)
            throw new InvalidOperationException("Proc_Contrapartidas no permite transacciones vacías.");

        if (string.IsNullOrWhiteSpace(request.CycleId) || string.IsNullOrWhiteSpace(request.CycleName))
            throw new InvalidOperationException("CycleId/CycleName son obligatorios para Proc_Contrapartidas.");

        if (request.ClearingHouseId <= 0)
            throw new InvalidOperationException("ClearingHouseId inválido en Proc_Contrapartidas.");
    }
}
