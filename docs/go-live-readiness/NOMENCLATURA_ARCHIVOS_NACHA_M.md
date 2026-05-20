# Nomenclatura archivos NACHA-M

Fecha: 2026-05-20  
Estado productivo: NO-GO

## Regla parametrizada

La salida NACHA-M por camara usa regla parametrizada en `NachaFileNamingRule`, no hard-codeada en el generador.

| Camara | Patron | Secuencia | Mapeo campo 7 | Fuente normativa | Estado |
|---|---|---|---|---|---|
| ACH Colombia | RRRRTTT.ZZZ.1 | 001-036 | 001-026 => A-Z; 027-036 => 0-9 | MAN-004 V32 | OK tecnico UAT |
| CENIT | RRRRTTT.ZZZ.1 | 001-036 | 001-026 => A-Z; 027-036 => 0-9 | Ejemplos CENIT del proyecto | OK tecnico UAT; homologacion formal pendiente |

## Originador default

El originador default se resuelve desde `FinancialInstitution.IsDefaultSource=true`. En UAT/local se confirmo una unica institucion default:

- Cooperativa Financiera de Antioquia.
- Codigo ruta usado para evidencia: 0001.
- Codigo transito usado para evidencia: 283.
- Prefijo generado: 0001283.

No se usa `Banco UAT Origen` como originador default.

## Evidencia runtime

| Camara | Archivo | ZZZ | Campo 7 | Hash SHA256 |
|---|---|---:|---|---|
| ACH Colombia | docs/uat/evidencias/nacha-m-uat/ach-colombia/0001283.002.1 | 002 | B | E4DAEEE551596D067357953C552CD521871F635F6703D27700171EBC10A0026E |
| CENIT | docs/uat/evidencias/nacha-m-uat/cenit/0001283.001.1 | 001 | A | FD52F7834ADEC53C720E4A877B1D48A8AC15B149BEB7FAFB91EC57CF1B88FCD4 |

## Controles

- Maximo diario parametrizado: 36 archivos por camara/originador/fecha.
- No reutilizacion: `ExternalFileNamePolicy` reserva secuencia externa y evita reutilizacion de nombre exitoso del dia.
- Validacion de coherencia: nombre externo y registro tipo 1 se normalizan/validan con el mismo identificador interno.
- Si falta configuracion RRRR/TTT o regla activa, el sistema debe devolver error funcional controlado.

## Decision

Nomenclatura queda OK tecnico UAT. CENIT mantiene brecha de homologacion documental formal, sin bloquear la parametrizacion tecnica ni la evidencia UAT local.
