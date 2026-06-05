# Instructions

- Following Playwright test failed.
- Explain why, be concise, respect Playwright best practices.
- Provide a snippet of code with the fix, if possible.

# Test info

- Name: nacha-export-flow.spec.ts >> NACHA export flow from ACH cycles >> ExportFlow_ShouldNotRequestNachaExportWithHash
- Location: e2e/nacha-export-flow.spec.ts:17:7

# Error details

```
Error: expect(received).toBe(expected) // Object.is equality

Expected: true
Received: false
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
    - navigation [ref=e11]
    - generic [ref=e12]:
      - paragraph [ref=e13]: Perfil
      - generic [ref=e14]:
        - generic [ref=e15]: U
        - generic [ref=e16]:
          - generic [ref=e17]: Usuario UAT Export
          - generic [ref=e18]: Admin, ACH.Operator
      - button "Cerrar sesión" [ref=e19] [cursor=pointer]
  - generic [ref=e20]:
    - banner [ref=e21]:
      - generic [ref=e23]:
        - heading "Exportar NACHA-M" [level=1] [ref=e24]
        - navigation "Breadcrumbs" [ref=e25]:
          - link "Ciclos ACH" [ref=e26] [cursor=pointer]:
            - /url: /ach-cycles
          - generic [ref=e27]: /
          - generic [ref=e28]: Exportar NACHA
      - generic [ref=e29]:
        - generic [ref=e30]:
          - generic [ref=e31]: Usuario UAT Export
          - generic [ref=e32]: Admin, ACH.Operator
        - button "Salir" [ref=e33] [cursor=pointer]
    - main [ref=e34]:
      - generic [ref=e35]:
        - generic "Exportar archivos NACHA-M" [ref=e36]:
          - generic [ref=e37]:
            - generic [ref=e38]:
              - paragraph [ref=e39]: Descarga los archivos planos por ciclo ejecutado
              - heading "Exportar archivos NACHA-M" [level=2] [ref=e40]
            - link "Volver a ciclos" [ref=e42] [cursor=pointer]:
              - /url: /ach-cycles
        - generic [ref=e44]:
          - combobox [ref=e45]:
            - option "Todas las cámaras" [selected]
            - option "ACH Colombia"
          - generic [ref=e46]:
            - text: Desde
            - textbox "Desde" [ref=e47]
          - generic [ref=e48]:
            - text: Hasta
            - textbox "Hasta" [ref=e49]
          - button "Filtrar" [ref=e50] [cursor=pointer]
        - generic [ref=e53]:
          - generic [ref=e56]:
            - generic [ref=e57]: search
            - searchbox [ref=e58]
          - generic [ref=e59]:
            - treegrid [ref=e61]:
              - rowgroup [ref=e62]:
                - row "Ciclo Cámara Fecha efectiva Transacciones Exportable Acciones" [ref=e63]:
                  - columnheader "Ciclo" [ref=e64]:
                    - text: 
                    - generic [ref=e66] [cursor=pointer]: Ciclo
                    - generic:    
                  - columnheader "Cámara" [ref=e67]:
                    - text: 
                    - generic [ref=e69] [cursor=pointer]: Cámara
                    - generic:    
                  - columnheader "Fecha efectiva" [ref=e70]:
                    - text: 
                    - generic [ref=e72] [cursor=pointer]: Fecha efectiva
                    - generic:    
                  - columnheader "Transacciones" [ref=e73]:
                    - text: 
                    - generic [ref=e75] [cursor=pointer]: Transacciones
                    - generic:    
                  - columnheader "Exportable" [ref=e76]:
                    - text: 
                    - generic [ref=e78] [cursor=pointer]: Exportable
                    - generic:    
                  - columnheader "Acciones" [ref=e79]:
                    - text: 
                    - generic [ref=e81]: Acciones
                    - text: 
                    - generic: 
                - row "Open Filter Menu Open Filter Menu Open Filter Menu Open Filter Menu Open Filter Menu" [ref=e82]:
                  - gridcell "Open Filter Menu" [ref=e83]:
                    - textbox "Ciclo Filter Input" [ref=e84]
                    - button "Open Filter Menu" [ref=e86] [cursor=pointer]: 
                  - gridcell "Open Filter Menu" [ref=e87]:
                    - textbox "Cámara Filter Input" [ref=e88]
                    - button "Open Filter Menu" [ref=e90] [cursor=pointer]: 
                  - gridcell "Open Filter Menu" [ref=e91]:
                    - textbox "Fecha efectiva Filter Input" [ref=e92]
                    - button "Open Filter Menu" [ref=e94] [cursor=pointer]: 
                  - gridcell "Open Filter Menu" [ref=e95]:
                    - spinbutton "Transacciones Filter Input" [ref=e96]
                    - button "Open Filter Menu" [ref=e98] [cursor=pointer]: 
                  - gridcell "Open Filter Menu" [ref=e99]:
                    - textbox "Exportable Filter Input" [ref=e100]
                    - button "Open Filter Menu" [ref=e102] [cursor=pointer]: 
                  - gridcell [ref=e103]
              - rowgroup [ref=e104]:
                - row "Ciclo exportable ACH Colombia 25/05/2026 1 Disponible Generar archivo NACHA Generar con sobre digital" [ref=e105]:
                  - gridcell "Ciclo exportable" [ref=e106]
                  - gridcell "ACH Colombia" [ref=e107]
                  - gridcell "25/05/2026" [ref=e108]
                  - gridcell "1" [ref=e109]
                  - gridcell "Disponible" [ref=e110]
                  - gridcell "Generar archivo NACHA Generar con sobre digital" [ref=e111]:
                    - generic [ref=e113]:
                      - button "Generar archivo NACHA" [ref=e114] [cursor=pointer]
                      - button "Generar con sobre digital" [ref=e115] [cursor=pointer]
              - rowgroup
              - rowgroup [ref=e116]
              - rowgroup
            - text:    
```

# Test source

```ts
  1   | import { expect, Page, test } from '@playwright/test';
  2   | 
  3   | const exportPagePath = '/ach-cycles/nacha/export';
  4   | const exportableEndpoint = /\/ach-cycles\/exportable(?:\?.*)?$/;
  5   | const clearingHousesEndpoint = /\/clearing-houses(?:\?.*)?$/;
  6   | const authRefreshEndpoint = /\/auth\/refresh$/;
  7   | const hashExportPattern = /\/NachaExport\/[a-f0-9]{32,64}$/i;
  8   | const numericCycleExportPattern = /\/NachaExport\/\d+$/;
  9   | 
  10  | test.describe('NACHA export flow from ACH cycles', () => {
  11  |   test.beforeEach(async ({ page }) => {
  12  |     await seedAuthenticatedSession(page);
  13  |     await mockAuthRefresh(page);
  14  |     await mockClearingHouses(page);
  15  |   });
  16  | 
  17  |   test('ExportFlow_ShouldNotRequestNachaExportWithHash', async ({ page }) => {
  18  |     const exportRequests = captureExportRequests(page);
  19  |     await mockExportableCycles(page, [
  20  |       exportableCycle({ id: '8dbe5a2ce0da7c9eaff2a82d6a9e704c34ef77fa', cycleId: '42', cycleName: 'Ciclo exportable' })
  21  |     ]);
  22  |     await mockNachaExport(page);
  23  | 
  24  |     await page.goto(exportPagePath);
  25  |     await expect(page.getByText('Ciclo exportable')).toBeVisible();
  26  |     await page.getByRole('button', { name: 'Generar archivo NACHA' }).first().click();
  27  | 
  28  |     expect(exportRequests.some(url => hashExportPattern.test(url))).toBe(false);
> 29  |     expect(exportRequests.some(url => numericCycleExportPattern.test(url))).toBe(true);
      |                                                                             ^ Error: expect(received).toBe(expected) // Object.is equality
  30  |   });
  31  | 
  32  |   test('ExportFlow_ShouldNotRequestNachaExportForNonExportableRows', async ({ page }) => {
  33  |     const exportRequests = captureExportRequests(page);
  34  |     await mockExportableCycles(page, [
  35  |       exportableCycle({
  36  |         id: '1b12995d45906869e194e237f3db64bfd7e07d2f',
  37  |         cycleId: null,
  38  |         cycleName: 'Demo no exportable',
  39  |         isExportable: false,
  40  |         exportUnavailableReason: 'Registro demo no persistido.'
  41  |       })
  42  |     ]);
  43  |     await mockNachaExport(page);
  44  | 
  45  |     await page.goto(exportPagePath);
  46  |     await expect(page.getByText('Demo no exportable')).toBeVisible();
  47  |     const disabledAction = page.getByRole('button', { name: 'Generar archivo NACHA' }).first();
  48  |     await expect(disabledAction).toBeDisabled();
  49  |     await page.getByText('Demo no exportable').click();
  50  | 
  51  |     expect(exportRequests.some(url => url.includes('/NachaExport/'))).toBe(false);
  52  |   });
  53  | });
  54  | 
  55  | function captureExportRequests(page: Page): string[] {
  56  |   const exportRequests: string[] = [];
  57  | 
  58  |   page.on('request', request => {
  59  |     const url = request.url();
  60  |     if (url.includes('/NachaExport/') || url.includes('/ach-cycles/nacha/export')) {
  61  |       exportRequests.push(url);
  62  |     }
  63  |   });
  64  | 
  65  |   return exportRequests;
  66  | }
  67  | 
  68  | async function mockExportableCycles(page: Page, items: unknown[]): Promise<void> {
  69  |   await page.route(exportableEndpoint, async route => {
  70  |     await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(items) });
  71  |   });
  72  | }
  73  | 
  74  | async function mockClearingHouses(page: Page): Promise<void> {
  75  |   await page.route(clearingHousesEndpoint, async route => {
  76  |     await route.fulfill({
  77  |       status: 200,
  78  |       contentType: 'application/json',
  79  |       body: JSON.stringify([{ id: 1, name: 'ACH Colombia' }])
  80  |     });
  81  |   });
  82  | }
  83  | 
  84  | async function mockNachaExport(page: Page): Promise<void> {
  85  |   await page.route(/\/NachaExport\//, async route => {
  86  |     await route.fulfill({
  87  |       status: 200,
  88  |       contentType: 'text/plain',
  89  |       headers: { 'content-disposition': 'attachment; filename="test.ach"' },
  90  |       body: '1010000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000'
  91  |     });
  92  |   });
  93  | }
  94  | 
  95  | async function seedAuthenticatedSession(page: Page): Promise<void> {
  96  |   const token = createUnsignedJwt({
  97  |     unique_name: 'uat.export',
  98  |     name: 'Usuario UAT Export',
  99  |     uid: 'uat-export',
  100 |     role: ['Admin', 'ACH.Operator'],
  101 |     permission: ['CanReadAch', 'CanManageAch'],
  102 |     exp: Math.floor(Date.now() / 1000) + 3600,
  103 |     iat: Math.floor(Date.now() / 1000)
  104 |   });
  105 | 
  106 |   await page.addInitScript((accessToken) => {
  107 |     window.sessionStorage.setItem('ach.interbank.access_token', accessToken);
  108 |   }, token);
  109 | }
  110 | 
  111 | async function mockAuthRefresh(page: Page): Promise<void> {
  112 |   const token = createUnsignedJwt({
  113 |     unique_name: 'uat.export',
  114 |     name: 'Usuario UAT Export',
  115 |     uid: 'uat-export',
  116 |     role: ['Admin', 'ACH.Operator'],
  117 |     permission: ['CanReadAch', 'CanManageAch'],
  118 |     exp: Math.floor(Date.now() / 1000) + 3600,
  119 |     iat: Math.floor(Date.now() / 1000)
  120 |   });
  121 | 
  122 |   await page.route(authRefreshEndpoint, async route => {
  123 |     await route.fulfill({
  124 |       status: 200,
  125 |       contentType: 'application/json',
  126 |       body: JSON.stringify({
  127 |         sucess: true,
  128 |         data: {
  129 |           token,
```