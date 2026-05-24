import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { chromium } from 'playwright';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, '../../..');
const evidenceRoot = path.join(repoRoot, 'docs/ux/evidencias/transactions-returns');
const screenshotPath = path.join(evidenceRoot, 'transactions-returns.png');
const jsonPath = path.join(evidenceRoot, 'transactions-returns-validation.json');
const mdPath = path.join(evidenceRoot, 'transactions-returns-validation.md');
const baseUrl = process.env.ACH_UAT_BASE_URL || 'http://localhost:743';
const username = process.env.ACH_UAT_DEMO_USERNAME || 'admin';
const password = process.env.ACH_UAT_DEMO_PASSWORD;

function isIgnorableFailure(item) {
  return item.failure === 'net::ERR_ABORTED'
    && (/\/api\/users\/branding$/i.test(item.url) || /^https:\/\/fonts\.gstatic\.com\//i.test(item.url));
}

async function login(page) {
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
}

async function main() {
  await mkdir(evidenceRoot, { recursive: true });

  const result = {
    generatedAt: new Date().toISOString(),
    baseUrl,
    ok: false,
    route: '/transactions/returns',
    loadingCleared: false,
    gridVisible: false,
    emptyVisible: false,
    errorVisible: false,
    blankPage: false,
    horizontalScroll: false,
    whiteCriticalButtons: [],
    responses: [],
    failedRequests: [],
    consoleErrors: [],
    screenshotPath: 'docs/ux/evidencias/transactions-returns/transactions-returns.png',
    errors: []
  };

  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage({ viewport: { width: 1366, height: 768 } });

  page.on('requestfailed', (request) => {
    result.failedRequests.push({
      method: request.method(),
      url: request.url(),
      failure: request.failure()?.errorText || ''
    });
  });
  page.on('console', (message) => {
    if (message.type() === 'error') {
      result.consoleErrors.push({ type: message.type(), text: message.text() });
    }
  });
  page.on('response', async (response) => {
    const url = response.url();
    if (!/\/transactions\/returns|\/ach-cycles|\/return-reasons|\/ach-returns/i.test(url)) {
      return;
    }

    let body = '';
    try {
      body = (await response.text()).slice(0, 800);
    } catch {
      body = '';
    }

    result.responses.push({
      method: response.request().method(),
      url,
      status: response.status(),
      contentType: response.headers()['content-type'] || null,
      body
    });
  });

  try {
    await login(page);
    await page.goto(`${baseUrl}/transactions/returns`, { waitUntil: 'domcontentloaded' });
    await page.waitForTimeout(3000);

    const consult = page.getByRole('button', { name: /Consultar/i });
    if (await consult.count()) {
      await consult.first().click();
      await page.waitForTimeout(3000);
    }

    const visual = await page.evaluate(() => {
      const root = document.querySelector('main') || document.body;
      const text = (root.innerText || '').trim();
      const visible = (element) => {
        const rect = element.getBoundingClientRect();
        const style = window.getComputedStyle(element);
        return rect.width > 0 && rect.height > 0 && style.visibility !== 'hidden' && style.display !== 'none';
      };
      const buttons = Array.from(root.querySelectorAll('button')).filter(visible).map((button) => {
        const style = window.getComputedStyle(button);
        return {
          text: (button.textContent || '').trim().replace(/\s+/g, ' '),
          className: button.getAttribute('class') || '',
          background: style.backgroundColor,
          color: style.color
        };
      });
      const whiteCriticalButtons = buttons.filter((button) => {
        if (!/Consultar|Asignar|Generar|Cancelar|Reintentar/i.test(button.text)) return false;
        if (/btn-outline|btn-secondary|btn-ghost|var-contorno|var-fantasma|limpiar/.test(button.className)) return false;
        return /rgb\(255, 255, 255\)|rgba\(0, 0, 0, 0\)/.test(button.background);
      });

      return {
        textSample: text.replace(/\s+/g, ' ').slice(0, 1000),
        blankPage: text.length < 30,
        loading: /Cargando|Loading|procesando/i.test(text) || Array.from(root.querySelectorAll('[class*="spinner"], [class*="loading"], mat-spinner')).some(visible),
        gridVisible: !!root.querySelector('ui-grilla-empresarial, ag-grid-angular, .ag-root'),
        emptyVisible: /No hay devoluciones registradas|No hay transacciones|Sin transacciones|Sin resultados/i.test(text),
        errorVisible: /No fue posible|No se pudo|Error|No autorizado/i.test(text),
        horizontalScroll: document.body.scrollWidth > document.documentElement.clientWidth + 16,
        whiteCriticalButtons
      };
    });

    Object.assign(result, {
      loadingCleared: !visual.loading,
      gridVisible: visual.gridVisible,
      emptyVisible: visual.emptyVisible,
      errorVisible: visual.errorVisible,
      blankPage: visual.blankPage,
      horizontalScroll: visual.horizontalScroll,
      whiteCriticalButtons: visual.whiteCriticalButtons,
      textSample: visual.textSample
    });

    await page.screenshot({ path: screenshotPath, fullPage: true });

    const relevantFailedRequests = result.failedRequests.filter((item) => !isIgnorableFailure(item));
    if (!result.loadingCleared) result.errors.push('La pantalla conserva loading/spinner despues del timeout.');
    if (result.blankPage) result.errors.push('La pantalla quedo en blanco.');
    if (!result.gridVisible && !result.emptyVisible && !result.errorVisible) result.errors.push('No hay grilla, estado vacio ni error funcional visible.');
    if (result.horizontalScroll) result.errors.push('Hay scroll horizontal critico.');
    if (result.whiteCriticalButtons.length) result.errors.push('Hay botones criticos blancos o de bajo contraste.');
    if (relevantFailedRequests.length) result.errors.push('Hay failed requests no manejados.');
    if (result.consoleErrors.length) result.errors.push('Hay console errors criticos.');
    result.ok = result.errors.length === 0;
  } finally {
    await browser.close();
  }

  await writeFile(jsonPath, JSON.stringify(result, null, 2), 'utf8');
  await writeFile(mdPath, [
    '# Validacion /transactions/returns',
    '',
    `Fecha: ${result.generatedAt}`,
    `Base URL: ${baseUrl}`,
    '',
    `- OK: ${result.ok ? 'SI' : 'NO'}`,
    `- Loading finalizado: ${result.loadingCleared ? 'SI' : 'NO'}`,
    `- Grilla visible: ${result.gridVisible ? 'SI' : 'NO'}`,
    `- Estado vacio visible: ${result.emptyVisible ? 'SI' : 'NO'}`,
    `- Error funcional visible: ${result.errorVisible ? 'SI' : 'NO'}`,
    `- Console errors: ${result.consoleErrors.length}`,
    `- Failed requests: ${result.failedRequests.filter((item) => !isIgnorableFailure(item)).length}`,
    `- Screenshot: ${result.screenshotPath}`,
    '',
    '## Errores',
    '',
    result.errors.length ? result.errors.map((error) => `- ${error}`).join('\n') : '- Sin errores bloqueantes.',
    '',
    'Productivo: NO-GO.'
  ].join('\n'), 'utf8');

  console.log(JSON.stringify({
    ok: result.ok,
    outputJson: path.relative(repoRoot, jsonPath).replace(/\\/g, '/'),
    outputMarkdown: path.relative(repoRoot, mdPath).replace(/\\/g, '/'),
    screenshot: result.screenshotPath,
    errors: result.errors
  }, null, 2));

  if (!result.ok) {
    process.exitCode = 1;
  }
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
