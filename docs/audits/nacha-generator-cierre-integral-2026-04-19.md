# Cierre integral técnico NACHA-M (ACH Colombia V32 / CENIT)

Fecha: 2026-04-19

## 1) Estado por recordCode
- R1: Integrado con mapping engine + fallback legacy + shadow compare.
- R5: Integrado con mapping engine + fallback legacy + shadow compare + settlement policy por cámara.
- R6: Integrado con mapping engine + fallback legacy + shadow compare.
- R7: Convergido a mapping engine con política de rollout y fallback legacy controlado.
- R8: Campos críticos de control siguen en código, render de campos por engine cuando aplica, fallback legacy y shadow compare.
- R9: Igual enfoque prudente a R8 con cálculos críticos en runtime.

## 2) Integridad del archivo
- Se mantiene secuencia 1/5/6/7/8/9 según definiciones activas.
- Todos los records se validan a longitud esperada del layout (106 en layouts estándar).
- Se traza integridad de archivo (batch count, entry/addenda count, totales débito/crédito, block count, padding requerido).
- Se conserva padding de 9s hasta múltiplo de 10 records.

## 3) BatchNumber policy
- Política activa: `DAILY_RESET_BY_CHAMBER_DATE_ORIGINATING_DFI`.
- Scope persistente: `ClearingHouseId + OriginatingDfi + ProcessingDate + PolicyCode`.
- Store transaccional con retry por colisión unique.
- `RowVersion` agregado para concurrencia optimista.
- R5/R8 consumen el mismo batch number por lote.
- Shadow compare no solicita secuenciales adicionales (una sola asignación por build).

## 4) Shadow compare global
- Se amplió diagnóstico para registrar:
  - conteo de diferencias
  - detalles de diferencias (incluye posiciones)
  - resumen agregado (`ShadowCompareSummary`).

## 5) Validación pre-publicación
- Se mantiene hardening previo en `NachaConfigValidationService` para:
  - DSL/pipeline/fallback soportado
  - alias/canonical
  - headers/controls normativos
  - restricciones de campos críticos de control

## 6) Pruebas de cierre incorporadas
- Integridad archivo completo (orden, 106, filler, block count, totals, batch count, R5/R8 consistency) para escenarios ACH y CENIT.
- Shadow compare no consume secuenciales adicionales.
- Batch number: nuevo scope, incremento, reset diario, separación cámara/ODFI, concurrencia y verificación de RowVersion + unique scope.

## 7) Comandos para CI/local
```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj
```

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

## 8) Riesgos residuales
- En este entorno no hay SDK .NET, por lo que no se ejecutaron pruebas/migraciones aquí.
- Se requiere validación final UAT técnico con datasets regulatorios ACH/CENIT completos.
- Diferencias normativas no documentadas en repo deben declararse/regirse por documentación oficial en mesa técnica.

## 9) Checklist UAT técnico
- [ ] Validar archivos completos ACH y CENIT contra validador externo/regla de negocio.
- [ ] Revisar ShadowCompareSummary en ambiente controlado.
- [ ] Verificar fallback por record en escenarios de error de mapeo.
- [ ] Validar no regresión de totales/hash con muestra histórica.
- [ ] Ejecutar migración EF en ambiente de integración.
