using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class AchRegulatoryCatalogService : IAchRegulatoryCatalogService
{
    private readonly AchDbContext _context;

    public AchRegulatoryCatalogService(AchDbContext context)
    {
        _context = context;
    }

    public async Task<int> GetPriorityAsync(TransactionTypeEnum transactionType, CancellationToken ct)
    {
        var type = MapType(transactionType);
        return await _context.AchTransactionTypePolicies
            .AsNoTracking()
            .Where(x => x.IsActive && x.TransactionType == type)
            .Select(x => x.PriorityOrder)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<bool> IsPrenotificationRequiredAsync(TransactionTypeEnum transactionType, CancellationToken ct)
    {
        var type = MapType(transactionType);
        return await _context.AchPrenotificationPolicies
            .AsNoTracking()
            .Where(x => x.IsActive && x.TransactionType == type)
            .Select(x => x.IsRequired)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<(bool IsAllowed, string? Reason)> ValidateReturnCodeAsync(string returnCode, TransactionTypeEnum transactionType, DateTime originalDate, DateTime currentDate, CancellationToken ct)
    {
        var code = returnCode.Trim().ToUpperInvariant();
        var model = await _context.AchReturnCodes.AsNoTracking().FirstOrDefaultAsync(x => x.Code == code && x.IsActive, ct);
        if (model is null)
        {
            return (false, $"Código de devolución {code} no existe en catálogo regulatorio.");
        }

        var days = (currentDate.Date - originalDate.Date).Days;
        if (model.MaxDaysAllowed.HasValue && days > model.MaxDaysAllowed.Value)
        {
            return (false, $"Código {code} excede ventana regulatoria de {model.MaxDaysAllowed} días.");
        }

        var isAllowedByType = transactionType switch
        {
            TransactionTypeEnum.Debit => model.AppliesToDebit,
            TransactionTypeEnum.Credit => model.AppliesToCredit,
            TransactionTypeEnum.Prenotification => model.AppliesToPrenotification,
            TransactionTypeEnum.Return => model.AppliesToReturn,
            _ => false
        };

        return isAllowedByType
            ? (true, null)
            : (false, $"Código {code} no aplica al tipo {transactionType}.");
    }

    public async Task<(bool IsAllowed, string? Reason, bool IsUniquePerTransaction)> ValidateReturnOfReturnAsync(string originalReturnCode, string newReturnCode, string originalState, DateTime originalDate, DateTime currentDate, CancellationToken ct)
    {
        var policy = await _context.AchReturnOfReturnPolicies
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IsActive && x.OriginalReturnCode == originalReturnCode.Trim().ToUpperInvariant(), ct);

        if (policy is null)
        {
            return (false, $"No existe política activa de devolución de devolución para causal {originalReturnCode}.", true);
        }

        var allowedCodes = policy.AllowedNewReturnCodesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (!allowedCodes.Contains(newReturnCode.Trim().ToUpperInvariant(), StringComparer.OrdinalIgnoreCase))
        {
            return (false, $"La causal {newReturnCode} no está permitida para causal origen {originalReturnCode}.", policy.IsUniquePerTransaction);
        }

        if (!string.Equals(policy.RequiredOriginalState, originalState, StringComparison.OrdinalIgnoreCase))
        {
            return (false, $"Estado origen {originalState} no cumple política (requerido: {policy.RequiredOriginalState}).", policy.IsUniquePerTransaction);
        }

        var days = (currentDate.Date - originalDate.Date).Days;
        if (days > policy.MaxDays)
        {
            return (false, $"La devolución de devolución excede ventana de {policy.MaxDays} días.", policy.IsUniquePerTransaction);
        }

        return (true, null, policy.IsUniquePerTransaction);
    }

    public async Task<AchFileRejectionCode?> ResolveFileRejectionCodeAsync(string stage, string code, CancellationToken ct)
    {
        var normalizedCode = code.Trim().ToUpperInvariant();
        var normalizedStage = stage.Trim();

        return await _context.AchFileRejectionCodes
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.IsActive
                                      && x.Code == normalizedCode
                                      && x.AppliesToStage == normalizedStage, ct);
    }

    public async Task<IReadOnlyList<AchReturnCode>> GetReturnCodesAsync(CancellationToken ct)
        => await _context.AchReturnCodes.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Code).ToListAsync(ct);

    public async Task<IReadOnlyList<AchFileRejectionCode>> GetFileRejectionCodesAsync(CancellationToken ct)
        => await _context.AchFileRejectionCodes.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.Code).ToListAsync(ct);

    public async Task<IReadOnlyList<AchTransactionTypePolicy>> GetTransactionTypePoliciesAsync(CancellationToken ct)
        => await _context.AchTransactionTypePolicies.AsNoTracking().Where(x => x.IsActive).OrderBy(x => x.PriorityOrder).ToListAsync(ct);

    private static string MapType(TransactionTypeEnum type) => type switch
    {
        TransactionTypeEnum.Credit => "Credit",
        TransactionTypeEnum.Debit => "Debit",
        TransactionTypeEnum.Prenotification => "Prenotification",
        TransactionTypeEnum.Return => "Return",
        TransactionTypeEnum.Reversal => "ReturnOfReturn",
        _ => type.ToString()
    };
}
