import { expect, Page, test } from '@playwright/test';

const enabled = process.env['RUN_JOB5C_SOAP_RESUME_LIVE'] === 'true';
const ui = (process.env['ACH_UI_URL'] ?? 'http://localhost:743').replace(/\/+$/, '');
const api = (process.env['ACH_API_URL'] ?? 'http://localhost:843').replace(/\/+$/, '');
const username = process.env['ACH_USER'] ?? 'admin';
const password = process.env['ACH_PASS'] ?? '';
const correlationId = process.env['JOB5C_SOAP_CORRELATION_ID'] ?? '';
const responseId = process.env['JOB5C_SOAP_RESPONSE_ID'] ?? '';

test.use({ trace: 'off', screenshot: 'off', video: 'off' });

test.describe.serial('JOB 5C - reanudación idempotente del intento SOAP Live', () => {
  test.skip(!enabled, 'RUN_JOB5C_SOAP_RESUME_LIVE=true habilita la reanudación controlada.');
  test.setTimeout(90_000);

  test('envía únicamente el intento pendiente ya persistido', async ({ page }) => {
    expect(correlationId).toMatch(/^JOB5C-LIVE-[A-Z0-9-]+$/);
    expect(responseId).toMatch(/^[0-9a-f-]{36}$/i);
    const token = await loginThroughUi(page);
    const headers = { Authorization: `Bearer ${token}`, 'X-Correlation-ID': correlationId };
    const detailBefore = await getDetail(page, headers);
    expect(detailBefore.notificationAttempts).toHaveLength(1);
    const attempt = detailBefore.notificationAttempts[0];
    expect(['Pendiente', 'Exitosa']).toContain(attempt.estadoNotificacion);
    expect({
      idCanal: attempt.idCanal,
      nombreCanal: attempt.nombreCanal,
      idTransaccion: attempt.idTransaccion,
      idEstado: attempt.idEstado,
      causal: attempt.causal ?? null,
      idTransaccionAxon: attempt.idTransaccionServicioExterno,
      descripcionCausal: attempt.descripcionCausal
    }).toEqual({
      idCanal: 1,
      nombreCanal: 'JOB5C-LOCAL',
      idTransaccion: attempt.idTransaccion,
      idEstado: 1,
      causal: null,
      idTransaccionAxon: 950501,
      descripcionCausal: 'Aprobada'
    });

    let first: {
      procesada: boolean;
      encontrada: boolean;
      yaProcesada: boolean;
      existeError: boolean;
      errorTecnico: boolean;
      estadoNotificacion?: string;
      estadoProcesamiento?: string;
      codigoError?: string | null;
    } | null = null;
    if (attempt.estadoNotificacion === 'Pendiente') {
      const firstResponse = await page.request.post(`${api}/api/ach/responses/notifications/send`, {
        headers,
        data: { notificationAttemptId: attempt.id, correlationId }
      });
      expect(firstResponse.ok(), await safeProblem(firstResponse)).toBeTruthy();
      first = await firstResponse.json();
      expect(first!.procesada).toBe(true);
      expect(first!.encontrada).toBe(true);
      expect(first!.yaProcesada).toBe(false);
      expect(first!.errorTecnico).toBe(false);
    }

    const secondResponse = await page.request.post(`${api}/api/ach/responses/notifications/send`, {
      headers,
      data: { notificationAttemptId: attempt.id, correlationId }
    });
    expect(secondResponse.ok(), await safeProblem(secondResponse)).toBeTruthy();
    const second = await secondResponse.json() as {
      procesada: boolean;
      encontrada: boolean;
      yaProcesada: boolean;
      estadoNotificacion?: string;
    };
    expect(second.procesada).toBe(true);
    expect(second.encontrada).toBe(true);
    expect(second.yaProcesada).toBe(true);

    const detailAfter = await getDetail(page, headers);
    expect(detailAfter.notificationAttempts).toHaveLength(1);
    expect(detailAfter.notificationAttempts[0].estadoNotificacion).toBe('Exitosa');

    await page.goto(`${ui}/ach-responses/${responseId}`);
    await expect(page.getByRole('heading', { name: /Detalle respuesta ACH/i }).first()).toBeVisible();
    expect(await page.locator('body').innerText()).not.toContain('[object Object]');
    await page.goto(`${ui}/ach/reconciliation`);
    await expect(page.getByRole('heading', { name: 'Consola de conciliación ACH', exact: true })).toBeVisible();
    expect(await page.locator('body').innerText()).not.toContain('[object Object]');

    console.log(`JOB5C_SOAP_RESUME_RESULT=${JSON.stringify({
      correlationId,
      responseId,
      attemptId: attempt.id,
      parameterNames: [
        'idCanal',
        'nombreCanal',
        'idTransaccion',
        'idEstado',
        'causal',
        'idTransaccionAxon',
        'descripcionCausal'
      ],
      firstSend: first === null ? 'already-persisted' : {
        existeError: first.existeError,
        errorTecnico: first.errorTecnico,
        estadoNotificacion: first.estadoNotificacion,
        estadoProcesamiento: first.estadoProcesamiento,
        codigoError: first.codigoError ?? null
      },
      secondSend: {
        yaProcesada: second.yaProcesada,
        estadoNotificacion: second.estadoNotificacion
      },
      persistedAttempts: detailAfter.notificationAttempts.length
    })}`);
  });
});

async function loginThroughUi(page: Page): Promise<string> {
  expect(password, 'ACH_PASS es obligatorio para la prueba Live local.').not.toBe('');
  await page.goto(`${ui}/login`);
  const loginResponsePromise = page.waitForResponse(response =>
    new URL(response.url()).pathname === '/auth/login'
    && response.request().method() === 'POST'
    && response.status() === 200);
  await page.locator('input[formControlName="username"]').fill(username);
  await page.locator('input[formControlName="password"]').fill(password);
  await page.getByRole('button', { name: 'Ingresar' }).click();
  const loginResponse = await loginResponsePromise;
  const payload = await loginResponse.json() as { data?: { token?: string } };
  expect(payload.data?.token).toBeTruthy();
  await expect(page).not.toHaveURL(/\/login(?:\?.*)?$/);
  return payload.data!.token!;
}

async function getDetail(page: Page, headers: Record<string, string>): Promise<any> {
  const response = await page.request.get(`${api}/api/ach/responses/${responseId}`, { headers });
  expect(response.ok(), await safeProblem(response)).toBeTruthy();
  return response.json();
}

async function safeProblem(response: { status(): number; text(): Promise<string> }): Promise<string> {
  const text = await response.text();
  return `HTTP ${response.status()}; bodyLength=${text.length}`;
}
