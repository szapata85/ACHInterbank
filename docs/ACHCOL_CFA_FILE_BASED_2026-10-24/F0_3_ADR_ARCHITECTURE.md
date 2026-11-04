# F0.3 — ADR-ACHCOL-001: NACHA-M ACH Colombia sin dependencia de ciclo interno

- **Programa:** `ACHCOL-NACHA-CFA-EXTCYCLE-001`
- **Fecha:** 2026-10-24
- **Estado:** **ADOPTADO PARA IMPLEMENTACIÓN** (decisión de diseño; no se ha implementado ni certificado el flujo)
- **Baseline:** ZIP consolidado F0.2, rama interna `ACH-Interbank`, `HEAD=0b4ce1c809ca5acfdafaf455cae4d0eaa5292f07`
- **Alcance:** archivos **entrantes ACHCOL** hasta `WSCFAACH.Proc_Transacciones`; proteger CENIT y el envío legacy.
- **Evidencia:** inspección estática de fuentes, `F0_1_BASELINE.md`, `F0_2_FLOW_TRACE.md`, manual ACH Colombia V35 aportado y WSDL de CFA. No es homologación con CFA.

## A. Decisión arquitectónica principal

**Adoptar una sola cola `IncomingNachaDispatchQueue` con dos orígenes tipados y disjuntos**, manteniendo el comportamiento actual para `CycleBound` e incorporando `AchColFileBased` con **referencia directa a `EntryDetail` y asociación opcional a `AchTransaction`/`AchCycle`**. Usar un DTO/record inmutable `IncomingNachaOperationalContext` para transportar la interpretación del archivo y `ProcTransaccionesNachaSourceContext` para el mapper, sin construir un `AchCycle` de conveniencia.

**Motivo:** el modelo existente ya tiene infraestructura de cola, `IncomingNachaIntegrationExecution`, eventos, transiciones, reintentos y un cliente SOAP. Duplicar cola y worker implicaría duplicar decisiones financieras y controles de idempotencia. La extensión requiere auditar y adaptar los consumidores que actualmente asumen que `.AchTransaction` y `.AchCycle` nunca son nulos. Una rama con discriminador explícito es **obligatoria**, no usar `if (AchCycleId is null)` como único discriminador.

**Alternativas descartadas:** (a) ciclos 8/9 ficticios: distorsionan auditoría y ventanas; (b) seleccionar arbitrariamente ciclo 7 u otro: asigna datos incorrectos; (c) nueva cola/worker independiente: duplica estados/reintentos/mecanismos SOAP y genera riesgo de doble despacho. **Plan contingente:** si una incompatibilidad contractual no resoluble exige separación física, registrar nuevo ADR previo al cambio; no asumirla.

## A.1. Aclaración posterior F0.3.A sobre nombre físico ACHCOL

**Corrección obligatoria:** la forma `RRRRTTT.ZZZ.N` que se usa en esta ADR es **una representación lógica/parcial**, NO una especificación exhaustiva del nombre físico que llega de ACH Colombia. El solicitante precisó que los archivos entrantes físicos terminan en **`.OUT`** (en claro) o **`.OUT.env`** (sobre digital cifrado). Las pruebas del repositorio demuestran también una forma con fecha intermedia: `RRRRTTT.ZZZ.AAAAMMDD.N.OUT[.env]`. Ejemplo `0001283.008.20260731.8.OUT` o `0001283.009.20260731.9.OUT.env`. `N` se obtiene del segmento numérico anterior a `.OUT`, nunca de las extensiones.

La propuesta de regex en §B debe leerse como **propuesta supersedida para nombres físicos entrantes**. Para F1.1 el parser debe reconocer el nombre físico y distinguir entrada en claro/cifrada; el descifrado canónico existente conserva `.OUT` y elimina únicamente `.env`. La ingestión manual admite `.OUT` y `.env`, pero el descubrimiento automático de MFT/SFTP solo cubre `.OUT.env`/`.RET.env`; **agregar soporte automático `.OUT`** en F1.1/F1.2 es parte del alcance. Véase [`F0_3_A_NAMING_OUT_ENVELOPE.md`](F0_3_A_NAMING_OUT_ENVELOPE.md) para evidencia y pruebas AT-21–AT-28.

## B. Invariantes y nombres (DEC-001 a DEC-003, DEC-010)

| Concepto | Fuente | Ejemplo H | Ejemplo independiente | Significado / restricción |
| --- | --- | --- | --- | --- |
| `FileIdModifier` | Campo 7 del header tipo 1 | `H` | `H` | Identificador alfanumérico NACHA-M; normalizar en mayúsculas. |
| `FileSequence` | `ZZZ` en nombre y mapa de identificador | `008` | `008` | `A..Z → 1..26`, `0..9 → 27..36`; contrastar encabezado/ZZZ **cuando aplique el perfil ACHCOL**. |
| `ExternalCycleNumber` | `N` del nombre `RRRRTTT.ZZZ.N` | `8` | `10` | Entero positivo del nombre, **sin tope arbitrario 7**; no exigir `N == FileSequence`. |
| `AchCycleId` | Referencia interna BD | `null` | `null` | Ausente **admisible** en `AchColFileBased`; nunca autogenerar ciclo o inferir por letra/ZZZ. |
| `OperationalDate` | Fecha válida del encabezado / reglas de calendario | Día de operación | Igual | Fecha comprobada de negocio, no el número de ciclo. |

**Ejemplos con origen ilustrativo:** `1234567.008.8` + header `H`, `1234567.009.9` + header `I`, y el caso **desacoplado** `1234567.008.10` + `H`. Para la ruta interna CFA, los tres son interpretables sin `AchCycle` 8/9/10. El manual ACH Colombia V35 suministrado describe `RRRRTTT.ZZZ.1` para el intercambio documentado: permitir `.N` aquí es **regla funcional solicitada para el flujo interno**, no afirmación de homologación del canal externo de ACH Colombia.

**PROPUESTA DE REGEX SUPERSEDIDA para nombre físico entrante (véase F0.3.A); solo contexto lógico parcial:** `AchColIncomingFileNameParser` con coincidencia estricta `^(?<route>\d{4})(?<transit>\d{3})\.(?<seq>\d{3})\.(?<externalCycle>[1-9]\d*)$` (anclado), `int.TryParse(InvariantCulture)` para evitar overflow, `seq∈[1,36]` si está activo el mapa NACHA ACHCOL y rechazo formal de valores incoherentes, negativos, cero, letras inválidas, separadores extra o sufijos con ceros iniciales. La admisión de `N>7` **no** suprime reglas de perfil/seguridad. No modificar `CenitOfficialFileNameParser`.

## C. Modelo de proceso y políticas (DEC-004, DEC-011)

`ProcessingMode` propuesto: enum `CycleBound=1`, `AchColFileBased=2`, persistido explícitamente. Valor por defecto para registros antiguos: `CycleBound` (sin migración masiva de comportamiento). La selección solo se habilita para cámara identificada inequívocamente como `ACHCOL` con `AchCol:AllowCycleIndependentInboundDispatch=true`. La funcionalidad inicia **apagada**. Un archivo CENIT usa inalterada la rama legacy; si la cámara/fecha no se pueden validar, rechazar/bloquear conforme al flujo seguro (nunca activar por mero sufijo `.8`).

**Resolución en dos pasos:**
1. `IncomingNachaCycleResolver`: determinar cámara, fecha y metadatos del header/nombre, **sin forzar** candidato `AchCycle` para la ruta ACHCOL habilitada; inferencia de cámara por nombre solo según reglas actualmente autorizadas, no por el nombre del feature flag.
2. `IIncomingNachaProcessingContextPolicy` selecciona la ruta y un resultado tipado; para `CycleBound` mantiene resolución/admisión actuales, para `AchColFileBased` devuelve `ResolvedAchCycleId=null` **con contexto operacional válido**, sin usar falsamente `IncomingNachaCycleResolutionStatus.ResueltoConfirmado` como si existiera una fila de ciclo. Crear un estado/vocabulario de resolución de *contexto* diferenciado del de ciclo y adaptarlo en UI/API para evitar campos engañosos.

**Admisión:** mantener controles comunes de autenticidad de cámara, fecha, estructura NACHA, integridad y duplicados. Extraer las validaciones específicas de `AchCycle`/ventana a una política solo de `CycleBound`; el flujo ACHCOL usa horario/calendario de **servicio/cámara** cuando exista regla respaldada, no una ventana inventada desde `N`. No saltarse el calendario general indiscriminadamente; si la política operativa real no está disponible, dejar evento explícito por política y continuar implementando sin falsificar aprobación Live.

## D. Persistencia y relaciones (DEC-005, DEC-012)

**Sin crear migraciones en F0.3**. Los siguientes cambios se planifican para F2.3 y F3.1/F3.2 (PostgreSQL y SQL Server):

| Tabla / entidad | Extensión aditiva recomendada | Integridad |
| --- | --- | --- |
| `IncomingNachaFileIngestion` | `FileIdModifier` (nullable legacy), `FileSequence` int?, `ExternalCycleNumber` int?, `ProcessingMode` persistido y `ResolutionEvidenceJson` con metadatos | Campo `ResolvedAchCycleId` ya nullable; no sobreescribir `DetectedCycleNumber` legacy ambiguo antes de migración. |
| `NachaHeader` | Ya tiene `FileIdModifier` y `CycleNumber`; mantener el significado legacy y exponer `ExternalCycleNumber` por contexto de ingesta, no reinterpretar masivamente `CycleNumber`. | Verificar ausencia de inconsistencias de desnormalización y consultas existentes. |
| `IncomingNachaDispatchQueue` | `SourceKind` no nullable (`CycleBound` default); `EntryDetailId` int? FK restrict; `AchTransactionId` int?; `AchCycleId` string? | Check constraint por origen: `CycleBound` necesita tx/ciclo; `AchColFileBased` necesita entry, tx/ciclo opcionales. Mantener unique `IdempotencyDispatchKey`. |
| `IncomingNachaTransactionLink` | Ya admite `EntryDetailId` y `AchTransactionId?` | La ruta nueva puede tener vinculación NACHA resuelta sin transacción legacy, **si la política de elegibilidad lo permite**; enlaces ambiguos o requeridos no resueltos se bloquean. |
| `IncomingNachaIntegrationExecution` | Referencia a la cola existente y `EntryDetailId?` ya disponible | Continuar con estados, auditoría, mapping snapshot, request/response y correlación.

Las migraciones serán **expand-first**, sin truncar tablas ni alterar registros históricos: columnas nuevas nullable o con default legacy seguro, FK opcionales, índices únicos existentes preservados, constraints compatibles y snapshot EF actualizado para **ambos proveedores**. No introducir FK obligatoria `AchCycleId` para ruta `AchColFileBased`.

**Compatibilidad de consumidores:** revisar específicamente `IncomingNachaDispatchPlanner`, `IncomingNachaPostProcessingPipeline` y sus etapas `ResolveContext`, `Eligibility`, `LinkCorrelation`, `ExternalDispatch`, además de command center/reportes/DTOs y configuración EF. Dejar accesos a `AchTransaction` solo dentro de rama `CycleBound`; para fuente NACHA usar `EntryDetail` + contexto explícito. Todos los puntos que emplean `queue.AchTransactionId`, `queue.AchCycleId`, `transaction.AchCycle` se enumerarán en cada PR antes de cambiar nulabilidad.

## E. Contrato de despacho e idempotencia (DEC-006, DEC-013)

**Entradas elegibles:** `IncomingNachaEntryClassification` + política de servicio determinan si la clase puede utilizar `Proc_Transacciones`. No enviar devoluciones, prenotificaciones ni tipos excluidos por el simple hecho de llevar `.8/.9`. Para clases que **exigen** vínculo previo, `AchTransactionId=null` o enlace ambiguo siempre bloquea; para orígenes que operan sobre su propio `EntryDetail`, permitir solo con regla explícita del producto y trazabilidad completa.

**Clave anti-duplicados en dos niveles:**
1. Identidad de archivo por contenido `FileHashSha256` + cámara y origen, independiente del nombre del ZIP/archivo. Reenvío con nombre cambiado y mismo contenido no genera otro pago.
2. Identidad de entrada CFA por huella canónica de origen bancario, fecha operativa, trace/identificador de transacción, código transaccional, valor, referencia de lote y destino suficiente; **excluir `AchCycleId` y `ExternalCycleNumber` como claves únicas por sí solas**, y excluir `IngestionId` como único componente distintivo en modo NACHA. Construir clave versionada `ACHCOL|Proc_Transacciones|v1|SHA256(canonicalBusinessKey)` y conservar índice único existente. Casos con mismos datos parciales pero contexto dudoso quedan en revisión manual y **no se retransmiten automáticamente**.

**Concurrencia / incertidumbre:** mantener transición atómica o toma de cola protegida; confirmar una sola ejecución efectiva por identidad, aun con dos workers. Timeout/red desconocida: estado `RequiresManualReview` (o equivalente), correlación/consulta de estado antes de nuevo intento; prohibido repetir pagos a ciegas. No habilitar `Live` por defecto ni siquiera cuando el parser ya funcione.

## F. Estrategia de SOAP CFA (DEC-006, DEC-014)

Mantener `WscfaachSoapClient.ProcTransaccionesAsync` y el motor de mapping actual. Introducir un contrato de **fuente NACHA explícita**, por ejemplo `ProcTransaccionesNachaSourceContext` (fecha, cámara, nombre, letra, ZZZ, `N`, lote, detalle, adendas y correlación). Adaptar `IProcTransaccionesRequestMapper` mediante una sobrecarga o interfaz de contextos tipados, conservando su método legacy para `CycleBound`. `IntegrationExecutionContext` del motor contract-driven actualmente recibe `transaction.Id` y `cycle.Id`; estudiar extensión por *source kind* **antes** de eliminar parámetros obligatorios y coordinar con el programa `CFA-SOAP-UX-MAP-001`.

Prohibido mapear ciegamente `ExternalCycleNumber` a `cycle.id` o al parámetro SOAP homónimo sin verificar su semántica. Dejar `sourceNotResolved`/mapping incompleto como error controlado en `DryRun` y deuda de homologación para F6, sin suspender los desarrollos F1–F5. Conservar `MappingSetId`, versión, hashes, request/response, catálogos funcionales y auditoría. El WSDL confirma operación; el XSD/convención específica de `N` continúa pendiente.

## G. Feature flag, flujo, rollback y deuda de pruebas (DEC-007 a DEC-009, DEC-015)

```text
Recepción NACHA-M
  → identificar ACHCOL y parsear `RRRRTTT.ZZZ.N` + header
  → validar FileIdModifier ↔ ZZZ y registrar N independiente
  → resolver cámara/fecha
  → política por cámara + flag:
       CENIT u otro → CycleBound (flujo intacto)
       ACHCOL + flag OFF → CycleBound legacy (flujo intacto)
       ACHCOL + flag ON → AchColFileBased (AchCycleId puede ser null)
  → admission / parse / clasificación de entradas
  → cola única (`SourceKind`, FK EntryDetail, Tx/Cycle opcionales)
  → elegibilidad específica por source, idempotencia, horario del servicio
  → contexto NACHA al mapper / validación de mapping
  → DryRun; Live solo con autorizaciones y homologación F6
  → registro de ejecución, respuesta y auditoría
```

**Rollback:** apagar `AchCol:AllowCycleIndependentInboundDispatch`; detener nuevas entradas por la ruta nueva, **no borrar** datos/eventos ni devolver el despacho al flujo legacy automáticamente. Encolados con `SourceKind=AchColFileBased` deben quedar retenidos en estado identificable hasta conciliación/manual, no reintentarse por una ruta diferente que pueda duplicar pagos. Migración expand-first queda instalada con columnas compatibles; retiro de columnas solo mediante operación de mantenimiento posterior y plan explícito.

**Política de pruebas:** unitarias/doubles y verificaciones locales se ejecutan cuando el entorno lo permite, pero las pruebas de ambiente, migraciones ejecutadas contra DB real, SOAP CFA y UAT son la **fase final F6**, posterior a F1–F5. Falta de infraestructura no detiene código. Registrar verificación no ejecutada con estado `IMPLEMENTED_UNVERIFIED`. No afirmar compilación ni autorización Live inexistentes.

## H. Matriz de implementación por microincremento

| Etapa | Cambios concretos | Evidencia local deseada y deuda F6 |
| --- | --- | --- |
| F1.1–F1.2 | Parser entrante estricto, `ExternalCycleNumber`, letra/ZZZ separados; casos H/I/H+10 | Unit tests de mapeo, overflow, `N=0` y secuencia discordante; pendientes si falta dotnet. |
| F2.1–F2.3 | `IncomingNachaOperationalContext`; resolver/admission/ingesta por cámara, estados honestos, metadatos persistidos | Casos sin filas AchCycle y CENIT intacto; persistencia real F6. |
| F3.1–F3.3 | Cola existente discriminada, migraciones duales, fingerprint/versionado, planner sin ciclo | Tests EF/índices/consistencia por provider cuando disponga runner; migración sobre BD real F6. |
| F4.1–F4.3 | Elegibilidad por clase, scheduler/workers, reintentos, command center, control de ventana independiente | Pruebas fake clock, dos workers, timeout; carga y regresión end-to-end F6. |
| F5.1–F5.3 | Origen NACHA de mapper, contrato y DryRun, simulador CFA | XML, hashes, regla `N` bajo control; validación externa/Live F6. |
| F6.1–F6.3 | Pruebas generales, contratos CFA, entrega de evidencias, rollout + plan revertible | Certificación final con entorno autorizado; nunca simular respuesta real. |

## I. Compatibilidad con el programa paralelo CFAUX

| Área común | Riesgo | Acuerdo de integración |
| --- | --- | --- |
| `ProcTransaccionesRequestMapper.cs`, `IProcTransaccionesRequestMapper.cs` | Cambios concurrentes en contrato y fuentes | Preservar método existente, introducir DTO de fuente nuevo por composición/sobrecarga, verificar `MEMORY_STATE.md` de CFAUX antes de editar (F5). |
| `IntegrationExecutionContext`, resolver genérico, mapping sets publicados | `cycle.Id` obligatorio, esquema versionado de otro programa | No alterar motor genérico en F0–F4; planificar interface de contexto de fuente con versionado CFAUX y revisión por F5. |
| `IncomingNachaPostProcessingPipeline`, etapas y tablas de auditoría | Doble ejecución, estados conflictivos | Reutilizar pipeline, cola y `IncomingNachaIntegrationExecution`; no crear worker paralelo. |
| `WscfaachSoapClient` | Efecto operativo de Live | Reutilizar sin habilitar Live ni alterar endpoint automáticamente. |
| Docs de programa CFAUX | Pérdida de estado de otro programa | **No modificar** `docs/CFA_SOAP_UX_MAPPING_IMPLEMENTATION_PACKAGE_2026-10-12/MEMORY_STATE.md` ni marcadores de su avance. |

## J. Decisiones resueltas y pendientes no bloqueantes

**Aprobadas como diseño F0.3:**
- **DEC-001** ruta aislada `AchColFileBased` por cámara/flag.
- **DEC-002** cuatro conceptos distintos (`FileIdModifier`, `FileSequence`, `ExternalCycleNumber`, `AchCycleId`).
- **DEC-003** parser entrante ACHCOL separado.
- **DEC-004** flag apagada por defecto; DryRun antes de Live.
- **DEC-005** ampliar cola actual con discriminador y FK a EntryDetail, Tx/Cycle opcionales en la ruta nueva.
- **DEC-006** reutilizar cliente y auditoría SOAP; contexto NACHA tipado y mapping homologado.
- **DEC-007/008/009** Live únicamente autorizado; pruebas finales en F6 sin frenar desarrollos; ZIP completo por microentrega.
- **DEC-010–015** convenciones de nomenclatura, resolución, esquema, idempotencia, SOAP, rollback aquí definidas.

**Pendientes de homologación/implementación (NO bloquean F1–F5):** semántica CFA del eventual campo de ciclo externo, fuentes `cycle.*` del mapping publicado, regla oficial de sufijo `.N` para canales directos ACH Colombia (diferente del requerimiento interno), calendario/ventana externa del servicio CFA, snapshot EF por ambos proveedores, reuso con CFAUX si cambia su motor. **No se inventarán valores contractuales.**

## K. Resultado F0.3

ADR concluido por análisis de código; **no se modificaron archivos productivos**, no se ejecutaron `dotnet build`, pruebas de BD, SOAP ni UAT. `F0.3=VERIFIED` **solo para documentación y toma de decisiones arquitectónicas**. **Siguiente incremento inequívoco: `F1.1`, implementación del parser entrante ACHCOL y pruebas locales de nombres `.8/.9/.10`.**
