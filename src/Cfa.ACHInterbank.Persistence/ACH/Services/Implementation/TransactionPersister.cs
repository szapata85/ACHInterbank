using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Interfaces.Repositories;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class TransactionPersister : ITransactionPersister
{
    private readonly IAchTransactionRepository _transactionRepository;
    private readonly IAchBatchRepository _batchRepository;
    private readonly ITransactionValidator _validator;

    public TransactionPersister(
        IAchTransactionRepository transactionRepository,
        IAchBatchRepository batchRepository,
        ITransactionValidator validator)
    {
        _transactionRepository = transactionRepository;
        _batchRepository = batchRepository;
        _validator = validator;
    }

    public async Task<TransactionPersistResult> PersistAsync(AchTransactionRequestData request, TransactionBatchContext context, CancellationToken ct = default)
    {
        var effectiveType = request.IsPrenotification || request.Type == TransactionTypeEnum.Prenotification
            ? TransactionTypeEnum.Prenotification
            : request.Type;

        var transactionCode = _validator.ResolveTransactionCode(effectiveType, request.AccountType, request.IsPrenotification || effectiveType == TransactionTypeEnum.Prenotification);

        string traceOriginatingDfi = context.OriginatingDfi.Length >= 8
            ? context.OriginatingDfi[..8]
            : context.OriginatingDfi;

        if (traceOriginatingDfi.Length != 8 || traceOriginatingDfi.Any(c => !char.IsDigit(c)))
        {
            throw new InvalidOperationException("Error Fatal ID 7: el segmento de entidad del número de secuencia (posiciones 88-95) debe ser un código originador de 8 dígitos numéricos.");
        }

        var processingDate = context.EffectiveEntryDate.Date;
        int nextSeq = await _transactionRepository.GetMaxTraceSequenceAsync(processingDate, traceOriginatingDfi, ct) ?? 0;
        nextSeq++;

        if (nextSeq > 6_999_999)
        {
            throw new InvalidOperationException("Error Fatal ID 7: el consecutivo diario excede el máximo permitido (6999999). El rango 7000001-9999999 está reservado para PSE.");
        }

        var duplicateSequenceExists = await _transactionRepository.ExistsTraceSequenceAsync(processingDate, traceOriginatingDfi, nextSeq, ct);

        if (duplicateSequenceExists)
        {
            throw new InvalidOperationException($"Error Fatal ID 7: el consecutivo diario {nextSeq:0000000} ya fue utilizado para la entidad originadora {traceOriginatingDfi} en la fecha de proceso {processingDate:yyyy-MM-dd}.");
        }

        string traceNumber = $"{traceOriginatingDfi}{nextSeq.ToString().PadLeft(7, '0')}";

        var tx = new AchTransaction
        {
            Amount = request.Amount,
            Reference = request.Reference,
            Type = effectiveType,

            TransactionCode = transactionCode,
            ServiceClassCode = context.ServiceClassCode,
            CompanyEntryDescriptionId = context.CompanyEntryDescriptionId,
            CompanyName = context.CompanyName,
            CompanyIdentification = context.CompanyIdentification,

            OriginatingDFI = context.OriginatingDfi,
            ReceivingDFI = context.ReceivingDfi,

            TraceNumber = traceNumber,
            TraceSequenceNumber = nextSeq,

            EffectiveEntryDate = context.EffectiveEntryDate,
            AddendaRecordIndicator = true,
            IsPrenotification = effectiveType == TransactionTypeEnum.Prenotification || request.IsPrenotification,
            SlaDeadlineAtUtc = context.ReturnSlaDeadlineAtUtc,
            RecipientIdNumber = request.RecipientIdNumber?.Trim() ?? string.Empty,
            DiscretionaryData = effectiveType == TransactionTypeEnum.Credit && request.RequiresIdentityValidation ? "V" : string.Empty,

            SourceAccountNumber = request.SourceAccountNumber,
            DestinationAccountNumber = request.DestinationAccountNumber,

            SourceInstitutionId = context.SourceInstitutionId,
            DestinationInstitutionId = context.DestinationInstitutionId,

            AchCycleId = context.AchCycleId,
            AchBatch = context.Batch
        };

        if (request.Addendas != null)
        {
            tx.Addendas = request.Addendas
                .Select((a, idx) =>
                {
                    var normalized = _validator.NormalizeAndValidateAddenda(
                        a,
                        effectiveType,
                        effectiveType == TransactionTypeEnum.Prenotification || request.IsPrenotification,
                        context.CompanyEntryDescription);

                    return new AchTransactionAddenda
                    {
                        AddendaType = normalized.AddendaType,
                        BusinessType = normalized.BusinessType!.Value,
                        Information = normalized.Information,
                        Purpose = normalized.Purpose,
                        Reference = normalized.Reference,
                        CollectorId = normalized.CollectorId,
                        ReceiverCustomerCode = normalized.ReceiverCustomerCode,
                        ServiceDescription = normalized.ServiceDescription,
                        ReturnReasonCode = normalized.ReturnReasonCode,
                        OriginalTraceNumber = normalized.OriginalTraceNumber,
                        NewTraceNumber = normalized.NewTraceNumber,
                        SequenceNumber = idx + 1
                    };
                })
                .ToList();
        }

        await _transactionRepository.AddAsync(tx, ct);

        return new TransactionPersistResult
        {
            Transaction = tx,
            Batch = context.Batch
        };
    }

    public async Task UpdateBatchTotalsAsync(AchBatch batch, CancellationToken ct = default)
    {
        var totals = await _transactionRepository.GetTotalsByBatchAsync(batch, ct);

        decimal debit = totals
            .Where(t => t.Type is TransactionTypeEnum.Debit or TransactionTypeEnum.Return or TransactionTypeEnum.Reversal)
            .Select(t => t.Sum)
            .FirstOrDefault();

        decimal credit = totals
            .Where(t => t.Type is TransactionTypeEnum.Credit or TransactionTypeEnum.Prenotification)
            .Select(t => t.Sum)
            .FirstOrDefault();

        if (batch.TotalDebitAmount != debit || batch.TotalCreditAmount != credit)
        {
            batch.TotalDebitAmount = debit;
            batch.TotalCreditAmount = credit;
            await _batchRepository.UpdateAsync(batch, ct);
        }
    }

    public async Task UpdateBatchServiceClassCodeAsync(AchBatch batch, CancellationToken ct = default)
    {
        var transactions = await _transactionRepository.GetTypesByBatchAsync(batch, ct);

        if (!transactions.Any())
        {
            return;
        }

        bool allCredits = transactions.All(t => t is TransactionTypeEnum.Credit or TransactionTypeEnum.Prenotification);
        bool allDebits = transactions.All(t => t is TransactionTypeEnum.Debit or TransactionTypeEnum.Return or TransactionTypeEnum.Reversal);

        string newCode = allCredits ? "220" : allDebits ? "225" : "200";

        if (batch.ServiceClassCode != newCode)
        {
            batch.ServiceClassCode = newCode;
            await _batchRepository.UpdateAsync(batch, ct);
        }
    }
}
