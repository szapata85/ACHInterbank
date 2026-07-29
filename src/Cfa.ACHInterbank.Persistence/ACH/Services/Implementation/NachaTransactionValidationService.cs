using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class NachaTransactionValidationService : INachaTransactionValidationService
{
    private readonly AchDbContext _context;
    private readonly ITransactionPrerequisitePolicyService _prerequisitePolicyService;

    private sealed record PrenoteLookupKey(int DestinationInstitutionId, string DestinationAccountNumber, string TransactionCode);

    public NachaTransactionValidationService(
        AchDbContext context,
        IBankHoliday holidayService,
        ITransactionPrerequisitePolicyService prerequisitePolicyService)
    {
        _context = context;
        _ = holidayService;
        _prerequisitePolicyService = prerequisitePolicyService
            ?? throw new ArgumentNullException(nameof(prerequisitePolicyService));
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
                var validation = await _prerequisitePolicyService.ValidateForNachaExportAsync(tx, prenoteDate, ct);
                if (!validation.IsValid)
                {
                    throw new NachaGenerationException(validation.Code, validation.Message);
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

}
