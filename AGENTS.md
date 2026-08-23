# Codex System Overrides

- Never explain code unless explicitly requested.
- When the task requests code changes, prefer raw unified diffs or minimal block modifications.
- Eliminate unnecessary conversational preambles and post-summaries.
- Prefer concise evidence over verbose narration.
- Do not repeat information already established in the current task.
- Use the minimum context and minimum number of tool calls required to reach a technically supported conclusion.
- Do not re-investigate durable facts already verified in Project Memory unless current evidence indicates they may be stale.
- Do not use a larger model context merely because additional context is available.
- Preserve exact evidence whenever correctness depends on exact values, ordering, runtime state, regulatory text, or test output.

---

# AGENTS.md — ACHInterbank

This file is the operational entry point for Codex and other coding agents working on ACHInterbank.

For changes directly related to:

- NACHA-M;
- ACH Colombia;
- CENIT;
- transaction processing;
- credits;
- debits;
- prenotifications;
- returns;
- returns of returns;
- differential responses;
- clearing cycles;
- settlement;
- Phase 6 behavior;

consult when necessary:

`docs/ai/ACH_PHASE6_CONTEXT.md`

Do not load that document automatically for unrelated tasks when the requested work can be completed without it.

Persistent project knowledge is maintained separately in:

`MEMORY.md`

Do not automatically load the complete `MEMORY.md`.

Use the progressive Project Memory retrieval policy defined in this file.

---

# 1. Core Rules

## 1.1 Security

- Never add, expose, print or persist real credentials.
- Never expose tokens, secrets, private keys, certificates, complete account numbers or personal data.
- Never print complete sensitive SOAP/XML payloads unless explicitly required and safely anonymized.
- Never commit credentials, secrets, certificates, tokens or production connection strings.
- Never execute against external production infrastructure.
- Never connect to real external financial networks unless a future explicit authorization safely supersedes this rule.
- Never generate real monetary movements.

`Productivo NO-GO` applies to real external production environments.

Controlled local integration testing is governed by the explicit LIVE policy defined below.

## 1.2 Architecture

Preserve Clean Architecture boundaries:

- Domain rules;
- Application contracts and use cases;
- Persistence/infrastructure implementations;
- API composition;
- frontend concerns isolated from backend domain logic.

Do not bypass architectural boundaries merely to make a test pass.

Prefer:

- dependency inversion;
- explicit contracts;
- reusable domain rules;
- deterministic behavior;
- configuration-driven behavior;
- data-driven behavior;
- testable components.

## 1.3 No hardcoded business behavior

Do not hardcode clearing-house behavior when an existing configurable or profile-driven mechanism exists.

Do not hardcode:

- ACH Colombia or CENIT business rules;
- regulatory causes;
- field positions;
- record layouts;
- operational dates;
- cycle windows;
- settlement windows;
- routing codes;
- transaction codes;
- filename conventions;
- return behavior;
- limits;
- clearing-house-specific assumptions;

when those values belong to:

- configuration;
- database catalogs;
- NACHA profiles;
- normative mappings;
- clearing-house metadata;
- seeded domain data;
- existing rule engines.

The official NACHA-M implementation remains profile/configuration driven.

Do not introduce legacy hardcoded layouts as an alternative implementation path.

## 1.4 Database

EF Code First remains the source of truth for versioned application schema changes.

Do not generate migrations unless required by the requested change.

Do not modify production-like schemas manually merely to bypass a required migration.

## 1.5 Tests

- Do not reduce test coverage.
- Do not remove existing tests unless an obsolete requirement has been explicitly demonstrated.
- Do not weaken assertions merely to obtain green CI.
- Do not change expected results to match a defective implementation.
- A passing test does not independently prove regulatory compliance.
- A failing test does not independently prove that the normative rule changed.

## 1.6 NACHA-M

Do not modify NACHA-M golden files unless explicitly requested or a proven normative change requires it.

Do not modify the table-driven NACHA-M engine unless:

- fixing a proven implementation defect; or
- implementing an explicitly verified normative change.

Do not infer normative rules from existing code.

Do not infer existing application behavior solely from normative documents.

When implementation and normative requirements must be compared, investigate both independently before reaching a conclusion.

## 1.7 Evidence states

Never mark a capability as:

- `CLOSED`;
- `VERIFIED`;
- `GAUNTLET_PASSED`;
- `UAT_READY`;
- `USER_ACCEPTED`;
- `RELEASE_READY`;

without appropriate evidence.

Previous Project Memory state does not override contradictory current evidence.

---

# 2. ACTIVE POLICY — CONTROLLED LOCAL LIVE TESTING

This policy is the authority for LIVE testing against controlled local infrastructure.

It supersedes historical repository instructions that:

- prohibit real SOAP calls against authorized local endpoints;
- globally limit execution to a single local SOAP call;
- require NACHA-M files to contain only one batch or one entry;
- prohibit using production-origin files as controlled fixtures;
- automatically classify `localhost` execution as NO-GO;
- require restoring `DryRun` after controlled local LIVE testing;
- prevent diagnosis and correction while the local runtime remains in Live mode.

## 2.1 Authorized environment

The following environment is local, controlled and is not external production:

- API: `http://localhost:843`
- SPA: `http://localhost:743`
- SOAP Windows: `http://localhost:7083/WSCFAACH.svc`
- SOAP Docker: `http://host.docker.internal:7083/WSCFAACH.svc`
- HostHeader: `localhost:7083`
- SQL Server: local container `achinterbank-sqlserver`

`127.0.0.1` is also allowed when it represents an equivalent local service.

Any endpoint other than:

- `localhost`;
- `127.0.0.1`;
- `host.docker.internal`;

must be considered external and unauthorized unless a later explicit and safe instruction supersedes this rule.

## 2.2 Permitted controlled LIVE operations

Within the controlled local environment it is allowed to:

- keep `ProcTransacciones__Mode=Live`;
- execute real SOAP calls against the local WCF service;
- execute `Proc_Contrapartidas`;
- execute `Proc_Transacciones`;
- execute `RegistrarRespuestaTransaccion`;
- process complete NACHA-M files;
- process multi-batch files;
- process multiple entries;
- process multiple addendas;
- generate one SOAP call for each eligible entry;
- perform uploads required for diagnosis and validation;
- correct source code during diagnosis;
- correct controlled configuration;
- correct controlled test data;
- correct classification logic;
- correct correlation logic;
- correct queueing;
- correct mappings;
- correct persistence;
- resume processing after a correction;
- use production-origin files only as controlled fixtures;
- create missing local test data through normal application services;
- invoke an orchestrator directly through DI when the local scheduler does not trigger;
- keep the local runtime in Live mode when required to complete controlled validation.

## 2.3 Expected multi-entry behavior

For a file containing multiple batches or entries:

- one ingestion per file;
- one classification per entry;
- one link/correlation per entry;
- one queue operation per eligible entry;
- one SOAP call per eligible entry;
- one persisted response per call.

Do not block solely because of:

- multiple batches;
- multiple entries;
- multiple addendas;
- multiple expected SOAP calls;
- local runtime being in Live mode;
- real-origin data being used exclusively as a controlled fixture;
- previous attempts that failed before reaching SOAP;
- CENIT filenames without extensions;
- ACH Colombia filenames ending in `.OUT`.

## 2.4 LIVE filenames

### CENIT

Directory:

`docs/uat/proc-transacciones-live/CENIT`

Files have no extension.

Expected format:

```regex
^\d{7}\.\d{3}\.\d{8}\.\d+$
```

### ACH Colombia

Directory:

`docs/uat/proc-transacciones-live/ACHCOL`

Files end in `.OUT`.

Expected format:

```regex
^\d{7}\.\d{3}\.\d{8}\.\d+\.OUT$
```

Never:

- append `.ach`;
- generate aliases derived from the batch;
- generate aliases derived from `IDLOTE`;
- generate aliases derived from `BatchNumber`.

## 2.5 Mandatory limits

It is prohibited to:

- call unauthorized external endpoints;
- execute against real production infrastructure;
- use production credentials;
- connect to external financial networks;
- generate real monetary movements;
- delete audit evidence;
- print credentials;
- print tokens;
- print complete account numbers;
- print personal data;
- print complete sensitive XML payloads;
- resend an entry that already received a successful or functionally definitive SOAP response.

There is no artificial global limit of one SOAP call when multiple eligible entries exist.

There is no general requirement to restore `DryRun` after a controlled local LIVE test.

There is no general requirement to stop the complete flow after the first correctable failure.

Idempotency, deduplication and prevention of definitive resends remain mandatory.

---

# 3. Platform and Shell Policy

Primary development environment:

- Windows;
- Codex CLI;
- Docker Desktop / Docker Compose where applicable.

Use **CMD** for repository and tooling instructions unless the user explicitly requests another shell.

Do not use PowerShell-specific commands in instructions, scripts or examples unless explicitly requested.

When writing portable repository scripts, prefer cross-platform tools or the repository's established scripting conventions.

Do not hardcode workstation-specific paths into application source.

MCP configuration may contain workstation-specific paths when required, but those paths must not leak into product code or portable repository documentation unless explicitly intended.

---

# 4. Repository Map

- Solution: `ACHInterbank.sln`
- Main API: `src/Cfa.ACHInterbank.Api`
- Application: `src/Cfa.ACHInterbank.Application`
- Domain: `src/Cfa.ACHInterbank.Domain`
- Persistence/infrastructure: `src/Cfa.ACHInterbank.Persistence`
- Backend tests: `tests/Cfa.ACHInterbank.Tests`
- NACHA golden files: `tests/Cfa.ACHInterbank.Tests/TestData/Nacha/GoldenFiles`
- Permanent Phase 6 context: `docs/ai/ACH_PHASE6_CONTEXT.md`
- Persistent project memory: `MEMORY.md`

Do not explore the complete repository when the relevant component is already known.

---

# 5. Build and Test Commands

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
- 0 errors.
- 0 warnings when the repository baseline requires it.
- Relevant tests passing.

## 5.1 Validation strategy

Prefer the narrowest useful validation first.

Order:

1. affected unit/focal tests;
2. affected project tests;
3. build;
4. affected integration tests;
5. runtime-backed tests when required;
6. broader regression suite when justified by scope.

Do not execute the complete suite repeatedly when focal validation is sufficient during diagnosis.

Before finalizing a significant change, execute the validations required to demonstrate that:

- the requested behavior works;
- the original defect is fixed;
- the affected area did not regress.

---

# 6. Phase 6 Guardrails

- Phase 6B.3C/6B.3C.1 golden files are semireal and anonymized and are not official certification artifacts.
- Phase 6B.4 covers internal incoming NACHA decisions.
- Phase 6B.5 covers SOAP integration boundaries.
- `Proc_Contrapartidas` and `Proc_Transacciones` may execute only against the explicitly authorized controlled local environment defined in this file.
- They remain prohibited against real external production infrastructure.
- `RegistrarRespuestaTransaccion` is non-monetary and must not independently move money.
- `None`, duplicates and `ManualReviewRequired` must not produce an inappropriate SOAP dispatch.
- A transaction with a successful or functionally definitive response must not be resent.
- When historical Phase 6 documentation contradicts the active controlled LIVE policy in this file, this file governs controlled local testing.

---

# 7. Tool Responsibility Matrix

Use each tool only for the responsibility it solves best.

| Need | Preferred tool |
|---|---|
| Previous verified project state | `ach_project_memory` |
| Durable decisions | `ach_project_memory` |
| Open/closed gaps | `ach_project_memory` |
| Known traps/root causes | `ach_project_memory` |
| Release candidate state | `ach_project_memory` |
| Code architecture | `codebase-memory` |
| Classes, methods, dependencies | `codebase-memory` |
| Callers / callees | `codebase-memory` |
| Execution paths | `codebase-memory` |
| Impact analysis | `codebase-memory` |
| Normative documents | `local-rag` |
| ACH Colombia rules | `local-rag` |
| CENIT rules | `local-rag` |
| Large/noisy terminal output | `RTK` when appropriate |
| Exact concrete file | Direct read |
| Exact runtime evidence | Original command/output |
| Current Git truth | Git |
| Current CI/PR evidence | GitHub when available |

Primary routing rule:

```text
Previous project state -> ach_project_memory
Normative rules        -> local-rag
Code structure         -> codebase-memory
Terminal compression   -> RTK
Exact file             -> direct read
Current runtime truth  -> direct evidence
Current Git truth      -> Git
```

Do not use multiple tools to answer exactly the same question unless cross-validation is technically necessary.

Project Memory is a context-routing and historical-state layer.

It is not a normative oracle.

It is not a source-code oracle.

---

# 8. Project Memory — Persistent Project State

Expected MCP server:

`ach_project_memory`

Persistent file:

`MEMORY.md`

at the repository root.

Project Memory exists to prevent future sessions from rediscovering durable, already verified project knowledge.

Its responsibility is limited to:

- durable architecture decisions;
- durable business decisions;
- verified business invariants;
- confirmed gaps;
- verified closures;
- known traps;
- proven root causes;
- release-candidate state;
- evidence pointers;
- delivery objectives;
- compact handoff information.

Project Memory must not become:

- a duplicate source-code index;
- a copy of Local RAG;
- a complete session transcript;
- a command log store;
- a CI log store;
- a source of credentials;
- a certificate repository;
- a private-key repository;
- a source of production data;
- a replacement for Git history;
- a replacement for tests;
- a replacement for runtime evidence.

`MEMORY.md` must be maintained in English.

Never store sensitive information in Project Memory.

## 8.1 Mandatory progressive retrieval

Never blindly retrieve the complete `MEMORY.md`.

When previous project knowledge may materially affect a non-trivial task, use:

```text
get_project_memory(head_only=true)
        |
        v
search_project_memory("<specific keywords or stable ID>")
        |
        v
get_project_memory(offset=<relevant line>, limit=<bounded range>)
```

Use `head_only` to determine:

- memory size;
- headings;
- section boundaries.

Then search narrowly.

Examples:

```text
CENIT RETURN
```

```text
DIFF-RESP-001
```

```text
TRAP DATE
```

```text
RC-CURRENT
```

```text
OPEN GAP
```

Retrieve only the relevant line range.

A complete memory read is acceptable only when:

- the file is demonstrably small; or
- deliberate consolidation/recovery requires the complete content.

## 8.2 When to query Project Memory

Query Project Memory when the current task depends on:

- what was previously decided;
- whether a gap was previously confirmed;
- whether a capability was previously closed;
- previously proven root causes;
- known traps;
- previous release evidence;
- current release-candidate state;
- previous delivery handoff;
- durable constraints that materially affect the requested work.

Project Memory is normally relevant for:

- return processing;
- differential responses;
- operational-date handling;
- cycle/date coherence;
- release preparation;
- UAT readiness;
- previously closed JOBs;
- regressions;
- recurring runtime failures;
- previous normative/implementation gaps.

It may be unnecessary for:

- isolated text changes;
- simple CSS changes;
- trivial naming changes;
- formatting-only changes with no behavioral effect.

## 8.3 Project Memory write policy

Update Project Memory only when durable verified knowledge changes.

Allowed writes:

- durable architecture decision;
- durable business decision;
- confirmed invariant;
- newly confirmed gap;
- gap closure with evidence;
- proven root cause;
- reusable known trap;
- release-candidate update;
- verified CI/runtime/UAT evidence pointer;
- meaningful delivery handoff;
- invalidation of stale memory.

Do not write:

- ordinary investigation progress;
- speculative hypotheses;
- chain-of-thought;
- temporary command output;
- raw logs;
- complete stack traces;
- repetitive test output;
- source code;
- full normative text;
- secrets;
- tokens;
- credentials;
- private keys;
- certificates;
- connection strings;
- complete account numbers;
- personal data;
- production data.

## 8.4 Project Memory write mechanism

Prefer:

`update_project_memory`

for ordinary incremental changes.

Use:

`set_project_memory`

only for:

- initial bootstrap;
- deliberate full consolidation;
- recovery from malformed memory;
- controlled complete replacement.

Do not rewrite the entire memory for a one-item update.

## 8.5 Stable searchable IDs

Use stable identifiers for durable knowledge.

Recommended prefixes:

```text
DEC-
INV-
GAP-
CLOSE-
TRAP-
EVID-
RC-
NEXT-
NORM-
```

Examples:

```text
DEC-DATE-001
INV-IDEMPOTENCY-001
GAP-CENIT-RET-001
CLOSE-DIFF-RESP-001
TRAP-DATE-UTC-001
EVID-JOB6-001
RC-CURRENT
NEXT-DELIVERY-001
```

Do not create multiple IDs for the same durable fact merely because it was encountered in different sessions.

## 8.6 Closure evidence requirement

A memory entry must not be marked `CLOSED` solely because:

- an agent says it was fixed;
- source code changed;
- one command succeeded;
- an old conversation described it as closed.

A closure should contain or reference evidence such as:

- exact affected behavior;
- focal test;
- regression test;
- build result;
- runtime-backed E2E;
- CI result;
- exact commit SHA;
- other directly verifiable evidence.

If evidence is incomplete, preserve a weaker state.

## 8.7 Release candidate rule

Release certification applies to an exact commit.

Project Memory should maintain:

```text
RC-CURRENT
COMMIT: <exact SHA>
STATUS: <state>
CI: <state/evidence>
RUNTIME_E2E: <state/evidence>
GAUNTLET: <state>
UAT: <state>
```

A source-code change invalidates certification of the previous candidate for the affected behavior.

Never transfer:

- `GAUNTLET_PASSED`;
- `UAT_READY`;
- `RELEASE_READY`;

from one commit to another without new evidence.

## 8.8 Staleness and conflicts

Project Memory represents previous verified knowledge.

It is not immutable truth.

If Project Memory conflicts with:

- current source code;
- current configuration;
- current tests;
- current runtime evidence;
- current Git state;
- current normative evidence;

then:

1. preserve the contradiction;
2. determine which source governs the question;
3. verify the current state independently;
4. treat the memory entry as potentially stale;
5. update or invalidate it only after verification.

Never force current evidence to fit old memory.

## 8.9 No Project Memory indexing

Project Memory does not require repository-style indexing.

Do not:

- run Codebase Memory indexing to refresh `MEMORY.md`;
- run Local RAG synchronization to refresh `MEMORY.md`;
- create a second vector database containing Project Memory;
- duplicate `MEMORY.md` into normative corpora.

## 8.10 Memory compactness

Keep `MEMORY.md` compact and information-dense.

Prefer:

```text
fact
-> evidence
-> durable consequence
```

over narrative session history.

Replace obsolete current-state entries rather than endlessly appending duplicates.

Use Git history when historical versions of `MEMORY.md` are required.

## 8.11 Project Memory fallback

If `ach_project_memory` is unavailable:

- do not stop the task solely because of its absence;
- do not invent remembered project state;
- use current source/runtime/normative evidence as appropriate;
- keep investigation narrow;
- report the missing memory layer briefly when it materially affects confidence.

Project Memory availability must never affect application runtime behavior.

---

# 9. Codebase Memory — Mandatory Structural Retrieval

The Codebase Memory project for this repository is:

`ACHInterbank`

Its responsibility is technical knowledge of the repository:

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
- repository infrastructure;
- execution paths;
- impact analysis.

## 9.1 Normative separation

The former Codebase Memory project:

`ACHInterbank-normativa`

is not the primary normative source.

Normative retrieval is the responsibility of:

`local-rag`

Do not duplicate normative documents into source directories merely to make Codebase Memory discover them.

Do not repeatedly reindex `ACHInterbank` in an attempt to ingest the external Local RAG corpus.

Do not duplicate Project Memory into Codebase Memory.

## 9.2 When to synchronize Codebase Memory

Treat `index_repository` as an incremental synchronization.

Synchronize when the index may be stale, especially after:

- changing Git branch;
- `git pull`;
- significant merge;
- adding a module;
- substantial SPA component/route changes;
- backend controller changes;
- handler changes;
- service changes;
- entity changes;
- EF Core configuration changes;
- migration changes;
- SQL changes;
- moving files;
- renaming files;
- deleting files;
- significant test changes;
- Docker/infrastructure changes represented in the repository;
- reinstalling/updating Codebase Memory;
- detecting that recent code cannot be found.

Do not run `index_repository` automatically before every task.

Do not repeat indexing during the same task when no repository change occurred.

Do not delete the existing index or force a full rebuild unless corruption or inconsistency is demonstrated.

## 9.3 Codebase Memory query strategy

Use the most specific tool possible.

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
- impact analysis.

### `get_code_snippet`

Use when the exact symbol is already known.

Retrieve only the implementation required.

### `search_code`

Use for:

- literal text;
- configuration;
- constants;
- repository documentation;
- SQL;
- information unavailable from the graph.

### `get_architecture`

Use only when broader architecture understanding is actually required.

Do not call it for narrowly scoped changes when the affected component is already known.

### `get_graph_schema` / `query_graph`

Use only when graph relationships cannot be answered efficiently through simpler tools.

## 9.4 Codebase Memory fallback

If Codebase Memory:

- is unavailable;
- returns errors;
- has an obviously stale graph;
- cannot locate information known to exist;

then:

1. report the limitation briefly;
2. fall back to targeted local searches;
3. keep the search scope narrow;
4. read only the necessary files.

Do not turn a Codebase Memory failure into an unrestricted repository scan.

---

# 10. Local RAG — Normative Source

The MCP:

`local-rag`

is the primary retrieval mechanism for external normative documentation.

Its configured corpus is external to the repository.

Do not hardcode workstation-specific absolute paths into application source.

The Local RAG root is defined through MCP configuration.

The corpus is expected to separate at least:

```text
ACH-Colombia/
CENIT/
```

Preserve that separation during retrieval.

## 10.1 Responsibility

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
- returns of returns;
- differential responses when normatively applicable;
- cycle rules;
- file structures;
- record fields;
- operating windows;
- security requirements;
- limits;
- normative annexes;
- other external clearing/regulatory documentation in the configured corpus.

Do not use Local RAG for:

- locating application classes;
- callers/callees;
- source-code architecture;
- runtime logs;
- repository dependency analysis;
- historical project-state memory.

Those belong to Codebase Memory, Project Memory or direct inspection.

---

# 11. Local RAG Synchronization Policy

Do not synchronize Local RAG before every query.

Synchronize when:

- new normative documents were added;
- documents changed;
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

Synchronization is an indexing operation.

It is not a prerequisite for every normative question.

---

# 12. Local RAG Query Strategy

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
ACH Colombia V35 received return D33 cause
```

```text
ACH Colombia V35 operator returns
```

```text
CENIT debit return next cycle
```

Avoid vague queries such as:

```text
returns
```

when sufficient context exists for a more precise request.

## 12.1 Query workflow

For a normative task:

1. query the relevant corpus with `query_documents`;
2. evaluate the highest-relevance results;
3. refine the query when results mix unrelated subjects;
4. use `read_chunk_neighbors` only when surrounding context is required;
5. identify source document;
6. identify version when available;
7. identify section/context when available;
8. extract only the rule needed for the task.

Do not ingest complete documents when a few chunks are sufficient.

## 12.2 Normative evidence standard

A normative conclusion should identify, when available:

- clearing house;
- source document;
- version/date;
- section;
- retrieved rule;
- whether the evidence is direct or inferred from neighboring context.

Never invent:

- section numbers;
- versions;
- causes;
- deadlines;
- formats;
- field values;
- operational rules.

If Local RAG cannot support a rule, explicitly state that normative evidence was not found.

---

# 13. ACH Colombia / CENIT Isolation

Never automatically merge ACH Colombia and CENIT rules.

## 13.1 ACH Colombia-only task

Search the ACH Colombia corpus first.

Do not use CENIT rules as substitutes.

## 13.2 CENIT-only task

Search the CENIT corpus first.

Do not use ACH Colombia rules as substitutes.

## 13.3 Comparative task

When comparing ACH Colombia and CENIT:

1. retrieve ACH Colombia independently;
2. retain its evidence;
3. retrieve CENIT independently;
4. retain its evidence;
5. compare only after both retrievals are complete.

Absence of a rule in one clearing house does not imply that a rule from the other clearing house applies.

---

# 14. Current ACH Colombia Normative Baseline

The canonical normative baseline for current ACH Colombia behavior is:

**Version 35 — April 2026**

Versions V32 and earlier are historical/comparative unless the task explicitly targets them.

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
2. never silently combine V35 and older versions;
3. identify the version of every rule used;
4. use older versions only for historical comparison or migration analysis.

If V35 cannot be located, report that condition rather than silently falling back to V32.

If code, tests, repository documentation, Project Memory or current runtime behavior contradict V35:

1. preserve the normative evidence;
2. verify implemented behavior independently;
3. report the discrepancy;
4. use V35 as normative authority unless another version is explicitly in scope;
5. do not invent a reconciliation rule.

For returns, investigate especially:

- section 6.6;
- section 6.7;
- applicable annexes.

Consider cause `D33` only when supported by retrieved normative evidence and applicable to the exact scenario.

---

# 15. Normative Rules vs Implementation

When a functional decision depends on both normative requirements and current implementation, use this order.

## Step 0 — Project Memory

When prior state matters:

- retrieve previous decisions;
- retrieve known traps;
- retrieve previous gaps;
- retrieve previous evidence.

Use Project Memory to avoid rediscovery.

Do not use it as current normative or implementation proof.

## Step 1 — Local RAG

Establish what the applicable normative rule requires.

## Step 2 — Codebase Memory

Establish where and how the behavior is currently implemented.

## Step 3 — Direct file read

Inspect only the concrete implementation identified.

## Step 4 — Tests

Identify existing evidence and affected coverage.

## Step 5 — Implementation

Make the minimum coherent change.

## Step 6 — Verification

Execute the minimum evidence-producing validation required.

## Step 7 — Project Memory update

Persist only new durable verified knowledge.

The analysis should conceptually produce:

```text
Previous memory
  -> prior decisions / traps / gaps

Normative evidence
  -> required behavior

Implementation evidence
  -> current behavior

Gap
  -> demonstrable difference

Action
  -> minimum coherent correction

Verification
  -> evidence proving the behavior

Durable memory
  -> only if reusable verified knowledge changed
```

Never reason:

```text
The code does X, therefore the normative rule must require X.
```

Never reason:

```text
The normative rule requires X, therefore the code probably already does X.
```

Never reason:

```text
MEMORY.md says CLOSED, therefore the current commit must still be correct.
```

Normative and implementation evidence must be independently verified whenever both matter.

---

# 16. RTK — Terminal Noise Reduction

RTK, Project Memory, Codebase Memory and Local RAG solve different problems.

- **Project Memory**: durable previous project state.
- **Codebase Memory**: repository knowledge and structure.
- **Local RAG**: normative retrieval.
- **RTK**: compact terminal output.
- **Direct read**: exact inspection of known files.
- **Direct runtime command**: exact current execution evidence.

RTK does not replace any other source.

## 16.1 Use RTK when appropriate

When available and supported, prefer RTK for terminal commands that may produce large noisy output.

Examples:

```bash
rtk git status
rtk git diff
rtk git log -n 10
rtk grep "<pattern>" .
rtk find "<pattern>" .
rtk docker ps
rtk docker logs <container>
```

For .NET, when supported:

```bash
rtk dotnet build ACHInterbank.sln -c Release
```

For large test output:

```bash
rtk test dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release
```

After an already successful build:

```bash
rtk test dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release --no-build
```

Canonical underlying commands remain unchanged.

RTK must never change:

- arguments;
- project target;
- test scope;
- command semantics.

If an RTK wrapper is unsupported by the installed version, use the original command.

## 16.2 Keep RTK searches narrow

Prefer:

```bash
rtk grep "Proc_Transacciones" src/Cfa.ACHInterbank.Application
```

over:

```bash
rtk grep "Proc_Transacciones" .
```

when the relevant area is already known.

Do not manually reconstruct architecture using terminal searches when Codebase Memory can retrieve it more efficiently.

---

# 17. When Exact Output Is Required

Do not rely exclusively on compact RTK output when the task requires:

- complete error evidence;
- exact values;
- exact ordering;
- audit evidence;
- UAT evidence;
- exact logs;
- diagnosis of a hidden/filtered failure;
- literal comparison against a specification;
- proof that may depend on omitted lines.

In these cases:

1. use RTK first when useful;
2. identify the narrow relevant segment;
3. rerun only the required command without RTK;
4. capture only the necessary exact evidence.

Using the original command after RTK is allowed when technically justified.

Project Memory may retain a compact evidence pointer or conclusion.

It must not replace exact evidence when exact proof is required.

---

# 18. RTK Availability

Do not assume RTK exists in every environment.

If `rtk` is unavailable:

- do not stop the task solely because of its absence;
- use the equivalent original command;
- preserve narrow scope;
- do not modify application dependencies to install RTK;
- do not introduce RTK as an application runtime dependency.

RTK availability must not affect application behavior.

---

# 19. Direct File Reading

Direct reading is appropriate after the relevant file has been identified.

Prefer:

- one method;
- one class;
- one configuration section;
- one test;
- one relevant line range;

over loading a complete large file.

Do not read entire directories or large files merely because they may contain relevant information.

Preferred discovery order:

```text
Previous project state -> Project Memory
Technical target       -> Codebase Memory
Normative target       -> Local RAG
Known exact target     -> direct read
```

---

# 20. Context Economy

The objective is:

**Use the minimum sufficient context that preserves all evidence required for a technically correct decision.**

Avoid:

- automatically loading complete `MEMORY.md`;
- retrieving unrelated Project Memory sections;
- storing temporary investigation output in Project Memory;
- global repository searches when the component is known;
- loading complete normative documents when one chunk is sufficient;
- reading complete source files when one symbol is sufficient;
- querying Codebase Memory and RTK for the same fact;
- querying Local RAG for purely technical tasks;
- querying Codebase Memory for normative rules;
- using Project Memory as current source-code truth;
- using Project Memory as normative truth;
- synchronizing Local RAG when documents have not changed;
- repeatedly synchronizing Codebase Memory without repository changes;
- repeating a search already answered in current context;
- rediscovering durable facts already stored in Project Memory unless freshness is questionable;
- collecting complete logs when a bounded window is enough;
- introducing unrelated documentation;
- introducing unrelated code;
- introducing unrelated tests;
- introducing unrelated runtime logs.

Do not minimize context at the expense of required evidence.

Do not maximize context merely because more information exists.

Prefer progressive retrieval over bulk ingestion.

---

# 21. Task Classification Before Tool Use

Before using tools:

1. identify the requested outcome;
2. classify the task;
3. determine whether previous project state matters;
4. select only the necessary tools.

## 21.1 Historical/status-sensitive task

Examples:

- previous closure;
- previous root cause;
- known trap;
- current release candidate;
- previous GAP status;
- previous UAT evidence.

Use:

```text
Project Memory head
-> narrow search
-> bounded memory range
-> current evidence only if freshness matters
```

Do not scan the repository merely to reconstruct history already stored in Project Memory.

## 21.2 Pure technical task

Examples:

- compile error;
- dependency injection;
- Angular component;
- EF configuration;
- controller;
- service;
- unit test;
- Docker configuration.

When previous state matters:

```text
Project Memory targeted retrieval
-> Codebase Memory
-> direct read
-> RTK/terminal
```

When previous state cannot affect the result:

```text
Codebase Memory
-> direct read
-> RTK/terminal
```

Do not query Local RAG unless a normative/business rule becomes relevant.

## 21.3 Pure normative task

Examples:

- what ACH Colombia requires;
- allowed return causes;
- CENIT rule;
- version comparison.

Use:

```text
Local RAG
-> neighboring chunks only when necessary
```

Do not query Project Memory merely to answer what a normative document says.

Do not inspect code unless implementation comparison is requested.

## 21.4 Normative + implementation task

Examples:

- does ACHInterbank comply with V35?;
- is this return implemented correctly?;
- differential-response gap;
- ACH Colombia versus current implementation.

Use:

```text
Project Memory targeted retrieval when previous state matters
-> Local RAG
-> Codebase Memory
-> direct read
-> tests
-> runtime evidence when required
```

## 21.5 Runtime diagnosis

Use:

```text
Project Memory targeted retrieval when previous traps/failures matter
-> Codebase Memory when structure is unknown
-> RTK/terminal
-> exact output when required
-> direct read
```

## 21.6 Release / UAT readiness

Use:

```text
Project Memory
-> exact Git commit
-> CI evidence
-> runtime-backed evidence
-> normative evidence when applicable
-> Gauntlet evidence when applicable
-> release-state decision
```

Never infer release readiness from development closure alone.

---

# 22. Expected Development Workflow

For a relevant development task:

1. determine the exact requested outcome;
2. classify the task;
3. determine whether previous durable project state matters;
4. when relevant, retrieve Project Memory progressively:
   - `head_only`;
   - narrow search;
   - bounded range;
5. choose only the remaining necessary tools;
6. synchronize Codebase Memory only if stale;
7. synchronize Local RAG only if its corpus may be stale;
8. retrieve minimum technical/normative context;
9. identify affected components;
10. identify dependencies/execution paths when necessary;
11. identify affected tests;
12. read only concrete files/symbols required;
13. implement the minimum coherent change;
14. run focal validation;
15. expand validation only when justified;
16. inspect exact output if compact output is insufficient;
17. reindex Codebase Memory when significant source changes must become queryable;
18. do not reindex Local RAG unless normative documents changed;
19. update Project Memory only if new durable verified knowledge was produced;
20. report concise evidence.

Do not write Project Memory merely to record that a task happened.

A Project Memory write must make a future session cheaper or safer.

---

# 23. Delivery and Release State Model

Development completion and release readiness are different states.

Use the following conceptual progression:

```text
IMPLEMENTED
    |
    v
VERIFIED
    |
    v
GAUNTLET_PASSED
    |
    v
UAT_READY
    |
    v
USER_ACCEPTED
```

A JOB being `CLOSED` does not independently imply `UAT_READY`.

## 23.1 IMPLEMENTED

Means the requested behavior exists in code.

Does not imply adequate tests.

## 23.2 VERIFIED

Requires evidence appropriate to the change, such as:

- focal tests;
- regression tests;
- integration tests;
- build;
- runtime evidence.

## 23.3 GAUNTLET_PASSED

Only applies if an independent adversarial validation has been explicitly executed against the exact candidate commit.

Do not assume this state when Gauntlet has not been executed.

## 23.4 UAT_READY

Requires an exact candidate commit and sufficient evidence that the capability is safe to present to a user.

## 23.5 USER_ACCEPTED

Requires explicit user/business acceptance.

Do not infer this state from technical validation.

---

# 24. Gauntlet Policy — When Used

Gauntlet is an optional independent release-hardening layer.

It is not required for every development task.

Use it primarily for high-risk flows such as:

- monetary transaction flows;
- NACHA generation/parsing;
- inbound/outbound processing;
- returns;
- returns of returns;
- prenotifications;
- differential responses;
- cycle/date logic;
- idempotency;
- transport;
- settlement;
- release candidates.

Do not use Gauntlet for trivial:

- CSS changes;
- copy changes;
- renaming;
- formatting;
- isolated low-risk refactors.

## 24.1 Separation of roles

The Gauntlet critic must not modify code.

The critic may only return:

```text
PASS
```

or:

```text
FAIL
+ evidence
+ reproduction
+ violated invariant/rule
```

The implementation agent performs corrections.

A subsequent independent critic validates the corrected candidate.

Do not let the same role implement and certify its own result.

## 24.2 Evidence

Gauntlet findings should refer to:

- exact candidate commit;
- tested scenario;
- expected behavior;
- observed behavior;
- evidence;
- relevant invariant/normative rule when applicable.

## 24.3 Loop limit

Avoid unbounded adversarial loops.

Preferred release-hardening sequence:

```text
Pass 1 -> discovery
Pass 2 -> regression verification
Pass 3 -> release-candidate validation
```

If a critical failure remains after controlled passes, preserve:

`NOT_READY`

and report the unresolved blocker.

## 24.4 Regression conversion

A confirmed Gauntlet defect should become a deterministic permanent regression test whenever practical.

Preferred progression:

```text
Gauntlet finding
-> proven defect
-> correction
-> permanent regression test
-> CI protection
```

---

# 25. Final Response Expectations

For code-changing tasks, keep final responses concise.

When useful, report:

- status;
- cause/root cause;
- change;
- affected files;
- tests executed;
- relevant test result;
- Codebase Memory use;
- Local RAG evidence when normative rules affected the decision;
- runtime evidence;
- Project Memory entry updated when durable knowledge changed;
- exact commit/PR when explicitly created.

Do not produce a long narrative when a concise technical summary is sufficient.

For normative work, identify:

- clearing house;
- document;
- version/date when available;
- relevant section/evidence.

For mixed normative/implementation work, distinguish:

- Project Memory context;
- normative evidence;
- implementation evidence;
- demonstrated gap;
- correction;
- verification evidence.

Never present Project Memory as direct normative or runtime evidence.

---

# 26. Prohibited Tool Anti-Patterns

Do not:

- load complete `MEMORY.md` before every task;
- use Project Memory as current runtime evidence;
- use Project Memory as normative evidence;
- use Project Memory as a source-code index;
- store full logs in Project Memory;
- store speculative hypotheses as durable facts;
- mark a Project Memory gap `CLOSED` without evidence;
- transfer release certification between commits;
- rewrite complete Project Memory for every small update;
- run `index_repository` before every prompt;
- run `sync_start` before every Local RAG query;
- perform broad `rg`/`grep` before trying Codebase Memory when structural discovery is required;
- use Codebase Memory as normative authority;
- use Local RAG to infer source-code architecture;
- run RTK and immediately repeat the same full unfiltered command without a technical reason;
- repeatedly read the same file;
- repeatedly retrieve the same Project Memory range;
- repeatedly retrieve the same RAG chunk;
- load every normative document for one rule;
- combine ACH Colombia and CENIT evidence without separating sources;
- treat an old ACH Colombia version as current when V35 exists;
- assume absence from one RAG query proves absence from the corpus;
- make normatively governed code changes before establishing the applicable rule;
- create hardcoded clearing-house behavior when configurable/profile-driven behavior is appropriate.

---

# 27. Source Hierarchy

Different questions have different authoritative sources.

## 27.1 Current implementation truth

For what the current repository implements:

1. current source code;
2. current configuration;
3. current tests.

Codebase Memory accelerates discovery.

It does not replace exact inspection when exact evidence is required.

## 27.2 Current runtime truth

For what the running system actually did:

1. exact runtime evidence;
2. logs/events/audit evidence;
3. persisted state;
4. runtime-backed E2E evidence.

A source-code reading does not replace runtime evidence when the question concerns observed behavior.

## 27.3 Previous verified project-state truth

For what was previously established:

1. `MEMORY.md`;
2. referenced Git commits;
3. referenced CI/runtime/UAT evidence.

Project Memory answers:

```text
What was previously verified or decided?
```

It does not independently answer:

```text
What is true in the current commit?
```

when the repository has changed.

## 27.4 ACH Colombia normative truth

1. applicable current normative document retrieved through Local RAG;
2. Version 35 for current ACH Colombia behavior;
3. older versions only for explicit historical/comparative work.

## 27.5 CENIT normative truth

1. applicable current CENIT document retrieved through Local RAG;
2. older documents only when explicitly relevant.

Repository documentation may provide context.

It must not silently override a newer applicable normative source.

Project Memory may store normative pointers or previous conclusions.

It must not override the applicable normative document.

Runtime behavior proves what the system did.

Runtime behavior does not independently prove what the normative rule requires.

---

# 28. Guiding Principle

Every task should converge through the shortest evidence-preserving path.

```text
Question
   |
   +-- Does previous verified project state matter?
   |       |
   |       +--> ach_project_memory
   |                |
   |                +--> head
   |                +--> narrow search
   |                +--> bounded range
   |
   +-- Is the question normative?
   |       |
   |       +--> local-rag
   |
   +-- Is the question about code structure?
   |       |
   |       +--> codebase-memory
   |
   +-- Is the exact file/symbol known?
   |       |
   |       +--> direct read
   |
   +-- Does execution matter?
           |
           +--> RTK / terminal
                    |
                    +--> exact output only when required
```

For mixed functional work:

```text
Project Memory
      |
      v
Previous decisions / gaps / traps
      |
      v
Local RAG
      |
      v
Normative requirement
      |
      v
Codebase Memory
      |
      v
Current implementation
      |
      v
Normative vs implementation
      |
      v
Demonstrable gap
      |
      v
Minimum coherent correction
      |
      v
Focused validation
      |
      v
Durable new knowledge?
     / \
   yes  no
    |    |
    v    |
Project  |
Memory   |
    \    /
     v  v
     Result
```

For release-oriented work:

```text
Project Memory
      |
      v
Current candidate state
      |
      v
Exact commit SHA
      |
      +--> CI evidence
      |
      +--> runtime-backed evidence
      |
      +--> normative evidence when applicable
      |
      +--> Gauntlet evidence when applicable
      |
      v
Release decision
      |
      v
Project Memory update
```

The goal is not to use every available tool.

The goal is to use the **smallest set of tools and context that produces a verifiable, technically correct result**.

Project Memory should reduce rediscovery.

Codebase Memory should reduce repository exploration.

Local RAG should reduce normative-document loading.

RTK should reduce terminal noise.

Deterministic tests should preserve discovered behavior.

Gauntlet, when used, should attempt to invalidate the candidate independently.

Direct evidence remains mandatory whenever correctness depends on exact current behavior.