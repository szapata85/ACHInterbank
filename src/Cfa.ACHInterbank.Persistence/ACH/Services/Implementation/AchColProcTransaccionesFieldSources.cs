using System.Collections.ObjectModel;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// F5.1: source inventory for ACH Colombia file-based Proc_Transacciones.
/// Candidates are NOT a published or externally homologated CFA contract.
/// F5.2 must resolve conditional/unresolved fields before constructing a request.
/// </summary>
public static class AchColProcTransaccionesFieldSources
{
    public const string DirectCandidate = "DirectCandidate";
    public const string Conditional = "Conditional";
    public const string Unresolved = "Unresolved";

    public sealed record Field(string Parameter, string? CandidateSource, string Status, string Caveat);

    private static readonly IReadOnlyDictionary<string, Field> Inventory =
        new ReadOnlyDictionary<string, Field>(new Dictionary<string, Field>(StringComparer.OrdinalIgnoreCase)
        {
            ["TREG"] = new("TREG", "\"6\" (tipo de registro 6)", Conditional, "Constante de operación; confirmar regla del contrato CFA"),
            ["TIPTRAN"] = new("TIPTRAN", "EntryDetail.TransactionCode", DirectCandidate, "Validación monetaria F4.1; confirmar equivalencia del código"),
            ["BCORECEP"] = new("BCORECEP", "EntryDetail.ReceivingParticipantEntityCode / FinancialInstitutions.TransitCode", Conditional, "Resolver institución y normalizar 3 dígitos, no truncar 8 a 3"),
            ["BCOORIG"] = new("BCOORIG", "BatchHeader.OriginParticipantEntityCode / FinancialInstitutions.TransitCode", Conditional, "Requiere asociación única con catálogo de instituciones"),
            ["NORIG"] = new("NORIG", "BatchHeader.CompanyName / entidad originadora", Conditional, "Homologar si es empresa o institución"),
            ["NCTAORIG"] = new("NCTAORIG", null, Unresolved, "Cuenta del originador no está inequívocamente en EntryDetail receptor"),
            ["IDORIG"] = new("IDORIG", "BatchHeader.CompanyId", Conditional, "Confirmar si es NIT/identificación contractual"),
            ["DESTRAN"] = new("DESTRAN", "BatchHeader.CompanyEntryDescription + Addenda", Conditional, "Normalización de descripción requiere regla aprobada"),
            ["FECEFEC"] = new("FECEFEC", "BatchHeader.EffectiveEntryDate", Conditional, "Formato/fecha efectiva debe ser confirmado"),
            ["NCTARECEP"] = new("NCTARECEP", "EntryDetail.AccountNumber", DirectCandidate, "Cuenta receptora; validar modalidad/tipo"),
            ["MONTO"] = new("MONTO", "EntryDetail.Amount", DirectCandidate, "Confirmar unidad y precisión monetaria"),
            ["NRECEP"] = new("NRECEP", "EntryDetail.RecipUserName", DirectCandidate, "Verificar reglas de longitud y codificación"),
            ["IDRECEP"] = new("IDRECEP", "EntryDetail.RecipIdNumber", DirectCandidate, "Verificar tipo documento/longitud"),
            ["DISCRE"] = new("DISCRE", "EntryDetail.DiscreData", DirectCandidate, "Opcional; no enviar por defecto sin mapeo publicado"),
            ["CONV"] = new("CONV", null, Unresolved, "No hay origen inequívoco de convenio"),
            ["PROD"] = new("PROD", null, Unresolved, "No hay origen inequívoco de producto"),
            ["INFPAG"] = new("INFPAG", "AddendaRecord.PaymentRelatedInformation + BatchHeader", Conditional, "Debe validar addenda y política de información de pago"),
            ["IDTRAN"] = new("IDTRAN", "EntryDetail.SequenceNumber (últimos 7)", Conditional, "Trace de 15 dígitos y rango/transacción homologada; no usar ID de BD"),
            ["IDLOTE"] = new("IDLOTE", "BatchHeader.BatchNumber", Conditional, "Convertir 7 dígitos a 6 solo bajo política de rango existente"),
            ["REGLOTE"] = new("REGLOTE", null, Unresolved, "Registro secuencial CFA aún no homologado"),
            ["IREVER"] = new("IREVER", null, Unresolved, "Confirmar semántica de indicador 0/1 y tipos de movimiento"),
            ["LIBRE"] = new("LIBRE", null, Unresolved, "Sin regla de negocio aprobada"),
            ["IDCAMCOMPE"] = new("IDCAMCOMPE", "ClearingHouse ACHCOL", Conditional, "Identificador CFA/cámara requiere homologación; nunca ciclo externo N"),
            ["DIRECCIONIP"] = new("DIRECCIONIP", null, Unresolved, "IP no aparece en NACHA; no inventar"),
            ["LIBRE1"] = new("LIBRE1", null, Unresolved, "Sin regla de negocio aprobada"),
        });

    public static IReadOnlyDictionary<string, Field> Fields => Inventory;

    public static Field Get(string parameter)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parameter);
        return Inventory.TryGetValue(parameter, out var field)
            ? field
            : throw new InvalidOperationException($"PROC_TRANSACCIONES_ACHCOL_FIELD_UNRECOGNIZED_F51: {parameter}");
    }

    public static void RequireHomologatedMapping(bool externallyApproved)
    {
        if (!externallyApproved || Inventory.Values.Any(x => x.Status != DirectCandidate))
        {
            throw new InvalidOperationException(
                "PROC_TRANSACCIONES_ACHCOL_MAPPING_NOT_HOMOLOGATED_F51: no liberar solicitud CFA basada en fuentes candidatas.");
        }
    }
}
