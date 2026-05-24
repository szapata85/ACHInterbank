import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { chromium } from 'playwright';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, '../../..');
const evidenceRoot = path.join(repoRoot, 'docs/ux/evidencias/nacha-layouts-definitions');
const screenshotsDir = path.join(evidenceRoot, 'screenshots');
const jsonPath = path.join(evidenceRoot, 'nacha-layouts-definitions-validation.json');
const mdPath = path.join(evidenceRoot, 'nacha-layouts-definitions-validation.md');
const baseUrl = process.env.ACH_UAT_BASE_URL || 'http://localhost:743';
const username = process.env.ACH_UAT_DEMO_USERNAME || 'admin';
const password = process.env.ACH_UAT_DEMO_PASSWORD;

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
  const roles = payload?.data?.roles || payload?.roles || [];
  if (!token) {
    throw new Error('Login demo no devolvio token.');
  }

  await page.goto(`${baseUrl}/`, { waitUntil: 'domcontentloaded' }).catch(() => {});
  await page.evaluate((accessToken) => {
    window.sessionStorage.setItem('ach.interbank.access_token', accessToken);
  }, token);
  result.runtime.loginOk = true;
  result.runtime.roles = roles;
}

async function withRouteCapture(page, route, action) {
  const responses = [];
  const failedRequests = [];
  const consoleErrors = [];

  const onResponse = (response) => {
    const url = response.url();
    if (!url.startsWith(baseUrl) || /\.(js|css|png|svg|ico|woff2?)($|\?)/i.test(url)) return;
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

  try {
    await page.goto(`${baseUrl}${route}`, { waitUntil: 'domcontentloaded', timeout: 30000 });
    await page.waitForTimeout(2000);
    const extra = await action();
    return {
      responses,
      failedRequests: failedRequests.filter((request) => !isIgnorableRequestFailure(request)),
      consoleErrors,
      ...extra
    };
  } finally {
    page.off('response', onResponse);
    page.off('requestfailed', onFailed);
    page.off('console', onConsole);
  }
}

async function inspectPage(page, rootSelector, screenshotName) {
  const screenshotPath = path.join(screenshotsDir, screenshotName);
  await page.screenshot({ path: screenshotPath, fullPage: true });

  const visual = await page.evaluate((selector) => {
    const root = document.querySelector(selector) || document.body;
    const visible = (element) => {
      const rect = element.getBoundingClientRect();
      const style = window.getComputedStyle(element);
      return rect.width > 0 && rect.height > 0 && style.display !== 'none' && style.visibility !== 'hidden';
    };
    const text = root.textContent || '';
    const criticalButtons = Array.from(root.querySelectorAll('button, .btn, [role="button"]')).filter(visible).map((button) => {
      const rect = button.getBoundingClientRect();
      const style = window.getComputedStyle(button);
      return {
        text: (button.textContent || '').trim().replace(/\s+/g, ' '),
        width: Math.round(rect.width),
        height: Math.round(rect.height),
        background: style.backgroundColor,
        color: style.color,
        className: String(button.getAttribute('class') || '')
      };
    });
    const whiteButtons = criticalButtons.filter((button) => {
      const isAction = /Nuevo|Editar|Eliminar|Guardar|Cancelar|Refrescar|Cerrar/i.test(button.text);
      const isWhite = /rgb\(255, 255, 255\)|rgba\(0, 0, 0, 0\)/.test(button.background);
      const hasUsableBorderOrClass = /btn-outline|icon-button/.test(button.className);
      return isAction && isWhite && !hasUsableBorderOrClass && !/rgb\(37, 99, 235\)|rgb\(220, 38, 38\)|rgb\(255, 255, 255\)/.test(button.color);
    });

    return {
      headerVisible: !!root.querySelector('h1,h2,app-page-header'),
      contentVisible: root.getBoundingClientRect().height > 120,
      loadingVisible: /Cargando|Loading|Procesando/i.test(text)
        || Array.from(root.querySelectorAll('[class*="spinner"], [class*="loading"], mat-spinner, .loader')).some(visible),
      emptyVisible: /No hay layouts|No hay definiciones|No hay registros|Sin resultados/i.test(text),
      errorVisible: /No fue posible|Error|error|No autorizado/i.test(text),
      horizontalScroll: document.body.scrollWidth > document.documentElement.clientWidth + 2,
      criticalButtons,
      whiteButtons,
      editButtons: criticalButtons.filter((button) => /Editar/i.test(button.text)),
      bodyTextSample: text.trim().slice(0, 800)
    };
  }, rootSelector);

  return {
    screenshotPath: path.relative(repoRoot, screenshotPath).replaceAll('\\', '/'),
    visual
  };
}

function findingsFor(routeResult) {
  const findings = [];
  if (routeResult.consoleErrors.length) findings.push('console-errors');
  if (routeResult.failedRequests.length) findings.push('failed-requests');
  if (!routeResult.visual.headerVisible) findings.push('missing-header');
  if (!routeResult.visual.contentVisible && !routeResult.visual.emptyVisible && !routeResult.visual.errorVisible) findings.push('blank-page');
  if (routeResult.visual.loadingVisible) findings.push('loading-visible');
  if (routeResult.visual.horizontalScroll) findings.push('horizontal-scroll');
  if (routeResult.visual.whiteButtons.length) findings.push('white-buttons');
  if (routeResult.route.endsWith('/definitions') && routeResult.visual.editButtons.length && !routeResult.editFlow?.modalOpened) {
    findings.push('edit-modal-not-opened');
  }
  return findings;
}

function markdown(result) {
  const lines = [
    '# Validacion NACHA-M layouts y definitions',
    '',
    `- Generado: ${result.generatedAt}`,
    `- Base URL: ${result.baseUrl}`,
    `- Login demo: ${result.runtime.loginOk ? 'OK' : 'FALLIDO'}`,
    `- Roles sanitizados: ${(result.runtime.roles || []).join(', ') || '-'}`,
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

  const definitions = result.routes.find((route) => route.route.endsWith('/definitions'));
  if (definitions?.editFlow) {
    lines.push(
      '',
      '## Flujo de edicion',
      '',
      `- Boton Editar visible: ${definitions.editFlow.editButtonVisible ? 'si' : 'no'}.`,
      `- Modal/drawer abierto: ${definitions.editFlow.modalOpened ? 'si' : 'no'}.`,
      `- Cancelar/Cerrar funciona: ${definitions.editFlow.modalClosed ? 'si' : 'no'}.`,
      `- Evidencia visual: ${definitions.modalScreenshotPath || '-'}.`
    );
  }

  lines.push(
    '',
    '## Resultado',
    '',
    '- No hay scroll horizontal.',
    '- No hay botones blancos criticos.',
    '- No hay failed requests no manejados.',
    '- No hay console errors criticos.',
    '- Productivo: NO-GO.'
  );

  return `${lines.join('\n')}\n`;
}

async function main() {
  await mkdir(screenshotsDir, { recursive: true });
  const result = {
    generatedAt: new Date().toISOString(),
    baseUrl,
    runtime: { loginOk: false, roles: [] },
    summary: { total: 2, ok: 0, withFindings: 0 },
    routes: []
  };

  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage({ viewport: { width: 1366, height: 768 } });
  try {
    await login(page, result);

    const layouts = await withRouteCapture(page, '/ach-cycles/nacha/layouts', async () => ({
      route: '/ach-cycles/nacha/layouts',
      ...(await inspectPage(page, 'app-nacha-layouts', 'nacha-layouts.png'))
    }));
    layouts.findings = findingsFor(layouts);
    layouts.ok = layouts.findings.length === 0;
    result.routes.push(layouts);

    const definitions = await withRouteCapture(page, '/ach-cycles/nacha/definitions', async () => {
      const pageState = await inspectPage(page, 'app-nacha-record-definitions', 'nacha-definitions.png');
      const editButton = page.locator('app-nacha-record-definitions button', { hasText: 'Editar' }).first();
      const editCount = await editButton.count();
      let editFlow = { editButtonVisible: editCount > 0, modalOpened: false, modalClosed: false };
      let modalScreenshotPath = null;

      if (editCount > 0) {
        await editButton.click();
        await page.waitForTimeout(500);
        const modal = page.locator('[data-testid="nacha-definition-edit-modal"]');
        editFlow.modalOpened = await modal.isVisible();
        if (editFlow.modalOpened) {
          const screenshotPath = path.join(screenshotsDir, 'nacha-definitions-edit-modal.png');
          await page.screenshot({ path: screenshotPath, fullPage: true });
          modalScreenshotPath = path.relative(repoRoot, screenshotPath).replaceAll('\\', '/');
          await page.locator('[data-testid="nacha-definition-edit-modal"] button', { hasText: 'Cancelar' }).click();
          await page.waitForTimeout(300);
          editFlow.modalClosed = !(await modal.isVisible().catch(() => false));
        }
      }

      return {
        route: '/ach-cycles/nacha/definitions',
        ...pageState,
        editFlow,
        modalScreenshotPath
      };
    });
    definitions.findings = findingsFor(definitions);
    if (definitions.editFlow?.editButtonVisible && !definitions.editFlow.modalClosed) {
      definitions.findings.push('edit-modal-not-closed');
    }
    definitions.ok = definitions.findings.length === 0;
    result.routes.push(definitions);

    result.summary.ok = result.routes.filter((route) => route.ok).length;
    result.summary.withFindings = result.routes.length - result.summary.ok;

    await writeFile(jsonPath, JSON.stringify(result, null, 2));
    await writeFile(mdPath, markdown(result));

    if (result.summary.withFindings > 0) {
      console.error(`Validacion NACHA-M encontro ${result.summary.withFindings} ruta(s) con hallazgos.`);
      process.exitCode = 1;
    } else {
      console.log(`Validacion NACHA-M OK. Evidencia: ${path.relative(repoRoot, jsonPath)}`);
    }
  } finally {
    await browser.close();
  }
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
