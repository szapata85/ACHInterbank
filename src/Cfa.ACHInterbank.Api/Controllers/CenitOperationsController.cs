using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Persistence.DataBase;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/cenit")]
[Authorize]
public class CenitOperationsController : ControllerBase
{
    private const string CenitClearingHouseCode = "CENIT";
    private readonly AchDbContext _dbContext;
    private readonly ICenitChamberResponseService? _chamberResponses;

    public CenitOperationsController(
        AchDbContext dbContext,
        ICenitChamberResponseService? chamberResponses)
    {
        _dbContext = dbContext;
        _chamberResponses = chamberResponses;
    }

    [EndpointSummary("Importar respuesta de cámara CENIT")]
    [HttpPost("chamber-responses")]
    [Authorize(Policy = "CanManageAch")]
    [ProducesResponseType(typeof(CenitChamberResponseResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(CenitChamberResponseResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ImportChamberResponseAsync(
        [FromBody] CenitChamberResponseImportCommand command,
        CancellationToken ct = default)
    {
        if (_chamberResponses is null)
        {
            return ProblemResult("CENIT_CHAMBER_RESPONSE_SERVICE_UNAVAILABLE", StatusCodes.Status503ServiceUnavailable, null);
        }

        CenitChamberResponseResult result;
        try
        {
            result = await _chamberResponses.ImportAsync(command, ct);
        }
        catch (ArgumentException exception)
        {
            return ProblemResult("CENIT_RESPONSE_INVALID", StatusCodes.Status400BadRequest, exception.Message);
        }

        if (!string.IsNullOrWhiteSpace(result.ProblemCode))
        {
            var status = result.ProblemCode.Contains("AMBIGUOUS", StringComparison.Ordinal)
                         || result.ProblemCode.Contains("CONFLICT", StringComparison.Ordinal)
                         || result.ProblemCode.Contains("TRANSITION", StringComparison.Ordinal)
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status422UnprocessableEntity;
            return ProblemResult(result.ProblemCode, status, "La respuesta CENIT quedó registrada con resultado operativo controlado.", result);
        }

        return result.IsDuplicate
            ? Ok(result)
            : CreatedAtAction(nameof(GetChamberResponseAsync), new { id = result.Id }, result);
    }

    [EndpointSummary("Detalle de respuesta de cámara CENIT")]
    [HttpGet("chamber-responses/{id:guid}")]
    [Authorize(Policy = "CanReadAch")]
    [ProducesResponseType(typeof(CenitChamberResponseResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChamberResponseAsync(Guid id, CancellationToken ct = default)
    {
        if (_chamberResponses is null) return NotFound();
        var result = await _chamberResponses.GetAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    [EndpointSummary("Consultar respuestas de cámara CENIT")]
    [HttpGet("chamber-responses")]
    [Authorize(Policy = "CanReadAch")]
    [ProducesResponseType(typeof(CenitChamberResponsePage), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListChamberResponsesAsync(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        if (_chamberResponses is null)
        {
            return Ok(new CenitChamberResponsePage([], 0, Math.Max(1, page), Math.Clamp(pageSize, 1, 200)));
        }
        return Ok(await _chamberResponses.ListAsync(page, pageSize, ct));
    }

    [EndpointSummary("Cola de ejecución de ciclos CENIT")]
    [EndpointDescription("Qué hace: consulta elementos en cola de ciclo CENIT con estado y contexto transaccional. Cuándo se usa: en monitoreo de encolamiento y despacho. Perfil consumidor: operación CENIT y soporte ACH. Permiso requerido: CanReadAch. Tipo de operación: solo consulta. Genera auditoría: sí, por bitácoras y eventos de ejecución. Riesgos operativos: paginación/filtros inadecuados pueden ocultar cuellos de botella. Errores esperados: 400 por parámetros; 401/403. Relación ACH/CENIT/NACHA-M: observa interacción entre transacciones ACH y ciclo CENIT. Precauciones para desarrollo u operación: usar page/pageSize dentro de límites operativos.")]
    [HttpGet("queues")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> GetQueueAsync(
        [FromQuery] string? status,
        [FromQuery] string? targetAchCycleId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = _dbContext.Set<CenitCycleQueue>()
            .AsNoTracking()
            .Include(x => x.AchTransaction)
            .Include(x => x.TargetAchCycle)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(x => x.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(targetAchCycleId))
        {
            query = query.Where(x => x.TargetAchCycleId == targetAchCycleId);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.EnqueuedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.Status,
                x.QueueReason,
                x.EnqueuedAtUtc,
                x.DequeuedAtUtc,
                x.TargetAchCycleId,
                TargetCycleName = x.TargetAchCycle.CycleName,
                x.OriginalAchCycleId,
                TransactionId = x.AchTransactionId,
                TransactionExternalId = x.AchTransaction.TransactionExternalId,
                x.AchTransaction.Reference,
                x.AchTransaction.Amount,
                TransactionType = x.AchTransaction.Type.ToString(),
                TransactionState = x.AchTransaction.State.ToString(),
                x.AchTransaction.EffectiveEntryDate,
                x.CenitCycleExecutionId
            })
            .ToListAsync(ct);

        return Ok(new { items, total, page, pageSize });
    }

    [EndpointSummary("Posiciones netas de liquidez por ejecución")]
    [EndpointDescription("Qué hace: muestra débitos, créditos y liquidez por entidad financiera en una ejecución CENIT. Cuándo se usa: en conciliación de liquidez y priorización operativa. Perfil consumidor: tesorería operativa y operación ACH. Permiso requerido: CanReadAch. Tipo de operación: solo consulta. Genera auditoría: sí, por consulta y trazas. Riesgos operativos: lectura fuera de ejecución correcta puede llevar a decisiones de liquidez erradas. Errores esperados: 401/403; resultados vacíos cuando no hay ejecución. Relación ACH/CENIT/NACHA-M: conecta neteo CENIT con operación ACH. Precauciones para desarrollo u operación: confirmar cenitCycleExecutionId antes de decisiones.")]
    [HttpGet("net-positions")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> GetNetPositionsAsync(
        [FromQuery] long? cenitCycleExecutionId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var latestExecutionId = cenitCycleExecutionId
            ?? await _dbContext.Set<CenitCycleExecution>()
                .AsNoTracking()
                .OrderByDescending(x => x.StartedAtUtc)
                .Select(x => (long?)x.Id)
                .FirstOrDefaultAsync(ct);

        if (!latestExecutionId.HasValue)
        {
            return Ok(new { items = Array.Empty<object>(), total = 0, page, pageSize, cenitCycleExecutionId = (long?)null });
        }

        var query = _dbContext.Set<CenitNetPosition>()
            .AsNoTracking()
            .Include(x => x.FinancialInstitution)
            .Where(x => x.CenitNettingExecution.CenitCycleExecutionId == latestExecutionId.Value);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.NetAmount)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.FinancialInstitutionId,
                FinancialInstitutionName = x.FinancialInstitution.Name,
                x.DebitAmount,
                x.CreditAmount,
                x.NetAmount,
                x.ExternalLiquidity,
                x.SimulatedLiquidity,
                x.AvailableLiquidity,
                x.LiquiditySourceType,
                x.HasInsufficientFunds
            })
            .ToListAsync(ct);

        return Ok(new { items, total, page, pageSize, cenitCycleExecutionId = latestExecutionId.Value });
    }

    [EndpointSummary("Decisiones de optimización de liquidez")]
    [EndpointDescription("Qué hace: lista decisiones de optimización aplicadas sobre transacciones. Cuándo se usa: en análisis de por qué una transacción fue priorizada o movida de ciclo. Perfil consumidor: operación CENIT, riesgo y auditoría. Permiso requerido: CanReadAch. Tipo de operación: solo consulta. Genera auditoría: sí. Riesgos operativos: interpretar decisiones fuera de contexto puede afectar post-mortem. Errores esperados: 400 por filtros inválidos; 401/403. Relación ACH/CENIT/NACHA-M: explica lógica operativa de neteo y enrutamiento ACH/CENIT. Precauciones para desarrollo u operación: correlacionar con estado de transacción y ciclo origen/destino.")]
    [HttpGet("optimization-decisions")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> GetOptimizationDecisionsAsync(
        [FromQuery] long? cenitCycleExecutionId,
        [FromQuery] string? decisionType,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var query = _dbContext.Set<LiquidityOptimizationDecision>()
            .AsNoTracking()
            .Include(x => x.AchTransaction)
            .AsQueryable();

        if (cenitCycleExecutionId.HasValue)
        {
            query = query.Where(x => x.CenitCycleExecutionId == cenitCycleExecutionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(decisionType))
        {
            query = query.Where(x => x.DecisionType == decisionType);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.DecidedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.CenitCycleExecutionId,
                x.AchTransactionId,
                TransactionExternalId = x.AchTransaction.TransactionExternalId,
                x.AchTransaction.Reference,
                TransactionState = x.AchTransaction.State.ToString(),
                x.DecisionType,
                x.DecisionReason,
                x.Priority,
                x.LiquidityModelUsed,
                x.FromCycleId,
                x.ToCycleId,
                x.DecidedAtUtc,
                x.ValueDate,
                x.ClearingHouseCode,
                x.SourceFileReference
            })
            .ToListAsync(ct);

        return Ok(new { items, total, page, pageSize });
    }

    [EndpointSummary("Trazabilidad CENIT de transacciones")]
    [EndpointDescription("Qué hace: entrega vista trazable con causales de devolución/rechazo y estado. Cuándo se usa: en conciliación operativa y gestión de excepciones. Perfil consumidor: operación ACH/CENIT y auditoría. Permiso requerido: CanReadAch. Tipo de operación: solo consulta. Genera auditoría: sí. Riesgos operativos: códigos normalizados incorrectamente pueden sesgar análisis. Errores esperados: 400 filtros inválidos; 401/403. Relación ACH/CENIT/NACHA-M: une catálogo regulatorio con ejecución transaccional ACH/CENIT. Precauciones para desarrollo u operación: validar estado y ciclo antes de emitir conclusiones.")]
    [HttpGet("traceability")]
    [Authorize(Policy = "CanReadAch")]
    public async Task<IActionResult> GetTraceabilityAsync(
        [FromQuery] string? state,
        [FromQuery] string? achCycleId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 200);

        var cenitClearingHouseId = await _dbContext.ClearingHouses
            .AsNoTracking()
            .Where(x => x.Code == CenitClearingHouseCode)
            .Select(x => (int?)x.Id)
            .FirstOrDefaultAsync(ct);

        if (!cenitClearingHouseId.HasValue)
        {
            return Ok(new { items = Array.Empty<object>(), total = 0, page, pageSize });
        }

        var returnCodeRows = await _dbContext.Set<AchReturnCode>()
            .AsNoTracking()
            .Where(x => x.IsActive && x.ClearingHouseId == cenitClearingHouseId.Value)
            .Select(x => new { x.Code, x.Description })
            .ToListAsync(ct);

        var returnCodeDescriptions = returnCodeRows
            .Select(x => new
            {
                Code = NormalizeCode(x.Code),
                Description = x.Description ?? string.Empty
            })
            .Where(x => x.Code is not null)
            .ToDictionary(x => x.Code!, x => x.Description, StringComparer.OrdinalIgnoreCase);

        var rejectionCodeRows = await _dbContext.Set<AchFileRejectionCode>()
            .AsNoTracking()
            .Where(x => x.IsActive)
            .Select(x => new { x.Code, x.Description })
            .ToListAsync(ct);

        var rejectionCodeDescriptions = rejectionCodeRows
            .Select(x => new
            {
                Code = NormalizeCode(x.Code),
                Description = x.Description ?? string.Empty
            })
            .Where(x => x.Code is not null)
            .ToDictionary(x => x.Code!, x => x.Description, StringComparer.OrdinalIgnoreCase);

        var query = _dbContext.AchTransactions
            .AsNoTracking()
            .Include(x => x.AchCycle)
                .ThenInclude(x => x.ClearingHouse)
            .Include(x => x.AchBatch)
            .Where(x => x.AchCycle.ClearingHouseId == cenitClearingHouseId.Value)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(state))
        {
            query = query.Where(x => x.State.ToString() == state);
        }

        if (!string.IsNullOrWhiteSpace(achCycleId))
        {
            query = query.Where(x => x.AchCycleId == achCycleId);
        }

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(x => x.StateChangedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new
            {
                x.Id,
                x.TransactionExternalId,
                x.Reference,
                x.Amount,
                State = x.State.ToString(),
                x.AchCycleId,
                AchCycleName = x.AchCycle.CycleName,
                x.EffectiveEntryDate,
                ClearingHouseName = x.AchCycle.ClearingHouse != null ? x.AchCycle.ClearingHouse.Name ?? string.Empty : string.Empty,
                BatchId = x.AchBatchId,
                BatchSequenceNumber = x.AchBatch.BatchSequenceNumber,
                x.ReturnReasonCode,
                x.ContrapartidasResponseCode,
                x.OriginalTraceRef,
                x.StateChangedAtUtc,
                DecisionType = _dbContext.Set<LiquidityOptimizationDecision>()
                    .Where(d => d.AchTransactionId == x.Id)
                    .OrderByDescending(d => d.DecidedAtUtc)
                    .Select(d => d.DecisionType)
                    .FirstOrDefault(),
                SourceFileReference = _dbContext.Set<LiquidityOptimizationDecision>()
                    .Where(d => d.AchTransactionId == x.Id)
                    .OrderByDescending(d => d.DecidedAtUtc)
                    .Select(d => d.SourceFileReference)
                    .FirstOrDefault()
            })
            .ToListAsync(ct);

        var enrichedItems = items.Select(x =>
        {
            var regulatoryCode = NormalizeCode(x.ReturnReasonCode);
            var technicalCode = NormalizeCode(x.ContrapartidasResponseCode);
            var hasRegulatoryCode = !string.IsNullOrWhiteSpace(regulatoryCode);
            var hasTechnicalCode = !string.IsNullOrWhiteSpace(technicalCode);

            var causalCode = hasRegulatoryCode ? regulatoryCode : technicalCode;
            var causalKind = hasRegulatoryCode ? "Regulatoria" : hasTechnicalCode ? "Técnica" : "Sin causal";
            var causalDescription = hasRegulatoryCode
                ? ResolveRegulatoryDescription(regulatoryCode!, returnCodeDescriptions)
                : hasTechnicalCode
                    ? ResolveTechnicalDescription(technicalCode!, rejectionCodeDescriptions)
                    : "Sin causal registrada para la transacción.";

            return new
            {
                x.Id,
                x.TransactionExternalId,
                x.Reference,
                x.Amount,
                x.State,
                x.AchCycleId,
                x.AchCycleName,
                x.EffectiveEntryDate,
                x.ClearingHouseName,
                x.BatchId,
                x.BatchSequenceNumber,
                CausalCode = causalCode,
                CausalDescription = causalDescription,
                CausalKind = causalKind,
                x.OriginalTraceRef,
                x.StateChangedAtUtc,
                x.DecisionType,
                x.SourceFileReference
            };
        });

        return Ok(new { items = enrichedItems, total, page, pageSize });
    }

    private static string? NormalizeCode(string? code)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return null;
        }

        return code.Trim().ToUpperInvariant();
    }

    private static ObjectResult ProblemResult(string code, int status, string? detail, object? result = null)
    {
        var problem = new ProblemDetails
        {
            Title = code,
            Detail = detail,
            Status = status
        };
        problem.Extensions["code"] = code;
        if (result is not null) problem.Extensions["result"] = result;
        return new ObjectResult(problem) { StatusCode = status };
    }

    private static string ResolveRegulatoryDescription(string code, IReadOnlyDictionary<string, string> dictionary)
    {
        return dictionary.TryGetValue(code, out var description)
            ? description
            : $"Causal regulatoria no catalogada ({code}).";
    }

    private static string ResolveTechnicalDescription(string code, IReadOnlyDictionary<string, string> dictionary)
    {
        return dictionary.TryGetValue(code, out var description)
            ? description
            : $"Causal técnica no catalogada ({code}).";
    }
}
