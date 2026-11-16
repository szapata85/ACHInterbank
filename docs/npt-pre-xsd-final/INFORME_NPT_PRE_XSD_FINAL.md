# Informe técnico — NPT_PRE_XSD_FINAL

**Incremento:** 2026-11-15. **Base:** `ACHInterbank_HOTFIX_NPT_CFA_LATE_01_2026-11-15.zip`; SHA-256 `07da2c52c5200f003a8b95dc3de20dc6d878fea48f2982c89d0289accbdd3d40`. **Decisión:** NO-GO SOAP Live / producción. No UAT. No certificación contractual. No migraciones nuevas. No dependencia nueva.

## Alcance aplicado directamente

| Hallazgo | Estado verificable | Fuente cambiada | Implementación y límite |
|---|---|---|---|
| **NPT-01** ACHCOL file-based | **PARCIALMENTE CORREGIDO** | `IncomingNachaFileBasedPreparationStage.PrepareAsync` | F4.2 exige `AchCycleId`, `AchTransactionId` y `IncomingNachaTransactionLinkId` nulos, cabecera física de la misma ingesta y cámara; retiene `Blocked`, `AttemptCount=0`, sin transporte, incluso Live. La política de elegibilidad F4.1 y el inventario F5.1 anteriores siguen activos. **F5 financiero no se habilita**: no hay contrato CFA de mapping/identidad/seguridad homologado para esta ruta sin ciclo. |
| **NPT-02** diferenciales físicos | **BLOQUEADO POR DEPENDENCIA EXTERNA** para procesamiento funcional; *compuerta técnica endurecida* | `IncomingNachaIngestionAppService.IsDifferentialCandidate`, selección/compuerta pre-parser | La adenda `7/99` se identifica aunque la longitud sea inválida; el selector sigue requiriendo `RequireHomologated`. Incluso un perfil devuelto como seleccionado no ejecuta parser, post-parse, correlación ni SOAP sin adaptador certificado; deja evento, `Bloqueado`, `NoEjecutado`, código `DIFFERENTIAL_TABLE_DRIVEN_PARSER_NOT_ENABLED`. No se interpretan fixtures sintéticos como evidencia oficial. **Excepción conservada:** CENIT devolución de devolución oficial F07 (`CenitReturnOfReturn2026Layout`) no se bloquea por usar adenda 99; **todas** sus adendas `99` deben ser de causal R60–R74 y el perfil sigue validando sus campos. Un archivo mixto/ambiguo permanece bloqueado. |
| **NPT-05** `NCTAORIG` | **PARCIALMENTE CORREGIDO** | `IntegrationMappingBootstrapper.ProcTransaccionesSourcePathFor`, `IntegrationMappingReadinessService.ValidateProcTransaccionesNctaOrigRule` | El *mapping semilla candidato* utiliza `transaction.sourceAccountNumber`, no `entrydetails.accountnumber` (cuenta receptora). Readiness exige `SourceKind=Transaction`, path exacto y ausencia de constantes/fallback, **pero sigue fallando** con `PROC_TRANSACCIONES_NCTAORIG_CONTRACT_NOT_HOMOLOGATED` hasta contar con procedencia y semántica CFA aprobadas. El código de readiness general permanece `PROC_TRANSACCIONES_NCTAORIG_SOURCE_INVALID` por compatibilidad; el guard `EnsureLiveOriginAccountHomologated` sigue impidiendo enviar valor no homologado. Los mappings de usuario no se alteran por el bootstrap. |
| **NPT-11** correlación de prenotificaciones | **PARCIALMENTE CORREGIDO** | `DifferentialPrenotificationResponseProcessor.ResolvePrenotificationMatchAsync` | Exige cámara/ref; la búsqueda física de traza sigue limitada a cámara; verifica vinculación final original, pertenencia al archivo, ingesta, cámara y ciclo cuando están informados, así como identidad de addenda y referencia original según `LinkType`. No selecciona un candidato arbitrario ante duplicados; mantiene revisión manual. No infiere fecha o identificador de archivo desde el comando `ProcesarRespuestaAchCommand`, porque **ese contrato no proporciona campos homologados para ello**. |

### Límites de afirmación

No se declara NPT-01/02/05 como funcionalmente homologado. NPT-11 impide correlaciones cruzadas con los datos disponibles, pero no puede resolver automáticamente dos referencias idénticas que no incluyan señal oficial de archivo/fecha. No se habilita Live ni se fabrican cuentas, ciclos, reglas XSD, identificadores ni formatos. El parser de una devolución CENIT ya soportada no se sustituye por la ruta de prenotificación física.

## Contratos efectivamente examinados

WSDL externos adjuntos, **no copiados en el ZIP**: `WSCFAACH` define `Proc_Transacciones` con SOAPAction `http://tempuri.org/IWSCFAACH/Proc_Transacciones` e importa `http://esparta/WSCFAACH/WSCFAACH.svc?xsd=xsd0` y `...?xsd=xsd1`; `WSAxonRespuestaTransacciones` define `RegistrarRespuestaTransaccion` e importa `http://esparta/WSCFAACH/WSAxonRespuestaTransacciones.svc?xsd=xsd0`, `...?xsd=xsd1` y `...?xsd=xsd2`. Los WSDL disponibles **no definen** directamente campos ni tipos de `NCTAORIG` ni layout físico NACHA diferencial. Manual CENIT NACHA-M 07/05/2026, §3 (106 caracteres/registro), §7.1.2 y Anexo 1.7 para ROR, ya documentados en el repositorio. ACH Colombia V35 04/2026 usado como contexto; no se crea un perfil diferencial sin un vector homologado.

## Pruebas automáticas nuevas o actualizadas (escritas, NO ejecutadas en .NET)

- `IncomingNachaFileBasedOrchestrationF42Tests.Npt01_ForgedFileBasedQueueContext_StaysBlockedWithoutSoap`: 3 casos para link, transacción y cabecera ajena; se espera `Blocked`, cero intentos y cero llamadas SOAP.
- `IncomingNachaIngestionAppServiceTests.Npt02_DifferentialWithSelectedProfile_RemainsBlockedWithoutPhysicalAdapter`: selección positiva simulada, parser nunca llamado, auditoría y ausencia de transición; `Npt02_MalformedAddenda99...` y `Npt02_OfficialCenitReturnOfReturn...` verifican discriminadores y no-regresión CENIT.
- `IntegrationBootstrapperTests.ProcTransacciones_BaseMapping...` comprueba origen semilla, sin defaults; `TransactionIntegrationReadinessGuaranteeTests.ProcTransaccionesReadiness...` comprueba bloqueo contractual aun con `transaction.sourceAccountNumber`. `ProcTransaccionesOriginAccountSafetyTests` conserva guard Live existente.
- `DifferentialPrenotificationResponseProcessorTests.Npt11_InconsistentOriginalLink_NeverMatchesOrMovesMoney`: 3 casos SQLite EF Core escritos (ingesta ajena, adenda ajena, traza original distinta); verifica Pending, sin estado monetario ni `Matched`. Permanecen las pruebas existentes de archivos duplicados/ausencia de enlace.
- Las pruebas previamente escritas NPT-03/04/06/07/08/09/10/12/13 y NPT-CFA-LATE-01 no se cambiaron.

## Ejecución real y evidencia

| Verificación | Resultado real | Archivo |
|---|---|---|
| Focal fuente + metadatos XML de los **dos WSDL externos** | **31/31 PASS** — sólo Python fuente + XML ET | `evidencias/npt_pre_xsd_final_verify.log` |
| Focal NPT-CFA-LATE-01 fuente/modelo SQLite | **15/15 PASS** | `evidencias/npt_cfa_late_static_sqlite_check.log` |
| Focal NPT siete puntos, fuente/modelo SQLite | **26/26 PASS** | `evidencias/npt_focal_sqlite_source_guard.log` |
| E2E-01/02 fuente/modelo | **35/35 PASS** | `evidencias/e2e01_e2e02_source_static_check.log` |
| Auditoría E2E final fuente/modelo | **31/31 PASS** | `evidencias/aud_e2e_final_static_and_sqlite_check.log` |
| RES-H02 fuente/modelo | **27/27 PASS** | `evidencias/res_h02_soap_preflight_sqlite_check.log` |
| CENIT F07 fuente/fixtures | **24/24 PASS** | `evidencias/cenit_f07_regression_check.log` |
| NPT histórico general | **41/42**, salida **1**: assert heredada sobre cancelación Axon/CommitAsync; no fue alterado | `evidencias/npt_static_guard.log` |
| `dotnet --info`, `restore`, `build`, `test` | **NO EJECUTADOS**: falta SDK, cada intento devuelve **127** | `evidencias/*_dotnet.log` |
| xUnit, EF Core en SQL Server/PostgreSQL, SOAP instrumentado, UAT | **NO EJECUTADOS**, falta infraestructura/SDK; UAT reservada al final | Esta matriz |

**Reproducir desde raíz `ACHInterbank_ajustado/`:**

```bash
python scripts/test/npt_pre_xsd_final_verify.py "/ruta/WSCFAACH.wsdl.xml" "/ruta/WSAxonRespuestaTransacciones.wsdl.xml"
python scripts/test/npt_cfa_late_static_sqlite_check.py
python scripts/test/npt_focal_sqlite_source_guard.py
python scripts/test/npt_static_guard.py
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln --no-restore -c Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter "FullyQualifiedName~Npt01_ForgedFileBasedQueueContext|FullyQualifiedName~Npt02_|FullyQualifiedName~Npt11_|FullyQualifiedName~ProcTransaccionesReadiness|FullyQualifiedName~Live_BlocksUnhomologatedOriginAccount"
dotnet test ACHInterbank.sln -c Release
```

Repetir las pruebas EF Core en servidores de integración reales **SQL Server y PostgreSQL** antes del cierre técnico; los `Mock<IWscfaachSoapClient>` prueban *ausencia de invocación*, no integración bancaria. No se ejecutaron pruebas del gateway real ni de compensación.

## Autoverificación adversarial

- **Segundo débito:** sin liberación de F5, y claim de worker sigue filtrando `CycleBound`; el hotfix CFA late conserva cuarentena. No se añadió ruta a SOAP.
- **Bypass al seleccionar perfil:** descubierto y bloqueado ANTES de parser para diferencial sin adaptador. CENIT ROR F07 preservado mediante discriminador por causal `R60..R74` ya publicada; otras adendas 99 no se reinterpretan automáticamente.
- **Cuenta origen falsa:** bootstrap no usa `EntryDetail.AccountNumber`; aunque se configure una cuenta origen candidata, readiness y guard Live continúan fail-closed.
- **Correlación falsa:** vínculo ambiguo, adenda ajena, ciclo distinto, ingesta/cámara cruzada y traza no correspondiente quedan a revisión. La fecha no se adivina.
- **NPT previos, migraciones:** se conserva la solución de 7 proyectos, scripts históricos y migraciones existentes; no se crearon columnas/índices nuevos. Riesgo no certificado: compilación y pruebas de concurrencia EF Core no pudieron ejecutarse.

## Dependencias externas mínimas inevitables

| Artefacto oficial necesario | Responsable | Validación pendiente |
|---|---|---|
| `WSCFAACH` XSD `xsd0`, `xsd1` + ficha homologada de la semántica `NCTAORIG`, procedencia de cuenta origen, contrato F5 y autorización de operaciones ACHCOL file-based sin ciclo | Propietario del servicio CFA / banco integrador y entidad participante (con ACH Colombia para la ruta operativa) | XSD/semántica, identidad, mapping F5, autorización de transporte, ciclo operativo file-based y pruebas negativas de Live |
| Layouts/perfiles/vectores **oficiales** de devoluciones físicas de prenotificación por cámara, incluyendo versión, vigencia, correlación original y casos positivos/negativos | ACH Colombia para ACHCOL; Banco de la República/CENIT para CENIT | Publicar perfiles únicamente homologados y desarrollar/validar adaptador físico no monetario, de archivo a `AchResponse`/Axon |

Las referencias de archivo/fecha faltantes en el comando diferencial no se consideran permiso para correlacionar: **la revisión manual ya cubre este caso de forma segura**. Si se exige correlación automática de referencias duplicadas, el proveedor deberá suministrar un campo de origen homologado; no se presupone que tal campo exista.

**Cierre:** avance de código verificable en NPT-01/02/05/11, sin afirmación de homologación ni compilación. **NO-GO Live.**
