import { expect, Page, test } from '@playwright/test';

const configProfilesPagePath = '/ach/nacha/config-profiles';
const refreshEndpoint = /\/auth\/refresh$/;
const navigationEndpoint = /\/navigation\/menu$/;
const dashboardEndpoint = /\/api\/ach\/nacha\/config-profiles\/dashboard$/;
const profilesEndpoint = /\/api\/ach\/nacha\/config-profiles$/;
const detailEndpoint = /\/api\/ach\/nacha\/config-profiles\/1$/;
const filterCatalogsEndpoint = /\/nacha-config\/catalogos-filtro$/;
const legacyEndpoint = /\/(ach-cycles\/nacha\/layouts|ach-cycles\/nacha\/definitions|nacha-layouts|nacha-record-definitions)(?:\?.*)?$/;
const mutatingConfigProfiles = /\/api\/ach\/nacha\/config-profiles/;
const hashExportPattern = /\/NachaExport\/[a-f0-9]{32,64}$/i;

test.describe('NACHA config profiles official read-only page', () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthenticatedSession(page);
    await mockAuthRefresh(page);
    await mockNavigation(page);
    await mockReadOnlyEndpoints(page);
  });

  test('ConfigProfiles_ShouldLoadOfficialPage', async ({ page }) => {
    await page.goto(configProfilesPagePath);

    await expect(page).toHaveURL(/\/nacha-config-admin\/perfiles$/);
    await expect(page.getByText('Config Profiles NACHA-M')).toBeVisible();
  });

  test('ConfigProfiles_ShouldShowOfficialModelBanner', async ({ page }) => {
    await page.goto(configProfilesPagePath);

    await expect(page.getByText('Modelo oficial NACHA-M: nacha-config profiles.')).toBeVisible();
  });

  test('ConfigProfiles_ShouldShowNoGoBanner', async ({ page }) => {
    await page.goto(configProfilesPagePath);

    await expect(page.getByText(/Productivo NO-GO/)).toBeVisible();
  });

  test('ConfigProfiles_ShouldNotRenderMutationButtons', async ({ page }) => {
    await page.goto(configProfilesPagePath);

    await expect(page.getByRole('button', { name: /Crear borrador|Publicar|Guardar|Eliminar|Archivar|Inactivar/i })).toHaveCount(0);
  });

  test('ConfigProfiles_ShouldNotCallLegacyLayoutsOrDefinitions', async ({ page }) => {
    let legacyCalled = false;
    await page.route(legacyEndpoint, async route => {
      legacyCalled = true;
      await route.abort();
    });

    await page.goto(configProfilesPagePath);
    await expect(page.getByText('OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0')).toBeVisible();

    expect(legacyCalled).toBe(false);
  });

  test('ConfigProfiles_ShouldNotSendPostPutDeletePatch', async ({ page }) => {
    const mutationRequests: string[] = [];
    page.on('request', request => {
      if (mutatingConfigProfiles.test(request.url()) && ['POST', 'PUT', 'PATCH', 'DELETE'].includes(request.method())) {
        mutationRequests.push(`${request.method()} ${request.url()}`);
      }
    });

    await page.goto(configProfilesPagePath);
    await expect(page.getByText('OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0')).toBeVisible();

    expect(mutationRequests).toEqual([]);
  });

  test('ExportFlow_ShouldStillNotRequestNachaExportWithHash', async ({ page }) => {
    const exportRequests: string[] = [];
    page.on('request', request => {
      if (request.url().includes('/NachaExport/')) {
        exportRequests.push(request.url());
      }
    });

    await page.goto(configProfilesPagePath);
    await expect(page.getByText('OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0')).toBeVisible();

    expect(exportRequests.some(url => hashExportPattern.test(url))).toBe(false);
  });
});

async function mockNavigation(page: Page): Promise<void> {
  await page.route(navigationEndpoint, async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([{ id: 3, label: 'Config Profiles', route: '/nacha-config-admin/perfiles' }])
    });
  });
}

async function mockReadOnlyEndpoints(page: Page): Promise<void> {
  await page.route(dashboardEndpoint, async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        productiveStatus: 'NO-GO',
        isOfficialModel: true,
        legacyDeprecated: true,
        profileCount: 1,
        publishedProfileCount: 1,
        currentProfileCount: 1,
        layoutVariantCount: 6,
        fieldCount: 20,
        clearingHouses: ['ACH'],
        recordTypes: ['1', '5', '6', '7', '8', '9'],
        warnings: []
      })
    });
  });

  await page.route(profilesEndpoint, async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([{
        profileId: 1,
        profileCode: 'OFFICIAL_ACH_SALIDA_ORIGINAL_V1_0',
        profileName: 'Perfil oficial ACH Colombia salida original',
        clearingHouseCode: 'ACH',
        flowType: 'ORIGINAL',
        status: 'PUBLICADO',
        version: 'v1.0',
        isPublished: true,
        isCurrent: true,
        effectiveFrom: '2026-01-01T00:00:00Z',
        effectiveTo: null,
        layoutVariantCount: 6,
        fieldCount: 20,
        recordTypes: ['1', '5', '6', '7', '8', '9'],
        isOfficialModel: true,
        legacyDeprecated: true
      }])
    });
  });

  await page.route(detailEndpoint, async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ profileId: 1, variants: [], fields: [] })
    });
  });

  await page.route(filterCatalogsEndpoint, async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        estados: [{ code: 'PUBLICADO', labelEs: 'PUBLICADO' }],
        camaras: [{ code: 'ACH', labelEs: 'ACH Colombia' }, { code: 'CENIT', labelEs: 'CENIT' }],
        flujos: [{ code: 'ORIGINAL', labelEs: 'Original' }],
        direcciones: [{ code: 'SALIDA', labelEs: 'Salida' }],
        servicios: []
      })
    });
  });
}

async function seedAuthenticatedSession(page: Page): Promise<void> {
  const token = createUnsignedJwt({
    unique_name: 'uat.config',
    name: 'Usuario UAT Config',
    uid: 'uat-config',
    role: ['Admin', 'ACH.Operator'],
    permission: ['CanReadAch', 'CanManageAch'],
    exp: Math.floor(Date.now() / 1000) + 3600,
    iat: Math.floor(Date.now() / 1000)
  });

  await page.addInitScript((accessToken) => {
    window.sessionStorage.setItem('ach.interbank.access_token', accessToken);
  }, token);
}

async function mockAuthRefresh(page: Page): Promise<void> {
  const token = createUnsignedJwt({
    unique_name: 'uat.config',
    name: 'Usuario UAT Config',
    uid: 'uat-config',
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
          username: 'uat.config',
          fullName: 'Usuario UAT Config',
          roles: ['Admin', 'ACH.Operator'],
          permissions: ['CanReadAch', 'CanManageAch']
        }
      })
    });
  });
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
