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

## Actualizacion 2026-05-20 - DEF-UAT-020 nomenclatura y NACHA-M UAT

Estado productivo: NO-GO.

Resultado del ciclo controlado:

| Camara | Archivo generado | SHA256 | ZZZ | Campo 7 registro 1 | Registros | Resultado |
|---|---|---|---:|---|---|---|
| ACH Colombia | docs/uat/evidencias/nacha-m-uat/ach-colombia/0001283.002.1 | E4DAEEE551596D067357953C552CD521871F635F6703D27700171EBC10A0026E | 002 | B | 1/5/6/7/8/9 | OK tecnico UAT |
| CENIT | docs/uat/evidencias/nacha-m-uat/cenit/0001283.001.1 | FD52F7834ADEC53C720E4A877B1D48A8AC15B149BEB7FAFB91EC57CF1B88FCD4 | 001 | A | 1/5/6/7/8/9 | OK tecnico UAT; homologacion normativa formal pendiente |

Evidencia comun:

- Patron aplicado: RRRRTTT.ZZZ.1.
- Originador: Cooperativa Financiera de Antioquia, unico FinancialInstitution.IsDefaultSource=true.
- RRRR=0001 y TTT=283 derivados de la configuracion de CFA.
- Mapeo validado: 001 -> A y 002 -> B en registro tipo 1 campo 7.
- Archivos generados por /NachaExport/{cycleId}; no fueron creados manualmente.
- Sin transmision externa a ACH Colombia o CENIT.
- Proc_Contrapartidas permanece en DryRun para UAT/local.

Observacion normativa:

- ACH Colombia se valida contra MAN-004 V32.
- CENIT se valida tecnicamente con ejemplos disponibles en el proyecto y queda pendiente homologacion normativa formal.
