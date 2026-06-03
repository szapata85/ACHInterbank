import { expect, Page, test } from '@playwright/test';

const refreshEndpoint = /\/auth\/refresh$/;
const navigationEndpoint = /\/navigation\/menu$/;
const layoutsEndpoint = /\/nacha-layouts(?:\?.*)?$/;
const definitionsEndpoint = /\/nacha-record-definitions(?:\?.*)?$/;
const configProfilesEndpoint = /\/api\/ach\/nacha\/config-profiles$/;

test.describe('NACHA Config official routes', () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthenticatedSession(page);
    await mockAuthRefresh(page);
    await mockNavigation(page);
  });

  test('Navigation_ShouldExposeOfficialNachaConfigMenuOnly', async ({ page }) => {
    await page.goto('/ach/nacha/operational-dashboard');

    await expect(page.getByRole('link', { name: 'Perfiles oficiales' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Records oficiales' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Variants y Fields' })).toBeVisible();
    await expect(page.getByText(/legacy/i)).toHaveCount(0);
    await expect(page.getByRole('link', { name: 'Layouts NACHA' })).toHaveCount(0);
    await expect(page.getByRole('link', { name: 'Definiciones NACHA' })).toHaveCount(0);
  });

  test('OfficialRoutes_ShouldUseConfigProfilesAndAvoidLegacyEndpoints', async ({ page }) => {
    await mockOfficialConfigProfiles(page);
    const legacyRequests: string[] = [];
    page.on('request', request => {
      if (layoutsEndpoint.test(request.url()) || definitionsEndpoint.test(request.url())) {
        legacyRequests.push(request.url());
      }
    });

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
    expect(legacyRequests).toEqual([]);
  });

  test('LegacyRoutes_ShouldEndInNotFound', async ({ page }) => {
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

async function seedAuthenticatedSession(page: Page): Promise<void> {
  const token = createUnsignedJwt({
    unique_name: 'uat.official',
    name: 'Usuario UAT Oficial',
    uid: 'uat-official',
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
