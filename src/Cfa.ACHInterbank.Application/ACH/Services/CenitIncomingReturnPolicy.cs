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

        var firstCycleExceptionValidation = ValidateFirstCycleReturnException(request, code);
        if (firstCycleExceptionValidation is not null)
        {
            return firstCycleExceptionValidation;
        }

        if (code == R06)
        {
            // The fifth CENIT session receives returns of fourth-session entries;
            // operational evidence does not override the session's source restriction.
            if (request.ReturnCycleNumber == 5)
            {
                if (request.OriginalCycleNumber != 4
                    || request.ReturnValueDate.Date != request.OriginalValueDate.Date)
                {
                    return Reject("CENIT_CYCLE5_R06_ORIGIN_C4_REQUIRED", "R06 del ciclo 5 exige una entrada original del ciclo 4 del mismo dia operacional.");
                }

                // Fecha Valor may differ from the actual clearing session date.
                // Both dates must come from the resolved original and return AchCycle,
                // not from the filename, clock time or an untrusted evidence flag.
                if (!request.OriginalReceivedDate.HasValue || !request.ReturnOperationalDate.HasValue)
                {
                    return Manual("CENIT_CYCLE5_R06_OPERATIONAL_DAY_EVIDENCE_REQUIRED",
                        "R06 en C5 requiere fechas operacionales verificables de los ciclos C4 original y C5 receptor.");
                }

                if (request.OriginalReceivedDate.Value.Date != request.ReturnOperationalDate.Value.Date)
                {
                    return Reject("CENIT_CYCLE5_R06_ORIGIN_C4_REQUIRED",
                        "R06 del ciclo 5 no puede devolver una entrada del ciclo 4 de otro dia operacional.");
                }
            }

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
            || request.OriginalCycleNumber > request.LastReturnCycleNumber
            || request.ReturnCycleNumber > request.LastReturnCycleNumber)
        {
            return Reject("CENIT_RETURN_CYCLE_INVALID", "El ciclo de la devolucion no pertenece a la ventana operacional CENIT resuelta.");
        }

        return null;
    }

    private static CenitIncomingReturnPolicyResult? ValidateFirstCycleReturnException(
        CenitIncomingReturnPolicyRequest request,
        string code)
    {
        if (request.ReturnCycleNumber != 1)
        {
            return null;
        }

        if (!string.Equals(code, R23, StringComparison.Ordinal))
        {
            return Reject(
                "CENIT_CYCLE1_RETURN_NOT_ALLOWED",
                "En el ciclo 1 CENIT no se permiten devoluciones ordinarias; la unica excepcion de Return es R23 correspondiente a una entrada de dias anteriores.");
        }

        // A prior Fecha Valor is not proof that the receiving participant obtained
        // the entry on a previous operational day. C1 must not use the R23
        // prior-day exception for an entry actually received today.
        if (request.ReturnValueDate.Date <= request.OriginalValueDate.Date
            || (request.OriginalReceivedDate.HasValue
                && request.OriginalReceivedDate.Value.Date >= request.ReturnValueDate.Date))
        {
            return Reject(
                "CENIT_CYCLE1_R23_PRIOR_DAY_REQUIRED",
                "R23 en el ciclo 1 exige una entrada recibida en un dia operacional anterior; la Fecha Valor por si sola no demuestra esa condicion.");
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

        // An earlier Fecha Valor does NOT turn an entry received today into a
        // prior-day receiver rejection. The original processing/receipt date is
        // persisted from AchCycle and takes precedence for the day classification.
        // The 15-day limit for genuinely later claims is evaluated below against
        // that same receipt date, not against Fecha Valor.
        var sameValueDate = request.ReturnValueDate.Date == request.OriginalValueDate.Date;
        // Fecha Valor is not a reliable clock for a later receiver claim.  The
        // actual return session's processing date is authoritative when known.
        var actualReturnDay = request.ReturnOperationalDate?.Date ?? request.ReturnValueDate.Date;
        var receivedToday = request.OriginalReceivedDate.HasValue
            && request.OriginalReceivedDate.Value.Date == actualReturnDay;
        var receivedBeforeReturnDay = request.OriginalReceivedDate.HasValue
            && request.OriginalReceivedDate.Value.Date < actualReturnDay;

        if (!request.ReturnRequestDate.HasValue || !request.ImmediateReturnCycleConfirmed.HasValue)
        {
            return Manual("CENIT_R23_REQUEST_EVIDENCE_REQUIRED", "R23 requiere evidencia normalizada de la reclamacion del Receptor y del ciclo de devoluciones mas inmediato.");
        }

        var claimDay = CenitR23BankingDate.FromNotificationTimestamp(request.ReturnRequestDate.Value);
        if (claimDay < DateOnly.FromDateTime(request.OriginalValueDate.Date)
            || claimDay > DateOnly.FromDateTime(request.ReturnOperationalDate?.Date ?? request.ReturnValueDate.Date))
        {
            return Reject("CENIT_R23_REQUEST_DATE_INVALID", "La fecha de reclamacion R23 no es consistente con la operacion original y la devolucion.");
        }

        if (!request.ImmediateReturnCycleConfirmed.Value)
        {
            return Reject("CENIT_R23_NOT_IMMEDIATE", "R23 debe enviarse dentro del ciclo de devoluciones mas inmediato a la reclamacion.");
        }

        if (receivedToday || (sameValueDate && !receivedBeforeReturnDay))
        {
            // A return received in today's actual session cannot be reclassified
            // as an older claim by shifting Fecha Valor. Conversely an older
            // receipt cannot be treated as intraday merely by backdating it.
            if (!sameValueDate || (request.ReturnOperationalDate.HasValue
                && request.ReturnValueDate.Date != actualReturnDay))
            {
                return Reject("CENIT_R23_SAME_DAY_VALUE_DATE_MISMATCH",
                    "R23 del mismo dia requiere una Fecha Valor coherente con la operacion y la sesion de devolucion.");
            }

            return IsImmediateFollowingCycle(request.OriginalCycleNumber!.Value, request.ReturnCycleNumber!.Value)
                ? Allow("CENIT_R23_SAME_DAY_NEXT_CYCLE_ALLOWED", "R23 del mismo dia cumple el ciclo inmediatamente siguiente.")
                : Reject("CENIT_R23_SAME_DAY_NOT_NEXT_CYCLE", "R23 del mismo dia no pertenece a la excepcion de devoluciones de dias anteriores.");
        }

        if (!request.OriginalReceivedDate.HasValue)
        {
            return Manual("CENIT_R23_ORIGINAL_RECEIPT_REQUIRED", "R23 posterior requiere fecha verificable de recepcion original, distinta de la Fecha Valor.");
        }

        // For an R23 from earlier days, the processing date of the resolved
        // destination cycle is mandatory.  Never certify timeliness using a
        // caller-provided Fecha Valor as a substitute for actual submission.
        if (!request.ReturnOperationalDate.HasValue)
        {
            return Manual("CENIT_R23_RETURN_OPERATIONAL_DATE_REQUIRED",
                "R23 posterior requiere el dia operacional del ciclo receptor resuelto.");
        }

        var receipt = request.OriginalReceivedDate.Value.Date;
        var submission = request.ReturnOperationalDate.Value.Date;
        if (receipt < request.OriginalValueDate.Date || receipt >= submission)
        {
            return Reject("CENIT_R23_ORIGINAL_RECEIPT_INVALID", "La fecha de entrada al participante receptor no es consistente con la operacion.");
        }

        // The R23 exception for an older receiver rejection takes the value date
        // on which the return is actually processed, not the original value date.
        if (request.ReturnValueDate.Date != submission)
        {
            return Reject("CENIT_R23_LATER_VALUE_DATE_MISMATCH",
                "La Fecha Valor de R23 posterior debe coincidir con el dia operacional de devolucion.");
        }

        if ((submission - receipt).Days > 15)
        {
            return Reject("CENIT_R23_MAX_CALENDAR_DAYS_EXCEEDED", "R23 excede quince dias calendario desde la entrada al Participante Receptor.");
        }

        if (!request.ReceiverNotificationReceivedDate.HasValue)
        {
            return Manual("CENIT_R23_NOTIFICATION_RECEIPT_REQUIRED", "R23 posterior requiere fecha de recepcion de la notificacion del Receptor.");
        }

        var notificationDate = CenitR23BankingDate.FromNotificationTimestamp(request.ReceiverNotificationReceivedDate.Value).ToDateTime(TimeOnly.MinValue);
        if (notificationDate < receipt || notificationDate > submission)
        {
            return Reject("CENIT_R23_NOTIFICATION_DATE_INVALID", "La fecha de notificacion R23 no es consistente con la entrada y la devolucion.");
        }

        // This value must be calculated by the existing bank-holiday calendar in
        // the production caller. Never trust ReceiverRejectionDeadlineDate supplied
        // as operational evidence to grant eligibility.
        if (!request.CalculatedNextBusinessDayDeadline.HasValue)
        {
            return Manual("CENIT_R23_CALENDAR_VALIDATION_REQUIRED", "No se ha calculado el siguiente Dia Habil Bancario desde la notificacion.");
        }

        return submission <= request.CalculatedNextBusinessDayDeadline.Value.Date
            ? Allow("CENIT_R23_LATER_ALLOWED", "R23 posterior cumple el plazo calculado desde la notificacion y los quince dias calendario.")
            : Reject("CENIT_R23_NOTIFICATION_DEADLINE_EXCEEDED", "R23 excede el Dia Habil Bancario siguiente a la notificacion del Receptor.");
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
