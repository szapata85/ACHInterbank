# Auditoría estática focalizada: SPA «Simulador de archivos recibidos» y backend NACHA-M

**Proyecto:** ACH Interbank  
**Fecha de corte:** 2026-11-02  
**Fuente inspeccionada:** `ACHInterbank_PROYECTO_COMPLETO_CORRECCIONES_CENIT_F08_2026-11-02.zip`, directorio `ACHInterbank_ajustado`.  
**Objeto:** `/uat/nacha-inbound-simulator`, servicios Angular, controlador REST, validaciones y generadores, catálogos, persistencia y entrega al flujo `NachaUpload` de ACH Colombia y CENIT.  
**Modalidad:** lectura estática de código y contraste documental; **sin alteración de fuentes**. No se ejecutó API, base de datos, Angular, pruebas .NET ni navegador: el entorno de análisis no incluye `dotnet`, `node_modules` ni una instancia de la aplicación/BD. Las verificaciones Python anexas examinan patrones en el código, no equivalen a pruebas funcionales ni a explotación confirmada.

## 1. Resultado ejecutivo

**Dictamen:** **NO APROBADO para considerar funcionalmente cerrado y normativamente homologado el simulador completo**, sin afirmar que el conjunto del proyecto sea no productivo. Se encontraron **12 hallazgos técnicos abiertos** (7 altos y 5 medios), además de **2 limitaciones/elementos de deuda de prueba** que deben registrarse por separado; la clasificación expresa riesgo estático, no incidencia observada en ejecución. El punto principal es la desalineación entre pantalla, controles del backend y formato físico para los modos entrante y diferencial.

### Flujo trazado

```text
/uat/nacha-inbound-simulator
  NachaInboundSimulatorComponent
    NachaInboundSimulatorService (Angular)
      GET /api/uat/nacha-inbound-simulator
      POST /eligibility-preview
      GET /eligible-differential-transactions
      GET /available-cycles
      POST /generate
      GET /{id}/file
        NachaInboundSimulatorController
          NachaInboundSimulationService
            AchDbContext / catálogos / ciclos / referencias
            BuildFile (entradas) o INachaFileBuilder.BuildReturnOutAsync (devoluciones)
            archivo físico / SHA-256 / NachaInboundSimulations / evidencia
  [separación explícita: no se importa automáticamente]
/transactions/nacha-upload
  POST /NachaUpload/upload
    NachaUploadController
      IncomingNachaIngestionAppService
        hash/deduplicación -> resolución de cámara/ciclo -> selección de perfil
        -> parser/validación -> postprocesamiento según resultado
```

### Controles positivos inspeccionados

- `NachaInboundSimulatorController.cs:11-12,26-42,59-106`: hay autenticación/autorización global `TestTools.Access`, políticas específicas de lectura, generación y descarga.
- `NachaInboundSimulationService.cs:250-285`: generación bloqueada cuando `Enabled=false`, autoimportación o transmisión externa configuradas; `:147-149,167-179` declara `generatedOnly=true`, `autoImported=false`, `uploadRequired=true`, `externalTransmission=false`.
- `NachaInboundSimulationService.cs:763-826`: distingue banco externo de entidad CFA/default, exige activos y único receptor predeterminado.
- `NachaInboundSimulationService.cs:951-1105`: para devoluciones aceptadas por la validación utiliza el constructor oficial `INachaFileBuilder` (Opción C), conserva trace original y reserva rango de nuevos traces.
- `NachaUploadController.cs:35-149`: el flujo posterior de ingesta tiene validación de archivo, tamaño, nombre/MIME, permiso `P1Policies.NachaUpload` y delegación a `IIncomingNachaIngestionAppService`.
- `IncomingNachaIngestionAppService.cs:71-125,127-194`: hay controles de duplicados por hash/tamaño y restricciones de reproceso; estos controles no prueban por sí solos compatibilidad de los artefactos simulados.

## 2. Registro de hallazgos (evidencia localizada)

| ID | Severidad | Hallazgo | Evidencia principal | Estado |
|---|---|---|---|---|
| SPA-SIM-01 | **Alta** | Descarga con `window.open` no adjunta el JWT usado por el interceptor, por lo que se espera 401 con autenticación Bearer y sin mecanismo alternativo. | `nacha-inbound-simulator.component.ts:691-693`; `auth.interceptor.ts:22-33`; `Cfa.ACHInterbank.External/DependencyInjectionService.cs:131-146` | Abierto; probar en ejecución |
| SPA-SIM-02 | **Alta** | Descripción de lote no incluida en la tabla oficial de CENIT: `CREDIT`/`DEBIT` frente a valores de la tabla 4. | `NachaInboundSimulationService.cs:1228-1236,1470-1474`; manual CENIT 2026, anexo 2 tabla 4, campo 7 del registro 5 | Abierto; incumplimiento estático |
| SPA-SIM-03 | **Alta** | En «nueva transacción entrante», el monto y la referencia introducidos pueden ser ignorados: se toman de transacciones existentes del ciclo. | `NachaInboundSimulationService.cs:92-101,675-700,1241-1246`; `component.ts:822-841` | Abierto |
| SPA-SIM-04 | **Alta** | La elegibilidad diferencial listada (pendiente/sin respuesta previa) no se vuelve a verificar íntegramente al generar con referencias enviadas directamente a la API. | `NachaInboundSimulationService.cs:515-563` frente a `838-910` y `343-355` | Abierto |
| SPA-SIM-05 | **Alta** | Se valida que exista un ciclo disponible, pero no que **todas las transacciones referenciadas pertenezcan al ciclo seleccionado y fecha de negocio**. | `NachaInboundSimulationService.cs:80-101,652-673,838-910` | Abierto |
| SPA-SIM-06 | **Alta** | Límite de entradas aplicado a `EntriesCount` pero no al número de referencias diferenciales, potencialmente mayor a 10 por selección multipágina o API. | `NachaInboundSimulationService.cs:298-301,338-355,1004-1045`; `component.ts:678-689,822-841` | Abierto |
| SPA-SIM-07 | **Alta** | Secuencia diaria por `CountAsync + 1` no es atómica; además el contador está segmentado por originador, pero el nombre externo no incorpora originador. Riesgo de nombres/consecutivos repetidos. | `NachaInboundSimulationService.cs:1180-1193,1355-1368`; `NachaInboundSimulationConfiguration.cs:46-49` | Abierto |
| SPA-SIM-08 | **Media** | No hay unidad atómica archivo/BD/evidencia: archivo escrito antes del `SaveChangesAsync`, evidencia después; fallas parciales dejan inconsistencias. | `NachaInboundSimulationService.cs:119-165` | Abierto |
| SPA-SIM-09 | **Media** | Al cambiar cámara no se limpia la selección diferencial previa, aunque se cargan nuevas filas; el rechazo posterior se delega al backend. | `component.ts:366-416` | Abierto |
| SPA-SIM-10 | **Media** | `RequireSyntheticData=true` se ofrece como control de configuración, pero nunca se consulta en el servicio; las entradas nuevas reutilizan referencias/valores persistidos y registran `IsSynthetic=false`. | `NachaInboundSimulatorOptions.cs:10`; `NachaInboundSimulationService.cs:675-700,1241-1293` | Abierto; revisar política de datos UAT |
| SPA-SIM-11 | **Media** | Errores por cámara desconocida pueden escalar como 500 en preview y consultas porque `ResolveClearingHouseAsync` lanza fuera de un manejo controlado en estos endpoints. | `NachaInboundSimulationService.cs:308-314,436-440,568-575,728-736`; `NachaInboundSimulatorController.cs:115-163` | Abierto |
| SPA-SIM-12 | **Media** | La pantalla fija «UAT LOCAL» incluso si se habilita el servicio con otro `Mode`; el backend no exige `IsUatLike()` al operar (bloquea por flags de efecto externo). | `component.html:7-18`; `NachaInboundSimulationService.cs:250-260`; `NachaInboundSimulatorOptions.cs:18-22` | Abierto; desalineación de comunicación |

**Elementos registrados aparte (no contados como hallazgos de incumplimiento):**

- **LIM-01 — Generación diferencial deliberadamente bloqueada.** La SPA expone confirmación, rechazo y prenotificación, pero el servidor devuelve `DIFFERENTIAL_GENERATOR_NOT_HOMOLOGATED` para escenarios diferenciales no devolutivos aunque exista perfil; `NachaInboundSimulationService.cs:418-429`. Se debe etiquetar en la UI «no homologado/no disponible», no anunciarlo como capacidad operativa. Las devoluciones tienen ruta distinta vía Opción C; su elegibilidad depende del perfil y causal.
- **PRU-01 — E2E con selectores obsoletos.** `web/ach-interbank-ui/e2e/nacha-simulator-differential-responses.spec.ts:31-34,108-110` busca `button.mode-card` y `aria-pressed`; la plantilla actual usa `<mat-card class="mode-card">` y `<mat-button-toggle>` (`component.html:21-51`). Esa prueba exige actualización antes de servir como evidencia de aceptación; no se ejecutó.

## 3. Análisis de los riesgos prioritarios

### SPA-SIM-01 — Descarga autenticada

Angular realiza llamadas con `HttpClient`; su `AuthInterceptor` inyecta `Authorization: Bearer ...`. `window.open(url)` inicia una navegación normal que no atraviesa dicho interceptor. El backend usa `JwtBearerDefaults.AuthenticationScheme` y el endpoint requiere políticas adicionales `TestTools.Access` y `P1.NachaSimulatorDownload`. **Efecto esperado:** error 401 al descargar desde la pantalla, aunque el usuario haya generado el archivo correctamente. **Corrección mínima:** `HttpClient.get(..., { responseType:'blob' })`, `URL.createObjectURL`, enlace de descarga temporal y revocación del objeto, con manejo explícito de 401/403/error y validación del tipo/filename. Mantener la autorización backend.

### SPA-SIM-02 — Formato CENIT del registro de lote

El campo 7 del registro tipo 5 (posiciones 54-63) es obligatorio y el manual **«Manual de Especificaciones Formato NACHA-M CENIT», 7 de mayo de 2026, Anexo 2, tabla 4** establece descripciones como `PAGOS`, `RECAUDOS`, `TRANSFER`, `DEVOLUCION`, `PRENOTIFIC`, `SUBSIDIOS`, entre otras. `BuildFile` escribe `ScenarioDescription(IncomingCredit)=CREDIT` y `ScenarioDescription(IncomingDebit)=DEBIT`, no listadas. **Efecto:** riesgo concreto de rechazo formal/fatal por descriptor de lote no permitido (la clase exacta de rechazo requiere ejecución con validador homologado). **Corrección mínima:** resolver descripción a partir del perfil y política normativa de cámara/tipo de transacción (p. ej. `TRANSFER`/`RECAUDOS` solo si la semántica corresponde); prohibir literales inventados. Prueba de registro 5 por cada cámara/escenario con perfil vigente.

### SPA-SIM-03 y SPA-SIM-10 — Semántica de nuevas entradas

Para entradas nuevas, `GenerateAsync` siempre sustituye `existingReferences` por operaciones del ciclo (`LoadCycleTransactionsAsync`); en el bucle `var entryAmount = tx?.Amount ?? request.Amount` y `var reference = tx?.Reference ?? ...`. Con al menos una transacción cargada, no se utiliza el monto introducido ni el prefijo de referencia. A la vez, la salida incorpora valores sintéticos de cuenta y receptor y marca `IsSynthetic = tx is null` como falso. **Efecto:** vista previa engañosa y trazabilidad que mezcla una operación real con una entrada fabricada, pese a `RequireSyntheticData=true`. **Corrección mínima:** fijar explícitamente la política: o escenario 100% sintético sin tomar identificadores existentes, o escenario basado en transacción existente con campo valor/referencia de solo lectura y controles de protección. No mantener ambas semánticas mezcladas.

### SPA-SIM-04, SPA-SIM-05 y SPA-SIM-06 — Integridad de selección diferencial

La lista de elegibles exige estado `Pending`, datos de correlación completos y ausencia de respuesta anterior. `ResolveReferencesAsync` al generar solo impide estados de devolución finales y correlación incompleta: no exige `Pending`, no consulta `AchResponses` para esos IDs, ni verifica el ciclo solicitado. Las referencias pueden enviarse sin pasar por la selección visible de la SPA, lo que es normal para una API. El máximo configurado comprueba `request.EntriesCount`, no el número de referencias seleccionadas. **Corrección mínima:** servicio de validación único para listar, previsualizar y generar; consulta transaccional de estado, cámara, ciclo, fecha, banco respondiente, estado de respuesta previa y unicidad; conteo real de referencias <= máximo; idealmente protección ante cambios de estado entre preview y generate. Pruebas adversariales vía API, no solo UI.

### SPA-SIM-07 y SPA-SIM-08 — Consecutivos e integridad de evidencia

Un `CountAsync` seguido de `+1` no reserva secuencia bajo concurrencia. El nombre externo identifica al receptor, la fecha y la secuencia, pero el cálculo del contador incluye originador; dos originadores pueden obtener `001` el mismo día, generando basename idéntico aunque cada ejecución se guarde bajo un GUID distinto. La columna `FileName` posee índice **no único**. El archivo físico se escribe antes de persistir la simulación; metadata/evidencia, después. **Corrección mínima:** reserva atómica por la misma clave que determine identidad de archivo externo, índice acorde a esa identidad, controles de reintento y compensación/limpieza para fallas de archivo y BD. Prueba paralela y de fallo inyectado en tres etapas.

## 4. Matriz de pruebas exigidas para cierre

| Prueba | Expectativa de aceptación |
|---|---|
| Descarga con sesión JWT válida y permiso Download | HTTP 200, bytes y SHA-256 iguales a lo persistido; 401 sin token; 403 sin permiso |
| Crédito/débito entrante ACHCOL y CENIT | Registros de 106 caracteres, adendas, controles, nomenclatura y campo 7 válido por cámara, entrada válida en el parser correspondiente |
| Monto/prefijo de referencia nuevos | Los datos del archivo coinciden con la promesa de la UI o los campos quedan deshabilitados si se usan datos originales |
| 2+ entidades originadoras simultáneas | Identificadores externos y secuencias sin colisiones por cada día/cámara |
| Diferenciales: operación no `Pending` | Bloqueo tanto en preview como en generate |
| Diferenciales: respuesta ya persistida | Bloqueo aunque se suministre referencia directamente por API |
| Diferenciales: ciclo de otra operación | Bloqueo de selección de ciclo/fecha incongruente |
| Diferenciales: 11+ referencias con límite 10 | Bloqueo coherente en SPA y backend |
| Cambio de cámara durante selección | Las referencias y estados anteriores se limpian, sin mezcla de orígenes |
| Error de cámara inexistente | Respuesta 400/422 documentada y no 500 |
| `RequireSyntheticData=true` | No incorpora datos de transacciones reales en escenario sintético, o bloquea explícitamente |
| Fallos entre creación física/SaveChanges/evidencia | Sin archivos huérfanos ni metadatos inconsistentes; errores controlados |
| Descarga → carga manual de fichero generado | `NachaUpload` aplica políticas de admisión, selecciona perfil/ciclo, persiste ingesta y resultados esperados en ambas cámaras; comparar el hash de entrada |
| Prueba E2E con selectores actuales | Playwright localiza controles Material nuevos, valida, confirma, descarga y recibe errores controlados sin mocks inconsistentes |

## 5. Cobertura, limitaciones y decisiones

- **Se revisó código real:** componente Angular HTML/TS y estilos como parte de inventario, servicio HTTP, rutas y guard, controlador, DTO, generadores, motor de referencias y ciclos, configuraciones, entidad de BD, ruta del backend `NachaUpload`, inicio de servicio de ingesta y pruebas específicas disponibles. Se contrastaron campos relevantes del manual oficial CENIT incorporado al proyecto. **No se ha probado un archivo generado por esta versión exacta contra un CENIT real**, ni se afirma homologación.
- **Sin runtime:** no hay prueba directa de regresión visual/accesibilidad, autorización efectiva, base de datos Postgres/SQL Server, parsers con fixtures actuales, concurrencia ni descarga en navegador. No son resultados «pasados» o «fallidos»: **no ejecutados**.
- **Sin cambios:** ni el ZIP fuente ni sus archivos de aplicación se modificaron. Este informe constituye línea base para una corrección incremental separada, sin mezclar automáticamente los hallazgos CENIT anteriores.
- **Prioridad sugerida:** 1) descarga JWT y descripción normativa del lote; 2) redefinición semántica entrante/datos sintéticos; 3) validación diferencial centralizada y máximo real; 4) secuencias/atomicidad; 5) coherencia SPA y E2E; 6) runtime integral en Docker/UAT con ambas cámaras.

## 6. Rutas revisadas para reproducción

Abreviaturas: `SPA = web/ach-interbank-ui/src/app/features/uat/`; `Backend = src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/`.

- `SPA/components/nacha-inbound-simulator/nacha-inbound-simulator.component.ts` / `.html` / `.spec.ts` / `.scss`
- `SPA/services/nacha-inbound-simulator.service.ts`; `SPA/uat-routing.module.ts`
- `web/ach-interbank-ui/src/app/core/interceptors/auth.interceptor.ts`; `core/services/api.service.ts`
- `src/Cfa.ACHInterbank.Api/Controllers/NachaInboundSimulatorController.cs`; `NachaUploadController.cs`
- `src/Cfa.ACHInterbank.Application/ACH/Models/NachaInboundSimulationModels.cs`; `ACH/Configuration/NachaInboundSimulatorOptions.cs`
- `Backend/NachaInboundSimulationService.cs`; `Backend/IncomingNachaIngestionAppService.cs`
- `src/Cfa.ACHInterbank.Persistence/Configuration/NachaInboundSimulationConfiguration.cs`
- `src/Cfa.ACHInterbank.External/DependencyInjectionService.cs`
- `tests/Cfa.ACHInterbank.Tests/NachaInboundSimulatorTests.cs`
- `web/ach-interbank-ui/e2e/nacha-simulator-differential-responses.spec.ts`
- Manual de Especificaciones Formato NACHA-M CENIT (2026-05-07), Anexo 2 tabla 4; CENIT DSP-152 Anexo 2 (2025); ACH Colombia V35 (2026).

**Evidencia automatizada complementaria:** `auditoria_spa_simulador_checks_2026-11-02.json` junto a `verificaciones_estaticas_spa_simulador.py`; 14 coincidencias de patrones revisadas, **no** 14 pruebas funcionales ejecutadas. Los hallazgos se consideran abiertos hasta corrección, revisión y pruebas runtime.
