import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { chromium } from 'playwright';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, '../../..');
const evidenceRoot = path.join(repoRoot, 'docs/ux/evidencias/spa-global-audit');
const screenshotsDir = path.join(evidenceRoot, 'screenshots');
const jsonPath = path.join(evidenceRoot, 'spa-critical-routes-audit.json');
const mdPath = path.join(evidenceRoot, 'spa-critical-routes-audit.md');
const baseUrl = process.env.ACH_UAT_BASE_URL || 'http://localhost:743';
const username = process.env.ACH_UAT_DEMO_USERNAME || 'admin';
const password = process.env.ACH_UAT_DEMO_PASSWORD;

const routes = [
  '/ach-cycles/nacha/export',
  '/catalogs/financial-institutions',
  '/catalogs/bank-holidays',
  '/catalogs/document-types',
  '/catalogs/person-types',
  '/catalogs/phone-types',
  '/catalogs/email-types',
  '/catalogs/address-types',
  '/catalogs/transaction-codes',
  '/customer-third-parties',
  '/ach-cycles/nacha/layouts',
  '/ach-cycles/nacha/definitions',
  '/reports',
  '/reports/sent',
  '/reports/received',
  '/reports/returns',
  '/reports/rejections',
  '/reports/files',
  '/reports/cycles',
  '/reports/reconciliation',
  '/reports/audit',
  '/reports/history',
  '/reports/traceability'
];

const result = {
  generatedAt: new Date().toISOString(),
  baseUrl,
  auditedRoutes: routes,
  summary: {
    totalRoutes: routes.length,
    p0: 0,
    p1: 0,
    p2: 0,
    ok: 0
  },
  runtime: {
    liveStatus: null,
    readyStatus: null,
    loginOk: false
  },
  routes: []
};

function slugifyRoute(route) {
  return route.replace(/^\/+/, '').replace(/[^a-zA-Z0-9]+/g, '_').replace(/^_+|_+$/g, '') || 'root';
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

function luminance(rgb) {
  const parts = rgb.match(/\d+(\.\d+)?/g)?.slice(0, 3).map(Number) ?? [0, 0, 0];
  const [r, g, b] = parts.map((value) => {
    const c = value / 255;
    return c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4;
  });
  return 0.2126 * r + 0.7152 * g + 0.0722 * b;
}

function contrastRatio(foreground, background) {
  const l1 = luminance(foreground);
  const l2 = luminance(background);
  const light = Math.max(l1, l2);
  const dark = Math.min(l1, l2);
  return (light + 0.05) / (dark + 0.05);
}

async function getHttpStatus(pathname) {
  try {
    const response = await fetch(`${baseUrl}${pathname}`);
    return response.status;
  } catch {
    return null;
  }
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

async function inspectVisualState(page, route) {
  return page.evaluate(({ routeValue }) => {
    const bodyText = document.body.innerText || '';
    const bodyRect = document.body.getBoundingClientRect();
    const viewportWidth = document.documentElement.clientWidth;
    const loadingPatterns = [
      /cargando/i,
      /loading/i,
      /obteniendo/i,
      /procesando/i
    ];
    const hasVisibleLoadingText = loadingPatterns.some((pattern) => pattern.test(bodyText));
    const hasSpinner = Array.from(document.querySelectorAll('[class*="spinner"], [class*="loading"], mat-spinner, .loader'))
      .some((element) => {
        const rect = element.getBoundingClientRect();
        const style = window.getComputedStyle(element);
        return rect.width > 0 && rect.height > 0 && style.display !== 'none' && style.visibility !== 'hidden';
      });
    const mainText = (document.querySelector('main')?.innerText || document.body.innerText || '').trim();
    const blankScreen = mainText.length < 30 && document.querySelectorAll('form, table, ag-grid-angular, .ag-root, button, a').length < 3;

    const buttons = Array.from(document.querySelectorAll('button, ui-boton button, .btn, [role="button"]')).map((button) => {
      const rect = button.getBoundingClientRect();
      const style = window.getComputedStyle(button);
      const text = (button.textContent || '').trim().replace(/\s+/g, ' ');
      const effectiveBackground = (element) => {
        let current = element;
        while (current) {
          const currentStyle = window.getComputedStyle(current);
          const bg = currentStyle.backgroundColor;
          const rgba = bg?.match(/rgba\(\s*\d+\s*,\s*\d+\s*,\s*\d+\s*,\s*([0-9.]+)\s*\)/i);
          const alpha = rgba ? Number(rgba[1]) : 1;
          if (bg && bg !== 'transparent' && alpha >= 0.5 && !/^rgba\(\s*0\s*,\s*0\s*,\s*0\s*,\s*0\s*\)$/i.test(bg)) {
            return bg;
          }
          current = current.parentElement;
        }
        return 'rgb(255, 255, 255)';
      };
      const backgroundColor = effectiveBackground(button);
      const contrast = (() => {
        try {
          return ((foreground, background) => {
            const lum = (rgb) => {
              const parts = rgb.match(/\d+(\.\d+)?/g)?.slice(0, 3).map(Number) ?? [0, 0, 0];
              const [r, g, b] = parts.map((value) => {
                const c = value / 255;
                return c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4;
              });
              return 0.2126 * r + 0.7152 * g + 0.0722 * b;
            };
            const l1 = lum(foreground);
            const l2 = lum(background);
            return (Math.max(l1, l2) + 0.05) / (Math.min(l1, l2) + 0.05);
          })(style.color, backgroundColor);
        } catch {
          return 99;
        }
      })();
      return {
        text,
        visible: rect.width > 0 && rect.height > 0 && style.visibility !== 'hidden' && style.display !== 'none',
        width: Math.round(rect.width),
        height: Math.round(rect.height),
        contrast: Number(contrast.toFixed(2)),
        color: style.color,
        backgroundColor
      };
    }).filter((button) => button.visible && button.text.length > 0);

    const lowContrastButtons = buttons.filter((button) => button.contrast < 3);

    const gridElements = Array.from(document.querySelectorAll('ag-grid-angular, .ag-root, .ag-theme-alpine, .ag-theme-quartz'));
    const agGridState = gridElements.map((grid) => {
      const rect = grid.getBoundingClientRect();
      const cells = Array.from(grid.querySelectorAll('.ag-cell, .ag-header-cell-text')).slice(0, 20).map((cell) => {
        const style = window.getComputedStyle(cell);
        const cellRect = cell.getBoundingClientRect();
        const text = (cell.textContent || '').trim();
        return {
          text,
          width: Math.round(cellRect.width),
          height: Math.round(cellRect.height),
          color: style.color,
          backgroundColor: style.backgroundColor
        };
      });
      const unreadableCells = cells.filter((cell) => {
        if (!cell.text || cell.width <= 0 || cell.height <= 0) return false;
        const foreground = cell.color;
        const background = cell.backgroundColor === 'rgba(0, 0, 0, 0)' ? 'rgb(255, 255, 255)' : cell.backgroundColor;
        const parts = (rgb) => rgb.match(/\d+(\.\d+)?/g)?.slice(0, 3).map(Number) ?? [0, 0, 0];
        const lum = (rgb) => {
          const [r, g, b] = parts(rgb).map((value) => {
            const c = value / 255;
            return c <= 0.03928 ? c / 12.92 : ((c + 0.055) / 1.055) ** 2.4;
          });
          return 0.2126 * r + 0.7152 * g + 0.0722 * b;
        };
        const ratio = (Math.max(lum(foreground), lum(background)) + 0.05) / (Math.min(lum(foreground), lum(background)) + 0.05);
        return ratio < 3;
      });
      return {
        visible: rect.width > 0 && rect.height > 0,
        width: Math.round(rect.width),
        height: Math.round(rect.height),
        cellCount: cells.length,
        unreadableCellCount: unreadableCells.length
      };
    });

    const pdfState = (() => {
      if (!routeValue.includes('/reports/reconciliation') && !routeValue.includes('/reports/traceability')) {
        return { checked: false };
      }
      const viewers = Array.from(document.querySelectorAll('iframe, embed, object')).map((viewer) => {
        const rect = viewer.getBoundingClientRect();
        const src = viewer.getAttribute('src') || viewer.getAttribute('data') || '';
        return {
          src,
          width: Math.round(rect.width),
          height: Math.round(rect.height),
          visible: rect.width > 0 && rect.height > 0
        };
      }).filter((viewer) => /\.pdf($|\?)/i.test(viewer.src) || /pdf/i.test(viewer.src));
      return {
        checked: true,
        viewerCount: viewers.length,
        viewers,
        emptyPdfSuspected: viewers.some((viewer) => viewer.visible && (viewer.width < 50 || viewer.height < 50))
      };
    })();

    return {
      title: document.title,
      url: window.location.href,
      bodyTextLength: bodyText.trim().length,
      mainTextSample: mainText.slice(0, 500),
      hasVisibleLoadingText,
      hasSpinner,
      blankScreen,
      bodyScrollWidth: document.body.scrollWidth,
      viewportWidth,
      hasHorizontalScroll: document.body.scrollWidth > viewportWidth + 2 || bodyRect.width > viewportWidth + 2,
      lowContrastButtons,
      buttonCount: buttons.length,
      agGridState,
      pdfState
    };
  }, { routeValue: route });
}

function classifyRoute(routeResult) {
  const findings = [];

  if (routeResult.redirectedToLogin) {
    findings.push({ severity: 'P0', type: 'unauthorized', message: 'La ruta redirige a login/auth; funcionalidad principal bloqueada.' });
  }
  if (routeResult.navigationError) {
    findings.push({ severity: 'P0', type: 'navigation-error', message: routeResult.navigationError });
  }
  for (const request of routeResult.failedRequests) {
    findings.push({ severity: 'P0', type: 'failed-request', message: `${request.method} ${request.url} fallo: ${request.failure}` });
  }
  for (const response of routeResult.httpErrors) {
    const severity = [401, 403, 404, 500].includes(response.status) || response.status >= 500 ? 'P0' : 'P1';
    findings.push({ severity, type: 'http-error', message: `${response.method} ${response.url} respondio ${response.status}` });
  }
  if (routeResult.visual.blankScreen) {
    findings.push({ severity: 'P0', type: 'blank-screen', message: 'Pantalla en blanco o sin contenido principal detectable.' });
  }
  if (routeResult.visual.hasVisibleLoadingText || routeResult.visual.hasSpinner) {
    findings.push({ severity: 'P0', type: 'infinite-loading', message: 'Loading/spinner visible despues del timeout de auditoria.' });
  }
  if (routeResult.visual.pdfState?.emptyPdfSuspected) {
    findings.push({ severity: 'P0', type: 'empty-pdf', message: 'PDF embebido con dimensiones vacias o no utilizable.' });
  }
  if (routeResult.visual.lowContrastButtons.length > 0) {
    findings.push({ severity: 'P1', type: 'low-contrast-buttons', message: `${routeResult.visual.lowContrastButtons.length} botones con contraste bajo o potencialmente ilegibles.` });
  }
  if (routeResult.visual.agGridState.some((grid) => grid.visible && grid.unreadableCellCount > 0)) {
    findings.push({ severity: 'P1', type: 'ag-grid-ilegible', message: 'AG Grid con celdas/header de bajo contraste detectadas.' });
  }
  if (routeResult.visual.hasHorizontalScroll) {
    findings.push({ severity: 'P1', type: 'horizontal-scroll', message: 'Scroll horizontal detectado en el contenedor principal.' });
  }
  if (routeResult.consoleErrors.length > 0) {
    findings.push({ severity: 'P1', type: 'console-error', message: `${routeResult.consoleErrors.length} errores de consola no atribuibles a extension.` });
  }
  if (findings.length === 0 && routeResult.visual.bodyTextLength < 200) {
    findings.push({ severity: 'P2', type: 'thin-content', message: 'Contenido visible escaso; revisar si la experiencia necesita estado vacio mas claro.' });
  }

  return findings;
}

async function auditRoute(browser, route) {
  const context = await browser.newContext({ viewport: { width: 1366, height: 768 } });
  const page = await context.newPage();
  const failedRequests = [];
  const ignoredFailedRequests = [];
  const httpErrors = [];
  const consoleErrors = [];
  const ignoredConsoleErrors = [];
  const screenshotName = `${slugifyRoute(route)}.png`;
  const screenshotPath = path.join(screenshotsDir, screenshotName);

  page.on('requestfailed', (request) => {
    const item = {
      method: request.method(),
      url: request.url(),
      failure: request.failure()?.errorText ?? 'unknown'
    };
    if (isIgnorableFailure(item)) {
      ignoredFailedRequests.push(item);
    } else {
      failedRequests.push(item);
    }
  });

  page.on('response', (response) => {
    const request = response.request();
    const status = response.status();
    if (status >= 400 && ['xhr', 'fetch', 'document'].includes(request.resourceType())) {
      httpErrors.push({
        method: request.method(),
        url: response.url(),
        status
      });
    }
  });

  page.on('console', (message) => {
    if (message.type() !== 'error') return;
    const text = message.text();
    if (isExtensionNoise(text)) {
      ignoredConsoleErrors.push(text);
    } else {
      consoleErrors.push(text);
    }
  });

  let navigationError = null;
  try {
    await loginAndSeedSession(page);
    await page.goto(`${baseUrl}${route}`, { waitUntil: 'domcontentloaded', timeout: 30000 });
    await page.waitForLoadState('networkidle', { timeout: 15000 }).catch(() => {});
    await page.waitForTimeout(2500);
  } catch (error) {
    navigationError = error instanceof Error ? error.message : String(error);
  }

  const visual = await inspectVisualState(page, route).catch((error) => ({
    title: '',
    url: page.url(),
    bodyTextLength: 0,
    mainTextSample: '',
    hasVisibleLoadingText: false,
    hasSpinner: false,
    blankScreen: true,
    bodyScrollWidth: 0,
    viewportWidth: 0,
    hasHorizontalScroll: false,
    lowContrastButtons: [],
    buttonCount: 0,
    agGridState: [],
    pdfState: { checked: route.includes('/reports/reconciliation') || route.includes('/reports/traceability'), error: error.message }
  }));

  await page.screenshot({ path: screenshotPath, fullPage: true }).catch(() => {});
  const redirectedToLogin = /\/auth\/login|\/login/i.test(visual.url);

  const routeResult = {
    route,
    finalUrl: visual.url,
    screenshot: `docs/ux/evidencias/spa-global-audit/screenshots/${screenshotName}`,
    navigationError,
    redirectedToLogin,
    failedRequests,
    ignoredFailedRequests,
    httpErrors,
    consoleErrors,
    ignoredConsoleErrors,
    visual,
    findings: []
  };
  routeResult.findings = classifyRoute(routeResult);

  await context.close();
  return routeResult;
}

function renderMarkdown() {
  const lines = [];
  lines.push('# Auditoria SPA rutas criticas');
  lines.push('');
  lines.push(`Fecha: ${result.generatedAt}`);
  lines.push(`Base URL: ${result.baseUrl}`);
  lines.push('');
  lines.push('## Runtime');
  lines.push('');
  lines.push(`- /health/live: ${result.runtime.liveStatus}`);
  lines.push(`- /health/ready: ${result.runtime.readyStatus}`);
  lines.push(`- Login demo: ${result.runtime.loginOk ? 'OK' : 'FALLIDO'}`);
  lines.push('- Productivo: NO-GO');
  lines.push('');
  lines.push('## Resumen');
  lines.push('');
  lines.push(`- Rutas auditadas: ${result.summary.totalRoutes}`);
  lines.push(`- OK: ${result.summary.ok}`);
  lines.push(`- P0: ${result.summary.p0}`);
  lines.push(`- P1: ${result.summary.p1}`);
  lines.push(`- P2: ${result.summary.p2}`);
  lines.push('');
  lines.push('## Hallazgos por ruta');
  lines.push('');
  lines.push('| Ruta | Estado | Hallazgos | Screenshot |');
  lines.push('|---|---|---|---|');
  for (const route of result.routes) {
    const highest = route.findings.find((finding) => finding.severity === 'P0')?.severity
      ?? route.findings.find((finding) => finding.severity === 'P1')?.severity
      ?? route.findings.find((finding) => finding.severity === 'P2')?.severity
      ?? 'OK';
    const findings = route.findings.length
      ? route.findings.map((finding) => `${finding.severity} ${finding.type}: ${finding.message}`).join('<br>')
      : 'Sin hallazgos automaticos';
    lines.push(`| \`${route.route}\` | ${highest} | ${findings.replace(/\|/g, '\\|')} | \`${route.screenshot}\` |`);
  }
  lines.push('');
  lines.push('## Orden recomendado de correccion');
  lines.push('');
  const p0 = result.routes.flatMap((route) => route.findings.filter((finding) => finding.severity === 'P0').map((finding) => ({ route: route.route, finding })));
  const p1 = result.routes.flatMap((route) => route.findings.filter((finding) => finding.severity === 'P1').map((finding) => ({ route: route.route, finding })));
  const p2 = result.routes.flatMap((route) => route.findings.filter((finding) => finding.severity === 'P2').map((finding) => ({ route: route.route, finding })));
  if (p0.length === 0 && p1.length === 0 && p2.length === 0) {
    lines.push('No hay correcciones obligatorias detectadas por la auditoria automatizada.');
  } else {
    for (const [label, items] of [['P0', p0], ['P1', p1], ['P2', p2]]) {
      if (items.length === 0) continue;
      lines.push(`### ${label}`);
      for (const item of items) {
        lines.push(`- \`${item.route}\`: ${item.finding.type} - ${item.finding.message}`);
      }
      lines.push('');
    }
  }
  lines.push('');
  lines.push('## Nota');
  lines.push('');
  lines.push('Esta auditoria no modifica backend, Angular, estilos ni reglas ACH/NACHA-M/CENIT/ROR. Productivo permanece NO-GO.');
  lines.push('');
  return lines.join('\n');
}

async function main() {
  await mkdir(screenshotsDir, { recursive: true });
  result.runtime.liveStatus = await getHttpStatus('/health/live');
  result.runtime.readyStatus = await getHttpStatus('/health/ready');

  const browser = await chromium.launch();
  try {
    for (const route of routes) {
      console.log(`Auditando ${route}`);
      const routeResult = await auditRoute(browser, route);
      result.routes.push(routeResult);
    }
  } finally {
    await browser.close();
  }

  for (const routeResult of result.routes) {
    const hasP0 = routeResult.findings.some((finding) => finding.severity === 'P0');
    const hasP1 = routeResult.findings.some((finding) => finding.severity === 'P1');
    const hasP2 = routeResult.findings.some((finding) => finding.severity === 'P2');
    if (hasP0) result.summary.p0 += routeResult.findings.filter((finding) => finding.severity === 'P0').length;
    if (hasP1) result.summary.p1 += routeResult.findings.filter((finding) => finding.severity === 'P1').length;
    if (hasP2) result.summary.p2 += routeResult.findings.filter((finding) => finding.severity === 'P2').length;
    if (!hasP0 && !hasP1 && !hasP2) result.summary.ok += 1;
  }

  await writeFile(jsonPath, JSON.stringify(result, null, 2), 'utf8');
  await writeFile(mdPath, renderMarkdown(), 'utf8');
  console.log(JSON.stringify({
    ok: true,
    outputJson: 'docs/ux/evidencias/spa-global-audit/spa-critical-routes-audit.json',
    outputMarkdown: 'docs/ux/evidencias/spa-global-audit/spa-critical-routes-audit.md',
    summary: result.summary
  }, null, 2));
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
