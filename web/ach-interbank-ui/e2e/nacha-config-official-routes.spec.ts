import { expect, Page, test } from '@playwright/test';

const refreshEndpoint = /\/auth\/refresh$/;
const navigationEndpoint = /\/navigation\/menu$/;
const catalogsEndpoint = /\/nacha-config\/catalogos-filtro$/;
const dashboardEndpoint = /\/api\/ach\/nacha\/config-profiles\/dashboard$/;
const profilesReadOnlyEndpoint = /\/api\/ach\/nacha\/config-profiles$/;
const layoutsEndpoint = /\/nacha-layouts(?:\?.*)?$/;
const definitionsEndpoint = /\/nacha-record-definitions(?:\?.*)?$/;
const configProfilesEndpoint = /\/api\/ach\/nacha\/config-profiles$/;
const configProfileDetailEndpoint = /\/nacha-config\/perfiles\/10$/;

test.describe('NACHA Config official routes', () => {
  test.beforeEach(async ({ page }) => {
    await mockNachaConfigBackend(page);
    await mockAuthRefresh(page);
    await mockNavigation(page);
    await authenticate(page);
  });

  test('Navigation_ShouldExposeOfficialNachaConfigMenuOnly', async ({ page }) => {
    await page.goto('/nacha-config-admin/perfiles');

    await expect(page.getByRole('link', { name: 'Perfiles oficiales' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Records oficiales' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Variants y Fields' })).toBeVisible();
    await expect(page.getByRole('link', { name: /legacy/i })).toHaveCount(0);
    await expect(page.getByRole('link', { name: 'Layouts NACHA' })).toHaveCount(0);
    await expect(page.getByRole('link', { name: 'Definiciones NACHA' })).toHaveCount(0);
  });

  test('OfficialRoutes_ShouldUseConfigProfilesAndAvoidLegacyEndpoints', async ({ page }) => {
    const legacyRequests: string[] = [];
    const htmlJsResponses: string[] = [];
    const chunkRequestFailures: string[] = [];
    const consoleErrors: string[] = [];
    page.on('request', request => {
      if (layoutsEndpoint.test(request.url()) || definitionsEndpoint.test(request.url())) {
        legacyRequests.push(request.url());
      }
    });
    page.on('requestfailed', request => {
      if (request.url().endsWith('.js')) {
        chunkRequestFailures.push(`${request.method()} ${request.url()} ${request.failure()?.errorText ?? ''}`.trim());
      }
    });
    page.on('console', message => {
      if (message.type() === 'error') {
        consoleErrors.push(message.text());
      }
    });
    page.on('response', async response => {
      const url = response.url();
      if (!url.endsWith('.js')) {
        return;
      }

      const contentType = response.headers()['content-type'] ?? '';
      if (contentType.includes('text/html')) {
        htmlJsResponses.push(`${response.status()} ${url} ${contentType}`);
      }
    });

    await page.goto('/nacha-config-admin/perfiles');

    await expect(page.getByTestId('nacha-config-profiles-page').getByRole('heading', { name: 'Config Profiles NACHA-M' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Crear borrador' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Validar' })).toBeVisible();

    await page.goto('/nacha-config-admin/variants-fields');

    await expect(page.getByTestId('nacha-config-variants-fields-page').getByRole('heading', { name: 'NACHA Config - Variants y Fields' })).toBeVisible();
    await expect(page.getByText('nacha-config profiles')).toBeVisible();
    await expect(page.getByText('CENIT-OUT-220')).toBeVisible();
    await expect(page.getByRole('button', { name: /Crear|Editar|Guardar|Eliminar/i })).toHaveCount(0);

    await page.goto('/nacha-config-admin/records');

    await expect(page.getByTestId('nacha-config-records-page').getByRole('heading', { name: 'NACHA Config - Records' })).toBeVisible();
    await expect(page.getByText('nacha-config profiles')).toBeVisible();
    await expect(page.getByText('1, 5, 6, 7, 8, 9')).toBeVisible();
    await expect(page.getByRole('button', { name: /Crear|Editar|Guardar|Eliminar/i })).toHaveCount(0);

    await page.goto('/nacha-config-admin/perfiles/10');

    await expect(page.getByTestId('nacha-config-profile-workspace-page').getByRole('heading', { name: 'Perfil CENIT-OUT-220' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Clonar como borrador' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Ir a records oficiales' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Ir a variants y fields' })).toBeVisible();
    expect(legacyRequests).toEqual([]);
    expect(htmlJsResponses).toEqual([]);
    expect(chunkRequestFailures).toEqual([]);
    expect(consoleErrors).toEqual([]);
  });

  test('LegacyRoutes_ShouldEndInNotFound', async ({ page }) => {
    await page.goto('/nacha-config-admin/perfiles');
    await page.goto('/ach-cycles/nacha/layouts');
    await expect(page).toHaveURL(/\/not-found$/);
    await expect(page.getByText('404', { exact: true })).toBeVisible();

    await page.goto('/ach-cycles/nacha/definitions');
    await expect(page).toHaveURL(/\/not-found$/);
    await expect(page.getByText('404', { exact: true })).toBeVisible();
  });
});

async function mockNavigation(page: Page): Promise<void> {
  await page.route(navigationEndpoint, async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        {
          id: 20,
          label: 'NACHA-M ConfiguraciÃ³n',
          route: '/nacha-config-admin/perfiles',
          children: [
            { id: 25, label: 'Perfiles oficiales', route: '/nacha-config-admin/perfiles' },
            { id: 2802, label: 'Records oficiales', route: '/nacha-config-admin/records' },
            { id: 2803, label: 'Variants y Fields', route: '/nacha-config-admin/variants-fields' }
          ]
        }
      ])
    });
  });
}

async function mockOfficialConfigProfiles(page: Page): Promise<void> {
  await page.route(configProfilesEndpoint, async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        {
          profileId: 10,
          profileCode: 'CENIT-OUT-220',
          profileName: 'CENIT salida 220',
          clearingHouseCode: 'CENIT',
          flowType: 'Outgoing',
          status: 'Published',
          version: '1.0',
          isPublished: true,
          isCurrent: true,
          effectiveFrom: '2026-01-01T00:00:00Z',
          effectiveTo: null,
          layoutVariantCount: 6,
          fieldCount: 42,
          recordTypes: ['1', '5', '6', '7', '8', '9'],
          isOfficialModel: true,
          legacyDeprecated: true
        }
      ])
    });
  });
}

async function mockNachaConfigBackend(page: Page): Promise<void> {
  await page.route(/https?:\/\/localhost:7269\/.*/i, async route => {
    const url = new URL(route.request().url());
    const path = url.pathname;
    const method = route.request().method().toUpperCase();

    if (method === 'GET' && path === '/nacha-config/catalogos-filtro') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          estados: [
            { code: 'BORRADOR', labelEs: 'Borrador' },
            { code: 'PUBLICADO', labelEs: 'Publicado' },
            { code: 'INACTIVO', labelEs: 'Inactivo' },
            { code: 'ARCHIVADO', labelEs: 'Archivado' }
          ],
          camaras: [
            { code: 'ACH', labelEs: 'ACH Colombia' },
            { code: 'CENIT', labelEs: 'CENIT' }
          ],
          flujos: [
            { code: 'ORIGINAL', labelEs: 'Original' },
            { code: 'RETORNO', labelEs: 'Retorno' }
          ],
          direcciones: [
            { code: 'SALIDA', labelEs: 'Salida' },
            { code: 'ENTRADA', labelEs: 'Entrada' }
          ],
          servicios: [
            { code: 'PPD', labelEs: 'PPD' },
            { code: 'CCD', labelEs: 'CCD' }
          ]
        })
      });
      return;
    }

    if (method === 'GET' && path === '/api/ach/nacha/config-profiles/dashboard') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          productiveStatus: 'NO-GO',
          isOfficialModel: true,
          legacyDeprecated: false,
          profileCount: 1,
          publishedProfileCount: 1,
          currentProfileCount: 1,
          layoutVariantCount: 6,
          fieldCount: 42,
          clearingHouses: ['ACH', 'CENIT'],
          recordTypes: ['1', '5', '6', '7', '8', '9'],
          warnings: []
        })
      });
      return;
    }

    if (method === 'GET' && path === '/api/ach/nacha/config-profiles') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify([
          {
            profileId: 10,
            profileCode: 'CENIT-OUT-220',
            profileName: 'CENIT salida 220',
            clearingHouseCode: 'CENIT',
            flowType: 'Outgoing',
            status: 'Published',
            version: '1.0',
            isPublished: true,
            isCurrent: true,
            effectiveFrom: '2026-01-01T00:00:00Z',
            effectiveTo: null,
            layoutVariantCount: 6,
            fieldCount: 42,
            recordTypes: ['1', '5', '6', '7', '8', '9'],
            isOfficialModel: true,
            legacyDeprecated: false
          }
        ])
      });
      return;
    }

    if (method === 'GET' && path === '/nacha-config/perfiles/10') {
      await route.fulfill({
        status: 200,
        contentType: 'application/json',
        body: JSON.stringify({
          id: 10,
          profileCode: 'CENIT-OUT-220',
          nombreEs: 'CENIT salida 220',
          descripcion: 'Perfil oficial administrable',
          estado: 'BORRADOR',
          versionMajor: 1,
          versionMinor: 0,
          contextPriority: 100,
          effectiveFrom: '2026-01-01T00:00:00Z',
          effectiveTo: null,
          rowVersion: 'cm93',
          records: [
            { id: 1, recordCode: '1', sequence: 1, isEnabled: true, minOccurs: 1, maxOccurs: 1, sourceStrategy: 'STATIC' }
          ],
          variantes: [
            {
              id: 2,
              recordCode: '1',
              variantCode: 'CENIT_R1_BASE_V1',
              nombreEs: 'Base',
              priority: 1,
              isDefaultForRecord: true,
              totalLength: 94,
              fields: []
            }
          ]
        })
      });
      return;
    }

    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: '{}'
    });
  });
}

async function mockNachaConfigCatalogs(page: Page): Promise<void> {
  await page.route(catalogsEndpoint, async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        estados: [
          { code: 'BORRADOR', labelEs: 'Borrador' },
          { code: 'PUBLICADO', labelEs: 'Publicado' },
          { code: 'INACTIVO', labelEs: 'Inactivo' },
          { code: 'ARCHIVADO', labelEs: 'Archivado' }
        ],
        camaras: [
          { code: 'ACH', labelEs: 'ACH Colombia' },
          { code: 'CENIT', labelEs: 'CENIT' }
        ],
        flujos: [
          { code: 'ORIGINAL', labelEs: 'Original' },
          { code: 'RETORNO', labelEs: 'Retorno' }
        ],
        direcciones: [
          { code: 'SALIDA', labelEs: 'Salida' },
          { code: 'ENTRADA', labelEs: 'Entrada' }
        ],
        servicios: [
          { code: 'PPD', labelEs: 'PPD' },
          { code: 'CCD', labelEs: 'CCD' }
        ]
      })
    });
  });
}

async function mockNachaConfigDashboard(page: Page): Promise<void> {
  await page.route(dashboardEndpoint, async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        productiveStatus: 'NO-GO',
        isOfficialModel: true,
        legacyDeprecated: false,
        profileCount: 1,
        publishedProfileCount: 1,
        currentProfileCount: 1,
        layoutVariantCount: 6,
        fieldCount: 42,
        clearingHouses: ['ACH', 'CENIT'],
        recordTypes: ['1', '5', '6', '7', '8', '9'],
        warnings: []
      })
    });
  });
}

async function mockNachaConfigProfilesReadOnly(page: Page): Promise<void> {
  await page.route(profilesReadOnlyEndpoint, async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        {
          profileId: 10,
          profileCode: 'CENIT-OUT-220',
          profileName: 'CENIT salida 220',
          clearingHouseCode: 'CENIT',
          flowType: 'Outgoing',
          status: 'Published',
          version: '1.0',
          isPublished: true,
          isCurrent: true,
          effectiveFrom: '2026-01-01T00:00:00Z',
          effectiveTo: null,
          layoutVariantCount: 6,
          fieldCount: 42,
          recordTypes: ['1', '5', '6', '7', '8', '9'],
          isOfficialModel: true,
          legacyDeprecated: false
        }
      ])
    });
  });
}

async function mockOfficialProfileDetail(page: Page): Promise<void> {
  await page.route(configProfileDetailEndpoint, async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        id: 10,
        profileCode: 'CENIT-OUT-220',
        nombreEs: 'CENIT salida 220',
        descripcion: 'Perfil oficial administrable',
        estado: 'BORRADOR',
        versionMajor: 1,
        versionMinor: 0,
        contextPriority: 100,
        effectiveFrom: '2026-01-01T00:00:00Z',
        effectiveTo: null,
        rowVersion: 'cm93',
        records: [
          { id: 1, recordCode: '1', sequence: 1, isEnabled: true, minOccurs: 1, maxOccurs: 1, sourceStrategy: 'STATIC' }
        ],
        variantes: [
          {
            id: 2,
            recordCode: '1',
            variantCode: 'CENIT_R1_BASE_V1',
            nombreEs: 'Base',
            priority: 1,
            isDefaultForRecord: true,
            totalLength: 94,
            fields: []
          }
        ]
      })
    });
  });
}

async function mockAuthRefresh(page: Page): Promise<void> {
  const token = createUnsignedJwt({
    unique_name: 'uat.official',
    name: 'Usuario UAT Oficial',
    uid: 'uat-official',
    role: ['Admin', 'ACH.Operator'],
    permission: ['CanReadAch', 'CanManageAch'],
    exp: Math.floor(Date.now() / 1000) + 3600,
    iat: Math.floor(Date.now() / 1000)
  });

  await page.route(refreshEndpoint, async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        sucess: true,
        data: {
          token,
          username: 'uat.official',
          fullName: 'Usuario UAT Oficial',
          roles: ['Admin', 'ACH.Operator'],
          permissions: ['CanReadAch', 'CanManageAch']
        }
      })
    });
  });
}

async function authenticate(page: Page): Promise<void> {
  const user = process.env['ACH_USER'] ?? 'admin';
  const pass = process.env['ACH_PASS'] ?? 'Admin123!';

  const apiBaseUrl = process.env['ACH_API_URL'] ?? 'http://localhost:843';
  const response = await page.request.post(`${apiBaseUrl.replace(/\/+$/, '')}/auth/login`, {
    data: { username: user, password: pass }
  });

  expect(response.ok()).toBeTruthy();

  const payload = await response.json() as {
    data?: {
      token?: string;
    };
  };

  const token = payload.data?.token;
  expect(token).toBeTruthy();

  await page.addInitScript((accessToken) => {
    window.sessionStorage.setItem('ach.interbank.access_token', accessToken);
  }, token as string);
}

function createUnsignedJwt(payload: Record<string, unknown>): string {
  return `${base64Url({ alg: 'none', typ: 'JWT' })}.${base64Url(payload)}.e2e`;
}

function base64Url(value: Record<string, unknown>): string {
  return Buffer.from(JSON.stringify(value))
    .toString('base64')
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=+$/g, '');
}
