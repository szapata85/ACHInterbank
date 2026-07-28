import { createHash } from 'node:crypto';
import { mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { expect, Page, test } from '@playwright/test';

const baseUrl = (process.env['E2E_BASE_URL'] ?? process.env['ACH_UI_URL'] ?? 'http://localhost:743').replace(/\/+$/, '');
const targetCycleId = process.env['NACHA_EXPORT_CYCLE_ID'] ?? 'f78ca7bae2b80c3034353fc3dbccd801c605e7ee';
const errorCycleId = process.env['NACHA_EXPORT_ERROR_CYCLE_ID'] ?? '0be2d05b401a4713ae7a5df83f3b7e808cbb5c1b';
const userName = process.env['E2E_ADMIN_USER'] ?? process.env['ACH_USER'] ?? 'admin';
const password = process.env['E2E_ADMIN_PASSWORD'] ?? process.env['ACH_PASS'] ?? 'Admin123!';
const evidenceDir = resolve(process.cwd(), '../../docs/uat/evidencias/nacha-export-ux');

interface ExportableCycle {
  cycleId: string;
  cycleName: string;
  clearingHouseName: string;
  transactionCount: number;
  batchCount: number;
  isExportable: boolean;
  exportUnavailableReason?: string | null;
}

interface EnvelopeCertificate {
  id: number;
  canDecrypt: boolean;
  purpose: number;
  notBefore: string;
  notAfter: string;
  keyAlgorithm?: string;
  keySize?: number;
  thumbprintMasked: string;
}

test.describe('Exportación NACHA-M — runtime real', () => {
  test.describe.configure({ mode: 'serial' });

  test('acceso, carga y filtros ejecutan una sola consulta', async ({ page }, testInfo) => {
    test.setTimeout(90_000);
    const diagnostics = captureDiagnostics(page);
    await loginThroughUi(page);

    let exportableRequests = 0;
    page.on('request', request => {
      if (request.method() === 'GET' && new URL(request.url()).pathname.endsWith('/ach-cycles/exportable')) {
        exportableRequests += 1;
      }
    });

    await page.goto(`${baseUrl}/ach-cycles/nacha/export`);
    await expect(page.getByRole('heading', { level: 1, name: 'Exportación NACHA-M' })).toBeVisible();
    await expect(page.getByText('Consulta los ciclos disponibles, valida su estado')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Actualizar ciclos NACHA-M' })).toBeVisible();
    await expect(page.getByText('Filtros de consulta')).toBeVisible();
    await expect(page.getByLabel('Cámara compensadora')).toBeVisible();
    await expect(page.getByLabel('Fecha inicial')).toBeVisible();
    await expect(page.getByLabel('Fecha final')).toBeVisible();
    await expect(page.getByLabel('Buscar en resultados')).toBeVisible();
    await expect(page.getByText('Ciclos disponibles', { exact: true })).toBeVisible();
    await expect(page.locator('table[aria-label*="Ciclos disponibles"]')).toBeVisible();

    await expect.poll(() => exportableRequests).toBe(1);
    await page.locator('mat-select[formcontrolname="clearingHouseId"] .mat-mdc-select-trigger').click({ force: true });
    await page.getByRole('option', { name: 'ACH Colombia' }).click();
    const beforeApply = exportableRequests;
    await Promise.all([
      page.waitForResponse(response =>
        response.request().method() === 'GET'
        && new URL(response.url()).pathname.endsWith('/ach-cycles/exportable')
        && response.status() === 200),
      page.getByRole('button', { name: 'Aplicar filtros' }).click()
    ]);
    expect(exportableRequests - beforeApply).toBe(1);

    const beforeClear = exportableRequests;
    await Promise.all([
      page.waitForResponse(response =>
        response.request().method() === 'GET'
        && new URL(response.url()).pathname.endsWith('/ach-cycles/exportable')
        && response.status() === 200),
      page.getByRole('button', { name: 'Limpiar', exact: true }).click()
    ]);
    expect(exportableRequests - beforeClear).toBe(1);

    await page.evaluate(() => window.scrollTo(0, 0));
    await page.waitForTimeout(250);
    const screenshot = await page.screenshot({ fullPage: true });
    saveEvidence('nacha-export-desktop-1440x900.png', screenshot);
    await testInfo.attach('nacha-export-desktop-1440x900.png', { body: screenshot, contentType: 'image/png' });
    expect(diagnostics.consoleErrors).toEqual([]);
    expect(diagnostics.networkConsoleErrors).toEqual([]);
    expect(diagnostics.serverErrors).toEqual([]);
  });

  test('descarga NACHA-M, evita doble ejecución y valida round-trip del sobre digital', async ({ page }, testInfo) => {
    test.setTimeout(120_000);
    const diagnostics = captureDiagnostics(page);
    await loginThroughUi(page);
    const token = await accessToken(page);
    const target = await getCycle(page, token, targetCycleId);

    await page.goto(`${baseUrl}/ach-cycles/nacha/export`);
    const row = cycleRow(page, targetCycleId);
    await expect(row).toBeVisible();

    const plainRequests: string[] = [];
    page.on('request', request => {
      if (new URL(request.url()).pathname === `/NachaExport/${targetCycleId}`) {
        plainRequests.push(request.url());
      }
    });

    await openActions(row);
    const plainResponsePromise = page.waitForResponse(response =>
      new URL(response.url()).pathname === `/NachaExport/${targetCycleId}`);
    const plainDownloadPromise = page.waitForEvent('download');
    const plainMenuItem = page.getByRole('menuitem', { name: 'Descargar NACHA-M' });
    await plainMenuItem.evaluate((element: HTMLElement) => {
      element.click();
      element.click();
    });
    const [plainResponse, plainDownload] = await Promise.all([plainResponsePromise, plainDownloadPromise]);
    expect(plainResponse.status()).toBe(200);
    expect(plainResponse.headers()['content-type']).toContain('text/plain');
    expect(plainRequests).toHaveLength(1);
    const plainName = plainDownload.suggestedFilename();
    expect(plainName).toBe(extractFileName(plainResponse.headers()['content-disposition']));
    expect(plainName).toMatch(/^\d{7}\.\d{3}\.\d+$/);
    const plainPath = testInfo.outputPath(plainName);
    await plainDownload.saveAs(plainPath);
    const plainBytes = readFileSync(plainPath);
    const nachaEvidence = validateNacha(plainBytes);

    await expect(row.getByText(/Generado|Protegido/)).toBeVisible();
    await openActions(row);
    const envelopeResponsePromise = page.waitForResponse(response =>
      new URL(response.url()).pathname === `/NachaExport/${targetCycleId}/sobre-digital`);
    const envelopeDownloadPromise = page.waitForEvent('download');
    await page.getByRole('menuitem', { name: 'Generar sobre digital' }).click();
    const [envelopeResponse, envelopeDownload] = await Promise.all([envelopeResponsePromise, envelopeDownloadPromise]);
    expect(envelopeResponse.status()).toBe(200);
    expect(envelopeResponse.headers()['content-type']).toContain('application/octet-stream');
    expect(new URL(envelopeResponse.url()).searchParams.get('forceEncryption')).toBe('true');
    const envelopeName = envelopeDownload.suggestedFilename();
    expect(envelopeName).toBe(`${plainName}.ENV`);
    const envelopePath = testInfo.outputPath(envelopeName);
    await envelopeDownload.saveAs(envelopePath);
    const envelopeBytes = readFileSync(envelopePath);
    validateEnvelopeContainer(envelopeBytes);
    expect(envelopeBytes.equals(plainBytes)).toBe(false);

    const certificatesResponse = await page.request.get(`${baseUrl}/api/nacha-security/digital-envelope/certificates`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(certificatesResponse.status()).toBe(200);
    const certificates = await certificatesResponse.json() as EnvelopeCertificate[];
    const decryptCertificate = certificates
      .filter(certificate => certificate.canDecrypt)
      .sort((left, right) => right.id - left.id)[0];
    expect(decryptCertificate, 'Debe existir el certificado privado controlado de round-trip.').toBeTruthy();

    const decryptResponse = await page.request.post(`${baseUrl}/api/nacha-security/digital-envelope/decrypt`, {
      headers: { Authorization: `Bearer ${token}` },
      multipart: {
        certificateVersionId: String(decryptCertificate.id),
        file: {
          name: envelopeName,
          mimeType: 'application/octet-stream',
          buffer: envelopeBytes
        }
      }
    });
    expect(decryptResponse.status(), await safeProblem(decryptResponse)).toBe(200);
    const decryptedBytes = Buffer.from(await decryptResponse.body());
    validateNacha(decryptedBytes);
    expect(sha256(decryptedBytes)).toBe(sha256(plainBytes));
    expect(sha256(decryptedBytes)).toBe(envelopeResponse.headers()['x-plaintext-sha256']);

    await page.evaluate(() => window.scrollTo(0, 0));
    await page.waitForTimeout(250);
    const screenshot = await page.screenshot();
    saveEvidence('nacha-export-after-envelope.png', screenshot);
    await testInfo.attach('nacha-export-after-envelope.png', { body: screenshot, contentType: 'image/png' });
    const evidence = {
      cycleId: target.cycleId,
      cycleName: target.cycleName,
      clearingHouseName: target.clearingHouseName,
      transactionCount: target.transactionCount,
      batchCount: target.batchCount,
      plain: {
        fileName: plainName,
        status: plainResponse.status(),
        contentType: plainResponse.headers()['content-type'],
        size: plainBytes.length,
        sha256Prefix: sha256(plainBytes).slice(0, 16),
        ...nachaEvidence
      },
      envelope: {
        fileName: envelopeName,
        status: envelopeResponse.status(),
        contentType: envelopeResponse.headers()['content-type'],
        size: envelopeBytes.length,
        cryptographicProfile: envelopeResponse.headers()['x-cryptographic-profile'],
        certificateVersionId: decryptCertificate.id,
        certificatePurpose: decryptCertificate.purpose,
        certificateValidity: { notBefore: decryptCertificate.notBefore, notAfter: decryptCertificate.notAfter },
        certificateThumbprintMasked: decryptCertificate.thumbprintMasked,
        roundTripMatches: sha256(decryptedBytes) === sha256(plainBytes)
      },
      effectivePlainRequests: plainRequests.length
    };
    saveEvidence('nacha-export-runtime-evidence.json', Buffer.from(JSON.stringify(evidence, null, 2)));
    await testInfo.attach('nacha-export-runtime-evidence.json', {
      body: JSON.stringify(evidence, null, 2),
      contentType: 'application/json'
    });
    expect(diagnostics.consoleErrors).toEqual([]);
    expect(diagnostics.networkConsoleErrors).toEqual([]);
    expect(diagnostics.serverErrors).toEqual([]);
  });

  test('muestra Problem Details 422 accionable y permite reintentar', async ({ page }, testInfo) => {
    test.setTimeout(90_000);
    const diagnostics = captureDiagnostics(page);
    await loginThroughUi(page);
    const token = await accessToken(page);
    await getCycle(page, token, errorCycleId);
    await page.goto(`${baseUrl}/ach-cycles/nacha/export`);
    const row = cycleRow(page, errorCycleId);
    await expect(row).toBeVisible();

    let downloadStarted = false;
    page.once('download', () => {
      downloadStarted = true;
    });
    await openActions(row);
    const responsePromise = page.waitForResponse(response =>
      new URL(response.url()).pathname === `/NachaExport/${errorCycleId}`);
    await page.getByRole('menuitem', { name: 'Descargar NACHA-M' }).click();
    const response = await responsePromise;
    expect(response.status()).toBe(422);
    expect(response.headers()['content-type']).toContain('application/problem+json');
    const problem = await response.json() as {
      title: string;
      detail: string;
      errorCode: string;
      traceId: string;
    };
    expect(problem.errorCode).toBeTruthy();
    expect(problem.detail).toBeTruthy();
    expect(problem.traceId).toBeTruthy();
    await expect(page.getByRole('alert')).toContainText(/No (se puede|fue posible)|No hay/);
    await page.getByText('Información para soporte').click();
    await expect(page.getByRole('alert')).toContainText(problem.errorCode);
    await expect(page.getByRole('alert')).toContainText(problem.traceId);
    await expect(page.getByRole('button', { name: 'Volver a intentar' })).toBeVisible();
    expect(downloadStarted).toBe(false);

    await page.evaluate(() => window.scrollTo(0, 0));
    await page.waitForTimeout(250);
    const screenshot = await page.screenshot();
    saveEvidence('nacha-export-business-error-422.png', screenshot);
    await testInfo.attach('nacha-export-business-error-422.png', { body: screenshot, contentType: 'image/png' });
    expect(diagnostics.consoleErrors).toEqual([]);
    expect(diagnostics.networkConsoleErrors).toHaveLength(1);
    expect(diagnostics.serverErrors).toEqual([]);
  });

  test('responsive tablet y móvil mantiene filtros y acciones utilizables', async ({ page }, testInfo) => {
    test.setTimeout(90_000);
    await page.setViewportSize({ width: 768, height: 1024 });
    const diagnostics = captureDiagnostics(page);
    await loginThroughUi(page);
    await page.goto(`${baseUrl}/ach-cycles/nacha/export`);
    await expect(page.getByRole('heading', { level: 1, name: 'Exportación NACHA-M' })).toBeVisible();
    await expect(page.locator('table[aria-label*="Ciclos disponibles"]')).toBeVisible();
    const tabletOverflow = await page.locator('body').evaluate(body => ({
      clientWidth: body.clientWidth,
      scrollWidth: body.scrollWidth
    }));
    expect(tabletOverflow.scrollWidth).toBeLessThanOrEqual(tabletOverflow.clientWidth + 1);
    const tabletScreenshot = await page.screenshot({ fullPage: true });
    saveEvidence('nacha-export-tablet-768x1024.png', tabletScreenshot);
    await testInfo.attach('nacha-export-tablet-768x1024.png', {
      body: tabletScreenshot,
      contentType: 'image/png'
    });

    await page.setViewportSize({ width: 390, height: 844 });
    await expect(page.locator('.mobile-list')).toBeVisible();
    const card = page.locator('.cycle-card').filter({ hasText: 'Ciclo 1' }).first();
    await expect(card).toBeVisible();
    await expect(card.getByRole('button', { name: /Acciones de exportación/ })).toBeVisible();

    const overflow = await page.locator('body').evaluate(body => ({
      clientWidth: body.clientWidth,
      scrollWidth: body.scrollWidth
    }));
    expect(overflow.scrollWidth).toBeLessThanOrEqual(overflow.clientWidth + 1);
    const screenshot = await page.screenshot({ fullPage: true });
    saveEvidence('nacha-export-mobile-390x844.png', screenshot);
    await testInfo.attach('nacha-export-mobile-390x844.png', { body: screenshot, contentType: 'image/png' });
    expect(diagnostics.consoleErrors).toEqual([]);
    expect(diagnostics.networkConsoleErrors).toEqual([]);
    expect(diagnostics.serverErrors).toEqual([]);
  });
});

function cycleRow(page: Page, cycleId: string) {
  return page.locator('tbody tr').filter({ hasText: `ID ${cycleId.slice(0, 8)}` });
}

async function openActions(row: ReturnType<typeof cycleRow>): Promise<void> {
  await row.getByRole('button', { name: /Acciones de exportación/ }).click();
}

async function getCycle(page: Page, token: string, cycleId: string): Promise<ExportableCycle> {
  const response = await page.request.get(`${baseUrl}/api/ach-cycles/exportable`, {
    headers: { Authorization: `Bearer ${token}` }
  });
  expect(response.status()).toBe(200);
  const payload = await response.json() as ExportableCycle[] | { items?: ExportableCycle[] };
  const cycles = Array.isArray(payload) ? payload : payload.items ?? [];
  const target = cycles.find(cycle => cycle.cycleId === cycleId);
  expect(target, `El listado debe contener el cycleId ${cycleId}.`).toBeTruthy();
  return target!;
}

function validateNacha(content: Buffer): {
  recordCount: number;
  recordTypes: string[];
  debitTotalMinorUnits: string;
  creditTotalMinorUnits: string;
  isHtml: boolean;
  isJson: boolean;
} {
  expect(content.length).toBeGreaterThan(0);
  expect(content.length % 106).toBe(0);
  expect(content.includes(0x0a)).toBe(false);
  expect(content.includes(0x0d)).toBe(false);
  const text = content.toString('ascii');
  const records = Array.from({ length: content.length / 106 }, (_, index) => text.slice(index * 106, (index + 1) * 106));
  expect(records.every(record => record.length === 106)).toBe(true);
  const recordTypes = records.map(record => record[0]);
  for (const required of ['1', '5', '6', '7', '8', '9']) {
    expect(recordTypes).toContain(required);
  }
  const batchControls = records.filter(record => record.startsWith('8'));
  const debit = batchControls.reduce((sum, record) => sum + BigInt(record.slice(20, 38)), 0n);
  const credit = batchControls.reduce((sum, record) => sum + BigInt(record.slice(38, 56)), 0n);
  const fileControl = records.find(record => record.startsWith('9'))!;
  expect(BigInt(fileControl.slice(31, 49))).toBe(debit);
  expect(BigInt(fileControl.slice(49, 67))).toBe(credit);
  expect(text.toLowerCase()).not.toContain('<html');
  expect(text.trimStart().startsWith('{')).toBe(false);
  return {
    recordCount: records.length,
    recordTypes: [...new Set(recordTypes)],
    debitTotalMinorUnits: debit.toString(),
    creditTotalMinorUnits: credit.toString(),
    isHtml: false,
    isJson: false
  };
}

function validateEnvelopeContainer(content: Buffer): void {
  expect(content.length).toBeGreaterThan(0);
  const text = content.toString('utf8');
  expect(text).toContain('<envelope ');
  expect(text).toContain('<keyEncryptionAlgorithm>RSA/NONE/PKCS1Padding</keyEncryptionAlgorithm>');
  expect(text).toContain('<contentEncryptionAlgorithm>AES/CBC/PKCS5padding</contentEncryptionAlgorithm>');
  expect(text).toContain('<encryptedKey>');
  expect(text).toContain('<encryptedContent>');
  expect(text.toLowerCase()).not.toContain('<html');
  expect(text.trimStart().startsWith('{')).toBe(false);
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

function captureDiagnostics(page: Page) {
  const consoleErrors: string[] = [];
  const networkConsoleErrors: string[] = [];
  const serverErrors: Array<{ path: string; status: number }> = [];
  page.on('pageerror', error => consoleErrors.push(error.message));
  page.on('console', message => {
    if (message.type() !== 'error') return;
    if (message.text().startsWith('Failed to load resource:')) {
      networkConsoleErrors.push(message.text());
      return;
    }
    consoleErrors.push(message.text());
  });
  page.on('response', response => {
    if (response.status() >= 500) {
      serverErrors.push({ path: new URL(response.url()).pathname, status: response.status() });
    }
  });
  return { consoleErrors, networkConsoleErrors, serverErrors };
}

function saveEvidence(name: string, content: Buffer): void {
  mkdirSync(evidenceDir, { recursive: true });
  writeFileSync(resolve(evidenceDir, name), content);
}

async function accessToken(page: Page): Promise<string> {
  const token = await page.evaluate(() => sessionStorage.getItem('ach.interbank.access_token'));
  expect(token).toBeTruthy();
  return token!;
}

async function safeProblem(response: import('@playwright/test').APIResponse): Promise<string> {
  if (response.ok()) return '';
  const contentType = response.headers()['content-type'] ?? '';
  return contentType.includes('json') ? await response.text() : `HTTP ${response.status()}`;
}

async function loginThroughUi(page: Page): Promise<void> {
  await page.goto(`${baseUrl}/login`);
  await page.locator('input[formControlName="username"]').fill(userName);
  await page.locator('input[formControlName="password"]').fill(password);
  await Promise.all([
    page.waitForResponse(response => new URL(response.url()).pathname === '/auth/login' && response.status() === 200),
    page.getByRole('button', { name: 'Ingresar' }).click()
  ]);
  await expect(page).not.toHaveURL(/\/login$/);
}
