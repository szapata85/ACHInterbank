import { expect, Page, test } from '@playwright/test';
import { mkdirSync } from 'node:fs';
import { resolve } from 'node:path';

const ui = (process.env['ACH_UI_URL'] ?? 'http://localhost:743').replace(/\/+$/, '');
const username = process.env['ACH_USER'] ?? process.env['E2E_ADMIN_USER'] ?? '';
const password = process.env['ACH_PASS'] ?? process.env['E2E_ADMIN_PASSWORD'] ?? '';
const evidenceDir = resolve(process.cwd(), '..', '..', 'docs', 'uat', 'evidencias', 'transactions-list');
mkdirSync(evidenceDir, { recursive: true });

test.describe.configure({ mode: 'serial' });
test.skip(!username || !password, 'ACH_USER/ACH_PASS o E2E_ADMIN_USER/E2E_ADMIN_PASSWORD son requeridos.');

test('valida filtros Material, errores, búsqueda, limpieza y resultados reales', async ({ page }, testInfo) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  const browserErrors = observeBrowserErrors(page);
  const transactionRequests: string[] = [];
  page.on('request', request => {
    const url = new URL(request.url());
    if (request.method() === 'GET' && url.pathname.toLowerCase() === '/api/transactions') {
      transactionRequests.push(url.toString());
    }
  });

  await authenticate(page);
  await page.goto(`${ui}/transactions/list`);
  await expect(page).toHaveURL(/\/transactions\/list(?:\?.*)?$/);
  await expect(page.getByTestId('transaction-list-page')).toBeVisible();
  await expect(
    page.getByTestId('transaction-list-page').getByRole('heading', { name: 'Transacciones', exact: true })
  ).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Filtros de consulta', exact: true })).toBeVisible();
  await expect(page.locator('mat-form-field')).toHaveCount(3);
  await expect(page.getByTestId('transaction-search')).toBeVisible();
  await expect(page.getByTestId('transaction-clear')).toBeVisible();
  await expect(page.getByTestId('transaction-results-summary')).toContainText(/transacciones encontradas/);

  const firstGrid = page.locator('ui-grilla-empresarial').first();
  await expect(firstGrid.locator('.ag-paging-panel')).toBeVisible();
  const idHeader = firstGrid.locator('[role="columnheader"][col-id="id"]');
  await idHeader.click();
  await expect(idHeader).toHaveAttribute('aria-sort', 'ascending');

  const firstTransactionRow = firstGrid.locator('.ag-center-cols-container .ag-row').first();
  const integrationResponse = page.waitForResponse(response =>
    response.request().method() === 'GET'
    && /\/api\/transactions\/\d+\/integration-result$/i.test(new URL(response.url()).pathname)
  );
  await firstTransactionRow.click();
  expect((await integrationResponse).ok(), 'La acción de fila debe consultar el resultado de integración.').toBeTruthy();
  await expect(page.locator('app-transaction-integration-result')).toBeVisible();
  await page.getByRole('button', { name: 'Cerrar resultado', exact: true }).click();
  await expect(page.locator('app-transaction-integration-result')).toHaveCount(0);

  const initialRequestCount = transactionRequests.length;
  const dateInput = page.getByTestId('transaction-filter-date');
  await dateInput.fill('fecha inválida');
  await page.getByTestId('transaction-search').click();

  await expect(page.getByText('Ingresa una fecha válida.', { exact: true })).toBeVisible();
  await expect(dateInput).toBeFocused();
  expect(transactionRequests.length, 'Una fecha inválida no debe consultar api/transactions.').toBe(initialRequestCount);

  await dateInput.fill('07/27/2026');
  await expect(page.getByText('Ingresa una fecha válida.', { exact: true })).toHaveCount(0);
  const validSearchResponse = page.waitForResponse(response =>
    response.request().method() === 'GET'
    && new URL(response.url()).pathname.toLowerCase() === '/api/transactions'
  );
  await page.getByTestId('transaction-search').click();
  expect((await validSearchResponse).ok(), 'La búsqueda válida debe responder correctamente.').toBeTruthy();
  await expect(page.getByTestId('transaction-results-summary')).toContainText(/transacciones encontradas/);

  const clearResponse = page.waitForResponse(response =>
    response.request().method() === 'GET'
    && new URL(response.url()).pathname.toLowerCase() === '/api/transactions'
  );
  await page.getByTestId('transaction-clear').click();
  expect((await clearResponse).ok(), 'Limpiar debe actualizar el listado.').toBeTruthy();
  await expect(dateInput).toHaveValue('');
  await expect(page.getByTestId('transaction-filter-clearing-house')).toContainText('Todas las cámaras');

  const resultSurface = page.locator('ui-grilla-empresarial, [data-testid="transactions-empty"]');
  expect(await resultSurface.count(), 'Debe renderizar la grilla o el estado vacío controlado.').toBeGreaterThan(0);
  await assertFilterPanelHasNoHorizontalOverflow(page);

  const screenshotPath = resolve(evidenceDir, 'transactions-list-desktop-1440x900.png');
  const sensitiveGrid = page.locator('ui-grilla-empresarial');
  await page.screenshot({
    path: screenshotPath,
    fullPage: true,
    mask: [sensitiveGrid],
    maskColor: '#e5e7eb'
  });
  await testInfo.attach('transactions-list-desktop-1440x900.png', {
    body: await page.screenshot({ fullPage: true, mask: [sensitiveGrid], maskColor: '#e5e7eb' }),
    contentType: 'image/png'
  });

  expect(
    browserErrors.filter(error => !/favicon|ResizeObserver/i.test(error)),
    `Errores no controlados: ${browserErrors.join(' | ')}`
  ).toEqual([]);
});

for (const viewport of [
  { name: 'laptop-1366x768', width: 1366, height: 768 },
  { name: 'tablet-768x1024', width: 768, height: 1024 },
  { name: 'mobile-390x844', width: 390, height: 844 }
]) {
  test(`mantiene filtros y acciones usables en ${viewport.name}`, async ({ page }, testInfo) => {
    await page.setViewportSize({ width: viewport.width, height: viewport.height });
    const browserErrors = observeBrowserErrors(page);

    await authenticate(page);
    await page.goto(`${ui}/transactions/list`);
    await expect(page.getByTestId('transaction-list-page')).toBeVisible();
    await expect(page.locator('mat-form-field')).toHaveCount(3);
    await expect(page.getByTestId('transaction-search')).toBeVisible();
    await expect(page.getByTestId('transaction-clear')).toBeVisible();
    await expect(page.getByTestId('transaction-results-summary')).toContainText(/transacciones encontradas/);
    await assertFilterPanelHasNoHorizontalOverflow(page);

    const screenshotName = `transactions-list-${viewport.name}.png`;
    const sensitiveGrid = page.locator('ui-grilla-empresarial');
    await page.screenshot({
      path: resolve(evidenceDir, screenshotName),
      fullPage: true,
      mask: [sensitiveGrid],
      maskColor: '#e5e7eb'
    });
    await testInfo.attach(screenshotName, {
      body: await page.screenshot({ fullPage: true, mask: [sensitiveGrid], maskColor: '#e5e7eb' }),
      contentType: 'image/png'
    });

    expect(
      browserErrors.filter(error => !/favicon|ResizeObserver/i.test(error)),
      `Errores no controlados en ${viewport.name}: ${browserErrors.join(' | ')}`
    ).toEqual([]);
  });
}

async function authenticate(page: Page): Promise<void> {
  await page.goto(`${ui}/login`);
  await page.getByLabel('Usuario').fill(username);
  await page.locator('input[formcontrolname="password"]').fill(password);
  const loginResponse = page.waitForResponse(response =>
    response.request().method() === 'POST'
    && new URL(response.url()).pathname.toLowerCase().endsWith('/auth/login')
  );
  await page.getByRole('button', { name: 'Ingresar', exact: true }).click();
  expect((await loginResponse).ok(), 'El login real debe responder correctamente.').toBeTruthy();
  await expect(page).not.toHaveURL(/\/login(?:\?.*)?$/);
}

function observeBrowserErrors(page: Page): string[] {
  const errors: string[] = [];
  page.on('console', message => {
    if (message.type() === 'error') {
      errors.push(`console:${message.text()}`);
    }
  });
  page.on('pageerror', error => errors.push(`page:${error.message}`));
  return errors;
}

async function assertFilterPanelHasNoHorizontalOverflow(page: Page): Promise<void> {
  const dimensions = await page.getByTestId('transaction-filter-panel').evaluate(element => ({
    scrollWidth: element.scrollWidth,
    clientWidth: element.clientWidth
  }));

  expect(
    dimensions.scrollWidth,
    `El formulario desborda horizontalmente: ${JSON.stringify(dimensions)}`
  ).toBeLessThanOrEqual(dimensions.clientWidth + 1);
}
