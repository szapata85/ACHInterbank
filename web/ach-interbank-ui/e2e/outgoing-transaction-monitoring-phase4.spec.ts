import { expect, Page, test } from '@playwright/test';
import { resolve } from 'node:path';
import { loginThroughUi } from './support/live-ui-auth';

const route = '/transactions/outgoing-monitoring';
const evidenceDirectory = resolve(process.cwd(), '../../docs/uat/outgoing-monitoring-phase4/evidencias');
const evidenceProvider = process.env['E2E_PROVIDER'] ?? 'sqlserver';

test.describe.serial('Fase 4 del monitor de transacciones de salida en Docker', () => {
  test('representa los escenarios funcionales sin inventar hechos', async ({ page }) => {
    test.setTimeout(180_000);
    await page.setViewportSize({ width: 1440, height: 900 });
    await loginThroughUi(page);
    await page.waitForLoadState('networkidle');
    const observations = observe(page);
    await page.goto(route);
    await expect(page.getByTestId('outgoing-monitoring-page')
      .getByRole('heading', { name: 'Monitoreo de transacciones de salida', level: 1 })).toBeVisible();
    await expect(page.locator('mat-sidenav')).toContainText('Panel principal');
    await expect(page.locator('mat-sidenav')).not.toContainText('Dashboard');

    await assertScenario(page, 'UAT-F4-MON-SAL-01-FUTURO', ['Asignada a un ciclo futuro']);
    await assertScenario(page, 'UAT-F4-MON-SAL-02-PENDIENTE', ['Pendiente de respuesta de la cámara compensadora']);
    await assertScenario(page, 'UAT-F4-MON-SAL-03-ACEPTADA', ['Aceptada']);
    await assertScenario(page, 'UAT-F4-MON-SAL-04-RECHAZADA', ['Rechazada']);
    await assertScenario(page, 'UAT-F4-MON-SAL-05-DEVUELTA', ['Aceptada', 'Devuelta posteriormente']);
    await assertScenario(page, 'UAT-F4-MON-SAL-07-ERROR-TECNICO', ['Error técnico', 'No determinado']);

    await search(page, 'UAT-F4-MON-SAL-05-DEVUELTA');
    await page.getByRole('button', { name: /Ver detalle/ }).click();
    const timeline = page.getByTestId('outgoing-timeline');
    await expect(timeline).toContainText('Aceptada');
    await expect(timeline).toContainText('Devuelta por la entidad receptora');
    await expect(page.locator('body')).not.toContainText('<Envelope');
    await expect(page.locator('body')).not.toContainText('1234567890');
    await page.screenshot({ path: resolve(evidenceDirectory, `${evidenceProvider}-aceptada-devuelta-escritorio.png`), fullPage: true });

    expect(observations.errors).toEqual([]);
  });

  test('usa el código de respuesta y conserva la relación exacta con el archivo', async ({ page }) => {
    test.setTimeout(120_000);
    await loginThroughUi(page);
    await page.goto(route);
    await page.getByLabel('Código de respuesta').fill('r01');
    await executeSearch(page);
    await expect(visibleResults(page)).toContainText('UAT-F4-MON-SAL-04-RECHAZADA');

    const cleared = page.waitForResponse(item => item.request().method() === 'GET'
      && new URL(item.url()).pathname === '/api/transactions/outgoing-monitoring');
    await page.getByRole('button', { name: /Limpiar filtros/ }).click();
    expect((await cleared).status()).toBe(200);
    await search(page, 'UAT-F4-MON-SAL-11-ARCHIVO-EXACTO');
    await page.getByRole('button', { name: /Ver detalle/ }).click();
    const file = page.getByTestId('outgoing-file');
    await expect(file).toHaveCount(1);
    await expect(file).toContainText('UAT-F4-SALIDA.001');
    await expect(file).toContainText('Versión');
    await expect(file).toContainText('1');
    await expect(file).toContainText('Sin evidencia de transmisión');
    await expect(file).not.toContainText('UAT-F4-SALIDA.002');
  });

  for (const viewport of [
    { name: 'escritorio-reducido', width: 1280, height: 720 },
    { name: 'tableta', width: 768, height: 1024 },
    { name: 'movil', width: 390, height: 844 }
  ]) {
    test(`${viewport.name}: mantiene contenido accesible y sin desbordamiento`, async ({ page }) => {
      test.setTimeout(120_000);
      await page.setViewportSize({ width: viewport.width, height: viewport.height });
      await loginThroughUi(page);
      await page.waitForLoadState('networkidle');
      const observations = observe(page);
      await page.goto(route);
      await search(page, 'UAT-F4-MON-SAL-01-FUTURO');
      await expect(visibleResults(page)).toContainText('Asignada a un ciclo futuro');
      expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth + 1)).toBe(true);
      await page.screenshot({ path: resolve(evidenceDirectory, `${evidenceProvider}-${viewport.name}.png`), fullPage: true });
      expect(observations.errors).toEqual([]);
    });
  }
});

async function assertScenario(page: Page, identifier: string, expected: string[]): Promise<void> {
  await search(page, identifier);
  for (const value of expected) await expect(visibleResults(page)).toContainText(value);
}

function visibleResults(page: Page) {
  return page.locator('.desktop-table:visible, .mobile-cards:visible');
}

async function search(page: Page, identifier: string): Promise<void> {
  await page.getByLabel('Identificador').fill(identifier);
  await executeSearch(page);
}

async function executeSearch(page: Page): Promise<void> {
  const response = page.waitForResponse(item => item.request().method() === 'GET'
    && new URL(item.url()).pathname === '/api/transactions/outgoing-monitoring');
  await page.getByTestId('outgoing-search').click();
  expect((await response).status()).toBe(200);
  await expect(page.locator('.loading-state')).toHaveCount(0);
}

function observe(page: Page) {
  const errors: string[] = [];
  page.on('pageerror', error => errors.push(error.message));
  page.on('console', message => { if (message.type() === 'error') errors.push(message.text()); });
  page.on('response', response => { if (response.status() >= 500) errors.push(`${response.status()} ${new URL(response.url()).pathname}`); });
  page.on('requestfailed', request => {
    const path = new URL(request.url()).pathname;
    const reason = request.failure()?.errorText ?? 'solicitud fallida';
    if (!(reason === 'net::ERR_ABORTED' && /\.(?:woff2?|ttf|otf)$/i.test(path))) errors.push(`${reason} ${path}`);
  });
  return { errors };
}
