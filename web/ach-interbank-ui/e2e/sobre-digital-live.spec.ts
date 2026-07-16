import { createHash } from 'node:crypto';
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { expect, Page, test } from '@playwright/test';

interface EnvelopeCertificate {
  id: number;
  displayName: string;
  fileName: string;
  thumbprintMasked: string;
  canEncrypt: boolean;
  canDecrypt: boolean;
}

const baseUrl = (process.env['E2E_BASE_URL'] ?? process.env['ACH_UI_URL'] ?? 'http://localhost:743').replace(/\/+$/, '');
const userName = process.env['E2E_ADMIN_USER'] ?? process.env['ACH_USER'] ?? 'admin';
const password = process.env['E2E_ADMIN_PASSWORD'] ?? process.env['ACH_PASS'] ?? 'Admin123!';
const originalPath = process.env['DIGITAL_ENVELOPE_TEST_FILE'] ?? resolve(
  process.cwd(),
  '..',
  '..',
  '..',
  'Datos',
  'Nacha_m',
  'ACH colombia 20250331',
  'Entrada1',
  '0001283.001.20250331.1.OUT'
);
const originalName = '0001283.001.20250331.1.OUT';

test('cifra y descifra un NACHA-M real mediante SPA, Nginx, API y SQL Server', async ({ page }, testInfo) => {
  test.setTimeout(90_000);
  const consoleErrors: string[] = [];
  const apiResponses: Array<{ method: string; path: string; status: number; contentType: string }> = [];

  page.on('console', (message) => {
    if (message.type() === 'error') {
      consoleErrors.push(message.text());
    }
  });
  page.on('response', (response) => {
    const url = new URL(response.url());
    if (url.pathname.startsWith('/api/') || url.pathname.startsWith('/auth/')) {
      apiResponses.push({
        method: response.request().method(),
        path: url.pathname,
        status: response.status(),
        contentType: response.headers()['content-type'] ?? ''
      });
    }
  });

  await loginThroughUi(page);
  await page.goto(`${baseUrl}/nacha-security/certificates`);
  await expect(page.getByRole('heading', { name: 'Seguridad de archivos NACHA-M' })).toBeVisible();
  await expect(page.locator('body')).toContainText('ACHcolombia.cer');
  await expect(page.locator('body')).toContainText('CFA.pfx');
  await page.screenshot({ path: testInfo.outputPath('certificates-from-database.png'), fullPage: true });

  const token = await page.evaluate(() => sessionStorage.getItem('ach.interbank.access_token'));
  expect(token).toBeTruthy();
  const certificateResponse = await page.request.get(`${baseUrl}/api/nacha-security/digital-envelope/certificates`, {
    headers: { Authorization: `Bearer ${token}` }
  });
  expect(certificateResponse.status()).toBe(200);
  const certificates = await certificateResponse.json() as EnvelopeCertificate[];
  const certificate = certificates.find((item) => item.canEncrypt && item.canDecrypt);
  expect(certificate, 'Debe existir una identidad activa con llave privada apta para el round-trip.').toBeTruthy();

  await page.goto(`${baseUrl}/nacha-security/sobre-digital`);
  await expect(page.getByRole('heading', { name: 'Herramienta de Sobre Digital' })).toBeVisible();
  await chooseCertificate(page, 'Certificado para cifrar', certificate!.fileName);
  await chooseCertificate(page, 'Certificado para descifrar', certificate!.fileName);
  await page.getByLabel('Archivo para cifrar').setInputFiles(originalPath);

  const encryptResponsePromise = page.waitForResponse((response) =>
    new URL(response.url()).pathname === '/api/nacha-security/digital-envelope/encrypt'
      && response.request().method() === 'POST'
  );
  const encryptDownloadPromise = page.waitForEvent('download');
  await page.getByRole('button', { name: 'Cifrar archivo', exact: true }).click();
  const [encryptResponse, encryptDownload] = await Promise.all([encryptResponsePromise, encryptDownloadPromise]);
  expect(encryptResponse.status()).toBe(200);
  expect(encryptResponse.headers()['content-type']).not.toContain('text/html');
  expect(encryptDownload.suggestedFilename()).toBe(`${originalName}.ENV`);
  const encryptedPath = testInfo.outputPath(`${originalName}.ENV`);
  await encryptDownload.saveAs(encryptedPath);

  const original = readFileSync(originalPath);
  const encrypted = readFileSync(encryptedPath);
  expect(encrypted.length).toBeGreaterThan(0);
  expect(encrypted.equals(original)).toBe(false);
  await expect(page.locator('.result').filter({ hasText: `${originalName}.ENV` })).toBeVisible();
  await page.screenshot({ path: testInfo.outputPath('encryption-completed.png'), fullPage: true });

  await page.getByLabel('Archivo .ENV para descifrar').setInputFiles(encryptedPath);
  const decryptResponsePromise = page.waitForResponse((response) =>
    new URL(response.url()).pathname === '/api/nacha-security/digital-envelope/decrypt'
      && response.request().method() === 'POST'
  );
  const decryptDownloadPromise = page.waitForEvent('download');
  await page.getByRole('button', { name: 'Descifrar archivo', exact: true }).click();
  const [decryptResponse, decryptDownload] = await Promise.all([decryptResponsePromise, decryptDownloadPromise]);
  expect(decryptResponse.status()).toBe(200);
  expect(decryptResponse.headers()['content-type']).not.toContain('text/html');
  expect(decryptDownload.suggestedFilename()).toBe(originalName);
  const decryptedPath = testInfo.outputPath(`recovered-${originalName}`);
  await decryptDownload.saveAs(decryptedPath);

  const decrypted = readFileSync(decryptedPath);
  const originalSha256 = sha256(original);
  const decryptedSha256 = sha256(decrypted);
  expect(decrypted.equals(original)).toBe(true);
  expect(decryptedSha256).toBe(originalSha256);
  await expect(page.locator('.result').filter({ hasText: `Recuperado: ${originalName}` })).toBeVisible();
  await page.screenshot({ path: testInfo.outputPath('decryption-completed.png'), fullPage: true });

  const rejectedResponses = apiResponses.filter((response) =>
    [404, 405, 500].includes(response.status)
      || (response.path.startsWith('/api/') && response.contentType.toLowerCase().includes('text/html'))
  );
  expect(rejectedResponses).toEqual([]);
  expect(consoleErrors).toEqual([]);

  const evidence = {
    originalName,
    encryptedName: encryptDownload.suggestedFilename(),
    decryptedName: decryptDownload.suggestedFilename(),
    originalSize: original.length,
    encryptedSize: encrypted.length,
    decryptedSize: decrypted.length,
    originalSha256,
    decryptedSha256,
    byteIdentical: decrypted.equals(original),
    certificateVersionId: certificate!.id,
    certificateThumbprint: certificate!.thumbprintMasked,
    apiResponses,
    consoleErrors
  };
  const evidencePath = testInfo.outputPath('sobre-digital-evidence.json');
  mkdirSync(dirname(evidencePath), { recursive: true });
  writeFileSync(evidencePath, JSON.stringify(evidence, null, 2));
  await testInfo.attach('sobre-digital-evidence', { path: evidencePath, contentType: 'application/json' });
});

async function loginThroughUi(page: Page): Promise<void> {
  await page.goto(`${baseUrl}/login`);
  await page.locator('input[formControlName="username"]').fill(userName);
  await page.locator('input[formControlName="password"]').fill(password);
  await Promise.all([
    page.waitForResponse((response) => new URL(response.url()).pathname === '/auth/login' && response.status() === 200),
    page.getByRole('button', { name: 'Ingresar' }).click()
  ]);
  await expect(page).not.toHaveURL(/\/login$/);
}

async function chooseCertificate(page: Page, label: string, fileName: string): Promise<void> {
  const select = page.getByLabel(label);
  const option = select.locator('option', { hasText: fileName }).first();
  await expect(option).toBeAttached();
  const value = await option.getAttribute('value');
  expect(value).toBeTruthy();
  await select.selectOption(value!);
}

function sha256(content: Buffer): string {
  return createHash('sha256').update(content).digest('hex').toUpperCase();
}
