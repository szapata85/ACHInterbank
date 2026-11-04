# F5.2 — Proyección NACHA-M y vista SOAP DryRun no transmisible

**Estado:** `IMPLEMENTED_UNVERIFIED` (código, tests y validación estática; .NET y entorno CFA no ejecutados).  
**Base:** `ACHInterbank_ACHCOL_CFA_F5_1_UNIFICADO.zip`.  
**Siguiente:** F5.3 — harness, preparación de autorizaciones y casos de integración, sin activar Live.  
**Fecha de registro:** 2026-10-25.

## Decisión de alcance (sin sobreingeniería)

El mapper existente mantiene su ruta `CycleBound` sin alteración; F5.2 agrega una única proyección diagnóstica `PreviewAchColFileBasedAsync` sobre **`EntryDetail` persistido**. No se agregan colas, tablas, migraciones, workers, endpoints, transportes o feature flags. F4.2 ya proporciona el bloqueo seguro de las filas ACHCOL y F4.3 protege las acciones manuales.

**No se entrega una solicitud aprobada ni un envío real.** El resultado `AchColProcTransaccionesDryRunPreview` es un tipo distinto de `ProcTransaccionesRequestContract`, establece `CanTransmit=false` de forma incondicional y muestra un XML de forma SOAP **parcial**, solo para diagnóstico local. No se invoca `IWscfaachSoapClient` ni el endpoint CFA. Construir el XML no equivale a la homologación contractual externa.

## Ruta de datos y controles

1. Exigir `SourceKind=AchColFileBased`, `AchCycleId=null`, `AchTransactionId=null`, `EntryDetailId` persistido y cola `Blocked/ACHCOL_MAPPING_NOT_READY_F42`, sin intentos ni reintentos pendientes.
2. Verificar que los identificadores de cola, clasificación, archivo y fecha operativa correspondan; comprobar nombre `.OUT`/`.OUT.env`, `FileIdModifier`, `FileSequence`, `ExternalCycleNumber`, cámara ACHCOL activa y origen de la entrada.
3. Cargar registro 6 `EntryDetail` por ID; registro 1 `NachaHeader` vinculado **a la misma ingesta**; lote único del mismo NACHA y número; validar huella idempotente SHA-256 sobre contexto y código monetario 22/32/52 o 27/37/55 según clase funcional. Sin asumir `AchCycle` o `AchTransaction`.
4. Exigir un método CFA activo, un único `IntegrationMappingSet` publicado/activo y parámetros técnicos de entrada conocidos. Este control **no transforma** el mapping CycleBound publicado en autorización ACHCOL; solo permite mostrar versión de referencia y parámetros requeridos aún no resueltos.
5. Proyectar *solo fuentes directas observables* (`TIPTRAN`, `MONTO`, `NCTARECEP`, `NRECEP`, `IDRECEP`, cuando existan). `DISCRE` se omite hasta homologación aunque esté presente. Normalización CFA, rangos, semánticas, catálogos y conversiones pendientes **no se inventan**.
6. Presentar `PendingHomologationParameters`, `MissingRequiredParameters`, `SoapPreviewXml` y `BlockingCode=ACHCOL_SOAP_CONTRACT_NOT_HOMOLOGATED_F52`. No hay guardado de XML sensible en base de datos ni llamada de red.

## Protección específica de campos

| Campo / grupo | En F5.2 | Límite |
|---|---|---|
| `TIPTRAN`, `MONTO`, `NCTARECEP`, `NRECEP`, `IDRECEP` | Valor observable NACHA-M, solo en memoria para diagnóstico | No declarar equivalencia CFA definitiva ni autorizar Live |
| `IDTRAN`, `IDLOTE`, `IDCAMCOMPE` | **No incluidos** en preview SOAP; pendientes | No inventar transacción/ciclo ni convertir `N` en identificador de cámara |
| `BCOORIG`, `BCORECEP`, `FECEFEC`, `INFPAG` | Pendientes | Requieren fuente homologada o transformación autorizada |
| `NCTAORIG`, `CONV`, `PROD`, `REGLOTE`, `IREVER`, `LIBRE`, `DIRECCIONIP`, `LIBRE1` | Pendientes | Fuente contractual no demostrada |
| `DISCRE` | Se omite del XML | Opcional; requiere mapping aprobado |

La matriz detallada de 25 entradas se conserva en `F5_1_IMPLEMENTATION.md`; no se modifica el mapping del programa paralelo CFAUX. XSD externos referenciados por el WSDL no disponibles. El XML generado es **incompleto** si falta cualquier requerido; es peligroso usarlo fuera del diagnóstico y no existe método que lo libere.

## Archivos del incremento

- `src/Cfa.ACHInterbank.Application/ACH/Models/AchColProcTransaccionesDryRunPreview.cs` — resultado tipado sin capacidad de envío.
- `src/Cfa.ACHInterbank.Application/ACH/Interfaces/IProcTransaccionesRequestMapper.cs` — API de previsualización separada de la resolución transaccional clásica.
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcTransaccionesRequestMapper.cs` — carga de contexto y validación fail-closed, proyección SOAP XML parcial de solo lectura.
- `tests/Cfa.ACHInterbank.Tests/AchColProcTransaccionesDryRunF52Tests.cs` — escenarios H/I/.10, bloqueo, prenote, huella y mapping publicado.
- `docs/ACHCOL_CFA_FILE_BASED_2026-10-24/` — estado, decisiones y continuidad.

## Decisiones F5.2

- **DEC-066:** adaptador diagnóstico por entrada como método del mapper existente, sin segundo pipeline ni entidad `AchCycle` artificial.
- **DEC-067:** separar el modelo DryRun del contrato transmisible; `CanTransmit=false` incondicional, sin mutaciones y sin SOAP Client.
- **DEC-068:** solo valores observables; no mapear IDTRAN/IDLOTE/IDCAMCOMPE u otros condicionales sin autorización contractual CFA.
- **DEC-069:** exigir F4.2 `Blocked/ACHCOL_MAPPING_NOT_READY_F42`, identidad válida, cámara ACHCOL activa y mapping de referencia publicado único para mostrar preview.
- **DEC-070:** no publicar payload, no cambiar mappings CFAUX, no ejecutar Live; completar harness y evidencia de contrato en F5.3/F6.

## Verificación real, no inferida

- ZIP base F5.1 inspeccionado, fuente y documentación revisadas; prueba de integridad previa (`ZipFile.testzip` exitosa).
- Cambios y pruebas C# incorporados. **No se han ejecutado** `dotnet build`, xUnit, migraciones ni pruebas de integración CFA, porque `dotnet` no está disponible en este entorno. La revisión estática no certifica compilación.
- No se ejecutaron transferencias monetarias. El método de DryRun **no** referencia `IWscfaachSoapClient` ni crea `IncomingNachaIntegrationExecution`.
- Continuidad F5.3: preparar escenarios de contrato/XSD, harness de simulación y guardas de activación; no afirmar homologación por la existencia de XML de preview. Pruebas de ambiente en F6.
