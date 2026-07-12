import { expect, test } from '@playwright/test';
import { appendFile, mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import {
  buildIncomingProcTransaccionesFixture,
  parseIncomingProcTransaccionesFixture
} from './support/incoming-proc-transacciones-fixture';
import { findProcTransaccionesLogEvidence, snapshotSoapLogDirectory } from './support/local-soap-log-evidence';
import {
  assertExpectedAmount,
  assertExpectedReceiverAccount,
  assertEffectiveProcTransaccionesPreflight,
  getConfirmedSoapCorrelationTokens
} from './support/proc-transacciones-preflight';
import {
  incomingProcTransaccionesReadinessTables,
  procContrapartidasReadinessTables
} from './support/g36-runtime-db';

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

  test('genera IDTRAN dinámico y parser-compatible recupera los campos escritos', () => {
    const first = buildIncomingProcTransaccionesFixture('PTX-20260712-000001');
    const second = buildIncomingProcTransaccionesFixture('PTX-20260712-000002');
    const parsed = parseIncomingProcTransaccionesFixture(first.content, first.uniqueRunKey);

    expect(first.transactionTrace).toMatch(/^\d{15}$/);
    expect(first.transactionTrace).not.toBe(second.transactionTrace);
    expect(parsed.transactionTrace).toBe(first.idTran);
    expect(parsed.batchNumber).toBe(first.idLote);
    expect(first.idTranSource).toBe('entryDetails.sequenceNumber');
    expect(first.idLoteSource).toBe('batchHeaders.batchNumber');
    expect(first.liveBlockedReason).toContain('0000001');
  });

  test('bloquea cuenta receptora o monto no autorizados antes del upload', () => {
    const fixture = buildIncomingProcTransaccionesFixture('PTX-20260712-000003');

    expect(() => assertExpectedReceiverAccount(fixture, '0000')).toThrow(/cuenta receptora/i);
    expect(() => assertExpectedAmount(fixture, '999999')).toThrow(/monto/i);
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
    const fixture = buildIncomingProcTransaccionesFixture('PTX-20260712-000004');
    const requestXml = `<Envelope><Proc_Transacciones><IDTRAN>${fixture.idTran}</IDTRAN><IDLOTE>${fixture.idLote}</IDLOTE></Proc_Transacciones></Envelope>`;
    const tokens = getConfirmedSoapCorrelationTokens(requestXml, fixture);
    const directory = testInfo.outputPath('soap-log');
    const logFile = path.join(directory, 'wscfaach.log');
    await mkdir(directory, { recursive: true });
    await writeFile(logFile, '<Envelope><Proc_Contrapartidas><OFIDTX>OLD</OFIDTX></Proc_Contrapartidas></Envelope>\n', 'utf8');
    const baseline = snapshotSoapLogDirectory(directory);
    const startedAt = new Date();
    await appendFile(logFile, `<Envelope><Proc_Transacciones><IDTRAN>999999999999999</IDTRAN></Proc_Transacciones></Envelope>\n`, 'utf8');
    await appendFile(logFile, `${requestXml}\n`, 'utf8');

    const evidence = findProcTransaccionesLogEvidence(directory, baseline, startedAt, tokens);
    expect(evidence.text).toContain('Proc_Transacciones');
    expect(evidence.text).toContain(fixture.idTran);
    expect(evidence.text).not.toContain('Proc_Contrapartidas');
  });
});
