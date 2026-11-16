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
            // Solo códigos de *devolución* NACHA-M: nunca tratar un pago
            // ordinario (22/27) ni una prenotificación (23/28) como devolución.
            // 21/31/51 abonan la devolución de un débito original;
            // 26/36/56 debitan la devolución de un crédito original.
            var isReturnDebit = transaction.TransactionCode is "21" or "31" or "51";
            var isReturnCredit = transaction.TransactionCode is "26" or "36" or "56";
            if (isReturnDebit)
                return sec switch { "CCD" => 10, "PPD" => 9, _ => 0 };
            if (isReturnCredit)
                return sec switch { "CCD" => 8, "PPD" => 7, "CTX" => 6, _ => 0 };
            return 0;
        }

        // SEC y tipo deben ser coherentes con el código NACHA-M. Una clase
        // desconocida o inconsistente no recibe prioridad por aproximación.
        var isOrdinaryCredit = transaction.TransactionCode is "22" or "32" or "42" or "52";
        var isOrdinaryDebit = transaction.TransactionCode is "27" or "37" or "55";
        if ((transaction.Type == TransactionTypeEnum.Credit && !isOrdinaryCredit)
            || (transaction.Type == TransactionTypeEnum.Debit && !isOrdinaryDebit))
            return 0;

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
