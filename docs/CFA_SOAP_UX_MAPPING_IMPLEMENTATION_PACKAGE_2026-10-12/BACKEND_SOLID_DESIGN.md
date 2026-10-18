# Diseño backend — Clean Architecture, SOLID y patrones

## 1. Principios

### Single Responsibility

- contrato: describe CFA;
- mapping: describe origen/tratamiento;
- source provider: obtiene un valor;
- transformation: transforma un valor;
- resolver: orquesta resolución;
- writer: serializa XML;
- transport: envía HTTP/SOAP;
- response parser: interpreta resultado;
- activation service: cambia versión activa.

Ninguna clase debe resolver todas estas responsabilidades.

### Open/Closed

Agregar un parámetro CFA no modifica el writer.

Agregar una fuente nueva se implementa agregando un provider.

Agregar una transformación se implementa agregando una strategy.

### Liskov

Providers y transformations deben cumplir contratos deterministas y no modificar estado externo durante `Resolve/Apply`.

### Interface Segregation

No crear `IIntegrationService` gigante. Usar interfaces pequeñas para lectura, administración, resolución, importación y activación.

### Dependency Inversion

Application define interfaces. Persistence/External implementan acceso a DB, parsing WSDL y transporte.

## 2. Interfaces de Application

### Contratos

```csharp
public interface IIntegrationContractQueryService
{
    Task<IReadOnlyCollection<IntegrationContractVersionDto>> GetVersionsAsync(int methodId, CancellationToken ct);
    Task<IntegrationContractVersionDetailDto> GetAsync(Guid contractVersionId, CancellationToken ct);
    Task<IntegrationContractDiffDto> CompareAsync(Guid left, Guid right, CancellationToken ct);
}

public interface IIntegrationContractCommandService
{
    Task<IntegrationContractVersionDto> CloneAsync(...);
    Task<IntegrationContractVersionDto> ImportAsync(...);
    Task<IntegrationContractVersionDto> UpdatePresentationAsync(...);
    Task<IntegrationContractValidationDto> ValidateAsync(...);
}

public interface IIntegrationContractActivationService
{
    Task<IntegrationActivationResultDto> ActivateAsync(...);
    Task<IntegrationActivationResultDto> RollbackAsync(...);
}
```

### Mapping

Conservar `IIntegrationMappingSetService`, `IIntegrationMappingValidationService`, `IIntegrationMappingPreviewService`, ampliándolos con contexto de `ContractVersionId`.

### Runtime

```csharp
public interface IIntegrationExecutionContractResolver
{
    Task<IntegrationExecutionContract> ResolveActiveAsync(string methodCode, CancellationToken ct);
}

public interface IIntegrationRequestResolver
{
    Task<ResolvedIntegrationRequest> ResolveAsync(IntegrationExecutionContext context, CancellationToken ct);
}

public interface ISoapRequestBodyWriter
{
    string Write(ResolvedIntegrationRequest request);
}
```

## 3. Patterns

### Strategy

- `IIntegrationSourceValueProvider`;
- `IIntegrationTransformation`;
- response parsers ya específicos.

### Registry

```csharp
IntegrationSourceProviderRegistry
IntegrationTransformationRegistry
```

Registries validan duplicados al arrancar y fallan rápido si existen dos handlers para la misma clave.

### Adapter

- adapters de `ProcContrapartidasRequestContract` al request genérico durante transición;
- adapters de métodos existentes de `IWscfaachSoapClient` hacia operación genérica;
- adapter del command de respuesta diferencial al `IntegrationExecutionContext`.

### Builder

`ResolvedIntegrationRequestBuilder` puede consolidar parámetros resueltos y validarlos antes de exponer el objeto inmutable.

### Specification

Usar specifications/validators explícitos para:

- contrato activable;
- mapping publicable;
- tipo compatible;
- source compatible;
- runtime method eligibility ya existente.

No introducir un framework externo de specifications si no aporta valor; interfaces/servicios focales son suficientes.

### Facade

`IntegrationAdministrationFacade` solo para read model del SPA, agregando estado de contrato + mapping + conexión sin mezclar comandos internos.

### State machine explícita

Estados de contrato y mapping se validan mediante métodos/servicios; no aceptar transiciones arbitrarias desde el controlador.

## 4. `IntegrationExecutionContext`

Modelo inmutable con solo referencias/contextos necesarios:

```text
MethodCode
TransactionId?
CycleId?
IncomingNachaQueueId?
DifferentialResponseId?
CorrelationId
ExecutionDateUtc
```

Los providers cargan datos autorizados según el tipo. No pasar DbContext ni entidades arbitrarias entre capas.

## 5. Source provider descriptors

Cada provider expone metadata para el catálogo:

```text
SourceKind
SourceKey / FieldPath
DisplayNameEs
DescriptionEs
DataType
Nullable
FunctionalGroup
SortOrder
SensitiveClassification
```

Un servicio de sincronización actualiza `IntegrationSourceCatalogField` sin desactivar entradas pertenecientes a providers no cargados por error hasta validar el registro completo.

## 6. Sensitive data

Cada descriptor puede marcar:

```text
None
Personal
Account
Financial
Technical
```

La vista previa usa un `IIntegrationPreviewSanitizer` para masking.

El runtime usa valor completo únicamente dentro del pipeline de construcción y transporte. Logs y traces conservan valores sanitizados según política existente.

## 7. Transformation registry

Cada strategy declara compatibilidad:

```text
InputTypes
OutputType
RequiresMask
SupportsNull
DisplayNameEs
DescriptionEs
```

La UI recibe este catálogo desde API. No hardcodea transformaciones.

## 8. Contract importer boundary

`IIntegrationContractImporter` vive en Application.

El parser WSDL vive en una implementación de infraestructura/persistence dedicada, no en controller ni frontend.

Entrada:

```text
stream/bytes
method selector
source file name
actor
```

Salida:

```text
ContractDraftDescriptor
Parameters
UnsupportedFeatures
Warnings
DocumentHash
```

El command service decide persistencia.

## 9. Writer genérico

No debe consultar DB.

No debe conocer `AchTransaction`.

No debe decidir fuentes.

No debe transformar valores.

Recibe un request ya resuelto e inmutable y produce XML.

Esto permite unit tests puros y paridad exacta.

## 10. Transport

Mantener transporte existente y no-retry para operaciones sensibles.

Puede introducirse un contrato genérico interno:

```csharp
public interface ISoapOperationTransport
{
    Task<string> SendAsync(
        string clientCode,
        string operationName,
        string requestBody,
        CancellationToken ct);
}
```

`WscfaachSoapClient` y `WsAxonRespuestaTransaccionesSoapClient` pueden actuar como adapters durante la transición.

No unificar seguridad de ambos clientes silenciosamente dentro de este programa; cualquier hardening de endpoint debe ser compatible con la configuración actual y tener pruebas focales.

## 11. Compatibilidad con servicios actuales

### `Proc_Transacciones`

- conservar classification/eligibility actuales;
- migrar payload mapping al resolver genérico;
- preservar builders especializados como providers cuando exista semántica compleja;
- writer no conoce reglas de NACHA.

### `Proc_Contrapartidas`

- conservar `IProcContrapartidasFunctionalMappingResolver` durante transición;
- extraer sus resoluciones a providers/reusable rules;
- eliminar construcción fija XML al cerrar paridad.

### `RegistrarRespuestaTransaccion`

- conservar `RegistrarRespuestaAchCommand` y validator;
- reemplazar mapper fijo por source provider + mapping;
- mantener response parser/transporte existente.

## 12. Errores tipados

Usar códigos estables, no parsing de mensajes en UI.

Categorías:

```text
CONTRACT_*       contrato/versionado/importación
MAPPING_*        mapping/publicación
SOURCE_*         resolución de fuente
TRANSFORM_*      transformación
SERIALIZATION_*  construcción XML
ACTIVATION_*     cambio de versión
```

DTO de error API:

```json
{
  "code": "MAPPING_REQUIRED_PARAMETER_MISSING",
  "message": "Falta configurar un dato obligatorio.",
  "details": {
    "parameterId": 123,
    "parameterPath": "TIPOCLIENTE"
  },
  "correlationId": "..."
}
```

El frontend usa `code` para presentar una frase funcional; el backend sigue siendo la autoridad.

## 13. Concurrencia

Contratos y mapping drafts deben usar `Revision` como token provider-neutral.

Todo update recibe `expectedRevision`.

Conflicto retorna `409` con código:

```text
CONTRACT_CONCURRENCY_CONFLICT
MAPPING_CONCURRENCY_CONFLICT
```

La UI ofrece recargar/comparar; no sobrescribe silenciosamente.

## 14. Feature flags de transición

Ejemplo:

```text
Integrations:ContractDrivenRuntime:ProcTransacciones
Integrations:ContractDrivenRuntime:ProcContrapartidas
Integrations:ContractDrivenRuntime:RegistrarRespuesta
```

Uso temporal:

- comparar resultados en tests;
- activar por servicio;
- rollback sin migración destructiva.

Eliminar flags al cerrar P8.
