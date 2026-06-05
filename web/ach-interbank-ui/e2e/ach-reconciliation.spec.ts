import { expect, Page, test } from '@playwright/test';

const path = '/ach/reconciliation';
const dashboardEndpoint = /\/api\/ach\/reconciliation\/dashboard$/;
const itemsEndpoint = /\/api\/ach\/reconciliation\/items$/;
const detailEndpoint = /\/api\/ach\/reconciliation\/items\/resp-1$/;
const refreshEndpoint = /\/auth\/refresh$/;
const legacyNachaEndpoint = /\/(?:nacha-layouts|nacha-record-definitions)(?:\/|\?|$)/;
const hashExportPattern = /\/NachaExport\/[a-f0-9]{32,64}$/i;

test.describe('ACH reconciliation read-only console', () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthenticatedSession(page);
    await mockAuthRefresh(page);
    await mockReconciliation(page);
  });

  test('Reconciliation_ShouldLoadReadOnlyPage', async ({ page }) => {
    await page.goto(path);

    await expect(page.getByRole('heading', { name: 'Consola de conciliación ACH solo lectura', level: 1 })).toBeVisible();
    await expect(page.getByText('Ítems conciliación ACH')).toBeVisible();
  });

  test('Reconciliation_ShouldShowNoGoAndNoMonetaryMovement', async ({ page }) => {
    await page.goto(path);

    await expect(page.getByText('Productivo NO-GO')).toBeVisible();
    await expect(page.getByText('Sin movimientos monetarios')).toBeVisible();
    await expect(page.getByLabel('Fuente conciliación').getByText('NO-GO', { exact: true })).toBeVisible();
  });

  test('Reconciliation_ShouldRenderItemsAndDetail', async ({ page }) => {
    await page.goto(path);

    await expect(page.getByText('Ítems conciliación ACH')).toBeVisible();
    await expect(page.getByText('Detalle de conciliación')).toBeVisible();
    await expect(page.getByText('Diferenciales', { exact: true })).toBeVisible();
    await expect(page.getByText('.RET', { exact: true })).toBeVisible();
    await expect(page.getByText('CONCILIADO', { exact: true })).toBeVisible();
  });

  test('Reconciliation_ShouldNotRenderDangerousActions', async ({ page }) => {
    await page.goto(path);

    await assertDangerousActionsAbsent(page);
  });

  test('Reconciliation_ShouldNotSendPostPutPatchDelete', async ({ page }) => {
    const blockedRequests: string[] = [];
    page.on('request', request => {
      if (request.url().includes('/api/ach/reconciliation') && request.method() !== 'GET') {
        blockedRequests.push(`${request.method()} ${request.url()}`);
      }
    });

    await page.goto(path);
    await expect(page.getByRole('heading', { name: 'Consola de conciliación ACH solo lectura', level: 1 })).toBeVisible();

    expect(blockedRequests).toEqual([]);
  });

  test('Reconciliation_ShouldNotCallLegacyLayoutsOrDefinitions', async ({ page }) => {
    const legacyRequests: string[] = [];
    page.on('request', request => {
      if (legacyNachaEndpoint.test(request.url())) {
        legacyRequests.push(request.url());
      }
    });

    await page.goto(path);
    await expect(page.getByRole('heading', { name: 'Consola de conciliación ACH solo lectura', level: 1 })).toBeVisible();

    expect(legacyRequests).toEqual([]);
  });

  test('Reconciliation_ShouldNotRequestNachaExportWithHash', async ({ page }) => {
    const hashRequests: string[] = [];
    page.on('request', request => {
      if (hashExportPattern.test(request.url())) {
        hashRequests.push(request.url());
      }
    });

    await page.goto(path);
    await expect(page.getByRole('heading', { name: 'Consola de conciliación ACH solo lectura', level: 1 })).toBeVisible();

    expect(hashRequests).toEqual([]);
  });
});

async function mockReconciliation(page: Page): Promise<void> {
  await page.route(dashboardEndpoint, async route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(dashboard()) }));
  await page.route(itemsEndpoint, async route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(items()) }));
  await page.route(detailEndpoint, async route => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(detail()) }));
}

async function assertDangerousActionsAbsent(page: Page): Promise<void> {
  for (const label of [/Aprobar/i, /Rechazar/i, /Reprocesar/i, /Ejecutar SOAP/i, /Mover dinero/i, /Generar archivo/i, /Editar estado/i]) {
    await expect(page.getByRole('button', { name: label })).toHaveCount(0);
  }
}

async function seedAuthenticatedSession(page: Page): Promise<void> {
  const token = createUnsignedJwt({ unique_name: 'uat.reconciliation', uid: 'uat-reconciliation', role: ['Admin', 'ACH.Operator'], permission: ['CanReadAch'], exp: Math.floor(Date.now() / 1000) + 3600, iat: Math.floor(Date.now() / 1000) });
  await page.addInitScript((accessToken) => window.sessionStorage.setItem('ach.interbank.access_token', accessToken), token);
}

async function mockAuthRefresh(page: Page): Promise<void> {
  const token = createUnsignedJwt({ unique_name: 'uat.reconciliation', uid: 'uat-reconciliation', role: ['Admin'], permission: ['CanReadAch'], exp: Math.floor(Date.now() / 1000) + 3600, iat: Math.floor(Date.now() / 1000) });
  await page.route(refreshEndpoint, async route => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ sucess: true, data: { token, username: 'uat.reconciliation', roles: ['Admin'], permissions: ['CanReadAch'] } }) });
  });
}

function dashboard() {
  return { productiveStatus: 'NO-GO', totalResponses: 2, totalDifferentialResponses: 1, totalReturns: 1, totalRejections: 1, totalPrenotifications: 1, totalRor: 1, totalReconciled: 1, totalPending: 1, totalInconsistent: 1, totalManualReviewRequired: 1, totalNonMonetary: 3, totalMonetaryCandidates: 1, lastUpdatedAt: '2026-05-31T12:00:00Z', dataSource: 'backend read-only', isPartialData: false, warnings: ['Productivo permanece NO-GO; consola read-only sin SOAP real ni movimientos.'] };
}

function items() {
  return [
    { reconciliationId: 'resp-1', correlationId: 'corr-1', fileName: 'entrada.ach', clearingHouseCode: 'ACH', flowType: 'DifferentialResponse', responseType: 'Respuesta diferencial', reasonCode: 'R01', traceNumberMasked: '***0001', originalTraceNumberMasked: '***9999', internalStatus: 'Notificada', reconciliationStatus: 'Conciliado', requiresManualReview: false, isReturnFile: false, isRor: false, isPrenotification: false, isNonMonetary: true, isMonetaryCandidate: false, soapOperationCandidate: 'RegistrarRespuestaTransaccion', createdAt: '2026-05-31T12:00:00Z', dataSource: 'backend read-only', isPersisted: true, isDerived: true },
    { reconciliationId: 'ret-1', correlationId: 'corr-ret', fileName: 'return.RET', clearingHouseCode: 'CENIT', flowType: 'Return', responseType: 'Devolucion .RET', reasonCode: 'R02', traceNumberMasked: '***0002', originalTraceNumberMasked: '***0001', internalStatus: 'Returned', reconciliationStatus: 'Pendiente', requiresManualReview: false, isReturnFile: true, isRor: false, isPrenotification: false, isNonMonetary: true, isMonetaryCandidate: false, soapOperationCandidate: 'RegistrarRespuestaTransaccion', createdAt: '2026-05-31T12:00:00Z', dataSource: 'backend read-only', isPersisted: true, isDerived: true }
  ];
}

function detail() {
  return { item: items()[0], nachaHeaderSummary: { headerId: 'N1' }, batchSummary: { batchNumber: 1 }, entrySummary: { traceNumberMasked: '***0001' }, addendaSummary: { originalTraceNumberMasked: '***9999' }, controlSummary: { entryAddendaCount: 2 }, internalTransactionSummary: { transactionId: 100 }, responseHistory: [], auditEvents: [], warnings: ['Detalle sanitizado.'], noSensitiveData: true };
}

function createUnsignedJwt(payload: Record<string, unknown>): string {
  return `${base64Url({ alg: 'none', typ: 'JWT' })}.${base64Url(payload)}.e2e`;
}

function base64Url(value: Record<string, unknown>): string {
  return Buffer.from(JSON.stringify(value)).toString('base64').replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/g, '');
}

