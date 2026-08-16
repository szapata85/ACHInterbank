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
        var isAchColombia = IsAchColombia(cycle.ClearingHouse?.Code);
        var isCenit = IsCenit(cycle.ClearingHouse?.Code);
        if (!isAchColombia && !isCenit)
        {
            throw new InvalidOperationException("La cámara no dispone de artefacto Return Out soportado en este flujo.");
        }

        var cenitSources = isCenit
            ? await LoadCenitOriginalSourcesAsync(rows.Select(row => row.OriginalTransactionId).ToArray(), ct)
            : new Dictionary<int, CenitOriginalSource>();

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
                ResolveReturnTransactionCode(x.OriginalTransaction.TransactionCode, isCenit),
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
            .GroupBy(x => new
            {
                x.OriginalTransaction.AchBatchId,
                StandardEntryClassCode = isCenit ? cenitSources[x.OriginalTransactionId].StandardEntryClassCode : "PPD"
            })
            .OrderBy(x => x.Key.AchBatchId)
            .ThenBy(x => x.Key.StandardEntryClassCode)
            .Select((group, index) => BuildBatch(group, entries, group.Key.StandardEntryClassCode, index + 1, createdAtUtc))
            .ToList();
        var immediateOrigin = participantCodes[0]
            + DigitoChequeoHelper.CalcularDigitoChequeo(participantCodes[0]);
        var header = isCenit
            ? ResolveCenitHeader(cenitSources.Values)
            : new ReturnHeader(ImmediateDestinationAchColombia, immediateOrigin, "ACH COLOMBIA", cycle.ClearingHouse?.Name ?? "CFA");
        var result = await nachaFileBuilder.BuildReturnOutAsync(new NachaReturnOutBuildRequest(
            createdAtUtc,
            fileIdModifier.ToString(),
            header.ImmediateDestination,
            header.ImmediateOrigin,
            header.ImmediateDestinationName,
            header.ImmediateOriginName,
            "RETURN",
            batches,
            PersistAudit: false,
            ClearingHouseCode: isCenit ? "CENIT" : "ACH",
            ClearingHouseName: cycle.ClearingHouse?.Name ?? (isCenit ? "CENIT" : "ACH Colombia"),
            NormativeVersion: isCenit ? CenitReturnOut2026Layout.NormativeVersion : "V35"), ct);
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

    private async Task<Dictionary<int, CenitOriginalSource>> LoadCenitOriginalSourcesAsync(
        IReadOnlyCollection<int> transactionIds,
        CancellationToken ct)
    {
        var rows = await context.IncomingNachaTransactionLinks
            .AsNoTracking()
            .Where(link => link.IsFinal
                && link.AchTransactionId.HasValue
                && transactionIds.Contains(link.AchTransactionId.Value)
                && link.EntryDetail != null
                && link.EntryDetail.NachaHeader != null
                && link.EntryDetail.BatchHeader != null)
            .Select(link => new
            {
                TransactionId = link.AchTransactionId!.Value,
                Sec = link.EntryDetail!.BatchHeader!.StandardEntryClassCode,
                Destination = link.EntryDetail.NachaHeader!.ImmediateDestination,
                Origin = link.EntryDetail.NachaHeader.ImmediateOrigin,
                DestinationName = link.EntryDetail.NachaHeader.ImmediateDestinationName,
                OriginName = link.EntryDetail.NachaHeader.ImmediateOriginName
            })
            .ToListAsync(ct);

        var result = new Dictionary<int, CenitOriginalSource>();
        foreach (var transactionId in transactionIds)
        {
            var candidates = rows.Where(row => row.TransactionId == transactionId)
                .Select(row => new CenitOriginalSource(
                    NormalizeCenitSec(row.Sec),
                    NormalizeDigits(row.Destination, 10),
                    NormalizeDigits(row.Origin, 10),
                    (row.DestinationName ?? string.Empty).Trim(),
                    (row.OriginName ?? string.Empty).Trim()))
                .Distinct()
                .ToList();
            if (candidates.Count != 1)
            {
                throw new InvalidOperationException($"CENIT_RETURN_ORIGINAL_RAW_EVIDENCE_REQUIRED: la transacción {transactionId} no tiene un único registro 1/5/6 original trazable.");
            }

            result[transactionId] = candidates[0];
        }

        return result;
    }

    private static ReturnHeader ResolveCenitHeader(IEnumerable<CenitOriginalSource> sources)
    {
        var headers = sources
            .Select(source => new ReturnHeader(
                source.ImmediateOrigin,
                source.ImmediateDestination,
                source.ImmediateOriginName,
                source.ImmediateDestinationName))
            .Distinct()
            .ToList();
        return headers.Count == 1
            ? headers[0]
            : throw new InvalidOperationException("CENIT_RETURN_HEADER_SCOPE_INVALID: el archivo mezcla participantes inmediatos de archivos originales distintos.");
    }

    private static string NormalizeCenitSec(string? value)
    {
        var sec = (value ?? string.Empty).Trim().ToUpperInvariant();
        return sec switch
        {
            "PPD" or "CCD" => sec,
            "CTX" => throw new InvalidOperationException(CenitReturnIn2026Layout.CtxScopeStatus),
            _ => throw new InvalidOperationException($"CENIT_RETURN_SEC_NOT_SUPPORTED: SEC {sec} no está soportado por Return Out CENIT.")
        };
    }

    private static string NormalizeDigits(string? value, int length)
    {
        var digits = new string((value ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length > length)
        {
            digits = digits[^length..];
        }

        return digits.PadLeft(length, '0');
    }

    private sealed record CenitOriginalSource(
        string StandardEntryClassCode,
        string ImmediateDestination,
        string ImmediateOrigin,
        string ImmediateDestinationName,
        string ImmediateOriginName);

    private sealed record ReturnHeader(
        string ImmediateDestination,
        string ImmediateOrigin,
        string ImmediateDestinationName,
        string ImmediateOriginName);

    private static NachaReturnOutBatch BuildBatch(
        IEnumerable<AchReturnGenerated> group,
        IReadOnlyDictionary<int, NachaReturnOutEntry> entriesByTransaction,
        string standardEntryClassCode,
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
            standardEntryClassCode,
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

    private static bool IsAchColombia(string? code)
        => string.Equals(code, "ACH", StringComparison.OrdinalIgnoreCase)
           || string.Equals(code, "ACHCOL", StringComparison.OrdinalIgnoreCase)
           || string.Equals(code, "ACHCOLOMBIA", StringComparison.OrdinalIgnoreCase);

    private static bool IsCenit(string? code)
        => string.Equals(code, "CENIT", StringComparison.OrdinalIgnoreCase);
}
