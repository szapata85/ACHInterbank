import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

export const auditDateRangeValidator: ValidatorFn = (
  control: AbstractControl
): ValidationErrors | null => {
  const startDate = control.get('startDate')?.value;
  const endDate = control.get('endDate')?.value;

  if (!isValidDate(startDate) || !isValidDate(endDate)) {
    return null;
  }

  return startDate.getTime() <= endDate.getTime() ? null : { dateRange: true };
};

export function toLocalDateParam(value: Date | null): string | null {
  if (!isValidDate(value)) {
    return null;
  }

  const year = value.getFullYear();
  const month = String(value.getMonth() + 1).padStart(2, '0');
  const day = String(value.getDate()).padStart(2, '0');
  return `${year}-${month}-${day}`;
}

function isValidDate(value: unknown): value is Date {
  return value instanceof Date && !Number.isNaN(value.getTime());
}

const secretAssignmentPattern =
  /((?:"|')?(?:password|passwd|pwd|contrase(?:ñ|n)a|access[_ -]?token|refresh[_ -]?token|token|authorization|secret|client[_ -]?secret|api[_ -]?key)(?:"|')?\s*[:=]\s*)(?:"[^"]*"|'[^']*'|[^,;\s}\]]+)/gi;
const bearerPattern = /\bbearer\s+[a-z0-9._~+/=-]+/gi;
const jwtPattern =
  /\beyJ[a-z0-9_-]{8,}\.[a-z0-9_-]{8,}(?:\.[a-z0-9_-]{8,})?\b/gi;
const sensitiveFieldPattern =
  /password|passwd|pwd|contrase(?:ñ|n)a|token|authorization|secret|api.?key/i;

export function sanitizeAuditText(
  value: string | null | undefined,
  fallback = '—',
  maxLength = 180
): string {
  if (!value) {
    return fallback;
  }

  const normalized = value
    .replace(/[\u0000-\u001f\u007f]+/g, ' ')
    .replace(/\s+/g, ' ')
    .trim();

  if (!normalized) {
    return fallback;
  }

  const redacted = normalized
    .replace(bearerPattern, 'Bearer [REDACTADO]')
    .replace(jwtPattern, '[TOKEN REDACTADO]')
    .replace(secretAssignmentPattern, '$1[REDACTADO]');

  if (redacted.length <= maxLength) {
    return redacted;
  }

  return `${redacted.slice(0, Math.max(0, maxLength - 1)).trimEnd()}…`;
}

export function summarizeChangedFields(value: string | null | undefined): string {
  if (!value) {
    return 'Sin campos informados';
  }

  try {
    const parsed: unknown = JSON.parse(value);
    if (!Array.isArray(parsed)) {
      return 'Detalle no disponible';
    }

    const fields = parsed
      .filter((field): field is string => typeof field === 'string')
      .map((field) =>
        sensitiveFieldPattern.test(field)
          ? 'Campo sensible'
          : sanitizeAuditText(field, 'Campo sin nombre', 64)
      );
    const uniqueFields = [...new Set(fields)];

    if (uniqueFields.length === 0) {
      return 'Sin campos informados';
    }

    const visibleFields = uniqueFields.slice(0, 8);
    const remaining = uniqueFields.length - visibleFields.length;

    return remaining > 0
      ? `${visibleFields.join(', ')} y ${remaining} más`
      : visibleFields.join(', ');
  } catch {
    return 'Detalle no disponible';
  }
}
