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
    nachaSources: 'docs/ux/evidencias/integration-mappings-nacha-sources.png',
    wsaxonResponse: 'docs/ux/evidencias/integration-mappings-wsaxon-response.png'
  },
  checks: [],
  errors: [],
  consoleErrors: []
};

function addCheck(name, ok, detail = '') {
  result.checks.push({ name, ok, detail });
  if (!ok) {
    result.errors.push(`${name}${detail ? `: ${detail}` : ''}`);
  }
}

async function login(page) {
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

  await page.addInitScript((accessToken) => {
    window.sessionStorage.setItem('ach.interbank.access_token', accessToken);
  }, token);
}

async function assertNoHorizontalScroll(page, name) {
  const metrics = await page.evaluate(() => ({
    bodyScrollWidth: document.body.scrollWidth,
    viewportWidth: document.documentElement.clientWidth
  }));
  addCheck(name, metrics.bodyScrollWidth <= metrics.viewportWidth + 2, JSON.stringify(metrics));
}

async function assertVisibleInsideViewport(page, selector, name) {
  const items = await page.$$eval(selector, (nodes) => nodes.map((node) => {
    const rect = node.getBoundingClientRect();
    return {
      text: node.textContent?.trim().replace(/\s+/g, ' ') ?? '',
      visible: rect.width > 0 && rect.height > 0,
      insideViewport:
        rect.left >= -1 &&
        rect.top >= -1 &&
        rect.right <= document.documentElement.clientWidth + 1 &&
        rect.bottom <= document.documentElement.clientHeight + 1
    };
  }));
  addCheck(name, items.length > 0 && items.every((item) => item.visible && item.insideViewport), JSON.stringify(items));
}

async function assertButtonsInsideCards(page, name) {
  const buttons = await page.$$eval('[data-testid="mapping-card"]', (cards) => cards.flatMap((card) => {
    const cardRect = card.getBoundingClientRect();
    return Array.from(card.querySelectorAll('[data-testid="mapping-detail-button"],[data-testid="mapping-edit-button"]')).map((button) => {
      const rect = button.getBoundingClientRect();
      return {
        text: button.textContent?.trim().replace(/\s+/g, ' ') ?? '',
        visible: rect.width > 0 && rect.height > 0,
        inside:
          rect.left >= cardRect.left - 1 &&
          rect.right <= cardRect.right + 1 &&
          rect.top >= cardRect.top - 1 &&
          rect.bottom <= cardRect.bottom + 1
      };
    });
  }));
  addCheck(name, buttons.length > 0 && buttons.every((button) => button.visible && button.inside), JSON.stringify(buttons));
}

async function selectOptionContaining(page, selector, text) {
  const value = await page.locator(`${selector} option`, { hasText: text }).first().getAttribute('value');
  if (!value) {
    throw new Error(`No se encontro opcion ${text}.`);
  }
  await page.locator(selector).selectOption(value);
  await page.waitForLoadState('networkidle');
}

async function waitForCatalog(page) {
  await page.waitForSelector('[data-testid="mapping-catalog-panel"]', { timeout: 20000 });
  await page.waitForFunction(() => {
    const text = document.body.textContent || '';
    return text.includes('Catalogo disponible') || text.includes('No hay fuentes disponibles') || text.includes('Error de catalogo');
  }, null, { timeout: 20000 });
}

async function validateNachaSources(page) {
  await page.goto('about:blank');
  await page.goto(`${baseUrl}/integraciones/mappings?method=WSCFAACH.Proc_Transacciones`, { waitUntil: 'networkidle' });
  await page.waitForSelector('[data-testid="integration-mappings-page"]', { timeout: 20000 });
  await waitForCatalog(page);
  await page.waitForTimeout(8000);
  await page.screenshot({ path: path.join(evidenceDir, 'integration-mappings-nacha-sources.png'), fullPage: true });

  const sourceLabels = (await page.locator('.source-chip strong').allTextContents()).map((item) => item.trim());
  for (const source of ['NachaHeaders', 'BatchHeaders', 'EntryDetails', 'AddendaRecords', 'BatchControls', 'FileControls']) {
    addCheck(`muestra fuente ${source}`, sourceLabels.includes(source), JSON.stringify(sourceLabels));
  }

  addCheck('muestra WSCFAACH', (await page.textContent('body'))?.includes('WSCFAACH') === true);
  addCheck('muestra Proc_Transacciones', (await page.textContent('body'))?.includes('Proc_Transacciones') === true);
  addCheck('muestra MonetaryCreditRequest', (await page.textContent('body'))?.includes('MonetaryCreditRequest') === true);
  addCheck('muestra OutboundRequest', (await page.textContent('body'))?.includes('OutboundRequest') === true);
  addCheck('no habilita SQL libre', (await page.textContent('body'))?.includes('No hay SQL libre') === true);

  await assertNoHorizontalScroll(page, 'Proc_Transacciones sin scroll horizontal');
  if ((await page.locator('[data-testid="mapping-card"]').count()) > 0) {
    await assertButtonsInsideCards(page, 'botones de mapping visibles dentro de card');
  } else {
    addCheck('muestra estado vacio claro sin mappings', (await page.locator('[data-testid="empty-mappings-state"]').count()) > 0);
  }
}

async function validateWsAxon(page) {
  await page.goto('about:blank');
  await page.goto(`${baseUrl}/integraciones/mappings?method=WSAXON.RegistrarRespuestaTransaccion`, { waitUntil: 'networkidle' });
  await page.waitForSelector('[data-testid="integration-mappings-page"]', { timeout: 20000 });
  await waitForCatalog(page);

  const bodyText = await page.textContent('body');
  addCheck('muestra WSAXON', bodyText?.includes('WSAXON') === true);
  addCheck('muestra RegistrarRespuestaTransaccion', bodyText?.includes('RegistrarRespuestaTransaccion') === true);
  addCheck('muestra DifferentialResponseNotification', bodyText?.includes('DifferentialResponseNotification') === true);
  addCheck('muestra InboundResponse', bodyText?.includes('InboundResponse') === true);
  addCheck(
    'muestra sources de respuesta diferencial o estado vacio claro',
    ['NachaHeaders', 'EntryDetails', 'AchTransaction', 'Prenotification', 'DifferentialResponse', 'No hay fuentes disponibles'].some((item) => bodyText?.includes(item))
  );
  addCheck(
    'muestra mappings o estado vacio claro',
    (await page.locator('[data-testid="mapping-card"]').count()) > 0 ||
      (await page.locator('[data-testid="empty-mappings-state"]').count()) > 0
  );

  await assertNoHorizontalScroll(page, 'WSAXON sin scroll horizontal');
  await page.screenshot({ path: path.join(evidenceDir, 'integration-mappings-wsaxon-response.png'), fullPage: true });
}

async function main() {
  await mkdir(evidenceDir, { recursive: true });
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1366, height: 768 } });
  page.on('console', (message) => {
    if (message.type() === 'error') {
      result.consoleErrors.push(message.text());
    }
  });

  try {
    await login(page);
    await validateNachaSources(page);
    addCheck('pagina integration mappings carga', true);
    await validateWsAxon(page);
    addCheck('console errors = 0', result.consoleErrors.length === 0, JSON.stringify(result.consoleErrors));
    result.ok = result.errors.length === 0;
  } catch (error) {
    result.errors.push(error instanceof Error ? error.message : String(error));
    try {
      result.failureBodyExcerpt = (await page.textContent('body'))?.slice(0, 3000) ?? '';
      await page.screenshot({ path: path.join(evidenceDir, 'integration-mappings-failure.png'), fullPage: true });
    } catch {
      // Ignore secondary capture failures.
    }
    result.ok = false;
  } finally {
    await browser.close();
    await writeFile(
      path.join(evidenceDir, 'integration-mappings-ux-validation.json'),
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
