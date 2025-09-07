using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services;

[Scoped]
public class AchTransactionService : IAchTransactionService
{
    private readonly AchDbContext _context;

    public AchTransactionService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<AchTransaction> RegisterTransactionAsync(
        decimal amount,
        string reference,
        TransactionTypeEnum type,
        int sourceInstitutionId,
        int destinationInstitutionId,
        int achCycleId,
        IEnumerable<(string addendaType, string information)>? addendas = null,
        CancellationToken ct = default)
    {
        var tx = new AchTransaction
        {
            Amount = amount,
            Reference = reference,
            Type = type,
            SourceInstitutionId = sourceInstitutionId,
            DestinationInstitutionId = destinationInstitutionId,
            AchCycleId = achCycleId,
            Addendas = new List<AchTransactionAddenda>()
        };

        // 🔹 Si se pasaron addendas, agregarlas
        if (addendas != null)
        {
            foreach (var add in addendas)
            {
                tx.Addendas.Add(new AchTransactionAddenda
                {
                    AddendaType = add.addendaType,
                    Information = add.information
                });
            }
        }

        _context.AchTransactions.Add(tx);
        await _context.SaveChangesAsync(ct);

        return tx;
    }

    public async Task<List<AchTransaction>> GetTransactionsByCycleAsync(
        int achCycleId,
        CancellationToken ct = default)
    {
        return await _context.AchTransactions
            .Include(t => t.SourceInstitution)
            .Include(t => t.DestinationInstitution)
            .Include(t => t.Addendas) //incluir Addendas en la consulta
            .Where(t => t.AchCycleId == achCycleId)
            .ToListAsync(ct);
    }
}


