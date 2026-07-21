import { expect, test, Page } from '@playwright/test';
import { mkdirSync } from 'node:fs';
import { resolve } from 'node:path';

const spa = process.env['ACH_UI_URL'] ?? 'http://localhost:743';
const api = process.env['ACH_API_URL'] ?? 'http://localhost:843';
const username = process.env['E2E_ADMIN_USER'] ?? process.env['ACH_USER'] ?? 'admin';
const password = process.env['E2E_ADMIN_PASSWORD'] ?? process.env['ACH_PASS'];
const evidence = resolve(process.cwd(), '../../artifacts/local');

test.describe.serial('Administración real de cámaras compensadoras', () => {
  test.skip(!password, 'E2E_ADMIN_PASSWORD o ACH_PASS es obligatorio para el flujo real.');
  test.setTimeout(120_000);
  test('flujo administrador completo en escritorio', async ({ page }) => {
    mkdirSync(evidence, { recursive: true });
    const jsErrors: string[] = [];
    const httpErrors: string[] = [];
    page.on('pageerror', error => jsErrors.push(error.message));
    page.on('response', response => { if (response.status() >= 500 || (response.status() === 404 && !response.url().includes('favicon'))) httpErrors.push(`${response.status()} ${response.url()}`); });
    const token = await login(page);

    await openFromMenu(page);

    const search = page.getByLabel('Buscar por código o nombre');
    await search.fill('ACHCOL'); await page.getByRole('button', { name: 'Buscar' }).click();
    await expect(page.getByText('Cargando cámaras compensadoras…')).toBeHidden(); expect(jsErrors).toEqual([]);
    await expect(page.getByText('ACHCOL', { exact: true })).toBeVisible();
    await search.fill('CENIT'); await page.getByRole('button', { name: 'Buscar' }).click(); await expect(page.getByRole('cell', { name: 'CENIT' }).first()).toBeVisible();
    await search.fill(''); await page.getByRole('button', { name: 'Buscar' }).click();

    const code = `NUEVARED${Date.now().toString().slice(-6)}`;
    await page.getByRole('button', { name: 'Crear cámara' }).click();
    await page.getByLabel('Código funcional').fill(` ${code.toLowerCase()} `);
    await page.getByLabel('Nombre', { exact: true }).fill('Nueva Red de Pruebas');
    await page.getByLabel('Código de origen').fill('900');
    await page.getByLabel('Zona horaria').fill('America/Bogota');
    await page.getByLabel('Estrategia de calendario').fill('Colombian');
    await expect(page.getByLabel('Estrategia operativa')).toHaveValue('');
    await page.screenshot({ path: resolve(evidence, 'clearing-house-create.png'), fullPage: true });
    await page.getByRole('button', { name: 'Guardar', exact: true }).click();
    await expect(page.getByText('Cámara compensadora guardada correctamente.')).toBeVisible();
    await search.fill(code); await page.getByRole('button', { name: 'Buscar' }).click();
    await expect(page.getByText(code, { exact: true })).toBeVisible();
    await page.screenshot({ path: resolve(evidence, 'clearing-houses-list.png'), fullPage: true });

    const row = page.locator('tr', { hasText: code });
    await row.getByRole('button', { name: 'Ver detalle' }).click();
    await expect(page.getByText('Configuración incompleta')).toBeVisible();
    await page.screenshot({ path: resolve(evidence, 'clearing-house-detail.png'), fullPage: true });
    await row.getByRole('button', { name: 'Editar' }).click();
    await page.getByLabel('Nombre', { exact: true }).fill('Nueva Red de Pruebas Editada');
    await page.getByRole('button', { name: 'Guardar', exact: true }).click();
    const detail = await page.request.get(`${api}/clearing-houses?search=${code}`, { headers: auth(token) });
    expect(detail.ok()).toBeTruthy();
    const item = (await detail.json()).items[0];

    await page.locator('tr', { hasText: code }).getByRole('link', { name: 'Administrar ciclos' }).click();
    expect(jsErrors).toEqual([]);
    await expect(page.getByRole('heading', { name: /Configuración de ciclos/i })).toBeVisible();
    await page.getByRole('button', { name: 'Nueva configuración' }).click();
    await page.getByLabel('Nombre del ciclo').fill('Ciclo propio E2E');
    await page.getByLabel('Inicio ventana operativa').fill('08:00');
    await page.getByLabel('Fin ventana operativa').fill('17:00');
    await page.getByLabel('Cutoff').fill('16:00');
    await page.getByRole('button', { name: 'Guardar versión' }).click();
    await expect(page.getByText('Configuración versionada correctamente.')).toBeVisible();
    await expect(page.getByText('Ciclo propio E2E', { exact: true })).toBeVisible();
    const cycles = await page.request.get(`${api}/clearing-house-cycle-configs?clearingHouseId=${item.id}`, { headers: auth(token) });
    expect(cycles.ok()).toBeTruthy();
    expect((await cycles.json()).some((x: { clearingHouseId: number; cycleName: string }) => x.clearingHouseId === item.id && x.cycleName === 'Ciclo propio E2E')).toBeTruthy();
    await page.screenshot({ path: resolve(evidence, 'clearing-house-cycles.png'), fullPage: true });

    await page.goBack(); await expect(page.locator('section.page h1')).toHaveText('Cámaras compensadoras'); await searchPage(page, code);
    let activeRow = page.locator('tr', { hasText: code });
    await activeRow.getByRole('button', { name: 'Activar' }).click();
    await expect(page.getByRole('alert')).toContainText('Estrategia operativa registrada');

    await activeRow.getByRole('button', { name: 'Editar' }).click();
    await page.getByLabel('Estrategia operativa').selectOption('ACH_COLOMBIA');
    await page.getByRole('button', { name: 'Guardar', exact: true }).click();
    await expect(page.getByText('Cámara compensadora guardada correctamente.')).toBeVisible();
    await searchPage(page, code);
    activeRow = page.locator('tr', { hasText: code });
    await expect(activeRow).toContainText('ACH_COLOMBIA');
    await activeRow.getByRole('button', { name: 'Activar' }).click(); await expect(activeRow.locator('.status')).toHaveText('Activa');
    let operational = await page.request.get(`${api}/clearing-houses/operational`, { headers: auth(token) });
    const operationalItem = (await operational.json()).find((x: { code: string }) => x.code === code);
    expect(operationalItem?.paymentRailCode).toBe('ACH_COLOMBIA');
    page.once('dialog', dialog => dialog.accept()); await activeRow.getByRole('button', { name: 'Desactivar' }).click();
    await expect(activeRow.locator('.status')).toHaveText('Inactiva');
    operational = await page.request.get(`${api}/clearing-houses/operational`, { headers: auth(token) });
    expect((await operational.json()).some((x: { code: string }) => x.code === code)).toBeFalsy();
    await activeRow.getByRole('button', { name: 'Activar' }).click();

    for (let i = 0; i < 2; i++) {
      const seed = await page.request.post(`${api}/maintenance/seed`, { headers: auth(token) }); expect(seed.ok(), await seed.text()).toBeTruthy();
    }
    await page.reload(); await login(page); await openFromMenu(page); await searchPage(page, code);
    await page.screenshot({ path: resolve(evidence, 'clearing-house-detail.png'), fullPage: true });
    page.once('dialog', dialog => dialog.accept()); await page.locator('tr', { hasText: code }).getByRole('button', { name: 'Desactivar' }).click();
    expect(jsErrors).toEqual([]); expect(httpErrors).toEqual([]); expect(await page.locator('body').innerText()).not.toContain('[object Object]');
  });

  test('vista móvil real', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 }); await login(page); await openFromMenu(page);
    await page.screenshot({ path: resolve(evidence, 'clearing-house-mobile.png'), fullPage: true });
    expect(await page.locator('body').innerText()).not.toContain('[object Object]');
  });

  test('RBAC real para consulta y usuario sin permiso', async ({ page }) => {
    const adminToken = await login(page);
    const suffix = Date.now().toString().slice(-7);
    const readerName = `ch-reader-${suffix}`;
    const deniedName = `ch-denied-${suffix}`;
    const testPassword = `Aa1!${suffix}Zz`;
    const roles = await page.request.get(`${api}/api/roles`, { headers: auth(adminToken) });
    const operatorRole = (await roles.json()).find((x: { name: string }) => x.name === 'ACH.Operator');
    expect(operatorRole).toBeTruthy();

    for (const user of [
      { userName: readerName, fullName: 'Consulta de cámaras E2E', roleIds: [operatorRole.id] },
      { userName: deniedName, fullName: 'Sin permiso de cámaras E2E', roleIds: [] }
    ]) {
      const created = await page.request.post(`${api}/api/users`, {
        headers: auth(adminToken),
        data: { ...user, email: `${user.userName}@example.com`, password: testPassword }
      });
      expect(created.status()).toBe(201);
      const location = created.headers()['location'];
      expect(location).toBeTruthy();
      const fetched = await page.request.get(new URL(location!, api).toString(), { headers: auth(adminToken) });
      expect(fetched.status()).toBe(200);
      expect((await fetched.json()).data.userName).toBe(user.userName);
    }

    await logoutIfNeeded(page);
    const readerToken = await loginAs(page, readerName, testPassword);
    await openFromMenu(page);
    await searchPage(page, 'ACHCOL');
    await page.locator('tr', { hasText: 'ACHCOL' }).getByRole('button', { name: 'Ver detalle' }).click();
    await expect(page.getByRole('button', { name: 'Crear cámara' })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Editar' })).toHaveCount(0);
    expect((await page.request.get(`${api}/clearing-houses`, { headers: auth(readerToken) })).status()).toBe(200);
    expect((await page.request.post(`${api}/clearing-houses`, { headers: auth(readerToken), data: {} })).status()).toBe(403);

    await logoutIfNeeded(page);
    const deniedToken = await loginAs(page, deniedName, testPassword);
    await expect(page.locator('a[href="/clearing-houses"]')).toHaveCount(0);
    await page.goto(`${spa}/clearing-houses`);
    await expect(page.getByRole('heading', { name: 'Cámaras compensadoras' })).toHaveCount(0);
    expect((await page.request.get(`${api}/clearing-houses`, { headers: auth(deniedToken) })).status()).toBe(403);

    const users = await page.request.get(`${api}/api/users?search=ch-${suffix}&page=1&pageSize=10`, { headers: auth(adminToken) });
    for (const user of (await users.json()).items ?? []) {
      const deactivated = await page.request.delete(`${api}/api/users/${user.id}`, { headers: auth(adminToken) });
      expect(deactivated.status()).toBe(204);
    }
  });
});

async function login(page: Page): Promise<string> {
  return loginAs(page, username, password!);
}
async function loginAs(page: Page, loginUsername: string, loginPassword: string): Promise<string> {
  const response = await page.request.post(`${api}/auth/login`, { data: { username: loginUsername, password: loginPassword } });
  expect(response.ok(), 'El login real debe responder 200.').toBeTruthy();
  const token = (await response.json()).data.token as string;
  await page.goto(`${spa}/login`); await page.locator('input[formControlName="username"]').fill(loginUsername); await page.locator('input[formControlName="password"]').fill(loginPassword);
  await page.getByRole('button', { name: 'Ingresar' }).click(); await expect(page).not.toHaveURL(/\/login$/);
  return token;
}
async function logoutIfNeeded(page: Page): Promise<void> {
  const logout = page.getByRole('button', { name: /salir|cerrar sesión/i });
  if (await logout.count()) await logout.click();
}
async function searchPage(page: Page, code: string): Promise<void> { await page.getByLabel('Buscar por código o nombre').fill(code); await page.getByRole('button', { name: 'Buscar' }).click(); await expect(page.getByText(code, { exact: true })).toBeVisible(); }
async function openFromMenu(page: Page): Promise<void> { if ((page.viewportSize()?.width ?? 1000) < 600) await page.getByRole('button', { name: /menú principal/i }).click(); await page.locator('button[data-menu-item-id="5"]').click(); const link = page.locator('a[href="/clearing-houses"]'); await expect(link).toBeVisible(); await link.click(); await expect(page.locator('section.page h1')).toHaveText('Cámaras compensadoras'); }
function auth(token: string): Record<string, string> { return { Authorization: `Bearer ${token}` }; }
