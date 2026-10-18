# TPA6 — Canonical third-party administration UX

**Program:** TPA-GOV-001  
**Phase:** TPA6 only  
**Date:** 2026-10-11  
**Scope:** customer-centric Angular administration UX over the TPA5 canonical APIs. No transaction-selector cutover, no global read cutover, no legacy removal, no backfill and no TPA7 work.

## 1. Phase boundary and invariants

The UX keeps the canonical master independent from clearing-house context:

```text
Customer
  -> ThirdParty
      -> ThirdPartyAccount
          -> Validation history / revalidation request
```

Creating or editing `ThirdParty` and `ThirdPartyAccount` never asks for ACH Colombia, CENIT, transaction nature, cycle, SLA or prenotification outcome. Clearing house and nature appear only in the separate revalidation form and in validation-history reads.

The UI never treats administrative `Active` as interbank eligibility. It explicitly labels master status as **administrative** and states that it does not mean “habilitada para transferir”. The new screens do not calculate policy, maturity, SLA, cycles or eligibility locally; those decisions remain backend-owned.

## 2. Navigation and customer-centric composition

TPA6 adds two guarded routes under the existing Customer feature:

```text
/customers/{customerId}/third-parties
/customers/{customerId}/third-parties/{thirdPartyId}
```

Both routes require existing `CanReadAch`. The Customers grid adds a `Terceros` action that navigates to the first route. This keeps the flow customer-scoped and avoids creating a second global master console.

The pre-existing `/customer-third-parties` screen is untouched and remains the legacy operational “Terceros y prenotificaciones” console.

## 3. Third-party list and create UX

`CustomerThirdPartiesAdminComponent` uses only the nested TPA5 canonical administration surface for third-party reads and writes:

- paged list/search by Customer;
- canonical detail navigation;
- explicit third-party create.

Create fields are limited to the TPA5 master command contract:

- person type;
- document type;
- document number;
- administrative display name.

There is no clearing-house or transaction-nature field in this form. After creation the raw identity input is cleared and navigation continues using the canonical ID returned by the backend.

The list renders only the backend-provided masked document value. It supports loading, empty, retryable error and conflict presentation. Write controls are hidden unless the current user has `CanManageAch`.

## 4. Third-party detail and administrative commands

`ThirdPartyAdminDetailComponent` loads:

- the TPA5 canonical ThirdParty detail;
- canonical accounts through the nested TPA5 accounts endpoint;
- existing reference catalogs needed to present account-create and revalidation choices.

Allowed ThirdParty editing is limited to `DisplayName`, matching TPA5. Document type, document number and person type are not editable.

Administrative activate/deactivate uses the dedicated TPA5 status command. The confirmation copy explicitly states that activation does not certify interbank eligibility and that deactivation does not rewrite historical validation outcomes.

## 5. Account administration UX

Account creation asks only for:

- financial institution;
- account type;
- account number.

No chamber/nature input is present.

Bank identity is not editable after creation. The page explains the TPA5 replacement strategy: create a new canonical account and deactivate the previous account when institution/type/number must change. The permitted generic account edit exposes administrative status only, while a dedicated activate/deactivate action uses the TPA5 status endpoint.

Account/document reads are rendered from masked TPA5 DTO values. `Active` is always labeled `Activo administrativo`.

## 6. Validation history presentation

Selecting an account loads the nested TPA5 validation-history endpoint. The UX shows backend facts only:

- clearing house;
- transaction nature;
- service type;
- validation status;
- effective date / expiry when present;
- attempt number;
- sent / response timestamps;
- return code as causal/result when present;
- source cycle identifier;
- ACH transaction ID and canonical attempt ID for safe traceability.

Presentation labels map backend statuses into explicit Spanish operational wording:

- pending send/response -> `Pendiente`;
- accepted -> `Aceptada / certificada`;
- tacit acceptance -> `Aceptación tácita`;
- rejected -> `Rechazada`;
- requires correction -> `Requiere corrección / revalidación`;
- technical review -> `Revisión técnica`.

This mapping is presentation-only; no policy or eligibility rule is reimplemented in Angular.

## 7. Revalidation / prenotification request UX

Revalidation is a separate action available only to `CanManageAch`. It requests the exact TPA5 command context:

- clearing house;
- transaction nature;
- effective date;
- company-entry description/service;
- source Customer account;
- generated idempotency key.

The service type is taken from the selected backend company-entry-description catalog row (`standardEntryClassCode`) and is not derived from a frontend normative table. The backend remains responsible for policy resolution, actual route verification, effective-date verification, request applicability and automatic result resolution.

The browser generates one idempotency key when the revalidation form opens and reuses it for the submission until success; a successful request rotates the key. The UI does not expose it.

There are no `Approve`, `Reject`, `Aprobar` or `Rechazar` prenotification commands/buttons. The UX states that results are resolved automatically.

## 8. Reference-data use without policy duplication

The canonical master/read/write operations use the TPA5 routes exclusively. Existing read-only APIs are reused only for display/input reference data required by the TPA5 commands:

- Customer-owned source accounts;
- financial institutions;
- operational clearing-house options;
- company-entry-description options.

TPA6 does not call transaction policy preview, eligibility evaluation, cycle calculation or SLA calculation from Angular.

## 9. Authorization and error states

Routes use `permissionGuard` with `CanReadAch`. Mutating controls are shown only when `AuthService.hasPermission('CanManageAch')` is true; server-side TPA5 authorization remains authoritative.

The new screens handle:

- loading;
- empty results;
- retryable read errors;
- 403/forbidden responses;
- 409/conflict responses;
- disabled write controls during submission.

No legacy roles are introduced.

## 10. Privacy and masking

TPA6 renders the masked document/account fields returned by TPA5. Source Customer accounts required by the revalidation command are displayed masked; the `<select>` stores only an array index in the DOM and resolves the full source account value in memory at submit time. Raw create inputs are used only for their explicit command and are cleared after success.

No full third-party document number or destination account number is written to logs, notifications or trace UI.

## 11. Legacy compatibility and transaction selector

Unchanged in TPA6:

- `CustomerThirdParty` persistence and services;
- legacy `/customer-third-parties` operational UX;
- SINGLE/BULK silent onboarding;
- TPA4 shadow comparison;
- productive transaction selector using legacy `status=Active`;
- global read authority/cutover.

The new administration UX itself does **not** depend on legacy global `Active`; however `GAP-TPA-004` remains open because its transaction-selector portion is intentionally deferred to TPA7.

## 12. Tests added

Angular focal coverage was added for:

- TPA5 service routes and payloads;
- third-party list/detail navigation;
- third-party create/update/status behavior;
- account create/update/status behavior;
- master commands containing no chamber/nature fields;
- validation history presentation;
- revalidation context and idempotency key;
- masked document/account presentation, including masked source-account options;
- `CanManageAch` write-control visibility;
- conflict and read-error states;
- absence of manual approve/reject actions;
- no transaction-policy preview call from revalidation UX.

Runtime Angular execution is environment-dependent. If dependencies are absent, TPA6 remains implementable/inspectable and records `NOT_RUN_ENVIRONMENT` rather than changing infrastructure.

## 13. Gaps and next boundary

No existing gap is fully closed in TPA6.

- `GAP-TPA-004`: administration UX no longer depends on global legacy `Active`, but the transaction creation selector still does; remains open for TPA7.
- `GAP-TPA-013`: unchanged; generic transaction ingress document-type gap remains.
- `GAP-TPA-014`: unchanged; monetary paths still do not maintain `LastMonetaryUseAtUtc`.
- `GAP-TPA-015`: unchanged; no historical mass backfill.
- `GAP-TPA-016`: unchanged; no invented V32/V35 historical split date.

TPA6 does not execute TPA7.
