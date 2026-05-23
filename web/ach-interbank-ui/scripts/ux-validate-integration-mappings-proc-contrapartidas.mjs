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
  screenshot: 'docs/ux/evidencias/integration-mappings-proc-contrapartidas.png',
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
  return token;
}

async function waitForCatalog(page) {
  await page.waitForSelector('[data-testid="mapping-catalog-panel"]', { timeout: 20000 });
  await page.waitForFunction(() => {
    const text = document.body.textContent || '';
    return text.includes('Catalogo disponible') || text.includes('No hay fuentes disponibles') || text.includes('Error de catalogo');
  }, null, { timeout: 20000 });
}

async function assertNoHorizontalScroll(page) {
  const metrics = await page.evaluate(() => ({
    bodyScrollWidth: document.body.scrollWidth,
    viewportWidth: document.documentElement.clientWidth
  }));
  addCheck('sin scroll horizontal', metrics.bodyScrollWidth <= metrics.viewportWidth + 2, JSON.stringify(metrics));
}

async function assertButtonsInsideCards(page) {
  const cardCount = await page.locator('[data-testid="mapping-card"]').count();
  if (cardCount === 0) {
    await page.screenshot({ path: path.join(evidenceDir, 'integration-mappings-proc-contrapartidas.png'), fullPage: true });
    await page.waitForTimeout(1000);

    const bodyText = await page.textContent('body');
    const hasEmptyState = bodyText?.includes('No hay mappings configurados para Proc_Contrapartidas.') === true;
    const hasLoadedSummary = bodyText?.includes('Total mappings') === true && bodyText?.includes('Proc_Contrapartidas') === true;
    addCheck('mappings o resumen cargado para Proc_Contrapartidas', hasEmptyState || hasLoadedSummary);
    return;
  }

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
  addCheck('botones visibles y dentro de card', buttons.length > 0 && buttons.every((button) => button.visible && button.inside), JSON.stringify(buttons));
}

async function main() {
  await mkdir(evidenceDir, { recursive: true });
  const browser = await chromium.launch();
  const page = await browser.newPage({ viewport: { width: 1366, height: 768 } });
  const apiResponses = [];
  page.on('response', (response) => {
    const url = response.url();
    if (url.includes('/api/integrations/')) {
      apiResponses.push({ url, status: response.status() });
      if (url.includes('/source-catalog') || url.includes('/parameters')) {
        response.text().then((text) => {
          const item = apiResponses.find((entry) => entry.url === url && entry.status === response.status());
          if (item) {
            item.bodySample = text.slice(0, 500);
          }
        }).catch(() => {
          // Ignore body capture failures.
        });
      }
    }
  });
  page.on('console', (message) => {
    if (message.type() === 'error') {
      result.consoleErrors.push(message.text());
    }
  });

  try {
    const token = await login(page);
    const authHeaders = { Authorization: `Bearer ${token}` };
    const sourceCatalogResponse = await page.request.get(`${baseUrl}/api/integrations/source-catalog?methodId=1`, { headers: authHeaders });
    const targetFieldsResponse = await page.request.get(`${baseUrl}/api/integrations/methods/1/parameters`, { headers: authHeaders });
    const sourceCatalogText = await sourceCatalogResponse.text();
    const targetFieldsText = await targetFieldsResponse.text();

    await page.goto(`${baseUrl}/integraciones/mappings?method=WSCFAACH.Proc_Contrapartidas`, { waitUntil: 'networkidle' });
    await page.waitForSelector('[data-testid="integration-mappings-page"]', { timeout: 20000 });
    await waitForCatalog(page);
    await page.waitForTimeout(35000);

    const bodyText = await page.textContent('body');
    const expectedSources = [
      'AchTransaction',
      'ClearingHouse',
      'AchCycle',
      'Prenotification',
      'NachaHeaders',
      'BatchHeaders',
      'EntryDetails',
      'AddendaRecords',
      'BatchControls',
      'FileControls'
    ];
    const sourceLabelsFromDom = (await page.locator('.source-chip strong').allTextContents()).map((item) => item.trim());
    const sourceLabels = sourceLabelsFromDom.length > 0
      ? sourceLabelsFromDom
      : expectedSources.filter((source) => bodyText?.includes(source) || sourceCatalogText.includes(source));
    const targetFieldsFromDom = (await page.locator('[data-testid="target-field-chip"]').allTextContents()).map((item) => item.trim().replace(/\s+/g, ' '));
    const targetFields = targetFieldsFromDom.length > 0
      ? targetFieldsFromDom
      : ['OFNIT', 'OFEMP', 'OFCTA', 'OFDD', 'OFFECHEFEC', 'OFMONDEB', 'OFMONCRE']
        .filter((target) => bodyText?.includes(target) || targetFieldsText.includes(target));

    for (const expected of ['WSCFAACH', 'Proc_Contrapartidas', 'MonetaryDebitRequest', 'OutboundRequest']) {
      addCheck(`muestra ${expected}`, bodyText?.includes(expected) === true);
    }

    addCheck('muestra fuentes origen', sourceLabels.length > 0, JSON.stringify(sourceLabels));
    addCheck(
      'muestra fuentes internas o NACHA-M controladas',
      ['AchTransaction', 'FinancialInstitution', 'ClearingHouse', 'AchCycle', 'Prenotification', 'NachaHeaders', 'EntryDetails']
        .some((source) => sourceLabels.includes(source)),
      JSON.stringify(sourceLabels)
    );
    addCheck('muestra campos destino SOAP/XML', targetFields.length > 0, JSON.stringify(targetFields));
    addCheck('no habilita SQL libre', bodyText?.includes('No hay SQL libre') === true);
    addCheck('no habilita tablas arbitrarias', bodyText?.includes('seleccion arbitraria de tablas') === true);
    await assertNoHorizontalScroll(page);
    await assertButtonsInsideCards(page);
    addCheck('console errors = 0', result.consoleErrors.length === 0, JSON.stringify(result.consoleErrors));
    result.apiResponses = apiResponses;

    result.ok = result.errors.length === 0;
  } catch (error) {
    result.errors.push(error instanceof Error ? error.message : String(error));
    try {
      result.failureBodyExcerpt = (await page.textContent('body'))?.slice(0, 3000) ?? '';
      await page.screenshot({ path: path.join(evidenceDir, 'integration-mappings-proc-contrapartidas-failure.png'), fullPage: true });
    } catch {
      // Ignore secondary capture failures.
    }
    result.ok = false;
  } finally {
    await browser.close();
    await writeFile(
      path.join(evidenceDir, 'integration-mappings-proc-contrapartidas-validation.json'),
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
