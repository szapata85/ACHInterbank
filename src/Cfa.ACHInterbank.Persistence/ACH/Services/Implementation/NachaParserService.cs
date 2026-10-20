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
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;
using NachaHeader = Cfa.ACHInterbank.Application.ACH.Models.ParsedNachaHeader;
using BatchHeader = Cfa.ACHInterbank.Application.ACH.Models.ParsedBatchHeader;
using EntryDetail = Cfa.ACHInterbank.Application.ACH.Models.ParsedEntryDetail;
using AddendaRecord = Cfa.ACHInterbank.Application.ACH.Models.ParsedAddendaRecord;
using BatchControl = Cfa.ACHInterbank.Application.ACH.Models.ParsedBatchControl;
using FileControl = Cfa.ACHInterbank.Application.ACH.Models.ParsedFileControl;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public class NachaParserService : INachaParserService
{
    private readonly AchDbContext _context;
    private readonly ILogger<NachaParserService> _logger;
    private readonly IAchStateTransitionService _stateTransitionService;
    private readonly IAchRegulatoryCatalogService? _catalogService;
    private readonly IConfiguration? _configuration;
    private readonly NachaInboundPersistenceCoordinator _persistenceCoordinator;
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
        _persistenceCoordinator = new NachaInboundPersistenceCoordinator(context);
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

            var selectedPublication = request?.SelectedProfileId is int publishedProfileId
                && request.RequirePublishedProfileSnapshot
                    ? await NachaProfileRecordReader.LoadPublishedSnapshotAsync(_context, publishedProfileId, ct)
                    : null;
            var profileReader = selectedPublication is not null
                ? NachaProfileRecordReader.FromPublication(selectedPublication)
                : request?.SelectedProfileId is int selectedProfileId
                    ? await NachaProfileRecordReader.LoadAsync(_context, selectedProfileId, ct)
                    : null;
            if (profileReader is not null
                && !string.IsNullOrWhiteSpace(request?.SelectedProfileCode)
                && !string.Equals(profileReader.ProfileCode, request.SelectedProfileCode, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("NACHA_PROFILE_SNAPSHOT_MISMATCH: la identidad del perfil seleccionado cambió antes del parser.");
            }
            var inboundValidator = new NachaInboundValidator(_rejectionCatalog);
            var databaseValidator = new NachaInboundDatabaseValidator(
                _context,
                inboundValidator,
                IsLocalLiveProcTransaccionesPreparationEnabled());
            var validationContext = inboundValidator.CreateContext(selectedPublication, request, profileReader);
            var publishedAuthority = validationContext.PublishedAuthority;
            var isCenitInboundProfile = validationContext.IsCenitInboundProfile;
            var isCenitCtxInboundProfile = validationContext.IsCenitCtxInboundProfile;

            var physicalFile = await NachaPhysicalParser.ParseAsync(nachaStream, ct, profileReader);
            var lines = physicalFile.Records.Select(record => record.Content).ToList();

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
            var expectedAddendaCount = 0;
            var parsedAddendaCount = 0;
            var entryDetails = new List<EntryDetail>();
            var addendaRecords = new List<AddendaRecord>();
            var optionalAddendaEntries = new HashSet<string>(StringComparer.Ordinal);
            var batchControls = new List<BatchControl>();
            var fileControls = new List<FileControl>();
            var lastConsecutiveByBatch = new Dictionary<int, int>();
            var seenSequenceNumbers = new HashSet<string>(StringComparer.Ordinal);
            NachaInboundBatchRuntimeMetrics? currentBatchMetrics = null;
            var fileMetrics = new NachaInboundFileRuntimeMetrics();
            var fileControlLineIndex = -1;
            var fileControlEncountered = false;

            for (int lineIndex = 0; lineIndex < lines.Count; lineIndex++)
            {
                var line = lines[lineIndex];
                var recordType = line[0];
                switch (recordType)
                {
                    case '1':
                        currentHeader = ParseFileHeaderLinq([line], clearingHouseMap, fileName, request, profileReader).FirstOrDefault();
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
                            inboundValidator.ThrowRegulatory("D01", "El Archivo NACHA ya existe!");
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
                            inboundValidator.EnsureNoPendingAddenda(lastEntryAwaitingAddenda, "ACHCOL-T6-T7-ORDER: una entrada T6 declaró adenda y no fue seguida por su T7.");
                        }

                        lastEntry = null;
                        lastEntryAccepted = false;
                        currentBatch = ParseBatchHeaderLinq([line], profileReader).FirstOrDefault();
                        if (currentBatch is not null)
                        {
                            if (publishedAuthority is not null)
                            {
                                inboundValidator.ValidatePublishedBatchService(publishedAuthority, currentBatch);
                            }
                            else if (isCenitInboundProfile)
                            {
                                NachaInboundCenitValidationRules.ValidateBatchService(profileReader!.ProfileCode, currentBatch);
                            }
                            currentBatchMetrics = new NachaInboundBatchRuntimeMetrics();
                            fileMetrics.RegisterBatch();
                            currentBatch.NachaID = currentHeader?.NachaID;
                            currentHeader?.Batches?.Add(currentBatch);
                            totalBatches++;
                        }
                        break;
                    case '6':
                        if (lastEntryAwaitingAddenda)
                        {
                            inboundValidator.EnsureNoPendingAddenda(lastEntryAwaitingAddenda, "ACHCOL-T6-T7-ORDER: una entrada T6 declaró adenda y no fue seguida inmediatamente por su T7.");
                        }

                        var entry = ParseEntryDetailLinq([line], profileReader).FirstOrDefault();
                        if (entry is null)
                        {
                            break;
                        }

                        entry.NachaID = currentHeader?.NachaID;
                        entry.NachaHeader = currentHeader;
                        entry.BatchNumber = currentBatch?.BatchNumber
                            ?? throw new InvalidOperationException("Registro tipo 6 recibido sin BatchHeader tipo 5 asociado.");
                        var publishedRule = inboundValidator.ResolvePublishedTransactionRule(publishedAuthority, entry);
                        if (currentBatchMetrics is null)
                        {
                            inboundValidator.ThrowRegulatory("D06", "Se recibió un registro tipo 6 sin un registro tipo 5 asociado.");
                        }
                        currentBatchMetrics.RegisterEntry(entry, publishedRule);
                        fileMetrics.RegisterEntry(entry, publishedRule);

                        await databaseValidator.ValidateEntrySequencePolicyAsync(entry, currentBatch, currentHeader, lastConsecutiveByBatch,
                            seenSequenceNumbers, publishedAuthority is not null
                                ? !publishedAuthority.IsCenit && NachaInboundAchColombiaValidationRules.IsPseCcdBatch(currentBatch)
                                : !isCenitInboundProfile, ct);
                        if (publishedAuthority is null && isCenitInboundProfile)
                        {
                            NachaInboundCenitValidationRules.ValidateEntry(profileReader!.ProfileCode, entry);
                        }

                        var addendaPreparation = inboundValidator.ResolveEntryAddendaPreparation(
                            validationContext,
                            currentBatch,
                            entry,
                            publishedRule);
                        var publishedBounds = addendaPreparation.PublishedBounds;
                        var optionalAddenda = addendaPreparation.OptionalAddenda;

                        var isReturnEntry = profileReader is not null
                                            && lineIndex + 1 < lines.Count
                                            && profileReader.IsReturnAddenda(lines[lineIndex + 1]);
                        var (isValid, failureReason) = await databaseValidator.ValidateEntryAsync(
                            entry,
                            currentBatch,
                            isReturnEntry,
                            failures,
                            optionalAddenda,
                            publishedRule,
                            publishedAuthority,
                            ct);
                        lastEntry = entry;
                        lastEntryAccepted = isValid;
                        expectedAddendaCount = inboundValidator.ResolveExpectedAddendaCount(profileReader, line, entry,
                            publishedBounds, publishedAuthority?.HasT6AddendaCount);
                        parsedAddendaCount = 0;
                        lastEntryAwaitingAddenda = expectedAddendaCount > 0;
                        if (isValid)
                        {
                            entryDetails.Add(entry);
                            if (optionalAddenda && !string.IsNullOrEmpty(entry.SequenceNumber))
                            {
                                optionalAddendaEntries.Add(entry.SequenceNumber);
                            }
                            totalEntries++;
                        }

                        break;
                    case '7':
                        if (lastEntry is null || !lastEntryAwaitingAddenda)
                        {
                            inboundValidator.EnsureAddendaCanAttach(lastEntry, lastEntryAwaitingAddenda);
                        }

                        var addenda = ParseAddendaLinq([line], lastEntry, profileReader, publishedAuthority).FirstOrDefault();
                        if (addenda is not null)
                        {
                            parsedAddendaCount++;
                            if (publishedAuthority is not null)
                            {
                                inboundValidator.ValidatePublishedAddenda(publishedAuthority, lastEntry, addenda, parsedAddendaCount);
                            }
                            else if (isCenitInboundProfile)
                            {
                                NachaInboundCenitValidationRules.ValidateAddenda(
                                    lastEntry,
                                    addenda,
                                    parsedAddendaCount,
                                    isCenitCtxInboundProfile);
                            }
                            addenda.NachaID = currentHeader?.NachaID;
                            currentBatchMetrics?.RegisterAddenda();
                            fileMetrics.RegisterAddenda();
                            if (lastEntryAccepted)
                            {
                                addenda.EntryDetailSequenceNumber ??= NachaInboundValidator.GetEntrySequenceSuffix(lastEntry.SequenceNumber);
                                addendaRecords.Add(addenda);
                                totalAddendas++;
                            }

                            lastEntryAwaitingAddenda = parsedAddendaCount < expectedAddendaCount;
                        }
                        break;
                    case '8':
                        if (lastEntryAwaitingAddenda)
                        {
                            inboundValidator.EnsureNoPendingAddenda(lastEntryAwaitingAddenda, "ACHCOL-T6-T7-ORDER: el lote cerró sin el T7 declarado por la última entrada T6.");
                        }

                        var batchControl = ParseBatchControlLinq([line], profileReader).FirstOrDefault();
                        if (batchControl is not null)
                        {
                            inboundValidator.ValidateCurrentBatchControl(currentBatch, currentBatchMetrics, batchControl);
                            batchControls.Add(batchControl);
                            currentBatchMetrics = null;
                            lastEntry = null;
                            lastEntryAccepted = false;
                        }
                        break;
                    case '9':
                        if (lastEntryAwaitingAddenda)
                        {
                            inboundValidator.EnsureNoPendingAddenda(lastEntryAwaitingAddenda, "ACHCOL-T6-T7-ORDER: el archivo cerró sin el T7 declarado por la última entrada T6.");
                        }

                        if (!fileControlEncountered)
                        {
                            var fileControl = ParseFileControlLinq([line], profileReader).FirstOrDefault();
                            if (fileControl is not null)
                            {
                                fileControls.Add(fileControl);
                                fileControlEncountered = true;
                                fileControlLineIndex = lineIndex;
                            }
                        }
                        else if (!NachaInboundValidator.IsPaddingRecord(line))
                        {
                            inboundValidator.ThrowRegulatory("D02", "Los registros de relleno después del Registro Tipo 9 deben contener únicamente el carácter '9' en sus 106 posiciones.");
                        }
                        break;
                }
            }

            var validEntries = inboundValidator.EnforceAddendaRequirements(entryDetails, addendaRecords, failures, optionalAddendaEntries);
            inboundValidator.ValidateBatchSequenceAndControlConsistency(headers.SelectMany(h => h.Batches ?? []), batchControls);
            inboundValidator.ValidateFileControlConsistency(headers.SelectMany(h => h.Batches ?? []), batchControls, fileControls, fileMetrics, lines, fileControlLineIndex);

            if (currentHeader is not null)
            {
                currentHeader.EntryDetails = validEntries;
                currentHeader.AddendaRecords = addendaRecords
                    .Where(addenda => validEntries.Any(entry => NachaInboundValidator.IsAddendaForEntry(entry, addenda)))
                    .ToList();
                currentHeader.BatchControls = batchControls;
                currentHeader.FileControls = fileControls;
            }

            await _persistenceCoordinator.PersistAsync(headers, ct);
            var validAddendas = currentHeader?.AddendaRecords?.ToList() ?? [];
            if (request?.IncomingNachaFileIngestionId is null)
            {
                await ApplyReturnStateTransitionsAsync(validEntries, validAddendas, failures, ct);
            }
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

    private List<NachaHeader> ParseFileHeaderLinq(
        List<string> line,
        Dictionary<string, int> clearingHouseMap,
        string FileName,
        NachaParseRequest? request,
        NachaProfileRecordReader? profileReader)
    {
        int? cycleNumber = CenitOfficialFileNameParser.ExtractCycleNumberFromFileName(FileName);

        return line.Select(a =>
        {
            string immediateOrigin = ReadField(profileReader, a, "1", "IMMEDIATEORIGIN").Trim();
            int? clearingHouseId = request?.ResolvedClearingHouseId ?? (clearingHouseMap.TryGetValue(immediateOrigin, out var chId) ? chId : null);
            string fileCreationDate = ReadField(profileReader, a, "1", "FILECREATIONDATE");
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
                    $"{ReadField(profileReader, a, "1", "IMMEDIATEDESTINATION").Trim()}{immediateOrigin}{fileCreationDate.Trim()}{ReadField(profileReader, a, "1", "FILECREATIONTIME").Trim()}{ReadField(profileReader, a, "1", "FILEIDMODIFIER").Trim()}"),
                PriorityCode = ReadField(profileReader, a, "1", "PRIORITYCODE"),
                ImmediateDestination = ReadField(profileReader, a, "1", "IMMEDIATEDESTINATION").Trim(),
                ImmediateOrigin = immediateOrigin,
                FileCreationDate = fileCreationDate,
                FileCreationTime = ReadField(profileReader, a, "1", "FILECREATIONTIME"),
                FileIdModifier = ReadField(profileReader, a, "1", "FILEIDMODIFIER"),
                RecordSize = ReadField(profileReader, a, "1", "RECORDSIZE"),
                BlockingFactor = ReadField(profileReader, a, "1", "BLOCKINGFACTOR"),
                FormatCode = ReadField(profileReader, a, "1", "FORMATCODE"),
                ImmediateDestinationName = ReadField(profileReader, a, "1", "IMMEDIATEDESTINATIONNAME").Trim(),
                ImmediateOriginName = ReadField(profileReader, a, "1", "IMMEDIATEORIGINNAME").Trim(),
                ReferenceCode = ReadField(profileReader, a, "1", "REFERENCECODE").Trim(),
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

    private List<BatchHeader> ParseBatchHeaderLinq(List<string> line, NachaProfileRecordReader? profileReader)
    {
        return line.Select(a =>
        {
            if (a.Length < BatchHeaderType5JulianDateValidator.RecordLength || a[0] != '5')
            {
                throw new InvalidOperationException("El Registro Tipo 5 debe iniciar con '5' y tener longitud fija de 106 caracteres.");
            }

            var rawCompensationDate = ReadField(profileReader, a, "5", "SETTLEMENTDATE");
            var compensationValidation = BatchHeaderType5JulianDateValidator.ValidateAndFormat(rawCompensationDate);
            if (!compensationValidation.IsValid)
            {
                throw new InvalidOperationException(compensationValidation.ErrorMessage ?? "Error Fatal 65: la Fecha de Compensación Juliana contiene caracteres no numéricos.");
            }

            var rawBatchNumber = ReadField(profileReader, a, "5", "BATCHNUMBER");
            if (!rawBatchNumber.All(char.IsDigit))
            {
                throw new InvalidOperationException("Error Fatal ID 5: el Número de Lote del registro tipo 5 (posiciones 92-98) debe ser numérico de 7 dígitos.");
            }

            var standardEntryClassCode = ReadField(profileReader, a, "5", "STANDARDENTRYCLASSCODE").Trim();
            if (CenitReturnIn2026Layout.IsProfile(profileReader?.ProfileCode)
                && standardEntryClassCode is not ("PPD" or "CCD"))
            {
                ThrowTechnical($"CENIT-2026-T5-SEC: RET-CENIT-IN-001 soporta únicamente PPD y CCD; {CenitReturnIn2026Layout.CtxScopeStatus}.");
            }
            if (CenitReturnOfReturn2026Layout.IsProfile(profileReader?.ProfileCode)
                && standardEntryClassCode != "PPD")
            {
                ThrowTechnical($"CENIT-2026-ROR-T5-SEC: el contrato específico de ROR aplica a PPD; {CenitReturnOfReturn2026Layout.CcdScopeStatus}; {CenitReturnOfReturn2026Layout.CtxScopeStatus}.");
            }

            return new BatchHeader
            {
                ServiceClassCode = ReadField(profileReader, a, "5", "SERVICECLASSCODE"),
                CompanyName = ReadField(profileReader, a, "5", "COMPANYNAME").Trim(),
                DiscretionaryData = ReadField(profileReader, a, "5", "COMPANYDISCRETIONARYDATA").Trim(),
                CompanyId = ReadField(profileReader, a, "5", "COMPANYIDENTIFICATION").Trim(),
                StandardEntryClassCode = standardEntryClassCode,
                CompanyEntryDescription = ReadField(profileReader, a, "5", "COMPANYENTRYDESCRIPTION").Trim(),
                DescriptiveDate = ReadField(profileReader, a, "5", "COMPANYDESCRIPTIVEDATE").Trim(),
                EffectiveEntryDate = ReadField(profileReader, a, "5", "EFFECTIVEENTRYDATE").Trim(),
                CompensationDate = compensationValidation.FormattedValue.Trim(),
                OriginUserStatusCode = ReadField(profileReader, a, "5", "ORIGINATORSTATUSCODE").Trim(),
                OriginParticipantEntityCode = ReadField(profileReader, a, "5", "ORIGINATINGDFI").Trim(),
                BatchNumber = int.Parse(rawBatchNumber)
            };
        }).ToList();
    }

    private List<EntryDetail> ParseEntryDetailLinq(List<string> line, NachaProfileRecordReader? profileReader)
    {
        return line.Select(a => new EntryDetail
        {
            TransactionCode = ReadField(profileReader, a, "6", "TRANSACTIONCODE").Trim(),
            ReceivingParticipantEntityCode = ReadField(profileReader, a, "6", "RECEIVINGDFI").Trim(),
            CheckDigit = ReadField(profileReader, a, "6", "CHECKDIGIT").Trim(),
            AccountNumber = ReadField(profileReader, a, "6", "DFIACCOUNTNUMBER").TrimEnd(),
            Amount = Convert.ToDecimal(ReadField(profileReader, a, "6", "AMOUNT"), CultureInfo.InvariantCulture) / 100,
            RecipIdNumber = ReadField(profileReader, a, "6", "INDIVIDUALIDENTIFICATION").TrimEnd(),
            RecipUserName = ReadField(profileReader, a, "6", "INDIVIDUALNAME"),
            DiscreData = ReadField(profileReader, a, "6", "DISCRETIONARYDATA"),
            AddendumIndicator = ReadField(profileReader, a, "6", "ADDENDARECORDINDICATOR").Trim(),
            SequenceNumber = ReadField(profileReader, a, "6", "TRACENUMBER").Trim()
        }).ToList();
    }

    internal List<AddendaRecord> ParseAddendaLinq(
        List<string> line,
        EntryDetail? associatedEntry,
        NachaProfileRecordReader? profileReader)
        => ParseAddendaLinq(line, associatedEntry, profileReader, null);

    private List<AddendaRecord> ParseAddendaLinq(
        List<string> line,
        EntryDetail? associatedEntry,
        NachaProfileRecordReader? profileReader,
        NachaInboundPublishedAuthority? publishedAuthority)
    {
        return line.Select(a =>
        {
            var addendaType = ReadField(profileReader, a, "7", "ADDENDATYPE").Trim();
            if (addendaType == "99")
            {
                if (profileReader is null)
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

                var addendaSequence = ReadField(profileReader, a, "7", "ADDENDASEQUENCENUMBER").Trim();
                var returnReasonCode = ReadField(profileReader, a, "7", "RETURNREASONCODE").Trim();
                var originalTraceNumber = ReadField(profileReader, a, "7", "ORIGINALTRACENUMBER").Trim();
                var originalReceivingDfi = ReadField(profileReader, a, "7", "ORIGINALRECEIVINGDFI").Trim();
                if (CenitReturnOfReturn2026Layout.IsProfile(profileReader.ProfileCode))
                {
                    var sourceReturnTrace = ReadField(profileReader, a, "7", "SOURCERETURNTRACENUMBER").Trim();
                    var sourceReturnDate = ReadField(profileReader, a, "7", "SOURCERETURNSETTLEMENTDATE").Trim();
                    var sourceReturnReason = ReadField(profileReader, a, "7", "SOURCERETURNREASONCODE").Trim();
                    if (!CenitReturnOfReturn2026Layout.TryParseAddenda(a, out _))
                    {
                        ThrowTechnical("CENIT-2026-ROR-T7: la adenda ROR no cumple el Anexo 1.7.");
                    }

                    return new AddendaRecord
                    {
                        CodeTypeAddendumRecord = addendaType,
                        BusinessType = "ReturnOfReturn",
                        IdUserOrig = originalReceivingDfi,
                        InvoiceOrAccountNumber = sourceReturnReason,
                        PurposeOfTransaction = sourceReturnDate,
                        ReturnReasonCode = returnReasonCode,
                        OriginalTraceNumber = originalTraceNumber,
                        NewTraceNumber = sourceReturnTrace,
                        AddendumSequence = addendaSequence,
                        EntryDetailSequenceNumber = NachaInboundValidator.GetEntrySequenceSuffix(addendaSequence)
                    };
                }
                if (CenitReturnIn2026Layout.IsProfile(profileReader.ProfileCode)
                    && CenitReturnIn2026Layout.IsReturnOfReturnCause(returnReasonCode))
                {
                    ThrowTechnical("CENIT_ROR_NOT_ORDINARY_RETURN: la devolución de una devolución requiere su contrato y flujo independientes.");
                }

                if (CenitReturnIn2026Layout.IsProfile(profileReader.ProfileCode)
                    && !CenitReturnIn2026Layout.IsOrdinaryReturnCause(returnReasonCode))
                {
                    ThrowTechnical($"CENIT-2026-T7-RETURN-REASON: la causal {returnReasonCode} no pertenece al catálogo de devolución CENIT.");
                }

                if (CenitReturnIn2026Layout.IsProfile(profileReader.ProfileCode)
                    && (originalTraceNumber.Length != 15 || !originalTraceNumber.All(char.IsDigit)))
                {
                    ThrowTechnical("CENIT-2026-T7-ORIGINAL-TRACE: el número de secuencia original debe contener 15 dígitos.");
                }

                if (CenitReturnIn2026Layout.IsProfile(profileReader.ProfileCode)
                    && (originalReceivingDfi.Length != 8 || !originalReceivingDfi.All(char.IsDigit)))
                {
                    ThrowTechnical("CENIT-2026-T7-ORIGINAL-RECEIVING-DFI: el participante receptor original debe contener 8 dígitos.");
                }

                if (CenitReturnIn2026Layout.IsProfile(profileReader.ProfileCode)
                    && (addendaSequence.Length != 15 || !addendaSequence.All(char.IsDigit)))
                {
                    ThrowTechnical("CENIT-2026-T7-ADDENDA-SEQUENCE: el número de secuencia de la adenda debe contener 15 dígitos.");
                }

                return new AddendaRecord
                {
                    CodeTypeAddendumRecord = addendaType,
                    BusinessType = "Return",
                    IdUserOrig = originalReceivingDfi,
                    InfofromOriginator = ReadField(profileReader, a, "7", "ADDITIONALINFORMATION").TrimEnd(),
                    ReturnReasonCode = returnReasonCode,
                    OriginalTraceNumber = originalTraceNumber,
                    NewTraceNumber = addendaSequence,
                    EntryDetailSequenceNumber = NachaInboundValidator.GetEntrySequenceSuffix(addendaSequence)
                };
            }

            if (profileReader is not null
                && (publishedAuthority?.IsCenit
                    ?? CenitOrdinaryInbound2026Layout.IsProfile(profileReader.ProfileCode)))
            {
                var paymentInformation = ReadField(profileReader, a, "7", "PAYMENTRELATEDINFORMATION").TrimEnd();
                return new AddendaRecord
                {
                    CodeTypeAddendumRecord = addendaType,
                    BusinessType = ParseBusinessTypeFromType05(associatedEntry?.TransactionCode, publishedAuthority?.TransactionCodes),
                    PaymentRelatedInformation = paymentInformation,
                    InfofromOriginator = paymentInformation,
                    AddendumSequence = ReadField(profileReader, a, "7", "SEQUENCENUMBER").Trim(),
                    EntryDetailSequenceNumber = ReadField(profileReader, a, "7", "TRACESUFFIX").Trim()
                };
            }

            if (publishedAuthority is null && addendaType != "05")
            {
                ThrowTechnical("ACHCOL-T7-ADDENDA-TYPE: el tipo de adenda no está demostrado para el perfil ACHCOL oficial.");
            }

            var businessType = ParseBusinessTypeFromType05(associatedEntry?.TransactionCode, publishedAuthority?.TransactionCodes);
            var isPrenotification = publishedAuthority is not null
                ? publishedAuthority.TransactionCodes.TryGetRule(associatedEntry?.TransactionCode ?? string.Empty, out var addendaRule)
                    && addendaRule.IsPrenotification
                : IsPrenotificationTransactionCode(associatedEntry?.TransactionCode);
            var variant = publishedAuthority is not null ? null : businessType == "Debit"
                ? AchColOfficialNachaLayout.Type7DebitVariant
                : isPrenotification
                    ? AchColOfficialNachaLayout.Type7CreditPrenotificationVariant
                    : AchColOfficialNachaLayout.Type7CreditMonetaryVariant;
            var selectionContext = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["BusinessType"] = businessType,
                ["TransactionFamily"] = isPrenotification
                    ? "PRENOTIFICATION"
                    : "MONETARY"
            };
            string? invoiceOrAccountNumber = null;
            string? originatorFreeInformation = null;
            if (businessType == "Credit")
            {
                if (isPrenotification)
                {
                    invoiceOrAccountNumber = ReadField(profileReader, a, "7", "REFERENCE", variant, selectionContext).Trim();
                }
                else
                {
                    invoiceOrAccountNumber = ReadField(profileReader, a, "7", "INVOICEORACCOUNTNUMBER", variant, selectionContext).Trim();
                    originatorFreeInformation = ReadField(profileReader, a, "7", "ORIGINATORFREEINFORMATION", variant, selectionContext).Trim();
                }
            }

            return new AddendaRecord
            {
                CodeTypeAddendumRecord = ReadField(profileReader, a, "7", "ADDENDATYPE", variant, selectionContext).Trim(),
                BusinessType = businessType,
                IdUserOrig = businessType == "Credit"
                    ? ReadField(profileReader, a, "7", "ORIGINATORIDENTIFICATION", variant, selectionContext).Trim()
                    : null,
                CollectorId = businessType == "Debit"
                    ? ReadField(profileReader, a, "7", "COLLECTORID", variant, selectionContext).Trim()
                    : null,
                ReceiverCustomerCode = businessType == "Debit"
                    ? ReadField(profileReader, a, "7", "RECEIVERCUSTOMERCODE", variant, selectionContext).Trim()
                    : null,
                ServiceDescription = businessType == "Debit"
                    ? ReadField(profileReader, a, "7", "SERVICEDESCRIPTION", variant, selectionContext).Trim()
                    : null,
                PurposeOfTransaction = businessType == "Credit"
                    ? ReadField(profileReader, a, "7", "PURPOSE", variant, selectionContext).Trim()
                    : null,
                InvoiceOrAccountNumber = invoiceOrAccountNumber,
                InfofromOriginator = originatorFreeInformation,
                AddendumSequence = ReadField(profileReader, a, "7", "SEQUENCENUMBER", variant, selectionContext).Trim(),
                EntryDetailSequenceNumber = ReadField(profileReader, a, "7", "TRACESUFFIX", variant, selectionContext).Trim()
            };
        }).ToList();
    }


    private List<BatchControl> ParseBatchControlLinq(List<string> line, NachaProfileRecordReader? profileReader)
    {
        return line.Select(a => new BatchControl
        {
            BatchTranClassCode = ReadField(profileReader, a, "8", "SERVICECLASSCODE"),
            EntryAddendaCount = int.Parse(ReadField(profileReader, a, "8", "ENTRYADDENDACOUNT"), CultureInfo.InvariantCulture),
            EntryHash = long.Parse(ReadField(profileReader, a, "8", "ENTRYHASH"), CultureInfo.InvariantCulture),
            TotalDebitAmount = Convert.ToDecimal(ReadField(profileReader, a, "8", "TOTALDEBITAMOUNT"), CultureInfo.InvariantCulture) / 100,
            TotalCreditAmount = Convert.ToDecimal(ReadField(profileReader, a, "8", "TOTALCREDITAMOUNT"), CultureInfo.InvariantCulture) / 100,
            IdUserOrig = ReadField(profileReader, a, "8", "COMPANYIDENTIFICATION").Trim(),
            CodAutMessage = ReadField(profileReader, a, "8", "MESSAGEAUTHENTICATIONCODE"),
            Reserved = ReadField(profileReader, a, "8", "RESERVED"),
            IdOrigEntity = ReadField(profileReader, a, "8", "ORIGINATINGDFI"),
            BatchNumber = ReadField(profileReader, a, "8", "BATCHNUMBER"),
        }).ToList();
    }

    private static string ParseBusinessTypeFromType05(
        string? transactionCode,
        NachaTransactionCodeSemanticContract? publishedTransactionCodes = null)
    {
        var normalized = (transactionCode ?? string.Empty).Trim();
        if (publishedTransactionCodes is not null)
        {
            if (!publishedTransactionCodes.TryGetRule(normalized, out var semantic))
            {
                throw new InvalidOperationException("INBOUND_PUBLISH_T6_SEMANTIC_MISMATCH");
            }
            return semantic.Direction == NachaEntryDirection.Debit ? "Debit" : "Credit";
        }
        if (NachaInboundTransactionCodeSets.DebitCodes.Contains(normalized))
        {
            return "Debit";
        }

        if (NachaInboundTransactionCodeSets.CreditCodes.Contains(normalized))
        {
            return "Credit";
        }

        ThrowTechnical("ACHCOL-T7-VARIANT: no se puede seleccionar la variante T7 sin un código de transacción T6 demostrado.");
        return string.Empty;
    }

    private static bool IsPrenotificationTransactionCode(string? transactionCode)
        => (transactionCode ?? string.Empty).Trim() is "23" or "28" or "33" or "38" or "53" or "57";

    private List<FileControl> ParseFileControlLinq(List<string> line, NachaProfileRecordReader? profileReader)
    {
        return line.Take(1).Select(a => new FileControl
        {
            BatchCount = int.Parse(ReadField(profileReader, a, "9", "BATCHCOUNT"), CultureInfo.InvariantCulture),
            BlockCount = int.Parse(ReadField(profileReader, a, "9", "BLOCKCOUNT"), CultureInfo.InvariantCulture),
            EntryAddendaCount = int.Parse(ReadField(profileReader, a, "9", "ENTRYADDENDACOUNT"), CultureInfo.InvariantCulture),
            EntryHash = long.Parse(ReadField(profileReader, a, "9", "ENTRYHASH"), CultureInfo.InvariantCulture),
            TotalDebitAmount = Convert.ToDecimal(ReadField(profileReader, a, "9", "TOTALDEBITAMOUNT"), CultureInfo.InvariantCulture) / 100,
            TotalCreditAmount = Convert.ToDecimal(ReadField(profileReader, a, "9", "TOTALCREDITAMOUNT"), CultureInfo.InvariantCulture) / 100,
            Reserved = ReadField(profileReader, a, "9", "RESERVED")
        }).ToList();
    }

    internal static void ValidateCenitCtxAddendaCardinality(int count)
        => NachaInboundCenitValidationRules.ValidateCtxAddendaCardinality(count);

    internal static async Task<List<string>> ReadPhysicalRecordsAsync(
        Stream nachaStream,
        CancellationToken ct,
        NachaProfileRecordReader? profileReader = null)
    {
        var physicalFile = await NachaPhysicalParser.ParseAsync(nachaStream, ct, profileReader);
        return physicalFile.Records.Select(record => record.Content).ToList();
    }

    private static string ReadField(
        NachaProfileRecordReader? profileReader,
        string record,
        string recordCode,
        string fieldCode,
        string? legacyVariantCode = null,
        IReadOnlyDictionary<string, string>? selectionContext = null)
    {
        if (profileReader is not null)
        {
            return profileReader.Read(record, recordCode, fieldCode, selectionContext);
        }

        return AchColOfficialNachaLayout.Read(record, recordCode, fieldCode, legacyVariantCode);
    }

    private bool IsLocalLiveProcTransaccionesPreparationEnabled()
        => string.Equals(_configuration?["RUN_LOCAL_SOAP_PROC_TRANSACCIONES_E2E"], "true", StringComparison.OrdinalIgnoreCase)
           && string.Equals(_configuration?["ALLOW_LOCAL_MONETARY_SOAP_E2E"], "true", StringComparison.OrdinalIgnoreCase)
           && string.Equals(_configuration?["ProcTransacciones:Mode"], "Live", StringComparison.OrdinalIgnoreCase);

    private static string BuildReplayNachaId(string? originalNachaId, Guid ingestionId)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{originalNachaId}|{ingestionId:N}"))).ToLowerInvariant()[..40];

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

        foreach (var entry in validEntries.Where(e => NachaInboundTransactionCodeSets.ReturnCodes.Contains(e.TransactionCode ?? string.Empty)))
        {
            var relatedAddenda = validAddendas
                .Where(addenda => NachaInboundValidator.IsAddendaForEntry(entry, addenda)
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

        var sequenceSuffix = NachaInboundValidator.GetEntrySequenceSuffix(normalized);
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

    private static void ThrowTechnical(string message)
        => NachaInboundValidator.ThrowTechnical(message);

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

}
