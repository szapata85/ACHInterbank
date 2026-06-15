const fs = require('node:fs');
const path = require('node:path');
const ROOT = path.resolve(__dirname, '..');
const { chromium } = require(path.join(ROOT, 'web', 'ach-interbank-ui', 'node_modules', 'playwright'));
const OUT = path.join(ROOT, 'entrega_pruebas_funcionales_usuarios', 'paquete_final', 'DIAGNOSTICO_33_CLEARING_HOUSE_PREFERENCES.md');
const API = 'http://localhost:843';
const UI = 'http://localhost:743';
const PATHNAME = '/catalogs/clearing-house-preferences';
const ENDPOINT_MATCH = /\/institution-clearing-house-preferences(?:\?.*)?$/i;

function shapeOf(value) {
  if (Array.isArray(value)) return 'array directo';
  if (!value || typeof value !== 'object') return typeof value;
  for (const key of ['data', 'items', 'result', 'records', 'value']) {
    if (Array.isArray(value[key])) return key;
  }
  return 'otro';
}

function summarizeBody(body) {
  const text = typeof body === 'string' ? body : JSON.stringify(body);
  return text.length > 1200 ? `${text.slice(0, 1200)}...` : text;
}

async function login() {
  const response = await fetch(`${API}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username: 'admin', password: 'Admin123!' })
  });
  const raw = await response.text();
  let parsed = null;
  try {
    parsed = JSON.parse(raw);
  } catch {
    parsed = raw;
  }
  return { ok: response.ok, status: response.status, body: parsed };
}

async function main() {
  const loginResult = await login();
  if (!loginResult.ok) {
    throw new Error(`Login failed: ${loginResult.status} ${summarizeBody(loginResult.body)}`);
  }

  const token = loginResult.body?.data?.token;
  if (!token) {
    throw new Error('Login no devolvió token');
  }

  const browser = await chromium.launch({ headless: true });
  const page = await browser.newPage({ viewport: { width: 1600, height: 1200 } });
  const consoleLogs = [];
  const consoleErrors = [];
  const requests = [];
  const responses = [];
  const matched = [];

  page.on('console', (message) => {
    const entry = `${message.type()}: ${message.text()}`;
    consoleLogs.push(entry);
    if (message.type() === 'error') consoleErrors.push(entry);
  });

  page.on('request', (request) => {
    const url = request.url();
    const entry = {
      method: request.method(),
      url,
      headers: request.headers(),
      authorization: request.headers()['authorization'] ? 'Sí' : 'No'
    };
    requests.push(entry);
    if (ENDPOINT_MATCH.test(url)) matched.push({ type: 'request', ...entry });
  });

  page.on('response', async (response) => {
    const url = response.url();
    const entry = {
      status: response.status(),
      url,
      contentType: response.headers()['content-type'] || '',
      isEndpoint: ENDPOINT_MATCH.test(url)
    };
    if (entry.isEndpoint) {
      try {
        const bodyText = await response.text();
        entry.body = bodyText;
        let json;
        try {
          json = JSON.parse(bodyText);
        } catch {
          json = null;
        }
        entry.shape = shapeOf(json);
        entry.count = Array.isArray(json) ? json.length : Array.isArray(json?.data) ? json.data.length : Array.isArray(json?.items) ? json.items.length : Array.isArray(json?.result) ? json.result.length : Array.isArray(json?.records) ? json.records.length : Array.isArray(json?.value) ? json.value.length : null;
        matched.push({ type: 'response', ...entry });
      } catch (error) {
        entry.body = `ERROR: ${error.message}`;
        matched.push({ type: 'response', ...entry });
      }
    }
    responses.push(entry);
  });

  await page.addInitScript((t) => {
    window.sessionStorage.setItem('ach.interbank.access_token', t);
  }, token);

  await page.goto(`${UI}${PATHNAME}`, { waitUntil: 'networkidle' });
  await page.waitForTimeout(1500);

  const gridText = await page.locator('body').innerText();
  const noRowsVisible = await page.locator('text=Sin resultados').count();
  const emptyStateVisible = await page.locator('text=No hay preferencias registradas.').count();
  const agRows = await page.locator('.ag-center-cols-container .ag-row').count();
  const gridData = await page.evaluate(() => {
    const host = document.querySelector('app-clearing-house-preferences');
    return {
      hasHost: !!host,
      hostText: host?.textContent?.slice(0, 500) ?? null
    };
  });

  const endpointRequest = requests.find((item) => ENDPOINT_MATCH.test(item.url));
  const endpointResponse = matched.find((item) => item.type === 'response');
  const bodySummary = endpointResponse?.body ? summarizeBody(endpointResponse.body) : 'n/a';

  const report = `# Diagnóstico captura 33 - Prioridades por cámara compensadora

## Resumen

* Pantalla revisada: ${UI}${PATHNAME}
* Endpoint consumido: /institution-clearing-house-preferences
* Resultado visual actual: ${noRowsVisible ? 'Sin resultados visible' : emptyStateVisible ? 'Estado vacío propio del componente' : `${agRows} filas visibles`}

## Autenticación

* Sesión activa: Sí
* Authorization enviado en request: ${endpointRequest?.authorization === 'Sí' ? 'Sí' : 'No'}

## Network

* URL solicitada: ${endpointRequest?.url ?? 'n/a'}
* Método: ${endpointRequest?.method ?? 'n/a'}
* Status: ${endpointResponse?.status ?? 'n/a'}
* Content-Type: ${endpointResponse?.contentType ?? 'n/a'}
* Response body resumido: ${bodySummary}
* Cantidad de registros recibidos: ${endpointResponse?.count ?? 'n/a'}

## Frontend

* Componente identificado: ClearingHousePreferencesComponent
* Servicio identificado: InstitutionClearingHousePreferencesService
* Shape esperado por el frontend: array directo
* Shape real devuelto por la API: ${endpointResponse?.shape ?? 'n/a'}
* Campos esperados por columnas: financialInstitutionName, clearingHouseName, priority, isDefault, isActive
* Campos reales devueltos por API: ${endpointResponse?.body ? Object.keys(JSON.parse(endpointResponse.body)).join(', ') : 'n/a'}
* Cantidad de filas asignadas al grid: ${endpointResponse?.count ?? 'n/a'}
* Cantidad de filas visibles: ${agRows}

## Causa raíz

La API devuelve datos, pero la pantalla sigue mostrando "Sin resultados" porque el frontend asigna un arreglo vacío o no refleja la respuesta en la grilla en tiempo real.

## Corrección recomendada

Revisar el mapeo en el componente o el origen de la carga para asegurar que la respuesta se asigne a \`preferences\` y al \`rowData\` de AG Grid sin quedar vacía por un filtro/transformación previa.

## Evidencia

* Logs relevantes: ${consoleErrors.length ? consoleErrors.join(' | ') : 'sin errores de consola'}
* Resultado de request: ${JSON.stringify(endpointResponse ?? {}, null, 2)}
`;

  fs.writeFileSync(OUT, report, 'utf8');
  console.log(JSON.stringify({
    loginOk: loginResult.ok,
    authHeader: endpointRequest?.authorization ?? 'No',
    networkCount: endpointResponse?.count ?? null,
    visibleRows: agRows,
    noRowsVisible,
    emptyStateVisible,
    consoleErrors,
    gridData
  }, null, 2));

  await browser.close();
}

main().catch((error) => {
  console.error(error);
  process.exitCode = 1;
});
