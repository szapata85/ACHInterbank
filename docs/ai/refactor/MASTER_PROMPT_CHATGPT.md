# Master Prompt — ChatGPT modo Chat para ejecutar el refactor ACHInterbank

Vamos a ejecutar el programa `REFACTOR-CA-SOLID-RES-001` del archivo `docs/architecture/REFACTOR_CA_SOLID_RESILIENCE_IMPLEMENTATION_PLAN.md` **fase por fase** sobre este repositorio ACHInterbank.

Reglas obligatorias:

1. Lee primero `AGENTS.md`.
2. Recupera únicamente el contexto relevante de `MEMORY.md` y `docs/ai/REFACTOR_CA_SOLID_RESILIENCE_STATE.md`; no asumas que el plan está por encima del código, las pruebas, las decisiones cerradas o la normativa vigente.
3. Lee `docs/CURRENT_PROJECT_STATUS.md` y la documentación específica de la fase.
4. Confirma `git status --short`, `git rev-parse HEAD` y ejecuta `python scripts/validate-solution-projects.py` antes de modificar. Si el ZIP no incluye metadatos `.git`, registra explícitamente `SHA: UNAVAILABLE_FROM_PACKAGE` y continúa con el baseline reproducible del árbol, sin inventar un commit.
5. Trabaja **solo la fase/subfase marcada como `NEXT_ALLOWED_PHASE`**. No avances automáticamente.
6. Este es un refactor incremental, no una reescritura. No introduzcas `PaymentRailOperationalStrategy` ni otro rediseño futuro salvo que una fase posterior lo autorice expresamente.
7. Mantén ACH Colombia y CENIT separados. No cambies reglas, layouts, causas, ciclos, ventanas, retornos, nombres, límites o semántica NACHA-M sin evidencia normativa explícita.
8. Mantén PUBLISH snapshot authority, golden files, idempotencia, hash, trazabilidad, audit, cluster-safety, retry/recovery y semántica MFT existente.
9. No cambies expected results o golden files para ocultar una regresión.
10. No conectes a infraestructura productiva real ni uses secretos reales.
11. EF Code First sigue siendo autoridad; no generes migraciones salvo que la fase requiera un cambio persistente justificado. Un refactor estructural por sí solo no requiere migración.
12. Resilience es específica por operación. No agregues retry ciego a SOAP/WCF/HTTP no idempotente.
13. Si el host no tiene .NET/Docker/Node necesarios para validar, termina como `IMPLEMENTED_UNVERIFIED`; no declares `VERIFIED` o `CLOSED` sin evidencia.
14. Antes de mover una clase pública, inspecciona callers, registrations DI, tests y contratos API.
15. Mantén diffs pequeños y evita cleanup no relacionado.
16. Actualiza `docs/ai/REFACTOR_CA_SOLID_RESILIENCE_STATE.md` al terminar la fase. Actualiza `MEMORY.md` solo con información durable conforme a `AGENTS.md`.

Formato obligatorio de la respuesta al terminar cada fase:

- `PHASE_STATE:`
- `SHA:`
- `SUMMARY:`
- `FILES_CHANGED:`
- `ARCHITECTURAL_DECISIONS:`
- `TESTS_EXECUTED:` con comandos y resultados exactos
- `ARCHITECTURE_RATCHET:` excepciones eliminadas/restantes
- `BEHAVIORAL_DIFF:` debe ser `NONE` salvo cambio explícitamente autorizado
- `NORMATIVE_DIFF:` debe ser `NONE` salvo cambio explícitamente autorizado
- `DB_MIGRATIONS:`
- `RISKS_OR_BLOCKERS:`
- `MEMORY_UPDATE:` IDs y texto compacto propuesto/realizado
- `NEXT_ALLOWED_PHASE:` sin ejecutarla

Al actualizar `MEMORY.md`, mantenlo en inglés, compacto y durable. No copies logs crudos ni el relato completo de la sesión. Usa IDs estables `DEC-`, `INV-`, `GAP-`, `CLOSE-`, `TRAP-`, `EVID-`, `NEXT-`, `NORM-` o los IDs `REF-*` definidos para este programa.

Empieza leyendo el state memory y ejecuta únicamente la fase que figure como `NEXT_ALLOWED_PHASE`. En el estado inicial debe ser **R0**.
