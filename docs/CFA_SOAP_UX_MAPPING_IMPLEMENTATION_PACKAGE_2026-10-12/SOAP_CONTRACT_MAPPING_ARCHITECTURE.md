# Arquitectura de contrato SOAP y mapping dinámico

## 1. Objetivo

Los tres métodos CFA deben admitir evolución de parámetros de entrada sin modificar un request DTO o un `XElement` por cada campo nuevo.

La capacidad sin desarrollo se limita a cambios que el motor puede representar de forma segura:

- parámetros escalares;
- tipos soportados;
- fuentes ya registradas;
- transformaciones registradas;
- namespace/operación compatibles con el transport actual.

## 2. Contrato versionado

### 2.1 `IntegrationContractVersion`

Nueva entidad conceptual:

```text
Id: Guid
MethodId: int
VersionLabel: string
Status: Draft | Validated | Active | Retired
OperationName: string
TargetNamespace: string
SoapAction: string
SourceType: WsdlImport | ManualApproved | BaselineMigration
SourceDocumentName: string
SourceDocumentHash: string
Notes: string
Revision: long
ImportedAtUtc: DateTime?
ImportedBy: string
ActivatedAtUtc: DateTime?
ActivatedBy: string
```

Solo una versión puede estar `Active` por método.

### 2.2 `IntegrationMethodParameter`

Se conserva la entidad existente, pero pasa a pertenecer a un contrato:

```text
ContractVersionId: Guid
ParameterPath: string
XmlElementName: string
DisplayName: string
DescriptionEs: string
UiHelpText: string
Category: string
ExampleValue: string
DataType: string
XsdType: string
Direction: Input | Output
Cardinality: Scalar | Object | Collection
Required: bool
Nillable: bool
MinOccurs: int
MaxOccurs: int?   // null para unbounded si se soporta en futuro
SortOrder: int
IsActive: bool
```

`ParameterPath` continúa siendo la clave técnica estable para mapping. `XmlElementName` es la etiqueta exacta serializada.

Para el alcance inicial, activación runtime solo acepta `Scalar` para parámetros input. Objetos/colecciones se importan pero quedan con `SupportStatus=RequiresDevelopment` o bloquean la versión según el diseño final de DTO.

## 3. Mapping ligado al contrato

`IntegrationMappingSet` agrega:

```text
ContractVersionId: Guid
Revision: long
```

Reglas:

- un mapping set solo puede contener reglas para parámetros de su contrato;
- clonar mapping entre contratos compara `ParameterPath`;
- parámetros existentes se copian;
- parámetros nuevos quedan sin regla y marcados pendientes;
- parámetros retirados no se copian;
- published es inmutable.

## 4. Ciclo de vida del contrato

```text
Draft
  │ validate
  ▼
Validated
  │ activate + mapping válido
  ▼
Active
  │ nueva versión activada
  ▼
Retired
```

No existe edición directa de `Active` o `Retired`.

Para modificar el contrato:

```text
Active v1
   │ clone/import
   ▼
Draft v2
```

## 5. Importación WSDL segura

Crear `IIntegrationContractImporter`.

Implementación `WsdlIntegrationContractImporter`:

1. recibe bytes locales;
2. valida tamaño;
3. calcula SHA-256;
4. parsea XML con DTD y external entity resolution deshabilitados;
5. localiza la operación elegida;
6. resuelve message/element/sequence básicos;
7. genera descriptores de parámetros;
8. clasifica tipos soportados/no soportados;
9. compara contra contrato activo;
10. crea `Draft` únicamente después de confirmación del usuario/comando.

No descargar WSDL remoto desde una URL introducida en UI.

## 6. Registro manual aprobado

Para casos sin WSDL, un usuario con permiso técnico puede crear en una versión `Draft` un parámetro con:

- nombre técnico;
- elemento XML;
- namespace heredado o explícito;
- tipo;
- obligatorio;
- nillable;
- orden;
- referencia documental obligatoria.

Nunca se agrega directamente al contrato activo.

## 7. Diff de contrato

Modelo:

```text
Added
Removed
RequiredChanged
TypeChanged
CardinalityChanged
OrderChanged
NamespaceChanged
DirectionChanged
```

Clasificación de impacto:

```text
CompatibleNoMappingChange
MappingRequired
RequiresDevelopment
Breaking
```

Ejemplos:

- campo opcional escalar agregado → `MappingRequired` solo si se quiere poblar;
- campo obligatorio escalar agregado → `MappingRequired` y bloquea activación;
- string → int → `Breaking` hasta validar mapping;
- Scalar → Object → `RequiresDevelopment`;
- campo removido → reglas antiguas quedan solo en versión histórica.

## 8. Resolución genérica de request

Nueva interfaz:

```csharp
public interface IIntegrationRequestResolver
{
    Task<ResolvedIntegrationRequest> ResolveAsync(
        IntegrationExecutionContext context,
        CancellationToken ct = default);
}
```

Salida conceptual:

```text
ResolvedIntegrationRequest
  MethodCode
  ContractVersionId
  MappingSetId
  MappingVersion
  OperationName
  TargetNamespace
  SoapAction
  Parameters[]
```

Cada parámetro resuelto contiene:

```text
ParameterId
ParameterPath
XmlElementName
DataType
SortOrder
Required
Nillable
Value
SourceDescriptor
TransformationDescriptor
```

## 9. Proveedores de fuente

Eliminar gradualmente switches monolíticos de rutas usando Strategy + Registry.

Interfaz:

```csharp
public interface IIntegrationSourceValueProvider
{
    IntegrationSourceKindEnum SourceKind { get; }
    IReadOnlyCollection<IntegrationSourceDescriptor> Descriptors { get; }
    ValueTask<IntegrationSourceValue> ResolveAsync(
        IntegrationExecutionContext context,
        IntegrationSourceCatalogField field,
        CancellationToken ct);
}
```

Implementaciones previstas:

- `TransactionSourceValueProvider`
- `BatchSourceValueProvider`
- `CycleSourceValueProvider`
- `ClearingHouseSourceValueProvider`
- `NachaHeaderSourceValueProvider`
- `BatchHeaderSourceValueProvider`
- `EntryDetailSourceValueProvider`
- `AddendaRecordSourceValueProvider`
- `BatchControlSourceValueProvider`
- `FileControlSourceValueProvider`
- `PrenotificationSourceValueProvider`
- `DifferentialResponseSourceValueProvider`
- `ConstantSourceValueProvider`

No usar reflection sobre rutas suministradas por el usuario.

El catálogo de fuentes se sincroniza desde descriptors registrados por código. Así, una nueva fuente funcional requiere una extensión controlada del backend, pero un nuevo campo CFA que use una fuente ya registrada no requiere desarrollo.

## 10. Transformaciones

Reemplazar el catálogo estático por Strategy Registry:

```csharp
public interface IIntegrationTransformation
{
    string Code { get; }
    TransformationDescriptor Descriptor { get; }
    TransformationValidationResult Validate(string? mask);
    string? Apply(string? value, TransformationContext context);
}
```

Implementaciones iniciales mantienen comportamiento actual:

- Trim;
- Uppercase;
- Lowercase;
- PadLeft;
- PadRight;
- Substring;
- DateFormat;
- NumericFormat;
- NullIfEmpty;
- DefaultIfNull;
- Concat solo si existe contrato multi-source explícito y determinista.

No habilitar expresiones arbitrarias ni scripts.

## 11. Validación de tipos

Pipeline por parámetro:

```text
valor fuente
  ↓
transformación
  ↓
normalización
  ↓
conversión según DataType
  ↓
validación required/nillable
  ↓
valor serializable
```

Tipos mínimos:

- string;
- int;
- long;
- decimal;
- bool;
- date;
- datetime.

Las conversiones usan `InvariantCulture` para payload técnico.

## 12. Serializador SOAP dirigido por contrato

Interfaz:

```csharp
public interface ISoapRequestBodyWriter
{
    string Write(ResolvedIntegrationRequest request);
}
```

Reglas:

- operación y namespace provienen del contrato activo;
- elementos se ordenan por `SortOrder`;
- nombre XML proviene de `XmlElementName`;
- solo parámetros `Input` activos se serializan;
- opcional null/empty se omite si contrato lo permite;
- nillable solo usa `xsi:nil` cuando el contrato lo declara y la política del writer lo soporta;
- required faltante bloquea antes del transporte;
- valores se escapan mediante `XElement`, nunca concatenando XML;
- output parameters nunca se envían como input.

## 13. Adaptación de `Proc_Transacciones`

Objetivo final:

```text
Current eligibility/classification
       ↓
IntegrationExecutionContext
       ↓
Generic resolver
       ↓
Contract-driven writer
       ↓
WscfaachSoapClient
```

Los campos funcionales derivados como `DESTRAN`/`INFPAG` no se codifican en el writer SOAP. Se representan como fuentes/transformaciones o builders de dominio registrados detrás de un provider.

El método `ProcTransaccionesAsync(IReadOnlyDictionary<...>)` puede mantenerse como compatibility adapter durante transición.

## 14. Adaptación de `Proc_Contrapartidas`

Eliminar la necesidad de agregar una propiedad al `ProcContrapartidasRequestContract` por cada parámetro nuevo.

Transición:

1. mantener `ProcContrapartidasRequestContract` como baseline de paridad;
2. construir simultáneamente `ResolvedIntegrationRequest` en pruebas;
3. comparar XML exacto/semántico;
4. migrar runtime al resolver genérico;
5. retirar el contrato tipado cuando no existan callers productivos y las pruebas de paridad estén cerradas.

Los controles funcionales del resolver actual permanecen como specifications/providers; no se mueven a la UI.

## 15. Adaptación de `RegistrarRespuestaTransaccion`

`RegistrarRespuestaAchCommand` permanece como contexto funcional de la respuesta.

El mapper fijo:

```text
idCanal
nombreCanal
idTransaccion
idEstado
causal
idTransaccionAxon
descripcionCausal
```

se reemplaza gradualmente por:

```text
RegistrarRespuestaAchCommand
        ↓
DifferentialResponseSourceValueProvider
        ↓
Generic mapping resolver
        ↓
Contract-driven writer
        ↓
WsAxonRespuestaTransaccionesSoapClient
```

La versión inicial del contrato conserva exactamente los siete parámetros actuales. Un octavo campo escalar en una versión nueva puede mapearse sin cambiar el request mapper.

## 16. Respuestas SOAP

Este programa hace genérica la construcción del request, no obliga a hacer genéricos los response parsers.

Los parsers específicos continúan porque contienen semántica de negocio y clasificación de códigos.

Si CFA cambia estructura de respuesta, se crea una fase de desarrollo separada y versionada.

## 17. Activación atómica

Activar una nueva versión debe ejecutarse en transacción:

1. validar contrato;
2. validar mapping set asociado;
3. verificar que no existen tipos no soportados;
4. verificar cobertura de required inputs;
5. registrar snapshot/hash;
6. retirar contrato activo previo;
7. activar nuevo contrato;
8. publicar/activar mapping seleccionado;
9. persistir historia;
10. commit.

Cualquier fallo realiza rollback completo.

## 18. Rollback

Rollback no edita versiones existentes.

Debe:

- seleccionar una versión `Retired` compatible;
- validar que su mapping publicado siga disponible;
- ejecutar la misma transición atómica;
- registrar motivo/actor.

## 19. Resultado de extensibilidad

Después del programa:

### No requiere desarrollo

- nuevo campo input escalar;
- cambio de orden;
- optional → required, si fuente ya existe y mapping se completa;
- valor fijo permitido;
- cambio de mapping;
- cambio de transformación ya soportada.

### Requiere extensión acotada

- exponer una nueva fuente existente en el modelo;
- nueva transformación determinista.

### Requiere desarrollo completo

- objetos/colecciones no soportados;
- nuevos métodos;
- cambio de response envelope;
- autenticación/transporte nuevos;
- información que ACHInterbank no posee;
- nuevas reglas monetarias o de elegibilidad.
