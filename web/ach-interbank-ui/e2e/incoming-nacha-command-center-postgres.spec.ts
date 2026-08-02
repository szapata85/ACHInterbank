import { expect, Page, test, TestInfo } from '@playwright/test';
import { Client } from 'pg';

const spa = (process.env['ACH_UI_URL'] ?? process.env['E2E_BASE_URL'] ?? 'http://localhost:743').replace(/\/+$/, '');
const configuredUsername = process.env['E2E_ADMIN_USER'] ?? process.env['ACH_USER'];
const configuredPassword = process.env['E2E_ADMIN_PASSWORD'] ?? process.env['ACH_PASS'];
const ingestionId = '8c2f7a0a-4f43-4aa8-9f1b-030000000001';
const processingResultId = '8c2f7a0a-4f43-4aa8-9f1b-030000000002';
const nachaId = 'E2E-PROMPT3-CLOSE';
const batchId = 930001;
const fileName = '0001283.001.20260801.99';

let database: Client;
let username: string;
let password: string;

test.describe.configure({ mode: 'serial' });

test.describe('centro de control NACHA-M con PostgreSQL real', () => {
  test.beforeAll(async () => {
    if (!configuredUsername || !configuredPassword) {
      throw new Error('ACH_USER y ACH_PASS son obligatorios para el escenario PostgreSQL real.');
    }
    username = configuredUsername;
    password = configuredPassword;

    const databasePassword = process.env['E2E_DB_PASSWORD'] ?? process.env['POSTGRES_PASSWORD'];
    if (!databasePassword) {
      throw new Error('E2E_DB_PASSWORD o POSTGRES_PASSWORD es obligatorio para preparar el fixture aislado.');
    }

    database = new Client({
      host: process.env['E2E_DB_HOST'] ?? '127.0.0.1',
      port: Number(process.env['E2E_DB_PORT'] ?? '5432'),
      database: process.env['E2E_DB_NAME'] ?? 'ACHInterbank',
      user: process.env['E2E_DB_USER'] ?? 'example_user',
      password: databasePassword
    });
    await database.connect();
    await cleanupFixture();
    await seedFixture();
  });

  test.afterAll(async () => {
    if (database) {
      await cleanupFixture();
      await database.end();
    }
  });

  test('inicia sesión y recorre listado, detalle, validaciones y lotes sin interceptar el centro de control', async ({ page }, testInfo) => {
    test.setTimeout(180_000);
    const evidence = observeRuntime(page);

    await loginThroughUi(page);

    const summaryResponse = page.waitForResponse((response) =>
      response.request().method() === 'GET'
      && new URL(response.url()).pathname === '/incoming-nacha-command-center/observability/summary');
    const listResponse = page.waitForResponse((response) =>
      response.request().method() === 'GET'
      && new URL(response.url()).pathname === '/incoming-nacha-command-center/ingestions');
    await page.goto(`${spa}/incoming-nacha-command-center`);

    expect((await summaryResponse).status(), 'El resumen operativo real debe responder correctamente.').toBe(200);
    expect((await listResponse).status(), 'El listado real debe responder correctamente.').toBe(200);
    await expect(page.getByRole('heading', { name: 'Seguimiento de archivos NACHA-M', level: 1 })).toBeVisible();
    await expect(page.getByText(fileName, { exact: true }).first()).toBeVisible();
    await page.screenshot({ path: testInfo.outputPath('listado-postgresql-real.png'), fullPage: true });

    const detailResponse = page.waitForResponse((response) =>
      response.request().method() === 'GET'
      && new URL(response.url()).pathname === `/incoming-nacha-command-center/ingestions/${ingestionId}`);
    const validationsResponse = page.waitForResponse((response) =>
      response.request().method() === 'GET'
      && new URL(response.url()).pathname === `/incoming-nacha-command-center/ingestions/${ingestionId}/validations`);
    const batchesResponse = page.waitForResponse((response) =>
      response.request().method() === 'GET'
      && new URL(response.url()).pathname === `/incoming-nacha-command-center/ingestions/${ingestionId}/batches`);
    await page.getByRole('button', { name: `Ver detalle del archivo ${fileName}` }).first().click();
    expect((await detailResponse).status(), 'El detalle real debe responder correctamente.').toBe(200);
    expect((await validationsResponse).status(), 'Las validaciones reales deben responder correctamente.').toBe(200);
    expect((await batchesResponse).status(), 'Los lotes reales deben responder correctamente.').toBe(200);
    await expect(page).toHaveURL(new RegExp(`/incoming-nacha-command-center/files/${ingestionId}`));
    await expect(page.getByText('Progreso del archivo')).toBeVisible();

    await page.getByRole('tab', { name: 'Validaciones' }).click();
    await expect(page.getByText('Archivo admitido')).toBeVisible();

    await page.getByRole('tab', { name: 'Lotes' }).click();
    await expect(page.getByText('PAGO E2E CONTROLADO')).toBeVisible();
    await page.screenshot({ path: testInfo.outputPath('detalle-postgresql-real.png'), fullPage: true });

    const directDetailResponse = page.waitForResponse((response) =>
      response.request().method() === 'GET'
      && new URL(response.url()).pathname === `/incoming-nacha-command-center/ingestions/${ingestionId}`);
    await page.goto(`${spa}/incoming-nacha-command-center/files/${ingestionId}?seccion=lotes`);
    expect((await directDetailResponse).status(), 'La navegación directa debe recuperar el detalle real.').toBe(200);
    await expect(page.getByRole('tab', { name: 'Lotes', selected: true })).toBeVisible();
    await expect(page.getByText('PAGO E2E CONTROLADO')).toBeVisible();

    expect(evidence.consoleErrors, JSON.stringify(evidence.consoleErrors, null, 2)).toEqual([]);
    expect(evidence.requestFailures, JSON.stringify(evidence.requestFailures, null, 2)).toEqual([]);
    expect(evidence.unexpectedHttpErrors, JSON.stringify(evidence.unexpectedHttpErrors, null, 2)).toEqual([]);
    await attachEvidence(testInfo, evidence);
  });
});

async function loginThroughUi(page: Page): Promise<void> {
  const loginResponse = page.waitForResponse((response) =>
    response.request().method() === 'POST' && new URL(response.url()).pathname === '/auth/login');

  await page.goto(`${spa}/login`);
  await page.locator('input[formControlName="username"]').fill(username);
  await page.locator('input[formControlName="password"]').fill(password);
  await page.getByRole('button', { name: 'Ingresar' }).click();

  expect((await loginResponse).status(), 'La autenticación real debe responder 200.').toBe(200);
  await expect(page).not.toHaveURL(/\/login(?:\?.*)?$/);
}

function observeRuntime(page: Page): RuntimeEvidence {
  const evidence: RuntimeEvidence = { consoleErrors: [], requestFailures: [], unexpectedHttpErrors: [], commandCenterResponses: [] };
  page.on('console', (message) => {
    if (message.type() === 'error') evidence.consoleErrors.push(message.text());
  });
  page.on('pageerror', (error) => evidence.consoleErrors.push(error.message));
  page.on('requestfailed', (request) => {
    evidence.requestFailures.push(`${request.method()} ${new URL(request.url()).pathname} ${request.failure()?.errorText ?? ''}`.trim());
  });
  page.on('response', (response) => {
    const url = new URL(response.url());
    if (url.pathname.startsWith('/incoming-nacha-command-center')) {
      evidence.commandCenterResponses.push({ method: response.request().method(), path: url.pathname, status: response.status() });
    }
    if (response.status() >= 400) {
      evidence.unexpectedHttpErrors.push(`${response.request().method()} ${url.pathname} ${response.status()}`);
    }
  });
  return evidence;
}

async function attachEvidence(testInfo: TestInfo, evidence: RuntimeEvidence): Promise<void> {
  await testInfo.attach('postgresql-runtime-evidence.json', {
    body: JSON.stringify({
      provider: 'PostgreSQL',
      ingestionId,
      fileName,
      interceptedCommandCenterEndpoints: [],
      ...evidence
    }, null, 2),
    contentType: 'application/json'
  });
}

async function seedFixture(): Promise<void> {
  const clearingHouse = await database.query<{ id: number }>(
    `SELECT "Id" AS id FROM "ClearingHouses" WHERE UPPER("Code") = 'CENIT' OR UPPER("Name") = 'CENIT' ORDER BY "Id" LIMIT 1`
  );
  expect(clearingHouse.rows, 'El seed runtime debe incluir la cámara CENIT.').toHaveLength(1);
  const clearingHouseId = clearingHouse.rows[0].id;

  await database.query('BEGIN');
  try {
    await database.query(
      `INSERT INTO "IncomingNachaFileIngestions" (
        "Id", "FileName", "FileHashSha256", "FileSize", "ContentType", "FileExtension",
        "UploadedAtUtc", "ReceivedAtUtc", "UploadedBy", "ReceivedBy", "IngestionStatus",
        "CycleResolutionStatus", "ParsingStatus", "DetectedClearingHouseId", "ResolvedClearingHouseId",
        "OperationalDate", "FileNameDate", "HeaderDate", "EffectiveDate", "DetectedCycleNumber",
        "ProfileCode", "ProfileVersion", "Stage", "ResolvedAchCycleId", "ResolutionMode",
        "ResolutionConfidence", "ResolutionEvidenceJson", "CorrelationId", "IsReprocess", "Notes",
        "WarningsJson", "CreatedAt", "UpdatedAt")
       VALUES ($1, $2, $3, 1060, 'text/plain', '', NOW(), NOW(), 'operador.e2e', 'operador.e2e',
        'Completado', 'ResueltoConfirmado', 'Exitoso', $4, $4, DATE '2026-08-01', DATE '2026-08-01',
        DATE '2026-08-01', DATE '2026-08-01', 1, 'E2E-CONTROLLED', '1.0', 'Persisted',
        'E2E-CICLO-01', 'FixturePostgreSql', 1.0, '{}', 'e2e-prompt3-runtime-correlation', FALSE,
        'Fixture sintético y eliminable para cierre del Prompt 3.', '[]', NOW(), NOW())`,
      [ingestionId, fileName, 'e2e-prompt3-close-hash', clearingHouseId]
    );
    await database.query(
      `INSERT INTO "IncomingNachaFileProcessingResults" (
        "Id", "IncomingNachaFileIngestionId", "AttemptNumber", "StartedAtUtc", "FinishedAtUtc",
        "TotalBatches", "TotalEntries", "TotalAddendas", "ValidCount", "InvalidCount", "WarningCount",
        "ErrorCount", "OutcomeStatus", "FailureStage", "ParserWarningsJson", "ParserErrorsJson",
        "IsReprocessable", "CreatedAt", "UpdatedAt")
       VALUES ($1, $2, 1, NOW(), NOW(), 1, 0, 0, 0, 0, 0, 0, 'Exitoso', '', '[]', '[]', FALSE, NOW(), NOW())`,
      [processingResultId, ingestionId]
    );
    await database.query(
      `INSERT INTO "NachaHeaders" (
        "NachaID", "ImmediateDestination", "ImmediateOrigin", "FileCreationDate", "FileCreationTime",
        "FileIdModifier", "RecordSize", "BlockingFactor", "FormatCode", "ImmediateDestinationName",
        "ImmediateOriginName", "ReferenceCode", "ClearingHouseId", "CycleNumber",
        "IncomingNachaFileIngestionId", "CreatedAt", "UpdatedAt")
       VALUES ($1, '000000000', '000000001', '260801', '1200', 'A', '106', '10', '1',
        'CENIT', 'ENTIDAD SINTETICA', 'E2E', $2, 1, $3, NOW(), NOW())`,
      [nachaId, clearingHouseId, ingestionId]
    );
    await database.query(
      `INSERT INTO "BatchHeaders" (
        "BatchID", "ServiceClassCode", "CompanyName", "CompanyId", "StandardEntryClassCode",
        "CompanyEntryDescription", "EffectiveEntryDate", "OriginParticipantEntityCode",
        "BatchNumber", "NachaID", "CreatedAt", "UpdatedAt")
       VALUES ($1, '220', 'EMPRESA E2E', 'E2E000001', 'PPD', 'PAGO E2E CONTROLADO',
        '260801', '00000001', 1, $2, NOW(), NOW())`,
      [batchId, nachaId]
    );
    await database.query('COMMIT');
  } catch (error) {
    await database.query('ROLLBACK');
    throw error;
  }
}

async function cleanupFixture(): Promise<void> {
  await database.query('BEGIN');
  try {
    await database.query('DELETE FROM "BatchHeaders" WHERE "NachaID" = $1', [nachaId]);
    await database.query('DELETE FROM "NachaHeaders" WHERE "NachaID" = $1', [nachaId]);
    await database.query('DELETE FROM "IncomingNachaFileProcessingResults" WHERE "IncomingNachaFileIngestionId" = $1', [ingestionId]);
    await database.query('DELETE FROM "IncomingNachaFileIngestions" WHERE "Id" = $1', [ingestionId]);
    await database.query('COMMIT');
  } catch (error) {
    await database.query('ROLLBACK');
    throw error;
  }
}

type RuntimeEvidence = {
  consoleErrors: string[];
  requestFailures: string[];
  unexpectedHttpErrors: string[];
  commandCenterResponses: Array<{ method: string; path: string; status: number }>;
};
