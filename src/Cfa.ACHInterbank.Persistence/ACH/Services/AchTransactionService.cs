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
    private readonly IAchCycleScheduler _achCycleScheduler;

    public AchTransactionService(AchDbContext context, IBankHoliday holidayService, IAchCycleScheduler achCycleScheduler)
    {
        _context = context;
        _holidayService = holidayService;
        _achCycleScheduler = achCycleScheduler;
    }

    public async Task<AchTransaction> RegisterTransactionAsync(
    decimal amount,
    string reference,
    TransactionTypeEnum type,
    int destinationInstitutionId,
    IEnumerable<(string addendaType, string information)>? addendas = null,
    CancellationToken ct = default)
    {
        // 🔹 Obtener institución de origen por defecto
        var defaultSource = await _context.FinancialInstitutions
            .Where(fi => fi.IsDefaultSource)              // usa tu criterio (columna bool o Code fijo)
            .FirstOrDefaultAsync(ct);

        if (defaultSource == null)
            throw new InvalidOperationException("No hay institución de origen predeterminada configurada.");

        // Validar destino
        var destExists = await _context.FinancialInstitutions
            .AnyAsync(fi => fi.Id == destinationInstitutionId, ct);
        if (!destExists)
            throw new InvalidOperationException("La institución de destino no existe.");

        // Buscar o crear próximo ciclo (igual que antes)
        var nextCycle = await GetOrCreateNextCycleAsync(ct);

        var tx = new AchTransaction
        {
            Amount = amount,
            Reference = reference,
            Type = type,
            SourceInstitutionId = defaultSource.Id,   // ✅ se toma de la BD
            DestinationInstitutionId = destinationInstitutionId,
            AchCycleId = nextCycle.Id
        };

        if (addendas != null)
        {
            tx.Addendas = addendas.Select(a => new AchTransactionAddenda
            {
                AddendaType = a.addendaType,
                Information = a.information
            }).ToList();
        }

        _context.AchTransactions.Add(tx);
        await _context.SaveChangesAsync(ct);

        return tx;
    }




    // Validar instituciones
    //bool sourceExists = await _context.FinancialInstitutions
    //    .AnyAsync(fi => fi.Id == sourceInstitutionId, ct);
    //bool destExists = await _context.FinancialInstitutions
    //    .AnyAsync(fi => fi.Id == destinationInstitutionId, ct);

    //if (!sourceExists || !destExists)
    //    throw new InvalidOperationException("La institución de origen o destino no existe.");

    /// <summary>
    /// Devuelve el próximo día hábil a partir de la fecha indicada,
    /// ignorando sábados, domingos y festivos.
    /// </summary>
    public DateTime GetNextBusinessDay(DateTime date)
    {
        var holidays = _holidayService.GetHolidays(date.Year)
                                      .Select(h => h.Date)
                                      .ToHashSet();

        var check = date.Date;
        do
        {
            check = check.AddDays(1);
        }
        while (check.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ||
               holidays.Contains(DateOnly.FromDateTime(check)));

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


    private async Task<AchCycle> GetOrCreateNextCycleAsync(CancellationToken ct)
    {
        var now = DateTime.Now;

        // 1️⃣ Buscar el siguiente ciclo ya existente
        var nextCycle = await _context.AchCycles
            .Where(c =>
                c.ProcessingDate > now.Date ||
                (c.ProcessingDate == now.Date && c.CutoffTime > now.TimeOfDay))
            .OrderBy(c => c.ProcessingDate)
            .ThenBy(c => c.CutoffTime)
            .FirstOrDefaultAsync(ct);

        if (nextCycle != null)
            return nextCycle;

        // 2️⃣ Calcular el próximo día hábil (sin sábados, domingos ni festivos)
        var nextBusinessDate = GetNextBusinessDay(now);

        // 3️⃣ Determinar la cámara compensadora por defecto
        var defaultClearingHouse = await _context.ClearingHouses
            .OrderBy(ch => ch.Id)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("No existe cámara de compensación predeterminada.");

        // 4️⃣ Tomar la hora de corte del Ciclo 1 desde la tabla de configuraciones
        var cycle1Config = await _context.ClearingHouseCycleConfigs
            .Where(cfg => cfg.ClearingHouseId == defaultClearingHouse.Id &&
                          cfg.CycleName == "Ciclo 1" && cfg.IsActive)
            .OrderByDescending(cfg => cfg.EffectiveFrom)
            .FirstOrDefaultAsync(ct);

        if (cycle1Config == null)
            throw new InvalidOperationException("No hay configuración activa para Ciclo 1.");

        // 5️⃣ Crear el ciclo si no existía
        nextCycle = new AchCycle
        {
            ClearingHouseId = defaultClearingHouse.Id,
            CycleName = "Ciclo 1",
            ProcessingDate = nextBusinessDate,
            CutoffTime = cycle1Config.CutoffTime,
            RescheduleOnHoliday = true
        };

        _context.AchCycles.Add(nextCycle);
        await _context.SaveChangesAsync(ct);

        return nextCycle;
    }

}
