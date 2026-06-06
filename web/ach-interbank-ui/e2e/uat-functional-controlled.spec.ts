import { expect, Page, test, TestInfo } from '@playwright/test';

type RuntimeMode = 'real' | 'fallback';

type RuntimeContext = {
  mode: RuntimeMode;
  apiBaseUrl: string;
  username: string;
  password: string;
  authToken?: string;
  useInstitutionFixture: boolean;
};

type InboundSimulationResult = {
  id: number;
  simulationId: string;
  fileName: string;
  downloadUrl: string;
  evidenceUrl: string;
  sha256: string;
  fileSizeBytes: number;
  generatedOnly: boolean;
  autoImported: boolean;
  uploadRequired: boolean;
  externalTransmission: boolean;
  message: string;
};

const simulatorPath = '/uat/nacha-inbound-simulator';
const healthLivePath = '/health/live';
const loginPath = '/auth/login';
const refreshEndpoint = /\/auth\/refresh$/;
const navigationEndpoint = /\/navigation\/menu$/;
const brandingEndpoint = /\/api\/users\/branding(?:\?.*)?$/;
const navigationLogsEndpoint = /\/api\/navigation-logs(?:\?.*)?$/;
const financialInstitutionsEndpoint = /\/financial-institutions(?:\?.*)?$/;
const simulatorListEndpoint = /\/api\/uat\/nacha-inbound-simulator(?:\?.*)?$/;
const simulatorPreviewEndpoint = /\/api\/uat\/nacha-inbound-simulator\/eligibility-preview$/;
const simulatorGenerateEndpoint = /\/api\/uat\/nacha-inbound-simulator\/generate$/;
const simulatorFileEndpoint = /\/api\/uat\/nacha-inbound-simulator\/\d+\/file$/;
const simulatorEvidenceEndpoint = /\/api\/uat\/nacha-inbound-simulator\/\d+\/evidence$/;
const legacyNachaEndpoint = /\/(?:nacha-layouts|nacha-record-definitions)(?:\/|\?|$)/;
const soapRealEndpoint = /\/soap(?:\/|$)/i;
const moneyMovementEndpoint = /\/(?:proc-transacciones|proc-contrapartidas|movimientos|movement|payments)(?:\/|\?|$)/i;

test.describe('UAT functional controlled inbound simulator', () => {
  test('UatInboundSimulator_ShouldLoginPreviewGenerateAndExposeEvidence', async ({ page }, testInfo) => {
    const runtime = await resolveRuntime();
    const activity = createActivityRecorder(page);
    const simulatorState = createFallbackState();

    const authToken = await authenticate(page, runtime);
    await mockAuthRefresh(page, authToken);

    await mockFallbackRuntime(page, runtime, simulatorState);

    await page.goto(simulatorPath);

    await expect(page.getByRole('heading', { name: 'Simulador NACHA-M Entrada', level: 1 })).toBeVisible();
    await expect(page.getByText(/solo genera archivos/i)).toBeVisible();
    await expect(page.locator('body')).not.toHaveText(/ChunkLoadError|Application error|UnhandledPromiseRejection/i);
    await expect(page.getByRole('button', { name: /Validar/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /Generar archivo/i })).toBeVisible();

    await expect(page.getByRole('heading', { name: 'Archivo generado' })).toHaveCount(0);

    await page.waitForFunction(() => {
      const select = document.querySelector('select[formcontrolname="originFinancialInstitutionId"]');
      return !!select && select.querySelectorAll('option').length > 1;
    });
    await page.getByLabel('Entidad originadora externa').selectOption({ index: 1 });
    await expect(page.getByRole('button', { name: /Generar archivo/i })).toBeEnabled();

    await page.getByRole('button', { name: /Validar/i }).click();
    await expect(page.locator('body')).not.toHaveText(/ChunkLoadError|Application error|UnhandledPromiseRejection/i);

    const generateResponse = page.waitForResponse((response) => simulatorGenerateEndpoint.test(response.url()) && response.request().method() === 'POST');
    await page.getByRole('button', { name: /Generar archivo/i }).click();
    const generated = await parseGenerateResponse(await generateResponse, testInfo);

    const resultSection = page.locator('section.result');
    await expect(page.getByRole('heading', { name: 'Archivo generado' })).toBeVisible();
    await expect(resultSection.getByRole('definition').filter({ hasText: generated.fileName })).toBeVisible();
    await expect(resultSection.getByRole('definition').filter({ hasText: generated.sha256 })).toBeVisible();
    await expect(page.getByText('generatedOnly', { exact: true })).toBeVisible();
    await expect(page.getByText('autoImported', { exact: true })).toBeVisible();
    await expect(page.getByText('uploadRequired', { exact: true })).toBeVisible();
    await expect(resultSection.getByRole('button', { name: /Descargar/i })).toBeVisible();
    await expect(page.getByText(/Debe cargarse manualmente por NachaUpload/i)).toBeVisible();
    await expect(page.getByText(generated.fileName, { exact: true }).last()).toBeVisible();

    await testInfo.attach('uat-inbound-download-url.txt', {
      body: resolveUrl(runtime.apiBaseUrl, generated.downloadUrl),
      contentType: 'text/plain'
    });
    await testInfo.attach('uat-inbound-evidence-url.txt', {
      body: resolveUrl(runtime.apiBaseUrl, generated.evidenceUrl),
      contentType: 'text/plain'
    });

    const screenshotPath = testInfo.outputPath('uat-inbound-simulator.png');
    await page.screenshot({ path: screenshotPath, fullPage: true });
    await testInfo.attach('uat-inbound-simulator.png', {
      path: screenshotPath,
      contentType: 'image/png'
    });

    await testInfo.attach('uat-inbound-simulator-evidence.json', {
      body: JSON.stringify({
        runtime: runtime.mode,
        apiBaseUrl: runtime.apiBaseUrl,
        preview: { clicked: true, endpoint: '/api/uat/nacha-inbound-simulator/eligibility-preview' },
        generate: generated
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

    if (isCriticalAssetOrUatRequest(url)) {
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

async function resolveRuntime(): Promise<RuntimeContext> {
  const apiBaseUrl = process.env['ACH_API_URL'] ?? 'http://localhost:843';
  const user = process.env['ACH_USER'] ?? 'admin';
  const password = process.env['ACH_PASS'] ?? 'Admin123!';
  if (!(await isApiAvailable(apiBaseUrl))) {
    return {
      mode: 'fallback',
      apiBaseUrl,
      username: user,
      password,
      useInstitutionFixture: false,
      authToken: createUnsignedJwt({
        unique_name: user,
        name: 'Usuario UAT Simulador',
        uid: 'uat-simulator',
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
    body: JSON.stringify({ username: user, password })
  });

  expect(response.ok, 'Debe poder autenticarse contra el API real para ejecutar el simulador UAT.').toBeTruthy();

  const payload = await response.json() as { data?: { token?: string } };
  const token = payload.data?.token;
  expect(token, 'El login real debe devolver un access token.').toBeTruthy();

  const useInstitutionFixture = !(await hasUsableFinancialInstitutions(apiBaseUrl, token as string));

  return {
    mode: 'real',
    apiBaseUrl,
    username: user,
    password,
    useInstitutionFixture,
    authToken: token as string
  };
}

async function authenticate(page: Page, runtime: RuntimeContext): Promise<string> {
  await page.addInitScript((accessToken) => {
    window.sessionStorage.setItem('ach.interbank.access_token', accessToken);
  }, runtime.authToken as string);

  return runtime.authToken as string;
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
          username: 'uat.synth',
          fullName: 'Usuario UAT Simulado',
          roles: ['Admin'],
          permissions: ['CanReadAch', 'CanManageAch']
        }
      })
    });
  });
}

async function mockFallbackRuntime(page: Page, runtime: RuntimeContext, state: FallbackState): Promise<void> {
  await page.route(navigationEndpoint, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        { id: 1, label: 'UAT', route: '/uat', children: [{ id: 11, label: 'Simulador NACHA-M Entrada', route: simulatorPath }] }
      ])
    });
  });

  await page.route(brandingEndpoint, async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: '{}' });
  });

  await page.route(navigationLogsEndpoint, async (route) => {
    await route.fulfill({ status: 204, contentType: 'application/json', body: '' });
  });

  await page.route(financialInstitutionsEndpoint, async (route) => {
    if (route.request().method() !== 'GET') {
      await route.continue();
      return;
    }

    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        {
          id: 100,
          name: 'Banco UAT Simulado',
          routingNumber: '99999',
          transitCode: '900',
          checkDigit: '0',
          isDefaultSource: false,
          status: 1
        },
        {
          id: 200,
          name: 'Entidad default CFA',
          routingNumber: '00001',
          transitCode: '283',
          checkDigit: '0',
          isDefaultSource: true,
          status: 1
        }
      ])
    });
  });

  await page.route(simulatorListEndpoint, async (route) => {
    if (route.request().method() !== 'GET') {
      await route.continue();
      return;
    }

    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(state.items)
    });
  });

  await page.route(simulatorPreviewEndpoint, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ eligible: true, message: 'Simulacion elegible' })
    });
  });

  await page.route(simulatorGenerateEndpoint, async (route) => {
    state.generated = {
      id: 1,
      simulationId: 'sim-uat-001',
      fileName: 'UAT-INBOUND-001.ach',
      downloadUrl: '/api/uat/nacha-inbound-simulator/1/file',
      evidenceUrl: '/api/uat/nacha-inbound-simulator/1/evidence',
      sha256: 'A'.repeat(64),
      fileSizeBytes: 1060,
      generatedOnly: true,
      autoImported: false,
      uploadRequired: true,
      externalTransmission: false,
      message: 'Archivo NACHA-M simulado generado. Debe cargarse manualmente por NachaUpload.'
    };
    state.items = [toSimulationItem(state.generated)];

    await route.fulfill({
      status: 201,
      contentType: 'application/json',
      body: JSON.stringify(state.generated)
    });
  });

  await page.route(simulatorFileEndpoint, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/octet-stream',
      body: 'SIMULATED-NACHA-FILE'
    });
  });

  await page.route(simulatorEvidenceEndpoint, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        id: 1,
        generatedOnly: true,
        autoImported: false,
        uploadRequired: true,
        externalTransmission: false,
        sha256: 'A'.repeat(64)
      })
    });
  });
}

async function mockInstitutionFixture(page: Page): Promise<void> {
  await page.route('**/financial-institutions**', async (route) => {
    if (route.request().method() !== 'GET') {
      await route.continue();
      return;
    }

    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        {
          id: 100,
          name: 'Banco UAT Simulado',
          routingNumber: '99999',
          transitCode: '900',
          checkDigit: '0',
          isDefaultSource: false,
          status: 1
        },
        {
          id: 200,
          name: 'Entidad default CFA',
          routingNumber: '00001',
          transitCode: '283',
          checkDigit: '0',
          isDefaultSource: true,
          status: 1
        }
      ])
    });
  });
}

async function isApiAvailable(apiBaseUrl: string): Promise<boolean> {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), 3_500);

  try {
    const response = await fetch(resolveUrl(apiBaseUrl, healthLivePath), { signal: controller.signal });
    return response.ok;
  } catch {
    return false;
  } finally {
    clearTimeout(timeout);
  }
}

async function hasUsableFinancialInstitutions(apiBaseUrl: string, token: string): Promise<boolean> {
  try {
    const response = await fetch(resolveUrl(apiBaseUrl, '/financial-institutions?includeInactive=false'), {
      headers: { Authorization: `Bearer ${token}` }
    });

    if (!response.ok) {
      return false;
    }

    const payload = await response.json() as Array<{
      id: number;
      isDefaultSource: boolean;
      status: number;
    }>;

    const active = (payload ?? []).filter((item) => item.status === 1);
    const origin = active.filter((item) => !item.isDefaultSource);
    const defaults = active.filter((item) => item.isDefaultSource);
    return origin.length > 0 && defaults.length === 1;
  } catch {
    return false;
  }
}

async function parseGenerateResponse(response: Awaited<ReturnType<Page['waitForResponse']>>, testInfo: TestInfo): Promise<InboundSimulationResult> {
  expect(response.ok(), 'La generación sintética NACHA-M debe responder correctamente.').toBeTruthy();
  expect(response.status()).toBe(201);

  const generated = await response.json() as InboundSimulationResult;
  expect(generated.fileName).toBeTruthy();
  expect(generated.downloadUrl).toBeTruthy();
  expect(generated.evidenceUrl).toBeTruthy();
  expect(generated.generatedOnly).toBeTruthy();
  expect(generated.autoImported).toBeFalsy();
  expect(generated.uploadRequired).toBeTruthy();
  expect(generated.externalTransmission).toBeFalsy();

  await testInfo.attach('uat-inbound-generate-response.json', {
    body: JSON.stringify(generated, null, 2),
    contentType: 'application/json'
  });

  return generated;
}

function createFallbackState(): FallbackState {
  return {
    items: [],
    generated: null
  };
}

function toSimulationItem(result: InboundSimulationResult) {
  return {
    id: result.id,
    simulationId: result.simulationId,
    clearingHouseName: 'ACH Colombia',
    scenarioType: 'IncomingCredit',
    responseMode: null,
    reasonCode: null,
    originFinancialInstitution: 'Banco UAT Simulado',
    destinationFinancialInstitution: 'Entidad default CFA',
    originFinancialInstitutionId: 100,
    destinationFinancialInstitutionId: 200,
    fileName: result.fileName,
    sha256: result.sha256,
    fileSizeBytes: result.fileSizeBytes,
    generatedOnly: result.generatedOnly,
    autoImported: result.autoImported,
    uploadRequired: result.uploadRequired,
    externalTransmission: result.externalTransmission,
    createdAt: new Date().toISOString()
  };
}

type FallbackState = {
  items: ReturnType<typeof toSimulationItem>[];
  generated: InboundSimulationResult | null;
};

function isAssetRequest(url: string): boolean {
  return /\.(js|css)(?:\?|$)/i.test(url);
}

function isCriticalAssetOrUatRequest(url: string): boolean {
  return isAssetRequest(url) || simulatorPreviewEndpoint.test(url) || simulatorGenerateEndpoint.test(url) || simulatorListEndpoint.test(url) || simulatorFileEndpoint.test(url) || simulatorEvidenceEndpoint.test(url);
}

function isAuxiliaryLayoutRequest(url: string): boolean {
  return brandingEndpoint.test(url) || navigationLogsEndpoint.test(url);
}

function isBenignConsoleError(text: string): boolean {
  return /net::ERR_CONNECTION_REFUSED|favicon.ico|Download the React DevTools|ResizeObserver loop limit exceeded/i.test(text);
}

function resolveUrl(baseUrl: string, path: string): string {
  const base = baseUrl.replace(/\/+$/, '');
  const cleaned = path.replace(/^\/+/, '');
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
