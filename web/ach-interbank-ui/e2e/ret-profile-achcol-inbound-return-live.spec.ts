import { expect, Page, test, TestInfo } from '@playwright/test';
import path from 'node:path';
import { loginThroughUi } from './support/live-ui-auth';
import { G36SqlServer, sqlString } from './support/g36-sqlserver';

const originalEnvelopePath = path.resolve(
  __dirname,
  '../../../docs/uat/certificados_pruebas/archivo_prueba/ACH Colombia/0001283.003.20260727.1.OUT.env'
);
const originalFileName = path.basename(originalEnvelopePath);
const operationalDate = '2026-07-27';
const targetProfile = 'OFFICIAL_ACH_ENTRADA_DEVOLUCION_V1_0';
const returnReason = 'R04';

test.use({ trace: 'retain-on-failure', video: 'off', screenshot: 'only-on-failure' });

test('archivo cifrado original resuelve el perfil Return y crea una única huérfana física', async ({ page }, testInfo) => {
  test.setTimeout(300_000);
  const database = new G36SqlServer();
  database.assertReady();
  const runtime = observeRuntime(page);

  await loginThroughUi(page);
  await verifyOperationalCertificates(page);
  await ensureOperationalCycle(page, database);

  const firstUpload = await uploadOriginalEnvelope(page, true);
  expect(firstUpload.status).toBe(200);
  const resumedFromCompletedEvidence = firstUpload.body.ingestionStatus === 'Duplicado';
  if (resumedFromCompletedEvidence) {
    expect(firstUpload.body.success).toBe(false);
  } else {
    expect(firstUpload.body.success).toBe(true);
    expect(firstUpload.body.ingestionStatus).toBe('Completado');
    expect(firstUpload.body.profileSelectionStatus).toBe('ProfileSelected');
    expect(firstUpload.body.selectedProfileCode).toBe(targetProfile);
    expect(firstUpload.body.selectedProfileVersion).toBe('1.0');
    expect(firstUpload.body.totalBatches).toBe(15);
    expect(firstUpload.body.totalEntries).toBe(2);
    expect(firstUpload.body.totalAddendas).toBe(2);
    expect(firstUpload.body.errors.join(' ')).not.toContain('ProfileNotFound');
  }

  const ingestionId = resumedFromCompletedEvidence
    ? completedIngestionId(database)
    : firstUpload.body.ingestionId!;
  expect(ingestionId).toBeTruthy();
  const persistence = readPersistence(database, ingestionId);
  expect(persistence.headers).toBe(1);
  expect(persistence.batches).toBe(15);
  expect(persistence.entries).toBe(2);
  expect(persistence.addendas).toBe(2);
  expect(persistence.batchControls).toBe(15);
  expect(persistence.fileControls).toBe(1);
  expect(persistence.classifications).toBe(2);
  expect(persistence.links).toBe(2);
  expect(persistence.returnAddendas).toBe(1);
  expect(persistence.nonFinalLinks).toBe(2);

  const returnEvidence = database.query<{ reasonCode: string; originalTraceLength: number }>(
    `SELECT TOP (1) a.[ReturnReasonCode] AS [reasonCode], LEN(a.[OriginalTraceNumber]) AS [originalTraceLength]
     FROM [AddendaRecords] a
     JOIN [NachaHeaders] h ON h.[NachaID] = a.[NachaID]
     WHERE h.[IncomingNachaFileIngestionId] = ${sqlString(ingestionId)}
       AND a.[BusinessType] = N'Return'`
  );
  expect(returnEvidence).toHaveLength(1);
  expect(returnEvidence[0].reasonCode).toBe(returnReason);
  expect(returnEvidence[0].originalTraceLength).toBe(15);

  await showPhysicalOrphan(page, testInfo);

  const duplicateUpload = resumedFromCompletedEvidence
    ? firstUpload
    : await uploadOriginalEnvelope(page, false);
  expect(duplicateUpload.status).toBe(200);
  expect(duplicateUpload.body.success).toBe(false);
  expect(duplicateUpload.body.ingestionStatus).toBe('Duplicado');
  expect(duplicateUpload.body.message).toBe('Archivo duplicado detectado.');

  const afterReplay = readPersistence(database, ingestionId);
  expect(afterReplay).toEqual(persistence);
  expect(database.scalar<number>(
    `SELECT COUNT(*) AS [value] FROM [IncomingNachaFileIngestions] WHERE [FileName] = ${sqlString(originalFileName)}`
  )).toBe(1);

  expect(runtime.soapRequests).toEqual([]);
  expect(runtime.moneyRequests).toEqual([]);
  expect(runtime.consoleErrors).toEqual([]);
  expect(runtime.requestFailures).toEqual([]);

  await testInfo.attach('ret-gap-007-upload-evidence.json', {
    body: JSON.stringify({
      originalFileName,
      ingestionId,
      targetProfile,
      profileVersion: firstUpload.body.selectedProfileVersion,
      returnReason,
      originalTracePreserved: returnEvidence[0].originalTraceLength === 15,
      persistence,
      duplicateStatus: duplicateUpload.body.ingestionStatus,
      resumedFromCompletedEvidence,
      soapRequests: runtime.soapRequests.length,
      moneyRequests: runtime.moneyRequests.length
    }, null, 2),
    contentType: 'application/json'
  });
});

async function verifyOperationalCertificates(page: Page): Promise<void> {
  await page.goto('/nacha-security/certificates', { waitUntil: 'networkidle' });
  await expect(page.getByRole('heading', { name: 'Administración de certificados de seguridad' })).toBeVisible();
  await expect(page.getByText('Certificado operativo de CFA', { exact: true })).toBeVisible();
  await expect(page.getByText('Disponible', { exact: true }).first()).toBeVisible();
  await expect(page.getByText('Cámaras con certificado operativo', { exact: true })).toBeVisible();
  await expect(page.getByText('1 de 2', { exact: true })).toBeVisible();
}

function completedIngestionId(database: G36SqlServer): string {
  const rows = database.query<{ id: string }>(
    `SELECT TOP (1) CONVERT(nvarchar(36), [Id]) AS [id]
     FROM [IncomingNachaFileIngestions]
     WHERE [FileName] = ${sqlString(originalFileName)} AND [IngestionStatus] = N'Completado'
     ORDER BY [CreatedAt] DESC`
  );
  expect(rows).toHaveLength(1);
  return rows[0].id;
}

async function ensureOperationalCycle(page: Page, database: G36SqlServer): Promise<void> {
  const existing = database.scalar<number>(
    `SELECT COUNT(*) AS [value]
     FROM [AchCycles] c
     JOIN [ClearingHouses] h ON h.[Id] = c.[ClearingHouseId]
     WHERE h.[Code] = N'ACHCOL' AND c.[ProcessingDate] = '${operationalDate}' AND c.[CycleName] = N'Ciclo 3'`
  );
  if (existing === 1) return;

  const config = database.query<{
    clearingHouseId: number;
    configId: number;
    cycleName: string;
    startTime: string;
    endTime: string;
    cutoffTime: string;
  }>(
    `SELECT TOP (1) h.[Id] AS [clearingHouseId], c.[Id] AS [configId], c.[CycleName] AS [cycleName],
            CONVERT(varchar(8), c.[StartTime]) AS [startTime], CONVERT(varchar(8), c.[EndTime]) AS [endTime],
            CONVERT(varchar(8), c.[CutoffTime]) AS [cutoffTime]
     FROM [ClearingHouses] h
     JOIN [ClearingHouseCycleConfigs] c ON c.[ClearingHouseId] = h.[Id]
     WHERE h.[Code] = N'ACHCOL' AND c.[CycleName] = N'Ciclo 3' AND c.[IsActive] = 1`
  );
  expect(config).toHaveLength(1);

  const result = await page.evaluate(async ({ cycle, date }) => {
    const token = window.sessionStorage.getItem('ach.interbank.access_token');
    const response = await fetch('/api/ach-cycles', {
      method: 'POST',
      headers: { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' },
      body: JSON.stringify({
        cycleName: cycle.cycleName,
        processingDate: date,
        startTime: cycle.startTime,
        endTime: cycle.endTime,
        cutoffTime: cycle.cutoffTime,
        rescheduleOnHoliday: false,
        clearingHouseId: cycle.clearingHouseId,
        clearingHouseCycleConfigId: cycle.configId
      })
    });
    return { status: response.status };
  }, { cycle: config[0], date: operationalDate });

  expect(result.status).toBe(201);
  expect(database.scalar<number>(
    `SELECT COUNT(*) AS [value]
     FROM [AchCycles] c
     JOIN [ClearingHouses] h ON h.[Id] = c.[ClearingHouseId]
     WHERE h.[Code] = N'ACHCOL' AND c.[ProcessingDate] = '${operationalDate}' AND c.[CycleName] = N'Ciclo 3'`
  )).toBe(1);
}

async function uploadOriginalEnvelope(page: Page, resumeFailedIngestion: boolean): Promise<UploadResult> {
  await page.goto('/transactions/nacha-upload');
  await expect(page.getByText('Cargar archivo NACHA-M', { exact: true })).toBeVisible();
  const clearingHouseSelect = page.locator('app-clearing-house-select select');
  await expect(clearingHouseSelect).toBeEnabled();
  await clearingHouseSelect.selectOption({ label: 'ACH Colombia (ACHCOL)' });
  await page.locator('input[type="file"]').setInputFiles(originalEnvelopePath);

  const responsePromise = page.waitForResponse(response =>
    response.request().method() === 'POST' && new URL(response.url()).pathname === '/NachaUpload/upload'
  );
  await page.getByRole('button', { name: 'Cargar archivo' }).click();
  const response = await responsePromise;
  const result = { status: response.status(), body: await response.json() as UploadResponse };

  if (resumeFailedIngestion
      && ['Fallido', 'Duplicado'].includes(result.body.ingestionStatus ?? '')
      && result.body.parsingStatus === 'FallidoReprocesable') {
    const reprocessButton = page.getByTestId('nacha-reprocess-button');
    await expect(reprocessButton).toBeVisible();
    const reprocessResponsePromise = page.waitForResponse(candidate =>
      candidate.request().method() === 'POST' && new URL(candidate.url()).pathname === '/NachaUpload/upload'
    );
    await reprocessButton.click();
    const reprocessResponse = await reprocessResponsePromise;
    return { status: reprocessResponse.status(), body: await reprocessResponse.json() as UploadResponse };
  }

  return result;
}

async function showPhysicalOrphan(page: Page, testInfo: TestInfo): Promise<void> {
  await page.goto('/incoming-nacha-command-center/orphan-resolution');
  await expect(page.getByRole('heading', { name: 'Devoluciones recibidas sin relación', level: 1 })).toBeVisible();
  await page.getByLabel('Archivo, causal o número de rastreo').fill(originalFileName);
  await page.getByRole('button', { name: 'Buscar' }).click();
  const returnRow = page.getByRole('row')
    .filter({ hasText: originalFileName })
    .filter({ hasText: returnReason });
  await expect(returnRow).toHaveCount(1);
  await expect(returnRow).toBeVisible();
  const screenshotPath = testInfo.outputPath('01-return-r04-fisica-sin-relacion.png');
  await page.screenshot({ path: screenshotPath, fullPage: true });
  await testInfo.attach('01-return-r04-fisica-sin-relacion.png', { path: screenshotPath, contentType: 'image/png' });
}

function readPersistence(database: G36SqlServer, ingestionId: string): PersistenceEvidence {
  const id = sqlString(ingestionId);
  const row = database.query<PersistenceEvidence>(
    `SELECT
       (SELECT COUNT(*) FROM [NachaHeaders] h WHERE h.[IncomingNachaFileIngestionId] = ${id}) AS [headers],
       (SELECT COUNT(*) FROM [BatchHeaders] b JOIN [NachaHeaders] h ON h.[NachaID] = b.[NachaID] WHERE h.[IncomingNachaFileIngestionId] = ${id}) AS [batches],
       (SELECT COUNT(*) FROM [EntryDetails] e JOIN [NachaHeaders] h ON h.[NachaID] = e.[NachaID] WHERE h.[IncomingNachaFileIngestionId] = ${id}) AS [entries],
       (SELECT COUNT(*) FROM [AddendaRecords] a JOIN [NachaHeaders] h ON h.[NachaID] = a.[NachaID] WHERE h.[IncomingNachaFileIngestionId] = ${id}) AS [addendas],
       (SELECT COUNT(*) FROM [BatchControls] b JOIN [NachaHeaders] h ON h.[NachaID] = b.[NachaID] WHERE h.[IncomingNachaFileIngestionId] = ${id}) AS [batchControls],
       (SELECT COUNT(*) FROM [FileControls] f JOIN [NachaHeaders] h ON h.[NachaID] = f.[NachaID] WHERE h.[IncomingNachaFileIngestionId] = ${id}) AS [fileControls],
       (SELECT COUNT(*) FROM [IncomingNachaEntryClassifications] c WHERE c.[IncomingNachaFileIngestionId] = ${id}) AS [classifications],
       (SELECT COUNT(*) FROM [IncomingNachaTransactionLinks] l WHERE l.[IncomingNachaFileIngestionId] = ${id}) AS [links],
       (SELECT COUNT(*) FROM [AddendaRecords] a JOIN [NachaHeaders] h ON h.[NachaID] = a.[NachaID] WHERE h.[IncomingNachaFileIngestionId] = ${id} AND a.[BusinessType] = N'Return') AS [returnAddendas],
       (SELECT COUNT(*) FROM [IncomingNachaTransactionLinks] l WHERE l.[IncomingNachaFileIngestionId] = ${id} AND l.[IsFinal] = 0) AS [nonFinalLinks]`
  );
  expect(row).toHaveLength(1);
  return row[0];
}

function observeRuntime(page: Page) {
  const consoleErrors: string[] = [];
  const requestFailures: string[] = [];
  const soapRequests: string[] = [];
  const moneyRequests: string[] = [];

  page.on('pageerror', error => consoleErrors.push(error.message));
  page.on('console', message => {
    if (message.type() === 'error' && !/favicon/i.test(message.text())) consoleErrors.push(message.text());
  });
  page.on('requestfailed', request => requestFailures.push(`${request.method()} ${new URL(request.url()).pathname}`));
  page.on('request', request => {
    const pathname = new URL(request.url()).pathname;
    if (/\/soap(?:\/|$)/i.test(pathname)) soapRequests.push(`${request.method()} ${pathname}`);
    if (/\/(?:proc-transacciones|proc-contrapartidas|movimientos|payments)(?:\/|$)/i.test(pathname)) {
      moneyRequests.push(`${request.method()} ${pathname}`);
    }
  });
  return { consoleErrors, requestFailures, soapRequests, moneyRequests };
}

type UploadResult = { status: number; body: UploadResponse };
type UploadResponse = {
  success: boolean;
  message: string;
  errors: string[];
  ingestionId?: string;
  ingestionStatus?: string;
  parsingStatus?: string;
  profileSelectionStatus?: string;
  selectedProfileCode?: string;
  selectedProfileVersion?: string;
  totalBatches?: number;
  totalEntries?: number;
  totalAddendas?: number;
};
type PersistenceEvidence = {
  headers: number;
  batches: number;
  entries: number;
  addendas: number;
  batchControls: number;
  fileControls: number;
  classifications: number;
  links: number;
  returnAddendas: number;
  nonFinalLinks: number;
};
