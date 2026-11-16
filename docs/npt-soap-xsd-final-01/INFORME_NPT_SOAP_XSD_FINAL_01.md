# NPT_SOAP_XSD_FINAL_01 — informe de implementación

**Fecha:** 2026-11-16. **Base:** `ACHInterbank_NPT_PRE_XSD_FINAL_2026-11-15.zip`. **Dictamen operativo:** **NO-GO Live**. Sin UAT. 

## Alcance acotado

Se implementaron controles contractuales de **NPT-01, NPT-05, NPT-10 y NPT-14** usando exclusivamente los WSDL y cinco XSD recibidos. No se cambiaron NPT-02/03/04/06/07/08/09/11/12/13, ciclos ni las salvaguardas financieras. Los XSD se incluyeron exactamente como venían; la conexión `localhost:7083` de los WSDL **no es endpoint operativo** y no se utiliza durante la validación.

| Hallazgo | Estado | Implementación comprobable en fuente | Pendiente real |
|---|---|---|---|
| NPT-01 — ACH Colombia F5 file-based | **PARCIAL** | Proyección F5.3 ahora valida su SOAP parcial con `WSCFAACH/xsd0.xml`, orden/tipos/namespaces y permanece `CanTransmit=false`, `HasApprovedAchColMapping=false`; las solicitudes CFA completas y `SOAPAction` se verifican antes de transporte. Sin ciclo ficticio. | Autorización operativa file-based, mapping y elegibilidad F5 homologados por **ACH Colombia / área integradora CFA**. |
| NPT-05 — NCTAORIG | **PARCIAL** | Se comprueba campo de tipo `xs:string` en la posición 6/27. En mapping contract-driven y legacy se exige origen `transaction.sourceAccountNumber`, tipo de fuente `Transaction`, sin constante/default y valor igual al de la transacción. Se impide usar cuenta receptora. La guarda Live permanece. | Evidencia del **proveedor CFA / entidad financiera** de que esa cuenta del originador es la fuente bancaria autorizada. |
| NPT-10 — parser SOAP CFA y Axon | **CORREGIDO TÉCNICAMENTE EN FUENTE**, pendiente compilación y xUnit | CFA exige respuesta `Proc_TransaccionesResponse` SOAP 1.1, único `RTAACH` no vacío, ruta directa y XSD; Axon exige operación `RegistrarRespuestaTransaccionResponse`, único result no nil y `existeError` boolean del namespace de datos, XSD y Fault fail-closed. Respuestas ambiguas y DTD se rechazan; no se activan reintentos. | Pruebas .NET y SOAP instrumentado, más interpretación homologada de códigos bancarios. |
| NPT-14 — contrato WSDL/XSD | **CORREGIDO TÉCNICAMENTE EN FUENTE**, pendiente compilación y xUnit | Ambos WSDL y cinco XSD embebidos con SHA-256 preservado. Resolución local de imports, `XmlResolver=null`, DTD prohibido, esquemas compilados, `SOAPAction`, SOAP 1.1 document/literal, namespaces, operaciones y tipos. `SOAPAction` configurado se contrasta con WSDL antes de HTTP. | Validar C# con SDK y pruebas runtime. No falta ningún XSD de estos paquetes. |

La etiqueta **«corregido técnicamente en fuente» NO equivale a compilación aprobada, homologación del banco ni aptitud Live**.

## Seguridad y no regresión

- **Sin cambio de los estados monetarios, claims, fencing, leases, recuperación ni idempotencia**. No se inicia un segundo débito: el transporte continúa con política sin reintentos automáticos.
- **No se retiraron bloqueos de Live**: para `NCTAORIG` persiste `PROC_TRANSACCIONES_NCTAORIG_NOT_HOMOLOGATED`; para F5 no se aprueba dispatch ni mapping en la proyección.
- Las restricciones de tipos, orden, cardinalidad y nulabilidad provienen directamente del XSD; el rechazo interno de `RTAACH` ausente/nil y de resultado Axon ausente/nil son **reglas internas de seguridad**, más estrictas que `minOccurs=0` del XSD.
- La lista de valores de negocio `RTAACH` se conserva del parser anterior. **No está homologada por los XSD**; una respuesta XSD-válida no implica éxito bancario.
- Se conserva SOAP 1.1 porque es el binding real de ambos WSDL. No se concede aceptación SOAP 1.2 para aprobar verificadores heredados.
- Importaciones y validación se realizan en memoria sin consulta a URLs de los WSDL, DTD, entidades externas ni resolutores remotos.

## Archivos productivos principales

- `src/Cfa.ACHInterbank.Application/Integrations/Contracts/OfficialSoapContracts.cs` y `OfficialSoapSchemas/**` (nuevo: dos WSDL, cinco XSD embebidos).
- `src/Cfa.ACHInterbank.Application/Cfa.ACHInterbank.Application.csproj` (resources).
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/{ProcTransaccionesRequestMapper,ProcTransaccionesResponseParser,AchColProcTransaccionesDryRunContractCheck}.cs`.
- `src/Cfa.ACHInterbank.External/Connections/RespuestaTransaccionesAch/{RegistrarRespuestaAchSoapResponseParser,RespuestaTransaccionesAchGateway}.cs`.
- `src/Cfa.ACHInterbank.External/Connections/{WscfaachSoapClient,WsAxonRespuestaTransaccionesSoapClient}.cs` (SOAPAction exacto).
- `IncomingNachaPostProcessingPipeline.cs` solo actualiza comentario sobre origen candidato sin quitar guarda Live.

Ver `INVENTARIO_CAMBIOS.md` para la lista exacta de todos los archivos incluidos, incluidas pruebas/documentación. **Ninguna migración ni dependencia NuGet nueva**.

## Pruebas y evidencia real

| Comprobación | Resultado | Evidencia |
|---|---|---|
| Cinco XSD y WSDL offline mediante `lxml` sin red | **38/38 PASS** | `evidencias/offline_xmlschema_results.json`; `verify_wsdl_xsd_offline.py` |
| Originalidad de recursos, presencia de guardas/llamadas a validación (revisión estática Python) | **33/33 PASS** | `evidencias/source_guard_results.json`; `verify_source_guards.py` |
| Regresiones históricas seleccionadas (siete scripts) | **7/7 salida 0** | Logs `evidencias/*.log` |
| Verificador NPT heredado | **40/42, salida 1** | `evidencias/npt_static_guard.log` (Axon `CommitAsync` obsoleto y exigencia SOAP 1.2 ajena a WSDL SOAP 1.1) |
| `dotnet --info`, `dotnet restore`, `dotnet build`, `dotnet test` | **No ejecutados: comando no disponible, salida 127** | `evidencias/dotnet_*.log` |
| xUnit, EF SQL Server/PostgreSQL, integración SOAP instrumentada | **Pendientes; pruebas C# escritas, NO ejecutadas** | `tests/Cfa.ACHInterbank.Tests/OfficialSoapXsdContractTests.cs` y pruebas actualizadas |
| UAT, SOAP Live | **No ejecutados** | Restricción del incremento |

**Los 38 casos `lxml` y 33 verificadores estáticos NO son xUnit ni prueba del código C# corriendo.** La verificación de `SOAPAction` se agregó con dos tests para rechazar configuración incorrecta antes del transporte; no se ejecutaron.

### Comandos reproducibles (en la carpeta `ACHInterbank_ajustado/`)

```bash
python -m pip install lxml
python docs/npt-soap-xsd-final-01/verify_wsdl_xsd_offline.py
python docs/npt-soap-xsd-final-01/verify_source_guards.py
dotnet --info
dotnet restore ACHInterbank.sln
dotnet build ACHInterbank.sln --no-restore
dotnet test ACHInterbank.sln --no-build
```

La instalación de `lxml` es únicamente para la comprobación offline opcional y no agrega dependencia al proyecto .NET.

## Dependencias EXTERNAS inevitables

1. **ACH Colombia / área responsable de integración F5:** especificación operacional y aprobación del mapping/elegibilidad file-based sin ciclo y vectores de aceptación; no se infiere de los XSD.
2. **Proveedor CFA / entidad financiera originadora:** autorización documentada de la procedencia bancaria de `NCTAORIG` (y su relación con la cuenta real del originador); no se deduce de `xs:string`.
3. **Proveedor CFA/Axon:** definición oficial, y vectores homologados, de códigos de aceptación, rechazo y resultado incierto. Los XSD proporcionan solo tipos, no semántica funcional; no se habilita Live basándose en los códigos heredados.

**Infraestructura de pruebas interna pendiente (no dependencia contractual):** SDK .NET 10, pruebas xUnit/SQL Server/PostgreSQL y simuladores SOAP sin movimiento de dinero. **No hay XSD pendientes** de los dos ZIP recibidos.

## Autoverificación adversarial

Se revisaron: campos unqualified/orden/duplicación, SOAP Fault, nil, códigos de negocio, DTD/XXE, importaciones externas, error `SOAPAction` configurado, ausencia de fuente de cuenta, paridad del XML legacy vs motor genérico, bloqueo F5 y guardas Live. Se corrigió el caso de test de namespace heredado que no representaba un elemento unqualified, y se añadieron comprobaciones exactas de `SOAPAction` de ambos clientes. El modelo de validación XML y los guards estáticos pasaron; **quedan por comprobar compilación, runtime y concurrente**. No se encontró justificación para abrir Live.

**Dictamen final: NO-GO PRODUCTIVO.**
