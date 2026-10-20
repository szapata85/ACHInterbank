# Índice de documentación — ACHInterbank

Este índice reduce la ambigüedad entre documentación vigente, evidencia histórica y artefactos de investigación.

## Leer primero

| Documento | Uso |
|---|---|
| `README.md` | Entrada al repositorio, build y estructura. |
| `docs/CURRENT_PROJECT_STATUS.md` | Estado consolidado actual, código vs. fuentes y pendientes; incluye el checkpoint interno ACH Colombia 7 ciclos y sus bloqueos de activación. |
| `docs/ACH_COLOMBIA_INTERNAL_CLOSURE.md` | Handoff corto de FASE 1–9: implementación completada, validación ejecutable final pendiente, operación y gates antes de homologación/producción. |
| `docs/BUILD_AND_TROUBLESHOOTING.md` | Restore/build, Visual Studio y NU1105. |
| `MEMORY.md` | Decisiones durables y cierres técnicos aceptados. |
| `AGENTS.md` | Reglas permanentes para agentes de desarrollo. |
| `docs/normativa/SOURCE_MANIFEST.md` | Versiones y hashes de las fuentes normativas. |

## Normativa

| Ruta | Contenido |
|---|---|
| `docs/normativa/md/ACH-Colombia-V35.md` | Baseline ACH Colombia vigente para este proyecto. |
| `docs/normativa/md/CENIT-DSP-152-Anexo-2.md` | Ciclos y operación CENIT. |
| `docs/normativa/md/CENIT-Anexo-A-Causales-Devolucion.md` | Causales CENIT de devolución / devolución de devolución. |
| `docs/normativa/md/CENIT-Anexo-B-Causales-Rechazo.md` | Causales CENIT de rechazo de archivo. |
| `docs/normativa/md/Archivos de CENIT/Manual de Especificaciones Formato NACHA-M CENIT.md` | Layout NACHA-M CENIT, 7 mayo 2026. |
| `docs/normativa/md/Archivos de CENIT/Manual de Usuario CENIT.md` | Operación CENIT-WEB/PO y sesiones. |
| `docs/normativa/nacha-m/` | Matrices de trazabilidad y reglas derivadas. |

## Arquitectura y operación

- `docs/architecture/`: decisiones, contratos, mapeos y modelos técnicos. Úselo como evidencia de diseño, no como sustituto de normativa o runtime.
- `docs/operations/`: runbooks y operación.
- `docs/ai/`: auditorías/planes generados durante cierres de gaps; pueden representar un punto histórico anterior al baseline vigente.

## UAT / release

- `docs/uat/`: escenarios y evidencia de pruebas de diferentes cortes.
- `docs/go-live-readiness/`: checklist y análisis de readiness de cortes anteriores.
- `docs/comite-go-no-go/`: material de comité histórico.

La existencia de evidencia de un corte anterior **no transfiere** `UAT_READY`, `USER_ACCEPTED`, `RELEASE_READY` o `GO` al paquete actual. La certificación debe corresponder al commit/artefacto exacto.

## Auditorías SPA

`docs/ux/SPA_ROUTE_FUNCTIONAL_AUDIT.md` conserva el inventario de rutas y las clasificaciones `KEEP/FIX/MERGE/REDIRECT/BLOCKED`. Los `BLOCKED` siguen siendo backlog salvo evidencia posterior explícita que los cierre.

## Qué no duplicar

Para nuevos trabajos, preferir actualizar `docs/CURRENT_PROJECT_STATUS.md` y `MEMORY.md` con referencias a evidencia en vez de crear múltiples archivos de estado con el mismo contenido. Los documentos de una ejecución puntual pueden conservarse bajo `docs/uat/evidencias` o una carpeta de auditoría específica.


## Backlog vigente

- `docs/PENDING_WORK_CLASSIFICATION.md` — fuente canónica para separar desarrollo propio, validación ejecutable, externos y UAT; registra el frente ACH Colombia como **IMPLEMENTADO / VALIDACIÓN EJECUTABLE FINAL PENDIENTE / UAT PENDIENTE** y mantiene como **BLOCKED / NEEDS CLARIFICATION** el `EffectiveFrom` oficial y la hora inicial del Ciclo 4.
- `MEMORY.md` — conserva el checkpoint durable: candidata `ACHCOL-7C-CANDIDATE` **INACTIVE / NOT ACTIVATABLE** hasta resolver ambos bloqueos.

## Administración de terceros, cuentas y prenotificaciones — TPA-GOV-001

Programa funcional nuevo inicializado el 2026-10-11. No forma parte de R11 ni reabre el refactor R0–R10.

| Documento | Uso |
|---|---|
| `docs/ai/third-party-accounts/START_HERE.md` | Entrada al paquete y orden de lectura. |
| `docs/architecture/THIRD_PARTY_ACCOUNT_GOVERNANCE_ARCHITECTURE.md` | Arquitectura objetivo: master genérico agnóstico de cámara + validación/elegibilidad contextual. |
| `docs/architecture/THIRD_PARTY_ACCOUNT_SILENT_ONBOARDING_CONTRACT.md` | Invariantes que preservan onboarding silencioso single/bulk. |
| `docs/architecture/THIRD_PARTY_ACCOUNT_GOVERNANCE_IMPLEMENTATION_PLAN.md` | Fases TPA0–TPA10 y gates. |
| `docs/adr/ADR-TPA-002-GENERIC-MASTER-CONTEXTUAL-VALIDATION.md` | Decisión: master genérico; cámara/naturaleza sólo en validación/elegibilidad. |
| `docs/adr/ADR-TPA-006-ELIGIBILITY-IS-DERIVED.md` | Decisión: elegibilidad derivada, no estado global de la cuenta. |
| `docs/normativa/THIRD_PARTY_ACCOUNT_NORMATIVE_TRACEABILITY.md` | ACH Colombia V35/CENIT: crédito, débito, prenotificación y causales relevantes. |
| `docs/data/THIRD_PARTY_ACCOUNT_DATA_MODEL_MIGRATION.md` | Esquema aditivo: ThirdParty/Account genéricos, validaciones contextuales, backfill y cutover. |
| `docs/api/THIRD_PARTY_ACCOUNT_API_CONTRACT.md` | API target y prohibición de resultado interbancario manual. |
| `docs/ux/THIRD_PARTY_ACCOUNT_UX_SPEC.md` | UX cliente-céntrica + consola operacional. |
| `docs/testing/THIRD_PARTY_ACCOUNT_TEST_STRATEGY.md` | Characterization y gates multi-DB/SPA. |
| `docs/ai/THIRD_PARTY_ACCOUNT_GOVERNANCE_STATE.md` | State memory del programa. |
| `docs/ai/third-party-accounts/MASTER_PROMPT_CHATGPT.md` | Prompt maestro, inicialmente TPA0 only. |

## Navegación y menú humanizado — NAV-MENU-IA-001

Programa incremental documentado para reorganizar el menú del SPA por seed, con copy en español claro, separación de parámetros/operación/administración, herramientas técnicas autorizadas y flujo sin Git basado en ZIP full.

| Documento | Uso |
|---|---|
| `docs/ai/navigation-menu/START_HERE.md` | Entrada obligatoria y orden de lectura. |
| `docs/ai/navigation-menu/MEMORY_STATE.md` | Estado vivo de la implementación gradual. |
| `docs/ai/navigation-menu/PHASE_PROMPTS_SHORT.md` | Prompts cortos para ChatGPT en modo chat. |
| `docs/architecture/NAVIGATION_MENU_INFORMATION_ARCHITECTURE.md` | Árbol objetivo aprobado. |
| `docs/architecture/NAVIGATION_MENU_IMPLEMENTATION_PLAN.md` | Fases NAV1–NAV6. |
| `docs/ux/NAVIGATION_MENU_ROUTE_MATRIX.md` | Ruta → nombre → padre → clasificación. |
| `docs/security/NAVIGATION_MENU_SECURITY_MODEL.md` | Roles/permisos, incluidas herramientas de prueba. |
| `docs/operations/NAVIGATION_MENU_NO_GIT_ZIP_WORKFLOW.md` | Handoff por ZIP full sin Git. |

