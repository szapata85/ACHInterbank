using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class TransactionPersister : ITransactionPersister
{
    private readonly AchDbContext _context;
    private readonly ITransactionValidator _validator;

    public TransactionPersister(AchDbContext context, ITransactionValidator validator)
    {
        _context = context;
        _validator = validator;
    }

    public async Task<TransactionPersistResult> PersistAsync(AchTransactionRequestData request, TransactionBatchContext context, CancellationToken ct = default)
    {
        var transactionCode = _validator.ResolveTransactionCode(request.Type, request.AccountType, request.IsPrenotification);

        string traceOriginatingDfi = context.OriginatingDfi.Length >= 8
            ? context.OriginatingDfi[..8]
            : context.OriginatingDfi;

        if (traceOriginatingDfi.Length != 8 || traceOriginatingDfi.Any(c => !char.IsDigit(c)))
        {
            throw new InvalidOperationException("Error Fatal ID 7: el segmento de entidad del número de secuencia (posiciones 88-95) debe ser un código originador de 8 dígitos numéricos.");
        }

        var processingDate = context.EffectiveEntryDate.Date;
        int nextSeq = await _context.AchTransactions
            .Where(t => t.EffectiveEntryDate.Date == processingDate)
            .Where(t => t.TraceNumber.StartsWith(traceOriginatingDfi))
            .Select(t => (int?)t.TraceSequenceNumber)
            .MaxAsync(ct) ?? 0;
        nextSeq++;

        if (nextSeq > 6_999_999)
        {
            throw new InvalidOperationException("Error Fatal ID 7: el consecutivo diario excede el máximo permitido (6999999). El rango 7000001-9999999 está reservado para PSE.");
        }

        var duplicateSequenceExists = await _context.AchTransactions
            .AnyAsync(t => t.EffectiveEntryDate.Date == processingDate
                           && t.TraceSequenceNumber == nextSeq
                           && t.TraceNumber.StartsWith(traceOriginatingDfi), ct);

        if (duplicateSequenceExists)
        {
            throw new InvalidOperationException($"Error Fatal ID 7: el consecutivo diario {nextSeq:0000000} ya fue utilizado para la entidad originadora {traceOriginatingDfi} en la fecha de proceso {processingDate:yyyy-MM-dd}.");
        }

        string traceNumber = $"{traceOriginatingDfi}{nextSeq.ToString().PadLeft(7, '0')}";

        var tx = new AchTransaction
        {
            Amount = request.Amount,
            Reference = request.Reference,
            Type = request.Type,

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
            IsPrenotification = request.IsPrenotification,
            SlaDeadlineAtUtc = context.ReturnSlaDeadlineAtUtc,
            RecipientIdNumber = request.RecipientIdNumber?.Trim() ?? string.Empty,
            DiscretionaryData = request.Type == TransactionTypeEnum.Credit && request.RequiresIdentityValidation ? "V" : string.Empty,

            SourceAccountNumber = request.SourceAccountNumber,
            DestinationAccountNumber = request.DestinationAccountNumber,

            SourceInstitutionId = context.SourceInstitutionId,
            DestinationInstitutionId = context.DestinationInstitutionId,

            AchCycleId = context.AchCycleId,
            AchBatchId = context.Batch.Id
        };

        if (request.Addendas != null)
        {
            tx.Addendas = request.Addendas.Select((a, idx) => new AchTransactionAddenda
            {
                AddendaType = _validator.ValidateAddendaType(a.AddendaType),
                Information = a.Information,
                SequenceNumber = idx + 1
            }).ToList();
        }

        _context.AchTransactions.Add(tx);
        await _context.SaveChangesAsync(ct);

        return new TransactionPersistResult
        {
            Transaction = tx,
            Batch = context.Batch
        };
    }

    public async Task UpdateBatchTotalsAsync(AchBatch batch, CancellationToken ct = default)
    {
        var totals = await _context.AchTransactions
            .Where(t => t.AchBatchId == batch.Id)
            .GroupBy(t => t.Type)
            .Select(g => new
            {
                Type = g.Key,
                Sum = g.Sum(t => t.Amount)
            })
            .ToListAsync(ct);

        decimal debit = totals
            .Where(t => t.Type == TransactionTypeEnum.Debit)
            .Select(t => t.Sum)
            .FirstOrDefault();

        decimal credit = totals
            .Where(t => t.Type == TransactionTypeEnum.Credit)
            .Select(t => t.Sum)
            .FirstOrDefault();

        if (batch.TotalDebitAmount != debit || batch.TotalCreditAmount != credit)
        {
            batch.TotalDebitAmount = debit;
            batch.TotalCreditAmount = credit;
            _context.AchBatches.Update(batch);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task UpdateBatchServiceClassCodeAsync(AchBatch batch, CancellationToken ct = default)
    {
        var transactions = await _context.AchTransactions
            .Where(t => t.AchBatchId == batch.Id)
            .Select(t => t.Type)
            .ToListAsync(ct);

        if (!transactions.Any())
        {
            return;
        }

        bool allCredits = transactions.All(t => t == TransactionTypeEnum.Credit);
        bool allDebits = transactions.All(t => t == TransactionTypeEnum.Debit);

        string newCode = allCredits ? "220" : allDebits ? "225" : "200";

        if (batch.ServiceClassCode != newCode)
        {
            batch.ServiceClassCode = newCode;
            _context.AchBatches.Update(batch);
            await _context.SaveChangesAsync(ct);
        }
    }
}
