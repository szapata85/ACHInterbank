using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class NachaFileBuilder : INachaFileBuilder
{
    private readonly AchDbContext _context;

    public NachaFileBuilder(AchDbContext context)
    {
        _context = context;
    }

    public async Task<byte[]> BuildNachaFileAsync(int achCycleId, CancellationToken ct = default)
    {
        // 1. Traer ciclo e instituciones con transacciones
        var cycle = await _context.AchCycles
            .Include(c => c.Transactions!)
                .ThenInclude(t => t.SourceInstitution)
            .Include(c => c.Transactions!)
                .ThenInclude(t => t.DestinationInstitution)
            .Include(c => c.Transactions!)
                .ThenInclude(t => t.Addendas)
            .FirstOrDefaultAsync(c => c.Id == achCycleId, ct)
            ?? throw new InvalidOperationException($"Ciclo {achCycleId} no encontrado.");

        var transactions = cycle.Transactions.ToList();
        if (!transactions.Any())
            throw new InvalidOperationException("No hay transacciones para este ciclo.");

        var sb = new StringBuilder();

        // 2. Header NACHA
        sb.AppendLine(BuildFileHeader(cycle));

        // 3. Lotes: en este ejemplo un solo batch
        sb.AppendLine(BuildBatchHeader(cycle));

        int entryHash = 0;
        int addendaCount = 0;

        foreach (var t in transactions)
        {
            sb.AppendLine(BuildEntryDetail(t));
            entryHash += int.Parse(t.DestinationInstitution!.RoutingNumber);
            if (t.Addendas != null)
            {
                foreach (var add in t.Addendas)
                {
                    sb.AppendLine(BuildAddendaRecord(add));
                    addendaCount++;
                }
            }
        }

        // 4. Batch Control
        sb.AppendLine(BuildBatchControl(transactions, entryHash, addendaCount));

        // 5. File Control
        sb.AppendLine(BuildFileControl(transactions, entryHash, addendaCount));

        return Encoding.ASCII.GetBytes(sb.ToString());
    }

    private string BuildFileHeader(AchCycle cycle)
    {
        // Ajusta campos según especificación NACHA
        string immediateDestination = "ACH"; // Ejemplo
        string immediateOrigin = "CFA"; // Ejemplo
        string fileIdModifier = "A";
        return $"101{immediateDestination.PadLeft(10)}{immediateOrigin.PadLeft(10)}" +
               $"{cycle.ProcessingDate:yyMMdd}{cycle.CutoffTime:hhmm}{fileIdModifier}094101";
    }

    private string BuildBatchHeader(AchCycle cycle)
    {
        string companyName = "CFA";
        string companyId = "123456789";
        return $"5200{companyName.PadRight(16)}PPD{companyId.PadLeft(10)}{cycle.ProcessingDate:yyMMdd}{cycle.CutoffTime:hhmm}1";
    }

    private string BuildEntryDetail(AchTransaction t)
    {
        string tranCode = t.Type == TransactionTypeEnum.Credit ? "22" : "27";
        string routing = t.DestinationInstitution!.RoutingNumber + t.DestinationInstitution.CheckDigit;

        // ✅ Ahora usamos las cuentas reales
        string account = t.DestinationAccountNumber.PadRight(17);
        string amount = ((int)(t.Amount * 100)).ToString().PadLeft(10, '0');

        // Origin/Destination references
        string individualId = t.SourceAccountNumber.PadRight(15);

        return $"6{tranCode}{routing}{account}{amount}{individualId}  ";
    }


    private string BuildAddendaRecord(AchTransactionAddenda add)
    {
        // Record type 7
        return $"7{add.AddendaType.PadRight(2)}{add.Information.PadRight(80)}";
    }

    private string BuildBatchControl(IEnumerable<AchTransaction> txs, int entryHash, int addendaCount)
    {
        decimal totalDebit = txs.Where(t => t.Type == TransactionTypeEnum.Debit).Sum(t => t.Amount);
        decimal totalCredit = txs.Where(t => t.Type == TransactionTypeEnum.Credit).Sum(t => t.Amount);
        return $"8200{txs.Count():000000}{entryHash:0000000000}" +
               $"{(int)(totalDebit * 100):000000000000}{(int)(totalCredit * 100):000000000000}";
    }

    private string BuildFileControl(IEnumerable<AchTransaction> txs, int entryHash, int addendaCount)
    {
        int blockCount = (int)Math.Ceiling((txs.Count() + addendaCount + 4) / 10.0);
        decimal totalDebit = txs.Where(t => t.Type == TransactionTypeEnum.Debit).Sum(t => t.Amount);
        decimal totalCredit = txs.Where(t => t.Type == TransactionTypeEnum.Credit).Sum(t => t.Amount);
        return $"9000001{txs.Count():000000}{entryHash:0000000000}" +
               $"{(int)(totalDebit * 100):000000000000}{(int)(totalCredit * 100):000000000000}";
    }
}

