import { expect, Page, test, TestInfo } from '@playwright/test';
import { existsSync, readdirSync, readFileSync, statSync } from 'node:fs';
import { join } from 'node:path';
import {
  G36RuntimeDb,
  pollUntil,
  type AchCycleSnapshot,
  type MappingRuleSnapshot
} from './support/g36-runtime-db';

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

type PagedResponse<T> = {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
};

type CustomerThirdParty = {
  id: number;
  destinationInstitutionId: number;
  destinationAccountNumber: string;
  recipientIdNumber: string;
  status: number | string;
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
test.use({ trace: 'off', screenshot: 'off', video: 'off' });
test.skip(!shouldRun, 'RUN_LOCAL_SOAP_PROC_CONTRAPARTIDAS_E2E=true y ALLOW_LOCAL_MONETARY_SOAP_E2E=true son requeridos para esta prueba local/UAT.');
test.skip(!hasRuntimeCredentials, 'ACH_USER y ACH_PASS deben venir del entorno; el spec no contiene credenciales.');
test.skip(!hasSoapLogSource, 'SOAP_LOCAL_WSCFAACH_LOG o SOAP_LOCAL_LOG_DIR es requerido para validar evidencia del SOAP local.');

const uiBaseUrl = (process.env['ACH_UI_URL'] ?? process.env['E2E_BASE_URL'] ?? 'http://localhost:743').replace(/\/+$/, '');
const apiBaseUrl = (process.env['ACH_API_URL'] ?? 'http://localhost:843').replace(/\/+$/, '');
const username = process.env['ACH_USER'] ?? '';
const password = process.env['ACH_PASS'] ?? '';
const wscfaachEndpoint = process.env['SOAP_LOCAL_WSCFAACH_URL'] ?? 'http://localhost:7083/WSCFAACH.svc';
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
  test.setTimeout(900_000);
  const startedAt = new Date();
  const db = new G36RuntimeDb(dispatchTriggeredBy);
  let originalSoapSettings: SoapIntegrationSettings | null = null;
  let cycleSnapshots: AchCycleSnapshot[] | null = null;
  let mappingSnapshot: MappingRuleSnapshot[] | null = null;

  const reference = `PW-CONTRA-${Date.now()}`;
  const sourceAccountNumber = `44${String(Date.now()).slice(-10)}`;
  const destinationAccountNumber = `55${String(Date.now()).slice(-10)}`;
  const recipientIdNumber = `70${String(Date.now()).slice(-8)}`;
  const sourceCompanyIdentification = `PW${String(Date.now()).slice(-8)}`;
  const sourceCompanyName = `PWCONTRA${String(Date.now()).slice(-6)}`;
  const collectorId = `90${String(Date.now()).slice(-11)}`;
  const receiverCustomerCode = `CLI${String(Date.now()).slice(-11)}`;
  const serviceDescription = 'SERVQA';
  const amount = 1500;

  const runtime = await authenticateThroughSpa(page);

  try {
    await db.assertReady();

    await page.goto(joinUrl(uiBaseUrl, '/transactions/create'));
    await expect(page, `La SPA debe conservar la ruta de creacion; URL actual=${page.url()}`)
      .toHaveURL(/\/transactions\/create(?:\?.*)?$/);
    await expect(page.getByRole('heading', { name: /Crear transaccion ACH|Crear transacción ACH/i })).toBeVisible();

    if (runSeed) {
      await seedDatabase(runtime.token);
    }

    cycleSnapshots = await db.loadCycleSnapshots();
    await db.configureCycles(cycleSnapshots, todayIsoDate());

    if (configureSoapSettings) {
      originalSoapSettings = await apiGetJson<SoapIntegrationSettings>(soapSettingsPath, runtime.token);
      const localSettings = buildLocalSoapSettings(originalSoapSettings);
      await apiPutJson<SoapIntegrationSettings>(soapSettingsPath, runtime.token, localSettings);
    }

    mappingSnapshot = await db.configureProcContrapartidasExpectedMapping();

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

    await activateSyntheticThirdParty(runtime.token, {
      sourceAccountNumber,
      destinationAccountNumber,
      recipientIdNumber,
      destinationInstitutionId: targetInstitution.id
    });

    await fillTransactionFormFromUi(page, {
      reference,
      amount,
      sourceAccountNumber,
      destinationAccountNumber,
      recipientIdNumber,
      collectorId,
      receiverCustomerCode,
      serviceDescription,
      sourceCompanyIdentification,
      sourceCompanyName,
      targetInstitutionName: targetInstitution.name,
      companyEntryDescriptionLabel: companyEntryDescription.term ?? companyEntryDescription.description ?? ''
    });

    const createResponsePromise = page.waitForResponse((response) =>
      response.request().method() === 'POST'
      && normalizeUrlPath(response.url()).endsWith('/transactions'));

    await page.getByRole('button', { name: /Registrar transacci[oó]n/i }).click();
    const createResponse = await createResponsePromise;
    const createResponseText = await createResponse.text();
    expect(
      createResponse.ok(),
      `POST /transactions desde la SPA debe responder OK. Status=${createResponse.status()}, body=${createResponseText}`
    ).toBeTruthy();
    const createdTransaction = JSON.parse(createResponseText) as CreatedTransaction;
    expect(createdTransaction.id, 'La transaccion monetaria creada desde UI debe devolver id.').toBeGreaterThan(0);

    await expect(page).toHaveURL(/\/transactions(?:\/list)?(?:\?.*)?$/);
    await testInfo.attach('transactions-list-after-create.png', {
      body: await page.screenshot({
        fullPage: false,
        mask: [page.locator('input'), page.locator('textarea'), page.locator('tbody')]
      }),
      contentType: 'image/png'
    });

    const transaction = await pollUntil(
      async () => db.findTransactionByExternalId(reference),
      `transaccion ${reference} creada en base`,
      120_000
    );

    expect([2, 'Debit']).toContain(transaction.type);
    expect(transaction.clearingHouseId, 'La prueba LIVE autorizada debe permanecer exclusivamente en ACH Colombia.').toBe(1);
    expect(transaction.sourceInstitutionId, 'La transaccion debe originarse desde CFA IsDefaultSource=true.').toBe(defaultSource!.id);
    expect(transaction.destinationInstitutionId, 'La entidad destino debe ser externa.').toBe(targetInstitution.id);
    expect(transaction.destinationInstitutionId).not.toBe(defaultSource!.id);

    const transactionCycle = await db.loadCycleSnapshot(transaction.achCycleId, transaction.clearingHouseId);
    await db.configureCycle(transactionCycle, transactionCycle.cycleName, todayIsoDate());

    await expect.poll(async () => db.countDispatchItems(transaction.id), {
      timeout: 120_000,
      intervals: [2_000, 5_000, 10_000]
    }).toBeGreaterThan(0);

    const dispatchResult = await apiPostJson<ContrapartidaDispatchResult>(contrapartidaDispatchPath, runtime.token, {
      cycleId: transaction.achCycleId,
      clearingHouseId: transaction.clearingHouseId,
      transactionId: transaction.id,
      triggeredBy: dispatchTriggeredBy,
      chunkSize: 50
    }, {
      'X-UAT-Transaction-Nacha-Dispatch': 'true'
    });

    expect(dispatchResult.cycleId).toBe(transaction.achCycleId);
    expect(dispatchResult.clearingHouseId).toBe(transaction.clearingHouseId);
    expect(dispatchResult.processed, 'El dispatch UAT dirigido debe procesar solamente la transaccion sintetica.').toBe(1);

    const evidence = await pollUntil(
      async () => db.findDispatchEvidence(reference),
      `evidencia Proc_Contrapartidas para ${reference}`,
      180_000
    );

    assertProcContrapartidasPayload(evidence.requestPayloadXml, transaction.clearingHouseId);
    expect(evidence.requestPayloadXml).not.toContain('Proc_Transacciones');
    expect(evidence.requestPayloadXml).not.toContain('RegistrarRespuestaTransaccion');
    expect(evidence.requestPayloadXml).not.toMatch(/<[^>]*METODO[^>]*>/i);

    expect(evidence.responsePayloadXml, 'Debe persistirse la respuesta inbound del SOAP, no solo el request outbound.').toBeTruthy();
    expect(evidence.responsePayloadXml.trim().length, 'La respuesta SOAP persistida no puede estar vacia.').toBeGreaterThan(0);
    expect(evidence.soapMethodName, 'El intento debe persistir el metodo SOAP ejecutado.').toBe('Proc_Contrapartidas');
    expect(evidence.executionMode, 'El intento debe persistir modo Live.').toMatch(/^Live$/i);
    expect(evidence.soapEndpoint ?? '', 'El intento debe persistir el endpoint WSCFAACH usado.').toContain('WSCFAACH.svc');
    expect(Number(evidence.durationMs ?? 0), 'El intento debe persistir duracion aproximada.').toBeGreaterThanOrEqual(0);

    expect(evidence.externalResponseCode ?? evidence.errorCode ?? '', [
      'El backend debe estar en modo live para esta prueba.',
      'Si aparece PROC_DRY_RUN o PROC_DISABLED, arranque la API con ProcContrapartidas__Mode=Live solo en local/UAT autorizado.'
    ].join(' ')).not.toMatch(/PROC_DRY_RUN|PROC_DISABLED/i);
    expect(evidence.soapResponseCode ?? '', 'El codigo SOAP normalizado no debe indicar dry-run/disabled en live.').not.toMatch(/PROC_DRY_RUN|PROC_DISABLED/i);
    expect(evidence.soapTechnicalStatus ?? '', 'El estado tecnico SOAP debe quedar persistido.').toMatch(/Succeeded|FunctionalRejection|SoapFault|RetryableFailure|ParserError|TechnicalException|UnknownFailure/i);
    expect(evidence.soapResponseCode ?? evidence.externalResponseCode ?? evidence.errorCode ?? '', 'Debe existir codigo funcional/tecnico normalizado consultable.').toBeTruthy();

    const persistedCode = evidence.soapResponseCode ?? '';
    const legacyCode = evidence.externalResponseCode ?? evidence.errorCode ?? '';
    if (persistedCode && legacyCode) {
      expect(persistedCode, 'SoapResponseCode debe corresponder al codigo persistido legacy del intento.').toBe(legacyCode);
    }

    if (persistedCode && !/UNKNOWN|SOAP_EXCEPTION|PARSER_ERROR|EMPTY_RESPONSE/i.test(persistedCode)) {
      expect(evidence.responsePayloadXml, 'La respuesta persistida debe contener el codigo SOAP normalizado cuando el legacy lo entrega explicitamente.')
        .toContain(persistedCode);
    }

    if (asBoolean(evidence.isFunctionalRejection)) {
      expect(asBoolean(evidence.isTechnicalFailure), 'Un rechazo funcional no debe marcarse como falla tecnica.').toBeFalsy();
    }

    if (asBoolean(evidence.isTechnicalFailure)) {
      expect(evidence.soapTechnicalStatus ?? '', 'Una falla tecnica debe tener estado tecnico explicito.').toMatch(/SoapFault|RetryableFailure|ParserError|TechnicalException|UnknownFailure/i);
    }

    const localSoapEvidence = readLocalSoapEvidence(startedAt, evidence.requestPayloadXml);
    const legacyLogFragment = extractEnvelopeNear(localSoapEvidence.text, evidence.requestPayloadXml) ?? localSoapEvidence.text;
    expect(legacyLogFragment, 'El log plano del SOAP local debe contener evidencia de Proc_Contrapartidas.').toContain('Proc_Contrapartidas');
    expect(legacyLogFragment).toContain('OFNIT');
    expect(legacyLogFragment).toContain('OFCTA');
    expect(legacyLogFragment).toContain('OFMONDEB');
    expect(legacyLogFragment).toContain('OFIDCAMCOMPE');
    expect(legacyLogFragment).toContain('OFFECHEFEC');
    expect(legacyLogFragment).not.toContain('Proc_Transacciones');

    await testInfo.attach('proc-contrapartidas-request-sanitized.xml', {
      body: sanitizeEvidence(evidence.requestPayloadXml),
      contentType: 'application/xml'
    });
    await testInfo.attach('proc-contrapartidas-response-sanitized.xml', {
      body: sanitizeEvidence(evidence.responsePayloadXml),
      contentType: 'application/xml'
    });
    await testInfo.attach('proc-contrapartidas-local-soap-log-sanitized.txt', {
      body: [
        `source=${localSoapEvidence.source}`,
        'nota=El SOAP legacy puede registrar <METODO> en su envelope interno de trazabilidad; la ausencia de <METODO> se valida en el request outbound persistido por ACHInterbank.',
        '',
        sanitizeEvidence(legacyLogFragment)
      ].join('\n'),
      contentType: 'text/plain'
    });
  } finally {
    if (cycleSnapshots) {
      await db.restoreCycles(cycleSnapshots);
    }

    if (originalSoapSettings) {
      await apiPutJson<SoapIntegrationSettings>(soapSettingsPath, runtime.token, originalSoapSettings);
    }

    if (mappingSnapshot) {
      await db.restoreProcContrapartidasMapping(mappingSnapshot);
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
  collectorId: string;
  receiverCustomerCode: string;
  serviceDescription: string;
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
  await fillInput(page, 'NIT/EAN-13 recaudador', data.collectorId);
  await fillInput(page, 'Código cliente receptor', data.receiverCustomerCode);
  await fillInput(page, 'Servicio', data.serviceDescription);
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
  const labelPattern = new RegExp(`^${escapeRegExp(labelText)}(?:\\s*\\([^)]*\\))?(?:\\s*\\*)?$`, 'i');
  return page.locator('label')
    .filter({ has: page.locator('span').filter({ hasText: labelPattern }) })
    .first();
}

async function authenticateThroughSpa(page: Page): Promise<{ token: string }> {
  await page.goto(joinUrl(uiBaseUrl, '/login'));
  await expect(page.getByRole('heading', { name: 'Ingreso al portal ACH Interbank' })).toBeVisible();

  await page.getByLabel('Usuario').fill(username);
  await page.locator('input[formcontrolname="password"]').fill(password);
  const loginResponsePromise = page.waitForResponse((response) =>
    response.request().method() === 'POST'
    && normalizeUrlPath(response.url()).endsWith(loginPath));

  await page.getByRole('button', { name: 'Ingresar', exact: true }).click();
  const loginResponse = await loginResponsePromise;
  expect(loginResponse.ok(), `Login SPA local debe responder OK. Status=${loginResponse.status()}`).toBeTruthy();
  const payload = await loginResponse.json() as AuthLoginResponse;
  const token = payload.data?.token;
  expect(token, 'El login SPA debe devolver access token.').toBeTruthy();
  await expect(page).not.toHaveURL(/\/login(?:\?.*)?$/);

  const storedToken = await page.evaluate(() => window.sessionStorage.getItem('ach.interbank.access_token'));
  expect(storedToken, 'La SPA debe persistir la sesion autenticada en sessionStorage.').toBe(token);
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

async function apiPatchJson<T>(path: string, token: string, body: unknown): Promise<T> {
  const response = await fetch(joinUrl(apiBaseUrl, path), {
    method: 'PATCH',
    headers: authHeaders(token, true),
    body: JSON.stringify(body)
  });

  if (!response.ok) {
    throw new Error(`PATCH ${path} debe responder 200. Status=${response.status}, body=${await response.text()}`);
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

async function activateSyntheticThirdParty(token: string, request: {
  sourceAccountNumber: string;
  destinationAccountNumber: string;
  recipientIdNumber: string;
  destinationInstitutionId: number;
}): Promise<void> {
  const query = new URLSearchParams({
    sourceAccountNumber: request.sourceAccountNumber,
    destinationAccountNumber: request.destinationAccountNumber,
    recipientIdNumber: request.recipientIdNumber,
    destinationInstitutionId: String(request.destinationInstitutionId),
    page: '1',
    pageSize: '20'
  });

  const thirdParty = await pollUntil(async () => {
    const page = await apiGetJson<PagedResponse<CustomerThirdParty>>(`/api/customer-third-parties?${query}`, token);
    return page.items.find((item) =>
      item.destinationInstitutionId === request.destinationInstitutionId
      && item.destinationAccountNumber === request.destinationAccountNumber
      && item.recipientIdNumber === request.recipientIdNumber
    );
  }, `tercero sintetico ${request.destinationAccountNumber}`, 60_000);

  await apiPatchJson<CustomerThirdParty>(`/api/customer-third-parties/${thirdParty.id}/status`, token, {
    status: 1,
    validationMessage: 'Playwright local SOAP Proc_Contrapartidas synthetic approval'
  });
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
  for (const mapping of cloned.wscfaachMappings) {
    if (mapping.methodName.toLowerCase() !== 'proc_contrapartidas') {
      mapping.enabled = false;
    }
  }
  for (const mapping of cloned.wsAxonRespuestaTransaccionesMappings) {
    mapping.enabled = false;
  }
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
    'OFIDCAMCOMPE'
  ]) {
    expect(xml, `El request debe contener ${field}.`).toContain(field);
  }

  expect(xml, 'OFDD observado debe ser TRANSFER con espacios finales.').toMatch(/<[^>]*OFDD[^>]*>TRANSFER\s{2}<\/[^>]*OFDD>/);
  expect(xml, 'OFFECHEFEC debe viajar en formato yyyyMMdd.').toMatch(/<[^>]*OFFECHEFEC[^>]*>\d{8}<\/[^>]*OFFECHEFEC>/);
  expect(xml, 'OFMONCRE observado debe ser 0.').toMatch(/<[^>]*OFMONCRE[^>]*>0(?:\.0+)?<\/[^>]*OFMONCRE>/);
  expect(xml, 'OFST observado debe ser OO.').toMatch(/<[^>]*OFST[^>]*>OO<\/[^>]*OFST>/);
  expect(xml, 'OFIDTX observado debe ser 0.').toMatch(/<[^>]*OFIDTX[^>]*>0<\/[^>]*OFIDTX>/);
  expect(xml, 'OFIDREVER observado debe ser 0.').toMatch(/<[^>]*OFIDREVER[^>]*>0<\/[^>]*OFIDREVER>/);
  expect(xml, 'OFIDEBAPLI observado debe ser 1.').toMatch(/<[^>]*OFIDEBAPLI[^>]*>1<\/[^>]*OFIDEBAPLI>/);

  if (clearingHouseId === 1) {
    expect(xml, 'ACH Colombia debe enviar OFIDCAMCOMPE=1.').toMatch(/<[^>]*OFIDCAMCOMPE[^>]*>1<\/[^>]*OFIDCAMCOMPE>/);
  } else if (clearingHouseId === 2) {
    expect(xml, 'CENIT debe enviar OFIDCAMCOMPE=2.').toMatch(/<[^>]*OFIDCAMCOMPE[^>]*>2<\/[^>]*OFIDCAMCOMPE>/);
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

  const expectedMarkers = [
    extractElementText(expectedRequestXml, 'OFCTA'),
    extractElementText(expectedRequestXml, 'OFNIT'),
    extractElementText(expectedRequestXml, 'OFIDLOT'),
    extractElementText(expectedRequestXml, 'OFIDTX')
  ].filter((value): value is string => Boolean(value && value.trim() && value.trim() !== '0'));

  for (const candidate of candidates) {
    const text = readFileSync(candidate, 'utf8');
    if (text.includes('Proc_Contrapartidas') && expectedMarkers.some((marker) => text.includes(marker))) {
      return { source: candidate, text };
    }
  }

  throw new Error(`No se encontro evidencia Proc_Contrapartidas en logs locales de ${logDir}.`);
}

function extractEnvelopeNear(logText: string, expectedRequestXml: string): string | null {
  const markers = [
    extractElementText(expectedRequestXml, 'OFCTA'),
    extractElementText(expectedRequestXml, 'OFNIT'),
    extractElementText(expectedRequestXml, 'OFIDLOT'),
    extractElementText(expectedRequestXml, 'OFIDTX'),
    'Proc_Contrapartidas'
  ].filter((value): value is string => Boolean(value && value.trim() && value.trim() !== '0'));
  const marker = markers.find((value) => logText.includes(value)) ?? 'Proc_Contrapartidas';
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

function asBoolean(value: boolean | number | string | null | undefined): boolean {
  return value === true || value === 1 || value === '1' || String(value).toLowerCase() === 'true';
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
