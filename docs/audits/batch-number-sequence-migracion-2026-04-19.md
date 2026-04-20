# Batch Number Sequence - EF Code First (Prompt 11)

## Objetivo
Persistir el consecutivo DAILY_RESET multi-instancia por scope:
- ClearingHouseId
- OriginatingDfi
- ProcessingDate
- PolicyCode

## Comandos EF Code First

```bash
dotnet ef migrations add AddBatchNumberSequences \
  --project src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj \
  --startup-project src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj \
  --context AchDbContext \
  --output-dir DataBase/Migrations/Postgres
```

```bash
dotnet ef database update \
  --project src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj \
  --startup-project src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj \
  --context AchDbContext
```

## Nota
No se usan scripts SQL manuales como fuente primaria; la fuente de verdad es el modelo EF Core.
