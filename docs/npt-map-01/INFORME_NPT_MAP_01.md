# NPT-MAP-01 — Cierre focal de seguridad y coherencia SOAP Mappings

**Fecha:** 2026-11-16  
**Base exclusiva de código:** `ACHInterbank_SOAP_MAPPING_FINAL.zip`.  
**Operaciones:** `Proc_Transacciones`, `Proc_Contrapartidas`, `RegistrarRespuestaTransaccion`.  
**Dictamen:** **NO-GO Live — cierre defensivo implementado, homologación y .NET 10 pendientes**.

## Resultado implementado

1. **`Proc_Contrapartidas`:** `OFEMP`, `OFIDARCH` y `OFDIRECCIONIP` fallan en readiness y en el resolvedor con `PROC_CONTRAPARTIDAS_SOURCE_NOT_HOMOLOGATED`, independientemente de ruta, constante o regla personalizada. Ni `clearinghouse.code`, ni `batch.id`, ni `0.0.0.0` constituyen fuente acreditada. Los mappings personalizados permanecen almacenados, sin reescritura silenciosa. Los cinco parámetros de identidad protegidos tampoco pueden desaparecer del catálogo activo sin provocar NO-GO.
2. **Cuenta y correlación salientes:** `OFCTA` solo admite `transaction.sourceAccountNumber`, y `OFIDTX` solo `transaction.reference`, como fuentes `Transaction`. Se rechazan `FixedValue`, `DefaultValue`, expresiones, transformaciones, formato y fuente inexistente o divergente. El resultado final debe ser idéntico, carácter por carácter, a los datos persistidos de `AchTransaction`.
3. **`Proc_Transacciones`:** se conservan `NORIG ← BatchHeaders.CompanyName`, `IDORIG ← BatchHeaders.CompanyId`, `NCTARECEP ← EntryDetails.AccountNumber`. Readiness y runtime impiden intercambio de cuenta receptora/originadora, constantes y transformaciones de estas fuentes físicas. `NCTAORIG` sigue únicamente ligado a `Transactions.SourceAccountNumber`, con bloqueo si se intenta suplantarlo por `NCTARECEP` y sin crear un origen artificial.
4. **Barrera de transporte:** `WscfaachSoapClient` valida XML/XSD de `Proc_Contrapartidas` y deniega **incondicionalmente antes de `HttpClient`/POST** esa operación mientras las tres fuentes funcionales no estén homologadas. Cierra el bypass por sobrecarga de XML directo. No hay vía Live habilitada por esta entrega.
5. **`RegistrarRespuestaTransaccion`:** se conservan sus siete mappings diferenciales y la correlación, estado y causal por cámara. **Sin cambios de código.** `PLValidarUsuarioBV` no se modificó funcionalmente. No se tocaron migraciones, ACH Colombia F5, controles de fondos, idempotencia, autorización ni interfaces de operación bancaria.

## Archivos de código modificados

- `src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingReadinessService.cs`
- `src/Cfa.ACHInterbank.Persistence/Integrations/Services/ProcContrapartidasFunctionalMappingResolver.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcTransaccionesRequestMapper.cs`
- `src/Cfa.ACHInterbank.External/Connections/WscfaachSoapClient.cs` (solo barrera de `Proc_Contrapartidas`, sin modificar el método `PLValidarUsuarioBV`)

**Pruebas C# modificadas:**

- `tests/Cfa.ACHInterbank.Tests/TransactionIntegrationReadinessGuaranteeTests.cs`
- `tests/Cfa.ACHInterbank.Tests/IntegrationMappingEndToEndTests.cs`
- `tests/Cfa.ACHInterbank.Tests/WscfaachSoapClientCharacterizationTests.cs`
- `tests/Cfa.ACHInterbank.Tests/SoapEnvelopeJournalTests.cs`
- `tests/Cfa.ACHInterbank.Tests/OutgoingTransactionMonitoringMultiDbTests.cs`

**Pruebas C# nuevas:** `tests/Cfa.ACHInterbank.Tests/NptMap01MappingSafetyTests.cs`. Casos negativos: rutas no acreditadas y alternativas, `FixedValue`, `DefaultValue`, `TransformationCode`, `FormatMask`, `ConditionExpression`, fuente inexistente, cuenta o referencia manipulada, swap de origen/receptor, XML/XSD inválido y cero HTTP POST; incluye preservación de reglas persistidas.

## Verificación ejecutada — comprobado

| Verificación | Resultado real | Naturaleza y evidencia |
|---|---|---|
| Conteo y esquema de los 66 elementos de request/response | **51/51 PASS** | Python + `lxml`, comprobaciones estáticas y XML sintético; `docs/npt-map-01/evidencias/01_soap_focal_offline.log` |
| Contratos WSDL/XSD originales y muestras XML positivas/negativas | **38/38 PASS** | Python + `lxml`; `02_wsdl_xsd_offline.log` |
| Guardias de fuente previos y regresión estática | **33/33 PASS** | Inspección de código automatizada; `03_regresion_estatica.log` |
| NPT-MAP-01 (identidades, barrera pre-POST, XML inválido, catálogos y matriz) | **32/32 PASS** | Código fuente + `lxml` offline; `04_npt_map01_offline.log`. **No ejecuta código C# ni POST HTTP.** |
| Integridad de solución | **PASS: 7/7 proyectos** | Grafo `.sln` y referencias `.csproj`; `05_grafo_solucion.log` |
| Comparación WSDL/XSD | **7/7 SHA-256 idénticos** | Originales `WSCFAACH.zip` y `WSAxonRespuestaTransacciones.zip` vs recursos embebidos; `CONTRATOS_SHA256.json` |

**No se ejecutó por limitación técnica** (`dotnet` no instalado; código 127): `dotnet --info`, `dotnet build ACHInterbank.sln --configuration Release --no-restore`, xUnit focal y regresión general `dotnet test ACHInterbank.sln --configuration Release`. Evidencias `06_dotnet_info.log` a `09_dotnet_regresion_general.log`. **Los tests C# están añadidos, pero NO se califican como pasados; no existe garantía de compilación hasta ejecutarlos.** Tampoco se ejecutaron SOAP Live, UAT ni operaciones bancarias.

Para cerrar en un entorno .NET 10 con paquetes restaurables, desde `ACHInterbank_ajustado/` ejecutar:

```bash
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln -c Release --no-restore
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build --filter 'FullyQualifiedName~NptMap01MappingSafetyTests|FullyQualifiedName~TransactionIntegrationReadinessGuaranteeTests|FullyQualifiedName~IntegrationMappingEndToEndTests|FullyQualifiedName~WscfaachSoapClientCharacterizationTests|FullyQualifiedName~SoapEnvelopeJournalTests'
dotnet test ACHInterbank.sln -c Release --no-build
```

## Trazabilidad y fuente documental

- **Matriz de 66 elementos:** `docs/npt-map-01/MATRIZ_TRAZABILIDAD_66_CAMPOS.md` (27 + 2 + 22 + 5 + 7 + 3), extraída de los contratos originales. Distingue fuentes evidenciadas, fuente faltante, solicitud y respuesta y los `minOccurs=0` del XSD, **sin declararlos todos obligatorios**.
- **Limitación de referencia:** el archivo literal `BARRIDO_INTEGRAL_SOAP_66_CAMPOS_ACH_CENIT_2026-11-16.md` no se encontró en el ZIP base ni entre los archivos accesibles. No se asume haberlo leído; el inventario de 66 elementos se reconstruyó de los WSDL/XSD originales y se contrastó con los seeds/código. La auditoría focal accesible `AUDITORIA_SOAP_MAPPING_FINAL_2026-11-16.md` se consultó para los riesgos H01–H04.
- La matriz documenta procedencia **técnica del seed**, no prueba de homologación de datos reales.

## Pendientes técnicos — NO-GO

1. Ejecutar `dotnet restore`, compilación Release .NET 10, xUnit focal y regresión general en un entorno con SDK y dependencias; corregir cualquier fallo observado y repetir evidencias.
2. Ejecutar de forma local/aislada (sin conexión bancaria) las pruebas HTTP negativas para verificar el bloqueo **real** antes de POST. La posición del bloqueo en el código está comprobada estáticamente, no por ejecución C# en este entorno.
3. Contrastar el barrido literal de 66 campos si se facilita posteriormente; no hay extrapolación de equivalencias ausentes.

## Dependencias de homologación — NO-GO funcional

| Campo | Evidencia faltante (no reemplazar por conjeturas) |
|---|---|
| `Proc_Contrapartidas.OFEMP` | Identidad funcional y fuente autorizada del código de empresa. `clearinghouse.code` no la acredita. |
| `Proc_Contrapartidas.OFIDARCH` | Identidad y origen persistido del archivo saliente. `batch.id` no equivale a identificador de archivo. |
| `Proc_Contrapartidas.OFDIRECCIONIP` | Procedencia y regla autorizada de la dirección IP real; `0.0.0.0` no se acepta. |
| `Proc_Transacciones.NCTAORIG` cuando el archivo entrante no permite acreditar origen | Cuenta originadora independiente de `EntryDetails.AccountNumber`; no inferir ni copiar la cuenta receptora. |

**Criterio de seguridad:** no se puede obtener readiness efectivo ni enviar `Proc_Contrapartidas` mientras sus campos de identidad carezcan de homologación. Cualquier reactivación posterior exige fuente aprobada, cambios explícitos y repetición completa de pruebas. **Estado final de este entregable: NO-GO Live.**
