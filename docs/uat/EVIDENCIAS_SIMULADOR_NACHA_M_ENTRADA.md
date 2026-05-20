# Evidencias - Simulador NACHA-M de Entrada

Fecha: 2026-05-20  
Ambiente: UAT/local  
Productivo: NO-GO

## Evidencia Tecnica

| Evidencia | Estado | Ruta/Fuente | Observacion |
|---|---|---|---|
| Entidades de dominio | OK | `NachaInboundSimulation`, `NachaInboundSimulationEntry` | Persisten metadata y entradas simuladas |
| Migracion EF Code First | OK | `AddNachaInboundSimulator` | Crea tablas del simulador |
| API | OK | `/api/uat/nacha-inbound-simulator` | Endpoints documentados para OpenAPI/Scalar |
| SPA | OK | `/uat/nacha-inbound-simulator` | Pantalla de generacion y descarga |
| Menu dinamico | OK | Seed de menu | Opcion para Admin y ACH.Operator |
| Guardrail no importacion | OK | Servicio backend | No llama NachaUpload |
| Guardrail no transmision | OK | Configuracion/metadata | `externalTransmission=false` |

## Evidencia Runtime Pendiente/Generada

Las evidencias runtime se guardan bajo:

- `docs/uat/evidencias/nacha-m-inbound-simulator/ach-colombia/`
- `docs/uat/evidencias/nacha-m-inbound-simulator/cenit/`
- `docs/uat/evidencias/nacha-m-inbound-simulator/prenotification-responses/ach-colombia/`
- `docs/uat/evidencias/nacha-m-inbound-simulator/prenotification-responses/cenit/`
- `docs/uat/evidencias/nacha-m-inbound-simulator/transaction-responses/ach-colombia/`
- `docs/uat/evidencias/nacha-m-inbound-simulator/transaction-responses/cenit/`

Cada simulacion debe conservar:

- archivo NACHA-M generado.
- `metadata.json`.
- `validation_report.md`.
- `README.md`.
- `before_state.json` si aplica.
- `expected_after_upload.json` si aplica.

## Confirmaciones

- No se incluyen passwords.
- No se incluyen tokens completos.
- No se incluyen datos reales.
- No se transmitio externamente.
- No se declaro GO productivo.

## Validacion Runtime 2026-05-20

| Escenario | Camara | Archivo | SHA256 | Resultado |
|---|---|---|---|---|
| IncomingCredit | ACH Colombia | `99999001.001.1` | `46173BD50F7864961103237C6A107B4408DBAAE1FD3089A3F963FF02721B4122` | OK tecnico UAT |
| IncomingCredit | CENIT | `99998002.001.1` | `F4675D854F3A79055FE230E23F86948F9BF2EB47AD8F36F5AD5190A25429DEDC` | OK tecnico UAT |
| IncomingPrenotificationResponse | ACH Colombia | `99999001.003.1` | `AA4B5FA47F89C0ADFE5B12DFA3D91043F6094110FB134E3C4000A37AA74F9FD5` | OK tecnico UAT |
| IncomingPrenotificationResponse | CENIT | `99998002.002.1` | `219AE7F11050FE7C60C292A1946F051BD52023B92511DC1708A69A1753176D07` | OK tecnico UAT |

Evidencia consolidada: `docs/uat/evidencias/nacha-m-inbound-simulator/runtime_summary.json`.

Controles runtime:

- `/navigation/menu` incluye `/uat/nacha-inbound-simulator`.
- `/openapi/v1.json` incluye `/api/uat/nacha-inbound-simulator`.
- Conteo `AchTransactions`: 255 antes y 255 despues.
- Conteo `IncomingNachaFileIngestions`: 0 antes y 0 despues.
- No hubo auto-import ni creacion automatica de transacciones de entrada.
