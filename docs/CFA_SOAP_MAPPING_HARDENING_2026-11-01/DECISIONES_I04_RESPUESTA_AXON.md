# I04 — RegistrarRespuestaTransaccion: campos, acuse y persistencia

**Programa:** SOAP-MAP-SAFE-001. **Fecha:** 2026-11-01. **Evidencia:** revisión del código fuente del ZIP I03; cambio mínimo en I04. No es homologación bancaria ni UAT.

## 1. Flujo confirmado

`ProcesarRespuestaAchUseCase` crea una `AchResponse` y un `AchResponseNotificationAttempt` al aplicar una homologación determinística con permiso de notificar. `NotificarRespuestaAchUseCase` carga el intento, el `RegistrarRespuestaAchCommandMapper` copia los datos, el gateway envía el request y el caso de uso guarda el acuse.

| Columna `AchResponseNotificationAttempts` | Propiedad del comando | Elemento SOAP de Axon | I04 |
| --- | --- | --- | --- |
| `IdCanal` | `IdCanal` | `idCanal` | Conservado; validación `>0` |
| `NombreCanal` | `NombreCanal` | `nombreCanal` | Conservado; no vacío |
| `IdTransaccion` | `IdTransaccion` | `idTransaccion` | Conservado; no vacío |
| `IdEstado` | `IdEstado` | `idEstado` | Conservado; validación `>0` |
| `Causal` | `Causal` | `causal` | Conservado, opcional |
| `IdTransaccionServicioExterno` | `IdTransaccionServicioExterno` | `idTransaccionAxon` | Conservado; validación `>0` |
| `DescripcionCausal` | `DescripcionCausal` | `descripcionCausal` | Conservado, opcional |

No se cambia el seed ni se activa `UseContractDrivenRequest`; no se modifica el request mapper físico ni el contrato WSDL.

## 2. Acuse SOAP y persistencia

| Resultado / incidencia | Estado del intento | Estado de `AchResponses` | Comportamiento |
| --- | --- | --- | --- |
| `existeError=false` | `Exitosa` | `Notificada` | Persiste `ExisteError`, `CodigoError`, `DescripcionError` y JSON de los tres campos, sin reenvío de ese intento |
| `existeError=true` | `ErrorFuncional` | `ErrorFuncional` | Persistencia de los tres campos, sin convertirlo en éxito |
| Parámetro obligatorio del comando inválido | `ErrorFuncional` | `ErrorFuncional` | **Nuevo:** `REGISTRAR_RESPUESTA_COMMAND_INVALID`; no llama al gateway, no anota fecha de envío |
| Readiness o trace requerido no satisfecho | `ErrorFuncional` | `ErrorFuncional` | Comportamiento previo, sin envío |
| Excepción conocida antes de invocar el gateway (p. ej. trace store) | `ErrorTecnico` | `PendienteReintento` | No se llamó al banco; conserva clasificación previa |
| Excepción después de entrar al gateway (incluye XML incompleto, SOAP fault, HTTP/timeout) | `RequiereRevisionManual` | `RequiereRevisionManual` | **Nuevo:** resultado remoto incierto, `ResultadoDesconocido`; no reintento ciego del intento |
| Intento en estado terminal (incluidos `Exitosa`, `ErrorFuncional`, `RequiereRevisionManual`) | Sin cambio | Sin cambio | Rechaza una segunda llamada del mismo `NotificationAttemptId` |

**Advertencia:** la clasificación conservadora posgateway no prueba entrega; una excepción también puede ocurrir antes de transmitir físicamente en el gateway. Sin evidencia de no entrega, se elige conciliación manual antes de reintento. También faltan pruebas ejecutables de reclamación/concurrencia y de fallos en commit tras el acuse, que se difieren a I06; no se autoriza Live por este incremento. Cancelación explícita del caller conserva el manejo actual (propaga la excepción), por lo que recuperación tras apagado abrupto queda como deuda de continuidad.

## 3. Cambio real de código

- `src/Cfa.ACHInterbank.Application/ACH/Responses/Notification/Services/NotificarRespuestaAchUseCase.cs`:
  - Reutiliza `RegistrarRespuestaAchCommandValidator` antes de iniciar SOAP.
  - Marca `gatewayInvoked` justo antes de llamar a `IRespuestaTransaccionesAchGateway` y clasifica cualquier excepción posterior como resultado desconocido, sin reenviar automáticamente el intento.
- `tests/Cfa.ACHInterbank.Tests/NotificarRespuestaAchUseCaseTests.cs`:
  - Verifica los siete campos, persistencia de los tres elementos de acuse, rechazo previo a gateway, clasificación distinta entre traza previa y gateway incierto, idempotencia de resultado definitivo.

**Sin:** nuevas entidades, colas, tablas, migraciones, pantallas, cambios NACHA-M, mutación de seed, activación de contract-driven, UAT, ni Live.

## 4. Evidencia y pendientes

- `python scripts/validate-solution-projects.py` → `SOLUTION GRAPH: OK` (7/7). Revisión estática de cambios y conteo de tests.
- SDK `dotnet` no está disponible en este entorno: compilación y pruebas `NOT_RUN_ENVIRONMENT`, nunca `PASS`.
- Con SDK en I06: `dotnet build ACHInterbank.sln -c Release` y `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~NotificarRespuestaAchUseCaseTests|FullyQualifiedName~RespuestaTransaccionesAchGatewayTests|FullyQualifiedName~RegistrarRespuestaAchContractsTests"`.
- Probar concurrencia/lease, fallos en persistencia tras respuesta, recuperación del proceso ante cancelación y escenarios reales de SOAP cuando exista entorno aprobado. UAT solamente I07.
- Próximo incremento: **I05**, paridad entre paths existentes sin cutover.
