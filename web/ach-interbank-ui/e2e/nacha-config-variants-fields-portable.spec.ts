import { expect, test, type Page } from '@playwright/test';
import { assertNoFunctionalSpanglish } from './support/nacha-ui-language';

const spa = process.env['ACH_UI_URL'] ?? process.env['E2E_BASE_URL'] ?? 'http://localhost:743';
const api = process.env['ACH_API_URL'] ?? process.env['E2E_API_BASE_URL'] ?? 'http://localhost:843';
const username = process.env['E2E_ADMIN_USER'] ?? process.env['ACH_USER'];
const password = process.env['E2E_ADMIN_PASSWORD'] ?? process.env['ACH_PASS'];

const stableCodes = {
  profile: 'LEGACY_ACH_SALIDA_ORIGINAL_V1_0',
  record: '9',
  variant: 'LEGACY_R9_BASE',
  field: 'R9_BATCHCOUNT'
} as const;

test('variantes y campos localiza el contexto por códigos estables en cualquier runtime', async ({ page }) => {
  test.setTimeout(90_000);
  expect(username, 'E2E_ADMIN_USER o ACH_USER es obligatorio.').toBeTruthy();
  expect(password, 'E2E_ADMIN_PASSWORD o ACH_PASS es obligatorio.').toBeTruthy();

  const token = await login(page);
  const context = await resolveStableContext(page, token);
  const path = `/nacha-config-admin/variants-fields?profileId=${context.profileId}`
    + `&recordCode=${encodeURIComponent(stableCodes.record)}`
    + `&variantId=${context.variantId}&fieldId=${context.fieldId}`;

  const detailResponse = page.waitForResponse(response =>
    response.request().method() === 'GET'
    && response.url().endsWith(`/nacha-config/perfiles/${context.profileId}`)
  );
  await page.goto(`${spa}${path}`, { waitUntil: 'domcontentloaded' });
  expect((await detailResponse).status()).toBe(200);

  const root = page.getByTestId('nacha-config-variants-fields-page');
  const visiblePage = page.locator('body');
  await expect(root).toBeVisible();
  await expect(page.getByTestId('profile-selector')).toHaveValue(String(context.profileId));
  await expect(page.getByTestId('record-selector')).toHaveValue(stableCodes.record);
  await expect(page.getByTestId('variant-selector')).toHaveValue(String(context.variantId));
  await expect(root.getByRole('heading', { name: 'Cantidad de lotes' })).toBeVisible();
  await expect(root.locator('.master-item.is-selected')).toContainText('Cantidad de lotes');
  await expect(root.locator('.master-item.is-selected')).toContainText(stableCodes.field);
  await expect(root.locator('.master-item.is-selected')).toContainText('BatchCount');
  await expect(root).toContainText('Publicado');
  await expect(root).toContainText('Entidad del dominio');
  await expect(root).toContainText('Alineación a la derecha');

  await root.locator('.semantic-legend summary').click();
  await expect(root.locator('.legend-grid')).toContainText('Error bloqueante');

  await root.getByTestId('field-technical-details').locator('summary').click();
  await expect(root.getByTestId('field-technical-details')).toContainText(stableCodes.field);
  await expect(root.getByTestId('field-technical-details')).toContainText('BatchCount');

  await assertNoFunctionalSpanglish(visiblePage);

  await root.getByRole('tab', { name: 'Variante', exact: true }).click();
  await expect(root.getByTestId('variant-detail')).toContainText('Variante base del control de archivo');
  await assertNoFunctionalSpanglish(visiblePage);

  await root.getByRole('tab', { name: /^Reglas/ }).click();
  await expect(root.getByTestId('rules-detail')).toContainText('Este campo no requiere reglas adicionales');
  await assertNoFunctionalSpanglish(visiblePage);
});

async function login(page: Page): Promise<string> {
  const response = await page.request.post(`${api}/auth/login`, {
    data: { username, password }
  });
  expect(response.ok(), 'El login real debe responder 200.').toBeTruthy();
  const payload = await response.json() as { data?: { token?: string }; token?: string };
  const token = payload.data?.token ?? payload.token;
  expect(token, 'El login debe entregar un token.').toBeTruthy();

  await page.goto(`${spa}/login`, { waitUntil: 'domcontentloaded' });
  await page.locator('input[formControlName="username"]').fill(username!);
  await page.locator('input[formControlName="password"]').fill(password!);
  await page.getByRole('button', { name: 'Ingresar' }).click();
  await expect(page).not.toHaveURL(/\/login$/);
  return token!;
}

async function resolveStableContext(page: Page, token: string): Promise<StableContext> {
  const headers = { Authorization: `Bearer ${token}` };
  const profilesResponse = await page.request.get(`${api}/nacha-config/perfiles`, { headers });
  expect(profilesResponse.status()).toBe(200);
  const profilesPayload = await profilesResponse.json() as ApiEnvelope<ProfileSummary[]>;
  const profiles = unwrap(profilesPayload);
  const profile = profiles.find(item => item.profileCode === stableCodes.profile);
  expect(profile, `Debe existir el perfil bootstrap ${stableCodes.profile}.`).toBeTruthy();

  const detailResponse = await page.request.get(`${api}/nacha-config/perfiles/${profile!.id}`, { headers });
  expect(detailResponse.status()).toBe(200);
  const detailPayload = await detailResponse.json() as ApiEnvelope<ProfileDetail>;
  const detail = unwrap(detailPayload);
  expect(detail.records.some(item => item.recordCode === stableCodes.record)).toBe(true);
  const variant = detail.variantes.find(item =>
    item.recordCode === stableCodes.record && item.variantCode === stableCodes.variant
  );
  expect(variant, `Debe existir la variante bootstrap ${stableCodes.variant}.`).toBeTruthy();
  const field = variant!.fields.find(item => item.fieldCode === stableCodes.field);
  expect(field, `Debe existir el campo bootstrap ${stableCodes.field}.`).toBeTruthy();

  return {
    profileId: profile!.id,
    variantId: variant!.id,
    fieldId: field!.id
  };
}

function unwrap<T>(payload: ApiEnvelope<T>): T {
  return payload && typeof payload === 'object' && 'data' in payload
    ? (payload as { data: T }).data
    : payload as T;
}

type ApiEnvelope<T> = T | { data: T };

interface ProfileSummary {
  id: number;
  profileCode: string;
}

interface ProfileDetail {
  records: Array<{ recordCode: string }>;
  variantes: Array<{
    id: number;
    recordCode: string;
    variantCode: string;
    fields: Array<{ id: number; fieldCode: string }>;
  }>;
}

interface StableContext {
  profileId: number;
  variantId: number;
  fieldId: number;
}
