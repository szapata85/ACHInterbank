# SPA — Modernización UX/UI de NACHA Upload — 2026-11-05

**Ruta:** `/transactions/nacha-upload`  
**Estado:** `SOURCE_IMPLEMENTED_STATIC_CHECKS_PASS_ANGULAR_RUNTIME_PENDING`  
**Origen:** `ACHInterbank_COMPLETO_LIVIANO_QUARTZ_4_7_1_CORREGIDO.zip`, entregado el 5 de noviembre de 2026.

## Diagnóstico de la base

El componente anterior utilizaba Angular Material solo en botones y filtros. La cámara seguía siendo un selector HTML nativo a través de `app-clearing-house-select`; el archivo se escogía desde un `input[type=file]` sin zona visual de selección, y los resultados/cargas estaban compuestos con tarjetas propias. La documentación histórica `SPA-4C3B3A-NACHA-UPLOAD-CHECKPOINT.md` declaraba una migración parcial estática con validación Angular pendiente.

## Cambios concretos

- Reemplazo de secciones visuales por `MatCard`, selector de cámara con `MatSelect`, botones `MatButton`, iconografía `MatIcon`, progreso `MatProgressBar` y ayuda contextual `MatExpansion`.
- Selector de archivo accesible con botón de apertura, zona para soltar archivos, clasificación vigente, estado de rechazo visible, tamaño y acción para retirar/reemplazar el archivo.
- Deshabilitación visual de `Cargar archivo` hasta que estén completos ambos campos y el nombre del archivo haya pasado la validación existente.
- Presentación legible de los campos de ingesta, estados, mensajes, trazabilidad, totales, errores controlados y reproceso explícito.
- Reintento visual al fallar la consulta de cámaras o registros; tablas y filtros originales preservados.
- Diseño adaptable y sin valores de color corporativo hardcodeados. Se reutilizan `--color-primary`, `--color-border`, `--color-focus`, `--color-surface`, y el puente de branding Angular Material `--mat-sys-primary`/`--mat-sys-on-primary` vigente.
- Nuevas pruebas unitarias focales para catálogo Material, arrastrar/soltar, archivo rechazado, estados de error y reintento; test anterior de estructura actualizado.

## Invariantes funcionales conservados

- Rutas y permisos: `/transactions/nacha-upload`; `Nacha.Upload` o `CanManageAch`, sin cambios.
- Endpoints: `NachaUpload/upload` y `NachaUpload/records` sin cambios.
- Se conserva `NachaUploadService` **sin edición**; el payload y los filtros siguen siendo idénticos.
- El bloque `classifyNachaUploadFile()` y las reglas de `submitUpload()`, `toUploadResult()` no cambian, al normalizar los finales de línea.
- No existe reproceso automático; requiere resultado elegible y acción del operador.
- Sin modificaciones a backend, reglas ACH/CENIT, archivo NACHA-M, ciclos, liquidación o envío SOAP.

## Evidencia obtenida

- `python scripts/validate-nacha-upload-ux-static.py`: **11/11 controles PASS**.
- Parseo sintáctico independiente de TypeScript para `nacha-upload.component.ts` y `nacha-upload.component.spec.ts`: **PASS** (global TypeScript, sin carga de módulos Angular).
- Ejecución aislada de clasificación NACHA con nueve casos (ACHCOL, CENIT, RET, ENV, fixture, extensiones rechazadas): **9/9 PASS**.
- Comprobaciones auxiliares de UI, tokens y enlaces de servicio: **26/26 PASS** incluyendo parseo y clasificación anteriores.
- La consulta `npm ping` falló por `getaddrinfo EAI_AGAIN registry.npmjs.org`; no existe `node_modules` en el SPA. **No se ejecutaron `npm ci`, `ng build`, Karma ni Playwright**. No se afirma validación de browser ni despliegue.

## Archivos del incremento

1. `web/ach-interbank-ui/src/app/features/transactions/components/nacha-upload/nacha-upload.component.html`
2. `web/ach-interbank-ui/src/app/features/transactions/components/nacha-upload/nacha-upload.component.scss`
3. `web/ach-interbank-ui/src/app/features/transactions/components/nacha-upload/nacha-upload.component.ts`
4. `web/ach-interbank-ui/src/app/features/transactions/components/nacha-upload/nacha-upload.component.spec.ts`
5. `scripts/validate-nacha-upload-ux-static.py`
6. Este documento y una entrada de estado no certificada en `MEMORY.md`.

## Validación pendiente en el entorno del proyecto

Con acceso a dependencias y Chrome/Chromium, ejecutar desde `web/ach-interbank-ui`:

```cmd
npm ci
npm run build
npm test -- --watch=false --browsers=ChromeHeadless --include=src/app/features/transactions/components/nacha-upload/nacha-upload.component.spec.ts
```

Posteriormente efectuar UAT visual de la ruta a resoluciones de escritorio y móvil, con distintas configuraciones de `buttonColor`, incluido un primario claro, y verificar operativamente carga, rechazo controlado, filtros y reproceso elegible **solo con fixtures locales autorizados**. Un `npm run build` exitoso no sustituye esta revisión visual.

**No hay evidencia de que el entorno SPA desplegado actualmente haya sido reconstruido/publicado con este ZIP.**
