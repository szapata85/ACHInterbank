import { expect, Page, test } from '@playwright/test';
import { buildIncomingProcTransaccionesFixture } from './support/incoming-proc-transacciones-fixture';
import { findProcTransaccionesLogEvidence, snapshotSoapLogDirectory } from './support/local-soap-log-evidence';
import { G36RuntimeDb, pollUntil, type IncomingNachaIntegrationEvidenceRow } from './support/g36-runtime-db';

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
  'SOAP_LOCAL_LOG_DIR'
].filter((name) => !process.env[name]);

test.describe.configure({ mode: 'serial' });
test.skip(!liveOptIn, 'RUN_LOCAL_SOAP_PROC_TRANSACCIONES_E2E=true, ALLOW_LOCAL_MONETARY_SOAP_E2E=true y ProcTransacciones__Mode=Live son requeridos para habilitar este E2E monetario local.');
test.skip(requiredSettings.length > 0, `Faltan variables requeridas para el E2E LIVE: ${requiredSettings.join(', ')}. El spec no contiene fallbacks de credenciales, URLs ni conexiones.`);

test('carga NACHA-M entrante y deja evidencia correlacionada de Proc_Transacciones LIVE', async ({ page }) => {
  test.setTimeout(300_000);
  const startedAt = new Date();
  const runKey = `PTX-${Date.now().toString(36).toUpperCase()}`;
  const fixture = buildIncomingProcTransaccionesFixture(runKey);
  const db = new G36RuntimeDb('playwright-local-proc-transacciones');
  const logDirectory = process.env['SOAP_LOCAL_LOG_DIR']!;
  const logSnapshot = snapshotSoapLogDirectory(logDirectory);
  let ingestionId: string | null = null;

  try {
    await db.assertIncomingProcTransaccionesReady();
    const token = await authenticate();
    await seedSession(page, token);
    await page.goto(`${process.env['ACH_UI_URL']!.replace(/\/+$/, '')}/transactions/nacha-upload`);
    await expect(page.getByRole('button', { name: 'Cargar archivo' })).toBeVisible();

    await page.locator('input[type="file"]').setInputFiles({
      name: fixture.fileName,
      mimeType: 'application/octet-stream',
      buffer: fixture.content
    });
    const uploadResponse = page.waitForResponse((response) =>
      /\/NachaUpload\/upload(?:\?.*)?$/.test(response.url()) && response.request().method() === 'POST');
    await page.getByRole('button', { name: 'Cargar archivo' }).click();
    expect((await uploadResponse).ok(), 'La carga NACHA-M debe completar antes de consultar la persistencia.').toBeTruthy();

    const ingestion = await pollUntil(
      () => db.findIncomingNachaIngestion({ uniqueRunKey: fixture.uniqueRunKey }),
      `IncomingNachaFileIngestion correlacionada con ${fixture.uniqueRunKey}`
    );
    ingestionId = ingestion.id;
    expect(ingestion.resolvedAchCycleId, 'El fixture debe resolver cámara y ciclo antes de encolar.').toBeTruthy();

    const queue = await pollUntil(
      () => db.findIncomingDispatchQueueItem(ingestion.id),
      `IncomingNachaDispatchQueue para ${ingestion.id}`
    );
    expect(queue.correlationId).toBe(ingestion.correlationId);
    expect(queue.functionalClass).toBe('CreditoEntrante');
    expect(queue.eligibilityStatus).toBe('Elegible');

    const taskSnapshot = await db.accelerateIncomingPostProcessing();
    try {
      const evidence = await pollUntil(
        () => db.findIncomingProcTransaccionesEvidence(queue.id),
        `IncomingNachaIntegrationExecution para ${queue.id}`
      );
      const queueAfterDispatch = await pollUntil(
        () => db.findIncomingDispatchQueueItem(ingestion.id),
        `IncomingNachaDispatchQueue actualizada para ${ingestion.id}`
      );
      assertLiveProcTransaccionesEvidence(evidence, queue.id, Number(queueAfterDispatch.attemptCount));

      const localSoapLog = findProcTransaccionesLogEvidence(logDirectory, logSnapshot, startedAt, fixture.uniqueRunKey);
      expect(localSoapLog.text).toContain('Proc_Transacciones');
      expect(localSoapLog.text).not.toContain('Proc_Contrapartidas');
    } finally {
      await db.restoreIncomingPostProcessing(taskSnapshot);
    }
  } finally {
    if (ingestionId) {
      await db.cleanupIncomingProcTransaccionesRun(ingestionId);
    }
    await db.close();
  }
});

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

async function seedSession(page: Page, token: string): Promise<void> {
  await page.addInitScript((accessToken) => {
    window.sessionStorage.setItem('ach.interbank.access_token', accessToken);
  }, token);
}

function assertLiveProcTransaccionesEvidence(
  evidence: IncomingNachaIntegrationEvidenceRow,
  dispatchQueueId: string,
  attemptCount: number
): void {
  expect(evidence.dispatchQueueId).toBe(dispatchQueueId);
  expect(evidence.integrationType).toBe('Proc_Transacciones');
  expect(evidence.soapMethodName).toBe('Proc_Transacciones');
  expect(evidence.executionMode).toBe('Live');
  expect(evidence.responsePayloadXml).not.toBe('');
  expect(evidence.soapResponseCode).not.toBe('');
  expect(evidence.correlationId).toBe(`in-nacha-${dispatchQueueId.replace(/-/g, '')}-${attemptCount}`);
  expect(evidence.requestPayloadXml).not.toContain('<METODO>');
  expect(evidence.requestPayloadXml).not.toContain('Proc_Contrapartidas');
  expect(evidence.requestPayloadXml).not.toContain('RegistrarRespuestaTransaccion');
  expect(evidence.requestPayloadXml).not.toContain('PLValidarUsuarioBV');

  const outcomes = [evidence.isSuccessful, evidence.isFunctionalRejection, evidence.isTechnicalFailure]
    .filter((value) => value === true || value === 1).length;
  expect(outcomes, 'La auditoría debe clasificar la ejecución como éxito operativo, rechazo funcional o falla técnica.').toBe(1);
}
