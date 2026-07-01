import { expect, Page, test } from '@playwright/test';

type NavigationMenuItem = {
  id: number;
  parentId: number | null;
  label: string;
  route: string;
  icon: string;
  order: number;
  exact: boolean;
  isActive: boolean;
  roleIds: string[];
  permissionIds: string[];
  children?: NavigationMenuItem[];
};

type SaveNavigationMenuItem = Omit<NavigationMenuItem, 'id' | 'children'>;

const refreshEndpoint = /\/auth\/refresh$/;
const navigationEndpoint = /\/navigation\/menu$/;

let createMenuItemHandler: ((payload: SaveNavigationMenuItem) => Promise<NavigationMenuItem> | NavigationMenuItem) | undefined;

test.use({ ignoreHTTPSErrors: true });

test.describe('Navigation menu items UX', () => {
  test.beforeEach(async ({ page }) => {
    createMenuItemHandler = undefined;
    await authenticate(page);
    await mockNavigation(page);
    await mockBackend(page);
  });

  test('NavigationMenuItems_ShouldLoadWithoutGridCollision_OnDesktop', async ({ page }, testInfo) => {
    const consoleErrors = collectCriticalConsoleErrors(page);

    await page.setViewportSize({ width: 1366, height: 768 });
    await page.goto('/navigation/menu-items');

    await expect(page).toHaveURL(/\/navigation\/menu-items$/);
    await expect(page.getByRole('heading', { name: 'Menú de navegación', exact: true })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Nueva opción de menú' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Estructura' })).toBeVisible();
    await expect(page.getByText('Administra los accesos visibles en la SPA')).toBeVisible();
    await expect(page.getByText('Roles permitidos')).toBeVisible();
    await expect(page.getByText('Permisos necesarios')).toBeVisible();

    const layout = page.locator('.navigation-admin__layout');
    await expect(layout).toBeVisible();
    await expect(layout).not.toHaveClass(/\bgrid\b/);

    const styleSnapshot = await layout.evaluate((element) => {
      const style = window.getComputedStyle(element);
      return {
        borderLeftWidth: style.borderLeftWidth,
        borderLeftStyle: style.borderLeftStyle,
        borderLeftColor: style.borderLeftColor,
        boxShadow: style.boxShadow,
        className: element.className
      };
    });

    expect(styleSnapshot.className).toContain('navigation-admin__layout');
    expect(styleSnapshot.className).not.toMatch(/\bgrid\b/);
    expect(styleSnapshot.borderLeftWidth).toBe('0px');
    expect(styleSnapshot.borderLeftStyle).toBe('none');
    expect(styleSnapshot.boxShadow).toBe('none');

    const formCard = page.locator('.navigation-admin__layout > .card').first();
    const formCardStyles = await formCard.evaluate((element) => {
      const style = window.getComputedStyle(element);
      return {
        borderLeftWidth: style.borderLeftWidth,
        borderLeftStyle: style.borderLeftStyle,
        borderLeftColor: style.borderLeftColor
      };
    });
    expect(formCardStyles.borderLeftWidth).toBe('1px');
    expect(formCardStyles.borderLeftStyle).toBe('solid');

    await assertNoHorizontalOverflow(page);
    await page.screenshot({ path: testInfo.outputPath('navigation-menu-items-desktop-initial.png'), fullPage: true });
    await assertNavigationGridLoaded(page);
    expect(consoleErrors()).toEqual([]);
  });

  test('NavigationMenuItems_ShouldSupportCreateEditCancelAndIconSelector', async ({ page }, testInfo) => {
    const consoleErrors = collectCriticalConsoleErrors(page);

    await page.setViewportSize({ width: 1366, height: 768 });
    await page.goto('/navigation/menu-items');

    await expect(page.getByRole('heading', { name: 'Nueva opción de menú' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Crear opción' })).toBeDisabled();
    await expect(page.getByRole('button', { name: 'Guardar cambios' })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Cancelar edición' })).toHaveCount(0);
    await expect(page.getByText('Usa una ruta interna de la SPA')).toBeVisible();
    await expect(page.getByText('Sin selección, no se exige permiso adicional desde el menú.')).toBeVisible();

    await page.getByPlaceholder('Texto visible').fill('Reportes QA');
    await page.getByPlaceholder('/ruta').fill('/qa/reportes');
    await expect(page.getByRole('button', { name: 'Crear opción' })).toBeEnabled();

    const iconTrigger = page.locator('.icon-select-trigger');
    await iconTrigger.click();
    await expect(page.locator('.icon-options')).toBeVisible();
    await expect(page.locator('.icon-option').filter({ hasText: 'dashboard' })).toBeVisible();
    await iconTrigger.click();
    await expect(page.locator('.icon-options')).toBeHidden();

    await assertNavigationGridLoaded(page);
    await page.getByRole('button', { name: 'Editar' }).first().click();
    await expect(page.getByRole('heading', { name: 'Editar opción de menú' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Guardar cambios' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Cancelar edición' }).first()).toBeVisible();
    await expect(page.getByPlaceholder('Texto visible')).toHaveValue('Panel principal');
    await expect(page.getByPlaceholder('/ruta')).toHaveValue('/dashboard');

    await page.screenshot({ path: testInfo.outputPath('navigation-menu-items-edit-mode.png'), fullPage: true });

    await page.getByRole('button', { name: 'Cancelar edición' }).first().click();
    await expect(page.getByRole('heading', { name: 'Nueva opción de menú' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Crear opción' })).toBeDisabled();
    await expect(page.getByPlaceholder('Texto visible')).toHaveValue('');
    await expect(page.getByPlaceholder('/ruta')).toHaveValue('');

    await expect(page.getByText('Panel principal')).toBeVisible();
    expect(consoleErrors()).toEqual([]);
  });

  test('NavigationMenuItems_ShouldPreventDuplicateCreateRequestsWhileSaving', async ({ page }) => {
    const consoleErrors = collectCriticalConsoleErrors(page);
    let createRequestCount = 0;
    let releaseCreateRequest!: () => void;
    const createRequestPending = new Promise<void>((resolve) => {
      releaseCreateRequest = resolve;
    });

    createMenuItemHandler = async (payload) => {
      createRequestCount += 1;
      await createRequestPending;
      return { id: 50, ...payload };
    };

    await page.setViewportSize({ width: 1366, height: 768 });
    await page.goto('/navigation/menu-items');
    await page.getByPlaceholder('Texto visible').fill('Opción QA');
    await page.getByPlaceholder('/ruta').fill('/qa/menu');

    const submit = page.getByRole('button', { name: 'Crear opción' });
    await expect(submit).toBeEnabled();
    await submit.click();
    await expect(page.getByRole('button', { name: 'Guardando...' })).toBeDisabled();
    await expect.poll(() => createRequestCount).toBe(1);
    await page.getByRole('button', { name: 'Guardando...' }).dblclick({ force: true });

    expect(createRequestCount).toBe(1);
    releaseCreateRequest();
    await expect(page.getByRole('heading', { name: 'Nueva opción de menú' })).toBeVisible();
    expect(createRequestCount).toBe(1);
    expect(consoleErrors()).toEqual([]);
  });

  test('NavigationMenuItems_ShouldRemainUsable_OnMobileViewport', async ({ page }, testInfo) => {
    const consoleErrors = collectCriticalConsoleErrors(page);

    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto('/navigation/menu-items');

    await expect(page.getByRole('heading', { name: 'Menú de navegación', exact: true })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Nueva opción de menú' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Estructura' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Crear opción' })).toBeVisible();
    await expect(page.locator('.navigation-admin__layout')).not.toHaveClass(/\bgrid\b/);

    const layoutStyle = await page.locator('.navigation-admin__layout').evaluate((element) => {
      const style = window.getComputedStyle(element);
      return {
        gridTemplateColumns: style.gridTemplateColumns,
        borderLeftWidth: style.borderLeftWidth,
        borderLeftStyle: style.borderLeftStyle,
        className: element.className
      };
    });

    expect(layoutStyle.className).not.toMatch(/\bgrid\b/);
    expect(layoutStyle.borderLeftWidth).toBe('0px');
    expect(layoutStyle.borderLeftStyle).toBe('none');

    await page.screenshot({ path: testInfo.outputPath('navigation-menu-items-mobile.png'), fullPage: true });
    await assertNoHorizontalOverflow(page);
    expect(consoleErrors()).toEqual([]);
  });
});

async function mockNavigation(page: Page): Promise<void> {
  await page.route(navigationEndpoint, async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        { id: 1, label: 'Panel principal', route: '/dashboard' },
        { id: 8, label: 'Navegación', route: '/navigation', children: [
          { id: 81, label: 'Menús', route: '/navigation/menu-items' },
          { id: 82, label: 'Logs de navegación', route: '/navigation-logs' }
        ] }
      ])
    });
  });
}

async function mockBackend(page: Page): Promise<void> {
  await page.route(/(?:https?:\/\/[^/]+)?\/(?:api\/.*|navigation\/menu-items(?:\/\d+)?)(?:\?.*)?$/i, async route => {
    if (route.request().resourceType() === 'document') {
      await route.fallback();
      return;
    }

    const url = new URL(route.request().url());
    const path = url.pathname;
    const method = route.request().method().toUpperCase();

    if (method === 'GET' && isNavigationMenuItemsPath(path)) {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildNavigationMenuItems()) });
      return;
    }

    if (method === 'POST' && isNavigationMenuItemsPath(path)) {
      const payload = (await route.request().postDataJSON()) as SaveNavigationMenuItem;
      const created = createMenuItemHandler ? await createMenuItemHandler(payload) : { id: 40, ...payload };
      await route.fulfill({ status: 201, contentType: 'application/json', body: JSON.stringify(created) });
      return;
    }

    if (method === 'PUT' && isNavigationMenuItemPath(path)) {
      const payload = (await route.request().postDataJSON()) as SaveNavigationMenuItem;
      const id = Number(path.split('/').pop());
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ id, ...payload }) });
      return;
    }

    if (method === 'GET' && path === '/api/roles') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildRoles()) });
      return;
    }

    if (method === 'GET' && path === '/api/permissions') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildPermissions()) });
      return;
    }

    if (method === 'GET' && path === '/api/users/branding') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          publicLogo: null,
          privateLogo: null,
          publicBackground: null,
          privateBackground: null,
          sidebarBackground: null
        })
      });
      return;
    }

    if (method === 'POST' && path === '/api/navigation-logs') {
      await route.fulfill({ status: 204 });
      return;
    }

    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({}) });
  });
}

function isNavigationMenuItemsPath(path: string): boolean {
  return path === '/navigation/menu-items' || path === '/api/navigation/menu-items';
}

function isNavigationMenuItemPath(path: string): boolean {
  return /^\/navigation\/menu-items\/\d+$/.test(path) || /^\/api\/navigation\/menu-items\/\d+$/.test(path);
}

async function authenticate(page: Page): Promise<void> {
  const token = createUnsignedJwt({
    unique_name: 'qa.navigation',
    name: 'Usuario QA Navegación',
    uid: 'qa-navigation',
    role: ['Admin'],
    permission: ['CanManageUsers', 'CanReadAch', 'CanManageAch'],
    exp: Math.floor(Date.now() / 1000) + 3600,
    iat: Math.floor(Date.now() / 1000)
  });

  await page.addInitScript((accessToken) => {
    window.sessionStorage.setItem('ach.interbank.access_token', accessToken);
  }, token);

  await page.route(refreshEndpoint, async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        sucess: true,
        data: {
          token,
          username: 'qa.navigation',
          fullName: 'Usuario QA Navegación',
          roles: ['Admin'],
          permissions: ['CanManageUsers', 'CanReadAch', 'CanManageAch']
        }
      })
    });
  });
}

function buildNavigationMenuItems(): NavigationMenuItem[] {
  return [
    {
      id: 1,
      parentId: null,
      label: 'Panel principal',
      route: '/dashboard',
      icon: 'dashboard',
      order: 1,
      exact: true,
      isActive: true,
      roleIds: [],
      permissionIds: []
    },
    {
      id: 2,
      parentId: null,
      label: 'Transacciones',
      route: '/transactions',
      icon: 'payments',
      order: 2,
      exact: false,
      isActive: true,
      roleIds: ['admin'],
      permissionIds: ['CanReadAch'],
      children: [
        {
          id: 3,
          parentId: 2,
          label: 'Carga masiva',
          route: '/transactions/bulk-ingestion/upload',
          icon: 'upload',
          order: 1,
          exact: false,
          isActive: true,
          roleIds: ['admin'],
          permissionIds: ['CanManageAch']
        }
      ]
    }
  ];
}

function buildRoles(): Record<string, string>[] {
  return [
    { id: 'admin', name: 'Admin' },
    { id: 'operator', name: 'Operador ACH' }
  ];
}

function buildPermissions(): Record<string, string>[] {
  return [
    { id: 'CanReadAch', name: 'CanReadAch', description: 'Leer ACH' },
    { id: 'CanManageAch', name: 'CanManageAch', description: 'Administrar ACH' },
    { id: 'CanManageUsers', name: 'CanManageUsers', description: 'Administrar usuarios' }
  ];
}

function collectCriticalConsoleErrors(page: Page): () => string[] {
  const errors: string[] = [];

  page.on('console', (message) => {
    if (message.type() === 'error') {
      errors.push(message.text());
    }
  });

  page.on('pageerror', (error) => {
    errors.push(error.message);
  });

  return () => errors;
}

async function assertNoHorizontalOverflow(page: Page): Promise<void> {
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
  expect(overflow).toBeLessThanOrEqual(2);
}

async function assertNavigationGridLoaded(page: Page): Promise<void> {
  const main = page.locator('main');
    await expect(main.getByText('No fue posible cargar el menú de navegación')).toHaveCount(0);
    await expect(main.getByRole('treegrid')).toBeVisible();
    await expect(main.getByRole('gridcell', { name: 'Panel principal' })).toBeVisible();
}

function createUnsignedJwt(payload: Record<string, unknown>): string {
  const header = { alg: 'none', typ: 'JWT' };
  return `${base64UrlEncode(JSON.stringify(header))}.${base64UrlEncode(JSON.stringify(payload))}.`;
}

function base64UrlEncode(value: string): string {
  return Buffer.from(value)
    .toString('base64')
    .replace(/=/g, '')
    .replace(/\+/g, '-')
    .replace(/\//g, '_');
}
