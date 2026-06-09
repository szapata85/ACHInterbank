import { expect, Page, test, TestInfo } from '@playwright/test';
import { spawnSync } from 'node:child_process';

type AuthLoginResponse = {
  data?: {
    token?: string;
  };
};

type FinancialInstitution = {
  id: number;
  name: string;
  isDefaultSource?: boolean;
  routingNumber?: string;
  transitCode?: string;
  status?: number;
};

type CompanyEntryDescription = {
  id: number;
  term?: string;
  isActive?: boolean;
};

type ClearingHouseRow = {
  id: number;
  name: string;
  code?: string;
};

type CreatedTransaction = {
  id: number;
  amount: number;
  transactionExternalId?: string;
  reference?: string;
  type?: number;
  accountType?: number;
  state?: number | string;
  achCycleId?: string;
  achBatch?: {
    id?: number;
    achCycleId?: string;
    companyName?: string;
    companyIdentification?: string;
  } | null;
};

type UploadResponse = {
  success?: boolean;
  partial?: boolean;
  message?: string;
  errors?: string[];
  traceId?: string;
  ingestionId?: string;
  ingestionStatus?: string;
  cycleResolutionStatus?: string;
  parsingStatus?: string;
  resolvedClearingHouseId?: number | null;
  resolvedAchCycleId?: string | null;
  totalBatches?: number;
  totalEntries?: number;
  totalAddendas?: number;
};

type SoapConsoleDashboard = {
  productiveStatus?: string;
  productiveExecution?: boolean;
  wouldInvokeRealSoap?: boolean;
};

type SoapConsoleCandidate = {
  operationCandidate?: string;
  wouldInvokeRealSoap?: boolean;
};

type SoapConsoleAudit = {
  eventType?: string;
  message?: string;
};

type PsqlResult = {
  stdout: string;
  stderr: string;
  status: number;
};

type SqlRow = string[];

const runUatFlag = process.env['RUN_UAT_TRANSACTION_NACHA_DISPATCH'] === 'true';
test.describe.configure({ mode: 'serial' });
test.skip(!runUatFlag, 'RUN_UAT_TRANSACTION_NACHA_DISPATCH=true requerido para ejecutar G3.4.');

const uiBaseUrl = (process.env['ACH_UI_URL'] ?? 'http://localhost:743').replace(/\/+$/, '');
const apiBaseUrl = (process.env['ACH_API_URL'] ?? 'http://localhost:843').replace(/\/+$/, '');
const username = process.env['ACH_USER'] ?? 'admin';
const password = process.env['ACH_PASS'] ?? 'Admin123!';

const loginPath = '/auth/login';
const seedPath = '/Maintenance/seed';
const financialInstitutionsPath = '/financial-institutions';
const companyEntryDescriptionsPath = '/transactions/company-entry-descriptions';
const transactionsPath = '/transactions';
const nachaExportPath = '/NachaExport';
const nachaUploadPath = '/NachaUpload/upload';
const queueUiPath = '/incoming-nacha-command-center/queue';
const soapConsoleUiPath = '/ach/nacha/soap-uat-console';

const uniqueSuffix = `${Date.now()}`.slice(-6);
const txPrefix = `UAT-G34-${new Date().toISOString().replace(/[-:]/g, '').replace(/\..+$/, '')}-${uniqueSuffix}`;
const sourceCompanyName = `UAT-G34-${uniqueSuffix}`;
const sourceCompanyIdentification = `G34${uniqueSuffix}`;
const transactionAmount = 15400 + Number(uniqueSuffix.slice(-2)) / 100;
const prenotificationTraceSequence = 6_000_001;
const transactionTraceSequence = 6_000_002;
const recipientName = 'UAT-G34-RCP';
const recipientIdNumber = '7000001';
const sourceAccountNumber = '320000000001';
const destinationAccountNumber = '320000000002';

test.describe('G3.4 Transaction -> NACHA-M -> Proc_Contrapartidas dispatch', () => {
  test('ShouldCreateTransactionGenerateOfficialNachaUploadAndExposeDispatchEvidence', async ({ page }, testInfo) => {
    const runtime = await authenticateRuntime();
    const cleanupState = {
      createdTransactionId: 0,
      createdCycleId: '',
      createdIngestionId: '',
      createdExternalFileName: '',
      createdClearingHouseId: 0,
      cleanupLabel: `${txPrefix}`
    };

    await page.addInitScript((accessToken) => {
      window.sessionStorage.setItem('ach.interbank.access_token', accessToken);
    }, runtime.token);

    try {
      await seedDatabase(runtime.token);

      const institutions = await apiGetJson<FinancialInstitution[]>(financialInstitutionsPath, runtime.token);
      const companyEntryDescriptions = await apiGetJson<CompanyEntryDescription[]>(companyEntryDescriptionsPath, runtime.token);

      const defaultSource = institutions.find((item) => item.isDefaultSource) ?? null;
      expect(defaultSource, 'Debe existir una FinancialInstitution default source activa.').not.toBeNull();

      const achColClearingHouse = await querySingleRow(`
        SELECT "Id" AS id, "Name" AS name, "Code" AS code
        FROM "ClearingHouses"
        WHERE "Code" = 'ACHCOL'
           OR "Name" = 'ACH Colombia'
        ORDER BY "Id"
        LIMIT 1;
      `, (row) => ({
        id: Number(row[0]),
        name: row[1] ?? '',
        code: row[2] ?? ''
      }));
      expect(achColClearingHouse, 'Debe existir la cámara ACH Colombia activa en PostgreSQL real.').not.toBeNull();

      const achColInstitution = await querySingleRow(`
        SELECT "Id" AS id, "Name" AS name, "IsDefaultSource" AS "isDefaultSource", "RoutingNumber" AS "routingNumber",
               "TransitCode" AS "transitCode", "Status" AS status
        FROM "FinancialInstitutions"
        WHERE "Name" = 'ACH Colombia'
          AND COALESCE("Status", 0) = 1
        ORDER BY "Id"
        LIMIT 1;
      `, (row) => ({
        id: Number(row[0]),
        name: row[1] ?? '',
        isDefaultSource: row[2] === 't',
        routingNumber: row[3] ?? '',
        transitCode: row[4] ?? '',
        status: Number(row[5] ?? 0)
      }));
      expect(achColInstitution, 'Debe existir una institución activa de ACH Colombia para el flujo G3.4 inicial.').not.toBeNull();

      const companyEntryDescription = companyEntryDescriptions.find((item) => /NOMINAS/i.test(item.term ?? ''))
        ?? companyEntryDescriptions.find((item) => item.isActive !== false)
        ?? null;
      expect(companyEntryDescription, 'Debe existir un CompanyEntryDescription activo para la transacción sintética.').not.toBeNull();

      const defaultOriginCode = buildOriginCode(defaultSource!);
      const sequenceScope = 'ACH_EXTERNAL_NAME';

      const prenotificationTx = await apiPostJson<CreatedTransaction>(transactionsPath, runtime.token, {
        amount: 0,
        transactionExternalId: `${txPrefix}-PN`,
        reference: `${txPrefix.slice(-20)}-PN`,
        type: 2,
        accountType: 1,
        isPrenotification: true,
        destinationInstitutionId: achColInstitution!.id,
        sourceAccountNumber,
        destinationAccountNumber,
        recipientIdNumber,
        recipientName,
        requiresIdentityValidation: false,
        companyName: sourceCompanyName,
        companyIdentification: sourceCompanyIdentification,
        companyEntryDescriptionId: companyEntryDescription!.id,
        sourcePersonType: 'PJ',
        recipientPersonType: 'PN',
        addendas: [
          {
            addendaType: '05',
            information: `${txPrefix}-PN-ADD`
          }
        ]
      });

      await executeSql(`
        UPDATE "AchTransactions"
        SET "EffectiveEntryDate" = CURRENT_DATE - INTERVAL '7 days'
        WHERE "Id" = ${prenotificationTx.id}
          AND "TransactionExternalId" = '${prenotificationTx.transactionExternalId}';
      `);

      const prenotificationDateCheck = await queryRows(`
        SELECT TO_CHAR("EffectiveEntryDate"::date, 'YYYY-MM-DD')
        FROM "AchTransactions"
        WHERE "Id" = ${prenotificationTx.id};
      `);
      expect(prenotificationDateCheck.length, 'La prenotificación debe seguir persistida tras el ajuste de fecha.').toBeGreaterThan(0);
      expect(prenotificationDateCheck[0][0]).not.toBe((prenotificationTx as { effectiveEntryDate?: string }).effectiveEntryDate?.slice(0, 10));

      const createdTx = await apiPostJson<CreatedTransaction>(transactionsPath, runtime.token, {
        amount: transactionAmount,
        transactionExternalId: `${txPrefix}-TX`,
        reference: `${txPrefix.slice(-20)}-REF`,
        type: 1,
        accountType: 1,
        isPrenotification: false,
        destinationInstitutionId: achColInstitution!.id,
        sourceAccountNumber,
        destinationAccountNumber,
        recipientIdNumber,
        recipientName,
        requiresIdentityValidation: false,
        companyName: sourceCompanyName,
        companyIdentification: sourceCompanyIdentification,
        companyEntryDescriptionId: companyEntryDescription!.id,
        sourcePersonType: 'PJ',
        recipientPersonType: 'PN',
        addendas: [
          {
            addendaType: '05',
            information: `${txPrefix}-ADD`
          }
        ]
      });

      await executeSql(`
        UPDATE "AchTransactions"
        SET "TraceSequenceNumber" = ${prenotificationTraceSequence},
            "TraceNumber" = '00001283${prenotificationTraceSequence.toString().padStart(7, '0')}'
        WHERE "Id" = ${prenotificationTx.id};

        UPDATE "AchTransactions"
        SET "TraceSequenceNumber" = ${transactionTraceSequence},
            "TraceNumber" = '00001283${transactionTraceSequence.toString().padStart(7, '0')}'
        WHERE "Id" = ${createdTx.id};

        UPDATE "AchBatches"
        SET "BatchSequenceNumber" = 1
        WHERE "Id" = ${createdTx.achBatchId ?? 0};

        UPDATE "AchBatches"
        SET "BatchSequenceNumber" = 2
        WHERE "Id" = ${prenotificationTx.achBatchId ?? 0};
      `);

      await executeSql(`
        DELETE FROM "BatchNumberSequences"
        WHERE "ClearingHouseId" = 'ACH'
          AND "OriginatingDfi" = '${createdTx.originatingDFI}'
          AND "ProcessingDate" = DATE '${(createdTx.achCycle?.processingDate ?? new Date().toISOString()).slice(0, 10)}'
          AND "PolicyCode" = 'DAILY_RESET_BY_CHAMBER_DATE_ORIGINATING_DFI';
      `);

      cleanupState.createdTransactionId = createdTx.id;
      cleanupState.createdCycleId = createdTx.achCycleId ?? createdTx.achBatch?.achCycleId ?? '';
      expect(cleanupState.createdCycleId, 'La transacción creada debe resolver un ciclo ACH válido.').not.toEqual('');

      const sequenceDate = (createdTx.achCycle?.processingDate ?? new Date().toISOString()).slice(0, 10);
      const expectedSequence = await resolveExpectedSequence(sequenceScope, achColClearingHouse!.id, sequenceDate);

      const exported = await downloadNachaFile(runtime.token, cleanupState.createdCycleId);
      cleanupState.createdExternalFileName = exported.fileName;
      cleanupState.createdClearingHouseId = achColClearingHouse!.id;

      expect(exported.fileName).toMatch(/^\d{7}\.\d{3}\.1$/);
      expect(exported.content.length).toBeGreaterThan(0);

      const externalFileRegistryRows = await queryRows(`
        SELECT "ClearingHouseId", "ExternalFileType", "Direction", "FlowCode", "ExternalFileName", "ExternalSequence", "ProcessingDate"
        FROM "ExternalFileNameRegistry"
        WHERE "ExternalFileType" = 'NachaOut'
          AND "Direction" = 'Outbound'
          AND "FlowCode" = 'Originacion'
        ORDER BY "CreatedAtUtc" DESC
        LIMIT 3;
      `);

      expect(externalFileRegistryRows.length, 'La exportación debe persistir ExternalFileNameRegistry.').toBeGreaterThan(0);
      expect(externalFileRegistryRows.some((row) => row[4] === exported.fileName)).toBeTruthy();

      const sequenceRowsBeforeUpload = await queryRows(`
        SELECT "ClearingHouseId", "ScopeCode", "SequenceDate", "LastValue"
        FROM "ExternalFileSequences"
        WHERE "ClearingHouseId" = ${cleanupState.createdClearingHouseId}
          AND "ScopeCode" = '${sequenceScope}'
          AND "SequenceDate" = DATE '${sequenceDate}'
        ORDER BY "UpdatedAtUtc" DESC;
      `);
      expect(sequenceRowsBeforeUpload.length, 'Debe existir evidencia de secuencia ACH actual antes del upload.').toBeGreaterThan(0);
      expect(sequenceRowsBeforeUpload[0][3]).toBe(expectedSequence.toString());

      const upload = await uploadNachaFile(runtime.token, `${exported.fileName}.ach`, exported.content);
      cleanupState.createdIngestionId = upload.ingestionId ?? '';

      if (!(upload.success ?? true)) {
        throw new Error(`Upload NACHA rechazado: ${JSON.stringify(upload, null, 2)}`);
      }
      expect(upload.ingestionId, 'El upload debe devolver ingestionId para correlación.').toBeTruthy();

      await expect.poll(async () => countRows(`
        SELECT COUNT(*)
        FROM "IncomingNachaDispatchQueue" q
        JOIN "AchTransactions" t ON t."Id" = q."AchTransactionId"
        WHERE t."TransactionExternalId" LIKE '${txPrefix}%';
      `), {
        timeout: 180_000,
        intervals: [5_000, 10_000]
      }).toBeGreaterThan(0);

      await expect.poll(async () => countRows(`
        SELECT COUNT(*)
        FROM "ContrapartidaDispatchItems" i
        JOIN "AchTransactions" t ON t."Id" = i."AchTransactionId"
        WHERE t."TransactionExternalId" LIKE '${txPrefix}%';
      `), {
        timeout: 180_000,
        intervals: [5_000, 10_000]
      }).toBeGreaterThan(0);

      await expect.poll(async () => countRows(`
        SELECT COUNT(*)
        FROM "ContrapartidaDispatchAttempts" a
        JOIN "ContrapartidaDispatchItems" i ON i."Id" = a."DispatchItemId"
        JOIN "AchTransactions" t ON t."Id" = i."AchTransactionId"
        WHERE t."TransactionExternalId" LIKE '${txPrefix}%'
          AND COALESCE(a."RequestPayloadXml", '') <> '';
      `), {
        timeout: 240_000,
        intervals: [5_000, 10_000]
      }).toBeGreaterThan(0);

      const contrapartidaEvidence = await queryRows(`
        SELECT b."Id", b."AchCycleId", b."ClearingHouseId", COALESCE(b."RequestPayloadXml", ''), COALESCE(b."ResponsePayloadXml", ''),
               COALESCE(a."RequestPayloadXml", ''), COALESCE(a."ResponsePayloadXml", ''), t."TransactionExternalId"
        FROM "ContrapartidaDispatchBatches" b
        JOIN "ContrapartidaDispatchAttempts" a ON a."DispatchBatchId" = b."Id"
        JOIN "ContrapartidaDispatchItems" i ON i."Id" = a."DispatchItemId"
        JOIN "AchTransactions" t ON t."Id" = i."AchTransactionId"
        WHERE t."TransactionExternalId" LIKE '${txPrefix}%'
        ORDER BY b."TriggeredAtUtc" DESC, a."StartedAtUtc" DESC
        LIMIT 5;
      `);

      expect(contrapartidaEvidence.length, 'Debe existir evidencia de dispatch Proc_Contrapartidas con envelope/request persistido.').toBeGreaterThan(0);
      expect(contrapartidaEvidence.some((row) => row[3].includes('Proc_Contrapartidas') || row[5].includes('Proc_Contrapartidas'))).toBeTruthy();
      expect(contrapartidaEvidence.some((row) => row[4].length > 0 || row[6].length > 0)).toBeTruthy();

      const soapConsoleReady = await fetchSoapConsole(runtime.token);
      expect(soapConsoleReady.dashboard.wouldInvokeRealSoap).toBeFalsy();
      expect(soapConsoleReady.dashboard.productiveExecution).toBeFalsy();
      expect(soapConsoleReady.dashboard.productiveStatus).toBe('NO-GO');
      expect(soapConsoleReady.candidates.some((candidate) => candidate.operationCandidate === 'ProcContrapartidas')).toBeTruthy();
      expect(soapConsoleReady.candidates.some((candidate) => candidate.wouldInvokeRealSoap === false)).toBeTruthy();

      await expect.poll(async () => countRows(`
        SELECT COUNT(*)
        FROM "IncomingNachaDispatchQueue" q
        JOIN "IncomingNachaIntegrationExecution" e ON e."DispatchQueueId" = q."Id"
        JOIN "AchTransactions" t ON t."Id" = q."AchTransactionId"
        WHERE t."TransactionExternalId" LIKE '${txPrefix}%'
          AND e."MethodName" = 'Proc_Contrapartidas';
      `), {
        timeout: 120_000,
        intervals: [5_000, 10_000]
      }).toBeGreaterThan(0);

      const incomingExecutionRows = await queryRows(`
        SELECT q."Id", q."QueueStatus", q."IdempotencyDispatchKey", e."MethodName", COALESCE(e."RequestPayloadXml", ''), COALESCE(e."ResponsePayloadXml", '')
        FROM "IncomingNachaDispatchQueue" q
        JOIN "IncomingNachaIntegrationExecution" e ON e."DispatchQueueId" = q."Id"
        JOIN "AchTransactions" t ON t."Id" = q."AchTransactionId"
        WHERE t."TransactionExternalId" LIKE '${txPrefix}%'
          AND e."MethodName" = 'Proc_Contrapartidas'
        ORDER BY e."StartedAtUtc" DESC
        LIMIT 3;
      `);

      expect(incomingExecutionRows.length, 'Debe existir IncomingNachaIntegrationExecution para Proc_Contrapartidas. Si falta, hay brecha backend.').toBeGreaterThan(0);
      expect(incomingExecutionRows.some((row) => row[4].length > 0)).toBeTruthy();
      expect(incomingExecutionRows.some((row) => row[5].length > 0 || row[5] === 'PROC_DRY_RUN' || row[5] === 'PROC_DISABLED')).toBeTruthy();

      await page.goto(`${queueUiPath}?ingestionId=${encodeURIComponent(upload.ingestionId ?? '')}`);
      await expect(page.getByRole('heading', { name: 'Cola dispatch inbound NACHA-M', level: 1 })).toBeVisible();
      await expect(page.getByText(upload.ingestionId ?? '', { exact: false })).toBeVisible();

      await page.goto(soapConsoleUiPath);
      await expect(page.getByRole('heading', { name: 'Consola SOAP/UAT solo lectura', level: 1 })).toBeVisible();
      await expect(page.getByText('Productivo NO-GO', { exact: false })).toBeVisible();
      await expect(page.getByText('ProcContrapartidas', { exact: false })).toBeVisible();
      await expect(page.getByText('false', { exact: false })).toBeVisible();

      await testInfo.attach('g34-evidence.json', {
        body: JSON.stringify({
          runtime: {
            uiBaseUrl,
            apiBaseUrl
          },
          defaultOriginCode,
          expectedSequence,
          transaction: createdTx,
          exportFileName: exported.fileName,
          upload,
          externalFileRegistryRows,
          sequenceRowsBeforeUpload,
          contrapartidaEvidence,
          incomingExecutionRows,
          soapConsoleReady
        }, null, 2),
        contentType: 'application/json'
      });
    } finally {
      await cleanupCorrelatedData();
    }
  });
});

async function authenticateRuntime(): Promise<{ token: string }> {
  const response = await fetch(joinUrl(apiBaseUrl, loginPath), {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username, password })
  });

  expect(response.ok, 'Debe autenticarse contra el API real para ejecutar G3.4.').toBeTruthy();
  const payload = await response.json() as AuthLoginResponse;
  const token = payload.data?.token;
  expect(token, 'El login debe devolver access token.').toBeTruthy();
  return { token: token as string };
}

async function seedDatabase(token: string): Promise<void> {
  const response = await fetch(joinUrl(apiBaseUrl, seedPath), {
    method: 'POST',
    headers: authHeaders(token)
  });

  expect(response.ok, 'El seeding real debe completar antes del flujo G3.4.').toBeTruthy();
}

async function apiGetJson<T>(path: string, token: string): Promise<T> {
  const response = await fetch(joinUrl(apiBaseUrl, path), {
    headers: authHeaders(token)
  });

  expect(response.ok, `GET ${path} debe responder 200.`).toBeTruthy();
  return await response.json() as T;
}

async function apiPostJson<T>(path: string, token: string, body: unknown): Promise<T> {
  const response = await fetch(joinUrl(apiBaseUrl, path), {
    method: 'POST',
    headers: authHeaders(token, true),
    body: JSON.stringify(body)
  });

  if (!(response.ok || response.status === 201)) {
    throw new Error(`POST ${path} debe responder 200/201. Status=${response.status}, body=${await response.text()}`);
  }
  return await response.json() as T;
}

async function downloadNachaFile(token: string, cycleId: string): Promise<{ fileName: string; content: string }> {
  const response = await fetch(joinUrl(apiBaseUrl, `${nachaExportPath}/${encodeURIComponent(cycleId)}`), {
    headers: authHeaders(token)
  });

  if (!response.ok) {
    throw new Error(`La exportación NACHA debe responder OK. Status=${response.status}, body=${await response.text()}`);
  }
  const disposition = response.headers.get('content-disposition') ?? '';
  const fileName = parseFileNameFromDisposition(disposition);
  expect(fileName, 'La exportación debe incluir filename en Content-Disposition.').toBeTruthy();

  return {
    fileName,
    content: await response.text()
  };
}

async function uploadNachaFile(token: string, fileName: string, content: string): Promise<UploadResponse> {
  const form = new FormData();
  form.append('file', new Blob([content], { type: 'text/plain' }), fileName);

  const response = await fetch(joinUrl(apiBaseUrl, nachaUploadPath), {
    method: 'POST',
    headers: authHeaders(token, false),
    body: form
  });

  if (!response.ok) {
    throw new Error(`Upload NACHA debe responder OK, obtuvo ${response.status}. body=${await response.text()}`);
  }
  return await response.json() as UploadResponse;
}

async function fetchSoapConsole(token: string): Promise<{ dashboard: SoapConsoleDashboard; candidates: SoapConsoleCandidate[]; audit: SoapConsoleAudit[] }> {
  const [dashboard, candidates, audit] = await Promise.all([
    apiGetJson<SoapConsoleDashboard>('/api/ach/nacha/soap-uat-console/dashboard', token),
    apiGetJson<SoapConsoleCandidate[]>('/api/ach/nacha/soap-uat-console/candidates', token),
    apiGetJson<SoapConsoleAudit[]>('/api/ach/nacha/soap-uat-console/audit', token)
  ]);

  return { dashboard, candidates, audit };
}

async function resolveExpectedSequence(scopeCode: string, clearingHouseId: number, sequenceDate: string): Promise<number> {
  const rows = await queryRows(`
    SELECT COALESCE(MAX("LastValue"), 0)::int
    FROM "ExternalFileSequences"
    WHERE "ClearingHouseId" = ${clearingHouseId}
      AND "ScopeCode" = '${scopeCode}'
      AND "SequenceDate" = DATE '${sequenceDate}';
  `);

  const lastValue = rows.length > 0 ? Number(rows[0][0]) : 0;
  return lastValue + 1;
}

function buildOriginCode(source: FinancialInstitution): string {
  const routing = (source.routingNumber ?? '').trim();
  const transit = (source.transitCode ?? '').trim();
  const origin = `${routing.slice(-4)}${transit}`;
  if (!/^\d{7}$/.test(origin)) {
    throw new Error('La institucion financiera origen no permite derivar RRRRTTT.');
  }

  return origin;
}

function parseFileNameFromDisposition(disposition: string): string {
  const match = /filename\*?=(?:UTF-8''|")?([^\";]+)"?/i.exec(disposition);
  return match?.[1]?.trim() ?? '';
}

function authHeaders(token: string, json = false): HeadersInit {
  return {
    Authorization: `Bearer ${token}`,
    ...(json ? { 'Content-Type': 'application/json' } : {})
  };
}

function joinUrl(base: string, path: string): string {
  if (/^https?:\/\//i.test(path)) {
    return path;
  }

  return `${base}${path.startsWith('/') ? '' : '/'}${path}`;
}

async function queryRows(sql: string): Promise<SqlRow[]> {
  const result = runPsql(sql);
  expect(result.status, `psql debe ejecutar sin error. stderr=${result.stderr}`).toBe(0);
  const stdout = result.stdout.trim();
  if (!stdout) {
    return [];
  }

  return stdout.split(/\r?\n/).filter(Boolean).map((line) => line.split('|'));
}

async function executeSql(sql: string): Promise<void> {
  const result = runPsql(sql);
  expect(result.status, `psql debe ejecutar sin error. stderr=${result.stderr}`).toBe(0);
}

async function querySingleRow<T>(sql: string, mapRow: (row: SqlRow) => T): Promise<T | null> {
  const rows = await queryRows(sql);
  if (rows.length === 0) {
    return null;
  }

  return mapRow(rows[0]);
}

async function countRows(sql: string): Promise<number> {
  const rows = await queryRows(sql);
  if (rows.length === 0 || rows[0].length === 0) {
    return 0;
  }

  return Number(rows[0][0] ?? 0);
}

function runPsql(sql: string): PsqlResult {
  const result = spawnSync(
    'docker',
    ['compose', 'exec', '-T', 'postgres', 'psql', '-U', 'example_user', '-d', 'ACHInterbank', '-At', '-F', '|', '-c', sql],
    {
      encoding: 'utf8',
      shell: false,
      maxBuffer: 20 * 1024 * 1024
    }
  );

  return {
    stdout: result.stdout ?? '',
    stderr: result.stderr ?? '',
    status: typeof result.status === 'number' ? result.status : 1
  };
}

async function cleanupCorrelatedData(): Promise<void> {
  const cleanupSql = `
    DELETE FROM "ContrapartidaDispatchBatches"
    WHERE "Id" IN (
      SELECT a."DispatchBatchId"
      FROM "ContrapartidaDispatchAttempts" a
      JOIN "ContrapartidaDispatchItems" i ON i."Id" = a."DispatchItemId"
      JOIN "AchTransactions" t ON t."Id" = i."AchTransactionId"
      WHERE t."TransactionExternalId" LIKE '${txPrefix}%'
        AND a."DispatchBatchId" IS NOT NULL
    );

    DELETE FROM "ContrapartidaDispatchAttempts"
    WHERE "DispatchItemId" IN (
      SELECT i."Id"
      FROM "ContrapartidaDispatchItems" i
      JOIN "AchTransactions" t ON t."Id" = i."AchTransactionId"
      WHERE t."TransactionExternalId" LIKE '${txPrefix}%'
    );

    DELETE FROM "ContrapartidaDispatchItems"
    WHERE "AchTransactionId" IN (
      SELECT "Id" FROM "AchTransactions" WHERE "TransactionExternalId" LIKE '${txPrefix}%'
    );

    DELETE FROM "IncomingNachaIntegrationExecution"
    WHERE "DispatchQueueId" IN (
      SELECT q."Id"
      FROM "IncomingNachaDispatchQueue" q
      JOIN "AchTransactions" t ON t."Id" = q."AchTransactionId"
      WHERE t."TransactionExternalId" LIKE '${txPrefix}%'
    );

    DELETE FROM "IncomingNachaDispatchQueue"
    WHERE "ClearingHouseId" = 1
      AND "OperationalDate" = CURRENT_DATE;

    DELETE FROM "IncomingNachaProcessingEvents"
    WHERE "IncomingNachaFileIngestionId" IN (
      SELECT DISTINCT l."IncomingNachaFileIngestionId"
      FROM "IncomingNachaTransactionLinks" l
      JOIN "AchTransactions" t ON t."Id" = l."AchTransactionId"
      WHERE t."TransactionExternalId" LIKE '${txPrefix}%'
    )
    OR "AchTransactionId" IN (
      SELECT "Id" FROM "AchTransactions" WHERE "TransactionExternalId" LIKE '${txPrefix}%'
    );

    DELETE FROM "IncomingNachaTransactionLinks"
    WHERE "IncomingNachaFileIngestionId" IN (
      SELECT DISTINCT l."IncomingNachaFileIngestionId"
      FROM "IncomingNachaTransactionLinks" l
      JOIN "AchTransactions" t ON t."Id" = l."AchTransactionId"
      WHERE t."TransactionExternalId" LIKE '${txPrefix}%'
    );

    DELETE FROM "IncomingNachaEntryClassifications"
    WHERE "IncomingNachaFileIngestionId" IN (
      SELECT DISTINCT l."IncomingNachaFileIngestionId"
      FROM "IncomingNachaTransactionLinks" l
      JOIN "AchTransactions" t ON t."Id" = l."AchTransactionId"
      WHERE t."TransactionExternalId" LIKE '${txPrefix}%'
    );

    DELETE FROM "IncomingNachaFileProcessingResults"
    WHERE "IncomingNachaFileIngestionId" IN (
      SELECT DISTINCT l."IncomingNachaFileIngestionId"
      FROM "IncomingNachaTransactionLinks" l
      JOIN "AchTransactions" t ON t."Id" = l."AchTransactionId"
      WHERE t."TransactionExternalId" LIKE '${txPrefix}%'
    );

    DELETE FROM "AddendaRecords"
    WHERE "NachaID" IN (
      SELECT "NachaID"
      FROM "NachaHeaders"
      WHERE "IncomingNachaFileIngestionId" IN (
        SELECT DISTINCT l."IncomingNachaFileIngestionId"
        FROM "IncomingNachaTransactionLinks" l
        JOIN "AchTransactions" t ON t."Id" = l."AchTransactionId"
        WHERE t."TransactionExternalId" LIKE '${txPrefix}%'
      )
    );

    DELETE FROM "EntryDetails"
    WHERE "NachaID" IN (
      SELECT "NachaID"
      FROM "NachaHeaders"
      WHERE "IncomingNachaFileIngestionId" IN (
        SELECT DISTINCT l."IncomingNachaFileIngestionId"
        FROM "IncomingNachaTransactionLinks" l
        JOIN "AchTransactions" t ON t."Id" = l."AchTransactionId"
        WHERE t."TransactionExternalId" LIKE '${txPrefix}%'
      )
    );

    DELETE FROM "BatchControls"
    WHERE "NachaID" IN (
      SELECT "NachaID"
      FROM "NachaHeaders"
      WHERE "IncomingNachaFileIngestionId" IN (
        SELECT DISTINCT l."IncomingNachaFileIngestionId"
        FROM "IncomingNachaTransactionLinks" l
        JOIN "AchTransactions" t ON t."Id" = l."AchTransactionId"
        WHERE t."TransactionExternalId" LIKE '${txPrefix}%'
      )
    );

    DELETE FROM "BatchHeaders"
    WHERE "NachaID" IN (
      SELECT "NachaID"
      FROM "NachaHeaders"
      WHERE "IncomingNachaFileIngestionId" IN (
        SELECT DISTINCT l."IncomingNachaFileIngestionId"
        FROM "IncomingNachaTransactionLinks" l
        JOIN "AchTransactions" t ON t."Id" = l."AchTransactionId"
        WHERE t."TransactionExternalId" LIKE '${txPrefix}%'
      )
    );

    DELETE FROM "FileControls"
    WHERE "NachaID" IN (
      SELECT "NachaID"
      FROM "NachaHeaders"
      WHERE "IncomingNachaFileIngestionId" IN (
        SELECT DISTINCT l."IncomingNachaFileIngestionId"
        FROM "IncomingNachaTransactionLinks" l
        JOIN "AchTransactions" t ON t."Id" = l."AchTransactionId"
        WHERE t."TransactionExternalId" LIKE '${txPrefix}%'
      )
    );

    DELETE FROM "NachaHeaders"
    WHERE "IncomingNachaFileIngestionId" IN (
      SELECT DISTINCT l."IncomingNachaFileIngestionId"
      FROM "IncomingNachaTransactionLinks" l
      JOIN "AchTransactions" t ON t."Id" = l."AchTransactionId"
      WHERE t."TransactionExternalId" LIKE '${txPrefix}%'
    );

    DELETE FROM "IncomingNachaFileIngestions"
    WHERE "Id" IN (
      SELECT DISTINCT l."IncomingNachaFileIngestionId"
      FROM "IncomingNachaTransactionLinks" l
      JOIN "AchTransactions" t ON t."Id" = l."AchTransactionId"
      WHERE t."TransactionExternalId" LIKE '${txPrefix}%'
    );

    DELETE FROM "IncomingNachaDispatchQueue"
    WHERE "ClearingHouseId" = 1
      AND "OperationalDate" = CURRENT_DATE;

    DELETE FROM "CustomerAccounts"
    WHERE "CustomerId" IN (
      SELECT "Id"
      FROM "Customers"
      WHERE "CompanyName" LIKE 'UAT-G34-%'
         OR "DocumentNumber" = '${sourceCompanyIdentification}'
         OR "DocumentNumber" = '${recipientIdNumber}'
    );

    DELETE FROM "CustomerThirdParties"
    WHERE "CustomerId" IN (
      SELECT "Id"
      FROM "Customers"
      WHERE "CompanyName" LIKE 'UAT-G34-%'
         OR "DocumentNumber" = '${sourceCompanyIdentification}'
         OR "DocumentNumber" = '${recipientIdNumber}'
    );

    DELETE FROM "Customers"
    WHERE "CompanyName" LIKE 'UAT-G34-%'
       OR "DocumentNumber" = '${sourceCompanyIdentification}'
       OR "DocumentNumber" = '${recipientIdNumber}';

    DELETE FROM "AchTransactionStateEvents"
    WHERE "AchTransactionId" IN (
      SELECT "Id" FROM "AchTransactions" WHERE "TransactionExternalId" LIKE '${txPrefix}%'
    );

    DELETE FROM "AchTransactions"
    WHERE "TransactionExternalId" LIKE '${txPrefix}%'
       OR "AchBatchId" IN (
         SELECT "Id"
         FROM "AchBatches"
         WHERE "CompanyName" LIKE 'UAT-G34-%'
            OR "CompanyIdentification" = '${sourceCompanyIdentification}'
       );

    DELETE FROM "AchBatches"
    WHERE "CompanyName" LIKE 'UAT-G34-%'
       OR "CompanyIdentification" = '${sourceCompanyIdentification}';
  `;

  const result = runPsql(cleanupSql);
  if (result.status !== 0) {
    throw new Error(`Cleanup UAT falló. stderr=${result.stderr}`);
  }
}
