# TPA1 — Canonical third-party master data

**Program:** TPA-GOV-001  
**Phase:** TPA1 only  
**Date:** 2026-10-11  
**Scope:** additive master data for `ThirdParty` and `ThirdPartyAccount`; no onboarding v2, dual-write, validation, eligibility, prenotification lifecycle, SPA or legacy cutover.

## 1. Implemented model

The canonical master remains deliberately clearing-house agnostic:

```text
Customer
  -> ThirdParty
      -> ThirdPartyAccount
```

`ThirdParty` contains:

- `Id` (`long`);
- `CustomerId`;
- `PersonType`;
- `DocumentType`;
- `DocumentNumberNormalized`;
- `DisplayName`;
- administrative `Status` (`Draft | Active | Inactive`);
- standard audit timestamps.

`ThirdPartyAccount` contains:

- `Id` (`long`);
- `ThirdPartyId`;
- `FinancialInstitutionId`;
- existing domain `AccountTypeEnum` (`Checking | Savings | ElectronicDeposits`);
- `AccountNumberNormalized`;
- administrative `Status`;
- optional `OriginSource` (`Manual | SingleTransaction | BulkTransaction | Backfill`);
- standard audit timestamps.

Neither entity contains clearing-house, transaction-nature, service-class, prenotification, cycle, return-code, deadline or SLA data.

## 2. Identity, normalization and concurrency defense

Canonical functional identity is enforced with database unique indexes:

```text
ThirdParty:
CustomerId + DocumentType + DocumentNumberNormalized

ThirdPartyAccount:
ThirdPartyId + FinancialInstitutionId + AccountType + AccountNumberNormalized
```

Normalization intentionally follows behavior already present in legacy matching rather than inventing a new broad canonicalization rule:

- catalog codes: `Trim + ToUpperInvariant`;
- third-party document number: `Trim + ToUpperInvariant`;
- account number: `Trim` only;
- display name: `Trim`.

The unique indexes are the database-level concurrency defense for equivalent canonical identities. TPA1 does not add the unique-conflict/re-read orchestration; that remains part of onboarding v2/TPA2.

## 3. Relationships and delete behavior

- `ThirdParty.CustomerId -> Customers.Id`: `Restrict`;
- `ThirdParty.DocumentType -> DocumentTypes.Code`: `Restrict`;
- `ThirdParty.PersonType -> PersonTypes.Code`: `Restrict`;
- `ThirdPartyAccount.ThirdPartyId -> ThirdParties.Id`: `Restrict`;
- `ThirdPartyAccount.FinancialInstitutionId -> FinancialInstitutions.Id`: `Restrict`.

Restrict semantics avoid deleting canonical master records implicitly through unrelated aggregate removal.

`Customer.CanonicalThirdParties` is additive. Existing `Customer.ThirdParties` continues to refer to legacy `CustomerThirdParty` and is unchanged in behavior.

## 4. EF Core and provider migrations

Added configurations:

- `ThirdPartyConfiguration`;
- `ThirdPartyAccountConfiguration`.

Added `AchDbContext` sets:

- `ThirdParties`;
- `ThirdPartyAccounts`.

Additive migrations:

- PostgreSQL: `20261011173000_AddThirdPartyMasterData`;
- SQL Server: `20261011173100_AddThirdPartyMasterData`.

Both migrations create only `ThirdParties` and `ThirdPartyAccounts`, equivalent logical FKs/indexes and no legacy-table mutation. Both provider model snapshots are updated.

## 5. Legacy compatibility

`CustomerThirdParty` remains present, mapped to `CustomerThirdParties`, and is not read from or written by the new model in TPA1. No transaction service, prenotification handler, controller, endpoint or SPA behavior is switched to canonical master data in this phase.

Therefore this phase is schema/domain additive and intentionally has no application behavioral cutover.

## 6. Validation subsystem boundary

Although the target architecture includes `ThirdPartyAccountValidation` and `PrenotificationValidationAttempt`, the explicit TPA1 execution scope is master data only. Those validation entities are not created in TPA1 and remain deferred to the validation lifecycle phase. This preserves the invariant that the mere existence of `ThirdParty`/`ThirdPartyAccount` never creates chamber-specific state.

## 7. Tests added

`ThirdPartyMasterDataModelTests` characterizes:

- normalization;
- administrative status defaults;
- canonical natural unique keys;
- `AccountTypeEnum` presence;
- absence of chamber/validation dimensions from the master;
- coexistence with legacy `CustomerThirdParty`.

`ThirdPartyMasterDataMigrationTests` verifies the PostgreSQL and SQL Server migration operation graphs have equivalent logical tables, columns, FKs and unique indexes, and that forbidden chamber/validation columns are absent.

## 8. Execution evidence

Executed:

```text
python scripts/validate-solution-projects.py
PASS — solution graph valid (7 solution projects / 7 disk projects)
```

Not executed because the host does not provide the required runtime:

```text
dotnet --info
NOT_RUN_ENVIRONMENT — dotnet: command not found

docker --version
NOT_RUN_ENVIRONMENT — docker: command not found
```

Per TPA1 instructions, these environment limitations defer runtime/provider execution evidence but do not block completion of the additive implementation.

## 9. Gap retained/identified

- `GAP-TPA-001` remains relevant for backfill because legacy `CustomerThirdParty` has no `AccountType`; TPA1 does not invent it.
- `GAP-TPA-009` remains for TPA2 because database unique indexes are present but application-level unique-conflict/re-read orchestration is not implemented yet.
- `GAP-TPA-012`: current customer/third-party account storage has no established reusable at-rest protection pattern for account numbers. TPA1 stores the canonical normalized account number using the same plain-data posture as current account models rather than introducing an unapproved crypto design. Protection/masking hardening remains explicit follow-up work.
