# Prompt maestro — Bootstrap amplio CFA-SOAP-UX-MAP-001

Usar solo para bootstrap/revisión completa. Para el trabajo incremental diario usar `CHATGPT_MICRO_START.md`; es mucho más pequeño y evita recargar el contexto.

```text
Quiero que ejecutes el programa `CFA-SOAP-UX-MAP-001` definido en:

`docs/CFA_SOAP_UX_MAPPING_IMPLEMENTATION_PACKAGE_2026-10-12/`

Objetivos exclusivos:
1) rediseñar el SPA de administración CFA con Angular Material, UX/UI funcional en español, sin Spanglish evitable, branding dinámico, responsive real y accesibilidad;
2) convertir el mapping de `Proc_Contrapartidas`, `Proc_Transacciones` y `RegistrarRespuestaTransaccion` en un modelo versionado dirigido por contrato, donde un nuevo campo input escalar solicitado por CFA pueda configurarse sin modificar el cliente/writer SOAP cuando la fuente ya existe.

Reglas de ejecución:
- Lee primero `AGENTS.md` y `MEMORY.md`.
- Luego lee, en este orden: `START_HERE.md`, `MEMORY_STATE.md`, `ARCHITECTURE_OVERVIEW.md`, `SOAP_CONTRACT_MAPPING_ARCHITECTURE.md`, `BACKEND_SOLID_DESIGN.md`, `DATA_MODEL_AND_MIGRATIONS.md`, `API_CONTRACTS.md`, `SPA_UX_UI_ARCHITECTURE.md`, `IMPLEMENTATION_PLAN.md`, `TESTING_AND_RELEASE_GATES.md`.
- Ejecuta solo `active_phase`/`next_allowed_phase` de `MEMORY_STATE.md`.
- No ejecutes dos fases en una sola intervención salvo autorización explícita.
- Si existe Git, registra `git status --short`, `git log -1 --oneline` y `git diff --stat` al comenzar.
- No pises cambios locales del usuario.
- Respeta Clean Architecture y SOLID.
- No cambies reglas NACHA-M, ciclos, causales, compensación, liquidación, clasificación monetaria ni semántica ACH/CENIT.
- Mantén response parsers específicos salvo una necesidad contractual explícita fuera del alcance.
- No permitas XML libre, reflection configurable, scripts ni expresiones arbitrarias.
- No conviertas Angular en autoridad de contrato/mapping.
- No agregues colores de marca fijos al feature; usa branding/tokens existentes.
- No uses nombres SOAP/códigos técnicos como lenguaje principal para el usuario.
- Mantén publicados/activos inmutables; usa nuevas versiones/borradores.
- Implementa migraciones para PostgreSQL y SQL Server cuando la fase las requiera.
- No retires el camino legacy hasta demostrar request parity de los tres servicios.
- No cambies tests/expectativas para ocultar una regresión.
- No declares una prueba PASS si no fue ejecutada.
- Nunca imprimas/commitees secretos, PII, cuentas completas, certificados, tokens ni SOAP sensible.

Al final de la fase:
1. ejecuta pruebas focales;
2. ejecuta regresión indicada por `TESTING_AND_RELEASE_GATES.md` cuando el entorno lo permita;
3. revisa diff;
4. actualiza `MEMORY_STATE.md` con decisiones/gaps/evidencia/next phase;
5. actualiza `MEMORY.md` únicamente con información durable y compacta si corresponde;
6. entrega `Phase Completion Report` con:
   - Status DONE/BLOCKED
   - Base/commit
   - Files changed
   - Migrations
   - Tests PASS/FAIL/NOT_RUN_ENVIRONMENT
   - Architecture assertions verified
   - Remaining gaps
   - Memory IDs updated
   - Next allowed phase

Empieza por la fase actualmente permitida en `MEMORY_STATE.md`. Si es P0, no cambies schema ni comportamiento funcional: caracteriza, congela request parity y registra baseline.
```
