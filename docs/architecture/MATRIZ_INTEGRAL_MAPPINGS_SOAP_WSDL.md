# Matriz integral de mappings SOAP WSDL

Fecha: 2026-06-26  
Estado: diagnostico y plan, sin implementacion  
Productivo: NO-GO

## 1. Resumen ejecutivo

El catalogo funcional de integraciones SOAP de ACHInterbank esta estructuralmente alineado con los tres servicios dentro del alcance:

- `WSCFAACH.Proc_Transacciones`
- `WSCFAACH.Proc_Contrapartidas`
- `WSAXON.RegistrarRespuestaTransaccion`

`PLValidarUsuarioBV` aparece en interfaces/clientes SOAP, pero esta fuera de alcance funcional. No debe catalogarse, sembrarse, aparecer en readiness, UI, mappings ni pruebas funcionales.

El estado actual ya corrige la desviacion principal detectada previamente: `RegistrarRespuestaTransaccion` queda con los 7 parametros reales del WSDL y sin `ANS*` vigentes. Los `ANS*` se conservan solo donde corresponden a `Proc_Contrapartidas`.

La principal brecha pendiente no esta en Registrar. Esta en la calidad funcional de mappings publicados para `Proc_Transacciones` y `Proc_Contrapartidas`: varios parametros requeridos quedan cubiertos por constantes genericas (`SEED`, `1`, `0`) o por fuentes soportadas por resolvers pero no visibles de forma consistente en la UI de mappings. Eso puede producir un readiness tecnico `OK` aunque existan campos funcionalmente debiles o pendientes de homologacion.

No se debe declarar readiness funcional completo mientras existan parametros requeridos funcionalmente sin fuente confiable, constantes sin politica documentada o placeholders de seed.

## 2. Alcance revisado

Se revisaron, en modo solo lectura, los siguientes componentes:

- Catalogo y bootstrap:
  - `src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationCatalogBootstrapper.cs`
  - `src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationCatalogService.cs`
  - `src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingBootstrapper.cs`
  - `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/Seeders/IntegrationMappingScenarioSeeder.cs`
- Mappings, validacion y readiness:
  - `src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingSetService.cs`
  - `src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingValidationService.cs`
  - `src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingReadinessService.cs`
  - `src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingTraceWriter.cs`
- Runtime funcional:
  - `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcTransaccionesRequestMapper.cs`
  - `src/Cfa.ACHInterbank.Persistence/Integrations/Services/ProcContrapartidasFunctionalMappingResolver.cs`
  - `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/DifferentialPrenotificationResponseProcessor.cs`
- Clientes/configuracion SOAP:
  - `src/Cfa.ACHInterbank.Persistence/Security/Services/SoapIntegrationSettingsService.cs`
  - `src/Cfa.ACHInterbank.External/Connections/WscfaachSoapClient.cs`
  - `src/Cfa.ACHInterbank.External/Connections/WsAxonRespuestaTransaccionesSoapClient.cs`
- Entidades/configuracion EF:
  - `src/Cfa.ACHInterbank.Domain/Entities/Integrations/IntegrationMappingModels.cs`
  - `src/Cfa.ACHInterbank.Persistence/Configuration/IntegrationMappingSetConfiguration.cs`
  - `src/Cfa.ACHInterbank.Persistence/Configuration/IntegrationMethodParameterConfiguration.cs`
- Seed runtime:
  - `src/Cfa.ACHInterbank.Persistence/DataBase/DbInitializer.cs`
  - `src/Cfa.ACHInterbank.Api/Controllers/MaintenanceController.cs`
- Frontend:
  - `web/ach-interbank-ui/src/app/features/integrations/pages/mapping-sets-page.component.ts`
  - `web/ach-interbank-ui/src/app/features/integrations/pages/mapping-sets-page.component.html`
  - `web/ach-interbank-ui/src/app/features/integrations/pages/mapping-sets-page.component.scss`
  - `web/ach-interbank-ui/src/app/features/integrations/pages/mapping-sets-page.component.spec.ts`
  - `web/ach-interbank-ui/src/app/core/services/integration-mapping-admin.service.ts`
  - `web/ach-interbank-ui/src/app/features/admin/components/soap-integration-settings.component.ts`
  - `web/ach-interbank-ui/src/app/core/services/soap-integration-settings.service.ts`
- Tests:
  - `tests/Cfa.ACHInterbank.Tests/IntegrationBootstrapperTests.cs`
  - `tests/Cfa.ACHInterbank.Tests/IntegrationMappingEndToEndTests.cs`
  - `tests/Cfa.ACHInterbank.Tests/TransactionIntegrationReadinessGuaranteeTests.cs`
  - `tests/Cfa.ACHInterbank.Tests/DifferentialPrenotificationResponseProcessorTests.cs`
  - `tests/Cfa.ACHInterbank.Tests/IntegrationMappingTraceWriterTests.cs`
  - `tests/Cfa.ACHInterbank.Tests/NachaDesagregadoIntegrationMappingTests.cs`
- Documentacion operativa y UX:
  - `README.md`
  - `docs/go-live-readiness/DOCKER_RUNTIME_READINESS.md`
  - `docs/go-live-readiness/README_OPERATIVO_RELEASE_UAT.md`
  - `docs/uat/EVIDENCIA_TECNICA_UAT_RUNTIME.md`
  - `docs/architecture/INTEGRACIONES_SOAP_SETTINGS_VS_MAPPINGS.md`
  - `docs/ux/REDISENO_INTEGRATION_MAPPINGS.md`
  - `docs/ux/REDISENO_SOAP_SETTINGS.md`

## 3. Exclusiones confirmadas

- `PLValidarUsuarioBV` queda excluido. No se cataloga en `IntegrationMethods`, no se siembra en mappings funcionales y no debe aparecer en readiness ni UI funcional.
- No se deben ejecutar llamadas SOAP reales.
- No se debe tocar NACHA-M ni golden files.
- No se debe tocar Docker ni compose.
- No se debe tocar OpenBao.
- No se debe tocar logica monetaria SOAP.
- No se deben cambiar endpoints publicos.
- No se debe mezclar `/integraciones/soap-settings` con la matriz funcional de `/integraciones/mappings`.

## 4. Contrato WSDL vigente por servicio

No se encontro un archivo `.wsdl` o `.xsd` fisico versionado en el repo para extraer automaticamente `minOccurs` y `nillable`. Por rigor, esos atributos deben incorporarse desde el WSDL oficial o desde un snapshot versionado del WSDL. En este documento se separa lo que el codigo cataloga actualmente de lo que debe confirmarse contra WSDL oficial.

| Servicio | Cliente tecnico | SOAP Action actual | Direccion funcional | Parametros vigentes | Observacion |
|---|---|---|---|---:|---|
| `Proc_Transacciones` | `WscfaachSoapClient` | `http://tempuri.org/IWSCFAACH/Proc_Transacciones` | request monetario credito | 27 | Incluye `RTAACH`/`RTALOC` en catalogo actual; confirmar si son request WSDL o respuesta. |
| `Proc_Contrapartidas` | `WscfaachSoapClient` | `http://tempuri.org/IWSCFAACH/Proc_Contrapartidas` | request monetario debito | 22 | Conserva `ANS*` donde corresponden por contrato de este servicio. |
| `RegistrarRespuestaTransaccion` | `WsAxonRespuestaTransaccionesSoapClient` | `http://tempuri.org/IWSAxonRespuestaTransacciones/RegistrarRespuestaTransaccion` | response/notificacion no monetaria | 7 | Correcto: 7 parametros WSDL y ningun `ANS*` vigente. |

Parametros vigentes esperados para `RegistrarRespuestaTransaccion`:

- `idCanal`
- `nombreCanal`
- `idTransaccion`
- `idEstado`
- `causal`
- `idTransaccionAxon`
- `descripcionCausal`

Parametros que no deben existir como contrato vigente de `RegistrarRespuestaTransaccion`:

- `ANSIDLOTE`
- `ANSST`
- `ANCLC`
- `ANSIDTX`
- `ANSIDREVER`

## 5. Matriz integral: RegistrarRespuestaTransaccion

| Servicio | Parametro | Tipo WSDL | Req. WSDL | Req. funcional | Fuente actual | Tabla/Campo | Regla | Estado | Seed/Bootstrap | Test actual | Accion recomendada |
|---|---|---|---|---|---|---|---|---|---|---|---|
| RegistrarRespuestaTransaccion | `idCanal` | int | Si | Si | DifferentialResponse | `differentialResponse.idCanal` | sin transformacion | Listo con fuente funcional | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | `IntegrationBootstrapperTests`, `DifferentialPrenotificationResponseProcessorTests`, spec Angular | Mantener. |
| RegistrarRespuestaTransaccion | `nombreCanal` | string | Si | Si | DifferentialResponse | `differentialResponse.nombreCanal` | sin transformacion | Listo con fuente funcional | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | `IntegrationBootstrapperTests`, spec Angular | Mantener. |
| RegistrarRespuestaTransaccion | `idTransaccion` | string | Si | Si | DifferentialResponse | `differentialResponse.idTransaccion` | sin transformacion | Listo con fuente funcional | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | `IntegrationBootstrapperTests`, `IntegrationMappingTraceWriterTests` | Mantener. |
| RegistrarRespuestaTransaccion | `idEstado` | int | Si | Si | DifferentialResponse | `differentialResponse.idEstado` | sin transformacion | Listo con fuente funcional | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | `IntegrationBootstrapperTests`, `DifferentialPrenotificationResponseProcessorTests` | Mantener. |
| RegistrarRespuestaTransaccion | `causal` | string | No confirmado | Condicional | DifferentialResponse | `differentialResponse.codigoCausalExterna` | default opcional actual | Listo, revisar fallback placeholder | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | `IntegrationBootstrapperTests`, `IntegrationMappingTraceWriterTests` | Evitar placeholder para opcionales; documentar condicion. |
| RegistrarRespuestaTransaccion | `idTransaccionAxon` | int | Si | Si | DifferentialResponse | `differentialResponse.idTransaccionServicioExterno` | sin transformacion | Listo con fuente funcional | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | `IntegrationBootstrapperTests`, `DifferentialPrenotificationResponseProcessorTests` | Mantener. |
| RegistrarRespuestaTransaccion | `descripcionCausal` | string | No confirmado | Condicional | DifferentialResponse | `differentialResponse.descripcionCausalExterna` | default opcional actual | Listo, revisar fallback placeholder | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | `IntegrationBootstrapperTests`, `DifferentialPrenotificationResponseProcessorTests` | Evitar placeholder para opcionales; documentar condicion. |

## 6. Matriz integral: Proc_Contrapartidas

| Servicio | Parametro | Tipo WSDL | Req. WSDL | Req. funcional | Fuente actual | Tabla/Campo | Regla | Estado | Seed/Bootstrap | Test actual | Accion recomendada |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Proc_Contrapartidas | `OFNIT` | string | Si | Si | Transaction | `transaction.companyidentification` | default `900123456` | Listo backend, no visible como fuente permitida UI | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | `IntegrationMappingEndToEndTests` | Alinear UI/catalogo o clasificar fuente tecnica. |
| Proc_Contrapartidas | `OFEMP` | string | Si | Si | ClearingHouse | `clearinghouse.code` | default `ACH` | Listo backend, no visible como fuente permitida UI | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | parcial | Definir si debe mostrarse como fuente valida en matriz. |
| Proc_Contrapartidas | `OFCTA` | string | Si | Si | Transaction | `transaction.originatingdfi` | default `000010070` | Brecha alta | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | parcial | Validar si debe ser cuenta origen real y no DFI. |
| Proc_Contrapartidas | `OFDD` | string | Si | Si | Constant | `constant.value` | default `C` | Brecha alta | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | parcial | Confirmar codigo oficial `D/C`; catalogo sugiere naturaleza debito. |
| Proc_Contrapartidas | `OFFECHEFEC` | string | Si | Si | Cycle | `cycle.processingdate` | default fecha actual | Listo backend, no visible UI | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | parcial | Documentar formato y transformacion de fecha. |
| Proc_Contrapartidas | `OFMONDEB` | decimal | Si | Si | Constant | `constant.value` | `0` | Brecha critica funcional | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | parcial | Confirmar semantica monetaria antes de modificar. |
| Proc_Contrapartidas | `OFMONCRE` | decimal | Si | Si | Transaction | `transaction.amount` | default `0` | Brecha critica funcional | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | parcial | Confirmar si monto real corresponde aqui o en `OFMONDEB`. |
| Proc_Contrapartidas | `OFIDARCH` | int | Si | Si | Batch | `batch.id` | default `1` | Listo backend, no visible UI | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | parcial | Definir fuente oficial de archivo. |
| Proc_Contrapartidas | `OFIDLOT` | int | Si | Si | Batch | `batch.id` | default `1` | Listo backend | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | `IntegrationMappingEndToEndTests` | Mantener si batch id es fuente correcta. |
| Proc_Contrapartidas | `OFST` | string | Si | Si | Constant | fixed `SEED` | placeholder | Pendiente de definicion funcional | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | parcial | Reemplazar por estado homologado o documentar constante. |
| Proc_Contrapartidas | `OFIDTX` | string | Si | Si | Transaction | `transaction.reference` | default `REF-1` | Listo backend, no visible UI | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | `IntegrationMappingEndToEndTests` | Mantener si reference es identificador oficial. |
| Proc_Contrapartidas | `OFIDREVER` | int | Si | Condicional | Constant | fixed `1` | placeholder | Pendiente/posible incorrecto | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | parcial | Definir reverso normal vs reverso real. |
| Proc_Contrapartidas | `OFIDEBAPLI` | int | Si | Si | Transaction | `transaction.id` | default `1` | Listo backend | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | parcial | Mantener si aplica a id interno. |
| Proc_Contrapartidas | `OFIDCAMCOMPE` | int | Si | Si | ClearingHouse | `clearinghouse.id` | default `1` | Listo backend, no visible UI | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | `TransactionIntegrationReadinessGuaranteeTests` | Alinear visibilidad/catologo. |
| Proc_Contrapartidas | `OFDIRECCIONIP` | string | Si | Auditoria | Constant | `0.0.0.0` | constante | Listo con constante tecnica | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | parcial | Documentar politica de IP. |
| Proc_Contrapartidas | `OFLIBRE` | string | Si | No definido | Constant | fixed `SEED` | placeholder | Pendiente de definicion funcional | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | parcial | Definir o clasificar reservado. |
| Proc_Contrapartidas | `OFLIBRE1` | int | Si | No definido | Constant | fixed `1` | placeholder | Pendiente de definicion funcional | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | parcial | Definir o clasificar reservado. |
| Proc_Contrapartidas | `ANSIDLOTE` | int | No confirmado/reservado | No bloquea | Sin regla publicada | N/A | N/A | Opcional/reservado por contrato | `IntegrationCatalogBootstrapper` | `IntegrationBootstrapperTests` | Mantener sin bloquear readiness. |
| Proc_Contrapartidas | `ANSST` | string | No confirmado/reservado | No bloquea | Sin regla publicada | N/A | N/A | Opcional/reservado por contrato | `IntegrationCatalogBootstrapper` | `IntegrationBootstrapperTests`, parser response | Mantener. |
| Proc_Contrapartidas | `ANCLC` | string | No confirmado/reservado | No bloquea | Sin regla publicada | N/A | N/A | Opcional/reservado por contrato | `IntegrationCatalogBootstrapper` | `IntegrationBootstrapperTests`, parser response | Mantener. |
| Proc_Contrapartidas | `ANSIDTX` | string | No confirmado/reservado | No bloquea | Sin regla publicada | N/A | N/A | Opcional/reservado por contrato | `IntegrationCatalogBootstrapper` | `IntegrationBootstrapperTests`, parser response | Mantener. |
| Proc_Contrapartidas | `ANSIDREVER` | int | No confirmado/reservado | No bloquea | Sin regla publicada | N/A | N/A | Opcional/reservado por contrato | `IntegrationCatalogBootstrapper` | `IntegrationBootstrapperTests` | Mantener. |

## 7. Matriz integral: Proc_Transacciones

| Servicio | Parametro | Tipo WSDL | Req. WSDL | Req. funcional | Fuente actual | Tabla/Campo | Regla | Estado | Seed/Bootstrap | Test actual | Accion recomendada |
|---|---|---|---|---|---|---|---|---|---|---|---|
| Proc_Transacciones | `TREG` | string | Si | Si | Constant | fixed `SEED` | placeholder | Incorrecto | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | `NachaDesagregadoIntegrationMappingTests` parcial | Usar constante oficial si aplica, probablemente tipo registro. |
| Proc_Transacciones | `TIPTRAN` | int | Si | Si | EntryDetail | `entryDetails.transactionCode` | sin conversion | Listo con revision de tipo | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | `NachaDesagregadoIntegrationMappingTests` | Validar int/string y transformacion. |
| Proc_Transacciones | `BCORECEP` | int | Si | Si | NachaHeader | `nachaHeaders.immediateDestination` | sin conversion | Listo con revision de tipo | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | `NachaDesagregadoIntegrationMappingTests` | Validar formato banco receptor. |
| Proc_Transacciones | `BCOORIG` | int | Si | Si | NachaHeader | `nachaHeaders.immediateOrigin` | sin conversion | Listo con revision de tipo | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | `NachaDesagregadoIntegrationMappingTests` | Validar formato banco origen. |
| Proc_Transacciones | `NORIG` | string | Si | Si | BatchHeader | `batchHeaders.companyName` | sin transformacion | Listo | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | `NachaDesagregadoIntegrationMappingTests` | Mantener. |
| Proc_Transacciones | `NCTAORIG` | string | Si | Si | BatchHeader | `batchHeaders.companyId` | sin transformacion | Pendiente funcional | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | parcial | Confirmar si `companyId` equivale a cuenta origen. |
| Proc_Transacciones | `IDORIG` | string | Si | Si | BatchHeader | `batchHeaders.companyId` | sin transformacion | Listo con fuente funcional | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | parcial | Mantener si WSDL pide id originador. |
| Proc_Transacciones | `DESTRAN` | string | Si | Si | BatchHeader | `batchHeaders.companyEntryDescription` | sin transformacion | Listo | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | `NachaDesagregadoIntegrationMappingTests` | Mantener. |
| Proc_Transacciones | `FECEFEC` | int | Si | Si | BatchHeader | `batchHeaders.effectiveEntryDate` | sin DateFormat | Listo con conversion pendiente | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | parcial | Definir transformacion de fecha documentada. |
| Proc_Transacciones | `NCTARECEP` | string | Si | Si | EntryDetail | `entryDetails.accountNumber` | sin transformacion | Listo | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | `NachaDesagregadoIntegrationMappingTests` | Mantener. |
| Proc_Transacciones | `MONTO` | double | Si | Si | EntryDetail | `entryDetails.amount` | sin NumericFormat | Listo con conversion pendiente | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | `NachaDesagregadoIntegrationMappingTests` | Definir formato monetario. |
| Proc_Transacciones | `NRECEP` | string | Si | Si | EntryDetail | `entryDetails.recipUserName` | sin transformacion | Listo | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | `NachaDesagregadoIntegrationMappingTests` | Mantener. |
| Proc_Transacciones | `IDRECEP` | string | Si | Si | EntryDetail | `entryDetails.recipIdNumber` | sin transformacion | Listo | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | `NachaDesagregadoIntegrationMappingTests` | Mantener. |
| Proc_Transacciones | `DISCRE` | string | Si | No definido | Constant | fixed `SEED` | placeholder | Pendiente de definicion funcional | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | parcial | Definir o reservar. |
| Proc_Transacciones | `CONV` | string | Si | Si/parametrico | Constant | fixed `SEED` | placeholder | Pendiente de definicion funcional | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | parcial | Definir fuente de convenio. |
| Proc_Transacciones | `PROD` | string | Si | Si/parametrico | Constant | fixed `SEED` | placeholder | Pendiente de definicion funcional | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | parcial | Definir fuente de producto. |
| Proc_Transacciones | `INFPAG` | string | Si | Si | AddendaRecord | `addendaRecords.infofromOriginator` | sin transformacion | Listo | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | `NachaDesagregadoIntegrationMappingTests` | Mantener. |
| Proc_Transacciones | `IDTRAN` | long | Si | Si | EntryDetail | `entryDetails.sequenceNumber` | sin conversion | Listo con revision de tipo | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | `NachaDesagregadoIntegrationMappingTests` | Validar long/string. |
| Proc_Transacciones | `IDLOTE` | string | Si | Si | BatchHeader | `batchHeaders.batchNumber` | sin transformacion | Listo | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | parcial | Mantener si batch number es suficiente. |
| Proc_Transacciones | `REGLOTE` | long | Si | Si | BatchControl | `batchControls.entryAddendaCount` | sin transformacion | Pendiente funcional | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | parcial | Confirmar que conteo equivale a registro lote. |
| Proc_Transacciones | `IREVER` | int | Si | Condicional | Constant | fixed `1` | placeholder | Posible incorrecto | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | parcial | Definir reverso normal. |
| Proc_Transacciones | `LIBRE` | string | Si | No definido | Constant | fixed `SEED` | placeholder | Pendiente de definicion funcional | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | parcial | Definir o reservar. |
| Proc_Transacciones | `IDCAMCOMPE` | int | Si | Si | Constant | fixed `1` | placeholder | Brecha alta | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | `TransactionIntegrationReadinessGuaranteeTests` | Mapear a clearing house/ciclo si fuente existe. |
| Proc_Transacciones | `DIRECCIONIP` | string | Si | Auditoria | Constant | fixed `SEED` | placeholder | Pendiente de definicion funcional | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | parcial | Definir IP tecnica o constante documentada. |
| Proc_Transacciones | `LIBRE1` | int | Si | No definido | FileControl | `fileControls.blockCount` | sin transformacion | Pendiente funcional | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | parcial | Confirmar semantica. |
| Proc_Transacciones | `RTAACH` | string | Si en catalogo actual | Probable no bloqueante | Constant | fixed `SEED` | placeholder | Brecha alta | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | parser response | Confirmar request vs response; no debe bloquear si es response. |
| Proc_Transacciones | `RTALOC` | string | Si en catalogo actual | Probable no bloqueante | Constant | fixed `SEED` | placeholder | Brecha alta | `IntegrationCatalogBootstrapper`, `IntegrationMappingBootstrapper` | parser response | Confirmar request vs response; no debe bloquear si es response. |

## 8. Comparacion contra catalogo actual

### IntegrationMethods

Estado actual esperado:

- `WSCFAACH.Proc_Contrapartidas`
- `WSCFAACH.Proc_Transacciones`
- `WSAXON.RegistrarRespuestaTransaccion`

No debe existir `PLValidarUsuarioBV` como metodo funcional catalogado.

### IntegrationMethodParameters

Conteos actuales cubiertos por tests:

- `Proc_Transacciones`: 27 parametros.
- `Proc_Contrapartidas`: 22 parametros.
- `RegistrarRespuestaTransaccion`: 7 parametros.

`RegistrarRespuestaTransaccion` solo debe tener:

- `idCanal`
- `nombreCanal`
- `idTransaccion`
- `idEstado`
- `causal`
- `idTransaccionAxon`
- `descripcionCausal`

### IntegrationSourceCatalogFields

El bootstrap crea 59 fuentes por metodo. Incluye fuentes de negocio (`Transaction`, `Cycle`, `ClearingHouse`, `Constant`) y fuentes NACHA desagregadas (`NachaHeader`, `BatchHeader`, `EntryDetail`, `AddendaRecord`, `BatchControl`, `FileControl`) mas `DifferentialResponse`.

Brecha: la UI de mappings considera permitidas visualmente solo:

- `NachaHeader`
- `BatchHeader`
- `EntryDetail`
- `AddendaRecord`
- `BatchControl`
- `FileControl`
- `DifferentialResponse`

Por eso reglas activas con `Transaction`, `Cycle`, `ClearingHouse` o `Constant` pueden aparecer como `Sin mapear` en la pantalla principal aunque readiness las cuente activas.

### IntegrationMappingSets

Esperado en base limpia:

- `ProcContrapartidas Published`
- `ProcTransacciones Published NACHA desagregado`
- `RegistrarRespuestaTransaccion Published respuesta diferencial`

En Development/Testing, el seeder demo puede crear drafts para `Proc_Contrapartidas`.

### IntegrationMappingRules

Conteos esperados:

- 27 reglas publicadas para `Proc_Transacciones`.
- 17 reglas publicadas para `Proc_Contrapartidas`.
- 7 reglas publicadas para `RegistrarRespuestaTransaccion`.

Los 5 `ANS*` de `Proc_Contrapartidas` quedan sin reglas publicadas y no bloquean readiness.

## 9. Estado de seeds/bootstrap

### Base limpia

`DbInitializer.SeedAllAsync` ejecuta:

1. `IntegrationCatalogBootstrapper.EnsureAsync`.
2. `IntegrationMappingBootstrapper.EnsureAsync`.
3. Seeders registrados por `IDbSeeder`.

`/Maintenance/seed` ejecuta `DbInitializer.SeedAllAsync` por POST.

### Base existente

Para `RegistrarRespuestaTransaccion`, el bootstrap:

- Normaliza acciones de historia previas.
- Desactiva parametros no-WSDL al aplicar el catalogo vigente.
- Archiva mapping sets publicados incompatibles.
- Crea mapping publicado con los 7 parametros WSDL.
- No borra historia.

### Idempotencia

Los tests existentes validan idempotencia de:

- catalogo;
- mappings base publicados;
- seeders demo en Development/Testing.

### SQL Server 2025 y migraciones

La documentacion ya establece que luego de `down -v` se debe arrancar con:

```powershell
$env:DATABASE_APPLY_MIGRATIONS="true"
```

antes de ejecutar:

```powershell
curl -i -X POST http://localhost:843/Maintenance/seed
```

## 10. Estado de mappings publicados

### RegistrarRespuestaTransaccion

Estado: correcto.

- 7 parametros WSDL activos.
- Sin `ANS*` activos.
- 7 reglas publicadas.
- Fuentes `DifferentialResponse` validas.
- Trazabilidad usa campos WSDL.

### Proc_Contrapartidas

Estado: tecnicamente publicable, funcionalmente incompleto.

- 22 parametros catalogados.
- 17 requeridos con regla publicada.
- 5 `ANS*` opcionales/reservados sin regla publicada.
- Brechas en semantica monetaria, constantes y fuentes no visibles en UI.

### Proc_Transacciones

Estado: tecnicamente completo, funcionalmente pendiente en varios campos.

- 27 parametros catalogados.
- 27 reglas publicadas.
- Varias reglas son fuentes NACHA reales.
- Varias reglas son placeholders `SEED` o constantes no homologadas.
- `RTAACH`/`RTALOC` requieren confirmacion de direccion real.

## 11. Estado de readiness

Readiness actual valida que parametros `Required=true` tengan reglas activas. No distingue suficientemente:

- requerido WSDL;
- requerido funcional;
- opcional/reservado;
- placeholder de seed;
- fuente funcional confiable;
- constante documentada;
- fallback transicional peligroso.

Riesgo principal: readiness puede devolver `OK` cuando un parametro requerido esta cubierto por `SEED`, `1`, `0` o una constante no homologada.

La correccion futura debe introducir politica de readiness funcional por parametro, no solo por presencia de regla.

## 12. Estado de trazabilidad/auditoria

Componentes existentes:

- `IntegrationMappingSetHistory`: historial de versiones y acciones de mapping sets.
- `IntegrationMappingTrace`: traza por operacion/mapping set/version.
- `IntegrationMappingTraceEntry`: traza campo a campo con valor sanitizado, origen, destino, regla y missing.

`RegistrarRespuestaTransaccion` ya tiene pruebas que validan:

- traza campo a campo;
- no movimiento monetario;
- no `ANS*`;
- uso de fuentes `DifferentialResponse`.

Brecha pendiente: robustecer traza equivalente para `Proc_Transacciones` y `Proc_Contrapartidas`, especialmente para detectar placeholders en campos criticos.

## 13. Impacto en /integraciones/mappings

La pantalla:

- consume metodos, parametros, catalogo de origen y mapping sets reales del backend;
- filtra los tres servicios funcionales esperados;
- no muestra `PLValidarUsuarioBV`;
- no mezcla endpoint ni SOAP Action;
- conserva auditoria con endpoint real `GET api/integrations/mappingsets/{id}/history`.

Brecha UX/funcional:

- fuentes `Transaction`, `Cycle`, `ClearingHouse` y `Constant` no se consideran visibles como fuentes permitidas principales;
- por eso mappings activos pueden mostrarse como `Sin mapear`;
- falta estado visual para `Opcional/reservado` y `Pendiente de definicion funcional`.

Mejora UX opcional:

- renombrar `Ver auditoria` a `Historial de cambios`, sin eliminar funcionalidad.

## 14. Impacto en /integraciones/soap-settings

`/integraciones/soap-settings` debe permanecer separado. Su responsabilidad es:

- endpoint;
- SOAP Action;
- enabled/disabled;
- input mappings tecnicos;
- prueba/control tecnico.

No debe ser fuente de verdad de la matriz campo a campo.

Observacion: para `RegistrarRespuestaTransaccion`, `soap-settings` mantiene input tecnico `respuesta -> Respuesta`, mientras la matriz funcional maneja los 7 parametros WSDL. Esa diferencia no debe corregirse desde mappings sin una decision tecnica separada sobre el cliente SOAP fisico.

## 15. Brechas criticas

| Servicio | Parametro | Problema | Impacto | Archivo probable | Test necesario |
|---|---|---|---|---|---|
| Proc_Contrapartidas | `OFMONDEB`/`OFMONCRE` | Monto real parece ambiguo o posiblemente invertido para debito originado por CFA. | Riesgo monetario si se habilita SOAP real. | `IntegrationMappingBootstrapper`, resolver funcional | Test de contrato monetario por operacion. |
| Proc_Contrapartidas | `OFDD` | Catalogo indica naturaleza debito/credito; seed usa `C`. | Naturaleza de operacion ambigua. | `IntegrationMappingBootstrapper` | Test de indicador por operacion. |
| Readiness | varios | Readiness acepta placeholders como mappings validos. | Falso `Ready`. | `IntegrationMappingReadinessService` | Test de rechazo de placeholders criticos. |
| Proc_Transacciones | `RTAACH`/`RTALOC` | Marcados requeridos en catalogo aunque parsers los tratan como respuesta. | Readiness o payload request pueden quedar conceptualmente incorrectos. | `IntegrationCatalogBootstrapper`, readiness | Test request/response direction. |
| UI mappings | varios | Backend tiene reglas activas con fuentes no visibles en UI. | Operador ve `Sin mapear` aunque backend cuenta mapping activo. | `mapping-sets-page.component.ts` | Spec de fuentes y estados. |

## 16. Brechas altas

| Servicio | Parametro | Problema | Impacto | Archivo probable | Test necesario |
|---|---|---|---|---|---|
| Proc_Transacciones | `TREG` | Placeholder `SEED`. | Payload invalido/debil. | `IntegrationMappingBootstrapper` | Test de constante oficial. |
| Proc_Transacciones | `CONV` | Placeholder `SEED`. | Convenio no homologado. | `IntegrationMappingBootstrapper` | Test de fuente/convenio. |
| Proc_Transacciones | `PROD` | Placeholder `SEED`. | Producto no homologado. | `IntegrationMappingBootstrapper` | Test de fuente/producto. |
| Proc_Transacciones | `IDCAMCOMPE` | Constante `1`. | Camara incorrecta en multi-camara. | `IntegrationMappingBootstrapper` | Test ACH/CENIT. |
| Proc_Contrapartidas | `OFCTA` | Usa DFI como cuenta. | Campo posiblemente incorrecto. | `IntegrationMappingBootstrapper`, resolver | Test de fuente de cuenta origen. |

## 17. Brechas medias y bajas

- Falta WSDL versionado para `minOccurs` y `nillable`.
- Falta distincion persistida entre `RequiredWsdl`, `RequiredFunctional` y `RequiredReadiness`.
- Textos de catalogo de `ANS*` en `Proc_Contrapartidas` usan "legado"; deberian decir reservado/contractual si son WSDL vigentes de ese servicio.
- Faltan pruebas de no sobrescritura de mappings manuales para `Proc_Transacciones` y `Proc_Contrapartidas`.
- Faltan pruebas Playwright que validen clasificacion visual de opcional/reservado.
- Falta validacion de paridad SQL Server 2025/PostgreSQL para seed de mappings.

## 18. Parametros listos

### RegistrarRespuestaTransaccion

Listos:

- `idCanal`
- `nombreCanal`
- `idTransaccion`
- `idEstado`
- `idTransaccionAxon`

Listos condicionales:

- `causal`
- `descripcionCausal`

### Proc_Transacciones

Listos o cercanos a listo con fuentes NACHA:

- `TIPTRAN`
- `BCORECEP`
- `BCOORIG`
- `NORIG`
- `DESTRAN`
- `FECEFEC`
- `NCTARECEP`
- `MONTO`
- `NRECEP`
- `IDRECEP`
- `INFPAG`
- `IDTRAN`
- `IDLOTE`

### Proc_Contrapartidas

Listos tecnicamente, con validacion funcional pendiente:

- `OFNIT`
- `OFEMP`
- `OFFECHEFEC`
- `OFIDARCH`
- `OFIDLOT`
- `OFIDTX`
- `OFIDEBAPLI`
- `OFIDCAMCOMPE`
- `OFDIRECCIONIP`

## 19. Parametros pendientes de definicion funcional

### Proc_Transacciones

- `TREG`
- `NCTAORIG`
- `DISCRE`
- `CONV`
- `PROD`
- `REGLOTE`
- `IREVER`
- `LIBRE`
- `IDCAMCOMPE`
- `DIRECCIONIP`
- `LIBRE1`
- `RTAACH`
- `RTALOC`

### Proc_Contrapartidas

- `OFCTA`
- `OFDD`
- `OFMONDEB`
- `OFMONCRE`
- `OFST`
- `OFIDREVER`
- `OFLIBRE`
- `OFLIBRE1`

## 20. Parametros opcionales/reservados

### Proc_Contrapartidas

Estos son validos solo donde correspondan a `Proc_Contrapartidas`:

- `ANSIDLOTE`
- `ANSST`
- `ANCLC`
- `ANSIDTX`
- `ANSIDREVER`

Deben permanecer como opcionales/reservados y no bloquear readiness mientras no exista regla funcional requerida.

### RegistrarRespuestaTransaccion

- `causal`
- `descripcionCausal`

Son condicionales y deben depender de respuesta/rechazo/causal homologada.

## 21. Parametros no-WSDL/inactivos

Para `RegistrarRespuestaTransaccion`, los siguientes parametros no deben existir como contrato vigente:

- `ANSIDLOTE`
- `ANSST`
- `ANCLC`
- `ANSIDTX`
- `ANSIDREVER`

Si aparecen en bases existentes, deben quedar inactivos o en historia archivada, nunca como mapping publicado vigente.

`PLValidarUsuarioBV` queda excluido y no debe catalogarse.

## 22. Plan de correccion por fases

### Fase A - Documentar matriz oficial

- Incorporar snapshot WSDL o tabla oficial con tipo, direccion, `minOccurs` y `nillable`.
- Crear matriz oficial versionada por servicio.
- Separar `Req. WSDL`, `Req. funcional` y `Req. readiness`.

### Fase B - Corregir parametros incorrectos o no-WSDL

- Mantener `RegistrarRespuestaTransaccion` como 7 parametros WSDL.
- Mantener `ANS*` solo en `Proc_Contrapartidas`.
- Revisar direccion real de `RTAACH`/`RTALOC`.
- No tocar logica monetaria hasta resolver preguntas bloqueantes.

### Fase C - Completar mappings base con fuentes existentes

- Reemplazar placeholders `SEED` en campos criticos.
- Usar solo fuentes ya existentes y verificables.
- Documentar constantes funcionales cuando sean oficiales.
- No inventar campos ni fuentes.

### Fase D - Clasificar opcionales/reservados

- Marcar `ANS*` de `Proc_Contrapartidas` como opcionales/reservados.
- Definir politica para `RTAACH`/`RTALOC`.
- Evitar que opcionales/reservados bloqueen readiness.

### Fase E - Ajustar readiness

- Readiness debe fallar si campo funcional requerido usa placeholder.
- Readiness debe diferenciar fuente funcional, constante homologada y fallback peligroso.
- Readiness no debe bloquear por opcionales/reservados correctamente clasificados.

### Fase F - Ajustar tests

- Agregar tests de matriz contractual completa.
- Agregar tests de readiness contra placeholders.
- Agregar tests de no-WSDL/inactivos por servicio.
- Agregar specs UI para estados y fuentes.

### Fase G - Validar SQL Server 2025 limpio

- Levantar con `DATABASE_APPLY_MIGRATIONS=true` despues de `down -v`.
- Ejecutar `/Maintenance/seed`.
- Consultar BD para metodos, parametros, mapping sets y reglas.

### Fase H - Validar UI /integraciones/mappings con Playwright

- Validar 27/22/7 parametros.
- Validar Registrar sin `ANS*`.
- Validar Contrapartidas con `ANS*` solo donde corresponde.
- Validar estados: mapeado, sin mapear, inactivo, opcional/reservado si se implementa.

### Fase I - Confirmar /integraciones/soap-settings separado y estable

- Validar endpoints y SOAP Actions sin cambios.
- Confirmar que la pantalla no se convierte en matriz de campos.
- Confirmar que guardar/cancelar/editar no muta mappings funcionales.

## 23. Archivos que probablemente se modificarian

Solo en fases futuras, no en este diagnostico:

- `src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationCatalogBootstrapper.cs`
- `src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingBootstrapper.cs`
- `src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingReadinessService.cs`
- `src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingValidationService.cs`
- `src/Cfa.ACHInterbank.Persistence/Integrations/Services/ProcContrapartidasFunctionalMappingResolver.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcTransaccionesRequestMapper.cs`
- `web/ach-interbank-ui/src/app/features/integrations/pages/mapping-sets-page.component.ts`
- `web/ach-interbank-ui/src/app/features/integrations/pages/mapping-sets-page.component.html`
- `web/ach-interbank-ui/src/app/features/integrations/pages/mapping-sets-page.component.spec.ts`
- `tests/Cfa.ACHInterbank.Tests/IntegrationBootstrapperTests.cs`
- `tests/Cfa.ACHInterbank.Tests/IntegrationMappingEndToEndTests.cs`
- `tests/Cfa.ACHInterbank.Tests/TransactionIntegrationReadinessGuaranteeTests.cs`
- `tests/Cfa.ACHInterbank.Tests/DifferentialPrenotificationResponseProcessorTests.cs`
- `tests/Cfa.ACHInterbank.Tests/IntegrationMappingTraceWriterTests.cs`
- documentacion de arquitectura/readiness.

## 24. Archivos que no se deben tocar

- Dockerfiles.
- `docker-compose.yml`.
- `docker-compose.sqlserver.yml`.
- OpenBao.
- NACHA-M engine.
- NACHA-M golden files.
- Logica monetaria SOAP real sin decision explicita.
- Endpoints publicos.
- `/integraciones/soap-settings`, salvo validacion visual/tecnica separada.
- Cualquier catalogacion funcional de `PLValidarUsuarioBV`.

## 25. Tests requeridos

Backend:

- Base limpia crea 3 metodos y conteos 27/22/7.
- `RegistrarRespuestaTransaccion` queda con 7 parametros WSDL.
- `RegistrarRespuestaTransaccion` no tiene `ANS*` vigentes.
- `Proc_Contrapartidas` conserva `ANS*` opcionales/reservados.
- `Proc_Transacciones` conserva 27 parametros.
- `PLValidarUsuarioBV` no se cataloga.
- Mapping publicado de Registrar tiene 7 reglas.
- Readiness falla con placeholders en campos funcionales criticos.
- Readiness no bloquea opcionales/reservados.
- Trace campo a campo para los tres servicios.
- No se pisan mappings manuales sin politica explicita.

Frontend:

- `/integraciones/mappings` muestra solo servicios funcionales.
- Registrar muestra 7 campos y no `ANS*`.
- Contrapartidas muestra `ANS*` solo donde corresponde.
- La UI diferencia mapeado, sin mapear, inactivo y opcional/reservado si se implementa.
- `Ver auditoria` conserva funcionalidad o se renombra como mejora UX sin eliminar historial.

Runtime/Docker:

- SQL Server 2025 limpio con migraciones habilitadas.
- `/Maintenance/seed` 200.
- API de mappings devuelve contrato vigente.
- Playwright valida `/integraciones/mappings`.
- Smoke de `/integraciones/soap-settings` separado.

## 26. Validaciones Docker/SQL Server 2025 requeridas

Comandos esperados para una validacion limpia:

```powershell
docker compose -f docker-compose.yml -f docker-compose.sqlserver.yml down -v --remove-orphans
$env:DATABASE_APPLY_MIGRATIONS="true"
docker compose -f docker-compose.yml -f docker-compose.sqlserver.yml build achinterbank-api achinterbank-spa
docker compose -f docker-compose.yml -f docker-compose.sqlserver.yml up -d
curl -i http://localhost:843/health/ready
curl -i -X POST http://localhost:843/Maintenance/seed
```

Validaciones BD/API:

- 3 metodos funcionales catalogados.
- `RegistrarRespuestaTransaccion` con 7 parametros WSDL activos.
- `RegistrarRespuestaTransaccion` sin `ANS*` activos.
- `Proc_Contrapartidas` con 22 parametros y `ANS*` donde corresponden.
- `Proc_Transacciones` con 27 parametros.
- `PLValidarUsuarioBV` ausente.
- MappingSet publicado de Registrar con 7 reglas.
- Readiness sin falsos positivos por placeholders.

## 27. Riesgos

- Falso readiness `OK` por placeholders.
- Ambiguedad monetaria en `OFMONDEB`/`OFMONCRE`.
- Indicador `OFDD` no homologado.
- Parametros de respuesta (`RTAACH`/`RTALOC`) tratados como request requerido.
- UI muestra `Sin mapear` para reglas activas con fuentes no consideradas visibles.
- Falta snapshot WSDL versionado.
- Riesgo de pisar configuracion manual si no se define politica por servicio.
- Confusion por textos de "legado" en campos que son validos para `Proc_Contrapartidas`.

## 28. Preguntas bloqueantes

1. Donde queda el WSDL oficial versionado para extraer `minOccurs`, `nillable`, tipos y direccion?
2. En `Proc_Contrapartidas`, el monto real debe ir en `OFMONDEB`, `OFMONCRE` o ambos segun naturaleza?
3. `OFDD` debe ser `D`, `C` u otro codigo homologado?
4. `RTAACH` y `RTALOC` son parametros request reales o campos de response?
5. Que fuente oficial alimenta `CONV`, `PROD`, `DIRECCIONIP`, `LIBRE` y `LIBRE1`?
6. La UI debe mostrar como fuentes validas `Transaction`, `Cycle`, `ClearingHouse` y `Constant`, o solo NACHA/DifferentialResponse?
7. Cual es la politica oficial para mappings manuales publicados incompatibles en `Proc_Transacciones` y `Proc_Contrapartidas`?
8. Las constantes funcionales deben vivir como seed, configuracion gobernada o catalogo parametrizable?

## 29. Criterios de aceptacion

- Base limpia + SQL Server 2025 + `DATABASE_APPLY_MIGRATIONS=true` + `/Maintenance/seed` genera los 3 servicios correctamente.
- `RegistrarRespuestaTransaccion` queda con exactamente 7 parametros WSDL.
- `RegistrarRespuestaTransaccion` no tiene `ANS*` vigentes.
- `Proc_Contrapartidas` conserva `ANS*` donde corresponde.
- `Proc_Transacciones` conserva 27 parametros.
- `PLValidarUsuarioBV` no se cataloga.
- `/integraciones/mappings` refleja contrato vigente.
- `/integraciones/soap-settings` sigue separado.
- Readiness no marca `OK` con mappings requeridos faltantes o placeholders criticos.
- Readiness no bloquea por opcionales/reservados correctamente clasificados.
- Tests backend verdes.
- Playwright valida mappings.
- No se inventan mappings sin fuente funcional.
- No se pisa configuracion manual sin politica explicita.

## 30. Veredicto

Diagnostico documentado. No cerrado para implementacion.

El sistema tiene la base correcta para Registrar y para el catalogo de tres servicios, pero requiere una fase posterior de correccion funcional controlada para completar mappings monetarios, distinguir required WSDL vs required funcional/readiness, eliminar placeholders criticos y hacer que la UI represente fielmente lo que readiness evalua.
