# Inventario contra el ZIP base

Base: `ACHInterbank_CIERRE_NACHAUPLOAD_SOAP_PRENOTIFICACIONES_NPT_2026-11-15.zip` (SHA-256 `3f99534d05603ba054faf90f782f6950bbd2a461121a1611c09ffbf24161a448`).

Las modificaciones adicionales de memoria, plan y documentos son parte de esta entrega. Se conserva toda la documentación normativa y el árbol anterior.

## Añadidos (antes de documentación final)

- `ACHInterbank_ajustado/docs/cierre-focal-npt-2026-11-15/evidencias/VERIFICACION_FOCAL_FUENTE_MODELO.json`
- `ACHInterbank_ajustado/docs/cierre-focal-npt-2026-11-15/evidencias/achcol_nacha_transaction_counts_static_check.log`
- `ACHInterbank_ajustado/docs/cierre-focal-npt-2026-11-15/evidencias/aud_e2e_final_static_and_sqlite_check.log`
- `ACHInterbank_ajustado/docs/cierre-focal-npt-2026-11-15/evidencias/dotnet-attempts.txt`
- `ACHInterbank_ajustado/docs/cierre-focal-npt-2026-11-15/evidencias/e2e01_e2e02_source_static_check.log`
- `ACHInterbank_ajustado/docs/cierre-focal-npt-2026-11-15/evidencias/h01_h02_static_sqlite_check.log`
- `ACHInterbank_ajustado/docs/cierre-focal-npt-2026-11-15/evidencias/npt_focal_guard.log`
- `ACHInterbank_ajustado/docs/cierre-focal-npt-2026-11-15/evidencias/npt_static_guard.log`
- `ACHInterbank_ajustado/docs/cierre-focal-npt-2026-11-15/evidencias/res_h02_soap_preflight_sqlite_check.log`
- `ACHInterbank_ajustado/docs/cierre-focal-npt-2026-11-15/evidencias/status.tsv`
- `ACHInterbank_ajustado/scripts/test/npt_focal_sqlite_source_guard.py`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AxonNotificationRecoveryHostedService.cs`
- `ACHInterbank_ajustado/tests/Cfa.ACHInterbank.Tests/NptFocalAxonFencingTests.cs`
- `ACHInterbank_ajustado/tests/Cfa.ACHInterbank.Tests/NptFocalCfaFencingTests.cs`
- `ACHInterbank_ajustado/tests/Cfa.ACHInterbank.Tests/NptFocalProcTransaccionesParserTests.cs`

## Modificados (antes de documentación final)

- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Application/ACH/Responses/Notification/Interfaces/IAxonNotificationClaimStore.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Application/ACH/Responses/Notification/Services/NotificarRespuestaAchUseCase.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AxonNotificationClaimStore.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/DifferentialPrenotificationResponseProcessor.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostProcessingPipeline.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcTransaccionesResponseParser.cs`
- `ACHInterbank_ajustado/src/Cfa.ACHInterbank.Persistence/DependencyInjectionService.cs`
- `ACHInterbank_ajustado/tests/Cfa.ACHInterbank.Tests/DifferentialPrenotificationResponseProcessorTests.cs`
- `ACHInterbank_ajustado/tests/Cfa.ACHInterbank.Tests/NotificarRespuestaAchUseCaseTests.cs`

## Eliminados

- Ninguno
