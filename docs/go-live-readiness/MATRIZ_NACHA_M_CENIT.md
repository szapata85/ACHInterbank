# Matriz NACHA-M CENIT

Fecha: 2026-05-19 America/Bogota.

## Estado

Estado actual: **PARCIAL / BLOQUEADO**.

Se creo la transaccion sintetica `UAT-CENIT-NACHA-SOAP-001` (TransactionId `4`) y se intento generar NACHA-M por el generador real. No se obtuvo archivo valido: tras DEF-UAT-021, `/NachaExport/{cycleId}` responde `HTTP 422` JSON controlado por prenotificacion previa ausente. No se genera archivo 0 bytes como exito.

## Matriz

| Registro | Proposito | Codigo encontrado | Tests | Documentacion | Estado | Brecha | Accion recomendada |
|---|---|---|---|---|---|---|---|
| 1 | File Header | `NachaFileBuilder`, normalizacion identificador CENIT | Tests header/naming existentes | Matriz tecnica parcial | No validable runtime | No hay archivo NACHA-M valido | Validar identificadores CENIT con fuente oficial o waiver. |
| 5 | Batch Header | `NachaFileBuilder`, `BatchResolver` | Tests batch/header | Matriz tecnica parcial | No validable runtime | No hay archivo NACHA-M valido | Validar ciclo CENIT, fecha efectiva y company entry description. |
| 6 | Entry Detail | `NachaFileBuilder`, estrategias record 6 | Tests record 6 | Matriz tecnica parcial | No validable runtime | No hay archivo NACHA-M valido | Generar archivo no vacio y validar DFI, monto, cuenta y referencia. |
| 7 | Addenda | `NachaType7GenerationStrategy`, legacy renderer | Tests tipo 7 | Matriz tecnica parcial | No validable runtime | No hay archivo NACHA-M valido | Confirmar si aplica addenda CENIT por flujo y validar estructura. |
| 8 | Batch Control | `BatchControlRecord`, calculos totales/hash | Tests control records | Matriz tecnica parcial | No validable runtime | No hay archivo NACHA-M valido | Reconciliar conteos, debitos, creditos y entry hash. |
| 9 | File Control | `FileControlRecord`, block count | Tests file integrity | Matriz tecnica parcial | No validable runtime | No hay archivo NACHA-M valido | Validar batch count, block count, padding y totales globales. |

## Evidencia

- `docs/uat/evidencias/nacha-m-uat/cenit/`
- `docs/uat/UAT_NACHA_M_CAMPO_A_CAMPO.md`
- `docs/uat/EVIDENCIAS_NACHA_M_UAT.md`

## Decision

CENIT NACHA-M no queda cerrado. Requiere prenotificacion UAT valida, archivo UAT no vacio, matriz campo-a-campo completa, homologacion o waiver. Productivo sigue **NO-GO**.

## Parametrizacion 2026-05-19

| Naturaleza | Prenotificacion | Fuente normativa | Implementacion | Estado |
|---|---|---|---|---|
| Debito | Obligatoria previa | CENIT DSP-152 Anexo 2, seccion 4.7 | `ClearingHouseTransactionRule` seed CENIT Debit | Implementada para UAT controlado |
| Credito | No obligatoria/opcional | CENIT DSP-152 Anexo 2, seccion 4.7 | `ClearingHouseTransactionRule` seed CENIT Credit | Implementada para UAT controlado |

Pendiente: crear prenotificacion UAT valida para debitos y reintentar archivo NACHA-M no vacio por sistema.
## Evidencia runtime 2026-05-20

Archivo generado por sistema: `docs/uat/evidencias/nacha-m-uat/cenit/nacha-m-uat-cenit-20260520.ach`.

| Control | Resultado |
|---|---|
| HTTP `/NachaExport/{cycleId}` | 200 |
| Tamano | 1060 bytes |
| SHA256 | `248205FCE69769B8047FEED94346E2E9910918B386D553BC46D6F1218B3D125C` |
| Registros | 1:1, 5:1, 6:2, 7:2, 8:1, 9:3 |
| Transmision externa | No |
| Estado | OK tecnico parcial; homologacion campo-a-campo pendiente |
