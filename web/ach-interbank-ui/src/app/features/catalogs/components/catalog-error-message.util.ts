const technicalMessagePattern =
  /\b(?:exception|stack\s*trace|entityframework|sqlstate|select\s+.+\s+from|insert\s+into|update\s+.+\s+set|system\.[a-z]|at\s+[a-z0-9_.]+\(|https?:\/\/)\b/i;
const sensitiveMessagePattern =
  /\b(?:bearer|password|passwd|contrase(?:ñ|n)a|access[_ -]?token|refresh[_ -]?token|authorization|secret|api[_ -]?key|eyJ[a-z0-9_-]{8,}\.)\b/i;

export function catalogErrorMessage(error: unknown, fallback: string): string {
  const candidates = extractCandidates(error);

  for (const candidate of candidates) {
    const normalized = candidate
      .replace(/[\u0000-\u001f\u007f]+/g, ' ')
      .replace(/\s+/g, ' ')
      .trim();

    if (
      normalized
      && normalized.length <= 220
      && normalized !== '[object Object]'
      && !technicalMessagePattern.test(normalized)
      && !sensitiveMessagePattern.test(normalized)
    ) {
      return normalized;
    }
  }

  return fallback;
}

function extractCandidates(error: unknown): string[] {
  if (typeof error === 'string') {
    return [error];
  }

  if (!error || typeof error !== 'object') {
    return [];
  }

  const candidate = error as {
    error?: string | { detail?: string; message?: string; title?: string };
    message?: string;
  };

  if (typeof candidate.error === 'string') {
    return [candidate.error, candidate.message ?? ''];
  }

  return [
    candidate.error?.detail ?? '',
    candidate.error?.message ?? '',
    candidate.error?.title ?? '',
    candidate.message ?? ''
  ];
}
