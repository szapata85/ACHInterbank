import { expect, Page, test } from '@playwright/test';

type CenitRoute = {
  id: string;
  path: string;
  expectedApi: RegExp;
  emptyText: RegExp;
};

const refreshEndpoint = /\/auth\/refresh$/;
const apiPattern = /\/api\//;
const runCenitE2e = process.env['RUN_CENIT_E2E'] === 'true';

const routes: CenitRoute[] = [
  { id: 'cenit-regulatorio-causales-devolucion', path: '/cenit/regulatorio/causales-devolucion', expectedApi: /\/api\/regulatory-catalogs\/return-codes(?:\?|$)/, emptyText: /No hay causales de devolucion CENIT/i },
  { id: 'cenit-regulatorio-causales-rechazo', path: '/cenit/regulatorio/causales-rechazo', expectedApi: /\/api\/regulatory-catalogs\/file-rejection-codes(?:\?|$)/, emptyText: /No hay causales de rechazo CENIT/i },
  { id: 'cenit-regulatorio-politicas-transaccion', path: '/cenit/regulatorio/politicas-transaccion', expectedApi: /\/api\/regulatory-catalogs\/transaction-type-policies(?:\?|$)/, emptyText: /No hay politicas de transaccion CENIT/i },
  { id: 'cenit-operacion-ciclos', path: '/cenit/operacion/ciclos', expectedApi: /\/api\/reports\/cycles(?:\?|$)/, emptyText: /No hay ciclos CENIT/i },
  { id: 'cenit-operacion-cola', path: '/cenit/operacion/cola', expectedApi: /\/api\/cenit\/queues(?:\?|$)/, emptyText: /No hay transacciones en cola CENIT/i },
  { id: 'cenit-operacion-neteo', path: '/cenit/operacion/neteo', expectedApi: /\/api\/cenit\/net-positions(?:\?|$)/, emptyText: /No hay posiciones netas CENIT/i },
  { id: 'cenit-operacion-optimizacion', path: '/cenit/operacion/optimizacion', expectedApi: /\/api\/cenit\/optimization-decisions(?:\?|$)/, emptyText: /No hay decisiones de optimizacion CENIT/i },
  { id: 'cenit-operacion-devoluciones', path: '/cenit/operacion/devoluciones', expectedApi: /\/api\/reports\/returns(?:\?|$)/, emptyText: /No hay devoluciones operativas CENIT/i },
  { id: 'cenit-operacion-trazabilidad', path: '/cenit/operacion/trazabilidad', expectedApi: /\/api\/cenit\/traceability(?:\?|$)/, emptyText: /No hay eventos de trazabilidad CENIT\/ACH/i }
];

test.use({ ignoreHTTPSErrors: true });

test.describe('CENIT routes render with API evidence', () => {
  test.skip(!runCenitE2e, 'RUN_CENIT_E2E !== "true"; suite CENIT UAT/E2E omitida porque no hay ambiente vivo habilitado.');

  test.beforeAll(async () => {
    await validateEnvironment();
  });

  test.beforeEach(async ({ page }) => {
    await authenticate(page);
  });

  for (const route of routes) {
    test(`CenitRoute_ShouldRender_${route.id}`, async ({ page }, testInfo) => {
      const consoleErrors: string[] = [];
      const apiRequests: string[] = [];
      const failedRequests: string[] = [];
      const apiResponses: Array<{ url: string; status: number; contentType: string }> = [];

      page.on('console', message => {
        if (message.type() === 'error') {
          consoleErrors.push(message.text());
        }
      });

      page.on('requestfailed', request => {
        if (apiPattern.test(request.url())) {
          failedRequests.push(`${request.method()} ${request.url()} ${request.failure()?.errorText ?? ''}`.trim());
        }
      });

      page.on('request', request => {
        if (apiPattern.test(request.url())) {
          apiRequests.push(`${request.method()} ${request.url()}`);
        }
      });

      page.on('response', response => {
        const url = response.url();
        if (!apiPattern.test(url)) {
          return;
        }

        apiResponses.push({
          url,
          status: response.status(),
          contentType: response.headers()['content-type'] ?? ''
        });
      });

      await page.goto(route.path);

      await expect(page.locator('ui-encabezado-pagina')).toBeVisible();
      await expect(page.locator('ui-grilla-empresarial')).toBeVisible();
      await expect.poll(
        async () => {
          const rows = await page.locator('.ag-center-cols-container .ag-row').count();
          const empty = await page.getByText(route.emptyText).count();
          const error = await page.locator('ui-estado-error').count();
          return rows > 0 || empty > 0 || error > 0;
        },
        { message: `La ruta ${route.path} debe mostrar filas, vacio claro o error visible.` }
      ).toBe(true);

      await page.screenshot({ path: testInfo.outputPath(`${route.id}.png`), fullPage: true });
      await testInfo.attach(`${route.id}-api-requests.json`, {
        body: JSON.stringify(apiRequests, null, 2),
        contentType: 'application/json'
      });
      await testInfo.attach(`${route.id}-api-responses.json`, {
        body: JSON.stringify(apiResponses, null, 2),
        contentType: 'application/json'
      });

      expect(apiRequests.some(request => route.expectedApi.test(request))).toBeTruthy();
      expect(failedRequests).toEqual([]);
      expect(apiResponses.filter(response => response.status >= 400).map(response => `${response.status} ${response.url}`)).toEqual([]);
      expect(apiResponses.filter(response => response.contentType.includes('text/html')).map(response => response.url)).toEqual([]);
      expect(consoleErrors).toEqual([]);
    });
  }
});

async function validateEnvironment(): Promise<void> {
  const uiUrl = process.env['ACH_UI_URL'];
  const apiUrl = process.env['ACH_API_URL'];
  const healthUrl = process.env['ACH_API_HEALTH_URL'] || (apiUrl ? joinUrl(apiUrl, '/health/live') : '');

  if (!uiUrl) {
    throw new Error('RUN_CENIT_E2E=true requiere ACH_UI_URL apuntando a la SPA UAT/local viva.');
  }

  if (!apiUrl && !process.env['ACH_API_HEALTH_URL']) {
    throw new Error('RUN_CENIT_E2E=true requiere ACH_API_URL o ACH_API_HEALTH_URL para validar disponibilidad de API.');
  }

  try {
    const response = await fetch(healthUrl, { signal: AbortSignal.timeout(10_000) });
    if (response.status < 200 || response.status >= 300) {
      throw new Error(`HTTP ${response.status}`);
    }
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);
    throw new Error(`API CENIT no disponible en health endpoint ${healthUrl}: ${message}`);
  }
}

async function authenticate(page: Page): Promise<void> {
  const user = process.env['ACH_USER'];
  const pass = process.env['ACH_PASS'];

  if (user && pass) {
    await page.goto('/auth/login');
    await page.getByLabel(/usuario/i).fill(user);
    await page.getByLabel(/contrase/i).fill(pass);
    await page.getByRole('button', { name: /ingresar|entrar|login/i }).click();
    await page.waitForURL(url => !url.pathname.includes('/auth/login'), { timeout: 15_000 });
    return;
  }

  await seedAuthenticatedSession(page);
  await mockAuthRefresh(page);
}

function joinUrl(baseUrl: string, path: string): string {
  return `${baseUrl.replace(/\/+$/, '')}/${path.replace(/^\/+/, '')}`;
}

async function seedAuthenticatedSession(page: Page): Promise<void> {
  const token = createUnsignedJwt({
    unique_name: 'cenit.e2e',
    name: 'Usuario CENIT E2E',
    uid: 'cenit-e2e',
    role: ['Admin', 'ACH.Operator'],
    permission: ['CanReadAch', 'CanManageAch'],
    exp: Math.floor(Date.now() / 1000) + 3600,
    iat: Math.floor(Date.now() / 1000)
  });

  await page.addInitScript((accessToken) => {
    window.sessionStorage.setItem('ach.interbank.access_token', accessToken);
  }, token);
}

async function mockAuthRefresh(page: Page): Promise<void> {
  const token = createUnsignedJwt({
    unique_name: 'cenit.e2e',
    name: 'Usuario CENIT E2E',
    uid: 'cenit-e2e',
    role: ['Admin', 'ACH.Operator'],
    permission: ['CanReadAch', 'CanManageAch'],
    exp: Math.floor(Date.now() / 1000) + 3600,
    iat: Math.floor(Date.now() / 1000)
  });

  await page.route(refreshEndpoint, async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        sucess: true,
        data: {
          token,
          username: 'cenit.e2e',
          fullName: 'Usuario CENIT E2E',
          roles: ['Admin', 'ACH.Operator'],
          permissions: ['CanReadAch', 'CanManageAch']
        }
      })
    });
  });
}

function createUnsignedJwt(payload: Record<string, unknown>): string {
  return `${base64Url({ alg: 'none', typ: 'JWT' })}.${base64Url(payload)}.e2e`;
}

function base64Url(value: Record<string, unknown>): string {
  return Buffer.from(JSON.stringify(value)).toString('base64').replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '');
}
