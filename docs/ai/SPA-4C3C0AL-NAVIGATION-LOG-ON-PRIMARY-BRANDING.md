# SPA 4C.3C0al — Navigation Log On-Primary Branding

## Estado

`4C3C0AL_NAVIGATION_LOG_ON_PRIMARY_BRANDING_IMPLEMENTED_VALIDATION_PENDING`

No se declara `VERIFIED`: Angular CLI no está disponible en el baseline y, por instrucción del micro-lote, no se ejecutaron instalación, build ni tests.

## Baseline

- ZIP: `ACHInterbank-v111-spa-4C3C0ak-third-parties-primary-accent-branding.zip`
- SHA-256 esperado: `eae0e7e00326c90ceca8ea4b8669b6e9cc736b4040dec8333428c5b94c957dda`
- SHA-256 observado: `eae0e7e00326c90ceca8ea4b8669b6e9cc736b4040dec8333428c5b94c957dda`

## Precondiciones confirmadas

En `web/ach-interbank-ui/src/styles/_tokens.scss`:

```scss
--brand-on-primary: #ffffff;
--color-on-primary: var(--brand-on-primary);
```

En `BrandingService`, `buttonColor` se normaliza como color primario, se calcula su contraste y ese contraste se publica en runtime mediante `RUNTIME_TOKENS.onPrimary`, cuyo nombre CSS es `--brand-on-primary`.

`BrandingService` permaneció byte-a-byte intacto durante este lote.

## Cambio implementado

Archivo único de source modificado:

`web/ach-interbank-ui/src/app/features/navigation-logs/components/navigation-log.component.css`

Dentro de `button.primary`:

```diff
 button.primary {
   background: var(--color-primary);
   border: none;
-  color: var(--color-surface);
+  color: var(--color-on-primary);
   padding: 0.5rem 1rem;
   border-radius: var(--radius-md);
   cursor: pointer;
 }
```

Se realizó exactamente una sustitución. La transformación binaria esperada del baseline coincide exactamente con el archivo resultante; el resto del CSS quedó byte-a-byte intacto. Se preservaron 61 finales de línea CRLF.

## Validación estática

- reemplazos exactos: `1`
- `color: var(--color-surface);` posterior en el CSS objetivo: `0`
- `color: var(--color-on-primary);` posterior: `1`
- `background: var(--color-primary);`: intacto (`1 -> 1`)
- `button.ghost`: bloque byte-a-byte intacto
- feature -> feature: `0`
- core -> features: `0`
- shared -> features: `0`
- backend `src/` diff: `0`
- routing: `29` archivos revisados, `0` modificados
- package/config protegido (`package*.json`, `angular.json`, `tsconfig*.json`): `6` archivos, `0` modificados

`/users/branding` permanece intacta: `app-routing.module.ts` conserva `path: 'users'` y `admin-routing.module.ts` conserva `path: 'branding'`. Ambos archivos tienen el mismo SHA-256 que en el baseline.

`api/users/branding` permanece intacto: las llamadas GET/PUT continúan en `BrandingService`, cuyo SHA-256 coincide con el baseline.

Antes de agregar este reporte, el diff de `web/ach-interbank-ui/src` contra el baseline contenía únicamente `navigation-log.component.css`.

## Angular

Se comprobó únicamente:

`web/ach-interbank-ui/node_modules/.bin/ng`

Resultado exacto:

```text
ng: not found
```

No se ejecutó `npm ping`, `npm ci`, retries de registry, build ni tests.

## Alcance

No se modificaron `button.primary` background/border/padding/radius, `button.ghost`, otros selectores/tokens/colores, HTML/TS/specs, otros estilos, backend, DTO/endpoints/pantalla de branding, rutas, permisos ni configuración.
