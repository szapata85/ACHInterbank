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
