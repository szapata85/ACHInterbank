import { expect, Page, test } from '@playwright/test';
import { mkdirSync } from 'node:fs';
import { resolve } from 'node:path';

type AuthLoginResponse = {
  data?: { token?: string };
};

type SoapMethod = {
  methodName: string;
  endpoint: string;
  soapAction: string;
  operatingMode: string;
  enabled: boolean;
  inputParameterMappings: Array<{
    inputName: string;
    soapParameterName: string;
    required: boolean;
  }>;
};

type SoapSettings = {
  wscfaachMappings: SoapMethod[];
  wsAxonRespuestaTransaccionesMappings: SoapMethod[];
};

type IntegrationMethod = { id: number; code: string; isActive: boolean };
type MappingSet = { id: string; status: string | number; isActive: boolean; rules: unknown[] };
type MappingValidation = { isValid: boolean; issues: Array<{ severity: string; message: string }> };

const ui = (process.env['ACH_UI_URL'] ?? 'http://localhost:743').replace(/\/+$/, '');
const api = (process.env['ACH_API_URL'] ?? 'http://localhost:843').replace(/\/+$/, '');
const username = process.env['ACH_USER'] ?? '';
const password = process.env['ACH_PASS'] ?? '';
const endpoint = 'http://localhost:7083/WSCFAACH.svc';
const verifyOnly = process.env['SOAP_SETTINGS_VERIFY_ONLY'] === 'true';
const evidenceDir = resolve(process.cwd(), '..', '..', 'docs', 'uat', 'evidencias', 'transactions-create');
mkdirSync(evidenceDir, { recursive: true });

test.describe.configure({ mode: 'serial' });
test.skip(!username || !password, 'ACH_USER y ACH_PASS son requeridos para la validación LIVE local.');

test('configura ambos servicios desde la SPA y conserva cada valor al navegar', async ({ page }, testInfo) => {
  test.setTimeout(180_000);
  const consoleErrors: string[] = [];
  const failedRequests: string[] = [];
  page.on('console', (message) => {
    if (message.type() === 'error') {
      consoleErrors.push(message.text());
    }
  });
  page.on('requestfailed', (request) => {
    failedRequests.push(`${request.method()} ${request.url()} ${request.failure()?.errorText ?? ''}`);
  });

  const token = await authenticate(page);
  await page.waitForLoadState('networkidle');
  await page.goto(`${ui}/integraciones/soap-settings`);
  await expect(page.locator('[data-testid="soap-settings-page"]')).toBeVisible();
  await page.waitForLoadState('networkidle');

  if (verifyOnly) {
    await assertMethod(page, 'Proc_Contrapartidas');
    await assertMethod(page, 'Proc_Transacciones');
    const apiResponse = await page.request.get(`${api}/api/users/soap-integrations`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(apiResponse.ok()).toBeTruthy();
    const persisted = await apiResponse.json() as SoapSettings;
    assertPersistedMethod(persisted, 'Proc_Contrapartidas');
    assertPersistedMethod(persisted, 'Proc_Transacciones');
    assertNoForbiddenMappings(persisted);
    await assertPublishedMappings(page, token);
    await page.screenshot({
      path: resolve(evidenceDir, 'configuracion-soap-live.png'),
      fullPage: true
    });
    expect(failedRequests).toEqual([]);
    expect(consoleErrors.filter((message) => !/favicon|ResizeObserver/i.test(message))).toEqual([]);
    return;
  }

  await configureMethod(page, 'Proc_Contrapartidas');
  await configureMethod(page, 'Proc_Transacciones');

  await page.reload();
  await assertMethod(page, 'Proc_Contrapartidas');
  await assertMethod(page, 'Proc_Transacciones');

  await selectMethod(page, 'Proc_Contrapartidas');
  await page.getByRole('link', { name: 'Ver relación de campos', exact: true }).click();
  await expect(page).toHaveURL(/\/integraciones\/mappings/);
  await page.waitForLoadState('networkidle');
  await expect(page.getByText('PLValidarUsuarioBV')).toHaveCount(0);

  await page.goto(`${ui}/integraciones/soap-settings`);
  await assertMethod(page, 'Proc_Contrapartidas');
  await page.waitForLoadState('networkidle');
  await page.locator('[data-testid="soap-edit-button"]').click();
  await page.locator('[data-testid="soap-endpoint-input"]').fill('http://valor-no-persistido.local');
  await page.locator('[data-testid="soap-cancel-button"]').click();
  await expect(page.locator('[data-testid="soap-endpoint-input"]')).toHaveValue(endpoint);

  await page.locator('[data-testid="soap-edit-button"]').click();
  const unchangedSave = page.waitForResponse((response) =>
    response.request().method() === 'PUT'
    && new URL(response.url()).pathname.toLowerCase().endsWith('/api/users/soap-integrations')
  );
  await page.locator('[data-testid="soap-save-button"]').click();
  expect((await unchangedSave).ok()).toBeTruthy();
  await expect(page.locator('[data-testid="soap-endpoint-input"]')).toHaveValue(endpoint);

  const apiResponse = await page.request.get(`${api}/api/users/soap-integrations`, {
    headers: { Authorization: `Bearer ${token}` }
  });
  expect(apiResponse.ok()).toBeTruthy();
  const persisted = await apiResponse.json() as SoapSettings;
  assertPersistedMethod(persisted, 'Proc_Contrapartidas');
  assertPersistedMethod(persisted, 'Proc_Transacciones');
  assertNoForbiddenMappings(persisted);
  await assertPublishedMappings(page, token);

  await testInfo.attach('soap-settings-live.png', {
    body: await page.screenshot({ fullPage: true }),
    contentType: 'image/png'
  });
  await page.screenshot({
    path: resolve(evidenceDir, 'configuracion-soap-live.png'),
    fullPage: true
  });
  await page.waitForLoadState('networkidle');

  expect(failedRequests, `No deben existir requests fallidos: ${failedRequests.join(' | ')}`).toEqual([]);
  expect(
    consoleErrors.filter((message) => !/favicon|ResizeObserver/i.test(message)),
    `No deben existir errores de consola: ${consoleErrors.join(' | ')}`
  ).toEqual([]);
});

async function configureMethod(page: Page, methodName: string): Promise<void> {
  await selectMethod(page, methodName);
  await page.locator('[data-testid="soap-edit-button"]').click();
  await page.locator('[data-testid="soap-endpoint-input"]').fill(endpoint);
  await page.locator('[data-testid="soap-operating-mode"]').selectOption('Live');
  const enabled = page.locator('input[formcontrolname="enabled"]');
  if (!(await enabled.isChecked())) {
    await enabled.check();
  }

  const saveResponse = page.waitForResponse((response) =>
    response.request().method() === 'PUT'
    && new URL(response.url()).pathname.toLowerCase().endsWith('/api/users/soap-integrations')
  );
  await page.locator('[data-testid="soap-save-button"]').click();
  const response = await saveResponse;
  expect(response.ok(), `${methodName} debe guardarse desde la SPA.`).toBeTruthy();
  await expect(page.locator('[data-testid="soap-endpoint-input"]')).toHaveValue(endpoint);
  await expect(page.locator('[data-testid="soap-operating-mode"]')).toHaveValue('Live');
}

async function assertMethod(page: Page, methodName: string): Promise<void> {
  await selectMethod(page, methodName);
  await expect(page.locator('[data-testid="soap-endpoint-input"]')).toHaveValue(endpoint);
  await expect(page.locator('[data-testid="soap-operating-mode"]')).toHaveValue('Live');
  await expect(page.locator('input[formcontrolname="enabled"]')).toBeChecked();
}

async function selectMethod(page: Page, methodName: string): Promise<void> {
  const method = page.locator('[data-testid="soap-service-card"]').filter({ hasText: methodName });
  await expect(method).toHaveCount(1);
  await method.click();
  await expect(page.getByRole('heading', { name: methodName, exact: true })).toBeVisible();
}

function assertPersistedMethod(settings: SoapSettings, methodName: string): void {
  const method = settings.wscfaachMappings.find((item) => item.methodName === methodName);
  expect(method, `${methodName} debe existir en el API.`).toBeTruthy();
  expect(method!.endpoint).toBe(endpoint);
  expect(method!.operatingMode).toBe('Live');
  expect(method!.enabled).toBeTruthy();
}

function assertNoForbiddenMappings(settings: SoapSettings): void {
  const methods = [...settings.wscfaachMappings, ...settings.wsAxonRespuestaTransaccionesMappings];
  for (const method of methods) {
    for (const mapping of method.inputParameterMappings ?? []) {
      expect(mapping.inputName).not.toMatch(/^METODO$|^PLValidarUsuarioBV$/i);
      expect(mapping.soapParameterName).not.toMatch(/^METODO$|^PLValidarUsuarioBV$/i);
    }
  }
}

async function assertPublishedMappings(page: Page, token: string): Promise<void> {
  const headers = { Authorization: `Bearer ${token}` };
  const methodsResponse = await page.request.get(`${api}/api/integrations/methods`, { headers });
  expect(methodsResponse.ok()).toBeTruthy();
  const methods = await methodsResponse.json() as IntegrationMethod[];

  for (const code of ['WSCFAACH.Proc_Contrapartidas', 'WSCFAACH.Proc_Transacciones']) {
    const method = methods.find((item) => item.code === code && item.isActive);
    expect(method, `${code} debe existir activo en el catálogo de integración.`).toBeTruthy();
    const publishedResponse = await page.request.get(
      `${api}/api/integrations/mappingsets/published?methodId=${method!.id}`,
      { headers }
    );
    expect(publishedResponse.ok(), `${code} debe tener un mapping publicado.`).toBeTruthy();
    const published = await publishedResponse.json() as MappingSet;
    expect([2, 'Published']).toContain(published.status);
    expect(published.isActive).toBeTruthy();
    expect(published.rules.length).toBeGreaterThan(0);

    const validationResponse = await page.request.post(
      `${api}/api/integrations/mappingsets/${published.id}/validate`,
      { headers, data: { includeWarnings: true } }
    );
    expect(validationResponse.ok()).toBeTruthy();
    const validation = await validationResponse.json() as MappingValidation;
    expect(
      validation.isValid,
      `${code} debe validar sin bloqueos: ${validation.issues.map((item) => `${item.severity}:${item.message}`).join(' | ')}`
    ).toBeTruthy();
  }
}

async function authenticate(page: Page): Promise<string> {
  await page.goto(`${ui}/login`);
  await page.getByLabel('Usuario').fill(username);
  await page.locator('input[formcontrolname="password"]').fill(password);
  const login = page.waitForResponse((response) =>
    response.request().method() === 'POST'
    && new URL(response.url()).pathname.toLowerCase().endsWith('/auth/login')
  );
  await page.getByRole('button', { name: 'Ingresar', exact: true }).click();
  const response = await login;
  expect(response.ok()).toBeTruthy();
  const payload = await response.json() as AuthLoginResponse;
  const token = payload.data?.token
    ?? await page.evaluate(() => window.sessionStorage.getItem('ach.interbank.access_token'))
    ?? '';
  expect(token).toBeTruthy();
  return token;
}
