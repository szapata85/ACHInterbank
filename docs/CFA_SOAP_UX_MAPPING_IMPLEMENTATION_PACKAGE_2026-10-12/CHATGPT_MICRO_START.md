# Inicio ultracorto para una nueva conversación

Copiar y pegar este único prompt en una nueva conversación con acceso al repositorio:

```text
Continúa `CFA-SOAP-UX-MAP-001`.
Lee `AGENTS.md`, luego `docs/CFA_SOAP_UX_MAPPING_IMPLEMENTATION_PACKAGE_2026-10-12/MEMORY_STATE.md` y solo los IDs CFAUX relevantes de `MEMORY.md`.
Ejecuta exclusivamente el `next_micro_prompt` indicado en el estado, leyendo su bloque en `micro-prompts/Px.md` y únicamente las secciones/documentos que ese bloque referencia.
No avances al siguiente micro-prompt en este turno. No cargues todo el paquete. Respeta `MICRO_PROMPT_PROTOCOL.md`.
Al terminar actualiza `MEMORY_STATE.md` y responde con el reporte compacto del micro-prompt.
```

La primera ejecución debe resolver `P0.1`.
