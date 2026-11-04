> Estado vigente: **I06**. Código SPA implementado y pruebas estáticas 32/32 PASS; compilación/ejecución Angular y E2E pendientes. Próximo incremento **I07**. Leer `MEMORY_STATE.md`.

**Entrega vigente: I05, derivada acumulativamente de I04.** Se incorpora reloj virtual read-only y aplicación solo a vencimientos REALES en UAT; no se ha ejecutado .NET, ni se ha certificado E2E. Ver `docs/implementacion-simulador-prenotificaciones/INFORME_I05.md` y `MEMORY_STATE.md`.

**Entrega vigente: I01, derivada acumulativamente de I00.** El contrato está implementado en código; no hay ejecución habilitada de devoluciones ni de aceptación tácita. `dotnet` no estuvo disponible para compilar; consultar `INFORME_I01.md`.

# ENTREGA — ACH Interbank / Simulador de respuestas a prenotificaciones

Fecha: 2026-11-02  
Incremento entregado: **I00 — Línea base empaquetada y documentada**  
Estado funcional de la nueva capacidad: **NO IMPLEMENTADA EN I00**.

## Contenido

Este ZIP conserva **todas las entradas del ZIP completo CENIT F08** de origen, incluyendo código, soluciones .NET, SPA Angular, scripts, migraciones, tests, documentación y evidencias preexistentes. Se agregaron documentos de planificación, continuidad, auditoría y referencias proporcionadas en la conversación, sin retirar ni cambiar archivos del proyecto original.

### Documentos nuevos principales

- `MEMORY_STATE.md`: memoria portátil, estado técnico y continuación entre chats.
- `docs/implementacion-simulador-prenotificaciones/PLAN_IMPLEMENTACION_2026-11-02.md`: ejecución por I01–I07.
- `docs/implementacion-simulador-prenotificaciones/AUDITORIA_BASE_SPA_2026-11-02.md`: hallazgos preexistentes.
- `docs/implementacion-simulador-prenotificaciones/ESTADO_INCREMENTAL.md`: estado y requisitos de cierre **por incremento**.
- `docs/implementacion-simulador-prenotificaciones/GUIA_CHATGPT_MODO_CHAT.md`: protocolo sin dependencia de runtime como condición de avance.
- `docs/implementacion-simulador-prenotificaciones/MATRIZ_ACEPTACION.md`: escenarios PRE-01 a PRE-14.
- `docs/implementacion-simulador-prenotificaciones/CHANGELOG_INCREMENTAL.md`: modificaciones realizadas.
- `docs/implementacion-simulador-prenotificaciones/INFORME_I00.md`: evidencia y alcance de esta entrega.
- `docs/implementacion-simulador-prenotificaciones/ESTADO_INCREMENTAL.json`: estado legible por herramientas.
- `docs/implementacion-simulador-prenotificaciones/evidencias/`: script y salida de auditoría estática previa.
- `docs/implementacion-simulador-prenotificaciones/referencias/`: documentos regulatorios y técnicos provistos externamente, copiados sin modificación.

El proyecto ya incluía archivos llamados `MEMORY.md` y otros `MEMORY_STATE.md` de distintas iniciativas. No se sobreescribieron.

## Estado y significado de I00

I00 prepara el paquete acumulativo completo y el protocolo. **No** pretende corregir los 12 hallazgos anteriores, generar archivos de prenotificación nuevos, ejecutar pruebas de runtime ni cerrar I01–I07.

## Uso en futuros incrementos

Se requiere entrega acumulativa **de ZIP completo en cada turno**. La siguiente ejecución debe partir de este ZIP, ejecutar I01 y producir `..._I01_...zip` con **código modificado, pruebas, documentos actualizados, MEMORY_STATE y estado de cierre**, no un ZIP reducido de diffs.

No parar la implementación segura porque falten ejecutables, Docker, una BD o acceso a UAT. Registrar qué pruebas no se ejecutaron por limitaciones del ambiente, ejecutar las pruebas posibles, y dejar advertencias/cierre UAT pendientes. Nunca inventar evidencia, marcar un test no ejecutado como aprobado, ni habilitar una funcionalidad financiera no validada.

**Seguridad**: usar datos sintéticos/locales autorizados. No llamar a entidades externas ni producir movimientos monetarios.

## I02 (2026-11-02) — Última entrega acumulativa

La entrega vigente es `ACHInterbank_COMPLETO_SIMULADOR_PRENOTIFICACIONES_I02_2026-11-02.zip`. Contiene el código I00+I01+I02 y documentación anterior. `BusinessEligible` es preflight solo lectura; no habilita devolución. `dotnet build/test` no se ejecutaron por SDK ausente. Ver `docs/implementacion-simulador-prenotificaciones/INFORME_I02.md`, `ESTADO_INCREMENTAL.md` y `MEMORY_STATE.md`.
