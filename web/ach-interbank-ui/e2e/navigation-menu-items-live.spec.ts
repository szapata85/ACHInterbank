import { expect, Page, test } from '@playwright/test';
import { mkdir } from 'node:fs/promises';
import { resolve } from 'node:path';

const spaUrl = (process.env['ACH_UI_URL'] ?? process.env['E2E_BASE_URL'] ?? 'http://localhost:743').replace(/\/+$/, '');
const apiUrl = (process.env['ACH_API_URL'] ?? 'http://localhost:843').replace(/\/+$/, '');
const adminUser = process.env['E2E_ADMIN_USER'] ?? process.env['ACH_USER'] ?? 'admin';
const adminPassword = process.env['E2E_ADMIN_PASSWORD'] ?? process.env['ACH_PASS'] ?? 'Admin123!';
const evidenceDir = resolve(
  process.cwd(),
  '../../docs/ux/evidencias/navigation-menu-items-angular-material/final'
);

interface MenuItemDto {
  id: number;
  parentId?: number | null;
  label: string;
  route: string;
  icon?: string | null;
  order: number;
  exact: boolean;
  isActive: boolean;
  roleIds: string[];
  permissionIds: string[];
  children?: MenuItemDto[];
}

test.describe('Administración del menú - runtime real', () => {
  test('maestro-detalle Material, persistencia segura, jerarquía y responsive', async ({ page }) => {
    test.setTimeout(150_000);
    await mkdir(evidenceDir, { recursive: true });

    const suffix = Date.now();
    const marker = `E2E_NAV_${suffix}`;
    const parentLabel = `${marker}_PADRE`;
    const editedParentLabel = `${parentLabel}_EDIT`;
    const childLabel = `${marker}_HIJO`;
    const parentRoute = `/e2e-nav-${suffix}`;
    const childRoute = `${parentRoute}/child`;
    let token = '';
    const menuWrites: string[] = [];
    const consoleErrors: string[] = [];
    const pageErrors: string[] = [];
    const failedRequests: string[] = [];
    const unexpectedResponses: string[] = [];

    try {
      token = await login(page);

      page.on('console', (message) => {
        if (message.type() === 'error') {
          consoleErrors.push(message.text());
        }
      });
      page.on('pageerror', (error) => pageErrors.push(error.message));
      page.on('requestfailed', (request) => {
        const path = new URL(request.url()).pathname;
        if (path.startsWith('/api/')) {
          failedRequests.push(`${request.method()} ${path}`);
        }
      });
      page.on('response', (response) => {
        const path = new URL(response.url()).pathname;
        if (path.startsWith('/api/') && response.status() >= 400) {
          unexpectedResponses.push(`${response.status()} ${response.request().method()} ${path}`);
        }
      });
      page.on('request', (request) => {
        const path = new URL(request.url()).pathname;
        if (
          /^\/api\/navigation\/menu-items(?:\/\d+)?$/.test(path)
          && ['POST', 'PUT', 'PATCH', 'DELETE'].includes(request.method())
        ) {
          menuWrites.push(`${request.method()} ${path}`);
        }
      });

      await page.goto(`${spaUrl}/navigation/menu-items`, { waitUntil: 'networkidle' });
      await expect(page.getByRole('heading', { name: 'Administración del menú' })).toBeVisible();
      await expect(page.getByRole('tree', { name: 'Árbol de opciones del menú' })).toBeVisible();
      await expect(page.getByLabel('Buscar opciones')).toBeVisible();
      await expect(page.getByText('Ninguna opción seleccionada')).toBeVisible();

      const selectableNodes = page.locator('.navigation-admin__node-main');
      expect(await selectableNodes.count()).toBeGreaterThan(0);
      await selectableNodes.first().click();
      await expect(page.getByText('Opción seleccionada')).toBeVisible();
      await page.getByRole('button', { name: 'Editar opción', exact: true }).click();
      await expect(page.getByRole('button', { name: 'Guardar cambios', exact: true })).toBeDisabled();
      await page.getByRole('button', { name: 'Cancelar', exact: true }).click();
      await expect(page.getByText('Opción seleccionada')).toBeVisible();
      expect(menuWrites, 'Abrir, seleccionar, editar y cancelar no debe escribir').toEqual([]);

      await assertResponsiveView(page, 1440, 900, 'menu-items-desktop.png');
      await assertResponsiveView(page, 768, 1024, 'menu-items-tablet.png');
      await assertResponsiveView(page, 390, 844, 'menu-items-mobile.png');
      await page.setViewportSize({ width: 1440, height: 900 });

      await page.locator('.navigation-admin__primary-action').click();
      await expect(page.getByRole('button', { name: 'Crear opción', exact: true })).toBeDisabled();
      await page.getByLabel('Etiqueta visible').fill('Temporal');
      await page.getByLabel('Etiqueta visible').fill('');
      await page.getByRole('textbox', { name: 'Ruta', exact: true }).fill('ruta con espacios');
      await page.getByRole('spinbutton', { name: 'Orden', exact: true }).fill('-1');
      await page.getByRole('combobox', { name: 'Icono', exact: true }).click();
      await page.keyboard.press('Escape');
      await expect(page.getByText('La etiqueta es obligatoria.')).toBeVisible();
      await expect(page.getByText('Usa una ruta interna válida, sin espacios.')).toBeVisible();
      await expect(page.getByText('El orden no puede ser negativo.')).toBeVisible();
      await page.screenshot({
        path: resolve(evidenceDir, 'menu-item-validation-errors.png'),
        fullPage: true
      });
      expect(menuWrites, 'Las validaciones no deben escribir').toEqual([]);

      await page.getByLabel('Etiqueta visible').fill(parentLabel);
      await page.getByRole('textbox', { name: 'Ruta', exact: true }).fill(parentRoute);
      await page.getByRole('spinbutton', { name: 'Orden', exact: true }).fill('9000');
      await page.screenshot({
        path: resolve(evidenceDir, 'menu-item-form-desktop.png'),
        fullPage: true
      });

      const createStart = menuWrites.length;
      await Promise.all([
        page.waitForResponse((response) => (
          new URL(response.url()).pathname === '/api/navigation/menu-items'
          && response.request().method() === 'POST'
          && response.status() === 201
        )),
        page.getByRole('button', { name: 'Crear opción', exact: true }).click()
      ]);
      await expect(page.getByRole('button', {
        name: `Seleccionar ${parentLabel}, activo`,
        exact: true
      })).toBeVisible();
      expect(menuWrites.slice(createStart).filter((entry) => entry.startsWith('POST'))).toHaveLength(1);

      await page.getByLabel('Etiqueta visible').fill(editedParentLabel);
      const updateStart = menuWrites.length;
      await Promise.all([
        page.waitForResponse((response) => (
          /^\/api\/navigation\/menu-items\/\d+$/.test(new URL(response.url()).pathname)
          && response.request().method() === 'PUT'
          && response.status() === 200
        )),
        page.getByRole('button', { name: 'Guardar cambios', exact: true }).click()
      ]);
      expect(menuWrites.slice(updateStart).filter((entry) => entry.startsWith('PUT'))).toHaveLength(1);

      await page.reload({ waitUntil: 'networkidle' });
      await page.getByLabel('Buscar opciones').fill(editedParentLabel);
      await selectNode(page, editedParentLabel);
      await page.getByRole('button', { name: 'Nueva opción hija', exact: true }).click();
      await expect(page.getByText(`Se creará dentro de “${editedParentLabel}”.`)).toBeVisible();
      await page.getByLabel('Etiqueta visible').fill(childLabel);
      await page.getByRole('textbox', { name: 'Ruta', exact: true }).fill(childRoute);
      await page.getByRole('spinbutton', { name: 'Orden', exact: true }).fill('1');

      const childCreateStart = menuWrites.length;
      await Promise.all([
        page.waitForResponse((response) => (
          new URL(response.url()).pathname === '/api/navigation/menu-items'
          && response.request().method() === 'POST'
          && response.status() === 201
        )),
        page.getByRole('button', { name: 'Crear opción', exact: true }).click()
      ]);
      expect(menuWrites.slice(childCreateStart).filter((entry) => entry.startsWith('POST'))).toHaveLength(1);

      await page.getByRole('button', { name: 'Cancelar', exact: true }).click();
      await page.getByLabel('Buscar opciones').fill(editedParentLabel);
      await selectNode(page, editedParentLabel);
      await page.getByRole('button', { name: 'Editar opción', exact: true }).click();
      await page.getByRole('combobox', { name: 'Elemento padre', exact: true }).click();
      await expect(page.getByRole('option', { name: editedParentLabel, exact: true })).toHaveCount(0);
      await expect(page.getByRole('option', { name: childLabel, exact: true })).toHaveCount(0);
      await page.keyboard.press('Escape');
      await page.getByRole('button', { name: 'Cancelar', exact: true }).click();
      await page.getByLabel('Buscar opciones').fill(marker);
      await page.screenshot({
        path: resolve(evidenceDir, 'menu-item-hierarchy.png'),
        fullPage: true
      });

      await page.getByLabel('Buscar opciones').fill(childLabel);
      await selectNode(page, childLabel);
      await openItemActions(page, childLabel);
      await page.getByRole('menuitem', { name: 'Eliminar', exact: true }).click();
      await expect(page.getByRole('heading', { name: 'Eliminar opción' })).toBeVisible();
      await page.screenshot({
        path: resolve(evidenceDir, 'menu-item-confirmation.png'),
        fullPage: true
      });
      await Promise.all([
        page.waitForResponse((response) => (
          /^\/api\/navigation\/menu-items\/\d+$/.test(new URL(response.url()).pathname)
          && response.request().method() === 'DELETE'
          && response.status() === 204
        )),
        page.getByRole('button', { name: 'Eliminar opción', exact: true }).click()
      ]);

      await page.getByLabel('Buscar opciones').fill(editedParentLabel);
      await selectNode(page, editedParentLabel);
      await openItemActions(page, editedParentLabel);
      await page.getByRole('menuitem', { name: 'Eliminar', exact: true }).click();
      await Promise.all([
        page.waitForResponse((response) => (
          /^\/api\/navigation\/menu-items\/\d+$/.test(new URL(response.url()).pathname)
          && response.request().method() === 'DELETE'
          && response.status() === 204
        )),
        page.getByRole('button', { name: 'Eliminar opción', exact: true }).click()
      ]);

      const leftovers = await findTemporaryItems(page, token, marker);
      expect(leftovers, 'No deben quedar opciones temporales después de la prueba').toEqual([]);
      expect(consoleErrors, 'No debe haber errores de consola').toEqual([]);
      expect(pageErrors, 'No debe haber excepciones de página').toEqual([]);
      expect(failedRequests, 'No debe haber requests API fallidos').toEqual([]);
      expect(unexpectedResponses, 'No debe haber HTTP 4xx/5xx inesperados').toEqual([]);
    } finally {
      if (token) {
        await cleanupTemporaryItems(page, token, marker);
        const leftovers = await findTemporaryItems(page, token, marker);
        expect(leftovers, 'La limpieza final debe eliminar toda opción E2E').toEqual([]);
      }
    }
  });
});

async function login(page: Page): Promise<string> {
  await page.goto(`${spaUrl}/login`, { waitUntil: 'domcontentloaded' });
  await page.locator('input[formControlName="username"]').fill(adminUser);
  await page.locator('input[formControlName="password"]').fill(adminPassword);
  const [response] = await Promise.all([
    page.waitForResponse((candidate) => (
      new URL(candidate.url()).pathname === '/auth/login'
      && candidate.request().method() === 'POST'
      && candidate.status() === 200
    )),
    page.getByRole('button', { name: 'Ingresar' }).click()
  ]);
  await expect(page).not.toHaveURL(/\/login(?:\?.*)?$/);
  await page.waitForLoadState('networkidle');
  const payload = await response.json() as { data?: { token?: string } };
  expect(payload.data?.token, 'El login debe devolver un token').toBeTruthy();
  return payload.data!.token!;
}

async function assertResponsiveView(
  page: Page,
  width: number,
  height: number,
  screenshotName: string
): Promise<void> {
  await page.setViewportSize({ width, height });
  await expect(page.getByRole('heading', { name: 'Administración del menú' })).toBeVisible();
  await expect(page.getByLabel('Buscar opciones')).toBeVisible();
  await expect(page.locator('.navigation-admin__tree-panel')).toBeVisible();
  await expect(page.locator('.navigation-admin__form-panel')).toBeVisible();
  const overflow = await page.evaluate(() => document.body.scrollWidth - window.innerWidth);
  expect(overflow, `No debe existir overflow horizontal en ${width}x${height}`).toBeLessThanOrEqual(1);
  await page.screenshot({ path: resolve(evidenceDir, screenshotName), fullPage: true });
}

async function selectNode(page: Page, label: string): Promise<void> {
  const node = page.getByRole('button', { name: `Seleccionar ${label}, activo`, exact: true });
  await expect(node).toHaveCount(1);
  await node.click();
}

async function openItemActions(page: Page, label: string): Promise<void> {
  const trigger = page.getByRole('button', { name: `Acciones para ${label}`, exact: true });
  await expect(trigger).toHaveCount(1);
  await trigger.click();
}

async function findTemporaryItems(page: Page, token: string, marker: string): Promise<MenuItemDto[]> {
  const response = await page.request.get(`${apiUrl}/api/navigation/menu-items`, {
    headers: { Authorization: `Bearer ${token}` }
  });
  expect(response.ok(), 'La consulta de limpieza debe responder correctamente').toBeTruthy();
  const items = await response.json() as MenuItemDto[];
  return flatten(items).filter((item) => item.label.startsWith(marker));
}

async function cleanupTemporaryItems(page: Page, token: string, marker: string): Promise<void> {
  const temporary = await findTemporaryItems(page, token, marker);
  for (const item of temporary.sort((left, right) => depth(right, temporary) - depth(left, temporary))) {
    const response = await page.request.delete(`${apiUrl}/api/navigation/menu-items/${item.id}`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    expect([204, 404]).toContain(response.status());
  }
}

function flatten(items: MenuItemDto[]): MenuItemDto[] {
  return items.flatMap((item) => [item, ...flatten(item.children ?? [])]);
}

function depth(item: MenuItemDto, items: MenuItemDto[]): number {
  let result = 0;
  let parentId = item.parentId;
  while (parentId) {
    result += 1;
    parentId = items.find((candidate) => candidate.id === parentId)?.parentId;
  }
  return result;
}
