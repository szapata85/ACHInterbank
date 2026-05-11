export function formatAchValue(value: unknown): string {
  if (value === null || value === undefined || value === '') return '-';
  if (typeof value === 'boolean') return value ? 'Sí' : 'No';
  return String(value);
}

export function formatAchBoolean(value: boolean | null | undefined): string {
  if (value === true) return 'Sí';
  if (value === false) return 'No';
  return '-';
}

export function formatAchDate(value: string | null | undefined): string {
  if (!value) return '-';
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? value : date.toLocaleString('es-CO');
}

export function normalizeAchFilter(value: string | null | undefined): string | undefined {
  const trimmed = value?.trim();
  return trimmed ? trimmed : undefined;
}

export function calculateAchRate(value: number, total: number): string {
  if (total <= 0) return '0%';
  const rate = (value / total) * 100;
  const rounded = Math.round(rate * 10) / 10;
  return `${rounded % 1 === 0 ? rounded.toFixed(0) : rounded.toFixed(1)}%`;
}
