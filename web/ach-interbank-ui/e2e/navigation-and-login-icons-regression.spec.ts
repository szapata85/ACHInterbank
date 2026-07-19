import { expect, Page, test } from '@playwright/test';
import path from 'node:path';

const viewports = [
  { width: 1920, height: 1080 },
  { width: 1366, height: 768 },
  { width: 1024, height: 768 }
];

const evidencePhase = process.env['ICON_EVIDENCE_PHASE'] ?? 'final';

test.describe('Regresión visual de iconos de navegación y login', () => {
  for (const viewport of viewports) {
    test(`login conserva el control visual y accesible en ${viewport.width}x${viewport.height}`, async ({ page }) => {
      const loginRequests: string[] = [];
      const externalFontRequests: string[] = [];

      page.on('request', (request) => {
        const url = new URL(request.url());
        if (url.pathname.endsWith('/auth/login')) {
          loginRequests.push(request.url());
        }
        if (url.hostname === 'fonts.googleapis.com' || url.hostname === 'fonts.gstatic.com') {
          externalFontRequests.push(request.url());
        }
      });

      await page.setViewportSize(viewport);
      await page.goto('/login');

      const passwordInput = page.locator('input[formControlName="password"]');
      const toggle = page.getByRole('button', { name: 'Mostrar contraseña', exact: true });

      await passwordInput.fill('Clave-Ficticia-123!');
      await screenshot(page, `login-oculto-${viewport.width}x${viewport.height}.png`);

      await expect(passwordInput).toHaveAttribute('type', 'password');
      await expect(toggle).toHaveAttribute('type', 'button');
      await expect(toggle).toHaveAttribute('title', 'Mostrar contraseña');
      await expect(toggle).toHaveAttribute('aria-pressed', 'false');
      await expect(toggle.locator('app-ui-icon[data-icon-key="visibility"]')).toBeVisible();

      await toggle.click();
      await screenshot(page, `login-visible-${viewport.width}x${viewport.height}.png`);

      const hideToggle = page.getByRole('button', { name: 'Ocultar contraseña', exact: true });
      await expect(passwordInput).toHaveAttribute('type', 'text');
      await expect(passwordInput).toHaveValue('Clave-Ficticia-123!');
      await expect(hideToggle).toHaveAttribute('aria-label', 'Ocultar contraseña');
      await expect(hideToggle).toHaveAttribute('title', 'Ocultar contraseña');
      await expect(hideToggle).toHaveAttribute('aria-pressed', 'true');
      await expect(hideToggle.locator('app-ui-icon[data-icon-key="visibility_off"]')).toBeVisible();

      await hideToggle.press('Enter');
      await expect(passwordInput).toHaveAttribute('type', 'password');
      await expect(passwordInput).toHaveValue('Clave-Ficticia-123!');
      expect(loginRequests).toEqual([]);
      expect(externalFontRequests).toEqual([]);
    });

    test(`menú runtime conserva iconos, jerarquía, colapso y recarga en ${viewport.width}x${viewport.height}`, async ({ page }) => {
      const consoleErrors: string[] = [];
      const failedRequests: string[] = [];

      page.on('console', (message) => {
        if (message.type() === 'error') {
          consoleErrors.push(message.text());
        }
      });
      page.on('requestfailed', (request) => {
        failedRequests.push(`${request.method()} ${request.url()} ${request.failure()?.errorText ?? ''}`.trim());
      });

      await page.setViewportSize(viewport);
      await configureAuthenticatedRuntime(page);
      await page.goto('/dashboard');

      const sidebar = page.locator('aside.sidebar');
      const rootIcon = sidebar.locator('a.menu-item[data-menu-item-id="1"] app-ui-icon[data-icon-key="dashboard"]');
      const parentIcon = sidebar.locator('a.menu-item[data-menu-item-id="2"] app-ui-icon[data-icon-key="account_balance"]');
      const childIcon = sidebar.locator('a.submenu-item[data-menu-item-id="21"] app-ui-icon[data-icon-key="schedule"]');
      const fallbackIcon = sidebar.locator('a.menu-item[data-menu-item-id="3"] app-ui-icon[data-icon-resolved="help"]');
      const submenuToggle = sidebar.getByRole('button', { name: 'Alternar submenú de Operación', exact: true });

      await expect(rootIcon).toBeVisible();
      await expect(parentIcon).toBeVisible();
      await expect(fallbackIcon).toBeVisible();
      await expect(sidebar.locator('a.menu-item.active')).toHaveCount(1);
      await expect(sidebar.locator('a.submenu-item.active')).toHaveCount(1);
      await expect(sidebar.getByText('Panel principal', { exact: true })).toBeVisible();
      await submenuToggle.click();
      await expect(submenuToggle).toHaveAttribute('aria-expanded', 'true');
      await expect(childIcon).toBeVisible();
      await expect(sidebar.getByText('Ciclos', { exact: true })).toBeVisible();
      await expect(sidebar.locator('#submenu-2')).toHaveCSS('opacity', '1');
      await screenshot(page, `menu-expandido-${viewport.width}x${viewport.height}.png`);

      const collapseToggle = page.getByRole('button', { name: 'Contraer menú principal', exact: true });
      await collapseToggle.click();

      await expect(page.locator('.layout.sidebar-collapsed')).toBeVisible();
      await expect(rootIcon).toBeVisible();
      await expect(parentIcon).toBeVisible();
      await expect(sidebar.getByText('Panel principal', { exact: true })).toBeHidden();
      await expect(page.getByRole('button', { name: 'Expandir menú principal', exact: true })).toBeVisible();
      await screenshot(page, `menu-colapsado-${viewport.width}x${viewport.height}.png`);

      await page.getByRole('button', { name: 'Expandir menú principal', exact: true }).click();
      await expect(sidebar.getByText('Panel principal', { exact: true })).toBeVisible();

      await page.reload();
      await expect(rootIcon).toBeVisible();
      await expect(parentIcon).toBeVisible();
      await expect(childIcon).toBeVisible();
      await expect(sidebar.locator('a.menu-item.active')).toHaveCount(1);
      await expect(sidebar.locator('a.submenu-item.active')).toHaveCount(1);
      expect(consoleErrors).toEqual([]);
      expect(failedRequests).toEqual([]);
    });
  }
});

async function configureAuthenticatedRuntime(page: Page): Promise<void> {
  const token = createUnsignedJwt({
    unique_name: 'ui.icons.regression',
    name: 'Usuario UI Icons',
    uid: 'ui-icons-regression',
    role: ['Admin'],
    permission: ['CanReadAch'],
    exp: Math.floor(Date.now() / 1000) + 3600,
    iat: Math.floor(Date.now() / 1000)
  });

  await page.addInitScript((accessToken) => {
    window.sessionStorage.setItem('ach.interbank.access_token', accessToken);
  }, token);

  await page.route(/\/auth\/refresh$/, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        sucess: true,
        data: {
          token,
          username: 'ui.icons.regression',
          fullName: 'Usuario UI Icons',
          roles: ['Admin'],
          permissions: ['CanReadAch']
        }
      })
    });
  });

  await page.route(/\/api\/users\/branding$/, async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: '{}' });
  });

  await page.route(/\/api\/navigation\/menu$/, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        { id: 1, label: 'Panel principal', route: '/dashboard', icon: 'dashboard', exact: true, order: 1 },
        {
          id: 2,
          label: 'Operación',
          route: '/transactions',
          icon: 'account_balance',
          exact: false,
          order: 2,
          children: [
            { id: 21, label: 'Ciclos', route: '/dashboard', icon: 'schedule', exact: true, order: 1 }
          ]
        },
        { id: 3, label: 'Icono desconocido', route: '/icon-fallback', icon: 'unknown_semantic_key', exact: false, order: 3 }
      ])
    });
  });

  await page.route(/\/api\/navigation-logs$/, async (route) => {
    await route.fulfill({ status: 204, body: '' });
  });
}

async function screenshot(page: Page, fileName: string): Promise<void> {
  await page.screenshot({
    path: path.join(process.cwd(), 'e2e-evidence', 'ui-icon-regression', evidencePhase, fileName),
    fullPage: true
  });
}

function createUnsignedJwt(payload: Record<string, unknown>): string {
  const encode = (value: Record<string, unknown>): string =>
    Buffer.from(JSON.stringify(value))
      .toString('base64url');

  return `${encode({ alg: 'none', typ: 'JWT' })}.${encode(payload)}.`;
}
