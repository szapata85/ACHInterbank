import { expect, Page, test } from '@playwright/test';

const username = process.env['ACH_USER'] ?? 'admin';
const password = process.env['ACH_PASS'];

test.describe.configure({ mode: 'serial' });
test.skip(!password, 'ACH_PASS es obligatorio para validar el calendario en el runtime local.');

test.beforeEach(async ({ page }) => {
  const response = await page.request.post('/auth/login', {
    data: { username, password }
  });
  expect(response.ok()).toBeTruthy();
  const payload = await response.json() as { data?: { token?: string } };
  expect(payload.data?.token).toBeTruthy();
  await page.addInitScript(token => {
    window.sessionStorage.setItem('ach.interbank.access_token', token);
  }, payload.data!.token!);
});

test('muestra los 19 festivos legales de 2026 y protege Chiquinquira', async ({ page }, testInfo) => {
  await page.goto('/catalogs/bank-holidays');
  await expect(page.getByRole('heading', { name: 'Festivos nacionales' })).toBeVisible();

  const year = page.getByLabel('Año');
  await expect(year).toHaveCount(1);
  await year.fill('2026');
  await page.getByRole('button', { name: 'Buscar' }).click();

  const table = page.locator('table');
  await expect(table.locator('tbody tr')).toHaveCount(19);
  await expect(table.getByText('Día de Nuestra Señora del Rosario de Chiquinquirá', { exact: true })).toBeVisible();
  await expect(table.getByText('13 de julio de 2026', { exact: true })).toBeVisible();
  await expect(table.getByText('9 de julio de 2026', { exact: true })).toBeVisible();
  await expect(table.getByText('Chiquinquirá: trasladado al lunes por la Ley Emiliani', { exact: true })).toBeVisible();
  await expect(table.getByText('Festivo nacional protegido', { exact: true })).toHaveCount(19);
  await page.screenshot({ path: testInfo.outputPath('festivos-2026-escritorio.png'), fullPage: true });

  await page.setViewportSize({ width: 390, height: 844 });
  await expect(page.locator('.holiday-cards')).toBeVisible();
  await page.screenshot({ path: testInfo.outputPath('festivos-2026-movil.png'), fullPage: true });
});

test('mantiene separadas las fechas especiales de ACH Colombia y CENIT', async ({ page }, testInfo) => {
  await assertSpecialDatesPage(
    page,
    '/clearing-houses/1/special-dates',
    'Fechas especiales de ACH Colombia',
    'UAT cierre exclusivo ACH Colombia',
    'UAT cierre exclusivo CENIT');
  await page.screenshot({ path: testInfo.outputPath('fechas-especiales-ach.png'), fullPage: true });

  await assertSpecialDatesPage(
    page,
    '/clearing-houses/2/special-dates',
    'Fechas especiales de CENIT',
    'UAT cierre exclusivo CENIT',
    'UAT cierre exclusivo ACH Colombia');
  await page.setViewportSize({ width: 390, height: 844 });
  await expect(page.locator('.mobile-cards')).toBeVisible();
  await page.screenshot({ path: testInfo.outputPath('fechas-especiales-cenit-movil.png'), fullPage: true });
});

async function assertSpecialDatesPage(
  page: Page,
  path: string,
  heading: string,
  expectedDescription: string,
  foreignDescription: string
): Promise<void> {
  await page.goto(path);
  await expect(page.getByRole('heading', { name: heading })).toBeVisible();
  await expect(page.getByText('Esta configuración solo afecta a la cámara seleccionada.', { exact: false })).toBeVisible();
  await expect(page.locator('table').getByText(expectedDescription, { exact: true })).toBeVisible();
  await expect(page.getByText(foreignDescription, { exact: true })).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Nueva fecha especial' })).toBeVisible();
}
