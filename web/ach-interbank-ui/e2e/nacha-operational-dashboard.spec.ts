import { expect, Page, test } from '@playwright/test';

const centerPath = '/incoming-nacha-command-center';
const ingestionId = '11111111-1111-1111-1111-111111111111';
const browserErrors = new WeakMap<Page, string[]>();

test.describe('seguimiento operativo integral NACHA-M', () => {
  test.beforeEach(async ({ page }) => {
    const errors: string[] = [];
    browserErrors.set(page, errors);
    page.on('console', (message) => {
      if (message.type() === 'error') errors.push(message.text());
    });
    page.on('pageerror', (error) => errors.push(error.message));
    await authenticatedSession(page);
    await mockSharedCatalogs(page);
    await mockSummary(page);
  });

  test.afterEach(async ({ page }) => {
    expect(browserErrors.get(page) ?? []).toEqual([]);
  });

  test('recorre archivos, filtros, validaciones, lotes, transacciones, addendas, procesamiento y causal', async ({ page }, testInfo) => {
    await page.setViewportSize({ width: 1440, height: 900 });
    const requestedQueries: string[] = [];
    await mockFiles(page, filesPage());
    await mockFileTrace(page, standardDetail(), transactionsPage());
    page.on('request', (request) => {
      if (request.url().includes('/incoming-nacha-command-center/ingestions?')) requestedQueries.push(request.url());
    });

    await page.goto(centerPath);
    await expect(page.getByRole('heading', { name: 'Seguimiento de archivos NACHA-M', level: 1 })).toBeVisible();
    await expect(page.getByText('$ 1.250.000,25').first()).toBeVisible();
    await expect(page.getByRole('button', { name: 'Abrir calendario' }).first()).toBeVisible();
    await page.screenshot({ path: testInfo.outputPath('listado-principal.png'), fullPage: true });
    await page.getByLabel('Nombre del archivo').fill('0001283');
    await page.getByLabel('Código de resultado').fill('r16');
    await page.getByRole('button', { name: 'Aplicar filtros' }).click();
    await expect.poll(() => requestedQueries.some((url) => url.includes('fileName=0001283') && url.includes('resultCode=R16'))).toBeTruthy();

    await page.getByRole('button', { name: `Ver detalle del archivo ${filesPage().items[0].fileName}` }).first().click();
    await expect(page).toHaveURL(new RegExp(`${centerPath}/files/${ingestionId}`));
    await expect(page.getByText('Progreso del archivo')).toBeVisible();
    await expect(page.getByText('Total crédito').first()).toBeVisible();
    await page.screenshot({ path: testInfo.outputPath('detalle-archivo.png'), fullPage: true });

    await page.getByRole('tab', { name: 'Validaciones' }).click();
    await expect(page.getByText('Fecha del encabezado')).toBeVisible();
    await expect(page.getByText('Continúe con el seguimiento.')).toBeVisible();
    await page.screenshot({ path: testInfo.outputPath('validaciones.png'), fullPage: true });

    await page.getByRole('tab', { name: 'Lotes' }).click();
    await expect(page.getByText('PAGOS PROVEEDORES')).toBeVisible();
    await page.screenshot({ path: testInfo.outputPath('lotes.png'), fullPage: true });

    await page.getByRole('tab', { name: 'Transacciones' }).click();
    await expect(page.getByRole('cell', { name: 'R96 — Operación procesada correctamente' })).toBeVisible();
    await expect(page.getByRole('cell', { name: 'R16 — Cuenta congelada' })).toBeVisible();
    await expect(page.getByRole('cell', { name: 'R17 — Registro no autorizado' })).toBeVisible();
    await expect(page.getByText('No disponible').first()).toBeVisible();
    await page.screenshot({ path: testInfo.outputPath('transacciones.png'), fullPage: true });
    await page.getByRole('button', { name: 'Ver detalle de la transacción 123456789012348' }).click();
    await expect(page.getByRole('heading', { name: '123456789012348' })).toBeVisible();
    await expect(page.getByText('La transacción no tiene addendas asociadas')).toBeVisible();
    await expect(page.getByText('El servicio no respondió dentro del tiempo esperado.', { exact: false })).toBeVisible();
    await expect(page.locator('.result-separation article').filter({ hasText: 'Código ACH' })).toContainText('No disponible');
    await page.screenshot({ path: testInfo.outputPath('detalle-transaccion-error-tecnico.png'), fullPage: true });

    await page.getByRole('tab', { name: 'Procesamiento' }).click();
    await expect(page.getByText('Pendiente de reintento').first()).toBeVisible();
    await expect(page.getByText('2 de 3').first()).toBeVisible();
    await page.screenshot({ path: testInfo.outputPath('procesamiento.png'), fullPage: true });
  });

  test('muestra archivo rechazado por validación y archivo sin transacciones', async ({ page }) => {
    await page.setViewportSize({ width: 1024, height: 768 });
    await mockFiles(page, filesPage());
    await mockFileTrace(page, rejectedDetail(), { items: [], page: 1, pageSize: 10, totalItems: 0 }, [{
      code: 'HEADER_DATE_MISMATCH', title: 'Fecha del encabezado', message: 'La fecha encontrada no corresponde a la fecha operativa.',
      expectedValue: '2026-07-31', foundValue: '2026-07-30', suggestedAction: 'Seleccione el archivo de la fecha vigente.',
      errorType: 'Functional', severity: 'Error', isSuccessful: false, occurredAtUtc: '2026-08-01T14:00:00Z'
    }]);

    await page.goto(`${centerPath}/files/${ingestionId}?seccion=validaciones`);
    await expect(page.getByText('No superada')).toBeVisible();
    await expect(page.getByText('Seleccione el archivo de la fecha vigente.')).toBeVisible();
    await page.getByRole('tab', { name: 'Transacciones' }).click();
    await expect(page.getByText('Este archivo no contiene transacciones para mostrar')).toBeVisible();
  });

  test('pagina desde servidor y conserva filtros en la URL', async ({ page }) => {
    await page.setViewportSize({ width: 768, height: 1024 });
    const requests: string[] = [];
    await page.route(/\/incoming-nacha-command-center\/ingestions\?/, async (route) => {
      requests.push(route.request().url());
      const pageNumber = new URL(route.request().url()).searchParams.get('page');
      await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ ...filesPage(), page: Number(pageNumber ?? 1), totalItems: 40 }) });
    });
    await page.goto(`${centerPath}?fileName=CENIT&page=1&pageSize=20`);
    await page.getByLabel('Paginación de archivos NACHA-M').getByLabel('Página siguiente').click();
    await expect(page).toHaveURL(/page=2/);
    await expect(page).toHaveURL(/fileName=CENIT/);
    await expect.poll(() => requests.some((url) => url.includes('page=2'))).toBeTruthy();
  });

  test('presenta estado vacío y recupera un error de API', async ({ page }) => {
    let attempts = 0;
    await page.route(/\/incoming-nacha-command-center\/ingestions\?/, async (route) => {
      attempts += 1;
      if (attempts === 1) {
        await route.fulfill({ status: 500, contentType: 'application/json', body: '{"message":"fallo controlado"}' });
      } else {
        await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items: [], page: 1, pageSize: 20, totalItems: 0 }) });
      }
    });
    await page.goto(centerPath);
    await expect(page.getByText('No fue posible consultar la información')).toBeVisible();
    await page.getByRole('button', { name: 'Reintentar' }).click();
    await expect(page.getByText('No se encontraron archivos', { exact: true })).toBeVisible();
    const expectedTransportError = 'Failed to load resource: the server responded with a status of 500';
    const errors = browserErrors.get(page) ?? [];
    expect(errors.some((message) => message.includes(expectedTransportError))).toBeTruthy();
    browserErrors.set(page, errors.filter((message) => !message.includes(expectedTransportError)));
  });

  test('mantiene la jerarquía y acciones accesibles en móvil', async ({ page }, testInfo) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await mockFiles(page, filesPage());
    await mockFileTrace(page, standardDetail(), transactionsPage());
    await page.goto(centerPath);
    await expect(page.getByRole('heading', { name: 'Seguimiento de archivos NACHA-M', level: 1 })).toBeVisible();
    const mobileFileAction = page.getByRole('button', { name: `Ver detalle del archivo ${filesPage().items[0].fileName}` });
    await expect(mobileFileAction).toBeVisible();
    await expect(page.locator('body')).not.toHaveCSS('overflow-x', 'scroll');
    await page.screenshot({ path: testInfo.outputPath('listado-movil.png'), fullPage: true });
    await mobileFileAction.click();
    await expect(page.getByRole('button', { name: 'Volver al listado' })).toBeVisible();
    await page.screenshot({ path: testInfo.outputPath('detalle-movil.png'), fullPage: true });
  });
});

async function mockFiles(page: Page, body: unknown): Promise<void> {
  await page.route(/\/incoming-nacha-command-center\/ingestions\?/, (route) => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(body) }));
}

async function mockSummary(page: Page): Promise<void> {
  await page.route(/\/incoming-nacha-command-center\/observability\/summary/, (route) => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({
    generatedAtUtc: '2026-08-01T15:00:00Z', windowHours: 168,
    pipelineHealth: { totalIngestions: 1, totalQueueItems: 4, backlogItems: 1, blockedItems: 0, retryPendingItems: 1, waitingWindowItems: 0, failedFinalItems: 0, confirmedItems: 3, averageQueueAgeMinutes: 4, oldestQueueAgeMinutes: 8 },
    ingestionsByStatus: [], queueByStatus: [], byClearingHouseCycle: [], topErrors: [], timeline: []
  }) }));
}

async function mockSharedCatalogs(page: Page): Promise<void> {
  await page.route(/\/api\/clearing-houses(?:\?|$)/, (route) => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify({ items: [{ id: 1, name: 'CENIT', code: 'CENIT', isActive: true }], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 }) }));
}

async function mockFileTrace(page: Page, detail: unknown, transactions: unknown, validations = defaultValidations()): Promise<void> {
  await page.route(new RegExp(`/incoming-nacha-command-center/ingestions/${ingestionId}$`), (route) => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(detail) }));
  await page.route(new RegExp(`/incoming-nacha-command-center/ingestions/${ingestionId}/validations$`), (route) => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(validations) }));
  await page.route(new RegExp(`/incoming-nacha-command-center/ingestions/${ingestionId}/batches\?`), (route) => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(batchesPage()) }));
  await page.route(new RegExp(`/incoming-nacha-command-center/ingestions/${ingestionId}/transactions\?`), (route) => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(transactions) }));
  await page.route(new RegExp(`/incoming-nacha-command-center/ingestions/${ingestionId}/transactions/12/addendas$`), (route) => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify([{ id: 1, typeCode: '05', sequence: '0001', returnReasonCode: '', originalTraceNumber: '****2345', paymentInformation: 'Información protegida · …1234' }]) }));
  await page.route(new RegExp(`/incoming-nacha-command-center/ingestions/${ingestionId}/transactions/15/addendas$`), (route) => route.fulfill({ status: 200, contentType: 'application/json', body: '[]' }));
  await page.route(/\/incoming-nacha-command-center\/queue\/queue-tech$/, (route) => route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(queueDetail()) }));
}

function filesPage() {
  return { items: [{ id: ingestionId, fileName: '0001283.001.20260731.1', correlationId: 'corr-file', ingestionStatus: 'Completado', ingestionStatusText: 'Completado', stageCode: 'Persisted', stageText: 'Carga completada', cycleResolutionStatus: 'ResueltoConfirmado', parsingStatus: 'Exitoso', resolvedClearingHouseId: 1, clearingHouseName: 'CENIT', resolvedAchCycleId: 'CICLO-01', operationalDate: '2026-07-31', uploadedAtUtc: '2026-08-01T14:00:00Z', uploadedBy: 'operador', queueItems: 4, processingEvents: 8, totalBatches: 1, totalTransactions: 4, totalDebit: 0, totalCredit: 1250000.25, processingStatusText: 'Procesado', overallResultText: 'Procesado con novedades', scheduledAtUtc: '2026-08-01T14:02:00Z', hasTechnicalErrors: true, hasIssues: true }], page: 1, pageSize: 20, totalItems: 1 };
}

function standardDetail() {
  return { id: ingestionId, fileName: filesPage().items[0].fileName, correlationId: 'correlation-1234567890', ingestionStatus: 'Completado', ingestionStatusText: 'Completado', stageCode: 'Persisted', stageText: 'Carga completada', cycleResolutionStatus: 'ResueltoConfirmado', parsingStatus: 'Exitoso', resolvedClearingHouseId: 1, clearingHouseName: 'CENIT', resolvedAchCycleId: 'CICLO-01', operationalDate: '2026-07-31', notes: '', uploadedBy: 'operador', uploadedAtUtc: '2026-08-01T14:00:00Z', receivedAtUtc: '2026-08-01T14:00:00Z', overallResultText: 'Procesado con novedades', pendingTransactions: 1, summary: { totalBatches: 1, totalTransactions: 4, totalAddendas: 1, totalDebit: 0, totalCredit: 1250000.25, successfulTransactions: 1, rejectedTransactions: 1, returnedTransactions: 1, technicalFailures: 1 }, admissionIssue: null, queue: [{ id: 'queue-tech', ingestionId, entryDetailId: 15, queueStatus: 'RetryPending', queueStatusText: 'Pendiente de reintento', attemptCount: 2, maxAttempts: 3, nextAttemptAtUtc: '2026-08-01T16:00:00Z', scheduledAtUtc: '2026-08-01T14:02:00Z', soapOperation: 'Proc_Transacciones', lastErrorCode: 'SOAP_TIMEOUT', lastErrorMessage: 'timeout', lastResponseCode: '' }], events: [] };
}

function rejectedDetail() {
  return { ...standardDetail(), ingestionStatus: 'Fallido', ingestionStatusText: 'Rechazado', stageCode: 'Rejected', stageText: 'Rechazado', overallResultText: 'Rechazado durante la validación', pendingTransactions: 0, summary: { ...standardDetail().summary, totalBatches: 0, totalTransactions: 0, totalAddendas: 0, totalCredit: 0 }, queue: [], admissionIssue: { code: 'HEADER_DATE_MISMATCH', title: 'Fecha del encabezado', message: 'La fecha no corresponde.', suggestedAction: 'Seleccione el archivo correcto.', severity: 'Error' } };
}

function defaultValidations() {
  return [{ code: 'ADMISSION_ACCEPTED', title: 'Fecha del encabezado', message: 'La fecha corresponde a la operación.', expectedValue: '2026-07-31', foundValue: '2026-07-31', suggestedAction: 'Continúe con el seguimiento.', errorType: 'Functional', severity: 'Information', isSuccessful: true, occurredAtUtc: '2026-08-01T14:00:00Z' }];
}

function batchesPage() {
  return { items: [{ id: 7, batchNumber: 1, companyName: 'Empresa', serviceClassCode: '220', standardEntryClassCode: 'PPD', companyEntryDescription: 'PAGOS PROVEEDORES', effectiveEntryDate: '260731', totalTransactions: 4, totalAmount: 1250000.25, totalDebit: 0, totalCredit: 1250000.25 }], page: 1, pageSize: 10, totalItems: 1 };
}

function transactionsPage() {
  const base = { batchId: 7, batchNumber: 1, transactionCode: '22', transactionCodeDescription: 'Crédito a cuenta corriente', amount: 100, addendaCount: 1, classificationCode: 'CreditoEntrante', classificationText: 'Crédito entrante', dispatchStatusCode: 'Confirmed', dispatchStatusText: 'Procesado', attemptCount: 1, maxAttempts: 3, processingStatus: 'Completed', processingStatusText: 'Procesado', correlationId: 'corr', clearingHouseId: 1, achCycleId: 'CICLO-01', soapOperation: 'Proc_Transacciones', externalTransactionId: 'EXT', technicalErrorCode: '', technicalErrorMessage: '', accountNumberMasked: '****1234', originInstitution: '****0001', destinationInstitution: '****0002', recipientNameMasked: 'P***', effectiveEntryDate: '260731', processedAtUtc: '2026-08-01T14:10:00Z' };
  return { items: [
    { ...base, id: 12, traceNumber: '123456789012345', businessOutcome: 'Successful', businessOutcomeText: 'Exitoso', resultCode: 'R96', resultDescription: 'Operación procesada correctamente' },
    { ...base, id: 13, traceNumber: '123456789012346', businessOutcome: 'Rejected', businessOutcomeText: 'Rechazado', resultCode: 'R16', resultDescription: 'Cuenta congelada' },
    { ...base, id: 14, traceNumber: '123456789012347', businessOutcome: 'Returned', businessOutcomeText: 'Devuelto', resultCode: 'R17', resultDescription: 'Registro no autorizado' },
    { ...base, id: 15, traceNumber: '123456789012348', dispatchQueueId: 'queue-tech', addendaCount: 0, attemptCount: 2, processingStatus: 'TechnicalFailed', processingStatusText: 'Error técnico', businessOutcome: 'NotProcessed', businessOutcomeText: 'No procesado', resultCode: '', resultDescription: '', externalTransactionId: '', technicalErrorCode: 'SOAP_TIMEOUT', technicalErrorMessage: 'timeout', processedAtUtc: null, scheduledAtUtc: '2026-08-01T14:02:00Z', nextRetryAtUtc: '2026-08-01T16:00:00Z' }
  ], page: 1, pageSize: 10, totalItems: 4 };
}

function queueDetail() {
  return { queue: standardDetail().queue[0], classification: { functionalClass: 'CreditoEntrante', eligibilityStatus: 'Elegible', requiresManualResolution: false, prenoteStatus: 'NoAplica', businessMeaning: 'Crédito entrante elegible' }, executions: [{ id: 'attempt-2', attemptNumber: 2, methodName: 'Proc_Transacciones', correlationId: 'corr', processingStatusText: 'Error técnico', businessOutcomeText: 'No procesado', resultCode: '', resultDescription: '', isSuccess: false, isRetryable: true, startedAtUtc: '2026-08-01T14:10:00Z', durationMs: 30000, transportStatusText: 'Tiempo de espera agotado', technicalErrorCode: 'SOAP_TIMEOUT', technicalErrorMessage: 'timeout', externalTransactionId: '' }], events: [] };
}

async function authenticatedSession(page: Page): Promise<void> {
  const now = Math.floor(Date.now() / 1000);
  const token = createUnsignedJwt({ unique_name: 'operador.e2e', uid: 'operador-e2e', role: ['Admin', 'ACH.Operator'], permission: ['CanReadAch'], exp: now + 3600, iat: now });
  await page.addInitScript((value) => window.sessionStorage.setItem('ach.interbank.access_token', value), token);
  await page.route('**/auth/refresh**', (route) => route.fulfill({
    status: 200,
    contentType: 'application/json',
    headers: {
      'access-control-allow-origin': 'http://localhost:4200',
      'access-control-allow-credentials': 'true'
    },
    body: JSON.stringify({
      statusCode: 200,
      sucess: true,
      data: { token, username: 'operador.e2e', fullName: 'Operador E2E', roles: ['Admin', 'ACH.Operator'], permissions: ['CanReadAch'] }
    })
  }));
  await page.route(/\/api\/navigation\/menu$/, (route) => route.fulfill({
    status: 200,
    contentType: 'application/json',
    headers: corsHeaders(),
    body: '[]'
  }));
  await page.route(/\/api\/navigation-logs$/, (route) => route.fulfill({
    status: 200,
    contentType: 'application/json',
    headers: corsHeaders(),
    body: '{}'
  }));
  await page.route(/\/api\/users\/branding$/, (route) => route.fulfill({
    status: 200,
    contentType: 'application/json',
    headers: corsHeaders(),
    body: '{}'
  }));
}

function corsHeaders(): Record<string, string> {
  return {
    'access-control-allow-origin': 'http://localhost:4200',
    'access-control-allow-credentials': 'true'
  };
}

function createUnsignedJwt(payload: Record<string, unknown>): string {
  return `${encode({ alg: 'none', typ: 'JWT' })}.${encode(payload)}.e2e`;
}

function encode(value: Record<string, unknown>): string {
  return Buffer.from(JSON.stringify(value)).toString('base64url');
}
