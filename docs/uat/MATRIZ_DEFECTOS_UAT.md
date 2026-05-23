# Matriz de Defectos UAT - ACH Interbank

Fecha de generacion/revalidacion: 2026-05-18 / 2026-05-19
Version: 0.8
Rama analizada: `fix/uat-operator-role-seed`
Estado: matriz actualizada tras cierre controlado de DEF-UAT-015.

## Severidades

- Bloqueante: impide UAT formal o productivo.
- Alta: afecta flujo critico o seguridad, requiere cierre antes de productivo.
- Media: requiere correccion o aceptacion formal.
- Baja: mejora o ajuste documental.

## Estados

- Abierto.
- En analisis.
- Corregido.
- Rechazado.
- Diferido.
- Aceptado como riesgo.
- Cerrado documentalmente.
- Cerrado.
- Abierto por seed/seguridad.

## Matriz

| ID defecto | Escenario | Severidad | Descripcion | Componente | Evidencia | Responsable | Estado | Fecha apertura | Fecha cierre | Decision |
|---|---|---|---|---|---|---|---|---|---|---|
| DEF-UAT-001 | UAT-REAL-033 / UAT-REAL-002 | Alta | `AchResponsesController` no evidenciaba `[Authorize]` explicito ni politicas por accion en el pre-check. | Backend API | `src/Cfa.ACHInterbank.Api/Controllers/AchResponsesController.cs`, `tests/Cfa.ACHInterbank.Tests/AchResponsesControllerTests.cs` | Tecnologia/Seguridad | Corregido | 2026-05-18 | 2026-05-18 | `[Authorize]` general aplicado; validar policy granular. |
| DEF-UAT-002 | UAT-REAL-034 | Alta | Configuracion productiva SPA apuntaba a `localhost`, no apta para despliegue productivo. | Angular config | `web/ach-interbank-ui/src/environments/environment.prod.ts`, `tests/Cfa.ACHInterbank.Tests/AngularSpaUatReadinessCharacterizationTests.cs` | Tecnologia/Operaciones | Corregido | 2026-05-18 | 2026-05-18 | Base relativa aplicada; proxy Docker validado. |
| DEF-UAT-003 | UAT-REAL-035 | Alta | SPA consume endpoints `nacha-security/interoperability/*` y no se encontro controller backend equivalente. | SPA/API | `web/ach-interbank-ui/src/app/features/nacha-security/services/interoperability-api.service.ts` | Tecnologia | En analisis | 2026-05-18 | PENDIENTE | Requiere decision backend/feature flag/NO APLICA. |
| DEF-UAT-004 | UAT-REAL-031 | Media | README raiz conservaba plantilla generica y drift operativo. | Documentacion | `README.md` | Tecnologia | Corregido | 2026-05-18 | 2026-05-18 | README saneado y enlaza docs UAT/go-live. |
| DEF-UAT-005 | UAT-REAL-030 | Alta | Existe `.env` versionado; requiere revision para asegurar que no contenga secretos reales. | Configuracion | `.env` | Seguridad/Operaciones | Abierto | 2026-05-18 | PENDIENTE | Revisar y sanear si aplica. |
| DEF-UAT-006 | UAT-REAL-030 | Alta | `docker-compose.yml` contenia defaults sensibles o credenciales de ejemplo parametrizadas. | Docker/config | `docker-compose.yml`, `.env.example`, `.env.test.example` | Seguridad/Operaciones | Corregido | 2026-05-18 | 2026-05-18 | Defaults reemplazados por placeholders; validar UAT/preprod. |
| DEF-UAT-007 | UAT-REAL-025 | Media | OpenBao no esta incluido en `docker-compose.yml` principal aunque existen scripts/configuracion. | Secretos/OpenBao | `docker-compose.yml`, `scripts/openbao` | Seguridad/Operaciones | Abierto | 2026-05-18 | PENDIENTE | Definir compose UAT o excepcion formal. |
| DEF-UAT-008 | UAT-REAL-028 | Media | `DocumentationFile` del API csproj usaba ruta absoluta. | Build/config | `src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj`, `tests/Cfa.ACHInterbank.Tests/ProjectConfigurationPortabilityTests.cs` | Tecnologia | Corregido | 2026-05-18 | 2026-05-18 | Ruta relativa MSBuild aplicada; validar build. |
| DEF-UAT-009 | UAT-REAL-032 | Media | Health checks actuales cubren live/ready DB, pero no evidencian Quartz/OpenBao/externos. | Observabilidad | `src/Cfa.ACHInterbank.Api/DependencyInjectionService.cs` | Tecnologia/Operaciones | Abierto | 2026-05-18 | PENDIENTE | Ampliar controles o documentar alcance. |
| DEF-UAT-010 | UAT-REAL-019 | Bloqueante | Evidencia CUD operacional no encontrada; si CUD aplica al alcance, bloquea productivo. | CENIT/CUD | `docs/uat/cenit-cycles-liquidity-cud-acceptance-checklist.md` | Operaciones/Tesoreria | Abierto | 2026-05-18 | PENDIENTE | Ejecutar E2E homologado y adjuntar evidencia. |
| DEF-UAT-011 | UAT-REAL-028 | Alta | Suite backend completa no quedaba verde por falla `AchPreproductionCertificationTests.BatchResolver_RejectsTransactionsWhenResolvedCycleIsClosed`. | Tests backend | `tests/Cfa.ACHInterbank.Tests/AchPreproductionCertificationTests.cs`; `dotnet test` 1091 OK, 1 omitida, 0 fallas | Tecnologia/QA | Cerrado | 2026-05-18 | 2026-05-19 | Causa: fixture no deterministico por timezone/local date. Se ajusto solo el test para que el ciclo este cerrado respecto al `FixedTimeProvider`; no se cambiaron reglas ACH/CENIT. |
| DEF-UAT-012 | UAT-REAL-033 | Alta | Suite Angular completa no quedaba verde en ejecuciones previas. | Tests Angular | `npm run build` OK; `npm test -- --watch=false --browsers=ChromeHeadless` 147 specs OK | Tecnologia/QA | Cerrado | 2026-05-18 | 2026-05-19 | Angular local queda verde; persisten warnings no bloqueantes de testbed/template y Browserslist. |
| DEF-UAT-013 | UAT-TECH-005 | Alta | Variables `ACH_UAT_DEMO_USERNAME` y `ACH_UAT_DEMO_PASSWORD` no estaban disponibles en ejecuciones previas; bloqueaban login real controlado. | Ambiente/UAT tecnico | `docs/uat/EJECUCION_UAT_TECNICO_BASICO.md` | Operaciones/QA/DevOps | Cerrado | 2026-05-18 | 2026-05-18 | Variables presentes en este reintento; login demo 200 ejecutado sin imprimir secretos. |
| DEF-UAT-014 | UAT-TECH-009 | Media | Browser integrado no pudo aportar evidencia visual automatizada confiable en esta sesion. | Browser/SPA | `docs/uat/EVIDENCIAS_UAT_TECNICO_BASICO.md` | QA/DevOps | Abierto | 2026-05-18 | PENDIENTE | Mantener como limitacion de herramienta; validacion HTTP con token y logs pasa. |
| DEF-UAT-015 | UAT-TECH-006 | Media | Rol esperado `ACH.Operator` no aparecia en respuesta de login ni JWT para el usuario demo `admin`. | Identidad/Autorizacion | `UserRoleConfiguration` ahora asigna `admin` a `Admin` y `ACH.Operator`; migracion `AddAdminOperatorRoleSeed`; login sanitizado por `http://localhost:743/auth/login` devuelve roles respuesta/JWT = `Admin, ACH.Operator`; `/navigation/menu`, `/api/roles`, `/api/users`, `/api/ach/responses` responden 200 con Bearer. | Seguridad/Tecnologia | Cerrado | 2026-05-18 | 2026-05-19 | Opcion A aprobada para UAT controlado: `admin` queda como usuario demo multirol. No se debilito autenticacion/autorizacion ni se tocaron reglas ACH/NACHA-M/CENIT/ROR. |
| DEF-UAT-016 | UAT-FUNC-001 / UAT-FUNC-004 | Bloqueante | La SPA Docker en `http://localhost:743` devolvia `index.html` para rutas funcionales raiz requeridas por pantallas ACH, incluyendo `/financial-institutions`, `/ach-cycles`, `/clearing-houses` y `/transactions/company-entry-descriptions`. | SPA/Nginx/proxy runtime | `web/ach-interbank-ui/nginx.conf`, `docs/uat/UAT_FUNCIONAL_SINTETICO.md`, `docs/uat/EVIDENCIAS_UAT_FUNCIONAL.md`; reintento `:743` sin token 401 no HTML y con token 200 JSON | Tecnologia/DevOps | Cerrado | 2026-05-18 | 2026-05-18 | Locations Nginx agregados y validados; `/transactions`, `/transactions/1`, `/transactions/policies/preview` y duplicado `POST /transactions` tambien responden desde API sin `index.html`. |
| DEF-UAT-017 | UAT-FUNC-006 | Alta | La creacion de la transaccion sintetica historica `UAT-SINT-001` no genero evento inicial; la correccion fue revalidada en runtime con nueva transaccion sintetica. | Backend trazabilidad/auditoria | `TransactionPersister.cs`, `AchTransactionNachaTests.cs`, `docs/uat/EVIDENCIAS_UAT_FUNCIONAL.md`; `UAT-SINT-TRACE-001` ID `2` con evento `Pending -> Pending`, `System`, `CREATED`; duplicado deja `transaction_count=1`, `event_count=1` | Tecnologia/QA | Cerrado | 2026-05-18 | 2026-05-19 | Cerrado funcionalmente para nuevas transacciones. No se hizo backfill ni migracion; `UAT-SINT-001` historica conserva 0 eventos. |
| DEF-UAT-018 | UAT-FUNC-005 | Media | La idempotencia/deduplicacion funciona de forma controlada para payload duplicado; se formaliza el contrato actual sin cambiar HTTP 400 ni implementar `Idempotency-Key`. | Backend transacciones/API contract | `docs/go-live-readiness/CONTRATO_IDEMPOTENCIA_TRANSACCIONES.md`, `TransactionPolicyService.cs`, `TransactionPolicyServiceTests.cs`, `TransactionsControllerTests.cs`, `docs/uat/EVIDENCIAS_UAT_FUNCIONAL.md` | Tecnologia/Arquitectura | Cerrado documentalmente | 2026-05-18 | 2026-05-19 | Contrato actual observado: deduplicacion previa a persistencia por ciclo, tipo, monto, cuentas y `TransactionExternalId`/`Reference`; duplicado retorna 400 JSON controlado y no duplica transaccion/evento. 409/Idempotency-Key/replay quedan como decision evolutiva. |
| DEF-UAT-019 | UAT-FUNC-002 | Media | Catalogo/configuracion NACHA-M no quedaba validado por endpoint esperado incorrecto y proxy SPA sin rutas NACHA. | Backend catalogos NACHA-M / SPA Nginx | `docs/go-live-readiness/MATRIZ_NACHA_M_LAYOUTS.md`; `/nacha-layouts` y `/nacha-record-definitions` responden 200 JSON con token por `:843` y `:743`; sin token 401 no HTML | Tecnologia/Compliance | Cerrado | 2026-05-18 | 2026-05-19 | Ruta real confirmada: `/nacha-layouts`; `/nacha-record-layouts` devuelve 404 controlado y no aplica como endpoint real. Proxy Nginx corregido. |
| DEF-UAT-020 | UAT-REAL-006 | Alta/Media | Validacion NACHA-M campo-a-campo y homologacion externa de registros 1/5/6/7/8/9 sigue parcial. En UAT integrado se crearon transacciones ACH Colombia/CENIT, pero no se obtuvo archivo NACHA-M valido: ahora `/NachaExport` responde 422 controlado por prenotificacion previa ausente. | NACHA-M layouts/normativa | `docs/uat/UAT_NACHA_M_CAMPO_A_CAMPO.md`, `docs/uat/EVIDENCIAS_NACHA_M_UAT.md`, `docs/go-live-readiness/MATRIZ_NACHA_M_ACH_COLOMBIA.md`, `docs/go-live-readiness/MATRIZ_NACHA_M_CENIT.md` | Compliance/Operaciones/Tecnologia | Abierto | 2026-05-19 | PENDIENTE | Crear prenotificaciones UAT validas sin bypass/backdating, esperar/usar ciclo valido, generar archivo UAT no vacio por sistema, validar hash/totales/block count y obtener firma o waiver. |
| DEF-UAT-021 | UAT-NACHA-EXPORT | Alta | `GET /NachaExport/{cycleId}` ya no responde `HTTP 200` con `Content-Length: 0`; retorna `HTTP 422` JSON controlado con `NACHA_EXPORT_PREREQUISITE_FAILED` cuando faltan prerequisitos de exportacion. | NACHA Export API | `docs/uat/evidencias/nacha-m-uat/*/attempt_4_controlled_422_response.txt`; tests `NachaExportControllerTests` | Tecnologia/QA | Cerrado tecnico | 2026-05-19 | CERRADO | Mantener pendiente solo la generacion de archivo no vacio cuando existan transacciones exportables con prenotificacion valida. |
| DEF-UAT-022 | UAT-SOAP-001 | Alta | Job automatico `Proc_Contrapartidas` queda protegido por `ProcContrapartidas:Mode=DryRun` en UAT/local; evidencia runtime `PROC_DRY_RUN` confirma payload generado y no transmitido. | Integracion SOAP | `docs/uat/EVIDENCIAS_SOAP_PROC_CONTRAPARTIDAS.md`, `docs/uat/evidencias/soap-proc-contrapartidas/runtime_dry_run_validation.md`, tests `ContrapartidaDispatchJobServiceTests` | Integracion/DevOps/Seguridad | Cerrado tecnico UAT/local | 2026-05-19 | CERRADO | Pendiente homologar endpoint UAT/mock autorizado para integracion externa real; no usar `Live` sin aprobacion. |
| DEF-UAT-SOAP-MAP-001 | UAT-SOAP-MAP | Media | `Proc_Contrapartidas` podia generar payload con fallback transicional si no existia `IntegrationMappingSet` publicado. | Integracion SOAP / mappings | `docs/uat/EVIDENCIAS_TRANSACTION_INTEGRATION_READINESS.md`; tests `ProcContrapartidasFallbackClosureTests`, `ContrapartidaDispatchJobServiceTests` | Arquitectura/Integracion | Cerrado tecnico | 2026-05-21 | 2026-05-21 | Cerrado: sin mapping requerido activo se falla antes de contrato/XML/DryRun/dispatch. `UsedFallback=true` se bloquea con `REQUIRED_MAPPING_USES_FALLBACK`. |
| DEF-UAT-SOAP-MAP-002 | UAT-SOAP-MAP | Alta | `Proc_Transacciones` requeria guardrail DryRun/Disabled especifico equivalente a `Proc_Contrapartidas`. | Integracion SOAP / UAT guardrail | `docs/uat/EVIDENCIAS_SOAP_PROC_TRANSACCIONES.md`; tests `IncomingNachaPostProcessingOrchestratorTests` | Arquitectura/Integracion/Seguridad | Cerrado tecnico | 2026-05-21 | 2026-05-21 | Cerrado: `ProcTransacciones:Mode=DryRun/Disabled` no transmite, valida readiness antes de payload y bloquea missing mapping/fallback requerido. |
| DEF-UAT-SOAP-MAP-003 | UAT-SOAP-MAP | Media | `RegistrarRespuestaTransaccion` requeria consumo de mapping y trace campo-a-campo persistido sin volverse monetario. | Integracion SOAP / respuestas | `docs/uat/EVIDENCIAS_SOAP_REGISTRAR_RESPUESTA_TRANSACCION.md`; tests `IntegrationMappingTraceWriterTests`, `NotificarRespuestaAchUseCaseTests` | Arquitectura/Integracion | Cerrado tecnico | 2026-05-21 | 2026-05-21 | Cerrado: readiness + `IntegrationMappingTraceWriter`; missing mapping bloquea gateway; `MonetaryMovementCreated=false`. |
| OBS-UAT-001 | UAT-TECH-011 | Baja | Logs PostgreSQL muestran FATAL previos por usuarios inexistentes `root`/`sa`. | PostgreSQL/Operacion | `docker compose logs postgres --tail=120` | Operaciones/DevOps | Abierto | 2026-05-18 | PENDIENTE | Revisar origen de probes/conexiones; no bloqueo del UAT tecnico basico. |

## Actualizacion 2026-05-21 - garantia readiness SOAP

Se implemento garantia automatizada `Transaction -> ExpectedIntegrationOperation -> IntegrationMappingReadiness`:

- Debito CFA resuelve `WSCFAACH / Proc_Contrapartidas / MonetaryDebitRequest`.
- Credito externo resuelve `WSCFAACH / Proc_Transacciones / MonetaryCreditRequest`.
- Respuesta diferencial resuelve `WSAXON / RegistrarRespuestaTransaccion / DifferentialResponseNotification` con `movesMoney=false`.
- `GET /Transactions/{id}/integration-readiness` expone la garantia sin mutar estado ni invocar SOAP.
- `Proc_Contrapartidas`, `Proc_Transacciones` y `RegistrarRespuestaTransaccion` validan readiness antes de XML/payload/gateway cuando los servicios estan registrados.
- Missing mapping falla controlado; fallback requerido queda `Failed` y no `Ok`.

Impacto sobre defectos:

- `DEF-UAT-SOAP-MAP-001`: cerrado tecnico; fallback transicional de campos requeridos ya no construye XML ni DryRun exitoso.
- `DEF-UAT-SOAP-MAP-002`: cerrado tecnico; `Proc_Transacciones` cuenta con guardrail DryRun/Disabled especifico y tests negativos.
- `DEF-UAT-SOAP-MAP-003`: cerrado tecnico; `RegistrarRespuestaTransaccion` persiste trace parametrizado campo-a-campo y mantiene naturaleza no monetaria.

Evidencia: `docs/uat/EVIDENCIAS_TRANSACTION_INTEGRATION_READINESS.md`.

## Actualizacion SPA mappings - 2026-05-23

- `/integraciones/mappings` queda alineada contra catalogo backend de `IntegrationMappingSet`, parametros destino y fuentes controladas.
- `WSCFAACH / Proc_Transacciones / MonetaryCreditRequest / OutboundRequest` visible y seleccionable en SPA.
- `WSAXON / RegistrarRespuestaTransaccion / DifferentialResponseNotification / InboundResponse` visible y seleccionable en SPA.
- Fuentes NACHA-M desagregadas visibles: `NachaHeaders`, `BatchHeaders`, `EntryDetails`, `AddendaRecords`, `BatchControls`, `FileControls`.
- El editor deriva `sourceFieldPath` desde catalogo controlado; no se habilita SQL libre ni seleccion arbitraria de tablas.
- Evidencia visual/DOM: `docs/ux/evidencias/integration-mappings-ux-validation.json`.

## Actualizacion DEF-UAT-020 - 2026-05-19

Se implemento parametrizacion tecnica de reglas de prenotificacion por camara/naturaleza/tipo:

- `ClearingHouseTransactionRule` + migracion EF `AddClearingHouseTransactionRules`.
- Seeds iniciales ACH Colombia y CENIT basados en MAN-004 V32 y CENIT DSP-152 Anexo 2.
- Endpoint CRUD `/api/clearing-house-transaction-rules`.
- Endpoint preview `/api/transaction-prerequisite-policy/preview`.
- Pantalla SPA `/transactions/clearing-house-rules`.

DEF-UAT-020 permanece **Abierto/Parcial**: la parametrizacion es prerequisito para reintento, pero falta crear prenotificaciones UAT validas y generar archivos NACHA-M no vacios por ACH Colombia y CENIT.

## Actualizacion DEF-UAT-020 - 2026-05-20

Se reintento en runtime Docker con reglas por camara/naturaleza aplicadas:

- Menu dinamico incluye `/transactions/clearing-house-rules` para usuario demo `admin` con roles `Admin, ACH.Operator`.
- `ClearingHouseTransactionRules`: 4 reglas activas en runtime.
- CFA/Cooperativa Financiera de Antioquia: unica institucion `IsDefaultSource=true`.
- Prenotificaciones UAT debito creadas por API:
  - ACH Colombia: `UAT-ACH-PRE-001`, TransactionId 246, codigo NACHA `28`.
  - CENIT: `UAT-CEN-PRE-001`, TransactionId 247, codigo NACHA `28`.
- Transacciones credito opcionales creadas por API:
  - ACH Colombia: `UAT-ACH-CRED-001`, TransactionId 248.
  - CENIT: `UAT-CEN-CRED-001`, TransactionId 249.
- Archivos NACHA-M UAT no vacios generados por `/NachaExport/{cycleId}`:
  - ACH Colombia: 1060 bytes, SHA256 `8EA137CBDCEA6CC4280E5183A66FD29983FE0BF0D4F42732A477AC18DD211844`.
  - CENIT: 1060 bytes, SHA256 `248205FCE69769B8047FEED94346E2E9910918B386D553BC46D6F1218B3D125C`.

Estado actualizado: **Abierto/Parcial**. Avanza la generacion tecnica no vacia por camara, pero sigue pendiente transaccion debito monetaria con prenotificacion ya madura por 3 dias habiles y homologacion campo-a-campo/waiver.

## Actualizacion 2026-05-20 - DEF-UAT-020 nomenclatura y NACHA-M UAT

Estado productivo: NO-GO.

Resultado del ciclo controlado:

| Camara | Archivo generado | SHA256 | ZZZ | Campo 7 registro 1 | Registros | Resultado |
|---|---|---|---:|---|---|---|
| ACH Colombia | docs/uat/evidencias/nacha-m-uat/ach-colombia/0001283.002.1 | E4DAEEE551596D067357953C552CD521871F635F6703D27700171EBC10A0026E | 002 | B | 1/5/6/7/8/9 | OK tecnico UAT |
| CENIT | docs/uat/evidencias/nacha-m-uat/cenit/0001283.001.1 | FD52F7834ADEC53C720E4A877B1D48A8AC15B149BEB7FAFB91EC57CF1B88FCD4 | 001 | A | 1/5/6/7/8/9 | OK tecnico UAT; homologacion normativa formal pendiente |

Evidencia comun:

- Patron aplicado: RRRRTTT.ZZZ.1.
- Originador: Cooperativa Financiera de Antioquia, unico FinancialInstitution.IsDefaultSource=true.
- RRRR=0001 y TTT=283 derivados de la configuracion de CFA.
- Mapeo validado: 001 -> A y 002 -> B en registro tipo 1 campo 7.
- Archivos generados por /NachaExport/{cycleId}; no fueron creados manualmente.
- Sin transmision externa a ACH Colombia o CENIT.
- Proc_Contrapartidas permanece en DryRun para UAT/local.

Observacion normativa:

- ACH Colombia se valida contra MAN-004 V32.
- CENIT se valida tecnicamente con ejemplos disponibles en el proyecto y queda pendiente homologacion normativa formal.

## Actualizacion DEF-UAT-020 - 2026-05-20 prenotificaciones CFA

Se ejecuto UAT controlado de prenotificaciones CFA:

- Endpoint read-only creado/validado: `GET /api/prenotifications/by-reference/{reference}`.
- ACH Colombia: `UAT-ACH-PRE-CFA-001`, TransactionId 256, estado `Pending` / `Pendiente`, codigo NACHA `28`, archivo `0001283.004.1`.
- CENIT: `UAT-CEN-PRE-CFA-001`, TransactionId 257, estado `Pending` / `Pendiente`, codigo NACHA `28`, archivo `0001283.002.1`.
- Archivos no vacios generados por `/NachaExport/{cycleId}`; patron `RRRRTTT.ZZZ.1`, campo 7 y hash validados.

Estado: **OK tecnico UAT para prenotificaciones CFA; DEF-UAT-020 permanece parcial normativo por homologacion formal CENIT/campo-a-campo externo**.

## Actualizacion 2026-05-20 - Simulador NACHA-M de Entrada

Se implemento funcionalidad UAT/local para generar archivos NACHA-M de entrada sinteticos y descargables, separados del flujo real de procesamiento.

| Defecto/Brecha | Estado | Evidencia | Observacion |
|---|---|---|---|
| DEF-UAT-024 Simulador NACHA-M entrada inexistente | Cerrado tecnico UAT | `/api/uat/nacha-inbound-simulator`, `/uat/nacha-inbound-simulator` | Solo genera archivos; procesamiento posterior por NachaUpload |
| DEF-UAT-020 NACHA-M campo-a-campo | Parcial | Simulador aporta datos de entrada controlados | Sigue pendiente validacion real por carga NachaUpload y homologacion normativa |

Guardrails confirmados por diseno:

- `generatedOnly=true`.
- `autoImported=false`.
- `uploadRequired=true`.
- `externalTransmission=false`.
- Productivo sigue **NO-GO**.

## Actualizacion 2026-05-20 - UX Configuracion SOAP

Se acota observacion UX sobre `/integraciones/soap-settings`:

| Defecto/Brecha | Estado | Evidencia | Observacion |
|---|---|---|---|
| OBS-UAT-002 Saturacion visual en Configuracion SOAP | Cerrado tecnico frontend | `web/ach-interbank-ui/src/app/features/admin/components/soap-integration-settings.component.*`, `docs/ux/REDISENO_SOAP_SETTINGS.md` | Se reemplaza formulario gigante inicial por resumen/lista compacta y modales de detalle, edicion y prueba. No se cambia backend ni semantica SOAP. |

Validaciones:

- `npm run build`: OK.
- `npm test -- --watch=false --browsers=ChromeHeadless`: OK, 164 SUCCESS.
- Secretos completos y certificados privados no se exponen.
- Productivo sigue **NO-GO**.

## Actualizacion UX Integraciones - 2026-05-21

- `/integraciones/soap-settings`: se corrige rediseño visual reemplazando la tabla principal por cards compactas y modales de detalle/edicion/prueba. Se agrega validacion DOM/screenshot en `docs/ux/evidencias/`.
- `/integraciones/mappings`: se corrige catalogo para que `WsAxonRespuestaTransaccionesSoapClient` aparezca como integracion activa. Si no tiene mappings, se muestra estado vacio claro; no se eliminan ni modifican mappings existentes.
- Estado productivo: **NO-GO**.

## Actualizacion 2026-05-23 - NACHA-M desagregado en mappings SOAP

| Defecto/Brecha | Estado | Evidencia | Observacion |
|---|---|---|---|
| DEF-UAT-SOAP-MAP-005 `Proc_Transacciones` no demostraba fuente NACHA-M desagregada | Cerrado tecnico UAT | `NachaDesagregadoIntegrationMappingTests`, `docs/architecture/NACHA_M_DESAGREGADO_MAPPING_CATALOG.md` | El mapper resuelve desde `NachaHeaders`, `BatchHeaders`, `EntryDetails`, `AddendaRecords`, `BatchControls`, `FileControls` y persiste trace con valor fuente. |
| DEF-UAT-SOAP-MAP-004 respuesta diferencial sobre prenotificacion pendiente CFA | Cerrado tecnico UAT | `DifferentialPrenotificationResponseProcessorTests`, `docs/uat/EVIDENCIAS_RESPUESTAS_PRENOTIFICACIONES.md` | El procesador aprueba/rechaza `AchTransaction.IsPrenotification=true` cruzando payload, NACHA-M desagregado y mapping publicado; persiste trace y evento; no mueve dinero. |

Productivo: **NO-GO**.
