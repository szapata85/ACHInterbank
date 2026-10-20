import { expect, Page, test, TestInfo } from '@playwright/test';
import { existsSync, mkdirSync, readdirSync, readFileSync, statSync, writeFileSync } from 'node:fs';
import { join, resolve } from 'node:path';
import {
  G36RuntimeDb,
  pollUntil
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

type TransactionIntegrationResult = {
  transactionId: number;
  latest?: {
    catalogId?: number | null;
    method: string;
    transportStatus: string;
    businessStatus: string;
    responseCode: string;
    responseDescription: string;
    retryAllowed: boolean;
    requiresManualReview: boolean;
  } | null;
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
  operatingMode: string;
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
  process.env['ACH_SOAP_LIVE_TESTS'] === 'true'
  && process.env['RUN_LOCAL_SOAP_PROC_CONTRAPARTIDAS_E2E'] === 'true'
  && process.env['ALLOW_LOCAL_MONETARY_SOAP_E2E'] === 'true';

const hasRuntimeCredentials = Boolean(process.env['ACH_USER'] && process.env['ACH_PASS']);
const hasSoapLogSource = Boolean(process.env['SOAP_LOCAL_WSCFAACH_LOG'] || process.env['SOAP_LOCAL_LOG_DIR']);

test.describe.configure({ mode: 'serial' });
test.use({ trace: 'on', screenshot: 'only-on-failure', video: 'retain-on-failure' });
test.skip(!shouldRun, 'ACH_SOAP_LIVE_TESTS=true, RUN_LOCAL_SOAP_PROC_CONTRAPARTIDAS_E2E=true y ALLOW_LOCAL_MONETARY_SOAP_E2E=true son requeridos para esta prueba local/UAT.');
test.skip(!hasRuntimeCredentials, 'ACH_USER y ACH_PASS deben venir del entorno; el spec no contiene credenciales.');
test.skip(!hasSoapLogSource, 'SOAP_LOCAL_WSCFAACH_LOG o SOAP_LOCAL_LOG_DIR es requerido para validar evidencia del SOAP local.');

const uiBaseUrl = (process.env['ACH_UI_URL'] ?? process.env['E2E_BASE_URL'] ?? 'http://localhost:743').replace(/\/+$/, '');
const apiBaseUrl = (process.env['ACH_API_URL'] ?? 'http://localhost:843').replace(/\/+$/, '');
const username = process.env['ACH_USER'] ?? '';
const password = process.env['ACH_PASS'] ?? '';
const wscfaachEndpoint = process.env['SOAP_LOCAL_WSCFAACH_URL'] ?? 'http://localhost:7083/WSCFAACH.svc';
const runSeed = process.env['PROC_CONTRA_RUN_SEED'] === 'true';
const targetInstitutionName = process.env['PROC_CONTRA_DESTINATION_INSTITUTION_NAME'] ?? '';
const dispatchTriggeredBy = username;
const liveResultFile = process.env['PROC_CONTRA_RESULT_FILE'] ?? '';
const resumeReadOnly = process.env['PROC_CONTRA_RESUME_READ_ONLY'] === 'true';
const resumeCreatedDispatch = process.env['PROC_CONTRA_RESUME_CREATED_DISPATCH'] === 'true';
const evidenceDir = resolve(process.cwd(), '..', '..', 'docs', 'uat', 'evidencias', 'transactions-create');
mkdirSync(evidenceDir, { recursive: true });

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
  let schedulerPausedByTest = false;
  let singleDispatchCompleted = false;

  if (resumeCreatedDispatch) {
    await dispatchPersistedCreatedTransaction(page, testInfo, db);
    return;
  }

  if (resumeReadOnly) {
    await validatePersistedLiveResultWithoutDispatch(page, testInfo, db);
    return;
  }

  const reference = `PW-CONTRA-${Date.now()}`;
  const sourceAccountNumber = process.env['PROC_CONTRA_SOURCE_ACCOUNT'] ?? `44${String(Date.now()).slice(-10)}`;
  const destinationAccountNumber = process.env['PROC_CONTRA_DESTINATION_ACCOUNT'] ?? `55${String(Date.now()).slice(-10)}`;
  const recipientIdNumber = process.env['PROC_CONTRA_RECIPIENT_ID'] ?? `70${String(Date.now()).slice(-8)}`;
  const hasApprovedOnboardingContext =
    Boolean(process.env['PROC_CONTRA_SOURCE_ACCOUNT'])
    && Boolean(process.env['PROC_CONTRA_DESTINATION_ACCOUNT'])
    && Boolean(process.env['PROC_CONTRA_RECIPIENT_ID']);
  const sourceCompanyIdentification =
    process.env['PROC_CONTRA_SOURCE_IDENTIFICATION'] ?? `PW${String(Date.now()).slice(-8)}`;
  const sourceCompanyName =
    process.env['PROC_CONTRA_SOURCE_NAME'] ?? `PWCONTRA${String(Date.now()).slice(-6)}`;
  const collectorId = `90${String(Date.now()).slice(-11)}`;
  const receiverCustomerCode = `CLI${String(Date.now()).slice(-11)}`;
  const serviceDescription = 'SERVQA';
  const amount = 1500;

  const runtime = await authenticateThroughSpa(page);

  try {
    await db.assertReady();
    schedulerPausedByTest = await pauseAutomaticContrapartidaDispatch(page);

    await page.goto(joinUrl(uiBaseUrl, '/transactions/create'));
    await expect(page, `La SPA debe conservar la ruta de creacion; URL actual=${page.url()}`)
      .toHaveURL(/\/transactions\/create(?:\?.*)?$/);
    await expect(page.getByRole('heading', { name: /Crear transaccion ACH|Crear transacción ACH/i })).toBeVisible();

    if (runSeed) {
      await seedDatabase(runtime.token);
    }

    const persistedSoapSettings = await apiGetJson<SoapIntegrationSettings>(soapSettingsPath, runtime.token);
    assertLiveLocalSoapSettings(persistedSoapSettings);

    const institutions = await apiGetJson<FinancialInstitution[]>(financialInstitutionsPath, runtime.token);
    const defaultSource = institutions.find((item) => item.isDefaultSource) ?? null;
    expect(defaultSource, 'Debe existir una entidad financiera CFA con IsDefaultSource=true.').not.toBeNull();

    const targetInstitution = resolveTargetInstitution(institutions, defaultSource!.id);
    expect(targetInstitution.isDefaultSource, 'La entidad destino debe ser externa, no CFA.').not.toBeTruthy();

    const companyEntryDescription = await resolveCompanyEntryDescription(runtime.token);

    expect(
      hasApprovedOnboardingContext,
      'La prueba LIVE exige cuentas y receptor aprobados previamente por el onboarding UI; no crea prenotificaciones ni activa terceros por API.'
    ).toBeTruthy();

    // El catálogo de cuentas se obtiene al construir el formulario. La
    // aprobación anterior debe reflejarse mediante el flujo normal de carga,
    // no inyectando la opción en el DOM.
    await page.reload();
    await expect(page).toHaveURL(/\/transactions\/create(?:\?.*)?$/);

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

    const transactionPosts: Array<Record<string, unknown>> = [];
    page.on('request', (request) => {
      if (request.method() === 'POST' && normalizeUrlPath(request.url()).endsWith('/transactions')) {
        transactionPosts.push(request.postDataJSON() as Record<string, unknown>);
      }
    });

    const createResponsePromise = page.waitForResponse((response) =>
      response.request().method() === 'POST'
      && normalizeUrlPath(response.url()).endsWith('/transactions'));

    await page.getByRole('button', { name: /Crear transacción/i }).click();
    const createResponse = await createResponsePromise;
    const createResponseText = await createResponse.text();
    expect(
      createResponse.ok(),
      `POST /transactions desde la SPA debe responder OK. Status=${createResponse.status()}, body=${createResponseText}`
    ).toBeTruthy();
    const createdTransaction = JSON.parse(createResponseText) as CreatedTransaction;
    expect(createdTransaction.id, 'La transaccion monetaria creada desde UI debe devolver id.').toBeGreaterThan(0);
    expect(transactionPosts, 'La acción final debe producir un único POST de creación.').toHaveLength(1);
    assertNoLegacyReferencePayload(transactionPosts[0]);
    writeLiveState({ transactionId: createdTransaction.id, phase: 'created' });

    await expect(page).toHaveURL(/\/transactions(?:\/list)?(?:\?.*)?$/);
    await testInfo.attach('transactions-list-after-create.png', {
      body: await page.screenshot({
        fullPage: false,
        mask: [page.locator('input'), page.locator('textarea'), page.locator('tbody')]
      }),
      contentType: 'image/png'
    });
    await page.screenshot({
      path: resolve(evidenceDir, 'transaccion-live-creada.png'),
      fullPage: false,
      mask: [page.locator('input'), page.locator('textarea'), page.locator('tbody')]
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

    await expect.poll(async () => db.countDispatchItems(transaction.id), {
      timeout: 120_000,
      intervals: [2_000, 5_000, 10_000]
    }).toBeGreaterThan(0);

    const dispatchResult = await apiPostJson<ContrapartidaDispatchResult>(contrapartidaDispatchPath, runtime.token, {
      transactionId: transaction.id
    });

    expect(dispatchResult.cycleId).toBe(transaction.achCycleId);
    expect(dispatchResult.clearingHouseId).toBe(transaction.clearingHouseId);
    expect(dispatchResult.processed, 'El dispatch UAT dirigido debe procesar solamente la transaccion sintetica.').toBe(1);

    const evidence = await pollUntil(
      async () => db.findDispatchEvidence(transaction.id),
      `evidencia Proc_Contrapartidas para TransactionId ${transaction.id}`,
      180_000
    );
    expect(Number(evidence.transactionId)).toBe(transaction.id);
    expect(evidence.attemptId).toBeTruthy();
    expect(evidence.dispatchItemId).toBeTruthy();
    expect(evidence.dispatchBatchId).toBeTruthy();
    writeLiveState({
      transactionId: transaction.id,
      attemptId: evidence.attemptId,
      dispatchItemId: evidence.dispatchItemId,
      dispatchBatchId: evidence.dispatchBatchId,
      responseCatalogId: evidence.responseCatalogId,
      phase: 'persisted'
    });

    assertProcContrapartidasPayload(evidence.requestPayloadXml, transaction.clearingHouseId);
    expect(evidence.requestPayloadXml).not.toContain('Proc_Transacciones');
    expect(evidence.requestPayloadXml).not.toContain('RegistrarRespuestaTransaccion');
    expect(evidence.requestPayloadXml).not.toMatch(/<[^>]*METODO[^>]*>/i);

    expect(evidence.responsePayloadXml, 'Debe persistirse la respuesta inbound del SOAP, no solo el request outbound.').toBeTruthy();
    expect(evidence.responsePayloadXml.trim().length, 'La respuesta SOAP persistida no puede estar vacia.').toBeGreaterThan(0);
    expect(evidence.soapMethodName, 'El intento debe persistir el metodo SOAP ejecutado.').toBe('Proc_Contrapartidas');
    expect(Number(evidence.responseCatalogId ?? 0), 'R96 debe quedar relacionado con el catalogo parametrizado.').toBeGreaterThan(0);
    expect(evidence.transportStatus, 'El transporte SOAP real local debe quedar exitoso.').toBe('Succeeded');
    expect(evidence.businessStatus, 'R96 debe resolverse como exito funcional desde el catalogo.').toBe('Success');
    expect(evidence.soapResponseCode).toBe('R96');
    expect(evidence.soapResponseDescription).toBe('Débito aplicado correctamente');
    expect(asBoolean(evidence.retryAllowed), 'R96 no debe permitir reintento.').toBeFalsy();
    expect(asBoolean(evidence.requiresManualReview), 'R96 no debe requerir revision manual.').toBeFalsy();
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
    if (localSoapEvidence) {
      const legacyLogFragment = extractEnvelopeNear(localSoapEvidence.text, evidence.requestPayloadXml) ?? localSoapEvidence.text;
      expect(legacyLogFragment, 'El log plano del SOAP local debe contener evidencia de Proc_Contrapartidas.').toContain('Proc_Contrapartidas');
      expect(legacyLogFragment).toContain('OFNIT');
      expect(legacyLogFragment).toContain('OFCTA');
      expect(legacyLogFragment).toContain('OFMONDEB');
      expect(legacyLogFragment).toContain('OFIDCAMCOMPE');
      expect(legacyLogFragment).toContain('OFFECHEFEC');
      expect(legacyLogFragment).not.toContain('Proc_Transacciones');
      await testInfo.attach('proc-contrapartidas-local-soap-log-summary.txt', {
        body: `evidence=correlated\nsource=${localSoapEvidence.source}\nmethod=Proc_Contrapartidas\n`,
        contentType: 'text/plain'
      });
    } else {
      await testInfo.attach('proc-contrapartidas-local-soap-log-gap.txt', {
        body: 'evidence=not-correlated\nreason=no matching append in the configured local WCF log window\n',
        contentType: 'text/plain'
      });
    }

    const integrationResult = await apiGetJson<TransactionIntegrationResult>(
      `${transactionsPath}/${transaction.id}/integration-result`,
      runtime.token
    );
    expect(integrationResult.transactionId).toBe(transaction.id);
    expect(Number(integrationResult.latest?.catalogId ?? 0)).toBeGreaterThan(0);
    expect(integrationResult.latest?.method).toBe('Proc_Contrapartidas');
    expect(integrationResult.latest?.transportStatus).toBe('Succeeded');
    expect(integrationResult.latest?.businessStatus).toBe('Success');
    expect(integrationResult.latest?.responseCode).toBe('R96');
    expect(integrationResult.latest?.responseDescription).toBe('Débito aplicado correctamente');
    expect(integrationResult.latest?.retryAllowed).toBeFalsy();
    expect(integrationResult.latest?.requiresManualReview).toBeFalsy();

    await page.goto(joinUrl(uiBaseUrl, '/transactions'));
    const transactionGrid = page.locator('ui-grilla-empresarial').filter({ hasText: reference }).first();
    await expect(transactionGrid, 'La transaccion sintetica debe estar visible en la grilla.').toBeVisible();
    await transactionGrid.locator('.ag-row').filter({ hasText: reference }).first().click();
    const resultPanel = page.locator('app-transaction-integration-result');
    await expect(resultPanel.getByRole('heading', { name: 'RESULTADO DEL PROCESAMIENTO EN EL CORE' })).toBeVisible();
    await expect(resultPanel).toContainText('Proc_Contrapartidas');
    await expect(resultPanel).toContainText('Exitoso');
    await expect(resultPanel).toContainText('R96');
    await expect(resultPanel).toContainText('Débito aplicado correctamente');
    await expect(resultPanel).not.toContainText(/<Envelope|<soap|RequestPayload|ResponsePayload|\{\s*"/i);
    await testInfo.attach('proc-contrapartidas-r96-panel.png', {
      body: await resultPanel.screenshot(),
      contentType: 'image/png'
    });
    await resultPanel.screenshot({
      path: resolve(evidenceDir, 'resultado-proc-contrapartidas.png')
    });

    expect(await db.countDispatchAttempts(transaction.id), 'La transaccion nueva debe tener un unico intento SOAP persistido.').toBe(1);
    writeLiveState({
      transactionId: transaction.id,
      attemptId: evidence.attemptId,
      dispatchItemId: evidence.dispatchItemId,
      dispatchBatchId: evidence.dispatchBatchId,
      responseCatalogId: evidence.responseCatalogId,
      phase: 'single-dispatch-complete'
    });
    singleDispatchCompleted = true;
  } finally {
    if (schedulerPausedByTest && singleDispatchCompleted) {
      await resumeAutomaticContrapartidaDispatch(page);
    }
    await db.close();
  }
});

async function pauseAutomaticContrapartidaDispatch(page: Page): Promise<boolean> {
  await page.goto(joinUrl(uiBaseUrl, '/scheduler/tasks'));
  const row = page.getByRole('region', { name: 'Tareas' })
    .getByRole('row')
    .filter({ hasText: 'CONTRAPARTIDA_DISPATCH' });
  await expect(row, 'Debe existir la tarea automática de Proc_Contrapartidas.').toHaveCount(1);
  await row.getByLabel('Abrir acciones').click();
  if (await row.getByRole('button', { name: 'Reanudar', exact: true }).count()) {
    // Una ejecución previa de este mismo spec puede haberla dejado pausada al
    // detenerse antes del límite SOAP. El baseline en cero se valida antes de
    // reanudar el intento y el cierre definitivo debe restaurar la tarea.
    return true;
  }

  const pauseResponse = page.waitForResponse((response) =>
    response.request().method() === 'POST'
    && new URL(response.url()).pathname.toLowerCase().endsWith('/scheduler/tasks/contrapartida_dispatch/pause')
  );
  await row.getByRole('button', { name: 'Pausar', exact: true }).click();
  expect((await pauseResponse).ok(), 'La tarea automática debe pausarse para garantizar un único dispatch dirigido.').toBeTruthy();
  return true;
}

async function resumeAutomaticContrapartidaDispatch(page: Page): Promise<void> {
  await page.goto(joinUrl(uiBaseUrl, '/scheduler/tasks'));
  const row = page.getByRole('region', { name: 'Tareas' })
    .getByRole('row')
    .filter({ hasText: 'CONTRAPARTIDA_DISPATCH' });
  await expect(row).toHaveCount(1);
  await row.getByLabel('Abrir acciones').click();
  const resumeResponse = page.waitForResponse((response) =>
    response.request().method() === 'POST'
    && new URL(response.url()).pathname.toLowerCase().endsWith('/scheduler/tasks/contrapartida_dispatch/resume')
  );
  await row.getByRole('button', { name: 'Reanudar', exact: true }).click();
  expect((await resumeResponse).ok(), 'La programación automática debe restaurarse después del resultado definitivo.').toBeTruthy();
}

async function validatePersistedLiveResultWithoutDispatch(
  page: Page,
  testInfo: TestInfo,
  db: G36RuntimeDb
): Promise<void> {
  expect(liveResultFile, 'PROC_CONTRA_RESULT_FILE es obligatorio para reanudar sin dispatch.').toBeTruthy();
  expect(existsSync(liveResultFile), 'El estado opaco del dispatch anterior debe existir.').toBeTruthy();
  const state = JSON.parse(readFileSync(liveResultFile, 'utf8')) as { transactionId?: number };
  const transactionId = Number(state.transactionId ?? 0);
  expect(transactionId).toBeGreaterThan(0);

  const runtime = await authenticateThroughSpa(page);
  try {
    await db.assertReady();
    const transaction = await db.findTransactionById(transactionId);
    expect(transaction, 'La transaccion del unico dispatch debe seguir persistida.').not.toBeNull();
    const evidence = await db.findDispatchEvidence(transactionId);
    expect(evidence, 'La evidencia del unico intento debe seguir persistida.').not.toBeNull();
    expect(Number(evidence!.transactionId)).toBe(transactionId);
    expect(evidence!.soapMethodName).toBe('Proc_Contrapartidas');
    expect(evidence!.soapResponseCode).toBe('R96');
    expect(evidence!.soapResponseDescription).toBe('Débito aplicado correctamente');
    expect(evidence!.transportStatus).toBe('Succeeded');
    expect(evidence!.businessStatus).toBe('Success');
    expect(Number(evidence!.responseCatalogId ?? 0)).toBeGreaterThan(0);
    expect(asBoolean(evidence!.retryAllowed)).toBeFalsy();
    expect(asBoolean(evidence!.requiresManualReview)).toBeFalsy();
    expect(evidence!.requestPayloadXml).not.toMatch(/<[^>]*METODO[^>]*>/i);
    expect(evidence!.requestPayloadXml).not.toContain('Proc_Transacciones');
    expect(evidence!.requestPayloadXml).not.toContain('RegistrarRespuestaTransaccion');
    expect(await db.countDispatchAttempts(transactionId)).toBe(1);

    const integrationResult = await apiGetJson<TransactionIntegrationResult>(
      `${transactionsPath}/${transactionId}/integration-result`,
      runtime.token
    );
    expect(integrationResult.latest?.method).toBe('Proc_Contrapartidas');
    expect(integrationResult.latest?.responseCode).toBe('R96');
    expect(integrationResult.latest?.responseDescription).toBe('Débito aplicado correctamente');
    expect(integrationResult.latest?.transportStatus).toBe('Succeeded');
    expect(integrationResult.latest?.businessStatus).toBe('Success');

    await page.goto(joinUrl(uiBaseUrl, '/transactions'));
    const transactionGrid = page.locator('ui-grilla-empresarial').filter({ hasText: transaction!.transactionExternalId }).first();
    await expect(transactionGrid).toBeVisible();
    await transactionGrid.locator('.ag-row').filter({ hasText: transaction!.transactionExternalId }).first().click();
    const resultPanel = page.locator('app-transaction-integration-result');
    await expect(resultPanel.getByRole('heading', { name: 'RESULTADO DEL PROCESAMIENTO EN EL CORE' })).toBeVisible();
    await expect(resultPanel).toContainText('Proc_Contrapartidas');
    await expect(resultPanel).toContainText('Exitoso');
    await expect(resultPanel).toContainText('R96');
    await expect(resultPanel).toContainText('D\u00e9bito aplicado correctamente');
    await expect(resultPanel).not.toContainText(/<Envelope|<soap|RequestPayload|ResponsePayload|\{\s*"/i);
    await testInfo.attach('proc-contrapartidas-r96-panel-resume.png', {
      body: await resultPanel.screenshot(),
      contentType: 'image/png'
    });
    await resultPanel.screenshot({
      path: resolve(evidenceDir, 'resultado-proc-contrapartidas.png')
    });

    const localSoapEvidence = readLocalSoapEvidence(new Date(0), evidence!.requestPayloadXml);
    expect(localSoapEvidence, 'La evidencia WCF debe correlacionar con el request persistido.').not.toBeNull();
    const fragment = extractEnvelopeNear(localSoapEvidence!.text, evidence!.requestPayloadXml) ?? '';
    expect(fragment).toContain('Proc_Contrapartidas');
    expect(fragment).not.toContain('Proc_Transacciones');
    expect(fragment).not.toContain('RegistrarRespuestaTransaccion');
    expect(fragment).not.toContain('PLValidarUsuarioBV');

    writeLiveState({
      transactionId,
      phase: 'complete'
    });
  } finally {
    await db.close();
  }
}

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
  await expect(page.getByText('Referencia legado')).toHaveCount(0);
  await fillInput(page, 'Valor de la transacción', String(data.amount));
  await fillInput(page, 'ID de operación del cliente', data.reference);
  await selectOption(page, 'Tipo de operación', 'Débito');
  await selectOption(page, 'Tipo de cuenta destino', 'Cuenta corriente');
  const thirdPartiesResponse = page.waitForResponse((response) => {
    if (response.request().method() !== 'GET') {
      return false;
    }

    const url = new URL(response.url());
    return url.pathname.toLowerCase().endsWith('/api/customer-third-parties')
      && url.searchParams.get('sourceAccountNumber') === data.sourceAccountNumber;
  });
  await fillInput(page, 'Número de cuenta de origen', data.sourceAccountNumber);
  expect((await thirdPartiesResponse).ok(), 'El catálogo real de terceros activos debe cargar desde el API.').toBeTruthy();
  await selectOption(page, 'Tipo de persona del originador', 'Persona jurídica');
  await fillInput(page, 'Número de identificación del originador', data.sourceCompanyIdentification);
  await fillInput(page, 'Nombre o razón social del originador', data.sourceCompanyName.slice(0, 16));
  await selectOption(page, 'Entidad financiera destino', data.targetInstitutionName);
  await selectOption(page, 'Número de cuenta destino', data.destinationAccountNumber);
  await selectOption(page, 'Tipo de identificación del receptor', 'Persona jurídica');
  await fillInput(page, 'Número de identificación del receptor', data.recipientIdNumber);
  await fillInput(page, 'Nombre o razón social del receptor', 'RECEPTOR QA CONTRA');
  await fillInput(page, 'Código del recaudador', data.collectorId);
  await fillInput(page, 'Código de cliente del receptor', data.receiverCustomerCode);
  await fillInput(page, 'Descripción del servicio', data.serviceDescription);
  await selectOption(page, 'Descripción de la entrada', data.companyEntryDescriptionLabel || 'NOMINAS');
  await selectOption(page, 'Tipo de addenda', '05 · Información adicional');
  await fillInput(page, 'Información adicional', `${data.reference}-ADDENDA`);
}

async function dispatchPersistedCreatedTransaction(
  page: Page,
  testInfo: TestInfo,
  db: G36RuntimeDb
): Promise<void> {
  expect(liveResultFile, 'PROC_CONTRA_RESULT_FILE es obligatorio para reanudar la transacción creada.').toBeTruthy();
  expect(existsSync(liveResultFile), 'El estado de la única transacción creada debe existir.').toBeTruthy();
  const state = JSON.parse(readFileSync(liveResultFile, 'utf8')) as { transactionId?: number; phase?: string };
  const transactionId = Number(state.transactionId ?? 0);
  expect(transactionId).toBeGreaterThan(0);
  expect(state.phase).toBe('created');

  const runtime = await authenticateThroughSpa(page);
  await db.assertReady();
  const transaction = await db.findTransactionById(transactionId);
  expect(transaction, 'La transacción creada desde la SPA debe continuar persistida.').not.toBeNull();
  expect(await db.countDispatchItems(transactionId), 'Debe existir un único elemento de dispatch para la transacción.').toBe(1);
  expect(await db.countDispatchAttempts(transactionId), 'No debe existir un intento SOAP previo antes de reanudar.').toBe(0);

  const persistedSoapSettings = await apiGetJson<SoapIntegrationSettings>(soapSettingsPath, runtime.token);
  assertLiveLocalSoapSettings(persistedSoapSettings);

  const dispatchResult = await apiPostJson<ContrapartidaDispatchResult>(contrapartidaDispatchPath, runtime.token, {
    transactionId
  });
  expect(dispatchResult.processed, 'El dispatch reanudado debe procesar únicamente la transacción creada.').toBe(1);

  await pollUntil(
    async () => db.findDispatchEvidence(transactionId),
    `evidencia SOAP de la transacción ${transactionId}`,
    120_000
  );
  writeLiveState({ transactionId, phase: 'dispatched' });

  await validatePersistedLiveResultWithoutDispatch(page, testInfo, db);
  await resumeAutomaticContrapartidaDispatch(page);
  writeLiveState({ transactionId, phase: 'complete' });
}

async function fillInput(page: Page, labelText: string, value: string): Promise<void> {
  const input = page.getByLabel(labelText, { exact: true });
  await expect(input, `Debe existir input para ${labelText}.`).toBeVisible();
  await input.fill(value);
}

async function selectOption(page: Page, labelText: string, optionText: string): Promise<void> {
  const control = page.getByLabel(labelText, { exact: true });
  await expect(control, `Debe existir selector para ${labelText}.`).toBeVisible();
  if (await control.getAttribute('aria-autocomplete') === 'list') {
    await control.fill(optionText);
    const options = page.getByRole('option').filter({ hasText: optionText });
    await expect(options, `Debe existir opción "${optionText}" en ${labelText}.`).toHaveCount(1);
    await control.press('ArrowDown');
    await control.press('Enter');
    return;
  }

  const normalize = (value: string) => value.normalize('NFD').replace(/\p{Diacritic}/gu, '').trim().toLowerCase();
  if (normalize(await control.innerText()).includes(normalize(optionText))) {
    return;
  }

  await control.click();
  const option = page.getByRole('option').filter({ hasText: optionText }).last();
  await expect(option, `Debe existir opcion "${optionText}" en ${labelText}.`).toBeVisible();
  await option.click();
}

async function authenticateThroughSpa(page: Page): Promise<{ token: string }> {
  await page.goto(joinUrl(uiBaseUrl, '/login'));
  await expect(page.getByRole('heading', { name: 'Ingreso al portal ACH Interbank' })).toBeVisible();

  await page.getByLabel('Usuario').fill(username);
  await page.locator('input[formcontrolname="password"]').fill(password);
  const loginResponsePromise = page.waitForResponse((response) =>
    response.request().method() === 'POST'
    && normalizeUrlPath(response.url()).toLowerCase().endsWith(loginPath),
  { timeout: 30_000 });

  await page.getByRole('button', { name: 'Ingresar', exact: true }).click();
  const loginResponse = await loginResponsePromise;
  expect(loginResponse.ok(), `Login SPA local debe responder OK. Status=${loginResponse.status()}`).toBeTruthy();
  const payload = await loginResponse.json() as AuthLoginResponse;
  const token = payload.data?.token;
  expect(token, 'El login SPA debe devolver access token.').toBeTruthy();
  await expect(page).not.toHaveURL(/\/login(?:\?.*)?$/);

  const storedToken = await page.evaluate(() => window.sessionStorage.getItem('ach.interbank.access_token'));
  expect(storedToken, 'La SPA debe persistir una sesion autenticada vigente en sessionStorage.').toBeTruthy();
  return { token: storedToken as string };
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

function assertLiveLocalSoapSettings(settings: SoapIntegrationSettings): void {
  for (const methodName of ['Proc_Contrapartidas', 'Proc_Transacciones']) {
    const mapping = settings.wscfaachMappings.find((item) => item.methodName === methodName);
    expect(mapping, `Debe existir la configuración persistida de ${methodName}.`).toBeTruthy();
    expect(mapping!.endpoint).toBe(wscfaachEndpoint);
    expect(mapping!.operatingMode).toBe('Live');
    expect(mapping!.enabled).toBeTruthy();
  }

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

function assertNoLegacyReferencePayload(payload: Record<string, unknown>): void {
  const forbiddenKeys = [
    'reference',
    'legacyReference',
    'legacyReferenceId',
    'referenciaLegado',
    'legacyTransactionReference',
    'customerId'
  ];
  for (const key of forbiddenKeys) {
    expect(Object.prototype.hasOwnProperty.call(payload, key), `El POST no debe contener ${key}.`).toBeFalsy();
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

function readLocalSoapEvidence(startedAt: Date, expectedRequestXml: string): LocalSoapEvidence | null {
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

  return null;
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
  const parts = new Intl.DateTimeFormat('en-CA', {
    timeZone: 'America/Bogota',
    year: 'numeric',
    month: '2-digit',
    day: '2-digit'
  }).formatToParts(new Date());
  const value = Object.fromEntries(parts.map((part) => [part.type, part.value]));
  return `${value['year']}-${value['month']}-${value['day']}`;
}

function escapeRegExp(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

function writeLiveState(value: Record<string, unknown>): void {
  if (!liveResultFile) return;
  writeFileSync(liveResultFile, JSON.stringify(value), { encoding: 'utf8' });
}
