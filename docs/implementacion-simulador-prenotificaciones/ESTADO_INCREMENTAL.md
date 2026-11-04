> **Actualización vigente I07-R1:** robustecimiento de UI y descarga incorporado; 20/20 verificaciones estáticas nuevas. La fase **I07-R2** de ejecución runtime sigue pendiente. Para el tablero consolidado revisar `ESTADO_I07R1.md`; ninguno de PRE-01..PRE-14 está homologado.

# Estado de implementación — simulador de respuestas a prenotificaciones

**Corte:** 2026-11-02 · **Entrega vigente:** I07 · **Versión funcional:** correcciones de ciclos y concurrencia visual, junto con SPA I06; runtime no homologado.

## Leyenda obligatoria

- `COMPLETADO`: cambios y evidencia requeridos para el incremento incluidos y revisables.
- `EN_PROGRESO`: cambios parciales, faltan criterios de cierre.
- `PENDIENTE`: no se han implementado los cambios de este incremento.
- `NO_EJECUTADA_POR_AMBIENTE`: una prueba concreta fue imposible de ejecutar; **no equivale a PASS, ni bloquea trabajo estático seguro**.
- `FALLA`: prueba ejecutada con resultado no conforme.
- `PASS`: prueba ejecutada exitosamente con comando, salida y artefacto verificables.

## Tablero detallado

| ID | Entregable verificable | Estado | Faltante para terminar |
|---|---|---|---|
| I00 | ZIP íntegro + documentación + `MEMORY_STATE.md` + tablero + referencias | **COMPLETADO (documental)** | No exige código de nueva función |
| I01 | Contrato API/DTO, resultados Returned vs NoReturnByDeadline, enum compatible, casos de uso, tests de contrato | **EN_PROGRESO** (código implementado, 20 verificaciones estáticas PASS) | Falta compilar y ejecutar Xunit cuando exista .NET; no habilitar ejecución sin I02/I03 |
| I02 | Consulta y preflight de prenotificaciones originadas; estado, cámara, banco, ciclo, transmisión y resultado | **EN_PROGRESO** (código, 26/26 PASS_STATIC) | Compilar y correr Xunit/API; reserva idempotente y revalidación de Generate quedan para I03; no se habilita ejecución |
| I03 | Devolución NACHA-M (ACHCOL y CENIT), secuencia/archivo/evidencia | **EN_PROGRESO** (ruta y generador incorporados; 31/31 PASS_STATIC) | Compilar y ejecutar Xunit, fixtures, importación de archivo real, concurrencia de BD y recuperación de huérfanos |
| I04 | NachaUpload correlaciona devolución y sincroniza intento de validación sin dobles efectos | **EN_PROGRESO** (guarda y pruebas C# incorporadas; 31/31 PASS_STATIC) | Compilación .NET, Xunit, prueba de integración real, transformaciones por cifrado, reupload y concurrency BD |
| I05 | Sin devolución: evaluación virtual y aceptación tácita al vencimiento real, motor oficial | **EN_PROGRESO** (código y 36/36 PASS_STATIC) | Compilar, Xunit, E2E SQL Server/PostgreSQL, feriados, concurrencia con Quartz, aceptación y rollback |
| I06 | SPA Angular guiada, búsqueda, preflight, generación, reloj virtual, descarga JWT blob | **EN_PROGRESO** (código + 5 tests incorporados; 32/32 PASS_STATIC) | Instalar dependencias, compilar Angular, ejecutar Karma/Playwright, verificar descarga autenticada y accesibilidad real |
| I07 | Regresión y cierre controlado | **EN_PROGRESO** (correcciones de código; 24/24 PASS_STATIC; total 200/200 estáticas) | .NET/Angular/BD/Playwright y PRE-01..PRE-14 en ambas cámaras; fase I07-R, no homologado |

## Bloqueos no terminales

- El entorno de chat puede no tener SDK .NET, Node, BD Docker o acceso a UAT. **Continuar** análisis de código, cambios controlados y pruebas ejecutables; clasificar por separado lo no ejecutado.
- Para cierre final de I07 y habilitación UAT se requieren evidencias runtime reales. La falta de ambiente impide declarar verificación de esas pruebas, **no** preparar sus cambios ni entregar el ZIP parcial.
- La generación diferencial no homologada debe permanecer deshabilitada hasta que el flujo de devolución correcto esté probado.

## Resumen cuantitativo

- **8 hitos** contemplados (I00 preparación + I01–I07 funcionales).
- **1 hito documental completado** (I00).
- **6 incrementos (I01–I06) con código entregado pero sin cierre runtime**.
- **7 incrementos EN_PROGRESO (I01–I07) y ninguno homologado en runtime**.
- **14 pruebas PRE definidas; 0/14 ejecutadas/homologadas en UAT. I07 agrega pruebas C#/Angular pendientes de ejecución.**
- **12 hallazgos heredados abiertos** según auditoría SPA del 2026-11-02; revisar y corregir los que intersecten este alcance en sus incrementos.

## Formato obligatorio al cerrar cada próximo incremento

Actualizar este tablero, `MEMORY_STATE.md` y `CHANGELOG_INCREMENTAL.md`; registrar lista exacta de archivos nuevos/modificados, pruebas **PASS/FAIL/NO_EJECUTADA_POR_AMBIENTE** con comando y motivo, pendientes abiertos, próximo ID y hash del ZIP entregado. Adjuntar **siempre ZIP completo**, preservando la línea base.

## Evidencia específica I01

- `PASS` — `python scripts/validate-solution-projects.py`: `SOLUTION GRAPH: OK`, 7/7 proyectos.
- `PASS_STATIC` — `python scripts/test/prenotification_simulator_i01_static_check.py`: 20/20 invariantes estructurales. No es compilación ni prueba de comportamiento.
- `NO_EJECUTADA_POR_AMBIENTE` — `dotnet build ACHInterbank.sln -c Release`: `dotnet: command not found`.
- `NO_EJECUTADA_POR_AMBIENTE` — `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --filter ...`: falta SDK; pruebas Xunit incorporadas sin afirmar aprobación.
- Ruta genérica de generación nueva conserva bloqueo `PRENOTIFICATION_RETURN_NOT_IMPLEMENTED`; I03 introduce endpoint dedicado con revalidación. El modo NoReturn no produce archivo.
- Siguiente trabajo implementable sin esperar al entorno: **I03 — generación normativa NACHA-M + reserva de idempotencia**, revisando asimismo la compilación de I01 cuando exista SDK.

## Evidencia específica I02

- `PASS`: `python scripts/validate-solution-projects.py` — grafo 7/7.
- `PASS_STATIC`: `python scripts/test/prenotification_simulator_i02_static_check.py` — 26/26 (consultas, guards, endpoints, casos adversariales incorporados); ver `EVIDENCIA_I02.txt`.
- `NO_EJECUTADA_POR_AMBIENTE`: `dotnet build` y `dotnet test` (SDK ausente); las pruebas C# añadidas **no** están aprobadas.
- `BusinessEligible` es diagnóstico read-only, `ExecutionEnabled=false`; Generate conserva bloqueo `PRENOTIFICATION_RETURN_NOT_IMPLEMENTED`.
- I02 no reserva `IdempotencyKey` ni modifica estados: la reserva única y revalidación bajo concurrencia es responsabilidad de I03.

## Evidencia específica I03

- `PASS`: grafo solución 7/7.
- `PASS_STATIC`: I03 31/31; regresiones I01 20/20 e I02 26/26.
- Endpoint `prenotifications/generate-return` incorpora reserva durable UNIQUE y revalidación serializable; genera solamente archivo en UAT/local y no importa.
- `NO_EJECUTADA_POR_AMBIENTE`: compilación, Xunit, PostgreSQL/SQL Server, generación de fixture NACHA-M y NachaUpload E2E. El código **no está homologado**.
- `I04`: guarda implementada, correlación SHA y trace original; integración real, estados e idempotencia pendientes de ejecución .NET/BD.

## Evidencia específica I04

- `PASS`: grafo .sln 7/7; `PASS_STATIC`: I04 31/31 más I01 20/20, I02 26/26, I03 31/31.
- Pruebas C#: 12 casos de guarda + 2 pruebas de orquestación añadidas, **NO_EJECUTADA_POR_AMBIENTE**.
- Correlación por hash SHA normalizado (I03 mayúscula / importador minúscula), banco, cámara, causal, trace y pertenencia; bloqueo manual antes de mover estado.
- Reutiliza `AchStateTransitionService` para sincronizar validación y tercero; no se introduce otro updater.
- `NO_EJECUTADA_POR_AMBIENTE`: compilación, casos Xunit, importación E2E y concurrencia multibase. Pendiente verificar archivos transformados al descifrar.
- Siguiente: **I05**, ruta sin devolución / aceptación tácita con tiempo controlado.

## Evidencia específica I05

- `PASS`: grafo solución 7/7. `PASS_STATIC`: I05 36/36; regresión acumulativa I01–I05 **144/144**.
- Preview `prenotifications/tacit-acceptance/preview`: solo lectura y reloj virtual, con motivos; no genera NACHA-M positivo.
- `apply-due`: permite transición **real** solo tras vencimiento auténtico y con actor autorizado en UAT/local; motor oficial e idempotencia por intento.
- `NO_EJECUTADA_POR_AMBIENTE`: compilación, Xunit, bases de datos, Quartz, E2E. No declarar I05 cerrado por runtime ni homologado.
- Próximo **I06**: conectar SPA, selección, resumen, advertencias, descarga JWT de devoluciones y confirmación explícita al aplicar vencimiento real.

## Evidencia específica I06

- **PASS_STATIC 32/32:** `python scripts/test/prenotification_simulator_i06_static_check.py` (balance de HTML, sintaxis de TS, contrataciones, seguridad de descarga, guardas, estados y responsividad).
- **PASS_STATIC 144/144** acumulativos I01–I05; **PASS** grafo .sln de 7 proyectos.
- SPA de tres pasos, una prenotificación por simulación, opción devolución de archivo vs ausencia de devolución, API actual, causales y ciclo, confirmaciones, descarga blob autenticada, enlace manual NachaUpload, estados y responsividad.
- Cinco pruebas unitarias de interfaz añadidas; **NO_EJECUTADA_POR_AMBIENTE** compilación/Karma/Playwright porque `npm ci --offline` falló `ENOTCACHED zone.js@0.16.3`, sin node_modules. Backend .NET tampoco ejecutado.
- Sin nuevas dependencias, rutas o actualizaciones de esquema. Riesgo de ciclos aplicables y catálogos por operador pendiente de UAT I07.
- **Próximo I07:** ejecutar y corregir integración real, fixtures, visual/regresión, roles, estados, mobile y evidencia PRE-01..PRE-14.

## Evidencia específica I07 — cierre técnico sin homologación

- `PASS_STATIC`: I07 24/24, I01–I06 176/176, total **200/200**. Grafo solución .sln **PASS 7/7**.
- Correcciones puntuales: ciclo de devolución puede no tener transacciones previas (mantiene vigencia/configuración y excluye cancelados); SPA ignora respuestas tardías de consultas/catálogos; vista previa tácita respeta los mismos guardas de aplicación real.
- Incorporadas pruebas C# (ACHCOL/CENIT/cancelado) y Angular (cámara cambiada/cierre/catálogo obsoleto) sin afirmar ejecución.
- **NO_EJECUTADA_POR_AMBIENTE** .NET/Xunit (no existe SDK), Angular/Karma/Playwright (`npm ci --offline`: ENOTCACHED zone.js), BD real y NachaUpload E2E.
- PRE-01 a PRE-14 **pendientes de homologación**; ningún hallazgo de la auditoría previa se declara cerrado por estas verificaciones.
- Siguiente actividad **I07-R**: homologación runtime y cierre comprobable, sin añadir funciones ni modificar producción; conservar ZIP I07.
