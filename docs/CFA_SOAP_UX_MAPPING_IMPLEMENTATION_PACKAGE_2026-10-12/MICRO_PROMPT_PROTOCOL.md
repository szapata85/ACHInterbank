# Protocolo de ejecución por micro-prompts — CFA-SOAP-UX-MAP-001

## Objetivo

Permitir que ChatGPT implemente el programa de forma incremental sin cargar todo el paquete en cada turno y sin mezclar cambios grandes.

## Regla principal

**Un turno = un micro-prompt.** No ejecutar el siguiente micro-prompt en el mismo turno, aunque el actual termine rápido.

## Lectura mínima por turno

Leer únicamente:

1. `AGENTS.md`.
2. `docs/CFA_SOAP_UX_MAPPING_IMPLEMENTATION_PACKAGE_2026-10-12/MEMORY_STATE.md`.
3. En `MEMORY.md`, buscar solo IDs `CFAUX` y cualquier decisión directamente relacionada con los archivos que se tocarán. No leer todo `MEMORY.md` salvo recuperación deliberada.
4. El bloque identificado por `next_micro_prompt` en `micro-prompts/Px.md`.
5. Solo las secciones de arquitectura que dicho micro-prompt referencia.
6. Código real afectado y pruebas focales.

No volver a cargar todos los documentos de arquitectura en cada turno.

## Estado canónico

`MEMORY_STATE.md` debe mantener:

```yaml
active_phase: P0
active_phase_status: NOT_STARTED
active_micro_prompt: P0.1
active_micro_prompt_status: NOT_STARTED
next_micro_prompt: P0.1
last_completed_micro_prompt: null
```

Al terminar un micro-prompt:

- marcarlo `DONE` o `BLOCKED`;
- registrar evidencia compacta;
- fijar `next_micro_prompt`;
- no avanzar automáticamente;
- actualizar `MEMORY.md` solo si apareció una decisión durable, un cierre, una trampa o un gap relevante.

## Tamaño de cambio

Cada micro-prompt debe intentar tocar una sola preocupación cohesiva. Si durante la ejecución aparece otra preocupación, registrarla como gap/next action en lugar de ampliarla silenciosamente.

## Evidencia compacta

No pegar logs completos. Registrar:

- comando;
- PASS/FAIL/NOT_RUN_ENVIRONMENT;
- archivo o test relevante;
- mensaje mínimo que demuestre el resultado.

## Condiciones de parada inmediata

Detener y marcar `BLOCKED` si:

- hay cambios locales del usuario que podrían perderse;
- el baseline ya falla por una causa no relacionada;
- se necesita secreto, certificado o infraestructura externa;
- cambia semántica ACH/CENIT/NACHA-M;
- el XML baseline cambia sin contrato CFA aprobado;
- una migración no puede ser segura/aditiva;
- el cambio obliga a inventar datos o reglas;
- el micro-prompt excede claramente su alcance.

## Respuesta esperada al usuario

Al final de cada turno entregar solo:

```text
Micro-prompt: Px.y
Status: DONE | BLOCKED
Archivos: <resumen corto>
Pruebas: <PASS/FAIL/NOT_RUN_ENVIRONMENT>
Memory: <IDs/estado actualizado>
Siguiente: Px.z | NONE
```

Detalles adicionales solo si son necesarios para explicar un bloqueo o una decisión.
