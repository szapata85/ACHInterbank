# UAT Prenotificaciones CFA NACHA-M

Fecha de ejecucion: 2026-05-20  
Rama: `feature/parametrizacion-reglas-camara-prenotificacion`  
Commit base: `33bbc145`  
Ambiente: UAT/local Docker (`http://localhost:743`)  
Decision productiva: **NO-GO**

## Alcance

Se ejecuto una prueba UAT controlada para crear prenotificaciones tecnicas originadas por CFA / Cooperativa Financiera de Antioquia, consultar su estado funcional y generar archivos NACHA-M de prenotificacion por camara.

No se usaron datos reales, no se transmitieron archivos a camaras reales y `Proc_Contrapartidas` permanecio en modo `DryRun`.

## Servicio de consulta

Se habilito consulta read-only autenticada:

- `GET /api/prenotifications/by-reference/{reference}`
- `GET /api/prenotifications/{id}`

La consulta devuelve estado tecnico, descripcion en espanol, codigo NACHA, camara, banco origen, maduracion y habilitacion para debito monetario posterior. No modifica datos.

## Prenotificaciones creadas

| Camara | Referencia | TransactionId | Banco origen | Codigo NACHA | Estado | Descripcion |
|---|---|---:|---|---:|---|---|
| ACH Colombia | `UAT-ACH-PRE-CFA-001` | 256 | Cooperativa Financiera de Antioquia | 28 | Pending | Pendiente |
| CENIT | `UAT-CEN-PRE-CFA-001` | 257 | Cooperativa Financiera de Antioquia | 28 | Pending | Pendiente |

La maduracion de 3 dias habiles se informa para habilitar debito monetario posterior. No se aplico backdating ni bypass.

## Archivos NACHA-M generados

| Camara | Archivo | Tamano | SHA256 | ZZZ | Campo 7 registro 1 | Codigo NACHA |
|---|---|---:|---|---:|---|---:|
| ACH Colombia | `0001283.004.1` | 1060 bytes | `E4695D004A35087B20485339E844F7C722E059C1DA58E732219370FAC0F9155A` | 004 | D | 28 |
| CENIT | `0001283.002.1` | 1060 bytes | `B36BE4DB8A9EC2E3384A69A06CC0866BF24E05A2E6886B056498E361236A024C` | 002 | B | 28 |

Ambos archivos cumplen patron `RRRRTTT.ZZZ.1`, usan prefijo CFA `0001283`, contienen registros `1/5/6/7/8/9` y no fueron transmitidos externamente.

## Resultado

UAT de prenotificaciones CFA: **OK tecnico UAT**.  
DEF-UAT-020: **OK tecnico UAT para generacion de prenotificaciones NACHA-M; permanece parcial normativo por homologacion formal CENIT/campo-a-campo externo**.  
Productivo: **NO-GO**.
