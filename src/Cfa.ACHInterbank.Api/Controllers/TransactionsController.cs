using Cfa.ACHInterbank.Api.OpenApi;
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
    private readonly ITransactionThirdPartyAccountSelectorService? _thirdPartyAccountSelectorService;

    public TransactionsController(
        IAchTransactionService transactionService,
        ITransactionPolicyService transactionPolicyService,
        IAchBulkTransactionService bulkTransactionService,
        IAchBulkIngestionService bulkIngestionService,
        ILogger<TransactionsController> logger,
        ITransactionIntegrationReadinessService? integrationReadinessService = null,
        ITransactionIntegrationResultService? integrationResultService = null,
        ITransactionThirdPartyAccountSelectorService? thirdPartyAccountSelectorService = null)
    {
        _transactionService = transactionService;
        _logger = logger;
        _transactionPolicyService = transactionPolicyService;
        _bulkTransactionService = bulkTransactionService;
        _bulkIngestionService = bulkIngestionService;
        _integrationReadinessService = integrationReadinessService;
        _integrationResultService = integrationResultService;
        _thirdPartyAccountSelectorService = thirdPartyAccountSelectorService;
    }
    /// <summary>
    /// Lista transacciones registradas usando filtros operativos opcionales.
    /// </summary>
    /// <param name="achCycleId">Identificador exacto del ciclo ACH usado como filtro.</param>
    /// <param name="achCycleName">Nombre exacto del ciclo ACH, después de eliminar espacios externos.</param>
    /// <param name="effectiveDate">Fecha efectiva usada para comparar únicamente la parte de fecha de EffectiveEntryDate.</param>
    /// <param name="clearingHouseId">Identificador de la cámara compensadora asociada al ciclo.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Listar transacciones")]
    [EndpointDescription("Devuelve las transacciones registradas y permite filtrar opcionalmente por identificador o nombre de ciclo, fecha efectiva y cámara compensadora. Los filtros se combinan cuando se proporciona más de uno.")]
    [HttpGet]
    [Authorize(Policy = P0Policies.TransactionsRead)]
    [ProducesResponseType(typeof(IEnumerable<AchTransactionListDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Colección de transacciones que cumplen los filtros solicitados.")]
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

    /// <summary>
    /// Consulta el catálogo activo de Company Entry Descriptions configurado en ACHInterbank.
    /// </summary>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar Company Entry Descriptions")]
    [EndpointDescription("Devuelve las Company Entry Descriptions activas configuradas en ACHInterbank, ordenadas por término. El resultado es un catálogo de configuración de la aplicación y no una definición normativa universal para todas las cámaras.")]
    [HttpGet("company-entry-descriptions")]
    [Authorize(Policy = P0Policies.TransactionsRead)]
    [ProducesResponseType(typeof(IEnumerable<CompanyEntryDescriptionDto>), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Catálogo activo de Company Entry Descriptions configurado en ACHInterbank.")]
    public async Task<IActionResult> GetCompanyEntryDescriptions(CancellationToken ct)
    {
        var items = await _transactionService.GetCompanyEntryDescriptionsAsync(ct);
        return Ok(items);
    }
    /// <summary>
    /// Consulta las cuentas canónicas de terceros para el contexto transaccional actualmente resuelto.
    /// </summary>
    [EndpointSummary("Consultar selector contextual de cuentas de terceros")]
    [EndpointDescription("Resuelve Customer, institución destino, cámara/ciclo, naturaleza, service type y fecha efectiva en backend; devuelve cada cuenta canónica con su EligibilityDecision y si puede seleccionarse. No usa CustomerThirdParty.Status como autoridad.")]
    [HttpGet("third-party-accounts/selector")]
    [Authorize(Policy = P0Policies.TransactionsRead)]
    [ProducesResponseType(typeof(TransactionThirdPartyAccountSelectorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetThirdPartyAccountSelector(
        [FromQuery] string sourceAccountNumber,
        [FromQuery] int destinationInstitutionId,
        [FromQuery] TransactionTypeEnum type,
        [FromQuery] int companyEntryDescriptionId,
        CancellationToken ct)
    {
        if (_thirdPartyAccountSelectorService is null)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { Message = "El selector contextual no está disponible." });
        }

        try
        {
            var result = await _thirdPartyAccountSelectorService.GetAsync(
                new TransactionThirdPartyAccountSelectorRequest(
                    sourceAccountNumber,
                    destinationInstitutionId,
                    type,
                    companyEntryDescriptionId),
                ct);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Evalúa la política transaccional con datos suministrados sin registrar una transacción.
    /// </summary>
    /// <param name="amount">Monto evaluado por las reglas y límites operativos aplicables.</param>
    /// <param name="transactionExternalId">Identificador operativo/idempotencia canónico de la transacción candidata, cuando se dispone de él.</param>
    /// <param name="reference">Referencia legada usada como identificador de respaldo cuando no se informa TransactionExternalId.</param>
    /// <param name="type">Tipo de transacción que se desea evaluar.</param>
    /// <param name="accountType">Tipo de cuenta o producto usado por la evaluación de políticas.</param>
    /// <param name="isPrenotification">Indica si la operación candidata corresponde a una prenotificación.</param>
    /// <param name="destinationInstitutionId">Identificador de la institución destino usado para resolver el contexto operativo.</param>
    /// <param name="sourceAccountNumber">Cuenta de origen incluida en la evaluación.</param>
    /// <param name="destinationAccountNumber">Cuenta de destino incluida en la evaluación.</param>
    /// <param name="companyIdentification">Identificación del originador incluida en los datos de evaluación.</param>
    /// <param name="recipientIdNumber">Identificación opcional del receptor incluida en los datos de evaluación.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Previsualizar política transaccional")]
    [EndpointDescription("Evalúa la política con los datos suministrados y devuelve el resultado sin persistir una transacción. CanSubmit indica el resultado de esta evaluación previa; no garantiza que un registro posterior complete todas las validaciones ni el procesamiento subsiguiente.")]
    [HttpGet("policies/preview")]
    [Authorize(Policy = P0Policies.TransactionsPolicyPreview)]
    [ProducesResponseType(typeof(TransactionPolicyPreview), StatusCodes.Status200OK)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Resultado de la evaluación previa de política para los datos suministrados.")]
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

    /// <summary>
    /// Registra una transacción individual después de aplicar las validaciones actuales del flujo de creación.
    /// </summary>
    /// <param name="request">Datos de la transacción que se desea registrar.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Registrar transacción individual")]
    [EndpointDescription("Registra una transacción individual. Las transacciones monetarias requieren Amount mayor que cero; para una prenotificación el validador del servicio exige Amount igual a cero. Debe informarse TransactionExternalId o Reference legado. Antes de persistir se validan, entre otros datos, las cuentas, el originador, los tipos de persona PN/PJ y la información del receptor cuando aplica. Una creación correcta responde 201.")]
    [HttpPost]
    [OpenApiRequestExample(typeof(TransactionCreateRequestExampleProvider))]
    [Authorize(Policy = P0Policies.TransactionsCreate)]
    [ProducesResponseType(typeof(AchTransaction), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [OpenApiResponseDescription(StatusCodes.Status201Created, "Transacción registrada correctamente.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "La solicitud incumple una validación de entrada o una regla de negocio capturada por la acción.")]
    [OpenApiResponseDescription(StatusCodes.Status500InternalServerError, "La acción capturó un error interno inesperado durante el registro de la transacción.")]
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



    /// <summary>
    /// Procesa una solicitud de ingestión masiva según la fuente y modalidad indicadas.
    /// </summary>
    /// <param name="request">Solicitud de ingestión masiva con la modalidad, fuente y datos de entrada.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Procesar ingestión masiva")]
    [EndpointDescription("Procesa la solicitud según ProcessingMode y SourceType. En modo Synchronous solo admite InlineTransactions y delega los ítems al registro masivo; en modo AsynchronousJob devuelve el estado NOT_IMPLEMENTED sin despachar un job. Se diferencia de POST /Transactions/bulk porque esta operación agrega la selección de fuente y modalidad alrededor del registro inline actual. El procesamiento síncrono reutiliza la resolución de lote existente, incluida la rama específica de CENIT cuando el ciclo resuelto está fuera de su ventana operativa; esa conducta no se generaliza a otras cámaras.")]
    [HttpPost("bulk/submit")]
    [OpenApiRequestExample(typeof(BulkIngestionRequestExampleProvider))]
    [OpenApiResponseExample(StatusCodes.Status200OK, typeof(BulkIngestionSuccessResponseExampleProvider))]
    [Authorize(Policy = P0Policies.TransactionsBulkSubmit)]
    [ProducesResponseType(typeof(BulkIngestionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Resultado de la modalidad de ingestión solicitada; en modo síncrono puede incluir el resultado inmediato del registro masivo.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "La solicitud incumple una validación global o usa un origen no soportado por el modo síncrono actual.")]
    [OpenApiResponseDescription(StatusCodes.Status500InternalServerError, "La acción capturó un error interno inesperado durante la ingestión masiva.")]
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

    /// <summary>
    /// Registra directamente un lote de transacciones suministradas en el request.
    /// </summary>
    /// <param name="request">Lote inline con referencia, ítems y tamaño de procesamiento opcional.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Registrar transacciones en lote")]
    [EndpointDescription("Registra directamente los ítems incluidos en Transactions. Primero valida el lote y después procesa cada ítem con las validaciones transaccionales actuales; los errores de validación o reglas capturados por ítem se reflejan en ItemResults mientras los ítems válidos se confirman por bloques. Por ello el resultado puede ser parcial y no representa una operación atómica global. Cuando la resolución del lote identifica CENIT fuera de su ventana operativa y el servicio de cola está disponible, el flujo actual encola la transacción; esta rama es específica de CENIT y no se aplica a otras cámaras. A diferencia de POST /Transactions/bulk/submit, esta ruta no selecciona SourceType ni ProcessingMode.")]
    [HttpPost("bulk")]
    [OpenApiRequestExample(typeof(BulkTransactionRequestExampleProvider))]
    [OpenApiResponseExample(StatusCodes.Status200OK, typeof(BulkTransactionSuccessResponseExampleProvider))]
    [Authorize(Policy = P0Policies.TransactionsBulkSubmit)]
    [ProducesResponseType(typeof(BulkAchTransactionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Resultado agregado del lote con totales, transacciones creadas y resultado individual de cada ítem procesado.")]
    [OpenApiResponseDescription(StatusCodes.Status400BadRequest, "La solicitud incumple una validación global del lote, como referencia o colección de transacciones inválida.")]
    [OpenApiResponseDescription(StatusCodes.Status500InternalServerError, "La acción capturó un error interno inesperado durante el registro masivo.")]
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

    /// <summary>
    /// Obtiene una transacción por su identificador interno.
    /// </summary>
    /// <param name="id">Identificador interno de la transacción.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar transacción")]
    [EndpointDescription("Devuelve la transacción identificada por id con las relaciones cargadas por el servicio actual. Si no existe, responde 404 con el mensaje de no encontrado que genera la acción.")]
    [HttpGet("{id:int}")]
    [Authorize(Policy = P0Policies.TransactionsRead)]
    [ProducesResponseType(typeof(AchTransaction), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Transacción encontrada.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe una transacción con el identificador indicado.")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var tx = await _transactionService.GetTransactionByIdAsync(id, ct);
        if (tx == null)
            return NotFound(new { message = $"No se encontró la transacción con ID {id}" });

        return Ok(tx);
    }

    /// <summary>
    /// Consulta el resultado técnico de integración asociado a una transacción.
    /// </summary>
    /// <param name="id">Identificador interno de la transacción consultada.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar resultado de integración")]
    [EndpointDescription("Devuelve el resultado más reciente y el historial de intentos de integración registrados para la transacción. Si la transacción existe pero aún no tiene intentos, responde 200 con Latest nulo y History vacío. El resultado describe estados técnicos y de negocio registrados por la integración; no equivale por sí solo a una confirmación de aceptación o compensación.")]
    [HttpGet("{id:int}/integration-result")]
    [Authorize(Policy = P0Policies.TransactionsRead)]
    [ProducesResponseType(typeof(TransactionIntegrationResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Resultado de integración de la transacción, con el último intento y su historial cuando existan registros.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe una transacción con el identificador indicado.")]
    [OpenApiResponseDescription(StatusCodes.Status503ServiceUnavailable, "El servicio de consulta de resultados de integración no está disponible.")]
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

    /// <summary>
    /// Consulta la preparación técnica de una transacción para su integración.
    /// </summary>
    /// <param name="id">Identificador interno de la transacción evaluada.</param>
    /// <param name="ct">Token de cancelación de la solicitud.</param>
    [EndpointSummary("Consultar readiness de integración")]
    [EndpointDescription("Resuelve la operación de integración correspondiente a la transacción y evalúa la disponibilidad de su configuración de mappings. El resultado informa si la operación es soportada, el motivo resuelto y el readiness técnico, incluidos mappings faltantes o inactivos, errores, advertencias y si puede construirse el payload. Esta consulta no transmite la transacción ni confirma que un procesamiento posterior resulte exitoso.")]
    [HttpGet("{id:int}/integration-readiness")]
    [Authorize(Policy = P0Policies.TransactionsRead)]
    [ProducesResponseType(typeof(TransactionIntegrationReadinessResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    [OpenApiResponseDescription(StatusCodes.Status200OK, "Evaluación de readiness técnico de la integración correspondiente a la transacción.")]
    [OpenApiResponseDescription(StatusCodes.Status404NotFound, "No existe una transacción con el identificador indicado.")]
    [OpenApiResponseDescription(StatusCodes.Status503ServiceUnavailable, "El servicio de readiness de integración no está disponible.")]
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

/// <summary>
/// Datos requeridos por el endpoint de registro individual de transacciones.
/// </summary>
public class AchTransactionRequest
{
    /// <summary>Monto de la transacción. Las monetarias requieren un valor mayor que cero y las prenotificaciones requieren cero en el validador actual.</summary>
    public decimal Amount { get; set; }

    /// <summary>Identificador operativo/idempotencia canónico de la instrucción. Puede omitirse únicamente cuando se utiliza Reference legado.</summary>
    public string? TransactionExternalId { get; set; }

    /// <summary>Referencia legada o transicional usada como respaldo cuando no se informa TransactionExternalId.</summary>
    public string Reference { get; set; } = string.Empty;

    /// <summary>Tipo de transacción que se desea registrar.</summary>
    public TransactionTypeEnum Type { get; set; }

    /// <summary>Tipo de cuenta o producto asociado a la transacción.</summary>
    public AccountTypeEnum AccountType { get; set; } = AccountTypeEnum.Checking;

    /// <summary>Indica si la instrucción corresponde a una prenotificación.</summary>
    public bool IsPrenotification { get; set; }

    /// <summary>Identificador de la institución financiera destino.</summary>
    public int DestinationInstitutionId { get; set; }

    /// <summary>Número de la cuenta de origen.</summary>
    public string SourceAccountNumber { get; set; } = string.Empty;

    /// <summary>Número de la cuenta de destino.</summary>
    public string DestinationAccountNumber { get; set; } = string.Empty;

    /// <summary>Identificación opcional del receptor; algunas validaciones del tipo de transacción pueden exigirla.</summary>
    public string? RecipientIdNumber { get; set; }

    /// <summary>Nombre opcional del receptor. Es requerido por la validación actual cuando se informa RecipientIdNumber.</summary>
    public string? RecipientName { get; set; }

    /// <summary>Indica si la transacción solicita validación de identidad del receptor dentro de las reglas aplicables.</summary>
    public bool RequiresIdentityValidation { get; set; }

    /// <summary>Nombre del originador usado por el flujo de registro.</summary>
    public string CompanyName { get; set; } = string.Empty;

    /// <summary>Identificación del originador usada por el flujo de registro.</summary>
    public string CompanyIdentification { get; set; } = string.Empty;

    /// <summary>Identificador de la Company Entry Description activa seleccionada para la transacción.</summary>
    public int CompanyEntryDescriptionId { get; set; }

    /// <summary>Tipo de persona opcional del originador. La validación actual acepta PN o PJ cuando se informa.</summary>
    public string? SourcePersonType { get; set; }

    /// <summary>Tipo de persona opcional del receptor. La validación actual acepta PN o PJ cuando se informa.</summary>
    public string? RecipientPersonType { get; set; }

    /// <summary>Addendas opcionales asociadas a la instrucción y validadas según sus campos reales.</summary>
    public List<AddendaDto>? Addendas { get; set; }
}
