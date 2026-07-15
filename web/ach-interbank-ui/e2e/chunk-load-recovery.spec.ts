import { expect, Page, test } from '@playwright/test';
import { readFileSync } from 'node:fs';
import path from 'node:path';

const spaBaseUrl = (process.env['E2E_BASE_URL'] ?? 'http://localhost:743').replace(/\/+$/, '');
const apiBaseUrl = (process.env['E2E_API_BASE_URL'] ?? 'http://localhost:843').replace(/\/+$/, '');
const loginPath = '/auth/login';
const chunkErrorPatterns = [
  /ChunkLoadError/i,
  /Loading chunk .* failed/i,
  /Failed to fetch dynamically imported module/i,
  /Importing a module script failed/i
];
test.use({ ignoreHTTPSErrors: true });

test.describe('Chunk load recovery and SPA navigation', () => {
  test('Nginx_ShouldServeIndexWithoutCacheAndAssetsWithImmutablePolicy', async ({ page }) => {
    const nginxConfig = readFileSync(path.resolve(__dirname, '../nginx.conf'), 'utf8');

    expect(nginxConfig).toContain('location = /index.html {');
    expect(nginxConfig).toContain('Cache-Control "no-store, no-cache, must-revalidate, proxy-revalidate, max-age=0" always;');
    expect(nginxConfig).toContain('location ~* "\\.[0-9a-f]{8,}\\.(?:js|css)$" {');
    expect(nginxConfig).toContain('Cache-Control "public, max-age=31536000, immutable" always;');
    expect(nginxConfig).toContain('location / {');
    expect(nginxConfig).toContain('try_files $uri $uri/ /index.html;');
    expect(nginxConfig).toContain('location @asset_not_found {');
    expect(nginxConfig).toContain('return 404 "Not Found";');

    const rootResponse = await page.request.get(`${spaBaseUrl}/`);
    const indexResponse = await page.request.get(`${spaBaseUrl}/index.html`);
    const rootHeaders = normalizeHeaders(rootResponse.headers());
    const indexHeaders = normalizeHeaders(indexResponse.headers());
    const rootHtml = await rootResponse.text();
    const hashedBundlePath = findFirstHashedAsset(rootHtml);

    expect(rootResponse.status()).toBe(200);
    expect(indexResponse.status()).toBe(200);
    expect(rootHeaders['cache-control']).toContain('no-store');
    expect(indexHeaders['cache-control']).toContain('no-store');

    expect(hashedBundlePath).toBeTruthy();
    const bundleResponse = await page.request.get(new URL(hashedBundlePath!, spaBaseUrl).href);
    const bundleHeaders = normalizeHeaders(bundleResponse.headers());
    expect(bundleResponse.status()).toBe(200);
    expect(bundleHeaders['cache-control']).toContain('immutable');

    const missingChunkResponse = await page.request.get(`${spaBaseUrl}/common.hash-inexistente.js`);
    const missingChunkHeaders = normalizeHeaders(missingChunkResponse.headers());
    expect(missingChunkResponse.status()).toBe(404);
    expect(missingChunkHeaders['content-type'] ?? '').not.toContain('text/html');
  });

  test('ChunkRecovery_ShouldReloadOnce_KeepSessionAndRecoverNavigation', async ({ page }) => {
    test.setTimeout(120_000);
    const token = await loginByApi();
    await seedAuthenticatedSession(page, token);

    const consoleErrors: string[] = [];
    const chunkMessages: string[] = [];
    const unexpectedConsoleErrors: string[] = [];
    const js404Responses: string[] = [];
    const htmlJsResponses: string[] = [];

    page.on('console', (message) => {
      if (message.type() !== 'error') {
        return;
      }

      const text = message.text();
      if (isChunkError(text)) {
        chunkMessages.push(text);
        return;
      }

      if (isBenignConsoleError(text)) {
        return;
      }

      consoleErrors.push(text);
      unexpectedConsoleErrors.push(text);
    });

    page.on('pageerror', (error) => {
      const text = String(error?.message ?? error);
      if (isChunkError(text)) {
        chunkMessages.push(text);
        return;
      }

      unexpectedConsoleErrors.push(text);
    });

    await page.setViewportSize({ width: 1366, height: 768 });
    const initialEntryScriptPattern = /\/(?:main|polyfills|runtime)\.[0-9a-f]+\.(?:js)$/i;
    let forcedChunk404s = 0;

    await page.route(/\.js(?:\?.*)?$/i, async (route) => {
      const request = route.request();
      const pathname = new URL(request.url()).pathname;

      if (request.resourceType() === 'script' && forcedChunk404s === 0 && !initialEntryScriptPattern.test(pathname)) {
        forcedChunk404s += 1;
        js404Responses.push(`${request.method()} ${pathname}`);
        await route.fulfill({
          status: 404,
          contentType: 'text/plain',
          body: 'Not Found'
        });
        return;
      }

      await route.continue();
    });

    await page.goto('/dashboard');
    await expect(page).toHaveURL(/\/dashboard$/);
    await expect(page.locator('.topbar .page-title')).toBeVisible({ timeout: 90_000 });
    await expect(page.locator('body')).not.toHaveText(/ChunkLoadError|Loading chunk .* failed|Failed to fetch dynamically imported module|Importing a module script failed/i);
    await expect.poll(async () => page.evaluate(() => window.sessionStorage.getItem('ach.interbank.access_token'))).not.toBeNull();

    expect(forcedChunk404s).toBe(1);
    expect(js404Responses).toHaveLength(1);
    expect(htmlJsResponses).toEqual([]);
    expect(unexpectedConsoleErrors).toEqual([]);
    expect(consoleErrors).toEqual([]);
  });

  test('MenuNavigation_ShouldLoadVisibleOptionsWithoutChunkErrorsOrHtmlJsResponses', async ({ page }) => {
    test.setTimeout(120_000);
    const token = await loginByApi();
    await seedAuthenticatedSession(page, token);

    const consoleErrors: string[] = [];
    const js404Responses: string[] = [];
    const htmlJsResponses: string[] = [];

    page.on('console', (message) => {
      if (message.type() !== 'error') {
        return;
      }

      const text = message.text();
      if (!isBenignConsoleError(text)) {
        consoleErrors.push(text);
      }
    });

    page.on('response', async (response) => {
      if (!isJavaScriptAsset(response.url())) {
        return;
      }

      const contentType = response.headers()['content-type'] ?? '';
      if (response.status() === 404) {
        js404Responses.push(`${response.status()} ${response.url()}`);
      }

      if (contentType.includes('text/html')) {
        htmlJsResponses.push(`${response.status()} ${response.url()} ${contentType}`);
      }
    });

    await page.setViewportSize({ width: 1366, height: 768 });
    await page.goto('/dashboard');
    await expect.poll(async () => page.locator('.menu-header > a.menu-item').count(), { timeout: 90_000 }).toBeGreaterThan(0);
    await expect(page.locator('.topbar .page-title')).toBeVisible({ timeout: 90_000 });

    const visibleMenuRoutes = await collectVisibleMenuRoutes(page);
    const navigableMenuRoutes = visibleMenuRoutes.filter((route) => route.href !== '/dashboard' && route.href !== '/logs' && route.href !== '/log');
    expect(navigableMenuRoutes.length).toBeGreaterThan(0);

    for (const route of navigableMenuRoutes) {
      const previousPath = new URL(page.url()).pathname;
      if (route.href === previousPath) {
        continue;
      }

      await page.locator(`.menu-header > a.menu-item[href="${route.href}"]`).click();
      await expect.poll(async () => new URL(page.url()).pathname).not.toBe(previousPath);
      await expect(page.locator('.topbar .page-title')).toBeVisible();
    }

    expect(js404Responses).toEqual([]);
    expect(htmlJsResponses).toEqual([]);
    expect(consoleErrors).toEqual([]);
  });
});

async function loginByApi(): Promise<string> {
  const response = await fetch(`${apiBaseUrl}${loginPath}`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      username: process.env['ACH_USER'] ?? 'admin',
      password: process.env['ACH_PASS'] ?? 'Admin123!'
    }),
    signal: AbortSignal.timeout(10_000)
  });

  expect(response.ok, `Login debe responder HTTP 200, obtuvo ${response.status}.`).toBeTruthy();
  const payload = (await response.json()) as { data?: { token?: string }; token?: string };
  const token = payload.data?.token ?? payload.token;
  expect(token, 'El login debe devolver un access token.').toBeTruthy();
  return token as string;
}

async function seedAuthenticatedSession(page: Page, token: string): Promise<void> {
  await page.addInitScript((accessToken) => {
    window.sessionStorage.setItem('ach.interbank.access_token', accessToken);
  }, token);
}

async function collectVisibleMenuRoutes(page: Page): Promise<Array<{ label: string; href: string }>> {
  const routes = await page.locator('.menu-header > a.menu-item').evaluateAll((nodes) =>
    nodes
      .map((node) => {
        const anchor = node as HTMLAnchorElement;
        return {
          label: (anchor.textContent ?? '').replace(/\s+/g, ' ').trim(),
          href: anchor.getAttribute('href') ?? ''
        };
      })
      .filter((item) => item.href.length > 0)
  );

  const unique = new Map<string, { label: string; href: string }>();
  for (const route of routes) {
    if (!unique.has(route.href)) {
      unique.set(route.href, route);
    }
  }

  return [...unique.values()];
}

function findFirstHashedAsset(html: string): string | null {
  const matches = [...html.matchAll(/<script[^>]+src="([^"]+\.js[^"]*)"/gi)];
  const hashed = matches
    .map((match) => match[1])
    .find((src) => /\.[0-9a-f]{8,}\.js(?:\?.*)?$/i.test(src));

  return hashed ?? null;
}

function normalizeHeaders(headers: Record<string, string>): Record<string, string> {
  return Object.fromEntries(Object.entries(headers).map(([key, value]) => [key.toLowerCase(), value]));
}

function isJavaScriptAsset(url: string): boolean {
  return /\.js(?:\?.*)?$/i.test(url);
}

function isChunkError(text: string): boolean {
  return chunkErrorPatterns.some((pattern) => pattern.test(text));
}

function isBenignConsoleError(text: string): boolean {
  return /net::ERR_CONNECTION_REFUSED/i.test(text)
    || /favicon\.ico/i.test(text)
    || /ResizeObserver loop limit exceeded/i.test(text)
    || /\[webpack-dev-server\] Errors while compiling/i.test(text)
    || /NG6009/i.test(text)
    || /standalone component, which can not be used in the `@NgModule\.bootstrap` array/i.test(text)
    || /Failed to load resource: the server responded with a status of 404/i.test(text);
}

function escapeForRegex(value: string): string {
  return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}
