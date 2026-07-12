import { readFileSync } from 'node:fs';
import path from 'node:path';

const recordLength = 106;
const addendaRecordIndex = 3;
const invoiceOffset = 30;
const invoiceLength = 53;

export type IncomingProcTransaccionesFixture = {
  fileName: string;
  content: Buffer;
  uniqueRunKey: string;
  transactionCode: '22';
  transactionTrace: string;
  batchId: string;
  immediateOrigin: string;
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

  content.fill(0x20, addendaStart + invoiceOffset, addendaStart + invoiceOffset + invoiceLength);
  content.write(uniqueRunKey, addendaStart + invoiceOffset, 'ascii');

  return {
    fileName: '0001283.001.6',
    content,
    uniqueRunKey,
    transactionCode: '22',
    transactionTrace: '123456780000001',
    batchId: '123456780000001',
    immediateOrigin: content.subarray(13, 23).toString('ascii').trim()
  };
}

function goldenFixturePath(): string {
  return path.resolve(
    __dirname,
    '../../../../tests/Cfa.ACHInterbank.Tests/TestData/Nacha/GoldenFiles/ACHColombia/Incoming/ACH_COL_IN_001.ach'
  );
}
