# Fase 6A - Resumen ejecutivo Opcion C NACHA-M

Fecha: 2026-05-24

Alcance: auditoria tecnica sin implementacion para evaluar si `nacha-config profiles` puede convertirse en fuente oficial NACHA-M por camara.

## Respuesta central

El proyecto no esta listo para cortar a Opcion C sin una Fase 6B de implementacion. El estado actual es **legacy-first con capacidades table-driven parciales**.

Hallazgos principales:

- `NachaGenerationOptions.Mode` tiene default `LEGACY`.
- `NachaFileBuilder` sigue cargando `NachaRecordDefinition`, `NachaRecordLayout` y `NachaRecordField`.
- `nacha-config` ya modela perfiles, camara, vigencia, estado publicado, version, variantes, campos y reglas.
- El resolver `NachaConfigResolver` existe, pero la ausencia de perfil/layout se maneja como warning/fallback, no como fallo oficial.
- El seeder de `nacha-config` backfillea desde legacy y solo crea perfil ACH salida original si no existen perfiles.
- CENIT existe en catalogo, pero no se evidencio perfil publicado completo equivalente.
- Las rutas legacy `/ach-cycles/nacha/layouts` y `/ach-cycles/nacha/definitions` siguen visibles y operativas.
- La ruta candidata oficial `/nacha-config-admin/perfiles` existe, pero no se evidencio como unica entrada oficial del menu.
- No existe trazabilidad normalizada `NachaGenerationTrace` / `NachaGenerationTraceEntry` campo-a-campo para FieldDefinition -> valor generado.
- No existe taxonomia completa de errores controlados Opcion C.

Conclusion: **Opcion C es la direccion tecnica recomendada, pero queda bloqueada por dependencias legacy, fallback silencioso, falta de perfiles CENIT/ACH oficiales completos, falta de errores controlados y falta de trace normalizado.**

Decision productiva: **Productivo NO-GO**.

