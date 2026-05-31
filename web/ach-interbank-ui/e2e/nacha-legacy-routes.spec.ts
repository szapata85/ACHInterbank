import { expect, Page, test } from '@playwright/test';

const refreshEndpoint = /\/auth\/refresh$/;
const navigationEndpoint = /\/navigation\/menu$/;
const layoutsEndpoint = /\/nacha-layouts(?:\?.*)?$/;
const definitionsEndpoint = /\/nacha-record-definitions(?:\?.*)?$/;

test.describe('NACHA legacy layouts and definitions audit', () => {
  test.beforeEach(async ({ page }) => {
    await seedAuthenticatedSession(page);
    await mockAuthRefresh(page);
  });

  test('Navigation_ShouldNotExposeLegacyNachaLayoutsAsOfficial', async ({ page }) => {
    await mockNavigationWithLegacyItems(page);

    await page.goto('/ach/nacha/operational-dashboard');

    await expect(page.getByRole('link', { name: 'Config Profiles' })).toBeVisible();
    await expect(page.getByText('Layouts NACHA legacy menu')).toHaveCount(0);
  });

  test('Navigation_ShouldNotExposeLegacyNachaDefinitionsAsOfficial', async ({ page }) => {
    await mockNavigationWithLegacyItems(page);

    await page.goto('/ach/nacha/operational-dashboard');

    await expect(page.getByRole('link', { name: 'Config Profiles' })).toBeVisible();
    await expect(page.getByText('Definiciones NACHA legacy menu')).toHaveCount(0);
  });

  test('LegacyRoute_ShouldShowDeprecatedBanner_IfRouteStillAccessible', async ({ page }) => {
    await mockNavigationWithLegacyItems(page);
    await mockLegacyEndpoints(page);

    await page.goto('/ach-cycles/nacha/layouts');

    await expect(page.getByText('LEGACY / Deprecated')).toBeVisible();
    await expect(page.getByText('El modelo oficial NACHA-M es nacha-config profiles')).toBeVisible();
    await expect(page.getByRole('button', { name: /Nuevo layout/i })).toHaveCount(0);
    await expect(page.getByRole('button', { name: /Eliminar/i })).toHaveCount(0);
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

async function mockLegacyEndpoints(page: Page): Promise<void> {
  await page.route(layoutsEndpoint, async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        { id: 1, recordCode: '6', recordType: 'Entry Detail', totalLength: 106, description: 'Legacy', fields: [] }
      ])
    });
  });
  await page.route(definitionsEndpoint, async route => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        { id: 1, recordCode: '6', sequence: 30, sourceType: 1, sourceName: 'AchTransaction', filterKey: 'EntryDetail', isEnabled: true }
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
