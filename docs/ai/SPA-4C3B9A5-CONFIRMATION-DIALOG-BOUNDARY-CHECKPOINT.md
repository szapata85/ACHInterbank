# SPA 4C.3B9a5 — Confirmation Dialog Boundary Checkpoint

## Estado

`CONFIRMATION_DIALOG_BOUNDARY_4C3B9A5_IMPLEMENTED_VALIDATION_PENDING`

## Alcance ejecutado

Baseline: `ACHInterbank-v58-spa-4C3B9a4-clearing-house-service-model-boundary.zip`.

Se retiró el `ConfirmationDialogComponent` genérico alojado en `features/clearing-houses` y sus consumidores fueron migrados al `OperationalConfirmDialogComponent` ya existente en `shared/components`.

No se creó ningún diálogo nuevo, no se modificó backend y no se avanzó a otro boundary B9.

## Consumidores actualizados

Se actualizaron únicamente los consumidores reales indicados por el inventario del código:

- `ClearingHousesComponent`
- `TransactionPoliciesComponent`
- `CycleConfigManagementComponent`
- `ClearingHouseSpecialDatesComponent`
- `OperationalRestrictionsComponent`

En todos ellos se preservaron `title`, `message`, `icon`, `destructive`, la confirmación explícita y los anchos de diálogo existentes. El único ajuste de contrato de datos fue `confirmText -> confirmLabel`.

Anchos preservados:

- clearing-houses admin: `min(92vw, 520px)`
- transaction policies: `min(92vw, 540px)`
- cycle config: `min(520px, calc(100vw - 2rem))`
- special dates: `min(520px, calc(100vw - 2rem))`
- operational restrictions: `min(520px, calc(100vw - 1.5rem))`

Una comparación normalizada contra el baseline confirmó que los cuerpos funcionales de los cinco consumidores son idénticos después de abstraer únicamente imports, nombres del componente/data del diálogo y `confirmText -> confirmLabel`.

## Duplicación retirada

De `features/clearing-houses/clearing-house-dialogs.component.ts` se retiraron únicamente:

- `ConfirmationDialogData`
- `ConfirmationDialogComponent`

Quedaron 0 referencias frontend a ambos símbolos.

`OperationalConfirmDialogComponent` en `shared/components/operational-confirm-dialog.component.ts` quedó byte-a-byte intacto respecto del baseline.

## CloseValidityDialog preservado

Se mantuvieron en `clearing-house-dialogs.component.ts`:

- `CloseValidityDialogData`
- `CloseValidityDialogComponent`
- validación de fecha requerida
- cierre explícito con la fecha seleccionada
- template, estilos y comportamiento existentes

La sección desde `CloseValidityDialogData` hasta EOF quedó byte-a-byte idéntica al baseline.

Después del cambio, `TransactionPoliciesComponent` es el único consumidor de `clearing-house-dialogs.component.ts`, exclusivamente para `CloseValidityDialogComponent`.

`transactions`, `catalogs` y `operational-restrictions` ya no importan `clearing-house-dialogs.component.ts`.

## Tests focales actualizados

Se actualizaron solamente los specs de los cinco consumidores afectados:

- `clearing-houses.component.spec.ts`
- `transaction-policies.component.spec.ts`
- `cycle-config-management.component.spec.ts`
- `clearing-house-special-dates.component.spec.ts`
- `operational-restrictions.component.spec.ts`

Cobertura preservada/ampliada para:

- confirmación explícita;
- cancelación sin ejecutar la operación;
- `destructive` en acciones destructivas;
- activate/inactivate;
- cierre de vigencia y versionado de transaction policies;
- uso del `OperationalConfirmDialogComponent` compartido y `confirmLabel`.

El spec propio de `OperationalConfirmDialogComponent` no fue modificado.

## Validación estática

PASS:

- 0 referencias a `ConfirmationDialogComponent`;
- 0 referencias a `ConfirmationDialogData`;
- `clearing-house-dialogs.component.ts` solo es importado por transaction policies para `CloseValidityDialogComponent`;
- `OperationalConfirmDialogComponent` shared: byte-a-byte intacto;
- `CloseValidityDialogData/Component`: sección byte-a-byte intacta;
- cuerpos funcionales de los cinco consumidores: preservados salvo adaptación de contrato del diálogo;
- 0 imports sin resolver en los TS/specs modificados;
- 0 nuevos imports `feature -> feature`;
- se eliminaron 3 edges `feature -> clearing-houses` existentes: transactions, catalogs y operational-restrictions;
- B9a1, B9a2, B9a3 y B9a4: artefactos canónicos verificados byte-a-byte intactos;
- backend: 1474 archivos bajo `src/`, 0 cambios.

TypeScript global 5.8.3, `transpileModule` sobre los 11 TS/spec funcionales modificados: 0 errores sintácticos.

## Validación ejecutable real

Entorno:

- Node `v22.16.0`
- npm `10.9.2`
- `node_modules/.bin/ng`: ausente

Se realizó una sola comprobación corta del registry:

- `timeout 12s npm ping` -> exit 124; quedó esperando `https://registry.npmjs.org/`.

Tests afectados intentados en una sola ejecución para los cinco specs modificados:

- resultado: exit 127, `ng: not found`.

Build:

- `npm run build` -> exit 127, `ng: not found`.

No se ejecutó ni repitió `npm ci`. No se ejecutó backend.

## Archivos funcionales modificados

Producción:

- `web/ach-interbank-ui/src/app/features/clearing-houses/clearing-houses.component.ts`
- `web/ach-interbank-ui/src/app/features/clearing-houses/transaction-policies.component.ts`
- `web/ach-interbank-ui/src/app/features/transactions/components/cycle-config-management/cycle-config-management.component.ts`
- `web/ach-interbank-ui/src/app/features/catalogs/components/clearing-house-special-dates.component.ts`
- `web/ach-interbank-ui/src/app/features/operational-restrictions/operational-restrictions.component.ts`
- `web/ach-interbank-ui/src/app/features/clearing-houses/clearing-house-dialogs.component.ts`

Tests:

- `web/ach-interbank-ui/src/app/features/clearing-houses/clearing-houses.component.spec.ts`
- `web/ach-interbank-ui/src/app/features/clearing-houses/transaction-policies.component.spec.ts`
- `web/ach-interbank-ui/src/app/features/transactions/components/cycle-config-management/cycle-config-management.component.spec.ts`
- `web/ach-interbank-ui/src/app/features/catalogs/components/clearing-house-special-dates.component.spec.ts`
- `web/ach-interbank-ui/src/app/features/operational-restrictions/operational-restrictions.component.spec.ts`
