import { expect, Page, test, TestInfo } from '@playwright/test';
import { mkdirSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';

const spa = process.env['E2E_BASE_URL'] ?? 'http://localhost:1743';
const api = process.env['E2E_API_BASE_URL'] ?? 'http://localhost:1843';
const username = process.env['ACH_USER'] ?? 'admin';
const password = process.env['ACH_PASS'];
const readerUsername = process.env['E2E_READER_USER'] ?? 'workspace.reader.e2e';
const readerPassword = process.env['E2E_READER_PASSWORD'];
const evidence = resolve(process.cwd(), '../../artifacts/clearing-house-workspace-final');
const viewports = [
  { width: 1440, height: 900 },
  { width: 1024, height: 768 },
  { width: 768, height: 1024 },
  { width: 390, height: 844 }
];

interface ClearingHouse {
  id: number;
  code: string;
  name: string;
}

interface Observation {
  pageErrors: string[];
  consoleErrors: string[];
  httpErrors: string[];
  mutations: string[];
}

interface Measurement {
  screen: string;
  route: string;
  viewportWidth: number;
  viewportHeight: number;
  documentWidth: number;
  bodyWidth: number;
  offenders: string[];
}

test.describe.serial('Cierre real del workspace de cámaras compensadoras', () => {
  test.skip(!password || !readerPassword, 'ACH_PASS y E2E_READER_PASSWORD son obligatorios para el runtime autenticado.');

  test('workspace Material, permisos, rutas, responsive y observabilidad', async ({ page }, testInfo) => {
    test.setTimeout(180_000);
    mkdirSync(evidence, { recursive: true });
    const observed = observe(page);
    const token = await login(page, username, password!);
    const ach = await getHouse(page, token, 'ACHCOL');
    const cenit = await getHouse(page, token, 'CENIT');

    await assertClearingHouses(page, ach, testInfo);
    await assertPolicies(page, ach, true, testInfo);
    await assertPolicies(page, cenit, false, testInfo);
    await assertCycles(page, ach, testInfo);
    await assertCycles(page, cenit, testInfo);
    await assertSpecialDates(page, ach, testInfo);
    await assertSpecialDates(page, cenit, testInfo);
    await assertLegacyRoutes(page, ach);
    await assertReaderPermissions(page, ach);

    const measurements = await measureAll(page, [
      ['Cámaras', '/clearing-houses', 'app-clearing-houses'],
      ['Políticas ACH Colombia', `/clearing-houses/${ach.id}/transaction-policies`, 'app-transaction-policies'],
      ['Políticas CENIT', `/clearing-houses/${cenit.id}/transaction-policies`, 'app-transaction-policies'],
      ['Ciclos ACH Colombia', `/clearing-houses/${ach.id}/cycles`, 'app-cycle-config-management'],
      ['Ciclos CENIT', `/clearing-houses/${cenit.id}/cycles`, 'app-cycle-config-management'],
      ['Fechas ACH Colombia', `/clearing-houses/${ach.id}/special-dates`, 'app-clearing-house-special-dates'],
      ['Fechas CENIT', `/clearing-houses/${cenit.id}/special-dates`, 'app-clearing-house-special-dates']
    ]);
    const measurementPath = resolve(evidence, 'responsive-measurements.json');
    writeFileSync(measurementPath, JSON.stringify(measurements, null, 2), 'utf8');
    await testInfo.attach('responsive-measurements.json', { path: measurementPath, contentType: 'application/json' });

    expect(observed.pageErrors, 'No debe haber pageerror inesperado.').toEqual([]);
    expect(observed.consoleErrors, 'No debe haber console.error inesperado.').toEqual([]);
    expect(observed.httpErrors, 'No debe haber HTTP 4xx/5xx inesperado.').toEqual([]);
    expect(await page.locator('body').innerText()).not.toMatch(/Ã|Â|â€¦|â€”|�/);
  });
});

async function login(page: Page, loginUser: string, loginPassword: string): Promise<string> {
  const response = await page.request.post(`${api}/auth/login`, {
    data: { username: loginUser, password: loginPassword }
  });
  expect(response.ok(), 'El login aislado debe responder correctamente.').toBeTruthy();
  const payload = await response.json() as { data?: { token?: string } };
  expect(payload.data?.token).toBeTruthy();
  await page.goto(`${spa}/login`);
  await page.locator('input[formControlName="username"]').fill(loginUser);
  await page.locator('input[formControlName="password"]').fill(loginPassword);
  await page.getByRole('button', { name: 'Ingresar' }).click();
  await expect(page).not.toHaveURL(/\/login(?:\?.*)?$/);
  return payload.data!.token!;
}

async function getHouse(page: Page, token: string, code: string): Promise<ClearingHouse> {
  const response = await page.request.get(`${api}/api/clearing-houses?search=${code}`, {
    headers: auth(token)
  });
  expect(response.ok()).toBeTruthy();
  const payload = await response.json() as { items?: ClearingHouse[] };
  const house = payload.items?.find(item => item.code === code);
  expect(house, `Debe existir ${code}.`).toBeTruthy();
  return house!;
}

async function assertClearingHouses(page: Page, ach: ClearingHouse, testInfo: TestInfo): Promise<void> {
  await page.setViewportSize(viewports[0]);
  await page.goto(`${spa}/clearing-houses`);
  await expect(page.locator('app-clearing-houses')).toBeVisible();
  await expect(page.locator('h1')).toHaveCount(1);
  await expect(page.locator('h1')).toHaveText('Cámaras compensadoras');
  await expect(page.locator('mat-card')).not.toHaveCount(0);
  await page.getByLabel('Nombre o código').fill(ach.code);
  await page.getByRole('button', { name: 'Buscar' }).click();
  await expect(page.locator('.desktop-table tr').filter({ hasText: ach.code })).toBeVisible();
  const row = page.locator('.desktop-table tr').filter({ hasText: ach.code });
  await row.getByRole('button', { name: `Administrar ${ach.name}` }).click();
  await expect(page.getByRole('menuitem', { name: 'Políticas transaccionales' })).toBeVisible();
  await expect(page.getByRole('menuitem', { name: 'Ciclos' })).toBeVisible();
  await expect(page.getByRole('menuitem', { name: 'Fechas especiales' })).toBeVisible();
  await page.keyboard.press('Escape');
  await screenshot(page, testInfo, 'clearing-houses-desktop.png');

  await page.setViewportSize(viewports[1]);
  await page.goto(`${spa}/clearing-houses`);
  await expect(page.locator('app-clearing-houses')).toBeVisible();
  await assertNoOverflow(page, 'Cámaras 1024');
  await screenshot(page, testInfo, 'clearing-houses-1024.png');

  await page.setViewportSize(viewports[3]);
  await page.goto(`${spa}/clearing-houses`);
  await expect(page.locator('.mobile-cards')).toBeVisible();
  await assertNoOverflow(page, 'Cámaras móvil');
  await screenshot(page, testInfo, 'clearing-houses-mobile.png');
}

async function assertPolicies(page: Page, house: ClearingHouse, achColombia: boolean, testInfo: TestInfo): Promise<void> {
  await page.setViewportSize(viewports[0]);
  await page.goto(`${spa}/clearing-houses/${house.id}/transaction-policies`);
  await expect(page.locator('app-transaction-policies')).toBeVisible();
  await expect(page.locator('mat-spinner')).toHaveCount(0);
  await expect(page.locator('h1')).toHaveCount(1);
  const debit = page.locator('.policy-summary').filter({ hasText: 'Débitos' });
  const credit = page.locator('.policy-summary').filter({ hasText: 'Créditos' });
  await expect(debit.getByText('Prenotificación obligatoria', { exact: true })).toBeVisible();
  await expect(credit.getByText('Prenotificación opcional', { exact: true })).toBeVisible();
  await expect(debit.getByText('Bloquea la exportación cuando no existe una prenotificación válida.', { exact: true })).toBeVisible();
  await expect(credit.getByText('No bloquea por ausencia de prenotificación.', { exact: true })).toBeVisible();
  if (achColombia) {
    await expect(debit.getByText('3 días hábiles', { exact: true })).toBeVisible();
  } else {
    await expect(debit.getByText('Sin plazo mínimo documentado', { exact: true })).toBeVisible();
    await expect(debit.getByText('3 días hábiles', { exact: true })).toHaveCount(0);
  }
  await expect(page.locator('.policy-page__summary')).not.toContainText('Referencia');
  await expect(page.locator('.policy-page__table-wrap')).not.toContainText('Referencia');
  await expect(page.getByText('Historial de cambios', { exact: true })).toBeVisible();

  await debit.getByRole('button', { name: 'Ver detalle normativo' }).click();
  await expect(page.getByRole('dialog').getByRole('heading', { name: 'Detalle normativo' })).toBeVisible();
  await expect(page.getByRole('dialog')).toContainText('Referencia');
  await page.getByRole('dialog').getByRole('button', { name: 'Cerrar' }).click();

  const mutationsBefore = mutationCount(page);
  await page.getByRole('button', { name: 'Crear nueva versión' }).click();
  const dialog = page.getByRole('dialog');
  await expect(dialog.getByText('Trazabilidad normativa')).toBeVisible();
  await expect(dialog.getByRole('button', { name: 'Comprobar regla para una fecha' })).toBeVisible();
  await dialog.getByRole('button', { name: 'Cancelar' }).click();
  expect(mutationCount(page)).toBe(mutationsBefore);
  await screenshot(page, testInfo, achColombia ? 'transaction-policies-achcol.png' : 'transaction-policies-cenit.png');
}

async function assertCycles(page: Page, house: ClearingHouse, testInfo: TestInfo): Promise<void> {
  await page.goto(`${spa}/clearing-houses/${house.id}/cycles`);
  await expect(page.locator('app-cycle-config-management')).toBeVisible();
  await expect(page.locator('mat-spinner')).toHaveCount(0);
  await expect(page.locator('app-clearing-house-context-navigation').getByText(house.name, { exact: true }).first()).toBeVisible();
  await expect(page.locator('app-cycle-config-management [formControlName="clearingHouseId"]')).toHaveCount(0);
  await expect(page.locator('app-cycle-config-management ui-selector-buscable')).toHaveCount(0);
  await expect(page.locator('app-cycle-config-management ui-grilla-empresarial')).toHaveCount(0);
  await expect(page.locator('app-cycle-config-management app-confirm-dialog')).toHaveCount(0);
  await expect(page.getByLabel('Nombre del ciclo')).toBeVisible();
  await page.getByLabel('Nombre del ciclo').fill('sin-coincidencias-e2e');
  await expect(page.getByText('Sin configuraciones para los filtros seleccionados')).toBeVisible();

  const mutationsBefore = mutationCount(page);
  await page.getByRole('button', { name: 'Nueva configuración' }).click();
  const dialog = page.getByRole('dialog');
  await dialog.getByLabel('Nombre del ciclo').fill('Validación E2E');
  await dialog.getByLabel('Inicio de ventana').fill('10:00');
  await dialog.getByLabel('Fin de ventana').fill('09:00');
  await dialog.getByLabel('Cutoff').fill('09:00');
  await expect(dialog.getByText('La hora inicial debe ser anterior a la hora final.')).toBeVisible();
  await dialog.getByRole('button', { name: 'Cancelar' }).click();
  expect(mutationCount(page)).toBe(mutationsBefore);
  if (house.code === 'ACHCOL') await screenshot(page, testInfo, 'cycles-achcol.png');
}

async function assertSpecialDates(page: Page, house: ClearingHouse, testInfo: TestInfo): Promise<void> {
  await page.goto(`${spa}/clearing-houses/${house.id}/special-dates`);
  await expect(page.locator('app-clearing-house-special-dates')).toBeVisible();
  await expect(page.locator('mat-spinner')).toHaveCount(0);
  await expect(page.locator('app-clearing-house-context-navigation').getByText(house.name, { exact: true }).first()).toBeVisible();
  await expect(page.locator('app-clearing-house-special-dates [formControlName="clearingHouseId"]')).toHaveCount(0);
  await expect(page.locator('app-clearing-house-special-dates select')).toHaveCount(0);
  await expect(page.locator('app-clearing-house-special-dates ui-grilla-empresarial')).toHaveCount(0);
  await expect(page.getByText('Fechas no operativas adicionales al calendario bancario general.', { exact: true })).toBeVisible();

  const mutationsBefore = mutationCount(page);
  await page.getByRole('button', { name: 'Nueva fecha especial' }).click();
  const dialog = page.getByRole('dialog');
  await expect(dialog.getByLabel('Fecha')).toBeVisible();
  await expect(dialog.getByLabel('Descripción')).toBeVisible();
  await expect(dialog.getByRole('button', { name: 'Guardar' })).toBeDisabled();
  await dialog.getByRole('button', { name: 'Cancelar' }).click();
  expect(mutationCount(page)).toBe(mutationsBefore);
  if (house.code === 'ACHCOL') await screenshot(page, testInfo, 'special-dates-achcol.png');
}

async function assertLegacyRoutes(page: Page, ach: ClearingHouse): Promise<void> {
  await page.goto(`${spa}/catalogs/clearing-house-special-dates?clearingHouseId=${ach.id}`);
  await expect(page).toHaveURL(new RegExp(`/clearing-houses/${ach.id}/special-dates$`));
  await expect(page.locator('app-clearing-house-special-dates')).toBeVisible();
  await page.goto(`${spa}/catalogs/clearing-house-special-dates`);
  await expect(page).toHaveURL(/\/clearing-houses$/);
  await page.goto(`${spa}/transactions/clearing-house-rules`);
  await expect(page).toHaveURL(/\/clearing-houses$/);
  await expect(page.locator('app-clearing-house-transaction-rules')).toHaveCount(0);
}

async function assertReaderPermissions(page: Page, ach: ClearingHouse): Promise<void> {
  await login(page, readerUsername, readerPassword!);
  await page.goto(`${spa}/clearing-houses/${ach.id}/cycles`);
  await expect(page.getByRole('button', { name: 'Nueva configuración' })).toHaveCount(0);
  await page.goto(`${spa}/clearing-houses/${ach.id}/special-dates`);
  await expect(page.getByRole('button', { name: 'Nueva fecha especial' })).toHaveCount(0);
  await page.goto(`${spa}/clearing-houses/${ach.id}/transaction-policies`);
  await expect(page.getByRole('button', { name: 'Crear nueva versión' })).toHaveCount(0);
  await expect(page.getByRole('tab', { name: 'Ciclos' })).toBeVisible();
  await expect(page.getByRole('tab', { name: 'Fechas especiales' })).toBeVisible();

  await login(page, username, password!);
}

async function measureAll(
  page: Page,
  screens: Array<[string, string, string]>
): Promise<Measurement[]> {
  const measurements: Measurement[] = [];
  for (const viewport of viewports) {
    await page.setViewportSize(viewport);
    for (const [screen, route, selector] of screens) {
      await page.goto(`${spa}${route}`);
      await expect(page.locator(selector)).toBeVisible();
      await expect(page.locator('mat-spinner')).toHaveCount(0);
      const dimensions = await page.evaluate(() => {
        const viewportWidth = window.innerWidth;
        const offenders = Array.from(document.querySelectorAll<HTMLElement>('body *'))
          .map(element => ({ element, rect: element.getBoundingClientRect() }))
          .filter(item => item.rect.right > viewportWidth + 1 || item.rect.left < -1)
          .slice(0, 8)
          .map(item => `${item.element.tagName.toLowerCase()}.${item.element.className || ''}:${Math.round(item.rect.left)}..${Math.round(item.rect.right)}`);
        return {
          documentWidth: document.documentElement.scrollWidth,
          bodyWidth: document.body.scrollWidth,
          offenders
        };
      });
      const measurement: Measurement = {
        screen,
        route,
        viewportWidth: viewport.width,
        viewportHeight: viewport.height,
        documentWidth: dimensions.documentWidth,
        bodyWidth: dimensions.bodyWidth,
        offenders: dimensions.offenders
      };
      measurements.push(measurement);
      expect(measurement.documentWidth, `${screen} ${viewport.width}x${viewport.height}: ${measurement.offenders.join(', ')}`)
        .toBeLessThanOrEqual(viewport.width + 1);
    }
  }
  return measurements;
}

async function assertNoOverflow(page: Page, label: string): Promise<void> {
  const widths = await page.evaluate(() => ({
    viewport: window.innerWidth,
    document: document.documentElement.scrollWidth
  }));
  expect(widths.document, label).toBeLessThanOrEqual(widths.viewport + 1);
}

function observe(page: Page): Observation {
  const observed: Observation = { pageErrors: [], consoleErrors: [], httpErrors: [], mutations: [] };
  page.on('pageerror', error => observed.pageErrors.push(error.message));
  page.on('console', message => {
    if (message.type() === 'error') observed.consoleErrors.push(message.text());
  });
  page.on('response', response => {
    if (response.status() >= 400 && !response.url().endsWith('/favicon.ico')) {
      observed.httpErrors.push(`${response.status()} ${response.request().method()} ${response.url()}`);
    }
  });
  page.on('request', request => {
    if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(request.method()) && !request.url().endsWith('/auth/login')) {
      observed.mutations.push(`${request.method()} ${request.url()}`);
    }
  });
  Reflect.set(page, '__workspaceObservation', observed);
  return observed;
}

function mutationCount(page: Page): number {
  return (Reflect.get(page, '__workspaceObservation') as Observation).mutations.length;
}

function auth(token: string): Record<string, string> {
  return { Authorization: `Bearer ${token}` };
}

async function screenshot(page: Page, testInfo: TestInfo, fileName: string): Promise<void> {
  const path = resolve(evidence, fileName);
  await page.screenshot({ path, fullPage: true });
  await testInfo.attach(fileName, { path, contentType: 'image/png' });
}
