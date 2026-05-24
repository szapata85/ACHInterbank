import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { chromium } from 'playwright';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, '../../..');
const evidenceRoot = path.join(repoRoot, 'docs/ux/evidencias/spa-regression-final');
const screenshotsDir = path.join(evidenceRoot, 'screenshots');
const jsonPath = path.join(evidenceRoot, 'spa-final-regression.json');
const mdPath = path.join(evidenceRoot, 'spa-final-regression.md');
const baseUrl = process.env.ACH_UAT_BASE_URL || 'http://localhost:743';
const username = process.env.ACH_UAT_DEMO_USERNAME || 'admin';
const password = process.env.ACH_UAT_DEMO_PASSWORD;

const baseRoutes = [
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

const extraRoutes = [
  '/integraciones/soap-settings',
  '/integraciones/mappings',
  '/uat/nacha-inbound-simulator',
  '/transactions/clearing-house-rules'
];

const editorMethodCodes = [
  'WSCFAACH.Proc_Contrapartidas',
  'WSCFAACH.Proc_Transacciones',
  'WSAXON.RegistrarRespuestaTransaccion'
];

function slug(route) {
  return route.replace(/^\/+/, '').replace(/[^a-z0-9]+/gi, '_').replace(/^_+|_+$/g, '') || 'root';
}

function isExtensionNoise(text) {
  return /message channel closed before a response was received/i.test(text)
    || /asynchronous response by returning true/i.test(text);
}

function isIgnorableFailure(item) {
  return item.failure === 'net::ERR_ABORTED'
    && (/fonts\.gstatic\.com/i.test(item.url)
      || /fonts\.googleapis\.com/i.test(item.url)
      || /\/api\/users\/branding$/i.test(item.url));
}

function isIgnorableConsole(text) {
  return isExtensionNoise(text);
}

async function httpStatus(pathname) {
  try {
    const response = await fetch(`${baseUrl}${pathname}`);
    return response.status;
  } catch {
    return null;
  }
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
  return token;
}

async function discoverEditorRoutes(page, token, result) {
  const headers = { Authorization: `Bearer ${token}` };
  const discovered = [];
  const notApplicable = [];

  try {
    const methodsResponse = await page.request.get(`${baseUrl}/api/integrations/methods`, { headers });
    if (!methodsResponse.ok()) {
      for (const methodCode of editorMethodCodes) {
        notApplicable.push({ route: `/integraciones/mappings/${methodCode}/{id}`, reason: `No se pudo consultar catalogo de metodos: HTTP ${methodsResponse.status()}` });
      }
      return { discovered, notApplicable };
    }

    const methods = await methodsResponse.json();
    for (const methodCode of editorMethodCodes) {
      const method = methods.find((item) => item.code === methodCode);
      if (!method) {
        notApplicable.push({ route: `/integraciones/mappings/${methodCode}/{id}`, reason: 'Metodo no disponible en catalogo runtime.' });
        continue;
      }

      const setsResponse = await page.request.get(`${baseUrl}/api/integrations/mappingsets?methodId=${method.id}`, { headers });
      if (!setsResponse.ok()) {
        notApplicable.push({ route: `/integraciones/mappings/${methodCode}/{id}`, reason: `No se pudieron consultar mapping sets: HTTP ${setsResponse.status()}` });
        continue;
      }

      const sets = await setsResponse.json();
      const selected = (sets || []).find((item) => item.isActive) || (sets || [])[0];
      if (!selected?.id) {
        notApplicable.push({ route: `/integraciones/mappings/${methodCode}/{id}`, reason: 'No existe mapping set runtime para abrir editor.' });
        continue;
      }

      discovered.push({
        route: `/integraciones/mappings/${methodCode}/${selected.id}`,
        label: `Editor ${methodCode}`,
        methodCode,
        mappingSetId: selected.id
      });
    }
  } catch (error) {
    result.notes.push(`No se pudieron descubrir editores de mappings: ${error instanceof Error ? error.message : String(error)}`);
  }

  return { discovered, notApplicable };
}

async function inspectVisual(page, route) {
  return page.evaluate((routeValue) => {
    const root = document.querySelector('main') || document.body;
    const text = root.innerText || '';
    const visible = (element) => {
      const rect = element.getBoundingClientRect();
      const style = window.getComputedStyle(element);
      return rect.width > 0 && rect.height > 0 && style.visibility !== 'hidden' && style.display !== 'none';
    };
    const isLoading = /Cargando|Loading|obteniendo|procesando/i.test(text)
      || Array.from(root.querySelectorAll('[class*="spinner"], [class*="loading"], mat-spinner, .loader')).some(visible);
    const blank = text.trim().length < 30 && root.querySelectorAll('form, table, ag-grid-angular, .ag-root, button, a, ui-estado-vacio, ui-estado-error').length < 3;
    const errorVisible = /No fue posible|No se pudo|Error|error|No autorizado|No hay informacion para exportar|No hay información para exportar/i.test(text);
    const emptyVisible = /No hay registros|No hay layouts|No hay definiciones|No hay mappings|Sin resultados|No se encontraron/i.test(text);
    const bodyScrollWidth = document.body.scrollWidth;
    const viewportWidth = document.documentElement.clientWidth;

    const buttons = Array.from(root.querySelectorAll('button, .btn, [role="button"]')).filter(visible).map((button) => {
      const rect = button.getBoundingClientRect();
      const style = window.getComputedStyle(button);
      const textValue = (button.textContent || '').trim().replace(/\s+/g, ' ');
      const aria = button.getAttribute('aria-label') || '';
      return {
        text: textValue,
        aria,
        width: Math.round(rect.width),
        height: Math.round(rect.height),
        background: style.backgroundColor,
        color: style.color,
        className: button.getAttribute('class') || ''
      };
    });
    const buttonsWithoutLabel = buttons.filter((button) => {
      if (button.text || button.aria) return false;
      if (/ag-floating-filter|ag-button/.test(button.className)) return false;
      return button.width > 12 && button.height > 12;
    });
    const whiteButtons = buttons.filter((button) => {
      if (!/Buscar|Limpiar|Exportar|Guardar|Editar|Eliminar|Probar|Detalle|Cancelar|Cerrar|Reintentar|Nuevo|Nueva|Refrescar/i.test(button.text)) {
        return false;
      }
      if (/btn-outline|btn-secondary|btn-ghost|report-action-secondary|icon-button|var-contorno|var-fantasma|limpiar/.test(button.className)) return false;
      return /rgb\(255, 255, 255\)|rgba\(0, 0, 0, 0\)/.test(button.background)
        && !/rgb\(37, 99, 235\)|rgb\(220, 38, 38\)|rgb\(255, 255, 255\)|rgb\(185, 28, 28\)/.test(button.color);
    });

    const grids = Array.from(root.querySelectorAll('ag-grid-angular, .ag-root')).filter(visible).map((grid) => {
      const rect = grid.getBoundingClientRect();
      const headers = Array.from(grid.querySelectorAll('.ag-header-cell')).filter(visible).map((header) => {
        const headerRect = header.getBoundingClientRect();
        return {
          text: (header.textContent || '').trim().replace(/\s+/g, ' '),
          width: Math.round(headerRect.width)
        };
      });
      const narrowHeaders = headers.filter((header) => header.text && header.width < 72);
      const actionButtons = Array.from(grid.querySelectorAll('button, .btn, [role="button"]')).filter(visible).map((button) => {
        const rectButton = button.getBoundingClientRect();
        return {
          text: (button.textContent || '').trim().replace(/\s+/g, ' '),
          width: Math.round(rectButton.width),
          height: Math.round(rectButton.height)
        };
      });
      const cutActions = actionButtons.filter((button) => button.text && (button.width < 36 || button.height < 24));
      return {
        width: Math.round(rect.width),
        height: Math.round(rect.height),
        headers,
        narrowHeaders,
        cutActions
      };
    });

    const modals = Array.from(root.querySelectorAll('[role="dialog"], .modal-panel, .dialog, aside[aria-modal="true"]')).filter(visible).map((modal) => {
      const rect = modal.getBoundingClientRect();
      return {
        width: Math.round(rect.width),
        height: Math.round(rect.height),
        text: (modal.textContent || '').trim().replace(/\s+/g, ' ').slice(0, 180)
      };
    });

    const pdfState = (() => {
      const checked = routeValue.includes('/reports/reconciliation') || routeValue.includes('/reports/traceability');
      if (!checked) return { checked: false };
      const misleadingDownload = /Descarga completada|PDF generado|Exportado correctamente/i.test(text)
        && /No hay informacion para exportar|No hay información para exportar/i.test(text);
      return {
        checked: true,
        noDataMessageVisible: /No hay informacion para exportar|No hay información para exportar/i.test(text),
        misleadingDownload
      };
    })();

    return {
      url: window.location.href,
      title: (root.querySelector('h1,h2,app-page-header')?.textContent || '').trim().replace(/\s+/g, ' ').slice(0, 180),
      textLength: text.trim().length,
      textSample: text.trim().slice(0, 900),
      isLoading,
      blank,
      errorVisible,
      emptyVisible,
      horizontalScroll: bodyScrollWidth > viewportWidth + 16,
      bodyScrollWidth,
      viewportWidth,
      buttonsWithoutLabel,
      whiteButtons,
      grids,
      modals,
      pdfState
    };
  }, route);
}

function classify(routeResult) {
  const findings = [];
  if (routeResult.navigationError) findings.push({ severity: 'P0', type: 'navigation-error', message: routeResult.navigationError });
  if (routeResult.redirectedToLogin) findings.push({ severity: 'P0', type: 'redirected-login', message: 'La ruta redirigio a login tras autenticar sesion.' });
  if (routeResult.visual.blank) findings.push({ severity: 'P0', type: 'blank-screen', message: 'Pantalla en blanco o sin contenido operativo.' });
  if (routeResult.visual.isLoading) findings.push({ severity: 'P0', type: 'loading-infinito', message: 'Loading/spinner visible despues de la espera.' });
  for (const item of routeResult.httpErrors) {
    const handled = routeResult.visual.errorVisible || routeResult.visual.emptyVisible;
    if (!handled || [401, 403, 404, 500].includes(item.status)) {
      findings.push({ severity: 'P0', type: `http-${item.status}`, message: `${item.method} ${item.url}` });
    }
  }
  if (routeResult.failedRequests.length) {
    findings.push({ severity: 'P0', type: 'failed-requests', message: `${routeResult.failedRequests.length} request(s) fallidos no ignorables.` });
  }
  if (routeResult.visual.pdfState.checked && routeResult.visual.pdfState.misleadingDownload) {
    findings.push({ severity: 'P0', type: 'pdf-vacio', message: 'La ruta sugiere PDF generado mientras muestra falta de datos.' });
  }
  if (routeResult.consoleErrors.length) findings.push({ severity: 'P1', type: 'console-errors', message: `${routeResult.consoleErrors.length} error(es) de consola.` });
  if (routeResult.visual.horizontalScroll) findings.push({ severity: 'P1', type: 'horizontal-scroll', message: 'Scroll horizontal critico detectado.' });
  if (routeResult.visual.whiteButtons.length) findings.push({ severity: 'P1', type: 'white-buttons', message: `${routeResult.visual.whiteButtons.length} boton(es) criticos blancos/ilegibles.` });
  if (routeResult.visual.buttonsWithoutLabel.length) findings.push({ severity: 'P1', type: 'buttons-without-label', message: `${routeResult.visual.buttonsWithoutLabel.length} boton(es) sin texto ni aria-label.` });
  if (routeResult.visual.grids.some((grid) => grid.narrowHeaders.length || grid.cutActions.length)) {
    findings.push({ severity: 'P1', type: 'ag-grid-ux', message: 'AG Grid con columnas estrechas o acciones cortadas.' });
  }
  return findings;
}

async function auditRoute(page, routeInfo) {
  const route = typeof routeInfo === 'string' ? routeInfo : routeInfo.route;
  const label = typeof routeInfo === 'string' ? routeInfo : routeInfo.label;
  const responses = [];
  const failedRequests = [];
  const ignoredFailedRequests = [];
  const httpErrors = [];
  const consoleErrors = [];
  const ignoredConsoleErrors = [];

  const onResponse = (response) => {
    const request = response.request();
    const status = response.status();
    const url = response.url();
    if (url.startsWith(baseUrl) && !/\.(js|css|png|svg|ico|woff2?)($|\?)/i.test(url)) {
      responses.push({ method: request.method(), url, status, contentType: response.headers()['content-type'] || '' });
    }
    if (status >= 400 && ['xhr', 'fetch', 'document'].includes(request.resourceType())) {
      httpErrors.push({ method: request.method(), url, status });
    }
  };
  const onFailed = (request) => {
    const item = { method: request.method(), url: request.url(), failure: request.failure()?.errorText || 'unknown' };
    if (isIgnorableFailure(item)) ignoredFailedRequests.push(item);
    else failedRequests.push(item);
  };
  const onConsole = (message) => {
    if (message.type() !== 'error') return;
    const text = message.text();
    if (isIgnorableConsole(text)) ignoredConsoleErrors.push(text);
    else consoleErrors.push(text);
  };

  page.on('response', onResponse);
  page.on('requestfailed', onFailed);
  page.on('console', onConsole);

  let navigationError = null;
  try {
    await page.goto(`${baseUrl}${route}`, { waitUntil: 'domcontentloaded', timeout: 30000 });
    await page.waitForLoadState('networkidle', { timeout: 15000 }).catch(() => {});
    await page.waitForTimeout(1800);

    if (route.includes('/ach-cycles/nacha/definitions')) {
      const edit = page.locator('app-nacha-record-definitions button', { hasText: 'Editar' }).first();
      if (await edit.count()) {
        await edit.click();
        await page.waitForTimeout(400);
        const modalVisible = await page.locator('[data-testid="nacha-definition-edit-modal"]').isVisible().catch(() => false);
        if (modalVisible) {
          await page.locator('[data-testid="nacha-definition-edit-modal"] button', { hasText: 'Cancelar' }).click();
          await page.waitForTimeout(250);
        }
      }
    }
  } catch (error) {
    navigationError = error instanceof Error ? error.message : String(error);
  }

  const screenshotName = `${slug(route)}.png`;
  const screenshotPath = path.join(screenshotsDir, screenshotName);
  await page.screenshot({ path: screenshotPath, fullPage: true }).catch(() => {});
  const visual = await inspectVisual(page, route).catch((error) => ({
    url: page.url(),
    title: '',
    textLength: 0,
    textSample: '',
    isLoading: false,
    blank: true,
    errorVisible: false,
    emptyVisible: false,
    horizontalScroll: false,
    buttonsWithoutLabel: [],
    whiteButtons: [],
    grids: [],
    modals: [],
    pdfState: { checked: false },
    inspectError: error instanceof Error ? error.message : String(error)
  }));
  const redirectedToLogin = /\/auth\/login|\/login/i.test(visual.url);
  page.off('response', onResponse);
  page.off('requestfailed', onFailed);
  page.off('console', onConsole);

  const routeResult = {
    route,
    label,
    finalUrl: visual.url,
    screenshot: path.relative(repoRoot, screenshotPath).replaceAll('\\', '/'),
    navigationError,
    redirectedToLogin,
    responses,
    failedRequests,
    ignoredFailedRequests,
    httpErrors,
    consoleErrors,
    ignoredConsoleErrors,
    visual,
    findings: []
  };
  routeResult.findings = classify(routeResult);
  routeResult.ok = routeResult.findings.length === 0;
  return routeResult;
}

function phaseSummary(routes) {
  const byRoute = new Map(routes.map((route) => [route.route, route]));
  const routeOk = (route) => byRoute.get(route)?.ok === true;
  return {
    fase1ReportButtons: [
      '/reports/sent',
      '/reports/received',
      '/reports/returns',
      '/reports/rejections',
      '/reports/files',
      '/reports/cycles',
      '/reports/audit',
      '/reports/history'
    ].every(routeOk),
    fase2ReportsPdf: ['/reports/reconciliation', '/reports/traceability'].every(routeOk),
    fase3CatalogsAgGrid: [
      '/catalogs/financial-institutions',
      '/catalogs/bank-holidays',
      '/catalogs/document-types',
      '/catalogs/person-types',
      '/catalogs/phone-types',
      '/catalogs/email-types',
      '/catalogs/address-types',
      '/catalogs/transaction-codes',
      '/customer-third-parties'
    ].every(routeOk),
    fase4NachaLayoutsDefinitions: ['/ach-cycles/nacha/layouts', '/ach-cycles/nacha/definitions'].every(routeOk),
    integrationsMappings: ['/integraciones/soap-settings', '/integraciones/mappings'].every(routeOk)
  };
}

function markdown(result) {
  const lines = [
    '# Regresion final SPA Angular UAT',
    '',
    `Fecha: ${result.generatedAt}`,
    `Base URL: ${result.baseUrl}`,
    '',
    '## Runtime',
    '',
    `- /health/live: ${result.runtime.liveStatus}`,
    `- /health/ready: ${result.runtime.readyStatus}`,
    `- Login demo: ${result.runtime.loginOk ? 'OK' : 'FALLIDO'}`,
    `- Roles sanitizados: ${(result.runtime.roles || []).join(', ') || '-'}`,
    '- Productivo: NO-GO',
    '',
    '## Resumen',
    '',
    `- Rutas auditadas: ${result.summary.totalRoutes}`,
    `- Rutas OK: ${result.summary.ok}`,
    `- Rutas no aplica: ${result.notApplicable.length}`,
    `- P0: ${result.summary.p0}`,
    `- P1: ${result.summary.p1}`,
    `- P2: ${result.summary.p2}`,
    '',
    '## Resultado por fase',
    '',
    `- Fase 1 botones reportes: ${result.phaseSummary.fase1ReportButtons ? 'OK' : 'Revisar'}`,
    `- Fase 2 PDFs reportes: ${result.phaseSummary.fase2ReportsPdf ? 'OK' : 'Revisar'}`,
    `- Fase 3 catalogos/AG Grid: ${result.phaseSummary.fase3CatalogsAgGrid ? 'OK' : 'Revisar'}`,
    `- Fase 4 NACHA layouts/definitions: ${result.phaseSummary.fase4NachaLayoutsDefinitions ? 'OK' : 'Revisar'}`,
    `- Integraciones/mappings: ${result.phaseSummary.integrationsMappings ? 'OK' : 'Revisar'}`,
    '',
    '## Rutas auditadas',
    '',
    '| Ruta | Estado | Hallazgos | Screenshot |',
    '|---|---:|---|---|'
  ];

  for (const route of result.routes) {
    const state = route.ok ? 'OK' : route.findings.some((finding) => finding.severity === 'P0') ? 'P0'
      : route.findings.some((finding) => finding.severity === 'P1') ? 'P1' : 'P2';
    const findings = route.findings.length
      ? route.findings.map((finding) => `${finding.severity} ${finding.type}: ${finding.message}`).join('<br>')
      : '-';
    lines.push(`| \`${route.route}\` | ${state} | ${findings.replace(/\|/g, '\\|')} | \`${route.screenshot}\` |`);
  }

  if (result.notApplicable.length) {
    lines.push('', '## No aplica', '', '| Ruta | Motivo |', '|---|---|');
    for (const item of result.notApplicable) {
      lines.push(`| \`${item.route}\` | ${item.reason.replace(/\|/g, '\\|')} |`);
    }
  }

  lines.push(
    '',
    '## Conclusion',
    '',
    result.summary.p0 === 0 && result.summary.p1 === 0 && result.summary.p2 === 0
      ? 'SPA Angular queda OK tecnico UAT para las rutas auditadas.'
      : 'SPA Angular requiere revision por hallazgos pendientes.',
    '',
    'Continuar UAT controlado. Productivo NO-GO.'
  );

  return `${lines.join('\n')}\n`;
}

async function main() {
  await mkdir(screenshotsDir, { recursive: true });
  const result = {
    generatedAt: new Date().toISOString(),
    baseUrl,
    runtime: { liveStatus: await httpStatus('/health/live'), readyStatus: await httpStatus('/health/ready'), loginOk: false, roles: [] },
    summary: { totalRoutes: 0, ok: 0, p0: 0, p1: 0, p2: 0 },
    auditedRoutes: [],
    notApplicable: [],
    routes: [],
    phaseSummary: {},
    notes: []
  };

  const browser = await chromium.launch();
  const context = await browser.newContext({ viewport: { width: 1366, height: 768 } });
  const page = await context.newPage();

  try {
    const token = await login(page, result);
    const { discovered, notApplicable } = await discoverEditorRoutes(page, token, result);
    result.notApplicable.push(...notApplicable);
    const routes = [...baseRoutes, ...extraRoutes, ...discovered];
    result.auditedRoutes = routes.map((item) => typeof item === 'string' ? item : item.route);
    result.summary.totalRoutes = routes.length;

    for (const route of routes) {
      const routePath = typeof route === 'string' ? route : route.route;
      console.log(`Auditando ${routePath}`);
      result.routes.push(await auditRoute(page, route));
    }
  } finally {
    await context.close();
    await browser.close();
  }

  for (const route of result.routes) {
    if (route.ok) result.summary.ok += 1;
    result.summary.p0 += route.findings.filter((finding) => finding.severity === 'P0').length;
    result.summary.p1 += route.findings.filter((finding) => finding.severity === 'P1').length;
    result.summary.p2 += route.findings.filter((finding) => finding.severity === 'P2').length;
  }
  result.phaseSummary = phaseSummary(result.routes);

  await writeFile(jsonPath, JSON.stringify(result, null, 2), 'utf8');
  await writeFile(mdPath, markdown(result), 'utf8');

  const ok = result.summary.p0 === 0 && result.summary.p1 === 0 && result.summary.p2 === 0;
  console.log(JSON.stringify({
    ok,
    outputJson: path.relative(repoRoot, jsonPath).replaceAll('\\', '/'),
    outputMarkdown: path.relative(repoRoot, mdPath).replaceAll('\\', '/'),
    summary: result.summary
  }, null, 2));
  if (!ok) {
    process.exit(1);
  }
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
