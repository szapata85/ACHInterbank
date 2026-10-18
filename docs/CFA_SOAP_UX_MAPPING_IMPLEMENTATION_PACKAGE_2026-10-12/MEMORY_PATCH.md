# Patch sugerido para `MEMORY.md` — CFA-SOAP-UX-MAP-001

> Aplicar solo al iniciar formalmente P0 y después de revisar `git diff -- MEMORY.md` y EOL según `AGENTS.md`.

## CFA SOAP UX and contract-driven mapping program — 2026-10-12

- DEC-CFAUX-001: Program `CFA-SOAP-UX-MAP-001` is PLANNED. Scope is implementation-only: redesign the CFA integration SPA and evolve the three existing SOAP request mappings to contract-versioned, governed, extensible mapping without changing ACH/CENIT monetary, NACHA-M, cycle, cause, compensation or settlement semantics.
- DEC-CFAUX-002: Target methods are `WSCFAACH.Proc_Contrapartidas`, `WSCFAACH.Proc_Transacciones`, and `WSAXON.RegistrarRespuestaTransaccion`. Request construction will converge on a shared contract-driven resolver/writer while eligibility and response parsing remain method/domain specific.
- DEC-CFAUX-003: Add `IntegrationContractVersion`; version `IntegrationMethodParameter` by contract and link `IntegrationMappingSet` to the exact contract version. Published mappings and active/retired contracts remain immutable.
- DEC-CFAUX-004: A new CFA scalar input parameter whose source already exists must be addable through a new contract version + mapping configuration without modifying SOAP writer/client code. Complex structures, new response shapes, missing business data or new transport/authentication remain development changes.
- DEC-CFAUX-005: Source values use registered Strategy providers; transformations use registered deterministic strategies; arbitrary XML, reflection paths, user scripts and unrestricted expression execution are prohibited.
- DEC-CFAUX-006: SPA remains Angular Material 21 with dynamic branding, typed Reactive Forms and feature-level signal stores. User-facing language is Spanish-first and functional; SOAP names/codes are secondary advanced details.
- DEC-CFAUX-007: WSDL import, if implemented, is local-only and hardened: no remote URL fetch, no DTD/external entity resolution, size limit, SHA-256 evidence and human activation review.
- GAP-CFAUX-001: Current checkout/package has no Git metadata; exact baseline SHA must be captured in P0 from a real checkout.
- NEXT-CFAUX-001: `NEXT_ALLOWED_PHASE=P0`. Use `docs/CFA_SOAP_UX_MAPPING_IMPLEMENTATION_PACKAGE_2026-10-12/START_HERE.md` and `MEMORY_STATE.md`; execute one phase at a time and do not remove legacy SOAP mapping paths before three-service request parity is proven.
