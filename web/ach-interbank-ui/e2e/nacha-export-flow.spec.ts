import { expect, Page, test } from '@playwright/test';

const exportPagePath = '/ach-cycles/nacha/export';
const exportableEndpoint = /\/ach-cycles\/exportable(?:\?.*)?$/;
const clearingHousesEndpoint = /\/clearing-houses(?:\?.*)?$/;
const authRefreshEndpoint = /\/auth\/refresh$/;
const hashExportPattern = /\/NachaExport\/[a-f0-9]{32,64}$/i;
const numericCycleExportPattern = /\/NachaExport\/\d+$/;

test.describe('NACHA export flow from ACH cycles', () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthenticatedSession(page);
    await mockAuthRefresh(page);
    await mockClearingHouses(page);
  });

  test('ExportFlow_ShouldNotRequestNachaExportWithHash', async ({ page }) => {
    const exportRequests = captureExportRequests(page);
    await mockExportableCycles(page, [
      exportableCycle({ id: '8dbe5a2ce0da7c9eaff2a82d6a9e704c34ef77fa', cycleId: '42', cycleName: 'Ciclo exportable' })
    ]);
    await mockNachaExport(page);

    await page.goto(exportPagePath);
    await expect(page.getByText('Ciclo exportable')).toBeVisible();
        const exportButton = page.getByRole('row', { name: /Ciclo exportable/i }).getByRole('button', { name: 'Generar archivo NACHA' });
    await expect(exportButton).toBeVisible();
    await expect(exportButton).toBeEnabled();
    await Promise.all([
      page.waitForRequest(request => numericCycleExportPattern.test(request.url())),
      exportButton.click()
    ]);

    expect(exportRequests.some(url => hashExportPattern.test(url))).toBe(false);
    expect(exportRequests.some(url => numericCycleExportPattern.test(url))).toBe(true);
  });

  test('ExportFlow_ShouldNotRequestNachaExportForNonExportableRows', async ({ page }) => {
    const exportRequests = captureExportRequests(page);
    await mockExportableCycles(page, [
      exportableCycle({
        id: '1b12995d45906869e194e237f3db64bfd7e07d2f',
        cycleId: null,
        cycleName: 'Demo no exportable',
        isExportable: false,
        exportUnavailableReason: 'Registro demo no persistido.'
      })
    ]);
    await mockNachaExport(page);

    await page.goto(exportPagePath);
    await expect(page.getByText('Demo no exportable')).toBeVisible();
    const disabledAction = page.getByRole('row', { name: /Demo no exportable/i }).getByRole('button', { name: 'Generar archivo NACHA' });
    await expect(disabledAction).toBeDisabled();
    await page.getByText('Demo no exportable').click();

    expect(exportRequests.some(url => url.includes('/NachaExport/'))).toBe(false);
  });
});

function captureExportRequests(page: Page): string[] {
  const exportRequests: string[] = [];

  page.on('request', request => {
    const url = request.url();
    if (url.includes('/NachaExport/') || url.includes('/ach-cycles/nacha/export')) {
      exportRequests.push(url);
    }
  });

  return exportRequests;
}

async function mockExportableCycles(page: Page, items: unknown[]): Promise<void> {
  await page.route(exportableEndpoint, async route => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(items) });
  });
}

async function mockClearingHouses(page: Page): Promise<void> {
  await page.route(clearingHousesEndpoint, async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([{ id: 1, name: 'ACH Colombia' }])
    });
  });
}

async function mockNachaExport(page: Page): Promise<void> {
  await page.route(/\/NachaExport\//, async route => {
    await route.fulfill({
      status: 200,
      contentType: 'text/plain',
      headers: { 'content-disposition': 'attachment; filename="test.ach"' },
      body: '1010000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000'
    });
  });
}

async function seedAuthenticatedSession(page: Page): Promise<void> {
  const token = createUnsignedJwt({
    unique_name: 'uat.export',
    name: 'Usuario UAT Export',
    uid: 'uat-export',
    role: ['Admin', 'ACH.Operator'],
    permission: ['CanReadAch', 'CanManageAch'],
    exp: Math.floor(Date.now() / 1000) + 3600,
    iat: Math.floor(Date.now() / 1000)
  });

  await page.addInitScript((accessToken) => {
    window.sessionStorage.setItem('ach.interbank.access_token', accessToken);
  }, token);
}

async function mockAuthRefresh(page: Page): Promise<void> {
  const token = createUnsignedJwt({
    unique_name: 'uat.export',
    name: 'Usuario UAT Export',
    uid: 'uat-export',
    role: ['Admin', 'ACH.Operator'],
    permission: ['CanReadAch', 'CanManageAch'],
    exp: Math.floor(Date.now() / 1000) + 3600,
    iat: Math.floor(Date.now() / 1000)
  });

  await page.route(authRefreshEndpoint, async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        sucess: true,
        data: {
          token,
          username: 'uat.export',
          fullName: 'Usuario UAT Export',
          roles: ['Admin', 'ACH.Operator'],
          permissions: ['CanReadAch', 'CanManageAch']
        }
      })
    });
  });
}

function exportableCycle(overrides: Record<string, unknown>) {
  return {
    id: 'cycle-row',
    cycleId: '42',
    exportIdentifier: '42',
    cycleName: 'Ciclo exportable',
    processingDate: '2026-05-25T00:00:00Z',
    clearingHouseName: 'ACH Colombia',
    transactionCount: 1,
    isExportable: true,
    exportUnavailableReason: null,
    ...overrides
  };
}

function createUnsignedJwt(payload: Record<string, unknown>): string {
  return `${base64Url({ alg: 'none', typ: 'JWT' })}.${base64Url(payload)}.e2e`;
}

function base64Url(value: Record<string, unknown>): string {
  return Buffer.from(JSON.stringify(value))
    .toString('base64')
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=+$/g, '');
}

