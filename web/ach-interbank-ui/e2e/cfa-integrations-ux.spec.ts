import { expect, test, type Page } from '@playwright/test';
import { installCfaIntegrationsMock, type CfaMockOptions, type CfaMockState } from './support/cfa-integrations-fixture';

const METHOD = 'Proc_Transacciones';
const CENTER_URL = '/integraciones';
const MAPPING_URL = `/integraciones/relacion-informacion?method=${METHOD}`;
const CONTRACT_URL = `/integraciones/contrato?method=${METHOD}`;
const CONNECTION_URL = `/integraciones/conexion?method=${METHOD}`;

async function openWithMock(page: Page, url: string, options: CfaMockOptions = {}): Promise<CfaMockState> {
  const state = await installCfaIntegrationsMock(page, options);
  await page.goto(url);
  return state;
}

async function configurePendingField(page: Page): Promise<void> {
  const pending = page.getByTestId('mapping-parameter-item').filter({ hasText: 'Número de referencia' });
  await pending.getByTestId('mapping-edit-button').click();
  await expect(pending.getByTestId('mapping-parameter-editor')).toBeVisible();

  await pending.getByTestId('mapping-source-search').fill('Referencia');
  await pending.getByTestId('mapping-source-option').filter({ hasText: 'Referencia de la transacción' }).click();
  await pending.getByTestId('mapping-transformation-selector').click();
  await page.getByRole('option', { name: /Recortar/ }).click();
  await pending.getByTestId('mapping-save-button').click();
}

test.describe('Integraciones CFA · cobertura UX P7.5 preparada para P8', () => {
  test('abre el Centro, prioriza nombres funcionales y navega a los tres servicios sin rutas técnicas redundantes', async ({ page }) => {
    await openWithMock(page, CENTER_URL);

    await expect(page.getByRole('heading', { level: 1, name: 'Centro de integración CFA' })).toBeVisible();
    for (const title of ['Créditos recibidos en CFA', 'Débitos originados en CFA', 'Respuestas de transacciones']) {
      await expect(page.getByRole('heading', { name: title })).toBeVisible();
    }
    for (const technicalName of ['Proc_Transacciones', 'Proc_Contrapartidas', 'RegistrarRespuestaTransaccion']) {
      await expect(page.getByText(technicalName, { exact: true })).toBeVisible();
    }

    await expect(page.locator('a[href*="/integraciones/mappings"], a[href*="/integraciones/soap-settings"]')).toHaveCount(0);
    await expect(page.getByTestId('service-mapping-link')).toHaveCount(3);

    const expectedMethods = ['Proc_Transacciones', 'Proc_Contrapartidas', 'RegistrarRespuestaTransaccion'];
    for (let index = 0; index < expectedMethods.length; index += 1) {
      await page.getByTestId('service-mapping-link').nth(index).click();
      await expect(page).toHaveURL(new RegExp(`/integraciones/relacion-informacion\\?method=${expectedMethods[index]}$`));
      await expect(page.getByRole('heading', { level: 1, name: 'Relación de información' })).toBeVisible();
      await page.goto(CENTER_URL);
    }
  });

  test('filtra pendientes, edita origen/tratamiento, refresca desde backend, valida y publica con cancelación sin mutación', async ({ page }) => {
    const state = await openWithMock(page, MAPPING_URL);

    await expect(page.getByTestId('mapping-publication-pending-summary')).toHaveText('1 dato nuevo pendiente');
    await expect(page.getByTestId('mapping-publication-cta')).toBeDisabled();
    await expect(page.getByTestId('mapping-general-validation')).toContainText('Requiere atención');
    await expect(page.getByTestId('mapping-sanitized-preview-value').first()).toContainText('••••9012');

    await page.getByTestId('mapping-search').fill('Número de referencia');
    await page.getByTestId('mapping-status-filter').click();
    await page.getByRole('option', { name: /Pendiente/ }).click();
    await expect(page.getByTestId('mapping-parameter-item')).toHaveCount(1);
    await expect(page.getByTestId('mapping-parameter-item')).toContainText('Número de referencia');

    await configurePendingField(page);
    await expect.poll(() => state.mappingWrites).toBe(1);
    await expect(page.getByTestId('coverage-complete')).toHaveText('2');
    await expect(page.getByTestId('mapping-publication-pending-summary')).toHaveText('0 datos requeridos pendientes');
    await page.getByTestId('mapping-preview-refresh').click();
    await expect(page.getByTestId('mapping-general-validation')).toContainText('Sin errores de validación');
    await expect(page.getByTestId('mapping-publication-cta')).toBeEnabled();

    await page.getByTestId('mapping-publication-cta').click();
    const dialog = page.getByRole('alertdialog', { name: /Confirmar publicación de configuración CFA/ });
    await expect(dialog).toBeVisible();
    await dialog.getByRole('button', { name: 'Cancelar' }).click();
    await expect(dialog).toBeHidden();
    expect(state.publishWrites).toBe(0);
    await expect(page.getByText('Borrador', { exact: true }).first()).toBeVisible();

    await page.getByTestId('mapping-publication-cta').click();
    const confirmation = page.getByRole('alertdialog', { name: /Confirmar publicación de configuración CFA/ });
    await confirmation.getByTestId('mapping-publication-confirm').click();
    await expect.poll(() => state.publishWrites).toBe(1);
    await expect(page.getByTestId('mapping-publication-success')).toContainText('workspace actualizado desde el servicio');
    await expect(page.getByText(/ya no está en borrador/i)).toBeVisible();
  });

  test('mantiene la publicación bloqueada mientras backend reporta un required pendiente', async ({ page }) => {
    const state = await openWithMock(page, MAPPING_URL);

    await expect(page.getByTestId('mapping-publication-required-pending')).toContainText('Número de referencia');
    await expect(page.getByTestId('mapping-publication-cta')).toBeDisabled();
    await page.getByTestId('mapping-publication-review').click();

    const dialog = page.getByRole('alertdialog', { name: /Confirmar publicación de configuración CFA/ });
    await expect(dialog.getByTestId('mapping-publication-confirm')).toBeDisabled();
    await dialog.getByRole('button', { name: 'Cancelar' }).click();
    expect(state.publishWrites).toBe(0);
  });

  test('humaniza conflicto Revision y exige recarga antes de continuar', async ({ page }) => {
    const state = await openWithMock(page, MAPPING_URL);
    state.mappingConflictOnce = true;

    await configurePendingField(page);
    await expect.poll(() => state.mappingWrites).toBe(1);
    await expect(page.getByTestId('mapping-revision-conflict')).toContainText('La configuración cambió en el servidor');
    await expect(page.getByTestId('mapping-revision-technical-detail')).not.toHaveAttribute('open', '');

    await page.getByTestId('mapping-revision-reload').click();
    await expect(page.getByTestId('mapping-revision-conflict')).toHaveCount(0);
    await expect(page.getByTestId('coverage-pending')).toHaveText('1');
  });

  test('lista versiones y completa WSDL stepper con diff backend y creación Draft-only', async ({ page }) => {
    const state = await openWithMock(page, CONTRACT_URL);

    await expect(page.getByRole('heading', { level: 2, name: 'Borrador' })).toBeVisible();
    await expect(page.getByRole('heading', { level: 2, name: 'Activa' })).toBeVisible();
    await expect(page.getByRole('heading', { level: 2, name: 'Retirada' })).toBeVisible();
    await expect(page.getByText('Solo lectura', { exact: true })).toHaveCount(2);

    await page.getByRole('button', { name: 'Importar WSDL/XSD como borrador' }).click();
    await expect(page.getByRole('heading', { name: /Importar WSDL\/XSD/ })).toBeVisible();

    const fileInput = page.locator('input[type="file"]');
    await expect(fileInput).toHaveAttribute('accept', /\.wsdl.*\.xsd/);
    await fileInput.setInputFiles({
      name: 'contrato-cfa.wsdl',
      mimeType: 'text/xml',
      buffer: Buffer.from('<definitions name="CFA"/>')
    });
    await page.getByRole('button', { name: 'Analizar con backend' }).click();

    await expect(page.getByLabel('Operación WSDL compatible')).toBeVisible();
    await page.getByLabel('Operación WSDL compatible').click();
    await page.getByRole('option', { name: 'Proc_Transacciones', exact: true }).click();
    await page.getByRole('button', { name: 'Validar operación con backend' }).click();
    await page.getByRole('button', { name: 'Continuar' }).click();

    await expect(page.getByRole('heading', { name: '3. Resumen del contrato detectado' })).toBeVisible();
    await page.getByLabel('Versión contractual base').click();
    await page.getByRole('option', { name: /v2-activa/ }).click();
    await page.getByRole('button', { name: 'Comparar con versión base' }).click();

    await expect(page.getByText('Requiere relacionar', { exact: true }).first()).toBeVisible();
    await expect(page.getByText('Requiere desarrollo', { exact: true }).first()).toBeVisible();
    for (const change of ['Dato nuevo', 'Dato eliminado', 'Obligatoriedad', 'Tipo', 'Cardinalidad', 'Orden', 'Namespace', 'Dirección']) {
      await expect(page.getByText(change, { exact: false }).first()).toBeVisible();
    }

    await page.getByRole('button', { name: 'Revisado, continuar' }).click();
    await expect(page.getByText('Resultado esperado: Borrador.')).toBeVisible();
    await page.getByLabel('Versión del nuevo borrador').fill('v4-qa-e2e');
    expect(state.contractImports).toBe(0);
    await page.getByRole('button', { name: 'Crear nueva versión Borrador' }).click();

    await expect.poll(() => state.contractImports).toBe(1);
    await expect(page.getByRole('heading', { name: 'Borrador creado' })).toBeVisible();
    await expect(page.getByText(/estado Draft/i)).toBeVisible();
    await expect(page.getByText(/estado Active/i)).toHaveCount(0);
  });

  test('respeta permisos solo lectura en relación, contratos y conexión', async ({ page }) => {
    await openWithMock(page, MAPPING_URL, { permissions: ['CanReadAch'] });
    await expect(page.getByTestId('mapping-edit-button')).toHaveCount(0);
    await expect(page.getByTestId('mapping-publication-cta')).toHaveCount(0);

    await page.goto(CONTRACT_URL);
    await expect(page.getByRole('button', { name: 'Importar WSDL/XSD como borrador' })).toHaveCount(0);
    await expect(page.getByText('Solo lectura', { exact: true })).toHaveCount(2);

    await page.goto(CONNECTION_URL);
    await expect(page.getByRole('heading', { level: 1, name: 'Conexión técnica avanzada' })).toBeVisible();
    await expect(page.getByTestId('soap-edit')).toHaveCount(0);
  });

  test('confirma envío real con destino sanitizado, permite cancelar sin mutación y refresca backend después de guardar', async ({ page }) => {
    const state = await openWithMock(page, CONNECTION_URL);

    await expect(page.getByRole('heading', { level: 1, name: 'Conexión técnica avanzada' })).toBeVisible();
    await expect(page.getByTestId('soap-sanitized-endpoint')).toContainText('https://cfa.example.test/services/Proc_Transacciones');
    await expect(page.getByTestId('soap-sanitized-endpoint')).not.toContainText('?');

    await page.getByTestId('soap-edit').click();
    await page.getByTestId('soap-operating-mode').click();
    await page.getByRole('option', { name: 'Envío real' }).click();
    await page.getByTestId('soap-save').click();

    const firstDialog = page.getByRole('alertdialog', { name: /Confirmar envío real para Créditos recibidos en CFA/ });
    await expect(firstDialog).toBeVisible();
    await expect(firstDialog.getByTestId('real-send-sanitized-destination')).toContainText('https://cfa.example.test/services/Proc_Transacciones');
    await expect(firstDialog.getByTestId('real-send-sanitized-destination')).not.toContainText('?');
    await firstDialog.getByRole('button', { name: 'Cancelar' }).click();
    expect(state.connectionWrites).toBe(0);

    await page.getByTestId('soap-save').click();
    const confirmDialog = page.getByRole('alertdialog', { name: /Confirmar envío real para Créditos recibidos en CFA/ });
    await confirmDialog.getByTestId('confirm-real-send').click();
    await expect.poll(() => state.connectionWrites).toBe(1);
    await expect(page.getByText('Envío real habilitado', { exact: true }).first()).toBeVisible();
    expect(state.currentConnectionRevision).toBe('rev-8');
  });

  test('humaniza conflicto Revision de conexión y requiere recarga', async ({ page }) => {
    const state = await openWithMock(page, CONNECTION_URL);
    state.connectionConflictOnce = true;

    await page.getByTestId('soap-edit').click();
    await page.getByTestId('soap-operating-mode').click();
    await page.getByRole('option', { name: 'Inactivo' }).click();
    await page.getByTestId('soap-save').click();

    await expect(page.getByTestId('soap-revision-conflict')).toContainText('La configuración cambió en backend');
    await expect(page.getByTestId('soap-save')).toBeDisabled();
    await page.getByRole('button', { name: 'Recargar configuración' }).click();
    await expect(page.getByTestId('soap-revision-conflict')).toHaveCount(0);
  });
});
