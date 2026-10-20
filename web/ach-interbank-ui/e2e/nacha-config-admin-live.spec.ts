import { expect, test, type Page, type TestInfo } from '@playwright/test';

const spaUrl = process.env['ACH_UI_URL'] ?? 'http://localhost:743';
const apiUrl = process.env['ACH_API_URL'] ?? 'http://localhost:843';
const adminUser = process.env['E2E_ADMIN_USER'] ?? process.env['ACH_USER'];
const adminPassword = process.env['E2E_ADMIN_PASSWORD'] ?? process.env['ACH_PASS'];
const runId = `${new Date().toISOString().slice(0, 10).replaceAll('-', '')}-${Date.now().toString().slice(-6)}`;
const createdCode = `PW-LIVE-${runId}-NUEVO`;
const cloneCode = `PW-LIVE-${runId}-CLON`;
const createdName = `Perfil Playwright LIVE ${runId}`;
const cloneName = `Perfil clonado Playwright LIVE ${runId}`;

let createdProfileId = 0;
let clonedProfileId = 0;
let editedFieldName = '';

const routes = [
  { path: '/nacha-config-admin/perfiles', testId: 'nacha-config-profiles-page', name: 'perfiles' },
  { path: '/nacha-config-admin/records', testId: 'nacha-config-records-page', name: 'registros' },
  { path: '/nacha-config-admin/variants-fields', testId: 'nacha-config-variants-fields-page', name: 'variantes-campos' }
];

const viewports = [
  { width: 1440, height: 900, name: 'desktop' },
  { width: 1366, height: 768, name: 'portatil' },
  { width: 1024, height: 768, name: 'intermedio' },
  { width: 768, height: 1024, name: 'tablet' },
  { width: 390, height: 844, name: 'movil' }
];

test.describe.serial('NACHA Config Admin LIVE', () => {
  test.beforeEach(async ({ page }) => {
    expect(adminUser, 'E2E_ADMIN_USER o ACH_USER es obligatorio.').toBeTruthy();
    expect(adminPassword, 'E2E_ADMIN_PASSWORD o ACH_PASS es obligatorio.').toBeTruthy();
    await login(page);
  });

  test('crea, cancela, edita y comprueba persistencia real', async ({ page }, testInfo) => {
    test.setTimeout(180_000);
    const evidence = observeRuntime(page);

    await expectHealth(page);
    await page.goto(`${spaUrl}/nacha-config-admin/perfiles`, { waitUntil: 'domcontentloaded' });
    await waitForRouteReady(page, 'perfiles');
    await expect(page.getByRole('heading', { name: 'Perfiles de configuración NACHA-M' })).toBeVisible();

    const firstCode = (await page.locator('.ag-row').first().locator('[col-id="profileCode"]').innerText()).trim();
    const profileSearch = page.getByRole('textbox', { name: 'Buscar', exact: true });
    await profileSearch.fill(firstCode);
    await expect(page.locator('.ag-row')).toHaveCount(1);
    await profileSearch.clear();

    const createPanel = page.getByTestId('create-profile-panel');
    await createPanel.locator('summary').click();
    const createPostsBefore = evidence.count('POST', '/nacha-config/perfiles');
    const profileCodeInput = createPanel.getByLabel('Código del perfil');
    await profileCodeInput.fill('x');
    await profileCodeInput.blur();
    await expect(createPanel.getByText('Debe tener mínimo 6 caracteres.')).toBeVisible();
    await expect(createPanel.getByRole('button', { name: 'Crear borrador' })).toBeDisabled();
    expect(evidence.count('POST', '/nacha-config/perfiles')).toBe(createPostsBefore);

    await profileCodeInput.fill(createdCode);
    await createPanel.getByLabel('Nombre', { exact: true }).fill(createdName);
    await createPanel.getByLabel('Descripción').fill('Dato controlado creado por Playwright LIVE.');
    const createResponsePromise = waitForApiResponse(page, 'POST', /\/nacha-config\/perfiles$/);
    await createPanel.getByRole('button', { name: 'Crear borrador' }).click();
    const createResponse = await createResponsePromise;
    expect(createResponse.status()).toBeGreaterThanOrEqual(200);
    expect(createResponse.status()).toBeLessThan(300);
    const created = await createResponse.json() as { id: number; profileCode: string };
    createdProfileId = created.id;
    expect(created.profileCode).toBe(createdCode);
    expect(evidence.count('POST', '/nacha-config/perfiles')).toBe(createPostsBefore + 1);
    await expect(page).toHaveURL(new RegExp(`/nacha-config-admin/perfiles/${createdProfileId}$`));

    const workspace = page.getByTestId('nacha-config-profile-workspace-page');
    await expect(workspace).toContainText(createdCode);
    const editName = workspace.getByLabel('Nombre', { exact: true });
    const persistedEditedName = `${createdName} editado`;
    const putsBeforeCancel = evidence.count('PUT', `/nacha-config/perfiles/${createdProfileId}`);
    await editName.fill('Cambio temporal que debe cancelarse');
    await workspace.getByRole('button', { name: 'Cancelar cambios' }).click();
    await expect(editName).toHaveValue(createdName);
    expect(evidence.count('PUT', `/nacha-config/perfiles/${createdProfileId}`)).toBe(putsBeforeCancel);

    await editName.fill(persistedEditedName);
    const editResponsePromise = waitForApiResponse(page, 'PUT', new RegExp(`/nacha-config/perfiles/${createdProfileId}$`));
    const editRefreshPromise = waitForApiResponse(page, 'GET', new RegExp(`/nacha-config/perfiles/${createdProfileId}$`));
    await workspace.getByRole('button', { name: 'Guardar borrador' }).click();
    expect((await editResponsePromise).status()).toBe(200);
    expect((await editRefreshPromise).status()).toBe(200);
    const reloadEditedProfilePromise = waitForApiResponse(page, 'GET', new RegExp(`/nacha-config/perfiles/${createdProfileId}$`));
    await page.reload({ waitUntil: 'domcontentloaded' });
    expect((await reloadEditedProfilePromise).status()).toBe(200);
    await expect(workspace.getByLabel('Nombre', { exact: true })).toHaveValue(persistedEditedName);

    await page.goto(`${spaUrl}/nacha-config-admin/perfiles`, { waitUntil: 'domcontentloaded' });
    await waitForRouteReady(page, 'perfiles');
    const publishedRow = page.locator('.ag-row').filter({ hasText: 'PUBLICADO' }).first();
    await expect(publishedRow).toBeVisible();
    const publishedRowId = await publishedRow.getAttribute('row-id');
    expect(publishedRowId).not.toBeNull();
    const horizontalViewport = page.locator('.ag-center-cols-viewport');
    await horizontalViewport.evaluate((element) => {
      element.scrollLeft = element.scrollWidth;
    });
    const openPublishedProfile = page.locator(`.ag-row[row-id="${publishedRowId}"] [data-action="ver"]`);
    await expect(openPublishedProfile).toBeVisible({ timeout: 10_000 });
    await openPublishedProfile.click();
    await expect(page).toHaveURL(/\/nacha-config-admin\/perfiles\/\d+$/);

    const cloneWorkspace = page.getByTestId('nacha-config-profile-workspace-page');
    await cloneWorkspace.getByLabel('Nuevo código').fill(cloneCode);
    await cloneWorkspace.getByLabel('Nuevo nombre').fill(cloneName);
    const cloneResponsePromise = waitForApiResponse(page, 'POST', /\/nacha-config\/perfiles\/\d+\/clonar$/);
    await cloneWorkspace.getByRole('button', { name: 'Clonar como borrador' }).click();
    const cloneResponse = await cloneResponsePromise;
    expect(cloneResponse.status()).toBeGreaterThanOrEqual(200);
    expect(cloneResponse.status()).toBeLessThan(300);
    const cloned = await cloneResponse.json() as { id: number; profileCode: string };
    clonedProfileId = cloned.id;
    expect(cloned.profileCode).toBe(cloneCode);
    await expect(page).toHaveURL(new RegExp(`/nacha-config-admin/perfiles/${clonedProfileId}$`));
    await expect(page.getByTestId('nacha-config-profile-workspace-page')).toContainText(cloneCode);

    await page.getByRole('button', { name: 'Ir a registros oficiales' }).first().click();
    await waitForRouteReady(page, 'registros');
    await expect(page).toHaveURL(new RegExp(`profileId=${clonedProfileId}`));
    await expect(page.locator('.summary-card')).toContainText(cloneCode);

    const firstRecordRow = page.locator('.records-table tbody tr').first();
    const editedRecordCode = (await firstRecordRow.locator('td').nth(1).locator('strong').innerText()).trim();
    const firstSequence = firstRecordRow.locator('input');
    const originalSequence = Number(await firstSequence.inputValue());
    const sequenceValues = await page.locator('.records-table tbody input').evaluateAll((inputs) =>
      inputs.map((input) => Number((input as HTMLInputElement).value))
    );
    const temporarySequence = Math.max(...sequenceValues) + 10;
    const sequencePutsBeforeCancel = evidence.count('PUT', `/nacha-config/perfiles/${clonedProfileId}/records/secuencia`);
    await firstSequence.fill(String(temporarySequence));
    await page.getByRole('button', { name: 'Cancelar cambios' }).click();
    await expect(firstSequence).toHaveValue(String(originalSequence));
    expect(evidence.count('PUT', `/nacha-config/perfiles/${clonedProfileId}/records/secuencia`)).toBe(sequencePutsBeforeCancel);

    await firstSequence.fill(String(temporarySequence));
    const sequenceResponsePromise = waitForApiResponse(
      page,
      'PUT',
      new RegExp(`/nacha-config/perfiles/${clonedProfileId}/records/secuencia$`)
    );
    const sequenceRefreshPromise = waitForApiResponse(page, 'GET', new RegExp(`/nacha-config/perfiles/${clonedProfileId}$`));
    await page.getByRole('button', { name: 'Guardar secuencia' }).click();
    expect((await sequenceResponsePromise).status()).toBe(200);
    expect((await sequenceRefreshPromise).status()).toBe(200);
    const reloadRecordsPromise = waitForApiResponse(page, 'GET', new RegExp(`/nacha-config/perfiles/${clonedProfileId}$`));
    await page.reload({ waitUntil: 'domcontentloaded' });
    expect((await reloadRecordsPromise).status()).toBe(200);
    await waitForRouteReady(page, 'registros');
    const persistedRecordRow = page.locator('.records-table tbody tr').filter({
      has: page.locator('td:nth-child(2) > strong', {
        hasText: new RegExp(`^\\s*${editedRecordCode}\\s*$`)
      })
    });
    await expect(persistedRecordRow.locator('input')).toHaveValue(String(temporarySequence));

    await page.getByRole('button', { name: 'Ver variantes y campos' }).click();
    await waitForRouteReady(page, 'variantes-campos');
    await expect(page).toHaveURL(new RegExp(`profileId=${clonedProfileId}`));
    await expect(page.locator('.profile-card')).toContainText(cloneCode);

    const variantName = page.getByLabel('Nombre ES');
    const originalVariantName = await variantName.inputValue();
    const variantPutsBeforeCancel = evidence.count('PUT', `/nacha-config/perfiles/${clonedProfileId}/variantes/`);
    await variantName.fill('Cambio temporal de variante');
    await page.getByRole('button', { name: 'Cancelar cambios' }).first().click();
    await expect(variantName).toHaveValue(originalVariantName);
    expect(evidence.count('PUT', `/nacha-config/perfiles/${clonedProfileId}/variantes/`)).toBe(variantPutsBeforeCancel);

    const persistedVariantName = `${originalVariantName} LIVE`;
    await variantName.fill(persistedVariantName);
    const variantResponsePromise = waitForApiResponse(
      page,
      'PUT',
      new RegExp(`/nacha-config/perfiles/${clonedProfileId}/variantes/\\d+$`)
    );
    const variantRefreshPromise = waitForApiResponse(page, 'GET', new RegExp(`/nacha-config/perfiles/${clonedProfileId}$`));
    await page.getByRole('button', { name: 'Guardar variante' }).click();
    expect((await variantResponsePromise).status()).toBe(200);
    expect((await variantRefreshPromise).status()).toBe(200);
    await expect(variantName).toHaveValue(persistedVariantName);

    const fieldName = page.getByLabel('Nombre del campo ES');
    const originalFieldName = await fieldName.inputValue();
    const startPosition = page.getByLabel('Posición inicial', { exact: true });
    const length = page.getByLabel('Longitud', { exact: true });
    const originalStart = await startPosition.inputValue();
    const originalLength = await length.inputValue();
    const fieldPutsBeforeInvalid = evidence.count('PUT', `/nacha-config/perfiles/${clonedProfileId}/fields/`);
    const fieldsCard = page.locator('article.card').filter({
      has: page.getByRole('heading', { name: 'Campos de la variante' })
    });
    const otherField = fieldsCard.locator('tbody tr').nth(1);
    const occupiedStart = (await otherField.locator('td').nth(2).innerText()).trim();
    await startPosition.fill(occupiedStart);
    await length.fill('1');
    await fieldName.fill('Campo con solapamiento');
    await expect(page.getByText('El campo se superpone con otro campo de la variante.')).toBeVisible();
    await expect(page.getByRole('button', { name: 'Guardar campo' })).toBeDisabled();
    expect(evidence.count('PUT', `/nacha-config/perfiles/${clonedProfileId}/fields/`)).toBe(fieldPutsBeforeInvalid);
    await page.getByRole('button', { name: 'Cancelar cambios' }).nth(1).click();
    await expect(startPosition).toHaveValue(originalStart);
    await expect(length).toHaveValue(originalLength);
    await expect(fieldName).toHaveValue(originalFieldName);

    editedFieldName = `${originalFieldName} LIVE`;
    await fieldName.fill(editedFieldName);
    const fieldResponsePromise = waitForApiResponse(
      page,
      'PUT',
      new RegExp(`/nacha-config/perfiles/${clonedProfileId}/fields/\\d+$`)
    );
    const fieldRefreshPromise = waitForApiResponse(page, 'GET', new RegExp(`/nacha-config/perfiles/${clonedProfileId}$`));
    await page.getByRole('button', { name: 'Guardar campo' }).click();
    expect((await fieldResponsePromise).status()).toBe(200);
    expect((await fieldRefreshPromise).status()).toBe(200);
    await expect(fieldName).toHaveValue(editedFieldName);

    const persistedUrl = page.url();
    const reloadFieldsPromise = waitForApiResponse(page, 'GET', new RegExp(`/nacha-config/perfiles/${clonedProfileId}$`));
    await page.reload({ waitUntil: 'domcontentloaded' });
    expect((await reloadFieldsPromise).status()).toBe(200);
    await waitForRouteReady(page, 'variantes-campos');
    await expect(page).toHaveURL(persistedUrl);
    await expect(page.locator('.profile-card')).toContainText(cloneCode);
    await expect(page.getByLabel('Nombre del campo ES')).toHaveValue(editedFieldName);

    await page.getByRole('button', { name: 'Ir a registros' }).click();
    await waitForRouteReady(page, 'registros');
    await expect(page.locator('.summary-card')).toContainText(cloneCode);

    const snapshot = evidence.snapshot();
    expect(snapshot.consoleErrors, 'Errores de consola inesperados').toEqual([]);
    expect(snapshot.pageErrors, 'Errores de página inesperados').toEqual([]);
    expect(snapshot.requestFailures, 'Solicitudes abortadas inesperadamente').toEqual([]);
    expect(snapshot.failedResponses, 'Respuestas HTTP fallidas inesperadas').toEqual([]);
    await testInfo.attach('runtime-live.json', {
      body: JSON.stringify({
        runId,
        createdCode,
        createdProfileId,
        cloneCode,
        clonedProfileId,
        editedFieldName,
        ...snapshot
      }, null, 2),
      contentType: 'application/json'
    });
  });

  for (const route of routes) {
    test(`validación responsive final de ${route.name}`, async ({ page }, testInfo) => {
      test.setTimeout(90_000);
      const evidence = observeRuntime(page);
      const query = route.name === 'perfiles' ? '' : `?profileId=${clonedProfileId}`;
      await page.goto(`${spaUrl}${route.path}${query}`, { waitUntil: 'domcontentloaded' });
      const root = page.getByTestId(route.testId);
      await expect(root).toBeVisible();
      await waitForRouteReady(page, route.name);

      const diagnostics: unknown[] = [];
      for (const viewport of viewports) {
        await page.setViewportSize(viewport);
        await expect(root).toBeVisible();
        await page.waitForFunction(() => {
          const sidebar = document.querySelector('.sidebar');
          return !sidebar || sidebar.getAnimations().every((animation) => animation.playState === 'finished');
        });
        const layout = await page.evaluate(() => ({
          bodyScrollWidth: document.body.scrollWidth,
          bodyClientWidth: document.body.clientWidth,
          documentScrollWidth: document.documentElement.scrollWidth,
          documentClientWidth: document.documentElement.clientWidth
        }));
        expect(layout.bodyScrollWidth, `${route.name} desborda el body en ${viewport.name}`).toBeLessThanOrEqual(layout.bodyClientWidth + 1);
        expect(layout.documentScrollWidth, `${route.name} desborda el documento en ${viewport.name}`).toBeLessThanOrEqual(layout.documentClientWidth + 1);
        diagnostics.push({ route: route.path, viewport, layout });
        await page.screenshot({
          path: testInfo.outputPath(`final-${route.name}-${viewport.name}.png`),
          fullPage: true
        });
      }

      const visibleText = await root.evaluate((element) => {
        const copy = element.cloneNode(true) as HTMLElement;
        copy.querySelectorAll('mat-icon, .material-icons, .material-symbols-outlined, .material-symbols-rounded')
          .forEach((icon) => icon.remove());
        return copy.innerText;
      });
      const forbiddenUiTerms = /\b(Create|Edit|Delete|Save|Cancel|Search|Actions|Status|Enabled|Disabled|Required|Loading|No records|Profile|Variant|Field)\b/i;
      expect(visibleText, `Texto de interfaz no traducido en ${route.path}`).not.toMatch(forbiddenUiTerms);
      const snapshot = evidence.snapshot();
      expect(snapshot.consoleErrors).toEqual([]);
      expect(snapshot.pageErrors).toEqual([]);
      expect(snapshot.requestFailures).toEqual([]);
      expect(snapshot.failedResponses).toEqual([]);
      await testInfo.attach('responsive-final.json', {
        body: JSON.stringify({ diagnostics, ...snapshot }, null, 2),
        contentType: 'application/json'
      });
    });
  }
});

async function login(page: Page): Promise<void> {
  await page.goto(`${spaUrl}/login`, { waitUntil: 'domcontentloaded' });
  await page.locator('input[formControlName="username"]').fill(adminUser!);
  await page.locator('input[formControlName="password"]').fill(adminPassword!);
  const loginResponsePromise = page.waitForResponse(
    response => response.url().endsWith('/auth/login') && response.request().method() === 'POST'
  );
  await page.getByRole('button', { name: 'Ingresar' }).click();
  expect((await loginResponsePromise).status()).toBe(200);
  await expect(page).not.toHaveURL(/\/login$/);
}

async function expectHealth(page: Page): Promise<void> {
  const [live, ready, spa] = await Promise.all([
    page.request.get(`${apiUrl}/health/live`),
    page.request.get(`${apiUrl}/health/ready`),
    page.request.get(`${spaUrl}/`)
  ]);
  expect(live.status()).toBe(200);
  expect(ready.status()).toBe(200);
  expect(spa.status()).toBe(200);
}

async function waitForRouteReady(page: Page, routeName: string): Promise<void> {
  if (routeName === 'perfiles') {
    await expect(page.locator('ui-grilla-empresarial .ag-root-wrapper')).toBeVisible({ timeout: 20_000 });
    await expect(page.locator('.ag-row').first()).toBeVisible({ timeout: 20_000 });
    return;
  }
  if (routeName === 'registros') {
    await expect(page.locator('.summary-card')).toBeVisible({ timeout: 20_000 });
    return;
  }
  await expect(page.locator('.profile-card .summary-grid')).toBeVisible({ timeout: 20_000 });
}

function waitForApiResponse(page: Page, method: string, url: RegExp) {
  return page.waitForResponse(response =>
    response.request().method() === method && url.test(response.url())
  );
}

function observeRuntime(page: Page): {
  count: (method: string, urlPart: string) => number;
  snapshot: () => {
    consoleErrors: string[];
    pageErrors: string[];
    requestFailures: string[];
    failedResponses: string[];
    nachaRequests: string[];
  };
} {
  const consoleErrors: string[] = [];
  const pageErrors: string[] = [];
  const requestFailures: string[] = [];
  const failedResponses: string[] = [];
  const nachaRequests: string[] = [];

  page.on('console', message => {
    if (message.type() === 'error') {
      consoleErrors.push(message.text());
    }
  });
  page.on('pageerror', error => pageErrors.push(error.message));
  page.on('requestfailed', request => {
    requestFailures.push(`${request.method()} ${request.url()} ${request.failure()?.errorText ?? ''}`.trim());
  });
  page.on('response', response => {
    if (response.status() >= 400) {
      failedResponses.push(`${response.status()} ${response.request().method()} ${response.url()}`);
    }
  });
  page.on('request', request => {
    if (/\/nacha-config\/|\/api\/ach\/nacha\/config-profiles/.test(request.url())) {
      nachaRequests.push(`${request.method()} ${request.url()}`);
    }
  });

  return {
    count: (method, urlPart) => nachaRequests.filter(item => item.startsWith(`${method} `) && item.includes(urlPart)).length,
    snapshot: () => ({ consoleErrors, pageErrors, requestFailures, failedResponses, nachaRequests })
  };
}
