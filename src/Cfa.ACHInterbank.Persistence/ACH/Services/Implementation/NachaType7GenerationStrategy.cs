using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class NachaType7GenerationStrategy : INachaType7GenerationStrategy
{
    private readonly INachaType7FieldValueResolver _fieldValueResolver;

    public NachaType7GenerationStrategy(INachaType7FieldValueResolver fieldValueResolver)
    {
        _fieldValueResolver = fieldValueResolver;
    }

    public IReadOnlyList<NachaType7RecordCandidate> BuildCandidates(IReadOnlyList<AchBatch> orderedBatches)
    {
        var result = new List<NachaType7RecordCandidate>(orderedBatches.Sum(x => x.Transactions.Count));

        foreach (var batch in orderedBatches)
        {
            foreach (var transaction in batch.Transactions.OrderBy(t => t.Id))
            {
                foreach (var addenda in BuildAddendasForTransaction(transaction))
                {
                    var values = _fieldValueResolver.Resolve(batch, transaction, addenda);
                    result.Add(new NachaType7RecordCandidate
                    {
                        Batch = batch,
                        Transaction = transaction,
                        Addenda = addenda,
                        FieldValues = values
                    });
                }
            }
        }

        return result;
    }

    private static IEnumerable<AchTransactionAddenda> BuildAddendasForTransaction(AchTransaction tx)
    {
        if (tx.Addendas != null && tx.Addendas.Any())
        {
            return tx.Addendas.OrderBy(a => a.SequenceNumber);
        }

        return new[]
        {
            new AchTransactionAddenda
            {
                AddendaType = "05",
                BusinessType = tx.Type is TransactionTypeEnum.Debit or TransactionTypeEnum.Return or TransactionTypeEnum.Reversal
                    ? AchAddendaBusinessType.Debit
                    : AchAddendaBusinessType.Credit,
                Purpose = tx.AchBatch?.CompanyEntryDescription,
                Reference = new string('0', 53),
                CollectorId = tx.Type is TransactionTypeEnum.Debit or TransactionTypeEnum.Return or TransactionTypeEnum.Reversal ? tx.CompanyIdentification : null,
                ReceiverCustomerCode = tx.Type is TransactionTypeEnum.Debit or TransactionTypeEnum.Return or TransactionTypeEnum.Reversal ? tx.RecipientIdNumber : null,
                ServiceDescription = tx.Type is TransactionTypeEnum.Debit or TransactionTypeEnum.Return or TransactionTypeEnum.Reversal
                    ? tx.AchBatch?.CompanyEntryDescription
                    : null,
                SequenceNumber = 1
            }
        };
    }
}
