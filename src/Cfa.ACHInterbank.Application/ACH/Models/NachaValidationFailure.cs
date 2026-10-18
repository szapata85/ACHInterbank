namespace Cfa.ACHInterbank.Application.ACH.Models;

/// <summary>Describe una falla de validación detectada durante el procesamiento de un registro NACHA-M.</summary>
/// <param name="RecordType">Tipo de registro NACHA-M al que corresponde la falla.</param>
/// <param name="BatchNumber">Número de lote asociado a la falla, cuando puede determinarse.</param>
/// <param name="EntrySequence">Número de secuencia de la entrada asociada, cuando está disponible.</param>
/// <param name="TransactionCode">Código de transacción del registro asociado, cuando está disponible.</param>
/// <param name="Reason">Motivo de validación producido por el parser para la falla.</param>
public record NachaValidationFailure(
    string RecordType,
    string? BatchNumber,
    string? EntrySequence,
    string? TransactionCode,
    string Reason);
