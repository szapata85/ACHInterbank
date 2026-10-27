# ACH-CR-001 — Reglas funcionales, política y escenarios de aceptación

## Autoridad de reglas y separación

| Cámara/fuente | Hecho sustentado | Uso en diseño |
|---|---|---|
| CENIT DSP-152 Anexo 2, §3.3 | Archivo recibido fuera de ventana se encola para ingreso en el siguiente ciclo por el sistema CENIT | Precedente de diferimiento del **operador externo**; no prueba que ACH Interbank pueda reenviar una transacción propia |
| CENIT DSP-152 Anexo 2, §5.2 | Por insuficiencia en ciclos 1–3 hay traslado automático; en 4/5 se rechazan pendientes después de optimización | Mantener decisión de liquidez CENIT separada del traslado de pendientes internos |
| CENIT DSP-152 Anexo 2, §3.1 y §3.2 | 5 ciclos hábiles; ciclo 5 restringe tipos | Filtrar destino por elegibilidad, no simplemente por ordinal |
| CENIT Manual Usuario, §3.4–3.5 | Ciclo 4 último para monetarias ordinarias; 5 orientado a devoluciones/ROR | Evitar salto monetario 4 → 5 |
| ACH Colombia V35, §2.4.1 | 5 ciclos en versión vigente; el quinto con restricciones de débito | Se consulta catálogo/política V35 efectiva, no horarios hardcodeados |
| Documento candidato 7 ciclos (adjunto) | Lista horarios/capacidades candidata pero inconsistencias visibles en ciclo 4 y leyenda | No activar sin fuente/fecha efectiva validada |

**Reglas de diseño que no vienen automáticamente de la norma externa:** criterios de idempotencia, `Pending` + no-exportación, uso de auditoría, límite de intentos, control de concurrencia, tratamiento de fecha valor. Son decisiones internas propuestas y deben comprobarse contra la configuración efectiva y contratos de producto.

## Matriz de decisión inicial (MVP)

| Condición | Acción | Motivo |
|---|---|---|
| Saliente ordinaria, `Pending`, ciclo expirado, no incluida en exportación, sin despacho, destino elegible misma cámara y fecha valor válida | `MOVED` | `UNSENT_CYCLE_EXPIRED` |
| Ciclo sigue abierto o programado para futuro | `SKIPPED` | `CYCLE_NOT_EXPIRED` |
| Dirección desconocida o entrante / return / ROR / evento de compensación | `SKIPPED` | `OUT_OF_SCOPE` |
| `State` distinto a `Pending` | `SKIPPED` | `NOT_PENDING` |
| Pertenece a cualquier archivo `AchFileExportTransaction`, incluso `HistoricalUnknown` | `SKIPPED` | `EXPORT_EVIDENCE_PRESENT` |
| Dispatch/transport/transmisión sin resultado definitivo o desconocido | `SKIPPED` | `EXTERNAL_STATUS_UNCERTAIN` |
| Ciclo siguiente no admite el tipo o la cámara | Buscar próximo permitido | `INELIGIBLE_TARGET_CYCLE` |
| No se encuentra siguiente ciclo permitido con fecha valor autorizada | `SKIPPED` y observación | `NO_ELIGIBLE_TARGET` |
| Se detecta concurrencia o ciclo ya cambió | `SKIPPED` | `STALE_ASSIGNMENT` |
| Reejecución con el mismo estado ya trasladado | `SKIPPED`, sin evento duplicado | `ALREADY_REASSIGNED` |
| CENIT ya en optimización/cola/liquidación | `SKIPPED` | `CENIT_PROCESSING_OWNED` |

Nombres de motivos son **propuesta interna**, no códigos de devolución de cámara. No emitirlos como Rxx/Dxx ni registrarlos como nueva causal de devolución.

## Casos para implementación y pruebas (sin ejecutar en I00)

| ID | Caso | Resultado mínimo |
|---|---|---|
| CR-01 | Sábado se crea crédito y proyecta lunes C1 | Persistir asignación futura válida; no ejecutar en sábado |
| CR-02 | Lunes C1 vence y nunca hubo exportación | Mover a C2 elegible de misma cámara |
| CR-03 | C2 también vence sin envío | Mover a siguiente elegible; mantener mismo ID |
| CR-04 | C1 todavía abierto | No mover |
| CR-05 | Archivo creado `PUBLISH` con membresía | No mover ni generar segundo archivo |
| CR-06 | Enviado pero sin respuesta/ack | No mover; espera conciliación |
| CR-07 | Archivo con `HistoricalUnknown` | No mover; evidencia ambigua |
| CR-08 | Crédito en ACH Colombia con ciclo 5 permitido | Solo si política vigente habilita exactamente esa clase |
| CR-09 | Débito con ciclo 5 restringido ACH V35 | Saltar ciclo 5; evaluar siguiente jornada y fecha valor |
| CR-10 | Monetaria CENIT después de ciclo 4 | No presentar en C5 reservado a otras clases |
| CR-11 | CENIT insuficiencia de fondos C1–C3 | Respetar `LiquidityOptimizationService`; no duplicar traslado |
| CR-12 | CENIT insuficiencia de fondos C4–C5 | No convertir rechazo regulado en reintento interno |
| CR-13 | Último ciclo elegible sin siguiente día autorizado | Dejar sin mover, motivo de fecha valor |
| CR-14 | Fecha festiva configurada | Elegir jornada hábil según calendario; no sumar 24 h ciegamente |
| CR-15 | Destino pertenece a otra cámara preferida | Rechazar cambio de cámara automático |
| CR-16 | Dos invocaciones intentan mover misma TX | Solo una actualización/evento efectivo |
| CR-17 | Exportación concurrente con traslado | Uno de los dos debe prevalecer sin doble envío; demostración requerida |
| CR-18 | Excepción en mitad de operación | Sin cambio parcial de ciclo/lote/auditoría |
| CR-19 | Lote destino existente compatible | Reusar, sin crear lote redundante |
| CR-20 | Lote incompatible (clase/empresa/fecha) | No mezclar; resolver lote correcto |
| CR-21 | Devolución o Return original | Excluir de este caso de uso |
| CR-22 | Transacción desconocida por clasificación histórica | Fail-closed; no inferir saliente |
| CR-23 | Reproceso del job después de un reinicio | No repetir traslado ni generar segunda salida |
| CR-24 | Candidato 7 ciclos ACHCOL aún inactivo | No alterar versión efectiva V35 |

## Criterios medibles de aceptación

1. Identidad `AchTransaction.Id` y `TransactionExternalId` preservada.
2. Cámara y participantes, cuentas, monto, tipo, addendas y TraceNumber sin mutación por simple reprogramación.
3. `AchCycleId` y `AchBatchId` coherentes; lote destino compatible según los criterios existentes.
4. Trazabilidad `fromCycle`/`toCycle`/fecha/reason en evento; sin claves duplicadas en reintentos.
5. Operaciones exportadas o con estado externo desconocido nunca elegibles.
6. Ventanas y calendarios derivados de políticas efectivas de su cámara.
7. Ninguna fecha valor reescrita sin aprobación normativa/contractual de I04.
8. En ausencia de información suficiente: no ejecutar traslado, reportar motivo no sensible.

## Cuestiones por cerrar con evidencia en incrementos

- ¿Cuál es el paso exacto donde se confirma que un archivo generado es inmutable/expuesto a MFT? Revisar canal y ciclo de vida de `AchFileExport`.
- ¿Cuáles son los estados persistidos del despacho `ContrapartidaDispatchItem` que prueban inicio/fin? No inventar enumeraciones.
- ¿Cómo se deducen totales de `AchBatch` cuando la transacción cambia de lote? Reutilizar mecanismo existente, no recalcular con supuestos.
- ¿Puede la originación trasladarse a siguiente día hábil modificando `EffectiveEntryDate`? Requiere contrato y norma, no inferir.
- ¿La política de siete ciclos ACH Colombia tiene fecha efectiva y corrección oficial de C4? En el estado de proyecto: NO.


## Estado verificable de escenarios al terminar I01

Hay **12 tests unitarios escritos** en `AchPendingCycleEligibilityServiceTests.cs` que cubren alcance read-only, calendario proyectado, política de ciclo, exportación sin/ con membresía, despacho, cola, estado y clasificación. Las pruebas **no se ejecutaron** (`dotnet` ausente). La creación real en sábado aún no está corregida; ver `07_I01_IMPLEMENTACION_Y_EVIDENCIA.md`. I02–I05 deben demostrar que la aplicación de la selección se realiza sin condición de carrera y sin duplicación de archivos.


## Estado de cobertura I02

Se escribieron pruebas focales para CR-02, CR-04, CR-05 (membresía NACHA), CR-16 (segunda invocación secuencial), CR-19 y CR-20; sin ejecución por ausencia del SDK .NET. CR-17 (exportador concurrente), CR-18 (rollback frente a error/serialización) y CR-23 (job recuperador) requieren pruebas con SQL Server/PostgreSQL en I05 y I03; **NO están certificados**. Los rechazos del servicio son códigos internos (`ASSIGNMENT_CHANGED`, `TARGET_BATCH_INCOMPATIBLE`, etc.), NO causales ACH.

## Observaciones de I03 (scheduler)

| Escenario | Decisión / criterio I03 |
|---|---|
| Tarea inexistente/Disabled o flag ausente | No se ejecuta ni consulta candidatas. |
| Tarea Paused aun con flag true | No se ejecuta. |
| Ciclo todavía no vencido según TZ | No se invoca el comando. |
| Saliente Pending con archivo/despacho registrado | No se preselecciona; I02 revalida todas las evidencias. |
| Una candidata no pasa la política I01 | Skip, continuar siguiente id sin escritura. |
| Error en una transacción | Continuar las demás, informar resumen y fallar ejecución Quartz al final. |
| Se alcanza BatchSize o MaxScanned | Terminar ejecución; sin escaneo ilimitado. |
| Más pendientes inelegibles que MaxScanned al inicio | Riesgo de inanición en ejecuciones sucesivas; I06 para medir y resolver con evidencia. |
| Otra aplicación está exportando al mismo tiempo | **No está demostrado seguro**; no activar antes de I05. |

No se modifica fecha valor ni lógica de cámaras: I04/I05 siguen pendientes.

## I04 — Decisión de jornada y fecha valor (as-built)

| Caso | Resultado implementado estáticamente | Evidencia pendiente |
|---|---|---|
| Alta sábado→lunes hábil / festivo→martes hábil | Routing elige jornada; BatchResolver acepta sesión futura `Before` solo si `LocalNow.Date < ProcessingDate` y calendario confirma hábil. `EffectiveEntryDate` = procesamiento. | `AchCycleCalendarProjectionTests` tests escritos, sin runtime. |
| Originación antes de apertura el MISMO día | `Before` todavía rechaza; no se simula entrega anticipada. | Runtime. |
| Ciclo en día inhábil / Closed / Cancelled | Rechaza en originación no-CENIT; Routing omite estados terminales. | Runtime. |
| Último ciclo del día vencido sin destino mismo día valor | I01 responde `NO_ELIGIBLE_TARGET_SAME_VALUE_DATE`; I02 no modifica `EffectiveEntryDate`. | Acuerdo contractual explícito para cualquier cambio de fecha, NO inferido. |
| Tipo restringido del ciclo próximo | `ICycleTransactionPolicy` conserva autoridad. | Integración con catálogo regulatorio efectivo. |
| Cola CENIT fuera de ventana | Mecanismo preexistente; I04 no lo reemplaza. | Regresión I05. |

Los códigos anteriores son internos, NO causales externas NACHA o CENIT. La transferencia **entre días** de instrucciones ya creadas permanece fuera del alcance autorizado.

## Ajustes de política I05

| Caso adicional | Resultado | Motivo |
|---|---|---|
| Archivo NACHA ya registrado para cualquier transacción del **ciclo fuente**, incluso membresía completa que no contiene el ID pendiente | `SKIPPED` | `SOURCE_CYCLE_EXPORT_EXISTS` (decisión interna conservadora) |
| Ciclo fuente CENIT ya tiene ejecución, cola relacionada o decisión de liquidez de dicho ciclo | `SKIPPED` | `CENIT_CYCLE_PROCESSING_EVIDENCE` |
| NACHA construido antes de que I02 cambie el ciclo, pero auditado después | **Fallará la admisión**; nunca publicar el payload | Validación exacta en `AchFileExportAuditService` |
| I02 intenta mover después de que audit registró NACHA | **No mover** | Evidencia por ciclo y/o membresía |
| Ruta alternativa seguridad genera salida | **Auditar antes de disponibilidad/descarga** | Reutilizar `IAchFileExportAuditService` |
| La DB falla o hay concurrencia no serializable en relacional | **No publicar automáticamente / remitir a revisión** | `NACHA_EXPORT_AMBIENT_TRANSACTION_UNSUPPORTED` o error relacional |

Una prueba estática es solo evidencia estructural; estas reglas deben validarse con transacciones simultáneas en proveedores reales antes de habilitar Quartz.

## I06 — Verificación de aceptación y deuda de homologación

| Grupo | Evidencia estática acumulada | Pendiente para aceptación |
|---|---|---|
| I01 elegibilidad (CR-01 a CR-12) | Tests escritos; filtro de alcance, estados, política y evidencia | Ejecutar xUnit y casos con catálogos reales sin pagos |
| I02 persistencia (CR-16, CR-18–20) | Comando y evento auditado en mismo bloque transaccional por inspección | Proveedor real SQL Server/PostgreSQL, rollback y conflictos concurrentes |
| I03 recuperación (CR-23) | Handler y seed Disabled; límites de lectura | Ejecución manual controlada + reejecución sin duplicados; starvation |
| I04 calendario (CR-01, CR-08–12) | Casos sábado, festivo, ciclo terminal; fecha valor conservada | Verificar zona horaria / festivos oficiales / corte final; confirmar política de rollover |
| I05 exportación (CR-17, CR-05) | Admisión NACHA serializable y auditoría previa en rutas conocidas | Dos procesos en carrera y fallos parciales filesystem/MFT; proveedor real |
| CENIT | Selección separada y guardias de ejecución/cola/liquidez | Regresión de liquidación y devolución sin interferencia |

**No confundir cobertura de código/tests escritos con aceptación bancaria.** La matriz operativa que habilita GO/NO-GO está en `13_PLAN_HOMOLOGACION_GO_NO_GO.md`.
