import { createHash } from 'node:crypto';
import { readFile } from 'node:fs/promises';
import { mkdtempSync, readFileSync, rmSync } from 'node:fs';
import { execFileSync } from 'node:child_process';
import { expect, test } from '@playwright/test';
import { appendFile, mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';
import { tmpdir } from 'node:os';
import {
  buildIncomingProcTransaccionesFixture,
  buildIncomingProcTransaccionesCenitFixture,
  incomingProcTransaccionesGoldenPath,
  parseIncomingProcTransaccionesFixture,
  validateIncomingProcTransaccionesControls
} from './support/incoming-proc-transacciones-fixture';
import {
  assertAuthorizedOfficialProcTransaccionesEntryName,
  deriveOfficialProcTransaccionesSingleBatchFixture,
  findOfficialProcTransaccionesEligibleEntries,
  loadOfficialProcTransaccionesArchiveInventory,
  selectOfficialProcTransaccionesEntry,
  selectUniqueIngestionCandidate
} from './support/official-proc-transacciones-cenit';
import { findProcTransaccionesLogEvidence, snapshotSoapLogDirectory } from './support/local-soap-log-evidence';
import {
  assertEffectiveProcTransaccionesPreflight,
  assertExactProcTransaccionesHealth,
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
    if (!process.env['CENIT_TEST_PACKAGE_PATH']) {
      expect(() => buildIncomingProcTransaccionesCenitFixture(input)).toThrow(/CENIT_TEST_PACKAGE_PATH/);
      return;
    }
    const fixture = buildIncomingProcTransaccionesCenitFixture(input);
    const parsed = parseIncomingProcTransaccionesFixture(fixture.content, fixture.uniqueRunKey, fixture.fileName);

    expect(parsed.receiverAccount).toBe(input.receiverAccount);
    expect(parsed.receivingDfi).toBe(input.receivingDfi);
    expect(parsed.amount).toBe(input.amount);
    expect(parsed.externalOriginRouting).toBe(input.externalOriginRouting);
    expect(parsed.transactionCode).toBe('32');
    expect(parsed.batchNumber).toBe('0000001');
    expect(() => validateIncomingProcTransaccionesControls(fixture.content)).not.toThrow();

    const batch = fixture.content.subarray(106, 212).toString('ascii');
    const entry = fixture.content.subarray(212, 318).toString('ascii');
    const addenda = fixture.content.subarray(318, 424).toString('ascii');
    expect(batch.slice(4, 20).trimEnd()).toBe('BANCO UAT CENIT');
    expect(batch.slice(20, 40).trimEnd()).toBe('ESCENARIO E2E');
    expect(batch.slice(40, 50)).toBe('E2ECENIT01');
    expect(batch.slice(53, 63)).toBe('CREDITOE2E');
    expect(batch.slice(83, 91)).toBe(input.externalOriginRouting);
    expect(batch.slice(91, 98)).toBe('0000001');
    expect(entry.slice(3, 12)).toBe(input.receivingDfi);
    expect(entry.slice(12, 29).trimEnd()).toBe(input.receiverAccount);
    expect(Number(entry.slice(29, 47)) / 100).toBe(input.amount);
    expect(entry.slice(47, 62)).toBe('E2EPTXANCHOR001');
    expect(entry.slice(62, 84).trimEnd()).toBe('RECEPTOR E2E');
    expect(entry.slice(84, 86)).toBe('  ');
    expect(entry.slice(87, 102)).toBe(fixture.transactionTrace);
    expect(addenda.slice(3, 16).trimEnd()).toBe('E2EPTXANCHOR');
    expect(addenda.slice(16, 30)).toBe(' '.repeat(14));
    expect(addenda.slice(30, 30 + fixture.uniqueRunKey.length)).toBe(fixture.uniqueRunKey);
    expect(addenda.slice(30 + fixture.uniqueRunKey.length, 87)).toBe(' '.repeat(87 - 30 - fixture.uniqueRunKey.length));
    expect(addenda.slice(87, 94)).toBe(fixture.transactionTrace.slice(-7));
    expect(addenda.slice(94)).toBe(' '.repeat(12));
  });

  test('SHA incorrecto bloquea el paquete CENIT antes de construir el fixture', () => {
    test.skip(
      !process.env['CENIT_TEST_PACKAGE_PATH']
      || !process.env['CENIT_TEST_PACKAGE_SHA256']
      || !process.env['CENIT_TEST_ENTRY_NAME'],
      'CENIT_TEST_PACKAGE_PATH, CENIT_TEST_PACKAGE_SHA256 y CENIT_TEST_ENTRY_NAME son requeridos para validar el hash oficial.'
    );
    return expect(selectOfficialProcTransaccionesEntry({
      packagePath: process.env['CENIT_TEST_PACKAGE_PATH']!,
      expectedPackageSha256: '0'.repeat(64),
      selectedEntryName: process.env['CENIT_TEST_ENTRY_NAME']!
    })).rejects.toThrow(/SHA-256/i);
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

  test.describe('official zip helper', () => {
    test.describe.configure({ timeout: 120_000 });

    let officialInventory: Awaited<ReturnType<typeof loadOfficialProcTransaccionesArchiveInventory>> | undefined;
    let officialSelection: Awaited<ReturnType<typeof selectOfficialProcTransaccionesEntry>> | undefined;
    let missingEntryZip: string | undefined;

    test.skip(
      !process.env['CENIT_TEST_PACKAGE_PATH']
      || !process.env['CENIT_TEST_PACKAGE_SHA256']
      || !process.env['CENIT_TEST_ENTRY_NAME']
      || !process.env['CENIT_TEST_BATCH_ORDINAL'],
      'CENIT_TEST_PACKAGE_PATH, CENIT_TEST_PACKAGE_SHA256, CENIT_TEST_ENTRY_NAME y CENIT_TEST_BATCH_ORDINAL son requeridos para validar el ZIP oficial.'
    );

    test.beforeAll(async () => {
      officialInventory = await loadOfficialProcTransaccionesArchiveInventory({
        packagePath: process.env['CENIT_TEST_PACKAGE_PATH']!,
        expectedPackageSha256: process.env['CENIT_TEST_PACKAGE_SHA256']!
      });
      officialSelection = await selectOfficialProcTransaccionesEntry({
        packagePath: process.env['CENIT_TEST_PACKAGE_PATH']!,
        expectedPackageSha256: process.env['CENIT_TEST_PACKAGE_SHA256']!,
        selectedEntryName: process.env['CENIT_TEST_ENTRY_NAME']!
      });
      missingEntryZip = createZipWithoutEntry(
        process.env['CENIT_TEST_PACKAGE_PATH']!,
        '0001283.002.20260713.1'
      );
    });

    test('acepta el nombre completo oficial y rechaza el abreviado', async () => {
      expect(officialInventory?.entries.map((entry) => entry.fileName)).toEqual([
        '0001283.001.20260713.1',
        '0001283.002.20260713.1',
        '0001283.003.20260713.1',
        '0001283.004.20260713.1',
        '0001283.005.20260713.1'
      ]);
      expect(assertAuthorizedOfficialProcTransaccionesEntryName('0001283.002.20260713.1')).toBe('0001283.002.20260713.1');
      expect(() => assertAuthorizedOfficialProcTransaccionesEntryName('0001283.002.1')).toThrow(/CENIT_TEST_ENTRY_NAME/);
    });

    test('bloquea una entrada inexistente aunque el hash sea correcto', async () => {
      try {
        const tempHash = sha256(readFileSync(missingEntryZip!));
        await expect(selectOfficialProcTransaccionesEntry({
          packagePath: missingEntryZip!,
          expectedPackageSha256: tempHash,
          selectedEntryName: '0001283.002.20260713.1'
        })).rejects.toThrow(/no contiene|no existe/i);
      } finally {
        rmSync(path.dirname(missingEntryZip!), { recursive: true, force: true });
      }
    });

    test('bloquea el ZIP con hash incorrecto', async () => {
      await expect(selectOfficialProcTransaccionesEntry({
        packagePath: process.env['CENIT_TEST_PACKAGE_PATH']!,
        expectedPackageSha256: '0'.repeat(64),
        selectedEntryName: '0001283.002.20260713.1'
      })).rejects.toThrow(/SHA-256/i);
    });

    test('mantiene intactos los bytes extraídos', async () => {
      expect(sha256(officialSelection!.selectedBytes)).toBe(officialSelection!.selectedEntry.sha256);
      expect(officialSelection!.selectedEntry.fileName).toBe(process.env['CENIT_TEST_ENTRY_NAME']!);
    });

    test('bloquea más de una entrada elegible en el mismo contenido', async () => {
      const duplicated = Buffer.concat([officialSelection!.selectedBytes, officialSelection!.selectedBytes]);
      const firstEligible = extractFirstEligibleEntrySignature(officialSelection!.selectedBytes);
      const matches = findOfficialProcTransaccionesEligibleEntries(
        officialSelection!.selectedEntry.fileName,
        duplicated,
        firstEligible.receiverAccount,
        firstEligible.amount
      );

      expect(matches.length).toBeGreaterThan(1);
    });

    test('correlaciona por filename y ventana sin duplicados', () => {
      const base = new Date('2026-07-13T10:00:00Z');
      const candidates = [
        { fileName: '0001283.002.20260713.1', uploadedAtUtc: '2026-07-13T09:59:59Z', correlationId: 'old' },
        { fileName: '0001283.002.20260713.1', uploadedAtUtc: '2026-07-13T10:00:01Z', correlationId: 'new' }
      ];

      expect(selectUniqueIngestionCandidate(candidates, '0001283.002.20260713.1', base).correlationId).toBe('new');
      expect(() => selectUniqueIngestionCandidate(candidates, '0001283.002.20260713.1', new Date('2026-07-13T11:00:00Z'))).toThrow(/ingesti/i);
      expect(() => selectUniqueIngestionCandidate([
        ...candidates,
        { fileName: '0001283.002.20260713.1', uploadedAtUtc: '2026-07-13T10:00:02Z', correlationId: 'third' }
      ], '0001283.002.20260713.1', base)).toThrow(/ingesti/i);
    });

    test('health exacta acepta Healthy y rechaza Unhealthy', () => {
      expect(() => assertExactProcTransaccionesHealth({
        status: 'Healthy',
        check: 'live',
        service: 'ACHInterbank'
      }, 'live')).not.toThrow();
      expect(() => assertExactProcTransaccionesHealth({
        status: 'Healthy',
        check: 'ready',
        database: 'Healthy'
      }, 'ready')).not.toThrow();
      expect(() => assertExactProcTransaccionesHealth({
        status: 'Unhealthy',
        check: 'live',
        service: 'ACHInterbank'
      }, 'live')).toThrow(/Healthy/i);
    });

    test('deriva un fixture UAT de un solo lote fisico y persiste ZIP y manifest', async () => {
      const sourcePackagePath = process.env['CENIT_TEST_PACKAGE_PATH']!;
      const sourcePackageSha256 = process.env['CENIT_TEST_PACKAGE_SHA256']!;
      const sourceEntryName = process.env['CENIT_TEST_ENTRY_NAME']!;
      const selectedBatchOrdinal = Number(process.env['CENIT_TEST_BATCH_ORDINAL']!);
      const receiverAccount = '02001033883';
      const expectedAmount = 3000;
      const derivedPackagePath = path.resolve('..', '..', 'docs', 'uat', 'CENIT_Proc_Transacciones_SINGLE_20260713.zip');
      const derivedManifestPath = path.resolve('..', '..', 'docs', 'uat', 'CENIT_Proc_Transacciones_SINGLE_20260713.manifest.json');

      const derived = await deriveOfficialProcTransaccionesSingleBatchFixture({
        sourcePackagePath,
        sourcePackageSha256,
        sourceEntryName,
        selectedBatchOrdinal,
        receiverAccount,
        expectedAmount,
        derivedPackagePath,
        derivedManifestPath
      });

      const bytes = readFileSync(derivedPackagePath);
      const manifest = JSON.parse(readFileSync(derivedManifestPath, 'utf8')) as typeof derived.manifest;

      expect(derived.selectedBatchOrdinal).toBe(4);
      expect(derived.batchNumberRaw7).toBe('0000004');
      expect(derived.idLoteExpectedD6).toBe('000004');
      expect(derived.selectedEligibleEntry.idTran).toBe(derived.selectedEligibleEntry.traceSequence7);
      expect(derived.selectedEligibleEntry.traceNumber15).toHaveLength(15);
      expect(derived.selectedEligibleEntry.originatorCode8).toHaveLength(8);
      expect(derived.selectedEligibleEntry.traceSequence7).toHaveLength(7);
      expect(derived.selectedEligibleEntry.transactionCode).toBe('32');
      expect(derived.manifest.transactionCode).toBe('32');
      expect(derived.manifest.batchCount).toBe(1);
      expect(derived.manifest.eligibleEntryCount).toBe(1);
      expect(derived.manifest.recordCount).toBe(10);
      expect(derived.manifest.scc).toBe('220');
      expect(derived.manifest.effectiveDate).toBe('20260713');
      expect(derived.manifest.idLoteExpectedD6).toBe('000004');
      expect(derived.manifest.derivedEntrySha256).toBe(derived.derivedEntrySha256);
      expect(derived.manifest.derivedPackageSha256).toBe(derived.derivedPackageSha256);
      expect(manifest.derivedPackageSha256).toBe(derived.derivedPackageSha256);
      expect(sha256(bytes)).toBe(derived.derivedPackageSha256);

      const derivedArchive = await selectOfficialProcTransaccionesEntry({
        packagePath: derivedPackagePath,
        expectedPackageSha256: derived.derivedPackageSha256,
        selectedEntryName: sourceEntryName,
        requiredEntryNames: [sourceEntryName]
      });
      expect(derivedArchive.entries).toHaveLength(1);
      expect(derivedArchive.selectedEntry.recordCount).toBe(10);
      expect(derivedArchive.selectedEntry.batchCount).toBe(1);
      expect(derivedArchive.selectedEntry.addenda05Count).toBe(1);
      expect(derivedArchive.selectedEntry.transactionCodes).toEqual(['32']);
      expect(derivedArchive.selectedEntry.effectiveDate).toBe('20260713');

      const derivedEligibleEntries = findOfficialProcTransaccionesEligibleEntries(
        sourceEntryName,
        derivedArchive.selectedBytes,
        receiverAccount,
        expectedAmount
      );
      expect(derivedEligibleEntries).toHaveLength(1);
      expect(derivedEligibleEntries[0].batchOrdinal).toBe(4);
      expect(derivedEligibleEntries[0].batchNumberRaw7).toBe('0000004');
      expect(derivedEligibleEntries[0].idTran).toHaveLength(7);
      expect(derivedEligibleEntries[0].idLote).toBe('000004');
    });

    test('el spec live no acelera Quartz ni limpia evidencia', () => {
      const liveSpec = readFileSync(path.resolve('e2e/transactions-proc-transacciones.spec.ts'), 'utf8');
      expect(liveSpec).toContain('http://localhost:843/health/live');
      expect(liveSpec).toContain('http://localhost:843/health/ready');
      expect(liveSpec).toContain('http://localhost:743/login');
      expect(liveSpec).toContain("process.env['ACH_TEST_FILE_PATH']");
      expect(liveSpec).toContain("process.env['ACH_TEST_FILE_NAME']");
      expect(liveSpec).toContain("name: fileName");
      expect(liveSpec).not.toContain('CENIT_TEST_PACKAGE_PATH');
      expect(liveSpec).not.toContain("uploadFileName = '0000004.002.1'");
      expect(liveSpec).not.toContain('buildIncomingProcTransaccionesCenitFixture');
      expect(liveSpec).not.toContain('readAuthorizedFixtureInput');
      expect(liveSpec).not.toContain('accelerateIncomingPostProcessing');
      expect(liveSpec).not.toContain('cleanupIncomingProcTransaccionesRun');
      expect(liveSpec).not.toContain('TaskDefinition');
    });
  });
});

function sha256(content: Buffer): string {
  return createHash('sha256').update(content).digest('hex').toUpperCase();
}

function createZipWithoutEntry(sourceZipPath: string, entryName: string): string {
  const tempDirectory = mkdtempSync(path.join(tmpdir(), 'proc-transacciones-zip-'));
  const tempZipPath = path.join(tempDirectory, 'missing-entry.zip');
  const script = `
Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.IO.Compression
$sourcePath = '${escapePowerShell(sourceZipPath)}'
$destinationPath = '${escapePowerShell(tempZipPath)}'
$entryName = '${escapePowerShell(entryName)}'
$source = [System.IO.Compression.ZipFile]::OpenRead($sourcePath)
$destination = [System.IO.Compression.ZipFile]::Open($destinationPath, [System.IO.Compression.ZipArchiveMode]::Create)
try {
  foreach ($entry in $source.Entries) {
    if ($entry.FullName -eq $entryName) { continue }
    $newEntry = $destination.CreateEntry($entry.FullName)
    $sourceStream = $entry.Open()
    $destinationStream = $newEntry.Open()
    try {
      $sourceStream.CopyTo($destinationStream)
    }
    finally {
      $destinationStream.Dispose()
      $sourceStream.Dispose()
    }
  }
}
finally {
  $destination.Dispose()
  $source.Dispose()
}
`;

  execFileSync('powershell', ['-NoProfile', '-Command', script], { stdio: 'pipe' });
  return tempZipPath;
}

function escapePowerShell(value: string): string {
  return value.replace(/'/g, "''");
}

function extractFirstEligibleEntrySignature(fileBytes: Buffer): { receiverAccount: string; amount: number } {
  const recordLength = 106;
  if (fileBytes.length % recordLength !== 0) {
    throw new Error('El archivo oficial debe conservar longitud fija para detectar la primera entrada elegible.');
  }

  for (let offset = 0; offset < fileBytes.length; offset += recordLength) {
    const record = fileBytes.subarray(offset, offset + recordLength).toString('ascii');
    if (record[0] !== '6' || record.slice(1, 3) !== '32') {
      continue;
    }

    return {
      receiverAccount: record.slice(12, 29).trimEnd(),
      amount: Number(record.slice(29, 47)) / 100
    };
  }

  throw new Error('No se encontró una entrada elegible 32 en el archivo oficial seleccionado.');
}
