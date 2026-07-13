import { expect, Page, test } from '@playwright/test';
import { findProcTransaccionesLogEvidence, snapshotSoapLogDirectory } from './support/local-soap-log-evidence';
import {
  assertEffectiveProcTransaccionesPreflight,
  getConfirmedSoapCorrelationTokens,
  maskSensitive,
  parseAuthorizedProcTransaccionesAmount,
  type SoapIntegrationSettings
} from './support/proc-transacciones-preflight';
import {
  assertAuthorizedOfficialProcTransaccionesEntryName,
  findOfficialProcTransaccionesEligibleEntries,
  loadOfficialProcTransaccionesArchiveInventory,
  selectOfficialProcTransaccionesEntry
} from './support/official-proc-transacciones-cenit';
import { G36RuntimeDb, pollUntil, type IncomingNachaDispatchQueueRow, type IncomingNachaIntegrationEvidenceRow } from './support/g36-runtime-db';

type AuthLoginResponse = { data?: { token?: string } };

const liveOptIn = process.env['RUN_LOCAL_SOAP_PROC_TRANSACCIONES_E2E'] === 'true'
  && process.env['ALLOW_LOCAL_MONETARY_SOAP_E2E'] === 'true'
  && (process.env['ProcTransacciones__Mode'] ?? '').trim().toLowerCase() === 'live';

const requiredSettings = [
  'ACH_E2E_DB_PROVIDER',
  'ACH_API_URL',
  'ACH_UI_URL',
  'ACH_USER',
  'ACH_PASS',
  'SOAP_LOCAL_LOG_DIR',
  'ACH_E2E_PROC_TRANSACCIONES_RECEIVER_ACCOUNT',
  'ACH_E2E_PROC_TRANSACCIONES_EXPECTED_AMOUNT',
  'ACH_E2E_PROC_TRANSACCIONES_EXPECTED_ENDPOINT',
  'CENIT_TEST_PACKAGE_PATH',
  'CENIT_TEST_PACKAGE_SHA256',
  'CENIT_TEST_ENTRY_NAME'
].filter((name) => !process.env[name]);

test.describe.configure({ mode: 'serial' });
test.skip(!liveOptIn, 'RUN_LOCAL_SOAP_PROC_TRANSACCIONES_E2E=true, ALLOW_LOCAL_MONETARY_SOAP_E2E=true y ProcTransacciones__Mode=Live son requeridos para habilitar este E2E monetario local.');
test.skip(requiredSettings.length > 0, `Faltan variables requeridas para el E2E LIVE: ${requiredSettings.join(', ')}. El spec no contiene fallbacks de credenciales, URLs ni conexiones.`);

test('carga NACHA-M entrante y deja evidencia correlacionada de Proc_Transacciones LIVE', async ({ page }) => {
  test.setTimeout(300_000);
  const startedAt = new Date();
  const db = new G36RuntimeDb('playwright-local-proc-transacciones');
  const logDirectory = process.env['SOAP_LOCAL_LOG_DIR']!;
  const logSnapshot = snapshotSoapLogDirectory(logDirectory);
  let ingestionId: string | null = null;

  try {
    const runtimeStatus = await assertRuntimeSurface();
    await db.assertIncomingProcTransaccionesReady();

    const expectedAmount = parseAuthorizedProcTransaccionesAmount(
      process.env['ACH_E2E_PROC_TRANSACCIONES_EXPECTED_AMOUNT']!
    );
    const selectedEntryName = assertAuthorizedOfficialProcTransaccionesEntryName(
      process.env['CENIT_TEST_ENTRY_NAME']!
    );
    const packagePath = process.env['CENIT_TEST_PACKAGE_PATH']!;
    const packageSha256 = process.env['CENIT_TEST_PACKAGE_SHA256']!;
    const archive = await loadOfficialProcTransaccionesArchiveInventory({
      packagePath,
      expectedPackageSha256: packageSha256
    });
    const selection = await selectOfficialProcTransaccionesEntry({
      packagePath,
      expectedPackageSha256: packageSha256,
      selectedEntryName
    });
    const eligibleEntries = findOfficialProcTransaccionesEligibleEntries(
      selection.selectedEntry.fileName,
      selection.selectedBytes,
      process.env['ACH_E2E_PROC_TRANSACCIONES_RECEIVER_ACCOUNT']!,
      expectedAmount
    );
    if (eligibleEntries.length === 0) {
      throw new Error('NO-GO HARNESS: NO_ELIGIBLE_ENTRY');
    }
    if (eligibleEntries.length > 1) {
      throw new Error('NO-GO HARNESS: MULTIPLE_ELIGIBLE_ENTRIES');
    }
    const selectedEligibleEntry = eligibleEntries[0];
    expect(selectedEligibleEntry.batchNumber).toBe('0000001');
    expect(selection.selectedEntry.fileName).toBe(selectedEntryName);
    expect(selection.selectedEntry.fixedLengthValid).toBe(true);
    expect(selection.selectedEntry.effectiveDate).toBe('20260713');
    expect(selection.selectedEntry.scc).toBe('220');
    expect(selection.selectedEntry.transactionCodes).toContain('32');
    expect(selection.selectedEntry.recordTypes).toEqual(expect.arrayContaining(['1', '5', '6', '7', '8', '9']));
    expect(selection.packageSha256).toBe(packageSha256.trim().toUpperCase());

    console.log(JSON.stringify({
      startedAtUtc: startedAt.toISOString(),
      packageSha256: selection.packageSha256,
      selectedEntryName,
      selectedEntrySha256: selection.selectedEntry.sha256,
      selectedEligibleEntry: {
        fileName: selectedEligibleEntry.fileName,
        batchNumber: selectedEligibleEntry.batchNumber,
        transactionCode: selectedEligibleEntry.transactionCode,
        amount: selectedEligibleEntry.amount,
        receiverAccount: maskSensitive(selectedEligibleEntry.receiverAccount),
        idTran: selectedEligibleEntry.idTran,
        idLote: selectedEligibleEntry.idLote
      },
      inventory: archive.entries.map((entry) => ({
        fileName: entry.fileName,
        size: entry.size,
        sha256: entry.sha256,
        recordCount: entry.recordCount,
        fixedLengthValid: entry.fixedLengthValid,
        recordTypes: entry.recordTypes,
        batchCount: entry.batchCount,
        scc: entry.scc,
        transactionCodes: entry.transactionCodes,
        addenda05Count: entry.addenda05Count,
        effectiveDate: entry.effectiveDate
      })),
      runtimeStatus
    }));

    const token = await authenticate();
    const settings = await getSoapIntegrationSettings(token);
    assertEffectiveProcTransaccionesPreflight(settings, process.env['ACH_E2E_PROC_TRANSACCIONES_EXPECTED_ENDPOINT']!);

    await db.assertIncomingProcTransaccionesFileAvailable(selectedEntryName);
    await seedSession(page, token);
    await page.goto(`${process.env['ACH_UI_URL']!.replace(/\/+$/, '')}/transactions/nacha-upload`);
    await expect(page.getByRole('button', { name: 'Cargar archivo' })).toBeVisible();

    await page.locator('input[type="file"]').setInputFiles({
      name: selectedEntryName,
      mimeType: 'application/octet-stream',
      buffer: selection.selectedBytes
    });
    const uploadResponse = page.waitForResponse((response) =>
      /\/NachaUpload\/upload(?:\?.*)?$/.test(response.url()) && response.request().method() === 'POST');
    await page.getByRole('button', { name: 'Cargar archivo' }).click();
    expect((await uploadResponse).ok(), 'La carga NACHA-M debe completar antes de consultar la persistencia.').toBeTruthy();

    const ingestion = await pollUntil(
      () => db.findIncomingNachaIngestionByFileName(selectedEntryName, startedAt),
      `IncomingNachaFileIngestion correlacionada con ${selectedEntryName}`
    );
    ingestionId = ingestion.id;
    expect(ingestion.fileName).toBe(selectedEntryName);
    expect(new Date(ingestion.uploadedAtUtc).getTime()).toBeGreaterThanOrEqual(startedAt.getTime());

    const queue = await pollUntil(
      async () => {
        const candidate = await db.findIncomingDispatchQueueItem(ingestion.id);
        return candidate && Number(candidate.attemptCount) >= 1 ? candidate : null;
      },
      `IncomingNachaDispatchQueue con intento real para ${ingestion.id}`
    );
    assertQueueGuardrails(queue);

    const evidence = await pollUntil(
      () => db.findIncomingProcTransaccionesEvidence(queue.id),
      `IncomingNachaIntegrationExecution para ${queue.id}`
    );
    assertLiveProcTransaccionesEvidence(evidence, queue, selectedEligibleEntry);

    const correlationTokens = getConfirmedSoapCorrelationTokens(evidence.requestPayloadXml, selectedEligibleEntry);
    const localSoapLog = findProcTransaccionesLogEvidence(logDirectory, logSnapshot, startedAt, correlationTokens);
    expect(localSoapLog.text).toContain('Proc_Transacciones');
    expect(localSoapLog.text).not.toContain('Proc_Contrapartidas');
    expect(localSoapLog.text).not.toContain('RegistrarRespuestaTransaccion');
    expect(localSoapLog.text).not.toContain('PLValidarUsuarioBV');

    console.log(JSON.stringify({
      ingestionId: ingestion.id,
      classificationId: queue.incomingNachaEntryClassificationId,
      transactionId: queue.achTransactionId,
      transactionLinkId: queue.incomingNachaTransactionLinkId,
      dispatchQueueId: queue.id,
      correlationId: queue.correlationId,
      attemptCount: queue.attemptCount,
      queueStatus: queue.queueStatus,
      lastResponseCode: queue.lastResponseCode,
      lastErrorCode: queue.lastErrorCode,
      integrationExecutionId: evidence.id,
      executionMode: evidence.executionMode,
      mappingSetId: evidence.mappingSetId,
      mappingVersion: evidence.mappingVersion,
      mappingSnapshotHash: evidence.mappingSnapshotHash,
      requestHash: evidence.requestHash,
      responseHash: evidence.responseHash,
      soapResponseCode: evidence.soapResponseCode,
      soapTechnicalStatus: evidence.soapTechnicalStatus,
      isSuccessful: evidence.isSuccessful,
      isFunctionalRejection: evidence.isFunctionalRejection,
      isTechnicalFailure: evidence.isTechnicalFailure,
      startedAtUtc: evidence.startedAtUtc,
      finishedAtUtc: evidence.finishedAtUtc,
      durationMs: evidence.durationMs,
      soapLogFile: localSoapLog.source,
      soapLogHasProcTransacciones: localSoapLog.text.includes('Proc_Transacciones'),
      soapLogHasCorrelationTokens: correlationTokens.every((tokenValue) => localSoapLog.text.includes(tokenValue)),
      runtimeStatus
    }));
  } finally {
    await db.close();
  }
});

async function assertRuntimeSurface(): Promise<{ liveStatus: number; readyStatus: number; readyHealthy: boolean; liveHealthy: boolean; scalarStatus: number; spaStatus: number }> {
  const liveResponse = await fetch('http://localhost:843/health/live', { signal: AbortSignal.timeout(10_000) });
  const liveText = await liveResponse.text();
  expect(liveResponse.ok, '/health/live debe responder HTTP 200.').toBeTruthy();
  expect(liveText).toMatch(/Healthy/i);

  const readyResponse = await fetch('http://localhost:843/health/ready', { signal: AbortSignal.timeout(10_000) });
  const readyText = await readyResponse.text();
  expect(readyResponse.ok, '/health/ready debe responder HTTP 200.').toBeTruthy();
  expect(readyText).toMatch(/Healthy/i);
  expect(readyText).toMatch(/database/i);

  const scalarResponse = await fetch('http://localhost:843/scalar/', { signal: AbortSignal.timeout(10_000) });

  const spaResponse = await fetch('http://localhost:743/login', { signal: AbortSignal.timeout(10_000) });
  const spaText = await spaResponse.text();
  expect(spaResponse.ok, 'La SPA debe responder en http://localhost:743/login.').toBeTruthy();
  expect((spaResponse.headers.get('content-type') ?? '').toLowerCase()).toContain('text/html');
  expect(spaText.toLowerCase()).toContain('<html');

  return {
    liveStatus: liveResponse.status,
    readyStatus: readyResponse.status,
    readyHealthy: readyText.toLowerCase().includes('healthy'),
    liveHealthy: liveText.toLowerCase().includes('healthy'),
    scalarStatus: scalarResponse.status,
    spaStatus: spaResponse.status
  };
}

async function authenticate(): Promise<string> {
  const response = await fetch(`${process.env['ACH_API_URL']!.replace(/\/+$/, '')}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username: process.env['ACH_USER'], password: process.env['ACH_PASS'] })
  });
  expect(response.ok, 'La autenticación UAT debe usar credenciales inyectadas por entorno.').toBeTruthy();
  const payload = await response.json() as AuthLoginResponse;
  expect(payload.data?.token, 'El login UAT debe devolver token.').toBeTruthy();
  return payload.data!.token!;
}

async function getSoapIntegrationSettings(token: string): Promise<SoapIntegrationSettings> {
  const response = await fetch(`${process.env['ACH_API_URL']!.replace(/\/+$/, '')}/api/users/soap-integrations`, {
    headers: { Authorization: `Bearer ${token}` }
  });
  expect(response.ok, 'El preflight debe leer la configuración autenticada existente de SOAP.').toBeTruthy();
  return response.json() as Promise<SoapIntegrationSettings>;
}

async function seedSession(page: Page, token: string): Promise<void> {
  await page.addInitScript((accessToken) => {
    window.sessionStorage.setItem('ach.interbank.access_token', accessToken);
  }, token);
}

function assertQueueGuardrails(queue: IncomingNachaDispatchQueueRow | null): asserts queue is IncomingNachaDispatchQueueRow {
  expect(queue, 'Debe existir exactamente una fila de cola para la entrada seleccionada.').toBeTruthy();
  expect(queue!.incomingNachaEntryClassificationId).toBeTruthy();
  expect(queue!.incomingNachaTransactionLinkId).toBeTruthy();
  expect(queue!.achTransactionId).toBeGreaterThan(0);
  expect(queue!.attemptCount, 'El harness live debe mantenerse en un único intento.').toBe(1);
  expect(Number(queue!.queueStatus), 'La cola no debe quedar en RetryPending.').not.toBe(5);
}

function assertLiveProcTransaccionesEvidence(
  evidence: IncomingNachaIntegrationEvidenceRow | null,
  queue: IncomingNachaDispatchQueueRow,
  eligibleEntry: { idTran: string; idLote: string }
): asserts evidence is IncomingNachaIntegrationEvidenceRow {
  expect(evidence, 'Debe existir una ejecución de integración para la cola seleccionada.').toBeTruthy();
  expect(evidence!.dispatchQueueId).toBe(queue.id);
  expect(evidence!.integrationType).toBe('Proc_Transacciones');
  expect(evidence!.soapMethodName).toBe('Proc_Transacciones');
  expect(evidence!.executionMode).toBe('Live');
  expect(evidence!.responsePayloadXml).not.toBe('');
  expect(evidence!.soapResponseCode).not.toBe('');
  expect(evidence!.correlationId).toBe(`in-nacha-${queue.id.replace(/-/g, '')}-${queue.attemptCount}`);
  expect(evidence!.requestPayloadXml).not.toContain('<METODO>');
  expect(evidence!.requestPayloadXml).not.toContain('Proc_Contrapartidas');
  expect(evidence!.requestPayloadXml).not.toContain('RegistrarRespuestaTransaccion');
  expect(evidence!.requestPayloadXml).not.toContain('PLValidarUsuarioBV');
  expect(evidence!.mappingSetId).toBeTruthy();
  expect(evidence!.mappingVersion).toBeTruthy();
  expect(evidence!.mappingSnapshotHash).toBeTruthy();
  expect(evidence!.requestHash).toBeTruthy();
  expect(evidence!.responseHash).toBeTruthy();

  const outcomes = [evidence!.isSuccessful, evidence!.isFunctionalRejection, evidence!.isTechnicalFailure]
    .filter((value) => value === true || value === 1).length;
  expect(outcomes, 'La auditoría debe clasificar la ejecución como éxito operativo, rechazo funcional o falla técnica.').toBe(1);

  const correlationTokens = getConfirmedSoapCorrelationTokens(evidence!.requestPayloadXml, eligibleEntry);
  expect(correlationTokens).toEqual([eligibleEntry.idTran, eligibleEntry.idLote]);
}
