using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;

namespace Cfa.ACHInterbank.Application.ACH.Services;

public sealed class CenitIncomingReturnPolicy : ICenitIncomingReturnPolicy
{
    private const string R06 = "R06";
    private const string R23 = "R23";

    public static IReadOnlyList<CenitReturnCauseDefinition> CauseDefinitions { get; } =
    [
        Cause("R01", "Fondos insuficientes", false, true, false, false, 0),
        Cause("R02", "Cuenta cerrada", true, true, true, true, 0),
        Cause("R03", "Cuenta no abierta", true, true, true, true, 0),
        Cause("R04", "Numero de cuenta invalido", true, true, true, true, 0),
        Cause(R06, "Devolucion solicitada por el Participante Originador o por el Originador", false, true, false, true, null),
        Cause("R07", "Autorizacion de recaudo revocada por el Receptor", false, true, false, false, 0),
        Cause("R08", "Orden de no pago", false, true, false, false, 0),
        Cause("R09", "Fondos no disponibles", false, true, false, false, 0),
        Cause("R10", "No existe prenotificacion", false, true, false, false, 0),
        Cause("R12", "Originador no autorizado", false, true, false, false, 0),
        Cause("R13", "Devolucion de entrada debito por solicitud del Receptor", false, true, false, false, 0),
        Cause("R14", "Muerte del delegado o representante", true, true, false, false, 0),
        Cause("R15", "Muerte del beneficiario o titular de la cuenta", true, true, false, false, 0),
        Cause("R16", "Cuenta inactiva o bloqueada", true, true, true, true, 0),
        Cause("R17", "Identificacion no coincide con la cuenta del Receptor", true, true, true, true, 0),
        Cause("R20", "Cuenta no habilitada para recibir transacciones", true, true, true, true, 0),
        Cause(R23, "Devolucion de entrada credito por solicitud del Receptor", false, false, false, true, 15),
        Cause("R29", "Devolucion de entrada debito por solicitud del Receptor corporativo", true, true, false, false, 0),
        Cause("R31", "Prenotificacion debito no procesada por el Participante Receptor", true, false, false, false, 0),
        Cause("R32", "Entrada credito no procesada por el Participante Receptor", false, false, false, true, 0),
        Cause("R33", "Deposito electronico excede los limites establecidos", false, true, false, true, 0),
        Cause("R34", "Cancelacion manual", true, true, true, true, 0),
        Cause("R35", "Tipo de cuenta errada", true, true, true, true, 0)
    ];

    public CenitIncomingReturnPolicyResult Evaluate(CenitIncomingReturnPolicyRequest request)
    {
        var code = (request.ReturnReasonCode ?? string.Empty).Trim().ToUpperInvariant();
        if (code.StartsWith('D'))
        {
            return Reject("CENIT_FILE_REJECTION_NOT_RETURN", "Las causales Dxx corresponden a rechazo de archivos STA, no a devoluciones transaccionales.");
        }

        var cause = CauseDefinitions.SingleOrDefault(x => string.Equals(x.Code, code, StringComparison.Ordinal));
        if (cause is null)
        {
            return Reject("CENIT_RETURN_CAUSE_NOT_SUPPORTED", $"La causal {code} no pertenece al catalogo CENIT Return In vigente.");
        }

        var applicability = EvaluateApplicability(request, cause);
        if (applicability is not null)
        {
            return applicability;
        }

        if (request.ReturnValueDate.Date < request.OriginalValueDate.Date)
        {
            return Reject("CENIT_RETURN_VALUE_DATE_BEFORE_ORIGINAL", "La Fecha Valor de la devolucion no puede ser anterior a la Fecha Valor original.");
        }

        var cycleValidation = ValidateKnownCycles(request);
        if (cycleValidation is not null)
        {
            return cycleValidation;
        }

        if (code == R06)
        {
            return EvaluateR06(request);
        }

        if (code == R23)
        {
            return EvaluateR23(request);
        }

        return request.OriginalTransactionType == TransactionTypeEnum.Prenotification
            ? EvaluatePrenotification(request)
            : EvaluateOrdinaryMonetaryReturn(request);
    }

    private static CenitIncomingReturnPolicyResult? EvaluateApplicability(
        CenitIncomingReturnPolicyRequest request,
        CenitReturnCauseDefinition cause)
    {
        if (request.OriginalTransactionType == TransactionTypeEnum.Debit && cause.AppliesToDebitMonetary)
        {
            return null;
        }

        if (request.OriginalTransactionType == TransactionTypeEnum.Credit && cause.AppliesToCreditMonetary)
        {
            return null;
        }

        if (request.OriginalTransactionType != TransactionTypeEnum.Prenotification)
        {
            return Reject("CENIT_RETURN_CAUSE_NOT_APPLICABLE", $"La causal {cause.Code} no aplica al tipo {request.OriginalTransactionType}.");
        }

        return request.PrenotificationDirection switch
        {
            CenitPrenotificationDirection.Debit when cause.AppliesToDebitPrenotification => null,
            CenitPrenotificationDirection.Credit when cause.AppliesToCreditPrenotification => null,
            CenitPrenotificationDirection.Unknown when cause.AppliesToDebitPrenotification && cause.AppliesToCreditPrenotification => null,
            CenitPrenotificationDirection.Unknown when cause.AppliesToDebitPrenotification || cause.AppliesToCreditPrenotification
                => Manual("CENIT_PRENOTE_DIRECTION_REQUIRED", $"La causal {cause.Code} requiere conocer si la prenotificacion original era debito o credito."),
            _ => Reject("CENIT_RETURN_CAUSE_NOT_APPLICABLE", $"La causal {cause.Code} no aplica a la prenotificacion original.")
        };
    }

    private static CenitIncomingReturnPolicyResult? ValidateKnownCycles(CenitIncomingReturnPolicyRequest request)
    {
        if (!request.OriginalCycleNumber.HasValue || !request.ReturnCycleNumber.HasValue || !request.LastReturnCycleNumber.HasValue)
        {
            return Manual("CENIT_RETURN_CYCLE_EVIDENCE_REQUIRED", "No existe evidencia normalizada suficiente para validar los ciclos CENIT.");
        }

        if (request.OriginalCycleNumber <= 0 || request.ReturnCycleNumber <= 0 || request.LastReturnCycleNumber <= 0
            || request.ReturnCycleNumber > request.LastReturnCycleNumber)
        {
            return Reject("CENIT_RETURN_CYCLE_INVALID", "El ciclo de la devolucion no pertenece a la ventana operacional CENIT resuelta.");
        }

        return null;
    }

    private static CenitIncomingReturnPolicyResult EvaluateOrdinaryMonetaryReturn(CenitIncomingReturnPolicyRequest request)
    {
        if (request.ReturnValueDate.Date != request.OriginalValueDate.Date)
        {
            return Reject("CENIT_ORDINARY_RETURN_VALUE_DATE_MISMATCH", "La devolucion ordinaria debe conservar la misma Fecha Valor.");
        }

        return IsImmediateFollowingCycle(request.OriginalCycleNumber!.Value, request.ReturnCycleNumber!.Value)
            ? Allow("CENIT_ORDINARY_RETURN_ALLOWED", "Devolucion ordinaria recibida en el ciclo inmediatamente siguiente y con la misma Fecha Valor.")
            : Reject("CENIT_ORDINARY_RETURN_NOT_NEXT_CYCLE", "La devolucion ordinaria debe recibirse en el ciclo inmediatamente siguiente.");
    }

    private static CenitIncomingReturnPolicyResult EvaluatePrenotification(CenitIncomingReturnPolicyRequest request)
    {
        if (request.ReturnValueDate.Date != request.OriginalValueDate.Date)
        {
            return Reject("CENIT_PRENOTE_RETURN_VALUE_DATE_MISMATCH", "La devolucion de prenotificacion debe conservar la misma Fecha Valor.");
        }

        return request.ReturnCycleNumber > request.OriginalCycleNumber
            && request.ReturnCycleNumber <= request.LastReturnCycleNumber
            ? Allow("CENIT_PRENOTE_RETURN_ALLOWED", "Devolucion de prenotificacion recibida dentro del ultimo ciclo de devoluciones del dia operacional.")
            : Reject("CENIT_PRENOTE_RETURN_CYCLE_INVALID", "La devolucion de prenotificacion debe ocurrir despues de la entrada y a mas tardar en el ultimo ciclo de devoluciones del dia.");
    }

    private static CenitIncomingReturnPolicyResult EvaluateR06(CenitIncomingReturnPolicyRequest request)
    {
        if (request.ReturnedAmount != request.OriginalAmount)
        {
            return Reject("CENIT_R06_PARTIAL_OR_AMOUNT_MISMATCH", "R06 solo permite devolver el valor original completo.");
        }

        if (!request.ReturnRequestDate.HasValue
            || !request.ImmediateReturnCycleConfirmed.HasValue
            || !request.ConfirmationToOriginatorRecorded.HasValue)
        {
            return Manual("CENIT_R06_OPERATIONAL_EVIDENCE_REQUIRED", "R06 requiere evidencia normalizada de la solicitud, el ciclo inmediato y la confirmacion al Participante Originador.");
        }

        if (!request.ImmediateReturnCycleConfirmed.Value)
        {
            return Reject("CENIT_R06_NOT_IMMEDIATE", "R06 debe enviarse dentro del ciclo de devoluciones mas inmediato a la solicitud.");
        }

        if (request.ReturnRequestDate.Value.Date < request.OriginalValueDate.Date
            || request.ReturnRequestDate.Value.Date > request.ReturnValueDate.Date)
        {
            return Reject("CENIT_R06_REQUEST_DATE_INVALID", "La fecha de solicitud R06 no es consistente con la operacion original y la devolucion.");
        }

        if (!request.ConfirmationToOriginatorRecorded.Value)
        {
            return Manual("CENIT_R06_CONFIRMATION_REQUIRED", "R06 requiere confirmacion al Participante Originador de la aplicacion de la causal.");
        }

        if (request.OriginalTransactionType == TransactionTypeEnum.Credit)
        {
            if (!request.FundsAvailabilityRequired.HasValue)
            {
                return Manual("CENIT_R06_FUNDS_APPLICABILITY_REQUIRED", "R06 credito requiere determinar si la solicitud fue posterior al abono y exige disponibilidad de fondos.");
            }

            if (request.FundsAvailabilityRequired.Value && request.FundsAvailabilityConfirmed is not true)
            {
                return Reject("CENIT_R06_FUNDS_NOT_AVAILABLE", "R06 no puede aplicarse automaticamente sin disponibilidad de fondos cuando esta es exigible.");
            }
        }

        return Allow("CENIT_R06_ALLOWED", "R06 conserva valor original, evidencia de solicitud, disponibilidad y ciclo inmediato.");
    }

    private static CenitIncomingReturnPolicyResult EvaluateR23(CenitIncomingReturnPolicyRequest request)
    {
        if (request.ReturnedAmount != request.OriginalAmount)
        {
            return Reject("CENIT_R23_PARTIAL_OR_AMOUNT_MISMATCH", "R23 solo permite devolver el valor original completo.");
        }

        var elapsedDays = (request.ReturnValueDate.Date - request.OriginalValueDate.Date).Days;
        if (elapsedDays > 15)
        {
            return Reject("CENIT_R23_MAX_CALENDAR_DAYS_EXCEEDED", "R23 excede quince dias calendario desde la entrada al Participante Receptor.");
        }

        if (!request.ReturnRequestDate.HasValue || !request.ImmediateReturnCycleConfirmed.HasValue)
        {
            return Manual("CENIT_R23_REQUEST_EVIDENCE_REQUIRED", "R23 requiere evidencia normalizada de la reclamacion del Receptor y del ciclo de devoluciones mas inmediato.");
        }

        if (request.ReturnRequestDate.Value.Date < request.OriginalValueDate.Date
            || request.ReturnRequestDate.Value.Date > request.ReturnValueDate.Date)
        {
            return Reject("CENIT_R23_REQUEST_DATE_INVALID", "La fecha de reclamacion R23 no es consistente con la operacion original y la devolucion.");
        }

        if (!request.ImmediateReturnCycleConfirmed.Value)
        {
            return Reject("CENIT_R23_NOT_IMMEDIATE", "R23 debe enviarse dentro del ciclo de devoluciones mas inmediato a la reclamacion.");
        }

        if (elapsedDays == 0)
        {
            return Allow("CENIT_R23_SAME_DAY_ALLOWED", "R23 del mismo dia conserva Fecha Valor y acredita el ciclo de devoluciones mas inmediato.");
        }

        if (!request.ReceiverRejectionDeadlineDate.HasValue)
        {
            return Manual("CENIT_R23_NOTIFICATION_DEADLINE_REQUIRED", "R23 posterior requiere la fecha limite calculada desde la notificacion del Receptor.");
        }

        return request.ReturnValueDate.Date <= request.ReceiverRejectionDeadlineDate.Value.Date
            ? Allow("CENIT_R23_LATER_ALLOWED", "R23 posterior cumple el maximo de quince dias y el plazo desde la notificacion del Receptor.")
            : Reject("CENIT_R23_NOTIFICATION_DEADLINE_EXCEEDED", "R23 no fue tramitada dentro del Dia Habil Bancario siguiente a la notificacion del Receptor.");
    }

    private static bool IsImmediateFollowingCycle(int originalCycle, int returnCycle)
        => returnCycle == originalCycle + 1;

    private static CenitReturnCauseDefinition Cause(
        string code,
        string description,
        bool debitPrenote,
        bool debitMonetary,
        bool creditPrenote,
        bool creditMonetary,
        int? maxDays)
        => new(code, description, debitPrenote, debitMonetary, creditPrenote, creditMonetary, maxDays);

    private static CenitIncomingReturnPolicyResult Allow(string code, string message)
        => new(CenitIncomingReturnPolicyStatus.Allowed, code, message);

    private static CenitIncomingReturnPolicyResult Reject(string code, string message)
        => new(CenitIncomingReturnPolicyStatus.Rejected, code, message);

    private static CenitIncomingReturnPolicyResult Manual(string code, string message)
        => new(CenitIncomingReturnPolicyStatus.ManualReviewRequired, code, message);
}
