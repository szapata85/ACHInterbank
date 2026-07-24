import { HttpErrorResponse } from '@angular/common/http';
import { MonoTypeOperatorFunction, retry, throwError, timer } from 'rxjs';

export const NACHA_CONFIG_READ_MAX_RETRIES = 2;
export const NACHA_CONFIG_READ_FALLBACK_DELAY_MS = 1000;

export interface NachaConfigReadRetryEvent {
  retryNumber: number;
  delayMs: number;
  retryAfter: string | null;
}

export function retryNachaConfigRead<T>(
  onRetry?: (event: NachaConfigReadRetryEvent) => void
): MonoTypeOperatorFunction<T> {
  return retry<T>({
    count: NACHA_CONFIG_READ_MAX_RETRIES,
    delay: (error: unknown, retryNumber: number) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 429) {
        return throwError(() => error);
      }

      const retryAfter = error.headers.get('Retry-After');
      const delayMs = retryAfterDelayMs(retryAfter);
      const event = { retryNumber, delayMs, retryAfter };
      onRetry?.(event);
      console.warn('[NACHA Config] Lectura limitada temporalmente; se reintentará.', event);
      return timer(delayMs);
    }
  });
}

export function retryAfterDelayMs(retryAfter: string | null): number {
  const normalized = retryAfter?.trim() ?? '';
  if (normalized) {
    const seconds = Number(normalized);
    if (Number.isFinite(seconds) && seconds >= 0) {
      return Math.ceil(seconds * 1000);
    }

    const retryAt = Date.parse(normalized);
    if (Number.isFinite(retryAt)) {
      return Math.max(0, retryAt - Date.now());
    }
  }

  return NACHA_CONFIG_READ_FALLBACK_DELAY_MS;
}
