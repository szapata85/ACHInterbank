# Informe técnico — H-01/H-02/H-03 | NPT SOAP XSD | 2026-11-16

**Base íntegra:** `ACHInterbank_NPT_SOAP_XSD_FINAL_01.zip` · SHA-256 `1434255a811427a06cc5b85af7d86bdf4da606b3fdfc63383b87c143dacfa34d`. Incremento únicamente focal; no cambia WSDL/XSD, modelo de datos, migraciones, librerías, endpoints ni políticas de habilitación Live. **NO-GO SOAP Live y UAT NO EJECUTADA.**

## Alcance, implementación y clasificación

| Hallazgo | Cambios en fuente | Estado comprobable |
|---|---|---|
| H-01 — transporte sin XSD final | `OfficialSoapContracts.ValidateTransportRequest` valida XML con lector DTD-off, resolver null, límite de tamaño, SOAP 1.1, operación única y `ValidateElement` contra XSD local. `WscfaachSoapClient.SendAsync` lo invoca para `Proc_Transacciones`; `WsAxonRespuestaTransaccionesSoapClient.SendAsync` para `RegistrarRespuestaTransaccion`. Se ubica antes de construir `HttpRequestMessage` y antes de la primera transmisión, para las sobrecargas XML y diccionario y envíos paralelos. Las otras operaciones CFA no se alteran. | **Corregido en fuente; pendiente compilación/xUnit e integración .NET.** |
| H-02 — guard Live `NCTAORIG` incompleto | `IncomingNachaExternalDispatchStage.EnsureLiveOriginAccountHomologated` ahora niega cualquier payload Live mientras no esté homologada la cuenta **del originador**; XSD permite omitirla o enviarla `xsi:nil`, pero esos casos tampoco abren Live. Se conserva `transaction.sourceAccountNumber` como origen candidato, NO como procedencia homologada. | **Corregido en fuente; no habilitado Live.** |
| H-03 — prueba readiness contradictoria | Pruebas de `NachaDesagregadoIntegrationMappingTests` ya esperan `PROC_TRANSACCIONES_NCTAORIG_SOURCE_INVALID` y causa `PROC_TRANSACCIONES_NCTAORIG_CONTRACT_NOT_HOMOLOGATED`, en lugar de readiness positivo. Los tests que vacían tránsito conservan el veto de homologación prioritario. Pruebas de caracterización Axon/CFA y journal que usaban XML no conforme se actualizan para usar cuerpos XSD válidos en casos de envío. | **Expectativa corregida en fuente; ejecución xUnit pendiente.** |

### Evidencia y límites de autorización

- Los cinco XSD y dos WSDL de la entrega previa permanecen **bit a bit iguales**; el verificador de snapshots confirmó hashes originales.
- `NCTAORIG` es el número de cuenta **del originador**, diferente de la cuenta receptora; su tipo `xs:string`, `minOccurs=0`, `nillable=true` solo describe el contrato XML.
- El veto de procedencia bancaria de `NCTAORIG`, los controles de readiness, el modo Live parametrizado, endpoint, SOAPAction y no-reintento HTTP permanecen operativos en código. La nueva comprobación XSD no confiere homologación.
- No se cambiaron otros NPT, CENIT, ACH Colombia, E2E o migraciones; tampoco se ejecutaron pagos ni UAT.

## Pruebas realmente ejecutadas

| Verificación | Resultado | Naturaleza |
|---|---|---|
| Validador WSDL/XSD original | **38/38**, salida 0 | `lxml`, offline, no C# |
| Guardas estáticas WSDL/source anterior | **33/33**, salida 0 | Python estático |
| Verificador focal H-01/H-02/H-03 | **21/21**, salida 0 | XSD `lxml` + inspección de fuente; **no invoca clientes .NET** |
| Siete regresiones históricas: contadores, E2E, H01/H02, auditoría E2E, NPT focal, PRE-XSD, CFA tardía | **7/7 scripts** salida 0 | fuente / modelos SQLite Python; no integración .NET |
| Verificador NPT general heredado | **40/42**, salida 1 | Discrepancias previas SOAP 1.2 y Axon `CommitAsync`, no forzado |
| `dotnet --info`, `restore`, `build`, `test` | **Intentados; todos salida 127** | SDK .NET ausente; no compilación ni xUnit |
| HTTP con contador, SQL Server/PostgreSQL, SOAP bancario, UAT | **NO EJECUTADOS** | Requieren SDK / infraestructura / homologación |

## Pruebas xUnit escritas pero NO ejecutadas

- H-01: XML directo CFA y Axon inválido, namespace erróneo, duplicados, tipos incompatibles, operación incorrecta, DTD/XXE y SOAP 1.2; antes del HTTP se comprueba `server.Requests` vacío.
- H-01: Sobrecargas de parámetros inválidos y transportes locales válidos con cuerpo oficial; ausencia de reintentos monetarios ya implementada y preservada.
- H-02: Cuenta origen informada, ausente, vacía, `xsi:nil`, objeto XML vacío: ninguna libera Live.
- H-03: Readiness y effective settings siguen bloqueados hasta homologación; prueba de fallo priorizado.

## Comandos reproducibles en equipo con .NET 10

```bash
cd ACHInterbank_ajustado
dotnet --info
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln -c Release --no-restore
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build --filter "FullyQualifiedName~WscfaachSoapClientCharacterizationTests|FullyQualifiedName~WsAxonRespuestaTransaccionesSoapClientCharacterizationTests|FullyQualifiedName~ProcTransaccionesOriginAccountSafetyTests|FullyQualifiedName~NachaDesagregadoIntegrationMappingTests|FullyQualifiedName~SoapEnvelopeJournalTests|FullyQualifiedName~OfficialSoapXsdContractTests"
python docs/npt-soap-xsd-final-01/verify_wsdl_xsd_offline.py
python docs/npt-transport-xsd-h010203/verify_h010203_offline.py
```

Registrar por separado el resultado EF Core/SQL Server, PostgreSQL, simulador HTTP real y compilación; no interpretar los 21/21 como pruebas C#.

## Revisión adversarial

1. Un cliente público CFA `Proc_Transacciones` o Axon puede ser llamado con XML directo: el payload se comprueba en el último límite anterior al envío. Los métodos paralelos reutilizan el mismo camino.
2. XML con DTD, namespace equivocado, más de una operación, orden inválido, tipos incorrectos o SOAP 1.2 no satisface el contrato local SOAP 1.1.
3. Un campo `NCTAORIG` ausente/nil es **válido por XSD** pero **no autoriza Live**; no debe reemplazarse por `NCTARECEP`.
4. Las excepciones de validación ocurren antes de instanciar `HttpRequestMessage` y antes de `SendAsync`; no se interpretan como resultados bancarios inciertos ni producen reintentos.
5. `Proc_Contrapartidas` y `PLValidarUsuarioBV` quedan fuera de la modificación de contrato, manteniendo compatibilidad.
6. Sin SDK no se puede descartar un defecto de compilación o una incompatibilidad runtime; mantener estado NO-GO hasta ejecutar xUnit y contadores físicos.

## Dependencia externa inevitable

Homologación por la entidad bancaria/operador CFA de la **procedencia y semántica de la cuenta del originador `NCTAORIG`**; evidencia de autorización operativa F5 (ACH Colombia) si se pretende habilitar dicha ruta. Los XSD ya están incluidos y **no** figuran como faltantes. El resto de pruebas pendientes requiere infraestructura técnica, no un artefacto contractual adicional de H-01/H-03.
