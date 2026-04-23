# Digital Envelope SPA ↔ Backend Integration Tests — Prompt 14 (2026-04-22 UTC)

## Objetivo
Cerrar revalidación operativa Prompt 14 sin cambios criptográficos: permisos finos, descarga segura, no regresión, build frontend y validación de secretos/WebCrypto.

## Comandos ejecutados
```bash
bash scripts/codex/setup-codex-env.sh
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$HOME/.dotnet:$HOME/.dotnet/tools:$PATH

dotnet --info
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln -c Release --no-restore

dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj \
  -c Release \
  --filter "FullyQualifiedName~NachaSecurityOperation|FullyQualifiedName~ManualEncrypt|FullyQualifiedName~ManualDecrypt|FullyQualifiedName~GenerateEncrypted|FullyQualifiedName~Download|FullyQualifiedName~Permission|FullyQualifiedName~Authorization" \
  -v minimal

dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj \
  -c Release \
  --filter "FullyQualifiedName~Signature|FullyQualifiedName~OpenEnvelope|FullyQualifiedName~DigitalEnvelope|FullyQualifiedName~CertificateResolver|FullyQualifiedName~SecretResolver" \
  -v minimal

dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj \
  -c Release \
  --filter "FullyQualifiedName~Nacha|FullyQualifiedName~Mapping|FullyQualifiedName~BatchNumber" \
  -v minimal

cd web/ach-interbank-ui
npm ci --include=dev
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
```

## Resultado backend
- Build (`dotnet build ... --no-restore`): **OK**.
- Filtro permisos/descarga/operaciones:
  - **Total 12 / Passed 12 / Failed 0 / Skipped 0**.
- Filtro DigitalEnvelope/Signature/OpenEnvelope/SecretResolver:
  - **Total 36 / Passed 36 / Failed 0 / Skipped 0**.
- Filtro NACHA/Mapping/BatchNumber:
  - **Total 166 / Passed 166 / Failed 0 / Skipped 0**.

## Resultado frontend
- `npm ci --include=dev`: **OK**.
- `npm run build`: **OK**.
- `npm test -- --watch=false --browsers=ChromeHeadless`: **FALLA de entorno/toolchain**:
  - `No binary for ChromeHeadless browser on your platform. Please, set "CHROME_BIN" env variable.`
  - `TypeError: Cannot read properties of undefined (reading 'filter')` (karma file-list).
  - `Error: invalid rimraf options`.

## Seguridad y alcance
- Permisos finos aplicados en backend/controller y rutas SPA.
- Descarga segura confirmada (`authorize-download` previo, validación de expiración/autorización y bloqueo de plano cuando `SIGNATURE_VALIDATION_FAILED`).
- `sanitizeDownloadFileName` activo en descargas SPA de NACHA security.
- No WebCrypto aplicado para criptografía de negocio en Angular (`crypto.subtle` no usado para cifrar/firmar/descifrar).
- Sin cambios en `CryptoServiceScoped`, `OpenEnvelopeAsync`, `RsaKeyProvider`, `identifier/IV`, XML o padding criptográfico.
- Workflow PostgreSQL permanece manual-only (`workflow_dispatch` + `if: github.event_name == 'workflow_dispatch'`).
