import { readFile } from 'node:fs/promises';
import { expect, Locator, Page, Response, test } from '@playwright/test';
import { loginThroughUi } from './support/live-ui-auth';

const listReports = [
  { name: 'Enviados', route: '/reports/sent', endpoint: '/api/reports/transactions/sent/pdf' },
  { name: 'Recibidos', route: '/reports/received', endpoint: '/api/reports/transactions/received/pdf' },
  { name: 'Devoluciones', route: '/reports/returns', endpoint: '/api/reports/returns/pdf' },
  { name: 'Rechazos', route: '/reports/rejections', endpoint: '/api/reports/rejections/pdf' },
  { name: 'Archivos', route: '/reports/files', endpoint: '/api/reports/nacha-files/pdf' },
  { name: 'Ciclos', route: '/reports/cycles', endpoint: '/api/reports/cycles/pdf' },
  { name: 'Auditoría', route: '/reports/audit', endpoint: '/api/reports/audit/pdf' },
  { name: 'Histórico', route: '/reports/history', endpoint: '/api/reports/history/pdf' }
] as const;

test.describe('Central de Reportes ACH contra Docker', () => {
  test.setTimeout(180_000);
  test.beforeEach(async ({ page }) => loginThroughUi(page));

  test('presenta la central y descarga los once PDF reales', async ({ page }, testInfo) => {
    await page.goto('/reports', { waitUntil: 'networkidle' });
    await expect(page.getByRole('heading', { name: 'Central de Reportes ACH', level: 2 })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Operación transaccional' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Procesamiento' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Seguimiento y control' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Revisión contable' })).toBeVisible();
    await expect(page.locator('body')).not.toContainText(/ReturnedByOperator|ReturnedByEpr|AppliedTacitly|Manual audit-only|Return of Return/);
    await page.screenshot({ path: testInfo.outputPath('reports-desktop.png'), fullPage: true });

    for (const report of listReports) {
      await page.goto(report.route, { waitUntil: 'networkidle' });
      await expect(page.getByRole('heading', { name: report.name, level: 1 })).toBeVisible();
      await downloadAndValidate(page, report.endpoint, page.getByRole('button', { name: /Descargar PDF/i }));
    }

    await page.goto('/reports/traceability', { waitUntil: 'networkidle' });
    await downloadAndValidate(page, '/api/reports/traceability/pdf', page.getByRole('button', { name: /Descargar PDF/i }));

    await page.goto('/reports/reconciliation', { waitUntil: 'networkidle' });
    await page.getByRole('button', { name: /Consultar/i }).click();
    await expect(page.getByText(/Estamos consultando/)).toBeHidden({ timeout: 30_000 });
    const reconciliationDownload = page.getByRole('button', { name: /Descargar PDF/i });
    if (await reconciliationDownload.isEnabled()) {
      await downloadAndValidate(page, '/api/reports/reconciliation/pdf', reconciliationDownload);
    } else {
      await expect(page.getByTestId('reconciliation-empty-state')).toContainText('No encontramos información');
      await validateAuthenticatedPdfRequest(page, '/api/reports/reconciliation/pdf');
    }

    await page.goto('/reports', { waitUntil: 'networkidle' });
    await downloadAndValidate(page, '/api/reports/accounting-review/export', page.getByRole('button', { name: /Descargar reporte/i }));
  });

  test('mantiene navegación, filtros y contenido dentro del viewport móvil', async ({ page }, testInfo) => {
    await page.setViewportSize({ width: 390, height: 844 });
    await page.goto('/reports', { waitUntil: 'networkidle' });
    await expect(page.getByRole('heading', { name: 'Central de Reportes ACH', level: 2 })).toBeVisible();
    await expect(page.getByRole('link', { name: /Abrir reporte de Transacciones enviadas/i })).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true);

    await page.goto('/reports/sent', { waitUntil: 'networkidle' });
    await expect(page.getByRole('button', { name: /Consultar/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /Limpiar filtros/i })).toBeVisible();
    await expect(page.getByRole('button', { name: /Descargar PDF/i })).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= document.documentElement.clientWidth)).toBe(true);
    await page.screenshot({ path: testInfo.outputPath('reports-mobile.png'), fullPage: true });
  });
});

async function downloadAndValidate(page: Page, endpoint: string, trigger: Locator): Promise<void> {
  const responsePromise = page.waitForResponse(response => response.url().includes(endpoint) && response.status() !== 429, { timeout: 30_000 });
  const downloadPromise = page.waitForEvent('download', { timeout: 30_000 });
  await trigger.click();
  const [response, download] = await Promise.all([responsePromise, downloadPromise]);
  await validateHttpResponse(response);

  const filePath = await download.path();
  expect(filePath).not.toBeNull();
  const bytes = await readFile(filePath!);
  expect(bytes.length).toBeGreaterThan(512);
  expect(bytes.subarray(0, 5).toString('ascii')).toBe('%PDF-');
  expect(download.suggestedFilename().toLowerCase()).toMatch(/\.pdf$/);
  await page.waitForTimeout(150);
}

async function validateHttpResponse(response: Response): Promise<void> {
  expect(response.status()).toBe(200);
  expect(response.headers()['content-type']?.toLowerCase()).toContain('application/pdf');
  expect(response.headers()['content-disposition']?.toLowerCase()).toContain('.pdf');
}

async function validateAuthenticatedPdfRequest(page: Page, endpoint: string): Promise<void> {
  const token = await page.evaluate(() => sessionStorage.getItem('ach.interbank.access_token'));
  expect(token).toBeTruthy();
  const response = await page.request.get(endpoint, { headers: { Authorization: `Bearer ${token}` } });
  expect(response.status()).toBe(200);
  expect(response.headers()['content-type']?.toLowerCase()).toContain('application/pdf');
  expect(response.headers()['content-disposition']?.toLowerCase()).toContain('.pdf');
  const bytes = await response.body();
  expect(bytes.length).toBeGreaterThan(512);
  expect(bytes.subarray(0, 5).toString('ascii')).toBe('%PDF-');
}
