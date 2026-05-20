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

## Parametrizacion 2026-05-19

| Naturaleza | Prenotificacion | Fuente normativa | Implementacion | Estado |
|---|---|---|---|---|
| Debito | Obligatoria | MAN-004 V32, secciones 2.11.4, 2.11.4.1, 2.11.4.2, 2.11.6 | `ClearingHouseTransactionRule` seed ACH Colombia Debit | Implementada para UAT controlado |
| Credito | Opcional | MAN-004 V32, secciones 2.10.2, 2.10.3, 2.10.3.1, 2.10.3.2 | `ClearingHouseTransactionRule` seed ACH Colombia Credit | Implementada para UAT controlado |

Pendiente: crear prenotificacion UAT valida para debitos y reintentar archivo NACHA-M no vacio por sistema.
## Evidencia runtime 2026-05-20

Archivo generado por sistema: `docs/uat/evidencias/nacha-m-uat/ach-colombia/nacha-m-uat-ach-colombia-20260520.ach`.

| Control | Resultado |
|---|---|
| HTTP `/NachaExport/{cycleId}` | 200 |
| Tamano | 1060 bytes |
| SHA256 | `8EA137CBDCEA6CC4280E5183A66FD29983FE0BF0D4F42732A477AC18DD211844` |
| Registros | 1:1, 5:1, 6:2, 7:2, 8:1, 9:3 |
| Transmision externa | No |
| Estado | OK tecnico parcial; homologacion campo-a-campo pendiente |
