using Cfa.ACHInterbank.Api.OpenApi;
using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/ach-colombia/file-exchange")]
public sealed class AchColombiaFileExchangeController(
    IAchColombiaManagedMftExecutionService executionService,
    IAchColombiaManagedMftTransferReadService readService,
    IAchColombiaManagedMftTransferOperationsService operationsService,
    IAchColombiaManagedMftAdministrationService administrationService,
    IAchColombiaManagedMftCredentialAdministrationService credentialAdministrationService) : ControllerBase
{
    /// <summary>Consulta transferencias administradas de archivos ACH Colombia.</summary>
    /// <param name="from">Fecha operativa mínima inclusive; el servicio compara solo la parte de fecha.</param>
    /// <param name="to">Fecha operativa máxima inclusive; el servicio consulta hasta antes del día siguiente.</param>
    /// <param name="direction">Dirección de transferencia opcional.</param>
    /// <param name="status">Estado de transferencia opcional.</param>
    /// <param name="cycleId">Identificador de ciclo ACH opcional.</param>
    /// <param name="executionOrigin">Origen de ejecución opcional.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    /// <param name="fileName">Fragmento opcional del nombre físico del archivo; la comparación del servicio no distingue mayúsculas de minúsculas.</param>
    /// <param name="transferId">Identificador exacto de transferencia opcional.</param>
    /// <param name="archived">Filtro opcional por presencia de fecha de archivado.</param>
    /// <param name="pageNumber">Número de página; el servicio normaliza valores menores que 1 a 1.</param>
    /// <param name="pageSize">Tamaño de página; el servicio lo normaliza al rango 1 a 500.</param>
    [EndpointSummary("Consultar transferencias administradas de ACH Colombia")]
    [EndpointDescription("Consulta transferencias administradas de archivos de ACH Colombia usando filtros opcionales por fechas, dirección, estado, ciclo, origen, nombre, identificador y archivado. El servicio ordena desde la transferencia más reciente y normaliza pageNumber a un mínimo de 1 y pageSize al rango 1..500. Para outbound, Transferred significa publicación confirmada en la frontera CFA y no recepción ni aceptación por ACH Colombia.")]
    [HttpGet("transfers")]
    [Authorize(Policy = "CanReadAch")]
    [ProducesResponseType(typeof(IReadOnlyList<AchManagedFileTransferSummary>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Colección de transferencias administradas con etiqueta operativa segura y evidencia externa explícitamente no confirmada cuando corresponde.")]
    public Task<IReadOnlyList<AchManagedFileTransferSummary>> Query(
        [FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] AchManagedFileDirection? direction,
        [FromQuery] AchManagedFileTransferStatus? status, [FromQuery] string? cycleId,
        [FromQuery] AchManagedFileExecutionOrigin? executionOrigin, CancellationToken ct,
        [FromQuery] string? fileName = null, [FromQuery] Guid? transferId = null,
        [FromQuery] bool? archived = null, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 50)
        => readService.QueryAsync(new(from, to, direction, status, cycleId, executionOrigin,
            fileName, transferId, archived, pageNumber, pageSize), ct);

    /// <summary>Obtiene el detalle de una transferencia administrada.</summary>
    /// <param name="id">Identificador de la transferencia.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Obtener transferencia administrada de ACH Colombia")]
    [EndpointDescription("Obtiene el detalle persistido de la transferencia identificada por id, incluida su historia ordenada, referencias sanitizadas y capacidades calculadas. Para outbound, la publicación en la frontera CFA se distingue de cualquier recepción o aceptación externa, que permanece no confirmada sin evidencia verificable. Si no existe una transferencia ACH Colombia con ese identificador, responde 404 sin body.")]
    [HttpGet("transfers/{id:guid}")]
    [Authorize(Policy = "CanReadAch")]
    [ProducesResponseType(typeof(AchManagedFileTransferDetail), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Detalle de la transferencia administrada con trazabilidad, semántica de publicación CFA y estado explícito de confirmación externa.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe una transferencia ACH Colombia con el identificador indicado; la respuesta no tiene body.")]
    public async Task<ActionResult<AchManagedFileTransferDetail>> Get(Guid id, CancellationToken ct)
        => await readService.GetAsync(id, ct) is { } result ? Ok(result) : NotFound();

    /// <summary>Descarga el contenido retenido de una transferencia administrada.</summary>
    /// <param name="id">Identificador de la transferencia cuyo contenido se solicita.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Descargar contenido de transferencia ACH Colombia")]
    [EndpointDescription("Solicita al servicio el contenido retenido de la transferencia. En éxito devuelve File(result.Content, result.ContentType, result.FileName): la implementación vigente entrega application/octet-stream y usa el nombre físico de la transferencia como filename de Content-Disposition. Si el servicio devuelve null por ausencia de contenido retenido, responde 404 sin body.")]
    [HttpGet("transfers/{id:guid}/download")]
    [Authorize(Policy = "CanReadAch")]
    [Produces("application/octet-stream")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Contenido retenido devuelto como application/octet-stream; Content-Disposition usa el FileName suministrado por AchManagedFileDownload, que el servicio vigente obtiene del nombre físico de la transferencia.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "El servicio no dispone de contenido retenido para devolver y la respuesta no tiene body.")]
    public async Task<IActionResult> Download(Guid id, CancellationToken ct)
    {
        var result = await operationsService.DownloadAsync(id, Actor(), ct);
        return result is null ? NotFound() : File(result.Content, result.ContentType, result.FileName);
    }

    /// <summary>Ejecuta manualmente el flujo de intercambio saliente para un ciclo ACH Colombia.</summary>
    /// <param name="request">Ciclo a ejecutar y transferencia predecesora corregida opcional.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Ejecutar intercambio saliente ACH Colombia")]
    [EndpointDescription("Solicita al servicio una ejecución manual de intercambio saliente para el CycleId indicado. CorrectedFromTransferId puede identificar una transferencia saliente previa del mismo ciclo que se corrige. La operación consume Idempotency-Key opcional; si no se envía, el controller deriva una clave manual desde HttpContext.TraceIdentifier.")]
    [HttpPost("outbound/execute")]
    [OpenApiRequestExample(typeof(AchManagedFileExecuteOutboundRequestExampleProvider))]
    [OpenApiResponseExample(StatusCodes.Status200OK, typeof(AchManagedFileExecutionSuccessResponseExampleProvider))]
    [Authorize(Policy = "CanManageAch")]
    [ProducesResponseType(typeof(AchManagedFileExecutionResult), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Resultado de la ejecución saliente con cantidades procesadas, exitosas, fallidas e identificadores de transferencia involucrados.")]
    public async Task<ActionResult<AchManagedFileExecutionResult>> ExecuteOutbound(ExecuteOutboundRequest request, CancellationToken ct)
        => Ok(await executionService.ExecuteOutboundAsync(request.CycleId, AchManagedFileExecutionOrigin.Manual, Actor(), Idempotency(), ct, request.CorrectedFromTransferId));

    /// <summary>Ejecuta manualmente el flujo de intercambio entrante ACH Colombia.</summary>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Ejecutar intercambio entrante ACH Colombia")]
    [EndpointDescription("Solicita al servicio una ejecución manual de recepción de archivos mediante el adaptador configurado. La operación consume Idempotency-Key opcional; si no se envía, el controller deriva una clave manual desde HttpContext.TraceIdentifier.")]
    [HttpPost("inbound/execute")]
    [OpenApiResponseExample(StatusCodes.Status200OK, typeof(AchManagedFileExecutionSuccessResponseExampleProvider))]
    [Authorize(Policy = "CanManageAch")]
    [ProducesResponseType(typeof(AchManagedFileExecutionResult), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Resultado de la ejecución entrante con cantidades procesadas, exitosas, fallidas e identificadores de transferencia involucrados.")]
    public async Task<ActionResult<AchManagedFileExecutionResult>> ExecuteInbound(CancellationToken ct)
        => Ok(await executionService.ExecuteInboundAsync(AchManagedFileExecutionOrigin.Manual, Actor(), Idempotency(), ct));

    /// <summary>Reintenta manualmente una transferencia saliente pendiente o incierta.</summary>
    /// <param name="id">Identificador de la transferencia a reintentar.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Reintentar transferencia saliente ACH Colombia")]
    [EndpointDescription("Solicita al servicio un reintento manual para la transferencia identificada. La implementación del servicio admite transferencias salientes en RetryPending o Uncertain, con contenido retenido, perfil habilitado y reintentos disponibles. La operación consume Idempotency-Key opcional con el mismo fallback del controller.")]
    [HttpPost("transfers/{id:guid}/retry")]
    [Authorize(Policy = "CanManageAch")]
    [ProducesResponseType(typeof(AchManagedFileTransferDetail), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Detalle actualizado de la transferencia después del reintento completado por el servicio.")]
    public async Task<ActionResult<AchManagedFileTransferDetail>> Retry(Guid id, CancellationToken ct)
        => Ok(await operationsService.RetryAsync(id, Actor(), Idempotency(), ct));

    /// <summary>Reprocesa una transferencia entrante elegible.</summary>
    /// <param name="id">Identificador de la transferencia a reprocesar.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Reprocesar transferencia entrante ACH Colombia")]
    [EndpointDescription("Solicita al servicio reprocesar la transferencia identificada. La implementación admite transferencias entrantes rechazadas o fallidas que conservan contenido y referencia a la ingestión previa; en éxito devuelve su detalle actualizado.")]
    [HttpPost("transfers/{id:guid}/reprocess")]
    [Authorize(Policy = "CanManageAch")]
    [ProducesResponseType(typeof(AchManagedFileTransferDetail), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Detalle actualizado de la transferencia después del reproceso completado por el servicio.")]
    public async Task<ActionResult<AchManagedFileTransferDetail>> Reprocess(Guid id, CancellationToken ct)
        => Ok(await operationsService.ReprocessAsync(id, Actor(), ct));

    /// <summary>Marca como archivada una transferencia administrada que no ha sido retirada.</summary>
    /// <param name="id">Identificador de la transferencia a archivar.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Archivar transferencia ACH Colombia")]
    [EndpointDescription("Solicita al servicio registrar el archivado operativo de la transferencia identificada. La implementación conserva el contenido retenido, establece la referencia de archivo cuando falta y rechaza internamente transferencias ya retiradas; en éxito devuelve el detalle actualizado.")]
    [HttpPost("transfers/{id:guid}/archive")]
    [Authorize(Policy = "CanManageAch")]
    [ProducesResponseType(typeof(AchManagedFileTransferDetail), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Detalle actualizado de la transferencia después del archivado completado por el servicio.")]
    public async Task<ActionResult<AchManagedFileTransferDetail>> Archive(Guid id, CancellationToken ct)
        => Ok(await operationsService.ArchiveAsync(id, Actor(), ct));

    /// <summary>Retira una transferencia administrada del almacenamiento activo.</summary>
    /// <param name="id">Identificador de la transferencia a retirar.</param>
    /// <param name="request">Motivo de retiro exigido por el servicio.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Retirar transferencia ACH Colombia")]
    [EndpointDescription("Solicita al servicio retirar la transferencia identificada del almacenamiento activo y registrar el motivo informado. El servicio exige una razón no vacía y no permite retirar una transferencia InProgress; en éxito devuelve el detalle actualizado.")]
    [HttpPost("transfers/{id:guid}/retire")]
    [Authorize(Policy = "CanManageAch")]
    [ProducesResponseType(typeof(AchManagedFileTransferDetail), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Detalle actualizado de la transferencia después del retiro completado por el servicio.")]
    public async Task<ActionResult<AchManagedFileTransferDetail>> Retire(Guid id, RetireRequest request, CancellationToken ct)
        => Ok(await operationsService.RetireAsync(id, Actor(), request.Reason, ct));

    /// <summary>Consulta la configuración operativa del intercambio administrado de archivos ACH Colombia.</summary>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar configuración de intercambio ACH Colombia")]
    [EndpointDescription("Obtiene la configuración operativa vigente del intercambio administrado de archivos ACH Colombia, incluidos indicadores de ejecución automática/manual, reintentos, retención, ubicaciones operativas y el token de concurrencia actual.")]
    [HttpGet("configuration")]
    [Authorize(Policy = "CanReadAch")]
    [ProducesResponseType(typeof(AchManagedFileTransferConfigurationDto), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Configuración operativa vigente del intercambio administrado ACH Colombia.")]
    public Task<AchManagedFileTransferConfigurationDto> GetConfiguration(CancellationToken ct)
        => administrationService.GetConfigurationAsync(ct);

    /// <summary>Actualiza la configuración operativa del intercambio administrado de archivos ACH Colombia.</summary>
    /// <param name="request">Configuración operativa completa y token de concurrencia obtenido previamente.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Actualizar configuración de intercambio ACH Colombia")]
    [EndpointDescription("Actualiza los indicadores operativos, límites de reintentos/retención y ubicaciones del intercambio administrado. El servicio compara ConcurrencyToken con la versión persistida y genera un token nuevo al guardar. El controller traduce únicamente el conflicto de concurrencia a 409 ProblemDetails.")]
    [HttpPut("configuration")]
    [Authorize(Policy = "CanManageAch")]
    [ProducesResponseType(typeof(AchManagedFileTransferConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Configuración operativa persistida con un nuevo ConcurrencyToken.")]
    [OpenApiResponseDescription(StatusCodes.Status409Conflict, "ConcurrencyToken no coincide con la configuración persistida; se devuelve ProblemDetails indicando recargar y reintentar.")]
    public async Task<ActionResult<AchManagedFileTransferConfigurationDto>> UpdateConfiguration(
        AchManagedFileTransferConfigurationDto request, CancellationToken ct)
    {
        try { return Ok(await administrationService.UpdateConfigurationAsync(request, Actor(), ct)); }
        catch (DbUpdateConcurrencyException) { return Conflict(new ProblemDetails { Status = 409, Title = "La configuración cambió", Detail = "Actualice la pantalla y vuelva a intentar." }); }
    }

    /// <summary>Consulta el perfil administrativo del intercambio administrado ACH Colombia.</summary>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar administración de intercambio ACH Colombia")]
    [EndpointDescription("Obtiene el perfil administrativo vigente: proveedor, protocolo, endpoint, principal, controles operativos, ubicaciones, estado de credencial y token de concurrencia. El contrato no devuelve el valor secreto de la credencial.")]
    [HttpGet("administration")]
    [Authorize(Policy = P1Policies.ConfigRead)]
    [ProducesResponseType(typeof(AchManagedMftAdministrationDto), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Perfil administrativo vigente; expone metadata de credencial pero nunca su valor secreto.")]
    public Task<AchManagedMftAdministrationDto> GetAdministration(CancellationToken ct)
        => administrationService.GetAdministrationAsync(ct);

    /// <summary>Actualiza el perfil administrativo del intercambio administrado ACH Colombia.</summary>
    /// <param name="request">Perfil administrativo completo y token de concurrencia obtenido previamente.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Actualizar administración de intercambio ACH Colombia")]
    [EndpointDescription("Actualiza perfil, proveedor, protocolo, endpoint/principal, controles operativos, reintentos, retención y ubicaciones. El servicio valida rangos operativos y campos requeridos; ArgumentException se traduce a 400 ProblemDetails. Si ConcurrencyToken no coincide con la versión persistida, devuelve 409 ProblemDetails. En éxito genera un token de concurrencia nuevo.")]
    [HttpPut("administration")]
    [Authorize(Policy = P1Policies.ConfigManage)]
    [ProducesResponseType(typeof(AchManagedMftAdministrationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Perfil administrativo persistido con metadata de credencial y un nuevo ConcurrencyToken.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "El servicio rechazó la configuración por campos requeridos o rangos operativos inválidos; se devuelve ProblemDetails.")]
    [OpenApiResponseDescription(StatusCodes.Status409Conflict, "ConcurrencyToken no coincide con la configuración persistida; se devuelve ProblemDetails indicando recargar y reintentar.")]
    public async Task<ActionResult<AchManagedMftAdministrationDto>> UpdateAdministration(UpdateAchManagedMftAdministrationRequest request, CancellationToken ct)
    {
        try { return Ok(await administrationService.UpdateAdministrationAsync(request, Actor(), ct)); }
        catch (DbUpdateConcurrencyException) { return Conflict(new ProblemDetails { Status = 409, Title = "La configuración cambió", Detail = "Actualice la pantalla y vuelva a intentar." }); }
        catch (ArgumentException ex) { return BadRequest(new ProblemDetails { Status = 400, Title = "Configuración inválida", Detail = ex.Message }); }
    }

    /// <summary>Establece la credencial protegida del perfil administrativo ACH Colombia.</summary>
    /// <param name="request">Tipo de credencial, secreto a proteger y token de concurrencia vigente. El secreto no forma parte del response.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Actualizar credencial de intercambio ACH Colombia")]
    [EndpointDescription("Establece la credencial del perfil administrativo. CredentialType y Secret deben contener un valor no vacío y ConcurrencyToken debe corresponder a la versión vigente; ArgumentException se traduce a 400 ProblemDetails y un token obsoleto a 409 ProblemDetails. El servicio protege el secreto antes de persistirlo y la respuesta devuelve únicamente metadata de credencial, nunca el secreto.")]
    [HttpPut("administration/credential")]
    [Authorize(Policy = P1Policies.ConfigManage)]
    [ProducesResponseType(typeof(AchManagedMftAdministrationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Perfil administrativo actualizado; confirma metadata de credencial sin devolver el secreto.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "CredentialType o Secret no contienen un valor válido; se devuelve ProblemDetails sin eco del secreto.")]
    [OpenApiResponseDescription(StatusCodes.Status409Conflict, "ConcurrencyToken no coincide con la configuración persistida; se devuelve ProblemDetails indicando recargar y reintentar.")]
    public async Task<ActionResult<AchManagedMftAdministrationDto>> SetCredential(SetAchManagedMftCredentialRequest request, CancellationToken ct)
    {
        try { return Ok(await credentialAdministrationService.SetCredentialAsync(request, Actor(), ct)); }
        catch (DbUpdateConcurrencyException) { return Conflict(new ProblemDetails { Status = 409, Title = "La configuración cambió", Detail = "Actualice la pantalla y vuelva a intentar." }); }
        catch (ArgumentException ex) { return BadRequest(new ProblemDetails { Status = 400, Title = "Credencial inválida", Detail = ex.Message }); }
    }

    [EndpointSummary("Probar conexión de intercambio ACH Colombia")]
    [EndpointDescription("Verifica la configuración guardada, autenticación y acceso a las rutas del CFA MFT Boundary. No envía archivos NACHA-M y nunca devuelve secretos ni excepciones técnicas.")]
    [HttpPost("administration/test-connection")]
    [Authorize(Policy = P1Policies.ConfigManage)]
    [ProducesResponseType(typeof(AchManagedMftConnectionTestResult), StatusCodes.Status200OK)]
    public async Task<ActionResult<AchManagedMftConnectionTestResult>> TestConnection(CancellationToken ct)
        => Ok(await operationsService.TestConnectionAsync(ct));

    private string Actor() => User.Identity?.Name ?? "usuario-api";
    private string Idempotency() => Request.Headers.TryGetValue("Idempotency-Key", out var value) && !string.IsNullOrWhiteSpace(value)
        ? value.ToString() : $"manual:{HttpContext.TraceIdentifier}";

    /// <summary>Solicitud de ejecución manual saliente del intercambio administrado ACH Colombia.</summary>
    /// <param name="CycleId">Identificador del ciclo ACH Colombia que se solicita ejecutar.</param>
    /// <param name="CorrectedFromTransferId">Identificador opcional de una transferencia saliente previa del mismo ciclo que se corrige.</param>
    public sealed record ExecuteOutboundRequest(string CycleId, Guid? CorrectedFromTransferId = null);

    /// <summary>Solicitud de retiro de una transferencia administrada.</summary>
    /// <param name="Reason">Motivo de retiro; el servicio exige un valor no vacío.</param>
    public sealed record RetireRequest(string Reason);
}
