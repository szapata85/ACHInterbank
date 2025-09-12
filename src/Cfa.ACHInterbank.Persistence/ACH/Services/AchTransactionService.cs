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
        // Validar instituciones
        //bool sourceExists = await _context.FinancialInstitutions
        //    .AnyAsync(fi => fi.Id == sourceInstitutionId, ct);
        //bool destExists = await _context.FinancialInstitutions
        //    .AnyAsync(fi => fi.Id == destinationInstitutionId, ct);

        //if (!sourceExists || !destExists)
        //    throw new InvalidOperationException("La institución de origen o destino no existe.");


        var now = DateTime.Now;

        // 1️⃣ Buscar el próximo ciclo disponible
        var nextCycle = await _context.AchCycles
            .Where(c =>
                c.ProcessingDate > now.Date ||
                (c.ProcessingDate == now.Date && c.CutoffTime > now.TimeOfDay))
            .OrderBy(c => c.ProcessingDate)
            .ThenBy(c => c.CutoffTime)
            .FirstOrDefaultAsync(ct);

        // Si no existe un próximo ciclo, creamos uno en el siguiente día hábil
        if (nextCycle == null)
        {
            var nextBusinessDate = GetNextBusinessDay(now);

            // Buscar cámara de compensación predeterminada (o pásala como parámetro)
            var defaultClearingHouse = await _context.ClearingHouses
                .OrderBy(ch => ch.Id)
                .FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("No existe ninguna cámara de compensación.");

            // 🔹 Obtener la hora de corte del ciclo 1 de esta cámara
            var cycle1Config = await _context.ClearingHouseCycleConfigs
                .Where(c => c.ClearingHouseId == defaultClearingHouse.Id && c.CycleName == "Ciclo 1" && c.IsActive)
                .OrderByDescending(c => c.EffectiveFrom)
                .FirstOrDefaultAsync(ct);

            if (cycle1Config == null)
                throw new InvalidOperationException("No hay configuración de ciclo 1 para la cámara seleccionada.");

            nextCycle = new AchCycle
            {
                ClearingHouseId = defaultClearingHouse.Id,
                CycleName = "Ciclo 1",
                ProcessingDate = nextBusinessDate,
                CutoffTime = cycle1Config.CutoffTime, // 🔸 valor leído de la tabla
                RescheduleOnHoliday = true
            };

            _context.AchCycles.Add(nextCycle);
            await _context.SaveChangesAsync(ct);
        }


        // 3️⃣ Crear la transacción ligada al ciclo obtenido o creado
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

        // 4️⃣ Agregar addendas si se enviaron
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

    /// <summary>
    /// Devuelve el próximo día hábil a partir de la fecha indicada,
    /// ignorando sábados, domingos y festivos.
    /// </summary>
    private DateTime GetNextBusinessDay(DateTime date)
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
}
