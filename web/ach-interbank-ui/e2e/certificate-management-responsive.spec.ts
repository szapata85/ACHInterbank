import { expect, test } from '@playwright/test';
import { mkdir } from 'node:fs/promises';
import { resolve } from 'node:path';
import { loginThroughUi } from './support/live-ui-auth';

const spa = (process.env['E2E_BASE_URL'] ?? 'http://localhost:743').replace(/\/+$/, '');
const evidenceDirectory = resolve(
  process.cwd(),
  '../../docs/uat/evidencias/certificados-live'
);

test.use({ trace: 'off', video: 'off', screenshot: 'off' });

test('mantiene la administración y el sobre digital utilizables en móvil', async ({ page }) => {
  await mkdir(evidenceDirectory, { recursive: true });
  await page.setViewportSize({ width: 390, height: 844 });

  const pageErrors: string[] = [];
  const unexpectedResponses: string[] = [];
  page.on('pageerror', error => pageErrors.push(error.message));
  page.on('response', response => {
    if (response.status() >= 400) {
      unexpectedResponses.push(`${response.status()} ${new URL(response.url()).pathname}`);
    }
  });

  await loginThroughUi(page);
  await page.goto(`${spa}/nacha-security/certificates`, { waitUntil: 'networkidle' });
  await expect(page.getByRole('heading', {
    name: 'Administración de certificados de seguridad'
  })).toBeVisible();
  await expect(page.getByRole('tab', { name: 'Certificado de CFA' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Cargar certificado de CFA' })).toBeVisible();
  await expect.poll(() => page.evaluate(() =>
    document.documentElement.scrollWidth <= window.innerWidth
  )).toBe(true);
  await page.screenshot({
    path: resolve(evidenceDirectory, 'certificados-movil.png'),
    fullPage: true
  });

  await page.goto(`${spa}/nacha-security/sobre-digital`, { waitUntil: 'networkidle' });
  await expect(page.getByRole('heading', { name: 'Sobre digital', exact: true })).toBeVisible();
  await expect(page.getByRole('tab', { name: 'Descifrar archivo' })).toBeVisible();
  await page.getByRole('tab', { name: 'Descifrar archivo' }).click();
  const activeTabBody = page.locator('.mat-mdc-tab-body-active');
  await expect(activeTabBody.getByText('Modo de operación', { exact: true })).toBeVisible();
  await expect(activeTabBody.locator('.mat-mdc-tab-body-content'))
    .toHaveCSS('transform', 'none');
  await expect.poll(() => page.evaluate(() =>
    document.documentElement.scrollWidth <= window.innerWidth
  )).toBe(true);
  await page.screenshot({
    path: resolve(evidenceDirectory, 'sobre-digital-movil.png'),
    fullPage: true
  });

  expect(pageErrors).toEqual([]);
  expect(unexpectedResponses).toEqual([]);
});
