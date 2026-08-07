import { expect, Page, test } from '@playwright/test';
import { randomUUID } from 'node:crypto';
import { loginThroughUi } from './support/live-ui-auth';

const spaUrl = (process.env['E2E_BASE_URL'] ?? 'http://localhost:743').replace(/\/+$/, '');

test.describe.serial('Administración de usuarios en runtime Docker', () => {
  const consoleErrors: string[] = [];
  const unexpectedResponses: string[] = [];
  let testUserName = '';
  let testUserId = '';

  test.beforeEach(async ({ page }) => {
    page.on('console', (message) => {
      if (message.type() === 'error') consoleErrors.push(message.text());
    });
    page.on('pageerror', (error) => consoleErrors.push(error.message));
    page.on('response', (response) => {
      if ([404, 500].includes(response.status())) unexpectedResponses.push(`${response.status()} ${response.url()}`);
    });
    await loginThroughUi(page);
  });

  test('muestra el menú, las rutas y formularios humanizados', async ({ page }) => {
    await page.getByRole('button', { name: 'Usuarios' }).click();
    const managementLink = page.getByRole('link', { name: 'Administrar usuarios', exact: true });
    await expect(managementLink).toHaveCount(1);
    await expect(page.getByRole('link', { name: 'Identidad y colores', exact: true })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Reglas de contraseña', exact: true })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Bloqueo de acceso', exact: true })).toBeVisible();
    await managementLink.click();
    await expect(page).toHaveURL(`${spaUrl}/users/list`);
    await expect(page.getByRole('banner').getByRole('heading', { name: 'Administración de usuarios' })).toBeVisible();

    for (const [route, title] of [
      ['/users', 'Administración de usuarios'],
      ['/users/list', 'Administración de usuarios'],
      ['/users/new', 'Crear usuario'],
      ['/users/branding', 'Identidad visual'],
      ['/users/password-rules', 'Reglas de contraseña'],
      ['/users/login-lockout', 'Bloqueo de acceso']
    ] as const) {
      await page.goto(`${spaUrl}${route}`);
      await expect(page.getByRole('banner').getByRole('heading', { name: title })).toBeVisible();
    }

    await page.goto(`${spaUrl}/users/new`);
    await page.getByRole('button', { name: 'Crear usuario' }).click();
    await expect(page.getByText('Ingresa el nombre de usuario.')).toBeVisible();
    await expect(page.getByText('Ingresa un correo electrónico.')).toBeVisible();
    await expect(page.getByText('Ingresa una contraseña.')).toBeVisible();
    await expect(page.getByText('Selecciona al menos un perfil de acceso.')).toBeVisible();
    await expect(page.getByText('required', { exact: true })).toHaveCount(0);

    await page.getByLabel('Nombre de usuario').fill('cambios.pendientes');
    await page.getByRole('button', { name: 'Cancelar', exact: true }).click();
    const pendingChangesDialog = page.getByRole('dialog');
    await expect(pendingChangesDialog.getByRole('heading', { name: 'Tienes cambios sin guardar' })).toBeVisible();
    await page.keyboard.press('Escape');
    await expect(pendingChangesDialog).toHaveCount(0);
    await expect(page).toHaveURL(`${spaUrl}/users/new`);
    await page.getByRole('button', { name: 'Cancelar', exact: true }).click();
    await pendingChangesDialog.getByRole('button', { name: 'Salir sin guardar', exact: true }).click();
    await expect(page).toHaveURL(`${spaUrl}/users/list`);
  });

  test('crea, edita, administra perfiles y desactiva un usuario sintético', async ({ page }) => {
    testUserName = `e2e.users.${randomUUID().replace(/-/g, '').slice(0, 12)}`;
    let createRequests = 0;
    let roleSaveRequests = 0;
    page.on('request', (request) => {
      if (request.method() === 'POST' && new URL(request.url()).pathname === '/api/users') {
        createRequests++;
      }
      if (request.method() === 'POST' && /\/api\/users\/[\w-]+\/roles$/.test(new URL(request.url()).pathname)) {
        roleSaveRequests++;
      }
    });
    await page.goto(`${spaUrl}/users/new`);
    await page.getByLabel('Nombre de usuario').fill(testUserName);
    await page.getByLabel('Nombre completo').fill('Usuario sintético E2E');
    await page.getByLabel('Correo electrónico').fill(`${testUserName}@example.com`);
    await page.getByLabel('Contraseña').fill(`E2e!${randomUUID().replace(/-/g, '').slice(0, 12)}A1`);
    await page.getByRole('combobox', { name: 'Perfiles de acceso' }).focus();
    await page.keyboard.press('Space');
    await page.getByRole('option', { name: 'Administrador', exact: true }).click();
    await page.keyboard.press('Escape');

    const createButton = page.getByRole('button', { name: 'Crear usuario', exact: true });
    await expect(createButton).toBeEnabled();
    await createButton.click();
    await expect(page).toHaveURL(`${spaUrl}/users/list`);
    expect(createRequests).toBe(1);
    await expect(page.getByText('Usuario creado correctamente.')).toBeVisible();

    await page.getByLabel('Buscar usuarios').fill(testUserName);
    await page.getByRole('button', { name: 'Buscar', exact: true }).click();
    const userRow = page.getByRole('row', { name: new RegExp(testUserName) });
    await expect(userRow).toBeVisible();
    await userRow.getByRole('button', { name: `Acciones para ${testUserName}` }).click();
    await page.getByRole('menuitem', { name: 'Editar usuario' }).click();
    await expect(page.getByRole('banner').getByRole('heading', { name: 'Editar usuario' })).toBeVisible();
    testUserId = new URL(page.url()).pathname.split('/')[2];
    await page.getByLabel('Teléfono').fill('3000000000');
    await page.getByRole('button', { name: 'Guardar cambios', exact: true }).click();
    await expect(page).toHaveURL(`${spaUrl}/users/list`);
    await expect(page.getByText('Los cambios se guardaron correctamente.')).toBeVisible();

    await page.getByLabel('Buscar usuarios').fill(testUserName);
    await page.getByRole('button', { name: 'Buscar', exact: true }).click();
    const updatedRow = page.getByRole('row', { name: new RegExp(testUserName) });
    await updatedRow.getByRole('button', { name: `Acciones para ${testUserName}` }).click();
    await page.getByRole('menuitem', { name: 'Administrar perfiles de acceso' }).click();
    await expect(page.getByRole('banner').getByRole('heading', { name: 'Administrar perfiles de acceso' })).toBeVisible();
    await expect(page.getByText('Administrador', { exact: true })).toBeVisible();
    const saveRoles = page.getByRole('button', { name: 'Guardar perfiles', exact: true });
    await saveRoles.click();
    await expect(page).toHaveURL(`${spaUrl}/users/list`);
    expect(roleSaveRequests).toBe(1);
    await expect(page.getByText('Los perfiles de acceso se actualizaron correctamente.')).toBeVisible();

    await page.getByLabel('Buscar usuarios').fill(testUserName);
    await page.getByRole('button', { name: 'Buscar', exact: true }).click();
    const finalRow = page.getByRole('row', { name: new RegExp(testUserName) });
    await finalRow.getByRole('button', { name: `Acciones para ${testUserName}` }).click();
    await page.getByRole('menuitem', { name: 'Desactivar usuario' }).click();
    await expect(page.getByText('La persona no podrá ingresar a la plataforma mientras su cuenta permanezca inactiva.')).toBeVisible();
    await page.getByRole('button', { name: 'Cancelar', exact: true }).click();
    await expect(page.getByText('La persona no podrá ingresar a la plataforma mientras su cuenta permanezca inactiva.')).toHaveCount(0);
    await finalRow.getByRole('button', { name: `Acciones para ${testUserName}` }).click();
    await page.getByRole('menuitem', { name: 'Desactivar usuario' }).click();
    await page.getByRole('button', { name: 'Desactivar usuario', exact: true }).click();
    await expect(page.getByText('El usuario fue desactivado correctamente.')).toBeVisible();
  });

  test('mantiene las pantallas recuperadas dentro de los viewports soportados', async ({ page }) => {
    test.setTimeout(120_000);
    expect(testUserId).not.toBe('');
    const routes = [
      '/users',
      '/users/list',
      '/users/new',
      `/users/${testUserId}/edit`,
      `/users/${testUserId}/roles`,
      '/users/branding',
      '/users/password-rules',
      '/users/login-lockout'
    ];

    for (const viewport of [
      { width: 1440, height: 900 },
      { width: 1024, height: 768 },
      { width: 768, height: 1024 },
      { width: 390, height: 844 }
    ]) {
      await page.setViewportSize(viewport);
      for (const route of routes) {
        await page.goto(`${spaUrl}${route}`);
        await page.waitForLoadState('networkidle');
        await expect(page.locator('main')).toBeVisible();
        expect(await page.locator('body').evaluate((body) => body.scrollWidth <= window.innerWidth)).toBeTruthy();
      }
    }
  });

  test.afterAll(() => {
    expect(consoleErrors.filter((error) => !/favicon/i.test(error))).toEqual([]);
    expect(unexpectedResponses).toEqual([]);
  });
});
