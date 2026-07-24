import { expect, Page, test } from '@playwright/test';
import { Client } from 'pg';

const spa = process.env['ACH_UI_URL'] ?? process.env['E2E_BASE_URL'] ?? 'http://localhost:743';
const api = process.env['ACH_API_URL'] ?? process.env['E2E_API_BASE_URL'] ?? 'http://localhost:843';
const username = process.env['E2E_ADMIN_USER'] ?? process.env['ACH_USER'];
const password = process.env['E2E_ADMIN_PASSWORD'] ?? process.env['ACH_PASS'];
const thirdCameraCode = 'JOB6TEST';
const thirdCameraName = 'Red sintética JOB 6';
const profileCode = 'JOB6-THIRD-CAMERA';

const viewports = [
  { width: 1440, height: 900 },
  { width: 1280, height: 720 },
  { width: 1024, height: 768 },
  { width: 768, height: 1024 },
  { width: 390, height: 844 },
  { width: 360, height: 800 }
];

const criticalRoutes = [
  '/dashboard',
  '/clearing-houses',
  '/nacha-config-admin/perfiles',
  '/scheduler/tasks',
  '/nacha-security/certificates',
  '/integraciones/mappings',
  '/transactions/list',
  '/ach-responses'
];

test.describe.serial('JOB 6 - runtime integrado real', () => {
  let database: Client;

  test.beforeAll(async () => {
    if (!username || !password) {
      throw new Error('ACH_USER y ACH_PASS son obligatorios; el gate runtime no admite skip.');
    }

    const dbPassword = process.env['E2E_DB_PASSWORD'] ?? process.env['POSTGRES_PASSWORD'];
    if (!dbPassword) {
      throw new Error('E2E_DB_PASSWORD o POSTGRES_PASSWORD es obligatorio para el fixture aislado.');
    }

    database = new Client({
      host: process.env['E2E_DB_HOST'] ?? '127.0.0.1',
      port: Number(process.env['E2E_DB_PORT'] ?? '5432'),
      database: process.env['E2E_DB_NAME'] ?? 'ACHInterbank',
      user: process.env['E2E_DB_USER'] ?? 'example_user',
      password: dbPassword
    });
    await database.connect();
    await cleanupFixture(database);
    await database.query(
      `INSERT INTO "CatClearingHouse" ("Code", "Name", "IsActive", "CreatedAt", "UpdatedAt")
       VALUES ($1, $2, TRUE, NOW(), NOW())`,
      [thirdCameraCode, thirdCameraName]
    );
  });

  test.afterAll(async () => {
    if (database) {
      await cleanupFixture(database);
      await database.end();
    }
  });

  test('login, menú autorizado y administración NACHA por tercera cámara', async ({ page }) => {
    test.setTimeout(120_000);
    const token = await login(page);
    await expect(page.getByRole('navigation', { name: /menú principal/i })).toBeVisible();

    const clearingHouseDeepLink = await page.goto(`${spa}/clearing-houses`);
    expect(clearingHouseDeepLink?.status()).toBe(200);
    expect(clearingHouseDeepLink?.headers()['content-type']).toContain('text/html');
    await expect(page.locator('app-clearing-houses')).toBeVisible();

    const invalid = await page.request.post(`${api}/nacha-config/perfiles`, {
      headers: authorization(token),
      data: {}
    });
    expect(invalid.status()).toBe(400);
    expect(await invalid.text()).not.toContain('[object Object]');

    await page.goto(`${spa}/nacha-config-admin/perfiles`);
    const profilesPage = page.getByTestId('nacha-config-profiles-page');
    await expect(profilesPage).toBeVisible();
    await expect(profilesPage.getByRole('heading', { name: 'Configuración NACHA-M' })).toBeVisible();

    const createForm = page.locator('form.crear-grid');
    await createForm.getByLabel('Código del perfil').fill('');
    await createForm.getByLabel('Nombre').fill('');
    await expect(page.getByRole('button', { name: 'Crear borrador' })).toBeDisabled();

    await createForm.getByLabel('Código del perfil').fill(profileCode);
    await createForm.getByLabel('Nombre').fill('Perfil autónomo tercera cámara');
    await createForm.getByLabel('Descripción').fill('Fixture controlado y eliminado por Playwright.');
    await createForm.getByLabel('Cámara').selectOption(thirdCameraCode);

    let createRequests = 0;
    page.on('request', request => {
      if (request.method() === 'POST' && request.url().endsWith('/nacha-config/perfiles')) {
        createRequests += 1;
      }
    });

    await page.getByRole('button', { name: 'Crear borrador' }).click();
    await expect(page).toHaveURL(/\/nacha-config-admin\/perfiles\/\d+$/);
    await expect(page.getByTestId('nacha-config-profile-workspace-page')).toContainText(
      `${thirdCameraCode} - ${thirdCameraName}`
    );
    expect(createRequests).toBe(1);
    expect(await page.locator('body').innerText()).not.toContain('[object Object]');

    await page.getByRole('button', { name: 'Volver' }).first().click();
    const filters = page.locator('ui-tarjeta').filter({ hasText: 'Filtros de perfiles' });
    await filters.getByPlaceholder('Buscar cámara').fill(thirdCameraCode);
    await filters.getByRole('button', { name: new RegExp(`${thirdCameraCode}.*${thirdCameraName}`, 'i') }).click();
    await expect(page.getByText(profileCode, { exact: true })).toBeVisible();
    await expect(
      page.getByRole('gridcell', { name: `${thirdCameraCode} - ${thirdCameraName}`, exact: true })
    ).toBeVisible();

    const publishedId = await publishedProfileId(database);
    await navigateClientSide(page, `/nacha-config-admin/perfiles/${publishedId}`);
    await expect(page.getByRole('button', { name: 'Inactivar' })).toBeVisible();
    for (const viewport of viewports.filter(item => item.width <= 390)) {
      await page.setViewportSize(viewport);
      await page.getByRole('button', { name: 'Inactivar' }).click();
      const dialog = page.getByRole('dialog', { name: 'Confirmación' });
      await expect(dialog).toBeVisible();
      const bounds = await dialog.boundingBox();
      expect(bounds).not.toBeNull();
      expect(bounds!.x).toBeGreaterThanOrEqual(0);
      expect(bounds!.y).toBeGreaterThanOrEqual(0);
      expect(bounds!.x + bounds!.width).toBeLessThanOrEqual(viewport.width);
      expect(bounds!.y + bounds!.height).toBeLessThanOrEqual(viewport.height);
      await dialog.getByRole('button', { name: 'Cancelar' }).click();
    }
  });

  test('matriz responsive de rutas críticas', async ({ page }) => {
    test.setTimeout(240_000);
    await login(page);

    for (const viewport of viewports.filter(item => item.width <= 768)) {
      await page.setViewportSize(viewport);
      const menuButton = page.getByRole('button', { name: /Abrir menú principal/i });
      await menuButton.click();
      await expect(page.locator('aside.sidebar')).toHaveClass(/open/);
      await page.keyboard.press('Escape');
      await expect(page.locator('aside.sidebar')).not.toHaveClass(/open/);
    }

    for (const route of criticalRoutes) {
      await navigateClientSide(page, route);
      await expect(page.locator('main.content')).toBeVisible();
      await expect(page).not.toHaveURL(/\/login$/);

      for (const viewport of viewports) {
        await page.setViewportSize(viewport);
        await assertNoGlobalOverflow(page, `${route} en ${viewport.width}x${viewport.height}`);
        expect(await page.locator('body').innerText()).not.toContain('[object Object]');
      }
    }

  });
});

async function login(page: Page): Promise<string> {
  const loginResponse = await page.request.post(`${api}/auth/login`, {
    data: { username, password }
  });
  expect(loginResponse.ok(), 'El login real debe responder 200.').toBeTruthy();
  const token = (await loginResponse.json()).data.token as string;

  await page.goto(`${spa}/login`);
  await page.locator('input[formControlName="username"]').fill(username!);
  await page.locator('input[formControlName="password"]').fill(password!);
  await page.getByRole('button', { name: 'Ingresar' }).click();
  await expect(page).not.toHaveURL(/\/login$/);
  return token;
}

async function assertNoGlobalOverflow(page: Page, context: string): Promise<void> {
  const dimensions = await page.evaluate(() => ({
    viewport: document.documentElement.clientWidth,
    document: document.documentElement.scrollWidth,
    body: document.body.scrollWidth
  }));
  expect(
    Math.max(dimensions.document, dimensions.body),
    `Overflow horizontal global: ${context}`
  ).toBeLessThanOrEqual(dimensions.viewport + 1);
}

async function navigateClientSide(page: Page, route: string): Promise<void> {
  await page.evaluate(path => {
    window.history.pushState({}, '', path);
    window.dispatchEvent(new PopStateEvent('popstate'));
  }, route);
  await expect(page).toHaveURL(new RegExp(`${route.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}$`));
}

async function cleanupFixture(database: Client): Promise<void> {
  const profileIds = `SELECT "Id" FROM "CfgProfile" WHERE "ProfileCode" = $1`;
  await database.query(`DELETE FROM "HistConfigChange" WHERE "ProfileId" IN (${profileIds})`, [profileCode]);
  await database.query(`DELETE FROM "HistConfigSnapshot" WHERE "ProfileId" IN (${profileIds})`, [profileCode]);
  await database.query(`DELETE FROM "CfgPublishRequest" WHERE "ProfileId" IN (${profileIds})`, [profileCode]);
  await database.query(`DELETE FROM "CfgProfile" WHERE "ProfileCode" = $1`, [profileCode]);
  await database.query(`DELETE FROM "CatClearingHouse" WHERE "Code" = $1`, [thirdCameraCode]);
}

async function publishedProfileId(database: Client): Promise<number> {
  const result = await database.query<{ Id: number }>(
    `SELECT p."Id"
       FROM "CfgProfile" p
       JOIN "CatConfigStatus" s ON s."Id" = p."StatusId"
      WHERE s."Code" = 'PUBLICADO'
      ORDER BY p."Id"
      LIMIT 1`
  );
  if (!result.rows[0]) {
    throw new Error('El runtime no contiene un perfil PUBLICADO para validar la confirmación.');
  }
  return result.rows[0].Id;
}

function authorization(token: string): Record<string, string> {
  return { Authorization: `Bearer ${token}` };
}
