# ACH Interbank — Correcciones focales SOAP

**Fecha:** 2026-11-16  
**Base:** `ACHInterbank_NPT_H02_H03_FINAL.zip`, matriz de 66 campos, WSDL/XSD originales de WSCFAACH y WSAxon, manuales ACH Colombia V35 y CENIT.  
**Alcance:** `Proc_Transacciones`, `Proc_Contrapartidas` y `RegistrarRespuestaTransaccion` (56 campos de solicitud y 10 de respuesta). `PLValidarUsuarioBV` excluido. No se incorporaron migraciones, tablas ni operaciones bancarias reales.

## Cambios implementados

| Archivos | Corrección |
|---|---|
| `src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingBootstrapper.cs` | `NORIG ← batchHeaders.companyName`, `IDORIG ← batchHeaders.companyId`, `NCTARECEP ← entrydetails.accountnumber`. En `Proc_Contrapartidas` suprime del seed los tres orígenes no acreditados para `OFEMP`, `OFIDARCH` y `OFDIRECCIONIP`; actualiza mappings **del sistema** ya publicados, sin sobrescribir mappings personalizados. |
| `src/Cfa.ACHInterbank.Application/ACH/Models/IncomingNachaDispatchModels.cs` y `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcTransaccionesFunctionalSourceResolver.cs` | Expone nombre e identificación del originador desde `BatchHeaders` NACHA-M tipo 5; conserva cuenta receptora desde `EntryDetails` tipo 6 y la correlación monto/cuenta/secuencia. |
| `src/Cfa.ACHInterbank.Persistence/Integrations/Providers/CurrentIntegrationSourceProviderFactory.cs` | Registra fuentes reales del lote NACHA-M para las nuevas rutas `NORIG`/`IDORIG`. Mantiene las fuentes diferenciales de Axon. |
| `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcTransaccionesRequestMapper.cs` | `NCTAORIG` opcional puede omitirse sin fabricar cuenta originadora; cuando se incluye exige fuente exacta `Transactions.SourceAccountNumber`, nunca cuenta receptora ni default. El control Live independiente **sigue rechazando** su ausencia. |
| `src/Cfa.ACHInterbank.Persistence/Integrations/Services/ProcContrapartidasFunctionalMappingResolver.cs` | Rechazo explícito de equivalencias antiguas `clearinghouse.code → OFEMP`, `batch.id → OFIDARCH` y de IP ficticia `0.0.0.0`, incluso si están publicadas. Preserva `OFCTA ← Transactions.SourceAccountNumber`, importe y débito monetario. |
| `src/Cfa.ACHInterbank.Application/Integrations/Contracts/OfficialSoapContracts.cs`, `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcContrapartidasRequestMapper.cs` y `src/Cfa.ACHInterbank.External/Connections/WscfaachSoapClient.cs` | Añade a la validación XSD local preenvío la solicitud `Proc_Contrapartidas` y verifica el `SOAPAction` oficial. No convierte los campos `minOccurs=0` en obligatorios. |
| `tests/Cfa.ACHInterbank.Tests/IntegrationBootstrapperTests.cs`, `IntegrationMappingEndToEndTests.cs`, `NachaDesagregadoIntegrationMappingTests.cs`, `OfficialSoapXsdContractTests.cs` y `TransactionIntegrationReadinessGuaranteeTests.cs` | Pruebas focales añadidas/ajustadas de fuentes físicas, ausencia de cuenta origen, rechazo Live, compatibilidad XML/XSD, normalización y falta de mapping saliente. |
| `docs/npt-soap-xsd-final-01/verify_source_guards.py` | Actualiza dos aserciones obsoletas para comprobar las garantías efectivas de cuenta origen y Live; sin relajar reglas. |

**Sin cambios de mapping:** `RegistrarRespuestaTransaccion`, sus 7 fuentes diferenciales y sus 3 campos de respuesta ya conservan correlación, estados y causales por cámara; `PLValidarUsuarioBV` intacto.

## Verificación efectivamente ejecutada

- **PASS — 51 comprobaciones focales offline** (`docs/soap-mapping-fixes-2026-11-16/verify_focal_offline.py`): conteo de 66 campos, tipos, orden, opcionalidad, muestras XML positivas y negativas de las tres operaciones, SOAPAction, y aserciones de fuente/correlación. Python `lxml`, **no** ejecución de C#.
- **PASS — 38 verificaciones WSDL/XSD offline** (`docs/npt-soap-xsd-final-01/verify_wsdl_xsd_offline.py`): cinco XSD originales, namespace, document/literal y muestras de solicitud/respuesta.
- **PASS — 33/33 controles estáticos** (`docs/npt-soap-xsd-final-01/verify_source_guards.py`): snapshots originales y barreras de transporte.
- **PASS — grafo de solución** (`python scripts/validate-solution-projects.py`): 7 proyectos con referencias existentes.
- **NO EJECUTADO — `dotnet build` ni `dotnet test`:** no hay SDK `dotnet` instalado. Las pruebas C# añadidas están incluidas, pero **no se declaran aprobadas**. No se ejecutó UAT, transporte SOAP bancario, bases productivas ni operaciones Live.

## Mappings bloqueados por datos funcionales realmente faltantes

| Campo | Falta para habilitar el mapping de manera segura |
|---|---|
| `Proc_Contrapartidas.OFEMP` | Identificar y documentar código real de empresa/originador esperado por CFA; `ClearingHouse.Code` no es equivalente demostrado. |
| `Proc_Contrapartidas.OFIDARCH` | Identificador efectivo del archivo saliente y su fuente persistida; `AchBatchId` no demuestra esa identidad. |
| `Proc_Contrapartidas.OFDIRECCIONIP` | Dirección IP de origen efectiva, capturada en un contexto técnico confiable, y su regla de uso; `0.0.0.0` queda rechazada. |
| `Proc_Transacciones.NCTAORIG` **para Live cuando no hay cuenta origen acreditada** | Fuente independiente, válida y trazable a la cuenta originadora entrante. No puede tomarse `EntryDetails.AccountNumber`; aunque XSD permite omitirla, Live conserva bloqueo. |

**Estado:** código corregido y empaquetado con cierre seguro de fuentes no acreditadas. **No equivale a una compilación aprobada ni a homologación bancaria funcional.**
