# Incoming NACHA-M — Matriz regulatoria/configurable (Prompt 2) — 2026-04-23

## Objetivo

Definir la base **P0 de configuración y catálogos** para operación inbound NACHA-M en ACHInterbank, sin introducir command center ni cambios de flujo operativo mayor.

## Versión de matriz

- `MatrixVersion`: `IN-NACHA-P2-2026-04-23-v1`
- `EffectiveFrom`: `2026-04-23`
- `Scope`: `Inbound NACHA-M / Clasificación / Prenotificaciones / Devoluciones / Integración`

## 1) Catálogos regulatorios auditados y consolidados

1. `AchReturnCodes`
   - Causales `Rxx` + `DEV14` con:
     - aplicabilidad por tipo de transacción,
     - requerimiento de addenda,
     - ventana máxima de días,
     - fuente regulatoria (`CENIT`, `ACH`, `OPERADOR`).

2. `AchReturnPolicies`
   - Política por tipo (`Debit`, `Credit`, `Prenotification`, `Return`) con:
     - códigos permitidos,
     - ventana máxima,
     - estado origen exigido,
     - requerimiento de addenda,
     - habilitación de return-of-return.

3. `AchPrenotificationPolicies`
   - Política por tipo de transacción:
     - si exige prenotificación,
     - si requiere addenda,
     - si bloquea monetaria en ausencia de prenote.

4. `AchFileRejectionCodes`
   - Taxonomía formal de errores para inbound:
     - códigos Dxx (parser/validación/transmisión),
     - códigos Ixxx (integración),
     - severidad,
     - etapa aplicable,
     - retry automático permitido o no.

5. `AchTransactionTypePolicies`
   - Prioridad y capacidad operativa por tipo.

6. `AchReturnOfReturnPolicies`
   - Reglas para devolución de devolución.

## 2) Taxonomía formal de errores (configurable)

### 2.1 Parser/validación (Dxx)

- `D01` duplicado de archivo.
- `D02` formato/estructura inválida.
- `D03` operador/canal incorrecto.
- `D04` secuencia/conteos inválidos.
- `D05` hash/cuadre inválido.
- `D06` campo obligatorio ausente.

Regla P0: en baseline Prompt 2 se consideran **no retry automáticos** (requieren corrección de origen o reproceso controlado).

### 2.2 Integración (Ixxx)

- `I500`, `I503`, `ITIMEOUT`, `ISOAP` => retryable.
- `IFUNC` => no retry automático (rechazo funcional).

Regla P0: separar explícitamente error técnico recuperable vs rechazo funcional para fases posteriores de command center/state machine.

## 3) Parámetros operativos inbound (base para prompts siguientes)

> Estos parámetros quedan formalizados en catálogo y documentación para posterior consumo por command center/state machine.

1. `Dispatch.MaxAttempts`: 5 (vigente en orquestador actual).
2. `Dispatch.BackoffPolicy`: incremental, tope 30 minutos.
3. `Dispatch.WaitingWindowPolicy`: basado en ventana de ciclo/cámara.
4. `Return.MaxCyclesForReturn`: 4 ciclos.
5. `Return.ReasonCodeValidation`: obligatorio por catálogo.
6. `Prenote.DeterminismRequired`: si no es determinístico => revisión manual.
7. `Integration.ErrorClassification`: técnica retryable vs funcional no retryable.

## 4) Gobernanza de seeders

Para minimizar drift entre ambientes:

- El seeder regulatorio se ejecuta en modo **upsert** sobre claves de negocio.
- Si existe registro baseline, se normaliza al estándar vigente.
- Si falta registro baseline, se inserta.

Esto permite homologar DEV/UAT/PROD sin depender de “tabla vacía”.

## 5) Compatibilidad y no-regresiones

- No se introducen cambios de flujo transaccional principal.
- No se modifica crypto ni pipeline OpenBao/SecretRef.
- No se implementa command center ni UI operativa en este prompt.
- Se mantiene compatibilidad con el pipeline inbound actual.

## 6) Evidencia de validación recomendada

- Pruebas de seeder para:
  - poblar baseline en DB vacía,
  - reparar drift en DB parcial,
  - garantizar presencia de códigos regulatorios críticos (`DEV14`, `ITIMEOUT`, `IFUNC`).


## 7) Revalidación ejecutada (Prompt 2B) — 2026-04-24

### Entorno

- .NET SDK instalado con `scripts/codex/setup-codex-env.sh`.
- `dotnet --info` validado (SDK `10.0.201`).
- `dotnet ef --version` validado (`10.0.7`).

### Build

- `dotnet restore ACHInterbank.sln` ✅
- `dotnet build ACHInterbank.sln -c Release` ✅ (sin errores; warnings de nullability preexistentes fuera de alcance Prompt 2).

### Tests ejecutados

1. Catálogos / políticas / seeders (Prompt 2)
   - `dotnet test ... --filter "FullyQualifiedName~AchRegulatoryCatalogServiceTests|FullyQualifiedName~RegulatoryCatalogSeederTests"`
   - Resultado: **9/9 passed**.

2. No regresión NACHA / Mapping / BatchNumber
   - `dotnet test ... --filter "FullyQualifiedName~Nacha|FullyQualifiedName~Mapping|FullyQualifiedName~BatchNumber"`
   - Resultado: **166/166 passed**.

### Verificaciones adicionales

- Consistencia matriz-documento vs códigos baseline (`DEV14`, `D01..D06`, `I500`, `I503`, `ITIMEOUT`, `ISOAP`, `IFUNC`) validada.
- Confirmado: cambios de Prompt 2 se limitaron a seeder, tests y documentación (sin cambios criptográficos).
- Confirmado: GitHub Actions disponible en modo manual (`workflow_dispatch`) para `postgres-integration-tests.yml`.
