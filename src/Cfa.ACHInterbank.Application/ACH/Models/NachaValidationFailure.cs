namespace Cfa.ACHInterbank.Application.ACH.Models;

public record NachaValidationFailure(
    string RecordType,
    string? BatchNumber,
    string? EntrySequence,
    string? TransactionCode,
    string Reason);
