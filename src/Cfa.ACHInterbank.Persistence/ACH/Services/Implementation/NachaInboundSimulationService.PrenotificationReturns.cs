using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.ACH.Services;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Cfa.ACHInterbank.Domain.Models.ACH.Enums;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Persistence.ACH.Services.Implementation;

/// <summary>
/// I03: solo genera un archivo de devolucion simulado. Toda la reserva en BD,
/// revalidacion y asignacion de trace se ejecutan dentro de la transaccion.
/// El archivo fisico se elimina si no se confirma la transaccion. No autoimporta.
/// </summary>
public sealed partial class NachaInboundSimulationService
{
    public async Task<GenerateNachaInboundSimulationResponse> GeneratePrenotificationReturnAsync(
        PrenotificationSimulationContractRequest request, string userName, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var shape = PrenotificationSimulationContract.Validate(request);
        if (!shape.ShapeValid)
            throw new ArgumentException($"PRENOTIFICATION_CONTRACT_INVALID: {string.Join("; ", shape.Violations.Select(v => v.Code))}");
        if (request.Outcome != PrenotificationSimulationOutcome.Returned)
            throw new InvalidOperationException("NO_RETURN_HAS_NO_NACHA_FILE: La aceptacion tacita se verificara en I05, no genera archivo.");
        if (!_options.IsUatLike() || !_options.DifferentialResponsesEnabled || !_options.RequirePublishedDifferentialProfile
            || _options.AllowAutoImport || _options.AllowExternalTransmission || _nachaFileBuilder is null)
            throw new InvalidOperationException("SIMULATOR_UNAVAILABLE: UAT/local, guardas y generador oficial son obligatorios.");
        if (request.OriginalPrenotificationIds.Count > _options.MaxEntriesPerSimulation)
            throw new InvalidOperationException("ENTRIES_COUNT_INVALID: La seleccion supera el maximo de la configuracion UAT.");

        var reservationId = PrenotificationSimulationIdentity.ReservationId(request);
        var requestHash = PrenotificationSimulationIdentity.RequestHash(request);
        var previous = await _context.NachaInboundSimulations.AsNoTracking()
            .FirstOrDefaultAsync(x => x.SimulationId == reservationId, ct);
        if (previous is not null)
            return ToPrenotificationReturnReplay(previous, requestHash);

        // SERIALIZABLE protege la consulta de devoluciones generadas para los IDs
        // seleccionados. UNIQUE(SimulationId) protege reintentos con igual clave
        // incluso en varias instancias. No reserva una clave en memoria local.
        await using var tx = await _context.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        string? outputDirectory = null;
        try
        {
            previous = await _context.NachaInboundSimulations
                .FirstOrDefaultAsync(x => x.SimulationId == reservationId, ct);
            if (previous is not null)
                return ToPrenotificationReturnReplay(previous, requestHash);

            var preview = await PreviewPrenotificationAsync(request, ct);
            if (!preview.ShapeValid || !preview.BusinessEligible || preview.Candidates.Count != request.OriginalPrenotificationIds.Count)
                throw new InvalidOperationException($"PRENOTIFICATION_NOT_ELIGIBLE: {string.Join("; ", preview.Violations.Select(v => v.Code))}");

            var ids = request.OriginalPrenotificationIds.ToArray();
            var priorFile = await _context.NachaInboundSimulationEntries.AsNoTracking()
                .AnyAsync(x => x.TransactionId.HasValue && ids.Contains(x.TransactionId.Value)
                    && x.Simulation.ScenarioType == NachaInboundSimulationType.IncomingPrenotificationReturn
                    && x.Simulation.Status != NachaInboundSimulationStatus.Failed
                    && x.Simulation.Status != NachaInboundSimulationStatus.Blocked, ct);
            if (priorFile)
                throw new InvalidOperationException("PRENOTIFICATION_RETURN_ALREADY_GENERATED: Ya existe devolucion simulada para al menos un ID seleccionado.");

            var clearingHouse = await ResolveClearingHouseAsync(request.ClearingHouseCode, ct);
            var respondingInstitution = await ResolveOriginAsync(request.RespondingFinancialInstitutionId, null, ct);
            var localReceivingInstitution = await ResolveDestinationAsync(null, null, ct);
            if (respondingInstitution.Id == localReceivingInstitution.Id)
                throw new InvalidOperationException("PRENOTIFICATION_BANK_MISMATCH: Banco receptor original y origen del retorno deben ser externos.");
            var responseCycle = await _context.AchCycles.AsNoTracking()
                .SingleAsync(x => x.Id == request.ResponseCycleId && x.ClearingHouseId == clearingHouse.Id, ct);
            var originals = await _context.AchTransactions.AsNoTracking()
                .Include(x => x.AchBatch)
                .Where(x => ids.Contains(x.Id) && x.AchCycleId == request.OriginalCycleId)
                .ToListAsync(ct);
            if (originals.Count != ids.Length || originals.Any(x => x.SourceInstitutionId != localReceivingInstitution.Id
                || x.DestinationInstitutionId != respondingInstitution.Id || !x.IsPrenotification))
                throw new InvalidOperationException("PRENOTIFICATION_ORIGIN_MISMATCH: Origen, receptor o correlacion inconsistente.");
            if (originals.Select(x => x.TraceNumber).Distinct(StringComparer.Ordinal).Count() != ids.Length)
                throw new InvalidOperationException("PRENOTIFICATION_TRACE_NOT_UNIQUE: No es seguro emitir dos devoluciones con el mismo trace original.");
            var reason = request.ReasonCode!.Trim().ToUpperInvariant();
            if (clearingHouse.Code.Contains("CENIT", StringComparison.OrdinalIgnoreCase)
                && CenitReturnIn2026Layout.IsReturnOfReturnCause(reason))
                throw new InvalidOperationException("CENIT_ROR_NOT_ORDINARY_RETURN: R60-R74 usan un flujo distinto.");
            var reasonCheck = await ValidateReturnReasonAsync(clearingHouse.Id, reason, request.BusinessDate, originals, ct);
            if (reasonCheck is not null)
                throw new InvalidOperationException($"{reasonCheck.FunctionalCode}: {reasonCheck.Message}");
            var profile = await ResolvePublishedDifferentialProfileAsync(clearingHouse, request.BusinessDate, ct);
            if (!profile.Success)
                throw new InvalidOperationException($"RETURN_PROFILE_NOT_HOMOLOGATED: {profile.DiagnosticCode}");

            var sequence = await NextDailySequenceAsync(clearingHouse.Id, respondingInstitution.Id, request.BusinessDate, ct);
            var fileName = BuildExternalFileName(clearingHouse, localReceivingInstitution, request.BusinessDate, sequence);
            var simulatorRequest = new GenerateNachaInboundSimulationRequest
            {
                SimulationMode = NachaSimulationMode.DifferentialResponses,
                ScenarioType = NachaInboundSimulationType.IncomingPrenotificationReturn,
                ClearingHouseCode = clearingHouse.Code,
                OriginFinancialInstitutionId = respondingInstitution.Id,
                DestinationFinancialInstitutionId = localReceivingInstitution.Id,
                EntriesCount = ids.Length,
                Amount = 0,
                BusinessDate = request.BusinessDate,
                CycleCode = request.ResponseCycleId!,
                ResponseMode = InboundResponseMode.Returned,
                ReasonCode = reason
            };
            outputDirectory = Path.Combine(ResolveOutputDirectory(clearingHouse.Code, simulatorRequest.ScenarioType), reservationId.ToString("N"));
            var path = Path.Combine(outputDirectory, fileName);
            var simulation = new NachaInboundSimulation
            {
                SimulationId = reservationId,
                ClearingHouseId = clearingHouse.Id,
                ClearingHouseName = clearingHouse.Name,
                ScenarioType = NachaInboundSimulationType.IncomingPrenotificationReturn,
                ResponseMode = InboundResponseMode.Returned,
                ReasonCode = reason,
                OriginFinancialInstitutionId = respondingInstitution.Id,
                DestinationFinancialInstitutionId = localReceivingInstitution.Id,
                EntriesCount = ids.Length,
                Amount = 0,
                BusinessDate = request.BusinessDate,
                CycleCode = request.ResponseCycleId!,
                FileName = fileName,
                FilePath = path,
                Status = NachaInboundSimulationStatus.Draft,
                GeneratedOnly = true,
                AutoImported = false,
                UploadRequired = true,
                ExternalTransmission = false,
                CreatedBy = string.IsNullOrWhiteSpace(userName) ? "uat-local" : userName,
                Notes = PrenotificationSimulationIdentity.AuditMarker(requestHash)
            };
            // Reserva de la llave UNIQUE antes de asignar traces y crear archivos.
            _context.NachaInboundSimulations.Add(simulation);
            await _context.SaveChangesAsync(ct);
            var artifact = await BuildOfficialReturnFileAsync(simulatorRequest, clearingHouse,
                respondingInstitution, localReceivingInstitution, responseCycle, originals,
                sequence, MapSequenceToFileId(sequence), ct);
            if (artifact.Entries.Count != ids.Length || artifact.Entries.Any(e => e.Amount != 0))
                throw new InvalidOperationException("PRENOTIFICATION_RETURN_ARTIFACT_INVALID: Una devolucion de prenotificacion debe conservar una entrada a valor cero por ID.");
            var bytes = Encoding.ASCII.GetBytes(artifact.Content);
            var sha = Convert.ToHexString(SHA256.HashData(bytes));
            simulation.Sha256 = sha;
            simulation.FileSizeBytes = bytes.LongLength;
            simulation.Status = NachaInboundSimulationStatus.Generated;
            foreach (var entry in artifact.Entries)
            {
                entry.PrenotificationReference = originals.Single(x => x.Id == entry.TransactionId).TraceNumber;
                simulation.Entries.Add(entry);
            }
            var metadata = CreateMetadata(simulation, respondingInstitution, localReceivingInstitution, artifact,
                sha, fileName, bytes.LongLength);
            simulation.MetadataJson = JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true });
            Directory.CreateDirectory(outputDirectory);
            await File.WriteAllBytesAsync(path, bytes, ct);
            await WriteEvidenceAsync(outputDirectory, simulation, metadata, artifact.Content, ct);
            await _context.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);
            return ToPrenotificationReturnReplay(simulation, requestHash);
        }
        catch
        {
            try { await tx.RollbackAsync(CancellationToken.None); }
            finally
            {
                // Un fallo del filesystem no debe ocultar el error de negocio/BD.
                if (outputDirectory is not null && Directory.Exists(outputDirectory))
                {
                    try { Directory.Delete(outputDirectory, recursive: true); }
                    catch (IOException) { /* residuo fisico: se documenta para reconciliacion I07 */ }
                    catch (UnauthorizedAccessException) { /* residuo fisico sin registro confirmado */ }
                }
            }
            throw;
        }
    }

    private static GenerateNachaInboundSimulationResponse ToPrenotificationReturnReplay(
        NachaInboundSimulation existing, string expectedHash)
    {
        if (!PrenotificationSimulationIdentity.MatchesStoredPayload(existing.Notes, expectedHash))
            throw new InvalidOperationException("IDEMPOTENCY_KEY_REUSED_WITH_DIFFERENT_REQUEST: La clave ya fue usada con otra seleccion o causal.");
        if (existing.Status is not (NachaInboundSimulationStatus.Generated or NachaInboundSimulationStatus.Downloaded)
            || string.IsNullOrWhiteSpace(existing.FilePath) || !File.Exists(existing.FilePath))
            throw new InvalidOperationException("IDEMPOTENCY_RESERVATION_NOT_READY: La reserva existe pero su archivo todavia no esta disponible.");
        return new GenerateNachaInboundSimulationResponse(existing.SimulationId, existing.Id,
            existing.FileName, $"/api/uat/nacha-inbound-simulator/{existing.Id}/file",
            $"/api/uat/nacha-inbound-simulator/{existing.Id}/evidence", existing.Sha256,
            existing.FileSizeBytes, true, false, true, false,
            "Devolucion NACHA-M generada o recuperada de una clave idempotente. Requiere NachaUpload manual; no se modificaron estados.");
    }
}
