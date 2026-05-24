import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { chromium } from 'playwright';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, '../../..');
const evidenceDir = path.join(repoRoot, 'docs/ux/evidencias');
const screenshotPath = path.join(evidenceDir, 'mapping-editor-proc-transacciones-loaded.png');
const jsonPath = path.join(evidenceDir, 'mapping-editor-proc-transacciones-validation.json');
const baseUrl = process.env.ACH_UAT_BASE_URL || 'http://localhost:743';
const username = process.env.ACH_UAT_DEMO_USERNAME || 'admin';
const password = process.env.ACH_UAT_DEMO_PASSWORD;
const editorPath = '/integraciones/mappings/WSCFAACH.Proc_Transacciones/dc1b034b-4de3-4043-93cc-79072bf8a5e9';

const result = {
  generatedAt: new Date().toISOString(),
  ok: false,
  baseUrl,
  path: editorPath,
  loadingCleared: false,
  formVisible: false,
  errorVisible: false,
  failedRequests: [],
  ignoredFailedRequests: [],
  apiCalls: [],
  consoleErrors: [],
  messageChannelErrorIgnoredAsExtensionNoise: false,
  screenshotPath: 'docs/ux/evidencias/mapping-editor-proc-transacciones-loaded.png',
  errors: []
};

function addError(message) {
  result.errors.push(message);
}

function isMessageChannelNoise(text) {
  return /message channel closed before a response was received/i.test(text)
    || /asynchronous response by returning true/i.test(text);
}

function isIgnorableRequestFailure(item) {
  const isAborted = item.failure === 'net::ERR_ABORTED';
  const isFont = /fonts\.gstatic\.com/i.test(item.url);
  const isInitialBranding = /\/api\/users\/branding$/i.test(item.url);
  return isAborted && (isFont || isInitialBranding);
}

async function loginAndSeedSession(page) {
  if (!password) {
    throw new Error('ACH_UAT_DEMO_PASSWORD no esta definido.');
  }

  const response = await page.request.post(`${baseUrl}/auth/login`, {
    data: { username, password }
  });
  if (!response.ok()) {
    throw new Error(`Login API fallo con HTTP ${response.status()}.`);
  }

  const payload = await response.json();
  const token = payload?.data?.token;
  if (!token) {
    throw new Error('Login API no devolvio token.');
  }

  await page.goto(`${baseUrl}/`, { waitUntil: 'domcontentloaded' }).catch(() => {
    // The first navigation only creates the localhost origin for sessionStorage.
  });
  await page.evaluate((accessToken) => {
    window.sessionStorage.setItem('ach.interbank.access_token', accessToken);
  }, token);
}

async function main() {
  await mkdir(evidenceDir, { recursive: true });
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1366, height: 768 } });
  const pending = new Map();

  page.on('request', (request) => {
    if (!['xhr', 'fetch'].includes(request.resourceType())) return;
    pending.set(request, {
      method: request.method(),
      url: request.url(),
      startedAt: Date.now()
    });
  });

  page.on('response', (response) => {
    const request = response.request();
    const pendingRequest = pending.get(request);
    if (!pendingRequest) return;

    pending.delete(request);
    result.apiCalls.push({
      method: pendingRequest.method,
      url: pendingRequest.url,
      status: response.status(),
      durationMs: Date.now() - pendingRequest.startedAt
    });
  });

  page.on('requestfailed', (request) => {
    const pendingRequest = pending.get(request) ?? {
      method: request.method(),
      url: request.url(),
      startedAt: Date.now()
    };
    pending.delete(request);

    const failed = {
      method: pendingRequest.method,
      url: pendingRequest.url,
      failure: request.failure()?.errorText ?? 'unknown',
      durationMs: Date.now() - pendingRequest.startedAt
    };

    if (isIgnorableRequestFailure(failed)) {
      result.ignoredFailedRequests.push(failed);
      return;
    }

    result.failedRequests.push(failed);
  });

  page.on('console', (message) => {
    if (message.type() !== 'error') return;

    const text = message.text();
    if (isMessageChannelNoise(text)) {
      result.messageChannelErrorIgnoredAsExtensionNoise = true;
      return;
    }

    result.consoleErrors.push(text);
  });

  try {
    await loginAndSeedSession(page);
    await page.goto(`${baseUrl}${editorPath}`, { waitUntil: 'domcontentloaded', timeout: 30000 });

    const loading = page.getByText('Cargando editor funcional', { exact: false });
    await loading.waitFor({ state: 'hidden', timeout: 25000 }).then(
      () => { result.loadingCleared = true; },
      () => { result.loadingCleared = false; }
    );

    const form = page.getByText('Diseñador de regla', { exact: false });
    const fallbackForm = page.getByText('DiseÃ±ador de regla', { exact: false });
    const error = page.getByText('No se pudo abrir el editor', { exact: false });

    result.formVisible = await form.isVisible().catch(() => false) || await fallbackForm.isVisible().catch(() => false);
    result.errorVisible = await error.isVisible().catch(() => false);

    if (!result.loadingCleared) {
      addError('El loading del editor funcional no desaparecio dentro del timeout.');
    }
    if (!result.formVisible && !result.errorVisible) {
      addError('No se mostro formulario ni error funcional claro.');
    }
    if (result.failedRequests.length > 0) {
      addError(`Hay requests fallidos: ${result.failedRequests.length}.`);
    }
    if (result.consoleErrors.length > 0) {
      addError(`Hay errores de consola no atribuibles a extension: ${result.consoleErrors.length}.`);
    }

    await page.screenshot({ path: screenshotPath, fullPage: true });
    result.ok = result.errors.length === 0;
  } catch (error) {
    addError(error instanceof Error ? error.message : String(error));
    await page.screenshot({ path: screenshotPath, fullPage: true }).catch(() => {});
  } finally {
    await writeFile(jsonPath, JSON.stringify(result, null, 2), 'utf8');
    await browser.close();
  }

  if (!result.ok) {
    console.error(JSON.stringify(result, null, 2));
    process.exit(1);
  }

  console.log(JSON.stringify(result, null, 2));
}

main();
