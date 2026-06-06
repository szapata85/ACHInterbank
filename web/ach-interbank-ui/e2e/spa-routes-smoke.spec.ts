import { expect, Page, test, TestInfo } from '@playwright/test';

type SmokeRoute = {
  id: string;
  path: string;
  title: RegExp;
  marker?: RegExp;
};

const refreshEndpoint = /\/auth\/refresh$/;
const navigationEndpoint = /\/navigation\/menu$/;
const legacyRoutes = [
  '/ach-cycles/nacha/layouts',
  '/ach-cycles/nacha/definitions',
  '/nacha-layouts',
  '/nacha-record-definitions'
];

const routes: SmokeRoute[] = [
  { id: 'nacha-config-profiles', path: '/nacha-config-admin/perfiles', title: /Configuración NACHA-M/i, marker: /perfiles de configuración oficiales/i },
  { id: 'nacha-config-records', path: '/nacha-config-admin/records', title: /Registros oficiales NACHA-M/i, marker: /nacha-config profiles es la fuente oficial/i },
  { id: 'nacha-config-variants-fields', path: '/nacha-config-admin/variants-fields', title: /Variantes y campos NACHA-M/i, marker: /Espacio administrativo oficial/i },
  { id: 'nacha-config-profile-10', path: '/nacha-config-admin/perfiles/10', title: /Perfil CENIT-OUT-220/i },
  { id: 'cenit-home', path: '/cenit', title: /Centro de operación CENIT/i, marker: /Centro de operación CENIT/i },
  { id: 'cenit-regulatorio-causales-devolucion', path: '/cenit/regulatorio/causales-devolucion', title: /Causales de devoluci[oó]n/i, marker: /Causales de devoluci[oó]n \(Rxx\)/i },
  { id: 'cenit-regulatorio-causales-rechazo', path: '/cenit/regulatorio/causales-rechazo', title: /Causales de rechazo/i, marker: /Causales de rechazo \(Dxx\)/i },
  { id: 'cenit-regulatorio-politicas-transaccion', path: '/cenit/regulatorio/politicas-transaccion', title: /Pol[ií]ticas de tipo de transacci[oó]n/i, marker: /Pol[ií]ticas de tipo de transacci[oó]n/i },
  { id: 'cenit-regulatorio-politicas-devolucion', path: '/cenit/regulatorio/politicas-devolucion', title: /Pol[ií]ticas de devoluci[oó]n/i, marker: /Pol[ií]ticas de devoluci[oó]n y de devoluci[oó]n de devoluci[oó]n/i },
  { id: 'cenit-regulatorio-politicas-prenotificacion', path: '/cenit/regulatorio/politicas-prenotificacion', title: /Pol[ií]ticas de prenotificaci[oó]n/i, marker: /Pol[ií]ticas de prenotificaci[oó]n/i },
  { id: 'cenit-operacion-ciclos', path: '/cenit/operacion/ciclos', title: /Ciclos del d[ií]a/i, marker: /Monitoreo de ejecuci[oó]n/i },
  { id: 'cenit-operacion-cola', path: '/cenit/operacion/cola', title: /Cola y transacciones diferidas/i, marker: /Visibilidad de pendientes/i },
  { id: 'cenit-operacion-neteo', path: '/cenit/operacion/neteo', title: /Posiciones netas por entidad/i, marker: /Consolidado de posici[oó]n neta/i },
  { id: 'cenit-operacion-optimizacion', path: '/cenit/operacion/optimizacion', title: /Decisiones de optimizaci[oó]n/i, marker: /Trazabilidad de reglas de liquidez/i },
  { id: 'cenit-operacion-devoluciones', path: '/cenit/operacion/devoluciones', title: /Devoluciones operativas/i, marker: /Consulta de causales/i },
  { id: 'cenit-operacion-trazabilidad', path: '/cenit/operacion/trazabilidad', title: /Trazabilidad operativa CENIT\/ACH/i, marker: /Vista integral de causal/i },
  { id: 'ach-operational-dashboard', path: '/ach/nacha/operational-dashboard', title: /Consulta operativa NACHA-M/i, marker: /Fuente: demo seguro|Fuente: backend read-only/i },
  { id: 'ach-operational-file-detail', path: '/ach/nacha/operational-dashboard/files/demo-ach-in-001', title: /Detalle operativo NACHA-M/i, marker: /Detalle operativo NACHA-M/i },
  { id: 'ach-soap-uat-console', path: '/ach/nacha/soap-uat-console', title: /Consola SOAP\/UAT solo lectura/i, marker: /SOAP\/UAT/i },
  { id: 'ach-reconciliation', path: '/ach/reconciliation', title: /Consola de conciliación ACH/i, marker: /Detalle conciliación/i },
  { id: 'incoming-ingestions', path: '/incoming-nacha-command-center', title: /Command Center inbound NACHA/i, marker: /Inbound NACHA/i },
  { id: 'incoming-ingestion-detail', path: '/incoming-nacha-command-center/ingestions/ing-1', title: /Detalle de ingesta inbound NACHA/i, marker: /Detalle de ingesta/i },
  { id: 'incoming-observability', path: '/incoming-nacha-command-center/observability', title: /Observabilidad inbound NACHA/i, marker: /Observabilidad/i },
  { id: 'incoming-queue', path: '/incoming-nacha-command-center/queue', title: /Cola dispatch inbound NACHA/i, marker: /Cola dispatch/i },
  { id: 'incoming-queue-detail', path: '/incoming-nacha-command-center/queue/q-1', title: /Detalle de item de cola inbound NACHA/i, marker: /Detalle de item de cola/i },
  { id: 'reports-home', path: '/reports', title: /Reportes ACH/i, marker: /M[oó]dulo corporativo de reportes/i },
  { id: 'reports-traceability', path: '/reports/traceability', title: /Reporte de trazabilidad ACH/i, marker: /Trazabilidad ACH/i },
  { id: 'reports-sent', path: '/reports/sent', title: /Enviados/i, marker: /Transacciones enviadas/i },
  { id: 'reports-received', path: '/reports/received', title: /Recibidos/i, marker: /Transacciones recibidas/i },
  { id: 'reports-returns', path: '/reports/returns', title: /Devoluciones/i, marker: /Operaciones devueltas/i },
  { id: 'reports-rejections', path: '/reports/rejections', title: /Rechazos/i, marker: /Operaciones rechazadas/i },
  { id: 'reports-files', path: '/reports/files', title: /Archivos/i, marker: /Archivos NACHA exportados/i },
  { id: 'reports-cycles', path: '/reports/cycles', title: /Ciclos/i, marker: /Ciclos y estado operativo/i },
  { id: 'reports-audit', path: '/reports/audit', title: /Auditor[ií]a/i, marker: /Trazabilidad por usuario\/acci[oó]n/i },
  { id: 'reports-history', path: '/reports/history', title: /Hist[oó]rico/i, marker: /Eventos por rango de fechas/i },
  { id: 'reports-reconciliation', path: '/reports/reconciliation', title: /Conciliaci[oó]n/i },
  { id: 'ach-responses-list', path: '/ach-responses', title: /Command Center Respuestas ACH/i, marker: /Bandeja/i },
  { id: 'ach-responses-manual-review', path: '/ach-responses/manual-review', title: /Revisi[oó]n manual ACH/i, marker: /Revisi[oó]n manual/i },
  { id: 'ach-responses-status-mappings', path: '/ach-responses/status-mappings', title: /Homologaciones ACH/i, marker: /Homologaciones/i },
  { id: 'ach-responses-dashboard', path: '/ach-responses/dashboard', title: /Dashboard Respuestas ACH/i, marker: /Dashboard/i },
  { id: 'ach-responses-detail', path: '/ach-responses/resp-1', title: /Detalle respuesta ACH/i, marker: /Detalle respuesta/i },
  { id: 'ach-responses-attempts', path: '/ach-responses/resp-1/notification-attempts', title: /Intentos de notificaci[oó]n ACH/i, marker: /Intentos/i },
  { id: 'payment-rail-capability-registry', path: '/payment-rail-capability-registry', title: /Capability Registry multi-riel/i, marker: /Capability Registry por riel/i },
  { id: 'catalogs', path: '/catalogs', title: /Cat[aá]logos/i, marker: /Instituciones financieras/i },
  { id: 'ach-cycles-list', path: '/ach-cycles', title: /Ciclos ACH/i, marker: /Ciclos ACH/i },
  { id: 'ach-cycles-export', path: '/ach-cycles/nacha/export', title: /Exportar NACHA-M/i, marker: /Exportable/i },
  { id: 'dashboard-home', path: '/dashboard', title: /.+/ },
  { id: 'users-home', path: '/users', title: /.+/ },
  { id: 'users-branding', path: '/users/branding', title: /.+/ },
  { id: 'users-password-rules', path: '/users/password-rules', title: /.+/ },
  { id: 'users-login-lockout', path: '/users/login-lockout', title: /.+/ },
  { id: 'aliases-home', path: '/aliases', title: /.+/ },
  { id: 'customers-home', path: '/customers', title: /.+/ },
  { id: 'navigation-root', path: '/navigation', title: /.+/ },
  { id: 'navigation-menu-items', path: '/navigation/menu-items', title: /.+/ },
  { id: 'navigation-logs', path: '/navigation-logs', title: /.+/ },
  { id: 'logs-redirect', path: '/logs', title: /.+/ },
  { id: 'audit-logs', path: '/audit-logs', title: /.+/ },
  { id: 'auth-logs', path: '/auth-logs', title: /.+/ },
  { id: 'scheduler-root', path: '/scheduler', title: /.+/ },
  { id: 'scheduler-tasks', path: '/scheduler/tasks', title: /.+/ },
  { id: 'integraciones-root', path: '/integraciones', title: /.+/ },
  { id: 'soap-integrations', path: '/soap-integrations', title: /.+/ },
  { id: 'transactions-root', path: '/transactions', title: /.+/ },
  { id: 'transactions-create', path: '/transactions/create', title: /.+/ },
  { id: 'transactions-list', path: '/transactions/list', title: /.+/ },
  { id: 'transactions-nacha-upload', path: '/transactions/nacha-upload', title: /.+/ },
  { id: 'customer-third-parties', path: '/customer-third-parties', title: /.+/ },
  { id: 'transactions-returns', path: '/transactions/returns', title: /.+/ },
  { id: 'transactions-clearing-house-rules', path: '/transactions/clearing-house-rules', title: /.+/ },
  { id: 'ach-root', path: '/ach', title: /.+/ },
  { id: 'uat-root', path: '/uat', title: /.+/ },
  { id: 'uat-nacha-inbound-simulator', path: '/uat/nacha-inbound-simulator', title: /.+/ },
  { id: 'nacha-security-root', path: '/nacha-security', title: /.+/ },
  { id: 'nacha-security-dashboard', path: '/nacha-security/dashboard', title: /.+/ },
  { id: 'nacha-security-certificates', path: '/nacha-security/certificates', title: /.+/ },
  { id: 'nacha-security-sobre-digital', path: '/nacha-security/sobre-digital', title: /.+/ }
];

const omittedRoutes = [
  { path: '/ach-cycles/nacha/layouts', reason: 'Legacy route controlled by not-found only.' },
  { path: '/ach-cycles/nacha/definitions', reason: 'Legacy route controlled by not-found only.' },
  { path: '/nacha-layouts', reason: 'Legacy API route controlled by not-found only.' },
  { path: '/nacha-record-definitions', reason: 'Legacy API route controlled by not-found only.' }
];

test.use({ ignoreHTTPSErrors: true });

test.describe('SPA route smoke', () => {
  test.beforeEach(async ({ page }) => {
    await authenticate(page);
    await mockNavigation(page);
    await mockBackend(page);
  });

  test.afterEach(async ({ page }, testInfo) => {
    if (testInfo.status === testInfo.expectedStatus) {
      return;
    }

    await page.screenshot({
      path: testInfo.outputPath(`${slugify(testInfo.title)}.png`),
      fullPage: true
    });
  });

  test('RouteCoverage_ShouldReportIncludedAndOmittedRoutes', async ({ page }, testInfo) => {
    await testInfo.attach('spa-routes-coverage.json', {
      body: JSON.stringify({ included: routes.map((route) => route.path), omitted: omittedRoutes }, null, 2),
      contentType: 'application/json'
    });

    await expect(routes.length).toBeGreaterThan(0);
    await expect(omittedRoutes.length).toBeGreaterThan(0);
  });

  for (const route of routes) {
    test(`Route_ShouldRender_${route.id}`, async ({ page }, testInfo) => {
      const consoleErrors: string[] = [];
      const criticalRequestFailures: string[] = [];
      const htmlAssetResponses: string[] = [];

      page.on('console', (message) => {
        if (message.type() !== 'error') {
          return;
        }

        const text = message.text();
        if (!isBenignConsoleError(text)) {
          consoleErrors.push(text);
        }
      });

      page.on('requestfailed', (request) => {
        const url = request.url();
        if (isCriticalAssetOrApi(url)) {
          criticalRequestFailures.push(`${request.method()} ${url} ${request.failure()?.errorText ?? ''}`.trim());
        }
      });

      page.on('response', async (response) => {
        const url = response.url();
        if (!isAssetRequest(url)) {
          return;
        }

        const contentType = response.headers()['content-type'] ?? '';
        if (contentType.includes('text/html')) {
          htmlAssetResponses.push(`${response.status()} ${url} ${contentType}`);
        }
      });

      await page.goto(route.path);

      await expect(page.locator('body')).not.toHaveText(/ChunkLoadError|Application error|UnhandledPromiseRejection/i);
      const bodyLength = await page.locator('body').evaluate((node) => (node.textContent ?? '').trim().length);
      expect(bodyLength).toBeGreaterThan(0);

      if (consoleErrors.length || criticalRequestFailures.length || htmlAssetResponses.length) {
        await testInfo.attach(`${route.id}-observability.json`, {
          body: JSON.stringify({ consoleErrors, criticalRequestFailures, htmlAssetResponses }, null, 2),
          contentType: 'application/json'
        });
      }

      expect(consoleErrors).toEqual([]);
      expect(criticalRequestFailures).toEqual([]);
      expect(htmlAssetResponses).toEqual([]);
    });
  }

  test('LegacyRoutes_ShouldEndInNotFound', async ({ page }) => {
    for (const legacyRoute of legacyRoutes) {
      await page.goto(legacyRoute);
      await expect(page).toHaveURL(/\/not-found$/);
      await expect(page.getByText('404', { exact: true })).toBeVisible();
    }
  });
});

async function mockNavigation(page: Page): Promise<void> {
  await page.route(navigationEndpoint, async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        { id: 1, label: 'Panel principal', route: '/dashboard' },
        { id: 2, label: 'Usuarios', route: '/users', children: [
          { id: 21, label: 'Identidad y colores', route: '/users/branding' },
          { id: 22, label: 'Reglas de contraseña', route: '/users/password-rules' },
          { id: 23, label: 'Bloqueo de acceso', route: '/users/login-lockout' }
        ]},
        { id: 3, label: 'Integraciones', route: '/integraciones', children: [
          { id: 31, label: 'Integraciones SOAP', route: '/soap-integrations' }
        ]},
        { id: 4, label: 'CENIT', route: '/cenit', children: [
          { id: 41, label: 'Regulatorio: Devoluciones', route: '/cenit/regulatorio/causales-devolucion' },
          { id: 42, label: 'Regulatorio: Rechazos', route: '/cenit/regulatorio/causales-rechazo' },
          { id: 43, label: 'Regulatorio: Políticas', route: '/cenit/regulatorio/politicas-transaccion' },
          { id: 44, label: 'Operación: Ciclos', route: '/cenit/operacion/ciclos' },
          { id: 45, label: 'Operación: Cola', route: '/cenit/operacion/cola' },
          { id: 46, label: 'Operación: Neteo', route: '/cenit/operacion/neteo' },
          { id: 47, label: 'Operación: Optimización', route: '/cenit/operacion/optimizacion' },
          { id: 48, label: 'Operación: Devoluciones', route: '/cenit/operacion/devoluciones' },
          { id: 49, label: 'Operación: Trazabilidad', route: '/cenit/operacion/trazabilidad' }
        ]},
        { id: 5, label: 'Configuración NACHA-M', route: '/nacha-config-admin/perfiles', children: [
          { id: 51, label: 'Perfiles oficiales', route: '/nacha-config-admin/perfiles' },
          { id: 52, label: 'Registros oficiales', route: '/nacha-config-admin/records' },
          { id: 53, label: 'Variantes y campos', route: '/nacha-config-admin/variants-fields' }
        ]},
        { id: 6, label: 'Catálogos', route: '/catalogs' },
        { id: 7, label: 'Transacciones', route: '/transactions', children: [
          { id: 71, label: 'Listado', route: '/transactions/list' },
          { id: 72, label: 'Crear transacción', route: '/transactions/create' },
          { id: 73, label: 'Carga masiva', route: '/transactions/bulk-create' },
          { id: 74, label: 'Carga masiva por archivo', route: '/transactions/bulk-ingestion/upload' },
          { id: 75, label: 'Seguimiento lotes', route: '/transactions/bulk-ingestion/tracking' },
          { id: 76, label: 'Configuración de ciclos', route: '/transactions/cycle-configs' },
          { id: 77, label: 'Reglas por cámara', route: '/transactions/clearing-house-rules' },
          { id: 78, label: 'Cargar NACHA-M', route: '/transactions/nacha-upload' },
          { id: 79, label: 'Devoluciones ACH', route: '/transactions/returns' }
        ]},
        { id: 8, label: 'Navegación', route: '/navigation', children: [
          { id: 81, label: 'Menús', route: '/navigation/menu-items' }
        ]},
        { id: 9, label: 'Seguridad NACHA', route: '/nacha-security/dashboard', children: [
          { id: 91, label: 'Panel seguridad NACHA', route: '/nacha-security/dashboard' },
          { id: 92, label: 'Certificados', route: '/nacha-security/certificates' },
          { id: 93, label: 'Sobre digital', route: '/nacha-security/sobre-digital' }
        ]},
        { id: 10, label: 'Programador', route: '/scheduler', children: [
          { id: 101, label: 'Tareas programadas', route: '/scheduler/tasks' }
        ]},
        { id: 11, label: 'Logs', route: '/audit-logs', children: [
          { id: 111, label: 'Logs de auditoría', route: '/audit-logs' },
          { id: 112, label: 'Logs de autenticaciones', route: '/auth-logs' },
          { id: 113, label: 'Logs de navegación', route: '/navigation-logs' }
        ]},
        { id: 12, label: 'UAT / Simuladores', route: '/uat', children: [
          { id: 121, label: 'Simulador NACHA-M Entrada', route: '/uat/nacha-inbound-simulator' }
        ]},
        { id: 13, label: 'Respuestas ACH', route: '/ach-responses' },
        { id: 14, label: 'Reportes', route: '/reports' },
        { id: 15, label: 'Command Center inbound NACHA', route: '/incoming-nacha-command-center' },
        { id: 16, label: 'Ciclos ACH', route: '/ach-cycles' },
        { id: 17, label: 'Capability Registry', route: '/payment-rail-capability-registry' },
        { id: 18, label: 'Clientes', route: '/customers' },
        { id: 19, label: 'Alias', route: '/aliases' },
        { id: 20, label: 'Panel operativo', route: '/ach' }
      ])
    });
  });
}

async function mockAuthRefresh(page: Page): Promise<void> {
  const token = createUnsignedJwt({
    unique_name: 'spa.smoke',
    name: 'Usuario SPA Smoke',
    uid: 'spa-smoke',
    role: ['Admin', 'ACH.Operator'],
    permission: [
      'CanReadAch',
      'CanManageAch',
      'CanReadCatalogs',
      'CanManageUsers',
      'CanViewPaymentRailCapabilityRegistry',
      'CanManageCertificates',
      'CanGenerateNacha',
      'CanGenerateEncryptedNacha',
      'CanManualEncryptEnvelope',
      'CanManualDecryptEnvelope',
      'CanViewNachaSecurityAudit',
      'CanRunInteroperabilityHarness'
    ],
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
          username: 'spa.smoke',
          fullName: 'Usuario SPA Smoke',
          roles: ['Admin', 'ACH.Operator'],
          permissions: [
            'CanReadAch',
            'CanManageAch',
            'CanReadCatalogs',
            'CanManageUsers',
            'CanViewPaymentRailCapabilityRegistry',
            'CanManageCertificates',
            'CanGenerateNacha',
            'CanGenerateEncryptedNacha',
            'CanManualEncryptEnvelope',
            'CanManualDecryptEnvelope',
            'CanViewNachaSecurityAudit',
            'CanRunInteroperabilityHarness'
          ]
        }
      })
    });
  });
}

async function authenticate(page: Page): Promise<void> {
  const apiBaseUrl = process.env['ACH_API_URL'] ?? '';
  const user = process.env['ACH_USER'] ?? 'admin';
  const pass = process.env['ACH_PASS'] ?? 'Admin123!';

  if (apiBaseUrl) {
    const token = await loginByApi(apiBaseUrl, user, pass);
    await seedAuthenticatedSession(page, token);
    await mockAuthRefresh(page);
    return;
  }

  await seedAuthenticatedSession(page);
  await mockAuthRefresh(page);
}

async function seedAuthenticatedSession(page: Page): Promise<void> {
  const token = createUnsignedJwt({
    unique_name: 'spa.smoke',
    name: 'Usuario SPA Smoke',
    uid: 'spa-smoke',
    role: ['Admin', 'ACH.Operator'],
    permission: [
      'CanReadAch',
      'CanManageAch',
      'CanReadCatalogs',
      'CanManageUsers',
      'CanViewPaymentRailCapabilityRegistry',
      'CanManageCertificates',
      'CanGenerateNacha',
      'CanGenerateEncryptedNacha',
      'CanManualEncryptEnvelope',
      'CanManualDecryptEnvelope',
      'CanViewNachaSecurityAudit',
      'CanRunInteroperabilityHarness'
    ],
    exp: Math.floor(Date.now() / 1000) + 3600,
    iat: Math.floor(Date.now() / 1000)
  });

  await page.addInitScript((accessToken) => {
    window.sessionStorage.setItem('ach.interbank.access_token', accessToken);
  }, token);
}

async function loginByApi(apiBaseUrl: string, user: string, pass: string): Promise<string> {
  const response = await fetch(`${apiBaseUrl.replace(/\/+$/, '')}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username: user, password: pass }),
    signal: AbortSignal.timeout(10_000)
  });

  if (!response.ok) {
    throw new Error(`Login del smoke falló con HTTP ${response.status}`);
  }

  const payload = (await response.json()) as { data?: { token?: string }; token?: string };
  const token = payload.data?.token ?? payload.token;
  if (!token) {
    throw new Error('Login del smoke no devolvió token.');
  }

  return token;
}

async function mockBackend(page: Page): Promise<void> {
  await page.route(/(?:https?:\/\/[^/]+)?\/(?:api\/.*|nacha-config\/catalogos-filtro|financial-institutions|clearing-houses|customers|transactions\/company-entry-descriptions|ach-cycles(?:\/exportable)?|incoming-nacha-command-center\/.*|NachaExport\/.*)(?:\?.*)?$/i, async route => {
    const url = new URL(route.request().url());
    const path = url.pathname;
    const method = route.request().method().toUpperCase();

    if (method === 'GET' && path === '/api/users') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildUsersPage()) });
      return;
    }

    if (method === 'GET' && path === '/api/roles') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildRoles()) });
      return;
    }

    if (method === 'GET' && path === '/api/permissions') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildPermissions()) });
      return;
    }

    if (method === 'GET' && path === '/nacha-config/catalogos-filtro') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildCatalogsFilter()) });
      return;
    }

    if (method === 'GET' && path === '/api/integrations/methods') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildIntegrationMethods()) });
      return;
    }

    if (method === 'GET' && path === '/api/integrations/mappingsets') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildIntegrationMappingSets()) });
      return;
    }

    if (method === 'GET' && path === '/api/integrations/transformations') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify([{ code: 'NONE', displayName: 'Sin transformación', description: 'Smoke', supportsFormatMask: false, supportsMultipleSources: false }]) });
      return;
    }

    if (method === 'GET' && path === '/api/integrations/source-catalog') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildIntegrationSourceCatalog()) });
      return;
    }

    if (/^\/api\/integrations\/methods\/\d+\/parameters$/.test(path)) {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildIntegrationMethodParameters()) });
      return;
    }

    if (method === 'GET' && path === '/api/ach-cycles') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildAchCycles(path)) });
      return;
    }

    if (method === 'GET' && path === '/api/ach-cycles/exportable') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildAchCycles(path)) });
      return;
    }

    if (method === 'GET' && path === '/api/clearing-houses') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildClearingHouses()) });
      return;
    }

    if (method === 'GET' && path === '/api/transactions') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildTransactions()) });
      return;
    }

    if (method === 'GET' && path === '/customers') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildCustomers()) });
      return;
    }

    if (method === 'GET' && path === '/transactions/company-entry-descriptions') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildCompanyEntryDescriptions()) });
      return;
    }

    if (method === 'GET' && path === '/api/navigation/menu-items') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildNavigationMenuItems()) });
      return;
    }

    if (method === 'GET' && path === '/api/clearing-house-transaction-rules') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildClearingHouseTransactionRules()) });
      return;
    }

    if (method === 'POST' && path === '/api/transaction-prerequisite-policy/preview') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          ruleConfigured: true,
          requiresPrenotification: true,
          prenotificationMode: 'Mandatory',
          requiresReceiverIdentificationValidation: true,
          receiverIdentificationValidationMode: 'Mandatory',
          normativeSource: 'smoke',
          normativeReference: 'smoke',
          decision: 'Allow',
          message: 'Vista previa controlada de smoke.'
        })
      });
      return;
    }

    if (method === 'GET' && (path === '/financial-institutions' || path === '/api/financial-institutions')) {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(buildFinancialInstitutions())
      });
      return;
    }

    if (method === 'GET' && path === '/financial-institutions') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
        { id: 1, label: 'Panel principal', route: '/dashboard' },
        { id: 2, label: 'Usuarios', route: '/users', children: [
          { id: 21, label: 'Identidad y colores', route: '/users/branding' },
          { id: 22, label: 'Reglas de contraseña', route: '/users/password-rules' },
          { id: 23, label: 'Bloqueo de acceso', route: '/users/login-lockout' }
        ]},
        { id: 3, label: 'Integraciones', route: '/integraciones', children: [
          { id: 31, label: 'Integraciones SOAP', route: '/soap-integrations' }
        ]},
        { id: 4, label: 'CENIT', route: '/cenit', children: [
          { id: 41, label: 'Regulatorio: Devoluciones', route: '/cenit/regulatorio/causales-devolucion' },
          { id: 42, label: 'Regulatorio: Rechazos', route: '/cenit/regulatorio/causales-rechazo' },
          { id: 43, label: 'Regulatorio: Políticas', route: '/cenit/regulatorio/politicas-transaccion' },
          { id: 44, label: 'Operación: Ciclos', route: '/cenit/operacion/ciclos' },
          { id: 45, label: 'Operación: Cola', route: '/cenit/operacion/cola' },
          { id: 46, label: 'Operación: Neteo', route: '/cenit/operacion/neteo' },
          { id: 47, label: 'Operación: Optimización', route: '/cenit/operacion/optimizacion' },
          { id: 48, label: 'Operación: Devoluciones', route: '/cenit/operacion/devoluciones' },
          { id: 49, label: 'Operación: Trazabilidad', route: '/cenit/operacion/trazabilidad' }
        ]},
        { id: 5, label: 'Configuración NACHA-M', route: '/nacha-config-admin/perfiles', children: [
          { id: 51, label: 'Perfiles oficiales', route: '/nacha-config-admin/perfiles' },
          { id: 52, label: 'Registros oficiales', route: '/nacha-config-admin/records' },
          { id: 53, label: 'Variantes y campos', route: '/nacha-config-admin/variants-fields' }
        ]},
        { id: 6, label: 'Catálogos', route: '/catalogs' },
        { id: 7, label: 'Transacciones', route: '/transactions', children: [
          { id: 71, label: 'Listado', route: '/transactions/list' },
          { id: 72, label: 'Crear transacción', route: '/transactions/create' },
          { id: 73, label: 'Carga masiva', route: '/transactions/bulk-create' },
          { id: 74, label: 'Carga masiva por archivo', route: '/transactions/bulk-ingestion/upload' },
          { id: 75, label: 'Seguimiento lotes', route: '/transactions/bulk-ingestion/tracking' },
          { id: 76, label: 'Configuración de ciclos', route: '/transactions/cycle-configs' },
          { id: 77, label: 'Reglas por cámara', route: '/transactions/clearing-house-rules' },
          { id: 78, label: 'Cargar NACHA-M', route: '/transactions/nacha-upload' },
          { id: 79, label: 'Devoluciones ACH', route: '/transactions/returns' }
        ]},
        { id: 8, label: 'Navegación', route: '/navigation', children: [
          { id: 81, label: 'Menús', route: '/navigation/menu-items' }
        ]},
        { id: 9, label: 'Seguridad NACHA', route: '/nacha-security/dashboard', children: [
          { id: 91, label: 'Panel seguridad NACHA', route: '/nacha-security/dashboard' },
          { id: 92, label: 'Certificados', route: '/nacha-security/certificates' },
          { id: 93, label: 'Sobre digital', route: '/nacha-security/sobre-digital' }
        ]},
        { id: 10, label: 'Programador', route: '/scheduler', children: [
          { id: 101, label: 'Tareas programadas', route: '/scheduler/tasks' }
        ]},
        { id: 11, label: 'Logs', route: '/audit-logs', children: [
          { id: 111, label: 'Logs de auditoría', route: '/audit-logs' },
          { id: 112, label: 'Logs de autenticaciones', route: '/auth-logs' },
          { id: 113, label: 'Logs de navegación', route: '/navigation-logs' }
        ]},
        { id: 12, label: 'UAT / Simuladores', route: '/uat', children: [
          { id: 121, label: 'Simulador NACHA-M Entrada', route: '/uat/nacha-inbound-simulator' }
        ]},
        { id: 13, label: 'Respuestas ACH', route: '/ach-responses' },
        { id: 14, label: 'Reportes', route: '/reports' },
        { id: 15, label: 'Command Center inbound NACHA', route: '/incoming-nacha-command-center' },
        { id: 16, label: 'Ciclos ACH', route: '/ach-cycles' },
        { id: 17, label: 'Capability Registry', route: '/payment-rail-capability-registry' },
        { id: 18, label: 'Clientes', route: '/customers' },
        { id: 19, label: 'Alias', route: '/aliases' },
        { id: 20, label: 'Panel operativo', route: '/ach' }
      ])
      });
      return;
    }

    if (path === '/api/users/branding') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({ logoUrl: null, primaryColor: '#1d4ed8', secondaryColor: '#0f172a' })
      });
      return;
    }

    if (path === '/api/navigation-logs') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ ok: true }) });
      return;
    }

    if (method === 'GET' && path === '/api/ach/nacha/config-profiles/dashboard') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildConfigProfilesDashboard()) });
      return;
    }

    if (method === 'GET' && path === '/api/ach/nacha/config-profiles') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildConfigProfilesList()) });
      return;
    }

    if (method === 'GET' && path === '/nacha-config/perfiles/10') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildConfigProfileDetail()) });
      return;
    }

    if (path.startsWith('/api/regulatory-catalogs/')) {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildCenitResponse(path)) });
      return;
    }

    if (path === '/api/ach/nacha/operational/dashboard') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildOperationalDashboard()) });
      return;
    }

    if (path === '/api/ach/nacha/operational/files/demo-ach-in-001') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildOperationalFileDetail()) });
      return;
    }

    if (path.startsWith('/api/ach/nacha/soap-uat-console/')) {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildSoapUatConsole(path)) });
      return;
    }

    if (path.startsWith('/api/ach/reconciliation/')) {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildReconciliation(path)) });
      return;
    }

    if (method === 'GET' && path === '/api/customer-third-parties') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildCustomerThirdParties()) });
      return;
    }

    if (path.startsWith('/incoming-nacha-command-center/')) {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildIncomingNacha(path)) });
      return;
    }

    if (path === '/api/reports/traceability/pdf') {
      await route.fulfill({ status: 200, contentType: 'application/pdf', body: Buffer.from('%PDF-1.4 smoke') });
      return;
    }

    if (path === '/api/reports/transactions/sent' || path === '/api/reports/transactions/received') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildTransactionsReport(path)) });
      return;
    }

    if (path === '/api/reports/returns' || path === '/api/reports/rejections') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildReturnRejectionReport(path)) });
      return;
    }

    if (path === '/api/reports/nacha-files') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildNachaFilesReport()) });
      return;
    }

    if (path === '/api/reports/cycles') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildCyclesReport()) });
      return;
    }

    if (path === '/api/reports/audit') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildAuditReport()) });
      return;
    }

    if (path === '/api/reports/history') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildHistoryReport()) });
      return;
    }

    if (path === '/api/reports/reconciliation') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildReconciliationReport()) });
      return;
    }

    if (path === '/api/ach/responses') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildAchResponsesList()) });
      return;
    }

    if (path === '/api/ach/response-status-mappings') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildAchResponseStatusMappings()) });
      return;
    }

    if (/^\/api\/ach\/responses\/[^/]+\/notification-attempts$/.test(path)) {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildAchResponseAttempts()) });
      return;
    }

    if (/^\/api\/ach\/responses\/[^/]+$/.test(path)) {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildAchResponseDetail()) });
      return;
    }

    if (path === '/api/payment-rails/capability-registry/rails') {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildPaymentRails()) });
      return;
    }

    if (/^\/api\/payment-rails\/capability-registry\/rails\/[^/]+\/capabilities\/[^/]+$/.test(path)) {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildPaymentRailCapability()) });
      return;
    }

    if (/^\/api\/payment-rails\/capability-registry\/rails\/[^/]+\/capabilities$/.test(path)) {
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildPaymentRailCapabilities()) });
      return;
    }

    if (method === 'GET' && path === '/clearing-houses') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify(buildClearingHouses())
      });
      return;
    }

    if (path === '/clearing-houses') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
        { id: 1, label: 'Panel principal', route: '/dashboard' },
        { id: 2, label: 'Usuarios', route: '/users', children: [
          { id: 21, label: 'Identidad y colores', route: '/users/branding' },
          { id: 22, label: 'Reglas de contraseña', route: '/users/password-rules' },
          { id: 23, label: 'Bloqueo de acceso', route: '/users/login-lockout' }
        ]},
        { id: 3, label: 'Integraciones', route: '/integraciones', children: [
          { id: 31, label: 'Integraciones SOAP', route: '/soap-integrations' }
        ]},
        { id: 4, label: 'CENIT', route: '/cenit', children: [
          { id: 41, label: 'Regulatorio: Devoluciones', route: '/cenit/regulatorio/causales-devolucion' },
          { id: 42, label: 'Regulatorio: Rechazos', route: '/cenit/regulatorio/causales-rechazo' },
          { id: 43, label: 'Regulatorio: Políticas', route: '/cenit/regulatorio/politicas-transaccion' },
          { id: 44, label: 'Operación: Ciclos', route: '/cenit/operacion/ciclos' },
          { id: 45, label: 'Operación: Cola', route: '/cenit/operacion/cola' },
          { id: 46, label: 'Operación: Neteo', route: '/cenit/operacion/neteo' },
          { id: 47, label: 'Operación: Optimización', route: '/cenit/operacion/optimizacion' },
          { id: 48, label: 'Operación: Devoluciones', route: '/cenit/operacion/devoluciones' },
          { id: 49, label: 'Operación: Trazabilidad', route: '/cenit/operacion/trazabilidad' }
        ]},
        { id: 5, label: 'Configuración NACHA-M', route: '/nacha-config-admin/perfiles', children: [
          { id: 51, label: 'Perfiles oficiales', route: '/nacha-config-admin/perfiles' },
          { id: 52, label: 'Registros oficiales', route: '/nacha-config-admin/records' },
          { id: 53, label: 'Variantes y campos', route: '/nacha-config-admin/variants-fields' }
        ]},
        { id: 6, label: 'Catálogos', route: '/catalogs' },
        { id: 7, label: 'Transacciones', route: '/transactions', children: [
          { id: 71, label: 'Listado', route: '/transactions/list' },
          { id: 72, label: 'Crear transacción', route: '/transactions/create' },
          { id: 73, label: 'Carga masiva', route: '/transactions/bulk-create' },
          { id: 74, label: 'Carga masiva por archivo', route: '/transactions/bulk-ingestion/upload' },
          { id: 75, label: 'Seguimiento lotes', route: '/transactions/bulk-ingestion/tracking' },
          { id: 76, label: 'Configuración de ciclos', route: '/transactions/cycle-configs' },
          { id: 77, label: 'Reglas por cámara', route: '/transactions/clearing-house-rules' },
          { id: 78, label: 'Cargar NACHA-M', route: '/transactions/nacha-upload' },
          { id: 79, label: 'Devoluciones ACH', route: '/transactions/returns' }
        ]},
        { id: 8, label: 'Navegación', route: '/navigation', children: [
          { id: 81, label: 'Menús', route: '/navigation/menu-items' }
        ]},
        { id: 9, label: 'Seguridad NACHA', route: '/nacha-security/dashboard', children: [
          { id: 91, label: 'Panel seguridad NACHA', route: '/nacha-security/dashboard' },
          { id: 92, label: 'Certificados', route: '/nacha-security/certificates' },
          { id: 93, label: 'Sobre digital', route: '/nacha-security/sobre-digital' }
        ]},
        { id: 10, label: 'Programador', route: '/scheduler', children: [
          { id: 101, label: 'Tareas programadas', route: '/scheduler/tasks' }
        ]},
        { id: 11, label: 'Logs', route: '/audit-logs', children: [
          { id: 111, label: 'Logs de auditoría', route: '/audit-logs' },
          { id: 112, label: 'Logs de autenticaciones', route: '/auth-logs' },
          { id: 113, label: 'Logs de navegación', route: '/navigation-logs' }
        ]},
        { id: 12, label: 'UAT / Simuladores', route: '/uat', children: [
          { id: 121, label: 'Simulador NACHA-M Entrada', route: '/uat/nacha-inbound-simulator' }
        ]},
        { id: 13, label: 'Respuestas ACH', route: '/ach-responses' },
        { id: 14, label: 'Reportes', route: '/reports' },
        { id: 15, label: 'Command Center inbound NACHA', route: '/incoming-nacha-command-center' },
        { id: 16, label: 'Ciclos ACH', route: '/ach-cycles' },
        { id: 17, label: 'Capability Registry', route: '/payment-rail-capability-registry' },
        { id: 18, label: 'Clientes', route: '/customers' },
        { id: 19, label: 'Alias', route: '/aliases' },
        { id: 20, label: 'Panel operativo', route: '/ach' }
      ])
      });
      return;
    }

    if (path === '/ach-cycles' || path === '/ach-cycles/exportable') {
      if (route.request().resourceType() === 'document' && path === '/ach-cycles') {
        await route.continue();
        return;
      }
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(buildAchCycles(path)) });
      return;
    }

    if (path.startsWith('/NachaExport/')) {
      await route.fulfill({
        status: 200,
        contentType: 'application/octet-stream',
        body: Buffer.from('smoke')
      });
      return;
    }

    if (route.request().method().toUpperCase() === 'POST' && path === '/api/reports/accounting-review/export') {
      await route.fulfill({
        status: 200,
        contentType: 'application/octet-stream',
        headers: { 'content-disposition': 'attachment; filename="accounting-review-operativo.pdf"' },
        body: Buffer.from('smoke-pdf')
      });
      return;
    }

    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: '{}'
    });
  });
}

function buildCatalogsFilter(): Record<string, unknown> {
  return {
    estados: [
      { code: 'BORRADOR', labelEs: 'Borrador' },
      { code: 'PUBLICADO', labelEs: 'Publicado' },
      { code: 'INACTIVO', labelEs: 'Inactivo' },
      { code: 'ARCHIVADO', labelEs: 'Archivado' }
    ],
    camaras: [
      { code: 'ACH', labelEs: 'ACH Colombia' },
      { code: 'CENIT', labelEs: 'CENIT' }
    ],
    flujos: [
      { code: 'ORIGINAL', labelEs: 'Original' },
      { code: 'RETORNO', labelEs: 'Retorno' }
    ],
    direcciones: [
      { code: 'SALIDA', labelEs: 'Salida' },
      { code: 'ENTRADA', labelEs: 'Entrada' }
    ],
    servicios: [
      { code: 'PPD', labelEs: 'PPD' },
      { code: 'CCD', labelEs: 'CCD' }
    ]
  };
}

function buildConfigProfilesDashboard(): Record<string, unknown> {
  return {
    productiveStatus: 'NO-GO',
    isOfficialModel: true,
    legacyDeprecated: false,
    profileCount: 1,
    publishedProfileCount: 1,
    currentProfileCount: 1,
    layoutVariantCount: 6,
    fieldCount: 42,
    clearingHouses: ['ACH', 'CENIT'],
    recordTypes: ['1', '5', '6', '7', '8', '9'],
    warnings: []
  };
}

function buildConfigProfilesList(): Record<string, unknown>[] {
  return [
    {
      profileId: 10,
      profileCode: 'CENIT-OUT-220',
      profileName: 'CENIT salida 220',
      clearingHouseCode: 'CENIT',
      flowType: 'Outgoing',
      status: 'Published',
      version: '1.0',
      isPublished: true,
      isCurrent: true,
      effectiveFrom: '2026-01-01T00:00:00Z',
      effectiveTo: null,
      layoutVariantCount: 6,
      fieldCount: 42,
      recordTypes: ['1', '5', '6', '7', '8', '9'],
      isOfficialModel: true,
      legacyDeprecated: false
    }
  ];
}

function buildConfigProfileDetail(): Record<string, unknown> {
  return {
    id: 10,
    profileCode: 'CENIT-OUT-220',
    nombreEs: 'CENIT salida 220',
    descripcion: 'Perfil oficial administrable',
    estado: 'BORRADOR',
    versionMajor: 1,
    versionMinor: 0,
    contextPriority: 100,
    effectiveFrom: '2026-01-01T00:00:00Z',
    effectiveTo: null,
    rowVersion: 'cm93',
    records: [
      { id: 1, recordCode: '1', sequence: 1, isEnabled: true, minOccurs: 1, maxOccurs: 1, sourceStrategy: 'STATIC' }
    ],
    variantes: [
      {
        id: 2,
        recordCode: '1',
        variantCode: 'CENIT_R1_BASE_V1',
        nombreEs: 'Base',
        priority: 1,
        isDefaultForRecord: true,
        totalLength: 94,
        fields: []
      }
    ]
  };
}

function buildRoles(): Record<string, unknown>[] {
  return [
    { id: 'admin', name: 'Admin', description: 'Administrador' },
    { id: 'ach.operator', name: 'ACH.Operator', description: 'Operador ACH' }
  ];
}

function buildPermissions(): Record<string, unknown>[] {
  return [
    { id: 'CanReadAch', name: 'CanReadAch', description: 'Leer ACH' },
    { id: 'CanManageAch', name: 'CanManageAch', description: 'Administrar ACH' },
    { id: 'CanManageUsers', name: 'CanManageUsers', description: 'Administrar usuarios' },
    { id: 'CanReadCatalogs', name: 'CanReadCatalogs', description: 'Leer catálogos' }
  ];
}

function buildNavigationMenuItems(): Record<string, unknown>[] {
  return [
    {
      id: 1,
      parentId: null,
      label: 'Panel principal',
      route: '/dashboard',
      icon: 'dashboard',
      order: 1,
      exact: true,
      isActive: true,
      roleIds: [],
      permissionIds: []
    },
    {
      id: 2,
      parentId: null,
      label: 'Transacciones',
      route: '/transactions',
      icon: 'swap_horiz',
      order: 2,
      exact: false,
      isActive: true,
      roleIds: ['admin'],
      permissionIds: ['CanReadAch']
    }
  ];
}

function buildUsersPage(): Record<string, unknown> {
  return {
    items: [
      {
        id: 'u-1',
        userName: 'spa.smoke',
        fullName: 'Usuario SPA Smoke',
        email: 'spa.smoke@example.com',
        phoneNumber: '3000000000',
        roles: [{ id: 'admin', name: 'Admin' }],
        isActive: true
      }
    ],
    total: 1,
    page: 1,
    pageSize: 10
  };
}

function buildIntegrationMethods(): Record<string, unknown>[] {
  return [
    {
      id: 1,
      code: 'ACH-CORE',
      displayName: 'ACH Core',
      soapClientCode: 'ACH_CORE',
      isActive: true,
      integrationKey: 'ACH',
      operationKey: 'PROCESS',
      mappingDirection: 'Outbound',
      mappingPurpose: 'Core',
      functionalNature: 'Monetary',
      functionalOriginator: 'SpaSmoke',
      movesMoney: false
    }
  ];
}

function buildIntegrationMethodParameters(): Record<string, unknown>[] {
  return [
    {
      id: 1,
      methodId: 1,
      parameterPath: 'transaction.id',
      displayName: 'ID transacción',
      descriptionEs: 'Identificador de prueba',
      category: 'Core',
      exampleValue: 'TX-1',
      uiHelpText: 'Smoke',
      dataType: 'string',
      direction: 'Input',
      cardinality: 'Scalar',
      required: true,
      sortOrder: 1,
      isActive: true
    }
  ];
}

function buildIntegrationSourceCatalog(): Record<string, unknown>[] {
  return [
    {
      id: 1,
      methodId: 1,
      sourceKind: 'Transaction',
      entityName: 'Transaction',
      fieldPath: 'reference',
      displayName: 'Referencia',
      dataType: 'string',
      cardinality: 'Scalar',
      nullable: false,
      sortOrder: 1,
      isActive: true
    }
  ];
}

function buildIntegrationMappingSets(): Record<string, unknown>[] {
  return [
    {
      id: 'm-1',
      methodId: 1,
      methodCode: 'ACH-CORE',
      name: 'Versión de smoke',
      version: 1,
      status: 'Draft',
      isActive: true,
      notes: 'Smoke',
      publishedAtUtc: null,
      publishedBy: 'spa.smoke',
      rules: []
    }
  ];
}

function buildClearingHouses(): Record<string, unknown>[] {
  return [
    { id: 1, name: 'ACH Colombia' },
    { id: 2, name: 'CENIT' }
  ];
}

function buildFinancialInstitutions(): Record<string, unknown>[] {
  return [
    {
      id: 1,
      name: 'ACH Colombia',
      routingNumber: '123456789',
      transitCode: 'ACH1',
      checkDigit: '9',
      isDefaultSource: true,
      status: 'Active'
    },
    {
      id: 2,
      name: 'CENIT',
      routingNumber: '987654321',
      transitCode: 'CEN1',
      checkDigit: '1',
      isDefaultSource: false,
      status: 'Active'
    }
  ];
}

function buildCustomers(): Record<string, unknown>[] {
  return [
    {
      id: 1,
      documentType: 'NIT',
      documentNumber: '900123456',
      accountNumber: '1234567890',
      accountNumbers: ['1234567890'],
      personType: 'PJ',
      companyName: 'Smoke Co',
      fullName: 'Smoke Co S.A.S.'
    },
    {
      id: 2,
      documentType: 'CC',
      documentNumber: '100200300',
      accountNumber: '9876543210',
      accountNumbers: ['9876543210'],
      personType: 'PN',
      companyName: null,
      fullName: 'Usuario Smoke'
    }
  ];
}

function buildCompanyEntryDescriptions(): Record<string, unknown>[] {
  return [
    { id: 1, term: 'NOMINAS', description: 'Nóminas', standardEntryClassCode: 'PPD' },
    { id: 2, term: 'PROVEEDORES', description: 'Proveedores', standardEntryClassCode: 'CCD' }
  ];
}

function buildCustomerThirdParties(): Record<string, unknown> {
  return {
    items: [
      {
        id: 1,
        destinationInstitutionId: 1,
        destinationInstitutionName: 'ACH Colombia',
        destinationAccountNumber: '6543210001',
        recipientIdNumber: '900123456'
      }
    ],
    total: 1,
    page: 1,
    pageSize: 500
  };
}

function buildTransactions(): Record<string, unknown>[] {
  return [
    {
      id: 1,
      amount: 1000,
      transactionExternalId: 'TX-1',
      reference: 'Ref smoke',
      type: 1,
      traceNumber: 'TRACE-1',
      effectiveEntryDate: '2026-05-24',
      createdAt: '2026-05-24T00:00:00Z',
      sourceAccountNumber: '123456',
      destinationAccountNumber: '654321',
      sourceInstitutionName: 'ACH Colombia',
      destinationInstitutionName: 'CENIT',
      isPrenotification: false,
      transactionCode: '21',
      achBatchId: 1,
      batchSequenceNumber: 1,
      batchCompanyName: 'Smoke Co',
      batchEffectiveEntryDate: '2026-05-24',
      achCycleId: 'CYCLE-1',
      achCycleName: 'Ciclo 1',
      clearingHouseName: 'ACH Colombia'
    }
  ];
}

function buildClearingHouseTransactionRules(): Record<string, unknown>[] {
  return [
    {
      id: 1,
      clearingHouseId: 1,
      clearingHouseName: 'ACH Colombia',
      transactionNature: 'Debit',
      transactionType: 2,
      requiresPrenotification: true,
      prenotificationMode: 'Mandatory',
      requiresReceiverIdentificationValidation: true,
      receiverIdentificationValidationMode: 'Mandatory',
      appliesToNachaExport: true,
      appliesToMonetaryTransactions: true,
      effectiveFrom: '2026-05-24T00:00:00Z',
      effectiveTo: null,
      isActive: true,
      normativeSource: 'smoke',
      normativeReference: 'smoke',
      notes: 'smoke',
      createdAt: '2026-05-24T00:00:00Z',
      updatedAt: '2026-05-24T00:00:00Z'
    }
  ];
}

function buildCenitResponse(path: string): unknown {
  if (path.endsWith('/return-codes')) {
    return [
      { code: 'R01', description: 'Origen inválido', appliesToDebit: true, appliesToCredit: false, appliesToPrenotification: false, appliesToReturn: true, maxDaysAllowed: 5, requiresAddenda: false, isActive: true }
    ];
  }
  if (path.endsWith('/file-rejection-codes')) {
    return [
      { code: 'D01', description: 'Cuenta inválida', severity: 'Alta', appliesToStage: 'Origen', isRetryable: false, isActive: true }
    ];
  }
  if (path.endsWith('/transaction-type-policies')) {
    return [
      { transactionType: 'Credit', priorityOrder: 1, isMonetary: true, requiresPrenotification: false, canBeReturned: true, canBeReturnedAgain: false, isActive: true }
    ];
  }
  if (path.endsWith('/return-policies')) {
    return [
      { transactionType: 'Credit', allowedReturnCodesCsv: 'R01', maxDays: 5, requiredOriginalTransactionState: 'Certified', allowsReturnOfReturn: false, requiresAddenda: false, isActive: true }
    ];
  }
  if (path.endsWith('/return-of-return-policies')) {
    return [
      { originalReturnCode: 'R01', allowedNewReturnCodesCsv: 'R02', maxDays: 5, requiredOriginalState: 'ReturnedByOperator', isActive: true }
    ];
  }
  return [
    { transactionType: 'Credit', isRequired: true, requiresAddenda: false, blocksMonetaryTransactionIfMissing: true, isActive: true }
  ];
}

function buildOperationalDashboard(): Record<string, unknown> {
  return {
    summary: {
      productiveStatus: 'NO-GO',
      backendPhase: '6B.5.6',
      soapMode: 'Simulated',
      productiveExecution: false,
      wouldInvokeRealSoap: false,
      totalFiles: 6,
      totalIncomingFiles: 2,
      totalOutgoingFiles: 2,
      totalReturnFiles: 2,
      totalDecisions: 6,
      totalSoapCandidates: 4,
      totalNoGoBlocks: 3,
      totalManualReview: 1,
      totalReadinessChecks: 2,
      lastUpdatedAt: '2026-05-24T23:00:00Z',
      isDemoData: true,
      isPartialData: false,
      dataSource: 'demo seguro',
      warnings: ['Datos demo seguros locales usados como fallback read-only.']
    },
    files: [
      {
        fileId: 'demo-ach-in-001',
        fileName: 'ACH_COL_IN_001.ach',
        clearingHouseCode: 'ACH',
        profileCode: 'OFFICIAL_ACH_ENTRADA_ORIGINAL_V1_0',
        flowType: 'IncomingCreditFromExternalOriginator',
        isReturnFile: false,
        validationPassed: true,
        batchCount: 1,
        entryCount: 2,
        addendaCount: 1,
        batchControlCount: 1,
        fileControlCount: 1,
        processingStatus: 'Processed',
        receivedAt: '2026-05-24T14:30:00Z',
        createdAt: '2026-05-24T14:30:00Z',
        correlationId: 'phase-6b5-uat-orch',
        hasErrors: false,
        warningCount: 0,
        errorCount: 0
      }
    ],
    decisions: [
      {
        correlationId: 'phase-6b5-uat-orch',
        fileName: 'ACH_COL_IN_001.ach',
        entryTraceNumber: '900000010000001',
        originalTraceNumber: null,
        decisionType: 'ApplyCreditMovement',
        soapOperationCandidate: 'ProcTransacciones',
        requiresMonetaryMovement: true,
        reasonCode: '00',
        reasonDescription: 'Simulacion UAT aprobada',
        newInternalStatus: 'Accepted',
        manualReviewRequired: false,
        isBlocked: false,
        blockReason: null,
        createdAt: '2026-05-24T23:00:00Z'
      }
    ],
    readiness: [],
    audit: [],
    generatedAt: '2026-05-24T23:00:00Z',
    isDemoData: true,
    isPartialData: false,
    dataSource: 'demo seguro',
    warnings: ['Datos demo seguros locales usados como fallback read-only.'],
    productiveStatus: 'NO-GO'
  };
}

function buildOperationalFileDetail(): Record<string, unknown> {
  return {
    file: {
      fileId: 'demo-ach-in-001',
      fileName: 'ACH_COL_IN_001.ach',
      clearingHouseCode: 'ACH',
      profileCode: 'OFFICIAL_ACH_ENTRADA_ORIGINAL_V1_0',
      flowType: 'IncomingCreditFromExternalOriginator',
      isReturnFile: false,
      validationPassed: true,
      batchCount: 1,
      entryCount: 2,
      addendaCount: 1,
      batchControlCount: 1,
      fileControlCount: 1,
      processingStatus: 'Processed',
      receivedAt: '2026-05-24T14:30:00Z',
      createdAt: '2026-05-24T14:30:00Z',
      correlationId: 'phase-6b5-uat-orch',
      hasErrors: false,
      warningCount: 0,
      errorCount: 0
    },
    header: {
      headerId: 'hdr-1',
      priorityCode: '01',
      immediateDestination: '123456789',
      immediateOrigin: '987654321',
      fileCreationDate: '260524',
      fileCreationTime: '1200',
      fileIdModifier: 'A',
      recordSize: '094',
      blockingFactor: '10',
      formatCode: '1',
      referenceCode: 'REF-1',
      cycleNumber: 1
    },
    batches: [],
    entries: [],
    addendas: [],
    batchControls: [],
    fileControls: [],
    totalsSummary: {
      batchCount: 1,
      entryCount: 2,
      addendaCount: 1,
      batchControlCount: 1,
      fileControlCount: 1,
      persistedRecordCount: 5,
      totalDebitAmount: 1000,
      totalCreditAmount: 1000,
      validationPassed: true
    },
    warnings: [],
    noSensitiveData: true,
    isPartialData: false,
    dataSource: 'demo seguro'
  };
}

function buildSoapUatConsole(path: string): unknown {
  if (path.endsWith('/dashboard')) {
    return {
      backendPhase: '6B.5.6',
      productiveStatus: 'NO-GO',
      soapMode: 'Simulated',
      summary: {
        totalCandidates: 1,
        totalReadyForUat: 1,
        totalBlocked: 0,
        totalManualReview: 0,
        totalRegistrarRespuesta: 0,
        totalProcTransacciones: 1,
        totalProcContrapartidas: 0,
        totalNone: 0,
        totalSimulationPassed: 1,
        totalSimulationFailed: 0,
        totalResilienceWarnings: 0,
        totalDuplicateOrIdempotent: 0,
        productiveExecution: false,
        wouldInvokeRealSoap: false
      },
      totalCandidates: 1,
      totalReadyForUat: 1,
      totalBlocked: 0,
      totalManualReview: 0,
      totalRegistrarRespuesta: 0,
      totalProcTransacciones: 1,
      totalProcContrapartidas: 0,
      totalNone: 0,
      totalSimulationPassed: 1,
      totalSimulationFailed: 0,
      totalResilienceWarnings: 0,
      totalDuplicateOrIdempotent: 0,
      lastUpdatedAt: '2026-05-24T23:00:00Z',
      dataSource: 'demo seguro',
      isPartialData: false,
      warnings: []
    };
  }

  if (path.endsWith('/candidates')) {
    return [
      {
        correlationId: 'phase-6b5-uat-orch',
        fileName: 'ACH_COL_IN_001.ach',
        entryTraceNumber: '900000010000001',
        decisionType: 'ApplyCreditMovement',
        operationCandidate: 'ProcTransacciones',
        requiresMonetaryMovement: true,
        productiveExecution: false,
        wouldInvokeRealSoap: false,
        isReadyForUat: true,
        isBlocked: false,
        blockReasons: [],
        readinessStatus: 'Ready',
        simulationStatus: 'Passed',
        resilienceStatus: 'Passed',
        idempotencyStatus: 'Passed',
        manualReviewRequired: false,
        attemptCount: 1,
        dataSource: 'demo seguro',
        isPersisted: true,
        isDerived: false
      }
    ];
  }

  return [
    {
      correlationId: 'phase-6b5-uat-orch',
      phase: '6B.5',
      eventType: 'PayloadMappingCompleted',
      severity: 'Information',
      message: 'Payload SOAP interno mapeado.',
      isBlocked: false,
      timestamp: '2026-05-24T23:00:01Z',
      sanitizedDetails: { OperationCandidate: 'ProcTransacciones', RequiresMonetaryMovement: 'True', Phase: '6B.5' }
    }
  ];
}

function buildReconciliation(path: string): unknown {
  if (path.endsWith('/dashboard')) {
    return {
      productiveStatus: 'NO-GO',
      totalResponses: 1,
      totalDifferentialResponses: 0,
      totalReturns: 0,
      totalRejections: 0,
      totalPrenotifications: 0,
      totalRor: 0,
      totalReconciled: 1,
      totalPending: 0,
      totalInconsistent: 0,
      totalManualReviewRequired: 0,
      totalNonMonetary: 1,
      totalMonetaryCandidates: 0,
      lastUpdatedAt: '2026-05-24T00:00:00Z',
      dataSource: 'demo seguro',
      isPartialData: false,
      warnings: []
    };
  }

  if (path.endsWith('/items')) {
    return [
      {
        reconciliationId: 'rec-1',
        correlationId: 'phase-6b5-uat-orch',
        fileName: 'ACH_COL_IN_001.ach',
        clearingHouseCode: 'ACH',
        flowType: 'IncomingCreditFromExternalOriginator',
        responseType: 'Accepted',
        reasonCode: '00',
        traceNumberMasked: '********0001',
        internalStatus: 'Accepted',
        reconciliationStatus: 'Reconciled',
        soapOperationCandidate: 'ProcTransacciones'
      }
    ];
  }

  return {
    item: {
      reconciliationId: 'rec-1',
      correlationId: 'phase-6b5-uat-orch',
      fileName: 'ACH_COL_IN_001.ach',
      clearingHouseCode: 'ACH',
      flowType: 'IncomingCreditFromExternalOriginator',
      responseType: 'Accepted',
      reasonCode: '00',
      reasonDescription: 'Aceptado',
      traceNumberMasked: '********0001',
      originalTraceNumberMasked: '********0000',
      internalStatus: 'Accepted',
      reconciliationStatus: 'Reconciled',
      requiresManualReview: false,
      isReturnFile: false,
      isRor: false,
      isPrenotification: false,
      isNonMonetary: true,
      isMonetaryCandidate: false,
      soapOperationCandidate: 'RegistrarRespuestaTransaccion',
      createdAt: '2026-05-24T00:00:00Z',
      dataSource: 'demo seguro',
      isPersisted: true,
      isDerived: false
    },
    nachaHeaderSummary: null,
    batchSummary: null,
    entrySummary: null,
    addendaSummary: null,
    controlSummary: null,
    internalTransactionSummary: null,
    responseHistory: [],
    auditEvents: [],
    warnings: [],
    noSensitiveData: true
  };
}

function buildIncomingNacha(path: string): unknown {
  if (path.endsWith('/observability/summary')) {
    return {
      windowHours: 24,
      totalIngestions: 1,
      totalFiles: 1,
      totalQueueItems: 1,
      blockingCount: 0,
      warnings: []
    };
  }

  if (path.endsWith('/ingestions')) {
    return {
      items: [
        {
          id: 'ing-1',
          fileName: 'IN_001.ach',
          fileType: 'ACH',
          status: 'Processed',
          receivedAtUtc: '2026-05-24T14:00:00Z'
        }
      ],
      total: 1,
      page: 1,
      pageSize: 20
    };
  }

  if (/\/ingestions\/[^/]+$/.test(path)) {
    return {
      id: 'ing-1',
      fileName: 'IN_001.ach',
      status: 'Processed',
      summary: {},
      events: []
    };
  }

  if (path.endsWith('/queue')) {
    return {
      items: [
        {
          id: 'q-1',
          fileName: 'IN_001.ach',
          status: 'Queued',
          queueReason: 'Pending review'
        }
      ],
      total: 1,
      page: 1,
      pageSize: 20
    };
  }

  if (/\/queue\/[^/]+$/.test(path)) {
    return {
      id: 'q-1',
      ingestion: { id: 'ing-1', fileName: 'IN_001.ach' },
      events: [],
      manualActions: []
    };
  }

  return {};
}

function buildTransactionsReport(path: string): Record<string, unknown> {
  const items = [
    {
      transactionId: 1,
      effectiveEntryDate: '2026-05-24',
      transactionExternalId: 'EXT-1',
      reference: 'REF-1',
      amount: 1000,
      transactionType: 'Credit',
      state: 'Certified',
      clearingHouseName: 'ACH Colombia',
      achCycleId: 'CYCLE-1',
      achCycleName: 'Ciclo 1',
      batchId: 1,
      batchSequenceNumber: 1,
      sourceBankName: 'Banco A',
      destinationBankName: 'Banco B',
      nachaFileName: 'file.ach'
    }
  ];

  return {
    items,
    totals: { totalRecords: 1, totalCreditAmount: 1000, totalDebitAmount: 0 },
    total: 1,
    page: 1,
    pageSize: 20
  };
}

function buildReturnRejectionReport(path: string): Record<string, unknown> {
  const items = [
    {
      transactionId: 1,
      effectiveEntryDate: '2026-05-24',
      transactionExternalId: 'EXT-1',
      reference: 'REF-1',
      amount: 1000,
      state: 'ReturnedByOperator',
      causalCode: 'R01',
      causalDescription: 'Cuenta inválida',
      clearingHouseName: 'ACH Colombia',
      achCycleId: 'CYCLE-1',
      achCycleName: 'Ciclo 1',
      originalTraceRef: 'TRACE-1',
      originalTransactionId: 10,
      originalTransactionReference: 'ORIG-1'
    }
  ];

  return {
    items,
    totals: { totalRecords: 1, totalAmount: 1000 },
    total: 1,
    page: 1,
    pageSize: 20
  };
}

function buildNachaFilesReport(): Record<string, unknown> {
  return {
    items: [
      { fileName: 'file.ach', generatedAtUtc: '2026-05-24T00:00:00Z', clearingHouseName: 'ACH Colombia', exportKind: 'Plain', totalRecords: 1, totalTransactions: 1 }
    ],
    totals: { totalFiles: 1, totalRecords: 1, totalTransactions: 1 },
    total: 1,
    page: 1,
    pageSize: 20
  };
}

function buildCyclesReport(): Record<string, unknown> {
  return {
    items: [
      { cycleId: 'CYCLE-1', cycleName: 'Ciclo 1', processingDate: '2026-05-24', startTime: '08:00', endTime: '12:00', cutoffTime: '11:30', schedule: 'Normal', status: 'Activo', clearingHouseName: 'ACH Colombia', totalTransactions: 1, totalAmount: 1000 }
    ],
    totals: { totalCycles: 1, totalTransactions: 1, totalAmount: 1000 },
    total: 1,
    page: 1,
    pageSize: 20
  };
}

function buildAuditReport(): Record<string, unknown> {
  return {
    items: [
      { user: 'spa.smoke', action: 'VIEW', entity: 'CENIT', entityId: '1', dateUtc: '2026-05-24T00:00:00Z' }
    ],
    page: 1,
    pageSize: 20,
    total: 1
  };
}

function buildHistoryReport(): Record<string, unknown> {
  return {
    items: [
      { transactionId: 1, fromState: 'Pending', toState: 'Certified', source: 'System', reasonCode: 'OK', dateUtc: '2026-05-24T00:00:00Z', changedBy: 'spa.smoke' }
    ],
    page: 1,
    pageSize: 20,
    total: 1
  };
}

function buildReconciliationReport(): Record<string, unknown> {
  return {
    totals: {
      sentCount: 1,
      sentAmount: 1000,
      receivedCount: 1,
      receivedAmount: 1000,
      returnedCount: 0,
      returnedAmount: 0
    },
    differences: {
      sentVsReceivedCountDiff: 0,
      sentVsReceivedAmountDiff: 0,
      sentVsReturnedCountDiff: 1,
      sentVsReturnedAmountDiff: 1000,
      receivedVsReturnedCountDiff: 1,
      receivedVsReturnedAmountDiff: 1000
    },
    inconsistencies: []
  };
}

function buildAchResponsesList(): Record<string, unknown> {
  return {
    items: [
      { id: 'resp-1', responseType: 'ACK', status: 'Recibida', createdAtUtc: '2026-05-24T00:00:00Z' }
    ],
    total: 1,
    page: 1,
    pageSize: 20
  };
}

function buildAchResponseDetail(): Record<string, unknown> {
  return {
    id: 'resp-1',
    responseType: 'ACK',
    status: 'Recibida',
    receivedAtUtc: '2026-05-24T00:00:00Z',
    details: []
  };
}

function buildAchResponseAttempts(): Record<string, unknown>[] {
  return [
    { attemptId: 'att-1', status: 'Sent', createdAtUtc: '2026-05-24T00:00:00Z' }
  ];
}

function buildAchResponseStatusMappings(): Record<string, unknown>[] {
  return [
    { codigoCamaraCompensacion: 'ACH', tipoRespuesta: 'ACK', estadoProcesamiento: 'Recibida', activo: true }
  ];
}

function buildPaymentRails(): Record<string, unknown>[] {
  return [
    { railCode: 'ACH', displayName: 'ACH Colombia', isOperational: true },
    { railCode: 'CENIT', displayName: 'CENIT', isOperational: false }
  ];
}

function buildPaymentRailCapabilities(): Record<string, unknown>[] {
  return [
    {
      railCode: 'ACH',
      capabilityCode: 'Netting',
      state: 'Active',
      source: 'demo',
      effectiveFromUtc: '2026-05-24T00:00:00Z',
      effectiveToUtc: null,
      version: '1',
      changeSource: 'smoke',
      changeTicket: 'TCK-1',
      changedBy: 'spa.smoke',
      changedAtUtc: '2026-05-24T00:00:00Z'
    }
  ];
}

function buildPaymentRailCapability(): Record<string, unknown> {
  return {
    railCode: 'ACH',
    capabilityCode: 'Netting',
    state: 'Active',
    source: 'demo',
    effectiveFromUtc: '2026-05-24T00:00:00Z',
    effectiveToUtc: null,
    version: '1',
    changeSource: 'smoke',
    changeTicket: 'TCK-1',
    changedBy: 'spa.smoke',
    changedAtUtc: '2026-05-24T00:00:00Z'
  };
}

function buildAchCycles(path: string): unknown {
  if (path === '/ach-cycles/exportable' || path === '/api/ach-cycles/exportable') {
    return [
      {
        cycleId: 'CYCLE-1',
        cycleName: 'Ciclo 1',
        processingDate: '2026-05-24',
        transactionCount: 1,
        isExportable: true,
        exportUnavailableReason: null
      }
    ];
  }

  return {
    items: [
      {
        id: 'CYCLE-1',
        cycleName: 'Ciclo 1',
        date: '2026-05-24',
        startTime: '08:00',
        endTime: '12:00',
        status: 'Activo',
        clearingHouseId: 1,
        clearingHouseName: 'ACH Colombia',
        totalTransactions: 1
      }
    ],
    total: 1,
    page: 1,
    pageSize: 10
  };
}

function isAssetRequest(url: string): boolean {
  return /\.(js|css)(?:\?.*)?$/i.test(url);
}

function isCriticalAssetOrApi(url: string): boolean {
  return isAssetRequest(url) || /\/api\/|\/incoming-nacha-command-center\/|\/ach\/nacha\/|\/ach\/reconciliation\/|\/financial-institutions$|\/nacha-config\/catalogos-filtro$/i.test(url);
}

function isBenignConsoleError(text: string): boolean {
  return /net::ERR_CONNECTION_REFUSED/i.test(text)
    || /favicon\.ico/i.test(text)
    || /ResizeObserver loop limit exceeded/i.test(text)
    || /\[webpack-dev-server\] Errors while compiling/i.test(text)
    || /NG6009/i.test(text)
    || /standalone component, which can not be used in the `@NgModule\.bootstrap` array/i.test(text);
}

function slugify(value: string): string {
  return value
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
}

function createUnsignedJwt(payload: Record<string, unknown>): string {
  return `${base64Url({ alg: 'none', typ: 'JWT' })}.${base64Url(payload)}.e2e`;
}

function base64Url(value: Record<string, unknown>): string {
  return Buffer.from(JSON.stringify(value))
    .toString('base64')
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=+$/g, '');
}
