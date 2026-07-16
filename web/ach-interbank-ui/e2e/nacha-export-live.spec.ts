import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { createHash } from 'node:crypto';
import { dirname } from 'node:path';
import { expect, Page, test } from '@playwright/test';

const baseUrl = (process.env['E2E_BASE_URL'] ?? process.env['ACH_UI_URL'] ?? 'http://localhost:743').replace(/\/+$/, '');
const targetCycleId = process.env['NACHA_EXPORT_CYCLE_ID'] ?? '5744d040c6a9110d3cf951f77cbd64221086d30b';
const userName = process.env['E2E_ADMIN_USER'] ?? process.env['ACH_USER'] ?? 'admin';
const password = process.env['E2E_ADMIN_PASSWORD'] ?? process.env['ACH_PASS'] ?? 'Admin123!';

interface ExportableCycle {
  cycleId: string;
  cycleName: string;
  clearingHouseName: string;
  transactionCount: number;
  isExportable: boolean;
  exportUnavailableReason?: string | null;
}

interface EnvelopeCertificate {
  id: number;
  canDecrypt: boolean;
  thumbprintMasked: string;
}

interface CapturedResponse {
  url: string;
  method: string;
  status: number;
  requestHeaders: Record<string, string>;
  responseHeaders: Record<string, string>;
  body?: string;
}

test('captura cada accion NACHA-M como una unica solicitud real', async ({ page }, testInfo) => {
  test.setTimeout(90_000);
  const requestUrls: string[] = [];
  const captured: CapturedResponse[] = [];
  const consoleErrors: string[] = [];
  const failedApiResponses: Array<{ url: string; status: number; contentType: string }> = [];

  page.on('request', (request) => {
    if (new URL(request.url()).pathname.startsWith('/NachaExport/')) {
      requestUrls.push(request.url());
    }
  });
  page.on('response', async (response) => {
    const path = new URL(response.url()).pathname;
    if ((path.startsWith('/NachaExport/') || path.startsWith('/api/nacha-security/')) && response.status() >= 400) {
      failedApiResponses.push({
        url: response.url(),
        status: response.status(),
        contentType: response.headers()['content-type'] ?? ''
      });
    }
    if (!path.startsWith('/NachaExport/')) return;
    const requestHeaders = { ...response.request().headers() };
    delete requestHeaders['authorization'];
    const contentType = response.headers()['content-type'] ?? '';
    captured.push({
      url: response.url(),
      method: response.request().method(),
      status: response.status(),
      requestHeaders,
      responseHeaders: response.headers(),
      body: contentType.includes('json') ? await response.text() : undefined
    });
  });
  page.on('console', (message) => {
    if (message.type() === 'error') consoleErrors.push(message.text());
  });

  await loginThroughUi(page);
  const token = await page.evaluate(() => sessionStorage.getItem('ach.interbank.access_token'));
  expect(token).toBeTruthy();
  const cyclesResponse = await page.request.get(`${baseUrl}/ach-cycles/exportable`, {
    headers: { Authorization: `Bearer ${token}` }
  });
  expect(cyclesResponse.status()).toBe(200);
  const cyclesPayload = await cyclesResponse.json() as ExportableCycle[] | { items?: ExportableCycle[] };
  const cycles = Array.isArray(cyclesPayload) ? cyclesPayload : cyclesPayload.items ?? [];
  const target = cycles.find((cycle) => cycle.cycleId === targetCycleId);
  expect(target, `El listado debe contener el cycleId ${targetCycleId}.`).toBeTruthy();

  await page.goto(`${baseUrl}/ach-cycles/nacha/export`);
  await expect(page.getByRole('heading', { name: 'Exportar archivos NACHA-M' })).toBeVisible();
  const row = page.locator('.ag-center-cols-container .ag-row').filter({ hasText: target!.cycleName });
  await expect(row).toHaveCount(1);
  await page.screenshot({ path: testInfo.outputPath('before-export.png'), fullPage: true });

  const plainResponsePromise = page.waitForResponse((response) =>
    new URL(response.url()).pathname === `/NachaExport/${targetCycleId}`
  );
  const plainDownloadPromise = page.waitForEvent('download');
  await row.locator('[data-action="generar-nacha"]').click();
  const [plainResponse, plainDownload] = await Promise.all([plainResponsePromise, plainDownloadPromise]);
  expect(plainResponse.status()).toBe(200);
  await expect.poll(() => requestUrls.length).toBe(1);
  expect(new URL(requestUrls[0]).pathname).toBe(`/NachaExport/${targetCycleId}`);
  const plainName = plainDownload.suggestedFilename();
  expect(plainName).toMatch(/^\d{7}\.\d{3}\.\d{8}\.\d+$/);
  const plainPath = testInfo.outputPath(plainName);
  await plainDownload.saveAs(plainPath);
  const plainBytes = readFileSync(plainPath);
  validateNacha(plainBytes);
  await page.screenshot({ path: testInfo.outputPath('after-plain-action.png'), fullPage: true });

  const encryptedButton = row.locator('[data-action="generar-sobre"]');
  await expect(encryptedButton).toBeEnabled();
  const encryptedResponsePromise = page.waitForResponse((response) =>
    new URL(response.url()).pathname === `/NachaExport/${targetCycleId}/sobre-digital`
  );
  const encryptedDownloadPromise = page.waitForEvent('download');
  await encryptedButton.click();
  const [encryptedResponse, encryptedDownload] = await Promise.all([encryptedResponsePromise, encryptedDownloadPromise]);
  expect(encryptedResponse.status()).toBe(200);
  await expect.poll(() => requestUrls.length).toBe(2);
  expect(new URL(requestUrls[1]).pathname).toBe(`/NachaExport/${targetCycleId}/sobre-digital`);
  expect(new URL(requestUrls[1]).searchParams.get('forceEncryption')).toBe('true');
  const encryptedName = encryptedDownload.suggestedFilename();
  expect(encryptedName).toMatch(/^\d{7}\.\d{3}\.\d{8}\.\d+\.ENV$/);
  const encryptedPath = testInfo.outputPath(encryptedName);
  await encryptedDownload.saveAs(encryptedPath);
  const encryptedBytes = readFileSync(encryptedPath);
  expect(encryptedBytes.length).toBeGreaterThan(0);
  expect(encryptedBytes.equals(plainBytes)).toBe(false);

  const certificatesResponse = await page.request.get(`${baseUrl}/api/nacha-security/digital-envelope/certificates`, {
    headers: { Authorization: `Bearer ${token}` }
  });
  expect(certificatesResponse.status()).toBe(200);
  const certificates = await certificatesResponse.json() as EnvelopeCertificate[];
  const decryptCertificate = certificates.find(certificate => certificate.canDecrypt);
  expect(decryptCertificate, 'Debe existir una versión activa con clave privada para la prueba controlada.').toBeTruthy();

  const decryptResponse = await page.request.post(`${baseUrl}/api/nacha-security/digital-envelope/decrypt`, {
    headers: { Authorization: `Bearer ${token}` },
    multipart: {
      certificateVersionId: String(decryptCertificate!.id),
      file: {
        name: encryptedName,
        mimeType: 'application/octet-stream',
        buffer: encryptedBytes
      }
    }
  });
  if (!decryptResponse.ok()) {
    throw new Error(`Descifrado real falló con HTTP ${decryptResponse.status()}: ${await decryptResponse.text()}`);
  }
  expect(decryptResponse.status()).toBe(200);
  const decryptedBytes = Buffer.from(await decryptResponse.body());
  const decryptedName = extractFileName(decryptResponse.headers()['content-disposition']);
  expect(decryptedName).toBe(encryptedName.replace(/\.ENV$/i, ''));
  validateNacha(decryptedBytes);
  const decryptedPath = testInfo.outputPath(`decrypted-${decryptedName}`);
  writeFileSync(decryptedPath, decryptedBytes);

  const plainHash = sha256(plainBytes);
  const decryptedHash = sha256(decryptedBytes);
  const envelopePlainHash = encryptedResponse.headers()['x-plaintext-sha256'];
  expect(envelopePlainHash).toBeTruthy();
  expect(decryptedHash).toBe(envelopePlainHash);
  expect(decryptedHash).toBe(plainHash);
  expect(decryptedBytes.equals(plainBytes)).toBe(true);
  await page.screenshot({ path: testInfo.outputPath('after-encrypted-action.png'), fullPage: true });

  const evidencePath = testInfo.outputPath('incident-evidence.json');
  mkdirSync(dirname(evidencePath), { recursive: true });
  writeFileSync(evidencePath, JSON.stringify({
    target,
    requestUrls,
    captured,
    consoleErrors,
    failedApiResponses,
    artifacts: {
      plainName,
      encryptedName,
      decryptedName,
      plainSize: plainBytes.length,
      encryptedSize: encryptedBytes.length,
      decryptedSize: decryptedBytes.length,
      plainSha256: plainHash,
      decryptedSha256: decryptedHash,
      certificateThumbprintMasked: decryptCertificate!.thumbprintMasked,
      cryptographicProfile: encryptedResponse.headers()['x-cryptographic-profile']
    }
  }, null, 2));
  await testInfo.attach('incident-evidence', { path: evidencePath, contentType: 'application/json' });

  expect(requestUrls.filter((url) => new URL(url).pathname === `/NachaExport/${targetCycleId}`)).toHaveLength(1);
  expect(requestUrls.filter((url) => new URL(url).pathname.endsWith('/sobre-digital'))).toHaveLength(1);
  expect(failedApiResponses).toEqual([]);
  expect(consoleErrors).toEqual([]);
});

function validateNacha(content: Buffer): void {
  expect(content.length).toBeGreaterThan(0);
  expect(content.length % 106).toBe(0);
  expect(content[0]).toBe('1'.charCodeAt(0));
  const recordTypes = Array.from({ length: content.length / 106 }, (_, index) =>
    String.fromCharCode(content[index * 106]));
  expect(recordTypes).toContain('5');
  expect(recordTypes).toContain('6');
  expect(recordTypes).toContain('7');
  expect(recordTypes).toContain('8');
  expect(recordTypes).toContain('9');
  expect(content.includes(0x0a)).toBe(false);
  expect(content.includes(0x0d)).toBe(false);
  expect(content.subarray(0, 3).equals(Buffer.from([0xef, 0xbb, 0xbf]))).toBe(false);
}

function sha256(content: Buffer): string {
  return createHash('sha256').update(content).digest('hex').toUpperCase();
}

function extractFileName(contentDisposition?: string): string | null {
  if (!contentDisposition) return null;
  const match = /filename\*=UTF-8''([^;]+)|filename="?([^";]+)"?/i.exec(contentDisposition);
  const value = match?.[1] ?? match?.[2];
  return value ? decodeURIComponent(value) : null;
}

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
