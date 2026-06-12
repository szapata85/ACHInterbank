# Evidencia de ejecución — UAT Operativo Controlado Inbound NACHA-M (Reejecución robusta)

Fecha de ejecución: 2026-04-25 (UTC)

## 1) Resultado ejecutivo

- Estado UAT: **GO condicionado**.
- Alcance validado en esta corrida:
  - setup .NET + verificación SDK 10.0.203,
  - build backend Release,
  - suites críticas backend,
  - build Angular (sin dependencia externa de Google Fonts),
  - verificación workflow manual-only.
- Restricción pendiente (no bloqueante para este cierre técnico):
  - `npm test` sigue bloqueado por entorno (`CHROME_BIN` ausente + runtime Karma/rimraf).

## 2) Comandos ejecutados y resultado real

### 2.1 Inspección inicial

```bash
git status --short
git log --oneline -10
test -f global.json && cat global.json || true
rg -n "10\.0\.203|rollForward|setup-codex-env|dotnet-install|DOTNET_ROOT|run-incoming-nacha-uat-controlled|fonts.googleapis|inlineFonts|optimization|styles|font" \
  global.json scripts web/ach-interbank-ui/angular.json web/ach-interbank-ui/src docs -S
```

Resultado: ejecutado correctamente, con pin SDK `10.0.203` + `rollForward: disable` confirmado.

### 2.2 Reejecución UAT robusta

```bash
bash scripts/uat/run-incoming-nacha-uat-controlled.sh
```

Resultado consolidado:

- Setup/SDK: OK (`dotnet 10.0.203`, `dotnet-ef 10.0.7`).
- Build backend: OK.
- Tests backend críticos:
  - Incoming/CommandCenter/StateMachine/Resilience/Observability: **63/63**.
  - NACHA/Mapping/BatchNumber: **193/193**.
  - DigitalEnvelope/Signature/OpenEnvelope: **32/32**.
  - Certificate/SecretRef/custodia de secretos: **35/35**.
- Frontend:
  - `npm ci`: OK.
  - `npm run build`: OK.
  - `npm test -- --watch=false --browsers=ChromeHeadless`: bloqueado por entorno (`CHROME_BIN` + Karma/rimraf).
- Workflow:
  - manual-only confirmado (`workflow_dispatch` + guard por evento).

## 3) Cambios de robustez aplicados

1. `scripts/uat/run-incoming-nacha-uat-controlled.sh`
   - usa SDK cacheado/local si ya existe `10.0.203`;
   - solo descarga SDK si no está disponible o no coincide;
   - mantiene reintentos para setup;
   - mantiene evidencia explícita de fallas de pruebas browser.

2. `web/ach-interbank-ui/angular.json`
   - ajuste de `optimization` en `production` para deshabilitar `fonts` inlining y evitar dependencia de `fonts.googleapis.com` en build UAT.

## 4) Matriz de escenarios (estado de esta corrida)

| ScenarioId | Estado | Evidencia |
|---|---|---|
| UAT-01..UAT-15 | Aprobado en validación técnica | build/tests backend + build frontend + observabilidad/command-center ya cubiertos |
| UAT-16 (pruebas browser automatizadas) | Bloqueado por entorno | `CHROME_BIN` ausente + error runtime Karma/rimraf |

## 5) Riesgo residual y acciones

- Riesgo residual: ausencia de runner con ChromeHeadless para `npm test`.
- Acción recomendada:
  1. provisionar `CHROME_BIN` en runner UAT/CI;
  2. estabilizar runtime Karma/rimraf del entorno;
  3. reejecutar únicamente bloque de pruebas browser para cerrar pendiente.
