# Inventario completo de cambios NPT-01…NPT-14

Comparación **byte a byte** del árbol completo contra el ZIP base, sin eliminar archivos preexistentes. El manifiesto incluye sus dos propios archivos por nombre (los hashes propios no se calculan por autorreferencia).

## Añadidos: 20

- `ACHInterbank_ajustado/docs/cierre-npt-2026-11-15/AUTOVERIFICACION_ADVERSARIAL.md`
- `ACHInterbank_ajustado/docs/cierre-npt-2026-11-15/COMANDOS_REPRODUCIBLES.md`
- `ACHInterbank_ajustado/docs/cierre-npt-2026-11-15/DEPENDENCIAS_EXTERNAS_Y_NO_GO.md`
- `ACHInterbank_ajustado/docs/cierre-npt-2026-11-15/INFORME_IMPLEMENTACION_NPT_01_14.md`
- `ACHInterbank_ajustado/docs/cierre-npt-2026-11-15/MANIFIESTO_ARCHIVOS.json`
- `ACHInterbank_ajustado/docs/cierre-npt-2026-11-15/MANIFIESTO_ARCHIVOS.md`
- `ACHInterbank_ajustado/docs/cierre-npt-2026-11-15/MATRIZ_38_ESCENARIOS.md`
- `ACHInterbank_ajustado/docs/cierre-npt-2026-11-15/evidencias/AMBIENTE_SDK_Y_BD.txt`
- `ACHInterbank_ajustado/docs/cierre-npt-2026-11-15/evidencias/GRAFO_SOLUCION.json`
- `ACHInterbank_ajustado/docs/cierre-npt-2026-11-15/evidencias/REGRESION_ESTATICA_HISTORICA.log`
- `ACHInterbank_ajustado/docs/cierre-npt-2026-11-15/evidencias/VERIFICACION_NPT_ESTATICA.json`
- `ACHInterbank_ajustado/docs/cierre-npt-2026-11-15/evidencias/VERIFICACION_NPT_ESTATICA.log`
- `ACHInterbank_ajustado/docs/cierre-npt-2026-11-15/evidencias/WSDL_SOAPACTIONS_XSD_AUSENTES.json`
- `ACHInterbank_ajustado/scripts/test/npt_static_guard.py`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Application/ACH/Responses/Notification/Interfaces/IAxonNotificationClaimStore.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Application/ACH/Responses/Processing/Interfaces/IAchResponseTransactionalRunner.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/20261115210000_Npt03Npt04NachaDispatchClaim.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchResponseTransactionalRunner.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AxonNotificationClaimStore.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres/20261115210000_Npt03Npt04NachaDispatchClaim.cs`

## Modificados: 26

- `ACHInterbank_ajustado/MEMORY_STATE.json`
- `ACHInterbank_ajustado/MEMORY_STATE.md`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Application/ACH/Responses/Notification/Services/NotificarRespuestaAchUseCase.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Application/ACH/Responses/Processing/Services/ProcesarRespuestaAchUseCase.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Domain/Models/ACH/AchResponseNotificationAttempt.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Domain/Models/ACH/Enums/AchResponseNotificationStatus.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Domain/Models/ACH/IncomingNachaDispatchModels.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer/DataBase/Migrations/SqlServer/AchDbContextModelSnapshot.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/DifferentialPrenotificationResponseProcessor.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaIngestionAppService.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostProcessingPipeline.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/NachaParserService.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcTransaccionesResponseParser.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Persistence/Configuration/AchResponseNotificationAttemptConfiguration.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Persistence/Configuration/IncomingNachaDispatchQueueConfiguration.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Persistence/DataBase/Migrations/Postgres/AchDbContextModelSnapshot.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Persistence/DependencyInjectionService.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingReadinessService.cs`
- `ACHInterbank_ajustado/tests/Cfa.ACHInterbank.Tests/IncomingNachaPostProcessingOrchestratorTests.cs`
- `ACHInterbank_ajustado/tests/Cfa.ACHInterbank.Tests/NotificarRespuestaAchUseCaseTests.cs`
- `ACHInterbank_ajustado/tests/Cfa.ACHInterbank.Tests/ProcTransaccionesResponseParserTests.cs`
- `ACHInterbank_ajustado/tests/Cfa.ACHInterbank.Tests/WscfaachSoapClientCharacterizationTests.cs`
- `LEER_PRIMERO.md`
- `PLAN_IMPLEMENTACION_13_CASOS/MEMORY_STATE.json`
- `PLAN_IMPLEMENTACION_13_CASOS/MEMORY_STATE.md`
- `PLAN_IMPLEMENTACION_13_CASOS/PLAN_IMPLEMENTACION_13_CASOS.md`

## Eliminados: 0

Ninguno.

