# Fase 6B - Plan exacto de implementacion recomendado

1. Definir modo oficial estricto: activar `TABLE_DRIVEN_STRICT` o equivalente, bloquear fallback legacy y mantener legacy solo como comparador/historico.
2. Crear perfiles oficiales ACH Colombia y CENIT con registros 1/5/6/7/8/9, estado `PUBLICADO`, vigencia explicita y version normativa.
3. Sustituir `NachaRecordDefinition` por `CfgProfileRecord` en modo oficial.
4. Sustituir `NachaRecordLayout`/`NachaRecordField` por `CfgLayoutVariant`/`CfgLayoutField`.
5. Eliminar dependencia legacy de longitud `ReceivingDFI`.
6. Mantener en codigo calculos criticos: EntryHash, BlockCount, BatchCount, totales, check digit. Trazarlos como fuentes calculadas.
7. Implementar errores controlados: `NACHA_PROFILE_NOT_PUBLISHED`, `NACHA_PROFILE_NOT_EFFECTIVE`, `NACHA_PROFILE_AMBIGUOUS`, `NACHA_REQUIRED_RECORD_MISSING`, `NACHA_REQUIRED_FIELD_MISSING`, `NACHA_FIELD_LENGTH_INVALID`, `NACHA_FIELD_VALIDATION_FAILED`, `NACHA_FIELD_SOURCE_NOT_FOUND`, `NACHA_CALCULATION_FAILED`, `NACHA_LEGACY_GENERATION_DISABLED`.
8. Implementar `NachaGenerationTrace` y `NachaGenerationTraceEntry`.
9. Declarar `/nacha-config-admin/perfiles` como modulo oficial SPA.
10. Deprecar/redirigir legacy layouts/definitions.
11. Usar catalogo controlado para `sourceFieldPath`; no permitir SQL libre.
12. Agregar tests de aislamiento ACH/CENIT, missing profile/record/field, no fallback, no truncado silencioso, trace persistido, y SPA oficial.
13. Generar evidencia UAT: archivo ACH desde perfil oficial, archivo CENIT desde perfil oficial, cambio aislado por camara, reporte de no fallback.

Criterio de cierre 6B: generacion oficial falla sin perfil publicado/vigente, no usa legacy funcional, separa ACH/CENIT por perfil, persiste trace campo-a-campo y tiene pruebas automatizadas.

