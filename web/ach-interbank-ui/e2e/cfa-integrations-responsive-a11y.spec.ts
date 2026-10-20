import AxeBuilder from '@axe-core/playwright';
import { expect, test, type Locator, type Page } from '@playwright/test';
import { CFA_BRANDING_CASES, installCfaIntegrationsMock } from './support/cfa-integrations-fixture';

const METHOD = 'Proc_Transacciones';
const VIEWPORTS = [
  { name: '360x800', width: 360, height: 800 },
  { name: '390x844', width: 390, height: 844 },
  { name: '768x1024', width: 768, height: 1024 },
  { name: '1024x768', width: 1024, height: 768 },
  { name: '1366x768', width: 1366, height: 768 },
  { name: '1440x900', width: 1440, height: 900 },
  { name: '1920x1080', width: 1920, height: 1080 }
] as const;

async function assertNoGlobalOverflow(page: Page, primarySelector: string): Promise<void> {
  const metrics = await page.evaluate((selector) => {
    const primary = document.querySelector<HTMLElement>(selector);
    const shell = document.documentElement;
    const body = document.body;
    return {
      shellClient: shell.clientWidth,
      shellScroll: shell.scrollWidth,
      bodyClient: body.clientWidth,
      bodyScroll: body.scrollWidth,
      primaryClient: primary?.clientWidth ?? 0,
      primaryScroll: primary?.scrollWidth ?? 0
    };
  }, primarySelector);

  expect(metrics.shellScroll, `shell overflow ${primarySelector}`).toBeLessThanOrEqual(metrics.shellClient + 2);
  expect(metrics.bodyScroll, `body overflow ${primarySelector}`).toBeLessThanOrEqual(metrics.bodyClient + 2);
  expect(metrics.primaryScroll, `primary overflow ${primarySelector}`).toBeLessThanOrEqual(metrics.primaryClient + 2);
}

async function assertNoNestedVerticalScroll(page: Page, primarySelector: string): Promise<void> {
  const nested = await page.evaluate((selector) => {
    const primary = document.querySelector<HTMLElement>(selector);
    if (!primary) return [] as string[];
    return Array.from(primary.querySelectorAll<HTMLElement>('*'))
      .filter(element => {
        if (element.tagName === 'TEXTAREA') return false;
        const style = getComputedStyle(element);
        const scrollable = style.overflowY === 'auto' || style.overflowY === 'scroll';
        return scrollable && element.scrollHeight > element.clientHeight + 2;
      })
      .map(element => `${element.tagName.toLowerCase()}.${element.className}`)
      .slice(0, 12);
  }, primarySelector);
  expect(nested, `scroll vertical anidado en ${primarySelector}`).toEqual([]);
}

async function assertCriticalTargets(page: Page, root: Locator): Promise<void> {
  const controls = root.locator([
    '[data-testid="service-contract-link"]',
    '[data-testid="service-mapping-link"]',
    '[data-testid="service-connection-link"]',
    '[data-testid="mapping-publication-review"]',
    '[data-testid="mapping-publication-cta"]',
    '[data-testid="mapping-preview-refresh"]',
    '[data-testid="soap-edit"]',
    '[data-testid="soap-save"]'
  ].join(','));

  for (let index = 0; index < await controls.count(); index += 1) {
    const control = controls.nth(index);
    if (!(await control.isVisible())) continue;
    const box = await control.boundingBox();
    expect(box?.height ?? 0, `target táctil ${index}`).toBeGreaterThanOrEqual(44);
    expect(box?.width ?? 0, `target táctil ${index}`).toBeGreaterThanOrEqual(44);
  }
}

async function assertDialogInsideViewport(page: Page, dialog: Locator): Promise<void> {
  await expect(dialog).toBeVisible();
  const box = await dialog.boundingBox();
  const viewport = page.viewportSize();
  expect(box).not.toBeNull();
  expect(viewport).not.toBeNull();
  if (!box || !viewport) return;
  expect(box.x).toBeGreaterThanOrEqual(-1);
  expect(box.y).toBeGreaterThanOrEqual(-1);
  expect(box.x + box.width).toBeLessThanOrEqual(viewport.width + 1);
  expect(box.y + box.height).toBeLessThanOrEqual(viewport.height + 1);
}

async function assertNoSeriousAxeViolations(page: Page): Promise<void> {
  const result = await new AxeBuilder({ page })
    .withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'])
    .analyze();
  const blocking = result.violations.filter(violation => violation.impact === 'critical' || violation.impact === 'serious');
  expect(blocking, blocking.map(item => `${item.id}: ${item.help}`).join('\n')).toEqual([]);
}

for (const viewport of VIEWPORTS) {
  test(`matriz responsive ${viewport.name}: centro, relación, contratos, stepper y conexión sin overflow/doble scroll`, async ({ page }) => {
    await page.setViewportSize({ width: viewport.width, height: viewport.height });
    await installCfaIntegrationsMock(page, { longContent: true });

    await page.goto('/integraciones');
    await expect(page.getByRole('heading', { level: 1, name: 'Centro de integración CFA' })).toBeVisible();
    await assertNoGlobalOverflow(page, '.integration-center');
    await assertNoNestedVerticalScroll(page, '.integration-center');
    if (viewport.width < 768) {
      await expect(page.locator('.integration-center table:visible')).toHaveCount(0);
      await assertCriticalTargets(page, page.locator('.integration-center'));
    }

    await page.goto(`/integraciones/relacion-informacion?method=${METHOD}`);
    await expect(page.getByRole('heading', { level: 1, name: 'Relación de información' })).toBeVisible();
    await assertNoGlobalOverflow(page, '[data-testid="integration-information-page"]');
    await assertNoNestedVerticalScroll(page, '[data-testid="integration-information-page"]');
    await expect(page.locator('[data-testid="integration-information-page"] table:visible')).toHaveCount(0);
    if (viewport.width < 768) await assertCriticalTargets(page, page.getByTestId('integration-information-page'));

    await page.getByTestId('mapping-publication-review').click();
    const publishDialog = page.getByRole('alertdialog', { name: /Confirmar publicación de configuración CFA/ });
    await assertDialogInsideViewport(page, publishDialog);
    await publishDialog.getByRole('button', { name: 'Cancelar' }).click();

    await page.goto(`/integraciones/contrato?method=${METHOD}`);
    await expect(page.getByRole('heading', { level: 1, name: 'Versiones contractuales' })).toBeVisible();
    await assertNoGlobalOverflow(page, '.contract-page');
    await assertNoNestedVerticalScroll(page, '.contract-page');
    await expect(page.locator('.contract-page table:visible')).toHaveCount(0);
    await page.getByRole('button', { name: 'Importar WSDL/XSD como borrador' }).click();
    await expect(page.locator('.import-flow')).toBeVisible();
    await assertNoGlobalOverflow(page, '.import-flow');
    await assertNoNestedVerticalScroll(page, '.import-flow');
    await expect(page.locator('.import-flow table:visible')).toHaveCount(0);
    await page.locator('.import-flow').getByRole('button', { name: 'Cancelar' }).click();

    await page.goto(`/integraciones/conexion?method=${METHOD}`);
    await expect(page.getByRole('heading', { level: 1, name: 'Conexión técnica avanzada' })).toBeVisible();
    await assertNoGlobalOverflow(page, '[data-testid="soap-settings-page"]');
    await assertNoNestedVerticalScroll(page, '[data-testid="soap-settings-page"]');
    await expect(page.locator('[data-testid="soap-settings-page"] table:visible')).toHaveCount(0);
    if (viewport.width < 768) await assertCriticalTargets(page, page.getByTestId('soap-settings-page'));
  });
}

test('keyboard-only smoke: foco visible, orden lógico, focus trap, Escape y restauración de foco', async ({ page }) => {
  await installCfaIntegrationsMock(page);
  await page.goto(`/integraciones/relacion-informacion?method=${METHOD}`);

  const opener = page.getByTestId('mapping-publication-review');
  await opener.focus();
  await expect(opener).toBeFocused();
  const focusStyle = await opener.evaluate(element => {
    const style = getComputedStyle(element);
    return { outline: style.outlineStyle, outlineWidth: style.outlineWidth, boxShadow: style.boxShadow };
  });
  expect(focusStyle.outline !== 'none' || focusStyle.outlineWidth !== '0px' || focusStyle.boxShadow !== 'none').toBeTruthy();

  await page.keyboard.press('Enter');
  const dialog = page.getByRole('alertdialog', { name: /Confirmar publicación de configuración CFA/ });
  await expect(dialog).toBeVisible();
  await expect.poll(() => page.evaluate(() => !!document.activeElement?.closest('[role="dialog"], [role="alertdialog"]'))).toBeTrue();

  for (let index = 0; index < 6; index += 1) {
    await page.keyboard.press('Tab');
    expect(await page.evaluate(() => !!document.activeElement?.closest('[role="dialog"], [role="alertdialog"]'))).toBeTrue();
  }

  await page.keyboard.press('Escape');
  await expect(dialog).toBeHidden();
  await expect(opener).toBeFocused();

  await page.keyboard.press('Tab');
  const activeName = await page.evaluate(() => (document.activeElement?.getAttribute('aria-label') || document.activeElement?.textContent || '').trim());
  expect(activeName.length).toBeGreaterThan(0);
});

test('semántica automatizable: headings, labels, aria-live, errores contextualizados y estado textual además de color', async ({ page }) => {
  await installCfaIntegrationsMock(page);
  await page.goto(`/integraciones/relacion-informacion?method=${METHOD}`);

  const headingLevels = await page.locator('main h1, main h2, main h3, main h4, main h5, main h6').evaluateAll(nodes =>
    nodes.filter(node => (node as HTMLElement).offsetParent !== null).map(node => Number(node.tagName.substring(1)))
  );
  expect(headingLevels[0]).toBe(1);
  for (let index = 1; index < headingLevels.length; index += 1) {
    expect(headingLevels[index] - headingLevels[index - 1]).toBeLessThanOrEqual(1);
  }

  await expect(page.getByLabel('Buscar por nombre, descripción o categoría')).toBeVisible();
  await expect(page.getByText('Estado', { exact: true }).first()).toBeVisible();
  await expect(page.locator('[aria-live="polite"]:visible')).not.toHaveCount(0);
  await expect(page.locator('[data-testid="mapping-parameter-item"] mat-chip').first()).not.toBeEmpty();

  const pending = page.getByTestId('mapping-parameter-item').filter({ hasText: 'Número de referencia' });
  const contextualIssue = pending.locator('.integration-information__validation-issues').first();
  await expect(contextualIssue).toBeVisible();
  await expect(contextualIssue).not.toBeEmpty();
  const supportDetail = pending.getByTestId('mapping-field-validation-technical-detail').first();
  await expect(supportDetail).toBeVisible();
  await expect(supportDetail).not.toHaveAttribute('open', '');

  await pending.getByTestId('mapping-edit-button').click();
  const sourceSearch = pending.getByTestId('mapping-source-search');
  const describedBy = await sourceSearch.getAttribute('aria-describedby');
  expect(describedBy).toBeTruthy();
  if (describedBy) await expect(page.locator(`#${describedBy}`)).toBeVisible();
});

test('zoom/reflow 200% mantiene shell y tarea primaria sin overflow global con textos largos', async ({ page }) => {
  await page.setViewportSize({ width: 1024, height: 768 });
  await installCfaIntegrationsMock(page, { longContent: true });
  await page.goto(`/integraciones/relacion-informacion?method=${METHOD}`);

  await page.evaluate(() => {
    document.documentElement.style.fontSize = '200%';
  });
  await assertNoGlobalOverflow(page, '[data-testid="integration-information-page"]');
  await assertNoNestedVerticalScroll(page, '[data-testid="integration-information-page"]');

  const longTechnical = page.getByText(/OFIDTX_TECHNICAL_IDENTIFIER_WITH_A_LONG_SUFFIX_FOR_REFLOW/).first();
  await expect(longTechnical).toBeVisible();
  const wrapping = await longTechnical.evaluate(element => {
    const style = getComputedStyle(element);
    return { overflowWrap: style.overflowWrap, whiteSpace: style.whiteSpace, scroll: (element as HTMLElement).scrollWidth, client: (element as HTMLElement).clientWidth };
  });
  expect(wrapping.overflowWrap !== 'normal' || wrapping.whiteSpace !== 'nowrap' || wrapping.scroll <= wrapping.client + 2).toBeTruthy();
});

for (const branding of CFA_BRANDING_CASES) {
  test(`branding ${branding.name}: contraste automatizado y textos largos sin truncamiento crítico`, async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await installCfaIntegrationsMock(page, { branding: branding.payload, longContent: true });
    await page.goto('/integraciones');

    await assertNoGlobalOverflow(page, '.integration-center');
    const functionalTitle = page.getByRole('heading', { name: /Créditos recibidos en CFA/ }).first();
    await expect(functionalTitle).toBeVisible();
    const titleMetrics = await functionalTitle.evaluate(element => {
      const style = getComputedStyle(element);
      return { textOverflow: style.textOverflow, whiteSpace: style.whiteSpace, scroll: (element as HTMLElement).scrollWidth, client: (element as HTMLElement).clientWidth };
    });
    expect(titleMetrics.textOverflow).not.toBe('ellipsis');
    expect(titleMetrics.whiteSpace).not.toBe('nowrap');
    expect(titleMetrics.scroll).toBeLessThanOrEqual(titleMetrics.client + 2);

    await assertNoSeriousAxeViolations(page);
  });
}

test('axe preparado para cero violaciones critical/serious en rutas principales CFA', async ({ page }) => {
  await installCfaIntegrationsMock(page);
  const routes = [
    { url: '/integraciones', heading: 'Centro de integración CFA' },
    { url: `/integraciones/relacion-informacion?method=${METHOD}`, heading: 'Relación de información' },
    { url: `/integraciones/contrato?method=${METHOD}`, heading: 'Versiones contractuales' },
    { url: `/integraciones/conexion?method=${METHOD}`, heading: 'Conexión técnica avanzada' }
  ];

  for (const route of routes) {
    await page.goto(route.url);
    await expect(page.getByRole('heading', { level: 1, name: route.heading })).toBeVisible();
    await assertNoSeriousAxeViolations(page);
  }
});

test('dialog de envío real conserva focus trap, Escape y restauración de foco', async ({ page }) => {
  await installCfaIntegrationsMock(page);
  await page.goto(`/integraciones/conexion?method=${METHOD}`);

  await page.getByTestId('soap-edit').click();
  await page.getByTestId('soap-operating-mode').click();
  await page.getByRole('option', { name: 'Envío real' }).click();
  const opener = page.getByTestId('soap-save');
  await opener.focus();
  await opener.click();

  const dialog = page.getByRole('alertdialog', { name: /Confirmar envío real para Créditos recibidos en CFA/ });
  await expect(dialog).toBeVisible();
  await expect.poll(() => page.evaluate(() => !!document.activeElement?.closest('[role="alertdialog"]'))).toBeTrue();
  for (let index = 0; index < 5; index += 1) {
    await page.keyboard.press('Tab');
    expect(await page.evaluate(() => !!document.activeElement?.closest('[role="alertdialog"]'))).toBeTrue();
  }
  await page.keyboard.press('Escape');
  await expect(dialog).toBeHidden();
  await expect(opener).toBeFocused();
});
