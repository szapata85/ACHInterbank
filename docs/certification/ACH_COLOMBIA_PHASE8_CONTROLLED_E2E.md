# ACH Colombia — Phase 8 controlled E2E certification harness

**Status:** `PREPARED_NOT_EXECUTED`  
**Scope:** ACH Colombia only. This is an internal controlled harness, not external UAT, homologation, or production evidence.

## Safety boundary

The harness never connects to ACH Colombia, GoAnywhere, S3/Integra, or any production endpoint. It uses a local PostgreSQL database, two local ACHInterbank API instances sharing a persistent Quartz store, a local OpenSSH SFTP server representing the **CFA MFT Boundary**, and a local actor that simulates the external MFT/GoAnywhere side. Credentials are generated per execution and are never written to certification evidence. No real certificate, account, token, or production connection string is committed.

The harness does not activate `ACHCOL-7C-CANDIDATE`, does not set its `EffectiveFrom`, and does not resolve C4. Exact C1/C7 timing semantics and C2-C6 no-hardcode behavior are certified by deterministic tests against the current implementation. The runtime compose smoke only uses already-active regulatory cycle data and deactivates its temporary operational schedule after verifying persist-then-reschedule behavior.

## Controlled topology

```text
                    PostgreSQL + QRTZ_ persistent store
                              /           \
                             /             \
                    API node 01           API node 02
                         \                    /
                          \--- Quartz cluster-/
                               |
                         ACH Colombia MFT
                               |
                          SFTP CFA local
                          /input /output
                               |
                    simulated GoAnywhere actor
```

Files:

- `docker-compose.achcol-phase8.yml` — persistent DB, two API nodes, Quartz cluster, existing SPA, controlled SFTP and actor.
- `docker/achcol-phase8-sftp/` — local-only SFTP boundary container.
- `scripts/ci/achcol-phase8/run-controlled-e2e.sh` — orchestrator.
- `scripts/ci/achcol-phase8/bootstrap-runtime.sh` — ephemeral administrator and SFTP profile configuration.
- `scripts/ci/achcol-phase8/reschedule-smoke.sh` — persist-first/reschedule-without-restart smoke on an already-active cycle; it never activates the 7-cycle candidate.
- `scripts/ci/achcol-phase8/goanywhere-simulator.sh` — local SFTP actor with strict known-host checking.
- `scripts/ci/achcol-phase8/collect-evidence.sh` — sanitized evidence collector.
- `scripts/ci/achcol-phase8/fixtures/` — existing repository golden payloads copied as non-production fixture sources.
- `docs/certification/ach-colombia-phase8-scenario-matrix.csv` — required scenario matrix and exact test evidence mapping.

## Execution gate

On a host with .NET 10, Docker/Compose, curl, jq and OpenSSL:

```bash
scripts/ci/achcol-phase8/run-controlled-e2e.sh
```

The runner executes, in order:

1. `python scripts/validate-solution-projects.py`;
2. `dotnet --info`;
3. `dotnet build ACHInterbank.sln -c Release`;
4. focal ACH Colombia operational schedule / Quartz / MFT / SFTP / inbound tests;
5. `docker compose -f docker-compose.achcol-phase8.yml config --quiet`;
6. local persistent cluster startup;
7. creation of a disposable administrator in the local DB;
8. secure SFTP profile configuration using the generated local host-key fingerprint and an ephemeral password;
9. connection test;
10. operational schedule update/reschedule/deactivate smoke without restart;
11. negative and positive credential-rotation connection checks;
12. sanitized evidence collection.

A successful script execution is still not enough to claim external receipt or acceptance. The matrix must be reviewed and every required case must have executable evidence from the exact package/commit.

## Required scenarios

The authoritative matrix is `ach-colombia-phase8-scenario-matrix.csv`. It covers the requested 18 scenarios plus the operator-visible state contract. Timing-sensitive C1/C7 behavior is exercised deterministically so the certification does not depend on the wall clock of the CI host. Cluster topology, persistent Quartz state, SFTP authentication and credential rotation are exercised by the controlled runtime harness.

### C1 and C7

- C1 `06:30` remains an **operational** trigger value and does not replace regulatory cycle values.
- C7 `17:50` is the operational latest-start boundary; `17:51` must fail closed for automatic generation.
- C2-C6 receive no times from the harness or product code unless explicitly persisted as operational configuration.

### No duplication

Two protections are intentionally verified separately:

1. Quartz persistent-cluster acquisition/non-concurrency prevents concurrent scheduler execution where applicable.
2. Persisted ACH Colombia file-transfer idempotency remains the functional exactly-once-effect gate across node, retry, restart and replay cases.

Quartz locking is not treated as a substitute for persisted idempotency.

## SFTP / simulated GoAnywhere

The SFTP service is a local CFA boundary. The actor does not represent ACH Colombia infrastructure and does not prove GoAnywhere compatibility or homologation. It can:

```bash
# Collect all final outbound .env files from /input via SFTP.
docker compose -f docker-compose.achcol-phase8.yml exec -T phase8-goanywhere \
  /phase8/goanywhere-simulator.sh pickup-all

# Deliver a controlled .OUT.env or .RET.env fixture to /output via temp upload + rename.
docker compose -f docker-compose.achcol-phase8.yml exec -T phase8-goanywhere \
  /phase8/goanywhere-simulator.sh deliver /fixtures/<fixture>.OUT.env <fixture>.OUT.env
```

The repository intentionally does not commit a private test certificate or pre-encrypted envelope. If a runtime integration environment supplies ephemeral authorized test certificates, its generated `.env` fixtures may be mounted under `artifacts/achcol-phase8-fixtures/`; those generated files are execution artifacts and are not part of source certification. The deterministic backend suite remains the source-controlled verification for OUT/RET processing and digital-envelope behavior.

## Evidence contract

`artifacts/achcol-phase8-evidence/` is generated only by execution and is excluded from source certification packaging unless explicitly requested later. The collector records, without secrets:

- both scheduler instance IDs/names and heartbeats;
- Quartz trigger identity/state;
- execution IDs, scheduler instance IDs, scheduled/actual timestamps, recovery/misfire flags and correlation IDs;
- transfer IDs, file names, hashes, sizes, operational dates, cycles, attempts and lifecycle states;
- transfer event chronology;
- sanitized MFT administration metadata and connection-test result;
- simulated MFT actor pickup/delivery log with file name and SHA-256 only.

Do not copy bearer tokens, passwords, PFX material, private keys, raw authorization headers or production-like connection strings into `docs/certification/`.

## Operator-visible state assertions

The controlled certification must preserve these user-facing semantics:

- `Transferred` → **Publicado para envío**;
- uncertain → **Pendiente de confirmación**;
- processed → **Procesado**;
- duplicate → **Duplicado**;
- retry pending → **Reintento pendiente**;
- failed inbound processing → **No se pudo procesar**;
- late inbound → **Recibido fuera del horario esperado**.

No state or event may claim **recibido/aceptado por ACH Colombia** without an external observability contract.

## Current host result

For this Phase 8 preparation host:

- package SHA-256 was verified before modification;
- solution graph validation passes;
- `dotnet` is unavailable;
- Docker is unavailable;
- therefore build, focal tests and the controlled runtime scenario were **not executed**.

The only valid status from this host is `IMPLEMENTED_UNVERIFIED / FINAL_EXECUTABLE_GATE_PENDING`. Do not convert this document to `E2E PASS` without executing the harness on the exact resulting package.

## Phase 9 closure note

FASE 9 reviewed this harness without executing it. Its status remains `PREPARED_NOT_EXECUTED`; the project-level state is **IMPLEMENTACIÓN COMPLETADA / VALIDACIÓN EJECUTABLE FINAL PENDIENTE**. The next internal gate is to run this exact harness on a capable host and attach runtime evidence before any UAT/homologation/release claim.
