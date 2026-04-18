# Validación de migración EF oficial NACHA enterprise (2026-04-18)

## Estado del entorno

- Comando ejecutado: `dotnet --info`
- Resultado: `/bin/bash: line 1: dotnet: command not found`
- Conclusión: el entorno actual no dispone de SDK .NET, por tanto no es posible ejecutar `dotnet ef` ni `dotnet test`.

## Comandos de validación intentados

1. `dotnet ef migrations add NachaEnterpriseConfigBaseline --project src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj --startup-project src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj --context AchDbContext`
2. `dotnet ef database update --project src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj --startup-project src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj --context AchDbContext`
3. `dotnet ef migrations script --project src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj --startup-project src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj --context AchDbContext --idempotent --output database/scripts/derived/20260418_nacha_enterprise_from_ef.sql`
4. `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter NachaConfigBackfillSeederTests --no-restore`

Todos los comandos fallaron con el mismo error base:

- `/bin/bash: line 1: dotnet: command not found`

## Resultado técnico de esta fase

1. **No se pudo generar físicamente la migración EF** por limitación de entorno.
2. **No se pudo aplicar migración** por la misma limitación.
3. **No se pudo generar script derivado desde EF** por la misma limitación.
4. **No se pudieron ejecutar pruebas .NET** por la misma limitación.

## Verificación de preparación en código (sí completada)

A pesar de la limitación, el código queda preparado para ejecución en entorno con SDK:

- Modelo `Cat*`, `Cfg*`, `Hist*` definido en entidades de dominio.
- Configuraciones EF con índices/constraints y seeds de catálogos.
- `AchDbContext` con DbSets para nuevo esquema y convivencia con legacy.
- Seeder de backfill inicial desde `NachaRecordDefinitions`, `NachaRecordLayouts`, `NachaRecordFields`.
- Política EF-first documentada.

## Paso siguiente obligatorio en entorno con SDK

Ejecutar en este orden:

1. `dotnet ef migrations add NachaEnterpriseConfigBaseline ...`
2. `dotnet ef database update ...`
3. `dotnet ef migrations script --idempotent ...`
4. `dotnet test ...`

Sin estos 4 pasos ejecutados y evidenciados, la migración queda en estado:

- **lista a nivel código pero no ejecutada**.
