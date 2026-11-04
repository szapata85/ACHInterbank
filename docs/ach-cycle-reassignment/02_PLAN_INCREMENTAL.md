# ACH-CR-001 — Plan de implementación por incrementos en modo chat

## Contrato de incremento (obligatorio para I01+)

**Entrada:** ZIP completo del incremento más reciente, `MEMORY_STATE.md`, petición del paso específico. **Salida:** nuevo ZIP completo con el código/documentación resultantes; `MEMORY_STATE.md`, `CHANGELOG.md` y registro de validación estática actualizados. No entregar solo diff y no perder archivos previos. **Límites:** una finalidad verificable por paso, reutilización primero, Clean Architecture/SOLID, sin integración real. Confirmar lo observado con pruebas estáticas; anotar separadamente lo que requiere `dotnet`/ambiente.

Un incremento queda `IMPLEMENTADO_ESTATICAMENTE` solo si tiene código integrado y validaciones estáticas ejecutadas. `DONE_RUNTIME` y `CLOSED` requieren evidencia ejecutable/aceptación distinta; no suponerla.

## I00 — Inventario, arquitectura y memoria [ENTREGADO]

**Objetivo:** congelar base, investigación focal, arquitectura y protocolo de avance.

- Incorporar documentación autocontenida y validador estático de integridad/documentación.
- Registrar riesgos que no se pueden resolver solo por inspección.
- Preservar byte a byte todos los archivos de la base; sin implementar la función ni nuevas migraciones.
- Evidencia: `04_VALIDACION_ESTATICA.md` y `CHANGELOG.md`.

**Criterio:** ZIP completo legible, archivos previos intactos, docs y memory accesibles, script validado. **Siguiente:** I01.

## I01 — Contrato, elegibilidad y selección del destino [IMPLEMENTADO_ESTATICAMENTE — PRUEBAS RUNTIME PENDIENTES]

**Objetivo:** decidir **sin persistir cambios** si una transacción es apta y cuál es su ciclo siguiente en la MISMA cámara.

**Reutilización:** `IOperationalCalendarService`, `IOperationalCycleWindowResolver`, `ICycleTransactionPolicy`, `AchCycle`, estado y membresía `AchFileExportTransaction`; revisar contratos de dispatch. Construir servicio/caso compacto solo si no existe equivalente.

**Tareas:**
1. Recorrer desde creación a exportación y dispatch; determinar exactamente qué dato/evidencia bloquea cada estado.
2. Fijar predicado fail-closed: saliente + Pending + ciclo vencido + no exportación/despacho/compensación + cámara vigente + fecha valor admisible.
3. Resolver próximos ciclos por fecha y corte; filtrarlos por política, tipo (crédito/débito/prenotificación/etc.), naturaleza y calendario; no saltar restricciones del último ciclo.
4. Aclarar las condiciones para creación en sábado/feriado: `BatchResolver.EnsureCycleIsOpenForTransactions` puede fallar en proyección futura ACH Colombia; evaluar **sin alterar comportamiento aún** o aplicar ajuste mínimo si es defecto demostrado y probado con análisis estático.
5. Pruebas focales de política determinista, diseñadas o implementadas según factibilidad. No introducir nuevas clases por mero patrón.

**Archivos candidatos:** `Application/ACH/Interfaces`, `Application/ACH/Models` (si procede), `Persistence/ACH/Services/Implementation`, `tests/Cfa.ACHInterbank.Tests`; no modificar Quartz/EF.

**Aceptación:** selección rechaza casos inciertos; misma cámara; ciclo posterior y válido; cobertura casos 01–12 de `03_REGLAS_Y_ESCENARIOS.md`; sin escrituras.

## I02 — Cambio atómico mínimo y auditoría [IMPLEMENTADO_ESTATICAMENTE — PRUEBAS Y CARRERAS RUNTIME PENDIENTES]

**Objetivo:** mover una única transacción elegible que no haya sido exportada.

**Tareas:**
1. Incorporar comando de reasignación en el caso de uso de Application / Persistence usando el resolver de I01.
2. Revalidar elegibilidad frente a concurrencia; especificar protocolo de no solapamiento con exportación/dispatch.
3. Actualizar `AchTransaction.AchCycleId`, `AchBatchId` y dependencias que correspondan; reutilizar lógica existente de composición de lotes y evitar creación duplicada.
4. Mantener fecha valor dentro del mismo día. Revisar contadores/totales materializados de lote contra código actual.
5. Escribir evento `AchTransactionStateEvent` con IDs old/new, causa, idempotencia y timestamps dentro de la misma transacción.
6. Verificar no duplicación tras segunda invocación y ante un fallo previo al commit (pruebas estáticas + tests agregados sin ejecutarlos si no hay SDK).

**Aceptación:** un solo cambio lógico; sin cambio de identidad/monto/trace; evento congruente; si hay archivo o carrera, skip; sin migración salvo evidencia de necesidad.

## I03 — Ejecución automática controlada y recuperación [IMPLEMENTADO_ESTATICAMENTE — DESACTIVADO]

**Objetivo:** detectar y trasladar automáticamente pendientes vencidas, usando Quartz existente.

**Tareas:**
1. [I03] Consultar ciclos vencidos con parámetros, paginación keyset y límite de escaneo; no barrer historia sin límite.
2. Añadir `ITaskHandler` delgado (un código de task nuevo si resulta necesario); registro DI por patrón existente; sin timer ni servicio alternativo.
3. Ejecutar caso I02 individualmente, reportar totales MOVED/SKIPPED/ERROR y razones agregadas sin datos sensibles.
4. Probar idempotencia de reejecución y fallas aisladas por candidato, sin pausa/locks globales.
5. Verificar orden relativo a procesos de exportación, despacho y cierre. Registrar como **condición de despliegue** lo no verificable estáticamente.

**Aceptación estática I03:** handler y task en catálogo/seeder deshabilitada, doble guardia de activación, keyset con límites y resumen; nueve pruebas escritas (NO ejecutadas). Reejecución y carreras reales aún sin homologar: I05/UAT. Ninguna presentación externa realizada por el job.

## I04 — Fin de jornada / día no hábil / fecha valor [IMPLEMENTADO ESTÁTICAMENTE]

**Objetivo:** resolver el último ciclo elegible y diferencias entre día de creación, procesamiento y fecha valor.

**Tareas:**
1. Estudiar contratos existentes de `EffectiveEntryDate` y reglas por cámara; decidir si puede pasar al siguiente día hábil sin nueva autorización.
2. Para fecha valor no modificable: dejar `NO_ELIGIBLE_VALUE_DATE` y observación, sin transferir.
3. Si la normativa y el contrato funcional habilitan fecha valor futura: actualizar lo estrictamente permitido y vincular el lote destino dentro de la misma transacción; registrar old/new en evento.
4. Preservar operación CENIT de liquidación y regla diferenciada de rechazos de ciclos 4/5.

**Aceptación:** sábado, festivo, C1 del siguiente día hábil, corte final y tipos restringidos; ninguna mutación silenciosa de fecha valor.

## I05 — Guardias de regresión de exportación y CENIT [IMPLEMENTADO ESTÁTICAMENTE]

**Objetivo:** cerrar los bordes de doble presentación e interacción con mecanismos existentes.

**Tareas:**
1. Recorrer generación/exportación `PUBLISH`, estados de archivo, `AchFileExportTransaction`, attempts y despacho; confirmar fail-closed con escenarios de carrera.
2. Verificar que se excluyan respuestas pendientes, archivos de estado histórico incierto y movimientos ya compensados/retornados.
3. Proteger `CenitCycleQueue` / `CenitCycleExecutionService` / `LiquidityOptimizationService` de reasignaciones paralelas o dobles, sin rediseñarlos.
4. Consolidar regresiones estáticas y tests escritos (sin alterar golden files sin evidencia).

**Aceptación:** no se abre vía a reenvío externo ni se altera compensación CENIT por esta funcionalidad.

## I06 — Cierre documental, evidencia y entrega operativa (no go-live) [ENTREGADO ESTÁTICAMENTE]

**Objetivo:** estabilizar el conjunto e informar precisamente qué se sabe y qué falta.

**Tareas:**
1. Revisar diffs, dependencias, imports, registro DI/Quartz, árbol solución y consistencia transversal.
2. Ejecutar validadores estáticos disponibles; preparar comandos dinámicos como **pendientes**, nunca simularlos.
3. Actualizar arquitectura as-built, casos de prueba, decisiones y memoria; resolver o visibilizar cualquier `OPEN`.
4. Generar ZIP completo I06 y compararlo con el ZIP I05; inventario de archivos cambiados y checksum del paquete.

**Aceptación:** `IMPLEMENTADO_ESTATICAMENTE` si aplica, evidencia clara y lista de validaciones posteriores; UAT/go-live no declarados.

## Política de diferencias e incremento ligero

- Si una fase ya está cubierta, registrar evidencia y omitir código redundante (fase documental).
- Si aparece impedimento con datos o contratos externos, implementar únicamente la parte segura, documentar la excepción, **no parar** el avance con otra capacidad independiente.
- Si un incremento necesita muchos cambios transversales, dividirlo en `I02a/I02b`, actualizando memory y entregando ZIP completo de ambos; no saltar checks.
- Solo una migración EF si es estrictamente necesaria; preserve SQL Server y PostgreSQL de manera consistente con repositorio.
- La interfaz de usuario no es obligatoria para el MVP. Priorizar trazabilidad existente y no crear nueva página por defecto.

**Estado al cierre I04:** I00–I04 entregados estáticamente; **2 incrementos restantes: I05, I06**. I04 permite proyección futura solo en originación, conserva misma fecha valor en reasignación y rechaza fin de jornada sin destino autorizado. No habilitar Quartz antes de I05/homologación.

## Resultado real de I04 (2026-10-25)

Se resolvió sábado/festivo → próximo día hábil **en el momento de crear**: el enrutador ya seleccionaba día hábil; `BatchResolver` permite `Before` solo para fecha local anterior a la fecha de ciclo, valida jornada y políticas, evita ciclos cerrados/cancelados. Se preserva bloqueo para intento de usar un ciclo aún no abierto el mismo día.

**No se implementa modificación de fecha valor de una instrucción ya creada** al pasar del último ciclo; al no haber autorización contractual comprobada, el motivo interno `NO_ELIGIBLE_TARGET_SAME_VALUE_DATE` sigue siendo final y se mantiene Pending sin cambio. No cambiar arbitrariamente a `NO_ELIGIBLE_VALUE_DATE` para evitar romper contratos internos existentes. Ver `10_I04_IMPLEMENTACION_Y_EVIDENCIA.md`. I05 es siguiente.

## Resultado real I05 (2026-10-25)

Se reforzó `AchFileExportAuditService` en admisión relacional NACHA `Serializable`; la ruta de seguridad NACHA audita su membresía antes de exponer un archivo; la elegibilidad se niega ante evidencia de exportación **del ciclo** y ejecución/cola/optimización CENIT. Se escribieron cuatro tests (dos en elegibilidad y dos en auditoría) y validador I05. No se habilitó Quartz ni se realizaron integraciones. Falta **I06 (1 incremento)**; la prueba de competencia real es condición de go-live posterior, no puede declararse completada por validación estática.

## Cierre definitivo del plan I00–I06 (I06, 2026-10-25)

| Entregable | Resultado |
|---|---|
| Contratos y selección I01 | Código y pruebas escritas, validación estática; runtime pendiente |
| Comando persistente I02 | Código y auditoría reutilizada; transacciones reales no ensayadas |
| Automatización I03 | Adaptador Quartz opt-in; **Disabled** + flag false; no ejecución productiva |
| Calendario/fecha valor I04 | Proyección de originación al siguiente día hábil; sin rollover automático de transacciones ya creadas |
| Admisión NACHA y regresiones CENIT I05 | Guardias estáticas; carreras interprocesos reales no probadas |
| I06 | Matriz de cierre, memoria, validadores, ZIP completo y protocolo de homologación |

**Incrementos adicionales planificados: 0.** La siguiente actividad es validación/homologación, no una implementación presuntamente aprobada. Si aparecen defectos en las pruebas dinámicas, abrir correcciones focales nuevas con nueva versión/registro, sin alterar retroactivamente I06.
