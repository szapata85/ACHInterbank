# Initial root MEMORY.md patch for REFACTOR-CA-SOLID-RES-001

Append the following compact block to the repository root `MEMORY.md` when the refactor program is formally started. Keep detailed transient execution state in `MEMORY_REFACTOR_STATE.md`.

```markdown
## REFACTOR-CA-SOLID-RES-001

STATUS: PLANNED. Incremental Clean Architecture/SOLID/resilience refactor for the current ACHInterbank tree. The program must preserve accepted ACH Colombia/CENIT behavior, immutable PUBLISH NACHA authority, golden outputs absent a proven normative change, idempotency/deduplication/audit, cluster safety and managed-MFT semantics. It does not authorize the separate PaymentRail blueprint, a normative change, an external production connection, UAT or release claims.

- DEC-REF-001: Execute one refactor phase/subphase at a time using `PLAN_IMPLEMENTACION_REFACTOR_CA_SOLID_RESILIENCIA.md` and `MEMORY_REFACTOR_STATE.md`; current code/tests and accepted memory/normative evidence override stale plan assumptions.
- DEC-REF-002: Architecture enforcement uses a shrinking legacy allowlist/ratchet. New boundary violations are prohibited; an exception is removed when its phase closes.
- INV-REF-001: Refactoring must not change ACH Colombia/CENIT rules, NACHA layouts/causes/cycles/returns, PUBLISH authority or golden files by convenience.
- INV-REF-002: Resilience policies are operation-specific; non-idempotent external operations must not receive blind retries.
- INV-REF-003: A phase is VERIFIED only with executable evidence on the exact current tree/commit; otherwise use IMPLEMENTED_UNVERIFIED or BLOCKED.
- NEXT-REF-001: Execute R0 only: baseline, architecture ratchet and characterization/golden guards. Do not start R1 in the same implementation session.
```
