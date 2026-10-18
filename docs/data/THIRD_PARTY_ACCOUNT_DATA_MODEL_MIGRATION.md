# Modelo de datos y migración — TPA-GOV-001

> **Fase materializada:** TPA1 crea únicamente `ThirdParties` y `ThirdPartyAccounts`. `ThirdPartyAccountValidations` y `PrenotificationValidationAttempts` siguen siendo parte del modelo objetivo, pero su esquema se difiere al lifecycle de validación contextual (TPA3) para mantener el master data completamente agnóstico de cámara.

## 1. Objetivo

Introducir un maestro genérico de terceros/cuentas y un subsistema separado de validaciones operativas, sin romper `CustomerThirdParty` durante la transición.

## 2. Tablas canónicas propuestas

### `ThirdParties`

- `Id`
- `CustomerId` FK
- `PersonType`
- `DocumentType`
- `DocumentNumberNormalized`
- `DisplayName`
- `Status`
- auditoría estándar

Unique lógico recomendado:

`CustomerId + DocumentType + DocumentNumberNormalized`

### `ThirdPartyAccounts`

- `Id`
- `ThirdPartyId` FK
- `FinancialInstitutionId` FK
- `AccountType`
- `AccountNumberNormalized`
- `AccountNumberProtected/Encrypted` según patrón existente
- `Status`
- `OriginSource` opcional (`Manual`, `SingleTransaction`, `BulkTransaction`, `Backfill`)
- auditoría estándar

Unique lógico recomendado:

`ThirdPartyId + FinancialInstitutionId + AccountType + AccountNumberNormalized`

**Prohibición:** esta tabla no contiene `ClearingHouseId`, `TransactionNature`, service class, prenotification status, Rxx, ciclo ni deadline.

### `ThirdPartyAccountValidations`

Contexto operacional creado sólo cuando hay validación solicitada/requerida.

- `Id`
- `ThirdPartyAccountId` FK
- `ClearingHouseId` FK
- `TransactionNature`
- `ServiceType` nullable
- `ValidationType`
- `Status`
- `CurrentAttemptId` nullable
- `PolicyRuleId`/referencia a autoridad efectiva cuando exista
- `EffectiveFromUtc` nullable
- `ExpiresAtUtc` nullable
- `LastMonetaryUseAtUtc` nullable
- auditoría estándar

Unique recomendado, sujeto a TPA0 y convenciones reales:

`ThirdPartyAccountId + ClearingHouseId + TransactionNature + ServiceType + ValidationType`

No usar esta tabla como parte de la identidad del maestro.

### `PrenotificationValidationAttempts`

- `Id`
- `ThirdPartyAccountValidationId` FK
- `AttemptNumber`
- `AchTransactionId` nullable FK/relación equivalente
- `Status`
- `SentAtUtc`
- `ResponseDeadlineAtUtc`
- `ResolvedAtUtc`
- `Resolution`
- `ReturnCode` nullable
- `ReturnDetail` nullable/sanitizado
- `EvidenceReference` nullable
- correlación técnica necesaria
- auditoría estándar

Unique recomendado:

- `AchTransactionId` cuando no sea null, si el modelo real permite 1:1;
- `(ThirdPartyAccountValidationId, AttemptNumber)`.

## 3. Proyección opcional

`ThirdPartyAccountOperationalState` puede materializarse en TPA4+ para listas/selectores. Debe ser reconstruible y nunca ser autoridad de identidad ni de reglas.

## 4. Normalización y protección

TPA0 debe confirmar funciones/conversores existentes antes de introducir nuevos. Reglas objetivo:

- documento y cuenta normalizados para matching;
- valor completo protegido según patrón de seguridad del proyecto;
- logs y DTOs de lista enmascarados;
- no almacenar hash como sustituto único si se necesita recuperar el número para NACHA; separar lookup técnico y valor protegido si hace falta.

## 5. Branch by abstraction

### Etapa A — esquema aditivo

Crear tablas/índices/migraciones para PostgreSQL y SQL Server sin cambiar reads legacy.

### Etapa B — dual-write de maestro

Single/bulk/manual escriben `ThirdParty`/`ThirdPartyAccount` mediante `IThirdPartyOnboardingService`; compatibility adapter conserva `CustomerThirdParty` cuando el flujo legacy lo requiere.

### Etapa C — validación contextual

Prenotificaciones nuevas crean/vinculan `ThirdPartyAccountValidation` y `PrenotificationValidationAttempt`. El onboarding no los crea por sí solo.

### Etapa D — backfill

Backfill en dos pasos:

1. **Master data**: migrar tercero/cuenta cuando los datos permitan resolver identidad y `AccountType` sin inventar.
2. **Validation history**: reconstruir contexto sólo si la transacción asociada aporta cámara/naturaleza/servicio de forma no ambigua.

Si no existe evidencia suficiente, conservar legacy y registrar divergencia; no inferir una cámara por defecto.

### Etapa E — shadow compare y cutover

Comparar legacy/canónico y pasar lectura/selector cuando las divergencias estén resueltas/allowlisted.

## 6. Reglas de backfill

- idempotente y reanudable;
- no reescribir intentos existentes;
- no crear `Validated` sin evidencia transaccional;
- no crear validación contextual sólo porque exista una cuenta legacy `Active` si no se puede demostrar el contexto;
- `AccountType` faltante es gap explícito, no valor inventado;
- el mismo maestro se reutiliza para todas las cámaras.

## 7. Concurrencia

Los unique indexes del maestro son la última defensa. El servicio trata conflicto de unique como concurrencia esperable: re-read y continuar con la entidad existente. No envuelve la operación monetaria completa en retry automático.

## 8. Compatibilidad de providers

Cada migración nueva debe validarse en PostgreSQL y SQL Server, incluyendo:

- longitudes/índices;
- filtered/partial unique semantics si se usan;
- FK cascade/restrict conforme a patrones del proyecto;
- tipos de fecha UTC;
- migration from-clean y upgrade desde baseline.
