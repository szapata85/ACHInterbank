import { expect, Page, test } from '@playwright/test';
import { readFileSync } from 'node:fs';
import path from 'node:path';
import { attachG36Evidence } from './support/g36-evidence';
import {
  AchCycleSnapshot,
  ClearingHouseOriginSnapshot,
  G36Postgres,
  pollUntil,
  TaskDefinitionSnapshot
} from './support/g36-postgres';

type AuthResponse = { data?: { token?: string } };
type FinancialInstitution = {
  id: number;
  isDefaultSource?: boolean;
  routingNumber?: string;
  transitCode?: string;
  status?: number;
};
type CompanyEntryDescription = { id: number; term?: string; isActive?: boolean };
type CreatedTransaction = { id: number; achCycleId?: string; achBatch?: { id?: number; achCycleId?: string } | null };
type UploadResponse = {
  traceId?: string;
  ingestionStatus?: string;
  cycleResolutionStatus?: string;
  resolvedAchCycleId?: string;
};

const enabled = process.env['RUN_UAT_E2E_POSTGRES'] === 'true'
  && process.env['RUN_UAT_NACHA_UPLOAD'] === 'true'
  && process.env['RUN_UAT_DISPATCH'] === 'true';
const apiBaseUrl = (process.env['API_BASE_URL'] ?? process.env['ACH_API_URL'] ?? 'http://localhost:843').replace(/\/+$/, '');
const spaBaseUrl = (process.env['SPA_BASE_URL'] ?? process.env['ACH_UI_URL'] ?? 'http://localhost:743').replace(/\/+$/, '');
const username = process.env['ACH_USER'] ?? 'admin';
const password = process.env['ACH_PASS'] ?? 'Admin123!';
const officialFileName = '0001283.001.6';
const fixturePath = path.resolve(
  __dirname,
  '../../../tests/Cfa.ACHInterbank.Tests/TestData/Nacha/GoldenFiles/ACHColombia/Incoming/ACH_COL_IN_001.ach'
);
const fixtureDate = '2026-05-24';
const fixtureImmediateOrigin = readFileSync(fixturePath, 'utf8').slice(13, 23).trim();
const runId = `G36A-${Date.now()}`;

test.describe.configure({ mode: 'serial' });
test.describe('G3.6A NACHA-M inbound PostgreSQL -> Proc_Transacciones dry-run', () => {
  test.skip(!enabled, 'RUN_UAT_E2E_POSTGRES, RUN_UAT_NACHA_UPLOAD y RUN_UAT_DISPATCH deben ser true.');

  let db: G36Postgres;
  let token = '';
  let cycleSnapshot: AchCycleSnapshot | null = null;
  let clearingHouseSnapshot: ClearingHouseOriginSnapshot | null = null;
  let taskSnapshot: TaskDefinitionSnapshot | null = null;
  let createdTransactionId = 0;
  let createdBatchId = 0;

  test.beforeAll(async () => {
    test.setTimeout(150_000);
    db = new G36Postgres();
    await db.assertReady();
    token = await authenticate();
    await assertSoapDryRunConsole(token);
    await seedDatabase(token);
    cycleSnapshot = await db.findReusableCycle();
    clearingHouseSnapshot = await db.configureClearingHouseOrigin(
      cycleSnapshot.clearingHouseId,
      fixtureImmediateOrigin
    );
    taskSnapshot = await db.snapshotTask('IncomingNachaPostProcessing');
    await db.pauseTask('IncomingNachaPostProcessing');
    await db.waitForSchedulerSyncCycle();
  });

  test.afterAll(async () => {
    try {
      await cleanupInboundRun(db, officialFileName, createdTransactionId, createdBatchId);
      if (cycleSnapshot) {
        await db.restoreCycle(cycleSnapshot);
      }
      if (clearingHouseSnapshot) {
        await db.restoreClearingHouseOrigin(clearingHouseSnapshot);
      }
      if (taskSnapshot) {
        await db.restoreTask(taskSnapshot);
      }
    } finally {
      await db.close();
    }
  });

  test('A1 carga, persiste, ejecuta Quartz y registra Proc_Transacciones dry-run', async ({ page }, testInfo) => {
    test.setTimeout(360_000);
    expect(cycleSnapshot).not.toBeNull();
    await cleanupInboundRun(db, officialFileName, createdTransactionId, createdBatchId);
    createdTransactionId = 0;
    createdBatchId = 0;

    await assertNoOtherCycleSixCandidate(db, cycleSnapshot!);
    await db.configureCycle(cycleSnapshot!, 'Ventana 6', fixtureDate);

    const transaction = await createCorrelatableIncomingTransaction(db, token, cycleSnapshot!);
    createdTransactionId = transaction.id;
    createdBatchId = transaction.achBatch?.id ?? 0;

    await seedSession(page, token);
    await page.goto(`${spaBaseUrl}/transactions/nacha-upload`);
    await expect(page.getByRole('button', { name: 'Cargar archivo' })).toBeVisible();

    await page.locator('input[type="file"]').setInputFiles({
      name: officialFileName,
      mimeType: 'application/octet-stream',
      buffer: readFileSync(fixturePath)
    });
    await expect(page.getByText(`Archivo seleccionado: ${officialFileName}`, { exact: false })).toBeVisible();

    const responsePromise = page.waitForResponse((response) =>
      /\/NachaUpload\/upload(?:\?.*)?$/.test(response.url()) && response.request().method() === 'POST');
    await page.getByRole('button', { name: 'Cargar archivo' }).click();
    const uploadResponse = await responsePromise;
    const uploadPayload = await uploadResponse.json() as UploadResponse;
    expect(uploadResponse.ok(), JSON.stringify(uploadPayload)).toBeTruthy();
    expect(uploadPayload.resolvedAchCycleId).toBe(cycleSnapshot!.id);

    const ingestion = await pollUntil(async () => {
      const rows = await db.query<{
        id: string;
        fileName: string;
        ingestionStatus: number;
        cycleResolutionStatus: number;
        resolvedAchCycleId: string | null;
        resolvedClearingHouseId: number | null;
        operationalDate: Date | null;
      }>(
        `SELECT "Id"::text AS id,
                "FileName" AS "fileName",
                "IngestionStatus" AS "ingestionStatus",
                "CycleResolutionStatus" AS "cycleResolutionStatus",
                "ResolvedAchCycleId" AS "resolvedAchCycleId",
                "ResolvedClearingHouseId" AS "resolvedClearingHouseId",
                "OperationalDate" AS "operationalDate"
         FROM "IncomingNachaFileIngestions"
         WHERE "FileName" = $1
         ORDER BY "UploadedAtUtc" DESC
         LIMIT 1`,
        [officialFileName]
      );
      return rows[0];
    }, 'IncomingNachaFileIngestion');
    expect(ingestion.resolvedAchCycleId).toBe(cycleSnapshot!.id);

    const counts = await collectInboundCounts(db, ingestion.id);
    expect(counts.processingResults).toBe(1);
    expect(counts.nachaHeaders).toBe(1);
    expect(counts.batchHeaders).toBe(1);
    expect(counts.entryDetails).toBe(1);
    expect(counts.addendaRecords).toBe(1);
    expect(counts.batchControls).toBe(1);
    expect(counts.fileControls).toBe(1);
    expect(counts.classifications).toBe(1);
    expect(counts.transactionLinks).toBeGreaterThanOrEqual(1);
    expect(counts.dispatchQueue).toBe(1);

    const queueBefore = await readInboundQueue(db, ingestion.id);
    expect(queueBefore.achCycleId).toBe(cycleSnapshot!.id);
    expect([1, 8]).toContain(queueBefore.queueStatus);

    const task = await db.snapshotTask('IncomingNachaPostProcessing');
    const taskBaseline = await db.taskExecutionBaseline(task.id);
    await db.accelerateTask('IncomingNachaPostProcessing');
    const taskExecution = await db.waitForTaskExecution(task.id, taskBaseline);
    expect(taskExecution.success, taskExecution.error ?? taskExecution.output ?? '').toBeTruthy();

    const integrationExecution = await pollUntil(async () => {
      const rows = await db.query<{
        id: string;
        dispatchQueueId: string;
        methodName: string;
        responseCode: string;
        isSuccess: boolean;
        isRetryable: boolean;
        correlationId: string;
        requestPresent: boolean;
        responsePresent: boolean;
        responsePayload: string;
        responseMessage: string;
      }>(
        `SELECT e."Id"::text AS id,
                e."DispatchQueueId"::text AS "dispatchQueueId",
                e."MethodName" AS "methodName",
                e."ResponseCode" AS "responseCode",
                e."IsSuccess" AS "isSuccess",
                e."IsRetryable" AS "isRetryable",
                e."CorrelationId" AS "correlationId",
                COALESCE(e."RequestPayloadXml", '') <> '' AS "requestPresent",
                COALESCE(e."ResponsePayloadXml", '') <> '' AS "responsePresent",
                COALESCE(e."ResponsePayloadXml", '') AS "responsePayload",
                e."ResponseMessage" AS "responseMessage"
         FROM "IncomingNachaIntegrationExecution" e
         JOIN "IncomingNachaDispatchQueue" q ON q."Id" = e."DispatchQueueId"
         WHERE q."IncomingNachaFileIngestionId" = $1::uuid
         ORDER BY e."StartedAtUtc" DESC
         LIMIT 1`,
        [ingestion.id]
      );
      return rows[0];
    }, 'IncomingNachaIntegrationExecution');
    expect(integrationExecution.methodName).toBe('Proc_Transacciones');
    expect(
      integrationExecution.responseCode,
      integrationExecution.responseMessage || integrationExecution.responsePayload
    )
      .toBe('PROC_TRANSACCIONES_DRY_RUN');
    expect(integrationExecution.requestPresent).toBeTruthy();
    expect(integrationExecution.responsePresent).toBeTruthy();

    const queueAfter = await readInboundQueue(db, ingestion.id);
    expect(queueAfter.queueStatus).toBe(6); // FailedFinal: dry-run no se interpreta como éxito monetario.
    expect(queueAfter.lastErrorCode).toBe('IFUNC');
    const dryRunEventCount = Number(await db.scalar<string>(
      `SELECT COUNT(*)::text
       FROM "IncomingNachaProcessingEvents"
       WHERE "IncomingNachaFileIngestionId" = $1::uuid
         AND "EventType" = 'ProcTransaccionesDryRunGuardrail'`,
      [ingestion.id]
    ) ?? 0);
    expect(dryRunEventCount).toBeGreaterThan(0);

    const screenshot = testInfo.outputPath('g36a-inbound.png');
    await page.screenshot({ path: screenshot, fullPage: true });
    await testInfo.attach('g36a-inbound.png', { path: screenshot, contentType: 'image/png' });
    await attachG36Evidence(testInfo, 'g36a-inbound-evidence', {
      testRunId: runId,
      ingestionId: ingestion.id,
      resolvedAchCycleId: ingestion.resolvedAchCycleId,
      cycleNumber: 6,
      counts,
      dispatchQueueId: queueAfter.id,
      initialStatus: queueBefore.queueStatus,
      finalStatus: queueAfter.queueStatus,
      taskExecutionLogId: taskExecution.id,
      procTarget: integrationExecution.methodName,
      dryRunResult: integrationExecution.responseCode,
      requestPayloadPresent: integrationExecution.requestPresent,
      responsePayloadPresent: integrationExecution.responsePresent,
      externalSoapInvoked: false,
      timestamps: {
        taskStartedAt: taskExecution.startedAt,
        taskFinishedAt: taskExecution.finishedAt
      }
    });
  });

  test('A2 nombre ciclo 6 no hace fallback a Ciclo 1', async ({ page }, testInfo) => {
    test.setTimeout(180_000);
    expect(cycleSnapshot).not.toBeNull();
    await cleanupInboundRun(db, officialFileName, createdTransactionId, createdBatchId);
    createdTransactionId = 0;
    createdBatchId = 0;
    await assertNoOtherCycleSixCandidate(db, cycleSnapshot!);
    await db.configureCycle(cycleSnapshot!, 'Ciclo 1', fixtureDate);

    await seedSession(page, token);
    await page.goto(`${spaBaseUrl}/transactions/nacha-upload`);
    await page.locator('input[type="file"]').setInputFiles({
      name: officialFileName,
      mimeType: 'application/octet-stream',
      buffer: readFileSync(fixturePath)
    });
    const responsePromise = page.waitForResponse((response) =>
      /\/NachaUpload\/upload(?:\?.*)?$/.test(response.url()) && response.request().method() === 'POST');
    await page.getByRole('button', { name: 'Cargar archivo' }).click();
    const uploadResponse = await responsePromise;
    const payload = await uploadResponse.json() as UploadResponse;
    expect([200, 422]).toContain(uploadResponse.status());
    expect(['NoResuelto', 'Ambiguo']).toContain(payload.cycleResolutionStatus);
    expect(['PendienteResolucion', 'Bloqueado']).toContain(payload.ingestionStatus);
    expect(payload.resolvedAchCycleId ?? '').not.toBe(cycleSnapshot!.id);

    const ingestionId = await db.scalar<string>(
      `SELECT "Id"::text
       FROM "IncomingNachaFileIngestions"
       WHERE "FileName" = $1
       ORDER BY "UploadedAtUtc" DESC
       LIMIT 1`,
      [officialFileName]
    );
    expect(ingestionId).toBeTruthy();
    const queueCount = Number(await db.scalar<string>(
      `SELECT COUNT(*)::text
       FROM "IncomingNachaDispatchQueue"
       WHERE "IncomingNachaFileIngestionId" = $1::uuid`,
      [ingestionId]
    ) ?? 0);
    expect(queueCount).toBe(0);

    await attachG36Evidence(testInfo, 'g36a-no-fallback-evidence', {
      testRunId: runId,
      ingestionId,
      requestedCycleNumber: 6,
      onlyConfiguredCycleNumber: 1,
      cycleResolutionStatus: payload.cycleResolutionStatus,
      ingestionStatus: payload.ingestionStatus,
      resolvedAchCycleId: payload.resolvedAchCycleId ?? null,
      dispatchQueueCount: queueCount,
      procTarget: null,
      externalSoapInvoked: false
    });
  });
});

async function authenticate(): Promise<string> {
  const response = await fetch(`${apiBaseUrl}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username, password })
  });
  expect(response.ok, 'La API real debe autenticar el usuario UAT.').toBeTruthy();
  const payload = await response.json() as AuthResponse;
  expect(payload.data?.token).toBeTruthy();
  return payload.data!.token!;
}

async function seedDatabase(authToken: string): Promise<void> {
  const response = await fetch(`${apiBaseUrl}/Maintenance/seed`, {
    method: 'POST',
    headers: { Authorization: `Bearer ${authToken}` }
  });
  expect(response.ok, `Maintenance/seed falló: ${await response.text()}`).toBeTruthy();
}

async function assertSoapDryRunConsole(authToken: string): Promise<void> {
  const response = await fetch(`${apiBaseUrl}/api/ach/nacha/soap-uat-console/dashboard`, {
    headers: { Authorization: `Bearer ${authToken}` }
  });
  expect(response.ok, 'La consola SOAP/UAT debe estar disponible para validar guardrails.').toBeTruthy();
  const dashboard = await response.json() as {
    productiveExecution?: boolean;
    wouldInvokeRealSoap?: boolean;
    productiveStatus?: string;
  };
  expect(dashboard.productiveExecution).toBeFalsy();
  expect(dashboard.wouldInvokeRealSoap).toBeFalsy();
  expect(dashboard.productiveStatus).toBe('NO-GO');
}

async function seedSession(page: Page, authToken: string): Promise<void> {
  await page.addInitScript((value) => {
    window.sessionStorage.setItem('ach.interbank.access_token', value);
  }, authToken);
}

async function createCorrelatableIncomingTransaction(
  dbClient: G36Postgres,
  authToken: string,
  cycle: AchCycleSnapshot
): Promise<CreatedTransaction> {
  const [institutionsResponse, descriptionsResponse] = await Promise.all([
    fetch(`${apiBaseUrl}/financial-institutions`, { headers: { Authorization: `Bearer ${authToken}` } }),
    fetch(`${apiBaseUrl}/transactions/company-entry-descriptions`, { headers: { Authorization: `Bearer ${authToken}` } })
  ]);
  expect(institutionsResponse.ok).toBeTruthy();
  expect(descriptionsResponse.ok).toBeTruthy();
  const institutions = await institutionsResponse.json() as FinancialInstitution[];
  const descriptions = await descriptionsResponse.json() as CompanyEntryDescription[];
  const defaultInstitution = institutions.find((item) => item.isDefaultSource);
  const externalInstitution = institutions.find((item) => !item.isDefaultSource && item.status !== 0);
  const description = descriptions.find((item) => item.isActive !== false);
  expect(defaultInstitution).toBeTruthy();
  expect(externalInstitution).toBeTruthy();
  expect(description).toBeTruthy();

  const response = await fetch(`${apiBaseUrl}/transactions`, {
    method: 'POST',
    headers: {
      Authorization: `Bearer ${authToken}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      amount: 1500,
      transactionExternalId: `${runId}-IN`,
      reference: 'REF00001',
      type: 1,
      accountType: 1,
      isPrenotification: false,
      destinationInstitutionId: externalInstitution!.id,
      sourceAccountNumber: '000123456789',
      destinationAccountNumber: '999988887777',
      recipientIdNumber: '900000001',
      recipientName: 'CLIENTE UAT',
      requiresIdentityValidation: false,
      companyName: 'EMPRESA DEMO',
      companyIdentification: '1234567800',
      companyEntryDescriptionId: description!.id,
      sourcePersonType: 'PJ',
      recipientPersonType: 'PJ',
      addendas: [{ addendaType: '05', information: 'INFO-ADDENDA' }]
    })
  });
  if (!response.ok) {
    throw new Error(`No se pudo preparar transacción inbound: ${response.status} ${await response.text()}`);
  }
  const transaction = await response.json() as CreatedTransaction;
  const batchId = transaction.achBatch?.id ?? 0;
  expect(batchId).toBeGreaterThan(0);

  const currentCycleId = transaction.achCycleId ?? transaction.achBatch?.achCycleId ?? '';
  await dbClient.executeTransaction([
    {
      sql: `UPDATE "AchTransactions"
     SET "Type" = 1,
         "TransactionCode" = '22',
         "Amount" = 1500,
         "TransactionExternalId" = '900000001',
         "EffectiveEntryDate" = $2::date,
         "SourceInstitutionId" = $3,
         "DestinationInstitutionId" = $4,
         "AchCycleId" = $1,
         "ReceivingDFI" = '765432104',
         "DestinationAccountNumber" = '999988887777',
         "RecipientIdNumber" = '900000001',
         "DiscretionaryData" = '',
         "UpdatedAt" = NOW()
     WHERE "Id" = $5`,
      values: [cycle.id, fixtureDate, externalInstitution!.id, defaultInstitution!.id, transaction.id]
    },
    {
      sql: `UPDATE "AchBatches"
     SET "AchCycleId" = $1,
         "EffectiveEntryDate" = $2::date,
         "UpdatedAt" = NOW()
     WHERE "Id" = $3`,
      values: [cycle.id, fixtureDate, batchId]
    },
    {
      sql: `DELETE FROM "ContrapartidaDispatchItems" WHERE "AchTransactionId" = $1`,
      values: [transaction.id]
    }
  ]);
  transaction.achCycleId = cycle.id;
  transaction.achBatch = { ...transaction.achBatch, id: batchId, achCycleId: cycle.id };
  void currentCycleId;
  return transaction;
}

async function assertNoOtherCycleSixCandidate(dbClient: G36Postgres, selected: AchCycleSnapshot): Promise<void> {
  const count = Number(await dbClient.scalar<string>(
    `SELECT COUNT(*)::text
     FROM "AchCycles"
     WHERE "ClearingHouseId" = $1
       AND "ProcessingDate"::date = $2::date
       AND "Id" <> $3
       AND "CycleName" ~ '(^|[^0-9])6([^0-9]|$)'`,
    [selected.clearingHouseId, fixtureDate, selected.id]
  ) ?? 0);
  expect(count, 'La base UAT dedicada no debe tener otro ciclo 6 para la misma cámara/fecha.').toBe(0);
}

async function collectInboundCounts(dbClient: G36Postgres, ingestionId: string): Promise<Record<string, number>> {
  const rows = await dbClient.query<Record<string, string>>(
    `SELECT
       (SELECT COUNT(*) FROM "IncomingNachaFileProcessingResults" WHERE "IncomingNachaFileIngestionId" = $1::uuid)::text AS "processingResults",
       (SELECT COUNT(*) FROM "NachaHeaders" WHERE "IncomingNachaFileIngestionId" = $1::uuid)::text AS "nachaHeaders",
       (SELECT COUNT(*) FROM "BatchHeaders" WHERE "NachaID" IN (SELECT "NachaID" FROM "NachaHeaders" WHERE "IncomingNachaFileIngestionId" = $1::uuid))::text AS "batchHeaders",
       (SELECT COUNT(*) FROM "EntryDetails" WHERE "NachaID" IN (SELECT "NachaID" FROM "NachaHeaders" WHERE "IncomingNachaFileIngestionId" = $1::uuid))::text AS "entryDetails",
       (SELECT COUNT(*) FROM "AddendaRecords" WHERE "NachaID" IN (SELECT "NachaID" FROM "NachaHeaders" WHERE "IncomingNachaFileIngestionId" = $1::uuid))::text AS "addendaRecords",
       (SELECT COUNT(*) FROM "BatchControls" WHERE "NachaID" IN (SELECT "NachaID" FROM "NachaHeaders" WHERE "IncomingNachaFileIngestionId" = $1::uuid))::text AS "batchControls",
       (SELECT COUNT(*) FROM "FileControls" WHERE "NachaID" IN (SELECT "NachaID" FROM "NachaHeaders" WHERE "IncomingNachaFileIngestionId" = $1::uuid))::text AS "fileControls",
       (SELECT COUNT(*) FROM "IncomingNachaEntryClassifications" WHERE "IncomingNachaFileIngestionId" = $1::uuid)::text AS classifications,
       (SELECT COUNT(*) FROM "IncomingNachaTransactionLinks" WHERE "IncomingNachaFileIngestionId" = $1::uuid)::text AS "transactionLinks",
       (SELECT COUNT(*) FROM "IncomingNachaDispatchQueue" WHERE "IncomingNachaFileIngestionId" = $1::uuid)::text AS "dispatchQueue"`,
    [ingestionId]
  );
  return Object.fromEntries(Object.entries(rows[0]).map(([key, value]) => [key, Number(value)]));
}

async function readInboundQueue(dbClient: G36Postgres, ingestionId: string) {
  const rows = await dbClient.query<{
    id: string;
    achCycleId: string;
    queueStatus: number;
    lastErrorCode: string;
    attemptCount: number;
  }>(
    `SELECT "Id"::text AS id,
            "AchCycleId" AS "achCycleId",
            "QueueStatus" AS "queueStatus",
            "LastErrorCode" AS "lastErrorCode",
            "AttemptCount" AS "attemptCount"
     FROM "IncomingNachaDispatchQueue"
     WHERE "IncomingNachaFileIngestionId" = $1::uuid
     ORDER BY "CreatedAt" DESC
     LIMIT 1`,
    [ingestionId]
  );
  expect(rows).toHaveLength(1);
  return rows[0];
}

async function cleanupInboundRun(
  dbClient: G36Postgres,
  fileName: string,
  transactionId: number,
  batchId: number
): Promise<void> {
  await dbClient.executeTransaction([
    {
      sql: `DELETE FROM "ExternalFileNameValidationLog"
       WHERE "RegistryId" IN (
         SELECT "Id" FROM "ExternalFileNameRegistry"
         WHERE "ExternalFileName" = $1
       )`,
      values: [fileName]
    },
    {
      sql: `DELETE FROM "ExternalFileNameRegistry" WHERE "ExternalFileName" = $1`,
      values: [fileName]
    },
    {
      sql: `DELETE FROM "IncomingNachaIntegrationExecution"
     WHERE "DispatchQueueId" IN (
       SELECT "Id" FROM "IncomingNachaDispatchQueue"
       WHERE "IncomingNachaFileIngestionId" IN (
         SELECT "Id" FROM "IncomingNachaFileIngestions" WHERE "FileName" = $1
       )
     )`,
      values: [fileName]
    },
    {
      sql: `DELETE FROM "IncomingNachaDispatchQueue"
     WHERE "IncomingNachaFileIngestionId" IN (
       SELECT "Id" FROM "IncomingNachaFileIngestions" WHERE "FileName" = $1
     )`,
      values: [fileName]
    },
    {
      sql: `DELETE FROM "IncomingNachaProcessingEvents"
     WHERE "IncomingNachaFileIngestionId" IN (
       SELECT "Id" FROM "IncomingNachaFileIngestions" WHERE "FileName" = $1
     )`,
      values: [fileName]
    },
    {
      sql: `DELETE FROM "IncomingNachaTransactionLinks"
     WHERE "IncomingNachaFileIngestionId" IN (
       SELECT "Id" FROM "IncomingNachaFileIngestions" WHERE "FileName" = $1
     )`,
      values: [fileName]
    },
    {
      sql: `DELETE FROM "IncomingNachaEntryClassifications"
     WHERE "IncomingNachaFileIngestionId" IN (
       SELECT "Id" FROM "IncomingNachaFileIngestions" WHERE "FileName" = $1
     )`,
      values: [fileName]
    },
    {
      sql: `DELETE FROM "IncomingNachaFileProcessingResults"
     WHERE "IncomingNachaFileIngestionId" IN (
       SELECT "Id" FROM "IncomingNachaFileIngestions" WHERE "FileName" = $1
     )`,
      values: [fileName]
    },
    {
      sql: `DELETE FROM "AddendaRecords" WHERE "NachaID" IN (
       SELECT "NachaID" FROM "NachaHeaders" WHERE "IncomingNachaFileIngestionId" IN (
         SELECT "Id" FROM "IncomingNachaFileIngestions" WHERE "FileName" = $1
       )
     )`,
      values: [fileName]
    },
    {
      sql: `DELETE FROM "BatchControls" WHERE "NachaID" IN (
       SELECT "NachaID" FROM "NachaHeaders" WHERE "IncomingNachaFileIngestionId" IN (
         SELECT "Id" FROM "IncomingNachaFileIngestions" WHERE "FileName" = $1
       )
     )`,
      values: [fileName]
    },
    {
      sql: `DELETE FROM "BatchHeaders" WHERE "NachaID" IN (
       SELECT "NachaID" FROM "NachaHeaders" WHERE "IncomingNachaFileIngestionId" IN (
         SELECT "Id" FROM "IncomingNachaFileIngestions" WHERE "FileName" = $1
       )
     )`,
      values: [fileName]
    },
    {
      sql: `DELETE FROM "EntryDetails" WHERE "NachaID" IN (
       SELECT "NachaID" FROM "NachaHeaders" WHERE "IncomingNachaFileIngestionId" IN (
         SELECT "Id" FROM "IncomingNachaFileIngestions" WHERE "FileName" = $1
       )
     )`,
      values: [fileName]
    },
    {
      sql: `DELETE FROM "FileControls" WHERE "NachaID" IN (
       SELECT "NachaID" FROM "NachaHeaders" WHERE "IncomingNachaFileIngestionId" IN (
         SELECT "Id" FROM "IncomingNachaFileIngestions" WHERE "FileName" = $1
       )
     )`,
      values: [fileName]
    },
    {
      sql: `DELETE FROM "NachaHeaders"
     WHERE "IncomingNachaFileIngestionId" IN (
       SELECT "Id" FROM "IncomingNachaFileIngestions" WHERE "FileName" = $1
     )`,
      values: [fileName]
    },
    {
      sql: `DELETE FROM "IncomingNachaFileIngestions" WHERE "FileName" = $1`,
      values: [fileName]
    }
  ]);

  if (transactionId > 0) {
    await dbClient.executeTransaction([
      {
        sql: `DELETE FROM "ContrapartidaDispatchAttempts"
       WHERE "DispatchItemId" IN (
         SELECT "Id" FROM "ContrapartidaDispatchItems" WHERE "AchTransactionId" = $1
       )`,
        values: [transactionId]
      },
      { sql: `DELETE FROM "ContrapartidaDispatchItems" WHERE "AchTransactionId" = $1`, values: [transactionId] },
      { sql: `DELETE FROM "AchTransactionAddenda" WHERE "AchTransactionId" = $1`, values: [transactionId] },
      { sql: `DELETE FROM "AchTransactionStateEvents" WHERE "AchTransactionId" = $1`, values: [transactionId] },
      { sql: `DELETE FROM "AchTransactions" WHERE "Id" = $1`, values: [transactionId] }
    ]);
  }
  if (batchId > 0) {
    await dbClient.execute(
      `DELETE FROM "AchBatches"
       WHERE "Id" = $1
         AND NOT EXISTS (SELECT 1 FROM "AchTransactions" WHERE "AchBatchId" = $1)`,
      [batchId]
    );
  }
}
