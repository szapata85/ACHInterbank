using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.Integrations.Services;

[Scoped]
public sealed class TransactionIntegrationOperationResolver : ITransactionIntegrationOperationResolver
{
    private readonly AchDbContext _context;

    public TransactionIntegrationOperationResolver(AchDbContext context)
    {
        _context = context;
    }

    public async Task<TransactionIntegrationOperationResult> ResolveAsync(AchTransaction transaction, CancellationToken ct = default)
    {
        await Task.CompletedTask;
        if (transaction is null)
        {
            return Unsupported(null, string.Empty, "TRANSACTION_REQUIRED", "La transaccion es requerida.");
        }

        var reference = ResolveReference(transaction);

        if (transaction.ClassificationStatus == AchTransactionClassificationStatus.Determined
            && transaction.MonetaryIntegrationRoute == AchMonetaryIntegrationRoute.ProcContrapartidas
            && transaction.Direction == AchTransactionDirection.Outgoing
            && transaction.Origin == AchTransactionOrigin.Cfa
            && transaction.Type == TransactionTypeEnum.Debit)
        {
            return new TransactionIntegrationOperationResult(
                transaction.Id,
                reference,
                IntegrationGuaranteeConstants.Wscfaach,
                IntegrationGuaranteeConstants.ProcContrapartidas,
                IntegrationGuaranteeConstants.MonetaryDebitRequest,
                IntegrationGuaranteeConstants.OutboundRequest,
                "Debito monetario",
                "CFA originadora",
                true,
                "Debito monetario originado por CFA.",
                true,
                []);
        }

        if (transaction.ClassificationStatus == AchTransactionClassificationStatus.Determined
            && transaction.MonetaryIntegrationRoute == AchMonetaryIntegrationRoute.ProcTransacciones
            && transaction.Direction == AchTransactionDirection.Incoming
            && transaction.Origin == AchTransactionOrigin.ExternalInstitution
            && transaction.Type == TransactionTypeEnum.Credit)
        {
            return new TransactionIntegrationOperationResult(
                transaction.Id,
                reference,
                IntegrationGuaranteeConstants.Wscfaach,
                IntegrationGuaranteeConstants.ProcTransacciones,
                IntegrationGuaranteeConstants.MonetaryCreditRequest,
                IntegrationGuaranteeConstants.OutboundRequest,
                "Credito monetario",
                "Entidad financiera externa; CFA receptora",
                true,
                "Credito monetario originado por otra entidad financiera.",
                true,
                []);
        }

        var errors = new List<string>
        {
            transaction.Type switch
            {
                TransactionTypeEnum.Debit => "DEBIT_NOT_CLASSIFIED_FOR_PROC_CONTRAPARTIDAS",
                TransactionTypeEnum.Credit => "CREDIT_NOT_CLASSIFIED_FOR_PROC_TRANSACCIONES",
                _ => "TRANSACTION_OPERATION_NOT_SUPPORTED"
            }
        };

        return new TransactionIntegrationOperationResult(
            transaction.Id,
            reference,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            "No soportada",
            "No resuelto",
            false,
            "La clasificación histórica de la transacción no autoriza una integración monetaria automática.",
            false,
            errors);
    }

    public TransactionIntegrationOperationResult ResolveDifferentialResponse(string? reference = null, int? transactionId = null)
    {
        return new TransactionIntegrationOperationResult(
            transactionId,
            reference?.Trim() ?? string.Empty,
            IntegrationGuaranteeConstants.WsAxon,
            IntegrationGuaranteeConstants.RegistrarRespuestaTransaccion,
            IntegrationGuaranteeConstants.DifferentialResponseNotification,
            IntegrationGuaranteeConstants.InboundResponse,
            "Respuesta diferencial / notificacion",
            "Entidad/camara/proveedor externo",
            false,
            "Notificacion/respuesta diferencial no monetaria.",
            true,
            []);
    }

    private static TransactionIntegrationOperationResult Unsupported(int? transactionId, string reference, string code, string reason)
        => new(
            transactionId,
            reference,
            string.Empty,
            string.Empty,
            string.Empty,
            string.Empty,
            "No soportada",
            "No resuelto",
            false,
            reason,
            false,
            [code]);

    private static string ResolveReference(AchTransaction transaction)
        => !string.IsNullOrWhiteSpace(transaction.TransactionExternalId)
            ? transaction.TransactionExternalId
            : transaction.Reference ?? string.Empty;
}
