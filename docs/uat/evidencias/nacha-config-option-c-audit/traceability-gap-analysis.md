# Fase 6A - Analisis de trazabilidad

| Pregunta | Respuesta |
|---|---|
| Se registra que perfil genero el archivo | Parcial, en auditoria JSON si se resolvio perfil |
| Se registra layout variant por registro | Parcial |
| Se registra `CfgLayoutField` por campo | Parcial/no normalizado |
| Se registra rawValue/renderedValue | Parcial en trazas de mapping engine, no universal |
| Se registra sourceFieldPath | Parcial |
| Se registra calculationType | No normalizado |
| Se registra validationStatus/errorCode | Parcial |
| Se registra correlationId | Parcial/generado |
| Evidencia UAT FieldDefinition -> valor generado | Parcial, no garantizada para todos los registros |

No existe entidad normalizada `NachaGenerationTrace` / `NachaGenerationTraceEntry`. La auditoria actual se apoya en `HistConfigChange` con JSON resumen y trazas internas, insuficiente para evidencia UAT campo-a-campo oficial.

Accion Fase 6B: crear trace persistido por archivo/registro/campo con profile, layoutVariant, cfgLayoutField, sourceFieldPath, rawValueSanitized, renderedValue, calculationType, validationStatus, errorCode, correlationId y clearingHouseCode.

