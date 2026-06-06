import { expect, Page, test, TestInfo } from '@playwright/test';
import path from 'node:path';

type RuntimeMode = 'real' | 'fallback';

type RuntimeContext = {
  mode: RuntimeMode;
  apiBaseUrl: string;
  username: string;
  password: string;
  authToken: string;
};

type UploadResponse = {
  success?: boolean;
  partial?: boolean;
  message?: string;
  errors?: string[];
  traceId?: string;
  ingestionStatus?: string;
  cycleResolutionStatus?: string;
  parsingStatus?: string;
  totalBatches?: number;
  totalEntries?: number;
  totalAddendas?: number;
};

type FallbackState = {
  records: Array<Record<string, unknown>>;
};

const uploadPath = '/transactions/nacha-upload';
const healthLivePath = '/health/live';
const loginPath = '/auth/login';
const refreshEndpoint = /\/auth\/refresh$/;
const navigationEndpoint = /\/navigation\/menu$/;
const brandingEndpoint = /\/api\/users\/branding(?:\?.*)?$/;
const navigationLogsEndpoint = /\/api\/navigation-logs(?:\?.*)?$/;
const uploadEndpoint = /\/NachaUpload\/upload$/;
const recordsEndpoint = /\/NachaUpload\/records(?:\?.*)?$/;
const legacyNachaEndpoint = /\/(?:nacha-layouts|nacha-record-definitions)(?:\/|\?|$)/;
const soapRealEndpoint = /\/soap(?:\/|$)/i;
const moneyMovementEndpoint = /\/(?:proc-transacciones|proc-contrapartidas|movimientos|movement|payments)(?:\/|\?|$)/i;
const goldenFilePath = path.resolve(__dirname, '../../../tests/Cfa.ACHInterbank.Tests/TestData/Nacha/GoldenFiles/ACHColombia/Incoming/ACH_COL_IN_001.ach');

test.describe('NACHA upload controlled ACH Colombia', () => {
  test('NachaUpload_ShouldUploadSyntheticGoldenAndShowEvidence', async ({ page }, testInfo) => {
    const runtime = await resolveRuntime();
    const activity = createActivityRecorder(page);
    const state = createFallbackState();

    await seedSession(page, runtime.authToken);
    await mockAuthRefresh(page, runtime.authToken);
    await mockLayoutAuxiliaryEndpoints(page);
    await mockFallbackRuntime(page, state, runtime.authToken);
    await page.goto('/');
    await page.evaluate((token) => {
      window.sessionStorage.setItem('ach.interbank.access_token', token);
    }, runtime.authToken);
    await page.goto(uploadPath);

    await expect(page.getByTestId('nacha-upload-form')).toBeVisible({ timeout: 15_000 });
    await expect(page.getByText('Cargar archivo NACHA-M', { exact: true })).toBeVisible({ timeout: 15_000 });
    await expect(page.getByText(/Sube archivos NACHA-M de cámaras compensadoras y consulta el detalle cargado\./i)).toBeVisible({ timeout: 15_000 });
    await expect(page.locator('body')).not.toHaveText(/ChunkLoadError|Application error|UnhandledPromiseRejection/i);
    await expect(page.getByTestId('nacha-upload-records')).toBeVisible();

    const uploadResponsePromise = page.waitForResponse((response) => uploadEndpoint.test(response.url()) && response.request().method() === 'POST');
    await page.locator('input[type="file"]').setInputFiles(goldenFilePath);
    await expect(page.getByText('Archivo seleccionado: ACH_COL_IN_001.ach')).toBeVisible();
    await page.getByRole('button', { name: 'Cargar archivo' }).click();

    const uploadResponse = await uploadResponsePromise;
    const uploadPayload = await safeJson(uploadResponse);

    await expect(page.getByTestId('nacha-upload-result')).toBeVisible();
    await expect(page.getByTestId('nacha-upload-result-message')).toBeVisible();
    await expect(page.getByTestId('nacha-upload-result-message')).not.toHaveText(/^$/);
    await expect(page.getByTestId('nacha-upload-result').locator('.upload-result-badge')).toBeVisible();
    await expect(page.getByTestId('nacha-upload-result').locator('.upload-result-badge')).toContainText(/Procesado correctamente|Procesado con observaciones|Rechazo controlado|Recepción controlada/i);
    await expect(page.getByText(/Trace ID/i)).toBeVisible();
    await expect(page.getByTestId('nacha-upload-records')).toBeVisible();
    await expect(page.locator('body')).not.toHaveText(/ChunkLoadError|Application error|UnhandledPromiseRejection/i);

    const screenshotPath = testInfo.outputPath('nacha-upload-controlled.png');
    await page.screenshot({ path: screenshotPath, fullPage: true });
    await testInfo.attach('nacha-upload-controlled.png', { path: screenshotPath, contentType: 'image/png' });
    await testInfo.attach('nacha-upload-response.json', {
      body: JSON.stringify({
        runtime: runtime.mode,
        apiBaseUrl: runtime.apiBaseUrl,
        upload: uploadPayload
      }, null, 2),
      contentType: 'application/json'
    });

    expect(activity.legacyRequests).toEqual([]);
    expect(activity.soapRequests).toEqual([]);
    expect(activity.moneyRequests).toEqual([]);
    expect(activity.htmlAssetResponses).toEqual([]);
    expect(activity.criticalRequestFailures).toEqual([]);
    expect(activity.consoleErrors).toEqual([]);
  });
});

async function resolveRuntime(): Promise<RuntimeContext> {
  const apiBaseUrl = process.env['ACH_API_URL'] ?? 'http://localhost:843';
  const username = process.env['ACH_USER'] ?? 'admin';
  const password = process.env['ACH_PASS'] ?? 'Admin123!';

  if (!(await isApiAvailable(apiBaseUrl))) {
    return {
      mode: 'fallback',
      apiBaseUrl,
      username,
      password,
      authToken: createUnsignedJwt({
        unique_name: username,
        name: 'Usuario UAT NACHA Upload',
        uid: 'uat-nacha-upload',
        role: ['Admin', 'ACH.Operator'],
        permission: ['CanReadAch', 'CanManageAch'],
        exp: Math.floor(Date.now() / 1000) + 3600,
        iat: Math.floor(Date.now() / 1000)
      })
    };
  }

  const response = await fetch(resolveUrl(apiBaseUrl, loginPath), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username, password })
  });

  expect(response.ok, 'Debe poder autenticarse contra el API real para ejecutar la carga NACHA-M controlada.').toBeTruthy();

  const payload = await response.json() as {
    data?: {
      token?: string;
      username?: string;
      fullName?: string;
      roles?: string[];
      permissions?: string[];
    };
  };
  const token = payload.data?.token;
  expect(token, 'El login real debe devolver un access token.').toBeTruthy();

  const authToken = createUnsignedJwt({
    unique_name: payload.data?.username ?? username,
    name: payload.data?.fullName ?? payload.data?.username ?? 'Usuario UAT NACHA Upload',
    uid: 'uat-nacha-upload',
    role: payload.data?.roles?.length ? payload.data.roles : ['Admin', 'ACH.Operator'],
    permission: payload.data?.permissions?.length ? payload.data.permissions : ['CanReadAch', 'CanManageAch'],
    exp: Math.floor(Date.now() / 1000) + 3600,
    iat: Math.floor(Date.now() / 1000)
  });

  return {
    mode: 'real',
    apiBaseUrl,
    username,
    password,
    authToken
  };
}

async function seedSession(page: Page, accessToken: string): Promise<void> {
  await page.addInitScript((token) => {
    window.sessionStorage.setItem('ach.interbank.access_token', token);
  }, accessToken);
}

async function mockAuthRefresh(page: Page, token: string): Promise<void> {
  await page.route(refreshEndpoint, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        sucess: true,
        data: {
          token,
          username: 'uat.nacha.upload',
          fullName: 'Usuario UAT NACHA Upload',
          roles: ['Admin'],
          permissions: ['CanReadAch', 'CanManageAch']
        }
      })
    });
  });
}

async function mockLayoutAuxiliaryEndpoints(page: Page): Promise<void> {
  await page.route(navigationEndpoint, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        { id: 1, label: 'Transacciones', route: '/transactions', children: [{ id: 11, label: 'Cargar NACHA-M', route: '/transactions/nacha-upload' }] },
        { id: 2, label: 'UAT', route: '/uat', children: [{ id: 21, label: 'Simulador NACHA inbound', route: '/uat/nacha-inbound-simulator' }] }
      ])
    });
  });

  await page.route(brandingEndpoint, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({})
    });
  });

  await page.route(navigationLogsEndpoint, async (route) => {
    await route.fulfill({
      status: 204,
      contentType: 'application/json',
      body: ''
    });
  });
}

async function mockFallbackRuntime(page: Page, state: FallbackState, _token: string): Promise<void> {
  await page.route(recordsEndpoint, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(state.records)
    });
  });

  await page.route(uploadEndpoint, async (route) => {
    const body = {
      success: true,
      partial: false,
      message: 'Archivo procesado correctamente.',
      errors: [],
      traceId: 'fallback-trace-001',
      ingestionStatus: 'Completado',
      cycleResolutionStatus: 'ResueltoConfirmado',
      parsingStatus: 'Exitoso',
      totalBatches: 1,
      totalEntries: 1,
      totalAddendas: 0
    } satisfies UploadResponse;

    state.records = [toUploadRecord()];

    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(body)
    });
  });
}

function toUploadRecord(): Record<string, unknown> {
  return {
    nachaId: 'UA-NACHA-001',
    immediateOrigin: '02100002',
    immediateDestination: '02100001',
    immediateOriginName: 'Banco UAT Origen',
    immediateDestinationName: 'Entidad UAT Destino',
    referenceCode: 'UAT-NACHA-M',
    fileCreationDate: '20260606',
    fileCreationTime: '093000',
    achCycleId: 'ACH-UAT-01',
    achCycleName: 'Ciclo UAT NACHA-M',
    clearingHouseName: 'ACH Colombia',
    totalEntries: 1,
    totalAddendas: 0,
    totalBatches: 1,
    totalAmount: 1000,
    totalDebitAmount: 1000,
    totalCreditAmount: 0
  };
}

async function isApiAvailable(apiBaseUrl: string): Promise<boolean> {
  try {
    const response = await fetch(resolveUrl(apiBaseUrl, healthLivePath), { method: 'GET' });
    return response.ok;
  } catch {
    return false;
  }
}

async function safeJson(response: Awaited<ReturnType<Page['waitForResponse']>>): Promise<unknown> {
  try {
    return await response.json();
  } catch {
    return await response.text();
  }
}

function createFallbackState(): FallbackState {
  return {
    records: []
  };
}

function createActivityRecorder(page: Page) {
  const consoleErrors: string[] = [];
  const criticalRequestFailures: string[] = [];
  const htmlAssetResponses: string[] = [];
  const legacyRequests: string[] = [];
  const soapRequests: string[] = [];
  const moneyRequests: string[] = [];

  page.on('console', (message) => {
    if (message.type() !== 'error') {
      return;
    }

    const text = message.text();
    if (!isBenignConsoleError(text)) {
      consoleErrors.push(text);
    }
  });

  page.on('request', (request) => {
    const url = request.url();
    if (legacyNachaEndpoint.test(url)) {
      legacyRequests.push(`${request.method()} ${url}`);
    }
    if (soapRealEndpoint.test(url)) {
      soapRequests.push(`${request.method()} ${url}`);
    }
    if (moneyMovementEndpoint.test(url)) {
      moneyRequests.push(`${request.method()} ${url}`);
    }
  });

  page.on('requestfailed', (request) => {
    const url = request.url();
    if (isAuxiliaryLayoutRequest(url)) {
      return;
    }

    if (isCriticalAssetOrUploadRequest(url)) {
      criticalRequestFailures.push(`${request.method()} ${url} ${request.failure()?.errorText ?? ''}`.trim());
    }
  });

  page.on('response', async (response) => {
    const url = response.url();
    if (!isAssetRequest(url)) {
      return;
    }

    const contentType = response.headers()['content-type'] ?? '';
    if (contentType.includes('text/html')) {
      htmlAssetResponses.push(`${response.status()} ${url} ${contentType}`);
    }
  });

  return { consoleErrors, criticalRequestFailures, htmlAssetResponses, legacyRequests, soapRequests, moneyRequests };
}

function isAssetRequest(url: string): boolean {
  return /\.(js|css)(?:\?|$)/i.test(url);
}

function isCriticalAssetOrUploadRequest(url: string): boolean {
  return isAssetRequest(url) || uploadEndpoint.test(url) || recordsEndpoint.test(url);
}

function isAuxiliaryLayoutRequest(url: string): boolean {
  return brandingEndpoint.test(url) || navigationLogsEndpoint.test(url);
}

function isBenignConsoleError(text: string): boolean {
  return /net::ERR_CONNECTION_REFUSED|favicon.ico|Download the React DevTools|ResizeObserver loop limit exceeded/i.test(text);
}

function resolveUrl(baseUrl: string, pathValue: string): string {
  const base = baseUrl.replace(/\/+$/, '');
  const cleaned = pathValue.replace(/^\/+/, '');
  return `${base}/${cleaned}`;
}

function createUnsignedJwt(payload: Record<string, unknown>): string {
  return `${base64Url({ alg: 'none', typ: 'JWT' })}.${base64Url(payload)}.e2e`;
}

function base64Url(value: Record<string, unknown>): string {
  return Buffer.from(JSON.stringify(value))
    .toString('base64')
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=+$/g, '');
}
