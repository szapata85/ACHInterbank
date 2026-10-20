import { expect, Page, test } from '@playwright/test';
import { mkdirSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';

const spa = (process.env['ACH_UI_URL'] ?? process.env['E2E_BASE_URL'] ?? 'http://localhost:743').replace(/\/+$/, '');
const api = (process.env['ACH_API_URL'] ?? process.env['E2E_API_BASE_URL'] ?? 'http://localhost:843').replace(/\/+$/, '');
const username = process.env['E2E_ADMIN_USER'] ?? process.env['ACH_USER'] ?? 'admin';
const password = process.env['E2E_ADMIN_PASSWORD'] ?? process.env['ACH_PASS'] ?? 'Admin123!';
const evidenceDir = resolve(process.cwd(), '../../docs/uat/evidencias/customer-third-parties');

test.describe('Terceros y prenotificaciones LIVE local controlado', () => {
  test('operador consulta estados de solo lectura y el API impide toda decisión manual', async ({ page }, testInfo) => {
    const browserErrors: string[] = [];
    page.on('pageerror', error => browserErrors.push(error.message));
    page.on('console', message => {
      if (message.type() === 'error') {
        browserErrors.push(message.text());
      }
    });

    const token = await login(page);
    await page.goto(`${spa}/customer-third-parties`);

    await expect(page.getByRole('heading', { name: 'Terceros y prenotificaciones' })).toBeVisible();
    await expect(page.getByTestId('prenotification-automatic-help')).toContainText('Ningún usuario aprueba o rechaza manualmente');
    await expect(page.getByTestId('third-party-filter-panel')).toBeVisible();
    await expect(page.getByTestId('third-party-filter-search')).toBeVisible();
    await expect(page.getByTestId('third-party-filter-account')).toBeVisible();
    await expect(page.getByTestId('third-party-filter-recipient')).toBeVisible();
    await expect(page.getByTestId('third-party-filter-status')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Buscar', exact: true })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Limpiar', exact: true })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Aprobar', exact: true })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Rechazar', exact: true })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Pendiente', exact: true })).toHaveCount(0);

    let searches = 0;
    const countSearch = (request: import('@playwright/test').Request) => {
      if (request.method() === 'GET' && new URL(request.url()).pathname === '/api/customer-third-parties') {
        searches++;
      }
    };
    page.on('request', countSearch);
    await page.getByTestId('third-party-filter-account').fill('cuenta-no-numérica');
    await page.getByRole('button', { name: 'Buscar', exact: true }).click();
    await expect(page.getByText('La cuenta destino debe contener solamente números.')).toBeVisible();
    expect(searches, 'El formulario inválido no debe consultar el API.').toBe(0);
    page.off('request', countSearch);

    await page.getByRole('button', { name: 'Limpiar', exact: true }).click();
    await expect(page.getByText('La cuenta destino debe contener solamente números.')).toHaveCount(0);
    await expect(page.getByTestId('third-party-results-summary')).toContainText(/tercero/);

    const listResponse = await page.request.get(`${api}/api/customer-third-parties?page=1&pageSize=20`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(listResponse.status()).toBe(200);
    const list = await listResponse.json() as { items?: Array<{ id: number; status: number | string }> };
    const first = list.items?.[0];
    if (first) {
      const beforeStatus = first.status;
      const forbidden = await page.request.patch(`${api}/api/customer-third-parties/${first.id}/status`, {
        headers: { Authorization: `Bearer ${token}` },
        data: { status: 1, validationMessage: 'Intento manual Playwright' }
      });
      expect([404, 405]).toContain(forbidden.status());

      const afterResponse = await page.request.get(`${api}/api/customer-third-parties?page=1&pageSize=20`, {
        headers: { Authorization: `Bearer ${token}` }
      });
      const after = await afterResponse.json() as { items?: Array<{ id: number; status: number | string }> };
      expect(after.items?.find(item => item.id === first.id)?.status).toBe(beforeStatus);

      const detailButton = page.getByTestId('third-party-view-detail').first();
      await expect(detailButton).toBeVisible();
      await detailButton.click();
      await expect(page.getByTestId('third-party-detail')).toContainText('Consulta de solo lectura');
      await expect(page.getByTestId('third-party-detail')).toContainText(/Cámara compensadora|Pendiente de asociación/);
      const detailEvidence = await page.screenshot({
        fullPage: true,
        mask: [page.locator('.ag-center-cols-container'), page.locator('.detail-panel dd')],
        maskColor: '#e2e8f0'
      });
      saveEvidence('customer-third-parties-detail.png', detailEvidence);
      await page.getByRole('button', { name: 'Cerrar detalle' }).click();
    }

    const unauthenticated = await page.request.get(`${api}/api/customer-third-parties`);
    expect(unauthenticated.status()).toBe(401);

    const desktopEvidence = await page.screenshot({
      fullPage: true,
      mask: [page.locator('.ag-center-cols-container')],
      maskColor: '#e2e8f0'
    });
    saveEvidence('customer-third-parties-desktop.png', desktopEvidence);
    await testInfo.attach('customer-third-parties-desktop.png', {
      body: desktopEvidence,
      contentType: 'image/png'
    });
    expect(browserErrors, `Errores no controlados: ${browserErrors.join(' | ')}`).toEqual([]);
  });

  for (const viewport of [
    { name: 'desktop-1440', width: 1440, height: 900 },
    { name: 'laptop-1366', width: 1366, height: 768 },
    { name: 'tablet-768', width: 768, height: 1024 },
    { name: 'mobile-390', width: 390, height: 844 }
  ]) {
    test(`responsive ${viewport.name} sin overflow del formulario`, async ({ page }, testInfo) => {
      await page.setViewportSize({ width: viewport.width, height: viewport.height });
      await login(page);
      await page.goto(`${spa}/customer-third-parties`);
      const panel = page.getByTestId('third-party-filter-panel');
      await expect(panel).toBeVisible();
      const dimensions = await panel.evaluate(element => ({
        clientWidth: element.clientWidth,
        scrollWidth: element.scrollWidth
      }));
      expect(dimensions.scrollWidth).toBeLessThanOrEqual(dimensions.clientWidth + 1);
      await expect(page.getByRole('button', { name: 'Buscar', exact: true })).toBeVisible();
      await expect(page.getByRole('button', { name: 'Limpiar', exact: true })).toBeVisible();

      if (viewport.name === 'mobile-390') {
        const mobileEvidence = await page.screenshot({
          fullPage: true,
          mask: [page.locator('.ag-center-cols-container')],
          maskColor: '#e2e8f0'
        });
        saveEvidence('customer-third-parties-mobile.png', mobileEvidence);
        await testInfo.attach('customer-third-parties-mobile.png', {
          body: mobileEvidence,
          contentType: 'image/png'
        });
      }
    });
  }
});

async function login(page: Page): Promise<string> {
  const response = await page.request.post(`${api}/auth/login`, {
    data: { username, password }
  });
  expect(response.status(), 'El login real debe responder 200.').toBe(200);
  const body = await response.json() as { data: { token: string } };

  await page.goto(`${spa}/login`);
  await page.locator('input[formControlName="username"]').fill(username);
  await page.locator('input[formControlName="password"]').fill(password);
  await Promise.all([
    page.waitForResponse(result =>
      new URL(result.url()).pathname.endsWith('/auth/login')
      && result.request().method() === 'POST'
      && result.status() === 200),
    page.getByRole('button', { name: 'Ingresar' }).click()
  ]);
  await expect(page).not.toHaveURL(/\/login$/);
  return body.data.token;
}

function saveEvidence(fileName: string, content: Buffer): void {
  mkdirSync(evidenceDir, { recursive: true });
  writeFileSync(resolve(evidenceDir, fileName), content);
}
