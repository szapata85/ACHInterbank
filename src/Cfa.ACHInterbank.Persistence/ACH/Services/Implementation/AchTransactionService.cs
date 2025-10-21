using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class AchTransactionService : IAchTransactionService
{
    private readonly AchDbContext _context;
    private readonly IBankHoliday _holidayService;
    private readonly IRoutingStrategyService _routing;

    public AchTransactionService(AchDbContext context,
                                 IBankHoliday holidayService,
                                 IRoutingStrategyService routing)
    {
        _context = context;
        _holidayService = holidayService;
        _routing = routing;
    }

    /// <summary>
    /// Registra una transacción completa alineada al estándar NACHA-M.
    /// </summary>
    public async Task<AchTransaction> RegisterTransactionAsync(
        decimal amount,
        string reference,
        TransactionTypeEnum type,
        int destinationInstitutionId,
        string sourceAccountNumber,
        string destinationAccountNumber,
        string companyName,
        string companyIdentification,
        string companyEntryDescription,
        IEnumerable<(string addendaType, string information)>? addendas = null,
        CancellationToken ct = default)
    {
        if (amount <= 0)
            throw new ArgumentException("El monto debe ser mayor a cero.");

        if (string.IsNullOrWhiteSpace(reference))
            throw new ArgumentException("Referencia obligatoria.");

        // 1️⃣ Obtener institución de origen por defecto
        var sourceInstitution = await _context.FinancialInstitutions
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.IsDefaultSource, ct)
            ?? throw new InvalidOperationException("No existe institución de origen por defecto.");

        // 2️⃣ Validar institución destino
        var destInstitution = await _context.FinancialInstitutions
            .AsNoTracking()
            .FirstOrDefaultAsync(f => f.Id == destinationInstitutionId, ct)
            ?? throw new InvalidOperationException("Institución destino no encontrada.");

        // 3️⃣ Resolver automáticamente cámara y ciclo
        int achCycleId = await _routing.ResolveClearingHouseForTransactionAsync(
            destinationInstitutionId, DateTime.Now, ct);

        var achCycle = await _context.AchCycles
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == achCycleId, ct)
            ?? throw new InvalidOperationException("No se encontró el ciclo asignado.");

        // 4️⃣ Calcular fecha hábil efectiva del ciclo
        var effectiveEntryDate = await GetNextBusinessDayAsync(achCycle.ProcessingDate, ct);

        // 5️⃣ Buscar o crear lote asociado al ciclo
        var existingBatch = await _context.AchBatches
            .FirstOrDefaultAsync(b =>
                b.AchCycleId == achCycleId &&
                b.CompanyIdentification == companyIdentification, ct);

        if (existingBatch == null)
        {
            existingBatch = new AchBatch
            {
                AchCycleId = achCycleId,
                CompanyName = companyName,
                CompanyIdentification = companyIdentification,
                CompanyEntryDescription = companyEntryDescription,
                EffectiveEntryDate = effectiveEntryDate,
                OriginOrOdfi = $"{sourceInstitution.RoutingNumber}{sourceInstitution.TransitCode}",
                ServiceClassCode = "220",
                TotalCreditAmount = 0,
                TotalDebitAmount = 0
            };

            _context.AchBatches.Add(existingBatch);
            await _context.SaveChangesAsync(ct);
        }

        // 6️⃣ Generar TraceNumber único
        string traceBase = $"{sourceInstitution.RoutingNumber}{sourceInstitution.TransitCode}";
        int seq = await _context.AchTransactions.CountAsync(ct) + 1;
        string traceNumber = $"{traceBase}{seq:D7}".Substring(0, 15);

        // 7️⃣ Crear transacción
        var transaction = new AchTransaction
        {
            Amount = amount,
            Reference = reference,
            Type = type,
            SourceInstitutionId = sourceInstitution.Id,
            DestinationInstitutionId = destinationInstitutionId,
            SourceAccountNumber = sourceAccountNumber,
            DestinationAccountNumber = destinationAccountNumber,
            CompanyName = companyName,
            CompanyIdentification = companyIdentification,
            CompanyEntryDescription = companyEntryDescription,
            OriginatingDFI = traceBase,
            ReceivingDFI = $"{destInstitution.RoutingNumber}{destInstitution.TransitCode}",
            TraceNumber = traceNumber,
            TraceSequenceNumber = seq,
            TransactionCode = type == TransactionTypeEnum.Credit ? "22" : "27",
            EffectiveEntryDate = effectiveEntryDate,
            AddendaRecordIndicator = addendas != null && addendas.Any(),
            AchBatchId = existingBatch.Id,
            AchCycleId = achCycleId
        };

        // 8️⃣ Registrar addendas (si existen)
        if (addendas != null)
        {
            int i = 1;
            transaction.Addendas = addendas.Select(a => new AchTransactionAddenda
            {
                AddendaType = a.addendaType,
                Information = a.information,
                SequenceNumber = i++,
                EntryDetailSequenceNumber = transaction.TraceSequenceNumber
            }).ToList();
        }

        // 9️⃣ Persistir y actualizar totales
        _context.AchTransactions.Add(transaction);

        if (type == TransactionTypeEnum.Debit)
            existingBatch.TotalDebitAmount += amount;
        else
            existingBatch.TotalCreditAmount += amount;

        await _context.SaveChangesAsync(ct);
        return transaction;
    }

    /// <summary>
    /// Obtiene la próxima fecha hábil.
    /// </summary>
    public Task<DateTime> GetNextBusinessDayAsync(DateTime baseDate, CancellationToken ct = default)
        => Task.FromResult(GetNextBusinessDay(baseDate));

    /// <summary>
    /// Consulta todas las transacciones por ciclo.
    /// </summary>
    public async Task<IReadOnlyList<AchTransaction>> GetTransactionsByCycleAsync(
        int achCycleId, bool includeRelations = false, CancellationToken ct = default)
    {
        IQueryable<AchTransaction> query = _context.AchTransactions
            .AsNoTracking()
            .Where(t => t.AchCycleId == achCycleId);

        if (includeRelations)
        {
            query = query
                .Include(t => t.SourceInstitution)
                .Include(t => t.DestinationInstitution)
                .Include(t => t.Addendas)
                .Include(t => t.AchBatch);
        }

        return await query.OrderBy(t => t.Id).ToListAsync(ct);
    }

    /// <summary>
    /// Calcula la próxima fecha hábil interna.
    /// </summary>
    private DateTime GetNextBusinessDay(DateTime startDate)
    {
        var date = startDate.Date;
        var holidays = _holidayService.GetHolidays(date.Year)
            .Select(h => h.Date)
            .ToHashSet();

        while (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday ||
               holidays.Contains(DateOnly.FromDateTime(date)))
        {
            date = date.AddDays(1);
        }

        return date;
    }
}
