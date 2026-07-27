import { expect, Page, test } from '@playwright/test';
import { mkdirSync } from 'node:fs';
import { resolve } from 'node:path';

const ui = (process.env['ACH_UI_URL'] ?? 'http://localhost:743').replace(/\/+$/, '');
const username = process.env['ACH_USER'] ?? '';
const password = process.env['ACH_PASS'] ?? '';
const evidenceDir = resolve(process.cwd(), '..', '..', 'docs', 'uat', 'evidencias', 'transactions-create');
mkdirSync(evidenceDir, { recursive: true });

test.skip(!username || !password, 'ACH_USER y ACH_PASS son requeridos para validar la SPA real.');

for (const viewport of [
  { name: 'desktop', width: 1440, height: 900 },
  { name: 'mobile', width: 390, height: 844 }
]) {
  test(`formulario compacto, accesible y sin referencia legado en ${viewport.name}`, async ({ page }, testInfo) => {
    await page.setViewportSize({ width: viewport.width, height: viewport.height });
    const consoleErrors: string[] = [];
    const failedRequests: string[] = [];
    const transactionPosts: string[] = [];
    page.on('console', (message) => {
      if (message.type() === 'error') {
        consoleErrors.push(message.text());
      }
    });
    page.on('requestfailed', (request) => failedRequests.push(`${request.method()} ${request.url()}`));
    page.on('request', (request) => {
      if (request.method() === 'POST' && new URL(request.url()).pathname.toLowerCase().endsWith('/transactions')) {
        transactionPosts.push(request.url());
      }
    });

    await authenticate(page);
    await page.goto(`${ui}/transactions/create`);
    await expect(page.locator('[data-testid="transaction-create-page"]')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Crear transacción ACH', exact: true })).toBeVisible();
    await expect(page.getByText('Referencia legado')).toHaveCount(0);
    await expect(page.locator('input[formcontrolname="reference"]')).toHaveCount(0);
    expect(await page.locator('mat-form-field').count()).toBeGreaterThan(10);
    expect(await page.locator('mat-select').count()).toBeGreaterThan(3);
    await page.screenshot({
      path: resolve(evidenceDir, `formulario-${viewport.name}.png`),
      fullPage: true
    });

    await expect(page.locator('[data-testid="validation-summary"]')).toHaveCount(0);
    await page.locator('[data-testid="transaction-submit"]').click();
    await expect(page.locator('[data-testid="validation-summary"]')).toBeVisible();
    await expect(page.getByText('Ingrese el identificador único de la operación.')).toBeVisible();
    await expect(page.getByText('Ingrese el valor de la transacción.')).toBeVisible();
    await expect(page.getByText('Seleccione la entidad financiera destino.')).toBeVisible();
    await expect(page.getByLabel('Valor de la transacción', { exact: true })).toBeFocused();
    expect(transactionPosts).toEqual([]);

    const transactionType = page.getByRole('combobox', { name: 'Tipo de operación', exact: true });
    await transactionType.scrollIntoViewIfNeeded();
    await transactionType.click();
    await page.keyboard.press('ArrowDown');
    await page.keyboard.press('Enter');
    await expect(transactionType).toContainText('Débito');
    await page.locator('[data-testid="transaction-submit"]').click();
    await expect(page.getByText('Ingrese el código del recaudador.')).toBeVisible();
    await expect(page.getByText('Ingrese el código de cliente del receptor.')).toBeVisible();
    await expect(page.getByText('Ingrese la descripción del servicio.')).toBeVisible();

    const dimensions = await page.evaluate(() => ({
      documentWidth: document.documentElement.scrollWidth,
      viewportWidth: document.documentElement.clientWidth,
      offenders: [...document.querySelectorAll<HTMLElement>('body *')]
        .map((element) => {
          const rect = element.getBoundingClientRect();
          return {
            tag: element.tagName.toLowerCase(),
            className: element.className?.toString().slice(0, 120) ?? '',
            parentClassName: element.parentElement?.className?.toString().slice(0, 120) ?? '',
            text: element.textContent?.trim().slice(0, 80) ?? '',
            left: Math.round(rect.left),
            right: Math.round(rect.right),
            width: Math.round(rect.width)
          };
        })
        .filter((item) => item.right > document.documentElement.clientWidth + 1 || item.left < -1)
        .slice(0, 15)
    }));
    expect(
      dimensions.documentWidth,
      `Elementos fuera del viewport: ${JSON.stringify(dimensions.offenders)}`
    ).toBeLessThanOrEqual(dimensions.viewportWidth + 1);
    await page.screenshot({
      path: resolve(evidenceDir, `validacion-${viewport.name}.png`),
      fullPage: true
    });

    await testInfo.attach(`transactions-create-${viewport.name}.png`, {
      body: await page.screenshot({ fullPage: true }),
      contentType: 'image/png'
    });

    expect(failedRequests).toEqual([]);
    expect(consoleErrors.filter((message) => !/favicon|ResizeObserver/i.test(message))).toEqual([]);
  });
}

async function authenticate(page: Page): Promise<void> {
  await page.goto(`${ui}/login`);
  await page.getByLabel('Usuario').fill(username);
  await page.locator('input[formcontrolname="password"]').fill(password);
  const login = page.waitForResponse((response) =>
    response.request().method() === 'POST'
    && new URL(response.url()).pathname.toLowerCase().endsWith('/auth/login')
  );
  await page.getByRole('button', { name: 'Ingresar', exact: true }).click();
  expect((await login).ok()).toBeTruthy();
  await expect(page).not.toHaveURL(/\/login(?:\?.*)?$/);
}
