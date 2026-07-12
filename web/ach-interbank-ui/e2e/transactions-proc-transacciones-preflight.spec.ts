import { createHash } from 'node:crypto';
import { readFile } from 'node:fs/promises';
import { expect, test } from '@playwright/test';
import { appendFile, mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import {
  buildIncomingProcTransaccionesFixture,
  incomingProcTransaccionesGoldenPath,
  parseIncomingProcTransaccionesFixture,
  validateIncomingProcTransaccionesControls
} from './support/incoming-proc-transacciones-fixture';
import { findProcTransaccionesLogEvidence, snapshotSoapLogDirectory } from './support/local-soap-log-evidence';
import {
  assertEffectiveProcTransaccionesPreflight,
  assertSyntheticSetupAuthorization,
  assertSyntheticSetupReadiness,
  getConfirmedSoapCorrelationTokens,
  readAuthorizedFixtureInput,
  type ProcTransaccionesSyntheticSetupResult
} from './support/proc-transacciones-preflight';
import {
  incomingProcTransaccionesReadinessTables,
  procContrapartidasReadinessTables
} from './support/g36-runtime-db';

const authorizedEnvironment = {
  ALLOW_PROC_TRANSACCIONES_SYNTHETIC_DATA_SETUP: 'true',
  ACH_E2E_PROC_TRANSACCIONES_RECEIVER_ACCOUNT: 'E2EACCOUNT0008684',
  ACH_E2E_PROC_TRANSACCIONES_EXPECTED_AMOUNT: '123.45'
};

const readySetup: ProcTransaccionesSyntheticSetupResult = {
  isReady: true,
  setupAuthorized: true,
  cfaInstitutionId: 1,
  externalInstitutionId: 2,
  transactionId: 3,
  receivingDfi: '000010063',
  externalOriginRouting: '99999900',
  receiverAccountMasked: '*************8684',
  authorizedAmount: 123.45,
  transactionExternalId: 'E2E-PTX-IN-0123456789abcdef01234567'
};

test.describe('Proc_Transacciones pre-LIVE guardrails', () => {
  test('separa readiness de Contrapartidas del schema NACHA entrante', () => {
    expect(procContrapartidasReadinessTables).toEqual([
      'AchCycles',
      'AchTransactions',
      'ContrapartidaDispatchBatches',
      'ContrapartidaDispatchItems',
      'ContrapartidaDispatchAttempts'
    ]);
    expect(incomingProcTransaccionesReadinessTables).toContain('IncomingNachaIntegrationExecution');
    expect(incomingProcTransaccionesReadinessTables).not.toContain('ContrapartidaDispatchAttempts');
  });

  test('setup sin autorización adicional se bloquea', () => {
    expect(() => assertSyntheticSetupAuthorization({})).toThrow(/ALLOW_PROC_TRANSACCIONES_SYNTHETIC_DATA_SETUP=true/);
  });

  test('cuenta autorizada vacía se bloquea sin fallback', () => {
    expect(() => readAuthorizedFixtureInput('PTX-20260712-000001', readySetup, {
      ...authorizedEnvironment,
      ACH_E2E_PROC_TRANSACCIONES_RECEIVER_ACCOUNT: ''
    })).toThrow(/cuenta/i);
  });

  test('monto autorizado vacío se bloquea sin fallback', () => {
    expect(() => readAuthorizedFixtureInput('PTX-20260712-000002', readySetup, {
      ...authorizedEnvironment,
      ACH_E2E_PROC_TRANSACCIONES_EXPECTED_AMOUNT: ''
    })).toThrow(/monto/i);
  });

  test('CFA inexistente o ambigua se bloquea', () => {
    expect(() => assertSyntheticSetupReadiness({ ...readySetup, cfaInstitutionId: 0 })).toThrow(/CFA única/i);
  });

  test('origen inexistente o confundido con CFA se bloquea', () => {
    expect(() => assertSyntheticSetupReadiness({ ...readySetup, externalInstitutionId: readySetup.cfaInstitutionId })).toThrow(/origen sintético/i);
  });

  test('transacción receptora inexistente se bloquea', () => {
    expect(() => assertSyntheticSetupReadiness({ ...readySetup, transactionId: 0 })).toThrow(/transacción receptora/i);
  });

  test('escenario completo pasa readiness y genera fixture autorizado', () => {
    assertSyntheticSetupReadiness(readySetup);
    const input = readAuthorizedFixtureInput('PTX-20260712-000003', readySetup, authorizedEnvironment);
    const fixture = buildIncomingProcTransaccionesFixture(input);
    const parsed = parseIncomingProcTransaccionesFixture(fixture.content, fixture.uniqueRunKey);

    expect(parsed.receiverAccount).toBe(input.receiverAccount);
    expect(parsed.receivingDfi).toBe(input.receivingDfi);
    expect(parsed.amount).toBe(input.amount);
    expect(parsed.externalOriginRouting).toBe(input.externalOriginRouting);
    expect(parsed.transactionCode).toBe('22');
    expect(parsed.batchNumber).toBe('0000001');
    expect(() => validateIncomingProcTransaccionesControls(fixture.content)).not.toThrow();
  });

  test('golden original no cambia y controles se recalculan para dos montos', async () => {
    const originalBefore = await readFile(incomingProcTransaccionesGoldenPath());
    const hashBefore = sha256(originalBefore);
    const first = buildIncomingProcTransaccionesFixture({
      receiverAccount: 'E2EACCOUNT0008684',
      receivingDfi: readySetup.receivingDfi,
      amount: 123.45,
      externalOriginRouting: readySetup.externalOriginRouting,
      uniqueRunKey: 'PTX-20260712-000004'
    });
    const second = buildIncomingProcTransaccionesFixture({
      receiverAccount: 'E2EACCOUNT0008684',
      receivingDfi: readySetup.receivingDfi,
      amount: 9876.54,
      externalOriginRouting: readySetup.externalOriginRouting,
      uniqueRunKey: 'PTX-20260712-000005'
    });
    const originalAfter = await readFile(incomingProcTransaccionesGoldenPath());

    expect(sha256(originalAfter)).toBe(hashBefore);
    expect(first.content.equals(originalBefore)).toBe(false);
    expect(second.content.equals(originalBefore)).toBe(false);
    expect(first.fileControls.totalCreditAmountInCents).toBe(12_345);
    expect(second.fileControls.totalCreditAmountInCents).toBe(987_654);
    expect(first.fileControls.totalDebitAmountInCents).toBe(0);
    expect(second.fileControls.totalDebitAmountInCents).toBe(0);
    expect(first.fileControls.entryHash).toBe(Number(readySetup.receivingDfi.slice(0, 8)));
    expect(second.fileControls.entryHash).toBe(Number(readySetup.receivingDfi.slice(0, 8)));
  });

  test('genera IDTRAN dinámico compatible con el origen externo', () => {
    const first = buildIncomingProcTransaccionesFixture({
      receiverAccount: 'E2EACCOUNT0008684',
      receivingDfi: readySetup.receivingDfi,
      amount: 123.45,
      externalOriginRouting: readySetup.externalOriginRouting,
      uniqueRunKey: 'PTX-20260712-000006'
    });
    const second = buildIncomingProcTransaccionesFixture({
      receiverAccount: 'E2EACCOUNT0008684',
      receivingDfi: readySetup.receivingDfi,
      amount: 123.45,
      externalOriginRouting: readySetup.externalOriginRouting,
      uniqueRunKey: 'PTX-20260712-000007'
    });

    expect(first.transactionTrace).toMatch(/^99999900\d{7}$/);
    expect(first.transactionTrace).not.toBe(second.transactionTrace);
    expect(first.idTranSource).toBe('entryDetails.sequenceNumber');
    expect(first.idLote).toBe('0000001');
  });

  test('bloquea DryRun antes del upload y acepta solo el preflight Live completo', () => {
    const base = {
      wscfaachMappings: [{ methodName: 'Proc_Transacciones', endpoint: 'http://local/WSCFAACH.svc', soapAction: 'action', enabled: true }],
      procTransaccionesEffectiveSettings: {
        operation: 'Proc_Transacciones',
        effectiveMode: 'Live',
        endpoint: 'http://local/WSCFAACH.svc',
        enabled: true,
        mappingReady: true
      }
    };

    expect(assertEffectiveProcTransaccionesPreflight(base, 'http://local/WSCFAACH.svc')).toBe('http://local/WSCFAACH.svc');
    expect(() => assertEffectiveProcTransaccionesPreflight({
      ...base,
      procTransaccionesEffectiveSettings: { ...base.procTransaccionesEffectiveSettings, effectiveMode: 'DryRun' }
    }, 'http://local/WSCFAACH.svc')).toThrow(/bloqueada antes del upload/i);
  });

  test('correlaciona el log solo por tokens confirmados en RequestPayloadXml', async ({}, testInfo) => {
    const fixture = buildIncomingProcTransaccionesFixture({
      receiverAccount: 'E2EACCOUNT0008684',
      receivingDfi: readySetup.receivingDfi,
      amount: 123.45,
      externalOriginRouting: readySetup.externalOriginRouting,
      uniqueRunKey: 'PTX-20260712-000008'
    });
    const requestXml = `<Envelope><Proc_Transacciones><IDTRAN>${fixture.idTran}</IDTRAN><IDLOTE>${fixture.idLote}</IDLOTE></Proc_Transacciones></Envelope>`;
    const tokens = getConfirmedSoapCorrelationTokens(requestXml, fixture);
    const directory = testInfo.outputPath('soap-log');
    const logFile = path.join(directory, 'wscfaach.log');
    await mkdir(directory, { recursive: true });
    await writeFile(logFile, '<Envelope><Proc_Contrapartidas><OFIDTX>OLD</OFIDTX></Proc_Contrapartidas></Envelope>\n', 'utf8');
    const baseline = snapshotSoapLogDirectory(directory);
    const startedAt = new Date();
    await appendFile(logFile, '<Envelope><Proc_Transacciones><IDTRAN>999999999999999</IDTRAN></Proc_Transacciones></Envelope>\n', 'utf8');
    await appendFile(logFile, `${requestXml}\n`, 'utf8');

    const evidence = findProcTransaccionesLogEvidence(directory, baseline, startedAt, tokens);
    expect(evidence.text).toContain('Proc_Transacciones');
    expect(evidence.text).toContain(fixture.idTran);
    expect(evidence.text).not.toContain('Proc_Contrapartidas');
  });
});

function sha256(content: Buffer): string {
  return createHash('sha256').update(content).digest('hex');
}
