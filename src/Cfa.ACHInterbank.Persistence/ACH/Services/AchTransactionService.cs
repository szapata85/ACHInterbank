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
    private readonly IBankHoliday _holidayService;

    public AchTransactionService(AchDbContext context, IBankHoliday holidayService)
    {
        _context = context;
        _holidayService = holidayService;
    }

    public async Task<AchTransaction> RegisterTransactionAsync(
        decimal amount,
        string reference,
        TransactionTypeEnum type,
        int sourceInstitutionId,
        int destinationInstitutionId,
        IEnumerable<(string addendaType, string information)>? addendas = null,
        CancellationToken ct = default)
    {
        // 1) Fecha de hoy, ajustada a día hábil
        var today = DateTime.Now;
        var processingDate = GetNextBusinessDay(today);

        // 2) Buscar próximo ciclo válido para hoy
        var nextCycle = await _context.AchCycles
            .Where(c =>
                c.ProcessingDate.Date == processingDate.Date &&
                c.CutoffTime > today.TimeOfDay)
            .OrderBy(c => c.CutoffTime)
            .FirstOrDefaultAsync(ct);

        if (nextCycle == null)
            throw new InvalidOperationException("No hay ciclos disponibles para la fecha actual.");

        // 3) Crear transacción
        var tx = new AchTransaction
        {
            Amount = amount,
            Reference = reference,
            Type = type,
            SourceInstitutionId = sourceInstitutionId,
            DestinationInstitutionId = destinationInstitutionId,
            AchCycleId = nextCycle.Id,
            Addendas = new List<AchTransactionAddenda>()
        };

        // 4) Registrar addendas si hay
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

    // 🔹 Reutilizar servicio de festivos
    private DateTime GetNextBusinessDay(DateTime date)
    {
        var holidays = _holidayService.GetHolidays(date.Year)
                                      .Select(h => h.Date)
                                      .ToHashSet();

        var check = date.Date;
        while (check.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ||
               holidays.Contains(DateOnly.FromDateTime(check)))
        {
            check = check.AddDays(1);
        }
        return check;
    }

    public async Task<List<AchTransaction>> GetTransactionsByCycleAsync(
        int achCycleId,
        CancellationToken ct = default)
    {
        return await _context.AchTransactions
            .Include(t => t.SourceInstitution)
            .Include(t => t.DestinationInstitution)
            .Include(t => t.Addendas)
            .Where(t => t.AchCycleId == achCycleId)
            .ToListAsync(ct);
    }
}


