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
  uniqueRunKey: string
): LocalSoapLogEvidence {
  assertLogDirectory(directory);
  const candidates = listLogFiles(directory)
    .map((filePath) => ({ filePath, before: baseline.get(filePath), after: statSync(filePath) }))
    .filter(({ before, after }) => after.mtimeMs >= startedAt.getTime() && (!before || after.size > before.size))
    .sort((a, b) => b.after.mtimeMs - a.after.mtimeMs);

  for (const candidate of candidates) {
    const text = readNewEvidence(candidate.filePath, candidate.before?.size ?? 0);
    if (text.includes('Proc_Transacciones') && text.includes(uniqueRunKey)) {
      return { source: candidate.filePath, text };
    }
  }

  throw new Error(`No se encontro evidencia nueva de Proc_Transacciones correlacionada con ${uniqueRunKey} en ${directory}.`);
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
