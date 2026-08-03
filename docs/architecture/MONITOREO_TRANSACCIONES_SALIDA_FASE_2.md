# Monitoreo de transacciones de salida — Fase 2

## 1. Objetivo

Implementar una consulta operativa, segura y comprobable del ciclo de vida de las transacciones originadas por CFA. El monitor es estrictamente de solo lectura y su raíz canónica es `AchTransactions`.

## 2. Alcance

La fase incorpora listado paginado, filtros, detalle, consolidación funcional, archivos exactos, respuestas, devoluciones, línea de tiempo, permisos, auditoría, navegación, SPA responsiva y pruebas automatizadas. No incorpora reprocesos, aprobaciones, rechazos manuales, transmisión, SOAP ni movimientos monetarios.

## 3. Estado base

- Rama: `ACH-Interbank-Postgresql`.
- HEAD inicial: `39e03247b33f5dbf93dd0726617535ac39fc5175`.
- Mensaje: `fix: restaurar reproceso gobernado de respuestas en revisión`.
- Estado inicial: sin cambios versionados; existía `docs/uat/certificados_pruebas/` como directorio local no rastreado y se mantuvo intacto.
- Se preservó la transición `RequiereRevisionManual -> PendienteReproceso` y no se modificaron sus archivos asociados.

## 4. Documentos analizados

- `AGENTS.md`.
- `docs/ai/ACH_PHASE6_CONTEXT.md`.
- `docs/architecture/MONITOREO_TRANSACCIONES_SALIDA_ANALISIS_VERIFICABLE.md` (Fase 0).
- `docs/architecture/MONITOREO_TRANSACCIONES_SALIDA_FASE_1A.md`.
- Código y configuraciones EF del HEAD para contrastar las relaciones descritas.

## 5. Decisiones

- Solo entran registros con `Direction = Outgoing` y `ClassificationStatus = Determined` persistidos.
- `FinancialInstitution.IsDefaultSource` no participa en la consulta ni reinterpreta historia.
- La política funcional es pura, determinista e independiente de EF y de la API.
- No se creó una tabla universal ni un modelo materializado.
- No se generaron migraciones ni índices: el esquema de Fase 1A resultó suficiente para la consulta validada.

## 6. Arquitectura

- Application: contratos, DTO, excepciones funcionales y política de consolidación.
- Persistence: proyección EF de solo lectura, detalle por consultas pequeñas, auditoría y seed idempotente.
- External/Api: políticas de autorización, composición y controlador REST.
- SPA: modelos, cliente HTTP, listado, detalle, seguridad de ruta y navegación.

El servicio especializado es `IOutgoingTransactionMonitoringQueryService`, implementado por `OutgoingTransactionMonitoringQueryService`.

## 7. Fuentes persistidas

La consulta usa relaciones demostradas desde `AchTransactions`: `AchTransactionStateEvents`, `AchFileExportTransactions`, `AchFileExports`, `ContrapartidaDispatchItems`, `ContrapartidaDispatchAttempts`, `AchResponses`, `IncomingNachaProcessingEvents` y `LiquidityOptimizationDecisions`. Cada fuente solo aporta hechos que persiste explícitamente.

## 8. Política de estados

`OutgoingTransactionMonitoringStatusPolicy` recibe hechos proyectados y produce:

- estado del proceso;
- resultado inicial;
- situación posterior;
- indicador y razón de atención.

Un error técnico no se convierte en rechazo funcional. Una correlación ambigua o en revisión genera atención, no un resultado de cámara inventado.

## 9. Proceso

El proceso distingue creada, en procesamiento, procesada y error técnico según despacho, intentos, eventos de estado y membresía de archivo. Los textos visibles están en español y no exponen enumeraciones como lenguaje primario.

## 10. Resultado inicial

La precedencia funcional conserva certificación, aceptación, rechazo funcional y éxito de integración como resultados diferentes. Ningún código, incluido `R96`, se trata como éxito global fuera de su contexto persistido.

## 11. Situación posterior

La devolución es una situación posterior. Una aceptación o certificación previa permanece en el resumen y en la línea de tiempo; no se reemplaza toda la historia por el texto “Devuelta”.

## 12. Devoluciones

Las devoluciones se obtienen de eventos persistidos y conservan fecha, estado y causal disponible. La ausencia o ambigüedad causal se presenta como no determinada, sin inferencias.

## 13. Archivos

La única asociación transacción–archivo se resuelve mediante `AchFileExportTransaction`. Las versiones mantienen membresía independiente, posición y fecha de inclusión. No se asocia el último archivo del ciclo ni se usan aproximaciones temporales.

## 14. Evidencia

- Generación permite mostrar `Generado`.
- Protección permite mostrar `Protegido`.
- Transmisión exige referencia externa y fecha persistida.
- Acuse exige evidencia de transmisión, fecha y código de acuse.

Sin esos datos se muestra `Sin evidencia de transmisión` o `Pendiente de información externa`.

## 15. API

- `GET /api/transactions/outgoing-monitoring`.
- `GET /api/transactions/outgoing-monitoring/{id}`.

Los endpoints exigen autenticación y permiso, devuelven `404` fuera del alcance visible y traducen validaciones a códigos `OUTGOING_MONITOR_*` con mensajes funcionales en español.

## 16. Filtros

Se implementaron fechas, cámara, ciclo, entidad destino, identificador, número de seguimiento, tipo de operación, proceso, resultado inicial, situación posterior, devolución, atención e importes. Identificadores se recortan, limitan y validan contra caracteres permitidos.

## 17. Paginación

- Predeterminado: 25.
- Permitidos: 10, 25, 50 y 100.
- Máximo: 100.
- Rango máximo: 90 días.
- Orden predeterminado estable: `CreatedAt DESC, Id DESC`.

Conteo, filtros, orden y página se ejecutan en base de datos.

## 18. Seguridad

Permisos agregados:

- `OutgoingTransactions.Monitor.Read`.
- `OutgoingTransactions.Monitor.TechnicalDetail.Read`.

El seed asigna lectura operativa a Admin y Operator, y detalle técnico a Admin. La autorización se evalúa en backend mediante políticas; el menú no sustituye el control del endpoint.

## 19. Enmascaramiento

La cuenta se proyecta en backend como últimos cuatro dígitos precedidos por asteriscos. La cuenta completa, XML, credenciales, tokens y contenidos SOAP no forman parte de los DTO.

## 20. Auditoría

Se reutiliza `AuditLog`. Se registra operación, usuario, correlación, rango, cámara, ciclo, filtros sanitizados e identificador interno de detalle. No se persisten cuentas, XML, JWT, secretos ni cadenas de conexión.

## 21. UX

La SPA usa Angular Material, formularios reactivos, `MatTable`, `MatPaginator`, `MatSort`, tarjetas móviles, estados de carga, vacío y error recuperable. Se registró `es-CO` para moneda y fechas. Las peticiones de sesión y auditoría de navegación son no bloqueantes; las búsquedas anteriores se cancelan con `switchMap`.

## 22. Rendimiento

La consulta parte de `AchTransactions`, usa `AsNoTracking`, proyección directa, subconsultas correlacionadas traducibles y paginación en servidor. No usa `Include` de colecciones, consultas por fila, XML ni entidades completas. El detalle materializa exclusivamente los conjuntos de una transacción y consolida su línea de tiempo en memoria.

## 23. SQL Server

La prueba relacional creó una base aislada, aplicó migraciones, sembró clasificación persistida, dos versiones de archivo y aceptación seguida de devolución. Resultado: 1 prueba relacional verde. La aplicación local desplegada se validó contra el contenedor SQL Server existente.

## 24. PostgreSQL

La misma prueba se ejecutó contra PostgreSQL 16 en una base aislada, con migraciones, filtros, orden, proyección, detalle y limpieza de la base temporal. Resultado: 1 prueba relacional verde. El contenedor de prueba se detuvo al finalizar sin eliminar su volumen.

## 25. Pruebas

- Build .NET Release: verde, 0 advertencias y 0 errores.
- Política y validación de consulta: 11/11 verdes.
- SQL Server/PostgreSQL reales: 2/2 verdes.
- Build Angular de producción: verde.
- Angular completo: 676/676 verdes.
- Angular focalizado del monitor: 8/8 verdes.
- Playwright contra API, SPA y SQL Server locales desplegados: 2/2 verdes, escritorio y móvil, sin mocks.
- Regresión backend: 2.104 pruebas verdes y 7 omisiones condicionadas preexistentes en la corrida integral. Dos pruebas multi-motor de cámaras solicitaron configuración ambiental explícita; se repitió únicamente ese bloque con la configuración local y quedó 2/2 verde. Resultado compuesto local: 2.106 aprobadas y 0 fallos en esa ejecución.

### Cierre de integración continua

El pipeline general ejecutaba las pruebas relacionales del monitor sin las variables ni los motores requeridos porque estas no tenían una categoría multi-motor. Se agregó `OutgoingMonitorMultiDb` a las dos pruebas por proveedor y la regresión general ahora excluye tanto `ClearingHouseMultiDb` como `OutgoingMonitorMultiDb`. El job dedicado `outgoing-monitor-multidb` inicia contenedores aislados de SQL Server y PostgreSQL, comprueba activamente su disponibilidad, define `RUN_OUTGOING_MONITOR_MULTIDB`, `OUTGOING_MONITOR_SQLSERVER_CONNECTION_STRING` y `OUTGOING_MONITOR_POSTGRES_CONNECTION_STRING`, y ejecuta únicamente la nueva categoría sin permitir omisiones.

La validación local del filtro general aprobó 2.102 pruebas, no presentó fallos y conservó las 7 omisiones condicionadas preexistentes; el TRX confirmó que ninguna de las dos pruebas relacionales del monitor fue ejecutada. La suite dedicada aprobó 2/2 contra los motores reales. La validación remota quedó completada en verde mediante GitHub Actions run `30781515656`: el workflow `dotnet-ci`, `build-and-test` y `Outgoing monitor (SQL Server + PostgreSQL)` terminaron en `success`. SQL Server y PostgreSQL fueron validados en el job dedicado, las dos pruebas multi-motor se ejecutaron sin omisiones, los resultados se publicaron y los contenedores se limpiaron. No quedan validaciones remotas pendientes para la Fase 2.

### Referencia canónica de cierre

Commit técnico definitivo: `57c2b929fdc650a899c8511f4dba251862e58b2d`
Mensaje: `fix: integrar pruebas multi-motor del monitor en CI`

El commit `31d740ccc864c244fe8dfc0f211ceacc242e3b3a` contiene la implementación funcional inicial. El commit `57c2b929fdc650a899c8511f4dba251862e58b2d` representa el cierre técnico definitivo de la Fase 2, incluida su integración continua multi-motor.

## 26. Limitaciones

No existe integración de transporte que permita afirmar transmisión o acuse para los fixtures usados. No se reconstruyen históricos desconocidos. Las siete pruebas omitidas por la regresión son condiciones preexistentes del repositorio y no fueron alteradas por esta fase.

## 27. Riesgos

- El volumen histórico puede requerir una medición productiva posterior antes de justificar índices adicionales.
- Catálogos o relaciones históricas incompletas se presentan como no determinadas.
- Las colecciones multi-motor requieren conservar sus indicadores y conexiones locales explícitas en el agente de CI que las ejecute.

## 28. Archivos modificados

Se agregaron contratos y política en `Application/OutgoingTransactionMonitoring`, consulta y auditoría en `Persistence/ACH/OutgoingTransactionMonitoring`, controlador, seed de permisos/navegación, tres archivos de pruebas backend, feature Angular y E2E. Se ajustaron permisos, composición DI, rutas, módulo de transacciones, localización y servicios globales de carga en segundo plano.

No se modificaron golden files, lógica monetaria, migraciones, credenciales ni el directorio protegido de certificados.

## 29. Veredicto

**CERRADA.** La Fase 2 quedó implementada y validada localmente, en SQL Server, en PostgreSQL, mediante Angular, mediante Playwright y mediante GitHub Actions. El monitor conserva `AchTransactions` como raíz canónica, usa evidencia persistida y no mantiene validaciones pendientes dentro del alcance.

## Preparación para la Fase 3

La Fase 3 debe partir del HEAD resultante de este microcierre documental y asumir la Fase 2 como cerrada. No debe reabrir la clasificación persistida, el monitor, la API, la SPA, la línea de tiempo, los permisos, las pruebas multi-motor ni la integración continua de la Fase 2 salvo que aparezca evidencia concreta de una regresión.
