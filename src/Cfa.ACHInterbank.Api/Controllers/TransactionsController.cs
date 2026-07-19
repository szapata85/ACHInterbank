using Cfa.ACHInterbank.Application.ACH.Interfaces;
using Cfa.ACHInterbank.Application.Security;
using Cfa.ACHInterbank.Application.ACH.Models;
using Cfa.ACHInterbank.Application.Integrations.Interfaces;
using Cfa.ACHInterbank.Application.Integrations.Models;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Dtos;
using Cfa.ACHInterbank.Domain.Entities.Transactions.Enums;
using Cfa.ACHInterbank.Domain.Models.ACH;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cfa.ACHInterbank.Api.Controllers;

[ApiController]
[Route("[controller]")]
[Route("api/transactions")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly IAchTransactionService _transactionService;
    private readonly ILogger<TransactionsController> _logger;
    private readonly ITransactionPolicyService _transactionPolicyService;
    private readonly IAchBulkTransactionService _bulkTransactionService;
    private readonly IAchBulkIngestionService _bulkIngestionService;
    private readonly ITransactionIntegrationReadinessService? _integrationReadinessService;
    private readonly ITransactionIntegrationResultService? _integrationResultService;

    public TransactionsController(
        IAchTransactionService transactionService,
        ITransactionPolicyService transactionPolicyService,
        IAchBulkTransactionService bulkTransactionService,
        IAchBulkIngestionService bulkIngestionService,
        ILogger<TransactionsController> logger,
        ITransactionIntegrationReadinessService? integrationReadinessService = null,
        ITransactionIntegrationResultService? integrationResultService = null)
    {
        _transactionService = transactionService;
        _logger = logger;
        _transactionPolicyService = transactionPolicyService;
        _bulkTransactionService = bulkTransactionService;
        _bulkIngestionService = bulkIngestionService;
        _integrationReadinessService = integrationReadinessService;
        _integrationResultService = integrationResultService;
    }
    [EndpointSummary("Listado operativo de transacciones ACH con filtros de ciclo y cámara")]
    [EndpointDescription("Qué consulta: retorna transacciones ACH por filtros de ciclo, fecha efectiva y cámara para monitoreo operativo. Quién lo usa: operación, soporte y auditoría funcional para seguimiento diario. Permiso requerido: CanReadAch con autorización explícita en la acción. Tipo: consulta sin mutación. Impacto operacional: habilita visibilidad de volumen y estado para conciliación y priorización de incidentes. Auditoría/trazabilidad: la consulta debe quedar trazada con filtros aplicados y usuario consumidor en infraestructura de observabilidad. Riesgos: filtros incompletos pueden omitir transacciones críticas o sesgar diagnósticos. Errores esperados: 400 por parámetros inválidos cuando aplique; 401/403 según capa de seguridad global; 500 no controlado. Relación ACH/NACHA-M: permite trazar transacciones originadas desde flujos individuales o masivos; a diferencia de BulkIngestionController, aquí se consulta entidad transacción y no estado de lote de archivo.")]
    [HttpGet]
    [Authorize(Policy = P0Policies.TransactionsRead)]
    [ProducesResponseType(typeof(IEnumerable<AchTransactionListDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? achCycleId,
        [FromQuery] string? achCycleName,
        [FromQuery] DateTime? effectiveDate,
        [FromQuery] int? clearingHouseId,
        CancellationToken ct)
    {
        var transactions = await _transactionService.GetAllAsync(achCycleId, achCycleName, effectiveDate, clearingHouseId, ct);
        return Ok(transactions);
    }

    [EndpointSummary("Catálogo de Company Entry Descriptions para originación ACH")]
    [EndpointDescription("Qué consulta: obtiene descripciones de lote permitidas para construir transacciones y lotes ACH válidos. Quién lo usa: integración, operación y QA para validar parametrización de originación. Permiso requerido: CanReadAch con autorización explícita en la acción. Tipo: consulta. Impacto operacional: evita usar descripciones no permitidas que luego rompan validaciones de negocio. Auditoría/trazabilidad: debe quedar registro de acceso al catálogo por cambios o pruebas operativas. Riesgos: usar catálogo desactualizado puede causar rechazos en registro de transacciones. Errores esperados: 401/403 por seguridad global; 500 no controlado. Relación ACH/lotes: alimenta composición de batch y semántica transaccional; no reemplaza el procesamiento de archivos de BulkIngestionController.")]
    [HttpGet("company-entry-descriptions")]
    [Authorize(Policy = P0Policies.TransactionsRead)]
    [ProducesResponseType(typeof(IEnumerable<CompanyEntryDescriptionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetCompanyEntryDescriptions(CancellationToken ct)
    {
        var items = await _transactionService.GetCompanyEntryDescriptionsAsync(ct);
        return Ok(items);
    }
    [EndpointSummary("Prevalidación de políticas ACH antes de registrar transacción")]
    [EndpointDescription("Qué consulta: evalúa reglas de política para una transacción candidata sin persistirla. Quién lo usa: canales de integración, soporte y operación para validar viabilidad previa. Permiso requerido: CanReadAch con autorización explícita en la acción. Tipo: consulta de simulación. Impacto operacional: reduce rechazos posteriores al anticipar reglas de prioridad, duplicidad y restricciones. Auditoría/trazabilidad: debe conservarse evidencia de solicitud y resultado de preview para diagnósticos. Riesgos: preview con datos incompletos puede generar falsa sensación de aprobación. Errores esperados: 400 solicitud inválida; 401/403 seguridad global; 500 no controlado. Relación ACH/transacciones: se centra en validación previa de entidad transacción; no procesa archivos ni lotes como BulkIngestionController.")]
    [HttpGet("policies/preview")]
    [Authorize(Policy = P0Policies.TransactionsPolicyPreview)]
    [ProducesResponseType(typeof(TransactionPolicyPreview), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> PreviewPolicy(
        [FromQuery] decimal amount,
        [FromQuery] string? transactionExternalId,
        [FromQuery] string? reference,
        [FromQuery] TransactionTypeEnum type,
        [FromQuery] AccountTypeEnum accountType,
        [FromQuery] bool isPrenotification,
        [FromQuery] int destinationInstitutionId,
        [FromQuery] string sourceAccountNumber,
        [FromQuery] string destinationAccountNumber,
        [FromQuery] string companyIdentification,
        [FromQuery] string? recipientIdNumber,
        CancellationToken ct)
    {
        var preview = await _transactionPolicyService.PreviewAsync(new TransactionPolicyPreviewRequest(
            amount,
            transactionExternalId,
            reference ?? string.Empty,
            type,
            accountType,
            isPrenotification,
            destinationInstitutionId,
            sourceAccountNumber,
            destinationAccountNumber,
            companyIdentification,
            recipientIdNumber), ct);

        return Ok(preview);
    }

    [EndpointSummary("Registro operativo de transacción ACH individual")]
    [EndpointDescription("Qué acción ejecuta: crea una transacción ACH individual aplicando validaciones de negocio y datos regulatorios. Quién lo usa: canales transaccionales, operación de originación y soporte de incidencias. Permiso requerido: CanManageAch con autorización explícita en la acción. Tipo: acción operativa con impacto sobre entidad transacción y conciliación. Auditoría/trazabilidad: debe registrar usuario, referencia/transactionExternalId, cuentas y resultado de validación para trazabilidad. Riesgos: datos inválidos o duplicados pueden afectar conciliación y generar rechazos de cámara. Errores esperados: 400 validación/regla incumplida; 401/403 seguridad global; 409 cuando exista conflicto de estado/duplicidad en capas superiores; 500 no controlado. Relación con BulkIngestionController: este endpoint gestiona una transacción individual; BulkIngestionController orquesta cargas por archivo/lote.")]
    [HttpPost]
    [Authorize(Policy = P0Policies.TransactionsCreate)]
    [ProducesResponseType(typeof(AchTransaction), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateTransaction([FromBody] AchTransactionRequest request, CancellationToken ct)
    {
        if (request is null) return BadRequest("El cuerpo de la solicitud no puede estar vacío.");
        if (!request.IsPrenotification && request.Amount <= 0) return BadRequest("El monto debe ser mayor a cero.");
        if (string.IsNullOrWhiteSpace(request.TransactionExternalId) && string.IsNullOrWhiteSpace(request.Reference))
            return BadRequest("Debe enviar transactionExternalId o reference (legado).");
        if (string.IsNullOrWhiteSpace(request.SourceAccountNumber)) return BadRequest("La cuenta de origen es obligatoria.");
        if (string.IsNullOrWhiteSpace(request.DestinationAccountNumber)) return BadRequest("La cuenta de destino es obligatoria.");
        if (string.IsNullOrWhiteSpace(request.CompanyName)) return BadRequest("El nombre del usuario originador es obligatorio.");
        if (string.IsNullOrWhiteSpace(request.CompanyIdentification)) return BadRequest("La identificación del usuario originador es obligatoria.");
        if (!string.IsNullOrWhiteSpace(request.SourcePersonType) && request.SourcePersonType is not ("PN" or "PJ"))
            return BadRequest("El tipo de persona del originador debe ser PN o PJ.");
        if (!string.IsNullOrWhiteSpace(request.RecipientPersonType) && request.RecipientPersonType is not ("PN" or "PJ"))
            return BadRequest("El tipo de persona del receptor debe ser PN o PJ.");
        if (!string.IsNullOrWhiteSpace(request.RecipientIdNumber) && string.IsNullOrWhiteSpace(request.RecipientName))
            return BadRequest("El nombre del receptor es obligatorio cuando se diligencia identificación de receptor.");

        try
        {
            var tx = await _transactionService.RegisterTransactionAsync(
                amount: request.Amount,
                reference: request.Reference,
                type: request.Type,
                accountType: request.AccountType,
                isPrenotification: request.IsPrenotification,
                destinationInstitutionId: request.DestinationInstitutionId,
                sourceAccountNumber: request.SourceAccountNumber,
                destinationAccountNumber: request.DestinationAccountNumber,
                companyName: request.CompanyName,
                companyIdentification: request.CompanyIdentification,
                companyEntryDescriptionId: request.CompanyEntryDescriptionId,
                sourcePersonType: request.SourcePersonType,
                recipientPersonType: request.RecipientPersonType,
                recipientIdNumber: request.RecipientIdNumber,
                recipientName: request.RecipientName,
                transactionExternalId: request.TransactionExternalId,
                requiresIdentityValidation: request.RequiresIdentityValidation,
                addendas: request.Addendas,
                ct: ct
            );

            _logger.LogInformation("Transacción ACH creada: {Id}", tx.Id);
            return CreatedAtAction(nameof(GetById), new { id = tx.Id }, tx);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validación fallida al registrar transacción");
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Regla de negocio incumplida al registrar transacción");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error interno al registrar transacción");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error interno del servidor" });
        }
    }



    [EndpointSummary("Submit bulk legacy de ingestión transaccional")]
    [EndpointDescription("Qué acción ejecuta: recibe una solicitud bulk legacy y la delega al servicio de ingestión masiva por modalidad declarada. Quién lo usa: integraciones legadas y operación en procesos de transición. Permiso requerido: CanManageAch con autorización explícita en la acción. Tipo: acción operativa de carga masiva con impacto en lote, validación y resultados agregados. Auditoría/trazabilidad: debe guardar usuario, sourceType, processingMode, batchReference y resultados para seguimiento. Riesgos: configuración errónea de origen/modo puede producir rechazos masivos. Errores esperados: 400 validación u origen no soportado; 401/403 seguridad global; 500 no controlado. Relación con BulkIngestionController: comparte dominio de masivos, pero este endpoint representa interfaz legacy de submit programático, mientras BulkIngestionController gestiona ciclo moderno por archivo y tracking de batch.")]
    [HttpPost("bulk/submit")]
    [Authorize(Policy = P0Policies.TransactionsBulkSubmit)]
    [ProducesResponseType(typeof(BulkIngestionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SubmitBulkIngestion([FromBody] BulkIngestionRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _bulkIngestionService.SubmitAsync(request, ct);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validación global fallida en submit de ingestión ACH");
            return BadRequest(new { message = ex.Message });
        }
        catch (NotSupportedException ex)
        {
            _logger.LogWarning(ex, "Origen masivo no soportado");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error interno al submit de ingestión ACH masiva");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error interno del servidor" });
        }
    }

    [EndpointSummary("Registro masivo legacy de transacciones en línea")]
    [EndpointDescription("Qué acción ejecuta: registra en bloque transacciones incluidas en el payload legacy bulk y retorna resultado agregado por ítem. Quién lo usa: integraciones heredadas, operación y soporte de cargas inline. Permiso requerido: CanManageAch con autorización explícita en la acción. Tipo: acción operativa con impacto en transacciones, batch y validación masiva. Auditoría/trazabilidad: debe guardar requestId/correlación, usuario invocante y detalle de resultados parciales. Riesgos: carga masiva sin controles previos puede introducir duplicados o inconsistencias de lote. Errores esperados: 400 validación global; 401/403 seguridad global; 409 conflictos de estado/duplicidad cuando aplique; 500 no controlado. Relación con BulkIngestionController: esta ruta mantiene compatibilidad legacy por payload de transacciones; BulkIngestionController cubre ingestión moderna orientada a archivo y lifecycle de batch.")]
    [HttpPost("bulk")]
    [Authorize(Policy = P0Policies.TransactionsBulkSubmit)]
    [ProducesResponseType(typeof(BulkAchTransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateTransactionsBulk([FromBody] BulkAchTransactionRequest request, CancellationToken ct)
    {
        try
        {
            var result = await _bulkTransactionService.RegisterBulkAsync(request, ct);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validación global fallida en carga masiva ACH");
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error interno al registrar transacciones ACH masivas");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = "Error interno del servidor" });
        }
    }

    [EndpointSummary("Consulta puntual de transacción ACH por identificador")]
    [EndpointDescription("Qué consulta: recupera una transacción específica para soporte, conciliación o auditoría técnica. Quién lo usa: operación ACH, soporte de incidentes y auditoría. Permiso requerido: CanReadAch con autorización explícita en la acción. Tipo: consulta. Impacto operacional: habilita análisis de caso puntual sin modificar estado. Auditoría/trazabilidad: debe registrarse quién consultó, id solicitado y contexto de diagnóstico. Riesgos: consultar id incorrecto puede conducir a análisis equivocado. Errores esperados: 401/403 seguridad global; 404 transacción no encontrada; 500 no controlado. Relación con BulkIngestionController: consulta entidad transacción, no estado de batch de ingestión moderna.")]
    [HttpGet("{id:int}")]
    [Authorize(Policy = P0Policies.TransactionsRead)]
    [ProducesResponseType(typeof(AchTransaction), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var tx = await _transactionService.GetTransactionByIdAsync(id, ct);
        if (tx == null)
            return NotFound(new { message = $"No se encontró la transacción con ID {id}" });

        return Ok(tx);
    }

    [EndpointSummary("Resultado derivado de integración core de una transacción")]
    [EndpointDescription("Retorna el último resultado y el historial resumido de integración sin exponer payloads SOAP.")]
    [HttpGet("{id:int}/integration-result")]
    [Authorize(Policy = P0Policies.TransactionsRead)]
    [ProducesResponseType(typeof(TransactionIntegrationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetIntegrationResult(int id, CancellationToken ct)
    {
        if (_integrationResultService is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        var result = await _integrationResultService.GetAsync(id, ct);
        return result is null
            ? NotFound(new { message = $"No se encontró la transacción con ID {id}" })
            : Ok(result);
    }

    [EndpointSummary("Garantía de readiness de integración SOAP para una transacción")]
    [EndpointDescription("Qué consulta: resuelve la operación SOAP esperada para una transacción y valida si sus mappings activos son suficientes antes de XML, DryRun o dispatch. Quién lo usa: QA UAT, operación, soporte e integraciones para evidenciar alineación Transaction -> Operation -> MappingReadiness. Permiso requerido: CanReadAch. Tipo: consulta read-only. Impacto operacional: no transmite, no genera XML, no cambia estados y no crea movimiento monetario. Errores esperados: 404 transacción inexistente; 503 si el servicio de garantía no está registrado; 401/403 por seguridad global.")]
    [HttpGet("{id:int}/integration-readiness")]
    [Authorize(Policy = P0Policies.TransactionsRead)]
    [ProducesResponseType(typeof(TransactionIntegrationReadinessResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetIntegrationReadiness(int id, CancellationToken ct)
    {
        if (_integrationReadinessService is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { message = "Servicio de readiness de integración no disponible." });
        }

        var readiness = await _integrationReadinessService.GetTransactionReadinessAsync(id, ct);
        if (readiness is null)
        {
            return NotFound(new { message = $"No se encontró la transacción con ID {id}" });
        }

        return Ok(readiness);
    }
}

// ✅ DTO para solicitud de creación
public class AchTransactionRequest
{
    public decimal Amount { get; set; }
    public string? TransactionExternalId { get; set; }
    public string Reference { get; set; } = string.Empty;
    public TransactionTypeEnum Type { get; set; }
    public AccountTypeEnum AccountType { get; set; } = AccountTypeEnum.Checking;
    public bool IsPrenotification { get; set; }
    public int DestinationInstitutionId { get; set; }
    public string SourceAccountNumber { get; set; } = string.Empty;
    public string DestinationAccountNumber { get; set; } = string.Empty;
    public string? RecipientIdNumber { get; set; }
    public string? RecipientName { get; set; }
    public bool RequiresIdentityValidation { get; set; }

    public string CompanyName { get; set; } = string.Empty;
    public string CompanyIdentification { get; set; } = string.Empty;
    public int CompanyEntryDescriptionId { get; set; }
    public string? SourcePersonType { get; set; }
    public string? RecipientPersonType { get; set; }

    public List<AddendaDto>? Addendas { get; set; }
}
