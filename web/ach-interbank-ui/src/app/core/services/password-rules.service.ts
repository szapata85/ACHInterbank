import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export interface PasswordRules {
  minLength: number;
  minUppercase: number;
  minNumbers: number;
  minSpecial: number;
  maxSpecial: number | null;
}

const STORAGE_KEY = 'passwordRules';
const DEFAULT_RULES: PasswordRules = {
  minLength: 6,
  minUppercase: 1,
  minNumbers: 1,
  minSpecial: 1,
  maxSpecial: 4
};

@Injectable({ providedIn: 'root' })
export class PasswordRulesService {
  private readonly rulesSubject = new BehaviorSubject<PasswordRules>(this.loadRules());
  readonly rules$ = this.rulesSubject.asObservable();

  getRulesSnapshot(): PasswordRules {
    return this.rulesSubject.value;
  }

  updateRules(rules: PasswordRules): void {
    const normalized = this.normalizeRules(rules);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(normalized));
    this.rulesSubject.next(normalized);
  }

  private loadRules(): PasswordRules {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return DEFAULT_RULES;
    }

    try {
      const parsed = JSON.parse(raw) as PasswordRules;
      return this.normalizeRules(parsed);
    } catch {
      return DEFAULT_RULES;
    }
  }

  private normalizeRules(rules: PasswordRules): PasswordRules {
    return {
      minLength: Math.max(1, Number(rules.minLength) || DEFAULT_RULES.minLength),
      minUppercase: Math.max(0, Number(rules.minUppercase) || 0),
      minNumbers: Math.max(0, Number(rules.minNumbers) || 0),
      minSpecial: Math.max(0, Number(rules.minSpecial) || 0),
      maxSpecial: rules.maxSpecial === null ? null : Math.max(0, Number(rules.maxSpecial) || 0)
    };
  }
}
