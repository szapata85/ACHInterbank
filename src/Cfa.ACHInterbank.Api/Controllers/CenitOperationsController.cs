using Cfa.ACHInterbank.Api.OpenApi;
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

    /// <summary>Importa una respuesta recibida desde la cámara CENIT y devuelve su clasificación persistida.</summary>
    /// <param name="command">Identificadores de origen, tipo de mensaje, contenido recibido y datos opcionales de correlación.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Importar respuesta de cámara CENIT")]
    [EndpointDescription("Registra y clasifica la respuesta recibida mediante el servicio CENIT. Devuelve 201 cuando crea un registro sin problema operativo y 200 cuando recupera idempotentemente una respuesta ya registrada. Las validaciones de entrada traducidas por el controller devuelven 400; los resultados persistidos con conflicto, correlación ambigua o transición inválida devuelven 409; los demás resultados operativos controlados devuelven 422. Si el servicio no está disponible en el controller, devuelve 503.")]
    [HttpPost("chamber-responses")]
    [Authorize(Policy = "CanManageAch")]
    [ProducesResponseType(typeof(CenitChamberResponseResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(CenitChamberResponseResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
    [OpenApiResponseDescription(StatusCodes.Status201Created, "Respuesta de cámara CENIT registrada y ubicación canónica devuelta mediante CreatedAtAction.")]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Respuesta de cámara CENIT ya registrada recuperada idempotentemente sin crear un nuevo registro.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "La solicitud incumple una validación explícita del servicio, como identificadores obligatorios, fecha de recepción o nombre de archivo válido.")]
    [OpenApiResponseDescription(StatusCodes.Status409Conflict, "La respuesta quedó registrada con conflicto, correlación ambigua o transición de estado no válida.")]
    [OpenApiResponseDescription(StatusCodes.Status422UnprocessableEntity, "La respuesta quedó registrada con un resultado operativo controlado distinto de conflicto, ambigüedad o transición inválida.")]
    [OpenApiResponseDescription(StatusCodes.Status503ServiceUnavailable, "El servicio de importación de respuestas de cámara CENIT no está disponible en el controller.")]
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

    /// <summary>Consulta una respuesta de cámara CENIT por su identificador persistido.</summary>
    /// <param name="id">Identificador de la respuesta de cámara.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Detalle de respuesta de cámara CENIT")]
    [EndpointDescription("Busca la respuesta persistida por id mediante el servicio de respuestas de cámara. Devuelve 404 sin cuerpo cuando el servicio no está disponible o cuando no existe el identificador solicitado.")]
    [HttpGet("chamber-responses/{id:guid}")]
    [Authorize(Policy = "CanReadAch")]
    [ProducesResponseType(typeof(CenitChamberResponseResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Respuesta de cámara CENIT encontrada para el identificador solicitado.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe una respuesta de cámara CENIT con el identificador indicado o el servicio no está disponible; la respuesta no contiene cuerpo.")]
    public async Task<IActionResult> GetChamberResponseAsync(Guid id, CancellationToken ct = default)
    {
        if (_chamberResponses is null) return NotFound();
        var result = await _chamberResponses.GetAsync(id, ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>Lista respuestas de cámara CENIT y archivos pendientes de respuesta usando paginación.</summary>
    /// <param name="page">Número de página; el servicio normaliza valores menores que 1 a 1.</param>
    /// <param name="pageSize">Tamaño solicitado; el servicio lo limita al intervalo de 1 a 200.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar respuestas de cámara CENIT")]
    [EndpointDescription("Devuelve una página que combina respuestas persistidas con archivos NACHA CENIT todavía pendientes de respuesta. page se normaliza a un mínimo de 1 y pageSize se limita entre 1 y 200; si el servicio no está disponible, devuelve una página vacía con esos valores normalizados.")]
    [HttpGet("chamber-responses")]
    [Authorize(Policy = "CanReadAch")]
    [ProducesResponseType(typeof(CenitChamberResponsePage), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Página de respuestas de cámara CENIT y archivos pendientes de respuesta.")]
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

    /// <summary>Consulta elementos de la cola de ciclos CENIT.</summary>
    /// <param name="status">Estado exacto opcional por el que se filtran los elementos de cola.</param>
    /// <param name="targetAchCycleId">Identificador exacto opcional del ciclo ACH destino.</param>
    /// <param name="page">Número de página; el controller normaliza valores menores que 1 a 1.</param>
    /// <param name="pageSize">Tamaño solicitado; el controller lo limita al intervalo de 1 a 200.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Cola de ejecución de ciclos CENIT")]
    [EndpointDescription("Consulta CenitCycleQueue sin seguimiento. Aplica igualdad exacta cuando se informan status o targetAchCycleId, normaliza la paginación, ordena por EnqueuedAtUtc descendente y devuelve un cuerpo anónimo con items, total, page y pageSize; cada item incluye datos de cola, ciclo destino y contexto de la transacción relacionada.")]
    [HttpGet("queues")]
    [Authorize(Policy = "CanReadAch")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Página de elementos de la cola CENIT que cumplen los filtros solicitados.")]
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

    /// <summary>Consulta posiciones netas asociadas a una ejecución de ciclo CENIT.</summary>
    /// <param name="cenitCycleExecutionId">Identificador opcional de ejecución; si se omite, el controller usa la ejecución más reciente por StartedAtUtc.</param>
    /// <param name="page">Número de página; el controller normaliza valores menores que 1 a 1.</param>
    /// <param name="pageSize">Tamaño solicitado; el controller lo limita al intervalo de 1 a 200.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Posiciones netas de liquidez por ejecución")]
    [EndpointDescription("Consulta posiciones netas de la ejecución indicada; cuando cenitCycleExecutionId no se informa, selecciona la ejecución más reciente por StartedAtUtc. Si no existe ninguna ejecución, devuelve items vacío, total 0 y cenitCycleExecutionId nulo. En caso contrario ordena las posiciones por NetAmount descendente y devuelve un cuerpo anónimo paginado con los importes y datos de liquidez persistidos.")]
    [HttpGet("net-positions")]
    [Authorize(Policy = "CanReadAch")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Página de posiciones netas de la ejecución CENIT seleccionada, o resultado vacío cuando no existe una ejecución.")]
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

    /// <summary>Consulta decisiones de optimización de liquidez persistidas.</summary>
    /// <param name="cenitCycleExecutionId">Identificador opcional de ejecución CENIT usado como filtro exacto.</param>
    /// <param name="decisionType">Tipo de decisión opcional usado como filtro exacto.</param>
    /// <param name="page">Número de página; el controller normaliza valores menores que 1 a 1.</param>
    /// <param name="pageSize">Tamaño solicitado; el controller lo limita al intervalo de 1 a 200.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Decisiones de optimización de liquidez")]
    [EndpointDescription("Consulta LiquidityOptimizationDecision sin seguimiento. Aplica filtros exactos por cenitCycleExecutionId y decisionType cuando se informan, normaliza la paginación, ordena por DecidedAtUtc descendente y devuelve un cuerpo anónimo con items, total, page y pageSize, incluyendo el contexto transaccional persistido para cada decisión.")]
    [HttpGet("optimization-decisions")]
    [Authorize(Policy = "CanReadAch")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Página de decisiones de optimización que cumplen los filtros solicitados.")]
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

    /// <summary>Consulta trazabilidad de transacciones asociadas a la cámara con código CENIT.</summary>
    /// <param name="state">Nombre textual opcional del estado de la transacción usado como filtro exacto.</param>
    /// <param name="achCycleId">Identificador opcional de ciclo ACH usado como filtro exacto.</param>
    /// <param name="page">Número de página; el controller normaliza valores menores que 1 a 1.</param>
    /// <param name="pageSize">Tamaño solicitado; el controller lo limita al intervalo de 1 a 200.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Trazabilidad CENIT de transacciones")]
    [EndpointDescription("Resuelve la cámara cuyo código es CENIT y consulta únicamente sus transacciones. Aplica filtros exactos por state y achCycleId cuando se informan, ordena por StateChangedAtUtc descendente y devuelve un cuerpo anónimo paginado. La respuesta deriva CausalCode, CausalKind y CausalDescription de ReturnReasonCode o, en su ausencia, ContrapartidasResponseCode y de los catálogos activos consultados por el controller. Si la cámara CENIT no está configurada, devuelve una página vacía.")]
    [HttpGet("traceability")]
    [Authorize(Policy = "CanReadAch")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Página de trazabilidad de transacciones pertenecientes a la cámara CENIT que cumplen los filtros solicitados.")]
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
