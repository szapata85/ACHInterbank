import { expect, Page } from '@playwright/test';

export type SimulatorRequest = {
  simulationMode: 'IncomingTransactions' | 'DifferentialResponses';
  clearingHouseCode: string;
  scenarioType: string;
  originFinancialInstitutionId: number;
  entriesCount: number;
  amount: number;
  referencePrefix: string;
  businessDate: string;
  cycleCode: string;
  pendingPrenotificationReferences: string[];
  transactionReferences: string[];
  responseMode?: string | null;
  reasonCode?: string | null;
  notes?: string | null;
};

export type EligibleTransaction = {
  id: number;
  identifier: string;
  traceNumber: string;
  clearingHouse: string;
  destinationFinancialInstitutionId: number;
  destinationFinancialInstitution: string;
  transactionType: string;
  effectiveDate: string;
  cycle: string;
  amount: number;
  state: string;
  hasPriorResponse: boolean;
  eligible: boolean;
  ineligibilityReason?: string | null;
};

export type SimulatorHarness = {
  previewRequests: SimulatorRequest[];
  generateRequests: SimulatorRequest[];
  eligibleQueries: URLSearchParams[];
  forbiddenSoapRequests: string[];
  forbiddenMoneyRequests: string[];
  forbiddenUploadRequests: string[];
  externalApiRequests: string[];
  consoleErrors: string[];
  pageErrors: string[];
};

export type SimulatorHarnessOptions = {
  previewEligible?: boolean;
  previewMessage?: string;
  generateStatus?: number;
  generateBody?: Record<string, unknown>;
  eligiblePage?: (page: number) => { items: EligibleTransaction[]; total: number };
};

export const requiredSimulatorViewports = [
  { width: 1920, height: 1080 },
  { width: 1366, height: 768 },
  { width: 1280, height: 720 },
  { width: 1024, height: 768 }
] as const;

const simulatorPath = '/uat/nacha-inbound-simulator';
const simulatorListEndpoint = /\/api\/uat\/nacha-inbound-simulator(?:\?.*)?$/;
const simulatorPreviewEndpoint = /\/api\/uat\/nacha-inbound-simulator\/eligibility-preview$/;
const simulatorGenerateEndpoint = /\/api\/uat\/nacha-inbound-simulator\/generate$/;
const simulatorEligibleEndpoint = /\/api\/uat\/nacha-inbound-simulator\/eligible-differential-transactions(?:\?.*)?$/;

export async function installControlledSimulatorHarness(
  page: Page,
  options: SimulatorHarnessOptions = {}
): Promise<SimulatorHarness> {
  const harness: SimulatorHarness = {
    previewRequests: [],
    generateRequests: [],
    eligibleQueries: [],
    forbiddenSoapRequests: [],
    forbiddenMoneyRequests: [],
    forbiddenUploadRequests: [],
    externalApiRequests: [],
    consoleErrors: [],
    pageErrors: []
  };

  const token = createUnsignedJwt({
    unique_name: 'uat.simulator',
    name: 'Usuario UAT Simulador',
    uid: 'uat-simulator',
    role: ['Admin', 'ACH.Operator'],
    permission: [
      'CanReadAch',
      'CanManageAch',
      'P1.NachaSimulatorRead',
      'P1.NachaSimulatorGenerateIncoming',
      'P1.NachaSimulatorGenerateDifferential',
      'P1.NachaSimulatorDownload'
    ],
    exp: Math.floor(Date.now() / 1000) + 3600,
    iat: Math.floor(Date.now() / 1000)
  });

  await page.addInitScript((accessToken) => {
    window.sessionStorage.setItem('ach.interbank.access_token', accessToken);
  }, token);

  page.on('console', (message) => {
    if (message.type() === 'error' && !/favicon\.ico|ResizeObserver loop limit exceeded/i.test(message.text())) {
      harness.consoleErrors.push(message.text());
    }
  });
  page.on('pageerror', (error) => harness.pageErrors.push(error.message));
  page.on('request', (request) => {
    const url = request.url();
    const methodAndUrl = `${request.method()} ${url}`;
    if (/WSCFAACH|WSAxonRespuestaTransacciones|\/soap(?:\/|\?|$)/i.test(url)) {
      harness.forbiddenSoapRequests.push(methodAndUrl);
    }
    if (/proc[_-]?(?:transacciones|contrapartidas)|movimientos|money-movement/i.test(url)) {
      harness.forbiddenMoneyRequests.push(methodAndUrl);
    }
    if (/nacha[-_]?upload|\/upload(?:\/|\?|$)/i.test(url)) {
      harness.forbiddenUploadRequests.push(methodAndUrl);
    }

    if (request.resourceType() === 'xhr' || request.resourceType() === 'fetch') {
      const hostname = new URL(url).hostname.toLowerCase();
      if (!['localhost', '127.0.0.1', 'host.docker.internal'].includes(hostname)) {
        harness.externalApiRequests.push(methodAndUrl);
      }
    }
  });

  await page.route('https://fonts.googleapis.com/**', async (route) => {
    await route.fulfill({ status: 200, contentType: 'text/css', body: '' });
  });
  await page.route(/\/auth\/refresh$/, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        sucess: true,
        data: {
          token,
          username: 'uat.simulator',
          fullName: 'Usuario UAT Simulador',
          roles: ['Admin', 'ACH.Operator'],
          permissions: ['CanReadAch', 'CanManageAch']
        }
      })
    });
  });
  await page.route(/\/navigation\/menu$/, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        {
          id: 1,
          label: 'Herramientas UAT',
          route: '/uat',
          order: 1,
          children: [{ id: 2, label: 'Simulador NACHA-M', route: simulatorPath, order: 1, children: [] }]
        }
      ])
    });
  });
  await page.route(/\/api\/users\/branding(?:\?.*)?$/, async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: '{}' });
  });
  await page.route(/\/api\/navigation-logs(?:\?.*)?$/, async (route) => {
    await route.fulfill({ status: 204, body: '' });
  });
  await page.route(/\/financial-institutions(?:\?.*)?$/, async (route) => {
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify([
        {
          id: 100,
          name: 'Banco Externo UAT',
          routingNumber: '99999',
          transitCode: '900',
          checkDigit: '0',
          isDefaultSource: false,
          status: 1
        },
        {
          id: 200,
          name: 'CFA Receptora',
          routingNumber: '00001',
          transitCode: '283',
          checkDigit: '0',
          isDefaultSource: true,
          status: 1
        }
      ])
    });
  });
  await page.route(simulatorEligibleEndpoint, async (route) => {
    const url = new URL(route.request().url());
    const query = new URLSearchParams(url.searchParams);
    harness.eligibleQueries.push(query);
    const pageNumber = Number(query.get('page') ?? '1');
    const response = options.eligiblePage?.(pageNumber) ?? defaultEligiblePage(pageNumber);
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({ items: response.items, page: pageNumber, pageSize: 10, total: response.total })
    });
  });
  await page.route(simulatorPreviewEndpoint, async (route) => {
    harness.previewRequests.push(route.request().postDataJSON() as SimulatorRequest);
    await route.fulfill({
      status: 200,
      contentType: 'application/json',
      body: JSON.stringify({
        eligible: options.previewEligible ?? true,
        decision: options.previewEligible === false ? 'Blocked' : 'Eligible',
        message: options.previewMessage ?? 'Solicitud elegible para simulacion controlada.',
        simulationMode: harness.previewRequests.at(-1)?.simulationMode ?? 'IncomingTransactions'
      })
    });
  });
  await page.route(simulatorGenerateEndpoint, async (route) => {
    harness.generateRequests.push(route.request().postDataJSON() as SimulatorRequest);
    const status = options.generateStatus ?? 201;
    const body = options.generateBody ?? incomingGenerationResult();
    await route.fulfill({ status, contentType: 'application/json', body: JSON.stringify(body) });
  });
  await page.route(simulatorListEndpoint, async (route) => {
    await route.fulfill({ status: 200, contentType: 'application/json', body: '[]' });
  });

  return harness;
}

export async function openSimulator(page: Page): Promise<void> {
  await page.goto(simulatorPath);
  await expect(page.getByRole('heading', { name: 'Simulador NACHA-M', level: 1 })).toBeVisible();
  await expect(page.locator('button.mode-card').filter({ hasText: 'Transacciones entrantes' })).toBeVisible();
  await expect(page.locator('button.mode-card').filter({ hasText: 'Respuestas diferenciales' })).toBeVisible();
}

export async function expectSimulatorFitsViewport(
  page: Page,
  viewport: { width: number; height: number }
): Promise<void> {
  await page.setViewportSize(viewport);

  const modeSelector = page.locator('.mode-selector');
  const actions = page.locator('section.summary .actions');
  await modeSelector.scrollIntoViewIfNeeded();
  await expect(modeSelector).toBeVisible();
  await expect(page.locator('button.mode-card')).toHaveCount(2);
  await expectElementsInsideViewport(page, page.locator('button.mode-card'), viewport.width);

  await actions.scrollIntoViewIfNeeded();
  await expect(actions).toBeVisible();
  await expect(actions.locator('button')).toHaveCount(2);
  await expectElementsInsideViewport(page, actions.locator('button'), viewport.width);

  const overflow = await page.evaluate(() => ({
    document: document.documentElement.scrollWidth - document.documentElement.clientWidth,
    body: document.body.scrollWidth - document.body.clientWidth,
    modes: (document.querySelector('.mode-options')?.scrollWidth ?? 0)
      - (document.querySelector('.mode-options')?.clientWidth ?? 0),
    actions: (document.querySelector('section.summary .actions')?.scrollWidth ?? 0)
      - (document.querySelector('section.summary .actions')?.clientWidth ?? 0)
  }));

  expect(overflow.document, `Overflow global en ${viewport.width}x${viewport.height}.`).toBeLessThanOrEqual(1);
  expect(overflow.body, `Overflow del body en ${viewport.width}x${viewport.height}.`).toBeLessThanOrEqual(1);
  expect(overflow.modes, `Overflow del selector de modos en ${viewport.width}x${viewport.height}.`).toBeLessThanOrEqual(1);
  expect(overflow.actions, `Overflow de acciones en ${viewport.width}x${viewport.height}.`).toBeLessThanOrEqual(1);
}

export function expectNoForbiddenSimulatorSideEffects(harness: SimulatorHarness): void {
  expect(harness.forbiddenSoapRequests, 'El simulador no debe invocar SOAP desde la SPA.').toEqual([]);
  expect(harness.forbiddenMoneyRequests, 'El simulador no debe invocar movimientos monetarios desde la SPA.').toEqual([]);
  expect(harness.forbiddenUploadRequests, 'Generar no debe evitar el flujo formal mediante un upload directo.').toEqual([]);
  expect(harness.externalApiRequests, 'La prueba controlada no debe llamar APIs externas.').toEqual([]);
  expect(harness.pageErrors, 'La pagina no debe producir pageerror.').toEqual([]);
  expect(harness.consoleErrors, 'La pagina no debe producir errores de consola.').toEqual([]);
}

export function eligibleTransaction(overrides: Partial<EligibleTransaction> = {}): EligibleTransaction {
  return {
    id: 501,
    identifier: 'TX-CFA-501',
    traceNumber: '000012830000501',
    clearingHouse: 'ACHCOL',
    destinationFinancialInstitutionId: 100,
    destinationFinancialInstitution: 'Banco Externo UAT',
    transactionType: 'Credit',
    effectiveDate: '2026-07-18T00:00:00Z',
    cycle: 'Ciclo 3',
    amount: 125000.5,
    state: 'Pending',
    hasPriorResponse: false,
    eligible: true,
    ineligibilityReason: null,
    ...overrides
  };
}

async function expectElementsInsideViewport(page: Page, locator: ReturnType<Page['locator']>, viewportWidth: number): Promise<void> {
  const count = await locator.count();
  for (let index = 0; index < count; index += 1) {
    const element = locator.nth(index);
    const box = await element.boundingBox();
    expect(box, `El elemento ${index + 1} debe tener geometria visible.`).not.toBeNull();
    expect(box?.x ?? -1, `El elemento ${index + 1} no debe salir por la izquierda.`).toBeGreaterThanOrEqual(0);
    expect(
      (box?.x ?? 0) + (box?.width ?? 0),
      `El elemento ${index + 1} no debe salir por la derecha.`
    ).toBeLessThanOrEqual(viewportWidth + 1);
  }
}

function defaultEligiblePage(pageNumber: number): { items: EligibleTransaction[]; total: number } {
  if (pageNumber > 1) {
    return { items: [eligibleTransaction({ id: 511, identifier: 'TX-CFA-511', traceNumber: '000012830000511' })], total: 11 };
  }

  return {
    items: [
      eligibleTransaction(),
      eligibleTransaction({
        id: 502,
        identifier: 'TX-CFA-502',
        traceNumber: '000012830000502',
        hasPriorResponse: true,
        eligible: false,
        ineligibilityReason: 'Ya tiene una respuesta asociada.'
      })
    ],
    total: 11
  };
}

function incomingGenerationResult(): Record<string, unknown> {
  return {
    id: 1,
    simulationId: 'sim-uat-incoming-001',
    fileName: '0001283.001.20260718.1.OUT',
    downloadUrl: '/api/uat/nacha-inbound-simulator/1/file',
    evidenceUrl: '/api/uat/nacha-inbound-simulator/1/evidence',
    sha256: 'A'.repeat(64),
    fileSizeBytes: 1060,
    generatedOnly: true,
    autoImported: false,
    uploadRequired: true,
    externalTransmission: false,
    message: 'Archivo NACHA-M simulado generado. Debe cargarse manualmente por NachaUpload.'
  };
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
