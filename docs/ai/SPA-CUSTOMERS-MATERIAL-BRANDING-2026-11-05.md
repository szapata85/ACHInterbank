# ACH Interbank — Modernización UX/UI de `/customers`

**Fecha:** 2026-11-05
**Estado:** `SOURCE_IMPLEMENTED_STATIC_CHECKS_PASS_ANGULAR_RUNTIME_PENDING`
**Base:** `ACHInterbank_CATALOGS_UX_MATERIAL_HUMANIZADO_2026-11-05.zip` (incluye mejoras previas de `/catalogs/*` y `/transactions/nacha-upload`).

## Alcance efectivo

| Ruta | Intervención |
|---|---|
| `/customers` | Listado con filtros Angular Material, etiquetas de persona natural/jurídica humanizadas, búsqueda por razón social, reintento, resultado vacío, progreso y acciones ajustadas al permiso `CanManageAch`. |
| `/customers/new` | Formulario Material estructurado por identificación, datos de la persona y cuentas; validaciones requeridas visibles, prevención de envío duplicado y mensajes en español. |
| `/customers/:id/edit` | Reutiliza el mismo editor; carga con reintento, actualización, manejo de error y confirmación por notificación. |

**Fuera del incremento:** los submódulos específicos `/customers/:id/third-parties` y `/customers/:id/third-parties/:thirdPartyId` se mantienen intactos. No hay cambios en el API, base de datos, dominios, modelos, rutas, guardias, integración CENIT/ACH Colombia ni reglas operativas.

## Cambios UX/UI

- Los controles `<select>` y los inputs sin directiva Material del formulario y del listado se sustituyen por `mat-form-field`, `mat-select`, `matInput`, `mat-error` y ayudas contextuales.
- Botones, paneles, indicadores de carga y etiquetas visuales siguen Angular Material y los tokens globales de `BrandingService` (`--mat-sys-primary`, `--color-primary`, `--color-surface`, `--color-border`). No se codificaron paletas de marca en estos componentes.
- La pantalla de creación/edición separa identificación, persona natural/jurídica y cuentas asociadas, con diseño adaptable, mensajes operativos legibles y etiquetas de accesibilidad.
- Se alinearon con el backend existente las reglas de obligatoriedad de primer nombre y primer apellido (PN), razón social (PJ), tipo/número de documento y al menos una cuenta. Los códigos válidos y el payload del API se conservan.
- En cuentas: advertencia ante blancos y duplicados, eliminación individual, conservación de ceros iniciales y autoinclusión de la cuenta escrita en el campo al guardar (para evitar pérdida silenciosa).
- Consultas y guardados tienen estados separados, errores humanizados, reintento y protección ante peticiones de guardado simultáneas. Se evitan mensajes técnicos de excepción en la interfaz.
- En listados se filtra por documento, nombre o razón social, cuenta y tipo de persona; se muestran nombres de tipo de persona completos, recuento de coincidencias y una ayuda diferenciada cuando no hay registros o los filtros no encuentran coincidencias.
- La opción de edición y «Nuevo cliente» solo se ofrecen visualmente con `CanManageAch`, sin sustituir las guardias de enrutamiento ni la autorización del servidor. «Ver terceros» conserva el flujo de consulta.

## Archivos intervenidos

1. `web/ach-interbank-ui/src/app/features/customers/components/customer-form.component.ts`
2. `web/ach-interbank-ui/src/app/features/customers/components/customer-form.component.html`
3. `web/ach-interbank-ui/src/app/features/customers/components/customer-form.component.scss`
4. `web/ach-interbank-ui/src/app/features/customers/components/customers-list.component.ts`
5. `web/ach-interbank-ui/src/app/features/customers/components/customers-list.component.html`
6. `web/ach-interbank-ui/src/app/features/customers/components/customers-list.component.scss`
7. `web/ach-interbank-ui/src/app/features/customers/components/customers-material-ux.component.spec.ts` (nuevo)
8. `web/ach-interbank-ui/scripts/validate-customers-ux-static.py` (nuevo)
9. `docs/ai/SPA-CUSTOMERS-MATERIAL-BRANDING-2026-11-05.md` (este informe)

## Validaciones y evidencia

- `python web/ach-interbank-ui/scripts/validate-customers-ux-static.py`: **25/25 PASS**.
- Parseo de sintaxis TypeScript de los tres archivos `.ts` del incremento con TypeScript global: **3/3 PASS** (no valida plantillas Angular ni enlaces de dependencias).
- Se incorporaron pruebas Jasmine para: obligatoriedad dinámica PN/PJ, números de cuenta, cuenta pendiente al guardar, protección de doble envío, fallos al recuperar un cliente, permisos de edición, filtro por razón social y error recuperable del listado. **No ejecutadas**.
- Registro npm: `npm ping --fetch-retries=0 --fetch-timeout=5000` devolvió `EAI_AGAIN registry.npmjs.org`; no hay `node_modules` en el SPA de esta sesión. **No se ejecutaron** `npm ci`, `ng build`, Karma, Playwright ni pruebas en navegador/Docker.

## Ejecución y comprobación en el equipo del usuario

Desde `web/ach-interbank-ui`, con registro npm disponible:

```cmd
npm ci
npm run build
npm test -- --watch=false --browsers=ChromeHeadless --include=src/app/features/customers/components/customers-material-ux.component.spec.ts
```

Luego, en la raíz del proyecto nuevo, reconstruir las imágenes (sin eliminar datos ni ejecutar sembrado/migraciones automáticas):

```cmd
docker-ach.cmd down
docker-ach.cmd config
docker-ach.cmd infra
docker-ach.cmd up --no-seed --no-auto-migrations
docker-ach.cmd status
```

**No usar `--no-build`**: de lo contrario Docker puede mostrar el SPA de la imagen anterior. Abrir `/customers`, `/customers/new` y `/customers/{id}/edit` con usuarios de lectura/gestión según permisos; comprobar mensajes, uso con teclado, guardado y persistencia de cuentas. Si no se recompila correctamente, esta entrega no debe considerarse validada en producción.
