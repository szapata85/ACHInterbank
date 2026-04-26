# UAT controlado — Prompt 10 Read-Only API + SPA Capability Registry (2026-04-26)

## Objetivo

Ejecutar validación UAT controlada de punta a punta para consulta read-only del Capability Registry desde API y SPA, con foco en seguridad, permisos, ausencia de escritura y evidencia Go/No-Go.

## Evidencia de inspección inicial

```bash
git status --short
git log --oneline -10
find web/ach-interbank-ui/src/app/features/payment-rail-capability-registry -maxdepth 6 -type f | sort
```

Resultado:

- Feature SPA presente y acotado al scope read-only.
- Último commit de la línea phases 1..9 visible para trazabilidad.

## UAT API + SPA (read-only) — checklist ejecutado

### 1) Ruta SPA existe y está protegida

```bash
rg -n "payment-rail-capability-registry|CanViewPaymentRailCapabilityRegistry|CanManageAch|CanReadAch|permissionGuard|roleGuard|loadChildren" web/ach-interbank-ui/src -S
```

Resultado: **OK**

- Ruta lazy `payment-rail-capability-registry` registrada.
- Guards `roleGuard` + `permissionGuard` aplicados.
- Permisos de acceso definidos (`CanViewPaymentRailCapabilityRegistry`, fallback `CanManageAch`/`CanReadAch`).

### 2) Servicio Angular usa solo GET

```bash
rg -n "get<|post<|put<|patch<|delete<|HttpClient" web/ach-interbank-ui/src/app/features/payment-rail-capability-registry -S
```

Resultado: **OK**

- Únicamente llamadas `get<...>` en el servicio del feature.

### 3) No existen llamadas de escritura en el feature

```bash
rg -n "post\(|put\(|patch\(|delete\(|post<|put<|patch<|delete<|HttpClient\.post|HttpClient\.put|HttpClient\.patch|HttpClient\.delete" web/ach-interbank-ui/src/app/features/payment-rail-capability-registry -S
```

Resultado: **OK** (sin coincidencias).

### 4) No uso de almacenamiento local/sesión

```bash
rg -n "localStorage|sessionStorage" web/ach-interbank-ui/src/app/features/payment-rail-capability-registry -S
```

Resultado: **OK** (sin coincidencias).

### 5) No uso de crypto frontend

```bash
rg -n "crypto\.subtle|window\.crypto|crypto" web/ach-interbank-ui/src/app/features/payment-rail-capability-registry -S
```

Resultado: **OK** (sin coincidencias).

### 6) No exposición de datos sensibles en el feature

```bash
rg -n "SecretRef|PFX|privateKey|password|payload|NACHA|account|identif|document|token|secret" web/ach-interbank-ui/src/app/features/payment-rail-capability-registry -S
```

Resultado: **OK** (sin coincidencias).

## Validación técnica frontend

```bash
cd web/ach-interbank-ui
npm ci
npm run build
```

Resultado: **OK**

```bash
npm test -- --watch=false --browsers=ChromeHeadless --include src/app/features/payment-rail-capability-registry/services/payment-rail-capability-registry-api.service.spec.ts
```

Resultado: **WARN (entorno)**

- `CHROME_BIN` no disponible.
- Error runtime Karma: `Cannot read properties of undefined (reading 'filter')`.
- Error runtime: `invalid rimraf options`.

No hay evidencia de falla funcional del feature; es bloqueo de ejecución browser/runtime en el entorno actual.

## Validación técnica backend complementaria

```bash
bash scripts/codex/setup-codex-env.sh
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$HOME/.dotnet:$HOME/.dotnet/tools:$PATH

dotnet build ACHInterbank.sln -c Release
```

Resultado: **OK** (build exitoso; warnings legacy preexistentes).

```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~PaymentRailCapabilityRegistryServiceTests|FullyQualifiedName~PaymentRailCapabilityRegistryControllerTests|FullyQualifiedName~PaymentRailCapabilityRegistryAuthorizationPolicyTests"
```

Resultado: **OK**

- Passed: 15
- Failed: 0

```bash
dotnet ef migrations list \
  --project src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj \
  --startup-project src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj \
  --context AchDbContext
```

Resultado: **OK (con warning de entorno DB)**

- Migration `20260426025056_AddPaymentRailCapabilityRegistryPhase7` presente.
- Warning esperado por no disponibilidad de PostgreSQL local para estado applied/pending.

## No crypto / No cutover / Manual-only

```bash
git diff --name-only | rg -n "CryptoServiceScoped|OpenEnvelopeAsync|RsaKeyProvider|ACHSobreDigital|Encryption"
```

- Sin cambios activos en criptografía.

```bash
rg -n "^on:|workflow_dispatch|push:|pull_request:" .github/workflows -S
```

- Workflows observados en modo manual (`workflow_dispatch`) para la suite Postgres.
- Sin cambios de workflow en este prompt.

## Matriz Go/No-Go UAT

| Criterio | Estado | Evidencia |
|---|---|---|
| API read-only disponible en código | GO | Controller/API y tests backend 15/15 |
| SPA read-only compila | GO | `npm run build` OK |
| Ruta protegida por guards/permisos | GO | `rg` routing/guards/permisos |
| Solo GET en feature | GO | escaneo sin POST/PUT/PATCH/DELETE |
| Sin almacenamiento local/sesión en feature | GO | escaneo sin `localStorage/sessionStorage` |
| Sin crypto frontend en feature | GO | escaneo sin `crypto` |
| Sin datos sensibles expuestos en feature | GO | escaneo de keywords sensibles sin hallazgos |
| Backend complementario estable | GO | build OK + tests registry/API 15/15 |
| Migración capability registry consistente | GO* | `ef migrations list` incluye `20260426025056...` (con warning por DB local no disponible) |
| No cutover / no impacto ACH-CENIT | GO | no cambios funcionales legacy reportados en este prompt |

## Decisión UAT

**GO CONDICIONAL**

- Go para despliegue/control UAT de consulta read-only API + SPA.
- Condición técnica pendiente de entorno CI local: habilitar runtime browser (`CHROME_BIN`) para ejecutar `npm test` automatizado en este entorno.
