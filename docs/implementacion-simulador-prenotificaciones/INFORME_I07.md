# Informe técnico I07 — regresión y cierre controlado del simulador de prenotificaciones

**Fecha de corte:** 2026-11-02  
**Base única:** ZIP completo I06. **Entrega:** I07 acumulativo.  
**Resultado:** correcciones y pruebas de regresión incorporadas; **NO homologado en runtime**.

## Alcance, sin sobreingeniería

Se mantienen los componentes y pantallas existentes, la descarga autenticada y el mecanismo manual NachaUpload. Sin migraciones, bibliotecas, rutas Angular, nuevas tablas, importaciones automáticas ni transmisiones externas.

### Cambios concretos

1. **Ciclos válidos disponibles para el retorno de prenotificación:** `NachaInboundSimulationService.AvailableCycleQuery` conserva filtros de cámara activa, configuración vigente y ciclo no cancelado, pero no exige transacciones preexistentes en un ciclo de devolución aún por construir. Este cambio aplica **únicamente** a `IncomingPrenotificationReturn`; los demás escenarios conservan el requisito original. `ListAvailableCyclesAsync` comunica cero transacciones de retorno existentes, no contabiliza créditos/debitos ajenos como si fueran devoluciones. La validación posterior de ciclo, orden, fecha y causal sigue siendo autoridad del backend.
2. **UX sin respuestas obsoletas:** la SPA invalida resultados pendientes cuando se cambia cámara/búsqueda, cierra el flujo o selecciona otra prenotificación. Consulta y catálogos usan versiones locales simples, sin dependencias ni manejo complejo de estado. Limpia la lista mientras se consulta y bloquea seleccionar una fila durante la carga.
3. **Aplicación tácita veraz:** el indicador de ejecución de la vista previa usa las **mismas** condiciones de entorno que `apply-due` (UAT/local, sin transmisión, sin autoimportación y motor canónico disponible). No usa el reloj virtual para aplicar cambios ni genera aprobación ficticia.
4. **Pruebas:** 3 casos C# nuevos en `NachaInboundSimulatorTests.I07.cs` (ACHCOL, CENIT y ciclo cancelado); 3 pruebas Angular contra respuestas tardías; verificador estático I07 de 24 controles. Las nuevas pruebas C#/Angular **se incorporaron, no se ejecutaron**.

## Evidencia reproducible

- `python scripts/test/prenotification_simulator_i07_static_check.py`: **24/24 PASS_STATIC** (incluye parser AST TypeScript, HTML y guardas; no verifica compilación Angular ni comportamiento SQL).
- Regresión de las comprobaciones estructurales de I01–I06: **176/176 PASS_STATIC**.
- `python scripts/validate-solution-projects.py`: **PASS** grafo de 7 proyectos; no equivale a compilación.
- `dotnet --version`: **NO_EJECUTADA_POR_AMBIENTE**, no existe SDK .NET.
- `npm ci --offline --ignore-scripts --no-audit --no-fund`: **FALLÓ INSTALACIÓN** por `ENOTCACHED zone.js@0.16.3`; por tanto `ng build`, Karma y Playwright **NO EJECUTADOS**.
- API real, PostgreSQL/SQL Server, importación/duplicado NachaUpload, validación NACHA-M por cámara y horarios: **NO EJECUTADOS**. Sin evidencias de los efectos runtime.

## Matriz PRE-01..PRE-14

Se conserva el estado de **14/14 PENDIENTE DE PRUEBA FUNCIONAL**. I07 añadió cobertura de código y pruebas aún no ejecutadas de `PRE-10`, `PRE-13` y ciclos de retorno, pero **ningún PRE queda cerrado** sin ejecución real. Véanse `MATRIZ_ACEPTACION.md` y `PLAN_EJECUCION_UAT_I07.md`.

## Riesgos que impiden homologación

1. Correlación real del archivo generado con el parser NACHA-M, la adenda 99 y el estado del intento, en ambas cámaras.
2. Concurrencia transaccional de la reserva idempotente, escritura de evidencia y reupload, bajo PostgreSQL/SQL Server.
3. Compatibilidad de horarios y causales configuradas, diferencias ACH Colombia/CENIT, feriados y ciclo permitido.
4. Interacción real con JWT/roles/descarga en navegador, responsive, lectores de pantalla y confirmación modal.
5. Regresiones heredadas del simulador (incluida la auditoría previa de 12 hallazgos): no se proclama cierre global ni se supone que quedan resueltas fuera de este alcance.

## Dictamen

**I07 — EN_PROGRESO, código y revisión estática incorporados. NO APTO PARA DECLARAR HOMOLOGACIÓN UAT/PRODUCCIÓN** hasta completar las pruebas de ejecución. Continuar por una fase de **I07-R (homologación runtime de I07, sin nuevas funciones)** cuando haya .NET, Node y BD disponibles. Si este chat sigue sin esas herramientas, preservar el ZIP I07 y la matriz de pendientes sin declarar falsos PASS.
