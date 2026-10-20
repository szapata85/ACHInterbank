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

interface CatalogEntry {
  id: string;
  name: string;
  description?: string | null;
}

test.describe('Administración del menú - runtime real', () => {
  test('hotfix: formulario sin solapamientos y etiquetas administrativas comprensibles', async ({ page }) => {
    test.setTimeout(180_000);
    await mkdir(evidenceDir, { recursive: true });

    const writes: string[] = [];
    const consoleErrors: string[] = [];
    const pageErrors: string[] = [];
    const failedRequests: string[] = [];
    const unexpectedResponses: string[] = [];

    const token = await login(page);
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
        writes.push(`${request.method()} ${path}`);
      }
    });

    const [rolesResponse, permissionsResponse] = await Promise.all([
      page.request.get(`${apiUrl}/api/roles`, { headers: { Authorization: `Bearer ${token}` } }),
      page.request.get(`${apiUrl}/api/permissions`, { headers: { Authorization: `Bearer ${token}` } })
    ]);
    expect(rolesResponse.ok()).toBeTruthy();
    expect(permissionsResponse.ok()).toBeTruthy();
    const roles = await rolesResponse.json() as CatalogEntry[];
    const permissions = await permissionsResponse.json() as CatalogEntry[];
    const adminRole = roles.find((role) => role.name === 'Admin');
    const operatorRole = roles.find((role) => role.name === 'ACH.Operator');
    const manageUsersPermission = permissions.find((permission) => permission.name === 'CanManageUsers');
    expect(adminRole).toBeTruthy();
    expect(operatorRole).toBeTruthy();
    expect(manageUsersPermission).toBeTruthy();

    await page.setViewportSize({ width: 1440, height: 900 });
    await page.goto(`${spaUrl}/navigation/menu-items`);
    await page.waitForLoadState('networkidle');

    const preferredNode = page.locator('[aria-label^="Seleccionar Bloqueo de acceso,"]');
    const node = await preferredNode.count()
      ? preferredNode
      : page.locator('.navigation-admin__node-main').first();
    await expect(node).toHaveCount(1);
    await node.click();

    const viewports = [
      { width: 1440, height: 900, screenshot: 'menu-item-form-desktop-fixed.png' },
      { width: 768, height: 1024, screenshot: 'menu-item-form-tablet-fixed.png' },
      { width: 390, height: 844, screenshot: 'menu-item-form-mobile-fixed.png' }
    ];

    try {
      for (const viewport of viewports) {
        await page.setViewportSize({ width: viewport.width, height: viewport.height });
        await page.getByRole('button', { name: 'Editar opción', exact: true }).click();
        await expect(page.locator('.navigation-admin__form')).toBeVisible();
        await expect(page.locator('.navigation-admin__form mat-hint')).toHaveCount(7);
        await expect(page.getByText('Roles permitidos', { exact: true })).toBeVisible();
        await expect(page.getByText('Permisos necesarios', { exact: true })).toBeVisible();

        await assertNoFormFieldOverlap(page);
        await assertNoHorizontalOverflow(page, viewport.width, viewport.height);

        const roleSelect = page.locator('mat-select[formcontrolname="roleIds"]');
        await roleSelect.press('Enter');
        const adminOption = page.locator(`mat-option[data-role-id="${adminRole!.id}"]`);
        const operatorOption = page.locator(`mat-option[data-role-id="${operatorRole!.id}"]`);
        await expect(adminOption.locator('.navigation-admin__select-option-label'))
          .toHaveText('Administrador');
        await expect(adminOption.locator('small')).toHaveText('Código interno: Admin');
        await expect(operatorOption.locator('.navigation-admin__select-option-label'))
          .toHaveText('Operador ACH');
        await expect(operatorOption.locator('small')).toHaveText('Código interno: ACH.Operator');
        expect(await operatorOption.getAttribute('data-role-id')).toBe(operatorRole!.id);
        if (await operatorOption.getAttribute('aria-selected') !== 'true') {
          await operatorOption.click();
        }
        await page.keyboard.press('Escape');
        await expect(roleSelect).toContainText('Operador ACH');

        const permissionSelect = page.locator('mat-select[formcontrolname="permissionIds"]');
        await permissionSelect.press('Enter');
        const manageUsersOption = page.locator(
          `mat-option[data-permission-id="${manageUsersPermission!.id}"]`
        );
        await expect(manageUsersOption.locator('.navigation-admin__select-option-label'))
          .toHaveText('Administrar usuarios');
        await expect(manageUsersOption.locator('small')).toHaveText('Código interno: CanManageUsers');
        expect(await manageUsersOption.getAttribute('data-permission-id')).toBe(manageUsersPermission!.id);
        if (viewport.width === 1440) {
          await page.screenshot({
            path: resolve(evidenceDir, 'menu-item-friendly-permissions.png'),
            fullPage: true
          });
        }
        if (await manageUsersOption.getAttribute('aria-selected') !== 'true') {
          await manageUsersOption.click();
        }
        await page.keyboard.press('Escape');
        await expect(permissionSelect).toContainText('Administrar usuarios');

        await page.screenshot({ path: resolve(evidenceDir, viewport.screenshot), fullPage: true });

        await page.getByRole('textbox', { name: 'Etiqueta visible', exact: true }).fill('');
        await page.getByRole('textbox', { name: 'Ruta', exact: true }).fill('ruta con espacios no permitidos');
        const orderInput = page.locator('input[formcontrolname="order"]');
        await orderInput.fill('-1');
        await orderInput.press('Tab');
        await expect(page.getByText('La etiqueta es obligatoria.', { exact: true })).toBeVisible();
        await expect(page.getByText('Usa una ruta interna válida, sin espacios.', { exact: true })).toBeVisible();
        await expect(page.getByText('El orden no puede ser negativo.', { exact: true })).toBeVisible();
        await assertNoFormFieldOverlap(page);
        await assertNoHorizontalOverflow(page, viewport.width, viewport.height);

        if (viewport.width === 1440) {
          await page.screenshot({
            path: resolve(evidenceDir, 'menu-item-validation-errors-fixed.png'),
            fullPage: true
          });
        }

        await page.getByRole('button', { name: 'Cancelar', exact: true }).click();
      }
    } finally {
      const cancel = page.getByRole('button', { name: 'Cancelar', exact: true });
      if (await cancel.isVisible()) {
        await cancel.click();
      }
    }

    expect(writes, 'Abrir, seleccionar y cancelar no debe escribir').toEqual([]);
    expect(consoleErrors, 'No debe haber errores de consola').toEqual([]);
    expect(pageErrors, 'No debe haber excepciones de página').toEqual([]);
    expect(failedRequests, 'No debe haber requests API fallidos').toEqual([]);
    expect(unexpectedResponses, 'No debe haber HTTP 4xx/5xx inesperados').toEqual([]);
  });

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

async function assertNoFormFieldOverlap(page: Page): Promise<void> {
  const violations = await page.locator('.navigation-admin__form-grid').evaluate((grid) => {
    const tolerance = 1;
    const intersects = (left: DOMRect, right: DOMRect) => (
      left.left < right.right - tolerance
      && left.right > right.left + tolerance
      && left.top < right.bottom - tolerance
      && left.bottom > right.top + tolerance
    );
    const fields = Array.from(grid.querySelectorAll<HTMLElement>('mat-form-field'))
      .filter((field) => field.getClientRects().length > 0);
    const issues: string[] = [];

    fields.forEach((field, index) => {
      const fieldRect = field.getBoundingClientRect();
      const label = field.querySelector('mat-label')?.textContent?.trim() ?? `campo ${index + 1}`;
      const messages = Array.from(field.querySelectorAll<HTMLElement>('mat-hint, mat-error'))
        .filter((message) => {
          const style = getComputedStyle(message);
          return style.display !== 'none' && style.visibility !== 'hidden' && message.getClientRects().length > 0;
        });

      for (const message of messages) {
        const messageRect = message.getBoundingClientRect();
        if (messageRect.bottom > fieldRect.bottom + tolerance) {
          issues.push(`${label}: el texto auxiliar sale del mat-form-field`);
        }

        for (const nextField of fields.slice(index + 1)) {
          const nextOutline = nextField.querySelector<HTMLElement>('.mat-mdc-text-field-wrapper');
          if (nextOutline && intersects(messageRect, nextOutline.getBoundingClientRect())) {
            issues.push(`${label}: el texto auxiliar intersecta el siguiente control`);
          }
        }
      }

      for (const nextField of fields.slice(index + 1)) {
        const nextRect = nextField.getBoundingClientRect();
        const sharesHorizontalSpace = fieldRect.left < nextRect.right - tolerance
          && fieldRect.right > nextRect.left + tolerance;
        if (sharesHorizontalSpace && fieldRect.top < nextRect.top && fieldRect.bottom > nextRect.top + tolerance) {
          issues.push(`${label}: el mat-form-field se superpone con la fila siguiente`);
        }
      }
    });

    return [...new Set(issues)];
  });

  expect(violations, `Solapamientos detectados:\n${violations.join('\n')}`).toEqual([]);
}

async function assertNoHorizontalOverflow(page: Page, width: number, height: number): Promise<void> {
  const overflow = await page.evaluate(() => document.body.scrollWidth - window.innerWidth);
  expect(overflow, `No debe existir overflow horizontal en ${width}x${height}`).toBeLessThanOrEqual(1);
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
