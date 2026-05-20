# Evidencias Prenotificaciones CFA NACHA-M

Fecha: 2026-05-20  
Ambiente: UAT/local Docker  
Productivo: **NO-GO**

## Evidencias por camara

| Camara | Tipo | Ruta | Resultado |
|---|---|---|---|
| ACH Colombia | Consulta read-only | `docs/uat/evidencias/prenotificaciones-cfa/ach-colombia/` | Estado `Pendiente`, codigo NACHA `28`, CFA `sourceIsDefault=true` |
| CENIT | Consulta read-only | `docs/uat/evidencias/prenotificaciones-cfa/cenit/` | Estado `Pendiente`, codigo NACHA `28`, CFA `sourceIsDefault=true` |
| ACH Colombia | NACHA-M prenotificacion | `docs/uat/evidencias/nacha-m-uat/prenotificaciones/ach-colombia/` | Archivo `0001283.004.1`, no vacio, codigo `28` |
| CENIT | NACHA-M prenotificacion | `docs/uat/evidencias/nacha-m-uat/prenotificaciones/cenit/` | Archivo `0001283.002.1`, no vacio, codigo `28` |

## Hashes

| Camara | Archivo | SHA256 |
|---|---|---|
| ACH Colombia | `0001283.004.1` | `E4695D004A35087B20485339E844F7C722E059C1DA58E732219370FAC0F9155A` |
| CENIT | `0001283.002.1` | `B36BE4DB8A9EC2E3384A69A06CC0866BF24E05A2E6886B056498E361236A024C` |

## Controles

- No se documento password ni token completo.
- No se usaron datos reales.
- No se transmitio a ACH Colombia ni CENIT.
- `Proc_Contrapartidas` permanecio en `DryRun`.
- Los archivos fueron generados por `/NachaExport/{cycleId}`.
