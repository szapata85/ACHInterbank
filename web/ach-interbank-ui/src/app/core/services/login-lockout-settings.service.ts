import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, tap } from 'rxjs';
import { ApiService } from './api.service';

export interface LoginLockoutSettings {
  maxFailedAttempts: number;
  lockoutMinutes: number;
}

const DEFAULT_SETTINGS: LoginLockoutSettings = {
  maxFailedAttempts: 5,
  lockoutMinutes: 5
};

@Injectable({ providedIn: 'root' })
export class LoginLockoutSettingsService {
  private readonly api = inject(ApiService);
  private readonly settingsSubject = new BehaviorSubject<LoginLockoutSettings>(DEFAULT_SETTINGS);
  readonly settings$ = this.settingsSubject.asObservable();

  constructor() {
    this.refreshFromServer().subscribe();
  }

  getSettingsSnapshot(): LoginLockoutSettings {
    return this.settingsSubject.value;
  }

  updateSettings(settings: LoginLockoutSettings): Observable<LoginLockoutSettings> {
    return this.api.put<LoginLockoutSettings>('api/users/login-lockout', settings).pipe(
      tap((response) => this.settingsSubject.next(response))
    );
  }

  refreshFromServer(): Observable<LoginLockoutSettings> {
    return this.api.get<LoginLockoutSettings>('api/users/login-lockout').pipe(
      tap((response) => this.settingsSubject.next(response))
    );
  }
}
