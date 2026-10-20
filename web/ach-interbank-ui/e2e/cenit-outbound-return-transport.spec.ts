import { expect, Page, test } from '@playwright/test';
import { loginThroughUi } from './support/live-ui-auth';

type MonitoringItem = { id: number; transactionExternalId: string; clearingHouseDisplayName: string };
type TransportDetail = {
  summary: { transactionExternalId: string; clearingHouseDisplayName: string };
  files: Array<{
    fileName: string;
    operationDisplayName: string;
    transmissionReference?: string;
    lifecycleStatusCode: string;
    transportAttempts: Array<{ attemptNumber: number; statusCode: string }>;
    transportResults: Array<{ id: string; outcomeCode: string; resultCode: string }>;
  }>;
  timeline: Array<{ stageCode: string; outcomeCode: string }>;
};

const enabled = process.env['RUN_CENIT_RETURN_TRANSPORT_E2E'] === 'true';
const api = (process.env['ACH_API_URL'] ?? 'http://localhost:843').replace(/\/+$/, '');
const externalId = 'UAT-F4-MON-SAL-14-CENIT-RETURN-TRANSPORT';

test.skip(!enabled, 'RUN_CENIT_RETURN_TRANSPORT_E2E=true es obligatorio para esta suite local controlada.');

test('CENIT Return Out — transporte, acuse, refresh y replay idempotente', async ({ page }) => {
  await loginThroughUi(page);
  const token = await page.evaluate(() => sessionStorage.getItem('ach.interbank.access_token'));
  expect(token).toBeTruthy();

  const list = await getJson<{ items: MonitoringItem[] }>(page, token!,
    `/api/transactions/outgoing-monitoring?transactionExternalId=${encodeURIComponent(externalId)}&pageNumber=1&pageSize=10`);
  expect(list.items).toHaveLength(1);
  expect(list.items[0].clearingHouseDisplayName).toContain('CENIT');
  const transactionId = list.items[0].id;

  const before = await getJson<TransportDetail>(page, token!, `/api/transactions/outgoing-monitoring/${transactionId}`);
  const artifact = before.files.find(item => item.operationDisplayName === 'Devolución / Return Out' && item.transmissionReference);
  expect(artifact).toBeTruthy();
  expect(artifact!.transportAttempts).toHaveLength(1);
  expect(artifact!.transportAttempts[0].statusCode).toBe('Succeeded');
  expect(artifact!.transportResults).toHaveLength(1);
  expect(artifact!.transportResults[0].outcomeCode).toBe('Accepted');
  expect(artifact!.transmissionReference).toBeTruthy();

  const replay = await page.request.post(`${api}/ach-returns/transport/results`, {
    headers: auth(token!),
    data: {
      externalEventId: 'CENIT-UAT-F4-ACK',
      fileName: artifact!.fileName,
      transmissionReference: artifact!.transmissionReference,
      outcome: 2,
      resultCode: 'ACCEPTED',
      occurredAtUtc: '2026-08-20T10:32:00Z',
      resultSummary: 'Replay controlado del acuse CENIT.'
    }
  });
  expect(replay.ok()).toBeTruthy();
  expect((await replay.json() as { wasDuplicate: boolean }).wasDuplicate).toBe(true);

  await page.goto('/transactions/outgoing-monitoring');
  await page.getByLabel('Identificador').fill(externalId);
  await page.getByRole('button', { name: 'Buscar' }).click();
  const table = page.getByRole('table', { name: 'Transacciones de salida' });
  await expect(table.getByText(externalId, { exact: false })).toBeVisible();
  await table.getByRole('button', { name: /Ver detalle/ }).click();
  const detail = page.getByTestId('outgoing-monitoring-detail');
  await expect(detail).toContainText('CENIT');
  await expect(detail).toContainText('Devolución / Return Out');
  await expect(detail).toContainText('CFA-MFT-HANDOFF:CENIT-UAT-F4');
  await expect(detail).toContainText('Número de intentos');
  await expect(detail).toContainText('HANDOFF_COMMITTED');
  await expect(detail).toContainText('Resultado CENIT aceptado');
  await expect(detail.getByTestId('outgoing-timeline')).toContainText('Transmisión registrada');

  await page.reload();
  await expect(page.getByTestId('outgoing-monitoring-detail')).toContainText('Resultado CENIT aceptado');
  const after = await getJson<TransportDetail>(page, token!, `/api/transactions/outgoing-monitoring/${transactionId}`);
  const persisted = after.files.find(item => item.operationDisplayName === 'Devolución / Return Out' && item.transmissionReference);
  expect(persisted!.transportAttempts).toHaveLength(1);
  expect(persisted!.transportResults).toHaveLength(1);
});

async function getJson<T>(page: Page, token: string, path: string): Promise<T> {
  const response = await page.request.get(`${api}${path}`, { headers: auth(token) });
  expect(response.ok(), `GET ${path} debe responder correctamente (${response.status()}).`).toBeTruthy();
  return await response.json() as T;
}

function auth(token: string): Record<string, string> {
  return { Authorization: `Bearer ${token}` };
}
