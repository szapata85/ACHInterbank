# Evidencias SOAP RegistrarRespuestaTransaccion

Fecha: 2026-05-21

## Clasificacion funcional

- IntegrationKey: `WSAXON`
- OperationKey: `RegistrarRespuestaTransaccion`
- Cliente tecnico: `WsAxonRespuestaTransaccionesSoapClient`
- Naturaleza: respuesta diferencial / notificacion.
- Mueve dinero: no.
- Proposito mapping: `DifferentialResponseNotification`.

## Hallazgos

- `NotificarRespuestaAchUseCase` solo actualiza estado de intento/respuesta.
- `RespuestaTransaccionesAchGateway` depende de `IWsAxonRespuestaTransaccionesSoapClient`.
- No depende de `IWscfaachSoapClient`.
- No se observaron llamadas a `Proc_Contrapartidas` ni `Proc_Transacciones`.
- Valida readiness `WSAXON / RegistrarRespuestaTransaccion / DifferentialResponseNotification / InboundResponse`.
- Persiste trace campo-a-campo mediante `IntegrationMappingTraceWriter`.
- Si el trace detecta campo requerido faltante, no invoca gateway y deja error funcional controlado.
- Mantiene `MonetaryMovementCreated=false`.

## Estado

`DEF-UAT-SOAP-MAP-003` queda **cerrado tecnicamente** en alcance UAT/local: consume mapping publicado para trace, persiste entradas campo-a-campo y conserva guardrail no monetario.

Productivo: **NO-GO**.

## Actualizacion 2026-05-23 - cruce NACHA/prenotificaciones

Se mantiene cerrado el guardrail no monetario de `RegistrarRespuestaTransaccion`:

- `MovesMoney=false`.
- No inyecta `IWscfaachSoapClient`.
- No llama `Proc_Contrapartidas`.
- No llama `Proc_Transacciones`.
- Valida readiness de `WSAXON / RegistrarRespuestaTransaccion / DifferentialResponseNotification / InboundResponse`.
- Persiste trace campo-a-campo.

Avance aplicado:

- El catalogo controlado ahora expone fuentes `DifferentialResponse` y `Prenotification`.
- El catalogo tambien expone las seis fuentes NACHA-M desagregadas para que los mappings puedan cruzar respuesta, archivo y transaccion/prenotificacion.

## Actualizacion 2026-05-23 - cierre DEF-UAT-SOAP-MAP-004

Se implemento el caso de uso end-to-end que aplica respuesta diferencial sobre `AchTransaction.IsPrenotification=true`:

- Respuesta aprobada: `Pending -> Certified`.
- Respuesta rechazada con causal `R03`: `Pending -> ReturnedByEpr`.
- Se crea `AchTransactionStateEvent`.
- Se persisten `IntegrationMappingTrace` y `IntegrationMappingTraceEntries`.
- Se cruza payload, NACHA-M desagregado y prenotificacion interna.
- Missing mapping falla controladamente.
- Prenotificacion no encontrada falla controladamente.
- Duplicado queda controlado.
- No se mueve dinero.
- No se afectan saldos.
- No se invoca `IWscfaachSoapClient`.
- No se invoca `Proc_Contrapartidas`.
- No se invoca `Proc_Transacciones`.

Evidencia:

- `docs/uat/evidencias/soap-integrations/prenotification-responses/approved/`
- `docs/uat/evidencias/soap-integrations/prenotification-responses/rejected/`

`DEF-UAT-SOAP-MAP-004`: **cerrado tecnico UAT**.

## Actualizacion 2026-08-20 - DIFF-RESP-001

Estado: **CLOSED**.

Ultimo paso completado: cierre transaccional LIVE local de una respuesta diferencial aprobada, con replay idempotente y verificacion UI sobre el registro persistido.

Gap corregido:

- La correlacion usaba seleccion del primer resultado y no detectaba multiples entradas, vinculos o prenotificaciones compatibles.
- El timeout SOAP quedaba habilitado para reintento aunque el resultado de entrega fuera desconocido.
- El procesamiento transaccional estaba acoplado al artefacto fisico diferencial no homologado. El flujo B ahora admite el payload transaccional existente con referencia unica y conserva intacto `DIFFERENTIAL_GENERATOR_NOT_HOMOLOGATED` para el flujo A.

Evidencia LIVE local controlada:

- CorrelationId: `JOB5C-LIVE-DIFF-RESP-001-20260820-009`.
- Respuesta: `FED44A63-ACD2-4218-8DC2-4F27308EDDA8`.
- Transaccion original: `2006`, `Prenotification`, monto `0.00`, estado final `Certified`.
- Correlacion persistida: `Matched`.
- Respuesta persistida: `Notificada`.
- Intentos persistidos: `1`; intento `1` en estado `Exitosa`.
- Request/response persistidos: longitudes sanitizadas `160` y `60`; no se registro XML completo en esta evidencia.
- Auditoria/traza posterior al replay: `3` auditorias de respuesta (incluye recepcion duplicada), `2` eventos transaccionales, `1` mapping trace y `7` entradas campo-a-campo.
- Log WCF: `Trama_ACH_20260820.log`, `541` bytes; una ocurrencia de `RegistrarRespuestaTransaccion` y una de cada uno de sus siete parametros.
- Replay del evento: `duplicada=true`, `DuplicateReceiptCount=1`, respuesta original conservada en `Notificada` e intento unico.
- Replay de la notificacion: `yaProcesada=true`, intento `1/Exitosa` conservado y log WCF sin crecimiento (`541` bytes).
- Imagen API final: `sha256:919373e955cb4d491a9f0cb7d01905454db442a01f896fcb16dde45d3c5c0d3e`, estado `healthy`; replay/UI verde despues del redeploy y log WCF aun en `541` bytes.
- No se crearon movimientos monetarios ni se afectaron saldos.

Contrato verificado:

- WSDL local: `http://localhost:7083/WSAxonRespuestaTransacciones.svc?wsdl`, HTTP 200.
- SOAP Action: `http://tempuri.org/IWSAxonRespuestaTransacciones/RegistrarRespuestaTransaccion`.
- Parametros: `idCanal`, `nombreCanal`, `idTransaccion`, `idEstado`, `causal`, `idTransaccionAxon`, `descripcionCausal`.
- Endpoint consumido por Docker: configuracion existente `host.docker.internal:7083` con `HostHeader=localhost:7083`; no se agrego hardcoding.

Comandos y resultados:

- `dotnet build ACHInterbank.sln -c Release`: exitoso, 0 warnings, 0 errores.
- Pruebas focalizadas DIFF: 119 aprobadas, 0 fallidas.
- `npx playwright test e2e/job5c-registrar-respuesta-live.spec.ts --project=chromium --reporter=list`: 1 aprobada; valida replay y UI sin redispatch.
- Suite backend completa: 2290 aprobadas, 15 omitidas y 10 fallidas; 8 requieren variables multi-DB no configuradas, 1 timeout OpenAPI paso al repetirse aisladamente y 1 fallo CENIT preexistente fuera de alcance. No hubo regresion directa DIFF.

Siguiente paso exacto: ninguno para `DIFF-RESP-001`. La homologacion/generacion fisica diferencial permanece separada y no fue modificada.
