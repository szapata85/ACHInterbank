import { mkdirSync } from 'node:fs';
import { join } from 'node:path';
import { expect, test, type Page } from '@playwright/test';
import { assertNoFunctionalSpanglish, collectVisibleFunctionalText } from './support/nacha-ui-language';

const spaUrl = process.env['ACH_UI_URL'] ?? 'http://localhost:743';
const apiUrl = process.env['ACH_API_URL'] ?? 'http://localhost:843';
const adminUser = process.env['E2E_ADMIN_USER'] ?? process.env['ACH_USER'];
const adminPassword = process.env['E2E_ADMIN_PASSWORD'] ?? process.env['ACH_PASS'];
const exactPath = '/nacha-config-admin/variants-fields?profileId=1&recordCode=9&variantId=6&fieldId=48';
const evidenceDir = join(process.cwd(), '..', '..', 'docs', 'uat', 'evidence', 'job6-variants-fields');

const viewports = [
  { width: 1440, height: 900, name: 'desktop' },
  { width: 768, height: 1024, name: 'tablet' },
  { width: 390, height: 844, name: 'movil' }
];

test.describe.serial('NACHA Config variantes y campos LIVE', () => {
  test.beforeEach(async ({ page }) => {
    expect(adminUser, 'E2E_ADMIN_USER o ACH_USER es obligatorio.').toBeTruthy();
    expect(adminPassword, 'E2E_ADMIN_PASSWORD o ACH_PASS es obligatorio.').toBeTruthy();
    await login(page);
  });

  test('representa los datos persistidos y conserva la URL exacta al recargar', async ({ page }) => {
    test.setTimeout(60_000);
    const runtime = observeRuntime(page);
    const detailResponse = await openExactSelection(page);
    const detail = await detailResponse.json() as ProfileDetail;
    const variant = detail.variantes.find(item => item.id === 6);
    const field = variant?.fields.find(item => item.id === 48);

    expect(detail.id).toBe(1);
    expect(detail.profileCode).toBe('LEGACY_ACH_SALIDA_ORIGINAL_V1_0');
    expect(detail.records.some(item => item.recordCode === '9')).toBe(true);
    expect(variant).toMatchObject({
      id: 6,
      recordCode: '9',
      variantCode: 'LEGACY_R9_BASE',
      descripcion: 'Registro control de archivo NACHA-M (resumen general del archivo)'
    });
    expect(variant?.effectiveTo ?? null).toBeNull();
    expect(field).toMatchObject({
      id: 48,
      fieldCode: 'R9_BATCHCOUNT',
      fieldNameEs: 'BatchCount',
      startPosition: 2,
      length: 6,
      padChar: '0',
      justification: 'R',
      sortOrder: 2,
      isVisibleInBackoffice: true,
      propertyPath: 'BatchCount',
      sourceType: 'ENTIDAD',
      sourceTypeName: 'Entidad del dominio',
      reglas: []
    });
    expect(field?.formatMask ?? null).toBeNull();
    expect(field?.transformationPipelineJson ?? null).toBeNull();
    expect(field?.constantValue ?? null).toBeNull();
    expect(field?.entityName ?? null).toBeNull();

    await assertExactSelection(page);
    const root = page.getByTestId('nacha-config-variants-fields-page');
    const visiblePage = page.locator('body');
    await expect(root.getByRole('heading', { name: 'Campos por variante NACHA-M' })).toBeVisible();
    await expect(root.getByRole('heading', { name: 'Cantidad de lotes' })).toBeVisible();
    await expect(root).toContainText('Perfil heredado de salidas ACH');
    await expect(root).toContainText('Variante base del control de archivo');
    await expect(root.locator('.context-path')).toContainText('LEGACY_ACH_SALIDA_ORIGINAL_V1_0');
    await expect(root.locator('.context-path')).toContainText('Control de archivo');
    await expect(root.locator('.context-path')).toContainText('LEGACY_R9_BASE');
    await expect(root.locator('.context-path')).toContainText('R9_BATCHCOUNT');
    await expect(root.locator('.detail-panel')).toHaveCount(1);
    await expect(root.locator('.data-table')).toHaveCount(0);

    const source = root.locator('.semantic-value').filter({ hasText: 'Ruta técnica del dato' });
    await expect(source).toContainText('BatchCount');
    await expect(source).toContainText('Configurado');
    const sourceType = root.locator('.semantic-value').filter({ hasText: 'Tipo de origen' });
    await expect(sourceType).toContainText('Entidad del dominio');
    await expect(sourceType).toContainText('ENTIDAD');
    const alignment = root.locator('.semantic-value').filter({ hasText: 'Alineación' });
    await expect(alignment).toContainText('Alineación a la derecha');
    await expect(alignment).toContainText('Código persistido: R');
    const endPosition = root.locator('.semantic-value').filter({ hasText: 'Posición final' });
    await expect(endPosition).toContainText('7');
    await expect(endPosition).toContainText('Calculado por el sistema');
    const format = root.locator('.semantic-value').filter({ hasText: 'Máscara de formato' });
    await expect(format).toContainText('No aplicable');
    await expect(root.locator('.semantic-value[data-state="blocking"]')).toHaveCount(0);
    await expect(root).not.toContainText('N/D');
    await expect(root).not.toContainText('Sin configurar');

    await root.locator('.semantic-legend summary').click();
    await expect(root.locator('.legend-grid')).toContainText('Pendiente de configuración');
    await expect(root.locator('.legend-grid')).toContainText('Error bloqueante');

    await root.getByTestId('field-technical-details').locator('summary').click();
    await expect(root.getByTestId('field-technical-details')).toContainText('Código del campo');
    await expect(root.getByTestId('field-technical-details')).toContainText('R9_BATCHCOUNT');
    await expect(root.getByTestId('field-technical-details')).toContainText('Ruta técnica del dato');
    await expect(root.getByTestId('field-technical-details')).toContainText('BatchCount');

    await assertNoFunctionalSpanglish(visiblePage);

    await root.getByRole('tab', { name: 'Variante' }).click();
    await expect(root.getByTestId('variant-detail')).toContainText('Registro control de archivo NACHA-M');
    await expect(root.getByTestId('variant-detail')).toContainText('Vigencia abierta');
    await expect(root.getByTestId('variant-detail')).toContainText('Variante base del control de archivo');
    await assertNoFunctionalSpanglish(visiblePage);

    await root.getByRole('tab', { name: /^Reglas/ }).click();
    await expect(root.getByTestId('rules-detail')).toContainText('No aplicable');
    await expect(root.getByTestId('rules-detail')).toContainText('Este campo no requiere reglas adicionales');
    await assertNoFunctionalSpanglish(visiblePage);

    await test.info().attach('texto-visible-funcional.txt', {
      body: Buffer.from((await collectVisibleFunctionalText(visiblePage)).join('\n'), 'utf8'),
      contentType: 'text/plain'
    });

    await page.waitForLoadState('networkidle');
    const reloadResponsePromise = waitForProfileDetail(page);
    await page.reload({ waitUntil: 'domcontentloaded' });
    expect((await reloadResponsePromise).status()).toBe(200);
    await assertExactSelection(page);

    expect(runtime.consoleErrors).toEqual([]);
    expect(runtime.pageErrors).toEqual([]);
    expect(runtime.requestFailures).toEqual([]);
    expect(runtime.failedResponses).toEqual([]);
    expect(runtime.requestAborts.every(item =>
      /\/auth\/refresh|\/api\/navigation-(?:logs|menu)/.test(item)
    )).toBe(true);
  });

  for (const viewport of viewports) {
    test(`es utilizable en ${viewport.name} (${viewport.width}x${viewport.height})`, async ({ page }) => {
      test.setTimeout(60_000);
      const runtime = observeRuntime(page);
      await page.setViewportSize({ width: viewport.width, height: viewport.height });
      await openExactSelection(page);
      await assertExactSelection(page);
      await waitForStableLayout(page);

      const diagnostics = await page.evaluate(() => ({
        viewportWidth: document.documentElement.clientWidth,
        documentWidth: document.documentElement.scrollWidth,
        bodyWidth: document.body.scrollWidth
      }));
      expect(diagnostics.documentWidth).toBeLessThanOrEqual(diagnostics.viewportWidth + 1);
      expect(diagnostics.bodyWidth).toBeLessThanOrEqual(diagnostics.viewportWidth + 1);

      const root = page.getByTestId('nacha-config-variants-fields-page');
      await expect(root.locator('.detail-panel')).toBeVisible();
      await expect(root.locator('.master-item.is-selected')).toContainText('R9_BATCHCOUNT');
      await expect(root.locator('.master-item.is-selected')).toContainText('Cantidad de lotes');
      await expect(root.locator('.master-item.is-selected')).toContainText('BatchCount');
      await expect(root.getByRole('tab', { name: 'Campo', exact: true })).toHaveAttribute('aria-selected', 'true');
      await assertNoFunctionalSpanglish(page.locator('body'));

      mkdirSync(evidenceDir, { recursive: true });
      await page.screenshot({
        path: join(evidenceDir, `final-${viewport.name}-campo-${viewport.width}x${viewport.height}.png`),
        fullPage: true
      });

      if (viewport.name === 'desktop') {
        await root.getByRole('tab', { name: 'Variante', exact: true }).click();
        await page.evaluate(() => window.scrollTo(0, 0));
        await page.screenshot({
          path: join(evidenceDir, `final-${viewport.name}-variante-${viewport.width}x${viewport.height}.png`),
          fullPage: true
        });
        await root.getByRole('tab', { name: /^Reglas/ }).click();
        await page.evaluate(() => window.scrollTo(0, 0));
        await page.screenshot({
          path: join(evidenceDir, `final-${viewport.name}-reglas-${viewport.width}x${viewport.height}.png`),
          fullPage: true
        });
      }

      expect(runtime.consoleErrors).toEqual([]);
      expect(runtime.pageErrors).toEqual([]);
      expect(runtime.requestFailures).toEqual([]);
      expect(runtime.failedResponses).toEqual([]);
    });
  }
});

async function login(page: Page): Promise<void> {
  await page.goto(`${spaUrl}/login`, { waitUntil: 'domcontentloaded' });
  await page.locator('input[formControlName="username"]').fill(adminUser!);
  await page.locator('input[formControlName="password"]').fill(adminPassword!);
  const responsePromise = page.waitForResponse(response =>
    response.url().endsWith('/auth/login') && response.request().method() === 'POST'
  );
  await page.getByRole('button', { name: 'Ingresar' }).click();
  expect((await responsePromise).status()).toBe(200);
  await expect(page).not.toHaveURL(/\/login$/);

  const [live, ready] = await Promise.all([
    page.request.get(`${apiUrl}/health/live`),
    page.request.get(`${apiUrl}/health/ready`)
  ]);
  expect(live.status()).toBe(200);
  expect(ready.status()).toBe(200);
}

async function openExactSelection(page: Page) {
  const responsePromise = waitForProfileDetail(page);
  await page.goto(`${spaUrl}${exactPath}`, { waitUntil: 'domcontentloaded' });
  const response = await responsePromise;
  expect(response.status()).toBe(200);
  return response;
}

function waitForProfileDetail(page: Page) {
  return page.waitForResponse(response =>
    response.request().method() === 'GET'
    && response.url().endsWith('/nacha-config/perfiles/1')
  );
}

async function assertExactSelection(page: Page): Promise<void> {
  const root = page.getByTestId('nacha-config-variants-fields-page');
  await expect(root).toBeVisible();
  await expect(page.getByTestId('profile-selector')).toHaveValue('1');
  await expect(page.getByTestId('record-selector')).toHaveValue('9');
  await expect(page.getByTestId('variant-selector')).toHaveValue('6');
  await expect(root.locator('.master-item.is-selected')).toContainText('R9_BATCHCOUNT');
  await expect(root.locator('.master-item.is-selected')).toContainText('Cantidad de lotes');
  await expect(page).toHaveURL(`${spaUrl}${exactPath}`);
}

async function waitForStableLayout(page: Page): Promise<void> {
  await page.evaluate(async () => {
    await document.fonts.ready;
    const animations = document.getAnimations();
    await Promise.all(animations.map(animation => animation.finished.catch(() => undefined)));
  });
}

function observeRuntime(page: Page) {
  const consoleErrors: string[] = [];
  const pageErrors: string[] = [];
  const requestFailures: string[] = [];
  const requestAborts: string[] = [];
  const failedResponses: string[] = [];

  page.on('console', message => {
    if (message.type() === 'error') {
      consoleErrors.push(message.text());
    }
  });
  page.on('pageerror', error => pageErrors.push(error.message));
  page.on('requestfailed', request => {
    const failure = `${request.method()} ${request.url()} ${request.failure()?.errorText ?? ''}`.trim();
    if (request.failure()?.errorText === 'net::ERR_ABORTED') {
      requestAborts.push(failure);
    } else {
      requestFailures.push(failure);
    }
  });
  page.on('response', response => {
    if (response.status() >= 400) {
      failedResponses.push(`${response.status()} ${response.request().method()} ${response.url()}`);
    }
  });

  return { consoleErrors, pageErrors, requestFailures, requestAborts, failedResponses };
}

interface ProfileDetail {
  id: number;
  profileCode: string;
  records: Array<{ recordCode: string }>;
  variantes: Array<{
    id: number;
    recordCode: string;
    variantCode: string;
    descripcion?: string | null;
    effectiveFrom?: string | null;
    effectiveTo?: string | null;
    fields: Array<{
      id: number;
      fieldCode: string;
      fieldNameEs: string;
      startPosition: number;
      length: number;
      padChar?: string | null;
      justification?: string | null;
      formatMask?: string | null;
      sortOrder?: number | null;
      isVisibleInBackoffice?: boolean | null;
      transformationPipelineJson?: string | null;
      propertyPath?: string | null;
      sourceType?: string | null;
      sourceTypeName?: string | null;
      constantValue?: string | null;
      entityName?: string | null;
      reglas: unknown[];
    }>;
  }>;
}
