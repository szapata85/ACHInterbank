import { expect, Page, test, TestInfo } from '@playwright/test';
import { existsSync, mkdirSync, readFileSync, writeFileSync } from 'node:fs';
import { dirname, resolve } from 'node:path';
import { spawnSync } from 'node:child_process';
import { G36RuntimeDb, pollUntil } from './support/g36-runtime-db';
import { loginThroughUi } from './support/live-ui-auth';

type LiveStage = 'create' | 'seed-and-replace-a' | 'replace-a' | 'replace-b' | 'dispatch-a' | 'dispatch-bc' | 'retry-c' | 'validate';
type LiveTransaction = { id: number; externalId: string; cycleId: string };
type LiveState = { createdAtUtc: string; transactions: Record<'A' | 'B' | 'C', LiveTransaction> };
type Institution = { id: number; name: string; isDefaultSource?: boolean; status?: number };
type EntryDescription = { id: number; term?: string; description?: string; isActive?: boolean };
type CreatedTransaction = { id: number; transactionExternalId: string; achCycleId?: string; achBatch?: { achCycleId?: string } | null };
type DispatchResult = { processed?: number; succeeded?: number; failed?: number; partial?: number };

const enabled = process.env['RUN_PROC_CONTRAPARTIDAS_LIVE_TESTS'] === 'true';
const stage = (process.env['PROC_CONTRA_LIVE_STAGE'] ?? 'validate') as LiveStage;
const api = (process.env['ACH_API_URL'] ?? 'http://localhost:843').replace(/\/+$/, '');
const providerKey = (process.env['ACH_E2E_DB_PROVIDER'] ?? 'unknown').toLowerCase();
const stateFile = resolve(process.cwd(), '..', '..', 'docs', 'uat', 'outgoing-monitoring-phase4-1-live', `.runtime-state-${providerKey}.json`);

test.skip(!enabled, 'RUN_PROC_CONTRAPARTIDAS_LIVE_TESTS=true es obligatorio para esta suite LIVE local.');
test.describe.configure({ mode: 'serial' });

test(`Fase 4.1 LIVE — ${stage}`, { tag: '@ProcContrapartidasLive' }, async ({ page }, testInfo) => {
  test.setTimeout(300_000);
  await loginThroughUi(page);
  const token = await page.evaluate(() => sessionStorage.getItem('ach.interbank.access_token'));
  expect(token, 'La autenticación real debe entregar un token de sesión.').toBeTruthy();
  const db = new G36RuntimeDb('phase4-1-live');

  try {
    await db.assertReady();
    if (stage === 'create') {
      await pauseScheduler(page, token!);
      const state = await createTransactions(page, token!, db);
      mkdirSync(dirname(stateFile), { recursive: true });
      writeFileSync(stateFile, JSON.stringify(state, null, 2), 'utf8');
      return;
    }

    const state = readState();
    if (stage === 'seed-and-replace-a') {
      await postJson(page, token!, '/Maintenance/seed', {});
      state.transactions.A = await createTransaction(page, token!, db, 'A');
      writeFileSync(stateFile, JSON.stringify(state, null, 2), 'utf8');
      return;
    }

    if (stage === 'replace-a') {
      state.transactions.A = await createTransaction(page, token!, db, 'A');
      writeFileSync(stateFile, JSON.stringify(state, null, 2), 'utf8');
      return;
    }

    if (stage === 'replace-b') {
      state.transactions.B = await createTransaction(page, token!, db, 'B');
      state.transactions.C = await createTransaction(page, token!, db, 'C');
      writeFileSync(stateFile, JSON.stringify(state, null, 2), 'utf8');
      return;
    }

    if (stage === 'dispatch-a') {
      const result = await dispatch(page, token!, state.transactions.A.id);
      expect(result.processed).toBe(1);
      expect(result.succeeded).toBe(1);
      const evidence = await pollUntil(() => db.findDispatchEvidence(state.transactions.A.id), 'respuesta LIVE TX-A', 120_000);
      expect(evidence.soapMethodName).toBe('Proc_Contrapartidas');
      expect(evidence.executionMode).toMatch(/^Live$/i);
      expect(evidence.soapEndpoint).toMatch(/^http:\/\/(?:host\.docker\.internal|localhost|127\.0\.0\.1):7083\/WSCFAACH\.svc$/i);
      expect(evidence.transportStatus).toBe('Succeeded');
      expect(evidence.responsePayloadXml).toBeTruthy();
      expect(await db.countDispatchAttempts(state.transactions.A.id)).toBe(1);
      return;
    }

    if (stage === 'dispatch-bc') {
      for (const key of ['B', 'C'] as const) {
        const attemptsBefore = await db.countDispatchAttempts(state.transactions[key].id);
        if (attemptsBefore === 0) {
          stopLocalWcf();
          const result = await dispatch(page, token!, state.transactions[key].id);
          expect(result.processed).toBe(1);
          expect((result.failed ?? 0) + (result.partial ?? 0)).toBe(1);
        }
        const evidence = await pollUntil(() => db.findDispatchEvidence(state.transactions[key].id), `error LIVE TX-${key}`, 120_000);
        expect(evidence.soapMethodName).toBe('Proc_Contrapartidas');
        expect(evidence.executionMode).toMatch(/^Live$/i);
        expect(evidence.soapEndpoint).toMatch(/^http:\/\/(?:host\.docker\.internal|localhost|127\.0\.0\.1):7083\/WSCFAACH\.svc$/i);
        expect(String(evidence.soapTechnicalStatus)).toMatch(/RetryableFailure|TechnicalException|UnknownFailure/i);
        expect(evidence.businessStatus).not.toBe('Rejected');
        expect(await db.countDispatchAttempts(state.transactions[key].id)).toBe(1);
      }
      return;
    }

    if (stage === 'retry-c') {
      const result = await dispatch(page, token!, state.transactions.C.id);
      expect(result.processed).toBe(1);
      expect(result.succeeded).toBe(1);
      const evidence = await pollUntil(() => db.findDispatchEvidence(state.transactions.C.id), 'respuesta LIVE final TX-C', 120_000);
      expect(evidence.transportStatus).toBe('Succeeded');
      expect(evidence.businessStatus).toBe('Success');
      expect(await db.countDispatchAttempts(state.transactions.C.id)).toBe(2);
      expect(await db.findTransactionByExternalId(state.transactions.C.externalId)).not.toBeNull();
      return;
    }

    await validateMonitor(page, token!, state, db, testInfo);
    await resumeScheduler(page, token!);
  } finally {
    await db.close();
  }
});

async function createTransactions(page: Page, token: string, db: G36RuntimeDb): Promise<LiveState> {
  const transactions = {} as LiveState['transactions'];
  for (const key of ['A', 'B', 'C'] as const) {
    transactions[key] = await createTransaction(page, token, db, key);
  }
  return { createdAtUtc: new Date().toISOString(), transactions };
}

async function createTransaction(page: Page, token: string, db: G36RuntimeDb, key: 'A' | 'B' | 'C'): Promise<LiveTransaction> {
  const institutions = await getJson<Institution[]>(page, token, '/financial-institutions');
  const source = institutions.find(item => item.isDefaultSource);
  const destination = institutions.find(item => !item.isDefaultSource && (item.status ?? 1) === 1);
  expect(source, 'Debe existir una institución origen CFA.').toBeTruthy();
  expect(destination, 'Debe existir una institución destino sintética activa.').toBeTruthy();
  const descriptions = await getJson<EntryDescription[]>(page, token, '/transactions/company-entry-descriptions');
  const description = descriptions.find(item => item.isActive !== false);
  expect(description, 'Debe existir un concepto activo.').toBeTruthy();
  const suffix = Date.now().toString().slice(-9);
  const index = key.charCodeAt(0) - 'A'.charCodeAt(0);
    const externalId = `LIVE-F4.1-PC-TX-${key}-${suffix}`;
    const created = await postJson<CreatedTransaction>(page, token, '/transactions', {
      amount: 1000 + index,
      transactionExternalId: externalId,
      reference: externalId.replace(/\./g, '-'),
      type: 2,
      accountType: 1,
      isPrenotification: false,
      destinationInstitutionId: destination!.id,
      sourceAccountNumber: `99001${suffix}${index}`,
      destinationAccountNumber: `99002${suffix}${index}`,
      recipientIdNumber: `UAT${suffix}${index}`,
      recipientName: `RECEPTOR UAT ${key}`,
      requiresIdentityValidation: false,
      companyName: 'EMPRESA UAT F41',
      companyIdentification: `F41${suffix.slice(-7)}`,
      companyEntryDescriptionId: description!.id,
      sourcePersonType: 'PJ',
      recipientPersonType: 'PJ',
      addendas: [{
        addendaType: '05',
        collectorId: `UATCOL${suffix}`,
        receiverCustomerCode: `UATCLI${suffix}${index}`,
        serviceDescription: 'PRUEBA LOCAL',
        information: externalId
      }]
    });
    const persisted = await pollUntil(() => db.findTransactionByExternalId(externalId), `raíz ${key}`, 60_000);
    expect(persisted.id).toBe(created.id);
    expect(persisted.achCycleId).toBeTruthy();
    await expect.poll(() => db.countDispatchItems(created.id), { timeout: 60_000 }).toBe(1);
  return { id: created.id, externalId, cycleId: persisted.achCycleId };
}

async function validateMonitor(page: Page, token: string, state: LiveState, db: G36RuntimeDb, testInfo: TestInfo): Promise<void> {
  for (const key of ['A', 'B', 'C'] as const) {
    const tx = state.transactions[key];
    const list = await getJson<{ items: Array<{ id: number; transactionExternalId: string; processStatusDisplayName: string; initialResultDisplayName: string }> }>(
      page, token, `/api/transactions/outgoing-monitoring?transactionExternalId=${encodeURIComponent(tx.externalId)}&pageNumber=1&pageSize=10`
    );
    expect(list.items).toHaveLength(1);
    expect(list.items[0].id).toBe(tx.id);
    const detail = await getJson<{ timeline: Array<{ title: string }>; technicalDetails?: unknown[] }>(page, token, `/api/transactions/outgoing-monitoring/${tx.id}`);
    expect(detail.timeline.length).toBeGreaterThan(0);
    expect(await db.findTransactionByExternalId(tx.externalId)).not.toBeNull();
  }

  expect(await db.countDispatchAttempts(state.transactions.A.id)).toBe(1);
  const transactionBAttempts = await db.countDispatchAttempts(state.transactions.B.id);
  expect(transactionBAttempts).toBeGreaterThanOrEqual(1);
  expect(transactionBAttempts).toBeLessThanOrEqual(2);
  expect(await db.countDispatchAttempts(state.transactions.C.id)).toBe(2);

  await page.goto('/transactions/outgoing-monitoring');
  const monitor = page.getByTestId('outgoing-monitoring-page');
  await expect(monitor.getByRole('heading', { name: 'Monitoreo de transacciones de salida' })).toBeVisible();
  const cycle = page.locator('mat-form-field').filter({ hasText: 'Ciclo' }).locator('mat-select');
  await expect(cycle).toBeVisible();
  await cycle.click();
  await expect(page.getByRole('option', { name: 'Todos los ciclos' })).toBeVisible();
  expect(await page.getByRole('option').count()).toBeGreaterThan(1);
  await page.keyboard.press('Escape');

  await page.getByLabel('Identificador').fill(state.transactions.C.externalId);
  const response = page.waitForResponse(item => item.request().method() === 'GET' && new URL(item.url()).pathname === '/api/transactions/outgoing-monitoring');
  await page.getByRole('button', { name: /Buscar/ }).click();
  expect((await response).status()).toBe(200);
  await expect(page.getByText(state.transactions.C.externalId, { exact: false }).first()).toBeVisible();
  await page.getByRole('button', { name: /Ver detalle/ }).first().click();
  const integrationCard = page.locator('mat-card').filter({ hasText: 'Resultado de integración' });
  await expect(integrationCard).toContainText('Intentos');
  await expect(integrationCard.getByText('2', { exact: true })).toBeVisible();
  await page.getByTestId('outgoing-technical-detail').locator('mat-expansion-panel-header').click();
  await expect(page.getByText('Proc_Contrapartidas', { exact: false }).first()).toBeVisible();
  const screenshotPath = testInfo.outputPath('fase4-1-live-monitor-sanitizado.png');
  await page.screenshot({ path: screenshotPath, fullPage: true, mask: [page.locator('[data-sensitive="true"]')] });
}

async function pauseScheduler(page: Page, token: string): Promise<void> {
  const response = await page.request.post(`${api}/api/scheduler/tasks/CONTRAPARTIDA_DISPATCH/pause`, { headers: auth(token) });
  expect([204, 409], 'El scheduler debe quedar pausado antes de crear el lote.').toContain(response.status());
}

async function resumeScheduler(page: Page, token: string): Promise<void> {
  const response = await page.request.post(`${api}/api/scheduler/tasks/CONTRAPARTIDA_DISPATCH/resume`, { headers: auth(token) });
  expect([204, 409]).toContain(response.status());
}

async function dispatch(page: Page, token: string, transactionId: number): Promise<DispatchResult> {
  return postJson<DispatchResult>(page, token, '/api/uat/contrapartidas/dispatch-cycle', { transactionId });
}

async function getJson<T>(page: Page, token: string, path: string): Promise<T> {
  const response = await page.request.get(`${api}${path}`, { headers: auth(token) });
  expect(response.ok(), `GET ${path} debe responder correctamente (${response.status()}).`).toBeTruthy();
  return await response.json() as T;
}

async function postJson<T>(page: Page, token: string, path: string, data: unknown): Promise<T> {
  const response = await page.request.post(`${api}${path}`, { headers: auth(token), data });
  const body = await response.text();
  let diagnostic = '';
  if (!response.ok()) {
    try {
      const problem = JSON.parse(body) as {
        errorCode?: string;
        ErrorCode?: string;
        message?: string;
        Message?: string;
        title?: string;
        Title?: string;
        data?: Array<{ code?: string; message?: string }>;
        Data?: Array<{ Code?: string; Message?: string }>;
      };
      const detail = problem.data?.[0];
      const legacyDetail = problem.Data?.[0];
      diagnostic = ` ${problem.errorCode ?? problem.ErrorCode ?? detail?.code ?? legacyDetail?.Code ?? ''} ${problem.message ?? problem.Message ?? problem.title ?? problem.Title ?? detail?.message ?? legacyDetail?.Message ?? ''}`
        .replace(/\d{6,}/g, '[SANITIZADO]');
    } catch {
      diagnostic = ' respuesta no estructurada';
    }
  }
  expect(response.ok(), `POST ${path} debe responder correctamente (${response.status()}).${diagnostic}`).toBeTruthy();
  return JSON.parse(body) as T;
}

function auth(token: string): Record<string, string> {
  return { Authorization: `Bearer ${token}` };
}

function readState(): LiveState {
  expect(existsSync(stateFile), 'La etapa create debe ejecutarse antes.').toBeTruthy();
  return JSON.parse(readFileSync(stateFile, 'utf8')) as LiveState;
}

function stopLocalWcf(): void {
  const command = [
    "$process=Get-CimInstance Win32_Process -Filter \"Name='iisexpress.exe'\" | Where-Object { $_.CommandLine -match 'WSCFAACH' } | Select-Object -First 1",
    'if($process){ Stop-Process -Id $process.ProcessId -Force }'
  ].join('; ');
  const result = spawnSync('powershell.exe', ['-NoProfile', '-NonInteractive', '-Command', command], {
    encoding: 'utf8',
    timeout: 10_000
  });
  expect(result.status, 'El proceso local WSCFAACH debe detenerse para provocar la indisponibilidad controlada.').toBe(0);
}
