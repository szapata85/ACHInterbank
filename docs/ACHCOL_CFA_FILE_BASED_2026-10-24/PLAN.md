> **Avance F6.2 (2026-10-25):** observabilidad ACHCOL añadida al resumen existente del Command Center (6 conteos agregados sin datos bancarios), tests escritos y `F6_2_OPERATIONS_RUNBOOK.md` de despliegue/rollback controlado. No se habilita `Live` ni se declaran pruebas .NET/BD/CFA aprobadas. **Siguiente: F6.3**; la deuda ejecutable de F6.1 sigue abierta.

# Plan de implementación — Recepción y despacho NACHA-M ACH Colombia sin ciclo operativo parametrizado

**Proyecto:** ACHINTERBANK  
**Alcance:** Archivos NACHA-M entrantes de ACH Colombia (`ACHCOL`), interpretación del ciclo declarado en el nombre externo y despacho CFA SOAP `WSCFAACH.Proc_Transacciones`; validar también generación del nombre en flujos salientes cuando aplique.  
**Base de análisis:** código del paquete `ACHInterbank_ajustado.zip`, manual ACH Colombia V35 (abril de 2026), WSDL WSCFAACH suministrado.  
**Estado actual:** F0 documental completado; F1.1/F1.2, F2.1/F2.2/F2.3 y F3.1/F3.2/F3.3 y F4.1/F4.2 **implementados en código, sin compilación ni ejecución de xUnit/migraciones reales**. PostgreSQL (F3.1) y SQL Server (F3.2) cuentan con migraciones de la cola, pendientes de aplicar/verificar; F3.3 implementado en código, pendiente verificación; F4.3 implementado sin verificación; F5.1 añade matriz de 25 entradas; F5.2 añade proyección de solo lectura `EntryDetail` a SOAP parcial DryRun sin autorización de envío; F5.3 incorpora guard local y harness de contrato, con evidencia redactada. F6.1 auditó el proyecto offline (12/12 checks estáticos), endureció el XML DryRun, sin ejecutar .NET/BD. F6.2 es el siguiente paso; las pruebas ejecutables F6.1 siguen pendientes.

## Aclaración vinculante F0.3.A — nombres físicos y sobres digitales

El **nombre físico entrante ACH Colombia** termina en `.OUT` cuando el NACHA-M está **en claro** o `.OUT.env` cuando el archivo está **cifrado en sobre digital**. La forma `RRRRTTT.ZZZ.N` citada en apartados previos es representación **lógica parcial**, no patrón físico completo. El código del proyecto contiene el ejemplo `0001283.001.20260731.1.OUT.env` y su nombre canónico posterior al descifrado `...1.OUT`. El soporte para `.8/.9` aplica al segmento numérico **antes** de `.OUT`, por ejemplo `0001283.008.20260731.8.OUT` y `0001283.009.20260731.9.OUT.env`. Preservar trazabilidad de nombre externo, nombre canónico, byte hashes y `ExternalCycleNumber` sin inferir `AchCycleId`.

**Brecha de implementación (resuelta en código F1.2, pruebas pendientes):** Managed Folder y SFTP fueron ampliados para descubrir `.OUT` en claro además de `.OUT.env`/`.RET.env`, sin alterar la semántica de RET. Se reutiliza el servicio existente de descifrado para `.OUT.env`. Agregar **AT-21–AT-28** a F6. Véase [`F0_3_A_NAMING_OUT_ENVELOPE.md`](F0_3_A_NAMING_OUT_ENVELOPE.md). Esta corrección no bloquea el desarrollo a falta de ambiente.

## Avance F2.3 — persistencia del contexto físico ACHCOL

F2.3 ya incorpora cinco columnas opcionales en la ingesta: `CanonicalPlainFileName`, `FileIdModifier`, `FileSequence`, `ExternalCycleNumber` y `FileProtectionKind`. `FileName` y `FileNameDate` ya existían; el primero mantiene el nombre original `.OUT.env` cuando aplica. La información se materializa después de la correlación tipo 1 y antes de validación de admisión; sobrevive a bloqueos posteriores y puede consultarse en respuesta de ingesta y detalle del Command Center. `ResolvedAchCycleId` continúa nullable. Las migraciones **PostgreSQL y SQL Server del contexto de archivo** se añadieron ahora (F2.3), por ser parte del alcance funcional de persistencia; F3.1–F3.2 tratarán **esquema de cola y despacho**, sin recrear estas columnas.

**Estado:** `IMPLEMENTED_UNVERIFIED`; código/migraciones/tests escritos, ambientes y pruebas finales aún no ejecutados. Véase [`F2_3_IMPLEMENTATION.md`](F2_3_IMPLEMENTATION.md).

## Avance F3.1 — cola única discriminada, esquema PostgreSQL

F3.1 implementó `SourceKind` con default `CycleBound`, `EntryDetailId` nullable en columna con FK `Restrict`, y `AchTransactionId?`/`AchCycleId?` opcionales en el modelo. Una **check constraint por origen** requiere transacción/ciclo en `CycleBound` y detalle NACHA en `AchColFileBased`. Se mantiene el índice único actual de `IdempotencyDispatchKey`; su cómputo canónico para la nueva fuente será F3.3. La migración `20261024213000_AchColFileBasedDispatchQueueF31.cs` actualiza **PostgreSQL** y su snapshot, sin tocar los metadatos F2.3. El worker legacy filtra `CycleBound` hasta la habilitación controlada de la nueva ruta en F4.

**No desplegar F3.1 en un entorno SQL Server hasta realizar F3.2.** Código y pruebas SQLite fueron incorporados pero no compilados/ejecutados; reportar estado `IMPLEMENTED_UNVERIFIED`. Consulte [`F3_1_IMPLEMENTATION.md`](F3_1_IMPLEMENTATION.md).

## 1. Objetivo y decisión funcional

Permitir la recepción, validación, persistencia, clasificación y despacho de transacciones elegibles incluidas en archivos ACH Colombia cuyo **identificador de archivo** sea `H` (consecutivo 008), `I` (009), u otro identificador oficial válido, aun cuando ACHINTERBANK no tenga parametrizado un ciclo operativo 8, 9 o de cualquier otro número correspondiente al consecutivo del archivo.

**Invariante funcional:** `FileIdModifier`/consecutivo de archivo y `ExternalCycleNumber` del nombre **no equivalen** a `AchCycleId`/ciclo operativo persistido. El despacho no puede bloquearse **solamente** porque no exista un ciclo operativo correspondiente al consecutivo ni al número declarado en el nombre. No se eliminan las validaciones de formato, cámara, autorización, duplicidad, correlación, elegibilidad de transacción, seguridad, mapeo SOAP ni conciliación.

Los identificadores ACH Colombia V35 son `A–Z ↔ 001–026` y `0–9 ↔ 027–036`. El manual adjunto describe `RRRRTTT.ZZZ.1` para el caso documentado. **Requisito adicional confirmado por el solicitante:** en el nombre que participa en el procesamiento interno de CFA, el último componente puede contener también ciclos `8`, `9` y superiores: `RRRRTTT.ZZZ.N`, donde `N` es un entero positivo válido y **no está limitado a 1–7 ni requiere una fila `AchCycle` con ese número**. La aceptación de `.8`/`.9` en intercambios directos con ACH Colombia, más allá del flujo interno CFA, deberá homologarse expresamente contra su contrato de nombres; no se afirma que el manual V35 ya lo contemple.

**Tres valores distintos — nunca fusionarlos:**

| Campo | Ejemplo | Semántica |
|---|---|---|
| `FileIdModifier` del encabezado (registro 1) | `H` | Identificador del archivo |
| `ZZZ` del nombre externo | `008` | Consecutivo diario del archivo; contrastar contra `H` según perfil |
| `N` final del nombre externo | `8` | Número de ciclo **declarado externamente**; conservar aunque no exista ciclo interno |
| `AchCycleId` | `null` | Relación opcional a ciclo operativo parametrizado en ACHINTERBANK |

Ejemplos ilustrativos de nombre externo: `1234567.008.8` con encabezado `H`; `1234567.009.9` con encabezado `I`. `1234567` es un origen ficticio. El `8`/`9` del sufijo debe poder viajar por el flujo de interpretación, persistencia y despacho SOAP **sin transformarse obligatoriamente en una FK `AchCycleId`**. El sufijo y `ZZZ` son conceptualmente independientes; no exigir igualdad salvo regla de negocio formalmente homologada.

### Resultado observable obligatorio

1. Se recibe un archivo válido `1234567.008.8` con `FileIdModifier=H`, consecutivo `008` y **ciclo externo declarado 8**, sin `AchCycle` 8; no se rechaza por ciclo inexistente.
2. Se valida y persiste el archivo y sus registros NACHA-M; se mantiene identificación externa y auditoría.
3. Las transacciones **que califican** para `Proc_Transacciones` se encolan y producen payload SOAP conforme a mapeo publicado.
4. Con un entorno CFA autorizado en modo `Live`, se transmite el payload, se interpreta la respuesta y se registra resultado por transacción.
5. El mismo resultado se cumple con `1234567.009.9` e `I=009`; la aceptación no depende de que el ciclo externo 9 exista en parámetros. Archivos CENIT y flujo ACH con ciclos normales mantienen su comportamiento.

## 2. Hallazgos confirmados en el código

| Componente | Estado actual y bloqueo | Acción |
|---|---|---|
| `NachaFileIdentifierMapConfiguration.cs` | Tiene secuencias 1–36 (`A–Z`,`0–9`). | **Reutilizar**, agregar pruebas de correspondencia y unicidad. |
| `IncomingNachaCycleResolver.cs` | Extrae ciclo del nombre **solo con parser de nomenclatura CENIT**; en ACHCOL recurre a cámara/fecha/ventanas. Si falta `AchCycle`, devuelve `NoResuelto`. | Reconocer `RRRRTTT.ZZZ.N` con parser ACHCOL separado; devolver `ExternalCycleNumber=N` aunque no haya `AchCycle`; asociar ciclo interno **opcionalmente**. |
| `IncomingNachaIngestionAppService.cs` | Si `resolution.IsResolved == false` aborta antes de parsear (`PendienteResolucion`, `NoEjecutado`). | Aceptar ruta ACHCOL válida sin ciclo; no continuar cuando cámara/fecha/formato sean inválidos. |
| `IncomingNachaAdmissionValidator.cs` | Extrae ciclo de archivo con `CenitOfficialFileNameParser`; exige `AchCycleId` configurado y valida ventana. | Parsear nomenclatura ACHCOL `RRRRTTT.ZZZ.N` para `N=8,9,...`; no exigir que `N` exista como `AchCycle` y mantener los controles ajenos a la existencia del ciclo. |
| `NachaParserService.cs` | Conserva `FileIdModifier`, pero usa búsquedas de ciclo; `CycleNumber` se origina del nombre CENIT. | Persistir campos separados: identificador, consecutivo y ciclo opcional; no inferir `AchCycleId` del consecutivo. |
| `IncomingNachaDispatchEligibilityPolicy.cs` | Bloquea si `ResolvedAchCycleId` está vacío y usa `transaction.AchCycle` para ventana y política. | Implementar elegibilidad para contexto ACHCOL sin ciclo; conservar filtros de clase funcional y linking seguro. |
| `IncomingNachaDispatchPlanner.cs` / `IncomingNachaDispatchQueue` | Exigen `AchTransaction` y `AchCycleId`, incluidos clave de idempotencia y prioridad. | Permitir origen NACHA desagregado mediante contexto de despacho independiente del ciclo. |
| `IncomingNachaPostProcessingPipeline.cs` | Carga `AchTransaction.AchCycle`, evalúa calendario/ventanas y construye SOAP desde el ciclo. | Incorporar rama de despacho desacoplada y reutilizar controles comunes, cliente, respuesta y auditoría. |
| `ProcTransaccionesRequestMapper.cs` | Contrato de entrada exige `AchTransaction` y `AchCycle`; algunas fuentes de mapeo leen `cycle.id`/`cycle.processingdate`. | Mapear desde `NachaDispatchContext`; validar campos obligatorios y homologación CFA; no inventar un ciclo artificial. |
| `WscfaachSoapClient.cs` | Tiene operación `ProcTransaccionesAsync`. La ruta sólo envía externamente cuando el modo está en `Live`. | **Reutilizar** cliente; no cambiar contrato SOAP salvo que el contrato CFA lo requiera. |
| `ExternalFileNameSupport.cs` | `AchRegex` acepta sufijo de ciclo positivo (`[1-9]\d*`) y `BuildAchName` construye `RRRRTTT.ZZZ.{cycle}`; no restringe expresamente a 7. | Reutilizar patrón, añadir pruebas con `.8/.9` y permitir que el ciclo declarado provenga del archivo y no solo de `AchCycle` interno. |
| `CenitOfficialFileNameParser.cs` | Su patrón (`origen.ciclo.fecha.sufijo`) corresponde a CENIT, **no** a `RRRRTTT.ZZZ.N` de ACHCOL. | Crear parser ACHCOL dedicado o separar parsers por cámara; evitar tratar `ZZZ` como ciclo. |
| `ExternalFileNameBuilder.cs` | Para generar nombre ACH usa `CycleNumber` o parsea `CycleName`, y en entorno Live existe compuerta de homologación de nombres ACHCOL. | Resolver `N` desde el contexto de archivo/ciclo externo declarado sin exigir `AchCycle`; conservar compuerta Live hasta homologación. |

## 3. Diseño propuesto

### 3.1. Nuevo contexto operativo de entrada

Crear un objeto de dominio/DTO inmutable `IncomingNachaOperationalContext`, alimentado por el encabezado y la cámara:

```csharp
public sealed record IncomingNachaOperationalContext(
    Guid IngestionId,
    int ClearingHouseId,
    DateOnly OperationalDate,
    string FileIdModifier,
    int FileSequence,
    int? ExternalCycleNumber, // sufijo N en RRRRTTT.ZZZ.N
    string? AchCycleId,
    string ProcessingMode // "CycleBound" o "AchColFileBased"
);
```

**Regla:** en `AchColFileBased`, `AchCycleId` puede ser `null` y no determina aceptación ni despacho. La cámara y la fecha operativa siguen siendo obligatorias. `H` se normaliza a `FileSequence=8` e `I` a `FileSequence=9`; `ExternalCycleNumber` se obtiene **independientemente** del último componente del nombre. Se conservan ambos datos originales. `.8`/`.9` son válidos para la nueva ruta aun sin ciclos 8/9 configurados.

### 3.2. Política exclusiva por cámara y feature flag

Incorporar una política inyectable (por ejemplo `IIncomingNachaProcessingContextPolicy`) que permita la nueva ruta únicamente para ACH Colombia (`ACHCOL`) cuando `AchCol:AllowCycleIndependentInboundDispatch=true`. En cualquier otra cámara, utilizar el flujo existente.

El feature flag puede habilitarse primero en `DryRun`, luego en producción después de UAT y aprobación operativa. No cambiar globalmente la semántica de `AchCycle`.

### 3.3. Cola y origen de la transacción

La ruta actual necesita una `AchTransaction` asociada a `AchCycle`. El nuevo origen también debe preservar `ExternalCycleNumber` si el archivo trae sufijo `.8`, `.9` u otro positivo. Para despachar sin ciclo, introducir un **origen de solicitud por entrada NACHA**, con trazabilidad directa a `IncomingNachaFileIngestion`, `EntryDetail` y, cuando aplique, `AddendaRecord`.

Alternativa de menor acoplamiento recomendada:

- Introducir `IncomingNachaDispatchSourceContext` o equivalente (`NachaEntry`, `ExistingAchTransaction`) y una cola/DTO que admita referencias opcionales a `AchTransaction` y `AchCycle`.
- Extender la persistencia relacional para que `EntryDetailId` sea el ancla obligatoria de la ruta NACHA y `AchCycleId` no lo sea.
- **No** crear ciclos 8/9 ficticios ni seleccionar arbitrariamente otro ciclo sólo para satisfacer una llave foránea.
- Mantener el camino `CycleBound` existente sin alteración.

Si la modificación del modelo/cola actual tiene demasiado alcance, crear una **cola específica** `IncomingNachaDirectDispatchQueue` y un adapter hacia los mismos servicios de SOAP, resultados y auditoría; decidir en análisis técnico con matriz de impacto y migraciones.

### 3.4. Elegibilidad y ventanas

Separar dos grupos de validaciones:

**Siempre obligatorias:** perfil NACHA-M, registro tipo 1 válido, cámara reconocida/activa, fecha y tipo de transacción válidos, consistencia de totales, duplicidad, clasificación, datos requeridos, destinatario, linking verificable, mapeo habilitado, control de concurrencia e idempotencia.

**No aplicables como bloqueo por el solo consecutivo:** ciclo `008` inexistente, `AchCycleId` ausente y ventana asociada a un ciclo inexistente. Si hay restricciones reales por calendario del servicio CFA, deben modelarse como **política explícita de despacho por cámara/operación**, no inferirse de la letra.

**Importante:** no enviar prenotificaciones, devoluciones ni clases excluidas a `Proc_Transacciones` por el solo hecho de que el archivo sea válido. Preservar su enrutamiento funcional.

### 3.5. Mapeo y SOAP

Crear un contrato fuente `ProcTransaccionesNachaSourceContext` con campos del archivo, `FileSequence`, `ExternalCycleNumber`, lote, entrada, adenda, cámara, fecha y correlación. Extender `IProcTransaccionesRequestMapper`/resolver de mappings para poblar parámetros CFA sin requerir `cycle.id`.

Revisar reglas publicadas y obligatorias que hoy usan `cycle.*`. Si CFA requiere un **número externo de ciclo**, poblarlo con el `ExternalCycleNumber` del nombre **solo tras validar la semántica contractual de ese campo**, nunca con `AchCycleId` inventado. Homologarlas con fuentes NACHA o parámetros explícitamente acordados con CFA; **no** sustituir arbitrariamente `cycle.id` por `008` o `009`. Conservar `IntegrationMappingReadiness`, mapeo publicado, hashes de request/response y resolución de respuesta. El WSDL suministrado confirma `WSCFAACH.Proc_Transacciones`, pero importa XSD externos: validar el contrato de campos exactos con el servicio CFA y el mapper existente.

## 3.6. Arquitectura adoptada al cerrar F0.3

**Decisión técnica registrada en [`F0_3_ADR_ARCHITECTURE.md`](F0_3_ADR_ARCHITECTURE.md)** — estado `ADOPTADA PARA IMPLEMENTACIÓN`, **no implementada ni certificada**.

- **Ruta:** `AchColFileBased` disponible solo para `ACHCOL` mediante `AchCol:AllowCycleIndependentInboundDispatch` inicialmente desactivada; `CycleBound` existente se conserva para todos los demás casos.
- **Nombre inbound:** parser dedicado para `RRRRTTT.ZZZ.N`, con `N >= 1` sin límite artificial `7`; `ZZZ` y `FileIdModifier` consistentes por perfil, **N independiente de ZZZ**. No intervenir parser CENIT.
- **Persistencia:** `IncomingNachaFileIngestion` guarda letra, ZZZ, N y `ProcessingMode`; cola `IncomingNachaDispatchQueue` **existente** amplía `SourceKind`, `EntryDetailId` y referencias opcionales `AchTransactionId`/`AchCycleId` solo para la ruta nueva. Migraciones EF compatibles para PostgreSQL y SQL Server.
- **Ejecución:** misma cola/pipeline y cliente CFA, pero fuente tipada NACHA para la rama nueva; no crear ciclos falsos ni sustituir automáticamente `cycle.id` por `.8/.9`.
- **Seguridad:** clasificación y controles de link, calendario real de cámara/servicio, idempotencia por identidad de negocio, revisión manual de resultado SOAP incierto; Live permanece inhabilitado hasta autorización y homologación.
- **Pruebas:** los tests locales se ejecutarán cuando estén disponibles; certificación de ambientes y E2E en F6 tras implementar F1–F5, **sin bloquear el desarrollo**. Cada incremento produce ZIP completo con fuentes + docs.

**Siguiente tarea después de F0.3:** `F1.1` (parser entrante ACHCOL), no F2 ni F3 directamente.

## 4. Fases de implementación y entregables

| Fase | Trabajo | Entregable / salida |
|---|---|---|
| **F0. Línea base** | Congelar ejemplo H/I, mapear flujo real, perfiles y nomenclatura, identificar dependencias EF/SQL Server/PostgreSQL y mappings CFA, definir elegibles. | ADR técnico + matriz de campos + casos H/I reproducibles. |
| **F1. Identificador, nombre físico y sobre digital** | Exponer `FileIdModifier`, `FileSequence` y `ExternalCycleNumber`; soportar `RRRRTTT.ZZZ.N` con `N=8`, `N=9` u otro positivo; validar la correspondencia letra/`ZZZ` cuando aplique, **sin limitar `N` por ciclo interno**. | Parser/header y pruebas de códigos y sufijos 8/9. |
| **F2. Ingesta sin ciclo** | Separar resolución cámara/fecha, `ExternalCycleNumber` y `AchCycleId`; admitir `ACHCOL` `.8`/`.9` aunque falte `AchCycle`; revisar `IngestionAppService`, `AdmissionValidator`, máquina de estados y eventos. | H/I y nombres con sufijos 8/9 llegan a `Parseado` sin `AchCycleId`. |
| **F3. Persistencia/cola** | Agregar contexto de despacho basado en `EntryDetail`; migraciones Postgres y SQL Server; clave idempotente independiente de número de ciclo y de una letra arbitraria. | Entidades y migraciones compatibles; no se requiere FK obligatoria a ciclo en nueva ruta. |
| **F4. Elegibilidad y orquestación** | Ruta `AchColFileBased`; clasificación/linking seguros; colas elegibles y scheduler; conservar ruta `CycleBound`. | Encolado/selección de transacciones sin buscar ciclo 8/9. |
| **F5. SOAP CFA** | Mapper sobre NACHA desagregado, validar contrato, publicar mappings, enviar por `WscfaachSoapClient.ProcTransaccionesAsync`, interpretar respuesta. | Envelope CFA homologado, dry-run aprobado y prueba Live controlada. |
| **F6. QA/UAT y despliegue** | Regresión por cámara, concurrente, seguridad, carga, duplicados, reintentos inciertos; monitoreo y rollback. | Evidencias UAT, acta de aprobación, feature flag controlado y runbook. |

## 5. Diseño de idempotencia y conciliación

- **Identidad de archivo:** hash del contenido canónico + origen/cámara + fecha; detectar duplicados aunque se renombre el archivo.
- **Identidad de entrada/transacción:** fuente/participante, fecha operativa, identificador de trazabilidad (trace), lote, tipo, valor y demás claves de negocio homologadas.
- **Clave de despacho:** combinación estable de identidad de entrada + operación CFA, sin depender de `AchCycleId`; conservar índice único y protección ante dos workers.
- Persistir intentos con estados explícitos `Queued`, `Dispatching`, `Confirmed`, `RetryPending`, `Blocked` o sus equivalentes.
- En timeout o resultado SOAP incierto, **conciliar antes de reenviar**. No activar retries automáticos a ciegas para operaciones monetarias.
- Conservar hashes, timestamp, respuesta CFA, IDs externos, código funcional, correlation ID, versión de mapping y usuario/proceso que ordenó el despacho.

## 6. Matriz mínima de pruebas de aceptación

| ID | Escenario | Resultado obligatorio |
|---|---|---|
| AT-01 | Archivo `1234567.008.8`, encabezado `H`, sin `AchCycle` 8 | Admite, conserva `FileSequence=8` y `ExternalCycleNumber=8`, crea despachos elegibles, genera SOAP. |
| AT-02 | Archivo `1234567.009.9`, encabezado `I`, sin `AchCycle` 9 | Igual AT-01 y conserva `ExternalCycleNumber=9`. |
| AT-03 | Sin ningún ciclo operativo parametrizado para esa fecha/cámara ACHCOL | Ruta `AchColFileBased` funcional, sin asignación de ciclo ficticio. |
| AT-04 | Ciclo real existente para ACHCOL | La ruta anterior y la nueva conviven sin duplicidades ni cambios regresivos. |
| AT-05 | Todos los identificadores `A–Z`, `0–9` con consecutivos 001–036 | Mapeo correcto; no equivalencia artificial con ciclo. |
| AT-06 | Letra/ZZZ inconsistentes en escenario que exige consistencia de nomenclatura | Error de archivo, **no** de ciclo; el sufijo `N` se valida por separado. |
| AT-07 | Archivo duplicado, incluyendo renombrado | Sin nuevo despacho monetario. |
| AT-08 | Registro 106 caracteres inválido, cámara errada, fecha inválida | Rechazo controlado. |
| AT-09 | Prenotificación/devolución/clase no elegible | Procesamiento funcional correspondiente, **no** `Proc_Transacciones`. |
| AT-10 | Falta mapping requerido, operación SOAP inactiva, falta permiso | Bloqueo controlado y auditable. |
| AT-11 | DryRun con `H`/`I` | Envelope completo y persistencia, 0 transmisiones externas. |
| AT-12 | Live contra CFA en ambiente habilitado | Invocación SOAP efectiva, respuesta y estado conciliado por entrada. |
| AT-13 | Timeout y respuesta incierta | Sin reenvío automático indiscriminado; revisión/reconciliación. |
| AT-14 | Dos workers procesan el mismo archivo | Una sola ejecución monetaria efectiva por identidad. |
| AT-15 | Archivo CENIT + ACHCOL tradicional | Sin regresiones. |
| AT-16 | `1234567.008.8` y `1234567.009.9` en parser de nombre ACHCOL | Aceptados; no se intenta interpretarlos como nombres CENIT ni se infiere ciclo interno. |
| AT-17 | `1234567.008.10` con letra `H`, sin `AchCycle` 10 | Admitido si el perfil permite `N=10`; `FileSequence=8` distinto de `ExternalCycleNumber=10`. |
| AT-18 | Nombre con `.8`/`.9`, cámara/ciclo interno inexistente | No aparece rechazo por `CYCLE_NOT_CONFIGURED` o por rango máximo 7; los errores genuinos siguen rechazándose. |
| AT-19 | Generación outbound ACHCOL con `ExternalCycleNumber=8` o `9` y sin `AchCycle` | Nombre generado con `.8`/`.9` correctamente; Live sigue sujeto a homologación de naming. |
| AT-20 | Payload SOAP CFA con ciclo externo 8/9, donde contrato lo requiera | El valor se obtiene del nombre/contrato homologado, no de FK inexistente; respuesta correctamente correlacionada. |

**Extensión de pruebas AT-21–AT-28:** cobertura de `*.OUT` en claro, `*.OUT.env` cifrado, firma inválida, canonicalización de nombre, descubrimiento MFT/SFTP, idempotencia y regresión RET/CENIT (detalle en `F0_3_A_NAMING_OUT_ENVELOPE.md`).

**Criterio de aceptación crítico:** no aparece `CYCLE_NOT_RESOLVED`, `CYCLE_NOT_CONFIGURED`, `ACHCOL_HEADER_TIME_WITHOUT_CYCLE_WINDOW`, límite artificial `N<=7` ni bloqueo equivalente **cuando la única causa es que el encabezado `H`/`I` o el sufijo de nombre `.8`/`.9` no tienen ciclo operativo parametrizado**.

## 7. Datos, migraciones y monitoreo

Persistir en entidad de ingesta o contexto:
- `FileIdModifier` (`char(1)`), `FileSequence` (`int`), **`ExternalCycleNumber` (`int`, nullable)**, `OriginalExternalFileName`, `ProcessingMode`, `ResolvedClearingHouseId`, `OperationalDate`.
- `ResolvedAchCycleId` nullable, explícito.
- `EntryDetailId`, `AddendaRecordId`, `AchTransactionId` (opcional según ruta), `IdempotencyDispatchKey` (unique), `CorrelationId`.
- `MappingSetId`, versión, request/response hash y estado de CFA.

Crear migraciones equivalentes **PostgreSQL y SQL Server** (proyecto contiene ambos proveedores). Backfill **sin cambiar** significado de datos históricos; no transformar retrospectivamente el identificador `H` en ciclo 8.

Dashboard/alertas: archivos `H/I` aceptados y fallidos, `sin ciclo` por cámara, entradas elegibles sin cola, colas atascadas, mapping bloqueante, tasa de respuesta CFA, resultado incierto, duplicados prevenidos y tiempos de procesamiento.

## 8. Habilitación y rollback

1. Desplegar migraciones compatibles y código con feature flag desactivado.
2. Activar en QA `AchColFileBased` en DryRun y validar AT-01–AT-20 aplicables.
3. UAT con endpoint CFA de pruebas y respuestas reales; aprobar contrato e idempotencia.
4. Activar Live por cámara/entorno autorizado, inicialmente con pocos archivos conocidos y seguimiento transaccional.
5. Disponer de rollback **apagando la feature flag**, sin borrar auditoría ni registros enviados.
6. Para transacciones con resultado incierto, conciliar antes de reintentar; nunca hacer rollback mediante reenvío masivo.

## 9. Dependencias y riesgos

- **Contrato CFA:** verificar esquema XSD importado por el WSDL y mapping publicado; no dar por probado que un campo SOAP `cycle` pueda omitirse si CFA lo requiere.
- **Acoplamiento a ciclo:** `IncomingNachaDispatchQueue`, `AchTransaction` y `ProcTransaccionesRequestMapper` exigen hoy `AchCycle` en su ruta; requiere cambio arquitectónico, no solo cambiar una comparación.
- **Nomenclatura:** la especificación V35 consultada describe `RRRRTTT.ZZZ.1`, mientras que el requisito de CFA permite `RRRRTTT.ZZZ.8` y `.9` y `ExternalFileNameSupport` implementa `RRRRTTT.ZZZ.{cycle}`. Homologar por cámara/dirección y contrato externo sin imponer la limitación `.1` al procesamiento interno definido por el solicitante. El parser CENIT no debe interpretar nombres ACHCOL.
- **Riesgo monetario:** duplicado, respuesta incierta y habilitación Live requieren aprobación de Operaciones/Seguridad/Integraciones.
- **Regresiones CENIT:** no aplicar indiscriminadamente el bypass de ciclo a otras cámaras.

## 10. Definition of Done

- [ ] `H=008` e `I=009` recibidos y despachados con nombres que terminan `.8` y `.9`, sin ciclos 8/9 parametrizados.
- [ ] Funciona para ACHCOL aun con **cero** filas de `AchCycle` disponibles, donde corresponda; se preservan `ZZZ` y el sufijo numérico `N` como campos distintos.
- [ ] El SOAP en modo Live llega al ambiente CFA autorizado con mapeo y respuesta homologados.
- [ ] Las operaciones no elegibles conservan su ruta correspondiente.
- [ ] 0 envíos monetarios duplicados en UAT de concurrencia y replay.
- [ ] 100% trazabilidad archivo → detalle → decisión → cola → request → respuesta CFA.
- [ ] Suite de regresión ACHCOL/CENIT y migraciones SQL Server/Postgres aprobadas.
- [ ] Feature flag de habilitación/rollback, métricas y runbook de operación.

**Recomendación de implementación:** diseñar el flujo `AchColFileBased` como extensión opt-in a la arquitectura existente, sin inventar ciclos ni relajar controles no relacionados con la existencia de ciclos. La versión de referencia del código y las evidencias son una **revisión estática**; las pruebas, homologación CFA y certificación Live están pendientes.

## 11. Ajuste de alcance aprobado en conversación — sufijo de ciclo en nombre de archivo

**Regla obligatoria añadida:** cuando el nombre externo incluye un número de ciclo, los valores `8`, `9` y superiores positivos deben aceptarse y procesarse sin restricción por cantidad de ciclos configurados en ACHINTERBANK. No basta con permitir `H`/`I` en el registro 1: debe soportarse integralmente `RRRRTTT.ZZZ.8`/`.9` en recepción, persistencia, UI/diagnósticos, encolado, generación de nombre outbound si aplica y mapping CFA.

**Implementación mínima a no omitir:**
1. `AchColExternalFileNameParser`: reconocer `RRRRTTT.ZZZ.N`, preservar `ZZZ` y `N`, sin depender del parser exclusivo de CENIT ni restringir `N` a 7.
2. Resolver/validador: la inexistencia del ciclo operativo `N` no es causal de rechazo para ACHCOL.
3. Modelo y cola: `ExternalCycleNumber` independiente de `AchCycleId` nullable.
4. `ExternalFileNameBuilder`: permitir nombre con `.8`/`.9` desde contexto externo sin requerir `CycleName` o `AchCycle`, manteniendo el gate de homologación Live.
5. SOAP CFA: transportar el ciclo externo donde la homologación lo exija; no inventar equivalencia con ID de base de datos.
6. QA: pruebas end-to-end con encabezado `H`, `I` y sufijos `.8`, `.9` sin ciclos 8/9, además de un caso que pruebe independencia entre `ZZZ` y `N`.

**Límite de afirmación documental:** este requisito proviene de la aclaración funcional del solicitante para el procesamiento CFA. El manual ACH Colombia V35 provisto describe `RRRRTTT.ZZZ.1` para el caso documentado; el uso de `.8`/`.9` en un intercambio directo con ACH Colombia debe certificarse con el receptor correspondiente.


## 12. Control incremental de implementación (MEMORY_STATE)

Esta iniciativa se ejecutará en microincrementos verificables y mantendrá un estado durable **independiente** del programa existente `CFA-SOAP-UX-MAP-001`:

- **Estado de trabajo:** `docs/ACHCOL_CFA_FILE_BASED_2026-10-24/MEMORY_STATE.md` (identificador `ACHCOL-NACHA-CFA-EXTCYCLE-001`).
- **Instrucción de reanudación:** `docs/ACHCOL_CFA_FILE_BASED_2026-10-24/CHATGPT_MICRO_START.md`.
- **Plan normativo interno de esta iniciativa:** el presente archivo, copiado como `docs/ACHCOL_CFA_FILE_BASED_2026-10-24/PLAN.md` en el paquete de trabajo.

**Reglas de ejecución:** trabajar una sola microtarea por sesión; validar los cambios que puedan validarse; registrar archivos modificados, hashes/commit, pruebas, resultados, bloqueos y siguiente paso **antes de cerrar cada sesión**. Nunca marcar `VERIFIED` sin evidencia ejecutada. El estado no se sincroniza de forma automática entre sesiones de ChatGPT: el archivo debe mantenerse versionado en el repositorio y compartirse en la sesión posterior. No tocar ni sobrescribir el `MEMORY_STATE.md` de otros programas.

## 13. Política de continuidad y entregables — aprobada 2026-10-24

**Decisión funcional del solicitante:** no suspender avances de implementación por falta de ambiente de pruebas. La validación de integración/homologación y UAT se efectuará **al final de todas las implementaciones**, en F6. Cuando se puedan ejecutar compilación, pruebas unitarias, mocks, revisiones estáticas o CI local, se ejecutarán y documentarán; su ausencia no es bloqueo de la siguiente microimplementación.

**Gates diferenciados:**
- **Durante F0–F5:** implementación incremental continua, revisiones/pruebas disponibles, estado `IMPLEMENTED_UNVERIFIED` para el código no comprobado por ausencia de infraestructura; backlog y deuda de pruebas visible.
- **Al concluir F1–F5 / F6:** pruebas finales E2E CFA, regresión, BD, conciliación, UAT y aprobación de Live.
- **Restricción de seguridad:** no enviar dinero ni invocar el endpoint real en modo Live sin autorización; no declarar pruebas no ejecutadas como exitosas.

**Entrega obligatoria tras cada incremento:** un **ZIP completo del proyecto ACHINTERBANK**, manteniendo fuentes, tests, configuraciones, migraciones, `docs/ACHCOL_CFA_FILE_BASED_2026-10-24/PLAN.md`, `MEMORY_STATE.md`, ADRs, reportes y `CHATGPT_MICRO_START.md` con el siguiente paso. No entregar únicamente diffs o documentos aislados. Mantener el historial del estado y las decisiones tomadas en cada sesión.

**Estado vigente del programa:** F0.1–F0.3 finalizadas como diseño; **F1.1/F1.2, F2.1 y F2.2 implementados sin verificación .NET**. **Siguiente:** F2.3 (persistencia/seguimiento ACHCOL sin ciclo). Pruebas de ambiente, CFA y UAT al final en F6.

## Avance de implementación F1.1 — 2026-10-24

**Entregado en código:** `AchColInboundFileNameParser.cs` para nombre físico `RRRRTTT.ZZZ.yyyyMMdd.N.OUT[.env]`, `FileSequence=001–036` independiente de `ExternalCycleNumber=N` positivo sin tope artificial 7, fecha válida y distinción de sobre digital. El parser conserva `OriginalPhysicalFileName` y `CanonicalPlainFileName`, sin forzar `AchCycleId`.

**Control de seguridad:** validación de nombre único (sin path traversal), extensiones exactas, ASCII numérico, enteros positivos, fecha calendario válida, detección de tipos `PlainNacha` y `DigitalEnvelope`. No descifra archivos ni invoca SOAP: esa integración es F1.2/F2–F5.

**Pruebas:** 31 casos xUnit agregados; **no ejecutados** porque no hay SDK .NET 10 en el entorno. Tests end-to-end, firma, MFT/SFTP, CFA y BD en fases posteriores/F6; su ausencia no bloquea F1.2.

**Siguiente microtarea:** F1.2 — integrar parser al flujo ACHCOL y recepción Managed Folder/SFTP de `.OUT` plano, correlacionar encabezado con `ZZZ` sin asociar `N` a un ciclo interno. Detalles y riesgos en [`F1_1_IMPLEMENTATION.md`](F1_1_IMPLEMENTATION.md).


## Avance de implementación F1.2 — 2026-10-24

- **Entradas automatizadas:** `.OUT` ahora puede ser reclamado por Managed Folder y SFTP además de `.OUT.env` y `.RET.env`; se mantienen semánticas de archivos temporales/ocultos, idempotencia, claims y archivado.
- **Correlación ACHCOL:** nombre físico y canónico, fecha de registro tipo 1 y `FileIdModifier` deben coincidir con `ZZZ`. Los errores se rechazan antes del parser y se auditan con códigos estables. `ExternalCycleNumber=N` es independiente de `ZZZ` y de `AchCycleId`.
- **Seguridad:** el contenido `.OUT.env` sigue pasando por el servicio de sobre digital existente; `.OUT` no se descifra. No se confía en nombres manipulados, no se hacen fallbacks de criptografía ni se generan ciclos ficticios.
- **Regresión:** archivos de prueba y mocks nuevos/ampliados, **sin ejecutar** por falta de SDK .NET 10. Pruebas finales de ambiente continúan en F6.
- **Alcance no resuelto:** `IncomingNachaCycleResolver` y `IncomingNachaAdmissionValidator` todavía requieren el ciclo interno. F2.1/F2.2 los desacoplarán; F1.2 no declara funcional el envío SOAP.
- **Siguiente:** F2.1. Detalle en [`F1_2_IMPLEMENTATION.md`](F1_2_IMPLEMENTATION.md).

## Avance F2.1 — 2026-10-24 — resolución ACHCOL sin ciclo interno

- Se añadió rama **opt-in** del resolver ACHCOL para nombres canónicos `RRRRTTT.ZZZ.yyyyMMdd.N.OUT` (ambas entradas físicas `.OUT` y `.OUT.env` llegan como `.OUT` tras descifrado F1.2). Cámara y fecha se resuelven sin consultar ni crear `AchCycle`.
- Se añadió `IncomingNachaCycleResolutionStatus.ResueltoSinCiclo=7` y `ResolutionMode=AchColFileBased`. `AchCycleId=null`, `FileSequence=ZZZ` y `ExternalCycleNumber=N` quedan independientes; evidencia JSON explícita.
- Configuración `AchCol:AllowCycleIndependentInboundDispatch=true` requerida para habilitar **solo** la rama del resolver; ausente o `false` conserva el algoritmo anterior. No habilitar en producción mientras admission/cola/mapper no estén preparados.
- Fecha y hora del tipo 1 inválidas siguen causando error, no falso éxito. RET/CENIT y nombres legacy no usan nueva rama.
- Cambios: `IncomingNachaCycleResolver.cs`, `IncomingNachaIngestion.cs` (enum), `IncomingNachaCycleResolverTests.cs`. Sin DB, SQL, SOAP ni endpoints modificados.
- **Verificación:** código y pruebas escritos, sin compilación ni xUnit por falta de SDK .NET 10. Pruebas finales/ambiente en F6 después de F1–F5, sin detener trabajo.
- **Límite actual:** F2.1 no autoriza parseo/despacho: `IncomingNachaAdmissionValidator` aún rechaza si `AchCycleId=null`. **Siguiente: F2.2** adaptar admisión manteniendo controles operacionales. Evidencia detallada en [`F2_1_IMPLEMENTATION.md`](F2_1_IMPLEMENTATION.md).


## Avance F2.2 — 2026-10-24 — admisión ACHCOL sin dependencia de ciclo

- `IncomingNachaAdmissionValidator` acepta el estado `ResueltoSinCiclo` **exclusivamente** en contexto `AchColFileBased` inequívoco, con `AchCycleId=null`, cámara activa de código `ACHCOL` y `AchCol:AllowCycleIndependentInboundDispatch=true`; no se consultan ciclos internos para esa vía. El flag sigue apagado por defecto.
- Conserva validación de festivos, coincidencia fecha de nombre/header, fecha efectiva ACHCOL en formato `yyyyMMdd`, hora tipo 1 y correlación identificador `H/I ↔ ZZZ` del archivo `.OUT` canónico. No admite un `.OUT.env` sin descifrar directamente.
- El `CycleNumber` del preview ACHCOL se llena con `ExternalCycleNumber=N` (8/9/10), **no** con consecutivo `ZZZ` ni con un `AchCycleId`. La persistencia independiente de esos datos sigue pendiente F2.3.
- CENIT, RET y la ruta de ciclo interno continúan usando reglas anteriores; no se alteran SQL/EF/colas/SOAP. Se añadieron casos de prueba unitarios, **no ejecutados** por ausencia de .NET 10. Detalle en [`F2_2_IMPLEMENTATION.md`](F2_2_IMPLEMENTATION.md).
- **Siguiente:** F2.3. Se pospone certificación integral hasta F6 sin bloquear desarrollo; no se activa Live ni envíos monetarios prematuros.

## Actualización F3.2 — SQL Server (2026-10-24)

Se añadió la migración `20261024224500_AchColFileBasedDispatchQueueF32.cs` y el snapshot de SQL Server para paridad con PostgreSQL F3.1: `SourceKind` `nvarchar(30)` no nulo/default `CycleBound`, `EntryDetailId` nulo con FK `Restrict`/índice, `AchTransactionId` y `AchCycleId` opcionales y check de forma por origen. El índice único de idempotencia existente no se altera. `Down` falla en modo seguro con `THROW 51032` si hay entradas que no pueden volver al esquema antiguo. Los tres métodos de prueba EF están escritos pero no ejecutados; tampoco se ejecutaron migraciones en DB reales.

**Decisiones DEC-036–DEC-040 y reporte:** [`F3_2_IMPLEMENTATION.md`](F3_2_IMPLEMENTATION.md). **Siguiente:** F3.3 planificador/idempotencia sin ciclo. La arquitectura de consumo SOAP, seguridad del sobre, RET y CENIT permanece inalterada; `Live` no autorizado. **Nueva fuente de continuidad:** `ACHInterbank_ACHCOL_CFA_F3_2_UNIFICADO.zip`. Las pruebas finales siguen en F6.


## Actualización F3.3 — planificador `AchColFileBased` y clave de negocio (2026-10-24)

Se añadió planificación persistente desde `EntryDetail` NACHA-M y `IncomingNachaEntryClassification` ligada a su encabezado de la misma ingesta, sin `AchCycle`, `AchTransaction` ni link artificial. Funciona tras la ruta de ingreso F1/F2 y solo con `ResolutionMode=AchColFileBased`, `ResueltoSinCiclo`, cámara activa código ACHCOL y flag opt-in. La identidad SHA-256 de negocio versionada ignora el nombre, sobre, secuencia y ciclo (interno/externo), para evitar reenvíos duplicados de la misma operación. Solo créditos/débitos elegibles, con trace 15 dígitos, monto positivo e identidad completa.

**Seguridad:** las filas se persisten `Blocked` con `ACHCOL_DISPATCH_NOT_ENABLED_F33`, nunca `Queued`/Live. Se cambió nulabilidad de `IncomingNachaTransactionLinkId` a opcional en la ruta NACHA y se agregaron migraciones F3.3 PostgreSQL + SQL Server y ambos snapshots; `CycleBound` sigue exigiendo link, transacción y ciclo. Índice único y rollback protegido. El control de carreras interprocesos se apoya en UNIQUE y debe verificarse/reconciliarse con bases reales; **no se considera certificado**. Las suites xUnit/SQLite fueron escritas sin ejecutarse; .NET 10 y BD no disponibles aquí. Detalle [`F3_3_IMPLEMENTATION.md`](F3_3_IMPLEMENTATION.md), decisiones DEC-041–DEC-046. Próxima microtarea **F4.1**, política de elegibilidad ACHCOL sin ciclo; ambiente y pruebas finales F6. Fuente de continuidad `ACHInterbank_ACHCOL_CFA_F3_3_UNIFICADO.zip`.

## Actualización F4.1 — política de elegibilidad ACHCOL `AchColFileBased` (2026-10-24)

Se añadió `EvaluateFileBasedAsync` a la política de elegibilidad y se conectó explícitamente al planificador file-based, sin modificar el método `CycleBound`. La ruta ACHCOL valida cámara activa, contexto `ResueltoSinCiclo`, nombre canónico/fecha/secuencia/modificador/protección, relación header/detalle/ingesta, clasificación aprobada, códigos monetarios coherentes (`22/32/52` crédito, `27/37/55` débito), exclusión de prenotificaciones/devoluciones/revisión manual e identidad determinística. `H/008/.10` mantiene independencia `ZZZ != N`. No se crean ciclos ficticios ni se consulta `AchCycle`.

**Seguridad:** resultado `IsEligible` de F4.1 no libera dinero ni autoriza `Proc_Transacciones`: filas file-based siguen `Blocked` con el código F3.3 y sin `NextAttemptAtUtc`; `Live` sigue deshabilitado. La política de ventanas específica de CFA permanece sujeta a diseño posterior/homologación; no se simulan horarios de ciclos inexistentes. Tests positivos/negativos añadidos pero **no ejecutados** por falta de .NET 10; ninguna BD, SOAP ni UAT ejecutados. Decisiones DEC-047–DEC-051, detalle [`F4_1_IMPLEMENTATION.md`](F4_1_IMPLEMENTATION.md). Próxima microtarea **F4.2**; entregar proyecto completo en `ACHInterbank_ACHCOL_CFA_F4_1_UNIFICADO.zip`.

## Actualización F4.2 — prevalidación desde orquestador ACHCOL sin ciclo (2026-10-25)

Se incorporó una etapa separada de preparación en el worker/orquestador R5D. Con el flag ACHCOL activado, procesa en orden de prioridad y por lotes acotados los registros file-based `Blocked` con error F3.3; vuelve a validar ingesta/encabezado/clasificación/detalle, cámara, fecha, política F4.1 e identidad SHA-256. Los válidos quedan `Blocked/ACHCOL_MAPPING_NOT_READY_F42`, los inconsistentes `Blocked/ACHCOL_PREFLIGHT_REJECTED_F42`. No hay `Queued`, `RetryPending`, envío SOAP, ni consulta de ciclo interno. El worker `CycleBound` sigue intacto.

Reejecuciones no crean nuevos eventos a partir de estados F4.2; ante flag OFF no hay mutación. Incluye tests xUnit/InMemory escritos, no ejecutados por falta de SDK .NET. La verificación de concurrencia multinodo, bases reales, cambios contractuales CFA y pruebas E2E siguen F6; no bloquean F4.3/F5. Decisiones DEC-052–056, ver [`F4_2_IMPLEMENTATION.md`](F4_2_IMPLEMENTATION.md). **Siguiente F4.3**.


## Actualización F4.3 — controles mínimos de reintentos, trazas y simulación local (2026-10-25)

**Estado `IMPLEMENTED_UNVERIFIED` (sin ejecución .NET).** La ruta `AchColFileBased` sigue en `Blocked`, sin SOAP, sin cron nuevo, sin duplicar worker. Se protegieron las operaciones manuales: no se permite retry/unblock/requeue ACHCOL hasta homologación y autorización F5; quedan auditadas como `Rejected` con código `ACHCOL_FILEBASED_MANUAL_RELEASE_FORBIDDEN_F43`. El Command Center no anuncia estas acciones. `MarkFailedFinal` solo permanece disponible antes de cualquier intento/ejecución CFA, para evitar declarar error definitivo ante una respuesta monetaria incierta. CENIT/CycleBound preservados.

La simulación segura utiliza la prevalidación local F4.2, con test adicional donde el modo de despacho tradicional está en `Live` pero **ACHCOL sigue sin invocar SOAP ni crear execution**. No se añadieron tablas ni motor de reintentos. Las pruebas .NET/DB/concurrencia/conciliación se difieren a F6 y no frenan el desarrollo. Decisiones **DEC-057–060** en [`F4_3_IMPLEMENTATION.md`](F4_3_IMPLEMENTATION.md). **Siguiente F5.1:** homologación campo a campo del contrato `Proc_Transacciones`, sin suposiciones sobre `AchCycleId`, y sin habilitar Live.


## Incremento F5.1 — contrato CFA y fuentes NACHA-M (2026-10-25)

La operación `WSCFAACH.Proc_Transacciones` consta en el WSDL, pero los XSD externos importados no se suministraron. El catálogo interno contiene **25 parámetros de entrada y 2 de salida**, clasificados en `F5_1_IMPLEMENTATION.md` en candidatos directos, condicionales y no resueltos. **No se declara homologación CFA completada.** El mapper legado rechaza el origen `AchColFileBased` antes de usar un `AchTransaction`/`AchCycle` artificial. Se incorpora inventario reutilizable y tests sin nuevo esquema ni infraestructura. DEC-061–DEC-065. Próxima F5.2: mapper por entrada NACHA y simulación DryRun, sin Live ni inventar semánticas contractuales; pruebas .NET/BD/CFA finales en F6.


## Entrega F5.2 — SOAP DryRun parcial, sin posibilidad de transmisión (2026-10-25)

F5.2 incorpora al mapper existente una API de previsualización de `EntryDetail` ACHCOL en cola bloqueada F4.2; devuelve un objeto específico `AchColProcTransaccionesDryRunPreview` (nunca `ProcTransaccionesRequestContract`) con valores NACHA observables, XML de vista previa e inventario explícito de requeridos no satisfechos. **CanTransmit=false** es incondicional. Se valida contexto, cámara, identidad de entrada y mapping publicado de referencia, pero no se autoriza ni invoca `IWscfaachSoapClient`. No se crean tablas ni se alteran mappings CFAUX. Véase `F5_2_IMPLEMENTATION.md`, decisiones DEC-066–DEC-070.

**Pendiente F5.3:** preparar harness, contrato y compuertas para homologación. **Pendiente F6:** ejecución real de .NET, pruebas E2E/DB/CFA, XSD importados, contrato completo y autorización de Live.


## Actualización F5.3 — harness local y condiciones para F6 (2026-10-25)

F5.3 implementa una verificación mínima de la previsualización SOAP F5.2: esquema de `Envelope/Body/Proc_Transacciones`, campos permitidos y coherencia XML/valores observados, rechazando entidades externas o datos inyectados. Se incorpora la inspección en el mapper y una suite de pruebas xUnit. La evidencia conserva **solo nombres de parámetros**, sin importes ni PII. El contrato sigue **no homologado** por faltar dos XSD del WSDL y reglas de campos CFA. No se crean tablas, colas, servicios ni feature flags. `CanTransmit=false` y las barreras de F4 siguen vigentes.

**F1–F5 están implementadas en código, no verificadas por ejecución.** F6.1 es el siguiente incremento: compilar, ejecutar regresión, migraciones y revisar faltantes; F6.2/F6.3 cubrirán E2E, homologación, UAT, cierre y decisión de activación. La falta de ambientes durante F1–F5 no constituye aprobación implícita para producción. DEC-071–DEC-075 y plan de evidencias: [`F5_3_IMPLEMENTATION.md`](F5_3_IMPLEMENTATION.md). Paquete de continuidad: `ACHInterbank_ACHCOL_CFA_F5_3_UNIFICADO.zip`.


## F6.1 — Auditoría técnica offline y deuda de verificación (2026-10-25)

El proyecto F5.3 pasó el grafo de solución (7 proyectos), un control estático focal nuevo (12/12), ratchet R5D y el estático del sobre digital NAV3.4J (145 aserciones). Se corrigió el rechazo de atributos/texto inesperados en el inspector SOAP diagnóstico y se agregó regresión escrita. **No** se ejecutaron build .NET, xUnit, migraciones BD ni CFA por ausencia de SDK/servicios. R5C histórico falla al detectar cambios respecto a un SHA fijo R5B; R7 no concluyó. **Ningún test de runtime se presume aprobado.** Reporte y comandos: [`F6_1_IMPLEMENTATION.md`](F6_1_IMPLEMENTATION.md).

F6.2 cubre documentación operacional, señales de observabilidad y plan de rollout/rollback sin habilitar Live. Antes de F6.3 debe constar ejecución reproducible de build, suite focal y total, concurrencia, PostgreSQL/SQL Server y homologación CFA formal. Los pendientes F6.1 continúan abiertos aun cuando el siguiente paquete sea F6.2.


## Incremento F6.2 — Operación y observabilidad sin infraestructura nueva

Consultar [`F6_2_IMPLEMENTATION.md`](F6_2_IMPLEMENTATION.md) y [`F6_2_OPERATIONS_RUNBOOK.md`](F6_2_OPERATIONS_RUNBOOK.md). El endpoint `observability/summary` ahora devuelve `achColFileBased` con bloqueos por causa y señales `UnexpectedNonBlocked`/`WithAttempts` (valor esperado 0). Los contadores están limitados a filas creadas en ventana 1–168 h, no constituyen certificación del histórico; antes de activar consultar DB solo lectura. No se añadieron tablas, migraciones, endpoints nuevos ni transporte monetario.

**Puertas de F6.3:** compilación y tests reales .NET 10.0.402, proveedores PostgreSQL/SQL Server, pruebas de concurrencia/migraciones/rollback en copia, regresión CENIT/RET, ratchets R5C/R7, XSD importados, homologación y aprobación CFA, UAT e identidad/idempotencia. Sin ellas, `Live` continúa prohibido.

## 14. F6.3 — Resultado de QA offline y certificación pendiente (2026-10-25)

**Entregado:** expediente `F6_3_VERIFICATION_REPORT.md` con controles estáticos realmente ejecutados, resultados, deuda y pasos reproducibles para CI/BD/CFA. Se corrigió un test estático que comparaba el puntero efímero `next_microtask`, en lugar de verificar seguridad permanente. **No se habilitó Live.**

**Gate final NO APROBADO:** falta .NET 10.0.402 ejecutable aquí, restore/build/xUnit, ensayos reales en PostgreSQL y SQL Server, regresión R5C/R7, XSD CFA y UAT/autorización de operación monetaria. Ratchet R5C reporta FAIL por SHA histórica; R7 no concluyó. La fase de implementación se documenta sin ocultar estos bloqueos. No se autoriza paso a producción ni se deben alterar filas `Blocked` hasta cerrar el gate de certificación. La matriz de aceptación AT-01–AT-28 sigue sin evidencia E2E satisfactoria.
