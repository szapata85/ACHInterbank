# Recomendacion Final - Comite GO/NO-GO

Fecha: 2026-05-19
Decision recomendada: Continuar UAT controlado / NO-GO productivo

## Recomendacion Principal

Se recomienda continuar con UAT controlado, usando exclusivamente datos sinteticos o anonimizados y manteniendo el alcance fuera de operacion productiva.

## Recomendacion Explicita

No aprobar productivo todavia.

## Condiciones Para Avanzar

- Cerrar brechas criticas y altas.
- Ejecutar UAT funcional formal.
- Firmar actas.
- Validar operacion, seguridad y soporte.
- Validar interoperabilidad externa.
- Completar backup/restore/rollback.
- Completar NACHA-M campo-a-campo y homologacion o waiver.
- Cumplir prerequisitos de prenotificacion/exportacion sin bypass para generar NACHA-M no vacio.
- Mantener `Proc_Contrapartidas` en `DryRun/Disabled` hasta contar con endpoint UAT/mock autorizado para modo `Live`.
- Mantener evidencia de `ACH.Operator` para UAT controlado y definir usuario operador separado si seguridad lo exige para preproductivo/productivo.

## Texto Formal Recomendado

Con base en la evidencia tecnica y funcional disponible, el proyecto ACH Interbank presenta avances significativos en CI, runtime Docker, autenticacion, proxy SPA/API, trazabilidad, idempotencia documentada, UAT tecnico, cierre de `ACH.Operator` para el usuario demo multirol, export NACHA con error controlado y evidencia SOAP dry-run sin transmision externa. Sin embargo, el UAT integrado NACHA-M no genero archivos no vacios para ACH Colombia ni CENIT y persisten brechas funcionales, operativas, de seguridad e interoperabilidad que impiden recomendar salida productiva. Se recomienda continuar con UAT controlado y mantener decision NO-GO productivo hasta nuevo comite.

Actualizacion: la parametrizacion de reglas por camara/naturaleza ya esta implementada para UAT controlado, pero no reemplaza el reintento NACHA-M con prenotificaciones validas ni la homologacion/waiver formal. Productivo permanece **NO-GO**.
## Actualizacion 2026-05-20

La recomendacion se mantiene: continuar UAT controlado y no aprobar productivo. La generacion NACHA-M UAT no vacia por camara es un avance tecnico, no una homologacion productiva.

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
