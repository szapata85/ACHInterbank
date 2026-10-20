import { expect, Page, test } from '@playwright/test';
import path from 'node:path';
import { loginThroughUi } from './support/live-ui-auth';

const evidenceDir = path.resolve(process.cwd(), '../../docs/uat/evidencias/prompt3-ui');

test.describe.serial('Operación ACH Material LIVE', () => {
  test('simulador desktop: catálogos, origen externo, fecha local y resultado trazable', async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 });
    const diagnostics = monitorPage(page);
    await loginThroughUi(page);
    await page.goto('/uat/nacha-inbound-simulator', { waitUntil: 'domcontentloaded' });

    await expect(page.getByRole('heading', { level: 1, name: 'Simulador NACHA-M de entrada' })).toBeVisible();
    await expect(page.getByText('UAT LOCAL').first()).toBeVisible();
    await expect(page.getByText(/IsDefaultSource = true/)).toBeVisible();

    const camera = page.locator('mat-select[formControlName="clearingHouseCode"]');
    await camera.click();
    await page.getByRole('option', { name: 'ACH Colombia', exact: true }).click();

    const businessDate = page.locator('input[formControlName="businessDate"]');
    await businessDate.fill('07/27/2026');
    await businessDate.press('Tab');

    const destinationName = (await page.locator('.destination-field strong').innerText()).trim();
    expect(destinationName).not.toBe('');
    expect(destinationName).not.toBe('No disponible');

    const origin = page.locator('mat-select[formControlName="originFinancialInstitutionId"]');
    await origin.click();
    const originOptions = page.getByRole('option');
    await expect(originOptions).not.toHaveCount(0);
    const optionLabels = (await originOptions.allInnerTexts()).map((value) => value.trim());
    expect(optionLabels.some((label) => label.includes(destinationName))).toBeFalsy();
    await originOptions.nth(1).click();

    const cycle = page.locator('mat-select[formControlName="cycleCode"]');
    await cycle.click();
    const cycleOptions = page.getByRole('option');
    await expect(cycleOptions).not.toHaveCount(0);
    await cycleOptions.first().click();

    await page.locator('input[formControlName="amount"]').fill('1234.56');
    await page.locator('input[formControlName="referencePrefix"]').fill(`P3-${Date.now()}`);
    await expect(page.locator('.profile-section mat-chip')).not.toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Generar archivo' })).toBeEnabled();
    await assertNoGlobalOverflow(page);
    await page.screenshot({ path: path.join(evidenceDir, 'simulador-desktop.png'), fullPage: true });

    let generateRequests = 0;
    page.on('request', (request) => {
      if (request.method() === 'POST'
        && new URL(request.url()).pathname.endsWith('/api/uat/nacha-inbound-simulator/generate')) {
        generateRequests += 1;
      }
    });

    await page.getByRole('button', { name: 'Generar archivo' }).click();
    await expect(page.getByRole('dialog')).toBeVisible();
    const generationResponse = page.waitForResponse((response) =>
      response.request().method() === 'POST'
      && new URL(response.url()).pathname.endsWith('/api/uat/nacha-inbound-simulator/generate')
    );
    await page.getByRole('dialog').getByRole('button', { name: 'Generar archivo' }).click();
    expect((await generationResponse).status()).toBe(201);

    const resultCard = page.locator('.result-card');
    await expect(resultCard.locator('mat-card-title')).toHaveText('Simulación generada');
    await expect(resultCard.getByText('ID de simulación', { exact: true })).toBeVisible();
    await expect(resultCard.getByText('SHA-256', { exact: true })).toBeVisible();
    expect(generateRequests).toBe(1);
    await page.screenshot({ path: path.join(evidenceDir, 'simulador-resultado.png'), fullPage: true });
    diagnostics.assertClean();
  });

  test('devoluciones desktop: filtros, grid y detalle sanitizado', async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 });
    const diagnostics = monitorPage(page);
    await loginThroughUi(page);
    await page.goto('/transactions/returns', { waitUntil: 'domcontentloaded' });

    await expect(page.getByRole('heading', { level: 1, name: 'Devoluciones ACH' })).toBeVisible();
    await expect(page.getByText('Prepara la consulta', { exact: true })).toBeVisible();

    const cycleSelect = page.locator('mat-select[formControlName="cycleId"]');
    await cycleSelect.click();
    await page.getByRole('option', { name: /Ciclo 1 · ACH Colombia · 27\/07\/2026/ }).click();

    const loadResponse = page.waitForResponse((response) =>
      response.request().method() === 'GET'
      && /\/ach-returns\/cycles\/[^/]+\/transactions$/.test(new URL(response.url()).pathname)
    );
    await page.getByRole('button', { name: 'Aplicar filtros' }).click();
    expect((await loadResponse).status()).toBe(200);
    await expect(page.locator('.ag-row')).toHaveCount(8);

    const firstSelectableCheckbox = page.locator('.ag-row')
      .filter({ hasText: /1[.,]500/ })
      .locator('input[type="checkbox"]:not([disabled])')
      .first();
    await firstSelectableCheckbox.click();
    await expect(page.getByRole('button', { name: 'Ver detalle' })).toBeEnabled();

    await page.getByRole('button', { name: 'Ver detalle' }).click();
    const detail = page.getByRole('dialog');
    await expect(detail.getByRole('heading', { name: 'Detalle de transacción retornable' })).toBeVisible();
    await expect(detail).not.toContainText('1234567890');
    await detail.screenshot({ path: path.join(evidenceDir, 'devoluciones-detalle.png') });
    await detail.getByRole('button', { name: 'Cerrar' }).click();

    await assertNoGlobalOverflow(page);
    await page.screenshot({ path: path.join(evidenceDir, 'devoluciones-desktop.png'), fullPage: true });
    await expect(page.getByRole('button', { name: 'Asignar causal y generar .RET' })).toBeEnabled();
    diagnostics.assertClean();
  });

  test('simulador móvil: formulario, resumen y controles sin overflow global', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    const diagnostics = monitorPage(page);
    await loginThroughUi(page);
    await page.goto('/uat/nacha-inbound-simulator', { waitUntil: 'domcontentloaded' });

    await page.locator('mat-select[formControlName="clearingHouseCode"]').click();
    await page.getByRole('option', { name: 'ACH Colombia', exact: true }).click();
    const businessDate = page.locator('input[formControlName="businessDate"]');
    await businessDate.fill('07/27/2026');
    await businessDate.press('Tab');
    await page.locator('mat-select[formControlName="originFinancialInstitutionId"]').click();
    await page.getByRole('option').nth(1).click();
    await page.locator('mat-select[formControlName="cycleCode"]').click();
    await page.getByRole('option').first().click();

    await expect(page.getByRole('button', { name: 'Generar archivo' })).toBeEnabled();
    await assertNoGlobalOverflow(page);
    await page.screenshot({ path: path.join(evidenceDir, 'simulador-movil.png'), fullPage: true });
    diagnostics.assertClean();
  });

  test('devoluciones móvil: filtros, grid y detalle dentro del viewport', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    const diagnostics = monitorPage(page);
    await loginThroughUi(page);
    await page.goto('/transactions/returns', { waitUntil: 'domcontentloaded' });

    await page.locator('mat-select[formControlName="cycleId"]').click();
    await page.getByRole('option', { name: /Ciclo 1 · ACH Colombia · 27\/07\/2026/ }).click();
    await page.getByRole('button', { name: 'Aplicar filtros' }).click();
    await expect(page.locator('.ag-row')).toHaveCount(8);

    await page.locator('ui-grilla-empresarial').scrollIntoViewIfNeeded();
    const checkbox = page.locator('.ag-row input[type="checkbox"]:not([disabled])').first();
    await checkbox.click();
    await page.getByRole('button', { name: 'Ver detalle' }).click();
    const detail = page.getByRole('dialog');
    await expect(detail).toBeVisible();
    await expect(detail).not.toContainText('1234567890');
    const dialogFitsViewport = await detail.evaluate((element) => {
      const rect = element.getBoundingClientRect();
      return rect.left >= 0
        && rect.top >= 0
        && rect.right <= window.innerWidth
        && rect.bottom <= window.innerHeight;
    });
    expect(dialogFitsViewport).toBeTruthy();
    await detail.getByRole('button', { name: 'Cerrar' }).click();

    await assertNoGlobalOverflow(page);
    await page.screenshot({ path: path.join(evidenceDir, 'devoluciones-movil.png'), fullPage: true });
    diagnostics.assertClean();
  });

  test('regresión focalizada: login, shell, menú y rutas cerradas', async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 });
    const diagnostics = monitorPage(page);
    await loginThroughUi(page);

    const sidenav = page.locator('mat-sidenav.primary-sidenav');
    const toggle = page.locator('button.menu-toggle');
    await expect(toggle).toHaveAttribute('aria-label', 'Contraer navegación');
    await toggle.click();
    await expect(toggle).toHaveAttribute('aria-label', 'Expandir navegación');
    await toggle.click();
    await expect(toggle).toHaveAttribute('aria-label', 'Contraer navegación');

    const routes = [
      '/audit-logs',
      '/auth-logs',
      '/catalogs/financial-institutions',
      '/catalogs/clearing-house-preferences',
      '/catalogs/bank-holidays',
      '/uat/nacha-inbound-simulator',
      '/transactions/returns'
    ];
    for (const route of routes) {
      await page.goto(route, { waitUntil: 'domcontentloaded' });
      await expect(page.getByRole('heading', { level: 1 }).first()).toBeVisible();
      await expect(page).toHaveURL(new RegExp(`${route.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}$`));
      await assertNoGlobalOverflow(page);
      await expect(page.locator('.mat-drawer-backdrop.mat-drawer-shown')).toHaveCount(0);
    }

    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto('/transactions/returns', { waitUntil: 'domcontentloaded' });
    await expect(sidenav).toHaveClass(/mat-drawer-over/);
    await expect(toggle).toHaveAttribute('aria-label', 'Abrir navegación');
    await toggle.click();
    await expect(page.locator('.mat-drawer-backdrop.mat-drawer-shown')).toBeVisible();
    await page.keyboard.press('Escape');
    await expect(sidenav).toBeHidden();
    await expect(page.locator('.mat-drawer-backdrop.mat-drawer-shown')).toHaveCount(0);
    diagnostics.assertClean();
  });
});

interface PageDiagnostics {
  assertClean(): void;
}

function monitorPage(page: Page): PageDiagnostics {
  const consoleErrors: string[] = [];
  const pageErrors: string[] = [];
  const failedResponses: string[] = [];

  page.on('console', (message) => {
    if (message.type() === 'error') {
      consoleErrors.push(message.text());
    }
  });
  page.on('pageerror', (error) => pageErrors.push(error.message));
  page.on('response', (response) => {
    if (response.status() >= 400) {
      failedResponses.push(`${response.status()} ${new URL(response.url()).pathname}`);
    }
  });

  return {
    assertClean(): void {
      expect(consoleErrors, 'La consola no debe registrar errores.').toEqual([]);
      expect(pageErrors, 'La página no debe lanzar excepciones.').toEqual([]);
      expect(failedResponses, 'La red no debe devolver respuestas 4xx/5xx.').toEqual([]);
    }
  };
}

async function assertNoGlobalOverflow(page: Page): Promise<void> {
  const hasOverflow = await page.evaluate(() =>
    document.documentElement.scrollWidth > document.documentElement.clientWidth + 1
  );
  expect(hasOverflow, 'La página no debe producir overflow horizontal global.').toBeFalsy();
}
