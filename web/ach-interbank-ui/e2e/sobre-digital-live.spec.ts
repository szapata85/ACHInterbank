import { createHash } from 'node:crypto';
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { expect, Page, test, TestInfo } from '@playwright/test';

interface EnvelopeCertificate {
  id: number;
  displayName: string;
  fileName: string;
  thumbprintMasked: string;
  purpose: string | number;
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
const evidenceDir = resolve(process.cwd(), '../../docs/uat/evidencias/nacha-security-ux/final');

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
  const token = await page.evaluate(() => sessionStorage.getItem('ach.interbank.access_token'));
  expect(token).toBeTruthy();
  const certificateResponse = await page.request.get(`${baseUrl}/api/nacha-security/digital-envelope/certificates`, {
    headers: { Authorization: `Bearer ${token}` }
  });
  expect(certificateResponse.status()).toBe(200);
  const certificates = await certificateResponse.json() as EnvelopeCertificate[];
  const encryptionCertificate = newestCertificate(certificates, 'OutboundEncryption');
  const decryptionCertificate = newestCertificate(certificates, 'InboundDecryption');
  expect(encryptionCertificate, 'Debe existir un certificado activo de cifrado de salida.').toBeTruthy();
  expect(decryptionCertificate, 'Debe existir una identidad activa de descifrado con llave privada.').toBeTruthy();

  await page.goto(`${baseUrl}/nacha-security/certificates`);
  await expect(page.getByRole('heading', { level: 1, name: 'Certificados de seguridad NACHA-M' })).toBeVisible();
  await expect(page.locator('body')).toContainText(encryptionCertificate!.displayName);
  await expect(page.locator('body')).toContainText(decryptionCertificate!.displayName);
  await expect(page.locator('body')).not.toContainText(decryptionCertificate!.fileName);
  await saveScreenshot(page, testInfo, 'certificates-from-database.png');

  await page.goto(`${baseUrl}/nacha-security/sobre-digital`);
  await expect(page.getByRole('heading', { level: 1, name: 'Sobre digital NACHA-M' })).toBeVisible();
  await expect(page.getByText('Certificado de cifrado seleccionado automáticamente')).toBeVisible();
  await page.getByLabel('Archivo NACHA-M para cifrar').setInputFiles(originalPath);

  const encryptResponsePromise = page.waitForResponse((response) =>
    new URL(response.url()).pathname === '/api/nacha-security/digital-envelope/encrypt'
      && response.request().method() === 'POST'
  );
  const encryptDownloadPromise = page.waitForEvent('download');
  await page.getByRole('button', { name: 'Generar sobre digital', exact: true }).click();
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
  await expect(page.getByText('Sobre digital generado correctamente', { exact: true })).toBeVisible();
  await expect(page.getByText(`${originalName}.ENV`, { exact: true })).toBeVisible();
  await expect(page.locator('body')).toContainText(encryptionCertificate!.displayName);
  await saveScreenshot(page, testInfo, 'encryption-completed.png');

  await page.getByRole('tab', { name: 'Descifrar archivo' }).click();
  await expect(page.getByText('Identidad privada seleccionada automáticamente')).toBeVisible();
  await page.getByLabel('Sobre digital para descifrar').setInputFiles(encryptedPath);
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
  await expect(page.getByText('Archivo descifrado correctamente', { exact: true })).toBeVisible();
  await expect(page.getByText('Firma digital válida')).toBeVisible();
  await expect(page.getByText('Integridad confirmada')).toBeVisible();
  await expect(page.locator('body')).toContainText(decryptionCertificate!.displayName);
  await saveScreenshot(page, testInfo, 'decryption-completed.png');

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
    encryptionCertificateVersionId: encryptionCertificate!.id,
    encryptionCertificateThumbprint: encryptionCertificate!.thumbprintMasked,
    decryptionCertificateVersionId: decryptionCertificate!.id,
    decryptionCertificateThumbprint: decryptionCertificate!.thumbprintMasked,
    apiResponses,
    consoleErrors
  };
  const evidencePath = testInfo.outputPath('sobre-digital-evidence.json');
  mkdirSync(dirname(evidencePath), { recursive: true });
  writeFileSync(evidencePath, JSON.stringify(evidence, null, 2));
  mkdirSync(evidenceDir, { recursive: true });
  writeFileSync(resolve(evidenceDir, 'sobre-digital-evidence.json'), JSON.stringify(evidence, null, 2));
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

function sha256(content: Buffer): string {
  return createHash('sha256').update(content).digest('hex').toUpperCase();
}

function newestCertificate(
  certificates: EnvelopeCertificate[],
  purpose: 'OutboundEncryption' | 'InboundDecryption'
): EnvelopeCertificate | undefined {
  const numericPurpose = purpose === 'OutboundEncryption' ? 1 : 2;
  return certificates
    .filter(certificate =>
      (certificate.purpose === purpose || certificate.purpose === numericPurpose)
      && (purpose === 'OutboundEncryption' ? certificate.canEncrypt : certificate.canDecrypt))
    .sort((left, right) => right.id - left.id)[0];
}

async function saveScreenshot(page: Page, testInfo: TestInfo, name: string): Promise<void> {
  const screenshot = await page.screenshot({ fullPage: true });
  const outputPath = testInfo.outputPath(name);
  writeFileSync(outputPath, screenshot);
  mkdirSync(evidenceDir, { recursive: true });
  writeFileSync(resolve(evidenceDir, name), screenshot);
  await testInfo.attach(name, { body: screenshot, contentType: 'image/png' });
}
