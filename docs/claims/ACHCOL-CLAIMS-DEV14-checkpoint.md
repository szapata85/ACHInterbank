# ACHCOL-CLAIMS-DEV14 — checkpoint 3E

Fecha: 2026-09-30.

## Estado

**Implementación completada; validación ejecutable pendiente.**

El alcance acordado está implementado de extremo a extremo: Domain/Persistence, Application/API, permisos propios de Reclamaciones y SPA “Reclamaciones ACH”. En este host no está disponible `dotnet`, por lo que build y tests backend no se declaran ejecutados. Node 22.16.0 y npm 10.9.2 están disponibles, pero el único `npm ci` de 3E no completó por resolución de red `EAI_AGAIN`; por ello tampoco se declaran ejecutados build ni tests Angular.

Como validación estática del checkpoint se ejecutó `scripts/validate-solution-projects.py` con resultado `SOLUTION GRAPH: OK` y una revisión dirigida de permisos, rutas, guardrails, snapshots, migraciones e integración Claims sin hallazgos bloqueantes.

## Contrato vigente

- DEV14 representa una reclamación por débito ACH no consentido.
- La Entidad Participante Receptora de la transacción presenta la reclamación a la Entidad Participante Originadora.
- DEV14 pertenece exclusivamente a Reclamaciones ACH Colombia.
- DEV14 no pertenece a `AchReturnCode`, no genera ReturnOut, no se serializa en Addenda 99 y no participa en Return-of-Return.
- El lifecycle de la reclamación no usa `AchTransferStateEnum`, `Certified`, SOL02 ni `AchTransactionStateEvent` como almacenamiento del caso.
- `ClaimTransaction` es el vínculo entre reclamaciones y transacciones.

## Permisos de Reclamaciones

La superficie usa únicamente:

- `Claims.Read`
- `Claims.CreateDev14`
- `Claims.Progress`
- `Claims.Resolve`
- `Claims.Expire`

`CanReadAch` y `CanManageAch` pertenecen a otras capacidades y no autorizan rutas, acciones, API ni navegación de Reclamaciones.

## Clasificación de deuda DEV14 revisada en 3E

| Artefacto | Clasificación | Resultado 3E |
|---|---|---|
| `ReturnReasonConfiguration.cs` | STALE_RETURN_MODEL | DEV14 retirado del catálogo Return actual. |
| `IncomingNachaFunctionalClassifierTests` | STALE_RETURN_MODEL | DEV14 en Addenda 99 ahora prueba falla cerrada hacia Reclamaciones. |
| `NachaSemanticAndStrategyTests` | STALE_RETURN_MODEL | Fixture Return cambiado a Rxx válido. |
| `AchReturnEligibilityServiceTests` | STALE_RETURN_MODEL | Fixture DEV14 retirado del flujo Return. |
| `AchReturnOfReturnEligibilityServiceTests` | STALE_RETURN_MODEL | DEV14 retirado de Return-of-Return. |
| `AchReturnIdempotencyTests` | STALE_RETURN_MODEL | Fixtures Return cambiados a Rxx válidos. |
| `docs/uat/cause-code-acceptance-checklist.md` | STALE_RETURN_MODEL | DEV14 separado explícitamente de ReturnOut/Addenda 99/ROR. |
| `docs/uat/outbound-return-state-traceability-acceptance-checklist.md` | STALE_RETURN_MODEL | Caso DEV14 actualizado al guardrail Claims-only. |
| `docs/dev/codex-test-environment.md` | HISTORICAL_CONTEXT | Evidencia fechada conservada y marcada expresamente como histórica. |
| Migraciones/designers anteriores que sembraron DEV14 como Return | HISTORICAL_CONTEXT | Conservados para trazabilidad histórica; snapshots actuales ya no contienen ese seed. |
| `ACHCOL_DEV14_CLAIMS_ONLY`, `ClaimCauseCode.DEV14` y pruebas negativas de Return | CURRENT_VALID | Conservados. |
| ACH Colombia V35 | CURRENT_VALID | Conservado como referencia normativa. |

Además se retiraron del modelo Return actual las aceptaciones residuales de DEV14 en `AchCauseCodePolicy`, `TransactionValidator`, `NachaSemanticValidator`, `NachaParserService` y fixtures relacionados. `RegulatoryCatalogSeeder` elimina cualquier fila antigua `ReturnReasons.DEV14` al sembrar catálogos actuales.

`docs/returns/MAPA_MAESTRO_BRECHAS_DEVOLUCIONES.md` se conserva como fotografía histórica del análisis del 2026-08-07 y quedó marcado explícitamente como tal; sus pasajes que mostraban `RET-GAP-019` abierto no representan el contrato vigente.

## Checkpoint técnico

- `ClaimCase`, `ClaimTransaction` y `ClaimCaseEvent` permanecen sin rediseño.
- Lifecycle vigente: `Open`, `Resolved`, `Expired`.
- Idempotencia de creación y eventos, `ExternalReference`, `DeadlineAt`, `ClosedAt`, `ResolutionNotes` e historial permanecen configurados.
- SQL Server y PostgreSQL conservan las migraciones 3B.1/3B.2; no existe migración 3E porque no hubo cambio de schema.
- La API conserva exactamente las siete rutas acordadas.
- La SPA conserva listado, detalle, creación DEV14, avance, resolución y vencimiento con permisos exclusivamente `Claims.*`.
- Se corrigió un defecto sintáctico objetivo heredado del incremental 3C en `AchClaimsController.GetById`; no se amplió el contrato HTTP.

No se declara `UAT_READY` ni `RELEASE_READY` sin build y suites ejecutables sobre el artefacto exacto.
