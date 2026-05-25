# ACH Phase 6C.3A - Legacy NACHA-M SPA Audit

Scope: `/ach-cycles/nacha/layouts`, `/ach-cycles/nacha/definitions`, `nacha-layouts`, `nacha-record-definitions`.

## Inventory And Classification

| Area | References | Classification | Action |
| --- | --- | --- | --- |
| Angular routes | `web/ach-interbank-ui/src/app/features/ach-cycles/ach-cycles-routing.module.ts` paths `nacha/layouts`, `nacha/definitions` | ActiveDiagnosticOnly | Kept accessible but marked `legacy` / `diagnosticOnly`; permission lowered to read-only. |
| Angular services | `NachaLayoutsService`, `NachaRecordDefinitionsService` | ActiveDiagnosticOnly | Marked `@deprecated`; official screens must not use them. |
| Angular components | `NachaLayoutsComponent`, `NachaRecordDefinitionsComponent` | ActiveDiagnosticOnly | Converted to diagnostic read-only with `LEGACY / Deprecated` banner and official profiles link. |
| Angular models | `nacha-layout.model.ts`, `nacha-record-definition.model.ts` | BackendCompatibilityOnly | Kept for read-only diagnostics and endpoint compatibility. |
| Angular tests | `nacha-layouts-definitions-phase4.component.spec.ts`, navigation/dashboard specs | TestOnly | Updated to assert legacy read-only/deprecated behavior. |
| Playwright | `nacha-legacy-routes.spec.ts`, `nacha-operational-dashboard.spec.ts` | TestOnly | Guards that navigation does not expose legacy official items and dashboard does not call legacy endpoints. |
| Backend controllers | `NachaRecordLayoutsController`, `NachaRecordDefinitionsController` | BackendCompatibilityOnly | GET kept as diagnostic/deprecated; POST/PUT/DELETE blocked with 410. |
| Backend services/models | `INachaRecordLayoutAppService`, `INachaRecordDefinitionAppService`, `NachaRecordLayout`, `NachaRecordDefinition` | BackendCompatibilityOnly | Kept for compatibility/backfill diagnostics. Official generation must use config profiles. |
| Official config | `NachaConfigProfilesController`, `CfgProfile`, `CfgLayoutVariant`, `CfgLayoutField` | ActiveOfficialUi | Official transition target for Fase 6C.4. |
| Documentation | `docs/ai/ACH_PHASE6_CONTEXT.md`, this file | ActiveDiagnosticOnly | Context updated to keep token footprint low for future phases. |

## Transition Rule

Nothing based on legacy layouts/definitions is official NACHA-M administration. Fase 6C.4 must continue with `nacha-config profiles`, published/effective profiles by ACH Colombia/CENIT, records 1/5/6/7/8/9, versioning, and states Draft / Published / Deprecated / Archived.

Productivo remains NO-GO.
