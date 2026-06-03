import { expect, Page, test } from '@playwright/test';

const refreshEndpoint = /\/auth\/refresh$/;
const navigationEndpoint = /\/navigation\/menu$/;
const layoutsEndpoint = /\/nacha-layouts(?:\?.*)?$/;
const definitionsEndpoint = /\/nacha-record-definitions(?:\?.*)?$/;
const configProfilesEndpoint = /\/api\/ach\/nacha\/config-profiles$/;

test.describe('NACHA legacy layouts and definitions audit', () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthenticatedSession(page);
    await mockAuthRefresh(page);
  });

  test('Navigation_ShouldNotExposeLegacyNachaLayoutsAsOfficial', async ({ page }) => {
    await mockNavigationWithLegacyItems(page);

    await page.goto('/ach/nacha/operational-dashboard');

    await expect(page.getByRole('link', { name: 'Perfiles oficiales' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Records oficiales' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Variants y Fields' })).toBeVisible();
    await expect(page.getByText('Layouts NACHA legacy menu')).toHaveCount(0);
  });

  test('Navigation_ShouldNotExposeLegacyNachaDefinitionsAsOfficial', async ({ page }) => {
    await mockNavigationWithLegacyItems(page);

    await page.goto('/ach/nacha/operational-dashboard');

    await expect(page.getByRole('link', { name: 'Perfiles oficiales' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Records oficiales' })).toBeVisible();
    await expect(page.getByRole('link', { name: 'Variants y Fields' })).toBeVisible();
    await expect(page.getByText('Definiciones NACHA legacy menu')).toHaveCount(0);
  });

  test('CompatibilityRoutes_ShouldUseOfficialProfilesAndAvoidLegacyEndpoints', async ({ page }) => {
    await mockNavigationWithLegacyItems(page);
    await mockOfficialConfigProfiles(page);
    const legacyRequests: string[] = [];
    page.on('request', request => {
      if (layoutsEndpoint.test(request.url()) || definitionsEndpoint.test(request.url())) {
        legacyRequests.push(request.url());
      }
    });

    await page.goto('/ach-cycles/nacha/layouts');

    await expect(page.getByTestId('nacha-layouts-page').getByRole('heading', { name: 'NACHA Config - Variants y Fields' })).toBeVisible();
    await expect(page.getByText('La fuente oficial NACHA-M es nacha-config profiles')).toBeVisible();
    await expect(page.getByText('CENIT-OUT-220')).toBeVisible();
    await expect(page.getByRole('button', { name: /Crear|Editar|Guardar|Eliminar/i })).toHaveCount(0);

    await page.goto('/ach-cycles/nacha/definitions');

    await expect(page.getByTestId('nacha-definitions-page').getByRole('heading', { name: 'NACHA Config - Records' })).toBeVisible();
    await expect(page.getByText('La fuente oficial NACHA-M es nacha-config profiles')).toBeVisible();
    await expect(page.getByText('1, 5, 6, 7, 8, 9')).toBeVisible();
    await expect(page.getByRole('button', { name: /Crear|Editar|Guardar|Eliminar/i })).toHaveCount(0);
    await expect(page.getByRole('button', { name: /Eliminar/i })).toHaveCount(0);
    expect(legacyRequests).toEqual([]);
  });
});

async function mockNavigationWithLegacyItems(page: Page): Promise<void> {
  await page.route(navigationEndpoint, async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        { id: 1, label: 'Layouts NACHA legacy menu', route: '/ach-cycles/nacha/layouts' },
        { id: 2, label: 'Definiciones NACHA legacy menu', route: '/ach-cycles/nacha/definitions' },
        { id: 3, label: 'Config Profiles', route: '/nacha-config-admin/perfiles' }
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
    unique_name: 'uat.legacy',
    name: 'Usuario UAT Legacy',
    uid: 'uat-legacy',
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
    unique_name: 'uat.legacy',
    name: 'Usuario UAT Legacy',
    uid: 'uat-legacy',
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
          username: 'uat.legacy',
          fullName: 'Usuario UAT Legacy',
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
