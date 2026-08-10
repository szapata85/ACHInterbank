using System.Security.Cryptography;
using System.Text;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Models.ExternalFileNames;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using DigitoChequeoHelper = Cfa.ACHInterbank.Application.Helpers.DigitoChequeo.DigitoChequeo;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class AchOutboundReturnArtifactService(
    AchDbContext context,
    INachaFileBuilder nachaFileBuilder,
    INachaFileIdentifierMapService identifierMapService) : IAchOutboundReturnArtifactService
{
    private const string ImmediateDestinationAchColombia = "000101006";

    public async Task<AchOutboundReturnArtifact> BuildAsync(string fileName, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        var safeFileName = Path.GetFileName(fileName.Trim());
        if (!string.Equals(safeFileName, fileName.Trim(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("El nombre de archivo Return Out no es válido.");
        }

        var rows = await context.AchReturnsGenerated
            .AsNoTracking()
            .Include(x => x.OriginalTransaction)
            .Include(x => x.ReturnCycle)
                .ThenInclude(x => x.ClearingHouse)
            .Where(x => x.FileName == safeFileName)
            .OrderBy(x => x.OriginalTransaction.AchBatchId)
            .ThenBy(x => x.OriginalTransactionId)
            .ToListAsync(ct);
        if (rows.Count == 0)
        {
            throw new InvalidOperationException("No existe un Return Out generado con el nombre indicado.");
        }

        var cycleIds = rows.Select(x => x.ReturnCycleId).Distinct(StringComparer.Ordinal).ToArray();
        if (cycleIds.Length != 1)
        {
            throw new InvalidOperationException("El archivo Return Out contiene más de un ciclo persistido.");
        }

        var cycle = rows[0].ReturnCycle;
        if (!IsAchColombia(cycle.ClearingHouse?.Code))
        {
            throw new InvalidOperationException("Solo ACH Colombia dispone de transporte Return Out soportado en este flujo.");
        }

        var participantCodes = rows.Select(x => x.OriginatorEntityCode).Distinct(StringComparer.Ordinal).ToArray();
        if (participantCodes.Length != 1)
        {
            throw new InvalidOperationException("El archivo Return Out no conserva un participante originador único.");
        }

        var contextForName = new ExternalFileNameContext
        {
            ClearingHouseId = cycle.ClearingHouseId,
            ClearingHouseCode = cycle.ClearingHouse?.Code ?? string.Empty,
            ExternalFileType = ExternalFileType.ReturnOut,
            Direction = ExternalFileDirection.Outbound
        };
        var components = ExternalFileNameSupport.Parse(contextForName, safeFileName);
        var sequence = components.ExternalSequence
            ?? throw new InvalidOperationException("No fue posible correlacionar el nombre Return Out con su secuencia externa.");
        var fileIdModifier = await identifierMapService.ResolveIdentifierAsync(sequence, ct);
        var createdAtUtc = rows.Select(x => x.GeneratedAtUtc).Distinct().Single();

        var entries = rows.ToDictionary(
            x => x.OriginalTransactionId,
            x => new NachaReturnOutEntry(
                x.OriginalTransactionId,
                ResolveV35ReturnTransactionCode(x.OriginalTransaction.TransactionCode),
                x.ReceiverEntityCode,
                DigitoChequeoHelper.CalcularDigitoChequeo(x.ReceiverEntityCode).ToString(),
                x.OriginalTransaction.DestinationAccountNumber,
                x.Amount,
                x.OriginalTransaction.RecipientIdNumber,
                x.OriginalTransaction.CompanyName,
                x.OriginalTransaction.DiscretionaryData,
                x.NewSequenceNumber,
                x.ReturnReasonCode,
                x.OriginalSequenceNumber,
                string.Empty,
                x.OriginatorEntityCode,
                string.Empty,
                x.NewSequenceNumber));

        var batches = rows
            .GroupBy(x => x.OriginalTransaction.AchBatchId)
            .OrderBy(x => x.Key)
            .Select((group, index) => BuildBatch(group, entries, index + 1, createdAtUtc))
            .ToList();
        var immediateOrigin = participantCodes[0]
            + DigitoChequeoHelper.CalcularDigitoChequeo(participantCodes[0]);
        var result = await nachaFileBuilder.BuildReturnOutAsync(new NachaReturnOutBuildRequest(
            createdAtUtc,
            fileIdModifier.ToString(),
            ImmediateDestinationAchColombia,
            immediateOrigin,
            "ACH COLOMBIA",
            cycle.ClearingHouse?.Name ?? "CFA",
            "RETURN",
            batches,
            PersistAudit: false), ct);
        var content = Encoding.UTF8.GetBytes(result.Content);

        return new AchOutboundReturnArtifact(
            safeFileName,
            content,
            result.RecordCount,
            rows.Count,
            cycle.Id,
            cycle.ClearingHouseId,
            rows.Select(x => x.OriginalTransactionId).ToArray(),
            Convert.ToHexString(SHA256.HashData(content)));
    }

    private static NachaReturnOutBatch BuildBatch(
        IGrouping<int, AchReturnGenerated> group,
        IReadOnlyDictionary<int, NachaReturnOutEntry> entriesByTransaction,
        int batchNumber,
        DateTime createdAtUtc)
    {
        var rows = group.OrderBy(x => x.OriginalTransactionId).ToList();
        var original = rows[0].OriginalTransaction;
        var entries = rows.Select(x => entriesByTransaction[x.OriginalTransactionId]).ToList();
        var originatingDfi = entries.Select(x => x.NewTraceNumber[..8]).Distinct(StringComparer.Ordinal).Single();
        return new NachaReturnOutBatch(
            ResolveReturnServiceClassCode(entries),
            original.CompanyName,
            string.Empty,
            original.CompanyIdentification,
            "PPD",
            "RETURN",
            original.EffectiveEntryDate,
            createdAtUtc,
            string.Empty,
            originatingDfi,
            batchNumber,
            entries);
    }

    private static string ResolveReturnServiceClassCode(IEnumerable<NachaReturnOutEntry> entries)
    {
        var materialized = entries.ToList();
        var hasCredits = materialized.Any(x => x.TransactionCode is "21" or "31" or "51");
        var hasDebits = materialized.Any(x => x.TransactionCode is "26" or "36" or "56");
        return hasCredits && hasDebits ? "200" : hasCredits ? "220" : "225";
    }

    private static string ResolveV35ReturnTransactionCode(string originalTransactionCode)
        => originalTransactionCode switch
        {
            "21" or "22" or "23" => "21",
            "31" or "32" or "33" => "31",
            "51" or "52" or "53" => "51",
            "26" or "27" or "28" => "26",
            "36" or "37" or "38" => "36",
            "55" or "56" or "57" => "56",
            _ => throw new InvalidOperationException($"RETURN_OUT_ACH_V35_TRANSACTION_CODE_UNSUPPORTED: {originalTransactionCode} no identifica una cuenta admitida por V35 6.6.")
        };

    private static bool IsAchColombia(string? code)
        => string.Equals(code, "ACH", StringComparison.OrdinalIgnoreCase)
           || string.Equals(code, "ACHCOL", StringComparison.OrdinalIgnoreCase)
           || string.Equals(code, "ACHCOLOMBIA", StringComparison.OrdinalIgnoreCase);
}
