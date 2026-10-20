import { defineConfig, devices } from '@playwright/test';

const externalUiUrl = process.env['ACH_UI_URL'];
const e2eBaseUrl = process.env['E2E_BASE_URL'];
const ignoreHttpsErrors = stringEquals(process.env['E2E_IGNORE_HTTPS_ERRORS'], 'true');

export default defineConfig({
  testDir: './e2e',
  outputDir: './test-results',
  timeout: 30_000,
  expect: {
    timeout: 7_500
  },
  fullyParallel: false,
  retries: process.env['CI'] ? 1 : 0,
  reporter: [
    ['list'],
    ['html', { outputFolder: 'playwright-report', open: 'never' }]
  ],
  use: {
    baseURL: e2eBaseUrl || externalUiUrl || 'http://localhost:4200',
    browserName: 'chromium',
    ignoreHTTPSErrors: ignoreHttpsErrors,
    trace: 'retain-on-failure',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure'
  },
  webServer: externalUiUrl || e2eBaseUrl ? undefined : {
    command: 'npm start',
    url: 'http://localhost:4200',
    reuseExistingServer: !process.env['CI'],
    timeout: 120_000
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] }
    }
  ]
});

function stringEquals(value: string | undefined, expected: string): boolean {
  return (value ?? '').trim().toLowerCase() === expected.trim().toLowerCase();
}
