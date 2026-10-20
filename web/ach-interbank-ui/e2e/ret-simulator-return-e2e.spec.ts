import { expect, Page, test, TestInfo } from '@playwright/test';
import { writeFileSync } from 'node:fs';
import { G36Postgres } from './support/g36-postgres';
import { G36SqlServer, sqlString } from './support/g36-sqlserver';
import { loginThroughUi } from './support/live-ui-auth';

const apiBaseUrl = (process.env['E2E_API_BASE_URL'] ?? process.env['ACH_API_URL'] ?? 'http://localhost:843')
  .replace(/\/+$/, '');

type CreatedTransaction = {
  id: number;
  transactionExternalId: string;
  traceNumber: string;
  achCycleId: string;
  state: string | number;
};

type GenerationResult = {
  id: number;
  simulationId: string;
  fileName: string;
  sha256: string;
  autoImported: boolean;
  uploadRequired: boolean;
  externalTransmission: boolean;
};

type UploadResult = {
  success: boolean;
  ingestionId?: string;
  ingestionStatus?: string;
  parsingStatus?: string;
  selectedProfileCode?: string;
  message?: string;
};

type ReturnEvidence = {
  transactionId: number;
  transactionExternalId: string;
  traceNumber: string;
  transactionState: string;
  cycleId: string;
  simulationCount: number;
  autoImportedCount: number;
  ingestionCount: number;
  headers: number;
  batches: number;
  entries: number;
  addendas: number;
  batchControls: number;
  fileControls: number;
  classifications: number;
  links: number;
  exactLinks: number;
  originalTrace: string;
  returnReason: string;
  returnStateEvents: number;
  returnCodeLinks: number;
  processingEvents: number;
};

test.describe.configure({ mode: 'serial' });

test('RET.SIMULATOR.RETURN.E2E.1: creación UI, devolución simulada, carga manual e idempotencia', async ({ page }, testInfo) => {
  test.setTimeout(300_000);
  const runtime = observeRuntime(page);
  const db = createEvidenceDb();
  const suffix = `${Date.now()}`.slice(-9);
  const externalId = `RET-SIM-${suffix}`;

  try {
    await db.assertReady();
    await loginThroughUi(page);
    const token = await page.evaluate(() => sessionStorage.getItem('ach.interbank.access_token'));
    expect(token, 'El login debe dejar un token para consumir servicios normales de aplicación.').toBeTruthy();

    const clearingHousePage = await apiGet<any>('/clearing-houses?search=ACHCOL&page=1&pageSize=100', token!);
    const clearingHouses = Array.isArray(clearingHousePage) ? clearingHousePage : clearingHousePage.items;
    const achColombia = clearingHouses.find((item) => item.code === 'ACHCOL');
    expect(achColombia, 'ACH Colombia debe existir y resolverse por código desde el catálogo.').toBeTruthy();
    expect(achColombia.isActive, 'ACH Colombia debe estar activa.').toBe(true);

    const institutions = await apiGet<any[]>('/financial-institutions', token!);
    const cfa = institutions.filter((item) => item.isDefaultSource === true);
    const external = institutions.find((item) => item.isDefaultSource !== true && isActive(item.status));
    expect(cfa, 'CFA debe resolverse únicamente mediante IsDefaultSource.').toHaveLength(1);
    expect(external, 'Debe existir una contraparte externa activa.').toBeTruthy();

    const descriptions = await apiGet<any[]>('/transactions/company-entry-descriptions', token!);
    const description = descriptions.find((item) => item.isActive !== false && item.standardEntryClassCode);
    expect(description, 'Debe existir una descripción de entrada activa con SEC configurado.').toBeTruthy();

    const sourceAccount = generatedDigits('41', suffix, 12);
    const destinationAccount = generatedDigits('72', suffix, 12);
    const recipientDocument = generatedDigits('80', suffix, 10);
    const targetOperationalDate = await ensureOperationalCycle(token!, {
      clearingHouseId: achColombia.id,
      timeZoneId: achColombia.timeZoneId,
      externalId,
      destinationInstitutionId: external.id,
      sourceAccount,
      destinationAccount,
      recipientDocument
    });
    const operationalHouses = await apiGet<any[]>('/clearing-houses/operational', token!);
    expect(operationalHouses.some((item) => item.id === achColombia.id),
      'ACH Colombia debe quedar operativa después de preparar los ciclos por servicios normales.').toBe(true);

    const created = await createPrenotificationThroughUi(page, {
      externalId,
      sourceAccount,
      destinationAccount,
      recipientDocument,
      destinationInstitutionName: external.name,
      entryDescription: description.term ?? description.description
    });
    expect(created.transactionExternalId).toBe(externalId);
    expect(created.traceNumber).toMatch(/^\d{15}$/);

    const beforeReturn = await db.transactionByExternalId(externalId);
    expect(beforeReturn, 'La transacción debe existir antes de generar el Return.').not.toBeNull();
    expect(beforeReturn!.traceNumber).toBe(created.traceNumber);
    expect(beforeReturn!.cycleId).toBeTruthy();
    const createdCycle = await apiGet<any>(`/ach-cycles/${encodeURIComponent(beforeReturn!.cycleId)}`, token!);
    expect(created.achCycleId).toBe(beforeReturn!.cycleId);
    expect(calendarDate(createdCycle.processingDate)).toBe(targetOperationalDate);

    await page.goto('/uat/nacha-inbound-simulator', { waitUntil: 'networkidle' });
    await expect(page.getByRole('heading', { name: 'Simular respuesta de otra entidad', exact: true })).toBeVisible();
    await expect(page.getByText('Generación sin transmisión automática')).toBeVisible();
    await expect(page.getByText('CFA se excluye automáticamente del catálogo de contrapartes.')).toBeVisible();

    const operationDate = calendarDate(createdCycle.processingDate);
    await page.locator('input[formcontrolname="businessDate"]').fill(
      formatDatePickerValue(operationDate));
    await page.locator('input[formcontrolname="businessDate"]').press('Tab');
    await selectMaterialOption(page, 'Cámara', 'ACH Colombia');
    await selectMaterialOption(page, 'Tipo de respuesta', 'Devolución de débito');
    await selectMaterialOption(page, 'Entidad que responde', external.name);

    const transactionRow = page.getByRole('row').filter({ hasText: externalId });
    await expect(transactionRow).toBeVisible({ timeout: 30_000 });
    await expect(transactionRow).toContainText(maskLastFour(destinationAccount));
    await transactionRow.getByRole('checkbox').check();

    const returnCodes = await apiGet<any[]>(`/api/regulatory-catalogs/return-codes?clearingHouseCode=${encodeURIComponent(achColombia.code)}`, token!);
    const applicableCause = returnCodes.find((item) =>
      item.isActive && item.appliesToPrenotification && (item.flowType === 'Any' || item.flowType === 'Return'));
    expect(applicableCause, 'Debe existir una causal oficial aplicable a prenotificaciones devueltas.').toBeTruthy();
    await selectMaterialOption(page, 'Causal de devolución', applicableCause.code);

    const cycleName = await db.cycleName(beforeReturn!.cycleId);
    await selectMaterialOption(page, 'Ciclo operativo', cycleName);
    await expect(page.locator('.operational-summary')).toContainText(applicableCause.code);
    await expect(page.locator('.operational-summary')).toContainText(external.name);
    await expect(page.locator('.operational-summary')).toContainText(cycleName);

    await assertSpanishAndResponsive(page, testInfo);
    const generateResponsePromise = page.waitForResponse((response) =>
      response.request().method() === 'POST'
      && new URL(response.url()).pathname === '/api/uat/nacha-inbound-simulator/generate');
    await page.getByRole('button', { name: 'Generar archivo NACHA-M', exact: true }).click();
    await page.getByRole('dialog').getByRole('button', { name: 'Generar archivo', exact: true }).click();
    const generationResponse = await generateResponsePromise;
    const generationBody = await generationResponse.text();
    expect(generationResponse.status(), generationBody).toBe(201);
    const generated = JSON.parse(generationBody) as GenerationResult;
    expect(generated.autoImported).toBe(false);
    expect(generated.uploadRequired).toBe(true);
    expect(generated.externalTransmission).toBe(false);
    expect(generated.fileName).toMatch(/^\d{7}\.\d{3}\.\d{8}\.\d+\.OUT$/);
    const generatedDate = generated.fileName.match(/^\d{7}\.\d{3}\.(\d{8})\./)?.[1];
    expect(generatedDate).toBe(operationDate.replaceAll('-', ''));
    expect(generatedDate).toBe(targetOperationalDate.replaceAll('-', ''));
    await expect(page.getByText('Archivo generado correctamente', { exact: true })).toBeVisible();
    await expect(page.getByText('El archivo no fue importado; usted decide cuándo cargarlo')).toBeVisible();

    expect(await db.ingestionCount(generated.fileName), 'Generar no debe autoimportar.').toBe(0);
    const download = await page.request.get(`${apiBaseUrl}/api/uat/nacha-inbound-simulator/${generated.id}/file`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(download.status()).toBe(200);
    const generatedPath = testInfo.outputPath(generated.fileName);
    writeFileSync(generatedPath, await download.body());
    await testInfo.attach('archivo-nacha-m-generado', { path: generatedPath, contentType: 'text/plain' });

    await page.getByRole('button', { name: 'Ir a carga de archivos NACHA-M', exact: true }).click();
    await expect(page).toHaveURL(/\/transactions\/nacha-upload$/);
    const firstUpload = await uploadThroughUi(page, generatedPath);
    expect(firstUpload.success, JSON.stringify(firstUpload)).toBe(true);
    expect(`${firstUpload.selectedProfileCode ?? ''}`).toBe('OFFICIAL_ACH_ENTRADA_DEVOLUCION_V1_0');
    expect(JSON.stringify(firstUpload)).not.toContain('ProfileNotFound');

    const firstEvidence = await db.returnEvidence(externalId, generated.fileName);
    assertReturnEvidence(firstEvidence, created, applicableCause.code);

    const replay = await uploadThroughUi(page, generatedPath);
    expect(replay.success).toBe(false);
    expect(replay.ingestionStatus).toBe('Duplicado');
    const replayEvidence = await db.returnEvidence(externalId, generated.fileName);
    expect({ ...replayEvidence, processingEvents: firstEvidence.processingEvents }).toEqual(firstEvidence);
    expect(replayEvidence.processingEvents).toBe(firstEvidence.processingEvents + 1);

    expect(runtime.soapRequests).toEqual([]);
    expect(runtime.externalRequests).toEqual([]);
    expect(runtime.pageErrors).toEqual([]);
    expect(runtime.requestFailures).toEqual([]);

    await testInfo.attach('ret-simulator-return-evidence.json', {
      body: JSON.stringify({
        scenario: 'RET.SIMULATOR.RETURN.E2E.1',
        transactionId: created.id,
        transactionExternalId: externalId,
        originalTrace: created.traceNumber,
        cause: applicableCause.code,
        cycleId: created.achCycleId,
        simulationId: generated.simulationId,
        fileName: generated.fileName,
        profile: firstUpload.selectedProfileCode,
        firstEvidence,
        replayStatus: replay.ingestionStatus,
        functionalApplicationsAfterReplay: replayEvidence.returnStateEvents
      }, null, 2),
      contentType: 'application/json'
    });
  } finally {
    await db.close();
  }
});

async function createPrenotificationThroughUi(page: Page, data: {
  externalId: string;
  sourceAccount: string;
  destinationAccount: string;
  recipientDocument: string;
  destinationInstitutionName: string;
  entryDescription: string;
}): Promise<CreatedTransaction> {
  await page.goto('/transactions/create', { waitUntil: 'networkidle' });
  await expect(page.getByRole('heading', { name: 'Crear transacción ACH', exact: true })).toBeVisible();
  await page.locator('input[formcontrolname="transactionExternalId"]').fill(data.externalId);
  await page.getByRole('checkbox', { name: 'Es una prenotificación' }).check();
  await selectAutocomplete(page, 'Descripción de la entrada', data.entryDescription);
  await page.locator('input[formcontrolname="sourceAccountNumber"]').fill(data.sourceAccount);
  await page.locator('input[formcontrolname="companyIdentification"]').fill(`U${data.externalId.slice(-9)}`);
  await page.locator('input[formcontrolname="companyName"]').fill('CFA UAT RETORNO');
  await selectAutocomplete(page, 'Entidad financiera destino', data.destinationInstitutionName);
  await page.locator('input[formcontrolname="destinationAccountNumber"]').fill(data.destinationAccount);
  await page.locator('input[formcontrolname="recipientIdNumber"]').fill(data.recipientDocument);
  await page.locator('input[formcontrolname="recipientName"]').fill('RECEPTOR UAT');
  await page.locator('input[formcontrolname="collectorId"]').fill(generatedDigits('90', data.externalId, 10));
  await page.locator('input[formcontrolname="receiverCustomerCode"]').fill(`CLI-${data.externalId.slice(-9)}`);
  await page.locator('input[formcontrolname="serviceDescription"]').fill('RET UAT');
  await page.locator('input[formcontrolname="information"]').fill(`PRENOTIFICACION ${data.externalId}`);

  const responsePromise = page.waitForResponse((response) =>
    response.request().method() === 'POST' && new URL(response.url()).pathname === '/api/transactions',
    { timeout: 30_000 });
  await page.getByTestId('transaction-submit').click();
  const response = await responsePromise;
  expect(response.status()).toBe(201);
  const created = await response.json() as CreatedTransaction;
  await expect(page).toHaveURL(/\/transactions(?:\/list)?$/);
  return created;
}

async function uploadThroughUi(page: Page, filePath: string): Promise<UploadResult> {
  if (!/\/transactions\/nacha-upload$/.test(new URL(page.url()).pathname)) {
    await page.goto('/transactions/nacha-upload');
  }
  await expect(page.getByText('Cargar archivo NACHA-M', { exact: true })).toBeVisible();
  const clearingHouse = page.locator('app-clearing-house-select select');
  await expect(clearingHouse).toBeEnabled();
  await clearingHouse.selectOption({ label: 'ACH Colombia (ACHCOL)' });
  await page.locator('input[type="file"]').setInputFiles(filePath);
  const responsePromise = page.waitForResponse((response) =>
    response.request().method() === 'POST' && new URL(response.url()).pathname === '/NachaUpload/upload');
  await page.getByRole('button', { name: 'Cargar archivo', exact: true }).click();
  const response = await responsePromise;
  const responseBody = await response.text();
  expect(response.status(), responseBody).toBe(200);
  return JSON.parse(responseBody) as UploadResult;
}

async function ensureOperationalCycle(token: string, data: {
  clearingHouseId: number;
  timeZoneId: string;
  externalId: string;
  destinationInstitutionId: number;
  sourceAccount: string;
  destinationAccount: string;
  recipientDocument: string;
}): Promise<string> {
  const previewQuery = new URLSearchParams({
    amount: '0',
    transactionExternalId: data.externalId,
    reference: data.externalId,
    type: 'Debit',
    accountType: 'Savings',
    isPrenotification: 'true',
    destinationInstitutionId: `${data.destinationInstitutionId}`,
    sourceAccountNumber: data.sourceAccount,
    destinationAccountNumber: data.destinationAccount,
    companyIdentification: `U${data.externalId.slice(-9)}`,
    recipientIdNumber: data.recipientDocument
  });
  const initialPreview = await apiGet<any>(`/transactions/policies/preview?${previewQuery}`, token);
  expect(initialPreview.cycleId, 'El preview oficial debe resolver el ciclo que utilizará la creación.').toBeTruthy();
  expect(initialPreview.processingDate, 'El preview oficial debe resolver la fecha operativa.').toBeTruthy();
  const localOperationalDate = calendarDateInTimeZone(data.timeZoneId);
  const localCycles = await apiGet<any[]>(
    `/ach-cycles?clearingHouseId=${data.clearingHouseId}&processingDate=${encodeURIComponent(localOperationalDate)}`,
    token);
  const allConfigs = await apiGet<any[]>(
    `/clearing-house-cycle-configs?clearingHouseId=${data.clearingHouseId}`,
    token);
  const openLocalCycle = localCycles.find((cycle) => cycle.acceptsTransactions === true
    && `${cycle.operationalStatus}`.toLowerCase() === 'open');
  const localCycle = openLocalCycle
    ?? localCycles.find((cycle) => `${cycle.operationalStatus}`.toLowerCase() !== 'cancelled'
      && !allConfigs.some((config) => config.isActive
        && `${config.cycleName}`.trim() === `${cycle.cycleName}`.trim()
        && `${config.effectiveFrom}`.slice(0, 10) === localOperationalDate))
    ?? localCycles.find((cycle) => `${cycle.operationalStatus}`.toLowerCase() !== 'cancelled');
  const processingDate = localCycle ? localOperationalDate : calendarDate(initialPreview.processingDate);
  if (initialPreview.canSubmit === true
    && initialPreview.isWithinProcessingWindow === true
    && calendarDate(initialPreview.processingDate) === processingDate) {
    return processingDate;
  }

  const configurationEffectiveDate = processingDate;
  const selectedCycle = await apiGet<any>(
    `/ach-cycles/${encodeURIComponent(processingDate === localOperationalDate ? localCycle!.id : initialPreview.cycleId)}`,
    token);
  const cycles = await apiGet<any[]>(
    `/ach-cycles?clearingHouseId=${data.clearingHouseId}&processingDate=${encodeURIComponent(processingDate)}`,
    token);
  const window = processingDate === localOperationalDate
    ? currentSameDayWindow(data.timeZoneId)
    : currentCrossMidnightWindow(data.timeZoneId, cycles.map((cycle) => cycle.endTime));
  const cycleName = `${selectedCycle.cycleName}`.trim();
  expect((cycleName.match(/\d+/g) ?? []).length,
    'El ciclo elegido por la política debe conservar un número canónico único.').toBe(1);
  let config = allConfigs.find((item) => item.isActive
    && `${item.cycleName}`.trim() === cycleName
    && `${item.effectiveFrom}`.slice(0, 10) === configurationEffectiveDate);
  if (!config) {
    const configResponse = await apiPost('/clearing-house-cycle-configs', token, {
      clearingHouseId: data.clearingHouseId,
      cycleName,
      startTime: window.startTime,
      endTime: window.endTime,
      cutoffTime: window.endTime,
      effectiveFrom: `${configurationEffectiveDate}T00:00:00Z`
    }, [200, 201]);
    config = await configResponse.json();
  }

  if (selectedCycle.clearingHouseCycleConfigId !== config.id) {
    const updateResponse = await apiPut(`/ach-cycles/${encodeURIComponent(selectedCycle.id)}`, token, {
      cycleName,
      processingDate: `${processingDate}T00:00:00`,
      startTime: config.startTime,
      endTime: config.endTime,
      cutoffTime: config.cutoffTime,
      rescheduleOnHoliday: selectedCycle.rescheduleOnHoliday,
      clearingHouseId: data.clearingHouseId,
      clearingHouseCycleConfigId: config.id
    }, [200, 400]);
    expect(updateResponse.status, await updateResponse.text()).toBe(200);
  }

  const finalPreview = await apiGet<any>(`/transactions/policies/preview?${previewQuery}`, token);
  expect(finalPreview.canSubmit, finalPreview.message ?? 'El ciclo preparado debe aceptar la transacción UAT.').toBe(true);
  expect(finalPreview.isWithinProcessingWindow).toBe(true);
  expect(calendarDate(finalPreview.processingDate)).toBe(processingDate);
  return processingDate;
}

function calendarDate(value: unknown): string {
  const result = `${value}`.slice(0, 10);
  expect(result).toMatch(/^\d{4}-\d{2}-\d{2}$/);
  return result;
}

function calendarDateInTimeZone(timeZoneId: string): string {
  const parts = new Intl.DateTimeFormat('en-US', {
    timeZone: timeZoneId,
    year: 'numeric',
    month: '2-digit',
    day: '2-digit'
  }).formatToParts(new Date());
  const year = parts.find((part) => part.type === 'year')?.value;
  const month = parts.find((part) => part.type === 'month')?.value;
  const day = parts.find((part) => part.type === 'day')?.value;
  expect(year && month && day, 'La zona horaria de la cÃ¡mara debe producir una fecha vÃ¡lida.').toBeTruthy();
  return `${year}-${month}-${day}`;
}

function formatDatePickerValue(date: string): string {
  const [year, month, day] = date.split('-');
  return `${month}/${day}/${year}`;
}

function currentSameDayWindow(timeZoneId: string): { startTime: string; endTime: string } {
  const nowMinutes = localMinutesInTimeZone(timeZoneId);
  return {
    startTime: formatTime(Math.max(0, nowMinutes - 1)),
    endTime: formatTime(Math.min(1439, nowMinutes + 60))
  };
}

function localMinutesInTimeZone(timeZoneId: string): number {
  const parts = new Intl.DateTimeFormat('en-US', {
    timeZone: timeZoneId,
    hour: '2-digit',
    minute: '2-digit',
    hourCycle: 'h23'
  }).formatToParts(new Date());
  const hour = Number(parts.find((part) => part.type === 'hour')?.value);
  const minute = Number(parts.find((part) => part.type === 'minute')?.value);
  expect(Number.isInteger(hour) && Number.isInteger(minute), 'La zona horaria de la cÃ¡mara debe ser vÃ¡lida.').toBe(true);
  return hour * 60 + minute;
}

function currentCrossMidnightWindow(timeZoneId: string, existingEndTimes: unknown[]): { startTime: string; endTime: string } {
  const parts = new Intl.DateTimeFormat('en-US', {
    timeZone: timeZoneId,
    hour: '2-digit',
    minute: '2-digit',
    hourCycle: 'h23'
  }).formatToParts(new Date());
  const hour = Number(parts.find((part) => part.type === 'hour')?.value);
  const minute = Number(parts.find((part) => part.type === 'minute')?.value);
  expect(Number.isInteger(hour) && Number.isInteger(minute), 'La zona horaria de la cámara debe ser válida.').toBe(true);
  const nowMinutes = hour * 60 + minute;
  const startMinutes = nowMinutes < 2 ? 1439 : nowMinutes - 1;
  const existingEndMinutes = existingEndTimes
    .map((value) => `${value}`.match(/^(\d{1,2}):(\d{2})/))
    .filter((match): match is RegExpMatchArray => Boolean(match))
    .map((match) => Number(match[1]) * 60 + Number(match[2]));
  const earliestExistingEnd = existingEndMinutes.length > 0 ? Math.min(...existingEndMinutes) : startMinutes;
  const endMinutes = Math.max(0, Math.min(startMinutes - 1, earliestExistingEnd - 1));
  return {
    startTime: formatTime(startMinutes),
    endTime: formatTime(endMinutes)
  };
}

function formatTime(totalMinutes: number): string {
  const hour = `${Math.floor(totalMinutes / 60)}`.padStart(2, '0');
  const minute = `${totalMinutes % 60}`.padStart(2, '0');
  return `${hour}:${minute}:00`;
}

async function selectMaterialOption(page: Page, label: string, text: string): Promise<void> {
  const field = page.locator('mat-form-field').filter({ has: page.locator('mat-label', { hasText: label }) }).first();
  await field.locator('mat-select').click();
  const option = page.getByRole('option').filter({ hasText: text }).first();
  await expect(option).toBeVisible();
  await option.click();
}

async function selectAutocomplete(page: Page, label: string, text: string): Promise<void> {
  const testId = label === 'Descripción de la entrada'
    ? 'transaction-company-entry-description'
    : label === 'Entidad financiera destino'
      ? 'transaction-destination-institution'
      : null;
  const input = testId ? page.getByTestId(testId) : page.getByRole('textbox', { name: label, exact: true });
  await expect(input).toBeVisible({ timeout: 15_000 });
  await input.fill(text, { timeout: 15_000 });
  const option = page.getByRole('option').filter({ hasText: text }).first();
  await expect(option).toBeVisible({ timeout: 15_000 });
  await input.press('ArrowDown');
  await input.press('Enter');
}

async function assertSpanishAndResponsive(page: Page, testInfo: TestInfo): Promise<void> {
  const body = page.locator('body');
  for (const forbidden of ['Transaction ID', 'Response Type', 'Submit', 'Batch', 'No data']) {
    await expect(body).not.toContainText(forbidden);
  }
  const desktopScreenshot = testInfo.outputPath('simulador-retorno-escritorio.png');
  await page.screenshot({ path: desktopScreenshot, fullPage: true });
  await testInfo.attach('simulador-retorno-escritorio', { path: desktopScreenshot, contentType: 'image/png' });
  await page.setViewportSize({ width: 390, height: 844 });
  await expect(page.getByRole('heading', { name: 'Simular respuesta de otra entidad' })).toBeVisible();
  const overflow = await page.evaluate(() => document.documentElement.scrollWidth - document.documentElement.clientWidth);
  expect(overflow, 'La vista móvil no debe producir desbordamiento horizontal global.').toBeLessThanOrEqual(1);
  const mobileScreenshot = testInfo.outputPath('simulador-retorno-movil.png');
  await page.screenshot({ path: mobileScreenshot, fullPage: true });
  await testInfo.attach('simulador-retorno-movil', { path: mobileScreenshot, contentType: 'image/png' });
  await page.setViewportSize({ width: 1440, height: 900 });
}

function assertReturnEvidence(evidence: ReturnEvidence, created: CreatedTransaction, cause: string): void {
  expect(evidence.transactionId).toBe(created.id);
  expect(evidence.originalTrace).toBe(created.traceNumber);
  expect(evidence.returnReason).toBe(cause);
  expect(evidence.transactionState).toMatch(/ReturnedByOperator|ReturnedByEpr|Return/i);
  expect(evidence.simulationCount).toBe(1);
  expect(evidence.autoImportedCount).toBe(0);
  expect(evidence.ingestionCount).toBe(1);
  expect(evidence.headers).toBe(1);
  expect(evidence.batches).toBe(1);
  expect(evidence.entries).toBe(1);
  expect(evidence.addendas).toBe(1);
  expect(evidence.batchControls).toBe(1);
  expect(evidence.fileControls).toBe(1);
  expect(evidence.classifications).toBe(1);
  expect(evidence.links).toBe(1);
  expect(evidence.exactLinks).toBe(1);
  expect(evidence.returnStateEvents).toBe(1);
  expect(evidence.returnCodeLinks).toBe(1);
  expect(evidence.processingEvents).toBeGreaterThanOrEqual(1);
}

function observeRuntime(page: Page) {
  const soapRequests: string[] = [];
  const externalRequests: string[] = [];
  const pageErrors: string[] = [];
  const requestFailures: string[] = [];
  page.on('pageerror', (error) => pageErrors.push(error.message));
  page.on('requestfailed', (request) => {
    const errorText = request.failure()?.errorText ?? 'error desconocido';
    if (!errorText.includes('ERR_ABORTED')) {
      requestFailures.push(`${request.method()} ${request.url()} (${errorText})`);
    }
  });
  page.on('request', (request) => {
    const url = new URL(request.url());
    if (/WSCFAACH|Proc_(?:Transacciones|Contrapartidas)|RegistrarRespuestaTransaccion/i.test(request.url())) {
      soapRequests.push(`${request.method()} ${url.pathname}`);
    }
    if (!['localhost', '127.0.0.1', 'host.docker.internal'].includes(url.hostname)
        && !/fonts\.(?:googleapis|gstatic)\.com/i.test(url.hostname)) {
      externalRequests.push(`${request.method()} ${url.hostname}${url.pathname}`);
    }
  });
  return { soapRequests, externalRequests, pageErrors, requestFailures };
}

function isActive(value: unknown): boolean {
  return value === 1 || `${value}`.toLowerCase() === 'active';
}

function generatedDigits(prefix: string, source: string, length: number): string {
  const digits = `${prefix}${source}`.replace(/\D/g, '');
  return digits.padEnd(length, '7').slice(0, length);
}

function maskLastFour(value: string): string {
  return `****${value.slice(-4)}`;
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
  const response = await fetch(`${apiBaseUrl}${path}`, {
    method: 'POST',
    headers: headers(token, true),
    body: JSON.stringify(body)
  });
  expect(statuses, `POST ${path}: ${response.status}`).toContain(response.status);
  return response;
}

async function apiPut(path: string, token: string, body: unknown, statuses: number[]): Promise<Response> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    method: 'PUT',
    headers: headers(token, true),
    body: JSON.stringify(body)
  });
  expect(statuses, `PUT ${path}: ${response.status}`).toContain(response.status);
  return response;
}

async function apiPatch(path: string, token: string, body: unknown, statuses: number[]): Promise<Response> {
  const response = await fetch(`${apiBaseUrl}${path}`, {
    method: 'PATCH',
    headers: headers(token, true),
    body: JSON.stringify(body)
  });
  expect(statuses, `PATCH ${path}: ${response.status}`).toContain(response.status);
  return response;
}

function createEvidenceDb(): EvidenceDb {
  const provider = `${process.env['ACH_E2E_PROVIDER'] ?? 'SqlServer'}`.toLowerCase();
  return provider.includes('post') ? new PostgresEvidenceDb() : new SqlServerEvidenceDb();
}

interface EvidenceDb {
  assertReady(): Promise<void>;
  close(): Promise<void>;
  transactionByExternalId(externalId: string): Promise<ReturnEvidence | null>;
  cycleName(cycleId: string): Promise<string>;
  ingestionCount(fileName: string): Promise<number>;
  returnEvidence(externalId: string, fileName: string): Promise<ReturnEvidence>;
}

class SqlServerEvidenceDb implements EvidenceDb {
  private readonly db = new G36SqlServer();
  async assertReady(): Promise<void> { this.db.assertReady(); }
  async close(): Promise<void> { this.db.close(); }
  async transactionByExternalId(externalId: string): Promise<ReturnEvidence | null> {
    return this.db.query<ReturnEvidence>(sqlServerTransactionQuery(externalId))[0] ?? null;
  }
  async cycleName(cycleId: string): Promise<string> {
    return this.db.scalar<string>(`SELECT [CycleName] AS [value] FROM [AchCycles] WHERE [Id] = ${sqlString(cycleId)}`)!;
  }
  async ingestionCount(fileName: string): Promise<number> {
    return this.db.scalar<number>(`SELECT COUNT(*) AS [value] FROM [IncomingNachaFileIngestions] WHERE [FileName] = ${sqlString(fileName)}`) ?? 0;
  }
  async returnEvidence(externalId: string, fileName: string): Promise<ReturnEvidence> {
    return this.db.query<ReturnEvidence>(sqlServerReturnEvidenceQuery(externalId, fileName))[0];
  }
}

class PostgresEvidenceDb implements EvidenceDb {
  private readonly db = new G36Postgres({
    host: process.env['ACH_E2E_POSTGRES_HOST'] ?? process.env['E2E_DB_HOST'],
    port: Number(process.env['ACH_E2E_POSTGRES_PORT'] ?? process.env['E2E_DB_PORT']),
    database: process.env['ACH_E2E_POSTGRES_DATABASE'] ?? process.env['E2E_DB_NAME'],
    user: process.env['ACH_E2E_POSTGRES_USER'] ?? process.env['E2E_DB_USER'],
    password: process.env['ACH_E2E_POSTGRES_PASSWORD'] ?? process.env['E2E_DB_PASSWORD']
  });
  async assertReady(): Promise<void> { await this.db.query('SELECT 1'); }
  async close(): Promise<void> { await this.db.close(); }
  async transactionByExternalId(externalId: string): Promise<ReturnEvidence | null> {
    return (await this.db.query<ReturnEvidence>(postgresTransactionQuery(), [externalId]))[0] ?? null;
  }
  async cycleName(cycleId: string): Promise<string> {
    return (await this.db.query<{ value: string }>('SELECT "CycleName" AS value FROM "AchCycles" WHERE "Id" = $1', [cycleId]))[0].value;
  }
  async ingestionCount(fileName: string): Promise<number> {
    return Number((await this.db.query<{ value: string }>('SELECT COUNT(*) AS value FROM "IncomingNachaFileIngestions" WHERE "FileName" = $1', [fileName]))[0].value);
  }
  async returnEvidence(externalId: string, fileName: string): Promise<ReturnEvidence> {
    return (await this.db.query<ReturnEvidence>(postgresReturnEvidenceQuery(), [externalId, fileName]))[0];
  }
}

function sqlServerTransactionQuery(externalId: string): string {
  return `SELECT t.[Id] AS [transactionId], t.[TransactionExternalId] AS [transactionExternalId],
    t.[TraceNumber] AS [traceNumber], t.[State] AS [transactionState], t.[AchCycleId] AS [cycleId],
    0 AS [simulationCount], 0 AS [autoImportedCount], 0 AS [ingestionCount], 0 AS [headers], 0 AS [batches],
    0 AS [entries], 0 AS [addendas], 0 AS [batchControls], 0 AS [fileControls], 0 AS [classifications],
    0 AS [links], 0 AS [exactLinks], N'' AS [originalTrace], N'' AS [returnReason],
    0 AS [returnStateEvents], 0 AS [returnCodeLinks], 0 AS [processingEvents]
    FROM [AchTransactions] t WHERE t.[TransactionExternalId] = ${sqlString(externalId)}`;
}

function postgresTransactionQuery(): string {
  return `SELECT t."Id" AS "transactionId", t."TransactionExternalId" AS "transactionExternalId",
    t."TraceNumber" AS "traceNumber", t."State"::text AS "transactionState", t."AchCycleId" AS "cycleId",
    0 AS "simulationCount", 0 AS "autoImportedCount", 0 AS "ingestionCount", 0 AS headers, 0 AS batches,
    0 AS entries, 0 AS addendas, 0 AS "batchControls", 0 AS "fileControls", 0 AS classifications,
    0 AS links, 0 AS "exactLinks", '' AS "originalTrace", '' AS "returnReason",
    0 AS "returnStateEvents", 0 AS "returnCodeLinks", 0 AS "processingEvents"
    FROM "AchTransactions" t WHERE t."TransactionExternalId" = $1`;
}

function sqlServerReturnEvidenceQuery(externalId: string, fileName: string): string {
  const tx = sqlString(externalId);
  const file = sqlString(fileName);
  return `SELECT t.[Id] AS [transactionId], t.[TransactionExternalId] AS [transactionExternalId],
    t.[TraceNumber] AS [traceNumber], t.[State] AS [transactionState], t.[AchCycleId] AS [cycleId],
    (SELECT COUNT(*) FROM [NachaInboundSimulations] s WHERE s.[FileName] = ${file}) AS [simulationCount],
    (SELECT COUNT(*) FROM [NachaInboundSimulations] s WHERE s.[FileName] = ${file} AND s.[AutoImported] = 1) AS [autoImportedCount],
    (SELECT COUNT(*) FROM [IncomingNachaFileIngestions] i WHERE i.[FileName] = ${file}) AS [ingestionCount],
    (SELECT COUNT(*) FROM [NachaHeaders] h JOIN [IncomingNachaFileIngestions] i ON i.[Id] = h.[IncomingNachaFileIngestionId] WHERE i.[FileName] = ${file}) AS [headers],
    (SELECT COUNT(*) FROM [BatchHeaders] b JOIN [NachaHeaders] h ON h.[NachaID] = b.[NachaID] JOIN [IncomingNachaFileIngestions] i ON i.[Id] = h.[IncomingNachaFileIngestionId] WHERE i.[FileName] = ${file}) AS [batches],
    (SELECT COUNT(*) FROM [EntryDetails] e JOIN [NachaHeaders] h ON h.[NachaID] = e.[NachaID] JOIN [IncomingNachaFileIngestions] i ON i.[Id] = h.[IncomingNachaFileIngestionId] WHERE i.[FileName] = ${file}) AS [entries],
    (SELECT COUNT(*) FROM [AddendaRecords] a JOIN [NachaHeaders] h ON h.[NachaID] = a.[NachaID] JOIN [IncomingNachaFileIngestions] i ON i.[Id] = h.[IncomingNachaFileIngestionId] WHERE i.[FileName] = ${file}) AS [addendas],
    (SELECT COUNT(*) FROM [BatchControls] b JOIN [NachaHeaders] h ON h.[NachaID] = b.[NachaID] JOIN [IncomingNachaFileIngestions] i ON i.[Id] = h.[IncomingNachaFileIngestionId] WHERE i.[FileName] = ${file}) AS [batchControls],
    (SELECT COUNT(*) FROM [FileControls] f JOIN [NachaHeaders] h ON h.[NachaID] = f.[NachaID] JOIN [IncomingNachaFileIngestions] i ON i.[Id] = h.[IncomingNachaFileIngestionId] WHERE i.[FileName] = ${file}) AS [fileControls],
    (SELECT COUNT(*) FROM [IncomingNachaEntryClassifications] c JOIN [IncomingNachaFileIngestions] i ON i.[Id] = c.[IncomingNachaFileIngestionId] WHERE i.[FileName] = ${file}) AS [classifications],
    (SELECT COUNT(*) FROM [IncomingNachaTransactionLinks] l JOIN [IncomingNachaFileIngestions] i ON i.[Id] = l.[IncomingNachaFileIngestionId] WHERE i.[FileName] = ${file}) AS [links],
    (SELECT COUNT(*) FROM [IncomingNachaTransactionLinks] l JOIN [IncomingNachaFileIngestions] i ON i.[Id] = l.[IncomingNachaFileIngestionId] WHERE i.[FileName] = ${file} AND l.[AchTransactionId] = t.[Id] AND l.[IsFinal] = 1 AND l.[LinkType] = N'ExactOriginalTraceRef') AS [exactLinks],
    (SELECT TOP (1) a.[OriginalTraceNumber] FROM [AddendaRecords] a JOIN [NachaHeaders] h ON h.[NachaID] = a.[NachaID] JOIN [IncomingNachaFileIngestions] i ON i.[Id] = h.[IncomingNachaFileIngestionId] WHERE i.[FileName] = ${file}) AS [originalTrace],
    (SELECT TOP (1) a.[ReturnReasonCode] FROM [AddendaRecords] a JOIN [NachaHeaders] h ON h.[NachaID] = a.[NachaID] JOIN [IncomingNachaFileIngestions] i ON i.[Id] = h.[IncomingNachaFileIngestionId] WHERE i.[FileName] = ${file}) AS [returnReason],
    (SELECT COUNT(*) FROM [AchTransactionStateEvents] e WHERE e.[AchTransactionId] = t.[Id] AND e.[AchReturnCodeId] IS NOT NULL AND e.[ToState] IN (N'ReturnedByOperator', N'ReturnedByEpr')) AS [returnStateEvents],
    (SELECT COUNT(*) FROM [AchTransactionStateEvents] e WHERE e.[AchTransactionId] = t.[Id] AND e.[AchReturnCodeId] IS NOT NULL) AS [returnCodeLinks],
    (SELECT COUNT(*) FROM [IncomingNachaProcessingEvents] p JOIN [IncomingNachaFileIngestions] i ON i.[Id] = p.[IncomingNachaFileIngestionId] WHERE i.[FileName] = ${file}) AS [processingEvents]
    FROM [AchTransactions] t WHERE t.[TransactionExternalId] = ${tx}`;
}

function postgresReturnEvidenceQuery(): string {
  return `SELECT t."Id" AS "transactionId", t."TransactionExternalId" AS "transactionExternalId",
    t."TraceNumber" AS "traceNumber", t."State"::text AS "transactionState", t."AchCycleId" AS "cycleId",
    (SELECT COUNT(*)::int FROM "NachaInboundSimulations" s WHERE s."FileName" = $2) AS "simulationCount",
    (SELECT COUNT(*)::int FROM "NachaInboundSimulations" s WHERE s."FileName" = $2 AND s."AutoImported" = TRUE) AS "autoImportedCount",
    (SELECT COUNT(*)::int FROM "IncomingNachaFileIngestions" i WHERE i."FileName" = $2) AS "ingestionCount",
    (SELECT COUNT(*)::int FROM "NachaHeaders" h JOIN "IncomingNachaFileIngestions" i ON i."Id" = h."IncomingNachaFileIngestionId" WHERE i."FileName" = $2) AS headers,
    (SELECT COUNT(*)::int FROM "BatchHeaders" b JOIN "NachaHeaders" h ON h."NachaID" = b."NachaID" JOIN "IncomingNachaFileIngestions" i ON i."Id" = h."IncomingNachaFileIngestionId" WHERE i."FileName" = $2) AS batches,
    (SELECT COUNT(*)::int FROM "EntryDetails" e JOIN "NachaHeaders" h ON h."NachaID" = e."NachaID" JOIN "IncomingNachaFileIngestions" i ON i."Id" = h."IncomingNachaFileIngestionId" WHERE i."FileName" = $2) AS entries,
    (SELECT COUNT(*)::int FROM "AddendaRecords" a JOIN "NachaHeaders" h ON h."NachaID" = a."NachaID" JOIN "IncomingNachaFileIngestions" i ON i."Id" = h."IncomingNachaFileIngestionId" WHERE i."FileName" = $2) AS addendas,
    (SELECT COUNT(*)::int FROM "BatchControls" b JOIN "NachaHeaders" h ON h."NachaID" = b."NachaID" JOIN "IncomingNachaFileIngestions" i ON i."Id" = h."IncomingNachaFileIngestionId" WHERE i."FileName" = $2) AS "batchControls",
    (SELECT COUNT(*)::int FROM "FileControls" f JOIN "NachaHeaders" h ON h."NachaID" = f."NachaID" JOIN "IncomingNachaFileIngestions" i ON i."Id" = h."IncomingNachaFileIngestionId" WHERE i."FileName" = $2) AS "fileControls",
    (SELECT COUNT(*)::int FROM "IncomingNachaEntryClassifications" c JOIN "IncomingNachaFileIngestions" i ON i."Id" = c."IncomingNachaFileIngestionId" WHERE i."FileName" = $2) AS classifications,
    (SELECT COUNT(*)::int FROM "IncomingNachaTransactionLinks" l JOIN "IncomingNachaFileIngestions" i ON i."Id" = l."IncomingNachaFileIngestionId" WHERE i."FileName" = $2) AS links,
    (SELECT COUNT(*)::int FROM "IncomingNachaTransactionLinks" l JOIN "IncomingNachaFileIngestions" i ON i."Id" = l."IncomingNachaFileIngestionId" WHERE i."FileName" = $2 AND l."AchTransactionId" = t."Id" AND l."IsFinal" = TRUE AND l."LinkType" = 'ExactOriginalTraceRef') AS "exactLinks",
    (SELECT a."OriginalTraceNumber" FROM "AddendaRecords" a JOIN "NachaHeaders" h ON h."NachaID" = a."NachaID" JOIN "IncomingNachaFileIngestions" i ON i."Id" = h."IncomingNachaFileIngestionId" WHERE i."FileName" = $2 LIMIT 1) AS "originalTrace",
    (SELECT a."ReturnReasonCode" FROM "AddendaRecords" a JOIN "NachaHeaders" h ON h."NachaID" = a."NachaID" JOIN "IncomingNachaFileIngestions" i ON i."Id" = h."IncomingNachaFileIngestionId" WHERE i."FileName" = $2 LIMIT 1) AS "returnReason",
    (SELECT COUNT(*)::int FROM "AchTransactionStateEvents" e WHERE e."AchTransactionId" = t."Id" AND e."AchReturnCodeId" IS NOT NULL AND e."ToState" IN ('ReturnedByOperator', 'ReturnedByEpr')) AS "returnStateEvents",
    (SELECT COUNT(*)::int FROM "AchTransactionStateEvents" e WHERE e."AchTransactionId" = t."Id" AND e."AchReturnCodeId" IS NOT NULL) AS "returnCodeLinks",
    (SELECT COUNT(*)::int FROM "IncomingNachaProcessingEvents" p JOIN "IncomingNachaFileIngestions" i ON i."Id" = p."IncomingNachaFileIngestionId" WHERE i."FileName" = $2) AS "processingEvents"
    FROM "AchTransactions" t WHERE t."TransactionExternalId" = $1`;
}
