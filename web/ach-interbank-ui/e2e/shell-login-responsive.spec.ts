import { expect, Page, test, TestInfo } from '@playwright/test';

const SMOKE_ROUTES = [
  '/uat/nacha-inbound-simulator',
  '/audit-logs',
  '/auth-logs',
  '/catalogs/financial-institutions',
  '/catalogs/clearing-house-preferences',
  '/catalogs/bank-holidays',
  '/transactions/returns'
] as const;

type Diagnostics = {
  consoleErrors: string[];
  pageErrors: string[];
  requestFailures: string[];
  responseErrors: string[];
};

test.describe.serial('Login and authenticated Material shell', () => {
  test('desktop: real login, dynamic navigation, smoke routes and logout', async ({ page }, testInfo) => {
    test.setTimeout(60_000);
    const diagnostics = monitorDiagnostics(page);
    await page.setViewportSize({ width: 1440, height: 900 });

    const loginRequests = await exerciseLogin(page, testInfo, 'desktop');
    expect(loginRequests, 'El formulario debe emitir una sola solicitud de autenticación.').toBe(1);

    await expectDesktopShell(page);
    await navigateThroughMenu(page, '/audit-logs', ['Logs']);
    await expect(page).toHaveURL(/\/audit-logs$/);
    await expect(page.locator('a[href="/audit-logs"]')).toHaveAttribute('aria-current', 'page');

    for (const route of SMOKE_ROUTES) {
      await page.goto(route, { waitUntil: 'domcontentloaded' });
      await page.waitForLoadState('networkidle');
      await expect(page.locator('app-loading-overlay .overlay')).toBeHidden({ timeout: 15_000 });
      await expect(page).toHaveURL(new RegExp(`${escapeRegExp(route)}$`));
      await expectDesktopShell(page);
      await assertShellLayout(page);

      const activeLink = page.locator(`a[href="${route}"][aria-current="page"]`);
      if (await activeLink.count()) {
        await expect(activeLink).toBeVisible();
      }
    }

    await attachScreenshot(page, testInfo, 'desktop-shell');

    await page.getByRole('button', { name: 'Cerrar sesión' }).click();
    await expect(page).toHaveURL(/\/login$/);
    await expect(page.getByRole('heading', { name: 'Ingreso al portal ACH Interbank' })).toBeVisible();

    assertCleanDiagnostics(diagnostics);
  });

  test('mobile: real login, overlay navigation, focus, Escape and backdrop', async ({ page }, testInfo) => {
    const diagnostics = monitorDiagnostics(page);
    await page.setViewportSize({ width: 390, height: 844 });

    const loginRequests = await exerciseLogin(page, testInfo, 'mobile');
    expect(loginRequests, 'El formulario debe emitir una sola solicitud de autenticación.').toBe(1);

    await expectMobileShell(page);
    const menuToggle = page.getByRole('button', { name: 'Abrir menú principal' });
    await menuToggle.click();

    const sidenav = page.locator('mat-sidenav.primary-sidenav');
    const backdrop = page.locator('.mat-drawer-backdrop.mat-drawer-shown');
    await expect(sidenav).toBeVisible();
    await expect(backdrop).toBeVisible();
    await expect.poll(() => focusIsInsideSidenav(page)).toBe(true);

    await navigateThroughMenu(page, '/catalogs/financial-institutions', ['Catálogos'], true);
    await expect(page).toHaveURL(/\/catalogs\/financial-institutions$/);
    await expect(sidenav).toBeHidden();
    await expect(page.locator('.mat-drawer-backdrop.mat-drawer-shown')).toHaveCount(0);
    await assertShellLayout(page);

    const reopenToggle = page.getByRole('button', { name: 'Abrir menú principal' });
    await reopenToggle.click();
    await expect(sidenav).toBeVisible();
    await expect.poll(() => focusIsInsideSidenav(page)).toBe(true);
    await page.keyboard.press('Escape');
    await expect(sidenav).toBeHidden();
    await expect(reopenToggle).toBeFocused();

    await reopenToggle.click();
    await expect(backdrop).toBeVisible();
    await backdrop.click({ position: { x: 380, y: 120 } });
    await expect(sidenav).toBeHidden();
    await expect(page.locator('.mat-drawer-backdrop.mat-drawer-shown')).toHaveCount(0);

    await attachScreenshot(page, testInfo, 'mobile-shell');
    assertCleanDiagnostics(diagnostics);
  });
});

async function exerciseLogin(
  page: Page,
  testInfo: TestInfo,
  viewportName: 'desktop' | 'mobile'
): Promise<number> {
  const username = requiredEnvironmentValue('ACH_USER');
  const password = requiredEnvironmentValue('ACH_PASS');
  let loginRequests = 0;

  page.on('request', (request) => {
    if (request.method() === 'POST' && new URL(request.url()).pathname.endsWith('/auth/login')) {
      loginRequests += 1;
    }
  });

  await page.goto('/login', { waitUntil: 'domcontentloaded' });
  await expect(page.getByRole('heading', { name: 'Ingreso al portal ACH Interbank' })).toBeVisible();
  await assertNoDocumentOverflow(page);

  const usernameInput = page.getByLabel('Usuario', { exact: true });
  const passwordInput = page.getByLabel('Contraseña', { exact: true });
  const submitButton = page.getByRole('button', { name: 'Ingresar', exact: true });

  await expect(submitButton).toBeDisabled();
  await usernameInput.focus();
  await usernameInput.press('Tab');
  await expect(page.getByText('El usuario es obligatorio.', { exact: true })).toBeVisible();
  await passwordInput.focus();
  await passwordInput.press('Tab');
  await expect(page.getByText('La contraseña es obligatoria.', { exact: true })).toBeVisible();

  const showPassword = page.getByRole('button', { name: 'Mostrar contraseña' });
  await showPassword.click();
  await expect(passwordInput).toHaveAttribute('type', 'text');
  await page.getByRole('button', { name: 'Ocultar contraseña' }).click();
  await expect(passwordInput).toHaveAttribute('type', 'password');

  await attachScreenshot(page, testInfo, `login-${viewportName}`);

  await usernameInput.fill(username);
  await passwordInput.fill(password);
  await expect(submitButton).toBeEnabled();
  await passwordInput.press('Enter');

  await expect(page).not.toHaveURL(/\/login$/, { timeout: 15_000 });
  await expect(page.locator('mat-sidenav-container.shell-container')).toBeVisible();
  await expect(page.locator('app-loading-overlay .overlay')).toBeHidden({ timeout: 15_000 });
  await expect(page.locator('a[data-menu-item-id][href="/dashboard"]')).toBeAttached();
  return loginRequests;
}

async function expectDesktopShell(page: Page): Promise<void> {
  await expect(page.locator('mat-toolbar.shell-toolbar')).toBeVisible();
  await expect(page.locator('mat-sidenav-container.shell-container')).toBeVisible();
  await expect(page.locator('mat-sidenav.primary-sidenav')).toHaveClass(/mat-drawer-side/);
  await expect(page.locator('mat-sidenav.primary-sidenav')).toBeVisible();
  await expect(page.locator('main#main-content')).toBeVisible();
  await expect(page.getByRole('button', { name: 'Contraer menú principal' })).toBeVisible();
}

async function expectMobileShell(page: Page): Promise<void> {
  await expect(page.locator('mat-toolbar.shell-toolbar')).toBeVisible();
  await expect(page.locator('mat-sidenav.primary-sidenav')).toHaveClass(/mat-drawer-over/);
  await expect(page.locator('mat-sidenav.primary-sidenav')).toBeHidden();
  await expect(page.getByRole('button', { name: 'Abrir menú principal' })).toBeVisible();
  await expect(page.locator('.mat-drawer-backdrop.mat-drawer-shown')).toHaveCount(0);
  await assertShellLayout(page);
}

async function navigateThroughMenu(
  page: Page,
  route: string,
  parentLabels: string[],
  assertDrawerRemainsOpen = false
): Promise<void> {
  for (const parentLabel of parentLabels) {
    const parent = page.getByRole('button', { name: parentLabel, exact: true });
    await expect(parent).toBeVisible();
    if ((await parent.getAttribute('aria-expanded')) !== 'true') {
      await parent.click();
    }

    if (assertDrawerRemainsOpen) {
      await expect(page.locator('mat-sidenav.primary-sidenav')).toBeVisible();
      await expect(page.locator('.mat-drawer-backdrop.mat-drawer-shown')).toBeVisible();
    }
  }

  const target = page.locator(`a[data-menu-item-id][href="${route}"]`);
  await expect(target).toHaveCount(1);
  await expect(target).toBeVisible();
  await target.click();
}

async function assertShellLayout(page: Page): Promise<void> {
  const metrics = await page.evaluate(() => {
    const toolbar = document.querySelector<HTMLElement>('mat-toolbar.shell-toolbar');
    const main = document.querySelector<HTMLElement>('main#main-content');
    const toolbarRect = toolbar?.getBoundingClientRect();
    const mainRect = main?.getBoundingClientRect();
    const root = document.documentElement;

    return {
      horizontalOverflow: Math.max(root.scrollWidth, document.body.scrollWidth) - root.clientWidth,
      mainStartsBelowToolbar:
        Boolean(toolbarRect && mainRect) && (mainRect?.top ?? 0) >= (toolbarRect?.bottom ?? 0) - 1,
      windowScrollsVertically: root.scrollHeight > root.clientHeight + 1,
      mainScrollsVertically: Boolean(main) && (main?.scrollHeight ?? 0) > (main?.clientHeight ?? 0) + 1,
      mainWidth: mainRect?.width ?? 0
    };
  });

  expect(metrics.horizontalOverflow, 'El shell no debe producir overflow horizontal global.').toBeLessThanOrEqual(2);
  expect(metrics.mainStartsBelowToolbar, 'El toolbar no debe cubrir el contenido principal.').toBe(true);
  expect(metrics.mainWidth, 'El área principal debe conservar un ancho utilizable.').toBeGreaterThan(0);
  expect(
    metrics.windowScrollsVertically && metrics.mainScrollsVertically,
    'Window y el área principal no deben crear doble scroll vertical.'
  ).toBe(false);
}

async function assertNoDocumentOverflow(page: Page): Promise<void> {
  const overflow = await page.evaluate(
    () => Math.max(document.documentElement.scrollWidth, document.body.scrollWidth) - window.innerWidth
  );
  expect(overflow, 'La pantalla no debe producir overflow horizontal.').toBeLessThanOrEqual(2);
}

async function focusIsInsideSidenav(page: Page): Promise<boolean> {
  return page.evaluate(() => {
    const sidenav = document.querySelector('mat-sidenav.primary-sidenav');
    return Boolean(sidenav?.contains(document.activeElement));
  });
}

function monitorDiagnostics(page: Page): Diagnostics {
  const diagnostics: Diagnostics = {
    consoleErrors: [],
    pageErrors: [],
    requestFailures: [],
    responseErrors: []
  };

  page.on('console', (message) => {
    if (message.type() === 'error') {
      diagnostics.consoleErrors.push(sanitizeDiagnostic(message.text()));
    }
  });

  page.on('pageerror', (error) => {
    diagnostics.pageErrors.push(sanitizeDiagnostic(error.message));
  });

  page.on('requestfailed', (request) => {
    diagnostics.requestFailures.push(
      `${request.method()} ${safePath(request.url())}: ${sanitizeDiagnostic(request.failure()?.errorText ?? 'falló')}`
    );
  });

  page.on('response', (response) => {
    if (response.status() >= 400) {
      diagnostics.responseErrors.push(`${response.status()} ${safePath(response.url())}`);
    }
  });

  return diagnostics;
}

function assertCleanDiagnostics(diagnostics: Diagnostics): void {
  expect(diagnostics.consoleErrors, 'No debe haber errores de consola.').toEqual([]);
  expect(diagnostics.pageErrors, 'No debe haber excepciones de página.').toEqual([]);
  expect(diagnostics.requestFailures, 'No debe haber solicitudes fallidas.').toEqual([]);
  expect(diagnostics.responseErrors, 'No debe haber respuestas HTTP de error.').toEqual([]);
}

async function attachScreenshot(page: Page, testInfo: TestInfo, name: string): Promise<void> {
  await testInfo.attach(name, {
    body: await page.screenshot({ fullPage: true }),
    contentType: 'image/png'
  });
}

function requiredEnvironmentValue(name: 'ACH_USER' | 'ACH_PASS'): string {
  const value = process.env[name]?.trim();
  if (!value) {
    throw new Error(`La variable ${name} es obligatoria para el login LIVE.`);
  }

  return value;
}

function safePath(rawUrl: string): string {
  try {
    return new URL(rawUrl).pathname;
  } catch {
    return '[ruta no disponible]';
  }
}

function sanitizeDiagnostic(value: string): string {
  return value
    .replace(/eyJ[\w.-]+/g, '[token redactado]')
    .replace(/[\w.+-]+@[\w.-]+\.[A-Za-z]{2,}/g, '[correo redactado]')
    .replace(/\b\d{6,}\b/g, '[dato redactado]')
    .slice(0, 300);
}

function escapeRegExp(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
