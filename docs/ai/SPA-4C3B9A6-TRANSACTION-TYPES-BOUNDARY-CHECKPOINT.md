# SPA 4C.3B9a6 — Transaction Types Boundary Checkpoint

## Estado

`TRANSACTION_TYPES_BOUNDARY_4C3B9A6_IMPLEMENTED_VALIDATION_PENDING`

## Alcance ejecutado

Baseline: `ACHInterbank-v59-spa-4C3B9a5-confirmation-dialog-boundary.zip`.

Se movieron a ownership `core` los enums transversales `TransactionTypeEnum` y `AccountTypeEnum`, retirando su ownership anterior en `features/transactions`.

No se modificó backend, no se cambió lógica funcional y no se avanzó a otro boundary B9.

## Ownership y valores preservados

Archivo canónico nuevo:

- `web/ach-interbank-ui/src/app/core/models/transaction-types.model.ts`

El archivo es byte-a-byte idéntico al anterior `features/transactions/transactions.types.ts`; solo cambió su ubicación.

Valores preservados exactamente:

```text
TransactionTypeEnum
Credit = 1
Debit = 2
Prenotification = 3
Reversal = 4
Return = 5

AccountTypeEnum
Checking = 1
Savings = 2
ElectronicDeposits = 3
```

Después del movimiento existe exactamente una definición de `TransactionTypeEnum` y una definición de `AccountTypeEnum`, ambas en `core`.

El archivo anterior `web/ach-interbank-ui/src/app/features/transactions/transactions.types.ts` fue eliminado; no quedó reexport desde `transactions`.

## Consumidores actualizados

Se actualizaron únicamente imports en los consumidores reales.

Transactions:

- `features/transactions/transactions.models.ts`
- `features/transactions/transaction-policy.validators.ts`
- `features/transactions/services/transactions-api.service.ts`
- `features/transactions/components/transaction-create/transaction-create.component.ts`
- `features/transactions/components/transaction-create/transaction-create.component.spec.ts`
- `features/transactions/components/transaction-list/transaction-list.component.ts`
- `features/transactions/components/transaction-list/transaction-list.component.spec.ts`

Clearing houses / transaction policies:

- `features/clearing-houses/transaction-policies.component.ts`
- `features/clearing-houses/transaction-policies.component.spec.ts`
- `features/clearing-houses/transaction-policies.models.ts`
- `features/clearing-houses/transaction-policies.service.ts`
- `features/clearing-houses/transaction-policies.service.spec.ts`

Operational restrictions:

- `features/operational-restrictions/operational-restrictions.component.ts`
- `features/operational-restrictions/operational-restrictions.component.spec.ts`
- `features/operational-restrictions/operational-restrictions.models.ts`
- `features/operational-restrictions/operational-restrictions.service.spec.ts`

Una comparación contra el ZIP baseline, eliminando únicamente las líneas `import`, confirmó que los cuerpos no-import de los 16 consumidores modificados son idénticos.

No se cambió la lógica de transaction policies, operational restrictions, Transaction Create/List, validators, modelos ni `TransactionsApiService`.

## Validación estática

PASS:

- 1 definición de `TransactionTypeEnum`;
- 1 definición de `AccountTypeEnum`;
- valores numéricos preservados byte-a-byte desde el archivo anterior;
- 0 referencias a `transactions.types` en `web/ach-interbank-ui/src/app`;
- 16 imports del nuevo `core/models/transaction-types.model`, todos resolviendo al archivo canónico;
- 0 imports `core -> features`;
- 0 nuevos imports `feature -> feature`;
- se eliminaron 9 edges `feature -> transactions` existentes: 5 desde clearing-houses y 4 desde operational-restrictions;
- cuerpos no-import de los 16 consumidores: idénticos al baseline;
- backend: 1474 archivos bajo `src/`, 0 cambios;
- artefactos canónicos B9a1–a5 no relacionados con este import quedaron intactos, incluyendo financial institutions core, shared clearing-house context navigation, clearing-house API/service/model core y shared operational confirmation dialog.

Antes de agregar este checkpoint, el diff contra el baseline era exactamente:

- 1 archivo core agregado;
- 16 consumidores modificados solo en imports;
- 1 archivo `transactions.types.ts` eliminado;
- 0 otros cambios.

TypeScript global 5.8.3, `transpileModule` sobre los 17 TS/spec nuevos o modificados: 0 errores sintácticos.

## Validación ejecutable real

Entorno:

- Node `v22.16.0`
- npm `10.9.2`
- TypeScript global `5.8.3`
- `node_modules/.bin/ng`: ausente

Se realizó una sola comprobación corta del registry:

- `timeout 12s npm ping` -> exit 124, esperando `https://registry.npmjs.org/`.

Tests afectados intentados en una sola ejecución para:

- `transaction-create.component.spec.ts`
- `transaction-list.component.spec.ts`
- `transaction-policies.component.spec.ts`
- `transaction-policies.service.spec.ts`
- `operational-restrictions.component.spec.ts`
- `operational-restrictions.service.spec.ts`

Resultado:

- exit 127, `ng: not found`.

Build:

- `npm run build` -> exit 127, `ng: not found`.

No se ejecutó ni repitió `npm ci`. No se ejecutó backend.

## Archivos funcionales agregados/eliminados/modificados

Agregado:

- `web/ach-interbank-ui/src/app/core/models/transaction-types.model.ts`

Eliminado:

- `web/ach-interbank-ui/src/app/features/transactions/transactions.types.ts`

Imports modificados:

- `web/ach-interbank-ui/src/app/features/transactions/transactions.models.ts`
- `web/ach-interbank-ui/src/app/features/transactions/transaction-policy.validators.ts`
- `web/ach-interbank-ui/src/app/features/transactions/services/transactions-api.service.ts`
- `web/ach-interbank-ui/src/app/features/transactions/components/transaction-create/transaction-create.component.ts`
- `web/ach-interbank-ui/src/app/features/transactions/components/transaction-create/transaction-create.component.spec.ts`
- `web/ach-interbank-ui/src/app/features/transactions/components/transaction-list/transaction-list.component.ts`
- `web/ach-interbank-ui/src/app/features/transactions/components/transaction-list/transaction-list.component.spec.ts`
- `web/ach-interbank-ui/src/app/features/clearing-houses/transaction-policies.component.ts`
- `web/ach-interbank-ui/src/app/features/clearing-houses/transaction-policies.component.spec.ts`
- `web/ach-interbank-ui/src/app/features/clearing-houses/transaction-policies.models.ts`
- `web/ach-interbank-ui/src/app/features/clearing-houses/transaction-policies.service.ts`
- `web/ach-interbank-ui/src/app/features/clearing-houses/transaction-policies.service.spec.ts`
- `web/ach-interbank-ui/src/app/features/operational-restrictions/operational-restrictions.component.ts`
- `web/ach-interbank-ui/src/app/features/operational-restrictions/operational-restrictions.component.spec.ts`
- `web/ach-interbank-ui/src/app/features/operational-restrictions/operational-restrictions.models.ts`
- `web/ach-interbank-ui/src/app/features/operational-restrictions/operational-restrictions.service.spec.ts`
