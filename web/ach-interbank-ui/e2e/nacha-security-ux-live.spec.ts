import { mkdirSync } from 'node:fs';
import { resolve } from 'node:path';
import { expect, Page, test } from '@playwright/test';

const baseUrl = (process.env['E2E_BASE_URL'] ?? process.env['ACH_UI_URL'] ?? 'http://localhost:743').replace(/\/+$/, '');
const userName = process.env['E2E_ADMIN_USER'] ?? process.env['ACH_USER'] ?? 'admin';
const password = process.env['E2E_ADMIN_PASSWORD'] ?? process.env['ACH_PASS'] ?? 'Admin123!';
const evidenceDir = resolve(process.cwd(), '../../docs/uat/evidencias/nacha-security-ux/final');

const forbiddenPrimaryLabels = [
  'Upload Certificate',
  'Outbound Encryption',
  'Inbound Decryption',
  'Outbound Signing',
  'Clearing House',
  'Participant',
  'Generate Envelope',
  'RuleId=',
  'ExpectedLength=',
  'Holder Type',
  'Thumbprint',
  'Valid From',
  'Valid To',
  'Private Key',
  'Public Key'
];

const routes = [
  {
    key: 'exportacion',
    path: '/ach-cycles/nacha/export',
    heading: 'Exportación NACHA-M',
    breadcrumb: 'Exportación NACHA-M'
  },
  {
    key: 'certificados',
    path: '/nacha-security/certificates',
    heading: 'Certificados de seguridad NACHA-M',
    breadcrumb: 'Certificados de seguridad NACHA-M'
  },
  {
    key: 'sobre-digital',
    path: '/nacha-security/sobre-digital',
    heading: 'Sobre digital NACHA-M',
    breadcrumb: 'Sobre digital NACHA-M'
  }
] as const;

test.describe('UX unificada de Exportación y Seguridad NACHA-M — runtime real', () => {
  test('navegación, traducciones, acciones seguras y ausencia de spanglish', async ({ page }) => {
    test.setTimeout(120_000);
    const diagnostics = captureDiagnostics(page);
    await loginThroughUi(page);

    for (const route of routes) {
      await page.goto(`${baseUrl}${route.path}`);
      await page.waitForLoadState('networkidle');
      await expect(page.getByRole('heading', { level: 1, name: route.heading })).toBeVisible();
      await expect(page.getByRole('navigation', { name: 'Ruta de navegación' })).toContainText(route.breadcrumb);
      await assertNoPrimarySpanglish(page);
    }

    await page.goto(`${baseUrl}/nacha-security/certificates`);
    await expect(page.getByText('Inventario de certificados')).toBeVisible();
    const firstCertificateRow = page.locator('table tbody tr').first();
    await expect(firstCertificateRow).toBeVisible();
    await firstCertificateRow.getByRole('button', { name: /Acciones para/ }).click();
    await page.getByRole('menuitem', { name: 'Ver detalles' }).click();
    const dialog = page.getByRole('dialog');
    await expect(dialog.getByRole('heading', { name: 'Detalles del certificado' })).toBeVisible();
    await expect(dialog).toContainText('Propósito');
    await expect(dialog).toContainText('Tipo de titular');
    await expect(dialog).toContainText(/Solo contiene clave pública|Disponible en almacenamiento seguro/);
    await expect(dialog).not.toContainText(/password|secretRef|contenido PFX/i);
    await dialog.getByRole('button', { name: 'Cerrar' }).click();

    await page.goto(`${baseUrl}/nacha-security/sobre-digital`);
    await expect(page.getByText('seleccionado automáticamente')).toBeVisible();
    await page.getByRole('tab', { name: 'Descifrar archivo' }).click();
    await expect(page.getByText('Identidad privada seleccionada automáticamente')).toBeVisible();
    await assertNoPrimarySpanglish(page);

    expect(diagnostics.pageErrors).toEqual([]);
    expect(diagnostics.consoleErrors).toEqual([]);
    expect(diagnostics.serverErrors).toEqual([]);
    expect(diagnostics.htmlApiResponses).toEqual([]);
  });

  for (const viewport of [
    { name: 'desktop-1440x900', width: 1440, height: 900 },
    { name: 'tablet-768x1024', width: 768, height: 1024 },
    { name: 'movil-390x844', width: 390, height: 844 }
  ]) {
    test(`responsive ${viewport.name} sin desbordamiento del cuerpo`, async ({ page }) => {
      test.setTimeout(120_000);
      await page.setViewportSize({ width: viewport.width, height: viewport.height });
      const diagnostics = captureDiagnostics(page);
      await loginThroughUi(page);

      for (const route of routes) {
        await page.goto(`${baseUrl}${route.path}`);
        await page.waitForLoadState('networkidle');
        await expect(page.getByRole('heading', { level: 1, name: route.heading })).toBeVisible();
        const overflow = await page.locator('body').evaluate(body => ({
          clientWidth: body.clientWidth,
          scrollWidth: body.scrollWidth,
          offenders: Array.from(body.querySelectorAll<HTMLElement>('*'))
            .filter(element => element.getBoundingClientRect().right > body.clientWidth + 1)
            .slice(0, 5)
            .map(element => `${element.tagName.toLowerCase()}.${element.className}`)
        }));
        expect(
          overflow.scrollWidth,
          `${route.path} no debe desbordar horizontalmente. Elementos: ${overflow.offenders.join(', ')}`
        ).toBeLessThanOrEqual(overflow.clientWidth + 1);
        await assertNoPrimarySpanglish(page);

        const path = resolve(evidenceDir, `${route.key}-${viewport.name}.png`);
        mkdirSync(evidenceDir, { recursive: true });
        await page.screenshot({ path, fullPage: true });
      }

      expect(diagnostics.pageErrors).toEqual([]);
      expect(diagnostics.consoleErrors).toEqual([]);
      expect(diagnostics.serverErrors).toEqual([]);
      expect(diagnostics.htmlApiResponses).toEqual([]);
    });
  }
});

async function assertNoPrimarySpanglish(page: Page): Promise<void> {
  const visibleText = await page.locator('body').innerText();
  for (const term of forbiddenPrimaryLabels) {
    expect(visibleText, `No debe mostrarse “${term}” como etiqueta principal`).not.toContain(term);
  }
}

function captureDiagnostics(page: Page) {
  const pageErrors: string[] = [];
  const consoleErrors: string[] = [];
  const serverErrors: Array<{ path: string; status: number }> = [];
  const htmlApiResponses: string[] = [];

  page.on('pageerror', error => pageErrors.push(error.message));
  page.on('console', message => {
    if (message.type() === 'error' && !message.text().startsWith('Failed to load resource:')) {
      consoleErrors.push(message.text());
    }
  });
  page.on('response', response => {
    const url = new URL(response.url());
    if (response.status() >= 500) {
      serverErrors.push({ path: url.pathname, status: response.status() });
    }
    if ((url.pathname.startsWith('/api/') || url.pathname.startsWith('/NachaExport/'))
        && (response.headers()['content-type'] ?? '').toLowerCase().includes('text/html')) {
      htmlApiResponses.push(url.pathname);
    }
  });
  return { pageErrors, consoleErrors, serverErrors, htmlApiResponses };
}

async function loginThroughUi(page: Page): Promise<void> {
  await page.goto(`${baseUrl}/login`);
  await page.locator('input[formControlName="username"]').fill(userName);
  await page.locator('input[formControlName="password"]').fill(password);
  await Promise.all([
    page.waitForResponse(response => new URL(response.url()).pathname === '/auth/login' && response.status() === 200),
    page.getByRole('button', { name: 'Ingresar' }).click()
  ]);
  await expect(page).not.toHaveURL(/\/login$/);
}
