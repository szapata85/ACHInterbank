import { expect, Page, test, TestInfo } from '@playwright/test';
import { existsSync, readdirSync, readFileSync, statSync } from 'node:fs';
import { join } from 'node:path';
import { G36Postgres, pollUntil, type AchCycleSnapshot } from './support/g36-postgres';

type AuthLoginResponse = {
  data?: {
    token?: string;
  };
};

type FinancialInstitution = {
  id: number;
  name: string;
  isDefaultSource?: boolean;
  routingNumber?: string;
  transitCode?: string;
  status?: number;
};

type CompanyEntryDescription = {
  id: number;
  term?: string;
  description?: string;
  isActive?: boolean;
};

type CreatedTransaction = {
  id: number;
  transactionExternalId?: string;
  reference?: string;
  amount?: number;
  achCycleId?: string;
  achBatch?: {
    id?: number;
    achCycleId?: string;
  } | null;
};

type TransactionRow = {
  id: number;
  transactionExternalId: string;
  achCycleId: string;
  clearingHouseId: number;
  sourceInstitutionId: number | null;
  destinationInstitutionId: number;
  type: number;
};

type ContrapartidaDispatchResult = {
  cycleId?: string;
  clearingHouseId?: number;
  processed?: number;
  succeeded?: number;
  failed?: number;
  partial?: number;
  chunks?: number;
  summary?: string;
};

type DispatchEvidenceRow = {
  result: number;
  externalResponseCode: string | null;
  externalResponseMessage: string | null;
  errorCode: string | null;
  errorMessage: string | null;
  requestPayloadXml: string;
  responsePayloadXml: string;
  requestedBy: string;
  batchStatus: number;
  correlationId: string;
};

type SoapInputParameterMapping = {
  inputName: string;
  soapParameterName: string;
  defaultValue?: string | null;
  required: boolean;
};

type SoapEndpointMethodMapping = {
  methodName: string;
  endpoint: string;
  soapAction: string;
  enabled: boolean;
  inputParameterMappings: SoapInputParameterMapping[];
};

type SoapIntegrationSettings = {
  wscfaachMappings: SoapEndpointMethodMapping[];
  wsAxonRespuestaTransaccionesMappings: SoapEndpointMethodMapping[];
};

type LocalSoapEvidence = {
  source: string;
  text: string;
};

const shouldRun =
  process.env['RUN_LOCAL_SOAP_PROC_CONTRAPARTIDAS_E2E'] === 'true'
  && process.env['ALLOW_LOCAL_MONETARY_SOAP_E2E'] === 'true';

const hasRuntimeCredentials = Boolean(process.env['ACH_USER'] && process.env['ACH_PASS']);
const hasSoapLogSource = Boolean(process.env['SOAP_LOCAL_WSCFAACH_LOG'] || process.env['SOAP_LOCAL_LOG_DIR']);

test.describe.configure({ mode: 'serial' });
test.skip(!shouldRun, 'RUN_LOCAL_SOAP_PROC_CONTRAPARTIDAS_E2E=true y ALLOW_LOCAL_MONETARY_SOAP_E2E=true son requeridos para esta prueba local/UAT.');
test.skip(!hasRuntimeCredentials, 'ACH_USER y ACH_PASS deben venir del entorno; el spec no contiene credenciales.');
test.skip(!hasSoapLogSource, 'SOAP_LOCAL_WSCFAACH_LOG o SOAP_LOCAL_LOG_DIR es requerido para validar evidencia del SOAP local.');

const uiBaseUrl = (process.env['ACH_UI_URL'] ?? process.env['E2E_BASE_URL'] ?? 'http://localhost:743').replace(/\/+$/, '');
const apiBaseUrl = (process.env['ACH_API_URL'] ?? 'http://localhost:843').replace(/\/+$/, '');
const username = process.env['ACH_USER'] ?? '';
const password = process.env['ACH_PASS'] ?? '';
const wscfaachEndpoint = process.env['SOAP_LOCAL_WSCFAACH_URL'] ?? 'http://localhost:7083/WSCFAACH.svc';
const axonEndpoint = process.env['SOAP_LOCAL_AXON_RESPONSE_URL'] ?? 'http://localhost:7083/WSAxonRespuestaTransacciones.svc';
const configureSoapSettings = process.env['PROC_CONTRA_CONFIGURE_SOAP_SETTINGS'] !== 'false';
const runSeed = process.env['PROC_CONTRA_RUN_SEED'] === 'true';
const targetInstitutionName = process.env['PROC_CONTRA_DESTINATION_INSTITUTION_NAME'] ?? '';
const dispatchTriggeredBy = 'playwright-local-proc-contrapartidas';

const loginPath = '/auth/login';
const seedPath = '/Maintenance/seed';
const transactionsPath = '/transactions';
const financialInstitutionsPath = '/financial-institutions';
const companyEntryDescriptionsPath = '/transactions/company-entry-descriptions';
const soapSettingsPath = '/api/users/soap-integrations';
const contrapartidaDispatchPath = '/api/uat/contrapartidas/dispatch-cycle';

test('SPA /transactions crea debito CFA y dispara Proc_Contrapartidas contra SOAP local', async ({ page }, testInfo) => {
  test.setTimeout(600_000);
  const startedAt = new Date();
  const db = new G36Postgres();
  let originalSoapSettings: SoapIntegrationSettings | null = null;
  let cycleSnapshot: AchCycleSnapshot | null = null;

  const reference = `PW-CONTRA-${Date.now()}`;
  const sourceAccountNumber = `44${String(Date.now()).slice(-10)}`;
  const destinationAccountNumber = `55${String(Date.now()).slice(-10)}`;
  const recipientIdNumber = `70${String(Date.now()).slice(-8)}`;
  const sourceCompanyIdentification = `PW${String(Date.now()).slice(-8)}`;
  const sourceCompanyName = `PWCONTRA${String(Date.now()).slice(-6)}`;
  const amount = 1500;

  const runtime = await authenticateRuntime();
  await page.addInitScript((accessToken) => {
    window.sessionStorage.setItem('ach.interbank.access_token', accessToken);
  }, runtime.token);

  try {
    await db.assertReady();

    if (runSeed) {
      await seedDatabase(runtime.token);
    }

    if (configureSoapSettings) {
      originalSoapSettings = await apiGetJson<SoapIntegrationSettings>(soapSettingsPath, runtime.token);
      const localSettings = buildLocalSoapSettings(originalSoapSettings);
      await apiPutJson<SoapIntegrationSettings>(soapSettingsPath, runtime.token, localSettings);
    }

    const institutions = await apiGetJson<FinancialInstitution[]>(financialInstitutionsPath, runtime.token);
    const defaultSource = institutions.find((item) => item.isDefaultSource) ?? null;
    expect(defaultSource, 'Debe existir una entidad financiera CFA con IsDefaultSource=true.').not.toBeNull();

    const targetInstitution = resolveTargetInstitution(institutions, defaultSource!.id);
    expect(targetInstitution.isDefaultSource, 'La entidad destino debe ser externa, no CFA.').not.toBeTruthy();

    const companyEntryDescription = await resolveCompanyEntryDescription(runtime.token);

    const createdPrenote = await apiPostJson<CreatedTransaction>(transactionsPath, runtime.token, {
      amount: 0,
      transactionExternalId: `${reference}-PRE`,
      reference: `${reference.slice(-20)}P`,
      type: 2,
      accountType: 1,
      isPrenotification: true,
      destinationInstitutionId: targetInstitution.id,
      sourceAccountNumber,
      destinationAccountNumber,
      recipientIdNumber,
      recipientName: 'RECEPTOR QA CONTRA',
      requiresIdentityValidation: false,
      companyName: sourceCompanyName,
      companyIdentification: sourceCompanyIdentification,
      companyEntryDescriptionId: companyEntryDescription.id,
      sourcePersonType: 'PJ',
      recipientPersonType: 'PJ',
      addendas: [
        {
          addendaType: '05',
          information: `${reference}-PRE-ADD`
        }
      ]
    });
    expect(createdPrenote.id, 'La prenotificacion sintetica debe quedar creada como prerequisito.').toBeGreaterThan(0);

    await page.goto(joinUrl(uiBaseUrl, '/transactions/create'));
    await expect(page.getByRole('heading', { name: /Crear transaccion ACH|Crear transacción ACH/i })).toBeVisible();

    await fillTransactionFormFromUi(page, {
      reference,
      amount,
      sourceAccountNumber,
      destinationAccountNumber,
      recipientIdNumber,
      sourceCompanyIdentification,
      sourceCompanyName,
      targetInstitutionName: targetInstitution.name,
      companyEntryDescriptionLabel: companyEntryDescription.term ?? companyEntryDescription.description ?? ''
    });

    await testInfo.attach('transactions-create-filled.png', {
      body: await page.screenshot({ fullPage: true }),
      contentType: 'image/png'
    });

    const createResponsePromise = page.waitForResponse((response) =>
      response.request().method() === 'POST'
      && normalizeUrlPath(response.url()).endsWith('/transactions'));

    await page.getByRole('button', { name: /Registrar transacci[oó]n/i }).click();
    const createResponse = await createResponsePromise;
    expect(createResponse.ok(), `POST /transactions desde la SPA debe responder OK. Status=${createResponse.status()}`).toBeTruthy();
    const createdTransaction = await createResponse.json() as CreatedTransaction;
    expect(createdTransaction.id, 'La transaccion monetaria creada desde UI debe devolver id.').toBeGreaterThan(0);

    await expect(page).toHaveURL(/\/transactions(?:\?.*)?$/);
    await testInfo.attach('transactions-list-after-create.png', {
      body: await page.screenshot({ fullPage: true }),
      contentType: 'image/png'
    });

    const transaction = await pollUntil(
      async () => findTransactionByExternalId(db, reference),
      `transaccion ${reference} creada en base`,
      120_000
    );

    expect(transaction.type, 'La transaccion objetivo debe ser debito.').toBe(2);
    expect(transaction.sourceInstitutionId, 'La transaccion debe originarse desde CFA IsDefaultSource=true.').toBe(defaultSource!.id);
    expect(transaction.destinationInstitutionId, 'La entidad destino debe ser externa.').toBe(targetInstitution.id);
    expect(transaction.destinationInstitutionId).not.toBe(defaultSource!.id);

    cycleSnapshot = await loadCycleSnapshot(db, transaction.achCycleId, transaction.clearingHouseId);
    await db.configureCycle(cycleSnapshot, cycleSnapshot.cycleName, todayIsoDate());

    await expect.poll(async () => countDispatchItems(db, transaction.id), {
      timeout: 120_000,
      intervals: [2_000, 5_000, 10_000]
    }).toBeGreaterThan(0);

    const dispatchResult = await apiPostJson<ContrapartidaDispatchResult>(contrapartidaDispatchPath, runtime.token, {
      cycleId: transaction.achCycleId,
      clearingHouseId: transaction.clearingHouseId,
      triggeredBy: dispatchTriggeredBy,
      chunkSize: 50
    }, {
      'X-UAT-Transaction-Nacha-Dispatch': 'true'
    });

    expect(dispatchResult.cycleId).toBe(transaction.achCycleId);
    expect(dispatchResult.clearingHouseId).toBe(transaction.clearingHouseId);
    expect(dispatchResult.processed ?? 0, 'El dispatch debe procesar al menos una transaccion.').toBeGreaterThan(0);

    const evidence = await pollUntil(
      async () => findDispatchEvidence(db, reference),
      `evidencia Proc_Contrapartidas para ${reference}`,
      180_000
    );

    assertProcContrapartidasPayload(evidence.requestPayloadXml, transaction.clearingHouseId);
    expect(evidence.requestPayloadXml).not.toContain('Proc_Transacciones');
    expect(evidence.requestPayloadXml).not.toContain('RegistrarRespuestaTransaccion');
    expect(evidence.requestPayloadXml).not.toMatch(/<[^>]*METODO[^>]*>/i);

    expect(evidence.externalResponseCode ?? evidence.errorCode ?? '', [
      'El backend debe estar en modo live para esta prueba.',
      'Si aparece PROC_DRY_RUN o PROC_DISABLED, arranque la API con ProcContrapartidas__Mode=Live solo en local/UAT autorizado.'
    ].join(' ')).not.toMatch(/PROC_DRY_RUN|PROC_DISABLED/i);

    const localSoapEvidence = readLocalSoapEvidence(startedAt, evidence.requestPayloadXml);
    const localEnvelope = extractEnvelopeNear(localSoapEvidence.text, evidence.requestPayloadXml) ?? localSoapEvidence.text;
    expect(localEnvelope, 'El log plano del SOAP local debe contener evidencia de Proc_Contrapartidas.').toContain('Proc_Contrapartidas');
    expect(localEnvelope).toContain('OFNIT');
    expect(localEnvelope).toContain('OFCTA');
    expect(localEnvelope).toContain('OFMONDEB');
    expect(localEnvelope).toContain('OFIDCAMCOMPE');
    expect(localEnvelope).toContain('OFFECHEFEC');
    expect(localEnvelope).not.toMatch(/<[^>]*METODO[^>]*>/i);
    expect(localEnvelope).not.toContain('Proc_Transacciones');

    await testInfo.attach('proc-contrapartidas-request-sanitized.xml', {
      body: sanitizeEvidence(evidence.requestPayloadXml),
      contentType: 'application/xml'
    });
    await testInfo.attach('proc-contrapartidas-local-soap-log-sanitized.txt', {
      body: `source=${localSoapEvidence.source}\n\n${sanitizeEvidence(localEnvelope)}`,
      contentType: 'text/plain'
    });
  } finally {
    if (cycleSnapshot) {
      await db.restoreCycle(cycleSnapshot);
    }

    if (originalSoapSettings) {
      await apiPutJson<SoapIntegrationSettings>(soapSettingsPath, runtime.token, originalSoapSettings);
    }

    await db.close();
  }
});

async function fillTransactionFormFromUi(page: Page, data: {
  reference: string;
  amount: number;
  sourceAccountNumber: string;
  destinationAccountNumber: string;
  recipientIdNumber: string;
  sourceCompanyIdentification: string;
  sourceCompanyName: string;
  targetInstitutionName: string;
  companyEntryDescriptionLabel: string;
}): Promise<void> {
  await fillInput(page, 'Monto', String(data.amount));
  await fillInput(page, 'ID operación cliente', data.reference);
  await fillInput(page, 'Referencia legado', data.reference.slice(-20));
  await selectOption(page, 'Tipo', 'Débito');
  await selectOption(page, 'Tipo de cuenta', 'Cuenta corriente');
  await fillInput(page, 'Cuenta origen', data.sourceAccountNumber);
  await selectOption(page, 'Tipo persona originador', 'Persona jurídica');
  await fillInput(page, 'Identificación usuario originador', data.sourceCompanyIdentification);
  await fillInput(page, 'Nombre usuario originador', data.sourceCompanyName.slice(0, 16));
  await selectOption(page, 'Institución destino', data.targetInstitutionName);
  await selectOption(page, 'Cuenta destino', data.destinationAccountNumber);
  await selectOption(page, 'Tipo persona receptor', 'Persona jurídica');
  await fillInput(page, 'Identificación del receptor', data.recipientIdNumber);
  await fillInput(page, 'Nombre del receptor', 'RECEPTOR QA CONTRA');
  await selectOption(page, 'Descripción de la entrada', data.companyEntryDescriptionLabel || 'NOMINAS');
  await selectOption(page, 'Código tipo registro adenda', '05 - Información adicional');
  await fillInput(page, 'Información', `${data.reference}-ADDENDA`);
}

async function fillInput(page: Page, labelText: string, value: string): Promise<void> {
  const field = fieldByLabel(page, labelText);
  const input = field.locator('input').first();
  await expect(input, `Debe existir input para ${labelText}.`).toBeVisible();
  await input.fill(value);
}

async function selectOption(page: Page, labelText: string, optionText: string): Promise<void> {
  const field = fieldByLabel(page, labelText);
  const input = field.locator('.ui-selector input').first();
  await expect(input, `Debe existir selector para ${labelText}.`).toBeVisible();
  await input.fill(optionText);

  const option = field.locator('button.opcion').filter({ hasText: optionText }).first();
  await expect(option, `Debe existir opcion "${optionText}" en ${labelText}.`).toBeVisible();
  await option.click();
}

function fieldByLabel(page: Page, labelText: string) {
  const labelPattern = new RegExp(`^${escapeRegExp(labelText)}(?:\\s*\\*)?$`, 'i');
  return page.locator('label')
    .filter({ has: page.locator('span').filter({ hasText: labelPattern }) })
    .first();
}

async function authenticateRuntime(): Promise<{ token: string }> {
  const response = await fetch(joinUrl(apiBaseUrl, loginPath), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username, password })
  });

  expect(response.ok, 'Debe autenticarse contra el API local/UAT real.').toBeTruthy();
  const payload = await response.json() as AuthLoginResponse;
  const token = payload.data?.token;
  expect(token, 'El login debe devolver access token.').toBeTruthy();
  return { token: token as string };
}

async function seedDatabase(token: string): Promise<void> {
  const response = await fetch(joinUrl(apiBaseUrl, seedPath), {
    method: 'POST',
    headers: authHeaders(token)
  });

  expect(response.ok, 'El seed local/UAT debe completar si PROC_CONTRA_RUN_SEED=true.').toBeTruthy();
}

async function apiGetJson<T>(path: string, token: string): Promise<T> {
  const response = await fetch(joinUrl(apiBaseUrl, path), {
    headers: authHeaders(token)
  });

  if (!response.ok) {
    throw new Error(`GET ${path} debe responder 200. Status=${response.status}, body=${await response.text()}`);
  }

  return await response.json() as T;
}

async function apiPostJson<T>(path: string, token: string, body: unknown, extraHeaders: Record<string, string> = {}): Promise<T> {
  const response = await fetch(joinUrl(apiBaseUrl, path), {
    method: 'POST',
    headers: {
      ...authHeaders(token, true),
      ...extraHeaders
    },
    body: JSON.stringify(body)
  });

  if (!(response.ok || response.status === 201)) {
    throw new Error(`POST ${path} debe responder 200/201. Status=${response.status}, body=${await response.text()}`);
  }

  return await response.json() as T;
}

async function apiPutJson<T>(path: string, token: string, body: unknown): Promise<T> {
  const response = await fetch(joinUrl(apiBaseUrl, path), {
    method: 'PUT',
    headers: authHeaders(token, true),
    body: JSON.stringify(body)
  });

  if (!response.ok) {
    throw new Error(`PUT ${path} debe responder 200. Status=${response.status}, body=${await response.text()}`);
  }

  return await response.json() as T;
}

async function resolveCompanyEntryDescription(token: string): Promise<CompanyEntryDescription> {
  const items = await apiGetJson<CompanyEntryDescription[]>(companyEntryDescriptionsPath, token);
  const selected = items.find((item) => item.term?.toUpperCase() === 'NOMINAS')
    ?? items.find((item) => item.isActive !== false)
    ?? null;
  expect(selected, 'Debe existir un concepto de entrada activo.').not.toBeNull();
  return selected!;
}

function resolveTargetInstitution(institutions: FinancialInstitution[], defaultSourceId: number): FinancialInstitution {
  const activeExternal = institutions.filter((item) => item.id !== defaultSourceId && item.isDefaultSource !== true && (item.status ?? 1) === 1);
  if (targetInstitutionName.trim()) {
    const byName = activeExternal.find((item) => item.name.toLowerCase().includes(targetInstitutionName.trim().toLowerCase()));
    expect(byName, `Debe existir entidad externa que coincida con PROC_CONTRA_DESTINATION_INSTITUTION_NAME=${targetInstitutionName}.`).not.toBeNull();
    return byName!;
  }

  expect(activeExternal.length, 'Debe existir al menos una entidad destino externa activa.').toBeGreaterThan(0);
  return activeExternal[0];
}

function buildLocalSoapSettings(settings: SoapIntegrationSettings): SoapIntegrationSettings {
  const cloned = JSON.parse(JSON.stringify(settings)) as SoapIntegrationSettings;
  setMappingEndpoint(cloned.wscfaachMappings, 'Proc_Contrapartidas', wscfaachEndpoint, 'http://tempuri.org/IWSCFAACH/Proc_Contrapartidas');
  setMappingEndpoint(cloned.wscfaachMappings, 'Proc_Transacciones', wscfaachEndpoint, 'http://tempuri.org/IWSCFAACH/Proc_Transacciones');
  setMappingEndpoint(cloned.wsAxonRespuestaTransaccionesMappings, 'RegistrarRespuestaTransaccion', axonEndpoint, 'http://tempuri.org/IWSAxonRespuestaTransacciones/RegistrarRespuestaTransaccion');
  assertNoMetodoMapping(cloned);
  return cloned;
}

function setMappingEndpoint(
  mappings: SoapEndpointMethodMapping[],
  methodName: string,
  endpoint: string,
  soapAction: string
): void {
  const mapping = mappings.find((item) => item.methodName.toLowerCase() === methodName.toLowerCase());
  expect(mapping, `Debe existir mapping SOAP para ${methodName}.`).toBeTruthy();
  mapping!.endpoint = endpoint;
  mapping!.soapAction = soapAction;
  mapping!.enabled = true;
}

function assertNoMetodoMapping(settings: SoapIntegrationSettings): void {
  const allMappings = [
    ...settings.wscfaachMappings,
    ...settings.wsAxonRespuestaTransaccionesMappings
  ];

  for (const mapping of allMappings) {
    for (const parameter of mapping.inputParameterMappings ?? []) {
      expect(parameter.inputName, `${mapping.methodName} no debe mapear METODO como input.`).not.toMatch(/^METODO$/i);
      expect(parameter.soapParameterName, `${mapping.methodName} no debe mapear METODO como parametro SOAP.`).not.toMatch(/^METODO$/i);
    }
  }
}

async function findTransactionByExternalId(db: G36Postgres, transactionExternalId: string): Promise<TransactionRow | null> {
  const rows = await db.query<TransactionRow>(
    `SELECT t."Id" AS id,
            t."TransactionExternalId" AS "transactionExternalId",
            t."AchCycleId" AS "achCycleId",
            c."ClearingHouseId" AS "clearingHouseId",
            t."SourceInstitutionId" AS "sourceInstitutionId",
            t."DestinationInstitutionId" AS "destinationInstitutionId",
            t."Type" AS type
     FROM "AchTransactions" t
     JOIN "AchCycles" c ON c."Id" = t."AchCycleId"
     WHERE t."TransactionExternalId" = $1
     ORDER BY t."Id" DESC
     LIMIT 1`,
    [transactionExternalId]
  );

  return rows[0] ?? null;
}

async function loadCycleSnapshot(db: G36Postgres, cycleId: string, clearingHouseId: number): Promise<AchCycleSnapshot> {
  const rows = await db.query<AchCycleSnapshot>(
    `SELECT "Id" AS id,
            "CycleName" AS "cycleName",
            "ProcessingDate" AS "processingDate",
            "CutoffTime"::text AS "cutoffTime",
            "StartTime"::text AS "startTime",
            "EndTime"::text AS "endTime",
            "RescheduleOnHoliday" AS "rescheduleOnHoliday",
            "ClearingHouseId" AS "clearingHouseId",
            "UpdatedAt" AS "updatedAt"
     FROM "AchCycles"
     WHERE "Id" = $1 AND "ClearingHouseId" = $2`,
    [cycleId, clearingHouseId]
  );

  expect(rows, `Debe existir el ciclo ${cycleId}.`).toHaveLength(1);
  return rows[0];
}

async function countDispatchItems(db: G36Postgres, transactionId: number): Promise<number> {
  return Number(await db.scalar<string>(
    `SELECT COUNT(*)::text
     FROM "ContrapartidaDispatchItems"
     WHERE "AchTransactionId" = $1`,
    [transactionId]
  ) ?? 0);
}

async function findDispatchEvidence(db: G36Postgres, transactionExternalId: string): Promise<DispatchEvidenceRow | null> {
  const rows = await db.query<DispatchEvidenceRow>(
    `SELECT a."Result" AS result,
            a."ExternalResponseCode" AS "externalResponseCode",
            a."ExternalResponseMessage" AS "externalResponseMessage",
            a."ErrorCode" AS "errorCode",
            a."ErrorMessage" AS "errorMessage",
            a."RequestPayloadXml" AS "requestPayloadXml",
            a."ResponsePayloadXml" AS "responsePayloadXml",
            b."RequestedBy" AS "requestedBy",
            b."Status" AS "batchStatus",
            a."CorrelationId" AS "correlationId"
     FROM "ContrapartidaDispatchAttempts" a
     JOIN "ContrapartidaDispatchItems" i ON i."Id" = a."DispatchItemId"
     JOIN "ContrapartidaDispatchBatches" b ON b."Id" = a."DispatchBatchId"
     JOIN "AchTransactions" t ON t."Id" = i."AchTransactionId"
     WHERE t."TransactionExternalId" = $1
       AND b."RequestedBy" = $2
     ORDER BY a."FinishedAtUtc" DESC
     LIMIT 1`,
    [transactionExternalId, dispatchTriggeredBy]
  );

  return rows[0] ?? null;
}

function assertProcContrapartidasPayload(xml: string, clearingHouseId: number): void {
  expect(xml, 'El request persistido debe contener Proc_Contrapartidas.').toContain('Proc_Contrapartidas');

  for (const field of [
    'OFNIT',
    'OFEMP',
    'OFCTA',
    'OFDD',
    'OFFECHEFEC',
    'OFMONDEB',
    'OFMONCRE',
    'OFIDARCH',
    'OFIDLOT',
    'OFST',
    'OFIDTX',
    'OFIDREVER',
    'OFIDEBAPLI',
    'OFIDCAMCOMPE',
    'ILR',
    'cantTrans'
  ]) {
    expect(xml, `El request debe contener ${field}.`).toContain(field);
  }

  expect(xml, 'OFFECHEFEC debe viajar en formato yyyyMMdd.').toMatch(/<[^>]*OFFECHEFEC[^>]*>\d{8}<\/[^>]*OFFECHEFEC>/);
  expect(xml, 'OFMONCRE observado debe ser 0.').toMatch(/<[^>]*OFMONCRE[^>]*>0(?:\.0+)?<\/[^>]*OFMONCRE>/);
  expect(xml, 'OFST observado debe ser OO.').toMatch(/<[^>]*OFST[^>]*>OO<\/[^>]*OFST>/);
  expect(xml, 'OFIDTX observado debe ser 0.').toMatch(/<[^>]*OFIDTX[^>]*>0<\/[^>]*OFIDTX>/);
  expect(xml, 'OFIDREVER observado debe ser 0.').toMatch(/<[^>]*OFIDREVER[^>]*>0<\/[^>]*OFIDREVER>/);
  expect(xml, 'OFIDEBAPLI observado debe ser 1.').toMatch(/<[^>]*OFIDEBAPLI[^>]*>1<\/[^>]*OFIDEBAPLI>/);
  expect(xml, 'ILR observado debe ser A.').toMatch(/<[^>]*ILR[^>]*>A<\/[^>]*ILR>/);
  expect(xml, 'cantTrans observado debe ser 1.').toMatch(/<[^>]*cantTrans[^>]*>1<\/[^>]*cantTrans>/);

  if (clearingHouseId === 1) {
    expect(xml, 'ACH Colombia debe enviar OFIDCAMCOMPE=1.').toMatch(/<[^>]*OFIDCAMCOMPE[^>]*>1<\/[^>]*OFIDCAMCOMPE>/);
  }
}

function readLocalSoapEvidence(startedAt: Date, expectedRequestXml: string): LocalSoapEvidence {
  const explicitLog = process.env['SOAP_LOCAL_WSCFAACH_LOG'];
  if (explicitLog) {
    expect(existsSync(explicitLog), `SOAP_LOCAL_WSCFAACH_LOG debe existir: ${explicitLog}`).toBeTruthy();
    return {
      source: explicitLog,
      text: readFileSync(explicitLog, 'utf8')
    };
  }

  const logDir = process.env['SOAP_LOCAL_LOG_DIR'] ?? '';
  expect(existsSync(logDir), `SOAP_LOCAL_LOG_DIR debe existir: ${logDir}`).toBeTruthy();
  const candidates = readdirSync(logDir)
    .map((name) => join(logDir, name))
    .filter((filePath) => {
      if (!/\.(log|txt|xml)$/i.test(filePath)) {
        return false;
      }

      const stats = statSync(filePath);
      return stats.isFile() && stats.mtime.getTime() >= startedAt.getTime() - 5_000;
    })
    .sort((a, b) => statSync(b).mtimeMs - statSync(a).mtimeMs);

  const expectedOfIdTx = extractElementText(expectedRequestXml, 'OFIDTX');
  for (const candidate of candidates) {
    const text = readFileSync(candidate, 'utf8');
    if (text.includes('Proc_Contrapartidas') || (expectedOfIdTx && text.includes(expectedOfIdTx))) {
      return { source: candidate, text };
    }
  }

  throw new Error(`No se encontro evidencia Proc_Contrapartidas en logs locales de ${logDir}.`);
}

function extractEnvelopeNear(logText: string, expectedRequestXml: string): string | null {
  const ofIdTx = extractElementText(expectedRequestXml, 'OFIDTX');
  const marker = ofIdTx && logText.includes(ofIdTx) ? ofIdTx : 'Proc_Contrapartidas';
  const markerIndex = logText.lastIndexOf(marker);
  if (markerIndex < 0) {
    return null;
  }

  const start = Math.max(
    logText.lastIndexOf('<soap:Envelope', markerIndex),
    logText.lastIndexOf('<s:Envelope', markerIndex),
    logText.lastIndexOf('<Envelope', markerIndex)
  );
  const endCandidates = [
    logText.indexOf('</soap:Envelope>', markerIndex),
    logText.indexOf('</s:Envelope>', markerIndex),
    logText.indexOf('</Envelope>', markerIndex)
  ].filter((index) => index >= 0);

  if (start >= 0 && endCandidates.length > 0) {
    const end = Math.min(...endCandidates);
    const closeLength = logText.startsWith('</soap:Envelope>', end)
      ? '</soap:Envelope>'.length
      : logText.startsWith('</s:Envelope>', end)
        ? '</s:Envelope>'.length
        : '</Envelope>'.length;
    return logText.slice(start, end + closeLength);
  }

  return logText.slice(Math.max(0, markerIndex - 8_000), markerIndex + 8_000);
}

function extractElementText(xml: string, localName: string): string | null {
  const match = new RegExp(`<[^>]*${escapeRegExp(localName)}[^>]*>([^<]*)<\\/[^>]*${escapeRegExp(localName)}>`, 'i').exec(xml);
  return match?.[1] ?? null;
}

function sanitizeEvidence(value: string): string {
  return value
    .replace(/\b\d{10,18}\b/g, '[cuenta-redactada]')
    .replace(/\b\d{6,10}\b/g, '[id-redactado]')
    .replace(/Bearer\s+[A-Za-z0-9._-]+/gi, 'Bearer [redactado]')
    .replace(/<password>.*?<\/password>/gi, '<password>[redactado]</password>');
}

function authHeaders(token: string, json = false): HeadersInit {
  return {
    Authorization: `Bearer ${token}`,
    ...(json ? { 'Content-Type': 'application/json' } : {})
  };
}

function joinUrl(base: string, path: string): string {
  if (/^https?:\/\//i.test(path)) {
    return path;
  }

  return `${base}${path.startsWith('/') ? '' : '/'}${path}`;
}

function normalizeUrlPath(url: string): string {
  try {
    return new URL(url).pathname;
  } catch {
    return url;
  }
}

function todayIsoDate(): string {
  return new Date().toISOString().slice(0, 10);
}

function escapeRegExp(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
