using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Services;

public sealed class AchTransactionClassificationPolicy(TimeProvider? timeProvider = null)
    : IAchTransactionClassificationPolicy
{
    public const int CurrentVersion = 1;
    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;

    public AchTransactionClassificationResult Classify(AchTransactionClassificationRequest request)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;

        if (request.SourceInstitutionIsDefault == request.DestinationInstitutionIsDefault)
        {
            return new(
                AchTransactionDirection.Unknown,
                AchTransactionOrigin.Unknown,
                AchMonetaryIntegrationRoute.ManualReview,
                AchTransactionClassificationStatus.Ambiguous,
                request.SourceInstitutionIsDefault,
                now,
                CurrentVersion,
                "No fue posible determinar de forma inequívoca el origen y destino de la transacción. Verifica las entidades seleccionadas.");
        }

        var outgoing = request.SourceInstitutionIsDefault;
        var direction = outgoing ? AchTransactionDirection.Outgoing : AchTransactionDirection.Incoming;
        var origin = outgoing ? AchTransactionOrigin.Cfa : AchTransactionOrigin.ExternalInstitution;

        if (request.IsPrenotification || request.TransactionType == TransactionTypeEnum.Prenotification)
        {
            return new(
                direction,
                origin,
                AchMonetaryIntegrationRoute.None,
                AchTransactionClassificationStatus.Determined,
                request.SourceInstitutionIsDefault,
                now,
                CurrentVersion,
                null);
        }

        if (outgoing && request.TransactionType == TransactionTypeEnum.Debit)
        {
            return new(
                direction,
                origin,
                AchMonetaryIntegrationRoute.ProcContrapartidas,
                AchTransactionClassificationStatus.Determined,
                true,
                now,
                CurrentVersion,
                null);
        }

        if (!outgoing && request.TransactionType == TransactionTypeEnum.Credit)
        {
            return new(
                direction,
                origin,
                AchMonetaryIntegrationRoute.ProcTransacciones,
                AchTransactionClassificationStatus.Determined,
                false,
                now,
                CurrentVersion,
                null);
        }

        return new(
            direction,
            origin,
            AchMonetaryIntegrationRoute.ManualReview,
            AchTransactionClassificationStatus.Invalid,
            request.SourceInstitutionIsDefault,
            now,
            CurrentVersion,
            outgoing
                ? "Las transacciones monetarias originadas por CFA deben registrarse como débito. La operación no fue encolada."
                : "Las transacciones monetarias originadas por otra entidad deben registrarse como crédito. La operación no fue encolada.");
    }
}
