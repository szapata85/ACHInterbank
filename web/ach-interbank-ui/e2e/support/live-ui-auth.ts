import { expect, Page } from '@playwright/test';

const username = process.env['E2E_ADMIN_USER'] ?? process.env['ACH_USER'] ?? 'admin';
const password = process.env['E2E_ADMIN_PASSWORD'] ?? process.env['ACH_PASS'] ?? 'Admin123!';

export async function loginThroughUi(page: Page): Promise<void> {
  await page.goto('/login', { waitUntil: 'domcontentloaded' });
  await page.locator('input[formControlName="username"]').fill(username);
  await page.locator('input[formControlName="password"]').fill(password);

  const loginResponse = page.waitForResponse((response) =>
    response.request().method() === 'POST'
    && new URL(response.url()).pathname.endsWith('/auth/login')
  );

  await page.getByRole('button', { name: 'Ingresar' }).click();
  expect((await loginResponse).status(), 'El login LIVE debe responder 200.').toBe(200);
  await expect(page).not.toHaveURL(/\/login(?:\?.*)?$/);
}
