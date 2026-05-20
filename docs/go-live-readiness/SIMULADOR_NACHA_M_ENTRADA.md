# Simulador NACHA-M de Entrada

Fecha: 2026-05-20  
Estado: Implementado para UAT/local  
Productivo: NO-GO

## Objetivo

Habilitar la generacion controlada de archivos NACHA-M de entrada sinteticos para que usuarios UAT los descarguen y los carguen manualmente por NachaUpload.

## Decision Arquitectonica

El simulador se implementa como capacidad separada del procesamiento real:

- API propia: `/api/uat/nacha-inbound-simulator`.
- Persistencia propia: `NachaInboundSimulations` y `NachaInboundSimulationEntries`.
- Pantalla propia: `/uat/nacha-inbound-simulator`.
- Menu propio: `UAT / Simuladores > Simulador NACHA-M Entrada`.

## Controles

| Control | Estado | Observacion |
|---|---|---|
| Solo UAT/local | OK | `appsettings.json` queda deshabilitado; Development habilita UAT |
| No auto-import | OK | `AllowAutoImport=false` y servicio no llama NachaUpload |
| No transmision externa | OK | `AllowExternalTransmission=false` |
| Datos sinteticos requeridos | OK | Guardrail configurado |
| Descarga manual | OK | Endpoint de archivo |
| Procesamiento posterior real | Pendiente | Debe ejecutarse por NachaUpload |

## Riesgos

| Riesgo | Estado | Mitigacion |
|---|---|---|
| Uso accidental fuera de UAT/local | Controlado | Config base Disabled |
| Homologacion normativa | Pendiente | Requiere validacion con carga real por NachaUpload |
| Procesamiento real | Pendiente | Fase posterior |

## Estado Readiness

La funcionalidad permite continuar UAT controlado. No habilita productivo.
