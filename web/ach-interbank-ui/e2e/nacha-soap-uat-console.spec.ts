import { expect, Page, test } from '@playwright/test';

const consolePath = '/ach/nacha/soap-uat-console';
const dashboardEndpoint = /\/api\/ach\/nacha\/soap-uat-console\/dashboard$/;
const candidatesEndpoint = /\/api\/ach\/nacha\/soap-uat-console\/candidates$/;
const auditEndpoint = /\/api\/ach\/nacha\/soap-uat-console\/audit$/;
const refreshEndpoint = /\/auth\/refresh$/;
const legacyNachaEndpoint = /\/(?:nacha-layouts|nacha-record-definitions)(?:\/|\?|$)/;
const hashExportPattern = /\/NachaExport\/[a-f0-9]{32,64}$/i;

test.describe('NACHA SOAP/UAT console read-only', () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthenticatedSession(page);
    await mockAuthRefresh(page);
    await mockConsole(page);
  });

  test('SoapUatConsole_ShouldLoadReadOnlyPage', async ({ page }) => {
    await page.goto(consolePath);

    await expect(page.getByRole('heading', { name: 'Consola SOAP/UAT read-only', level: 1 })).toBeVisible();
    await expect(page.getByText('Candidatos SOAP/UAT')).toBeVisible();
    await expect(page.getByText('Auditoria SOAP/UAT')).toBeVisible();
  });

  test('SoapUatConsole_ShouldShowNoGoAndSoapDisabled', async ({ page }) => {
    await page.goto(consolePath);

    await expect(page.getByText('Productivo NO-GO')).toBeVisible();
    await expect(page.getByText('SOAP real deshabilitado', { exact: true })).toBeVisible();
    await expect(page.getByText('ProductiveExecution', { exact: true })).toBeVisible();
    await expect(page.getByText('WouldInvokeRealSoap', { exact: true })).toBeVisible();
  });

  test('SoapUatConsole_ShouldRenderCandidatesAndAudit', async ({ page }) => {
    await page.goto(consolePath);

    await expect(page.getByText('ProcTransacciones', { exact: true }).first()).toBeVisible();
    await expect(page.getByText('RegistrarRespuesta', { exact: true })).toBeVisible();
    await expect(page.getByText('IDEMPOTENT/DUPLICATE', { exact: true })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Auditoria SOAP/UAT' })).toBeVisible();
  });

  test('SoapUatConsole_ShouldNotRenderExecutionButtons', async ({ page }) => {
    await page.goto(consolePath);

    await assertDangerousActionsAbsent(page);
  });

  test('SoapUatConsole_ShouldNotSendPostPutPatchDelete', async ({ page }) => {
    const blockedRequests: string[] = [];
    page.on('request', request => {
      if (request.url().includes('/api/ach/nacha/soap-uat-console') && request.method() !== 'GET') {
        blockedRequests.push(`${request.method()} ${request.url()}`);
      }
    });

    await page.goto(consolePath);
    await expect(page.getByRole('heading', { name: 'Consola SOAP/UAT read-only', level: 1 })).toBeVisible();

    expect(blockedRequests).toEqual([]);
  });

  test('SoapUatConsole_ShouldNotCallLegacyLayoutsOrDefinitions', async ({ page }) => {
    const legacyRequests: string[] = [];
    page.on('request', request => {
      if (legacyNachaEndpoint.test(request.url())) {
        legacyRequests.push(request.url());
      }
    });

    await page.goto(consolePath);
    await expect(page.getByRole('heading', { name: 'Consola SOAP/UAT read-only', level: 1 })).toBeVisible();

    expect(legacyRequests).toEqual([]);
  });

  test('SoapUatConsole_ShouldNotRequestNachaExportWithHash', async ({ page }) => {
    const hashRequests: string[] = [];
    page.on('request', request => {
      if (hashExportPattern.test(request.url())) {
        hashRequests.push(request.url());
      }
    });

    await page.goto(consolePath);
    await expect(page.getByRole('heading', { name: 'Consola SOAP/UAT read-only', level: 1 })).toBeVisible();

    expect(hashRequests).toEqual([]);
  });
});

async function mockConsole(page: Page): Promise<void> {
  await page.route(dashboardEndpoint, async route => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(dashboard()) });
  });
  await page.route(candidatesEndpoint, async route => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(candidates()) });
  });
  await page.route(auditEndpoint, async route => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(audit()) });
  });
}

async function assertDangerousActionsAbsent(page: Page): Promise<void> {
  for (const label of [/Ejecutar SOAP/i, /Reintentar SOAP/i, /Enviar movimiento/i, /Invocar core/i, /Editar/i, /Cargar certificado/i]) {
    await expect(page.getByRole('button', { name: label })).toHaveCount(0);
  }
}

async function seedAuthenticatedSession(page: Page): Promise<void> {
  const token = createUnsignedJwt({
    unique_name: 'uat.console',
    name: 'Usuario UAT Console',
    uid: 'uat-console',
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
    unique_name: 'uat.console',
    name: 'Usuario UAT Console',
    uid: 'uat-console',
    role: ['Admin', 'ACH.Operator'],
    permission: ['CanReadAch', 'CanManageAch'],
    exp: Math.floor(Date.now() / 1000) + 3600,
    iat: Math.floor(Date.now() / 1000)
  });

  await page.route(refreshEndpoint, async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ sucess: true, data: { token, username: 'uat.console', fullName: 'Usuario UAT Console', roles: ['Admin'], permissions: ['CanReadAch'] } })
    });
  });
}

function dashboard() {
  return {
    productiveStatus: 'NO-GO',
    productiveExecution: false,
    wouldInvokeRealSoap: false,
    totalCandidates: 2,
    totalReadyForUat: 0,
    totalBlocked: 1,
    totalManualReview: 1,
    totalRegistrarRespuesta: 1,
    totalProcTransacciones: 1,
    totalProcContrapartidas: 0,
    totalNone: 0,
    totalSimulationPassed: 1,
    totalSimulationFailed: 1,
    totalResilienceWarnings: 1,
    totalDuplicateOrIdempotent: 2,
    lastUpdatedAt: '2026-05-31T12:00:00Z',
    dataSource: 'backend read-only',
    isPartialData: false,
    warnings: ['Productivo permanece NO-GO; SOAP real deshabilitado.']
  };
}

function candidates() {
  return [
    { correlationId: 'corr-proc', fileName: 'entrada.ach', entryTraceNumber: '***0001', decisionType: 'CreditoEntrante', operationCandidate: 'ProcTransacciones', requiresMonetaryMovement: true, productiveExecution: false, wouldInvokeRealSoap: false, isReadyForUat: false, isBlocked: true, blockReasons: ['NO-GO'], manualReviewRequired: false, readinessStatus: 'BlockedByNoGo', simulationStatus: 'Passed', resilienceStatus: 'Warning', idempotencyStatus: 'Idempotent', lastAttemptAt: '2026-05-31T12:00:00Z', attemptCount: 1, dataSource: 'backend read-only', isPersisted: true, isDerived: true },
    { correlationId: 'corr-reg', fileName: 'respuesta.ach', entryTraceNumber: '***0002', decisionType: 'Respuesta', operationCandidate: 'RegistrarRespuestaTransaccion', requiresMonetaryMovement: false, productiveExecution: false, wouldInvokeRealSoap: false, isReadyForUat: true, isBlocked: false, blockReasons: ['WouldInvokeRealSoap=false'], manualReviewRequired: false, readinessStatus: 'ReadyUat', simulationStatus: 'Passed', resilienceStatus: 'Passed', idempotencyStatus: 'Idempotent', lastAttemptAt: '2026-05-31T12:00:00Z', attemptCount: 1, dataSource: 'backend read-only', isPersisted: true, isDerived: true }
  ];
}

function audit() {
  return [{ correlationId: 'corr-proc', phase: '6B.5', eventType: 'IncomingNachaIntegrationExecution', severity: 'Information', message: 'Sanitized', isBlocked: false, timestamp: '2026-05-31T12:00:00Z', sanitizedDetails: { Payload: 'Sanitized', WouldInvokeRealSoap: 'false' }, dataSource: 'backend read-only', isPersisted: true }];
}

function createUnsignedJwt(payload: Record<string, unknown>): string {
  return `${base64Url({ alg: 'none', typ: 'JWT' })}.${base64Url(payload)}.e2e`;
}

function base64Url(value: Record<string, unknown>): string {
  return Buffer.from(JSON.stringify(value)).toString('base64').replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '');
}
