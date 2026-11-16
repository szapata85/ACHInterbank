# Matriz reproducible de 38 escenarios — cierre NPT

**Estado real de ejecución:** 0/38 ejecutados en .NET, SQL Server, PostgreSQL o SOAP integrado desde este entorno. Los nombres de test son **código fuente añadido o existente, no un resultado de ejecución**. En el script `scripts/test/npt_static_guard.py` sólo se ensayan invariantes de fuente y modelo SQLite Python; no equivalen a xUnit ni integración banco.

| ID | Escenario original | Esperado original | Evidencia preparada | Estado |
|---|---|---|---|---|
| UP-01 | Archivo NACHA-M válido 106 posiciones PPD, crédito | Ingesta, cabeceras, detalle, adendas y conteo exacto | Suite existente / pendiente de ampliar o ejecutar | **Pendiente** |
| UP-02 | Archivo incompleto o control de lote incorrecto | Rechazo/errores conforme a catálogo, sin despacho | Suite existente / pendiente de ampliar o ejecutar | **Pendiente** |
| UP-03 | Archivo duplicado mismo hash, diferente nombre | Una sola ingesta canónica, sin segundo dispatch | Suite existente / pendiente de ampliar o ejecutar | **Pendiente** |
| UP-04 | Mismo nombre con contenido diferente | Incidencia y trazabilidad, sin sobrescritura | Suite existente / pendiente de ampliar o ejecutar | **Pendiente** |
| UP-05 | Reproceso sin `ForceReprocess`/padre | No reprocesa | Suite existente / pendiente de ampliar o ejecutar | **Pendiente** |
| UP-06 | Reproceso con padre incorrecto | Rechazo controlado | Suite existente / pendiente de ampliar o ejecutar | **Pendiente** |
| UP-07 | Sobre `.env` válido/inválido o manipulado | Validación criptográfica, sin plaintext en disco | Suite existente / pendiente de ampliar o ejecutar | **Pendiente** |
| UP-08 | Archivo mayor de 10 MB | Rechazo temprano | Suite existente / pendiente de ampliar o ejecutar | **Pendiente** |
| UP-09 | Códigos de prenotificación y adendas obligatorias PPD/CCD/CTX | Validación normativa por cámara/servicio | Suite existente / pendiente de ampliar o ejecutar | **Pendiente** |
| UP-10 | Archivo diferencial físico sin perfil homologado | Rechazo seguro antes del parser, ningún SOAP | Suite existente / pendiente de ampliar o ejecutar | **Pendiente** |
| UP-11 | ACHCOL file-based válido | Se mantiene `Blocked`/preflight hasta autorización F5 | Suite existente / pendiente de ampliar o ejecutar | **Pendiente** |
| UP-12 | Conteo físico 3 T6, 2 aceptados, 1 inválido | Total=3, Valid=2, Invalid=1 | NachaParserService + IncomingNachaIngestionAppService: contadores T6 preexistentes, runtime pendiente | **Pendiente** |
| DP-01 | cycle-bound elegible y ventana válida | Un claim durable, mapping y llamada SOAP | Suite existente / pendiente de ampliar o ejecutar | **Pendiente** |
| DP-02 | cycle-bound fuera de ventana | No se invoca SOAP | Suite existente / pendiente de ampliar o ejecutar | **Pendiente** |
| DP-03 | Dos procesos Quartz reclaman la misma fila | Sólo un envío físico | Suite existente / pendiente de ampliar o ejecutar | **Pendiente** |
| DP-04 | Caída después del claim y antes del HTTP | Estado recuperable sin retransmisión ciega | Suite existente / pendiente de ampliar o ejecutar | **Pendiente** |
| DP-05 | Caída después de HTTP y antes de persistir respuesta | `OutcomeUnknown`, exige conciliación | Suite existente / pendiente de ampliar o ejecutar | **Pendiente** |
| DP-06 | Timeout de transporte y respuesta `RTAACH=TIMEOUT` | No reenviar sin certeza de no aplicación | ProcTransaccionesResponseParserTests.Parse_NeverSchedulesReplay_ForAmbiguousCoreCode; WscfaachSoapClientCharacterizationTests.Npt06_HttpGatewayTimeout_DoesNotDuplicatePhysicalPost | **Pendiente** |
| DP-07 | Fallo SOAP Fault 500, respuesta parcial y código desconocido | Clasificación correcta; no falso éxito | ProcTransaccionesResponseParserTests.Parse_Quarantines_WhenSoapFault | **Pendiente** |
| DP-08 | XML de otra operación con `RTAACH=00` | Rechazo del parser por operación incorrecta | ProcTransaccionesResponseParserTests.Parse_RejectsFalseSuccessFromWrongEnvelopeOrOperation | **Pendiente** |
| DP-09 | Mapping mutado entre readiness y envío | Aborto por snapshot | Suite existente / pendiente de ampliar o ejecutar | **Pendiente** |
| DP-10 | `NCTAORIG` con fuente de cuenta receptora | Live bloqueado y evidencia del guard | Suite existente / pendiente de ampliar o ejecutar | **Pendiente** |
| DP-11 | `NCTAORIG` desde fuente oficial homologada | XML validado contra XSD y CFA, sin bloqueo incorrecto | Suite existente / pendiente de ampliar o ejecutar | **Pendiente** |
| DP-12 | Respuesta SOAP éxito y reinicio del job | No se duplica estado ni efecto externo | Suite existente / pendiente de ampliar o ejecutar | **Pendiente** |
| PR-01 | Respuesta `Prenota` válida por API y referencia única | Estado coherente, sin movimiento monetario | Suite existente / pendiente de ampliar o ejecutar | **Pendiente** |
| PR-02 | Respuesta con cámara distinta | Error/ManualReview, no `Matched` validado | Suite existente / pendiente de ampliar o ejecutar | **Pendiente** |
| PR-03 | Respuesta con cuenta o identificación distinta | No transición, no falso `Matched` | Suite existente / pendiente de ampliar o ejecutar | **Pendiente** |
| PR-04 | Dos archivos con igual `SequenceNumber` y distintas cámaras/fechas | Correlación por clave oficial; error seguro si ambigüedad real | Suite existente / pendiente de ampliar o ejecutar | **Pendiente** |
| PR-05 | Duplicado exacto de respuesta | Una respuesta/una transición, conteo de duplicado | Suite existente / pendiente de ampliar o ejecutar | **Pendiente** |
| PR-06 | Dos respuestas simultáneas diferentes para una prenotificación | Sólo un estado terminal, conflicto controlado | Suite existente / pendiente de ampliar o ejecutar | **Pendiente** |
| PR-07 | Fallo de commit al guardar `AchResponse` | Estado/evento/trace revertidos transaccionalmente | Suite existente / pendiente de ampliar o ejecutar | **Pendiente** |
| AX-01 | Intento de notificación exitoso | Un solo `RegistrarRespuestaTransaccion` y estado Notificada | Suite existente / pendiente de ampliar o ejecutar | **Pendiente** |
| AX-02 | Dos llamadas concurrentes al mismo intento | Sólo un gateway invocado | NotificarRespuestaAchUseCaseTests.Npt08_TwoConcurrentNotifications_WithAtomicClaimFake_CallGatewayOnce | **Pendiente** |
| AX-03 | Cancelación HTTP justo después de iniciar SOAP | Estado incierto durable, sin replay | NotificarRespuestaAchUseCaseTests.Npt09_CancellationAfterAxonTransport_UsesIndependentTokenToQuarantine | **Pendiente** |
| AX-04 | Crash tras respuesta Axon antes de commit | Reconciliar, no repetir sin confirmar | Suite existente / pendiente de ampliar o ejecutar | **Pendiente** |
| AX-05 | WSDL sin XSD o mapping incompleto | Bloqueo seguro antes de red | NotificarRespuestaAchUseCaseTests.Npt08_NoClaimStore_MustFailClosedBeforeSoap | **Pendiente** |
| SEC-01 | SOAP Live sin allowlist/autorización/certificado requerido | Fallo cerrado, ningún envío | Suite existente / pendiente de ampliar o ejecutar | **Pendiente** |
| SEC-02 | Logs con cuenta/nombre/identificación | Accesos, cifrado, retención y sanitización según política | Suite existente / pendiente de ampliar o ejecutar | **Pendiente** |

Los escenarios DP-03, DP-04, DP-05, PR-06/07 y AX-02/04 requieren **dos conexiones independientes a SQL Server y PostgreSQL**, inyección de crash y contadores HTTP físicos instrumentados; un fake CAS en memoria no certifica el comportamiento multinstancia. DP-11 y AX-05 requieren XSD/vectores CFA oficiales; UP-10 requiere perfiles físicos diferenciales. UAT excluida.
