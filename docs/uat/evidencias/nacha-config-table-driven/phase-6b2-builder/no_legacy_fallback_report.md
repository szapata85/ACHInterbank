# Fase 6B.2 - Reporte de no fallback legacy

Fecha: 2026-05-24

## Resultado

La generacion oficial NACHA-M usa `NachaGenerationOptions.Mode=TABLE_DRIVEN` por defecto y resuelve `CfgProfile` publicado/vigente antes de construir el archivo.

## Controles aplicados

- `LoadLayoutsAsync` no se invoca en modo oficial.
- `LoadDefinitionsAsync` no se invoca en modo oficial.
- `NachaRecordLayout` y `NachaRecordDefinition` quedan solo para modo `LEGACY`, pruebas historicas o flujos no oficiales.
- Si falta perfil, record, field, source, longitud o calculo, se lanza `NachaGenerationException` con codigo funcional.
- No se transmite externamente.

## Codigos fail-fast cubiertos

- `NACHA_PROFILE_NOT_PUBLISHED`
- `NACHA_REQUIRED_RECORD_MISSING`
- `NACHA_REQUIRED_FIELD_MISSING`
- `NACHA_FIELD_SOURCE_NOT_FOUND`
- `NACHA_FIELD_LENGTH_INVALID`
- `NACHA_CALCULATION_FAILED`
- `NACHA_LEGACY_GENERATION_DISABLED`

Productivo: **NO-GO**.
