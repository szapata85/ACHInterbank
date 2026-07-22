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
            SimulationMode = request.SimulationMode,
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
        await EnsureCycleAvailableAsync(request.CycleCode, clearingHouse.Id, request.BusinessDate, request.ScenarioType, ct);
        var destination = await ResolveDestinationAsync(request.DestinationFinancialInstitutionId, request.DestinationFinancialInstitutionCode, ct);
        var origin = await ResolveOriginAsync(request.OriginFinancialInstitutionId, request.OriginFinancialInstitutionCode, ct);
        if (origin.Id == destination.Id)
        {
            throw new InvalidOperationException("ORIGIN_AND_DESTINATION_FINANCIAL_INSTITUTION_CANNOT_BE_SAME: La entidad originadora externa no puede ser la misma entidad destino/receptora.");
        }

        var existingReferences = await ResolveReferencesAsync(request, clearingHouse.Id, ct);
        if (request.SimulationMode == NachaSimulationMode.IncomingTransactions)
        {
            existingReferences = await LoadCycleTransactionsAsync(
                request.CycleCode,
                request.ScenarioType,
                request.EntriesCount,
                ct);
        }
        var entriesCount = Math.Max(1, existingReferences.Count == 0 ? request.EntriesCount : existingReferences.Count);
        var sequence = await NextDailySequenceAsync(clearingHouse.Id, origin.Id, request.BusinessDate, ct);
        var fileId = MapSequenceToFileId(sequence);
        var fileName = BuildExternalFileName(clearingHouse, origin, request.BusinessDate, sequence);
        var build = BuildFile(request, clearingHouse, origin, destination, existingReferences, entriesCount, sequence, fileId);
        var bytes = Encoding.ASCII.GetBytes(build.Content);
        var sha = Convert.ToHexString(SHA256.HashData(bytes));
        var executionId = Guid.NewGuid();
        var outputDirectory = Path.Combine(
            ResolveOutputDirectory(clearingHouse.Code, request.ScenarioType),
            executionId.ToString("N"));
        Directory.CreateDirectory(outputDirectory);
        var path = Path.Combine(outputDirectory, fileName);
        await File.WriteAllBytesAsync(path, bytes, ct);

        var simulation = new NachaInboundSimulation
        {
            SimulationId = executionId,
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
            entity.FileSizeBytes, string.Empty, 0, 0, string.Empty, true, false, true, UploadFlow, false, _options.Mode)
        {
            SimulationMode = InferSimulationMode(entity.ScenarioType),
            Environment = _options.Mode
        };
    }

    public async Task<InboundSimulationEligibilityPreviewResponse> PreviewAsync(InboundSimulationEligibilityPreviewRequest request, CancellationToken ct = default)
    {
        if (!_options.IsUatLike())
        {
            return Blocked("SIMULATOR_DISABLED", "El simulador NACHA-M está deshabilitado o no está en modo UAT/local.", request.SimulationMode);
        }

        if (_options.AllowAutoImport || _options.AllowExternalTransmission)
        {
            return Blocked("SIMULATOR_GUARDRAIL_FAILED", "El simulador no puede autoimportar ni transmitir externamente.", request.SimulationMode);
        }

        if (request.SimulationMode == NachaSimulationMode.IncomingTransactions
            && RequiresTransactionReferences(request.ScenarioType))
        {
            return Blocked("SIMULATION_MODE_SCENARIO_MISMATCH",
                "El escenario seleccionado corresponde a una respuesta diferencial, no a una transacción entrante nueva.",
                request.SimulationMode);
        }

        if (request.SimulationMode == NachaSimulationMode.DifferentialResponses
            && !RequiresTransactionReferences(request.ScenarioType)
            && request.ScenarioType != NachaInboundSimulationType.IncomingPrenotificationResponse)
        {
            return Blocked("SIMULATION_MODE_SCENARIO_MISMATCH",
                "El escenario seleccionado crea una transacción nueva y no puede usarse como respuesta diferencial.",
                request.SimulationMode);
        }

        if (request.SimulationMode == NachaSimulationMode.DifferentialResponses
            && !_options.DifferentialResponsesEnabled)
        {
            return Blocked("DIFFERENTIAL_RESPONSES_DISABLED",
                "El modo Respuestas diferenciales está deshabilitado por configuración.",
                request.SimulationMode);
        }

        if (string.IsNullOrWhiteSpace(request.ClearingHouseCode))
        {
            return Blocked("CLEARING_HOUSE_REQUIRED", "Debe seleccionar una cámara.", request.SimulationMode);
        }

        var originValidation = await ValidateOriginAndDestinationAsync(request.OriginFinancialInstitutionId, request.DestinationFinancialInstitutionId, request.DestinationFinancialInstitutionCode, ct);
        if (originValidation is not null)
        {
            return originValidation with { SimulationMode = request.SimulationMode };
        }

        if (request.EntriesCount < 1 || request.EntriesCount > Math.Max(1, _options.MaxEntriesPerSimulation))
        {
            return Blocked("ENTRIES_COUNT_INVALID", $"La cantidad debe estar entre 1 y {_options.MaxEntriesPerSimulation}.", request.SimulationMode);
        }

        if (RequiresReason(request.ScenarioType) && string.IsNullOrWhiteSpace(request.ReasonCode))
        {
            return Blocked("TRANSACTION_REASON_CODE_REQUIRED", "El escenario requiere causal.", request.SimulationMode);
        }

        var clearingHouse = await ResolveClearingHouseAsync(request.ClearingHouseCode, ct);
        if (!_options.AllowedClearingHouses.Any(x => MatchesCode(clearingHouse, x)))
        {
            return Blocked("CLEARING_HOUSE_NOT_SUPPORTED", "La cámara no está habilitada para el simulador UAT/local.", request.SimulationMode);
        }

        try
        {
            await EnsureCycleAvailableAsync(
                request.CycleCode,
                clearingHouse.Id,
                request.BusinessDate,
                request.ScenarioType,
                ct);
        }
        catch (InvalidOperationException ex)
        {
            var split = ex.Message.Split(':', 2);
            return Blocked(
                split[0],
                split.Length == 2 ? split[1].Trim() : ex.Message,
                request.SimulationMode);
        }

        if (request.ScenarioType == NachaInboundSimulationType.IncomingPrenotificationResponse
            && request.PendingPrenotificationReferences.Count == 0)
        {
            return Blocked("PRENOTIFICATION_NOT_FOUND", "Debe suministrar al menos una referencia de prenotificación pendiente.", request.SimulationMode);
        }

        if (RequiresTransactionReferences(request.ScenarioType) && request.TransactionReferences.Count == 0)
        {
            return Blocked("TRANSACTION_NOT_FOUND", "Debe suministrar referencias de transacciones UAT para este escenario.", request.SimulationMode);
        }

        try
        {
            await ResolveReferencesAsync(request, clearingHouse.Id, ct);
        }
        catch (InvalidOperationException ex)
        {
            var split = ex.Message.Split(':', 2);
            return Blocked(
                split[0],
                split.Length == 2 ? split[1].Trim() : ex.Message,
                request.SimulationMode);
        }

        if (request.SimulationMode == NachaSimulationMode.DifferentialResponses)
        {
            var profile = await ResolvePublishedDifferentialProfileAsync(clearingHouse, request.BusinessDate, ct);
            if (profile is null)
            {
                return Blocked("DIFFERENTIAL_PROFILE_NOT_PUBLISHED",
                    "No existe un perfil NACHA-M publicado y vigente para RETORNO/ENTRADA en la cámara seleccionada. La generación permanece bloqueada para no inventar una regla financiera.",
                    request.SimulationMode);
            }

            return Blocked("DIFFERENTIAL_GENERATOR_NOT_HOMOLOGATED",
                $"El perfil {profile} existe, pero el generador diferencial aún no tiene homologación demostrada. No se generará un archivo con semántica ambigua.",
                request.SimulationMode);
        }

        return new InboundSimulationEligibilityPreviewResponse(true, "ELIGIBLE",
            "Solicitud elegible. La generacion solo creara un archivo para descarga; el procesamiento real debe hacerse por NachaUpload.",
            null, request.SimulationMode, true, false, true, false);
    }

    public async Task<DifferentialResponseTransactionPage> ListEligibleDifferentialTransactionsAsync(
        DifferentialResponseTransactionQuery request,
        CancellationToken ct = default)
    {
        var clearingHouse = await ResolveClearingHouseAsync(request.ClearingHouseCode, ct);
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var query = _context.AchTransactions
            .AsNoTracking()
            .Where(x => x.SourceInstitution.IsDefaultSource
                && !x.DestinationInstitution.IsDefaultSource
                && x.AchCycle.ClearingHouseId == clearingHouse.Id);

        if (request.DestinationFinancialInstitutionId.HasValue)
        {
            query = query.Where(x => x.DestinationInstitutionId == request.DestinationFinancialInstitutionId.Value);
        }

        if (request.FromDate.HasValue)
        {
            var from = request.FromDate.Value.ToDateTime(TimeOnly.MinValue);
            query = query.Where(x => x.EffectiveEntryDate >= from);
        }

        if (request.ToDate.HasValue)
        {
            var toExclusive = request.ToDate.Value.AddDays(1).ToDateTime(TimeOnly.MinValue);
            query = query.Where(x => x.EffectiveEntryDate < toExclusive);
        }

        if (Enum.TryParse<AchTransferStateEnum>(request.State, true, out var state))
        {
            query = query.Where(x => x.State == state);
        }

        if (Enum.TryParse<TransactionTypeEnum>(request.TransactionType, true, out var transactionType))
        {
            query = query.Where(x => x.Type == transactionType);
        }

        if (!string.IsNullOrWhiteSpace(request.TraceNumber))
        {
            var trace = request.TraceNumber.Trim();
            query = query.Where(x => x.TraceNumber == trace);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(x => x.TransactionExternalId.Contains(search)
                || x.Reference.Contains(search)
                || x.TraceNumber.Contains(search));
        }

        var total = await query.CountAsync(ct);
        var rows = await query
            .OrderByDescending(x => x.EffectiveEntryDate)
            .ThenByDescending(x => x.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.TransactionExternalId,
                x.Reference,
                x.TraceNumber,
                ClearingHouse = x.AchCycle.ClearingHouse!.Name,
                x.DestinationInstitutionId,
                DestinationInstitution = x.DestinationInstitution.Name,
                TransactionType = x.Type.ToString(),
                x.EffectiveEntryDate,
                Cycle = x.AchCycle.CycleName,
                x.Amount,
                State = x.State.ToString()
            })
            .ToListAsync(ct);

        var correlationKeys = rows
            .SelectMany(x => new[] { x.TransactionExternalId, x.Reference, x.TraceNumber })
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        var priorResponses = correlationKeys.Length == 0
            ? new HashSet<string>(StringComparer.Ordinal)
            : (await _context.AchResponses
                .AsNoTracking()
                .Where(x => correlationKeys.Contains(x.IdTransaccion))
                .Select(x => x.IdTransaccion)
                .Distinct()
                .ToListAsync(ct))
                .ToHashSet(StringComparer.Ordinal);

        var items = rows.Select(x =>
        {
            var hasPriorResponse = priorResponses.Contains(x.TransactionExternalId)
                || priorResponses.Contains(x.Reference)
                || priorResponses.Contains(x.TraceNumber);
            var correlationComplete = !string.IsNullOrWhiteSpace(x.TraceNumber)
                && !string.IsNullOrWhiteSpace(x.TransactionExternalId);
            var pending = string.Equals(x.State, AchTransferStateEnum.Pending.ToString(), StringComparison.Ordinal);
            var eligible = pending && correlationComplete && !hasPriorResponse;
            var reason = !pending
                ? "La operación ya no está pendiente."
                : !correlationComplete
                    ? "Faltan trace number o identificador de correlación."
                    : hasPriorResponse
                        ? "La operación ya tiene una respuesta persistida."
                        : null;

            return new DifferentialResponseEligibleTransactionDto(
                x.Id,
                string.IsNullOrWhiteSpace(x.TransactionExternalId) ? x.Reference : x.TransactionExternalId,
                x.TraceNumber,
                x.ClearingHouse,
                x.DestinationInstitutionId,
                x.DestinationInstitution,
                x.TransactionType,
                x.EffectiveEntryDate,
                x.Cycle,
                x.Amount,
                x.State,
                hasPriorResponse,
                eligible,
                reason);
        }).ToList();

        return new DifferentialResponseTransactionPage(items, page, pageSize, total);
    }

    public async Task<IReadOnlyList<AvailableInboundCycleDto>> ListAvailableCyclesAsync(
        AvailableInboundCycleQuery request,
        CancellationToken ct = default)
    {
        var clearingHouse = await ResolveClearingHouseAsync(request.ClearingHouseCode, ct);
        if (!_options.AllowedClearingHouses.Any(x => MatchesCode(clearingHouse, x)))
        {
            return [];
        }

        var transactionType = ScenarioTransactionType(request.ScenarioType);
        var rows = await AvailableCycleQuery(
            clearingHouse.Id,
            request.ProcessingDate,
            request.ScenarioType)
            .Select(x => new
            {
                x.Id,
                x.CycleName,
                x.ClearingHouseId,
                ClearingHouseCode = x.ClearingHouse!.Code,
                ClearingHouseName = x.ClearingHouse.Name,
                x.ProcessingDate,
                x.StartTime,
                TransactionCount = x.Transactions.Count(t =>
                    NachaExportEligibility.ExportableStates.Contains(t.State)
                    && t.Type == transactionType)
            })
            .ToListAsync(ct);

        return rows
            .OrderBy(x => x.ProcessingDate)
            .ThenBy(x => x.StartTime)
            .ThenBy(x => x.CycleName)
            .Select(x => new AvailableInboundCycleDto(
                x.Id,
                x.Id,
                x.CycleName,
                x.ClearingHouseId,
                x.ClearingHouseCode,
                x.ClearingHouseName,
                DateOnly.FromDateTime(x.ProcessingDate),
                x.TransactionCount,
                "Disponible"))
            .ToList();
    }

    private IQueryable<AchCycle> AvailableCycleQuery(
        int clearingHouseId,
        DateOnly processingDate,
        NachaInboundSimulationType scenarioType)
    {
        var dayStart = processingDate.ToDateTime(TimeOnly.MinValue);
        var dayEnd = processingDate.AddDays(1).ToDateTime(TimeOnly.MinValue);
        var transactionType = ScenarioTransactionType(scenarioType);

        return _context.AchCycles
            .AsNoTracking()
            .Where(x => x.ClearingHouseId == clearingHouseId
                && x.ClearingHouse != null
                && x.ClearingHouse.IsActive
                && x.ProcessingDate >= dayStart
                && x.ProcessingDate < dayEnd
                && x.ClearingHouseCycleConfigId.HasValue
                && x.ClearingHouseCycleConfig != null
                && x.ClearingHouseCycleConfig.ClearingHouseId == x.ClearingHouseId
                && x.ClearingHouseCycleConfig.IsActive
                && x.ClearingHouseCycleConfig.EffectiveFrom < dayEnd
                && (!x.ClearingHouseCycleConfig.EffectiveTo.HasValue
                    || x.ClearingHouseCycleConfig.EffectiveTo.Value >= dayStart)
                && x.ClearingHouseCycleConfig.StartTime == x.StartTime
                && x.ClearingHouseCycleConfig.EndTime == x.EndTime
                && x.ClearingHouseCycleConfig.CutoffTime == x.CutoffTime
                && x.Transactions.Any(t =>
                    NachaExportEligibility.ExportableStates.Contains(t.State)
                    && t.Type == transactionType));
    }

    private async Task EnsureCycleAvailableAsync(
        string cycleCode,
        int clearingHouseId,
        DateOnly processingDate,
        NachaInboundSimulationType scenarioType,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(cycleCode))
        {
            throw new InvalidOperationException("CYCLE_REQUIRED: Debe seleccionar un ciclo disponible.");
        }

        var normalizedCode = cycleCode.Trim();
        var available = await AvailableCycleQuery(clearingHouseId, processingDate, scenarioType)
            .AnyAsync(x => x.Id == normalizedCode, ct);

        if (!available)
        {
            throw new InvalidOperationException(
                "CYCLE_NOT_AVAILABLE: El ciclo no existe, no está vigente o no tiene transacciones elegibles para la cámara y fecha seleccionadas.");
        }
    }

    private async Task<List<AchTransaction>> LoadCycleTransactionsAsync(
        string cycleCode,
        NachaInboundSimulationType scenarioType,
        int requestedCount,
        CancellationToken ct)
    {
        var transactionType = ScenarioTransactionType(scenarioType);
        var transactions = await _context.AchTransactions
            .AsNoTracking()
            .Where(x => x.AchCycleId == cycleCode.Trim()
                && NachaExportEligibility.ExportableStates.Contains(x.State)
                && x.Type == transactionType)
            .OrderBy(x => x.Id)
            .Take(requestedCount)
            .ToListAsync(ct);

        if (transactions.Count < requestedCount)
        {
            throw new InvalidOperationException(
                $"ENTRIES_COUNT_EXCEEDS_AVAILABLE: El ciclo solo tiene {transactions.Count} transacciones elegibles para la simulación solicitada.");
        }

        return transactions;
    }

    private static TransactionTypeEnum ScenarioTransactionType(NachaInboundSimulationType scenario)
        => scenario == NachaInboundSimulationType.IncomingPrenotificationResponse
            ? TransactionTypeEnum.Prenotification
            : IsDebitScenario(scenario) ? TransactionTypeEnum.Debit : TransactionTypeEnum.Credit;

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
            .Include(x => x.SourceInstitution)
            .Include(x => x.DestinationInstitution)
            .Where(x => references.Contains(x.Reference)
                || references.Contains(x.TransactionExternalId)
                || references.Contains(x.TraceNumber))
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

        if (request.SimulationMode == NachaSimulationMode.DifferentialResponses)
        {
            if (!request.OriginFinancialInstitutionId.HasValue)
            {
                throw new InvalidOperationException("RESPONDING_FINANCIAL_INSTITUTION_REQUIRED: Debe seleccionar el banco destino que responde.");
            }

            if (transactions.Any(x => !x.SourceInstitution.IsDefaultSource))
            {
                throw new InvalidOperationException("DIFFERENTIAL_SOURCE_MUST_BE_CFA: Las respuestas diferenciales solo pueden partir de operaciones originadas por CFA.");
            }

            if (transactions.Any(x => x.DestinationInstitutionId != request.OriginFinancialInstitutionId.Value
                                      || x.DestinationInstitution.IsDefaultSource))
            {
                throw new InvalidOperationException("RESPONDING_BANK_MISMATCH: El banco que responde debe ser el destino externo de cada operación original.");
            }

            if (transactions.Any(x => x.State is AchTransferStateEnum.ReturnedByOperator or AchTransferStateEnum.ReturnedByEpr))
            {
                throw new InvalidOperationException("DIFFERENTIAL_TRANSACTION_FINAL_STATE: Una o más operaciones ya están finalizadas con una devolución incompatible.");
            }

            if (transactions.Any(x => string.IsNullOrWhiteSpace(x.TraceNumber)
                                      || string.IsNullOrWhiteSpace(x.TransactionExternalId)))
            {
                throw new InvalidOperationException("DIFFERENTIAL_CORRELATION_DATA_MISSING: La operación no conserva trace number e identificador suficientes para correlación determinística.");
            }

            if (transactions.Select(x => x.TraceNumber.Trim()).Distinct(StringComparer.Ordinal).Count() != transactions.Count)
            {
                throw new InvalidOperationException("DIFFERENTIAL_TRACE_NOT_UNIQUE: Los trace numbers seleccionados no son únicos.");
            }
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
        FinancialInstitution destination, IReadOnlyList<AchTransaction> references, int entriesCount, int fileSequence, char fileId)
    {
        var records = new List<string>();
        var entryRecords = new List<string>();
        var addendaRecords = new List<string>();
        var effective = request.BusinessDate.ToString("yyyyMMdd");
        var receivingDfi = (destination.RoutingNumber + destination.TransitCode).PadLeft(8, '0')[..8];
        var originDfi = (origin.RoutingNumber + origin.TransitCode).PadLeft(8, '0')[..8];
        var entries = new List<NachaInboundSimulationEntry>();
        long debitTotal = 0;
        long creditTotal = 0;

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
            var entryAmount = tx?.Amount ?? request.Amount;
            var amountCents = (long)Math.Round(entryAmount * 100m, 0, MidpointRounding.AwayFromZero);
            var reference = tx?.Reference ?? $"{request.ReferencePrefix}-{i + 1:000}";
            var transactionCode = TransactionCode(request.ScenarioType, request.ResponseMode);
            var traceSequence = ((fileSequence - 1) * Math.Max(1, _options.MaxEntriesPerSimulation)) + i + 1;
            var trace = $"{originDfi}{traceSequence:0000000}";
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
                Amount = entryAmount,
                Nature = Nature(request.ScenarioType),
                PreviousStatus = tx?.State.ToString(),
                ExpectedStatusAfterUpload = ExpectedStatus(request.ScenarioType, request.ResponseMode),
                ReasonCode = request.ReasonCode,
                IsSynthetic = tx is null
            });
            if (IsDebitScenario(request.ScenarioType)) debitTotal += amountCents;
            if (IsCreditScenario(request.ScenarioType)) creditTotal += amountCents;
        }

        var entryHashValue = entryRecords.Sum(x => long.Parse(x.Substring(3, 8))) % 10_000_000_000;
        var entryAddendaCount = entriesCount * 2;
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

    private static string BuildExternalFileName(
        ClearingHouse clearingHouse,
        FinancialInstitution origin,
        DateOnly businessDate,
        int sequence)
    {
        var routingDigits = new string($"{origin.RoutingNumber}{origin.TransitCode}".Where(char.IsDigit).ToArray())
            .PadLeft(7, '0');
        var participant = routingDigits[..7];
        var baseName = $"{participant}.{sequence:000}.{businessDate:yyyyMMdd}.{sequence}";
        return clearingHouse.Code.Contains("CENIT", StringComparison.OrdinalIgnoreCase)
            ? baseName
            : $"{baseName}.OUT";
    }

    private async Task<string?> ResolvePublishedDifferentialProfileAsync(
        ClearingHouse clearingHouse,
        DateOnly businessDate,
        CancellationToken ct)
    {
        if (!_options.RequirePublishedDifferentialProfile)
        {
            return "PROFILE_CHECK_EXPLICITLY_DISABLED";
        }

        var effectiveAt = businessDate.ToDateTime(TimeOnly.MinValue);
        var clearingHouseCodes = clearingHouse.Code.Contains("CENIT", StringComparison.OrdinalIgnoreCase)
            ? new[] { "CENIT" }
            : new[] { "ACH", "ACHCOL" };

        return await _context.CfgProfiles
            .AsNoTracking()
            .Where(x => clearingHouseCodes.Contains(x.ClearingHouse.Code)
                && x.FlowType.Code == "RETORNO"
                && x.Direction.Code == "ENTRADA"
                && x.Status.Code == "PUBLICADO"
                && x.EffectiveFrom <= effectiveAt
                && (!x.EffectiveTo.HasValue || x.EffectiveTo.Value >= effectiveAt))
            .OrderByDescending(x => x.ContextPriority)
            .ThenByDescending(x => x.VersionMajor)
            .ThenByDescending(x => x.VersionMinor)
            .Select(x => x.ProfileCode)
            .FirstOrDefaultAsync(ct);
    }

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
        => new NachaInboundSimulationMetadataDto(simulation.SimulationId, simulation.ClearingHouseName, simulation.ScenarioType.ToString(), simulation.ResponseMode?.ToString(),
            simulation.ReasonCode, origin.Name, destination.Name, origin.Id, InstitutionCode(origin), origin.IsDefaultSource,
            destination.Id, InstitutionCode(destination), destination.IsDefaultSource, "FinancialInstitution.IsDefaultSource",
            simulation.BusinessDate, simulation.CycleCode, fileName, sha, size,
            build.RecordsDetected, build.BlockCount, build.EntryAddendaCount, build.EntryHash, true, false, true, UploadFlow, false, "UAT")
        {
            SimulationMode = InferSimulationMode(simulation.ScenarioType),
            Environment = "UAT"
        };

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
    private static InboundSimulationEligibilityPreviewResponse Blocked(
        string code,
        string message,
        NachaSimulationMode simulationMode = NachaSimulationMode.IncomingTransactions)
        => new(false, "BLOCKED", message, code, simulationMode, true, false, true, false);
    private static bool RequiresReason(NachaInboundSimulationType scenario)
        => scenario.ToString().Contains("Rejection", StringComparison.OrdinalIgnoreCase) || scenario.ToString().Contains("Return", StringComparison.OrdinalIgnoreCase);
    private static bool RequiresTransactionReferences(NachaInboundSimulationType scenario)
        => scenario is NachaInboundSimulationType.IncomingCreditConfirmation or NachaInboundSimulationType.IncomingCreditRejection
            or NachaInboundSimulationType.IncomingCreditReturn or NachaInboundSimulationType.IncomingDebitConfirmation
            or NachaInboundSimulationType.IncomingDebitRejection or NachaInboundSimulationType.IncomingDebitReturn;
    private static NachaSimulationMode InferSimulationMode(NachaInboundSimulationType scenario)
        => RequiresTransactionReferences(scenario) || scenario == NachaInboundSimulationType.IncomingPrenotificationResponse
            ? NachaSimulationMode.DifferentialResponses
            : NachaSimulationMode.IncomingTransactions;
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
