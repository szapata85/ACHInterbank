# START HERE — ACHInterbank unified refactor package

Este ZIP contiene el proyecto completo ACHInterbank v124 y el programa `REFACTOR-CA-SOLID-RES-001` integrado en el mismo árbol.

## Estado inicial

- El `MEMORY.md` raíz ya fue reconciliado de forma append-only con el bloque durable inicial del programa.
- `AGENTS.md` original se conserva sin reemplazo.
- No se modificó código funcional, pruebas, migraciones ni configuración de producto para construir este paquete.
- La primera fase autorizada es **R0 únicamente**.

## Documentos canónicos para ejecución

1. `docs/architecture/REFACTOR_CA_SOLID_RESILIENCE_IMPLEMENTATION_PLAN.md` — plan R0–R10.
2. `docs/ai/REFACTOR_CA_SOLID_RESILIENCE_STATE.md` — state memory operativo del programa.
3. `docs/ai/REFACTOR_CA_SOLID_RESILIENCE_PHASE_PROMPTS.md` — prompts por fase/subfase.
4. `docs/ai/refactor/MASTER_PROMPT_CHATGPT.md` — prompt maestro para iniciar ChatGPT modo Chat.
5. `MEMORY.md` — memoria durable del proyecto, ya inicializada para el programa.
6. `AGENTS.md` — reglas permanentes del agente.

## Copia de los documentos fuente del plan

La carpeta `docs/ai/refactor/` conserva además los siete documentos originales del paquete de planificación para trazabilidad:

- `PLAN_IMPLEMENTACION_REFACTOR_CA_SOLID_RESILIENCIA.md`
- `MEMORY_REFACTOR_STATE.md`
- `MEMORY_PATCH_INITIAL.md`
- `MEMORY_WITH_REFACTOR_INITIALIZED.md`
- `MASTER_PROMPT_CHATGPT.md`
- `PHASE_PROMPTS.md`
- `START_HERE.md` (esta versión integrada)

## Primer chat

1. Carga este ZIP como el repositorio de trabajo.
2. Pide a ChatGPT que lea `AGENTS.md`.
3. Pega el contenido de `docs/ai/refactor/MASTER_PROMPT_CHATGPT.md`.
4. ChatGPT debe leer `MEMORY.md` y `docs/ai/REFACTOR_CA_SOLID_RESILIENCE_STATE.md`.
5. Ejecutar **R0 solamente**.
6. No iniciar R1 en el mismo chat.

## Continuidad entre chats

Al final de cada fase actualizar:

- `docs/ai/REFACTOR_CA_SOLID_RESILIENCE_STATE.md` con estado, SHA, evidencia, architecture ratchet y `NEXT_ALLOWED_PHASE`;
- `MEMORY.md` únicamente con decisiones, cierres, blockers o evidencia durable;
- documentación específica de la fase cuando corresponda.

No declarar `VERIFIED`, `CLOSED`, `UAT_READY` o `RELEASE_READY` sin la evidencia requerida por `AGENTS.md` y el plan.
