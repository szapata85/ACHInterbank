# PostgreSQL Integration Harness (ExternalFileNamePolicy / NACHA)

## 1) Propósito
Este harness permite validar localmente/CI en PostgreSQL real:
- migraciones EF,
- tablas/índices/constraints clave,
- integración `ExternalFileNamePolicy` (secuencia, duplicate guard, registry/log, validaciones ACH/STA),
- no regresión NACHA (filtros 60/60 y 154/154).

## 2) Pre-requisitos
- Docker + Docker Compose
- .NET SDK 10
- `dotnet-ef`

## 3) Linux/macOS
```bash
bash scripts/codex/setup-codex-env.sh
export DOTNET_ROOT=$HOME/.dotnet
export PATH=$HOME/.dotnet:$HOME/.dotnet/tools:$PATH

bash scripts/test/run-postgres-integration-tests.sh
```

Opciones:
- `--full`: ejecuta suite completa.
- `--clean`: hace `docker compose down -v` al finalizar.

## 4) Windows PowerShell
```powershell
.\scripts\test\run-postgres-integration-tests.ps1
```
Opciones:
- `-Full`
- `-Clean`

## 5) Solo tests PostgreSQL
```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build --filter "Category=Postgres" -v minimal
```

## 6) No regresión NACHA
```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~BatchNumber|FullyQualifiedName~NachaFileBuilder|FullyQualifiedName~Mapping" -v minimal

dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~Nacha|FullyQualifiedName~Mapping|FullyQualifiedName~BatchNumber" -v minimal
```

## 7) Limpiar contenedores/volúmenes
```bash
docker compose -f docker-compose.test.yml --env-file .env.test.example down -v
```

## 8) Troubleshooting
- **Puerto ocupado (5433)**: cambie `POSTGRES_PORT` en `.env.test.example`.
- **Docker no disponible**: instale Docker Engine/Desktop y valide `docker --version`.
- **dotnet no disponible**: ejecute `bash scripts/codex/setup-codex-env.sh`.
- **migraciones fallan**: valide `Database__Provider=PostgreSQL` y `ConnectionStrings__PostgresConnection`.
- **connection string incorrecto**: use formato `Host=localhost;Port=5433;Database=...`.
- **Npgsql provider no seleccionado**: confirme `Database__Provider` en `PostgreSQL/Postgres/npgsql`.

## 9) Interpretación de resultados
- `Category=Postgres` en verde confirma integración real de adapters/policy sobre PostgreSQL.
- filtros 60/60 y 154/154 en verde confirman no regresión NACHA.

## 10) Limitaciones conocidas
- En entornos sin Docker (como Codex) no se puede ejecutar PostgreSQL real; solo build/tests no-Postgres y validación sintáctica de scripts.
