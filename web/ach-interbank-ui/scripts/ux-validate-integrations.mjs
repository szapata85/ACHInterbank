import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';
import { chromium } from 'playwright';

const __dirname = path.dirname(fileURLToPath(import.meta.url));
const repoRoot = path.resolve(__dirname, '../../..');
const evidenceDir = path.join(repoRoot, 'docs/ux/evidencias');
const baseUrl = process.env.ACH_UAT_BASE_URL || 'http://localhost:743';
const username = process.env.ACH_UAT_DEMO_USERNAME || 'admin';
const password = process.env.ACH_UAT_DEMO_PASSWORD;

const result = {
  generatedAt: new Date().toISOString(),
  baseUrl,
  ok: false,
  screenshots: {
    soapSettings: 'docs/ux/evidencias/soap-settings-after.png',
    integrationMappings: 'docs/ux/evidencias/integration-mappings-after.png'
  },
  checks: [],
  errors: [],
  consoleErrors: [],
  consoleWarnings: []
};

function addCheck(name, ok, detail = '') {
  result.checks.push({ name, ok, detail });
  if (!ok) {
    result.errors.push(`${name}${detail ? `: ${detail}` : ''}`);
  }
}

async function assertNoHorizontalScroll(page, name) {
  const metrics = await page.evaluate(() => ({
    bodyScrollWidth: document.body.scrollWidth,
    viewportWidth: document.documentElement.clientWidth
  }));
  const ok = metrics.bodyScrollWidth <= metrics.viewportWidth + 2;
  addCheck(name, ok, `${metrics.bodyScrollWidth}px body / ${metrics.viewportWidth}px viewport`);
}

async function assertVisibleButtonsInside(page, cardSelector, buttonSelector, name) {
  const checks = await page.$$eval(cardSelector, (cards, selector) => cards.map((card) => {
    const cardRect = card.getBoundingClientRect();
    return Array.from(card.querySelectorAll(selector)).map((button) => {
      const rect = button.getBoundingClientRect();
      return {
        text: button.textContent?.trim() ?? '',
        visible: rect.width > 0 && rect.height > 0,
        inside:
          rect.left >= cardRect.left - 1 &&
          rect.right <= cardRect.right + 1 &&
          rect.top >= cardRect.top - 1 &&
          rect.bottom <= cardRect.bottom + 1
      };
    });
  }), buttonSelector);

  const flat = checks.flat();
  const ok = flat.length > 0 && flat.every((item) => item.visible && item.inside);
  addCheck(name, ok, JSON.stringify(flat));
}

async function assertNoCriticalOverlap(page, selector, name) {
  const overlaps = await page.$$eval(selector, (items) => {
    const rects = items.map((item) => ({
      text: item.textContent?.trim() ?? '',
      rect: item.getBoundingClientRect()
    }));
    const invalid = [];
    for (let i = 0; i < rects.length; i += 1) {
      for (let j = i + 1; j < rects.length; j += 1) {
        const a = rects[i].rect;
        const b = rects[j].rect;
        const overlapX = Math.max(0, Math.min(a.right, b.right) - Math.max(a.left, b.left));
        const overlapY = Math.max(0, Math.min(a.bottom, b.bottom) - Math.max(a.top, b.top));
        if (overlapX > 2 && overlapY > 2) {
          invalid.push({ a: rects[i].text, b: rects[j].text });
        }
      }
    }
    return invalid;
  });
  addCheck(name, overlaps.length === 0, JSON.stringify(overlaps));
}

async function login(page) {
  if (!password) {
    throw new Error('ACH_UAT_DEMO_PASSWORD no esta definido; no se puede ejecutar validacion visual autenticada.');
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

  await page.addInitScript((accessToken) => {
    window.sessionStorage.setItem('ach.interbank.access_token', accessToken);
  }, token);
}

async function validateSoapSettings(page) {
  await page.goto(`${baseUrl}/integraciones/soap-settings`, { waitUntil: 'networkidle' });
  await page.waitForSelector('[data-testid="soap-settings-page"]', { timeout: 20000 });

  addCheck(
    'soap-settings no usa tabla principal problematica',
    (await page.locator('.desktop-table').count()) === 0
  );
  addCheck('soap-settings tiene cards', (await page.locator('[data-testid="soap-service-card"]').count()) > 0);
  await assertVisibleButtonsInside(
    page,
    '[data-testid="soap-service-card"]',
    '[data-testid="soap-service-detail-button"],[data-testid="soap-service-edit-button"],[data-testid="soap-service-test-button"]',
    'soap-settings botones visibles dentro de card'
  );
  await assertNoHorizontalScroll(page, 'soap-settings sin scroll horizontal');
  await assertNoCriticalOverlap(page, '[data-ux-critical]', 'soap-settings sin superposicion critica');

  const endpointCss = await page.locator('[data-testid="soap-service-endpoint-preview"]').first().evaluate((node) => {
    const style = window.getComputedStyle(node);
    const rect = node.getBoundingClientRect();
    const parentRect = node.parentElement?.getBoundingClientRect();
    return {
      overflow: style.overflow,
      textOverflow: style.textOverflow,
      withinParent: parentRect ? rect.right <= parentRect.right + 1 : false
    };
  });
  addCheck(
    'soap-settings endpoint truncado en card',
    endpointCss.textOverflow === 'ellipsis' && endpointCss.withinParent,
    JSON.stringify(endpointCss)
  );

  await page.locator('[data-testid="soap-service-detail-button"]').first().click();
  await page.waitForSelector('aside[aria-label="Detalle SOAP"]');
  addCheck(
    'soap-settings endpoint completo solo en detalle',
    await page.locator('aside[aria-label="Detalle SOAP"] .breakable').first().isVisible()
  );
  await page.screenshot({ path: path.join(evidenceDir, 'soap-settings-after.png'), fullPage: true });
}

async function validateMappings(page) {
  await page.goto(`${baseUrl}/integraciones/mappings`, { waitUntil: 'networkidle' });
  await page.waitForSelector('[data-testid="integration-mappings-page"]', { timeout: 20000 });

  const options = await page.locator('[data-testid="integration-select"] option').allTextContents();
  const hasWsAxon = options.some((item) => item.includes('WsAxonRespuestaTransaccionesSoapClient'));
  addCheck('mappings dropdown carga opciones', options.length > 0, JSON.stringify(options));
  addCheck('mappings muestra WsAxonRespuestaTransaccionesSoapClient', hasWsAxon, JSON.stringify(options));

  if (hasWsAxon) {
    const value = await page.locator('[data-testid="integration-select"] option', {
      hasText: 'WsAxonRespuestaTransaccionesSoapClient'
    }).first().getAttribute('value');
    await page.locator('[data-testid="integration-select"]').selectOption(value ?? '');
    await page.waitForLoadState('networkidle');
  }

  addCheck(
    'mappings muestra cards o estado vacio claro',
    (await page.locator('[data-testid="mapping-card"]').count()) > 0 ||
      (await page.locator('[data-testid="empty-mappings-state"]').count()) > 0
  );
  await assertNoHorizontalScroll(page, 'mappings sin scroll horizontal');
  await assertNoCriticalOverlap(page, '[data-ux-critical]', 'mappings sin superposicion critica');

  const cardCount = await page.locator('[data-testid="mapping-card"]').count();
  if (cardCount > 0) {
    await assertVisibleButtonsInside(
      page,
      '[data-testid="mapping-card"]',
      '[data-testid="mapping-detail-button"],[data-testid="mapping-edit-button"]',
      'mappings botones visibles dentro de card'
    );
    await page.locator('[data-testid="mapping-detail-button"]').first().click();
    await page.waitForSelector('aside[aria-label="Detalle mapping"]');
    addCheck('mappings detalle abre modal read-only', true);
  }

  await page.screenshot({ path: path.join(evidenceDir, 'integration-mappings-after.png'), fullPage: true });
}

async function main() {
  await mkdir(evidenceDir, { recursive: true });
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1366, height: 768 } });
  page.on('console', (message) => {
    if (message.type() === 'error') {
      result.consoleErrors.push(message.text());
    }
    if (message.type() === 'warning') {
      result.consoleWarnings.push(message.text());
    }
  });

  try {
    await login(page);
    await validateSoapSettings(page);
    await validateMappings(page);
    addCheck('console errors = 0', result.consoleErrors.length === 0, JSON.stringify(result.consoleErrors));
    addCheck('console warnings UX relevantes = 0', result.consoleWarnings.length === 0, JSON.stringify(result.consoleWarnings));
    result.ok = result.errors.length === 0;
  } catch (error) {
    result.errors.push(error instanceof Error ? error.message : String(error));
    result.ok = false;
  } finally {
    await browser.close();
    await writeFile(
      path.join(evidenceDir, 'ux-validation-integrations.json'),
      JSON.stringify(result, null, 2),
      'utf8'
    );
  }

  if (!result.ok) {
    console.error(JSON.stringify(result, null, 2));
    process.exit(1);
  }

  console.log(JSON.stringify(result, null, 2));
}

await main();
