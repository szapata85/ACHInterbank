import { expect, Page, test, TestInfo } from '@playwright/test';
import { loginThroughUi } from './support/live-ui-auth';

const canonicalRoute = '/incoming-nacha-command-center';
const canonicalLabel = 'Seguimiento de archivos NACHA-M';

test.describe.serial('seguimiento NACHA-M con menú y runtime reales', () => {
  test('escritorio: abre la vista humanizada desde el menú dinámico', async ({ page }, testInfo) => {
    test.setTimeout(120_000);
    await page.setViewportSize({ width: 1440, height: 900 });
    const evidence = observeRuntime(page);

    await loginThroughUi(page);
    const sidenav = page.locator('mat-sidenav.primary-sidenav');
    const transactions = page.getByRole('button', { name: 'Transacciones', exact: true });
    if ((await transactions.getAttribute('aria-expanded')) !== 'true') await transactions.click();

    const canonicalLinks = sidenav.locator(`a[href="${canonicalRoute}"]`);
    await expect(canonicalLinks).toHaveCount(1);
    await expect(canonicalLinks.locator('.nav-label')).toHaveText(canonicalLabel);
    await expect(canonicalLinks.locator('app-ui-icon')).toHaveAttribute('data-icon-resolved', 'manage_search');
    await page.screenshot({ path: testInfo.outputPath('menu-seguimiento-nacha-m.png'), fullPage: true });

    const summaryResponse = page.waitForResponse((response) =>
      response.request().method() === 'GET'
      && new URL(response.url()).pathname === `${canonicalRoute}/observability/summary`);
    const listResponse = page.waitForResponse((response) =>
      response.request().method() === 'GET'
      && new URL(response.url()).pathname === `${canonicalRoute}/ingestions`);
    await canonicalLinks.click();

    expect((await summaryResponse).status()).toBe(200);
    expect((await listResponse).status()).toBe(200);
    await expect(page).toHaveURL(new RegExp(`${canonicalRoute}/?$`));
    await expect(page.getByRole('heading', { name: canonicalLabel, level: 1 })).toBeVisible();
    await expect(page.getByText(/Consulte el estado de validación, procesamiento y resultado/)).toBeVisible();
    await expect(page.getByRole('button', { name: /Actualizar información/ })).toBeVisible();
    await expect(page.getByRole('button', { name: /Aplicar filtros/ })).toBeVisible();
    await assertTechnicalScreenAbsent(page);
    await page.screenshot({ path: testInfo.outputPath('seguimiento-nacha-m-escritorio.png'), fullPage: true });

    const detailButtons = page.getByRole('button', { name: /^Ver detalle del archivo / });
    if (await detailButtons.count()) {
      await detailButtons.first().click();
      await expect(page).toHaveURL(new RegExp(`${canonicalRoute}/files/`));
      await expect(page.getByText('Progreso del archivo')).toBeVisible();
      await assertTechnicalScreenAbsent(page);
      await page.screenshot({ path: testInfo.outputPath('detalle-nacha-m-real.png'), fullPage: true });

      const directUrl = page.url();
      await page.goto(directUrl, { waitUntil: 'domcontentloaded' });
      await expect(page.getByText('Progreso del archivo')).toBeVisible();
    }

    expect(evidence.consoleErrors, JSON.stringify(evidence, null, 2)).toEqual([]);
    expect(evidence.requestFailures, JSON.stringify(evidence, null, 2)).toEqual([]);
    expect(evidence.httpErrors, JSON.stringify(evidence, null, 2)).toEqual([]);
    await attachEvidence(testInfo, evidence, 'escritorio');
  });

  test('móvil: conserva el menú, la ruta y el contenido esencial', async ({ page }, testInfo) => {
    test.setTimeout(120_000);
    await page.setViewportSize({ width: 390, height: 844 });
    const evidence = observeRuntime(page);

    await loginThroughUi(page);
    await page.locator('button.menu-toggle').click();
    const sidenav = page.locator('mat-sidenav.primary-sidenav');
    await expect(sidenav).toBeVisible();
    const transactions = page.getByRole('button', { name: 'Transacciones', exact: true });
    if ((await transactions.getAttribute('aria-expanded')) !== 'true') await transactions.click();
    const canonicalLink = sidenav.locator(`a[href="${canonicalRoute}"]`);
    await expect(canonicalLink).toHaveCount(1);
    await canonicalLink.click();

    await expect(page).toHaveURL(new RegExp(`${canonicalRoute}/?$`));
    await expect(page.getByRole('heading', { name: canonicalLabel, level: 1 })).toBeVisible();
    await expect(page.locator('mat-sidenav.primary-sidenav')).toBeHidden();
    await assertNoHorizontalDocumentScroll(page);
    await assertTechnicalScreenAbsent(page);
    await page.screenshot({ path: testInfo.outputPath('seguimiento-nacha-m-movil.png'), fullPage: true });

    expect(evidence.consoleErrors, JSON.stringify(evidence, null, 2)).toEqual([]);
    expect(evidence.requestFailures, JSON.stringify(evidence, null, 2)).toEqual([]);
    expect(evidence.httpErrors, JSON.stringify(evidence, null, 2)).toEqual([]);
    await attachEvidence(testInfo, evidence, 'móvil');
  });
});

async function assertTechnicalScreenAbsent(page: Page): Promise<void> {
  const forbidden = [
    'Command Center inbound NACHA',
    'Command Center Inbound NACHA-M',
    'AllowedActions',
    'idempotency key',
    'Cola dispatch inbound NACHA-M'
  ];
  for (const text of forbidden) await expect(page.getByText(text, { exact: true })).toHaveCount(0);
  await expect(page.getByText('Ingestas', { exact: true })).toHaveCount(0);
}

async function assertNoHorizontalDocumentScroll(page: Page): Promise<void> {
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth + 1)).toBe(true);
}

function observeRuntime(page: Page): RuntimeEvidence {
  const evidence: RuntimeEvidence = { consoleErrors: [], requestFailures: [], httpErrors: [], responses: [] };
  page.on('console', (message) => { if (message.type() === 'error') evidence.consoleErrors.push(message.text()); });
  page.on('pageerror', (error) => evidence.consoleErrors.push(error.message));
  page.on('requestfailed', (request) => {
    if (new URL(request.url()).origin === new URL(page.url() || 'http://localhost:743').origin) {
      evidence.requestFailures.push(`${request.method()} ${new URL(request.url()).pathname}`);
    }
  });
  page.on('response', (response) => {
    const url = new URL(response.url());
    if (url.pathname === '/api/navigation/menu' || url.pathname.startsWith(canonicalRoute)) {
      evidence.responses.push({ method: response.request().method(), path: url.pathname, status: response.status() });
      if (response.status() >= 400) evidence.httpErrors.push(`${response.request().method()} ${url.pathname} ${response.status()}`);
    }
  });
  return evidence;
}

async function attachEvidence(testInfo: TestInfo, evidence: RuntimeEvidence, viewport: string): Promise<void> {
  await testInfo.attach(`runtime-real-${viewport}.json`, {
    body: JSON.stringify({
      viewport,
      interceptedLogin: false,
      interceptedMenu: false,
      interceptedCommandCenter: false,
      ...evidence
    }, null, 2),
    contentType: 'application/json'
  });
}

type RuntimeEvidence = {
  consoleErrors: string[];
  requestFailures: string[];
  httpErrors: string[];
  responses: Array<{ method: string; path: string; status: number }>;
};
