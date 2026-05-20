# Matriz NACHA-M ACH Colombia

Fecha: 2026-05-19 America/Bogota.

## Estado

Estado actual: **PARCIAL / BLOQUEADO**.

Se creo la transaccion sintetica `UAT-ACHCOL-NACHA-SOAP-001` (TransactionId `3`) y se intento generar NACHA-M por el generador real. No se obtuvo archivo valido: tras DEF-UAT-021, `/NachaExport/{cycleId}` responde `HTTP 422` JSON controlado por prenotificacion previa ausente. No se genera archivo 0 bytes como exito.

## Matriz

| Registro | Proposito | Codigo encontrado | Tests | Documentacion | Estado | Brecha | Accion recomendada |
|---|---|---|---|---|---|---|---|
| 1 | File Header | `NachaFileBuilder`, `NachaLayoutSeeder` | Tests mapping/header existentes | `docs/go-live-readiness/MATRIZ_NACHA_M_LAYOUTS.md` | No validable runtime | No hay archivo NACHA-M valido | Resolver prerequisitos de exportacion y validar posiciones/longitudes con fuente ACH Colombia oficial. |
| 5 | Batch Header | `NachaFileBuilder`, `BatchResolver` | Tests batch/header | `docs/ACH/RegistroTipo5FechaJuliana.md` parcial | No validable runtime | No hay archivo NACHA-M valido | Validar fecha efectiva, company entry description y codigos de servicio contra norma de camara. |
| 6 | Entry Detail | `NachaFileBuilder`, estrategias record 6 | Tests record 6 | Matriz tecnica parcial | No validable runtime | No hay archivo NACHA-M valido | Generar archivo no vacio con datos sinteticos y validar monto/cuenta/DFI/referencia. |
| 7 | Addenda | `NachaType7GenerationStrategy`, legacy renderer | Tests tipo 7 | Matriz tecnica parcial | No validable runtime | No hay archivo NACHA-M valido | Confirmar obligatoriedad por camara/flujo y validar tipo/longitud. |
| 8 | Batch Control | `BatchControlRecord`, calculos totales/hash | Tests control records | Matriz tecnica parcial | No validable runtime | No hay archivo NACHA-M valido | Reconciliar conteos, debitos, creditos y entry hash. |
| 9 | File Control | `FileControlRecord`, block count | Tests file integrity | Matriz tecnica parcial | No validable runtime | No hay archivo NACHA-M valido | Validar batch count, block count, padding y totales globales. |

## Evidencia

- `docs/uat/evidencias/nacha-m-uat/ach-colombia/`
- `docs/uat/UAT_NACHA_M_CAMPO_A_CAMPO.md`
- `docs/uat/EVIDENCIAS_NACHA_M_UAT.md`

## Decision

ACH Colombia NACHA-M no queda cerrado. Requiere prenotificacion UAT valida, archivo UAT no vacio, matriz campo-a-campo completa, homologacion o waiver. Productivo sigue **NO-GO**.
