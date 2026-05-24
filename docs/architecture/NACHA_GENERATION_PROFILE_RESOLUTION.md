# NACHA-M Profile Resolution

Fecha: 2026-05-24

## Fase 6B.2

La exportacion oficial NACHA-M resuelve un perfil publicado y vigente antes de construir el archivo.

## Contexto de resolucion

- Camara: ACH Colombia o CENIT.
- FlowType: `ORIGINAL`, `PRENOTIFICACION` o `RETORNO` segun transacciones.
- Direction: `SALIDA` para exportaciones originales/prenotificaciones; `ENTRADA` para retornos/reversos.
- ServiceClass: codigo del lote si aplica.
- Fecha: `AchCycle.ProcessingDate`.
- Records requeridos: `1`, `5`, `6`, `7`, `8`, `9`.

## Perfiles oficiales UAT/local

| Camara | Perfil |
|---|---|
| ACH Colombia | `OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0` |
| CENIT | `OFFICIAL_CENIT_SALIDA_ORIGINAL_V1_0` |

## Fail-fast

| Condicion | Codigo |
|---|---|
| Perfil no publicado | `NACHA_PROFILE_NOT_PUBLISHED` |
| Perfil no vigente | `NACHA_PROFILE_NOT_EFFECTIVE` |
| Perfil ambiguo | `NACHA_PROFILE_AMBIGUOUS` |
| Record requerido faltante | `NACHA_REQUIRED_RECORD_MISSING` |
| Campo requerido faltante | `NACHA_REQUIRED_FIELD_MISSING` |
| Fuente no encontrada | `NACHA_FIELD_SOURCE_NOT_FOUND` |
| Longitud excedida | `NACHA_FIELD_LENGTH_INVALID` |
| Validacion de campo falla | `NACHA_FIELD_VALIDATION_FAILED` |
| Calculo falla | `NACHA_CALCULATION_FAILED` |
| Fallback legacy intentado | `NACHA_LEGACY_GENERATION_DISABLED` |

El API de exportacion conserva estos codigos en respuesta `422 Unprocessable Entity`.

Productivo: **NO-GO**.
