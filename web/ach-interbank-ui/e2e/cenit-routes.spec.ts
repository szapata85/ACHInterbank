import { expect, Page, test } from '@playwright/test';

type CenitRoute = {
  id: string;
  path: string;
  expectedApi: RegExp;
  heading: RegExp;
  marker: RegExp;
  emptyText: RegExp;
};

type RuntimeMode = 'real' | 'fallback';

const refreshEndpoint = /\/auth\/refresh$/;
const apiPattern = /\/api\//;
const auxiliaryLayoutEndpoint = /\/api\/users\/branding(?:\?.*)?$/;
const auxiliaryNavigationLogEndpoint = /\/api\/navigation-logs(?:\?.*)?$/;
const uiCandidates = unique([
  process.env['ACH_UI_URL'],
  'http://localhost:743',
  'http://localhost:4200'
]);
const apiCandidates = unique([process.env['ACH_API_URL'], 'http://localhost:843']);
const loginUser = process.env['ACH_USER'] ?? 'admin';
const loginPass = process.env['ACH_PASS'] ?? 'Admin123!';

const routes: CenitRoute[] = [
  {
    id: 'cenit-regulatorio-causales-devolucion',
    path: '/cenit/regulatorio/causales-devolucion',
    expectedApi: /\/api\/regulatory-catalogs\/return-codes(?:\?|$)/,
    heading: /Causales de devolución \(Rxx\)/i,
    marker: /Consulta de causal, aplicabilidad y vigencia normativa\./i,
    emptyText: /No hay causales de devolución CENIT disponibles para los filtros aplicados\./i
  },
  {
    id: 'cenit-regulatorio-causales-rechazo',
    path: '/cenit/regulatorio/causales-rechazo',
    expectedApi: /\/api\/regulatory-catalogs\/file-rejection-codes(?:\?|$)/,
    heading: /Causales de rechazo \(Dxx\)/i,
    marker: /Consulta por severidad, etapa y reintento permitido\./i,
    emptyText: /No hay causales de rechazo CENIT disponibles para los filtros aplicados\./i
  },
  {
    id: 'cenit-regulatorio-politicas-transaccion',
    path: '/cenit/regulatorio/politicas-transaccion',
    expectedApi: /\/api\/regulatory-catalogs\/transaction-type-policies(?:\?|$)/,
    heading: /Políticas de tipo de transacción/i,
    marker: /Alinee la configuración de productos con las reglas vigentes de CENIT\./i,
    emptyText: /No hay políticas de transacción CENIT disponibles para los filtros aplicados\./i
  },
  {
    id: 'cenit-operacion-ciclos',
    path: '/cenit/operacion/ciclos',
    expectedApi: /\/api\/reports\/cycles(?:\?|$)/,
    heading: /Ciclos del día/i,
    marker: /Supervise el avance del día operacional y detecte desbalances de forma temprana\./i,
    emptyText: /No hay ciclos CENIT para los filtros aplicados\./i
  },
  {
    id: 'cenit-operacion-cola',
    path: '/cenit/operacion/cola',
    expectedApi: /\/api\/cenit\/queues(?:\?|$)/,
    heading: /Cola y transacciones diferidas/i,
    marker: /Priorice transacciones en riesgo y reduzca acumulación operacional\./i,
    emptyText: /No hay transacciones en cola CENIT para los filtros aplicados\./i
  },
  {
    id: 'cenit-operacion-neteo',
    path: '/cenit/operacion/neteo',
    expectedApi: /\/api\/cenit\/net-positions(?:\?|$)/,
    heading: /Posiciones netas por entidad/i,
    marker: /Liquidez simulada para evaluación interna\. No representa saldo real CUD ni liquidación firme\./i,
    emptyText: /No hay posiciones netas CENIT registradas para la ejecución consultada\./i
  },
  {
    id: 'cenit-operacion-optimizacion',
    path: '/cenit/operacion/optimizacion',
    expectedApi: /\/api\/cenit\/optimization-decisions(?:\?|$)/,
    heading: /Decisiones de optimización/i,
    marker: /Analice decisiones internas de liquidez\. DXX-LIQ es causal interna y no representa rechazo oficial CUD\./i,
    emptyText: /No hay decisiones de optimización CENIT registradas para los filtros aplicados\./i
  },
  {
    id: 'cenit-operacion-devoluciones',
    path: '/cenit/operacion/devoluciones',
    expectedApi: /\/api\/reports\/returns(?:\?|$)/,
    heading: /Devoluciones operativas/i,
    marker: /Detecte patrones de devolución y reduzca reprocesos\./i,
    emptyText: /No hay devoluciones operativas CENIT para los filtros aplicados\./i
  },
  {
    id: 'cenit-operacion-trazabilidad',
    path: '/cenit/operacion/trazabilidad',
    expectedApi: /\/api\/cenit\/traceability(?:\?|$)/,
    heading: /Trazabilidad operativa CENIT\/ACH/i,
    marker: /Evidencia detallada para auditoría operativa y regulatoria\./i,
    emptyText: /No hay eventos de trazabilidad CENIT\/ACH para los filtros aplicados\./i
  }
];

test.use({ ignoreHTTPSErrors: true });

test.describe('CENIT routes render with API evidence', () => {
  let runtime: { mode: RuntimeMode; uiBaseUrl: string; apiBaseUrl: string; token: string } | undefined;

  test.beforeAll(async () => {
    runtime = await resolveRuntime();
  });

  test.beforeEach(async ({ page }) => {
    if (!runtime) {
      throw new Error('Runtime CENIT no inicializado.');
    }

    await authenticate(page, runtime);
  });

  for (const route of routes) {
    test(`CenitRoute_ShouldRender_${route.id}`, async ({ page }, testInfo) => {
      if (!runtime) {
        throw new Error('Runtime CENIT no inicializado.');
      }

      const consoleErrors: string[] = [];
      const apiRequests: string[] = [];
      const failedRequests: string[] = [];
      const htmlJsResponses: string[] = [];

      page.on('console', message => {
        if (message.type() === 'error') {
          const text = message.text();
          if (!isBenignConsoleError(text)) {
            consoleErrors.push(text);
          }
        }
      });

      page.on('requestfailed', request => {
        if (isExpectedCenitRequest(request.url(), route) || isCriticalAsset(request.url())) {
          failedRequests.push(`${request.method()} ${request.url()} ${request.failure()?.errorText ?? ''}`.trim());
        }
      });

      page.on('request', request => {
        if (isExpectedCenitRequest(request.url(), route)) {
          apiRequests.push(`${request.method()} ${request.url()}`);
        }
      });

      page.on('response', response => {
        const url = response.url();
        if (!url.endsWith('.js')) {
          return;
        }

        const contentType = response.headers()['content-type'] ?? '';
        if (contentType.includes('text/html')) {
          htmlJsResponses.push(`${response.status()} ${url} ${contentType}`);
        }
      });

      if (runtime.mode === 'fallback') {
        await mockCenitApi(page);
      }

      await mockAuxiliaryLayoutEndpoints(page);

      await page.goto(joinUrl(runtime.uiBaseUrl, route.path));

      await expect(page.getByRole('heading', { name: route.heading })).toBeVisible();
      await expect(page.locator('ui-encabezado-pagina')).toBeVisible();
      await expect(page.locator('ui-grilla-empresarial')).toBeVisible();
      await expect(page.getByText(route.marker)).toBeVisible();

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
      await testInfo.attach(`${route.id}-html-js-responses.json`, {
        body: JSON.stringify(htmlJsResponses, null, 2),
        contentType: 'application/json'
      });

      expect(apiRequests.some(request => route.expectedApi.test(request))).toBeTruthy();
      expect(failedRequests).toEqual([]);
      expect(htmlJsResponses).toEqual([]);
      expect(consoleErrors).toEqual([]);
    });
  }

  test('LegacyRoutes_ShouldEndInNotFound', async ({ page }) => {
    if (!runtime) {
      throw new Error('Runtime CENIT no inicializado.');
    }

    const legacyRoutes = [
      '/ach-cycles/nacha/layouts',
      '/ach-cycles/nacha/definitions',
      '/nacha-layouts',
      '/nacha-record-definitions'
    ];

    for (const path of legacyRoutes) {
      await page.goto(joinUrl(runtime.uiBaseUrl, path));
      const currentUrl = page.url();
      if (currentUrl.endsWith('/not-found')) {
        await expect(page.getByText('404', { exact: true })).toBeVisible();
      } else {
        await expect(page.locator('body')).toBeEmpty();
        await expect(page.locator('ui-encabezado-pagina')).toHaveCount(0);
      }
    }
  });
});

async function resolveRuntime(): Promise<{ mode: RuntimeMode; uiBaseUrl: string; apiBaseUrl: string; token: string }> {
  const uiBaseUrl = await firstReachableUrl(uiCandidates, '/', 5_000);
  if (!uiBaseUrl) {
    throw new Error(`No fue posible detectar un runtime UI CENIT. Candidatos probados: ${uiCandidates.join(', ')}`);
  }

  const apiBaseUrl = await firstReachableUrl(apiCandidates, '/health/live', 5_000);

  if (uiBaseUrl.includes(':743') && apiBaseUrl) {
    return {
      mode: 'real',
      uiBaseUrl,
      apiBaseUrl,
      token: ''
    };
  }

  return {
    mode: 'fallback',
    uiBaseUrl: uiBaseUrl.includes(':4200') ? uiBaseUrl : 'http://localhost:4200',
    apiBaseUrl: apiBaseUrl ?? apiCandidates[0] ?? 'http://localhost:843',
    token: createUnsignedJwt({
      unique_name: 'cenit.e2e',
      name: 'Usuario CENIT E2E',
      uid: 'cenit-e2e',
      role: ['Admin', 'ACH.Operator'],
      permission: ['CanReadAch', 'CanManageAch'],
      exp: Math.floor(Date.now() / 1000) + 3600,
      iat: Math.floor(Date.now() / 1000)
    })
  };
}

async function authenticate(page: Page, runtime: { mode: RuntimeMode; apiBaseUrl: string; token: string }): Promise<void> {
  if (runtime.mode === 'real') {
    const token = await loginByApi(runtime.apiBaseUrl, loginUser, loginPass);
    await seedAuthenticatedSession(page, token);
    return;
  }

  await seedAuthenticatedSession(page, runtime.token);
  await mockAuthRefresh(page, runtime.token);
}

async function loginByApi(apiBaseUrl: string, user: string, pass: string): Promise<string> {
  const request = await fetch(joinUrl(apiBaseUrl, '/auth/login'), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username: user, password: pass }),
    signal: AbortSignal.timeout(10_000)
  });

  if (!request.ok) {
    throw new Error(`Login CENIT falló con HTTP ${request.status}`);
  }

  const payload = (await request.json()) as { data?: { token?: string }; token?: string };
  const token = payload.data?.token ?? payload.token;
  if (!token) {
    throw new Error('Login CENIT no devolvió token.');
  }

  return token;
}

async function seedAuthenticatedSession(page: Page, accessToken: string): Promise<void> {
  await page.addInitScript((token) => {
    window.sessionStorage.setItem('ach.interbank.access_token', token);
  }, accessToken);
}

async function mockAuthRefresh(page: Page, token: string): Promise<void> {
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

async function mockCenitApi(page: Page): Promise<void> {
  await mockJson(page, /\/api\/regulatory-catalogs\/return-codes(?:\?|$)/, []);
  await mockJson(page, /\/api\/regulatory-catalogs\/file-rejection-codes(?:\?|$)/, []);
  await mockJson(page, /\/api\/regulatory-catalogs\/transaction-type-policies(?:\?|$)/, []);
  await mockJson(page, /\/api\/reports\/cycles(?:\?|$)/, {
    items: [],
    totals: { totalCycles: 0, totalTransactions: 0, totalAmount: 0 },
    total: 0,
    page: 1,
    pageSize: 50
  });
  await mockJson(page, /\/api\/cenit\/queues(?:\?|$)/, { items: [] });
  await mockJson(page, /\/api\/cenit\/net-positions(?:\?|$)/, { items: [] });
  await mockJson(page, /\/api\/cenit\/optimization-decisions(?:\?|$)/, { items: [] });
  await mockJson(page, /\/api\/reports\/returns(?:\?|$)/, {
    items: [],
    totals: {
      totalCount: 0,
      totalAmount: 0
    },
    total: 0,
    page: 1,
    pageSize: 50
  });
  await mockJson(page, /\/api\/cenit\/traceability(?:\?|$)/, { items: [] });
}

async function mockAuxiliaryLayoutEndpoints(page: Page): Promise<void> {
  await page.route(auxiliaryLayoutEndpoint, async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ logoUrl: null, primaryColor: '#1d4ed8', secondaryColor: '#0f172a' })
    });
  });

  await page.route(auxiliaryNavigationLogEndpoint, async route => {
    await route.fulfill({
      status: 204,
      contentType: 'application/json',
      body: ''
    });
  });
}

async function mockJson(page: Page, pattern: RegExp, body: unknown): Promise<void> {
  await page.route(pattern, async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify(body)
    });
  });
}

function isExpectedCenitRequest(url: string, route: CenitRoute): boolean {
  return route.expectedApi.test(url);
}

function isCriticalAsset(url: string): boolean {
  return url.endsWith('.js') || url.endsWith('.css');
}

function isBenignConsoleError(text: string): boolean {
  return text.includes('net::ERR_CONNECTION_REFUSED');
}

async function firstReachableUrl(candidates: string[], path: string, timeoutMs: number): Promise<string | undefined> {
  for (const candidate of candidates) {
    if (!candidate) {
      continue;
    }

    const url = joinUrl(candidate, path);
    try {
      const response = await fetch(url, { signal: AbortSignal.timeout(timeoutMs) });
      if (response.ok) {
        return candidate.replace(/\/+$/, '');
      }
    } catch {
      // try next candidate
    }
  }

  return undefined;
}

function joinUrl(baseUrl: string, path: string): string {
  return `${baseUrl.replace(/\/+$/, '')}/${path.replace(/^\/+/, '')}`;
}

function unique(values: Array<string | undefined>): string[] {
  return [...new Set(values.filter((value): value is string => Boolean(value)))];
}

function createUnsignedJwt(payload: Record<string, unknown>): string {
  return `${base64Url({ alg: 'none', typ: 'JWT' })}.${base64Url(payload)}.e2e`;
}

function base64Url(value: Record<string, unknown>): string {
  return Buffer.from(JSON.stringify(value)).toString('base64').replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '');
}
