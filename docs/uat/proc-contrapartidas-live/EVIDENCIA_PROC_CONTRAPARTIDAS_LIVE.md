# Evidencia Proc_Contrapartidas LIVE local

Fecha local: 2026-07-16 (America/Bogota)

## Entorno y topología

- SPA host: `http://localhost:743`, configuración Angular `local-e3`.
- API host: `http://localhost:843`, Development, SQL Server local.
- WCF local: `http://localhost:7083/WSCFAACH.svc`.
- SQL Server: contenedor local controlado.
- `ProcContrapartidas=Live`.
- `ProcTransacciones=Disabled` durante la ejecución.
- Retry HTTP: 0; una sola transmisión posible.
- Jobs automáticos `AchContrapartidasByCycle` e `IncomingNachaPostProcessing`: deshabilitados durante y después de la prueba para evitar competencia o replay.
- No se usó `host.docker.internal`, endpoint externo, mock SOAP ni DryRun.

## Datos

Se utilizaron exclusivamente identificaciones, cuentas, referencias, nombres, addenda y valor sintéticos generados por el spec. No se copiaron datos NACHA-M reales. Este documento no conserva los valores individuales.

La transacción fue débito originado por CFA, cámara ACH Colombia (`ClearingHouseId=1`), con `collectorId`, `receiverCustomerCode` y `serviceDescription` sintéticos.

## Precondiciones

- Build Release verde.
- Suite offline verde.
- Migraciones SQL Server/PostgreSQL y pruebas de concurrencia verdes.
- API, SPA, SQL Server y WCF healthy.
- CENIT bloqueado explícitamente por el spec antes del despacho.
- Despacho UAT dirigido por `transactionId`; no consume otros pendientes del ciclo.

## Ejecución

Playwright autenticó por la SPA, abrió `/transactions/create`, creó prenotificación y transacción sintéticas, activó el tercero sintético, guardó addenda, verificó clasificación y llamó el endpoint UAT dirigido.

Resultado real:

- Método: `Proc_Contrapartidas`.
- Modo persistido: `Live`.
- Endpoint autorizado: sí.
- Resultado técnico: `Succeeded`.
- Código funcional: `R96`.
- Intentos del item exitoso: 1.
- Retry elegible: no.
- Estado final: `ReportedToContrapartida`.
- Duración persistida: 2.401 ms.
- Request SHA-256: `B11656219051092B2815C72E0DE4F905D863087BF09B744814FF1E2D6AAA1F6A`.
- Response SHA-256: `1467142895ED6EA1DF2B8215E49DAA7ECDAD5D54A9E413F45E2AFC14ED9A1552`.

El request persistido contiene cero ocurrencias de `METODO`, `Proc_Transacciones`, `RegistrarRespuestaTransaccion` y `PLValidarUsuarioBV`.

## Incidente pre-transporte conservado

Una ejecución previa creó un intento de auditoría pero no realizó transporte: `MaxRetryAttempts=0` era rechazado por Polly con `ValidationException` antes de crear/enviar el request HTTP. No apareció log WCF. Se corrigió el pipeline para omitir la política de retry cuando el valor es cero, se añadió prueba y el item sintético quedó terminal para impedir replay. La auditoría no fue eliminada.

Conteo total del arnés: 2 intentos persistidos = 1 fallo pre-transporte + 1 transporte real exitoso. Intentos con método prohibido: 0.

## Evidencia WCF

- Archivo local: `Trama_ACH_20260716.log`.
- Tamaño observado: 709 bytes.
- Timestamp: `2026-07-16T17:45:21-05:00`.
- SHA-256: `09347951A253D16044587176E535F01F498EB95DE6D21E8865B5F9707CC30610`.
- `Proc_Contrapartidas`: presente en la única entrada.
- `Proc_Transacciones`: 0.
- `RegistrarRespuestaTransaccion`: 0.
- `PLValidarUsuarioBV`: 0.

El logger legacy agrega un tag interno `METODO` a su representación de trazabilidad. No pertenece al envelope outbound: la copia exacta persistida antes del transporte, validada por Playwright y por consulta de base, tiene cero tags `METODO`. Esta distinción evita confundir instrumentación interna del WCF con el contrato enviado por ACHInterbank.

## Persistencia y reinicio

Tras reiniciar la API, la fila continuó consultable, con respuesta no vacía, estado `Succeeded` y los mismos hashes de request/response. Se persistieron método, endpoint lógico, modo, duración, intento, código técnico/funcional, descripción, banderas de resultado, correlación y timestamps.

## Playwright y artefactos anonimizados

- Resultado: 1 passed, 0 failed, duración total 1,3 minutos.
- Captura enmascarada SHA-256: `FB92591A8F5010AEA6F3EB4D4C8E99807B3E7A91547B5A2EFBFA338E09F484A9`.
- Request sanitizado SHA-256: `0F3811508072630F7D4CB53F71DF2FB7C823AE8D1739B06BB71C0A40CF73A2BF`.
- Response sanitizado SHA-256: `9BBF75A8B018ED94645F187AD4D5257463D60A2E0E826CD4D694E4EECDF2E660`.

No se versionó el log WCF ni payload crudo.

