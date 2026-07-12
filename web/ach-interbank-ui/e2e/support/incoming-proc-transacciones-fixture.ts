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

export type IncomingProcTransaccionesFixtureFields = {
  transactionCode: string;
  receiverAccount: string;
  receivingDfi: string;
  amount: number;
  transactionTrace: string;
  batchNumber: string;
  uniqueRunKey: string;
};

export type IncomingProcTransaccionesFixture = IncomingProcTransaccionesFixtureFields & {
  fileName: string;
  content: Buffer;
  immediateOrigin: string;
  idTran: string;
  idTranSource: 'entryDetails.sequenceNumber';
  idLote: string;
  idLoteSource: 'batchHeaders.batchNumber';
  liveBlockedReason: string;
};

export function buildIncomingProcTransaccionesFixture(uniqueRunKey: string): IncomingProcTransaccionesFixture {
  if (!/^[A-Z0-9-]{8,40}$/.test(uniqueRunKey)) {
    throw new Error('uniqueRunKey debe ser alfanumerico en mayusculas, con guiones y entre 8 y 40 caracteres.');
  }

  const content = Buffer.from(readFileSync(goldenFixturePath()));
  if (content.length !== recordLength * 10) {
    throw new Error(`El fixture NACHA-M entrante debe conservar 10 registros de ${recordLength} bytes.`);
  }

  const addendaStart = addendaRecordIndex * recordLength;
  if (content.subarray(addendaStart, addendaStart + 3).toString('ascii') !== '705') {
    throw new Error('El fixture base debe contener una addenda tipo 05 para correlacionar el flujo entrante.');
  }

  const transactionTrace = deriveFifteenDigitTrace(uniqueRunKey);
  content.write(transactionTrace, entryDetailRecordIndex * recordLength + entrySequenceOffset, entrySequenceLength, 'ascii');
  content.fill(0x20, addendaStart + invoiceOffset, addendaStart + invoiceOffset + invoiceLength);
  content.write(uniqueRunKey, addendaStart + invoiceOffset, 'ascii');

  const fields = parseIncomingProcTransaccionesFixture(content, uniqueRunKey);
  if (fields.transactionTrace !== transactionTrace) {
    throw new Error('El parser compatible NACHA-M no recupero el IDTRAN dinamico escrito en el registro 6.');
  }
  if (fields.batchNumber !== '0000001') {
    throw new Error('El fixture de un solo lote debe conservar BatchHeader.BatchNumber=0000001 y BatchControl consistente.');
  }

  return {
    fileName: '0001283.001.6',
    content,
    ...fields,
    immediateOrigin: content.subarray(13, 23).toString('ascii').trim(),
    idTran: transactionTrace,
    idTranSource: 'entryDetails.sequenceNumber',
    idLote: fields.batchNumber,
    idLoteSource: 'batchHeaders.batchNumber',
    liveBlockedReason: 'IDLOTE usa BatchHeader.BatchNumber; el parser exige 0000001 para el unico lote del fixture. No habilitar LIVE hasta disponer de una estrategia de lote idempotente aprobada.'
  };
}

export function parseIncomingProcTransaccionesFixture(content: Buffer, uniqueRunKey: string): IncomingProcTransaccionesFixtureFields {
  if (content.length < recordLength * 5) {
    throw new Error('Contenido NACHA-M insuficiente para leer registros 5, 6, 7 y 8.');
  }

  const entryStart = entryDetailRecordIndex * recordLength;
  const batchStart = batchHeaderRecordIndex * recordLength;
  const controlStart = batchControlRecordIndex * recordLength;
  const addendaStart = addendaRecordIndex * recordLength;
  const transactionCode = content.subarray(entryStart + 1, entryStart + 3).toString('ascii').trim();
  const receivingDfi = content.subarray(entryStart + 3, entryStart + 12).toString('ascii').trim();
  const receiverAccount = content.subarray(entryStart + 12, entryStart + 29).toString('ascii').trimEnd();
  const amountInCents = content.subarray(entryStart + 29, entryStart + 47).toString('ascii');
  const transactionTrace = content.subarray(entryStart + entrySequenceOffset, entryStart + entrySequenceOffset + entrySequenceLength).toString('ascii');
  const batchNumber = content.subarray(batchStart + batchNumberOffset, batchStart + batchNumberOffset + batchNumberLength).toString('ascii');
  const controlBatchNumber = content.subarray(controlStart + 99, controlStart + 106).toString('ascii');
  const parsedRunKey = content.subarray(addendaStart + invoiceOffset, addendaStart + invoiceOffset + invoiceLength).toString('ascii').trimEnd();

  if (!/^\d{15}$/.test(transactionTrace)) {
    throw new Error('EntryDetail.SequenceNumber debe contener exactamente 15 digitos para IDTRAN.');
  }
  if (!/^\d{7}$/.test(batchNumber) || controlBatchNumber !== batchNumber) {
    throw new Error('BatchHeader/BatchControl deben conservar el mismo BatchNumber NACHA-M.');
  }
  if (parsedRunKey !== uniqueRunKey) {
    throw new Error('La addenda no conserva el uniqueRunKey de correlacion.');
  }

  return {
    transactionCode,
    receiverAccount,
    receivingDfi,
    amount: Number(amountInCents) / 100,
    transactionTrace,
    batchNumber,
    uniqueRunKey
  };
}

function deriveFifteenDigitTrace(uniqueRunKey: string): string {
  const digest = createHash('sha256').update(uniqueRunKey, 'utf8').digest('hex').slice(0, 16);
  const value = BigInt(`0x${digest}`) % 1_000_000_000_000_000n;
  return value.toString().padStart(entrySequenceLength, '0');
}

function goldenFixturePath(): string {
  return path.resolve(
    __dirname,
    '../../../../tests/Cfa.ACHInterbank.Tests/TestData/Nacha/GoldenFiles/ACHColombia/Incoming/ACH_COL_IN_001.ach'
  );
}
