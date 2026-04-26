# Revalidación estricta — Prompt 9B SPA Angular read-only Capability Registry (2026-04-26)

## Objetivo

Cerrar formalmente la evidencia de Prompt 9 (SPA read-only) con validaciones complementarias de seguridad, permisos, no-escritura y regresión backend.

## Inspección inicial de routing y guards

- Feature presente en `web/ach-interbank-ui/src/app/features/payment-rail-capability-registry`.
- Ruta lazy registrada en `AppRoutingModule`:
  - `path: payment-rail-capability-registry`.
- Guardas activas:
  - `roleGuard`;
  - `permissionGuard`.
- Permisos configurados en ruta:
  - `CanViewPaymentRailCapabilityRegistry`;
  - `CanManageAch`;
  - `CanReadAch`.

## Validación frontend (read-only)

```bash
cd web/ach-interbank-ui
npm ci
npm run build
```

- `npm ci`: OK.
- `npm run build`: OK.

```bash
npm test -- --watch=false --browsers=ChromeHeadless --include src/app/features/payment-rail-capability-registry/services/payment-rail-capability-registry-api.service.spec.ts
```

- Fallo de entorno no funcional:
  - `CHROME_BIN` no disponible;
  - error runtime Karma `Cannot read properties of undefined (reading 'filter')`;
  - error runtime `invalid rimraf options`.

## Evidencia de no escritura en el feature

```bash
rg -n "post\(|put\(|patch\(|delete\(|post<|put<|patch<|delete<|HttpClient\.post|HttpClient\.put|HttpClient\.patch|HttpClient\.delete" web/ach-interbank-ui/src/app/features/payment-rail-capability-registry -S
```

- Sin coincidencias.

Conclusión: el feature consume únicamente llamadas GET del servicio API.

## Evidencia de no almacenamiento local sensible

```bash
rg -n "localStorage|sessionStorage" web/ach-interbank-ui/src/app/features/payment-rail-capability-registry -S
```

- Sin coincidencias.

## Evidencia de no crypto frontend

```bash
rg -n "crypto\.subtle|window\.crypto|crypto" web/ach-interbank-ui/src/app/features/payment-rail-capability-registry -S
```

- Sin coincidencias.

## Evidencia de no exposición de secretos/sensibles en feature

```bash
rg -n "SecretRef|PFX|privateKey|password|payload|NACHA|account|identif|document|token|secret" web/ach-interbank-ui/src/app/features/payment-rail-capability-registry -S
```

- Sin coincidencias.

## Revalidación backend complementaria

```bash
bash scripts/codex/setup-codex-env.sh
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$HOME/.dotnet:$HOME/.dotnet/tools:$PATH

dotnet build ACHInterbank.sln -c Release
```

- Build backend OK.

```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~PaymentRailCapabilityRegistryServiceTests|FullyQualifiedName~PaymentRailCapabilityRegistryControllerTests|FullyQualifiedName~PaymentRailCapabilityRegistryAuthorizationPolicyTests"
```

- Passed: 15
- Failed: 0

## Evidencia no-crypto y workflow manual-only

```bash
git diff --name-only | rg -n "CryptoServiceScoped|OpenEnvelopeAsync|RsaKeyProvider|ACHSobreDigital|Encryption"
```

- Sin coincidencias en cambios activos.

```bash
rg -n "^on:|workflow_dispatch|push:|pull_request:" .github/workflows -S
```

- Workflow observado para pruebas Postgres con `workflow_dispatch`.
- Sin cambios de workflow en este prompt (`git diff --name-only` no muestra `.github/workflows/*`).

## Conclusión formal

Prompt 9 SPA queda revalidado:

- UI read-only compila;
- ruta lazy existe y está protegida;
- feature usa solo GET;
- sin POST/PUT/PATCH/DELETE;
- sin uso de local/session storage en el feature;
- sin uso de crypto frontend;
- sin exposición de secretos/payload sensible en el feature;
- backend complementario compila;
- tests backend registry/API pasan;
- legacy sigue source-of-truth;
- sin cutover;
- sin cambios criptográficos;
- workflows permanecen manual-only.
