import { expect, test } from '@playwright/test';
import {
  expectNoForbiddenSimulatorSideEffects,
  expectSimulatorFitsViewport,
  installControlledSimulatorHarness,
  openSimulator,
  requiredSimulatorViewports
} from './support/nacha-simulator-harness';

test.describe('Simulador NACHA-M - transacciones entrantes', () => {
  test('genera una operacion nueva con contrato explicito y sin efectos laterales', async ({ page }, testInfo) => {
    const harness = await installControlledSimulatorHarness(page);
    await openSimulator(page);

    const incomingMode = page.locator('button.mode-card').filter({ hasText: 'Transacciones entrantes' });
    const differentialMode = page.locator('button.mode-card').filter({ hasText: 'Respuestas diferenciales' });
    await expect(incomingMode).toHaveAttribute('aria-pressed', 'true');
    await expect(differentialMode).toHaveAttribute('aria-pressed', 'false');
    await expect(page.locator('.badge-simulation')).toContainText('SIMULACI');
    await expect(page.getByText('UAT LOCAL', { exact: true })).toBeVisible();

    const institution = page.locator('select[formcontrolname="originFinancialInstitutionId"]');
    await expect(institution.locator('option')).toHaveCount(2);
    await institution.selectOption({ index: 1 });

    const previewResponsePromise = page.waitForResponse((response) =>
      response.url().endsWith('/eligibility-preview') && response.request().method() === 'POST');
    await page.getByRole('button', { name: /Validar configuraci/ }).click();
    const previewResponse = await previewResponsePromise;
    expect(previewResponse.status()).toBe(200);
    await expect.poll(() => harness.previewRequests.length).toBe(1);
    expect(harness.previewRequests[0]).toMatchObject({
      simulationMode: 'IncomingTransactions',
      clearingHouseCode: 'ACHCOL',
      scenarioType: 'IncomingCredit',
      originFinancialInstitutionId: 100,
      transactionReferences: []
    });

    const generateResponsePromise = page.waitForResponse((response) =>
      response.url().endsWith('/generate') && response.request().method() === 'POST');
    await page.getByRole('button', { name: 'Generar archivo' }).click();
    const generateResponse = await generateResponsePromise;
    expect(generateResponse.status()).toBe(201);
    await expect.poll(() => harness.generateRequests.length).toBe(1);
    const result = page.locator('section.result');
    await expect(result.getByRole('heading', { name: 'Archivo generado y pendiente de carga' })).toBeVisible();
    await expect(result.getByText('0001283.001.20260718.1.OUT', { exact: true })).toBeVisible();
    await expect(result.getByText('A'.repeat(64), { exact: true })).toBeVisible();
    await expect(result.getByText('Debe cargarse manualmente por NachaUpload.', { exact: false })).toBeVisible();

    const payload = harness.generateRequests[0];
    expect(payload.simulationMode).toBe('IncomingTransactions');
    expect(payload.transactionReferences).toEqual([]);
    expect(payload.entriesCount).toBe(1);
    expect(payload.amount).toBe(1000);
    expect(payload.originFinancialInstitutionId).toBe(100);
    expect('0001283.001.20260718.1.OUT').toMatch(/^\d{7}\.\d{3}\.\d{8}\.\d+\.OUT$/);
    expect('0001283.001.20260718.1.OUT').not.toMatch(/\.ach$/i);

    const screenshotPath = testInfo.outputPath('simulador-transacciones-entrantes.png');
    await page.screenshot({ path: screenshotPath, fullPage: true });
    await testInfo.attach('simulador-transacciones-entrantes.png', { path: screenshotPath, contentType: 'image/png' });

    expectNoForbiddenSimulatorSideEffects(harness);
  });

  test('mantiene selector y acciones sin overflow en los cuatro viewports requeridos', async ({ page }) => {
    const harness = await installControlledSimulatorHarness(page);
    await page.setViewportSize(requiredSimulatorViewports[0]);
    await openSimulator(page);

    for (const viewport of requiredSimulatorViewports) {
      await expectSimulatorFitsViewport(page, viewport);
    }

    expectNoForbiddenSimulatorSideEffects(harness);
  });
});
