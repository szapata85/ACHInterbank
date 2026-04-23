const CONTROL_CHARS_REGEX = /[\u0000-\u001f\u007f]/g;
const RESERVED_WINDOWS_NAMES = /^(con|prn|aux|nul|com[1-9]|lpt[1-9])(?:\..*)?$/i;

export function sanitizeDownloadFileName(fileName: string | null | undefined, fallback: string): string {
  const safeFallback = sanitizeCandidate(fallback, 'archivo_seguro.dat', false);
  const candidate = sanitizeCandidate(fileName, safeFallback, true);
  return candidate || safeFallback;
}

function sanitizeCandidate(value: string | null | undefined, fallback: string, enforceExpectedExt: boolean): string {
  const raw = (value ?? '').trim();
  if (!raw) {
    return fallback;
  }

  let normalized = raw
    .replace(/\.\.\//g, '')
    .replace(/\.\.\\/g, '')
    .replace(/[\\/]/g, '_')
    .replace(CONTROL_CHARS_REGEX, '')
    .replace(/[:*?"<>|]/g, '_')
    .replace(/\s+/g, ' ')
    .trim();

  if (!normalized || normalized === '.' || normalized === '..' || normalized.startsWith('.')) {
    return fallback;
  }

  if (RESERVED_WINDOWS_NAMES.test(normalized)) {
    return fallback;
  }

  normalized = normalized.slice(0, 120).trim();

  if (!normalized) {
    return fallback;
  }

  if (enforceExpectedExt) {
    const fallbackExt = extensionOf(fallback);
    if (fallbackExt) {
      const currentExt = extensionOf(normalized);
      if (!currentExt || currentExt !== fallbackExt) {
        normalized = `${stripExtension(normalized)}.${fallbackExt}`;
      }
    }
  }

  if (!normalized || normalized.startsWith('.')) {
    return fallback;
  }

  return normalized;
}

function extensionOf(fileName: string): string {
  const idx = fileName.lastIndexOf('.');
  if (idx <= 0 || idx === fileName.length - 1) {
    return '';
  }

  return fileName.slice(idx + 1).toLowerCase();
}

function stripExtension(fileName: string): string {
  const idx = fileName.lastIndexOf('.');
  if (idx <= 0) {
    return fileName;
  }

  return fileName.slice(0, idx);
}
