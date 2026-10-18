# Phase prompts — TPA-GOV-001

## Global guardrail for every phase

`ThirdParty` and `ThirdPartyAccount` are generic master data. Never add camera/nature fields to their identity or onboarding request. ACH Colombia/CENIT context belongs to eligibility/validation only.

## TPA0

Lee `AGENTS.md`, TPA entries in `MEMORY.md`, architecture, plan, onboarding contract, normative matrix, tests and state. Ejecuta TPA0 únicamente. Characteriza legacy, single/bulk onboarding, automatic prenotification resolution and the four chamber/nature policy combinations. Explicitly identify any existing chamber coupling in onboarding as a gap; do not fix it yet. No migrations, no behavior changes, no UI changes. Produce `third-party-account-tpa0-baseline.md` and update state/memory only with durable evidence.

## TPA1

Implementa sólo el esquema aditivo: `ThirdParty`, `ThirdPartyAccount`, `ThirdPartyAccountValidation`, `PrenotificationValidationAttempt`. Master tables must not contain `ClearingHouseId`/nature. Add dual-provider migrations/domain tests. Legacy remains read authority. Do not centralize transaction onboarding yet.

## TPA2

Implementa sólo `IThirdPartyOnboardingService` generic master onboarding and dual-write from single/bulk. It must accept master data only, no chamber/nature/service/policy. No external I/O. Prove same canonical account is reused across different chamber contexts in tests. Do not implement policy lifecycle beyond compatibility required by existing behavior.

## TPA3

Implementa only contextual eligibility/validation: effective policy resolution, `ThirdPartyAccountValidation`, prenotification attempts, deadlines/tacit acceptance, re-prenotification. Camera/nature/service are inputs here. Do not modify master identity.

## TPA4

Implement canonical read model/shadow compare. Optional operational-state projection is derived/rebuildable and not master authority.

## TPA5

Implement master CRUD without chamber fields, plus separate request/retry validation commands with context. No manual interbank approve/reject.

## TPA6

Implement customer-centric UX: generic account master separated from operational validation cards/history. Manual account wizard does not ask camera.

## TPA7

Integrate transaction selector with contextual `EligibilityDecision`; same account can be reused across chambers. Preserve manual-data path and silent onboarding.

## TPA8

Backfill/cutover. Never infer validation context when legacy evidence is ambiguous. Master backfill can proceed independently from validation-history backfill.

## TPA9

Hardening security/resilience/performance/observability/concurrency without blind retry of non-idempotent work.

## TPA10

Recertify exact artifact and decide legacy-write retirement. No physical deletion without explicit follow-up plan.
