import { readFileSync } from 'node:fs';
import { expect, test } from '@playwright/test';
import {
  buildIncomingProcTransaccionesFixture,
  incomingProcTransaccionesGoldenPath,
  validateIncomingProcTransaccionesControls
} from './support/incoming-proc-transacciones-fixture';
import {
  assertSyntheticSetupReadiness,
  readAuthorizedFixtureInput,
  type ProcTransaccionesSyntheticSetupResult
} from './support/proc-transacciones-preflight';
import { G36RuntimeDb } from './support/g36-runtime-db';

type LoginResponse = { data?: { token?: string }; token?: string; accessToken?: string };

const setupAuthorized = (process.env['ALLOW_PROC_TRANSACCIONES_SYNTHETIC_DATA_SETUP'] ?? '').trim().toLowerCase() === 'true';
const required = [
  'ACH_API_URL',
  'ACH_USER',
  'ACH_PASS',
  'ACH_E2E_DB_PROVIDER',
  'ACH_E2E_PROC_TRANSACCIONES_RECEIVER_ACCOUNT',
  'ACH_E2E_PROC_TRANSACCIONES_EXPECTED_AMOUNT'
].filter((name) => !process.env[name]);

test.describe.configure({ mode: 'serial' });
test.skip(!setupAuthorized, 'ALLOW_PROC_TRANSACCIONES_SYNTHETIC_DATA_SETUP=true es obligatorio; este spec no autoriza SOAP.');
test.skip(required.length > 0, `Faltan variables para setup PRE-LIVE: ${required.join(', ')}.`);

test('provisiona escenario sintético idempotente, genera fixture en memoria y valida controles sin upload', async () => {
  const token = await authenticate();
  const golden = readFileSync(incomingProcTransaccionesGoldenPath());
  const rawOperationalDate = golden.subarray(23, 31).toString('ascii');
  const operationalDate = `${rawOperationalDate.slice(0, 4)}-${rawOperationalDate.slice(4, 6)}-${rawOperationalDate.slice(6, 8)}`;
  const cycleNumber = 6;
  const first = await setupScenario(token, operationalDate, cycleNumber);
  const second = await setupScenario(token, operationalDate, cycleNumber);

  assertSyntheticSetupReadiness(first);
  assertSyntheticSetupReadiness(second);
  expect(second.createdExternalInstitution).toBe(false);
  expect(second.createdTransaction).toBe(false);
  expect(second.externalInstitutionId).toBe(first.externalInstitutionId);
  expect(second.transactionId).toBe(first.transactionId);

  const amount = Number(process.env['ACH_E2E_PROC_TRANSACCIONES_EXPECTED_AMOUNT']);
  const db = new G36RuntimeDb('playwright-local-proc-transacciones-setup');
  try {
    await db.assertIncomingProcTransaccionesReady();
    const scenario = await db.resolveIncomingProcTransaccionesScenario(
      process.env['ACH_E2E_PROC_TRANSACCIONES_RECEIVER_ACCOUNT']!,
      amount
    );
    const fixture = buildIncomingProcTransaccionesFixture(
      readAuthorizedFixtureInput('PTX-SETUP-PREFLIGHT-0001', scenario)
    );
    await db.assertIncomingProcTransaccionesFileAvailable(fixture.fileName);
    expect(fixture.content.equals(golden)).toBe(false);
    expect(fixture.receiverAccount).toBe(process.env['ACH_E2E_PROC_TRANSACCIONES_RECEIVER_ACCOUNT']);
    expect(fixture.amount).toBe(amount);
    expect(fixture.receivingDfi).toBe(scenario.receivingDfi);
    expect(fixture.externalOriginRouting).toBe(scenario.externalOriginRouting);
    expect(() => validateIncomingProcTransaccionesControls(fixture.content)).not.toThrow();
  } finally {
    await db.close();
  }
});

async function authenticate(): Promise<string> {
  const response = await fetch(`${process.env['ACH_API_URL']!.replace(/\/+$/, '')}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username: process.env['ACH_USER'], password: process.env['ACH_PASS'] })
  });
  expect(response.ok, 'El setup debe autenticarse con credenciales inyectadas.').toBeTruthy();
  const payload = await response.json() as LoginResponse;
  const token = payload.data?.token ?? payload.token ?? payload.accessToken;
  expect(token, 'El login debe devolver token sin imprimirlo.').toBeTruthy();
  return token!;
}

async function setupScenario(
  token: string,
  operationalDate: string,
  cycleNumber: number
): Promise<ProcTransaccionesSyntheticSetupResult & { createdExternalInstitution: boolean; createdTransaction: boolean }> {
  const response = await fetch(`${process.env['ACH_API_URL']!.replace(/\/+$/, '')}/Maintenance/proc-transacciones-e2e/setup`, {
    method: 'POST',
    headers: {
      Authorization: `Bearer ${token}`,
      'Content-Type': 'application/json'
    },
    body: JSON.stringify({ operationalDate, cycleNumber })
  });
  if (!response.ok) {
    throw new Error(`Setup PRE-LIVE falló con HTTP ${response.status}: ${await response.text()}`);
  }
  return await response.json() as ProcTransaccionesSyntheticSetupResult & {
    createdExternalInstitution: boolean;
    createdTransaction: boolean;
  };
}
