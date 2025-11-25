import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { map } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class LoadingService {
  private readonly counter = new BehaviorSubject<number>(0);

  readonly pending$: Observable<number> = this.counter.asObservable();
  readonly isLoading$: Observable<boolean> = this.pending$.pipe(map((value) => value > 0));

  show(): void {
    this.counter.next(this.counter.value + 1);
  }

  hide(): void {
    const next = this.counter.value - 1;
    this.counter.next(next < 0 ? 0 : next);
  }

  reset(): void {
    this.counter.next(0);
  }
}
