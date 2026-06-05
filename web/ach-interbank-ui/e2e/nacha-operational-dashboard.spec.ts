import { expect, Page, test } from '@playwright/test';

const dashboardPath = '/ach/nacha/operational-dashboard';
const dashboardEndpoint = /\/api\/ach\/nacha\/operational\/dashboard$/;
const fileDetailEndpoint = /\/api\/ach\/nacha\/operational\/files\/e2e-ach-in-001$/;
const refreshEndpoint = /\/auth\/refresh$/;
const legacyNachaEndpoint = /\/(?:nacha-layouts|nacha-record-definitions)(?:\/|\?|$)/;
const hashExportPattern = /\/NachaExport\/[a-f0-9]{32,64}$/i;

test.describe('NACHA-M operational dashboard read-only evidence', () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthenticatedSession(page);
    await mockAuthRefresh(page);
  });

  test('Dashboard_ShouldRenderReadOnlyOperationalEvidence', async ({ page }, testInfo) => {
    await mockDashboard(page, backendDashboard());

    await page.goto(dashboardPath);

    await expect(page.getByRole('heading', { name: 'Consulta operativa NACHA-M y readiness SOAP', level: 1 })).toBeVisible();
    await expect(page.getByText('Productivo NO-GO')).toBeVisible();
    await expect(page.getByText('SOAP REAL DESHABILITADO', { exact: true })).toBeVisible();
    await expect(page.getByText('BACKEND READ-ONLY SANITIZADO')).toBeVisible();
    await expect(page.getByText('Fuente: backend read-only')).toBeVisible();
    await expect(page.locator('section[aria-label="Resumen operativo"]')).toBeVisible();
    await expect(page.getByText('ProductiveExecution')).toBeVisible();
    await expect(page.getByText('WouldInvokeRealSoap')).toBeVisible();
    await expect(page.getByText('false').first()).toBeVisible();

    await expect(page.getByText('Archivos NACHA-M')).toBeVisible();
    await expect(page.getByText('Decisiones funcionales')).toBeVisible();
    await expect(page.getByText('Preparación SOAP/UAT')).toBeVisible();
    await expect(page.getByText('Auditoría Fase 6B.5')).toBeVisible();

    await assertDangerousActionsAbsent(page);
    await page.screenshot({ path: testInfo.outputPath('dashboard-full-page.png'), fullPage: true });
  });

  test('Dashboard_ShouldShowDemoFallbackWhenBackendUnavailable', async ({ page }) => {
    await page.route(dashboardEndpoint, async (route) => {
      await route.fulfill({ status: 500, contentType: 'application/json', body: '{"message":"fallback test"}' });
    });

    await page.goto(dashboardPath);

    await expect(page.getByText('DEMO READ-ONLY')).toBeVisible();
    await expect(page.getByText('Fuente: demo seguro')).toBeVisible();
    await expect(page.getByText('Productivo NO-GO')).toBeVisible();
    await expect(page.getByText('SOAP REAL DESHABILITADO', { exact: true })).toBeVisible();
    await assertDangerousActionsAbsent(page);
  });

  test('Dashboard_ShouldNotExposeDangerousActions', async ({ page }) => {
    await mockDashboard(page, backendDashboard());

    await page.goto(dashboardPath);

    await expect(page.getByRole('heading', { name: 'Consulta operativa NACHA-M y readiness SOAP', level: 1 })).toBeVisible();
    await assertDangerousActionsAbsent(page);
    await expect(page.getByText('Error')).toHaveCount(0);
  });

  test('Dashboard_ShouldShowBackendReadOnlySourceOrSafeFallback', async ({ page }) => {
    await mockDashboard(page, backendDashboard({ isPartialData: true, dataSource: 'parcial' }));

    await page.goto(dashboardPath);

    await expect(page.getByText('Fuente: parcial')).toBeVisible();
    await expect(page.getByText('Datos operativos parciales/read-only')).toBeVisible();
  });

  test('Dashboard_ShouldKeepNoGoAndReadOnlyStateAfterReadStoreChange', async ({ page }) => {
    await mockDashboard(page, backendDashboard({ backendPhase: '6C.3', soapMode: 'ReadOnly' }));

    await page.goto(dashboardPath);

    await expect(page.getByText('Productivo NO-GO')).toBeVisible();
    await expect(page.getByText('SOAP REAL DESHABILITADO', { exact: true })).toBeVisible();
    await expect(page.getByText('Fuente: backend read-only')).toBeVisible();
    await assertDangerousActionsAbsent(page);
  });

  test('Dashboard_ShouldNotCallLegacyLayoutsOrDefinitions', async ({ page }) => {
    const legacyRequests: string[] = [];
    page.on('request', request => {
      const url = request.url();
      if (legacyNachaEndpoint.test(url)) {
        legacyRequests.push(url);
      }
    });
    await mockDashboard(page, backendDashboard());

    await page.goto(dashboardPath);

    await expect(page.getByRole('heading', { name: 'Consulta operativa NACHA-M y readiness SOAP', level: 1 })).toBeVisible();
    expect(legacyRequests).toEqual([]);
  });

  test('Dashboard_ShouldNavigateToFileDetail', async ({ page }) => {
    await mockDashboard(page, backendDashboard());
    await mockFileDetail(page, fileDetail());

    await page.goto(dashboardPath);
    await page.getByRole('button', { name: /Ver detalle ACH_COL_IN_001\.ach/i }).click();

    await expect(page).toHaveURL(/\/ach\/nacha\/operational-dashboard\/files\/e2e-ach-in-001$/);
    await expect(page.getByRole('heading', { name: 'Detalle operativo NACHA-M', level: 1 })).toBeVisible();
  });

  test('FileDetail_ShouldLoadReadOnlyPage', async ({ page }) => {
    const blockedRequests: string[] = [];
    page.on('request', request => {
      const url = request.url();
      if (legacyNachaEndpoint.test(url) || hashExportPattern.test(url)) {
        blockedRequests.push(url);
      }
      if (/\/api\/ach\/nacha\/operational\/files\//.test(url) && !['GET'].includes(request.method())) {
        blockedRequests.push(`${request.method()} ${url}`);
      }
    });
    await mockFileDetail(page, fileDetail());

    await page.goto(`${dashboardPath}/files/e2e-ach-in-001`);

    await expect(page.getByText('Productivo NO-GO')).toBeVisible();
    await expect(page.getByText('Backend read-only sanitizado')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Encabezado' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Lotes' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Entradas' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Addendas' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Controles' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Totales' })).toBeVisible();
    await assertDangerousActionsAbsent(page);
    expect(blockedRequests).toEqual([]);
  });
});

async function seedAuthenticatedSession(page: Page): Promise<void> {
  const token = createUnsignedJwt({
    unique_name: 'uat.evidence',
    name: 'Usuario UAT Evidencia',
    uid: 'uat-evidence',
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
    unique_name: 'uat.evidence',
    name: 'Usuario UAT Evidencia',
    uid: 'uat-evidence',
    role: ['Admin', 'ACH.Operator'],
    permission: ['CanReadAch', 'CanManageAch'],
    exp: Math.floor(Date.now() / 1000) + 3600,
    iat: Math.floor(Date.now() / 1000)
  });

  await page.route(refreshEndpoint, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        sucess: true,
        data: {
          token,
          username: 'uat.evidence',
          fullName: 'Usuario UAT Evidencia',
          roles: ['Admin', 'ACH.Operator'],
          permissions: ['CanReadAch', 'CanManageAch']
        }
      })
    });
  });
}

async function mockDashboard(page: Page, body: unknown): Promise<void> {
  await page.route(dashboardEndpoint, async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(body) });
  });
}

async function mockFileDetail(page: Page, body: unknown): Promise<void> {
  await page.route(fileDetailEndpoint, async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify(body) });
  });
}

async function assertDangerousActionsAbsent(page: Page): Promise<void> {
  const dangerousLabels = [
    /Ejecutar SOAP real/i,
    /Ejecutar movimiento/i,
    /Mover dinero/i,
    /Cambiar a GO/i,
    /Activar productivo/i,
    /Confirmar operaci[oó]n real/i,
    /Editar perfil/i,
    /Subir archivo productivo/i,
    /Invocar core/i,
    /Reintentar ejecuci[oó]n real/i
  ];

  for (const label of dangerousLabels) {
    await expect(page.getByRole('button', { name: label })).toHaveCount(0);
  }
}

function backendDashboard(overrides: { isPartialData?: boolean; dataSource?: string; backendPhase?: string; soapMode?: string } = {}) {
  const isPartialData = overrides.isPartialData ?? false;
  const dataSource = overrides.dataSource ?? (isPartialData ? 'parcial' : 'backend read-only');

  return {
    summary: {
      productiveStatus: 'NO-GO',
      backendPhase: overrides.backendPhase ?? '6B.5.6',
      soapMode: overrides.soapMode ?? 'Simulated',
      productiveExecution: false,
      wouldInvokeRealSoap: false,
      totalFiles: 3,
      totalIncomingFiles: 2,
      totalOutgoingFiles: 0,
      totalReturnFiles: 1,
      totalDecisions: 3,
      totalSoapCandidates: 2,
      totalNoGoBlocks: 1,
      totalManualReview: 1,
      totalReadinessChecks: 2,
      lastUpdatedAt: '2026-05-25T04:00:00Z',
      isDemoData: false,
      isPartialData,
      dataSource,
      warnings: isPartialData ? ['No persisted SOAP readiness data found; using safe read-only placeholder.'] : []
    },
    files: [
      {
        fileId: 'e2e-ach-in-001',
        fileName: 'ACH_COL_IN_001.ach',
        dataSource: 'backend read-only',
        headerId: 'N1',
        persistedRecordCount: 6,
        lastParsedAt: '2026-05-25T04:00:00Z',
        noSensitiveData: true,
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
        receivedAt: '2026-05-25T04:00:00Z',
        createdAt: '2026-05-25T04:00:00Z',
        correlationId: 'phase-6c2e-ach-in',
        hasErrors: false,
        warningCount: 0,
        errorCount: 0
      }
    ],
    decisions: [
      {
        correlationId: 'phase-6c2e-ach-in',
        fileName: 'ACH_COL_IN_001.ach',
        entryTraceNumber: '900000010000001',
        originalTraceNumber: null,
        decisionType: 'ApplyCreditMovement',
        soapOperationCandidate: 'ProcTransacciones',
        requiresMonetaryMovement: true,
        reasonCode: '00',
        reasonDescription: 'Evidencia E2E UAT simulada',
        newInternalStatus: 'Accepted',
        manualReviewRequired: false,
        isBlocked: false,
        blockReason: null,
        dataSource: 'backend read-only',
        isDerived: true,
        isPersisted: true,
        warning: null,
        createdAt: '2026-05-25T04:00:00Z'
      }
    ],
    readiness: [
      {
        correlationId: 'phase-6c2e-ach-in',
        operationCandidate: 'ProcTransacciones',
        isReadyForUat: true,
        isBlocked: false,
        blockReasons: [],
        payloadMappingPassed: true,
        requestMappingPassed: true,
        operationalGatePassed: true,
        readinessCheckPassed: true,
        simulationPassed: true,
        resiliencePassed: true,
        wouldInvokeRealSoap: false,
        productiveExecution: false,
        requiresMonetaryMovement: true,
        phase: '6B.5',
        dataSource: 'backend read-only',
        isDerived: true,
        isPersisted: true,
        warning: null,
        lastCheckedAt: '2026-05-25T04:00:00Z'
      }
    ],
    audit: [
      {
        correlationId: 'phase-6c2e-ach-in',
        phase: '6B.5',
        eventType: 'ReadinessDashboardProjected',
        severity: 'Information',
        message: 'Evidencia E2E read-only generada.',
        isBlocked: false,
        dataSource: 'backend read-only',
        isDerived: false,
        isPersisted: true,
        warning: null,
        timestamp: '2026-05-25T04:00:00Z',
        sanitizedDetails: {
          Phase: '6B.5',
          Productivo: 'NO-GO',
          WouldInvokeRealSoap: 'false'
        }
      }
    ],
    generatedAt: '2026-05-25T04:00:00Z',
    isDemoData: false,
    isPartialData,
    dataSource,
    warnings: isPartialData ? ['No persisted SOAP readiness data found; using safe read-only placeholder.'] : [],
    productiveStatus: 'NO-GO'
  };
}

function fileDetail() {
  return {
    fileId: 'e2e-ach-in-001',
    headerId: 'N1',
    fileName: 'ACH_COL_IN_001.ach',
    clearingHouseCode: 'ACH',
    profileCode: 'nacha-config profiles',
    flowType: 'IncomingPersisted',
    isReturnFile: false,
    processingStatus: 'Processed',
    validationPassed: true,
    receivedAt: '2026-05-25T04:00:00Z',
    createdAt: '2026-05-25T04:00:00Z',
    correlationId: 'corr-e2e',
    dataSource: 'backend read-only',
    isPartialData: false,
    warnings: ['Productivo permanece NO-GO; esta consulta no ejecuta SOAP ni movimientos.'],
    header: { headerId: 'N1', priorityCode: '01', recordSize: '094', blockingFactor: '10', cycleNumber: 1 },
    batches: [{ batchId: 1, batchNumber: 1, serviceClassCode: '220', companyName: 'CFA', standardEntryClassCode: 'PPD' }],
    entries: [{ entryDetailId: 1, transactionCode: '22', accountNumberMasked: '****3456', recipIdNumberMasked: '****6789', amount: 100 }],
    addendas: [{ addendaId: 1, codeTypeAddendumRecord: '05', invoiceOrAccountNumberMasked: '****1111' }],
    batchControls: [{ batchControlId: 1, batchNumber: '1', entryAddendaCount: 2, entryHash: 1, totalDebitAmount: 0, totalCreditAmount: 100 }],
    fileControls: [{ fileControlId: 1, batchCount: 1, blockCount: 1, entryAddendaCount: 2, entryHash: 1, totalDebitAmount: 0, totalCreditAmount: 100 }],
    totalsSummary: {
      batchCount: 1,
      entryCount: 1,
      addendaCount: 1,
      batchControlCount: 1,
      fileControlCount: 1,
      persistedRecordCount: 5,
      totalDebitAmount: 0,
      totalCreditAmount: 100,
      validationPassed: true
    },
    noSensitiveData: true
  };
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
