import {
  ErrorHandler,
  ENVIRONMENT_INITIALIZER,
  EnvironmentProviders,
  Injectable,
  inject,
  makeEnvironmentProviders
} from '@angular/core';

export const CHUNK_RELOAD_STORAGE_KEY = 'achinterbank:chunk-reload';
export const CHUNK_RELOAD_GUARD_WINDOW_MS = 2 * 60 * 1000;

interface ChunkReloadGuardState {
  timestamp: number;
  reason: string;
}

@Injectable({
  providedIn: 'root'
})
export class BrowserPageReloader {
  reload(): void {
    window.location.reload();
  }
}

@Injectable({
  providedIn: 'root'
})
export class ChunkLoadRecoveryService {
  private listenersRegistered = false;

  constructor(private readonly pageReloader: BrowserPageReloader) {}

  registerGlobalListeners(): void {
    if (this.listenersRegistered || typeof window === 'undefined') {
      return;
    }

    this.listenersRegistered = true;
    window.addEventListener('error', this.onWindowError, true);
    window.addEventListener('unhandledrejection', this.onUnhandledRejection);
  }

  handle(error: unknown): boolean {
    const reason = this.toReason(error);

    if (!reason || !this.isChunkLoadFailure(reason)) {
      return false;
    }

    const now = Date.now();
    if (!this.canReload(now)) {
      return false;
    }

    this.persistGuard(now, reason);
    this.pageReloader.reload();
    return true;
  }

  private onWindowError = (event: Event): void => {
    const handled = this.handle(this.extractWindowError(event));

    if (handled) {
      event.preventDefault();
    }
  };

  private onUnhandledRejection = (event: PromiseRejectionEvent): void => {
    const handled = this.handle(event.reason);

    if (handled) {
      event.preventDefault();
    }
  };

  private extractWindowError(event: Event): unknown {
    if (event instanceof ErrorEvent) {
      return event.error ?? event.message;
    }

    return event;
  }

  private isChunkLoadFailure(reason: string): boolean {
    return /ChunkLoadError/i.test(reason)
      || /Loading chunk .* failed/i.test(reason)
      || /Failed to fetch dynamically imported module/i.test(reason)
      || /Importing a module script failed/i.test(reason);
  }

  private canReload(now: number): boolean {
    const guard = this.readGuard();
    if (!guard) {
      return true;
    }

    return now - guard.timestamp >= CHUNK_RELOAD_GUARD_WINDOW_MS;
  }

  private persistGuard(timestamp: number, reason: string): void {
    try {
      window.sessionStorage.setItem(CHUNK_RELOAD_STORAGE_KEY, JSON.stringify({ timestamp, reason }));
    } catch {
      // Session storage is best effort only; the handler still works without it.
    }
  }

  private readGuard(): ChunkReloadGuardState | null {
    try {
      const raw = window.sessionStorage.getItem(CHUNK_RELOAD_STORAGE_KEY);
      if (!raw) {
        return null;
      }

      const parsed = JSON.parse(raw) as Partial<ChunkReloadGuardState>;
      if (typeof parsed.timestamp !== 'number' || typeof parsed.reason !== 'string') {
        return null;
      }

      return { timestamp: parsed.timestamp, reason: parsed.reason };
    } catch {
      return null;
    }
  }

  private toReason(error: unknown): string {
    return this.flattenError(error).trim();
  }

  private flattenError(value: unknown, depth = 0): string {
    if (depth > 3 || value == null) {
      return '';
    }

    if (typeof value === 'string') {
      return value;
    }

    if (value instanceof Error) {
      return [value.name, value.message].filter(Boolean).join(' ');
    }

    if (typeof value === 'object') {
      const record = value as Record<string, unknown>;
      return [record['name'], record['message'], record['reason'], record['error']]
        .map((entry) => this.flattenError(entry, depth + 1))
        .filter(Boolean)
        .join(' ');
    }

    return String(value);
  }
}

@Injectable()
export class ChunkLoadRecoveryErrorHandler extends ErrorHandler {
  constructor(private readonly recovery: ChunkLoadRecoveryService) {
    super();
  }

  override handleError(error: unknown): void {
    if (this.recovery.handle(error)) {
      return;
    }

    super.handleError(error);
  }
}

export function provideChunkLoadRecovery(): EnvironmentProviders {
  return makeEnvironmentProviders([
    BrowserPageReloader,
    ChunkLoadRecoveryService,
    ChunkLoadRecoveryErrorHandler,
    { provide: ErrorHandler, useExisting: ChunkLoadRecoveryErrorHandler },
    {
      provide: ENVIRONMENT_INITIALIZER,
      multi: true,
      useValue: () => {
        inject(ChunkLoadRecoveryService).registerGlobalListeners();
      }
    }
  ]);
}
