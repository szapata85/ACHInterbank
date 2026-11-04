# Evidencia I00 — inventario e integridad documental

**Fecha:** 2026-11-02. **Alcance:** revisión estática del ZIP y preparación del plan. **Código fuente:** sin modificaciones.

## Observaciones verificadas

- Archivo ZIP base existe y se extrajo correctamente en entorno de trabajo.
- Contiene 4.767 entradas y árbol raíz `ACHInterbank_ajustado/`.
- Existe `AGENTS.md`, `MEMORY.md`, `MEMORY_STATE.md` y numerosos estados por iniciativa.
- `AchReturnsController` ofrece consulta de transacciones de un ciclo y generación de archivo por solicitud.
- `AchReturnsService` tiene validación de elegibilidad, causal, registro `AchReturnGenerated`, controles de concurrencia y llamada a transición; no basarse en informes antiguos para negar ese código.
- `IncomingNachaPostParseProcessor` maneja respuestas entrantes y correlación; I02 deberá localizar el verdadero evento de fallo de aplicación **de una transacción recibida** antes de disparar devolución saliente.
- Existe pantalla de devoluciones con Angular Material y pruebas de componente.
- Se conservan restricciones previas de homologación/NO-GO, especialmente CENIT saliente y simulador de prenotificaciones.

## Ambiente de I00

- Python disponible: sí.
- .NET SDK invocable: no localizado.
- Node/npm disponibles: sí; no se instaló ni compiló Angular porque I00 no modificó código.
- Integración real/cámaras/SQL Server: NO EJECUTADA.
- Xunit, Karma, Playwright, homologación: NO EJECUTADA (fuera de alcance I00).
- Se realizaron: lectura ZIP, búsqueda localizada de código/memoria, revisión de rutas existentes, empaquetado e integridad de ZIP.

## Límites del resultado

La existencia de un servicio o prueba no demuestra que la funcionalidad de devoluciones automáticas esté activa o homologada. No se agregaron endpoints, configuraciones, migraciones, tareas ni cambios UI en I00. Siguiente trabajo I01.
