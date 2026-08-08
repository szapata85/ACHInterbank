namespace Cfa.ACHInterbank.Application.ACH.Models;

public sealed record NachaReturnOutBuildRequest(
    DateTime CreatedAtUtc,
    string FileIdModifier,
    string ImmediateDestination,
    string ImmediateOrigin,
    string ImmediateDestinationName,
    string ImmediateOriginName,
    string ReferenceCode,
    IReadOnlyList<NachaReturnOutBatch> Batches,
    bool PersistAudit = true);

public sealed record NachaReturnOutBatch(
    string ServiceClassCode,
    string CompanyName,
    string CompanyDiscretionaryData,
    string CompanyIdentification,
    string StandardEntryClassCode,
    string CompanyEntryDescription,
    DateTime CompanyDescriptiveDate,
    DateTime EffectiveEntryDate,
    string SettlementDate,
    string OriginatingDfi,
    int BatchNumber,
    IReadOnlyList<NachaReturnOutEntry> Entries);

public sealed record NachaReturnOutEntry(
    int TransactionId,
    string TransactionCode,
    string ReceivingDfi,
    string CheckDigit,
    string AccountNumber,
    decimal Amount,
    string IndividualIdentification,
    string IndividualName,
    string DiscretionaryData,
    string NewTraceNumber,
    string ReturnReasonCode,
    string OriginalTraceNumber,
    string DeathDate,
    string OriginalReceivingDfi,
    string AdditionalInformation,
    string AddendaSequenceNumber);

public sealed record NachaReturnOutBuildResult(
    string Content,
    int RecordCount,
    string ProfileCode,
    string NormativeVersion,
    bool LegacyFallbackUsed);
