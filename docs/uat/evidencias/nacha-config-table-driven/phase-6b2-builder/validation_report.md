# Fase 6B.2 - Validation Report

## Alcance

Cambiar la generacion oficial NACHA-M para usar perfiles `nacha-config` publicados/vigentes, separados por camara, sin fallback legacy silencioso.

## Resultado tecnico

| Criterio | Resultado |
|---|---|
| Default oficial deja de ser `LEGACY` | OK |
| ACH Colombia resuelve `OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0` | OK |
| CENIT resuelve `OFFICIAL_CENIT_SALIDA_ORIGINAL_V1_0` | OK |
| Records 1/5/6/7/8/9 renderizados desde `CfgLayoutVariant`/`CfgLayoutField` | OK |
| Archivo ACH Colombia no vacio | OK |
| Archivo CENIT no vacio | OK |
| Missing profile bloquea controladamente | OK |
| Missing required field bloquea controladamente | OK |
| Field length invalid bloquea controladamente | OK |
| No fallback legacy en modo oficial | OK |
| Cambio ACH no afecta CENIT | OK |
| Cambio CENIT no afecta ACH | OK |

## Brechas pendientes

- Fase 6B.3: trace `FieldDefinition -> valor generado`.
- Fase 6B.4: deprecacion/redireccion SPA legacy.
- Homologacion externa y aprobaciones productivas.

Productivo: **NO-GO**.
