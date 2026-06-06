# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: nacha-config-profiles.spec.ts >> NACHA config profiles official read-only page >> ConfigProfiles_ShouldNotRenderMutationButtons
- Location: e2e/nacha-config-profiles.spec.ts:41:7

# Error details

```
Error: expect(locator).toHaveCount(expected) failed

Locator:  getByRole('button', { name: /Crear borrador|Publicar|Guardar|Eliminar|Archivar|Inactivar/i })
Expected: 0
Received: 1
Timeout:  7500ms

Call log:
  - Expect "toHaveCount" with timeout 7500ms
  - waiting for getByRole('button', { name: /Crear borrador|Publicar|Guardar|Eliminar|Archivar|Inactivar/i })
    19 × locator resolved to 1 element
       - unexpected value "1"

```

# Page snapshot

```yaml
- generic [ref=e4]:
  - navigation "Menú principal" [ref=e5]:
    - generic [ref=e6]:
      - generic [ref=e7]: ACH
      - generic [ref=e8]:
        - paragraph [ref=e9]: ACH Interbank
        - generic [ref=e10]: Portal backoffice
    - navigation [ref=e11]:
      - generic [ref=e12]:
        - generic [ref=e13]:
          - link "NACHA-M Configuración" [ref=e14] [cursor=pointer]:
            - /url: /nacha-config-admin/perfiles
            - generic [ref=e15]: NACHA-M Configuración
          - button "Alternar submenú de NACHA-M Configuración" [expanded] [ref=e16] [cursor=pointer]:
            - generic [ref=e17]: expand_more
        - group "Submenú de NACHA-M Configuración" [ref=e18]:
          - link "Perfiles oficiales" [ref=e19] [cursor=pointer]:
            - /url: /nacha-config-admin/perfiles
            - generic [ref=e20]: fact_check
            - generic [ref=e21]: Perfiles oficiales
          - link "Records oficiales" [ref=e22] [cursor=pointer]:
            - /url: /nacha-config-admin/records
            - generic [ref=e23]: view_list
            - generic [ref=e24]: Records oficiales
          - link "Variants y Fields" [ref=e25] [cursor=pointer]:
            - /url: /nacha-config-admin/variants-fields
            - generic [ref=e26]: schema
            - generic [ref=e27]: Variants y Fields
      - generic [ref=e28]:
        - generic [ref=e29]:
          - link "Transacciones" [ref=e30] [cursor=pointer]:
            - /url: /transactions
            - generic [ref=e31]: payments
            - generic [ref=e32]: Transacciones
          - button "Alternar submenú de Transacciones" [ref=e33] [cursor=pointer]:
            - generic [ref=e34]: expand_more
        - group "Submenú de Transacciones":
          - link "Listado":
            - /url: /transactions/list
            - generic: list
            - generic: Listado
          - link "Crear transacción":
            - /url: /transactions/create
            - generic: add_circle
            - generic: Crear transacción
          - link "Carga masiva":
            - /url: /transactions/bulk-create
            - generic: upload_file
            - generic: Carga masiva
          - link "Carga masiva por archivo":
            - /url: /transactions/bulk-ingestion/upload
            - generic: file_upload
            - generic: Carga masiva por archivo
          - link "Seguimiento lotes":
            - /url: /transactions/bulk-ingestion/tracking
            - generic: monitoring
            - generic: Seguimiento lotes
          - link "Config. ciclos":
            - /url: /transactions/cycle-configs
            - generic: schedule
            - generic: Config. ciclos
          - link "Reglas por camara":
            - /url: /transactions/clearing-house-rules
            - generic: rule
            - generic: Reglas por camara
          - link "Cargar NACHA-M":
            - /url: /transactions/nacha-upload
            - generic: upload
            - generic: Cargar NACHA-M
          - link "Devoluciones ACH":
            - /url: /transactions/returns
            - generic: assignment_return
            - generic: Devoluciones ACH
      - generic [ref=e35]:
        - generic [ref=e36]:
          - link "Respuestas ACH" [ref=e37] [cursor=pointer]:
            - /url: /ach-responses
            - generic [ref=e38]: assignment
            - generic [ref=e39]: Respuestas ACH
          - button "Alternar submenú de Respuestas ACH" [ref=e40] [cursor=pointer]:
            - generic [ref=e41]: expand_more
        - group "Submenú de Respuestas ACH":
          - link "Bandeja":
            - /url: /ach-responses
            - generic: assignment
            - generic: Bandeja
          - link "Revisión manual":
            - /url: /ach-responses/manual-review
            - generic: rule
            - generic: Revisión manual
          - link "Homologaciones":
            - /url: /ach-responses/status-mappings
            - generic: sync_alt
            - generic: Homologaciones
          - link "Dashboard operativo":
            - /url: /ach-responses/dashboard
            - generic: dashboard
            - generic: Dashboard operativo
      - link "Clientes" [ref=e44] [cursor=pointer]:
        - /url: /customers
        - generic [ref=e45]: group
        - generic [ref=e46]: Clientes
      - link "Reportes" [ref=e49] [cursor=pointer]:
        - /url: /reports
        - generic [ref=e50]: analytics
        - generic [ref=e51]: Reportes
      - generic [ref=e52]:
        - generic [ref=e53]:
          - link "CENIT" [ref=e54] [cursor=pointer]:
            - /url: /cenit
            - generic [ref=e55]: monitoring
            - generic [ref=e56]: CENIT
          - button "Alternar submenú de CENIT" [ref=e57] [cursor=pointer]:
            - generic [ref=e58]: expand_more
        - group "Submenú de CENIT":
          - 'link "Regulatorio: Devoluciones"':
            - /url: /cenit/regulatorio/causales-devolucion
            - generic: rule
            - generic: "Regulatorio: Devoluciones"
          - 'link "Regulatorio: Rechazos"':
            - /url: /cenit/regulatorio/causales-rechazo
            - generic: gavel
            - generic: "Regulatorio: Rechazos"
          - 'link "Regulatorio: Políticas"':
            - /url: /cenit/regulatorio/politicas-transaccion
            - generic: policy
            - generic: "Regulatorio: Políticas"
          - 'link "Operación: Ciclos"':
            - /url: /cenit/operacion/ciclos
            - generic: schedule
            - generic: "Operación: Ciclos"
          - 'link "Operación: Cola"':
            - /url: /cenit/operacion/cola
            - generic: queue
            - generic: "Operación: Cola"
          - 'link "Operación: Neteo"':
            - /url: /cenit/operacion/neteo
            - generic: account_balance
            - generic: "Operación: Neteo"
          - 'link "Operación: Optimización"':
            - /url: /cenit/operacion/optimizacion
            - generic: tune
            - generic: "Operación: Optimización"
          - 'link "Operación: Devoluciones"':
            - /url: /cenit/operacion/devoluciones
            - generic: assignment_return
            - generic: "Operación: Devoluciones"
          - 'link "Operación: Trazabilidad"':
            - /url: /cenit/operacion/trazabilidad
            - generic: travel_explore
            - generic: "Operación: Trazabilidad"
      - generic [ref=e59]:
        - generic [ref=e60]:
          - link "SOAP UAT Console" [ref=e61] [cursor=pointer]:
            - /url: /ach/nacha/soap-uat-console
            - generic [ref=e62]: fact_check
            - generic [ref=e63]: SOAP UAT Console
          - button "Alternar submenú de SOAP UAT Console" [ref=e64] [cursor=pointer]:
            - generic [ref=e65]: expand_more
        - group "Submenú de SOAP UAT Console":
          - link "SOAP UAT Console":
            - /url: /ach/nacha/soap-uat-console
            - generic: fact_check
            - generic: SOAP UAT Console
      - generic [ref=e66]:
        - generic [ref=e67]:
          - link "Conciliacion ACH" [ref=e68] [cursor=pointer]:
            - /url: /ach/reconciliation
            - generic [ref=e69]: fact_check
            - generic [ref=e70]: Conciliacion ACH
          - button "Alternar submenú de Conciliacion ACH" [ref=e71] [cursor=pointer]:
            - generic [ref=e72]: expand_more
        - group "Submenú de Conciliacion ACH":
          - link "Conciliacion ACH":
            - /url: /ach/reconciliation
            - generic: fact_check
            - generic: Conciliacion ACH
      - generic [ref=e73]:
        - generic [ref=e74]:
          - link "Seguridad NACHA" [ref=e75] [cursor=pointer]:
            - /url: /nacha-security/dashboard
            - generic [ref=e76]: security
            - generic [ref=e77]: Seguridad NACHA
          - button "Alternar submenú de Seguridad NACHA" [ref=e78] [cursor=pointer]:
            - generic [ref=e79]: expand_more
        - group "Submenú de Seguridad NACHA":
          - link "Dashboard seguridad":
            - /url: /nacha-security/dashboard
            - generic: shield
            - generic: Dashboard seguridad
          - link "Certificados":
            - /url: /nacha-security/certificates
            - generic: badge
            - generic: Certificados
          - link "Generar NACHA-M":
            - /url: /nacha-security/nacha/generate
            - generic: description
            - generic: Generar NACHA-M
          - link "Generar NACHA-M cifrado":
            - /url: /nacha-security/nacha/generate-encrypted
            - generic: encrypted
            - generic: Generar NACHA-M cifrado
          - link "Cifrado manual":
            - /url: /nacha-security/digital-envelope/manual-encrypt
            - generic: lock
            - generic: Cifrado manual
          - link "Descifrado manual":
            - /url: /nacha-security/digital-envelope/manual-decrypt
            - generic: lock_open
            - generic: Descifrado manual
          - link "Auditoría operaciones":
            - /url: /nacha-security/digital-envelope/audit
            - generic: fact_check
            - generic: Auditoría operaciones
          - link "Interoperabilidad":
            - /url: /nacha-security/digital-envelope/interoperability
            - generic: hub
            - generic: Interoperabilidad
      - generic [ref=e80]:
        - generic [ref=e81]:
          - link "Logs" [ref=e82] [cursor=pointer]:
            - /url: /audit-logs
            - generic [ref=e83]: receipt_long
            - generic [ref=e84]: Logs
          - button "Alternar submenú de Logs" [ref=e85] [cursor=pointer]:
            - generic [ref=e86]: expand_more
        - group "Submenú de Logs":
          - link "Log de auditoría":
            - /url: /audit-logs
            - generic: fact_check
            - generic: Log de auditoría
          - link "Log de autenticaciones":
            - /url: /auth-logs
            - generic: shield
            - generic: Log de autenticaciones
          - link "Log de navegación":
            - /url: /navigation-logs
            - generic: route
            - generic: Log de navegación
      - generic [ref=e87]:
        - generic [ref=e88]:
          - link "Catálogos" [ref=e89] [cursor=pointer]:
            - /url: /catalogs
            - generic [ref=e90]: list_alt
            - generic [ref=e91]: Catálogos
          - button "Alternar submenú de Catálogos" [ref=e92] [cursor=pointer]:
            - generic [ref=e93]: expand_more
        - group "Submenú de Catálogos":
          - link "Conceptos de lote":
            - /url: /catalogs/company-entry-descriptions
            - generic: list
            - generic: Conceptos de lote
          - link "Tipos de documento":
            - /url: /catalogs/document-types
            - generic: badge
            - generic: Tipos de documento
          - link "Tipos de género":
            - /url: /catalogs/gender-types
            - generic: diversity_3
            - generic: Tipos de género
          - link "Tipos de persona":
            - /url: /catalogs/person-types
            - generic: apartment
            - generic: Tipos de persona
          - link "Tipos de teléfono":
            - /url: /catalogs/phone-types
            - generic: call
            - generic: Tipos de teléfono
          - link "Tipos de correo":
            - /url: /catalogs/email-types
            - generic: mail
            - generic: Tipos de correo
          - link "Tipos de dirección":
            - /url: /catalogs/address-types
            - generic: location_on
            - generic: Tipos de dirección
          - link "Códigos de transacción ACH":
            - /url: /catalogs/transaction-codes
            - generic: numbers
            - generic: Códigos de transacción ACH
    - generic [ref=e94]:
      - paragraph [ref=e95]: Perfil
      - generic [ref=e96]:
        - generic [ref=e97]: U
        - generic [ref=e98]:
          - generic [ref=e99]: Usuario UAT Config
          - generic [ref=e100]: Admin, ACH.Operator
      - button "Cerrar sesión" [ref=e101] [cursor=pointer]
  - generic [ref=e102]:
    - banner [ref=e103]:
      - generic [ref=e105]:
        - heading "Config Profiles NACHA" [level=1] [ref=e106]
        - navigation "Breadcrumbs" [ref=e107]:
          - link "Config Profiles" [ref=e108] [cursor=pointer]:
            - /url: /nacha-config-admin
          - generic [ref=e109]: /
          - generic [ref=e110]: Config Profiles
      - generic [ref=e111]:
        - generic [ref=e112]:
          - generic [ref=e113]: Usuario UAT Config
          - generic [ref=e114]: Admin, ACH.Operator
        - button "Salir" [ref=e115] [cursor=pointer]
    - main [ref=e116]:
      - generic [ref=e118]:
        - navigation "Ruta de navegación" [ref=e120]:
          - generic [ref=e121]: /
        - generic [ref=e124]:
          - heading "Config Profiles NACHA-M" [level=2] [ref=e125]
          - paragraph [ref=e126]: Administracion oficial read-only y administrativa de nacha-config profiles
        - generic [ref=e127]:
          - generic [ref=e130]: "Modelo oficial NACHA-M: nacha-config profiles."
          - generic [ref=e133]: "Legacy layouts/definitions deprecated: solo diagnostico read-only, no fuente oficial."
          - generic [ref=e136]: "Productivo NO-GO: certificacion/UAT formal pendiente; sin ejecucion SOAP ni mutaciones."
        - generic [ref=e138]:
          - generic [ref=e139]:
            - heading "Crear borrador" [level=3] [ref=e140]
            - paragraph [ref=e141]: Solo perfiles BORRADOR/CLONE pueden editarse; create borra datos si el usuario confirma.
          - generic [ref=e142]:
            - generic [ref=e144]:
              - generic [ref=e145]: Codigo del perfil
              - textbox "Codigo del perfil" [ref=e146]:
                - /placeholder: UAT-NACHA-CONFIG-...
            - generic [ref=e148]:
              - generic [ref=e149]: Nombre
              - textbox "Nombre" [ref=e150]:
                - /placeholder: Nombre descriptivo
            - generic [ref=e152]:
              - generic [ref=e153]: Descripcion
              - textbox "Descripcion" [ref=e154]:
                - /placeholder: Descripcion opcional
            - generic [ref=e155]:
              - generic [ref=e156]: Camara
              - combobox "Camara" [ref=e157]
            - generic [ref=e158]:
              - generic [ref=e159]: Flujo
              - combobox "Flujo" [ref=e160]
            - generic [ref=e161]:
              - generic [ref=e162]: Direccion
              - combobox "Direccion" [ref=e163]
            - generic [ref=e164]:
              - generic [ref=e165]: Servicio
              - combobox "Servicio" [ref=e166]:
                - option "Sin servicio" [selected]
            - generic [ref=e168]:
              - generic [ref=e169]: Vigencia inicial
              - textbox "Vigencia inicial" [ref=e170]: 2026-06-04
          - generic [ref=e171]:
            - button "Crear borrador" [disabled] [ref=e173]:
              - generic [ref=e174]: Crear borrador
            - button "Validar" [disabled] [ref=e176]:
              - generic [ref=e177]: Validar
        - generic [ref=e179]:
          - generic [ref=e180]:
            - heading "Filtros read-only" [level=3] [ref=e181]
            - paragraph [ref=e182]: Consulta oficial GET-only de perfiles, camaras ACH Colombia/CENIT, records 1/5/6/7/8/9, variants y fields.
          - generic [ref=e183]:
            - generic [ref=e185]:
              - generic [ref=e186]: Buscar
              - textbox "Buscar" [ref=e187]:
                - /placeholder: Codigo o nombre
            - generic [ref=e188]:
              - generic [ref=e189]: Estado
              - generic [ref=e191]:
                - generic [ref=e192]:
                  - textbox "Estado Limpiar Todos" [ref=e193]:
                    - /placeholder: Buscar estado
                  - button "Limpiar" [ref=e194] [cursor=pointer]
                - button "Todos" [ref=e196] [cursor=pointer]:
                  - generic [ref=e197]: Todos
            - generic [ref=e198]:
              - generic [ref=e199]: Camara
              - generic [ref=e201]:
                - generic [ref=e202]:
                  - textbox "Camara Limpiar Todas" [ref=e203]:
                    - /placeholder: Buscar camara
                  - button "Limpiar" [ref=e204] [cursor=pointer]
                - button "Todas" [ref=e206] [cursor=pointer]:
                  - generic [ref=e207]: Todas
            - generic [ref=e208]:
              - generic [ref=e209]: Flujo
              - generic [ref=e211]:
                - generic [ref=e212]:
                  - textbox "Flujo Limpiar Todos" [ref=e213]:
                    - /placeholder: Buscar flujo
                  - button "Limpiar" [ref=e214] [cursor=pointer]
                - button "Todos" [ref=e216] [cursor=pointer]:
                  - generic [ref=e217]: Todos
        - generic [ref=e219]:
          - generic [ref=e222]:
            - generic [ref=e223]: search
            - searchbox [ref=e224]
          - paragraph [ref=e229]: Cargando información...
          - generic [ref=e230]:
            - generic [ref=e231]: 0 a 0 de 0. Página 0 de 0
            - treegrid [ref=e232]:
              - rowgroup [ref=e233]:
                - row "Codigo Nombre Camara Estado Version Variants" [ref=e234]:
                  - columnheader [ref=e235]:
                    - text: 
                    - text: 
                    - generic: 
                  - columnheader "Codigo" [ref=e236]:
                    - text: 
                    - generic [ref=e238] [cursor=pointer]: Codigo
                    - generic:    
                  - columnheader "Nombre" [ref=e239]:
                    - text: 
                    - generic [ref=e241] [cursor=pointer]: Nombre
                    - generic:    
                  - columnheader "Camara" [ref=e242]:
                    - text: 
                    - generic [ref=e244] [cursor=pointer]: Camara
                    - generic:    
                  - columnheader "Estado" [ref=e245]:
                    - text: 
                    - generic [ref=e247] [cursor=pointer]: Estado
                    - generic:    
                  - columnheader "Version" [ref=e248]:
                    - text: 
                    - generic [ref=e250] [cursor=pointer]: Version
                    - generic:    
                  - columnheader "Variants" [ref=e251]:
                    - text: 
                    - generic [ref=e253] [cursor=pointer]: Variants
                    - generic:    
                - row "Open Filter Menu Open Filter Menu Open Filter Menu Open Filter Menu Open Filter Menu Open Filter Menu" [ref=e254]:
                  - gridcell [ref=e255]
                  - gridcell "Open Filter Menu" [ref=e256]:
                    - textbox "Codigo Filter Input" [ref=e257]
                    - button "Open Filter Menu" [ref=e259] [cursor=pointer]: 
                  - gridcell "Open Filter Menu" [ref=e260]:
                    - textbox "Nombre Filter Input" [ref=e261]
                    - button "Open Filter Menu" [ref=e263] [cursor=pointer]: 
                  - gridcell "Open Filter Menu" [ref=e264]:
                    - textbox "Camara Filter Input" [ref=e265]
                    - button "Open Filter Menu" [ref=e267] [cursor=pointer]: 
                  - gridcell "Open Filter Menu" [ref=e268]:
                    - textbox "Estado Filter Input" [ref=e269]
                    - button "Open Filter Menu" [ref=e271] [cursor=pointer]: 
                  - gridcell "Open Filter Menu" [ref=e272]:
                    - textbox "Version Filter Input" [ref=e273]
                    - button "Open Filter Menu" [ref=e275] [cursor=pointer]: 
                  - gridcell "Open Filter Menu" [ref=e276]:
                    - textbox "Variants Filter Input" [ref=e277]
                    - button "Open Filter Menu" [ref=e279] [cursor=pointer]: 
              - rowgroup [ref=e280]
              - rowgroup
              - rowgroup [ref=e281]
              - rowgroup
              - generic [ref=e285]: Cargando...
            - generic [ref=e286]:
              - generic [ref=e287]:
                - generic [ref=e288]: "Page Size:"
                - combobox "Page Size" [ref=e289]:
                  - generic [ref=e290]: "15"
                  - generic [ref=e291] [cursor=pointer]: 
              - generic [ref=e292]: 0 a 0 de 0
              - button "First Page" [disabled] [ref=e293]: 
              - button "Previous Page" [disabled] [ref=e294]: 
              - generic [ref=e295]: Página 0 de 0
              - button "Next Page" [disabled] [ref=e296]: 
              - button "Last Page" [disabled] [ref=e297]: 
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
> 44  |     await expect(page.getByRole('button', { name: /Crear borrador|Publicar|Guardar|Eliminar|Archivar|Inactivar/i })).toHaveCount(0);
      |                                                                                                                      ^ Error: expect(locator).toHaveCount(expected) failed
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
  69  |     await expect(page.getByText('OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0')).toBeVisible();
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
```