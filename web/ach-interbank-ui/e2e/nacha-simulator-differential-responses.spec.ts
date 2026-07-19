import { expect, test } from '@playwright/test';
import {
  expectNoForbiddenSimulatorSideEffects,
  installControlledSimulatorHarness,
  openSimulator
} from './support/nacha-simulator-harness';

test.describe('Simulador NACHA-M - respuestas diferenciales', () => {
  test('selecciona operaciones CFA en servidor y bloquea generacion sin perfil homologado', async ({ page }, testInfo) => {
    const blockingDetail = 'No existe un perfil RETORNO/ENTRADA publicado y homologado para ACHCOL.';
    const harness = await installControlledSimulatorHarness(page, {
      previewEligible: false,
      previewMessage: blockingDetail,
      generateStatus: 409,
      generateBody: {
        type: 'https://achinterbank.local/problems/differential-profile-not-published',
        title: 'Generacion diferencial bloqueada',
        status: 409,
        detail: blockingDetail,
        code: 'DIFFERENTIAL_PROFILE_NOT_PUBLISHED'
      }
    });
    await openSimulator(page);

    await page.locator('textarea[formcontrolname="notes"]').fill('Configuracion temporal');
    let confirmationSeen = false;
    page.once('dialog', async (dialog) => {
      confirmationSeen = /limpiar/i.test(dialog.message());
      await dialog.accept();
    });
    await page.locator('button.mode-card').filter({ hasText: 'Respuestas diferenciales' }).click();
    expect(confirmationSeen).toBeTruthy();

    await expect(page.locator('button.mode-card').filter({ hasText: 'Respuestas diferenciales' })).toHaveAttribute('aria-pressed', 'true');
    await expect(page.locator('input[formcontrolname="entriesCount"]')).toHaveCount(0);
    await expect(page.locator('input[formcontrolname="amount"]')).toHaveCount(0);
    await expect(page.locator('select[formcontrolname="responseMode"]')).toBeVisible();

    const institution = page.locator('select[formcontrolname="originFinancialInstitutionId"]');
    await expect(institution.locator('option')).toHaveCount(2);
    await institution.selectOption({ index: 1 });
    await expect.poll(() => harness.eligibleQueries.length).toBeGreaterThanOrEqual(2);
    const latestQuery = harness.eligibleQueries.at(-1);
    expect(latestQuery?.get('clearingHouseCode')).toBe('ACHCOL');
    expect(latestQuery?.get('destinationFinancialInstitutionId')).toBe('100');
    expect(latestQuery?.get('state')).toBe('Pending');
    expect(latestQuery?.get('pageSize')).toBe('10');

    const eligible = page.locator('input[type="checkbox"][aria-label*="TX-CFA-501"]');
    const ineligible = page.locator('input[type="checkbox"][aria-label*="TX-CFA-502"]');
    await expect(eligible).toBeEnabled();
    await expect(ineligible).toBeDisabled();
    await eligible.check();
    await expect(page.getByText('Seleccionadas: 1.', { exact: false })).toBeVisible();

    await page.getByRole('button', { name: 'Siguiente' }).click();
    await expect.poll(() => harness.eligibleQueries.some((query) => query.get('page') === '2')).toBeTruthy();
    await expect(page.getByText('TX-CFA-511', { exact: true })).toBeVisible();

    const previewResponsePromise = page.waitForResponse((response) =>
      response.url().endsWith('/eligibility-preview') && response.request().method() === 'POST');
    await page.getByRole('button', { name: /Validar configuraci/ }).click();
    const previewResponse = await previewResponsePromise;
    expect(previewResponse.status()).toBe(200);
    const previewBody = await previewResponse.json();
    expect(previewBody).toMatchObject({
      eligible: false,
      decision: 'Blocked',
      message: blockingDetail,
      simulationMode: 'DifferentialResponses'
    });
    await expect.poll(() => harness.previewRequests.length).toBe(1);
    expect(harness.previewRequests[0]).toMatchObject({
      simulationMode: 'DifferentialResponses',
      clearingHouseCode: 'ACHCOL',
      scenarioType: 'IncomingCreditConfirmation',
      originFinancialInstitutionId: 100,
      responseMode: 'Approved',
      reasonCode: null,
      transactionReferences: ['TX-CFA-501']
    });
    const generateResponsePromise = page.waitForResponse((response) =>
      response.url().endsWith('/generate') && response.request().method() === 'POST');
    await page.getByRole('button', { name: 'Generar archivo' }).click();
    const generateResponse = await generateResponsePromise;
    expect(generateResponse.status()).toBe(409);
    const generateBody = await generateResponse.json();
    expect(generateBody).toMatchObject({
      status: 409,
      detail: blockingDetail,
      code: 'DIFFERENTIAL_PROFILE_NOT_PUBLISHED'
    });
    await expect.poll(() => harness.generateRequests.length).toBe(1);
    expect(harness.generateRequests[0].simulationMode).toBe('DifferentialResponses');
    expect(harness.generateRequests[0].transactionReferences).toEqual(['TX-CFA-501']);
    await expect(page.locator('article.toast.error').getByText(blockingDetail, { exact: true })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Archivo generado y pendiente de carga' })).toHaveCount(0);

    const screenshotPath = testInfo.outputPath('simulador-respuestas-diferenciales-bloqueado.png');
    await page.screenshot({ path: screenshotPath, fullPage: true });
    await testInfo.attach('simulador-respuestas-diferenciales-bloqueado.png', { path: screenshotPath, contentType: 'image/png' });

    let cleanupConfirmationSeen = false;
    page.once('dialog', async (dialog) => {
      cleanupConfirmationSeen = /limpiar/i.test(dialog.message());
      await dialog.accept();
    });
    await page.locator('button.mode-card').filter({ hasText: 'Transacciones entrantes' }).click();
    expect(cleanupConfirmationSeen).toBeTruthy();
    await expect(page.locator('button.mode-card').filter({ hasText: 'Transacciones entrantes' })).toHaveAttribute('aria-pressed', 'true');
    await expect(page.getByText('Operaciones CFA elegibles', { exact: true })).toHaveCount(0);
    await expect(page.locator('input[formcontrolname="entriesCount"]')).toBeVisible();

    expect(harness.consoleErrors).toEqual([
      'Failed to load resource: the server responded with a status of 409 (Conflict)'
    ]);
    harness.consoleErrors.length = 0;
    expectNoForbiddenSimulatorSideEffects(harness);
  });
});
