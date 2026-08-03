import { expect, Page, Response, test } from '@playwright/test';
import { loginThroughUi } from './support/live-ui-auth';

const route = '/transactions/outgoing-monitoring';

test.describe.serial('monitoreo real de transacciones de salida', () => {
  test('escritorio: menú, filtros, paginación y detalle comprobable', async ({ page }, testInfo) => {
    test.setTimeout(120_000);
    await page.setViewportSize({ width: 1440, height: 900 });
    const evidence = observe(page);
    await loginThroughUi(page);

    const transactions = page.getByRole('button', { name: 'Transacciones', exact: true });
    if ((await transactions.getAttribute('aria-expanded')) !== 'true') await transactions.click();
    const menuLink = page.locator(`mat-sidenav a[href="${route}"]`);
    await expect(menuLink).toHaveCount(1);
    const initialResponse = waitForMonitoringResponse(page);
    await menuLink.click();

    const firstQuery = await initialResponse;
    expect(firstQuery.status()).toBe(200);
    await waitForRenderedPage(page, firstQuery);
    await expect(page).toHaveURL(new RegExp(`${route}/?$`));
    await expect(page.getByTestId('outgoing-monitoring-page').getByRole('heading', { name: 'Monitoreo de transacciones de salida', level: 1 })).toBeVisible();
    await expect(page.getByText('Consulta operativa')).toBeVisible();
    await expect(page.getByRole('button', { name: /Buscar/ })).toBeVisible();
    await expect(page.getByRole('button', { name: /Limpiar filtros/ })).toBeVisible();

    await executeFilter(page, 'Cámara compensadora');
    await page.getByLabel('Ciclo').fill('CICLO-E2E-SIN-COINCIDENCIA');
    await executeSearch(page);
    await page.getByLabel('Ciclo').fill('');
    await page.getByLabel('Identificador').fill('TX-E2E');
    await executeSearch(page);
    await page.getByLabel('Identificador').fill('');
    await page.getByLabel('Número de seguimiento').fill('000000000000001');
    await executeSearch(page);
    const clearResponse = waitForMonitoringResponse(page);
    await page.getByRole('button', { name: /Limpiar filtros/ }).click();
    await waitForRenderedPage(page, await clearResponse);

    const nextPage = page.getByRole('button', { name: 'Página siguiente' });
    if (await nextPage.count() && await nextPage.isEnabled()) {
      const response = waitForMonitoringResponse(page);
      await nextPage.click();
      expect((await response).status()).toBe(200);
    }

    const details = page.getByRole('button', { name: /Ver detalle/ });
    if (await details.count()) {
      const detailResponse = page.waitForResponse(response => response.request().method() === 'GET'
        && /\/api\/transactions\/outgoing-monitoring\/\d+$/.test(new URL(response.url()).pathname));
      await details.first().click();
      const detailPayload = await detailResponse;
      expect(detailPayload.status()).toBe(200);
      const detail = await detailPayload.json() as {
        summary: { transactionExternalId: string; initialResultDisplayName: string; subsequentSituationDisplayName: string; maskedDestinationAccount: string };
        timeline: unknown[];
        files: Array<{ version?: number; hasTransmissionEvidence: boolean }>;
        returns: Array<{ causeCode?: string }>;
      };
      expect(detail.timeline.length).toBeGreaterThan(0);
      expect(detail.summary.maskedDestinationAccount).toMatch(/^\*+\d{4}$/);
      if (detail.summary.transactionExternalId === 'MON2-OUT-001') {
        expect(detail.summary.initialResultDisplayName).toBe('Aceptada');
        expect(detail.summary.subsequentSituationDisplayName).toBe('Devuelta posteriormente');
        expect(detail.files.map(file => file.version)).toEqual([1, 2]);
        expect(detail.files.every(file => !file.hasTransmissionEvidence)).toBe(true);
        expect(detail.returns.some(item => item.causeCode === 'R01')).toBe(true);
      }
      await expect(page).toHaveURL(new RegExp(`${route}/\\d+$`));
      await expect(page.getByRole('heading', { name: 'Línea de tiempo', level: 2 })).toBeVisible();
      const timeline = page.locator('[data-testid="outgoing-timeline"]');
      await timeline.scrollIntoViewIfNeeded();
      await expect(timeline.locator('li')).toHaveCount(detail.timeline.length);
      const body = await page.locator('body').innerText();
      expect(body).not.toContain('<Envelope');
      expect(body).not.toContain('<soap');
      expect(body).not.toContain('Reprocesar');
      expect(body).not.toContain('Aprobar');
      expect(body).not.toContain('Rechazar');

      const files = page.locator('[data-testid="outgoing-file"]');
      if (await files.count()) {
        await expect(files.first()).toContainText('Versión');
        const fileText = await files.first().innerText();
        if (fileText.includes('Sin evidencia de transmisión')) expect(fileText).not.toContain('Enviada');
      }
      const pageText = await page.getByTestId('outgoing-monitoring-detail').innerText();
      if (pageText.includes('Devuelta posteriormente')) {
        expect(pageText).toMatch(/Aceptada|Certificada/);
        expect(pageText).toMatch(/Causal contextual|Causal/);
      }
      await page.screenshot({ path: testInfo.outputPath('detalle-salida-real.png'), fullPage: true });
    } else {
      await expect(page.getByText(/No encontramos transacciones de salida/)).toBeVisible();
    }

    expect(evidence.apiErrors, JSON.stringify(evidence, null, 2)).toEqual([]);
    expect(evidence.consoleErrors, JSON.stringify(evidence, null, 2)).toEqual([]);
  });

  test('móvil: usa tarjetas sin desplazamiento horizontal', async ({ page }, testInfo) => {
    test.setTimeout(120_000);
    await page.setViewportSize({ width: 390, height: 844 });
    await loginThroughUi(page);
    const response = waitForMonitoringResponse(page);
    await page.goto(route);
    await response;
    await expect(page.getByTestId('outgoing-monitoring-page').getByRole('heading', { name: 'Monitoreo de transacciones de salida', level: 1 })).toBeVisible();
    await expect(page.locator('[data-testid="outgoing-mobile-list"]').or(page.getByText(/No encontramos transacciones de salida/))).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth + 1)).toBe(true);
    await page.screenshot({ path: testInfo.outputPath('monitoreo-salidas-movil.png'), fullPage: true });
  });
});

async function executeFilter(page: Page, label: string): Promise<void> {
  const select = page.locator('mat-form-field').filter({ hasText: label }).locator('mat-select');
  await select.click({ force: true });
  const options = page.getByRole('option');
  if (await options.count() > 1) await options.nth(1).click(); else await page.keyboard.press('Escape');
  await executeSearch(page);
  await select.click({ force: true });
  if (await options.count()) await options.first().click(); else await page.keyboard.press('Escape');
}

async function executeSearch(page: Page): Promise<void> {
  const button = page.getByRole('button', { name: /Buscar/ });
  await expect(button).toBeEnabled();
  const response = waitForMonitoringResponse(page);
  await button.click();
  const completed = await response;
  expect(completed.status()).toBe(200);
  await waitForRenderedPage(page, completed);
}

async function waitForRenderedPage(page: Page, response: Response): Promise<void> {
  const body = await response.json() as { items?: unknown[]; totalItems?: number };
  await expect(page.locator('.results-summary strong')).toHaveText(String(body.totalItems ?? 0));
  if ((body.items?.length ?? 0) > 0) {
    await expect(page.getByRole('button', { name: /Ver detalle/ })).toHaveCount(body.items!.length);
  } else {
    await expect(page.getByText(/No encontramos transacciones de salida/)).toBeVisible();
  }
}

function waitForMonitoringResponse(page: Page) {
  return page.waitForResponse(response => response.request().method() === 'GET'
    && new URL(response.url()).pathname === '/api/transactions/outgoing-monitoring');
}

function observe(page: Page) {
  const evidence = { consoleErrors: [] as string[], apiErrors: [] as string[] };
  page.on('console', message => { if (message.type() === 'error') evidence.consoleErrors.push(message.text()); });
  page.on('pageerror', error => evidence.consoleErrors.push(error.message));
  page.on('response', response => {
    const url = new URL(response.url());
    if (url.pathname.startsWith('/api/transactions/outgoing-monitoring') && response.status() >= 400)
      evidence.apiErrors.push(`${response.request().method()} ${url.pathname} ${response.status()}`);
  });
  return evidence;
}
