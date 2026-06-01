# Fase 6A - Analisis del flujo NachaFileBuilder

## Flujo actual

1. `NachaExportController` recibe la solicitud de exportacion.
2. El servicio de exportacion carga ciclo/transacciones.
3. `NachaFileBuilder.BuildFileAsync` determina camara con logica basada en `ClearingHouse.Name` que contiene `CENIT`; si no, usa `ACH`.
4. Carga definiciones legacy desde `NachaRecordDefinitions`; si no hay, usa definiciones default.
5. Crea `layoutCache` desde `NachaRecordLayouts`.
6. Resuelve runtime config con `NachaConfigResolver` solo como capacidad adicional.
7. Segun modo y flags, decide entre legacy, hybrid, table-driven o shadow compare.
8. Genera registros 1/5/6/7/8/9 con mezcla de objetos calculados, renderer legacy, renderer table-driven y fallback.
9. Persiste auditoria resumen en `HistConfigChange` si hubo perfil.

## Modos

| Modo | Existe | Default | Observacion |
|---|---|---|---|
| `LEGACY` | Si | Si | Modo default actual |
| `HYBRID` | Si | No | Permite usar perfiles si estan disponibles |
| `TABLE_DRIVEN` | Si | No | No elimina todos los fallback por si solo |
| `SHADOW_COMPARE` | Si | No | Compara salidas para migracion |

## Riesgos

- Faltante de perfil/layout puede quedar como warning y fallback.
- Registros 1/5/8/9 tienen caminos de fallback a objetos calculados/legacy.
- Registro 6 depende de legacy para longitud de `ReceivingDFI`.
- Registro 7 tiene rollout policy y fallback legacy opcional.
- Renderer de ancho fijo trunca valores largos y omite valores faltantes sin error oficial Opcion C.
- `FileIdModifier` puede normalizarse despues de construir el archivo, fuera del perfil/trace.

Accion Fase 6B: crear modo oficial estricto o convertir `TABLE_DRIVEN` a comportamiento fail-fast parametrizado.

