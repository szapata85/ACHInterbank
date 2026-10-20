import { expect, test, type Page, type TestInfo } from '@playwright/test';

const scalarUrl = process.env['ACH_SCALAR_URL'] ?? 'http://localhost:843/scalar/';
const spaUrl = process.env['ACH_UI_URL'] ?? 'http://localhost:743/';

test('Scalar renders the live OpenAPI document and exposes Bearer authentication', async ({ browser }, testInfo) => {
  const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
  const browserFailures = observeBrowserFailures(page);
  const startedAt = Date.now();
  const openApiResponsePromise = page.waitForResponse(
    response => response.url().includes('/openapi/v1.json'),
    { timeout: 20_000 });

  const navigation = await page.goto(scalarUrl, { waitUntil: 'domcontentloaded' });
  expect(navigation?.status()).toBe(200);

  const openApiResponse = await openApiResponsePromise;
  expect(openApiResponse.status()).toBe(200);
  expect(openApiResponse.headers()['content-type']).toContain('application/json');
  await openApiResponse.finished();
  const openApiCompletedAt = Date.now();

  const achCyclesGroup = page.getByRole('button', { name: /AchCycles Open Group/i });
  await expect(achCyclesGroup).toBeVisible({ timeout: 20_000 });
  const endpointsVisibleMs = Date.now() - startedAt;
  const renderAfterOpenApiMs = Date.now() - openApiCompletedAt;
  expect(endpointsVisibleMs).toBeLessThan(20_000);

  const search = page.locator('search, [role="search"]').first();
  await expect(search).toBeVisible();

  await achCyclesGroup.click();
  const endpoint = page.getByRole('button', {
    name: /\/ach-cycles\b.*HTTP Method: GET/i
  }).first();
  await expect(endpoint).toBeVisible();
  await endpoint.click();
  const endpointRegion = page.getByRole('region', { name: /\/ach-cycles\b/i }).first();
  await expect(endpointRegion).toBeVisible();
  await expect(endpointRegion.getByText(/Precauciones/i).first()).toBeVisible();
  await expect(endpointRegion.getByText(/Responses/i).first()).toBeVisible();
  await page.screenshot({ path: testInfo.outputPath('scalar-endpoint.png') });

  const authentication = page.getByRole('button', { name: /Select Auth Type/i });
  await expect(authentication).toBeVisible();
  await authentication.click();
  await expect(page.getByText(/Bearer/i).first()).toBeVisible();
  await page.screenshot({ path: testInfo.outputPath('scalar-bearer.png') });

  expect(browserFailures.consoleErrors, browserFailures.summary()).toEqual([]);
  expect(browserFailures.pageErrors, browserFailures.summary()).toEqual([]);
  expect(browserFailures.requestFailures, browserFailures.summary()).toEqual([]);
  expect(browserFailures.failedResponses, browserFailures.summary()).toEqual([]);

  const spaPage = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
  const spaFailures = observeBrowserFailures(spaPage);
  const brandingResponsePromise = spaPage.waitForResponse(
    response => response.url().includes('/api/users/branding'),
    { timeout: 20_000 });
  const spaNavigation = await spaPage.goto(spaUrl, { waitUntil: 'domcontentloaded' });
  expect(spaNavigation?.status()).toBe(200);
  expect((await brandingResponsePromise).status()).toBe(200);
  await expect(spaPage.locator('body')).not.toBeEmpty();
  await expect(spaPage.locator('script[src]').first()).toBeAttached();
  await spaPage.screenshot({ path: testInfo.outputPath('spa-runtime.png') });

  expect(spaFailures.consoleErrors, spaFailures.summary()).toEqual([]);
  expect(spaFailures.pageErrors, spaFailures.summary()).toEqual([]);
  expect(spaFailures.requestFailures, spaFailures.summary()).toEqual([]);
  expect(spaFailures.failedResponses, spaFailures.summary()).toEqual([]);

  console.log(`SCALAR_RUNTIME=${JSON.stringify({
    scalarStatus: navigation?.status(),
    openApiStatus: openApiResponse.status(),
    endpointsVisibleMs,
    renderAfterOpenApiMs,
    spaStatus: spaNavigation?.status(),
    brandingStatus: 200
  })}`);
});

function observeBrowserFailures(page: Page): {
  consoleErrors: string[];
  pageErrors: string[];
  requestFailures: string[];
  failedResponses: string[];
  summary: () => string;
} {
  const consoleErrors: string[] = [];
  const pageErrors: string[] = [];
  const requestFailures: string[] = [];
  const failedResponses: string[] = [];

  page.on('console', message => {
    if (message.type() === 'error') {
      consoleErrors.push(message.text());
    }
  });
  page.on('pageerror', error => pageErrors.push(error.message));
  page.on('requestfailed', request => {
    requestFailures.push(`${request.method()} ${request.url()} ${request.failure()?.errorText ?? ''}`.trim());
  });
  page.on('response', response => {
    if (response.status() >= 400) {
      failedResponses.push(`${response.status()} ${response.request().method()} ${response.url()}`);
    }
  });

  return {
    consoleErrors,
    pageErrors,
    requestFailures,
    failedResponses,
    summary: () => JSON.stringify(
      { consoleErrors, pageErrors, requestFailures, failedResponses },
      null,
      2)
  };
}
