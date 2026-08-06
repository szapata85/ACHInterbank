import { expect, Page, Response, test } from '@playwright/test';

const username = process.env['ACH_USER'];
const password = process.env['ACH_PASS'];
const viewOnlyToken = process.env['E2E_SCHEDULER_VIEW_TOKEN'];
const safeTaskName = 'Preparar ciclos operativos';

test.describe.serial('administración empresarial del programador', () => {
  test.beforeAll(() => {
    const missing = [
      ['ACH_USER', username],
      ['ACH_PASS', password]
    ].filter(([, value]) => !value).map(([name]) => name);

    if (missing.length) {
      throw new Error(`Faltan variables obligatorias para el E2E real del programador: ${missing.join(', ')}.`);
    }
  });

  test('presenta información funcional, constructor visual y ejecución manual sin mover la próxima fecha', async ({ page }, testInfo) => {
    test.setTimeout(90_000);
    await login(page);

    const initialLoad = expectSchedulerApiLoad(page);
    await page.goto('/scheduler/tasks');
    const initialTasks = await initialLoad;

    await expect(page.getByRole('heading', { name: 'Tareas programadas', level: 1 })).toBeVisible();
    await expect(page.getByText('Consulta y administra los procesos automáticos que mantienen actualizada la información necesaria para la operación ACH.')).toBeVisible();
    const tasksTable = page.getByRole('table', { name: 'Tareas programadas' });
    await expect(tasksTable.locator('td').filter({ hasText: safeTaskName })).toBeVisible();
    await expect(tasksTable.locator('td').filter({ hasText: 'Actualizar días festivos' })).toBeVisible();
    await expect(tasksTable.locator('td').filter({ hasText: 'Actualizar ciclos de compensación' })).toBeVisible();
    await expect(page.locator('body')).not.toContainText('[object Object]');
    await expect(page.locator('body')).not.toContainText(/\b(Store|Jobs|Misfire|DoNothing|FireAndProceed|Heartbeat|Recovery|Request ID|Correlation ID|Synchronized|Online|Offline)\b/);

    const before = initialTasks.find(task => task.name === safeTaskName);
    expect(before, `No se encontró la tarea segura «${safeTaskName}».`).toBeTruthy();

    await openDesktopActions(page, safeTaskName);
    await page.getByRole('menuitem', { name: 'Editar programación' }).click();
    const scheduleDialog = page.getByRole('dialog', { name: 'Editar programación' });
    await expect(scheduleDialog).toBeVisible();
    await expect(scheduleDialog.getByText('Todas las horas corresponden a la hora de Colombia.')).toBeVisible();
    await expect(scheduleDialog.getByText('Próximas cinco ejecuciones')).toBeVisible();
    await expect(scheduleDialog.locator('.preview-panel li')).toHaveCount(5);
    await scheduleDialog.getByRole('button', { name: 'Cancelar' }).click();

    await openDesktopActions(page, safeTaskName);
    await page.getByRole('menuitem', { name: 'Consultar información técnica' }).click();
    const detailDialog = page.getByRole('dialog', { name: 'Detalle de la tarea' });
    await expect(detailDialog).toBeVisible();
    await expect(detailDialog.getByText('Información técnica', { exact: true })).toBeVisible();
    await detailDialog.getByText('Información técnica', { exact: true }).click();
    await expect(detailDialog.getByText('Identificador interno', { exact: true })).toBeVisible({ timeout: 20_000 });
    await detailDialog.getByRole('button', { name: 'Cerrar' }).click();

    let executeRequests = 0;
    page.on('request', request => {
      if (/\/api\/scheduler\/tasks\/[^/]+\/execute$/i.test(new URL(request.url()).pathname)) executeRequests++;
    });

    await openDesktopActions(page, safeTaskName);
    await page.getByRole('menuitem', { name: 'Ejecutar ahora' }).click();
    const manualDialog = page.getByRole('dialog', { name: 'Ejecutar tarea' });
    await expect(manualDialog).toBeVisible();
    await expect(manualDialog.getByText(`Está a punto de ejecutar «${safeTaskName}» antes de su próxima fecha programada.`)).toBeVisible();
    const submit = manualDialog.getByRole('button', { name: 'Ejecutar ahora' });
    await expect(submit).toBeDisabled();
    await manualDialog.getByLabel('Motivo de la ejecución extraordinaria').fill('Validación E2E autorizada del flujo extraordinario mediante Quartz.');

    const executeResponse = page.waitForResponse(response => /\/api\/scheduler\/tasks\/[^/]+\/execute$/i.test(new URL(response.url()).pathname));
    const refreshedTasks = page.waitForResponse(response => new URL(response.url()).pathname === '/api/scheduler/tasks' && response.status() === 200);
    const refreshedHistory = page.waitForResponse(response => new URL(response.url()).pathname === '/api/scheduler/history' && response.status() === 200);
    await submit.dblclick();
    expect((await executeResponse).status()).toBe(202);
    await expect(manualDialog).toBeHidden();
    await expect(page.getByText('La ejecución fue solicitada correctamente. Puedes consultar su progreso en el historial.')).toBeVisible();
    expect(executeRequests).toBe(1);

    const afterTasks = await (await refreshedTasks).json() as SchedulerTaskResponse[];
    await refreshedHistory;
    const after = afterTasks.find(task => task.name === safeTaskName);
    expect(after?.nextExecutionUtc ?? null).toBe(before?.nextExecutionUtc ?? null);
    const manualHistory = page.getByRole('table', { name: 'Historial de ejecuciones' }).getByText('Manual', { exact: true });
    expect(await manualHistory.count()).toBeGreaterThan(0);
    await expect(manualHistory.first()).toBeVisible();

    await page.locator('.tasks-panel .desktop-table').evaluate(element => { element.scrollLeft = 0; });
    await page.locator('#main-content').evaluate(element => { element.scrollTop = 0; });
    await page.screenshot({ path: testInfo.outputPath('scheduler-desktop.png'), fullPage: true });
  });

  test('conserva una experiencia adaptable en móvil', async ({ page }, testInfo) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await login(page);
    const load = expectSchedulerApiLoad(page);
    await page.goto('/scheduler/tasks');
    await load;

    await expect(page.getByRole('heading', { name: 'Tareas programadas', level: 1 })).toBeVisible();
    const card = page.locator('mat-card.task-card').filter({ hasText: safeTaskName });
    await expect(card).toBeVisible();
    await expect(page.locator('.tasks-panel .desktop-table')).toBeHidden();
    await card.getByLabel(`Acciones de ${safeTaskName}`).click();
    await expect(page.getByRole('menuitem', { name: 'Ver detalle' })).toBeVisible();
    await expect(page.getByRole('menuitem', { name: 'Ejecutar ahora' })).toBeVisible();
    const actionsMenu = page.getByRole('menu');
    await page.keyboard.press('Escape');
    await expect(actionsMenu).toBeHidden();
    await page.locator('#main-content').evaluate(element => { element.scrollTop = 0; });
    await page.screenshot({ path: testInfo.outputPath('scheduler-mobile.png'), fullPage: true });
    await card.scrollIntoViewIfNeeded();
    await page.screenshot({ path: testInfo.outputPath('scheduler-mobile-tasks.png'), fullPage: true });
  });

  test('oculta acciones no autorizadas con un token de solo consulta', async ({ page }) => {
    test.skip(!viewOnlyToken, 'No se proporcionó E2E_SCHEDULER_VIEW_TOKEN para la comprobación real de permisos en navegador.');
    await page.addInitScript(token => window.sessionStorage.setItem('ach.interbank.access_token', token), viewOnlyToken!);
    const load = expectSchedulerApiLoad(page, false);
    await page.goto('/scheduler/tasks');
    await load;

    await openDesktopActions(page, safeTaskName);
    await expect(page.getByRole('menuitem', { name: 'Ver detalle' })).toBeVisible();
    await expect(page.getByRole('menuitem', { name: 'Ejecutar ahora' })).toHaveCount(0);
    await expect(page.getByRole('menuitem', { name: 'Editar programación' })).toHaveCount(0);
    await expect(page.getByRole('menuitem', { name: 'Desactivar' })).toHaveCount(0);
    await expect(page.getByRole('menuitem', { name: 'Consultar información técnica' })).toHaveCount(0);
  });
});

interface SchedulerTaskResponse {
  name: string;
  nextExecutionUtc: string | null;
}

async function login(page: Page): Promise<void> {
  await page.goto('/login');
  await page.locator('input[formcontrolname="username"]').fill(username!);
  await page.locator('input[formcontrolname="password"]').fill(password!);
  await page.getByRole('button', { name: 'Ingresar' }).click();
  await page.waitForURL(/\/dashboard/);
}

async function openDesktopActions(page: Page, taskName: string): Promise<void> {
  const row = page.getByRole('table', { name: 'Tareas programadas' }).getByRole('row').filter({ hasText: taskName });
  await row.getByLabel(`Acciones de ${taskName}`).click();
}

async function expectSchedulerApiLoad(page: Page, includeHistory = true): Promise<SchedulerTaskResponse[]> {
  const endpoints = ['/api/scheduler/overview', '/api/scheduler/tasks'];
  if (includeHistory) endpoints.push('/api/scheduler/history');

  const responses = await Promise.all(endpoints.map(endpoint => page.waitForResponse(candidate => new URL(candidate.url()).pathname === endpoint)));
  await Promise.all(responses.map(expectSuccessfulSchedulerResponse));
  return await responses[endpoints.indexOf('/api/scheduler/tasks')].json() as SchedulerTaskResponse[];
}

async function expectSuccessfulSchedulerResponse(response: Response): Promise<void> {
  if (response.status() === 200) return;
  const retryAfter = response.headers()['retry-after'] ?? 'no enviado';
  const body = sanitizeResponseBody(await response.text());
  throw new Error(`La SPA recibió una respuesta inesperada del programador. URL=${response.url()} status=${response.status()} Retry-After=${retryAfter} body=${body}`);
}

function sanitizeResponseBody(body: string): string {
  return body
    .replace(/"(?:access_?token|token|password|authorization|account|document|email)"\s*:\s*"[^"]*"/gi, '"datoSensible":"[redacted]"')
    .replace(/\bBearer\s+[A-Za-z0-9._~+/=-]+/gi, 'Bearer [redacted]')
    .replace(/\s+/g, ' ')
    .trim()
    .slice(0, 1000) || '[cuerpo vacío]';
}
