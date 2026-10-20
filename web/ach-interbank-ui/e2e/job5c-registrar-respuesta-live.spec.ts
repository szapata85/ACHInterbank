import { expect, Page, test } from '@playwright/test';

const enabled = process.env['RUN_JOB5C_SOAP_LIVE'] === 'true';
const ui = (process.env['ACH_UI_URL'] ?? 'http://localhost:743').replace(/\/+$/, '');
const api = (process.env['ACH_API_URL'] ?? 'http://localhost:843').replace(/\/+$/, '');
const username = process.env['ACH_USER'] ?? 'admin';
const password = process.env['ACH_PASS'] ?? '';
const correlationId = process.env['JOB5C_SOAP_CORRELATION_ID'] ?? '';
const verifyExisting = process.env['JOB5C_VERIFY_EXISTING'] === 'true';

test.use({ trace: 'off', screenshot: 'off', video: 'off' });

test.describe.serial('JOB 5C - RegistrarRespuestaTransaccion Live local controlado', () => {
  test.skip(!enabled, 'RUN_JOB5C_SOAP_LIVE=true habilita esta prueba Live local.');
  test.setTimeout(180_000);

  test('envía una respuesta sintética validada y conserva la idempotencia del intento', async ({ page }) => {
    expect(correlationId, 'JOB5C_SOAP_CORRELATION_ID es obligatorio.').toMatch(/^JOB5C-LIVE-[A-Z0-9-]+$/);
    const token = await loginThroughUi(page);
    const headers = { Authorization: `Bearer ${token}`, 'X-Correlation-ID': correlationId };
    if (verifyExisting) {
      await verifyExistingResponseWithoutRedispatch(page, headers);
      return;
    }
    await ensureAchColombiaApprovedPrenoteMapping(page, headers);
    const technicalSource = await prepareSyntheticCorrelatedPrenotification(page, headers);
    const idTransaccion = technicalSource.trace;

    const processResponse = await page.request.post(`${api}/api/ach/responses/process`, {
      headers,
      data: {
        tipoRespuesta: 'Prenota',
        idTransaccion,
        codigoCamaraCompensacion: 'ACHCOL',
        codigoEntidadOrigen: 'SYNTHETIC-OTHER',
        codigoEntidadDestino: 'CFA-TEST',
        codigoEstadoExterno: '00',
        codigoCausalExterna: null,
        descripcionCausalExterna: null,
        idCanal: 1,
        nombreCanal: 'JOB5C-LOCAL',
        idTransaccionServicioExterno: 950501,
        fechaRecepcion: new Date().toISOString(),
        correlationId
      }
    });
    const processed = await processResponse.json() as {
      achResponseId: string;
      procesada: boolean;
      duplicada: boolean;
      existeHomologacion: boolean;
      permiteNotificacion: boolean;
      intentoPendienteCreado: boolean;
      estadoProcesamiento: string;
      motivo?: string | null;
    };
    if (!processResponse.ok()) {
      console.log(`JOB5C_PROCESS_BLOCKED=${JSON.stringify({
        status: processResponse.status(),
        procesada: processed.procesada,
        existeHomologacion: processed.existeHomologacion,
        permiteNotificacion: processed.permiteNotificacion,
        estadoProcesamiento: processed.estadoProcesamiento,
        motivo: processed.motivo ?? null
      })}`);
    }
    console.log(`DIFF_RESP_PROCESS=${JSON.stringify({
      procesada: processed.procesada,
      duplicada: processed.duplicada,
      existeHomologacion: processed.existeHomologacion,
      permiteNotificacion: processed.permiteNotificacion,
      intentoPendienteCreado: processed.intentoPendienteCreado,
      estadoProcesamiento: processed.estadoProcesamiento,
      motivo: processed.motivo ?? null
    })}`);
    expect(processResponse.ok(), `HTTP ${processResponse.status()}`).toBeTruthy();
    expect(processed.procesada).toBe(true);
    expect(processed.duplicada).toBe(false);
    expect(processed.existeHomologacion).toBe(true);
    expect(processed.permiteNotificacion).toBe(true);
    expect(processed.intentoPendienteCreado).toBe(true);

    const detailBefore = await getDetail(page, headers, processed.achResponseId);
    expect(detailBefore.notificationAttempts).toHaveLength(1);
    const attempt = detailBefore.notificationAttempts[0];
    expect({
      idCanal: attempt.idCanal,
      nombreCanal: attempt.nombreCanal,
      idTransaccion: attempt.idTransaccion,
      idEstado: attempt.idEstado,
      causal: attempt.causal ?? null,
      idTransaccionAxon: attempt.idTransaccionServicioExterno,
      descripcionCausal: attempt.descripcionCausal
    }).toEqual({
      idCanal: 1,
      nombreCanal: 'JOB5C-LOCAL',
      idTransaccion,
      idEstado: 1,
      causal: null,
      idTransaccionAxon: 950501,
      descripcionCausal: 'Aprobada'
    });

    const sendResponse = await page.request.post(`${api}/api/ach/responses/notifications/send`, {
      headers,
      data: { notificationAttemptId: attempt.id, correlationId }
    });
    expect(sendResponse.ok(), await safeProblem(sendResponse)).toBeTruthy();
    const firstSend = await sendResponse.json() as {
      procesada: boolean;
      encontrada: boolean;
      yaProcesada: boolean;
      existeError: boolean;
      errorTecnico: boolean;
      estadoNotificacion?: string;
      estadoProcesamiento?: string;
      codigoError?: string | null;
    };
    expect(firstSend.procesada).toBe(true);
    expect(firstSend.encontrada).toBe(true);
    expect(firstSend.yaProcesada).toBe(false);
    expect(firstSend.errorTecnico).toBe(false);

    const idempotentResponse = await page.request.post(`${api}/api/ach/responses/notifications/send`, {
      headers,
      data: { notificationAttemptId: attempt.id, correlationId }
    });
    expect(idempotentResponse.ok(), await safeProblem(idempotentResponse)).toBeTruthy();
    const secondSend = await idempotentResponse.json() as {
      procesada: boolean;
      encontrada: boolean;
      yaProcesada: boolean;
      estadoNotificacion?: string;
    };
    expect(secondSend.procesada).toBe(true);
    expect(secondSend.encontrada).toBe(true);
    expect(secondSend.yaProcesada).toBe(true);

    const detailAfter = await getDetail(page, headers, processed.achResponseId);
    expect(detailAfter.notificationAttempts).toHaveLength(1);
    expect(detailAfter.notificationAttempts[0].estadoNotificacion).toBe(firstSend.estadoNotificacion);

    await verifyResponseUi(page, processed.achResponseId);
    expect(await page.locator('body').innerText()).not.toContain('[object Object]');

    console.log(`JOB5C_SOAP_RESULT=${JSON.stringify({
      correlationId,
      responseId: processed.achResponseId,
      attemptId: attempt.id,
      technicalSource: {
        source: 'TransactionalPayload',
        transactionId: technicalSource.transactionId,
        trace: mask(technicalSource.trace)
      },
      parameterNames: [
        'idCanal',
        'nombreCanal',
        'idTransaccion',
        'idEstado',
        'causal',
        'idTransaccionAxon',
        'descripcionCausal'
      ],
      firstSend: {
        existeError: firstSend.existeError,
        errorTecnico: firstSend.errorTecnico,
        estadoNotificacion: firstSend.estadoNotificacion,
        estadoProcesamiento: firstSend.estadoProcesamiento,
        codigoError: firstSend.codigoError ?? null
      },
      secondSend: {
        yaProcesada: secondSend.yaProcesada,
        estadoNotificacion: secondSend.estadoNotificacion
      },
      persistedAttempts: detailAfter.notificationAttempts.length
    })}`);
  });
});

async function verifyExistingResponseWithoutRedispatch(
  page: Page,
  headers: Record<string, string>
): Promise<void> {
  const searchResponse = await page.request.get(
    `${api}/api/ach/responses?correlationId=${encodeURIComponent(correlationId)}&pageNumber=1&pageSize=10`,
    { headers });
  expect(searchResponse.ok(), await safeProblem(searchResponse)).toBeTruthy();
  const search = await searchResponse.json() as {
    items: Array<{ id: string; estadoProcesamiento: string; correlationId?: string }>;
  };
  expect(search.items).toHaveLength(1);
  expect(search.items[0].estadoProcesamiento).toBe('Notificada');
  expect(search.items[0].correlationId).toBe(correlationId);

  const detail = await getDetail(page, headers, search.items[0].id);
  expect(detail.notificationAttempts).toHaveLength(1);
  expect(detail.notificationAttempts[0].estadoNotificacion).toBe('Exitosa');

  const eventReplayResponse = await page.request.post(`${api}/api/ach/responses/process`, {
    headers,
    data: {
      tipoRespuesta: detail.tipoRespuesta,
      idTransaccion: detail.idTransaccion,
      codigoCamaraCompensacion: detail.codigoCamaraCompensacion,
      codigoEntidadOrigen: detail.codigoEntidadOrigen,
      codigoEntidadDestino: detail.codigoEntidadDestino,
      codigoEstadoExterno: detail.codigoEstadoExterno,
      codigoCausalExterna: null,
      descripcionCausalExterna: null,
      idCanal: 1,
      nombreCanal: 'JOB5C-LOCAL',
      idTransaccionServicioExterno: 950501,
      fechaRecepcion: detail.fechaRecepcion,
      correlationId
    }
  });
  expect(eventReplayResponse.ok(), await safeProblem(eventReplayResponse)).toBeTruthy();
  const eventReplay = await eventReplayResponse.json() as {
    achResponseId: string;
    procesada: boolean;
    duplicada: boolean;
    intentoPendienteCreado: boolean;
  };
  expect(eventReplay.achResponseId).toBe(search.items[0].id);
  expect(eventReplay.procesada).toBe(true);
  expect(eventReplay.duplicada).toBe(true);
  expect(eventReplay.intentoPendienteCreado).toBe(false);

  const replayResponse = await page.request.post(`${api}/api/ach/responses/notifications/send`, {
    headers,
    data: { notificationAttemptId: detail.notificationAttempts[0].id, correlationId }
  });
  expect(replayResponse.ok(), await safeProblem(replayResponse)).toBeTruthy();
  const replay = await replayResponse.json() as { procesada: boolean; yaProcesada: boolean; estadoNotificacion: string };
  expect(replay.procesada).toBe(true);
  expect(replay.yaProcesada).toBe(true);
  expect(replay.estadoNotificacion).toBe('Exitosa');
  const detailAfterReplay = await getDetail(page, headers, search.items[0].id);
  expect(detailAfterReplay.estadoProcesamiento).toBe('Notificada');
  expect(detailAfterReplay.duplicateReceiptCount).toBeGreaterThanOrEqual(1);
  expect(detailAfterReplay.notificationAttempts).toHaveLength(1);
  await verifyResponseUi(page, search.items[0].id);
}

async function verifyResponseUi(page: Page, responseId: string): Promise<void> {
  await page.goto(`${ui}/ach-responses/${responseId}`);
  await expect(page.getByRole('heading', { name: 'Detalle respuesta ACH', exact: true, level: 1 })).toBeVisible();
  expect(await page.locator('body').innerText()).not.toContain('[object Object]');
  await page.goto(`${ui}/ach/reconciliation`);
  await expect(page.getByRole('heading', { name: 'Consola de conciliación ACH', exact: true })).toBeVisible();
}

async function loginThroughUi(page: Page): Promise<string> {
  expect(password, 'ACH_PASS es obligatorio para la prueba Live local.').not.toBe('');
  await page.goto(`${ui}/login`);
  const loginResponsePromise = page.waitForResponse(response =>
    new URL(response.url()).pathname === '/auth/login'
    && response.request().method() === 'POST'
    && response.status() === 200);
  await page.locator('input[formControlName="username"]').fill(username);
  await page.locator('input[formControlName="password"]').fill(password);
  await page.getByRole('button', { name: 'Ingresar' }).click();
  const loginResponse = await loginResponsePromise;
  const payload = await loginResponse.json() as { data?: { token?: string } };
  expect(payload.data?.token).toBeTruthy();
  await expect(page).not.toHaveURL(/\/login(?:\?.*)?$/);
  return payload.data!.token!;
}

async function getDetail(page: Page, headers: Record<string, string>, responseId: string): Promise<any> {
  const response = await page.request.get(`${api}/api/ach/responses/${responseId}`, { headers });
  expect(response.ok(), await safeProblem(response)).toBeTruthy();
  return response.json();
}

async function prepareSyntheticCorrelatedPrenotification(
  page: Page,
  headers: Record<string, string>
): Promise<{
  trace: string;
  transactionId: number;
}> {
  const housesResponse = await page.request.get(`${api}/clearing-houses?search=ACHCOL`, { headers });
  expect(housesResponse.ok(), await safeProblem(housesResponse)).toBeTruthy();
  const housesPayload = await housesResponse.json();
  const houses = Array.isArray(housesPayload) ? housesPayload : housesPayload.items;
  const achColombia = houses.find((item: { code?: string }) => item.code === 'ACHCOL') as { id: number } | undefined;
  expect(achColombia?.id).toBeGreaterThan(0);
  await ensureOpenAchColombiaCycle(page, headers, achColombia!.id);
  const institutionsResponse = await page.request.get(`${api}/financial-institutions`, { headers });
  expect(institutionsResponse.ok(), await safeProblem(institutionsResponse)).toBeTruthy();
  const institutions = await institutionsResponse.json() as Array<{
    id: number;
    name: string;
    isDefaultSource: boolean;
    status: number | string;
  }>;
  let external = institutions.find(item => item.name === 'DIFF RESP LOCAL INSTITUTION');
  if (!external) {
    const institutionResponse = await page.request.post(`${api}/financial-institutions`, {
      headers,
      data: {
        id: 0,
        name: 'DIFF RESP LOCAL INSTITUTION',
        isDefaultSource: false,
        routingNumber: '9919',
        transitCode: '9001',
        checkDigit: '0',
        status: 1
      }
    });
    expect(institutionResponse.ok(), await safeProblem(institutionResponse)).toBeTruthy();
    external = await institutionResponse.json() as typeof external;
  }
  expect(external?.id).toBeGreaterThan(0);

  const preferencesResponse = await page.request.get(
    `${api}/institution-clearing-house-preferences`,
    { headers });
  expect(preferencesResponse.ok(), await safeProblem(preferencesResponse)).toBeTruthy();
  const preferences = await preferencesResponse.json() as Array<{
    id: number;
    financialInstitutionId: number;
    clearingHouseId: number;
    isDefault: boolean;
    priority: number;
    isActive: boolean;
  }>;
  const achColombiaPreference = preferences.find(item =>
    item.financialInstitutionId === external!.id && item.clearingHouseId === achColombia!.id);
  if (!achColombiaPreference) {
    const preferenceResponse = await page.request.post(
      `${api}/institution-clearing-house-preferences`,
      {
        headers,
        data: {
          id: 0,
          financialInstitutionId: external!.id,
          clearingHouseId: achColombia!.id,
          isDefault: true,
          priority: 1,
          isActive: true
        }
      });
    expect(preferenceResponse.ok(), await safeProblem(preferenceResponse)).toBeTruthy();
  } else if (!achColombiaPreference.isDefault
      || achColombiaPreference.priority !== 1
      || !achColombiaPreference.isActive) {
    const preferenceResponse = await page.request.put(
      `${api}/institution-clearing-house-preferences/${achColombiaPreference.id}`,
      { headers, data: { isDefault: true, priority: 1, isActive: true } });
    expect(preferenceResponse.ok(), await safeProblem(preferenceResponse)).toBeTruthy();
  }

  const descriptionsResponse = await page.request.get(`${api}/transactions/company-entry-descriptions`, { headers });
  expect(descriptionsResponse.ok(), await safeProblem(descriptionsResponse)).toBeTruthy();
  const descriptions = await descriptionsResponse.json() as Array<{ id: number; isActive?: boolean }>;
  const description = descriptions.find(item => item.isActive !== false);
  expect(description?.id).toBeGreaterThan(0);
  const trace = `9${Date.now().toString().padStart(14, '0').slice(-14)}`;
  expect(trace).toMatch(/^\d{15}$/);

  const transactionResponse = await page.request.post(`${api}/api/transactions`, {
    headers,
    data: {
      amount: 0,
      transactionExternalId: trace,
      reference: trace,
      type: 3,
      accountType: 1,
      isPrenotification: true,
      destinationInstitutionId: external!.id,
      sourceAccountNumber: '7700000000001001',
      destinationAccountNumber: '8800000000001002',
      recipientIdNumber: '900000001',
      recipientName: 'JOB5C RECEPTOR SINTETICO',
      requiresIdentityValidation: false,
      companyName: 'JOB5C SYNTH',
      companyIdentification: '900000002',
      companyEntryDescriptionId: description!.id,
      sourcePersonType: 'PJ',
      recipientPersonType: 'PJ',
      addendas: [{ addendaType: '05', information: 'JOB5C PRENOTIFICACION SINTETICA' }]
    }
  });
  expect(transactionResponse.status(), await safeProblem(transactionResponse)).toBe(201);
  const transaction = await transactionResponse.json() as { id: number };
  expect(transaction.id).toBeGreaterThan(0);

  return {
    trace,
    transactionId: transaction.id
  };
}

async function ensureOpenAchColombiaCycle(
  page: Page,
  headers: Record<string, string>,
  clearingHouseId: number
): Promise<void> {
  const businessDate = new Date().toISOString().slice(0, 10);
  const configResponse = await page.request.get(
    `${api}/clearing-house-cycle-configs/current?clearingHouseId=${clearingHouseId}&effectiveAt=${businessDate}T12:00:00Z`,
    { headers });
  expect(configResponse.ok(), await safeProblem(configResponse)).toBeTruthy();
  const configs = await configResponse.json() as Array<{ id: number; cycleName: string }>;
  let config = configs.find(item => item.cycleName === 'Ciclo 0 DIFF RESP');
  if (!config) {
    const createConfigResponse = await page.request.post(`${api}/clearing-house-cycle-configs`, {
      headers,
      data: {
        clearingHouseId,
        cycleName: 'Ciclo 0 DIFF RESP',
        startTime: '00:00:00',
        endTime: '23:59:59',
        cutoffTime: '23:58:00',
        effectiveFrom: `${businessDate}T00:00:00Z`
      }
    });
    expect(createConfigResponse.ok(), await safeProblem(createConfigResponse)).toBeTruthy();
    config = await createConfigResponse.json() as { id: number; cycleName: string };
  }

  const cyclesResponse = await page.request.get(
    `${api}/api/ach-cycles?clearingHouseId=${clearingHouseId}&processingDate=${businessDate}`,
    { headers });
  expect(cyclesResponse.ok(), await safeProblem(cyclesResponse)).toBeTruthy();
  const cycles = await cyclesResponse.json() as Array<{ id: string; cycleName: string }>;
  if (cycles.some(item => item.cycleName === 'Ciclo 0 DIFF RESP')) return;

  const createCycleResponse = await page.request.post(`${api}/api/ach-cycles`, {
    headers,
    data: {
      cycleName: 'Ciclo 0 DIFF RESP',
      processingDate: `${businessDate}T00:00:00Z`,
      startTime: '00:00:00',
      endTime: '23:59:59',
      cutoffTime: '23:58:00',
      rescheduleOnHoliday: false,
      clearingHouseId,
      clearingHouseCycleConfigId: config!.id
    }
  });
  expect(createCycleResponse.status(), await safeProblem(createCycleResponse)).toBe(201);
}

async function ensureAchColombiaApprovedPrenoteMapping(
  page: Page,
  headers: Record<string, string>
): Promise<void> {
  const existingResponse = await page.request.get(
    `${api}/api/ach/response-status-mappings?codigoCamaraCompensacion=ACHCOL&tipoRespuesta=Prenota&activo=true`,
    { headers });
  expect(existingResponse.ok(), await safeProblem(existingResponse)).toBeTruthy();
  const existing = await existingResponse.json() as Array<{
    id: number;
    codigoEstadoExterno?: string;
    codigoCausalExterna?: string | null;
    idEstadoInterno: number;
    idEstadoServicioExterno: number;
    estadoInternoNombre: string;
    causalNormalizada?: string | null;
    descripcionCausalNormalizada?: string | null;
    requiereCausal: boolean;
    permiteNotificacion: boolean;
    fechaInicioVigencia: string;
    fechaFinVigencia?: string | null;
    clearingHouseId: number;
    priority: number;
    version: string;
  }>;
  const approvedMapping = existing.find(item => item.codigoEstadoExterno === '00');
  if (approvedMapping?.permiteNotificacion) return;

  if (approvedMapping) {
    const updateResponse = await page.request.put(
      `${api}/api/ach/response-status-mappings/${approvedMapping.id}`,
      {
        headers,
        data: {
          clearingHouseId: approvedMapping.clearingHouseId,
          responseType: 'Prenota',
          externalCode: '00',
          externalCause: approvedMapping.codigoCausalExterna ?? null,
          internalStatusId: approvedMapping.idEstadoInterno,
          externalServiceStatusId: approvedMapping.idEstadoServicioExterno,
          internalStatusName: approvedMapping.estadoInternoNombre,
          normalizedCause: approvedMapping.causalNormalizada ?? null,
          normalizedDescription: approvedMapping.descripcionCausalNormalizada ?? 'Aprobada',
          requiresCause: approvedMapping.requiereCausal,
          allowsNotification: true,
          priority: approvedMapping.priority,
          effectiveFrom: approvedMapping.fechaInicioVigencia,
          effectiveTo: approvedMapping.fechaFinVigencia ?? null,
          isActive: true,
          expectedVersion: approvedMapping.version,
          reason: 'DIFF-RESP-001 habilitación local controlada de notificación'
        }
      });
    expect(updateResponse.ok(), await safeProblem(updateResponse)).toBeTruthy();
    return;
  }

  const housesResponse = await page.request.get(`${api}/clearing-houses?search=ACHCOL`, { headers });
  expect(housesResponse.ok(), await safeProblem(housesResponse)).toBeTruthy();
  const housesPayload = await housesResponse.json();
  const houses = Array.isArray(housesPayload) ? housesPayload : housesPayload.items;
  const achColombia = houses.find((item: { code?: string }) => item.code === 'ACHCOL') as { id: number } | undefined;
  expect(achColombia?.id).toBeGreaterThan(0);

  const createResponse = await page.request.post(`${api}/api/ach/response-status-mappings`, {
    headers,
    data: {
      clearingHouseId: achColombia!.id,
      responseType: 'Prenota',
      externalCode: '00',
      externalCause: null,
      internalStatusId: 1,
      externalServiceStatusId: 1,
      internalStatusName: 'Aprobada',
      normalizedCause: null,
      normalizedDescription: 'Aprobada',
      requiresCause: false,
      allowsNotification: true,
      priority: 1000,
      effectiveFrom: new Date(Date.now() - 86_400_000).toISOString(),
      effectiveTo: new Date(Date.now() + 86_400_000).toISOString(),
      isActive: true,
      expectedVersion: null,
      reason: 'JOB5C local controlled mapping'
    }
  });
  expect(createResponse.status(), await safeProblem(createResponse)).toBe(201);
}

async function safeProblem(response: { status(): number; text(): Promise<string> }): Promise<string> {
  const text = await response.text();
  return `HTTP ${response.status()}; bodyLength=${text.length}`;
}

function mask(value: string): string {
  return value.length <= 6 ? '***' : `${value.slice(0, 3)}***${value.slice(-3)}`;
}
