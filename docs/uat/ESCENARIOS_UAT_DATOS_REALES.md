# Escenarios UAT con Datos Reales o Anonimizados - ACH Interbank

Fecha de generacion: 2026-05-18  
Version: 0.1 preliminar  
Rama analizada: `ACH-Interbank-Postgresql`  
Estado: escenarios iniciales; requieren validacion humana y ejecucion formal.  
Politica: no registrar datos sensibles reales, secretos, tokens, llaves privadas, PFX ni cuentas reales en Git.

## Referencias Reales Del Repositorio

- API: `src/Cfa.ACHInterbank.Api/Controllers`.
- Application: `src/Cfa.ACHInterbank.Application`.
- Domain: `src/Cfa.ACHInterbank.Domain`.
- Persistence/DbContext: `src/Cfa.ACHInterbank.Persistence/DataBase/AchDbContext.cs`.
- SPA: `web/ach-interbank-ui/src/app`.
- Normativa: `docs/normativa/md`.
- UAT previo: `docs/uat`.
- Auditorias vigentes: `docs/audits`.

## Formato De Estado

Todos los escenarios inician en estado `PENDIENTE`. Donde una ruta, DTO, tabla o validacion no se identifica con certeza, se marca `NO ENCONTRADO`, `NO CLARO` o `PENDIENTE VALIDAR`.

## Escenarios

### UAT-REAL-001 - Login y autorizacion

- Objetivo: validar autenticacion y sesion UAT.
- Flujo funcional/camara: Auth transversal.
- Precondiciones/datos: usuario UAT sin datos reales; roles definidos.
- Pasos: abrir SPA, iniciar sesion, validar menu y token; cerrar sesion.
- Resultado esperado: acceso permitido solo a rutas autorizadas.
- SPA/backend: componente auth `PENDIENTE VALIDAR`; servicio `AuthService`; endpoint `POST /auth/login`, `POST /auth/refresh`; DTO frontend `AuthResponse`; DTO backend `LoginRequest/AuthResult`; entidades `Users`, `Roles`, `Permissions`, `AuthLogs`.
- Validaciones: frontend requerido usuario/password; backend credenciales, lockout y token.
- Evidencia/criterios/responsable: captura SPA, request/response redactado, `AuthLog`; acepta acceso correcto; rechaza acceso indebido; responsable Tecnologia/Seguridad; estado PENDIENTE.
- Brechas conocidas: roles UAT negocio/operacion/compliance no estan formalizados en matriz final.

### UAT-REAL-002 - Roles y permisos operativos

- Objetivo: validar segregacion de funciones.
- Flujo funcional/camara: Seguridad transversal.
- Precondiciones/datos: usuarios `Admin`, `ACH.Operator` u otros perfiles UAT.
- Pasos: probar rutas protegidas y acciones criticas por perfil.
- Resultado esperado: 401/403 o acceso segun permisos.
- SPA/backend: guards `authGuard`, `roleGuard`, `permissionGuard`; servicio `PermissionsService`; endpoints `GET /api/Permissions`, `GET /api/Roles`, `GET/POST/PUT /api/Users`; DTO frontend `Permission/UserSummary`; DTO backend `RolePermissionDtos/UserDtos`; entidades `Users`, `Roles`, `Permissions`, `RolePermissions`.
- Validaciones: frontend guards; backend `[Authorize]` y politicas `P0Policies/P1Policies`.
- Evidencia/criterios/responsable: capturas, responses 403/200, matriz rol-permiso; responsable Seguridad/Tecnologia; estado PENDIENTE.
- Brechas conocidas: roles UAT por Tesoreria, Auditoria y Compliance NO CLAROS.

### UAT-REAL-003 - Parametrizacion de camaras ACH Colombia / CENIT / STA

- Objetivo: validar camaras y preferencias por institucion.
- Flujo funcional/camara: ACH Colombia, CENIT, STA PENDIENTE VALIDAR.
- Precondiciones/datos: catalogos de camaras e instituciones.
- Pasos: consultar camaras, ciclos y preferencias; validar STA si aplica.
- Resultado esperado: camaras activas y consistentes.
- SPA/backend: componentes `CatalogsListComponent`, `FinancialInstitutionAdminComponent`, `InstitutionClearingHousePreferences`; servicios catalogos; endpoints `/clearing-houses`, `/financial-institutions`, `/institution-clearing-house-preferences`; DTOs `ClearingHouseDtos`, `FinancialInstitutionDto`; entidades `ClearingHouses`, `FinancialInstitutions`, `InstitutionClearingHousePreferences`.
- Validaciones: frontend formularios reactivos parciales; backend servicios de catalogo.
- Evidencia/criterios/responsable: captura catalogos, registros BD anonimizados; responsable Operaciones; estado PENDIENTE.
- Brechas conocidas: STA no evidenciado como flujo completo; marcar PENDIENTE VALIDAR.

### UAT-REAL-004 - Parametrizacion de ciclos

- Objetivo: validar ciclos ACH/CENIT y vigencias.
- Flujo funcional/camara: ACH/CENIT.
- Precondiciones/datos: camara, ciclo, cutoff, vigencia.
- Pasos: consultar, crear/actualizar/inactivar configuracion de ciclo en UAT.
- Resultado esperado: ciclo vigente calculado y auditable.
- SPA/backend: `CycleConfigManagementComponent`, `AchCycleListComponent`; servicios `ClearingHouseCycleConfigsApiService`, `AchCyclesApiService`; endpoints `/clearing-house-cycle-configs`, `/ach-cycles`; DTO frontend `ClearingHouseCycleConfigItem`; DTO backend `ClearingHouseCycleConfigDtos`, `AchCycleDto`; entidades `ClearingHouseCycleConfigs`, `AchCycles`, `AuditLogs`.
- Validaciones: frontend fechas/horas requeridas; backend vigencia/camara/cutoff.
- Evidencia/criterios/responsable: captura, respuesta API, audit log; responsable Operaciones/Tecnologia; estado PENDIENTE.
- Brechas conocidas: equivalencia normativa horarios/cutoff requiere aprobacion Operaciones/Compliance.

### UAT-REAL-005 - Carga o generacion de archivo NACHA-M

- Objetivo: validar carga/generacion NACHA-M sin datos reales sensibles.
- Flujo funcional/camara: ACH/CENIT.
- Precondiciones/datos: archivo NACHA-M anonimo o ciclo exportable.
- Pasos: cargar archivo o exportar por ciclo; registrar hash.
- Resultado esperado: archivo aceptado/generado con trazabilidad.
- SPA/backend: `NachaUploadComponent`, `NachaExportComponent`; servicios `NachaUploadService`, `NachaExportApiService`; endpoints `POST /NachaUpload/upload`, `GET /NachaUpload/records`, `GET /NachaExport/{cycleId}`, `GET /NachaExport/{cycleId}/sobre-digital`; DTOs `NachaDtos`, `NachaUploadRecord`; entidades `NachaHeaders`, `BatchHeaders`, `EntryDetails`, `AddendaRecords`, `FileControls`, `AchFileExports`.
- Validaciones: frontend archivo/tamano; backend parser/generador NACHA.
- Evidencia/criterios/responsable: hash archivo, log, registros BD; responsable Operaciones; estado PENDIENTE.
- Brechas conocidas: validacion externa de formato/naming pendiente.

### UAT-REAL-006 - Validacion registros NACHA-M tipo 1, 5, 6, 7, 8 y 9

- Objetivo: validar estructura y controles de registros NACHA-M.
- Flujo funcional/camara: ACH/CENIT.
- Precondiciones/datos: archivo controlado con registros 1/5/6/7/8/9.
- Pasos: cargar/generar y comparar registros contra matriz normativa.
- Resultado esperado: campos, totales, hash y block count consistentes.
- SPA/backend: pantallas NACHA upload/export/layouts; endpoints `/nacha-layouts`, `/nacha-record-definitions`, `/NachaUpload/*`, `/NachaExport/*`; DTOs `NachaRecordLayoutDto`, `NachaRecordDefinitionDto`; entidades `NachaRecordLayouts`, `NachaRecordDefinitions`, `NachaRecordFields`, `FileControls`, `BatchControls`.
- Validaciones: frontend layout admin; backend validadores `NachaValidator` y builder.
- Evidencia/criterios/responsable: archivo anonimo, comparativo campo a campo, hash; responsable QA/Operaciones; estado PENDIENTE.
- Brechas conocidas: cierre normativo por camara/campo requiere firma.

### UAT-REAL-007 - Envio de transaccion ACH de salida

- Objetivo: validar originacion individual.
- Flujo funcional/camara: ACH/CENIT segun configuracion.
- Precondiciones/datos: cliente, cuenta, banco destino y ciclo anonimos.
- Pasos: crear transaccion, validar policy preview, consultar lista.
- Resultado esperado: transaccion creada con estado/traza consistente.
- SPA/backend: `TransactionCreateComponent`, `TransactionListComponent`; servicio `TransactionsApiService`; endpoints `GET /transactions/policies/preview`, `POST /transactions`, `GET /transactions`; DTO frontend `TransactionDraft`; DTO backend `CreateTransactionDto`; entidades `AchTransactions`, `AchBatches`, `AchTransactionStateEvents`.
- Validaciones: frontend identidad, monto, cuenta, policy preview; backend reglas transaccionales, idempotencia.
- Evidencia/criterios/responsable: captura, response, registro BD; responsable Operaciones; estado PENDIENTE.
- Brechas conocidas: validar mapeo completo de estados contra UAT.

### UAT-REAL-008 - Bulk ingestion de transacciones

- Objetivo: validar carga masiva y tracking.
- Flujo funcional/camara: ACH/CENIT.
- Precondiciones/datos: archivo JSON/CSV/Excel anonimo.
- Pasos: cargar archivo, consultar lote, items, resumen, retry/cancel si aplica.
- Resultado esperado: batch con conteos y estados correctos.
- SPA/backend: `BulkIngestionUploadComponent`, `BulkIngestionTrackingComponent`, `BulkIngestionDetailComponent`; servicios `BulkIngestionTrackingApiService`, `BulkIngestionApiService`; endpoints `/api/transactions/bulk-ingestion/*`, `/transactions/bulk/submit`; DTOs `BulkFileUploadResponse`, `BulkBatchStatusDto`, `BulkIngestionRequest`; entidades `BulkIngestionBatches`, `BulkIngestionItems`, `BulkIngestionAttempts`.
- Validaciones: frontend archivo y estado; backend parser, lifecycle, retry/idempotencia.
- Evidencia/criterios/responsable: archivo anonimo, resumen, attempts; responsable Operaciones/Tecnologia; estado PENDIENTE.
- Brechas conocidas: validar comportamiento con errores parciales.

### UAT-REAL-009 - Devolucion de salida

- Objetivo: generar archivo de devoluciones salientes.
- Flujo funcional/camara: ACH/CENIT.
- Precondiciones/datos: ciclo con transacciones elegibles y causales validas.
- Pasos: consultar elegibles, seleccionar causales, generar archivo.
- Resultado esperado: archivo de devolucion generado y auditado.
- SPA/backend: `AchReturnsManagementComponent`; servicio `AchReturnsApiService`; endpoints `GET /ach-returns/cycles/{cycleId}/transactions`, `POST /ach-returns/generate-file`; DTO frontend `GenerateReturnsFileRequest`; DTO backend `ReturnEligibleTransactionDto`; entidades `AchReturnsGenerated`, `ReturnReasons`, `AchFileExports`.
- Validaciones: frontend seleccion/ciclo; backend elegibilidad y causales.
- Evidencia/criterios/responsable: archivo, hash, audit log; responsable Operaciones; estado PENDIENTE.
- Brechas conocidas: causales por camara requieren cierre firmable.

### UAT-REAL-010 - Devolucion de entrada aplicada E2E

- Objetivo: validar aplicacion inbound de devolucion.
- Flujo funcional/camara: ACH/CENIT.
- Precondiciones/datos: archivo NACHA-M entrante con return correlacionable.
- Pasos: cargar/ingestar, clasificar, vincular transaccion, validar estado.
- Resultado esperado: devolucion aplicada sin duplicidad.
- SPA/backend: `IncomingNachaIngestionsPageComponent`, `IncomingNachaIngestionDetailPageComponent`; servicio `IncomingNachaCommandCenterApiService`; endpoints `/incoming-nacha-command-center/ingestions/*`; DTOs `IncomingNachaIngestionDetail`; entidades `IncomingNachaFileIngestions`, `IncomingNachaTransactionLinks`, `IncomingNachaEntryClassifications`, `AchTransactionStateEvents`.
- Validaciones: frontend detalle/estado; backend parser, classifier, linker, state machine.
- Evidencia/criterios/responsable: timeline, estado final, audit event; responsable Operaciones/QA; estado PENDIENTE.
- Brechas conocidas: UAT E2E con datos reales/anonimos pendiente.

### UAT-REAL-011 - Devolucion de entrada huerfana

- Objetivo: validar tratamiento de devolucion no correlacionada.
- Flujo funcional/camara: ACH/CENIT.
- Precondiciones/datos: archivo entrante con registro no vinculable.
- Pasos: ingestar, revisar command center, validar estado huerfano/no resuelto.
- Resultado esperado: no se aplica indebidamente y queda trazabilidad.
- SPA/backend: command center incoming; endpoints `/incoming-nacha-command-center/ingestions`, `/queue`; DTOs `IncomingNachaQueueListItem`; entidades `IncomingNachaDispatchQueue`, `IncomingNachaProcessingEvents`.
- Validaciones: frontend queue/detalle; backend orphan manual resolution/state machine.
- Evidencia/criterios/responsable: captura, evento, no impacto contable; responsable Operaciones/Auditoria; estado PENDIENTE.
- Brechas conocidas: cierre humano del proceso manual pendiente.

### UAT-REAL-012 - Rechazo total

- Objetivo: validar rechazo total de archivo/lote.
- Flujo funcional/camara: ACH/CENIT/STA PENDIENTE VALIDAR.
- Precondiciones/datos: caso con rechazo total controlado.
- Pasos: procesar archivo/caso, revisar reporte y trazabilidad.
- Resultado esperado: estado `RejectedTotal` o equivalente soportado y evidenciado.
- SPA/backend: reportes `rejections`, command center; endpoints `/api/reports/rejections`, incoming endpoints; DTOs reportes; entidades `AchFileRejectionCodes`, `IncomingNachaProcessingEvents`.
- Validaciones: frontend reporte; backend causa/estado.
- Evidencia/criterios/responsable: reporte, causal, evento; responsable Operaciones/Compliance; estado PENDIENTE.
- Brechas conocidas: mapeo exacto estado/codigo PENDIENTE VALIDAR por camara.

### UAT-REAL-013 - Devolucion parcial

- Objetivo: validar frontera entre devolucion parcial y rechazo parcial por registro.
- Flujo funcional/camara: ACH/CENIT/STA PENDIENTE VALIDAR.
- Precondiciones/datos: caso parcial controlado.
- Pasos: ejecutar caso, revisar estado, causal y reporte.
- Resultado esperado: semantica no se confunde con parcial por monto si no aplica.
- SPA/backend: reportes, returns, command center; endpoints `/api/reports/returns`, `/api/reports/rejections`, `/ach-returns/*`; DTOs reportes/returns; entidades `AchReturnGenerated`, `AchTransactionStateEvents`.
- Validaciones: frontend etiquetas; backend estado/casos.
- Evidencia/criterios/responsable: reporte y acta de interpretacion; responsable Negocio/Compliance; estado PENDIENTE.
- Brechas conocidas: requiere validacion humana de semantica.

### UAT-REAL-014 - ROR / Return of Return

- Objetivo: validar elegibilidad y generacion ROR.
- Flujo funcional/camara: ACH/CENIT.
- Precondiciones/datos: devolucion fuente valida y causal nueva.
- Pasos: evaluar ROR, generar archivo auditoria y NACHA si aplica.
- Resultado esperado: ROR permitido o bloqueado con fallas claras.
- SPA/backend: `AchReturnOfReturnManagementComponent`; servicio `AchReturnsApiService`; endpoints `POST /ach-returns/return-of-return/evaluate`, `/generate-audit-file`, `/generate-nacha-file`; DTO frontend `EvaluateReturnOfReturnRequest`; DTO backend records en `AchReturnOfReturnController`; entidades `ReturnOfReturnFlows`, `AchReturnOfReturnGeneratedFileAudits`.
- Validaciones: frontend campos/cargando; backend elegibilidad, causal, uniqueness.
- Evidencia/criterios/responsable: response, archivo, hash, audit; responsable Operaciones/Compliance; estado PENDIENTE.
- Brechas conocidas: validacion normativa por camara y UAT E2E pendientes.

### UAT-REAL-015 - Causales por camara y flujo

- Objetivo: validar catalogo de causales y politicas.
- Flujo funcional/camara: ACH/CENIT/STA PENDIENTE VALIDAR.
- Precondiciones/datos: causales por camara y flujo.
- Pasos: consultar catalogos, ejecutar casos positivos/negativos.
- Resultado esperado: causal permitida o bloqueada segun politica documentada.
- SPA/backend: `CenitRegulatoryPageComponent`; servicio `CenitRegulatoryApiService`; endpoints `/api/regulatory-catalogs/return-codes`, `/file-rejection-codes`, `/return-policies`, `/return-of-return-policies`; DTOs `CenitReturnCode`; entidades `AchReturnCodes`, `AchFileRejectionCodes`, `AchReturnPolicies`, `AchReturnOfReturnPolicies`.
- Validaciones: frontend consulta; backend service catalog.
- Evidencia/criterios/responsable: tabla causal/camara/flujo, acta; responsable Compliance/Operaciones; estado PENDIENTE.
- Brechas conocidas: firma normativa pendiente.

### UAT-REAL-016 - CENIT ciclos

- Objetivo: validar ciclos CENIT operativos.
- Flujo funcional/camara: CENIT.
- Precondiciones/datos: calendario/cutoff CENIT.
- Pasos: consultar ciclos y cola por ciclo.
- Resultado esperado: encolamiento y horarios consistentes.
- SPA/backend: `CenitOperationPageComponent`, `CycleConfigManagementComponent`; servicios `CenitOperationsApiService`, `ClearingHouseCycleConfigsApiService`; endpoints `/api/cenit/queues`, `/clearing-house-cycle-configs`; entidades `CenitCycleQueues`, `CenitCycleExecutions`, `ClearingHouseCycleConfigs`.
- Validaciones: frontend filtros; backend calendario/policy.
- Evidencia/criterios/responsable: reporte ciclo, captura, acta; responsable Operaciones; estado PENDIENTE.
- Brechas conocidas: evidencia externa/homologada pendiente.

### UAT-REAL-017 - CENIT neteo

- Objetivo: validar neteo por ciclo/participante.
- Flujo funcional/camara: CENIT.
- Precondiciones/datos: lote controlado con debitos/creditos.
- Pasos: ejecutar/consultar net positions y comparar totales.
- Resultado esperado: posiciones netas consistentes y conciliables.
- SPA/backend: `CenitOperationPageComponent`; servicio `CenitOperationsApiService`; endpoint `GET /api/cenit/net-positions`; DTO `CenitNetPositionRow`; entidades `CenitNettingExecutions`, `CenitNetPositions`, `CenitNettingDetails`.
- Validaciones: frontend lectura; backend netting service.
- Evidencia/criterios/responsable: reporte neteo, comparativo, hash; responsable Operaciones/Tesoreria; estado PENDIENTE.
- Brechas conocidas: bloquea productivo sin E2E homologado.

### UAT-REAL-018 - CENIT liquidez

- Objetivo: validar decisiones de liquidez/optimizacion.
- Flujo funcional/camara: CENIT.
- Precondiciones/datos: transacciones con restricciones de liquidez.
- Pasos: consultar decisiones de optimizacion y trazabilidad.
- Resultado esperado: decision explicable y auditada.
- SPA/backend: `CenitOperationPageComponent`; servicio `CenitOperationsApiService`; endpoints `/api/cenit/optimization-decisions`, `/api/cenit/traceability`; entidades `LiquidityOptimizationDecisions`, `CenitCycleExecutions`.
- Validaciones: frontend lectura; backend policy/resolver.
- Evidencia/criterios/responsable: decision, razon, responsable; responsable Tesoreria/Operaciones; estado PENDIENTE.
- Brechas conocidas: liquidez simulada no equivale a CUD real.

### UAT-REAL-019 - CENIT CUD

- Objetivo: validar evidencia CUD como frontera operacional.
- Flujo funcional/camara: CENIT/CUD.
- Precondiciones/datos: soporte CUD real controlado o evidencia operacional aprobada.
- Pasos: registrar referencia segura, comparar con neteo/liquidez, firmar evidencia.
- Resultado esperado: soporte CUD trazable o brecha documentada.
- SPA/backend: no se encontro API CUD directa; reportes/operacion CENIT como soporte; endpoint CUD `NO ENCONTRADO`; DTO `NO ENCONTRADO`; entidades CUD `NO ENCONTRADO`.
- Validaciones: frontend `NO ENCONTRADO`; backend `NO ENCONTRADO`; validacion humana obligatoria.
- Evidencia/criterios/responsable: soporte externo seguro, hash, firma Tesoreria/Riesgo; estado PENDIENTE.
- Brechas conocidas: bloqueante productivo si aplica al alcance.

### UAT-REAL-020 - Conciliacion

- Objetivo: validar conciliacion operativa.
- Flujo funcional/camara: ACH/CENIT.
- Precondiciones/datos: transacciones, archivos, devoluciones y reportes.
- Pasos: consultar reporte conciliacion y comparar totales.
- Resultado esperado: diferencias identificadas y explicadas.
- SPA/backend: `ReconciliationReportComponent`; servicio `ReportsApiService`; endpoint `GET /api/reports/reconciliation`, `GET /api/reports/reconciliation/pdf`; DTO `ReconciliationReportResponse`; entidades reportes/ACH.
- Validaciones: frontend filtros; backend report services.
- Evidencia/criterios/responsable: reporte, PDF, comparativo; responsable Operaciones/Auditoria; estado PENDIENTE.
- Brechas conocidas: no equivale a contabilidad definitiva.

### UAT-REAL-021 - Contabilidad

- Objetivo: validar frontera no-contable.
- Flujo funcional/camara: Transversal.
- Precondiciones/datos: reportes de revision contable operativa.
- Pasos: exportar accounting-review y confirmar que no genera asiento.
- Resultado esperado: soporte operativo, no ledger/journal/posting.
- SPA/backend: `AccountingReviewExportComponent`; servicio `ReportsApiService`; endpoint `POST /api/reports/accounting-review/export`; DTO `AccountingReviewExportRequest`; entidades contables definitivas `NO ENCONTRADO`.
- Validaciones: frontend filtros/export; backend export service.
- Evidencia/criterios/responsable: export PDF/CSV/XLSX, acta frontera no-contable; responsable Negocio/Auditoria; estado PENDIENTE.
- Brechas conocidas: contabilidad online NO ENCONTRADA.

### UAT-REAL-022 - Sobre digital

- Objetivo: validar cifrado/descifrado y fail-close.
- Flujo funcional/camara: ACH/CENIT.
- Precondiciones/datos: archivo anonimo y certificados UAT.
- Pasos: generar cifrado/manual encrypt/decrypt, validar audit.
- Resultado esperado: operacion exitosa o falla cerrada sin plano.
- SPA/backend: `DigitalEnvelopeToolComponent`, `ManualEncryptOperationComponent`, `ManualDecryptOperationComponent`; servicios `NachaSecurityOperationsApiService`, `SobreDigitalService`; endpoints `/nacha-security/operations/envelope/manual-encrypt`, `/manual-decrypt`, `/SobreDigital/encrypt`, `/SobreDigital/decrypt`; entidades `DigitalEnvelopeOperationLogs`, `NachaSecurityOperations`.
- Validaciones: frontend archivo/campos; backend policy/signature validation.
- Evidencia/criterios/responsable: evidencia cifrado, hash, audit; responsable Seguridad; estado PENDIENTE.
- Brechas conocidas: validacion externa oficial pendiente.

### UAT-REAL-023 - Firma

- Objetivo: validar firma y verificacion.
- Flujo funcional/camara: ACH/CENIT.
- Precondiciones/datos: certificados UAT y archivo anonimo.
- Pasos: generar sobre/archivo firmado, validar resultado.
- Resultado esperado: firma valida o rechazo fail-close.
- SPA/backend: componentes NACHA security; endpoints `/nacha-security/operations/nacha/generate-encrypted`, auditoria; DTO `NachaSecurityOperationResponse`; entidades `DigitalEnvelopeOperationLogs`, `CertificateUsageLogs`.
- Validaciones: frontend proceso; backend `DigitalEnvelopeSignatureValidator`.
- Evidencia/criterios/responsable: resultado firma, auditoria, hash; responsable Seguridad; estado PENDIENTE.
- Brechas conocidas: vector oficial/interoperabilidad pendiente.

### UAT-REAL-024 - Certificados

- Objetivo: validar carga, activacion, revocacion y auditoria.
- Flujo funcional/camara: Transversal.
- Precondiciones/datos: certificados publicos UAT; privados fuera de Git.
- Pasos: cargar/listar/versionar/activar/revocar/validar.
- Resultado esperado: estado certificado auditable y seguro.
- SPA/backend: `NachaCertificateManagerComponent`, `CertificateVersionsComponent`; servicio `CertificateManagementApiService`; endpoints `/nacha-security/certificates/management/*`, `/nacha-security/certificates/*`; entidades `DigitalCertificates`, `DigitalCertificateVersions`, `CertificateLoadAudits`.
- Validaciones: frontend formularios; backend certificate services.
- Evidencia/criterios/responsable: audit log, estado version, secretRef enmascarado; responsable Seguridad; estado PENDIENTE.
- Brechas conocidas: no incluir material privado en Git.

### UAT-REAL-025 - OpenBao / secretos

- Objetivo: validar proveedor de secretos si aplica.
- Flujo funcional/camara: Seguridad transversal.
- Precondiciones/datos: OpenBao UAT o excepcion aprobada.
- Pasos: validar configuracion, lectura de secretRef enmascarado y operacion sin exponer token.
- Resultado esperado: secretos resueltos sin datos sensibles en logs/Git.
- SPA/backend: no aplica SPA; backend `OpenBaoCertificateSecretProvider`, scripts `scripts/openbao`; compose principal no incluye OpenBao; endpoints directos `NO ENCONTRADO`.
- Validaciones: backend provider/config; frontend NO APLICA.
- Evidencia/criterios/responsable: captura de estado redactada, hash de evidencia; responsable Seguridad/Operaciones; estado PENDIENTE.
- Brechas conocidas: OpenBao no levantado en `docker-compose.yml` principal.

### UAT-REAL-026 - Auditoria y trazabilidad

- Objetivo: validar audit logs, auth logs, navigation logs y trazabilidad ACH.
- Flujo funcional/camara: Transversal.
- Precondiciones/datos: operaciones UAT ejecutadas.
- Pasos: consultar logs y trazabilidad transaccion/reporte.
- Resultado esperado: usuario, accion, entidad, fecha y estado trazables.
- SPA/backend: `AuditLogComponent`, `AuthLogComponent`, `NavigationLogComponent`, reportes traceability; endpoints `/api/audit-logs`, `/api/auth-logs`, `/api/navigation-logs`, `/api/ach-traceability/*`; entidades `AuditLogs`, `AuthLogs`, `NavigationLogs`, `AchTransactionStateEvents`.
- Validaciones: frontend filtros; backend query services.
- Evidencia/criterios/responsable: logs redactados, timeline; responsable Auditoria; estado PENDIENTE.
- Brechas conocidas: cobertura de auditoria por todos los endpoints criticos PENDIENTE VALIDAR.

### UAT-REAL-027 - Idempotencia

- Objetivo: validar no duplicidad en reproceso.
- Flujo funcional/camara: ACH/CENIT.
- Precondiciones/datos: request/archivo repetible controlado.
- Pasos: ejecutar dos veces y comparar resultado.
- Resultado esperado: duplicidad bloqueada o misma respuesta controlada.
- SPA/backend: transacciones, bulk ingestion, incoming command center; endpoints `/transactions`, `/api/transactions/bulk-ingestion/*`, `/incoming-nacha-command-center/*`; entidades `AchTransactions`, `BulkIngestionBatches`, `IncomingNachaFileIngestions`.
- Validaciones: frontend no doble click/cargando; backend keys/indices/idempotency hash.
- Evidencia/criterios/responsable: respuestas, registros, unique indexes; responsable Tecnologia/QA; estado PENDIENTE.
- Brechas conocidas: evidencia multiinstancia debe cerrarse en UAT.

### UAT-REAL-028 - Reportes

- Objetivo: validar reportes operativos y exportaciones.
- Flujo funcional/camara: ACH/CENIT.
- Precondiciones/datos: dataset UAT con transacciones, returns, cycles.
- Pasos: consultar reportes y PDFs.
- Resultado esperado: filtros, conteos y totales consistentes.
- SPA/backend: `ReportsHomeComponent`, `ReportListPageComponent`, `TraceabilityReportComponent`, `ReconciliationReportComponent`; servicio `ReportsApiService`; endpoints `/api/reports/transactions/sent`, `/received`, `/returns`, `/rejections`, `/nacha-files`, `/cycles`, `/audit`, `/history`, PDFs; DTOs report models.
- Validaciones: frontend filtros; backend report services.
- Evidencia/criterios/responsable: PDFs, capturas, comparativos; responsable Operaciones/Auditoria; estado PENDIENTE.
- Brechas conocidas: validacion humana de reportes pendiente.

### UAT-REAL-029 - Manejo de errores

- Objetivo: validar errores controlados.
- Flujo funcional/camara: Transversal.
- Precondiciones/datos: requests invalidos controlados.
- Pasos: generar 400/401/403/404/409/500 controlado sin datos sensibles.
- Resultado esperado: mensajes seguros, sin stack trace sensible.
- SPA/backend: interceptors `ErrorInterceptor`, `AuthInterceptor`; backend `GlobalExceptionMiddleware`, controllers; endpoints representativos.
- Validaciones: frontend notificaciones; backend middleware/ProblemDetails.
- Evidencia/criterios/responsable: capturas y responses redactados; responsable Tecnologia/Seguridad; estado PENDIENTE.
- Brechas conocidas: revisar logs con datos sensibles.

### UAT-REAL-030 - Reproceso

- Objetivo: validar retry/requeue/reprocess operacional.
- Flujo funcional/camara: ACH/CENIT.
- Precondiciones/datos: lote/queue en estado reintentable.
- Pasos: retry bulk, retry/unblock/requeue incoming, validar auditoria.
- Resultado esperado: reproceso controlado e idempotente.
- SPA/backend: `BulkIngestionDetailComponent`, `IncomingNachaQueueDetailPageComponent`; servicios tracking/command center; endpoints `/api/transactions/bulk-ingestion/{batchId}/retry`, `/incoming-nacha-command-center/queue/{queueId}/retry|unblock|requeue|mark-failed-final`; entidades `BulkIngestionAttempts`, `IncomingNachaDispatchQueue`.
- Validaciones: frontend acciones disabled/loading; backend state machine.
- Evidencia/criterios/responsable: attempt, evento, audit; responsable Operaciones/Tecnologia; estado PENDIENTE.
- Brechas conocidas: procedimientos de rollback/reproceso deben firmarse.

### UAT-REAL-031 - Cierre operativo

- Objetivo: validar cierre diario/UAT y responsabilidades.
- Flujo funcional/camara: Transversal.
- Precondiciones/datos: escenarios ejecutados.
- Pasos: revisar reportes, conciliacion, defectos, evidencias y acta.
- Resultado esperado: cierre con decision y pendientes claros.
- SPA/backend: reportes y docs; endpoint `NO APLICA` para acta; DTO `NO APLICA`.
- Validaciones: humanas/documentales.
- Evidencia/criterios/responsable: acta, indice, matriz defectos; responsable Operaciones/Auditoria; estado PENDIENTE.
- Brechas conocidas: no existe modulo UAT integral en SPA.

### UAT-REAL-032 - Health checks

- Objetivo: validar salud basica del ambiente.
- Flujo funcional/camara: Tecnico transversal.
- Precondiciones/datos: API levantada.
- Pasos: consultar live/ready antes y despues de pruebas.
- Resultado esperado: live OK y ready OK con DB.
- SPA/backend: no aplica SPA; endpoints `GET /health/live`, `GET /health/ready`; DTO anonimo; entidades DB solo `CanConnectAsync`.
- Validaciones: backend ready DB; frontend NO APLICA.
- Evidencia/criterios/responsable: salida curl/Postman, timestamp; responsable Tecnologia; estado PENDIENTE.
- Brechas conocidas: no cubre Quartz/OpenBao/externos.

### UAT-REAL-033 - Validacion SPA contra API

- Objetivo: validar que pantallas consumen endpoints reales.
- Flujo funcional/camara: Transversal.
- Precondiciones/datos: SPA/API UAT levantadas.
- Pasos: recorrer rutas SPA y verificar requests/responses.
- Resultado esperado: sin 404 por endpoint inexistente ni DTO incompatible.
- SPA/backend: rutas en `app-routing.module.ts`, servicios Angular, controllers API; endpoints multiples.
- Validaciones: frontend interceptors y render; backend contratos.
- Evidencia/criterios/responsable: capturas network, matriz SPA-backend; responsable QA/Tecnologia; estado PENDIENTE.
- Brechas conocidas: endpoints de interoperabilidad SPA no encontrados en backend.

### UAT-REAL-034 - Validacion de configuracion productiva no-localhost

- Objetivo: validar que build productivo no dependa de localhost.
- Flujo funcional/camara: Tecnico transversal.
- Precondiciones/datos: configuracion de ambiente productivo/UAT.
- Pasos: revisar `environment.prod.ts` y mecanismo de configuracion.
- Resultado esperado: URL API parametrizada o apuntando a host UAT/productivo aprobado.
- SPA/backend: `web/ach-interbank-ui/src/environments/environment.prod.ts`; endpoint `NO APLICA`; DTO `NO APLICA`.
- Validaciones: frontend build/config; backend NO APLICA.
- Evidencia/criterios/responsable: revision de archivo, decision de configuracion; responsable Tecnologia/Operaciones; estado PENDIENTE.
- Brechas conocidas: actualmente apunta a `localhost`.

### UAT-REAL-035 - Validacion de endpoints SPA sin backend equivalente

- Objetivo: identificar pantallas/servicios que consumen rutas no implementadas.
- Flujo funcional/camara: Transversal.
- Precondiciones/datos: inventario SPA/API.
- Pasos: cruzar servicios Angular contra controllers.
- Resultado esperado: cada consumo tiene backend o queda documentado como NO APLICA.
- SPA/backend: `InteroperabilityApiService` consume `nacha-security/interoperability/status`, `run-harness`, `reports/{id}`; backend equivalente NO ENCONTRADO.
- Validaciones: frontend debe manejar 404 o feature flag; backend PENDIENTE VALIDAR.
- Evidencia/criterios/responsable: matriz de brecha, decision funcional; responsable Tecnologia/Seguridad; estado PENDIENTE.
- Brechas conocidas: desalineacion SPA/backend de interoperabilidad.

