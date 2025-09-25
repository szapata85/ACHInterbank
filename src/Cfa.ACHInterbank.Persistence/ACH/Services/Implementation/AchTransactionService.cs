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
    private readonly IRoutingStrategyService _routing;
    private readonly IBankHoliday _holidayService;

    public AchTransactionService(
        AchDbContext context,
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
        string sourceAccountNumber,
        string destinationAccountNumber,
        IEnumerable<(string addendaType, string information)>? addendas = null,
        CancellationToken ct = default)
    {
        if (amount <= 0) throw new ArgumentException("El monto debe ser mayor a cero.", nameof(amount));
        if (string.IsNullOrWhiteSpace(reference)) throw new ArgumentException("Referencia obligatoria.", nameof(reference));
        if (string.IsNullOrWhiteSpace(sourceAccountNumber)) throw new ArgumentException("Cuenta de origen obligatoria.", nameof(sourceAccountNumber));
        if (string.IsNullOrWhiteSpace(destinationAccountNumber)) throw new ArgumentException("Cuenta de destino obligatoria.", nameof(destinationAccountNumber));

        // 1) Institución de origen por defecto y ACTIVA
        var sourceInstitution = await _context.FinancialInstitutions
            .AsNoTracking()
            .FirstOrDefaultAsync(fi => fi.IsDefaultSource && fi.Status == FinancialInstitutionStatus.Active, ct)
            ?? throw new InvalidOperationException("No existe institución de origen por defecto activa.");

        // 2) Resolver ciclo (el ruteo ya maneja días hábiles y prioridades de cámara)
        int nextCycleId = await _routing.ResolveClearingHouseForTransactionAsync(
            destinationInstitutionId, DateTime.UtcNow, ct);

        // 3) Crear/Reusar lote para ese ciclo
        var batch = await _context.AchBatches
            .FirstOrDefaultAsync(b => b.AchCycleId == nextCycleId, ct);

        if (batch == null)
        {
            var companyIdentification = ComposeCompanyIdentification(sourceInstitution);
            batch = new AchBatch
            {
                AchCycleId = nextCycleId,
                CompanyName = sourceInstitution.Name,
                CompanyIdentification = companyIdentification,
                EffectiveEntryDate = DateTime.UtcNow.Date
            };
            _context.AchBatches.Add(batch);
            await _context.SaveChangesAsync(ct);
        }

        // 4) Crear transacción
        var transaction = new AchTransaction
        {
            Amount = amount,
            Reference = reference,
            Type = type,
            SourceInstitutionId = sourceInstitution.Id,
            SourceAccountNumber = sourceAccountNumber,
            DestinationInstitutionId = destinationInstitutionId,
            DestinationAccountNumber = destinationAccountNumber,
            AchCycleId = nextCycleId,
            AchBatchId = batch.Id
        };

        // 5) Addendas (opcional)
        if (addendas != null)
        {
            transaction.Addendas = addendas.Select(a => new AchTransactionAddenda
            {
                AddendaType = a.addendaType,
                Information = a.information
            }).ToList();
        }

        _context.AchTransactions.Add(transaction);
        await _context.SaveChangesAsync(ct);

        return transaction;
    }

    public Task<DateTime> GetNextBusinessDayAsync(DateTime baseDate, CancellationToken ct = default)
        => Task.FromResult(GetNextBusinessDay(baseDate));

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

        return await q.OrderBy(t => t.Id).ToListAsync(ct);
    }

    // ───────────────────────────────────────────────
    // Helpers privados
    // ───────────────────────────────────────────────

    /// Construye el CompanyIdentification desde la entidad de origen.
    /// Usa el CheckDigit ya almacenado o lo calcula en caliente si viniera vacío.
    private static string ComposeCompanyIdentification(FinancialInstitution fi)
    {
        // CheckDigit tiene private set; si por alguna razón viene vacío, lo calculamos localmente
        string checkDigit = string.IsNullOrWhiteSpace(fi.CheckDigit)
            ? Domain.Helpers.DigitoChequeoHelper.CalcularDigitoChequeo($"{fi.RoutingNumber}{fi.TransitCode}")
            : fi.CheckDigit;

        return $"{fi.RoutingNumber}{fi.TransitCode}{checkDigit}";
    }

    private DateTime GetNextBusinessDay(DateTime startDate)
    {
        var date = startDate.Date;
        var currentYear = date.Year;
        var holidays = _holidayService.GetHolidays(currentYear)
                                      .Select(h => h.Date)
                                      .ToHashSet();

        while (true)
        {
            if (date.Year != currentYear)
            {
                currentYear = date.Year;
                holidays = _holidayService.GetHolidays(currentYear)
                                          .Select(h => h.Date)
                                          .ToHashSet();
            }

            bool isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            bool isHoliday = holidays.Contains(DateOnly.FromDateTime(date));

            if (!isWeekend && !isHoliday)
                return date;

            date = date.AddDays(1);
        }
    }
}
