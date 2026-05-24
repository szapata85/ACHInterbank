# Fase 6A - Evaluacion del modelo oficial candidato

| Capacidad | Estado | Evidencia tecnica | Brecha |
|---|---|---|---|
| Camara | Soportada | `CfgProfile.ClearingHouseId`, `CatClearingHouse` | Falta demostrar perfiles completos ACH y CENIT |
| ACH Colombia | Catalogada | `CatClearingHouse` code `ACH` | Perfil publicado oficial depende de backfill legacy |
| CENIT | Catalogada | `CatClearingHouse` code `CENIT` | No se evidencio perfil publicado completo |
| Vigencia | Soportada | `EffectiveFrom`, `EffectiveTo` | Falta fail-fast oficial si no esta vigente |
| Estado publicado | Soportado | `CatConfigStatus`, `PublishedAt` | Falta uso obligatorio en generacion |
| Version normativa | Soportada | `VersionMajor`, `VersionMinor` | Falta politica de version oficial por camara |
| Registros 1/5/6/7/8/9 | Soportados | `CatRecordCode`, `CfgProfileRecord`, `CfgLayoutVariant` | Cobertura real depende de seeds/backfill |
| Posicion/longitud | Soportada | `CfgLayoutField.StartPosition`, `Length` | Falta bloqueo runtime ante longitud invalida |
| Source field path | Soportado | `CfgFieldSourceDefinition.PropertyPath` | Debe controlarse desde catalogo, no texto libre operacional |
| Constantes | Soportadas | `DataSourceType.CONSTANTE`, `ConstantValue` | Deben quedar auditables por campo |
| Calculos/reglas | Parcial | `CfgRule`, `CfgRuleSet`, data source `EXPRESION` | Calculos criticos NACHA deben permanecer en codigo y trazarse |
| Validaciones | Parcial | `NachaConfigValidationService` | Falta taxonomia runtime Opcion C |

El catalogo incluye `SQL_VIEW` y `SQL_PROCEDURE`. Para Opcion C no debe exponerse SQL libre desde UI. Si se permite SQL, debe ser por catalogo controlado y revision tecnica, nunca por entrada arbitraria.

El modelo candidato es reutilizable y suficientemente cercano para convertirse en oficial, pero necesita enforcement en builder, seeds oficiales por camara, source catalog controlado y trazabilidad normalizada.

