using System.Security.Cryptography;
using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>One immutable external instruction per originating institution, independent of cycle.</summary>
internal static class TransactionIdempotency
{
    internal const string UniqueIndexName = "UX_AchTransactions_SourceInstitution_ExternalId";

    internal static string OperationalId(AchTransactionRequestData request) =>
        !string.IsNullOrWhiteSpace(request.TransactionExternalId)
            ? request.TransactionExternalId.Trim()
            : (request.Reference ?? string.Empty).Trim();

    // Snapshot all request inputs that may affect a monetary instruction (including ordered addendas).
    // Do not hash routing/cycle decisions: retries can cross cycles without creating another debit.
    internal static string Fingerprint(AchTransactionRequestData request)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new
        {
            version = 1,
            operationalId = OperationalId(request),
            reference = (request.Reference ?? string.Empty).Trim(),
            amount = request.Amount.ToString("0.############################", System.Globalization.CultureInfo.InvariantCulture),
            effectiveType = request.IsPrenotification || request.Type == TransactionTypeEnum.Prenotification
                ? TransactionTypeEnum.Prenotification.ToString() : request.Type.ToString(),
            accountType = request.AccountType.ToString(),
            request.IsPrenotification,
            request.DestinationInstitutionId,
            sourceAccount = request.SourceAccountNumber.Trim(),
            destinationAccount = request.DestinationAccountNumber.Trim(),
            company = request.CompanyName.Trim(),
            companyId = request.CompanyIdentification.Trim(),
            request.CompanyEntryDescriptionId,
            sourcePersonType = request.SourcePersonType?.Trim() ?? "",
            recipientPersonType = request.RecipientPersonType?.Trim() ?? "",
            recipientId = request.RecipientIdNumber?.Trim() ?? "",
            recipientName = request.RecipientName?.Trim() ?? "",
            request.RequiresIdentityValidation,
            addendas = request.Addendas?.ToArray() ?? []
        });
        return Convert.ToHexString(SHA256.HashData(bytes));
    }

    internal static async Task<int> ResolveOriginatingInstitutionAsync(AchDbContext db, CancellationToken ct)
    {
        var origins = await db.FinancialInstitutions.AsNoTracking()
            .Where(x => x.IsDefaultSource && x.Status == FinancialInstitutionStatus.Active)
            .Select(x => x.Id).Take(2).ToListAsync(ct);
        return origins.Count == 1 ? origins[0] : throw new InvalidOperationException(
            "E2E01_ORIGIN_SCOPE_UNAVAILABLE: debe existir una única entidad originadora activa predeterminada.");
    }

    internal static async Task<AchTransaction?> FindAsync(
        AchDbContext db, int originId, AchTransactionRequestData request, CancellationToken ct)
    {
        var id = OperationalId(request);
        if (string.IsNullOrWhiteSpace(id) || id.Length > 64)
            throw new ArgumentException("E2E01_EXTERNAL_ID_INVALID: el identificador externo es obligatorio y tiene máximo 64 caracteres.");
        var existing = await db.AchTransactions.AsNoTracking()
            .Where(t => t.SourceInstitutionId == originId &&
                (t.TransactionExternalId == id
                 || ((t.TransactionExternalId == null || t.TransactionExternalId == "") && t.Reference == id)))
            .Take(2).ToListAsync(ct);
        if (existing.Count > 1)
            throw new TransactionIdempotencyConflictException("E2E01_HISTORICAL_DUPLICATES: hay registros históricos duplicados; conciliar antes de continuar.");
        if (existing.Count == 0) return null;
        if (string.IsNullOrEmpty(existing[0].RequestFingerprint))
            throw new TransactionIdempotencyConflictException("E2E01_LEGACY_RECONCILIATION: una instrucción histórica carece de huella completa; verificar manualmente, no redespachar.");
        if (!string.Equals(existing[0].RequestFingerprint, Fingerprint(request), StringComparison.Ordinal))
            throw new TransactionIdempotencyConflictException("E2E01_IDEMPOTENCY_CONFLICT: el identificador externo ya pertenece a una instrucción diferente.");
        return existing[0];
    }
}

public sealed class TransactionIdempotencyConflictException : InvalidOperationException
{
    public TransactionIdempotencyConflictException(string message) : base(message) { }
}
