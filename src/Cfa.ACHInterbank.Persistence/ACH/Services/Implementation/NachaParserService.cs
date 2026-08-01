using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Helpers.Hash;
using Cfa.ACHInterbank.Application.Helpers.ACH;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Helpers;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.ACH.Services.Implementation.ExternalFileNames;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class NachaParserService : INachaParserService
{
    // Frontera explícita:
    // - Regulatory: causal formal Dxx / rechazo regulatorio gobernado por catálogo.
    // - Technical: integridad estructural NACHA (longitud, offsets, campos reservados, etc.).
    private enum ValidationBoundary
    {
        Regulatory,
        Technical
    }

    private readonly AchDbContext _context;
    private readonly ILogger<NachaParserService> _logger;
    private readonly IAchStateTransitionService _stateTransitionService;
    private readonly IAchRegulatoryCatalogService? _catalogService;
    private readonly IConfiguration? _configuration;
    private HashSet<string>? _configuredTransactionCodes;
    private Dictionary<string, AchFileRejectionCode> _rejectionCatalog = new(StringComparer.OrdinalIgnoreCase);

    public NachaParserService(
        AchDbContext context,
        ILogger<NachaParserService> logger,
        IAchStateTransitionService stateTransitionService,
        IAchRegulatoryCatalogService? catalogService = null,
        IConfiguration? configuration = null)
    {
        _context = context;
        _logger = logger;
        _stateTransitionService = stateTransitionService;
        _catalogService = catalogService;
        _configuration = configuration;
    }

    public async Task<IReadOnlyList<NachaValidationFailure>> ParseAndSaveAsync(Stream nachaStream, string FileName, CancellationToken ct = default)
    {
        var result = await ParseAndSaveDetailedAsync(nachaStream, FileName, null, ct);
        return result.Failures;
    }

    public async Task<NachaParseResult> ParseAndSaveDetailedAsync(Stream nachaStream, string fileName, NachaParseRequest? request = null, CancellationToken ct = default)
    {
        var failures = new List<NachaValidationFailure>();
        int totalBatches = 0;
        int totalEntries = 0;
        int totalAddendas = 0;
        string? parsedNachaId = null;
        var originalAutoDetectChangesEnabled = _context.ChangeTracker.AutoDetectChangesEnabled;

        try
        {
            _context.ChangeTracker.AutoDetectChangesEnabled = false;
            _rejectionCatalog = await _context.AchFileRejectionCodes
                .AsNoTracking()
                .Where(x => x.IsActive)
                .ToDictionaryAsync(x => x.Code, StringComparer.OrdinalIgnoreCase, ct);

            var lines = await ReadPhysicalRecordsAsync(nachaStream, ct);

            var clearingHouseOrigins = await _context.ClearingHouses
                .AsNoTracking()
                .Select(ch => new { ch.Id, ch.OriginCode })
                .ToListAsync(ct);
            var originGroups = clearingHouseOrigins
                .Where(ch => !string.IsNullOrWhiteSpace(ch.OriginCode))
                .GroupBy(ch => ch.OriginCode.Trim(), StringComparer.OrdinalIgnoreCase)
                .ToList();
            var clearingHouseMap = originGroups
                .Where(group => group.Count() == 1)
                .ToDictionary(group => group.Key, group => group.Single().Id, StringComparer.OrdinalIgnoreCase);
            var ambiguousOriginCount = originGroups.Count(group => group.Count() > 1);
            if (ambiguousOriginCount > 0)
            {
                _logger.LogWarning(
                    "Se excluyeron {AmbiguousOriginCount} códigos OriginCode ambiguos del catálogo de cámaras durante el parseo NACHA-M.",
                    ambiguousOriginCount);
            }

            List<NachaHeader> headers = new();
            NachaHeader? currentHeader = null;
            BatchHeader? currentBatch = null;
            EntryDetail? lastEntry = null;
            var lastEntryAwaitingAddenda = false;
            var lastEntryAccepted = false;
            var entryDetails = new List<EntryDetail>();
            var addendaRecords = new List<AddendaRecord>();
            var batchControls = new List<BatchControl>();
            var fileControls = new List<FileControl>();
            var lastConsecutiveByBatch = new Dictionary<int, int>();
            var seenSequenceNumbers = new HashSet<string>(StringComparer.Ordinal);
            BatchRuntimeMetrics? currentBatchMetrics = null;
            var fileMetrics = new FileRuntimeMetrics();
            var fileControlLineIndex = -1;
            var fileControlEncountered = false;

            for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
            {
                var line = lines[lineIndex];
                var recordType = line[0];
                switch (recordType)
                {
                    case '1':
                        currentHeader = ParseFileHeaderLinq([line], clearingHouseMap, fileName, request).FirstOrDefault();
                        if (currentHeader is null)
                        {
                            break;
                        }
                        currentHeader.Batches = new List<BatchHeader>();
                        currentHeader.EntryDetails = new List<EntryDetail>();
                        currentHeader.AddendaRecords = new List<AddendaRecord>();
                        currentHeader.BatchControls = new List<BatchControl>();
                        currentHeader.FileControls = new List<FileControl>();

                        bool NachaHeadersExists = await _context.NachaHeaders
                            .AnyAsync(p => p.FileCreationDate == currentHeader.FileCreationDate
                                        && p.FileCreationTime == currentHeader.FileCreationTime
                                        && p.FileIdModifier == currentHeader.FileIdModifier
                                        && p.ImmediateOrigin == currentHeader.ImmediateOrigin, ct);

                        var isAuthorizedReplay = request?.IncomingNachaFileIngestionId is Guid replayIngestionId
                                                 && await _context.IncomingNachaFileIngestions
                                                     .AsNoTracking()
                                                     .AnyAsync(x => x.Id == replayIngestionId && x.IsReprocess, ct);
                        if (NachaHeadersExists && !isAuthorizedReplay)
                        {
                            ThrowRegulatory("D01", "El Archivo NACHA ya existe!");
                        }

                        if (NachaHeadersExists && isAuthorizedReplay)
                        {
                            currentHeader.NachaID = BuildReplayNachaId(
                                currentHeader.NachaID,
                                request!.IncomingNachaFileIngestionId!.Value);
                        }

                        parsedNachaId = currentHeader.NachaID;

                        headers.Add(currentHeader);
                        break;
                    case '5':
                        if (lastEntryAwaitingAddenda)
                        {
                            ThrowTechnical("ACHCOL-T6-T7-ORDER: una entrada T6 declaró adenda y no fue seguida por su T7.");
                        }

                        lastEntry = null;
                        lastEntryAccepted = false;
                        currentBatch = ParseBatchHeaderLinq([line]).FirstOrDefault();
                        if (currentBatch is not null)
                        {
                            currentBatchMetrics = new BatchRuntimeMetrics();
                            fileMetrics.RegisterBatch();
                            currentBatch.NachaID = currentHeader?.NachaID;
                            currentHeader?.Batches?.Add(currentBatch);
                            totalBatches++;
                        }
                        break;
                    case '6':
                        if (lastEntryAwaitingAddenda)
                        {
                            ThrowTechnical("ACHCOL-T6-T7-ORDER: una entrada T6 declaró adenda y no fue seguida inmediatamente por su T7.");
                        }

                        var entry = ParseEntryDetailLinq([line]).FirstOrDefault();
                        if (entry is null)
                        {
                            break;
                        }

                        entry.NachaID = currentHeader?.NachaID;
                        entry.BatchNumber = currentBatch?.BatchNumber
                            ?? throw new InvalidOperationException("Registro tipo 6 recibido sin BatchHeader tipo 5 asociado.");
                        UpdateBatchMetricsForEntry(currentBatchMetrics, entry);
                        fileMetrics.RegisterEntry(entry, CreditCodes, DebitCodes);

                        await ValidateEntrySequencePolicyAsync(entry, currentBatch, currentHeader, lastConsecutiveByBatch, seenSequenceNumbers, ct);

                        var (isValid, failureReason) = await ValidateEntryAsync(entry, currentBatch, failures, ct);
                        lastEntry = entry;
                        lastEntryAccepted = isValid;
                        lastEntryAwaitingAddenda = string.Equals(entry.AddendumIndicator, "1", StringComparison.Ordinal);
                        if (isValid)
                        {
                            entryDetails.Add(entry);
                            totalEntries++;
                        }

                        break;
                    case '7':
                        if (lastEntry is null || !lastEntryAwaitingAddenda)
                        {
                            ThrowTechnical("ACHCOL-T6-T7-ORDER: se recibió T7 sin un T6 inmediatamente asociado que declare adenda.");
                        }

                        var addenda = ParseAddendaLinq([line], lastEntry).FirstOrDefault();
                        if (addenda is not null)
                        {
                            addenda.NachaID = currentHeader?.NachaID;
                            currentBatchMetrics?.RegisterAddenda();
                            fileMetrics.RegisterAddenda();
                            if (lastEntryAccepted)
                            {
                                addenda.EntryDetailSequenceNumber ??= GetEntrySequenceSuffix(lastEntry.SequenceNumber);
                                addendaRecords.Add(addenda);
                                totalAddendas++;
                            }

                            lastEntryAwaitingAddenda = false;
                        }
                        break;
                    case '8':
                        if (lastEntryAwaitingAddenda)
                        {
                            ThrowTechnical("ACHCOL-T6-T7-ORDER: el lote cerró sin el T7 declarado por la última entrada T6.");
                        }

                        var batchControl = ParseBatchControlLinq([line]).FirstOrDefault();
                        if (batchControl is not null)
                        {
                            ValidateCurrentBatchControl(currentBatch, currentBatchMetrics, batchControl);
                            batchControls.Add(batchControl);
                            currentBatchMetrics = null;
                            lastEntry = null;
                            lastEntryAccepted = false;
                        }
                        break;
                    case '9':
                        if (lastEntryAwaitingAddenda)
                        {
                            ThrowTechnical("ACHCOL-T6-T7-ORDER: el archivo cerró sin el T7 declarado por la última entrada T6.");
                        }

                        if (!fileControlEncountered)
                        {
                            var fileControl = ParseFileControlLinq([line]).FirstOrDefault();
                            if (fileControl is not null)
                            {
                                fileControls.Add(fileControl);
                                fileControlEncountered = true;
                                fileControlLineIndex = lineIndex;
                            }
                        }
                        else if (!IsPaddingRecord(line))
                        {
                            ThrowRegulatory("D02", "Los registros de relleno después del Registro Tipo 9 deben contener únicamente el carácter '9' en sus 106 posiciones.");
                        }
                        break;
                }
            }

            var validEntries = EnforceAddendaRequirements(entryDetails, addendaRecords, failures);
            ValidateBatchSequenceAndControlConsistency(headers.SelectMany(h => h.Batches ?? []), batchControls);
            ValidateFileControlConsistency(headers.SelectMany(h => h.Batches ?? []), batchControls, fileControls, fileMetrics, lines, fileControlLineIndex);

            if (currentHeader is not null)
            {
                currentHeader.EntryDetails = validEntries;
                currentHeader.AddendaRecords = addendaRecords
                    .Where(addenda => validEntries.Any(entry => IsAddendaForEntry(entry, addenda)))
                    .ToList();
                currentHeader.BatchControls = batchControls;
                currentHeader.FileControls = fileControls;
            }

            _context.NachaHeaders.AddRange(headers);

            await _context.SaveChangesAsync(ct);
            var validAddendas = currentHeader?.AddendaRecords?.ToList() ?? [];
            await ApplyReturnStateTransitionsAsync(validEntries, validAddendas, failures, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "NACHA_PARSE_FAILED ErrorType={ErrorType} Incident={Incident}",
                ex.GetType().Name,
                ComputeSafeIncident(ex.GetType().Name));
            throw;
        }
        finally
        {
            _context.ChangeTracker.AutoDetectChangesEnabled = originalAutoDetectChangesEnabled;
        }

        return new NachaParseResult
        {
            Failures = failures,
            TotalBatches = totalBatches,
            TotalEntries = totalEntries,
            TotalAddendas = totalAddendas,
            WarningCount = failures.Count,
            ErrorCount = failures.Count,
            NachaId = parsedNachaId
        };
    }

    private async Task ValidateEntrySequencePolicyAsync(
        EntryDetail entry,
        BatchHeader? batch,
        NachaHeader? header,
        Dictionary<int, int> lastConsecutiveByBatch,
        HashSet<string> seenSequenceNumbers,
        CancellationToken ct)
    {
        if (batch is null)
        {
            ThrowTechnical("Error Fatal ID 7: no se encontró lote (registro tipo 5) para validar el Número de Secuencia del registro tipo 6.");
        }

        var rawSequence = (entry.SequenceNumber ?? string.Empty).Trim();
        if (rawSequence.Length != 15 || rawSequence.Any(c => !char.IsDigit(c)))
        {
            ThrowTechnical("Error Fatal ID 7: el Número de Secuencia del registro tipo 6 (posiciones 88-102) debe contener exactamente 15 dígitos numéricos.");
        }

        if (!seenSequenceNumbers.Add(rawSequence))
        {
            ThrowRegulatory("D04", "Número de secuencia duplicado en el archivo.");
        }

        var originSegment = rawSequence[..8];
        var consecutiveSegment = rawSequence[8..];

        var batchOrigin = (batch.OriginParticipantEntityCode ?? string.Empty).Trim();
        if (batchOrigin.Length != 8 || batchOrigin.Any(c => !char.IsDigit(c)))
        {
            throw new InvalidOperationException("Error Fatal ID 7: el código de entidad originadora del registro tipo 5 debe ser numérico de 8 dígitos para validar secuencia.");
        }

        if (!string.Equals(originSegment, batchOrigin, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Error Fatal ID 7: el segmento de entidad del Número de Secuencia no coincide con la entidad originadora del lote.");
        }

        var consecutiveValue = int.Parse(consecutiveSegment);
        if (consecutiveValue < 1)
        {
            throw new InvalidOperationException("Error Fatal ID 7: el segmento consecutivo del Número de Secuencia debe iniciar en 0000001 o superior.");
        }

        if (consecutiveValue > 6_999_999)
        {
            throw new InvalidOperationException("Error Fatal ID 7: el segmento consecutivo del Número de Secuencia excede 6999999. El rango 7000001-9999999 está reservado para PSE.");
        }

        if (lastConsecutiveByBatch.TryGetValue(batch.BatchNumber, out var previousConsecutive) && consecutiveValue <= previousConsecutive)
        {
            ThrowRegulatory("D04", "La secuencia del lote no es estrictamente ascendente.");
        }

        lastConsecutiveByBatch[batch.BatchNumber] = consecutiveValue;

        var processingDate = ParseNachaProcessingDate(header?.FileCreationDate)
            ?? throw new InvalidOperationException(
                "ACHCOL-T1-FILE-CREATION-DATE: no existe una fecha operacional válida para comprobar duplicidad de secuencia.");
        var existsInPreviousCycle = await _context.AchTransactions
            .AsNoTracking()
            .AnyAsync(t => t.EffectiveEntryDate.Date == processingDate
                           && t.TraceNumber == rawSequence, ct);

        if (existsInPreviousCycle)
        {
            ThrowRegulatory("D01", "Número de secuencia duplicado en el ámbito de fecha operacional.");
        }
    }

    private void ValidateBatchSequenceAndControlConsistency(IEnumerable<BatchHeader> batchHeaders, IEnumerable<BatchControl> batchControls)
    {
        var headers = batchHeaders.ToList();
        var controls = batchControls.ToList();

        if (headers.Count == 0)
        {
            return;
        }

        if (headers.Count != controls.Count)
        {
            ThrowRegulatory("D04", "La cantidad de registros tipo 5 no coincide con los registros tipo 8 del archivo.");
        }

        for (int index = 0; index < headers.Count; index++)
        {
            var expectedBatchNumber = index + 1;
            var header = headers[index];
            var control = controls[index];

            if (header.BatchNumber != expectedBatchNumber)
            {
                throw new InvalidOperationException("Error Fatal ID 5: el Número de Lote del registro tipo 5 debe iniciar en 0000001 y ser secuencial ascendente.");
            }

            if (!int.TryParse(control.BatchNumber?.Trim(), out var controlBatchNumber))
            {
                ThrowRegulatory("D04", "El Número de Lote del registro tipo 8 debe ser numérico en posiciones 100-106.");
            }

            if (controlBatchNumber != expectedBatchNumber || controlBatchNumber != header.BatchNumber)
            {
                ThrowRegulatory("D04", "Inconsistencia en Número de Lote entre registros tipo 5 y tipo 8.");
            }
        }
    }

    private void ValidateCurrentBatchControl(BatchHeader? header, BatchRuntimeMetrics? metrics, BatchControl control)
    {
        if (header is null || metrics is null)
        {
            ThrowRegulatory("D06", "Se recibió un registro tipo 8 sin un registro tipo 5 asociado.");
        }

        var serviceClassCode = (control.BatchTranClassCode ?? string.Empty).Trim();
        if (!string.Equals(serviceClassCode, (header.ServiceClassCode ?? string.Empty).Trim(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Error Fatal 8: el Código Clase de Transacciones del registro tipo 8 no coincide con el del registro tipo 5.");
        }

        if ((control.EntryAddendaCount ?? 0) != metrics.EntryAddendaCount)
        {
            ThrowRegulatory("D04", $"El conteo físico de registros tipo 6 y 7 del lote ({metrics.EntryAddendaCount}) no coincide con el valor reportado en el registro tipo 8 ({control.EntryAddendaCount ?? 0}).");
        }

        if ((control.EntryHash ?? 0) != metrics.EntryHash)
        {
            ThrowRegulatory("D05", "El Total de Control calculado del lote no coincide con el registro tipo 8.");
        }

        if (control.TotalDebitAmount != metrics.TotalDebitAmount)
        {
            throw new InvalidOperationException("Error Fatal 8: el total de débitos calculado del lote no coincide con el registro tipo 8.");
        }

        if (control.TotalCreditAmount != metrics.TotalCreditAmount)
        {
            throw new InvalidOperationException("Error Fatal 8: el total de créditos calculado del lote no coincide con el registro tipo 8.");
        }

        if (!string.Equals((control.IdUserOrig ?? string.Empty).Trim(), (header.CompanyId ?? string.Empty).Trim(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Error Fatal 8: la Identificación del Usuario Originador del registro tipo 8 no coincide con el registro tipo 5.");
        }

        if (!string.IsNullOrEmpty(control.Reserved) && control.Reserved.Any(c => c != ' '))
        {
            throw new InvalidOperationException("Error Fatal 87: el campo reservado del registro tipo 8 (posiciones 86-91) debe contener únicamente espacios en blanco.");
        }

        if (!string.Equals((control.IdOrigEntity ?? string.Empty).Trim(), (header.OriginParticipantEntityCode ?? string.Empty).Trim(), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Error Fatal 8: la Identificación de la Entidad Participante Originadora del registro tipo 8 no coincide con el registro tipo 5.");
        }
    }

    private void ValidateFileControlConsistency(
        IEnumerable<BatchHeader> batchHeaders,
        IEnumerable<BatchControl> batchControls,
        IEnumerable<FileControl> fileControls,
        FileRuntimeMetrics fileMetrics,
        IReadOnlyList<string> lines,
        int fileControlLineIndex)
    {
        var headers = batchHeaders.ToList();
        if (headers.Count == 0)
        {
            return;
        }

        var controls = fileControls.ToList();
        if (controls.Count != 1)
        {
            ThrowRegulatory("D06", "El archivo debe contener exactamente un Registro Tipo 9 de control de archivo.");
        }

        if (fileControlLineIndex < 0)
        {
            ThrowRegulatory("D06", "No se pudo ubicar el Registro Tipo 9 dentro del archivo.");
        }

        var control = controls[0];
        var batchControlList = batchControls.ToList();
        var expectedBatchCount = headers.Count;
        if (fileMetrics.BatchCount != expectedBatchCount)
        {
            ThrowRegulatory("D04", $"La cantidad de lotes calculada durante el recorrido del archivo ({fileMetrics.BatchCount}) no coincide con los registros tipo 5 ({expectedBatchCount}).");
        }

        if (control.BatchCount != expectedBatchCount)
        {
            ThrowRegulatory("D04", $"La cantidad de lotes del Registro Tipo 9 ({control.BatchCount}) no coincide con la cantidad de registros tipo 5 ({expectedBatchCount}).");
        }

        if (lines.Count % 10 != 0)
        {
            ThrowRegulatory("D02", $"El archivo debe ocupar un número entero de bloques de 10 registros y se recibieron {lines.Count} registros.");
        }

        var expectedBlockCount = lines.Count / 10;
        if (control.BlockCount != expectedBlockCount)
        {
            ThrowRegulatory("D04", $"El número de bloques físicos del Registro Tipo 9 ({control.BlockCount}) no coincide con los bloques del archivo ({expectedBlockCount}).");
        }

        if (control.EntryAddendaCount != fileMetrics.EntryAddendaCount)
        {
            ThrowRegulatory("D04", $"El conteo total de registros tipo 6 y 7 del archivo ({fileMetrics.EntryAddendaCount}) no coincide con el Registro Tipo 9 ({control.EntryAddendaCount}).");
        }

        if (control.EntryHash != fileMetrics.EntryHash)
        {
            ThrowRegulatory("D05", "El Hash Total calculado del archivo no coincide con el Registro Tipo 9.");
        }

        if (control.TotalDebitAmount != fileMetrics.TotalDebitAmount)
        {
            throw new InvalidOperationException("Error Fatal 62: el total de débitos calculado del archivo no coincide con el Registro Tipo 9.");
        }

        if (control.TotalCreditAmount != fileMetrics.TotalCreditAmount)
        {
            throw new InvalidOperationException("Error Fatal 63: el total de créditos calculado del archivo no coincide con el Registro Tipo 9.");
        }

        if (!string.IsNullOrEmpty(control.Reserved) && control.Reserved.Any(c => c != ' '))
        {
            throw new InvalidOperationException("Error Fatal 9: el campo reservado del Registro Tipo 9 (posiciones 68-106) debe contener únicamente espacios en blanco.");
        }

        for (int index = fileControlLineIndex + 1; index < lines.Count; index++)
        {
            if (!IsPaddingRecord(lines[index]))
            {
                ThrowRegulatory("D02", "Los registros de relleno posteriores al Registro Tipo 9 deben contener únicamente el carácter '9' en sus 106 posiciones.");
            }
        }

        var aggregatedBatchCount = batchControlList.Count;
        if (aggregatedBatchCount != control.BatchCount)
        {
            ThrowRegulatory("D04", $"La cantidad de registros tipo 8 ({aggregatedBatchCount}) no coincide con la cantidad de lotes reportada en el Registro Tipo 9 ({control.BatchCount}).");
        }

        var aggregatedEntryAddendaCount = batchControlList.Sum(batchControl => batchControl.EntryAddendaCount ?? 0);
        if (aggregatedEntryAddendaCount != control.EntryAddendaCount)
        {
            ThrowRegulatory("D04", $"La sumatoria de conteos de los registros tipo 8 ({aggregatedEntryAddendaCount}) no coincide con el Registro Tipo 9 ({control.EntryAddendaCount}).");
        }

        const long maxHash = 10_000_000_000L;
        var aggregatedHash = batchControlList.Aggregate(0L, (current, batchControl) => (current + (batchControl.EntryHash ?? 0)) % maxHash);
        if (aggregatedHash != control.EntryHash)
        {
            ThrowRegulatory("D05", "La sumatoria de hashes de los registros tipo 8 no coincide con el Registro Tipo 9.");
        }

        var aggregatedDebit = batchControlList.Sum(batchControl => batchControl.TotalDebitAmount);
        if (aggregatedDebit != control.TotalDebitAmount)
        {
            throw new InvalidOperationException("Error Fatal 62: la sumatoria de débitos de los registros tipo 8 no coincide con el Registro Tipo 9.");
        }

        var aggregatedCredit = batchControlList.Sum(batchControl => batchControl.TotalCreditAmount);
        if (aggregatedCredit != control.TotalCreditAmount)
        {
            throw new InvalidOperationException("Error Fatal 63: la sumatoria de créditos de los registros tipo 8 no coincide con el Registro Tipo 9.");
        }
    }

    private static bool IsPaddingRecord(string line)
    {
        return line.Length == 106 && line.All(character => character == '9');
    }

    private List<NachaHeader> ParseFileHeaderLinq(List<string> line, Dictionary<string, int> clearingHouseMap, string FileName, NachaParseRequest? request)
    {
        int? cycleNumber = CenitOfficialFileNameParser.ExtractCycleNumberFromFileName(FileName);

        return line.Select(a =>
        {
            string immediateOrigin = AchColOfficialNachaLayout.Read(a, "1", "IMMEDIATEORIGIN").Trim();
            int? clearingHouseId = request?.ResolvedClearingHouseId ?? (clearingHouseMap.TryGetValue(immediateOrigin, out var chId) ? chId : null);
            string fileCreationDate = AchColOfficialNachaLayout.Read(a, "1", "FILECREATIONDATE");
            DateTime? processingDate = ParseNachaProcessingDate(fileCreationDate);

            IQueryable<AchCycle> cycleQuery = _context.AchCycles
                .Where(c => c.ClearingHouseId == clearingHouseId);

            if (processingDate.HasValue)
            {
                cycleQuery = cycleQuery.Where(c => c.ProcessingDate == processingDate.Value.Date);
            }

            if (cycleNumber.HasValue)
            {
                cycleQuery = cycleQuery.Where(c => c.CycleName.Contains(cycleNumber.Value.ToString(CultureInfo.InvariantCulture)));
            }

            string? achCycleId = request?.ResolvedAchCycleId;
            if (achCycleId is null)
            {
                if (string.Equals(_context.Database.ProviderName, "Microsoft.EntityFrameworkCore.Sqlite", StringComparison.OrdinalIgnoreCase))
                {
                    achCycleId = cycleQuery
                        .AsNoTracking()
                        .Select(c => new { c.Id, c.ProcessingDate, c.CutoffTime })
                        .ToList()
                        .OrderByDescending(c => c.ProcessingDate)
                        .ThenByDescending(c => c.CutoffTime)
                        .Select(c => c.Id)
                        .FirstOrDefault();
                }
                else
                {
                    achCycleId = cycleQuery
                        .OrderByDescending(c => c.ProcessingDate)
                        .ThenByDescending(c => c.CutoffTime)
                        .Select(c => c.Id)
                        .FirstOrDefault();
                }
            }

            return new NachaHeader
            {
                NachaID = HashHelper.GenerateHashSha1(
                    $"{AchColOfficialNachaLayout.Read(a, "1", "IMMEDIATEDESTINATION").Trim()}{immediateOrigin}{fileCreationDate.Trim()}{AchColOfficialNachaLayout.Read(a, "1", "FILECREATIONTIME").Trim()}"),
                PriorityCode = AchColOfficialNachaLayout.Read(a, "1", "PRIORITYCODE"),
                ImmediateDestination = AchColOfficialNachaLayout.Read(a, "1", "IMMEDIATEDESTINATION").Trim(),
                ImmediateOrigin = immediateOrigin,
                FileCreationDate = fileCreationDate,
                FileCreationTime = AchColOfficialNachaLayout.Read(a, "1", "FILECREATIONTIME"),
                FileIdModifier = AchColOfficialNachaLayout.Read(a, "1", "FILEIDMODIFIER"),
                RecordSize = AchColOfficialNachaLayout.Read(a, "1", "RECORDSIZE"),
                BlockingFactor = AchColOfficialNachaLayout.Read(a, "1", "BLOCKINGFACTOR"),
                FormatCode = AchColOfficialNachaLayout.Read(a, "1", "FORMATCODE"),
                ImmediateDestinationName = AchColOfficialNachaLayout.Read(a, "1", "IMMEDIATEDESTINATIONNAME").Trim(),
                ImmediateOriginName = AchColOfficialNachaLayout.Read(a, "1", "IMMEDIATEORIGINNAME").Trim(),
                ReferenceCode = AchColOfficialNachaLayout.Read(a, "1", "REFERENCECODE").Trim(),
                ClearingHouseId = clearingHouseId,
                CycleNumber = cycleNumber ?? 0,
                AchCycleId = achCycleId,
                IncomingNachaFileIngestionId = request?.IncomingNachaFileIngestionId
            };
        }).ToList();
    }

    private static DateTime? ParseNachaProcessingDate(string? fileCreationDate)
    {
        if (string.IsNullOrWhiteSpace(fileCreationDate))
        {
            return null;
        }

        var value = fileCreationDate.Trim();
        if (DateTime.TryParseExact(value, "yyyyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var yyyyMMdd))
        {
            return yyyyMMdd.Date;
        }

        if (DateTime.TryParseExact(value, "yyMMdd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var yyMMdd))
        {
            return yyMMdd.Date;
        }

        return null;
    }

    private List<BatchHeader> ParseBatchHeaderLinq(List<string> line)
    {
        return line.Select(a =>
        {
            if (a.Length < BatchHeaderType5JulianDateValidator.RecordLength || a[0] != '5')
            {
                throw new InvalidOperationException("El Registro Tipo 5 debe iniciar con '5' y tener longitud fija de 106 caracteres.");
            }

            var rawCompensationDate = AchColOfficialNachaLayout.Read(a, "5", "SETTLEMENTDATE");
            var compensationValidation = BatchHeaderType5JulianDateValidator.ValidateAndFormat(rawCompensationDate);
            if (!compensationValidation.IsValid)
            {
                throw new InvalidOperationException(compensationValidation.ErrorMessage ?? "Error Fatal 65: la Fecha de Compensación Juliana contiene caracteres no numéricos.");
            }

            var rawBatchNumber = AchColOfficialNachaLayout.Read(a, "5", "BATCHNUMBER");
            if (!rawBatchNumber.All(char.IsDigit))
            {
                throw new InvalidOperationException("Error Fatal ID 5: el Número de Lote del registro tipo 5 (posiciones 92-98) debe ser numérico de 7 dígitos.");
            }

            return new BatchHeader
            {
                ServiceClassCode = AchColOfficialNachaLayout.Read(a, "5", "SERVICECLASSCODE"),
                CompanyName = AchColOfficialNachaLayout.Read(a, "5", "COMPANYNAME").Trim(),
                DiscretionaryData = AchColOfficialNachaLayout.Read(a, "5", "COMPANYDISCRETIONARYDATA").Trim(),
                CompanyId = AchColOfficialNachaLayout.Read(a, "5", "COMPANYIDENTIFICATION").Trim(),
                StandardEntryClassCode = AchColOfficialNachaLayout.Read(a, "5", "STANDARDENTRYCLASSCODE").Trim(),
                CompanyEntryDescription = AchColOfficialNachaLayout.Read(a, "5", "COMPANYENTRYDESCRIPTION").Trim(),
                DescriptiveDate = AchColOfficialNachaLayout.Read(a, "5", "COMPANYDESCRIPTIVEDATE").Trim(),
                EffectiveEntryDate = AchColOfficialNachaLayout.Read(a, "5", "EFFECTIVEENTRYDATE").Trim(),
                CompensationDate = compensationValidation.FormattedValue.Trim(),
                OriginUserStatusCode = AchColOfficialNachaLayout.Read(a, "5", "ORIGINATORSTATUSCODE").Trim(),
                OriginParticipantEntityCode = AchColOfficialNachaLayout.Read(a, "5", "ORIGINATINGDFI").Trim(),
                BatchNumber = int.Parse(rawBatchNumber)
            };
        }).ToList();
    }

    private List<EntryDetail> ParseEntryDetailLinq(List<string> line)
    {
        return line.Select(a => new EntryDetail
        {
            TransactionCode = AchColOfficialNachaLayout.Read(a, "6", "TRANSACTIONCODE").Trim(),
            ReceivingParticipantEntityCode = AchColOfficialNachaLayout.Read(a, "6", "RECEIVINGDFI").Trim(),
            CheckDigit = AchColOfficialNachaLayout.Read(a, "6", "CHECKDIGIT").Trim(),
            AccountNumber = AchColOfficialNachaLayout.Read(a, "6", "DFIACCOUNTNUMBER").TrimEnd(),
            Amount = Convert.ToDecimal(AchColOfficialNachaLayout.Read(a, "6", "AMOUNT"), CultureInfo.InvariantCulture) / 100,
            RecipIdNumber = AchColOfficialNachaLayout.Read(a, "6", "INDIVIDUALIDENTIFICATION").TrimEnd(),
            RecipUserName = AchColOfficialNachaLayout.Read(a, "6", "INDIVIDUALNAME"),
            DiscreData = AchColOfficialNachaLayout.Read(a, "6", "DISCRETIONARYDATA"),
            AddendumIndicator = AchColOfficialNachaLayout.Read(a, "6", "ADDENDARECORDINDICATOR").Trim(),
            SequenceNumber = AchColOfficialNachaLayout.Read(a, "6", "TRACENUMBER").Trim()
        }).ToList();
    }

    private List<AddendaRecord> ParseAddendaLinq(List<string> line, EntryDetail? associatedEntry = null)
    {
        return line.Select(a =>
        {
            var addendaType = a.Substring(1, 2).Trim();
            if (addendaType == "99")
            {
                return new AddendaRecord
                {
                    CodeTypeAddendumRecord = addendaType,
                    BusinessType = "Return",
                    IdUserOrig = a.Substring(3, 5).Trim(),
                    InvoiceOrAccountNumber = a.Substring(8, 15).Trim(),
                    InfofromOriginator = a.Substring(81, 15).Trim(),
                    ReturnReasonCode = a.Substring(3, 5).Trim(),
                    OriginalTraceNumber = a.Substring(8, 15).Trim(),
                    NewTraceNumber = a.Substring(81, 15).Trim(),
                    EntryDetailSequenceNumber = a.Substring(99, 7).Trim()
                };
            }

            if (addendaType != "05")
            {
                ThrowTechnical("ACHCOL-T7-ADDENDA-TYPE: el tipo de adenda no está demostrado para el perfil ACHCOL oficial.");
            }

            var businessType = ParseBusinessTypeFromType05(associatedEntry?.TransactionCode);
            var variant = businessType == "Debit"
                ? AchColOfficialNachaLayout.Type7DebitVariant
                : AchColOfficialNachaLayout.Type7CreditVariant;

            return new AddendaRecord
            {
                CodeTypeAddendumRecord = AchColOfficialNachaLayout.Read(a, "7", "ADDENDATYPE", variant).Trim(),
                BusinessType = businessType,
                IdUserOrig = businessType == "Credit"
                    ? AchColOfficialNachaLayout.Read(a, "7", "ORIGINATORIDENTIFICATION", variant).Trim()
                    : null,
                CollectorId = businessType == "Debit"
                    ? AchColOfficialNachaLayout.Read(a, "7", "COLLECTORID", variant).Trim()
                    : null,
                ReceiverCustomerCode = businessType == "Debit"
                    ? AchColOfficialNachaLayout.Read(a, "7", "RECEIVERCUSTOMERCODE", variant).Trim()
                    : null,
                ServiceDescription = businessType == "Debit"
                    ? AchColOfficialNachaLayout.Read(a, "7", "SERVICEDESCRIPTION", variant).Trim()
                    : null,
                PurposeOfTransaction = businessType == "Credit"
                    ? AchColOfficialNachaLayout.Read(a, "7", "PURPOSE", variant).Trim()
                    : null,
                InvoiceOrAccountNumber = businessType == "Credit"
                    ? AchColOfficialNachaLayout.Read(a, "7", "REFERENCE", variant).Trim()
                    : null,
                AddendumSequence = AchColOfficialNachaLayout.Read(a, "7", "SEQUENCENUMBER", variant).Trim(),
                EntryDetailSequenceNumber = AchColOfficialNachaLayout.Read(a, "7", "TRACESUFFIX", variant).Trim()
            };
        }).ToList();
    }


    private List<BatchControl> ParseBatchControlLinq(List<string> line)
    {
        return line.Select(a => new BatchControl
        {
            BatchTranClassCode = AchColOfficialNachaLayout.Read(a, "8", "SERVICECLASSCODE"),
            EntryAddendaCount = int.Parse(AchColOfficialNachaLayout.Read(a, "8", "ENTRYADDENDACOUNT"), CultureInfo.InvariantCulture),
            EntryHash = long.Parse(AchColOfficialNachaLayout.Read(a, "8", "ENTRYHASH"), CultureInfo.InvariantCulture),
            TotalDebitAmount = Convert.ToDecimal(AchColOfficialNachaLayout.Read(a, "8", "TOTALDEBITAMOUNT"), CultureInfo.InvariantCulture) / 100,
            TotalCreditAmount = Convert.ToDecimal(AchColOfficialNachaLayout.Read(a, "8", "TOTALCREDITAMOUNT"), CultureInfo.InvariantCulture) / 100,
            IdUserOrig = AchColOfficialNachaLayout.Read(a, "8", "COMPANYIDENTIFICATION").Trim(),
            CodAutMessage = AchColOfficialNachaLayout.Read(a, "8", "MESSAGEAUTHENTICATIONCODE"),
            Reserved = AchColOfficialNachaLayout.Read(a, "8", "RESERVED"),
            IdOrigEntity = AchColOfficialNachaLayout.Read(a, "8", "ORIGINATINGDFI"),
            BatchNumber = AchColOfficialNachaLayout.Read(a, "8", "BATCHNUMBER"),
        }).ToList();
    }

    private void UpdateBatchMetricsForEntry(BatchRuntimeMetrics? metrics, EntryDetail entry)
    {
        if (metrics is null)
        {
            ThrowRegulatory("D06", "Se recibió un registro tipo 6 sin un registro tipo 5 asociado.");
        }

        metrics.RegisterEntry(entry, CreditCodes, DebitCodes);
    }

    private sealed class BatchRuntimeMetrics
    {
        private const long MaxHash = 10_000_000_000L;

        public int EntryAddendaCount { get; private set; }
        public long EntryHash { get; private set; }
        public decimal TotalDebitAmount { get; private set; }
        public decimal TotalCreditAmount { get; private set; }

        public void RegisterEntry(EntryDetail entry, IReadOnlySet<string> creditCodes, IReadOnlySet<string> debitCodes)
        {
            EntryAddendaCount++;

            var entityCode = new string((entry.ReceivingParticipantEntityCode ?? string.Empty).Where(char.IsDigit).ToArray());
            if (entityCode.Length > 0)
            {
                var hashDigits = entityCode.Length > 8 ? entityCode[..8] : entityCode;
                if (long.TryParse(hashDigits, out var hashValue))
                {
                    EntryHash = (EntryHash + hashValue) % MaxHash;
                }
            }

            var amount = entry.Amount ?? 0m;
            var transactionCode = (entry.TransactionCode ?? string.Empty).Trim();
            if (debitCodes.Contains(transactionCode))
            {
                TotalDebitAmount += amount;
            }
            else if (creditCodes.Contains(transactionCode))
            {
                TotalCreditAmount += amount;
            }
        }

        public void RegisterAddenda()
        {
            EntryAddendaCount++;
        }
    }

    private static string ParseBusinessTypeFromType05(string? transactionCode)
    {
        var normalized = (transactionCode ?? string.Empty).Trim();
        if (DebitCodes.Contains(normalized))
        {
            return "Debit";
        }

        if (CreditCodes.Contains(normalized))
        {
            return "Credit";
        }

        ThrowTechnical("ACHCOL-T7-VARIANT: no se puede seleccionar la variante T7 sin un código de transacción T6 demostrado.");
        return string.Empty;
    }

    private List<FileControl> ParseFileControlLinq(List<string> line)
    {
        return line.Take(1).Select(a => new FileControl
        {
            BatchCount = int.Parse(AchColOfficialNachaLayout.Read(a, "9", "BATCHCOUNT"), CultureInfo.InvariantCulture),
            BlockCount = int.Parse(AchColOfficialNachaLayout.Read(a, "9", "BLOCKCOUNT"), CultureInfo.InvariantCulture),
            EntryAddendaCount = int.Parse(AchColOfficialNachaLayout.Read(a, "9", "ENTRYADDENDACOUNT"), CultureInfo.InvariantCulture),
            EntryHash = long.Parse(AchColOfficialNachaLayout.Read(a, "9", "ENTRYHASH"), CultureInfo.InvariantCulture),
            TotalDebitAmount = Convert.ToDecimal(AchColOfficialNachaLayout.Read(a, "9", "TOTALDEBITAMOUNT"), CultureInfo.InvariantCulture) / 100,
            TotalCreditAmount = Convert.ToDecimal(AchColOfficialNachaLayout.Read(a, "9", "TOTALCREDITAMOUNT"), CultureInfo.InvariantCulture) / 100,
            Reserved = AchColOfficialNachaLayout.Read(a, "9", "RESERVED")
        }).ToList();
    }

    private sealed class FileRuntimeMetrics
    {
        private const long MaxHash = 10_000_000_000L;

        public int BatchCount { get; private set; }
        public int EntryAddendaCount { get; private set; }
        public long EntryHash { get; private set; }
        public decimal TotalDebitAmount { get; private set; }
        public decimal TotalCreditAmount { get; private set; }

        public void RegisterBatch()
        {
            BatchCount++;
        }

        public void RegisterEntry(EntryDetail entry, IReadOnlySet<string> creditCodes, IReadOnlySet<string> debitCodes)
        {
            EntryAddendaCount++;

            var entityCode = new string((entry.ReceivingParticipantEntityCode ?? string.Empty).Where(char.IsDigit).ToArray());
            if (entityCode.Length > 0)
            {
                var hashDigits = entityCode.Length > 8 ? entityCode[..8] : entityCode;
                if (long.TryParse(hashDigits, out var hashValue))
                {
                    EntryHash = (EntryHash + hashValue) % MaxHash;
                }
            }

            var amount = entry.Amount ?? 0m;
            var transactionCode = (entry.TransactionCode ?? string.Empty).Trim();
            if (debitCodes.Contains(transactionCode))
            {
                TotalDebitAmount += amount;
            }
            else if (creditCodes.Contains(transactionCode))
            {
                TotalCreditAmount += amount;
            }
        }

        public void RegisterAddenda()
        {
            EntryAddendaCount++;
        }
    }

    private static readonly HashSet<string> FallbackTransactionCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "21", "22", "23", "26", "27", "28",
        "31", "32", "33", "36", "37", "38",
        "42", "51", "52", "53", "55", "56", "57"
    };

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

    private async Task<(bool IsValid, string? FailureReason)> ValidateEntryAsync(
        EntryDetail entry,
        BatchHeader? batch,
        List<NachaValidationFailure> failures,
        CancellationToken ct)
    {
        var code = entry.TransactionCode ?? string.Empty;
        var configuredCodes = await GetConfiguredTransactionCodesAsync(ct);
        if (!configuredCodes.Contains(code))
        {
            const string reason = "Código de transacción inválido.";
            failures.Add(new NachaValidationFailure("6", batch?.BatchNumber.ToString(), entry.SequenceNumber, code, reason));
            return (false, reason);
        }

        var isCredit = CreditCodes.Contains(code);
        var isDebit = DebitCodes.Contains(code);
        var serviceClassCode = batch?.ServiceClassCode?.Trim();
        var isPseCcdCredit = isCredit && IsPseCcdBatch(batch);

        if (serviceClassCode == "220" && !isCredit)
        {
            const string reason = "Lote exclusivo de crédito (220) no permite débitos.";
            failures.Add(new NachaValidationFailure("6", batch?.BatchNumber.ToString(), entry.SequenceNumber, code, reason));
            return (false, reason);
        }

        if (serviceClassCode == "225" && !isDebit)
        {
            const string reason = "Lote exclusivo de débito (225) no permite créditos.";
            failures.Add(new NachaValidationFailure("6", batch?.BatchNumber.ToString(), entry.SequenceNumber, code, reason));
            return (false, reason);
        }

        if (PrenoteCodes.Contains(code) && entry.Amount.GetValueOrDefault() != 0m)
        {
            const string reason = "Prenotificación debe tener valor 0.";
            failures.Add(new NachaValidationFailure("6", batch?.BatchNumber.ToString(), entry.SequenceNumber, code, reason));
            return (false, reason);
        }

        if (!string.Equals(entry.AddendumIndicator, "1", StringComparison.OrdinalIgnoreCase))
        {
            const string reason = "El registro 7 es obligatorio para todas las transacciones.";
            failures.Add(new NachaValidationFailure("6", batch?.BatchNumber.ToString(), entry.SequenceNumber, code, reason));
            return (false, reason);
        }

        var checkDigitValidationReason = await ValidateType6CheckDigitAsync(entry, ct);
        if (checkDigitValidationReason is not null)
        {
            failures.Add(new NachaValidationFailure("6", batch?.BatchNumber.ToString(), entry.SequenceNumber, code, checkDigitValidationReason));
            return (false, checkDigitValidationReason);
        }

        var receiverNameValidationReason = ValidateType6ReceiverName(entry);
        if (receiverNameValidationReason is not null)
        {
            failures.Add(new NachaValidationFailure("6", batch?.BatchNumber.ToString(), entry.SequenceNumber, code, receiverNameValidationReason));
            return (false, receiverNameValidationReason);
        }

        var requiresIdentityValidation = isDebit || ShouldValidateCreditIdentity(entry.DiscreData);

        if (isPseCcdCredit)
        {
            var accountNumber = entry.AccountNumber ?? string.Empty;
            var accountExists = await _context.CustomerAccounts
                .AsNoTracking()
                .AnyAsync(a => a.AccountNumber == accountNumber, ct);

            if (!accountExists)
            {
                const string reason = "R03: Cuenta de destino no existe en la entidad receptora.";
                failures.Add(new NachaValidationFailure("6", batch?.BatchNumber.ToString(), entry.SequenceNumber, code, reason));
                return (false, reason);
            }

            // Excepción de negocio para CCD/PSE como banco receptor:
            // si la cuenta existe, no se aplican validaciones adicionales de límites.
            return (true, null);
        }

        if (requiresIdentityValidation)
        {
            if (string.IsNullOrWhiteSpace(entry.RecipIdNumber))
            {
                const string reason = "R17: La identificación no coincide con cuenta del usuario receptor.";
                failures.Add(new NachaValidationFailure("6", batch?.BatchNumber.ToString(), entry.SequenceNumber, code, reason));
                return (false, reason);
            }

            var accountNumber = entry.AccountNumber ?? string.Empty;
            var recipientId = entry.RecipIdNumber ?? string.Empty;
            var matches = await _context.Customers
                .AsNoTracking()
                .AnyAsync(c => c.DocumentNumber == recipientId && c.Accounts.Any(a => a.AccountNumber == accountNumber), ct);

            if (!matches)
            {
                if (IsLocalLiveProcTransaccionesPreparationEnabled()
                    && string.Equals(code, "32", StringComparison.Ordinal))
                {
                    return (true, "LOCAL_LIVE_RECIPIENT_PREPARATION_PENDING");
                }

                const string reason = "R17: La identificación no coincide con cuenta del usuario receptor.";
                failures.Add(new NachaValidationFailure("6", batch?.BatchNumber.ToString(), entry.SequenceNumber, code, reason));
                return (false, reason);
            }
        }

        return (true, null);
    }

    internal static async Task<List<string>> ReadPhysicalRecordsAsync(Stream nachaStream, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(nachaStream);

        using var buffer = new MemoryStream();
        await nachaStream.CopyToAsync(buffer, ct);
        var bytes = buffer.ToArray();

        if (bytes.Length == 0)
        {
            ThrowTechnical("ACHCOL-PHYSICAL-FILE-NOT-EMPTY: el archivo está vacío.");
        }

        if (bytes.AsSpan().StartsWith(Encoding.UTF8.Preamble)
            || bytes.AsSpan().StartsWith(new byte[] { 0xFF, 0xFE })
            || bytes.AsSpan().StartsWith(new byte[] { 0xFE, 0xFF }))
        {
            ThrowTechnical("ACHCOL-PHYSICAL-NO-BOM: el archivo contiene BOM.");
        }

        if (bytes.Any(value => value is (byte)'\r' or (byte)'\n'))
        {
            ThrowTechnical("ACHCOL-PHYSICAL-NO-LINE-ENDINGS: el archivo contiene CR/LF.");
        }

        if (bytes.Any(value => value > 0x7F))
        {
            ThrowTechnical("ACHCOL-PHYSICAL-ASCII-REPERTOIRE: el archivo contiene bytes fuera del repertorio permitido.");
        }

        if (bytes.Length % AchColOfficialNachaLayout.RecordLength != 0)
        {
            ThrowTechnical("ACHCOL-PHYSICAL-RECORD-LENGTH: existen bytes residuales; cada registro debe tener 106 bytes.");
        }

        var content = Encoding.ASCII.GetString(bytes);
        var lines = Enumerable.Range(0, bytes.Length / AchColOfficialNachaLayout.RecordLength)
            .Select(index => content.Substring(index * AchColOfficialNachaLayout.RecordLength, AchColOfficialNachaLayout.RecordLength))
            .ToList();

        if (lines[0][0] != '1')
        {
            ThrowTechnical("ACHCOL-T1-RECORD-TYPE: el primer registro debe ser tipo 1.");
        }

        var declaredLength = AchColOfficialNachaLayout.Read(lines[0], "1", "RECORDSIZE");
        if (!string.Equals(declaredLength, "106", StringComparison.Ordinal))
        {
            ThrowTechnical("ACHCOL-T1-RECORD-SIZE: el tamaño declarado debe ser 106.");
        }

        if (lines.Count % AchColOfficialNachaLayout.BlockingFactor != 0)
        {
            ThrowTechnical("ACHCOL-PHYSICAL-BLOCKING-FACTOR: el archivo no ocupa bloques exactos de 10 registros.");
        }

        return lines;
    }

    private bool IsLocalLiveProcTransaccionesPreparationEnabled()
        => string.Equals(_configuration?["RUN_LOCAL_SOAP_PROC_TRANSACCIONES_E2E"], "true", StringComparison.OrdinalIgnoreCase)
           && string.Equals(_configuration?["ALLOW_LOCAL_MONETARY_SOAP_E2E"], "true", StringComparison.OrdinalIgnoreCase)
           && string.Equals(_configuration?["ProcTransacciones:Mode"], "Live", StringComparison.OrdinalIgnoreCase);

    private static string BuildReplayNachaId(string? originalNachaId, Guid ingestionId)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{originalNachaId}|{ingestionId:N}"))).ToLowerInvariant()[..40];

    private static string? ValidateType6ReceiverName(EntryDetail entry)
    {
        return NachaReceiverNameHelper.ValidateType6RawField(entry.RecipUserName);
    }

    private async Task<string?> ValidateType6CheckDigitAsync(EntryDetail entry, CancellationToken ct)
    {
        var receivingCode = (entry.ReceivingParticipantEntityCode ?? string.Empty).Trim();
        if (receivingCode.Length != 8 || receivingCode.Any(c => !char.IsDigit(c)))
        {
            return "Error Fatal ID 35: el Código Entidad Participante Receptor (posiciones 4-11) debe contener 8 dígitos numéricos.";
        }

        var fileCheckDigit = (entry.CheckDigit ?? string.Empty).Trim();
        if (fileCheckDigit.Length != 1 || !char.IsDigit(fileCheckDigit[0]))
        {
            return "Error Fatal ID 35: el Dígito de Chequeo (posición 12) debe ser numérico de longitud 1.";
        }

        var expectedCheckDigit = DigitoChequeoHelper.CalcularDigitoChequeo(receivingCode);
        if (!string.Equals(fileCheckDigit, expectedCheckDigit, StringComparison.Ordinal))
        {
            return "Error Fatal ID 35: el Dígito de Chequeo (posición 12) no corresponde al Código Entidad Participante Receptor (posiciones 4-11).";
        }

        var institution = await _context.FinancialInstitutions
            .AsNoTracking()
            .FirstOrDefaultAsync(fi => (fi.RoutingNumber + fi.TransitCode) == receivingCode, ct);

        if (institution is not null)
        {
            var dbCheckDigit = (institution.CheckDigit ?? string.Empty).Trim();
            if (dbCheckDigit.Length != 1 || !char.IsDigit(dbCheckDigit[0]))
            {
                return "Error Fatal ID 35: el dígito de chequeo almacenado en FinancialInstitutions no es válido.";
            }

            if (!string.Equals(dbCheckDigit, expectedCheckDigit, StringComparison.Ordinal))
            {
                return "Error Fatal ID 35: inconsistencia en FinancialInstitutions.CheckDigit.";
            }
        }

        return null;
    }

    private static bool IsPseCcdBatch(BatchHeader? batch)
    {
        if (batch is null)
        {
            return false;
        }

        if (!string.Equals(batch.StandardEntryClassCode?.Trim(), "CCD", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var pseHints = new[]
        {
            batch.CompanyEntryDescription,
            batch.CompanyName,
            batch.DiscretionaryData,
            batch.CompanyId
        };

        return pseHints.Any(value =>
            !string.IsNullOrWhiteSpace(value) &&
            value.Contains("PSE", StringComparison.OrdinalIgnoreCase));
    }

    private static List<EntryDetail> EnforceAddendaRequirements(
        List<EntryDetail> entries,
        List<AddendaRecord> addendaRecords,
        List<NachaValidationFailure> failures)
    {
        var validEntries = new List<EntryDetail>();
        foreach (var entry in entries)
        {
            var relatedAddendas = addendaRecords
                .Where(addenda => IsAddendaForEntry(entry, addenda))
                .ToList();

            if (!relatedAddendas.Any())
            {
                failures.Add(new NachaValidationFailure("6", null, entry.SequenceNumber, entry.TransactionCode,
                    "No se encontró registro 7 asociado al detalle."));
                continue;
            }

            if (ReturnCodes.Contains(entry.TransactionCode ?? string.Empty))
            {
                var hasReturnAddenda = relatedAddendas.Any(addenda =>
                    string.Equals(addenda.CodeTypeAddendumRecord?.Trim(), "99", StringComparison.OrdinalIgnoreCase));

                if (!hasReturnAddenda)
                {
                    failures.Add(new NachaValidationFailure("7", null, entry.SequenceNumber, entry.TransactionCode,
                        "Las devoluciones (21/26/31/36/51/56) deben incluir adenda tipo 99."));
                    continue;
                }

                var hasReturnReason = relatedAddendas.Any(addenda => HasReturnReason(addenda));
                if (!hasReturnReason)
                {
                    failures.Add(new NachaValidationFailure("7", null, entry.SequenceNumber, entry.TransactionCode,
                        "La adenda de devolución debe incluir causal (Rxx o DEV14)."));
                    continue;
                }

                var hasOriginalTraceReference = relatedAddendas.Any(addenda =>
                    !string.IsNullOrWhiteSpace(addenda.OriginalTraceNumber) &&
                    addenda.OriginalTraceNumber.Trim().Length == 15);

                if (!hasOriginalTraceReference)
                {
                    failures.Add(new NachaValidationFailure("7", null, entry.SequenceNumber, entry.TransactionCode,
                        "La devolución debe incluir referencia de secuencia original en la adenda."));
                    continue;
                }
            }

            validEntries.Add(entry);
        }

        return validEntries;
    }

    private static bool IsAddendaForEntry(EntryDetail entry, AddendaRecord addenda)
    {
        var entrySequence = GetEntrySequenceSuffix(entry.SequenceNumber);
        return !string.IsNullOrWhiteSpace(entrySequence) &&
               string.Equals(addenda.EntryDetailSequenceNumber, entrySequence, StringComparison.OrdinalIgnoreCase);
    }

    private static bool HasReturnReason(AddendaRecord addenda)
    {
        return !string.IsNullOrWhiteSpace(addenda.ReturnReasonCode)
               && Regex.IsMatch(addenda.ReturnReasonCode.Trim(), @"^R\d{2}$|^DEV14$", RegexOptions.IgnoreCase);
    }

    private static string? GetEntrySequenceSuffix(string? sequence)
    {
        if (string.IsNullOrWhiteSpace(sequence))
        {
            return null;
        }

        return sequence.Length <= 7 ? sequence : sequence[^7..];
    }


    private async Task ApplyReturnStateTransitionsAsync(
        IReadOnlyList<EntryDetail> validEntries,
        IReadOnlyList<AddendaRecord> validAddendas,
        IReadOnlyList<NachaValidationFailure> failures,
        CancellationToken ct)
    {
        await ApplyReturnedByEprTransitionsAsync(validEntries, validAddendas, ct);
        await ApplyReturnedByOperatorTransitionsAsync(failures, ct);
    }

    private async Task ApplyReturnedByEprTransitionsAsync(
        IReadOnlyList<EntryDetail> validEntries,
        IReadOnlyList<AddendaRecord> validAddendas,
        CancellationToken ct)
    {
        var processedTransactionIds = new HashSet<int>();

        foreach (var entry in validEntries.Where(e => ReturnCodes.Contains(e.TransactionCode ?? string.Empty)))
        {
            var relatedAddenda = validAddendas
                .Where(addenda => IsAddendaForEntry(entry, addenda)
                    && string.Equals(addenda.CodeTypeAddendumRecord?.Trim(), "99", StringComparison.OrdinalIgnoreCase))
                .ToList();

            var reasonCode = ExtractReturnReasonCode(relatedAddenda);
            var originalTraceRef = ResolveOriginalTraceReference(entry, relatedAddenda);

            if (string.IsNullOrWhiteSpace(reasonCode) || string.IsNullOrWhiteSpace(originalTraceRef))
            {
                continue;
            }

            var transaction = await FindTransactionByTraceReferenceAsync(originalTraceRef, ct);
            if (transaction is null || !processedTransactionIds.Add(transaction.Id))
            {
                continue;
            }

            if (_catalogService is not null)
            {
                var processingDate = ResolveNachaFileDate(entry.NachaHeader?.FileCreationDate) ?? DateTime.UtcNow.Date;
                var rule = await _catalogService.ValidateReturnCodeAsync(
                    transaction.AchCycle.ClearingHouseId,
                    reasonCode,
                    TransactionTypeEnum.Return,
                    processingDate,
                    DateTime.UtcNow.Date,
                    ct);
                if (!rule.IsAllowed)
                {
                    _logger.LogWarning("Causal de devolución {ReasonCode} descartada por catálogo regulatorio: {Reason}", reasonCode, rule.Reason);
                    continue;
                }

                var policy = await _catalogService.ValidateReturnPolicyAsync(
                    transaction.AchCycle.ClearingHouseId,
                    transaction.Type,
                    reasonCode,
                    transaction.EffectiveEntryDate.Date,
                    DateTime.UtcNow.Date,
                    relatedAddenda.Any(),
                    transaction.State.ToString(),
                    ct);
                if (!policy.IsAllowed)
                {
                    _logger.LogWarning("Transición de devolución descartada por política: {Reason}", policy.Reason);
                    continue;
                }
            }

            try
            {
                await _stateTransitionService.TransitionAsync(
                    transaction.Id,
                    AchTransferStateEnum.ReturnedByEpr,
                    AchStateEventSourceEnum.Epr,
                    reasonCode: reasonCode,
                    payloadJson: BuildParserPayload(entry, relatedAddenda),
                    originalTraceRef: originalTraceRef);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    "NACHA_RETURN_TRANSITION_REJECTED Transition=ReturnedByEpr ErrorType={ErrorType}",
                    ex.GetType().Name);
            }
        }

        static DateTime? ResolveNachaFileDate(string? rawDate)
        {
            if (string.IsNullOrWhiteSpace(rawDate))
            {
                return null;
            }

            var value = rawDate.Trim();
            var formats = new[] { "yyMMdd", "yyyyMMdd" };
            return DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
                ? parsed.Date
                : null;
        }
    }

    private async Task ApplyReturnedByOperatorTransitionsAsync(IReadOnlyList<NachaValidationFailure> failures,
        CancellationToken ct)
    {
        var processedTransactionIds = new HashSet<int>();

        foreach (var failure in failures.Where(f => !string.IsNullOrWhiteSpace(f.EntrySequence)))
        {
            var transaction = await FindTransactionByTraceReferenceAsync(failure.EntrySequence, ct);
            if (transaction is null || !processedTransactionIds.Add(transaction.Id))
            {
                continue;
            }

            var reasonCode = ExtractOperatorReasonCode(failure.Reason);

            try
            {
                await _stateTransitionService.TransitionAsync(
                    transaction.Id,
                    AchTransferStateEnum.ReturnedByOperator,
                    AchStateEventSourceEnum.Operator,
                    reasonCode: reasonCode,
                    payloadJson: BuildOperatorFailurePayload(failure));
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(
                    "NACHA_RETURN_TRANSITION_REJECTED Transition=ReturnedByOperator ErrorType={ErrorType}",
                    ex.GetType().Name);
            }
        }
    }

    private async Task<AchTransaction?> FindTransactionByTraceReferenceAsync(string? traceReference, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(traceReference))
        {
            return null;
        }

        var normalized = traceReference.Trim();

        var transaction = await _context.AchTransactions
            .Include(t => t.AchCycle)
            .FirstOrDefaultAsync(t => t.TraceNumber == normalized, ct);

        if (transaction is not null)
        {
            return transaction;
        }

        var sequenceSuffix = GetEntrySequenceSuffix(normalized);
        if (!int.TryParse(sequenceSuffix, out var sequenceNumber))
        {
            return null;
        }

        return await _context.AchTransactions
            .Include(t => t.AchCycle)
            .FirstOrDefaultAsync(t => t.TraceSequenceNumber == sequenceNumber, ct);
    }

    private static string? ResolveOriginalTraceReference(EntryDetail entry, IReadOnlyList<AddendaRecord> relatedAddenda)
    {
        var originalTrace = relatedAddenda
            .Select(addenda => addenda.OriginalTraceNumber)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));

        if (!string.IsNullOrWhiteSpace(originalTrace))
        {
            var digits = new string(originalTrace.Where(char.IsDigit).ToArray());
            if (digits.Length == 15)
            {
                return digits;
            }
        }

        return null;
    }

    private static string? ExtractReturnReasonCode(IReadOnlyList<AddendaRecord> relatedAddenda)
    {
        return relatedAddenda
            .Select(addenda => addenda.ReturnReasonCode?.Trim().ToUpperInvariant())
            .FirstOrDefault(code => !string.IsNullOrWhiteSpace(code));
    }

    private static string ExtractOperatorReasonCode(string reason)
    {
        var match = Regex.Match(reason ?? string.Empty, @"\bD\d{2}\b", RegexOptions.IgnoreCase);
        return match.Success ? match.Value.ToUpperInvariant() : "D00";
    }

    private static string BuildOperatorFailurePayload(NachaValidationFailure failure)
    {
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            recordType = failure.RecordType,
            hasEntrySequence = !string.IsNullOrWhiteSpace(failure.EntrySequence),
            errorCode = ExtractOperatorReasonCode(failure.Reason)
        });
    }

    private string GetRegulatoryError(string dxxCode, string fallbackMessage)
    {
        if (_rejectionCatalog.TryGetValue(dxxCode, out var catalog))
        {
            return $"{catalog.Code}: {catalog.Description}. {fallbackMessage}";
        }

        return $"{dxxCode}: {fallbackMessage}";
    }

    [DoesNotReturn]
    private void ThrowRegulatory(string dxxCode, string fallbackMessage)
    {
        throw new InvalidOperationException($"[{ValidationBoundary.Regulatory}] {GetRegulatoryError(dxxCode, fallbackMessage)}");
    }

    [DoesNotReturn]
    private static void ThrowTechnical(string message)
    {
        throw new InvalidOperationException($"[{ValidationBoundary.Technical}] {message}");
    }

    private static string BuildParserPayload(EntryDetail entry, IReadOnlyList<AddendaRecord> relatedAddenda)
    {
        var addendaCount = relatedAddenda.Count;
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            transactionCode = entry.TransactionCode,
            hasEntrySequence = !string.IsNullOrWhiteSpace(entry.SequenceNumber),
            addendaCount
        });
    }

    private static string ComputeSafeIncident(string errorType)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"NACHA_PARSE|{errorType}|{Guid.NewGuid():N}")))[..12];

    private async Task<HashSet<string>> GetConfiguredTransactionCodesAsync(CancellationToken ct)
    {
        if (_configuredTransactionCodes is not null)
        {
            return _configuredTransactionCodes;
        }

        var configuredCodes = await _context.TransactionCodes
            .AsNoTracking()
            .Select(x => x.Code)
            .ToListAsync(ct);

        _configuredTransactionCodes = configuredCodes.Count == 0
            ? new HashSet<string>(FallbackTransactionCodes, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(configuredCodes, StringComparer.OrdinalIgnoreCase);

        return _configuredTransactionCodes;
    }

    private static bool ShouldValidateCreditIdentity(string? discretionaryData)
    {
        if (string.IsNullOrWhiteSpace(discretionaryData))
        {
            return false;
        }

        return discretionaryData.StartsWith("V", StringComparison.OrdinalIgnoreCase);
    }
}
