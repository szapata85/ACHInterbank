import { expect, Page, test } from '@playwright/test';

const enabled = process.env['ACH_CORS_SMOKE_TESTS'] === 'true';
const uiBaseUrl = (process.env['ACH_UI_URL'] ?? 'http://localhost:743').replace(/\/+$/, '');
const username = process.env['ACH_USER'] ?? '';
const password = process.env['ACH_PASS'] ?? '';

test.describe.configure({ mode: 'serial' });
test.skip(!enabled, 'ACH_CORS_SMOKE_TESTS=true es requerido para el smoke local read-only.');
test.skip(!username || !password, 'ACH_USER y ACH_PASS deben suministrarse por el entorno.');

test('SPA navega sin errores CORS y consulta el panel del core sin ejecutar SOAP', async ({ page }, testInfo) => {
  test.setTimeout(120_000);
  const browserFailures: string[] = [];
  const brandingResponses: Array<{ status: number; allowOrigin?: string }> = [];

  page.on('console', message => {
    if (message.type() === 'error') browserFailures.push(`console:${message.text()}`);
  });
  page.on('pageerror', error => browserFailures.push(`page:${error.message}`));
  page.on('requestfailed', request => {
    browserFailures.push(`request:${request.method()} ${new URL(request.url()).pathname} ${request.failure()?.errorText ?? ''}`);
  });
  page.on('response', response => {
    if (new URL(response.url()).pathname.toLowerCase() === '/api/users/branding') {
      brandingResponses.push({
        status: response.status(),
        allowOrigin: response.headers()['access-control-allow-origin']
      });
    }
  });

  await loginThroughSpa(page);

  expect(brandingResponses.length, 'La SPA debe consultar branding desde la API real.').toBeGreaterThan(0);
  expect(brandingResponses.every(item => item.status === 200)).toBeTruthy();
  expect(brandingResponses.some(item => item.allowOrigin === uiBaseUrl)).toBeTruthy();
  await expect(page.getByRole('navigation', { name: /men.*principal/i })).toBeVisible();

  await page.goto(`${uiBaseUrl}/users`);
  await expect(page).toHaveURL(/\/users(?:\?.*)?$/);
  await expect(page.getByRole('main')).toBeVisible();

  await page.goto(`${uiBaseUrl}/transactions/list`);
  await expect(page).toHaveURL(/\/transactions\/list(?:\?.*)?$/);
  await expect(page.getByRole('heading', { name: /Transacciones/i }).first()).toBeVisible();

  const firstRow = page.locator('ui-grilla-empresarial .ag-row').first();
  if (await firstRow.count()) {
    await firstRow.click();
    await expect(page.locator('app-transaction-integration-result')).toBeVisible();
  }

  await testInfo.attach('cors-navigation-smoke.png', {
    body: await page.screenshot({
      fullPage: false,
      mask: [page.locator('.user-info'), page.locator('ui-grilla-empresarial')]
    }),
    contentType: 'image/png'
  });

  const corsFailures = browserFailures.filter(isCorsFailure);
  expect(corsFailures, `Errores de navegador detectados: ${corsFailures.join(' | ')}`).toEqual([]);
});

async function loginThroughSpa(page: Page): Promise<void> {
  await page.goto(`${uiBaseUrl}/login`);
  await expect(page.getByRole('heading', { name: /Ingreso al portal ACH Interbank/i })).toBeVisible();
  await page.getByLabel('Usuario').fill(username);
  await page.locator('input[formControlName="password"]').fill(password);
  await page.getByRole('button', { name: /^Ingresar$/ }).click();
  await expect(page).not.toHaveURL(/\/login(?:\?.*)?$/);
}

function isCorsFailure(value: string): boolean {
  return /blocked by cors policy|no ['"]access-control-allow-origin|net::err_failed|cors error/i.test(value);
}
