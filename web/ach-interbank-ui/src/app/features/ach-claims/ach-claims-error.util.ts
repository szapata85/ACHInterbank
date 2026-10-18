import { HttpErrorResponse } from '@angular/common/http';
import { getHttpErrorMessage } from '../../core/utils/http-error-message.util';

export function claimsErrorMessage(error: unknown, fallback: string): string {
  if (error instanceof HttpErrorResponse && error.status === 409) {
    return 'Esta solicitud ya fue procesada anteriormente o contiene información diferente a una solicitud registrada.';
  }

  const message = getHttpErrorMessage(error, fallback)
    .replace(/caso\s+Claims/gi, 'reclamación')
    .replace(/\bClaims\b/gi, 'reclamaciones')
    .replace(/\bClaimCase\b/gi, 'reclamación')
    .replace(/\bClaim\b/gi, 'reclamación')
    .replace(/deadline/gi, 'fecha límite de gestión')
    .replace(/expiración/gi, 'vencimiento')
    .replace(/\bBadRequest\b|\bConflict\b|\bNotFound\b/gi, '')
    .replace(/\s{2,}/g, ' ')
    .trim();

  return message || fallback;
}

export function newIdempotencyKey(): string {
  const source = globalThis.crypto;
  if (source?.randomUUID) {
    return source.randomUUID();
  }
  const random = Math.random().toString(36).slice(2);
  return `ui-${Date.now()}-${random}`;
}
