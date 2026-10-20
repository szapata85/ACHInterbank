using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Models.ACH;
using DigitoChequeoHelper = Cfa.ACHInterbank.Application.Helpers.DigitoChequeo.DigitoChequeo;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

internal sealed class AchReturnBatchComposerComponent(IAchReturnTraceSequenceService returnTraceSequenceService)
{
    private const string ImmediateDestinationAchColombia = "000101006";

    public AchReturnPreparedItem Prepare(
        AchTransaction transaction,
        string reasonCode,
        decimal amount,
        AchReturnGenerationScope scope,
        AchReturnCenitOriginalSource? cenitSource,
        AchColombiaR10ReturnBasis? achColombiaR10Basis)
    {
        var originalSequence = NormalizeDigits(transaction.TraceNumber, 15);
        var receiverEntity = NormalizeDigits(transaction.OriginatingDFI, 8);
        var originEntity = NormalizeDigits(transaction.ReceivingDFI, 8);
        var returnTransactionCode = ResolveReturnTransactionCode(transaction.TransactionCode, scope.IsCenit);
        return new AchReturnPreparedItem(
            transaction,
            reasonCode,
            amount,
            originalSequence,
            receiverEntity,
            originEntity,
            returnTransactionCode,
            cenitSource?.StandardEntryClassCode ?? "PPD",
            achColombiaR10Basis);
    }

    public IReadOnlyList<AchReturnPreparedItem> OrderAndValidate(IReadOnlyList<AchReturnPreparedItem> prepared)
    {
        var ordered = prepared
            .OrderBy(item => item.Transaction.AchBatchId)
            .ThenBy(item => item.Transaction.Id)
            .ToList();
        var participantCodes = ordered
            .Select(item => item.OriginEntity)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (participantCodes.Length != 1)
        {
            throw new InvalidOperationException("RETURN_TRACE_PARTICIPANT_SCOPE_INVALID: un archivo ReturnOut debe pertenecer a un único participante generador.");
        }

        return ordered;
    }

    public async Task<AchReturnAllocatedEntries> AllocateAsync(
        IReadOnlyList<AchReturnPreparedItem> prepared,
        AchReturnGenerationScope scope,
        DateTime now,
        CancellationToken ct)
    {
        var participantCode = prepared.Select(item => item.OriginEntity).Distinct(StringComparer.Ordinal).Single();
        var sequenceDate = DateOnly.FromDateTime(scope.ReturnCycle.ProcessingDate.Date);
        var sequenceRange = await returnTraceSequenceService.ReserveRangeAsync(
            participantCode,
            sequenceDate,
            prepared.Count,
            now,
            ct);

        var generatedRows = new List<AchReturnGenerated>(prepared.Count);
        var semanticEntries = new Dictionary<int, NachaReturnOutEntry>(prepared.Count);
        for (var index = 0; index < prepared.Count; index++)
        {
            var item = prepared[index];
            var newSequence = $"{item.OriginEntity}{sequenceRange.StartValue + index:0000000}";
            semanticEntries[item.Transaction.Id] = new NachaReturnOutEntry(
                item.Transaction.Id,
                item.ReturnTransactionCode,
                item.ReceiverEntity,
                DigitoChequeoHelper.CalcularDigitoChequeo(item.ReceiverEntity).ToString(),
                item.Transaction.DestinationAccountNumber,
                item.Amount,
                item.Transaction.RecipientIdNumber,
                item.Transaction.CompanyName,
                item.Transaction.DiscretionaryData,
                newSequence,
                item.ReasonCode,
                item.OriginalSequence,
                string.Empty,
                item.OriginEntity,
                string.Empty,
                newSequence);

            generatedRows.Add(new AchReturnGenerated
            {
                OriginalTransactionId = item.Transaction.Id,
                ReturnCycleId = scope.ReturnCycle.Id,
                ReturnReasonCode = item.ReasonCode,
                Amount = item.Amount,
                NewSequenceNumber = newSequence,
                OriginalSequenceNumber = item.OriginalSequence,
                ReceiverEntityCode = item.ReceiverEntity,
                OriginatorEntityCode = item.OriginEntity,
                FileName = string.Empty,
                SequenceDate = sequenceDate,
                GeneratedAtUtc = now
            });
        }

        return new AchReturnAllocatedEntries(generatedRows, semanticEntries, participantCode, sequenceDate);
    }

    public AchReturnAssemblyResult BuildAssembly(
        IReadOnlyList<AchReturnPreparedItem> prepared,
        AchReturnGenerationScope scope,
        IReadOnlyDictionary<int, AchReturnCenitOriginalSource> cenitSources,
        AchReturnAllocatedEntries allocated,
        DateTime now)
    {
        var batches = prepared
            .GroupBy(item => new { item.Transaction.AchBatchId, item.StandardEntryClassCode })
            .OrderBy(group => group.Key.AchBatchId)
            .ThenBy(group => group.Key.StandardEntryClassCode)
            .Select((group, index) => BuildReturnOutBatch(group, allocated.EntriesByTransaction, index + 1, now))
            .ToList();
        var immediateOrigin = allocated.ParticipantCode
            + DigitoChequeoHelper.CalcularDigitoChequeo(allocated.ParticipantCode).ToString();
        var header = ResolveReturnHeader(prepared, scope.ReturnCycle, immediateOrigin, cenitSources, scope.IsCenit);

        return new AchReturnAssemblyResult(
            allocated.GeneratedRows,
            batches,
            header,
            allocated.ParticipantCode,
            allocated.SequenceDate);
    }

    private static AchReturnHeader ResolveReturnHeader(
        IReadOnlyCollection<AchReturnPreparedItem> prepared,
        AchCycle cycle,
        string immediateOrigin,
        IReadOnlyDictionary<int, AchReturnCenitOriginalSource> cenitSources,
        bool isCenit)
    {
        if (!isCenit)
        {
            return new AchReturnHeader(
                ImmediateDestinationAchColombia,
                immediateOrigin,
                "ACH COLOMBIA",
                cycle.ClearingHouse?.Name ?? "CFA");
        }

        var headers = prepared
            .Select(item => cenitSources[item.Transaction.Id])
            .Select(source => new AchReturnHeader(
                source.ImmediateOrigin,
                source.ImmediateDestination,
                source.ImmediateOriginName,
                source.ImmediateDestinationName))
            .Distinct()
            .ToList();
        if (headers.Count != 1)
        {
            throw new InvalidOperationException("CENIT_RETURN_HEADER_SCOPE_INVALID: el archivo mezcla participantes inmediatos de archivos originales distintos.");
        }

        return headers[0];
    }

    private static NachaReturnOutBatch BuildReturnOutBatch(
        IEnumerable<AchReturnPreparedItem> group,
        IReadOnlyDictionary<int, NachaReturnOutEntry> entriesByTransaction,
        int batchNumber,
        DateTime now)
    {
        var prepared = group.OrderBy(item => item.Transaction.Id).ToList();
        var original = prepared[0].Transaction;
        var entries = prepared.Select(item => entriesByTransaction[item.Transaction.Id]).ToList();
        var serviceClassCode = ResolveReturnServiceClassCode(entries);
        var originatingDfi = entries.Select(entry => entry.NewTraceNumber[..8]).Distinct(StringComparer.Ordinal).Single();

        return new NachaReturnOutBatch(
            serviceClassCode,
            original.CompanyName,
            string.Empty,
            original.CompanyIdentification,
            prepared[0].StandardEntryClassCode,
            "RETURN",
            original.EffectiveEntryDate,
            now,
            string.Empty,
            originatingDfi,
            batchNumber,
            entries);
    }

    private static string ResolveReturnServiceClassCode(IEnumerable<NachaReturnOutEntry> entries)
    {
        var materialized = entries.ToList();
        var hasCredits = materialized.Any(entry => entry.TransactionCode is "21" or "31" or "51");
        var hasDebits = materialized.Any(entry => entry.TransactionCode is "26" or "36" or "56");
        return hasCredits && hasDebits ? "200" : hasCredits ? "220" : "225";
    }

    private static string ResolveReturnTransactionCode(string originalTransactionCode, bool isCenit)
        => originalTransactionCode switch
        {
            "21" or "22" or "23" => "21",
            "31" or "32" or "33" => "31",
            "51" or "52" or "53" => "51",
            "26" or "27" or "28" => "26",
            "36" or "37" or "38" => "36",
            "55" or "56" or "57" => "56",
            _ => throw new InvalidOperationException($"{(isCenit ? "CENIT_RETURN_TRANSACTION_CODE_UNSUPPORTED" : "RETURN_OUT_ACH_V35_TRANSACTION_CODE_UNSUPPORTED")}: {originalTransactionCode} no identifica una cuenta admitida.")
        };

    private static string NormalizeDigits(string? value, int length)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length > length)
        {
            digits = digits[^length..];
        }

        return digits.PadLeft(length, '0');
    }
}
