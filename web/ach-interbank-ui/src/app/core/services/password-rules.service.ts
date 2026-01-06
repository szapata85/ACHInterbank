import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, catchError, map, of, tap } from 'rxjs';
import { ApiService } from './api.service';

export interface PasswordRules {
  minLength: number;
  minUppercase: number;
  minNumbers: number;
  minSpecial: number;
  maxSpecial: number | null;
}

const DEFAULT_RULES: PasswordRules = {
  minLength: 6,
  minUppercase: 1,
  minNumbers: 1,
  minSpecial: 1,
  maxSpecial: 4
};

@Injectable({ providedIn: 'root' })
export class PasswordRulesService {
  private readonly api = inject(ApiService);
  private readonly rulesSubject = new BehaviorSubject<PasswordRules>(DEFAULT_RULES);
  readonly rules$ = this.rulesSubject.asObservable();

  constructor() {
    this.refreshFromServer().subscribe();
  }

  getRulesSnapshot(): PasswordRules {
    return this.rulesSubject.value;
  }

  updateRules(rules: PasswordRules): Observable<PasswordRules> {
    const normalized = this.normalizeRules(rules);
    return this.api.put<PasswordRules>('api/users/password-rules', normalized).pipe(
      map((response) => this.normalizeRules(response ?? normalized)),
      tap((saved) => this.rulesSubject.next(saved)),
      catchError(() => {
        const fallback = this.rulesSubject.value;
        this.rulesSubject.next(fallback);
        return of(fallback);
      })
    );
  }

  refreshFromServer(): Observable<PasswordRules> {
    return this.api.get<PasswordRules>('api/users/password-rules').pipe(
      map((rules) => this.normalizeRules(rules ?? DEFAULT_RULES)),
      tap((rules) => this.rulesSubject.next(rules)),
      catchError(() => {
        const fallback = this.rulesSubject.value;
        this.rulesSubject.next(fallback);
        return of(fallback);
      })
    );
  }

  private normalizeRules(rules: PasswordRules): PasswordRules {
    const parsedMinLength = Number(rules.minLength);
    const minLength = Number.isFinite(parsedMinLength) ? parsedMinLength : DEFAULT_RULES.minLength;

    return {
      minLength: Math.max(1, minLength),
      minUppercase: Math.max(0, Number(rules.minUppercase) || 0),
      minNumbers: Math.max(0, Number(rules.minNumbers) || 0),
      minSpecial: Math.max(0, Number(rules.minSpecial) || 0),
      maxSpecial: rules.maxSpecial === null ? null : Math.max(0, Number(rules.maxSpecial) || 0)
    };
  }
}
