# I07-R1 — Fortalecimiento de regresión sin homologación funcional

**Fecha:** 2026-11-02 · **Base:** ZIP completo I07 · **Alcance:** correcciones mínimas, sin cambios NACHA-M ni migraciones.  
**Resultado:** código actualizado; **0/14 pruebas PRE homologadas**. No confundir comprobaciones estáticas con pruebas E2E.

## Cambios de código confirmados

1. SPA `nacha-inbound-simulator.component.ts`: cada preview de respuesta captura versión, operación, cámara e ID original. Al cambiar opción/causal/señal de detalle, la respuesta en vuelo se descarta. No se vuelve a interpretar una respuesta de devolución como de aceptación tácita.
2. Backend `NachaInboundSimulationService.cs`: antes de marcar `Downloaded`, lee el archivo y coteja **longitud y SHA-256** contra la evidencia de la simulación. Si difiere, lanza error explícito y no devuelve contenido corrupto.
3. API `NachaInboundSimulatorController.cs`: convierte fallos controlados de integridad de descarga en HTTP 422 (`ProblemDetails`). No se revelan bytes del archivo corrupto.
4. Pruebas: 2 casos Angular para respuestas tardías; 2 variantes xUnit para ACHCOL/CENIT, con corrupción tanto de longitud como de contenido de tamaño idéntico. Test estático I07-R1.

**Preservado:** misma SPA de tres pasos, mismo motor de estado, generación original, NachaUpload manual, autorización/descarga JWT, idempotencia y distinción ACH Colombia/CENIT. No se agregaron dependencias, bases de datos, rutas ni componentes.

## Evidencia realmente ejecutada

| Comprobación | Resultado | Alcance real |
|---|---|---|
| `python scripts/validate-solution-projects.py` | PASS 7/7 | Solo referencias de proyectos |
| Scripts estáticos I01–I07 | PASS 200/200 | Inspección de código, no comportamiento |
| `python scripts/test/prenotification_simulator_i07r_static_check.py` | PASS 20/20 | Nuevas guardas/contratos escritos |
| TypeScript `transpileModule` global (fuente/spec) | PASS 2/2 | Análisis sintáctico, no Angular build |
| `dotnet --info` | NO_EJECUTADA_POR_AMBIENTE: exit 127 | SDK ausente |
| `npm ci --offline` | BLOQUEADA: ENOTCACHED | zone.js@0.16.3 no está en caché |
| Angular/Karma/Playwright/.NET/SQL/NachaUpload E2E | NO_EJECUTADA_POR_AMBIENTE | Sin herramientas/servicios requeridos |
| PRE-01..PRE-14 | 0/14 HOMOLOGADAS | Se necesitan UAT y evidencias por cámara |

## Riesgos y pendientes

- Es **obligatorio** compilar y ejecutar los tests C#/Angular en un ambiente con dependencias; sintaxis TS y checks estáticos no prueban compilación de Angular ni persistencia.
- Validar descargas reales autenticadas, generación NACHA-M oficial, causal, trace, importe cero, concurrencia y reingesta con NachaUpload; verificar reloj/ciclos/feriados por cámara.
- Probar UX visual en 375/768/1440px y roles con JWT vencido/denegado; no hay capturas nuevas en navegador.
- Los 12 hallazgos históricos de la auditoría SPA no se declaran cerrados globalmente por I07-R1.
- Si UAT sigue inaccesible, continuar código seguro y casos reproducibles, pero no activar flujo productivo ni certificar PRE.

## Próximo paso

**I07-R2: homologación ejecutada** en entorno habilitado (dotnet, npm, DB/UAT, usuarios y fixture). Registrar por cada PRE código, comando, archivo/hash, resultado, captura/log y responsable; remediar solamente fallas verificadas y volver a entregar ZIP acumulativo.
