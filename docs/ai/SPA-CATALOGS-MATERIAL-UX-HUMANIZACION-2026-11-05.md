# ACH Interbank — Modernización del módulo `/catalogs/*`

**Fecha:** 2026-11-05  
**Estado:** `SOURCE_IMPLEMENTED_STATIC_CHECKS_PASS_ANGULAR_RUNTIME_PENDING`  
**Base recibida:** `ACHInterbank_NACHA_UPLOAD_UX_MATERIAL_BRANDING_2026-11-05.zip` (incluye el cambio anterior de `/transactions/nacha-upload`).  
**Alcance:** solo frontend de catálogos; conserva rutas, autorizaciones, DTO y servicios existentes.

## Inventario de entradas de ruta y cobertura

| Ruta | Componente / comportamiento | Intervención |
|---|---|---|
| `/catalogs` | `CatalogsListComponent` | Índice con navegación Material en tarjetas, control de permiso de gestión, tabla con carga/error/reintento y diseño adaptable. |
| `/catalogs/financial-institutions` | `FinancialInstitutionsComponent` | Material existente conservado; textos descriptivos y encabezados de ruta/tránsito humanizados. |
| `/catalogs/clearing-house-preferences` | `ClearingHousePreferencesComponent` | Material existente conservado; textos y explicación de cámara predeterminada, campos y estado más claros. |
| `/catalogs/bank-holidays` | `BankHolidaysComponent` | Editor Material conservado; lenguaje unificado a «días no hábiles» y prueba E2E actualizada. |
| `/catalogs/clearing-house-special-dates` | Redirección heredada hacia contexto de cámara | Redirección existente preservada. El editor `ClearingHouseSpecialDatesComponent`, bajo el contexto de una cámara, conserva Material y tiene mensajes humanizados. No se inventó una pantalla independiente. |
| `/catalogs/company-entry-descriptions` | `CompanyEntryDescriptionAdminComponent` | Migración de todos los campos/selectores a Material; ayuda para PPD/CCD, validaciones, errores recuperables, confirmación Material y diseño adaptable. |
| `/catalogs/document-types` | `CatalogTypesAdminComponent` | Formulario Material compartido. |
| `/catalogs/gender-types` | `CatalogTypesAdminComponent` | Formulario Material compartido. |
| `/catalogs/person-types` | `CatalogTypesAdminComponent` | Formulario Material compartido. |
| `/catalogs/phone-types` | `CatalogTypesAdminComponent` | Formulario Material compartido. |
| `/catalogs/email-types` | `CatalogTypesAdminComponent` | Formulario Material compartido. |
| `/catalogs/address-types` | `CatalogTypesAdminComponent` | Formulario Material compartido. |
| `/catalogs/transaction-codes` | `CatalogTypesAdminComponent` | Formulario Material compartido. |

La migración de `CatalogTypesAdminComponent` aplica a **siete rutas** mediante la configuración de enrutamiento vigente, sin duplicar formularios. Los componentes heredados por cada tipo (`address-types-admin`, `gender-types-admin`, etc.) permanecen en el árbol de código pero **no están referenciados por el enrutamiento actual**; no se cambiaron ni se presentan como formularios activos.

## Decisiones UX/UI

- Formularios reactivos con `mat-form-field`, `matInput`, `mat-select`, `mat-error`, `mat-hint` y botones Material; se mantienen los nombres de campos y el payload original.
- Edición con código bloqueado en los catálogos de tipos, validaciones de campos visibles no vacíos, límites explicados y estado de guardado.
- Eliminaciones de tipos y conceptos mediante `CatalogActionConfirmDialogComponent` (Material), en lugar de `window.confirm`; solo se elimina tras confirmar.
- Mensajes de carga, vacíos, error, reintento y resultado en el contexto de la operación; errores técnicos/potencialmente sensibles filtrados mediante la utilidad `catalogErrorMessage` existente.
- Traducción de «Routing»/«Transit» a «Código de ruta»/«Código de tránsito» y explicación del significado de `SEC`/PPD/CCD. Los identificadores técnicos no se traducen ni se cambian en API, persistencia o NACHA.
- Se preserva el modelo de autorización: solo usuarios con `CanManageAch` ven accesos de administración desde el índice; la guardia original sigue siendo la autoridad de acceso a las páginas.
- Reemplazo de los colores azul fijo de hover/selección de la grilla compartida por `color-mix` con `--color-primary` / `--color-surface`; el color procede del branding dinámico del tenant. Esta mejora alcanza también a otros lugares que usan la grilla compartida.
- El índice muestra enlaces a once secciones administrables; las fechas especiales requieren escoger primero la cámara, según el flujo existente.

## Contratos e invariantes preservados

- `catalogs-routing.module.ts` no se editó; quedan 13 entradas, incluido el índice y la redirección.
- Servicios HTTP de catálogos y modelos de negocio no editados.
- Sin cambios en la autorización backend, reglas ACH Colombia/CENIT, estructuras NACHA-M, días/ciclos efectivos, montos, liquidación o endpoints.
- La lógica ya existente de los formularios Material avanzados (entidades, preferencias, festivos, fechas especiales) no se reescribió.
- No se generaron datos maestros, movimientos ni peticiones reales de compensación.

## Evidencia de esta entrega

1. `python scripts/validate-catalogs-ux-static.py` → **23/23 verificaciones PASS**: rutas, componentes, formularios Material, confirmación, errores/reintentos, lenguaje y tokens.
2. Parseo sintáctico TypeScript de los componentes del módulo → **15/15 PASS** con TypeScript global; no implica compilación Angular ni validación del template type-checker.
3. Se añadió `catalogs-material-ux.component.spec.ts` con pruebas sobre formulario Material, validación de espacios, confirmación cancelada, consulta con error y preservación del código SEC.
4. Se ajustó `e2e/audits-catalogs-material.spec.ts` para que los nombres de pantallas y acciones esperados correspondan a la UI real (sin suprimir aserciones).
5. La disponibilidad npm se comprobó: `npm ping --fetch-retries=0 --fetch-timeout=5000` respondió con `EAI_AGAIN registry.npmjs.org`; `node_modules` del SPA no está disponible en este entorno. **No se ejecutaron `npm ci`, `ng build`, Karma, Playwright ni captura visual/UAT.**

## Validaciones pendientes antes de desplegar

En la carpeta `web/ach-interbank-ui` del entorno con acceso a dependencias:

```cmd
npm ci
npm run build
npm test -- --watch=false --browsers=ChromeHeadless --include=src/app/features/catalogs/components/catalogs-material-ux.component.spec.ts
npm test -- --watch=false --browsers=ChromeHeadless --include=src/app/features/catalogs/components/catalogs-phase3.component.spec.ts
```

Después, con backend local autorizado y credenciales de prueba, realizar UAT de **todas** las entradas del inventario en escritorio y móvil; comprobar rutas y permisos, los estados de las listas, las opciones predeterminadas, validaciones y errores, y confirmar que `Cancelar` no hace peticiones de eliminación. Verificar branding de al menos dos entidades con colores primarios distintos, incluyendo uno claro. Las pruebas E2E de catálogos existentes pueden ejecutarse en el entorno local preparado:

```cmd
npm run e2e -- e2e/audits-catalogs-material.spec.ts --project=chromium --workers=1
```

**No se afirma compilación, resultado de pruebas de navegador, aprobación UAT ni despliegue.** El nuevo ZIP requiere reconstrucción/publicación del SPA para observar los cambios.
