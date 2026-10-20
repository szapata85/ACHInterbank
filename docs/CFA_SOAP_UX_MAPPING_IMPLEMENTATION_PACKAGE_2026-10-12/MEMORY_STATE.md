# CFA-SOAP-UX-MAP-001 — Memory State

```yaml
program: CFA-SOAP-UX-MAP-001
program_status: IN_PROGRESS
baseline_mode: PACKAGE_SNAPSHOT
baseline_commit: UNKNOWN
baseline_archive: ACHInterbank-R34-TPA9B4-P0.1-BLOCKED-FULL-PROJECT.zip
baseline_archive_sha256: d7d98e2bbe414ae11baa0a22c40200ef073fd563bb97bfe0bdf4fd47c50c6e4d
baseline_archive_entries: 4033
snapshot_fingerprint: 43c3d9567274adf96124b5eb69ef83d6040c5c0ba3973ef47657d1fb82578b55
snapshot_evidence_timestamp_utc: 2026-10-12T21:56:21Z
git_metadata_available: false
git_provenance: UNAVAILABLE
working_tree_concurrency_detection: UNAVAILABLE
snapshot_reproducibility: AVAILABLE
file_level_change_detection: AVAILABLE
concurrent_change_detection: FILE_HASH
active_phase: P8
active_phase_status: IN_PROGRESS
active_micro_prompt: P8.4
active_micro_prompt_status: BLOCKED
next_micro_prompt: P8.4
last_completed_micro_prompt: P8.3
next_allowed_phase: P8
last_verified_commit: null
validation_policy: IMPLEMENTATION_FIRST_VALIDATION_AT_END
validation_execution: IN_PROGRESS_P8
blocked_by: [DOTNET_NOT_AVAILABLE, DOCKER_NOT_AVAILABLE, FRONTEND_DEPENDENCIES_UNAVAILABLE, GAUNTLET_NOT_CERTIFIED, CUTOVER_NOT_CERTIFIED]
```

## 1. Invariantes

- `INV-CFAUX-001`: Los tres servicios objetivo son `WSCFAACH.Proc_Contrapartidas`, `WSCFAACH.Proc_Transacciones` y `WSAXON.RegistrarRespuestaTransaccion`.
- `INV-CFAUX-002`: Este programa no cambia reglas NACHA-M, ciclos, causales, compensación, liquidación ni clasificación monetaria.
- `INV-CFAUX-003`: Angular Material 21 permanece como base de UI del módulo.
- `INV-CFAUX-004`: Branding continúa dinámico mediante el sistema de tokens/BrandingService; la feature no define branding fijo.
- `INV-CFAUX-005`: Texto funcional visible es español-first y evita Spanglish; nombres SOAP/códigos técnicos son información secundaria.
- `INV-CFAUX-006`: Angular no es autoridad de contrato, mapping, tipos, obligatoriedad ni compatibilidad.
- `INV-CFAUX-007`: Runtime solo serializa parámetros pertenecientes a una ContractVersion aprobada.
- `INV-CFAUX-008`: Mapping publicado y ContractVersion Active/Retired son inmutables.
- `INV-CFAUX-009`: Un campo CFA input escalar con fuente ya registrada debe poder agregarse mediante nueva ContractVersion + MappingSet sin modificar el writer/cliente SOAP.
- `INV-CFAUX-010`: No se permite XML libre ni reflection arbitraria configurada por usuario.
- `INV-CFAUX-011`: Response parsers continúan específicos salvo cambio contractual explícito de respuesta.
- `INV-CFAUX-012`: No se retira runtime legacy hasta demostrar paridad para los tres servicios.

## 2. Decisiones iniciales

- `DEC-CFAUX-001`: Introducir `IntegrationContractVersion` y asociar parámetros/mapping sets a la versión exacta del contrato.
- `DEC-CFAUX-002`: Conservar `IntegrationMethodParameter` como entidad de parámetro, agregando `ContractVersionId` y metadata XML, para minimizar migración de reglas existentes.
- `DEC-CFAUX-003`: Source resolution usa Strategy + Registry; no crecer switches monolíticos ni usar reflection de rutas arbitrarias.
- `DEC-CFAUX-004`: Transformaciones son comportamiento de código registrado mediante Strategy; no se crean scripts/expresiones arbitrarias desde DB.
- `DEC-CFAUX-005`: Request SOAP será construido por `ISoapRequestBodyWriter` dirigido por contrato y request resuelto inmutable.
- `DEC-CFAUX-006`: Importación WSDL es local/segura, sin resolver DTD/external entities ni URLs remotas arbitrarias.
- `DEC-CFAUX-007`: Nuevo contrato se activa solamente de forma atómica junto con un mapping completo y validado.
- `DEC-CFAUX-008`: SPA objetivo usa feature stores con Angular signals, Reactive Forms tipados, Material/CDK y componentes pequeños.
- `DEC-CFAUX-009`: Configuración de conexión SOAP queda separada de relación de información; endpoint/action/mode son detalle técnico avanzado.
- `DEC-CFAUX-010`: Rutas legacy se preservan temporalmente mediante redirects durante la transición.

## 3. Brechas abiertas del programa

- `GAP-CFAUX-001`: Provenance Git y diff real de working tree siguen `UNAVAILABLE` en `PACKAGE_SNAPSHOT`; esto no bloquea micro-prompts locales. Una integración futura en un checkout Git real deberá capturar commit y diff reales.
- `GAP-CFAUX-002`: Parámetros SOAP actuales todavía tienen bootstrap code-defined como autoridad.
- `GAP-CFAUX-003`: `Proc_Contrapartidas` ya dispone de ruta contract-driven detrás de `ProcContrapartidas:UseContractDrivenRequest`; el cutover final permanece pendiente de validación P8 y el camino legacy sigue activo por defecto.
- `GAP-CFAUX-004`: `RegistrarRespuestaTransaccion` ya dispone de ruta contract-driven detrás de `RegistrarRespuestaTransaccion:UseContractDrivenRequest`; el mapper fijo legacy permanece activo por defecto hasta la validación P8.
- `GAP-CFAUX-005`: El frontend de integraciones todavía concentra responsabilidades en páginas grandes y lenguaje técnico.
- `GAP-CFAUX-006`: El módulo de integraciones necesita completar consumo consistente de branding tokens y Material/CDK.
- `GAP-CFAUX-008`: No existe flujo administrable de diff/importación/activación de contrato.
- `GAP-CFAUX-009`: `mapping-sets-page` usa dialogs artesanales (`backdrop` + `aside role="dialog"`) sin `MatDialog`/focus trap/restauración de foco; el editor usa un `<li (click)>` para selección de parámetro y la matriz agrega `(click)` a un `span` de label de tab.
- `GAP-CFAUX-011`: Operaciones sensibles todavía no tienen confirmación explícita dedicada: `publish()` publica después de validar y configuración SOAP puede guardar `Live`/habilitado tras mostrar advertencia, pero sin `MatDialog` de confirmación.

## 4. Fases

| Fase | Estado | Propósito |
|---|---|---|
| P0 | DONE | Baseline y caracterización |
| P1 | DONE | Versionado de contrato + migraciones |
| P2 | DONE | Motor genérico de resolución/serialización |
| P3 | DONE | Paridad/cutover de tres servicios |
| P4 | DONE | Campo nuevo sin código + administración contrato |
| P5 | DONE | SPA foundation/Centro de integración |
| P6 | DONE | Mapping guiado humanizado |
| P7 | DONE | Contratos/conexión/responsive/accesibilidad |
| P8 | IN_PROGRESS | Consolidación/gauntlet/cierre |

## 5. Evidencia

- `EVID-CFAUX-P0-GIT-001`: en la raíz del paquete no existe `.git`. `git rev-parse HEAD` y `git status --short` terminaron con código 128 (`not a git repository`); `git diff --stat` terminó con código 129 por la misma condición. Por tanto, SHA base, estado real del árbol y diff real no son verificables en este snapshot.
- `EVID-CFAUX-P0-EOL-001`: inspección binaria directa de `MEMORY.md`: UTF-8 con LF; 459 `LF`, 0 `CRLF`, 0 `CR`. `git ls-files --eol MEMORY.md` no pudo ejecutarse por ausencia de `.git`.
- `EVID-CFAUX-P0-SOAP-001`: mapa de ejecución localizado sin modificar lógica: `Proc_Transacciones` entra por `IncomingNachaPostProcessingHandler` o `MaintenanceController` -> `IncomingNachaPostProcessingOrchestrator` -> `IncomingNachaPostProcessingPipeline/IncomingNachaExternalDispatchStage` -> `ProcTransaccionesRequestMapper.ResolveAsync/BuildSoapBody` -> `WscfaachSoapClient.ProcTransaccionesAsync`.
- `EVID-CFAUX-P0-SOAP-002`: `Proc_Contrapartidas` entra por `AchContrapartidasByCycleHandler`, `UatContrapartidasController` o delegación desde `IncomingNachaPostProcessingPipeline` -> `ContrapartidaDispatchJobService` -> `ProcContrapartidasRequestMapper` (resolución vía `ProcContrapartidasFunctionalMappingResolver`) -> `BuildSoapBody` -> `WscfaachSoapClient.ProcContrapartidasAsync`.
- `EVID-CFAUX-P0-SOAP-003`: `RegistrarRespuestaTransaccion` entra por `AchResponsesController` -> `NotificarRespuestaAchUseCase` -> `RegistrarRespuestaAchCommandMapper` -> `RespuestaTransaccionesAchGateway` -> `RegistrarRespuestaAchSoapRequestMapper` -> `WsAxonRespuestaTransaccionesSoapClient.RegistrarRespuestaTransaccionAsync`; el body físico se genera con `SoapParameterMapper`.
- `EVID-CFAUX-P0-FE-001`: impacto frontend directo localizado en `web/ach-interbank-ui/src/app/app-routing.module.ts`, `core/services/integration-mapping-admin.service.ts`, `core/services/soap-integration-settings.service.ts` y `features/integrations/**` (routing/module, workspace, mapping sets, mapping editor, SOAP settings y specs). No se rediseñó ni modificó frontend.
- `EVID-CFAUX-P0-MEMORY-001`: `MEMORY.md` raíz no contiene IDs `CFAUX` en este snapshot; no se modificó.
- `EVID-CFAUX-P0-SNAPSHOT-001`: P0.1R normalizó el baseline a `PACKAGE_SNAPSHOT`. El ZIP fuente `ACHInterbank-R34-TPA9B4-P0.1-BLOCKED-FULL-PROJECT.zip` no contiene `.git`, tiene 4033 entradas y SHA-256 `d7d98e2bbe414ae11baa0a22c40200ef073fd563bb97bfe0bdf4fd47c50c6e4d` (evidencia `2026-10-12T21:56:21Z`). El fingerprint `43c3d9567274adf96124b5eb69ef83d6040c5c0ba3973ef47657d1fb82578b55` se calculó sobre un manifiesto determinista, ordenado por ruta, de 31 hashes SHA-256 críticos del ZIP fuente antes de modificar este estado. Git provenance y detección de concurrencia de working tree quedan `UNAVAILABLE`; reproducibilidad de snapshot y detección de cambios por archivo quedan `AVAILABLE` mediante hash. No se inventó commit Git.
- `EVID-CFAUX-P0-SNAPSHOT-CHECK-002`: antes de P0.2 se recalcularon SHA-256 de los siete archivos productivos críticos de los tres requests (`ProcTransaccionesRequestMapper`, `ProcContrapartidasRequestMapper`, `ProcContrapartidasFunctionalMappingResolver`, `RegistrarRespuestaAchSoapRequestMapper`, `SoapParameterMapper`, `WscfaachSoapClient`, `WsAxonRespuestaTransaccionesSoapClient`) y todos coincidieron exactamente con los hashes de P0.1R; no se detectó cambio inesperado.
- `EVID-CFAUX-P0-SOAP-GOLDEN-TRANS-001`: se agregó caracterización determinista inline para `Proc_Transacciones` que fija SOAP 1.1 Envelope/Body, operación y namespace `http://tempuri.org/`, orden de enumeración del diccionario dinámico, omisión actual de valores null/vacíos/whitespace y de `ILR`/`METODO`, y escape XML. El cliente ya tenía caracterización de SOAPAction `http://tempuri.org/IWSCFAACH/Proc_Transacciones`. La nueva prueba no pudo ejecutarse en este entorno.
- `EVID-CFAUX-P0-SOAP-GOLDEN-CONTRA-001`: se agregó caracterización determinista inline para `Proc_Contrapartidas` que fija el request tipado, operación/namespace, los 22 elementos en su orden explícito actual, presencia de elementos vacíos, formato decimal invariant y escape XML. El cliente ya tenía caracterización de SOAPAction `http://tempuri.org/IWSCFAACH/Proc_Contrapartidas`. La nueva prueba no pudo ejecutarse en este entorno.
- `EVID-CFAUX-P0-SOAP-GOLDEN-RESP-001`: se agregó caracterización determinista inline para `RegistrarRespuestaTransaccion` que fija los siete parámetros físicos, su orden, mapping `IdTransaccionServicioExterno -> idTransaccionAxon`, representación null como elemento vacío, namespace `http://tempuri.org/` y escape XML. Las pruebas existentes del cliente fijan SOAP 1.1 y SOAPAction `http://tempuri.org/IWSAxonRespuestaTransacciones/RegistrarRespuestaTransaccion`. La nueva prueba no pudo ejecutarse en este entorno.
- `EVID-CFAUX-P0-TEST-001`: intento focal `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter FullyQualifiedName~CfaSoapRequestCharacterizationTests` el `2026-10-12T22:04:20Z` terminó con código 127 porque `dotnet` no está instalado ni disponible en `PATH`; búsquedas de `dotnet`, `msbuild`, `csc`, `mcs` y `mono` no encontraron toolchain alternativo. No se declara ningún PASS.
- `EVID-CFAUX-P0-SNAPSHOT-CHECK-003`: al reanudar P0.2 bajo `IMPLEMENTATION_FIRST_VALIDATION_AT_END`, los SHA-256 de los siete archivos productivos críticos SOAP coincidieron nuevamente con el baseline `PACKAGE_SNAPSHOT`; no se detectó cambio productivo inesperado antes de actualizar estado. Evidencia `2026-10-12T22:23:20Z`.
- `EVID-CFAUX-P0-IMPL-FIRST-001`: P0.2 queda implementado mediante `CfaSoapRequestCharacterizationTests` más las caracterizaciones existentes de ambos clientes SOAP. La ejecución de build/tests se registra `DEFERRED_TO_P8` por política explícita; `DOTNET_SDK_UNAVAILABLE` deja de ser bloqueo estructural y no se declara PASS no ejecutado.
- `EVID-CFAUX-P0-SNAPSHOT-CHECK-004`: antes de P0.3 se verificó el paquete de entrada `ACHInterbank-R34-TPA9B4-P0.2-DONE-FULL-PROJECT.zip` (4034 entradas; SHA-256 `1664c06824000775fc40f25489ea69e1ad56beb904ae39851d30ed397f024a4d`). `AGENTS.md`, `MEMORY.md` y los 21 archivos frontend críticos registrados en P0.1R coincidieron exactamente con sus hashes esperados; no hubo cambio inesperado previo a la caracterización SPA. Evidencia `2026-10-12T22:31:09Z`.
- `EVID-CFAUX-P0-SPA-BASELINE-001`: P0.3 caracterizó estáticamente rutas, páginas, servicios, tests, Material, branding, responsive, accesibilidad y separación de responsabilidades de `features/integrations/**` sin modificar frontend. Hallazgos durables: entrada `/integraciones` -> `mappings`; rutas `soap-settings`, `mappings`, `mappings/:methodCode/:mappingSetId`; redirect legacy `/soap-integrations`; cuatro páginas relevantes; `mapping-sets-page` (1339 TS/309 HTML/362 SCSS) y `mapping-editor-page` (717/242/47) son sobredimensionados y mezclan data/state/presentation; `soap-integration-settings` (447/176/234) concentra conexión técnica; dialogs manuales están en `mapping-sets-page`; la matriz usa tabla de 1120px con scroll horizontal; workspace/editor contienen hardcodes visuales; Material y tokens dinámicos ya existen parcialmente; tests actuales son 1 routing, 28 mapping-list, 11 editor y 10 SOAP-settings. Validación ejecutable queda `DEFERRED_TO_P8`.

- `EVID-CFAUX-P0-SNAPSHOT-CHECK-005`: cierre P0 sobre `ACHInterbank-R34-TPA9B4-P0.3-DONE-FULL-PROJECT.zip` (4034 entradas; SHA-256 `ee3efcd9099734a072c32e491b824d07ae0812d5a5ccb5f63738941d3a4466ed`). `.git` continúa ausente. Los 30 archivos críticos inmutables de la tabla 13.2 (excluyendo el `MEMORY_STATE.md` operativo mutable) coinciden exactamente con sus hashes P0.1R; 0 faltantes y 0 cambios inesperados. La prueba de caracterización `tests/Cfa.ACHInterbank.Tests/CfaSoapRequestCharacterizationTests.cs` existe con SHA-256 `b8530e4f2985dda9581aec05d38a316818e2677f32de31f766fbe7cc17fd0146`. Evidencia `2026-10-12T23:01:00Z`.
- `EVID-CFAUX-P0-CLOSE-001`: P0 queda consolidado con identidad reproducible `PACKAGE_SNAPSHOT`, mapa de ejecución de los tres requests, caracterización contractual implementada para `Proc_Transacciones`, `Proc_Contrapartidas` y `RegistrarRespuestaTransaccion`, baseline SPA durable de Integraciones, gaps CFAUX abiertos preservados y validaciones ejecutables marcadas `DEFERRED_TO_P8`. No se ejecutó build, tests runtime, Playwright, Docker, migraciones runtime ni UAT en P0.4; no se declara PASS no ejecutado.
- `EVID-CFAUX-P1-CONTRACT-MODEL-001`: P1.1 implementó de forma aditiva el modelo versionado de contrato sin tocar mappers/clientes/dispatch/parsers/runtime SOAP: `IntegrationContractVersion` auditable y revisionado; enums tipados `IntegrationContractVersionStatusEnum` (`Draft/Validated/Active/Retired`) e `IntegrationContractSourceTypeEnum` (`WsdlImport/ManualApproved/BaselineMigration`); `IntegrationMethod.ContractVersions`; metadata funcional reutilizando `IntegrationMethod.DisplayName` como nombre funcional y agregando `FunctionalDescription/FunctionalCategory/RiskLevel`; `IntegrationMethodParameter.ContractVersionId` nullable + metadata XML/XSD/cardinalidad/soporte; `IntegrationMappingSet.ContractVersionId` nullable + `Revision`; configuración EF con FK `Restrict`, unique `(MethodId, VersionLabel)`, índices de estado/hash y vínculos por contrato. Se preservó temporalmente el unique legacy `(MethodId, ParameterPath)` durante la ventana nullable; el unique final por contrato queda para el backfill/endurecimiento posterior. Antes de modificar se verificaron 30/30 hashes críticos P0 + caracterización SOAP sin cambios; fingerprint pre-change de los cinco archivos existentes modificados `a31273359d6aed8c2711af058c27b5190b003702527eab839ce4ac5d4d69bafe`, post-change `6e0633296d57961d442aa79d9438aae0885a9e4e2d1748a05fa0d8ea49c5006a`. Se agregó `IntegrationContractVersionModelTests.cs`; build/tests/DB/Docker/UAT no se ejecutaron y quedan `DEFERRED_TO_P8`. No se creó ninguna migración. Evidencia `2026-10-12T23:13:24Z`.
- `EVID-CFAUX-P1-CONTRACT-MIGRATION-001`: P1.2 se implementó sobre `ACHInterbank-R34-TPA9B4-P1.1-DONE-FULL-PROJECT.zip` (SHA-256 `cca0dae51e88af9468b6c17fcdf4bd2c2dc5683e42251b60292cc068abab9a69`, 4625 entradas, `.git` ausente). Se añadieron migraciones expand-only equivalentes para PostgreSQL `20261012231000_IntegrationsAddContractVersioningBaseline` y SQL Server `20261012231100_IntegrationsAddContractVersioningBaseline`, con snapshots/designer actualizados, tabla `IntegrationContractVersions`, FK `Restrict`, metadata funcional/XML, `Revision`, y `ContractVersionId` nullable. El backfill crea `baseline-v1` determinista para `WSCFAACH.Proc_Contrapartidas` (`39b5693c-407b-505c-8e8a-e49e8558a82e`), `WSCFAACH.Proc_Transacciones` (`9f3a13b6-eb79-5e43-803b-4c97b71a0f5d`) y `WSAXON.RegistrarRespuestaTransaccion` (`4a093eb5-beed-5b15-bc7c-5768c0e0547e`) usando operation/namespace/SOAPAction caracterizados en P0, preserva IDs existentes, enlaza todos los parámetros y mapping sets del método, inicializa `Revision >= 1` y deriva metadata XML únicamente del catálogo actual (`XmlElementName=ParameterPath`, `XsdType=DataType`, cardinalidad desde `Required`), sin afirmar WSDL nuevo. Los bootstrappers de catálogo/mapping solo completan metadata/vínculos baseline cuando la migración ya existe para cubrir clean-start posterior a migración; no cambian resolución ni serialización SOAP. Se escribieron `IntegrationContractVersionMigrationTests` y una prueba de bootstrap de asociación/IDs. Inspección estática confirmó snapshots/designer equivalentes, Up sin drops/deletes y los siete archivos productivos SOAP byte-identical al paquete P1.1; build/tests/DB/Docker/UAT no se ejecutaron y quedan `DEFERRED_TO_P8`. Evidencia `2026-10-12T23:27:36Z`.
- `EVID-CFAUX-P1-CONTRACT-GOVERNANCE-001`: P1.3 quedó implementado por inspección estática con `IIntegrationContractVersionService`/`IntegrationContractVersionService` para queries y resolución explícita/Active; lifecycle `Draft -> Validated -> Active -> Retired`; `Revision` como concurrency token; retiro automático de la Active previa al activar una Validated; prohibición de delete de ContractVersion; inmutabilidad de ContractVersion Active/Retired y de sus parámetros; guard de only-one-active y migraciones PostgreSQL `20261012233000_IntegrationsContractVersionConstraints` / SQL Server `20261012233100_IntegrationsContractVersionConstraints` con índices filtrados para Active, parámetros versionados/legacy y MappingSet por ContractVersion. `ContractVersionId` permanece nullable como puente legacy intencional, sin cutover ni pérdida de historia. Pruebas focales escritas, no ejecutadas; validación ejecutable `DEFERRED_TO_P8`.
- `EVID-CFAUX-P1-CLOSE-001`: P1.4 inspeccionó únicamente el modelo/servicios/migraciones/bootstrappers/tests P1 relacionados. `IntegrationContractVersion` está relacionado `IntegrationMethod -> ContractVersions` y por FK `Restrict` con parámetros/mapping sets; las migraciones dual-provider contienen el backfill determinista `baseline-v1` para `WSCFAACH.Proc_Contrapartidas`, `WSCFAACH.Proc_Transacciones` y `WSAXON.RegistrarRespuestaTransaccion`; snapshots de ambos providers contienen `UX_IntegrationContractVersions_Method_Active`, unicidad versionada/legacy de parámetros y unicidad de MappingSet por contrato; resolución/lifecycle/inmutabilidad existen en source. Los siete archivos productivos críticos del runtime SOAP legacy conservan exactamente los SHA-256 P0 (`ProcTransaccionesRequestMapper`, `ProcContrapartidasRequestMapper`, `ProcContrapartidasFunctionalMappingResolver`, `RegistrarRespuestaAchSoapRequestMapper`, `SoapParameterMapper`, `WscfaachSoapClient`, `WsAxonRespuestaTransaccionesSoapClient`), por lo que no se detectó cambio de XML/dispatch/cliente SOAP en P1. No se ejecutaron build, tests, migraciones, Docker ni UAT; todo ello permanece `DEFERRED_TO_P8`.

## 6. Closures

- `CLOSE-CFAUX-P0-SNAPSHOT-001`: bloqueo operativo de P0.1 resuelto mediante baseline reproducible `PACKAGE_SNAPSHOT`; ausencia de `.git` ya no bloquea micro-prompts locales bajo las reglas de hash registradas.
- `CLOSE-CFAUX-P0-P02-001`: P0.2 completado a nivel implementación/characterization; las validaciones ejecutables quedan diferidas a P8 por `IMPLEMENTATION_FIRST_VALIDATION_AT_END`, sin alterar comportamiento productivo.
- `CLOSE-CFAUX-P0-P03-001`: P0.3 completado como baseline SPA durable por inspección estática; no hubo cambios de rutas, UI ni comportamiento frontend y toda validación runtime queda `DEFERRED_TO_P8`.
- `EVID-CFAUX-P2-CLOSE-001`: P2.4 cerró P2 por inspección estática dirigida sobre `ACHInterbank-R34-TPA9B4-P2.3-DONE-FULL-PROJECT.zip` (4665 entradas; SHA-256 `d2600a3ea8e8783e7e3248c72f89311cd65fcf8d2ffc14f87887018d31738ccd`). Se confirmó existencia/coherencia de contratos/modelos del motor, registries de sources/transformations, providers actuales para `WSCFAACH.Proc_Contrapartidas`, `WSCFAACH.Proc_Transacciones` y `WSAXON.RegistrarRespuestaTransaccion`, resolución genérica de parámetros y `ContractDrivenSoapBodyWriter`; las pruebas de contratos/source-resolution/writer y paridad legacy de los tres servicios existen y permanecen no ejecutadas. Búsqueda únicamente por símbolos del motor nuevo confirmó que writer/resolver/registries no están registrados ni referenciados desde runtime productivo; no hay cutover. Los siete archivos críticos legacy conservaron exactamente los SHA-256 P0 registrados. Por instrucción explícita de P2.4 no se agregó feature flag, no se implementó funcionalidad nueva y no se ejecutaron build/tests/DB/Docker/UAT; validación `DEFERRED_TO_P8`. Evidencia `2026-10-13T01:27:39Z`.
- `CLOSE-CFAUX-P0-P04-001`: P0 cerrado formalmente. `NEXT_ALLOWED_PHASE=P1`; `P1.1` es el siguiente micro-prompt permitido. La ausencia de Git permanece documentada y no se inventa commit; la protección concurrente continúa por hashes de archivo.
- `CLOSE-CFAUX-P1-GAP007-001`: `GAP-CFAUX-007` queda cerrado/sustituido por el diseño P1.3: la unicidad contractual quedó endurecida con índices filtrados y guards, mientras `ContractVersionId IS NULL` se conserva explícitamente como puente de compatibilidad legacy; no se exige `NOT NULL` antes del cutover.
- `CLOSE-CFAUX-P1-P14-001`: P1 cerrado formalmente por inspección estática. `P1 = DONE`, `P1.4 = DONE`, runtime SOAP legacy intacto, historia contractual preservada y validaciones ejecutables `DEFERRED_TO_P8`. `NEXT_ALLOWED_PHASE=P2`; el siguiente micro-prompt permitido es `P2.1`.
- `CLOSE-CFAUX-P2-P24-001`: P2 cerrado formalmente por inspección estática bajo el alcance explícito de P2.4. `P2 = DONE`, `P2.4 = DONE`, motor genérico contract-driven consistente, cobertura de tres servicios preservada, runtime SOAP legacy intacto, cero cutover y validaciones ejecutables `DEFERRED_TO_P8`. `NEXT_ALLOWED_PHASE=P3`; el siguiente micro-prompt permitido es `P3.1`.

## 7. Files touched

- `docs/CFA_SOAP_UX_MAPPING_IMPLEMENTATION_PACKAGE_2026-10-12/MEMORY_STATE.md` (P0.1/P0.1R, cierres P0.2/P0.3/P0.4 y estado/evidencia P1.1; sin comportamiento runtime).
- `tests/Cfa.ACHInterbank.Tests/CfaSoapRequestCharacterizationTests.cs` (P0.2; pruebas de caracterización SOAP sintéticas; código productivo intacto).
- `src/Cfa.ACHInterbank.Domain/Entities/Integrations/IntegrationMappingModels.cs` (P1.1; ContractVersion, enums tipados, metadata y vínculos nullable).
- `src/Cfa.ACHInterbank.Persistence/Configuration/IntegrationContractVersionConfiguration.cs` (P1.1; nueva configuración EF).
- `src/Cfa.ACHInterbank.Persistence/Configuration/IntegrationMethodConfiguration.cs` (P1.1; metadata funcional).
- `src/Cfa.ACHInterbank.Persistence/Configuration/IntegrationMethodParameterConfiguration.cs` (P1.1; metadata XML/XSD y FK nullable de contrato).
- `src/Cfa.ACHInterbank.Persistence/Configuration/IntegrationMappingSetConfiguration.cs` (P1.1; Revision, FK nullable e índice por contrato; comportamiento legacy de Rules/History conservado).
- `src/Cfa.ACHInterbank.Persistence/DataBase/AchDbContext.cs` (P1.1; DbSet de ContractVersion).
- `tests/Cfa.ACHInterbank.Tests/IntegrationContractVersionModelTests.cs` (P1.1; invariantes de dominio/modelo EF, no ejecutadas).
- `src/Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres/20261012231000_IntegrationsAddContractVersioningBaseline.cs` + `.Designer.cs` + `AchDbContextModelSnapshot.cs` (P1.2; migración/backfill PostgreSQL).
- `src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/20261012231100_IntegrationsAddContractVersioningBaseline.cs` + `.Designer.cs` + `AchDbContextModelSnapshot.cs` (P1.2; migración/backfill SQL Server).
- `src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationCatalogBootstrapper.cs` (P1.2; metadata XML/funcional y enlace baseline nullable para parámetros creados después de la migración; sin cutover).
- `src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingBootstrapper.cs` (P1.2; enlace idempotente de mapping sets legacy/seed al baseline cuando `ContractVersionId` está vacío; sin cambiar reglas/resolución).
- `tests/Cfa.ACHInterbank.Tests/IntegrationContractVersionMigrationTests.cs` (P1.2; invariantes dual-provider/backfill/snapshots, no ejecutadas).
- `tests/Cfa.ACHInterbank.Tests/IntegrationBootstrapperTests.cs` (P1.2; prueba de asociación baseline preservando IDs, no ejecutada).
- `src/Cfa.ACHInterbank.Application/Integrations/Dtos/IntegrationCatalogDtos.cs` + `Interfaces/IIntegrationContractVersionService.cs` (P1.3; contrato Application para queries/resolución/lifecycle).
- `src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationContractVersionService.cs` + `DataBase/IntegrationContractVersionInvariantGuard.cs` (P1.3; resolución, lifecycle, revisión, only-one-active, historia e inmutabilidad).
- `src/Cfa.ACHInterbank.Persistence/DataBase/AchDbContext.cs` + configuraciones de ContractVersion/Parameter/MappingSet (P1.3; índices filtrados, concurrency y guards).
- `src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationCatalogService.cs` + `IntegrationMappingSetService.cs` (P1.3; queries/admin contract-aware con fallback legacy).
- `src/Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres/20261012233000_IntegrationsContractVersionConstraints.cs` + snapshot (P1.3; constraints PostgreSQL).
- `src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/20261012233100_IntegrationsContractVersionConstraints.cs` + snapshot (P1.3; constraints SQL Server).
- `tests/Cfa.ACHInterbank.Tests/IntegrationContractVersionLifecycleTests.cs` + `IntegrationContractVersionConstraintMigrationTests.cs` + actualización de `IntegrationContractVersionModelTests.cs` (P1.3; pruebas escritas, no ejecutadas).
- `docs/CFA_SOAP_UX_MAPPING_IMPLEMENTATION_PACKAGE_2026-10-12/MEMORY_STATE.md` (P1.4; cierre formal P1 y habilitación P2; sin funcionalidad productiva nueva).
- `docs/CFA_SOAP_UX_MAPPING_IMPLEMENTATION_PACKAGE_2026-10-12/MEMORY_STATE.md` (P2.4; cierre formal P2 y habilitación P3; única modificación del micro-prompt, sin funcionalidad productiva nueva).

## 8. Migrations

- P0: ninguna.
- P1.1: ninguna.
- P1.2: PostgreSQL `20261012231000_IntegrationsAddContractVersioningBaseline`; SQL Server `20261012231100_IntegrationsAddContractVersioningBaseline`; expand-only en `Up`, backfill determinista, `ContractVersionId` aún nullable.
- P1.3: PostgreSQL `20261012233000_IntegrationsContractVersionConstraints`; SQL Server `20261012233100_IntegrationsContractVersionConstraints`; only-one-active, unicidad por ContractVersion, puente legacy filtrado y downgrade guardado contra pérdida/colisión de historia.
- P1.4: ninguna migración nueva; revisión estática únicamente.

## 9. Tests passed

- P0.1/P0.1R: no ejecutados.
- P0.2: `DEFERRED_TO_P8` por `IMPLEMENTATION_FIRST_VALIDATION_AT_END`. Se conserva como evidencia histórica el intento anterior `NOT_RUN_ENVIRONMENT`; no se reintentó build/test en este turno y no se declara PASS.
- P0.3: `DEFERRED_TO_P8`; no se ejecutaron build Angular, unit tests, Playwright, Lighthouse, axe, Docker, UAT ni pruebas responsive runtime.
- P0.4: `DEFERRED_TO_P8`; cierre documental/estático únicamente. No se ejecutaron build, tests, Playwright, Docker, migraciones runtime ni UAT.
- P1.1: `DEFERRED_TO_P8`; se agregó `IntegrationContractVersionModelTests.cs`, pero no se ejecutaron build, tests, DB, Docker ni UAT por `IMPLEMENTATION_FIRST_VALIDATION_AT_END`.
- P1.2: `DEFERRED_TO_P8`; se escribieron pruebas dual-provider/backfill/bootstrap, pero no se ejecutaron `dotnet ef`, build, tests, DB, Docker ni UAT.
- P1.3: `DEFERRED_TO_P8`; se escribieron pruebas focales de resolución/lifecycle/inmutabilidad/constraints, pero no se ejecutaron build, tests, migraciones runtime, PostgreSQL/SQL Server, Docker, SOAP runtime ni UAT.
- P1.4: `DEFERRED_TO_P8`; cierre por inspección estática. No se ejecutaron build, tests, migraciones, Docker ni UAT y no se declara PASS ejecutable.
- P2.1: `DEFERRED_TO_P8`; se escribieron unit tests de contratos/invariantes del motor genérico, pero no se ejecutaron build, tests, Docker, SOAP runtime ni UAT.
- P2.2: `DEFERRED_TO_P8`; se escribieron unit tests de fixed/default/source/null/transform/type y compatibilidad source/method, pero no se ejecutaron build, tests, Docker, SOAP runtime ni UAT.
- P2.3: `DEFERRED_TO_P8`; se escribieron pruebas del writer SOAP y paridad/golden para los tres servicios, pero no se ejecutaron build, tests, Docker, SOAP runtime ni UAT.
- P2.4: `DEFERRED_TO_P8`; cierre por inspección estática únicamente. No se ejecutaron build, tests, DB, Docker ni UAT y no se declara PASS ejecutable.

## 10. Siguiente acción concreta

`NEXT-CFAUX-001`: Reanudar exclusivamente `P8.4` en un entorno con .NET SDK, Docker/providers PostgreSQL y SQL Server, y dependencias frontend/Chromium disponibles. Mantener `baseline_mode: PACKAGE_SNAPSHOT`, `baseline_commit: UNKNOWN` y protección `FILE_HASH`. P8.4 quedó bloqueado porque los gates ejecutables obligatorios no pudieron certificarse; no se realizó cutover y `GAUNTLET_PASSED` continúa NO.

## 11. Regla de actualización

Al cerrar una fase:

1. actualizar `active_phase_status`;
2. registrar decisiones nuevas solo si realmente fueron tomadas;
3. cerrar gaps con evidencia;
4. agregar evidencia resumida, nunca logs completos;
5. listar migraciones y pruebas ejecutadas;
6. fijar `next_allowed_phase`;
7. no declarar `PASS` a una prueba no ejecutada;
8. no registrar secretos, payloads completos, cuentas, PII, tokens o certificados.


## 12. Micro-prompts

- `P0.1` es el primer micro-prompt permitido.
- Un turno ejecuta un solo micro-prompt.
- El cierre de fase ocurre únicamente en el último micro-prompt de esa fase (`P0.4`, `P1.4`, `P2.4`, `P3.4`, `P4.5`, `P5.4`, `P6.4`, `P7.5`, `P8.4`).
- Un micro-prompt `DONE` actualiza `last_completed_micro_prompt` y `next_micro_prompt`.
- Un micro-prompt `BLOCKED` conserva `next_micro_prompt` en el mismo código hasta resolver el bloqueo.
- Consultar `MICRO_PROMPT_PROTOCOL.md` y `MICRO_PROMPTS_INDEX.md`.

## 13. Baseline `PACKAGE_SNAPSHOT`

### 13.1 Capacidades y regla durable

- Git provenance: `UNAVAILABLE`.
- Working-tree concurrency detection: `UNAVAILABLE`.
- Snapshot reproducibility: `AVAILABLE` mediante SHA-256 del ZIP fuente y fingerprint determinista de archivos críticos.
- File-level change detection: `AVAILABLE` mediante SHA-256; `concurrent_change_detection: FILE_HASH`.
- Cuando `baseline_mode = PACKAGE_SNAPSHOT`, la ausencia de `.git` no bloquea micro-prompts locales.
- Antes de modificar un archivo crítico se compara su hash actual con el hash esperado cuando aplique; no se sobrescribe un archivo cuyo contenido haya cambiado inesperadamente.
- Cada micro-prompt registra los archivos modificados.
- Nunca se reporta commit Git en este modo; `baseline_commit` permanece `UNKNOWN`.
- Los cierres usan evidencia de snapshot/hash/tests/build según el alcance real ejecutado.
- Cualquier integración futura en un checkout Git real deberá capturar posteriormente commit y diff reales.
- El hash de `MEMORY_STATE.md` registrado abajo corresponde a los bytes del ZIP fuente al inicio de P0.1R. El archivo es estado operativo mutable y fue el único archivo modificado por P0.1R; por ello su hash embebido no pretende auto-hashear la versión que lo contiene.

### 13.2 Fingerprint y hashes críticos del ZIP fuente

Algoritmo de `snapshot_fingerprint`: SHA-256 de líneas UTF-8/LF `"<sha256>  <ruta>\n"`, ordenadas lexicográficamente por ruta, para los 31 archivos listados a continuación.

| Archivo | SHA-256 |
|---|---|
| `AGENTS.md` | `3de1851386fc9650c0c100b7383bd418bfde9a79858cffb00879353c5abd1266` |
| `MEMORY.md` | `1de34e48b7498da6f54e1f55e2cceab48b8c7db9c2da9a7dac0fd374bb5b0fa4` |
| `docs/CFA_SOAP_UX_MAPPING_IMPLEMENTATION_PACKAGE_2026-10-12/MEMORY_STATE.md` | `ee4d1a41394e112e1c7a6916554cd02435f865552f3e73d72dbe406b43caf3fe` |
| `src/Cfa.ACHInterbank.External/Connections/RespuestaTransaccionesAch/RegistrarRespuestaAchSoapRequestMapper.cs` | `c306245abca0d1503460870eff2a6591c86d0b541d7895ea30a3942d1f53ab76` |
| `src/Cfa.ACHInterbank.External/Connections/SoapParameterMapper.cs` | `1bff95f16d8c59cf6dd7a4bbc23d565bc5e7cc5c70b1486840cfd2255e4882d3` |
| `src/Cfa.ACHInterbank.External/Connections/WsAxonRespuestaTransaccionesSoapClient.cs` | `d8f0844a3ff904683ec357d4a51f93c642eb136686dce50a7d07deccc011657d` |
| `src/Cfa.ACHInterbank.External/Connections/WscfaachSoapClient.cs` | `c7cf6bc000fe4c5fbebfc60351aa6f25e3085ee1dd3ae364cd0536c919f98f72` |
| `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcContrapartidasRequestMapper.cs` | `030310a0eaa066e9ce5591bae30c53da6514f136500578ab16793e52782c176a` |
| `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcTransaccionesRequestMapper.cs` | `1242813d9d3c412db72dce06c64d246cdf73ede74b2f2c4655850c8a8b2badf0` |
| `src/Cfa.ACHInterbank.Persistence/Integrations/Services/ProcContrapartidasFunctionalMappingResolver.cs` | `e0e1e4ebc878773730d659fca336da2727fae74956b5bd266588bd448b19af8e` |
| `web/ach-interbank-ui/src/app/app-routing.module.ts` | `8aebb136a0f5d1ae9f22b4af1467a3191766aa6836f3314d08e2b9c94150e11c` |
| `web/ach-interbank-ui/src/app/core/services/integration-mapping-admin.service.ts` | `a78ea7bfc0e826c6772eb0e9577b02ed9d257539632b423589cf8325fa5f13c4` |
| `web/ach-interbank-ui/src/app/core/services/soap-integration-settings.service.ts` | `80544d93d955be577db25dcedcae24b0f54211870c5e2e0c7b3c5befeca717c2` |
| `web/ach-interbank-ui/src/app/features/integrations/integrations-routing.module.spec.ts` | `637e9d91780d516cb2d2ac50ae82b825e3409e5bc2bc1d71c977a7cf2b92a921` |
| `web/ach-interbank-ui/src/app/features/integrations/integrations-routing.module.ts` | `f1059cba94a17101c9875cc2af6e1a42928750ed29bb443d75afc38980946b0e` |
| `web/ach-interbank-ui/src/app/features/integrations/integrations.module.ts` | `7594285a7b7d1a259179ae975335a05b863132613b2a4fd195a6cdbea1a4701c` |
| `web/ach-interbank-ui/src/app/features/integrations/pages/integration-workspace.component.html` | `97b9dc2cfaacb02183a022f6891f6727747cb9d06cda16ac0b07018a721346fd` |
| `web/ach-interbank-ui/src/app/features/integrations/pages/integration-workspace.component.scss` | `2d6ab79bb98ea948fe3981a832b8a8e6ffe8ce840536dfcbe7e3b5950dee2e85` |
| `web/ach-interbank-ui/src/app/features/integrations/pages/integration-workspace.component.ts` | `2735203893a9126fc713b6b55254c197894a813ba7492e413d3e2212dec83a73` |
| `web/ach-interbank-ui/src/app/features/integrations/pages/mapping-editor-page.component.html` | `f90f71147af4554bb25b8b8aeac1d343e11bac9c25d473822159293fe92b1084` |
| `web/ach-interbank-ui/src/app/features/integrations/pages/mapping-editor-page.component.scss` | `f88e678a888f3109668a89793822e0878d0a430dcd5fa960c48f66578907aba2` |
| `web/ach-interbank-ui/src/app/features/integrations/pages/mapping-editor-page.component.spec.ts` | `9d5bef67b97e91e45f0404868c96201770464a0c02f1283577ef4bb493e08584` |
| `web/ach-interbank-ui/src/app/features/integrations/pages/mapping-editor-page.component.ts` | `f3d81b69cd95a4d30451e81c2c19c9d6b3031d4988c1c83d3b1d20bcdec567bd` |
| `web/ach-interbank-ui/src/app/features/integrations/pages/mapping-sets-page.component.html` | `2edc4ef469db60c583ec78e403f5a0e164baabba9ea0adef59bcf5d927dd5f48` |
| `web/ach-interbank-ui/src/app/features/integrations/pages/mapping-sets-page.component.scss` | `af1d82b6e93ccd76595b59b26f7269e096c5d252fcc14d33f4bef781d06fdb59` |
| `web/ach-interbank-ui/src/app/features/integrations/pages/mapping-sets-page.component.spec.ts` | `aa6b81bc0fca7397f3154c44ae3ca61d2e9260a83ab3d1a62885f8af0b804761` |
| `web/ach-interbank-ui/src/app/features/integrations/pages/mapping-sets-page.component.ts` | `2dc802cf526e547bcab7aedaaee6495e4f80bd5ab2dceeae20f0314df41af115` |
| `web/ach-interbank-ui/src/app/features/integrations/pages/soap-integration-settings.component.html` | `42b87ba0389610215a849b5f0f12ce5ddaea45acf22131cb81445332455db0b6` |
| `web/ach-interbank-ui/src/app/features/integrations/pages/soap-integration-settings.component.scss` | `6f3ffe73a7fdd057d06406591d836944da138cd9194a84cd9d032538d102408b` |
| `web/ach-interbank-ui/src/app/features/integrations/pages/soap-integration-settings.component.spec.ts` | `b384ed11e13e365e694e217a72c3751e9de816c1fb13f87fe4cd6ba85538a232` |
| `web/ach-interbank-ui/src/app/features/integrations/pages/soap-integration-settings.component.ts` | `23e5a55ed765c5bce10c1a0267e298f60fb3b9723702a71d37f6768995bdecc6` |



## 14. P0.3 — Baseline SPA de Integraciones

### 14.1 Navegación actual

| Ruta | Componente / efecto | Estado actual |
|---|---|---|
| `/integraciones` | `IntegrationsModule` lazy + `IntegrationWorkspaceComponent` | Entrada protegida por rol/permisos; redirige por defecto a `mappings`. |
| `/integraciones/mappings` | `MappingSetsPageComponent` | Matriz/listado/versiones de relación de campos. |
| `/integraciones/mappings/:methodCode/:mappingSetId` | `MappingEditorPageComponent` | Editor avanzado; historial y preview están embebidos, no son rutas propias. |
| `/integraciones/soap-settings` | `SoapIntegrationSettingsComponent` | Configuración técnica endpoint/SOAPAction/modo/estado. |
| `/soap-integrations` | redirect | Compatibilidad legacy hacia `/integraciones/soap-settings`. |

- `IntegrationWorkspaceComponent` ofrece navegación primaria entre “Matriz de campos SOAP” y “Configuración técnica SOAP”.
- Detalle y auditoría del listado se abren como dialogs manuales dentro de `MappingSetsPageComponent`; no existen rutas independientes de detalle/historial.
- `SoapIntegrationSettingsComponent` enlaza de vuelta a `/integraciones/mappings` con `queryParams.method`; `MappingEditorPageComponent` vuelve al listado mediante router.

### 14.2 Inventario de componentes y complejidad

| Pantalla | TS / HTML / SCSS aprox. | Dependencias / servicios | Responsabilidades actuales | Complejidad / deuda |
|---|---:|---|---|---|
| `IntegrationWorkspaceComponent` | 19 / 17 / 63 | Router, `SharedModule`, `MatButton`, `MatIcon` | Shell y navegación | Pequeño; mezcla tokens Material con 4 colores hex hardcodeados; navegación compacta usa scroll horizontal. |
| `MappingSetsPageComponent` | 1339 / 309 / 362 | `IntegrationMappingAdminService`, `AuthService`, `NotificationService`, Router/Route, Forms, CDR; Material button/card/checkbox/chips/form/input/menu/progress/select/sort/table/tabs/tooltip | Carga methods/mappings/catálogos; selección de servicio; filtros/sort; matriz/read model; etiquetas/copy; clasificación funcional; draft/edit; navegación editor; historial; 4 dialogs manuales | **Sobredimensionado**. Mezcla page/container, state, data access, presenter, reglas de clasificación, forms y dialogs. Tabla Material depende de `min-width:1120px` + `overflow-x:auto`. |
| `MappingEditorPageComponent` | 717 / 242 / 47 | `IntegrationMappingAdminService`, `AuthService`, `NotificationService`, Router/Route, Forms, CDR; Material button/card/checkbox/chips/expansion/form/input/progress/select/tooltip + controles `ui-*` | Orquesta carga; selecciona parámetro; edita regla; valida; preview; publica; clona; historial; copy/status y normalización | **Sobredimensionado**. Mezcla data/state/presentation/acciones sensibles. Layout desktop de 3 columnas; lista seleccionable es `<li (click)>`; 33 ocurrencias de colores hex en SCSS. |
| `SoapIntegrationSettingsComponent` | 447 / 176 / 234 | `SoapIntegrationSettingsService`, `AuthService`, `NotificationService`, Forms, CDR; Material button/card/chips/expansion/form/input/progress/select/slide-toggle/tabs/tooltip | Carga/hidrata settings; selecciona método; edita endpoint/SOAPAction/modo/estado; guarda/reload; copy por servicio | Mediano-alto. `OnPush` ya presente y estilos tokenizados; data/state/presentation siguen en un solo componente; configuración técnica continúa siendo una entrada de primer nivel del workspace. |

Servicios actuales:

- `IntegrationMappingAdminService` (~249 líneas): data access y DTOs de métodos, parámetros, fuentes, transformaciones, mapping sets, validación, preview, publish, clone e historial.
- `SoapIntegrationSettingsService` (~59 líneas): data access + clonación/ReplaySubject de settings de los dos clientes SOAP.
- No existe todavía feature store/facade/presenter dedicado para Integraciones; las páginas consumen directamente estos servicios.

Tests frontend localizados: `integrations-routing.module.spec.ts` (1 test de creación), `mapping-sets-page.component.spec.ts` (28), `mapping-editor-page.component.spec.ts` (11), `soap-integration-settings.component.spec.ts` (10). No existe spec dedicado de `IntegrationWorkspaceComponent`; no se ejecutaron en P0.3.

### 14.3 Baseline UX / Material / branding / responsive / accesibilidad

| Área | Estado actual | Refactor requerido |
|---|---|---|
| Lenguaje funcional | Predomina español, pero “SOAP”, operation keys (`Proc_*`), “Endpoint”, “SOAP Action”, “LIVE”, `parameterPath`, `mapping set` y rutas técnicas aparecen como contenido primario o visible. | Hacer primarios los nombres funcionales de los tres servicios y relegar nombres SOAP/códigos a detalle técnico colapsado. |
| Exceso de información / progressive disclosure | Matriz muestra 8 columnas técnicas/funcionales; editor presenta lista + formulario + centro de control/preview/historial simultáneamente; configuración SOAP expone toda la conexión técnica como pantalla primaria. | Dividir tareas, usar detalle técnico colapsado, drawer/dialog y read model orientado a usuario. |
| Comprensión de los tres servicios | Existen descripciones funcionales y `SERVICE_COPY`, pero el identificador técnico sigue encabezando tabs/cards y navegación. | Adoptar “Créditos recibidos en CFA”, “Débitos originados en CFA” y “Respuestas de transacciones” como etiquetas principales; operation key secundaria. |
| Estados vacío/carga/error | Hay progress bars, empty states y mensajes de error/retry en páginas principales; editor modela `loading/error/ready`. | Conservar patrón y uniformar componentes/copy; añadir live-region/foco donde corresponda. |
| Confirmaciones sensibles | `publish()` valida y publica directamente; guardar configuración `Live`/habilitada muestra advertencia visual pero no confirmación explícita. | `MatDialog` de confirmación para publicar y habilitar envío real, con resumen de impacto. |
| Angular Material | Uso amplio de cards, tabs, table, sort, menu, chips, form-field, select, checkbox, slide-toggle, progress y tooltip. | Migrar dialogs manuales a `MatDialog`; considerar `MatDrawer`/expansion para detalle; eliminar controles clickables sin semántica; converger controles `ui-*` cuando dupliquen Material. |
| Dialogs / overlays | `MappingSetsPageComponent` implementa backdrop + `aside role="dialog"` para detail/edit/draft/history. | `MatDialog`/CDK con focus trap, Escape, foco inicial y restauración de foco. |
| Branding dinámico | `BrandingService` actualiza `--brand-*` y `--mat-sys-primary/on-primary`; `_tokens.scss` expone `--color-*`; mapping-list y SOAP settings consumen `--mat-sys-*`. Workspace tiene 4 hex hardcodeados; editor 33 ocurrencias/23 valores únicos hardcodeados. Material theme configura `typography: Roboto` mientras el body usa `--font-sans` (`Inter`, `Segoe UI`, ...). | Eliminar hardcodes de feature, consumir tokens semánticos globales y alinear tipografía Material/global. |
| Responsive escritorio | Editor usa grid 24%/44%/32%; matriz y SOAP settings aprovechan ancho amplio. | Mantener jerarquía, pero evitar que el ancho sea requisito de operación. |
| Responsive tablet | Editor colapsa a 1 columna bajo 1100px; SOAP form/summary a 1 columna bajo 900px; matriz conserva tabla horizontal. | Sustituir tabla horizontal por lista/drawer o representación adaptativa de la tarea primaria. |
| Responsive móvil | Workspace hace nav con `overflow-x:auto`; matriz usa tabla `min-width:1120px` y resumen 6 columnas con scroll; dialogs manuales pasan a pantalla completa; editor sigue denso aunque apila; SOAP actions se apilan a <600px. | Cero scroll horizontal de la tarea primaria; acciones críticas siempre accesibles; lista/campo seleccionado como flujo secuencial en compacto. |
| Accesibilidad | Material aporta semántica a gran parte de formularios; dialogs manuales tienen `role/aria-modal/aria-labelledby` pero sin focus trap/restore explícitos. Editor usa `<li (click)>` sin rol/tabindex/teclado; tab label de matriz añade `(click)` a `span`. Estados suelen combinar texto + color. | Migrar a Material/CDK; controles semánticos; navegación teclado/foco visible; `aria-live` para operaciones; mantener texto además de color. |
| Arquitectura | Pages consumen data services directamente y contienen estado mutable, forms, copy/presenters, clasificación y side effects; no hay store/facade/adapters separados. | Page/container delgado + data-access + signal store + presenters/adapters + presentation components + dialogs. |
| Mapping UX | Matriz sirve de listado y editor ligero; editor avanzado expone source kind/path, prioridad, fixed/default, transform/mask, ids reales de preview y resultados técnicos simultáneamente. | Relación de información guiada por dato CFA, origen, tratamiento y preview; técnica avanzada bajo progressive disclosure. |
| Configuración SOAP | Endpoint/SOAP Action/modo/estado están bien separados del mapping a nivel de ruta/servicio, pero siguen visibles como navegación primaria y no tienen confirmación Live. | Mantener separación conceptual, mover conexión a sección avanzada/overview y confirmar cambios operativos. |

### 14.4 Clasificación de responsabilidades para refactor posterior

- **Page/container:** rutas, selección de servicio/configuración y coordinación de casos de uso; hoy vive dentro de los tres componentes grandes.
- **Presentation:** cards, lista de campos, resumen/estado, preview e historial; hoy embebido en templates y helpers de páginas.
- **State:** `loading`, error, selección, filtros, mapping actual, forms, validation/preview/history; hoy como propiedades mutables y getters en cada page.
- **Data access:** servicios `IntegrationMappingAdminService` y `SoapIntegrationSettingsService` ya concentran HTTP, pero no están encapsulados detrás de APIs feature/facades.
- **Adapters/presenters:** labels de source kind, transformaciones, estado, servicio y read-model de matriz están principalmente en `MappingSetsPageComponent`; deben salir del page.
- **Dialogs:** detail/edit/draft/history están embebidos manualmente en `MappingSetsPageComponent`; publish/Live carecen de dialog de confirmación.

P0.3 no implementa store/facade, no cambia rutas, no cambia frontend y no ejecuta validación runtime; esas acciones quedan para fases posteriores y P8 según el plan.

## 15. P0.4 — Cierre formal P0

### 15.1 Baseline consolidado

```yaml
baseline_mode: PACKAGE_SNAPSHOT
baseline_commit: UNKNOWN
snapshot_reproducibility: AVAILABLE
concurrent_change_detection: FILE_HASH
git_metadata_available: false
validation_execution: DEFERRED_TO_P8
p0_status: DONE
next_allowed_phase: P1
next_micro_prompt: P1.1
```

- La identidad primaria del baseline continúa siendo el ZIP fuente P0.1R registrado en la cabecera y su `snapshot_fingerprint` determinista; P0.4 verificó además el paquete de entrada P0.3 mediante `EVID-CFAUX-P0-SNAPSHOT-CHECK-005`.
- Git provenance y detección de concurrencia de working tree siguen `UNAVAILABLE`; no se reporta ni infiere commit Git.
- La ausencia de `.git` no bloquea trabajo local mientras se respete la comparación de hashes antes de modificar archivos críticos.

### 15.2 Cobertura de baseline alcanzada

- **SOAP execution map:** preservado por `EVID-CFAUX-P0-SOAP-001/002/003` para los tres servicios objetivo.
- **SOAP request behavior:** caracterización contractual implementada por `EVID-CFAUX-P0-SOAP-GOLDEN-TRANS-001`, `EVID-CFAUX-P0-SOAP-GOLDEN-CONTRA-001` y `EVID-CFAUX-P0-SOAP-GOLDEN-RESP-001`; ejecución runtime diferida a P8 por política.
- **SPA Integraciones:** navegación, componentes, deuda UX, Material, branding, responsive, accesibilidad y arquitectura congelados por `EVID-CFAUX-P0-SPA-BASELINE-001`.
- **Gaps:** `GAP-CFAUX-001` a `GAP-CFAUX-011` permanecen abiertos salvo que una fase posterior los cierre con evidencia.

### 15.3 Archivos protegidos para el refactor

- Los 30 archivos críticos inmutables enumerados en 13.2, excluyendo el `MEMORY_STATE.md` mutable, permanecen bajo comparación SHA-256 antes de cualquier modificación intencional.
- `tests/Cfa.ACHInterbank.Tests/CfaSoapRequestCharacterizationTests.cs` queda incorporado al conjunto de protección con SHA-256 `b8530e4f2985dda9581aec05d38a316818e2677f32de31f766fbe7cc17fd0146` hasta que una fase posterior lo modifique explícitamente.
- Un cambio inesperado en un archivo protegido bloquea el micro-prompt; un cambio intencional debe registrarse como archivo modificado y actualizar la evidencia correspondiente.

### 15.4 Validaciones diferidas

Toda validación ejecutable de P0 queda `DEFERRED_TO_P8` conforme a `IMPLEMENTATION_FIRST_VALIDATION_AT_END`. P0.4 no ejecutó build, tests, Playwright, Lighthouse, axe, Docker, migraciones runtime, UAT ni pruebas responsive runtime y no declara resultados no observados.

P0 queda `DONE` y habilita exclusivamente la entrada a P1 mediante `P1.1`; P1 no fue implementado en este turno.
## 16. P1.1 — Modelo de ContractVersion

### 16.1 Modelo y compatibilidad

- `IntegrationContractVersion` hereda `AuditableEntity`; conserva `CreatedAt/UpdatedAt`, incorpora `Revision`, metadata de importación/activación, referencia/fingerprint de fuente y contrato SOAP (`OperationName`, `TargetNamespace`, `SoapAction`).
- Estado y origen son tipados. `SourceType` se persiste como string y `Status` como int para mantener semántica explícita sin acoplar Application/Domain a Infrastructure.
- `IntegrationMethod.DisplayName` se reutiliza como equivalente existente de `FunctionalDisplayName`; no se duplicó el concepto. Se agregaron únicamente `FunctionalDescription`, `FunctionalCategory` y `RiskLevel`.
- `IntegrationMethodParameter.ContractVersionId` y `IntegrationMappingSet.ContractVersionId` permanecen nullable durante P1.1/P1.2 para backfill backward-compatible.
- `IntegrationMethodParameter` agrega `XmlElementName`, `XsdType`, `Nillable`, `MinOccurs`, `MaxOccurs` y `SupportStatus`; `IntegrationMappingSet` agrega `Revision`.
- Las nuevas FK hacia contrato usan `DeleteBehavior.Restrict`; no se modificaron mappers, clientes SOAP, dispatch, parsers, resolución funcional ni mappings runtime.
- El unique legacy `(MethodId, ParameterPath)` se conserva mientras `ContractVersionId` sea nullable; el índice `(ContractVersionId, ParameterPath)` existe como soporte de transición. El endurecimiento final de unicidad por contrato se difiere hasta que P1.2 complete backfill.

### 16.2 Validación y cierre

- Pruebas estáticas/modelo escritas en `IntegrationContractVersionModelTests.cs` para lifecycle/source tipados, audit fields/revision, índices, FK `Restrict`, metadata funcional, nulabilidad de `ContractVersionId` y metadata de parámetro.
- Validación ejecutable: `DEFERRED_TO_P8`.
- Migraciones: ninguna.
- `P1.1 = DONE`; P1 permanece `IN_PROGRESS`; siguiente micro-prompt permitido `P1.2`.

## 17. P1.2 — Migraciones dual-provider y backfill

### 17.1 Persistencia y backfill

- Se crean migraciones aditivas para PostgreSQL y SQL Server con `IntegrationContractVersions`, metadata P1.1, índices y FK `Restrict`.
- `ContractVersionId` continúa nullable en `IntegrationMethodParameters` y `IntegrationMappingSets`; no se ejecuta el endurecimiento de P1.3.
- Los tres métodos reciben `baseline-v1` con IDs deterministas y metadata SOAP exacta caracterizada en P0; `SourceType=BaselineMigration`, `Status=Active`, `Revision=1`.
- Parámetros existentes conservan `Id`/`MethodId` y reciben `ContractVersionId`, metadata XML derivada del catálogo actual y cardinalidad derivada de `Required`; `RegistrarRespuestaTransaccion` conserva exactamente sus siete parámetros actuales.
- Mapping sets existentes conservan `Id`, `Version`, reglas e historia y reciben `ContractVersionId`; `Revision` queda al menos en 1. No se toca `IntegrationMappingRule`.
- Compatibilidad clean-start: los bootstrappers existentes completan el vínculo baseline únicamente cuando la versión migrada existe, cubriendo parámetros/mapping sets creados después de una migración limpia sin alterar resolución/serialización SOAP.
- `Up` no elimina columnas, tablas, mappings ni historia. `Down` rechaza rollback si ya existen contract versions no-baseline antes de retirar las estructuras aditivas.

### 17.2 Validación y cierre

- Inspección estática: migraciones dual-provider presentes; snapshots y designers consistentes; `ContractVersionId` nullable; SOAP mappers/clientes sin cambio de hash frente al paquete P1.1.
- Pruebas escritas: `IntegrationContractVersionMigrationTests.cs` y caso de bootstrap en `IntegrationBootstrapperTests.cs`.
- Validación ejecutable: `DEFERRED_TO_P8`; no se ejecutaron `dotnet ef`, build, tests, PostgreSQL/SQL Server, Docker ni UAT.
- `P1.2 = DONE`; P1 permanece `IN_PROGRESS`; siguiente micro-prompt permitido `P1.3`.

## 18. P1.3 — Queries, constraints e inmutabilidad contractual

### 18.1 Resolución y lifecycle

- Se incorpora `IIntegrationContractVersionService` / `IntegrationContractVersionService` para consultar versiones por método/Id, resolver explícitamente un `ContractVersionId` o la única versión `Active`, consultar parámetros por contrato y ejecutar transiciones controladas.
- Lifecycle permitido: `Draft -> Validated -> Active -> Retired`; no se permiten regresiones. Activar una versión `Validated` retira la versión `Active` previa del mismo método en la misma unidad de trabajo y preserva ambas filas como historia.
- `Revision` de `IntegrationContractVersion` es concurrency token y avanza exactamente una unidad por cambio persistido; el servicio exige `expectedRevision` en transiciones.
- `Active` es inmutable salvo la transición a `Retired`; `Retired` es completamente inmutable. Las versiones contractuales persistidas no se eliminan.
- Los `IntegrationMethodParameter` asociados a contratos `Active`/`Retired` son inmutables. Se conserva únicamente el puente de compatibilidad para asociar una fila legacy aún no versionada al `baseline-v1` de `BaselineMigration`.

### 18.2 Constraints y compatibilidad legacy

- Se agregan migraciones P1.3 para PostgreSQL y SQL Server sin borrar tablas/columnas ni datos.
- Se exige una sola versión contractual `Active` por `MethodId` mediante índice único filtrado `UX_IntegrationContractVersions_Method_Active` y guard de dominio/persistencia.
- La unicidad final de parámetros es `(ContractVersionId, ParameterPath)` para filas versionadas; las filas legacy con `ContractVersionId IS NULL` conservan unicidad `(MethodId, ParameterPath)` mediante índice filtrado separado. `ContractVersionId` permanece nullable como puente legacy; no hay cutover.
- Los `IntegrationMappingSet` versionados con `Version > 0` son únicos por `(ContractVersionId, Version)`. `Revision` es concurrency token y se incrementa en cambios del agregado; las actualizaciones de reglas incrementan explícitamente la revisión del set.
- Los `Down` de P1.3 rechazan downgrade cuando la historia contractual ya contiene paths repetidos entre versiones y el esquema P1.2 no podría representarlos sin pérdida/colisión.

### 18.3 Bootstrap y mapping admin contract-aware

- `IntegrationCatalogBootstrapper` crea en clean-start los tres `baseline-v1` deterministas cuando no existen, sin WSDL, conservando los IDs/acciones SOAP fijados en P1.2; los parámetros nuevos se asocian al baseline desde el inicio.
- `IntegrationCatalogService.GetMethodParametersAsync` resuelve por el contrato `Active` y conserva fallback a filas legacy no versionadas.
- `IntegrationMappingSetService` añade queries por `ContractVersion`, asigna el contrato `Active` a drafts nuevos, valida reglas contra parámetros del mismo contrato, publica/versiona dentro del contrato y conserva `ContractVersionId` al clonar.
- Validación, preview y snapshot de mapping filtran parámetros por el `ContractVersionId` del set con fallback legacy; no se modificaron mappers SOAP, clientes SOAP, dispatch ni serialización runtime.
- `RegistrarRespuestaTransaccion` conserva exactamente, y en el mismo orden, sus siete parámetros: `idCanal`, `nombreCanal`, `idTransaccion`, `idEstado`, `causal`, `idTransaccionAxon`, `descripcionCausal`.

### 18.4 Pruebas y cierre

- Pruebas focales escritas en `IntegrationContractVersionLifecycleTests.cs` para resolución active, siete parámetros exactos, activación/retiro, only-one-active e inmutabilidad de contrato/parámetros.
- Pruebas de modelo actualizadas para concurrency/indexes P1.3 y pruebas estáticas dual-provider agregadas en `IntegrationContractVersionConstraintMigrationTests.cs`.
- Pruebas no ejecutadas. Build, migraciones runtime, PostgreSQL/SQL Server, Docker, SOAP runtime y UAT: `DEFERRED_TO_P8` por `IMPLEMENTATION_FIRST_VALIDATION_AT_END`.
- No se introdujo WSDL, cutover ni eliminación de legacy.
- `P1.3 = DONE`; P1 permanece `IN_PROGRESS`; `next_micro_prompt = P1.4`.

## 19. P1.4 — Cierre formal P1

### 19.1 Confirmación por inspección

- `IntegrationContractVersion` existe, pertenece a `IntegrationMethod.ContractVersions` y mantiene relaciones `Restrict` hacia parámetros y mapping sets; `Revision` es concurrency token.
- Las migraciones/backfill dual-provider P1.2 existen para PostgreSQL y SQL Server, preservan filas/IDs/historia y crean `baseline-v1` determinista para los tres servicios SOAP objetivo.
- P1.3 aporta queries/resolución contractual por Id/Active, consulta de parámetros por contrato, lifecycle `Draft -> Validated -> Active -> Retired`, only-one-active por método, inmutabilidad Active/Retired y rechazo de delete contractual.
- Los snapshots PostgreSQL/SQL Server contienen la unicidad filtrada de Active, parámetros versionados/legacy y MappingSet por ContractVersion; el puente legacy nullable permanece intencional y no existe cutover.
- La activación retira la versión Active previa y conserva ambas filas; los `Down` de constraints rechazan downgrade cuando la historia contractual ya no puede representarse sin colisión.
- Los siete archivos críticos del runtime SOAP legacy continúan byte-identical al baseline P0 por SHA-256; P1 no cambió XML, mappers físicos, clientes ni dispatch SOAP.

### 19.2 Validación y estado

- No se implementó funcionalidad nueva en P1.4.
- No se ejecutaron build, tests, migraciones, Docker ni UAT. Toda validación ejecutable permanece `DEFERRED_TO_P8` por `IMPLEMENTATION_FIRST_VALIDATION_AT_END`.
- `P1 = DONE`.
- `P1.4 = DONE`.
- `last_completed_micro_prompt = P1.4`.
- `next_allowed_phase = P2`.
- `next_micro_prompt = P2.1`.
- P2.1 ejecutado y cerrado en este turno; P2.2 no fue ejecutado.

## 20. P2.1 — Contratos del motor genérico

### 20.1 Contratos, modelos y extensibilidad

- Se agregan `IIntegrationExecutionContractResolver` y `IIntegrationRequestResolver` en Application con las firmas contract-driven definidas para resolver contrato activo por `MethodCode` y producir `ResolvedIntegrationRequest` desde `IntegrationExecutionContext`.
- Se agregan modelos inmutables/snapshot para `IntegrationExecutionContext`, `IntegrationExecutionContract`, bindings de parámetros, referencias de fuente, parámetros resueltos y `ResolvedIntegrationRequest`; el contrato de ejecución conserva `ContractVersionId`/`Revision` y `MappingSetId`/versión/revisión para resolución determinista y trazable.
- Se agregan `IIntegrationSourceValueProvider` / `IIntegrationSourceProviderRegistry` y `IIntegrationTransformation` / `IIntegrationTransformationRegistry`, más descriptors tipados de sources y transformations. Los registries son lookup-based, validan duplicados al construirse y fallan rápido; no incorporan switches por servicio/campo.
- Los source providers se seleccionan por `(SourceKind, SourceKey)` explícito; el motor no inspecciona paths mediante reflection. `IntegrationSourceKindEnum.Expression` se rechaza explícitamente en los contratos nuevos para impedir expresiones arbitrarias.
- La extensibilidad queda por Dependency Inversion/Strategy/Registry: una fuente o transformación nueva se agrega mediante una implementación registrada, sin modificar resolver/writer ni agregar ramas por servicio.
- P2.1 no registra los nuevos componentes en DI, no implementa transporte/writer, no conecta resolvers al runtime, no hace cutover y no modifica clientes SOAP, mappers físicos, parsers ni dispatch legacy.

### 20.2 Pruebas y cierre

- Se agrega `IntegrationGenericEngineContractTests.cs` con invariantes de snapshot/orden determinista, paths de parámetro únicos, rechazo de expresiones arbitrarias, fail-fast de providers/transformations duplicados y resolución de strategies por registry.
- Las pruebas fueron escritas y no ejecutadas. Build, tests, Docker, SOAP runtime y UAT permanecen `DEFERRED_TO_P8` por `IMPLEMENTATION_FIRST_VALIDATION_AT_END`.
- `P2.1 = DONE`.
- `P2 = IN_PROGRESS`.
- `last_completed_micro_prompt = P2.1`.
- `next_micro_prompt = P2.2`.
- P2.2 no fue ejecutado en este turno.

## 21. P2.2 — Source providers y transformaciones

### 21.1 Implementación y extensibilidad

- Se implementa resolución de valor de parámetro mediante `IntegrationParameterValueResolver`, que compone `IIntegrationSourceProviderRegistry` + `IIntegrationTransformationRegistry`, aplica fixed/constant, fallback `DefaultValue`, nullability, transformación registrada y validación de representabilidad del tipo destino sin reflection ni expresiones arbitrarias.
- `IntegrationSourceProviderDescriptor` incorpora snapshot inmutable de `SupportedMethodCodes`; `IntegrationSourceProviderRegistry` normaliza `SourceKey` case-insensitive, conserva fail-fast de duplicados y agrega detección explícita `SOURCE_METHOD_INCOMPATIBLE`.
- Se agrega `ConstantIntegrationSourceValueProvider` y `CurrentIntegrationSourceProviderFactory` con strategies declarativas únicamente para las fuentes actualmente usadas por los mappings publicados de `WSCFAACH.Proc_Contrapartidas`, `WSCFAACH.Proc_Transacciones` y `WSAXON.RegistrarRespuestaTransaccion`. Los providers cargan datos autorizados desde referencias de `IntegrationExecutionContext`; no se pasan entidades/DbContext a Application y no se usa reflection de paths.
- Las fuentes de `Proc_Transacciones` preservan las resoluciones directas hoy requeridas para transacción, instituciones, EntryDetail, lote funcional, información de pago y cámara; las de respuesta diferencial leen el intento de notificación asociado a `DifferentialResponseId`; `Proc_Contrapartidas` cubre transacción/lote/cámara y fixed constants.
- Se implementa el catálogo de strategies existentes: `Trim`, `Uppercase`, `Lowercase`, `PadLeft`, `PadRight`, `Substring`, `Concat`, `DateFormat`, `NumericFormat`, `NullIfEmpty` y `DefaultIfNull`, con descriptors de compatibilidad y máscaras; nuevas transformaciones se agregan como `IIntegrationTransformation`, no como ramas de servicio/campo.
- P2.2 no registra cutover, no conecta el resolver al runtime SOAP, no modifica clientes SOAP, parsers, XML productivo, dispatch ni mappers legacy.

### 21.2 Pruebas y cierre

- Se agrega `IntegrationGenericEngineSourceResolutionTests.cs` para fixed, default, source, null opcional/obligatorio, transformación, tipo incompatible, source/method incompatible, duplicado case-insensitive y catálogo completo de transformaciones.
- Las pruebas fueron escritas y no ejecutadas; build/tests/Docker/SOAP runtime/UAT permanecen `DEFERRED_TO_P8` por `IMPLEMENTATION_FIRST_VALIDATION_AT_END`.
- `P2.2 = DONE`.
- `P2 = IN_PROGRESS`.
- `last_completed_micro_prompt = P2.2`.
- `next_micro_prompt = P2.3`.
- P2.3 no fue ejecutado en este turno.


## 22. P2.3 — SOAP body writer contract-driven

### 22.1 Writer/serializer genérico

- Se agrega `ISoapRequestBodyWriter` y `ContractDrivenSoapBodyWriter` en Application; el writer recibe únicamente `ResolvedIntegrationRequest`, usa el snapshot `IntegrationExecutionContract` asociado y construye `Envelope`/`Body` SOAP 1.1 mediante `System.Xml.Linq`, sin concatenación insegura de XML.
- `OperationName` y `TargetNamespace` provienen exclusivamente del contrato. Los parámetros se recorren por `SortOrder` + `ParameterPath`, se serializan solo cuando su `Direction` es `Input` y nunca se generan elementos fuera del catálogo contractual.
- `IntegrationExecutionParameterBinding` incorpora `Direction`; `ResolvedIntegrationParameter` conserva `Direction`, `MinOccurs`, `MaxOccurs`, `Required` e `IsPresent` para representar de forma explícita los estados omitido, null y vacío sin reflection ni expresiones arbitrarias. `ResolvedIntegrationRequest` queda ligado al snapshot inmutable de `IntegrationExecutionContract`.
- El writer rechaza parámetros ajenos al contrato, metadata resuelta distinta de la contractual, parámetros de salida presentes en el request, omisión de parámetros obligatorios, null no permitido y cardinalidad no escalar.
- Null nillable se emite con `xsi:nil="true"`; un parámetro string opcional no nillable presente con null se emite como elemento vacío para conservar el comportamiento legacy de `RegistrarRespuestaTransaccion`; la omisión se expresa con `IsPresent=false`. Valores string preformateados se preservan literalmente y valores CLR escalares se normalizan con representación XML/invariant culture.
- No se agregan ramas por servicio/campo. La resolución de sources/transformations continúa usando Strategy + Registry de P2.1/P2.2; el writer queda detrás de `ISoapRequestBodyWriter` y usa un XML builder seguro.

### 22.2 Paridad y alcance

- Se agregan pruebas `IntegrationContractDrivenSoapBodyWriterTests.cs` para SOAP 1.1, operation/namespace contractuales, orden determinista, escaping, input-only, omisión/null/nillable/vacío, normalización escalar, rechazo de parámetros ajenos y mismatch de metadata.
- Se escriben pruebas de paridad/golden contra los builders legacy existentes de los tres servicios: envelope de `Proc_Transacciones`, operación de `Proc_Contrapartidas` y los siete parámetros ordenados de `RegistrarRespuestaTransaccion`, incluyendo null legacy de `causal` como elemento vacío.
- Los siete archivos críticos del runtime SOAP legacy (`ProcTransaccionesRequestMapper`, `ProcContrapartidasRequestMapper`, `ProcContrapartidasFunctionalMappingResolver`, `RegistrarRespuestaAchSoapRequestMapper`, `SoapParameterMapper`, `WscfaachSoapClient`, `WsAxonRespuestaTransaccionesSoapClient`) conservaron exactamente sus SHA-256 antes/después de P2.3. No hubo cutover, registro DI, cambio de clientes, parsers, dispatch ni XML productivo.
- Para resolver la firma del writer se consultó únicamente la sección `Runtime`/`Builder` puntual de `BACKEND_SOLID_DESIGN.md`, que define `ISoapRequestBodyWriter.Write(ResolvedIntegrationRequest)`.

### 22.3 Validación y cierre

- Las pruebas fueron escritas y NO ejecutadas. No se ejecutaron build, tests, Docker, SOAP runtime ni UAT; toda validación permanece `DEFERRED_TO_P8` por `IMPLEMENTATION_FIRST_VALIDATION_AT_END`.
- `P2.3 = DONE`.
- `P2 = IN_PROGRESS`.
- `last_completed_micro_prompt = P2.3`.
- `next_micro_prompt = P2.4`.
- P2.4 no fue ejecutado en este turno.

## 23. P2.4 — Cierre formal P2

### 23.1 Inspección de cierre

- Se inspeccionaron únicamente `AGENTS.md`, `MEMORY_STATE.md`, el bloque `P2.4` y símbolos/archivos directamente creados o modificados en P2.1–P2.3; no se recorrió funcionalmente el repositorio completo.
- Existen y son coherentes los contratos/modelos del motor genérico, registries de sources y transformations, providers de fuentes actuales, resolución genérica de parámetros y writer SOAP contract-driven.
- `CurrentIntegrationSourceProviderFactory` declara cobertura explícita para `WSCFAACH.Proc_Contrapartidas`, `WSCFAACH.Proc_Transacciones` y `WSAXON.RegistrarRespuestaTransaccion`; el catálogo estándar conserva las strategies `Trim`, `Uppercase`, `Lowercase`, `PadLeft`, `PadRight`, `Substring`, `Concat`, `DateFormat`, `NumericFormat`, `NullIfEmpty` y `DefaultIfNull`.
- `IntegrationContractDrivenSoapBodyWriterTests` contiene paridad contra los builders legacy de `Proc_Transacciones`, `Proc_Contrapartidas` y `RegistrarRespuestaTransaccion`; las pruebas de contratos y source resolution también existen. Ninguna prueba fue ejecutada.
- La búsqueda por símbolos nuevos mostró que `ContractDrivenSoapBodyWriter`, `IntegrationParameterValueResolver`, `IntegrationSourceProviderRegistry`, `IntegrationTransformationRegistry` y `CurrentIntegrationSourceProviderFactory` permanecen fuera de composición/runtime productivo; no existe cutover.
- Los siete archivos críticos del runtime SOAP legacy coinciden exactamente con los hashes P0 registrados en 13.2. No se modificaron clientes SOAP, parsers, dispatch ni XML productivo.
- El texto base de P2.4 menciona feature flags y ejecución de tests/build; la instrucción explícita vigente para este micro-prompt ordena no implementar funcionalidad nueva, no ejecutar build/tests/DB/Docker/UAT y mantener validación `DEFERRED_TO_P8`. El cierre se realiza bajo ese alcance superior y queda documentado sin afirmar ejecución inexistente.

### 23.2 Estado final

- `P2 = DONE`.
- `P2.4 = DONE`.
- `last_completed_micro_prompt = P2.4`.
- `next_allowed_phase = P3`.
- `next_micro_prompt = P3.1`.
- `validation_execution = DEFERRED_TO_P8`.
- P3.1 no fue ejecutado en este turno.

## 24. P3.1 — `Proc_Transacciones` detrás del motor contract-driven

### 24.1 Implementación

- `ProcTransaccionesRequestMapper` conserva el camino legacy por defecto y selecciona el motor contract-driven únicamente cuando `ProcTransacciones:UseContractDrivenRequest=true`; el flag nuevo tiene default `false`, por lo que no se activó cutover sin validación ejecutada.
- El camino nuevo construye `IntegrationExecutionContext`, resuelve el contrato/mapping publicado mediante `IIntegrationRequestResolver`, adapta la semántica legacy con `ProcTransaccionesRequestParityPolicy` y serializa exclusivamente con `ISoapRequestBodyWriter`. El pipeline, parser y transporte SOAP no fueron modificados.
- Se implementan `IntegrationExecutionContractResolver` e `IntegrationRequestResolver` para materializar el snapshot activo de contrato + mapping y resolver solo parámetros input mediante los Strategy/Registry de P2. Los parámetros opcionales sin mapping quedan explícitamente no mapeados; expresiones/condiciones arbitrarias continúan rechazadas.
- `ProcTransaccionesFunctionalSourceResolver` extrae la derivación/validación funcional de instituciones, transit codes, evidencia NACHA-M, cámara canónica, addenda 05, lote funcional e información de pago. El resolver se usa tanto por el camino legacy como por los source providers del motor nuevo y conserva los códigos/guardas funcionales previos.
- `CurrentIntegrationSourceProviderFactory` delega las fuentes derivadas de `Proc_Transacciones` a ese resolver; no se agregaron switches por servicio/campo al motor genérico. La composición DI registra contract resolver, request resolver, source/transformation registries, value resolver, writer y la strategy funcional.
- `IntegrationExecutionParameterBinding` incorpora `IsMapped` para distinguir un parámetro contractual opcional sin regla publicada de un parámetro mapeado cuyo valor resulte null/vacío, evitando inventar elementos XML.
- `ProcTransaccionesRequestParityPolicy` conserva la semántica legacy relevante de request: valores requeridos vacíos fallan, placeholders opcionales no forman parte del request legado, null/vacío/whitespace no se serializan y `ILR`/`METODO` permanecen omitidos; orden/nombre/namespace continúan dirigidos por el contrato activo.
- No se ejecutó ni se inició trabajo de `P3.2`; `Proc_Contrapartidas` y `RegistrarRespuestaTransaccion` conservan sus rutas runtime actuales.

### 24.2 Pruebas y validación

- `NachaDesagregadoIntegrationMappingTests` agrega una prueba de paridad que construye el mismo caso persistido con mapper legacy y con `UseContractDrivenRequest=true`, exige la misma identidad de MappingSet/snapshot, los mismos parámetros resueltos y equivalencia semántica ordenada del SOAP request. El test legacy existente también fija que, con el default del flag, no existe `ContractDrivenRequest`.
- `IntegrationContractDrivenSoapBodyWriterTests` agrega cobertura del adapter de paridad contra el golden P0 de `Proc_Transacciones`: omisión de null/vacío/whitespace y `ILR`/`METODO`, preservación de orden y escaping XML.
- No se ejecutaron build, tests, DB, migraciones, Docker, Playwright, SOAP runtime ni UAT. `validation_execution = DEFERRED_TO_P8` bajo `IMPLEMENTATION_FIRST_VALIDATION_AT_END`; la ausencia de ejecución no activa `BLOCKED`.
- Inspección estática dirigida confirmó que los cambios de runtime se limitan a `Proc_Transacciones` y su composición/dependencias directas; no se modificaron `WscfaachSoapClient`, parsers de respuesta ni transporte.
- Evidencia de implementación registrada `2026-10-13T01:46:37Z`.

### 24.3 Estado

- `P3.1 = DONE`.
- `P3 = IN_PROGRESS`.
- `last_completed_micro_prompt = P3.1`.
- `next_micro_prompt = P3.2`.
- `validation_execution = DEFERRED_TO_P8`.
- P3.2 no fue ejecutado en este turno.

## 25. P3.2 — Paridad `Proc_Contrapartidas`

### 25.1 Implementación

- `ProcContrapartidasRequestMapper` conserva exactamente el camino legacy por defecto y activa la resolución/serialización contract-driven solo cuando `ProcContrapartidas:UseContractDrivenRequest=true`; el flag nuevo tiene default `false`.
- El camino contract-driven ejecuta primero `IProcContrapartidasFunctionalMappingResolver` como compuerta de compatibilidad, preservando sus validaciones funcionales, de mapping y conversiones actuales; luego construye `IntegrationExecutionContext`, resuelve el mismo contrato/mapping publicado mediante `IIntegrationRequestResolver` y serializa con `ISoapRequestBodyWriter`.
- `ProcContrapartidasRequestParityPolicy` implementa un adapter explícito basado en registry para congelar la paridad de los 22 parámetros del request tipado actual. Compara los valores ya resueltos por el motor común y repone únicamente los cinco placeholders opcionales `ANS*` que el baseline legacy siempre serializa aunque no tengan regla activa. Campos futuros desconocidos para el baseline pasan por el request contract-driven sin agregar casos al writer/cliente.
- La identidad del MappingSet/version se contrasta entre el validador legacy y el motor genérico antes de construir el request. Namespace, operation name, orden y `SoapAction` permanecen dirigidos por la ContractVersion activa; el cliente SOAP, parser y transporte no fueron modificados.
- La composición DI registra `ProcContrapartidasRequestParityPolicy`; se reutilizan sin ampliar switches los source providers/registries y transformaciones ya creados en P1/P2/P3.1.
- No se ejecutó ni inició trabajo de `P3.3`; `RegistrarRespuestaTransaccion` conserva su runtime actual.

### 25.2 Pruebas y validación

- `IntegrationMappingEndToEndTests` agrega una prueba de paridad legacy/contract-driven para `Proc_Contrapartidas`: exige misma identidad de MappingSet/version/snapshot, metadata contractual `WSCFAACH.Proc_Contrapartidas`/`Proc_Contrapartidas`/`http://tempuri.org/`/`http://tempuri.org/IWSCFAACH/Proc_Contrapartidas` y equivalencia exacta de los 22 elementos SOAP en nombre, orden y valor.
- Se agrega cobertura que activa el flag y verifica que una fuente requerida ausente (`OFIDTX`) conserva el error funcional legacy `INTEGRATION_MAPPING_SOURCE_REQUIRED`.
- El golden legacy existente continúa usando el constructor sin opciones; por default `UseContractDrivenRequest=false`, por lo que el XML tipado original sigue siendo la ruta de compatibilidad.
- No se ejecutaron build, tests, DB, migraciones, Docker, Playwright, SOAP runtime ni UAT. `validation_execution = DEFERRED_TO_P8` bajo `IMPLEMENTATION_FIRST_VALIDATION_AT_END`; la ausencia de ejecución no activa `BLOCKED`.
- Inspección estática dirigida confirmó que `BuildLegacySoapBody` conserva los mismos 22 elementos, valores, namespace y orden del mapper previo; `WscfaachSoapClient` no fue modificado.
- `EVID-CFAUX-P3-CONTRAPARTIDAS-001`: implementación y pruebas de P3.2 cerradas por inspección estática dirigida el `2026-10-13T02:04:43Z`; validación ejecutable diferida a P8.

### 25.3 Estado

- `P3.2 = DONE`.
- `P3 = IN_PROGRESS`.
- `last_completed_micro_prompt = P3.2`.
- `next_micro_prompt = P3.3`.
- `validation_execution = DEFERRED_TO_P8`.
- P3.3 no fue ejecutado en este turno.

## 26. P3.3 — Paridad `RegistrarRespuestaTransaccion`

### 26.1 Implementación

- `RespuestaTransaccionesAchGateway` conserva el mapper físico legacy como ruta por defecto y activa el motor contract-driven únicamente con `RegistrarRespuestaTransaccion:UseContractDrivenRequest=true`; `RegistrarRespuestaDispatchOptions.UseContractDrivenRequest` tiene default `false`, sin cutover global.
- `RegistrarRespuestaAchCommand` incorpora únicamente la identidad interna `DifferentialResponseId` necesaria para resolver el provider específico de respuesta diferencial. `RegistrarRespuestaAchCommandMapper` la deriva de `AchResponse.Id`; el `RegistrarRespuestaAchCommandValidator` permanece sin cambios.
- El camino contract-driven construye `IntegrationExecutionContext` con `MethodCode=WSAXON.RegistrarRespuestaTransaccion` y `DifferentialResponseId`, reutiliza `IIntegrationRequestResolver`/Strategy/Registry de P1/P2 y por tanto resuelve los siete sources `differentialResponse.*` registrados por `CurrentIntegrationSourceProviderFactory` para la notificación diferencial vigente.
- `RegistrarRespuestaRequestParityPolicy` congela el baseline v1: ContractVersion determinista `4a093eb5-beed-5b15-bc7c-5768c0e0547e`, operación/namespace/SoapAction caracterizados y exactamente siete inputs en el orden `idCanal`, `nombreCanal`, `idTransaccion`, `idEstado`, `causal`, `idTransaccionAxon`, `descripcionCausal`. Además compara cada valor resuelto con el command ya validado para fallar cerrado ante divergencias.
- Los dos campos opcionales `causal` y `descripcionCausal` continúan presentes como elementos vacíos cuando su valor es null, preservando la semántica legacy caracterizada.
- `ISoapRequestBodyWriter` sigue siendo el serializer autoritativo del camino nuevo. `RegistrarRespuestaSoapOperationAdapter` extrae la única operación del envelope producido por el writer porque `WsAxonRespuestaTransaccionesSoapClient` conserva su contrato legacy de recibir el XML de operación y construir el SOAP 1.1 Envelope internamente; el cliente/transporte no se modificó.
- `RegistrarRespuestaAchSoapResponseParser` permanece sin cambios; P3.3 no modifica semántica de respuesta ni parser WSAXON.
- No se ejecutó ni inició trabajo de `P3.4`.

### 26.2 Pruebas y validación

- `RespuestaTransaccionesAchGatewayTests` conserva la caracterización del camino legacy por default y agrega cobertura del flag contract-driven: contexto con `DifferentialResponseId`, operación/namespace y los siete elementos exactos en orden/valor, presencia vacía de opcionales null, rechazo de contratos v1 con cantidad distinta de siete campos y rechazo temprano cuando falta la identidad de respuesta diferencial.
- `NotificarRespuestaAchUseCaseTests` verifica que `RegistrarRespuestaAchCommandMapper` propaga `AchResponse.Id` hacia `DifferentialResponseId`, conectando el provider diferencial sin exponer nombres físicos SOAP en Application.
- No se ejecutaron build, tests, DB, migraciones, Docker, Playwright, SOAP runtime ni UAT. `validation_execution = DEFERRED_TO_P8` bajo `IMPLEMENTATION_FIRST_VALIDATION_AT_END`; la ausencia de ejecución no activa `BLOCKED`.
- Inspección estática dirigida confirmó que `RegistrarRespuestaAchCommandValidator`, `RegistrarRespuestaAchSoapResponseParser` y `WsAxonRespuestaTransaccionesSoapClient` permanecen byte-identical a su baseline previo; no se hizo cutover global ni cambios de transporte.
- `EVID-CFAUX-P3-REGISTRAR-001`: implementación y pruebas de P3.3 cerradas por inspección estática dirigida el `2026-10-13T02:31:30Z`; validación ejecutable diferida a P8.

### 26.3 Estado

- `P3.3 = DONE`.
- `P3 = IN_PROGRESS`.
- `last_completed_micro_prompt = P3.3`.
- `next_micro_prompt = P3.4`.
- `validation_execution = DEFERRED_TO_P8`.
- P3.4 no fue ejecutado en este turno.

## 27. P3.4 — Cierre formal de P3

### 27.1 Inspección dirigida

- `Proc_Transacciones`: `ProcTransaccionesRequestMapper` conserva selección explícita entre `ResolveContractDrivenAsync` y `ResolveLegacyAsync` mediante `ProcTransaccionesDispatchOptions.UseContractDrivenRequest`; `BuildSoapBody` conserva el writer contract-driven detrás del mismo flag y `BuildLegacySoapBody` como ruta legacy.
- `Proc_Contrapartidas`: `ProcContrapartidasRequestMapper` conserva selección explícita entre `ResolveContractDrivenAsync` y `ResolveLegacyAsync` mediante `ProcContrapartidasDispatchOptions.UseContractDrivenRequest`; el camino contract-driven mantiene la compuerta funcional legacy y `BuildLegacySoapBody` permanece disponible.
- `RegistrarRespuestaTransaccion`: `RespuestaTransaccionesAchGateway` selecciona el camino contract-driven solo con `RegistrarRespuestaDispatchOptions.UseContractDrivenRequest=true`; cuando el flag está desactivado continúa usando `RegistrarRespuestaAchSoapRequestMapper` y el cliente SOAP existente.
- Los tres `UseContractDrivenRequest` son `bool` sin inicialización a `true`, por lo que su default de runtime permanece `false`. La inspección dirigida de `appsettings*.json` no encontró overrides explícitos de estos flags; no existe cutover global.
- `WscfaachSoapClient` conserva SHA-256 `c7cf6bc000fe4c5fbebfc60351aa6f25e3085ee1dd3ae364cd0536c919f98f72`, idéntico entre los snapshots dirigidos P3.2/P3.3. `RegistrarRespuestaAchSoapResponseParser` conserva SHA-256 `a7860c3015940125f06e2f9e83994873550f05af545e7354d1db968c7a979091`, coincidente con el baseline registrado en P3.3. El manifiesto P3.3 mantiene `WsAxonRespuestaTransaccionesSoapClient` como archivo crítico sin cambios con SHA-256 `d8f0844a3ff904683ec357d4a51f93c642eb136686dce50a7d07deccc011657d`.
- No se modificaron parsers de respuesta ni transporte SOAP en P3.4; P3.4 no introduce funcionalidad nueva ni cambia composición/cutover.

### 27.2 Pruebas y validación

- `Proc_Transacciones`: permanecen escritas las pruebas de paridad legacy/contract-driven en `NachaDesagregadoIntegrationMappingTests` y la caracterización de semántica de omisión/escaping en `IntegrationContractDrivenSoapBodyWriterTests`.
- `Proc_Contrapartidas`: permanece escrita la prueba de paridad exacta de 22 elementos y la preservación de la validación requerida `OFIDTX` en `IntegrationMappingEndToEndTests`.
- `RegistrarRespuestaTransaccion`: permanecen escritas las pruebas del flag contract-driven, baseline de siete campos, opcionales vacíos, identidad diferencial y propagación de `DifferentialResponseId` en `RespuestaTransaccionesAchGatewayTests` y `NotificarRespuestaAchUseCaseTests`.
- No se ejecutaron build, tests, DB, migraciones, Docker, Playwright, SOAP runtime ni UAT. `validation_execution = DEFERRED_TO_P8` permanece vigente bajo `IMPLEMENTATION_FIRST_VALIDATION_AT_END`.
- `EVID-CFAUX-P3-CLOSE-001`: cierre de P3 confirmado por inspección estática dirigida el `2026-10-13T02:37:42Z`; los tres servicios tienen ruta contract-driven detrás de flags seguros por defecto, legacy permanece disponible, no existe cutover global y la validación ejecutable queda diferida a P8.

### 27.3 Estado

- `P3.4 = DONE`.
- `P3 = DONE`.
- `last_completed_micro_prompt = P3.4`.
- `next_allowed_phase = P4`.
- `next_micro_prompt = P4.1`.
- `validation_execution = DEFERRED_TO_P8`.
- P4.1 no fue ejecutado en este turno.
## 28. P4.1 — Servicios de versiones y diff

### 28.1 Implementación

- `IIntegrationContractVersionService` / `IntegrationContractVersionService` amplían P1 con queries/commands para crear una versión `ManualApproved` en `Draft`, clonar/versionar una versión existente sin mutarla, consultar detalle con parámetros, actualizar metadata funcional de presentación y comparar dos versiones del mismo método. No se agregó importación WSDL/XSD ni se ejecutó P4.2.
- La clonación copia metadata técnica y parámetros a una nueva `ContractVersionId`, preserva el origen histórico y mantiene el bridge `baseline-v1` para leer filas legacy nullable sin propagarlo al clon. La nueva versión queda `Draft`, `Revision=1`, con `ImportedAtUtc`/`ImportedBy`; no hay activación ni cutover.
- La edición de presentación modifica únicamente `DisplayName`, `DescriptionEs`, `UiHelpText`, `Category` y `ExampleValue`; rechaza `Active`/`Retired`, exige `ExpectedRevision` del contrato y avanza la revisión agregada exactamente una unidad. Nombres XML, tipos, cardinalidad, orden, dirección y demás metadata técnica permanecen intactos.
- El diff contractual implementa `Added`, `Removed`, `RequiredChanged`, `TypeChanged`, `CardinalityChanged`, `OrderChanged`, `NamespaceChanged` y `DirectionChanged`. Cambios representables por el motor escalar/input se clasifican `MappingRequired`; cardinalidad no escalar, dirección no-input, `SupportStatus` no soportado o cambio de namespace se clasifican `RequiresDevelopment`.
- `IntegrationContractsController` expone los contratos definidos en `API_CONTRACTS.md`: listado por método, detalle, clone, actualización de presentación y compare; además expone creación manual de `Draft`. Todas las operaciones administrativas reutilizan las políticas `CanReadAch`/`CanManageAch`.
- No se modificó modelo EF, migraciones, clientes SOAP, parsers, transporte ni flags de cutover. La auditoría existente de `AchDbContext` y `AuditableEntity` continúa registrando cambios; `ImportedBy` conserva actor para creación/clonación y el runtime autenticado sigue siendo autoridad de auditoría para updates.

### 28.2 Pruebas y validación

- Se agregó `IntegrationContractVersionAdministrationTests` con cobertura focal escrita para: creación manual sin alterar el Active, clonación completamente versionada preservando el origen, edición de metadata funcional con avance/rechazo de revisión obsoleta, rechazo de edición sobre Active y clasificación de parámetros nuevos `MappingRequired` vs `RequiresDevelopment`.
- No se ejecutaron build, tests, DB, migraciones, Docker, Playwright, SOAP runtime ni UAT. `validation_execution = DEFERRED_TO_P8` bajo `IMPLEMENTATION_FIRST_VALIDATION_AT_END`.
- El snapshot fuente autoritativo fue `ACHInterbank-R34-TPA9B4-P3.4-DONE-FULL-PROJECT.zip`, SHA-256 `b1fd371c575dcb0397274590d00715632fcaaa13390349064d7e35df43f36c36`. Antes de modificar P4.1 se capturaron hashes de los archivos P1 directamente afectados y se verificaron los hashes SOAP críticos; `WscfaachSoapClient`=`c7cf6bc000fe4c5fbebfc60351aa6f25e3085ee1dd3ae364cd0536c919f98f72` y `WsAxonRespuestaTransaccionesSoapClient`=`d8f0844a3ff904683ec357d4a51f93c642eb136686dce50a7d07deccc011657d`, coincidentes con la evidencia P3.4.
- `EVID-CFAUX-P4-VERSION-DIFF-001`: implementación P4.1 cerrada por inspección estática dirigida el `2026-10-13T03:35:54Z`; validación ejecutable diferida a P8.

### 28.3 Estado

- `P4.1 = DONE`.
- `P4 = IN_PROGRESS`.
- `last_completed_micro_prompt = P4.1`.
- `next_micro_prompt = P4.2`.
- `validation_execution = DEFERRED_TO_P8`.
- P4.2 no fue ejecutado en este turno.

## 29. P4.2 — Análisis/importación WSDL/XSD segura

### 29.1 Implementación

- `IIntegrationContractArtifactAnalyzer` / `IntegrationContractArtifactAnalyzer` separan análisis de persistencia. El analyzer recibe bytes locales, acepta únicamente `.wsdl`/`.xsd`, aplica límite de artefacto de 2 MiB, límite de caracteres XML, profundidad, número de elementos y parámetros, y calcula SHA-256 determinista solo después de superar el límite de tamaño.
- El parseo usa `XmlReaderSettings` con `DtdProcessing=Prohibit` y `XmlResolver=null`; no existe resolución de entidades, `wsdl:import`, `xsd:import`, `xsd:include` ni `xsd:redefine`. Toda referencia externa/local secundaria se rechaza fail-closed antes de intentar resolverla. SOAP 1.2, rpc/encoded, `soap:header`, multipart inputs, `choice/all/any`, atributos, estructuras anidadas, colecciones, facetas no representadas y ambigüedades de operación/binding/message/type/root XSD se rechazan explícitamente.
- WSDL 1.1 soportado: selección inequívoca de una operación, binding SOAP 1.1 document/literal, `soapAction` único y mensaje de entrada con un part; el schema inline se proyecta únicamente a una `sequence` plana de parámetros escalares soportados, preservando orden, required/minOccurs, nillable y tipo XSD.
- XSD standalone soportado: requiere exactamente un elemento global raíz y metadata SOAP explícita (`OperationName` + `StandaloneXsdSoapAction`) porque el schema por sí solo no define operación/action; cualquier ausencia se reporta como análisis no importable, sin inferencias silenciosas.
- `IIntegrationContractArtifactImportService` / `IntegrationContractArtifactImportService` vuelven a ejecutar el analyzer antes de persistir, reutilizan el catálogo/versionado existente, crean siempre una nueva `IntegrationContractVersion` con `Status=Draft`, `SourceType=WsdlImport`, `Revision=1`, `SourceDocumentName`, `SourceDocumentHash`, `ImportedAtUtc` e `ImportedBy`, y crean parámetros ligados exclusivamente al nuevo `ContractVersionId`. No se valida, activa, retira ni modifica una versión existente durante import.
- `IntegrationContractsController` agrega endpoints multipart separados de analyze/import con `CanManageAch`, límite de request y actor derivado del principal autenticado. El endpoint analyze no persiste; import responde `400` con evidencia del análisis cuando el artefacto es rechazado y `201` solo para el nuevo Draft.
- No se modificaron modelo EF, migraciones, clientes SOAP, parsers, transporte, feature flags ni cutover.

### 29.2 Pruebas y validación

- Se agregó `IntegrationContractArtifactImportTests` y fixtures adversariales P4.2 para DTD, XXE con entidad `file://`, referencia remota HTTPS, archivo sobre 2 MiB, WSDL malformado, `xsd:choice` no soportado, WSDL válido y XSD standalone válido.
- La cobertura escrita verifica SHA-256 determinista, extracción escalar soportada, necesidad de metadata explícita para XSD standalone, persistencia fail-closed nula ante artefacto rechazado y que una importación válida permanece `Draft` sin alterar la versión `Active` previa.
- No se ejecutaron build, tests, DB, migraciones, Docker, Playwright, SOAP runtime ni UAT. `validation_execution = DEFERRED_TO_P8` bajo `IMPLEMENTATION_FIRST_VALIDATION_AT_END`.
- El snapshot fuente autoritativo fue `ACHInterbank-R34-TPA9B4-P4.1-DONE-FULL-PROJECT.zip`, SHA-256 `0678fdbc00ad6f7ea9d540e1f5c0ed5fdd8ad6557a1332d47e526cd676399123`. Los hashes SOAP críticos permanecen: `WscfaachSoapClient`=`c7cf6bc000fe4c5fbebfc60351aa6f25e3085ee1dd3ae364cd0536c919f98f72`, `WsAxonRespuestaTransaccionesSoapClient`=`d8f0844a3ff904683ec357d4a51f93c642eb136686dce50a7d07deccc011657d`, `RegistrarRespuestaAchSoapResponseParser`=`a7860c3015940125f06e2f9e83994873550f05af545e7354d1db968c7a979091`.
- `EVID-CFAUX-P4-WSDL-SAFE-001`: implementación P4.2 cerrada por inspección estática dirigida el `2026-10-13T03:51:07Z`; validación ejecutable diferida a P8.

### 29.3 Estado

- `P4.2 = DONE`.
- `P4 = IN_PROGRESS`.
- `last_completed_micro_prompt = P4.2`.
- `next_micro_prompt = P4.3`.
- `validation_execution = DEFERRED_TO_P8`.
- P4.3 no fue ejecutado en este turno.


## 30. P4.3 — Clonación de mapping entre ContractVersions

### 30.1 Implementación

- `IIntegrationMappingSetService` / `IntegrationMappingSetService` agregan clonación explícita `CloneToContractVersionAsync` desde un `MappingSet` versionado hacia otra `ContractVersion` del mismo método. La operación exige revisiones esperadas del mapping fuente y del contrato destino, usa transacción local, no muta la fuente y crea únicamente un `MappingSet` destino `Draft` ligado al `ContractVersionId` destino.
- La correspondencia primaria usa `ParameterPath` ordinal. Cada parámetro de entrada se clasifica como `Copied`, `New`, `Removed` o `Incompatible`. Solo se copian reglas cuando ruta, dirección, cardinalidad y tipos técnicos son compatibles; los prefijos XSD comunes se normalizan para comparar el tipo semántico. Cambios de tipo/cardinalidad o de `ParameterPath` no se copian de forma implícita.
- Un parámetro nuevo optional queda `New` sin mapping implícito. Un parámetro nuevo required queda `New` y la validación lo mantiene incompleto hasta que exista una regla válida. Un parámetro eliminado queda solo en la evidencia histórica de la clonación y no crea regla en el mapping destino.
- Las clasificaciones de clonación se persisten como evidencia en `IntegrationMappingSetHistory` con acción `ClonedAcrossContractVersions` y hash SHA-256 del snapshot/evidencia, de modo que incompatibilidades no resueltas puedan bloquear validación posterior sin introducir reglas activas ficticias.
- `ValidateContractMappingAsync` agrega validación conjunta `ContractVersion + MappingSet` con control optimista por `Revision`; falla cerrado ante reglas fuera del contrato, parámetros de entrada no soportados e incompatibilidades de clonación todavía sin mapping explícito. El resumen de validación continúa persistido en el `MappingSet` y avanza su `Revision` mediante el guard existente.
- `IntegrationMappingSetDto` expone `ContractVersionId` y `Revision` para administración auditable/concurrente. `IntegrationMappingSnapshotBuilder` y `IntegrationMappingValidationService` mantienen el bridge legacy `baseline-v1`, pero para contratos no-baseline operan exclusivamente sobre parámetros de la versión contractual asociada.
- `IntegrationMappingSetsController` agrega endpoints administrativos para clone-to-contract-version y validate-contract-mapping. No se realizó publicación, activación, cutover ni cambios en clientes SOAP, parsers o transporte.

### 30.2 Pruebas y validación

- Se agregó `IntegrationMappingContractVersionCloneTests` con cobertura focal escrita para clon compatible, parámetro nuevo optional, parámetro nuevo required incompleto, removed solo como evidencia, incompatibilidad por tipo, incompatibilidad por cardinalidad, `ParameterPath` cambiado, validación contract+mapping incompleta/completa y preservación de fuente v1.
- No se ejecutaron build, tests, DB, migraciones, Docker, Playwright, SOAP runtime ni UAT. `validation_execution = DEFERRED_TO_P8` bajo `IMPLEMENTATION_FIRST_VALIDATION_AT_END`.
- El snapshot fuente autoritativo fue `ACHInterbank-R34-TPA9B4-P4.2-DONE-FULL-PROJECT.zip`, SHA-256 `f1b8d135fb9e8afd432879de99894d31ee2097daff68125d502bf64750b9072a`. Los hashes SOAP críticos permanecen: `WscfaachSoapClient`=`c7cf6bc000fe4c5fbebfc60351aa6f25e3085ee1dd3ae364cd0536c919f98f72`, `WsAxonRespuestaTransaccionesSoapClient`=`d8f0844a3ff904683ec357d4a51f93c642eb136686dce50a7d07deccc011657d`, `RegistrarRespuestaAchSoapResponseParser`=`a7860c3015940125f06e2f9e83994873550f05af545e7354d1db968c7a979091`.
- Archivos modificados P4.3: `src/Cfa.ACHInterbank.Application/Integrations/Dtos/IntegrationMappingSetDtos.cs`, `src/Cfa.ACHInterbank.Application/Integrations/Interfaces/IIntegrationMappingSetService.cs`, `src/Cfa.ACHInterbank.Api/Controllers/IntegrationMappingSetsController.cs`, `src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingSetService.cs`, `src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingValidationService.cs`, `src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingSnapshotBuilder.cs`, `tests/Cfa.ACHInterbank.Tests/IntegrationMappingContractVersionCloneTests.cs` y este `MEMORY_STATE.md`.
- `EVID-CFAUX-P4-MAPPING-CLONE-001`: implementación P4.3 cerrada por inspección estática dirigida el `2026-10-13T04:52:10Z`; validación ejecutable diferida a P8.

### 30.3 Estado

- `P4.3 = DONE`.
- `P4 = IN_PROGRESS`.
- `last_completed_micro_prompt = P4.3`.
- `next_micro_prompt = P4.4`.
- `validation_execution = DEFERRED_TO_P8`.
- P4.4 no fue ejecutado en este turno.

## 31. P4.4 — Activación atómica y rollback no destructivo

### 31.1 Implementación

- `IIntegrationContractVersionService` / `IntegrationContractVersionService` incorporan activación gobernada de `ContractVersion + MappingSet` como una sola transición transaccional `Serializable`. La solicitud exige `MappingSetId`, `ExpectedContractRevision`, `ExpectedMappingRevision` y actor; la activación directa legacy sin revisión del mapping queda fail-closed y el retiro contractual directo queda deshabilitado para evitar estados parciales.
- La activación acepta únicamente `ContractVersion=Validated` y `MappingSet=Draft` ligado exactamente al mismo `ContractVersionId`/método. Antes de mutar estado reevalúa el mapping sin persistir efectos, exige cobertura completa/compatible, rechaza incompatibilidades no resueltas y reutiliza el diff P4.1 contra la versión Active actual para bloquear cualquier `RequiresDevelopment` pendiente.
- La transición retira primero dentro de la misma transacción el par vigente (`Active -> Retired`, `Published -> Archived`) y solo después activa/publica el candidato. Si cualquier precondición, guard, persistencia o auditoría falla, la transacción revierte y el par vigente no queda parcialmente retirado. El índice/guard existente continúa garantizando una sola `ContractVersion Active` por método.
- `Revision` se valida antes de iniciar cambios y se vuelve a comprobar dentro de la transacción; los guards existentes avanzan exactamente una unidad por mutación persistida de contrato/mapping. La activación asigna la siguiente versión publicada del mapping del contrato destino y conserva el versionado histórico.
- `IIntegrationMappingValidationService` agrega `EvaluateAsync`, que ejecuta la misma validación contract-driven sin persistir `ValidationSummaryJson`; la activación/rollback usan esa evaluación pura y solo persisten el resumen junto con la transición exitosa. La validación contractual se endureció para aceptar únicamente parámetros `Input + Scalar + Supported`, alineada con la clasificación `RequiresDevelopment` de P4.1.
- Rollback recibe explícitamente el par histórico destino y el par Active/Published actual, junto con las cuatro revisiones esperadas. Solo permite `Retired + Archived` como destino, revalida diff contractual y mapping, retira/archiva el par vigente y reactiva el par histórico sin clonar, sin renumerar su mapping y sin reescribir `PublishedAtUtc/PublishedBy` ni `ActivatedAtUtc/ActivatedBy` históricos.
- `IntegrationContractVersionInvariantGuard` mantiene contenido de `Active/Retired` inmutable y añade únicamente la transición de estado `Retired -> Active` necesaria para rollback gobernado; cualquier otra edición de una versión Retired continúa rechazada.
- La auditoría de transición agrega snapshots SHA-256 en `IntegrationMappingSetHistory` (`ActivatedWithContract`, `ArchivedByContractActivation`, `ReactivatedByRollback`, `ArchivedByRollback`) y conserva la auditoría genérica de `AchDbContext` para las mutaciones de contrato/mapping. `AchDbContext` ahora rechaza modificaciones o eliminaciones de filas `IntegrationMappingSetHistory`; el historial solo admite append.
- `IntegrationContractsController` expone endpoints administrativos `POST /contracts/{id}/activate` y `POST /contracts/{id}/rollback` bajo `CanManageAch`. No se hizo cutover adicional ni se modificaron clientes SOAP, parsers, transporte o flags de runtime.

### 31.2 Pruebas y validación

- Se agregó `IntegrationContractActivationRollbackTests` con cobertura focal escrita para activación exitosa, mapping incompleto con atomicidad sin estado parcial, mapping perteneciente a otra ContractVersion, conflicto de `Revision`, diff `RequiresDevelopment`, garantía de una sola Active, rollback exitoso sin clone/renumeración, rollback incompatible, conflicto de revisión en rollback, append-only del historial y rechazo explícito de reescritura del historial.
- `IntegrationContractVersionLifecycleTests` se actualizó para caracterizar que la activación directa legacy queda rechazada después de P4.4 y que la inmutabilidad de contenido Retired sigue vigente.
- No se ejecutaron build, tests, DB, migraciones, Docker, Playwright, SOAP runtime ni UAT. `validation_execution = DEFERRED_TO_P8` bajo `IMPLEMENTATION_FIRST_VALIDATION_AT_END`.
- El snapshot fuente autoritativo fue `ACHInterbank-R34-TPA9B4-P4.3-DONE-FULL-PROJECT.zip`, SHA-256 `166fc34c502dbb427accbcc26027480eca8c8c3ca83092d394e4a8d4ed5828fb`. Los hashes SOAP críticos permanecen: `WscfaachSoapClient`=`c7cf6bc000fe4c5fbebfc60351aa6f25e3085ee1dd3ae364cd0536c919f98f72`, `WsAxonRespuestaTransaccionesSoapClient`=`d8f0844a3ff904683ec357d4a51f93c642eb136686dce50a7d07deccc011657d`, `RegistrarRespuestaAchSoapResponseParser`=`a7860c3015940125f06e2f9e83994873550f05af545e7354d1db968c7a979091`.
- Archivos modificados P4.4: `src/Cfa.ACHInterbank.Application/Integrations/Dtos/IntegrationContractVersionAdministrationDtos.cs`, `src/Cfa.ACHInterbank.Application/Integrations/Interfaces/IIntegrationContractVersionService.cs`, `src/Cfa.ACHInterbank.Application/Integrations/Interfaces/IIntegrationMappingValidationService.cs`, `src/Cfa.ACHInterbank.Api/Controllers/IntegrationContractsController.cs`, `src/Cfa.ACHInterbank.Persistence/DataBase/AchDbContext.cs`, `src/Cfa.ACHInterbank.Persistence/DataBase/IntegrationContractVersionInvariantGuard.cs`, `src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationContractVersionService.cs`, `src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingValidationService.cs`, `tests/Cfa.ACHInterbank.Tests/IntegrationContractVersionLifecycleTests.cs`, `tests/Cfa.ACHInterbank.Tests/IntegrationContractActivationRollbackTests.cs` y este `MEMORY_STATE.md`.
- `EVID-CFAUX-P4-ACTIVATION-ROLLBACK-001`: implementación P4.4 cerrada por inspección estática dirigida el `2026-10-13T05:11:09Z`; validación ejecutable diferida a P8.

### 31.3 Estado

- `P4.4 = DONE`.
- `P4 = IN_PROGRESS`.
- `last_completed_micro_prompt = P4.4`.
- `next_micro_prompt = P4.5`.
- `validation_execution = DEFERRED_TO_P8`.
- P4.5 no fue ejecutado en este turno.

## 32. P4.5 — Prueba no-code `TIPOCLIENTE_TEST` y cierre P4

### 32.1 Escenario implementado

- Se agregó `IntegrationContractNoCodeVersioningEndToEndTests` como escenario de aplicación controlado y exclusivo de tests para demostrar el invariante no-code de P4: parte de `baseline-v1`, clona una ContractVersion v2, agrega metadata de parámetro input `Scalar`/`required` para `TIPOCLIENTE_TEST`, clona el MappingSet v1→v2 y verifica la clasificación `New` con validación inicialmente incompleta por `REQUIRED_PARAMETER_MISSING`.
- El parámetro de prueba se mapea después a la fuente preexistente `Transaction:transaction.reference`, resuelta por el `IntegrationSourceProviderRegistry` existente. La regla no define `TransformationCode`, `FixedValue`, provider nuevo, transformación nueva ni lógica por nombre de campo.
- Tras `ValidateContractMappingAsync`, la prueba marca v2 como `Validated`, ejecuta la activación atómica P4.4 y resuelve el request con `IntegrationRequestResolver` + `IntegrationExecutionContractResolver` + `IntegrationParameterValueResolver`; `ContractDrivenSoapBodyWriter` genera el XML SOAP y la prueba exige que `TIPOCLIENTE_TEST` aparezca con el valor de la fuente existente y en el orden contractual persistido.
- El mismo test ejecuta conceptualmente el rollback no destructivo P4.4 hacia v1 usando las revisiones actuales de ambos pares, y comprueba v1 `Active/Published`, v2 `Retired/Archived`, conservación del número de reglas fuente y evidencia `ReactivatedByRollback` / `ArchivedByRollback`.
- Búsqueda literal dirigida de `TIPOCLIENTE_TEST` no encontró referencias en código productivo, bootstrap ni seed. Las únicas referencias no-test son las especificaciones del propio paquete (`IMPLEMENTATION_PLAN.md` y `micro-prompts/P4.md`); no se modificaron.

### 32.2 Pruebas y validación

- Se escribió el test focal end-to-end `tests/Cfa.ACHInterbank.Tests/IntegrationContractNoCodeVersioningEndToEndTests.cs`; no se ejecutó.
- No se ejecutaron build, tests, DB, migraciones, Docker, Playwright, SOAP runtime ni UAT. `validation_execution = DEFERRED_TO_P8` bajo `IMPLEMENTATION_FIRST_VALIDATION_AT_END`.
- Snapshot fuente autoritativo: `ACHInterbank-R34-TPA9B4-P4.4-DONE-FULL-PROJECT.zip`, SHA-256 `603af05a48f54a59900b16b221b029c5d8a67dd0256c4d6d4a412dfc3e8b93ca`.
- Evidencia de no-code respecto de P4.4: `ContractDrivenSoapBodyWriter.cs` SHA-256 `88e18c76b9b1baeaedcd2452b3ea00b62c03209bcc6cd58fef0fdd51f8dd7c6a`; `WscfaachSoapClient.cs`=`c7cf6bc000fe4c5fbebfc60351aa6f25e3085ee1dd3ae364cd0536c919f98f72`; `WsAxonRespuestaTransaccionesSoapClient.cs`=`d8f0844a3ff904683ec357d4a51f93c642eb136686dce50a7d07deccc011657d`; `RegistrarRespuestaAchSoapResponseParser.cs`=`a7860c3015940125f06e2f9e83994873550f05af545e7354d1db968c7a979091`.
- Archivos modificados P4.5: `tests/Cfa.ACHInterbank.Tests/IntegrationContractNoCodeVersioningEndToEndTests.cs` y este `MEMORY_STATE.md`. No se modificó código productivo.
- `EVID-CFAUX-P4-NOCODE-001`: implementación P4.5 cerrada por inspección estática dirigida el `2026-10-13T05:26:03Z`; validación ejecutable diferida a P8.

### 32.3 Estado

- `P4.5 = DONE`.
- `P4 = DONE`.
- `last_completed_micro_prompt = P4.5`.
- `next_allowed_phase = P5`.
- `next_micro_prompt = P5.1`.
- `validation_execution = DEFERRED_TO_P8`.
- P5.1 no fue ejecutado en este turno.

## 33. P5.1 — Arquitectura frontend de la feature

### 33.1 Implementación

- Se creó la base aditiva de la feature CFA bajo `web/ach-interbank-ui/src/app/features/integrations/` con separación explícita `data-access / state / presentation`, sin sustituir ni rediseñar las rutas/páginas legacy `mappings`, `mappings/:methodCode/:mappingSetId` y `soap-settings`.
- `data-access/integration-api.models.ts` refleja los contratos API actuales relevantes para catálogo, `ContractVersion`, parámetros contractuales, `MappingSet` con `ContractVersionId`/`Revision`, validación/cobertura, clone contract-aware, activación/rollback, import/analyze de WSDL/XSD y configuración de conexión. `integration-read.models.ts` introduce read models funcionales para servicio/parámetro; estados contractuales, mapping, cobertura y capacidades quedan `null` cuando el backend no los entrega y no se sintetizan reglas de elegibilidad en Angular.
- Se agregaron adapters API focales: `IntegrationWorkspaceApi` para catálogo, `IntegrationContractApi` para versionado/diff/import/activación/rollback, `IntegrationMappingApi` para mapping/validación/clone contract-aware e `IntegrationConnectionApi` para configuración SOAP. Los componentes no necesitan realizar llamadas HTTP directas en el nuevo flujo.
- `IntegrationCenterStore`, `ContractWorkspaceStore` y `MappingWorkspaceStore` usan Angular signals/computed y estados tipados `idle/loading/ready/empty/error/forbidden`. `IntegrationCenterStore` expone permisos de presentación derivados de la sesión (`CanReadAch`/`CanManageAch`), manteniendo la autorización efectiva en backend. `MappingWorkspaceStore` expone cobertura únicamente después de recibir `IntegrationMappingValidationResultDto`; no calcula localmente `canPublish`, compatibilidad contractual ni completitud.
- La frontera de presentación agrega copy/presenters puros (`integration-copy`, `mapping-status.presenter`, `technical-detail.presenter`) para traducir/organizar valores recibidos sin decidir transiciones de negocio. `IntegrationWorkspaceComponent` queda `OnPush` y actúa como scope de providers para los tres stores, sin disparar nuevas cargas ni alterar el HTML/rutas actuales.
- No se implementó todavía el Centro de integración CFA completo, no se creó el editor guiado P6, no se rediseñó navegación, no se reemplazaron dialogs legacy ni se alteraron branding/tokens/Angular Material existentes.

### 33.2 Pruebas y validación

- Se escribieron pruebas focales no ejecutadas para read models, `IntegrationContractApi`, `IntegrationMappingApi`, `IntegrationCenterStore`, `ContractWorkspaceStore`, `MappingWorkspaceStore` y presenter de estados. Cubren autoridad de backend/no invención de estado, transporte de revisiones optimistas, multipart de análisis contractual, estados `empty/forbidden`, permisos de presentación y cobertura proveniente de validación backend.
- No se ejecutaron `npm`, build Angular, tests Karma/Jasmine, Playwright ni validación runtime. `validation_execution = DEFERRED_TO_P8` bajo `IMPLEMENTATION_FIRST_VALIDATION_AT_END`.
- Snapshot fuente autoritativo: `ACHInterbank-R34-TPA9B4-P4.5-DONE-FULL-PROJECT.zip`, SHA-256 `ca44199920a5f72f0c8f1b0dc059f586c1d33ba294d621406754de607e94b7b2`. Se trabajó exclusivamente en modo `PACKAGE_SNAPSHOT`; no se consultó Git ni historial Git.
- Archivos productivos agregados P5.1: `data-access/integration-api.models.ts`, `data-access/integration-read.models.ts`, `data-access/integration-workspace.api.ts`, `data-access/integration-contract.api.ts`, `data-access/integration-mapping.api.ts`, `data-access/integration-connection.api.ts`, `state/integration-state.models.ts`, `state/integration-center.store.ts`, `state/contract-workspace.store.ts`, `state/mapping-workspace.store.ts`, `presentation/integration-copy.ts`, `presentation/mapping-status.presenter.ts`, `presentation/technical-detail.presenter.ts`; se modificó únicamente `pages/integration-workspace.component.ts` para `OnPush` + scope de stores.
- Pruebas agregadas P5.1: `data-access/integration-read.models.spec.ts`, `data-access/integration-contract.api.spec.ts`, `data-access/integration-mapping.api.spec.ts`, `state/integration-center.store.spec.ts`, `state/contract-workspace.store.spec.ts`, `state/mapping-workspace.store.spec.ts`, `presentation/mapping-status.presenter.spec.ts`.
- `EVID-CFAUX-P5-FRONTEND-ARCH-001`: implementación P5.1 cerrada por inspección estática dirigida el `2026-10-13T12:28:51Z`; validación ejecutable diferida a P8.

### 33.3 Estado

- `P5.1 = DONE`.
- `P5 = IN_PROGRESS`.
- `last_completed_micro_prompt = P5.1`.
- `next_micro_prompt = P5.2`.
- `validation_execution = DEFERRED_TO_P8`.
- P5.2 no fue ejecutado en este turno.

## 34. P5.2 — Centro de integración CFA

### 34.1 Implementación

- Se implementó el shell funcional `Centro de integración CFA` sobre la feature existente de Integraciones, manteniendo intactas las rutas legacy `mappings`, `mappings/:methodCode/:mappingSetId` y `soap-settings`. El shell se monta en `IntegrationWorkspaceComponent` antes del `router-outlet`, por lo que no introduce redirects ni adelanta P5.4.
- La presentación principal usa exclusivamente read models construidos a partir de adapters/backend P5.1. Los tres servicios objetivo se limitan y ordenan según la arquitectura CFA (`Proc_Transacciones`, `Proc_Contrapartidas`, `RegistrarRespuestaTransaccion`), pero los títulos y descripciones funcionales se consumen de la metadata backend (`displayName`/`mappingPurpose`); no se sintetizan estados contractuales, de mapping o de conexión en componentes.
- `IntegrationCenterStore` fue ampliado como orquestador de estado con signals. Carga catálogo CFA, ContractVersion activa reportada por backend, MappingSet publicado reportado por backend, cobertura producida por `validate` cuando el endpoint está disponible y configuración de conexión. Los 403/404 de lecturas secundarias degradan solo el dato correspondiente a `null`; errores no opcionales mantienen el estado tipado `error` con reintento. La ausencia local de `CanReadAch` produce `forbidden` sin invocar APIs.
- `integration-read.models.ts` conserva al backend como autoridad y ahora representa conexión deshabilitada como `Disabled` cuando el propio backend entrega `enabled=false`; no deriva elegibilidad ni capacidad de publicación. Las capacidades `canEditMapping/canPublish/canManageContract/canManageConnection` continúan en `null` cuando el backend no las suministra.
- Se agregaron componentes presentacionales `IntegrationCenterShellComponent` e `IntegrationServiceCardComponent`, ambos `OnPush`. Cada tarjeta muestra título funcional, descripción funcional, nombre técnico secundario, versión/estado contractual, estado y cobertura de relación de información, estado de conexión y acciones compatibles con permisos. `CanManageAch` controla la acción de administración de conexión; una futura capacidad backend `canManageConnection=false` prevalece sobre el permiso genérico de sesión.
- El lenguaje visible principal fue alineado con `SPA_UX_UI_ARCHITECTURE.md`: `Centro de integración CFA`, `Relación de información`, `Contrato del servicio`, `Simulación sin envío`, `Envío habilitado`, `Publicada` y `Archivada`. Los nombres SOAP se presentan únicamente como detalle técnico secundario en las tarjetas.
- Se implementaron estados explícitos `loading`, `error` con `Reintentar`, `empty` y `forbidden/permisos insuficientes`. La UI usa elementos semánticos (`section`, `article`, `header`, `dl`, `nav`), regiones `role=status/alert`, links/botones nativos para teclado y grid responsive 3→2→1 columnas para desktop/tablet/mobile.
- No se realizó limpieza global de branding/colores, migración general de dialogs, cambios backend, editor guiado P6, rutas/redirects P5.4 ni trabajo P5.3. El contenido legacy debajo del nuevo shell y sus rutas continúan disponible.

### 34.2 Pruebas y validación

- Se agregaron pruebas focales no ejecutadas para `IntegrationServiceCardComponent` y `IntegrationCenterShellComponent`, cubriendo jerarquía funcional/técnica, traducción visible de estados, cobertura, acciones por permisos, loading, error/reintento, empty, forbidden y render de las tres tarjetas objetivo.
- `IntegrationCenterStore` se actualizó con pruebas focales para orden de servicios CFA, snapshot de contrato/mapping/cobertura/conexión proveniente del backend, sincronización de permisos de sesión, `forbidden` sin llamadas de red y error backend no opcional. `integration-read.models.spec.ts` agrega el caso de conexión `enabled=false`; `mapping-status.presenter.spec.ts` refleja el vocabulario funcional acordado.
- No se ejecutaron `npm`, build Angular, tests Karma/Jasmine, Playwright ni validación runtime. `validation_execution = DEFERRED_TO_P8` bajo `IMPLEMENTATION_FIRST_VALIDATION_AT_END`.
- Snapshot fuente autoritativo: `ACHInterbank-R34-TPA9B4-P5.1-DONE-FULL-PROJECT.zip`, SHA-256 `a9ea680fd28960660ceba420c019ad7bfc43bbed4e8dffb336bd9819f896c905`. Se trabajó exclusivamente en modo `PACKAGE_SNAPSHOT`; no se consultó Git ni historial Git.
- Archivos productivos agregados P5.2: `presentation/integration-center-shell.component.ts`, `presentation/integration-center-shell.component.html`, `presentation/integration-center-shell.component.scss`, `presentation/integration-service-card.component.ts`, `presentation/integration-service-card.component.html`, `presentation/integration-service-card.component.scss`.
- Archivos productivos modificados P5.2: `data-access/integration-read.models.ts`, `state/integration-center.store.ts`, `presentation/integration-copy.ts`, `presentation/mapping-status.presenter.ts`, `pages/integration-workspace.component.ts`, `pages/integration-workspace.component.html`, `pages/integration-workspace.component.scss`.
- Pruebas agregadas/modificadas P5.2: `presentation/integration-center-shell.component.spec.ts`, `presentation/integration-service-card.component.spec.ts`, `state/integration-center.store.spec.ts`, `data-access/integration-read.models.spec.ts`, `presentation/mapping-status.presenter.spec.ts`.
- `EVID-CFAUX-P5-CENTER-001`: implementación P5.2 cerrada por inspección estática dirigida el `2026-10-13T12:45:36Z`; validación ejecutable diferida a P8.

### 34.3 Estado

- `P5.2 = DONE`.
- `P5 = IN_PROGRESS`.
- `last_completed_micro_prompt = P5.2`.
- `next_micro_prompt = P5.3`.
- `validation_execution = DEFERRED_TO_P8`.
- P5.3 no fue ejecutado en este turno.

## 35. P5.3 — Material, branding y dialogs

### 35.1 Implementación

- Se endureció el alcance frontend de Integraciones CFA sin avanzar P5.4/P6. Los estilos tocados en `features/integrations` ya no contienen colores `hex/rgb/hsl` hardcodeados; shell, tarjetas, workspace, matriz legacy y editor legacy consumen únicamente tokens globales (`--color-*`, `--mat-sys-*`) y radios/foco del sistema.
- Se ampliaron únicamente los tokens semánticos globales necesarios (`--color-warning-border`, `--color-info`, `--color-info-surface`, `--color-info-text`) en `src/styles/_tokens.scss`. Los estados de error/advertencia/éxito/información usan tokens semánticos y dejaron de depender del color primario de marca. Branding dinámico continúa entrando por los tokens ya gobernados por `BrandingService`; no se introdujeron colores locales nuevos en la feature.
- Angular Material quedó alineado con la familia tipográfica global `--font-sans` mediante el bridge de tokens tipográficos `--mat-sys-*-font` en `src/styles.scss`. No se agregó ninguna familia tipográfica nueva ni `font-family` local en Integraciones CFA.
- Los cuatro overlays artesanales prioritarios de `MappingSetsPageComponent` (detalle de relación, edición de relación, creación de borrador y auditoría) fueron migrados a `MatDialog`/CDK. Se eliminaron backdrop/fixed overlay manuales y se usa `MatDialogModule` + `A11yModule`, cierre por backdrop/Escape, focus trap, `restoreFocus`, `ariaLabelledBy`, límites de `maxWidth/maxHeight` relativos al viewport y foco inicial explícito con `cdkFocusInitial`.
- La migración de dialogs conserva la lógica/API en el page container existente; no se introdujo data-access en componentes presentacionales P5.1/P5.2 ni se duplicaron reglas contractuales/mapping en frontend. Abrir/cancelar dialogs no crea borradores ni persiste reglas; la persistencia continúa usando el flujo backend existente.
- Se eliminó la deuda de `li` clicable en el selector de parámetros del editor legacy: ahora cada opción es un `button type="button"` con `aria-pressed`, foco visible por token global y semántica nativa de teclado. No se rediseñó el editor ni se adelantó P6.
- Breakpoints del alcance tocado se alinearon a compacto `<768px`, mediano `768–1199px` y amplio `>=1200px`; shell 3→2→1, acciones compactas y dialogs acotados al viewport. Las rutas y navegación legacy permanecen sin cambios.

### 35.2 Pruebas y validación

- Se agregaron/actualizaron pruebas focales no ejecutadas para: ausencia de colores hardcodeados y `font-family` local en estilos compilados de Integraciones CFA; consumo de tokens globales/Material; presencia de breakpoints 1199/767; apertura de `MatDialog`, foco inicial, cierre por Escape, restauración de foco y límites de viewport; y semántica nativa de botón para selección de parámetros.
- No se ejecutaron `npm`, build Angular, Karma/Jasmine, Playwright ni validación runtime. `validation_execution = DEFERRED_TO_P8` bajo `IMPLEMENTATION_FIRST_VALIDATION_AT_END`.
- Snapshot fuente autoritativo: `ACHInterbank-R34-TPA9B4-P5.2-DONE-FULL-PROJECT.zip`, SHA-256 `5a6056a13e58f81754e1ca3b3efc7fb95aecb8b02b6f439b8a312d60025a1877`. Se trabajó exclusivamente en modo `PACKAGE_SNAPSHOT`; no se consultó Git ni historial Git.
- Archivos productivos modificados P5.3: `web/ach-interbank-ui/src/styles.scss`, `web/ach-interbank-ui/src/styles/_tokens.scss`, `features/integrations/pages/integration-workspace.component.scss`, `features/integrations/pages/mapping-sets-page.component.ts`, `features/integrations/pages/mapping-sets-page.component.html`, `features/integrations/pages/mapping-sets-page.component.scss`, `features/integrations/pages/mapping-editor-page.component.html`, `features/integrations/pages/mapping-editor-page.component.scss`, `features/integrations/presentation/integration-center-shell.component.html`, `features/integrations/presentation/integration-center-shell.component.scss`, `features/integrations/presentation/integration-service-card.component.scss`.
- Pruebas agregadas/modificadas P5.3: `features/integrations/integrations-branding-tokens.spec.ts`, `features/integrations/pages/mapping-sets-page.component.spec.ts`, `features/integrations/pages/mapping-editor-page.component.spec.ts`.
- `EVID-CFAUX-P5-MATERIAL-BRANDING-DIALOGS-001`: implementación P5.3 cerrada por inspección estática dirigida el `2026-10-13T13:16:24Z`; validación ejecutable diferida a P8.

### 35.3 Estado

- `P5.3 = DONE`.
- `P5 = IN_PROGRESS`.
- `last_completed_micro_prompt = P5.3`.
- `next_micro_prompt = P5.4`.
- `validation_execution = DEFERRED_TO_P8`.
- P5.4 no fue ejecutado en este turno.

## 36. P5.4 — Rutas, navegación y cierre P5

### 36.1 Implementación

- `Centro de integración CFA` queda como acceso principal en `/integraciones`. La ruta superior conserva los guards/roles/permisos existentes (`Admin`/`ACH.Operator`, `CanReadAch`/`CanManageAch`) y ahora carga de forma lazy el arreglo standalone `INTEGRATIONS_ROUTES`; no se agregó ningún redirect histórico.
- La feature dejó de usar `IntegrationsModule`/`IntegrationsRoutingModule`. Las pantallas que siguen siendo dependencias internas se cargan con `loadComponent`: centro (`/integraciones`), `Relación de información` (`/integraciones/relacion-informacion`), editor reutilizable (`/integraciones/relacion-informacion/:methodCode/:mappingSetId`) y `Conexión CFA` (`/integraciones/conexion`). Se retiraron las rutas técnicas frontend `mappings`, `soap-settings` y el alias superior `soap-integrations`; no quedaron ciclos de redirect dentro de Integraciones.
- `IntegrationWorkspaceComponent` quedó dedicado al Centro de integración y ya no aloja simultáneamente un segundo árbol de navegación/router-outlet técnico. Se eliminaron la barra de navegación redundante y sus estilos muertos; shell, estados y stores P5.1–P5.3 se conservan.
- Las tres tarjetas navegan con contexto del servicio mediante `?method=<operationKey>` hacia `Relación de información` y, cuando el permiso de gestión lo permite, `Conexión CFA`. Los tres servicios mantienen los títulos funcionales `Créditos recibidos en CFA`, `Débitos originados en CFA` y `Respuestas de transacciones`; `Proc_Transacciones`, `Proc_Contrapartidas` y `RegistrarRespuestaTransaccion` continúan visibles únicamente como información técnica secundaria.
- `MappingSetsPageComponent` conserva el filtro/deep-link interno por `method` y el editor avanzado necesario para P6, pero bajo la ruta funcional nueva. `MappingEditorPageComponent` actualizó breadcrumb, navegación de creación y retorno a la ruta funcional. `SoapIntegrationSettingsComponent` usa el query param `method` para preseleccionar la conexión correspondiente y actualizó su jerarquía visible a `Conexión CFA`; endpoint/SOAP Action siguen siendo detalle técnico.
- `NavigationService`, dependencia directa del menú principal dinámico, normaliza entradas backend que aún apunten a `/soap-integrations`, `/integraciones/soap-settings` o `/integraciones/mappings` hacia un único `Centro de integración CFA` en `/integraciones`, eliminando duplicados de navegación sin mantener rutas/redirects paralelos. Esto adapta el menú actual sin cambiar backend.
- No se modificó backend, contrato/mapping ni reglas de autorización de negocio. Los estados `loading/error/empty/forbidden`, autoridad backend, branding/tokens P5.3, responsive y accesibilidad del shell/cards permanecen intactos. No se ejecutó P6 ni se rediseñó el editor guiado.

### 36.2 Pruebas y validación

- Se agregaron/actualizaron pruebas focales no ejecutadas para: ruta principal lazy, rutas standalone necesarias, ausencia de rutas técnicas redundantes/redirect loops, guards/permisos de la ruta superior, normalización del menú principal, navegación contextual desde las tres tarjetas, acción de conexión condicionada por permisos y rutas internas del editor/mapping.
- Se actualizaron pruebas de `MappingSetsPageComponent`, `MappingEditorPageComponent` y `SoapIntegrationSettingsComponent` para los paths/copy funcionales nuevos; las pruebas existentes de estados, permisos, branding/tokens, dialogs y stores permanecen como regresión de P5.1–P5.3.
- No se ejecutaron `npm`, build Angular, Karma/Jasmine, Playwright ni validación runtime. `validation_execution = DEFERRED_TO_P8` bajo `IMPLEMENTATION_FIRST_VALIDATION_AT_END`.
- Snapshot fuente autoritativo: `ACHInterbank-R34-TPA9B4-P5.3-DONE-FULL-PROJECT.zip`, SHA-256 `b649bd61dad3755fd4a4c1133af932d5dc66aef01639f0f750f48cc644e4867c`. Se trabajó exclusivamente en modo `PACKAGE_SNAPSHOT`; no se consultó Git ni historial Git.
- Archivos productivos agregados P5.4: `web/ach-interbank-ui/src/app/features/integrations/integrations.routes.ts`.
- Archivos productivos retirados P5.4: `web/ach-interbank-ui/src/app/features/integrations/integrations.module.ts`, `integrations-routing.module.ts`, `integrations-routing.module.spec.ts`.
- Archivos productivos modificados P5.4: `web/ach-interbank-ui/src/app/app-routing.module.ts`, `core/services/navigation.service.ts`, `features/integrations/pages/integration-workspace.component.ts/.html/.scss`, `pages/mapping-sets-page.component.ts/.html`, `pages/mapping-editor-page.component.ts`, `pages/soap-integration-settings.component.ts/.html`, `presentation/integration-service-card.component.html`.
- Pruebas agregadas/modificadas P5.4: `features/integrations/integrations.routes.spec.ts`, `app-routing-access.spec.ts`, `core/services/navigation.service.spec.ts`, `presentation/integration-service-card.component.spec.ts`, `presentation/integration-center-shell.component.spec.ts`, `pages/mapping-sets-page.component.spec.ts`, `pages/mapping-editor-page.component.spec.ts`, `pages/soap-integration-settings.component.spec.ts`.
- `EVID-CFAUX-P5-ROUTING-CLOSE-001`: P5.4 cerrado por inspección estática dirigida el `2026-10-13T13:33:24Z`; validación ejecutable diferida a P8.

### 36.3 Estado

- `P5.4 = DONE`.
- `P5 = DONE`.
- `last_completed_micro_prompt = P5.4`.
- `next_allowed_phase = P6`.
- `next_micro_prompt = P6.1`.
- `validation_execution = DEFERRED_TO_P8`.
- P6.1 no fue ejecutado en este turno.

## 37. P6.1 — Relación de información funcional

### 37.1 Implementación

- Se reemplazó la matriz técnica de `Relación de información` por una vista de consulta funcional basada en nombres y descripciones provistos por backend. Cada dato muestra uno de cuatro estados del read model: `Completo`, `Pendiente`, `Inválido` o `No aplica`; identificadores técnicos no son información principal.
- La vista usa lista escaneable por tarjetas, búsqueda sobre nombre/descripción/categoría funcional, filtro por estado y resumen de cobertura. Los conteos `total/complete/pending/invalid/notApplicable` se consumen directamente del read model backend; Angular no calcula compatibilidad ni publicabilidad.
- El detalle técnico usa un elemento semántico `details/summary`, cerrado por defecto, y concentra `ParameterPath`, elemento XML, tipo XSD/dato y namespace. No se expusieron acciones de edición de origen ni tratamiento en P6.1.
- Se implementaron estados `loading`, `error`, `empty` y `forbidden`, además de estado vacío por filtros. La tarea principal evita tablas anchas y scroll horizontal: dos columnas en amplio, una columna en mediano y disposición apilada en compacto. La interacción usa controles nativos/Material con navegación por teclado y semántica de encabezados, listas, regiones de estado y detalle expandible.
- Contradicción estructural mínima resuelta: `API_CONTRACTS.md` declara `GET /api/integrations/mappingsets/{mappingSetId}/workspace`, pero el snapshot P5.4 no lo implementaba. Se agregó únicamente ese GET `CanReadAch` y su DTO/read model; reutiliza `IIntegrationMappingValidationService.EvaluateAsync` para estados/conteos sin persistir validación y mantiene backend como autoridad. No se cambió serialización SOAP, resolución runtime, publicación ni reglas de mapping.

### 37.2 Pruebas y validación

- Se escribieron pruebas focales no ejecutadas para lista funcional, búsqueda, filtro por los cuatro estados, cobertura backend, estados loading/error/empty/forbidden, detalle técnico colapsado y estructura accesible/navegable por teclado. También se cubrieron consumo del endpoint workspace, proyección del read model y etiquetas de estado.
- No se ejecutaron `npm`, build Angular, Karma/Jasmine, Playwright, build .NET ni tests .NET. `validation_execution = DEFERRED_TO_P8` bajo `IMPLEMENTATION_FIRST_VALIDATION_AT_END`.
- Snapshot fuente autoritativo: `ACHInterbank-R34-TPA9B4-P5.4-DONE-FULL-PROJECT.zip`, SHA-256 `b2355f5e8e4bbafc84c8aea21887c66053d01ed68220feb1946ceb16d463534d`. Se trabajó exclusivamente en modo `PACKAGE_SNAPSHOT`; no se consultó Git ni historial Git.
- Archivos backend modificados P6.1: `src/Cfa.ACHInterbank.Application/Integrations/Dtos/IntegrationMappingSetDtos.cs`, `Interfaces/IIntegrationMappingSetService.cs`, `src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingSetService.cs`, `src/Cfa.ACHInterbank.Api/Controllers/IntegrationMappingSetsController.cs`.
- Archivos frontend modificados P6.1: `features/integrations/data-access/integration-api.models.ts`, `integration-mapping.api.ts`, `integration-read.models.ts`, `presentation/mapping-status.presenter.ts`, `state/mapping-workspace.store.ts`, `pages/mapping-sets-page.component.ts/.html/.scss`.
- Pruebas agregadas/modificadas P6.1: `integration-mapping.api.spec.ts`, `integration-read.models.spec.ts`, `mapping-status.presenter.spec.ts`, `mapping-workspace.store.spec.ts`, `mapping-sets-page.component.spec.ts`.
- `EVID-CFAUX-P6-RELATION-001`: P6.1 cerrado por inspección estática dirigida el `2026-10-13T13:57:26Z`; validación ejecutable diferida a P8.

### 37.3 Estado

- `P6.1 = DONE`.
- `P6 = IN_PROGRESS`.
- `last_completed_micro_prompt = P6.1`.
- `next_micro_prompt = P6.2`.
- `validation_execution = DEFERRED_TO_P8`.
- P6.2 no fue ejecutado en este turno.
## 38. P6.2 — Edición guiada de origen y tratamiento

### 38.1 Implementación

- `Relación de información` incorpora edición por parámetro con `Reactive Forms` tipados y `OnPush`. El formulario carga la regla actual desde el workspace backend y mantiene como conceptos principales `Origen del dato` y `Tratamiento del dato`.
- El selector de origen se alimenta únicamente de `sourceGroups` y `sourceCompatibility` del read model backend. Los orígenes se agrupan funcionalmente, permiten búsqueda, muestran primero el nombre funcional y dejan `entityName`, `fieldPath` e identificador como detalle secundario. La edición guiada no expone controles de path libre ni expresiones.
- El backend excluye `Constant` y `Expression` del catálogo guiado y publica por parámetro/origen la compatibilidad autoritativa. Angular solo presenta las combinaciones recibidas; no mantiene catálogo paralelo de sources ni reglas propias de compatibilidad.
- Los tratamientos compatibles se derivan del registry backend y se publican en `sourceCompatibility`. `Sin tratamiento` aparece únicamente cuando `allowsNoTransformation` lo permite. Los requisitos adicionales del tratamiento, como máscara, se informan en metadata backend y se aplican como validación del formulario.
- Se agregó el endpoint guiado `PUT /api/integrations/mappingsets/{id}/parameters/{parameterId}` porque el contrato de edición existente aceptaba `SourceFieldPath`/`ConditionExpression` libres y no transportaba `ExpectedRevision`, contradicción estructural con P6.2. El request nuevo acepta exclusivamente `SourceCatalogFieldId`, tratamiento/configuración admitidos, `ExpectedRevision` y actor.
- El guardado verifica `Revision` en backend, resuelve `SourceKind`/`SourceFieldPath` desde el catálogo persistido, valida la selección contra la compatibilidad backend, persiste y aumenta la revisión. Un conflicto de revisión se devuelve como `409`.
- La SPA no aplica éxito optimista: después del `PUT` ejecuta un nuevo `GET workspace` y solo entonces sustituye estado/read model y muestra confirmación. Los errores de guardado, incluido `403`, conservan el workspace visible y se presentan de forma accesible.
- La UI mantiene Material y tokens P5.3, detalle técnico avanzado colapsado, foco visible, controles nativos/Material navegables por teclado y disposición responsive que apila resumen/editor/acciones en compacto. No se agregó preview de valores, validación humanizada global ni publicación.

### 38.2 Pruebas y validación

- Se agregaron/actualizaron pruebas focales no ejecutadas para: formulario tipado, catálogo backend, búsqueda/agrupación, tratamientos compatibles, disponibilidad condicional de `Sin tratamiento`, rechazo de paths/expresiones libres, configuración requerida por metadata, `Revision` en guardado, refresh posterior, ausencia de éxito optimista, errores/forbidden y semántica/teclado.
- Se añadió una prueba focal backend que cubre workspace de metadata guiada, derivación del path desde catálogo, incremento de `Revision` y rechazo de revisión obsoleta.
- No se ejecutaron `npm`, build Angular, Karma/Jasmine, Playwright, build .NET ni tests .NET. `validation_execution = DEFERRED_TO_P8` bajo `IMPLEMENTATION_FIRST_VALIDATION_AT_END`.
- Snapshot fuente autoritativo: `ACHInterbank-R34-TPA9B4-P6.1-DONE-FULL-PROJECT.zip`, SHA-256 `719d6b6fcc5e81a0c06f47ae38eaec2072bd21a19d9b444c77df19884263d83d`, 4728 entradas. Se trabajó exclusivamente en modo `PACKAGE_SNAPSHOT`; no se consultó Git ni historial Git.
- Archivos backend agregados P6.2: `src/Cfa.ACHInterbank.Application/Integrations/Exceptions/IntegrationMappingRevisionConflictException.cs`.
- Archivos backend modificados P6.2: `src/Cfa.ACHInterbank.Application/Integrations/Dtos/IntegrationMappingSetDtos.cs`, `Interfaces/IIntegrationMappingSetService.cs`, `src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingSetService.cs`, `src/Cfa.ACHInterbank.Api/Controllers/IntegrationMappingSetsController.cs`.
- Archivos frontend modificados P6.2: `features/integrations/data-access/integration-api.models.ts`, `integration-mapping.api.ts`, `integration-read.models.ts`, `state/mapping-workspace.store.ts`, `pages/mapping-sets-page.component.ts/.html/.scss`.
- Pruebas agregadas/modificadas P6.2: `tests/Cfa.ACHInterbank.Tests/IntegrationMappingEndToEndTests.cs`, `integration-mapping.api.spec.ts`, `integration-read.models.spec.ts`, `mapping-workspace.store.spec.ts`, `mapping-sets-page.component.spec.ts`.
- `EVID-CFAUX-P6-GUIDED-EDIT-001`: P6.2 cerrado por inspección estática dirigida el `2026-10-13T14:46:07Z`; validación ejecutable diferida a P8.

### 38.3 Estado

- `P6.2 = DONE`.
- `P6 = IN_PROGRESS`.
- `last_completed_micro_prompt = P6.2`.
- `next_micro_prompt = P6.3`.
- `validation_execution = DEFERRED_TO_P8`.
- P6.3 no fue ejecutado en este turno.

## 39. P6.3 — Vista previa por campo y validación humanizada

### 39.1 Implementación

- `Relación de información` incorpora una vista previa por parámetro consumida desde el endpoint backend de preview. Cada tarjeta presenta en lenguaje funcional `Dato requerido por CFA`, `Origen del dato`, `Tratamiento del dato`, `Valor resultante sanitizado` y `Estado / errores de validación`, sin reconstruir valores en Angular.
- El backend de preview se endureció como frontera obligatoria de sanitización. Reutiliza la metadata de sensibilidad de los source providers vigentes (`None`, `Personal`, `Account`, `Financial`, `Technical`), enmascara cuentas/identificadores/referencias sensibles, redacta datos personales/financieros y aplica una política conservadora a valores configurados sin metadata de sensibilidad propia. El payload y el JSON de preview también se construyen únicamente con valores sanitizados.
- Para muestras reales, el preview intenta resolver el origen mediante los source providers backend ya registrados para el método y aplicar el tratamiento mediante el transformation registry existente; si una fuente no dispone del contexto adicional necesario para una muestra, conserva el resolvedor acotado de preview. La sanitización siempre ocurre después de la resolución/tratamiento y antes de construir cualquier DTO de salida.
- El DTO backend de preview expone nombre/descripción funcional CFA, obligatoriedad, nombre funcional de origen y tratamiento, estado/issues de validación e indicador `IsMasked`, además del detalle técnico ya existente. El resultado incluye la validación general obtenida de `IIntegrationMappingValidationService`; Angular no decide validez ni compatibilidad.
- Se agregó un presenter puro en frontend que traduce los códigos backend conocidos a mensajes funcionales en español. Para soporte se conservan código, severidad, categoría, ruta y mensaje técnico backend dentro de `details/summary` colapsado; la UI no presenta stack traces, tokens, credenciales ni valores sin sanitizar.
- `MappingWorkspaceStore` carga preview después del workspace, permite refrescarlo y vuelve a solicitarlo después de un guardado exitoso/refresco del read model. Mantiene estados independientes `loading/error/forbidden` para preview sin ocultar la relación de información y protege contra respuestas de preview obsoletas con una versión de solicitud dedicada.
- La UI diferencia explícitamente la vista previa del valor real persistido, indica que visualizarla no guarda cambios y usa Material/tokens existentes. Se mantienen `OnPush`, foco visible, controles nativos/Material, `details/summary` accesible por teclado y layout amplio/mediano/compacto sin convertir el preview en tarea de scroll horizontal.
- No se implementó publicación ni ninguna capacidad de P6.4.

### 39.2 Pruebas y validación

- Se agregaron/actualizaron pruebas focales no ejecutadas para preview por campo, masking backend de una cuenta real de muestra, ausencia del valor completo en `PayloadPreviewJson`/`RawPreviewJson`, metadata funcional del preview, validación general backend, mensajes humanizados, preservación de detalle técnico colapsado, estados loading/error/forbidden, refresh posterior y navegación/semántica accesible.
- Se añadió una prueba focal de API frontend que confirma que la solicitud de preview solo envía contexto de muestra y no transporta valores sensibles ni campos reconstruidos por Angular.
- No se ejecutaron `npm`, build Angular, Karma/Jasmine, Playwright, build .NET ni tests .NET. `validation_execution = DEFERRED_TO_P8` bajo `IMPLEMENTATION_FIRST_VALIDATION_AT_END`.
- Snapshot fuente autoritativo: `ACHInterbank-R34-TPA9B4-P6.2-DONE-FULL-PROJECT.zip`, SHA-256 `3cf5439a4fe40dff330d3ead3401eb24a91819a4cdb0d3c38e02fef8ba215af7`, 4730 entradas. Se trabajó exclusivamente en modo `PACKAGE_SNAPSHOT`; no se consultó Git ni historial Git.
- Archivos backend modificados P6.3: `src/Cfa.ACHInterbank.Application/Integrations/Dtos/IntegrationMappingSetDtos.cs`, `src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingPreviewService.cs`.
- Archivos frontend agregados P6.3: `features/integrations/presentation/mapping-validation.presenter.ts`, `mapping-validation.presenter.spec.ts`.
- Archivos frontend modificados P6.3: `features/integrations/data-access/integration-api.models.ts`, `integration-mapping.api.spec.ts`, `state/mapping-workspace.store.ts`, `mapping-workspace.store.spec.ts`, `pages/mapping-sets-page.component.ts/.html/.scss`, `mapping-sets-page.component.spec.ts`.
- Pruebas backend modificadas P6.3: `tests/Cfa.ACHInterbank.Tests/IntegrationMappingEndToEndTests.cs`.
- `EVID-CFAUX-P6-PREVIEW-001`: P6.3 cerrado por inspección estática dirigida; validación ejecutable diferida a P8.

### 39.3 Estado

- `P6.3 = DONE`.
- `P6 = IN_PROGRESS`.
- `last_completed_micro_prompt = P6.3`.
- `next_micro_prompt = P6.4`.
- `validation_execution = DEFERRED_TO_P8`.
- P6.4 no fue ejecutado en este turno.

## 40. P6.4 — Conflicto de Revision y publicación humanizada

### 40.1 Implementación

- `Relación de información` detecta `409` por `Revision` obsoleta tanto en edición de parámetro como en publicación. La SPA conserva el workspace visible, muestra un mensaje funcional en español, no mezcla ni sobrescribe cambios ajenos y ofrece `Recargar datos actuales`; esa acción vuelve a solicitar el workspace completo al backend antes de continuar.
- El detalle de conflicto mantiene en `details/summary` colapsado la operación, `Revision` esperada/vigente y el mensaje técnico backend necesario para soporte, sin mostrar stack traces, secretos, tokens ni credenciales.
- Se agregó un `MatDialog` de publicación con foco inicial en el encabezado y restauración de foco. Resume servicio, versión contractual, cobertura y pendientes requeridos; el detalle técnico secundario conserva configuración y `Revision` esperada. `Escape`/cierre no confirma la operación.
- La elegibilidad de publicación permanece autoritativa en backend mediante `workspace.publication.canPublish`, calculada a partir de la validación backend y el estado Draft. El read model añade `pendingRequired`, `newRequiredPending` y `blockingIssues`; Angular no reconstruye reglas de compatibilidad o validez. Como protección fail-closed, CTA, diálogo y store también bloquean cuando el mismo read model backend informa `pendingRequired > 0`, incluso ante metadata contradictoria.
- La publicación exige confirmación explícita, envía `ExpectedRevision` y no aplica éxito optimista. `PublishAsync` compara la revisión esperada, evalúa validación sin persistencia intermedia, incrementa `Revision` al publicar y traduce concurrencia EF a `IntegrationMappingRevisionConflictException`; el controller devuelve `409 ProblemDetails` con revisiones esperada/vigente.
- Después de una respuesta de publicación exitosa, el store ejecuta un nuevo `GET workspace` y solo entonces actualiza estado, cobertura y señal de éxito. No existe merge automático en frontend.
- El escenario contract-driven de un scalar required nuevo se apoya en evidencia de clonación persistida (`ClonedAcrossContractVersions`) para contar `newRequiredPending` sin lógica específica por nombre de campo. Antes de mapear, el workspace reporta exactamente `1 dato nuevo pendiente`; tras seleccionar una fuente existente compatible, guardar y refrescar backend, el conteo pasa a 0 y `CanPublish` puede ser informado por backend.
- Para impedir una ruta interna alternativa de publicación, el editor técnico existente ya no invoca publicación directamente: su acción `Revisar publicación` navega al workspace funcional gobernado por P6.4. El backend sigue aplicando validación y concurrencia como última autoridad.
- Se mantienen `OnPush`, Material/tokens existentes, layout responsive amplio/mediano/compacto, controles semánticos, foco visible y navegación completa por teclado. No se ejecutó P7.1.

### 40.2 Pruebas y validación

- Se agregaron/actualizaron pruebas focales no ejecutadas para: conflicto de `Revision`, preservación del estado local, recarga desde backend, detalle técnico colapsado, diálogo `MatDialog`, servicio/versión/cobertura/pendientes, CTA disabled/enabled según read model backend, confirmación explícita, `ExpectedRevision`, ausencia de éxito optimista, refresh posterior, conflicto durante publicación y ausencia de bypass desde store/CTA/dialog/ruta interna.
- Los Harness/keyboard tests cubren foco inicial lógico, cierre por `Escape` mediante `MatDialogHarness.close()` y restauración de foco al control que abrió el diálogo.
- Se agregó prueba backend del escenario de campo scalar nuevo: contrato destino agrega un required, la clonación no crea regla para ese campo, el workspace reporta `PendingRequired = 1`, `NewRequiredPending = 1` y `CanPublish = false`; después del mapping guiado a un source catalog existente y refresh backend, ambos pendientes son 0 y `CanPublish = true`. No hay lógica productiva ligada al nombre del campo de prueba.
- No se ejecutaron `npm`, build Angular, Karma/Jasmine, Playwright, build .NET ni tests .NET. `validation_execution = DEFERRED_TO_P8` bajo `IMPLEMENTATION_FIRST_VALIDATION_AT_END`.
- Snapshot fuente autoritativo: `ACHInterbank-R34-TPA9B4-P6.3-DONE-FULL-PROJECT.zip`, SHA-256 `8fa67cc09932e75585b266808ff981199c7bea4bd14ccdcb656f32be775a5fd1`, 4732 entradas. Se trabajó exclusivamente en modo `PACKAGE_SNAPSHOT`; no se consultó Git ni historial Git.
- Archivos backend modificados P6.4: `src/Cfa.ACHInterbank.Application/Integrations/Dtos/IntegrationMappingSetDtos.cs`, `src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingSetService.cs`, `src/Cfa.ACHInterbank.Api/Controllers/IntegrationMappingSetsController.cs`.
- Archivos frontend agregados P6.4: `features/integrations/pages/mapping-publication-dialog.component.ts/.html/.scss`.
- Archivos frontend modificados P6.4: `features/integrations/data-access/integration-api.models.ts`, `integration-mapping.api.ts`, `state/mapping-workspace.store.ts`, `pages/mapping-sets-page.component.ts/.html/.scss`, `pages/mapping-editor-page.component.ts/.html`.
- Pruebas agregadas/modificadas P6.4: `tests/Cfa.ACHInterbank.Tests/IntegrationMappingEndToEndTests.cs`, `IntegrationMappingContractVersionCloneTests.cs`, `features/integrations/data-access/integration-mapping.api.spec.ts`, `state/mapping-workspace.store.spec.ts`, `pages/mapping-sets-page.component.spec.ts`, `pages/mapping-editor-page.component.spec.ts`.
- `EVID-CFAUX-P6-PUBLISH-001`: P6.4 cerrado por inspección estática dirigida; validación ejecutable diferida a P8.

### 40.3 Estado

- `P6.4 = DONE`.
- `P6 = DONE`.
- `last_completed_micro_prompt = P6.4`.
- `next_allowed_phase = P7`.
- `next_micro_prompt = P7.1`.
- `validation_execution = DEFERRED_TO_P8`.
- P7.1 no fue ejecutado en este turno.

## 41. P7.1 — Lista y detalle de versiones contractuales

### 41.1 Implementación

- Se implementó la pantalla standalone `Contrato del servicio` en `/integraciones/contrato?method=<operationKey>`, accesible desde cada tarjeta del Centro de integración CFA. La pantalla consume `ContractWorkspaceStore`/adapters existentes y resuelve el servicio por metadata backend antes de consultar versiones y MappingSets.
- La lista presenta las versiones agrupadas funcionalmente como `Borrador`, `Activa` y `Retirada`; el estado backend `Validated` se presenta como `Borrador validado` dentro del grupo de borrador sin usar esa traducción para decidir acciones. Cada tarjeta muestra versión, `Revision`, origen funcional, actor/fecha cuando el backend los entrega y el estado de `Relación de información`; la asociación se toma únicamente del MappingSet backend con el mismo `ContractVersionId` y, si no existe, se informa que no hay relación asociada.
- El detalle prioriza metadata funcional: nombre/propósito del servicio, versión, revisión, notas y parámetros por nombre/descripción/categoría/ayuda/ejemplo. Operación SOAP, namespace, SOAP Action, documento/hash fuente y XML/XSD de parámetros permanecen dentro de `details/summary` colapsados por defecto.
- Para evitar duplicar lifecycle en Angular, `IntegrationContractVersionDto` expone desde backend `Capabilities.IsReadOnly` y `Capabilities.CanEditFunctionalMetadata`, derivados de las mismas reglas de mutabilidad ya aplicadas por el servicio: `Draft/Validated` permiten metadata funcional; `Active/Retired` son read-only. Angular falla cerrado si esas capacidades no llegan.
- La edición de metadata funcional aparece solo con `CanManageAch` y `CanEditFunctionalMetadata=true`; abre un formulario explícito para `DisplayName`, descripción, ayuda, categoría y ejemplo, transporta `ExpectedRevision` y no permite editar `ParameterPath`, XML/XSD, namespace, SOAP Action ni otros datos técnicos. Active/Retired nunca muestran controles de edición.
- El store conserva autoridad backend: permisos se leen de la sesión autenticada, la mutabilidad se toma de capabilities backend, el estado de relación se toma de MappingSets backend y errores/403 no se reinterpretan como elegibilidad local. La actualización exitosa aplica únicamente la revisión y parámetro devueltos por backend.
- Se mantienen `OnPush`, Angular Material/tokens existentes, controles nativos/Material, foco visible, `details/summary` accesible por teclado y grids 3→2→1 dentro de esta pantalla. No se realizó el trabajo responsive global de P7.4.
- No se implementaron carga/análisis WSDL ni diff humanizado de P7.2, configuración técnica P7.3 ni cambios de lifecycle backend. El único cambio backend fue la capacidad de lectura necesaria para evitar duplicar reglas de mutabilidad en UI.

### 41.2 Pruebas y validación

- Se agregaron/actualizaron pruebas focales no ejecutadas para: agrupación por estado, detalle, metadata funcional, relación de información, edición Draft gobernada por capability backend + `CanManageAch`, Active/Retired read-only, fail-closed si faltan capabilities, detalle técnico/XML/XSD colapsado, loading/error/empty/forbidden, controles nativos enfocables/semántica accesible y navegación al contrato desde el Centro de integración.
- Se agregó `IntegrationContractVersionCapabilitiesTests.cs` para fijar en backend la capacidad de edición/read-only de `Draft/Validated/Active/Retired`; no se ejecutó.
- No se ejecutaron `npm`, build Angular, Karma/Jasmine, Playwright, build .NET ni tests .NET. `validation_execution = DEFERRED_TO_P8` bajo `IMPLEMENTATION_FIRST_VALIDATION_AT_END`.
- Snapshot fuente autoritativo: `ACHInterbank-R34-TPA9B4-P6.4-DONE-FULL-PROJECT.zip`, SHA-256 `d3df3297cecef0ebd249a8a85c0756f7397ac605df1a3ef6b8bb6203a7c6ebdf`, 4735 entradas. Se trabajó exclusivamente en modo `PACKAGE_SNAPSHOT`; no se consultó Git ni historial Git.
- Archivos backend modificados/agregados P7.1: `src/Cfa.ACHInterbank.Application/Integrations/Dtos/IntegrationCatalogDtos.cs`, `tests/Cfa.ACHInterbank.Tests/IntegrationContractVersionCapabilitiesTests.cs`.
- Archivos frontend agregados P7.1: `features/integrations/pages/contract-versions-page.component.ts/.html/.scss/.spec.ts`.
- Archivos frontend modificados P7.1: `data-access/integration-api.models.ts`, `data-access/integration-read.models.ts`, `data-access/integration-read.models.spec.ts`, `data-access/integration-contract.api.spec.ts`, `state/contract-workspace.store.ts`, `state/contract-workspace.store.spec.ts`, `integrations.routes.ts`, `integrations.routes.spec.ts`, `presentation/integration-service-card.component.html`, `presentation/integration-service-card.component.spec.ts`.
- `EVID-CFAUX-P7-CONTRACT-VERSIONS-001`: P7.1 cerrado por inspección estática dirigida; validación ejecutable diferida a P8.

### 41.3 Estado

- `P7.1 = DONE`.
- `P7 = IN_PROGRESS`.
- `last_completed_micro_prompt = P7.1`.
- `next_micro_prompt = P7.2`.
- `validation_execution = DEFERRED_TO_P8`.
- P7.2 no fue ejecutado en este turno.


## 42. P7.2 — Stepper WSDL/XSD y diff humanizado

### 42.1 Implementación

- Se agregó flujo standalone Angular Material tipo stepper dentro de `Contrato del servicio`, disponible solo con `CanManageAch`, para seleccionar un archivo local WSDL/XSD, enviarlo al backend, resolver operación cuando aplique, revisar resumen, comparar contra una versión base y confirmar la creación de una nueva versión contractual.
- Angular no parsea XML/WSDL/XSD ni resuelve imports. La selección local solo conserva el `File` y usa los endpoints multipart backend; errores y decisiones de importabilidad se presentan a partir de `IntegrationContractArtifactAnalysisDto`.
- El analyzer backend ahora expone `AvailableOperations`; cuando un WSDL contiene varias operaciones, calcula en backend cuáles son compatibles mediante el mismo análisis seguro y la UI obliga a elegir/reanalizar antes de permitir avanzar. Para XSD standalone se confirma la metadata SOAP explícita requerida por P4.2, inicialmente sembrada desde la versión base backend cuando existe.
- Se agregó preview backend no persistente `methods/{methodId}/contracts/preview/{baseContractVersionId}`. Repite el análisis seguro y delega el diff a `IntegrationContractVersionService.CompareArtifactAsync`, que reutiliza exactamente las reglas de diff/clasificación P4.1 contra parámetros transitorios del artefacto; no crea ContractVersion ni muta lifecycle.
- El diff se presenta en español con categorías funcionales para datos nuevos/eliminados y cambios de obligatoriedad, tipo, cardinalidad, orden, namespace y dirección. `MappingRequired` se muestra literalmente como `Requiere relacionar`; `RequiresDevelopment` como `Requiere desarrollo`. Angular no recalcula ni reclasifica impactos.
- Archivo, tamaño, operación y cantidad de datos se muestran en el resumen; SHA-256, namespace, SOAP Action, nombres técnicos y valores antes/después permanecen en `details/summary` colapsados para soporte.
- La confirmación exige versión/nota y deja explícito que el resultado es `Borrador`. El store acepta éxito únicamente si el backend devuelve `Status=Draft`; una respuesta inesperada `Active` se trata fail-closed como error y no se incorpora al estado local. No existe activación/publicación automática.
- El stepper usa `linear`, estados backend y guards adicionales para impedir saltos cuando análisis, base o preview no son válidos. Se mantienen `OnPush`, Material/tokens existentes, layout local responsive, foco visible, controles semánticos y navegación por teclado. No se implementó P7.3 ni el trabajo responsive global P7.4.

### 42.2 Pruebas y validación

- Se agregaron/actualizaron pruebas focales no ejecutadas para: selección de archivo, análisis backend, operación única/múltiple, preview previo sin persistencia, `MappingRequired`/`RequiresDevelopment`, textos `Requiere relacionar`/`Requiere desarrollo`, error de análisis, resumen/metadata técnica colapsada, creación Draft, rechazo fail-closed de un resultado Active, navegación/guards del stepper y foco/semántica de teclado.
- Se amplió `IntegrationContractArtifactImportTests` para fijar exposición backend de operaciones compatibles y preview de diff sin persistir una versión Draft.
- No se ejecutaron `npm`, build Angular, Karma/Jasmine, Playwright, build .NET ni tests .NET. `validation_execution = DEFERRED_TO_P8` bajo `IMPLEMENTATION_FIRST_VALIDATION_AT_END`.
- Snapshot fuente autoritativo: `ACHInterbank-R34-TPA9B4-P7.1-DONE-FULL-PROJECT.zip`, SHA-256 `0e6e9ae329d6cadd45a72c2fe5342e8165308f2e465bf5afaa8c74a7b55da3ab`, 4740 entradas. Se trabajó exclusivamente en modo `PACKAGE_SNAPSHOT`; no se consultó Git ni historial Git.
- Backend modificado P7.2: `src/Cfa.ACHInterbank.Application/Integrations/Dtos/IntegrationContractArtifactDtos.cs`, `Interfaces/IIntegrationContractArtifactImportService.cs`, `Interfaces/IIntegrationContractVersionService.cs`, `src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationContractArtifactAnalyzer.cs`, `IntegrationContractArtifactImportService.cs`, `IntegrationContractVersionService.cs`, `src/Cfa.ACHInterbank.Api/Controllers/IntegrationContractsController.cs`.
- Frontend agregado P7.2: `features/integrations/pages/contract-import-stepper.component.ts/.html/.scss/.spec.ts`.
- Frontend modificado P7.2: `features/integrations/data-access/integration-api.models.ts`, `integration-contract.api.ts`, `integration-contract.api.spec.ts`, `state/contract-workspace.store.ts`, `contract-workspace.store.spec.ts`, `pages/contract-versions-page.component.ts/.html/.spec.ts`.
- Prueba backend modificada P7.2: `tests/Cfa.ACHInterbank.Tests/IntegrationContractArtifactImportTests.cs`.
- `EVID-CFAUX-P7-WSDL-DIFF-001`: P7.2 cerrado por inspección estática dirigida; validación ejecutable diferida a P8.

### 42.3 Estado

- `P7.2 = DONE`.
- `P7 = IN_PROGRESS`.
- `last_completed_micro_prompt = P7.2`.
- `next_micro_prompt = P7.3`.
- `validation_execution = DEFERRED_TO_P8`.
- P7.3 no fue ejecutado en este turno.


## 43. P7.3 — Conexión técnica avanzada y modo de envío

### 43.1 Implementación

- La configuración SOAP se refactorizó como sección `Conexión técnica avanzada`, manteniendo nombres funcionales de servicio primero y dejando nombre técnico, dirección sanitizada y acción técnica dentro del detalle avanzado. Se presentan modo de envío y estado de configuración con español funcional y sin convertir esta pantalla en experiencia principal.
- La pantalla de conexión pasó a usar `IntegrationConnectionApi` y los modelos de `features/integrations/data-access`, con `OnPush`, Angular Material/tokens existentes, estados explícitos `loading/error/forbidden/empty`, foco visible y layout responsive local. No se realizó el trabajo responsive global de P7.4.
- Para administración, el backend expone `Revision`, `ConfigurationState`, `SanitizedEndpoint`, `SanitizedSoapAction` y capabilities por método. El `GET` administrativo usa una lectura sanitizada separada; `ISoapIntegrationSettingsService.GetAsync` conserva valores runtime sin sanitizar para no modificar clientes SOAP, parsers ni transporte.
- Al solicitar un cambio que habilita o modifica el contexto de envío real, Angular primero llama `POST api/users/soap-integrations/preview-change`. Backend resuelve el método y estado vigentes, valida `ExpectedRevision` y devuelve destino/acción sanitizados y `CanApply`; Angular no implementa allowlists, SSRF, DNS/IP, certificados, hosts, puertos ni bypasses.
- La transición a `Live` con configuración habilitada usa `MatDialog` contextual con nombre funcional, nombre técnico y destino sanitizado entregado por backend. La confirmación es explícita; `Escape`/cancelación no guardan; foco inicial y restauración de foco quedan delegados a `MatDialog` con configuración accesible.
- El frontend falla cerrado si no puede determinar permiso, servicio, `Revision`, estado vigente o destino sanitizado. También bloquea doble submit durante preview, diálogo o guardado.
- El `PUT api/users/soap-integrations` exige `ExpectedRevision`. Un conflicto devuelve HTTP 409 `SOAP_SETTINGS_REVISION_CONFLICT`; la UI humaniza el conflicto, deshabilita nuevos guardados y exige recarga antes de continuar.
- No existe éxito optimista: después del `PUT`, la UI hace un nuevo `GET` y solo entonces hidrata el estado y muestra éxito. Si el refresh posterior falla, exige recarga y no asume el estado devuelto por el `PUT`.
- El backend sanitiza destinos para administración reutilizando la sanitización backend existente y elimina user-info/query/fragment de la acción técnica presentada. Valores sanitizados reenviados por la UI se reconocen como placeholders y no sustituyen los valores sensibles persistidos; no se muestran credenciales, secretos, tokens, certificados privados ni query sensible.
- No se modificaron reglas lifecycle, clientes SOAP, parsers, transporte ni configuración técnica de P7.4. P7.4 no fue ejecutado.

### 43.2 Pruebas y validación

- Se actualizaron pruebas Angular focales no ejecutadas para: sección avanzada; dirección/acción/modo/estado; permisos y forbidden; loading/error; detalle técnico colapsado; confirmación contextual; destino sanitizado; cancelación; doble submit; `ExpectedRevision`; conflicto 409 y recarga obligatoria; refresh posterior sin éxito optimista; fail-closed; delegación de endpoint-security al backend; foco inicial, `Escape`, restauración de foco y `alertdialog` accesible.
- Se ampliaron pruebas backend no ejecutadas para: respuesta administrativa sanitizada y capabilities; preview de cambio hacia envío real; sanitización de destino/acción; conflicto por `ExpectedRevision`; preservación de valores sensibles persistidos cuando la UI reenvía placeholders sanitizados; separación entre lectura runtime y lectura administrativa.
- No se ejecutaron `npm`, build Angular, Karma/Jasmine, Playwright, build .NET ni tests .NET. `validation_execution = DEFERRED_TO_P8` bajo `IMPLEMENTATION_FIRST_VALIDATION_AT_END`.
- Snapshot fuente autoritativo: `ACHInterbank-R34-TPA9B4-P7.2-DONE-FULL-PROJECT.zip`, SHA-256 `25f7340e7c5cf68cb6a4e4b5f9eafa4256dd390f950a0ab3b5aef8e3c523c9e3`, 4744 entradas. Se trabajó exclusivamente en modo `PACKAGE_SNAPSHOT`; no se consultó Git ni historial Git.
- Backend modificado/agregado P7.3: `src/Cfa.ACHInterbank.Api/Controllers/SoapIntegrationSettingsController.cs`, `src/Cfa.ACHInterbank.Application/Security/Dtos/SoapIntegrationSettingsDto.cs`, `SoapEndpointMethodMappingDto.cs`, `Interfaces/ISoapIntegrationSettingsService.cs`, `Exceptions/SoapIntegrationSettingsRevisionConflictException.cs`, `src/Cfa.ACHInterbank.Persistence/Security/Services/SoapIntegrationSettingsService.cs`.
- Frontend modificado P7.3: `features/integrations/data-access/integration-api.models.ts`, `integration-connection.api.ts`, `pages/soap-integration-settings.component.ts/.html/.scss/.spec.ts`.
- Prueba backend modificada P7.3: `tests/Cfa.ACHInterbank.Tests/SoapIntegrationSettingsEffectiveModeTests.cs`.
- `EVID-CFAUX-P7-ADVANCED-CONNECTION-001`: P7.3 cerrado por inspección estática dirigida; validación ejecutable diferida a P8.

### 43.3 Estado

- `P7.3 = DONE`.
- `P7 = IN_PROGRESS`.
- `last_completed_micro_prompt = P7.3`.
- `next_micro_prompt = P7.4`.
- `validation_execution = DEFERRED_TO_P8`.
- P7.4 no fue ejecutado en este turno.

## 44. P7.4 — Responsive y accesibilidad integral

### 44.1 Implementación

- Se endureció la experiencia completa de Integraciones CFA para tres rangos explícitos: compacto `<768px`, mediano `768–1199px` y amplio `>=1200px`, manteniendo el layout amplio como base y aplicando reflow real en cards, resúmenes, edición, preview, publicación, versiones, stepper y conexión avanzada.
- Centro de integración CFA: tarjetas 3→2→1 columnas, copy funcional/técnico con wrap seguro, acciones alcanzables y targets táctiles mínimos de 44 px/48 px en compacto.
- Relación de información, edición de origen/tratamiento, preview/validación y publicación: se preservó el patrón cards/listas como interacción primaria, se reforzaron `min-width: 0`, wrapping de texto, acciones apilables, foco visible y regiones dinámicas anunciables. No se introdujeron tablas horizontales.
- El editor de configuración eliminó scrolls internos anidados en listas/historial; en mediano usa dos columnas con centro de control debajo y en compacto una sola columna, conservando un único scroll de página.
- Versiones contractuales: cards 3→2→1, detalle y metadata con reflow, acciones de edición/consulta apiladas en compacto, texto técnico largo con `overflow-wrap` y estados de carga/vacío anunciables.
- Stepper WSDL/XSD: en compacto las cabeceras se apilan y se ocultan separadores horizontales; formularios, archivo, diff y acciones ocupan el ancho disponible sin forzar desplazamiento horizontal. El backend continúa gobernando la validez de cada paso.
- Conexión técnica avanzada: tabs y branding largo dejan de truncarse por elipsis, grids técnicos colapsan por breakpoint, acciones se apilan en compacto y estados de error/conflicto/capability usan semántica `role=alert`.
- MatDialog de publicación y confirmación de envío real se limita al viewport (`maxWidth`/`maxHeight`), conserva focus trap de Material, restauración de foco, cierre por `Escape` y targets táctiles; el dialog de publicación se marca como `alertdialog`.
- Se reforzó foco visible con tokens existentes, controles nativos con nombre accesible, `aria-live` para estados dinámicos, errores contextuales mediante `mat-error`/`role=alert`, headings/landmarks existentes y comunicación de estado mediante texto además de color. Los pares de colores continúan usando tokens Material/branding existentes como objetivo WCAG AA.
- No se cambiaron reglas de negocio, lifecycle, autoridad backend, parsers, clientes SOAP ni transporte. P7.5 no fue ejecutado.
- `CLOSE-CFAUX-010`: la interacción primaria de Integraciones CFA ya no depende de tabla/scroll horizontal en compacto; el endurecimiento P7.4 cubre reflow, touch, foco, dialogs y ausencia de scroll anidado.

### 44.2 Pruebas y validación

- Se agregaron/actualizaron pruebas focales no ejecutadas para branding extremo, textos funcionales/técnicos largos, contratos CSS de zoom/reflow, breakpoints compacto/mediano/amplio, teclado/foco visible, dialogs y restauración de foco, `aria-live`, targets táctiles y ausencia de tablas horizontales como interacción primaria.
- Se agregó `features/integrations/integrations-responsive-a11y.spec.ts` para fijar breakpoints, touch targets, wrapping, foco visible, reflow compatible con zoom, ausencia de scroll interno anidado, comportamiento compacto del stepper y límites/accessibilidad de dialogs.
- Se ampliaron specs de `IntegrationServiceCardComponent`, `MappingSetsPageComponent`, `MappingEditorPageComponent`, `ContractVersionsPageComponent`, `ContractImportStepperComponent` y `SoapIntegrationSettingsComponent` con casos semánticos/robustez P7.4.
- No se ejecutaron `npm`, build Angular, Karma/Jasmine, Playwright ni pruebas de viewport. `validation_execution = DEFERRED_TO_P8` bajo `IMPLEMENTATION_FIRST_VALIDATION_AT_END`.
- Snapshot fuente autoritativo: `ACHInterbank-R34-TPA9B4-P7.3-DONE-FULL-PROJECT.zip`, SHA-256 `3994ea9185a3cb47d8412c9fbb14c44013dd943aacca0191a25ccbdb4a8d2c99`, 4745 entradas. Se trabajó exclusivamente en modo `PACKAGE_SNAPSHOT`; no se consultó Git ni historial Git.
- Frontend modificado P7.4: `pages/integration-workspace.component.scss`, `presentation/integration-center-shell.component.scss`, `presentation/integration-service-card.component.scss/.spec.ts`, `pages/mapping-sets-page.component.ts/.html/.scss/.spec.ts`, `pages/mapping-editor-page.component.html/.scss/.spec.ts`, `pages/mapping-publication-dialog.component.scss`, `pages/contract-versions-page.component.html/.scss/.spec.ts`, `pages/contract-import-stepper.component.html/.scss/.spec.ts`, `pages/soap-integration-settings.component.ts/.html/.scss/.spec.ts`.
- Prueba agregada P7.4: `features/integrations/integrations-responsive-a11y.spec.ts`.
- `EVID-CFAUX-P7-RESPONSIVE-A11Y-001`: P7.4 cerrado por inspección estática dirigida; validación ejecutable diferida a P8.

### 44.3 Estado

- `P7.4 = DONE`.
- `P7 = IN_PROGRESS`.
- `last_completed_micro_prompt = P7.4`.
- `next_micro_prompt = P7.5`.
- `validation_execution = DEFERRED_TO_P8`.
- P7.5 no fue ejecutado en este turno.


## 45. P7.5 — E2E UX, matriz de robustez y cierre de P7

### 45.1 Implementación de cobertura preparada

- Se completó la cobertura Playwright de Integraciones CFA sin ejecutar suites. Los nuevos specs cubren apertura del Centro de integración CFA, navegación por los tres servicios, prioridad de nombres funcionales frente a nombres técnicos, Relación de información, búsqueda/filtro de pendientes, edición guiada de origen/tratamiento, persistencia seguida de refresh backend, preview/validación sanitizados, publicación cancelada sin mutación, publicación permitida y publicación bloqueada por required pendiente.
- El escenario contract-driven `1 dato nuevo pendiente` queda incluido en E2E: inicia con publicación bloqueada, permite completar el origen/tratamiento con opciones backend y espera el refresh del workspace antes de habilitar publicación.
- La cobertura contractual incluye listado Draft/Active/Retired, stepper WSDL/XSD, análisis backend, selección de operación múltiple, resumen, diff humanizado, impactos `MappingRequired` → `Requiere relacionar` y `RequiresDevelopment` → `Requiere desarrollo`, creación Draft-only y aserción explícita de que la importación no crea Active. Se añadió además cobertura unitaria del flujo XSD standalone gobernado por metadata backend/base.
- Se cubren conflictos de `Revision` en Relación de información y Conexión técnica avanzada, con mensaje humanizado y recarga obligatoria. Los escenarios de permisos solo lectura verifican ausencia de controles de edición/publicación/importación cuando la sesión carece de `CanManageAch`.
- Conexión técnica avanzada queda cubierta con destino sanitizado, cambio hacia modo de envío real, `MatDialog` contextual, cancelación sin mutación, confirmación explícita, refresh posterior y estado resultante obtenido del backend.
- La navegación E2E utiliza únicamente las rutas funcionales vigentes (`/integraciones`, `/integraciones/contrato`, `/integraciones/relacion-informacion`, `/integraciones/conexion`) y verifica que las tarjetas no expongan rutas técnicas redundantes. No se agregaron redirects históricos para satisfacer documentación anterior.
- Se preparó una matriz Playwright explícita para `360x800`, `390x844`, `768x1024`, `1024x768`, `1366x768`, `1440x900` y `1920x1080`. Cada caso recorre Centro, Relación de información, versiones/stepper y conexión avanzada con aserciones de `scrollWidth <= clientWidth`, ausencia de overflow global, ausencia de scroll vertical anidado, dialogs dentro del viewport, ausencia de tablas horizontales como interacción primaria y targets críticos táctiles en compacto.
- Se agregó fixture E2E CFA autocontenido y stateful para métodos, mapping workspace, preview/validación, publicación, contratos, importación/diff, conexión y conflictos. Incluye casos de branding `primary` oscuro/claro, sidebar oscuro, background claro, nombre de entidad largo y contenido funcional/técnico largo.
- Se añadió `@axe-core/playwright` `^4.13.0` como dev dependency y un script focal `test:e2e:cfa`; las suites preparan checks para cero violaciones `critical`/`serious` en rutas principales y escenarios de branding, pero no se ejecutaron.
- La cobertura automatizable de accesibilidad preparada incluye keyboard-only smoke, foco visible, focus trap/restauración/Escape en dialogs, headings, labels, `aria-live`, errores contextualizados, estado con texto además de color y reflow al 200% mediante escalado tipográfico de prueba.
- Se reforzó cobertura Harness/unit existente: `MatStepperHarness` fija orden/selección del import stepper, Harness Material cubre source picker + transformation selector en mapping y selector de modo en conexión. El spec de branding estático ahora incluye versiones contractuales, import stepper, diálogo de publicación, conexión avanzada y diálogo de envío real además de los componentes P5/P6 existentes.
- La inspección estática de P7.5 no modifica reglas de negocio, lifecycle contractual, autoridad backend, clientes SOAP, parsers, transporte ni lógica específica por campo. P8.1 no fue ejecutado.

### 45.2 Validación y release gates

- No se ejecutaron `npm test`, `ng build`, Playwright, axe, .NET, DB, Docker ni UAT. Toda ejecución/certificación permanece `DEFERRED_TO_P8` bajo `IMPLEMENTATION_FIRST_VALIDATION_AT_END`.
- No se declara PASS, VERIFIED ni resultado exitoso de suites. La cobertura quedó escrita y preparada para ejecución en P8; responsive/accesibilidad no se consideran certificados hasta entonces.
- Snapshot fuente autoritativo: `ACHInterbank-R34-TPA9B4-P7.4-DONE-FULL-PROJECT.zip`, SHA-256 `fcbe37c4a0055151088699a4dafe6ee8085437dbd2c46baab898df4c0bd04979`, 4746 entradas. Se trabajó exclusivamente en modo `PACKAGE_SNAPSHOT`; no se consultó Git ni historial Git.
- Archivos E2E agregados P7.5: `web/ach-interbank-ui/e2e/cfa-integrations-ux.spec.ts`, `web/ach-interbank-ui/e2e/cfa-integrations-responsive-a11y.spec.ts`, `web/ach-interbank-ui/e2e/support/cfa-integrations-fixture.ts`.
- Frontend/test tooling modificado P7.5: `web/ach-interbank-ui/package.json`, `web/ach-interbank-ui/package-lock.json`.
- Specs focales modificados P7.5: `features/integrations/pages/mapping-sets-page.component.spec.ts`, `contract-import-stepper.component.spec.ts`, `soap-integration-settings.component.spec.ts`, `features/integrations/integrations-branding-tokens.spec.ts`.
- `EVID-CFAUX-P7-E2E-CLOSE-001`: P7.5 cerrado por inspección estática dirigida; validación ejecutable y certificación diferidas a P8.

### 45.3 Estado

- `P7.5 = DONE`.
- `P7 = DONE`.
- `P7_verification = DEFERRED_TO_P8`.
- `last_completed_micro_prompt = P7.5`.
- `next_allowed_phase = P8`.
- `next_micro_prompt = P8.1`.
- `validation_execution = DEFERRED_TO_P8`.
- P8.1 no fue ejecutado en este turno.

## 46. P8.1 — Prueba de ausencia de callers de implementación preexistente

### 46.1 Inventario KEEP / REMOVE_CANDIDATE / UNRESOLVED

P8.1 usa **implementación preexistente/reemplazada dentro del proyecto** como significado de “legacy”. No se infiere ni afirma que existiera una versión productiva previa. La evidencia se obtuvo únicamente del snapshot autoritativo P7.5, mediante referencias estáticas deterministas y lectura del código directamente relacionado.

| Símbolo / pieza | Definición | Referencias productivas / DI / ruta-config | Referencias de test | Clasificación | Evidencia de decisión |
|---|---|---|---|---|---|
| `ProcTransaccionesRequestMapper` / `IProcTransaccionesRequestMapper` | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcTransaccionesRequestMapper.cs` | Consumido por `IncomingNachaPostProcessingOrchestrator`/`IncomingNachaPostProcessingPipeline`; `BuildSoapBody` tiene caller runtime. Registro por convención `[Scoped]` → interfaz desde `DependencyInjectionService`/Scrutor. `ProcTransacciones:UseContractDrivenRequest` conserva dispatch contract-driven/legacy. | Orchestrator/pipeline y tests de mapper/paridad existentes. | `KEEP` | El mapper sigue en runtime y el camino preexistente es rollback válido mientras P8 no certifique cutover. |
| `ProcContrapartidasRequestMapper` / `IProcContrapartidasRequestMapper` | `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcContrapartidasRequestMapper.cs` | Consumido por `ContrapartidaDispatchJobService`; `BuildSoapBody` tiene callers runtime. Registro `[Scoped]` por convención. `ProcContrapartidas:UseContractDrivenRequest` mantiene ambos caminos; el contract-driven ejecuta además gate de compatibilidad preexistente. | Tests del job/mapper/paridad existentes. | `KEEP` | Tiene callers reales y soporte de feature flag/rollback aún requerido. |
| `RegistrarRespuestaAchSoapRequestMapper` | `src/Cfa.ACHInterbank.External/Connections/RespuestaTransaccionesAch/RegistrarRespuestaAchSoapRequestMapper.cs` | `RespuestaTransaccionesAchGateway` lo instancia y usa cuando `RegistrarRespuestaTransaccion:UseContractDrivenRequest` está desactivado. Existe además registro explícito en `Cfa.ACHInterbank.External/DependencyInjectionService.cs`. | Tests de gateway/caracterización/writer. | `KEEP` | Caller runtime directo + rollback por feature flag. |
| `SoapParameterMapper` | `src/Cfa.ACHInterbank.External/Connections/SoapParameterMapper.cs` | Usado por los paths dictionary de `WscfaachSoapClient` y `WsAxonRespuestaTransaccionesSoapClient`. | Tests específicos de clientes/mapper. | `KEEP` | Sigue atendiendo caminos productivos preexistentes. |
| `ProcTransaccionesRequestParityPolicy` | Persistence/ACH services | Registrada en DI y usada por `ProcTransaccionesRequestMapper` en camino contract-driven. | Tests de paridad existentes. | `KEEP` | Gate de migración todavía activo. |
| `ProcContrapartidasRequestParityPolicy` | Persistence/ACH services | Registrada en DI y usada por `ProcContrapartidasRequestMapper`. | Tests de paridad existentes. | `KEEP` | Gate de migración todavía activo. |
| `RegistrarRespuestaRequestParityPolicy` | External/RespuestaTransaccionesAch | Usada por `RespuestaTransaccionesAchGateway` en camino contract-driven; compara baseline esperado antes de emitir operación. | Cobertura a través de tests del gateway. | `KEEP` | Hardcode de paridad aún necesario para el feature-flag path; no se retira antes de certificación P8. |
| `RegistrarRespuestaSoapOperationAdapter` | External/RespuestaTransaccionesAch | Usado por el gateway para adaptar el body contract-driven a la operación SOAP. | Cobertura del gateway. | `KEEP` | Parte del camino runtime nuevo. |
| `IntegrationCatalogBootstrapper` | `src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationCatalogBootstrapper.cs` | Invocado por `DbInitializer`, servicios de catálogo/import/ContractVersion, `IntegrationMappingBootstrapper` y seeder de escenarios. Crea baseline v1 y metadata de parámetros para los 3 métodos. | Suite de bootstrap/versionado/import existente. | `KEEP` | Es bootstrap/migración seguro requerido. No es la autoridad de ejecución: `IntegrationExecutionContractResolver` selecciona `ContractVersion Active` y exige parámetros/mapping ligados a ese `ContractVersionId`. |
| `IntegrationMappingBootstrapper` | `src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingBootstrapper.cs` | Invocado por `DbInitializer`, readiness, mapping-set service, resolver funcional de contrapartidas y seeder. | Tests de bootstrap/mapping existentes. | `KEEP` | Seed/bootstrap necesario para instalación y compatibilidad; retirarlo en P8.1 rompería bootstrap seguro. |
| `IntegrationMappingScenarioSeeder` | Persistence/Integrations | Gateado a `Development`/`Testing`; usa bootstrappers para escenarios. | Soporte de pruebas/dev. | `KEEP` | No compite como autoridad runtime productiva y conserva utilidad de entorno. |
| `IntegrationMappingAdminService` (frontend) | `web/ach-interbank-ui/src/app/core/services/integration-mapping-admin.service.ts` | Consumido por el editor actual de Relación de información. | Spec del editor actual. | `KEEP` | Continúa reutilizado por ruta funcional vigente. |
| Rutas/páginas/stores actuales de Integraciones CFA | `web/ach-interbank-ui/src/app/features/integrations/**` | Rutas vigentes: raíz, `contrato`, `relacion-informacion`, editor por método/mapping set y `conexion`; lazy mount desde routing de app. | Specs P5–P7 vigentes. | `KEEP` | Son la UX funcional actual: centro, Relación de información, editor, versiones, import stepper y conexión. |
| `SoapIntegrationSettingsService` y DTOs locales (`SoapInputParameterMapping`, `SoapEndpointMethodMapping`, `SoapIntegrationSettings`) | `web/ach-interbank-ui/src/app/core/services/soap-integration-settings.service.ts` | **0 callers productivos externos** en `web/ach-interbank-ui/src/app/**/*.ts`; sin ruta, token DI explícito, config ni feature flag que lo necesite. El componente actual de conexión usa `IntegrationConnectionApi`. `providedIn: 'root'` no constituye caller. | **0 referencias externas de test** detectadas para esos símbolos. | `REMOVE_CANDIDATE` | Funcionalidad reemplazada por `features/integrations/data-access/integration-connection.api.ts`; no soporta rollback, feature flag, migración ni bootstrap. No se elimina en P8.1. |

`UNRESOLVED`: ninguno entre los candidatos inspeccionados. No se promovió a `REMOVE_CANDIDATE` ninguna pieza con ambigüedad dinámica, reflection o DI.

### 46.2 Autoridad contractual y bootstrap hardcoded

- `IntegrationCatalogBootstrapper` y `IntegrationMappingBootstrapper` contienen baseline/metadata/seeds hardcoded de parámetros, orden, tipos y mappings para los tres servicios, pero quedan clasificados `KEEP` porque forman parte de instalación/migración/bootstrap seguro y aún son invocados por lifecycle/readiness.
- El runtime contract-driven común no toma esos catálogos hardcoded como autoridad de ejecución: `IntegrationExecutionContractResolver` exige una única `IntegrationContractVersion` `Active`, liga parámetros y mapping set por `ContractVersionId`, y deriva `OperationName`, `TargetNamespace` y `SoapAction` de esa versión. `IntegrationRequestResolver` consume ese contrato y `ContractDrivenSoapBodyWriter` valida pertenencia/header y serializa por `SortOrder` contractual.
- Los hardcodes de paridad de `RegistrarRespuestaRequestParityPolicy` permanecen temporalmente porque protegen el feature-flag path de migración; por la regla de P8.1 no son candidatos de retiro aún.

### 46.3 Protección del runtime nuevo

- Se agregó `tests/Cfa.ACHInterbank.Tests/CfaSoapP81StaticProtectionTests.cs` como ratchet estático/unitario **no ejecutado aún**. Protege los tres caminos contract-driven, wiring de source/transformation registries, `IIntegrationRequestResolver`, `ContractDrivenSoapBodyWriter`, autoridad `ContractVersion`, permanencia de rollback por feature flags, rutas SPA funcionales y ausencia de nuevos callers hacia el `REMOVE_CANDIDATE`.
- Se agregó `scripts/validate-cfa-p8-1-static.py`, que ejecuta las mismas invariantes por análisis determinista sin build/suite, y genera `artifacts/p8.1-static-validation.json`.
- Resultado del script P8.1: `PASS_STATIC_ONLY`; el candidato frontend registra `0` referencias productivas externas y `0` referencias externas de test. Este resultado no equivale a build/test/runtime certification.

### 46.4 Validación y estado

- Integridad de entrada confirmada: `ACHInterbank-R34-TPA9B4-P7.5-DONE-FULL-PROJECT.zip`, SHA-256 `31b8ed8b1f1b9723682ee5ab3cd67f3c34f44e280f52d039e4b0196df8b4beb1`, 4749 entradas. Modo exclusivo `PACKAGE_SNAPSHOT`; no se usó Git ni historial Git y la ausencia de `.git` no bloquea.
- Validación ejecutada en P8.1: `sha256sum`, búsquedas estáticas deterministas, script local `scripts/validate-cfa-p8-1-static.py` (`PASS_STATIC_ONLY`) y comparación hash/snapshot para verificar cambios.
- Diff de snapshot por hash contra el ZIP fuente: `0` archivos fuente eliminados, `1` archivo fuente existente modificado (`MEMORY_STATE.md`) y `3` archivos agregados (`CfaSoapP81StaticProtectionTests.cs`, `validate-cfa-p8-1-static.py`, `p8.1-static-validation.json`); ningún archivo productivo existente fue modificado o retirado.
- No se ejecutaron build completo, suite .NET completa, `npm test` completo, Playwright, Docker, DB/migraciones ni UAT; quedan para P8.2–P8.4.
- **P8.1 no retiró código productivo.** El único `REMOVE_CANDIDATE` permanece físicamente en el proyecto.
- `P8.1 = DONE`.
- `P8 = IN_PROGRESS`.
- `last_completed_micro_prompt = P8.1`.
- `next_micro_prompt = P8.2`.
- `validation_execution = IN_PROGRESS_P8`.
- P8.2 no fue ejecutado en este turno.

## 47. P8.2 — Retiro controlado de implementación preexistente sin callers

### 47.1 Retiro autorizado y revalidación

- P8.2 mantuvo el alcance cerrado al único `REMOVE_CANDIDATE` certificado por P8.1; no se amplió el inventario de retiro.
- Antes del retiro se reconfirmó estáticamente que `SoapIntegrationSettingsService` y sus DTOs locales no tenían callers productivos externos, referencias de test externas, registro DI explícito requerido, ruta, config, feature flag, factory/delegate, import indirecto ni uso dinámico identificable. La ruta vigente `conexion` carga `SoapIntegrationSettingsComponent`, y el componente/store actuales consumen `IntegrationConnectionApi`.
- Se retiró `web/ach-interbank-ui/src/app/core/services/soap-integration-settings.service.ts`. Los DTOs locales `SoapInputParameterMapping`, `SoapEndpointMethodMapping` y `SoapIntegrationSettings` definidos exclusivamente en ese archivo se retiraron junto con él. Tipos homónimos locales usados por fixtures/e2e no son imports/callers de ese servicio y no fueron tocados.
- Evidencia final: `scripts/validate-cfa-p8-1-static.py` fue actualizado como ratchet post-retiro y produjo `PASS_STATIC_ONLY`; `remove_candidate_retired = true`, `remove_candidate_external_production_refs = []`, `remove_candidate_external_test_refs = []` y `remove_candidate_import_path_refs = []` en `artifacts/p8.1-static-validation.json`.

### 47.2 KEEP y autoridad contractual

- Permanecieron `KEEP` y sin retiro: `ProcTransaccionesRequestMapper`, `ProcContrapartidasRequestMapper`, `RegistrarRespuestaAchSoapRequestMapper`, `SoapParameterMapper`, políticas de paridad, `RegistrarRespuestaSoapOperationAdapter`, `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper`, `IntegrationMappingScenarioSeeder`, `IntegrationMappingAdminService`, runtime `ContractVersion`, source/transformation registries, `ContractDrivenSoapBodyWriter` y SPA funcional actual.
- Los feature flags `UseContractDrivenRequest`, fallbacks y rollback preexistentes permanecieron intactos; P8.2 no certifica arquitectura única activa ni fuerza defaults.
- La revalidación estática reconfirmó `ContractVersion Active` como autoridad del runtime contract-driven: `IntegrationExecutionContractResolver` resuelve `IntegrationContractVersions` activas y liga mappings/parámetros por `ContractVersionId`; `IntegrationRequestResolver` consume `ResolveActiveAsync`; `ContractDrivenSoapBodyWriter` valida contrato y ordena por `SortOrder`. No apareció evidencia contradictoria de una autoridad runtime hardcoded que compita con ese flujo.
- `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` e `IntegrationMappingScenarioSeeder` permanecieron `KEEP`; no se retiró baseline, seed o metadata requerida para instalación/migración segura.
- No se retiró ni modificó ningún parser específico, validator, guard, control funcional, idempotencia, resiliencia, source provider, transformation, SOAP client, `SoapAction`, namespace, orden contractual, NACHA-M, regla ACH Colombia/CENIT, ciclos, compensación, liquidación, causal, routing financiero o feature flag. No se cambió XML SOAP.

### 47.3 Protección y validación focal

- `CfaSoapP81StaticProtectionTests.cs` se actualizó para exigir que el archivo retirado permanezca ausente, que no reaparezcan referencias productivas/de test/import-path y que `IntegrationConnectionApi` siga siendo el camino vigente; las protecciones existentes de los tres caminos contract-driven, registries, `ContractVersion`, writer, rollback y rutas SPA permanecen.
- `scripts/validate-cfa-p8-1-static.py` se ejecutó después del retiro con resultado `PASS_STATIC_ONLY` y conserva/expande evidencia de integridad de los elementos `KEEP`.
- Validación backend focal: `NOT_RUN_ENVIRONMENT`; `dotnet` no está disponible en el entorno, por lo que no se ejecutaron `CfaSoapP81StaticProtectionTests` ni tests contract-driven .NET. No se declara PASS.
- Validación frontend focal: `NOT_RUN_ENVIRONMENT`; `node_modules` y Angular CLI local están ausentes. No se reinstalaron dependencias a ciegas y no se ejecutaron build Angular, specs de conexión/componente/API ni routing. No se declara PASS.
- No se ejecutaron suite .NET completa, `npm test` completo, Playwright completo, Docker, DB/migraciones, UAT ni gauntlet completo; quedan para P8.3–P8.4 según corresponda.

### 47.4 Estado

- Snapshot de entrada P8.1 verificado por SHA-256 exacto `0b0b74625cdbbd3d061a79d6cf84c8908a1a927a622ae9c66911018e6f7e8aff`, con 4753 entradas. Modo `PACKAGE_SNAPSHOT`; no se usó Git ni historial Git.
- `SoapIntegrationSettingsService` retirado con evidencia final de 0 callers productivos, 0 callers de test y 0 imports/path runtime en `src/app`; `IntegrationConnectionApi` permanece como camino vigente.
- `P8.2 = DONE`.
- `P8 = IN_PROGRESS`.
- `last_completed_micro_prompt = P8.2`.
- `next_micro_prompt = P8.3`.
- `validation_execution = IN_PROGRESS_P8`.
- No se marca P8 como DONE, programa COMPLETE, gauntlet PASS, UAT PASS ni release ready.

## 48. P8.3 — Gauntlet técnico y UX

### 48.1 Snapshot y entorno

- Fuente P8.2 verificada antes de modificar: `ACHInterbank-R34-TPA9B4-P8.2-DONE-FULL-PROJECT.zip`, SHA-256 exacto `89a7de3078ae2974144b2c26b453e2b7315af7e229cc762c6a2fe9a316b554c9`, 4752 entradas. Modo exclusivo `PACKAGE_SNAPSHOT`; no se usó Git ni historial Git.
- `dotnet` no está disponible. `node`/`npm` existen, pero `node_modules`, Angular CLI local y Playwright Node local no están disponibles; no se reinstalaron dependencias. Por ello, los casos backend y UX que requieren ejecución .NET/Angular/Playwright quedan `NOT_RUN_ENVIRONMENT`, sin convertir la limitación en PASS.
- No se hicieron llamadas reales al CFA externo, no se usaron secretos/certificados/PII reales y no se hizo network externo para XXE.

### 48.2 Casos del gauntlet

- Parameter injection no registrado: `NOT_RUN_ENVIRONMENT`; se amplió `IntegrationContractDrivenSoapBodyWriterTests` para los tres servicios y el ratchet estático confirma rechazo por pertenencia/metadata.
- XML element name malicioso: `NOT_RUN_ENVIRONMENT`; se agregó regresión con cierres de tags, namespaces/atributos e identificadores inválidos y el ratchet confirma `VerifyNCName` + nombres derivados del contrato.
- WSDL XXE / DTD: `NOT_RUN_ENVIRONMENT`; existen fixtures DTD, XXE `file://` y referencia HTTPS, se conserva `DtdProcessing.Prohibit`, `XmlResolver = null`, `MaxCharactersFromEntities = 0` y rechazo de referencias externas. Se agregó regresión de facet XSD no soportada fail-closed.
- Required missing: `NOT_RUN_ENVIRONMENT`; se agregó regresión explícita de required ausente y se conservan guards de omitted/null y validación de mapping requerido.
- Type mismatch: `NOT_RUN_ENVIRONMENT`; regresión ampliada para entero, decimal y boolean inválidos; null requerido ya estaba cubierto. Facetas de longitud/formato/dominio no representables por el modelo se rechazan fail-closed en importación.
- Mapping a source de otro method: `NOT_RUN_ENVIRONMENT`; regresión nueva valida source catalog de otro método y permanece guard runtime `SOURCE_METHOD_INCOMPATIBLE`.
- Mapping a parameter de otro ContractVersion: `NOT_RUN_ENVIRONMENT`; regresión nueva intenta `ParameterId` de otra versión y permanece validación exacta por `ContractVersionId`.
- Concurrent update: `NOT_RUN_ENVIRONMENT`; permanecen tests de revision conflict para mapping/activation/rollback y guards `ExpectedRevision`/`DbUpdateConcurrencyException` bajo transición serializable.
- Activation con mapping incompleto: `NOT_RUN_ENVIRONMENT`; permanece `Activation_IncompleteMapping_ShouldFailWithoutPartialState`.
- Rollback sin mapping compatible: `NOT_RUN_ENVIRONMENT`; permanece `Rollback_WhenHistoricalMappingBecameIncompatible_ShouldFailWithoutPartialState`.
- Field order incorrecto: `NOT_RUN_ENVIRONMENT`; regresión ampliada a los tres servicios y writer ordena por `SortOrder` contractual, no por inserción.
- Output parameter enviado como input: `NOT_RUN_ENVIRONMENT`; se agregó regresión explícita y writer rechaza `RESOLVED_REQUEST_OUTPUT_PARAMETER`.
- Mobile overflow: `NOT_RUN_ENVIRONMENT`; matriz focal existente incluye 360x800, 390x844 y 768x1024; ratchet estático de reflow/overflow continúa PASS.
- Keyboard trap: `NOT_RUN_ENVIRONMENT`; Playwright focal existente cubre dialogs, focus trap, Escape y restauración de foco; no se ejecutó por entorno.
- Branding sin contraste: `NOT_RUN_ENVIRONMENT`; Playwright/axe focal existente no se ejecutó; guard estático de branding sin hex/rgb/hsl nuevo pasó.
- Technical code expuesto como lenguaje principal: `PASS`; reproducción estática pre-fix falló por dos errores primarios del Mapping Editor que exponían “mapping set”. Se corrigieron al lenguaje funcional “configuración de relación de información”, se eliminó el código de método del mensaje primario de mismatch, se actualizó spec y el ratchet post-fix pasó.

### 48.3 Defectos, regresión y validación

- Defecto confirmado/corregido: `P8.3-UX-LANGUAGE-001` — lenguaje técnico “mapping set” expuesto como error primario en `mapping-editor-page.component.ts`. Corrección mínima, sin refactor cosmético ni cambio de negocio.
- No se confirmó otro defecto pendiente mediante la evidencia ejecutable disponible.
- Tests/regresiones actualizados: `IntegrationContractDrivenSoapBodyWriterTests.cs`, `IntegrationGenericEngineSourceResolutionTests.cs`, `IntegrationMappingEndToEndTests.cs`, `IntegrationContractActivationRollbackTests.cs`, `IntegrationContractArtifactImportTests.cs`, `mapping-editor-page.component.spec.ts`.
- Se agregó `scripts/validate-cfa-p8-3-gauntlet.py` y evidencia `artifacts/p8.3-gauntlet-validation.json` / `artifacts/p8.3-gauntlet-execution.json`. El script ejecuta primero el ratchet P8.1/P8.2 y luego guardas P8.3. Resultado final: `PASS_STATIC_GAUNTLET`.
- Backend gauntlet: `NOT_RUN_ENVIRONMENT`; no se ejecutaron tests .NET porque `dotnet` no existe en el entorno.
- Frontend/UX gauntlet: `NOT_RUN_ENVIRONMENT`; no se ejecutaron Angular build/unit/Playwright porque faltan dependencias locales.
- `GAUNTLET_PASSED` = **NO CERTIFICADO**. No se marca `VERIFIED`, `UAT_READY`, `USER_ACCEPTED`, `program_status = COMPLETE` ni `P8 = DONE`.

### 48.4 Estado

- `P8.3 = DONE`.
- `P8 = IN_PROGRESS`.
- `last_completed_micro_prompt = P8.3`.
- `next_micro_prompt = P8.4`.
- `validation_execution = IN_PROGRESS_P8`.
- Siguiente acción: ejecutar únicamente P8.4.

## 49. P8.4 — Cierre global bloqueado por entorno

### 49.1 Pre-flight y gates estáticos

- Fuente P8.3 verificada antes de modificar: `ACHInterbank-R34-TPA9B4-P8.3-DONE-FULL-PROJECT.zip`, SHA-256 exacto `829db186cad573f6cd4348c5555f7d6cbb52b34ee80e265bbe10f2c31174990d`, exactamente 4755 entradas, ZIP íntegro y sin `.git`. Modo exclusivo `PACKAGE_SNAPSHOT`; no se usó Git ni historial Git.
- `python scripts/validate-solution-projects.py`: PASS.
- `python scripts/validate-cfa-p8-1-static.py`: PASS.
- `python scripts/validate-cfa-p8-3-gauntlet.py`: `PASS_STATIC_GAUNTLET`. No existe un script adicional `validate-cfa-soap-ux-architecture.py`; el ratchet P8.3 concentra las guardas CFA-SOAP-UX aplicables.
- La revalidación estática conserva `ContractVersion Active` como autoridad del motor nuevo, `ContractVersionId` en mappings, writer/registries/SPA actuales, ausencia del frontend `SoapIntegrationSettingsService`, ausencia de nuevos callers al retirado, no clickable `div/li`, no branding fijo nuevo y language guard. Estos PASS son necesarios pero no certifican cierre.

### 49.2 Gates ejecutables y cutover

- Backend Release build: `NOT_RUN_ENVIRONMENT`; `dotnet` no existe en el host (`command not found`). Full backend regression: `NOT_RUN_ENVIRONMENT` por la misma causa.
- Paridad ejecutable de `Proc_Transacciones`, `Proc_Contrapartidas` y `RegistrarRespuestaTransaccion`: `NOT_RUN_ENVIRONMENT`; los tests existen pero no pudieron ejecutarse sin .NET.
- No-code new-field scenario: `NOT_RUN_ENVIRONMENT`; `IntegrationContractNoCodeVersioningEndToEndTests` no pudo ejecutarse sin .NET.
- Migraciones/data preservation PostgreSQL/SQL Server: `NOT_RUN_ENVIRONMENT`; no hay `dotnet` ni Docker/provider runtime disponible.
- P8.3 backend gauntlet: `NOT_RUN_ENVIRONMENT`; no se convierte cobertura estática en PASS ejecutable.
- Frontend dependency restore: el `npm ci` canónico se intentó una sola vez y agotó el tiempo de entorno sin crear `node_modules`. Angular build y full unit regression quedaron `NOT_RUN_ENVIRONMENT` (`ng` no disponible); Playwright/E2E quedó `NOT_RUN_ENVIRONMENT` porque el runner Node local no está instalado. No se reinstalaron dependencias mediante mecanismos alternos.
- `GAUNTLET_PASSED = NO`.
- Cutover contract-driven: NO realizado. Los tres `UseContractDrivenRequest` continúan con default efectivo `false` y no se encontró override runtime explícito en el snapshot. P8.4 prohíbe cambiar esos defaults sin full backend regression + paridad de los tres servicios ejecutadas y PASS.

### 49.3 Seguridad, invariantes y defectos

- Secret review: PASS. El diff acumulado P8 no contiene candidatos de alta confianza a password/token/client secret/private key/certificado embebido. La revisión amplia encontró solo fixtures/ejemplos sintéticos o referencias dinámicas a entorno/runtime; no se persistieron valores sensibles en la evidencia.
- Invariantes normativos/negocio: PASS por diff estático file-level P8.1→P8.3. Los cambios productivos se limitaron al retiro frontend aprobado de `soap-integration-settings.service.ts` y a lenguaje funcional del Mapping Editor; no se modificaron NACHA-M, ciclos, causales, compensación, liquidación, clasificación monetaria, códigos de transacción, routing financiero ni response parsers.
- Defectos confirmados en P8.4: ninguno. Defectos corregidos en P8.4: ninguno. No se modificó código productivo ni se hizo cleanup adicional.
- `MEMORY.md` raíz sigue sin IDs `CFAUX`; no se modificó.
- Evidencia consolidada: `artifacts/p8.4-final-validation.json`.

### 49.4 Estado de cierre

- `P8.4 = BLOCKED`.
- `P8 = IN_PROGRESS`.
- `active_phase_status = IN_PROGRESS`.
- `active_micro_prompt = P8.4`.
- `active_micro_prompt_status = BLOCKED`.
- `last_completed_micro_prompt = P8.3`.
- `next_micro_prompt = P8.4`.
- `next_allowed_phase = P8`.
- `program_status = IN_PROGRESS`.
- `validation_execution = IN_PROGRESS_P8`.
- No se marca `program_status = COMPLETE`, `P8 = DONE`, `GAUNTLET_PASSED`, `VERIFIED`, `UAT_READY`, `USER_ACCEPTED` ni release ready.
- Entorno requerido para reanudar: .NET SDK compatible con la solución; Docker y providers/credenciales locales de prueba autorizadas para PostgreSQL y SQL Server; dependencias frontend restaurables, Angular CLI local, Chromium/Playwright Node local. Después deberán ejecutarse los gates completos y, solo si full backend regression + paridad de los tres servicios pasan, evaluar el cutover y repetir las regresiones afectadas.


## 50. P8.4-A — External Validation Handoff

### 50.1 Alcance y pre-flight

- Subincremento `P8.4-A — External Validation Handoff` completado únicamente como preparación de ejecución externa; no reemplaza ni completa el micro-prompt canónico `P8.4`.
- Fuente verificada antes de modificar: `ACHInterbank-R34-TPA9B4-P8.4-BLOCKED-FULL-PROJECT.zip`, SHA-256 exacto `ce85f07b0374e829810a5cf38fba69443ce53e468bcb8f5efa03b8e903d040c9`, exactamente 4756 entradas, modo `PACKAGE_SNAPSHOT`, sin `.git` y sin uso de Git/historial Git.
- Manifiesto de entrada persistido en `artifacts/p8.4-a-input-file-manifest.json`: 4154 archivos con SHA-256 file-level; fingerprint `496fc0f395cb9c8d280c2325c131f5725482eb6755dcd468486698213bc9f387`.
- No se intentó instalar/ejecutar .NET, npm restore, Angular, Playwright, browsers, Docker, PostgreSQL, SQL Server, migraciones reales ni conexiones CFA/SOAP reales. Todo gate que requiere esas capacidades permanece `EXTERNAL_EXECUTION_REQUIRED`.

### 50.2 Handoff ejecutable

- Matriz humana creada: `docs/CFA_SOAP_UX_MAPPING_IMPLEMENTATION_PACKAGE_2026-10-12/P8_4_EXTERNAL_VALIDATION_HANDOFF.md`.
- Matriz machine-readable creada: `artifacts/p8.4-external-validation-matrix.json`.
- El handoff contiene comandos reales derivados de `AGENTS.md`, `TESTING_AND_RELEASE_GATES.md`, `package.json`, `playwright.config.ts` y tests existentes; inventaría build/regresión backend, paridad de los tres servicios, lifecycle/version isolation, WSDL security, resolver/registries, activation/rollback/concurrency, no-code scenario, backend gauntlet, frontend build/unit/E2E y UX gauntlet.
- Se documentó runbook A–S con fail-fast, evidencia requerida, pre-cutover decision gate, cutover manual mínimo y regresión post-cutover.
- Se documentó que el cutover solo puede modificar `src/Cfa.ACHInterbank.Api/appsettings.json` después de los prerequisitos PASS, agregando `UseContractDrivenRequest=true` a `ProcTransacciones`, `ProcContrapartidas` y `RegistrarRespuestaTransaccion`; no se autoriza retirar fallback/mappers/parsers.

### 50.3 Cobertura adicional preparada

- Se identificó un único `TEST_COVERAGE_GAP` evitable para el gate runtime de migración/data preservation CFA: los tests existentes `IntegrationContractVersionMigrationTests` y `IntegrationContractVersionConstraintMigrationTests` verificaban fuentes/snapshots, pero no ejecutaban clean-create/upgrade reales en ambos providers.
- Se agregó `tests/Cfa.ACHInterbank.Tests/IntegrationContractVersionProviderMigrationTests.cs` como harness externo determinista dual-provider. Requiere `CFA_P8_REQUIRE_DATABASES=true`, `CFA_P8_SQLSERVER_CONNECTION_STRING` y `CFA_P8_POSTGRES_CONNECTION_STRING`; crea recursos de prueba aislados, ejecuta latest/upgrade, valida backfill `ContractVersionId`, preservación de parameter/mapping/rule, revision, baseline active y uniqueness/version isolation, y limpia los recursos al terminar.
- El test nuevo NO fue ejecutado en ChatGPT chat mode; su estado es `EXTERNAL_EXECUTION_REQUIRED`, no PASS.
- No quedan `TEST_COVERAGE_GAP` evitables identificados para los gates P8.4 inventariados; la cobertura pendiente requiere ejecución externa real.

### 50.4 Estado contractual y UX preservado

- `ContractVersion Active` continúa como autoridad contractual del motor nuevo; mappings siguen aislados por `ContractVersionId`.
- `SoapIntegrationSettingsService` frontend retirado en P8.2 continúa ausente; `IntegrationConnectionApi` sigue siendo el camino vigente.
- `P8.3-UX-LANGUAGE-001` permanece corregido estáticamente y su prueba ejecutable está incluida en el handoff externo.
- No hubo cambios de producción, XML SOAP, NACHA-M, ciclos, causales, compensación, liquidación, routing financiero, parsers funcionales ni reglas ACH Colombia/CENIT.
- Cutover NO realizado. Los defaults efectivos del snapshot continúan:
  - `ProcTransacciones:UseContractDrivenRequest=false`
  - `ProcContrapartidas:UseContractDrivenRequest=false`
  - `RegistrarRespuestaTransaccion:UseContractDrivenRequest=false`
- `GAUNTLET_PASSED = NO` hasta ejecución externa completa.

### 50.5 Estado actual

- `P8.4-A = DONE`.
- `P8.4 = BLOCKED`.
- `P8 = IN_PROGRESS`.
- `active_phase_status = IN_PROGRESS`.
- `active_micro_prompt = P8.4`.
- `active_micro_prompt_status = BLOCKED_EXTERNAL_EXECUTION`.
- `last_completed_micro_prompt = P8.3`.
- `next_micro_prompt = P8.4`.
- `next_allowed_phase = P8`.
- `program_status = IN_PROGRESS`.
- `validation_execution = EXTERNAL_EXECUTION_REQUIRED`.
- `GAUNTLET_PASSED = NO`.
- No se marca `P8 = DONE`, `P8.4 = DONE`, `program_status = COMPLETE`, `VERIFIED`, `UAT_READY` ni `USER_ACCEPTED`.
- Siguiente acción: ejecutar externamente el runbook de `P8_4_EXTERNAL_VALIDATION_HANDOFF.md`; solo después de todos los gates obligatorios PASS puede reanudarse el cierre canónico P8.4 y evaluar el cutover.
