# Revalidación formal Prompt 7B — Observabilidad Inbound NACHA-M

Fecha: 2026-04-24

## Alcance de revalidación

Se revalidó **sin agregar features nuevas** la entrega de observabilidad Prompt 7, enfocando:

1. compilación backend;
2. compilación de endpoint/servicio/DTOs observabilidad;
3. compatibilidad EF/LINQ de agregados;
4. tests de observabilidad e IncomingNacha;
5. no regresión NACHA/Mapping/BatchNumber;
6. compilación Angular;
7. estado real de `npm test`;
8. revisión de seguridad frontend (sin storage sensible / sin cripto frontend);
9. verificación de workflows GitHub en modo manual.

## Setup ejecutado

```bash
bash scripts/codex/setup-codex-env.sh
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$HOME/.dotnet:$HOME/.dotnet/tools:$PATH
dotnet --info
dotnet ef --version
git status --short
git log --oneline -8
```

Resultado:

- SDK instalado y activo: .NET **10.0.201**.
- `dotnet-ef` instalado: **10.0.7**.

## Build backend

```bash
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln -c Release
```

Resultado:

- **OK** (0 errores).
- Se mantienen warnings preexistentes fuera del alcance de Prompt 7.

## Tests backend ejecutados (reales)

```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~IncomingNachaCommandCenterServiceTests"
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~IncomingNacha"
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~Nacha|FullyQualifiedName~Mapping|FullyQualifiedName~BatchNumber"
```

Resultados:

- `IncomingNachaCommandCenterServiceTests`: **9/9 OK**.
  - Incluye prueba agregada de observabilidad (`GetObservabilitySummaryAsync_ShouldReturnAggregatedOperationalKpis`).
- `IncomingNacha*`: **63/63 OK**.
- `Nacha|Mapping|BatchNumber`: **193/193 OK**.

## Validación frontend

```bash
cd web/ach-interbank-ui
npm ci
npm run build
npm test -- --watch=false --browsers=ChromeHeadless
```

Resultados:

- `npm ci`: **OK**.
- `npm run build`: **OK**.
- `npm test`: **FAIL por runner**, causas observadas:
  - `No binary for ChromeHeadless browser on your platform. Please, set "CHROME_BIN" env variable.`
  - `TypeError: Cannot read properties of undefined (reading 'filter')` en `karma/lib/file-list.js`.
  - `Error: invalid rimraf options` en cleanup de launcher.

## Seguridad y restricciones

Validación ejecutada:

```bash
rg -n "localStorage|sessionStorage|crypto\.subtle|window\.crypto|privateKey|pfx|SecretRef|secretRef" web/ach-interbank-ui/src/app/features/incoming-nacha-command-center -S
```

Resultado:

- Sin hallazgos en el scope del feature de observabilidad.
- No se introdujo criptografía frontend ni almacenamiento sensible para este feature.

## GitHub Actions (manual-only)

Validación ejecutada:

```bash
find .github/workflows -maxdepth 2 -type f | sort
rg -n "workflow_dispatch|pull_request|push|schedule" .github/workflows -S
```

Resultado:

- Workflow presente: `.github/workflows/postgres-integration-tests.yml`.
- Trigger detectado: `workflow_dispatch`.
- No se detectan triggers `push`, `pull_request` ni `schedule`.

## Conclusión

Prompt 7 queda revalidado formalmente:

- backend compila;
- observabilidad compila y pasa pruebas del Command Center;
- agregados EF/LINQ funcionales (incluida prueba específica);
- no regresión IncomingNacha y NACHA/Mapping/BatchNumber;
- Angular compila;
- falla de `npm test` documentada con causa real de entorno.
