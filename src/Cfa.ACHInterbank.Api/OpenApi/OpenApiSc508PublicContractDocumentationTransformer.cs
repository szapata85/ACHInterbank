using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;
using System.Text.RegularExpressions;

namespace Cfa.ACHInterbank.Api.OpenApi;

/// <summary>
/// Completa únicamente Summary/Description faltantes o placeholder para las operaciones
/// promovidas a contrato público por SC-508. No altera routing, responses, schemas ni seguridad.
/// </summary>
public sealed class OpenApiSc508PublicContractDocumentationTransformer : IOpenApiOperationTransformer
{
    private sealed record Documentation(string Summary, string Description);

    private static readonly Regex GenericModulePlaceholder = new(
        @"^Operación del módulo [^.!?]+\.?$",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly IReadOnlyDictionary<(string Controller, string Action), Documentation> DocumentationByAction =
        new Dictionary<(string Controller, string Action), Documentation>
        {
            [("AchClaims", "Query")] = new(
                "Consultar casos Claims",
                "Consulta los casos Claims usando los filtros publicados por ClaimCaseQuery y devuelve el resultado paginado del servicio de Claims."),
            [("AchClaims", "GetByTransaction")] = new(
                "Consultar Claims por transacción ACH",
                "Consulta los casos Claims relacionados con la transacción ACH indicada y conserva las respuestas declaradas por la acción cuando la transacción no existe."),
            [("AchClaims", "CreateDev14")] = new(
                "Crear caso Claims DEV14",
                "Crea un caso Claims DEV14 a partir del request publicado. Usa Idempotency-Key cuando el cliente lo envía y devuelve el caso creado o el error funcional declarado por la acción."),
            [("AchClaims", "GetById")] = new(
                "Consultar detalle de caso Claims",
                "Consulta un caso Claims por identificador y devuelve su detalle cuando existe, conservando la respuesta de no encontrado declarada por la acción."),
            [("AchClaims", "Expire")] = new(
                "Cerrar caso Claims por expiración",
                "Registra el cierre por expiración del caso Claims indicado usando el request publicado y conserva el control de idempotencia de la operación."),
            [("AchClaims", "RecordProgress")] = new(
                "Registrar avance de caso Claims",
                "Registra un avance sobre el caso Claims indicado usando el request publicado y conserva el control de idempotencia de la operación."),
            [("AchClaims", "Resolve")] = new(
                "Resolver y cerrar caso Claims",
                "Resuelve el caso Claims indicado usando el request publicado y devuelve el detalle actualizado o los errores funcionales declarados por la acción."),

            [("AchOutboundReturnTransport", "DispatchExisting")] = new(
                "Despachar archivo de devoluciones existente",
                "Despacha por el transporte configurado el archivo de devoluciones identificado por fileName usando la clave de idempotencia del request."),
            [("AchOutboundReturnTransport", "GenerateAndDispatch")] = new(
                "Generar y despachar archivo de devoluciones",
                "Genera el archivo de devoluciones con el request de generación publicado y solicita su despacho por el transporte configurado en una sola operación."),
            [("AchOutboundReturnTransport", "ProcessResult")] = new(
                "Procesar resultado de transporte de devoluciones",
                "Procesa el resultado reportado por el transporte para un despacho de devoluciones y devuelve el resultado de procesamiento persistido por el servicio."),

            [("AchReconciliation", "GetDashboard")] = new(
                "Consultar dashboard de conciliación ACH",
                "Consulta el resumen de conciliación ACH calculado por el read model vigente."),
            [("AchReconciliation", "GetExceptions")] = new(
                "Listar excepciones de conciliación ACH",
                "Lista casos de conciliación aplicando los filtros opcionales de cámara y estado publicados por la acción."),
            [("AchReconciliation", "GetException")] = new(
                "Consultar excepción de conciliación ACH",
                "Consulta un caso de conciliación por identificador dentro del conjunto de casos disponible y conserva la respuesta de no encontrado declarada por la acción."),
            [("AchReconciliation", "ResolveException")] = new(
                "Resolver excepción de conciliación ACH",
                "Resuelve el caso de conciliación indicado con control de versión, resolución, motivo y correlación suministrados por el request."),
            [("AchReconciliation", "GetItems")] = new(
                "Listar ítems de conciliación ACH",
                "Consulta los ítems del read model de conciliación ACH disponibles en el estado actual."),
            [("AchReconciliation", "GetItemByCorrelation")] = new(
                "Consultar conciliación por correlación",
                "Consulta el detalle de conciliación asociado al correlationId indicado y conserva la respuesta de no encontrado declarada por la acción."),
            [("AchReconciliation", "GetItem")] = new(
                "Consultar detalle de conciliación ACH",
                "Consulta el detalle de conciliación identificado por reconciliationId y conserva la respuesta de no encontrado declarada por la acción."),

            [("BankHolidays", "GetAll")] = new(
                "Listar festivos bancarios",
                "Consulta los festivos bancarios administrados y permite filtrar opcionalmente por año."),
            [("BankHolidays", "Create")] = new(
                "Crear festivo bancario",
                "Crea un festivo bancario con los datos de BankHolidayDto y devuelve el registro resultante del servicio administrativo."),
            [("BankHolidays", "Ensure")] = new(
                "Asegurar años de calendario bancario",
                "Solicita al servicio de aprovisionamiento asegurar los años indicados en BankHolidayEnsureRequest y devuelve el resultado del aprovisionamiento."),
            [("BankHolidays", "Delete")] = new(
                "Eliminar festivo bancario",
                "Elimina el festivo bancario identificado por id y responde sin contenido cuando el servicio completa la operación."),
            [("BankHolidays", "Update")] = new(
                "Actualizar festivo bancario",
                "Actualiza el festivo bancario suministrado cuando el id de la ruta coincide con el id del body; una discrepancia se rechaza como solicitud inválida."),

            [("Branding", "GetBrandingAsync")] = new(
                "Consultar configuración de branding",
                "Obtiene la configuración de branding vigente. La operación es anónima y de solo lectura."),
            [("Branding", "SaveBrandingAsync")] = new(
                "Actualizar configuración de branding",
                "Guarda la configuración de branding recibida en el body y devuelve la configuración resultante."),

            [("CatalogTypes", "GetAll")] = new(
                "Listar valores de un tipo de catálogo",
                "Consulta los valores del tipo de catálogo indicado por catalogType y conserva la respuesta de validación declarada por la acción cuando el tipo no es válido."),
            [("CatalogTypes", "Create")] = new(
                "Crear valor de tipo de catálogo",
                "Crea un valor dentro del tipo de catálogo indicado usando CatalogTypeUpsertRequest; los errores de validación y conflictos se conservan como respuestas de la acción."),
            [("CatalogTypes", "Delete")] = new(
                "Eliminar valor de tipo de catálogo",
                "Elimina el código indicado dentro del tipo de catálogo solicitado; conserva las respuestas de validación, no encontrado y conflicto producidas por la acción."),
            [("CatalogTypes", "Update")] = new(
                "Actualizar valor de tipo de catálogo",
                "Actualiza el código indicado dentro del tipo de catálogo solicitado usando CatalogTypeUpsertRequest y conserva las respuestas de validación y no encontrado de la acción."),

            [("ClearingHouseCycleConfigs", "GetByClearingHouse")] = new(
                "Listar configuraciones de ciclo por cámara",
                "Consulta las versiones de configuración de ciclo para la cámara indicada y permite evaluar su vigencia en effectiveAt cuando se suministra."),
            [("ClearingHouseCycleConfigs", "CreateVersion")] = new(
                "Crear versión de configuración de ciclos",
                "Crea una nueva versión de configuración de ciclos de cámara con UpsertClearingHouseCycleConfigDto y devuelve la versión resultante."),
            [("ClearingHouseCycleConfigs", "GetCurrentByClearingHouse")] = new(
                "Consultar configuración de ciclo vigente",
                "Consulta la configuración de ciclo vigente para la cámara indicada y el instante effectiveAt cuando se suministra."),
            [("ClearingHouseCycleConfigs", "Inactivate")] = new(
                "Inactivar configuración de ciclo",
                "Inactiva la configuración de ciclo identificada por id usando EffectiveTo del request publicado."),
            [("ClearingHouseCycleConfigs", "ChangeStatus")] = new(
                "Cambiar estado de configuración de ciclo",
                "Cambia el estado activo de la configuración de ciclo indicada y aplica EffectiveTo cuando el request lo suministra."),

            [("ClearingHouseSpecialDates", "GetAll")] = new(
                "Listar fechas especiales de cámaras",
                "Consulta las fechas especiales configuradas y permite filtrar por año y cámara compensadora."),
            [("ClearingHouseSpecialDates", "Create")] = new(
                "Crear fecha especial de cámara",
                "Crea una fecha especial para una cámara compensadora y devuelve conflicto cuando la fecha ya está configurada según la validación del servicio."),
            [("ClearingHouseSpecialDates", "Update")] = new(
                "Actualizar fecha especial de cámara",
                "Actualiza la fecha especial suministrada cuando el id de ruta coincide con el body y conserva el conflicto por configuración duplicada producido por el servicio."),
            [("ClearingHouseSpecialDates", "ChangeStatus")] = new(
                "Cambiar estado de fecha especial",
                "Activa o desactiva la fecha especial identificada por id según IsActive del request."),

            [("CompanyEntryDescriptions", "GetAll")] = new(
                "Listar descripciones de entrada de compañía",
                "Consulta el catálogo administrado de descripciones de entrada de compañía."),
            [("CompanyEntryDescriptions", "Create")] = new(
                "Crear descripción de entrada de compañía",
                "Crea una descripción de entrada de compañía con CompanyEntryDescriptionUpsertRequest y conserva los errores de validación o conflicto producidos por el servicio."),
            [("CompanyEntryDescriptions", "Delete")] = new(
                "Eliminar descripción de entrada de compañía",
                "Elimina la descripción de entrada de compañía identificada por id y conserva las respuestas de no encontrado o conflicto producidas por el servicio."),
            [("CompanyEntryDescriptions", "Update")] = new(
                "Actualizar descripción de entrada de compañía",
                "Actualiza la descripción de entrada de compañía identificada por id usando CompanyEntryDescriptionUpsertRequest y conserva los errores declarados por la acción."),

            [("CustomerThirdParties", "Get")] = new(
                "Consultar terceros de clientes",
                "Consulta terceros de clientes con los filtros publicados de búsqueda, cuentas, identificación, institución, estado y paginación."),

            [("Customers", "GetAll")] = new(
                "Listar clientes",
                "Obtiene el listado de clientes registrados mediante el servicio de clientes."),
            [("Customers", "Create")] = new(
                "Crear cliente",
                "Registra un cliente con SaveCustomerRequest y conserva las respuestas de creación y validación declaradas por la acción."),
            [("Customers", "Delete")] = new(
                "Eliminar cliente",
                "Elimina el cliente identificado por id y conserva la respuesta de no encontrado declarada por la acción."),
            [("Customers", "GetById")] = new(
                "Consultar cliente",
                "Obtiene el detalle del cliente identificado por id y conserva la respuesta de no encontrado declarada por la acción."),
            [("Customers", "Update")] = new(
                "Actualizar cliente",
                "Actualiza el cliente identificado por id con SaveCustomerRequest y conserva las respuestas de solicitud inválida o cliente no encontrado."),

            [("FinancialInstitutions", "GetAll")] = new(
                "Listar instituciones financieras",
                "Consulta las instituciones financieras y permite incluir registros inactivos mediante includeInactive."),
            [("FinancialInstitutions", "Create")] = new(
                "Crear institución financiera",
                "Crea una institución financiera con FinancialInstitutionDto y devuelve el resultado del servicio."),
            [("FinancialInstitutions", "GetById")] = new(
                "Consultar institución financiera",
                "Consulta la institución financiera identificada por id mediante el servicio administrativo."),
            [("FinancialInstitutions", "Update")] = new(
                "Actualizar institución financiera",
                "Actualiza la institución financiera suministrada cuando el id de ruta coincide con el id del body."),
            [("FinancialInstitutions", "SetStatus")] = new(
                "Cambiar estado de institución financiera",
                "Cambia el estado de la institución financiera identificada por id usando el valor FinancialInstitutionStatus recibido en el body."),

            [("InstitutionClearingHousePreferences", "GetAll")] = new(
                "Listar preferencias de cámara por institución",
                "Consulta las preferencias administradas que relacionan instituciones con cámaras compensadoras."),
            [("InstitutionClearingHousePreferences", "Create")] = new(
                "Crear preferencia de cámara por institución",
                "Crea una preferencia de cámara para una institución usando InstitutionClearingHousePreferenceDto."),
            [("InstitutionClearingHousePreferences", "Delete")] = new(
                "Eliminar preferencia de cámara por institución",
                "Elimina la preferencia de cámara por institución identificada por id y responde sin contenido al completar la operación."),
            [("InstitutionClearingHousePreferences", "Update")] = new(
                "Actualizar preferencia de cámara por institución",
                "Actualiza los campos isDefault, priority e isActive presentes en el objeto JSON recibido para la preferencia identificada por id."),

            [("LoginLockoutSettings", "GetAsync")] = new(
                "Consultar configuración de bloqueo de inicio de sesión",
                "Obtiene la configuración vigente de bloqueo de inicio de sesión administrada por el servicio de seguridad."),
            [("LoginLockoutSettings", "SaveAsync")] = new(
                "Actualizar configuración de bloqueo de inicio de sesión",
                "Guarda LoginLockoutSettingsDto y devuelve la configuración resultante del servicio de seguridad."),

            [("Nacha", "SaveHeader")] = new(
                "Guardar encabezado NACHA-M",
                "Persiste el NachaHeader recibido mediante el servicio NACHA-M y conserva la respuesta declarada por la acción."),

            [("Oauths", "GenerateToken")] = new(
                "Generar token de cliente",
                "Valida TokenModelClient con el validador configurado y genera un token de cliente, conservando las respuestas de validación y éxito declaradas por la acción."),
            [("Oauths", "GenerateTokenAsync")] = new(
                "Generar token a partir de assertion",
                "Genera un token de cliente a partir de la assertion recibida en el body y conserva la respuesta declarada por la acción."),

            [("PasswordRules", "GetRulesAsync")] = new(
                "Consultar reglas de contraseña",
                "Obtiene las reglas de contraseña vigentes administradas por el servicio de seguridad."),
            [("PasswordRules", "SaveRulesAsync")] = new(
                "Actualizar reglas de contraseña",
                "Guarda PasswordRulesDto y devuelve las reglas de contraseña resultantes del servicio de seguridad."),

            [("Permissions", "GetPermissionsAsync")] = new(
                "Listar permisos",
                "Consulta el catálogo de permisos disponible para administración de seguridad."),

            [("Prenotifications", "GetByReference")] = new(
                "Consultar prenotificación por referencia",
                "Consulta el estado de prenotificación asociado a la referencia indicada y conserva la respuesta de no encontrado declarada por la acción."),
            [("Prenotifications", "GetById")] = new(
                "Consultar prenotificación por identificador",
                "Consulta el estado de prenotificación identificado por id y conserva la respuesta de no encontrado declarada por la acción."),

            [("RegulatoryCatalogs", "GetFileRejectionCodes")] = new(
                "Consultar causales de rechazo de archivo",
                "Consulta el catálogo regulatorio de causales de rechazo de archivo expuesto por el servicio ACH."),
            [("RegulatoryCatalogs", "GetPrenotificationPolicies")] = new(
                "Consultar políticas de prenotificación",
                "Consulta las políticas regulatorias de prenotificación expuestas por el servicio ACH."),
            [("RegulatoryCatalogs", "GetReturnCodes")] = new(
                "Consultar causales de devolución",
                "Consulta las causales de devolución y permite limitar el resultado por clearingHouseId o clearingHouseCode cuando se suministran."),
            [("RegulatoryCatalogs", "GetReturnOfReturnPolicies")] = new(
                "Consultar políticas de devolución de devolución",
                "Consulta las políticas regulatorias de devolución de devolución expuestas por el servicio ACH."),
            [("RegulatoryCatalogs", "GetReturnPolicies")] = new(
                "Consultar políticas de devolución",
                "Consulta las políticas regulatorias de devolución expuestas por el servicio ACH."),
            [("RegulatoryCatalogs", "GetTransactionTypePolicies")] = new(
                "Consultar políticas por tipo de transacción",
                "Consulta las políticas regulatorias asociadas a tipos de transacción ACH."),

            [("Roles", "GetRolesAsync")] = new(
                "Listar roles",
                "Consulta el catálogo de roles disponible para administración de usuarios y permisos."),

            [("Users", "GetUsersAsync")] = new(
                "Listar usuarios",
                "Consulta usuarios con filtros opcionales de búsqueda y rol y con los parámetros de paginación publicados por la acción."),
            [("Users", "CreateUserAsync")] = new(
                "Crear usuario",
                "Crea un usuario con SaveUserRequest y devuelve el recurso creado o las respuestas de validación, conflicto o error inesperado producidas por la acción."),
            [("Users", "ValidateEmailDomainAsync")] = new(
                "Validar dominio de correo",
                "Valida el dominio del correo suministrado usando la política configurada por el servicio de usuarios."),
            [("Users", "DeactivateUserAsync")] = new(
                "Desactivar usuario",
                "Desactiva el usuario identificado por id y conserva las respuestas de éxito y no encontrado declaradas por la acción."),
            [("Users", "GetUserAsync")] = new(
                "Consultar usuario",
                "Consulta el usuario identificado por id y conserva la respuesta de no encontrado declarada por la acción."),
            [("Users", "UpdateUserAsync")] = new(
                "Actualizar usuario",
                "Actualiza el usuario identificado por id con SaveUserRequest y conserva las respuestas de validación, conflicto, no encontrado y error inesperado de la acción."),
            [("Users", "AssignRolesAsync")] = new(
                "Asignar roles a usuario",
                "Asigna al usuario identificado por id los roles indicados en AssignRolesRequest y conserva las respuestas de validación, no encontrado y error inesperado de la acción.")
        };

    public Task TransformAsync(
        OpenApiOperation operation,
        OpenApiOperationTransformerContext context,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (context.Description.ActionDescriptor is not ControllerActionDescriptor action
            || !DocumentationByAction.TryGetValue((action.ControllerName, action.ActionName), out var documentation))
        {
            return Task.CompletedTask;
        }

        if (IsIncomplete(operation.Summary))
        {
            operation.Summary = documentation.Summary;
        }

        if (IsIncomplete(operation.Description))
        {
            operation.Description = documentation.Description;
        }

        return Task.CompletedTask;
    }

    private static bool IsIncomplete(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        var normalized = string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return string.Equals(normalized, "Endpoint de la API ACH Interbank.", StringComparison.OrdinalIgnoreCase)
               || string.Equals(normalized, "Consulta información del sistema.", StringComparison.OrdinalIgnoreCase)
               || string.Equals(normalized, "DOCUMENTATION INCOMPLETE", StringComparison.OrdinalIgnoreCase)
               || GenericModulePlaceholder.IsMatch(normalized);
    }
}
