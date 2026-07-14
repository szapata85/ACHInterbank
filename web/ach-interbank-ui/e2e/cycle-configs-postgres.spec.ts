import { expect, Page, test, TestInfo } from '@playwright/test';
import { G36Postgres } from './support/g36-postgres';
import { G36SqlServer, sqlString } from './support/g36-sqlserver';

type LoginResponse = {
  data?: {
    token?: string;
  };
};

type ClearingHouseRow = {
  id: number;
  name: string;
};

type CycleConfigRow = {
  id: number;
  clearingHouseId: number;
  clearingHouseName: string | null;
  cycleName: string;
  startTime: string;
  endTime: string;
  cutoffTime: string;
  isActive: boolean;
  effectiveFrom: string;
  effectiveTo: string | null;
  isCurrent: boolean;
};

type ObservedRequest = {
  url: string;
  method: string;
  authorization: string | null;
};

type ObservedResponse = {
  url: string;
  method: string;
  status: number;
  contentType: string;
  bodySnippet: string;
  isHtml: boolean;
};

type RuntimeProvider = 'SqlServer' | 'Postgres';

const e2eBaseUrl = (process.env['E2E_BASE_URL'] ?? process.env['ACH_UI_URL'] ?? 'http://localhost:743').replace(/\/+$/, '');
const spaBaseUrl = e2eBaseUrl;
const username = process.env['E2E_ADMIN_USER'] ?? process.env['ACH_USER'] ?? 'admin';
const password = process.env['E2E_ADMIN_PASSWORD'] ?? process.env['ACH_PASS'] ?? 'Admin123!';
const effectiveAt = new Date().toISOString().slice(0, 10);

test.describe.configure({ mode: 'serial' });

test.describe('Cycle configs SPA + runtime database', () => {
  test('Network diagnostics should proxy JSON and attach Bearer', async ({ page }, testInfo) => {
    test.setTimeout(120_000);
    const db = createDb();
    try {
      const authToken = await login(page);
      await seedSession(page, authToken);

      const observed = observeNetwork(page);
      const housesResponsePromise = page.waitForResponse((response) =>
        new URL(response.url()).pathname === '/clearing-houses' && response.request().method() === 'GET');
      const cycleConfigResponsePromise = page.waitForResponse((response) =>
        new URL(response.url()).pathname === '/clearing-house-cycle-configs' && response.request().method() === 'GET');

      await page.goto(`${spaBaseUrl}/transactions/cycle-configs`);
      const housesResponse = await housesResponsePromise;
      const housesBody = await housesResponse.text();
      const houses = await db.findClearingHouses(['ACH Colombia', 'CENIT']);
      expect(houses.length, 'La base runtime debe contener ACH Colombia y CENIT.').toBeGreaterThanOrEqual(2);

      const filterPanel = page.locator('section.panel').first();
      await expect(filterPanel.getByRole('button', { name: /ACH Colombia/i })).toBeVisible();

      await filterPanel.getByRole('button', { name: /ACH Colombia/i }).click();
      const consultButton = filterPanel.getByRole('button', { name: 'Consultar' });
      await expect(consultButton).toBeEnabled();

      const cycleConfigRequestPromise = page.waitForRequest((request) =>
        new URL(request.url()).pathname === '/clearing-house-cycle-configs' && request.method() === 'GET');

      await consultButton.click();

      const cycleConfigRequest = await cycleConfigRequestPromise;
      const cycleConfigResponse = await cycleConfigResponsePromise;
      const cycleConfigBody = await cycleConfigResponse.text();

      const requestEvidence = {
        provider: db.providerName,
        clearingHouses: {
          url: housesResponse.url(),
          method: housesResponse.request().method(),
          status: housesResponse.status(),
          contentType: housesResponse.headers()['content-type'] ?? '',
          bodySnippet: bodySnippet(housesBody)
        },
        cycleConfigs: {
          url: cycleConfigResponse.url(),
          method: cycleConfigResponse.request().method(),
          requestAuthorization: cycleConfigRequest.headers()['authorization'] ?? null,
          status: cycleConfigResponse.status(),
          contentType: cycleConfigResponse.headers()['content-type'] ?? '',
          bodySnippet: bodySnippet(cycleConfigBody)
        },
        consoleErrors: observed.consoleErrors,
        requestFailures: observed.requestFailures
      };

      await testInfo.attach('cycle-configs-network-evidence.json', {
        body: JSON.stringify(requestEvidence, null, 2),
        contentType: 'application/json'
      });

      expect(requestEvidence.cycleConfigs.requestAuthorization, 'La solicitud protegida debe llevar Authorization Bearer.').toMatch(/^Bearer\s+/);
      expect(requestEvidence.cycleConfigs.status).not.toBe(401);
      expect(requestEvidence.cycleConfigs.status).not.toBe(403);
      expect(requestEvidence.cycleConfigs.status).not.toBe(500);
      expect(requestEvidence.cycleConfigs.contentType.toLowerCase()).toContain('application/json');
      expect(requestEvidence.cycleConfigs.bodySnippet).not.toMatch(/<html|<!doctype html/i);
      expect(requestEvidence.cycleConfigs.bodySnippet).not.toContain('index.html');
      expect(observed.consoleErrors, JSON.stringify(observed.consoleErrors, null, 2)).toEqual([]);
      expect(observed.requestFailures, JSON.stringify(observed.requestFailures, null, 2)).toEqual([]);
    } finally {
      await db.close();
    }
  });

  test('Reading should match runtime database for ACH Colombia and CENIT', async ({ page }, testInfo) => {
    test.setTimeout(180_000);
    const db = createDb();
    try {
      const authToken = await login(page);
      await seedSession(page, authToken);

      const expectedHouses = await db.findClearingHouses(['ACH Colombia', 'CENIT']);
      expect(expectedHouses.length, 'Deben existir ACH Colombia y CENIT en la base runtime.').toBeGreaterThanOrEqual(2);

      await page.goto(`${spaBaseUrl}/transactions/cycle-configs`);
      const filterPanel = page.locator('section.panel').first();
      const resultsPanel = page.locator('section.panel').last();

      await expect(filterPanel.getByRole('button', { name: /ACH Colombia/i })).toBeVisible();
      await expect(filterPanel.getByRole('button', { name: /CENIT/i })).toBeVisible();

      const resultEvidence: Array<Record<string, unknown>> = [];
      for (const houseName of ['ACH Colombia', 'CENIT']) {
        const house = expectedHouses.find((item) => item.name === houseName);
        expect(house, `La cámara ${houseName} debe existir en la base runtime.`).toBeTruthy();

        const responsePromise = page.waitForResponse((response) =>
          new URL(response.url()).pathname === '/clearing-house-cycle-configs'
          && response.request().method() === 'GET'
          && new URL(response.url()).searchParams.get('clearingHouseId') === String(house!.id));
        const requestPromise = page.waitForRequest((request) =>
          new URL(request.url()).pathname === '/clearing-house-cycle-configs'
          && request.method() === 'GET'
          && new URL(request.url()).searchParams.get('clearingHouseId') === String(house!.id));

        await filterPanel.getByRole('button', { name: new RegExp(`^${escapeRegex(houseName)}$`, 'i') }).click();
        await filterPanel.getByRole('button', { name: 'Consultar' }).click();

        const request = await requestPromise;
        const response = await responsePromise;
        const body = await response.text();
        const apiRows = parseCycleConfigResponse(body);
        const dbRows = await db.listCycleConfigs(house!.id, effectiveAt);

        await expect(resultsPanel.locator('.ag-center-cols-container .ag-row').first()).toBeVisible();
        const uiRows = await resultsPanel.locator('.ag-center-cols-container .ag-row').allInnerTexts();

        expect(request.headers()['authorization'], `${houseName} debe enviar Authorization Bearer.`).toMatch(/^Bearer\s+/);
        expect(new URL(request.url()).searchParams.get('effectiveAt')).toBe(effectiveAt);
        expect(response.status(), `${houseName} no debe responder 401/403/500.`).not.toBeGreaterThanOrEqual(500);
        expect(response.headers()['content-type'] ?? '').toContain('application/json');
        expect(body).not.toMatch(/<html|<!doctype html/i);
        expect(apiRows.length, `${houseName} debe devolver la misma cantidad de filas que la base runtime.`).toBe(dbRows.length);
        expect(uiRows.length, `${houseName} debe mostrar la misma cantidad de filas que la API/DB.`).toBe(dbRows.length);
        for (const row of dbRows) {
          expect(uiRows.join('\n')).toContain(row.cycleName);
        }

        resultEvidence.push({
          provider: db.providerName,
          houseName,
          clearingHouseId: house!.id,
          requestUrl: request.url(),
          requestAuthorization: request.headers()['authorization'] ?? null,
          status: response.status(),
          contentType: response.headers()['content-type'] ?? '',
          apiRows: apiRows.length,
          dbRows: dbRows.length,
          uiRows: uiRows.length,
          visibleCycleNames: dbRows.map((row) => row.cycleName)
        });
      }

      await testInfo.attach('cycle-configs-read-evidence.json', {
        body: JSON.stringify({
          provider: db.providerName,
          effectiveAt,
          houses: resultEvidence
        }, null, 2),
        contentType: 'application/json'
      });
    } finally {
      await db.close();
    }
  });

  test('Edit action should open the versioning form when clicking the inner icon', async ({ page }) => {
    test.setTimeout(120_000);
    const db = createDb();
    try {
      const authToken = await login(page);
      await seedSession(page, authToken);

      const houses = await db.findClearingHouses(['ACH Colombia']);
      expect(houses.length, 'Debe existir ACH Colombia en la base runtime.').toBeGreaterThanOrEqual(1);

      await page.goto(`${spaBaseUrl}/transactions/cycle-configs`);
      const filterPanel = page.locator('section.panel').first();
      const resultsPanel = page.locator('section.panel').last();

      await filterPanel.getByRole('button', { name: /ACH Colombia/i }).click();
      await filterPanel.getByRole('button', { name: 'Consultar' }).click();

      const firstRow = resultsPanel.locator('.ag-center-cols-container .ag-row').first();
      await expect(firstRow).toBeVisible();

      await firstRow.locator('[data-testid="cycle-config-action-edit"] .material-symbols-outlined').click();

      await expect(page.getByRole('heading', { name: 'Versionar configuración' })).toBeVisible();
      await expect(page.getByLabel('Nombre del ciclo')).toBeVisible();
      await expect(page.getByLabel('Nombre del ciclo')).not.toHaveValue('');
    } finally {
      await db.close();
    }
  });
});

class CycleConfigsDb {
  private readonly provider: RuntimeProvider;
  private readonly postgres: G36Postgres | null;
  private readonly sqlServer: G36SqlServer | null;

  constructor() {
    this.provider = readProvider();
    this.postgres = this.provider === 'Postgres'
      ? new G36Postgres({ requireExplicitConfig: true })
      : null;
    this.sqlServer = this.provider === 'SqlServer'
      ? new G36SqlServer()
      : null;
  }

  get providerName(): RuntimeProvider {
    return this.provider;
  }

  async close(): Promise<void> {
    await this.postgres?.close();
    this.sqlServer?.close();
  }

  async findClearingHouses(names: readonly string[]): Promise<ClearingHouseRow[]> {
    if (this.postgres) {
      return this.postgres.query<ClearingHouseRow>(
        `SELECT "Id" AS id,
                "Name" AS name
         FROM "ClearingHouses"
         WHERE "Name" = ANY($1::text[])
         ORDER BY "Id"`,
        [names]
      );
    }

    return this.sqlServer!.query<ClearingHouseRow>(
      `SELECT [Id] AS [id],
              [Name] AS [name]
       FROM [ClearingHouses]
       WHERE [Name] IN (${names.map(sqlString).join(', ')})
       ORDER BY [Id]`
    );
  }

  async listCycleConfigs(clearingHouseId: number, effectiveAt: string): Promise<CycleConfigRow[]> {
    if (this.postgres) {
      return this.postgres.query<CycleConfigRow>(
        `SELECT cfg."Id" AS id,
                cfg."ClearingHouseId" AS "clearingHouseId",
                ch."Name" AS "clearingHouseName",
                cfg."CycleName" AS "cycleName",
                cfg."StartTime"::text AS "startTime",
                cfg."EndTime"::text AS "endTime",
                cfg."CutoffTime"::text AS "cutoffTime",
                cfg."IsActive" AS "isActive",
                cfg."EffectiveFrom"::date::text AS "effectiveFrom",
                cfg."EffectiveTo"::date::text AS "effectiveTo",
                (cfg."EffectiveFrom"::date <= $2::date AND (cfg."EffectiveTo" IS NULL OR cfg."EffectiveTo"::date >= $2::date)) AS "isCurrent"
         FROM "ClearingHouseCycleConfigs" cfg
         INNER JOIN "ClearingHouses" ch ON ch."Id" = cfg."ClearingHouseId"
         WHERE cfg."ClearingHouseId" = $1
           AND cfg."EffectiveFrom"::date <= $2::date
           AND (cfg."EffectiveTo" IS NULL OR cfg."EffectiveTo"::date >= $2::date)
         ORDER BY cfg."CycleName", cfg."EffectiveFrom" DESC, cfg."Id"`,
        [clearingHouseId, effectiveAt]
      );
    }

    return this.sqlServer!.query<CycleConfigRow>(
      `SELECT cfg.[Id] AS [id],
              cfg.[ClearingHouseId] AS [clearingHouseId],
              ch.[Name] AS [clearingHouseName],
              cfg.[CycleName] AS [cycleName],
              CONVERT(varchar(16), cfg.[StartTime], 114) AS [startTime],
              CONVERT(varchar(16), cfg.[EndTime], 114) AS [endTime],
              CONVERT(varchar(16), cfg.[CutoffTime], 114) AS [cutoffTime],
              cfg.[IsActive] AS [isActive],
              CONVERT(varchar(10), cfg.[EffectiveFrom], 23) AS [effectiveFrom],
              CONVERT(varchar(10), cfg.[EffectiveTo], 23) AS [effectiveTo],
              CONVERT(bit, CASE WHEN cfg.[EffectiveFrom] <= CONVERT(date, ${sqlString(effectiveAt)}, 23)
                                AND (cfg.[EffectiveTo] IS NULL OR cfg.[EffectiveTo] >= CONVERT(date, ${sqlString(effectiveAt)}, 23))
                                THEN 1 ELSE 0 END) AS [isCurrent]
       FROM [ClearingHouseCycleConfigs] cfg
       INNER JOIN [ClearingHouses] ch ON ch.[Id] = cfg.[ClearingHouseId]
       WHERE cfg.[ClearingHouseId] = ${clearingHouseId}
         AND cfg.[EffectiveFrom] <= CONVERT(date, ${sqlString(effectiveAt)}, 23)
         AND (cfg.[EffectiveTo] IS NULL OR cfg.[EffectiveTo] >= CONVERT(date, ${sqlString(effectiveAt)}, 23))
       ORDER BY cfg.[CycleName], cfg.[EffectiveFrom] DESC, cfg.[Id]`
    );
  }
}

function createDb(): CycleConfigsDb {
  return new CycleConfigsDb();
}

async function login(page: Page): Promise<string> {
  const response = await page.request.post(`${e2eBaseUrl}/auth/login`, {
    data: {
      username,
      password
    }
  });

  expect(response.ok(), 'El login real debe devolver token para las pruebas de configuración de ciclos.').toBeTruthy();

  const payload = await response.json() as LoginResponse;
  const token = payload.data?.token;
  expect(token, 'La autenticación real debe devolver un token.').toBeTruthy();
  return token as string;
}

async function seedSession(page: Page, accessToken: string): Promise<void> {
  await page.addInitScript((token) => {
    window.sessionStorage.setItem('ach.interbank.access_token', token);
  }, accessToken);
}

function observeNetwork(page: Page): { consoleErrors: string[]; requestFailures: string[] } {
  const consoleErrors: string[] = [];
  const requestFailures: string[] = [];

  page.on('console', (message) => {
    if (message.type() === 'error') {
      const text = message.text();
      if (!/ChunkLoadError|Failed to load resource: the server responded with a status of 401/i.test(text)) {
        consoleErrors.push(text);
      }
    }
  });

  page.on('requestfailed', (request) => {
    requestFailures.push(`${request.method()} ${request.url()} ${request.failure()?.errorText ?? ''}`.trim());
  });

  return { consoleErrors, requestFailures };
}

function parseCycleConfigResponse(body: string): unknown[] {
  const parsed = JSON.parse(body) as unknown;
  return Array.isArray(parsed) ? parsed : [];
}

function bodySnippet(body: string): string {
  return body.slice(0, 240).replace(/\s+/g, ' ').trim();
}

function escapeRegex(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

function readProvider(): RuntimeProvider {
  const explicit = process.env['ACH_E2E_DB_PROVIDER'] ?? process.env['Database__Provider'];
  if (explicit) {
    return normalizeProvider(explicit);
  }

  if (hasSqlServerConfig()) {
    return 'SqlServer';
  }

  if (hasPostgresConfig()) {
    return 'Postgres';
  }

  return 'Postgres';
}

function normalizeProvider(provider: string): RuntimeProvider {
  const normalized = provider.trim().toLowerCase();
  if (normalized === 'sqlserver' || normalized === 'mssql') {
    return 'SqlServer';
  }

  if (normalized === 'postgres' || normalized === 'postgresql') {
    return 'Postgres';
  }

  throw new Error(`ACH_E2E_DB_PROVIDER invalido: ${provider}. Use SqlServer o Postgres.`);
}

function hasSqlServerConfig(): boolean {
  return Boolean(
    process.env['ACH_E2E_SQLSERVER_CONNECTION_STRING']
    || process.env['ACH_E2E_SQLSERVER_HOST']
    || process.env['ACH_E2E_SQLSERVER_PORT']
    || process.env['ACH_E2E_SQLSERVER_DATABASE']
    || process.env['ACH_E2E_SQLSERVER_USER']
    || process.env['ACH_E2E_SQLSERVER_PASSWORD']
  );
}

function hasPostgresConfig(): boolean {
  return Boolean(
    process.env['ACH_E2E_POSTGRES_CONNECTION_STRING']
    || process.env['ACH_E2E_POSTGRES_HOST']
    || process.env['POSTGRES_HOST']
    || process.env['POSTGRES_DB']
    || process.env['POSTGRES_USER']
    || process.env['POSTGRES_PASSWORD']
  );
}
