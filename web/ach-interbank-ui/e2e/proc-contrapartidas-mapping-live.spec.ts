import { expect, Page, test } from '@playwright/test';

type AuthLoginResponse = { data?: { token?: string } };
type IntegrationMethod = { id: number; code: string; isActive: boolean };
type MappingRule = {
  parameterId: number;
  sourceKind: string | number;
  sourceFieldPath: string;
  fixedValue?: string | null;
  defaultValue?: string | null;
  enabled: boolean;
};
type MappingSet = {
  id: string;
  methodId: number;
  methodCode: string;
  status: string | number;
  isActive: boolean;
  rules: MappingRule[];
};
type MethodParameter = { id: number; parameterPath: string };

type ExpectedRule = {
  parameterPath: string;
  sourceKind: 'Transaction' | 'Constant';
  sourceFieldPath?: string;
  fixedValue?: string;
  defaultValue?: string;
};

const ui = (process.env['ACH_UI_URL'] ?? 'http://localhost:743').replace(/\/+$/, '');
const api = (process.env['ACH_API_URL'] ?? 'http://localhost:843').replace(/\/+$/, '');
const username = process.env['ACH_USER'] ?? '';
const password = process.env['ACH_PASS'] ?? '';
const configure = process.env['PROC_CONTRA_MAPPING_CONFIGURE'] === 'true';
const existingDraftId = process.env['PROC_CONTRA_MAPPING_DRAFT_ID'] ?? '';
const methodCode = 'WSCFAACH.Proc_Contrapartidas';

const expectedRules: ExpectedRule[] = [
  { parameterPath: 'OFCTA', sourceKind: 'Transaction', sourceFieldPath: 'transaction.sourceAccountNumber' },
  { parameterPath: 'OFDD', sourceKind: 'Constant', fixedValue: 'TRANSFER  ', defaultValue: 'TRANSFER  ' },
  { parameterPath: 'OFMONDEB', sourceKind: 'Transaction', sourceFieldPath: 'transaction.amount', defaultValue: '0' },
  { parameterPath: 'OFMONCRE', sourceKind: 'Constant', fixedValue: '0', defaultValue: '0' },
  { parameterPath: 'OFST', sourceKind: 'Constant', fixedValue: 'OO', defaultValue: 'OO' },
  { parameterPath: 'OFIDTX', sourceKind: 'Constant', fixedValue: '0', defaultValue: '0' },
  { parameterPath: 'OFIDREVER', sourceKind: 'Constant', fixedValue: '0', defaultValue: '0' },
  { parameterPath: 'OFIDEBAPLI', sourceKind: 'Constant', fixedValue: '1', defaultValue: '1' },
  { parameterPath: 'OFLIBRE', sourceKind: 'Transaction', sourceFieldPath: 'transaction.reference' },
  { parameterPath: 'OFLIBRE1', sourceKind: 'Transaction', sourceFieldPath: 'transaction.id' }
];

test.skip(!username || !password, 'ACH_USER y ACH_PASS son requeridos para validar el mapping real.');

test('configura y verifica la semántica débito de Proc_Contrapartidas desde el editor SPA', async ({ page }) => {
  test.setTimeout(300_000);
  const token = await authenticate(page);
  const headers = { Authorization: `Bearer ${token}` };
  const method = await getMethod(page, headers);
  let published = await getPublished(page, headers, method.id);

  if (configure) {
    let cloned: MappingSet;
    if (existingDraftId) {
      const draftResponse = await page.request.get(`${api}/api/integrations/mappingsets/${existingDraftId}`, { headers });
      expect(draftResponse.ok()).toBeTruthy();
      cloned = await draftResponse.json() as MappingSet;
      await page.goto(`${ui}/integraciones/mappings/${encodeURIComponent(methodCode)}/${cloned.id}`);
    } else {
      await page.goto(`${ui}/integraciones/mappings/${encodeURIComponent(methodCode)}/${published.id}`);
      await expect(page.getByRole('heading', { name: publishedNamePattern() })).toBeVisible();
      const cloneResponse = page.waitForResponse((response) =>
        response.request().method() === 'POST'
        && new URL(response.url()).pathname.toLowerCase().endsWith(`/api/integrations/mappingsets/${published.id.toLowerCase()}/clone`)
      );
      await page.getByRole('button', { name: 'Clonar', exact: true }).click();
      cloned = await (await cloneResponse).json() as MappingSet;
      await expect(page).toHaveURL(new RegExp(escapeRegExp(cloned.id), 'i'));
    }
    await expect(page.getByRole('heading', { name: publishedNamePattern() })).toBeVisible();

    for (const rule of expectedRules) {
      await configureRuleFromUi(page, rule);
    }

    const validationResponse = page.waitForResponse((response) =>
      response.request().method() === 'POST'
      && new URL(response.url()).pathname.toLowerCase().endsWith(`/api/integrations/mappingsets/${cloned.id.toLowerCase()}/validate`)
    );
    const publishResponse = page.waitForResponse((response) =>
      response.request().method() === 'POST'
      && new URL(response.url()).pathname.toLowerCase().endsWith(`/api/integrations/mappingsets/${cloned.id.toLowerCase()}/publish`)
    );
    await page.getByRole('button', { name: 'Publicar', exact: true }).click();
    expect((await (await validationResponse).json() as { isValid: boolean }).isValid).toBeTruthy();
    expect((await publishResponse).ok()).toBeTruthy();
    published = await getPublished(page, headers, method.id);
    expect(published.id).toBe(cloned.id);
  }

  const parametersResponse = await page.request.get(`${api}/api/integrations/methods/${method.id}/parameters`, { headers });
  expect(parametersResponse.ok()).toBeTruthy();
  const parameters = await parametersResponse.json() as MethodParameter[];
  assertExpectedRules(published, parameters);
});

async function configureRuleFromUi(page: Page, expectedRule: ExpectedRule): Promise<void> {
  const parameter = page.locator('aside.left li').filter({
    has: page.locator('small').filter({ hasText: new RegExp(`^${escapeRegExp(expectedRule.parameterPath)}$`) })
  });
  await expect(parameter).toHaveCount(1);
  await parameter.click();

  const sourceKind = page.locator('[data-testid="source-kind-select"]');
  await sourceKind.selectOption(expectedRule.sourceKind);

  if (expectedRule.sourceFieldPath) {
    const sourceCatalog = page.locator('[data-testid="source-catalog-select"]');
    const option = sourceCatalog.locator('option').filter({ hasText: expectedRule.sourceFieldPath });
    await expect(option).toHaveCount(1);
    await sourceCatalog.selectOption({ label: (await option.textContent())!.trim() });
    await expect(page.locator('[data-testid="source-field-path-readonly"]')).toHaveValue(expectedRule.sourceFieldPath);
  }

  await page.locator('input[formcontrolname="fixedValue"]').fill(expectedRule.fixedValue ?? '');
  await page.locator('input[formcontrolname="defaultValue"]').fill(expectedRule.defaultValue ?? '');
  await page.locator('select[formcontrolname="transformationCode"]').selectOption('');
  await page.locator('input[formcontrolname="formatMask"]').fill('');
  await page.locator('input[formcontrolname="priority"]').fill('1');
  const enabled = page.locator('input[formcontrolname="enabled"]');
  if (!(await enabled.isChecked())) {
    await enabled.check();
  }

  const saveResponse = page.waitForResponse((response) =>
    response.request().method() === 'PUT'
    && /\/api\/integrations\/mappingsets\/[^/]+\/rules$/i.test(new URL(response.url()).pathname)
  );
  await page.getByRole('button', { name: 'Guardar borrador', exact: true }).click();
  const response = await saveResponse;
  expect(response.ok(), `No se pudo guardar ${expectedRule.parameterPath}: HTTP ${response.status()} ${await response.text()}`).toBeTruthy();
}

function assertExpectedRules(mappingSet: MappingSet, parameters: MethodParameter[]): void {
  expect([2, 'Published']).toContain(mappingSet.status);
  expect(mappingSet.isActive).toBeTruthy();
  for (const expectedRule of expectedRules) {
    const parameter = parameters.find((item) => item.parameterPath === expectedRule.parameterPath);
    expect(parameter, `Debe existir el parámetro ${expectedRule.parameterPath}.`).toBeTruthy();
    const rule = mappingSet.rules.find((item) => item.parameterId === parameter!.id && item.enabled);
    expect(rule, `${expectedRule.parameterPath} debe tener una regla activa.`).toBeTruthy();
    expect(normalizeSourceKind(rule!.sourceKind)).toBe(expectedRule.sourceKind);
    if (expectedRule.sourceFieldPath) {
      expect(rule!.sourceFieldPath.toLowerCase()).toBe(expectedRule.sourceFieldPath.toLowerCase());
    }
    expect(rule!.fixedValue ?? '').toBe(expectedRule.fixedValue ?? '');
    expect(rule!.defaultValue ?? '').toBe(expectedRule.defaultValue ?? '');
  }
}

async function authenticate(page: Page): Promise<string> {
  await page.goto(`${ui}/login`);
  await page.getByLabel('Usuario').fill(username);
  await page.locator('input[formcontrolname="password"]').fill(password);
  const loginResponse = page.waitForResponse((response) =>
    response.request().method() === 'POST'
    && new URL(response.url()).pathname.toLowerCase().endsWith('/auth/login')
  );
  await page.getByRole('button', { name: 'Ingresar', exact: true }).click();
  const response = await loginResponse;
  expect(response.ok()).toBeTruthy();
  const payload = await response.json() as AuthLoginResponse;
  const token = payload.data?.token
    ?? await page.evaluate(() => window.sessionStorage.getItem('ach.interbank.access_token'))
    ?? '';
  expect(token).toBeTruthy();
  return token;
}

async function getMethod(page: Page, headers: Record<string, string>): Promise<IntegrationMethod> {
  const response = await page.request.get(`${api}/api/integrations/methods`, { headers });
  expect(response.ok()).toBeTruthy();
  const methods = await response.json() as IntegrationMethod[];
  const method = methods.find((item) => item.code === methodCode && item.isActive);
  expect(method).toBeTruthy();
  return method!;
}

async function getPublished(page: Page, headers: Record<string, string>, methodId: number): Promise<MappingSet> {
  const response = await page.request.get(`${api}/api/integrations/mappingsets/published?methodId=${methodId}`, { headers });
  expect(response.ok()).toBeTruthy();
  return await response.json() as MappingSet;
}

function normalizeSourceKind(value: string | number): string {
  const byNumber: Record<number, string> = { 1: 'Transaction', 6: 'Constant' };
  return typeof value === 'number' ? (byNumber[value] ?? String(value)) : value;
}

function publishedNamePattern(): RegExp {
  return /ProcContrapartidas|configuración funcional/i;
}

function escapeRegExp(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
