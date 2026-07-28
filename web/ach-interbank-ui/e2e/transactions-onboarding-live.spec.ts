import { expect, Page, test, TestInfo } from '@playwright/test';
import { mkdirSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { G36SqlServer, sqlString } from './support/g36-sqlserver';

type LoginResponse = { data?: { token?: string } };
type FinancialInstitution = {
  id: number;
  name: string;
  isDefaultSource?: boolean;
  status?: number | string;
};
type CreatedTransaction = {
  id: number;
  transactionExternalId?: string;
};
type OnboardingSnapshot = {
  originators: number;
  recipients: number;
  sourceAccounts: number;
  destinationAccounts: number;
  thirdParties: number;
  transactions: number;
  stateEvents: number;
};
type ThirdPartySnapshot = {
  id: number;
  status: string;
  prenotificationTransactionId: number | null;
};

const shouldRun = process.env['RUN_LOCAL_TRANSACTION_ONBOARDING_E2E'] === 'true';
const hasCredentials = Boolean(process.env['ACH_USER'] && process.env['ACH_PASS']);
const ui = (process.env['ACH_UI_URL'] ?? 'http://localhost:743').replace(/\/+$/, '');
const username = process.env['ACH_USER'] ?? '';
const password = process.env['ACH_PASS'] ?? '';
const expectedMainBundle = process.env['ACH_EXPECTED_MAIN_BUNDLE'] ?? '';
const stateFile = resolve(process.cwd(), 'test-results', 'transactions-onboarding-state.json');

test.describe.configure({ mode: 'serial' });
test.use({
  trace: 'on',
  screenshot: 'only-on-failure',
  video: 'retain-on-failure'
});
test.skip(!shouldRun, 'RUN_LOCAL_TRANSACTION_ONBOARDING_E2E=true es requerido para la prueba LIVE no monetaria.');
test.skip(!hasCredentials, 'ACH_USER y ACH_PASS deben venir del entorno.');

test('onboarding silencioso desde la SPA conserva la prenotificación sin decisión manual', async ({ page }, testInfo) => {
  test.setTimeout(300_000);
  const db = new G36SqlServer();
  const diagnostics = installDiagnostics(page);
  const suffix = process.env['ACH_ONBOARDING_RUN_SUFFIX'] ?? String(Date.now()).slice(-8);
  const data = {
    sourceDocument: `91${suffix}`,
    recipientDocument: `80${suffix}`,
    sourceAccount: `71${suffix}01`,
    destinationAccount: `82${suffix}02`,
    sourceName: `ORIGEN${suffix}`,
    recipientName: `RECEPTOR SINT ${suffix}`,
    firstExternalId: `PW-PRE-${suffix}-01`,
    secondExternalId: `PW-PRE-${suffix}-02`,
    collectorId: `91${suffix}`,
    receiverCustomerCode: `CLI-${suffix}`,
    serviceDescription: 'PRENOTIF QA'
  };

  try {
    const token = await loginThroughUi(page);
    const institutionsResponse = await page.request.get(`${ui}/financial-institutions`, {
      headers: { Authorization: `Bearer ${token}` }
    });
    expect(institutionsResponse.ok()).toBeTruthy();
    const institutions = await institutionsResponse.json() as FinancialInstitution[];
    const destination = institutions.find(item =>
      !item.isDefaultSource
      && (item.status === 1 || String(item.status).toLowerCase() === 'active'));
    expect(destination, 'Debe existir una entidad destino sintética activa distinta de CFA.').toBeTruthy();

    const initial = loadOnboardingSnapshot(db, data);
    expect([0, 1, 2]).toContain(initial.transactions);

    const first = initial.transactions === 0
      ? await createPrenotificationFromUi(page, testInfo, data, destination!, false)
      : {
          transaction: loadTransaction(db, data.firstExternalId),
          payload: undefined,
          postCount: 0
        };
    if (first.payload) {
      expect(first.postCount).toBe(1);
      expect(first.payload).not.toHaveProperty('reference');
      assertNoLegacyReference(first.payload);
    }

    const afterFirst = loadOnboardingSnapshot(db, data);
    const expectedTransactionsAfterFirst = initial.transactions === 0 ? 1 : initial.transactions;
    expect(afterFirst).toEqual({
      originators: 1,
      recipients: 1,
      sourceAccounts: 1,
      destinationAccounts: 1,
      thirdParties: 1,
      transactions: expectedTransactionsAfterFirst,
      stateEvents: expectedTransactionsAfterFirst
    });

    const approved = loadThirdParty(db, data);
    await assertThirdPartyIsReadOnly(page, token, approved.id, data.recipientDocument);
    expect(['Pending', 'Active']).toContain(approved.status);
    expect(approved.prenotificationTransactionId).toBe(first.transaction.id);
    if (approved.status === 'Pending') {
      return;
    }

    const second = initial.transactions < 2
      ? await createPrenotificationFromUi(page, testInfo, data, destination!, true)
      : {
          transaction: loadTransaction(db, data.secondExternalId),
          payload: undefined,
          postCount: 0
        };
    if (second.payload) {
      expect(second.postCount).toBe(1);
      assertNoLegacyReference(second.payload);
    }

    const afterSecond = loadOnboardingSnapshot(db, data);
    expect(afterSecond).toEqual({
      originators: 1,
      recipients: 1,
      sourceAccounts: 1,
      destinationAccounts: 1,
      thirdParties: 1,
      transactions: 2,
      stateEvents: 2
    });
    const stillApproved = loadThirdParty(db, data);
    expect(stillApproved.status).toBe('Active');
    expect(stillApproved.prenotificationTransactionId).toBe(first.transaction.id);

    const postsBeforeControlledFailure = diagnostics.transactionPosts.length;
    await page.goto(`${ui}/transactions/create`);
    await expect(page.getByTestId('transaction-create-page')).toBeVisible();
    await page.getByTestId('transaction-submit').click();
    await expect(page.getByTestId('validation-summary')).toBeVisible();
    await expect(page.locator('mat-error')).not.toHaveCount(0);
    expect(diagnostics.transactionPosts).toHaveLength(postsBeforeControlledFailure);
    expect(loadOnboardingSnapshot(db, data)).toEqual(afterSecond);

    await assertResponsive(page, testInfo);
    await diagnostics.assertClean();

    mkdirSync(resolve(process.cwd(), 'test-results'), { recursive: true });
    writeFileSync(stateFile, JSON.stringify({
      ...data,
      destinationInstitutionId: destination!.id,
      destinationInstitutionName: destination!.name,
      firstPrenotificationTransactionId: first.transaction.id,
      secondPrenotificationTransactionId: second.transaction.id,
      thirdPartyId: stillApproved.id
    }, null, 2));
  } finally {
    db.close();
  }
});

async function loginThroughUi(page: Page): Promise<string> {
  await page.goto(`${ui}/login`);
  await page.getByLabel('Usuario', { exact: true }).fill(username);
  await page.locator('input[formcontrolname="password"]').fill(password);
  const responsePromise = page.waitForResponse(response =>
    response.request().method() === 'POST'
    && new URL(response.url()).pathname === '/auth/login');
  await page.getByRole('button', { name: 'Ingresar', exact: true }).click();
  const response = await responsePromise;
  expect(response.ok()).toBeTruthy();
  const body = await response.json() as LoginResponse;
  expect(body.data?.token).toBeTruthy();
  await expect(page).not.toHaveURL(/\/login(?:\?.*)?$/);
  return body.data!.token!;
}

async function createPrenotificationFromUi(
  page: Page,
  testInfo: TestInfo,
  data: {
    sourceDocument: string;
    recipientDocument: string;
    sourceAccount: string;
    destinationAccount: string;
    sourceName: string;
    recipientName: string;
    firstExternalId: string;
    secondExternalId: string;
    collectorId: string;
    receiverCustomerCode: string;
    serviceDescription: string;
  },
  destination: FinancialInstitution,
  selectExistingCustomer: boolean
): Promise<{ transaction: CreatedTransaction; payload: Record<string, unknown> | undefined; postCount: number }> {
  const customersResponsePromise = page.waitForResponse(response =>
    response.request().method() === 'GET'
    && new URL(response.url()).pathname === '/customers');
  await page.goto(`${ui}/transactions/create`);
  const customersResponse = await customersResponsePromise;
  expect(customersResponse.status()).toBe(200);
  expect(customersResponse.headers()['content-type'] ?? '').toContain('application/json');
  const customersBody = await customersResponse.text();
  expect(customersBody).not.toMatch(/<!doctype|<html/i);
  expect(() => JSON.parse(customersBody)).not.toThrow();

  await expect(page.getByTestId('transaction-create-page')).toBeVisible();
  await expect(page.locator('mat-form-field')).not.toHaveCount(0);
  await expect(page.locator('mat-error')).toHaveCount(0);
  await expect(page.getByText('Referencia legado')).toHaveCount(0);
  const mainBundle = await page.evaluate(() =>
    performance.getEntriesByType('resource')
      .map(entry => entry.name)
      .find(name => /\/main\.[0-9a-f]+\.js(?:\?|$)/i.test(name)) ?? '');
  expect(mainBundle).toBeTruthy();
  if (expectedMainBundle) {
    expect(mainBundle).toContain(`/${expectedMainBundle}`);
  }

  if (selectExistingCustomer) {
    await selectMaterialOption(page, 'Cliente originador', data.sourceDocument);
  } else {
    await fill(page, 'Número de cuenta de origen', data.sourceAccount);
    await fill(page, 'Número de identificación del originador', data.sourceDocument);
    await fill(page, 'Nombre o razón social del originador', data.sourceName.slice(0, 16));
  }

  await fill(page, 'ID de operación del cliente', selectExistingCustomer ? data.secondExternalId : data.firstExternalId);
  await selectMaterialOption(page, 'Tipo de operación', 'Débito');
  await page.getByRole('checkbox', { name: 'Es una prenotificación' }).check();
  await selectMaterialOption(page, 'Entidad financiera destino', destination.name);
  await fill(page, 'Número de cuenta destino', data.destinationAccount);
  await fill(page, 'Número de identificación del receptor', data.recipientDocument);
  await fill(page, 'Nombre o razón social del receptor', data.recipientName);
  await fill(page, 'Código del recaudador', data.collectorId);
  await fill(page, 'Código de cliente del receptor', data.receiverCustomerCode);
  await fill(page, 'Descripción del servicio', data.serviceDescription);
  await fill(page, 'Información adicional', `PRENOTE-${selectExistingCustomer ? '02' : '01'}-${data.recipientDocument}`);

  const posts: Array<Record<string, unknown>> = [];
  const listener = (request: import('@playwright/test').Request) => {
    if (request.method() === 'POST' && new URL(request.url()).pathname.endsWith('/transactions')) {
      posts.push(request.postDataJSON() as Record<string, unknown>);
    }
  };
  page.on('request', listener);
  const responsePromise = page.waitForResponse(response =>
    response.request().method() === 'POST'
    && new URL(response.url()).pathname.endsWith('/transactions'), {
    timeout: 30_000
  });
  await page.getByTestId('transaction-submit').click();
  const response = await responsePromise;
  page.off('request', listener);
  const responseText = await response.text();
  expect(response.status(), responseText).toBe(201);
  expect(posts).toHaveLength(1);
  const transaction = JSON.parse(responseText) as CreatedTransaction;
  expect(transaction.id).toBeGreaterThan(0);
  expect(transaction.transactionExternalId).toBe(selectExistingCustomer ? data.secondExternalId : data.firstExternalId);
  await testInfo.attach(selectExistingCustomer ? 'prenotificacion-idempotente-ui.png' : 'prenotificacion-inicial-ui.png', {
    body: await page.screenshot({
      fullPage: false,
      mask: [page.locator('input'), page.locator('tbody'), page.locator('.ag-center-cols-container')]
    }),
    contentType: 'image/png'
  });
  return { transaction, payload: posts[0], postCount: posts.length };
}

async function assertThirdPartyIsReadOnly(
  page: Page,
  token: string,
  thirdPartyId: number,
  recipientDocument: string
): Promise<void> {
  await page.goto(`${ui}/customer-third-parties`);
  await expect(page.getByRole('heading', { name: 'Terceros y prenotificaciones' })).toBeVisible();
  await page.getByLabel('Documento receptor', { exact: true }).fill(recipientDocument);
  await page.getByRole('button', { name: 'Buscar', exact: true }).click();
  const row = page.locator('.ag-row').filter({ hasText: recipientDocument });
  await expect(row).toHaveCount(1);
  await expect(page.getByRole('button', { name: 'Aprobar', exact: true })).toHaveCount(0);
  await expect(page.getByRole('button', { name: 'Rechazar', exact: true })).toHaveCount(0);
  const forbidden = await page.request.patch(`${ui}/api/customer-third-parties/${thirdPartyId}/status`, {
    headers: { Authorization: `Bearer ${token}` },
    data: { status: 1, validationMessage: 'Intento manual bloqueado' }
  });
  expect([404, 405]).toContain(forbidden.status());
}

function loadOnboardingSnapshot(
  db: G36SqlServer,
  data: { sourceDocument: string; recipientDocument: string; sourceAccount: string; destinationAccount: string; firstExternalId: string }
): OnboardingSnapshot {
  return db.query<OnboardingSnapshot>(
    `SELECT
      (SELECT COUNT(*) FROM [Customers] WHERE [DocumentNumber] = ${sqlString(data.sourceDocument)}) AS [originators],
      (SELECT COUNT(*) FROM [Customers] WHERE [DocumentNumber] = ${sqlString(data.recipientDocument)}) AS [recipients],
      (SELECT COUNT(*) FROM [CustomerAccounts] WHERE [AccountNumber] = ${sqlString(data.sourceAccount)}) AS [sourceAccounts],
      (SELECT COUNT(*) FROM [CustomerAccounts] WHERE [AccountNumber] = ${sqlString(data.destinationAccount)}) AS [destinationAccounts],
      (SELECT COUNT(*) FROM [CustomerThirdParties]
        WHERE [DestinationAccountNumber] = ${sqlString(data.destinationAccount)}
          AND [RecipientIdNumber] = ${sqlString(data.recipientDocument)}) AS [thirdParties],
      (SELECT COUNT(*) FROM [AchTransactions]
        WHERE [TransactionExternalId] LIKE ${sqlString(`${data.firstExternalId.slice(0, -2)}%`)}) AS [transactions],
      (SELECT COUNT(*) FROM [AchTransactionStateEvents] e
        JOIN [AchTransactions] t ON t.[Id] = e.[AchTransactionId]
        WHERE t.[TransactionExternalId] LIKE ${sqlString(`${data.firstExternalId.slice(0, -2)}%`)}) AS [stateEvents]`
  )[0] ?? emptySnapshot();
}

function loadThirdParty(
  db: G36SqlServer,
  data: { destinationAccount: string; recipientDocument: string }
): ThirdPartySnapshot {
  const rows = db.query<ThirdPartySnapshot>(
    `SELECT [Id] AS [id], [Status] AS [status],
            [PrenotificationTransactionId] AS [prenotificationTransactionId]
     FROM [CustomerThirdParties]
     WHERE [DestinationAccountNumber] = ${sqlString(data.destinationAccount)}
       AND [RecipientIdNumber] = ${sqlString(data.recipientDocument)}`
  );
  expect(rows).toHaveLength(1);
  return rows[0];
}

function loadTransaction(db: G36SqlServer, externalId: string): CreatedTransaction {
  const rows = db.query<CreatedTransaction>(
    `SELECT [Id] AS [id], [TransactionExternalId] AS [transactionExternalId]
     FROM [AchTransactions]
     WHERE [TransactionExternalId] = ${sqlString(externalId)}`
  );
  expect(rows).toHaveLength(1);
  return rows[0];
}

function emptySnapshot(): OnboardingSnapshot {
  return {
    originators: 0,
    recipients: 0,
    sourceAccounts: 0,
    destinationAccounts: 0,
    thirdParties: 0,
    transactions: 0,
    stateEvents: 0
  };
}

function assertNoLegacyReference(payload: Record<string, unknown>): void {
  const keys = Object.keys(payload).map(key => key.toLowerCase());
  expect(keys).not.toContain('legacyreference');
  expect(keys).not.toContain('legacyreferenceid');
  expect(keys).not.toContain('referencialegado');
  expect(keys.some(key => key.includes('legacy') || key.includes('legado'))).toBeFalsy();
}

async function fill(page: Page, label: string, value: string): Promise<void> {
  const input = page.getByLabel(label, { exact: true });
  await expect(input).toBeVisible();
  await input.fill(value);
}

async function selectMaterialOption(page: Page, label: string, optionText: string): Promise<void> {
  const control = page.getByLabel(label, { exact: true }).first();
  await expect(control).toBeVisible();
  if (await control.getAttribute('aria-autocomplete') === 'list') {
    await control.fill(optionText);
    const options = page.getByRole('option').filter({ hasText: optionText });
    await expect(options).toHaveCount(1);
    await control.press('ArrowDown');
    await control.press('Enter');
    return;
  }

  await control.click();
  const option = page.getByRole('option').filter({ hasText: optionText });
  await expect(option).toHaveCount(1);
  await option.evaluate((element: HTMLElement) => element.click());
}

async function assertResponsive(page: Page, testInfo: TestInfo): Promise<void> {
  await page.setViewportSize({ width: 1440, height: 900 });
  await page.goto(`${ui}/transactions/create`);
  await expect(page.getByTestId('transaction-create-page')).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth + 1)).toBeTruthy();
  await testInfo.attach('transactions-create-desktop.png', {
    body: await page.screenshot({ fullPage: true, mask: [page.locator('input')] }),
    contentType: 'image/png'
  });

  await page.setViewportSize({ width: 390, height: 844 });
  await page.reload();
  await expect(page.getByTestId('transaction-create-page')).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth + 1)).toBeTruthy();
  await testInfo.attach('transactions-create-mobile.png', {
    body: await page.screenshot({ fullPage: true, mask: [page.locator('input')] }),
    contentType: 'image/png'
  });
}

function installDiagnostics(page: Page): {
  transactionPosts: string[];
  assertClean: () => Promise<void>;
} {
  const consoleErrors: string[] = [];
  const pageErrors: string[] = [];
  const requestFailures: string[] = [];
  const unexpectedHtml: string[] = [];
  const transactionPosts: string[] = [];
  const responseChecks: Array<Promise<void>> = [];

  page.on('console', message => {
    if (message.type() === 'error') {
      consoleErrors.push(message.text());
    }
  });
  page.on('pageerror', error => pageErrors.push(error.message));
  page.on('requestfailed', request => requestFailures.push(`${request.method()} ${request.url()}`));
  page.on('request', request => {
    if (request.method() === 'POST' && new URL(request.url()).pathname.endsWith('/transactions')) {
      transactionPosts.push(request.url());
    }
  });
  page.on('response', response => {
    const path = new URL(response.url()).pathname;
    const expectedJson = path === '/customers'
      || path.endsWith('/transactions')
      || path.startsWith('/api/customer-third-parties')
      || path === '/financial-institutions';
    if (!expectedJson) {
      return;
    }
    responseChecks.push((async () => {
      const contentType = response.headers()['content-type'] ?? '';
      if (contentType.includes('text/html')) {
        const prefix = (await response.text()).slice(0, 80);
        unexpectedHtml.push(`${response.status()} ${path}: ${prefix}`);
      }
    })());
  });

  return {
    transactionPosts,
    assertClean: async () => {
      await Promise.all(responseChecks);
      expect(consoleErrors, 'No debe haber console.error relevante.').toEqual([]);
      expect(pageErrors, 'No debe haber pageerror.').toEqual([]);
      expect(requestFailures, 'No debe haber requestfailed.').toEqual([]);
      expect(unexpectedHtml, 'Las rutas JSON no deben devolver HTML.').toEqual([]);
    }
  };
}
