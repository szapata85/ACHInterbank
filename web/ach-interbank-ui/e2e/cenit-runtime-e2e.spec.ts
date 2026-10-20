import { expect, Page, test, TestInfo } from '@playwright/test';
import { execFileSync } from 'node:child_process';
import {
  closeSync,
  existsSync,
  fsyncSync,
  mkdirSync,
  openSync,
  readdirSync,
  readFileSync,
  renameSync,
  rmSync,
  writeFileSync
} from 'node:fs';
import path from 'node:path';
import { loginThroughUi } from './support/live-ui-auth';

const cyclePrefix = 'CENIT-E2E-20260831';
const cycleIds = {
  ppd: `${cyclePrefix}-PPD`,
  ccd: `${cyclePrefix}-CCD`,
  ctx: `${cyclePrefix}-CTX`
} as const;
const cycleId = cycleIds.ctx;
const operationalDate = '2026-08-31';
const gatewayRoot = path.resolve(__dirname, '../../../.runtime/cenit');
const gatewayInput = path.join(gatewayRoot, 'input');
const gatewayOutput = path.join(gatewayRoot, 'output');
const sourceIds = {
  ack: 'CENIT-E2E-ACK-001',
  nack: 'CENIT-E2E-NACK-001',
  multi: 'CENIT-E2E-MULTI-001',
  reconciliation: 'CENIT-E2E-RECON-001',
  noActivity: 'CENIT-E2E-NOACT-001',
  conflict: 'CENIT-E2E-CONFLICT-001'
} as const;

test.describe.configure({ mode: 'serial' });

test('certifica CENIT outbound, Gateway local, respuestas, persistencia, API y SPA', async ({ page }, testInfo) => {
  test.setTimeout(300_000);
  const database = new DockerSqlServer();
  database.assertReady();
  resetLocalGateway();
  seedScenario(database);

  await loginThroughUi(page);
  for (const scenarioCycleId of Object.values(cycleIds)) {
    const exportResponse = await browserFetch(page, `/NachaExport/${scenarioCycleId}`, 'GET');
    expect(exportResponse.status, JSON.stringify({
      response: exportResponse.bodyText,
      persistedExports: database.query<{ id: number; fileName: string }>(`SELECT [Id] AS [id], [FileName] AS [fileName] FROM [AchFileExports] WHERE [AchCycleId] = N'${scenarioCycleId}'`)
    }, null, 2)).toBe(200);
  }

  const transactions = database.query<TransactionEvidence>(`
    SELECT t.[Id] AS [transactionId], t.[TransactionExternalId] AS [externalId], t.[TraceNumber] AS [traceNumber]
    FROM [AchTransactions] t
    WHERE t.[TransactionExternalId] LIKE N'CENIT-E2E-%'
    ORDER BY t.[Id]`);
  expect(transactions).toHaveLength(5);

  const exports = database.query<ExportEvidence>(`
    SELECT f.[Id] AS [fileId], f.[FileName] AS [fileName], f.[AchCycleId] AS [achCycleId], f.[TransmissionReference] AS [transmissionReference],
           t.[Id] AS [transactionId], t.[TransactionExternalId] AS [externalId], m.[TraceNumber] AS [traceNumber]
    FROM [AchFileExports] f
    JOIN [AchFileExportTransactions] m ON m.[AchFileExportId] = f.[Id]
    JOIN [AchTransactions] t ON t.[Id] = m.[AchTransactionId]
    WHERE f.[AchCycleId] LIKE N'${cyclePrefix}-%'
    ORDER BY f.[Id], m.[FileSequence]`);
  expect(exports).toHaveLength(5);

  const ppd = oneExport(exports, 'CENIT-E2E-PPD-001');
  const ccd = oneExport(exports, 'CENIT-E2E-CCD-001');
  expect(ccd.fileId).not.toBe(ppd.fileId);
  const ctxRows = exports.filter(item => item.externalId.startsWith('CENIT-E2E-CTX-'));
  expect(new Set(ctxRows.map(item => item.fileId)).size).toBe(1);
  const ctx = ctxRows[0];

  await expect.poll(() => readdirSync(gatewayInput).filter(name => !name.endsWith('.tmp')).length).toBeGreaterThanOrEqual(3);
  for (const fileName of new Set(exports.map(item => item.fileName))) {
    const artifact = path.join(gatewayInput, fileName);
    expect(existsSync(artifact), `Gateway input debe contener ${fileName}.`).toBe(true);
    expect(readFileSync(artifact).length).toBeGreaterThan(0);
  }

  await page.goto('/cenit/operacion/respuestas-camara');
  await expect(page.getByRole('heading', { name: 'Respuestas de cámara CENIT', exact: true })).toBeVisible();
  await expect(page.getByText('Pendiente', { exact: true }).first()).toBeVisible();
  await page.screenshot({ path: testInfo.outputPath('01-cenit-pending.png'), fullPage: true });

  const ackXml = fileAck(ppd.fileName, 'ACK-E2E-0001');
  const nackXml = fileNack(ccd.fileName, 'NACK-E2E-0001');
  const multiXml = operatorNack(ctx.fileName, ctxRows[0].traceNumber, ctxRows[1].traceNumber);
  writeInbound(sourceIds.ack, 'ack.xml', 'XML', ackXml, ppd.fileName, ppd.fileName, null);
  writeInbound(sourceIds.nack, 'nack.xml', 'XML', nackXml, ccd.fileName, ccd.fileName, null);
  writeInbound(sourceIds.multi, 'operator-multi.xml', 'XML', multiXml, ctx.fileName, ctx.fileName, null);
  writeInbound(sourceIds.reconciliation, 'reconciliation.nacha', 'Reconciliation', reconciliationArtifact(), null, null, cycleId);
  writeInbound(sourceIds.noActivity, 'no-activity.empty', 'NoActivity', '', null, null, cycleId);

  await expect.poll(async () => {
    const response = await browserFetch(page, '/api/cenit/chamber-responses?page=1&pageSize=200', 'GET');
    expect(response.status, response.bodyText).toBe(200);
    const body = response.json as ChamberPage;
    return body.items?.filter(item => Object.values(sourceIds).includes(item.sourceResponseId as never)).length ?? 0;
  }, { timeout: 30_000 }).toBe(6);

  const list = await browserFetch(page, '/api/cenit/chamber-responses?page=1&pageSize=200', 'GET');
  expect(list.status).toBe(200);
  expect(list.contentType).toContain('application/json');
  const pageBody = list.json as ChamberPage;
  const jobItems = pageBody.items.filter(item => Object.values(sourceIds).includes(item.sourceResponseId as never));
  assertRuntimeResponses(jobItems, ppd, ccd, ctxRows);

  const ack = jobItems.find(item => item.sourceResponseId === sourceIds.ack)!;
  const detail = await browserFetch(page, `/api/cenit/chamber-responses/${ack.id}`, 'GET');
  expect(detail.status).toBe(200);
  expect((detail.json as ChamberResponse).messageGroupId).toBe('ACK-E2E-0001');

  const replay = await browserFetch(page, '/api/cenit/chamber-responses', 'POST', {
    sourceResponseId: sourceIds.ack,
    sourceFileName: 'ack.xml',
    messageType: 'XML',
    content: ackXml,
    receivedAtUtc: '2026-08-31T18:01:00Z',
    relatedOutboundFileName: ppd.fileName,
    relatedReference: ppd.fileName
  });
  expect(replay.status).toBe(200);
  expect((replay.json as ChamberResponse).isDuplicate).toBe(true);

  const conflict = await browserFetch(page, '/api/cenit/chamber-responses', 'POST', {
    sourceResponseId: sourceIds.conflict,
    sourceFileName: 'late-ack.xml',
    messageType: 'XML',
    content: fileAck(ccd.fileName, 'ACK-E2E-LATE01'),
    receivedAtUtc: '2026-08-31T18:02:00Z',
    relatedOutboundFileName: ccd.fileName,
    relatedReference: ccd.fileName
  });
  expect(conflict.status).toBe(409);
  expect(conflict.contentType).toContain('application/problem+json');
  expect((conflict.json as ProblemDetails).title).toBe('CENIT_INVALID_LIFECYCLE_TRANSITION');

  const persisted = database.query<PersistenceEvidence>(`
    SELECT
      (SELECT COUNT(*) FROM [CenitChamberResponses] WHERE [SourceResponseId] LIKE N'CENIT-E2E-%') AS [responses],
      (SELECT COUNT(*) FROM [CenitChamberResponses] WHERE [SourceResponseId] = N'${sourceIds.multi}') AS [multiItems],
      (SELECT COUNT(*) FROM [CenitChamberResponses] WHERE [SourceResponseId] = N'${sourceIds.multi}' AND [AchTransactionId] IS NOT NULL) AS [correlatedMultiItems],
      (SELECT COUNT(*) FROM [CenitChamberResponses] WHERE [SourceResponseId] IN (N'${sourceIds.reconciliation}', N'${sourceIds.noActivity}') AND [AchFileExportId] IS NULL AND [AchCycleId] = N'${cycleId}') AS [sessionOutputs],
      (SELECT COUNT(*) FROM [AchFileExports] WHERE [AchCycleId] LIKE N'${cyclePrefix}-%' AND [ChamberResponseState] = N'Accepted') AS [acceptedFiles],
      (SELECT COUNT(*) FROM [AchFileExports] WHERE [AchCycleId] LIKE N'${cyclePrefix}-%' AND [ChamberResponseState] = N'Rejected') AS [rejectedFiles],
      (SELECT COUNT(*) FROM [AchFileExports] WHERE [AchCycleId] LIKE N'${cyclePrefix}-%' AND [ChamberResponseState] = N'OperatorRejected') AS [operatorRejectedFiles]`);
  expect(persisted).toEqual([{ responses: 7, multiItems: 2, correlatedMultiItems: 2, sessionOutputs: 2, acceptedFiles: 1, rejectedFiles: 1, operatorRejectedFiles: 1 }]);

  await page.reload();
  await expect(page.getByText('ACK aceptado', { exact: true })).toBeVisible();
  await expect(page.getByText('NACK rechazado', { exact: true }).first()).toBeVisible();
  await expect(page.getByText('Rechazo definitivo del operador', { exact: true }).first()).toBeVisible();
  await expect(page.getByText('Reconciliación', { exact: true }).first()).toBeVisible();
  await expect(page.getByText('Sin actividad', { exact: true }).first()).toBeVisible();
  await expect(page.getByText('InvalidTransition · CENIT_INVALID_LIFECYCLE_TRANSITION', { exact: true })).toBeVisible();
  await scrollGridRight(page);
  await expect(page.getByText('ERR_TRACE_NO_INV', { exact: true }).first()).toBeVisible();
  await expect(page.getByText(String(ctxRows[0].transactionId), { exact: true }).first()).toBeVisible();
  await page.screenshot({ path: testInfo.outputPath('02-cenit-runtime-certified.png'), fullPage: true });

  await testInfo.attach('cenit-runtime-evidence.json', {
    body: JSON.stringify({
      cycleId,
      transactionIds: transactions.map(item => item.transactionId),
      exports: [...new Map(exports.map(item => [item.fileId, { fileId: item.fileId, fileName: item.fileName }])).values()],
      responseIds: jobItems.map(item => ({ id: item.id, sourceResponseId: item.sourceResponseId, state: item.state })),
      persisted: persisted[0]
    }, null, 2),
    contentType: 'application/json'
  });
  console.log('CENIT_RUNTIME_EVIDENCE', JSON.stringify({
    cycleIds,
    transactionIds: transactions.map(item => item.transactionId),
    exports: [...new Map(exports.map(item => [item.fileId, { fileId: item.fileId, fileName: item.fileName }])).values()],
    responseIds: jobItems.map(item => ({ id: item.id, sourceResponseId: item.sourceResponseId, state: item.state })),
    persisted: persisted[0]
  }));
});

function seedScenario(database: DockerSqlServer): void {
  const clearingHouseId = database.scalar<number>(`SELECT [Id] AS [value] FROM [ClearingHouses] WHERE [Code] = N'CENIT'`);
  expect(clearingHouseId).toBeTruthy();
  const institutions = database.query<{ id: number; routingNumber: string; transitCode: string; checkDigit: string }>(`
    SELECT [Id] AS [id], [RoutingNumber] AS [routingNumber], [TransitCode] AS [transitCode], [CheckDigit] AS [checkDigit]
    FROM [FinancialInstitutions]
    WHERE [Status] = 1
    ORDER BY [Id]`).filter(item => calculateCheckDigit(`${item.routingNumber}${item.transitCode}`) === item.checkDigit);
  expect(institutions.length).toBeGreaterThanOrEqual(2);
  const originatingDfi = `${institutions[0].routingNumber}${institutions[0].transitCode}`;
  const receivingDfi = `${institutions[1].routingNumber}${institutions[1].transitCode}`;
  const documentType = database.scalar<string>(`SELECT TOP (1) [Code] AS [value] FROM [DocumentTypes] ORDER BY [Code]`);
  const personType = database.scalar<string>(`SELECT TOP (1) [Code] AS [value] FROM [PersonTypes] WHERE [Code] = N'PJ' OR [Code] = N'J' ORDER BY CASE WHEN [Code] = N'PJ' THEN 0 ELSE 1 END`);
  expect(documentType).toBeTruthy();
  expect(personType).toBeTruthy();
  database.execute(`
    SET IDENTITY_INSERT [CompanyEntryDescription] ON;
    IF NOT EXISTS (SELECT 1 FROM [CompanyEntryDescription] WHERE [Term] = N'CORPORATE')
      INSERT INTO [CompanyEntryDescription] ([Id], [Term], [Description], [StandardEntryClassCode], [IsActive])
      VALUES (39, N'CORPORATE', N'Pagos corporativos CENIT con múltiples adendas.', N'CTX', 1);
    SET IDENTITY_INSERT [CompanyEntryDescription] OFF;`);
  const descriptions = database.query<{ id: number; term: string; sec: string }>(`
    SELECT [Id] AS [id], [Term] AS [term], [StandardEntryClassCode] AS [sec]
    FROM [CompanyEntryDescription]`);
  expect(new Set(descriptions.map(item => item.sec)), JSON.stringify(descriptions)).toEqual(new Set(['PPD', 'CCD', 'CTX']));
  const descriptionId = (term: string) => descriptions.find(item => item.term === term)!.id;

  database.execute(`
    DECLARE @CycleConfigId int, @Cutoff time, @Start time, @End time, @Release time;
    SELECT TOP (1) @CycleConfigId = cfg.[Id], @Cutoff = cfg.[CutoffTime], @Start = cfg.[StartTime], @End = cfg.[EndTime], @Release = cfg.[OutputReleaseTime]
    FROM [ClearingHouseCycleConfigs] cfg
    WHERE cfg.[ClearingHouseId] = ${clearingHouseId}
      AND cfg.[IsActive] = 1
      AND cfg.[EffectiveFrom] <= '${operationalDate}'
      AND (cfg.[EffectiveTo] IS NULL OR cfg.[EffectiveTo] >= '${operationalDate}')
    ORDER BY cfg.[StartTime];

    INSERT INTO [AchCycles] (
      [Id], [CycleName], [ProcessingDate], [CutoffTime], [StartTime], [EndTime], [OutputReleaseTime],
      [OperationalStatus], [ReceptionToleranceMinutes], [AllowsExplicitReprocessing], [RescheduleOnHoliday],
      [OriginalProcessingDate], [CalendarDeferredAtUtc], [CalendarDeferralReason], [CalendarDeferralCount],
      [ClearingHouseId], [ClearingHouseCycleConfigId], [CreatedAt], [UpdatedAt])
    SELECT v.[Id], v.[CycleName], '${operationalDate}', @Cutoff, @Start, @End, @Release, 2, 30, 0, 0,
      NULL, NULL, NULL, 0, ${clearingHouseId}, @CycleConfigId, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()
    FROM (VALUES (N'${cycleIds.ppd}', N'901'), (N'${cycleIds.ccd}', N'902'), (N'${cycleIds.ctx}', N'903')) v([Id], [CycleName])
    WHERE NOT EXISTS (SELECT 1 FROM [AchCycles] c WHERE c.[Id] = v.[Id]);
    UPDATE c SET c.[CycleName] = v.[CycleName], c.[UpdatedAt] = SYSDATETIMEOFFSET()
    FROM [AchCycles] c JOIN (VALUES (N'${cycleIds.ppd}', N'901'), (N'${cycleIds.ccd}', N'902'), (N'${cycleIds.ctx}', N'903')) v([Id], [CycleName]) ON v.[Id] = c.[Id];

    DELETE FROM [CenitChamberResponses] WHERE [AchCycleId] LIKE N'${cyclePrefix}%' OR [SourceResponseId] LIKE N'CENIT-E2E-%';
    DELETE m FROM [AchFileExportTransactions] m JOIN [AchFileExports] f ON f.[Id] = m.[AchFileExportId] WHERE f.[AchCycleId] LIKE N'${cyclePrefix}%';
    DELETE FROM [AchFileExports] WHERE [AchCycleId] LIKE N'${cyclePrefix}%';
    DELETE a FROM [AchTransactionAddenda] a JOIN [AchTransactions] t ON t.[Id] = a.[AchTransactionId] WHERE t.[AchCycleId] LIKE N'${cyclePrefix}%';
    DELETE FROM [AchTransactions] WHERE [AchCycleId] LIKE N'${cyclePrefix}%';
    DELETE FROM [AchBatches] WHERE [AchCycleId] LIKE N'${cyclePrefix}%';

    DELETE FROM [Customers] WHERE [DocumentNumber] = N'900999900';
    DECLARE @ReceiverCustomer int;
    INSERT INTO [Customers] ([FirstName], [LastName], [PersonType], [CompanyName], [DocumentType], [DocumentNumber], [CreatedAt], [UpdatedAt])
    VALUES (N'RECEPTOR', N'E2E', N'${personType}', N'RECEPTOR E2E', N'${documentType}', N'900999900', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());
    SET @ReceiverCustomer = SCOPE_IDENTITY();
    INSERT INTO [CustomerAccounts] ([CustomerId], [AccountNumber], [CreatedAt], [UpdatedAt])
    VALUES
      (@ReceiverCustomer, N'422000000101', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
      (@ReceiverCustomer, N'422000000102', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
      (@ReceiverCustomer, N'422000000103', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
      (@ReceiverCustomer, N'422000000104', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
      (@ReceiverCustomer, N'422000000105', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());

    DECLARE @PpdBatch int, @CcdBatch int, @CtxBatch int;
    INSERT INTO [AchBatches] ([AchCycleId], [ServiceClassCode], [CompanyName], [CompanyIdentification], [CompanyEntryDescription], [CompanyEntryDescriptionId], [OriginOrOdfi], [EffectiveEntryDate], [BatchSequenceNumber], [TotalDebitAmount], [TotalCreditAmount], [CreatedAt], [UpdatedAt])
    VALUES (N'${cycleIds.ppd}', N'220', N'EMPRESA E2E', N'900000001', N'NOMINAS', ${descriptionId('NOMINAS')}, N'${originatingDfi}', '${operationalDate}', 101, 0, 1000, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());
    SET @PpdBatch = SCOPE_IDENTITY();
    INSERT INTO [AchBatches] ([AchCycleId], [ServiceClassCode], [CompanyName], [CompanyIdentification], [CompanyEntryDescription], [CompanyEntryDescriptionId], [OriginOrOdfi], [EffectiveEntryDate], [BatchSequenceNumber], [TotalDebitAmount], [TotalCreditAmount], [CreatedAt], [UpdatedAt])
    VALUES (N'${cycleIds.ccd}', N'220', N'EMPRESA E2E', N'900000001', N'PAGOS PSE', ${descriptionId('PAGOS PSE')}, N'${originatingDfi}', '${operationalDate}', 102, 0, 2000, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());
    SET @CcdBatch = SCOPE_IDENTITY();
    INSERT INTO [AchBatches] ([AchCycleId], [ServiceClassCode], [CompanyName], [CompanyIdentification], [CompanyEntryDescription], [CompanyEntryDescriptionId], [OriginOrOdfi], [EffectiveEntryDate], [BatchSequenceNumber], [TotalDebitAmount], [TotalCreditAmount], [CreatedAt], [UpdatedAt])
    VALUES (N'${cycleIds.ctx}', N'220', N'EMPRESA E2E', N'900000001', N'CORPORATE', ${descriptionId('CORPORATE')}, N'${originatingDfi}', '${operationalDate}', 103, 0, 9000, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());
    SET @CtxBatch = SCOPE_IDENTITY();

    INSERT INTO [AchTransactions] (
      [Amount], [TransactionExternalId], [Reference], [Type], [TransactionCode], [ServiceClassCode], [CompanyEntryDescriptionId],
      [CompanyName], [CompanyIdentification], [OriginatingDFI], [ReceivingDFI], [TraceNumber], [TraceSequenceNumber], [EffectiveEntryDate],
      [AddendaRecordIndicator], [IsPrenotification], [State], [StateChangedAtUtc], [ContrapartidasResponseCode], [ReturnReasonCode],
      [OriginalTraceRef], [RecipientIdNumber], [DiscretionaryData], [SourceAccountNumber], [DestinationAccountNumber],
      [SourceInstitutionId], [DestinationInstitutionId], [AchCycleId], [AchBatchId], [CreatedAt], [UpdatedAt],
      [Direction], [Origin], [MonetaryIntegrationRoute], [ClassificationStatus], [ClassificationVersion])
    VALUES
      (1000, N'CENIT-E2E-PPD-001', N'E2E-PPD', N'Credit', N'22', N'220', ${descriptionId('NOMINAS')}, N'EMPRESA E2E', N'900000001', N'${originatingDfi}', N'${receivingDfi}', N'${originatingDfi}0000101', 101, '${operationalDate}', 1, 0, N'Pending', SYSUTCDATETIME(), N'', N'', N'', N'900999900', N'', N'411000000101', N'422000000101', ${institutions[0].id}, ${institutions[1].id}, N'${cycleIds.ppd}', @PpdBatch, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET(), N'Outgoing', N'Cfa', N'None', N'Determined', 1),
      (2000, N'CENIT-E2E-CCD-001', N'E2E-CCD', N'Credit', N'22', N'220', ${descriptionId('PAGOS PSE')}, N'EMPRESA E2E', N'900000001', N'${originatingDfi}', N'${receivingDfi}', N'${originatingDfi}0000102', 102, '${operationalDate}', 1, 0, N'Pending', SYSUTCDATETIME(), N'', N'', N'', N'900999900', N'', N'411000000102', N'422000000102', ${institutions[0].id}, ${institutions[1].id}, N'${cycleIds.ccd}', @CcdBatch, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET(), N'Outgoing', N'Cfa', N'None', N'Determined', 1),
      (3000, N'CENIT-E2E-CTX-001', N'E2E-CTX-A', N'Credit', N'22', N'220', ${descriptionId('CORPORATE')}, N'EMPRESA E2E', N'900000001', N'${originatingDfi}', N'${receivingDfi}', N'${originatingDfi}0000103', 103, '${operationalDate}', 1, 0, N'Pending', SYSUTCDATETIME(), N'', N'', N'', N'900999900', N'', N'411000000103', N'422000000103', ${institutions[0].id}, ${institutions[1].id}, N'${cycleId}', @CtxBatch, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET(), N'Outgoing', N'Cfa', N'None', N'Determined', 1),
      (3000, N'CENIT-E2E-CTX-002', N'E2E-CTX-B', N'Credit', N'22', N'220', ${descriptionId('CORPORATE')}, N'EMPRESA E2E', N'900000001', N'${originatingDfi}', N'${receivingDfi}', N'${originatingDfi}0000104', 104, '${operationalDate}', 1, 0, N'Pending', SYSUTCDATETIME(), N'', N'', N'', N'900999900', N'', N'411000000104', N'422000000104', ${institutions[0].id}, ${institutions[1].id}, N'${cycleId}', @CtxBatch, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET(), N'Outgoing', N'Cfa', N'None', N'Determined', 1),
      (3000, N'CENIT-E2E-CTX-003', N'E2E-CTX-C', N'Credit', N'22', N'220', ${descriptionId('CORPORATE')}, N'EMPRESA E2E', N'900000001', N'${originatingDfi}', N'${receivingDfi}', N'${originatingDfi}0000105', 105, '${operationalDate}', 1, 0, N'Pending', SYSUTCDATETIME(), N'', N'', N'', N'900999900', N'', N'411000000105', N'422000000105', ${institutions[0].id}, ${institutions[1].id}, N'${cycleId}', @CtxBatch, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET(), N'Outgoing', N'Cfa', N'None', N'Determined', 1);

    INSERT INTO [AchTransactionAddenda] ([AchTransactionId], [AddendaType], [BusinessType], [Information], [Purpose], [Reference], [SequenceNumber], [CreatedAt], [UpdatedAt])
    SELECT t.[Id], N'05', N'Credit', N'PAGO E2E CENIT', N'PAGOS', t.[Reference], 1, SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()
    FROM [AchTransactions] t
    WHERE t.[TransactionExternalId] IN (N'CENIT-E2E-PPD-001', N'CENIT-E2E-CCD-001');

    INSERT INTO [AchTransactionAddenda] ([AchTransactionId], [AddendaType], [BusinessType], [Information], [Purpose], [Reference], [SequenceNumber], [CreatedAt], [UpdatedAt])
    SELECT t.[Id], N'05', N'Credit', CONCAT(N'CTX PAYMENT ', t.[TraceSequenceNumber], N'-', s.[n]), N'PAGOS', CONCAT(N'E2E-', t.[TraceSequenceNumber], N'-', s.[n]), s.[n], SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()
    FROM [AchTransactions] t CROSS JOIN (VALUES (1), (2)) s([n])
    WHERE t.[TransactionExternalId] LIKE N'CENIT-E2E-CTX-%';
  `);

  for (const scenarioCycleId of Object.values(cycleIds)) {
    expect(database.scalar<string>(`SELECT [Id] AS [value] FROM [AchCycles] WHERE [Id] = N'${scenarioCycleId}'`)).toBe(scenarioCycleId);
  }
}

function calculateCheckDigit(route: string): string {
  expect(route).toMatch(/^\d{8}$/);
  const weights = [3, 7, 1, 3, 7, 1, 3, 7];
  const sum = [...route].reduce((total, digit, index) => total + Number(digit) * weights[index], 0);
  return String((10 - (sum % 10)) % 10);
}

function resetLocalGateway(): void {
  expect(gatewayRoot.endsWith(path.join('.runtime', 'cenit'))).toBe(true);
  rmSync(gatewayRoot, { recursive: true, force: true });
  mkdirSync(gatewayInput, { recursive: true });
  mkdirSync(gatewayOutput, { recursive: true });
}

function oneExport(items: ExportEvidence[], externalId: string): ExportEvidence {
  const matches = items.filter(item => item.externalId === externalId);
  expect(matches).toHaveLength(1);
  return matches[0];
}

function writeInbound(sourceResponseId: string, artifactFileName: string, messageType: string, content: string, relatedOutboundFileName: string | null, relatedReference: string | null, achCycleId: string | null): void {
  mkdirSync(gatewayOutput, { recursive: true });
  atomicWrite(path.join(gatewayOutput, artifactFileName), content);
  atomicWrite(path.join(gatewayOutput, `${artifactFileName}.meta.json`), JSON.stringify({
    sourceResponseId,
    artifactFileName,
    messageType,
    receivedAtUtc: '2026-08-31T18:00:00Z',
    relatedOutboundFileName,
    relatedReference,
    transactionTraceNumber: null,
    achCycleId
  }));
}

function atomicWrite(target: string, content: string): void {
  const temporary = `${target}.tmp`;
  const descriptor = openSync(temporary, 'w');
  try {
    writeFileSync(descriptor, content, { encoding: 'utf8' });
    fsyncSync(descriptor);
  } finally {
    closeSync(descriptor);
  }
  renameSync(temporary, target);
}

function fileAck(fileName: string, groupId: string): string {
  return `<?xml version="1.0" encoding="UTF-8"?>
<FileAck xmlns="urn:xs:FileAck"><GroupHeader><GroupId>${groupId}</GroupId><Status>ACCP</Status><CreationDate>2026-08-31T13:00:00-05:00</CreationDate></GroupHeader><AdditionalRefs><RelatedRef>${fileName}</RelatedRef><OrigSender>00012839</OrigSender></AdditionalRefs></FileAck>`;
}

function fileNack(fileName: string, groupId: string): string {
  return `<?xml version="1.0" encoding="UTF-8"?>
<FileNack xmlns="urn:xs:FileNack"><GroupHeader><GroupId>${groupId}</GroupId><Status>RJCT</Status><CreationDate>2026-08-31T13:01:00-05:00</CreationDate></GroupHeader><AdditionalRefs><RelatedRef>${fileName}</RelatedRef><OrigSender>00012839</OrigSender></AdditionalRefs><FileErrorHandling><AdditionalDesc>Extensión de archivo inválida</AdditionalDesc><Status>RJCT</Status><ErrorCode>ERR_FILENAME_EXTENSION</ErrorCode></FileErrorHandling></FileNack>`;
}

function operatorNack(fileName: string, firstTrace: string, secondTrace: string): string {
  return `<?xml version="1.0" encoding="UTF-8"?>
<FileNack xmlns="urn:xs:FileNack"><GroupHeader><GroupId>OPR-E2E-0001</GroupId><Status>RJCT</Status><CreationDate>2026-08-31T13:02:00-05:00</CreationDate></GroupHeader><AdditionalRefs><RelatedRef>${fileName}</RelatedRef><OrigSender>00012839</OrigSender></AdditionalRefs><FileErrorHandling><AdditionalDesc>Primer número de rastreo inválido</AdditionalDesc><Status>RJCT</Status><BatchNo>103</BatchNo><TraceNo>${firstTrace}</TraceNo><ErrorCode>ERR_TRACE_NO_INV</ErrorCode></FileErrorHandling><FileErrorHandling><AdditionalDesc>Segundo número de rastreo inválido</AdditionalDesc><Status>RJCT</Status><BatchNo>103</BatchNo><TraceNo>${secondTrace}</TraceNo><ErrorCode>ERR_TRACE_NO_INV</ErrorCode></FileErrorHandling></FileNack>`;
}

function reconciliationArtifact(): string {
  return '101 000128390 0001283902608311300A10610CENIT                  CFA                    E2E00001';
}

function assertRuntimeResponses(items: ChamberResponse[], ppd: ExportEvidence, ccd: ExportEvidence, ctxRows: ExportEvidence[]): void {
  const ack = items.find(item => item.sourceResponseId === sourceIds.ack)!;
  expect(ack).toMatchObject({ responseType: 'Ack', state: 'Accepted', correlationOutcome: 'Matched', relatedFileId: ppd.fileId, xmlNamespace: 'urn:xs:FileAck', messageStatus: 'ACCP', originatingSender: '00012839', isApplied: true });
  const nack = items.find(item => item.sourceResponseId === sourceIds.nack)!;
  expect(nack).toMatchObject({ responseType: 'Nack', state: 'Rejected', correlationOutcome: 'Matched', relatedFileId: ccd.fileId, reasonCode: 'ERR_FILENAME_EXTENSION', isApplied: true });
  const operators = items.filter(item => item.sourceResponseId === sourceIds.multi).sort((a, b) => a.itemSequence - b.itemSequence);
  expect(operators).toHaveLength(2);
  expect(operators.map(item => item.itemSequence)).toEqual([1, 2]);
  expect(operators.map(item => item.itemCount)).toEqual([2, 2]);
  expect(new Set(operators.map(item => item.relatedTransactionId))).toEqual(new Set(ctxRows.slice(0, 2).map(item => item.transactionId)));
  expect(operators.every(item => item.reasonCode === 'ERR_TRACE_NO_INV')).toBe(true);
  expect(operators.some(item => item.relatedTransactionId === ctxRows[2].transactionId)).toBe(false);
  const reconciliation = items.find(item => item.sourceResponseId === sourceIds.reconciliation)!;
  expect(reconciliation).toMatchObject({ responseType: 'Reconciliation', state: 'Reconciliation', achCycleId: cycleId, isApplied: true });
  expect(reconciliation.relatedFileId ?? null).toBeNull();
  const noActivity = items.find(item => item.sourceResponseId === sourceIds.noActivity)!;
  expect(noActivity).toMatchObject({ responseType: 'NoActivity', state: 'NoActivity', achCycleId: cycleId, isApplied: true });
  expect(noActivity.relatedFileId ?? null).toBeNull();
}

async function scrollGridRight(page: Page): Promise<void> {
  const viewport = page.locator('.ag-center-cols-viewport').first();
  await viewport.evaluate(element => { element.scrollLeft = element.scrollWidth; });
}

async function browserFetch(page: Page, url: string, method: 'GET' | 'POST', body?: unknown): Promise<BrowserResponse> {
  return page.evaluate(async ({ target, verb, payload }) => {
    const token = window.sessionStorage.getItem('ach.interbank.access_token');
    const response = await fetch(target, {
      method: verb,
      headers: {
        Authorization: `Bearer ${token}`,
        ...(payload === undefined ? {} : { 'Content-Type': 'application/json' })
      },
      body: payload === undefined ? undefined : JSON.stringify(payload)
    });
    const contentType = response.headers.get('content-type') ?? '';
    const bytes = await response.arrayBuffer();
    const bodyText = new TextDecoder().decode(bytes);
    let json: unknown = null;
    if (contentType.includes('json') && bodyText) json = JSON.parse(bodyText);
    return { status: response.status, contentType, bodyText: contentType.includes('json') ? bodyText : '', json };
  }, { target: url, verb: method, payload: body });
}

class DockerSqlServer {
  private readonly databaseName: string;

  constructor() {
    this.databaseName = this.run('master', `SELECT TOP (1) [name] FROM sys.databases WHERE [database_id] > 4 ORDER BY [create_date] DESC;`, true).trim();
    if (!this.databaseName) throw new Error('No se encontró la base local ACHInterbank en SQL Server Docker.');
  }

  assertReady(): void {
    expect(this.scalar<string>('SELECT DB_NAME() AS [value]')).toBe(this.databaseName);
    expect(this.scalar<number>(`SELECT COUNT(*) AS [value] FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260831183444_CenitRuntimeE2EOwnershipAndMultiError'`)).toBe(1);
  }

  query<T>(selectSql: string): T[] {
    const output = this.run(this.databaseName, `${selectSql} FOR JSON PATH, INCLUDE_NULL_VALUES;`);
    const start = output.indexOf('[');
    const end = output.lastIndexOf(']');
    return start < 0 || end < start ? [] : JSON.parse(output.slice(start, end + 1).replace(/\r?\n/g, '')) as T[];
  }

  scalar<T>(selectSql: string): T | null {
    return this.query<{ value: T }>(selectSql)[0]?.value ?? null;
  }

  execute(sql: string): void {
    this.run(this.databaseName, sql);
  }

  private run(databaseName: string, sql: string, headersOff = false): string {
    const outputOptions = headersOff ? '-h -1 -W' : '-w 65535 -y 0';
    const script = `/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -d "$2" -C -b ${outputOptions} -Q "$1"`;
    try {
      return execFileSync('docker', ['exec', 'achinterbank-sqlserver-1', '/bin/bash', '-lc', script, '_', `SET NOCOUNT ON; SET QUOTED_IDENTIFIER ON; SET ANSI_NULLS ON; ${sql}`, databaseName], {
        encoding: 'utf8',
        windowsHide: true,
        maxBuffer: 10 * 1024 * 1024
      });
    } catch (error) {
      const sqlError = error as { stderr?: Buffer | string; stdout?: Buffer | string };
      const rawDetails = sqlError.stderr || sqlError.stdout;
      const details = Buffer.isBuffer(rawDetails) ? rawDetails.toString('utf8') : rawDetails;
      throw new Error(details?.trim() || 'sqlcmd falló sin detalle de diagnóstico.');
    }
  }
}

type BrowserResponse = { status: number; contentType: string; bodyText: string; json: unknown };
type ProblemDetails = { title: string; status: number; code?: string };
type TransactionEvidence = { transactionId: number; externalId: string; traceNumber: string };
type ExportEvidence = { fileId: number; fileName: string; achCycleId: string; transmissionReference: string; transactionId: number; externalId: string; traceNumber: string };
type PersistenceEvidence = { responses: number; multiItems: number; correlatedMultiItems: number; sessionOutputs: number; acceptedFiles: number; rejectedFiles: number; operatorRejectedFiles: number };
type ChamberPage = { items: ChamberResponse[]; total: number; page: number; pageSize: number };
type ChamberResponse = {
  id: string;
  isDuplicate: boolean;
  sourceResponseId: string;
  responseType: string;
  state: string;
  correlationOutcome: string;
  relatedFileId: number | null;
  relatedFileName: string | null;
  achCycleId: string | null;
  xmlNamespace: string | null;
  messageGroupId: string | null;
  messageStatus: string | null;
  originatingSender: string | null;
  relatedTransactionId: number | null;
  transactionTraceNumber: string | null;
  reasonCode: string | null;
  isApplied: boolean;
  problemCode: string | null;
  itemSequence: number;
  itemCount: number;
};
