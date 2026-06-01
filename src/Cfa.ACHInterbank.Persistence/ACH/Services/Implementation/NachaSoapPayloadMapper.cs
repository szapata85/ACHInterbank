using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class NachaSoapPayloadMapper : INachaSoapPayloadMapper
{
    public NachaSoapPayloadMappingResult Map(NachaIncomingDecision decision, NachaSoapExecutionContext context)
    {
        ArgumentNullException.ThrowIfNull(decision);
        ArgumentNullException.ThrowIfNull(context);

        var errors = new List<string>();
        var warnings = new List<string>();
        ValidateCommonContext(context, errors);

        return decision.SoapOperation switch
        {
            NachaSoapOperationCandidate.ProcContrapartidas => MapProcContrapartidas(decision, context, errors, warnings),
            NachaSoapOperationCandidate.ProcTransacciones => MapProcTransacciones(decision, context, errors, warnings),
            NachaSoapOperationCandidate.RegistrarRespuestaTransaccion => MapRegistrarRespuesta(decision, context, errors, warnings),
            _ => NotMapped(decision, context, "Operacion None no produce payload SOAP ejecutable.", warnings)
        };
    }

    private static NachaSoapPayloadMappingResult MapProcContrapartidas(
        NachaIncomingDecision decision,
        NachaSoapExecutionContext context,
        List<string> errors,
        List<string> warnings)
    {
        ValidateMonetaryDecision(decision, context, NachaIncomingDecisionType.ApplyDebitMovement, "ProcContrapartidas", errors);
        if (errors.Count > 0)
        {
            return Failed(decision, context, errors, warnings);
        }

        var payload = new NachaSoapProcContrapartidasPayload
        {
            CorrelationId = context.CorrelationId,
            SourceFileName = context.SourceFileName,
            ClearingHouseCode = context.ClearingHouseCode,
            ProfileCode = context.ProfileCode,
            OperationCandidate = decision.SoapOperation,
            TransactionId = decision.TransactionId,
            PrenotificationId = decision.PrenotificationId,
            EntryTraceNumber = decision.EntryTraceNumber,
            OriginalTraceNumber = decision.OriginalTraceNumber ?? string.Empty,
            AmountInCents = context.AmountInCents,
            Currency = NormalizeCurrency(context.Currency),
            ReasonCode = decision.ReasonCode ?? string.Empty,
            ReasonDescription = decision.ReasonDescription,
            SourceFinancialInstitutionCode = context.SourceFinancialInstitutionCode,
            DestinationFinancialInstitutionCode = context.DestinationFinancialInstitutionCode,
            CreatedAt = ResolveCreatedAt(context.CreatedAt),
            Metadata = SanitizeMetadata(context.Metadata),
            SourceAccountReference = MaskAccount(context.SourceAccountReference),
            DestinationAccountReference = MaskAccount(context.DestinationAccountReference)
        };

        return Success(payload, decision, context, warnings);
    }

    private static NachaSoapPayloadMappingResult MapProcTransacciones(
        NachaIncomingDecision decision,
        NachaSoapExecutionContext context,
        List<string> errors,
        List<string> warnings)
    {
        ValidateMonetaryDecision(decision, context, NachaIncomingDecisionType.ApplyCreditMovement, "ProcTransacciones", errors);
        if (errors.Count > 0)
        {
            return Failed(decision, context, errors, warnings);
        }

        var payload = new NachaSoapProcTransaccionesPayload
        {
            CorrelationId = context.CorrelationId,
            SourceFileName = context.SourceFileName,
            ClearingHouseCode = context.ClearingHouseCode,
            ProfileCode = context.ProfileCode,
            OperationCandidate = decision.SoapOperation,
            TransactionId = decision.TransactionId,
            PrenotificationId = decision.PrenotificationId,
            EntryTraceNumber = decision.EntryTraceNumber,
            OriginalTraceNumber = decision.OriginalTraceNumber ?? string.Empty,
            AmountInCents = context.AmountInCents,
            Currency = NormalizeCurrency(context.Currency),
            ReasonCode = decision.ReasonCode ?? string.Empty,
            ReasonDescription = decision.ReasonDescription,
            SourceFinancialInstitutionCode = context.SourceFinancialInstitutionCode,
            DestinationFinancialInstitutionCode = context.DestinationFinancialInstitutionCode,
            CreatedAt = ResolveCreatedAt(context.CreatedAt),
            Metadata = SanitizeMetadata(context.Metadata),
            ExternalOriginatorInstitutionCode = context.ExternalOriginatorInstitutionCode,
            CfaReceiverInstitutionCode = context.CfaReceiverInstitutionCode,
            SourceAccountReference = MaskAccount(context.SourceAccountReference),
            DestinationAccountReference = MaskAccount(context.DestinationAccountReference)
        };

        return Success(payload, decision, context, warnings);
    }

    private static NachaSoapPayloadMappingResult MapRegistrarRespuesta(
        NachaIncomingDecision decision,
        NachaSoapExecutionContext context,
        List<string> errors,
        List<string> warnings)
    {
        if (decision.RequiresMonetaryMovement)
        {
            errors.Add("RegistrarRespuestaTransaccion no puede mapear decisiones con movimiento monetario.");
        }

        if (decision.DecisionType is NachaIncomingDecisionType.ManualReviewRequired or NachaIncomingDecisionType.IgnoreDuplicate)
        {
            errors.Add($"{decision.DecisionType} no produce payload ejecutable para RegistrarRespuestaTransaccion.");
        }

        if (decision.DecisionType is not (NachaIncomingDecisionType.RegisterDifferentialResponse
            or NachaIncomingDecisionType.ApprovePrenotification
            or NachaIncomingDecisionType.RejectPrenotification
            or NachaIncomingDecisionType.MarkTransactionRejected
            or NachaIncomingDecisionType.MarkTransactionAccepted))
        {
            errors.Add("DecisionType no compatible con RegistrarRespuestaTransaccion.");
        }

        if (errors.Count > 0)
        {
            return Failed(decision, context, errors, warnings);
        }

        var payload = new NachaSoapRegistrarRespuestaTransaccionPayload
        {
            CorrelationId = context.CorrelationId,
            SourceFileName = context.SourceFileName,
            ClearingHouseCode = context.ClearingHouseCode,
            ProfileCode = context.ProfileCode,
            OperationCandidate = decision.SoapOperation,
            TransactionId = decision.TransactionId,
            PrenotificationId = decision.PrenotificationId,
            EntryTraceNumber = decision.EntryTraceNumber,
            OriginalTraceNumber = decision.OriginalTraceNumber ?? string.Empty,
            AmountInCents = 0,
            Currency = NormalizeCurrency(context.Currency),
            ReasonCode = decision.ReasonCode ?? string.Empty,
            ReasonDescription = decision.ReasonDescription,
            SourceFinancialInstitutionCode = context.SourceFinancialInstitutionCode,
            DestinationFinancialInstitutionCode = context.DestinationFinancialInstitutionCode,
            CreatedAt = ResolveCreatedAt(context.CreatedAt),
            Metadata = SanitizeMetadata(context.Metadata),
            NewInternalStatus = decision.NewInternalStatus,
            ResponseType = ResolveResponseType(decision.DecisionType)
        };

        return Success(payload, decision, context, warnings);
    }

    private static void ValidateCommonContext(NachaSoapExecutionContext context, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(context.CorrelationId)) errors.Add("CorrelationId es obligatorio.");
        if (string.IsNullOrWhiteSpace(context.ClearingHouseCode)) errors.Add("ClearingHouseCode es obligatorio.");
        if (string.IsNullOrWhiteSpace(context.ProfileCode)) errors.Add("ProfileCode es obligatorio.");
        if (string.IsNullOrWhiteSpace(context.SourceFileName)) errors.Add("SourceFileName es obligatorio.");
    }

    private static void ValidateMonetaryDecision(
        NachaIncomingDecision decision,
        NachaSoapExecutionContext context,
        NachaIncomingDecisionType expectedDecisionType,
        string operationName,
        List<string> errors)
    {
        if (!decision.RequiresMonetaryMovement)
        {
            errors.Add($"{operationName} requiere decision con movimiento monetario.");
        }

        if (decision.DecisionType == NachaIncomingDecisionType.ManualReviewRequired)
        {
            errors.Add("ManualReviewRequired no produce payload monetario.");
        }

        if (decision.DecisionType != expectedDecisionType)
        {
            errors.Add($"{operationName} requiere {expectedDecisionType}.");
        }

        if (context.AmountInCents <= 0)
        {
            errors.Add($"{operationName} requiere AmountInCents mayor a cero.");
        }

        if (!decision.TransactionId.HasValue)
        {
            errors.Add($"{operationName} requiere TransactionId.");
        }
    }

    private static NachaSoapPayloadMappingResult Success(
        NachaSoapPayloadBase payload,
        NachaIncomingDecision decision,
        NachaSoapExecutionContext context,
        IReadOnlyList<string> warnings)
        => new()
        {
            CorrelationId = context.CorrelationId,
            OperationCandidate = decision.SoapOperation,
            IsMapped = true,
            IsExecutable = true,
            RequiresMonetaryMovement = payload.RequiresMonetaryMovement,
            PayloadType = payload.GetType().Name,
            Payload = payload,
            SanitizedSummary = BuildSummary(payload),
            Warnings = warnings,
            Metadata = SanitizeMetadata(context.Metadata)
        };

    private static NachaSoapPayloadMappingResult Failed(
        NachaIncomingDecision decision,
        NachaSoapExecutionContext context,
        IReadOnlyList<string> errors,
        IReadOnlyList<string> warnings)
        => new()
        {
            CorrelationId = context.CorrelationId,
            OperationCandidate = decision.SoapOperation,
            IsMapped = false,
            IsExecutable = false,
            RequiresMonetaryMovement = false,
            PayloadType = string.Empty,
            Payload = null,
            SanitizedSummary = BuildSummary(decision, context),
            Errors = errors,
            Warnings = warnings,
            Metadata = SanitizeMetadata(context.Metadata)
        };

    private static NachaSoapPayloadMappingResult NotMapped(
        NachaIncomingDecision decision,
        NachaSoapExecutionContext context,
        string warning,
        List<string> warnings)
    {
        warnings.Add(warning);
        return Failed(decision, context, [], warnings);
    }

    private static Dictionary<string, string> BuildSummary(NachaSoapPayloadBase payload)
        => new(StringComparer.OrdinalIgnoreCase)
        {
            ["OperationCandidate"] = payload.OperationCandidate.ToString(),
            ["CorrelationId"] = payload.CorrelationId,
            ["TransactionId"] = payload.TransactionId?.ToString() ?? string.Empty,
            ["PrenotificationId"] = payload.PrenotificationId?.ToString() ?? string.Empty,
            ["AmountInCents"] = payload.AmountInCents.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["Currency"] = payload.Currency,
            ["ReasonCode"] = payload.ReasonCode,
            ["RequiresMonetaryMovement"] = payload.RequiresMonetaryMovement.ToString(),
            ["Phase"] = payload.Phase
        };

    private static Dictionary<string, string> BuildSummary(NachaIncomingDecision decision, NachaSoapExecutionContext context)
        => new(StringComparer.OrdinalIgnoreCase)
        {
            ["OperationCandidate"] = decision.SoapOperation.ToString(),
            ["CorrelationId"] = context.CorrelationId,
            ["TransactionId"] = decision.TransactionId?.ToString() ?? string.Empty,
            ["PrenotificationId"] = decision.PrenotificationId?.ToString() ?? string.Empty,
            ["AmountInCents"] = context.AmountInCents.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["Currency"] = NormalizeCurrency(context.Currency),
            ["ReasonCode"] = decision.ReasonCode ?? string.Empty,
            ["RequiresMonetaryMovement"] = "False",
            ["Phase"] = "6B.5"
        };

    private static Dictionary<string, string> SanitizeMetadata(IReadOnlyDictionary<string, string> metadata)
        => metadata
            .Where(x => !IsSensitiveKey(x.Key))
            .ToDictionary(x => x.Key, x => MaskAccount(x.Value), StringComparer.OrdinalIgnoreCase);

    private static bool IsSensitiveKey(string key)
        => key.Contains("password", StringComparison.OrdinalIgnoreCase)
           || key.Contains("token", StringComparison.OrdinalIgnoreCase)
           || key.Contains("secret", StringComparison.OrdinalIgnoreCase)
           || key.Contains("credential", StringComparison.OrdinalIgnoreCase);

    private static string MaskAccount(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length < 7)
        {
            return value;
        }

        return $"***{digits[^4..]}";
    }

    private static string NormalizeCurrency(string currency)
        => string.IsNullOrWhiteSpace(currency) ? "COP" : currency.Trim().ToUpperInvariant();

    private static DateTime ResolveCreatedAt(DateTime value)
        => value == default ? DateTime.UtcNow : value;

    private static string ResolveResponseType(NachaIncomingDecisionType decisionType)
        => decisionType switch
        {
            NachaIncomingDecisionType.ApprovePrenotification => "PrenotificationApproved",
            NachaIncomingDecisionType.RejectPrenotification => "PrenotificationRejected",
            NachaIncomingDecisionType.MarkTransactionRejected => "TransactionRejected",
            NachaIncomingDecisionType.MarkTransactionAccepted => "TransactionAccepted",
            _ => "DifferentialResponse"
        };
}
