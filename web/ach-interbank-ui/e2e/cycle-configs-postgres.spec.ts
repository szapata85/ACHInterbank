import { expect, Page, test, TestInfo } from '@playwright/test';
import { Pool } from 'pg';

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

const spaBaseUrl = (process.env['E2E_BASE_URL'] ?? process.env['ACH_UI_URL'] ?? 'http://localhost:743').replace(/\/+$/, '');
const username = process.env['E2E_ADMIN_USER'] ?? process.env['ACH_USER'] ?? 'admin';
const password = process.env['E2E_ADMIN_PASSWORD'] ?? process.env['ACH_PASS'] ?? 'Admin123!';
const effectiveAt = new Date().toISOString().slice(0, 10);

test.describe.configure({ mode: 'serial' });

test.describe('Cycle configs SPA + PostgreSQL', () => {
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
      expect(houses.length, 'PostgreSQL debe contener ACH Colombia y CENIT.').toBeGreaterThanOrEqual(2);

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

  test('Reading should match PostgreSQL for ACH Colombia and CENIT', async ({ page }, testInfo) => {
    test.setTimeout(180_000);
    const db = createDb();
    try {
      const authToken = await login(page);
      await seedSession(page, authToken);

      const expectedHouses = await db.findClearingHouses(['ACH Colombia', 'CENIT']);
      expect(expectedHouses.length, 'Deben existir ACH Colombia y CENIT en PostgreSQL.').toBeGreaterThanOrEqual(2);

      await page.goto(`${spaBaseUrl}/transactions/cycle-configs`);
      const filterPanel = page.locator('section.panel').first();
      const resultsPanel = page.locator('section.panel').last();

      await expect(filterPanel.getByRole('button', { name: /ACH Colombia/i })).toBeVisible();
      await expect(filterPanel.getByRole('button', { name: /CENIT/i })).toBeVisible();

      const resultEvidence: Array<Record<string, unknown>> = [];
      for (const houseName of ['ACH Colombia', 'CENIT']) {
        const house = expectedHouses.find((item) => item.name === houseName);
        expect(house, `La cámara ${houseName} debe existir en PostgreSQL.`).toBeTruthy();

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
        expect(apiRows.length, `${houseName} debe devolver la misma cantidad de filas que PostgreSQL.`).toBe(dbRows.length);
        expect(uiRows.length, `${houseName} debe mostrar la misma cantidad de filas que la API/DB.`).toBe(dbRows.length);
        for (const row of dbRows) {
          expect(uiRows.join('\n')).toContain(row.cycleName);
        }

        resultEvidence.push({
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
      expect(houses.length, 'Debe existir ACH Colombia en PostgreSQL.').toBeGreaterThanOrEqual(1);

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
  private readonly pool: Pool;

  constructor() {
    this.pool = new Pool({
      host: process.env['E2E_DB_HOST'] ?? process.env['POSTGRES_HOST'] ?? '127.0.0.1',
      port: Number(process.env['E2E_DB_PORT'] ?? process.env['POSTGRES_PORT'] ?? 5432),
      database: process.env['E2E_DB_NAME'] ?? process.env['POSTGRES_DB'] ?? 'ACHInterbank',
      user: process.env['E2E_DB_USER'] ?? process.env['POSTGRES_USER'] ?? 'example_user',
      password: process.env['E2E_DB_PASSWORD'] ?? process.env['POSTGRES_PASSWORD'] ?? 'example_password_change_me',
      max: 2,
      connectionTimeoutMillis: 10_000,
      idleTimeoutMillis: 10_000
    });
  }

  async close(): Promise<void> {
    await this.pool.end();
  }

  async findClearingHouses(names: readonly string[]): Promise<ClearingHouseRow[]> {
    const result = await this.pool.query<ClearingHouseRow>(
      `SELECT "Id" AS id,
              "Name" AS name
       FROM "ClearingHouses"
       WHERE "Name" = ANY($1::text[])
       ORDER BY "Id"`,
      [names]
    );
    return result.rows;
  }

  async listCycleConfigs(clearingHouseId: number, effectiveAt: string): Promise<CycleConfigRow[]> {
    const result = await this.pool.query<CycleConfigRow>(
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
    return result.rows;
  }
}

function createDb(): CycleConfigsDb {
  return new CycleConfigsDb();
}

async function login(page: Page): Promise<string> {
  const response = await page.request.post(`${spaBaseUrl}/auth/login`, {
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
