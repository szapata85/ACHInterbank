import { expect, Locator, Page, test } from '@playwright/test';

const spaBaseUrl = (
  process.env['E2E_BASE_URL']
  ?? process.env['ACH_UI_URL']
  ?? 'http://localhost:743'
).replace(/\/+$/, '');
const adminUser = process.env['E2E_ADMIN_USER'] ?? process.env['ACH_USER'] ?? 'admin';
const adminPassword =
  process.env['E2E_ADMIN_PASSWORD'] ?? process.env['ACH_PASS'] ?? 'Admin123!';
const evidenceDirectory = '../../docs/uat/evidencias/prompt2-ui';

interface RouteDefinition {
  path: string;
  heading: RegExp;
}

interface BrowserDiagnostics {
  consoleErrors: string[];
  pageErrors: string[];
  failedApiRequests: string[];
  failedHttpResponses: string[];
}

interface FinancialInstitutionResponse {
  name: string;
  isDefaultSource: boolean;
}

const scopedRoutes: RouteDefinition[] = [
  { path: '/audit-logs', heading: /Registro de auditoría/i },
  { path: '/auth-logs', heading: /Registro de autenticaciones/i },
  { path: '/catalogs/financial-institutions', heading: /Instituciones financieras/i },
  {
    path: '/catalogs/clearing-house-preferences',
    heading: /Prioridades por cámara compensadora/i
  },
  { path: '/catalogs/bank-holidays', heading: /Festivos bancarios/i }
];

test.describe.serial('Auditorías y catálogos Material', () => {
  test('valida filtros, formularios, tablas y diálogos en desktop', async ({ page }) => {
    await page.setViewportSize({ width: 1440, height: 900 });
    const diagnostics = monitorBrowser(page);
    await loginThroughUi(page);

    await openRoute(page, scopedRoutes[0]);
    await verifyAuditLog(page);
    await page.screenshot({
      path: `${evidenceDirectory}/audit-logs-desktop.png`,
      fullPage: true
    });

    await openRoute(page, scopedRoutes[1]);
    await verifyAuthLog(page);
    await page.screenshot({
      path: `${evidenceDirectory}/auth-logs-desktop.png`,
      fullPage: true
    });

    await openRoute(page, scopedRoutes[2]);
    await verifyFinancialInstitutions(page);
    await page.screenshot({
      path: `${evidenceDirectory}/financial-institutions-desktop.png`,
      fullPage: true
    });

    await openRoute(page, scopedRoutes[3]);
    await verifyClearingHousePreferences(page);
    await page.screenshot({
      path: `${evidenceDirectory}/clearing-house-preferences-desktop.png`,
      fullPage: true
    });

    await openRoute(page, scopedRoutes[4]);
    await verifyBankHolidays(page);
    await page.screenshot({
      path: `${evidenceDirectory}/bank-holidays-desktop.png`,
      fullPage: true
    });

    await assertNoGlobalOverflow(page, 'festivos en 1440x900');
    assertDiagnosticsAreClean(diagnostics);
  });

  for (const viewport of [
    { name: 'desktop compacto', width: 1280, height: 720 },
    { name: 'tablet', width: 768, height: 1024 }
  ]) {
    test(`mantiene las cinco rutas utilizables en ${viewport.name}`, async ({ page }) => {
      await page.setViewportSize({ width: viewport.width, height: viewport.height });
      const diagnostics = monitorBrowser(page);
      await loginThroughUi(page);

      if (viewport.width <= 768) {
        await assertMobileNavigationCycle(page);
      } else {
        await expect(navigationToggle(page)).toHaveAttribute(
          'aria-label',
          /^(Contraer|Expandir) navegación$/
        );
      }

      for (const route of scopedRoutes) {
        await openRoute(page, route);
        await assertShellLayout(page, `${route.path} en ${viewport.width}x${viewport.height}`);
        await assertNoGlobalOverflow(
          page,
          `${route.path} en ${viewport.width}x${viewport.height}`
        );
        await assertNoSensitiveOrBrokenText(page);
      }

      assertDiagnosticsAreClean(diagnostics);
    });
  }

  test('navega por el menú móvil y cierra el overlay en las cinco rutas', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    const diagnostics = monitorBrowser(page);
    await loginThroughUi(page);

    for (const route of scopedRoutes) {
      await navigateThroughMobileMenu(page, route);
      await assertShellLayout(page, `${route.path} en 390x844`);
      await assertNoGlobalOverflow(page, `${route.path} en 390x844`);
      await assertNoSensitiveOrBrokenText(page);

      if (route.path === '/audit-logs') {
        await page.screenshot({
          path: `${evidenceDirectory}/audit-logs-mobile.png`,
          fullPage: true
        });
      }

      if (route.path === '/catalogs/bank-holidays') {
        await page.screenshot({
          path: `${evidenceDirectory}/bank-holidays-mobile.png`,
          fullPage: true
        });
      }
    }

    assertDiagnosticsAreClean(diagnostics);
  });
});

async function verifyAuditLog(page: Page): Promise<void> {
  const form = page.getByRole('form', { name: 'Filtros del registro de auditoría' });
  const userFilter = form.getByRole('textbox', { name: 'Usuario' });
  await expect(form).toBeVisible();
  await expect(page.getByText('Prepara tu consulta', { exact: true })).toBeVisible();

  await userFilter.fill('filtro-sintetico-no-persistido');
  await form.getByRole('button', { name: 'Limpiar' }).click();
  await expect(userFilter).toHaveValue('');

  await form.getByRole('combobox', { name: 'Acción' }).click();
  await page.getByRole('option', { name: 'Modificado', exact: true }).click();
  const responsePromise = page.waitForResponse((response) => {
    const url = new URL(response.url());
    return response.request().method() === 'GET' && url.pathname === '/api/audit-logs';
  });
  await form.getByRole('button', { name: 'Buscar' }).click();
  expect((await responsePromise).status()).toBe(200);
  await expect(form.getByRole('button', { name: 'Buscar' })).toBeEnabled();

  const table = page.getByRole('table', { name: 'Registros de auditoría' });
  const emptyState = page.getByText('Sin resultados', { exact: true });
  await expect(table.or(emptyState)).toBeVisible();

  if (await table.isVisible()) {
    await expect(table.getByRole('columnheader', { name: 'Fecha' })).toBeVisible();
    await expect(table.getByRole('columnheader', { name: 'Usuario' })).toBeVisible();
    await expect(table.getByRole('columnheader', { name: 'Acción' })).toBeVisible();
    const detailCell = table.locator('.detail-cell').first();
    if (await detailCell.count()) {
      await expect(detailCell).toHaveAttribute('aria-label', /.+/);
    }
  }

  await assertNoSensitiveOrBrokenText(page);
  await form.getByRole('button', { name: 'Limpiar' }).click();
  await expect(page.getByText('Prepara tu consulta', { exact: true })).toBeVisible();
}

async function verifyAuthLog(page: Page): Promise<void> {
  const form = page.getByRole('form', { name: 'Filtros del registro de autenticaciones' });
  const userFilter = form.getByRole('textbox', { name: 'Usuario' });
  await expect(form).toBeVisible();
  await expect(page.getByText('Prepara tu consulta', { exact: true })).toBeVisible();

  await userFilter.fill('filtro-sintetico-no-persistido');
  await form.getByRole('combobox', { name: 'Resultado' }).click();
  await page.getByRole('option', { name: 'Exitoso', exact: true }).click();
  const responsePromise = page.waitForResponse((response) => {
    const url = new URL(response.url());
    return response.request().method() === 'GET' && url.pathname === '/api/auth-logs';
  });
  await form.getByRole('button', { name: 'Buscar' }).click();
  expect((await responsePromise).status()).toBe(200);
  await expect(form.getByRole('button', { name: 'Buscar' })).toBeEnabled();

  const table = page.getByRole('table', { name: 'Registros de autenticación' });
  const emptyState = page.getByText('Sin resultados', { exact: true });
  await expect(table.or(emptyState)).toBeVisible();

  if (await table.isVisible()) {
    await expect(table.getByRole('columnheader', { name: 'Resultado' })).toBeVisible();
    await expect(table.getByRole('columnheader', { name: 'Dirección IP' })).toBeVisible();
    const resultDetail = table.locator('.detail-cell').first();
    if (await resultDetail.count()) {
      await expect(resultDetail).toHaveAttribute('aria-label', /.+/);
    }
  }

  await assertNoSensitiveOrBrokenText(page);
  await form.getByRole('button', { name: 'Limpiar' }).click();
  await expect(userFilter).toHaveValue('');
  await expect(page.getByText('Prepara tu consulta', { exact: true })).toBeVisible();
}

async function verifyFinancialInstitutions(page: Page): Promise<void> {
  const reloadButton = page.getByRole('button', { name: 'Recargar' });
  await expect(reloadButton).toBeEnabled();
  await expect(page.getByRole('treegrid')).toBeVisible();
  const quickFilter = page.getByPlaceholder('Buscar institución por nombre, ruta o estado');
  await expect(quickFilter).toBeVisible();

  const institutionsResponsePromise = page.waitForResponse((response) => {
    const url = new URL(response.url());
    return response.request().method() === 'GET' && url.pathname === '/financial-institutions';
  });
  await reloadButton.click();
  const institutionsResponse = await institutionsResponsePromise;
  expect(institutionsResponse.status()).toBe(200);
  const responsePayload: unknown = await institutionsResponse.json();
  expect(Array.isArray(responsePayload), 'El endpoint debe conservar el contrato de lista').toBeTruthy();
  const institutions = responsePayload as FinancialInstitutionResponse[];
  const defaultSource = institutions.find((institution) => institution.isDefaultSource);
  expect(
    defaultSource,
    'El origen predeterminado debe resolverse exclusivamente por IsDefaultSource'
  ).toBeDefined();

  await quickFilter.fill(defaultSource!.name);
  await quickFilter.press('Enter');
  const rows = page.locator('.ag-center-cols-container .ag-row');
  expect(await rows.count(), 'El catálogo real debe contener instituciones').toBeGreaterThan(0);
  const defaultSourceRow = rows.filter({ hasText: defaultSource!.name }).first();
  await expect(defaultSourceRow).toBeVisible();
  await expect(defaultSourceRow.locator('[col-id="isDefaultSource"]')).toHaveText('Sí');
  await quickFilter.fill('');
  await quickFilter.press('Enter');

  const firstEditAction = page.getByRole('button', { name: /^Editar .+/ }).first();
  await expect(firstEditAction).toBeVisible();
  expect(await firstEditAction.evaluate((element) => element.tagName)).toBe('BUTTON');

  const toggleAction = page.getByRole('button', { name: /^(Activar|Desactivar) .+/ }).first();
  await expect(toggleAction).toBeVisible();
  await toggleAction.click();
  const dialog = page.getByRole('dialog');
  await expect(dialog).toBeVisible();
  await assertWithinViewport(dialog);
  await dialog.getByRole('button', { name: 'Cancelar' }).click();
  await expect(dialog).toBeHidden();

  await page.getByRole('button', { name: 'Nueva institución' }).click();
  const editor = page.locator('mat-card.editor-card');
  await expect(editor.getByRole('heading', { name: 'Nueva institución' })).toBeVisible();
  await expect(editor.getByRole('button', { name: 'Guardar' })).toBeDisabled();
  await editor.getByRole('textbox', { name: 'Nombre' }).fill(' ');
  await editor.getByRole('textbox', { name: 'Nombre' }).blur();
  await expect(editor.getByText(/carácter visible/i)).toBeVisible();
  await resetGridHorizontalScroll(page);
  await assertNoGlobalOverflow(page, 'formulario de instituciones en desktop');
}

async function verifyClearingHousePreferences(page: Page): Promise<void> {
  await expect(page.getByRole('button', { name: 'Recargar' })).toBeEnabled();
  await expect(page.getByRole('button', { name: 'Nueva relación' })).toBeEnabled();
  await expect(page.getByRole('treegrid')).toBeVisible();
  await expect(
    page.getByPlaceholder('Buscar por institución, cámara, prioridad o estado')
  ).toBeVisible();

  const safeGridAction = page.locator('button[data-action]').first();
  if (await safeGridAction.count()) {
    expect(await safeGridAction.evaluate((element) => element.tagName)).toBe('BUTTON');
  }

  const deleteAction = page.locator('button[data-action="delete"]').first();
  if (await deleteAction.count()) {
    await deleteAction.click();
    const dialog = page.getByRole('dialog', { name: 'Eliminar relación' });
    await expect(dialog).toBeVisible();
    await assertWithinViewport(dialog);
    await dialog.getByRole('button', { name: 'Cancelar' }).click();
    await expect(dialog).toBeHidden();
  }

  await page.getByRole('button', { name: 'Nueva relación' }).click();
  const editor = page.locator('mat-card.editor-card');
  await expect(editor.getByRole('heading', { name: 'Nueva relación' })).toBeVisible();
  await expect(editor.getByRole('button', { name: 'Crear relación' })).toBeDisabled();

  const institutionSelect = editor.getByRole('combobox', { name: 'Institución' });
  await institutionSelect.click();
  const institutionOptions = page.locator('mat-option:not([aria-disabled="true"])');
  expect(
    await institutionOptions.count(),
    'Las instituciones deben provenir del catálogo real'
  ).toBeGreaterThan(0);
  await page.keyboard.press('Escape');

  const clearingHouseSelect = editor.getByRole('combobox', {
    name: 'Cámara compensadora'
  });
  await clearingHouseSelect.click();
  const clearingHouseOptions = page.locator('mat-option:not([aria-disabled="true"])');
  expect(
    await clearingHouseOptions.count(),
    'Las cámaras deben cargarse dinámicamente'
  ).toBeGreaterThan(0);
  await page.keyboard.press('Escape');
  await resetGridHorizontalScroll(page);
  await assertNoGlobalOverflow(page, 'formulario de preferencias en desktop');
}

async function verifyBankHolidays(page: Page): Promise<void> {
  const yearInput = page.getByRole('spinbutton', { name: 'Año' });
  await expect(yearInput).toBeVisible();
  await expect(page.getByRole('button', { name: 'Buscar' })).toBeEnabled();

  await yearInput.fill('2025');
  const responsePromise = page.waitForResponse((response) => {
    const url = new URL(response.url());
    return (
      response.request().method() === 'GET'
      && url.pathname === '/bank-holidays'
      && url.searchParams.get('year') === '2025'
    );
  });
  await page.getByRole('button', { name: 'Buscar' }).click();
  expect((await responsePromise).status()).toBe(200);
  await expect(page.getByRole('button', { name: 'Buscar' })).toBeEnabled();

  const deleteAction = page.getByRole('button', { name: /^Eliminar festivo / }).first();
  if (await deleteAction.count()) {
    await deleteAction.click();
    const dialog = page.getByRole('dialog', { name: 'Eliminar festivo' });
    await expect(dialog).toBeVisible();
    await assertWithinViewport(dialog);
    await dialog.getByRole('button', { name: 'Cancelar' }).click();
    await expect(dialog).toBeHidden();
  }

  await page.getByRole('button', { name: 'Nuevo festivo' }).click();
  const editor = page.locator('mat-card.editor-card');
  await expect(editor.getByText('Nuevo festivo', { exact: true })).toBeVisible();
  const saveButton = editor.getByRole('button', { name: 'Guardar' });
  await expect(saveButton).toBeDisabled();

  const dateInput = editor.getByRole('textbox', { name: 'Fecha' });
  await dateInput.fill('12/31/2026');
  await dateInput.blur();
  await editor.getByRole('textbox', { name: 'Descripción' }).fill(
    'Festivo sintético no guardado'
  );
  await editor.getByRole('textbox', { name: 'País' }).fill('CO');
  await expect(saveButton).toBeEnabled();
  await expect(dateInput).toHaveValue(/^(31\/12\/2026|12\/31\/2026)$/);

  await dateInput.fill('01/01/2027');
  await dateInput.blur();
  await expect(dateInput).toHaveValue(/^0?1\/0?1\/2027$/);
  await expect(saveButton).toBeEnabled();
  await assertNoGlobalOverflow(page, 'formulario de festivos en desktop');
}

async function loginThroughUi(page: Page): Promise<void> {
  await page.goto(`${spaBaseUrl}/login`, { waitUntil: 'domcontentloaded' });
  await page.locator('input[formControlName="username"]').fill(adminUser);
  await page.locator('input[formControlName="password"]').fill(adminPassword);

  const loginResponsePromise = page.waitForResponse((response) => {
    const url = new URL(response.url());
    return response.request().method() === 'POST' && url.pathname === '/auth/login';
  });
  await page.getByRole('button', { name: 'Ingresar' }).click();
  expect((await loginResponsePromise).status()).toBe(200);
  await expect(page).not.toHaveURL(/\/login(?:\?.*)?$/);
  await expect(page.getByRole('main')).toBeVisible();
}

async function openRoute(page: Page, route: RouteDefinition): Promise<void> {
  const response = await page.request.get(`${spaBaseUrl}${route.path}`);
  expect(response.status(), `${route.path} debe responder HTML 200`).toBe(200);
  expect(response.headers()['content-type']).toContain('text/html');
  await page.evaluate((path) => {
    window.history.pushState({}, '', path);
    window.dispatchEvent(new PopStateEvent('popstate'));
  }, route.path);
  await expect(page).toHaveURL(new RegExp(`${escapeRegExp(route.path)}(?:[?#].*)?$`));
  await expect(page.getByRole('main')).toBeVisible();
  await expect(page.getByRole('heading', { name: route.heading }).first()).toBeVisible();
  await waitForRouteReady(page, route.path);
}

async function waitForRouteReady(page: Page, path: string): Promise<void> {
  if (path === '/catalogs/financial-institutions') {
    await expect(page.getByRole('button', { name: 'Recargar' })).toBeEnabled();
    return;
  }

  if (path === '/catalogs/clearing-house-preferences') {
    await expect(page.getByRole('button', { name: 'Nueva relación' })).toBeEnabled();
    return;
  }

  if (path === '/catalogs/bank-holidays') {
    await expect(page.getByRole('button', { name: 'Buscar' })).toBeEnabled();
  }
}

async function navigateThroughMobileMenu(page: Page, route: RouteDefinition): Promise<void> {
  const toggle = navigationToggle(page);
  await expect(toggle).toBeVisible();
  await expect(toggle).toHaveAttribute('aria-label', 'Abrir navegación');
  await toggle.click();

  const navigation = page.getByRole('navigation', { name: 'Menú principal', exact: true });
  await expect(navigation).toBeVisible();
  await expect(toggle).toHaveAttribute('aria-label', 'Cerrar navegación');
  await expandAllMenuGroups(navigation);

  const routeLink = navigation.locator(`a[href="${route.path}"]`);
  await expect(routeLink).toBeVisible();
  await routeLink.click();
  await expect(page).toHaveURL(new RegExp(`${escapeRegExp(route.path)}(?:[?#].*)?$`));
  await expect(navigation).toBeHidden();
  await expect(toggle).toHaveAttribute('aria-label', 'Abrir navegación');
  await expect(page.locator('.mat-drawer-backdrop.mat-drawer-shown')).toHaveCount(0);
  await expect(page.getByRole('heading', { name: route.heading }).first()).toBeVisible();
  await waitForRouteReady(page, route.path);
}

async function expandAllMenuGroups(navigation: Locator): Promise<void> {
  for (let expanded = 0; expanded < 40; expanded += 1) {
    const collapsedGroup = navigation.locator(
      'button.nav-parent[aria-expanded="false"]'
    ).first();
    if ((await collapsedGroup.count()) === 0) {
      return;
    }
    await collapsedGroup.click();
  }

  expect(
    await navigation.locator('button.nav-parent[aria-expanded="false"]').count(),
    'Todos los grupos del menú deben poder expandirse'
  ).toBe(0);
}

async function assertMobileNavigationCycle(page: Page): Promise<void> {
  const toggle = navigationToggle(page);
  await expect(toggle).toBeVisible();
  await expect(toggle).toHaveAttribute('aria-label', 'Abrir navegación');
  await toggle.click();

  const navigation = page.getByRole('navigation', { name: 'Menú principal', exact: true });
  await expect(navigation).toBeVisible();
  await expect(toggle).toHaveAttribute('aria-label', 'Cerrar navegación');
  await navigation.focus();
  await page.keyboard.press('Escape');
  await expect(navigation).toBeHidden();
  await expect(toggle).toHaveAttribute('aria-label', 'Abrir navegación');
  await expect(page.locator('.mat-drawer-backdrop.mat-drawer-shown')).toHaveCount(0);
}

function navigationToggle(page: Page): Locator {
  return page.locator('button[aria-controls="primary-navigation"]');
}

async function assertShellLayout(page: Page, context: string): Promise<void> {
  const toolbar = page.locator('mat-toolbar.shell-toolbar');
  const main = page.getByRole('main');
  await expect(toolbar).toBeVisible();
  await expect(main).toBeVisible();

  const [toolbarBox, mainBox] = await Promise.all([toolbar.boundingBox(), main.boundingBox()]);
  expect(toolbarBox, `${context}: toolbar debe tener dimensiones`).not.toBeNull();
  expect(mainBox, `${context}: main debe tener dimensiones`).not.toBeNull();
  expect(
    mainBox!.y,
    `${context}: el contenido no debe quedar tapado por la toolbar`
  ).toBeGreaterThanOrEqual(toolbarBox!.y + toolbarBox!.height - 1);
}

async function assertNoGlobalOverflow(page: Page, context: string): Promise<void> {
  const metrics = await page.evaluate(() => ({
    viewportWidth: window.innerWidth,
    documentWidth: document.documentElement.scrollWidth,
    bodyWidth: document.body.scrollWidth
  }));
  const globalWidth = Math.max(metrics.documentWidth, metrics.bodyWidth);
  expect(
    globalWidth,
    `${context}: el documento no debe desbordar horizontalmente`
  ).toBeLessThanOrEqual(metrics.viewportWidth + 1);
}

async function resetGridHorizontalScroll(page: Page): Promise<void> {
  await page
    .locator('.ag-center-cols-viewport, .ag-body-horizontal-scroll-viewport')
    .evaluateAll((elements) => {
      elements.forEach((element) => {
        element.scrollLeft = 0;
      });
    });
}

async function assertNoSensitiveOrBrokenText(page: Page): Promise<void> {
  const text = await page.getByRole('main').innerText();
  expect(text).not.toContain('[object Object]');
  expect(text).not.toMatch(/\bBearer\s+[A-Za-z0-9._~-]+/i);
  expect(text).not.toMatch(/\beyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}\b/);
}

async function assertWithinViewport(locator: Locator): Promise<void> {
  const box = await locator.boundingBox();
  expect(box, 'El diálogo debe tener dimensiones visibles').not.toBeNull();
  const viewport = await locator.page().evaluate(() => ({
    width: window.innerWidth,
    height: window.innerHeight
  }));
  expect(box!.x).toBeGreaterThanOrEqual(0);
  expect(box!.y).toBeGreaterThanOrEqual(0);
  expect(box!.x + box!.width).toBeLessThanOrEqual(viewport.width);
  expect(box!.y + box!.height).toBeLessThanOrEqual(viewport.height);
}

function monitorBrowser(page: Page): BrowserDiagnostics {
  const diagnostics: BrowserDiagnostics = {
    consoleErrors: [],
    pageErrors: [],
    failedApiRequests: [],
    failedHttpResponses: []
  };

  page.on('console', (message) => {
    if (message.type() === 'error') {
      diagnostics.consoleErrors.push(message.text());
    }
  });
  page.on('pageerror', (error) => diagnostics.pageErrors.push(error.message));
  page.on('requestfailed', (request) => {
    if (isApiUrl(request.url())) {
      diagnostics.failedApiRequests.push(
        `${request.method()} ${new URL(request.url()).pathname}: `
        + `${request.failure()?.errorText ?? 'error de red'}`
      );
    }
  });
  page.on('response', (response) => {
    if (isApiUrl(response.url()) && response.status() >= 400) {
      diagnostics.failedHttpResponses.push(
        `${response.request().method()} ${new URL(response.url()).pathname}: ${response.status()}`
      );
    }
  });

  return diagnostics;
}

function assertDiagnosticsAreClean(diagnostics: BrowserDiagnostics): void {
  expect(diagnostics.consoleErrors, 'La consola no debe registrar errores').toEqual([]);
  expect(diagnostics.pageErrors, 'La página no debe emitir excepciones').toEqual([]);
  expect(diagnostics.failedApiRequests, 'La red no debe tener solicitudes API fallidas').toEqual([]);
  expect(
    diagnostics.failedHttpResponses,
    'La API no debe responder 4xx o 5xx durante estos escenarios'
  ).toEqual([]);
}

function isApiUrl(value: string): boolean {
  const url = new URL(value);
  return url.port === '843' || url.pathname.startsWith('/api/');
}

function escapeRegExp(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
