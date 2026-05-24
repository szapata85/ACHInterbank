import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { chromium } from 'playwright';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, '../../..');
const evidenceRoot = path.join(repoRoot, 'docs/ux/evidencias/reports-pdf');
const screenshotsDir = path.join(evidenceRoot, 'screenshots');
const validationPath = path.join(evidenceRoot, 'reports-pdf-validation.json');
const reconciliationPath = path.join(evidenceRoot, 'reconciliation-pdf-result.json');
const traceabilityPath = path.join(evidenceRoot, 'traceability-pdf-result.json');
const baseUrl = process.env.ACH_UAT_BASE_URL || 'http://localhost:743';
const username = process.env.ACH_UAT_DEMO_USERNAME || 'admin';
const password = process.env.ACH_UAT_DEMO_PASSWORD;

const result = {
  generatedAt: new Date().toISOString(),
  baseUrl,
  ok: false,
  runtime: {
    loginOk: false
  },
  summary: {
    reconciliationOk: false,
    traceabilityOk: false,
    failedRequests: 0,
    consoleErrors: 0
  },
  reconciliation: null,
  traceability: null,
  errors: []
};

function addError(message) {
  result.errors.push(message);
}

function isExtensionNoise(text) {
  return /message channel closed before a response was received/i.test(text)
    || /asynchronous response by returning true/i.test(text);
}

function isIgnorableFailure(item) {
  const aborted = item.failure === 'net::ERR_ABORTED';
  return aborted && (
    /fonts\.gstatic\.com/i.test(item.url)
    || /fonts\.googleapis\.com/i.test(item.url)
    || /\/api\/users\/branding$/i.test(item.url)
  );
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

  await page.goto(`${baseUrl}/`, { waitUntil: 'domcontentloaded' }).catch(() => {});
  await page.evaluate((accessToken) => {
    window.sessionStorage.setItem('ach.interbank.access_token', accessToken);
  }, token);
  result.runtime.loginOk = true;
}

async function waitForOptionalDownload(page, clickAction) {
  const downloadPromise = page.waitForEvent('download', { timeout: 5000 }).catch(() => null);
  await clickAction();
  const download = await downloadPromise;
  if (!download) {
    return null;
  }

  const stream = await download.createReadStream();
  let bytes = 0;
  for await (const chunk of stream) {
    bytes += chunk.length;
  }

  return {
    suggestedFilename: download.suggestedFilename(),
    bytes
  };
}

async function validateReconciliation(page) {
  const apiResponses = [];
  page.on('response', (response) => {
    if (response.url().includes('/api/reports/reconciliation')) {
      apiResponses.push({
        url: response.url(),
        status: response.status(),
        contentType: response.headers()['content-type'] ?? null,
        contentLength: response.headers()['content-length'] ?? null
      });
    }
  });

  await page.goto(`${baseUrl}/reports/reconciliation`, { waitUntil: 'networkidle', timeout: 30000 });
  await page.screenshot({ path: path.join(screenshotsDir, 'reconciliation-before-export.png'), fullPage: true });

  const download = await waitForOptionalDownload(page, () => page.getByRole('button', { name: /Exportar PDF/i }).click());
  await page.waitForTimeout(500);

  const messageVisible = await page.getByText(/No hay informacion para exportar|No hay información para exportar/i).isVisible().catch(() => false);
  const outcome = {
    route: '/reports/reconciliation',
    apiResponses,
    download,
    noDataMessageVisible: messageVisible,
    ok: false,
    note: ''
  };

  if (download && download.bytes === 0) {
    outcome.note = 'FALLO: se descargo PDF vacio.';
  } else if (!download && messageVisible) {
    outcome.ok = true;
    outcome.note = 'Sin datos: descarga bloqueada con mensaje claro.';
  } else if (download && download.bytes > 512) {
    outcome.ok = true;
    outcome.note = 'Con datos: PDF descargado con contenido no vacio.';
  } else {
    outcome.note = 'FALLO: no se pudo confirmar PDF valido ni bloqueo claro por falta de datos.';
  }

  await page.screenshot({ path: path.join(screenshotsDir, 'reconciliation-after-export.png'), fullPage: true });
  return outcome;
}

async function validateTraceability(page) {
  const apiResponses = [];
  page.on('response', (response) => {
    if (response.url().includes('/api/reports/traceability/pdf')) {
      apiResponses.push({
        url: response.url(),
        status: response.status(),
        contentType: response.headers()['content-type'] ?? null,
        contentLength: response.headers()['content-length'] ?? null
      });
    }
  });

  await page.goto(`${baseUrl}/reports/traceability`, { waitUntil: 'networkidle', timeout: 30000 });
  const cycleSelect = page.locator('select[formcontrolname="achCycleId"]');
  const cycleOptions = await cycleSelect.locator('option').evaluateAll((options) => options.map((option) => ({
    value: option.getAttribute('value') ?? '',
    label: option.textContent?.trim() ?? ''
  }))).catch(() => []);
  const selectedValues = cycleOptions.map((option) => option.value).filter(Boolean).slice(0, 2);
  if (selectedValues.length > 0) {
    await cycleSelect.selectOption(selectedValues);
  }

  await page.screenshot({ path: path.join(screenshotsDir, 'traceability-before-export.png'), fullPage: true });

  const download = await waitForOptionalDownload(page, () => page.getByRole('button', { name: /Generar PDF/i }).click());
  await page.waitForTimeout(500);

  const messageVisible = await page.getByText(/No hay informacion para exportar|No hay información para exportar/i).isVisible().catch(() => false);
  const requestedUrls = apiResponses.map((response) => response.url);
  const duplicatedCycleQuery = requestedUrls.some((url) => {
    const query = new URL(url).searchParams.get('achCycleId') ?? '';
    const ids = query.split(',').filter(Boolean);
    return ids.length !== new Set(ids).size;
  });

  const outcome = {
    route: '/reports/traceability',
    selectedCycleCount: selectedValues.length,
    apiResponses,
    download,
    noDataMessageVisible: messageVisible,
    duplicatedCycleQuery,
    ok: false,
    note: ''
  };

  if (download && download.bytes === 0) {
    outcome.note = 'FALLO: se descargo PDF vacio.';
  } else if (duplicatedCycleQuery) {
    outcome.note = 'FALLO: la seleccion multiple envio ciclos duplicados.';
  } else if (!download && messageVisible) {
    outcome.ok = true;
    outcome.note = 'Sin datos: descarga bloqueada con mensaje claro y sin ciclos duplicados.';
  } else if (download && download.bytes > 512) {
    outcome.ok = true;
    outcome.note = 'Con datos: PDF descargado con contenido no vacio y sin ciclos duplicados.';
  } else {
    outcome.note = 'FALLO: no se pudo confirmar PDF valido ni bloqueo claro por falta de datos.';
  }

  await page.screenshot({ path: path.join(screenshotsDir, 'traceability-after-export.png'), fullPage: true });
  return outcome;
}

async function main() {
  await mkdir(screenshotsDir, { recursive: true });
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1366, height: 768 }, acceptDownloads: true });
  const failedRequests = [];
  const consoleErrors = [];

  page.on('requestfailed', (request) => {
    const item = {
      method: request.method(),
      url: request.url(),
      failure: request.failure()?.errorText ?? 'unknown'
    };
    if (!isIgnorableFailure(item)) {
      failedRequests.push(item);
    }
  });

  page.on('console', (message) => {
    if (message.type() !== 'error') {
      return;
    }

    const text = message.text();
    if (!isExtensionNoise(text)) {
      consoleErrors.push(text);
    }
  });

  try {
    await loginAndSeedSession(page);
    result.reconciliation = await validateReconciliation(page);
    result.traceability = await validateTraceability(page);
    result.summary.reconciliationOk = result.reconciliation.ok;
    result.summary.traceabilityOk = result.traceability.ok;
    result.summary.failedRequests = failedRequests.length;
    result.summary.consoleErrors = consoleErrors.length;
    if (failedRequests.length > 0) {
      addError(`Requests fallidos no ignorables: ${failedRequests.length}.`);
    }
    if (consoleErrors.length > 0) {
      addError(`Errores de consola: ${consoleErrors.length}.`);
    }
    if (!result.reconciliation.ok) {
      addError(result.reconciliation.note);
    }
    if (!result.traceability.ok) {
      addError(result.traceability.note);
    }
  } catch (error) {
    addError(error instanceof Error ? error.message : String(error));
  } finally {
    result.failedRequests = failedRequests;
    result.consoleErrors = consoleErrors;
    result.ok = result.errors.length === 0;
    await writeFile(reconciliationPath, JSON.stringify(result.reconciliation, null, 2), 'utf8');
    await writeFile(traceabilityPath, JSON.stringify(result.traceability, null, 2), 'utf8');
    await writeFile(validationPath, JSON.stringify(result, null, 2), 'utf8');
    await browser.close();
  }

  if (!result.ok) {
    console.error(JSON.stringify(result, null, 2));
    process.exit(1);
  }

  console.log(JSON.stringify(result, null, 2));
}

main();
