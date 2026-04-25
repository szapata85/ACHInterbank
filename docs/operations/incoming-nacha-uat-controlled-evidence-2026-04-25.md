# Evidencia de ejecución — UAT Operativo Controlado Inbound NACHA-M

Fecha de ejecución: 2026-04-25 (UTC)

## 1) Resultado ejecutivo

- Estado UAT: **NO_GO temporal**.
- Motivo principal: bloqueo de entorno para instalar SDK .NET (error HTTP 503 sobre `dot.net`), lo que impidió ejecutar build/tests backend en esta corrida.
- Alcance sí entregado en este prompt:
  - matriz UAT ejecutable,
  - checklist Go/No-Go,
  - script de ejecución controlada,
  - criterios de evidencia y severidad.

## 2) Comandos ejecutados y resultado real

### 2.1 Inspección requerida

```bash
git status --short
git log --oneline -10
rg -n "IncomingNacha|CommandCenter|StateMachine|Observability|DispatchResilience|AllowedActions|RetryPending|WaitingWindow|FailedFinal|Blocked|Prenote|Return|DEV14|IFUNC|ITIMEOUT" src tests docs web/ach-interbank-ui/src -S
find docs -maxdepth 3 -type f | sort | rg "incoming-nacha|uat|deployment|observability|command-center|state-machine|resilience|sdk"
```

Resultado: ejecutados correctamente; se identificó cobertura documental/técnica existente para Command Center, State Machine, Resiliencia y Observabilidad.

### 2.2 Ejecución de paquete UAT

```bash
bash scripts/uat/run-incoming-nacha-uat-controlled.sh
```

Resultado real:

- Falla en paso de setup:
  - `curl: (22) The requested URL returned error: 503`
- Impacto: no fue posible completar build/tests backend en esta corrida.
- Se reintentó con estrategia de 3 intentos en el script y persistió el 503.


### 2.3 Frontend (ejecución donde aplica)

```bash
cd web/ach-interbank-ui
npm ci
npm run build
```

Resultado real:

- `npm ci`: OK.
- `npm run build`: falla por dependencia externa de fonts con HTTP 503:
  - `Inlining of fonts failed ... fonts.googleapis.com ... returned status code: 503`.

### 2.4 Workflow manual-only

```bash
rg -n "workflow_dispatch|if: github.event_name == 'workflow_dispatch'|push:|pull_request|schedule|workflow_run" .github/workflows/postgres-integration-tests.yml -S
```

Resultado: se mantiene `workflow_dispatch` y guard manual-only.

## 3) Matriz de escenarios (estado de esta corrida)

| ScenarioId | Estado | Severidad si bloquea salida | Evidencia |
|---|---|---|---|
| UAT-01..UAT-16 | Bloqueado por entorno (parcial) | Sev-1 (bloqueo de salida UAT) | Error 503 en instalación SDK .NET |

## 4) Riesgo y clasificación

- Hallazgo: indisponibilidad transitoria del origen de instalación de SDK.
- Clasificación: **Sev-1 de ejecución** (infra/tooling del runner), no defecto funcional de negocio.

## 5) Recomendación operativa para destrabar

1. Reintentar ejecución cuando `https://dot.net/v1/dotnet-install.sh` responda correctamente.
2. Como mitigación de runner, mantener cache local del SDK `10.0.203` o imagen base pre-provisionada.
3. Re-ejecutar script:

```bash
bash scripts/uat/run-incoming-nacha-uat-controlled.sh
```

4. Reemitir acta UAT con estatus final Go/No-Go tras corrida completa.

## 6) Criterio de aprobación pendiente

No se marca aprobación UAT en esta corrida por falta de evidencia completa de build/tests backend bajo bloqueo de entorno.
