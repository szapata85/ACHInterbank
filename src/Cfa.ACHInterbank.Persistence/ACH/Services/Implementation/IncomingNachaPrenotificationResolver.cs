using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class IncomingNachaPrenotificationResolver : IIncomingNachaPrenotificationResolver
{
    private readonly AchDbContext _context;

    public IncomingNachaPrenotificationResolver(AchDbContext context)
    {
        _context = context;
    }

    public async Task<IncomingNachaPrenoteResolutionResult> ResolveAsync(
        Guid ingestionId,
        EntryDetail entry,
        int? linkedTransactionId,
        int? resolvedClearingHouseId,
        DateTime? operationalDate,
        string executedBy,
        CancellationToken ct = default)
    {
        var destinationAccount = (entry.AccountNumber ?? string.Empty).Trim();
        var recipientId = (entry.RecipIdNumber ?? string.Empty).Trim();
        var receivingDfi = $"{(entry.ReceivingParticipantEntityCode ?? string.Empty).Trim()}{(entry.CheckDigit ?? string.Empty).Trim()}".Trim();

        AchTransaction? linkedTransaction = null;
        if (linkedTransactionId.HasValue)
        {
            linkedTransaction = await _context.AchTransactions.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == linkedTransactionId.Value, ct);
        }

        var thirdPartyQuery = _context.CustomerThirdParties
            .Where(x => x.DestinationAccountNumber == destinationAccount);

        if (!string.IsNullOrWhiteSpace(recipientId))
        {
            thirdPartyQuery = thirdPartyQuery.Where(x => x.RecipientIdNumber == recipientId);
        }

        if (linkedTransaction is not null)
        {
            thirdPartyQuery = thirdPartyQuery.Where(x => x.DestinationInstitutionId == linkedTransaction.DestinationInstitutionId);
        }
        else if (!string.IsNullOrWhiteSpace(receivingDfi))
        {
            var receivingDfiPrefix = (entry.ReceivingParticipantEntityCode ?? string.Empty).Trim();
            var matchingInstitutionIds = await _context.FinancialInstitutions.AsNoTracking()
                .Where(x => (x.RoutingNumber + x.TransitCode + x.CheckDigit) == receivingDfi
                            || (x.RoutingNumber + x.TransitCode) == receivingDfiPrefix)
                .Select(x => x.Id)
                .ToListAsync(ct);

            if (matchingInstitutionIds.Count == 1)
            {
                var institutionId = matchingInstitutionIds[0];
                thirdPartyQuery = thirdPartyQuery.Where(x => x.DestinationInstitutionId == institutionId);
            }
        }

        if (linkedTransactionId.HasValue)
        {
            thirdPartyQuery = thirdPartyQuery.Where(x => x.PrenotificationTransactionId == linkedTransactionId.Value || x.PrenotificationTransactionId == null);
        }

        var candidates = await thirdPartyQuery
            .OrderBy(x => x.Id)
            .ToListAsync(ct);

        if (candidates.Count == 0)
        {
            return new IncomingNachaPrenoteResolutionResult
            {
                PrenoteStatus = IncomingNachaPrenoteStatus.RequiereRevision,
                Applied = false,
                RequiresManualReview = true,
                Message = "No se encontró tercero candidato para prenotificación entrante.",
                EvidenceJson = JsonSerializer.Serialize(new
                {
                    ingestionId,
                    destinationAccount,
                    recipientId,
                    receivingDfi,
                    linkedTransactionId,
                    resolvedClearingHouseId,
                    operationalDate
                })
            };
        }

        if (candidates.Count > 1)
        {
            return new IncomingNachaPrenoteResolutionResult
            {
                PrenoteStatus = IncomingNachaPrenoteStatus.RequiereRevision,
                Applied = false,
                RequiresManualReview = true,
                Message = "Se encontraron múltiples terceros candidatos para prenotificación entrante.",
                EvidenceJson = JsonSerializer.Serialize(new
                {
                    ingestionId,
                    candidateIds = candidates.Select(x => x.Id).ToList(),
                    destinationAccount,
                    recipientId,
                    receivingDfi,
                    linkedTransactionId,
                    resolvedClearingHouseId,
                    operationalDate
                })
            };
        }

        if (string.IsNullOrWhiteSpace(recipientId) && !linkedTransactionId.HasValue)
        {
            return new IncomingNachaPrenoteResolutionResult
            {
                PrenoteStatus = IncomingNachaPrenoteStatus.RequiereRevision,
                Applied = false,
                RequiresManualReview = true,
                Message = "Prenotificación sin identificación de receptor y sin vínculo previo: requiere revisión manual.",
                EvidenceJson = JsonSerializer.Serialize(new
                {
                    ingestionId,
                    destinationAccount,
                    recipientId,
                    receivingDfi,
                    linkedTransactionId,
                    resolvedClearingHouseId,
                    operationalDate
                })
            };
        }

        var selected = candidates[0];
        selected.Status = CustomerThirdPartyStatusEnum.Active;
        selected.ValidationReceivedAt = DateTime.UtcNow;
        selected.ValidationMessage = "Prenotificación entrante aplicada por resolvedor NACHA-M.";
        if (linkedTransactionId.HasValue)
        {
            selected.PrenotificationTransactionId = linkedTransactionId.Value;
        }

        return new IncomingNachaPrenoteResolutionResult
        {
            PrenoteStatus = IncomingNachaPrenoteStatus.ActivaTercero,
            Applied = true,
            RequiresManualReview = false,
            Message = "Prenotificación aplicada con resolución determinística de tercero.",
            EvidenceJson = JsonSerializer.Serialize(new
            {
                ingestionId,
                selectedThirdPartyId = selected.Id,
                destinationAccount,
                recipientId,
                receivingDfi,
                linkedTransactionId,
                resolvedClearingHouseId,
                operationalDate,
                executedBy
            })
        };
    }
}
