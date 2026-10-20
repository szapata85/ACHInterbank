# NACHA-CONFIG-UX-001 — Línea base UX0

**Fecha:** 2026-10-11  
**Fase:** UX0 — Congelación y línea base  
**Resultado:** CLOSED con limitaciones de ejecución del entorno registradas.

## Alcance caracterizado

- Rutas administrativas vigentes: `/nacha-config-admin/perfiles`, `/nacha-config-admin/records`, `/nacha-config-admin/variants-fields` y `/nacha-config-admin/perfiles/:id`.
- La ruta histórica `/ach/nacha/config-profiles` continúa redirigiendo al listado oficial.
- El listado navega al detalle usando el identificador persistido del perfil. Se agregó regresión E2E que abre un perfil existente desde el listado y verifica que el detalle no termine en 404.
- Los catálogos del filtro/creación se cargan desde `CatConfigStatus`, `CatClearingHouse`, `CatFlowType`, `CatDirection` y `CatServiceClass`. UX0 no cambia la brecha conocida de servicios todavía no dependientes de cámara.
- La edición de metadatos está limitada a perfiles `BORRADOR`; perfiles `PUBLICADO` no pueden ejecutar `UpdateDraftAsync`. Se agregó caracterización Angular explícita del guard además de la caracterización backend existente.
- El branding se obtiene del servidor por `BrandingService` y actualiza en runtime `--brand-primary`, `--mat-sys-primary` y sus puentes de tokens. Se agregó una caracterización E2E de la pantalla NACHA Config con color servido por API.

## Congelación del motor NACHA-M

El manifiesto `NACHA_CONFIG_UX0_ENGINE_BASELINE.json` fija SHA-256 y tamaño de:

- `NachaFileBuilder.cs`;
- `NachaFileGenerationCoordinator.cs`;
- `NachaConfigResolver.cs`;
- `NachaOfficialRecordRenderer.cs`;
- `NachaPublicationSnapshotSerializer.cs`;
- seis golden files ACH Colombia/CENIT de entrada, salida y retornos.

El guardrail `scripts/validate-nacha-config-ux0-baseline.py` verifica esos bytes y los contratos de caracterización de rutas, catálogos, borrador/publicado y branding. No se modificó ningún archivo del motor ni ningún golden.

## Pruebas y verificaciones ejecutadas

| Verificación | Resultado | Evidencia |
|---|---|---|
| `python scripts/validate-nacha-config-ux0-baseline.py` | PASS_STATIC | 25 checks; 11 archivos protegidos con hash idéntico al manifiesto UX0. |
| Transpilación sintáctica de los 3 specs TypeScript modificados con TypeScript 5.8.3 disponible en el host | PASS_SYNTAX | Sin errores sintácticos en E2E, listado ni workspace. |
| Angular focal: perfiles, workspace y `BrandingService` | NOT_RUN_ENVIRONMENT | `node_modules` no venía en el ZIP. `npm ci --offline --ignore-scripts` falló `ENOTCACHED`; `npm ci --ignore-scripts` agotó el tiempo del entorno; `npm test ...` no pudo resolver `ng`. |
| Playwright focal `nacha-config-official-routes.spec.ts` | NOT_RUN_ENVIRONMENT | No hay instalación local de Playwright en el artefacto; el comando disponible no soportó `playwright test`. |
| Backend focal `NachaConfigAdminServicesHardeningTests` + generación oficial | NOT_RUN_ENVIRONMENT | `dotnet` no está instalado en el entorno (`command not found`). |

La imposibilidad de ejecutar Angular/.NET/Playwright es una limitación real del entorno, no un PASS implícito. Las pruebas focales quedaron incorporadas para ejecución en un host con dependencias instaladas.

## Cambios UX0

- Nueva prueba E2E listado → detalle sin 404.
- Nueva prueba E2E de branding dinámico servido por API.
- Nuevas caracterizaciones unitarias de navegación a perfil existente y rechazo de guardado sobre perfil publicado.
- Nuevo manifiesto de hashes del motor/goldens y guardrail estático UX0.
- Sin cambios funcionales de generación NACHA-M, catálogos, reglas, persistencia o esquema.
