# I03 — Semántica `Proc_Transacciones` / correspondencia entrada NACHA → SOAP

**Programa:** SOAP-MAP-SAFE-001  
**Fecha:** 2026-11-01  
**Base integrada:** I01 + I02. **No es una homologación bancaria ni validación de runtime**.

## Resultado campo por campo

| Campo SOAP | Fuente publicada (seed existente) | Decisión en I03 | Estado |
|---|---|---|---|
| `NCTAORIG` | `EntryDetails.AccountNumber` | Esa columna es cuenta **receptora** NACHA (tipo 6); no acredita cuenta del originador. En el preparado local `SourceAccountNumber` también se copia desde `EntryDetail.AccountNumber` y tampoco constituye prueba de cuenta origen. **No remapear a esa columna ni inventar alternativa.** El dispatch bloquea **Live** cuando el SOAP lleva un `NCTAORIG` no vacío, hasta homologar una fuente independiente y modificar deliberadamente el guard. DryRun sigue generando evidencia de diagnóstico. | `BLOCKED_FUNCTIONAL` |
| `NCTARECEP` | `AchTransactions.DestinationAccountNumber` | Se exige coincidencia exacta (sin espacios externos) con `EntryDetails.AccountNumber` antes de construir solicitud. | `GUARDED_I03` |
| `MONTO` | `AchTransactions.Amount` | Se exige monto monetario positivo y coincidencia exacta con `EntryDetails.Amount`. | `GUARDED_I03` |
| `TIPTRAN` | `AchTransactions.TransactionCode` | Se exige crédito monetario y coincidencia con `EntryDetails.TransactionCode`. | `GUARDED_I03` |
| `IDTRAN` | `AchTransactions.TraceSequenceNumber` → 7 dígitos | Se verifica contra los últimos siete dígitos del `EntryDetails.SequenceNumber` de 15 dígitos. Los controles adicionales de secuencia/homologación existentes permanecen. | `GUARDED_I03` |
| `IDLOTE` | `BatchHeaders.BatchNumber` → 6 dígitos por fuente funcional | Se conserva la validación existente de rango/formato y vínculo con el lote NACHA; sin otra homologación del banco. | `EXISTING_GUARD` |
| `IDORIG` | `AchTransactions.CompanyIdentification` | Existe una identificación en `BatchHeaders.CompanyId`, pero el significado contractual frente a `CompanyIdentification` no está aprobado. No sustituir sin homologación ni asumir equivalencia. | `BLOCKED_FUNCTIONAL_CONFIRMATION` |
| `IDRECEP` | `EntryDetails.RecipIdNumber` | Proveniencia explícita del receptor NACHA. La validación de identidad de titular ante el banco continúa fuera de alcance. | `SOURCE_IDENTIFIED` |
| `FECEFEC` | `AchTransactions.EffectiveEntryDate` → `yyyyMMdd` | La transformación ya está implementada; debe certificarse la fecha valor frente al ciclo y el banco en UAT final. No redefinir calendario ni fecha efectiva en I03. | `EXISTING_MAPPING` |
| `DESTRAN` | `procTransacciones.paymentInformation` | Se conserva `ProcTransaccionesPaymentInformationBuilder` de 77 caracteres; no modificar la semántica de adendas. | `EXISTING_MAPPING` |
| `INFPAG` | `procTransacciones.paymentInformation` | Comparte intencionalmente builder de 77 caracteres con `DESTRAN`; equivalencia funcional pendiente de confirmación bancaria. | `BLOCKED_FUNCTIONAL_CONFIRMATION` |

## Código implementado (mínimo)

1. `ProcTransaccionesFunctionalSourceResolver.ValidateMonetaryEntryCorrelation`: antes de resolver campos para SOAP comprueba naturaleza crédito/código, monto, cuenta receptora y trace con el registro tipo 6 persistido. Códigos explícitos: `PROC_TRANSACCIONES_CREDIT_ONLY`, `PROC_TRANSACCIONES_AMOUNT_MISMATCH`, `PROC_TRANSACCIONES_RECEIVER_ACCOUNT_MISMATCH`, `PROC_TRANSACCIONES_TRACE_MISMATCH`.
2. `IncomingNachaExternalDispatchStage.SendAsync`: cuando el `operatingMode` efectivo es `Live`, verifica el XML justo antes de llamar a `ProcTransaccionesAsync`. Si contiene `NCTAORIG` no vacío, falla con `PROC_TRANSACCIONES_NCTAORIG_NOT_HOMOLOGATED`. Así cubre legacy y contract-driven y evita depender exclusivamente de `ProcTransaccionesDispatchOptions.Mode`, que puede diferir del modo efectivo configurado. No se transmite la solicitud. Para `DryRun`, la vista previa no cambia.
3. Pruebas escritas: fallos por cuatro inconsistencias NACHA/DB, rechazo Live del campo de cuenta origen (incluido namespace SOAP), y ausencia/vacío permitido. **No se ejecutaron .NET** en este ambiente.

## Riesgos abiertos y límites

- El `IntegrationMappingBootstrapper` **NO se modificó**; sigue publicando `NCTAORIG=entrydetails.accountnumber` en el baseline existente. Esta es una fuente semánticamente incorrecta para cuenta origen y ahora **no debe ejecutarse en Live**. No se alteraron reglas publicadas en una BD externa ni se crearon versiones/migraciones.
- La pantalla de readiness puede seguir presentando el seed como técnicamente configurado, porque su regla histórica da por válida precisamente esa fuente. **Readiness `OK` no demuestra homologación**, ni anula el veto Live incorporado. La revisión de paridad/readiness y eventual fuente aprobada se conservan como deuda en I05/I07; no activar Live.
- No se introdujeron campos alternativos de cuenta origen, códigos de banco, cálculos de monto, ni transformaciones NACHA nuevas.
- No se cambiaron archivos NACHA-M ni se consumió ningún servicio SOAP.

## Verificaciones y UAT

- `python scripts/validate-solution-projects.py`: `SOLUTION GRAPH: OK` (7 proyectos).
- `dotnet`: no instalado; compilación y unit tests `NOT_RUN_ENVIRONMENT`. Pruebas focales pendientes:
  - `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~NachaDesagregadoIntegrationMappingTests|FullyQualifiedName~ProcTransaccionesOriginAccountSafetyTests"`
- La homologación externa de origen (`NCTAORIG`) y de los significados de `IDORIG`/`INFPAG`, junto con UAT, corresponde al incremento final I07. El desarrollo de I04 no depende de esa homologación.
- `UseContractDrivenRequest`, `DryRun` y política no-Live permanecen intactos.
