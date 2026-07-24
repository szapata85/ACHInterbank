import { expect, Page, test } from '@playwright/test';

const spaBaseUrl = (process.env['E2E_BASE_URL'] ?? 'http://localhost:743').replace(/\/+$/, '');
const adminUser = process.env['E2E_ADMIN_USER'] ?? process.env['ACH_USER'] ?? 'admin';
const adminPassword = process.env['E2E_ADMIN_PASSWORD'] ?? process.env['ACH_PASS'] ?? 'Admin123!';

test.describe('Navegación administrativa contra el stack SQL Server', () => {
  test('muestra certificados y catálogo de ciclos sin duplicados en desktop', async ({ page }) => {
    const errors: string[] = [];
    page.on('pageerror', (error) => errors.push(error.message));
    page.on('console', (message) => {
      if (message.type() === 'error') errors.push(message.text());
    });

    await loginThroughUi(page);
    await page.getByRole('button', { name: 'Seguridad NACHA' }).click();
    const certificateLink = page.getByRole('link', { name: 'Certificados digitales', exact: true });
    await expect(certificateLink).toHaveCount(1);
    await certificateLink.click();
    await expect(page).toHaveURL(/\/nacha-security\/certificates$/);
    await expect(page.getByRole('main').getByRole('heading', { name: 'Certificados digitales' })).toBeVisible();
    await page.screenshot({ path: '../../docs/uat/evidencias/navigation/certificates-desktop.png', fullPage: true });

    await page.getByRole('button', { name: 'Configuración de ciclos' }).click();
    const catalogLink = page.getByRole('link', { name: 'Catálogo de ciclos', exact: true });
    await expect(catalogLink).toHaveCount(1);
    await catalogLink.click();
    await expect(page).toHaveURL(/\/ach-cycles$/);
    await expect(page.locator('app-page-header')).toBeVisible();
    await expect(page.locator('select[formControlName="clearingHouseId"]')).toBeVisible();
    await expect(page.locator('a[href="/transactions/cycle-configs"]')).toHaveCount(1);
    await expect(page.locator('body')).not.toContainText('[object Object]');
    expect(errors.filter((error) => !/favicon/i.test(error))).toEqual([]);
  });

  test('mantiene las rutas directas y el menú utilizable en móvil', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await loginThroughUi(page);

    await page.goto(`${spaBaseUrl}/nacha-security/certificates`);
    await expect(page.getByRole('main').getByRole('heading', { name: 'Certificados digitales' })).toBeVisible();
    await page.reload();
    await expect(page).toHaveURL(/\/nacha-security\/certificates$/);

    await page.goto(`${spaBaseUrl}/transactions/cycle-configs`);
    await expect(page.locator('app-page-header')).toBeVisible();
    await page.reload();
    await expect(page).toHaveURL(/\/transactions\/cycle-configs$/);
    await page.screenshot({ path: '../../docs/uat/evidencias/navigation/cycles-mobile.png', fullPage: true });
  });
});

async function loginThroughUi(page: Page): Promise<void> {
  await page.goto(`${spaBaseUrl}/login`);
  await page.locator('input[formControlName="username"]').fill(adminUser);
  await page.locator('input[formControlName="password"]').fill(adminPassword);
  await Promise.all([
    page.waitForResponse((response) => response.url().includes('/auth/login') && response.request().method() === 'POST' && response.status() === 200),
    page.getByRole('button', { name: 'Ingresar' }).click()
  ]);
  await expect(page).not.toHaveURL(/\/login$/);
}
