# CIERRE NPT-MAP-01.1 — RA-01 y RA-02

**Fecha:** 2026-11-16  
**Paquete base:** `ACHInterbank_NPT_MAP_01_CORREGIDO.zip`  
**Fuente de hallazgos:** `REAUDITORIA_NPT_MAP_01_2026-11-16.md`  
**Alcance:** únicamente RA-01 y RA-02. **Estado: implementación estática; NO-GO SOAP Live.**

## RA-01 — Readiness: clasificación del código público

**Archivo:** `src/Cfa.ACHInterbank.Persistence/Integrations/Services/IntegrationMappingReadinessService.cs`

**Métodos:** `EvaluateAsync` y nuevo helper privado `ResolveFunctionalErrorCode`.

- Cuando existen errores `PROC_CONTRAPARTIDAS_PROTECTED_SOURCE_INVALID` por `OFCTA`/`OFIDTX`, `readiness.Code` devuelve ese mismo código. **Tiene prioridad sobre otros errores concurrentes**, pues los tres identificadores no homologados pueden estar bloqueados simultáneamente.
- Sin una infracción protegida y con error `PROC_CONTRAPARTIDAS_SOURCE_NOT_HOMOLOGATED`, `readiness.Code` devuelve ese código específico.
- Otros errores funcionales sin esos dos prefijos siguen el tratamiento anterior `FUNCTIONAL_MAPPING_PLACEHOLDER`. No se modifica el bloqueo ni el contenido de `Errors`, `IsReady=false` ni `CanBuildPayload=false`.
- No se cambia `ProcContrapartidasFunctionalMappingResolver`, la barrera HTTP ni mappings persistidos. `OFEMP`, `OFIDARCH` y `OFDIRECCIONIP` **siguen sin fuente acreditada**; no se sustituye `clearinghouse.code`, `batch.id`, `0.0.0.0` por otra ficción.

**Evidencia estática:** constantes y discriminador en `IntegrationMappingReadinessService.cs`; evaluación de fuente original en `ProcContrapartidasFunctionalMappingResolver.AssessProtectedRule`; código de bloqueo pre-POST existente en `WscfaachSoapClient`. La semántica se deduce del flujo de código; **no se ejecutó C#**.

## RA-02 — NCTAORIG: integridad de metadatos

**Archivo:** `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcTransaccionesRequestMapper.cs`

**Métodos:** `ResolveLegacyAsync`, `ResolveContractDrivenAsync` y `EnsureNctaOrigSource`.

- `EnsureNctaOrigSource` recibe adicionalmente `TransformationCode`, `FormatMask` y `ConditionExpression`; cualquiera que contenga texto no vacío se rechaza con `PROC_TRANSACCIONES_NCTAORIG_SOURCE_UNVERIFIED`, **antes incluso del retorno por cuenta opcional ausente**.
- **Legacy:** transmite directamente esos tres metadatos desde la regla publicada.
- **Contract-driven:** toma `TransformationCode` y `FormatMask` del binding; consulta el `ConditionExpression` de la regla publicada ganadora porque el modelo de binding no contiene esa propiedad. El motor contractual también rechaza una condición arbitraria previamente (`MAPPING_CONDITION_NOT_SUPPORTED`).
- Se mantiene `NCTAORIG ← Transactions.SourceAccountNumber` con comparación exacta. **Nunca** se usa `EntryDetails.AccountNumber` como origen; ese campo continúa siendo `NCTARECEP`.
- La ausencia de cuenta origen acreditada puede producir **omisión de `NCTAORIG`** en SOAP si se mantiene la regla segura y el valor no existe. El guard Live existente sigue rechazando un `NCTAORIG` ausente o vacío para operaciones Live.

**Evidencia estática:** llamadas al guard en ambas rutas, tupla de metadatos y comparación `StringComparison.Ordinal` en `ProcTransaccionesRequestMapper.cs`; condición contractual en `IntegrationExecutionContractResolver.cs`; guard Live en `IncomingNachaPostProcessingPipeline.cs`.

## Contratos originales — inspección estática

`WSCFAACH.zip`, `WSCFAACH/WSCFAACH/xsd0.xml`: 

| Campo | Contrato original | Criterio de datos y autorización |
|---|---|---|
| `NCTAORIG` | `minOccurs="0"`, `nillable="true"`, `xs:string` | Puede omitirse; **no satisface** el guard Live bancario si falta la fuente real. |
| `OFEMP` | `minOccurs="0"`, `nillable="true"`, `xs:string` | Cadena vacía/omisión técnicamente representable; **no implica homologación**. |
| `OFIDARCH` | `minOccurs="0"`, `xs:int` | No enviar `""` como entero: el elemento puede omitirse. La falta de fuente **bloquea la operación**. |
| `OFDIRECCIONIP` | `minOccurs="0"`, `nillable="true"`, `xs:string` | Vacío/omisión técnicamente representable; nunca sustituir por `0.0.0.0`. |

**No se modificaron WSDL/XSD.** Las operaciones monetarias siguen gobernadas por validaciones funcionales adicionales al XSD.

## Archivos C# intervenidos y pruebas actualizadas, sin ejecución

| Archivo | Cambio |
|---|---|
| `IntegrationMappingReadinessService.cs` | Determinación de código público específico en errores funcionales. |
| `ProcTransaccionesRequestMapper.cs` | Rechazo de tres metadatos de `NCTAORIG` en legacy y contract-driven. |
| `TransactionIntegrationReadinessGuaranteeTests.cs` | Expectativas de códigos, prioridad de fuente protegida y casos de valores vacíos sin homologación. |
| `NachaDesagregadoIntegrationMappingTests.cs` | Seis casos negativos nuevos para los tres metadatos y ambos modos. |

**TODAS LAS PRUEBAS: PENDIENTE DE EJECUCIÓN FINAL.** No se ejecutaron `dotnet restore`, `dotnet build`, xUnit, scripts sustitutivos, pruebas XML, integración, BD, HTTP, SOAP ni UAT. La ausencia de .NET 10 no se clasifica como defecto. Al cierre global corresponde realizar compilación .NET 10, pruebas focales y regresión general en el entorno autorizado, y validar sus resultados antes de considerar GO.

## Homologaciones aún faltantes y riesgos

- **`OFEMP`:** identidad y fuente de empresa acreditadas (no `clearinghouse.code`).
- **`OFIDARCH`:** identificador real del archivo saliente (no `batch.id`).
- **`OFDIRECCIONIP`:** dirección IP verificable y norma funcional aprobada (no `0.0.0.0`).
- **`NCTAORIG`:** cuenta originadora independiente real si `Transactions.SourceAccountNumber` no está disponible; nunca copiar la receptora.
- El XSD permite algunos campos vacíos, **pero ello no levanta el control financiero ni la barrera pre-POST**. No se inventó ni actualizó ningún valor de negocio.

### RA-03 y RA-04 — siguen documentados, no implementados

- **RA-03:** `OFCTA`/`OFIDTX` se cotejan actualmente con el objeto `AchTransaction` entregado al resolver; un cotejo adicional contra snapshot/DB deberá estudiarse antes de Live. **No se introdujeron lecturas nuevas de DB en el flujo saliente ni cambios de fencing.**
- **RA-04:** el transporte XML directo de `Proc_Transacciones` no transporta por sí mismo una prueba de procedencia/preflight; el llamador productivo conocido sí valida readiness y Live. **No se alteró ese transporte**.

**Invariantes:** sin cambios en `PLValidarUsuarioBV`, `RegistrarRespuestaTransaccion`, tablas/migraciones, idempotencia, fencing, controles monetarios, ACH Colombia F5 ni envío Live. **NO-GO SOAP Live hasta homologación y validaciones finales.**
