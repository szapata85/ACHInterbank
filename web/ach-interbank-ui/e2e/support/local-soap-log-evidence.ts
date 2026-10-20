import { existsSync, readdirSync, readFileSync, statSync } from 'node:fs';
import path from 'node:path';

export type SoapLogDirectorySnapshot = Map<string, { size: number; mtimeMs: number }>;

export type LocalSoapLogEvidence = {
  source: string;
  text: string;
};

export function snapshotSoapLogDirectory(directory: string): SoapLogDirectorySnapshot {
  assertLogDirectory(directory);
  return new Map(listLogFiles(directory).map((filePath) => {
    const stat = statSync(filePath);
    return [filePath, { size: stat.size, mtimeMs: stat.mtimeMs }];
  }));
}

export function findProcTransaccionesLogEvidence(
  directory: string,
  baseline: SoapLogDirectorySnapshot,
  startedAt: Date,
  correlationTokens: readonly string[]
): LocalSoapLogEvidence {
  if (correlationTokens.length === 0 || correlationTokens.some((token) => !token.trim())) {
    throw new Error('La evidencia SOAP requiere uno o más tokens de correlación confirmados en RequestPayloadXml.');
  }
  assertLogDirectory(directory);
  const candidates = listLogFiles(directory)
    .map((filePath) => ({ filePath, before: baseline.get(filePath), after: statSync(filePath) }))
    .filter(({ before, after }) => !before || after.size > before.size)
    .sort((a, b) => b.after.mtimeMs - a.after.mtimeMs);

  for (const candidate of candidates) {
    const text = readNewEvidence(candidate.filePath, candidate.before?.size ?? 0);
    const block = findCorrelatedBlock(text, correlationTokens);
    if (block) {
      return { source: candidate.filePath, text: block };
    }
  }

  void startedAt;
  throw new Error(`No se encontro evidencia nueva de Proc_Transacciones correlacionada con los tokens confirmados en ${directory}.`);
}

function assertLogDirectory(directory: string): void {
  if (!directory || !existsSync(directory)) {
    throw new Error(`SOAP_LOCAL_LOG_DIR debe existir para validar evidencia SOAP local: ${directory || '<vacio>'}.`);
  }
}

function listLogFiles(directory: string): string[] {
  return readdirSync(directory)
    .map((name) => path.join(directory, name))
    .filter((filePath) => statSync(filePath).isFile() && /\.(log|txt|xml)$/i.test(filePath));
}

function readNewEvidence(filePath: string, offset: number): string {
  const content = readFileSync(filePath);
  return content.subarray(offset).toString('utf8');
}

function findCorrelatedBlock(text: string, correlationTokens: readonly string[]): string | null {
  const tokenIndex = correlationTokens
    .map((token) => text.lastIndexOf(token))
    .find((index) => index >= 0);
  if (tokenIndex === undefined) {
    return null;
  }

  const start = Math.max(
    text.lastIndexOf('<soap:Envelope', tokenIndex),
    text.lastIndexOf('<s:Envelope', tokenIndex),
    text.lastIndexOf('<Envelope', tokenIndex),
    text.lastIndexOf('Proc_Transacciones', tokenIndex)
  );
  const envelopeEnd = ['</soap:Envelope>', '</s:Envelope>', '</Envelope>']
    .map((tag) => ({ tag, index: text.indexOf(tag, tokenIndex) }))
    .filter((candidate) => candidate.index >= 0)
    .sort((left, right) => left.index - right.index)[0];
  const end = envelopeEnd
    ? envelopeEnd.index + envelopeEnd.tag.length
    : Math.min(text.length, tokenIndex + 8_000);
  const block = start >= 0
    ? text.slice(start, end)
    : text.slice(Math.max(0, tokenIndex - 8_000), end);
  return block.includes('Proc_Transacciones') ? block : null;
}
