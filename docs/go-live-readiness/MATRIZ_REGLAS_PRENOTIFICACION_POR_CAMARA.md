# Matriz Reglas Prenotificacion Por Camara

Fecha: 2026-05-19  
Estado: reglas iniciales implementadas por seed para UAT controlado  
Productivo: **NO-GO**

| Camara | Naturaleza | Tipo | Prenotificacion | Validacion identificacion receptor | Aplica NACHA Export | Fuente normativa | Estado |
|---|---|---|---|---|---|---|---|
| ACH Colombia | Debito | Debit | Obligatoria | Obligatoria | Si | MAN-004 ACH Colombia V32, 2.11.4, 2.11.4.1, 2.11.4.2, 2.11.6 | Implementada |
| ACH Colombia | Credito | Credit | Opcional | Opcional | Si | MAN-004 ACH Colombia V32, 2.10.2, 2.10.3, 2.10.3.1, 2.10.3.2 | Implementada |
| CENIT | Debito | Debit | Obligatoria | Obligatoria | Si | CENIT DSP-152 Anexo 2, 4.7 Prenotificaciones | Implementada |
| CENIT | Credito | Credit | Opcional/no obligatoria | Opcional | Si | CENIT DSP-152 Anexo 2, 4.7 Prenotificaciones | Implementada |

## Observaciones

- La matriz no sustituye homologacion externa.
- La regla CENIT no fue inferida desde ACH Colombia; se respaldo en DSP-152 Anexo 2.
- DEF-UAT-020 sigue abierto hasta generar archivos NACHA-M no vacios por sistema y validar registros 1/5/6/7/8/9.

## Revalidacion runtime 2026-05-20

Las 4 reglas aparecen activas en PostgreSQL y el endpoint `POST /api/transaction-prerequisite-policy/preview` responde:

| Caso | Decision | Resultado |
|---|---|---|
| ACH Colombia Debito | `PRENOTIFICATION_REQUIRED` | OK |
| ACH Colombia Credito | `PRENOTIFICATION_OPTIONAL` | OK |
| CENIT Debito | `PRENOTIFICATION_REQUIRED` | OK |
| CENIT Credito | `PRENOTIFICATION_OPTIONAL` | OK |

Se generaron archivos NACHA-M UAT no vacios por camara con prenotificacion debito sintetica y transaccion credito opcional. Sigue pendiente debito monetario post-prenotificacion madura por 3 dias habiles.

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
