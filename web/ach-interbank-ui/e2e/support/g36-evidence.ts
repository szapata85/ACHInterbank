import { TestInfo } from '@playwright/test';
import { mkdir, writeFile } from 'node:fs/promises';
import path from 'node:path';

const sensitiveKey = /(password|secret|token|credential|account|payloadxml|requestxml|responsexml)/i;

export async function attachG36Evidence(
  testInfo: TestInfo,
  baseName: string,
  evidence: Record<string, unknown>
): Promise<void> {
  const sanitized = sanitize(evidence) as Record<string, unknown>;
  const jsonPath = testInfo.outputPath(`${baseName}.json`);
  const markdownPath = testInfo.outputPath(`${baseName}.md`);
  await mkdir(path.dirname(jsonPath), { recursive: true });
  await writeFile(jsonPath, JSON.stringify(sanitized, null, 2), 'utf8');
  await writeFile(markdownPath, toMarkdown(baseName, sanitized), 'utf8');
  await testInfo.attach(`${baseName}.json`, { path: jsonPath, contentType: 'application/json' });
  await testInfo.attach(`${baseName}.md`, { path: markdownPath, contentType: 'text/markdown' });
}

function sanitize(value: unknown, key = ''): unknown {
  if (value === null || value === undefined) {
    return value;
  }
  if (sensitiveKey.test(key)) {
    if (typeof value === 'string') {
      return value.length > 0 ? `[REDACTED:${value.length}]` : '';
    }
    return '[REDACTED]';
  }
  if (value instanceof Date) {
    return value.toISOString();
  }
  if (Array.isArray(value)) {
    return value.map((item) => sanitize(item));
  }
  if (typeof value === 'object') {
    return Object.fromEntries(
      Object.entries(value as Record<string, unknown>).map(([childKey, childValue]) => [
        childKey,
        sanitize(childValue, childKey)
      ])
    );
  }
  return value;
}

function toMarkdown(title: string, evidence: Record<string, unknown>): string {
  const lines = [
    `# ${title}`,
    '',
    `- Generado: ${new Date().toISOString()}`,
    '- Productivo: NO-GO',
    '- SOAP real: no invocado',
    '- Datos: sinteticos UAT',
    '',
    '```json',
    JSON.stringify(evidence, null, 2),
    '```',
    ''
  ];
  return lines.join('\n');
}
