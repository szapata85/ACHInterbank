# Inventario exacto NPT_SOAP_XSD_FINAL_01

Comparación de la raíz `ACHInterbank_ajustado/` contra el ZIP base.

- Añadidos: **30**
- Modificados: **20**
- Eliminados: **0**
- Fuera de esa raíz: `LEER_PRIMERO.md` (actualizado).

## Añadidos

- `LEER_PRIMERO.md`
- `docs/npt-soap-xsd-final-01/INFORME_NPT_SOAP_XSD_FINAL_01.md`
- `docs/npt-soap-xsd-final-01/INVENTARIO_CAMBIOS.md`
- `docs/npt-soap-xsd-final-01/MATRIZ_WSDL_XSD_IMPLEMENTACION.md`
- `docs/npt-soap-xsd-final-01/PLAN_IMPLEMENTACION.md`
- `docs/npt-soap-xsd-final-01/evidencias/achcol_nacha_transaction_counts_static_check.log`
- `docs/npt-soap-xsd-final-01/evidencias/aud_e2e_final_static_and_sqlite_check.log`
- `docs/npt-soap-xsd-final-01/evidencias/cenit_f04_static_check.log`
- `docs/npt-soap-xsd-final-01/evidencias/dotnet_--info.log`
- `docs/npt-soap-xsd-final-01/evidencias/dotnet_build.log`
- `docs/npt-soap-xsd-final-01/evidencias/dotnet_restore.log`
- `docs/npt-soap-xsd-final-01/evidencias/dotnet_test.log`
- `docs/npt-soap-xsd-final-01/evidencias/npt_cfa_late_static_sqlite_check.log`
- `docs/npt-soap-xsd-final-01/evidencias/npt_focal_sqlite_source_guard.log`
- `docs/npt-soap-xsd-final-01/evidencias/npt_pre_xsd_final_verify.log`
- `docs/npt-soap-xsd-final-01/evidencias/npt_static_guard.log`
- `docs/npt-soap-xsd-final-01/evidencias/offline_xmlschema_results.json`
- `docs/npt-soap-xsd-final-01/evidencias/res_h02_soap_preflight_sqlite_check.log`
- `docs/npt-soap-xsd-final-01/evidencias/source_guard_results.json`
- `docs/npt-soap-xsd-final-01/verify_source_guards.py`
- `docs/npt-soap-xsd-final-01/verify_wsdl_xsd_offline.py`
- `src/Cfa.ACHInterbank.Application/Integrations/Contracts/OfficialSoapContracts.cs`
- `src/Cfa.ACHInterbank.Application/Integrations/Contracts/OfficialSoapSchemas/WSAxonRespuestaTransacciones/WSAxonRespuestaTransacciones.wsdl.xml`
- `src/Cfa.ACHInterbank.Application/Integrations/Contracts/OfficialSoapSchemas/WSAxonRespuestaTransacciones/xsd0.xml`
- `src/Cfa.ACHInterbank.Application/Integrations/Contracts/OfficialSoapSchemas/WSAxonRespuestaTransacciones/xsd1.xml`
- `src/Cfa.ACHInterbank.Application/Integrations/Contracts/OfficialSoapSchemas/WSAxonRespuestaTransacciones/xsd2.xml`
- `src/Cfa.ACHInterbank.Application/Integrations/Contracts/OfficialSoapSchemas/WSCFAACH/WSCFAACH.wsdl.xml`
- `src/Cfa.ACHInterbank.Application/Integrations/Contracts/OfficialSoapSchemas/WSCFAACH/xsd0.xml`
- `src/Cfa.ACHInterbank.Application/Integrations/Contracts/OfficialSoapSchemas/WSCFAACH/xsd1.xml`
- `tests/Cfa.ACHInterbank.Tests/OfficialSoapXsdContractTests.cs`

## Modificados

- `MEMORY_STATE.json`
- `MEMORY_STATE.md`
- `src/Cfa.ACHInterbank.Application/Cfa.ACHInterbank.Application.csproj`
- `src/Cfa.ACHInterbank.External/Connections/RespuestaTransaccionesAch/RegistrarRespuestaAchSoapResponseParser.cs`
- `src/Cfa.ACHInterbank.External/Connections/RespuestaTransaccionesAch/RespuestaTransaccionesAchGateway.cs`
- `src/Cfa.ACHInterbank.External/Connections/WsAxonRespuestaTransaccionesSoapClient.cs`
- `src/Cfa.ACHInterbank.External/Connections/WscfaachSoapClient.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColProcTransaccionesDryRunContractCheck.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostProcessingPipeline.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcTransaccionesRequestMapper.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcTransaccionesResponseParser.cs`
- `tests/Cfa.ACHInterbank.Tests/AchColProcTransaccionesContractReadinessF53Tests.cs`
- `tests/Cfa.ACHInterbank.Tests/CfaSoapRequestCharacterizationTests.cs`
- `tests/Cfa.ACHInterbank.Tests/IncomingNachaPostProcessingOrchestratorTests.cs`
- `tests/Cfa.ACHInterbank.Tests/IntegrationContractDrivenSoapBodyWriterTests.cs`
- `tests/Cfa.ACHInterbank.Tests/NachaDesagregadoIntegrationMappingTests.cs`
- `tests/Cfa.ACHInterbank.Tests/NptFocalProcTransaccionesParserTests.cs`
- `tests/Cfa.ACHInterbank.Tests/ProcTransaccionesResponseParserTests.cs`
- `tests/Cfa.ACHInterbank.Tests/WsAxonRespuestaTransaccionesSoapClientCharacterizationTests.cs`
- `tests/Cfa.ACHInterbank.Tests/WscfaachSoapClientCharacterizationTests.cs`

## Eliminados

Ninguno.
