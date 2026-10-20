import { expect, Locator, Page, test } from '@playwright/test';
import { mkdir } from 'node:fs/promises';
import path from 'node:path';

const desktopViewport = { width: 1440, height: 900 };
const mobileViewport = { width: 390, height: 844 };
const evidenceDirectory = path.join(
  process.cwd(),
  'e2e-evidence',
  'login-shell-corrective-1.1'
);

type Diagnostics = {
  consoleErrors: string[];
  pageErrors: string[];
  requestFailures: string[];
  responseErrors: string[];
  externalFontRequests: string[];
};

test.describe.serial('Login y shell correctivo LIVE', () => {
  test('desktop: login real, grupo activo y navegación expandida/compacta', async ({ page }) => {
    test.setTimeout(60_000);
    const diagnostics = monitorDiagnostics(page);
    await page.setViewportSize(desktopViewport);

    const loginRequests = await exerciseLogin(page, 'login-desktop.png');
    expect(loginRequests).toBe(1);

    const sidenav = page.locator('mat-sidenav.primary-sidenav');
    const toggle = page.locator('button.menu-toggle');
    await expect(sidenav).toHaveClass(/mat-drawer-side/);
    await expect(toggle).toHaveAttribute('aria-label', 'Contraer navegación');
    await expectControlSize(toggle);

    const cyclesGroup = page.getByRole('button', {
      name: 'Configuración de ciclos',
      exact: true
    });
    const cyclesGroupId = await cyclesGroup.getAttribute('data-menu-item-id');
    expect(cyclesGroupId).not.toBeNull();
    const cyclesGroupRow = sidenav.locator(
      `button.nav-parent[data-menu-item-id="${cyclesGroupId}"]`
    );
    if ((await cyclesGroup.getAttribute('aria-expanded')) !== 'true') {
      await cyclesGroup.click();
    }

    const cyclesLink = sidenav.locator('a[data-menu-item-id][href="/ach-cycles"]');
    await expect(cyclesLink).toBeVisible();
    await cyclesLink.click();
    await expect(page).toHaveURL(/\/ach-cycles$/);
    await expect(page.locator('app-loading-overlay .overlay')).toBeHidden({ timeout: 15_000 });
    await expect(cyclesGroup).toHaveAttribute('aria-expanded', 'true');
    await expect(cyclesGroup).toHaveClass(/active/);
    await expectBrandVisible(sidenav);
    await assertShellLayout(page);
    await assertSidenavNoHorizontalScroll(page);
    if (process.env['SKIP_EXPANDED_EVIDENCE'] !== 'true') {
      await saveEvidence(page, 'shell-desktop-expanded.png');
    }

    await toggle.click();
    await expect(toggle).toHaveAttribute('aria-label', 'Expandir navegación');
    await expect(sidenav).toHaveClass(/compact/);
    await expectControlSize(toggle);
    await expect(cyclesGroupRow.locator('app-ui-icon')).toBeVisible();
    await expect(cyclesGroupRow.locator('.nav-label')).toBeHidden();
    await assertShellLayout(page);
    await assertSidenavNoHorizontalScroll(page);
    if (process.env['SKIP_COMPACT_EVIDENCE'] !== 'true') {
      await saveEvidence(page, 'shell-desktop-compact.png');
    }

    assertCleanDiagnostics(diagnostics);
  });

  test('móvil: login real, overlay, navegación, Escape y backdrop', async ({ page }) => {
    const diagnostics = monitorDiagnostics(page);
    await page.setViewportSize(mobileViewport);

    const loginRequests = await exerciseLogin(page, 'login-mobile.png');
    expect(loginRequests).toBe(1);

    const sidenav = page.locator('mat-sidenav.primary-sidenav');
    const toggle = page.locator('button.menu-toggle');
    await expect(sidenav).toHaveClass(/mat-drawer-over/);
    await expect(sidenav).toBeHidden();
    await expect(toggle).toHaveAttribute('aria-label', 'Abrir navegación');
    await expectControlSize(toggle);
    await assertShellLayout(page);

    await toggle.click();
    await expect(sidenav).toBeVisible();
    await expect(page.locator('.mat-drawer-backdrop.mat-drawer-shown')).toBeVisible();
    await expect.poll(() => focusIsInsideSidenav(page)).toBe(true);

    const cyclesGroup = page.getByRole('button', {
      name: 'Configuración de ciclos',
      exact: true
    });
    if ((await cyclesGroup.getAttribute('aria-expanded')) !== 'true') {
      await cyclesGroup.click();
    }
    await expect(sidenav).toBeVisible();
    await sidenav.locator('a[data-menu-item-id][href="/ach-cycles"]').click();
    await expect(page).toHaveURL(/\/ach-cycles$/);
    await expect(sidenav).toBeHidden();
    await expect(page.locator('.mat-drawer-backdrop.mat-drawer-shown')).toHaveCount(0);
    await assertShellLayout(page);

    await toggle.click();
    await expect(sidenav).toBeVisible();
    await expect.poll(() => focusIsInsideSidenav(page)).toBe(true);
    await page.keyboard.press('Escape');
    await expect(sidenav).toBeHidden();
    await expect(toggle).toBeFocused();

    await toggle.click();
    const backdrop = page.locator('.mat-drawer-backdrop.mat-drawer-shown');
    await expect(backdrop).toBeVisible();
    await backdrop.click({ position: { x: 380, y: 120 } });
    await expect(sidenav).toBeHidden();
    await expect(page.locator('.mat-drawer-backdrop.mat-drawer-shown')).toHaveCount(0);

    assertCleanDiagnostics(diagnostics);
  });
});

async function exerciseLogin(page: Page, evidenceName: string): Promise<number> {
  const username = requiredEnvironmentValue('ACH_USER');
  const password = requiredEnvironmentValue('ACH_PASS');
  let loginRequests = 0;

  page.on('request', (request) => {
    if (request.method() === 'POST' && new URL(request.url()).pathname.endsWith('/auth/login')) {
      loginRequests += 1;
    }
  });

  await page.goto('/login', { waitUntil: 'networkidle' });
  await expect(page.getByRole('heading', { name: 'Ingreso al portal ACH Interbank' })).toBeVisible();
  await assertNoDocumentOverflow(page);

  const accountIcon = page.locator('svg[data-login-icon="account"]');
  const lockIcon = page.locator('svg[data-login-icon="lock"]');
  const showIcon = page.locator('svg[data-login-icon="visibility"]');
  await expectLocalSvgIcon(accountIcon);
  await expectLocalSvgIcon(lockIcon);
  await expectLocalSvgIcon(showIcon);
  await expect(page.locator('.login-card mat-icon')).toHaveCount(0);
  if (process.env['SKIP_LOGIN_EVIDENCE'] !== 'true') {
    await saveEvidence(page, evidenceName);
  }

  const usernameInput = page.locator('input[formControlName="username"]');
  const passwordInput = page.locator('input[formControlName="password"]');
  const submitButton = page.getByRole('button', { name: 'Ingresar', exact: true });

  await expect(submitButton).toBeDisabled();
  await usernameInput.focus();
  await usernameInput.press('Tab');
  await expect(page.getByText('El usuario es obligatorio.', { exact: true })).toBeVisible();
  await passwordInput.focus();
  await passwordInput.press('Tab');
  await expect(page.getByText('La contraseña es obligatoria.', { exact: true })).toBeVisible();

  const showPassword = page.getByRole('button', { name: 'Mostrar contraseña' });
  await expectControlSize(showPassword);
  await showPassword.click();
  await expect(passwordInput).toHaveAttribute('type', 'text');
  const hidePassword = page.getByRole('button', { name: 'Ocultar contraseña' });
  await expectLocalSvgIcon(hidePassword.locator('svg[data-login-icon="visibility-off"]'));
  await hidePassword.click();
  await expect(passwordInput).toHaveAttribute('type', 'password');

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

async function expectLocalSvgIcon(icon: Locator): Promise<void> {
  await expect(icon).toBeVisible();
  await expect(icon.locator('path, circle, rect').first()).toBeAttached();
  expect((await icon.textContent())?.trim() ?? '').toBe('');

  const box = await icon.boundingBox();
  expect(box?.width ?? 0).toBeGreaterThanOrEqual(18);
  expect(box?.height ?? 0).toBeGreaterThanOrEqual(18);
}

async function expectControlSize(control: Locator): Promise<void> {
  await expect(control).toBeVisible();
  const box = await control.boundingBox();
  expect(box?.width ?? 0).toBeGreaterThanOrEqual(44);
  expect(box?.height ?? 0).toBeGreaterThanOrEqual(44);
}

async function expectBrandVisible(sidenav: Locator): Promise<void> {
  const logo = sidenav.locator('.brand .logo');
  await expect(logo).toBeVisible();
  await expect(logo).toContainText('ACH');
  const box = await logo.boundingBox();
  expect(box?.width ?? 0).toBeGreaterThanOrEqual(40);
  expect(box?.height ?? 0).toBeGreaterThanOrEqual(40);
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
      mainWidth: mainRect?.width ?? 0
    };
  });

  expect(metrics.horizontalOverflow).toBeLessThanOrEqual(2);
  expect(metrics.mainStartsBelowToolbar).toBe(true);
  expect(metrics.mainWidth).toBeGreaterThan(0);
}

async function assertNoDocumentOverflow(page: Page): Promise<void> {
  const overflow = await page.evaluate(
    () => Math.max(document.documentElement.scrollWidth, document.body.scrollWidth) - window.innerWidth
  );
  expect(overflow).toBeLessThanOrEqual(2);
}

async function assertSidenavNoHorizontalScroll(page: Page): Promise<void> {
  const metrics = await page.locator('mat-sidenav.primary-sidenav').evaluate((element) => {
    const inner = element.querySelector<HTMLElement>('.mat-drawer-inner-container');
    return {
      scrollLeft: inner?.scrollLeft ?? 0,
      overflow: (inner?.scrollWidth ?? 0) - (inner?.clientWidth ?? 0)
    };
  });

  expect(metrics.scrollLeft).toBe(0);
  expect(metrics.overflow).toBeLessThanOrEqual(1);
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
    responseErrors: [],
    externalFontRequests: []
  };

  page.on('request', (request) => {
    const url = new URL(request.url());
    if (url.hostname === 'fonts.googleapis.com' || url.hostname === 'fonts.gstatic.com') {
      diagnostics.externalFontRequests.push(url.hostname);
    }
  });
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
  expect(diagnostics.consoleErrors).toEqual([]);
  expect(diagnostics.pageErrors).toEqual([]);
  expect(diagnostics.requestFailures).toEqual([]);
  expect(diagnostics.responseErrors).toEqual([]);
  expect(diagnostics.externalFontRequests).toEqual([]);
}

async function saveEvidence(page: Page, fileName: string): Promise<void> {
  await mkdir(evidenceDirectory, { recursive: true });
  await page.screenshot({
    path: path.join(evidenceDirectory, fileName),
    fullPage: true
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
