import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, Observable, catchError, map, of, tap } from 'rxjs';
import { BrandingSettings } from '../models/branding.model';
import { ApiService } from './api.service';

@Injectable({ providedIn: 'root' })
export class BrandingService {
  private readonly api = inject(ApiService);
  private readonly brandingSubject = new BehaviorSubject<BrandingSettings>({});
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
        const fallback = this.brandingSubject.value;
        this.brandingSubject.next(fallback);
        return of(fallback);
      })
    );
  }

  private persistBranding(settings: BrandingSettings): void {
    const next: BrandingSettings = { ...this.brandingSubject.value, ...settings };
    this.brandingSubject.next(next);
  }
}
