import { expect, Page, test } from '@playwright/test';

const username = process.env['ACH_USER'];
const password = process.env['ACH_PASS'];
const viewOnlyToken = process.env['E2E_SCHEDULER_VIEW_TOKEN'];

test.describe.serial('administracion real del scheduler', () => {
  test.beforeAll(() => {
    const missing = [
      ['ACH_USER', username],
      ['ACH_PASS', password],
      ['E2E_SCHEDULER_VIEW_TOKEN', viewOnlyToken]
    ].filter(([, value]) => !value).map(([name]) => name);

    if (missing.length) {
      throw new Error(`Faltan variables obligatorias para el E2E real del scheduler: ${missing.join(', ')}.`);
    }
  });

  test('dashboard, historial, ejecucion segura, pausa, reanudacion y programacion', async ({ page }, testInfo) => {
    test.setTimeout(70_000);
    await login(page);
    await page.goto('/scheduler/tasks');

    await expect(page.getByRole('heading', { name: 'Administración de tareas programadas', level: 1 })).toBeVisible();
    await expect(page.getByText('ACHInterbankScheduler', { exact: true })).toBeVisible();
    const tasks = page.getByRole('region', { name: 'Tareas' });
    await expect(tasks.getByText('SCHEDULER_CLUSTER_PROBE', { exact: true })).toBeVisible();
    const instances = page.getByRole('region', { name: 'Instancias del clúster' });
    await expect(instances.getByRole('heading', { name: 'Instancias del clúster' })).toBeVisible();
    expect(await instances.locator('article').filter({ hasText: 'achinterbank-api-01' }).count()).toBeGreaterThan(0);
    expect(await instances.locator('article').filter({ hasText: 'achinterbank-api-02' }).count()).toBeGreaterThan(0);
    await expect(page.locator('body')).not.toContainText('[object Object]');

    await openActions(page);
    await page.getByRole('button', { name: 'Ejecutar ahora' }).click();
    const manualDialog = page.getByRole('dialog', { name: 'Ejecutar tarea ahora' });
    await expect(manualDialog).toBeVisible();
    const submit = manualDialog.getByRole('button', { name: 'Ejecutar ahora' });
    await expect(submit).toBeDisabled();
    await manualDialog.getByLabel('Motivo').fill('Ejecucion E2E autorizada desde la administracion');
    await submit.dblclick();
    await expect(manualDialog).toBeHidden();
    const history = page.getByRole('region', { name: 'Historial funcional' });
    expect(await history.getByRole('cell', { name: 'Manual' }).count()).toBeGreaterThan(0);

    await openActions(page);
    await page.getByRole('button', { name: 'Ejecutar ahora' }).click();
    await manualDialog.getByLabel('Motivo').fill('Segundo clic E2E durante una ejecucion activa');
    await submit.click();
    await expect(manualDialog).toBeVisible();
    await expect(page.getByText(/ejecución activa/i)).toBeVisible();
    await manualDialog.getByRole('button', { name: 'Cancelar' }).click();

    await openActions(page);
    await page.getByRole('button', { name: 'Pausar' }).click();
    await openActions(page);
    await expect(page.getByRole('button', { name: 'Reanudar' })).toBeVisible();
    await page.getByRole('button', { name: 'Reanudar' }).click();

    await openActions(page);
    await page.getByRole('button', { name: 'Editar programación' }).click();
    const scheduleDialog = page.getByRole('dialog', { name: 'Editar programación' });
    await expect(scheduleDialog).toBeVisible();
    await expect(scheduleDialog.getByLabel('Zona horaria')).toHaveValue('America/Bogota');
    await scheduleDialog.getByRole('button', { name: 'Vista previa' }).click();
    await expect(scheduleDialog.getByText('Próximas ejecuciones:')).toBeVisible();
    await scheduleDialog.getByRole('button', { name: 'Cancelar' }).click();

    await page.screenshot({ path: testInfo.outputPath('scheduler-desktop.png'), fullPage: true });
  });

  test('oculta acciones no autorizadas y conserva vista movil', async ({ page }, testInfo) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await page.addInitScript((token) => {
      window.sessionStorage.setItem('ach.interbank.access_token', token);
    }, viewOnlyToken!);
    await page.goto('/scheduler/tasks');

    await expect(page.getByRole('heading', { name: 'Administración de tareas programadas', level: 1 })).toBeVisible();
    await openActions(page);
    await expect(page.getByRole('button', { name: 'Ver detalle' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Ejecutar ahora' })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Pausar' })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Editar programación' })).toHaveCount(0);
    await expect(page.locator('body')).not.toContainText('[object Object]');
    await page.screenshot({ path: testInfo.outputPath('scheduler-mobile-view-only.png'), fullPage: true });
  });
});

async function login(page: Page): Promise<void> {
  await page.goto('/login');
  await page.locator('input[formcontrolname="username"]').fill(username!);
  await page.locator('input[formcontrolname="password"]').fill(password!);
  await page.getByRole('button', { name: 'Ingresar' }).click();
  await page.waitForURL(/\/dashboard/);
}

async function openActions(page: Page): Promise<void> {
  const tasks = page.getByRole('region', { name: 'Tareas' });
  const row = tasks.getByRole('row').filter({ hasText: 'SCHEDULER_CLUSTER_PROBE' });
  const details = row.locator('details.action-menu');
  if ((await details.getAttribute('open')) === null) {
    await row.getByLabel('Abrir acciones').click();
  }
}
