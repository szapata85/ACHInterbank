import { expect, Page, test } from '@playwright/test';

const ui = process.env['ACH_UI_URL'] ?? 'http://localhost:4200';
const api = process.env['ACH_API_URL'] ?? 'http://localhost:843';
const username = process.env['ACH_USER'] ?? 'admin';
const password = process.env['ACH_PASS'];

test.describe.serial('JOB 4 - dominio productivo de respuestas ACH', () => {
  test.beforeAll(() => {
    if (!password) {
      throw new Error('ACH_PASS es obligatorio para el flujo runtime real de JOB 4.');
    }
  });
  test.setTimeout(180_000);

  test('administra mappings y resuelve el ciclo operacional con API y base reales', async ({ page }) => {
    const jsErrors: string[] = [];
    const httpErrors: string[] = [];
    page.on('pageerror', error => jsErrors.push(error.message));
    page.on('response', response => {
      if (response.status() >= 500 || (response.status() === 404 && !response.url().includes('favicon'))) {
        httpErrors.push(`${response.status()} ${response.url()}`);
      }
    });

    const token = await login(page);
    const headers = auth(token);
    await allowReprocessDispatcherOnNonBusinessDay(page, headers);
    const housesResponse = await page.request.get(`${api}/clearing-houses?search=ACHCOL`, { headers });
    expect(housesResponse.ok()).toBeTruthy();
    const housesPayload = await housesResponse.json();
    const houses = Array.isArray(housesPayload) ? housesPayload : housesPayload.items;
    const house = houses.find((x: { code: string }) => x.code === 'ACHCOL') as { id: number; code: string };
    expect(house?.id).toBeGreaterThan(0);

    const suffix = Date.now().toString().slice(-7);
    const mappedCode = `J4M${suffix}`;
    const orphanCode = `J4O${suffix}`;
    await page.goto(`${ui}/ach-responses/status-mappings`);
    await expect(page.getByRole('heading', { name: 'Mappings de respuestas ACH' })).toBeVisible();
    await page.getByRole('button', { name: 'Nuevo mapping' }).click();
    await page.locator('input[formControlName="clearingHouseId"]').fill(String(house.id));
    await page.locator('input[formControlName="externalCode"]').fill(mappedCode);
    await page.locator('input[formControlName="internalStatusId"]').fill('1');
    await page.locator('input[formControlName="externalServiceStatusId"]').fill('1');
    await page.locator('input[formControlName="internalStatusName"]').fill('JOB4 E2E');
    await page.locator('input[formControlName="priority"]').fill('900');
    await page.locator('input[formControlName="effectiveFrom"]').fill(new Date(Date.now() - 86_400_000).toISOString().slice(0, 10));
    await page.locator('input[formControlName="effectiveTo"]').fill(new Date(Date.now() + 86_400_000).toISOString().slice(0, 10));
    await page.locator('textarea[formControlName="reason"]').fill('Creación sintética controlada JOB 4');
    await page.getByRole('button', { name: 'Guardar', exact: true }).click();
    await expect(page.getByText('Mapping guardado correctamente.')).toBeVisible();

    const mappingsResponse = await page.request.get(
      `${api}/api/ach/response-status-mappings?codigoCamaraCompensacion=${house.code}&tipoRespuesta=Transaccion&activo=true`,
      { headers });
    const mapping = (await mappingsResponse.json()).find((x: { codigoEstadoExterno: string }) => x.codigoEstadoExterno === mappedCode);
    expect(mapping).toBeTruthy();

    const mappingDetailResponse = await page.request.get(`${api}/api/ach/response-status-mappings/${mapping.id}`, { headers });
    const mappingDetail = await mappingDetailResponse.json();
    const mappingUpdate = mappingPayload(house.id, mappedCode, mappingDetail.version, 901, 'Edición controlada JOB 4');
    const edited = await page.request.put(`${api}/api/ach/response-status-mappings/${mapping.id}`, { headers, data: mappingUpdate });
    expect(edited.ok(), await edited.text()).toBeTruthy();
    const conflict = await page.request.put(`${api}/api/ach/response-status-mappings/${mapping.id}`, { headers, data: mappingUpdate });
    expect(conflict.status()).toBe(409);
    expect((await conflict.json()).currentVersion).toBeTruthy();

    const receivedAt = new Date().toISOString();
    const mappedRequest = responsePayload(house.code, mappedCode, `JOB4-DUP-${suffix}`, receivedAt);
    const concurrentReceipts = await Promise.all([
      page.request.post(`${api}/api/ach/responses/process`, { headers, data: mappedRequest }),
      page.request.post(`${api}/api/ach/responses/process`, { headers, data: mappedRequest })
    ]);
    expect(concurrentReceipts.every(x => x.ok())).toBeTruthy();
    const receiptBodies = await Promise.all(concurrentReceipts.map(x => x.json()));
    expect(receiptBodies.filter(x => x.duplicada === false)).toHaveLength(1);
    expect(receiptBodies.filter(x => x.duplicada === true)).toHaveLength(1);

    const orphanResponse = await page.request.post(`${api}/api/ach/responses/process`, {
      headers, data: responsePayload(house.code, orphanCode, `JOB4-REPROCESS-${suffix}`, new Date().toISOString())
    });
    expect(orphanResponse.ok(), await orphanResponse.text()).toBeTruthy();
    const orphanResponseId = (await orphanResponse.json()).achResponseId as string;

    const responseDetail = await (await page.request.get(`${api}/api/ach/responses/${orphanResponseId}`, { headers })).json();
    const duplicateCountBefore = responseDetail.duplicateReceiptCount as number;
    const reprocess = await page.request.post(`${api}/api/ach/responses/${orphanResponseId}/reprocess`, {
      headers,
      data: { commandId: crypto.randomUUID(), expectedVersion: responseDetail.version, reason: 'Reproceso gobernado E2E' }
    });
    expect(reprocess.status()).toBe(202);
    const requestedAttempt = await reprocess.json();
    expect(requestedAttempt.status).toBe('Pending');

    const execute = await page.request.post(
      `${api}/api/scheduler/tasks/ach-response-reprocess-dispatcher/execute`,
      {
        headers,
        data: {
          reason: 'Certificación runtime JOB 4 desde el mecanismo manual existente',
          requestId: crypto.randomUUID()
        }
      });
    expect(execute.status(), await execute.text()).toBe(202);

    const observedStatuses = new Set<string>(['Pending']);
    let terminalAttempt: any;
    for (let poll = 0; poll < 120; poll++) {
      const attemptResponse = await page.request.get(
        `${api}/api/ach/responses/${orphanResponseId}/reprocess-attempts/${requestedAttempt.id}`,
        { headers });
      expect(attemptResponse.ok(), await attemptResponse.text()).toBeTruthy();
      const attempt = await attemptResponse.json();
      observedStatuses.add(attempt.status);
      if (['Completed', 'FailedFunctional', 'FailedTechnical'].includes(attempt.status)) {
        terminalAttempt = attempt;
        break;
      }
      await page.waitForTimeout(250);
    }
    expect(terminalAttempt, `Estados observados: ${[...observedStatuses].join(', ')}`).toBeTruthy();
    expect(terminalAttempt.status).toBe('FailedFunctional');
    expect(terminalAttempt.resultCode).toBe('MappingNotFound');

    const attempts = await (await page.request.get(
      `${api}/api/ach/responses/${orphanResponseId}/reprocess-attempts`,
      { headers })).json();
    expect(attempts).toHaveLength(1);
    expect(attempts[0].id).toBe(requestedAttempt.id);

    const terminalResponse = await (await page.request.get(
      `${api}/api/ach/responses/${orphanResponseId}`,
      { headers })).json();
    expect(terminalResponse.estadoProcesamiento).toBe('RequiereRevisionManual');
    expect(terminalResponse.duplicateReceiptCount).toBe(duplicateCountBefore);

    const schedulerHistory = await (await page.request.get(
      `${api}/api/scheduler/tasks/ach-response-reprocess-dispatcher/history?page=1&pageSize=20`,
      { headers })).json();
    const schedulerExecution = schedulerHistory.items.find((x: { taskCode: string; requestReason?: string; status: string }) =>
      x.taskCode === 'ach-response-reprocess-dispatcher'
      && x.requestReason === 'Certificación runtime JOB 4 desde el mecanismo manual existente');
    expect(schedulerExecution).toBeTruthy();
    expect(schedulerExecution.status).not.toBe('Skipped');

    const audit = await (await page.request.get(`${api}/api/ach/responses/${orphanResponseId}/audit`, { headers })).json();
    expect(audit.some((x: { action: string }) => x.action === 'ReprocessRequested')).toBeTruthy();
    expect(audit.filter((x: { action: string }) => x.action === 'ReprocessClaimed')).toHaveLength(1);
    expect(audit.filter((x: { action: string }) => x.action === 'ReprocessFailedFunctional')).toHaveLength(1);

    await page.goto(`${ui}/ach-responses/manual-review`);
    await expect(page.getByRole('heading', { name: 'Revisión manual de respuestas ACH' })).toBeVisible();
    await page.goto(`${ui}/ach-responses/${orphanResponseId}`);
    await expect(page.getByRole('heading', { name: 'Historial de reprocesos' })).toBeVisible();
    await expect(page.getByText('Requiere revisión', { exact: true })).toBeVisible();
    await page.goto(`${ui}/ach/reconciliation`);
    await expect(page.getByRole('heading', { name: 'Consola de conciliación ACH', exact: true })).toBeVisible();
    expect(await page.locator('body').innerText()).not.toContain('[object Object]');
    expect(jsErrors).toEqual([]);
    expect(httpErrors).toEqual([]);
  });

  test('mantiene el workspace utilizable en viewport móvil', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await login(page);
    await page.goto(`${ui}/ach-responses`);
    await expect(page.getByRole('heading', { name: 'Respuestas ACH', exact: true })).toBeVisible();
    expect(await page.locator('body').innerText()).not.toContain('[object Object]');
  });
});

async function login(page: Page): Promise<string> {
  await page.goto(`${ui}/login`);
  await page.locator('input[formControlName="username"]').fill(username);
  await page.locator('input[formControlName="password"]').fill(password!);
  await page.getByRole('button', { name: 'Ingresar' }).click();
  await expect(page).not.toHaveURL(/\/login$/);
  const token = await page.evaluate(() =>
    window.sessionStorage.getItem('ach.interbank.access_token'));
  expect(token, 'El login real debe persistir el token de la sesión UI.').toBeTruthy();
  return token;
}

function auth(token: string): Record<string, string> {
  return { Authorization: `Bearer ${token}`, 'X-Correlation-ID': crypto.randomUUID() };
}

async function allowReprocessDispatcherOnNonBusinessDay(page: Page, headers: Record<string, string>): Promise<void> {
  const taskResponse = await page.request.get(`${api}/api/scheduler/tasks/ach-response-reprocess-dispatcher`, { headers });
  expect(taskResponse.ok(), await taskResponse.text()).toBeTruthy();
  const task = await taskResponse.json();
  if (!task.onlyBusinessDays) return;

  const update = await page.request.put(`${api}/api/scheduler/tasks/ach-response-reprocess-dispatcher/schedule`, {
    headers,
    data: {
      periodicityType: task.periodicityType,
      n: task.n,
      minute: task.minute,
      timeOfDay: task.timeOfDay,
      weeklyDay: task.weeklyDay,
      monthDay: task.monthDay,
      cronExpression: task.cronExpression,
      timeZoneId: task.timeZoneId,
      misfirePolicy: task.misfirePolicy,
      onlyBusinessDays: false,
      startAt: task.startAt,
      endAt: task.endAt
    }
  });
  expect(update.ok(), await update.text()).toBeTruthy();
  expect((await update.json()).onlyBusinessDays).toBe(false);
}

function mappingPayload(clearingHouseId: number, externalCode: string, expectedVersion: string, priority: number, reason: string) {
  return {
    clearingHouseId, responseType: 'Transaccion', externalCode, externalCause: null,
    internalStatusId: 1, externalServiceStatusId: 1, internalStatusName: 'JOB4 E2E',
    normalizedCause: null, normalizedDescription: 'Mapping sintético JOB 4', requiresCause: false,
    allowsNotification: false, priority,
    effectiveFrom: new Date(Date.now() - 86_400_000).toISOString(),
    effectiveTo: new Date(Date.now() + 86_400_000).toISOString(), isActive: true, expectedVersion, reason
  };
}

function responsePayload(clearingHouseCode: string, externalCode: string, transactionId: string, receivedAt: string) {
  return {
    tipoRespuesta: 'Transaccion', idTransaccion: transactionId, codigoCamaraCompensacion: clearingHouseCode,
    codigoEntidadOrigen: 'SYNTHETIC', codigoEntidadDestino: 'SYNTHETIC', codigoEstadoExterno: externalCode,
    codigoCausalExterna: null, descripcionCausalExterna: null, idCanal: 1, nombreCanal: 'E2E',
    idTransaccionServicioExterno: 1, fechaRecepcion: receivedAt, correlationId: crypto.randomUUID()
  };
}
