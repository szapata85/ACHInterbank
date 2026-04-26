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
    private readonly AchDbContext _dbContext;

    public CenitOperationsController(AchDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    [EndpointSummary("GET queues: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'queues'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: solo consulta. Genera auditoría: consulta sin modificación directa; trazable por logs de acceso.")]
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

    [EndpointSummary("GET net-positions: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'net-positions'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: solo consulta. Genera auditoría: consulta sin modificación directa; trazable por logs de acceso.")]
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

    [EndpointSummary("GET optimization-decisions: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'optimization-decisions'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: solo consulta. Genera auditoría: consulta sin modificación directa; trazable por logs de acceso.")]
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

    [EndpointSummary("GET traceability: servicio documentado para operación ACH/CENIT/NACHA-M.")]
    [EndpointDescription("Descripción funcional: expone la operación 'traceability'. Cuándo se usa: durante operación diaria y soporte. Perfil que consume: operador ACH, seguridad, auditor o integrador según el módulo. Permiso requerido: revisar [Authorize]/Policy del método y del controller. Parámetros: revisar parámetros de ruta y consulta definidos en la firma. Cuerpo de solicitud: aplica en métodos de escritura y se valida por modelo. Respuesta exitosa: 200 OK (o archivo cuando corresponda). Errores esperados: 400 validación, 401/403 autorización, 404 no encontrado, 409 conflicto cuando aplique. Notas operativas: respetar trazabilidad, controles NACHA-M y segregación de funciones. Tipo de operación: solo consulta. Genera auditoría: consulta sin modificación directa; trazable por logs de acceso.")]
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

        var returnCodeRows = await _dbContext.Set<AchReturnCode>()
            .AsNoTracking()
            .Where(x => x.IsActive)
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
