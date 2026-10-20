using System.Text;
using Cfa.ACHInterbank.Application.ACH.Interfaces.PaymentRails;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Models.PaymentRails;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.Extensions.Logging;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

internal sealed class AchReturnAuditMonitoringComponent(
    IPaymentRailContextService? paymentRailContextService,
    IPaymentRailOperationalStrategyResolver? strategyResolver,
    IPaymentRailShadowCompareService? shadowCompareService,
    ILogger<AchReturnsService> logger)
{
    public string BuildReturnFileGeneratedPayload(
        AchTransaction originalTx,
        AchReturnGenerated generatedRow,
        AchCycle cycle,
        string fileName,
        int recordCount,
        int returnCount,
        DateTime createdAtUtc,
        string fileContent,
        AchTransferStateEnum newState,
        AchColombiaR10ReturnBasis? achColombiaR10Basis)
    {
        var payload = new
        {
            schemaVersion = 1,
            eventType = "ReturnFileGenerated",
            source = $"{nameof(AchReturnsService)}.{nameof(AchReturnsService.GenerateReturnsFileAsync)}",
            generationMode = "outbound-return",
            stateChanged = true,
            originalTransactionId = generatedRow.OriginalTransactionId,
            transactionExternalId = originalTx.TransactionExternalId,
            reference = originalTx.Reference,
            transactionType = originalTx.Type.ToString(),
            previousState = originalTx.State.ToString(),
            newState = newState.ToString(),
            returnReasonCode = generatedRow.ReturnReasonCode,
            achColombiaR10Basis = achColombiaR10Basis?.ToString(),
            returnCycleId = generatedRow.ReturnCycleId,
            clearingHouseId = cycle.ClearingHouseId,
            clearingHouseCode = cycle.ClearingHouse?.Code,
            clearingHouseName = cycle.ClearingHouse?.Name,
            fileName,
            externalFileName = fileName,
            contentSha256 = ComputeSha256Hex(fileContent),
            recordCount,
            returnCount,
            originalTraceNumber = generatedRow.OriginalSequenceNumber,
            newTraceNumber = generatedRow.NewSequenceNumber,
            originalSequenceNumber = generatedRow.OriginalSequenceNumber,
            newSequenceNumber = generatedRow.NewSequenceNumber,
            amount = generatedRow.Amount,
            currency = "COP",
            receiverEntityCode = generatedRow.ReceiverEntityCode,
            originatorEntityCode = generatedRow.OriginatorEntityCode,
            generatedAtUtc = generatedRow.GeneratedAtUtc,
            createdAtUtc,
            warnings = Array.Empty<string>(),
            transmissionStatus = "GeneratedNotTransmitted",
            productiveStatus = "TechnicalGeneratedOnly"
        };

        return System.Text.Json.JsonSerializer.Serialize(payload);
    }

    public void CompareReturnShadow(
        int? clearingHouseId,
        string? clearingHouseCode,
        string? paymentRailCode,
        string? achCycleId,
        DateTime? operationalDate,
        string legacyDecisionCode,
        bool legacyOperationSucceeded)
    {
        if (paymentRailContextService is null || strategyResolver is null || shadowCompareService is null)
        {
            return;
        }

        try
        {
            var context = paymentRailContextService.ResolveContext(
                clearingHouseId,
                clearingHouseCode,
                achCycleId,
                operationalDate,
                paymentRailCode);
            var strategy = strategyResolver.ResolveStrategy(new PaymentRailResolveRequest(
                clearingHouseId,
                clearingHouseCode,
                paymentRailCode));
            var wrapperResult = strategy.EvaluateCapabilityWrapper(new PaymentRailWrapperCallRequest(
                context.OperationalContext,
                PaymentRailCapabilityKind.Return,
                legacyDecisionCode));
            var shadowResult = shadowCompareService.CompareReturnOperation(
                context,
                wrapperResult,
                legacyDecisionCode,
                legacyOperationSucceeded);

            logger.LogInformation(
                "PAYMENT_RAIL_SHADOW_COMPARE_RETURN|RailCode={RailCode}|LegacyDecision={LegacyDecision}|WrapperDecision={WrapperDecision}|Equivalent={Equivalent}|Code={Code}",
                shadowResult.RailCode,
                shadowResult.LegacyDecisionCode,
                shadowResult.WrapperDecisionCode,
                shadowResult.IsEquivalent,
                shadowResult.ComparisonCode);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "PAYMENT_RAIL_SHADOW_COMPARE_RETURN_FAILED");
        }
    }

    private static string ComputeSha256Hex(string content)
    {
        var bytes = Encoding.UTF8.GetBytes(content);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
