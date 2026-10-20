using Cfa.ACHInterbank.Application.OutgoingTransactionMonitoring;
using Cfa.ACHInterbank.Application.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("api/transactions/outgoing-monitoring")]
[Authorize]
public sealed class OutgoingTransactionMonitoringController : ControllerBase
{
    private readonly IOutgoingTransactionMonitoringQueryService _queryService;
    private readonly IOutgoingTransactionMonitoringAuditWriter _auditWriter;
    private readonly IAuthorizationService _authorization;

    public OutgoingTransactionMonitoringController(
        IOutgoingTransactionMonitoringQueryService queryService,
        IOutgoingTransactionMonitoringAuditWriter auditWriter,
        IAuthorizationService authorization)
    {
        _queryService = queryService;
        _auditWriter = auditWriter;
        _authorization = authorization;
    }

    [HttpGet]
    public async Task<IActionResult> Search([FromQuery] OutgoingTransactionMonitoringQuery query, CancellationToken cancellationToken)
    {
        if (!await IsAuthorizedAsync(P0Policies.OutgoingTransactionsMonitorRead))
        {
            await AuditAsync("Search", "list", false, SanitizedCriteria(query), cancellationToken);
            return StatusCode(StatusCodes.Status403Forbidden, Error("OUTGOING_MONITOR_FORBIDDEN", "No tienes permiso para consultar este monitor."));
        }

        try
        {
            var result = await _queryService.SearchAsync(query, cancellationToken);
            await AuditAsync("Search", "list", true, SanitizedCriteria(query), cancellationToken);
            return Ok(result);
        }
        catch (OutgoingTransactionMonitoringException exception)
        {
            return BadRequest(Error(exception.Code, exception.Message));
        }
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDetail(int id, CancellationToken cancellationToken)
    {
        if (!await IsAuthorizedAsync(P0Policies.OutgoingTransactionsMonitorRead))
        {
            await AuditAsync("Detail", id.ToString(), false, "{}", cancellationToken);
            return StatusCode(StatusCodes.Status403Forbidden, Error("OUTGOING_MONITOR_FORBIDDEN", "No tienes permiso para consultar esta transacción."));
        }

        var includeTechnical = await IsAuthorizedAsync(P0Policies.OutgoingTransactionsMonitorTechnicalDetailRead);
        var detail = await _queryService.GetDetailAsync(id, includeTechnical, cancellationToken);
        await AuditAsync("Detail", id.ToString(), true,
            JsonSerializer.Serialize(new { technicalDetailAuthorized = includeTechnical }), cancellationToken);
        return detail is null
            ? NotFound(Error("OUTGOING_TRANSACTION_NOT_FOUND", "No encontramos una transacción de salida con ese identificador."))
            : Ok(detail);
    }

    private async Task<bool> IsAuthorizedAsync(string policy)
        => (await _authorization.AuthorizeAsync(User, policy)).Succeeded;

    private async Task AuditAsync(string operation, string entityId, bool authorized, string criteria, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.Identity?.Name ?? "authenticated-user";
        var correlationId = HttpContext.Request.Headers["X-Correlation-ID"].FirstOrDefault()
            ?? HttpContext.TraceIdentifier;
        await _auditWriter.WriteAsync(new OutgoingTransactionMonitoringAudit(
            userId, operation, entityId, correlationId, authorized, criteria), cancellationToken);
    }

    private static string SanitizedCriteria(OutgoingTransactionMonitoringQuery query)
        => JsonSerializer.Serialize(new
        {
            query.FromUtc,
            query.ToUtc,
            query.ClearingHouseId,
            query.CycleId,
            query.DestinationInstitutionId,
            query.TransactionType,
            query.ResponseCode,
            query.ProcessStatus,
            query.InitialResult,
            query.SubsequentSituation,
            query.HasReturn,
            query.RequiresAttention,
            query.MinimumAmount,
            query.MaximumAmount,
            query.PageNumber,
            query.PageSize,
            query.SortBy,
            query.SortDirection,
            hasExternalIdentifierFilter = !string.IsNullOrWhiteSpace(query.TransactionExternalId),
            hasTraceFilter = !string.IsNullOrWhiteSpace(query.TraceNumber)
        });

    private static object Error(string errorCode, string message) => new { errorCode, message };
}
