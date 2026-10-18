# SPA 4C.3B9a12 — Roles Boundary checkpoint

Baseline: `ACHInterbank-v65-spa-4C3B9a11-ach-cycles-scss-boundary.zip`

## Scope

This micro-batch removes only the `navigation -> admin` boundary caused by the transverse `RoleSummary` contract and `RolesApiService`.

No backend, routing, guards, permissions, business behavior, or other B9 boundary is changed.

## References inspected

Before the move, `RoleSummary` was owned by:

- `features/admin/models/user.model.ts`

It was consumed by admin user UI/presentation/API code and, outside admin, by:

- `features/navigation/components/navigation-menu.component.ts`

Before the move, `RolesApiService` was declared inside:

- `features/admin/services/users-api.service.ts`

Its product consumers were:

- `features/admin/components/user-form.component.ts`
- `features/admin/components/user-roles.component.ts`
- `features/admin/components/users-list.component.ts`
- `features/navigation/components/navigation-menu.component.ts`

Its direct test consumers were:

- `features/admin/components/users-list.component.spec.ts`
- `features/navigation/components/navigation-menu.component.spec.ts`

This confirms both symbols are transverse to `admin`; `navigation` was importing them directly from the admin feature.

## Ownership change

`RoleSummary` now has one canonical definition in:

- `core/models/role.model.ts`

`RolesApiService` now has one canonical definition in:

- `core/services/roles-api.service.ts`

The old admin files were not deleted because they still contain valid, unrelated contracts/services:

- `features/admin/models/user.model.ts` still owns `UserSummary`, `UserFilter`, and `SaveUserRequest` and now imports `RoleSummary` from core for `UserSummary.roles`;
- `features/admin/services/users-api.service.ts` still owns `UsersApiService`; only `RolesApiService` was extracted.

No wrapper or re-export was left under `admin`.

## Contracts preserved

Direct block comparison against the v65 baseline confirms the moved `RoleSummary` declaration is textually identical:

- `id: string`
- `name: string`
- `description?: string`
- `permissions?: string[]`

Direct class-block comparison against the v65 baseline confirms `RolesApiService` is textually identical:

- `@Injectable({ providedIn: 'root' })`
- `basePath = 'api/roles'`
- `getRoles(): Observable<RoleSummary[]>`
- `GET api/roles` through the existing `ApiService`

Only import paths changed to reflect core ownership.

## Consumer changes

Imports were updated only in:

- `features/admin/components/user-form.component.ts`
- `features/admin/components/user-roles.component.ts`
- `features/admin/components/users-list.component.ts`
- `features/admin/components/users-list.component.spec.ts`
- `features/admin/services/user-presentation.service.ts`
- `features/navigation/components/navigation-menu.component.ts`
- `features/navigation/components/navigation-menu.component.spec.ts`

For all seven files, comparison after removing import declarations is identical to the v65 baseline. No functional body changed.

## Static validation

Pre-checkpoint SHA-256 comparison against v65:

- baseline files: **3605**;
- modified baseline files: **9**;
- added files before checkpoint: **2**;
- removed files: **0**;
- backend `src/` changes: **0**.

The nine modified baseline files are exactly the two admin owner files plus the seven import-only consumers listed above. The two additions are the canonical core model/service.

Boundary checks:

- `RoleSummary` definitions: **1**, in core;
- `RolesApiService` definitions: **1**, in core;
- navigation imports of these symbols from admin: **0**;
- imports `core -> features`: **0**;
- new concrete feature-to-feature imports versus v65: **0**;
- feature dependency directions: **8 -> 7**;
- removed direction: `navigation -> admin`;
- removed concrete imports for this boundary: **3**;
- B9a1–B9a11 checkpoint files remain unchanged;
- no legacy boundary path was reintroduced;
- backend remains byte-for-byte unchanged.

## Executable validation

Per this micro-batch instruction, only Angular CLI availability was checked:

```text
ng: not found
```

`web/ach-interbank-ui/node_modules/.bin/ng` is absent.

Not executed by instruction:

- `npm ping`;
- `npm ci`;
- Angular tests;
- Angular build;
- backend build/tests.

## Final state

`ROLES_BOUNDARY_4C3B9A12_IMPLEMENTED_VALIDATION_PENDING`
