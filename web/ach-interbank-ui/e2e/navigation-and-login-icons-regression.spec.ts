import { expect, Locator, Page, test } from '@playwright/test';

const desktopViewport = { width: 1440, height: 900 };
const mobileViewport = { width: 390, height: 844 };

test.describe('Regresión focalizada del sidenav', () => {
  test('mantiene el modo compacto recuperable y todos los estados legibles', async ({ page }) => {
    const diagnostics = monitorDiagnostics(page);
    await page.setViewportSize(desktopViewport);
    await configureAuthenticatedRuntime(page);
    await page.goto('/dashboard');

    const sidenav = page.locator('mat-sidenav.primary-sidenav');
    const toggle = page.locator('button.menu-toggle');
    const rootLink = sidenav.locator('a.nav-item[data-menu-item-id="1"]');
    const parent = sidenav.locator('button.nav-parent[data-menu-item-id="2"]');
    const child = sidenav.locator('a.nav-item[data-menu-item-id="21"]');
    const normal = sidenav.locator('a.nav-item[data-menu-item-id="3"]');
    const normalParent = sidenav.locator('button.nav-parent[data-menu-item-id="4"]');

    await expect(toggle).toHaveAttribute('aria-label', 'Contraer navegación');
    await expectControlSize(toggle);

    for (let cycle = 0; cycle < 10; cycle += 1) {
      const compact = cycle % 2 === 0;
      await toggle.click();
      await expect(toggle).toHaveAttribute(
        'aria-label',
        compact ? 'Expandir navegación' : 'Contraer navegación'
      );
      await expect(sidenav).toHaveClass(compact ? /compact/ : /^(?!.*compact)/);
      await expectControlSize(toggle);
    }

    await toggle.click();
    await expect(toggle).toHaveAttribute('aria-label', 'Expandir navegación');
    await expect(sidenav).toHaveClass(/compact/);
    await expect(rootLink.locator('app-ui-icon')).toBeVisible();
    await expect(rootLink.locator('.nav-label')).toBeHidden();

    await rootLink.click();
    await expect(page).toHaveURL(/\/dashboard$/);
    await expect(sidenav).toHaveClass(/compact/);

    await parent.click();
    await expect(sidenav).not.toHaveClass(/compact/);
    await expect(parent).toHaveAttribute('aria-expanded', 'true');
    await expect(child).toBeVisible();

    await child.click();
    await expect(page).toHaveURL(/\/dashboard$/);
    await expect(rootLink).toHaveAttribute('aria-current', 'page');
    await expect(child).toHaveAttribute('aria-current', 'page');
    await expect(parent).toHaveClass(/active/);

    await expectReadableOnSidenav(rootLink.locator('.nav-label'), 4.5);
    await expectReadableOnSidenav(rootLink.locator('.nav-icon'), 3);
    await expectReadableOnSidenav(normal.locator('.nav-label'), 4.5);
    await expectReadableOnSidenav(normal.locator('.nav-icon'), 3);
    await expectReadableOnSidenav(normalParent.locator('.nav-label'), 4.5);
    await expectReadableOnSidenav(normalParent.locator('.nav-icon'), 3);
    await expectReadableOnSidenav(normalParent.locator('.expand-icon'), 3);
    await expectReadableOnSidenav(parent.locator('.nav-label'), 4.5);
    await expectReadableOnSidenav(parent.locator('.nav-icon'), 3);
    await expectReadableOnSidenav(parent.locator('.expand-icon'), 3);
    await expectReadableOnSidenav(child.locator('.nav-label'), 4.5);
    await expectReadableOnSidenav(child.locator('.nav-icon'), 3);

    const activeSurface = await parent.evaluate((element) => {
      const style = getComputedStyle(element);
      const indicator = getComputedStyle(element, '::before');
      return {
        background: style.backgroundColor,
        border: style.borderColor,
        indicator: indicator.backgroundColor
      };
    });

    expect(activeSurface.background).not.toBe('rgba(0, 0, 0, 0)');
    expect(activeSurface.border).not.toBe('rgba(0, 0, 0, 0)');
    expect(activeSurface.indicator).not.toBe('rgba(0, 0, 0, 0)');
    expect(diagnostics.consoleErrors).toEqual([]);
    expect(diagnostics.requestFailures).toEqual([]);
  });

  test('separa modo compacto de overlay al cambiar desktop, móvil y desktop', async ({ page }) => {
    await page.setViewportSize(desktopViewport);
    await configureAuthenticatedRuntime(page);
    await page.goto('/dashboard');

    const sidenav = page.locator('mat-sidenav.primary-sidenav');
    const toggle = page.locator('button.menu-toggle');

    await toggle.click();
    await expect(sidenav).toHaveClass(/compact/);
    await expect(toggle).toHaveAttribute('aria-label', 'Expandir navegación');

    await page.setViewportSize(mobileViewport);
    await expect(sidenav).toHaveClass(/mat-drawer-over/);
    await expect(sidenav).toBeHidden();
    await expect(toggle).toHaveAttribute('aria-label', 'Abrir navegación');

    await toggle.click();
    await expect(sidenav).toBeVisible();
    await expect(page.locator('.mat-drawer-backdrop.mat-drawer-shown')).toBeVisible();

    const parent = sidenav.locator('button.nav-parent[data-menu-item-id="2"]');
    if ((await parent.getAttribute('aria-expanded')) !== 'true') {
      await parent.click();
    }
    await expect(sidenav).toBeVisible();
    await sidenav.locator('a.nav-item[data-menu-item-id="21"]').click();
    await expect(sidenav).toBeHidden();
    await expect(page.locator('.mat-drawer-backdrop.mat-drawer-shown')).toHaveCount(0);

    await page.setViewportSize(desktopViewport);
    await expect(sidenav).toHaveClass(/mat-drawer-side/);
    await expect(sidenav).toBeVisible();
    await expect(sidenav).toHaveClass(/compact/);
    await expect(toggle).toHaveAttribute('aria-label', 'Expandir navegación');

    await toggle.click();
    await expect(toggle).toHaveAttribute('aria-label', 'Contraer navegación');
    await expect(sidenav).not.toHaveClass(/compact/);
    await toggle.click();
    await expect(toggle).toHaveAttribute('aria-label', 'Expandir navegación');
    await expect(sidenav).toHaveClass(/compact/);

    await page.setViewportSize(mobileViewport);
    await expect(toggle).toHaveAttribute('aria-label', 'Abrir navegación');
    await toggle.click();
    await expect(sidenav).toBeVisible();
    await sidenav.locator('button.nav-parent[data-menu-item-id="2"]').focus();
    await page.keyboard.press('Escape');
    await expect(sidenav).toBeHidden();
    await expect(toggle).toBeFocused();

    await toggle.click();
    const backdrop = page.locator('.mat-drawer-backdrop.mat-drawer-shown');
    await expect(backdrop).toBeVisible();
    await backdrop.click({ position: { x: 380, y: 120 } });
    await expect(sidenav).toBeHidden();
    await expect(page.locator('.mat-drawer-backdrop.mat-drawer-shown')).toHaveCount(0);
  });
});

async function expectControlSize(control: Locator): Promise<void> {
  const box = await control.boundingBox();
  expect(box?.width ?? 0).toBeGreaterThanOrEqual(44);
  expect(box?.height ?? 0).toBeGreaterThanOrEqual(44);
  await expect(control.locator('svg.menu-toggle-icon')).toBeVisible();
}

async function expectReadableOnSidenav(target: Locator, minimumContrast: number): Promise<void> {
  const colors = await target.evaluate((element) => {
    const sidenav = element.closest('mat-sidenav');
    return {
      foreground: getComputedStyle(element).color,
      background: getComputedStyle(sidenav ?? document.body).backgroundColor
    };
  });

  expect(colors.foreground).not.toBe('rgba(0, 0, 0, 0)');
  expect(colors.foreground).not.toBe('transparent');
  expect(colors.foreground).not.toBe('rgb(0, 0, 0)');
  expect(contrastRatio(colors.foreground, colors.background)).toBeGreaterThanOrEqual(
    minimumContrast
  );
}

function contrastRatio(foreground: string, background: string): number {
  const foregroundLuminance = luminance(parseColor(foreground));
  const backgroundLuminance = luminance(parseColor(background));
  const lighter = Math.max(foregroundLuminance, backgroundLuminance);
  const darker = Math.min(foregroundLuminance, backgroundLuminance);
  return (lighter + 0.05) / (darker + 0.05);
}

function parseColor(value: string): [number, number, number] {
  const channels = value.match(/\d+(?:\.\d+)?/g)?.slice(0, 3).map(Number);
  if (!channels || channels.length !== 3) {
    throw new Error(`Color CSS no soportado: ${value}`);
  }
  return channels as [number, number, number];
}

function luminance([red, green, blue]: [number, number, number]): number {
  const normalize = (channel: number): number => {
    const value = channel / 255;
    return value <= 0.03928 ? value / 12.92 : ((value + 0.055) / 1.055) ** 2.4;
  };
  return 0.2126 * normalize(red) + 0.7152 * normalize(green) + 0.0722 * normalize(blue);
}

function monitorDiagnostics(page: Page): {
  consoleErrors: string[];
  requestFailures: string[];
} {
  const diagnostics = {
    consoleErrors: [] as string[],
    requestFailures: [] as string[]
  };

  page.on('console', (message) => {
    if (message.type() === 'error') {
      diagnostics.consoleErrors.push(message.text());
    }
  });
  page.on('requestfailed', (request) => {
    diagnostics.requestFailures.push(
      `${request.method()} ${new URL(request.url()).pathname} ${request.failure()?.errorText ?? ''}`.trim()
    );
  });

  return diagnostics;
}

async function configureAuthenticatedRuntime(page: Page): Promise<void> {
  const token = createUnsignedJwt({
    unique_name: 'ui.shell.regression',
    name: 'Usuario de validación',
    uid: 'ui-shell-regression',
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
          username: 'ui.shell.regression',
          fullName: 'Usuario de validación',
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
        {
          id: 1,
          label: 'Panel principal',
          route: '/dashboard',
          icon: 'dashboard',
          exact: true,
          order: 1
        },
        {
          id: 2,
          label: 'Operación',
          route: '/transactions',
          icon: 'account_balance',
          exact: false,
          order: 2,
          children: [
            {
              id: 21,
              label: 'Ciclos',
              route: '/dashboard',
              icon: 'schedule',
              exact: true,
              order: 1
            }
          ]
        },
        {
          id: 3,
          label: 'Consulta auxiliar',
          route: '/icon-fallback',
          icon: 'unknown_semantic_key',
          exact: true,
          order: 3
        },
        {
          id: 4,
          label: 'Reportes',
          route: '/reports',
          icon: 'summarize',
          exact: false,
          order: 4,
          children: [
            {
              id: 41,
              label: 'Reporte auxiliar',
              route: '/report-fallback',
              icon: 'receipt_long',
              exact: true,
              order: 1
            }
          ]
        }
      ])
    });
  });

  await page.route(/\/api\/navigation-logs$/, async (route) => {
    await route.fulfill({ status: 204, body: '' });
  });
}

function createUnsignedJwt(payload: Record<string, unknown>): string {
  const encode = (value: Record<string, unknown>): string =>
    Buffer.from(JSON.stringify(value)).toString('base64url');

  return `${encode({ alg: 'none', typ: 'JWT' })}.${encode(payload)}.`;
}
