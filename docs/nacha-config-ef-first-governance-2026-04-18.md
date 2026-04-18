# Gobernanza EF-first para esquema NACHA-M enterprise

Fecha: 2026-04-18

## Decisión técnica

A partir de esta fase, el esquema NACHA-M enterprise usa **EF Core Code First como única fuente de verdad**.

- **Fuente oficial**: entidades + configuraciones EF + migraciones EF.
- **No oficial**: scripts SQL manuales escritos a mano.

## Implicaciones

1. Las tablas `Cat*`, `Cfg*`, `Hist*` se evolucionan exclusivamente mediante migraciones EF.
2. Cualquier script SQL para DBA debe ser **derivado** de migraciones EF (ej. `dotnet ef migrations script`) y nunca mantenido como esquema paralelo.
3. El backfill se mantiene en seeder de aplicación (`NachaConfigBackfillSeeder`), no como SQL manual.

## Pasos operativos para equipo

1. Crear migración oficial (entorno con SDK):

```bash
dotnet ef migrations add NachaEnterpriseConfigBaseline \
  --project src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj \
  --startup-project src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj \
  --context AchDbContext
```

2. Aplicar migración:

```bash
dotnet ef database update \
  --project src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj \
  --startup-project src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj \
  --context AchDbContext
```

3. Generar SQL derivado para revisión DBA (si se requiere):

```bash
dotnet ef migrations script \
  --project src/Cfa.ACHInterbank.Persistence/Cfa.ACHInterbank.Persistence.csproj \
  --startup-project src/Cfa.ACHInterbank.Api/Cfa.ACHInterbank.Api.csproj \
  --context AchDbContext \
  --idempotent \
  --output database/scripts/derived/$(date +%Y%m%d)_nacha_enterprise_from_ef.sql
```

## Estado de esta fase

- Se retiraron scripts SQL manuales creados en la fase anterior para eliminar doble fuente de verdad.
- Se reforzaron constraints e índices críticos directamente en configuraciones EF.
- Se mantuvo compatibilidad con modelo legado y backfill por código.
