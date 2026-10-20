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

test('LIVE con datos: valida grilla, acciones, filtros y resultados reales', async ({ page }, testInfo) => {
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
  const initialTransactionsResponse = page.waitForResponse(response =>
    response.request().method() === 'GET'
    && new URL(response.url()).pathname.toLowerCase() === '/api/transactions'
  );
  await page.goto(`${ui}/transactions/list`);
  const liveResponse = await initialTransactionsResponse;
  expect(liveResponse.ok(), 'La consulta LIVE inicial de transacciones debe responder correctamente.').toBeTruthy();
  const liveTransactions = await liveResponse.json() as unknown;
  expect(
    Array.isArray(liveTransactions) && liveTransactions.length > 0,
    'Precondición LIVE faltante: /api/transactions no devolvió transacciones para validar grilla, fila e integración.'
  ).toBeTruthy();
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

  const grids = page.locator('ui-grilla-empresarial');
  await expect.poll(
    () => grids.count(),
    { message: 'Precondición LIVE faltante: la respuesta contiene datos, pero no se renderizó ninguna grilla.' }
  ).toBeGreaterThan(0);
  const firstGrid = grids.first();
  await expect(firstGrid.locator('.ag-paging-panel')).toBeVisible();
  const idHeader = firstGrid.locator('[role="columnheader"][col-id="id"]');
  await idHeader.click();
  await expect(idHeader).toHaveAttribute('aria-sort', 'ascending');

  const transactionRows = firstGrid.locator('.ag-center-cols-container .ag-row');
  await expect.poll(
    () => transactionRows.count(),
    { message: 'Precondición LIVE faltante: la primera grilla no contiene filas seleccionables.' }
  ).toBeGreaterThan(0);
  const firstTransactionRow = transactionRows.first();
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
  await expect(page.getByTestId('transaction-filter-clearing-house')).toHaveValue('');
  await expect(page.getByTestId('transaction-filter-cycle')).toHaveValue('');
  await expect(grids.first()).toBeVisible();
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

test('estado vacío controlado: conserva formulario utilizable y sin overflow', async ({ page }, testInfo) => {
  await page.setViewportSize({ width: 390, height: 844 });
  const browserErrors = observeBrowserErrors(page);
  let controlledTransactionQueries = 0;

  await authenticate(page);
  await page.route(/\/api\/transactions(?:\?.*)?$/i, async route => {
    if (route.request().method() !== 'GET') {
      await route.continue();
      return;
    }

    controlledTransactionQueries += 1;
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: '[]'
    });
  });

  await page.goto(`${ui}/transactions/list`);
  await expect(page).toHaveURL(/\/transactions\/list(?:\?.*)?$/);
  await expect(page.getByTestId('transactions-empty')).toBeVisible();
  await expect(page.getByTestId('transaction-results-summary')).toContainText('0 transacciones encontradas');
  expect(controlledTransactionQueries, 'El escenario debe controlar al menos una consulta de transacciones.').toBeGreaterThan(0);

  const clearingHouseInput = page.getByTestId('transaction-filter-clearing-house');
  await clearingHouseInput.fill('todas');
  await expect(page.getByRole('option', { name: 'Todas las cámaras', exact: true })).toBeVisible();
  await clearingHouseInput.press('ArrowDown');
  await clearingHouseInput.press('Enter');
  await expect(clearingHouseInput).toHaveValue('Todas las cámaras');

  const cycleInput = page.getByTestId('transaction-filter-cycle');
  await cycleInput.fill('todos');
  await expect(page.getByRole('option', { name: 'Todos los ciclos', exact: true })).toBeVisible();
  await cycleInput.press('ArrowDown');
  await cycleInput.press('Enter');
  await expect(cycleInput).toHaveValue('Todos los ciclos');

  await expect(page.getByTestId('transaction-search')).toBeVisible();
  await expect(page.getByTestId('transaction-search')).toBeEnabled();
  await expect(page.getByTestId('transaction-clear')).toBeVisible();
  await expect(page.getByTestId('transaction-clear')).toBeEnabled();

  const controlledSearchResponse = page.waitForResponse(response =>
    response.request().method() === 'GET'
    && new URL(response.url()).pathname.toLowerCase() === '/api/transactions'
  );
  await page.getByTestId('transaction-search').click();
  expect((await controlledSearchResponse).ok(), 'La búsqueda controlada debe responder correctamente.').toBeTruthy();
  await expect(page.getByTestId('transactions-empty')).toBeVisible();

  const controlledClearResponse = page.waitForResponse(response =>
    response.request().method() === 'GET'
    && new URL(response.url()).pathname.toLowerCase() === '/api/transactions'
  );
  await page.getByTestId('transaction-clear').click();
  expect((await controlledClearResponse).ok(), 'Limpiar debe volver a consultar el estado vacío controlado.').toBeTruthy();
  await expect(clearingHouseInput).toHaveValue('');
  await expect(cycleInput).toHaveValue('');
  await expect(page.getByTestId('transactions-empty')).toBeVisible();
  await assertFilterPanelHasNoHorizontalOverflow(page);

  const screenshotName = 'transactions-list-empty-controlled-mobile-390x844.png';
  await page.screenshot({
    path: resolve(evidenceDir, screenshotName),
    fullPage: true
  });
  await testInfo.attach(screenshotName, {
    body: await page.screenshot({ fullPage: true }),
    contentType: 'image/png'
  });

  expect(
    browserErrors.filter(error => !/favicon|ResizeObserver/i.test(error)),
    `Errores no controlados en el estado vacío: ${browserErrors.join(' | ')}`
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
