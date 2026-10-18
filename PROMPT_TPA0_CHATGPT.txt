# Prompt maestro optimizado — TPA-GOV-001 / iniciar TPA0

Trabaja sobre este repositorio ACHInterbank y ejecuta el programa `TPA-GOV-001`.

## Objetivo único de esta sesión

Ejecuta **solamente `next_allowed_phase`** de `docs/ai/THIRD_PARTY_ACCOUNT_GOVERNANCE_STATE.md`. En este paquete es `TPA0`. **No ejecutes TPA1 aunque TPA0 termine bien.**

## Lectura obligatoria y orden

1. `AGENTS.md`.
2. Sólo el contexto R10 + TPA de `MEMORY.md`.
3. `docs/ai/THIRD_PARTY_ACCOUNT_GOVERNANCE_STATE.md`.
4. `docs/architecture/THIRD_PARTY_ACCOUNT_GOVERNANCE_IMPLEMENTATION_PLAN.md`.
5. `docs/architecture/THIRD_PARTY_ACCOUNT_GOVERNANCE_ARCHITECTURE.md`.
6. `docs/architecture/THIRD_PARTY_ACCOUNT_SILENT_ONBOARDING_CONTRACT.md`.
7. `docs/normativa/THIRD_PARTY_ACCOUNT_NORMATIVE_TRACEABILITY.md`.
8. `docs/testing/THIRD_PARTY_ACCOUNT_TEST_STRATEGY.md`.

Luego ejecuta:

```text
git status --short
git rev-parse HEAD
python scripts/validate-solution-projects.py
```

Si no hay `.git`, reporta `SHA=UNAVAILABLE_SOURCE_ARCHIVE_NO_GIT_METADATA`. Nunca inventes un commit.

## Invariantes no negociables

1. El onboarding silencioso single/bulk se conserva o mejora; no se reemplaza por preregistro manual.
2. **Onboarding = master data genérico.** `ThirdParty`/`ThirdPartyAccount` no dependen de ACH Colombia, CENIT, crédito/débito, PPD/CCD/CTX, ciclos, deadlines o causales.
3. Si detectas acoplamiento de cámara en el onboarding actual, regístralo como `GAP-TPA-*`; TPA0 no lo corrige.
4. Single, bulk y futuro alta manual deben converger al mismo servicio de onboarding.
5. Cámara/naturaleza sólo entran en elegibilidad y validación, después del onboarding.
6. Ningún usuario aprueba/rechaza manualmente el resultado interbancario.
7. ACH Colombia y CENIT se resuelven por políticas efectivas separadas.
8. Débito obligatorio falla cerrado; crédito opcional no se vuelve obligatorio por conveniencia de software.
9. No hardcodees layout, causal, ciclo, deadline o madurez si existe/puede extenderse autoridad configurada.
10. No cambies goldens NACHA-M sin cambio normativo probado.
11. Onboarding no hace I/O externo.
12. No retry ciego sobre operaciones monetarias/no idempotentes.
13. Mantén EF Code First, PostgreSQL y SQL Server.
14. No expongas PII/cuentas completas en logs, fixtures o docs.
15. No hagas cleanup fuera de fase.

## TPA0 — exactamente qué hacer

TPA0 es **characterization + baseline**, no implementación.

1. Inventaría readers/writers/callers de `CustomerThirdParty`, `PrenotificationHandler`, la parte de prenotificaciones de `AchStateTransitionService`, `PrenotificationQueryService`, `AchTacitAcceptanceJobHandler`, `TransactionPrerequisitePolicyService`, `ClearingHouseTransactionRule`, `CustomerThirdPartiesController` y SPA relacionada.
2. Characteriza `AchTransactionService` y `AchBulkTransactionService`: existente/inexistente, legacy Active/Rejected, source account sin Customer, deduplicación/comportamiento partial actual.
3. Determina explícitamente qué parte del onboarding actual es master data y qué parte está acoplada a prenotificación/cámara. No refactorices todavía.
4. Characteriza resultado automático e idempotencia de prenotificaciones y confirma ausencia de write endpoint manual de estado.
5. Characteriza por separado ACH Colombia credit/debit y CENIT credit/debit.
6. Registra sin corregir:
   - `PrenotificationQueryService` global `+3 business days`;
   - `SlaDeadlineAtUtc`/fallback de tacit acceptance;
   - legacy sin `AccountType`, `ThirdParty` separado, `ThirdPartyAccount` separado e historial multi-intento; su status global mezcla master/validation;
   - selector SPA basado en `status=Active`;
   - metadata V32 vs V35;
   - cualquier dependencia de clearing house dentro de onboarding actual.
7. No crees migraciones, endpoints, estados ni UI.
8. Crea `docs/architecture/third-party-account-tpa0-baseline.md` con evidencia, callers, gaps, tests y límites concretos para TPA1.
9. Ejecuta focal tests primero; luego build/broad gates si el host lo permite. Si falta herramienta, usa `NOT_RUN_ENVIRONMENT`, nunca PASS inventado.
10. Actualiza state y `MEMORY.md` sólo con evidencia durable. No reabras R10.

## Criterio anti-divagación

No propongas frameworks/tecnologías nuevas. No rediseñes otros módulos. No implementes TPA1. Un defecto funcional nuevo se registra como gap y, si es seguro, se protege con characterization test.

## Respuesta final exacta

```text
PHASE_STATE:
SHA:
SUMMARY:
FILES_CHANGED:
CHARACTERIZATION_ADDED:
BASELINE_FINDINGS:
TESTS_EXECUTED:
DB_MIGRATIONS: NONE
BEHAVIORAL_DIFF: NONE
NORMATIVE_DIFF: NONE
RISKS_OR_BLOCKERS:
MEMORY_UPDATE:
NEXT_ALLOWED_PHASE: TPA1 | TPA0
```

Usa `TPA1` sólo si todos los gates aplicables de TPA0 pasan. No ejecutes TPA1 en este chat.
