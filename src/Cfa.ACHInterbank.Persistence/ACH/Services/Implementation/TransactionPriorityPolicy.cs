using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class TransactionPriorityPolicy : ITransactionPriorityPolicy
{
    private readonly IAchRegulatoryCatalogService _catalogService;
    private readonly AchDbContext? _context;

    public TransactionPriorityPolicy(IAchRegulatoryCatalogService catalogService, AchDbContext? context = null)
    {
        _catalogService = catalogService;
        _context = context;
    }

    public async Task<int> ResolvePriorityAsync(AchTransaction transaction, CancellationToken ct)
    {
        // DSP-152, Anexo 2 §5.1. Solo para una proyección informativa; no
        // replica ni ejecuta el algoritmo de optimización del Banco.
        // Sin SEC verificable, devolver 0 (no inferir prioridad financiera).
        if (_context is null || transaction.CompanyEntryDescriptionId <= 0)
            return 0;

        var sec = await _context.CompanyEntryDescriptionCatalogs.AsNoTracking()
            .Where(x => x.Id == transaction.CompanyEntryDescriptionId && x.IsActive)
            .Select(x => x.StandardEntryClassCode)
            .FirstOrDefaultAsync(ct);
        sec = sec?.Trim().ToUpperInvariant();
        if (sec is not ("PPD" or "CCD" or "CTX")) return 0;

        // Una devolución invierte el signo de la operación original. Cuando
        // falta el código NACHA verificable, no inferir tipo de devolución.
        if (transaction.Type == TransactionTypeEnum.Return)
        {
            var isReturnDebit = transaction.TransactionCode is "21" or "22" or "23" or "31" or "32" or "33" or "51" or "52" or "53";
            var isReturnCredit = transaction.TransactionCode is "26" or "27" or "28" or "36" or "37" or "38" or "55" or "56" or "57";
            if (isReturnDebit)
                return sec switch { "CCD" => 10, "PPD" => 9, _ => 0 };
            if (isReturnCredit)
                return sec switch { "CCD" => 8, "PPD" => 7, "CTX" => 6, _ => 0 };
            return 0;
        }

        return (sec, transaction.Type) switch
        {
            ("CCD", TransactionTypeEnum.Debit) => 5,
            ("PPD", TransactionTypeEnum.Debit) => 4,
            ("CCD", TransactionTypeEnum.Credit) => 3,
            ("PPD", TransactionTypeEnum.Credit) => 2,
            ("CTX", TransactionTypeEnum.Credit) => 1,
            _ => 0
        };
    }
}
