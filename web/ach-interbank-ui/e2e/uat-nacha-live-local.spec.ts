import { createHash } from 'node:crypto';
import { readFileSync, statSync } from 'node:fs';
import path from 'node:path';
import { BrowserContext, expect, Page, test, TestInfo } from '@playwright/test';

type UploadEvidence = {
  status: number;
  success: boolean | null;
  partial: boolean | null;
  traceId: string | null;
  ingestionId: string | null;
  ingestionStatus: string | null;
  cycleResolutionStatus: string | null;
  parsingStatus: string | null;
  detectedClearingHouseId: number | null;
  resolvedClearingHouseId: number | null;
  resolvedAchCycleId: string | null;
  operationalDate: string | null;
  totalBatches: number | null;
  totalEntries: number | null;
  totalAddendas: number | null;
  errors: string[];
};

type BrowserActivity = {
  consoleErrors: string[];
  pageErrors: string[];
  networkErrors: string[];
};

const uiBaseUrl = process.env['ACH_UI_URL'] ?? 'http://localhost:743';
const apiBaseUrl = process.env['ACH_API_URL'] ?? 'http://localhost:843';
const username = process.env['ACH_USER'] ?? '';
const password = process.env['ACH_PASS'] ?? '';
const uploadPath = '/transactions/nacha-upload';
const soapSettingsPath = '/integraciones/soap-settings';
const mappingsPath = '/integraciones/mappings';
const uploadEndpoint = /\/NachaUpload\/upload(?:\?.*)?$/;
const cenitSourcePath = process.env['CENIT_LIVE_FILE'] ?? path.resolve(
  __dirname,
  '../../../docs/uat/certificados_pruebas/archivo_prueba/CENIT/0001283.001.20260731.1'
);
const achColombiaSourcePath = process.env['ACHCOL_LIVE_FILE'] ?? path.resolve(
  __dirname,
  '../../../docs/uat/certificados_pruebas/archivo_prueba/ACH Colombia/0001283.001.20260727.1.OUT.env'
);

test.describe.configure({ mode: 'serial' });
test.use({ trace: 'off', screenshot: 'on', video: 'on' });

test.describe('NACHA-M LIVE local controlado', () => {
  test.beforeAll(() => {
    test.skip(!stringEquals(process.env['RUN_LIVE_NACHA_E2E'], 'true'), 'RUN_LIVE_NACHA_E2E=true is required.');
    assertLocalEndpoint(uiBaseUrl, 'ACH_UI_URL');
    assertLocalEndpoint(apiBaseUrl, 'ACH_API_URL');
    expect(username, 'ACH_USER must be provided through the process environment.').not.toBe('');
    expect(password, 'ACH_PASS must be provided through the process environment.').not.toBe('');
    expect(statSync(cenitSourcePath).isFile(), 'The authorized CENIT source must exist.').toBeTruthy();
    expect(statSync(achColombiaSourcePath).isFile(), 'The authorized encrypted ACH Colombia source must exist.').toBeTruthy();
    expect(path.basename(achColombiaSourcePath)).toMatch(/\.env$/i);
  });

  test('01 - persisted SOAP settings and published mappings are visible in the SPA', async ({ page, context }, testInfo) => {
    test.setTimeout(120_000);
    const activity = monitorBrowser(page);
    await loginThroughRealForm(page);
    await context.tracing.start({ screenshots: true, snapshots: true, sources: true });

    try {
      await page.goto(resolveUiUrl(soapSettingsPath));
      await expect(page.getByTestId('soap-settings-page')).toBeVisible({ timeout: 20_000 });

      await assertSoapCard(
        page,
        'Proc_Transacciones',
        'http://localhost:7083/WSCFAACH.svc',
        'http://tempuri.org/IWSCFAACH/Proc_Transacciones'
      );
      await assertSoapCard(
        page,
        'RegistrarRespuestaTransaccion',
        'http://localhost:7083/WSAxonRespuestaTransacciones.svc',
        'http://tempuri.org/IWSAxonRespuestaTransacciones/RegistrarRespuestaTransaccion'
      );

      await page.reload();
      await expect(page.getByTestId('soap-settings-page')).toBeVisible({ timeout: 20_000 });
      await assertSoapCard(
        page,
        'RegistrarRespuestaTransaccion',
        'http://localhost:7083/WSAxonRespuestaTransacciones.svc',
        'http://tempuri.org/IWSAxonRespuestaTransacciones/RegistrarRespuestaTransaccion'
      );

      await page.goto(resolveUiUrl(mappingsPath));
      await expect(page.getByTestId('integration-mappings-page')).toBeVisible({ timeout: 20_000 });
      await assertPublishedMappingVisible(page, 'Proc_Transacciones');
      await assertPublishedMappingVisible(page, 'RegistrarRespuestaTransaccion');

      await captureScreenshot(page, testInfo, 'soap-settings-and-mappings.png');
      assertCleanBrowser(activity);
    } finally {
      await stopTrace(context, testInfo, 'soap-settings-and-mappings-trace.zip');
    }
  });

  test('02 - CENIT plaintext upload uses the canonical pipeline and is idempotent', async ({ page, context }, testInfo) => {
    test.setTimeout(180_000);
    const activity = monitorBrowser(page);
    await loginThroughRealForm(page);
    await context.tracing.start({ screenshots: true, snapshots: true, sources: true });

    try {
      const sourceManifest = buildSourceManifest('CENIT', cenitSourcePath);
      const first = await uploadThroughSpa(page, 'CENIT', cenitSourcePath);
      expect(first.status).toBe(200);
      expect(first.ingestionStatus).toBe('Completado');
      expect(first.parsingStatus).toMatch(/^Exitoso/);
      expect(first.resolvedClearingHouseId).not.toBeNull();
      expect(first.totalEntries ?? 0).toBeGreaterThan(0);
      await expect(page.getByTestId('nacha-upload-result')).toContainText(/Completado/i);
      await captureScreenshot(page, testInfo, 'cenit-live-result.png');

      const duplicate = await uploadThroughSpa(page, 'CENIT', cenitSourcePath);
      expect(duplicate.status).toBe(200);
      expect(duplicate.ingestionStatus).toBe('Duplicado');
      await captureScreenshot(page, testInfo, 'cenit-duplicate-result.png');

      await attachJson(testInfo, 'cenit-live-evidence.json', { sourceManifest, first, duplicate });
      assertCleanBrowser(activity);
    } finally {
      await stopTrace(context, testInfo, 'cenit-live-trace.zip');
    }
  });

  test('03 - original ACH Colombia envelope decrypts in the backend and is idempotent', async ({ page, context }, testInfo) => {
    test.setTimeout(300_000);
    const activity = monitorBrowser(page);
    await loginThroughRealForm(page);
    await context.tracing.start({ screenshots: true, snapshots: true, sources: true });

    try {
      const sourceManifest = buildSourceManifest('ACH Colombia', achColombiaSourcePath);
      const initial = await uploadThroughSpa(page, 'ACH Colombia', achColombiaSourcePath);
      let recovery: UploadEvidence | null = null;
      let completed = initial;
      const alreadyCompleted = initial.ingestionStatus === 'Duplicado'
        && /^Exitoso/.test(initial.parsingStatus ?? '')
        && (initial.totalEntries ?? 0) > 0;

      if (!alreadyCompleted
          && ['Duplicado', 'Fallido'].includes(initial.ingestionStatus ?? '')
          && ['EnProceso', 'FallidoReprocesable'].includes(initial.parsingStatus ?? '')) {
        expect(initial.ingestionId).not.toBeNull();
        await expect(page.getByTestId('nacha-reprocess-button')).toBeVisible();
        recovery = await reprocessThroughSpa(page);
        expect(recovery.ingestionId).not.toBe(initial.ingestionId);
        completed = recovery;
      }

      expect(completed.status).toBe(200);
      expect(completed.ingestionStatus).toBe(alreadyCompleted ? 'Duplicado' : 'Completado');
      expect(completed.parsingStatus).toMatch(/^Exitoso/);
      expect(completed.resolvedClearingHouseId).not.toBeNull();
      expect(completed.totalEntries ?? 0).toBeGreaterThan(0);
      await expect(page.getByTestId('nacha-upload-result')).toContainText(alreadyCompleted ? /Duplicado/i : /Completado/i);
      if (alreadyCompleted) {
        await expect(page.getByTestId('nacha-reprocess-button')).toHaveCount(0);
      }
      await captureScreenshot(page, testInfo, 'achcol-envelope-live-result.png');

      const duplicate = await uploadThroughSpa(page, 'ACH Colombia', achColombiaSourcePath);
      expect(duplicate.status).toBe(200);
      expect(duplicate.ingestionStatus).toBe('Duplicado');
      await captureScreenshot(page, testInfo, 'achcol-envelope-duplicate-result.png');

      await attachJson(testInfo, 'achcol-envelope-live-evidence.json', {
        sourceManifest,
        initial,
        recovery,
        completed,
        duplicate
      });
      assertCleanBrowser(activity);
    } finally {
      await stopTrace(context, testInfo, 'achcol-envelope-live-trace.zip');
    }
  });
});

async function loginThroughRealForm(page: Page): Promise<void> {
  await page.goto(resolveUiUrl('/login'));
  await expect(page.getByRole('heading', { name: /Ingreso al portal ACH Interbank/i })).toBeVisible({ timeout: 20_000 });
  await page.getByLabel('Usuario').fill(username);
  await page.locator('input[formcontrolname="password"]').fill(password);
  await page.getByRole('button', { name: 'Ingresar', exact: true }).click();
  await expect(page).not.toHaveURL(/\/login(?:\?|$)/, { timeout: 20_000 });
}

async function assertSoapCard(page: Page, method: string, endpoint: string, soapAction: string): Promise<void> {
  const card = page.getByTestId('soap-service-card').filter({ hasText: method });
  await expect(card).toHaveCount(1);
  await card.click();
  await expect(page.getByTestId('soap-endpoint-input')).toHaveValue(endpoint);
  await expect(page.getByTestId('soap-action-input')).toHaveValue(soapAction);
  await expect(page.getByTestId('soap-operating-mode')).toContainText(/LIVE/i);
  await expect(page.locator('mat-slide-toggle button[role="switch"]')).toHaveAttribute('aria-checked', 'true');
}

async function assertPublishedMappingVisible(page: Page, methodName: string): Promise<void> {
  const option = page.getByTestId('soap-service-option').filter({ hasText: methodName });
  await expect(option).toHaveCount(1);
  await option.click();
  const summary = page.getByTestId('mapping-functional-summary');
  await expect(summary).toBeVisible();
  await expect(summary.getByText('Bloqueantes', { exact: true }).locator('..').locator('strong')).toHaveText('0');
  await expect(page.getByTestId('mapping-matrix-row').first()).toBeVisible();
  await expect(page.locator('mat-card-title')).toContainText(methodName);
  await expect(page.locator('mat-chip-set mat-chip').first()).toContainText(/Publicado/i);
}

async function uploadThroughSpa(page: Page, clearingHouseText: string, filePath: string): Promise<UploadEvidence> {
  await page.goto(resolveUiUrl(uploadPath));
  await expect(page.getByTestId('nacha-upload-form')).toBeVisible({ timeout: 20_000 });

  const clearingHouseSelect = page.locator('app-clearing-house-select select');
  await expect(clearingHouseSelect).toBeEnabled({ timeout: 20_000 });
  const optionLabels = (await clearingHouseSelect.locator('option').allTextContents()).map((value) => value.trim());
  const label = optionLabels.find((value) => value.toLocaleLowerCase().includes(clearingHouseText.toLocaleLowerCase()));
  expect(label, `Clearing house option containing '${clearingHouseText}' must exist.`).toBeTruthy();
  await clearingHouseSelect.selectOption({ label: label as string });

  const fileInput = page.locator('input[type="file"]');
  await fileInput.setInputFiles(filePath);
  await expect(page.getByText(`Archivo seleccionado: ${path.basename(filePath)}`, { exact: false })).toBeVisible();

  const responsePromise = page.waitForResponse((response) =>
    uploadEndpoint.test(response.url()) && response.request().method() === 'POST');
  await page.getByRole('button', { name: 'Cargar archivo', exact: true }).click();
  const response = await responsePromise;
  const payload = await safePayload(response);
  await expect(page.getByTestId('nacha-upload-result')).toBeVisible({ timeout: 30_000 });
  await expect(
    page.getByTestId('nacha-upload-form').getByRole('button', { name: 'Cargar archivo', exact: true })
  ).toBeEnabled({ timeout: 30_000 });
  return sanitizeUploadEvidence(response.status(), payload);
}

async function reprocessThroughSpa(page: Page): Promise<UploadEvidence> {
  const button = page.getByTestId('nacha-reprocess-button');
  await expect(button).toBeVisible();
  const responsePromise = page.waitForResponse((response) =>
    uploadEndpoint.test(response.url()) && response.request().method() === 'POST',
  { timeout: 180_000 });
  await button.click();
  const response = await responsePromise;
  const payload = await safePayload(response);
  await expect(page.getByTestId('nacha-upload-result')).toBeVisible({ timeout: 60_000 });
  await expect(page.getByText('Reprocesando...', { exact: true })).toBeHidden({ timeout: 60_000 });
  return sanitizeUploadEvidence(response.status(), payload);
}

function sanitizeUploadEvidence(status: number, payload: unknown): UploadEvidence {
  const body = payload && typeof payload === 'object' ? payload as Record<string, unknown> : {};
  return {
    status,
    success: readBoolean(body, 'success'),
    partial: readBoolean(body, 'partial'),
    traceId: readString(body, 'traceId'),
    ingestionId: readString(body, 'ingestionId'),
    ingestionStatus: readString(body, 'ingestionStatus'),
    cycleResolutionStatus: readString(body, 'cycleResolutionStatus'),
    parsingStatus: readString(body, 'parsingStatus'),
    detectedClearingHouseId: readNumber(body, 'detectedClearingHouseId'),
    resolvedClearingHouseId: readNumber(body, 'resolvedClearingHouseId'),
    resolvedAchCycleId: readString(body, 'resolvedAchCycleId'),
    operationalDate: readString(body, 'operationalDate'),
    totalBatches: readNumber(body, 'totalBatches'),
    totalEntries: readNumber(body, 'totalEntries'),
    totalAddendas: readNumber(body, 'totalAddendas'),
    errors: readStrings(body, 'errors').map(redact)
  };
}

function buildSourceManifest(clearingHouse: string, filePath: string): Record<string, unknown> {
  const content = readFileSync(filePath);
  const stats = statSync(filePath);
  return {
    clearingHouse,
    sourcePath: filePath,
    fileName: path.basename(filePath),
    extension: path.extname(filePath),
    size: stats.size,
    lastWriteTimeUtc: stats.mtime.toISOString(),
    sha256: createHash('sha256').update(content).digest('hex').toUpperCase()
  };
}

function monitorBrowser(page: Page): BrowserActivity {
  const activity: BrowserActivity = { consoleErrors: [], pageErrors: [], networkErrors: [] };
  page.on('console', (message) => {
    if (message.type() === 'error' && !/favicon\.ico|ResizeObserver loop/i.test(message.text())) {
      activity.consoleErrors.push(redact(message.text()));
    }
  });
  page.on('pageerror', (error) => activity.pageErrors.push(redact(error.message)));
  page.on('requestfailed', (request) => {
    const failureText = request.failure()?.errorText ?? '';
    if (isExpectedNavigationCancellation(request.method(), request.url(), failureText)) {
      return;
    }

    const url = new URL(request.url());
    activity.networkErrors.push(redact(`${request.method()} ${url.origin}${url.pathname} ${failureText}`));
  });
  return activity;
}

function isExpectedNavigationCancellation(method: string, requestUrl: string, failureText: string): boolean {
  if (!/net::ERR_ABORTED/i.test(failureText)) {
    return false;
  }

  const pathname = new URL(requestUrl).pathname.toLowerCase();
  if (method.toUpperCase() === 'GET'
      && /^\/material-symbols-outlined\.[a-f0-9]+\.woff2$/.test(pathname)) {
    return true;
  }

  const expectedCancellations = new Set([
    'POST /api/navigation-logs',
    'POST /auth/refresh',
    'GET /api/navigation/menu',
    'GET /nachaupload/records'
  ]);
  return expectedCancellations.has(`${method.toUpperCase()} ${pathname}`);
}

function assertCleanBrowser(activity: BrowserActivity): void {
  expect(activity.consoleErrors).toEqual([]);
  expect(activity.pageErrors).toEqual([]);
  expect(activity.networkErrors).toEqual([]);
}

async function captureScreenshot(page: Page, testInfo: TestInfo, name: string): Promise<void> {
  const screenshotPath = testInfo.outputPath(name);
  await page.screenshot({ path: screenshotPath, fullPage: true });
  await testInfo.attach(name, { path: screenshotPath, contentType: 'image/png' });
}

async function stopTrace(
  context: BrowserContext,
  testInfo: TestInfo,
  name: string
): Promise<void> {
  const tracePath = testInfo.outputPath(name);
  await context.tracing.stop({ path: tracePath });
  await testInfo.attach(name, { path: tracePath, contentType: 'application/zip' });
}

async function attachJson(testInfo: TestInfo, name: string, value: unknown): Promise<void> {
  await testInfo.attach(name, {
    body: JSON.stringify(value, null, 2),
    contentType: 'application/json'
  });
}

async function safePayload(response: { json(): Promise<unknown>; text(): Promise<string> }): Promise<unknown> {
  try {
    return await response.json();
  } catch {
    return { errors: [redact(await response.text())] };
  }
}

function readRaw(body: Record<string, unknown>, key: string): unknown {
  return body[key] ?? body[key[0].toUpperCase() + key.slice(1)];
}

function readString(body: Record<string, unknown>, key: string): string | null {
  const value = readRaw(body, key);
  return typeof value === 'string' ? value : null;
}

function readBoolean(body: Record<string, unknown>, key: string): boolean | null {
  const value = readRaw(body, key);
  return typeof value === 'boolean' ? value : null;
}

function readNumber(body: Record<string, unknown>, key: string): number | null {
  const value = readRaw(body, key);
  return typeof value === 'number' ? value : null;
}

function readStrings(body: Record<string, unknown>, key: string): string[] {
  const value = readRaw(body, key);
  return Array.isArray(value) ? value.filter((item): item is string => typeof item === 'string') : [];
}

function resolveUiUrl(relativePath: string): string {
  return new URL(relativePath, uiBaseUrl).toString();
}

function assertLocalEndpoint(value: string, variableName: string): void {
  const host = new URL(value).hostname.toLocaleLowerCase();
  expect(['localhost', '127.0.0.1', 'host.docker.internal'], `${variableName} must be local.`).toContain(host);
}

function stringEquals(value: string | undefined, expected: string): boolean {
  return (value ?? '').trim().toLocaleLowerCase() === expected.toLocaleLowerCase();
}

function redact(value: string): string {
  return value
    .replace(/bearer\s+[a-z0-9._~-]+/gi, 'Bearer [REDACTED]')
    .replace(/eyJ[a-z0-9._~-]+/gi, '[REDACTED_TOKEN]')
    .slice(0, 500);
}
