import { expect, test } from '@playwright/test';

const spa = process.env['ACH_UI_URL'] ?? 'http://localhost:743';
const api = process.env['ACH_API_URL'] ?? 'http://localhost:843';
const username = process.env['E2E_ADMIN_USER'] ?? process.env['ACH_USER'] ?? 'admin';
const password = process.env['E2E_ADMIN_PASSWORD'] ?? process.env['ACH_PASS'];

test.describe('Políticas transaccionales por cámara', () => {
  test.skip(!password, 'E2E_ADMIN_PASSWORD o ACH_PASS es obligatorio.');
  test('muestra ACH Colombia, CENIT y redirige la interfaz anterior', async ({ page }) => {
    const errors: string[] = []; page.on('pageerror', error => errors.push(error.message));
    const login = await page.request.post(`${api}/auth/login`, { data: { username, password } });
    expect(login.ok()).toBeTruthy(); const token = (await login.json()).data.token as string;
    await page.goto(`${spa}/login`); await page.locator('input[formControlName="username"]').fill(username); await page.locator('input[formControlName="password"]').fill(password!); await page.getByRole('button', { name: 'Ingresar' }).click();
    const houses = await page.request.get(`${api}/clearing-houses?search=ACHCOL`, { headers: { Authorization: `Bearer ${token}` } });
    expect(houses.ok()).toBeTruthy(); const ach = (await houses.json()).items[0];
    await page.goto(`${spa}/clearing-houses/${ach.id}/transaction-policies`);
    await expect(page.getByRole('heading', { name: 'Políticas transaccionales' })).toBeVisible();
    await expect(page.getByText('3 días hábiles')).toBeVisible(); await expect(page.getByText('Prenotificación obligatoria')).toBeVisible();
    await page.setViewportSize({ width: 390, height: 844 }); await expect(page.getByRole('heading', { name: 'Políticas transaccionales' })).toBeVisible();
    expect(await page.locator('body').evaluate(body => body.scrollWidth <= window.innerWidth)).toBeTruthy();
    const cenit = await page.request.get(`${api}/clearing-houses?search=CENIT`, { headers: { Authorization: `Bearer ${token}` } });
    const cenitHouse = (await cenit.json()).items[0]; await page.goto(`${spa}/clearing-houses/${cenitHouse.id}/transaction-policies`);
    await expect(page.getByText('Sin plazo mínimo documentado')).toBeVisible();
    await page.goto(`${spa}/transactions/clearing-house-rules`); await expect(page).toHaveURL(/\/clearing-houses$/);
    expect(errors).toEqual([]);
  });
});
