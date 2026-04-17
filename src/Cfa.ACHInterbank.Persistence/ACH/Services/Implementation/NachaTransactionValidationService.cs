using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class NachaTransactionValidationService : INachaTransactionValidationService
{
    private readonly AchDbContext _context;
    private readonly IBankHoliday _holidayService;

    private sealed record PrenoteLookupKey(int DestinationInstitutionId, string DestinationAccountNumber, string TransactionCode);

    public NachaTransactionValidationService(AchDbContext context, IBankHoliday holidayService)
    {
        _context = context;
        _holidayService = holidayService;
    }

    public async Task ValidateTransactionsForSendAsync(IReadOnlyList<AchTransaction> transactions, CancellationToken ct = default)
    {
        var prenoteLookup = await BuildPrenoteLookupAsync(transactions, ct);

        foreach (var tx in transactions)
        {
            if (tx.IsPrenotification && tx.Amount != 0)
            {
                throw new InvalidOperationException($"La prenotificación {tx.Id} debe tener valor 0.");
            }

            if (!tx.IsPrenotification)
            {
                var prenoteDate = GetPrenoteDate(tx, prenoteLookup);
                if (prenoteDate is null)
                {
                    throw new InvalidOperationException($"La transacción {tx.Id} no tiene prenotificación previa.");
                }

                var minDate = AddBusinessDays(prenoteDate.Value.Date, 3);
                if (tx.EffectiveEntryDate.Date < minDate)
                {
                    throw new InvalidOperationException($"La transacción {tx.Id} no cumple los 3 días hábiles desde la prenotificación.");
                }
            }
        }
    }

    private static DateTime? GetPrenoteDate(AchTransaction tx, IReadOnlyDictionary<PrenoteLookupKey, DateTime> prenoteLookup)
    {
        var prenoteCode = ResolvePrenoteCode(tx.TransactionCode);
        if (string.IsNullOrWhiteSpace(prenoteCode))
        {
            return null;
        }

        var key = new PrenoteLookupKey(
            tx.DestinationInstitutionId,
            (tx.DestinationAccountNumber ?? string.Empty).Trim(),
            prenoteCode);

        return prenoteLookup.TryGetValue(key, out var date) ? date : null;
    }

    private async Task<IReadOnlyDictionary<PrenoteLookupKey, DateTime>> BuildPrenoteLookupAsync(
        IReadOnlyList<AchTransaction> transactions,
        CancellationToken ct)
    {
        var keys = transactions
            .Where(tx => !tx.IsPrenotification)
            .Select(tx => new
            {
                Tx = tx,
                PrenoteCode = ResolvePrenoteCode(tx.TransactionCode)
            })
            .Where(item => !string.IsNullOrWhiteSpace(item.PrenoteCode))
            .Select(item => new PrenoteLookupKey(
                item.Tx.DestinationInstitutionId,
                (item.Tx.DestinationAccountNumber ?? string.Empty).Trim(),
                item.PrenoteCode!))
            .Distinct()
            .ToArray();

        if (keys.Length == 0)
        {
            return new Dictionary<PrenoteLookupKey, DateTime>();
        }

        var institutionIds = keys.Select(k => k.DestinationInstitutionId).Distinct().ToArray();
        var accounts = keys.Select(k => k.DestinationAccountNumber).Distinct(StringComparer.Ordinal).ToArray();
        var codes = keys.Select(k => k.TransactionCode).Distinct(StringComparer.Ordinal).ToArray();
        var keySet = keys.ToHashSet();

        var prenotes = await _context.AchTransactions
            .AsNoTracking()
            .Where(t =>
                t.IsPrenotification &&
                institutionIds.Contains(t.DestinationInstitutionId) &&
                accounts.Contains(t.DestinationAccountNumber) &&
                codes.Contains(t.TransactionCode))
            .Select(t => new
            {
                t.DestinationInstitutionId,
                t.DestinationAccountNumber,
                t.TransactionCode,
                Date = t.EffectiveEntryDate.Date
            })
            .ToListAsync(ct);

        return prenotes
            .Select(item => new
            {
                Key = new PrenoteLookupKey(
                    item.DestinationInstitutionId,
                    (item.DestinationAccountNumber ?? string.Empty).Trim(),
                    item.TransactionCode),
                item.Date
            })
            .Where(item => keySet.Contains(item.Key))
            .GroupBy(item => item.Key)
            .ToDictionary(group => group.Key, group => group.Max(x => x.Date));
    }

    private static string? ResolvePrenoteCode(string transactionCode)
    {
        return transactionCode switch
        {
            "22" => "23",
            "27" => "28",
            "32" => "33",
            "37" => "38",
            "52" => "53",
            "55" => "57",
            _ => null
        };
    }

    private DateTime AddBusinessDays(DateTime start, int days)
    {
        var date = start;
        var remaining = days;
        var currentYear = date.Year;
        var holidays = _holidayService.GetHolidays(currentYear)
            .Select(h => h.Date)
            .ToHashSet();

        while (remaining > 0)
        {
            date = date.AddDays(1);

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
            {
                remaining--;
            }
        }

        return date;
    }
}
