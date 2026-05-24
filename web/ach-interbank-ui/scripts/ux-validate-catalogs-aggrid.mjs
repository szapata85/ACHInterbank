import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { chromium } from 'playwright';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, '../../..');
const evidenceRoot = path.join(repoRoot, 'docs/ux/evidencias/catalogs-aggrid');
const screenshotsDir = path.join(evidenceRoot, 'screenshots');
const jsonPath = path.join(evidenceRoot, 'catalogs-aggrid-validation.json');
const mdPath = path.join(evidenceRoot, 'catalogs-aggrid-validation.md');
const baseUrl = process.env.ACH_UAT_BASE_URL || 'http://localhost:743';
const username = process.env.ACH_UAT_DEMO_USERNAME || 'admin';
const password = process.env.ACH_UAT_DEMO_PASSWORD;

const routes = [
  '/catalogs/financial-institutions',
  '/catalogs/bank-holidays',
  '/catalogs/document-types',
  '/catalogs/person-types',
  '/catalogs/phone-types',
  '/catalogs/email-types',
  '/catalogs/address-types',
  '/catalogs/transaction-codes',
  '/customer-third-parties'
];

function slug(route) {
  return route.replace(/^\/+/, '').replace(/[^a-z0-9]+/gi, '_').replace(/^_+|_+$/g, '');
}

function isExtensionNoise(text) {
  return /message channel closed before a response was received/i.test(text)
    || /asynchronous response by returning true/i.test(text);
}

function isIgnorableRequestFailure(request) {
  return /fonts\.gstatic\.com|fonts\.googleapis\.com|api\/users\/branding/i.test(request.url)
    && /net::ERR_ABORTED/i.test(request.failure || '');
}

async function login(page, result) {
  if (!password) {
    throw new Error('ACH_UAT_DEMO_PASSWORD no esta definido.');
  }

  const response = await page.request.post(`${baseUrl}/auth/login`, {
    data: { username, password }
  });
  if (!response.ok()) {
    throw new Error(`Login demo fallo con HTTP ${response.status()}.`);
  }

  const payload = await response.json();
  const token = payload?.data?.token || payload?.token;
  if (!token) {
    throw new Error('Login demo no devolvio token.');
  }

  await page.goto(`${baseUrl}/`, { waitUntil: 'domcontentloaded' }).catch(() => {});
  await page.evaluate((accessToken) => {
    window.sessionStorage.setItem('ach.interbank.access_token', accessToken);
  }, token);
  result.runtime.loginOk = true;
}

async function inspectRoute(page, route) {
  const responses = [];
  const failedRequests = [];
  const consoleErrors = [];

  const onResponse = async (response) => {
    const url = response.url();
    if (!url.startsWith(baseUrl)) return;

    responses.push({
      method: response.request().method(),
      url,
      status: response.status(),
      contentType: response.headers()['content-type'] || ''
    });
  };
  const onFailed = (request) => {
    failedRequests.push({
      method: request.method(),
      url: request.url(),
      failure: request.failure()?.errorText || ''
    });
  };
  const onConsole = (message) => {
    if (message.type() === 'error' && !isExtensionNoise(message.text())) {
      consoleErrors.push(message.text());
    }
  };

  page.on('response', onResponse);
  page.on('requestfailed', onFailed);
  page.on('console', onConsole);

  await page.goto(`${baseUrl}${route}`, { waitUntil: 'domcontentloaded', timeout: 30000 });
  await page.waitForTimeout(2500);

  const screenshotPath = path.join(screenshotsDir, `${slug(route)}.png`);
  await page.screenshot({ path: screenshotPath, fullPage: true });

  const visual = await page.evaluate(() => {
    const visible = (element) => {
      const rect = element.getBoundingClientRect();
      const style = window.getComputedStyle(element);
      return rect.width > 0 && rect.height > 0 && style.display !== 'none' && style.visibility !== 'hidden';
    };
    const bodyText = document.body.innerText || '';
    const gridRoots = Array.from(document.querySelectorAll('ag-grid-angular, .ag-root')).filter(visible);
    const headers = Array.from(document.querySelectorAll('.ag-header-cell')).filter(visible).map((header) => {
      const rect = header.getBoundingClientRect();
      return {
        text: (header.textContent || '').trim().replace(/\s+/g, ' '),
        width: Math.round(rect.width)
      };
    });
    const criticalButtons = Array.from(document.querySelectorAll('button, .btn, [role="button"]')).filter(visible).map((button) => {
      const rect = button.getBoundingClientRect();
      return {
        text: (button.textContent || '').trim().replace(/\s+/g, ' '),
        width: Math.round(rect.width),
        height: Math.round(rect.height)
      };
    });
    const loadingVisible = /Cargando|Loading|Procesando/i.test(bodyText)
      || Array.from(document.querySelectorAll('[class*="spinner"], [class*="loading"], mat-spinner, .loader')).some(visible);
    const emptyVisible = /No hay registros|No hay instituciones|No hay festivos|Sin resultados/i.test(bodyText);
    const errorVisible = /No fue posible|error|Error|No autorizado/i.test(bodyText);
    const horizontalScroll = document.body.scrollWidth > document.documentElement.clientWidth + 2;
    const narrowHeaders = headers.filter((header) => header.text && header.width < 80);
    const cutButtons = criticalButtons.filter((button) => button.text && (button.width < 40 || button.height < 24));

    return {
      url: window.location.href,
      gridCount: gridRoots.length,
      headers,
      narrowHeaders,
      criticalButtons,
      cutButtons,
      loadingVisible,
      emptyVisible,
      errorVisible,
      horizontalScroll,
      bodyTextSample: bodyText.slice(0, 600)
    };
  });

  page.off('response', onResponse);
  page.off('requestfailed', onFailed);
  page.off('console', onConsole);

  const apiResponses = responses.filter((response) => !/\.(js|css|png|svg|ico|woff2?)($|\?)/i.test(response.url));
  const htmlApiResponses = apiResponses.filter((response) => {
    const pathname = new URL(response.url).pathname;
    return response.contentType.includes('text/html')
      && !pathname.startsWith('/catalogs/')
      && pathname !== '/customer-third-parties';
  });
  const unhandledFailures = failedRequests.filter((request) => !isIgnorableRequestFailure(request));
  const findings = [];

  if (consoleErrors.length) findings.push('console-errors');
  if (unhandledFailures.length) findings.push('failed-requests');
  if (htmlApiResponses.length) findings.push('api-returned-html');
  if (visual.loadingVisible) findings.push('loading-visible');
  if (visual.horizontalScroll) findings.push('horizontal-scroll');
  if (visual.narrowHeaders.length) findings.push('narrow-grid-columns');
  if (visual.cutButtons.length) findings.push('cut-buttons');
  if (!visual.gridCount && !visual.emptyVisible && !visual.errorVisible) findings.push('missing-grid-or-empty-state');

  return {
    route,
    screenshotPath: path.relative(repoRoot, screenshotPath).replaceAll('\\', '/'),
    ok: findings.length === 0,
    findings,
    apiResponses,
    failedRequests: unhandledFailures,
    consoleErrors,
    visual
  };
}

function markdown(result) {
  const lines = [
    '# Validacion catalogos y AG Grid',
    '',
    `- Generado: ${result.generatedAt}`,
    `- Base URL: ${result.baseUrl}`,
    `- Login demo: ${result.runtime.loginOk ? 'OK' : 'FALLIDO'}`,
    `- Rutas: ${result.summary.total}`,
    `- OK: ${result.summary.ok}`,
    `- Con hallazgos: ${result.summary.withFindings}`,
    '',
    '| Ruta | Estado | Hallazgos | Screenshot |',
    '|---|---:|---|---|'
  ];

  for (const route of result.routes) {
    lines.push(`| ${route.route} | ${route.ok ? 'OK' : 'Revisar'} | ${route.findings.join(', ') || '-'} | ${route.screenshotPath} |`);
  }

  return `${lines.join('\n')}\n`;
}

async function main() {
  await mkdir(screenshotsDir, { recursive: true });
  const result = {
    generatedAt: new Date().toISOString(),
    baseUrl,
    runtime: { loginOk: false },
    summary: { total: routes.length, ok: 0, withFindings: 0 },
    routes: []
  };

  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage({ viewport: { width: 1366, height: 768 } });

  await login(page, result);
  for (const route of routes) {
    const inspected = await inspectRoute(page, route);
    result.routes.push(inspected);
    if (inspected.ok) result.summary.ok += 1;
  }

  result.summary.withFindings = result.routes.length - result.summary.ok;
  await browser.close();

  await writeFile(jsonPath, JSON.stringify(result, null, 2), 'utf8');
  await writeFile(mdPath, markdown(result), 'utf8');

  if (result.summary.withFindings > 0) {
    console.error(`Validacion catalogos/AG Grid con ${result.summary.withFindings} ruta(s) con hallazgos.`);
    process.exitCode = 1;
  } else {
    console.log('Validacion catalogos/AG Grid OK.');
  }
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
