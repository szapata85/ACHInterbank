# UAT - Simulador NACHA-M de Entrada

Fecha: 2026-05-20  
Ambiente: UAT/local Docker  
Estado: Implementado para validacion controlada  
Productivo: NO-GO

## Alcance

El simulador NACHA-M de entrada permite generar archivos sinteticos por camara para pruebas UAT/local. El archivo resultante debe descargarse y cargarse manualmente en el flujo real NachaUpload desde la SPA.

## Guardrails

- Solo genera archivos.
- No invoca NachaUpload.
- No importa automaticamente.
- No crea transacciones de entrada.
- No cambia estados de transacciones ni prenotificaciones.
- No transmite a ACH Colombia ni CENIT.
- No invoca SOAP productivo.
- No usa datos reales.

## Camaras Soportadas

| Camara | Codigo | Estado |
|---|---:|---|
| ACH Colombia | ACHCOL | Soportada UAT/local |
| CENIT | CENIT | Soportada UAT/local |

## Escenarios Soportados

| Escenario | Descripcion | Requiere referencias |
|---|---|---|
| IncomingCredit | Credito entrante sintetico | No |
| IncomingDebit | Debito entrante sintetico | No |
| IncomingPrenotificationResponse | Respuesta de prenotificacion | Si, prenotificaciones pendientes |
| IncomingCreditConfirmation | Confirmacion de credito | Si, transacciones UAT |
| IncomingCreditRejection | Rechazo de credito | Si, transacciones UAT y causal |
| IncomingCreditReturn | Devolucion de credito | Si, transacciones UAT y causal |
| IncomingDebitConfirmation | Confirmacion de debito | Si, transacciones UAT |
| IncomingDebitRejection | Rechazo de debito | Si, transacciones UAT y causal |
| IncomingDebitReturn | Devolucion de debito | Si, transacciones UAT y causal |

## API

Ruta base: `/api/uat/nacha-inbound-simulator`

| Metodo | Ruta | Uso |
|---|---|---|
| POST | `/generate` | Genera archivo simulado |
| GET | `/` | Lista simulaciones |
| GET | `/{id}` | Consulta detalle |
| GET | `/{id}/file` | Descarga archivo |
| GET | `/{id}/evidence` | Consulta metadata/evidencia |
| POST | `/eligibility-preview` | Valida elegibilidad antes de generar |

## SPA

Ruta: `/uat/nacha-inbound-simulator`  
Menu esperado: `UAT / Simuladores > Simulador NACHA-M Entrada`

## Resultado Esperado

La simulacion queda en estado `Generated`, con:

- `generatedOnly=true`
- `autoImported=false`
- `uploadRequired=true`
- `externalTransmission=false`

## Ajuste origen/destino 2026-05-20

- La entidad originadora externa se selecciona desde `FinancialInstitution` filtrando `IsDefaultSource != true`.
- La entidad destino/receptora no es editable por el usuario.
- La entidad destino/receptora se resuelve automaticamente desde `FinancialInstitution.IsDefaultSource = true`.
- Para ACH Interbank, el destino/receptor default esperado es CFA / Cooperativa Financiera de Antioquia.
- El request del simulador envia `originFinancialInstitutionId`.
- El backend persiste y expone en metadata `originFinancialInstitutionId`, `originIsDefaultSource=false`, `destinationFinancialInstitutionId`, `destinationIsDefaultSource=true` y `destinationResolvedFrom=FinancialInstitution.IsDefaultSource`.
- Se mantiene el guardrail: el simulador solo genera archivos, no llama NachaUpload, no autoimporta, no crea transacciones y no cambia estados.

## Limitaciones

La validacion de procesamiento real se realiza en una fase posterior cargando manualmente el archivo por NachaUpload. Esta fase no cierra homologacion normativa ni salida productiva.

## Resultado Runtime 2026-05-20

Validacion ejecutada contra SPA/API Docker en `http://localhost:743`:

- Health live/ready: OK.
- Login demo: OK, token recibido y no documentado.
- Roles confirmados: Admin, ACH.Operator.
- Menu dinamico: OK, incluye Simulador NACHA-M Entrada.
- OpenAPI/Scalar: OK, `/openapi/v1.json` incluye endpoints del simulador.
- Generacion ACH Colombia IncomingCredit: OK.
- Generacion CENIT IncomingCredit: OK.
- Generacion ACH Colombia IncomingPrenotificationResponse: OK.
- Generacion CENIT IncomingPrenotificationResponse: OK.
- No se crearon transacciones de entrada automaticamente.
- No se crearon registros de ingestion NachaUpload.
- No hubo transmision externa.

Archivos y hashes: ver `docs/uat/EVIDENCIAS_SIMULADOR_NACHA_M_ENTRADA.md`.
