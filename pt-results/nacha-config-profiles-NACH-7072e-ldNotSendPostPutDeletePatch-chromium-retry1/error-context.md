# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: nacha-config-profiles.spec.ts >> NACHA config profiles official read-only page >> ConfigProfiles_ShouldNotSendPostPutDeletePatch
- Location: e2e/nacha-config-profiles.spec.ts:60:7

# Error details

```
Error: expect(locator).toBeVisible() failed

Locator: getByText('OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0')
Expected: visible
Timeout: 7500ms
Error: element(s) not found

Call log:
  - Expect "toBeVisible" with timeout 7500ms
  - waiting for getByText('OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0')

```

```yaml
- navigation "Menú principal":
  - text: ACH
  - paragraph: ACH Interbank
  - text: Portal backoffice
  - navigation:
    - link "NACHA-M Configuración":
      - /url: /nacha-config-admin/perfiles
    - button "Alternar submenú de NACHA-M Configuración" [expanded]
    - group "Submenú de NACHA-M Configuración":
      - link "Perfiles oficiales":
        - /url: /nacha-config-admin/perfiles
      - link "Records oficiales":
        - /url: /nacha-config-admin/records
      - link "Variants y Fields":
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
      - link "Reglas por camara":
        - /url: /transactions/clearing-house-rules
      - link "Cargar NACHA-M":
        - /url: /transactions/nacha-upload
      - link "Devoluciones ACH":
        - /url: /transactions/returns
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
    - link "Clientes":
      - /url: /customers
    - link "Reportes":
      - /url: /reports
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
      - 'link "Operación: Optimización"':
        - /url: /cenit/operacion/optimizacion
      - 'link "Operación: Devoluciones"':
        - /url: /cenit/operacion/devoluciones
      - 'link "Operación: Trazabilidad"':
        - /url: /cenit/operacion/trazabilidad
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
  - text: Usuario UAT Config Admin, ACH.Operator
  - button "Cerrar sesión"
- banner:
  - heading "Config Profiles NACHA" [level=1]
  - navigation "Breadcrumbs":
    - link "Config Profiles":
      - /url: /nacha-config-admin
    - text: / Config Profiles
  - text: Usuario UAT Config Admin, ACH.Operator
  - button "Salir"
- main:
  - navigation "Ruta de navegación": /
  - heading "Config Profiles NACHA-M" [level=2]
  - paragraph: Administracion oficial read-only y administrativa de nacha-config profiles
  - text: "Modelo oficial NACHA-M: nacha-config profiles. Legacy layouts/definitions deprecated: solo diagnostico read-only, no fuente oficial. Productivo NO-GO: certificacion/UAT formal pendiente; sin ejecucion SOAP ni mutaciones."
  - heading "Crear borrador" [level=3]
  - paragraph: Solo perfiles BORRADOR/CLONE pueden editarse; create borra datos si el usuario confirma.
  - text: Codigo del perfil
  - textbox "Codigo del perfil":
    - /placeholder: UAT-NACHA-CONFIG-...
  - text: Nombre
  - textbox "Nombre":
    - /placeholder: Nombre descriptivo
  - text: Descripcion
  - textbox "Descripcion":
    - /placeholder: Descripcion opcional
  - text: Camara
  - combobox "Camara"
  - text: Flujo
  - combobox "Flujo"
  - text: Direccion
  - combobox "Direccion"
  - text: Servicio
  - combobox "Servicio":
    - option "Sin servicio" [selected]
  - text: Vigencia inicial
  - textbox "Vigencia inicial": 2026-06-04
  - button "Crear borrador" [disabled]
  - button "Validar" [disabled]
  - heading "Filtros read-only" [level=3]
  - paragraph: Consulta oficial GET-only de perfiles, camaras ACH Colombia/CENIT, records 1/5/6/7/8/9, variants y fields.
  - text: Buscar
  - textbox "Buscar":
    - /placeholder: Codigo o nombre
  - text: Estado
  - textbox "Estado Limpiar Todos":
    - /placeholder: Buscar estado
  - button "Limpiar"
  - button "Todos"
  - text: Camara
  - textbox "Camara Limpiar Todas":
    - /placeholder: Buscar camara
  - button "Limpiar"
  - button "Todas"
  - text: Flujo
  - textbox "Flujo Limpiar Todos":
    - /placeholder: Buscar flujo
  - button "Limpiar"
  - button "Todos"
  - searchbox
  - paragraph: Cargando información...
  - text: 0 a 0 de 0. Página 0 de 0
  - treegrid:
    - rowgroup:
      - row "Codigo Nombre Camara Estado Version Variants":
        - columnheader
        - columnheader "Codigo"
        - columnheader "Nombre"
        - columnheader "Camara"
        - columnheader "Estado"
        - columnheader "Version"
        - columnheader "Variants"
      - row "Open Filter Menu Open Filter Menu Open Filter Menu Open Filter Menu Open Filter Menu Open Filter Menu":
        - gridcell
        - gridcell "Open Filter Menu":
          - textbox "Codigo Filter Input"
          - button "Open Filter Menu": 
        - gridcell "Open Filter Menu":
          - textbox "Nombre Filter Input"
          - button "Open Filter Menu": 
        - gridcell "Open Filter Menu":
          - textbox "Camara Filter Input"
          - button "Open Filter Menu": 
        - gridcell "Open Filter Menu":
          - textbox "Estado Filter Input"
          - button "Open Filter Menu": 
        - gridcell "Open Filter Menu":
          - textbox "Version Filter Input"
          - button "Open Filter Menu": 
        - gridcell "Open Filter Menu":
          - textbox "Variants Filter Input"
          - button "Open Filter Menu": 
    - rowgroup
    - rowgroup
    - rowgroup
    - rowgroup
    - text: Cargando...
  - text: "Page Size:"
  - combobox "Page Size": "15"
  - text: 0 a 0 de 0
  - button "First Page" [disabled]: 
  - button "Previous Page" [disabled]: 
  - text: Página 0 de 0
  - button "Next Page" [disabled]: 
  - button "Last Page" [disabled]: 
```

# Test source

```ts
  1   | import { expect, Page, test } from '@playwright/test';
  2   | 
  3   | const configProfilesPagePath = '/ach/nacha/config-profiles';
  4   | const refreshEndpoint = /\/auth\/refresh$/;
  5   | const navigationEndpoint = /\/navigation\/menu$/;
  6   | const dashboardEndpoint = /\/api\/ach\/nacha\/config-profiles\/dashboard$/;
  7   | const profilesEndpoint = /\/api\/ach\/nacha\/config-profiles$/;
  8   | const detailEndpoint = /\/api\/ach\/nacha\/config-profiles\/1$/;
  9   | const filterCatalogsEndpoint = /\/nacha-config\/catalogos-filtro$/;
  10  | const legacyEndpoint = /\/(ach-cycles\/nacha\/layouts|ach-cycles\/nacha\/definitions|nacha-layouts|nacha-record-definitions)(?:\?.*)?$/;
  11  | const mutatingConfigProfiles = /\/api\/ach\/nacha\/config-profiles/;
  12  | const hashExportPattern = /\/NachaExport\/[a-f0-9]{32,64}$/i;
  13  | 
  14  | test.describe('NACHA config profiles official read-only page', () => {
  15  |   test.beforeEach(async ({ page }) => {
  16  |     await seedAuthenticatedSession(page);
  17  |     await mockAuthRefresh(page);
  18  |     await mockNavigation(page);
  19  |     await mockReadOnlyEndpoints(page);
  20  |   });
  21  | 
  22  |   test('ConfigProfiles_ShouldLoadOfficialPage', async ({ page }) => {
  23  |     await page.goto(configProfilesPagePath);
  24  | 
  25  |     await expect(page).toHaveURL(/\/nacha-config-admin\/perfiles$/);
  26  |     await expect(page.getByText('Config Profiles NACHA-M')).toBeVisible();
  27  |   });
  28  | 
  29  |   test('ConfigProfiles_ShouldShowOfficialModelBanner', async ({ page }) => {
  30  |     await page.goto(configProfilesPagePath);
  31  | 
  32  |     await expect(page.getByText('Modelo oficial NACHA-M: nacha-config profiles.')).toBeVisible();
  33  |   });
  34  | 
  35  |   test('ConfigProfiles_ShouldShowNoGoBanner', async ({ page }) => {
  36  |     await page.goto(configProfilesPagePath);
  37  | 
  38  |     await expect(page.getByText(/Productivo NO-GO/)).toBeVisible();
  39  |   });
  40  | 
  41  |   test('ConfigProfiles_ShouldNotRenderMutationButtons', async ({ page }) => {
  42  |     await page.goto(configProfilesPagePath);
  43  | 
  44  |     await expect(page.getByRole('button', { name: /Crear borrador|Publicar|Guardar|Eliminar|Archivar|Inactivar/i })).toHaveCount(0);
  45  |   });
  46  | 
  47  |   test('ConfigProfiles_ShouldNotCallLegacyLayoutsOrDefinitions', async ({ page }) => {
  48  |     let legacyCalled = false;
  49  |     await page.route(legacyEndpoint, async route => {
  50  |       legacyCalled = true;
  51  |       await route.abort();
  52  |     });
  53  | 
  54  |     await page.goto(configProfilesPagePath);
  55  |     await expect(page.getByText('OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0')).toBeVisible();
  56  | 
  57  |     expect(legacyCalled).toBe(false);
  58  |   });
  59  | 
  60  |   test('ConfigProfiles_ShouldNotSendPostPutDeletePatch', async ({ page }) => {
  61  |     const mutationRequests: string[] = [];
  62  |     page.on('request', request => {
  63  |       if (mutatingConfigProfiles.test(request.url()) && ['POST', 'PUT', 'PATCH', 'DELETE'].includes(request.method())) {
  64  |         mutationRequests.push(`${request.method()} ${request.url()}`);
  65  |       }
  66  |     });
  67  | 
  68  |     await page.goto(configProfilesPagePath);
> 69  |     await expect(page.getByText('OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0')).toBeVisible();
      |                                                                       ^ Error: expect(locator).toBeVisible() failed
  70  | 
  71  |     expect(mutationRequests).toEqual([]);
  72  |   });
  73  | 
  74  |   test('ExportFlow_ShouldStillNotRequestNachaExportWithHash', async ({ page }) => {
  75  |     const exportRequests: string[] = [];
  76  |     page.on('request', request => {
  77  |       if (request.url().includes('/NachaExport/')) {
  78  |         exportRequests.push(request.url());
  79  |       }
  80  |     });
  81  | 
  82  |     await page.goto(configProfilesPagePath);
  83  |     await expect(page.getByText('OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0')).toBeVisible();
  84  | 
  85  |     expect(exportRequests.some(url => hashExportPattern.test(url))).toBe(false);
  86  |   });
  87  | });
  88  | 
  89  | async function mockNavigation(page: Page): Promise<void> {
  90  |   await page.route(navigationEndpoint, async route => {
  91  |     await route.fulfill({
  92  |       status: 200,
  93  |       contentType: 'application/json',
  94  |       body: JSON.stringify([{ id: 3, label: 'Config Profiles', route: '/nacha-config-admin/perfiles' }])
  95  |     });
  96  |   });
  97  | }
  98  | 
  99  | async function mockReadOnlyEndpoints(page: Page): Promise<void> {
  100 |   await page.route(dashboardEndpoint, async route => {
  101 |     await route.fulfill({
  102 |       status: 200,
  103 |       contentType: 'application/json',
  104 |       body: JSON.stringify({
  105 |         productiveStatus: 'NO-GO',
  106 |         isOfficialModel: true,
  107 |         legacyDeprecated: true,
  108 |         profileCount: 1,
  109 |         publishedProfileCount: 1,
  110 |         currentProfileCount: 1,
  111 |         layoutVariantCount: 6,
  112 |         fieldCount: 20,
  113 |         clearingHouses: ['ACH'],
  114 |         recordTypes: ['1', '5', '6', '7', '8', '9'],
  115 |         warnings: []
  116 |       })
  117 |     });
  118 |   });
  119 | 
  120 |   await page.route(profilesEndpoint, async route => {
  121 |     await route.fulfill({
  122 |       status: 200,
  123 |       contentType: 'application/json',
  124 |       body: JSON.stringify([{
  125 |         profileId: 1,
  126 |         profileCode: 'OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0',
  127 |         profileName: 'Perfil oficial ACH Colombia salida original',
  128 |         clearingHouseCode: 'ACH',
  129 |         flowType: 'ORIGINAL',
  130 |         status: 'PUBLICADO',
  131 |         version: 'v1.0',
  132 |         isPublished: true,
  133 |         isCurrent: true,
  134 |         effectiveFrom: '2026-01-01T00:00:00Z',
  135 |         effectiveTo: null,
  136 |         layoutVariantCount: 6,
  137 |         fieldCount: 20,
  138 |         recordTypes: ['1', '5', '6', '7', '8', '9'],
  139 |         isOfficialModel: true,
  140 |         legacyDeprecated: true
  141 |       }])
  142 |     });
  143 |   });
  144 | 
  145 |   await page.route(detailEndpoint, async route => {
  146 |     await route.fulfill({
  147 |       status: 200,
  148 |       contentType: 'application/json',
  149 |       body: JSON.stringify({ profileId: 1, variants: [], fields: [] })
  150 |     });
  151 |   });
  152 | 
  153 |   await page.route(filterCatalogsEndpoint, async route => {
  154 |     await route.fulfill({
  155 |       status: 200,
  156 |       contentType: 'application/json',
  157 |       body: JSON.stringify({
  158 |         estados: [{ code: 'PUBLICADO', labelEs: 'PUBLICADO' }],
  159 |         camaras: [{ code: 'ACH', labelEs: 'ACH Colombia' }, { code: 'CENIT', labelEs: 'CENIT' }],
  160 |         flujos: [{ code: 'ORIGINAL', labelEs: 'Original' }],
  161 |         direcciones: [{ code: 'SALIDA', labelEs: 'Salida' }],
  162 |         servicios: []
  163 |       })
  164 |     });
  165 |   });
  166 | }
  167 | 
  168 | async function seedAuthenticatedSession(page: Page): Promise<void> {
  169 |   const token = createUnsignedJwt({
```