import { expect, Locator, Page, test } from '@playwright/test';
import { mkdirSync } from 'node:fs';
import { resolve } from 'node:path';

const ui = (process.env['ACH_UI_URL'] ?? 'http://localhost:743').replace(/\/+$/, '');
const username = process.env['ACH_USER'] ?? '';
const password = process.env['ACH_PASS'] ?? '';
const evidenceDir = resolve(process.cwd(), '..', '..', 'docs', 'uat', 'evidencias', 'transactions-create');
mkdirSync(evidenceDir, { recursive: true });

test.describe.configure({ mode: 'serial' });
test.skip(!username || !password, 'ACH_USER y ACH_PASS son requeridos para validar la SPA real.');

for (const viewport of [
  { name: 'desktop', width: 1440, height: 900 },
  { name: 'laptop', width: 1366, height: 768 },
  { name: 'tablet', width: 768, height: 1024 },
  { name: 'mobile', width: 390, height: 844 }
]) {
  test(`LIVE: formulario buscable, accesible y sin overflow en ${viewport.name}`, async ({ page }, testInfo) => {
    await page.setViewportSize({ width: viewport.width, height: viewport.height });
    const consoleErrors: string[] = [];
    const failedRequests: string[] = [];
    const transactionPosts: string[] = [];
    await authenticate(page);
    page.on('console', (message) => {
      if (message.type() === 'error') {
        consoleErrors.push(message.text());
      }
    });
    page.on('requestfailed', (request) => failedRequests.push(`${request.method()} ${request.url()}`));
    page.on('request', (request) => {
      if (request.method() === 'POST' && new URL(request.url()).pathname.toLowerCase().endsWith('/transactions')) {
        transactionPosts.push(request.url());
      }
    });
    await page.goto(`${ui}/transactions/create`);
    await expect(page.locator('[data-testid="transaction-create-page"]')).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Crear transacción ACH', exact: true })).toBeVisible();
    await expect(page.getByText('Referencia legado')).toHaveCount(0);
    await expect(page.locator('input[formcontrolname="reference"]')).toHaveCount(0);
    expect(await page.locator('mat-form-field').count()).toBeGreaterThan(10);
    expect(await page.locator('mat-select').count()).toBeGreaterThan(3);
    await expect(page.getByTestId('transaction-customer')).toBeVisible();
    await expect(page.getByTestId('transaction-company-entry-description')).toBeVisible();
    await expect(page.getByTestId('transaction-destination-institution')).toBeVisible();
    await expect(page.getByTestId('transaction-destination-account')).toBeVisible();
    await page.screenshot({
      path: resolve(evidenceDir, `formulario-${viewport.name}.png`),
      fullPage: true,
      mask: [page.locator('input'), page.locator('textarea')]
    });

    await expect(page.locator('[data-testid="validation-summary"]')).toHaveCount(0);
    await page.locator('[data-testid="transaction-submit"]').click();
    await expect(page.locator('[data-testid="validation-summary"]')).toBeVisible();
    await expect(page.getByText('Ingrese el identificador único de la operación.')).toBeVisible();
    await expect(page.getByText('Ingrese el valor de la transacción.')).toBeVisible();
    await expect(page.getByText('Seleccione una entidad financiera destino de la lista.')).toBeVisible();
    await expect(page.getByLabel('Valor de la transacción', { exact: true })).toBeFocused();
    expect(transactionPosts).toEqual([]);

    const transactionType = page.getByRole('combobox', { name: 'Tipo de operación', exact: true });
    await transactionType.scrollIntoViewIfNeeded();
    await transactionType.click();
    await page.keyboard.press('ArrowDown');
    await page.keyboard.press('Enter');
    await expect(transactionType).toContainText('Débito');
    await page.locator('[data-testid="transaction-submit"]').click();
    await expect(page.getByText('Ingrese el código del recaudador.')).toBeVisible();
    await expect(page.getByText('Ingrese el código de cliente del receptor.')).toBeVisible();
    await expect(page.getByText('Ingrese la descripción del servicio.')).toBeVisible();

    const dimensions = await page.evaluate(() => ({
      documentWidth: document.documentElement.scrollWidth,
      viewportWidth: document.documentElement.clientWidth,
      offenders: [...document.querySelectorAll<HTMLElement>('body *')]
        .map((element) => {
          const rect = element.getBoundingClientRect();
          return {
            tag: element.tagName.toLowerCase(),
            className: element.className?.toString().slice(0, 120) ?? '',
            parentClassName: element.parentElement?.className?.toString().slice(0, 120) ?? '',
            text: element.textContent?.trim().slice(0, 80) ?? '',
            left: Math.round(rect.left),
            right: Math.round(rect.right),
            width: Math.round(rect.width)
          };
        })
        .filter((item) => item.right > document.documentElement.clientWidth + 1 || item.left < -1)
        .slice(0, 15)
    }));
    expect(
      dimensions.documentWidth,
      `Elementos fuera del viewport: ${JSON.stringify(dimensions.offenders)}`
    ).toBeLessThanOrEqual(dimensions.viewportWidth + 1);
    await page.screenshot({
      path: resolve(evidenceDir, `validacion-${viewport.name}.png`),
      fullPage: true,
      mask: [page.locator('input'), page.locator('textarea')]
    });

    await testInfo.attach(`transactions-create-${viewport.name}.png`, {
      body: await page.screenshot({
        fullPage: true,
        mask: [page.locator('input'), page.locator('textarea')]
      }),
      contentType: 'image/png'
    });

    expect(failedRequests).toEqual([]);
    expect(consoleErrors.filter((message) => !/favicon|ResizeObserver/i.test(message))).toEqual([]);
  });
}

test('controlado: autocompletados separan texto e IDs, preservan dependencias y prenotificación manual', async ({ page }, testInfo) => {
  await page.setViewportSize({ width: 1440, height: 900 });
  const consoleErrors: string[] = [];
  const pageErrors: string[] = [];
  const transactionPosts: string[] = [];
  page.on('console', message => {
    if (message.type() === 'error') {
      consoleErrors.push(message.text());
    }
  });
  page.on('pageerror', error => pageErrors.push(error.message));
  page.on('request', request => {
    if (request.method() === 'POST' && new URL(request.url()).pathname.toLowerCase().endsWith('/transactions')) {
      transactionPosts.push(request.url());
    }
  });

  await authenticate(page);
  await page.route(/\/customers(?:\?.*)?$/i, route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify([
      {
        id: 101,
        fullName: 'Álvaro Prueba',
        documentType: 'CC',
        documentNumber: '10000001',
        accountNumber: '71000001',
        accountNumbers: ['71000001', '71000002'],
        personType: 'PN',
        companyName: null
      }
    ])
  }));
  await page.route(/\/financial-institutions(?:\?.*)?$/i, route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify([
      {
        id: 701,
        name: 'Banco Destino Controlado',
        routingNumber: '0701',
        transitCode: '701',
        checkDigit: '1',
        isDefaultSource: false,
        status: 1
      },
      {
        id: 702,
        name: 'Banco Inactivo Controlado',
        routingNumber: '0702',
        transitCode: '702',
        checkDigit: '2',
        isDefaultSource: false,
        status: 2
      }
    ])
  }));
  await page.route(/\/api\/transactions\/company-entry-descriptions(?:\?.*)?$/i, route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify([
      { id: 801, term: 'NOMINAS', description: 'Nómina controlada', standardEntryClassCode: 'PPD' },
      { id: 802, term: 'PAGOS', description: 'Pago proveedores', standardEntryClassCode: 'CCD' }
    ])
  }));
  await page.route(/\/api\/customer-third-parties(?:\?.*)?$/i, route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({
      items: [
        {
          id: 901,
          destinationInstitutionId: 701,
          destinationInstitutionName: 'Banco Destino Controlado',
          destinationAccountNumber: '82000001',
          recipientIdNumber: '20000001'
        }
      ],
      total: 1,
      page: 1,
      pageSize: 500
    })
  }));

  await page.goto(`${ui}/transactions/create`);
  await expect(page.getByTestId('transaction-create-page')).toBeVisible();
  await expect(page.getByTestId('transaction-company-entry-description')).toHaveValue('Nómina controlada (NOMINAS)');

  const customerInput = page.getByTestId('transaction-customer');
  await selectAutocompleteWithKeyboard(page, customerInput, 'alvaro');
  await expect(customerInput).toHaveValue(/Álvaro Prueba/);
  await expect(page.getByLabel('Número de identificación del originador', { exact: true })).not.toHaveValue('');
  await expect(page.getByLabel('Nombre o razón social del originador', { exact: true })).not.toHaveValue('');

  const sourceAccountInput = page.getByTestId('transaction-source-account');
  await expect(sourceAccountInput).toHaveValue('71000001');
  await selectAutocompleteWithKeyboard(page, sourceAccountInput, '0002');
  await expect(sourceAccountInput).toHaveValue('71000002');

  const institutionInput = page.getByTestId('transaction-destination-institution');
  await selectAutocompleteWithKeyboard(page, institutionInput, 'destino controlado');
  await expect(institutionInput).toHaveValue('Banco Destino Controlado');

  const destinationAccountInput = page.getByTestId('transaction-destination-account');
  await selectAutocompleteWithKeyboard(page, destinationAccountInput, '20000001');
  await expect(destinationAccountInput).toHaveValue(/82000001/);
  await expect(page.getByLabel('Número de identificación del receptor', { exact: true })).toHaveValue('20000001');

  const descriptionInput = page.getByTestId('transaction-company-entry-description');
  await selectAutocompleteWithKeyboard(page, descriptionInput, 'proveedores pagos');
  await expect(descriptionInput).toHaveValue('Pago proveedores (PAGOS)');
  await selectAutocompleteWithKeyboard(page, descriptionInput, 'nominas');
  await expect(descriptionInput).toHaveValue('Nómina controlada (NOMINAS)');

  await institutionInput.fill('texto sin selección');
  await page.getByTestId('transaction-submit').click();
  await expect(page.getByText('Seleccione una entidad financiera destino de la lista.')).toBeVisible();
  expect(transactionPosts, 'Texto arbitrario no debe producir POST de transacción.').toEqual([]);
  const institutionIssue = page.getByTestId('validation-summary')
    .getByRole('button')
    .filter({ hasText: 'Entidad financiera destino' });
  await expect(institutionIssue).toHaveCount(1);
  await institutionIssue.click();
  await expect(institutionInput).toBeFocused();

  await page.getByRole('checkbox', { name: 'Es una prenotificación' }).check();
  await expect(page.getByLabel('Valor de la transacción', { exact: true })).toHaveValue('0');
  const manualDestinationInput = page.getByTestId('transaction-destination-account');
  await expect(manualDestinationInput).toHaveAttribute('formcontrolname', 'destinationAccountNumber');
  await manualDestinationInput.fill('82000003');
  await expect(manualDestinationInput).toHaveValue('82000003');

  await assertNoHorizontalOverflow(page);
  const screenshotName = 'autocompletados-controlados-desktop.png';
  await page.screenshot({
    path: resolve(evidenceDir, screenshotName),
    fullPage: true,
    mask: [page.locator('input'), page.locator('textarea')]
  });
  await testInfo.attach(screenshotName, {
    body: await page.screenshot({
      fullPage: true,
      mask: [page.locator('input'), page.locator('textarea')]
    }),
    contentType: 'image/png'
  });

  expect(consoleErrors.filter(message => !/favicon|ResizeObserver/i.test(message))).toEqual([]);
  expect(pageErrors).toEqual([]);
});

async function authenticate(page: Page): Promise<void> {
  await page.goto(`${ui}/login`);
  await page.getByLabel('Usuario').fill(username);
  await page.locator('input[formcontrolname="password"]').fill(password);
  const login = page.waitForResponse((response) =>
    response.request().method() === 'POST'
    && new URL(response.url()).pathname.toLowerCase().endsWith('/auth/login')
  );
  await page.getByRole('button', { name: 'Ingresar', exact: true }).click();
  expect((await login).ok()).toBeTruthy();
  await expect(page).not.toHaveURL(/\/login(?:\?.*)?$/);
}

async function selectAutocompleteWithKeyboard(
  page: Page,
  input: Locator,
  searchText: string
): Promise<void> {
  await input.fill(searchText);
  const options = page.getByRole('option');
  await expect(options).toHaveCount(1);
  await input.press('ArrowDown');
  await input.press('Enter');
}

async function assertNoHorizontalOverflow(page: Page): Promise<void> {
  const dimensions = await page.evaluate(() => ({
    documentWidth: document.documentElement.scrollWidth,
    viewportWidth: document.documentElement.clientWidth
  }));
  expect(dimensions.documentWidth).toBeLessThanOrEqual(dimensions.viewportWidth + 1);
}
