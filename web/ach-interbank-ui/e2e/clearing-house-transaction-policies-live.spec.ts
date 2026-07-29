import { expect, Page, test, TestInfo } from '@playwright/test';

const spa = process.env['ACH_UI_URL'] ?? process.env['E2E_BASE_URL'] ?? 'http://localhost:743';
const api = process.env['ACH_API_URL'] ?? process.env['E2E_API_BASE_URL'] ?? 'http://localhost:843';
const username = process.env['E2E_ADMIN_USER'] ?? process.env['ACH_USER'] ?? 'admin';
const password = process.env['E2E_ADMIN_PASSWORD'] ?? process.env['ACH_PASS'];

interface ClearingHouse {
  id: number;
  code: string;
  name: string;
}

interface RuntimeObservability {
  pageErrors: string[];
  consoleErrors: string[];
  httpErrors: string[];
}

test.describe.serial('Políticas transaccionales por cámara', () => {
  test.skip(!password, 'E2E_ADMIN_PASSWORD o ACH_PASS es obligatorio para el runtime autenticado.');

  test('menú, ACH Colombia, CENIT, móvil y ruta histórica', async ({ page }, testInfo) => {
    test.setTimeout(120_000);
    const observed = observeRuntime(page);
    const token = await login(page);
    const ach = await getClearingHouse(page, token, 'ACHCOL');
    const cenit = await getClearingHouse(page, token, 'CENIT');

    await assertLegacyMenuIsAbsent(page);
    await openPoliciesFromClearingHouses(page, ach);
    await assertAchColombiaDesktop(page, ach, testInfo);

    await page.goto(`${spa}/clearing-houses/${cenit.id}/transaction-policies`);
    await assertCenitDesktop(page, cenit, testInfo);

    await assertMobileLayout(page, ach, testInfo);
    await assertLegacyRouteRedirect(page);

    expect(observed.pageErrors, 'No debe haber pageerror inesperado.').toEqual([]);
    expect(observed.consoleErrors, 'No debe haber console.error inesperado.').toEqual([]);
    expect(observed.httpErrors, 'No debe haber respuestas HTTP 4xx/5xx inesperadas.').toEqual([]);
  });
});

async function login(page: Page): Promise<string> {
  const response = await page.request.post(`${api}/auth/login`, {
    data: { username, password }
  });
  expect(response.ok(), 'El login del runtime aislado debe responder correctamente.').toBeTruthy();
  const payload = await response.json() as { data?: { token?: string } };
  const token = payload.data?.token;
  expect(token, 'El login debe entregar un token de sesión.').toBeTruthy();

  await page.goto(`${spa}/login`, { waitUntil: 'domcontentloaded' });
  await page.locator('input[formControlName="username"]').fill(username);
  await page.locator('input[formControlName="password"]').fill(password!);
  await page.getByRole('button', { name: 'Ingresar' }).click();
  await expect(page).not.toHaveURL(/\/login(?:\?.*)?$/);
  return token!;
}

async function getClearingHouse(page: Page, token: string, code: string): Promise<ClearingHouse> {
  const response = await page.request.get(`${api}/clearing-houses?search=${encodeURIComponent(code)}`, {
    headers: { Authorization: `Bearer ${token}` }
  });
  expect(response.ok(), `La consulta de la cámara ${code} debe responder correctamente.`).toBeTruthy();
  const payload = await response.json() as { items?: ClearingHouse[] };
  const clearingHouse = payload.items?.find(item => item.code === code);
  expect(clearingHouse, `Debe existir la cámara ${code} en el runtime aislado.`).toBeTruthy();
  return clearingHouse!;
}

async function assertLegacyMenuIsAbsent(page: Page): Promise<void> {
  await expect(page.getByRole('navigation', { name: /menú principal/i })).toBeVisible();
  await expect(page.getByText('Reglas por cámara', { exact: true })).toHaveCount(0);
  await expect(page.locator('a[href="/transactions/clearing-house-rules"]')).toHaveCount(0);
}

async function openPoliciesFromClearingHouses(page: Page, clearingHouse: ClearingHouse): Promise<void> {
  await page.goto(`${spa}/clearing-houses`);
  await expect(page.locator('app-clearing-houses')).toBeVisible();
  await page.locator('input[name="search"]').fill(clearingHouse.code);
  await page.getByRole('button', { name: 'Buscar' }).click();

  const row = page.locator('tr').filter({ hasText: clearingHouse.code });
  const policiesLink = row.getByRole('link', { name: 'Políticas transaccionales' });
  await expect(policiesLink).toBeVisible();
  await policiesLink.click();
  await expect(page).toHaveURL(new RegExp(`/clearing-houses/${clearingHouse.id}/transaction-policies$`));
}

async function assertAchColombiaDesktop(page: Page, clearingHouse: ClearingHouse, testInfo: TestInfo): Promise<void> {
  await page.setViewportSize({ width: 1440, height: 900 });
  await expect(page.getByRole('heading', { name: 'Políticas transaccionales' })).toBeVisible();
  await expect(page.getByText(clearingHouse.name, { exact: true })).toBeVisible();
  await expect(page.getByText(`Código: ${clearingHouse.code}`, { exact: true })).toBeVisible();
  await expect(page.getByText('Prenotificación obligatoria', { exact: true })).toBeVisible();
  await expect(page.getByText('3 días hábiles', { exact: true })).toBeVisible();
  await expect(page.getByText('Prenotificación opcional', { exact: true })).toBeVisible();
  await expect(page.getByText('Versiones e historial', { exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Crear nueva versión' })).toBeVisible();

  await page.getByRole('button', { name: 'Crear nueva versión' }).click();
  await expect(page.getByRole('heading', { name: 'Crear nueva versión' })).toBeVisible();
  await page.getByRole('button', { name: 'Cancelar' }).click();
  await screenshot(page, testInfo, 'transaction-policies-achcol-desktop.png');
}

async function assertCenitDesktop(page: Page, clearingHouse: ClearingHouse, testInfo: TestInfo): Promise<void> {
  await page.setViewportSize({ width: 1440, height: 900 });
  await expect(page.getByText(clearingHouse.name, { exact: true })).toBeVisible();
  await expect(page.getByText(`Código: ${clearingHouse.code}`, { exact: true })).toBeVisible();
  await expect(page.getByText('Prenotificación obligatoria', { exact: true })).toBeVisible();
  await expect(page.getByText('Sin plazo mínimo documentado', { exact: true })).toBeVisible();
  await expect(page.getByText('Prenotificación opcional', { exact: true })).toBeVisible();
  await expect(page.getByText('3 días hábiles', { exact: true })).toHaveCount(0);
  await screenshot(page, testInfo, 'transaction-policies-cenit-desktop.png');
}

async function assertMobileLayout(page: Page, clearingHouse: ClearingHouse, testInfo: TestInfo): Promise<void> {
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto(`${spa}/clearing-houses/${clearingHouse.id}/transaction-policies`);
  await expect(page.getByRole('heading', { name: 'Políticas transaccionales' })).toBeVisible();
  await expect(page.getByText('3 días hábiles', { exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Crear nueva versión' })).toBeVisible();
  expect(await page.locator('body').evaluate(body => body.scrollWidth <= window.innerWidth)).toBeTruthy();

  await page.getByRole('button', { name: 'Crear nueva versión' }).click();
  const form = page.locator('form.policy-form');
  await expect(form).toBeVisible();
  const bounds = await form.boundingBox();
  expect(bounds, 'El formulario debe tener límites visibles en móvil.').not.toBeNull();
  expect(bounds!.x).toBeGreaterThanOrEqual(0);
  expect(bounds!.x + bounds!.width).toBeLessThanOrEqual(390);
  await page.getByRole('button', { name: 'Cancelar' }).click();
  await screenshot(page, testInfo, 'transaction-policies-mobile.png');
}

async function assertLegacyRouteRedirect(page: Page): Promise<void> {
  await page.goto(`${spa}/transactions/clearing-house-rules`);
  await expect(page).toHaveURL(new RegExp(`${escapeRegExp('/clearing-houses')}$`));
  await expect(page.locator('app-clearing-houses')).toBeVisible();
  await expect(page.locator('app-clearing-house-transaction-rules')).toHaveCount(0);
}

function observeRuntime(page: Page): RuntimeObservability {
  const observed: RuntimeObservability = { pageErrors: [], consoleErrors: [], httpErrors: [] };
  page.on('pageerror', error => observed.pageErrors.push(error.message));
  page.on('console', message => {
    if (message.type() === 'error') {
      observed.consoleErrors.push(message.text());
    }
  });
  page.on('response', response => {
    if (response.status() >= 400 && !response.url().endsWith('/favicon.ico')) {
      observed.httpErrors.push(`${response.status()} ${response.request().method()} ${response.url()}`);
    }
  });
  return observed;
}

async function screenshot(page: Page, testInfo: TestInfo, fileName: string): Promise<void> {
  const path = testInfo.outputPath(fileName);
  await page.screenshot({ path, fullPage: true });
  await testInfo.attach(fileName, { path, contentType: 'image/png' });
}

function escapeRegExp(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
