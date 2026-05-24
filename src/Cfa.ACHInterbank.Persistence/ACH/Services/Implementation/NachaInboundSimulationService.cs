using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Configuration;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Cfa.ACHInterbank.Domain.Models.Configurations;
using Cfa.ACHInterbank.Persistence.DataBase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

[Scoped]
public sealed class NachaInboundSimulationService : INachaInboundSimulationService
{
    private const string UploadFlow = "NachaUpload";
    private readonly AchDbContext _context;
    private readonly NachaInboundSimulatorOptions _options;

    public NachaInboundSimulationService(AchDbContext context, IOptions<NachaInboundSimulatorOptions> options)
    {
        _context = context;
        _options = options.Value;
    }

    public async Task<GenerateNachaInboundSimulationResponse> GenerateAsync(GenerateNachaInboundSimulationRequest request, string userName, CancellationToken ct = default)
    {
        var preview = await PreviewAsync(new InboundSimulationEligibilityPreviewRequest
        {
            ClearingHouseCode = request.ClearingHouseCode,
            ScenarioType = request.ScenarioType,
            OriginFinancialInstitutionId = request.OriginFinancialInstitutionId,
            DestinationFinancialInstitutionId = request.DestinationFinancialInstitutionId,
            OriginFinancialInstitutionCode = request.OriginFinancialInstitutionCode,
            DestinationFinancialInstitutionCode = request.DestinationFinancialInstitutionCode,
            EntriesCount = request.EntriesCount,
            Amount = request.Amount,
            ReferencePrefix = request.ReferencePrefix,
            BusinessDate = request.BusinessDate,
            CycleCode = request.CycleCode,
            PendingPrenotificationReferences = request.PendingPrenotificationReferences,
            TransactionReferences = request.TransactionReferences,
            ResponseMode = request.ResponseMode,
            ReasonCode = request.ReasonCode,
            Notes = request.Notes
        }, ct);

        if (!preview.Eligible)
        {
            throw new InvalidOperationException($"{preview.FunctionalCode}: {preview.Message}");
        }

        var clearingHouse = await ResolveClearingHouseAsync(request.ClearingHouseCode, ct);
        var destination = await ResolveDestinationAsync(request.DestinationFinancialInstitutionId, request.DestinationFinancialInstitutionCode, ct);
        var origin = await ResolveOriginAsync(request.OriginFinancialInstitutionId, request.OriginFinancialInstitutionCode, ct);
        if (origin.Id == destination.Id)
        {
            throw new InvalidOperationException("ORIGIN_AND_DESTINATION_FINANCIAL_INSTITUTION_CANNOT_BE_SAME: La entidad originadora externa no puede ser la misma entidad destino/receptora.");
        }

        var existingReferences = await ResolveReferencesAsync(request, clearingHouse.Id, ct);
        var entriesCount = Math.Max(1, existingReferences.Count == 0 ? request.EntriesCount : existingReferences.Count);
        var sequence = await NextDailySequenceAsync(clearingHouse.Id, origin.Id, request.BusinessDate, ct);
        var fileId = MapSequenceToFileId(sequence);
        var fileName = $"{origin.RoutingNumber}{origin.TransitCode}.{sequence:000}.1";
        var build = BuildFile(request, clearingHouse, origin, destination, existingReferences, entriesCount, fileId);
        var bytes = Encoding.ASCII.GetBytes(build.Content);
        var sha = Convert.ToHexString(SHA256.HashData(bytes));
        var outputDirectory = ResolveOutputDirectory(clearingHouse.Code, request.ScenarioType);
        Directory.CreateDirectory(outputDirectory);
        var path = Path.Combine(outputDirectory, fileName);
        await File.WriteAllBytesAsync(path, bytes, ct);

        var simulation = new NachaInboundSimulation
        {
            SimulationId = Guid.NewGuid(),
            ClearingHouseId = clearingHouse.Id,
            ClearingHouseName = clearingHouse.Name,
            ScenarioType = request.ScenarioType,
            ResponseMode = request.ResponseMode,
            ReasonCode = request.ReasonCode,
            OriginFinancialInstitutionId = origin.Id,
            DestinationFinancialInstitutionId = destination.Id,
            EntriesCount = entriesCount,
            Amount = request.Amount,
            BusinessDate = request.BusinessDate,
            CycleCode = request.CycleCode,
            FileName = fileName,
            FilePath = path,
            Sha256 = sha,
            FileSizeBytes = bytes.LongLength,
            Status = NachaInboundSimulationStatus.Generated,
            GeneratedOnly = true,
            AutoImported = false,
            UploadRequired = true,
            ExternalTransmission = false,
            CreatedBy = string.IsNullOrWhiteSpace(userName) ? "uat-local" : userName,
            Notes = request.Notes ?? "Simulacion NACHA-M entrada UAT/local. Debe cargarse manualmente por NachaUpload."
        };

        foreach (var entry in build.Entries)
        {
            simulation.Entries.Add(entry);
        }

        var metadata = CreateMetadata(simulation, origin, destination, build, sha, fileName, bytes.LongLength);
        simulation.MetadataJson = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });

        _context.NachaInboundSimulations.Add(simulation);
        await _context.SaveChangesAsync(ct);

        await WriteEvidenceAsync(outputDirectory, simulation, metadata, build.Content, ct);

        return new GenerateNachaInboundSimulationResponse(
            simulation.SimulationId,
            simulation.Id,
            fileName,
            $"/api/uat/nacha-inbound-simulator/{simulation.Id}/file",
            $"/api/uat/nacha-inbound-simulator/{simulation.Id}/evidence",
            sha,
            bytes.LongLength,
            true,
            false,
            true,
            false,
            "Archivo NACHA-M de entrada generado. Debe cargarse manualmente por NachaUpload; no se importo ni se cambiaron estados.");
    }

    public async Task<IReadOnlyList<NachaInboundSimulationDto>> ListAsync(CancellationToken ct = default)
        => await _context.NachaInboundSimulations
            .AsNoTracking()
            .Include(x => x.OriginFinancialInstitution)
            .Include(x => x.DestinationFinancialInstitution)
            .Include(x => x.Entries)
            .OrderByDescending(x => x.CreatedAt)
            .Take(100)
            .Select(x => Map(x))
            .ToListAsync(ct);

    public async Task<NachaInboundSimulationDto?> GetAsync(int id, CancellationToken ct = default)
    {
        var entity = await _context.NachaInboundSimulations
            .AsNoTracking()
            .Include(x => x.OriginFinancialInstitution)
            .Include(x => x.DestinationFinancialInstitution)
            .Include(x => x.Entries)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        return entity is null ? null : Map(entity);
    }

    public async Task<(string FileName, string ContentType, byte[] Content)?> GetFileAsync(int id, CancellationToken ct = default)
    {
        var entity = await _context.NachaInboundSimulations.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (entity is null || string.IsNullOrWhiteSpace(entity.FilePath) || !File.Exists(entity.FilePath))
        {
            return null;
        }

        entity.Status = NachaInboundSimulationStatus.Downloaded;
        await _context.SaveChangesAsync(ct);
        return (entity.FileName, "text/plain", await File.ReadAllBytesAsync(entity.FilePath, ct));
    }

    public async Task<NachaInboundSimulationMetadataDto?> GetEvidenceAsync(int id, CancellationToken ct = default)
    {
        var entity = await _context.NachaInboundSimulations
            .AsNoTracking()
            .Include(x => x.OriginFinancialInstitution)
            .Include(x => x.DestinationFinancialInstitution)
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (entity is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(entity.MetadataJson))
        {
            return JsonSerializer.Deserialize<NachaInboundSimulationMetadataDto>(entity.MetadataJson);
        }

        return new NachaInboundSimulationMetadataDto(entity.SimulationId, entity.ClearingHouseName, entity.ScenarioType.ToString(),
            entity.ResponseMode?.ToString(), entity.ReasonCode, entity.OriginFinancialInstitution.Name,
            entity.DestinationFinancialInstitution.Name, entity.OriginFinancialInstitutionId,
            InstitutionCode(entity.OriginFinancialInstitution), entity.OriginFinancialInstitution.IsDefaultSource,
            entity.DestinationFinancialInstitutionId, InstitutionCode(entity.DestinationFinancialInstitution),
            entity.DestinationFinancialInstitution.IsDefaultSource, "FinancialInstitution.IsDefaultSource",
            entity.BusinessDate, entity.CycleCode, entity.FileName, entity.Sha256,
            entity.FileSizeBytes, string.Empty, 0, 0, string.Empty, true, false, true, UploadFlow, false, _options.Mode);
    }

    public async Task<InboundSimulationEligibilityPreviewResponse> PreviewAsync(InboundSimulationEligibilityPreviewRequest request, CancellationToken ct = default)
    {
        if (!_options.IsUatLike())
        {
            return Blocked("SIMULATOR_DISABLED", "El simulador NACHA-M de entrada esta deshabilitado o no esta en modo UAT/local.");
        }

        if (_options.AllowAutoImport || _options.AllowExternalTransmission)
        {
            return Blocked("SIMULATOR_GUARDRAIL_FAILED", "El simulador no puede autoimportar ni transmitir externamente.");
        }

        if (string.IsNullOrWhiteSpace(request.ClearingHouseCode))
        {
            return Blocked("CLEARING_HOUSE_REQUIRED", "Debe seleccionar una camara.");
        }

        var originValidation = await ValidateOriginAndDestinationAsync(request.OriginFinancialInstitutionId, request.DestinationFinancialInstitutionId, request.DestinationFinancialInstitutionCode, ct);
        if (originValidation is not null)
        {
            return originValidation;
        }

        if (request.EntriesCount < 1 || request.EntriesCount > Math.Max(1, _options.MaxEntriesPerSimulation))
        {
            return Blocked("ENTRIES_COUNT_INVALID", $"La cantidad debe estar entre 1 y {_options.MaxEntriesPerSimulation}.");
        }

        if (RequiresReason(request.ScenarioType) && string.IsNullOrWhiteSpace(request.ReasonCode))
        {
            return Blocked("TRANSACTION_REASON_CODE_REQUIRED", "El escenario requiere causal.");
        }

        var clearingHouse = await ResolveClearingHouseAsync(request.ClearingHouseCode, ct);
        if (!_options.AllowedClearingHouses.Any(x => MatchesCode(clearingHouse, x)))
        {
            return Blocked("CLEARING_HOUSE_NOT_SUPPORTED", "La camara no esta habilitada para el simulador UAT/local.");
        }

        if (request.ScenarioType == NachaInboundSimulationType.IncomingPrenotificationResponse
            && request.PendingPrenotificationReferences.Count == 0)
        {
            return Blocked("PRENOTIFICATION_NOT_FOUND", "Debe suministrar al menos una referencia de prenotificacion pendiente.");
        }

        if (RequiresTransactionReferences(request.ScenarioType) && request.TransactionReferences.Count == 0)
        {
            return Blocked("TRANSACTION_NOT_FOUND", "Debe suministrar referencias de transacciones UAT para este escenario.");
        }

        await ResolveReferencesAsync(request, clearingHouse.Id, ct);

        return new InboundSimulationEligibilityPreviewResponse(true, "ELIGIBLE",
            "Solicitud elegible. La generacion solo creara un archivo para descarga; el procesamiento real debe hacerse por NachaUpload.",
            null, true, false, true, false);
    }

    private async Task<ClearingHouse> ResolveClearingHouseAsync(string code, CancellationToken ct)
    {
        var normalized = Normalize(code);
        var clearingHouse = await _context.ClearingHouses.FirstOrDefaultAsync(x =>
            x.Code.ToUpper() == normalized
            || x.Name.ToUpper().Replace(" ", "").Replace("_", "") == normalized.Replace("_", ""), ct);
        return clearingHouse ?? throw new InvalidOperationException("CLEARING_HOUSE_NOT_SUPPORTED: Camara no encontrada.");
    }

    private async Task<InboundSimulationEligibilityPreviewResponse?> ValidateOriginAndDestinationAsync(
        int? originFinancialInstitutionId,
        int? destinationFinancialInstitutionId,
        string? destinationFinancialInstitutionCode,
        CancellationToken ct)
    {
        try
        {
            var destination = await ResolveDestinationAsync(destinationFinancialInstitutionId, destinationFinancialInstitutionCode, ct);
            var origin = await ResolveOriginAsync(originFinancialInstitutionId, null, ct);
            if (origin.Id == destination.Id)
            {
                return Blocked("ORIGIN_AND_DESTINATION_FINANCIAL_INSTITUTION_CANNOT_BE_SAME", "La originadora externa no puede ser igual a la entidad destino/receptora.");
            }

            return null;
        }
        catch (InvalidOperationException ex)
        {
            var split = ex.Message.Split(':', 2);
            return Blocked(split[0], split.Length == 2 ? split[1].Trim() : ex.Message);
        }
    }

    private async Task<FinancialInstitution> ResolveDestinationAsync(int? requestedDestinationId, string? requestedDestinationCode, CancellationToken ct)
    {
        var defaults = await _context.FinancialInstitutions
            .Where(x => x.IsDefaultSource && x.Status == FinancialInstitutionStatus.Active)
            .ToListAsync(ct);
        if (defaults.Count == 0)
        {
            throw new InvalidOperationException("DEFAULT_DESTINATION_FINANCIAL_INSTITUTION_NOT_CONFIGURED: No existe una entidad destino/receptora default activa.");
        }
        if (defaults.Count > 1)
        {
            throw new InvalidOperationException("MULTIPLE_DEFAULT_DESTINATION_FINANCIAL_INSTITUTIONS: Existe mas de una entidad destino/receptora default activa.");
        }

        var destination = defaults[0];
        if (!destination.Name.Contains("Cooperativa Financiera de Antioquia", StringComparison.OrdinalIgnoreCase)
            && !destination.Name.Contains("CFA", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("DEFAULT_DESTINATION_FINANCIAL_INSTITUTION_INVALID: La entidad default debe corresponder a CFA / Cooperativa Financiera de Antioquia.");
        }

        if (requestedDestinationId.HasValue && requestedDestinationId.Value != destination.Id)
        {
            throw new InvalidOperationException("DESTINATION_FINANCIAL_INSTITUTION_MUST_BE_DEFAULT_SOURCE: La entidad destino enviada debe coincidir con FinancialInstitution.IsDefaultSource=true.");
        }

        if (!string.IsNullOrWhiteSpace(requestedDestinationCode))
        {
            var requested = await FindInstitutionAsync(requestedDestinationCode, ct);
            if (requested is not null && requested.Id != destination.Id)
            {
                throw new InvalidOperationException("DESTINATION_FINANCIAL_INSTITUTION_MUST_BE_DEFAULT_SOURCE: La entidad destino enviada debe coincidir con FinancialInstitution.IsDefaultSource=true.");
            }
        }

        return destination;
    }

    private async Task<FinancialInstitution> ResolveOriginAsync(int? id, string? legacyCode, CancellationToken ct)
    {
        FinancialInstitution? origin = null;
        if (id.HasValue)
        {
            origin = await _context.FinancialInstitutions.FirstOrDefaultAsync(x => x.Id == id.Value, ct);
        }
        else if (!string.IsNullOrWhiteSpace(legacyCode))
        {
            origin = await FindInstitutionAsync(legacyCode, ct);
        }
        else
        {
            throw new InvalidOperationException("ORIGIN_FINANCIAL_INSTITUTION_REQUIRED: Debe seleccionar una entidad originadora externa.");
        }

        if (origin is null)
        {
            throw new InvalidOperationException("ORIGIN_FINANCIAL_INSTITUTION_NOT_FOUND: Entidad originadora externa no encontrada.");
        }

        if (origin.Status != FinancialInstitutionStatus.Active)
        {
            throw new InvalidOperationException("ORIGIN_FINANCIAL_INSTITUTION_INACTIVE: La entidad originadora externa no esta activa.");
        }

        if (origin.IsDefaultSource)
        {
            throw new InvalidOperationException("ORIGIN_FINANCIAL_INSTITUTION_CANNOT_BE_DEFAULT_SOURCE: CFA/default no puede usarse como entidad originadora externa.");
        }

        return origin;
    }

    private async Task<FinancialInstitution?> FindInstitutionAsync(string code, CancellationToken ct)
    {
        var normalized = Normalize(code);
        return await _context.FinancialInstitutions.FirstOrDefaultAsync(x =>
            x.Name.ToUpper().Replace(" ", "_").Contains(normalized)
            || (x.RoutingNumber + x.TransitCode).ToUpper() == normalized
            || x.TransitCode.ToUpper() == normalized, ct);
    }

    private async Task<List<AchTransaction>> ResolveReferencesAsync(GenerateNachaInboundSimulationRequest request, int clearingHouseId, CancellationToken ct)
    {
        var references = request.ScenarioType == NachaInboundSimulationType.IncomingPrenotificationResponse
            ? request.PendingPrenotificationReferences
            : request.TransactionReferences;

        if (references.Count == 0)
        {
            return [];
        }

        var transactions = await _context.AchTransactions
            .Include(x => x.AchCycle)
            .Where(x => references.Contains(x.Reference))
            .ToListAsync(ct);

        if (transactions.Count != references.Count)
        {
            throw new InvalidOperationException("TRANSACTION_NOT_FOUND: No se encontraron todas las referencias solicitadas.");
        }

        if (transactions.Any(x => x.AchCycle == null || x.AchCycle.ClearingHouseId != clearingHouseId))
        {
            throw new InvalidOperationException("PRENOTIFICATION_CLEARING_HOUSE_MISMATCH: La referencia no pertenece a la camara solicitada.");
        }

        if (request.ScenarioType == NachaInboundSimulationType.IncomingPrenotificationResponse
            && transactions.Any(x => !x.IsPrenotification))
        {
            throw new InvalidOperationException("PRENOTIFICATION_NOT_PENDING: La referencia no corresponde a una prenotificacion pendiente.");
        }

        return transactions;
    }

    private async Task<int> NextDailySequenceAsync(int clearingHouseId, int originId, DateOnly date, CancellationToken ct)
    {
        var count = await _context.NachaInboundSimulations.CountAsync(x =>
            x.ClearingHouseId == clearingHouseId
            && x.OriginFinancialInstitutionId == originId
            && x.BusinessDate == date, ct);
        var next = count + 1;
        if (next > 36)
        {
            throw new InvalidOperationException("DAILY_SEQUENCE_EXHAUSTED: Maximo diario de 36 archivos alcanzado.");
        }

        return next;
    }

    private FileBuildResult BuildFile(GenerateNachaInboundSimulationRequest request, ClearingHouse clearingHouse, FinancialInstitution origin,
        FinancialInstitution destination, IReadOnlyList<AchTransaction> references, int entriesCount, char fileId)
    {
        var records = new List<string>();
        var entryRecords = new List<string>();
        var addendaRecords = new List<string>();
        var effective = request.BusinessDate.ToString("yyyyMMdd");
        var amountCents = (long)Math.Round(request.Amount * 100m, 0, MidpointRounding.AwayFromZero);
        var receivingDfi = (destination.RoutingNumber + destination.TransitCode).PadLeft(8, '0')[..8];
        var originDfi = (origin.RoutingNumber + origin.TransitCode).PadLeft(8, '0')[..8];
        var entries = new List<NachaInboundSimulationEntry>();

        records.Add(Record('1',
            (4, "01"),
            (14, originDfi),
            (24, receivingDfi),
            (34, fileId.ToString()),
            (36, DateTime.UtcNow.ToString("yyMMdd")),
            (42, DateTime.UtcNow.ToString("HHmm")),
            (50, "106"),
            (54, clearingHouse.Name),
            (78, destination.Name),
            (97, "1")));

        records.Add(Record('5',
            (2, ServiceClassCode(request.ScenarioType)),
            (5, "UAT INBOUND"),
            (41, "900999999"),
            (51, "PPD"),
            (54, ScenarioDescription(request.ScenarioType)),
            (64, effective),
            (72, effective),
            (83, "1"),
            (84, originDfi),
            (99, "1")));

        for (var i = 0; i < entriesCount; i++)
        {
            var tx = references.Count > i ? references[i] : null;
            var reference = tx?.Reference ?? $"{request.ReferencePrefix}-{i + 1:000}";
            var transactionCode = TransactionCode(request.ScenarioType, request.ResponseMode);
            var trace = $"{originDfi}{i + 1:0000000}";
            var entry = Record('6',
                (2, transactionCode),
                (4, receivingDfi),
                (12, "0"),
                (13, $"0000009{i + 1:000}"),
                (30, amountCents.ToString().PadLeft(18, '0')),
                (48, "900900900"),
                (63, "CLIENTE UAT"),
                (85, "UT"),
                (87, "1"),
                (88, trace));
            var addenda = Record('7',
                (2, "05"),
                (4, $"{reference} {request.ResponseMode?.ToString() ?? "SIMULADO"} {request.ReasonCode ?? "OK"}"),
                (84, (i + 1).ToString().PadLeft(4, '0')),
                (88, (i + 1).ToString().PadLeft(7, '0')));

            entryRecords.Add(entry);
            addendaRecords.Add(addenda);
            records.Add(entry);
            records.Add(addenda);
            entries.Add(new NachaInboundSimulationEntry
            {
                Reference = reference,
                TransactionId = tx?.Id,
                PrenotificationReference = request.ScenarioType == NachaInboundSimulationType.IncomingPrenotificationResponse ? reference : null,
                AccountNumberMasked = $"****{i + 1:0000}",
                Amount = request.Amount,
                Nature = Nature(request.ScenarioType),
                PreviousStatus = tx?.State.ToString(),
                ExpectedStatusAfterUpload = ExpectedStatus(request.ScenarioType, request.ResponseMode),
                ReasonCode = request.ReasonCode,
                IsSynthetic = true
            });
        }

        var entryHashValue = entryRecords.Sum(x => long.Parse(x.Substring(3, 8))) % 10_000_000_000;
        var entryAddendaCount = entriesCount * 2;
        var debitTotal = IsDebitScenario(request.ScenarioType) ? amountCents * entriesCount : 0;
        var creditTotal = IsCreditScenario(request.ScenarioType) ? amountCents * entriesCount : 0;
        var entryHash = entryHashValue.ToString().PadLeft(10, '0');
        records.Add(Record('8',
            (2, ServiceClassCode(request.ScenarioType)),
            (5, entryAddendaCount.ToString().PadLeft(6, '0')),
            (11, entryHash),
            (21, debitTotal.ToString().PadLeft(18, '0')),
            (39, creditTotal.ToString().PadLeft(18, '0')),
            (57, "900999999"),
            (92, originDfi),
            (100, "1")));

        var blockCount = (int)Math.Ceiling((records.Count + 1) / 10m);
        records.Add(Record('9',
            (2, "1".PadLeft(6, '0')),
            (8, blockCount.ToString().PadLeft(6, '0')),
            (14, entryAddendaCount.ToString().PadLeft(8, '0')),
            (22, entryHash),
            (32, debitTotal.ToString().PadLeft(18, '0')),
            (50, creditTotal.ToString().PadLeft(18, '0'))));

        while (records.Count % 10 != 0)
        {
            records.Add(new string('9', 106));
        }

        return new FileBuildResult(string.Concat(records), entries, string.Join(", ", records.GroupBy(x => x[0]).Select(x => $"{x.Key}:{x.Count()}")),
            blockCount, entryAddendaCount, entryHash);
    }

    private static string Record(char type, params (int Start, string Value)[] fields)
    {
        var chars = Enumerable.Repeat(' ', 106).ToArray();
        chars[0] = type;
        foreach (var (start, value) in fields)
        {
            var index = start - 1;
            var source = value ?? string.Empty;
            var text = source.Length > 106 - index ? source[..(106 - index)] : source;
            for (var i = 0; i < text.Length; i++)
            {
                chars[index + i] = text[i];
            }
        }

        return new string(chars);
    }

    private static char MapSequenceToFileId(int sequence)
        => sequence switch
        {
            >= 1 and <= 26 => (char)('A' + sequence - 1),
            >= 27 and <= 36 => (char)('0' + sequence - 27),
            _ => throw new InvalidOperationException("DAILY_SEQUENCE_EXHAUSTED: Secuencia fuera de rango 001-036.")
        };

    private string ResolveOutputDirectory(string clearingHouseCode, NachaInboundSimulationType scenario)
    {
        var chamber = clearingHouseCode.Contains("CENIT", StringComparison.OrdinalIgnoreCase) ? "cenit" : "ach-colombia";
        var bucket = scenario == NachaInboundSimulationType.IncomingPrenotificationResponse
            ? $"prenotification-responses/{chamber}"
            : (RequiresTransactionReferences(scenario) ? $"transaction-responses/{chamber}" : chamber);
        return Path.GetFullPath(Path.Combine(_options.OutputDirectory, bucket));
    }

    private static NachaInboundSimulationMetadataDto CreateMetadata(NachaInboundSimulation simulation, FinancialInstitution origin,
        FinancialInstitution destination, FileBuildResult build, string sha, string fileName, long size)
        => new(simulation.SimulationId, simulation.ClearingHouseName, simulation.ScenarioType.ToString(), simulation.ResponseMode?.ToString(),
            simulation.ReasonCode, origin.Name, destination.Name, origin.Id, InstitutionCode(origin), origin.IsDefaultSource,
            destination.Id, InstitutionCode(destination), destination.IsDefaultSource, "FinancialInstitution.IsDefaultSource",
            simulation.BusinessDate, simulation.CycleCode, fileName, sha, size,
            build.RecordsDetected, build.BlockCount, build.EntryAddendaCount, build.EntryHash, true, false, true, UploadFlow, false, "UAT");

    private static async Task WriteEvidenceAsync(string directory, NachaInboundSimulation simulation, NachaInboundSimulationMetadataDto metadata, string content, CancellationToken ct)
    {
        await File.WriteAllTextAsync(Path.Combine(directory, "metadata.json"), JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }), ct);
        await File.WriteAllTextAsync(Path.Combine(directory, "validation_report.md"),
            $"# Simulador NACHA-M Entrada\n\n- SimulationId: `{simulation.SimulationId}`\n- Archivo: `{simulation.FileName}`\n- SHA256: `{simulation.Sha256}`\n- generatedOnly: true\n- autoImported: false\n- uploadRequired: true\n- externalTransmission: false\n\nEl archivo fue generado por el simulador UAT/local y debe cargarse manualmente por NachaUpload.\n", ct);
        await File.WriteAllTextAsync(Path.Combine(directory, "README.md"),
            "# Evidencia simulador NACHA-M entrada\n\nArchivo generado con datos sinteticos para UAT/local. No se importa automaticamente, no crea transacciones, no cambia estados y no transmite externamente.\n", ct);
        await File.WriteAllTextAsync(Path.Combine(directory, "expected_after_upload.json"),
            JsonSerializer.Serialize(simulation.Entries.Select(x => new { x.Reference, x.ExpectedStatusAfterUpload, x.ReasonCode }), new JsonSerializerOptions { WriteIndented = true }), ct);
    }

    private static NachaInboundSimulationDto Map(NachaInboundSimulation x)
        => new(x.Id, x.SimulationId, x.ClearingHouseName, x.ScenarioType, x.ResponseMode, x.ReasonCode,
            x.OriginFinancialInstitution.Name, x.DestinationFinancialInstitution.Name, x.OriginFinancialInstitutionId,
            x.DestinationFinancialInstitutionId, x.EntriesCount, x.Amount, x.BusinessDate,
            x.CycleCode, x.FileName, x.Sha256, x.FileSizeBytes, x.Status.ToString(), x.GeneratedOnly, x.AutoImported,
            x.UploadRequired, x.ExternalTransmission, x.CreatedAt, x.Entries.Select(e => new NachaInboundSimulationEntryDto(e.Id,
                e.Reference, e.TransactionId, e.PrenotificationReference, e.AccountNumberMasked, e.Amount, e.Nature,
                e.PreviousStatus, e.ExpectedStatusAfterUpload, e.ReasonCode, e.IsSynthetic)).ToList());

    private static bool MatchesCode(ClearingHouse clearingHouse, string code)
        => Normalize(clearingHouse.Code) == Normalize(code) || Normalize(clearingHouse.Name) == Normalize(code);
    private static string InstitutionCode(FinancialInstitution institution) => $"{institution.RoutingNumber}{institution.TransitCode}";
    private static string Normalize(string value) => value.Trim().ToUpperInvariant().Replace(" ", "_").Replace("-", "_");
    private static InboundSimulationEligibilityPreviewResponse Blocked(string code, string message)
        => new(false, "BLOCKED", message, code, true, false, true, false);
    private static bool RequiresReason(NachaInboundSimulationType scenario)
        => scenario.ToString().Contains("Rejection", StringComparison.OrdinalIgnoreCase) || scenario.ToString().Contains("Return", StringComparison.OrdinalIgnoreCase);
    private static bool RequiresTransactionReferences(NachaInboundSimulationType scenario)
        => scenario is NachaInboundSimulationType.IncomingCreditConfirmation or NachaInboundSimulationType.IncomingCreditRejection
            or NachaInboundSimulationType.IncomingCreditReturn or NachaInboundSimulationType.IncomingDebitConfirmation
            or NachaInboundSimulationType.IncomingDebitRejection or NachaInboundSimulationType.IncomingDebitReturn;
    private static bool IsDebitScenario(NachaInboundSimulationType scenario) => scenario.ToString().Contains("Debit", StringComparison.OrdinalIgnoreCase);
    private static bool IsCreditScenario(NachaInboundSimulationType scenario) => scenario.ToString().Contains("Credit", StringComparison.OrdinalIgnoreCase);
    private static string Nature(NachaInboundSimulationType scenario) => IsDebitScenario(scenario) ? "Debit" : "Credit";
    private static string TransactionCode(NachaInboundSimulationType scenario, InboundResponseMode? mode)
        => scenario == NachaInboundSimulationType.IncomingPrenotificationResponse ? "28" : IsDebitScenario(scenario) ? "27" : "22";
    private static string ServiceClassCode(NachaInboundSimulationType scenario) => IsDebitScenario(scenario) ? "225" : "220";
    private static string ScenarioDescription(NachaInboundSimulationType scenario)
    {
        var value = scenario.ToString().Replace("Incoming", "").ToUpperInvariant();
        return value[..Math.Min(10, value.Length)];
    }
    private static string ExpectedStatus(NachaInboundSimulationType scenario, InboundResponseMode? mode)
        => scenario == NachaInboundSimulationType.IncomingPrenotificationResponse
            ? $"Prenotificacion {mode?.ToString() ?? "simulada"} tras carga NachaUpload"
            : $"Respuesta {scenario} aplicada tras carga NachaUpload";

    private sealed record FileBuildResult(string Content, IReadOnlyList<NachaInboundSimulationEntry> Entries, string RecordsDetected, int BlockCount, int EntryAddendaCount, string EntryHash);
}
