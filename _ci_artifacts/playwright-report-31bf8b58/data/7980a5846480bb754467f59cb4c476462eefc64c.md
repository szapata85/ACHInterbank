# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: nacha-config-official-routes.spec.ts >> NACHA Config official routes >> OfficialRoutes_ShouldUseConfigProfilesAndAvoidLegacyEndpoints
- Location: e2e/nacha-config-official-routes.spec.ts:32:7

# Error details

```
Error: expect(locator).toBeVisible() failed

Locator: getByTestId('nacha-config-variants-fields-page').getByRole('heading', { name: 'NACHA Config - Variantes y campos' })
Expected: visible
Timeout: 7500ms
Error: element(s) not found

Call log:
  - Expect "toBeVisible" with timeout 7500ms
  - waiting for getByTestId('nacha-config-variants-fields-page').getByRole('heading', { name: 'NACHA Config - Variantes y campos' })

```

```yaml
- navigation "Menú principal":
  - text: ACH
  - paragraph: ACH Interbank
  - text: Portal backoffice
  - navigation:
    - link "Configuración NACHA-M":
      - /url: /nacha-config-admin/perfiles
    - button "Alternar submenú de Configuración NACHA-M" [expanded]
    - group "Submenú de Configuración NACHA-M":
      - link "Perfiles oficiales":
        - /url: /nacha-config-admin/perfiles
      - link "Registros oficiales":
        - /url: /nacha-config-admin/records
      - link "Variantes y campos":
        - /url: /nacha-config-admin/variants-fields
    - link "Transacciones":
      - /url: /transactions
    - button "Alternar submenú de Transacciones"
    - group "Submenú de Transacciones":
      - link "Listado":
        - /url: /transactions/list
      - link "Crear transacción":
        - /url: /transactions/create
      - link "Carga masiva":
        - /url: /transactions/bulk-create
      - link "Carga masiva por archivo":
        - /url: /transactions/bulk-ingestion/upload
      - link "Seguimiento lotes":
        - /url: /transactions/bulk-ingestion/tracking
      - link "Config. ciclos":
        - /url: /transactions/cycle-configs
      - link "Reglas por cámara":
        - /url: /transactions/clearing-house-rules
      - link "Cargar NACHA-M":
        - /url: /transactions/nacha-upload
      - link "Devoluciones ACH":
        - /url: /transactions/returns
    - link "Clientes":
      - /url: /customers
    - link "Reportes":
      - /url: /reports
    - link "Respuestas ACH":
      - /url: /ach-responses
    - button "Alternar submenú de Respuestas ACH"
    - group "Submenú de Respuestas ACH":
      - link "Bandeja":
        - /url: /ach-responses
      - link "Revisión manual":
        - /url: /ach-responses/manual-review
      - link "Homologaciones":
        - /url: /ach-responses/status-mappings
      - link "Dashboard operativo":
        - /url: /ach-responses/dashboard
    - link "CENIT":
      - /url: /cenit
    - button "Alternar submenú de CENIT"
    - group "Submenú de CENIT":
      - 'link "Regulatorio: Devoluciones"':
        - /url: /cenit/regulatorio/causales-devolucion
      - 'link "Regulatorio: Rechazos"':
        - /url: /cenit/regulatorio/causales-rechazo
      - 'link "Regulatorio: Políticas"':
        - /url: /cenit/regulatorio/politicas-transaccion
      - 'link "Operación: Ciclos"':
        - /url: /cenit/operacion/ciclos
      - 'link "Operación: Cola"':
        - /url: /cenit/operacion/cola
      - 'link "Operación: Neteo"':
        - /url: /cenit/operacion/neteo
      - 'link "Operación: Optimizacion"':
        - /url: /cenit/operacion/optimizacion
      - 'link "Operación: Devoluciones"':
        - /url: /cenit/operacion/devoluciones
      - 'link "Operación: Trazabilidad"':
        - /url: /cenit/operacion/trazabilidad
    - link "Seguridad NACHA":
      - /url: /nacha-security/dashboard
    - button "Alternar submenú de Seguridad NACHA"
    - group "Submenú de Seguridad NACHA":
      - link "Dashboard seguridad":
        - /url: /nacha-security/dashboard
      - link "Certificados":
        - /url: /nacha-security/certificates
      - link "Generar NACHA-M":
        - /url: /nacha-security/nacha/generate
      - link "Generar NACHA-M cifrado":
        - /url: /nacha-security/nacha/generate-encrypted
      - link "Cifrado manual":
        - /url: /nacha-security/digital-envelope/manual-encrypt
      - link "Descifrado manual":
        - /url: /nacha-security/digital-envelope/manual-decrypt
      - link "Auditoría operaciones":
        - /url: /nacha-security/digital-envelope/audit
      - link "Interoperabilidad":
        - /url: /nacha-security/digital-envelope/interoperability
    - link "SOAP UAT Console":
      - /url: /ach/nacha/soap-uat-console
    - button "Alternar submenú de SOAP UAT Console"
    - group "Submenú de SOAP UAT Console":
      - link "SOAP UAT Console":
        - /url: /ach/nacha/soap-uat-console
    - link "Conciliacion ACH":
      - /url: /ach/reconciliation
    - button "Alternar submenú de Conciliacion ACH"
    - group "Submenú de Conciliacion ACH":
      - link "Conciliacion ACH":
        - /url: /ach/reconciliation
    - link "Logs":
      - /url: /audit-logs
    - button "Alternar submenú de Logs"
    - group "Submenú de Logs":
      - link "Log de auditoría":
        - /url: /audit-logs
      - link "Log de autenticaciones":
        - /url: /auth-logs
      - link "Log de navegación":
        - /url: /navigation-logs
    - link "Catálogos":
      - /url: /catalogs
    - button "Alternar submenú de Catálogos"
    - group "Submenú de Catálogos":
      - link "Conceptos de lote":
        - /url: /catalogs/company-entry-descriptions
      - link "Tipos de documento":
        - /url: /catalogs/document-types
      - link "Tipos de género":
        - /url: /catalogs/gender-types
      - link "Tipos de persona":
        - /url: /catalogs/person-types
      - link "Tipos de teléfono":
        - /url: /catalogs/phone-types
      - link "Tipos de correo":
        - /url: /catalogs/email-types
      - link "Tipos de dirección":
        - /url: /catalogs/address-types
      - link "Códigos de transacción ACH":
        - /url: /catalogs/transaction-codes
  - paragraph: Perfil
  - text: Usuario UAT Oficial Admin, ACH.Operator
  - button "Cerrar sesión"
- banner:
  - heading "Variantes y campos" [level=1]
  - navigation "Breadcrumbs":
    - link "Config Profiles":
      - /url: /nacha-config-admin
    - text: / Variantes y campos
  - text: Usuario UAT Oficial Admin, ACH.Operator
  - button "Salir"
- main:
  - heading "NACHA Config - Variants y Fields" [level=2]
  - paragraph: Workspace administrativo oficial sobre nacha-config profiles.
  - button "Ir a perfil"
  - button "Ir a records"
  - text: NACHA-M oficial usa nacha-config profiles como fuente maestra para variants y fields.
  - paragraph: Cargando detalle del profile seleccionado...
  - article:
    - heading "Perfil seleccionado" [level=3]
    - paragraph: Selecciona el profile oficial sobre el que quieres administrar variants y fields.
    - text: SIN ESTADO Profile
    - combobox "Profile":
      - option "Selecciona un profile" [selected]
      - option "CENIT-OUT-220 · CENIT salida 220 · Published · v1.0"
```

# Test source

```ts
  1   | import { expect, Page, test } from '@playwright/test';
  2   | 
  3   | const refreshEndpoint = /\/auth\/refresh$/;
  4   | const navigationEndpoint = /\/navigation\/menu$/;
  5   | const catalogsEndpoint = /\/nacha-config\/catalogos-filtro$/;
  6   | const dashboardEndpoint = /\/api\/ach\/nacha\/config-profiles\/dashboard$/;
  7   | const profilesReadOnlyEndpoint = /\/api\/ach\/nacha\/config-profiles$/;
  8   | const layoutsEndpoint = /\/nacha-layouts(?:\?.*)?$/;
  9   | const definitionsEndpoint = /\/nacha-record-definitions(?:\?.*)?$/;
  10  | const configProfilesEndpoint = /\/api\/ach\/nacha\/config-profiles$/;
  11  | const configProfileDetailEndpoint = /\/nacha-config\/perfiles\/10$/;
  12  | 
  13  | test.describe('NACHA Config official routes', () => {
  14  |   test.beforeEach(async ({ page }) => {
  15  |     await mockNachaConfigBackend(page);
  16  |     await mockAuthRefresh(page);
  17  |     await mockNavigation(page);
  18  |     await authenticate(page);
  19  |   });
  20  | 
  21  |   test('Navigation_ShouldExposeOfficialNachaConfigMenuOnly', async ({ page }) => {
  22  |     await page.goto('/nacha-config-admin/perfiles');
  23  | 
  24  |     await expect(page.getByRole('link', { name: 'Perfiles oficiales' })).toBeVisible();
  25  |     await expect(page.getByRole('link', { name: 'Registros oficiales' })).toBeVisible();
  26  |     await expect(page.getByRole('link', { name: 'Variantes y campos' })).toBeVisible();
  27  |     await expect(page.getByRole('link', { name: /legacy/i })).toHaveCount(0);
  28  |     await expect(page.getByRole('link', { name: 'Layouts NACHA' })).toHaveCount(0);
  29  |     await expect(page.getByRole('link', { name: 'Definiciones NACHA' })).toHaveCount(0);
  30  |   });
  31  | 
  32  |   test('OfficialRoutes_ShouldUseConfigProfilesAndAvoidLegacyEndpoints', async ({ page }) => {
  33  |     const legacyRequests: string[] = [];
  34  |     const htmlJsResponses: string[] = [];
  35  |     const chunkRequestFailures: string[] = [];
  36  |     const consoleErrors: string[] = [];
  37  |     page.on('request', request => {
  38  |       if (layoutsEndpoint.test(request.url()) || definitionsEndpoint.test(request.url())) {
  39  |         legacyRequests.push(request.url());
  40  |       }
  41  |     });
  42  |     page.on('requestfailed', request => {
  43  |       if (request.url().endsWith('.js')) {
  44  |         chunkRequestFailures.push(`${request.method()} ${request.url()} ${request.failure()?.errorText ?? ''}`.trim());
  45  |       }
  46  |     });
  47  |     page.on('console', message => {
  48  |       if (message.type() === 'error') {
  49  |         const text = message.text();
  50  |         if (!text.includes('net::ERR_CONNECTION_REFUSED')) {
  51  |           consoleErrors.push(text);
  52  |         }
  53  |       }
  54  |     });
  55  |     page.on('response', async response => {
  56  |       const url = response.url();
  57  |       if (!url.endsWith('.js')) {
  58  |         return;
  59  |       }
  60  | 
  61  |       const contentType = response.headers()['content-type'] ?? '';
  62  |       if (contentType.includes('text/html')) {
  63  |         htmlJsResponses.push(`${response.status()} ${url} ${contentType}`);
  64  |       }
  65  |     });
  66  | 
  67  |     await page.goto('/nacha-config-admin/perfiles');
  68  | 
  69  |     await expect(page.getByTestId('nacha-config-profiles-page').getByRole('heading', { name: 'Config Profiles NACHA-M' })).toBeVisible();
  70  |     await expect(page.getByRole('button', { name: 'Crear borrador' })).toBeVisible();
  71  |     await expect(page.getByRole('button', { name: 'Validar' })).toBeVisible();
  72  | 
  73  |     await page.goto('/nacha-config-admin/variants-fields');
  74  | 
> 75  |     await expect(page.getByTestId('nacha-config-variants-fields-page').getByRole('heading', { name: 'NACHA Config - Variantes y campos' })).toBeVisible();
      |                                                                                                                                             ^ Error: expect(locator).toBeVisible() failed
  76  |     await expect(page.getByTestId('nacha-config-variants-fields-page')).toContainText('Workspace administrativo oficial sobre nacha-config profiles.');
  77  |     await expect(page.getByRole('button', { name: /Crear|Editar|Eliminar/i })).toHaveCount(0);
  78  | 
  79  |     await page.goto('/nacha-config-admin/records');
  80  | 
  81  |     await expect(page.getByTestId('nacha-config-records-page').getByRole('heading', { name: 'NACHA Config - Records' })).toBeVisible();
  82  |     await expect(page.getByTestId('nacha-config-records-page').locator('ui-alerta').first()).toContainText('nacha-config profiles es la fuente oficial.');
  83  |     await expect(page.getByTestId('nacha-config-records-page').getByRole('row', { name: /1 1 Si 1 1 STATIC/ })).toBeVisible();
  84  |     await expect(page.getByTestId('nacha-config-records-page').getByRole('button', { name: /Crear|Editar|Eliminar/i })).toHaveCount(0);
  85  | 
  86  |     await page.goto('/nacha-config-admin/perfiles/10');
  87  | 
  88  |     await expect(page.getByTestId('nacha-config-profile-workspace-page').getByRole('heading', { name: 'Perfil CENIT-OUT-220' })).toBeVisible();
  89  |     await expect(page.getByRole('button', { name: 'Clonar como borrador' })).toBeVisible();
  90  |     await expect(page.getByRole('button', { name: 'Ir a records oficiales' })).toBeVisible();
  91  |     await expect(page.getByRole('button', { name: 'Ir a variants y fields' })).toBeVisible();
  92  |     expect(legacyRequests).toEqual([]);
  93  |     expect(htmlJsResponses).toEqual([]);
  94  |     expect(chunkRequestFailures).toEqual([]);
  95  |     expect(consoleErrors).toEqual([]);
  96  |   });
  97  | 
  98  |   test('LegacyRoutes_ShouldEndInNotFound', async ({ page }) => {
  99  |     await page.goto('/nacha-config-admin/perfiles');
  100 |     await page.goto('/ach-cycles/nacha/layouts');
  101 |     await expect(page).toHaveURL(/\/not-found$/);
  102 |     await expect(page.getByText('404', { exact: true })).toBeVisible();
  103 | 
  104 |     await page.goto('/ach-cycles/nacha/definitions');
  105 |     await expect(page).toHaveURL(/\/not-found$/);
  106 |     await expect(page.getByText('404', { exact: true })).toBeVisible();
  107 | 
  108 |     await page.goto('/nacha-layouts');
  109 |     await expect(page).toHaveURL(/\/not-found$/);
  110 |     await expect(page.getByText('404', { exact: true })).toBeVisible();
  111 | 
  112 |     await page.goto('/nacha-record-definitions');
  113 |     await expect(page).toHaveURL(/\/not-found$/);
  114 |     await expect(page.getByText('404', { exact: true })).toBeVisible();
  115 |   });
  116 | });
  117 | 
  118 | async function mockNavigation(page: Page): Promise<void> {
  119 |   await page.route(navigationEndpoint, async route => {
  120 |     await route.fulfill({
  121 |       status: 200,
  122 |       contentType: 'application/json',
  123 |       body: JSON.stringify([
  124 |         {
  125 |           id: 20,
  126 |           label: 'Configuración NACHA-M',
  127 |           route: '/nacha-config-admin/perfiles',
  128 |           children: [
  129 |             { id: 25, label: 'Perfiles oficiales', route: '/nacha-config-admin/perfiles' },
  130 |             { id: 2802, label: 'Registros oficiales', route: '/nacha-config-admin/records' },
  131 |             { id: 2803, label: 'Variantes y campos', route: '/nacha-config-admin/variants-fields' }
  132 |           ]
  133 |         }
  134 |       ])
  135 |     });
  136 |   });
  137 | }
  138 | 
  139 | async function mockOfficialConfigProfiles(page: Page): Promise<void> {
  140 |   await page.route(configProfilesEndpoint, async route => {
  141 |     await route.fulfill({
  142 |       status: 200,
  143 |       contentType: 'application/json',
  144 |       body: JSON.stringify([
  145 |         {
  146 |           profileId: 10,
  147 |           profileCode: 'CENIT-OUT-220',
  148 |           profileName: 'CENIT salida 220',
  149 |           clearingHouseCode: 'CENIT',
  150 |           flowType: 'Outgoing',
  151 |           status: 'Published',
  152 |           version: '1.0',
  153 |           isPublished: true,
  154 |           isCurrent: true,
  155 |           effectiveFrom: '2026-01-01T00:00:00Z',
  156 |           effectiveTo: null,
  157 |           layoutVariantCount: 6,
  158 |           fieldCount: 42,
  159 |           recordTypes: ['1', '5', '6', '7', '8', '9'],
  160 |           isOfficialModel: true,
  161 |           legacyDeprecated: true
  162 |         }
  163 |       ])
  164 |     });
  165 |   });
  166 | }
  167 | 
  168 | async function mockNachaConfigBackend(page: Page): Promise<void> {
  169 |   await page.route(/(?:https?:\/\/[^/]+)?\/(?:nacha-config\/catalogos-filtro|api\/ach\/nacha\/config-profiles(?:\/dashboard)?|nacha-config\/perfiles\/10)(?:\?.*)?$/i, async route => {
  170 |     const url = new URL(route.request().url());
  171 |     const path = url.pathname;
  172 |     const method = route.request().method().toUpperCase();
  173 | 
  174 |     if (method === 'GET' && path === '/nacha-config/catalogos-filtro') {
  175 |       await route.fulfill({
```