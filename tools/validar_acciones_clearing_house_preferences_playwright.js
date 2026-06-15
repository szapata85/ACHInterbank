const fs = require('node:fs');
const path = require('node:path');
const { chromium } = require(path.join(__dirname, '..', 'web', 'ach-interbank-ui', 'node_modules', 'playwright'));

const ROOT = path.resolve(__dirname, '..');
const API = 'http://localhost:843';
const UI = 'http://localhost:743';
const PAGE = '/catalogs/clearing-house-preferences';
const OUT = path.join(ROOT, 'entrega_pruebas_funcionales_usuarios', 'paquete_final', 'capturas');

async function loginToken() {
  const response = await fetch(`${API}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username: 'admin', password: 'Admin123!' })
  });
  const json = await response.json();
  return json?.data?.token;
}

async function openPage(page, token) {
  await page.addInitScript((value) => {
    window.sessionStorage.setItem('ach.interbank.access_token', value);
  }, token);
  await page.goto(`${UI}${PAGE}`, { waitUntil: 'networkidle' });
  await page.waitForTimeout(1000);
}

async function main() {
  const token = await loginToken();
  if (!token) throw new Error('Sin token de autenticacion');

  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage({ viewport: { width: 1600, height: 1200 } });
  const consoleErrors = [];
  const endpoint = [];

  page.on('console', (message) => {
    if (message.type() === 'error') {
      consoleErrors.push(message.text());
    }
  });

  page.on('request', (request) => {
    if (request.url().includes('institution-clearing-house-preferences')) {
      endpoint.push({ type: 'request', url: request.url(), method: request.method(), auth: !!request.headers()['authorization'] });
    }
  });

  page.on('response', async (response) => {
    if (response.url().includes('institution-clearing-house-preferences')) {
      endpoint.push({
        type: 'response',
        url: response.url(),
        status: response.status(),
        contentType: response.headers()['content-type'] || ''
      });
    }
  });

  await openPage(page, token);

  const rows = await page.locator('.ag-center-cols-container .ag-row').count();
  const text = await page.locator('body').innerText();
  await page.screenshot({ path: path.join(OUT, '33_clearing_house_preferences_prioridades_camara.png'), fullPage: true });

  await page.getByRole('button', { name: 'Nueva relación' }).click();
  await page.getByText('Nueva relación').waitFor({ state: 'visible', timeout: 5000 });
  await page.screenshot({ path: path.join(OUT, '33A_clearing_house_preferences_nueva_relacion.png'), fullPage: true });

  const pageEdit = await browser.newPage({ viewport: { width: 1600, height: 1200 } });
  await openPage(pageEdit, token);
  await pageEdit.getByRole('button', { name: 'Editar' }).first().click();
  await pageEdit.getByText('Editar relación').waitFor({ state: 'visible', timeout: 5000 });
  await pageEdit.screenshot({ path: path.join(OUT, '33B_clearing_house_preferences_editar_relacion.png'), fullPage: true });

  const result = {
    endpoint,
    rows,
    hasNoResults: text.includes('Sin resultados'),
    consoleErrors,
    editOpened: true,
    newOpened: true
  };
  console.log(JSON.stringify(result, null, 2));
  await browser.close();
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
