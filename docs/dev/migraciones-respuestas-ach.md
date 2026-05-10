# Migraciones de respuestas ACH

## Migración vigente
- **Id:** `20260509224122_AddAchResponseStatusMappingsAndAuditTables`
- **Contexto:** `AchDbContext`
- **Proyecto de migraciones:** `src/Cfa.ACHInterbank.Persistence`
- **Script SQL generado:** `artifacts/sql/AddAchResponseStatusMappingsAndAuditTables.sql`

## Objetos de base de datos creados
1. `AchResponses`
2. `AchResponseStatusMappings`
3. `AchResponseNotificationAttempts`

### Índices clave
- `UX_AchResponses_HashIdempotencia` (único)
- `UX_AchRespAttempts_Response_Attempt` (único)
- `IX_AchRespAttempts_Estado`
- `IX_AchRespAttempts_FechaCreacion`
- `IX_AchResponses_CorrelationId`
- `IX_AchResponses_EstadoProcesamiento`
- `IX_AchResponses_Filter`
- `IX_AchResponses_IdTransaccion`
- `IX_AchRespStatusMap_Causal`
- `IX_AchRespStatusMap_Search`
- `IX_AchRespStatusMap_Vigency`

### Relación/FK
- `AchResponseNotificationAttempts.AchResponseId -> AchResponses.Id`
- Delete behavior: **Restrict** (equivalente NO ACTION en PostgreSQL).

## Comandos operativos
> Requiere `dotnet tool restore` y `dotnet-ef` local.

### 1) Aplicar migraciones
```bash
dotnet tool restore
Database__Provider=Postgres \
ConnectionStrings__PostgresConnection="Host=localhost;Port=5433;Database=achinterbank_test;Username=ach_test;Password=ach_test_password" \
dotnet tool run dotnet-ef database update \
  --project src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj \
  --startup-project src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj \
  --context AchDbContext
```

### 2) Generar script SQL
```bash
dotnet tool run dotnet-ef migrations script \
  --project src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj \
  --startup-project src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj \
  --context AchDbContext \
  --output artifacts/sql/AddAchResponseStatusMappingsAndAuditTables.sql
```

### 3) Rollback (última migración)
```bash
dotnet tool run dotnet-ef database update <MigracionAnterior> \
  --project src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj \
  --startup-project src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj \
  --context AchDbContext
```

## Variables de entorno / connection string
- `Database__Provider=Postgres`
- `ConnectionStrings__PostgresConnection=<cadena Npgsql>`

## Notas de alcance
- Esta migración **no incluye seed funcional** ACH/CENIT.
- La persistencia usa el nombre interno `IdTransaccionServicioExterno`.
- No expone nombres físicos SOAP (por ejemplo `idTransaccionAxon`).
