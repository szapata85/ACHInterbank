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
    private readonly IRoutingStrategyService _routing;
    private readonly IBankHoliday _holidayService;

    public AchTransactionService(AchDbContext context,
                                 IRoutingStrategyService routing,
                                 IBankHoliday holidayService)
    {
        _context = context;
        _routing = routing;
        _holidayService = holidayService;
    }

    public async Task<AchTransaction> RegisterTransactionAsync(
        decimal amount,
        string reference,
        TransactionTypeEnum type,
        int destinationInstitutionId,
        IEnumerable<(string addendaType, string information)>? addendas = null,
        CancellationToken ct = default)
    {
        if (amount <= 0) throw new ArgumentException("El monto debe ser mayor a cero.");
        if (string.IsNullOrWhiteSpace(reference)) throw new ArgumentException("Referencia obligatoria.");

        // 1) Institución origen por defecto
        var sourceId = await _context.FinancialInstitutions
            .Where(fi => fi.IsDefaultSource)
            .Select(fi => fi.Id)
            .FirstOrDefaultAsync(ct);
        if (sourceId == 0)
            throw new InvalidOperationException("No existe institución de origen por defecto.");

        // 2) Resolver ciclo (el ruteo ya considera días hábiles)
        var now = DateTime.Now;
        var cycleId = await _routing.ResolveClearingHouseForTransactionAsync(
            destinationInstitutionId, now, ct);

        // 3) Transacción
        var tx = new AchTransaction
        {
            Amount = amount,
            Reference = reference,
            Type = type,
            SourceInstitutionId = sourceId,
            DestinationInstitutionId = destinationInstitutionId,
            AchCycleId = cycleId
        };

        _context.AchTransactions.Add(tx);
        await _context.SaveChangesAsync(ct);

        // 4) Addendas
        if (addendas != null)
        {
            foreach (var (addendaType, information) in addendas)
            {
                _context.Set<AchTransactionAddenda>().Add(new AchTransactionAddenda
                {
                    AchTransactionId = tx.Id,
                    AddendaType = addendaType,
                    Information = information
                });
            }
            await _context.SaveChangesAsync(ct);
        }

        return tx;
    }

    // ✅ Antes faltaba: próxima fecha hábil
    public Task<DateTime> GetNextBusinessDayAsync(DateTime baseDate, CancellationToken ct = default)
        => Task.FromResult(GetNextBusinessDay(baseDate));

    // ✅ Antes faltaba: transacciones por ciclo
    public async Task<IReadOnlyList<AchTransaction>> GetTransactionsByCycleAsync(
        int achCycleId,
        bool includeRelations = false,
        CancellationToken ct = default)
    {
        IQueryable<AchTransaction> q = _context.AchTransactions.AsNoTracking()
            .Where(t => t.AchCycleId == achCycleId);

        if (includeRelations)
        {
            q = q
                .Include(t => t.SourceInstitution)
                .Include(t => t.DestinationInstitution)
                .Include(t => t.Addendas);
        }

        return await q
            .OrderBy(t => t.Id)
            .ToListAsync(ct);
    }

    // ─────────────────────────────────────────────────────────────────────────────
    // Helpers privados
    // ─────────────────────────────────────────────────────────────────────────────
    private DateTime GetNextBusinessDay(DateTime startDate)
    {
        var date = startDate.Date;
        var currentYear = date.Year;
        var holidays = _holidayService.GetHolidays(currentYear)
                                      .Select(h => h.Date) // DateOnly
                                      .ToHashSet();

        while (true)
        {
            // refrescar festivos si cambia el año
            if (date.Year != currentYear)
            {
                currentYear = date.Year;
                holidays = _holidayService.GetHolidays(currentYear)
                                          .Select(h => h.Date)
                                          .ToHashSet();
            }

            var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            var isHoliday = holidays.Contains(DateOnly.FromDateTime(date));

            if (!isWeekend && !isHoliday)
                return date;

            date = date.AddDays(1);
        }
    }
}

