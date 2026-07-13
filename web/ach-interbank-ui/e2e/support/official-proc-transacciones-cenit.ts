import { createHash } from 'node:crypto';
import { readFile } from 'node:fs/promises';
import * as yauzl from 'yauzl';

const recordLength = 106;
const authorizedEntryNames = [
  '0001283.001.20260713.1',
  '0001283.002.20260713.1',
  '0001283.003.20260713.1',
  '0001283.004.20260713.1',
  '0001283.005.20260713.1'
] as const;

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
  batchNumber: string;
  entryIndex: number;
  transactionCode: string;
  receiverAccount: string;
  amount: number;
  idTran: string;
  idLote: string;
};

export type IngestionCandidate = {
  fileName: string;
  uploadedAtUtc: Date | string;
  correlationId: string;
};

export type OfficialProcTransaccionesArchiveInventory = {
  packageSha256: string;
  entries: OfficialProcTransaccionesEntryInventory[];
};

export type OfficialProcTransaccionesSelection = OfficialProcTransaccionesArchiveInventory & {
  selectedEntryName: string;
  selectedBytes: Buffer;
  selectedEntry: OfficialProcTransaccionesEntryInventory;
};

export function assertAuthorizedOfficialProcTransaccionesEntryName(entryName: string): string {
  const normalized = entryName.trim();
  if (!normalized) {
    throw new Error('CENIT_TEST_ENTRY_NAME es obligatoria y no admite fallback.');
  }

  if (!authorizedEntryNames.includes(normalized as typeof authorizedEntryNames[number])) {
    throw new Error(`CENIT_TEST_ENTRY_NAME debe ser uno de los nombres autorizados: ${authorizedEntryNames.join(', ')}.`);
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
    throw new Error('No se encontro una ingestiÃ³n NACHA candidata en la ventana solicitada.');
  }

  if (matches.length > 1) {
    throw new Error('La correlaciÃ³n por FileName y ventana devolviÃ³ mÃ¡s de una ingestiÃ³n.');
  }

  return matches[0];
}

export async function loadOfficialProcTransaccionesArchiveInventory(
  options: OfficialProcTransaccionesArchiveOptions
): Promise<OfficialProcTransaccionesArchiveInventory> {
  const zipBytes = await readFile(options.packagePath);
  const packageSha256 = sha256(zipBytes);
  assertPackageHash(packageSha256, options.expectedPackageSha256);

  const entries = await readOfficialZipEntriesFromBuffer(zipBytes);
  return buildOfficialArchiveInventory(entries, packageSha256);
}

export async function selectOfficialProcTransaccionesEntry(
  options: OfficialProcTransaccionesArchiveOptions & { selectedEntryName: string }
): Promise<OfficialProcTransaccionesSelection> {
  const selectedEntryName = assertAuthorizedOfficialProcTransaccionesEntryName(options.selectedEntryName);
  const zipBytes = await readFile(options.packagePath);
  const packageSha256 = sha256(zipBytes);
  assertPackageHash(packageSha256, options.expectedPackageSha256);

  const zipEntries = await readOfficialZipEntriesFromBuffer(zipBytes);
  const archive = buildOfficialArchiveInventory(zipEntries, packageSha256);
  const matchingEntries = zipEntries.filter((entry) => entry.fileName === selectedEntryName);

  if (matchingEntries.length === 0) {
    throw new Error(`La entrada autorizada ${selectedEntryName} no existe exactamente en el ZIP.`);
  }

  if (matchingEntries.length > 1) {
    throw new Error(`La entrada autorizada ${selectedEntryName} aparece más de una vez en el ZIP.`);
  }

  const selected = matchingEntries[0];
  const selectedEntry = analyzeOfficialEntry(selected.fileName, selected.buffer);
  if (selectedEntry.sha256 !== selected.sha256) {
    throw new Error(`La entrada ${selectedEntryName} cambió durante la extracción.`);
  }

  return {
    ...archive,
    selectedEntryName,
    selectedBytes: Buffer.from(selected.buffer),
    selectedEntry
  };
}

export function findOfficialProcTransaccionesEligibleEntries(
  fileName: string,
  fileBytes: Buffer,
  receiverAccount: string,
  expectedAmount: number
): OfficialProcTransaccionesEligibleEntry[] {
  const records = splitRecords(fileBytes);
  if (!records) {
    return [];
  }

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

    const batchNumber = locateBatchNumber(records, index);
    const idTran = record.slice(87, 102);
    eligible.push({
      fileName,
      batchNumber,
      entryIndex: index,
      transactionCode,
      receiverAccount: entryAccount,
      amount,
      idTran,
      idLote: batchNumber
    });
  }

  return eligible;
}

function analyzeOfficialEntry(fileName: string, buffer: Buffer): OfficialProcTransaccionesEntryInventory {
  const fixedLengthValid = buffer.length % recordLength === 0;
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

  const records = splitRecords(buffer);
  if (!records) {
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

function buildOfficialArchiveInventory(
  entries: Array<{ fileName: string; buffer: Buffer; sha256: string }>,
  packageSha256: string
): OfficialProcTransaccionesArchiveInventory {
  const duplicates = authorizedEntryNames.filter((entryName) =>
    entries.filter((entry) => entry.fileName === entryName).length > 1);
  if (duplicates.length > 0) {
    throw new Error(`El paquete CENIT contiene entradas duplicadas: ${duplicates.join(', ')}.`);
  }

  const inventory = entries
    .filter((entry) => authorizedEntryNames.includes(entry.fileName as typeof authorizedEntryNames[number]))
    .map((entry) => analyzeOfficialEntry(entry.fileName, entry.buffer));

  const missing = authorizedEntryNames.filter((name) => !inventory.some((entry) => entry.fileName === name));
  if (missing.length > 0) {
    throw new Error(`El paquete CENIT no contiene las entradas autorizadas requeridas: ${missing.join(', ')}.`);
  }

  return {
    packageSha256,
    entries: inventory.sort((left, right) => authorizedEntryNames.indexOf(left.fileName as typeof authorizedEntryNames[number])
      - authorizedEntryNames.indexOf(right.fileName as typeof authorizedEntryNames[number]))
  };
}

async function readOfficialZipEntriesFromBuffer(zipBytes: Buffer): Promise<Array<{ fileName: string; buffer: Buffer; sha256: string }>> {
  const zipfile = await openZipFromBuffer(zipBytes);
  const entries: Array<{ fileName: string; buffer: Buffer; sha256: string }> = [];

  await new Promise<void>((resolve, reject) => {
    zipfile.on('entry', (entry) => {
      if (!authorizedEntryNames.includes(entry.fileName as typeof authorizedEntryNames[number])) {
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

function splitRecords(buffer: Buffer): string[] | null {
  if (buffer.length % recordLength !== 0) {
    return null;
  }

  const records: string[] = [];
  for (let offset = 0; offset < buffer.length; offset += recordLength) {
    records.push(buffer.subarray(offset, offset + recordLength).toString('ascii'));
  }

  return records;
}

function locateBatchNumber(records: string[], entryIndex: number): string {
  for (let index = entryIndex; index >= 0; index--) {
    if (records[index][0] === '5') {
      return records[index].slice(91, 98);
    }
  }

  throw new Error('No se pudo resolver el BatchNumber para la entrada elegible.');
}

function openZipFromBuffer(zipBytes: Buffer): Promise<yauzl.ZipFile> {
  return new Promise((resolve, reject) => {
    yauzl.fromBuffer(zipBytes, { lazyEntries: true }, (error, zipfile) => {
      if (error) {
        reject(error);
        return;
      }

      if (!zipfile) {
        reject(new Error('No se pudo abrir el ZIP oficial de CENIT.'));
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
        reject(new Error(`No se pudo abrir la entrada ${entry.fileName}.`));
        return;
      }

      const chunks: Buffer[] = [];
      stream.on('data', (chunk: Buffer) => chunks.push(chunk));
      stream.once('end', () => resolve(Buffer.concat(chunks)));
      stream.once('error', reject);
    });
  });
}

function sha256(content: Buffer): string {
  return createHash('sha256').update(content).digest('hex').toUpperCase();
}

function assertPackageHash(actual: string, expected: string): void {
  if (actual.toLowerCase() !== expected.trim().toLowerCase()) {
    throw new Error('El SHA-256 del paquete CENIT no coincide con el valor autorizado.');
  }
}
