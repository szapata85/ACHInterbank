# Informe de implementación I06 — SPA de respuestas a prenotificaciones

**Fecha:** 2026-11-02  
**Base:** ZIP completo I05 (no versión antigua F08)  
**Clasificación:** Código incorporado; comprobaciones estáticas aprobadas; compilación, Karma/Playwright y backend real no ejecutados.

## Objetivo y criterios de UX

Integrar, sin agregar pantallas ni dependencias, la selección y simulación de resultados de prenotificaciones realmente originadas por ACH Interbank desde la SPA `/uat/nacha-inbound-simulator`. Una persona debe entender la diferencia entre **devolución explícita** y **ausencia de devolución** sin aprender un flujo nuevo complejo.

- Se añade un acceso claro «Responder prenotificaciones enviadas» a la tarjeta existente «¿Qué desea simular?». El formulario legado se oculta únicamente mientras se utiliza el flujo nuevo; se conserva completamente.
- Flujo en **tres pasos**: (1) buscar y seleccionar una prenotificación; (2) elegir devolución o vencimiento; (3) previsualizar y confirmar. Una operación por vez, para reducir errores de selección de ciclos y bancos.
- Lista paginada con referencia, trace, banco, cuenta enmascarada, fecha, elegibilidad y motivo de exclusión.
- «Devolución»: catálogo de causales aplicables a prenotificación, ciclo de respuesta, preflight del backend y confirmación antes de generar archivo. Al terminar ofrece descarga autenticada e ingreso manual a NachaUpload.
- «Sin devolución»: hora virtual **solo para vista previa**; la aplicación requiere reloj real, elegibilidad confirmada por API y modal de confirmación. Sin fichero NACHA de aprobación.
- Se oculta en el selector el escenario legado `IncomingPrenotificationResponse` que permanece inhabilitado; contrato de servicio y representación en historial conservados.
- Tratamiento de espera, error, vacío, duplicación, vencimiento, control de acciones y diseño móvil utilizando clases y tokens existentes.
- La descarga ahora usa `HttpClient.get(..., {responseType:'blob'})` con interceptores de autenticación, enlace temporal y limpieza, en vez de `window.open()` sin JWT.

## Cambios fuente

1. `nacha-inbound-simulator.service.ts`: DTOs tipados y llamadas a cinco rutas I02–I05, descarga blob autenticada.
2. `nacha-inbound-simulator.component.ts`: selección, previsualización de ambas ramas, confirmación, idempotencyKey, recarga e indicaciones, descarga compartida.
3. `nacha-inbound-simulator.component.html`: tres etapas y tratamientos UX accesibles; formulario histórico preservado.
4. `nacha-inbound-simulator.component.scss`: estilos del tema existente y adaptación responsiva sin nueva biblioteca.
5. `nacha-inbound-simulator.component.spec.ts`: cinco pruebas unitarias nuevas para consulta, devolución, reloj virtual, vencimiento real y exclusiones.

## Verificación reproducible

| Prueba | Resultado | Evidencia/limitación |
|---|---|---|
| `python scripts/test/prenotification_simulator_i06_static_check.py` | **32/32 PASS_STATIC** | HTML equilibrado, TypeScript sintaxis, rutas, guardas, usabilidad, ausencia de `window.open` |
| `python scripts/validate-solution-projects.py` | **PASS 7/7** | Grafo .sln; no constituye compilación |
| `python scripts/test/prenotification_simulator_i01..i05_static_check.py` (cada uno) | **144/144 PASS_STATIC** | Regresión de invariantes ya existentes |
| `npm ci --offline --ignore-scripts --no-audit --no-fund` | **NO EJECUTADA POR AMBIENTE (instalación incompleta)** | `ENOTCACHED` para `zone.js@0.16.3`; no existe `node_modules` |
| `npm run build` / Karma / Playwright | **NO EJECUTADA POR AMBIENTE** | Requiere instalar dependencias y entorno UAT; las pruebas TS están incorporadas sin afirmar que pasaron |
| `dotnet build`, API real, NachaUpload E2E | **NO EJECUTADA POR AMBIENTE** | SDK .NET/servidores no disponibles en el entorno de esta entrega |

## Riesgos y pendientes I07

- Compilar Angular 21 y ejecutar Karma con las nuevas pruebas, más Playwright a 375px, 768px y escritorio.
- Probar guardas de permisos y sesión expirada al descargar; confirmar Content-Disposition y extensión correcta.
- Ejecutar un fixture real de devolución por cada cámara y realizar carga manual completa por NachaUpload; validar persistencia, causal, trace, SHA y transiciones.
- Probar que el endpoint `available-cycles` liste realmente ciclos de devolución posteriores para cada cámara y fecha, pues se reutilizó su filtro actual (requiere transacciones elegibles en el ciclo). Si no expone ciclos correctos, corregirlo en I07 sin inventarlos en frontend.
- Probar efectivamente la elegibilidad/códigos para prenotificaciones CENIT y ACH Colombia. UI muestra vacíos como configuración no disponible, no activa una devolución no homologada.
- La descarga por blob depende del interceptor JWT, no fue ejecutada en navegador.
- Mantener cerradas condiciones de producción y no declarar certificación UAT sin ejecutar PRE-01 a PRE-14.

**Siguiente incremento:** I07 regresión, verificación de runtime y cierre controlado; si el entorno continúa bloqueado, continuar con fixtures y correcciones estáticas sin declarar falsos PASS.
