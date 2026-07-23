import { expect, Page, test } from '@playwright/test';

const ui = process.env['ACH_UI_URL'] ?? 'http://localhost:4200';
const api = process.env['ACH_API_URL'] ?? 'http://localhost:843';
const username = process.env['ACH_USER'] ?? 'admin';
const password = process.env['ACH_PASS'];
const transactionReference = process.env['ACH_E2E_TRANSACTION_REFERENCE'];

test.describe.serial('JOB 4 - dominio productivo de respuestas ACH', () => {
  test.skip(!password || !transactionReference, 'ACH_PASS y ACH_E2E_TRANSACTION_REFERENCE son obligatorios para el flujo real.');
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
    const housesResponse = await page.request.get(`${api}/clearing-houses?search=ACHCOL`, { headers });
    expect(housesResponse.ok()).toBeTruthy();
    const house = (await housesResponse.json()).items[0] as { id: number; code: string };
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
      headers, data: responsePayload(house.code, orphanCode, transactionReference!, new Date().toISOString())
    });
    expect(orphanResponse.ok(), await orphanResponse.text()).toBeTruthy();
    const orphanResponseId = (await orphanResponse.json()).achResponseId as string;
    const createdOrphan = await page.request.post(`${api}/api/ach/responses/${orphanResponseId}/orphan`, {
      headers, data: { reason: 'Sin correlación inequívoca durante recepción E2E', candidateReferences: transactionReference }
    });
    expect(createdOrphan.ok(), await createdOrphan.text()).toBeTruthy();
    let orphan = await createdOrphan.json();
    const started = await page.request.post(`${api}/api/ach/responses/orphans/${orphan.id}/review/start`, {
      headers, data: { expectedVersion: orphan.version, reason: 'Inicio de revisión manual E2E' }
    });
    expect(started.ok(), await started.text()).toBeTruthy();
    orphan = await started.json();
    const resolved = await page.request.post(`${api}/api/ach/responses/orphans/${orphan.id}/resolve`, {
      headers,
      data: { expectedVersion: orphan.version, reason: 'Asociación exacta verificada E2E', functionalReference: transactionReference, reject: false }
    });
    expect(resolved.ok(), await resolved.text()).toBeTruthy();
    expect((await resolved.json()).resolutionStatus).toBe('Resolved');

    const responseDetail = await (await page.request.get(`${api}/api/ach/responses/${orphanResponseId}`, { headers })).json();
    const reprocess = await page.request.post(`${api}/api/ach/responses/${orphanResponseId}/reprocess`, {
      headers,
      data: { commandId: crypto.randomUUID(), expectedVersion: responseDetail.version, reason: 'Reproceso gobernado E2E' }
    });
    expect(reprocess.status()).toBe(202);
    expect((await reprocess.json()).status).toBe('Pending');

    const cases = await (await page.request.get(`${api}/api/ach/reconciliation/exceptions`, { headers })).json();
    const reconciliation = cases.find((x: { achResponseId: string; status: string }) => x.achResponseId === orphanResponseId && x.status === 'Open');
    expect(reconciliation).toBeTruthy();
    const reconciled = await page.request.post(`${api}/api/ach/reconciliation/exceptions/${reconciliation.id}/resolve`, {
      headers,
      data: { expectedVersion: reconciliation.version, resolution: 'Associated', reason: 'Conciliación operacional E2E' }
    });
    expect(reconciled.ok(), await reconciled.text()).toBeTruthy();
    expect((await reconciled.json()).status).toBe('Resolved');

    const audit = await (await page.request.get(`${api}/api/ach/responses/${orphanResponseId}/audit`, { headers })).json();
    expect(audit.some((x: { action: string }) => x.action === 'ManualAssociationResolved')).toBeTruthy();
    expect(audit.some((x: { action: string }) => x.action === 'ReprocessRequested')).toBeTruthy();

    await page.goto(`${ui}/ach-responses/manual-review`);
    await expect(page.getByRole('heading', { name: 'Revisión manual de respuestas ACH' })).toBeVisible();
    await page.goto(`${ui}/ach/reconciliation`);
    await expect(page.getByRole('heading', { name: 'Consola de conciliación ACH', exact: true })).toBeVisible();
    await expect(page.getByText('Excepciones operacionales')).toBeVisible();
    expect(await page.locator('body').innerText()).not.toContain('[object Object]');
    expect(jsErrors).toEqual([]);
    expect(httpErrors).toEqual([]);
  });

  test('mantiene el workspace utilizable en viewport móvil', async ({ page }) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await login(page);
    await page.goto(`${ui}/ach-responses/status-mappings`);
    await expect(page.getByRole('heading', { name: 'Mappings de respuestas ACH' })).toBeVisible();
    expect(await page.locator('body').innerText()).not.toContain('[object Object]');
  });
});

async function login(page: Page): Promise<string> {
  const response = await page.request.post(`${api}/auth/login`, { data: { username, password } });
  expect(response.ok(), 'El login real debe responder 200.').toBeTruthy();
  const token = (await response.json()).data.token as string;
  await page.goto(`${ui}/login`);
  await page.locator('input[formControlName="username"]').fill(username);
  await page.locator('input[formControlName="password"]').fill(password!);
  await page.getByRole('button', { name: 'Ingresar' }).click();
  await expect(page).not.toHaveURL(/\/login$/);
  return token;
}

function auth(token: string): Record<string, string> {
  return { Authorization: `Bearer ${token}`, 'X-Correlation-ID': crypto.randomUUID() };
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
