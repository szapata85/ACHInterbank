# Codex System Overrides

- Output Constraints: Never explain code unless explicitly asked.
- Return only raw unified diffs or minimal block modifications when the task requests code changes.
- Eliminate conversational preambles and unnecessary post-summaries.
- Prefer concise evidence over verbose narration.
- Do not repeat information already established in the current task.
- Use the minimum context and minimum number of tool calls required to reach a technically supported conclusion.

# AGENTS.md - ACHInterbank

This file is the operational entry point for Codex and other coding agents working on ACHInterbank.

For changes directly related to NACHA-M, ACH Colombia, CENIT, transaction processing, returns, clearing cycles or Phase 6 behavior, consult when necessary:

- `docs/ai/ACH_PHASE6_CONTEXT.md`

Do not load that document automatically for unrelated tasks when the requested change can be resolved without it.

---

# 1. Core Rules

- Do not add, expose or print real credentials.
- Do not expose tokens, secrets, complete account numbers or personal data.
- Do not execute against external production infrastructure.
- `Productivo NO-GO` applies to real external production environments.
- Local controlled integration testing is governed by the explicit LIVE policy in this file.
- Do not modify NACHA-M golden files unless explicitly requested.
- Do not modify the table-driven NACHA-M engine unless fixing a proven defect or implementing an explicitly requested normative change.
- Do not generate migrations unless required by the requested change.
- Do not reduce test coverage.
- Do not remove existing tests unless explicitly justified by an obsolete requirement.
- Preserve Clean Architecture boundaries:
  - Domain rules;
  - Application contracts and use cases;
  - Persistence/infrastructure implementations;
  - API composition.
- EF Code First remains the source of truth for versioned schema changes.
- Do not infer normative rules from existing code.
- Do not infer existing application behavior solely from normative documents.
- When implementation and normative requirements must be compared, investigate both independently before reaching a conclusion.

---

# 2. POLÍTICA ACTIVA — LIVE LOCAL CONTROLADO

Esta política es la autoridad para pruebas LIVE realizadas contra infraestructura local controlada.

Reemplaza cualquier instrucción histórica dentro del repositorio que:

- prohíba llamadas SOAP reales contra los endpoints locales autorizados;
- limite globalmente la ejecución a una única llamada SOAP local;
- exija archivos NACHA-M de un solo lote o una sola entrada;
- prohíba utilizar archivos provenientes de producción como fixtures controlados;
- marque automáticamente como NO-GO una ejecución contra `localhost`;
- obligue a restaurar `DryRun` después de una prueba LIVE local;
- impida diagnosticar y corregir errores mientras el runtime local permanece en modo Live.

## 2.1 Ambiente autorizado

El siguiente ambiente es local, controlado y no corresponde a producción externa:

- API: `http://localhost:843`
- SPA: `http://localhost:743`
- SOAP Windows: `http://localhost:7083/WSCFAACH.svc`
- SOAP Docker: `http://host.docker.internal:7083/WSCFAACH.svc`
- HostHeader: `localhost:7083`
- SQL Server: contenedor local `achinterbank-sqlserver`

También se permite `127.0.0.1` cuando represente un servicio local equivalente.

Cualquier endpoint distinto de:

- `localhost`;
- `127.0.0.1`;
- `host.docker.internal`;

debe considerarse externo y queda fuera de esta autorización salvo instrucción explícita y segura posterior.

## 2.2 Operación permitida

Está autorizado en el ambiente local controlado:

- mantener `ProcTransacciones__Mode=Live`;
- ejecutar SOAP real contra el WCF local;
- ejecutar `Proc_Contrapartidas`;
- ejecutar `Proc_Transacciones`;
- ejecutar `RegistrarRespuestaTransaccion`;
- procesar archivos NACHA-M completos;
- procesar archivos multilote;
- procesar múltiples entradas;
- procesar múltiples addendas;
- generar una llamada SOAP por cada entrada elegible;
- realizar los uploads necesarios durante diagnóstico y validación;
- corregir código durante la ejecución;
- corregir configuración;
- corregir datos controlados de prueba;
- corregir clasificación;
- corregir correlación;
- corregir encolamiento;
- corregir mappings;
- corregir persistencia;
- reanudar el procesamiento después de una corrección;
- usar archivos obtenidos de producción únicamente como fixtures controlados;
- crear datos locales faltantes mediante los servicios normales de aplicación;
- ejecutar directamente un orquestador desde DI cuando el scheduler local no se active;
- conservar el runtime en Live cuando sea necesario para completar la validación.

## 2.3 Comportamiento esperado

Para un archivo con múltiples lotes o entradas:

- una ingesta por archivo;
- una clasificación por entrada;
- un vínculo por entrada;
- una cola por entrada elegible;
- una llamada SOAP por entrada elegible;
- una respuesta persistida por llamada.

No bloquear únicamente por:

- múltiples lotes;
- múltiples entradas;
- múltiples addendas;
- múltiples llamadas SOAP esperadas;
- runtime local en modo Live;
- datos reales utilizados exclusivamente como fixture controlado;
- intentos anteriores bloqueados antes de llegar al SOAP;
- nombres CENIT sin extensión;
- archivos ACH Colombia terminados en `.OUT`.

## 2.4 Nombres de archivos LIVE

### CENIT

Carpeta:

`docs/uat/proc-transacciones-live/CENIT`

Los archivos no llevan extensión.

Formato esperado:

```regex
^\d{7}\.\d{3}\.\d{8}\.\d+$
```

### ACH Colombia

Carpeta:

`docs/uat/proc-transacciones-live/ACHCOL`

Los archivos terminan en `.OUT`.

Formato esperado:

```regex
^\d{7}\.\d{3}\.\d{8}\.\d+\.OUT$
```

Nunca:

- agregar `.ach`;
- generar alias derivados del lote;
- generar alias derivados de `IDLOTE`;
- generar alias derivados de `BatchNumber`.

## 2.5 Límites obligatorios

Está prohibido:

- llamar endpoints externos no autorizados;
- ejecutar contra infraestructura productiva real;
- utilizar credenciales productivas;
- conectarse a redes financieras externas;
- generar movimientos monetarios reales;
- eliminar evidencia de auditoría;
- imprimir credenciales;
- imprimir tokens;
- imprimir cuentas completas;
- imprimir datos personales;
- imprimir XML sensible completo;
- volver a enviar una entrada que ya obtuvo una respuesta SOAP exitosa o funcionalmente definitiva.

No existe un límite global artificial de una única llamada SOAP cuando existen múltiples entradas elegibles.

No existe obligación general de restaurar `DryRun` después de una prueba LIVE local.

No existe obligación de detener todo el flujo después del primer error corregible.

La idempotencia, deduplicación y prevención de reenvíos definitivos continúan siendo obligatorias.

---

# 3. Repository Map

- Solution: `ACHInterbank.sln`
- Main API: `src/Cfa.ACHInterbank.Api`
- Application: `src/Cfa.ACHInterbank.Application`
- Domain: `src/Cfa.ACHInterbank.Domain`
- Persistence/infrastructure: `src/Cfa.ACHInterbank.Persistence`
- Backend tests: `tests/Cfa.ACHInterbank.Tests`
- NACHA golden files: `tests/Cfa.ACHInterbank.Tests/TestData/Nacha/GoldenFiles`
- Permanent Phase 6 context: `docs/ai/ACH_PHASE6_CONTEXT.md`

Do not explore the complete repository when the relevant component is already known.

---

# 4. Build And Test Commands

Canonical backend validation:

```bash
dotnet build ACHInterbank.sln -c Release
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release
```

After a successful build:

```bash
dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build
```

Expected result:

- Build succeeded.
- 0 warnings when the repository baseline requires it.
- 0 errors.
- Relevant tests passing.

## Validation strategy

Prefer the narrowest useful validation first.

Order:

1. affected unit or focal tests;
2. affected project tests;
3. build;
4. broader regression suite when justified by the scope.

Do not execute the complete suite repeatedly when a focal validation is sufficient during diagnosis.

Before finalizing a significant change, execute the validations necessary to prove that the requested behavior works and that the affected area did not regress.

---

# 5. Phase 6 Guardrails

- Fase 6B.3C/6B.3C.1 golden files are semireal, anonymized and are not official certification artifacts.
- Fase 6B.4 covers internal incoming NACHA decisions.
- Fase 6B.5 covers SOAP integration boundaries.
- `Proc_Contrapartidas` and `Proc_Transacciones` may execute only against the explicitly authorized local controlled environment defined in this file.
- They remain prohibited against real external production infrastructure.
- `RegistrarRespuestaTransaccion` is non-monetary and must not independently move money.
- `None`, duplicates and `ManualReviewRequired` must not produce an inappropriate SOAP dispatch.
- A transaction with a successful or functionally definitive response must not be resent.
- When Phase 6 historical documentation contradicts the active LIVE local policy in this file, this file governs local controlled testing.

---

# 6. Tool Responsibility Matrix

Use each tool only for the responsibility it solves best.

| Need | Preferred tool |
|---|---|
| Code architecture | `codebase-memory` |
| Classes, methods, dependencies | `codebase-memory` |
| Callers / callees | `codebase-memory` |
| Execution paths | `codebase-memory` |
| Impact analysis | `codebase-memory` |
| Normative documents | `local-rag` |
| ACH Colombia rules | `local-rag` |
| CENIT rules | `local-rag` |
| Terminal output | `RTK` when appropriate |
| Exact concrete file | Direct read |
| Exact runtime evidence | Original command/output when RTK could hide evidence |

Primary rule:

```text
Normativa        -> local-rag
Código           -> codebase-memory
Terminal         -> RTK
Archivo concreto -> lectura directa
```

Do not use multiple tools to answer exactly the same question unless cross-validation is technically necessary.

---

# 7. Codebase Memory — obligatorio para comprensión estructural

The Codebase Memory project for this repository is:

`ACHInterbank`

Its responsibility is exclusively technical knowledge of the repository:

- source code;
- architecture;
- classes;
- methods;
- controllers;
- handlers;
- services;
- entities;
- Angular components;
- routes;
- dependencies;
- callers;
- callees;
- tests;
- configuration;
- infrastructure represented as repository files;
- execution paths;
- impact analysis.

## 7.1 `ACHInterbank-normativa` is no longer the primary normative source

Do not require the former Codebase Memory project:

`ACHInterbank-normativa`

for normative retrieval.

Normative retrieval is now the responsibility of:

`local-rag`

Do not duplicate normative documents inside source folders merely to make Codebase Memory discover them.

Do not repeatedly reindex the primary `ACHInterbank` project in an attempt to incorporate the external Local RAG corpus.

## 7.2 When to synchronize Codebase Memory

Treat `index_repository` as an incremental synchronization.

Synchronize when the index may be stale, especially after:

- changing Git branch;
- `git pull`;
- significant merge;
- adding a module;
- adding or changing substantial SPA components or routes;
- adding or changing backend controllers;
- adding or changing handlers;
- adding or changing services;
- adding or changing entities;
- adding or changing EF Core configurations;
- adding or changing migrations;
- adding or changing SQL files;
- moving files;
- renaming files;
- deleting files;
- significant test changes;
- Docker or infrastructure changes represented in the repository;
- reinstalling or updating Codebase Memory;
- detecting that searches cannot find recently created or modified code.

Do not run `index_repository` automatically before every small action.

Do not run it repeatedly during the same task when no repository changes occurred since the previous synchronization.

Do not delete the existing index or force a full rebuild unless there is evidence of corruption or inconsistency.

## 7.3 Codebase Memory query strategy

After ensuring that the index is sufficiently current, use the most specific tool possible.

### `search_graph`

Use for:

- classes;
- methods;
- controllers;
- services;
- entities;
- components;
- routes;
- tests;
- structural relationships.

### `trace_path`

Use for:

- callers;
- callees;
- dependencies;
- execution paths;
- transaction flows;
- impacted components.

### `get_code_snippet`

Use when the specific symbol is already known.

Retrieve only the implementation required.

### `search_code`

Use for:

- literal text;
- configuration;
- constants;
- documentation inside the repository;
- SQL;
- information unavailable in the graph.

### `get_architecture`

Use only when understanding broader modules or layers is necessary.

Do not call it for a narrowly scoped change when the affected component is already known.

### `get_graph_schema` / `query_graph`

Use only for graph relationships that cannot be answered efficiently through the simpler tools.

## 7.4 Codebase Memory fallback

If Codebase Memory:

- is unavailable;
- returns errors;
- has an obviously stale graph;
- cannot locate information known to exist;

then:

1. report the limitation briefly;
2. fall back to targeted local searches;
3. keep the search scope narrow;
4. read only the files necessary.

Do not turn a Codebase Memory failure into an unrestricted repository scan.

---

# 8. Local RAG — fuente normativa

The MCP:

`local-rag`

is the primary retrieval mechanism for external normative documentation.

Its configured corpus is external to the repository.

Do not hardcode workstation-specific absolute paths such as `C:\Users\...` into this file or application source.

The Local RAG root is defined through the MCP configuration.

The configured corpus is expected to separate at least:

```text
ACH-Colombia/
CENIT/
```

This separation must be preserved during retrieval.

## 8.1 Responsibility

Use Local RAG for:

- ACH Colombia manuals;
- CENIT manuals;
- NACHA-M normative requirements;
- clearing rules;
- credits;
- debits;
- prenotifications;
- returns;
- returns by operator;
- return causes;
- differential responses;
- cycle rules;
- file structure;
- record fields;
- operating windows;
- security requirements;
- limits;
- normative annexes;
- other external regulatory or clearing documentation included in the configured corpus.

Do not use Local RAG for:

- locating application classes;
- callers/callees;
- source-code architecture;
- runtime logs;
- repository dependency analysis.

Those belong to Codebase Memory or direct inspection.

---

# 9. Local RAG synchronization policy

Do not synchronize Local RAG before every query.

Synchronize when:

- new normative documents were added;
- documents were modified;
- documents were replaced;
- documents were removed;
- expected documentation cannot be found;
- Local RAG was reinstalled;
- its database was recreated;
- its configuration changed;
- there is evidence that the vector index is stale.

Expected synchronization flow:

1. execute `sync_start`;
2. obtain the synchronization job identifier;
3. query `sync_status`;
4. continue until the job reaches `succeeded` or `failed`;
5. validate with `status` or `list_files` when necessary.

Do not repeatedly synchronize an unchanged corpus.

A synchronization is an indexing operation, not a prerequisite for every normative question.

---

# 10. Local RAG query strategy

Queries must be narrow and evidence-oriented.

Prefer queries containing:

- clearing house;
- document/version when known;
- business concept;
- transaction direction;
- specific process;
- relevant field/code/cause when known.

Examples:

```text
ACH Colombia V35 devolución recibida causal D33
```

```text
ACH Colombia V35 sección devoluciones operador
```

```text
CENIT devolución transacción débito ciclo
```

Avoid vague queries such as:

```text
devoluciones
```

when enough context exists to formulate a more precise search.

## 10.1 Query workflow

For a normative task:

1. query the relevant corpus with `query_documents`;
2. evaluate the highest-relevance results;
3. refine the query when results mix unrelated subjects;
4. use `read_chunk_neighbors` only when surrounding context is needed to interpret the rule;
5. identify source document;
6. identify version when available;
7. identify section or neighboring context when available;
8. extract only the rule needed for the task.

Do not ingest complete documents into model context when a few chunks are sufficient.

## 10.2 Evidence standard

A normative conclusion should identify, when available:

- clearing house;
- source document;
- version;
- section;
- retrieved rule;
- whether the evidence is direct or inferred from surrounding context.

Do not invent:

- section numbers;
- versions;
- causes;
- deadlines;
- formats;
- field values;
- operational rules.

If Local RAG cannot support the rule, explicitly state that normative evidence was not found.

---

# 11. ACH Colombia / CENIT isolation

Never automatically merge rules from ACH Colombia and CENIT.

## ACH Colombia-only task

Search the ACH Colombia corpus first.

Do not use CENIT rules as substitutes.

## CENIT-only task

Search the CENIT corpus first.

Do not use ACH Colombia rules as substitutes.

## Comparative task

When comparing ACH Colombia and CENIT:

1. query ACH Colombia separately;
2. retain its evidence;
3. query CENIT separately;
4. retain its evidence;
5. compare only after both retrievals are complete.

The absence of a rule in one clearing house does not imply that the same rule applies from the other clearing house.

---

# 12. Normativa ACH Colombia vigente

The canonical normative baseline for ACH Colombia is:

**Versión 35 — abril de 2026**

Versions V32 and earlier are historical or comparative unless the task explicitly requires an earlier version.

Before modifying behavior related to:

- ACH Colombia;
- NACHA-M;
- cycles;
- credits;
- debits;
- prenotifications;
- returns;
- returns by operator;
- return causes;
- files;
- limits;
- security;

search Local RAG for V35 first.

If Local RAG contains multiple versions:

1. prefer V35 for current ACH Colombia behavior;
2. do not mix V35 and older versions without identifying the version of each rule;
3. use older versions only for historical comparison or migration analysis.

If V35 cannot be located in Local RAG, report that condition instead of silently falling back to V32.

If code, tests, repository documentation or existing runtime behavior contradict V35:

1. preserve the normative evidence;
2. identify the implemented behavior independently;
3. report the discrepancy;
4. use V35 as the normative authority unless the task explicitly targets another version;
5. do not invent a reconciliation rule.

For returns, investigate especially:

- section 6.6;
- section 6.7;
- applicable annexes.

Consider causal `D33` only when supported by the retrieved normative evidence and applicable to the scenario.

---

# 13. Norma vs implementación

When a functional decision depends on both normative requirements and existing implementation, use this order:

1. **Local RAG**
   - establish what the applicable rule requires.

2. **Codebase Memory**
   - establish where and how the behavior is implemented.

3. **Direct file read**
   - inspect only the concrete implementation identified.

4. **Tests**
   - identify existing evidence and affected coverage.

5. **Implementation**
   - make the minimum coherent change.

The analysis should conceptually produce:

```text
Norma
  -> comportamiento requerido

Código
  -> comportamiento implementado

Gap
  -> diferencia demostrable

Acción
  -> cambio mínimo necesario

Prueba
  -> evidencia que demuestra el comportamiento
```

Never use:

```text
el código hace X, por lo tanto la norma debe exigir X
```

Never use:

```text
la norma exige X, por lo tanto el código seguramente ya hace X
```

Both sides must be independently verified.

---

# 14. RTK — reducción de ruido de terminal

RTK and Codebase Memory solve different problems.

- **Codebase Memory**: knowledge and structure of the repository.
- **Local RAG**: normative retrieval.
- **RTK**: compact terminal output.
- **Direct read**: exact inspection of already identified files.

RTK does not replace Codebase Memory.

RTK does not replace Local RAG.

## 14.1 Use RTK when appropriate

When available and supported, prefer RTK for terminal commands that may produce large noisy output.

Examples:

```bash
rtk git status
rtk git diff
rtk git log -n 10
rtk grep "<patrón>" .
rtk find "<patrón>" .
rtk docker ps
rtk docker logs <contenedor>
```

For `.NET`, when supported by the installed RTK version:

```bash
rtk dotnet build ACHInterbank.sln -c Release
```

For test runners with extensive output:

```bash
rtk test dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release
```

Second execution:

```bash
rtk test dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build
```

The canonical underlying commands remain the commands defined in `Build And Test Commands`.

RTK must not change:

- arguments;
- target project;
- test scope;
- command semantics.

If a specific RTK wrapper is not supported by the installed version, use the original command.

## 14.2 Keep RTK searches narrow

Prefer:

```bash
rtk grep "Proc_Transacciones" src/Cfa.ACHInterbank.Application
```

over:

```bash
rtk grep "Proc_Transacciones" .
```

when the relevant area is already known.

Do not use RTK searches to manually reconstruct architecture that Codebase Memory can retrieve more accurately.

---

# 15. When exact output is required

Do not rely exclusively on compacted RTK output when the task requires:

- complete error evidence;
- exact values;
- exact ordering;
- audit evidence;
- UAT evidence;
- complete logs;
- diagnosis of a hidden or filtered failure;
- literal comparison against a specification;
- proof that may depend on omitted lines.

In these cases:

1. use RTK first when useful;
2. identify the narrow relevant segment;
3. rerun only the required command without RTK;
4. capture only the necessary exact evidence.

Using an original command after RTK is allowed when technically justified.

---

# 16. RTK availability

Do not assume RTK exists in every environment.

If `rtk` is unavailable:

- do not stop the task solely because of its absence;
- use the equivalent original command;
- keep the same narrow scope;
- do not modify application dependencies to install RTK;
- do not introduce RTK as an application runtime dependency.

RTK availability must not affect application behavior.

---

# 17. Direct file reading

Direct reading is appropriate after the relevant file is known.

Prefer:

- one method;
- one class;
- one configuration section;
- one test;
- one relevant line range;

over loading a complete large file.

Do not read entire directories or large files merely because they may contain relevant information.

Codebase Memory should normally discover the technical target first.

Local RAG should normally discover the normative target first.

---

# 18. Context economy

The objective is:

**the minimum sufficient context that preserves the evidence required for a technically correct decision.**

Avoid:

- global repository searches when the component is known;
- loading complete documentation when a relevant chunk is enough;
- reading complete source files when a symbol is enough;
- querying both Codebase Memory and RTK for exactly the same fact;
- querying Local RAG for purely technical tasks;
- querying Codebase Memory for normative rules;
- synchronizing Local RAG when documents have not changed;
- synchronizing Codebase Memory repeatedly without repository changes;
- repeating searches whose answer is already available in the current context;
- collecting complete logs when a bounded window is enough;
- introducing unrelated documentation;
- introducing unrelated code;
- introducing unrelated test output;
- introducing unrelated runtime logs.

Do not minimize context at the expense of required evidence.

Do not maximize context merely because more information is available.

---

# 19. Task classification before tool use

Before using tools, classify the task.

## Pure technical task

Examples:

- compile error;
- dependency injection;
- Angular component;
- EF configuration;
- controller;
- service;
- unit test;
- Docker configuration.

Use:

```text
Codebase Memory
-> direct read
-> RTK/terminal
```

Do not query Local RAG unless a functional rule becomes relevant.

## Pure normative task

Examples:

- what ACH Colombia requires;
- allowed return causes;
- CENIT rule;
- version comparison.

Use:

```text
Local RAG
-> neighboring chunks only if necessary
```

Do not inspect source code unless implementation comparison is requested.

## Normative + implementation task

Examples:

- does ACHInterbank comply with V35?;
- is this return implemented correctly?;
- differential response gap;
- ACH Colombia vs current implementation.

Use:

```text
Local RAG
-> Codebase Memory
-> direct read
-> tests
```

## Runtime diagnosis

Use:

```text
Codebase Memory when structure is unknown
-> RTK/terminal
-> exact output when required
-> direct read
```

---

# 20. Expected development workflow

For a relevant development task:

1. determine the exact requested outcome;
2. classify the task as technical, normative, mixed or runtime;
3. choose only the necessary tools;
4. synchronize Codebase Memory only if its index may be stale;
5. synchronize Local RAG only if its corpus may be stale;
6. retrieve the minimum technical or normative context;
7. identify affected components;
8. identify dependencies and execution paths when necessary;
9. identify affected tests;
10. read only the concrete files or symbols required;
11. implement the minimum coherent change;
12. run focal validation;
13. expand validation only when justified;
14. inspect exact output if compact output is insufficient;
15. reindex Codebase Memory when significant source changes need to become queryable in subsequent work;
16. do not reindex Local RAG unless the normative corpus itself changed;
17. report concise evidence.

---

# 21. Final response expectations

For code-changing tasks, keep the final response concise.

When useful, report:

- status;
- cause;
- change;
- affected files;
- tests executed;
- relevant test result;
- Codebase Memory tools used;
- Local RAG evidence used when normative rules affected the decision;
- relevant runtime evidence;
- commit or PR when one was explicitly created.

Do not produce a long narrative when a concise technical summary is sufficient.

For normative work, identify the document/version used.

For mixed normative/implementation work, distinguish clearly between:

- normative evidence;
- implementation evidence;
- inferred gap;
- implemented correction.

---

# 22. Prohibited tool anti-patterns

Do not:

- run `index_repository` before every prompt;
- run `sync_start` before every Local RAG query;
- perform broad `rg`/`grep` searches before trying Codebase Memory when structural discovery is required;
- use Codebase Memory as the authoritative normative store;
- use Local RAG to infer source-code architecture;
- run RTK and then the exact same unfiltered command without a technical reason;
- repeatedly read the same file;
- repeatedly retrieve the same RAG chunk;
- load every normative document for a single rule;
- combine ACH Colombia and CENIT evidence without separating their sources;
- treat an older ACH Colombia version as current when V35 exists;
- assume absence from retrieval proves absence from the corpus without refining the search;
- make code changes before identifying the applicable normative rule when the behavior is normatively governed.

---

# 23. Source hierarchy

For implementation truth:

1. current source code;
2. current configuration;
3. current tests;
4. actual local runtime evidence.

For ACH Colombia normative truth:

1. applicable current normative document retrieved through Local RAG;
2. V35 for current ACH Colombia behavior;
3. older versions only for historical or comparative analysis.

For CENIT normative truth:

1. applicable current CENIT document retrieved through Local RAG;
2. older documents only when explicitly relevant.

Repository documentation may provide context but must not silently override a newer applicable normative source.

Runtime behavior demonstrates what the system does.

Runtime behavior does not independently prove what the normative requirement is.

---

# 24. Guiding principle

Every task should converge using the shortest evidence-preserving path:

```text
Question
   |
   +-- Is it normative?
   |       |
   |       +--> local-rag
   |
   +-- Is it about code structure?
   |       |
   |       +--> codebase-memory
   |
   +-- Is the concrete target known?
   |       |
   |       +--> direct read
   |
   +-- Does it require execution?
           |
           +--> RTK / terminal
                    |
                    +--> exact output only when necessary
```

For mixed functional work:

```text
Local RAG
   |
   v
Normative requirement
   |
   +---------------------+
                         |
                         v
                 Codebase Memory
                         |
                         v
                 Implementation
                         |
                         v
                  Norma vs código
                         |
                         v
                       Gap
                         |
                         v
                Minimum correction
                         |
                         v
                 Focused validation
```

The goal is not to use every available tool.

The goal is to use the **smallest set of tools and context that produces a verifiable, technically correct result**.