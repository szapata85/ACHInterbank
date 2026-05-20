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
