# Informe focal NPT_H02_H03_FINAL

**Fecha:** 2026-11-16  
**Base:** `ACHInterbank_HOTFIX_XSD_TRANSPORTE_NCTAORIG_H010203_2026-11-16.zip`  
**Alcance:** solo H-02 (origen de NCTAORIG) y H-03 (expectativas readiness)  
**Decisión operativa:** NO-GO SOAP Live; no se alteró la configuración ni se autorizó despacho.

## H-02 — corregido en fuente, pendiente prueba .NET

La correspondencia funcional es **`AchTransactions.SourceAccountNumber` → `Proc_Transacciones.NCTAORIG`**. Se aplica tanto al mapping legacy como al motor de contrato:

- `IntegrationMappingReadinessService.ValidateProcTransaccionesNctaOrigRule`: acepta únicamente regla `Transaction / transaction.sourceAccountNumber`, sin `FixedValue` ni `DefaultValue`; retira el bloqueo por homologación externa de procedencia. El readiness con `transactionId` rechaza cuentas origen vacías.
- `CurrentIntegrationSourceProviderFactory.AddTransactionProviders`: habilita la fuente `transaction.sourceAccountNumber` para `WSCFAACH.Proc_Transacciones`, además de Contrapartidas, sin nueva estrategia.
- `ProcTransaccionesRequestMapper.EnsureNctaOrigSource`: exige valor presente y **comparación ordinal exacta** con la cuenta persistida; rechaza sustitución receptora, defaults, constantes y ausencia del campo aunque el XSD lo permita.
- `IncomingNachaExternalDispatchStage.EnsureLiveOriginAccountPresent`: conserva rechazo de solicitud sin NCTAORIG, vacía, nil, XML inapropiado o duplicado; sin rechazo incondicional por supuesta homologación. Se prohíbe DTD/resolución de entidades externas.

**Protecciones preservadas:** validación XSD final H-01 en CFA/Axon, SOAPAction, permisos/endpoint, idempotencia, fencing, recuperación sin reintento de resultado monetario incierto; F5 ACH Colombia sin ciclo sigue bloqueado independientemente de NCTAORIG. No se modificaron WSDL/XSD, migraciones, configuración de entornos ni clientes SOAP.

## H-03 — expectativas de pruebas corregidas; ejecución xUnit pendiente

- `TransactionIntegrationReadinessGuaranteeTests`: elimina un intento NCTAORIG duplicado del fixture, usa la fuente origen canónica, exige readiness positivo con mapping completo y negativo ante mapping a cuenta receptora o detalle NACHA.
- `NachaDesagregadoIntegrationMappingTests`: actualiza readiness positivo para la fuente correcta, rechazos por tránsito inválido, y aserciones del XML SOAP que contiene el valor exacto de `SourceAccountNumber`; añade prueba de cuenta origen ausente.
- `ProcTransaccionesOriginAccountSafetyTests`: actualiza prueba Live para aceptación de cuenta presente y rechazo de ausente/vacía/nil/duplicada, y DTD.
- `OfficialSoapXsdContractTests`: alinea la prueba con la nueva política funcional sin confundir XSD con aprobación operativa.
- `ProcTransaccionesRequestMapperTests`: completa el fixture mínimo con NCTAORIG y verifica su valor en contrato y XML.

## Verificaciones efectivamente realizadas

| Comprobación | Resultado real | Límite |
|---|---|---|
| `verify_h02_h03_source.py` | 23/23 PASS | Estático Python; no ejecuta C# |
| `verify_wsdl_xsd_offline.py` | exit 0 | Esquemas XML offline, no SOAP ni .NET |
| `achcol_nacha_transaction_counts_static_check.py` | exit 0 (28/28) | Fuente, no .NET |
| `e2e01_e2e02_source_static_check.py` | exit 0 (35/35) | Fuente/modelo SQLite |
| `res_h02_soap_preflight_sqlite_check.py` | exit 0 (27/27) | Fuente/modelo SQLite |
| `npt_cfa_late_static_sqlite_check.py` | exit 0 (15/15) | Fuente/modelo SQLite |
| `cenit_i13_nack_static_check.py` | exit 0 (33/33) | Fuente/modelo SQLite |
| `npt_focal_sqlite_source_guard.py` | exit 0 (26/26) | Fuente/modelo SQLite |
| `dotnet --info` | **exit 127, SDK ausente** | No se puede comprobar entorno .NET |
| `dotnet restore` | **exit 127, SDK ausente** | No ejecutado |
| `dotnet build --no-restore` | **exit 127, SDK ausente** | No compilado |
| `dotnet test --no-restore` | **exit 127, SDK ausente** | xUnit no ejecutado |

**No se ejecutaron** pruebas EF Core con SQL Server/PostgreSQL, simuladores HTTP ni UAT. Las pruebas xUnit son código añadido/modificado, no evidencia de ejecución. Los controles estáticos no prueban por sí solos ausencia de regresiones en CLR.

## Reproducción de lo pendiente (SDK .NET 10)

Desde `ACHInterbank_ajustado/`:

```bash
dotnet --info
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln --no-restore
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --no-restore --filter "FullyQualifiedName~NachaDesagregadoIntegrationMappingTests|FullyQualifiedName~TransactionIntegrationReadinessGuaranteeTests|FullyQualifiedName~ProcTransaccionesOriginAccountSafetyTests|FullyQualifiedName~ProcTransaccionesRequestMapperTests|FullyQualifiedName~OfficialSoapXsdContractTests"
dotnet test ACHInterbank.sln --no-restore
python docs/npt-h02-h03-final/verify_h02_h03_source.py
```

No habilitar Live ni concluir cierre ejecutable hasta completar compilación y xUnit. Verificaciones de homologación F5, políticas bancarias y operación productiva ajenas al criterio de procedencia H-02 permanecen independientes.

## Inventario preciso de código C# modificado

**Productivo (4):**
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/IncomingNachaPostProcessingPipeline.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcTransaccionesRequestMapper.cs`
- `src/Cfa.ACHInterbank.Persistence/Integrations/Providers/CurrentIntegrationSourceProviderFactory.cs`
- `src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingReadinessService.cs`

**Pruebas (5):**
- `tests/Cfa.ACHInterbank.Tests/NachaDesagregadoIntegrationMappingTests.cs`
- `tests/Cfa.ACHInterbank.Tests/OfficialSoapXsdContractTests.cs`
- `tests/Cfa.ACHInterbank.Tests/ProcTransaccionesOriginAccountSafetyTests.cs`
- `tests/Cfa.ACHInterbank.Tests/ProcTransaccionesRequestMapperTests.cs`
- `tests/Cfa.ACHInterbank.Tests/TransactionIntegrationReadinessGuaranteeTests.cs`

**Archivos eliminados:** ninguno. **Migraciones:** ninguna. **WSDL/XSD:** sin cambios.
