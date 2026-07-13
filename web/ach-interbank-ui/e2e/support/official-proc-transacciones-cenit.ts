import { createHash } from 'node:crypto';
import { mkdir, readFile, writeFile } from 'node:fs/promises';
import * as path from 'node:path';
import * as yauzl from 'yauzl';

const recordLength = 106;
const authorizedEntryNames = [
  '0001283.001.20260713.1',
  '0001283.002.20260713.1',
  '0001283.003.20260713.1',
  '0001283.004.20260713.1',
  '0001283.005.20260713.1'
] as const;

type AuthorizedEntryName = typeof authorizedEntryNames[number];

type SelectedBatchSegment = {
  batchOrdinal: number;
  batchHeader: string;
  batchHeaderIndex: number;
  batchNumberRaw7: string;
  effectiveDate: string;
  scc: string;
  records: string[];
  entryRecords: string[];
  addendaRecords: string[];
  batchControl: string;
  batchControlIndex: number;
};

export type OfficialProcTransaccionesArchiveOptions = {
  packagePath: string;
  expectedPackageSha256: string;
};

export type OfficialProcTransaccionesEntryInventory = {
  fileName: string;
  sha256: string;
  size: number;
  recordCount: number | null;
  fixedLengthValid: boolean;
  recordTypes: string[];
  batchCount: number;
  scc: string | null;
  transactionCodes: string[];
  addenda05Count: number;
  effectiveDate: string | null;
};

export type OfficialProcTransaccionesEligibleEntry = {
  fileName: string;
  batchOrdinal: number;
  batchNumber: string;
  batchNumberRaw7: string;
  entryIndex: number;
  transactionCode: string;
  receiverAccount: string;
  amount: number;
  traceNumber15: string;
  originatorCode8: string;
  traceSequence7: string;
  idTran: string;
  idLote: string;
};

export type OfficialProcTransaccionesArchiveInventory = {
  packageSha256: string;
  entries: OfficialProcTransaccionesEntryInventory[];
};

export type OfficialProcTransaccionesDerivedFixtureManifest = {
  sourcePackageSha256: string;
  sourceEntrySha256: string;
  sourceEntryName: string;
  selectedBatchOrdinal: number;
  batchNumberRaw7: string;
  traceSequence7: string;
  idLoteExpectedD6: string;
  derivedEntrySha256: string;
  derivedPackageSha256: string;
  recordCount: number;
  batchCount: number;
  eligibleEntryCount: number;
  transactionCode: string;
  effectiveDate: string;
  scc: string;
  createdAtUtc: string;
};

export type OfficialProcTransaccionesDerivedFixture = {
  packageSha256: string;
  sourcePackageSha256: string;
  sourceEntrySha256: string;
  sourceEntryName: string;
  selectedBatchOrdinal: number;
  batchNumberRaw7: string;
  traceSequence7: string;
  idLoteExpectedD6: string;
  selectedEligibleEntry: OfficialProcTransaccionesEligibleEntry;
  derivedEntrySha256: string;
  derivedPackageSha256: string;
  bytes: Buffer;
  manifest: OfficialProcTransaccionesDerivedFixtureManifest;
};

export type OfficialProcTransaccionesSelection = OfficialProcTransaccionesArchiveInventory & {
  selectedEntryName: string;
  selectedBytes: Buffer;
  selectedEntry: OfficialProcTransaccionesEntryInventory;
};

export type IngestionCandidate = {
  fileName: string;
  uploadedAtUtc: Date | string;
  correlationId: string;
};

export function assertAuthorizedOfficialProcTransaccionesEntryName(entryName: string): string {
  const normalized = entryName.trim();
  if (!normalized) {
    throw new Error('CENIT_TEST_ENTRY_NAME is required and does not admit fallback.');
  }

  if (!authorizedEntryNames.includes(normalized as AuthorizedEntryName)) {
    throw new Error(`CENIT_TEST_ENTRY_NAME must be one of: ${authorizedEntryNames.join(', ')}.`);
  }

  return normalized;
}

export function selectUniqueIngestionCandidate(
  candidates: readonly IngestionCandidate[],
  fileName: string,
  uploadedAfterUtc: Date
): IngestionCandidate {
  const normalizedFileName = fileName.trim();
  const matches = candidates.filter((candidate) =>
    candidate.fileName === normalizedFileName && new Date(candidate.uploadedAtUtc).getTime() >= uploadedAfterUtc.getTime());

  if (matches.length === 0) {
    throw new Error('No se encontro una ingestión NACHA candidata en la ventana solicitada.');
  }

  if (matches.length > 1) {
    throw new Error('La correlación por FileName y ventana devolvió más de una ingestión.');
  }

  return matches[0];
}

export async function loadOfficialProcTransaccionesArchiveInventory(
  options: OfficialProcTransaccionesArchiveOptions & { requiredEntryNames?: readonly string[] }
): Promise<OfficialProcTransaccionesArchiveInventory> {
  const zipBytes = await readFile(options.packagePath);
  const packageSha256 = sha256(zipBytes);
  assertPackageHash(packageSha256, options.expectedPackageSha256);

  const requiredEntryNames = normalizeRequiredEntryNames(options.requiredEntryNames);
  const entries = await readOfficialZipEntriesFromBuffer(zipBytes, requiredEntryNames);
  return buildOfficialArchiveInventory(entries, packageSha256, requiredEntryNames);
}

export async function selectOfficialProcTransaccionesEntry(
  options: OfficialProcTransaccionesArchiveOptions & { selectedEntryName: string; requiredEntryNames?: readonly string[] }
): Promise<OfficialProcTransaccionesSelection> {
  const selectedEntryName = assertAuthorizedOfficialProcTransaccionesEntryName(options.selectedEntryName);
  const zipBytes = await readFile(options.packagePath);
  const packageSha256 = sha256(zipBytes);
  assertPackageHash(packageSha256, options.expectedPackageSha256);

  const requiredEntryNames = normalizeRequiredEntryNames(options.requiredEntryNames ?? [selectedEntryName]);
  const zipEntries = await readOfficialZipEntriesFromBuffer(zipBytes, requiredEntryNames);
  const archive = buildOfficialArchiveInventory(zipEntries, packageSha256, requiredEntryNames);
  const matchingEntries = zipEntries.filter((entry) => entry.fileName === selectedEntryName);

  if (matchingEntries.length === 0) {
    throw new Error(`La entrada autorizada ${selectedEntryName} no existe exactamente en el ZIP.`);
  }

  if (matchingEntries.length > 1) {
    throw new Error(`La entrada autorizada ${selectedEntryName} aparece mas de una vez en el ZIP.`);
  }

  const selected = matchingEntries[0];
  const selectedEntry = analyzeOfficialEntry(selected.fileName, selected.buffer);
  if (selectedEntry.sha256 !== selected.sha256) {
    throw new Error(`La entrada ${selectedEntryName} cambio durante la extraccion.`);
  }

  return {
    ...archive,
    selectedEntryName,
    selectedBytes: Buffer.from(selected.buffer),
    selectedEntry
  };
}

export async function deriveOfficialProcTransaccionesSingleBatchFixture(options: {
  sourcePackagePath: string;
  sourcePackageSha256: string;
  sourceEntryName: string;
  selectedBatchOrdinal: number;
  receiverAccount: string;
  expectedAmount: number;
  derivedPackagePath: string;
  derivedManifestPath: string;
}): Promise<OfficialProcTransaccionesDerivedFixture> {
  const sourceSelection = await selectOfficialProcTransaccionesEntry({
    packagePath: options.sourcePackagePath,
    expectedPackageSha256: options.sourcePackageSha256,
    selectedEntryName: options.sourceEntryName
  });

  const sourceRecords = splitRecords(sourceSelection.selectedBytes);
  const selectedBatch = selectBatchByOrdinal(sourceRecords, options.selectedBatchOrdinal);
  const fileControlTemplate = sourceRecords[findFileControlRecordIndex(sourceRecords)];
  const eligibleEntries = findOfficialProcTransaccionesEligibleEntries(
    sourceSelection.selectedEntry.fileName,
    Buffer.from(selectedBatch.records.join(''), 'ascii'),
    options.receiverAccount,
    options.expectedAmount
  );

  const batchNumberRaw7 = selectedBatch.batchNumberRaw7;
  const batchEligibleEntries = eligibleEntries.filter((entry) => entry.batchNumberRaw7 === batchNumberRaw7);
  if (batchEligibleEntries.length !== 1) {
    throw new Error('NO-GO SINGLE FIXTURE: SELECTED_BATCH_NOT_UNIQUE');
  }

  const selectedEligibleEntry = batchEligibleEntries[0];
  const derivedRecords = buildDerivedSingleBatchRecords(sourceRecords[0], selectedBatch, fileControlTemplate);
  const derivedBytes = Buffer.from(derivedRecords.join(''), 'ascii');
  const derivedEntrySha256 = sha256(derivedBytes);
  const derivedZipBytes = createStoredZip(sourceSelection.selectedEntry.fileName, derivedBytes, selectedBatch.effectiveDate);
  const derivedPackageSha256 = sha256(derivedZipBytes);

  const manifest: OfficialProcTransaccionesDerivedFixtureManifest = {
    sourcePackageSha256: sourceSelection.packageSha256,
    sourceEntrySha256: sourceSelection.selectedEntry.sha256,
    sourceEntryName: sourceSelection.selectedEntry.fileName,
    selectedBatchOrdinal: options.selectedBatchOrdinal,
    batchNumberRaw7,
    traceSequence7: selectedEligibleEntry.traceSequence7,
    idLoteExpectedD6: selectedEligibleEntry.idLote,
    derivedEntrySha256,
    derivedPackageSha256,
    recordCount: derivedRecords.length,
    batchCount: 1,
    eligibleEntryCount: 1,
    transactionCode: selectedEligibleEntry.transactionCode,
    effectiveDate: selectedBatch.effectiveDate,
    scc: selectedBatch.scc,
    createdAtUtc: new Date().toISOString()
  };

  await mkdir(path.dirname(options.derivedPackagePath), { recursive: true });
  await writeFile(options.derivedPackagePath, derivedZipBytes);
  await writeFile(options.derivedManifestPath, `${JSON.stringify(manifest, null, 2)}\n`, 'utf8');

  return {
    packageSha256: derivedPackageSha256,
    sourcePackageSha256: sourceSelection.packageSha256,
    sourceEntrySha256: sourceSelection.selectedEntry.sha256,
    sourceEntryName: sourceSelection.selectedEntry.fileName,
    selectedBatchOrdinal: options.selectedBatchOrdinal,
    batchNumberRaw7,
    traceSequence7: selectedEligibleEntry.traceSequence7,
    idLoteExpectedD6: selectedEligibleEntry.idLote,
    selectedEligibleEntry,
    derivedEntrySha256,
    derivedPackageSha256,
    bytes: derivedBytes,
    manifest
  };
}

export function findOfficialProcTransaccionesEligibleEntries(
  fileName: string,
  fileBytes: Buffer,
  receiverAccount: string,
  expectedAmount: number
): OfficialProcTransaccionesEligibleEntry[] {
  const records = splitRecords(fileBytes);
  const eligible: OfficialProcTransaccionesEligibleEntry[] = [];
  const trimmedAccount = receiverAccount.trim();

  for (const [index, record] of records.entries()) {
    if (record[0] !== '6') {
      continue;
    }

    const transactionCode = record.slice(1, 3);
    const entryAccount = record.slice(12, 29).trimEnd();
    const amount = Number(record.slice(29, 47)) / 100;
    if (transactionCode !== '32' || entryAccount !== trimmedAccount || amount !== expectedAmount) {
      continue;
    }

    const batchNumberRaw7 = locateBatchNumber(records, index);
    const batchOrdinal = Number(batchNumberRaw7);
    if (!Number.isInteger(batchOrdinal) || batchOrdinal < 1) {
      throw new Error('BatchNumber must contain a valid physical batch ordinal.');
    }

    const traceNumber15 = record.slice(87, 102);
    if (!/^\d{15}$/.test(traceNumber15)) {
      throw new Error('TraceNumber must contain exactly 15 digits.');
    }

    const originatorCode8 = traceNumber15.slice(0, 8);
    const traceSequence7 = traceNumber15.slice(8);
    if (!/^\d{8}$/.test(originatorCode8)) {
      throw new Error('OriginatorCode must contain exactly 8 digits.');
    }
    if (!/^\d{7}$/.test(traceSequence7)) {
      throw new Error('TraceSequenceNumber must contain exactly 7 digits.');
    }

    const batchNumberValue = Number(batchNumberRaw7);
    if (!Number.isInteger(batchNumberValue) || batchNumberValue < 0 || batchNumberValue > 999999) {
      throw new Error('BatchNumber cannot be represented as IDLOTE D6.');
    }

    const idLoteExpectedD6 = batchNumberValue.toString().padStart(6, '0');
    eligible.push({
      fileName,
      batchOrdinal,
      batchNumber: batchNumberRaw7,
      batchNumberRaw7,
      entryIndex: index,
      transactionCode,
      receiverAccount: entryAccount,
      amount,
      traceNumber15,
      originatorCode8,
      traceSequence7,
      idTran: traceSequence7,
      idLote: idLoteExpectedD6
    });
  }

  return eligible;
}

function buildOfficialArchiveInventory(
  entries: Array<{ fileName: string; buffer: Buffer; sha256: string }>,
  packageSha256: string,
  requiredEntryNames: readonly string[]
): OfficialProcTransaccionesArchiveInventory {
  const duplicates = requiredEntryNames.filter((entryName) =>
    entries.filter((entry) => entry.fileName === entryName).length > 1);
  if (duplicates.length > 0) {
    throw new Error(`El paquete CENIT contiene entradas duplicadas: ${duplicates.join(', ')}.`);
  }

  const inventory = entries
    .filter((entry) => requiredEntryNames.includes(entry.fileName as AuthorizedEntryName))
    .map((entry) => analyzeOfficialEntry(entry.fileName, entry.buffer));

  const missing = requiredEntryNames.filter((name) => !inventory.some((entry) => entry.fileName === name));
  if (missing.length > 0) {
    throw new Error(`El paquete CENIT no contiene las entradas requeridas: ${missing.join(', ')}.`);
  }

  return {
    packageSha256,
    entries: inventory.sort((left, right) =>
      requiredEntryNames.indexOf(left.fileName as AuthorizedEntryName) - requiredEntryNames.indexOf(right.fileName as AuthorizedEntryName))
  };
}

function analyzeOfficialEntry(fileName: string, buffer: Buffer): OfficialProcTransaccionesEntryInventory {
  const fixedLengthValid = buffer.length % recordLength === 0;
  const records = fixedLengthValid ? splitRecords(buffer) : [];
  if (!fixedLengthValid) {
    return {
      fileName,
      sha256: sha256(buffer),
      size: buffer.length,
      recordCount: null,
      fixedLengthValid: false,
      recordTypes: [],
      batchCount: 0,
      scc: null,
      transactionCodes: [],
      addenda05Count: 0,
      effectiveDate: null
    };
  }

  const batchHeaders = records.filter((record) => record[0] === '5');
  const entryDetails = records.filter((record) => record[0] === '6');
  const recordTypes = [...new Set(records.map((record) => record[0]))].sort();
  const effectiveDate = batchHeaders
    .map((record) => record.match(/20260713/)?.[0] ?? null)
    .find((value) => value !== null) ?? null;

  return {
    fileName,
    sha256: sha256(buffer),
    size: buffer.length,
    recordCount: records.length,
    fixedLengthValid: true,
    recordTypes,
    batchCount: batchHeaders.length,
    scc: batchHeaders[0]?.slice(1, 4) ?? null,
    transactionCodes: [...new Set(entryDetails.map((record) => record.slice(1, 3)))].sort(),
    addenda05Count: records.filter((record) => record.startsWith('705')).length,
    effectiveDate
  };
}

async function readOfficialZipEntriesFromBuffer(
  zipBytes: Buffer,
  requiredEntryNames: readonly string[]
): Promise<Array<{ fileName: string; buffer: Buffer; sha256: string }>> {
  const zipfile = await openZipFromBuffer(zipBytes);
  const entries: Array<{ fileName: string; buffer: Buffer; sha256: string }> = [];
  const requiredSet = new Set(requiredEntryNames);

  await new Promise<void>((resolve, reject) => {
    zipfile.on('entry', (entry) => {
      if (!requiredSet.has(entry.fileName as AuthorizedEntryName)) {
        zipfile.readEntry();
        return;
      }

      readEntryBuffer(zipfile, entry)
        .then((buffer) => {
          entries.push({
            fileName: entry.fileName,
            buffer,
            sha256: sha256(buffer)
          });
          zipfile.readEntry();
        })
        .catch(reject);
    });

    zipfile.once('end', () => {
      zipfile.close();
      resolve();
    });
    zipfile.once('error', (error) => {
      zipfile.close();
      reject(error);
    });
    zipfile.readEntry();
  });

  return entries;
}

function selectBatchByOrdinal(records: string[], batchOrdinal: number): SelectedBatchSegment {
  if (!Number.isInteger(batchOrdinal) || batchOrdinal < 1) {
    throw new Error('CENIT_TEST_BATCH_ORDINAL must be a positive integer.');
  }

  const fileControlIndex = findFileControlRecordIndex(records);
  const batchHeaderIndices = records
    .map((record, index) => record[0] === '5' && index < fileControlIndex ? index : -1)
    .filter((index) => index >= 0);

  if (batchOrdinal > batchHeaderIndices.length) {
    throw new Error('NO-GO SINGLE FIXTURE: SELECTED_BATCH_NOT_UNIQUE');
  }

  const start = batchHeaderIndices[batchOrdinal - 1];
  const end = batchOrdinal === batchHeaderIndices.length ? fileControlIndex : batchHeaderIndices[batchOrdinal];
  const batchRecords = records.slice(start, end);
  if (batchRecords.length < 4) {
    throw new Error('NO-GO SINGLE FIXTURE: SELECTED_BATCH_NOT_UNIQUE');
  }

  const batchHeader = batchRecords[0];
  const batchControl = batchRecords[batchRecords.length - 1];
  if (batchHeader[0] !== '5' || batchControl[0] !== '8') {
    throw new Error('NO-GO SINGLE FIXTURE: SELECTED_BATCH_NOT_UNIQUE');
  }

  const batchNumberRaw7 = batchHeader.slice(91, 98);
  if (!/^\d{7}$/.test(batchNumberRaw7)) {
    throw new Error('BatchNumber must contain exactly 7 digits.');
  }

  const effectiveDate = batchHeader.match(/20260713/)?.[0] ?? '20260713';
  const scc = batchHeader.slice(1, 4);
  const entryRecords = batchRecords.filter((record) => record[0] === '6');
  const addendaRecords = batchRecords.filter((record) => record[0] === '7');

  return {
    batchOrdinal,
    batchHeader,
    batchHeaderIndex: start,
    batchNumberRaw7,
    effectiveDate,
    scc,
    records: batchRecords,
    entryRecords,
    addendaRecords,
    batchControl,
    batchControlIndex: batchRecords.length - 1
  };
}

function buildDerivedSingleBatchRecords(
  fileHeader: string,
  selectedBatch: SelectedBatchSegment,
  fileControlTemplate: string
): string[] {
  const batchMetrics = calculateControls(selectedBatch.records);
  if (batchMetrics.eligibleEntryCount !== 1) {
    throw new Error('NO-GO SINGLE FIXTURE: SELECTED_BATCH_NOT_UNIQUE');
  }

  const batchControl = writeBatchControl(selectedBatch.batchControl, batchMetrics);
  const provisionalRecords = [
    fileHeader,
    ...selectedBatch.records.slice(0, -1),
    batchControl
  ];
  const paddingCount = (10 - ((provisionalRecords.length + 1) % 10)) % 10;
  const fileMetrics = {
    batchCount: 1,
    blockCount: (provisionalRecords.length + 1 + paddingCount) / 10,
    entryAddendaCount: batchMetrics.entryAddendaCount,
    entryHash: batchMetrics.entryHash,
    totalDebitAmountInCents: batchMetrics.totalDebitAmountInCents,
    totalCreditAmountInCents: batchMetrics.totalCreditAmountInCents
  };
  const fileControl = writeFileControl(fileControlTemplate, fileMetrics);

  return [
    fileHeader,
    ...selectedBatch.records.slice(0, -1),
    batchControl,
    fileControl,
    ...Array.from({ length: paddingCount }, () => '9'.repeat(recordLength))
  ];
}

function calculateControls(records: string[]): {
  eligibleEntryCount: number;
  entryAddendaCount: number;
  entryHash: number;
  totalDebitAmountInCents: number;
  totalCreditAmountInCents: number;
} {
  const entries = records.filter((record) => record[0] === '6');
  const addendas = records.filter((record) => record[0] === '7');
  let totalDebitAmountInCents = 0;
  let totalCreditAmountInCents = 0;
  let eligibleEntryCount = 0;

  for (const entry of entries) {
    const code = entry.slice(1, 3);
    const amount = Number(entry.slice(29, 47));
    if (code === '32') {
      eligibleEntryCount += 1;
    }
    if (['26', '27', '28', '36', '37', '38', '55', '56', '57'].includes(code)) {
      totalDebitAmountInCents += amount;
    } else if (['21', '22', '23', '31', '32', '33', '42', '51', '52', '53'].includes(code)) {
      totalCreditAmountInCents += amount;
    } else {
      throw new Error(`TransactionCode ${code} is not classified for NACHA-M totals.`);
    }
  }

  return {
    eligibleEntryCount,
    entryAddendaCount: entries.length + addendas.length,
    entryHash: entries.reduce((sum, entry) => (sum + Number(entry.slice(3, 11))) % 10_000_000_000, 0),
    totalDebitAmountInCents,
    totalCreditAmountInCents
  };
}

function writeBatchControl(templateRecord: string, controls: {
  entryAddendaCount: number;
  entryHash: number;
  totalDebitAmountInCents: number;
  totalCreditAmountInCents: number;
}): string {
  let record = templateRecord;
  record = replaceField(record, 4, 6, controls.entryAddendaCount.toString());
  record = replaceField(record, 10, 10, controls.entryHash.toString());
  record = replaceField(record, 20, 18, controls.totalDebitAmountInCents.toString());
  record = replaceField(record, 38, 18, controls.totalCreditAmountInCents.toString());
  return record;
}

function writeFileControl(templateRecord: string, controls: {
  batchCount: number;
  blockCount: number;
  entryAddendaCount: number;
  entryHash: number;
  totalDebitAmountInCents: number;
  totalCreditAmountInCents: number;
}): string {
  let record = templateRecord;
  record = replaceField(record, 1, 6, controls.batchCount.toString());
  record = replaceField(record, 7, 6, controls.blockCount.toString());
  record = replaceField(record, 13, 8, controls.entryAddendaCount.toString());
  record = replaceField(record, 21, 10, controls.entryHash.toString());
  record = replaceField(record, 31, 18, controls.totalDebitAmountInCents.toString());
  record = replaceField(record, 49, 18, controls.totalCreditAmountInCents.toString());
  return record;
}

function replaceField(record: string, startOneBased: number, length: number, value: string): string {
  const zeroBased = startOneBased - 1;
  if (record.length !== recordLength) {
    throw new Error('Each NACHA record must be 106 ASCII characters long.');
  }
  if (zeroBased < 0 || zeroBased + length > record.length) {
    throw new Error('Field replacement exceeds the NACHA record length.');
  }
  const normalized = value.trim().padStart(length, '0').slice(-length);
  return `${record.slice(0, zeroBased)}${normalized}${record.slice(zeroBased + length)}`;
}

function locateBatchNumber(records: string[], entryIndex: number): string {
  for (let index = entryIndex; index >= 0; index--) {
    if (records[index][0] === '5') {
      return records[index].slice(91, 98);
    }
  }

  throw new Error('No batch header found for eligible entry.');
}

function findFileControlRecordIndex(records: string[]): number {
  const index = records.findIndex((record) => record[0] === '9' && !/^9{106}$/.test(record));
  if (index < 0) {
    throw new Error('No FileControl record was found before padding.');
  }
  for (const padding of records.slice(index + 1)) {
    if (!/^9{106}$/.test(padding)) {
      throw new Error('Padding after FileControl must contain only 9 characters.');
    }
  }
  return index;
}

function splitRecords(content: Buffer): string[] {
  if (content.length % recordLength !== 0) {
    throw new Error('The fixture length is not a multiple of 106 bytes.');
  }

  const records: string[] = [];
  for (let offset = 0; offset < content.length; offset += recordLength) {
    const record = content.subarray(offset, offset + recordLength).toString('ascii');
    if (record.length !== recordLength || /[^\x20-\x7E]/.test(record)) {
      throw new Error('Each NACHA record must contain exactly 106 printable ASCII characters.');
    }
    records.push(record);
  }
  return records;
}

function openZipFromBuffer(zipBytes: Buffer): Promise<yauzl.ZipFile> {
  return new Promise((resolve, reject) => {
    yauzl.fromBuffer(zipBytes, { lazyEntries: true }, (error, zipfile) => {
      if (error) {
        reject(error);
        return;
      }

      if (!zipfile) {
        reject(new Error('Unable to open the official CENIT ZIP.'));
        return;
      }

      resolve(zipfile);
    });
  });
}

function readEntryBuffer(zipfile: yauzl.ZipFile, entry: yauzl.Entry): Promise<Buffer> {
  return new Promise((resolve, reject) => {
    zipfile.openReadStream(entry, (error, stream) => {
      if (error) {
        reject(error);
        return;
      }

      if (!stream) {
        reject(new Error(`Unable to open ZIP entry ${entry.fileName}.`));
        return;
      }

      const chunks: Buffer[] = [];
      stream.on('data', (chunk: Buffer) => chunks.push(chunk));
      stream.once('end', () => resolve(Buffer.concat(chunks)));
      stream.once('error', reject);
    });
  });
}

function normalizeRequiredEntryNames(requiredEntryNames?: readonly string[]): readonly string[] {
  if (requiredEntryNames && requiredEntryNames.length > 0) {
    return [...requiredEntryNames];
  }
  return authorizedEntryNames;
}

function sha256(content: Buffer): string {
  return createHash('sha256').update(content).digest('hex').toUpperCase();
}

function assertPackageHash(actual: string, expected: string): void {
  if (actual.toLowerCase() !== expected.trim().toLowerCase()) {
    throw new Error('The SHA-256 of the CENIT package does not match the authorized value.');
  }
}

function createStoredZip(entryName: string, content: Buffer, effectiveDate: string): Buffer {
  const fileNameBytes = Buffer.from(entryName, 'utf8');
  const crc = crc32(content);
  const dosDateTime = toDosDateTime(effectiveDate);

  const localHeader = Buffer.alloc(30 + fileNameBytes.length);
  let offset = 0;
  offset = writeUInt32LE(localHeader, offset, 0x04034b50);
  offset = writeUInt16LE(localHeader, offset, 20);
  offset = writeUInt16LE(localHeader, offset, 0);
  offset = writeUInt16LE(localHeader, offset, 0);
  offset = writeUInt16LE(localHeader, offset, dosDateTime.time);
  offset = writeUInt16LE(localHeader, offset, dosDateTime.date);
  offset = writeUInt32LE(localHeader, offset, crc);
  offset = writeUInt32LE(localHeader, offset, content.length);
  offset = writeUInt32LE(localHeader, offset, content.length);
  offset = writeUInt16LE(localHeader, offset, fileNameBytes.length);
  offset = writeUInt16LE(localHeader, offset, 0);
  fileNameBytes.copy(localHeader, offset);

  const centralHeader = Buffer.alloc(46 + fileNameBytes.length);
  offset = 0;
  offset = writeUInt32LE(centralHeader, offset, 0x02014b50);
  offset = writeUInt16LE(centralHeader, offset, 20);
  offset = writeUInt16LE(centralHeader, offset, 20);
  offset = writeUInt16LE(centralHeader, offset, 0);
  offset = writeUInt16LE(centralHeader, offset, 0);
  offset = writeUInt16LE(centralHeader, offset, dosDateTime.time);
  offset = writeUInt16LE(centralHeader, offset, dosDateTime.date);
  offset = writeUInt32LE(centralHeader, offset, crc);
  offset = writeUInt32LE(centralHeader, offset, content.length);
  offset = writeUInt32LE(centralHeader, offset, content.length);
  offset = writeUInt16LE(centralHeader, offset, fileNameBytes.length);
  offset = writeUInt16LE(centralHeader, offset, 0);
  offset = writeUInt16LE(centralHeader, offset, 0);
  offset = writeUInt16LE(centralHeader, offset, 0);
  offset = writeUInt16LE(centralHeader, offset, 0);
  offset = writeUInt32LE(centralHeader, offset, 0);
  offset = writeUInt32LE(centralHeader, offset, 0);
  fileNameBytes.copy(centralHeader, offset);

  const centralDirectoryOffset = localHeader.length + content.length;
  const endRecord = Buffer.alloc(22);
  offset = 0;
  offset = writeUInt32LE(endRecord, offset, 0x06054b50);
  offset = writeUInt16LE(endRecord, offset, 0);
  offset = writeUInt16LE(endRecord, offset, 0);
  offset = writeUInt16LE(endRecord, offset, 1);
  offset = writeUInt16LE(endRecord, offset, 1);
  offset = writeUInt32LE(endRecord, offset, centralHeader.length);
  offset = writeUInt32LE(endRecord, offset, centralDirectoryOffset);
  writeUInt16LE(endRecord, offset, 0);

  return Buffer.concat([localHeader, content, centralHeader, endRecord]);
}

function writeUInt16LE(buffer: Buffer, offset: number, value: number): number {
  buffer.writeUInt16LE(value & 0xffff, offset);
  return offset + 2;
}

function writeUInt32LE(buffer: Buffer, offset: number, value: number): number {
  buffer.writeUInt32LE(value >>> 0, offset);
  return offset + 4;
}

function crc32(content: Buffer): number {
  let crc = 0xffffffff;
  for (const byte of content) {
    crc ^= byte;
    for (let bit = 0; bit < 8; bit++) {
      crc = (crc >>> 1) ^ (0xedb88320 & -(crc & 1));
    }
  }
  return (crc ^ 0xffffffff) >>> 0;
}

function toDosDateTime(effectiveDate: string): { time: number; date: number } {
  const year = Number(effectiveDate.slice(0, 4));
  const month = Number(effectiveDate.slice(4, 6));
  const day = Number(effectiveDate.slice(6, 8));
  if (!Number.isInteger(year) || !Number.isInteger(month) || !Number.isInteger(day)) {
    return { time: 0, date: 0 };
  }
  const time = 0;
  const date = ((year - 1980) << 9) | (month << 5) | day;
  return { time, date };
}
