import { expect, Page, test } from '@playwright/test';
import { createHash } from 'node:crypto';
import { readFileSync } from 'node:fs';
import path from 'node:path';

type UploadResult = {
  message?: string;
  fileHash?: string;
  ingestionId?: string;
  correlationId?: string;
  ingestionStatus?: string;
  cycleResolutionStatus?: string;
  parsingStatus?: string;
  detectedClearingHouseId?: number;
  resolvedClearingHouseId?: number;
  profileSelectionStatus?: string | null;
  selectedProfileCode?: string | null;
  totalBatches?: number;
  totalEntries?: number;
  totalAddendas?: number;
};

const enabled = process.env['RUN_JOB5C_PRODUCTION_REFERENCE_LIVE'] === 'true';
const ui = (process.env['ACH_UI_URL'] ?? 'http://localhost:743').replace(/\/+$/, '');
const api = (process.env['ACH_API_URL'] ?? 'http://localhost:843').replace(/\/+$/, '');
const username = process.env['ACH_USER'] ?? 'admin';
const password = process.env['ACH_PASS'] ?? '';
const sourceFile = process.env['JOB5C_SOURCE_FILE'] ?? '';
const expectedClearingHouse = process.env['JOB5C_CLEARING_HOUSE'] ?? 'CENIT';
const uploadEndpoint = /\/NachaUpload\/upload(?:\?.*)?$/;

test.use({ trace: 'off', screenshot: 'off', video: 'off' });

test.describe.serial('JOB 5C - referencia productiva local controlada', () => {
  test.skip(!enabled, 'RUN_JOB5C_PRODUCTION_REFERENCE_LIVE=true habilita esta prueba Live local.');
  test.setTimeout(180_000);

  test('carga por SPA, bloquea el procesamiento no elegible y audita la segunda carga', async ({ page }) => {
    expect(sourceFile, 'JOB5C_SOURCE_FILE es obligatorio.').not.toBe('');

    const browserErrors: string[] = [];
    const httpErrors: string[] = [];
    page.on('pageerror', error => browserErrors.push(error.message));
    page.on('console', message => {
      if (message.type() === 'error') {
        browserErrors.push(message.text());
      }
    });
    page.on('response', response => {
      if (response.status() >= 500 || (response.status() === 404 && !response.url().includes('favicon'))) {
        httpErrors.push(`${response.status()} ${new URL(response.url()).pathname}`);
      }
    });

    const token = await loginThroughUi(page);
    const headers = { Authorization: `Bearer ${token}`, 'X-Correlation-ID': crypto.randomUUID() };
    const dashboardBefore = await getResponseCount(page, headers);
    const clearingHouse = await resolveClearingHouse(page, headers, expectedClearingHouse);

    await navigateToUploadFromMenu(page);
    const fileName = path.basename(sourceFile);
    const expectedHash = createHash('sha256').update(readFileSync(sourceFile)).digest('hex').toUpperCase();

    const first = await uploadFromUi(page, sourceFile, fileName);
    expect(first.fileHash?.toUpperCase()).toBe(expectedHash);
    expect(first.detectedClearingHouseId).toBe(clearingHouse.id);
    expect(first.profileSelectionStatus ?? null).toBeNull();
    expect(first.selectedProfileCode ?? null).toBeNull();
    expect(first.cycleResolutionStatus).toMatch(/NoResuelto|Ambiguo/i);
    expect(first.parsingStatus).toBe('NoEjecutado');
    expect(first.totalEntries ?? 0).toBe(0);
    expect(await getResponseCount(page, headers)).toBe(dashboardBefore);

    const second = await uploadFromUi(page, sourceFile, fileName);
    expect(second.fileHash?.toUpperCase()).toBe(expectedHash);
    expect(second.ingestionStatus).toBe('Duplicado');
    expect(second.parsingStatus).toBe(first.parsingStatus);
    expect(await getResponseCount(page, headers)).toBe(dashboardBefore);

    await page.goto(`${ui}/ach/reconciliation`);
    await expect(page.getByRole('heading', { name: 'Consola de conciliación ACH', exact: true })).toBeVisible();
    expect(await page.locator('body').innerText()).not.toContain('[object Object]');
    expect(browserErrors).toEqual([]);
    expect(httpErrors).toEqual([]);

    console.log(`JOB5C_RESULT=${JSON.stringify({
      camera: expectedClearingHouse,
      file: maskFileName(fileName),
      sha256: expectedHash,
      first: sanitize(first),
      second: sanitize(second),
      achResponsesBefore: dashboardBefore,
      achResponsesAfter: await getResponseCount(page, headers),
      browserErrors: browserErrors.length,
      httpErrors: httpErrors.length
    })}`);
  });
});

async function loginThroughUi(page: Page): Promise<string> {
  expect(password, 'ACH_PASS es obligatorio para la prueba Live local.').not.toBe('');
  await page.goto(`${ui}/login`);
  const loginResponsePromise = page.waitForResponse(response =>
    new URL(response.url()).pathname === '/auth/login'
    && response.request().method() === 'POST'
    && response.status() === 200);
  await page.locator('input[formControlName="username"]').fill(username);
  await page.locator('input[formControlName="password"]').fill(password);
  await page.getByRole('button', { name: 'Ingresar' }).click();
  const loginResponse = await loginResponsePromise;
  const payload = await loginResponse.json() as { data?: { token?: string } };
  expect(payload.data?.token, 'El login SPA real debe devolver token.').toBeTruthy();
  await expect(page).not.toHaveURL(/\/login(?:\?.*)?$/);
  return payload.data!.token!;
}

async function navigateToUploadFromMenu(page: Page): Promise<void> {
  const parent = page.getByRole('button', { name: /Transacciones/i }).first();
  if (await parent.isVisible()) {
    await parent.click();
  }
  await page.getByRole('link', { name: /Cargar NACHA-M/i }).click();
  await expect(page).toHaveURL(/\/transactions\/nacha-upload$/);
  await expect(page.getByRole('heading', { name: 'Cargar archivo NACHA-M' })).toBeVisible();
}

async function uploadFromUi(page: Page, filePath: string, fileName: string): Promise<UploadResult> {
  await page.locator('input[type="file"]').setInputFiles(filePath);
  await expect(page.getByText(`Archivo seleccionado: ${fileName}`, { exact: false })).toBeVisible();
  await expect(page.getByTestId('nacha-upload-selected-kind')).toContainText(/Referencia productiva/i);
  await expect(page.getByTestId('nacha-upload-selected-error')).toHaveCount(0);

  const responsePromise = page.waitForResponse(response =>
    uploadEndpoint.test(response.url()) && response.request().method() === 'POST');
  await page.getByRole('button', { name: 'Cargar archivo' }).click();
  const response = await responsePromise;
  const contentType = response.headers()['content-type'] ?? '';
  const responseText = await response.text();

  expect(contentType).toContain('application/json');
  expect(responseText.trimStart()).not.toMatch(/^<!doctype html|^<html/i);
  expect([200, 422]).toContain(response.status());
  await expect(page.getByTestId('nacha-upload-result')).toBeVisible();
  await expect(page.getByTestId('nacha-upload-result-message')).not.toHaveText(/^$/);
  expect(await page.locator('body').innerText()).not.toContain('[object Object]');

  return JSON.parse(responseText) as UploadResult;
}

async function resolveClearingHouse(
  page: Page,
  headers: Record<string, string>,
  code: string
): Promise<{ id: number; code: string }> {
  const response = await page.request.get(`${api}/clearing-houses?search=${encodeURIComponent(code)}`, { headers });
  expect(response.ok(), 'La cámara debe existir en el API real.').toBeTruthy();
  const payload = await response.json();
  const items = Array.isArray(payload) ? payload : payload.items;
  const match = items.find((item: { code?: string }) => item.code === code);
  expect(match, `No se encontró la cámara ${code}.`).toBeTruthy();
  return match;
}

async function getResponseCount(page: Page, headers: Record<string, string>): Promise<number> {
  const response = await page.request.get(`${api}/api/ach/responses?pageNumber=1&pageSize=1`, { headers });
  expect(response.ok(), 'La consulta diagnóstica de respuestas ACH debe estar disponible.').toBeTruthy();
  return (await response.json() as { totalCount: number }).totalCount;
}

function sanitize(result: UploadResult) {
  return {
    ingestionId: result.ingestionId,
    correlationId: result.correlationId,
    ingestionStatus: result.ingestionStatus,
    cycleResolutionStatus: result.cycleResolutionStatus,
    parsingStatus: result.parsingStatus,
    detectedClearingHouseId: result.detectedClearingHouseId,
    profileSelectionStatus: result.profileSelectionStatus ?? null,
    totalBatches: result.totalBatches ?? 0,
    totalEntries: result.totalEntries ?? 0,
    totalAddendas: result.totalAddendas ?? 0
  };
}

function maskFileName(fileName: string): string {
  const suffix = path.extname(fileName);
  return `${fileName.slice(0, 7)}.***${suffix}`;
}
