import { createHash } from 'node:crypto';
import { readFileSync } from 'node:fs';
import path from 'node:path';

const recordLength = 106;
const batchHeaderRecordIndex = 1;
const entryDetailRecordIndex = 2;
const addendaRecordIndex = 3;
const batchControlRecordIndex = 4;
const invoiceOffset = 30;
const invoiceLength = 53;
const entrySequenceOffset = 87;
const entrySequenceLength = 15;
const batchNumberOffset = 91;
const batchNumberLength = 7;
const syntheticRecipientId = 'E2EPTXANCHOR001';

export type IncomingProcTransaccionesFixtureInput = {
  receiverAccount: string;
  receivingDfi: string;
  amount: number;
  externalOriginRouting: string;
  uniqueRunKey: string;
};

export type IncomingProcTransaccionesControls = {
  batchCount: number;
  blockCount: number;
  entryAddendaCount: number;
  entryHash: number;
  totalDebitAmountInCents: number;
  totalCreditAmountInCents: number;
};

export type IncomingProcTransaccionesFixtureFields = {
  transactionCode: string;
  receiverAccount: string;
  receivingDfi: string;
  amount: number;
  transactionTrace: string;
  batchNumber: string;
  externalOriginRouting: string;
  uniqueRunKey: string;
  operationalDate: string;
  cycleNumber: number;
  batchControls: IncomingProcTransaccionesControls;
  fileControls: IncomingProcTransaccionesControls;
};

export type IncomingProcTransaccionesFixture = IncomingProcTransaccionesFixtureFields & {
  fileName: string;
  content: Buffer;
  immediateOrigin: string;
  idTran: string;
  idTranSource: 'entryDetails.sequenceNumber';
  idLote: string;
  idLoteSource: 'batchHeaders.batchNumber';
};

export function buildIncomingProcTransaccionesFixture(
  input: IncomingProcTransaccionesFixtureInput
): IncomingProcTransaccionesFixture {
  validateInput(input);

  const golden = readFileSync(goldenFixturePath());
  const content = Buffer.from(golden);
  if (content.length !== recordLength * 10) {
    throw new Error(`El fixture NACHA-M entrante debe conservar 10 registros de ${recordLength} bytes.`);
  }

  const addendaStart = addendaRecordIndex * recordLength;
  if (content.subarray(addendaStart, addendaStart + 3).toString('ascii') !== '705') {
    throw new Error('El fixture base debe contener una addenda tipo 05 para correlacionar el flujo entrante.');
  }

  const entryStart = entryDetailRecordIndex * recordLength;
  const batchStart = batchHeaderRecordIndex * recordLength;
  const batchControlStart = batchControlRecordIndex * recordLength;
  const receivingRouting = input.receivingDfi.slice(0, 8);
  const receivingCheckDigit = input.receivingDfi.slice(8);
  const transactionTrace = deriveFifteenDigitTrace(input.uniqueRunKey, input.externalOriginRouting);
  const amountInCents = toAmountInCents(input.amount);

  writeFixed(content, batchStart + 83, 8, input.externalOriginRouting, '0');
  writeFixed(content, entryStart + 3, 8, receivingRouting, '0');
  writeFixed(content, entryStart + 11, 1, receivingCheckDigit, '0');
  writeFixed(content, entryStart + 12, 17, input.receiverAccount, ' ');
  writeFixed(content, entryStart + 29, 18, amountInCents.toString(), '0');
  writeFixed(content, entryStart + 47, 15, syntheticRecipientId, ' ');
  writeFixed(content, entryStart + entrySequenceOffset, entrySequenceLength, transactionTrace, '0');
  writeFixed(content, batchControlStart + 91, 8, input.externalOriginRouting, '0');

  content.fill(0x20, addendaStart + invoiceOffset, addendaStart + invoiceOffset + invoiceLength);
  content.write(input.uniqueRunKey, addendaStart + invoiceOffset, 'ascii');

  const calculated = calculateControls(content);
  writeBatchControl(content, calculated);
  writeFileControl(content, calculated);

  const fields = parseIncomingProcTransaccionesFixture(content, input.uniqueRunKey);
  validateDerivedFixture(fields, input, transactionTrace, calculated);

  return {
    fileName: goldenFileName(),
    content,
    ...fields,
    immediateOrigin: content.subarray(13, 23).toString('ascii').trim(),
    idTran: transactionTrace,
    idTranSource: 'entryDetails.sequenceNumber',
    idLote: fields.batchNumber,
    idLoteSource: 'batchHeaders.batchNumber'
  };
}

export function parseIncomingProcTransaccionesFixture(
  content: Buffer,
  uniqueRunKey: string
): IncomingProcTransaccionesFixtureFields {
  if (content.length !== recordLength * 10) {
    throw new Error('El contenido NACHA-M derivado debe conservar exactamente 10 registros de 106 bytes.');
  }

  const entryStart = entryDetailRecordIndex * recordLength;
  const batchStart = batchHeaderRecordIndex * recordLength;
  const controlStart = batchControlRecordIndex * recordLength;
  const addendaStart = addendaRecordIndex * recordLength;
  const fileControlIndex = findFileControlRecordIndex(content);
  const fileControlStart = fileControlIndex * recordLength;
  const transactionCode = readAscii(content, entryStart + 1, 2).trim();
  const receivingRouting = readAscii(content, entryStart + 3, 8).trim();
  const receivingCheckDigit = readAscii(content, entryStart + 11, 1).trim();
  const receiverAccount = readAscii(content, entryStart + 12, 17).trimEnd();
  const amountInCents = readNumeric(content, entryStart + 29, 18, 'monto del registro 6');
  const transactionTrace = readAscii(content, entryStart + entrySequenceOffset, entrySequenceLength);
  const batchNumber = readAscii(content, batchStart + batchNumberOffset, batchNumberLength);
  const controlBatchNumber = readAscii(content, controlStart + 99, 7);
  const externalOriginRouting = readAscii(content, batchStart + 83, 8);
  const controlOriginRouting = readAscii(content, controlStart + 91, 8);
  const parsedRunKey = readAscii(content, addendaStart + invoiceOffset, invoiceLength).trimEnd();

  if (!/^\d{15}$/.test(transactionTrace)) {
    throw new Error('EntryDetail.SequenceNumber debe contener exactamente 15 dígitos para IDTRAN.');
  }
  if (!/^\d{8}$/.test(externalOriginRouting)
    || transactionTrace.slice(0, 8) !== externalOriginRouting
    || controlOriginRouting !== externalOriginRouting) {
    throw new Error('El origen externo debe coincidir entre BatchHeader, IDTRAN y BatchControl.');
  }
  if (!/^\d{7}$/.test(batchNumber) || controlBatchNumber !== batchNumber) {
    throw new Error('BatchHeader/BatchControl deben conservar el mismo BatchNumber NACHA-M.');
  }
  if (parsedRunKey !== uniqueRunKey) {
    throw new Error('La addenda no conserva el uniqueRunKey de correlación.');
  }

  const batchControls = readControls(content, controlStart, true);
  const fileControls = readControls(content, fileControlStart, false);

  return {
    transactionCode,
    receiverAccount,
    receivingDfi: `${receivingRouting}${receivingCheckDigit}`,
    amount: amountInCents / 100,
    transactionTrace,
    batchNumber,
    externalOriginRouting,
    uniqueRunKey,
    operationalDate: readAscii(content, 23, 8),
    cycleNumber: readCycleNumber(goldenFileName()),
    batchControls,
    fileControls
  };
}

export function validateIncomingProcTransaccionesControls(
  content: Buffer,
  expected?: IncomingProcTransaccionesControls
): IncomingProcTransaccionesControls {
  const calculated = calculateControls(content);
  const parsed = parseIncomingProcTransaccionesFixture(
    content,
    readAscii(content, addendaRecordIndex * recordLength + invoiceOffset, invoiceLength).trimEnd()
  );
  const target = expected ?? calculated;

  assertControlsEqual(parsed.batchControls, target, 'BatchControl');
  assertControlsEqual(parsed.fileControls, target, 'FileControl');

  if (parsed.batchControls.batchCount !== 1 || parsed.fileControls.batchCount !== 1) {
    throw new Error('El fixture Proc_Transacciones debe conservar exactamente un lote.');
  }

  return calculated;
}

export function incomingProcTransaccionesGoldenPath(): string {
  return goldenFixturePath();
}

function validateInput(input: IncomingProcTransaccionesFixtureInput): void {
  if (!/^[A-Z0-9-]{8,40}$/.test(input.uniqueRunKey)) {
    throw new Error('uniqueRunKey debe ser alfanumérico en mayúsculas, con guiones y entre 8 y 40 caracteres.');
  }
  if (!input.receiverAccount || input.receiverAccount.length > 17 || !/^[A-Za-z0-9-]+$/.test(input.receiverAccount)) {
    throw new Error('La cuenta receptora autorizada debe tener 1-17 caracteres ASCII alfanuméricos o guion.');
  }
  if (!/^\d{9}$/.test(input.receivingDfi)) {
    throw new Error('El DFI receptor de CFA debe contener exactamente 9 dígitos, incluido el dígito de chequeo.');
  }
  if (!/^\d{8}$/.test(input.externalOriginRouting)) {
    throw new Error('El routing de la entidad externa debe contener exactamente 8 dígitos.');
  }
  toAmountInCents(input.amount);
}

function validateDerivedFixture(
  fields: IncomingProcTransaccionesFixtureFields,
  input: IncomingProcTransaccionesFixtureInput,
  transactionTrace: string,
  calculated: IncomingProcTransaccionesControls
): void {
  if (fields.transactionCode !== '22') {
    throw new Error('El fixture Proc_Transacciones debe conservar TransactionCode=22.');
  }
  if (fields.receiverAccount !== input.receiverAccount
    || fields.receivingDfi !== input.receivingDfi
    || fields.amount !== input.amount
    || fields.externalOriginRouting !== input.externalOriginRouting) {
    throw new Error('El parser E2E no recuperó exactamente cuenta, DFI, monto u origen autorizados.');
  }
  if (fields.transactionTrace !== transactionTrace || fields.batchNumber !== '0000001') {
    throw new Error('IDTRAN dinámico o IDLOTE del fixture derivado no son consistentes.');
  }
  assertControlsEqual(fields.batchControls, calculated, 'BatchControl');
  assertControlsEqual(fields.fileControls, calculated, 'FileControl');
}

function calculateControls(content: Buffer): IncomingProcTransaccionesControls {
  const records = splitRecords(content);
  const businessRecords = records.filter((record) => record[0] !== '9' || !/^9{106}$/.test(record));
  const entries = businessRecords.filter((record) => record[0] === '6');
  const addendas = businessRecords.filter((record) => record[0] === '7');
  const batchCount = businessRecords.filter((record) => record[0] === '5').length;
  const entryHash = entries.reduce((sum, entry) => (sum + Number(entry.slice(3, 11))) % 10_000_000_000, 0);
  let totalDebitAmountInCents = 0;
  let totalCreditAmountInCents = 0;
  for (const entry of entries) {
    const code = entry.slice(1, 3);
    const amount = Number(entry.slice(29, 47));
    if (['26', '27', '28', '36', '37', '38', '55', '56', '57'].includes(code)) {
      totalDebitAmountInCents += amount;
    } else if (['21', '22', '23', '31', '32', '33', '42', '51', '52', '53'].includes(code)) {
      totalCreditAmountInCents += amount;
    } else {
      throw new Error(`TransactionCode ${code} no está clasificado para totales NACHA-M.`);
    }
  }

  return {
    batchCount,
    blockCount: records.length / 10,
    entryAddendaCount: entries.length + addendas.length,
    entryHash,
    totalDebitAmountInCents,
    totalCreditAmountInCents
  };
}

function writeBatchControl(content: Buffer, controls: IncomingProcTransaccionesControls): void {
  const start = batchControlRecordIndex * recordLength;
  writeFixed(content, start + 4, 6, controls.entryAddendaCount.toString(), '0');
  writeFixed(content, start + 10, 10, controls.entryHash.toString(), '0');
  writeFixed(content, start + 20, 18, controls.totalDebitAmountInCents.toString(), '0');
  writeFixed(content, start + 38, 18, controls.totalCreditAmountInCents.toString(), '0');
}

function writeFileControl(content: Buffer, controls: IncomingProcTransaccionesControls): void {
  const start = findFileControlRecordIndex(content) * recordLength;
  writeFixed(content, start + 1, 6, controls.batchCount.toString(), '0');
  writeFixed(content, start + 7, 6, controls.blockCount.toString(), '0');
  writeFixed(content, start + 13, 8, controls.entryAddendaCount.toString(), '0');
  writeFixed(content, start + 21, 10, controls.entryHash.toString(), '0');
  writeFixed(content, start + 31, 18, controls.totalDebitAmountInCents.toString(), '0');
  writeFixed(content, start + 49, 18, controls.totalCreditAmountInCents.toString(), '0');
}

function readControls(content: Buffer, start: number, batch: boolean): IncomingProcTransaccionesControls {
  return batch
    ? {
        batchCount: 1,
        blockCount: content.length / recordLength / 10,
        entryAddendaCount: readNumeric(content, start + 4, 6, 'EntryAddendaCount batch'),
        entryHash: readNumeric(content, start + 10, 10, 'EntryHash batch'),
        totalDebitAmountInCents: readNumeric(content, start + 20, 18, 'débito batch'),
        totalCreditAmountInCents: readNumeric(content, start + 38, 18, 'crédito batch')
      }
    : {
        batchCount: readNumeric(content, start + 1, 6, 'BatchCount file'),
        blockCount: readNumeric(content, start + 7, 6, 'BlockCount file'),
        entryAddendaCount: readNumeric(content, start + 13, 8, 'EntryAddendaCount file'),
        entryHash: readNumeric(content, start + 21, 10, 'EntryHash file'),
        totalDebitAmountInCents: readNumeric(content, start + 31, 18, 'débito file'),
        totalCreditAmountInCents: readNumeric(content, start + 49, 18, 'crédito file')
      };
}

function assertControlsEqual(
  actual: IncomingProcTransaccionesControls,
  expected: IncomingProcTransaccionesControls,
  label: string
): void {
  for (const key of Object.keys(expected) as (keyof IncomingProcTransaccionesControls)[]) {
    if (actual[key] !== expected[key]) {
      throw new Error(`${label}.${key}=${actual[key]} no coincide con el valor calculado ${expected[key]}.`);
    }
  }
}

function findFileControlRecordIndex(content: Buffer): number {
  const records = splitRecords(content);
  const index = records.findIndex((record, position) => position > batchControlRecordIndex
    && record[0] === '9'
    && !/^9{106}$/.test(record));
  if (index < 0) {
    throw new Error('No se encontró el FileControl tipo 9 antes del padding.');
  }
  for (const padding of records.slice(index + 1)) {
    if (!/^9{106}$/.test(padding)) {
      throw new Error('El padding posterior al FileControl debe contener sólo caracteres 9.');
    }
  }
  return index;
}

function splitRecords(content: Buffer): string[] {
  if (content.length % recordLength !== 0) {
    throw new Error('La longitud del fixture no es múltiplo de 106 bytes.');
  }
  const records: string[] = [];
  for (let offset = 0; offset < content.length; offset += recordLength) {
    const record = content.subarray(offset, offset + recordLength).toString('ascii');
    if (record.length !== recordLength || /[^\x20-\x7E]/.test(record)) {
      throw new Error('Cada registro debe contener exactamente 106 caracteres ASCII imprimibles.');
    }
    records.push(record);
  }
  return records;
}

function deriveFifteenDigitTrace(uniqueRunKey: string, externalOriginRouting: string): string {
  const digest = createHash('sha256').update(uniqueRunKey, 'utf8').digest();
  const consecutive = (digest.readUInt32BE(0) % 6_999_999) + 1;
  return `${externalOriginRouting}${consecutive.toString().padStart(7, '0')}`;
}

function toAmountInCents(amount: number): number {
  if (!Number.isFinite(amount) || amount <= 0) {
    throw new Error('TransactionCode=22 requiere un monto autorizado mayor que cero.');
  }
  const rawCents = amount * 100;
  const cents = Math.round(rawCents);
  if (!Number.isSafeInteger(cents) || Math.abs(rawCents - cents) > 1e-7) {
    throw new Error('El monto autorizado debe tener máximo dos decimales y caber de forma exacta en centavos.');
  }
  if (cents.toString().length > 18) {
    throw new Error('El monto autorizado excede el campo NACHA-M de 18 dígitos.');
  }
  return cents;
}

function writeFixed(content: Buffer, offset: number, length: number, value: string, pad: '0' | ' '): void {
  if (value.length > length) {
    throw new Error(`El valor de longitud ${value.length} excede el campo fixed-width de ${length}.`);
  }
  const rendered = pad === '0' ? value.padStart(length, '0') : value.padEnd(length, ' ');
  content.write(rendered, offset, length, 'ascii');
}

function readNumeric(content: Buffer, offset: number, length: number, label: string): number {
  const value = readAscii(content, offset, length);
  if (!/^\d+$/.test(value)) {
    throw new Error(`${label} debe contener únicamente dígitos.`);
  }
  const parsed = Number(value);
  if (!Number.isSafeInteger(parsed)) {
    throw new Error(`${label} excede la precisión segura del preflight E2E.`);
  }
  return parsed;
}

function readAscii(content: Buffer, offset: number, length: number): string {
  return content.subarray(offset, offset + length).toString('ascii');
}

function readCycleNumber(fileName: string): number {
  const segment = path.basename(fileName).split('.').at(-1) ?? '';
  const parsed = Number(segment);
  if (!Number.isInteger(parsed) || parsed <= 0) {
    throw new Error(`No se pudo resolver el ciclo desde ${fileName}.`);
  }
  return parsed;
}

function goldenFileName(): string {
  return '0001283.001.6';
}

function goldenFixturePath(): string {
  return path.resolve(
    __dirname,
    '../../../../tests/Cfa.ACHInterbank.Tests/TestData/Nacha/GoldenFiles/ACHColombia/Incoming/ACH_COL_IN_001.ach'
  );
}
