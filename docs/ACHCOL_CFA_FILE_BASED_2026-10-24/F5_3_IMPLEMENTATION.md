# F5.3 — Preparación mínima de contrato CFA y harness local DryRun

**Estado:** `IMPLEMENTED_UNVERIFIED` — código, tests e inspección estática preparados; no compilación .NET, xUnit, ni llamada CFA.  
**Base:** `ACHInterbank_ACHCOL_CFA_F5_2_UNIFICADO.zip`.  
**Entrega:** `ACHInterbank_ACHCOL_CFA_F5_3_UNIFICADO.zip`.  
**Próximo incremento:** F6.1 — ejecución de regresiones y pruebas de integración cuando esté disponible el ambiente.  
**Fecha:** 2026-10-25.

## Alcance real

F5.3 finaliza los desarrollos previstos en F1–F5 **sin habilitar operaciones monetarias Live**. Se agrega una verificación pequeña, pura y local del XML parcial de F5.2; no hay un segundo sistema de mapping, nuevos endpoints, colas, tablas, migraciones, workers, configuración o transportes. No se introduce un contrato CFA ficticio.

La verificación `AchColProcTransaccionesDryRunContractCheck.Inspect` se ejecuta dentro de `ProcTransaccionesRequestMapper.PreviewAchColFileBasedAsync` antes de entregar el XML de diagnóstico. Con una vista previa F5.2 válida:

1. Comprueba que siga bloqueada y no transmisible (`CanTransmit=false`, código `ACHCOL_SOAP_CONTRACT_NOT_HOMOLOGATED_F52`), que exista identificación de la fila, entrada y mapping de referencia.
2. Acepta únicamente los campos candidatos **observables** `TIPTRAN`, `MONTO`, `NCTARECEP`, `NRECEP` e `IDRECEP`, y requiere al menos `TIPTRAN` y `MONTO`.
3. Comprueba que el XML de diagnóstico tenga solo `soap:Envelope/soap:Body/Proc_Transacciones`, sin parámetros desconocidos, duplicados, atributos de campos ni discrepancias entre los valores observados y el XML.
4. Analiza el XML con `XmlReaderSettings.DtdProcessing=Prohibit`, `XmlResolver=null` y límite de tamaño. No resuelve entidades externas ni realiza consultas HTTP.
5. Devuelve evidencia **redactada** que contiene nombres de campos presentes, faltantes, pendientes y motivos, pero **nunca** cuentas, personas, identificaciones o montos. Las operaciones existentes con `CycleBound` no pasan por esta verificación nueva.
6. Conserva `HasExternalXsdEvidence=false`, `HasApprovedAchColMapping=false`, `CanTransmit=false`. No existe camino de este componente hacia `IWscfaachSoapClient` o las operaciones de desbloqueo/reintento.

## Evidencia contractual disponible

El WSDL aportado `WSCFAACH.wsdl (1).xml` declara `Proc_Transacciones`, pero contiene **dos importaciones XSD externas no incluidas**:

- `http://esparta/WSCFAACH/WSCFAACH.svc?xsd=xsd0` (namespace `http://tempuri.org/`).
- `http://esparta/WSCFAACH/WSCFAACH.svc?xsd=xsd1` (namespace `http://schemas.microsoft.com/2003/10/Serialization/`).

Estas ubicaciones se anotan **solo como referencias del WSDL**, no se accede a ellas ni se considera que el contrato esté disponible. El inventario interno de 25 parámetros y dos respuestas F5.1 **no sustituye** la semántica contractual externa. `IDTRAN`, `IDLOTE`, `IDCAMCOMPE`, `BCOORIG`, `BCORECEP` y las demás reglas condicionales/no resueltas continúan pendientes; no se usará `ExternalCycleNumber` como cámara, lote ni transacción.

## Criterios pendientes para habilitación real (F6 y autorización externa)

| Evidencia requerida | Estado en F5.3 | Condición para autorización posterior |
| --- | --- | --- |
| XSD `xsd0` y `xsd1` oficiales | **NO DISPONIBLES** | Incorporar copia aprobada, versionada y verificable; validar WSDL+XSD sin resolver URLs externas en producción. |
| Homologación semántica 25 entradas y respuestas | **PENDIENTE** | Confirmación de tipos, longitudes, obligatoriedad, catálogo bancario, identificadores/fecha y dirección de cada campo. |
| Mapping ACHCOL publicado y aprobado | **PENDIENTE** | Mapping propio de archivo NACHA, sin reciclar `cycle.id` ni mapeo `CycleBound` como evidencia de aprobación. |
| Pruebas locales .NET y BD ambos proveedores | **NO EJECUTADAS** | Ejecutar build, xUnit, migraciones y regresión CENIT/RET; resolver fallos. |
| Pruebas con CFA en entorno autorizado | **NO EJECUTADAS** | Confirmar solicitud/respuesta, estado de resultado incierto, conciliación, no doble pago e idempotencia. |
| Aprobación operativa Live y rollback | **PENDIENTE** | Autorización explícita con controles de feature flag y ventana de operación. |

## Harness de prueba incorporado

`tests/Cfa.ACHInterbank.Tests/AchColProcTransaccionesContractReadinessF53Tests.cs`: casos de XML válido parcial sin autorización, datos sensibles excluidos de evidencia, rechazo de `IDCAMCOMPE`, `IDTRAN`, `IDLOTE` y `DISCRE`, XML manipulado, DTD/entidades y operación SOAP ajena, más el bloqueo de `RequireHomologatedMapping(true)` con inventario no resuelto.

**Para ejecutar en F6 cuando exista SDK:**

```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj --filter 'FullyQualifiedName~AchColProcTransaccionesDryRunF52Tests|FullyQualifiedName~AchColProcTransaccionesContractReadinessF53Tests'
```

No se ejecuta cliente CFA desde esos tests. Validar en F6 el resto de suites F1–F5, concurrencia y proveedores DB.

## Decisiones F5.3

- **DEC-071:** reutilizar el DryRun F5.2, no crear nuevo proceso, endpoint o motor de mapeos.
- **DEC-072:** inspección de SOAP parcial y allowlist estricto en memoria, falla cerrada ante XML inseguro o campo sin homologación.
- **DEC-073:** evidencia diagnóstica redactada (nombres/códigos, nunca valores monetarios o PII).
- **DEC-074:** XSD importados y reglas CFA quedan pendientes; no declarar XML parcial como contrato validado ni habilitar Live.
- **DEC-075:** cerrar F5 en estado `IMPLEMENTED_UNVERIFIED`, trasladar compilación/pruebas y autorización a F6 sin bloquear la entrega de código.

## Archivos cambiados

- Modificado: `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcTransaccionesRequestMapper.cs` (invocación del guard en DryRun solamente).
- Nuevo: `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/AchColProcTransaccionesDryRunContractCheck.cs` (validación estática y resumen de nombres sin datos sensibles).
- Nuevo: `tests/Cfa.ACHInterbank.Tests/AchColProcTransaccionesContractReadinessF53Tests.cs` (harness de simulación).
- Docs: `F5_3_IMPLEMENTATION.md`, `PLAN.md`, `MEMORY_STATE.md`, `CHATGPT_MICRO_START.md` y `README_PAQUETE_UNIFICADO.md`.

## Verificación realizada y limitaciones

- El código y tests se revisaron de forma estática; la verificación del WSDL se hizo leyendo los nodos `xs:import` del archivo proporcionado.
- **No se han ejecutado:** compilación .NET 10, xUnit, migraciones PostgreSQL/SQL Server, entornos CFA, UAT ni operaciones SOAP monetarias.
- ZIP comparado contra F5.2: **8.124 entradas**, 8.116 anteriores con CRC y tamaño idénticos, 5 actualizadas, 3 nuevas; lectura CRC completa `ZipFile.testzip()` exitosa. Esta comprobación no equivale a build ni certificación de negocio.
- Cualquier hallazgo en F6 puede requerir correcciones a código: el estado F5.3 no significa aprobación productiva.
