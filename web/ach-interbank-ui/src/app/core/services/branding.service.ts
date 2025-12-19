import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, catchError, map, of, tap } from 'rxjs';
import { BrandingSettings } from '../models/branding.model';
import { ApiService } from './api.service';

const STORAGE_KEY = 'ach-branding-settings';

@Injectable({ providedIn: 'root' })
export class BrandingService {
  private readonly api = inject(ApiService);
  private readonly brandingSubject = new BehaviorSubject<BrandingSettings>(this.loadFromStorage());
  readonly branding$: Observable<BrandingSettings> = this.brandingSubject.asObservable();

  constructor() {
    this.refreshFromServer().subscribe();
  }

  updateBranding(settings: Partial<BrandingSettings>): Observable<BrandingSettings> {
    return this.api
      .put<BrandingSettings>('api/users/branding', settings)
      .pipe(
        map((response) => response ?? {}),
        tap((saved) => this.persistBranding(saved))
      );
  }

  getBrandingSnapshot(): BrandingSettings {
    return this.brandingSubject.value;
  }

  refreshFromServer(): Observable<BrandingSettings> {
    return this.api.get<BrandingSettings>('api/users/branding').pipe(
      map((settings) => settings ?? {}),
      tap((settings) => this.persistBranding(settings)),
      catchError(() => {
        const stored = this.loadFromStorage();
        this.brandingSubject.next(stored);
        return of(stored);
      })
    );
  }

  private loadFromStorage(): BrandingSettings {
    if (typeof localStorage === 'undefined') {
      return {};
    }

    const stored = localStorage.getItem(STORAGE_KEY);
    if (!stored) {
      return {};
    }

    try {
      return JSON.parse(stored) as BrandingSettings;
    } catch {
      return {};
    }
  }

  private saveToStorage(settings: BrandingSettings): void {
    if (typeof localStorage === 'undefined') {
      return;
    }

    localStorage.setItem(STORAGE_KEY, JSON.stringify(settings));
  }

  private persistBranding(settings: BrandingSettings): void {
    const next: BrandingSettings = { ...this.brandingSubject.value, ...settings };
    this.brandingSubject.next(next);
    this.saveToStorage(next);
  }
}
