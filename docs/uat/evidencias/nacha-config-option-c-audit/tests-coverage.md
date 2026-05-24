# Fase 6A - Cobertura de tests existente

| Test/archivo | Que cubre | Que no cubre | Sirve para Opcion C | Accion Fase 6B |
|---|---|---|---|---|
| `NachaConfigResolverTests.cs` | Resolucion basica de perfiles/config | Corte oficial sin fallback y ACH/CENIT aislados | Parcial | Agregar missing profile, ambiguous, not effective, ACH vs CENIT |
| `NachaConfigBackfillSeederTests.cs` | Backfill desde legacy | Seeds oficiales deterministas por camara | Parcial | Agregar seeds oficiales ACH/CENIT |
| `NachaConfigAdminServicesHardeningTests.cs` | Hardening servicios admin | Gobierno completo de official cutover | Parcial | Validar publish/clone/version por camara |
| `NachaFileBuilderHeaderMappingEngineTests.cs` | Mapping engine headers | Default table-driven oficial | Parcial | Exigir profile en record 1 sin fallback |
| `NachaFileBuilderControlRecordsMappingTests.cs` | Records control 8/9 | Trace oficial y fail-fast | Parcial | Validar calculos trazados con layout profile |
| `NachaFileBuilderRecord6HardeningTests.cs` | Record 6 hardening | Eliminacion dependencia legacy para DFI length | Parcial | Fallar si field requerido no existe |
| `Type7CommonMappingConvergenceTests.cs` | Convergencia type 7 | Sin fallback legacy | Parcial | Activar strict type7 por camara |
| `NachaType7RolloutPolicyTests.cs` | Rollout/fallback policy | Modo oficial sin fallback | Parcial | Bloquear fallback en Opcion C |
| `NachaExportControllerTests.cs` | Export y errores generales | Taxonomia Opcion C | Parcial | Mapear errores NACHA_* especificos |
| Angular `nacha-config-admin` specs | Render/servicio admin | Flujo oficial unico y catalogo source controlado | Parcial | Tests menu oficial/deprecacion legacy |
| Angular `nacha-layouts-definitions` specs | Legacy UX | Oficialidad Opcion C | No | Mantener solo como no regresion legacy/deprecated |

Brechas: no se prueba default table-driven oficial, aislamiento ACH/CENIT, corte sin fallback en registros 1/5/6/7/8/9, trace persistido por campo ni errores `NACHA_*`.

