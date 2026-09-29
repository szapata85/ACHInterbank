# Índice de documentación — ACHInterbank

Este índice reduce la ambigüedad entre documentación vigente, evidencia histórica y artefactos de investigación.

## Leer primero

| Documento | Uso |
|---|---|
| `README.md` | Entrada al repositorio, build y estructura. |
| `docs/CURRENT_PROJECT_STATUS.md` | Estado consolidado actual, código vs. fuentes y pendientes. |
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
