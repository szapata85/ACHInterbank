# Revalidación Prompt 2B — PaymentRail Fase 1 (2026-04-25)

## Objetivo

Cerrar brecha de validación operativa del Prompt 2 ejecutando setup real de .NET, restore/build y suites de pruebas solicitadas, sin introducir nuevas funcionalidades.

## Alcance de control

- Sin cambios funcionales en ACH/CENIT.
- Sin cambios en parser, command center, state machine, resiliencia u observabilidad.
- Sin cambios criptográficos (`CryptoServiceScoped`, `OpenEnvelopeAsync`, `RsaKeyProvider`, identifier/IV, XML/AES/RSA/padding).
- Sin cambios de DB/migraciones.

## Evidencia de entorno (.NET 10.0.203)

Comandos ejecutados:

```bash
bash scripts/codex/setup-codex-env.sh
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$HOME/.dotnet:$HOME/.dotnet/tools:$PATH

dotnet --info
dotnet --list-sdks
dotnet ef --version
cat global.json
```

Resultados relevantes:

- SDK activo: `10.0.203`
- Runtime host: `10.0.7`
- `dotnet ef`: `10.0.7`
- `global.json` fija `10.0.203` con `rollForward=disable`

## Restore/Build

Comando:

```bash
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln -c Release
```

Resultado:

- **Build succeeded**.
- 8 warnings preexistentes de nulabilidad en archivos legacy (`CryptoServiceScoped`, `NachaParserService`, `IncomingNachaIngestionAppService`, `ExcelBulkFileParser`).
- 0 errores.

## Pruebas ejecutadas

### 1) PaymentRail

```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~PaymentRail"
```

- Passed: 3
- Failed: 0

### 2) Incoming / CommandCenter / Observability / Resilience

```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~IncomingNacha"
```

- Passed: 63
- Failed: 0

```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~CommandCenter|FullyQualifiedName~Observability|FullyQualifiedName~Resilience"
```

- Passed: 9
- Failed: 0

### 3) NACHA / Mapping / BatchNumber

```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~Nacha|FullyQualifiedName~Mapping|FullyQualifiedName~BatchNumber"
```

- Passed: 193
- Failed: 0

### 4) DI

```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~DependencyInjection"
```

- Passed: 2
- Failed: 0

## Resultado de revalidación

Revalidación **exitosa** para Prompt 2B:

- Setup .NET corregido y operativo con SDK requerido.
- Restore/build backend exitoso.
- Suites objetivo ejecutadas con éxito.
- Base contractual multi-riel de Fase 1 validada en ejecución real.
- Sin alterar componentes prohibidos ni lógica operativa ACH/CENIT.

## Nota de gobernanza

El bloqueo previo (`dotnet: command not found`) queda cerrado con evidencia de ejecución real en esta fecha (2026-04-25).
