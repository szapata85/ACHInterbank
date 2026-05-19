# Matriz NACHA-M Layouts - ACH Interbank

Fecha de revalidacion: 2026-05-19 America/Bogota
Rama: `fix/spa-functional-root-routes-proxy`
Commit base: `49b810f9`
Ambiente: Docker Compose local, SPA `http://localhost:743`, API directa `http://localhost:843`.

## Resultado Ejecutivo

La configuracion tecnica de layouts NACHA-M existe para registros `1`, `5`, `6`, `7`, `8` y `9`. Se confirmaron:

- Entidades EF Code First: `NachaRecordDefinition`, `NachaRecordLayout`, `NachaRecordField`.
- Seed de definiciones: 6 registros en `NachaRecordDefinitionConfiguration`.
- Seed de layouts: 6 layouts en `NachaLayoutSeeder`.
- Endpoints API: `GET /nacha-layouts` y `GET /nacha-record-definitions`.
- Proxy SPA Docker corregido: `http://localhost:743/nacha-layouts` y `http://localhost:743/nacha-record-definitions` devuelven JSON con token, y 401 controlado sin token.

No se cierra aun la validacion normativa campo-a-campo ni homologacion bancaria. Productivo permanece **NO-GO**.

## Matriz Por Registro

| Registro | Proposito | Codigo encontrado | Tests | Documentacion | Estado | Brecha | Accion recomendada |
|---|---|---|---|---|---|---|---|
| 1 | Encabezado de archivo | `NachaLayoutSeeder` (`FILE_HEADER`), `NachaFileBuilder`, `NachaRecordConfigProvider`, endpoint `/nacha-layouts` | `NachaFileBuilderHeaderMappingEngineTests`, `NachaRecordConfigProviderTests`, validaciones de config admin | `docs/uat/nacha-records-acceptance-checklist.md`, matriz normativa actual | OK tecnico / PARCIAL normativo | Falta firma campo-a-campo y vector externo | Ejecutar comparativo formal contra ACH Colombia/CENIT y adjuntar evidencia. |
| 5 | Encabezado de lote | `NachaLayoutSeeder` (`BATCH_HEADER`), `NachaFileBuilder`, resolver de `CompanyEntryDescription` | `NachaFileBuilderHeaderMappingEngineTests`, `NachaConfigAdminServicesHardeningTests` | Checklist NACHA y docs de configuracion | OK tecnico / PARCIAL normativo | Falta cierre de reglas por camara/flujo | Validar campos, codigos SEC/servicio y fechas con evidencia UAT. |
| 6 | Detalle de transaccion | `NachaLayoutSeeder` (`ENTRY_DETAIL`), `EntryDetailRecord`, mapping engine record 6, validaciones DFI/check digit | `NachaFileBuilderRecord6HardeningTests`, `AchTransactionNachaTests` | Checklist NACHA y docs de transacciones | OK tecnico / PARCIAL normativo | Falta dataset anonimo representativo y aceptacion formal | Ejecutar UAT con transacciones sinteticas/anonimizadas y verificar longitudes/totales. |
| 7 | Addenda | `NachaLayoutSeeder` (`ADDENDA`), `NachaType7GenerationStrategy`, `NachaType7LegacyRenderer`, common mapping engine | `NachaType7GenerationStrategyTests`, `Type7CommonMappingConvergenceTests`, `NachaType7RolloutPolicyTests` | Checklist NACHA y docs mapping engine | PARCIAL | Conviven fallback legacy, shadow compare y rollout policy; falta cierre por variantes de negocio | Mantener fallback; cerrar matriz de variantes 05/99/retornos antes de preproductivo. |
| 8 | Control de lote | `NachaLayoutSeeder` (`BATCH_CONTROL`), `BatchControlRecord`, calculo `EntryHash`, totales debito/credito | `NachaFileBuilderControlRecordsMappingTests`, pruebas config admin de controles 8/9 | Checklist NACHA y docs auditoria | OK tecnico / PARCIAL normativo | Falta reconciliacion formal de totales contra archivo completo | Generar archivo controlado y reconciliar conteos, hash y montos con evidencia. |
| 9 | Control de archivo | `NachaLayoutSeeder` (`FILE_CONTROL`), `FileControlRecord`, `BlockCount`, `EntryHash`, totales | `NachaFileBuilderControlRecordsMappingTests`, `NachaFileBuilderFileIntegrityClosureTests` | Checklist NACHA y docs auditoria | OK tecnico / PARCIAL normativo | Falta homologacion externa y evidencia de padding/block count | Validar archivo completo con vector oficial o waiver formal. |

## Evidencia Runtime

| Control | Resultado |
|---|---|
| `GET http://localhost:843/nacha-layouts` con token | HTTP 200, `application/json`, 6 registros. |
| `GET http://localhost:843/nacha-record-definitions` con token | HTTP 200, `application/json`, 6 registros. |
| `GET http://localhost:843/nacha-record-layouts` con token | HTTP 404 controlado; no es la ruta real. |
| `GET http://localhost:743/nacha-layouts` sin token | HTTP 401 controlado, no HTML. |
| `GET http://localhost:743/nacha-record-definitions` sin token | HTTP 401 controlado, no HTML. |
| `GET http://localhost:743/nacha-layouts` con token | HTTP 200, `application/json`, 6 registros. |
| `GET http://localhost:743/nacha-record-definitions` con token | HTTP 200, `application/json`, 6 registros. |
| `GET http://localhost:743/nacha-config/catalogos-filtro` con token | HTTP 200, `application/json`. |

## Decision

DEF-UAT-019 queda cerrado como defecto tecnico de endpoint/proxy: la ruta real es `/nacha-layouts`, no `/nacha-record-layouts`, y el proxy SPA Docker fue corregido.

La brecha normativa NACHA-M permanece **PARCIAL** hasta ejecutar validacion campo-a-campo, generacion/carga de archivo controlado, firma de matriz regulatoria y homologacion externa o waiver formal.
