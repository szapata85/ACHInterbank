export interface ReportOption<T extends string = string> {
  value: T | '';
  label: string;
}

export const REPORT_STATE_OPTIONS: ReadonlyArray<ReportOption> = [
  { value: '', label: 'Todos los estados' },
  { value: 'Pending', label: 'Pendiente' },
  { value: 'ReturnedByOperator', label: 'Devuelta por el operador' },
  { value: 'ReturnedByEpr', label: 'Devuelta por la entidad receptora' },
  { value: 'AppliedTacitly', label: 'Aplicada tácitamente' },
  { value: 'Certified', label: 'Certificada' }
];

export const REPORT_SOURCE_OPTIONS: ReadonlyArray<ReportOption> = [
  { value: '', label: 'Todas las fuentes' },
  { value: 'Operator', label: 'Operador' },
  { value: 'Epr', label: 'Entidad receptora' },
  { value: 'System', label: 'Sistema' },
  { value: 'Claims', label: 'Reclamaciones' }
];

const LABELS = new Map<string, string>([
  ...REPORT_STATE_OPTIONS.filter((option) => option.value).map((option) => [option.value, option.label] as const),
  ...REPORT_SOURCE_OPTIONS.filter((option) => option.value).map((option) => [option.value, option.label] as const),
  ['Open', 'Abierto'],
  ['Closed', 'Cerrado'],
  ['Completed', 'Completado'],
  ['Failed', 'Fallido'],
  ['Processing', 'En procesamiento'],
  ['Created', 'Creación'],
  ['Updated', 'Actualización'],
  ['Deleted', 'Eliminación']
]);

const COLOMBIA_DATE_TIME = new Intl.DateTimeFormat('es-CO', {
  dateStyle: 'medium',
  timeStyle: 'short',
  timeZone: 'America/Bogota'
});

const COLOMBIA_DATE = new Intl.DateTimeFormat('es-CO', { dateStyle: 'medium', timeZone: 'UTC' });

const COLOMBIA_CURRENCY = new Intl.NumberFormat('es-CO', {
  style: 'currency',
  currency: 'COP',
  minimumFractionDigits: 2
});

export function humanizeReportValue(value: unknown): string {
  if (value === null || value === undefined || value === '') {
    return 'Sin información';
  }

  const raw = String(value);
  return LABELS.get(raw) ?? raw.replace(/([a-záéíóúñ])([A-ZÁÉÍÓÚÑ])/g, '$1 $2');
}

export function formatReportValue(key: string, value: unknown): unknown {
  if (value === null || value === undefined || value === '') {
    return 'Sin información';
  }

  if (/amount/i.test(key) && typeof value === 'number') {
    return COLOMBIA_CURRENCY.format(value);
  }

  if (/(date|utc|at)$/i.test(key)) {
    const raw = String(value);
    const dateOnly = /^\d{4}-\d{2}-\d{2}$/.test(raw);
    const date = new Date(dateOnly ? `${raw}T00:00:00Z` : raw);
    if (!Number.isNaN(date.getTime())) {
      return dateOnly ? COLOMBIA_DATE.format(date) : COLOMBIA_DATE_TIME.format(date);
    }
  }

  if (/(state|status|source|action)$/i.test(key)) {
    return humanizeReportValue(value);
  }

  return value;
}

export async function validatePdfBlob(blob: Blob, contentType: string | null): Promise<string | null> {
  if (blob.size === 0) {
    return 'No encontramos información para incluir en el reporte.';
  }

  if (!(contentType ?? blob.type ?? '').toLowerCase().includes('application/pdf')) {
    return 'El archivo recibido no tiene el formato PDF esperado.';
  }

  const header = await blob.slice(0, 5).text().catch(() => '');
  return header === '%PDF-' ? null : 'El archivo recibido no es un PDF válido.';
}

export function extractReportFileName(contentDisposition: string | null, fallback: string): string {
  const match = contentDisposition
    ? /filename\*=UTF-8''([^;]+)|filename="?([^";]+)"?/i.exec(contentDisposition)
    : null;
  const encoded = match?.[1] ?? match?.[2];
  return encoded ? decodeURIComponent(encoded) : fallback;
}
