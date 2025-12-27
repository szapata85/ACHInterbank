using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Helpers;
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
        IEnumerable<AddendaDto>? addendas = null,
        CancellationToken ct = default)
    {
        if (amount <= 0) throw new ArgumentException("El monto debe ser mayor a cero.", nameof(amount));
        if (string.IsNullOrWhiteSpace(reference)) throw new ArgumentException("La referencia es obligatoria.", nameof(reference));

        // 1) Institución origen por defecto (activa)
        var source = await _context.FinancialInstitutions
            .AsNoTracking()
            .Where(fi => fi.IsDefaultSource && fi.Status == FinancialInstitutionStatus.Active)
            .Select(fi => new
            {
                fi.Id,
                fi.Name,
                fi.RoutingNumber,
                fi.TransitCode,
                fi.CheckDigit
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("No existe institución de origen por defecto y activa.");

        string sourceRouting = source.RoutingNumber?.Trim() ?? string.Empty;
        string sourceTransit = source.TransitCode?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(sourceRouting) || string.IsNullOrWhiteSpace(sourceTransit))
            throw new InvalidOperationException("La institución de origen no tiene configurado el código de ruteo/transito.");

        string originBase = $"{sourceRouting}{sourceTransit}";
        if (originBase.Length != 8)
            throw new InvalidOperationException($"La institución de origen tiene una longitud inválida para el ruteo: {originBase}.");

        string sourceCheckDigit = string.IsNullOrWhiteSpace(source.CheckDigit)
            ? DigitoChequeoHelper.CalcularDigitoChequeo(originBase)
            : source.CheckDigit.Trim();

        // 2) Institución destino (activa)
        var dest = await _context.FinancialInstitutions
            .AsNoTracking()
            .Where(fi => fi.Id == destinationInstitutionId && fi.Status == FinancialInstitutionStatus.Active)
            .Select(fi => new
            {
                fi.Id,
                fi.RoutingNumber,
                fi.TransitCode,
                fi.CheckDigit
            })
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("Institución destino no encontrada o inactiva.");

        string destRouting = dest.RoutingNumber?.Trim() ?? string.Empty;
        string destTransit = dest.TransitCode?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(destRouting) || string.IsNullOrWhiteSpace(destTransit))
            throw new InvalidOperationException("La institución destino no tiene configurado el código de ruteo/transito.");

        string destinationBase = $"{destRouting}{destTransit}";
        if (destinationBase.Length != 8)
            throw new InvalidOperationException($"La institución destino tiene una longitud inválida para el ruteo: {destinationBase}.");

        // 3) Ruteo + fecha efectiva alineada al ciclo ACH
        var now = DateTime.Now;
        string achCycleId = await _routing.ResolveClearingHouseForTransactionAsync(destinationInstitutionId, now, ct);
        var cycle = await _context.AchCycles
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == achCycleId, ct)
            ?? throw new InvalidOperationException("No se encontró el ciclo ACH para la transacción.");
        DateTime effectiveEntryDate = cycle.ProcessingDate.Date;

        // 4) Determinar/crear el lote (para este ciclo + compañía/identificación)
        string companyName = source.Name;
        string companyIdentification = $"{originBase}{sourceCheckDigit}";
        string companyEntryDescription = "PAGOS"; // puedes parametrizar por tipo si lo deseas

        // Intentamos reutilizar un lote del mismo ciclo/compañía/fecha
        var batch = await _context.AchBatches
            .FirstOrDefaultAsync(b =>
                b.AchCycleId == achCycleId &&
                b.CompanyName == companyName &&
                b.CompanyIdentification == companyIdentification &&
                b.EffectiveEntryDate == effectiveEntryDate, ct);

        if (batch is null)
        {
            batch = new AchBatch
            {
                AchCycleId = achCycleId,
                CompanyName = companyName,
                CompanyIdentification = companyIdentification,
                EffectiveEntryDate = effectiveEntryDate,
                OriginOrOdfi = originBase
            };
            _context.AchBatches.Add(batch);
            await _context.SaveChangesAsync(ct);
        }

        // 5) Calcular campos NACHA-m por defecto:
        // ServiceClassCode: “200” mixed (débitos+créditos) o “220” (créditos) / “225” (débitos)
        // TransactionCode: ejemplo “22” (credit to checking) ó “27” (debit to checking)
        // OriginatingDFI: 8 dígitos del ODFI (sin check digit)
        // ReceivingDFI : 8 dígitos del RDFI (sin check digit)
        string serviceClass = "200"; // Mixed por defecto (puedes variarlo por lote/empresa)
        string transactionCode = type == TransactionTypeEnum.Credit ? "22" : "27";

        string originatingDfi = originBase;
        string receivingDfi = destinationBase;

        // Trace: suele ser ODFI(8) + 7 secuencia; aquí generamos una secuencia simple por lote.
        int nextSeq = await _context.AchTransactions
            .Where(t => t.AchBatchId == batch.Id)
            .Select(t => (int?)t.TraceSequenceNumber)
            .MaxAsync(ct) ?? 0;
        nextSeq++;
        string traceNumber = $"{originatingDfi}{nextSeq.ToString().PadLeft(7, '0')}";

        // 6) Crear la transacción completa alineada a tu modelo extendido
        var tx = new AchTransaction
        {
            Amount = amount,
            Reference = reference,
            Type = type,

            TransactionCode = transactionCode,
            ServiceClassCode = serviceClass,
            CompanyEntryDescription = companyEntryDescription,
            CompanyName = companyName,
            CompanyIdentification = companyIdentification,

            OriginatingDFI = originatingDfi,
            ReceivingDFI = receivingDfi,

            TraceNumber = traceNumber,
            TraceSequenceNumber = nextSeq,

            EffectiveEntryDate = effectiveEntryDate,
            AddendaRecordIndicator = (addendas != null && addendas.Any()),

            SourceAccountNumber = sourceAccountNumber,
            DestinationAccountNumber = destinationAccountNumber,

            SourceInstitutionId = source.Id,
            DestinationInstitutionId = dest.Id,

            AchCycleId = achCycleId,
            AchBatchId = batch.Id
        };

        if (addendas != null)
        {
            tx.Addendas = addendas.Select((a, idx) => new AchTransactionAddenda
            {
                AddendaType = a.AddendaType,
                Information = a.Information,
                SequenceNumber = idx + 1
            }).ToList();
        }

        _context.AchTransactions.Add(tx);
        await _context.SaveChangesAsync(ct);

        await UpdateBatchTotalsAsync(batch, ct);
        // 🔁 Recalcular el ServiceClassCode del lote si aplica
        await UpdateBatchServiceClassCodeAsync(batch, ct);


        return tx;
    }

    public Task<DateTime> GetNextBusinessDayAsync(DateTime baseDate, CancellationToken ct = default)
    {
        var date = baseDate.Date;
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

            bool weekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
            bool holiday = holidays.Contains(DateOnly.FromDateTime(date));

            if (!weekend && !holiday)
                return Task.FromResult(date);

            date = date.AddDays(1);
        }
    }


    public async Task<IReadOnlyList<AchTransaction>> GetTransactionsByCycleAsync(
        string achCycleId, bool includeRelations = false, CancellationToken ct = default)
    {
        IQueryable<AchTransaction> q = _context.AchTransactions.AsNoTracking()
            .Where(t => t.AchCycleId == achCycleId);

        if (includeRelations)
        {
            q = q
                .Include(t => t.SourceInstitution)
                .Include(t => t.DestinationInstitution)
                .Include(t => t.Addendas)
                .Include(t => t.AchBatch); // ← importante: el nombre correcto es AchBatch
        }

        return await q.OrderBy(t => t.Id).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<AchTransactionListDto>> GetAllAsync(
        string? achCycleId = default,
        DateTime? effectiveDate = default,
        int? clearingHouseId = default,
        CancellationToken ct = default)
    {
        var query = _context.AchTransactions
            .AsNoTracking()
            .Include(t => t.AchBatch)
            .Include(t => t.AchCycle)
            .ThenInclude(cycle => cycle.ClearingHouse)
            .Include(t => t.SourceInstitution)
            .Include(t => t.DestinationInstitution)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(achCycleId))
        {
            query = query.Where(t => t.AchCycleId == achCycleId);
        }

        if (effectiveDate.HasValue)
        {
            var targetDate = effectiveDate.Value.Date;
            query = query.Where(t => t.EffectiveEntryDate.Date == targetDate);
        }

        if (clearingHouseId.HasValue)
        {
            query = query.Where(t => t.AchCycle.ClearingHouseId == clearingHouseId.Value);
        }

        return await query
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new AchTransactionListDto
            {
                Id = t.Id,
                Amount = t.Amount,
                Reference = t.Reference,
                Type = t.Type,
                TraceNumber = t.TraceNumber,
                EffectiveEntryDate = t.EffectiveEntryDate,
                CreatedAt = t.CreatedAt,
                SourceAccountNumber = t.SourceAccountNumber,
                DestinationAccountNumber = t.DestinationAccountNumber,
                SourceInstitutionName = t.SourceInstitution.Name,
                DestinationInstitutionName = t.DestinationInstitution.Name,
                AchBatchId = t.AchBatchId,
                BatchSequenceNumber = t.AchBatch.BatchSequenceNumber,
                BatchCompanyName = t.AchBatch.CompanyName,
                BatchEffectiveEntryDate = t.AchBatch.EffectiveEntryDate,
                AchCycleId = t.AchCycleId,
                AchCycleName = t.AchCycle.CycleName,
                ClearingHouseName = t.AchCycle.ClearingHouse!.Name
            })
            .ToListAsync(ct);
    }

    private async Task UpdateBatchTotalsAsync(AchBatch batch, CancellationToken ct)
    {
        var totals = await _context.AchTransactions
            .Where(t => t.AchBatchId == batch.Id)
            .GroupBy(t => t.Type)
            .Select(g => new
            {
                Type = g.Key,
                Sum = g.Sum(t => t.Amount)
            })
            .ToListAsync(ct);

        decimal debit = totals
            .Where(t => t.Type == TransactionTypeEnum.Debit)
            .Select(t => t.Sum)
            .FirstOrDefault();

        decimal credit = totals
            .Where(t => t.Type == TransactionTypeEnum.Credit)
            .Select(t => t.Sum)
            .FirstOrDefault();

        if (batch.TotalDebitAmount != debit || batch.TotalCreditAmount != credit)
        {
            batch.TotalDebitAmount = debit;
            batch.TotalCreditAmount = credit;
            _context.AchBatches.Update(batch);
            await _context.SaveChangesAsync(ct);
        }
    }

    private async Task UpdateBatchServiceClassCodeAsync(AchBatch batch, CancellationToken ct)
    {
        var transactions = await _context.AchTransactions
            .Where(t => t.AchBatchId == batch.Id)
            .Select(t => t.Type)
            .ToListAsync(ct);

        if (!transactions.Any())
            return;

        bool allCredits = transactions.All(t => t == TransactionTypeEnum.Credit);
        bool allDebits = transactions.All(t => t == TransactionTypeEnum.Debit);

        string newCode = allCredits ? "220" : allDebits ? "225" : "200";

        if (batch.ServiceClassCode != newCode)
        {
            batch.ServiceClassCode = newCode;
            _context.AchBatches.Update(batch);
            await _context.SaveChangesAsync(ct);
        }
    }

    public async Task<AchTransaction?> GetTransactionByIdAsync(int transactionId, CancellationToken ct = default)
    {
        return await _context.AchTransactions
            .Include(t => t.SourceInstitution)
            .Include(t => t.DestinationInstitution)
            .Include(t => t.Addendas)
            .Include(t => t.AchBatch)
            .Include(t => t.AchCycle)
            .FirstOrDefaultAsync(t => t.Id == transactionId, ct);
    }

}
