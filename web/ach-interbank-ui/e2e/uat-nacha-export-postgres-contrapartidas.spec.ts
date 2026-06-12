import { expect, Page, test } from '@playwright/test';
import { attachG36Evidence } from './support/g36-evidence';
import {
  AchCycleSnapshot,
  G36Postgres,
  pollUntil,
  TaskDefinitionSnapshot
} from './support/g36-postgres';

type AuthResponse = { data?: { token?: string } };
type FinancialInstitution = {
  id: number;
  name?: string;
  isDefaultSource?: boolean;
  status?: number;
};
type CompanyEntryDescription = { id: number; term?: string; isActive?: boolean };
type CreatedTransaction = {
  id: number;
  achCycleId?: string;
  achBatch?: { id?: number; achCycleId?: string } | null;
};
type OutboundSeed = {
  transaction: CreatedTransaction;
  prenotification: CreatedTransaction;
};
type AlternatePlacement = {
  cycleId: string;
  batchId: number;
};
type ExportableCycle = {
  cycleId?: string | null;
  cycleName?: string;
  isExportable?: boolean;
};

const enabled = process.env['RUN_UAT_E2E_POSTGRES'] === 'true'
  && process.env['RUN_UAT_NACHA_EXPORT'] === 'true'
  && process.env['RUN_UAT_CONTRAPARTIDAS'] === 'true';
const apiBaseUrl = (process.env['API_BASE_URL'] ?? process.env['ACH_API_URL'] ?? 'http://localhost:843').replace(/\/+$/, '');
const spaBaseUrl = (process.env['SPA_BASE_URL'] ?? process.env['ACH_UI_URL'] ?? 'http://localhost:743').replace(/\/+$/, '');
const username = process.env['ACH_USER'] ?? 'admin';
const password = process.env['ACH_PASS'] ?? 'Admin123!';
const runId = `G36B-${Date.now()}`;
const today = new Date().toISOString().slice(0, 10);

test.describe.configure({ mode: 'serial' });
test.describe('G3.6B NachaExport PostgreSQL -> Proc_Contrapartidas dry-run', () => {
  test.skip(!enabled, 'RUN_UAT_E2E_POSTGRES, RUN_UAT_NACHA_EXPORT y RUN_UAT_CONTRAPARTIDAS deben ser true.');

  let db: G36Postgres;
  let token = '';
  let cycleSnapshot: AchCycleSnapshot | null = null;
  let taskSnapshot: TaskDefinitionSnapshot | null = null;
  let transactionId = 0;
  let batchId = 0;
  let prenotificationId = 0;
  let prenotificationBatchId = 0;
  let cycleId = '';
  let generatedFileName = '';

  test.beforeAll(async () => {
    test.setTimeout(150_000);
    db = new G36Postgres();
    await db.assertReady();
    token = await authenticate();
    await assertSoapDryRunConsole(token);
    await seedDatabase(token);
    taskSnapshot = await db.snapshotTask('AchContrapartidasByCycle');
    await db.pauseTask('AchContrapartidasByCycle');
    await db.waitForSchedulerSyncCycle();
  });

  test.afterAll(async () => {
    try {
      if (taskSnapshot) {
        await db.restoreTask(taskSnapshot);
      }
      if (cycleSnapshot) {
        await db.restoreCycle(cycleSnapshot);
      }
      await cleanupOutboundRun(
        db,
        [transactionId, prenotificationId],
        [batchId, prenotificationBatchId],
        cycleId,
        generatedFileName
      );
    } finally {
      await db.close();
    }
  });

  test('B1 exporta ciclo 6 y correlaciona dispatch dry-run por AchCycleId', async ({ page }, testInfo) => {
    test.setTimeout(420_000);
    const seed = await createOutboundSeed(token);
    const transaction = seed.transaction;
    transactionId = transaction.id;
    batchId = transaction.achBatch?.id ?? 0;
    prenotificationId = seed.prenotification.id;
    prenotificationBatchId = seed.prenotification.achBatch?.id ?? 0;
    cycleId = transaction.achCycleId ?? transaction.achBatch?.achCycleId ?? '';
    expect(cycleId).toBeTruthy();
    expect(batchId).toBeGreaterThan(0);
    expect(prenotificationBatchId).toBeGreaterThan(0);

    cycleSnapshot = await snapshotCycle(db, cycleId);
    const alternatePlacement = await findAlternatePlacement(db, cycleId);
    const prenotificationDate = addUtcDays(today, -10);
    await db.configureCycle(cycleSnapshot, 'Ventana 6', today);
    await db.executeTransaction([
      {
        sql: `UPDATE "AchTransactions"
       SET "Type" = 2,
           "TransactionCode" = '27',
           "ServiceClassCode" = '225',
           "EffectiveEntryDate" = $2::date,
           "UpdatedAt" = NOW()
       WHERE "Id" = $1`,
        values: [transactionId, today]
      },
      {
        sql: `UPDATE "AchBatches"
       SET "ServiceClassCode" = '225',
           "TotalDebitAmount" = 2500,
           "TotalCreditAmount" = 0,
           "EffectiveEntryDate" = $2::date,
           "UpdatedAt" = NOW()
       WHERE "Id" = $1`,
        values: [batchId, today]
      },
      {
        sql: `UPDATE "AchTransactions"
       SET "Type" = 3,
           "TransactionCode" = '28',
           "ServiceClassCode" = '225',
           "IsPrenotification" = TRUE,
           "Amount" = 0,
           "EffectiveEntryDate" = $2::date,
           "AchCycleId" = $3,
           "AchBatchId" = $4,
           "UpdatedAt" = NOW()
       WHERE "Id" = $1`,
        values: [
          prenotificationId,
          prenotificationDate,
          alternatePlacement.cycleId,
          alternatePlacement.batchId
        ]
      }
    ]);

    await seedSession(page, token);
    const exportableResponse = page.waitForResponse((response) =>
      /\/ach-cycles\/exportable(?:\?.*)?$/.test(response.url()) && response.request().method() === 'GET');
    await page.goto(`${spaBaseUrl}/ach-cycles/nacha/export`);
    const exportablePayload = await (await exportableResponse).json() as ExportableCycle[] | { items?: ExportableCycle[] };
    const exportableCycles = Array.isArray(exportablePayload) ? exportablePayload : exportablePayload.items ?? [];
    const targetCycle = exportableCycles.find((item) => item.cycleId === cycleId);
    expect(targetCycle, `La SPA debe listar cycleId=${cycleId}.`).toBeTruthy();
    expect(targetCycle?.cycleName).toBe('Ventana 6');
    expect(targetCycle?.isExportable).toBeTruthy();

    const exportResponsePromise = page.waitForResponse((response) =>
      response.url().includes(`/NachaExport/${encodeURIComponent(cycleId)}`)
      && response.request().method() === 'GET');
    const cycleCell = page.getByText('Ventana 6', { exact: true }).first();
    await expect(cycleCell).toBeVisible();
    const row = cycleCell.locator('xpath=ancestor::*[@role="row"][1]');
    await row.locator('[data-action="generar-nacha"]').click();
    const exportResponse = await exportResponsePromise;
    const exportBody = await exportResponse.body();
    expect(
      exportResponse.ok(),
      `NachaExport respondió HTTP ${exportResponse.status()}: ${exportBody.toString('utf8')}`
    ).toBeTruthy();
    generatedFileName = extractResponseFileName(exportResponse.headers()['content-disposition']);
    expect(generatedFileName).toMatch(/^\d{7}\.\d{3}\.6$/);
    expect(generatedFileName.endsWith('.1')).toBeFalsy();

    const nacha = exportBody.toString('ascii');
    expect(nacha.length).toBeGreaterThanOrEqual(106);
    const sequence = generatedFileName.split('.')[1];
    const expectedFileIdModifier = sequenceToFileIdModifier(Number(sequence));
    expect(nacha[35], 'Registro 1 campo File ID Modifier debe corresponder a ZZZ.').toBe(expectedFileIdModifier);

    const exportAudit = await pollUntil(async () => {
      const rows = await db.query<{
        id: number;
        achCycleId: string;
        clearingHouseId: number;
        fileName: string;
        totalRecords: number;
        totalTransactions: number;
        generatedAtUtc: Date;
      }>(
        `SELECT "Id" AS id,
                "AchCycleId" AS "achCycleId",
                "ClearingHouseId" AS "clearingHouseId",
                "FileName" AS "fileName",
                "TotalRecords" AS "totalRecords",
                "TotalTransactions" AS "totalTransactions",
                "GeneratedAtUtc" AS "generatedAtUtc"
         FROM "AchFileExports"
         WHERE "AchCycleId" = $1 AND "FileName" = $2
         ORDER BY "GeneratedAtUtc" DESC
         LIMIT 1`,
        [cycleId, generatedFileName]
      );
      return rows[0];
    }, 'AchFileExport');

    const registry = await db.query<{
      id: string;
      cycleId: string | null;
      externalFileName: string;
      externalSequence: number | null;
      validationDisposition: string;
    }>(
      `SELECT "Id"::text AS id,
              "CycleId" AS "cycleId",
              "ExternalFileName" AS "externalFileName",
              "ExternalSequence" AS "externalSequence",
              "ValidationDisposition" AS "validationDisposition"
       FROM "ExternalFileNameRegistry"
       WHERE "ExternalFileName" = $1
       ORDER BY "CreatedAtUtc" DESC
       LIMIT 1`,
      [generatedFileName]
    );
    expect(registry).toHaveLength(1);
    expect(registry[0].cycleId).toBe(cycleId);

    const task = await db.snapshotTask('AchContrapartidasByCycle');
    const taskBaseline = await db.taskExecutionBaseline(task.id);
    await db.accelerateTask('AchContrapartidasByCycle');
    const taskExecution = await db.waitForTaskExecution(task.id, taskBaseline, 180_000);
    expect(taskExecution.success, taskExecution.error ?? taskExecution.output ?? '').toBeTruthy();

    const dispatch = await pollUntil(async () => {
      const rows = await db.query<{
        batchId: string;
        batchStatus: string;
        itemId: string;
        itemState: string;
        attemptId: string;
        attemptResult: string;
        retryEligible: boolean;
        externalResponseCode: string;
        requestPresent: boolean;
        responsePresent: boolean;
      }>(
        `SELECT b."Id"::text AS "batchId",
                b."Status" AS "batchStatus",
                i."Id"::text AS "itemId",
                i."State" AS "itemState",
                a."Id"::text AS "attemptId",
                a."Result" AS "attemptResult",
                a."RetryEligible" AS "retryEligible",
                a."ExternalResponseCode" AS "externalResponseCode",
                COALESCE(a."RequestPayloadXml", '') <> '' AS "requestPresent",
                COALESCE(a."ResponsePayloadXml", '') <> '' AS "responsePresent"
         FROM "ContrapartidaDispatchItems" i
         JOIN "ContrapartidaDispatchAttempts" a ON a."DispatchItemId" = i."Id"
         JOIN "ContrapartidaDispatchBatches" b ON b."Id" = a."DispatchBatchId"
         WHERE i."AchTransactionId" = $1
           AND i."AchCycleId" = $2
           AND b."AchCycleId" = $2
         ORDER BY a."StartedAtUtc" DESC
         LIMIT 1`,
        [transactionId, cycleId]
      );
      return rows[0];
    }, 'ContrapartidaDispatchAttempt');
    expect(dispatch.batchStatus).toBe('Failed');
    expect(dispatch.itemState).toBe('ContrapartidaReportFailed');
    expect(dispatch.attemptResult).toBe('Failed');
    expect(dispatch.retryEligible).toBeFalsy();
    expect(dispatch.externalResponseCode).toBe('PROC_DRY_RUN');
    expect(dispatch.requestPresent).toBeTruthy();
    expect(dispatch.responsePresent).toBeTruthy();

    const screenshot = testInfo.outputPath('g36b-outbound.png');
    await page.screenshot({ path: screenshot, fullPage: true });
    await testInfo.attach('g36b-outbound.png', { path: screenshot, contentType: 'image/png' });
    await attachG36Evidence(testInfo, 'g36b-outbound-evidence', {
      testRunId: runId,
      generatedFileId: exportAudit.id,
      generatedFileName,
      resolvedAchCycleId: cycleId,
      cycleNumber: 6,
      externalSequence: registry[0].externalSequence,
      fileIdModifier: expectedFileIdModifier,
      transactionId,
      prenotificationId,
      prenotificationDate,
      dispatchBatchId: dispatch.batchId,
      dispatchItemId: dispatch.itemId,
      dispatchAttemptId: dispatch.attemptId,
      initialStatus: 1,
      finalBatchStatus: dispatch.batchStatus,
      finalItemStatus: dispatch.itemState,
      taskExecutionLogId: taskExecution.id,
      procTarget: 'Proc_Contrapartidas',
      dryRunResult: dispatch.externalResponseCode,
      requestPayloadPresent: dispatch.requestPresent,
      responsePayloadPresent: dispatch.responsePresent,
      externalSoapInvoked: false,
      correlationModel: 'AchCycleId correlation only; NachaExport does not cause dispatch.',
      timestamps: {
        generatedAtUtc: exportAudit.generatedAtUtc,
        taskStartedAt: taskExecution.startedAt,
        taskFinishedAt: taskExecution.finishedAt
      }
    });
  });

  test('B2 CycleName ambiguo bloquea exportación y no dispara Proc_Contrapartidas', async ({ page }, testInfo) => {
    test.setTimeout(180_000);
    expect(cycleSnapshot).not.toBeNull();
    await db.configureCycle(cycleSnapshot!, 'Ciclo 6 2026', today);

    const beforeExports = Number(await db.scalar<string>(
      `SELECT COUNT(*)::text FROM "AchFileExports" WHERE "AchCycleId" = $1`,
      [cycleId]
    ) ?? 0);
    const beforeAttempts = Number(await db.scalar<string>(
      `SELECT COUNT(*)::text
       FROM "ContrapartidaDispatchAttempts" a
       JOIN "ContrapartidaDispatchItems" i ON i."Id" = a."DispatchItemId"
       WHERE i."AchTransactionId" = $1`,
      [transactionId]
    ) ?? 0);

    await seedSession(page, token);
    await page.goto(`${spaBaseUrl}/ach-cycles/nacha/export`);
    const responsePromise = page.waitForResponse((response) =>
      response.url().includes(`/NachaExport/${encodeURIComponent(cycleId)}`)
      && response.request().method() === 'GET');
    const row = page.getByText('Ciclo 6 2026', { exact: true }).first().locator('xpath=ancestor::*[@role="row"][1]');
    await expect(row).toBeVisible();
    await row.locator('[data-action="generar-nacha"]').click();
    const response = await responsePromise;
    expect(response.status()).toBe(422);
    const body = await response.json() as { codigo?: string; mensaje?: string };
    expect(body.mensaje ?? '').toMatch(/ciclo|número|ambigu/i);

    const afterExports = Number(await db.scalar<string>(
      `SELECT COUNT(*)::text FROM "AchFileExports" WHERE "AchCycleId" = $1`,
      [cycleId]
    ) ?? 0);
    const afterAttempts = Number(await db.scalar<string>(
      `SELECT COUNT(*)::text
       FROM "ContrapartidaDispatchAttempts" a
       JOIN "ContrapartidaDispatchItems" i ON i."Id" = a."DispatchItemId"
       WHERE i."AchTransactionId" = $1`,
      [transactionId]
    ) ?? 0);
    expect(afterExports).toBe(beforeExports);
    expect(afterAttempts).toBe(beforeAttempts);

    await attachG36Evidence(testInfo, 'g36b-ambiguous-cycle-evidence', {
      testRunId: runId,
      achCycleId: cycleId,
      cycleName: 'Ciclo 6 2026',
      exportStatus: response.status(),
      exportErrorCode: body.codigo,
      generatedFileDelta: afterExports - beforeExports,
      dispatchAttemptDelta: afterAttempts - beforeAttempts,
      defaultedToCycleOne: false,
      procTargetInvoked: false,
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
  expect(response.ok).toBeTruthy();
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

async function createOutboundSeed(authToken: string): Promise<OutboundSeed> {
  const [institutionsResponse, descriptionsResponse] = await Promise.all([
    fetch(`${apiBaseUrl}/financial-institutions`, { headers: { Authorization: `Bearer ${authToken}` } }),
    fetch(`${apiBaseUrl}/transactions/company-entry-descriptions`, { headers: { Authorization: `Bearer ${authToken}` } })
  ]);
  const institutions = await institutionsResponse.json() as FinancialInstitution[];
  const descriptions = await descriptionsResponse.json() as CompanyEntryDescription[];
  const destination = institutions.find((item) => !item.isDefaultSource && item.status !== 0);
  const description = descriptions.find((item) => item.isActive !== false);
  expect(destination).toBeTruthy();
  expect(description).toBeTruthy();

  const prenotification = await createOutboundTransaction(
    authToken,
    destination!.id,
    description!.id,
    true
  );
  const transaction = await createOutboundTransaction(
    authToken,
    destination!.id,
    description!.id,
    false
  );
  return { transaction, prenotification };
}

async function createOutboundTransaction(
  authToken: string,
  destinationInstitutionId: number,
  companyEntryDescriptionId: number,
  isPrenotification: boolean
): Promise<CreatedTransaction> {
  const suffix = isPrenotification ? 'PRE' : 'OUT';
  const response = await fetch(`${apiBaseUrl}/transactions`, {
    method: 'POST',
    headers: {
      Authorization: `Bearer ${authToken}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({
      amount: isPrenotification ? 0 : 2500,
      transactionExternalId: `${runId}-${suffix}`,
      reference: `${runId}-${suffix}`.slice(-20),
      type: isPrenotification ? 3 : 1,
      accountType: 1,
      isPrenotification,
      destinationInstitutionId,
      sourceAccountNumber: '320000000001',
      destinationAccountNumber: '320000000002',
      recipientIdNumber: '7000001',
      recipientName: 'UAT G36B RECEIVER',
      requiresIdentityValidation: false,
      companyName: 'UAT G36B CFA',
      companyIdentification: runId.replace(/\D/g, '').slice(-10).padStart(10, '7'),
      companyEntryDescriptionId,
      sourcePersonType: 'PJ',
      recipientPersonType: 'PJ',
      addendas: [{
        addendaType: '05',
        collectorId: '9001234567890',
        receiverCustomerCode: runId.slice(-12),
        serviceDescription: 'UAT G36B',
        information: `${runId}-ADD`
      }]
    })
  });
  if (!response.ok) {
    const kind = isPrenotification ? 'prenotificación' : 'transacción';
    throw new Error(`No se pudo crear ${kind} outbound: ${response.status} ${await response.text()}`);
  }
  return await response.json() as CreatedTransaction;
}

async function snapshotCycle(dbClient: G36Postgres, targetCycleId: string): Promise<AchCycleSnapshot> {
  const rows = await dbClient.query<AchCycleSnapshot>(
    `SELECT "Id" AS id,
            "CycleName" AS "cycleName",
            "ProcessingDate" AS "processingDate",
            "CutoffTime"::text AS "cutoffTime",
            "StartTime"::text AS "startTime",
            "EndTime"::text AS "endTime",
            "RescheduleOnHoliday" AS "rescheduleOnHoliday",
            "ClearingHouseId" AS "clearingHouseId",
            "UpdatedAt" AS "updatedAt"
     FROM "AchCycles"
     WHERE "Id" = $1`,
    [targetCycleId]
  );
  expect(rows, `Debe existir AchCycle ${targetCycleId}.`).toHaveLength(1);
  return rows[0];
}

function sequenceToFileIdModifier(sequence: number): string {
  if (sequence >= 1 && sequence <= 26) {
    return String.fromCharCode('A'.charCodeAt(0) + sequence - 1);
  }
  if (sequence >= 27 && sequence <= 36) {
    return String(sequence - 27);
  }
  throw new Error(`ZZZ fuera del rango oficial 001-036: ${sequence}`);
}

function extractResponseFileName(contentDisposition?: string): string {
  const match = /filename\*?=(?:UTF-8''|")?([^";]+)/i.exec(contentDisposition ?? '');
  expect(match?.[1], 'NachaExport debe devolver Content-Disposition con filename.').toBeTruthy();
  return decodeURIComponent(match![1].trim());
}

async function findAlternatePlacement(
  dbClient: G36Postgres,
  excludedCycleId: string
): Promise<AlternatePlacement> {
  const rows = await dbClient.query<AlternatePlacement>(
    `SELECT c."Id" AS "cycleId", b."Id" AS "batchId"
     FROM "AchCycles" c
     JOIN "AchBatches" b ON b."AchCycleId" = c."Id"
     WHERE c."Id" <> $1
     ORDER BY c."ProcessingDate" DESC, c."Id", b."Id"
     LIMIT 1`,
    [excludedCycleId]
  );
  expect(rows, 'Debe existir otro batch/ciclo para aislar la prenotificación UAT.').toHaveLength(1);
  return rows[0];
}

function addUtcDays(isoDate: string, days: number): string {
  const date = new Date(`${isoDate}T00:00:00Z`);
  date.setUTCDate(date.getUTCDate() + days);
  return date.toISOString().slice(0, 10);
}

async function cleanupOutboundRun(
  dbClient: G36Postgres,
  targetTransactionIds: number[],
  targetBatchIds: number[],
  targetCycleId: string,
  fileName: string
): Promise<void> {
  const transactionIds = targetTransactionIds.filter(id => id > 0);
  const batchIds = [...new Set(targetBatchIds.filter(id => id > 0))];
  if (transactionIds.length === 0) {
    return;
  }
  const dispatchBatches = await dbClient.query<{ id: string }>(
    `SELECT DISTINCT a."DispatchBatchId" AS "id"
     FROM "ContrapartidaDispatchAttempts" a
     JOIN "ContrapartidaDispatchItems" i ON i."Id" = a."DispatchItemId"
     WHERE i."AchTransactionId" = ANY($1::int[])`,
    [transactionIds]
  );
  const dispatchBatchIds = dispatchBatches.map(row => row.id);

  await dbClient.executeTransaction([
    {
      sql: `DELETE FROM "IncomingNachaIntegrationExecution"
     WHERE "DispatchQueueId" IN (
       SELECT "Id" FROM "IncomingNachaDispatchQueue" WHERE "AchTransactionId" = ANY($1::int[])
     )`,
      values: [transactionIds]
    },
    {
      sql: `DELETE FROM "IncomingNachaDispatchQueue" WHERE "AchTransactionId" = ANY($1::int[])`,
      values: [transactionIds]
    },
    {
      sql: `DELETE FROM "ContrapartidaDispatchAttempts"
     WHERE "DispatchItemId" IN (
       SELECT "Id" FROM "ContrapartidaDispatchItems" WHERE "AchTransactionId" = ANY($1::int[])
     )`,
      values: [transactionIds]
    },
    {
      sql: `DELETE FROM "ContrapartidaDispatchItems" WHERE "AchTransactionId" = ANY($1::int[])`,
      values: [transactionIds]
    },
    ...(dispatchBatchIds.length > 0
      ? [{
          sql: `DELETE FROM "ContrapartidaDispatchBatches" WHERE "Id" = ANY($1::uuid[])`,
          values: [dispatchBatchIds]
        }]
      : []),
    ...(fileName
      ? [
          {
            sql: `DELETE FROM "AchFileExports" WHERE "AchCycleId" = $1 AND "FileName" = $2`,
            values: [targetCycleId, fileName]
          },
          {
            sql: `DELETE FROM "ExternalFileNameValidationLog"
     WHERE "RegistryId" IN (
       SELECT "Id" FROM "ExternalFileNameRegistry"
       WHERE "CycleId" = $1 AND "ExternalFileName" = $2
     )`,
            values: [targetCycleId, fileName]
          },
          {
            sql: `DELETE FROM "ExternalFileNameRegistry"
       WHERE "CycleId" = $1 AND "ExternalFileName" = $2`,
            values: [targetCycleId, fileName]
          }
        ]
      : []),
    {
      sql: `DELETE FROM "AchTransactionAddenda" WHERE "AchTransactionId" = ANY($1::int[])`,
      values: [transactionIds]
    },
    {
      sql: `DELETE FROM "AchTransactionStateEvents" WHERE "AchTransactionId" = ANY($1::int[])`,
      values: [transactionIds]
    },
    { sql: `DELETE FROM "AchTransactions" WHERE "Id" = ANY($1::int[])`, values: [transactionIds] }
  ]);
  for (const targetBatchId of batchIds) {
    await dbClient.execute(
      `DELETE FROM "AchBatches"
       WHERE "Id" = $1
         AND NOT EXISTS (SELECT 1 FROM "AchTransactions" WHERE "AchBatchId" = $1)`,
      [targetBatchId]
    );
  }
}
