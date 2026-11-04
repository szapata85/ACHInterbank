# ACHInterbank — Plan incremental de corrección funcional de mappings SOAP

**Programa:** `SOAP-MAP-SAFE-001`  
**Base:** snapshot ZIP `ACHInterbank_NAV_UX_MFT_2026-10-27_I06_PROYECTO_COMPLETO.zip` (sin historial Git incluido).  
**Alcance:** `WSCFAACH.Proc_Contrapartidas`, `WSCFAACH.Proc_Transacciones`, `WSAXON.RegistrarRespuestaTransaccion`.  
**Estado actual (2026-11-01):** I01–I06 implementados sin certificación de runtime; I07 es `next_increment` condicionado a prerrequisitos. UAT solo en I07.  
**Estado durable:** `MEMORY_STATE.md` de esta misma carpeta.

## 1. Mandato de ejecución (obligatorio)

1. **Sin sobreingeniería.** Corregir el flujo existente; reutilizar `IntegrationContractVersion`, `IntegrationMappingSet`, source providers, mappers, request writer, response parsers, auditoría y configuración actual. No crear un segundo motor, DSL, tablas, microservicios, UI, migraciones ni capas nuevas salvo un defecto demostrado que lo exija.
2. **Un incremento por turno/PR.** Leer `MEMORY_STATE.md`, ejecutar solo `next_increment`, modificar los mínimos archivos necesarios, reportar resultado y actualizar el estado. No saltar al siguiente incremento en la misma ejecución.
3. **Seed = baseline vigente.** No asumir que alguien cambió reglas en base de datos: hasta nueva evidencia el mapping efectivo es el seed. No editar directamente reglas publicadas/inmutables ni sobrescribir el seed como sustituto de versionado; corregir la fuente en código si está equivocada y, cuando corresponda, publicar una versión nueva mediante el mecanismo existente.
4. **Bloqueo por ambiente no detiene el desarrollo.** Intentar una vez los tests/comandos pertinentes si la herramienta existe. Si falta SDK, Docker, DB, dependencia o endpoint: escribir `NOT_RUN_ENVIRONMENT` con comando/causa, realizar revisión estática y continuar con el siguiente incremento **independiente**. No instalar una infraestructura nueva ni gastar turnos en perseguir el ambiente. Una prueba que falla por un defecto de código (`FAILED_CODE`) sí debe corregirse antes de declarar el incremento implementado.
5. **UAT solo al final.** Durante incrementos: compilación y pruebas unitarias/fixture locales si están disponibles; **sin pruebas de aceptación de usuario ni llamadas a banco real**. Reservar UAT y homologación externa para `I07`, después del cierre técnico pendiente. `NOT_RUN_ENVIRONMENT` nunca equivale a `PASS`.
6. **Seguridad monetaria.** No llamadas productivas, no cuentas reales ni XML sensible en evidencia. Mantener los defaults `ProcTransacciones:Mode=DryRun`, `ProcContrapartidas:Mode=DryRun`, `UseContractDrivenRequest=false` para los tres hasta que se satisfagan los gates técnicos ya establecidos en `CFA-SOAP-UX-MAP-001/P8.4`. Si un mapping requerido no tiene semántica demostrada, registrar la duda, conservar el comportamiento seguro y avanzar en otros cambios; **no inventar datos** ni activar esa ruta en Live.
7. **No reabrir el programa anterior.** P0–P7 de `CFA-SOAP-UX-MAP-001` ya existen. Su P8.4 sigue pendiente de validaciones ejecutables; este plan **no lo declara cerrado** ni suplanta sus gates. No rediseñar SPA, navegación, IAM, NACHA-M, ciclos, causales, liquidación ni reconciliación ajenos a un fallo probado.

## 2. Forma mínima de trabajo por incremento

Cada incremento debe incluir cinco secciones en la respuesta: **objetivo**, **cambios concretos**, **evidencia**, **pendientes/bloqueos** y **nuevo `next_increment`**. No escribir informes extensos.

Estados válidos: `TODO`, `IN_PROGRESS`, `IMPLEMENTED`, `VERIFIED_LOCAL`, `NOT_RUN_ENVIRONMENT`, `BLOCKED_FUNCTIONAL`, `FAILED_CODE`; el estado global no será `UAT_APPROVED` sin aceptación externa.

En `MEMORY_STATE.md` registrar exclusivamente: último incremento terminado, siguiente incremento, archivos modificados, decisión tomada, evidencia local, bloqueos de entorno/funcionales, riesgos pendientes y bandera de Live. No copiar logs ni datos sensibles.

## 3. Secuencia fija — siete incrementos

### I01 — Cardinalidad y correlación de `Proc_Contrapartidas` **[IMPLEMENTADO, runtime pendiente]**

**Problema confirmado por lectura del snapshot:** `ContrapartidaDispatchJobService.ProcessPendingAsync` toma múltiples pendientes (`Take(chunkSize)`), hace una resolución y una invocación SOAP para `transactions`, mientras que `ProcContrapartidasFunctionalMappingResolver.TryResolveAsync` y el camino contract-driven seleccionan `transactions.OrderBy(Id).First...`; luego el servicio itera todos los `dispatchItems` para guardar respuestas. No considerar garantizada la correspondencia 1:1.

**Cambio mínimo:** una solicitud `Proc_Contrapartidas` debe corresponder a una sola `AchTransaction` y su resultado debe aplicarse únicamente a ese `AchTransactionId`. Se permite conservar el batch operacional como agrupador, pero cada petición y respuesta SOAP debe estar correlacionada individualmente. Elegir el menor diff seguro que preserve cola, leases/reclamación, auditoría, reintentos e idempotencia; no crear un nuevo scheduler. Eliminar el supuesto de “primera transacción representa todo el chunk”. Una respuesta global sin elementos individuales no puede aprobar otras transacciones diferentes de la enviada. En timeout/resultado incierto no reintentar automáticamente una transacción sin conciliación.

**Archivos focales:**
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ContrapartidaDispatchJobService.cs`
- `src/Cfa.ACHInterbank.Persistence/ACH/Services/Implementation/ProcContrapartidasRequestMapper.cs`
- `src/Cfa.ACHInterbank.Persistence/Integrations/Services/ProcContrapartidasFunctionalMappingResolver.cs` (solo si hace falta para hacer explícita una transacción)
- `tests/Cfa.ACHInterbank.Tests/ContrapartidaDispatchJobServiceTests.cs`

**Casos de prueba focales:** una pendiente → una petición; dos pendientes → dos peticiones con `OFIDTX/OFCTA` correctos y dos respuestas independientes; éxito/fallo mezclados; timeout incierto → no doble envío; paralelismo/reclamación → no duplicados. **Cierre:** no se atribuye una respuesta a otra transacción. No cambiar códigos financieros por cuenta de este incremento.

### I02 — Semántica de campos `Proc_Contrapartidas` **[IMPLEMENTADO, 5 campos por homologar]**

**Objetivo:** revisar SOLO los campos observados en el seed y corregir los que puedan comprobarse con evidencia de dominio/contrato:
`OFEMP` ← `ClearingHouses.Code`; `OFIDARCH` y `OFIDLOT` ← `AchBatchId`; `OFST="OO"`; `OFDIRECCIONIP="0.0.0.0"`; validar conjuntamente `OFDD`/`OFMONDEB`/`OFMONCRE`.

**Cambio mínimo:** tabla de decisión por campo (correcto / corregido / requiere homologación). Reutilizar los source providers ya disponibles y las transformaciones registradas. No inventar el código empresa, la IP, un ID bancario ni valores alternativos. Si falta la fuente oficial: dejar `BLOCKED_FUNCTIONAL` para ese **campo**, documentar el dato pendiente y continuar con I03; no habilitar Live de la operación afectada.

**Archivos focales:** `IntegrationMappingBootstrapper.cs`, `ProcContrapartidasFunctionalMappingResolver.cs`, tests actuales de resolver/mapping y `IntegrationMappingEndToEndTests.cs`. Cambiar seed/bootstrap solo si hay definición demostrada y se mantiene la compatibilidad/versionado.

**Cierre:** cada campo revisado tiene fuente y decisión explícita, y no existe fallback engañoso en campos obligatorios.

### I03 — Semántica de campos `Proc_Transacciones` **[IMPLEMENTADO, runtime pendiente / origen no homologado]**

**Objetivo:** validar `NCTAORIG` (seed: `EntryDetails.AccountNumber`), `NCTARECEP`, `IDORIG`, `IDRECEP`, `IDTRAN`, `IDLOTE`, `MONTO`, `DESTRAN`, `INFPAG` y fecha efectiva. Separar origen, receptor y descripción; no usar el número de cuenta receptora como “origen” por su nombre.

**Cambio mínimo:** resolver correctamente desde entidades ya existentes cuando la fuente esté demostrada; si no existe información de cuenta de origen confiable, no fabricar una. Preservar la construcción de 77 caracteres y restricciones NACHA-M vigentes; no tocar el parser ni layouts NACHA por defecto.

**Archivos focales:** `ProcTransaccionesRequestMapper.cs`, su resolver funcional/source provider, `IntegrationMappingBootstrapper.cs`, `ProcTransaccionesRequestMapperTests.cs` y tests de paridad existentes.

**Cierre:** casos diferenciados donde cuentas origen y destino sean distintas; el XML usa la fuente correcta, o queda pendiente homologación funcional explícita sin activar Live.

### I04 — Respuesta `RegistrarRespuestaTransaccion` **[IMPLEMENTADO, ejecución .NET pendiente]**

**Objetivo:** confirmar la trazabilidad `AchResponses` → `AchResponseNotificationAttempts` → los **siete** inputs SOAP (`idCanal`, `nombreCanal`, `idTransaccion`, `idEstado`, `causal`, `idTransaccionAxon`, `descripcionCausal`) → `existeError`, `codigoError`, `descripcionError` persistidos.

**Cambio mínimo:** corregir solamente inconsistencias demostradas entre el mapper, request writer, parser, estados y reintentos. Conservar `AchResponseNotificationAttempts`; no crear otra cola. Un acuse de transporte no equivale automáticamente a éxito funcional. No reenviar una respuesta definitiva ya notificada.

**Archivos focales:** `RegistrarRespuestaAchCommandMapper.cs`, `RegistrarRespuestaAchSoapRequestMapper.cs`, `NotificarRespuestaAchUseCase.cs`, `RegistrarRespuestaAchSoapResponseParser.cs`, `NotificarRespuestaAchUseCaseTests.cs`, `RegistrarRespuestaAchContractsTests.cs`.

**Cierre:** correlación y estados inequívocos para éxito, rechazo funcional, timeout y duplicado.

### I05 — Paridad mínima entre runtime legacy y contract-driven **[IMPLEMENTADO, ejecución .NET pendiente]**

**Objetivo:** comprobar con los tests ya existentes que los mappings corregidos entregan el mismo contrato intencional en ambas rutas, sin introducir un tercer camino.

**Cambio mínimo:** ajustar providers/parity y fixtures estrictamente afectados; aislar diferencias de `SOAPAction`, namespace, nombres, orden, vacíos, formatos y conversiones. Cuando hay cambio funcional deliberado, documentar el nuevo valor esperado y actualizar solo la prueba pertinente. No activar los tres `UseContractDrivenRequest` por una mera inspección estática.

**Archivos focales:** `IntegrationContractDrivenSoapBodyWriterTests.cs`, `IntegrationMappingEndToEndTests.cs`, políticas de paridad existentes y clases de options actuales.

**Cierre:** paridad verificable localmente cuando haya runtime; si no está disponible, `IMPLEMENTED / NOT_RUN_ENVIRONMENT` y seguir con I06. **Cutover continúa pendiente** hasta satisfacer P8.4.

### I06 — Cierre técnico y deuda de ambiente (sin UAT) **[IMPLEMENTADO, pruebas .NET pendientes]**

**Objetivo:** consolidar cambios, ejecutar gates que sea posible y producir una única lista corta de pendientes antes de UAT.

**Ejecución preferente en la raíz (Windows CMD):**

```cmd
python scripts/validate-solution-projects.py
dotnet build ACHInterbank.sln -c Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build
```

Filtrar primero los tests directamente afectados si la suite global tarda; revisar suites de contrapartidas, transacciones, respuesta y contrato. Reutilizar la matriz de pruebas P8.4 existente para migraciones dual-provider y no-code; **no duplicar el plan de certificación**.

Si faltan herramientas/DB: anotar cada gate `NOT_RUN_ENVIRONMENT` y la instrucción exacta pendiente; no reescribir código para saltarlo. `I06` puede cerrar **implementación técnica documentada**, pero no `VERIFIED_LOCAL` cuando faltan pruebas críticas. No activar Live/cutover por cumplir solo pruebas estáticas.

**Cierre:** inventario final por campo, diff controlado, evidencias, riesgos abiertos y paquete de UAT listo para ejecutar cuando exista ambiente.

### I07-A — Preparación UAT oficial **[PREPARADO, SIN EJECUTAR UAT]**

**Único alcance:** matriz específica de I01–I06, controles de entrada, guía de ejecución/evidencia, script local puramente estático. **No** se ejecutan transacciones con el banco, no se cambia código monetario, no se activa Live ni `UseContractDrivenRequest`. Ver `I07A_PREPARACION_UAT_SOAP.md`, `I07A_MATRIZ_CASOS_SOAP.md`, `I07A_EVIDENCIA_Y_BLOQUEOS.md`.

**Cierre I07-A:** documentación, lista de 18 escenarios y estado de bloqueos listos; esto **no** es `UAT_READY`, homologación ni PASS de xUnit. La falta de SDK/banco no impide cerrar la **preparación**.

### I07-B — UAT y homologación externa oficial **[PENDIENTE POR PRERREQUISITOS]**

**Prerequisitos:** gates técnicos/cutover requeridos disponibles y aprobados; contrato y fuente funcional homologados por responsables; ambiente UAT autorizado. No ejecutar en incrementos I01–I06.

**Escenarios:** 1 y N transacciones; cuentas origen/destino diferentes; débito/crédito; respuestas de éxito, rechazo, SOAP fault, timeout e incertidumbre; idempotencia/reproceso; conciliación; lectura de auditoría y versionado; prueba de rollback. Registrar evidencias sin datos bancarios sensibles.

**Salida posible:** `UAT_APPROVED` solo con aceptación formal; de lo contrario `UAT_PENDING`/`UAT_FAILED`. Sin autorización de producción implícita.

## 4. Gestión de bloqueos sin perder continuidad

| Bloqueo | Acción | ¿Detiene el código siguiente? | ¿Autoriza Live/UAT? |
|---|---|---|---|
| No está instalado .NET | Marcar test `NOT_RUN_ENVIRONMENT` y seguir con otro incremento | **No** | **No** |
| Docker/SQL Server/PostgreSQL indisponible | Diferir prueba de integración y conservar fixture/contrato | **No** | **No** |
| No responde WSDL/XSD o banco | Usar contrato disponible y marcar campo no homologado | **No**, continuar otros campos/servicios | **No** |
| Falta significado de un campo obligatorio | Dejar pendiente funcional, sin valor inventado ni Live de esa ruta | **No**, continuar componentes independientes | **No** |
| Test reproducible falla por un bug nuestro | Corregir bug antes de marcar implementado | **Sí, para ese incremento** | **No** |
| Respuesta incierta a movimiento monetario | Mantener estado de conciliación/manual y prohibir retransmisión ciega | **No** para el desarrollo independiente | **No** |

## 5. Prompt de inicio — copiar en la próxima sesión de implementación

> Continúa `SOAP-MAP-SAFE-001` en el repositorio ACHInterbank. Lee `AGENTS.md`, `docs/CFA_SOAP_MAPPING_HARDENING_2026-11-01/MEMORY_STATE.md`, y únicamente la sección `next_increment` de `PLAN_INCREMENTAL.md`; consulta `MEMORY.md` raíz y el paquete previo CFA-SOAP-UX solo para decisiones pertinentes. Ejecuta **exactamente un incremento** (según `next_increment` vigente). Mínimo diff, sin arquitectura nueva, sin cambios normativos NACHA y sin UAT hasta I07. Si una herramienta/DB/endpoint no existe, registra `NOT_RUN_ENVIRONMENT` y sigue con el desarrollo que no dependa de ello; no simules PASS. No actives Live, no hagas llamadas externas, no cambies `UseContractDrivenRequest` todavía. Al terminar actualiza `MEMORY_STATE.md` con archivos, decisiones, evidencia, bloqueos y siguiente incremento; reporta el resultado compacto.

## 6. Referencias internas que deben prevalecer

- `AGENTS.md` y `MEMORY.md` del proyecto.
- `docs/CFA_SOAP_UX_MAPPING_IMPLEMENTATION_PACKAGE_2026-10-12/MEMORY_STATE.md` (P8.4 pendiente).
- `docs/CFA_SOAP_UX_MAPPING_IMPLEMENTATION_PACKAGE_2026-10-12/P8_4_EXTERNAL_VALIDATION_HANDOFF.md` (gates técnicos del motor; sin reimplementarlos).
- Contratos WSDL `WSCFAACH.wsdl (1).xml` y `WSAxonRespuestaTransacciones.wsdl (1).xml` disponibles en los adjuntos; **importan esquemas externos no incluidos**, por lo cual no sustituyen una homologación bancaria completa.
- Matriz de mapping campo a campo `ACHInterbank_Mapping_SOAP_Campo_a_Campo.xlsx`; baseline documental, no evidencia de ejecución en base productiva.

## Estado después de I06 (2026-11-01)

- I01–I05 siguen `IMPLEMENTED / NOT_RUN_ENVIRONMENT`, con homologaciones funcionales pendientes en I02/I03.
- I06 culminó inventario, revisión estática y lista priorizada; solución 7/7 y P8.1 PASS estáticos; P8.3 inconcluso por timeout; compilación y tests sin SDK .NET.
- I07 figura como próximo *por secuencia*, pero `UAT_PENDING_PREREQUISITES`: **no** iniciar sesiones de UAT hasta cerrar defectos/gates y disponer de autorización/homologación.
- `appsettings.Development.json` contiene `Mode=Live` y configuración SOAP local: revisar configuración efectiva antes de cualquier ejecución externa.
- Fuentes concretas: `CIERRE_TECNICO_I06.md`, `INVENTARIO_CAMPOS_SOAP_I06.md`, handoff original P8.4.

## Estado después de I07-A (2026-11-01)

- I07-A documentado, sin pruebas bancarias ni cambios productivos. Todos los casos preparados permanecen PENDING/BLOCKED.
- `last_completed_increment=I07-A`, `next_increment=I07-B`; ejecución condicionada a gates .NET/P8.4, homologación funcional y autorización.
- Ningún gate técnico pendiente queda convertido en PASS por la preparación. Live, cutover y producción no autorizados.
