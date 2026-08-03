import { Injectable, computed, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class LoadingService {
  private readonly counter = signal(0);
  readonly isLoading = computed(() => this.counter() > 0);

  show(): void {
    this.counter.update(value => value + 1);
  }

  hide(): void {
    this.counter.update(value => Math.max(0, value - 1));
  }

  reset(): void {
    this.counter.set(0);
  }
}
