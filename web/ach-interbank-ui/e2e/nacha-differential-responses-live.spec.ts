import { expect, test } from '@playwright/test';
import { access } from 'node:fs/promises';

const liveEnabled = /^true$/i.test((process.env['ACH_DIFFERENTIAL_RESPONSES_LIVE_OPT_IN'] ?? '').trim());
const packagePath = (process.env['ACH_DIFFERENTIAL_RESPONSES_PACKAGE_PATH'] ?? '').trim();

test.describe('Respuestas diferenciales LIVE local - preflight opt-in', () => {
  test.skip(!liveEnabled, 'Live deshabilitado: ACH_DIFFERENTIAL_RESPONSES_LIVE_OPT_IN no es true.');

  test('acepta exclusivamente infraestructura local y un paquete configurado', async ({ request }) => {
    expect(packagePath, 'ACH_DIFFERENTIAL_RESPONSES_PACKAGE_PATH debe configurarse de forma explicita.').not.toBe('');
    await expect(access(packagePath)).resolves.toBeUndefined();

    const apiUrl = new URL(process.env['ACH_API_URL'] ?? 'http://localhost:843');
    const spaUrl = new URL(process.env['ACH_UI_URL'] ?? 'http://localhost:743');
    expectLocalControlledUrl(apiUrl, 'ACH_API_URL');
    expectLocalControlledUrl(spaUrl, 'ACH_UI_URL');

    const ready = await request.get(new URL('/health/ready', apiUrl).toString());
    expect(ready.ok(), 'El API local debe estar ready antes de cualquier flujo LIVE.').toBeTruthy();

    const spa = await request.get(spaUrl.toString());
    expect(spa.ok(), 'La SPA local debe estar disponible antes de cualquier flujo LIVE.').toBeTruthy();
  });
});

function expectLocalControlledUrl(url: URL, variableName: string): void {
  expect(['http:', 'https:'], `${variableName} debe usar HTTP(S).`).toContain(url.protocol);
  expect(
    ['localhost', '127.0.0.1', 'host.docker.internal'],
    `${variableName} no puede apuntar a infraestructura externa.`
  ).toContain(url.hostname.toLowerCase());
}
