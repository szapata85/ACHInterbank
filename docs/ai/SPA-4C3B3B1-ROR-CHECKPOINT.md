# SPA 4C.3B3b1 — Return of Return (ROR) Checkpoint

## Baseline

Baseline obligatorio utilizado:

`ACHInterbank-v41-spa-4C3B3a-nacha-upload.zip`

Objetivo aplicado únicamente a `AchReturnOfReturnManagementComponent` (`/transactions/returns-ror`). No se avanzó a `AchReturnsManagementComponent`.

Estado final del lote:

`RETURN_OF_RETURN_4C3B3B1_IMPLEMENTED_VALIDATION_PENDING`

## Archivos modificados

Superficie funcional:

- `web/ach-interbank-ui/src/app/features/transactions/components/ach-return-of-return-management/ach-return-of-return-management.component.ts`
- `web/ach-interbank-ui/src/app/features/transactions/components/ach-return-of-return-management/ach-return-of-return-management.component.html`
- `web/ach-interbank-ui/src/app/features/transactions/components/ach-return-of-return-management/ach-return-of-return-management.component.scss`
- `web/ach-interbank-ui/src/app/features/transactions/components/ach-return-of-return-management/ach-return-of-return-management.component.spec.ts`

Evidencia:

- `docs/ai/SPA-4C3B3B1-ROR-CHECKPOINT.md`

La comparación completa de `features/transactions` contra v41, antes de agregar este checkpoint, mostró exactamente cuatro archivos funcionales modificados: TS/HTML/SCSS/spec de ROR.

## Lógica TypeScript preservada

El cuerpo completo de la clase `AchReturnOfReturnManagementComponent` es textual y funcionalmente idéntico al baseline v41.

El diff del `.ts` contiene únicamente:

- imports de `MatButtonModule`, `MatFormFieldModule` y `MatInputModule`;
- incorporación de esos módulos al array `imports` del componente standalone.

No se modificó ninguna condición, request, payload, validación, descarga, notificación, estado de carga ni interpretación de respuesta backend.

## APIs y source preservados

Se mantienen exactamente los métodos existentes:

- `evaluateReturnOfReturn(...)`;
- `generateReturnOfReturnAuditFile(...)`;
- `generateReturnOfReturnNachaFile(...)`.

El `source` continúa siendo exactamente:

`spa-angular-ror`

para evaluación, generación de auditoría y generación NACHA-M productiva.

`AchReturnsApiService` quedó byte-a-byte intacto.

## Elegibilidad preservada

La evaluación continúa requiriendo:

- `sourceReturnTransactionId` requerido y `> 0` mediante `Validators.required` + `Validators.min(1)`;
- `newReturnReasonCode` requerido;
- envío de `newReturnReasonCode` con `trim()`.

Se preservan:

- `eligibilityResult.isEligible`;
- `failures` devueltos por backend;
- notificación de éxito cuando es elegible;
- warning cuando no es elegible;
- error backend/fallback actual;
- ausencia de reglas de elegibilidad duplicadas en Angular.

La evaluación no dispara generación NACHA automáticamente.

## FlowIds preservados

`parseFlowIds()` permanece sin cambios:

1. lee el texto del formulario;
2. separa por coma;
3. aplica `Number(x.trim())`;
4. conserva únicamente enteros `> 0`;
5. descarta valores inválidos.

No se cambió el payload generado.

## Generación de auditoría preservada

La generación de auditoría continúa:

- requiriendo al menos un `flowId` válido;
- bloqueándose si ya existe generación audit/NACHA en curso;
- llamando `generateReturnOfReturnAuditFile({ flowIds, source: 'spa-angular-ror' })`;
- descargando `ROR_AUDIT_<timestamp>.ach`;
- sin requerir evaluación previa;
- sin retry automático.

## Generación NACHA-M productiva preservada

La generación productiva continúa requiriendo:

1. al menos un `flowId` válido;
2. `eligibilityResult` existente;
3. `eligibilityResult.isEligible === true`;
4. confirmación explícita mediante `window.confirm(...)`.

Se conserva la advertencia de que el backend revalidará nuevamente la elegibilidad.

La llamada sigue siendo:

`generateReturnOfReturnNachaFile({ flowIds, source: 'spa-angular-ror' })`

La descarga continúa usando:

`RORNACHA_<timestamp>.ach`

No se agregó generación automática ni retry automático.

## Errores funcionales preservados

`generationFailures` conserva exactamente los campos backend:

- `code`;
- `message`;
- `field`.

`notifyFunctionalError(...)` permanece sin cambios: si existen `failures`, conserva todos y notifica el primer mensaje; si no existen, usa `error.message`/fallback actual.

No se oculta detalle funcional backend.

## Migración foundation 4C.1

Se conserva:

- `app-page-header`;
- `ui-tarjeta` como contenedor existente.

Se retiraron los cuatro `<ui-boton>` de la página y se reemplazaron por:

- `mat-flat-button` para acciones primarias;
- `mat-stroked-button` para acciones secundarias.

Los inputs se migraron a:

- `MatFormFieldModule`;
- `MatInputModule`;
- `matInput`.

Se incorporó `UiEstadoCargaComponent` para reflejar visualmente:

- evaluación en curso;
- generación de archivo de auditoría;
- generación NACHA-M productiva.

No se agregó estado/error backend nuevo ni nueva llamada API.

### CSS

Los dos colores legacy locales de la advertencia fueron sustituidos por tokens foundation:

- `var(--color-warning-surface)`;
- `var(--color-warning-text)`;
- borde con `color-mix(...)` basado en `--color-warning` y `--color-border`.

Resultado estático: cero hex/rgb hardcoded en TS/HTML/SCSS funcionales de ROR.

## Pruebas focales actualizadas

Solo se actualizó `ach-return-of-return-management.component.spec.ts`.

Cubre:

- request exacto de `evaluateReturnOfReturn`, incluido `source: 'spa-angular-ror'`;
- trim de causal;
- validación de `sourceReturnTransactionId > 0`;
- evaluación elegible/no elegible y preservación de failures;
- parseo de flowIds válidos y descarte de inválidos;
- generación de auditoría y nombre `ROR_AUDIT_*.ach`;
- auditoría sin evaluación previa;
- bloqueo NACHA sin evaluación;
- bloqueo NACHA si `isEligible=false`;
- confirmación explícita requerida;
- generación NACHA elegible con request exacto y `RORNACHA_*.ach`;
- preservación de `code/message/field` en failures backend;
- fallback funcional de error;
- cero `<ui-boton>` y presencia de controles Material.

No se escribieron tests sobre capacidades inexistentes.

## Validación ejecutable real

Entorno observado:

- Node: `v22.16.0`;
- npm: `10.9.2`;
- `node_modules/.bin/ng`: ausente;
- TypeScript global: `5.8.3`.

Se realizó una única comprobación corta del registry:

- comando: `timeout 12s npm ping`;
- exit code: `124`;
- causa: `registry.npmjs.org` no respondió dentro del timeout;
- salida observable: `npm notice PING https://registry.npmjs.org/`.

Intentos reales:

1. `npm test -- --watch=false --browsers=ChromeHeadless --include=src/app/features/transactions/components/ach-return-of-return-management/ach-return-of-return-management.component.spec.ts`
   - exit code: `127`;
   - causa: `ng: not found`.
2. `npm run build`
   - exit code: `127`;
   - causa: `ng: not found`.

No se ejecutó backend.

Validación sintáctica complementaria con TypeScript global `5.8.3` y `transpileModule(..., reportDiagnostics=true)`:

- component `.ts`: 0 diagnostics sintácticos;
- spec `.ts`: 0 diagnostics sintácticos.

Esta comprobación no sustituye Angular test/build.

## Validación estática contra v41

PASS antes de agregar este checkpoint.

Se confirmó byte-a-byte sin cambios:

- `AchReturnsApiService`;
- `transactions.models.ts`;
- `transactions-routing.module.ts` y permiso `CanManageAch` de `returns-ror`;
- `AchReturnsManagementComponent` completo (4 archivos);
- backend `src/` completo: 1475 archivos.

Hashes preservados:

- `AchReturnsApiService`: `9ac18009018b726c3427894db564b7457230ea8332e863e97c6b5046e4a4449a`;
- routing: `b46e8b240ded22bc88af2bbc33280ed0845baa674e9a9eeb013f3a7bff687150`;
- models: `b6a7a4fc41f6d41834cfe65de4c648877ea831b1d0a7befe44975ffe20631b35`.

También se confirmó:

- ruta actual preservada: `/transactions/returns-ror`;
- permiso preservado: `CanManageAch`;
- cuerpo de clase ROR idéntico al baseline;
- cero `<ui-boton>` en ROR;
- cero hardcoded hex/rgb en TS/HTML/SCSS funcionales;
- cero nuevos imports `feature -> feature`;
- únicos archivos funcionales de `transactions` modificados: los cuatro archivos de ROR.

## Pendiente deliberado

Fuera de este lote permanecen sin modificación:

- `AchReturnsManagementComponent` / Returns;
- NACHA upload;
- bulk-ingestion;
- cycle-configs;
- clearing-houses;
- ach-cycles;
- boundaries cross-feature reservados para B9;
- validación Angular ejecutable cuando el entorno disponga nuevamente de dependencias/registry.

No se avanzó a Returns.
