# Informe de implementación NPT-01 a NPT-14 — 2026-11-15

**Base integral:** `ACHInterbank_PROYECTO_COMPLETO_RES_H02_01_RES_H02_02_SOAP_LIVE_2026-11-15.zip`; SHA-256 `f28654ce10542c9acaec7d42d454a9ddd0c6e19744e8046c26141b620b42eaa9`.

**Decisión:** NO-GO SOAP Live y NO-GO productivo. La implementación es un **incremento de fuente no certificado**: falta el SDK .NET, ejecución xUnit, pruebas SQL Server y PostgreSQL, ensayos SOAP instrumentados de carrera/crash y los contratos externos indicados. No se ejecutó UAT. Los estados «corregido en fuente pendiente de prueba» **no son cierres técnicos certificados**. No se habilitó Live ni se retiraron bloqueos F5/diferenciales/NCTAORIG.

**Separación de flujos:** A: respuesta inmediata `Proc_Transacciones` (monetaria potencial); B: respuesta diferencial de prenotificación no monetaria con notificación `RegistrarRespuestaTransaccion` a Axon. Un resultado del flujo A no homologa el B, ni la carga de un archivo físico diferencial.

## Estado por hallazgo, método y evidencia

| Hallazgo | Estado | Código y método | Evidencia/acción concreta | Riesgo o dependencia residual |
|---|---|---|---|---|
| **NPT-01** — F5 ACH Colombia file-based | Bloqueado por contrato externo | `IncomingNachaFileBasedPreparationStage.PrepareAsync; IncomingNachaDispatchPlanner` | F5 requiere mapping/origen, reglas de elegibilidad, ciclo o mecanismo propio, cuentas y certificación; se retiene Blocked y SOAP=0. | Contrato externo/homologación |
| **NPT-02** — Archivos diferenciales físicos | Bloqueado por contrato externo | `IncomingNachaIngestionAppService (bloqueo homologación); docs/job-5/JOB5_NACHA_M_DIFERENCIAL.md` | Falta XSD/perfil NACHA diferencial y vectores de cámara. No se conecta archivo físico al uso de respuesta por API. | Contrato externo/homologación |
| **NPT-03** — Claim monetario persistente | Corregido en fuente pendiente de prueba | `IncomingNachaPostProcessingPipeline.TryClaimAsync; IncomingNachaDispatchModels; IncomingNachaDispatchQueueConfiguration; migraciones SQL Server y PG` | ExecuteUpdate CAS estado+token+lease+recuento. Prueba simultánea multi-worker SQL Server/PostgreSQL pendiente. | Compilación/xUnit/integración de proveedores pendiente |
| **NPT-04** — Recuperación segura | Corregido en fuente pendiente de prueba | `IncomingNachaPostProcessingPipeline.RecoverExpiredClaimsAsync, MarkTransportIntentAsync; migraciones` | Dispatching vencido -> Blocked/RequiresReconciliation; NPT_PRETRANSPORT_REVIEW vs NPT_TRANSPORT_OUTCOME_UNKNOWN. No replay automático. | Compilación/xUnit/integración de proveedores pendiente |
| **NPT-05** — NCTAORIG fuente autorizada | Bloqueado por contrato externo | `IntegrationMappingReadinessService.ValidateProcTransaccionesNctaOrigRule; IncomingNachaExternalDispatchStage.EnsureLiveOriginAccountHomologated` | Readiness no permite cuenta receptora como origen; guard Live bloquea hasta disponer fuente CFA homologada. | Contrato externo/homologación |
| **NPT-06** — No reenvío de timeout/resultado incierto | Corregido en fuente pendiente de prueba | `ProcTransaccionesResponseParser.Parse; IncomingNachaExternalDispatchStage.InterpretAsync; IncomingNachaStateTransitionStage.ApplyDispatchOutcome` | IsRetryable=false; TIMEOUT/errores SOAP/resultado desconocido posterior al transporte -> manual, no RetryPending. | Compilación/xUnit/integración de proveedores pendiente |
| **NPT-07** — Atomicidad respuesta/prenotificación | Corregido en fuente pendiente de prueba | `ProcesarRespuestaAchUseCase.ExecuteAsync; AchResponseTransactionalRunner.RunAsync; IAchResponseTransactionalRunner` | Transacción relacional cubre transición/trace/response/idempotencia, excepción revierte. Falta test de rollback inyectado contra motores. | Compilación/xUnit/integración de proveedores pendiente; rollback real no verificado |
| **NPT-08** — Claim Axon | Corregido en fuente pendiente de prueba | `NotificarRespuestaAchUseCase.ExecuteAsync; AxonNotificationClaimStore.TryClaimAsync; AchResponseNotificationAttemptConfiguration; migraciones` | CAS pendiente->InFlight antes de llamada, token lease y fence; xUnit fake cuenta gateway, prueba SQL real pendiente. | Compilación/xUnit/integración de proveedores pendiente |
| **NPT-09** — Cancelación Axon incierta | Corregido en fuente pendiente de prueba | `NotificarRespuestaAchUseCase.ExecuteAsync; AxonNotificationClaimStore.MarkTransportIntentAsync` | Ante cancelación posterior a gateway, revisión manual persistida con CancellationToken.None. Crash tras enviar queda InFlight y recuperación cuarentena. | Compilación/xUnit/integración de proveedores pendiente |
| **NPT-10** — Parser SOAP estricto | Corregido en fuente pendiente de prueba | `ProcTransaccionesResponseParser.Parse; IncomingNachaExternalDispatchStage.ResolveTransportStatus` | SOAP 1.1/1.2 envelope/body y operación http://tempuri.org/Proc_TransaccionesResponse; Fault o RTAACH ausente/múltiple bloquea. Validación XSD pendiente. | Compilación/xUnit/integración de proveedores pendiente |
| **NPT-11** — Correlación por cámara | Corregido en fuente pendiente de prueba | `DifferentialPrenotificationResponseProcessor.ResolvePrenotificationMatchAsync, ResolvePrenotificationAsync` | Filtra cámara tanto entrada NACHA como transacción; si 2 candidatos misma cámara se conserva revisión manual. Falta fecha/archivo oficial. | Compilación/xUnit/integración de proveedores pendiente; falta referencia oficial de fecha/archivo |
| **NPT-12** — Matched tras verificar | Corregido en fuente pendiente de prueba | `DifferentialPrenotificationResponseProcessor.ProcessAsync; ProcesarRespuestaAchUseCase.ExecuteCoreAsync` | La asociación validada se asigna después de checks de cámara/cuenta/identidad + transición. Candidato inválido es manual. | Compilación/xUnit/integración de proveedores pendiente |
| **NPT-13** — Errores y advertencias | Corregido en fuente pendiente de prueba | `NachaParserService.ParseAsync; IncomingNachaIngestionAppService` | Failures se guarda sólo como ParserErrorsJson, ParserWarningsJson=[] y WarningCount=0; contadores físicos T6 no alterados. | Compilación/xUnit/integración de proveedores pendiente |
| **NPT-14** — WSDL/XSD | Bloqueado por contrato externo | `WSCFAACH.wsdl (1).xml, WSAxonRespuestaTransacciones.wsdl (1).xml, evidencias/WSDL.json` | Operaciones/SOAPAction verificadas, esquemas http://esparta ausentes; no certificar estructura/longitudes/semántica y no abrir Live. | Contrato externo/homologación |

## Pruebas y resultados reales

- **`dotnet restore`, `dotnet build`, `dotnet test`: no ejecutados** por falta del SDK. NO se presentan tests xUnit como pasados.
- **SQL Server y PostgreSQL: no ejecutados**, migraciones verificadas únicamente en fuente, no aplicadas a bases reales.
- **SOAP Live: no ejecutado**; test HTTP con `LocalSoapServer` incorporado para contar peticiones físicas, pero no corrido.
- **UAT: no ejecutado** por instrucción expresa.
- **Verificaciones Python estáticas/modelo SQLite:** resultado y hora en `evidencias/VERIFICACION_NPT_ESTATICA.log` después de correr el script; exclusivamente prueba de fuente y simulación de CAS, no ejecución C#.
- Matriz origen 38 escenarios replicada sin falsos aprobados en `MATRIZ_38_ESCENARIOS.md`.

## Prueba adversarial de dinero y transacciones

1. Dos Quartz pueden seleccionar la misma fila; el SQL `ExecuteUpdate` condicionado en token/estado/lease intenta hacer que sólo uno llegue a SOAP. La garantía real **no está demostrada** en SQL Server/PG sin DP-03.
2. Caída tras claim antes de red: se bloquea para revisión; caída después de `TransportStartedAtUtc`: se bloquea para conciliación. No hay reenvío automático posterior a vencimiento del lease. El lease no demuestra que el core no recibió dinero.
3. `TIMEOUT`, cancelación, SOAP Fault, parser inválido o código desconocido no generan `RetryPending` post-transporte.
4. `NCTAORIG` no usa la cuenta receptora como origen, ni se inventa cuenta. Readiness falla cerrado sin fuente homologada.
5. Atomicidad de prenotificaciones: `AchResponseTransactionalRunner` envuelve los `SaveChanges` de transición, trace y `AchResponse` bajo una transacción relacional. Falta inyectar excepción de commit real y verificar reversión en ambas bases.
6. Axon: claim SQL + token+lease antes del gateway, y cancelación incierta con persistencia no cancelable. Si el proceso muere dentro de la llamada, queda `InFlight` y luego cuarentena. Sólo reconciliación permite gestionar repetición.
7. Correlación de prenotificaciones: se acota cámara y falla ante duplicados indistinguibles; no se elige un registro por orden. Falta referencia oficial para desempatar archivos de misma cámara.
8. Parser verifica SOAP correcto y la respuesta sólo puede ser `BusinessSuccess` si SOAP y catálogo confirman. XSD ausente: el contrato de los campos sigue sin certificar.
9. F4.2/ACHCOL y diferencial físico siguen detenidos; nunca se convierten en cycle-bound para forzar el envío.
10. Migraciones cruzadas: archivos independientes para SQL Server y PostgreSQL, ADD nullable y backfill conservador; requieren despliegue en copia de base, backup, verificación de valores e índices antes de Live.

**Defectos de autoverificación subsanados durante el mismo incremento:** (a) conteo de advertencias separadas de errores; (b) plan posterior al claim puede fallar y quedar Dispatching: se bloquea y persiste; (c) no equiparar DryRun a éxito o rechazo bancario; (d) éxito de catálogo sin `parsed.IsSuccess` prohibido; (e) corrección del filtro de cámara en búsqueda del registro NACHA y la transacción; (f) migración en proyecto proveedor SQL Server verificada.

**Riesgos aún abiertos:** tests C# podrían fallar hasta que se ejecuten y adapten los fixtures legados; incertidumbre de crash después de envío frente a conciliación; coordinación con cierre de ciclo H02; datos SOAP existentes guardados por el mecanismo histórico exigen controles operativos de cifrado/retención; no es una certificación de seguridad ni banco. **Nada de esto autoriza Live.**
