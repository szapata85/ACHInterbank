using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

internal sealed record AchReturnGenerationScope(
    AchCycle OriginalCycle,
    AchCycle ReturnCycle,
    string? PaymentRailCode,
    bool IsAchColombia,
    bool IsCenit);

internal sealed record AchReturnCenitOriginalSource(
    string StandardEntryClassCode,
    string ImmediateDestination,
    string ImmediateOrigin,
    string ImmediateDestinationName,
    string ImmediateOriginName);

internal sealed record AchReturnCenitCycleEvidence(
    int OriginalCycleNumber,
    int ReturnCycleNumber,
    int LastReturnCycleNumber);

internal sealed record AchReturnCorrelationResult(
    AchReturnGenerationScope Scope,
    IReadOnlyList<int> SelectedIds,
    IReadOnlyDictionary<int, AchTransaction> TransactionsById,
    IReadOnlyDictionary<int, AchReturnCenitOriginalSource> CenitSources,
    AchReturnCenitCycleEvidence? CenitCycleEvidence);

internal sealed record AchReturnPreparedItem(
    AchTransaction Transaction,
    string ReasonCode,
    decimal Amount,
    string OriginalSequence,
    string ReceiverEntity,
    string OriginEntity,
    string ReturnTransactionCode,
    string StandardEntryClassCode,
    AchColombiaR10ReturnBasis? AchColombiaR10Basis);

internal sealed record AchReturnHeader(
    string ImmediateDestination,
    string ImmediateOrigin,
    string ImmediateDestinationName,
    string ImmediateOriginName);

internal sealed record AchReturnAllocatedEntries(
    IReadOnlyList<AchReturnGenerated> GeneratedRows,
    IReadOnlyDictionary<int, NachaReturnOutEntry> EntriesByTransaction,
    string ParticipantCode,
    DateOnly SequenceDate);

internal sealed record AchReturnAssemblyResult(
    IReadOnlyList<AchReturnGenerated> GeneratedRows,
    IReadOnlyList<NachaReturnOutBatch> Batches,
    AchReturnHeader Header,
    string ParticipantCode,
    DateOnly SequenceDate);

internal sealed record AchReturnArtifactResult(
    string FileName,
    string Content,
    int RecordCount);
