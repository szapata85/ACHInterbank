import { expect, Page, test } from '@playwright/test';
import { mkdirSync } from 'node:fs';
import { resolve } from 'node:path';

type AuthLoginResponse = { data?: { token?: string } };
type SoapMethod = {
  methodName: string;
  endpoint: string;
  soapAction: string;
  operatingMode: string;
  enabled: boolean;
  inputParameterMappings: Array<{
    inputName: string;
    soapParameterName: string;
    defaultValue?: string | null;
    required: boolean;
  }>;
};
type SoapSettings = {
  wscfaachMappings: SoapMethod[];
  wsAxonRespuestaTransaccionesMappings: SoapMethod[];
};

const ui = (process.env['ACH_UI_URL'] ?? 'http://localhost:743').replace(/\/+$/, '');
const api = (process.env['ACH_API_URL'] ?? 'http://localhost:843').replace(/\/+$/, '');
const username = process.env['ACH_USER'] ?? '';
const password = process.env['ACH_PASS'] ?? '';
const evidenceDir = resolve(process.cwd(), '..', '..', 'docs', 'ux', 'evidencias', 'integraciones-angular-material', 'final');
mkdirSync(evidenceDir, { recursive: true });

test.describe.configure({ mode: 'serial' });
test.skip(!username || !password, 'ACH_USER y ACH_PASS deben suministrarse mediante el mecanismo UAT existente.');

test('settings: editar, cancelar y navegar no persiste; guardar es aislado, único y reversible', async ({ page }) => {
  test.setTimeout(180_000);
  const monitor = monitorPage(page);
  const token = await authenticate(page);
  const headers = { Authorization: `Bearer ${token}` };
  const originalResponse = await page.request.get(`${api}/api/users/soap-integrations`, { headers });
  expect(originalResponse.ok()).toBeTruthy();
  const original = await originalResponse.json() as SoapSettings;
  let restorationRequired = false;

  try {
    await navigateTo(page, '/integraciones/soap-settings');
    await expect(page.locator('[data-testid="soap-settings-page"]')).toBeVisible();
    await expect(page.locator('[data-testid="soap-settings-page"]').getByRole('heading', { name: 'Configuración de servicios SOAP', exact: true })).toBeVisible();
    await expect(page.locator('[data-testid="soap-service-card"]')).toHaveCount(3);

    const beforePassiveActions = monitor.adminWrites.length;
    for (const method of allMethods(original)) {
      await selectMethod(page, method.methodName);
      await expect(page.locator('[data-testid="soap-endpoint-input"]')).toHaveValue(method.endpoint);
    }

    await selectMethod(page, 'Proc_Transacciones');
    await page.locator('[data-testid="soap-edit-button"]').click();
    await page.locator('[data-testid="soap-endpoint-input"]').fill('');
    await page.locator('[data-testid="soap-endpoint-input"]').blur();
    await expect(page.getByText('El endpoint es obligatorio.')).toBeVisible();
    await page.locator('[data-testid="soap-cancel-button"]').click();
    await expect(page.locator('[data-testid="soap-endpoint-input"]')).toHaveValue(findMethod(original, 'Proc_Transacciones').endpoint);

    await page.locator('[data-testid="soap-view-mappings"]').click();
    await expect(page).toHaveURL(/\/integraciones\/mappings/);
    await page.goBack();
    await expect(page.locator('[data-testid="soap-settings-page"]')).toBeVisible();
    expect(monitor.adminWrites.length).toBe(beforePassiveActions);

    const target = findMethod(original, 'RegistrarRespuestaTransaccion');
    await selectMethod(page, target.methodName);
    await page.locator('[data-testid="soap-edit-button"]').click();
    const reversibleSoapAction = `${target.soapAction.replace(/#ux-check$/, '')}#ux-check`;
    await page.locator('[data-testid="soap-action-input"]').fill(reversibleSoapAction);
    const putsBeforeSave = monitor.settingsPuts;
    restorationRequired = true;
    const saveResponse = page.waitForResponse((response) =>
      response.request().method() === 'PUT'
      && new URL(response.url()).pathname.toLowerCase().endsWith('/api/users/soap-integrations')
    );
    await page.locator('[data-testid="soap-save-button"]').click({ clickCount: 2 });
    expect((await saveResponse).ok()).toBeTruthy();
    await expect.poll(() => monitor.settingsPuts).toBe(putsBeforeSave + 1);

    await page.reload();
    await selectMethod(page, target.methodName);
    await expect(page.locator('[data-testid="soap-action-input"]')).toHaveValue(reversibleSoapAction);
    const persisted = await readSettings(page, headers);
    expect(findMethod(persisted, target.methodName).soapAction).toBe(reversibleSoapAction);
    expect(findMethod(persisted, 'Proc_Transacciones')).toEqual(findMethod(original, 'Proc_Transacciones'));
    expect(findMethod(persisted, 'Proc_Contrapartidas')).toEqual(findMethod(original, 'Proc_Contrapartidas'));
  } finally {
    if (restorationRequired) {
      const restore = await page.request.put(`${api}/api/users/soap-integrations`, { headers, data: original });
      expect(restore.ok(), 'La configuración SOAP original debe restaurarse incluso si falla la prueba.').toBeTruthy();
      expect(await readSettings(page, headers)).toEqual(original);
    }
  }

  assertSafeMonitor(monitor);
});

test('mappings: separa servicios, filtra y no persiste al abrir, validar o cancelar', async ({ page }) => {
  test.setTimeout(240_000);
  const monitor = monitorPage(page);
  await authenticate(page);
  await navigateToMappings(page);
  await expect(page.locator('[data-testid="integration-mappings-page"]')).toBeVisible();
  await expect(page.locator('[data-testid="integration-mappings-page"]').getByRole('heading', { name: 'Matriz de campos SOAP', exact: true })).toBeVisible();
  try {
    await expect(page.getByRole('tab')).toHaveCount(3, { timeout: 30_000 });
  } catch {
    throw new Error(`No se cargaron los servicios SOAP. Red=${JSON.stringify({
      failedRequests: monitor.failedRequests,
      unexpectedResponses: monitor.unexpectedResponses,
      pageErrors: monitor.pageErrors,
      consoleErrors: monitor.consoleErrors
    })}`);
  }

  for (const service of ['Proc_Transacciones', 'Proc_Contrapartidas', 'RegistrarRespuestaTransaccion']) {
    await page.getByRole('tab', { name: service, exact: true }).click();
    await expect(page.locator('mat-card-title').filter({ hasText: service })).toBeVisible();
    await expect(page.locator('[data-testid="mapping-matrix-row"]').first()).toBeVisible({ timeout: 30_000 });
  }

  await page.getByRole('tab', { name: 'Proc_Transacciones', exact: true }).click();
  const search = page.getByLabel('Buscar parámetro, tabla, campo o regla');
  await search.fill('NACHA');
  await expect(page.getByText(/\d+ de \d+ parámetros/)).toBeVisible();
  await page.getByRole('button', { name: 'Limpiar' }).click();

  const writesBeforeEdit = monitor.adminWrites.length;
  await openFirstRowMenu(page);
  await page.getByRole('menuitem', { name: 'Editar relación' }).click();
  await expect(page.locator('.integration-mappings__dialog')).toBeVisible();
  await page.screenshot({
    path: resolve(evidenceDir, 'mapping-editor-dialog-desktop.png'),
    fullPage: true
  });
  await page.locator('[data-testid="source-field-select"]').click();
  await page.getByRole('option', { name: 'Sin mapear' }).click();
  await page.getByRole('button', { name: 'Guardar relación' }).click();
  await expect(page.getByText('Selecciona un campo de origen para activar la relación.')).toBeVisible();
  await page.screenshot({
    path: resolve(evidenceDir, 'mapping-validation-error-desktop.png'),
    fullPage: true
  });
  expect(monitor.adminWrites.length).toBe(writesBeforeEdit);
  await page.getByRole('button', { name: 'Cancelar' }).click();
  expect(monitor.adminWrites.length).toBe(writesBeforeEdit);

  await openFirstRowMenu(page);
  await page.getByRole('menuitem', { name: 'Abrir editor avanzado' }).click();
  await expect(page).toHaveURL(/\/integraciones\/mappings\/.+\/.+/);
  await expect(page.getByText('Diseñador de regla')).toBeVisible();
  await expect(page.locator('[data-testid="source-kind-select"]')).toBeVisible();
  await page.screenshot({
    path: resolve(evidenceDir, 'mapping-advanced-editor-desktop.png'),
    fullPage: true
  });
  expect(monitor.adminWrites.length).toBe(writesBeforeEdit);

  assertSafeMonitor(monitor);
});

test('desktop, tablet y móvil mantienen controles y scroll dentro del contenido', async ({ page }) => {
  test.setTimeout(180_000);
  const monitor = monitorPage(page);
  await authenticate(page);
  const viewports = [
    { name: 'desktop', width: 1440, height: 900 },
    { name: 'tablet', width: 768, height: 1024 },
    { name: 'mobile', width: 390, height: 844 }
  ];

  for (const viewport of viewports) {
    await page.setViewportSize({ width: viewport.width, height: viewport.height });
    for (const route of ['mappings', 'soap-settings']) {
      if (route === 'mappings') {
        await navigateToMappings(page);
      } else {
        await navigateTo(page, '/integraciones/soap-settings');
      }
      const root = route === 'mappings'
        ? page.locator('[data-testid="integration-mappings-page"]')
        : page.locator('[data-testid="soap-settings-page"]');
      await expect(root).toBeVisible();
      await page.waitForLoadState('networkidle');
      await expect(page.locator('mat-progress-bar')).toHaveCount(0);
      const bodyOverflow = await page.evaluate(() => document.documentElement.scrollWidth > document.documentElement.clientWidth + 1);
      expect(bodyOverflow, `${route} no debe desbordar el body en ${viewport.name}.`).toBeFalsy();
      await page.screenshot({
        path: resolve(evidenceDir, `${route}-${viewport.name}.png`),
        fullPage: true
      });
    }
  }

  assertSafeMonitor(monitor);
});

function monitorPage(page: Page) {
  const state = {
    consoleErrors: [] as string[],
    pageErrors: [] as string[],
    failedRequests: [] as string[],
    navigationAborts: [] as string[],
    unexpectedResponses: [] as string[],
    adminWrites: [] as string[],
    operationalCalls: [] as string[],
    settingsPuts: 0
  };
  page.on('console', (message) => {
    if (message.type() === 'error') state.consoleErrors.push(message.text());
  });
  page.on('pageerror', (error) => state.pageErrors.push(error.message));
  page.on('requestfailed', (request) => {
    const path = new URL(request.url()).pathname;
    const failure = request.failure()?.errorText ?? 'desconocido';
    const description = `${request.method()} ${path} (${failure})`;
    if (request.method() === 'POST' && path.toLowerCase().endsWith('/auth/refresh') && /abort/i.test(failure)) {
      state.navigationAborts.push(description);
      return;
    }
    state.failedRequests.push(description);
  });
  page.on('response', (response) => {
    if (response.status() >= 400) state.unexpectedResponses.push(`${response.status()} ${response.request().method()} ${new URL(response.url()).pathname}`);
  });
  page.on('request', (request) => {
    const method = request.method();
    const url = new URL(request.url());
    const path = url.pathname.toLowerCase();
    if (method === 'PUT' && path.endsWith('/api/users/soap-integrations')) state.settingsPuts += 1;
    if (['POST', 'PUT', 'PATCH', 'DELETE'].includes(method)
      && !/\/(auth\/login|auth\/refresh|api\/navigation-logs)$/.test(path)) {
      state.adminWrites.push(`${method} ${path}`);
    }
    if (url.port === '7083'
      || /dispatch-cycle|post-processing|\/execute$|\/send$/.test(path)) {
      state.operationalCalls.push(`${method} ${url.origin}${path}`);
    }
  });
  return state;
}

function assertSafeMonitor(monitor: ReturnType<typeof monitorPage>): void {
  expect(monitor.consoleErrors.filter((message) => !/favicon|ResizeObserver/i.test(message))).toEqual([]);
  expect(monitor.pageErrors).toEqual([]);
  expect(monitor.failedRequests).toEqual([]);
  expect(monitor.unexpectedResponses).toEqual([]);
  expect(monitor.operationalCalls, 'No debe invocarse ningún procedimiento SOAP operativo.').toEqual([]);
}

async function authenticate(page: Page): Promise<string> {
  await navigateTo(page, '/login');
  await page.getByLabel('Usuario').fill(username);
  await page.locator('input[formcontrolname="password"]').fill(password);
  const login = page.waitForResponse((response) =>
    response.request().method() === 'POST' && new URL(response.url()).pathname.toLowerCase().endsWith('/auth/login')
  );
  await page.getByRole('button', { name: 'Ingresar', exact: true }).click();
  const response = await login;
  expect(response.ok()).toBeTruthy();
  const payload = await response.json() as AuthLoginResponse;
  const token = payload.data?.token
    ?? await page.evaluate(() => window.sessionStorage.getItem('ach.interbank.access_token'))
    ?? '';
  expect(token).toBeTruthy();
  await expect(page).toHaveURL(/\/dashboard(?:[/?#]|$)/);
  await expect(page.locator('app-main-layout')).toBeVisible();
  await expect(page.locator('app-loading-overlay .overlay')).toHaveCount(0);
  return token;
}

async function navigateTo(page: Page, path: string): Promise<void> {
  await page.goto(`${ui}${path}`, {
    waitUntil: 'domcontentloaded',
    timeout: 30_000
  });
}

async function navigateToMappings(page: Page): Promise<void> {
  await navigateTo(page, '/integraciones/soap-settings');
  await expect(page.locator('[data-testid="soap-settings-page"]')).toBeVisible();
  await page.locator('[data-testid="soap-view-mappings"]').click();
  await expect(page).toHaveURL(/\/integraciones\/mappings(?:[/?#]|$)/);
}

async function selectMethod(page: Page, methodName: string): Promise<void> {
  const method = page.locator('[data-testid="soap-service-card"]').filter({ hasText: methodName });
  await expect(method).toHaveCount(1);
  await method.click();
  await expect(page.locator('mat-card-title').filter({ hasText: methodName })).toBeVisible();
}

async function openFirstRowMenu(page: Page): Promise<void> {
  await page.locator('[data-testid="mapping-detail-button"]').first().click();
  await expect(page.getByRole('menu')).toBeVisible();
}

function allMethods(settings: SoapSettings): SoapMethod[] {
  return [...settings.wscfaachMappings, ...settings.wsAxonRespuestaTransaccionesMappings];
}

function findMethod(settings: SoapSettings, methodName: string): SoapMethod {
  const method = allMethods(settings).find((item) => item.methodName === methodName);
  expect(method, `${methodName} debe existir en la configuración.`).toBeTruthy();
  return method!;
}

async function readSettings(page: Page, headers: Record<string, string>): Promise<SoapSettings> {
  const response = await page.request.get(`${api}/api/users/soap-integrations`, { headers });
  expect(response.ok()).toBeTruthy();
  return response.json() as Promise<SoapSettings>;
}
