import { expect, Page, test, TestInfo } from '@playwright/test';
import { readFileSync } from 'node:fs';
import path from 'node:path';
import { loginThroughUi } from './support/live-ui-auth';
import { G36SqlServer, sqlString } from './support/g36-sqlserver';

const fileTemplate = path.resolve(
  __dirname,
  '../../../tests/Cfa.ACHInterbank.Tests/TestData/Nacha/GoldenFiles/ACHColombia/Returns/ACH_COL_RET_001.RET'
);
const originalTrace = '000128390009171';
const destinationAccount = '4999988887777';
const reasonCode = 'R31';
const cycleDate = '2026-08-06';

test.describe.configure({ mode: 'serial' });

test('demuestra el gate de archivo y resuelve una huérfana persistida sin doble aplicación', async ({ page }, testInfo) => {
  test.setTimeout(240_000);
  const database = new G36SqlServer();
  database.assertReady();

  const runToken = Date.now().toString().slice(-8);
  const fileName = `0001283.001.20260806.${runToken}.OUT`;
  const transactionPrefix = `RET-ORPHAN-E2E-${runToken}`;
  const fixture = buildReturnFixture(runToken);
  const runtime = observeRuntime(page);

  const cycleId = database.scalar<string>(
    `SELECT TOP (1) c.[Id] AS [value]
     FROM [AchCycles] c
     JOIN [ClearingHouses] h ON h.[Id] = c.[ClearingHouseId]
     WHERE h.[Code] = N'ACHCOL' AND c.[ProcessingDate] = '${cycleDate}'
       AND c.[StartTime] <= '12:00:00' AND c.[EndTime] >= '12:00:00'
     ORDER BY c.[CycleName]`
  );
  expect(cycleId, `Debe existir un ciclo ACH Colombia para ${cycleDate}.`).toBeTruthy();

  seedOriginatedTransactions(database, cycleId!, transactionPrefix);
  const candidates = database.query<{ id: number; externalId: string }>(
    `SELECT [Id] AS [id], [TransactionExternalId] AS [externalId]
     FROM [AchTransactions]
     WHERE [TransactionExternalId] LIKE ${sqlString(`${transactionPrefix}-%`)}
     ORDER BY [Id]`
  );
  expect(candidates).toHaveLength(2);
  const selectedTransactionId = candidates[0].id;

  const before = readPersistenceEvidence(database, transactionPrefix, null);
  expect(before.transactions).toBe(2);
  expect(before.returnedTransactions).toBe(0);
  expect(before.stateEvents).toBe(0);

  await loginThroughUi(page);
  const upload = await uploadReturnThroughUi(page, fileName, fixture);
  expect(upload.status).toBe(422);
  expect(upload.body.profileSelectionStatus).toBe('ProfileNotFound');
  expect(upload.body.errors).toContain('ProfileNotFound');
  expect(upload.body.ingestionId).toBeTruthy();

  const ingestion = database.query<{ id: string }>(
    `SELECT TOP (1) CONVERT(nvarchar(36), [Id]) AS [id]
     FROM [IncomingNachaFileIngestions]
     WHERE [FileName] = ${sqlString(fileName)}
     ORDER BY [CreatedAt] DESC`
  );
  expect(ingestion).toHaveLength(1);
  const ingestionId = ingestion[0].id;
  expect(ingestionId.toLowerCase()).toBe(upload.body.ingestionId!.toLowerCase());
  seedControlledParsedOrphan(database, ingestionId, runToken, candidates.map(candidate => candidate.id));

  await expect.poll(() => database.scalar<number>(
    `SELECT COUNT(*) AS [value]
     FROM [IncomingNachaTransactionLinks]
     WHERE [IncomingNachaFileIngestionId] = ${sqlString(ingestionId)}
       AND [IsFinal] = 0`
  )).toBe(1);

  const orphan = database.query<{ id: string; linkType: string; candidates: string }>(
    `SELECT CONVERT(nvarchar(36), [Id]) AS [id], [LinkType] AS [linkType], [EvidenceJson] AS [candidates]
     FROM [IncomingNachaTransactionLinks]
     WHERE [IncomingNachaFileIngestionId] = ${sqlString(ingestionId)}`
  );
  expect(orphan).toHaveLength(1);
  expect(orphan[0].linkType).toBe('Ambiguous');
  expect(orphan[0].candidates).toContain(String(candidates[0].id));
  expect(orphan[0].candidates).toContain(String(candidates[1].id));
  const linkId = orphan[0].id;

  const initialOrphansResponse = page.waitForResponse(response =>
    response.request().method() === 'GET'
    && new URL(response.url()).pathname === '/incoming-nacha-command-center/orphans'
  );
  await page.goto('/incoming-nacha-command-center/orphan-resolution');
  expect((await initialOrphansResponse).status()).toBe(200);
  await expect(page.getByRole('heading', { name: 'Devoluciones recibidas sin relación', level: 1 })).toBeVisible();
  await page.getByLabel('Archivo, causal o número de rastreo').fill(fileName);
  const filteredOrphansResponse = page.waitForResponse(response =>
    response.request().method() === 'GET'
    && new URL(response.url()).pathname === '/incoming-nacha-command-center/orphans'
    && new URL(response.url()).searchParams.get('search') === fileName
  );
  await page.getByRole('button', { name: 'Buscar' }).click();
  expect((await filteredOrphansResponse).status()).toBe(200);
  await expect(page.getByText(fileName, { exact: true })).toBeVisible();
  await expect(page.getByText(reasonCode, { exact: true })).toBeVisible();
  await page.screenshot({ path: testInfo.outputPath('01-devolucion-sin-relacion.png'), fullPage: true });

  await page.getByRole('button', { name: 'Investigar' }).click();
  await expect(page.getByText('Devolución recibida', { exact: true }).first()).toBeVisible();
  await expect(page.getByText('Transacciones candidatas', { exact: true })).toBeVisible();
  await expect(page.getByLabel(`Seleccionar transacción ${selectedTransactionId}`)).toBeVisible();
  await page.getByLabel(`Seleccionar transacción ${selectedTransactionId}`).click();
  await expect(page.getByText('Confirmar relación', { exact: true }).first()).toBeVisible();
  await page.getByLabel('Justificación de la relación').fill('Validación operativa E2E del rastreo, valor y cuenta receptora.');
  await page.getByLabel('Comentario adicional').fill('RET.ORPHAN.E2E.1');
  await page.getByRole('checkbox', { name: /Confirmo que revisé cámara/ }).check();
  await page.screenshot({ path: testInfo.outputPath('02-comparacion-y-confirmacion.png'), fullPage: true });

  const resolutionResponse = page.waitForResponse((response) =>
    response.request().method() === 'POST'
    && new URL(response.url()).pathname.toLowerCase()
      === `/incoming-nacha-command-center/orphans/${linkId}/resolve`.toLowerCase()
  );
  await page.getByRole('button', { name: 'Confirmar relación' }).click();
  const firstResponse = await resolutionResponse;
  expect(firstResponse.status()).toBe(200);
  const firstResult = await firstResponse.json() as ResolutionResult;
  expect(firstResult.isResolved).toBe(true);
  expect(firstResult.isIdempotentReplay).toBe(false);
  expect(firstResult.achTransactionStateEventId).toBeTruthy();

  await expect(page.getByText(/La devolución fue relacionada y aplicada correctamente/)).toBeVisible();
  await expect(page.getByText(new RegExp(`Relacionada con la transacción ${selectedTransactionId}`))).toBeVisible();
  await expect(page.getByText(new RegExp(`${reasonCode}.*Entrada permitida de retorno`))).toBeVisible();
  await page.screenshot({ path: testInfo.outputPath('03-resolucion-aplicada.png'), fullPage: true });

  const replayResult = await repeatResolutionInBrowser(page, linkId, selectedTransactionId);
  expect(replayResult.status).toBe(200);
  expect(replayResult.body.isResolved).toBe(true);
  expect(replayResult.body.isIdempotentReplay).toBe(true);
  expect(replayResult.body.achTransactionStateEventId).toBe(firstResult.achTransactionStateEventId);

  const after = readPersistenceEvidence(database, transactionPrefix, ingestionId);
  expect(after.transactions).toBe(2);
  expect(after.returnedTransactions).toBe(1);
  expect(after.stateEvents).toBe(1);
  expect(after.manualResolutionEvents).toBe(1);
  expect(after.finalLinks).toBe(1);
  expect(after.reasonCode).toBe(reasonCode);
  expect(after.returnCodeCatalogLinks).toBe(1);
  expect(after.resolvedBy).toBeTruthy();

  expect(runtime.consoleErrors, JSON.stringify(runtime.consoleErrors, null, 2)).toEqual([]);
  expect(runtime.requestFailures, JSON.stringify(runtime.requestFailures, null, 2)).toEqual([]);
  expect(runtime.unexpectedHttpErrors, JSON.stringify(runtime.unexpectedHttpErrors, null, 2)).toEqual([]);
  await attachEvidence(testInfo, { fileName, ingestionId, linkId, selectedTransactionId, before, after, firstResult, replayResult, runtime });
});

function buildReturnFixture(runToken: string): Buffer {
  let content = readFileSync(fileTemplate, 'utf8');
  content = content.replaceAll('20260524', '20260806');
  content = content.replaceAll('123456780000001', originalTrace);
  content = content.replace('R01  ', `${reasonCode}  `);
  content = content.replace('REF00001', `OR${runToken.slice(-6)}`);
  content = `${content.slice(0, 13)} 000101006${content.slice(23)}`;
  return Buffer.from(content, 'ascii');
}

function seedOriginatedTransactions(database: G36SqlServer, cycleId: string, transactionPrefix: string): void {
  database.execute(`
    DECLARE @BatchId int;
    INSERT INTO [AchBatches] (
      [AchCycleId], [ServiceClassCode], [CompanyName], [CompanyIdentification],
      [CompanyEntryDescription], [CompanyEntryDescriptionId], [OriginOrOdfi],
      [EffectiveEntryDate], [BatchSequenceNumber], [TotalDebitAmount], [TotalCreditAmount],
      [CreatedAt], [UpdatedAt])
    VALUES (
      ${sqlString(cycleId)}, N'225', N'EMPRESA E2E', N'900000001',
      N'DEVOLUCION E2E', 1, N'0001283', '${cycleDate}', 9171, 3000, 0,
      SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());
    SET @BatchId = SCOPE_IDENTITY();

    INSERT INTO [AchTransactions] (
      [Amount], [TransactionExternalId], [Reference], [Type], [TransactionCode], [ServiceClassCode],
      [CompanyEntryDescriptionId], [CompanyName], [CompanyIdentification], [OriginatingDFI], [ReceivingDFI],
      [TraceNumber], [TraceSequenceNumber], [EffectiveEntryDate], [AddendaRecordIndicator], [IsPrenotification],
      [State], [StateChangedAtUtc], [ContrapartidasResponseCode], [ReturnReasonCode], [OriginalTraceRef],
      [RecipientIdNumber], [DiscretionaryData], [SourceAccountNumber], [DestinationAccountNumber],
      [SourceInstitutionId], [DestinationInstitutionId], [AchCycleId], [AchBatchId], [CreatedAt], [UpdatedAt],
      [Direction], [Origin], [MonetaryIntegrationRoute], [ClassificationStatus], [ClassificationVersion])
    VALUES
      (1500, ${sqlString(`${transactionPrefix}-A`)}, N'ORPHAN-E2E-A', N'Debit', N'27', N'225',
       1, N'EMPRESA E2E', N'900000001', N'12345678', N'76543210', ${sqlString(originalTrace)}, 9171,
       '${cycleDate}', 0, 0, N'Pending', SYSUTCDATETIME(), N'', N'', N'', N'900000001', N'',
       N'411000000001', ${sqlString(destinationAccount)}, 1, 2, ${sqlString(cycleId)}, @BatchId,
       SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET(), N'Outgoing', N'Cfa', N'None', N'Determined', 1),
      (1500, ${sqlString(`${transactionPrefix}-B`)}, N'ORPHAN-E2E-B', N'Debit', N'27', N'225',
       1, N'EMPRESA E2E', N'900000001', N'12345678', N'76543210', ${sqlString(originalTrace)}, 9172,
       '${cycleDate}', 0, 0, N'Pending', SYSUTCDATETIME(), N'', N'', N'', N'900000001', N'',
       N'411000000002', ${sqlString(destinationAccount)}, 1, 2, ${sqlString(cycleId)}, @BatchId,
       SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET(), N'Outgoing', N'Cfa', N'None', N'Determined', 1);
  `);
}

async function uploadReturnThroughUi(page: Page, fileName: string, fixture: Buffer): Promise<UploadResult> {
  await page.goto('/transactions/nacha-upload');
  await expect(page.getByText('Cargar archivo NACHA-M', { exact: true })).toBeVisible();
  const clearingHouseSelect = page.locator('app-clearing-house-select select');
  await expect(clearingHouseSelect).toBeEnabled();
  await clearingHouseSelect.selectOption({ label: 'ACH Colombia (ACHCOL)' });
  await page.locator('input[type="file"]').setInputFiles({
    name: fileName,
    mimeType: 'application/octet-stream',
    buffer: fixture
  });

  const uploadResponse = page.waitForResponse((response) =>
    response.request().method() === 'POST' && new URL(response.url()).pathname === '/NachaUpload/upload'
  );
  await page.getByRole('button', { name: 'Cargar archivo' }).click();
  const response = await uploadResponse;
  const body = await response.json() as UploadResult['body'];
  await expect(page.getByTestId('nacha-upload-result-message')).toContainText('Archivo bloqueado por selección de perfil NACHA-M');
  return { status: response.status(), body };
}

function seedControlledParsedOrphan(
  database: G36SqlServer,
  ingestionId: string,
  runToken: string,
  candidateIds: number[]
): void {
  const nachaId = `ORPHAN${runToken}`;
  const candidateList = candidateIds.join(',');
  database.execute(`
    SET QUOTED_IDENTIFIER ON;
    INSERT INTO [NachaHeaders] (
      [NachaID], [ImmediateDestination], [ImmediateOrigin], [FileCreationDate], [FileCreationTime],
      [FileIdModifier], [RecordSize], [BlockingFactor], [FormatCode], [ImmediateDestinationName],
      [ImmediateOriginName], [ReferenceCode], [ClearingHouseId], [CycleNumber], [AchCycleId],
      [IncomingNachaFileIngestionId], [CreatedAt], [UpdatedAt])
    VALUES (
      ${sqlString(nachaId)}, N'000000000', N'000101006', N'20260806', N'1200', N'A', N'106', N'10', N'1',
      N'ACH COLOMBIA', N'CFA UAT', N'RET-ORPH', 1, 3,
      (SELECT [ResolvedAchCycleId] FROM [IncomingNachaFileIngestions] WHERE [Id] = ${sqlString(ingestionId)}),
      ${sqlString(ingestionId)}, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());
  `);
  database.execute(`
    SET QUOTED_IDENTIFIER ON;
    INSERT INTO [EntryDetails] (
      [TransactionCode], [ReceivingParticipantEntityCode], [CheckDigit], [AccountNumber], [Amount],
      [RecipIdNumber], [RecipUserName], [DiscreData], [AddendumIndicator], [SequenceNumber], [BatchNumber],
      [NachaID], [CreatedAt], [UpdatedAt])
    VALUES (
      N'21', N'76543210', N'4', ${sqlString(destinationAccount)}, 1500, N'900000001', N'CLIENTE UAT',
      N'', N'1', N'000128390009172', 1, ${sqlString(nachaId)}, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());
  `);
  const entryId = database.scalar<number>(
    `SELECT TOP (1) [EntryDetailID] AS [value] FROM [EntryDetails] WHERE [NachaID] = ${sqlString(nachaId)} ORDER BY [EntryDetailID] DESC`
  );
  expect(entryId).toBeTruthy();
  database.execute(`
    SET QUOTED_IDENTIFIER ON;
    INSERT INTO [AddendaRecords] (
      [CodeTypeAddendumRecord], [BusinessType], [ReturnReasonCode], [OriginalTraceNumber], [NewTraceNumber],
      [AddendumSequence], [EntryDetailSequenceNumber], [NachaID], [EntryDetailId], [CreatedAt], [UpdatedAt])
    VALUES (
      N'99', N'Return', ${sqlString(reasonCode)}, ${sqlString(originalTrace)}, N'000128390009172',
      N'0001', N'0091712', ${sqlString(nachaId)}, ${entryId}, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());
  `);
  const addendaId = database.scalar<number>(
    `SELECT TOP (1) [AddendaID] AS [value] FROM [AddendaRecords] WHERE [NachaID] = ${sqlString(nachaId)} ORDER BY [AddendaID] DESC`
  );
  expect(addendaId).toBeTruthy();
  const evidenceDeclaration = `SET QUOTED_IDENTIFIER ON;
    DECLARE @Evidence nvarchar(max) = (
    SELECT 1 AS [schemaVersion], N'IncomingReturnUnresolved' AS [eventType],
           N'Unresolved' AS [resolutionStatus], N'Ambiguous' AS [resolutionReason],
           JSON_QUERY(N'[${candidateList}]') AS [candidateTransactionIds],
           N'ProfileNotFound' AS [fileAdmissionBlocker]
    FOR JSON PATH, WITHOUT_ARRAY_WRAPPER);`;
  database.execute(`
    ${evidenceDeclaration}
    INSERT INTO [IncomingNachaEntryClassifications] (
      [Id], [IncomingNachaFileIngestionId], [EntryDetailId], [AddendaRecordId], [FunctionalClass],
      [EligibilityStatus], [RequiresLink], [RequiresManualResolution], [OriginalTraceRef], [ReturnReasonCode],
      [PrenoteStatus], [BusinessMeaning], [ClassifierVersion], [ClassificationEvidenceJson], [CreatedAt], [UpdatedAt])
    VALUES (
      NEWID(), ${sqlString(ingestionId)}, ${entryId}, ${addendaId}, N'Devolucion', N'Bloqueada', 1, 1,
      ${sqlString(originalTrace)}, ${sqlString(reasonCode)}, N'NoAplica', N'Devolución entrante sin correlación inequívoca',
      N'v1.0.0', @Evidence, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());
  `);
  database.execute(`
    ${evidenceDeclaration}
    INSERT INTO [IncomingNachaTransactionLinks] (
      [Id], [IncomingNachaFileIngestionId], [EntryDetailId], [AddendaRecordId], [AchTransactionId],
      [LinkType], [ConfidenceScore], [EvidenceJson], [LinkedAtUtc], [LinkedBy], [IsFinal], [CreatedAt], [UpdatedAt])
    VALUES (
      NEWID(), ${sqlString(ingestionId)}, ${entryId}, ${addendaId}, NULL, N'Ambiguous', 0.30, @Evidence,
      SYSUTCDATETIME(), N'sistema-e2e', 0, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());
  `);
}

async function repeatResolutionInBrowser(
  page: Page,
  linkId: string,
  achTransactionId: number
): Promise<{ status: number; body: ResolutionResult }> {
  return page.evaluate(async ({ id, transactionId }) => {
    const token = window.sessionStorage.getItem('ach.interbank.access_token');
    const response = await fetch(`/incoming-nacha-command-center/orphans/${id}/resolve`, {
      method: 'POST',
      headers: {
        'Authorization': `Bearer ${token}`,
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        achTransactionId: transactionId,
        justification: 'Repetición operativa controlada del mismo objetivo.',
        comment: 'Verificación de idempotencia E2E.',
        correlationId: crypto.randomUUID()
      })
    });
    return { status: response.status, body: await response.json() as ResolutionResult };
  }, { id: linkId, transactionId: achTransactionId });
}

function readPersistenceEvidence(
  database: G36SqlServer,
  transactionPrefix: string,
  ingestionId: string | null
): PersistenceEvidence {
  const ingestionFilter = ingestionId ? sqlString(ingestionId) : 'NULL';
  const rows = database.query<PersistenceEvidence>(`
    SELECT
      (SELECT COUNT(*) FROM [AchTransactions] WHERE [TransactionExternalId] LIKE ${sqlString(`${transactionPrefix}-%`)}) AS [transactions],
      (SELECT COUNT(*) FROM [AchTransactions] WHERE [TransactionExternalId] LIKE ${sqlString(`${transactionPrefix}-%`)} AND [State] = N'ReturnedByOperator') AS [returnedTransactions],
      (SELECT COUNT(*) FROM [AchTransactionStateEvents] e JOIN [AchTransactions] t ON t.[Id] = e.[AchTransactionId] WHERE t.[TransactionExternalId] LIKE ${sqlString(`${transactionPrefix}-%`)}) AS [stateEvents],
      (SELECT COUNT(*) FROM [IncomingNachaProcessingEvents] WHERE [IncomingNachaFileIngestionId] = ${ingestionFilter} AND [EventType] = N'OrphanManualResolution') AS [manualResolutionEvents],
      (SELECT COUNT(*) FROM [IncomingNachaTransactionLinks] WHERE [IncomingNachaFileIngestionId] = ${ingestionFilter} AND [IsFinal] = 1 AND [LinkType] = N'Manual') AS [finalLinks],
      (SELECT TOP (1) t.[ReturnReasonCode] FROM [AchTransactions] t WHERE t.[TransactionExternalId] LIKE ${sqlString(`${transactionPrefix}-%`)} AND t.[State] = N'ReturnedByOperator') AS [reasonCode],
      (SELECT COUNT(*) FROM [AchTransactionStateEvents] e JOIN [AchTransactions] t ON t.[Id] = e.[AchTransactionId] WHERE t.[TransactionExternalId] LIKE ${sqlString(`${transactionPrefix}-%`)} AND e.[AchReturnCodeId] IS NOT NULL) AS [returnCodeCatalogLinks],
      (SELECT TOP (1) [LinkedBy] FROM [IncomingNachaTransactionLinks] WHERE [IncomingNachaFileIngestionId] = ${ingestionFilter} AND [IsFinal] = 1) AS [resolvedBy]
  `);
  return rows[0];
}

function observeRuntime(page: Page): RuntimeEvidence {
  const evidence: RuntimeEvidence = { consoleErrors: [], requestFailures: [], unexpectedHttpErrors: [] };
  page.on('console', message => {
    if (message.type() !== 'error') return;

    const locationPath = message.location().url ? new URL(message.location().url).pathname : '';
    const expectedProfileGate = locationPath === '/NachaUpload/upload' && message.text().includes('422');
    if (!expectedProfileGate) evidence.consoleErrors.push(message.text());
  });
  page.on('pageerror', error => evidence.consoleErrors.push(error.message));
  page.on('requestfailed', request => evidence.requestFailures.push(`${request.method()} ${new URL(request.url()).pathname}`));
  page.on('response', response => {
    const pathName = new URL(response.url()).pathname;
    const expectedProfileGate = pathName === '/NachaUpload/upload' && response.status() === 422;
    if (response.status() >= 400 && !pathName.endsWith('/auth/refresh') && !expectedProfileGate) {
      evidence.unexpectedHttpErrors.push(`${response.request().method()} ${pathName} ${response.status()}`);
    }
  });
  return evidence;
}

async function attachEvidence(testInfo: TestInfo, evidence: unknown): Promise<void> {
  await testInfo.attach('ret-orphan-runtime-and-persistence.json', {
    body: JSON.stringify(evidence, null, 2),
    contentType: 'application/json'
  });
}

type ResolutionResult = {
  isResolved: boolean;
  status: string;
  processingEventId?: string | null;
  achTransactionStateEventId?: number | null;
  message: string;
  isIdempotentReplay: boolean;
};

type UploadResult = {
  status: number;
  body: {
    ingestionId?: string;
    profileSelectionStatus?: string;
    errors?: string[];
  };
};

type PersistenceEvidence = {
  transactions: number;
  returnedTransactions: number;
  stateEvents: number;
  manualResolutionEvents: number;
  finalLinks: number;
  reasonCode: string | null;
  returnCodeCatalogLinks: number;
  resolvedBy: string | null;
};

type RuntimeEvidence = {
  consoleErrors: string[];
  requestFailures: string[];
  unexpectedHttpErrors: string[];
};
