import { expect, Page, test } from '@playwright/test';
import { G36SqlServer, sqlString } from './support/g36-sqlserver';

const uiBaseUrl = (process.env['ACH_UI_URL'] ?? 'http://localhost:743').replace(/\/+$/, '');
const apiBaseUrl = (process.env['ACH_API_URL'] ?? 'http://localhost:843').replace(/\/+$/, '');
const username = process.env['E2E_ADMIN_USER'] ?? process.env['ACH_USER'] ?? 'admin';
const password = process.env['E2E_ADMIN_PASSWORD'] ?? process.env['ACH_PASS'] ?? '';

test.describe.configure({ mode: 'serial' });
test.skip(!password, 'E2E_ADMIN_PASSWORD o ACH_PASS es obligatorio para el flujo real.');

test('transacción y simulador consumen un ciclo operativo real y explican bloqueos', async ({ page }) => {
  test.setTimeout(180_000);
  const jsErrors: string[] = [];
  const httpErrors: string[] = [];
  page.on('pageerror', error => jsErrors.push(error.message));
  page.on('response', response => {
    if (response.status() >= 500 || (response.status() === 404 && !response.url().includes('favicon'))) {
      httpErrors.push(`${response.status()} ${response.url()}`);
    }
  });

  const token = await login(page);
  const sql = new G36SqlServer();
  sql.assertReady();
  const today = new Date().toISOString().slice(0, 10);
  const suffix = Date.now().toString().slice(-8);
  const houses = await apiGet<any[]>('/clearing-houses/operational', token);
  const achcol = houses.find(item => item.code === 'ACHCOL');
  expect(achcol, 'ACHCOL debe estar operativa para el flujo real.').toBeTruthy();

  const configResponse = await apiPost('/clearing-house-cycle-configs', token, {
    clearingHouseId: achcol.id,
    cycleName: `PW ciclo canónico ${suffix}`,
    startTime: '00:00:00',
    endTime: '23:59:00',
    cutoffTime: '23:30:00',
    effectiveFrom: `${today}T00:00:00Z`
  }, [200]);
  const config = await configResponse.json();
  const operationalCycleCode = await ensureOperationalCycle(token, achcol.id, config, today);

  const institutions = await apiGet<any[]>('/financial-institutions', token);
  const external = institutions.find(item => !item.isDefaultSource && (item.status === 1 || item.status === 'Active'));
  expect(external, 'Debe existir una entidad externa activa.').toBeTruthy();
  const concepts = await apiGet<any[]>('/transactions/company-entry-descriptions', token);
  const concept = concepts.find(item => item.isActive !== false);
  expect(concept, 'Debe existir una descripción de entrada activa.').toBeTruthy();

  const sourceAccount = `44${suffix}`;
  const destinationAccount = `55${suffix}`;
  const recipientId = `70${suffix}`;
  await apiPost('/transactions', token, transactionPayload({
    externalId: `PW323-PRE-${suffix}`,
    amount: 0,
    type: 3,
    isPrenotification: true,
    sourceAccount,
    destinationAccount,
    recipientId,
    destinationInstitutionId: external.id,
    companyEntryDescriptionId: concept.id
  }), [200, 201]);
  await activateThirdParty(token, sourceAccount, destinationAccount, recipientId, external.id);

  await page.goto(`${uiBaseUrl}/transactions/create`);
  await expect(page.getByRole('heading', { name: 'Crear transacción ACH' })).toBeVisible();
  await label(page, 'Descripción de la entrada').locator('button.limpiar').click();
  await page.getByRole('button', { name: 'Registrar transacción' }).click();
  const summary = page.locator('.validation-summary');
  await expect(summary).toBeFocused();
  await expect(summary).toContainText('Faltan datos para registrar la transacción');
  await expect(summary).toContainText('Descripción de la entrada');
  await expect(summary).toContainText('Información adicional');
  await expect(summary).not.toContainText('companyEntryDescriptionId');
  await expect(page.locator('body')).not.toContainText('[object Object]');

  const initialCount = await incompleteCount(page);
  await fillInput(page, 'Monto', '1500');
  expect(await incompleteCount(page)).toBeLessThan(initialCount);
  await summary.getByRole('button', { name: /ID operación cliente/ }).click();
  await expect(page.locator('[data-validation-path="transactionExternalId"]')).toBeFocused();

  const externalId = `PW323-TX-${suffix}`;
  await fillInput(page, 'ID operación cliente', externalId);
  await fillInput(page, 'Cuenta origen', sourceAccount);
  await fillInput(page, 'Identificación usuario originador', `PW${suffix}`);
  await fillInput(page, 'Nombre usuario originador', `PWJOB${suffix}`.slice(0, 16));
  await selectSearchable(page, 'Institución destino', external.name);
  await selectSearchable(page, 'Cuenta destino', destinationAccount);
  await fillInput(page, 'Nombre del receptor', 'RECEPTOR JOB 323');
  await selectSearchable(page, 'Descripción de la entrada', concept.term ?? concept.description);
  await selectSearchable(page, 'Código tipo registro adenda', '05 - Información adicional');
  await fillInput(page, 'Información', `JOB323-${suffix}`);
  await expect(page.locator('.form-incomplete-help')).toHaveCount(0);

  const createResponse = page.waitForResponse(response =>
    response.request().method() === 'POST' && new URL(response.url()).pathname.endsWith('/transactions'));
  await page.getByRole('button', { name: 'Registrar transacción' }).click();
  expect((await createResponse).status()).toBe(201);
  await expect(page).toHaveURL(/\/transactions\/list(?:\?.*)?$/);

  sql.execute(`UPDATE [AchTransactions]
               SET [AchCycleId] = ${sqlString(operationalCycleCode)}
               WHERE [TransactionExternalId] = ${sqlString(externalId)};`);
  expect(sql.scalar<string>(`SELECT [AchCycleId] AS [value] FROM [AchTransactions]
                             WHERE [TransactionExternalId] = ${sqlString(externalId)}`)).toBe(operationalCycleCode);
  const transactionAmountBeforeRepair = sql.scalar<number>(`SELECT [Amount] AS [value] FROM [AchTransactions]
                                                             WHERE [TransactionExternalId] = ${sqlString(externalId)}`);
  expect(transactionAmountBeforeRepair).toBe(1500);

  sql.execute(`UPDATE [AchCycles] SET [ClearingHouseCycleConfigId] = NULL
               WHERE [Id] = ${sqlString(operationalCycleCode)};`);
  try {
    const firstRepairResponse = await apiPost('/ach-cycles/repair-configuration-links', token, {}, [200]);
    const firstRepair = await firstRepairResponse.json();
    expect(firstRepair.repairedCount).toBeGreaterThanOrEqual(1);
    expect(sql.scalar<number>(`SELECT [ClearingHouseCycleConfigId] AS [value] FROM [AchCycles]
                               WHERE [Id] = ${sqlString(operationalCycleCode)}`)).toBe(config.id);

    const secondRepairResponse = await apiPost('/ach-cycles/repair-configuration-links', token, {}, [200]);
    expect((await secondRepairResponse.json()).repairedCount).toBe(0);
    const repairedAvailable = await apiGet<any[]>(
      `/api/uat/nacha-inbound-simulator/available-cycles?clearingHouseCode=ACHCOL&processingDate=${today}&scenarioType=IncomingCredit`,
      token);
    expect(repairedAvailable.some(item => item.cycleCode === operationalCycleCode)).toBeTruthy();
  } finally {
    if (sql.scalar<number>(`SELECT [ClearingHouseCycleConfigId] AS [value] FROM [AchCycles]
                            WHERE [Id] = ${sqlString(operationalCycleCode)}`) == null) {
      sql.execute(`UPDATE [AchCycles] SET [ClearingHouseCycleConfigId] = ${config.id}
                   WHERE [Id] = ${sqlString(operationalCycleCode)};`);
    }
  }

  // La API limita ráfagas por IP; deje cerrar la ventana usada por la creación real.
  await page.waitForTimeout(1_100);
  await page.goto(`${uiBaseUrl}/uat/nacha-inbound-simulator`);
  await expect(page.getByRole('heading', { name: 'Simulador NACHA-M de entrada', exact: true })).toBeVisible();
  expect(await page.locator('a[href="/uat/nacha-inbound-simulator"]').count()).toBe(1);
  const cycleSelector = page.locator('ui-selector-buscable[formcontrolname="cycleCode"]');
  await expect(cycleSelector).toBeVisible();
  await expect(page.locator('select[formcontrolname="cycleCode"]')).toHaveCount(0);
  await expect(page.locator('input[formcontrolname="cycleCode"]')).toHaveCount(0);
  await page.locator('select[formcontrolname="clearingHouseCode"]').selectOption('ACHCOL');
  await page.locator('input[formcontrolname="businessDate"]').fill(today);
  await page.locator('input[formcontrolname="businessDate"]').dispatchEvent('change');
  await expect(cycleSelector.locator('button.opcion')).toHaveCount(1);
  await expect(cycleSelector).toContainText(config.cycleName);
  await expect(cycleSelector).toContainText('ACH Colombia');
  await expect(cycleSelector).toContainText('transacciones');
  await cycleSelector.locator('input').fill(config.cycleName);
  await cycleSelector.locator('button.opcion').click();
  expect(operationalCycleCode).toBeTruthy();
  await page.locator('select[formcontrolname="originFinancialInstitutionId"]').selectOption({ index: 1 });

  const generateResponse = page.waitForResponse(response =>
    response.request().method() === 'POST' && new URL(response.url()).pathname.endsWith('/nacha-inbound-simulator/generate'));
  await page.getByRole('button', { name: 'Generar archivo' }).click();
  const generated = await generateResponse;
  expect(generated.status()).toBe(201);
  const generatedBody = await generated.json();
  const simulation = await apiGet<any>(`/api/uat/nacha-inbound-simulator/${generatedBody.id}`, token);
  expect(simulation.entries[0].transactionId).toBeGreaterThan(0);
  expect(simulation.entries[0].amount).toBe(transactionAmountBeforeRepair);
  expect(simulation.entries[0].isSynthetic).toBe(false);
  expect(sql.scalar<number>(`SELECT [Amount] AS [value] FROM [AchTransactions]
                             WHERE [TransactionExternalId] = ${sqlString(externalId)}`)).toBe(transactionAmountBeforeRepair);
  await expect(page.getByText('Archivo generado y pendiente de carga')).toBeVisible();

  const manipulated = await apiPost('/api/uat/nacha-inbound-simulator/generate', token, {
    simulationMode: 'IncomingTransactions', clearingHouseCode: 'ACHCOL', scenarioType: 'IncomingCredit',
    originFinancialInstitutionId: external.id, entriesCount: 1, amount: 1000,
    referencePrefix: `BAD-${suffix}`, businessDate: today, cycleCode: 'CICLO-INVENTADO',
    pendingPrenotificationReferences: [], transactionReferences: []
  }, [422]);
  expect(manipulated.status).toBe(422);
  expect((await manipulated.json()).title).toBe('CYCLE_NOT_AVAILABLE');
  await page.waitForTimeout(1_100);
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto(`${uiBaseUrl}/uat/nacha-inbound-simulator`);
  await expect(page.locator('ui-selector-buscable[formcontrolname="cycleCode"]')).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Simulador NACHA-M de entrada', exact: true })).toBeVisible();
  await apiPatch(`/clearing-house-cycle-configs/${config.id}/status`, token, {
    isActive: false,
    effectiveTo: `${today}T23:59:59Z`
  }, [200]);
  sql.close();
  expect(jsErrors).toEqual([]);
  expect(httpErrors).toEqual([]);
});

async function login(page: Page): Promise<string> {
  await page.goto(`${uiBaseUrl}/login`);
  await page.getByLabel('Usuario').fill(username);
  await page.locator('input[formcontrolname="password"]').fill(password);
  await page.getByRole('button', { name: 'Ingresar', exact: true }).click();
  await expect(page).not.toHaveURL(/\/login$/);
  return await page.evaluate(() => sessionStorage.getItem('ach.interbank.access_token')) as string;
}

async function ensureOperationalCycle(token: string, clearingHouseId: number, config: any, processingDate: string): Promise<string> {
  const cycles = await apiGet<any[]>(`/ach-cycles?clearingHouseId=${clearingHouseId}&processingDate=${processingDate}`, token);
  const existing = cycles.find(item => item.cycleName === config.cycleName);
  const body = {
    cycleName: config.cycleName, processingDate: `${processingDate}T00:00:00`,
    startTime: '00:00:00', endTime: '23:59:59', cutoffTime: '23:59:59',
    rescheduleOnHoliday: false, clearingHouseId, clearingHouseCycleConfigId: config.id
  };
  if (existing) {
    await apiPut(`/ach-cycles/${encodeURIComponent(existing.id)}`, token, body, [200]);
    return existing.id;
  } else {
    const response = await apiPost('/ach-cycles', token, body, [200, 201]);
    return (await response.json()).id;
  }
}

function transactionPayload(data: any): any {
  return {
    amount: data.amount, transactionExternalId: data.externalId, reference: data.externalId.slice(-20),
    type: data.type, accountType: 1, isPrenotification: data.isPrenotification,
    destinationInstitutionId: data.destinationInstitutionId, sourceAccountNumber: data.sourceAccount,
    destinationAccountNumber: data.destinationAccount, recipientIdNumber: data.recipientId,
    recipientName: 'RECEPTOR JOB 323', requiresIdentityValidation: false,
    companyName: 'PWJOB323', companyIdentification: 'PW323001',
    companyEntryDescriptionId: data.companyEntryDescriptionId, sourcePersonType: 'PJ', recipientPersonType: 'PJ',
    addendas: [{ addendaType: '05', information: `${data.externalId}-ADD` }]
  };
}

async function activateThirdParty(token: string, source: string, destination: string, recipient: string, institutionId: number): Promise<void> {
  for (let attempt = 0; attempt < 20; attempt++) {
    const query = new URLSearchParams({ sourceAccountNumber: source, destinationAccountNumber: destination, recipientIdNumber: recipient, destinationInstitutionId: String(institutionId), page: '1', pageSize: '20' });
    const result = await apiGet<any>(`/api/customer-third-parties?${query}`, token);
    const item = result.items?.[0];
    if (item) {
      await apiPatch(`/api/customer-third-parties/${item.id}/status`, token, { status: 1, validationMessage: 'Aprobación sintética Playwright JOB 3.2.3' }, [200]);
      return;
    }
    await new Promise(resolve => setTimeout(resolve, 250));
  }
  throw new Error('No se creó el tercero sintético esperado.');
}

function label(page: Page, name: string) {
  return page.locator('label').filter({ has: page.locator('span').filter({ hasText: new RegExp(`^${name}`) }) }).first();
}
async function fillInput(page: Page, name: string, value: string) { await label(page, name).locator('input').first().fill(value); }
async function selectSearchable(page: Page, name: string, value: string) {
  const field = label(page, name);
  await field.locator('.ui-selector input').fill(value);
  await field.locator('button.opcion').filter({ hasText: value }).first().click();
}
async function incompleteCount(page: Page): Promise<number> {
  const text = await page.locator('.form-incomplete-help').innerText();
  return Number(text.match(/\d+/)?.[0] ?? 0);
}

function headers(token: string, json = false): Record<string, string> {
  return { Authorization: `Bearer ${token}`, ...(json ? { 'Content-Type': 'application/json' } : {}) };
}
async function apiGet<T>(path: string, token: string): Promise<T> {
  const response = await fetch(`${apiBaseUrl}${path}`, { headers: headers(token) });
  expect(response.ok, `GET ${path}: ${response.status}`).toBeTruthy();
  return await response.json() as T;
}
async function apiPost(path: string, token: string, body: unknown, statuses: number[]): Promise<Response> {
  const response = await fetch(`${apiBaseUrl}${path}`, { method: 'POST', headers: headers(token, true), body: JSON.stringify(body) });
  expect(statuses, `POST ${path}: ${response.status}`).toContain(response.status);
  return response;
}
async function apiPut(path: string, token: string, body: unknown, statuses: number[]): Promise<Response> {
  const response = await fetch(`${apiBaseUrl}${path}`, { method: 'PUT', headers: headers(token, true), body: JSON.stringify(body) });
  expect(statuses, `PUT ${path}: ${response.status}`).toContain(response.status);
  return response;
}
async function apiPatch(path: string, token: string, body: unknown, statuses: number[]): Promise<Response> {
  const response = await fetch(`${apiBaseUrl}${path}`, { method: 'PATCH', headers: headers(token, true), body: JSON.stringify(body) });
  expect(statuses, `PATCH ${path}: ${response.status}`).toContain(response.status);
  return response;
}
