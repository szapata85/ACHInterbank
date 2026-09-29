# AGENTS.md — ACHInterbank

Permanent coding-agent instructions. Current decisions and status belong in `MEMORY.md`; human-facing architecture, normative, operational, UAT, and audit material belongs in `docs/`. Consult `docs/ai/ACH_PHASE6_CONTEXT.md` when Phase 6 or transaction work requires it.

## General rules

- Keep responses concise and evidence based. Do not explain code unless asked; prefer minimal diffs for code changes.
- Preserve Clean Architecture: Domain, Application contracts/use cases, Persistence/infrastructure, API composition, and isolated frontend concerns. Favor dependency inversion and deterministic, testable behavior.
- Keep ACH Colombia/CENIT behavior, causes, record layouts, transaction codes, cycles, settlement windows, routing, filenames, limits, and returns profile/configuration/catalog driven. Do not add legacy hardcoded layouts or bypass existing rule engines.
- EF Code First owns versioned schema; create migrations only when needed. Never alter production-like schema to bypass a migration.
- Do not reduce coverage, weaken assertions, or change expected results to match a defect. Tests alone do not prove regulatory compliance or a normative change.
- Change NACHA-M golden files only on explicit request or proven normative change; change the table-driven engine only for a proven defect or verified normative change. Investigate normative requirements and implementation independently.
- Preserve exact evidence where values, ordering, runtime state, regulatory text, or test output matter. Never assert CLOSED, VERIFIED, GAUNTLET_PASSED, UAT_READY, USER_ACCEPTED, or RELEASE_READY without appropriate scope and commit evidence.
- Do not reopen accepted functional or architectural contracts without contradictory current evidence. Their current state is in `MEMORY.md`.

## Security and controlled local LIVE

- Never expose, print, commit, or persist real credentials, tokens, keys, certificates, full account numbers, personal data, complete sensitive SOAP/XML, or production connection strings. Never generate real monetary movement or connect to external production infrastructure or financial networks.
- Productivo NO-GO applies to real external production. Authorized controlled local endpoints: API `http://localhost:843`; SPA `http://localhost:743`; SOAP `http://localhost:7083/WSCFAACH.svc` or `http://host.docker.internal:7083/WSCFAACH.svc` with HostHeader `localhost:7083`; local SQL Server container `achinterbank-sqlserver`. Equivalent `127.0.0.1` is allowed. Other hosts are external absent later explicit safe authorization.
- Controlled local LIVE may retain `ProcTransacciones__Mode=Live`, call local WCF, execute `Proc_Contrapartidas`, `Proc_Transacciones`, and non-monetary `RegistrarRespuestaTransaccion`, process complete multibatch/multientry/multiaddenda NACHA-M files, upload controlled fixtures, correct local code/configuration/data, invoke an orchestrator through DI, and resume after a correctable failure. Production-origin files may be used only as controlled fixtures. No global one-SOAP-call limit or mandatory DryRun restoration applies.
- Per file: one ingestion. Per entry: classification and correlation. Per eligible entry: queue, SOAP call, persisted response. Preserve idempotency, deduplication, and audit evidence. Never resend after a successful or functionally definitive response; `None`, duplicates, and `ManualReviewRequired` must not dispatch SOAP.
- CENIT LIVE fixtures: `docs/uat/proc-transacciones-live/CENIT`, name `^\d{7}\.\d{3}\.\d{8}\.\d+$`, no extension. ACH Colombia LIVE fixtures: `docs/uat/proc-transacciones-live/ACHCOL`, name `^\d{7}\.\d{3}\.\d{8}\.\d+\.OUT$`. Never append `.ach` or derive aliases from batch, IDLOTE, or BatchNumber.

## Platform and validation

- Primary environment: Windows, Codex CLI, Docker Desktop/Compose. Use CMD for repository/tooling instructions unless the user requests another shell. Do not embed workstation paths in product code or portable documentation.
- Solution `ACHInterbank.sln`; API `src/Cfa.ACHInterbank.Api`; Application `src/Cfa.ACHInterbank.Application`; Domain `src/Cfa.ACHInterbank.Domain`; Persistence `src/Cfa.ACHInterbank.Persistence`; SQL Server migrations `src/Cfa.ACHInterbank.Persistence.Migrations.SqlServer`; backend tests `tests/Cfa.ACHInterbank.Tests`; NACHA golden files `tests/Cfa.ACHInterbank.Tests/TestData/Nacha/GoldenFiles`.
- Canonical backend commands: `dotnet build ACHInterbank.sln -c Release` and `dotnet test tests/Cfa.ACHInterbank.Tests/Cfa.ACHInterbank.Tests.csproj -c Release`; use `--no-build` after a successful build. Validate focal tests first, then affected project, build, integration/runtime, and broader regression when justified.
- Before restore/build after solution/project changes, run `python scripts/validate-solution-projects.py`. Every `.csproj` under `src/` and `tests/` and every `ProjectReference` target must be present in `ACHInterbank.sln`; do not remove the SQL Server migrations project to work around NuGet/Visual Studio restore errors.
- Use RTK to compress noisy terminal output when supported. Use the original command for unsupported wrappers or when exact audit/runtime evidence is required.

## Evidence and tool routing

| Need | Preferred authority/tool |
| --- | --- |
| Previous decisions, gaps, traps, release state | `ach_project_memory` / `MEMORY.md` |
| Current code structure, callers, impact | `codebase-memory` |
| ACH Colombia, CENIT, NACHA-M rules | `local-rag` |
| Exact known file or runtime result | Direct read / original command |
| Current repository and CI | Git / GitHub evidence |

- Project Memory is historical context, not normative, source, or runtime authority. Current source/config/tests govern implementation; runtime and persisted evidence govern observed behavior; applicable normative documents govern requirements.
- Retrieve memory progressively: `get_project_memory(head_only=true)`, narrow `search_project_memory`, bounded `get_project_memory(offset, limit)`. Full reads are for deliberate consolidation/recovery or tasks needing most sections. Do not bootstrap/reindex an existing `MEMORY.md`.
- Keep `MEMORY.md` English and compact: durable verified decisions, invariants, gaps, closures, traps, evidence pointers, release state. No raw logs, transcript, secrets, source-code index, or speculation. Prefer `update_project_memory`; use `set_project_memory` only for deliberate whole-file consolidation/recovery. Use stable IDs such as DEC-, INV-, GAP-, CLOSE-, TRAP-, EVID-, RC-, NEXT-, NORM-.
- Before a memory write, check `git diff -- MEMORY.md` and `git ls-files --eol MEMORY.md`; after, also check `git diff --ignore-space-at-eol -- MEMORY.md`. Preserve UTF-8/LF and unrelated edits. Never commit EOL-only churn. Close items only with sufficient behavioral and test/runtime/CI evidence. Release certification belongs to an exact commit and does not transfer.
- Use Codebase Memory `search_graph` for symbols, `trace_path` for execution/impact, `get_code_snippet` for a known symbol, `search_code` for literals, and `get_architecture` only for broad structure. Incrementally index after branch/merge/significant source changes or proven staleness, not every task. Fall back to targeted local search if unavailable.
- Use Local RAG for external normative material: narrow chamber/version/concept queries, neighbors only when needed, source/version/section when available. Synchronize only after corpus/config changes or proven stale index. Do not index normative documents into Codebase Memory or Project Memory into another index.
- Evaluate ACH Colombia and CENIT separately. ACH Colombia V35 (April 2026) is current authority; V32 and earlier are historical, and V36 is not current implementation authority without a later explicit decision. Search V35 before normatively governed changes; if unavailable, report it rather than silently falling back. For Returns examine sections 6.6/6.7 and annexes as applicable; use D33 only with scenario-specific evidence. Use accepted `DEC-ACHCOL-R10-DEV14-001` from memory rather than rederiving R10/DEV14.

## Durable transaction guardrails

- An incoming Return must correlate exact original trace and amount before mutating the original transaction.
- Prenotification differential responses are non-monetary and idempotent through `RegistrarRespuestaTransaccion`; they are not physical simulator files. The inbound simulator is generate-only for an external non-default counterparty.
- Operational dates follow the configured timezone; cycles must not target future operational dates. Effective-dated chamber cycle policy governs stages and eligibility.
- ACH Colombia V35 Return Addenda 99 has a three-character physical cause at positions 4–6 and original trace at 7–21. DEV14 is Claims-only, never an Rxx alias or Addenda 99 cause. R10 uses the accepted configurable same-day cycle window and proven basis in `DEC-ACHCOL-R10-DEV14-001`.
- CENIT local Gateway simulation is disabled by default and prohibited in Production. External Gateway/PO and managed-MFT deployment/homologation remain distinct from internal completion.
- Immutable `PUBLISH` NACHA snapshots are runtime authority for supported ordinary flows; do not revive live-row or static-layout fallbacks. PSE-specific inbound work remains deferred pending separate authority.

## Release discipline

- Development closure does not imply global release readiness. VERIFIED needs scope-appropriate test/build/runtime evidence; GAUNTLET_PASSED needs independent adversarial validation on the exact commit; UAT_READY needs a candidate and user-facing evidence; USER_ACCEPTED needs explicit business acceptance.
- Gauntlet is optional for routine changes and useful for monetary, NACHA, return, cycle, transport, settlement, idempotency, and release work. A critic reports PASS or FAIL with evidence and reproduction without editing code; convert confirmed defects to deterministic regression tests when practical.
- Never infer normative rules from code, implementation from normative text, or current correctness from old memory. Report only the precise evidence relevant to the task.
