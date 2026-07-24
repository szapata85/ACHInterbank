import { expect, Page, test } from '@playwright/test';
import { execFile } from 'node:child_process';
import { mkdirSync, readFileSync } from 'node:fs';
import path from 'node:path';
import { promisify } from 'node:util';

const execFileAsync = promisify(execFile);
const spaBaseUrl = (process.env['E2E_BASE_URL'] ?? 'http://localhost:743').replace(/\/+$/, '');
const adminUser = process.env['E2E_ADMIN_USER'] ?? process.env['ACH_USER'] ?? 'admin';
const adminPassword = process.env['E2E_ADMIN_PASSWORD'] ?? process.env['ACH_PASS'] ?? 'Admin123!';
const repositoryRoot = path.resolve(__dirname, '../../..');
const publicCertificatePath = path.join(repositoryRoot, 'docs/uat/certificados_pruebas/ACHcolombia.cer');
const privateCertificatePath = path.join(repositoryRoot, 'docs/uat/certificados_pruebas/CFA.pfx');
const privateCertificatePasswordPath = path.join(repositoryRoot, 'docs/uat/certificados_pruebas/pass.txt');
const evidenceDirectory = path.join(repositoryRoot, 'docs/uat/evidencias/nacha-security-certificates-sqlserver');
const publicUploadPath = '/api/nacha-security/certificates/management/public';
const privateUploadPath = '/api/nacha-security/certificates/management/private';

test.describe.configure({ mode: 'serial' });
test.use({ trace: 'off', video: 'off', screenshot: 'off' });

test.describe('Administracion real de certificados con SQL Server', () => {
  test('smoke recupera certificados despues de recrear la API', async ({ page }) => {
    const browserErrors: string[] = [];
    page.on('console', (message) => {
      if (message.type() === 'error') browserErrors.push(message.text());
    });
    page.on('pageerror', (error) => browserErrors.push(error.message));

    await loginThroughUi(page, adminUser, adminPassword);
    await openCertificateScreen(page);
    await assertBothCertificates(page);
    await expect(page.locator('body')).not.toContainText('[object Object]');
    expect(browserErrors).toEqual([]);
  });

  test('exige el permiso de administracion antes de abrir la pantalla', async ({ page }) => {
    test.setTimeout(120_000);
    const suffix = `${Date.now()}`;
    const userName = `cert-no-permission-${suffix}`;
    const userPassword = 'UatCertificate1!';

    await loginThroughUi(page, adminUser, adminPassword);
    await page.goto(`${spaBaseUrl}/users/new`);
    await page.locator('input[formControlName="userName"]').fill(userName);
    await page.locator('input[formControlName="fullName"]').fill('UAT Certificados Sin Permisos');
    await page.locator('input[formControlName="email"]').fill(`${userName}@example.com`);
    await page.locator('input[formControlName="email"]').blur();
    await page.locator('input[formControlName="password"]').fill(userPassword);
    await expect(page.locator('form button[type="submit"]')).toBeEnabled();
    await Promise.all([
      page.waitForResponse((response) => apiPath(response.url()) === '/api/users' && response.request().method() === 'POST' && response.status() === 201),
      page.locator('form button[type="submit"]').click()
    ]);

    await logoutThroughUi(page);
    await loginThroughUi(page, userName, userPassword);
    await page.goto(`${spaBaseUrl}/nacha-security/certificates`);
    await expect(page).not.toHaveURL(/\/nacha-security\/certificates$/);
    await expect(page.getByRole('heading', { name: 'Certificados digitales' })).toHaveCount(0);

    await logoutThroughUi(page);
    await loginThroughUi(page, adminUser, adminPassword);
    await deactivateTestUser(page, userName);
  });

  test('muestra errores comprensibles para archivos invalidos y password incorrecta', async ({ page }) => {
    test.setTimeout(120_000);
    await loginThroughUi(page, adminUser, adminPassword);
    await openCertificateScreen(page);

    const publicCard = certificateCard(page, 'EncryptionPublic');
    const privateCard = certificateCard(page, 'SigningKeyPair');
    let uploadRequests = 0;
    page.on('request', (request) => {
      const pathname = apiPath(request.url());
      if (pathname === publicUploadPath || pathname === privateUploadPath) uploadRequests += 1;
    });

    await publicCard.locator('input[type="file"]').setInputFiles({ name: 'invalid.txt', mimeType: 'text/plain', buffer: Buffer.from('invalid') });
    await expect(publicCard).toContainText('Formato no permitido');

    await publicCard.locator('input[type="file"]').setInputFiles({ name: 'empty.cer', mimeType: 'application/pkix-cert', buffer: Buffer.alloc(0) });
    await expect(publicCard).toContainText(/archivo est.*vac/i);

    await publicCard.locator('input[type="file"]').setInputFiles(privateCertificatePath);
    await expect(publicCard).toContainText('Formato no permitido');

    await privateCard.locator('input[type="file"]').setInputFiles(publicCertificatePath);
    await expect(privateCard).toContainText('Formato no permitido');
    expect(uploadRequests).toBe(0);

    await privateCard.locator('input[type="file"]').setInputFiles(privateCertificatePath);
    await privateCard.locator('input[type="password"]').fill('password-incorrecta-controlada');
    const responsePromise = page.waitForResponse((response) => apiPath(response.url()) === privateUploadPath && response.request().method() === 'POST');
    await privateCard.getByRole('button', { name: 'Cargar certificado' }).click();
    const response = await responsePromise;
    expect(response.status()).toBe(400);
    await expect(page.locator('.alerts')).toContainText(/contrase|PKCS|certificado privado/i);
    await expect(page.locator('body')).not.toContainText('[object Object]');
    await expect(privateCard.locator('input[type="password"]')).toHaveValue('');
  });

  test('carga CER y PFX reales, conserva la sesion y persiste tras recargar y autenticar de nuevo', async ({ page }) => {
    test.setTimeout(180_000);
    const browserErrors: string[] = [];
    page.on('console', (message) => {
      if (message.type() === 'error') browserErrors.push(message.text());
    });
    page.on('pageerror', (error) => browserErrors.push(error.message));
    await loginThroughUi(page, adminUser, adminPassword);
    await openCertificateScreen(page);

    const publicCard = certificateCard(page, 'EncryptionPublic');
    if (!(await publicCard.textContent())?.includes('ACHcolombia.cer')) {
      await publicCard.locator('input[type="file"]').setInputFiles(publicCertificatePath);
      const responsePromise = page.waitForResponse((response) => apiPath(response.url()) === publicUploadPath && response.request().method() === 'POST');
      await publicCard.getByRole('button', { name: 'Cargar certificado' }).click();
      expect((await responsePromise).status()).toBe(200);
      await expect(page.locator('.alerts')).toContainText(/carg.*correctamente/i);
    }
    await assertPublicCertificate(publicCard);

    await page.reload();
    await assertPublicCertificate(certificateCard(page, 'EncryptionPublic'));

    const privateCard = certificateCard(page, 'SigningKeyPair');
    if (!(await privateCard.textContent())?.includes('CFA.pfx')) {
      const pfxPassword = readFileSync(privateCertificatePasswordPath, 'utf8').trim();
      await privateCard.locator('input[type="file"]').setInputFiles(privateCertificatePath);
      await privateCard.locator('input[type="password"]').fill(pfxPassword);
      const responsePromise = page.waitForResponse((response) => apiPath(response.url()) === privateUploadPath && response.request().method() === 'POST');
      await privateCard.getByRole('button', { name: 'Cargar certificado' }).click();
      expect((await responsePromise).status()).toBe(200);
      await expect(privateCard.locator('input[type="password"]')).toHaveValue('');
      await expect(page.locator('.alerts')).toContainText(/carg.*correctamente/i);
    }
    await assertPrivateCertificate(privateCard);
    await expect.poll(() => page.evaluate(() => sessionStorage.getItem('ach.interbank.access_token'))).not.toBeNull();

    await page.reload();
    await assertBothCertificates(page);
    await logoutThroughUi(page);
    await loginThroughUi(page, adminUser, adminPassword);
    await openCertificateScreen(page);
    await assertBothCertificates(page);
    expect(browserErrors).toEqual([]);

    mkdirSync(evidenceDirectory, { recursive: true });
    await page.screenshot({ path: path.join(evidenceDirectory, 'despues-certificados.png'), fullPage: true });
  });

  test('rechaza un certificado duplicado con HTTP 409 y mensaje visible', async ({ page }) => {
    await loginThroughUi(page, adminUser, adminPassword);
    await openCertificateScreen(page);
    const publicCard = certificateCard(page, 'EncryptionPublic');
    await publicCard.locator('input[type="file"]').setInputFiles(publicCertificatePath);
    const responsePromise = page.waitForResponse((response) => apiPath(response.url()) === publicUploadPath && response.request().method() === 'POST');
    await publicCard.getByRole('button', { name: 'Cargar certificado' }).click();
    expect((await responsePromise).status()).toBe(409);
    await expect(page.locator('.alerts')).toContainText(/duplicado|registrado/i);
    await expect(page.locator('body')).not.toContainText('[object Object]');

    const privateCard = certificateCard(page, 'SigningKeyPair');
    const pfxPassword = readFileSync(privateCertificatePasswordPath, 'utf8').trim();
    await privateCard.locator('input[type="file"]').setInputFiles(privateCertificatePath);
    await privateCard.locator('input[type="password"]').fill(pfxPassword);
    const privateResponsePromise = page.waitForResponse((response) => apiPath(response.url()) === privateUploadPath && response.request().method() === 'POST');
    await privateCard.getByRole('button', { name: 'Cargar certificado' }).click();
    expect((await privateResponsePromise).status()).toBe(409);
    await expect(page.locator('.alerts')).toContainText(/duplicado|registrado/i);
    await expect(privateCard.locator('input[type="password"]')).toHaveValue('');
  });

  test('persiste al reiniciar SPA y API y al reiniciar el stack conservando SQL Server', async ({ page }) => {
    test.setTimeout(300_000);
    await restartContainers(['achinterbank-api', 'achinterbank-spa']);
    await waitForContainerHealth('achinterbank-api');
    await waitForContainerHealth('achinterbank-spa');
    await loginThroughUi(page, adminUser, adminPassword);
    await openCertificateScreen(page);
    await assertBothCertificates(page);

    await restartContainers(['achinterbank-sqlserver']);
    await waitForContainerHealth('achinterbank-sqlserver');
    await restartContainers(['achinterbank-api', 'achinterbank-spa']);
    await waitForContainerHealth('achinterbank-api');
    await waitForContainerHealth('achinterbank-spa');
    await page.context().clearCookies();
    await page.goto(`${spaBaseUrl}/login`);
    await loginThroughUi(page, adminUser, adminPassword);
    await openCertificateScreen(page);
    await assertBothCertificates(page);
  });

  test('informa indisponibilidad real del API sin mostrar objetos serializados', async ({ page }) => {
    test.setTimeout(180_000);
    await loginThroughUi(page, adminUser, adminPassword);
    await openCertificateScreen(page);
    try {
      await execFileAsync('docker', ['stop', 'achinterbank-api']);
      const publicCard = certificateCard(page, 'EncryptionPublic');
      await publicCard.locator('input[type="file"]').setInputFiles(publicCertificatePath);
      const responsePromise = page.waitForResponse((response) => apiPath(response.url()) === publicUploadPath);
      await publicCard.getByRole('button', { name: 'Cargar certificado' }).click();
      expect((await responsePromise).status()).toBeGreaterThanOrEqual(500);
      await expect(page.locator('.alerts')).toBeVisible();
      await expect(page.locator('body')).not.toContainText('[object Object]');
    } finally {
      await execFileAsync('docker', ['start', 'achinterbank-api']);
      await waitForContainerHealth('achinterbank-api');
    }
  });
});

async function loginThroughUi(page: Page, username: string, password: string): Promise<void> {
  await page.goto(`${spaBaseUrl}/login`);
  await page.locator('input[formControlName="username"]').fill(username);
  await page.locator('input[formControlName="password"]').fill(password);
  await Promise.all([
    page.waitForResponse((response) => apiPath(response.url()) === '/auth/login' && response.request().method() === 'POST' && response.status() === 200),
    page.getByRole('button', { name: 'Ingresar' }).click()
  ]);
  await expect(page).not.toHaveURL(/\/login$/);
}

async function logoutThroughUi(page: Page): Promise<void> {
  await page.getByRole('button', { name: 'Salir' }).click();
  await expect(page).toHaveURL(/\/login$/);
}

async function deactivateTestUser(page: Page, userName: string): Promise<void> {
  const token = await page.evaluate(() => sessionStorage.getItem('ach.interbank.access_token'));
  expect(token).toBeTruthy();
  const usersResponse = await page.request.get(`${spaBaseUrl}/api/users?search=${encodeURIComponent(userName)}&page=1&pageSize=10`, {
    headers: { Authorization: `Bearer ${token}` }
  });
  expect(usersResponse.status()).toBe(200);
  const users = await usersResponse.json() as { items?: Array<{ id: string; userName: string }> };
  const user = users.items?.find((item) => item.userName === userName);
  expect(user).toBeTruthy();
  const deactivateResponse = await page.request.delete(`${spaBaseUrl}/api/users/${user!.id}`, {
    headers: { Authorization: `Bearer ${token}` }
  });
  expect(deactivateResponse.status()).toBe(204);
}

async function openCertificateScreen(page: Page): Promise<void> {
  await page.goto(`${spaBaseUrl}/nacha-security/certificates`);
  await expect(page).toHaveURL(/\/nacha-security\/certificates$/);
  await expect(page.getByRole('heading', { name: 'Certificados digitales' })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Certificados digitales', exact: true })).toHaveCount(1);
  await expect(page.locator('section.cards')).toBeVisible();
  await expect(page.locator('body')).not.toContainText('[object Object]');
}

function certificateCard(page: Page, type: 'EncryptionPublic' | 'SigningKeyPair') {
  return page.locator(`[data-certificate-type="${type}"]`);
}

async function assertPublicCertificate(card: ReturnType<typeof certificateCard>): Promise<void> {
  await expect(card).toContainText('ACHcolombia.cer');
  await expect(card).toContainText('ACH COLOMBIA');
  await expect(card).toContainText('Huella SHA-256');
  await expect(card).toContainText('Vigencia');
  await expect(card).toContainText(/Llave privada\s*No/i);
}

async function assertPrivateCertificate(card: ReturnType<typeof certificateCard>): Promise<void> {
  await expect(card).toContainText('CFA.pfx');
  await expect(card).toContainText('COOPERATIVA FINANCIERA DE ANTIOQUIA');
  await expect(card).toContainText('Huella SHA-256');
  await expect(card).toContainText(/Llave privada\s*S/i);
}

async function assertBothCertificates(page: Page): Promise<void> {
  await assertPublicCertificate(certificateCard(page, 'EncryptionPublic'));
  await assertPrivateCertificate(certificateCard(page, 'SigningKeyPair'));
  await expect(page.locator('body')).not.toContainText('[object Object]');
}

function apiPath(url: string): string {
  return new URL(url).pathname;
}

async function restartContainers(names: string[]): Promise<void> {
  await execFileAsync('docker', ['restart', ...names]);
}

async function waitForContainerHealth(name: string): Promise<void> {
  await expect.poll(async () => {
    const { stdout } = await execFileAsync('docker', ['inspect', '--format', '{{if .State.Health}}{{.State.Health.Status}}{{else}}{{.State.Status}}{{end}}', name]);
    return stdout.trim();
  }, { timeout: 120_000, intervals: [1_000, 2_000, 5_000] }).toBe('healthy');
}
