import { expect, Page, test } from '@playwright/test';

const enabled = process.env['RUN_JOB5C_SOAP_LIVE'] === 'true';
const ui = (process.env['ACH_UI_URL'] ?? 'http://localhost:743').replace(/\/+$/, '');
const api = (process.env['ACH_API_URL'] ?? 'http://localhost:843').replace(/\/+$/, '');
const username = process.env['ACH_USER'] ?? 'admin';
const password = process.env['ACH_PASS'] ?? '';
const correlationId = process.env['JOB5C_SOAP_CORRELATION_ID'] ?? '';

test.use({ trace: 'off', screenshot: 'off', video: 'off' });

test.describe.serial('JOB 5C - RegistrarRespuestaTransaccion Live local controlado', () => {
  test.skip(!enabled, 'RUN_JOB5C_SOAP_LIVE=true habilita esta prueba Live local.');
  test.setTimeout(180_000);

  test('envía una respuesta sintética validada y conserva la idempotencia del intento', async ({ page }) => {
    expect(correlationId, 'JOB5C_SOAP_CORRELATION_ID es obligatorio.').toMatch(/^JOB5C-LIVE-[A-Z0-9-]+$/);
    const token = await loginThroughUi(page);
    const headers = { Authorization: `Bearer ${token}`, 'X-Correlation-ID': correlationId };
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

    await page.goto(`${ui}/ach-responses/${processed.achResponseId}`);
    await expect(page.getByRole('heading', { name: /Detalle respuesta ACH/i })).toBeVisible();
    expect(await page.locator('body').innerText()).not.toContain('[object Object]');
    await page.goto(`${ui}/ach/reconciliation`);
    await expect(page.getByRole('heading', { name: 'Consola de conciliación ACH', exact: true })).toBeVisible();
    expect(await page.locator('body').innerText()).not.toContain('[object Object]');

    console.log(`JOB5C_SOAP_RESULT=${JSON.stringify({
      correlationId,
      responseId: processed.achResponseId,
      attemptId: attempt.id,
      technicalSource: {
        simulationId: technicalSource.simulationId,
        fileName: technicalSource.maskedFileName,
        ingestionId: technicalSource.ingestionId,
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
  simulationId: string;
  maskedFileName: string;
  ingestionId: string;
  transactionId: number;
}> {
  const businessDate = new Date().toISOString().slice(0, 10);
  const housesResponse = await page.request.get(`${api}/clearing-houses?search=ACHCOL`, { headers });
  expect(housesResponse.ok(), await safeProblem(housesResponse)).toBeTruthy();
  const housesPayload = await housesResponse.json();
  const houses = Array.isArray(housesPayload) ? housesPayload : housesPayload.items;
  const achColombia = houses.find((item: { code?: string }) => item.code === 'ACHCOL') as { id: number } | undefined;
  expect(achColombia?.id).toBeGreaterThan(0);
  const configsResponse = await page.request.get(
    `${api}/clearing-house-cycle-configs/current?clearingHouseId=${achColombia!.id}&effectiveAt=${businessDate}T00:00:00Z`,
    { headers });
  expect(configsResponse.ok(), await safeProblem(configsResponse)).toBeTruthy();
  const configs = await configsResponse.json() as Array<{
    id: number;
    cycleName: string;
    startTime: string;
    endTime: string;
    cutoffTime: string;
  }>;
  const cycleConfig = configs.find(config => config.cycleName === 'Ciclo 1');
  expect(cycleConfig?.id).toBeGreaterThan(0);

  const cycleResponse = await page.request.get(
    `${api}/api/ach-cycles?clearingHouseId=${achColombia!.id}&processingDate=${businessDate}`,
    { headers });
  expect(cycleResponse.ok(), await safeProblem(cycleResponse)).toBeTruthy();
  const cycles = await cycleResponse.json() as Array<{ id: string; cycleName?: string }>;
  let operationalCycle = cycles.find(cycle => cycle.cycleName === 'Ciclo 1');
  if (!operationalCycle) {
    const createCycleResponse = await page.request.post(`${api}/api/ach-cycles`, {
      headers,
      data: {
        cycleName: 'Ciclo 1',
        processingDate: `${businessDate}T00:00:00Z`,
        startTime: cycleConfig!.startTime,
        endTime: cycleConfig!.endTime,
        cutoffTime: cycleConfig!.cutoffTime,
        rescheduleOnHoliday: false,
        clearingHouseId: achColombia!.id,
        clearingHouseCycleConfigId: cycleConfig!.id
      }
    });
    expect(createCycleResponse.status(), await safeProblem(createCycleResponse)).toBe(201);
    operationalCycle = await createCycleResponse.json() as { id: string; cycleName?: string };
  }
  expect(operationalCycle!.id).toBeTruthy();

  const institutionsResponse = await page.request.get(`${api}/financial-institutions`, { headers });
  expect(institutionsResponse.ok(), await safeProblem(institutionsResponse)).toBeTruthy();
  const institutions = await institutionsResponse.json() as Array<{ id: number; isDefaultSource: boolean; status: number | string }>;
  const cfa = institutions.find(item => item.isDefaultSource);
  const external = institutions.find(item =>
    !item.isDefaultSource && (item.status === 1 || item.status === 'Active'));
  expect(cfa?.id).toBeGreaterThan(0);
  expect(external?.id).toBeGreaterThan(0);

  const descriptionsResponse = await page.request.get(`${api}/transactions/company-entry-descriptions`, { headers });
  expect(descriptionsResponse.ok(), await safeProblem(descriptionsResponse)).toBeTruthy();
  const descriptions = await descriptionsResponse.json() as Array<{ id: number; isActive?: boolean }>;
  const description = descriptions.find(item => item.isActive !== false);
  expect(description?.id).toBeGreaterThan(0);
  const seedExternalId = `${correlationId}-CREDIT-SEED`;
  const cycleTransactionsResponse = await page.request.get(
    `${api}/api/transactions?achCycleName=Ciclo%201&effectiveDate=${businessDate}&clearingHouseId=${achColombia!.id}`,
    { headers });
  expect(cycleTransactionsResponse.ok(), await safeProblem(cycleTransactionsResponse)).toBeTruthy();
  const cycleTransactions = await cycleTransactionsResponse.json() as Array<{ transactionExternalId?: string }>;
  if (!cycleTransactions.some(transaction => transaction.transactionExternalId === seedExternalId)) {
    const seedResponse = await page.request.post(`${api}/api/transactions`, {
      headers,
      data: {
        amount: 1,
        transactionExternalId: seedExternalId,
        reference: `J5C-CREDIT-${correlationId.slice(-3)}`,
        type: 1,
        accountType: 1,
        isPrenotification: false,
        destinationInstitutionId: external!.id,
        sourceAccountNumber: '7700000000003001',
        destinationAccountNumber: '8800000000003002',
        recipientIdNumber: '900000021',
        recipientName: 'JOB5C CREDIT SEED',
        requiresIdentityValidation: false,
        companyName: 'JOB5C SYNTH',
        companyIdentification: '900000022',
        companyEntryDescriptionId: description!.id,
        sourcePersonType: 'PJ',
        recipientPersonType: 'PJ',
        addendas: [{ addendaType: '05', information: 'JOB5C CREDIT SEED SINTETICO' }]
      }
    });
    expect(seedResponse.status(), await safeProblem(seedResponse)).toBe(201);
  }

  const generateResponse = await page.request.post(`${api}/api/uat/nacha-inbound-simulator/generate`, {
    headers,
    data: {
      simulationMode: 'IncomingTransactions',
      clearingHouseCode: 'ACHCOL',
      scenarioType: 'IncomingCredit',
      originFinancialInstitutionId: external!.id,
      destinationFinancialInstitutionId: cfa!.id,
      entriesCount: 1,
      amount: 1,
      referencePrefix: 'JOB5C-SYN',
      businessDate,
      cycleCode: operationalCycle!.id,
      pendingPrenotificationReferences: [],
      transactionReferences: [],
      responseMode: null,
      reasonCode: null,
      notes: 'JOB5C SOAP technical source'
    }
  });
  if (!generateResponse.ok()) {
    const problem = await generateResponse.json() as { title?: string; detail?: string };
    console.log(`JOB5C_SIMULATOR_BLOCKED=${JSON.stringify({
      status: generateResponse.status(),
      title: problem.title ?? null,
      detail: problem.detail ?? null
    })}`);
  }
  expect(generateResponse.status(), await safeProblem(generateResponse)).toBe(201);
  const generated = await generateResponse.json() as {
    id: number;
    simulationId: string;
    fileName: string;
    downloadUrl: string;
  };

  const downloadResponse = await page.request.get(new URL(generated.downloadUrl, api).toString(), { headers });
  expect(downloadResponse.ok(), await safeProblem(downloadResponse)).toBeTruthy();
  const fileBuffer = await downloadResponse.body();
  const fileText = fileBuffer.toString('utf8');
  const records = /\r|\n/.test(fileText)
    ? fileText.split(/\r?\n/).filter(Boolean)
    : Array.from({ length: Math.floor(fileText.length / 106) }, (_, index) =>
        fileText.slice(index * 106, (index + 1) * 106));
  const entryLine = records.find(line => line.length >= 102 && line.startsWith('6'));
  expect(entryLine, 'El simulador debe producir un registro tipo 6.').toBeTruthy();
  const trace = entryLine!.slice(87, 102).trim();
  expect(trace).toMatch(/^\d{15}$/);

  await navigateToUploadFromMenu(page);
  await page.locator('input[type="file"]').setInputFiles({
    name: generated.fileName,
    mimeType: 'application/octet-stream',
    buffer: fileBuffer
  });
  const uploadResponsePromise = page.waitForResponse(response =>
    /\/NachaUpload\/upload(?:\?.*)?$/.test(response.url())
    && response.request().method() === 'POST');
  await page.getByRole('button', { name: 'Cargar archivo' }).click();
  const uploadResponse = await uploadResponsePromise;
  expect(uploadResponse.ok(), `Synthetic technical upload HTTP ${uploadResponse.status()}`).toBeTruthy();
  const upload = await uploadResponse.json() as {
    ingestionId: string;
    ingestionStatus: string;
    parsingStatus: string;
    totalEntries: number;
  };
  expect(upload.ingestionStatus).toBe('Completado');
  expect(upload.parsingStatus).toMatch(/^Exitoso/);
  expect(upload.totalEntries).toBe(1);

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
    simulationId: generated.simulationId,
    maskedFileName: maskFileName(generated.fileName),
    ingestionId: upload.ingestionId,
    transactionId: transaction.id
  };
}

async function navigateToUploadFromMenu(page: Page): Promise<void> {
  const parent = page.getByRole('button', { name: /Transacciones/i }).first();
  if (await parent.isVisible()) {
    await parent.click();
  }
  await page.getByRole('link', { name: /Cargar NACHA-M/i }).click();
  await expect(page).toHaveURL(/\/transactions\/nacha-upload$/);
}

async function ensureAchColombiaApprovedPrenoteMapping(
  page: Page,
  headers: Record<string, string>
): Promise<void> {
  const existingResponse = await page.request.get(
    `${api}/api/ach/response-status-mappings?codigoCamaraCompensacion=ACHCOL&tipoRespuesta=Prenota&activo=true`,
    { headers });
  expect(existingResponse.ok(), await safeProblem(existingResponse)).toBeTruthy();
  const existing = await existingResponse.json() as Array<{ codigoEstadoExterno?: string }>;
  if (existing.some(item => item.codigoEstadoExterno === '00')) {
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

function maskFileName(value: string): string {
  const parts = value.split('.');
  return `${parts[0]?.slice(0, 7) ?? '***'}.***${value.toUpperCase().endsWith('.OUT') ? '.OUT' : ''}`;
}
