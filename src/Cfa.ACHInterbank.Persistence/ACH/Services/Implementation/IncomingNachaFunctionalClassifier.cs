using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class IncomingNachaFunctionalClassifier : IIncomingNachaFunctionalClassifier
{
    private static readonly HashSet<string> CreditCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "21", "22", "23", "31", "32", "33", "42", "51", "52", "53"
    };

    private static readonly HashSet<string> DebitCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "26", "27", "28", "36", "37", "38", "55", "56", "57"
    };

    private static readonly HashSet<string> PrenoteCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "23", "33", "53", "28", "38", "57"
    };

    private static readonly HashSet<string> ReturnCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "21", "26", "31", "36", "51", "56"
    };

    public IncomingNachaClassificationResult Classify(EntryDetail entry, AddendaRecord? addenda)
    {
        var code = (entry.TransactionCode ?? string.Empty).Trim();
        var isCredit = CreditCodes.Contains(code);
        var isDebit = DebitCodes.Contains(code);
        var isPrenote = PrenoteCodes.Contains(code) && entry.Amount.GetValueOrDefault() == 0m;
        var isReturn = ReturnCodes.Contains(code) && addenda is not null && string.Equals(addenda.CodeTypeAddendumRecord?.Trim(), "99", StringComparison.OrdinalIgnoreCase);

        IncomingNachaFunctionalClass functionalClass;
        IncomingNachaEligibilityStatus eligibility;
        IncomingNachaPrenoteStatus prenoteStatus = IncomingNachaPrenoteStatus.NoAplica;
        string meaning;
        bool requiresLink = true;
        bool requiresManual = false;

        if (!isCredit && !isDebit)
        {
            functionalClass = IncomingNachaFunctionalClass.Inconsistente;
            eligibility = IncomingNachaEligibilityStatus.Bloqueada;
            meaning = "Código de transacción no soportado para clasificación funcional.";
            requiresManual = true;
        }
        else if (isPrenote)
        {
            functionalClass = IncomingNachaFunctionalClass.Prenotificacion;
            eligibility = IncomingNachaEligibilityStatus.Elegible;
            prenoteStatus = IncomingNachaPrenoteStatus.Pendiente;
            meaning = "Prenotificación entrante pendiente de resolución contra tercero/contraparte.";
        }
        else if (isReturn)
        {
            var reason = (addenda?.ReturnReasonCode ?? string.Empty).Trim().ToUpperInvariant();
            if (CenitReturnIn2026Layout.IsReturnOfReturnCause(reason))
            {
                functionalClass = IncomingNachaFunctionalClass.Inconsistente;
                eligibility = IncomingNachaEligibilityStatus.RevisionManual;
                meaning = "Devolución de una devolución (ROR) identificada; no pertenece al flujo Return In ordinario.";
                requiresLink = false;
                requiresManual = true;
            }
            else
            {
                functionalClass = reason.StartsWith("DEV", StringComparison.OrdinalIgnoreCase)
                    ? IncomingNachaFunctionalClass.RetornoEpr
                    : reason.StartsWith("R", StringComparison.OrdinalIgnoreCase)
                        ? IncomingNachaFunctionalClass.Devolucion
                        : IncomingNachaFunctionalClass.RechazadaOperador;
                eligibility = IncomingNachaEligibilityStatus.PendienteResolucion;
                meaning = "Devolución/Rechazo entrante identificado por addenda 99; requiere reconciliación obligatoria.";
            }
        }
        else if (isCredit)
        {
            functionalClass = IncomingNachaFunctionalClass.CreditoEntrante;
            eligibility = IncomingNachaEligibilityStatus.Elegible;
            meaning = "Crédito entrante clasificado por código de transacción NACHA.";
        }
        else
        {
            functionalClass = IncomingNachaFunctionalClass.DebitoEntrante;
            eligibility = IncomingNachaEligibilityStatus.Elegible;
            meaning = "Débito entrante clasificado por código de transacción NACHA.";
        }

        return new IncomingNachaClassificationResult
        {
            FunctionalClass = functionalClass,
            EligibilityStatus = eligibility,
            RequiresLink = requiresLink,
            RequiresManualResolution = requiresManual,
            OriginalTraceRef = addenda?.OriginalTraceNumber?.Trim(),
            ReturnReasonCode = addenda?.ReturnReasonCode?.Trim(),
            PrenoteStatus = prenoteStatus,
            BusinessMeaning = meaning,
            ClassifierVersion = "v1.2.0",
            ClassificationEvidenceJson = JsonSerializer.Serialize(new
            {
                code,
                amount = entry.Amount,
                addendaType = addenda?.CodeTypeAddendumRecord,
                addendaReason = addenda?.ReturnReasonCode,
                addendaOriginalTrace = addenda?.OriginalTraceNumber
            })
        };
    }
}
