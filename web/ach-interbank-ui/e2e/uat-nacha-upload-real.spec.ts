import { expect, Page, test, TestInfo } from '@playwright/test';
import { readFileSync } from 'node:fs';
import path from 'node:path';

type RuntimeContext = {
  apiBaseUrl: string;
  uiBaseUrl: string;
  authToken: string;
  username: string;
};

const uploadPath = '/transactions/nacha-upload';
const loginPath = '/auth/login';
const uploadEndpoint = /\/NachaUpload\/upload(?:\?.*)?$/;
const soapRealEndpoint = /\/soap(?:\/|$)/i;
const moneyMovementEndpoint = /\/(?:proc-transacciones|proc-contrapartidas|movimientos|movement|payments)(?:\/|\?|$)/i;
const legacyNachaEndpoint = /\/(?:nacha-layouts|nacha-record-definitions)(?:\/|\?|$)/;

const operationalFixtureFileName = '0001283.001.1';
const operationalFixturePath = path.resolve(__dirname, '../../../tests/Cfa.ACHInterbank.Tests/TestData/Nacha/GoldenFiles/ACHColombia/Incoming/ACH_COL_IN_001.ach');

test.describe('NACHA upload UAT real', () => {
  test('ShouldUploadInternalAchFixtureAgainstRealUiAndApi', async ({ page }, testInfo) => {
    test.skip(!shouldRunUat(), 'RUN_UAT_NACHA_UPLOAD=true is required for the real UAT spec.');

    const runtime = await resolveRuntime();
    test.skip(runtime === null, 'Real UAT requires the API to be reachable.');
    const resolvedRuntime = runtime as RuntimeContext;
    const activity = createActivityRecorder(page);

    await seedSession(page, resolvedRuntime.authToken);
    await page.goto(resolvedRuntime.uiBaseUrl + uploadPath);

    await expect(page.getByRole('button', { name: 'Cargar archivo' })).toBeVisible({ timeout: 15_000 });

    await page.locator('input[type="file"]').setInputFiles({
      name: operationalFixtureFileName,
      mimeType: 'application/octet-stream',
      buffer: readFileSync(operationalFixturePath)
    });

    await expect(page.getByText('Archivo seleccionado: 0001283.001.1', { exact: false })).toBeVisible();

    const uploadResponsePromise = page.waitForResponse((response) =>
      uploadEndpoint.test(response.url()) && response.request().method() === 'POST');

    await page.getByRole('button', { name: 'Cargar archivo' }).click();

    const uploadResponse = await uploadResponsePromise;

    const screenshotPath = testInfo.outputPath('nacha-upload-real-uat.png');
    await page.screenshot({ path: screenshotPath, fullPage: true });
    await testInfo.attach('nacha-upload-real-uat.png', { path: screenshotPath, contentType: 'image/png' });
    await testInfo.attach('nacha-upload-real-response.json', {
      body: JSON.stringify({
        runtime: {
          apiBaseUrl: resolvedRuntime.apiBaseUrl,
          uiBaseUrl: resolvedRuntime.uiBaseUrl,
          username: resolvedRuntime.username
        },
        upload: {
          status: uploadResponse.status(),
          ok: uploadResponse.ok()
        }
      }, null, 2),
      contentType: 'application/json'
    });

    expect(activity.legacyRequests).toEqual([]);
    expect(activity.soapRequests).toEqual([]);
    expect(activity.moneyRequests).toEqual([]);
    expect(activity.criticalRequestFailures).toEqual([]);
  });
});

function shouldRunUat(): boolean {
  return stringEquals(process.env['RUN_UAT_NACHA_UPLOAD'], 'true');
}

async function resolveRuntime(): Promise<RuntimeContext | null> {
  const apiBaseUrl = process.env['ACH_API_URL'] ?? 'http://localhost:843';
  const uiBaseUrl = process.env['ACH_UI_URL'] ?? 'http://localhost:743';
  const username = process.env['ACH_USER'] ?? 'admin';
  const password = process.env['ACH_PASS'] ?? 'Admin123!';

  if (!(await isApiAvailable(apiBaseUrl))) {
    return null;
  }

  const response = await fetch(resolveUrl(apiBaseUrl, loginPath), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username, password })
  });

  expect(response.ok, 'Debe poder autenticarse contra el API real para ejecutar el UAT de NACHA Upload.').toBeTruthy();

  const payload = await response.json() as { data?: { token?: string } };
  const authToken = payload.data?.token;
  expect(authToken, 'El login real debe devolver un token de acceso.').toBeTruthy();

  return {
    apiBaseUrl,
    uiBaseUrl,
    authToken: authToken as string,
    username
  };
}

async function seedSession(page: Page, accessToken: string): Promise<void> {
  await page.addInitScript((token) => {
    window.sessionStorage.setItem('ach.interbank.access_token', token);
  }, accessToken);
}

function createActivityRecorder(page: Page) {
  const consoleErrors: string[] = [];
  const criticalRequestFailures: string[] = [];
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

  return { consoleErrors, criticalRequestFailures, legacyRequests, soapRequests, moneyRequests };
}

async function isApiAvailable(apiBaseUrl: string): Promise<boolean> {
  try {
    const response = await fetch(resolveUrl(apiBaseUrl, '/health/live'));
    return response.ok;
  } catch {
    return false;
  }
}

function resolveUrl(base: string, relativePath: string): string {
  return new URL(relativePath, base).toString();
}

function stringEquals(value: string | undefined, expected: string): boolean {
  return (value ?? '').trim().toLowerCase() === expected.trim().toLowerCase();
}

function isBenignConsoleError(text: string): boolean {
  return /ChunkLoadError|Non-Error exception captured|Failed to load resource: the server responded with a status of 401/i.test(text);
}

function isAuxiliaryLayoutRequest(url: string): boolean {
  return /\/(navigation\/menu|api\/users\/branding|api\/navigation-logs|auth\/refresh)(?:\?|$)/.test(url);
}

function isCriticalAssetOrUatRequest(url: string): boolean {
  return /\/(transactions\/nacha-upload|NachaUpload\/upload|NachaUpload\/records|api\/)/i.test(url);
}
