import { expect, Page, test } from '@playwright/test';
import { createHash } from 'node:crypto';
import { mkdir, readFile } from 'node:fs/promises';
import { resolve } from 'node:path';
import { loginThroughUi } from './support/live-ui-auth';

const spa = (process.env['E2E_BASE_URL'] ?? 'http://localhost:743').replace(/\/+$/, '');
const fixtureRoot = resolve(process.cwd(), '../../docs/uat/certificados_pruebas');
const achCertificatePath = resolve(fixtureRoot, 'ACHcolombia.cer');
const cfaCertificatePath = resolve(fixtureRoot, 'CFA.pfx');
const pfxPasswordPath = resolve(fixtureRoot, 'pass.txt');
const encryptedFilePath = resolve(
  fixtureRoot,
  'archivo_prueba/ACH Colombia/0001283.001.20260727.1.OUT.env'
);
const evidenceDirectory = resolve(
  process.cwd(),
  '../../docs/uat/evidencias/certificados-live'
);
const resumeFromOperationalCertificates =
  process.env['ACH_UAT_RESUME_DECRYPT'] === 'true';

interface CertificateApiItem {
  id: number;
  fileName: string;
  financialInstitutionId: number | null;
  financialInstitutionName: string | null;
  clearingHouseId: number | null;
  clearingHouseName: string | null;
  purpose: string | number;
  status: string | number;
  functionalStatus: string | number;
  normalizedThumbprint?: string;
  thumbprint: string;
  notBefore: string;
  notAfter: string;
  hasPrivateKey: boolean;
  canDelete: boolean;
}

test.use({ trace: 'off', video: 'off', screenshot: 'off' });

test.describe.serial('Administración de certificados y sobre digital LIVE', () => {
  test('carga los certificados reales, descifra y conserva la condición operativa', async ({ page }) => {
    test.setTimeout(180_000);
    await mkdir(evidenceDirectory, { recursive: true });

    const consoleErrors: string[] = [];
    const unexpectedResponses: string[] = [];
    page.on('pageerror', error => consoleErrors.push(error.message));
    page.on('console', message => {
      if (message.type() === 'error' && !/favicon/i.test(message.text())) {
        consoleErrors.push(message.text());
      }
    });
    page.on('response', response => {
      if (response.status() >= 400) {
        unexpectedResponses.push(`${response.status()} ${new URL(response.url()).pathname}`);
      }
    });

    await loginThroughUi(page);
    await page.goto(`${spa}/nacha-security/certificates`, { waitUntil: 'networkidle' });
    await expect(page.getByRole('heading', {
      name: 'Administración de certificados de seguridad'
    })).toBeVisible();

    if (!resumeFromOperationalCertificates) {
      await removeUnusedPreviousFixtureRecords(page);
      await uploadClearingHouseCertificate(page, achCertificatePath);
    }

    let certificates = await listCertificates(page);
    const achCertificate = findLatest(certificates, 'ClearingHouseValidation');
    expect(achCertificate.clearingHouseName).toBe('ACH Colombia');
    expect(achCertificate.financialInstitutionId ?? null).toBeNull();
    expect(achCertificate.hasPrivateKey).toBe(false);
    expect(achCertificate.notBefore).toBe('2026-06-22T20:27:17Z');
    expect(achCertificate.notAfter).toBe('2027-06-22T20:27:17Z');

    if (!resumeFromOperationalCertificates) {
      await uploadCfaCertificate(page, cfaCertificatePath, pfxPasswordPath);
    }
    certificates = await listCertificates(page);
    const cfaCertificate = findLatest(certificates, 'CfaSigningAndDecryption');
    expect(cfaCertificate.financialInstitutionId).not.toBeNull();
    expect(cfaCertificate.financialInstitutionName).toBeTruthy();
    expect(cfaCertificate.clearingHouseId ?? null).toBeNull();
    expect(cfaCertificate.hasPrivateKey).toBe(true);
    expect(cfaCertificate.notBefore).toBe('2024-09-20T17:29:25Z');
    expect(cfaCertificate.notAfter).toBe('2026-09-20T17:29:24Z');

    await page.reload({ waitUntil: 'networkidle' });
    await expect(page.getByText('Información técnica').first()).toBeVisible();
    await expect(page.locator('mat-expansion-panel.mat-expanded')).toHaveCount(0);
    await page.screenshot({
      path: resolve(evidenceDirectory, 'certificados-operativos.png'),
      fullPage: true
    });

    await uploadCfaExpectingDuplicate(
      page,
      cfaCertificatePath,
      pfxPasswordPath
    );

    const decryption = await decryptLive(page, encryptedFilePath);
    expect(decryption.output.length).toBeGreaterThan(0);
    expect(decryption.output.equals(decryption.input)).toBe(false);
    expect(decryption.outputType).not.toBe('vacío');
    expect(decryption.structureValid).toBe(true);
    console.info('Evidencia no sensible del descifrado LIVE', {
      inputBytes: decryption.input.length,
      outputBytes: decryption.output.length,
      outputType: decryption.outputType,
      structureValid: decryption.structureValid
    });

    await page.screenshot({
      path: resolve(evidenceDirectory, 'sobre-digital-live-correcto.png'),
      fullPage: true
    });

    const token = await sessionToken(page);
    const auth = { Authorization: `Bearer ${token}` };

    const revokeResponse = await page.request.post(
      `${spa}/api/nacha-security/certificates/management/versions/${achCertificate.id}/revoke`,
      { headers: auth, data: { reason: 'Prueba controlada de revocación y restauración' } }
    );
    expect(revokeResponse.status()).toBe(200);

    const revokedDecryption = await page.request.post(
      `${spa}/api/nacha-security/digital-envelope/decrypt`,
      {
        headers: auth,
        multipart: {
          clearingHouseId: String(achCertificate.clearingHouseId),
          operationMode: 'LIVE',
          file: {
            name: 'archivo-correspondiente.env',
            mimeType: 'application/octet-stream',
            buffer: await readFile(encryptedFilePath)
          }
        }
      }
    );
    expect(revokedDecryption.status()).toBe(404);
    const revokedProblem = await revokedDecryption.json();
    expect(revokedProblem.detail).toContain('No existe un certificado vigente');

    const deleteResponse = await page.request.delete(
      `${spa}/api/nacha-security/certificates/management/versions/${achCertificate.id}`,
      { headers: auth }
    );
    expect(deleteResponse.status()).toBe(200);

    await page.goto(`${spa}/nacha-security/certificates`, { waitUntil: 'networkidle' });
    await uploadClearingHouseCertificate(page, achCertificatePath);
    certificates = await listCertificates(page);
    const restoredAchCertificate = findLatest(certificates, 'ClearingHouseValidation');
    expect(restoredAchCertificate.id).not.toBe(achCertificate.id);

    await page.goto(`${spa}/nacha-security/sobre-digital`, { waitUntil: 'networkidle' });
    await page.getByRole('tab', { name: 'Descifrar archivo' }).click();
    const decryptForm = page.locator('form').filter({ hasText: 'Descifrar archivo' });
    await decryptForm.locator('input[type="file"]').setInputFiles({
      name: 'archivo.txt',
      mimeType: 'text/plain',
      buffer: Buffer.from('contenido no cifrado')
    });
    await expect(decryptForm.getByText('El archivo para descifrar debe terminar en .ENV.')).toBeVisible();

    const corrupted = Buffer.from(await readFile(encryptedFilePath));
    corrupted[Math.floor(corrupted.length / 2)] ^= 0x01;
    await selectDecryptContext(decryptForm);
    await decryptForm.locator('input[type="file"]').setInputFiles({
      name: 'archivo-corrupto.env',
      mimeType: 'application/octet-stream',
      buffer: corrupted
    });
    const corruptResponsePromise = page.waitForResponse(response =>
      response.request().method() === 'POST'
      && new URL(response.url()).pathname.endsWith('/api/nacha-security/digital-envelope/decrypt')
    );
    await decryptForm.getByRole('button', { name: 'Descifrar archivo' }).click();
    const corruptResponse = await corruptResponsePromise;
    expect(corruptResponse.status()).toBe(422);
    await expect(page.locator('app-operational-error-panel')).toBeVisible();
    await expect(page.locator('body')).not.toContainText(/CryptographicException|StackTrace|System\./);

    expect(consoleErrors.filter(message =>
      !/Failed to load resource: the server responded with a status of (404|409|422)/i.test(message)
    )).toEqual([]);
    expect(unexpectedResponses.filter(item =>
      item !== '409 /api/nacha-security/certificates/management/managed'
      && item !== '404 /api/nacha-security/digital-envelope/decrypt'
      && item !== '422 /api/nacha-security/digital-envelope/decrypt'
    )).toEqual([]);
    expect(unexpectedResponses).toContain('409 /api/nacha-security/certificates/management/managed');
    expect(unexpectedResponses).toContain('422 /api/nacha-security/digital-envelope/decrypt');
  });
});

async function uploadClearingHouseCertificate(page: Page, filePath: string): Promise<void> {
  await page.getByRole('tab', { name: 'Certificados de cámaras compensadoras' }).click();
  await page.getByRole('button', { name: 'Agregar certificado de cámara' }).click();
  const dialog = page.getByRole('dialog');
  await dialog.getByText('Validar información recibida de una cámara compensadora', { exact: true }).click();
  await dialog.getByRole('button', { name: 'Continuar' }).click();
  await dialog.getByRole('combobox', { name: 'Cámara compensadora' }).click();
  await page.getByRole('option', { name: 'ACH Colombia', exact: true }).click();
  await dialog.getByRole('button', { name: 'Continuar' }).click();
  await dialog.locator('input[type="file"]').setInputFiles(filePath);
  const previewResponse = page.waitForResponse(response =>
    response.request().method() === 'POST'
    && new URL(response.url()).pathname.endsWith('/managed/preview')
  );
  await dialog.getByRole('button', { name: 'Verificar información' }).click();
  expect((await previewResponse).status()).toBe(200);
  await expect(dialog.getByText('El certificado cumple las validaciones requeridas.')).toBeVisible();
  await expect(dialog.getByText('Válido desde')).toBeVisible();
  await expect(dialog.getByText('Válido hasta')).toBeVisible();
  const saveResponse = page.waitForResponse(response =>
    response.request().method() === 'POST'
    && new URL(response.url()).pathname.endsWith('/management/managed')
  );
  await dialog.getByRole('button', { name: 'Guardar certificado' }).click();
  const saved = await saveResponse;
  expect(saved.status(), await saved.text()).toBe(200);
  await expect(dialog).toBeHidden();
}

async function uploadCfaCertificate(
  page: Page,
  filePath: string,
  passwordPath: string
): Promise<void> {
  await page.getByRole('tab', { name: 'Certificado de CFA' }).click();
  await page.getByRole('button', { name: 'Cargar certificado de CFA' }).click();
  const dialog = page.getByRole('dialog');
  await dialog.getByText('Firmar y descifrar información de CFA', { exact: true }).click();
  await dialog.getByRole('button', { name: 'Continuar' }).click();
  await expect(dialog.getByText('La entidad financiera configurada actualmente como origen es CFA.')).toBeVisible();
  await dialog.getByRole('button', { name: 'Continuar' }).click();
  await dialog.locator('input[type="file"]').setInputFiles(filePath);

  let password = (await readFile(passwordPath, 'utf8')).trim();
  const passwordField = dialog.getByLabel('Contraseña del certificado');
  await passwordField.fill(password);
  const previewResponse = page.waitForResponse(response =>
    response.request().method() === 'POST'
    && new URL(response.url()).pathname.endsWith('/managed/preview')
  );
  await dialog.getByRole('button', { name: 'Verificar información' }).click();
  expect((await previewResponse).status()).toBe(200);
  await expect(dialog.getByText('Permite firmar y descifrar')).toBeVisible();
  const saveResponse = page.waitForResponse(response =>
    response.request().method() === 'POST'
    && new URL(response.url()).pathname.endsWith('/management/managed')
  );
  await dialog.getByRole('button', { name: 'Guardar certificado' }).click();
  const saved = await saveResponse;
  expect(saved.status(), await saved.text()).toBe(200);
  await expect(dialog).toBeHidden();
  password = '';
  expect(password).toBe('');
}

async function uploadCfaExpectingDuplicate(
  page: Page,
  filePath: string,
  passwordPath: string
): Promise<void> {
  await page.getByRole('tab', { name: 'Certificado de CFA' }).click();
  await page.getByRole('button', { name: 'Cargar certificado de CFA' }).click();
  const dialog = page.getByRole('dialog');
  await dialog.getByRole('button', { name: 'Continuar' }).click();
  await dialog.getByRole('button', { name: 'Continuar' }).click();
  await dialog.locator('input[type="file"]').setInputFiles(filePath);
  let password = (await readFile(passwordPath, 'utf8')).trim();
  await dialog.getByLabel('Contraseña del certificado').fill(password);
  await dialog.getByRole('button', { name: 'Verificar información' }).click();
  await expect(dialog.getByText('El certificado cumple las validaciones requeridas.')).toBeVisible();
  const responsePromise = page.waitForResponse(response =>
    response.request().method() === 'POST'
    && new URL(response.url()).pathname.endsWith('/management/managed')
  );
  await dialog.getByRole('button', { name: 'Guardar certificado' }).click();
  const response = await responsePromise;
  expect(response.status()).toBe(409);
  await expect(dialog.getByRole('alert')).toContainText('Este certificado ya se encuentra registrado');
  await dialog.getByRole('button', { name: 'Cancelar' }).click();
  password = '';
}

async function decryptLive(page: Page, filePath: string): Promise<{
  input: Buffer;
  output: Buffer;
  outputType: string;
  structureValid: boolean;
}> {
  await page.goto(`${spa}/nacha-security/sobre-digital`, { waitUntil: 'networkidle' });
  await page.getByRole('tab', { name: 'Descifrar archivo' }).click();
  const form = page.locator('form').filter({ hasText: 'Descifrar archivo' });
  await selectDecryptContext(form);
  await form.locator('input[type="file"]').setInputFiles(filePath);

  const responsePromise = page.waitForResponse(response =>
    response.request().method() === 'POST'
    && new URL(response.url()).pathname.endsWith('/api/nacha-security/digital-envelope/decrypt')
  );
  const downloadPromise = page.waitForEvent('download');
  await form.getByRole('button', { name: 'Descifrar archivo' }).click();
  const response = await responsePromise;
  expect(response.status()).toBe(200);
  const download = await downloadPromise;
  const stream = await download.createReadStream();
  const chunks: Buffer[] = [];
  for await (const chunk of stream) chunks.push(Buffer.from(chunk));
  const output = Buffer.concat(chunks);
  const input = await readFile(filePath);
  const validation = validateOutput(output);
  expect(createHash('sha256').update(output).digest('hex'))
    .not.toBe(createHash('sha256').update(input).digest('hex'));
  await expect(page.getByText('Archivo descifrado correctamente', { exact: true })).toBeVisible();
  return { input, output, ...validation };
}

async function selectDecryptContext(form: ReturnType<Page['locator']>): Promise<void> {
  const selects = form.locator('mat-select');
  await selects.nth(0).click();
  await form.page().getByRole('option', { name: 'ACH Colombia', exact: true }).click();
  await selects.nth(1).click();
  await form.page().getByRole('option', { name: 'Producción', exact: true }).click();
  await selects.nth(2).click();
  await form.page().getByRole('option', { name: 'Modo LIVE', exact: true }).click();
}

function validateOutput(output: Buffer): { outputType: string; structureValid: boolean } {
  if (output.length === 0) return { outputType: 'vacío', structureValid: false };
  const text = output.toString('utf8');
  const trimmed = text.trimStart();
  if (trimmed.startsWith('<?xml') || trimmed.startsWith('<')) {
    return { outputType: 'XML', structureValid: /<[^>]+>/.test(trimmed) };
  }
  if (trimmed.startsWith('{') || trimmed.startsWith('[')) {
    try {
      JSON.parse(trimmed);
      return { outputType: 'JSON', structureValid: true };
    } catch {
      return { outputType: 'JSON', structureValid: false };
    }
  }
  const records = text.split(/\r?\n/).filter(Boolean);
  if (records.length > 0 && records.every(record => record.length === 106)) {
    return {
      outputType: 'NACHA-M',
      structureValid: records[0].startsWith('1') && records.at(-1)?.startsWith('9') === true
    };
  }
  const printable = [...output].filter(value =>
    value === 9 || value === 10 || value === 13 || (value >= 32 && value <= 126)
  ).length / output.length;
  return {
    outputType: printable > 0.9 ? 'Texto estructurado' : 'Datos binarios',
    structureValid: output.length > 16
  };
}

async function listCertificates(page: Page): Promise<CertificateApiItem[]> {
  const token = await sessionToken(page);
  const response = await page.request.get(
    `${spa}/api/nacha-security/certificates/management`,
    { headers: { Authorization: `Bearer ${token}` } }
  );
  expect(response.status()).toBe(200);
  return response.json();
}

function findLatest(
  certificates: CertificateApiItem[],
  purpose: 'CfaSigningAndDecryption' | 'ClearingHouseValidation'
): CertificateApiItem {
  const numericPurpose = purpose === 'CfaSigningAndDecryption' ? 5 : 6;
  const matches = certificates
    .filter(item => item.purpose === purpose || item.purpose === numericPurpose)
    .sort((left, right) => right.id - left.id);
  expect(matches.length).toBeGreaterThan(0);
  return matches[0];
}

async function sessionToken(page: Page): Promise<string> {
  const token = await page.evaluate(() =>
    window.sessionStorage.getItem('ach.interbank.access_token') ?? ''
  );
  expect(token).toBeTruthy();
  return token;
}

async function removeUnusedPreviousFixtureRecords(page: Page): Promise<void> {
  const token = await sessionToken(page);
  const auth = { Authorization: `Bearer ${token}` };
  const records = (await listCertificates(page))
    .filter(item =>
      (item.fileName.toLowerCase() === 'achcolombia.cer'
        || item.fileName.toLowerCase() === 'cfa.pfx')
      && (item.purpose === 5
        || item.purpose === 6
        || item.purpose === 'CfaSigningAndDecryption'
        || item.purpose === 'ClearingHouseValidation'));

  for (const record of records) {
    if (!record.canDelete) {
      const revoke = await page.request.post(
        `${spa}/api/nacha-security/certificates/management/versions/${record.id}/revoke`,
        {
          headers: auth,
          data: { reason: 'Limpieza de una carga UAT incompleta y sin uso' }
        }
      );
      expect(revoke.status()).toBe(200);
    }
    const response = await page.request.delete(
      `${spa}/api/nacha-security/certificates/management/versions/${record.id}`,
      { headers: auth }
    );
    expect(response.status()).toBe(200);
  }
  await page.reload({ waitUntil: 'networkidle' });
}
